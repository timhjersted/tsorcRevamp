using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items.Weapons.Magic;
using tsorcRevamp.NPCs.Invaders;

namespace tsorcRevamp.NPCs.Bosses.Attraidies
{
    public enum AttraidiesPose : byte
    {
        Carry,
        Aim,
        Overhead,
        Conduct,
        Throw,
    }

    /// <summary>
    /// The body shared by every Attraidies appearance: Moon Lord Mask, Solar Cultist Robe and Soul Arrow Staff at 1.5x.
    /// The staff is held in the far hand, whose arm draws behind the head, so raised poses never cover the mask.
    /// Each puppet stays in the Custom phase and hovers; its fight runs from TickForm on every machine, with spawns and
    /// decisions gated to the server. Poses are derived from synced fight state and never synced themselves.
    /// </summary>
    public abstract class AttraidiesPuppet : PuppetNPC
    {
        public const int BodyWidth = 42;
        public const int BodyHeight = 66;
        public const int RigWidth = 28;
        public const int RigHeight = 44;
        public const float BodyScale = 1.5f;

        // Facing-space staff angles: 0 = ahead, negative = up.
        public const float CarryAngle = -0.35f;
        public const float OverheadAngle = -MathHelper.PiOver2;
        public const float ConductHighAngle = -1.3f;
        public const float ConductLowAngle = -0.3f;
        public const float ConductSweepRate = 0.07f;
        public const int ThrowTicks = 8;

        // Near (free) hand angles in the same space. Hanging straight down is +90 degrees. Raised poses swing out behind
        // the body through values above 180 degrees, so the easing never carries this arm forward across the face.
        public const float FreeHandHangAngle = MathHelper.PiOver2;
        public const float FreeHandAimAngle = 0.9f;
        public const float FreeHandRaisedAngle = 3.7f;
        public const float FreeHandConductHighAngle = 3.5f;
        public const float FreeHandConductLowAngle = 2.4f;

        // Grip-to-gem length of the staff in rig pixels, before the 1.5x feet scaling.
        private const float StaffTipReach = 43f;

        // The sprite's shaft runs along its diagonal, with the gem toward the upper right.
        private const float StaffSpriteAngle = MathHelper.PiOver4;
        private static readonly Vector2 StaffGripNorm = new Vector2(0.16f, 0.84f);
        private const float StaffDrawScale = 0.9f;
        private const float PoseBlendRate = 0.18f;
        private const int CustomHoldTicks = 2;
        private const float FacingDeadZone = 8f;
        private const float TeleportFlashDistance = 160f;

        protected static readonly Color ArrivalColor = new Color(170, 70, 255);

        private AttraidiesPose currentPose = AttraidiesPose.Carry;
        private int poseTicks;
        private Vector2 poseAimPoint;
        private Vector2 throwDirection = Vector2.UnitX;
        private float staffAngle = CarryAngle;
        private float freeHandAngle = FreeHandHangAngle;
        private bool freeHandPosed;
        private int facing = 1;
        private bool firstTickDone;
        private Vector2 lastCenter;

        protected AttraidiesPose CurrentPose => currentPose;

        /// <summary>Overrides the pose's staff angle for scripted raises that must follow an exact clock.</summary>
        protected float? ScriptedStaffAngle;

        /// <summary>Overrides the pose's free hand for gestures, or null to use the pose's own.</summary>
        protected float? FreeHandGesture;

        protected Player TargetPlayer => Main.player[NPC.target];
        protected static bool IsAuthority => Main.netMode != NetmodeID.MultiplayerClient;

        // ── Identity and loadout ────────────────────────────────────────────
        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(Attraidies)) + "_TomePortrait";
        public override string BossHeadTexture => UsefulFunctions.RefactorableFilepath(typeof(Attraidies)) + "_Head_Boss";

        public override bool UsesPuppetDifficultyScaling => false;
        protected override bool AnnounceInvasion => false;
        protected override bool AnnounceInvaderDefeat => false;
        protected override bool DespawnsOnPartyWipe => false;
        protected override string InvaderTitle => "Attraidies";
        protected override int HeadArmorItemType => ItemID.BossMaskMoonlord;
        protected override int BodyArmorItemType => ItemID.WhiteLunaticRobe;

        // The robe supplies its own skirt; a separate leg item would cover it.
        protected override int LegsArmorItemType => 0;
        protected override int HeadArmorDyeItemType => ArmorDyeItemType;
        protected override int BodyArmorDyeItemType => ArmorDyeItemType;
        protected virtual int ArmorDyeItemType => 0;
        protected override int MeleeWeaponItemType => -1;
        protected override int RangedWeaponItemType => -1;
        protected override int MagicWeaponItemType => ModContent.ItemType<FarronDart>();
        protected override int MeleeDamage => 0;
        protected override int RangedDamage => 0;
        protected override int EstusChargesMax => 0;
        protected override int OnKillDustCount => 0;
        protected override int CasualStrollChance => 0;
        protected override int UnblockableFinisherBlocks => 0;
        protected override bool HoldAttackSelection => true;
        protected override bool SlowDownDuringCustom => false;
        protected override bool CustomAttackCommitted => true;

        // Hovering with a standing lower body: the airborne frame would raise both arms, and no form walks.
        protected override bool WalkCycleWhileAirborne => true;
        protected override float WalkAnimationSpeedMultiplier => 0f;

        // ── Body size ───────────────────────────────────────────────────────
        // A 28x44 rig drawn at 1.5x about its feet fills the 42x66 hitbox. The base rig position sits half a rig width
        // left of the feet, so this offset is what puts the drawn body on NPC.Center.
        protected override int PuppetVisualWidth => RigWidth;
        protected override int PuppetVisualHeight => RigHeight;
        protected override float PuppetDrawScale => BodyScale;
        protected override Vector2 PuppetVisualOffset => new Vector2(RigWidth * 0.5f, 0f);
        internal override float BodyOpacity => 1f - NPC.alpha / 255f;

        // ── Staff ───────────────────────────────────────────────────────────
        protected override bool IndependentBackHandWeapon => true;
        protected override float IndependentBackHandFacingAngle => staffAngle;
        protected override float IndependentBackHandDrawRotation => staffAngle + StaffSpriteAngle;

        // Palm position measured from the far shoulder with the arm hanging. This path also follows the drawn rig offset.
        protected override Vector2? IndependentBackHandGripFromPivot => new Vector2(0f, 11f);

        protected override float? FreeFrontArmFacingAngle
        {
            get
            {
                if (!freeHandPosed)
                {
                    return null;
                }

                return freeHandAngle;
            }
        }

        /// <summary>The gem, valid on a dedicated server: the far-hand grip plus the staff's reach, then the 1.5x feet scaling.</summary>
        internal Vector2 StaffTip
        {
            get
            {
                Vector2 hand = PuppetBackHandPosition;

                float worldAngle = staffAngle;
                if (NPC.direction != 1)
                {
                    worldAngle = MathHelper.Pi - staffAngle;
                }

                Vector2 unscaledTip = hand + worldAngle.ToRotationVector2() * StaffTipReach;
                return NPC.Bottom + (unscaledTip - NPC.Bottom) * BodyScale;
            }
        }

        // ── Setup ───────────────────────────────────────────────────────────
        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[Type] = 1;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Confused] = true;
        }

        protected void ApplyBodyDefaults()
        {
            NPC.aiStyle = -1;
            NPC.width = BodyWidth;
            NPC.height = BodyHeight;
            NPC.scale = 1f;
            NPC.damage = 0;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.lavaImmune = true;
            NPC.knockBackResist = 0f;
        }

        protected override void DoMeleeAttack()
        {
        }

        protected override void DoRangedAttack()
        {
        }

        // Every form positions itself; the ground navigator must never run.
        protected override void RunMovementAI(float speedMult)
        {
        }

        protected virtual NPCDespawnHandler DespawnHandler => null;
        protected virtual bool PlaysArrivalBurst => false;
        protected virtual bool ShowsTeleportFlash => true;
        protected virtual Color TeleportFlashColor => ArrivalColor;

        /// <summary>The point this puppet turns toward. A fixed-facing script returns its own side instead.</summary>
        protected virtual Vector2 FacingPoint => TargetPlayer.Center;

        /// <summary>One tick of the form's fight, run on every machine from the Custom phase.</summary>
        protected abstract void TickForm();

        // ── Per-tick shell ──────────────────────────────────────────────────
        public override void AI()
        {
            // The whole fight is hyper-armored set-piece time, so nothing may pause the Custom clock.
            tsorcRevampGlobalNPC globalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            globalNPC.StaggerTimer = 0;
            globalNPC.DodgeTimer = 0;
            NPC.knockBackResist = 0f;

            DespawnHandler?.TargetAndDespawn(NPC.whoAmI);
            if (!NPC.active)
            {
                return;
            }

            if (Phase != AttackPhase.Custom)
            {
                StartCustomAttack(CustomHoldTicks);
            }

            PhaseTimer = CustomHoldTicks;

            if (!firstTickDone)
            {
                firstTickDone = true;
                EquipWeapon(PuppetHandSlot.Back, new PuppetWeapon(MagicWeaponItemType, damage: 0, handleNorm: StaffGripNorm, drawScale: StaffDrawScale));
                lastCenter = NPC.Center;
                facing = ResolveFacing();

                if (PlaysArrivalBurst)
                {
                    BurstDust(NPC.Center, ArrivalColor, 60, 7f);
                }
            }

            base.AI();

            if (!NPC.active)
            {
                return;
            }

            facing = ResolveFacing();
            NPC.direction = facing;
            NPC.spriteDirection = facing;
            TickPose();

            // A jump this long is a teleport on any machine, so both ends get their dust without a packet.
            if (ShowsTeleportFlash && Vector2.Distance(lastCenter, NPC.Center) > TeleportFlashDistance)
            {
                PlayTeleportFlash(lastCenter, NPC.Center);
            }

            lastCenter = NPC.Center;
        }

        protected virtual void PlayTeleportFlash(Vector2 from, Vector2 to)
        {
            BurstDust(from, TeleportFlashColor, 24, 5f);
            BurstDust(to, TeleportFlashColor, 24, 5f);
        }

        protected override void DoCustomTick(int ticksRemaining)
        {
            TickForm();
        }

        private int ResolveFacing()
        {
            Vector2 point = FacingPoint;
            if (currentPose == AttraidiesPose.Aim)
            {
                point = poseAimPoint;
            }

            float offsetX = point.X - NPC.Center.X;
            if (Math.Abs(offsetX) < FacingDeadZone)
            {
                return facing;
            }

            return Math.Sign(offsetX);
        }

        // ── Poses ───────────────────────────────────────────────────────────
        protected void SetPose(AttraidiesPose pose)
        {
            if (pose != currentPose)
            {
                currentPose = pose;
                poseTicks = 0;
            }
        }

        protected void SetAimPose(Vector2 worldPoint)
        {
            SetPose(AttraidiesPose.Aim);
            poseAimPoint = worldPoint;
        }

        protected void SetThrowPose(Vector2 worldDirection)
        {
            SetPose(AttraidiesPose.Throw);
            throwDirection = worldDirection.SafeNormalize(Vector2.UnitX * facing);
        }

        private float FacingAngleOf(Vector2 worldDirection)
        {
            float angle = worldDirection.ToRotation();
            if (facing != 1)
            {
                angle = MathHelper.Pi - angle;
            }

            return MathHelper.WrapAngle(angle);
        }

        /// <summary>The full pose table. Public and pure so the offline pose render reads the same numbers as the game.</summary>
        public static void ResolvePoseAngles(AttraidiesPose pose, int ticksInPose, float aimFacingAngle, float throwFacingAngle, out float staff, out float? freeHand)
        {
            float conductWave = 0.5f + 0.5f * (float)Math.Sin(ticksInPose * ConductSweepRate);

            switch (pose)
            {
                case AttraidiesPose.Aim:
                    staff = aimFacingAngle;
                    freeHand = FreeHandAimAngle;
                    break;

                case AttraidiesPose.Overhead:
                    staff = OverheadAngle;
                    freeHand = FreeHandRaisedAngle;
                    break;

                case AttraidiesPose.Conduct:
                    staff = MathHelper.Lerp(ConductHighAngle, ConductLowAngle, conductWave);
                    freeHand = MathHelper.Lerp(FreeHandConductLowAngle, FreeHandConductHighAngle, conductWave);
                    break;

                case AttraidiesPose.Throw:
                    float throwProgress = MathHelper.Clamp(ticksInPose / (float)ThrowTicks, 0f, 1f);
                    float easedThrow = MathHelper.SmoothStep(0f, 1f, throwProgress);
                    staff = OverheadAngle + MathHelper.WrapAngle(throwFacingAngle - OverheadAngle) * easedThrow;
                    freeHand = MathHelper.Lerp(FreeHandRaisedAngle, throwFacingAngle + 0.6f, easedThrow);
                    break;

                default:
                    staff = CarryAngle;
                    freeHand = null;
                    break;
            }
        }

        // Eases toward the pose on every machine, so server tips and drawn staffs agree without syncing angles.
        private void TickPose()
        {
            poseTicks++;

            float aimFacingAngle = FacingAngleOf(poseAimPoint - NPC.Center);
            float throwFacingAngle = FacingAngleOf(throwDirection);
            ResolvePoseAngles(currentPose, poseTicks, aimFacingAngle, throwFacingAngle, out float staffTarget, out float? freeHandTarget);

            if (ScriptedStaffAngle.HasValue)
            {
                staffAngle = ScriptedStaffAngle.Value;
            }
            else if (currentPose == AttraidiesPose.Throw)
            {
                staffAngle = staffTarget;
            }
            else
            {
                staffAngle += MathHelper.WrapAngle(staffTarget - staffAngle) * PoseBlendRate;
            }

            if (FreeHandGesture.HasValue)
            {
                freeHandTarget = FreeHandGesture;
            }

            float freeHandGoal = FreeHandHangAngle;
            if (freeHandTarget.HasValue)
            {
                freeHandGoal = freeHandTarget.Value;
            }

            freeHandAngle += (freeHandGoal - freeHandAngle) * PoseBlendRate;

            // Once a released arm has settled, hand it back to the body frame.
            bool settledAtRest = !freeHandTarget.HasValue && Math.Abs(freeHandAngle - FreeHandHangAngle) < 0.05f;
            freeHandPosed = !settledAtRest;
        }

        // ── Shared fight helpers ────────────────────────────────────────────
        /// <summary>The living player on this machine, or null on a dedicated server or while dead.</summary>
        protected static Player LocalLivingPlayer
        {
            get
            {
                if (Main.dedServ)
                {
                    return null;
                }

                Player player = Main.LocalPlayer;
                if (!player.active || player.dead)
                {
                    return null;
                }

                return player;
            }
        }

        // Clocks are written by the server, so clients get them back through periodic world data.
        protected static void ShareWorldTime()
        {
            if (Main.netMode == NetmodeID.Server && Main.GameUpdateCount % 60 == 0)
            {
                NetMessage.SendData(MessageID.WorldData);
            }
        }

        /// <summary>Server only. Places the next form on these feet with this facing and target; optionally hands over life.</summary>
        protected void SpawnNextForm<T>(bool removeSelf, int transferredLife = 0) where T : ModNPC
        {
            if (!IsAuthority)
            {
                return;
            }

            // NewNPC treats Y as the new NPC's bottom edge, so passing the feet keeps them planted.
            Vector2 feet = NPC.Bottom;
            int nextIndex = NPC.NewNPC(NPC.GetSource_FromAI(), (int)feet.X, (int)feet.Y, ModContent.NPCType<T>());
            if (nextIndex < Main.maxNPCs)
            {
                NPC nextForm = Main.npc[nextIndex];
                nextForm.direction = NPC.direction;
                nextForm.spriteDirection = NPC.spriteDirection;
                nextForm.target = NPC.target;

                if (transferredLife > 0)
                {
                    nextForm.life = transferredLife;
                }

                if (Main.netMode == NetmodeID.Server)
                {
                    NetMessage.SendData(MessageID.SyncNPC, number: nextIndex);
                }
            }

            if (removeSelf)
            {
                NPC.active = false;
                if (Main.netMode == NetmodeID.Server)
                {
                    NetMessage.SendData(MessageID.SyncNPC, number: NPC.whoAmI);
                }
            }
        }

        protected void DeactivateAll(int npcType)
        {
            if (!IsAuthority)
            {
                return;
            }

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                if (Main.npc[i].active && Main.npc[i].type == npcType)
                {
                    Main.npc[i].active = false;
                    if (Main.netMode == NetmodeID.Server)
                    {
                        NetMessage.SendData(MessageID.SyncNPC, number: i);
                    }
                }
            }
        }

        // ── Dust ────────────────────────────────────────────────────────────
        /// <summary>One mote drawn inward to a point: the telegraph language for tips, spawn points and teleport ends.</summary>
        protected static void GatherDust(Vector2 point, Color color, float radius, float scale = 0.9f)
        {
            if (Main.dedServ)
            {
                return;
            }

            Vector2 offset = Main.rand.NextVector2CircularEdge(radius, radius);
            Vector2 inward = -offset.SafeNormalize(Vector2.UnitY) * (radius / 14f);
            Dust mote = Dust.NewDustPerfect(point + offset, DustID.TintableDustLighted, inward, 70, color, scale);
            mote.noGravity = true;
            Lighting.AddLight(point, color.ToVector3() * 0.25f);
        }

        protected static void BurstDust(Vector2 point, Color color, int count, float speed)
        {
            if (Main.dedServ)
            {
                return;
            }

            for (int i = 0; i < count; i++)
            {
                Vector2 velocity = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(0.3f, 1f) * speed;
                Dust mote = Dust.NewDustPerfect(point, DustID.TintableDustLighted, velocity, 70, color, Main.rand.NextFloat(1f, 1.6f));
                mote.noGravity = true;
            }
        }

        // ── Drawing ─────────────────────────────────────────────────────────
        /// <summary>Restores the sprite batch state the NPC pass normally runs in, after a shader pass.</summary>
        protected static void RestoreNpcBatch()
        {
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (BodyOpacity <= 0.01f)
            {
                return false;
            }

            return base.PreDraw(spriteBatch, screenPos, drawColor);
        }
    }
}

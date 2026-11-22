using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.Bestiary;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Banners;
using tsorcRevamp.Content.Projectiles.Enemy.Weapons;
using tsorcRevamp.NPCs.AI;
using tsorcRevamp.NPCs.Invaders;
using tsorcRevamp.Systems;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.NPCs.Enemies
{
    public class ObsidianGuard : PuppetNPC
    {
        private enum Move : byte
        {
            None,
            MeasuredShot,
            DrawnShot,
            BackstepShot,
            SpearCheck
        }

        public const int SpearTellTicks = 48;
        public const int SpearStrokeTicks = 22;
        public const int SpearSecondTellStart = SpearTellTicks + SpearStrokeTicks;
        public const int SpearSecondStrike = SpearSecondTellStart + SpearTellTicks;
        public const int SingleSpearTicks = SpearSecondTellStart + 32;
        public const int DoubleSpearTicks = SpearSecondStrike + SpearStrokeTicks + 44;
        public const float SpearCarryRotation = -0.30f;
        public const float SpearCarryGrip = 0.65f;
        public const float SpearDrawnGrip = 0.20f;
        public const float SpearExtendedGrip = 0.88f;
        public const float SpearHeadLength = 20f;
        public const float SpearWidth = 10f;
        public static readonly Vector2 SpearHeadTexel = new Vector2(1f, 1f);
        public static readonly Vector2 SpearButtTexel = new Vector2(68f, 68f);

        private static readonly int[] ExpertLife = { 600, 1200, 10000 };
        private static readonly int[] Defense = { 10, 24, 42 };
        private static readonly int[] ArrowExpert = { 36, 72, 120 };
        private static readonly int[] DrawnExpert = { 48, 92, 156 };
        private static readonly int[] SpearExpert = { 40, 80, 136 };
        private static readonly int[] SoulValue = { 1000, 3000, 9000 };
        private static readonly float[] Poise = { 18f, 24f, 30f };
        private static readonly float[] Knockback = { 0.45f, 0.40f, 0.35f };
        private static readonly float[] WalkSpeed = { 1.7f, 2f, 2.2f };
        private static readonly Color Obsidian = new Color(155, 108, 210);

        private readonly AttackBag<Move> shotBag = new AttackBag<Move>(cards =>
        {
            cards.Add(Move.MeasuredShot);
            cards.Add(Move.DrawnShot);
        });
        private readonly HashSet<int> hitPlayers = new HashSet<int>();
        private Move move;
        private int stage;
        private bool statsInitialized;
        private bool spearEquipped;
        private bool equipping;
        private bool retreating;
        private bool readyShot;
        private bool followup;
        private int actionTicks;
        private int actionDelay = 30;
        private int retreatCooldown;
        private int actionTarget = -1;
        private int facing = 1;
        private int previousBeat = -1;
        private float spearPitch;
        private float retreatDistance;
        private Vector2 retreatStart;
        private Vector2 shotAim = Vector2.UnitX;
        private bool hasPreviousBlade;
        private Vector2 previousOrigin;
        private Vector2 previousTip;

        protected override string InvaderTitle => "Obsidian Guard";
        protected override bool AnnounceInvasion => false;
        protected override bool AnnounceInvaderDefeat => false;
        protected override bool DespawnsOnPartyWipe => false;
        protected override int EstusChargesMax => 0;
        protected override int CasualStrollChance => 0;
        protected override bool EvadesProjectiles => false;
        protected override int HeadArmorItemType => ItemID.ObsidianHelm;
        protected override int BodyArmorItemType => ItemID.ObsidianShirt;
        protected override int LegsArmorItemType => ItemID.ObsidianPants;
        protected override int MeleeWeaponItemType => ItemID.ThunderSpear;
        protected override int RangedWeaponItemType => ItemID.DemonBow;
        protected override WeaponArchetype MeleeArchetype => WeaponArchetype.None;
        protected override WeaponArchetype RangedArchetype => WeaponArchetype.None;
        protected override int MeleeDamage => EnemyDamage.Projectile(SpearExpert[stage]);
        protected override int RangedDamage => EnemyDamage.Projectile(ArrowExpert[stage]);
        protected override float MeleeRange => 0f;
        protected override bool HoldAttackSelection => !readyShot;
        protected override float TopSpeed => WalkSpeed[stage];
        protected override float Acceleration => 0.07f;
        protected override float RangedRange => 640f;
        protected override float MinRangedRange => 128f;
        protected override int MaxRangedBurst => 1;
        protected override int RangedTelegraphTicks => move == Move.DrawnShot ? 64 : 42;
        protected override int RangedAttackTicks => move == Move.DrawnShot ? 8 : 6;
        protected override int RangedCooldownAfterUse => 0;
        protected override RangedStyle RangedAnimStyle => RangedStyle.Bow;
        protected override bool ShowRangedWeaponDuringRecovery => true;
        protected override bool ShowWeaponDuringNeutral => true;
        protected override bool HoldNeutralWeaponOnCompositeArm => spearEquipped;
        protected override bool UseCompositeArmSwing => true;
        protected override bool UseCompositeArmForAdditionalPhase => Phase == AttackPhase.Custom
            || Phase == AttackPhase.RangedTelegraph || Phase == AttackPhase.RangedAttack;
        protected override bool UseBowStringDrawPose => true;
        protected override bool HasRangedJumpOverride => true;
        protected override bool SlowDownDuringCustom => !retreating;
        protected override bool DrawWeaponAsSpear => spearEquipped;
        protected override bool MirrorSpearRotationByFacing => true;
        protected override bool MirrorMeleeSwingRotationByFacing => true;
        protected override string SpearDrawTexturePath => "Terraria/Images/Projectile_" + ProjectileID.ThunderSpear;
        protected override Vector2 SpearTextureSize => new Vector2(70f, 70f);
        protected override Vector2 SpearHeadNorm => SpearHeadTexel / 70f;
        protected override Vector2 SpearBaseNorm => SpearButtTexel / 70f;
        protected override float SpearDrawRotationOffset => MathHelper.PiOver2;
        protected override string NockedBowArrowTexture => "Terraria/Images/Projectile_" + ProjectileID.UnholyArrow;
        // Installed Demon Bow: 18x40, riser (14,20), inset string x2..3/y10..29; retain both decorated limb tips.
        protected override Vector2 GetHeldRangedGripNorm(int itemType) => new Vector2(14f / 18f, 0.5f);
        protected override float GetHeldRangedDrawScale(int itemType) => 1f;
        protected override Rectangle GetBowStringTexels(int itemType) => new Rectangle(2, 10, 2, 20);
        protected override float BowAimAngle => (float)Math.Atan2(shotAim.Y, shotAim.X * facing);

        private bool IsNeutral => Phase == AttackPhase.Idle || Phase == AttackPhase.CasualStroll;
        private bool IsBowAction => Phase == AttackPhase.RangedTelegraph || Phase == AttackPhase.RangedAttack;
        private int CustomElapsed => actionTicks - PhaseTimer;

        protected override float? CustomWeaponRotation
        {
            get
            {
                if (move != Move.SpearCheck || equipping)
                {
                    return SpearCarryRotation;
                }

                ResolveSpearPose(CustomElapsed, followup, out float rotation, out _, out _);
                return rotation + spearPitch;
            }
        }

        protected override float? OverrideSpearGrip(float swingClockT)
        {
            if (Phase != AttackPhase.Custom || move != Move.SpearCheck || equipping)
            {
                return SpearCarryGrip;
            }

            ResolveSpearPose(CustomElapsed, followup, out _, out float grip, out _);
            return grip;
        }

        protected override Vector2? ResolveCompositeHandAnchor(float armRotation, Player.CompositeArmStretchAmount stretch)
        {
            if (!spearEquipped)
            {
                return null;
            }

            // Dedicated servers have no draw Player. Solve the same wrist from the current rig and stretch
            // geometry, so collision, tip glints and the rendered spear use one hand position on every peer.
            float angle = armRotation + MathHelper.PiOver2;
            float armReach = 10f;

            if (stretch == Player.CompositeArmStretchAmount.ThreeQuarters)
            {
                armReach = 8f;
            }
            else if (stretch == Player.CompositeArmStretchAmount.Quarter)
            {
                armReach = 6f;
            }
            else if (stretch == Player.CompositeArmStretchAmount.None)
            {
                armReach = 4f;
            }

            Vector2 wrist = angle.ToRotationVector2() * armReach;
            wrist += new Vector2(-4f * NPC.direction, -2f);
            wrist += new Vector2(0f, 3f * NPC.direction).RotatedBy(angle);
            Vector2 center = NPC.Bottom - new Vector2(PuppetVisualWidth * 0.5f, PuppetVisualHeight * 0.5f);
            center += PuppetVisualOffset;
            center.Y += NPC.gfxOffY;
            return center + wrist;
        }

        public override void SetDefaults()
        {
            NPC.width = 20;
            NPC.height = 42;
            NPC.aiStyle = -1;
            NPC.lifeMax = ExpertLife[0];
            NPC.defense = Defense[0];
            NPC.value = SoulValue[0];
            NPC.knockBackResist = Knockback[0];
            NPC.npcSlots = 2f;
            NPC.HitSound = SoundID.NPCHit4;
            NPC.DeathSound = SoundID.NPCDeath2;
            EnemyDamage.SetContact(NPC, 0);
            Banner = Type;
            BannerItem = ModContent.ItemType<ObsidianGuardBanner>();

            tsorcRevampGlobalNPC globalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            globalNPC.NavSearchRadius = 60;
            globalNPC.RemembersLastKnownPos = true;
            globalNPC.PatrolUsesNavigation = true;
            globalNPC.PatrolAnchorSource = PatrolAnchorSource.SpawnPoint;
            globalNPC.PatrolRange = 10;
            globalNPC.CanTeleport = false;
            globalNPC.TeleportMaxCharges = 0;
            globalNPC.PoiseStaggerResetsAI = true;
        }

        public override void SetBestiary(BestiaryDatabase database, BestiaryEntry bestiaryEntry)
        {
            bestiaryEntry.Info.Add(BestiaryDatabaseNPCsPopulator.CommonTags.SpawnConditions.Biomes.Meteor);
            bestiaryEntry.Info.Add(new FlavorTextBestiaryInfoElement("Mods.tsorcRevamp.NPCs.ObsidianGuard.BestiaryDescription"));
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            npcLoot.Add(ItemDropRule.Common(ItemID.Obsidian, 2, 3, 6));
            npcLoot.Add(ItemDropRule.Common(ItemID.UnholyArrow, 3, 8, 16));
        }

        public override void AI()
        {
            bool authority = Main.netMode != NetmodeID.MultiplayerClient;

            // First-AI assignment runs after vanilla's scaling. The synchronized stage is also used on late joins.
            if (!statsInitialized && authority)
            {
                stage = 0;

                if (tsorcRevampWorld.SuperHardMode)
                {
                    stage = 2;
                }
                else if (Main.hardMode)
                {
                    stage = 1;
                }

                NPC.lifeMax = EnemyBalance.ModeScaledLife(NPC, ExpertLife[stage]);
                NPC.life = NPC.lifeMax;
                // Value has already received vanilla's mode multiplier by the first AI tick.
                NPC.value = SoulValue[stage] * Main.GameModeInfo.EnemyMoneyDropMultiplier;
                statsInitialized = true;
                NPC.netUpdate = true;
            }

            NPC.defense = Defense[stage];
            NPC.knockBackResist = Knockback[stage];
            tsorcRevampGlobalNPC globalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            globalNPC.PoiseMax = Poise[stage];
            ConfigureCustomRangedWeapon(RangedWeaponItemType, RangedStyle.Bow);

            if (authority)
            {
                if (actionDelay > 0)
                {
                    actionDelay--;
                }

                if (retreatCooldown > 0)
                {
                    retreatCooldown--;
                }

                if (IsNeutral)
                {
                    NPC.TargetClosest(false);
                }

                if (actionTarget >= 0 && !IsNeutral)
                {
                    NPC.target = actionTarget;
                }

                if (NPC.HasValidTarget && globalNPC.StaggerTimer <= 0)
                {
                    Player target = Main.player[NPC.target];

                    if (IsNeutral && actionDelay == 0 && NPC.velocity.Y == 0f && !readyShot)
                    {
                        SelectAction(target);
                    }

                    // A player can follow the retreat. Release the pending shot so the close-range stance can take over.
                    if (IsNeutral && readyShot && (NPC.Distance(target.Center) < 128f
                        || !Collision.CanHitLine(PuppetBowGripPosition, 1, 1, target.Center, 1, 1)))
                    {
                        readyShot = false;
                        move = Move.None;
                        actionDelay = 24;
                        NPC.netUpdate = true;
                    }

                    if (IsBowAction && Phase == AttackPhase.RangedTelegraph)
                    {
                        int lockTicks = 10;

                        if (move == Move.DrawnShot)
                        {
                            lockTicks = 14;
                        }

                        if (PhaseTimer > lockTicks)
                        {
                            LockAim(target, false);

                            if (PhaseTimer % 6 == 0 || PhaseTimer == lockTicks + 1)
                            {
                                NPC.netUpdate = true;
                            }
                        }

                        Vector2 muzzle = PuppetBowGripPosition;
                        bool clear = Collision.CanHitLine(muzzle, 1, 1, target.Center, 1, 1);

                        if (!clear || NPC.Distance(target.Center) < 88f || NPC.velocity.Y != 0f)
                        {
                            NPC.velocity.X = 0f;
                            EnterPhase(AttackPhase.RangedRecovery, 24);
                        }
                    }
                }
                else if (!IsNeutral && globalNPC.StaggerTimer <= 0)
                {
                    readyShot = false;
                    retreating = false;
                    equipping = false;
                    move = Move.None;
                    EnterPhase(AttackPhase.Idle, 0);
                    actionDelay = 30;
                }
            }

            AttackPhase previousPhase = Phase;
            base.AI();

            if (!IsNeutral)
            {
                NPC.direction = facing;
                NPC.spriteDirection = facing;
            }

            if (Phase == AttackPhase.Custom && move == Move.SpearCheck && !equipping)
            {
                TickSpearCollision();
            }
            else
            {
                hasPreviousBlade = false;
            }

            if (IsBowAction)
            {
                DebugAttackLabel = move.ToString();
            }
            else if (Phase == AttackPhase.Custom && move == Move.SpearCheck && !equipping)
            {
                DebugAttackLabel = "Spear Check";
            }
            else
            {
                DebugAttackLabel = null;
            }

            if (IsNeutral && previousPhase != Phase)
            {
                if (retreating)
                {
                    retreating = false;
                    readyShot = true;
                    retreatCooldown = 240;
                }
                else
                {
                    equipping = false;
                    actionDelay = 24;
                    move = Move.None;
                }

                if (authority)
                {
                    NPC.netUpdate = true;
                }
            }

            // Recovery keeps the weapon that performed the action; the next equip gets its own harmless 30t.
            if (IsNeutral || Phase == AttackPhase.RangedRecovery)
            {
                int itemType = RangedWeaponItemType;

                if (spearEquipped)
                {
                    itemType = MeleeWeaponItemType;
                }

                SetDisplayWeapon(itemType, false);
            }

            if (Phase == AttackPhase.RangedRecovery)
            {
                NPC.velocity.X = 0f;
            }
        }

        private void SelectAction(Player target)
        {
            float distance = NPC.Distance(target.Center);
            bool wantsSpear = distance < 96f;

            if (spearEquipped)
            {
                wantsSpear = distance < 160f;
            }

            actionTarget = NPC.target;
            LockAim(target, wantsSpear);

            if (wantsSpear != spearEquipped)
            {
                spearEquipped = wantsSpear;
                equipping = true;
                actionTicks = 30;
                int itemType = RangedWeaponItemType;

                if (spearEquipped)
                {
                    itemType = MeleeWeaponItemType;
                }

                StartCustomAttack(actionTicks, itemType, false);
                return;
            }

            if (spearEquipped)
            {
                if (distance <= 80f && Math.Abs(target.Center.Y - NPC.Center.Y) <= 32f)
                {
                    move = Move.SpearCheck;
                    followup = stage > 0;
                    actionTicks = SingleSpearTicks;

                    if (followup)
                    {
                        actionTicks = DoubleSpearTicks;
                    }

                    hitPlayers.Clear();
                    previousBeat = -1;
                    StartCustomAttack(actionTicks, MeleeWeaponItemType, true);
                }

                return;
            }

            // A physical step has no dodge immunity. Validate every 8px of the chosen 3–6-tile lane before committing.
            float approaching = target.velocity.X * (target.Center.X - NPC.Center.X);

            if (distance < 128f && approaching < -0.1f && retreatCooldown == 0)
            {
                int requestedTiles = Main.rand.Next(3, 7);
                retreatDistance = requestedTiles * 16f;
                bool safe = true;

                for (int i = 1; i <= requestedTiles * 2; i++)
                {
                    Vector2 position = NPC.position - new Vector2(facing * i * 8f, 0f);
                    bool wall = Collision.SolidCollision(position, NPC.width, NPC.height);
                    bool ground = Collision.SolidCollision(position + new Vector2(0f, NPC.height), NPC.width, 6);

                    if (wall || !ground)
                    {
                        safe = false;
                        break;
                    }
                }

                if (safe)
                {
                    move = Move.BackstepShot;
                    retreating = true;
                    retreatStart = NPC.Center;
                    actionTicks = (int)Math.Ceiling(retreatDistance / 3f);
                    StartCustomAttack(actionTicks, RangedWeaponItemType, false);
                    return;
                }
            }

            bool clearLane = Collision.CanHitLine(PuppetBowGripPosition, 1, 1, target.Center, 1, 1);

            if (distance < 128f || !clearLane)
            {
                return;
            }

            bool selected = shotBag.TryDraw(candidate =>
            {
                if (candidate == Move.DrawnShot)
                {
                    return distance >= 192f && distance <= 640f;
                }

                return distance <= 560f;
            }, out Move selectedMove);

            if (selected)
            {
                move = selectedMove;
                readyShot = true;
                NPC.netUpdate = true;
            }
        }

        private void LockAim(Player target, bool spear)
        {
            facing = 1;

            if (target.Center.X < NPC.Center.X)
            {
                facing = -1;
            }

            NPC.direction = facing;
            NPC.spriteDirection = facing;
            Vector2 difference = target.Center - PuppetBowGripPosition;
            float angle = (float)Math.Atan2(difference.Y, Math.Abs(difference.X));
            angle = MathHelper.Clamp(angle, -MathHelper.PiOver4, MathHelper.PiOver4);
            shotAim = new Vector2((float)Math.Cos(angle) * facing, (float)Math.Sin(angle));

            if (spear)
            {
                Vector2 spearDifference = target.Center - NPC.Center;
                spearPitch = (float)Math.Atan2(spearDifference.Y, Math.Abs(spearDifference.X));
                spearPitch = MathHelper.Clamp(spearPitch, -0.35f, 0.35f);
            }
        }

        protected override void RunMovementAI(float speedMult)
        {
            if (!IsNeutral || readyShot)
            {
                return;
            }

            float stopDistance = 220f;

            if (spearEquipped)
            {
                stopDistance = 48f;
            }
            else if (NPC.HasValidTarget && NPC.Distance(Main.player[NPC.target].Center) < 128f)
            {
                // Bridge the bow's minimum range and the 96px spear-switch threshold instead of idling in that gap.
                stopDistance = 64f;
            }

            SmartFighter4AI.Run(NPC, topSpeed: TopSpeed * speedMult, acceleration: Acceleration,
                doorBreakingDamage: 4, attackRange: stopDistance);
        }

        protected override void TickRangedJumpOverride()
        {
            // The shared ranged movement hook also supports grounded walking casts; it bypasses the base shot brake.
            NPC.velocity.X = 0f;

            if (move == Move.MeasuredShot && NPC.velocity.Y == 0f)
            {
                Vector2 nextPosition = NPC.position + new Vector2(facing * TopSpeed * 0.65f, 0f);
                bool wall = Collision.SolidCollision(nextPosition, NPC.width, NPC.height);
                bool ground = Collision.SolidCollision(nextPosition + new Vector2(0f, NPC.height), NPC.width, 6);

                if (!wall && ground)
                {
                    NPC.velocity.X = facing * TopSpeed * 0.65f;
                }
            }
        }

        protected override void FaceTargetForRangedShot(Player target)
        {
            NPC.direction = facing;
            NPC.spriteDirection = facing;
        }

        protected override void ModifyRangedBurst(bool secondary, ref int itemType, ref RangedStyle style,
            ref Color flashColor, ref int telegraphTicks, ref int attackTicks, ref int recoveryTicks)
        {
            readyShot = false;
            telegraphTicks = 42;
            attackTicks = 6;
            recoveryTicks = 30;
            flashColor = Obsidian;

            if (move == Move.DrawnShot)
            {
                telegraphTicks = 64;
                attackTicks = 8;
                recoveryTicks = 48;
            }
            else if (move == Move.BackstepShot)
            {
                recoveryTicks = 42;
            }
        }

        protected override void DoMeleeAttack()
        {
            // Spear Check uses the custom controller and its visible spearhead capsule.
        }

        protected override void DoRangedAttack()
        {
            Vector2 origin = PuppetBowGripPosition;
            float speed = 10f;
            int expertDamage = ArrowExpert[stage];

            if (move == Move.DrawnShot)
            {
                speed = 12f;
                expertDamage = DrawnExpert[stage];
            }

            if (!Main.dedServ)
            {
                SoundEngine.PlaySound(SoundID.Item5 with { Volume = 0.65f }, origin);
            }

            if (Main.netMode != NetmodeID.MultiplayerClient && NPC.HasValidTarget
                && Collision.CanHitLine(origin, 1, 1, Main.player[NPC.target].Center, 1, 1)
                && !Collision.SolidCollision(origin - new Vector2(2f), 4, 4))
            {
                Projectile.NewProjectile(NPC.GetSource_FromAI(), origin, shotAim * speed,
                    ModContent.ProjectileType<ObsidianGuardArrow>(), EnemyDamage.Projectile(expertDamage), 2f, Main.myPlayer);
            }
        }

        protected override void DoRangedTelegraphVFX(bool secondary, float progress)
        {
            if (Main.dedServ || Phase != AttackPhase.RangedTelegraph)
            {
                return;
            }

            Vector2 origin = PuppetBowGripPosition;

            if (Main.GameUpdateCount % 3 == 0)
            {
                Dust dust = Dust.NewDustPerfect(origin, DustID.DemonTorch, -shotAim * 0.3f, 140, default, 0.55f);
                dust.noGravity = true;
            }

            if (Main.GameUpdateCount % 6 == 0)
            {
                for (int i = 1; i <= 12; i++)
                {
                    Vector2 point = origin + shotAim * (i * 16f);

                    if (Collision.SolidCollision(point - Vector2.One, 2, 2))
                    {
                        break;
                    }

                    Dust line = Dust.NewDustPerfect(point, DustID.DemonTorch, Vector2.Zero, 200, default, 0.35f);
                    line.noGravity = true;
                }
            }
        }

        protected override void DoCustomTick(int ticksRemaining)
        {
            NPC.velocity.X = 0f;

            if (equipping)
            {
                return;
            }

            if (retreating)
            {
                float travelled = Math.Abs(NPC.Center.X - retreatStart.X);
                Vector2 nextPosition = NPC.position - new Vector2(facing * 3f, 0f);
                bool safe = !Collision.SolidCollision(nextPosition, NPC.width, NPC.height)
                    && Collision.SolidCollision(nextPosition + new Vector2(0f, NPC.height), NPC.width, 6);

                if (travelled < retreatDistance && safe)
                {
                    NPC.velocity.X = -facing * Math.Min(3f, retreatDistance - travelled);
                }

                return;
            }

            if (move != Move.SpearCheck)
            {
                return;
            }

            int elapsed = CustomElapsed;
            bool authority = Main.netMode != NetmodeID.MultiplayerClient;

            if (authority && NPC.HasValidTarget)
            {
                Player target = Main.player[NPC.target];
                bool tracking = elapsed < SpearTellTicks - 8
                    || followup && elapsed >= SpearSecondTellStart && elapsed < SpearSecondStrike - 8;

                if (tracking)
                {
                    LockAim(target, true);

                    if (elapsed % 6 == 0 || elapsed == SpearTellTicks - 9 || elapsed == SpearSecondStrike - 9)
                    {
                        NPC.netUpdate = true;
                    }
                }

                if (followup && elapsed == SpearSecondTellStart)
                {
                    bool reachable = NPC.Distance(target.Center) <= 80f
                        && Math.Abs(target.Center.Y - NPC.Center.Y) <= 32f;

                    if (!reachable)
                    {
                        followup = false;
                        actionTicks = SingleSpearTicks;
                        PhaseTimer = SingleSpearTicks - elapsed;
                        NPC.netUpdate = true;
                    }
                }
            }

            if (!Main.dedServ)
            {
                bool beginsTell = elapsed == 0 || followup && elapsed == SpearSecondTellStart;
                bool beginsStrike = elapsed == SpearTellTicks || followup && elapsed == SpearSecondStrike;

                if (beginsTell || beginsStrike)
                {
                    SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.5f }, NPC.Center);
                }

                bool tipGlint = elapsed >= SpearTellTicks - 12 && elapsed < SpearTellTicks
                    || followup && elapsed >= SpearSecondStrike - 12 && elapsed < SpearSecondStrike;

                if (tipGlint && elapsed % 3 == 0)
                {
                    Dust glint = Dust.NewDustPerfect(PuppetSpearTipPosition, DustID.SilverCoin, Vector2.Zero, 140, default, 0.6f);
                    glint.noGravity = true;
                }
            }
        }

        // One authored pose feeds the game and offline preview. Each 48t tell ends on the 2t harmless drive;
        // live t2..9, 4t end hold, 8t withdrawal, then exposed recovery. No body push or electric payload.
        public static void ResolveSpearPose(int elapsed, bool secondThrust, out float rotation, out float grip, out bool live)
        {
            int strike = SpearTellTicks;
            int tellStart = 0;

            if (secondThrust && elapsed >= SpearSecondTellStart)
            {
                tellStart = SpearSecondTellStart;
                strike = SpearSecondStrike;
            }

            rotation = MathHelper.PiOver4 - 0.2f;
            grip = SpearDrawnGrip;
            live = false;

            if (elapsed < strike)
            {
                float tell = MathHelper.Clamp((elapsed - tellStart) / (float)SpearTellTicks, 0f, 1f);
                rotation = MathHelper.SmoothStep(SpearCarryRotation, rotation, tell);
                grip = MathHelper.SmoothStep(SpearCarryGrip, SpearDrawnGrip, tell);
                return;
            }

            int stroke = elapsed - strike;

            if (stroke < 2)
            {
                rotation = MathHelper.SmoothStep(MathHelper.PiOver4 - 0.2f, MathHelper.PiOver4, stroke / 2f);
            }
            else if (stroke < 10)
            {
                rotation = MathHelper.PiOver4;
                float extension = MathHelper.Clamp((stroke - 2) / 5f, 0f, 1f);
                grip = MathHelper.SmoothStep(SpearDrawnGrip, SpearExtendedGrip, extension);
                live = true;
            }
            else if (stroke < 14)
            {
                rotation = MathHelper.PiOver4;
                grip = SpearExtendedGrip;
            }
            else if (stroke < SpearStrokeTicks)
            {
                float withdrawal = (stroke - 14) / 8f;
                rotation = MathHelper.SmoothStep(MathHelper.PiOver4, SpearCarryRotation, withdrawal);
                grip = MathHelper.SmoothStep(SpearExtendedGrip, SpearCarryGrip, withdrawal);
            }
            else
            {
                rotation = SpearCarryRotation;
                grip = SpearCarryGrip;
            }
        }

        private void TickSpearCollision()
        {
            ResolveSpearPose(CustomElapsed, followup, out _, out _, out bool live);

            if (!live || Main.netMode == NetmodeID.MultiplayerClient)
            {
                hasPreviousBlade = false;
                return;
            }

            int beat = 0;

            if (CustomElapsed >= SpearSecondStrike)
            {
                beat = 1;
            }

            if (beat != previousBeat)
            {
                hitPlayers.Clear();
                hasPreviousBlade = false;
                previousBeat = beat;
            }

            Vector2 tip = PuppetSpearTipPosition;
            Vector2 origin = tip - PuppetWeaponDirection * SpearHeadLength;

            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player player = Main.player[i];

                if (!player.active || player.dead || hitPlayers.Contains(i) || NPC.Distance(player.Center) > 140f)
                {
                    continue;
                }

                Vector2 hitOrigin = origin;
                Vector2 hitTip = tip;
                bool intersects = MeleeBladeCollision.SegmentIntersectsRect(origin, tip, SpearWidth, player.getRect());

                if (hasPreviousBlade)
                {
                    intersects = MeleeBladeCollision.SweptSegmentIntersectsRect(previousOrigin, previousTip,
                        origin, tip, SpearWidth, player.getRect(), out hitOrigin, out hitTip);
                }

                bool clear = Collision.CanHitLine(NPC.Center, 1, 1, player.Center, 1, 1);

                if (intersects && clear)
                {
                    Vector2 contact = MeleeBladeCollision.ClosestPointOnSegment(hitOrigin, hitTip, player.Center);
                    int projectileIndex = Projectile.NewProjectile(NPC.GetSource_FromAI(), contact, Vector2.Zero,
                        ModContent.ProjectileType<PuppetMeleeHitbox>(), MeleeDamage, 2f, Main.myPlayer, SpearWidth, SpearWidth);

                    if (projectileIndex < Main.maxProjectiles)
                    {
                        // Keep the shared contact-delivery lifetime for remote players; geometry is resolved only in live ticks.
                        Main.projectile[projectileIndex].netUpdate = true;
                    }

                    hitPlayers.Add(i);
                }
            }

            previousOrigin = origin;
            previousTip = tip;
            hasPreviousBlade = true;
        }

        public override void OnStagger(NPC npc)
        {
            readyShot = false;
            retreating = false;
            equipping = false;
            followup = false;
            move = Move.None;
            actionDelay = 30;
            hasPreviousBlade = false;
            retreatCooldown = Math.Max(retreatCooldown, 120);
            base.OnStagger(npc);
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write((byte)stage);
            writer.Write(statsInitialized);
            writer.Write((byte)move);
            writer.Write(spearEquipped);
            writer.Write(equipping);
            writer.Write(retreating);
            writer.Write(readyShot);
            writer.Write(followup);
            writer.Write(actionTicks);
            writer.Write(actionDelay);
            writer.Write(retreatCooldown);
            writer.Write(actionTarget);
            writer.Write(facing);
            writer.Write(spearPitch);
            writer.Write(retreatDistance);
            writer.Write(retreatStart.X);
            writer.Write(retreatStart.Y);
            writer.Write(shotAim.X);
            writer.Write(shotAim.Y);
            base.SendExtraAI(writer);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            stage = Math.Clamp((int)reader.ReadByte(), 0, 2);
            statsInitialized = reader.ReadBoolean();
            move = (Move)reader.ReadByte();
            spearEquipped = reader.ReadBoolean();
            equipping = reader.ReadBoolean();
            retreating = reader.ReadBoolean();
            readyShot = reader.ReadBoolean();
            followup = reader.ReadBoolean();
            actionTicks = reader.ReadInt32();
            actionDelay = reader.ReadInt32();
            retreatCooldown = reader.ReadInt32();
            actionTarget = reader.ReadInt32();
            facing = reader.ReadInt32();
            spearPitch = reader.ReadSingle();
            retreatDistance = reader.ReadSingle();
            retreatStart = new Vector2(reader.ReadSingle(), reader.ReadSingle());
            shotAim = new Vector2(reader.ReadSingle(), reader.ReadSingle());
            base.ReceiveExtraAI(reader);

            if (statsInitialized)
            {
                NPC.lifeMax = EnemyBalance.ModeScaledLife(NPC, ExpertLife[stage]);
            }
        }
    }
}

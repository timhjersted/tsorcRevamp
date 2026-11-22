using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;
using tsorcRevamp.Content.Items.Armor.Enemy.DworcShamanSet;
using tsorcRevamp.Content.Items.Weapons.Enemy;
using tsorcRevamp.Content.Projectiles.Enemy;
using tsorcRevamp.Content.Projectiles.Enemy.Dworc;
using tsorcRevamp.NPCs.Invaders;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.NPCs.Enemies.Dworc
{
    // Local generation IDs are used server-side for heal recipients. Caster IDs also travel in
    // family ExtraAI so owned summons/flasks cannot adopt a recycled NPC slot on another peer.
    public sealed class DworcSpawnIdentity : GlobalNPC
    {
        private static int nextIdentity;
        public override bool InstancePerEntity => true;
        public int Token { get; private set; }

        public override void OnSpawn(NPC npc, IEntitySource source)
        {
            nextIdentity = nextIdentity % 16000000 + 1;
            Token = nextIdentity;
        }
    }

    public abstract class DworcCasterPuppet : PuppetNPC
    {
        protected enum CasterKind : byte
        {
            Alchemist, Shaman, Abysswalker
        }

        public enum CasterMove : byte
        {
            None, Strike, Storm, Spirit, VenomFlask, FireFlask, StormFlask, SuppressionFlask, SelfHeal, AllyHeal
        }

        private readonly struct HealMark
        {
            public readonly int Index;
            public readonly int Token;

            public HealMark(int index, int token)
            {
                Index = index;
                Token = token;
            }
        }

        public const int VenomExpertDamage = 28;
        public const int FireExpertDamage = 72;
        public const int StormFlaskExpertDamage = 36;
        public const int ShamanStrikeExpertDamage = 72;
        public const int ShamanStormExpertDamage = 100;
        public const int AbyssStrikeExpertDamage = 108;
        public const int AbyssStormExpertDamage = 120;
        public const int ShamanSpiritExpertDamage = 80;
        public const int AbyssSpiritExpertDamage = 140;
        public static readonly Vector2 StaffGrip = new Vector2(15f / 18f, 27f / 42f);
        public static readonly Vector2 StaffTipOffset = new Vector2(-9f, -26f);

        protected abstract CasterKind Kind { get; }
        private AttackBag<CasterMove> attacks;
        private readonly List<HealMark> healMarks = new List<HealMark>();
        private readonly int[] cooldowns = new int[11];
        private CasterMove move;
        private CasterMove pendingMove;
        private int elapsed;
        private int tellTicks;
        private int tailTicks;
        private int recoveryTicks;
        private int targetIndex;
        private int facing = 1;
        private int sequence;
        private int releasedSequence = -1;
        private int selectionDelay = 30;
        private int sinceRelease = 10000;
        private int chainCount;
        private int chainMaximum = 1;
        private float chainVariance;
        private bool initialized;
        private bool aimLocked;
        private bool utilityUsed;
        private bool lichSlain;
        private Vector2 aim = Vector2.UnitX;
        private Vector2 landing;
        private Vector2 launchVelocity;

        public int SpawnToken { get; private set; }
        public CasterMove CurrentMove => move;
        public bool IsAbyssCaster => Kind == CasterKind.Abysswalker;
        private bool Authority => Main.netMode != NetmodeID.MultiplayerClient;
        private bool Neutral => move == CasterMove.None;
        private bool Utility => move == CasterMove.SelfHeal || move == CasterMove.AllyHeal;
        private bool Throwing => move >= CasterMove.VenomFlask && move <= CasterMove.SuppressionFlask;
        private bool Recovering => elapsed > tellTicks + tailTicks;
        private int SelfHealAmount
        {
            get
            {
                if (Kind == CasterKind.Alchemist)
                {
                    return 30;
                }

                return Kind == CasterKind.Shaman ? 20 : 60;
            }
        }

        public override bool UsesPuppetDifficultyScaling => false;
        protected override bool AnnounceInvasion => false;
        protected override bool AnnounceInvaderDefeat => false;
        protected override bool DespawnsOnPartyWipe => false;
        protected override string InvaderTitle => "Dworc";
        protected override int HeadArmorItemType => ModContent.ItemType<DworcShamanHeadpiece>();
        protected override int BodyArmorItemType => ModContent.ItemType<DworcShamanBreastplate>();
        protected override int LegsArmorItemType => ModContent.ItemType<DworcShamanGreaves>();
        protected override int MeleeWeaponItemType => -1;
        protected override int RangedWeaponItemType => -1;
        protected override int MagicWeaponItemType => ModContent.ItemType<EnemyDworcStaff>();
        protected override int MeleeDamage => 0;
        protected override int RangedDamage => 0;
        protected override int EstusChargesMax => 0;
        protected override int CasualStrollChance => 0;
        protected override int UnblockableFinisherBlocks => 0;
        protected override bool HoldAttackSelection => true;
        protected override bool ShowWeaponDuringNeutral => true;
        protected override bool IndependentBackHandWeapon => true;
        protected override float IndependentBackHandFacingAngle => StaffAngle;
        protected override float IndependentBackHandDrawRotation => StaffRotation;
        protected override bool UseFrontArmSpriteForBack => true;
        protected override float BackArmPivotOffsetX => 2f;
        protected override bool SlowDownDuringCustom => true;
        protected override bool CustomAttackCommitted
        {
            get
            {
                int commitTick = tellTicks - 12;
                if (move == CasterMove.Storm)
                {
                    commitTick = tellTicks / 2;
                }

                return !Neutral && !Utility && elapsed >= commitTick && elapsed <= tellTicks + tailTicks;
            }
        }
        internal override float BodyOpacity => 1f - NPC.alpha / 255f;
        protected override float PuppetJumpPower => 9f;
        protected override float PuppetJumpBoost => 4f;
        protected override float RunSpeedMult => 1f;
        protected override Color PuppetSkinColor => Color.Black;
        protected override int BodyArmorDyeItemType => HeadArmorDyeItemType;
        protected override int LegsArmorDyeItemType => HeadArmorDyeItemType;
        protected override int HeadArmorDyeItemType
        {
            get
            {
                if (Kind == CasterKind.Alchemist)
                {
                    return ItemID.BrownDye;
                }

                return Kind == CasterKind.Abysswalker ? ItemID.PurpleDye : 0;
            }
        }

        protected override float TopSpeed
        {
            get
            {
                if (Kind == CasterKind.Alchemist)
                {
                    return NPC.life < NPC.lifeMax * 0.5f ? 1.6f : 0.8f;
                }

                if (NPC.life < NPC.lifeMax * 0.3f)
                {
                    return 3f;
                }

                return Kind == CasterKind.Shaman ? 1.5f : 2f;
            }
        }

        protected override float Acceleration
        {
            get
            {
                if (Kind == CasterKind.Alchemist)
                {
                    return 0.02f;
                }

                return Kind == CasterKind.Shaman ? 0.04f : 0.05f;
            }
        }

        private float StaffAngle
        {
            get
            {
                float desired = 0.2f;
                if (!Neutral && !Throwing)
                {
                    if (move == CasterMove.Strike)
                    {
                        desired = -0.65f;
                    }
                    else if (move == CasterMove.Spirit)
                    {
                        desired = -0.75f + SpiritShake * 0.45f;
                    }
                    else
                    {
                        desired = -1.15f;
                    }
                }

                float raiseTicks = move == CasterMove.Spirit ? 12f : 18f;
                float blend = MathHelper.Clamp(elapsed / raiseTicks, 0f, 1f);
                if (Recovering)
                {
                    blend = 1f - MathHelper.Clamp((elapsed - tellTicks - tailTicks) / 20f, 0f, 1f);
                }

                return MathHelper.SmoothStep(0.2f, desired, blend);
            }
        }

        // Lift the rear hand without laying the upright source sprite along the forearm.
        // The same facing-space texture rotation drives rendering and the spell-tip offset.
        private float StaffRotation
        {
            get
            {
                float desired = 0.04f;
                if (!Neutral && !Throwing)
                {
                    desired = -0.12f;
                    if (move == CasterMove.Strike)
                    {
                        desired = 0.1f;
                    }
                    else if (move == CasterMove.Spirit)
                    {
                        desired = 0.04f + SpiritShake;
                    }
                }

                float raiseTicks = move == CasterMove.Spirit ? 12f : 18f;
                float blend = MathHelper.Clamp(elapsed / raiseTicks, 0f, 1f);
                if (Recovering)
                {
                    blend = 1f - MathHelper.Clamp((elapsed - tellTicks - tailTicks) / 20f, 0f, 1f);
                }

                return MathHelper.SmoothStep(0.04f, desired, blend);
            }
        }

        private float SpiritShake
        {
            get
            {
                if (move != CasterMove.Spirit || elapsed < 12 || elapsed > 36)
                {
                    return 0f;
                }

                // Two twelve-tick rocks, eased into and out of the shake window; finish
                // nine ticks before the summon releases so its final tip position settles.
                float progress = (elapsed - 12) % 12 / 12f;
                float envelope = (float)Math.Sin(progress * MathHelper.Pi);
                float oscillation = (float)Math.Sin(progress * MathHelper.TwoPi);
                return oscillation * envelope * 0.22f;
            }
        }

        protected override float? FreeFrontArmFacingAngle
        {
            get
            {
                if (Throwing)
                {
                    return ThrowPose(elapsed, tellTicks);
                }

                if (!Neutral)
                {
                    float gesture = MathHelper.Clamp(elapsed / 18f, 0f, 1f);
                    if (Recovering)
                    {
                        gesture = 1f - MathHelper.Clamp((elapsed - tellTicks - tailTicks) / 20f, 0f, 1f);
                    }

                    return MathHelper.SmoothStep(MathHelper.PiOver2, 1.35f, gesture);
                }

                return null;
            }
        }

        // Shared by runtime and the offline body preview; a held bottle goes behind the head,
        // punches forward at release, then settles while the independent rear staff stays out.
        public static float ThrowPose(int ticks, int tell)
        {
            if (ticks < 0)
            {
                return MathHelper.SmoothStep(MathHelper.PiOver2, -0.5f, MathHelper.Clamp((ticks + 30) / 30f, 0f, 1f));
            }

            if (ticks <= tell - 6)
            {
                return MathHelper.SmoothStep(-0.5f, -2.15f, MathHelper.Clamp(ticks / (float)(tell - 6), 0f, 1f));
            }

            if (ticks <= tell)
            {
                return MathHelper.SmoothStep(-2.15f, -0.1f, (ticks - tell + 6) / 6f);
            }

            if (ticks <= tell + 8)
            {
                return -0.1f;
            }

            return MathHelper.SmoothStep(-0.1f, MathHelper.PiOver2, MathHelper.Clamp((ticks - tell - 8) / 30f, 0f, 1f));
        }

        public Vector2 StaffTip
        {
            get
            {
                Vector2 offset = StaffTipOffset;
                offset.X *= NPC.direction;
                float rotation = StaffRotation * NPC.direction;
                return PuppetBackHandPosition + offset.RotatedBy(rotation) * NPC.scale;
            }
        }

        protected override void DoMeleeAttack()
        {
            // The held staff is a prop; family actions exclusively own outgoing damage.
        }

        protected override void DoRangedAttack()
        {
            // Throws are released once by the family action clock, not a parallel ranged selector.
        }

        public override void SetStaticDefaults()
        {
            // Preserve the old sheet's frame slicing for bestiary/resource consumers. Gameplay
            // rendering and walk/arm clocks are supplied independently by the puppet Player.
            Main.npcFrameCount[Type] = 15;
        }

        public override void SetDefaults()
        {
            NPC.aiStyle = -1;
            NPC.damage = 0;
            NPC.width = 18;
            NPC.height = 40;
            NPC.scale = 1f;
            AnimationType = -1;

            tsorcRevampGlobalNPC global = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            global.NavSearchRadius = 50;
            global.CanUseRopes = true;
            global.RemembersLastKnownPos = true;
            global.CanSelfHeal = false;
            global.CanHealAllies = false;
            global.CanGoInvisible = true;
            global.CanTeleport = true;
            global.CanWalkBackwards = true;
            global.PoiseStaggerResetsAI = true;
            global.InvisibleAlpha = 205;
            float cloakChance = 0.3f;
            if (Kind != CasterKind.Shaman)
            {
                global.InvisibleAlpha = Kind == CasterKind.Alchemist ? 220 : 210;
                cloakChance = 0.45f;
            }

            EvasiveProfile.EvasiveCloak(global, cloakChance, 220);
            EvasiveProfile.DworcShaman(global);
            if (Kind == CasterKind.Shaman)
            {
                // Register the Standard damage urgency bookkeeping, without the old AddAttack scheduler.
                CombatTempoProfile.Standard(global, new CombatComboMove((int)CasterMove.Strike, 0f, float.MaxValue));
            }
        }

        protected override void RunMovementAI(float speedMult)
        {
            if (!Neutral)
            {
                NPC.velocity.X *= 0.7f;
                return;
            }

            SmartFighter4AI.Run(NPC, TopSpeed * speedMult, Acceleration, doorBreakingDamage: 0, attackRange: 700f);
        }

        public override void AI()
        {
            tsorcRevampGlobalNPC global = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            if (!initialized)
            {
                initialized = true;
                EquipWeapon(PuppetHandSlot.Back, new PuppetWeapon(MagicWeaponItemType, damage: 0,
                    handleNorm: StaffGrip, rotationOffset: MathHelper.PiOver2));
                if (Authority)
                {
                    cooldowns[(int)CasterMove.SelfHeal] = 360;
                    cooldowns[(int)CasterMove.AllyHeal] = Kind == CasterKind.Alchemist ? 500 : 400;
                    cooldowns[(int)CasterMove.SuppressionFlask] = 600;
                    SpawnToken = NPC.GetGlobalNPC<DworcSpawnIdentity>().Token;
                    NPC.netUpdate = true;
                }
            }

            if (Authority)
            {
                for (int i = 0; i < cooldowns.Length; i++)
                {
                    cooldowns[i] = Math.Max(0, cooldowns[i] - 1);
                }

                sinceRelease++;
                if (Kind == CasterKind.Abysswalker && !lichSlain)
                {
                    lichSlain = tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<Bosses.SuperHardMode.Fiends.EarthFiendLich>()));
                    if (lichSlain)
                    {
                        NPC.netUpdate = true;
                    }
                }

                if (global.StaggerTimer <= 0)
                {
                    selectionDelay = Math.Max(0, selectionDelay - 1);
                    if (Neutral && selectionDelay == 0 && !global.InEvasion && global.TeleportCountdown == 0 && global.TeleportAppearanceTimer == 0)
                    {
                        NPC.TargetClosest(false);
                        if (NPC.HasValidTarget)
                        {
                            SelectMove();
                        }
                    }
                }
            }

            if (!Neutral)
            {
                PhaseTimer = 2;
                NPC.target = targetIndex;
                NPC.direction = facing;
                NPC.spriteDirection = facing;
                global.IsInvisible = false;
                NPC.alpha = 0;
            }

            global.CanGoInvisible = Neutral;
            global.CanTeleport = Neutral;
            base.AI();
            if (!Neutral)
            {
                NPC.direction = facing;
                NPC.spriteDirection = facing;
                global.AttackCommitted = CustomAttackCommitted;
                global.AttackTelegraphing = elapsed <= tellTicks && !global.AttackCommitted;
            }

            DebugAttackLabel = move + "/" + elapsed;
            if (!Main.dedServ && !Neutral)
            {
                DrawActionDust();
            }
        }

        private void SelectMove()
        {
            if (chainCount == 0 && !utilityUsed)
            {
                if (Kind != CasterKind.Shaman && cooldowns[(int)CasterMove.AllyHeal] == 0)
                {
                    healMarks.Clear();
                    float radius = Kind == CasterKind.Alchemist ? 640f : 800f;
                    for (int i = 0; i < Main.maxNPCs; i++)
                    {
                        NPC ally = Main.npc[i];
                        bool spiritCanHeal = ally.ModNPC is not ShamanSpirit helper || helper.CanReceiveHealing;
                        if (ally.active && spiritCanHeal && i != NPC.whoAmI && !ally.friendly && ally.life > 0 && ally.life < ally.lifeMax && ally.Distance(NPC.Center) <= radius)
                        {
                            healMarks.Add(new HealMark(i, ally.GetGlobalNPC<DworcSpawnIdentity>().Token));
                        }
                    }

                    if (healMarks.Count > 0)
                    {
                        BeginMove(CasterMove.AllyHeal);
                        return;
                    }
                }

                if (cooldowns[(int)CasterMove.SelfHeal] == 0 && NPC.life < NPC.lifeMax)
                {
                    BeginMove(CasterMove.SelfHeal);
                    return;
                }
            }

            attacks ??= new AttackBag<CasterMove>(RefillAttacks);
            if (pendingMove == CasterMove.None && !attacks.TryDraw(IsEligible, out pendingMove))
            {
                // If offense is impossible, a ready utility need not wait behind a vanished target lane.
                utilityUsed = false;
                selectionDelay = 15;
                return;
            }

            if (!IsEligible(pendingMove))
            {
                pendingMove = CasterMove.None;
                selectionDelay = 10;
                return;
            }

            int cap = SchedulingCap(pendingMove);
            int tell = TellDuration(pendingMove);
            int preparation = 0;
            if (pendingMove >= CasterMove.VenomFlask && pendingMove <= CasterMove.SuppressionFlask)
            {
                preparation = 30;
            }

            if (chainCount == 0 && sinceRelease < cap - tell - preparation)
            {
                return;
            }

            CasterMove chosen = pendingMove;
            pendingMove = CasterMove.None;
            BeginMove(chosen);
        }

        private void RefillAttacks(List<CasterMove> cards)
        {
            if (Kind == CasterKind.Alchemist)
            {
                cards.Add(CasterMove.VenomFlask);
                cards.Add(CasterMove.FireFlask);
                cards.Add(CasterMove.StormFlask);
                cards.Add(CasterMove.SuppressionFlask);
                return;
            }

            int strikeCount = 20;
            int stormCount = 4;
            int spiritCount = 1;
            if (Kind == CasterKind.Abysswalker)
            {
                strikeCount = 10;
                stormCount = 3;
                spiritCount = 2;
            }

            for (int i = 0; i < strikeCount; i++)
            {
                cards.Add(CasterMove.Strike);
            }

            for (int i = 0; i < stormCount; i++)
            {
                cards.Add(CasterMove.Storm);
            }

            for (int i = 0; i < spiritCount; i++)
            {
                cards.Add(CasterMove.Spirit);
            }
        }

        private bool IsEligible(CasterMove candidate)
        {
            if (!NPC.HasValidTarget || cooldowns[(int)candidate] > 0)
            {
                return false;
            }

            Player target = Main.player[NPC.target];
            float distance = NPC.Distance(target.Center);
            if (candidate == CasterMove.Spirit)
            {
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    if (Main.npc[i].active && Main.npc[i].ModNPC is ShamanSpirit spirit && spirit.BelongsTo(NPC.whoAmI, SpawnToken))
                    {
                        return false;
                    }
                }

                return distance < 900f;
            }

            if (candidate < CasterMove.VenomFlask)
            {
                // The legacy Shaman bolt lasts only 60 ticks at 8px/t. Keep a 32px
                // origin margin; ring gates likewise leave room for the raised staff tip.
                if (candidate == CasterMove.Strike)
                {
                    float maximumStrikeRange = Kind == CasterKind.Shaman ? 448f : 320f;
                    return distance <= maximumStrikeRange
                        && Collision.CanHitLine(StaffTip, 1, 1, target.Center, 1, 1);
                }

                float maximumStormRange = Kind == CasterKind.Shaman ? 240f : 880f;
                return distance <= maximumStormRange;
            }

            float maximum = 480f;
            if (candidate == CasterMove.VenomFlask)
            {
                maximum = 320f;
            }
            else if (candidate == CasterMove.SuppressionFlask)
            {
                maximum = 640f;
            }

            if (distance < 96f || distance > maximum || !Collision.CanHitLine(NPC.Center, 1, 1, target.Center, 1, 1))
            {
                return false;
            }

            if (candidate == CasterMove.SuppressionFlask)
            {
                if (distance < 140f)
                {
                    return false;
                }

                bool useful = false;
                for (int i = 0; i < Main.maxPlayers; i++)
                {
                    Player player = Main.player[i];
                    if (player.active && !player.dead && player.Distance(target.Center) < 1600f
                        && (!player.HasBuff<Buffs.Debuffs.Suppressed>() || !player.HasBuff<Buffs.Debuffs.GrappleMalfunction>()))
                    {
                        useful = true;
                    }
                }

                if (!useful)
                {
                    return false;
                }
            }

            if (candidate == CasterMove.FireFlask || candidate == CasterMove.StormFlask)
            {
                for (int i = 0; i < Main.maxProjectiles; i++)
                {
                    Projectile projectile = Main.projectile[i];
                    if (!projectile.active)
                    {
                        continue;
                    }

                    if (candidate == CasterMove.FireFlask && projectile.ModProjectile is DworcFireFlame flame && flame.BelongsTo(NPC.whoAmI, SpawnToken))
                    {
                        return false;
                    }

                    if (candidate == CasterMove.StormFlask && projectile.ModProjectile is DworcStormRing ring && ring.BelongsTo(NPC.whoAmI, SpawnToken))
                    {
                        return false;
                    }
                }
            }

            return DworcFlask.TrySolve(candidate, NPC.Center, target, NPC.direction, out _, out _);
        }

        private int TellDuration(CasterMove candidate)
        {
            return candidate switch
            {
                CasterMove.Strike => 36,
                CasterMove.Storm => 90,
                CasterMove.Spirit => 45,
                CasterMove.StormFlask => 60,
                CasterMove.SuppressionFlask => 40,
                CasterMove.SelfHeal => 60,
                CasterMove.AllyHeal => 75,
                _ => 42
            };
        }

        private int SchedulingCap(CasterMove candidate)
        {
            if (candidate == CasterMove.StormFlask)
            {
                return 700;
            }

            if (candidate == CasterMove.Storm)
            {
                if (Kind == CasterKind.Shaman)
                {
                    return 480;
                }

                return lichSlain ? 250 : 400;
            }

            if (candidate == CasterMove.Spirit)
            {
                return Kind == CasterKind.Shaman ? 200 : 300;
            }

            return Kind == CasterKind.Abysswalker ? 120 : 150;
        }

        private void BeginMove(CasterMove chosen)
        {
            move = chosen;
            elapsed = 0;
            if (Throwing)
            {
                elapsed = -30;
            }
            aimLocked = false;
            releasedSequence = -1;
            sequence++;
            targetIndex = NPC.target;
            facing = 1;
            if (Main.player[targetIndex].Center.X < NPC.Center.X)
            {
                facing = -1;
            }

            NPC.direction = facing;
            tellTicks = TellDuration(chosen);
            tailTicks = 8;
            recoveryTicks = 36;
            if (chosen == CasterMove.Strike)
            {
                tailTicks = 6;
                recoveryTicks = 30;
            }
            else if (chosen == CasterMove.Storm || chosen == CasterMove.StormFlask)
            {
                recoveryTicks = Kind == CasterKind.Abysswalker ? 48 : 45;
            }
            else if (chosen == CasterMove.SuppressionFlask || Utility)
            {
                recoveryTicks = 30;
            }

            if (Utility)
            {
                tailTicks = 0;
                utilityUsed = true;
                cooldowns[(int)chosen] = chosen == CasterMove.SelfHeal ? 360 : 400;
                if (chosen == CasterMove.AllyHeal && Kind == CasterKind.Alchemist)
                {
                    cooldowns[(int)chosen] = 500;
                }
            }
            else
            {
                utilityUsed = false;
                if (chainCount == 0 && Kind == CasterKind.Shaman)
                {
                    CombatTempoProfile tempo = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>().CombatTempo;
                    chainMaximum = tempo.GetMaximumChainLength(NPC);
                    chainVariance = Main.rand.NextFloat(-tempo.SequenceVariance, tempo.SequenceVariance);
                }
            }

            NPC.GetGlobalNPC<tsorcRevampGlobalNPC>().EvasiveOnHitRequested = false;
            StartCustomAttack(2);
            NPC.netUpdate = true;
        }

        protected override void DoCustomTick(int ticksRemaining)
        {
            if (Neutral)
            {
                return;
            }

            elapsed++;
            if (Authority && (!Main.player[targetIndex].active || Main.player[targetIndex].dead))
            {
                OnStagger(NPC);
                return;
            }

            if (Authority && !aimLocked && elapsed <= tellTicks)
            {
                Player target = Main.player[targetIndex];
                aim = (target.Center - StaffTip).SafeNormalize(new Vector2(facing, 0f));
                if (Throwing)
                {
                    Vector2 releaseOrigin = PuppetFrontHandPositionAt(-0.1f);
                    if (!DworcFlask.TrySolve(move, releaseOrigin, target, facing, out launchVelocity, out landing))
                    {
                        if (elapsed >= tellTicks - 12)
                        {
                            OnStagger(NPC);
                            return;
                        }
                    }
                }
                else if (move == CasterMove.Strike && Kind == CasterKind.Abysswalker)
                {
                    Vector2 ballistic = UsefulFunctions.BallisticTrajectory(StaffTip, target.Center, 9f);
                    aim = ballistic.SafeNormalize(new Vector2(facing, 0f));
                }

                int lockTick = tellTicks - 12;
                if (move == CasterMove.Storm)
                {
                    lockTick = tellTicks / 2;
                }

                aimLocked = elapsed >= lockTick;
                if (aimLocked || elapsed % 6 == 0)
                {
                    NPC.netUpdate = true;
                }
            }

            if (Authority && elapsed == tellTicks && releasedSequence != sequence)
            {
                releasedSequence = sequence;
                ReleaseMove();
                sinceRelease = 0;
                NPC.netUpdate = true;
            }

            if (Authority && elapsed >= tellTicks + tailTicks + recoveryTicks)
            {
                FinishMove();
            }
        }

        private void ReleaseMove()
        {
            IEntitySource source = NPC.GetSource_FromAI();
            if (move == CasterMove.SelfHeal)
            {
                int amount = Math.Min(SelfHealAmount, NPC.lifeMax - NPC.life);
                if (amount > 0)
                {
                    NPC.life += amount;
                    NPC.HealEffect(amount);
                }

                return;
            }

            if (move == CasterMove.AllyHeal)
            {
                float radius = Kind == CasterKind.Alchemist ? 640f : 800f;
                float percent = Kind == CasterKind.Alchemist ? 0.08f : 0.12f;
                foreach (HealMark mark in healMarks)
                {
                    NPC ally = Main.npc[mark.Index];
                    if (!ally.active || ally.friendly || ally.life <= 0 || ally.GetGlobalNPC<DworcSpawnIdentity>().Token != mark.Token || ally.Distance(NPC.Center) > radius)
                    {
                        continue;
                    }

                    if (ally.ModNPC is ShamanSpirit helper && !helper.CanReceiveHealing)
                    {
                        continue;
                    }

                    int amount = Math.Min(ally.lifeMax - ally.life, Math.Max(1, (int)(ally.lifeMax * percent)));
                    if (amount > 0)
                    {
                        ally.life += amount;
                        ally.HealEffect(amount);
                        ally.netUpdate = true;
                    }
                }

                return;
            }

            if (Throwing)
            {
                int expertDamage = VenomExpertDamage;
                if (move == CasterMove.FireFlask)
                {
                    expertDamage = FireExpertDamage;
                    cooldowns[(int)move] = 360;
                }
                else if (move == CasterMove.StormFlask)
                {
                    expertDamage = StormFlaskExpertDamage;
                }
                else if (move == CasterMove.SuppressionFlask)
                {
                    expertDamage = 0;
                    cooldowns[(int)move] = 1500;
                }

                Projectile.NewProjectile(source, PuppetHandPosition, launchVelocity, ModContent.ProjectileType<DworcFlask>(),
                    EnemyDamage.Projectile(expertDamage), 3f, Main.myPlayer, (int)move, NPC.whoAmI, SpawnToken);
                return;
            }

            if (move == CasterMove.Spirit)
            {
                int index = NPC.NewNPC(source, (int)StaffTip.X, (int)StaffTip.Y, ModContent.NPCType<ShamanSpirit>());
                if (index < Main.maxNPCs && Main.npc[index].ModNPC is ShamanSpirit spirit)
                {
                    Main.npc[index].Center = StaffTip;
                    spirit.Initialize(NPC.whoAmI, SpawnToken, IsAbyssCaster);
                }

                return;
            }

            int projectileType;
            int damage;
            Vector2 velocity = Vector2.Zero;
            if (move == CasterMove.Strike)
            {
                projectileType = ModContent.ProjectileType<DworcPoisonBolt>();
                damage = ShamanStrikeExpertDamage;
                velocity = aim * 8f;
                if (Kind == CasterKind.Abysswalker)
                {
                    projectileType = ModContent.ProjectileType<DworcAbyssBolt>();
                    damage = AbyssStrikeExpertDamage;
                    velocity = aim * 9f;
                }
            }
            else
            {
                projectileType = ModContent.ProjectileType<EnemySpellPoisonStorm>();
                damage = ShamanStormExpertDamage;
                if (Kind == CasterKind.Abysswalker)
                {
                    projectileType = ModContent.ProjectileType<EnemySpellAbyssStorm>();
                    damage = AbyssStormExpertDamage;
                }
            }

            Projectile.NewProjectile(source, StaffTip, velocity, projectileType, EnemyDamage.Projectile(damage), 0f, Main.myPlayer);
        }

        private void FinishMove()
        {
            bool continueChain = false;
            if (!Utility && Kind == CasterKind.Shaman)
            {
                chainCount++;
                tsorcRevampGlobalNPC global = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
                CombatTempoProfile tempo = global.CombatTempo;
                float urgency = tempo.GetRecentDamageUrgency(global.CombatTempoRecentDamageFraction);
                float chance = tempo.GetContinueChance(chainMaximum, urgency, chainVariance);
                continueChain = move == CasterMove.Strike && chainCount < chainMaximum && Main.rand.NextFloat() < chance;
                if (continueChain)
                {
                    selectionDelay = tempo.GetChainGapTicks(NPC, urgency, chainVariance);
                }
                else if (chainCount > 1)
                {
                    selectionDelay = tempo.GetRecoveryTicks(SchedulingCap(move) - tellTicks, chainCount);
                }
            }

            if (!continueChain)
            {
                chainCount = 0;
                selectionDelay = Math.Max(selectionDelay, 1);
            }

            move = CasterMove.None;
            elapsed = 0;
            healMarks.Clear();
            EnterPhase(AttackPhase.Idle, 0);
            NPC.netUpdate = true;
        }

        public override void OnStagger(NPC npc)
        {
            base.OnStagger(npc);
            move = CasterMove.None;
            pendingMove = CasterMove.None;
            elapsed = 0;
            chainCount = 0;
            selectionDelay = 30;
            healMarks.Clear();
            if (Authority)
            {
                NPC.netUpdate = true;
            }
        }

        public override void OnHitByItem(Player player, Item item, NPC.HitInfo hit, int damageDone)
        {
            if (Neutral)
            {
                tsorcRevampAIs.EvasiveOnHit(NPC, true);
            }
        }

        public override void OnHitByProjectile(Projectile projectile, NPC.HitInfo hit, int damageDone)
        {
            if (Neutral)
            {
                tsorcRevampAIs.EvasiveOnHit(NPC, projectile.DamageType == DamageClass.Melee);
            }
        }

        private Color ActionColor => move switch
        {
            CasterMove.FireFlask => Color.OrangeRed,
            CasterMove.StormFlask => Color.Cyan,
            CasterMove.SuppressionFlask => Color.CornflowerBlue,
            CasterMove.Spirit => Color.MediumPurple,
            _ => Color.YellowGreen
        };

        private void DrawActionDust()
        {
            if (elapsed == tellTicks)
            {
                SoundStyle sound = SoundID.Item20;
                if (Throwing)
                {
                    sound = SoundID.Item1;
                }

                SoundEngine.PlaySound(sound, NPC.Center);
            }

            if (elapsed >= 0 && elapsed <= tellTicks)
            {
                Vector2 origin = StaffTip;
                if (Throwing)
                {
                    origin = PuppetHandPosition;
                }

                Vector2 offset = Main.rand.NextVector2CircularEdge(12f, 12f);
                int dustType = move switch
                {
                    CasterMove.FireFlask => DustID.Torch,
                    CasterMove.StormFlask or CasterMove.SuppressionFlask => DustID.BlueCrystalShard,
                    CasterMove.Spirit => DustID.ShadowbeamStaff,
                    CasterMove.SelfHeal or CasterMove.AllyHeal => DustID.TintableDustLighted,
                    _ => DustID.CursedTorch
                };
                Dust dust = Dust.NewDustPerfect(origin + offset, dustType, -offset * 0.12f, 120, ActionColor, 0.8f);
                dust.noGravity = true;
                if (move == CasterMove.Storm)
                {
                    float radius = tellTicks - elapsed;
                    UsefulFunctions.DustRing(origin, radius, Kind == CasterKind.Abysswalker ? DustID.BlueCrystalShard : DustID.CursedTorch, 12, 4);
                }
                else if (move == CasterMove.StormFlask)
                {
                    float angle = elapsed * 0.2f;
                    Vector2 ringOffset = angle.ToRotationVector2() * 14f;
                    Dust ring = Dust.NewDustPerfect(origin + ringOffset, DustID.BlueCrystalShard, Vector2.Zero, 100, Color.Cyan, 0.75f);
                    ring.noGravity = true;
                }

                foreach (HealMark mark in healMarks)
                {
                    NPC ally = Main.npc[mark.Index];
                    if (ally.active && Main.rand.NextBool(3))
                    {
                        Dust mote = Dust.NewDustDirect(ally.position, ally.width, ally.height, DustID.MagicMirror, 0f, -1f, 120, Color.Teal, 0.8f);
                        mote.noGravity = true;
                    }
                }
            }
        }

        protected override bool DrawSpecialHeldWeapon(ref PlayerDrawSet drawInfo)
        {
            if (!Throwing || elapsed > tellTicks)
            {
                // The staff belongs solely to the independent rear slot.
                return true;
            }

            Main.instance.LoadItem(ItemID.FlaskofNanites);
            Main.instance.LoadItem(ItemID.FlaskofVenom);
            Texture2D texture = TextureAssets.Item[ItemID.FlaskofNanites].Value;
            if (move == CasterMove.VenomFlask)
            {
                texture = TextureAssets.Item[ItemID.FlaskofVenom].Value;
            }
            else if (move == CasterMove.FireFlask)
            {
                texture = ModContent.Request<Texture2D>(UsefulFunctions.RefactorableFilepath(typeof(Content.Projectiles.Ranged.FireFlaskProj))).Value;
            }

            drawInfo.DrawDataCache.Add(new DrawData(texture, PuppetHandPosition - Main.screenPosition, null,
                Color.White, ThrowPose(elapsed, tellTicks) * facing, texture.Size() * 0.5f, NPC.scale,
                SpriteEffects.None, 0));
            return true;
        }

        public override void PostDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            base.PostDraw(spriteBatch, screenPos, drawColor);
            if (Neutral || elapsed > tellTicks)
            {
                return;
            }

            // MagicPixel is 1x1000: every draw below samples one texel, or a null source rect paints 1000px-tall blocks.
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            Rectangle onePixel = new Rectangle(0, 0, 1, 1);
            Vector2 origin = StaffTip;
            if (Throwing && launchVelocity.LengthSquared() > 0f)
            {
                origin = PuppetFrontHandPositionAt(-0.1f);
                Vector2 point = origin;
                Vector2 velocity = launchVelocity;
                float gravity = DworcFlask.GravityFor(move);
                for (int i = 0; i < 90; i++)
                {
                    velocity.Y += gravity;
                    point += velocity;
                    if (i % 3 == 0)
                    {
                        spriteBatch.Draw(pixel, point - screenPos, onePixel, ActionColor * 0.35f, 0f, Vector2.Zero, new Vector2(2f), SpriteEffects.None, 0f);
                    }

                    if (Collision.SolidCollision(point - Vector2.One * 3f, 6, 6))
                    {
                        break;
                    }
                }
            }
            else if (move == CasterMove.Strike)
            {
                spriteBatch.Draw(pixel, origin - screenPos, onePixel, ActionColor * 0.35f, aim.ToRotation(), Vector2.Zero, new Vector2(480f, 1f), SpriteEffects.None, 0f);
            }

            float scale = MathHelper.Lerp(0.25f, 1f, MathHelper.Clamp((elapsed - tellTicks + 20) / 20f, 0f, 1f));
            if (!Throwing && !Utility)
            {
                int projectileType = ModContent.ProjectileType<EnemySpellGreatPoisonStrikeBall>();
                if (Kind == CasterKind.Abysswalker)
                {
                    projectileType = ModContent.ProjectileType<EnemySpellAbyssPoisonStrikeBall>();
                }

                Main.instance.LoadProjectile(projectileType);
                Texture2D preview = TextureAssets.Projectile[projectileType].Value;
                if (move == CasterMove.Spirit)
                {
                    preview = ModContent.Request<Texture2D>(UsefulFunctions.RefactorableFilepath(typeof(ShamanSpirit))).Value;
                    Rectangle frame = new Rectangle(0, 0, 52, 42);
                    SpriteEffects effects = SpriteEffects.None;
                    if (NPC.direction == 1)
                    {
                        effects = SpriteEffects.FlipHorizontally;
                    }

                    spriteBatch.Draw(preview, origin - screenPos, frame, Color.White * 0.65f, 0f, frame.Size() * 0.5f, scale, effects, 0f);
                }
                else
                {
                    spriteBatch.Draw(preview, origin - screenPos, null, ActionColor * 0.65f, Main.GlobalTimeWrappedHourly, preview.Size() * 0.5f, scale, SpriteEffects.None, 0f);
                }
            }
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            base.SendExtraAI(writer);
            writer.Write(SpawnToken);
            writer.Write((byte)move);
            writer.Write((byte)pendingMove);
            writer.Write(elapsed);
            writer.Write(tellTicks);
            writer.Write(tailTicks);
            writer.Write(recoveryTicks);
            writer.Write(targetIndex);
            writer.Write(facing);
            writer.Write(sequence);
            writer.Write(releasedSequence);
            writer.Write(selectionDelay);
            writer.Write(sinceRelease);
            writer.Write(chainCount);
            writer.Write(chainMaximum);
            writer.Write(chainVariance);
            writer.Write(aimLocked);
            writer.Write(utilityUsed);
            writer.Write(lichSlain);
            writer.WriteVector2(aim);
            writer.WriteVector2(landing);
            writer.WriteVector2(launchVelocity);
            for (int i = 0; i < cooldowns.Length; i++)
            {
                writer.Write(cooldowns[i]);
            }

            writer.Write((short)healMarks.Count);
            foreach (HealMark mark in healMarks)
            {
                writer.Write(mark.Index);
                writer.Write(mark.Token);
            }
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            base.ReceiveExtraAI(reader);
            SpawnToken = reader.ReadInt32();
            move = (CasterMove)reader.ReadByte();
            pendingMove = (CasterMove)reader.ReadByte();
            elapsed = reader.ReadInt32();
            tellTicks = reader.ReadInt32();
            tailTicks = reader.ReadInt32();
            recoveryTicks = reader.ReadInt32();
            targetIndex = reader.ReadInt32();
            facing = reader.ReadInt32();
            sequence = reader.ReadInt32();
            releasedSequence = reader.ReadInt32();
            selectionDelay = reader.ReadInt32();
            sinceRelease = reader.ReadInt32();
            chainCount = reader.ReadInt32();
            chainMaximum = reader.ReadInt32();
            chainVariance = reader.ReadSingle();
            aimLocked = reader.ReadBoolean();
            utilityUsed = reader.ReadBoolean();
            lichSlain = reader.ReadBoolean();
            aim = reader.ReadVector2();
            landing = reader.ReadVector2();
            launchVelocity = reader.ReadVector2();
            for (int i = 0; i < cooldowns.Length; i++)
            {
                cooldowns[i] = reader.ReadInt32();
            }

            healMarks.Clear();
            int count = reader.ReadInt16();
            for (int i = 0; i < count; i++)
            {
                healMarks.Add(new HealMark(reader.ReadInt32(), reader.ReadInt32()));
            }
        }
    }
}

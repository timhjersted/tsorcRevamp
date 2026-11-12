using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;
using tsorcRevamp.Content.Items.Armor.Enemy.BasiliskSet;
using tsorcRevamp.Content.Projectiles.Enemy.Basilisk;
using tsorcRevamp.NPCs.Puppets;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.NPCs.Enemies.Basilisk
{
    // Three content identities share one attack clock. Projectiles retain their own lifecycles,
    // especially the tongue's indefinite attachment, direct drain and damage-to-release contract.
    public abstract class BasiliskPuppet : PuppetNPC
    {
        protected enum FamilyKind : byte
        {
            Walker,
            Shifter,
            Hunter
        }

        private enum Move : byte
        {
            None,
            Bite,
            Bound,
            Spit,
            Lob,
            Disrupter,
            Tongue,
            Breath,
            Desperate
        }

        private enum Stage : byte
        {
            None,
            Tell,
            Action,
            Recovery
        }

        public const float ClawRaisedAngle = -2f;
        public const float ClawFinishAngle = 1.2f;
        public const float ArmRestAngle = 1.45f;
        public const int BiteLiveStart = 4;
        public const int BiteLiveEnd = 12;
        public const int BoundLiveStart = 4;
        public const int BoundLiveEnd = 14;
        public static readonly WeightedSwing BiteCurve = new WeightedSwing(6, 24, 6f);
        public static readonly WeightedSwing BoundCurve = new WeightedSwing(8, 32, 7f);

        public static int BiteWindupTicks(int tier)
        {
            if (tier == 2)
            {
                return 24;
            }

            return tier == 1 ? 26 : 30;
        }

        // Terraria applies gravity before displacement. The extra half-tick term lands the
        // discrete trajectory on its requested point, rather than using a continuous approximation.
        public static Vector2 SolveArc(Vector2 source, Vector2 destination, float gravity, float flightTicks)
        {
            float duration = Math.Max(1f, flightTicks);
            Vector2 displacement = destination - source;
            Vector2 velocity = displacement / duration;
            float gravityOffset = gravity * (duration + 1f) * 0.5f;
            velocity.Y -= gravityOffset;
            return velocity;
        }

        private Move move;
        private Stage stage;
        private int stageTimer;
        private int stageDuration;
        private int tellTicks;
        private int selectionDelay = 60;
        private int targetIndex;
        private int lockedFacing = 1;
        private int sequence;
        private int shotCount;
        private int disrupterPattern;
        private int healthBand = -1;
        private int boundFlightTicks;
        private bool verticalTargets;
        private bool aimLocked;
        private bool firstBreath = true;
        private bool landed;
        private Vector2 lockedTarget;
        private Vector2 boundVelocity;
        private Vector2 landingPosition;
        private readonly int[] cooldowns = new int[10];
        private AttackBag<Move> attacks;
        private uint breathCommitTick;
        private bool hasBreathCommit;
        private int presentationSequence = -1;
        private Stage presentationStage;

        protected abstract FamilyKind Kind { get; }
        protected override int HeadArmorItemType => ModContent.ItemType<BasiliskHeadpiece>();
        protected override int BodyArmorItemType => ModContent.ItemType<BasiliskCarapace>();
        protected override int LegsArmorItemType => ModContent.ItemType<BasiliskGreaves>();
        protected override int MeleeWeaponItemType => -1;
        protected override int RangedWeaponItemType => -1;
        protected override int MeleeDamage => 0;
        protected override int RangedDamage => 0;
        protected override bool AnnounceInvasion => false;
        protected override bool AnnounceInvaderDefeat => false;
        protected override bool DespawnsOnPartyWipe => false;
        public override bool UsesPuppetDifficultyScaling => false;
        protected override int EstusChargesMax => 0;
        protected override int CasualStrollChance => 0;
        protected override int UnblockableFinisherBlocks => 0;
        protected override bool HoldAttackSelection => true;
        protected override bool SlowDownDuringCustom => false;
        protected override Color PuppetSkinColor => Color.Black;
        protected override float RunSpeedMult => 1f;
        protected override float TopSpeed
        {
            get
            {
                if (Kind == FamilyKind.Hunter)
                {
                    return 2.6f;
                }

                if (Kind == FamilyKind.Shifter)
                {
                    return 1.8f;
                }

                return Main.hardMode ? 1.3f : 1f;
            }
        }

        protected override float Acceleration
        {
            get
            {
                if (Kind == FamilyKind.Hunter)
                {
                    return 0.10f;
                }

                return Kind == FamilyKind.Shifter ? 0.08f : 0.05f;
            }
        }
        protected override float PuppetJumpPower => 9f + (int)Kind;
        protected override float PuppetJumpBoost => 5f + (int)Kind;
        protected override bool PuppetCanDoubleJump => Kind == FamilyKind.Hunter && move != Move.Bound;
        protected override float PuppetDoubleJumpPower => 7f;
        protected override int HeadArmorDyeItemType
        {
            get
            {
                if (Kind == FamilyKind.Shifter)
                {
                    return ItemID.BlueDye;
                }

                if (Kind == FamilyKind.Hunter)
                {
                    return ItemID.PurpleDye;
                }

                return 0;
            }
        }

        protected override int BodyArmorDyeItemType => HeadArmorDyeItemType;
        protected override int LegsArmorDyeItemType => HeadArmorDyeItemType;
        protected override bool CustomAttackCommitted => stage == Stage.Action;
        protected override float? FreeFrontArmFacingAngle => ResolveArmAngle();
        protected override float? FreeBackArmFacingAngle => ResolveArmAngle();

        protected override void DoMeleeAttack()
        {
            // Biological strikes are owned by the family clock and its shared jaw/claw projectile.
        }

        protected override void DoRangedAttack()
        {
            // The attack bag dispatches mouth-fired projectiles through the Custom phase.
        }

        public int StrikeSequence => sequence;
        public bool StrikeRunning => stage == Stage.Action && (move == Move.Bite || move == Move.Bound);
        public bool IsTongueCasting => move == Move.Tongue && stage != Stage.Recovery;
        public bool OwnsTongue(int ownerSequence) => sequence != 0 && sequence == ownerSequence;
        public Vector2 MouthPosition => NPC.Center + new Vector2(NPC.direction * 20f, -12f + NPC.gfxOffY);
        public bool MeleeLive
        {
            get
            {
                if (stage != Stage.Action)
                {
                    return false;
                }

                int elapsed = stageTimer;
                if (move == Move.Bound)
                {
                    elapsed -= boundFlightTicks - 8;
                    return elapsed >= BoundLiveStart && elapsed < BoundLiveEnd;
                }

                return move == Move.Bite && elapsed >= BiteLiveStart && elapsed < BiteLiveEnd;
            }
        }

        public int MouthFrame
        {
            get
            {
                if (move == Move.None)
                {
                    return 0;
                }

                if (stage == Stage.Tell)
                {
                    return (int)MathHelper.Lerp(0f, 6f, MathHelper.Clamp(stageTimer / (float)tellTicks, 0f, 1f));
                }

                if (stage == Stage.Recovery)
                {
                    return Math.Max(0, 6 - stageTimer / 2);
                }

                if (stage == Stage.Action && (move == Move.Lob || move == Move.Disrupter))
                {
                    int interval = 30;
                    if (move == Move.Lob)
                    {
                        interval = 40;
                    }

                    int beat = stageTimer % interval;
                    if (beat < 8)
                    {
                        return Math.Max(0, 6 - beat);
                    }

                    return Math.Clamp((beat - interval + 16) * 6 / 16, 0, 6);
                }

                if (move == Move.Bite || move == Move.Bound)
                {
                    int elapsed = stageTimer;
                    if (move == Move.Bound)
                    {
                        elapsed -= boundFlightTicks - 8;
                    }

                    return Math.Clamp(6 - Math.Max(0, elapsed - 4), 0, 6);
                }

                return 6;
            }
        }

        // Used by the runtime and offline body preview: one clock supplies the overhead rake,
        // peak jaw closure and harmless decaying tail. Angles are facing-space, not weapon-space.
        public static float ClawPose(int elapsed, int windupTicks, bool bound)
        {
            if (elapsed < windupTicks)
            {
                return MathHelper.SmoothStep(ArmRestAngle, ClawRaisedAngle, MathHelper.Clamp(elapsed / (float)windupTicks, 0f, 1f));
            }

            WeightedSwing curve = BiteCurve;
            if (bound)
            {
                curve = BoundCurve;
            }

            int actionElapsed = elapsed - windupTicks;
            if (actionElapsed <= curve.TotalTicks)
            {
                return curve.Apply(ClawRaisedAngle, ClawFinishAngle, actionElapsed);
            }

            float settle = MathHelper.Clamp((actionElapsed - curve.TotalTicks) / 20f, 0f, 1f);
            return MathHelper.SmoothStep(ClawFinishAngle, ArmRestAngle, settle);
        }

        private float? ResolveArmAngle()
        {
            if (move == Move.None)
            {
                return null;
            }

            if (move == Move.Bite || move == Move.Bound)
            {
                int elapsed = stageTimer;
                if (stage == Stage.Action)
                {
                    elapsed += tellTicks;
                    if (move == Move.Bound)
                    {
                        elapsed -= boundFlightTicks - 8;
                        elapsed = Math.Max(tellTicks, elapsed);
                    }
                }
                else if (stage == Stage.Recovery)
                {
                    int actionTicks = BiteCurve.TotalTicks;
                    if (move == Move.Bound)
                    {
                        actionTicks = BoundCurve.TotalTicks;
                    }

                    elapsed += tellTicks + actionTicks;
                }

                return ClawPose(elapsed, tellTicks, move == Move.Bound);
            }

            float targetAngle = -0.5f;
            if (move == Move.Breath)
            {
                targetAngle = -1.1f;
            }

            float amount = MathHelper.Clamp(stageTimer / 15f, 0f, 1f);
            if (stage == Stage.Recovery)
            {
                return MathHelper.SmoothStep(targetAngle, ArmRestAngle, amount);
            }

            if (stage == Stage.Action)
            {
                return targetAngle;
            }

            return MathHelper.SmoothStep(ArmRestAngle, targetAngle, amount);
        }

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[Type] = 1;
        }

        public override void SetDefaults()
        {
            NPC.aiStyle = -1;
            NPC.height = 46;
            NPC.damage = 0;
            NPC.HitSound = SoundID.NPCHit20;
            NPC.DeathSound = SoundID.NPCDeath5;
            NPC.lavaImmune = true;

            tsorcRevampGlobalNPC globalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            globalNPC.NavSearchRadius = 80;
            globalNPC.KiteRangeMin = 4f;
            globalNPC.KiteRangeMax = 10f;
            globalNPC.KiteLooseness = 0.7f;
            if (Kind == FamilyKind.Walker)
            {
                globalNPC.NavSearchRadius = 70;
                globalNPC.KiteRangeMin = 6f;
                globalNPC.KiteRangeMax = 12f;
                globalNPC.CanTeleport = true;
                globalNPC.TeleportMaxCharges = 2;
            }
            else
            {
                globalNPC.PatrolMode = PatrolMode.Wander;
                globalNPC.PatrolAnchorSource = PatrolAnchorSource.GiveUpLocation;
                if (Kind == FamilyKind.Shifter)
                {
                    globalNPC.KiteRangeMin = 8f;
                    globalNPC.KiteRangeMax = 14f;
                }
            }

            if (Kind != FamilyKind.Hunter)
            {
                globalNPC.KiteLooseness = 0.5f;
            }

            EvasiveProfile.Basilisk(globalNPC);
        }

        protected override void RunMovementAI(float speedMult)
        {
            if (move == Move.None)
            {
                NPC.GetGlobalNPC<tsorcRevampGlobalNPC>().CanUseRopes = Kind != FamilyKind.Walker;
                base.RunMovementAI(1f);
            }
        }

        public override void AI()
        {
            bool authority = Main.netMode != NetmodeID.MultiplayerClient;
            tsorcRevampGlobalNPC globalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            if (authority)
            {
                for (int i = 0; i < cooldowns.Length; i++)
                {
                    cooldowns[i] = Math.Max(0, cooldowns[i] - 1);
                }

                selectionDelay = Math.Max(0, selectionDelay - 1);
                if (move == Move.None)
                {
                    NPC.TargetClosest(false);
                    if (NPC.HasValidTarget && globalNPC.StaggerTimer == 0 && selectionDelay == 0)
                    {
                        SelectMove(Main.player[NPC.target]);
                    }
                }
                else if (targetIndex >= Main.maxPlayers || !Main.player[targetIndex].active || Main.player[targetIndex].dead)
                {
                    OnStagger(NPC);
                }
            }

            // Base movement/pose/network maintenance still runs. The family's Custom phase is held
            // open explicitly; in particular, no phase duration can time out an attached tongue.
            if (move != Move.None)
            {
                PhaseTimer = 2;
                NPC.target = targetIndex;
            }

            globalNPC.CanTeleport = Kind == FamilyKind.Walker && move == Move.None;
            NPC.noGravity = move == Move.Bound && stage == Stage.Action && !landed;
            base.AI();
            DebugAttackLabel = move + "/" + stage;

            if (move != Move.None)
            {
                globalNPC.AttackTelegraphing = stage == Stage.Tell;
                globalNPC.AttackCommitted = stage == Stage.Action;
                globalNPC.SuppressPreAttackJump = true;
            }

            if (!Main.dedServ && move != Move.None)
            {
                DecorateMove();
            }
        }

        private void SelectMove(Player target)
        {
            int currentBand = 0;
            if (Kind == FamilyKind.Shifter && NPC.life < NPC.lifeMax / 2)
            {
                currentBand = 1;
            }
            else if (Kind == FamilyKind.Hunter)
            {
                if (NPC.life <= NPC.lifeMax / 5)
                {
                    currentBand = 2;
                }
                else if (NPC.life <= NPC.lifeMax * 0.4f)
                {
                    currentBand = 1;
                }
            }

            attacks ??= new AttackBag<Move>(RefillAttacks);
            if (healthBand != currentBand)
            {
                healthBand = currentBand;
                attacks.Clear();
            }

            if (!attacks.TryDraw(IsEligible, out Move chosen))
            {
                return;
            }

            move = chosen;
            targetIndex = target.whoAmI;
            lockedFacing = 1;
            if (target.Center.X < NPC.Center.X)
            {
                lockedFacing = -1;
            }

            NPC.direction = lockedFacing;
            NPC.spriteDirection = lockedFacing;
            lockedTarget = target.Center;
            aimLocked = false;
            shotCount = 0;
            landed = false;
            if (sequence == 0)
            {
                sequence = Main.rand.Next(1, 1 << 20);
            }

            sequence++;
            // Projectile.ai stores floats. Keep ownership IDs exactly representable so a newly
            // spawned jaw/tongue does not reject its NPC after a lossy int-to-float conversion.
            if (sequence >= 1 << 23)
            {
                sequence = 1;
            }
            tellTicks = TellDuration(chosen);
            disrupterPattern = Main.rand.Next(Kind == FamilyKind.Hunter ? 4 : 2);
            verticalTargets = Main.rand.NextBool();
            stage = Stage.Tell;
            stageTimer = 0;
            stageDuration = tellTicks;
            StartCustomAttack(2);
            NPC.netUpdate = true;
            RequestNetworkSnapshot();
        }

        private void RefillAttacks(List<Move> cards)
        {
            cards.Add(Move.Bite);
            cards.Add(Move.Bound);
            cards.Add(Move.Tongue);
            cards.Add(Move.Disrupter);
            cards.Add(Move.Spit);
            cards.Add(Move.Spit);
            if (Kind != FamilyKind.Walker)
            {
                cards.Add(Move.Lob);
                if ((Kind == FamilyKind.Shifter && healthBand == 0) || (Kind == FamilyKind.Hunter && healthBand < 2))
                {
                    cards.Add(Move.Breath);
                }
            }

            if (Kind == FamilyKind.Hunter && healthBand == 2)
            {
                cards.Clear();
                cards.Add(Move.Desperate);
                cards.Add(Move.Bite);
                cards.Add(Move.Tongue);
            }
        }

        private bool IsEligible(Move candidate)
        {
            if (cooldowns[(int)candidate] > 0 || !NPC.HasValidTarget)
            {
                return false;
            }

            Player target = Main.player[NPC.target];
            float distance = NPC.Distance(target.Center);
            bool lineOfSight = Collision.CanHitLine(MouthPosition, 1, 1, target.Center, 1, 1);
            switch (candidate)
            {
                case Move.Bite:
                    return distance < 72f && Math.Abs(target.Center.Y - NPC.Center.Y) < 48f;
                case Move.Bound:
                    return distance >= 120f && distance <= 360f && NPC.velocity.Y == 0f && SolveBound(target);
                case Move.Tongue:
                    return distance >= 120f && distance <= BasiliskLeechTongue.MaxLaunchRange && lineOfSight && FamilyHazardAvailable(false, target);
                case Move.Breath:
                    return distance > 20f && distance < 400f && NPC.velocity.Y == 0f && FamilyHazardAvailable(true, target);
                case Move.Disrupter:
                    return distance > 160f && distance < 640f && lineOfSight;
                case Move.Lob:
                    return distance >= 160f && distance <= 480f && lineOfSight;
                default:
                    return distance >= 72f && distance <= 640f && lineOfSight;
            }
        }

        // Reserve tongue casts as well as attachments so two same-tick decisions cannot both grab.
        // Breath reservations last 30t from commitment and are scoped to the same target player.
        private bool FamilyHazardAvailable(bool breath, Player target)
        {
            if (!breath)
            {
                for (int i = 0; i < Main.maxProjectiles; i++)
                {
                    Projectile projectile = Main.projectile[i];
                    if (projectile.active && projectile.ModProjectile is BasiliskLeechTongue tongue && tongue.AttachedVictim == target.whoAmI)
                    {
                        return false;
                    }
                }
            }

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC other = Main.npc[i];
                if (!other.active || other.whoAmI == NPC.whoAmI || other.ModNPC is not BasiliskPuppet family)
                {
                    continue;
                }

                if (breath)
                {
                    if (family.targetIndex == target.whoAmI && family.hasBreathCommit && unchecked((uint)Main.GameUpdateCount - family.breathCommitTick) < 30u)
                    {
                        return false;
                    }
                }
                else if (family.IsTongueCasting && (family.targetIndex == target.whoAmI || other.Distance(target.Center) < 640f))
                {
                    return false;
                }
            }

            return true;
        }

        private int TellDuration(Move candidate)
        {
            switch (candidate)
            {
                case Move.Bite:
                    return BiteWindupTicks((int)Kind);
                case Move.Bound:
                    return 40;
                case Move.Tongue:
                    return 45;
                case Move.Disrupter:
                    return 50 - (int)Kind * 5;
                case Move.Lob:
                    return Kind == FamilyKind.Shifter ? 48 : 40;
                case Move.Breath:
                    return Kind == FamilyKind.Shifter ? 120 : 95;
                case Move.Desperate:
                    return 30;
                default:
                    return 40 - (int)Kind * 4;
            }
        }

        protected override void DoCustomTick(int ticksRemaining)
        {
            if (move == Move.None)
            {
                return;
            }

            bool authority = Main.netMode != NetmodeID.MultiplayerClient;
            Player target = Main.player[targetIndex];
            if (stage == Stage.Tell)
            {
                NPC.velocity.X *= 0.8f;
                int aimLead = 16;
                if (move == Move.Spit)
                {
                    aimLead = 12;
                }

                if (!aimLocked && authority)
                {
                    lockedTarget = target.Center;
                    lockedFacing = target.Center.X >= NPC.Center.X ? 1 : -1;
                    if (stageTimer >= tellTicks - aimLead)
                    {
                        aimLocked = true;
                        NPC.netUpdate = true;
                        RequestNetworkSnapshot();
                    }
                }

                NPC.direction = lockedFacing;
                NPC.spriteDirection = lockedFacing;
                if (authority && stageTimer >= stageDuration)
                {
                    BeginAction(target);
                    return;
                }
            }
            else if (stage == Stage.Action)
            {
                NPC.direction = lockedFacing;
                NPC.spriteDirection = lockedFacing;
                RunAction(target, authority);
            }
            else if (stage == Stage.Recovery)
            {
                NPC.velocity.X *= 0.8f;
                if (authority && stageTimer >= stageDuration)
                {
                    move = Move.None;
                    stage = Stage.None;
                    selectionDelay = 1;
                    EnterPhase(AttackPhase.Idle, 0);
                    NPC.netUpdate = true;
                    RequestNetworkSnapshot();
                    return;
                }
            }

            stageTimer++;
        }

        private void BeginAction(Player target)
        {
            if ((move == Move.Tongue || move == Move.Breath) && !FamilyHazardAvailable(move == Move.Breath, target))
            {
                BeginRecovery();
                return;
            }

            if (move == Move.Bound && !SolveBound(target))
            {
                BeginRecovery();
                return;
            }

            stage = Stage.Action;
            stageTimer = 0;
            stageDuration = 12;
            if (move == Move.Bite || move == Move.Bound)
            {
                int expertDamage = 46;
                if (Kind == FamilyKind.Walker && Main.hardMode)
                {
                    expertDamage = 66;
                }
                else if (Kind == FamilyKind.Shifter)
                {
                    expertDamage = 60;
                }
                else if (Kind == FamilyKind.Hunter)
                {
                    expertDamage = 96;
                }

                stageDuration = BiteCurve.TotalTicks;
                if (move == Move.Bound)
                {
                    NPC.velocity = boundVelocity;
                    NPC.noGravity = true;
                    stageDuration = boundFlightTicks - 8 + BoundCurve.TotalTicks;
                    expertDamage = 52;
                    if (Kind == FamilyKind.Shifter)
                    {
                        expertDamage = 70;
                    }
                    else if (Kind == FamilyKind.Hunter)
                    {
                        expertDamage = 160;
                    }
                }

                Projectile.NewProjectile(NPC.GetSource_FromAI(), MouthPosition, Vector2.Zero, ModContent.ProjectileType<BasiliskBiteHitbox>(), EnemyDamage.Projectile(expertDamage), 3f, Main.myPlayer, NPC.whoAmI, sequence);
            }
            else if (move == Move.Tongue)
            {
                Vector2 mouth = MouthPosition;
                // Lob arc whose FALLING half passes through the player, then keeps going (the overshoot is just
                // the tongue's own follow-through). Solving for a point 7.5 tiles past the player instead put the
                // tip ~80-100px above a standing player's head as it crossed them: only the drawn tendril touched
                // them, and the tendril can't grab. Aims at the player's live position at launch, not lockedTarget
                // (locked ~16 ticks earlier), matching the pre-puppet tongue.
                Vector2 offset = target.Center - mouth;
                float distanceFactor = MathHelper.Clamp(offset.Length() / BasiliskLeechTongue.MaxLaunchRange, 0f, 1f);
                float rise = -MathHelper.Lerp(10f, 14f, distanceFactor);

                // Gravity is added before the move each tick, so after t ticks y = (rise + g/2) * t + g/2 * t^2.
                // The later root of y = offset.Y is the descending pass.
                float adjustedRise = rise + BasiliskLeechTongue.Gravity * 0.5f;
                float discriminant = adjustedRise * adjustedRise + 2f * BasiliskLeechTongue.Gravity * offset.Y;
                float flightTicks = Math.Max(1f, (-adjustedRise + MathF.Sqrt(Math.Max(0f, discriminant))) / BasiliskLeechTongue.Gravity);
                Vector2 velocity = new Vector2(offset.X / flightTicks, rise);
                // The spawn value is also the tongue's direct drain: 10 / 20 / 30 life,
                // independent of the normal projectile hit's Expert scaling.
                int damage = EnemyDamage.Projectile(40 + (int)Kind * 40);
                Projectile.NewProjectile(NPC.GetSource_FromAI(), mouth, velocity, ModContent.ProjectileType<BasiliskLeechTongue>(), damage, 0f, Main.myPlayer, NPC.whoAmI, 0f, sequence);
            }
            else if (move == Move.Breath)
            {
                stageDuration = 60;
                if (Kind == FamilyKind.Hunter)
                {
                    stageDuration = firstBreath ? 120 : 160;
                }

                firstBreath = false;
                breathCommitTick = (uint)Main.GameUpdateCount;
                hasBreathCommit = true;
            }
            else if (move == Move.Lob)
            {
                stageDuration = 92;
            }
            else if (move == Move.Disrupter && Kind != FamilyKind.Walker)
            {
                stageDuration = 72;
            }
            else if (move == Move.Desperate)
            {
                stageDuration = 52;
            }

            NPC.netUpdate = true;
            RequestNetworkSnapshot();
        }

        private void RunAction(Player target, bool authority)
        {
            if (move == Move.Bound)
            {
                bool ceilingHit = NPC.collideY && stageTimer < boundFlightTicks / 2;
                if (authority && stageTimer > 2 && (NPC.collideX || ceilingHit))
                {
                    BeginRecovery();
                    return;
                }

                if (!landed && stageTimer > 2 && NPC.collideY && NPC.velocity.Y == 0f)
                {
                    landed = true;
                    if (authority)
                    {
                        NPC.netUpdate = true;
                        RequestNetworkSnapshot();
                    }
                }

                if (!landed)
                {
                    NPC.velocity.Y += 0.4f;
                }
                else
                {
                    NPC.velocity.X *= 0.8f;
                }
            }
            else if (move == Move.Bite && MeleeLive)
            {
                NPC.velocity.X = lockedFacing * 3f;
            }
            else
            {
                NPC.velocity.X *= 0.75f;
            }

            if (!authority)
            {
                return;
            }

            if (move == Move.Tongue)
            {
                // Projectile owns attachment time and release. Never compare stageTimer to a
                // fixed channel duration: the victim must damage the owner or suffer the flee burst.
                bool activeTongue = false;
                for (int i = 0; i < Main.maxProjectiles; i++)
                {
                    Projectile projectile = Main.projectile[i];
                    if (projectile.active && projectile.ModProjectile is BasiliskLeechTongue && (int)projectile.ai[0] == NPC.whoAmI)
                    {
                        activeTongue = true;
                        break;
                    }
                }

                if (!activeTongue && stageTimer > 2)
                {
                    BeginRecovery();
                }

                return;
            }

            if (move == Move.Breath)
            {
                NPC.velocity.X = 0f;
                int remaining = stageDuration - stageTimer;
                if (Kind == FamilyKind.Hunter && stageTimer > 0 && (remaining == 120 || remaining == 60))
                {
                    BreathTeleport(target);
                }

                if (stageTimer < stageDuration)
                {
                    Vector2 velocity;
                    int projectileType;
                    int damage;
                    if (Kind == FamilyKind.Shifter)
                    {
                        float facing = lockedFacing > 0 ? 0f : MathHelper.Pi;
                        float offset = MathHelper.WrapAngle((target.Center - MouthPosition).ToRotation() - facing);
                        float angle = facing + MathHelper.Clamp(offset, -1.3962634f, 1.3962634f);
                        velocity = angle.ToRotationVector2() * 9f + Main.rand.NextVector2Circular(1.5f, 1.5f);
                        projectileType = ModContent.ProjectileType<BasiliskShifterBreath>();
                        damage = EnemyDamage.Projectile(52);
                    }
                    else
                    {
                        velocity = new Vector2(Main.rand.Next(-2, 2), Main.rand.Next(-2, 2));
                        projectileType = ModContent.ProjectileType<BasiliskHunterBreath>();
                        damage = EnemyDamage.Projectile(80);
                    }

                    Projectile.NewProjectile(NPC.GetSource_FromAI(), MouthPosition, velocity, projectileType, damage, 0f, Main.myPlayer);
                }
            }
            else if (move != Move.Bite && move != Move.Bound)
            {
                int interval = 1;
                int count = 1;
                if (move == Move.Lob)
                {
                    interval = 40;
                    count = 3;
                    if (stageTimer == 20 || stageTimer == 60)
                    {
                        lockedTarget = target.Center;
                        NPC.netUpdate = true;
                    }
                }
                else if (move == Move.Disrupter && Kind != FamilyKind.Walker)
                {
                    interval = 30;
                    count = 3;
                }
                else if (move == Move.Desperate)
                {
                    interval = 10;
                    count = 5;
                }

                if (shotCount < count && stageTimer >= shotCount * interval)
                {
                    FireShot(shotCount);
                    shotCount++;
                    NPC.netUpdate = true;
                }
            }

            if (stageTimer >= stageDuration)
            {
                if (move == Move.Bound && !landed && stageTimer < 150)
                {
                    return;
                }

                BeginRecovery();
            }
        }

        private void FireShot(int shotIndex)
        {
            int acidExpert = 40;
            int disrupterExpert = 60;
            if (Kind == FamilyKind.Walker && Main.hardMode)
            {
                acidExpert = 72;
                disrupterExpert = 92;
            }
            else if (Kind == FamilyKind.Shifter)
            {
                acidExpert = 72;
                disrupterExpert = 72;
            }
            else if (Kind == FamilyKind.Hunter)
            {
                acidExpert = 180;
                disrupterExpert = 172;
            }

            Vector2 mouth = MouthPosition;
            Vector2 velocity = (lockedTarget - mouth).SafeNormalize(new Vector2(lockedFacing, 0f)) * (8f + (int)Kind);
            int projectileType = ModContent.ProjectileType<BasiliskAcidSpit>();
            int damage = EnemyDamage.Projectile(acidExpert);
            float mode = 0f;
            Vector2 target = lockedTarget;
            if (move == Move.Lob)
            {
                target.X += lockedFacing * 64f;
                const float flightTicks = 64f;
                velocity = SolveArc(mouth, target, 0.25f, flightTicks);
                Vector2 sample = mouth;
                Vector2 step = velocity;
                for (int i = 0; i < 64; i++)
                {
                    step.Y += 0.25f;
                    sample += step;
                    if (Collision.SolidCollision(sample - new Vector2(12f), 24, 24))
                    {
                        return;
                    }
                }

                mode = 1f;
            }
            else if (move == Move.Desperate)
            {
                float flightTicks = MathHelper.Clamp(Vector2.Distance(mouth, lockedTarget) / 8f, 20f, 80f);
                velocity = SolveArc(mouth, lockedTarget, 0.25f, flightTicks);
                velocity += Main.rand.NextVector2Circular(6f, 2f);
                damage = EnemyDamage.Projectile(200);
                mode = 2f;
            }
            else if (move == Move.Disrupter)
            {
                projectileType = ModContent.ProjectileType<BasiliskHypnoticDisrupter>();
                damage = EnemyDamage.Projectile(disrupterExpert);
                target = DisrupterTarget(shotIndex);
                if (Kind == FamilyKind.Walker)
                {
                    target = lockedTarget;
                    velocity = UsefulFunctions.BallisticTrajectory(mouth, target, 3f);
                    // Walker's legacy orb follows a player; Shifter/Hunter use fixed points.
                    // Aim-lock changes its launch tell, not its deliberately unusual homing mode.
                    Projectile.NewProjectile(NPC.GetSource_FromAI(), mouth, velocity, projectileType, damage, 0f, Main.myPlayer, targetIndex);
                    return;
                }
                else
                {
                    float angle = lockedFacing > 0 ? -MathHelper.PiOver4 : -3f * MathHelper.PiOver4;
                    if (disrupterPattern == 0)
                    {
                        angle += (shotIndex - 1) * MathHelper.ToRadians(10f);
                    }
                    else if (disrupterPattern == 1)
                    {
                        angle = lockedFacing > 0 ? MathHelper.ToRadians(60f) : MathHelper.ToRadians(120f);
                    }
                    else if (disrupterPattern == 2)
                    {
                        angle = (shotIndex - 1) * MathHelper.ToRadians(20f);
                        if (lockedFacing < 0)
                        {
                            angle += MathHelper.Pi;
                        }

                    }
                    else
                    {
                        angle = -MathHelper.PiOver2 + (shotIndex - 1) * MathHelper.ToRadians(18f);
                    }

                    velocity = angle.ToRotationVector2() * 4f;
                }

                Projectile.NewProjectile(NPC.GetSource_FromAI(), mouth, velocity, projectileType, damage, 0f, Main.myPlayer, target.X, -target.Y);
                return;
            }

            Projectile.NewProjectile(NPC.GetSource_FromAI(), mouth, velocity, projectileType, damage, 0f, Main.myPlayer, mode);
        }

        private Vector2 DisrupterTarget(int shotIndex)
        {
            if (Kind == FamilyKind.Walker || disrupterPattern == 0)
            {
                return lockedTarget;
            }

            float offset = (shotIndex - 1) * 60f;
            if (disrupterPattern == 1)
            {
                if (verticalTargets)
                {
                    return lockedTarget + new Vector2(0f, offset);
                }

                return lockedTarget + new Vector2(offset, 0f);
            }

            if (disrupterPattern == 2)
            {
                return lockedTarget + new Vector2(offset * lockedFacing, -Math.Abs(offset));
            }

            return lockedTarget + new Vector2(offset * 1.5f, -80f);
        }

        private void BeginRecovery()
        {
            int downtime = 60;
            if (Kind == FamilyKind.Shifter)
            {
                downtime = 120;
            }
            else if (Kind == FamilyKind.Hunter)
            {
                downtime = 90;
            }

            cooldowns[(int)move] = downtime;
            if (move == Move.Tongue)
            {
                cooldowns[(int)move] = 360 + (int)Kind * 70;
            }
            else if (move == Move.Breath)
            {
                cooldowns[(int)move] = Kind == FamilyKind.Shifter ? 360 : 540;
            }

            stage = Stage.Recovery;
            stageTimer = 0;
            stageDuration = downtime;
            if (move == Move.Bite)
            {
                stageDuration = 30;
            }
            else if (move == Move.Bound)
            {
                stageDuration = 36;
            }

            NPC.noGravity = false;
            NPC.netUpdate = true;
            RequestNetworkSnapshot();
        }

        private bool SolveBound(Player target)
        {
            Vector2 destination = lockedTarget;
            if (move != Move.Bound)
            {
                destination = target.Center;
            }

            int facing = destination.X >= NPC.Center.X ? 1 : -1;
            int tileX = (int)((destination.X + facing * 32f) / 16f);
            int startY = (int)((destination.Y + target.height * 0.5f) / 16f) - 4;
            for (int y = startY; y < startY + 12; y++)
            {
                if (!WorldGen.InWorld(tileX, y, 10) || !UsefulFunctions.IsTileReallySolid(tileX, y))
                {
                    continue;
                }

                Vector2 landing = new Vector2(tileX * 16f - NPC.width * 0.5f, y * 16f - NPC.height);
                if (Collision.SolidCollision(landing, NPC.width, NPC.height))
                {
                    continue;
                }

                const int flightTicks = 40;
                Vector2 velocity = SolveArc(NPC.position, landing, 0.4f, flightTicks);
                if (Math.Abs(velocity.X) > 10.5f)
                {
                    return false;
                }

                Vector2 sample = NPC.position;
                Vector2 step = velocity;
                for (int i = 0; i < flightTicks - 1; i++)
                {
                    step.Y += 0.4f;
                    sample += step;
                    if (Collision.SolidCollision(sample, NPC.width, NPC.height))
                    {
                        return false;
                    }
                }

                landingPosition = landing;
                boundVelocity = velocity;
                boundFlightTicks = flightTicks;
                return true;
            }

            return false;
        }

        private void BreathTeleport(Player target)
        {
            int side = NPC.Center.X < target.Center.X ? 1 : -1;
            for (int distanceIndex = 0; distanceIndex < 3; distanceIndex++)
            {
                int tileX = (int)((target.Center.X + side * (120f + distanceIndex * 40f)) / 16f);
                int startY = (int)(target.Center.Y / 16f) - 11;
                for (int y = startY; y < startY + 19; y++)
                {
                    if (!WorldGen.InWorld(tileX, y, 10) || !UsefulFunctions.IsTileReallySolid(tileX, y))
                    {
                        continue;
                    }

                    Vector2 position = new Vector2(tileX * 16f - NPC.width * 0.5f, y * 16f - NPC.height);
                    if (Collision.SolidCollision(position, NPC.width, NPC.height) || !Collision.CanHit(position, NPC.width, NPC.height, target.position, target.width, target.height))
                    {
                        continue;
                    }

                    NPC.position = position;
                    NPC.velocity = Vector2.Zero;
                    lockedFacing = target.Center.X >= NPC.Center.X ? 1 : -1;
                    NPC.direction = lockedFacing;
                    NPC.spriteDirection = lockedFacing;
                    NPC.netUpdate = true;
                    RequestNetworkSnapshot();
                    return;
                }
            }

            tsorcRevampAIs.TeleportImmediately(NPC, 15, true);
            lockedFacing = NPC.direction;
        }

        // Vanilla's full-stretch front/back hand solves, evaluated without the draw-only Player.
        // Collision follows the same empty-hand angles on dedicated servers and rendering clients.
        public Vector2 ClawPosition(bool backHand, int timerOffset = 0)
        {
            float angle = ResolveArmAngle() ?? ArmRestAngle;
            if (stage == Stage.Action && (move == Move.Bite || move == Move.Bound))
            {
                int elapsed = stageTimer + timerOffset;
                if (move == Move.Bound)
                {
                    elapsed -= boundFlightTicks - 8;
                }

                angle = ClawPose(tellTicks + Math.Max(0, elapsed), tellTicks, move == Move.Bound);
            }

            float rotation = (angle - MathHelper.PiOver2) * lockedFacing + MathHelper.PiOver2;
            Vector2 hand;
            if (backHand)
            {
                hand = rotation.ToRotationVector2() * new Vector2(10f, 12f) + new Vector2(6f * lockedFacing, -2f);
            }
            else
            {
                hand = rotation.ToRotationVector2() * 10f + new Vector2(-4f * lockedFacing, -2f) + new Vector2(0f, 3f * lockedFacing).RotatedBy(rotation);
            }

            return NPC.Center + new Vector2(0f, NPC.gfxOffY) + hand;
        }

        public bool StrikeCollides(Rectangle target)
        {
            if (!MeleeLive)
            {
                return false;
            }

            float collisionPoint = 0f;
            Vector2 mouth = MouthPosition;
            Vector2 previousMouth = mouth - NPC.velocity;
            if (Collision.CheckAABBvLineCollision(target.TopLeft(), target.Size(), previousMouth, mouth, 18f, ref collisionPoint))
            {
                return true;
            }

            for (int i = 0; i < 2; i++)
            {
                Vector2 hand = ClawPosition(i == 1);
                Vector2 previousHand = ClawPosition(i == 1, -1) - NPC.velocity;
                if (Collision.CheckAABBvLineCollision(target.TopLeft(), target.Size(), previousHand, hand, 10f, ref collisionPoint))
                {
                    return true;
                }
            }

            return false;
        }

        public void ApplyBiteEffects(Player target)
        {
            int seconds = 2;
            if (Kind == FamilyKind.Shifter)
            {
                seconds = 3;
            }
            else if (Kind == FamilyKind.Hunter)
            {
                seconds = 5;
            }

            target.AddBuff(BuffID.Poisoned, seconds * 60, false);
            PoisonBuildup.Apply(target, 40);
        }

        protected override void OnHitRegistered(Vector2 sourceCenter, int damageDone, bool meleeHit)
        {
            BasiliskLeechTongue.NotifyOwnerHit(NPC, damageDone);
            if (move == Move.None && Main.netMode != NetmodeID.MultiplayerClient)
            {
                tsorcRevampAIs.EvasiveOnHit(NPC, meleeHit);
            }
        }

        public override void OnStagger(NPC npc)
        {
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                for (int i = 0; i < Main.maxProjectiles; i++)
                {
                    Projectile projectile = Main.projectile[i];
                    if (projectile.active && projectile.ModProjectile is BasiliskLeechTongue tongue && (int)projectile.ai[0] == NPC.whoAmI)
                    {
                        tongue.StartRetract();
                    }
                }
            }

            base.OnStagger(npc);
            move = Move.None;
            stage = Stage.None;
            stageTimer = 0;
            selectionDelay = 60;
            NPC.noGravity = false;
            NPC.netUpdate = true;
        }

        private Color MoveColor()
        {
            if (move == Move.Disrupter)
            {
                return Color.Purple;
            }

            if (move == Move.Tongue)
            {
                return Color.HotPink;
            }

            if (move == Move.Breath)
            {
                return PowerfulCurseBuildup.PowerfulCurseColor;
            }

            return Color.GreenYellow;
        }

        private void DecorateMove()
        {
            if (presentationSequence != sequence || presentationStage != stage)
            {
                presentationSequence = sequence;
                presentationStage = stage;
                if (stage == Stage.Action)
                {
                    SoundStyle sound = SoundID.Item20 with { Volume = 0.25f };
                    if (move == Move.Disrupter)
                    {
                        sound = SoundID.Item24 with { Volume = 0.6f, Pitch = -0.5f };
                    }
                    else if (move == Move.Tongue || move == Move.Bite || move == Move.Bound)
                    {
                        sound = SoundID.NPCHit8 with { Volume = 0.45f, Pitch = 0.35f };
                    }

                    SoundEngine.PlaySound(sound, MouthPosition);
                }
            }

            if (stage == Stage.Tell || (stage == Stage.Action && move == Move.Breath))
            {
                Color color = MoveColor();
                Lighting.AddLight(MouthPosition, color.ToVector3() * 0.6f);
                if (Main.rand.NextBool(3))
                {
                    int dustType = DustID.CursedTorch;
                    if (move == Move.Disrupter)
                    {
                        dustType = DustID.Shadowflame;
                    }
                    else if (move == Move.Tongue || move == Move.Breath)
                    {
                        dustType = DustID.PinkTorch;
                    }

                    Dust mote = Dust.NewDustPerfect(MouthPosition + Main.rand.NextVector2Circular(7f, 5f), dustType, Main.rand.NextVector2Circular(0.6f, 0.6f), 80, color, 1f);
                    mote.noGravity = true;
                }
            }

            if (MeleeLive)
            {
                for (int i = 0; i < 2; i++)
                {
                    Dust.NewDustPerfect(ClawPosition(i == 1), DustID.GemEmerald, NPC.velocity * 0.1f, 80, Color.GreenYellow, 0.7f);
                }
            }
        }

        public override void PostDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            base.PostDraw(spriteBatch, screenPos, drawColor);
            bool volleyCharge = stage == Stage.Action && shotCount < 3 && (move == Move.Lob || (move == Move.Disrupter && Kind != FamilyKind.Walker));
            if ((stage != Stage.Tell && !volleyCharge) || move == Move.Bite || move == Move.Bound || move == Move.Breath)
            {
                return;
            }

            Vector2 mouth = MouthPosition - screenPos;
            Color color = MoveColor();
            float progress = MathHelper.Clamp(stageTimer / (float)tellTicks, 0f, 1f);
            if (volleyCharge)
            {
                int interval = 30;
                if (move == Move.Lob)
                {
                    interval = 40;
                }

                progress = (stageTimer % interval) / (float)interval;
            }
            int projectileType = ModContent.ProjectileType<BasiliskAcidSpit>();
            if (move == Move.Disrupter)
            {
                projectileType = ModContent.ProjectileType<BasiliskHypnoticDisrupter>();
            }
            else if (move == Move.Tongue)
            {
                projectileType = ModContent.ProjectileType<BasiliskLeechTongue>();
            }

            Texture2D texture = TextureAssets.Projectile[projectileType].Value;
            int frameCount = Math.Max(1, Main.projFrames[projectileType]);
            int frameHeight = texture.Height / frameCount;
            int frame = (stageTimer / 6) % frameCount;
            Rectangle source = new Rectangle(0, frame * frameHeight, texture.Width, frameHeight);
            float scale = MathHelper.Lerp(0.25f, 0.85f, progress);
            if (move == Move.Lob)
            {
                scale *= 10f / 24f;
            }

            spriteBatch.Draw(texture, mouth, source, color * 0.7f, 0f, source.Size() * 0.5f, scale, SpriteEffects.None, 0f);

            if (move != Move.Tongue)
            {
                Vector2 aim = lockedTarget;
                if (move == Move.Disrupter)
                {
                    aim = DisrupterTarget(shotCount);
                }

                Vector2 offset = aim - MouthPosition;
                float length = Math.Min(offset.Length(), 640f);
                // MagicPixel is 1x1000: a null source rect drew this as a length x 1000px block. Sample one texel.
                Rectangle onePixel = new Rectangle(0, 0, 1, 1);
                spriteBatch.Draw(TextureAssets.MagicPixel.Value, mouth, onePixel, color * 0.2f, offset.ToRotation(), Vector2.Zero, new Vector2(length, 1f), SpriteEffects.None, 0f);
            }
        }

        public override void OnKill()
        {
            if (Main.dedServ)
            {
                return;
            }

            for (int i = 0; i < 7; i++)
            {
                int piece = 1 + i % 3;
                Vector2 velocity = Main.rand.NextVector2Circular(6f, 6f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, velocity, Mod.Find<ModGore>("Parasite Zombie Gore " + piece).Type, 1.1f);
            }

            for (int i = 0; i < 10; i++)
            {
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, Main.rand.NextVector2Circular(6f, 6f), Mod.Find<ModGore>("Blood Splat").Type, 1.1f);
            }
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            base.SendExtraAI(writer);
            writer.Write((byte)move);
            writer.Write((byte)stage);
            writer.Write(stageTimer);
            writer.Write(stageDuration);
            writer.Write(tellTicks);
            writer.Write(targetIndex);
            writer.Write(lockedFacing);
            writer.Write(sequence);
            writer.Write(shotCount);
            writer.Write(disrupterPattern);
            writer.Write(verticalTargets);
            writer.Write(aimLocked);
            writer.Write(firstBreath);
            writer.Write(landed);
            writer.Write(boundFlightTicks);
            writer.Write(lockedTarget.X);
            writer.Write(lockedTarget.Y);
            writer.Write(boundVelocity.X);
            writer.Write(boundVelocity.Y);
            writer.Write(landingPosition.X);
            writer.Write(landingPosition.Y);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            base.ReceiveExtraAI(reader);
            move = (Move)reader.ReadByte();
            stage = (Stage)reader.ReadByte();
            stageTimer = reader.ReadInt32();
            stageDuration = reader.ReadInt32();
            tellTicks = reader.ReadInt32();
            targetIndex = reader.ReadInt32();
            lockedFacing = reader.ReadInt32();
            sequence = reader.ReadInt32();
            shotCount = reader.ReadInt32();
            disrupterPattern = reader.ReadInt32();
            verticalTargets = reader.ReadBoolean();
            aimLocked = reader.ReadBoolean();
            firstBreath = reader.ReadBoolean();
            landed = reader.ReadBoolean();
            boundFlightTicks = reader.ReadInt32();
            lockedTarget = new Vector2(reader.ReadSingle(), reader.ReadSingle());
            boundVelocity = new Vector2(reader.ReadSingle(), reader.ReadSingle());
            landingPosition = new Vector2(reader.ReadSingle(), reader.ReadSingle());
        }
    }
}

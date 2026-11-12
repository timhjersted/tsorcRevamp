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
using tsorcRevamp.Content.Items.Armor.Enemy.FireLurkerSet;
using tsorcRevamp.Content.Projectiles.Enemy;
using tsorcRevamp.Content.Projectiles.Enemy.Lurker;
using tsorcRevamp.NPCs.Invaders;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.NPCs.Enemies
{
    // Fire Lurker and Abyss Lurker share one unarmed pyromancer-brute body. The crown, hurled orb and
    // meteor trap keep their legacy periods; Ember Rake and Leap Swipe replace the old body contact.
    // Every decision is made on the server; clients pose from the synced move/stage/gesture fields.
    public abstract class LurkerPuppet : PuppetNPC, IHitReactor
    {
        private enum LurkerMove : byte
        {
            None,
            Hurl,
            Burst,
            Rake,
            Leap
        }

        private enum Stage : byte
        {
            None,
            Tell,
            Action,
            Recovery
        }

        private enum CrownGesture : byte
        {
            None,
            Form,
            Launch
        }

        // Legacy periods and ranges (ticks / px), unchanged from the pre-puppet FighterAI versions.
        public const int MaxCrownOrbs = 3;
        public const int CrownPeriod = 3 * 60;
        public const int ReleasedOrbTicks = 3 * 60;
        public const int TrapPeriod = 10 * 60;
        public const float CombatRange = 50 * 16f;
        public const float CrownHeight = 56f;
        // Must match the orbs' own FormingTicks (FireLurkerFlameOrb / AbyssLurkerFlameOrb).
        public const int CrownFormingTicks = 20;

        // Minimum gap between two attack releases. The crown's launch-on-hit is exempt.
        public const int MinReleaseGap = 30;

        // Hurled orb: the orb sits in the front hand for 60t before release; the last HurlTellTicks are the cock-back tell.
        public const int HurlPrepTicks = 60;
        public const int HurlThrowTicks = 6;
        public const int HurlRecoveryTicks = 20;
        public const float HurlHoldAngle = 0.7f;
        public const float HurlCockedAngle = -1.6f;
        public const float HurlFollowAngle = 0.4f;

        // Fire Burst: the projectile's own 40t waist tell is the NPC's tell; ignition flings the arms out.
        public const int BurstTellTicks = 40;
        public const int BurstFlingTicks = 10;
        public const int BurstRecoveryTicks = 20;
        public const int BurstCooldownTicks = 300;
        public const float BurstRange = 6 * 16f;
        public const float BurstGatherAngle = 2.3f;
        public const float BurstFlingAngle = -0.7f;

        // Shared claw rake: both arms from raised-behind-head to low-forward on one weighted curve.
        public const float ClawRaisedAngle = -2f;
        public const float ClawFinishAngle = 1.2f;
        public const float ArmRestAngle = 1.45f;
        public const int ClawRaiseTicks = 20;
        public const int ClawLiveStart = 5;
        public const int ClawLiveEnd = 18;
        public static readonly WeightedSwing ClawCurve = new WeightedSwing(6, 24, 6f);

        // Ember Rake.
        public const int RakeTellTicks = 32;
        public const int RakeRecoveryTicks = 30;
        public const int RakeCooldownTicks = 240;
        public const float RakeEdgeGap = 48f;
        public const float RakeVerticalRange = 40f;
        public const float RakeStepSpeed = 2f;
        public const int RakeStepTicks = 12;

        // Leap Swipe: the rebuilt FighterAI high-arc pounce.
        public const int LeapTellTicks = 28;
        public const int LeapAimLockTick = 20;
        public const int LeapCooldownTicks = 300;
        public const float LeapMaxRange = 400f;
        // Land this far SHORT of the target's centre. The strike box faces forward, so a landing past the
        // target left a standing player behind it (0-3t of live overlap in the preview); short keeps them in it.
        public const float LeapLandShort = 28f;
        public const float LeapGravity = 0.4f;
        public const float LeapMaxHorizontalSpeed = 10.5f;
        public const float LeapSwipeRange = 56f;
        public const int LeapLandingLiveTicks = 6;
        public const int LeapRecoveryTicks = 36;
        public const int LeapWhiffRecoveryTicks = 42;
        public const int LeapFailsafeTicks = 150;

        // Strike box in front of the chest, shared by both melee moves.
        public const float StrikeBoxReach = 40f;
        public const float StrikeBoxHeight = 44f;

        // Arm gestures that play over neutral walking.
        public const int CrownFormGestureTicks = 20;
        public const int CrownLaunchGestureTicks = 15;
        public const int TrapGestureTicks = 60;

        private LurkerMove move;
        private Stage stage;
        private int stageTimer;
        private int stageDuration;
        private int tellTicks;
        private int targetIndex;
        private int lockedFacing = 1;
        private int sequence;
        private Vector2 lockedTarget;
        private Vector2 leapVelocity;
        private bool swipeStarted;
        private int swipeTimer;
        private bool landed;
        private int landedTimer;
        private bool hurlPrep;
        private int hurlPrepTimer;
        private CrownGesture crownGesture;
        private int crownGestureTimer;
        private int trapGestureTimer = TrapGestureTicks;
        private Vector2 trapSite;

        // Server-only scheduling state. Clients never decide, so none of this is synced.
        private int crownTimer;
        private int hurlTimer;
        private int trapTimer;
        private int burstCooldown;
        private int rakeCooldown;
        private int leapCooldown;
        private int ticksSinceRelease = MinReleaseGap;
        private int selectionDelay = 30;
        private int burstProjectileIndex = -1;
        private AttackBag<LurkerMove> closeMoves;

        protected abstract int OrbType { get; }
        protected abstract int MeteorType { get; }
        protected abstract int LostSoulDamage { get; }
        protected abstract int HurlPeriod { get; }
        protected abstract float HurlSpeed { get; }
        protected abstract int HurlTellTicks { get; }
        protected abstract bool CrownAutoLaunch { get; }
        protected abstract bool HasFireBurst { get; }
        protected abstract float BaseTopSpeed { get; }
        protected abstract float BaseAcceleration { get; }
        protected abstract float EnrageFraction { get; }
        protected abstract float EnrageTopSpeed { get; }
        protected abstract int MeleeExpertDamage { get; }
        protected abstract int EmberDust { get; }
        protected abstract Color EmberColor { get; }
        protected abstract int GroanFrequency { get; }
        protected virtual int FireBurstExpertDamage => 0;
        protected virtual float SpeedScale => 1f;
        protected virtual float AccelScale => 1f;

        public abstract void ApplyMeleeDebuffs(Player target);

        protected override int HeadArmorItemType => ModContent.ItemType<FireLurkerHeadpiece>();
        protected override int BodyArmorItemType => ModContent.ItemType<FireLurkerHide>();
        protected override int LegsArmorItemType => ModContent.ItemType<FireLurkerLegs>();
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
        protected override bool EvadesProjectiles => true;
        protected override Color PuppetSkinColor => new Color(170, 40, 25);
        protected override float RunSpeedMult => 1f;

        // Below the enrage fraction the legacy FighterAI swapped to a fixed top speed and doubled acceleration.
        private bool Enraged => NPC.life < NPC.lifeMax * EnrageFraction;

        protected override float TopSpeed
        {
            get
            {
                float speed = BaseTopSpeed;
                if (Enraged)
                {
                    speed = EnrageTopSpeed;
                }

                return speed * SpeedScale;
            }
        }

        protected override float Acceleration
        {
            get
            {
                float acceleration = BaseAcceleration * AccelScale;
                if (Enraged)
                {
                    acceleration *= 2f;
                }

                return acceleration;
            }
        }

        protected override float? FreeFrontArmFacingAngle => ResolveArmAngle(false);
        protected override float? FreeBackArmFacingAngle => ResolveArmAngle(true);

        protected override void DoMeleeAttack()
        {
            // Both melee moves are owned by the Lurker clock and its LurkerClawStrike.
        }

        protected override void DoRangedAttack()
        {
            // Orbs and traps are spawned by the Lurker clock.
        }

        public int StrikeSequence => sequence;
        public bool StrikeRunning => stage == Stage.Action && (move == LurkerMove.Rake || move == LurkerMove.Leap);

        // Rake: live on its curve ticks. Leap: live from the swipe's ramp through landing + 6t
        // (the whole moving tail stays dangerous; the overlap time decides rollability).
        public bool MeleeLive
        {
            get
            {
                if (stage != Stage.Action)
                {
                    return false;
                }

                if (move == LurkerMove.Rake)
                {
                    return stageTimer >= ClawLiveStart && stageTimer < ClawLiveEnd;
                }

                if (move != LurkerMove.Leap || !swipeStarted || swipeTimer < ClawLiveStart)
                {
                    return false;
                }

                if (!landed)
                {
                    return true;
                }

                return landedTimer <= LeapLandingLiveTicks || swipeTimer < ClawLiveEnd;
            }
        }

        public Rectangle StrikeBox
        {
            get
            {
                int width = (int)(NPC.width * 0.5f + StrikeBoxReach);
                int left = (int)NPC.Center.X;
                if (lockedFacing < 0)
                {
                    left -= width;
                }

                int top = (int)(NPC.Center.Y - StrikeBoxHeight * 0.6f);
                return new Rectangle(left, top, width, (int)StrikeBoxHeight);
            }
        }

        public bool StrikeCollides(Rectangle target)
        {
            if (!MeleeLive)
            {
                return false;
            }

            return StrikeBox.Intersects(target);
        }

        // Vanilla's full-stretch empty-hand solve (PuppetNPC.GetUnscaledHandPosition), evaluated
        // without the draw-only Player so a dedicated server spawns orbs from the same palm.
        public Vector2 HandPosition(float facingAngle)
        {
            float rotation = (facingAngle - MathHelper.PiOver2) * NPC.direction + MathHelper.PiOver2;
            Vector2 wrist = rotation.ToRotationVector2() * 10f;
            wrist += new Vector2(-4f * NPC.direction, -2f);
            wrist += new Vector2(0f, 3f * NPC.direction).RotatedBy(rotation);
            return NPC.Center + new Vector2(0f, NPC.gfxOffY) + wrist;
        }

        // Claw pose on the shared curve. elapsed counts from the start of the raise.
        public static float ClawPose(int elapsedInSwing)
        {
            if (elapsedInSwing <= ClawCurve.TotalTicks)
            {
                return ClawCurve.Apply(ClawRaisedAngle, ClawFinishAngle, elapsedInSwing);
            }

            return ClawFinishAngle;
        }

        private float? ResolveArmAngle(bool backArm)
        {
            if (move == LurkerMove.Rake || move == LurkerMove.Leap)
            {
                return ResolveClawAngle();
            }

            if (move == LurkerMove.Burst)
            {
                return ResolveBurstAngle();
            }

            if (move == LurkerMove.Hurl && !backArm)
            {
                return ResolveHurlAngle();
            }

            // Neutral gestures. The crown launch thrusts both arms forward; the crown form lifts the back arm.
            if (crownGesture == CrownGesture.Launch)
            {
                float launchProgress = MathHelper.Clamp(crownGestureTimer / 4f, 0f, 1f);
                return MathHelper.SmoothStep(ArmRestAngle, 0f, launchProgress);
            }

            if (backArm)
            {
                if (crownGesture == CrownGesture.Form)
                {
                    return GestureAngle(-2f, crownGestureTimer, CrownFormGestureTicks, 6);
                }

                return null;
            }

            if (hurlPrep)
            {
                return HurlHoldAngle;
            }

            if (trapGestureTimer < TrapGestureTicks)
            {
                Vector2 toSite = trapSite - NPC.Center;
                float pointAngle = (float)Math.Atan2(toSite.Y, toSite.X * NPC.direction);
                pointAngle = MathHelper.Clamp(pointAngle, -2.2f, 1.2f);
                return GestureAngle(pointAngle, trapGestureTimer, TrapGestureTicks, 10);
            }

            return null;
        }

        // Ease from the hanging rest pose into a held angle, then back out at the end of the gesture.
        private static float GestureAngle(float heldAngle, int timer, int duration, int easeTicks)
        {
            float easeIn = MathHelper.Clamp(timer / (float)easeTicks, 0f, 1f);
            float easeOut = MathHelper.Clamp((duration - timer) / (float)easeTicks, 0f, 1f);
            float amount = Math.Min(easeIn, easeOut);
            return MathHelper.SmoothStep(ArmRestAngle, heldAngle, amount);
        }

        private float ResolveClawAngle()
        {
            if (stage == Stage.Tell)
            {
                float raise = MathHelper.Clamp(stageTimer / (float)ClawRaiseTicks, 0f, 1f);
                return MathHelper.SmoothStep(ArmRestAngle, ClawRaisedAngle, raise);
            }

            if (stage == Stage.Action)
            {
                if (move == LurkerMove.Rake)
                {
                    return ClawPose(stageTimer);
                }

                if (!swipeStarted)
                {
                    return ClawRaisedAngle;
                }

                return ClawPose(swipeTimer);
            }

            // Recovery lowers whatever the action ended on: the finish pose after a swipe, the raised pose after a whiff.
            float startAngle = ClawFinishAngle;
            if (move == LurkerMove.Leap && !swipeStarted)
            {
                startAngle = ClawRaisedAngle;
            }

            float settle = MathHelper.Clamp(stageTimer / 12f, 0f, 1f);
            return MathHelper.SmoothStep(startAngle, ArmRestAngle, settle);
        }

        private float ResolveBurstAngle()
        {
            if (stage == Stage.Tell)
            {
                float gather = MathHelper.Clamp(stageTimer / 20f, 0f, 1f);
                return MathHelper.SmoothStep(ArmRestAngle, BurstGatherAngle, gather);
            }

            if (stage == Stage.Action)
            {
                float fling = MathHelper.Clamp(stageTimer / 4f, 0f, 1f);
                return MathHelper.Lerp(BurstGatherAngle, BurstFlingAngle, fling);
            }

            float settle = MathHelper.Clamp(stageTimer / (float)BurstRecoveryTicks, 0f, 1f);
            return MathHelper.SmoothStep(BurstFlingAngle, ArmRestAngle, settle);
        }

        private float ResolveHurlAngle()
        {
            if (stage == Stage.Tell)
            {
                float cock = MathHelper.Clamp(stageTimer / (float)Math.Max(1, tellTicks), 0f, 1f);
                return MathHelper.SmoothStep(HurlHoldAngle, HurlCockedAngle, cock);
            }

            if (stage == Stage.Action)
            {
                float snap = MathHelper.Clamp(stageTimer / 3f, 0f, 1f);
                return MathHelper.Lerp(HurlCockedAngle, HurlFollowAngle, snap);
            }

            float settle = MathHelper.Clamp(stageTimer / (float)HurlRecoveryTicks, 0f, 1f);
            return MathHelper.SmoothStep(HurlFollowAngle, ArmRestAngle, settle);
        }

        // Shared SetDefaults body for both variants; the concrete classes set stats first.
        protected void ApplyLurkerDefaults()
        {
            NPC.aiStyle = -1;
            NPC.lavaImmune = true;
            tsorcRevampGlobalNPC globalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            globalNPC.NavSearchRadius = 60;
            globalNPC.TeleportReacquireMaxRange = CombatRange;
            // The legacy FighterAI `canTeleport: true` argument meant unlimited Normal blink; SF4 puppets
            // never run that migration, so request it directly.
            globalNPC.CanTeleport = true;
        }

        protected override void RunMovementAI(float speedMult)
        {
            if (move == LurkerMove.None)
            {
                base.RunMovementAI(1f);
            }
        }

        public override bool CanHitPlayer(Player target, ref int cooldownSlot)
        {
            // No body contact: Ember Rake and Leap Swipe carry the old contact damage. NPC.damage stays set
            // because the meteor trap and the scripted-event overrides still read it.
            return false;
        }

        public override void AI()
        {
            bool authority = Main.netMode != NetmodeID.MultiplayerClient;
            tsorcRevampGlobalNPC globalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();

            if (authority)
            {
                RunServerSchedule(globalNPC);
            }

            if (move != LurkerMove.None)
            {
                PhaseTimer = 2;
                NPC.target = targetIndex;
            }

            globalNPC.CanTeleport = move == LurkerMove.None;
            NPC.noGravity = move == LurkerMove.Leap && stage == Stage.Action && !landed;
            base.AI();
            DebugAttackLabel = move + "/" + stage;

            if (move != LurkerMove.None)
            {
                globalNPC.AttackTelegraphing = stage == Stage.Tell;
                globalNPC.AttackCommitted = stage == Stage.Action;
                globalNPC.SuppressPreAttackJump = true;
            }

            AdvanceGestures();

            if (!Main.dedServ)
            {
                DecorateLurker();
            }
        }

        private void AdvanceGestures()
        {
            if (crownGesture != CrownGesture.None)
            {
                crownGestureTimer++;
                int gestureLength = CrownFormGestureTicks;
                if (crownGesture == CrownGesture.Launch)
                {
                    gestureLength = CrownLaunchGestureTicks;
                }

                if (crownGestureTimer >= gestureLength)
                {
                    crownGesture = CrownGesture.None;
                    crownGestureTimer = 0;
                }
            }

            if (trapGestureTimer < TrapGestureTicks)
            {
                trapGestureTimer++;
            }

            if (hurlPrep)
            {
                hurlPrepTimer++;
            }
        }

        // Server: the legacy timers, the 30t release gap and neutral selection, in that order.
        private void RunServerSchedule(tsorcRevampGlobalNPC globalNPC)
        {
            burstCooldown = Math.Max(0, burstCooldown - 1);
            rakeCooldown = Math.Max(0, rakeCooldown - 1);
            leapCooldown = Math.Max(0, leapCooldown - 1);
            selectionDelay = Math.Max(0, selectionDelay - 1);
            if (ticksSinceRelease < 10000)
            {
                ticksSinceRelease++;
            }

            if (move == LurkerMove.None)
            {
                int previousTarget = NPC.target;
                NPC.TargetClosest(true);
                if (NPC.target != previousTarget)
                {
                    NPC.netUpdate = true;
                }
            }
            else if (targetIndex >= Main.maxPlayers || !Main.player[targetIndex].active || Main.player[targetIndex].dead)
            {
                CancelMove();
                return;
            }

            if (!HasCombatTarget())
            {
                // The legacy crown and trap timers reset when the target was lost; the hurl timer only paused.
                crownTimer = 0;
                trapTimer = 0;
                return;
            }

            bool gapClear = ticksSinceRelease >= MinReleaseGap;
            bool busy = stage == Stage.Tell || stage == Stage.Action;

            crownTimer++;
            if (crownTimer >= CrownPeriod)
            {
                int slot = GetOpenCrownSlot();
                if (slot >= 0)
                {
                    crownTimer = 0;
                    SpawnCrownOrb(slot);
                }
                else if (!CrownAutoLaunch)
                {
                    // Abyss Lurker: a full crown waits until the Lurker is hit.
                    crownTimer = 0;
                }
                else if (gapClear && !busy)
                {
                    crownTimer = 0;
                    if (LaunchCrown())
                    {
                        ticksSinceRelease = 0;
                    }
                }
                else
                {
                    crownTimer = CrownPeriod;
                }
            }

            trapTimer++;
            if (trapTimer >= TrapPeriod)
            {
                if (gapClear && !busy)
                {
                    trapTimer = 0;
                    SpawnTrap();
                    ticksSinceRelease = 0;
                }
                else
                {
                    trapTimer = TrapPeriod;
                }
            }

            int hurlStart = HurlPeriod - HurlTellTicks;
            hurlTimer = Math.Min(hurlTimer + 1, hurlStart);
            if (!hurlPrep && hurlTimer >= HurlPeriod - HurlPrepTicks)
            {
                hurlPrep = true;
                hurlPrepTimer = HurlPrepTicks - (HurlPeriod - hurlTimer);
                NPC.netUpdate = true;
            }

            if (move != LurkerMove.None || globalNPC.StaggerTimer > 0 || selectionDelay > 0)
            {
                return;
            }

            if (globalNPC.InEvasion || globalNPC.TeleportCountdown > 0 || globalNPC.TeleportAppearanceTimer > 0)
            {
                return;
            }

            Player target = Main.player[NPC.target];
            if (hurlTimer >= hurlStart && ticksSinceRelease + HurlTellTicks >= MinReleaseGap)
            {
                BeginMove(LurkerMove.Hurl, target, HurlTellTicks);
                return;
            }

            closeMoves ??= new AttackBag<LurkerMove>(RefillCloseMoves);
            if (closeMoves.TryDraw(candidate => CloseMoveEligible(candidate, target), out LurkerMove chosen))
            {
                int chosenTell = RakeTellTicks;
                if (chosen == LurkerMove.Burst)
                {
                    chosenTell = BurstTellTicks;
                }

                BeginMove(chosen, target, chosenTell);
                return;
            }

            if (LeapEligible(globalNPC, target))
            {
                BeginMove(LurkerMove.Leap, target, LeapTellTicks);
            }
        }

        private void RefillCloseMoves(List<LurkerMove> cards)
        {
            cards.Add(LurkerMove.Rake);
            cards.Add(LurkerMove.Rake);
            if (HasFireBurst)
            {
                cards.Add(LurkerMove.Burst);
            }
        }

        private bool CloseMoveEligible(LurkerMove candidate, Player target)
        {
            if (candidate == LurkerMove.Rake)
            {
                float edgeGap = Math.Abs(target.Center.X - NPC.Center.X) - (NPC.width + target.width) * 0.5f;
                float verticalGap = Math.Abs(target.Center.Y - NPC.Center.Y);
                return rakeCooldown == 0 && edgeGap <= RakeEdgeGap && verticalGap <= RakeVerticalRange;
            }

            if (candidate == LurkerMove.Burst)
            {
                return HasFireBurst && burstCooldown == 0 && NPC.Distance(target.Center) < BurstRange;
            }

            return false;
        }

        // Same trigger as FighterAI's HighArcPounce (beyond 200px / sqrt(Aggression), a 1-in-(180 / Aggression)
        // roll per tick, 300t cooldown), capped at 400px so the solved arc stays a leap rather than a launch.
        private bool LeapEligible(tsorcRevampGlobalNPC globalNPC, Player target)
        {
            if (leapCooldown > 0 || NPC.velocity.Y != 0f)
            {
                return false;
            }

            float aggression = Math.Max(0.01f, globalNPC.Aggression);
            float distanceSquared = NPC.DistanceSQ(target.Center);
            if (distanceSquared <= 40000f / aggression || distanceSquared > LeapMaxRange * LeapMaxRange)
            {
                return false;
            }

            if (!Collision.CanHitLine(NPC.Center, 1, 1, target.Center, 1, 1))
            {
                return false;
            }

            if (Main.rand.NextFloat() * 180f >= aggression)
            {
                return false;
            }

            return SolveLeap(target.Center);
        }

        private bool HasCombatTarget()
        {
            if (NPC.target < 0 || NPC.target >= Main.maxPlayers)
            {
                return false;
            }

            Player target = Main.player[NPC.target];
            return target.active && !target.dead && NPC.Distance(target.Center) <= CombatRange;
        }

        private void BeginMove(LurkerMove chosen, Player target, int tell)
        {
            move = chosen;
            stage = Stage.Tell;
            stageTimer = 0;
            stageDuration = tell;
            tellTicks = tell;
            targetIndex = target.whoAmI;
            lockedTarget = target.Center;
            lockedFacing = 1;
            if (target.Center.X < NPC.Center.X)
            {
                lockedFacing = -1;
            }

            NPC.direction = lockedFacing;
            NPC.spriteDirection = lockedFacing;
            swipeStarted = false;
            swipeTimer = 0;
            landed = false;
            landedTimer = 0;
            if (sequence == 0)
            {
                sequence = Main.rand.Next(1, 1 << 20);
            }

            sequence++;
            // Projectile.ai stores floats; keep the strike's owner token exactly representable.
            if (sequence >= 1 << 23)
            {
                sequence = 1;
            }

            if (chosen == LurkerMove.Burst)
            {
                // The burst projectile's own 40t waist-tracking tell is the NPC's tell. Its owner index is read
                // here, at the spawn call: the legacy AddAttack captured NPC.whoAmI inside SetDefaults, before
                // vanilla NewNPC assigns it, so the tell always followed whatever NPC sat in slot 0.
                int damage = EnemyDamage.Projectile(FireBurstExpertDamage);
                burstProjectileIndex = Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center + new Vector2(0f, 6f), Vector2.Zero, ModContent.ProjectileType<FireLurkerFireBurst>(), damage, 0f, Main.myPlayer, NPC.whoAmI);
            }

            StartCustomAttack(2);
            NPC.netUpdate = true;
            RequestNetworkSnapshot();
        }

        protected override void DoCustomTick(int ticksRemaining)
        {
            if (move == LurkerMove.None)
            {
                return;
            }

            bool authority = Main.netMode != NetmodeID.MultiplayerClient;
            Player target = Main.player[targetIndex];
            NPC.direction = lockedFacing;
            NPC.spriteDirection = lockedFacing;

            if (stage == Stage.Tell)
            {
                NPC.velocity.X *= 0.8f;
                if (authority && move == LurkerMove.Leap && stageTimer == LeapAimLockTick)
                {
                    lockedTarget = target.Center;
                    NPC.netUpdate = true;
                }

                if (authority && stageTimer >= stageDuration)
                {
                    BeginAction(target);
                    return;
                }
            }
            else if (stage == Stage.Action)
            {
                RunAction(target, authority);
                if (move == LurkerMove.None)
                {
                    return;
                }
            }
            else if (stage == Stage.Recovery)
            {
                NPC.velocity.X *= 0.8f;
                if (authority && stageTimer >= stageDuration)
                {
                    move = LurkerMove.None;
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
            stage = Stage.Action;
            stageTimer = 0;
            ticksSinceRelease = 0;

            if (move == LurkerMove.Hurl)
            {
                stageDuration = HurlThrowTicks;
                Vector2 hand = HandPosition(HurlCockedAngle);
                Vector2 velocity = UsefulFunctions.Aim(hand, target.Center, HurlSpeed);
                // Damage is read at release so the Path of Pain override (lostSoulDamage = 16) still applies.
                Projectile.NewProjectile(NPC.GetSource_FromAI(), hand, velocity, OrbType, LostSoulDamage, 0f, Main.myPlayer, -1, -ReleasedOrbTicks);
                hurlTimer = 0;
                hurlPrep = false;
                hurlPrepTimer = 0;
            }
            else if (move == LurkerMove.Burst)
            {
                stageDuration = BurstFlingTicks;
                burstCooldown = BurstCooldownTicks;
                burstProjectileIndex = -1;
            }
            else if (move == LurkerMove.Rake)
            {
                stageDuration = ClawCurve.TotalTicks;
                rakeCooldown = RakeCooldownTicks;
                SpawnStrike();
            }
            else if (move == LurkerMove.Leap)
            {
                leapCooldown = LeapCooldownTicks;
                if (!SolveLeap(lockedTarget))
                {
                    BeginRecovery();
                    return;
                }

                stageDuration = LeapFailsafeTicks;
                // This tick's move happens after this call with no RunLeap gravity step, so add the first
                // tick's gravity here: the solver's path applies gravity before every move, including the first.
                NPC.velocity = leapVelocity + new Vector2(0f, LeapGravity);
                NPC.noGravity = true;
                SpawnStrike();
            }

            NPC.netUpdate = true;
            RequestNetworkSnapshot();
        }

        private void SpawnStrike()
        {
            int meleeExpert = MeleeExpertDamage;
            // Scripted events (Path of Ambition / Path of Pain) override the old contact damage; the melee
            // moves inherit that number as their Expert hit, exactly as the body contact used to.
            tsorcRevampGlobalNPC globalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            ScriptedEvent owner = globalNPC.ScriptedEventOwner;
            int eventIndex = globalNPC.ScriptedEventIndex;
            if (owner != null && owner.eventNPCs != null && eventIndex >= 0 && eventIndex < owner.eventNPCs.Count)
            {
                int? customDamage = owner.eventNPCs[eventIndex].customDamage;
                if (customDamage.HasValue)
                {
                    meleeExpert = customDamage.Value;
                }
            }

            int damage = EnemyDamage.Projectile(meleeExpert);
            Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<LurkerClawStrike>(), damage, 3f, Main.myPlayer, NPC.whoAmI, sequence);
        }

        private void RunAction(Player target, bool authority)
        {
            if (move == LurkerMove.Rake)
            {
                // Lean into the rake: 2 px/t for the first 12 ticks (~24px), then plant.
                if (stageTimer < RakeStepTicks)
                {
                    NPC.velocity.X = lockedFacing * RakeStepSpeed;
                }
                else
                {
                    NPC.velocity.X *= 0.75f;
                }
            }
            else if (move == LurkerMove.Leap)
            {
                RunLeap(target, authority);
                if (move == LurkerMove.None || stage != Stage.Action)
                {
                    return;
                }
            }
            else
            {
                NPC.velocity.X *= 0.8f;
            }

            if (!authority)
            {
                return;
            }

            if (move == LurkerMove.Leap)
            {
                bool swipeDone = !swipeStarted || swipeTimer >= ClawCurve.TotalTicks;
                if ((landed && swipeDone) || stageTimer >= stageDuration)
                {
                    BeginRecovery();
                }

                return;
            }

            if (stageTimer >= stageDuration)
            {
                BeginRecovery();
            }
        }

        private void RunLeap(Player target, bool authority)
        {
            // A ceiling bump zeroes vertical velocity on the way up; the pre-collision velocity says which way it was going.
            bool ceilingHit = !landed && NPC.collideY && NPC.oldVelocity.Y < 0f;
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
                NPC.velocity.Y += LeapGravity;
            }
            else
            {
                landedTimer++;
                NPC.velocity.X *= 0.8f;
            }

            if (swipeStarted)
            {
                swipeTimer++;
                return;
            }

            // Past the apex, swipe only once the target is actually within reach of the front hand.
            bool pastApex = NPC.velocity.Y >= 0f;
            if (authority && !landed && pastApex)
            {
                Vector2 hand = HandPosition(ClawRaisedAngle);
                float reach = LeapSwipeRange + target.width * 0.5f;
                if (Vector2.Distance(hand, target.Center) <= reach || NPC.Distance(target.Center) <= reach)
                {
                    swipeStarted = true;
                    swipeTimer = 0;
                    NPC.netUpdate = true;
                    RequestNetworkSnapshot();
                }
            }
        }

        private void BeginRecovery()
        {
            stage = Stage.Recovery;
            stageTimer = 0;
            stageDuration = RakeRecoveryTicks;
            if (move == LurkerMove.Hurl)
            {
                stageDuration = HurlRecoveryTicks;
            }
            else if (move == LurkerMove.Burst)
            {
                stageDuration = BurstRecoveryTicks;
            }
            else if (move == LurkerMove.Leap)
            {
                stageDuration = LeapRecoveryTicks;
                if (!swipeStarted)
                {
                    stageDuration = LeapWhiffRecoveryTicks;
                }
            }

            NPC.noGravity = false;
            NPC.netUpdate = true;
            RequestNetworkSnapshot();
        }

        // Launch velocity that lands the discrete (gravity-then-move) path exactly on `to` after a whole number of
        // ticks: 1 tick per 7px of horizontal travel, clamped to 30-46t. Public so the offline preview proves reach.
        public static Vector2 SolveLeapArc(Vector2 from, Vector2 to, out int flightTicks)
        {
            float horizontal = Math.Abs(to.X - from.X);
            flightTicks = (int)Math.Round(MathHelper.Clamp(horizontal / 7f, 30f, 46f));
            Vector2 displacement = to - from;
            Vector2 velocity = displacement / flightTicks;
            velocity.Y -= LeapGravity * (flightTicks + 1f) * 0.5f;
            return velocity;
        }

        // Find solid ground just past the target and a clear arc onto it.
        private bool SolveLeap(Vector2 destination)
        {
            int facing = 1;
            if (destination.X < NPC.Center.X)
            {
                facing = -1;
            }

            float landingX = destination.X - facing * LeapLandShort;
            int tileX = (int)(landingX / 16f);
            int startY = (int)(destination.Y / 16f) - 3;
            for (int y = startY; y < startY + 12; y++)
            {
                if (!WorldGen.InWorld(tileX, y, 10) || !UsefulFunctions.IsTileReallySolid(tileX, y))
                {
                    continue;
                }

                Vector2 landing = new Vector2(tileX * 16f + 8f - NPC.width * 0.5f, y * 16f - NPC.height);
                if (Collision.SolidCollision(landing, NPC.width, NPC.height))
                {
                    continue;
                }

                Vector2 velocity = SolveLeapArc(NPC.position, landing, out int flightTicks);
                if (Math.Abs(velocity.X) > LeapMaxHorizontalSpeed)
                {
                    return false;
                }

                Vector2 sample = NPC.position;
                Vector2 step = velocity;
                for (int i = 0; i < flightTicks - 1; i++)
                {
                    step.Y += LeapGravity;
                    sample += step;
                    if (Collision.SolidCollision(sample, NPC.width, NPC.height))
                    {
                        return false;
                    }
                }

                leapVelocity = velocity;
                return true;
            }

            return false;
        }

        private int GetOpenCrownSlot()
        {
            bool[] occupiedSlots = new bool[MaxCrownOrbs];
            int activeOrbs = 0;
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile projectile = Main.projectile[i];
                if (!projectile.active || projectile.type != OrbType || (int)projectile.ai[0] != NPC.whoAmI)
                {
                    continue;
                }

                activeOrbs++;
                int slot = (int)projectile.ai[1];
                if (projectile.ai[1] >= 0 && slot < MaxCrownOrbs)
                {
                    occupiedSlots[slot] = true;
                }
            }

            if (activeOrbs >= MaxCrownOrbs)
            {
                return -1;
            }

            for (int i = 0; i < MaxCrownOrbs; i++)
            {
                if (!occupiedSlots[i])
                {
                    return i;
                }
            }

            return -1;
        }

        private void SpawnCrownOrb(int slot)
        {
            Vector2 crownCenter = NPC.Top + new Vector2(0f, -CrownHeight);
            Projectile.NewProjectile(NPC.GetSource_FromAI(), crownCenter, Vector2.Zero, OrbType, LostSoulDamage, 0f, Main.myPlayer, NPC.whoAmI, slot);
            crownGesture = CrownGesture.Form;
            crownGestureTimer = 0;
            NPC.netUpdate = true;
        }

        // Launch every orb held in this Lurker's crown at the nearest valid player. Returns whether any launched.
        private bool LaunchCrown()
        {
            NPC.TargetClosest(true);
            if (!HasCombatTarget())
            {
                return false;
            }

            Player target = Main.player[NPC.target];
            bool launched = false;
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile projectile = Main.projectile[i];
                if (!projectile.active || projectile.type != OrbType || (int)projectile.ai[0] != NPC.whoAmI || projectile.ai[1] < 0)
                {
                    continue;
                }

                // An orb still forming (its localAI[1] age, counted on every machine) stays in the crown until it is whole.
                if (projectile.localAI[1] < CrownFormingTicks)
                {
                    continue;
                }

                projectile.ai[1] = -ReleasedOrbTicks;
                projectile.velocity = UsefulFunctions.Aim(projectile.Center, target.Center, 1.4f);
                projectile.timeLeft = ReleasedOrbTicks;
                projectile.netUpdate = true;
                launched = true;
            }

            if (launched)
            {
                crownGesture = CrownGesture.Launch;
                crownGestureTimer = 0;
                NPC.netUpdate = true;
            }

            return launched;
        }

        // The trap lands 62px behind the way the target is facing; the meteor reads NPC.damage / 2 for its own
        // body/blast (so scripted-event overrides still reach it) and ai0 for its orb pair.
        private void SpawnTrap()
        {
            Player target = Main.player[NPC.target];
            Vector2 site = target.Center - new Vector2(target.direction * 62f, 0f);
            Projectile.NewProjectile(NPC.GetSource_FromAI(), site, Vector2.Zero, MeteorType, NPC.damage / 2, 0f, Main.myPlayer, LostSoulDamage);
            trapSite = site;
            trapGestureTimer = 0;
            NPC.netUpdate = true;
        }

        private void CancelMove()
        {
            if (Main.netMode != NetmodeID.MultiplayerClient && burstProjectileIndex >= 0)
            {
                Projectile burst = Main.projectile[burstProjectileIndex];
                bool ownBurst = burst.active && burst.type == ModContent.ProjectileType<FireLurkerFireBurst>() && (int)burst.ai[0] == NPC.whoAmI;
                if (ownBurst && stage == Stage.Tell)
                {
                    burst.Kill();
                }
            }

            burstProjectileIndex = -1;
            move = LurkerMove.None;
            stage = Stage.None;
            stageTimer = 0;
            selectionDelay = 60;
            NPC.noGravity = false;
            NPC.netUpdate = true;
        }

        public override void OnStagger(NPC npc)
        {
            base.OnStagger(npc);
            CancelMove();
        }

        public override void OnHitByItem(Player player, Item item, NPC.HitInfo hit, int damageDone)
        {
            NPC.GetGlobalNPC<tsorcRevampGlobalNPC>().RequestHitReaction(NPC, true);
        }

        public override void OnHitByProjectile(Projectile projectile, NPC.HitInfo hit, int damageDone)
        {
            NPC.GetGlobalNPC<tsorcRevampGlobalNPC>().RequestHitReaction(NPC, projectile.DamageType == DamageClass.Melee);
        }

        // Any hit launches the held crown instantly. This ignores the 30t release gap on purpose.
        void IHitReactor.OnServerHit(NPC npc, bool melee)
        {
            LaunchCrown();
        }

        private void DecorateLurker()
        {
            if (Main.rand.NextBool(GroanFrequency))
            {
                SoundEngine.PlaySound(SoundID.Mummy, NPC.Center);
            }

            if (move != LurkerMove.None && stage == Stage.Action && stageTimer == 0)
            {
                if (move == LurkerMove.Hurl)
                {
                    SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.35f, Pitch = -0.2f }, NPC.Center);
                }
                else if (move == LurkerMove.Leap)
                {
                    SoundEngine.PlaySound(SoundID.Item1 with { Pitch = -0.5f }, NPC.Center);
                }
            }

            bool swipeSoundTick = (move == LurkerMove.Rake && stage == Stage.Action && stageTimer == ClawLiveStart)
                || (move == LurkerMove.Leap && swipeStarted && swipeTimer == ClawLiveStart);
            if (swipeSoundTick)
            {
                SoundEngine.PlaySound(SoundID.Item1 with { Pitch = -0.3f }, NPC.Center);
            }

            bool clawTell = (move == LurkerMove.Rake || move == LurkerMove.Leap) && stage == Stage.Tell;
            bool leapAirborne = move == LurkerMove.Leap && stage == Stage.Action && !landed;
            if (clawTell || leapAirborne || MeleeLive)
            {
                float angle = ResolveClawAngle();
                for (int hand = 0; hand < 2; hand++)
                {
                    Vector2 handPosition = HandPosition(angle) + new Vector2(hand * 6f * lockedFacing, 0f);
                    if (MeleeLive || Main.rand.NextBool(3))
                    {
                        Dust ember = Dust.NewDustPerfect(handPosition + Main.rand.NextVector2Circular(3f, 3f), EmberDust, NPC.velocity * 0.2f + Main.rand.NextVector2Circular(0.6f, 0.6f), 90, EmberColor, MeleeLive ? 1.4f : 1.1f);
                        ember.noGravity = true;
                    }
                }

                Lighting.AddLight(NPC.Center, EmberColor.ToVector3() * 0.45f);
            }

            if (move == LurkerMove.Hurl && stage == Stage.Tell && Main.rand.NextBool(3))
            {
                Vector2 hand = HandPosition(ResolveHurlAngle());
                Dust ember = Dust.NewDustPerfect(hand + Main.rand.NextVector2Circular(5f, 5f), EmberDust, Main.rand.NextVector2Circular(0.3f, 0.3f), 100, EmberColor, 1.2f);
                ember.noGravity = true;
            }

            if (move == LurkerMove.Burst && stage == Stage.Action && stageTimer == 0)
            {
                for (int i = 0; i < 16; i++)
                {
                    Vector2 velocity = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(2f, 4.5f);
                    Dust fire = Dust.NewDustPerfect(NPC.Center, EmberDust, velocity, 80, EmberColor, 1.4f);
                    fire.noGravity = true;
                }
            }

            if (trapGestureTimer < TrapGestureTicks && Main.rand.NextBool(3))
            {
                float pointAngle = ResolveArmAngle(false) ?? ArmRestAngle;
                Dust ember = Dust.NewDustPerfect(HandPosition(pointAngle), EmberDust, new Vector2(0f, -1f), 100, EmberColor, 1.1f);
                ember.noGravity = true;
            }

            if (crownGesture == CrownGesture.Form)
            {
                Vector2 crownCenter = NPC.Top + new Vector2(0f, -CrownHeight);
                Vector2 offset = Main.rand.NextVector2CircularEdge(26f, 26f);
                Dust converge = Dust.NewDustPerfect(crownCenter + offset, EmberDust, -offset * 0.08f, 90, EmberColor, 1.1f);
                converge.noGravity = true;
            }

            // Enraged: faint flames lick along both arms.
            if (Enraged && Main.rand.NextBool(4))
            {
                Vector2 armPosition = NPC.Center + new Vector2(Main.rand.NextFloat(-9f, 9f), Main.rand.NextFloat(-8f, 10f));
                Dust flame = Dust.NewDustPerfect(armPosition, EmberDust, new Vector2(0f, -1.2f), 100, EmberColor, 1f);
                flame.noGravity = true;
            }
        }

        public override void PostDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            base.PostDraw(spriteBatch, screenPos, drawColor);
            bool holdingOrb = (hurlPrep && move == LurkerMove.None) || (move == LurkerMove.Hurl && stage == Stage.Tell);
            if (!holdingOrb)
            {
                return;
            }

            // The orb forms in the palm: .25 -> 1 scale over the last 20 of its 60 visible ticks.
            int visibleTicks = hurlPrepTimer;
            float armAngle = HurlHoldAngle;
            if (move == LurkerMove.Hurl)
            {
                visibleTicks = HurlPrepTicks - tellTicks + stageTimer;
                armAngle = ResolveHurlAngle();
            }

            float growth = MathHelper.Clamp((visibleTicks - (HurlPrepTicks - 20)) / 20f, 0f, 1f);
            float scale = MathHelper.Lerp(0.25f, 1f, growth) * NPC.scale;
            Texture2D texture = TextureAssets.Projectile[OrbType].Value;
            int frameCount = Math.Max(1, Main.projFrames[OrbType]);
            int frameHeight = texture.Height / frameCount;
            int frame = (int)(Main.GameUpdateCount / 4 % (ulong)frameCount);
            Rectangle source = new Rectangle(0, frame * frameHeight, texture.Width, frameHeight);
            Vector2 hand = HandPosition(armAngle);
            Color color = Color.Lerp(drawColor, Color.White, 0.6f);
            Lighting.AddLight(hand, EmberColor.ToVector3() * 0.45f);
            spriteBatch.Draw(texture, hand - screenPos, source, color, 0f, source.Size() * 0.5f, scale, SpriteEffects.None, 0f);
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
            writer.Write(lockedTarget.X);
            writer.Write(lockedTarget.Y);
            writer.Write(leapVelocity.X);
            writer.Write(leapVelocity.Y);
            writer.Write(swipeStarted);
            writer.Write(swipeTimer);
            writer.Write(landed);
            writer.Write(landedTimer);
            writer.Write(hurlPrep);
            writer.Write(hurlPrepTimer);
            writer.Write((byte)crownGesture);
            writer.Write(crownGestureTimer);
            writer.Write(trapGestureTimer);
            writer.Write(trapSite.X);
            writer.Write(trapSite.Y);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            base.ReceiveExtraAI(reader);
            move = (LurkerMove)reader.ReadByte();
            stage = (Stage)reader.ReadByte();
            stageTimer = reader.ReadInt32();
            stageDuration = reader.ReadInt32();
            tellTicks = reader.ReadInt32();
            targetIndex = reader.ReadInt32();
            lockedFacing = reader.ReadInt32();
            sequence = reader.ReadInt32();
            lockedTarget = new Vector2(reader.ReadSingle(), reader.ReadSingle());
            leapVelocity = new Vector2(reader.ReadSingle(), reader.ReadSingle());
            swipeStarted = reader.ReadBoolean();
            swipeTimer = reader.ReadInt32();
            landed = reader.ReadBoolean();
            landedTimer = reader.ReadInt32();
            hurlPrep = reader.ReadBoolean();
            hurlPrepTimer = reader.ReadInt32();
            crownGesture = (CrownGesture)reader.ReadByte();
            crownGestureTimer = reader.ReadInt32();
            trapGestureTimer = reader.ReadInt32();
            trapSite = new Vector2(reader.ReadSingle(), reader.ReadSingle());
        }
    }
}

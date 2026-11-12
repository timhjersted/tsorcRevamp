using System;
using System.IO;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.NPCs.Invaders;

namespace tsorcRevamp.NPCs.Enemies.Archer
{
    /// <summary>
    /// Shared longbow archer for the hunter puppets (Man Hunter, Assassin). The bow rests flat at the waist in the far
    /// hand, lifts into the shared PuppetNPC string-draw pose to shoot drop-compensated, leading arrows, and sinks back
    /// after the shot. Subclasses supply the bow, arrow, timings and anything else they do.
    /// </summary>
    public abstract class HunterArcherPuppet : PuppetNPC
    {
        // ── Bow carry and raise ──────────────────────────────────────────────────────────────────────────────
        // Facing-space angles (0 = level forward, +PiOver2 = hanging straight down). At 1.2 rad the far forearm
        // points down-forward and the bow, kept perpendicular to it, lies almost flat with its string on top.
        // The near hand rests on the bow.
        protected const float RestCarryAngle = 1.20f;
        protected const float RestFrontHandAngle = 1.00f;
        protected const int RaiseTicks = 12;
        protected const int LowerTicks = 12;
        // Composite-arm string hand at release, relative to the bow axis: base BowPulledAngle 2.2 + flick 0.25.
        private const float ReleasedStringHandOffset = 2.45f;

        // ── Aim solve ────────────────────────────────────────────────────────────────────────────────────────
        // Vanilla arrow AI: straight for 15t, then +0.1 px/t² gravity.
        private const float ArrowGravityDelayTicks = 15f;
        private const float ArrowGravity = 0.1f;
        // Half the target's vertical speed: a jump arc bends back down within the flight time.
        private const float ArrowVerticalLeadFraction = 0.5f;
        private const int ArrowInterceptPasses = 4;
        // Aim limits in facing space (negative = up): straight up to straight down.
        private const float MaxUpAim = -MathHelper.PiOver2;
        private const float MaxDownAim = MathHelper.PiOver2;

        // ── Subclass inputs ──────────────────────────────────────────────────────────────────────────────────
        /// <summary>Presentation-only bow, held in the far hand (back slot).</summary>
        protected abstract PuppetWeapon CarriedBow { get; }
        protected abstract Vector2 BowGripNorm { get; }
        /// <summary>The bow sprite's string texels (facing right), replaced by the live string while drawing.</summary>
        protected abstract Rectangle BowStringTexels { get; }
        protected virtual float BowDrawScale => 1f;
        protected abstract int ArrowProjectileType { get; }
        /// <summary>Passed as the arrow's ai[2] (ai[0] is vanilla arrow AI's gravity counter).</summary>
        protected virtual float ArrowAi2 => 0f;
        protected abstract float ArrowSpeed { get; }
        /// <summary>The aim locks this many ticks before each release; hits during the draw cancel it only before.</summary>
        protected abstract int AimLockTicks { get; }
        protected virtual int CancelledShotRecoveryTicks => 24;
        /// <summary>Added to the recovery for each chained shot after the first.</summary>
        protected virtual int ChainRecoveryPerExtraShot => 0;
        /// <summary>Hyper-armor (no flinch, knockback or poise) from the aim lock until the release.</summary>
        protected virtual bool HyperArmorFromAimLock => false;
        /// <summary>Full-draw effects at the nocked arrowhead (dust, light). Called every tick of the last 35% of the draw.</summary>
        protected abstract void DoArrowheadVFX(Vector2 arrowhead);

        /// <summary>Every peer, on the first tick of a shot's recovery (after the chained-recovery extension).</summary>
        protected virtual void OnShotRecoveryStarted(bool authority)
        {
        }

        // Synced: the locked shot direction, and the shots fired this volley (sizes the chained recovery).
        protected Vector2 _shotAim = Vector2.UnitX;
        protected int _shotsThisVolley;
        // Visual only, re-derived on every peer: the last drawn bow angle, so the lower starts where the bow is.
        private float _lowerFromAngle = RestCarryAngle;
        private int _recoveryTicksElapsed;
        private AttackPhase _lastPhase = AttackPhase.Idle;
        // Server: the phase at the start of the previous tick, to spot the first tick of each chained re-draw.
        private AttackPhase _phaseBeforeLastAI = AttackPhase.Idle;

        // ── Puppet configuration shared by every hunter ──────────────────────────────────────────────────────
        protected override RangedStyle RangedAnimStyle => RangedStyle.Bow;
        // Always plants to shoot.
        protected override int StandingRangedChance => 100;
        protected override bool UseCompositeArmSwing => true;
        protected override bool UseCompositeArmForAdditionalPhase => InShotPose;
        protected override bool UseBowStringDrawPose => true;
        protected override Vector2 GetHeldRangedGripNorm(int itemType) => BowGripNorm;
        protected override float GetHeldRangedDrawScale(int itemType) => BowDrawScale;
        protected override Rectangle GetBowStringTexels(int itemType) => BowStringTexels;

        // The bow is never in the near hand outside a shot: the far-hand prop below carries it.
        protected override bool ShowWeaponDuringNeutral => false;
        protected override bool ShowRangedWeaponDuringRecovery => false;
        // Far-hand bow outside the three shot phases. Inside them the shared string-draw pose draws the bow at the
        // same back-hand grip (PuppetBowGripPosition), so the bow never changes hands.
        protected override bool IndependentBackHandWeapon => !InShotPose;
        protected override float IndependentBackHandFacingAngle => CarryAngle;
        // Bow kept perpendicular to the forearm, as in the draw pose, so carry and draw are one geometry.
        protected override float IndependentBackHandDrawRotation => CarryAngle;

        protected bool InShotPose => Phase == AttackPhase.RangedTelegraph || Phase == AttackPhase.RangedAttack
            || Phase == AttackPhase.CrossbowBurstPause;

        protected bool IsNeutral => Phase == AttackPhase.Idle || Phase == AttackPhase.CasualStroll
            || Phase == AttackPhase.ClosingDistance;

        // Facing-space aim of the synced shot, clamped to the archer's articulation.
        protected float LockedAimAngle
        {
            get
            {
                float facingAngle = (float)Math.Atan2(_shotAim.Y, _shotAim.X * NPC.direction);
                return MathHelper.Clamp(facingAngle, MaxUpAim, MaxDownAim);
            }
        }

        // Shared string-draw aim. The opening draw lifts the bow from the carry angle over RaiseTicks; the bow arm,
        // bow tilt, string hand and arrow origin all follow this one angle. Pure function of synced state.
        protected override float BowAimAngle
        {
            get
            {
                float aim = LockedAimAngle;

                if (Phase != AttackPhase.RangedTelegraph)
                {
                    return aim;
                }

                int elapsed = RangedTelegraphTicks - PhaseTimer;
                float raise = MathHelper.Clamp(elapsed / (float)RaiseTicks, 0f, 1f);
                float blend = MathHelper.SmoothStep(0f, 1f, raise);
                return MathHelper.Lerp(RestCarryAngle, aim, blend);
            }
        }

        // Far-hand carry angle. Recovery lowers the bow from wherever it was drawn over LowerTicks.
        protected float CarryAngle
        {
            get
            {
                if (Phase != AttackPhase.RangedRecovery)
                {
                    return RestCarryAngle;
                }

                float lower = MathHelper.Clamp(_recoveryTicksElapsed / (float)LowerTicks, 0f, 1f);
                float blend = MathHelper.SmoothStep(0f, 1f, lower);
                return MathHelper.Lerp(_lowerFromAngle, RestCarryAngle, blend);
            }
        }

        // Near hand: rests on the bow in neutral. After a shot it drops from the cheek, back and down behind the
        // body (the -2π target keeps the lerp on that side), to the bow over LowerTicks.
        protected override float? FreeFrontArmFacingAngle
        {
            get
            {
                if (IsNeutral)
                {
                    return RestFrontHandAngle;
                }

                if (Phase != AttackPhase.RangedRecovery)
                {
                    return null;
                }

                float releasedHand = _lowerFromAngle - ReleasedStringHandOffset;
                float lower = MathHelper.Clamp(_recoveryTicksElapsed / (float)LowerTicks, 0f, 1f);
                float blend = MathHelper.SmoothStep(0f, 1f, lower);
                return MathHelper.Lerp(releasedHand, RestFrontHandAngle - MathHelper.TwoPi, blend);
            }
        }

        public override void AI()
        {
            bool authority = Main.netMode != NetmodeID.MultiplayerClient;

            EquipWeapon(PuppetHandSlot.Back, CarriedBow);

            if (authority && NPC.HasValidTarget)
            {
                // Flinch: a hit while drawing cancels the shot, but the last AimLockTicks before a release are
                // committed. NPC.justHit is set on the server by every strike, including clients' hits.
                bool drawing = Phase == AttackPhase.RangedTelegraph || Phase == AttackPhase.CrossbowBurstPause;

                if (drawing && NPC.justHit && PhaseTimer > AimLockTicks)
                {
                    EnterPhase(AttackPhase.RangedRecovery, CancelledShotRecoveryTicks);
                    NPC.netUpdate = true;
                }
                else if (drawing)
                {
                    bool newPause = Phase == AttackPhase.CrossbowBurstPause && _phaseBeforeLastAI != AttackPhase.CrossbowBurstPause;
                    TrackShotAim(Main.player[NPC.target], newPause);
                }
            }

            _phaseBeforeLastAI = Phase;

            base.AI();

            // Every peer sees the same synced phase changes, so these visual and timing edits agree everywhere.
            if (Phase == AttackPhase.RangedTelegraph && _lastPhase != AttackPhase.RangedTelegraph)
            {
                _shotsThisVolley = 0;
            }

            if (InShotPose)
            {
                _lowerFromAngle = BowAimAngle;
            }

            // Lower clock: a local counter, so a late PhaseTimer snapshot can't rewind the bow back up.
            _recoveryTicksElapsed++;

            if (Phase == AttackPhase.RangedRecovery && _lastPhase != AttackPhase.RangedRecovery)
            {
                _recoveryTicksElapsed = 0;

                // Chained recovery: +ChainRecoveryPerExtraShot per extra shot. Server only; the snapshot carries the
                // longer timer to clients, so a client that saw the transition first can't add it twice.
                if (authority)
                {
                    int extraShots = Math.Max(0, _shotsThisVolley - 1);
                    PhaseTimer += ChainRecoveryPerExtraShot * extraShots;
                    NPC.netUpdate = true;
                }

                OnShotRecoveryStarted(authority);
            }

            // Hyper-armor from the aim lock to the release. PuppetNPC.AI has just published the draw as a
            // still-interruptible windup; overwrite that with "committed" (no knockback, no poise) until next tick.
            // Derived from synced Phase/PhaseTimer, so every peer agrees without a packet.
            bool lockedDraw = Phase == AttackPhase.RangedTelegraph && PhaseTimer <= AimLockTicks;

            if (HyperArmorFromAimLock && lockedDraw)
            {
                tsorcRevampGlobalNPC globalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
                globalNPC.AttackCommitted = true;
                globalNPC.AttackTelegraphing = false;
            }

            _lastPhase = Phase;
        }

        // Server: tracks the target until AimLockTicks before each release, then holds. The first tick of every
        // chained re-draw re-solves once, since those pauses are mostly inside the lock window.
        private void TrackShotAim(Player target, bool newPause)
        {
            bool tracking = PhaseTimer > AimLockTicks;

            if (!tracking && !newPause)
            {
                return;
            }

            _shotAim = SolveArrowAim(target);

            bool lockTick = PhaseTimer == AimLockTicks + 1;

            if (newPause || lockTick || PhaseTimer % 6 == 0)
            {
                NPC.netUpdate = true;
            }
        }

        // Intercept aim for a vanilla-AI arrow: lead the target by the flight time and raise the aim point by the
        // arrow's drop after its 15t straight phase. Four passes converge well under a pixel.
        private Vector2 SolveArrowAim(Player target)
        {
            Vector2 origin = PuppetBowGripPosition;
            Vector2 aimAt = target.Center;

            for (int i = 0; i < ArrowInterceptPasses; i++)
            {
                float flightTicks = Vector2.Distance(origin, aimAt) / ArrowSpeed;
                Vector2 lead = new Vector2(target.velocity.X, target.velocity.Y * ArrowVerticalLeadFraction) * flightTicks;
                float fallingTicks = Math.Max(0f, flightTicks - ArrowGravityDelayTicks);
                // Discrete fall: sum of 0.1 * k for k = 1..n.
                float gravityDrop = ArrowGravity * fallingTicks * (fallingTicks + 1f) * 0.5f;
                aimAt = target.Center + lead - new Vector2(0f, gravityDrop);
            }

            Vector2 fallback = new Vector2(NPC.direction, 0f);
            return (aimAt - origin).SafeNormalize(fallback);
        }

        // Shots face the synced aim once it locks, so a roll behind the archer can't flip the committed release.
        protected override void FaceTargetForRangedShot(Player target)
        {
            bool committed = Phase == AttackPhase.RangedAttack || PhaseTimer <= AimLockTicks;

            if (!committed || Math.Abs(_shotAim.X) < 0.05f)
            {
                base.FaceTargetForRangedShot(target);
                return;
            }

            int facing = 1;

            if (_shotAim.X < 0f)
            {
                facing = -1;
            }

            NPC.direction = facing;
            NPC.spriteDirection = facing;
        }

        protected override void DoRangedAttack()
        {
            _shotsThisVolley++;
            Vector2 origin = PuppetBowGripPosition;

            if (!Main.dedServ)
            {
                SoundEngine.PlaySound(SoundID.Item5 with { Volume = 0.7f, PitchVariance = 0.15f }, origin);
            }

            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            // The arrow leaves along the drawn bow's axis, so a clamped aim fires where the bow points.
            float aim = LockedAimAngle;
            Vector2 direction = new Vector2((float)Math.Cos(aim) * NPC.direction, (float)Math.Sin(aim));

            Projectile.NewProjectile(NPC.GetSource_FromAI(), origin, direction * ArrowSpeed,
                ArrowProjectileType, RangedDamage, 0f, Main.myPlayer, 0f, 0f, ArrowAi2);
        }

        // Full draw: the subclass's effects at the nocked arrowhead, just ahead of the grip.
        protected override void DoRangedTelegraphVFX(bool secondary, float progress)
        {
            if (Main.dedServ || PhaseTimer > RangedTelegraphTicks * 0.35f)
            {
                return;
            }

            float aim = BowAimAngle;
            Vector2 aimDirection = new Vector2((float)Math.Cos(aim) * NPC.direction, (float)Math.Sin(aim));
            Vector2 arrowhead = PuppetBowGripPosition + aimDirection * 6f;
            DoArrowheadVFX(arrowhead);
        }

        // Evasion only fires in neutral, so it never cuts a draw, a melee attack or a recovery short.
        public override void OnHitByItem(Player player, Item item, NPC.HitInfo hit, int damageDone)
        {
            if (IsNeutral)
            {
                tsorcRevampAIs.EvasiveOnHit(NPC, true);
            }
        }

        public override void OnHitByProjectile(Projectile projectile, NPC.HitInfo hit, int damageDone)
        {
            if (IsNeutral)
            {
                tsorcRevampAIs.EvasiveOnHit(NPC, projectile.DamageType == DamageClass.Melee);
            }
        }

        // Subclass state first, then this, then PuppetNPC, read back in the same order.
        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(_shotAim.X);
            writer.Write(_shotAim.Y);
            writer.Write((byte)_shotsThisVolley);
            base.SendExtraAI(writer);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            _shotAim = new Vector2(reader.ReadSingle(), reader.ReadSingle());
            _shotsThisVolley = reader.ReadByte();
            base.ReceiveExtraAI(reader);
        }
    }
}

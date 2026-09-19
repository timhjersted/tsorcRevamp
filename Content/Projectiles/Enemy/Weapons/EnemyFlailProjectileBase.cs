using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.NPCs.Puppets;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.Content.Projectiles.Enemy.Weapons
{
    /// <summary>Projectile-owned mace sequences. Values 0-2 are the legacy Throw/Spin/SpinThenLash
    /// modes; these authored patterns start at 3 so existing callers keep their wire format.</summary>
    public enum EnemyFlailAttackPattern
    {
        None = 0,
        OverheadChain = 3,
        UnderhandChain = 4,
        AdvancingCyclone = 5,
        ReverseHalo = 6,
        AnkleReaper = 7,
        BacklashReversal = 8,
    }

    /// <summary>Shared names and timing for PuppetNPC mace attacks. The projectile owns the whole
    /// sequence, so a follow-up changes the motion of one head instead of spawning another head.</summary>
    /// <remarks>
    /// Attack spec — Chainfall / Chainrise / Advancing Chainstorm / Reverse Halo / Ankle Reaper:
    /// Weapon: the owner's existing flail head+chain subclass, anchored through IFlailAnchor and visible
    /// for the entire 40t tell. Tell: one harmless mirrored revolution at 60px; the head itself predicts
    /// direction and reach, so no aim line or extra spawn prop is used. Birth: at the starting orbit
    /// point with the chain already connected, rather than a second prop appearing at release.
    /// Life: directional attacks expand through a 90-degree arc to 240px in 18t; Chainstorm expands over
    /// one 36t revolution, holds 240px for three more 36t revolutions, and advances its committed owner.
    /// Reverse Halo and Ankle Reaper instead grow 60->96px through their 40t tell, lash level to 208px
    /// in 16t, and are only selectable when the target is within +/-48px vertically. Reverse Halo uses
    /// a bounded shoulder; Ankle Reaper uses its authored full 360-degree arm turn and flows directly
    /// from counter-clockwise cruise into extension. Both are live only on lash frames 3-16.
    /// Backlash Reversal is a reactive follow-up rather than a free-standing pick: after an authored mace
    /// move, it requires the target to remain behind the attacker's committed facing. It uses a 40t load,
    /// 6t planted reversal, 18t eased rear whip to 192px, 15 live whip frames, 18t retract, and 60t recovery.
    /// Death: 18t visible retract to the hand, then deliberate removal; no timeout disappearance or child.
    /// Reach: 60px close / 208px level lash / 240px maximum, all position-driven and exact.
    /// Terrain: the swept chain deliberately keeps the existing flail behavior of passing through tiles;
    /// owner locomotion still collides normally, and walls therefore remain usable against the advance.
    /// Player effect: subclass effects only (Dread Wraith retains On Fire); no throw or pull. VFX: chain
    /// behind head, head over world, no shader; Dread Wraith retains its fire dust. Selection: the original
    /// three patterns are in shared Flail/Dread Wraith pools; level lashes are optional, with Black Ninja
    /// explicitly opted in. The existing one-head gate remains. Fairness: directional live
    /// window is 18t; Chainstorm crossings are 36t apart and can also be escaped by spacing/cross-through.
    /// Multiplayer: server chooses/spawns/releases; ai[0..2] synchronize owner, pattern, release and reach.
    /// Damage: owner melee damage times combo multiplier (1.15 / 1.10 / 0.75 respectively).
    /// </remarks>
    public static class EnemyFlailAttackPatterns
    {
        public const string OverheadName = "Chainfall Overhead";
        public const string UnderhandName = "Chainrise Underhand";
        public const string CycloneName = "Advancing Chainstorm";
        public const string ReverseHaloName = "Reverse Halo";
        public const string AnkleReaperName = "Ankle Reaper";
        public const string BacklashName = "Backlash Reversal";

        public const int TelegraphTicks = 40;
        public const int ArcSwingTicks = 18;
        public const int LevelLashTicks = 16;
        public const int BacklashLoadTicks = 6;
        public const int BacklashWhipTicks = 18;
        public const int RetractTicks = 18;
        public const int RotationTicks = 36;
        public const int MaximumRadiusRotations = 3;
        public const float TelegraphRadius = 60f;
        public const float LevelLashOrbitRadius = 96f;
        public const float LevelLashReach = 13f * 16f;
        public const float LevelTargetVerticalTolerance = 48f;
        public const float LevelLashCarryArm = -1.87f;
        public const float BacklashReach = 12f * 16f;
        public const float BacklashTargetVerticalTolerance = 64f;
        public const float MaximumReach = 15f * 16f;

        public const int ArcAttackTicks = ArcSwingTicks + RetractTicks;
        public const int LevelLashAttackTicks = LevelLashTicks + RetractTicks;
        public const int BacklashAttackTicks = BacklashLoadTicks + BacklashWhipTicks + RetractTicks;
        public const int CycloneLiveTicks = RotationTicks * (1 + MaximumRadiusRotations);
        public const int CycloneAttackTicks = CycloneLiveTicks + RetractTicks;
        public const int MaximumLifetime = TelegraphTicks + CycloneAttackTicks + 30;

        public static bool TryResolve(string comboName, out EnemyFlailAttackPattern pattern)
        {
            pattern = comboName switch
            {
                OverheadName => EnemyFlailAttackPattern.OverheadChain,
                UnderhandName => EnemyFlailAttackPattern.UnderhandChain,
                CycloneName => EnemyFlailAttackPattern.AdvancingCyclone,
                ReverseHaloName => EnemyFlailAttackPattern.ReverseHalo,
                AnkleReaperName => EnemyFlailAttackPattern.AnkleReaper,
                BacklashName => EnemyFlailAttackPattern.BacklashReversal,
                _ => EnemyFlailAttackPattern.None,
            };
            return pattern != EnemyFlailAttackPattern.None;
        }

        public static float ReachFor(EnemyFlailAttackPattern pattern)
            => pattern == EnemyFlailAttackPattern.ReverseHalo
                || pattern == EnemyFlailAttackPattern.AnkleReaper
                    ? LevelLashReach
                    : pattern == EnemyFlailAttackPattern.BacklashReversal
                        ? BacklashReach
                    : MaximumReach;

        public static bool RequiresLevelTarget(string comboName)
            => comboName == ReverseHaloName || comboName == AnkleReaperName;

        public static bool LocksFacingDuringTell(string comboName)
            => RequiresLevelTarget(comboName) || comboName == BacklashName;
    }

    /// <summary>
    /// Shared "ball + chain" flail projectile for AI enemies: the chain+head owns the projectile visual while
    /// the owning puppet's composite arm follows its bounded ball direction. The projectile is drawn from the
    /// owner NPC's hand anchor. Its motion mode
    /// is selected via ai[1]: the legacy Spin/Throw/SpinThenLash modes plus projectile-owned authored patterns.
    /// ai[0] is the owner NPC's whoAmI.
    /// <para/>
    /// Extracted from BlackNinja's EnemyDiamondCrusherBall so any sprite-sheet NPC can reuse it: implement
    /// IFlailAnchor for a proper rigged anchor point (falls back to a generic Center+offset guess otherwise, which
    /// keeps existing owners like BlackNinja unchanged), subclass this for the chain/head textures, and override
    /// OnFlailTick for extras (e.g. a periodic AOE pulse).
    /// </summary>
    public abstract class EnemyFlailProjectileBase : ModProjectile
    {
        protected abstract string ChainTexturePath { get; }

        protected NPC Owner => Projectile.ai[0] >= 0 && Projectile.ai[0] < Main.maxNPCs ? Main.npc[(int)Projectile.ai[0]] : null;
        protected bool SpinMode => Projectile.ai[1] == 1f;

        /// <summary>Third motion mode: orbit the anchor as a visible telegraph, then lash straight out to a
        /// caller-chosen reach (<see cref="LashReach"/>, passed as ai[2]) and reel back. Unlike Throw, the
        /// reach is exact rather than a product of speed and airtime, so the owner can aim it at a measured
        /// distance. The aim direction is locked at the moment of discharge, so the lash can be dodged.</summary>
        protected bool SpinThenLashMode => Projectile.ai[1] == 2f;

        protected EnemyFlailAttackPattern AuthoredPattern =>
            Projectile.ai[1] >= (float)EnemyFlailAttackPattern.OverheadChain
                && Projectile.ai[1] <= (float)EnemyFlailAttackPattern.BacklashReversal
                ? (EnemyFlailAttackPattern)(int)Projectile.ai[1]
                : EnemyFlailAttackPattern.None;

        private bool AuthoredPatternMode => AuthoredPattern != EnemyFlailAttackPattern.None;

        /// <summary>Target extension (px) for a SpinThenLash discharge, set by the owner via ai[2].</summary>
        protected float LashReach => Projectile.ai[2];

        private const int WindupSpinTicks = 9;
        private Vector2 _launchVelocity;
        private Vector2 _lashDirection;
        private float _lashExtent;
        private int _patternFacing;
        private bool _patternDamageActive;
        private bool _useAuthoredArmPose;
        private float _authoredArmFacingRight;

        /// <summary>Ticks the head flies outward before reeling back. Reach = this * launch speed.</summary>
        protected virtual float OutwardTicks => 18f;
        /// <summary>Speed the head reels back in at.</summary>
        protected virtual float ReturnSpeed => 13f;
        /// <summary>Total lifetime. Must cover windup + outward + the return trip, or the head is
        /// deleted mid-flight and the chain visually snaps.</summary>
        protected virtual int Lifetime => 54;

        // ── SpinThenLash tuning ──────────────────────────────────────────────────
        /// <summary>Length of the orbiting telegraph before the lash fires.</summary>
        protected virtual int SpinTelegraphTicks => 45;
        /// <summary>Orbit radius (px) held during the telegraph. 80px = 5 tiles.</summary>
        protected virtual float SpinRadius => 80f;
        /// <summary>Ticks taken to extend from the orbit out to LashReach. Constant regardless of reach, so
        /// a long lash is visibly faster than a short one — distance reads as danger.</summary>
        protected virtual int LashOutTicks => 10;
        /// <summary>Ticks taken to retract from full extension back to the hand.</summary>
        protected virtual int LashReturnTicks => 16;

        public override void SetDefaults()
        {
            Projectile.width = 26;
            Projectile.height = 26;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = Lifetime;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 16;
        }

        /// <summary>The point the chain is drawn to and the ball orbits/launches from. Uses the owner's IFlailAnchor
        /// if it implements one, otherwise the generic Center+offset guess the original Invader ball used.</summary>
        private Vector2 GetHandAnchor(NPC owner)
        {
            if (owner.ModNPC is NPCs.IFlailAnchor anchor)
            {
                return anchor.GetFlailAnchor();
            }
            return owner.Center + new Vector2(owner.direction * 12f, -owner.height * 0.2f);
        }

        /// <summary>Called once per AI tick after the ball's own motion is resolved. Base does nothing; override for
        /// extras like a periodic AOE pulse.</summary>
        protected virtual void OnFlailTick(NPC owner, Vector2 hand) { }

        /// <summary>Called exactly once, the tick a Throw-mode ball is released (never fires in Spin mode). Base
        /// does nothing; override for a one-off effect at the moment of the swing, e.g. flicking off embers along
        /// the launch direction.</summary>
        protected virtual void OnLaunch(NPC owner, Vector2 launchVelocity) { }

        /// <summary>Releases an authored pattern after the PuppetNPC's visible combo telegraph completes.
        /// The negative reach is the synchronized release flag; its absolute value remains the reach.</summary>
        public void ReleaseAuthoredPattern()
        {
            if (!AuthoredPatternMode || Projectile.ai[2] < 0f)
            {
                return;
            }

            Projectile.ai[2] = -System.Math.Max(1f, Projectile.ai[2]);
            Projectile.localAI[1] = 0f;
            Projectile.netUpdate = true;
        }

        public override bool? CanDamage()
        {
            if (AuthoredPatternMode)
            {
                return _patternDamageActive ? null : false;
            }

            // SpinThenLash calls its orbit a telegraph, so only the outward lash is damaging.
            if (SpinThenLashMode)
            {
                int tick = (int)Projectile.localAI[0];
                int lashTick = tick - SpinTelegraphTicks;
                return lashTick >= 1 && lashTick <= LashOutTicks ? null : false;
            }

            return null;
        }

        public override void AI()
        {
            NPC owner = Owner;
            if (owner == null || !owner.active)
            {
                Projectile.Kill();
                return;
            }

            Projectile.localAI[0]++;
            Vector2 hand = GetHandAnchor(owner);

            if (AuthoredPatternMode)
            {
                TickAuthoredPattern(owner, hand);
                if (Projectile.active && owner.ModNPC is PuppetNPC puppet)
                {
                    // Keep the real composite arm attached to the same head/chain that the
                    // projectile just positioned. Ankle Reaper deliberately opts into the
                    // unwrapped full-circle pose approved in its preview; Reverse Halo supplies
                    // its approved bounded pose directly. Other patterns retain the smoothed
                    // generic shoulder follower.
                    puppet.UpdateFlailProjectileArmPose(hand, Projectile.Center,
                        _useAuthoredArmPose, _authoredArmFacingRight);
                }
            }
            else if (SpinMode)
            {
                float radius = MathHelper.Lerp(24f, 82f, MathHelper.Clamp(Projectile.localAI[0] / 28f, 0f, 1f));
                float angle = Projectile.localAI[0] * 0.42f * owner.direction;
                Vector2 offset = new Vector2(radius, 0f).RotatedBy(angle);
                offset.Y *= 0.75f;
                Projectile.Center = hand + offset;
                Projectile.velocity = Vector2.Zero;
            }
            else if (SpinThenLashMode)
            {
                int tick = (int)Projectile.localAI[0];

                if (tick <= SpinTelegraphTicks)
                {
                    // Telegraph: orbit the hand on a growing radius so the wind-up is unmistakable.
                    // Held at SpinRadius (5 tiles) — big enough to read from off-screen edge.
                    float windupProgress = MathHelper.Clamp(tick / (float)SpinTelegraphTicks, 0f, 1f);
                    float radius = MathHelper.Lerp(20f, SpinRadius, windupProgress);
                    float angle = tick * 0.5f * owner.direction;
                    Vector2 offset = new Vector2(radius, 0f).RotatedBy(angle);
                    offset.Y *= 0.75f;

                    Projectile.Center = hand + offset;
                }
                else
                {
                    // Discharge. Direction AND reach are resolved once, here, from the target's position at
                    // this instant, then frozen — so the spin is the readable tell and sidestepping after
                    // the lash fires beats it. ai[2] is the MAX reach; the lash only extends as far as it
                    // needs to, clamped into the [SpinRadius, LashReach] band.
                    if (tick == SpinTelegraphTicks + 1)
                    {
                        Player lashTarget = Main.player[owner.target];
                        Vector2 towardTarget = lashTarget.Center - hand;

                        _lashDirection = towardTarget.SafeNormalize(new Vector2(owner.direction, 0f));
                        _lashExtent = MathHelper.Clamp(towardTarget.Length(), SpinRadius, LashReach);

                        SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.65f, PitchVariance = 0.15f }, Projectile.Center);
                        OnLaunch(owner, _lashDirection * _lashExtent);
                    }

                    // Position-driven rather than velocity-driven: extension is interpolated directly, so the
                    // head stops at exactly _lashExtent instead of drifting to speed x airtime.
                    int lashTick = tick - SpinTelegraphTicks;
                    float extension;

                    if (lashTick <= LashOutTicks)
                    {
                        float outProgress = lashTick / (float)LashOutTicks;
                        extension = MathHelper.Lerp(SpinRadius, _lashExtent, outProgress);
                    }
                    else
                    {
                        float returnProgress = (lashTick - LashOutTicks) / (float)LashReturnTicks;

                        if (returnProgress >= 1f)
                        {
                            Projectile.Kill();
                            return;
                        }

                        extension = MathHelper.Lerp(_lashExtent, 0f, returnProgress);
                    }

                    Projectile.Center = hand + _lashDirection * extension;
                }

                // Zeroed for the same reason as Spin mode: this branch owns Center directly, and a non-zero
                // velocity would be applied again by the engine after AI() and double-move the head.
                Projectile.velocity = Vector2.Zero;
            }
            else
            {
                // Capture the intended launch direction on the first tick, then wind up.
                if (Projectile.localAI[0] == 1f)
                {
                    _launchVelocity = Projectile.velocity;
                }

                if (Projectile.localAI[0] <= WindupSpinTicks)
                {
                    // Brief wind-up: spin the ball around the hand on a growing radius, then release.
                    Projectile.velocity = Vector2.Zero;
                    float windupT = Projectile.localAI[0] / (float)WindupSpinTicks;
                    float radius = MathHelper.Lerp(10f, 30f, windupT);
                    float angle = Projectile.localAI[0] * 0.55f * owner.direction;
                    Vector2 offset = new Vector2(radius, 0f).RotatedBy(angle);
                    offset.Y *= 0.7f;
                    Projectile.Center = hand + offset;
                }
                else
                {
                    // Release on the first post-windup tick.
                    if (Projectile.localAI[0] == WindupSpinTicks + 1)
                    {
                        Projectile.velocity = _launchVelocity;
                        SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.55f, PitchVariance = 0.15f }, Projectile.Center);
                        OnLaunch(owner, _launchVelocity);
                    }

                    // After flying out a bit, reel back toward the hand.
                    if (Projectile.localAI[0] - WindupSpinTicks > OutwardTicks)
                    {
                        Vector2 toHand = hand - Projectile.Center;
                        if (toHand.Length() < 18f)
                        {
                            Projectile.Kill();
                            return;
                        }

                        Projectile.velocity = Vector2.Lerp(Projectile.velocity, toHand.SafeNormalize(Vector2.Zero) * ReturnSpeed, 0.18f);
                    }
                }
            }

            Projectile.rotation += Projectile.velocity.X * 0.08f + owner.direction * 0.25f;
            OnFlailTick(owner, hand);
        }

        private void TickAuthoredPattern(NPC owner, Vector2 hand)
        {
            if (_patternFacing == 0)
            {
                _patternFacing = System.Math.Sign(Projectile.velocity.X);
                if (_patternFacing == 0)
                {
                    _patternFacing = owner.direction == 0 ? 1 : owner.direction;
                }
            }

            Projectile.velocity = Vector2.Zero;
            float reach = System.Math.Abs(Projectile.ai[2]);
            float releaseStartAngle = _patternFacing < 0 ? 0f : MathHelper.Pi;
            _useAuthoredArmPose = false;

            if (Projectile.ai[2] >= 0f)
            {
                if (AuthoredPattern == EnemyFlailAttackPattern.ReverseHalo)
                {
                    TickReverseHaloTell(hand);
                    return;
                }

                if (AuthoredPattern == EnemyFlailAttackPattern.AnkleReaper)
                {
                    TickAnkleReaperTell(hand);
                    return;
                }

                if (AuthoredPattern == EnemyFlailAttackPattern.BacklashReversal)
                {
                    TickBacklashTell(hand);
                    return;
                }

                // Exactly one mirrored revolution over the 40-tick tell. Facing left turns
                // counter-clockwise on screen and finishes at 3 o'clock; facing right mirrors it.
                float tellProgress = (Projectile.localAI[0] % EnemyFlailAttackPatterns.TelegraphTicks)
                    / EnemyFlailAttackPatterns.TelegraphTicks;
                float angle = releaseStartAngle + _patternFacing * MathHelper.TwoPi * tellProgress;
                Projectile.Center = hand + new Vector2(EnemyFlailAttackPatterns.TelegraphRadius, 0f).RotatedBy(angle);
                _patternDamageActive = false;
                return;
            }

            Projectile.localAI[1]++;
            int attackTick = (int)Projectile.localAI[1];

            if (attackTick == 1)
            {
                if (!Main.dedServ)
                    SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.7f, PitchVariance = 0.12f }, Projectile.Center);
                int launchFacing = AuthoredPattern == EnemyFlailAttackPattern.BacklashReversal
                    ? -_patternFacing
                    : _patternFacing;
                OnLaunch(owner, new Vector2(launchFacing * reach, 0f));
            }

            if (AuthoredPattern == EnemyFlailAttackPattern.AdvancingCyclone)
            {
                TickAdvancingCyclone(hand, releaseStartAngle, reach, attackTick);
            }
            else if (AuthoredPattern == EnemyFlailAttackPattern.ReverseHalo)
            {
                TickLevelLash(hand, reach, attackTick, ankleReaper: false);
            }
            else if (AuthoredPattern == EnemyFlailAttackPattern.AnkleReaper)
            {
                TickLevelLash(hand, reach, attackTick, ankleReaper: true);
            }
            else if (AuthoredPattern == EnemyFlailAttackPattern.BacklashReversal)
            {
                TickBacklashReversal(hand, reach, attackTick);
            }
            else
            {
                TickDirectionalChainSwing(hand, releaseStartAngle, reach, attackTick);
            }
        }

        private void TickReverseHaloTell(Vector2 hand)
        {
            float progress = Smoother(MathHelper.Clamp(
                Projectile.localAI[0] / EnemyFlailAttackPatterns.TelegraphTicks, 0f, 1f));
            float angleFacingRight = MathHelper.TwoPi * progress;
            float worldAngle = MirrorFlailAngle(angleFacingRight);
            float radius = MathHelper.Lerp(EnemyFlailAttackPatterns.TelegraphRadius,
                EnemyFlailAttackPatterns.LevelLashOrbitRadius, progress);
            Projectile.Center = hand + new Vector2(radius, 0f).RotatedBy(worldAngle);
            SetAuthoredArm(OrbitArm(angleFacingRight));
            _patternDamageActive = false;
        }

        private void TickAnkleReaperTell(Vector2 hand)
        {
            // The first frame is exactly 3 o'clock and frame 40 returns there after a true
            // counter-clockwise screen-space revolution. Eight ticks accelerate into a non-zero
            // cruise speed, so release can continue into extension without an endpoint freeze.
            float tellTick = MathHelper.Clamp(Projectile.localAI[0] - 1f, 0f,
                EnemyFlailAttackPatterns.TelegraphTicks - 1f);
            float progress = tellTick / (EnemyFlailAttackPatterns.TelegraphTicks - 1f);
            float orbitalProgress = AccelerateThenCruise(progress,
                8f / (EnemyFlailAttackPatterns.TelegraphTicks - 1f));
            float angleFacingRight = -MathHelper.TwoPi * orbitalProgress;
            float worldAngle = MirrorFlailAngle(angleFacingRight);
            float radius = MathHelper.Lerp(EnemyFlailAttackPatterns.TelegraphRadius,
                EnemyFlailAttackPatterns.LevelLashOrbitRadius, orbitalProgress);
            Projectile.Center = hand + new Vector2(radius, 0f).RotatedBy(worldAngle);
            SetFullOrbitArm(angleFacingRight);
            _patternDamageActive = false;
        }

        private void TickBacklashTell(Vector2 hand)
        {
            float progress = Smoother(MathHelper.Clamp(
                Projectile.localAI[0] / EnemyFlailAttackPatterns.TelegraphTicks, 0f, 1f));
            float x = MathHelper.Lerp(EnemyFlailAttackPatterns.TelegraphRadius, 76f, progress);
            float y = MathHelper.Lerp(0f, 42f, progress);
            Projectile.Center = hand + FacingOffset(x, y);
            SetAuthoredArm(MathHelper.Lerp(-1.30f, -0.62f, progress));
            _patternDamageActive = false;
        }

        private void TickBacklashReversal(Vector2 hand, float reach, int attackTick)
        {
            reach = System.Math.Min(reach, EnemyFlailAttackPatterns.BacklashReach);

            if (attackTick <= EnemyFlailAttackPatterns.BacklashLoadTicks)
            {
                float load = Smoother(attackTick / (float)EnemyFlailAttackPatterns.BacklashLoadTicks);
                Projectile.Center = hand + FacingOffset(76f, 42f);
                SetAuthoredArm(MathHelper.Lerp(-0.62f, -0.92f, load));
                _patternDamageActive = false;
                return;
            }

            int whipTick = attackTick - EnemyFlailAttackPatterns.BacklashLoadTicks;
            if (whipTick <= EnemyFlailAttackPatterns.BacklashWhipTicks)
            {
                float progress = SwingEase.ApplyWeighted(0f, 1f, whipTick,
                    EnemyFlailAttackPatterns.BacklashWhipTicks, 7, 11, 7f);
                float reachScale = reach / EnemyFlailAttackPatterns.BacklashReach;
                (float x, float y) = CubicBezier(
                    76f, 42f,
                    30f, 54f,
                    -108f * reachScale, 43f,
                    -reach, 8f,
                    progress);
                Projectile.Center = hand + FacingOffset(x, y);
                SetAuthoredArm(MathHelper.Lerp(-0.92f, -2.42f, progress));
                _patternDamageActive = whipTick >= 3 && whipTick <= 17;
                return;
            }

            int retractTick = whipTick - EnemyFlailAttackPatterns.BacklashWhipTicks;
            float retractProgress = Smoother(retractTick / (float)EnemyFlailAttackPatterns.RetractTicks);
            if (retractProgress >= 1f)
            {
                Projectile.Center = hand;
                _patternDamageActive = false;
                Projectile.Kill();
                return;
            }

            Projectile.Center = hand + FacingOffset(
                MathHelper.Lerp(-reach, 0f, retractProgress),
                MathHelper.Lerp(8f, 0f, retractProgress));
            SetAuthoredArm(MathHelper.Lerp(-2.42f,
                EnemyFlailAttackPatterns.LevelLashCarryArm, retractProgress));
            _patternDamageActive = false;
        }

        private void TickLevelLash(Vector2 hand, float reach, int attackTick, bool ankleReaper)
        {
            reach = System.Math.Min(reach, EnemyFlailAttackPatterns.LevelLashReach);
            float frontAngle = _patternFacing > 0 ? 0f : MathHelper.Pi;

            if (attackTick <= EnemyFlailAttackPatterns.LevelLashTicks)
            {
                float progress;
                if (ankleReaper)
                {
                    // Non-zero initial slope: the first released frame moves immediately away from
                    // the completed orbit instead of adding a second ease-in and visible pause.
                    float t = attackTick / (float)EnemyFlailAttackPatterns.LevelLashTicks;
                    progress = 1f - (1f - t) * (1f - t);
                    SetFullOrbitArm(-MathHelper.TwoPi);
                }
                else
                {
                    progress = SwingEase.ApplyWeighted(0f, 1f, attackTick,
                        EnemyFlailAttackPatterns.LevelLashTicks, 6, 10, 6f);
                    SetAuthoredArm(MathHelper.Lerp(OrbitArm(0f), -MathHelper.PiOver2, progress));
                }

                float radius = MathHelper.Lerp(EnemyFlailAttackPatterns.LevelLashOrbitRadius,
                    reach, progress);
                Projectile.Center = hand + new Vector2(radius, 0f).RotatedBy(frontAngle);
                _patternDamageActive = attackTick >= 3;
                return;
            }

            int retractTick = attackTick - EnemyFlailAttackPatterns.LevelLashTicks;
            float retractProgress = Smoother(retractTick / (float)EnemyFlailAttackPatterns.RetractTicks);
            if (retractProgress >= 1f)
            {
                Projectile.Center = hand;
                _patternDamageActive = false;
                Projectile.Kill();
                return;
            }

            float retractRadius = MathHelper.Lerp(reach, 0f, retractProgress);
            Projectile.Center = hand + new Vector2(retractRadius, 0f).RotatedBy(frontAngle);
            if (ankleReaper)
                SetFullOrbitArm(-MathHelper.TwoPi);
            else
                SetAuthoredArm(MathHelper.Lerp(-MathHelper.PiOver2,
                    EnemyFlailAttackPatterns.LevelLashCarryArm, retractProgress));
            _patternDamageActive = false;
        }

        private float MirrorFlailAngle(float angleFacingRight)
            => _patternFacing > 0 ? angleFacingRight : MathHelper.Pi - angleFacingRight;

        private Vector2 FacingOffset(float xFacingRight, float y)
            => new Vector2(xFacingRight * _patternFacing, y);

        private void SetFullOrbitArm(float angleFacingRight)
        {
            SetAuthoredArm(angleFacingRight - MathHelper.PiOver2);
        }

        private void SetAuthoredArm(float armFacingRight)
        {
            _useAuthoredArmPose = true;
            _authoredArmFacingRight = armFacingRight;
        }

        private static float OrbitArm(float angleFacingRight)
            => -1.23f + 0.72f * (float)System.Math.Sin(angleFacingRight)
                - 0.14f * (float)System.Math.Cos(angleFacingRight);

        private static float Smoother(float value)
        {
            value = MathHelper.Clamp(value, 0f, 1f);
            return value * value * value * (value * (value * 6f - 15f) + 10f);
        }

        private static float AccelerateThenCruise(float value, float accelerationFraction)
        {
            value = MathHelper.Clamp(value, 0f, 1f);
            accelerationFraction = MathHelper.Clamp(accelerationFraction, 0.01f, 0.99f);
            float cruiseSpeed = 1f / (1f - accelerationFraction * 0.5f);
            if (value < accelerationFraction)
                return cruiseSpeed * value * value / (2f * accelerationFraction);

            return cruiseSpeed * (value - accelerationFraction * 0.5f);
        }

        private static (float x, float y) CubicBezier(
            float x0, float y0, float x1, float y1,
            float x2, float y2, float x3, float y3, float progress)
        {
            progress = MathHelper.Clamp(progress, 0f, 1f);
            float inverse = 1f - progress;
            float inverseSquared = inverse * inverse;
            float progressSquared = progress * progress;
            return (
                inverseSquared * inverse * x0
                    + 3f * inverseSquared * progress * x1
                    + 3f * inverse * progressSquared * x2
                    + progressSquared * progress * x3,
                inverseSquared * inverse * y0
                    + 3f * inverseSquared * progress * y1
                    + 3f * inverse * progressSquared * y2
                    + progressSquared * progress * y3);
        }

        private void TickDirectionalChainSwing(Vector2 hand, float releaseStartAngle, float reach, int attackTick)
        {
            if (attackTick <= EnemyFlailAttackPatterns.ArcSwingTicks)
            {
                float progress = attackTick / (float)EnemyFlailAttackPatterns.ArcSwingTicks;
                float eased = progress * progress * (3f - 2f * progress);
                float direction = AuthoredPattern == EnemyFlailAttackPattern.OverheadChain
                    ? _patternFacing
                    : -_patternFacing;
                float angle = releaseStartAngle + direction * MathHelper.PiOver2 * eased;
                float radius = MathHelper.Lerp(EnemyFlailAttackPatterns.TelegraphRadius, reach, eased);
                Projectile.Center = hand + new Vector2(radius, 0f).RotatedBy(angle);
                _patternDamageActive = true;
                return;
            }

            int retractTick = attackTick - EnemyFlailAttackPatterns.ArcSwingTicks;
            float retractProgress = retractTick / (float)EnemyFlailAttackPatterns.RetractTicks;
            if (retractProgress >= 1f)
            {
                Projectile.Center = hand;
                _patternDamageActive = false;
                Projectile.Kill();
                return;
            }

            float finalDirection = AuthoredPattern == EnemyFlailAttackPattern.OverheadChain
                ? _patternFacing
                : -_patternFacing;
            float finalAngle = releaseStartAngle + finalDirection * MathHelper.PiOver2;
            float retractRadius = MathHelper.Lerp(reach, 0f, retractProgress);
            Projectile.Center = hand + new Vector2(retractRadius, 0f).RotatedBy(finalAngle);
            _patternDamageActive = false;
        }

        private void TickAdvancingCyclone(Vector2 hand, float releaseStartAngle, float reach, int attackTick)
        {
            if (attackTick <= EnemyFlailAttackPatterns.CycloneLiveTicks)
            {
                float completedRotations = attackTick / (float)EnemyFlailAttackPatterns.RotationTicks;
                float angle = releaseStartAngle + _patternFacing * MathHelper.TwoPi * completedRotations;
                float expansion = MathHelper.Clamp(
                    attackTick / (float)EnemyFlailAttackPatterns.RotationTicks, 0f, 1f);
                float radius = MathHelper.Lerp(EnemyFlailAttackPatterns.TelegraphRadius, reach,
                    expansion * expansion * (3f - 2f * expansion));
                Projectile.Center = hand + new Vector2(radius, 0f).RotatedBy(angle);
                _patternDamageActive = true;
                return;
            }

            int retractTick = attackTick - EnemyFlailAttackPatterns.CycloneLiveTicks;
            float retractProgress = retractTick / (float)EnemyFlailAttackPatterns.RetractTicks;
            if (retractProgress >= 1f)
            {
                Projectile.Center = hand;
                _patternDamageActive = false;
                Projectile.Kill();
                return;
            }

            float rotations = EnemyFlailAttackPatterns.MaximumRadiusRotations + 1f
                + retractTick / (float)EnemyFlailAttackPatterns.RotationTicks;
            float angleDuringRetract = releaseStartAngle + _patternFacing * MathHelper.TwoPi * rotations;
            float radiusDuringRetract = MathHelper.Lerp(reach, 0f, retractProgress);
            Projectile.Center = hand + new Vector2(radiusDuringRetract, 0f).RotatedBy(angleDuringRetract);
            _patternDamageActive = false;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            NPC owner = Owner;
            if (owner == null || !owner.active)
            {
                return true;
            }

            Texture2D chainTexture = ModContent.Request<Texture2D>(ChainTexturePath).Value;
            Vector2 mountedCenter = GetHandAnchor(owner);
            Vector2 center = Projectile.Center;
            Vector2 distToProj = mountedCenter - center;
            float projRotation = distToProj.ToRotation() - MathHelper.PiOver2;
            float distance = distToProj.Length();

            while (distance > 20f && !float.IsNaN(distance))
            {
                distToProj.Normalize();
                distToProj *= chainTexture.Height;
                center += distToProj;
                distToProj = mountedCenter - center;
                distance = distToProj.Length();

                Main.EntitySpriteDraw(
                    chainTexture,
                    center - Main.screenPosition,
                    null,
                    lightColor,
                    projRotation,
                    chainTexture.Size() * 0.5f,
                    0.97f,
                    SpriteEffects.None,
                    0);
            }

            Texture2D ballTexture = TextureAssets.Projectile[Type].Value;
            Main.EntitySpriteDraw(
                ballTexture,
                Projectile.Center - Main.screenPosition,
                null,
                lightColor,
                Projectile.rotation,
                ballTexture.Size() * 0.5f,
                Projectile.scale * 0.97f,
                SpriteEffects.None,
                0);

            return false;
        }
    }
}

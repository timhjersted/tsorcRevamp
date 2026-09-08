using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy.Weapons
{
    /// <summary>
    /// Shared "ball + chain" flail projectile for AI enemies: no puppet-player rendering, no arm-swing animation —
    /// the chain+head projectile owns 100% of the visual, drawn from an owner NPC's hand anchor. Two motion modes
    /// selected via ai[1]: Spin (orbits the anchor) or Throw (brief wind-up spin, launches on the initial velocity,
    /// then reels back and self-destructs once close). ai[0] is the owner NPC's whoAmI.
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

        /// <summary>Target extension (px) for a SpinThenLash discharge, set by the owner via ai[2].</summary>
        protected float LashReach => Projectile.ai[2];

        private const int WindupSpinTicks = 9;
        private Vector2 _launchVelocity;
        private Vector2 _lashDirection;
        private float _lashExtent;

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

            if (SpinMode)
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

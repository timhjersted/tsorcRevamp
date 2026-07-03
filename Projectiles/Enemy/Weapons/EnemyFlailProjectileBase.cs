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

        private const int WindupSpinTicks = 9;
        private Vector2 _launchVelocity;

        public override void SetDefaults()
        {
            Projectile.width = 26;
            Projectile.height = 26;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 54;
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
                    if (Projectile.localAI[0] - WindupSpinTicks > 18f)
                    {
                        Vector2 toHand = hand - Projectile.Center;
                        if (toHand.Length() < 18f)
                        {
                            Projectile.Kill();
                            return;
                        }

                        Projectile.velocity = Vector2.Lerp(Projectile.velocity, toHand.SafeNormalize(Vector2.Zero) * 13f, 0.18f);
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

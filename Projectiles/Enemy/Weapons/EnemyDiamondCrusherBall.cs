using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy.Weapons
{
    public class EnemyDiamondCrusherBall : ModProjectile
    {
        private const string ChainTexturePath = "tsorcRevamp/Projectiles/Melee/Flails/DiamondCrusherChain";

        private NPC Owner => Projectile.ai[0] >= 0 && Projectile.ai[0] < Main.maxNPCs ? Main.npc[(int)Projectile.ai[0]] : null;
        private bool SpinMode => Projectile.ai[1] == 1f;

        // Every non-spin throw does a brief wind-up spin around the hand before release.
        private const int WindupSpinTicks = 9;
        private Vector2 _launchVelocity;

        public override string Texture => "tsorcRevamp/Projectiles/Melee/Flails/DiamondCrusherBall";

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

        public override void AI()
        {
            NPC owner = Owner;
            if (owner == null || !owner.active)
            {
                Projectile.Kill();
                return;
            }

            Projectile.localAI[0]++;
            Vector2 hand = owner.Center + new Vector2(owner.direction * 12f, -owner.height * 0.2f);

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
        }

        public override bool PreDraw(ref Color lightColor)
        {
            NPC owner = Owner;
            if (owner == null || !owner.active)
            {
                return true;
            }

            Texture2D chainTexture = ModContent.Request<Texture2D>(ChainTexturePath).Value;
            Vector2 mountedCenter = owner.Center + new Vector2(owner.direction * 12f, -owner.height * 0.2f);
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

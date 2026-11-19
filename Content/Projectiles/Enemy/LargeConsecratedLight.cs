using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;

namespace tsorcRevamp.Content.Projectiles.Enemy
{
    // Uses EnemyConsecratedLight's host-following sweep; ai[1] is the casting NPC.
    // Its 90x246 sheet has three-times-size sections painted on a 2x2 pixel grid.
    public class LargeConsecratedLight : EnemyConsecratedLight
    {
        public const int SizeMultiplier = 3;
        private const float DamageWidth = 22f * SizeMultiplier;

        public override void SetDefaults()
        {
            base.SetDefaults();

            Projectile.width *= SizeMultiplier;
            Projectile.height *= SizeMultiplier;
            LaserSize = SizeMultiplier;
            LaserName = nameof(LargeConsecratedLight);

            LaserTextureHead = new Rectangle(0, 0, 90, 72);
            LaserTextureBody = new Rectangle(0, 78, 90, 90);
            LaserTextureTail = new Rectangle(0, 174, 90, 72);
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (FiringTimeLeft <= 0 || !IsAtMaxCharge || TargetingMode != 0)
            {
                return false;
            }

            float collisionPoint = 0f;
            Vector2 origin = GetOrigin();
            Vector2 endpoint = origin + Projectile.velocity * Distance;

            return Collision.CheckAABBvLineCollision(
                targetHitbox.TopLeft(), targetHitbox.Size(), origin, endpoint, DamageWidth, ref collisionPoint);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (!additiveContext)
            {
                return false;
            }

            timeFactor++;

            if ((IsAtMaxCharge && TargetingMode == 0) || TargetingMode == 2)
            {
                if (FiringTimeLeft < FadeOutFrames)
                {
                    fadePercent = (float)FiringTimeLeft / FadeOutFrames;
                }
                else
                {
                    fadePercent = MathHelper.Clamp(fadePercent + FadeInSpeed, 0f, 1f);
                }
            }
            else if (TelegraphTime + Charge >= MaxCharge || TargetingMode == 1)
            {
                fadePercent = 0.2f + Math.Abs((float)Math.Cos(timeFactor / 30f)) * 0.2f;
            }
            else
            {
                fadePercent = 0f;
            }

            // Draw the actual enlarged texture at native scale so each block stays 2x2.
            // GenericLaser's additive pass supplies point sampling and beam segmentation.
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Vector2 origin = GetOrigin();
            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitY);
            Color beamColor = Color.White * fadePercent;

            DrawLaser(
                texture, origin, direction,
                LaserTextureHead, LaserTextureBody, LaserTextureTail,
                -MathHelper.PiOver2, Projectile.scale, beamColor);

            return false;
        }
    }
}

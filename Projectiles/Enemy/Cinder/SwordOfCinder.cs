using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy.Cinder
{
    class SwordOfCinder : ModProjectile
    {
        internal const float SwordLength = 220f;
        internal const float ImageOffsetDegrees = 45f;
        internal const float ImageOffsetRadians = 0.7853f;

        public override void SetDefaults()
        {
            Projectile.width = 32;
            Projectile.height = 32;
            Projectile.friendly = false;
            Projectile.hostile = true;
            Projectile.timeLeft = 225;
            Projectile.tileCollide = false;
            Projectile.light = 0.7f;
            Projectile.penetrate = 1000;
        }

        internal int OwnerIndex => (int)Projectile.ai[0];
        internal int Timer => 225 - Projectile.timeLeft;

        public override void AI()
        {
            Projectile.friendly = false;

            if (OwnerIndex < 0 || OwnerIndex >= Main.maxNPCs || !Main.npc[OwnerIndex].active)
            {
                Projectile.Kill();
                return;
            }

            NPC owner = Main.npc[OwnerIndex];
            if (Timer == 0)
            {
                Projectile.rotation -= ImageOffsetRadians;
                for (int i = 0; i < 80; i++)
                {
                    Vector2 dustPosition = Projectile.Center + new Vector2(0, SwordLength * (i / 80f)).RotatedBy(Projectile.rotation + MathHelper.ToRadians(180 + ImageOffsetDegrees));
                    Dust.NewDust(dustPosition, 1, 1, DustID.Torch);
                }
            }
            else
            {
                Projectile.Center = owner.Center;
                Vector2 facing = owner.velocity.LengthSquared() > 0.01f ? owner.velocity : new Vector2(owner.direction == 0 ? 1 : owner.direction, 0f);
                Projectile.rotation = -facing.ToRotation() - ImageOffsetRadians;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = (Texture2D)Terraria.GameContent.TextureAssets.Projectile[Projectile.type];
            Rectangle sourceRectangle = new Rectangle(0, 0, texture.Width, texture.Height);
            Vector2 origin = new Vector2(0, texture.Height);
            Color drawColor = Projectile.GetAlpha(lightColor);
            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition + new Vector2(0f, Projectile.gfxOffY), sourceRectangle, drawColor, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0);
            return false;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float point = 0f;
            return Collision.CheckAABBvLineCollision(
                targetHitbox.TopLeft(),
                targetHitbox.Size(),
                Projectile.Center,
                Projectile.Center + new Vector2(0, SwordLength).RotatedBy(Projectile.rotation + MathHelper.ToRadians(180 + ImageOffsetDegrees)),
                32,
                ref point);
        }
    }
}

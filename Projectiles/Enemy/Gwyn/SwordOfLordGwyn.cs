using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;


namespace tsorcRevamp.Projectiles.Enemy.Gwyn
{
    class SwordOfLordGwyn : ModProjectile
    {
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

        internal float AI_Owner
        {
            get => Projectile.ai[0];
            set => Projectile.ai[0] = value;
        }

        internal float AI_Rotation
        {
            get => Projectile.ai[1];
            set => Projectile.ai[1] = value;
        }

        internal int AI_Timer
        {
            get => 225 - Projectile.timeLeft;
        }

        internal const float SWORD_LENGTH = 220;
        internal const float IMG_OFFSET_DEG = 45f;
        internal const float IMG_OFFSET_RAD = 0.7853f;

        public override void AI()
        {
            Projectile.friendly = false;
            if (AI_Timer == 0)
            {
                Projectile.rotation -= IMG_OFFSET_RAD;
                for (int i = 0; i < 100; i++)
                {
                    Dust.NewDust(Projectile.Center + new Vector2(0, SWORD_LENGTH * (i / 100f)).RotatedBy(Projectile.rotation + MathHelper.ToRadians(180 + IMG_OFFSET_DEG)), 1, 1, DustID.Clentaminator_Red);
                }
                Main.NewText("spawning dust");
            }

            else
            {
                Projectile.Center = Main.npc[(int)AI_Owner].Center;
                Projectile.rotation = (-Main.npc[(int)AI_Owner].velocity.ToRotation()) - IMG_OFFSET_RAD;
                Main.NewText("" + (int)AI_Owner);
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            int frameWidth = (Texture2D)Terraria.GameContent.TextureAssets.Projectile[Projectile.type].Width;
            int frameHeight = ((Texture2D)Terraria.GameContent.TextureAssets.Projectile[Projectile.type]).Height;
            Rectangle sourceRectangle = new Rectangle(0, 0, frameWidth, frameHeight);
            Vector2 origin = new Vector2(0, frameHeight);
            Color drawColor = Projectile.GetAlpha(lightColor);
            Main.EntitySpriteDraw((Texture2D)Terraria.GameContent.TextureAssets.Projectile[Projectile.type],
                Projectile.Center - Main.screenPosition + new Vector2(0f, Projectile.gfxOffY),
                sourceRectangle, drawColor, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0f);

            return false;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float point = 0f;
            return Collision.CheckAABBvLineCollision(
                targetHitbox.TopLeft(),
                targetHitbox.Size(),
                Projectile.Center,
                Projectile.Center + (new Vector2(0, SWORD_LENGTH)).RotatedBy(Projectile.rotation + MathHelper.ToRadians(180 + IMG_OFFSET_DEG)),
                32,
                ref point);
        }
    }
}

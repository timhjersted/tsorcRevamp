using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Enums;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;


namespace tsorcRevamp.Projectiles.Enemy.Gwyn {
    class SwordOfLordGwyn : ModProjectile {
        public override void SetDefaults() {
            projectile.width = 32;
            projectile.height = 32;
            projectile.friendly = false;
            projectile.hostile = true;
            projectile.timeLeft = 225;
            projectile.tileCollide = false;
            projectile.light = 0.7f;
            projectile.penetrate = 1000;
        }

        internal float AI_Owner {
            get => projectile.ai[0];
            set => projectile.ai[0] = value;
        }

        internal float AI_Rotation {
            get => projectile.ai[1];
            set => projectile.ai[1] = value;
        }

        internal int AI_Timer {
            get => 225 - projectile.timeLeft;
        }

        internal const float SWORD_LENGTH = 220;
        internal const float IMG_OFFSET_DEG = 45f;
        internal const float IMG_OFFSET_RAD = 0.7853f;

        public override void AI() {
            projectile.friendly = false;
            if (AI_Timer == 0) {
                projectile.rotation -= IMG_OFFSET_RAD;
                for (int i = 0; i < 100; i++) {
                    Dust.NewDust(projectile.Center + new Vector2(0, SWORD_LENGTH * (i / 100f)).RotatedBy(projectile.rotation + MathHelper.ToRadians(180 + IMG_OFFSET_DEG)), 1, 1, DustID.Clentaminator_Red);
                }
                Main.NewText("spawning dust");
            }
            
            else {
                projectile.Center = Main.npc[(int)AI_Owner].Center;
                projectile.rotation = (-Main.npc[(int)AI_Owner].velocity.ToRotation()) - IMG_OFFSET_RAD;
                Main.NewText("" + (int)AI_Owner);
            }
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor) {
            int frameWidth = Main.projectileTexture[projectile.type].Width;
            int frameHeight = Main.projectileTexture[projectile.type].Height;
            Rectangle sourceRectangle = new Rectangle(0, 0, frameWidth, frameHeight);
            Vector2 origin = new Vector2(0, frameHeight);
            Color drawColor = projectile.GetAlpha(lightColor);
            Main.spriteBatch.Draw(Main.projectileTexture[projectile.type],
                projectile.Center - Main.screenPosition + new Vector2(0f, projectile.gfxOffY),
                sourceRectangle, drawColor, projectile.rotation, origin, projectile.scale, SpriteEffects.None, 0f);

            return false;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
            float point = 0f;
            return Collision.CheckAABBvLineCollision(
                targetHitbox.TopLeft(),
                targetHitbox.Size(),
                projectile.Center,
                projectile.Center + (new Vector2(0, SWORD_LENGTH)).RotatedBy(projectile.rotation + MathHelper.ToRadians(180 + IMG_OFFSET_DEG)),
                32,
                ref point);
        }
    }
}

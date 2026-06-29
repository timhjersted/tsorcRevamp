using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Projectiles.VFX;

namespace tsorcRevamp.Projectiles.Enemy.OolacileSorcerer
{
    public class OccultistStar : ModProjectile
    {
        public override void SetDefaults()
        {
            Projectile.CloneDefaults(ProjectileID.Bullet);
            AIType = ProjectileID.Bullet;
            Projectile.width = 22;
            Projectile.height = 22;
            Projectile.light = 0.8f;
            Projectile.hostile = true;
            Projectile.hide = true;
        }

        public bool accel = true;

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 1;
        }

        float teleAlpha = 0.5f;
        public override bool PreDraw(ref Color lightColor)
        {
            if (!accel)
            {
                return true;
            }
            Texture2D texture = ModContent.Request<Texture2D>("tsorcRevamp/NPCs/Bosses/SuperHardMode/telegraphtelegraph").Value;

            Vector2 start = Projectile.Center - Main.screenPosition;
            Vector2 end = (Projectile.Center + Projectile.velocity.SafeNormalize(Vector2.UnitY) * 6000f) - Main.screenPosition;

            float rotation = (end - start).ToRotation();
            Vector2 scale = new Vector2(Vector2.Distance(start, end) / texture.Width, 0.0025f);

            Main.spriteBatch.Draw(texture, start, null, Color.DarkRed * teleAlpha, rotation, texture.Size() * Vector2.UnitY * 0.5f, scale, SpriteEffects.None, 0f);

            return true;
        }

        public override void AI()
        {
            /*if (Projectile.frameCounter < 5)
                Projectile.frame = 0;
            else if (Projectile.frameCounter >= 5 && Projectile.frameCounter < 10)
                Projectile.frame = 1;
            else if (Projectile.frameCounter >= 10 && Projectile.frameCounter < 15)
                Projectile.frame = 2;
            else if (Projectile.frameCounter >= 15 && Projectile.frameCounter < 20)
                Projectile.frame = 3;
            else
                Projectile.frameCounter = 0;
            Projectile.frameCounter++;*/

            if (accel)
            {
                Projectile.velocity *= 1.02f;
            }
            teleAlpha -= 0.0025f;

            if (Main.rand.NextBool(2))
            {
                int dust = Dust.NewDust(new Vector2((float)Projectile.position.X, (float)Projectile.position.Y), Projectile.width, Projectile.height, DustID.YellowStarDust, 0, 0, 50, Color.White, 1.0f);
                Main.dust[dust].noGravity = false;
            }
        }
    }
}
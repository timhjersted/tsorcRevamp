using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy.DarkCloud
{
    class DarkCloudDragoonLance : ModProjectile
    {
        public override void SetDefaults()
        {
            Projectile.width = 14;
            Projectile.height = 161;
            //projectile.aiStyle = 19;
            //projectile.timeLeft = 700;
            //projectile.penetrate = 12;
            Projectile.hostile = true;
            Projectile.tileCollide = false;
        }

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Dragoon Lance");
        }

        public override void AI()
        {
            Lighting.AddLight(Projectile.Center, Color.Cyan.ToVector3());
            if (Projectile.ai[0] > 0)
            {
                Projectile.ai[0]--;
                if (Projectile.ai[0] == 0)
                {
                    Projectile.velocity = new Vector2(-1, -30);
                }
                else
                {
                    Projectile.velocity = new Vector2(0, -1);
                }
            }
        }

        #region Kill
        public override void Kill(int timeLeft)
        {
            if (!Projectile.active)
            {
                int num40 = Dust.NewDust(new Vector2(Projectile.position.X + Projectile.velocity.X, Projectile.position.Y + Projectile.velocity.Y), Projectile.width, Projectile.height, 15, Projectile.velocity.X, Projectile.velocity.Y, 100, default(Color), 3f);
                return;
            }
            Projectile.timeLeft = 0;
            {
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Dig, Projectile.Center);
                for (int i = 0; i < 10; i++)
                {
                    int dust = Dust.NewDust(new Vector2(Projectile.position.X + Projectile.velocity.X, Projectile.position.Y + Projectile.velocity.Y), Projectile.width, Projectile.height, 15, Projectile.velocity.X, Projectile.velocity.Y, 200, default(Color), 1f);
                    Main.dust[dust].noGravity = false;
                    Dust.NewDust(new Vector2(Projectile.position.X + Projectile.velocity.X, Projectile.position.Y + Projectile.velocity.Y), Projectile.width, Projectile.height, 15, Projectile.velocity.X, Projectile.velocity.Y, 200, default(Color), 2f);
                    Dust.NewDust(new Vector2(Projectile.position.X + Projectile.velocity.X, Projectile.position.Y + Projectile.velocity.Y), Projectile.width, Projectile.height, 15, Projectile.velocity.X, Projectile.velocity.Y, 200, default(Color), 1f);
                    Dust.NewDust(new Vector2(Projectile.position.X + Projectile.velocity.X, Projectile.position.Y + Projectile.velocity.Y), Projectile.width, Projectile.width, 15, Projectile.velocity.X, Projectile.velocity.Y, 200, default(Color), 1f);
                    Dust.NewDust(new Vector2(Projectile.position.X + Projectile.velocity.X, Projectile.position.Y + Projectile.velocity.Y), Projectile.width, Projectile.width, 15, Projectile.velocity.X, Projectile.velocity.Y, 200, default(Color), 1f);
                    Dust.NewDust(new Vector2(Projectile.position.X + Projectile.velocity.X, Projectile.position.Y + Projectile.velocity.Y), Projectile.width, Projectile.height, 15, Projectile.velocity.X, Projectile.velocity.Y, 200, default(Color), 2f);
                    Dust.NewDust(new Vector2(Projectile.position.X + Projectile.velocity.X, Projectile.position.Y + Projectile.velocity.Y), Projectile.width, Projectile.height, 15, Projectile.velocity.X, Projectile.velocity.Y, 200, default(Color), 1f);
                    Dust.NewDust(new Vector2(Projectile.position.X + Projectile.velocity.X, Projectile.position.Y + Projectile.velocity.Y), Projectile.width, Projectile.width, 15, Projectile.velocity.X, Projectile.velocity.Y, 200, default(Color), 1f);
                    Dust.NewDust(new Vector2(Projectile.position.X + Projectile.velocity.X, Projectile.position.Y + Projectile.velocity.Y), Projectile.width, Projectile.width, 15, Projectile.velocity.X, Projectile.velocity.Y, 200, default(Color), 1f);
                }
            }
            Projectile.active = false;
        }
        #endregion

        static Texture2D texture;
        public override bool PreDraw(ref Color lightColor)
        {
            UsefulFunctions.DrawSimpleLitProjectile(Projectile, ref texture);
            return false;
        }
    }
}
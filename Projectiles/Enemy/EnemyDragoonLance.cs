using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ModLoader;
using Terraria.ID;

namespace tsorcRevamp.Projectiles.Enemy
{
    class EnemyDragoonLance : ModProjectile
    {
        public override void SetDefaults()
        {
            Projectile.width = 19;
            Projectile.height = 161;
            Projectile.aiStyle = 19;
            Projectile.timeLeft = 700;
            Projectile.penetrate = 12;
            Projectile.hostile = true;
            Projectile.tileCollide = true;
        }

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Dragoon Lance");
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
                Terraria.Audio.SoundEngine.PlaySound(0, (int)Projectile.position.X, (int)Projectile.position.Y, 1);
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
    }
}
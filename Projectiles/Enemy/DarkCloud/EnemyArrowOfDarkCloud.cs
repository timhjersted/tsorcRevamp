using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ModLoader;
using Terraria.ID;

namespace tsorcRevamp.Projectiles.Enemy.DarkCloud
{
    class EnemyArrowOfDarkCloud : ModProjectile
    {
        public override void SetDefaults()
        {
            Projectile.width = 10;
            Projectile.height = 5;
            Projectile.hostile = true;
            Projectile.penetrate = 2;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.tileCollide = false;
        }
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Arrow of Dark Cloud");
        }

        
        public override void AI()
        {
            //Secret forbidden color unlocked: Green 3
            Lighting.AddLight(Projectile.Center, Color.SeaGreen.ToVector3() * 3);
            Projectile.velocity.Y += 0.05f;
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
        }

        static Texture2D texture;
        public override bool PreDraw(ref Color lightColor)
        {
            UsefulFunctions.DrawSimpleLitProjectile(Projectile, ref texture);
            return false;
        }

        #region Kill
        public override void Kill(int timeLeft)
        {
            //int num98 = -1;
            if (!Projectile.active)
            {
                return;


            }
            Projectile.timeLeft = 0;
            {
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Dig, Projectile.Center);
                for (int i = 0; i < 10; i++)
                {
                    Vector2 arg_92_0 = new Vector2(Projectile.position.X, Projectile.position.Y);
                    int arg_92_1 = Projectile.width;
                    int arg_92_2 = Projectile.height;
                    int arg_92_3 = 7;
                    float arg_92_4 = 0f;
                    float arg_92_5 = 0f;
                    int arg_92_6 = 0;
                    Color newColor = default(Color);
                    Dust.NewDust(arg_92_0, arg_92_1, arg_92_2, arg_92_3, arg_92_4, arg_92_5, arg_92_6, newColor, 1f);

                }
            }
            Projectile.active = false;
        }
        #endregion       
    }
}
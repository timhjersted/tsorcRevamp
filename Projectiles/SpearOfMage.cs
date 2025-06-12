using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;


namespace tsorcRevamp.Projectiles
{
    class SpearOfMage : ModProjectile
    {
        public override void SetStaticDefaults()
        {
        }
        public override void SetDefaults()
        {
            Projectile.width = 40;
            Projectile.height = 40;
            Projectile.timeLeft = 120;
            Projectile.light = 0.5f;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.ownerHitCheck = false;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.tileCollide = false;
            Projectile.scale = 1f;
            Projectile.penetrate = 2;
        }

        public override void AI()
        {
            Projectile.ai[0] += 1f;
            if (++Projectile.frameCounter >= 3)
            {
                Projectile.frameCounter = 0;
                if (++Projectile.frame >= 3)
                {
                    Projectile.frame = 0;
                }
            }
            if (Projectile.ai[0] >= 15f)
            { //this is the bit that makes it arc down
                Projectile.ai[0] = 15f;
                Projectile.velocity.Y += 0.1f;
            }
            if (Projectile.velocity.Y > 16f)
            { //this bit caps down velocity, and thus also caps down rotation if fired at a positive angle
                Projectile.velocity.Y = 16f;
            }
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.ToRadians(90f); //simplified rotation code (no trig!)

            Color color = new Color();
            int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, 242, 0f, 0f, 80, color, 1f);
            Main.dust[dust].noGravity = true;
        }

        public override void OnKill(int timeLeft)
        {
            if (!Projectile.active)
            {
                return;
            }
            Projectile.timeLeft = 0;
            {
                for (int i = 0; i < 10; i++)
                {
                    Vector2 arg_92_0 = new Vector2(Projectile.position.X, Projectile.position.Y);
                    int arg_92_1 = Projectile.width;
                    int arg_92_2 = Projectile.height;
                    int arg_92_3 = 242;
                    float arg_92_4 = 0f;
                    float arg_92_5 = 0f;
                    int arg_92_6 = 0;
                    Color newColor = default(Color);
                    Dust.NewDust(arg_92_0, arg_92_1, arg_92_2, arg_92_3, arg_92_4, arg_92_5, arg_92_6, newColor, 1f);
                }
            }
            Projectile.active = false;
        }
    }
}

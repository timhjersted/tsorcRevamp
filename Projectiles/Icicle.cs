using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles {
    class Icicle : ModProjectile {
        public override void SetDefaults() {
            //projectile.aiStyle = 9;
            projectile.friendly = true;
            projectile.penetrate = 3;
            projectile.height = 38;
            projectile.width = 38;
            projectile.magic = true;
            projectile.tileCollide = false;
            projectile.timeLeft = 70;
            projectile.alpha = 70;
        }

        public override void AI()
        {
            projectile.rotation = projectile.velocity.ToRotation() + MathHelper.ToRadians(225f);

            if (projectile.timeLeft <= 15)
            {
                projectile.alpha += 9;
            }

            Vector2 arg_2675_0 = new Vector2(projectile.position.X, projectile.position.Y);
            int arg_2675_1 = projectile.width;
            int arg_2675_2 = projectile.height;
            int arg_2675_3 = 15;
            float arg_2675_4 = 0f;
            float arg_2675_5 = 0f;
            int arg_2675_6 = 100;
            Color newColor = default(Color);
            if (Main.rand.Next(2) == 0)
            {
                int num47 = Dust.NewDust(arg_2675_0, arg_2675_1, arg_2675_2, arg_2675_3, arg_2675_4, arg_2675_5, arg_2675_6, newColor, 2f);
                Dust expr_2684 = Main.dust[num47];
                expr_2684.velocity *= 0.3f;

                Main.dust[num47].position.X = projectile.position.X + (float)(projectile.width / 2) + 4f + (float)Main.rand.Next(-4, 5);
                Main.dust[num47].position.Y = projectile.position.Y + (float)(projectile.height / 2) + (float)Main.rand.Next(-4, 5);
                Main.dust[num47].noGravity = true;
            }

            int n1337 = Dust.NewDust(arg_2675_0, arg_2675_1, arg_2675_2, 172, arg_2675_4, arg_2675_5, arg_2675_6, newColor, 2f);


            for (int i = 0; i < 2; i++)
            {
                Main.dust[n1337].position.X = projectile.position.X + (float)(projectile.width / 2) + 4f + (float)Main.rand.Next(-4, 5);
                Main.dust[n1337].position.Y = projectile.position.Y + (float)(projectile.height / 2) + (float)Main.rand.Next(-4, 5);
                Main.dust[n1337].noGravity = true;
                Main.dust[n1337].velocity *= 0.8f;
            }
        }
    }
}

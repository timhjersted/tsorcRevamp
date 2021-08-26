using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles {
    class PoisonBombBall : ModProjectile {

        public override string Texture => "tsorcRevamp/Projectiles/PoisonFieldBall";
        public override void SetDefaults()
        {
            projectile.friendly = true;
            projectile.height = 16;
            projectile.width = 16;
            projectile.light = 0.3f;
            projectile.magic = true;
            projectile.penetrate = 1;
            projectile.tileCollide = true;
        }

        public override void AI()
        {
            if (projectile.soundDelay == 0 && Math.Abs(projectile.velocity.X) + Math.Abs(projectile.velocity.Y) > 2f)
            {
                projectile.soundDelay = 10;
                Main.PlaySound(SoundID.Item, (int)projectile.position.X, (int)projectile.position.Y, 9);
            }
            for (int d = 0; d < 2; d++)
            {
                int dust = Dust.NewDust(new Vector2(projectile.position.X, projectile.position.Y), projectile.width, projectile.height, DustID.Poisoned, 0, 0, 100, default(Color), 1.3f);
                Main.dust[dust].noGravity = true;
                Main.dust[dust].velocity *= .5f;
                Main.dust[dust].fadeIn = 0.3f;
            }

            Vector2 arg_2675_0 = new Vector2(projectile.position.X, projectile.position.Y);
            int arg_2675_1 = projectile.width;
            int arg_2675_2 = projectile.height;
            int arg_2675_3 = 74;
            float arg_2675_4 = 0f;
            float arg_2675_5 = 0f;
            int arg_2675_6 = 100;
            Color newColor = default(Color);
            int num47 = Dust.NewDust(arg_2675_0, arg_2675_1, arg_2675_2, arg_2675_3, arg_2675_4, arg_2675_5, arg_2675_6, newColor, 1f);
            Dust expr_2684 = Main.dust[num47];
            expr_2684.velocity *= 0.3f;
            Main.dust[num47].position.X = projectile.position.X + (float)(projectile.width / 2) + 4f + (float)Main.rand.Next(-4, 5);
            Main.dust[num47].position.Y = projectile.position.Y + (float)(projectile.height / 2) + (float)Main.rand.Next(-4, 5);
            Main.dust[num47].noGravity = true;

            if (projectile.velocity.Y > 16f)
            {
                projectile.velocity.Y = 16f;
                return;
            }
        }

        public override void Kill(int timeLeft)
        {
            if (!projectile.active)
            {
                return;
            }
            projectile.timeLeft = 0;
            {
                Main.PlaySound(SoundID.Item, (int)projectile.position.X, (int)projectile.position.Y, 10);
                Projectile.NewProjectile(projectile.position.X + projectile.width, projectile.position.Y + projectile.height, 0, 0, ModContent.ProjectileType<PoisonField2>(), projectile.damage, 1f, projectile.owner);
                Projectile.NewProjectile(projectile.position.X + projectile.width * 4, projectile.position.Y + projectile.height, 0, 0, ModContent.ProjectileType<PoisonField2>(), projectile.damage, 1f, projectile.owner);
                Projectile.NewProjectile(projectile.position.X + projectile.width * -2, projectile.position.Y + projectile.height, 0, 0, ModContent.ProjectileType<PoisonField2>(), projectile.damage, 1f, projectile.owner);
                Projectile.NewProjectile(projectile.position.X + projectile.width, projectile.position.Y + projectile.height * 4, 0, 0, ModContent.ProjectileType<PoisonField2>(), projectile.damage, 1f, projectile.owner);
                Projectile.NewProjectile(projectile.position.X + projectile.width * 4, projectile.position.Y + projectile.height * 4, 0, 0, ModContent.ProjectileType<PoisonField2>(), projectile.damage, 1f, projectile.owner);
                Projectile.NewProjectile(projectile.position.X + projectile.width * -2, projectile.position.Y + projectile.height * 4, 0, 0, ModContent.ProjectileType<PoisonField2>(), projectile.damage, 1f, projectile.owner);
                Projectile.NewProjectile(projectile.position.X + projectile.width, projectile.position.Y + projectile.height * -2, 0, 0, ModContent.ProjectileType<PoisonField2>(), projectile.damage, 1f, projectile.owner);
                Projectile.NewProjectile(projectile.position.X + projectile.width * 4, projectile.position.Y + projectile.height * -2, 0, 0, ModContent.ProjectileType<PoisonField2>(), projectile.damage, 1f, projectile.owner);
                Projectile.NewProjectile(projectile.position.X + projectile.width * -2, projectile.position.Y + projectile.height * -2, 0, 0, ModContent.ProjectileType<PoisonField2>(), projectile.damage, 1f, projectile.owner);
            
            for (int num40 = 0; num40 < 40; num40++)
                {
                    Vector2 arg_1394_0 = new Vector2(projectile.position.X + projectile.velocity.X, projectile.position.Y + projectile.velocity.Y);
                    int arg_1394_1 = projectile.width;
                    int arg_1394_2 = projectile.height;
                    int arg_1394_3 = 74;
                    float arg_1394_4 = 0f;
                    float arg_1394_5 = 0f;
                    int arg_1394_6 = 100;
                    Color newColor = default;
                    int num41 = Dust.NewDust(arg_1394_0, arg_1394_1, arg_1394_2, arg_1394_3, arg_1394_4, arg_1394_5, arg_1394_6, newColor, 1f);
                    Main.dust[num41].noGravity = true;
                    Dust expr_13B1 = Main.dust[num41];
                    expr_13B1.velocity *= 1.2f;
                }
            }
            if (projectile.owner == Main.myPlayer)
            {
                if (Main.netMode != NetmodeID.SinglePlayer)
                {
                    NetMessage.SendData(MessageID.KillProjectile, -1, -1, null, projectile.identity, (float)projectile.owner, 0f, 0f, 0);
                }
            }
            projectile.active = false;
        }
    }
}

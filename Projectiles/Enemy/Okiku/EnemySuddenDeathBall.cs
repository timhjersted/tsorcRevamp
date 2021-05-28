using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy.Okiku {
    class EnemySuddenDeathBall : ModProjectile {

        public override void SetDefaults() {
            projectile.width = 24;
            projectile.height = 38;
            projectile.aiStyle = 1;
            projectile.hostile = true;
            projectile.penetrate = 1;
            projectile.magic = true;
            projectile.light = 1;
            projectile.tileCollide = true;
            projectile.ignoreWater = true;
        }

        public override void SetStaticDefaults() {
            DisplayName.SetDefault("Sudden Death Ball");
        }
        #region AI
        public override void AI() {
            if (projectile.ai[1] == 0f) {
                projectile.ai[1] = 1f;
                Main.PlaySound(SoundID.Item, (int)projectile.position.X, (int)projectile.position.Y, 9);
            }
            projectile.rotation = (float)Math.Atan2((double)projectile.velocity.Y, (double)projectile.velocity.X) + 1.57f;
            if (projectile.velocity.Y > 16f) {
                projectile.velocity.Y = 16f;
                return;
            }
        }
        #endregion

        #region Kill
        public override void Kill(int timeLeft) {

            Main.PlaySound(SoundID.Item, (int)projectile.position.X, (int)projectile.position.Y, 10);
            if (projectile.owner == Main.myPlayer) Projectile.NewProjectile(projectile.position.X + (float)(projectile.width / 2), projectile.position.Y + (float)(projectile.height - 16), 0, 0, ModContent.ProjectileType<EnemySuddenDeathStrike>(), 0, 3f, projectile.owner);
            Vector2 arg_1394_0 = new Vector2(projectile.position.X - projectile.velocity.X, projectile.position.Y - projectile.velocity.Y);
            int arg_1394_1 = projectile.width;
            int arg_1394_2 = projectile.height;
            int arg_1394_3 = 15;
            float arg_1394_4 = 0f;
            float arg_1394_5 = 0f;
            int arg_1394_6 = 100;
            int num41 = Dust.NewDust(arg_1394_0, arg_1394_1, arg_1394_2, arg_1394_3, arg_1394_4, arg_1394_5, arg_1394_6, default, 2f);
            Main.dust[num41].noGravity = true;
            Dust expr_13B1 = Main.dust[num41];
            expr_13B1.velocity *= 2f;
            Vector2 arg_1422_0 = new Vector2(projectile.position.X - projectile.velocity.X, projectile.position.Y - projectile.velocity.Y);
            int arg_1422_1 = projectile.width;
            int arg_1422_2 = projectile.height;
            int arg_1422_3 = 15;
            float arg_1422_4 = 0f;
            float arg_1422_5 = 0f;
            int arg_1422_6 = 100;
            Dust.NewDust(arg_1422_0, arg_1422_1, arg_1422_2, arg_1422_3, arg_1422_4, arg_1422_5, arg_1422_6, default, 1f);

            projectile.active = false;
        }
        #endregion
    }
}

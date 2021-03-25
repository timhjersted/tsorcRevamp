using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles {
    class EphemeralThrowingAxe : ModProjectile {

        public override void SetDefaults() {
            projectile.aiStyle = 2;
            projectile.friendly = true;
            projectile.height = 22;
            projectile.penetrate = 1;
            projectile.melee = true;
            projectile.tileCollide = false;
            projectile.width = 22;
            projectile.timeLeft = 50;
        }

        public override void AI() {
            Color color = new Color();
            int dust = Dust.NewDust(projectile.position, projectile.width, projectile.height, 57, 0f, 0f, 80, color, 1f);
            Main.dust[dust].noGravity = true;
        }
        public override void Kill(int timeLeft) {

            if (!projectile.active) {
                return;
            }
            projectile.timeLeft = 0;
            {
                Main.PlaySound(SoundID.Dig, (int)projectile.position.X, (int)projectile.position.Y, 1);
                for (int i = 0; i < 10; i++) {
                    Vector2 arg_92_0 = new Vector2(projectile.position.X, projectile.position.Y);
                    int arg_92_1 = projectile.width;
                    int arg_92_2 = projectile.height;
                    int arg_92_3 = 7;
                    float arg_92_4 = 0f;
                    float arg_92_5 = 0f;
                    int arg_92_6 = 0;
                    Color newColor = default(Color);
                    Dust.NewDust(arg_92_0, arg_92_1, arg_92_2, arg_92_3, arg_92_4, arg_92_5, arg_92_6, newColor, 1f);
                }
            }
            projectile.active = false;

        }
    }
}

using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles {
    class EphemeralThrowingAxe : ModProjectile {

        public override string Texture => "tsorcRevamp/Items/Weapons/Melee/EphemeralThrowingAxe";

        public override void SetDefaults() {
            Projectile.aiStyle = 2;
            Projectile.friendly = true;
            Projectile.height = 22;
            Projectile.penetrate = 4;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.tileCollide = false;
            Projectile.width = 22;
            Projectile.timeLeft = 50;
        }

        public override void AI() {
            Color color = new Color();
            int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, 57, 0f, 0f, 80, color, 1f);
            Main.dust[dust].noGravity = true;
        }
        public override void Kill(int timeLeft) {

            if (!Projectile.active) {
                return;
            }
            Projectile.timeLeft = 0;
            {
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Dig, (int)Projectile.position.X, (int)Projectile.position.Y, 1);
                for (int i = 0; i < 10; i++) {
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
    }
}

using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;

namespace tsorcRevamp.Projectiles.Melee.Spears
{
    class LonginusThrown : ModdedSpearProjectileThrown
    {
        public override void OnKill(int timeLeft)
        {
            if (Projectile.ai[0] == 1)
            {
                for (int i = 0; i < 150; i++)
                {
                    Vector2 direction = Main.rand.NextVector2Circular(1f, 1f).SafeNormalize(Vector2.UnitX);
                    float speed = Main.rand.NextFloat(5.5f, 19f);

                    int dust1 = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, 90, 0f, 0f, 70, default, 1.55f);
                    Main.dust[dust1].velocity = direction * speed;
                    Main.dust[dust1].noGravity = true;

                    int dust2 = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, 219, 0f, 0f, 70, default, 1.95f);
                    Main.dust[dust2].velocity = direction * (speed * 1.2f);
                    Main.dust[dust2].noGravity = true;
                }
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item14, Projectile.position);

                Projectile.penetrate = 15;
                Vector2 oldCenter = Projectile.Center;
                Projectile.width = 320;
                Projectile.height = 320;
                Projectile.position = oldCenter - new Vector2(Projectile.width / 2f, Projectile.height / 2f);
                Projectile.damage /= 2;
                Projectile.Damage();
            }
        }
    }
}
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;

namespace tsorcRevamp.Projectiles.Ranged.Ammo
{
    public class DragoonRocket : ModProjectile
    {
        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            Projectile.aiStyle = 1;
            Projectile.width = 18;
            Projectile.height = 18;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 600;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 60;
            Projectile.extraUpdates = 1;
            AIType = ProjectileID.Bullet; 
        }

        public override void AI()
        {
            int dust = Dust.NewDust(
                Projectile.position,
                Projectile.width,
                Projectile.height,
                228,
                Projectile.velocity.X * -0.3f,
                Projectile.velocity.Y * -0.3f,
                150,
                default,
                2.2f
            );
            Main.dust[dust].noGravity = true;

            int dust2 = Dust.NewDust(
                Projectile.Center,
                8, 
                8, 
                228, 
                Projectile.velocity.X * 0.3f, 
                Projectile.velocity.Y * 0.3f,
                0, 
                default, 
                2f 
            );
            Main.dust[dust2].noGravity = true;
            Main.dust[dust2].velocity *= 0.2f;

            int dust3 = Dust.NewDust(
                Projectile.Center - new Vector2(2, 2),
                8, 
                8, 
                31, 
                Projectile.velocity.X * 0.2f, 
                Projectile.velocity.Y * 0.2f,
                0, 
                default, 
                1.3f 
            );
            Main.dust[dust3].noGravity = true;
            Main.dust[dust3].velocity *= 0.2f;

            Lighting.AddLight(Projectile.Center, 0.8f, 0.6f, 0.1f);
        }

        public override void OnKill(int timeLeft)
        {
            for (int i = 0; i < 50; i++)
            {
                int dust = Dust.NewDust(
                    Projectile.position,
                    Projectile.width,
                    Projectile.height,
                    228,
                    Main.rand.NextFloat(-30f, 30f),
                    Main.rand.NextFloat(-30f, 30f),
                    100,
                    default,
                    1.8f
                );

                Main.dust[dust].noGravity = true;
            }

            Collision.HitTiles(Projectile.position + Projectile.velocity, Projectile.velocity, Projectile.width, Projectile.height);
            SoundEngine.PlaySound(SoundID.Item62, Projectile.position);
        }
    }
}

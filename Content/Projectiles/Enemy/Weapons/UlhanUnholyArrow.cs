using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Projectiles.Enemy.Weapons
{
    public class UlhanUnholyArrow : ModProjectile
    {
        private bool _hitGround;
        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.UnholyArrow;

        public override void SetDefaults()
        {
            Projectile.CloneDefaults(ProjectileID.UnholyArrow);
            Projectile.friendly = false;
            Projectile.hostile = true;
            Projectile.DamageType = DamageClass.Ranged;
            AIType = ProjectileID.UnholyArrow;
            Projectile.timeLeft = 600;
        }

        public override void AI()
        {
            if (Projectile.timeLeft < 20)
                Projectile.alpha = (20 - Projectile.timeLeft) * 255 / 20;
            if (Main.dedServ)
                return;
            if (Main.GameUpdateCount % 3 == 0)
            {
                Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.DemonTorch,
                    -Projectile.velocity * 0.08f, 100, default, 0.6f);
                dust.noGravity = true;
            }
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            _hitGround = oldVelocity.Y > 0f && Projectile.velocity.Y != oldVelocity.Y;
            return true;
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.dedServ)
                return;
            Collision.HitTiles(Projectile.position, Projectile.velocity, Projectile.width, Projectile.height);
            SoundEngine.PlaySound(SoundID.Dig, Projectile.Center);
            if (_hitGround)
            {
                for (int i = 0; i < 18; i++)
                {
                    Dust.NewDustPerfect(Projectile.Bottom + Main.rand.NextVector2Circular(7f, 2f),
                        i % 4 == 0 ? DustID.Stone : DustID.Dirt,
                        Main.rand.NextVector2Circular(2.2f, 1.5f) - Vector2.UnitY,
                        80, default, Main.rand.NextFloat(0.7f, 1.1f));
                    // Dirt and stone fragments fall back to the terrain.
                }
            }
            for (int i = 0; i < 24; i++)
            {
                Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.DemonTorch,
                    Main.rand.NextVector2Circular(2.5f, 2.5f), 100, default, 0.7f);
                dust.noGravity = true;
            }
        }
    }
}

using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy.Weapons
{
    public class EnemyTaintedArrow : ModProjectile
    {
        public override string Texture => "tsorcRevamp/Projectiles/Ranged/Ammo/TaintedArrowProjectile";

        public override void SetDefaults()
        {
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.friendly = false;
            Projectile.hostile = true;
            Projectile.penetrate = 1;
            Projectile.tileCollide = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.aiStyle = 1;
            AIType = ProjectileID.WoodenArrowFriendly;
            Projectile.timeLeft = 600;
        }

        public override void AI()
        {
            Lighting.AddLight(Projectile.Center, 0.2f, 0.5f, 0.2f);

            if (Main.rand.NextBool(5))
            {
                Dust dust = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.Poisoned,
                    Projectile.velocity.X * 0.2f, Projectile.velocity.Y * 0.2f, 100, default, 1f);
                dust.velocity *= 0.5f;
            }
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(BuffID.Poisoned, 3 * 60);
        }

        public override void OnKill(int timeLeft)
        {
            Collision.HitTiles(Projectile.position + Projectile.velocity, Projectile.velocity, Projectile.width, Projectile.height);
            SoundEngine.PlaySound(SoundID.Dig, Projectile.position);
        }
    }
}

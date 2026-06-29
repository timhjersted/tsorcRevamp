using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy.Weapons
{
    /// <summary>
    /// Hostile purple venom fang fired in a shotgun spread by an invader's enemy Venom Staff.  Straight
    /// flight, applies the Venom debuff on hit.  Visuals borrow the vanilla VenomFang sprite + a
    /// DustID.VenomStaff purple trail.
    /// </summary>
    public class EnemyVenomStaffProj : ModProjectile
    {
        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.VenomBullet;

        public override void SetDefaults()
        {
            Projectile.width = 14;
            Projectile.height = 14;
            Projectile.friendly = false;
            Projectile.hostile = true;
            Projectile.tileCollide = true;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 120;
            Projectile.ignoreWater = true;
            Projectile.light = 0.4f;
            Projectile.DamageType = DamageClass.Magic;
        }

        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation();
            if (Main.rand.NextBool())
            {
                int d = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.VenomStaff,
                    Projectile.velocity.X * 0.2f, Projectile.velocity.Y * 0.2f, 100, default, 1.1f);
                Main.dust[d].noGravity = true;
            }
            Lighting.AddLight(Projectile.Center, 0.3f, 0.05f, 0.4f);
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(BuffID.Venom, 600); // 10 s venom
        }

        public override void OnKill(int timeLeft)
        {
            for (int i = 0; i < 6; i++)
            {
                int d = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.VenomStaff,
                    Main.rand.NextFloat(-2f, 2f), Main.rand.NextFloat(-2f, 2f), 100, default, 1.2f);
                Main.dust[d].noGravity = true;
            }
        }
    }
}

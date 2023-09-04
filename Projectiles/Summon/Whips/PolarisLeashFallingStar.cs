using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;

namespace tsorcRevamp.Projectiles.Summon.Whips
{

	public class PolarisLeashFallingStar : ModProjectile
	{
        public override void SetStaticDefaults()
        {
        }
        public override void SetDefaults()
		{

			Projectile.CloneDefaults(ProjectileID.FallingStar);
			Projectile.friendly = true;
			Projectile.width = 22;
			Projectile.height = 18;
			Projectile.tileCollide = false;
			Projectile.DamageType = DamageClass.SummonMeleeSpeed;
			Projectile.penetrate = 3;
			Projectile.usesLocalNPCImmunity = true;
			Projectile.localNPCHitCooldown = 60;
		}
		public override void AI()
		{
			Dust.NewDust(Projectile.Center, Projectile.width, Projectile.height, 15, 0f, 0f, 10, Color.AliceBlue, 0.5f);
		}
	}
}

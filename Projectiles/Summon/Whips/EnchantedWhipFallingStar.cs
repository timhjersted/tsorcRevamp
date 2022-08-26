using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;

namespace tsorcRevamp.Projectiles.Summon.Whips
{

	public class EnchantedWhipFallingStar : ModProjectile
	{

		public override void SetDefaults()
		{

			Projectile.CloneDefaults(ProjectileID.FallingStar);

			AIType = ProjectileID.Starfury;
			Projectile.tileCollide = false;
			Projectile.DamageType = DamageClass.SummonMeleeSpeed;
			Projectile.penetrate += 2;
		}
        public override void AI()
        {
            base.AI();
			Dust.NewDust(Projectile.Center, Projectile.height, Projectile.width, 57, 0f, 0f, 10, Color.AliceBlue, 0.5f);
		}
    }
}

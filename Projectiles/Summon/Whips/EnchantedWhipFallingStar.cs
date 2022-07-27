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
			Projectile.penetrate += 3;
		}
        public override void AI()
        {
            base.AI();
			Dust.NewDust(Projectile.Center, 50, 50, 57, 0f, 0f, 150, Color.AliceBlue, 1f);
		}
    }
}

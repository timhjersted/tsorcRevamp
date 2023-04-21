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
			Projectile.friendly = true;
			Projectile.tileCollide = false;
            Projectile.width = 22;
            Projectile.height = 24;
            Projectile.DamageType = DamageClass.SummonMeleeSpeed;
			Projectile.penetrate = 2;
			Projectile.usesIDStaticNPCImmunity = true;
			Projectile.idStaticNPCHitCooldown = 15;
		}
        public override void AI()
        {
			Dust.NewDust(Projectile.Center, Projectile.height, Projectile.width, 57, 0f, 0f, 10, Color.AliceBlue, 0.5f);
		}
    }
}

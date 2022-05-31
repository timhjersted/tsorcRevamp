using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
	class EnemySpellGreatEnergyBeam : ModProjectile
	{

		public override void SetStaticDefaults()
		{
			DisplayName.SetDefault("Enemy Spell Great Energy Beam");

		}
		public override void SetDefaults()
		{
			Projectile.height = 40;
			Projectile.width = 350;
			Main.projFrames[Projectile.type] = 17;
			Projectile.aiStyle = -1;
			Projectile.hostile = true;
			Projectile.scale = 1;
			Projectile.DamageType = DamageClass.Magic;
			Projectile.light = 1;
			Projectile.tileCollide = true;
			Projectile.ignoreWater = true;
			Projectile.timeLeft = 360;
			Projectile.penetrate = 50;
		}
		#region AI
		public override void AI()
		{
			Projectile.frameCounter++;
			if (Projectile.frameCounter > 2)
			{
				Projectile.frame++;
				Projectile.frameCounter = 0;
			}
			if (Projectile.frame >= 17)
			{
				Projectile.Kill();
				return;
			}
		}
		#endregion

	}
}
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
			projectile.height = 40;
			projectile.width = 350;
			Main.projFrames[projectile.type] = 17;
			projectile.aiStyle = -1;
			projectile.hostile = true;
			projectile.scale = 1;
			projectile.magic = true;
			projectile.light = 1;
			projectile.tileCollide = true;
			projectile.ignoreWater = true;
			projectile.timeLeft = 360;
			projectile.penetrate = 50;
		}
		#region AI
		public override void AI()
		{
			projectile.frameCounter++;
			if (projectile.frameCounter > 2)
			{
				projectile.frame++;
				projectile.frameCounter = 0;
			}
			if (projectile.frame >= 17)
			{
				projectile.Kill();
				return;
			}
		}
		#endregion

	}
}
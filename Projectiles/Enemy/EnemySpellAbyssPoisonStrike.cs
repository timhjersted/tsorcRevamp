using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
	class EnemySpellAbyssPoisonStrike : ModProjectile
	{
		public override void SetDefaults()
		{
			projectile.width = 26;
			projectile.height = 40;
			Main.projFrames[projectile.type] = 5;
			projectile.aiStyle = 4;
			projectile.hostile = true;
			projectile.magic = true;
			projectile.light = 1;
			projectile.tileCollide = true;
			projectile.ignoreWater = true;
			projectile.penetrate = 50;
		}

		#region AI
		public override void AI()
		{
			projectile.frameCounter++;
			if (projectile.frameCounter > 3)
			{
				projectile.frame++;
				projectile.frameCounter = 0;
			}
			if (projectile.frame >= 5)
			{
				projectile.Kill();
				return;
			}
		}
		#endregion
	}
}
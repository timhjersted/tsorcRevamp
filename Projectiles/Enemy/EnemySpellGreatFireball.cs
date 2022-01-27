using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
	class EnemySpellGreatFireball : ModProjectile
	{

		public override void SetStaticDefaults()
		{
			DisplayName.SetDefault("Enemy Spell Great Fireball");

		}
		public override void SetDefaults()
		{
			projectile.width = 150;
			projectile.height = 150;
			Main.projFrames[projectile.type] = 9;
			projectile.aiStyle = -1;
			projectile.hostile = true;
			projectile.scale = 2;
			projectile.magic = true;
			projectile.light = 1;
			projectile.tileCollide = true;
			projectile.ignoreWater = true;
			projectile.timeLeft = 360;
			projectile.penetrate = 50;
			drawOriginOffsetX = -75;
			drawOriginOffsetY = 70;
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
			if (projectile.frame >= 9)
			{
				projectile.Kill();
				return;
			}
		}
		#endregion
	}
}
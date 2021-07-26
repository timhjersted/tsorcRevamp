using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
	class EnemySpellEffectHealing : ModProjectile
	{
		public override void SetDefaults()
		{
			projectile.width = 52;
			projectile.height = 44;
			Main.projFrames[projectile.type] = 5;
			projectile.aiStyle = 1;
			projectile.hostile = true;
			projectile.penetrate = 50;
			projectile.scale = 1.2f;
			projectile.magic = true;
			projectile.light = 1;
			projectile.tileCollide = false;
			projectile.ignoreWater = true;
		}
		#region AI
		public override void AI()
		{
			projectile.frameCounter++;
			if (projectile.frameCounter > 5)
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
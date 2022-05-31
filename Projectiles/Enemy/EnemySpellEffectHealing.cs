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
			Projectile.width = 52;
			Projectile.height = 44;
			Main.projFrames[Projectile.type] = 5;
			Projectile.aiStyle = 1;
			Projectile.hostile = true;
			Projectile.penetrate = 50;
			Projectile.scale = 1.2f;
			Projectile.DamageType = DamageClass.Magic;
			Projectile.light = 1;
			Projectile.tileCollide = false;
			Projectile.ignoreWater = true;
		}
		#region AI
		public override void AI()
		{
			Projectile.frameCounter++;
			if (Projectile.frameCounter > 5)
			{
				Projectile.frame++;
				Projectile.frameCounter = 0;
			}
			if (Projectile.frame >= 5)
			{
				Projectile.Kill();
				return;
			}
		}
		#endregion
	}
}
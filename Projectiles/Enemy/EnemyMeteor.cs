using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
	class EnemyMeteor : ModProjectile
	{
		public override void SetDefaults()
		{
			projectile.aiStyle = 9;
			projectile.hostile = true;
			projectile.friendly = false;
			projectile.height = 16;
			projectile.light = 0.5f;
			projectile.magic = true;
			projectile.penetrate = 1;
			projectile.timeLeft = 45;
			projectile.tileCollide = true;
			projectile.width = 16;
		}
		public override bool PreKill(int timeLeft)
        {
			projectile.type = 29;
			return true;
        }
	}
}
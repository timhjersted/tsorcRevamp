using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
	class EnemyGreatAttack : ModProjectile
	{

		public override void SetStaticDefaults()
		{
			DisplayName.SetDefault("Enemy Great Attack");

		}
		public override void SetDefaults()
		{
			projectile.aiStyle = 2;
			projectile.hostile = true;
			projectile.height = 5;
			projectile.light = 1f;
			projectile.melee = true;
			projectile.penetrate = 4;
			projectile.scale = 1;
			projectile.tileCollide = true;
			projectile.width = 5;
			projectile.alpha = 200;
		}
		public override bool PreKill(int timeLeft)
        {
			projectile.type = 102;
			return true;
        }
	}
}
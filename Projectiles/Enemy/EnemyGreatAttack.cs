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
			Projectile.aiStyle = 2;
			Projectile.hostile = true;
			Projectile.height = 5;
			Projectile.light = 1f;
			Projectile.DamageType = DamageClass.Melee;
			Projectile.penetrate = 4;
			Projectile.scale = 1;
			Projectile.tileCollide = true;
			Projectile.width = 5;
			Projectile.alpha = 200;
			Projectile.timeLeft = 20;
		}
		public override bool PreKill(int timeLeft)
        {
			Projectile.type = 102;
			//Main.PlaySound(2, (int)projectile.position.X, (int)projectile.position.Y, 125, 0.3f, .2f); //phantasmal bolt fire 2
			return true;
        }
	}
}
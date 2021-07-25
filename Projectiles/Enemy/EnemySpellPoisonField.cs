using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
	public class EnemySpellPoisonField : ModProjectile
	{
		public override void SetDefaults()
		{

			projectile.width = 26;
			projectile.height = 40;
			Main.projFrames[projectile.type] = 5;
			projectile.aiStyle = 4;
			projectile.hostile = true;
			projectile.damage = 60;
			projectile.magic = true;
			projectile.light = 1;
			projectile.tileCollide = true;
			projectile.ignoreWater = true;
			projectile.timeLeft = 260;
			projectile.penetrate = 50;
		}

		public override void OnHitPlayer(Player target, int damage, bool crit)
		{
			target.AddBuff(BuffID.Poisoned, 360);
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
				projectile.frame = 0;
				return;
			}
		}
		#endregion

	}
}
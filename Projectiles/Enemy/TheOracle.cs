using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
	class TheOracle : ModProjectile
	{

		public override void SetDefaults()
		{
			projectile.damage = 166;
			projectile.aiStyle = ProjectileID.CrystalShard;
			projectile.friendly = false;
			projectile.hostile = true;
			projectile.tileCollide = false;
			projectile.height = 16;
			projectile.width = 16;
			projectile.timeLeft = 250;

		}
		public override void AI()
		{
			projectile.rotation += 1f;
			if (Main.rand.Next(4) == 0)
			{
				int dust = Dust.NewDust(new Vector2((float)projectile.position.X, (float)projectile.position.Y), projectile.width, projectile.height, 6, 0, 0, 50, Color.Green, 3.0f);
				Main.dust[dust].noGravity = false;
			}
			Lighting.AddLight(projectile.position, 0.4f, 0.1f, 0.1f);

			if (projectile.velocity.X <= 4 && projectile.velocity.Y <= 4 && projectile.velocity.X >= -4 && projectile.velocity.Y >= -4)
			{
				float accel = 1f + (Main.rand.Next(10, 30) * 0.001f);
				projectile.velocity.X *= accel;
				projectile.velocity.Y *= accel;
			}

			Rectangle projrec = new Rectangle((int)projectile.position.X + (int)projectile.velocity.X, (int)projectile.position.Y + (int)projectile.velocity.Y, projectile.width, projectile.height);
			Rectangle prec = new Rectangle((int)Main.player[Main.myPlayer].position.X, (int)Main.player[Main.myPlayer].position.Y, (int)Main.player[Main.myPlayer].width, (int)Main.player[Main.myPlayer].height);
		}
		public override void OnHitPlayer(Player target, int damage, bool crit)
		{
				target.AddBuff(BuffID.Battle, 600);
				target.AddBuff(BuffID.BrokenArmor, 300);
				target.AddBuff(BuffID.Poisoned, 3600);
				target.AddBuff(BuffID.Bleeding, 7200);
		}

	}
}

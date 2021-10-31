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
			projectile.friendly = false;
			projectile.hostile = true;
			projectile.tileCollide = false;
			projectile.height = 16;
			projectile.width = 16;
			projectile.timeLeft = 250;

		}
		public override void AI()
		{
			projectile.rotation += .5f;
			if (Main.rand.Next(4) == 0)
			{
				int dust = Dust.NewDust(new Vector2((float)projectile.position.X, (float)projectile.position.Y), projectile.width, projectile.height, 6, 0, 0, 50, Color.Green, 3.0f);
				Main.dust[dust].noGravity = false;
			}
			Lighting.AddLight(projectile.position, 0.5f, 0.6f, 0.1f);

			if (projectile.velocity.X <= 4 && projectile.velocity.Y <= 4 && projectile.velocity.X >= -4 && projectile.velocity.Y >= -4)
			{
				float accel = 1f + (Main.rand.Next(10, 30) * 0.001f);
				projectile.velocity.X *= accel;
				projectile.velocity.Y *= accel;
			}
		}
		public override bool PreKill(int timeLeft)
		{
			projectile.type = ProjectileID.Grenade;
			return true;
		}
		public override void OnHitPlayer(Player target, int damage, bool crit)
		{
			int buffLengthMod = 1;
			if (Main.expertMode)
			{
				buffLengthMod = 2;
			}
			target.AddBuff(BuffID.Battle, 600 / buffLengthMod);
			target.AddBuff(BuffID.BrokenArmor, 300 / buffLengthMod);
			target.AddBuff(BuffID.Poisoned, 600 / buffLengthMod);
			target.AddBuff(BuffID.Bleeding, 600 / buffLengthMod);
		}

	}
}

using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
	public class OolacileDarkOrb : ModProjectile
	{
		public override void SetStaticDefaults()
		{
			DisplayName.SetDefault("Oolacile's Dark Orb");
		}

		public override void SetDefaults()
		{
			projectile.aiStyle = 0;
			projectile.hostile = true;
			projectile.height = 34;
			projectile.scale = 2f;
			projectile.tileCollide = false;
			projectile.width = 34;
			projectile.timeLeft = 600;
			Main.projFrames[projectile.type] = 4;
			projectile.light = 1;
		}

		public override void AI()
		{

			projectile.rotation += 0.5f;

			if (Main.player[(int)projectile.ai[0]].position.X < projectile.position.X)
			{
				if (projectile.velocity.X > -10) projectile.velocity.X -= 0.1f;
			}

			if (Main.player[(int)projectile.ai[0]].position.X > projectile.position.X)
			{
				if (projectile.velocity.X < 10) projectile.velocity.X += 0.1f;
			}

			if (Main.player[(int)projectile.ai[0]].position.Y < projectile.position.Y)
			{
				if (projectile.velocity.Y > -10) projectile.velocity.Y -= 0.1f;
			}

			if (Main.player[(int)projectile.ai[0]].position.Y > projectile.position.Y)
			{
				if (projectile.velocity.Y < 10) projectile.velocity.Y += 0.1f;
			}

			if (Main.rand.Next(2) == 0)
			{
				int dust = Dust.NewDust(new Vector2((float)projectile.position.X, (float)projectile.position.Y), projectile.width, projectile.height, 27, 0, 0, 50, Color.Purple, 1.0f);
				Main.dust[dust].noGravity = false;
			}
			Lighting.AddLight((int)(projectile.position.X / 16f), (int)(projectile.position.Y / 16f), 0.7f, 0.2f, 0.2f);

			projectile.frameCounter++;
			if (projectile.frameCounter > 2)
			{
				projectile.frame++;
				projectile.frameCounter = 3;
			}
			if (projectile.frame >= 4)
			{
				projectile.frame = 0;
			}
		}

        public override bool PreKill(int timeLeft)
        {
			projectile.type = 44;
            return base.PreKill(timeLeft);
        }

        public override void OnHitPlayer(Player target, int damage, bool crit)
        {
            if (Main.expertMode)
            {
				target.AddBuff(BuffID.Poisoned, 9000, false);
				target.AddBuff(BuffID.Darkness, 9000, false);
				target.AddBuff(BuffID.Bleeding, 9000, false);
			} else
			{
				target.AddBuff(BuffID.Poisoned, 18000, false);
				target.AddBuff(BuffID.Darkness, 18000, false);
				target.AddBuff(BuffID.Bleeding, 18000, false);
			}
			target.AddBuff(ModContent.BuffType<Buffs.Attraction>(), 3600, false);
		}
	}
}
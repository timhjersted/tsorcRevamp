using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.GameContent.Achievements;

namespace tsorcRevamp.Projectiles.Enemy
{
	class HypnoticDisrupter : ModProjectile
	{

		public override void SetDefaults()
		{
			//projectile.aiStyle = 18;
			projectile.width = 16;
			projectile.height = 16;
			projectile.penetrate = 1;
			projectile.hostile = true;
			projectile.timeLeft = 600;
			projectile.scale = 1f;
			projectile.tileCollide = false;
			projectile.light = 1;
		}

		public override void SetStaticDefaults()
		{
			DisplayName.SetDefault("Hypnotic Disrupter");
		}

		public override void AI()
		{
			if(projectile.ai[1] > 0 && projectile.timeLeft > 300)
            {
				projectile.timeLeft = 300;
				projectile.tileCollide = true;
            }
			projectile.rotation += 3f;

			if (Main.player[(int)projectile.ai[0]].position.X < projectile.position.X)
			{
				if (projectile.velocity.X > -10) projectile.velocity.X -= 0.1f;
			}

			if (Main.player[(int)projectile.ai[0]].position.X > projectile.position.X)
			{
				if (projectile.velocity.X < 10) projectile.velocity.X += 0.2f;
			}

			if (Main.player[(int)projectile.ai[0]].position.Y < projectile.position.Y)
			{
				if (projectile.velocity.Y > -10) projectile.velocity.Y -= 0.1f;
			}

			if (Main.player[(int)projectile.ai[0]].position.Y > projectile.position.Y)
			{
				if (projectile.velocity.Y < 10) projectile.velocity.Y += 0.1f;
			}


			Color color = new Color();
			int dust = Dust.NewDust(new Vector2((float)projectile.position.X, (float)projectile.position.Y - 10), projectile.width, projectile.height, DustID.Shadowflame, 0, 0, 50, color, 3.0f);
			Main.dust[dust].noGravity = true;

			if (Main.rand.Next(2) == 0)
			{
				Lighting.AddLight((int)(projectile.position.X / 16f), (int)(projectile.position.Y / 16f), 0.7f, 0.2f, 0.2f);
			}
		}
		public override void OnHitPlayer(Player target, int damage, bool crit)
		{
			int buffLengthMod = 1;
			if (!Main.expertMode) //surely that was the wrong way round
			{
				buffLengthMod = 2;
			}

			target.AddBuff(BuffID.Bleeding, 600 / buffLengthMod, false); //bleeding
			target.AddBuff(ModContent.BuffType<Buffs.BrokenSpirit>(), 600 / buffLengthMod, false); //you take knockback

			if (projectile.ai[1] < 1)
			{
				target.AddBuff(ModContent.BuffType<Buffs.Crippled>(), 1800 / buffLengthMod, false);
			}
		}

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            return true;
        }
    }
}
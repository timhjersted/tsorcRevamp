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
			projectile.timeLeft = 2600;
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

			if (Main.rand.Next(4) == 0)
			{
				Lighting.AddLight((int)(projectile.position.X / 16f), (int)(projectile.position.Y / 16f), 0.7f, 0.2f, 0.2f);
			}
			Rectangle projrec = new Rectangle((int)projectile.position.X + (int)projectile.velocity.X, (int)projectile.position.Y + (int)projectile.velocity.Y, projectile.width, projectile.height);
			Rectangle prec = new Rectangle((int)Main.player[Main.myPlayer].position.X, (int)Main.player[Main.myPlayer].position.Y, (int)Main.player[Main.myPlayer].width, (int)Main.player[Main.myPlayer].height);

			if (projrec.Intersects(prec))
			{
				Main.player[Main.myPlayer].AddBuff(BuffID.Confused, 60, false); //confused
				Main.player[Main.myPlayer].AddBuff(BuffID.Bleeding, 600, false); //bleeding
				Main.player[Main.myPlayer].AddBuff(ModContent.BuffType<Buffs.BrokenSpirit>(), 600, false); //you take knockback

			}

		}
	}
}
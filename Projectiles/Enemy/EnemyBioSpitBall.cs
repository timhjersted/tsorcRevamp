using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
	class EnemyBioSpitBall : ModProjectile
	{
		public override void SetDefaults()
		{
			aiType = 96;
			projectile.aiStyle = 1;
			projectile.hostile = true;
			projectile.height = 24;
			projectile.light = 1;
			projectile.magic = true;
			projectile.penetrate = 1; //was 8
			projectile.tileCollide = true;
			projectile.width = 24;
			projectile.timeLeft = 70;
		}

		public override bool PreKill(int timeLeft)
		{
			projectile.type = 96; //killpretendtype
			return true;
		}

		public override void AI()
			{
				//if (projectile.soundDelay == 0 && Math.Abs(projectile.velocity.X) + Math.Abs(projectile.velocity.Y) > 2f)
				//{
				//	projectile.soundDelay = 10;
				Main.PlaySound(2, (int)projectile.position.X, (int)projectile.position.Y, 17);
				//}

				projectile.rotation += 1f;
				if (Main.rand.Next(2) == 0)
				{
					int dust = Dust.NewDust(new Vector2((float)projectile.position.X, (float)projectile.position.Y), projectile.width, projectile.height, 6, 0, 0, 50, Color.Green, 3.0f);
					Main.dust[dust].noGravity = false;
				}
				Lighting.AddLight((int)(projectile.position.X / 16f), (int)(projectile.position.Y / 16f), 0.4f, 0.1f, 0.1f);

				if (projectile.velocity.X <= 4 && projectile.velocity.Y <= 4 && projectile.velocity.X >= -4 && projectile.velocity.Y >= -4)
				{
					float accel = 1f + (Main.rand.Next(10, 30) * 0.001f);
					projectile.velocity.X *= accel;
					projectile.velocity.Y *= accel;
				}

			#region commented out ???
			//Vector2 arg_2675_0 = new Vector2(projectile.position.X, projectile.position.Y);
			//int arg_2675_1 = projectile.width;
			//int arg_2675_2 = projectile.height;
			//int arg_2675_3 = 15;
			//float arg_2675_4 = 0f;
			//float arg_2675_5 = 0f;
			//int arg_2675_6 = 100;
			////Color newColor = default(Color);
			////int num47 = Dust.NewDust(arg_2675_0, arg_2675_1, arg_2675_2, arg_2675_3, arg_2675_4, arg_2675_5, arg_2675_6, newColor, 2f);
			/////Dust expr_2684 = Main.dust[num47];expr_2684.velocity *= 0.3f;
			////Main.dust[num47].position.X = projectile.position.X + (float)(projectile.width / 2) + 4f + (float)Main.rand.Next(-4, 5);
			////Main.dust[num47].position.Y = projectile.position.Y + (float)(projectile.height / 2) + (float)Main.rand.Next(-4, 5);
			////Main.dust[num47].noGravity = true;
			//if (Main.myPlayer == projectile.owner && projectile.ai[0] == 0f)
			//{
			//	if (Main.player[projectile.owner].channel)
			//	{
			//		float num48 = 12f;
			//		Vector2 vector6 = new Vector2(projectile.position.X + (float)projectile.width * 0.5f, projectile.position.Y + (float)projectile.height * 0.5f);
			//		float num49 = (float)Main.mouseX + Main.screenPosition.X - vector6.X;
			//		float num50 = (float)Main.mouseY + Main.screenPosition.Y - vector6.Y;
			//		float num51 = (float)Math.Sqrt((double)(num49 * num49 + num50 * num50));
			//		num51 = (float)Math.Sqrt((double)(num49 * num49 + num50 * num50));
			//		if (num51 > num48)
			//		{
			//			num51 = num48 / num51;
			//			num49 *= num51;
			//			num50 *= num51;
			//			int num52 = (int)(num49 * 1000f);
			//			int num53 = (int)(projectile.velocity.X * 1000f);
			//			int num54 = (int)(num50 * 1000f);
			//			int num55 = (int)(projectile.velocity.Y * 1000f);
			//			if (num52 != num53 || num54 != num55)
			//			{
			//				projectile.netUpdate = true;
			//			}
			//			projectile.velocity.X = num49;
			//			projectile.velocity.Y = num50;
			//		}
			//		else
			//		{
			//			int num56 = (int)(num49 * 1000f);
			//			int num57 = (int)(projectile.velocity.X * 1000f);
			//			int num58 = (int)(num50 * 1000f);
			//			int num59 = (int)(projectile.velocity.Y * 1000f);
			//			if (num56 != num57 || num58 != num59)
			//			{
			//				projectile.netUpdate = true;
			//			}
			//			projectile.velocity.X = num49;
			//			projectile.velocity.Y = num50;
			//		}
			//	}
			//	else
			//	{
			//		if (projectile.ai[0] == 0f)
			//		{
			//			projectile.ai[0] = 1f;
			//			projectile.netUpdate = true;
			//			float num60 = 12f;
			//			Vector2 vector7 = new Vector2(projectile.position.X + (float)projectile.width * 0.5f, projectile.position.Y + (float)projectile.height * 0.5f);
			//			float num61 = (float)Main.mouseX + Main.screenPosition.X - vector7.X;
			//			float num62 = (float)Main.mouseY + Main.screenPosition.Y - vector7.Y;
			//			float num63 = (float)Math.Sqrt((double)(num61 * num61 + num62 * num62));
			//			if (num63 == 0f)
			//			{
			//				Projectile.NewProjectile(projectile.position.X + (float)(projectile.width / 2), projectile.position.Y + (float)(projectile.height / 2), 0, 0, Config.projectileID["Enemy Bio Spit"], 42, 8f, projectile.owner);
			//				vector7 = new Vector2(Main.player[projectile.owner].position.X + (float)(Main.player[projectile.owner].width / 2), Main.player[projectile.owner].position.Y + (float)(Main.player[projectile.owner].height / 2));
			//				num61 = projectile.position.X + (float)projectile.width * 0.5f - vector7.X;
			//				num62 = projectile.position.Y + (float)projectile.height * 0.5f - vector7.Y;
			//				num63 = (float)Math.Sqrt((double)(num61 * num61 + num62 * num62));
			//			}
			//			num63 = num60 / num63;
			//			num61 *= num63;
			//			num62 *= num63;
			//			projectile.velocity.X = num61;
			//			projectile.velocity.Y = num62;
			//			if (projectile.velocity.X == 0f && projectile.velocity.Y == 0f)
			//			{
			//				projectile.Kill();
			//			}
			//		}
			//	}
			//}
			//if (projectile.type == 34)
			//{
			//	projectile.rotation += 0.3f * (float)projectile.direction;
			//}
			//else
			//{
			//	if (projectile.velocity.X != 0f || projectile.velocity.Y != 0f)
			//	{
			//		projectile.rotation = (float)Math.Atan2((double)projectile.velocity.Y, (double)projectile.velocity.X) - 2.355f;
			//	}
			//}
			//if (projectile.velocity.Y > 16f)
			//{
			//	projectile.velocity.Y = 16f;
			//	return;
			//}
			#endregion
			}

			public override void OnHitPlayer(Player target, int damage, bool crit)
			{
				//Was 300, cut in half to counter expert mode doubling it
				target.AddBuff(30, 150, false); //bleeding
				target.AddBuff(20, 150, false); //poisoned
				target.AddBuff(ModContent.BuffType<Buffs.CurseBuildup>(), 18000, false); //-20 HP after several hits
			}

			#region commentedout
			//This was all commented out in the original. I'll just leave it like this for now.
			//public void Kill()
			//{
			//	if (!projectile.active)
			//	{
			//		return;
			//	}
			//	projectile.timeLeft = 0;
			//	{
			//		Main.PlaySound(3, (int)projectile.position.X, (int)projectile.position.Y, 9);
			//		for (int num40 = 0; num40 < 20; num40++)
			//		{
			//			Projectile.NewProjectile(projectile.position.X + (float)(projectile.width / 2), projectile.position.Y + (float)(projectile.height / 2), 0, 0, Config.projectileID["Enemy Bio Spit"], 12, 8f, projectile.owner);
			//			Vector2 arg_1394_0 = new Vector2(projectile.position.X - projectile.velocity.X, projectile.position.Y - projectile.velocity.Y);
			//			int damage = 60;
			//			int arg_1394_1 = projectile.width;
			//			int arg_1394_2 = projectile.height;
			//			int arg_1394_3 = 15;
			//			float arg_1394_4 = 0f;
			//			float arg_1394_5 = 0f;
			//			int arg_1394_6 = 100;
			//			Color newColor = default(Color);
			//			int num41 = Dust.NewDust(arg_1394_0, arg_1394_1, arg_1394_2, arg_1394_3, arg_1394_4, arg_1394_5, arg_1394_6, newColor, 2f);
			//			Main.dust[num41].noGravity = true;
			//			Dust expr_13B1 = Main.dust[num41];
			//			expr_13B1.velocity *= 2f;
			//			Vector2 arg_1422_0 = new Vector2(projectile.position.X - projectile.velocity.X, projectile.position.Y - projectile.velocity.Y);
			//			int arg_1422_1 = projectile.width;
			//			int arg_1422_2 = projectile.height;
			//			int arg_1422_3 = 15;
			//			float arg_1422_4 = 0f;
			//			float arg_1422_5 = 0f;
			//			int arg_1422_6 = 100;
			//			newColor = default(Color);
			//			num41 = Dust.NewDust(arg_1422_0, arg_1422_1, arg_1422_2, arg_1422_3, arg_1422_4, arg_1422_5, arg_1422_6, newColor, 1f);
			//		}
			//	}
			//	if (projectile.owner == Main.myPlayer)
			//	{
			//		if (Main.netMode != 0)
			//		{
			//			NetMessage.SendData(29, -1, -1, "", projectile.identity, (float)projectile.owner, 0f, 0f, 0);
			//		}
			//		int num98 = -1;
			//		if (projectile.aiStyle == 10)
			//		{
			//			int num99 = (int)(projectile.position.X + (float)(projectile.width / 2)) / 16;
			//			int num100 = (int)(projectile.position.Y + (float)(projectile.width / 2)) / 16;
			//			int num101 = 0;
			//			int num102 = 2;
			//			if (!Main.tile[num99, num100].active)
			//			{
			//				WorldGen.PlaceTile(num99, num100, num101, false, true, -1, 0);
			//				if (Main.tile[num99, num100].active && (int)Main.tile[num99, num100].type == num101)
			//				{
			//					NetMessage.SendData(17, -1, -1, "", 1, (float)num99, (float)num100, (float)num101, 0);
			//				}
			//				else
			//				{
			//					if (num102 > 0)
			//					{
			//						num98 = Item.NewItem((int)projectile.position.X, (int)projectile.position.Y, projectile.width, projectile.height, num102, 1, false, 0);
			//					}
			//				}
			//			}
			//			else
			//			{
			//				if (num102 > 0)
			//				{
			//					num98 = Item.NewItem((int)projectile.position.X, (int)projectile.position.Y, projectile.width, projectile.height, num102, 1, false, 0);
			//				}
			//			}
			//		}
			//		if (Main.netMode == 1 && num98 >= 0)
			//		{
			//			NetMessage.SendData(21, -1, -1, "", num98, 0f, 0f, 0f, 0);
			//		}
			//	}
			//	projectile.active = false;
			//}
			#endregion
	}
}


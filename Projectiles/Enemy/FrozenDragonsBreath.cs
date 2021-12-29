using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{

	public class FrozenDragonsBreath : ModProjectile
	{

		

		public override void SetDefaults()
		{
			projectile.width = 6;
			projectile.height = 6;
			projectile.timeLeft = 3600;
			projectile.hostile = true;
			projectile.penetrate = 3;
			projectile.ignoreWater = true;
			projectile.tileCollide = true;
			
			aiType = 4;
		}

		

		public override void AI()
		{
			if (projectile.timeLeft > 60)


			{
				projectile.timeLeft = 60;
			}
			if (projectile.ai[0] > 7f)
			{
				float num152 = 1f;
				if (projectile.ai[0] == 8f)
				{
					num152 = 0.25f;
				}
				else
				{
					if (projectile.ai[0] == 9f)
					{
						num152 = 0.5f;
					}
					else
					{
						if (projectile.ai[0] == 10f)
						{
							num152 = 0.75f;
						}
					}
				}
				projectile.ai[0] += 1f;
				int num153 = 6;
				if (projectile.type == ModContent.ProjectileType<FrozenDragonsBreath>())
				{
					num153 = 76;
				}
				if (num153 == 6 || Main.rand.Next(2) == 0)
				{
					for (int num154 = 0; num154 < 1; num154++)
					{
						int num155 = Dust.NewDust(new Vector2(projectile.position.X, projectile.position.Y), projectile.width, projectile.height, num153, projectile.velocity.X * 0.2f, projectile.velocity.Y * 0.2f, 100, default(Color), 1f);
						if (Main.rand.Next(3) != 0 || (num153 == 76 && Main.rand.Next(3) == 0))
						{
							Main.dust[num155].noGravity = true; 
							Main.dust[num155].scale *= 1.5f; //was 3
							Dust expr_6767_cp_0 = Main.dust[num155];
							expr_6767_cp_0.velocity.X = expr_6767_cp_0.velocity.X * 2f;
							Dust expr_6785_cp_0 = Main.dust[num155];
							expr_6785_cp_0.velocity.Y = expr_6785_cp_0.velocity.Y * 2f;
						}
						Main.dust[num155].scale *= 1f; //was 1.5
						Dust expr_67BC_cp_0 = Main.dust[num155];
						expr_67BC_cp_0.velocity.X = expr_67BC_cp_0.velocity.X * 1.2f;
						Dust expr_67DA_cp_0 = Main.dust[num155];
						expr_67DA_cp_0.velocity.Y = expr_67DA_cp_0.velocity.Y * 1.2f;
						Main.dust[num155].scale *= num152;
						if (num153 == 75)
						{
							Main.dust[num155].velocity += projectile.velocity;
							if (!Main.dust[num155].noGravity)
							{
								Main.dust[num155].velocity *= 0.5f;
							}
						}
					}
				}
			}
			else
			{
				projectile.ai[0] += 1f;
			}
			projectile.rotation += 0.3f * (float)projectile.direction;
		}

        public override void OnHitPlayer(Player target, int damage, bool crit)
		{
			target.AddBuff(ModContent.BuffType<Buffs.PowerfulCurseBuildup>(), 18000, false); //may lose -100 max HP after taking enough hits
			if (Main.expertMode)
			{
				target.AddBuff(BuffID.Frozen, 10, false);
			} else
            {
				target.AddBuff(BuffID.Frozen, 20, false);
			}
		}

		public override void DrawBehind(int index, List<int> drawCacheProjsBehindNPCsAndTiles, List<int> drawCacheProjsBehindNPCs, List<int> drawCacheProjsBehindProjectiles, List<int> drawCacheProjsOverWiresUI)
		{
			drawCacheProjsBehindNPCs.Add(index);
		}

	}

}
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System;
using Microsoft.Xna.Framework.Graphics;

namespace tsorcRevamp.NPCs.Enemies
{
	class FireLurker : ModNPC
	{

		float customAi1;
		int drownTimerMax = 2000;
		int drownTimer = 2000;
		int drowningRisk = 1200;
		int boredTimer = 0;
		int tBored = 1;//increasing this increases how long it take for the NP to get bored
		int boredResetT = 0;
		int bReset = 120;//increasing this will increase how long an NPC "gives up" before coming back to try again. Was 50
		int chargeDamage = 0;
		bool chargeDamageFlag = false;
		int meteorDamage = 17;
		//int hypnoticDisruptorDamage = 15;
		
		public int bioSpitDamage = 27;


		public override void SetDefaults()
		{
			npc.npcSlots = 3;
			Main.npcFrameCount[npc.type] = 15;
			animationType = 28;
			npc.knockBackResist = 0.4f;
			npc.aiStyle = -1;//was 3
			npc.damage = 40;
			npc.defense = 10;
			npc.height = 40;
			npc.width = 20;
			npc.lifeMax = 200;
			npc.HitSound = SoundID.NPCHit1;
			npc.DeathSound = SoundID.NPCDeath5;
			npc.value = 430;
			npc.lavaImmune = true;
			//banner = npc.type;
			npc.buffImmune[BuffID.Confused] = true;
			npc.buffImmune[24] = true;

			if (Main.hardMode)
			{
				npc.lifeMax = 380;
				npc.defense = 22;
				npc.value = 650;
				npc.damage = 60;
				bioSpitDamage = 43;
				npc.knockBackResist = 0.2f;
			}

			if (tsorcRevampWorld.SuperHardMode)
            {
				npc.lifeMax = 2660;
				npc.defense = 47;
				npc.value = 3650;
				npc.damage = 95;
				bioSpitDamage = 73;
				npc.knockBackResist = 0.1f;

			}
		}
		public override void ScaleExpertStats(int numPlayers, float bossLifeScale)
		{
			npc.lifeMax = (int)(npc.lifeMax / 2);
			npc.damage = (int)(npc.damage / 2);
			meteorDamage = (int)(meteorDamage / 2);
			//hypnoticDisruptorDamage = (int)(hypnoticDisruptorDamage / 2);
			bioSpitDamage = (int)(bioSpitDamage / 2);
		}

		

		#region Spawn
		public override float SpawnChance(NPCSpawnInfo spawnInfo)
		{
			Player P = spawnInfo.player; //These are mostly redundant with the new zone definitions, but it still works.
			bool Meteor = P.ZoneMeteor;
			bool Jungle = P.ZoneJungle;
			bool Dungeon = P.ZoneDungeon;
			bool Corruption = (P.ZoneCorrupt || P.ZoneCrimson);
			bool Hallow = P.ZoneHoly;
			bool AboveEarth = P.ZoneOverworldHeight;
			bool InBrownLayer = P.ZoneDirtLayerHeight;
			bool InGrayLayer = P.ZoneRockLayerHeight;
			bool InHell = P.ZoneUnderworldHeight;
			bool Ocean = spawnInfo.spawnTileX < 3600 || spawnInfo.spawnTileX > (Main.maxTilesX - 100) * 16;
			// P.townNPCs > 0f // is no town NPCs nearby

			if (spawnInfo.invasion)
			{
				return 0;
			}

			//ONLY SPAWNS IN HELL
			if (!Main.hardMode && InHell && Main.rand.Next(6) == 1) return 1;

			if (Main.hardMode && !tsorcRevampWorld.SuperHardMode && InHell && Main.rand.Next(5) == 1) return 1;

			if (tsorcRevampWorld.SuperHardMode && InHell && Main.rand.Next(5) == 1) return 1; //8 is 3%, 5 is 5, 3 IS 3%???
			return 0;
		}
		#endregion

		float spitTimer = 0;
		public override void AI()
		{
			tsorcRevampAIs.FighterAI(npc, 1.5f, 0.07f, canTeleport: true, soundType: 26, soundFrequency: 1000, enragePercent: 0.36f, enrageTopSpeed: 3f, lavaJumping: true);
			bool lineOfSight = Collision.CanHitLine(npc.Center, 0, 0, Main.player[npc.target].Center, 0, 0);
			tsorcRevampAIs.SimpleProjectile(npc, ref spitTimer, 160, ModContent.ProjectileType<Projectiles.Enemy.EnemyBioSpitBall>(), bioSpitDamage, 9, lineOfSight, true, 4, 9);

			if (spitTimer >= 130)
			{
				Lighting.AddLight(npc.Center, Color.Green.ToVector3());
				if (Main.rand.Next(3) == 1)
				{
					Dust.NewDust(npc.position, npc.width, npc.height, DustID.CursedTorch, npc.velocity.X, npc.velocity.Y);
					Dust.NewDust(npc.position, npc.width, npc.height, DustID.IchorTorch, npc.velocity.X, npc.velocity.Y);
				}
			}			
		}

		#region Find Frame
		public override void FindFrame(int currentFrame)
		{
			int num = 1;
			if (!Main.dedServ)
			{
				num = Main.npcTexture[npc.type].Height / Main.npcFrameCount[npc.type];
			}			
			if (npc.velocity.Y == 0f)
			{
				if (npc.direction == 1)
				{
					npc.spriteDirection = 1;
				}
				if (npc.direction == -1)
				{
					npc.spriteDirection = -1;
				}
				if (npc.velocity.X == 0f)
				{
					npc.frame.Y = 0;
					npc.frameCounter = 0.0;
				}
				else
				{
					npc.frameCounter += (double)(Math.Abs(npc.velocity.X) * 2f);
					npc.frameCounter += 1.0;
					if (npc.frameCounter > 6.0)
					{
						npc.frame.Y = npc.frame.Y + num;
						npc.frameCounter = 0.0;
					}
					if (npc.frame.Y / num >= Main.npcFrameCount[npc.type])
					{
						npc.frame.Y = num * 2;
					}
				}
			}
			else
			{
				npc.frameCounter = 0.0;
				npc.frame.Y = num;
				npc.frame.Y = 0;
			}
		}

		#endregion

		#region Debuffs
		public override void OnHitPlayer(Player player, int damage, bool crit)
		{
			if (Main.rand.Next(2) == 0)
			{
				player.AddBuff(24, 600, false); //on fire!
			}

			if (Main.rand.Next(10) == 0)
			{
				player.AddBuff(36, 600, false); //broken armor
				player.AddBuff(22, 180, false); //darkness
				player.AddBuff(ModContent.BuffType<Buffs.CurseBuildup>(), 18000, false); //-20 life if counter hits 100
			}
		}
		#endregion

		public override void NPCLoot()
		{
			Gore.NewGore(npc.position, npc.velocity, mod.GetGoreSlot("Gores/FireLurkerGore1"), 1f);
			Gore.NewGore(npc.position, npc.velocity, mod.GetGoreSlot("Gores/FireLurkerGore2"), 1f);
			Gore.NewGore(npc.position, npc.velocity, mod.GetGoreSlot("Gores/FireLurkerGore3"), 1f);
			Gore.NewGore(npc.position, npc.velocity, mod.GetGoreSlot("Gores/FireLurkerGore2"), 1f);
			Gore.NewGore(npc.position, npc.velocity, mod.GetGoreSlot("Gores/FireLurkerGore3"), 1f);
			for (int i = 0; i < 10; i++)
			{
				Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Blood Splat"), 1.1f);
			}

			if (Main.rand.Next(100) < 50) Item.NewItem(npc.getRect(), ItemID.GreaterHealingPotion);
			if (Main.rand.Next(100) < 30) Item.NewItem(npc.getRect(), ItemID.ManaRegenerationPotion);

			if (tsorcRevampWorld.SuperHardMode)
			{
				Item.NewItem(npc.getRect(), ModContent.ItemType<Items.RedTitanite>(), 1 + Main.rand.Next(1));
				Item.NewItem(npc.getRect(), ModContent.ItemType<Items.FlameOfTheAbyss>());
			}
			

		}

		/*
		#region Draw Projectile
		static Texture2D spearTexture;
		public override void PostDraw(SpriteBatch spriteBatch, Color drawColor)
		{
			if (spearTexture == null || spearTexture.IsDisposed)
			{
				spearTexture = mod.GetTexture("Projectiles/Enemy/EnemyBioSpitBall");
			}
			if (customAi1 >= 120)
			{
				Lighting.AddLight(npc.Center, Color.Green.ToVector3() * 1f); //Pick a color, any color. The 0.5f tones down its intensity by 50%
				if (Main.rand.Next(3) == 1)
				{
					Dust.NewDust(npc.position, npc.width, npc.height, DustID.CursedTorch, npc.velocity.X, npc.velocity.Y);
					Dust.NewDust(npc.position, npc.width, npc.height, DustID.IchorTorch, npc.velocity.X, npc.velocity.Y);
				}

				SpriteEffects effects = npc.spriteDirection < 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
				if (npc.spriteDirection == -1)
				{
					spriteBatch.Draw(spearTexture, npc.Center - Main.screenPosition, new Rectangle(0, 0, spearTexture.Width, spearTexture.Height), drawColor, -MathHelper.PiOver2, new Vector2(8, 10), npc.scale, effects, 0); // facing left (8, 38 work)
				}
				else
				{
					spriteBatch.Draw(spearTexture, npc.Center - Main.screenPosition, new Rectangle(0, 0, spearTexture.Width, spearTexture.Height), drawColor, MathHelper.PiOver2, new Vector2(8, 13), npc.scale, effects, 0); // facing right, first value is height, higher number is higher, 2nd value is width axis
				
				}
			}
		}
		#endregion
		*/
	}
}
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System;

namespace tsorcRevamp.NPCs.Enemies
{
	class BasiliskWalker : ModNPC
	{

		float shotTimer;
		int chargeDamage = 0;
		bool chargeDamageFlag = false;
		int hypnoticDisruptorDamage = 30; //was 15
		int bioSpitDamage = 20; //was 10


		public override void SetDefaults()
		{
			npc.npcSlots = 2;
			Main.npcFrameCount[npc.type] = 12;
			animationType = 28;
			npc.knockBackResist = 0.3f;
			npc.damage = 50;
			npc.defense = 8;
			npc.height = 50;
			npc.width = 24;
			npc.lifeMax = 180; //was 280

			if (Main.hardMode)
			{
				npc.lifeMax = 380;
				npc.defense = 20;
				npc.value = 1000;
				npc.damage = 60;
				hypnoticDisruptorDamage = 45;
				bioSpitDamage = 35;
			}

			npc.HitSound = SoundID.NPCHit20;
			npc.DeathSound = SoundID.NPCDeath5;
			npc.value = 400; //was 2000
			npc.lavaImmune = true;
			banner = npc.type;
			bannerItem = ModContent.ItemType<Banners.BasiliskWalkerBanner>();

			npc.buffImmune[BuffID.Confused] = true;
			npc.buffImmune[24] = true;
		}



		public override void ScaleExpertStats(int numPlayers, float bossLifeScale)
		{
			npc.lifeMax = (int)(npc.lifeMax / 2);
			npc.damage = (int)(npc.damage / 2);
			hypnoticDisruptorDamage = (int)(hypnoticDisruptorDamage / 2);
			bioSpitDamage = (int)(bioSpitDamage / 2);
		}
		
	
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
			bool FrozenOcean = spawnInfo.spawnTileX > (Main.maxTilesX - 800);
			bool Ocean = spawnInfo.spawnTileX < 800 || FrozenOcean;
			// P.townNPCs > 0f // is no town NPCs nearby

			if (spawnInfo.invasion)
			{
				return 0;
			}

			if (spawnInfo.water) return 0f;

			if (!Main.hardMode && !Main.dayTime && (Corruption || Jungle) && AboveEarth && P.townNPCs <= 0f && tsorcRevampWorld.Slain.ContainsKey(NPCID.SkeletronHead) && Main.rand.Next(24) == 1) return 1;
			

			//new chance to spawn in the corruption or crimson below ground (poison and cursed aren't activated until EoW and Skeletron respectively for balance; now we'll finally have a unique mod npc that fits well in these zones)
			if (!Main.hardMode && P.ZoneCorrupt && !Main.dayTime && !AboveEarth && Main.rand.Next(10) == 1) return 1;

			if (!Main.hardMode && P.ZoneCorrupt && Main.dayTime && !AboveEarth && Main.rand.Next(20) == 1) return 1;

			//higher chance to spawn in the crimson 
			if (!Main.hardMode && P.ZoneCrimson && !Main.dayTime && Main.rand.Next(4) == 1) return 1;

			if (!Main.hardMode && P.ZoneCrimson && Main.dayTime && Main.rand.Next(8) == 1) return 1;//10 is 3%, 5 is 6%

			//jungle or meteor
			if (!Main.hardMode && Meteor && !Dungeon && !Main.dayTime && (InBrownLayer || InGrayLayer) && Main.rand.Next(4) == 1) return 1;

			if (!Main.hardMode && Meteor && !Dungeon && Main.dayTime && InGrayLayer && Main.rand.Next(8) == 1) return 1; //was 60

			if (!Main.hardMode && Jungle && Main.dayTime && !Dungeon && InGrayLayer && Main.rand.Next(80) == 1) return 1; //was 200

			if (!Main.hardMode && Jungle && !Main.dayTime && !Dungeon && InGrayLayer && Main.rand.Next(60) == 1) return 1; //was 850

			//hard mode
			if (Main.hardMode && P.townNPCs <= 0f && !Main.dayTime && (Meteor || Jungle || Corruption) && !Dungeon && (AboveEarth || InBrownLayer || InGrayLayer) && Main.rand.Next(45) == 1) return 1;

			if (Main.hardMode && P.townNPCs <= 0f && Main.dayTime && (Meteor || Jungle || Corruption) && !Dungeon && (InBrownLayer || InGrayLayer) && Main.rand.Next(55) == 1) return 1;

			return 0;
		}

		public override void AI()
		{
			tsorcRevampAIs.FighterAI(npc, 1, 0.03f, canTeleport: true, soundType: 26, soundFrequency: 1000, enragePercent: 0.2f, enrageTopSpeed: 2);

			bool clearLineOfSight = Collision.CanHitLine(npc.Center, 2, 2, Main.player[npc.target].Center, 2, 2);
			if(tsorcRevampAIs.SimpleProjectile(npc, ref shotTimer, 140, ModContent.ProjectileType<Projectiles.Enemy.EnemyBioSpitBall>(), bioSpitDamage, 8, clearLineOfSight && Main.rand.Next(15) != 0, true))
            {
				Main.PlaySound(2, (int)npc.position.X, (int)npc.position.Y, 20, 0.2f, 0.3f); //fire
			}
			if (tsorcRevampAIs.SimpleProjectile(npc, ref shotTimer, 140, ModContent.ProjectileType<Projectiles.Enemy.HypnoticDisrupter>(), hypnoticDisruptorDamage, 3, clearLineOfSight, false))
			{
				Main.PlaySound(2, (int)npc.position.X, (int)npc.position.Y, 24, 0.6f, -0.5f); //wobble
			}

			//MAKE SOUND WHEN JUMPING/HOVERING
			if (Main.rand.Next(12) == 0 && npc.velocity.Y <= -1f)
			{
				Main.PlaySound(2, (int)npc.position.X, (int)npc.position.Y, 24, 0.2f, .1f);
			}

			//TELEGRAPH DUSTS
			if (shotTimer >= 100)
			{
				Lighting.AddLight(npc.Center, Color.Purple.ToVector3() * 0.5f); //Pick a color, any color. The 0.5f tones down its intensity by 50%
				if (Main.rand.Next(3) == 1)
				{
					Dust.NewDust(npc.position, npc.width, npc.height, DustID.CursedTorch, npc.velocity.X, npc.velocity.Y);
					//Dust.NewDust(npc.position, npc.width, npc.height, DustID.EmeraldBolt, npc.velocity.X, npc.velocity.Y);
				}
			}			

			//JUSTHIT CODE
			Player player2 = Main.player[npc.target];
			if (npc.justHit && npc.Distance(player2.Center) < 100)
			{
				shotTimer = 40f;
			}
			if (npc.justHit && npc.Distance(player2.Center) < 150 && Main.rand.Next(2) == 1)
			{
				shotTimer = 100f;
				npc.velocity.Y = Main.rand.NextFloat(-6f, -3f);
				npc.velocity.X = npc.velocity.X + (float)npc.direction * Main.rand.NextFloat(-5f, -3f);
				npc.netUpdate = true;
			}
			if (npc.justHit && npc.Distance(player2.Center) > 150 && Main.rand.Next(2) == 1)
			{
				npc.velocity.Y = Main.rand.NextFloat(-5f, -2f);
				npc.velocity.X = npc.velocity.X + (float)npc.direction * Main.rand.NextFloat(-5f, 3f);
				npc.netUpdate = true;
			}
			
			//Shift toward the player randomly
			if (Main.netMode != NetmodeID.MultiplayerClient)
			{
				Player player = Main.player[npc.target];
				if (Main.rand.Next(200) == 1 && npc.Distance(player.Center) > 260)
				{
					chargeDamageFlag = true;
					Vector2 vector8 = new Vector2(npc.position.X + (npc.width * 0.5f), npc.position.Y + (npc.height / 2));
					float rotation = (float)Math.Atan2(vector8.Y - (Main.player[npc.target].position.Y + (Main.player[npc.target].height * 0.5f)), vector8.X - (Main.player[npc.target].position.X + (Main.player[npc.target].width * 0.5f)));
					npc.velocity.X = (float)(Math.Cos(rotation) * 10) * -1;
					npc.velocity.Y = (float)(Math.Sin(rotation) * 10) * -1;
					npc.netUpdate = true;
				}
				if (chargeDamageFlag == true)
				{
					npc.damage = 50;
					chargeDamage++;
				}
				if (chargeDamage >= 55)
				{
					chargeDamageFlag = false;
					npc.damage = 45;
					chargeDamage = 0;
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
					npc.frameCounter += (double)(Math.Abs(npc.velocity.X) * .2f);
					//npc.frameCounter += 1.0;
					if (npc.frameCounter > 10)
					{
						npc.frame.Y = npc.frame.Y + num;
						npc.frameCounter = 0;
					}
					if (npc.frame.Y / num >= Main.npcFrameCount[npc.type])
					{
						npc.frame.Y = num * 1;
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
			player.AddBuff(17, 180, false); //hunter

			if (Main.rand.Next(2) == 0)
			{
				player.AddBuff(20, 300, false); //poisoned
			}
			if(tsorcRevampWorld.Slain.ContainsKey(NPCID.EaterofWorldsHead))
			{ 
				player.AddBuff(ModContent.BuffType<Buffs.CurseBuildup>(), 18000, false); //-20 life if counter hits 100
				player.GetModPlayer<tsorcRevampPlayer>().CurseLevel += 5;
			}
			if (Main.rand.Next(10) == 0)
			{
				player.AddBuff(36, 600, false); //broken armor
			}

		}
		#endregion

		public override void NPCLoot()
		{
			Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Parasite Zombie Gore 1"), 1.1f);
			Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Parasite Zombie Gore 2"), 1.1f);
			Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Parasite Zombie Gore 3"), 1.1f);
			Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Parasite Zombie Gore 2"), 1.1f);
			Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Parasite Zombie Gore 1"), 1.1f);
			Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Parasite Zombie Gore 3"), 1.1f);
			Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Parasite Zombie Gore 2"), 1.1f);
			Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Parasite Zombie Gore 3"), 1.1f);
			for (int i = 0; i < 10; i++)
			{
				Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Blood Splat"), 1.1f);
			}

			if (!Main.hardMode && Main.rand.Next(100) < 30) Item.NewItem(npc.getRect(), ItemID.HealingPotion);
			if (Main.rand.Next(100) < 30) Item.NewItem(npc.getRect(), ItemID.ManaRegenerationPotion);
			//if (Main.rand.Next(100) < 20) Item.NewItem(npc.getRect(), ModContent.ItemType<Items.BossItems.TomeOfSlograAndGaibon>());
			
		}
	}
}
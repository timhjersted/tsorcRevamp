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
			NPC.npcSlots = 2;
			Main.npcFrameCount[NPC.type] = 12;
			animationType = 28;
			NPC.knockBackResist = 0.3f;
			NPC.damage = 50;
			NPC.defense = 8;
			NPC.height = 50;
			NPC.width = 24;
			NPC.lifeMax = 180; //was 280

			if (Main.hardMode)
			{
				NPC.lifeMax = 380;
				NPC.defense = 20;
				NPC.value = 1000;
				NPC.damage = 60;
				hypnoticDisruptorDamage = 45;
				bioSpitDamage = 35;
			}

			NPC.HitSound = SoundID.NPCHit20;
			NPC.DeathSound = SoundID.NPCDeath5;
			NPC.value = 400; //was 2000
			NPC.lavaImmune = true;
			banner = NPC.type;
			bannerItem = ModContent.ItemType<Banners.BasiliskWalkerBanner>();

			NPC.buffImmune[BuffID.Confused] = true;
			NPC.buffImmune[24] = true;
		}



		public override void ScaleExpertStats(int numPlayers, float bossLifeScale)
		{
			NPC.lifeMax = (int)(NPC.lifeMax / 2);
			NPC.damage = (int)(NPC.damage / 2);
			hypnoticDisruptorDamage = (int)(hypnoticDisruptorDamage / 2);
			bioSpitDamage = (int)(bioSpitDamage / 2);
		}
		
	
		public override float SpawnChance(NPCSpawnInfo spawnInfo)
		{
			Player P = spawnInfo.Player; //These are mostly redundant with the new zone definitions, but it still works.
			bool Meteor = P.ZoneMeteor;
			bool Jungle = P.ZoneJungle;
			bool Dungeon = P.ZoneDungeon;
			bool Corruption = (P.ZoneCorrupt || P.ZoneCrimson);
			bool Hallow = P.ZoneHallow;
			bool AboveEarth = P.ZoneOverworldHeight;
			bool InBrownLayer = P.ZoneDirtLayerHeight;
			bool InGrayLayer = P.ZoneRockLayerHeight;
			bool InHell = P.ZoneUnderworldHeight;
			bool FrozenOcean = spawnInfo.SpawnTileX > (Main.maxTilesX - 800);
			bool Ocean = spawnInfo.SpawnTileX < 800 || FrozenOcean;
			// P.townNPCs > 0f // is no town NPCs nearby

			if (spawnInfo.Invasion)
			{
				return 0;
			}

			if (spawnInfo.Water) return 0f;

			if (!Main.hardMode && !Main.dayTime && (Corruption || Jungle) && AboveEarth && P.townNPCs <= 0f && tsorcRevampWorld.Slain.ContainsKey(NPCID.SkeletronHead) && Main.rand.Next(24) == 1) return 1;
			

			//new chance to spawn in the corruption or crimson below ground (poison and cursed aren't activated until EoW and Skeletron respectively for balance; now we'll finally have a unique mod npc that fits well in these zones)
			if (!Main.hardMode && P.ZoneCorrupt && !Main.dayTime && !AboveEarth && Main.rand.Next(10) == 1) return 1;

			if (!Main.hardMode && P.ZoneCorrupt && Main.dayTime && !AboveEarth && Main.rand.Next(20) == 1) return 1;

			//higher chance to spawn in the crimson 
			if (!Main.hardMode && P.ZoneCrimson && !Main.dayTime && Main.rand.Next(5) == 1) return 1;

			if (!Main.hardMode && P.ZoneCrimson && Main.dayTime && Main.rand.Next(10) == 1) return 1;//10 is 3%, 5 is 6%

			//meteor not desert
			if (!Main.hardMode && Meteor && !Dungeon && !Main.dayTime && !P.ZoneUndergroundDesert && (InBrownLayer || InGrayLayer) && Main.rand.Next(5) == 1) return 1;

			if (!Main.hardMode && Meteor && !Dungeon && Main.dayTime && !P.ZoneUndergroundDesert && InGrayLayer && Main.rand.Next(10) == 1) return 1; 
			//meteor and desert
			if (!Main.hardMode && Meteor && !Dungeon && !Main.dayTime && P.ZoneUndergroundDesert && (InBrownLayer || InGrayLayer) && Main.rand.Next(12) == 1) return 1;

			if (!Main.hardMode && Meteor && !Dungeon && Main.dayTime && P.ZoneUndergroundDesert && InGrayLayer && Main.rand.Next(24) == 1) return 1; 
			//jungle
			if (!Main.hardMode && Jungle && Main.dayTime && !Dungeon && InGrayLayer && Main.rand.Next(80) == 1) return 1; //was 200

			if (!Main.hardMode && Jungle && !Main.dayTime && !Dungeon && InGrayLayer && Main.rand.Next(60) == 1) return 1; //was 850

			//hard mode
			if (Main.hardMode && P.townNPCs <= 0f && !Main.dayTime && (Meteor || Jungle || Corruption) && !Dungeon && (AboveEarth || InBrownLayer || InGrayLayer) && Main.rand.Next(45) == 1) return 1;

			if (Main.hardMode && P.townNPCs <= 0f && Main.dayTime && (Meteor || Jungle || Corruption) && !Dungeon && (InBrownLayer || InGrayLayer) && Main.rand.Next(55) == 1) return 1;

			return 0;
		}

		public override void AI()
		{
			tsorcRevampAIs.FighterAI(NPC, 1, 0.03f, canTeleport: true, soundType: 26, soundFrequency: 1000, enragePercent: 0.2f, enrageTopSpeed: 2);

			bool clearLineOfSight = Collision.CanHitLine(NPC.Center, 2, 2, Main.player[NPC.target].Center, 2, 2);
			if(tsorcRevampAIs.SimpleProjectile(NPC, ref shotTimer, 140, ModContent.ProjectileType<Projectiles.Enemy.EnemyBioSpitBall>(), bioSpitDamage, 8, clearLineOfSight && Main.rand.Next(15) != 0, true))
            {
				Terraria.Audio.SoundEngine.PlaySound(2, (int)NPC.position.X, (int)NPC.position.Y, 20, 0.2f, 0.3f); //fire
			}
			if (tsorcRevampAIs.SimpleProjectile(NPC, ref shotTimer, 140, ModContent.ProjectileType<Projectiles.Enemy.HypnoticDisrupter>(), hypnoticDisruptorDamage, 3, clearLineOfSight, false))
			{
				Terraria.Audio.SoundEngine.PlaySound(2, (int)NPC.position.X, (int)NPC.position.Y, 24, 0.6f, -0.5f); //wobble
			}

			//MAKE SOUND WHEN JUMPING/HOVERING
			if (Main.rand.Next(12) == 0 && NPC.velocity.Y <= -1f)
			{
				Terraria.Audio.SoundEngine.PlaySound(2, (int)NPC.position.X, (int)NPC.position.Y, 24, 0.2f, .1f);
			}

			//TELEGRAPH DUSTS
			if (shotTimer >= 100)
			{
				Lighting.AddLight(NPC.Center, Color.Purple.ToVector3() * 0.5f); //Pick a color, any color. The 0.5f tones down its intensity by 50%
				if (Main.rand.Next(3) == 1)
				{
					Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.CursedTorch, NPC.velocity.X, NPC.velocity.Y);
					//Dust.NewDust(npc.position, npc.width, npc.height, DustID.EmeraldBolt, npc.velocity.X, npc.velocity.Y);
				}
			}			

			//JUSTHIT CODE
			Player player2 = Main.player[NPC.target];
			if (NPC.justHit && NPC.Distance(player2.Center) < 150)
			{
				shotTimer = 40f;
			}
			if (NPC.justHit && NPC.Distance(player2.Center) < 150 && Main.rand.Next(2) == 1)
			{
				shotTimer = 100f;
				NPC.velocity.Y = Main.rand.NextFloat(-6f, -3f);
				NPC.velocity.X = NPC.velocity.X + (float)NPC.direction * Main.rand.NextFloat(-5f, -3f);
				NPC.netUpdate = true;
			}
			if (NPC.justHit && NPC.Distance(player2.Center) > 150 && Main.rand.Next(2) == 1)
			{
				NPC.velocity.Y = Main.rand.NextFloat(-5f, -2f);
				NPC.velocity.X = NPC.velocity.X + (float)NPC.direction * Main.rand.NextFloat(-5f, 3f);
				NPC.netUpdate = true;
			}
			
			//Shift toward the player randomly
			if (Main.netMode != NetmodeID.MultiplayerClient)
			{
				Player player = Main.player[NPC.target];
				if (Main.rand.Next(200) == 1 && NPC.Distance(player.Center) > 260)
				{
					chargeDamageFlag = true;
					Vector2 vector8 = new Vector2(NPC.position.X + (NPC.width * 0.5f), NPC.position.Y + (NPC.height / 2));
					float rotation = (float)Math.Atan2(vector8.Y - (Main.player[NPC.target].position.Y + (Main.player[NPC.target].height * 0.5f)), vector8.X - (Main.player[NPC.target].position.X + (Main.player[NPC.target].width * 0.5f)));
					NPC.velocity.X = (float)(Math.Cos(rotation) * 10) * -1;
					NPC.velocity.Y = (float)(Math.Sin(rotation) * 10) * -1;
					NPC.netUpdate = true;
				}
				if (chargeDamageFlag == true)
				{
					NPC.damage = 50;
					chargeDamage++;
				}
				if (chargeDamage >= 55)
				{
					chargeDamageFlag = false;
					NPC.damage = 45;
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
				num = Main.npcTexture[NPC.type].Height / Main.npcFrameCount[NPC.type];
			}			
			if (NPC.velocity.Y == 0f)
			{
				if (NPC.direction == 1)
				{
					NPC.spriteDirection = 1;
				}
				if (NPC.direction == -1)
				{
					NPC.spriteDirection = -1;
				}
				if (NPC.velocity.X == 0f)
				{
					NPC.frame.Y = 0;
					NPC.frameCounter = 0.0;
				}
				else
				{
					NPC.frameCounter += (double)(Math.Abs(NPC.velocity.X) * .2f);
					//npc.frameCounter += 1.0;
					if (NPC.frameCounter > 10)
					{
						NPC.frame.Y = NPC.frame.Y + num;
						NPC.frameCounter = 0;
					}
					if (NPC.frame.Y / num >= Main.npcFrameCount[NPC.type])
					{
						NPC.frame.Y = num * 1;
					}
				}
			}
			else
			{
				NPC.frameCounter = 0.0;
				NPC.frame.Y = num;
				NPC.frame.Y = 0;
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

		public override void OnKill()
		{
			Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.GetGoreSlot("Gores/Parasite Zombie Gore 1"), 1.1f);
			Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.GetGoreSlot("Gores/Parasite Zombie Gore 2"), 1.1f);
			Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.GetGoreSlot("Gores/Parasite Zombie Gore 3"), 1.1f);
			Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.GetGoreSlot("Gores/Parasite Zombie Gore 2"), 1.1f);
			Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.GetGoreSlot("Gores/Parasite Zombie Gore 1"), 1.1f);
			Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.GetGoreSlot("Gores/Parasite Zombie Gore 3"), 1.1f);
			Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.GetGoreSlot("Gores/Parasite Zombie Gore 2"), 1.1f);
			Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.GetGoreSlot("Gores/Parasite Zombie Gore 3"), 1.1f);
			for (int i = 0; i < 10; i++)
			{
				Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.GetGoreSlot("Gores/Blood Splat"), 1.1f);
			}

			if (!Main.hardMode && Main.rand.Next(100) < 30) Item.NewItem(NPC.getRect(), ItemID.HealingPotion);
			if (Main.rand.Next(100) < 30) Item.NewItem(NPC.getRect(), ItemID.ManaRegenerationPotion);
			//if (Main.rand.Next(100) < 20) Item.NewItem(npc.getRect(), ModContent.ItemType<Items.BossItems.TomeOfSlograAndGaibon>());
			
		}
	}
}
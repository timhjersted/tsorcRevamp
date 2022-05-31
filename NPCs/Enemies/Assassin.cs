using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.NPCs.Enemies
{
	public class Assassin : ModNPC
	{
		public override void SetDefaults()
		{
			aiType = NPCID.SkeletonArcher;
			NPC.HitSound = SoundID.NPCHit48;
			NPC.DeathSound = SoundID.NPCDeath1;
			NPC.damage = 70;
			NPC.lifeMax = 1000; //was 3000 which means 6000
			if (tsorcRevampWorld.SuperHardMode) { NPC.lifeMax = 3000; NPC.defense = 60; NPC.damage = 80; NPC.value = 6900; }
			NPC.scale = 1.0f; //was 1.1
			NPC.defense = 40;
			NPC.value = 4600;
			NPC.width = 18;
			NPC.aiStyle = -1;
			NPC.height = 48;
			NPC.knockBackResist = 0.5f;
			NPC.rarity = 3;
			banner = NPC.type;
			bannerItem = ModContent.ItemType<Banners.AssassinBanner>();

			animationType = NPCID.SkeletonArcher;
			Main.npcFrameCount[NPC.type] = 20;
		}

		public override void ScaleExpertStats(int numPlayers, float bossLifeScale)
		{
			NPC.lifeMax = (int)(NPC.lifeMax / 2);
			NPC.damage = (int)(NPC.damage / 2);
		}


		public override void OnKill()
		{
			Item.NewItem(NPC.getRect(), ModContent.ItemType<Items.Ammo.ArrowOfBard>(), Main.rand.Next(3, 6));
			Item.NewItem(NPC.getRect(), ItemID.ArcheryPotion);
			Item.NewItem(NPC.getRect(), ItemID.GreaterHealingPotion);
			if (Main.rand.Next(5) == 0) Item.NewItem(NPC.getRect(), ModContent.ItemType<Items.Potions.CrimsonPotion>());
			if (Main.rand.Next(5) == 0) Item.NewItem(NPC.getRect(), ModContent.ItemType<Items.Potions.StrengthPotion>());
			if (Main.rand.Next(5) == 0) Item.NewItem(NPC.getRect(), ItemID.FlaskofFire);
			if (Main.rand.Next(5) == 0) Item.NewItem(NPC.getRect(), ModContent.ItemType<Items.Potions.ShockwavePotion>());
			if (Main.rand.Next(5) == 0) Item.NewItem(NPC.getRect(), ModContent.ItemType<Items.Potions.BattlefrontPotion>());
			if (Main.rand.Next(50) == 0) Item.NewItem(NPC.getRect(), ModContent.ItemType<Items.Potions.AttractionPotion>());
			Item.NewItem(NPC.getRect(), ItemID.IronskinPotion);
		}


		#region Spawn
		public override float SpawnChance(NPCSpawnInfo spawnInfo)
		{
			float chance = 0f;

			if (Main.hardMode && !Main.dayTime && spawnInfo.Player.ZoneJungle && !spawnInfo.Player.ZoneOverworldHeight && !spawnInfo.Player.ZoneDungeon && !spawnInfo.Player.ZoneCorrupt && !spawnInfo.Player.ZoneCrimson && Main.rand.Next(140) == 0)
			{
				if(Main.rand.Next(2) == 0)
                {
					UsefulFunctions.BroadcastText("An assassin is tracking your position...", 175, 75, 255);
				}
				
				return 1f;
			}

			if (Main.hardMode && !Main.dayTime && spawnInfo.Player.ZoneJungle && !spawnInfo.Player.ZoneDungeon && !spawnInfo.Player.ZoneCorrupt && !spawnInfo.Player.ZoneCrimson && Main.rand.Next(200) == 0)
			{
				if (Main.rand.Next(2) == 0)
				{
					UsefulFunctions.BroadcastText("You hear a bow draw...", 175, 75, 255);
				}
				return 1f;
			}

			if (Main.hardMode && Main.dayTime && spawnInfo.Player.ZoneJungle && !spawnInfo.Player.ZoneDungeon && !spawnInfo.Player.ZoneCorrupt && !spawnInfo.Player.ZoneCrimson && Main.rand.Next(300) == 0)
			{
				if (Main.rand.Next(2) == 0)
				{
					UsefulFunctions.BroadcastText("You hear foot steps...", 175, 75, 255);
				}
				return 1f;
			}

			if (Main.hardMode && (spawnInfo.Player.ZoneDungeon || spawnInfo.Player.ZoneHallow || spawnInfo.Player.ZoneSnow || spawnInfo.Player.ZoneUndergroundDesert || spawnInfo.Player.ZoneDesert) && Main.rand.Next(200) == 0)
			{
				if (Main.rand.Next(2) == 0)
				{
					UsefulFunctions.BroadcastText("An assassin is tracking your position...", 175, 75, 255);
				}
				return 1f;
			}

			

			if (Main.hardMode && !Main.dayTime && spawnInfo.Player.ZoneOverworldHeight && Main.rand.Next(300) == 0)
			{
				if (Main.rand.Next(2) == 0)
				{
					UsefulFunctions.BroadcastText("You are being hunted...", 175, 75, 255);
				}
				return 1f;
			}

			//SUPER-HM

			/*if (ModWorld.superHardmode && !Main.dayTime && !Corruption && !Ocean && AboveEarth && Main.rand.Next(30) == 1)

			{
				UsefulFunctions.BroadcastText("An assassin is nearby...", 175, 75, 255);
				return true;
			}

			if (ModWorld.superHardmode && Main.dayTime && !Corruption && !Ocean && Jungle && AboveEarth && Main.rand.Next(30) == 1)

			{
				UsefulFunctions.BroadcastText("An assassin is nearby...", 175, 75, 255);
				return true;
			}

			if (ModWorld.superHardmode && !Corruption && !Ocean && Jungle && InGrayLayer && Main.rand.Next(20) == 1)

			{
				UsefulFunctions.BroadcastText("An assassin is tracking your position...", 175, 75, 255);
				return true;
			}*/

			return chance;
		}

		#endregion

		public override void AI()
		{
			tsorcRevampAIs.ArcherAI(NPC, ModContent.ProjectileType<Projectiles.Enemy.EnemyArrowOfBard>(), 50, 14, 100, 2f, .05f, canTeleport: true, enragePercent: 0.4f, enrageTopSpeed: 4f);			
		}


		#region Gore
		public override void HitEffect(int hitDirection, double damage)
		{
			for (int i = 0; i < 5; i++)
			{
				int dustType = 5;
				int dustIndex = Dust.NewDust(NPC.position, NPC.width, NPC.height, dustType);
				Dust dust = Main.dust[dustIndex];
				dust.velocity.X = dust.velocity.X + Main.rand.Next(-50, 51) * 0.06f;
				dust.velocity.Y = dust.velocity.Y + Main.rand.Next(-50, 51) * 0.06f;
				dust.scale *= 1f + Main.rand.Next(-30, 31) * 0.01f;
				dust.noGravity = true;
			}
			if (NPC.life <= 0)
			{
				for (int i = 0; i < 25; i++)
				{
					Dust.NewDust(NPC.position, NPC.width, NPC.height, 5, Main.rand.Next(-3, 3), Main.rand.Next(-3, 3), 70, default(Color), 1f);
				}

				Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.GetGoreSlot("Gores/Assassin Gore 1"), 1f);
				Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.GetGoreSlot("Gores/Assassin Gore 2"), 1f);
				Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.GetGoreSlot("Gores/Assassin Gore 3"), 1f);
				Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.GetGoreSlot("Gores/Assassin Gore 2"), 1f);
				Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.GetGoreSlot("Gores/Assassin Gore 3"), 1f);
			}
		}
		#endregion
	}
}
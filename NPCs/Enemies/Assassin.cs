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
			npc.HitSound = SoundID.NPCHit48;
			npc.DeathSound = SoundID.NPCDeath1;
			npc.damage = 70;
			npc.lifeMax = 1000; //was 3000 which means 6000
			if (tsorcRevampWorld.SuperHardMode) { npc.lifeMax = 3000; npc.defense = 60; npc.damage = 80; npc.value = 6900; }
			npc.scale = 1.0f; //was 1.1
			npc.defense = 40;
			npc.value = 4600;
			npc.width = 18;
			npc.aiStyle = -1;
			npc.height = 48;
			npc.knockBackResist = 0.5f;
			npc.rarity = 3;
			banner = npc.type;
			bannerItem = ModContent.ItemType<Banners.AssassinBanner>();

			animationType = NPCID.SkeletonArcher;
			Main.npcFrameCount[npc.type] = 20;
		}

		public override void ScaleExpertStats(int numPlayers, float bossLifeScale)
		{
			npc.lifeMax = (int)(npc.lifeMax / 2);
			npc.damage = (int)(npc.damage / 2);
		}


		public override void NPCLoot()
		{
			Item.NewItem(npc.getRect(), ModContent.ItemType<Items.Ammo.ArrowOfBard>(), Main.rand.Next(3, 6));
			Item.NewItem(npc.getRect(), ItemID.ArcheryPotion);
			Item.NewItem(npc.getRect(), ItemID.GreaterHealingPotion);
			if (Main.rand.Next(5) == 0) Item.NewItem(npc.getRect(), ModContent.ItemType<Items.Potions.CrimsonPotion>());
			if (Main.rand.Next(5) == 0) Item.NewItem(npc.getRect(), ModContent.ItemType<Items.Potions.StrengthPotion>());
			if (Main.rand.Next(5) == 0) Item.NewItem(npc.getRect(), ItemID.FlaskofFire);
			if (Main.rand.Next(5) == 0) Item.NewItem(npc.getRect(), ModContent.ItemType<Items.Potions.ShockwavePotion>());
			if (Main.rand.Next(5) == 0) Item.NewItem(npc.getRect(), ModContent.ItemType<Items.Potions.BattlefrontPotion>());
			if (Main.rand.Next(50) == 0) Item.NewItem(npc.getRect(), ModContent.ItemType<Items.Potions.AttractionPotion>());
			Item.NewItem(npc.getRect(), ItemID.IronskinPotion);
		}


		#region Spawn
		public override float SpawnChance(NPCSpawnInfo spawnInfo)
		{
			float chance = 0f;

			if (Main.hardMode && !Main.dayTime && spawnInfo.player.ZoneJungle && !spawnInfo.player.ZoneOverworldHeight && !spawnInfo.player.ZoneDungeon && !spawnInfo.player.ZoneCorrupt && !spawnInfo.player.ZoneCrimson && Main.rand.Next(140) == 0)
			{
				if(Main.rand.Next(2) == 0)
                {
					Main.NewText("An assassin is tracking your position...", 175, 75, 255);
				}
				
				return 1f;
			}

			if (Main.hardMode && !Main.dayTime && spawnInfo.player.ZoneJungle && !spawnInfo.player.ZoneDungeon && !spawnInfo.player.ZoneCorrupt && !spawnInfo.player.ZoneCrimson && Main.rand.Next(200) == 0)
			{
				if (Main.rand.Next(2) == 0)
				{
					Main.NewText("You hear a bow draw...", 175, 75, 255);
				}
				return 1f;
			}

			if (Main.hardMode && Main.dayTime && spawnInfo.player.ZoneJungle && !spawnInfo.player.ZoneDungeon && !spawnInfo.player.ZoneCorrupt && !spawnInfo.player.ZoneCrimson && Main.rand.Next(300) == 0)
			{
				if (Main.rand.Next(2) == 0)
				{
					Main.NewText("You hear foot steps...", 175, 75, 255);
				}
				return 1f;
			}

			if (Main.hardMode && (spawnInfo.player.ZoneDungeon || spawnInfo.player.ZoneHoly || spawnInfo.player.ZoneSnow || spawnInfo.player.ZoneUndergroundDesert || spawnInfo.player.ZoneDesert) && Main.rand.Next(200) == 0)
			{
				if (Main.rand.Next(2) == 0)
				{
					Main.NewText("An assassin is tracking your position...", 175, 75, 255);
				}
				return 1f;
			}

			

			if (Main.hardMode && !Main.dayTime && spawnInfo.player.ZoneOverworldHeight && Main.rand.Next(300) == 0)
			{
				if (Main.rand.Next(2) == 0)
				{
					Main.NewText("You are being hunted...", 175, 75, 255);
				}
				return 1f;
			}
			
			//SUPER-HM

			/*if (ModWorld.superHardmode && !Main.dayTime && !Corruption && !Ocean && AboveEarth && Main.rand.Next(30) == 1)

			{
				Main.NewText("An assassin is nearby...", 175, 75, 255);
				return true;
			}

			if (ModWorld.superHardmode && Main.dayTime && !Corruption && !Ocean && Jungle && AboveEarth && Main.rand.Next(30) == 1)

			{
				Main.NewText("An assassin is nearby...", 175, 75, 255);
				return true;
			}

			if (ModWorld.superHardmode && !Corruption && !Ocean && Jungle && InGrayLayer && Main.rand.Next(20) == 1)

			{
				Main.NewText("An assassin is tracking your position...", 175, 75, 255);
				return true;
			}*/

			return chance;
		}

		#endregion

		public override void AI()
		{
			tsorcRevampAIs.ArcherAI(npc, ModContent.ProjectileType<Projectiles.Enemy.EnemyArrowOfBard>(), 50, 14, 100, 2f, .05f, canTeleport: true, enragePercent: 0.4f, enrageTopSpeed: 4f);			
		}


		#region Gore
		public override void HitEffect(int hitDirection, double damage)
		{
			for (int i = 0; i < 5; i++)
			{
				int dustType = 5;
				int dustIndex = Dust.NewDust(npc.position, npc.width, npc.height, dustType);
				Dust dust = Main.dust[dustIndex];
				dust.velocity.X = dust.velocity.X + Main.rand.Next(-50, 51) * 0.06f;
				dust.velocity.Y = dust.velocity.Y + Main.rand.Next(-50, 51) * 0.06f;
				dust.scale *= 1f + Main.rand.Next(-30, 31) * 0.01f;
				dust.noGravity = true;
			}
			if (npc.life <= 0)
			{
				for (int i = 0; i < 25; i++)
				{
					Dust.NewDust(npc.position, npc.width, npc.height, 5, Main.rand.Next(-3, 3), Main.rand.Next(-3, 3), 70, default(Color), 1f);
				}

				Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Assassin Gore 1"), 1f);
				Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Assassin Gore 2"), 1f);
				Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Assassin Gore 3"), 1f);
				Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Assassin Gore 2"), 1f);
				Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Assassin Gore 3"), 1f);
			}
		}
		#endregion
	}
}
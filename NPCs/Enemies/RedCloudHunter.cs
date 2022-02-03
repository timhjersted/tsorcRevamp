using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Armors;

namespace tsorcRevamp.NPCs.Enemies
{
	public class RedCloudHunter : ModNPC
	{

		public int archerBoltDamage = 25; //was 85, whoa, how did no one complain about this?

		public override void SetDefaults()
		{
			aiType = NPCID.SkeletonArcher;
			npc.HitSound = SoundID.NPCHit1;
			npc.DeathSound = SoundID.NPCDeath1;
			npc.damage = 52;
			npc.lifeMax = 1150;
			npc.scale = 0.9f;
			npc.defense = 30;
			npc.value = 6500;
			npc.width = 18;
			npc.aiStyle = -1;
			npc.height = 48;
			npc.knockBackResist = 0.6f;
			npc.rarity = 3;
			banner = npc.type;
			npc.buffImmune[BuffID.Confused] = true;
			bannerItem = ModContent.ItemType<Banners.RedCloudHunterBanner>();

			animationType = NPCID.SkeletonArcher;
			Main.npcFrameCount[npc.type] = 20;

			if (Main.hardMode)
			{
				npc.defense = 14;
				npc.value = 3500;
				npc.damage = 40;
				archerBoltDamage = 65;
			}

			if (tsorcRevampWorld.SuperHardMode)
			{
				npc.lifeMax = 1750;
				npc.defense = 40;
				npc.value = 3700;
				npc.damage = 70;
				archerBoltDamage = 85;
			}

	}

		public override void ScaleExpertStats(int numPlayers, float bossLifeScale)
		{
			npc.lifeMax = (int)(npc.lifeMax / 2);
			npc.damage = (int)(npc.damage / 2);
			npc.defense = (int)(npc.defense * (2 / 3));
			archerBoltDamage = (int)(archerBoltDamage / 2);
		}

		public override void NPCLoot()
		{
			if (Main.rand.Next(2) == 0) Item.NewItem(npc.getRect(), ModContent.ItemType<Items.Ammo.ArrowOfBard>(), Main.rand.Next(1, 3));
		}

		float customAi1;
		int drownTimerMax = 3000;
		int drownTimer = 3000;
		int drowningRisk = 1200;
		

		#region Spawn

		public override float SpawnChance(NPCSpawnInfo spawnInfo)
		{
			float chance = 0f;


			if (!Main.hardMode && spawnInfo.player.ZoneDungeon) return 0.02f;

			if (Main.hardMode && !spawnInfo.player.ZoneCorrupt && !spawnInfo.player.ZoneCrimson && !spawnInfo.player.ZoneBeach && spawnInfo.player.ZoneJungle) return 0.05f;
			if (Main.hardMode && (spawnInfo.player.ZoneCorrupt || spawnInfo.player.ZoneCrimson)) return 0.09f;
			if (Main.hardMode && spawnInfo.player.ZoneOverworldHeight && (spawnInfo.player.ZoneCorrupt || spawnInfo.player.ZoneCrimson || spawnInfo.player.ZoneBeach || spawnInfo.player.ZoneJungle)) return 0.0125f;

			if (Main.hardMode && spawnInfo.lihzahrd) return 0.15f;

			if (tsorcRevampWorld.SuperHardMode && (spawnInfo.player.ZoneCorrupt || spawnInfo.player.ZoneCrimson)) return 0.13f;
			if (tsorcRevampWorld.SuperHardMode && spawnInfo.player.ZoneOverworldHeight && (spawnInfo.player.ZoneJungle || spawnInfo.player.ZoneCorrupt || spawnInfo.player.ZoneCrimson)) return 0.1f;
			if (tsorcRevampWorld.SuperHardMode && (spawnInfo.player.ZoneDesert || spawnInfo.player.ZoneUndergroundDesert)) return 0.13f;
			if (tsorcRevampWorld.SuperHardMode && spawnInfo.player.ZoneDungeon) return 0.01f; //.08% is 4.28%
			return chance;
		}
		#endregion

		public override void AI()
		{
			tsorcRevampAIs.ArcherAI(npc, ProjectileID.FlamingArrow, 22, 13, 100, 2, canTeleport: true, enragePercent: 0.3f, enrageTopSpeed: 2.6f);			
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

				Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Red Cloud Hunter Gore 1"), 1f);
				Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Red Cloud Hunter Gore 2"), 1f);
				Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Red Cloud Hunter Gore 3"), 1f);
				Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Red Cloud Hunter Gore 2"), 1f);
				Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Red Cloud Hunter Gore 3"), 1f);
			}
		}
		#endregion
	}
}
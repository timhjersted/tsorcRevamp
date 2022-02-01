using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System;

namespace tsorcRevamp.NPCs.Enemies
{
	class Necromancer : ModNPC
	{
		public override void SetDefaults()
		{
			Main.npcFrameCount[npc.type] = 15;
			animationType = 21;
			npc.knockBackResist = 0.2f;
			npc.aiStyle = 3;
			npc.damage = 60;
			npc.defense = 25;
			npc.height = 40;
			npc.width = 20;
			npc.lifeMax = 1580;
			npc.HitSound = SoundID.NPCHit1;
			npc.DeathSound = SoundID.NPCDeath1;
			npc.value = 2700;
			banner = npc.type;
			bannerItem = ModContent.ItemType<Banners.NecromancerBanner>();
		}

		public override void ScaleExpertStats(int numPlayers, float bossLifeScale)
		{
			npc.lifeMax = (int)(npc.lifeMax / 2);
			npc.damage = (int)(npc.damage / 2);
			deathStrikeDamage = (int)(deathStrikeDamage / 2);
		}

		int deathStrikeDamage = 35;

		float strikeTimer;
		float skeletonTimer;
		float skeletonsSpawned;
		//Spawns in the Underground and Cavern before 3.5/10ths and after 7.5/10ths (Width). Does not Spawn in the Jungle, Meteor, or if there are Town NPCs.

		#region Spawn
		public override float SpawnChance(NPCSpawnInfo spawnInfo)
		{
			bool oSky = (spawnInfo.spawnTileY < (Main.maxTilesY * 0.1f));
			bool oSurface = (spawnInfo.spawnTileY >= (Main.maxTilesY * 0.1f) && spawnInfo.spawnTileY < (Main.maxTilesY * 0.2f));
			bool oUnderSurface = (spawnInfo.spawnTileY >= (Main.maxTilesY * 0.2f) && spawnInfo.spawnTileY < (Main.maxTilesY * 0.3f));
			bool oUnderground = (spawnInfo.spawnTileY >= (Main.maxTilesY * 0.3f) && spawnInfo.spawnTileY < (Main.maxTilesY * 0.4f));
			bool oCavern = (spawnInfo.spawnTileY >= (Main.maxTilesY * 0.4f) && spawnInfo.spawnTileY < (Main.maxTilesY * 0.6f));
			bool oMagmaCavern = (spawnInfo.spawnTileY >= (Main.maxTilesY * 0.6f) && spawnInfo.spawnTileY < (Main.maxTilesY * 0.8f));
			bool oUnderworld = (spawnInfo.spawnTileY >= (Main.maxTilesY * 0.8f));

			if (spawnInfo.player.townNPCs > 0f || spawnInfo.player.ZoneJungle || spawnInfo.player.ZoneMeteor) return 0;

			if (spawnInfo.water) return 0f;

			if (!Main.hardMode)
			{
				if (spawnInfo.player.ZoneDungeon && Main.rand.Next(3000) == 1) return 1;
				if (oUnderworld && Main.rand.Next(500) == 1) return 1;
				if (oUnderworld && !Main.dayTime && Main.rand.Next(200) == 1) return 1;
				if ((spawnInfo.spawnTileX < Main.maxTilesX * 0.35f || spawnInfo.spawnTileX > Main.maxTilesX * 0.75f) && (oUnderground || oCavern) && Main.rand.Next(2000) == 1) return 1;
				return 0;
			}
			else if (Main.hardMode)
			{
				if (oUnderworld && Main.dayTime && Main.rand.Next(60) == 1) return 1;
				if (oUnderworld && !Main.dayTime && Main.rand.Next(35) == 1) return 1;
				if (spawnInfo.player.ZoneDungeon && Main.rand.Next(100) == 1) return 1;
				if (spawnInfo.player.ZoneHoly && (oUnderground || oCavern) && Main.rand.Next(120) == 1) return 1;
				if ((spawnInfo.player.ZoneCorrupt || spawnInfo.player.ZoneCrimson) && (oUnderground || oCavern) && Main.rand.Next(220) == 1) return 1;
				return 0;
			}
			return 0;
		}
		#endregion

		public override void AI()
		{
			tsorcRevampAIs.FighterAI(npc, 1.5f, 0.05f, canTeleport: true, lavaJumping: true);

			strikeTimer++;
			skeletonTimer++;
			bool lineOfSight = Collision.CanHit(npc.position, npc.width, npc.height, Main.player[npc.target].position, Main.player[npc.target].width, Main.player[npc.target].height);
			tsorcRevampAIs.SimpleProjectile(npc, ref strikeTimer, 120, ModContent.ProjectileType<Projectiles.Enemy.EnemySpellSuddenDeathStrike>(), deathStrikeDamage, 8, lineOfSight && Main.rand.NextBool(), false, 2, 17, 0);
			if(tsorcRevampAIs.SimpleProjectile(npc, ref strikeTimer, 120, ModContent.ProjectileType<Projectiles.Enemy.EnemySpellEffectHealing>(), 0, 0, !lineOfSight, false, 2, 17, 0))
			{
				npc.life += 10;
				npc.HealEffect(10);
				if (npc.life > npc.lifeMax) npc.life = npc.lifeMax;
			}

			if (npc.justHit)
			{
				strikeTimer = 0;
			}

			if ((skeletonsSpawned < 11) && skeletonTimer > 600 && lineOfSight)
			{
				skeletonTimer = 0;
				skeletonsSpawned += 1;

				if (Main.netMode != NetmodeID.MultiplayerClient) {
					int spawnedNPC = NPC.NewNPC((int)npc.Center.X, (int)npc.Center.Y, NPCID.ArmoredSkeleton, 0);
					if (Main.netMode == NetmodeID.Server)
					{
						NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, spawnedNPC, 0f, 0f, 0f, 0);
					}
				}
			}
		}

		#region Gore
		public override void NPCLoot()
		{
			Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Necromancer Gore 1"), 1.1f);
			Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Necromancer Gore 2"), 1.1f);
			Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Necromancer Gore 3"), 1.1f);
			Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Necromancer Gore 2"), 1.1f);
			Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Necromancer Gore 3"), 1.1f);

			if (Main.rand.Next(99) < 5) Item.NewItem(npc.getRect(), ModContent.ItemType<Items.Accessories.BootsOfHaste>(), 1);
			if (Main.rand.Next(99) < 5) Item.NewItem(npc.getRect(), ModContent.ItemType<Items.Potions.CrimsonPotion>(), 1);
			//if (Main.rand.Next(99) < 5) Item.NewItem(npc.getRect(), ModContent.ItemType<Items.Potions.PiercingPotion>(), 1);
			if (Main.rand.Next(99) < 5) Item.NewItem(npc.getRect(), ModContent.ItemType<Items.Potions.StrengthPotion>(), 1);
			//if (Main.rand.Next(99) < 5) Item.NewItem(npc.getRect(), ModContent.ItemType<Items.Potions.FiresoulPotion>(), 1);
			if (Main.rand.Next(99) < 5) Item.NewItem(npc.getRect(), ModContent.ItemType<Items.Potions.ShockwavePotion>(), 1);
			if (Main.rand.Next(99) < 4) Item.NewItem(npc.getRect(), ModContent.ItemType<Items.Potions.BattlefrontPotion>(), 1);
			if (Main.rand.Next(99) < 4) Item.NewItem(npc.getRect(), ModContent.ItemType<Items.Potions.AttractionPotion>(), 1);
			if (Main.rand.Next(99) < 5) Item.NewItem(npc.getRect(), ItemID.IronskinPotion, 1);
		}
		#endregion
	}
}
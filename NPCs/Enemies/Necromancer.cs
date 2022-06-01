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
			Main.npcFrameCount[NPC.type] = 15;
			animationType = 21;
			NPC.knockBackResist = 0.2f;
			NPC.aiStyle = 3;
			NPC.damage = 60;
			NPC.defense = 25;
			NPC.height = 40;
			NPC.width = 20;
			NPC.lifeMax = 1580;
			NPC.HitSound = SoundID.NPCHit1;
			NPC.DeathSound = SoundID.NPCDeath1;
			NPC.value = 2700;
			banner = NPC.type;
			bannerItem = ModContent.ItemType<Banners.NecromancerBanner>();
		}

		public override void ScaleExpertStats(int numPlayers, float bossLifeScale)
		{
			NPC.lifeMax = (int)(NPC.lifeMax / 2);
			NPC.damage = (int)(NPC.damage / 2);
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
			bool oSky = (spawnInfo.SpawnTileY < (Main.maxTilesY * 0.1f));
			bool oSurface = (spawnInfo.SpawnTileY >= (Main.maxTilesY * 0.1f) && spawnInfo.SpawnTileY < (Main.maxTilesY * 0.2f));
			bool oUnderSurface = (spawnInfo.SpawnTileY >= (Main.maxTilesY * 0.2f) && spawnInfo.SpawnTileY < (Main.maxTilesY * 0.3f));
			bool oUnderground = (spawnInfo.SpawnTileY >= (Main.maxTilesY * 0.3f) && spawnInfo.SpawnTileY < (Main.maxTilesY * 0.4f));
			bool oCavern = (spawnInfo.SpawnTileY >= (Main.maxTilesY * 0.4f) && spawnInfo.SpawnTileY < (Main.maxTilesY * 0.6f));
			bool oMagmaCavern = (spawnInfo.SpawnTileY >= (Main.maxTilesY * 0.6f) && spawnInfo.SpawnTileY < (Main.maxTilesY * 0.8f));
			bool oUnderworld = (spawnInfo.SpawnTileY >= (Main.maxTilesY * 0.8f));

			if (spawnInfo.Player.townNPCs > 0f || spawnInfo.Player.ZoneJungle || spawnInfo.Player.ZoneMeteor) return 0;

			if (spawnInfo.Water) return 0f;

			if (!Main.hardMode)
			{
				if (spawnInfo.Player.ZoneDungeon && Main.rand.Next(3000) == 1) return 1;
				if (oUnderworld && Main.rand.Next(500) == 1) return 1;
				if (oUnderworld && !Main.dayTime && Main.rand.Next(200) == 1) return 1;
				if ((spawnInfo.SpawnTileX < Main.maxTilesX * 0.35f || spawnInfo.SpawnTileX > Main.maxTilesX * 0.75f) && (oUnderground || oCavern) && Main.rand.Next(2000) == 1) return 1;
				return 0;
			}
			else if (Main.hardMode)
			{
				if (oUnderworld && Main.dayTime && Main.rand.Next(60) == 1) return 1;
				if (oUnderworld && !Main.dayTime && Main.rand.Next(35) == 1) return 1;
				if (spawnInfo.Player.ZoneDungeon && Main.rand.Next(100) == 1) return 1;
				if (spawnInfo.Player.ZoneHallow && (oUnderground || oCavern) && Main.rand.Next(120) == 1) return 1;
				if ((spawnInfo.Player.ZoneCorrupt || spawnInfo.Player.ZoneCrimson) && (oUnderground || oCavern) && Main.rand.Next(220) == 1) return 1;
				return 0;
			}
			return 0;
		}
		#endregion

		public override void AI()
		{
			tsorcRevampAIs.FighterAI(NPC, 1.5f, 0.05f, canTeleport: true, lavaJumping: true);

			strikeTimer++;
			skeletonTimer++;
			bool lineOfSight = Collision.CanHit(NPC.position, NPC.width, NPC.height, Main.player[NPC.target].position, Main.player[NPC.target].width, Main.player[NPC.target].height);
			tsorcRevampAIs.SimpleProjectile(NPC, ref strikeTimer, 120, ModContent.ProjectileType<Projectiles.Enemy.EnemySpellSuddenDeathStrike>(), deathStrikeDamage, 8, lineOfSight && Main.rand.NextBool(), false, 2, 17, 0);
			if(tsorcRevampAIs.SimpleProjectile(NPC, ref strikeTimer, 120, ModContent.ProjectileType<Projectiles.Enemy.EnemySpellEffectHealing>(), 0, 0, !lineOfSight, false, 2, 17, 0))
			{
				NPC.life += 10;
				NPC.HealEffect(10);
				if (NPC.life > NPC.lifeMax) NPC.life = NPC.lifeMax;
			}

			if (NPC.justHit)
			{
				strikeTimer = 0;
			}

			if ((skeletonsSpawned < 11) && skeletonTimer > 600 && lineOfSight)
			{
				skeletonTimer = 0;
				skeletonsSpawned += 1;

				if (Main.netMode != NetmodeID.MultiplayerClient) {
					int spawnedNPC = NPC.NewNPC(NPC.GetSource_FromAI(), (int)NPC.Center.X, (int)NPC.Center.Y, NPCID.ArmoredSkeleton, 0);
					if (Main.netMode == NetmodeID.Server)
					{
						NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, spawnedNPC, 0f, 0f, 0f, 0);
					}
				}
			}
		}

		#region Gore
		public override void OnKill()
		{
			Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Gores/Necromancer Gore 1"), 1.1f);
			Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Gores/Necromancer Gore 2"), 1.1f);
			Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Gores/Necromancer Gore 3"), 1.1f);
			Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Gores/Necromancer Gore 2"), 1.1f);
			Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Gores/Necromancer Gore 3"), 1.1f);

			if (Main.rand.Next(99) < 5) Item.NewItem(NPC.getRect(), ModContent.ItemType<Items.Accessories.BootsOfHaste>(), 1);
			if (Main.rand.Next(99) < 5) Item.NewItem(NPC.getRect(), ModContent.ItemType<Items.Potions.CrimsonPotion>(), 1);
			//if (Main.rand.Next(99) < 5) Item.NewItem(npc.getRect(), ModContent.ItemType<Items.Potions.PiercingPotion>(), 1);
			if (Main.rand.Next(99) < 5) Item.NewItem(NPC.getRect(), ModContent.ItemType<Items.Potions.StrengthPotion>(), 1);
			//if (Main.rand.Next(99) < 5) Item.NewItem(npc.getRect(), ModContent.ItemType<Items.Potions.FiresoulPotion>(), 1);
			if (Main.rand.Next(99) < 5) Item.NewItem(NPC.getRect(), ModContent.ItemType<Items.Potions.ShockwavePotion>(), 1);
			if (Main.rand.Next(99) < 4) Item.NewItem(NPC.getRect(), ModContent.ItemType<Items.Potions.BattlefrontPotion>(), 1);
			if (Main.rand.Next(99) < 4) Item.NewItem(NPC.getRect(), ModContent.ItemType<Items.Potions.AttractionPotion>(), 1);
			if (Main.rand.Next(99) < 5) Item.NewItem(NPC.getRect(), ItemID.IronskinPotion, 1);
		}
		#endregion
	}
}
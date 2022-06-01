using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System;

namespace tsorcRevamp.NPCs.Enemies
{
	class NecromancerElemental : ModNPC
	{
		public override void SetDefaults()
		{
			Main.npcFrameCount[NPC.type] = 15;
			AnimationType = 21;
			NPC.knockBackResist = 0f;
			NPC.aiStyle = 3;
			NPC.damage = 70;
			NPC.defense = 30;
			NPC.height = 40;
			NPC.width = 20;
			NPC.lifeMax = 8780;
			NPC.HitSound = SoundID.NPCHit1;
			NPC.DeathSound = SoundID.NPCDeath1;
			NPC.lavaImmune = true;
			NPC.value = 7500;
			banner = NPC.type;
			bannerItem = ModContent.ItemType<Banners.NecromancerElementalBanner>();
		}

		public override void ScaleExpertStats(int numPlayers, float bossLifeScale)
		{
			NPC.lifeMax = (int)(NPC.lifeMax / 2);
			NPC.damage = (int)(NPC.damage / 2);
			deathStrikeDamage = (int)(deathStrikeDamage / 2);
		}

		int deathStrikeDamage = 35;

		//Spawns in the Underground and Cavern before 4.5/10ths and after 7.5/10ths (Width). Does not Spawn in the Jungle, Meteor, or if there are Town NPCs.

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


			if (Main.hardMode && (oUnderground || oCavern))
			{
				if ((spawnInfo.SpawnTileX < Main.maxTilesX * 0.45f || spawnInfo.SpawnTileX > Main.maxTilesX * 0.75f) && Main.rand.Next(350) == 1) return 1;
			}

			else if (Main.hardMode && oUnderworld)
			{
				if (Main.rand.Next(150) == 1) return 1;
			}

			return 0;
		}
		#endregion

		float strikeTimer = 0;
		int chaosElementalTimer;
		int chaosElementalsSpawned = 0;
		public override void AI()
		{
			tsorcRevampAIs.FighterAI(NPC, 1.8f, 0.05f, canTeleport: true, lavaJumping: true);

			strikeTimer++;
			chaosElementalTimer++;
			bool lineOfSight = Collision.CanHit(NPC.position, NPC.width, NPC.height, Main.player[NPC.target].position, Main.player[NPC.target].width, Main.player[NPC.target].height);
			tsorcRevampAIs.SimpleProjectile(NPC, ref strikeTimer, 120, ModContent.ProjectileType<Projectiles.Enemy.EnemySpellSuddenDeathStrike>(), deathStrikeDamage, 8, lineOfSight, false, 2, 17, 0);
			if (tsorcRevampAIs.SimpleProjectile(NPC, ref strikeTimer, 120, ModContent.ProjectileType<Projectiles.Enemy.EnemySpellEffectHealing>(), 0, 0, !lineOfSight, false, 2, 17, 0))
			{
				NPC.life += 50;
				NPC.HealEffect(50);
				if (NPC.life > NPC.lifeMax) NPC.life = NPC.lifeMax;
			}

			if (NPC.justHit)
			{
				strikeTimer = 0;
			}

			if ((chaosElementalsSpawned < 11) && chaosElementalTimer > 300 && lineOfSight)
			{
				chaosElementalTimer = 0;
				chaosElementalsSpawned += 1;

				if (Main.netMode != NetmodeID.MultiplayerClient)
				{
					int spawnedNPC = NPC.NewNPC(NPC.GetSource_FromAI(), (int)NPC.Center.X, (int)NPC.Center.Y, NPCID.ChaosElemental, 0);
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

			Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ItemID.ManaRegenerationPotion, 1);
			Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ItemID.GreaterHealingPotion, 1);
			if (Main.rand.Next(99) < 50) Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ItemID.IronskinPotion, 1);
			if (Main.rand.Next(99) < 20) Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ItemID.MagicPowerPotion, 1);
			if (Main.rand.Next(99) < 20) Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ItemID.RegenerationPotion, 1);
			if (Main.rand.Next(99) < 2) Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.Potions.CrimsonPotion>(), 1);
			//if (Main.rand.Next(99) < 2) Item.NewItem(NPC.GetSource_Loot(), npc.getRect(), ModContent.ItemType<Items.Potions.PiercingPotion>(), 1);
			if (Main.rand.Next(99) < 2) Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.Potions.StrengthPotion>(), 1);
			//if (Main.rand.Next(99) < 2) Item.NewItem(NPC.GetSource_Loot(), npc.getRect(), ModContent.ItemType<Items.Potions.FiresoulPotion>(), 1);
			if (Main.rand.Next(99) < 2) Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.Potions.ShockwavePotion>(), 1);
			if (Main.rand.Next(99) < 2) Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.Potions.BattlefrontPotion>(), 1);
		}
		#endregion
	}
}
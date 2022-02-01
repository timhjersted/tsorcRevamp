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
			Main.npcFrameCount[npc.type] = 15;
			animationType = 21;
			npc.knockBackResist = 0f;
			npc.aiStyle = 3;
			npc.damage = 70;
			npc.defense = 30;
			npc.height = 40;
			npc.width = 20;
			npc.lifeMax = 8780;
			npc.HitSound = SoundID.NPCHit1;
			npc.DeathSound = SoundID.NPCDeath1;
			npc.lavaImmune = true;
			npc.value = 7500;
			banner = npc.type;
			bannerItem = ModContent.ItemType<Banners.NecromancerElementalBanner>();
		}

		public override void ScaleExpertStats(int numPlayers, float bossLifeScale)
		{
			npc.lifeMax = (int)(npc.lifeMax / 2);
			npc.damage = (int)(npc.damage / 2);
			deathStrikeDamage = (int)(deathStrikeDamage / 2);
		}

		int deathStrikeDamage = 35;

		//Spawns in the Underground and Cavern before 4.5/10ths and after 7.5/10ths (Width). Does not Spawn in the Jungle, Meteor, or if there are Town NPCs.

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


			if (Main.hardMode && (oUnderground || oCavern))
			{
				if ((spawnInfo.spawnTileX < Main.maxTilesX * 0.45f || spawnInfo.spawnTileX > Main.maxTilesX * 0.75f) && Main.rand.Next(350) == 1) return 1;
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
			tsorcRevampAIs.FighterAI(npc, 1.8f, 0.05f, canTeleport: true, lavaJumping: true);

			strikeTimer++;
			chaosElementalTimer++;
			bool lineOfSight = Collision.CanHit(npc.position, npc.width, npc.height, Main.player[npc.target].position, Main.player[npc.target].width, Main.player[npc.target].height);
			tsorcRevampAIs.SimpleProjectile(npc, ref strikeTimer, 120, ModContent.ProjectileType<Projectiles.Enemy.EnemySpellSuddenDeathStrike>(), deathStrikeDamage, 8, lineOfSight, false, 2, 17, 0);
			if (tsorcRevampAIs.SimpleProjectile(npc, ref strikeTimer, 120, ModContent.ProjectileType<Projectiles.Enemy.EnemySpellEffectHealing>(), 0, 0, !lineOfSight, false, 2, 17, 0))
			{
				npc.life += 50;
				npc.HealEffect(50);
				if (npc.life > npc.lifeMax) npc.life = npc.lifeMax;
			}

			if (npc.justHit)
			{
				strikeTimer = 0;
			}

			if ((chaosElementalsSpawned < 11) && chaosElementalTimer > 300 && lineOfSight)
			{
				chaosElementalTimer = 0;
				chaosElementalsSpawned += 1;

				if (Main.netMode != NetmodeID.MultiplayerClient)
				{
					int spawnedNPC = NPC.NewNPC((int)npc.Center.X, (int)npc.Center.Y, NPCID.ChaosElemental, 0);
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

			Item.NewItem(npc.getRect(), ItemID.ManaRegenerationPotion, 1);
			Item.NewItem(npc.getRect(), ItemID.GreaterHealingPotion, 1);
			if (Main.rand.Next(99) < 50) Item.NewItem(npc.getRect(), ItemID.IronskinPotion, 1);
			if (Main.rand.Next(99) < 20) Item.NewItem(npc.getRect(), ItemID.MagicPowerPotion, 1);
			if (Main.rand.Next(99) < 20) Item.NewItem(npc.getRect(), ItemID.RegenerationPotion, 1);
			if (Main.rand.Next(99) < 2) Item.NewItem(npc.getRect(), ModContent.ItemType<Items.Potions.CrimsonPotion>(), 1);
			//if (Main.rand.Next(99) < 2) Item.NewItem(npc.getRect(), ModContent.ItemType<Items.Potions.PiercingPotion>(), 1);
			if (Main.rand.Next(99) < 2) Item.NewItem(npc.getRect(), ModContent.ItemType<Items.Potions.StrengthPotion>(), 1);
			//if (Main.rand.Next(99) < 2) Item.NewItem(npc.getRect(), ModContent.ItemType<Items.Potions.FiresoulPotion>(), 1);
			if (Main.rand.Next(99) < 2) Item.NewItem(npc.getRect(), ModContent.ItemType<Items.Potions.ShockwavePotion>(), 1);
			if (Main.rand.Next(99) < 2) Item.NewItem(npc.getRect(), ModContent.ItemType<Items.Potions.BattlefrontPotion>(), 1);
		}
		#endregion
	}
}
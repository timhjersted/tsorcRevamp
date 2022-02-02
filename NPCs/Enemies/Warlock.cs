using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System;

namespace tsorcRevamp.NPCs.Enemies
{
	class Warlock : ModNPC
	{
		public override void SetDefaults()
		{
			Main.npcFrameCount[npc.type] = 15;
			animationType = 21;
			npc.knockBackResist = 0.2f;
			npc.aiStyle = 3;
			npc.damage = 65;
			npc.npcSlots = 5;
			npc.defense = 5;
			npc.height = 40;
			npc.width = 20;
			npc.lifeMax = 2600;
			npc.scale = 1.1f;
			npc.HitSound = SoundID.NPCHit1;
			npc.DeathSound = SoundID.NPCDeath1;
			npc.value = 6700;
			npc.rarity = 3;
			banner = npc.type;
			bannerItem = ModContent.ItemType<Banners.WarlockBanner>();
			if (!(Main.hardMode || ModContent.GetInstance<tsorcRevampConfig>().LegacyMode))
			{
				npc.lifeMax = 2000;
			}
		}

		public override void ScaleExpertStats(int numPlayers, float bossLifeScale)
		{
			npc.lifeMax = (int)(npc.lifeMax / 2);
			npc.damage = (int)(npc.damage / 2);
			greatEnergyBeamDamage = (int)(greatEnergyBeamDamage / 2);
			energyBallDamage = (int)(energyBallDamage / 2);
		}

		int greatEnergyBeamDamage = 35;
		int energyBallDamage = 35;



		//Spawn in the Cavern, mostly before 3/10th and after 7/10th (Width). Does not spawn in the Dungeon, Jungle, Meteor, or if there are Town NPCs
		#region Spawn
		
		public override float SpawnChance(NPCSpawnInfo spawnInfo)
		{
			if (!NPC.downedBoss1)
			{
				return 0;
			}

			Player P = spawnInfo.player; //These are mostly redundant with the new zone definitions, but it still works.
			bool Meteor = P.ZoneMeteor;
			bool Jungle = P.ZoneJungle;
			bool Dungeon = P.ZoneDungeon;
			bool Corruption = (P.ZoneCorrupt || P.ZoneCrimson);
			bool Hallow = P.ZoneHoly;
			bool AboveEarth = P.ZoneOverworldHeight;
			bool oUnderground = P.ZoneDirtLayerHeight;
			bool oCavern = P.ZoneRockLayerHeight;
			bool InHell = P.ZoneUnderworldHeight;
			bool Ocean = spawnInfo.spawnTileX < 3600 || spawnInfo.spawnTileX > (Main.maxTilesX - 100) * 16;
			// P.townNPCs > 0f // is no town NPCs nearby

			//if (spawnInfo.player.townNPCs > 0f || spawnInfo.player.ZoneMeteor) return 0;
			if (!Main.hardMode && oCavern)
			{
				if (Main.rand.Next(1000) == 1) return 1;
				else if ((spawnInfo.spawnTileX < Main.maxTilesX * 0.3f || spawnInfo.spawnTileX > Main.maxTilesX * 0.7f) && Main.rand.Next(400) == 1)
				{
					Main.NewText("A Warlock is near... ", 175, 75, 255);
					return 1;
				}

			}
			if (Main.hardMode && (oCavern || oUnderground || Jungle))
			{
				if (Main.rand.Next(180) == 1) return 1;
				else if ((spawnInfo.spawnTileX < Main.maxTilesX * 0.3f || spawnInfo.spawnTileX > Main.maxTilesX * 0.7f) && Main.rand.Next(150) == 1)
				{
					Main.NewText("A Warlock is hunting you... ", 175, 75, 255);
					return 1;
				}
			}
			return 0;
		}
		#endregion

		float attackTimer = 0;
		public override void AI()
		{
			tsorcRevampAIs.FighterAI(npc, 2.8f, 0.05f, .2f, canTeleport: true, lavaJumping: true);

			bool clearShot = Collision.CanHit(npc.position, npc.width, npc.height, Main.player[npc.target].position, Main.player[npc.target].width, Main.player[npc.target].height) && Vector2.Distance(npc.Center, Main.player[npc.target].Center) <= 1000;

			tsorcRevampAIs.SimpleProjectile(npc, ref attackTimer, 120, ModContent.ProjectileType<Projectiles.Enemy.EnemySpellGreatEnergyBall>(), energyBallDamage, 8, clearShot && Main.rand.NextBool(), soundType: 2, soundStyle: 17);
			tsorcRevampAIs.SimpleProjectile(npc, ref attackTimer, 120, ModContent.ProjectileType<Projectiles.Enemy.EnemySpellGreatEnergyBeamBall>(), greatEnergyBeamDamage, 8, clearShot, false, soundType: 2, soundStyle: 17);
			
			if(tsorcRevampAIs.SimpleProjectile(npc, ref attackTimer, 120, ModContent.ProjectileType<Projectiles.Enemy.EnemySpellEffectHealing>(), 1, 0, !clearShot, false, soundType: 2, soundStyle: 17))
            {
				npc.life += 10;
				npc.HealEffect(10);
				if (npc.life > npc.lifeMax) npc.life = npc.lifeMax;
			}

			if (Main.rand.Next(230) == 1)
			{
				npc.alpha = 0;
				npc.netUpdate = true;
			}
			if (Main.rand.Next(150) == 1)
			{
				npc.alpha = 215;
				npc.netUpdate = true;
			}			

			Lighting.AddLight((int)npc.position.X / 16, (int)npc.position.Y / 16, 0.4f, 0.4f, 0.4f);

			if (Main.rand.Next(600) == 0)
			{
				NPC.NewNPC((int)npc.Center.X, (int)npc.Center.Y, NPCID.IlluminantBat);
			}

			if (attackTimer > 60)
			{
				for (int j = 0; j < 10; j++)
				{
					Vector2 dir = Main.rand.NextVector2CircularEdge(32, 32);
					Vector2 dustPos = npc.Center + dir;
					Vector2 dustVel = new Vector2(5, 0).RotatedBy(dir.ToRotation() + MathHelper.Pi / 2);
					Dust.NewDustPerfect(dustPos, DustID.MagicMirror, dustVel, 200).noGravity = true;
				}
			}
		}

		#region Gore
		public override void NPCLoot()
		{
			Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Warlock Gore 1"), 1.1f);
			Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Warlock Gore 2"), 1.1f);
			Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Warlock Gore 3"), 1.1f);
			Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Warlock Gore 2"), 1.1f);
			Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Warlock Gore 3"), 1.1f);			
		}
		#endregion
	}
}
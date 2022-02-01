using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System;

namespace tsorcRevamp.NPCs.Enemies
{
	class DarkElfMage : ModNPC
	{
		public override void SetDefaults()
		{
			Main.npcFrameCount[npc.type] = 16;
			animationType = 28;
			npc.knockBackResist = 0.01f;
			npc.aiStyle = 3;
			npc.damage = 76;
			npc.defense = 35;
			npc.height = 40;
			npc.width = 20;
			npc.lifeMax = 810;
			npc.HitSound = SoundID.NPCHit1;
			npc.DeathSound = SoundID.NPCDeath1;
			npc.value = 1800;
			banner = npc.type;
			bannerItem = ModContent.ItemType<Banners.DarkElfMageBanner>();
		}

		public override void ScaleExpertStats(int numPlayers, float bossLifeScale)
		{
			npc.lifeMax = (int)(npc.lifeMax / 2);
			npc.damage = (int)(npc.damage / 2);
			meteorDamage = (int)(meteorDamage / 2);
			iceBallDamage = (int)(iceBallDamage / 2);
			iceStormDamage = (int)(iceStormDamage / 2);
			lightningDamage = (int)(lightningDamage / 2);
		}

		int meteorDamage = 17;
		int iceBallDamage = 40;
		int iceStormDamage = 35;
		int lightningDamage = 35;




		//Spawns in Hardmode Surface and Underground, 6.5/10th of the world to the right edge (Width). Does not spawn in Dungeons, Jungle, or Meteor. Only spawns with Town NPCs during Blood Moons.

		#region Spawn
		public override float SpawnChance(NPCSpawnInfo spawnInfo)
		{
			Player P = spawnInfo.player; //this shortens our code up from writing this line over and over.

			bool Sky = spawnInfo.spawnTileY <= (Main.rockLayer * 4);
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

			// these are all the regular stuff you get , now lets see......
			if (spawnInfo.player.townNPCs > 0f) return 0;

			if (Main.hardMode && !Meteor && !Jungle && !Dungeon && !Corruption && Hallow && Main.rand.Next(55) == 1) return 1;

			if (Main.hardMode && !Meteor && !Jungle && !Dungeon && !Corruption && Hallow && InBrownLayer && Main.rand.Next(35) == 1) return 1;

			if (Main.hardMode && !Meteor && !Jungle && !Dungeon && !Corruption && Hallow && InGrayLayer && Main.rand.Next(25) == 1) return 1;

			if (Main.hardMode && FrozenOcean && Main.rand.Next(20) == 1) return 1;


			return 0;
		}
		#endregion


		public override void AI()
		{
			tsorcRevampAIs.FighterAI(npc, 2, 0.07f, 0.2f, true, enragePercent: 0.2f, enrageTopSpeed: 3);
			tsorcRevampAIs.LeapAtPlayer(npc, 4, 3, 1, 100);

			npc.localAI[1]++;
			bool validTarget = Collision.CanHit(npc.position, npc.width, npc.height, Main.player[npc.target].position, Main.player[npc.target].width, Main.player[npc.target].height);
			tsorcRevampAIs.SimpleProjectile(npc, ref npc.localAI[1], 90, ModContent.ProjectileType<Projectiles.Enemy.EnemySpellLightning3Ball>(), lightningDamage, 9, validTarget, false, 2, 17, 0.1f, 120, 1);
			tsorcRevampAIs.SimpleProjectile(npc, ref npc.localAI[1], 90, ModContent.ProjectileType<Projectiles.Enemy.EnemySpellIcestormBall>(), iceStormDamage, 8, validTarget, false, 2, 17);

			
			if (npc.localAI[1] >= 90 && validTarget)
			{
				npc.localAI[1] = 0;
				if (Main.netMode != NetmodeID.MultiplayerClient)
				{
					Vector2 overshoot = new Vector2(0, -240);
					Vector2 projectileVector = UsefulFunctions.BallisticTrajectory(npc.Center, Main.player[npc.target].Center + overshoot, 12, 0.035f);
					Projectile.NewProjectile(npc.Center.X, npc.Center.Y, projectileVector.X, projectileVector.Y, ModContent.ProjectileType<Projectiles.Enemy.EnemySpellIce3Ball>(), iceBallDamage, 0f, Main.myPlayer, 1, npc.target);
				}
				
				Main.PlaySound(2, (int)npc.position.X, (int)npc.position.Y, 17);
				
			}
		}

		#region Gore
		public override void NPCLoot()
		{
			
			Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Dark Elf Magi Gore 1"), 1f);
			Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Dark Elf Magi Gore 2"), 1f);
			Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Dark Elf Magi Gore 3"), 1f);
			Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Dark Elf Magi Gore 2"), 1f);
			Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Dark Elf Magi Gore 3"), 1f);

			if (Main.rand.Next(100) < 5) Item.NewItem(npc.getRect(), ModContent.ItemType<Items.Weapons.Melee.ForgottenIceRod>());
			if (Main.rand.Next(100) < 5) Item.NewItem(npc.getRect(), ModContent.ItemType<Items.Weapons.Melee.ForgottenThunderRod>());
			if (Main.rand.Next(100) < 1) Item.NewItem(npc.getRect(), ModContent.ItemType<Items.Weapons.Melee.ForgottenStardustRod>());
			if (Main.rand.Next(100) < 10) Item.NewItem(npc.getRect(), ItemID.IronskinPotion);
			if (Main.rand.Next(100) < 20) Item.NewItem(npc.getRect(), ItemID.ManaRegenerationPotion);
			if (Main.rand.Next(100) < 10) Item.NewItem(npc.getRect(), ItemID.GreaterHealingPotion);
			if (Main.rand.Next(100) < 6) Item.NewItem(npc.getRect(), ItemID.GillsPotion);
			if (Main.rand.Next(100) < 6) Item.NewItem(npc.getRect(), ItemID.HunterPotion);
			if (Main.rand.Next(100) < 20) Item.NewItem(npc.getRect(), ItemID.MagicPowerPotion);
			if (Main.rand.Next(100) < 20) Item.NewItem(npc.getRect(), ItemID.ShinePotion);

		}
		#endregion

	}
}
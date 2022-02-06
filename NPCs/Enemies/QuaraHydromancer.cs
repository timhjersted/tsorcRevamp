using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System;
using Microsoft.Xna.Framework.Graphics;

namespace tsorcRevamp.NPCs.Enemies
{
	class QuaraHydromancer : ModNPC
	{

		int bubbleDamage = 60;
		public override void SetDefaults()
		{
			Main.npcFrameCount[npc.type] = 15;
			animationType = 21;
			npc.aiStyle = 3;
			npc.damage = 65;
			npc.defense = 22;
			npc.height = 45;
			npc.lifeMax = 500;
			npc.HitSound = SoundID.NPCHit1;
			npc.DeathSound = SoundID.NPCDeath1;
			npc.value = 1500;
			npc.width = 18;
			npc.lavaImmune = true;
			npc.knockBackResist = 0.25f;
			banner = npc.type;
			bannerItem = ModContent.ItemType<Banners.QuaraHydromancerBanner>();

			if (Main.hardMode) { npc.lifeMax = 1000; npc.defense = 22; npc.damage = 125; npc.value = 1500; bubbleDamage = 70; }
			if (tsorcRevampWorld.SuperHardMode) { npc.lifeMax = 3000; npc.defense = 50; npc.damage = 160; npc.value = 3600; bubbleDamage = 80; }
		}

		public override void ScaleExpertStats(int numPlayers, float bossLifeScale)
		{
			npc.lifeMax = (int)(npc.lifeMax / 2);
			npc.damage = (int)(npc.damage / 2);
			bubbleDamage = (int)(bubbleDamage / 2);
		}

		

		float bubbleTimer;

		#region Spawn
		public override float SpawnChance(NPCSpawnInfo spawnInfo)
		{
			Player P = spawnInfo.player;

			if (spawnInfo.water) return 0f;

			//now spawns in hallow, since jungle was getting crowded
			//spawns more before the rage is defeated
			
			if (Main.hardMode && tsorcRevampWorld.Slain.ContainsKey(ModContent.NPCType<NPCs.Bosses.TheRage>()) && !Main.dayTime && P.ZoneHoly && P.ZoneOverworldHeight && Main.rand.Next(30) == 1) return 1;
			if (Main.hardMode && tsorcRevampWorld.Slain.ContainsKey(ModContent.NPCType<NPCs.Bosses.TheRage>()) && !Main.dayTime && P.ZoneHoly && (P.ZoneRockLayerHeight || P.ZoneDirtLayerHeight) && Main.rand.Next(25) == 1) return 1;
			if (Main.hardMode && tsorcRevampWorld.Slain.ContainsKey(ModContent.NPCType<NPCs.Bosses.TheRage>()) && Main.dayTime && P.ZoneHoly && (P.ZoneRockLayerHeight || P.ZoneDirtLayerHeight) && Main.rand.Next(35) == 1) return 1;
			if (Main.hardMode && !tsorcRevampWorld.Slain.ContainsKey(ModContent.NPCType<NPCs.Bosses.TheRage>()) && P.ZoneHoly && (P.ZoneRockLayerHeight || P.ZoneDirtLayerHeight) && Main.rand.Next(10) == 1) return 1;
			if (Main.hardMode && spawnInfo.lihzahrd && Main.rand.Next(45) == 1) return 1;
			if (Main.hardMode && spawnInfo.player.ZoneDesert && Main.rand.Next(45) == 1) return 1;
			if (tsorcRevampWorld.SuperHardMode && P.ZoneHoly && Main.rand.Next(10) == 1) return 1;
			if (tsorcRevampWorld.SuperHardMode && spawnInfo.player.ZoneGlowshroom && Main.rand.Next(5) == 1) return 1;
			return 0;
		}
		#endregion

		int inkJetCooldown = 0;
		public override void AI()
		{
			tsorcRevampAIs.FighterAI(npc, 2, 0.05f, canTeleport: false, lavaJumping: true);
			bool lineOfSight = Collision.CanHitLine(npc.Center, 0, 0, Main.player[npc.target].Center, 0, 0);
			tsorcRevampAIs.SimpleProjectile(npc, ref bubbleTimer, 80, ModContent.ProjectileType<Projectiles.Enemy.Bubble>(), bubbleDamage, 6, lineOfSight, true, 2, 87, 0); //2, 87 is bubble 2 sound

			if (Main.GameUpdateCount % 600 == 0 && tsorcRevampWorld.SuperHardMode & Main.netMode != NetmodeID.MultiplayerClient)
			{
				Projectile.NewProjectileDirect(npc.Center, Vector2.Zero, ModContent.ProjectileType<Projectiles.Enemy.InkGeyser>(), bubbleDamage, 0, Main.myPlayer);
				inkJetCooldown = 120;
			}

			if(inkJetCooldown > 0)
            {
				npc.velocity = Vector2.Zero;
				inkJetCooldown--;
            }


			//projectile sound needs volume and pitch variables (the last two below)
			//if (bubbleTimer >= 80)
			//{
			//	Main.PlaySound(2, (int)npc.position.X, (int)npc.position.Y, 87, 0.2f, -0.1f); // water2 sound
			//}


			//PLAY CREATURE SOUND
			if (Main.rand.Next(1000) == 1)
			{
				Main.PlaySound(23, (int)npc.position.X, (int)npc.position.Y, 0, 0.3f, -0.3f); // water sound
			}


			//JUSTHIT CODE

			Player player2 = Main.player[npc.target];
			if (npc.justHit && npc.Distance(player2.Center) < 100)
			{
				bubbleTimer = 0f;
			}
			if (npc.justHit && npc.Distance(player2.Center) < 150 && Main.rand.Next(2) == 1)
			{
				bubbleTimer = 40f;
				npc.velocity.Y = Main.rand.NextFloat(-11f, -3f);
				npc.velocity.X = npc.velocity.X + (float)npc.direction * Main.rand.NextFloat(-4f, -3f);
				npc.netUpdate = true;
			}
			if (npc.justHit && npc.Distance(player2.Center) > 200 && Main.rand.Next(2) == 1)
			{
				npc.velocity.Y = Main.rand.NextFloat(-11f, -3f);
				npc.velocity.X = npc.velocity.X + (float)npc.direction * Main.rand.NextFloat(4f, 3f);
				npc.netUpdate = true;
			}

			//TELEGRAPH DUST
			if (bubbleTimer >= 40)
			{
				Lighting.AddLight(npc.Center, Color.Blue.ToVector3());

				for (int j = 0; j < bubbleTimer - 39; j++)
				{
					Vector2 dir = Main.rand.NextVector2CircularEdge(48, 64);
					Vector2 dustPos = npc.Center + dir;
					Vector2 dustVel = dir * -1;
					dustVel.Normalize();
					dustVel *= 3;
					Dust.NewDustPerfect(dustPos, 29, dustVel, 200).noGravity = true;
				}
			}
		}



		#region Gore
		public override void NPCLoot()
		{
			Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Quara Hydromancer Gore 1"), 1.2f);
			Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Quara Hydromancer Gore 2"), 1.2f);
			Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Quara Hydromancer Gore 3"), 1.2f);
			Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Quara Hydromancer Gore 2"), 1.2f);
			Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Quara Hydromancer Gore 3"), 1.2f);

			//if (Main.rand.Next(99) < 10) Item.NewItem(npc.getRect(), ItemID.HealingPotion, 1);
			if (Main.rand.Next(99) < 2) Item.NewItem(npc.getRect(), ModContent.ItemType<Items.Weapons.Magic.GreatEnergyBeamScroll>(), 1);
			if (Main.rand.Next(99) < 5) Item.NewItem(npc.getRect(), ItemID.ManaRegenerationPotion, 1);
			if (Main.rand.Next(99) < 8) Item.NewItem(npc.getRect(), ItemID.GreaterHealingPotion, 1);
			if (Main.rand.Next(99) < 5) Item.NewItem(npc.getRect(), ItemID.IronskinPotion, 1);
			if (Main.rand.Next(99) < 5) Item.NewItem(npc.getRect(), ItemID.SwiftnessPotion, 1);
			if (Main.rand.Next(99) < 3) Item.NewItem(npc.getRect(), ItemID.WaterWalkingPotion, 1);
			if (Main.rand.Next(99) < 3) Item.NewItem(npc.getRect(), ItemID.BattlePotion, 1);
		}
		#endregion
	}
}
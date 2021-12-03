using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.NPCs.Bosses.SuperHardMode
{
	[AutoloadBossHead]
	class AbysmalOolacileSorcerer : ModNPC
	{
		public override void SetDefaults()
		{
			npc.npcSlots = 180;
			Main.npcFrameCount[npc.type] = 3;
			animationType = 29;
			npc.aiStyle = 0;
			npc.damage = 96;
			npc.defense = 127;
			npc.height = 44;
			npc.timeLeft = 22500;
			npc.lifeMax = 156800;
			npc.HitSound = SoundID.NPCHit1;
			npc.DeathSound = SoundID.NPCDeath6;
			npc.boss = true;
			npc.noGravity = false;
			npc.noTileCollide = false;
			npc.lavaImmune = true;
			npc.value = 430000;
			npc.width = 28;
			npc.knockBackResist = 0.1f;
			npc.buffImmune[BuffID.Poisoned] = true;
			npc.buffImmune[BuffID.Confused] = true;
			npc.buffImmune[BuffID.OnFire] = true;
			bossBag = ModContent.ItemType<Items.BossBags.OolacileSorcererBag>();
			despawnHandler = new NPCDespawnHandler("The Abysmal Oolacile Sorcerer has shattered your mind...", Color.DarkRed, DustID.Firework_Red);
		}


		int darkBeadDamage = 81;
		int darkOrbDamage = 94;
		int seekerDamage = 69;

		public override void ScaleExpertStats(int numPlayers, float bossLifeScale)
		{
			npc.damage = (int)(npc.damage / 2);
			darkBeadDamage = (int)(darkBeadDamage / 2);
			darkOrbDamage = (int)(darkOrbDamage / 2);
			seekerDamage = (int)(seekerDamage / 2);
		}

		public float DarkBeadShotTimer
		{
			get => npc.ai[0];
			set => npc.ai[0] = value;
		}
		public float TeleportTimer
		{
			get => npc.ai[1];
			set => npc.ai[1] = value;
		}
		public float DarkBeadShotCounter
		{
			get => npc.ai[2];
			set => npc.ai[2] = value;
		}
		public float SecondAttackCounter
		{
			get => npc.ai[3];
			set => npc.ai[3] = value;
		}

		float NPCSpawningTimer;
		float customspawn1;
		float customspawn2;
		float customspawn3;

		#region Spawn
		public override float SpawnChance(NPCSpawnInfo spawnInfo)
		{
			Player P = spawnInfo.player;
			bool Meteor = P.ZoneMeteor;
			bool Jungle = P.ZoneJungle;
			bool Dungeon = P.ZoneDungeon;
			bool Corruption = (P.ZoneCorrupt || P.ZoneCrimson);
			bool Hallow = P.ZoneHoly;
			bool AboveEarth = P.position.Y < Main.worldSurface;
			bool InBrownLayer = P.position.Y >= Main.worldSurface && P.position.Y < Main.rockLayer;
			bool InGrayLayer = P.position.Y >= Main.rockLayer && P.position.Y < (Main.maxTilesY - 200) * 16;
			bool InHell = P.position.Y >= (Main.maxTilesY - 200) * 16;
			bool Ocean = P.position.X < 3600 || P.position.X > (Main.maxTilesX - 100) * 16;

			// these are all the regular stuff you get , now lets see......

			if (tsorcRevampWorld.SuperHardMode && !Main.dayTime && tsorcRevampWorld.Slain.ContainsKey(ModContent.NPCType<Witchking>()) && !tsorcRevampWorld.Slain.ContainsKey(ModContent.NPCType<AbysmalOolacileSorcerer>()) && AboveEarth && Main.rand.Next(200) == 1) return 1;

			if (tsorcRevampWorld.SuperHardMode && !Main.dayTime && tsorcRevampWorld.Slain.ContainsKey(ModContent.NPCType<Witchking>()) && !tsorcRevampWorld.Slain.ContainsKey(ModContent.NPCType<AbysmalOolacileSorcerer>()) && InBrownLayer && Main.rand.Next(500) == 1) return 1;

			if (tsorcRevampWorld.SuperHardMode && Main.bloodMoon && tsorcRevampWorld.Slain.ContainsKey(ModContent.NPCType<Witchking>()) && !tsorcRevampWorld.Slain.ContainsKey(ModContent.NPCType<AbysmalOolacileSorcerer>()) && AboveEarth && Main.rand.Next(50) == 1) return 1;

			if (tsorcRevampWorld.SuperHardMode && Main.bloodMoon && tsorcRevampWorld.Slain.ContainsKey(ModContent.NPCType<Witchking>()) && tsorcRevampWorld.Slain.ContainsKey(ModContent.NPCType<AbysmalOolacileSorcerer>()) && !tsorcRevampWorld.Slain.ContainsKey(ModContent.NPCType<Artorias>()) && AboveEarth && Main.rand.Next(2850) == 1) return 1;

			return 0;
		}
		#endregion


		#region AI
		NPCDespawnHandler despawnHandler;
		public override void AI()
		{
			despawnHandler.TargetAndDespawn(npc.whoAmI);

			DarkBeadShotTimer++;
			TeleportTimer++;
			SecondAttackCounter++;

			if (npc.life > npc.lifeMax / 4)
			{
				int dust = Dust.NewDust(new Vector2((float)npc.position.X, (float)npc.position.Y), npc.width, npc.height, 54, npc.velocity.X, npc.velocity.Y, 210, Color.Black, 2f);
				Main.dust[dust].noGravity = true;
			}
			else
			{
				int dust = Dust.NewDust(new Vector2((float)npc.position.X, (float)npc.position.Y), npc.width, npc.height, 54, npc.velocity.X, npc.velocity.Y, 140, Color.Black, 3f);
				Main.dust[dust].noGravity = true;
			}

			if (Main.netMode != NetmodeID.MultiplayerClient)
			{
				SpawnNPCs();
				FireProjectiles();
			}

			if (TeleportTimer >= 20) //How long it should float after teleporting before coming to a stop
			{
				npc.velocity.X *= 0.27f;
				npc.velocity.Y *= 0.17f;
			}

			OolacileTeleport();
		}

		public void SpawnNPCs()
        {
			NPCSpawningTimer += (Main.rand.Next(2, 5) * 0.1f);
			if (NPCSpawningTimer >= 10f)
			{
				if ((NPC.CountNPCS(ModContent.NPCType<Enemies.SuperHardMode.BarrowWightPhantom>()) < 200) && Main.rand.Next(130) == 1)
				{
					int Spawned = NPC.NewNPC((int)npc.position.X + (npc.width / 2), (int)npc.position.Y + (npc.height / 2), ModContent.NPCType<NPCs.Enemies.SuperHardMode.BarrowWightPhantom>(), 0);
					Main.npc[Spawned].velocity.Y = -8;
					Main.npc[Spawned].velocity.X = Main.rand.Next(-10, 10) / 10;
					if (Main.netMode == 2)
					{
						NetMessage.SendData(23, -1, -1, null, Spawned, 0f, 0f, 0f, 0);
						
					}
				}
				if ((NPC.CountNPCS(ModContent.NPCType<Enemies.SuperHardMode.BarrowWightNemesis>()) < 2) && Main.rand.Next(3000) == 1)
				{
					int Spawned = NPC.NewNPC((int)npc.position.X + (npc.width / 2), (int)npc.position.Y + (npc.height / 2), ModContent.NPCType<NPCs.Enemies.SuperHardMode.BarrowWightNemesis>(), 0);
					Main.npc[Spawned].velocity.Y = -8;
					Main.npc[Spawned].velocity.X = Main.rand.Next(-10, 10) / 10;
					customspawn2 += 1f;
					if (Main.netMode == 2)
					{
						NetMessage.SendData(23, -1, -1, null, Spawned, 0f, 0f, 0f, 0);
					}
				}
				if ((NPC.CountNPCS(ModContent.NPCType<Enemies.SuperHardMode.TaurusKnight>()) < 1) && Main.rand.Next(2050) == 1)
				{
					int Spawned = NPC.NewNPC((int)npc.position.X + (npc.width / 2), (int)npc.position.Y + (npc.height / 2), ModContent.NPCType<NPCs.Enemies.SuperHardMode.TaurusKnight>(), 0);
					Main.npc[Spawned].velocity.Y = -8;
					Main.npc[Spawned].velocity.X = Main.rand.Next(-10, 10) / 10;
					customspawn3 += 1f;
					if (Main.netMode == 2)
					{
						NetMessage.SendData(23, -1, -1, null, Spawned, 0f, 0f, 0f, 0);
					}
				}
			}
		}

		public void FireProjectiles()
        {
			if (DarkBeadShotTimer >= 12 && DarkBeadShotCounter < 5)
			{
				Vector2 projVelocity = UsefulFunctions.GenerateTargetingVector(npc.Center, Main.player[npc.target].Center, 7);
				Projectile.NewProjectile(npc.Center.X, npc.Center.Y, projVelocity.X, projVelocity.Y, ModContent.ProjectileType<Projectiles.Enemy.OolacileDarkBead>(), darkBeadDamage, 0f, Main.myPlayer);
				Main.PlaySound(2, (int)npc.position.X, (int)npc.position.Y, 0x11);
				DarkBeadShotTimer = 0;
				DarkBeadShotCounter++;
			}

			if (SecondAttackCounter >= 60) //how often the crystal attack can happen in frames per second
			{
				if (Main.rand.Next(20) == 0) //1 in 2 chance boss will use attack when it flies down on top of you
				{
					Vector2 projVelocity = UsefulFunctions.GenerateTargetingVector(npc.Center, Main.player[npc.target].Center, 2);
					projVelocity.Y -= 520;
					Projectile.NewProjectile(npc.Center.X, npc.Center.Y, projVelocity.X, projVelocity.Y, ModContent.ProjectileType<Projectiles.Enemy.OolacileDarkOrb>(), darkOrbDamage, 0f, Main.myPlayer);
					Main.PlaySound(2, (int)npc.position.X, (int)npc.position.Y, 25);
					NPCSpawningTimer = 1f;
					SecondAttackCounter = 0;
				}

				if (Main.rand.Next(16) == 1)
				{
					Vector2 projVelocity = UsefulFunctions.GenerateTargetingVector(npc.Center, Main.player[npc.target].Center, 8);
					Projectile.NewProjectile(npc.Center.X, npc.Center.Y, projVelocity.X, projVelocity.Y, ModContent.ProjectileType<Projectiles.Enemy.OolacileSeeker>(), seekerDamage, 0f, Main.myPlayer);
					Main.PlaySound(2, (int)npc.position.X, (int)npc.position.Y, 0x11);
					NPCSpawningTimer = 1f;
				}
			}
		}

		public void OolacileTeleport()
		{
			if ((TeleportTimer >= 200 && npc.life > npc.lifeMax / 4) || (TeleportTimer >= 120 && npc.life <= npc.lifeMax / 4))
			{
				Main.PlaySound(2, (int)npc.position.X, (int)npc.position.Y, 8);
				for (int i = 0; i < 10; i++)
				{
					int dust = Dust.NewDust(new Vector2((float)npc.position.X, (float)npc.position.Y), npc.width, npc.height, 27, npc.velocity.X + Main.rand.Next(-10, 10), npc.velocity.Y + Main.rand.Next(-10, 10), 200, Color.Purple, 1f);
					Main.dust[dust].noGravity = true;
				}
				DarkBeadShotCounter = 0;
				TeleportTimer = 0;

				//region teleportation - can't believe I got this to work.. yayyyyy :D lol

				int target_x_blockpos = (int)Main.player[npc.target].position.X / 16; // corner not center
				int target_y_blockpos = (int)Main.player[npc.target].position.Y / 16; // corner not center
				int x_blockpos = (int)npc.position.X / 16; // corner not center
				int y_blockpos = (int)npc.position.Y / 16; // corner not center
				int tp_radius = 30; // radius around target(upper left corner) in blocks to teleport into
				int tp_counter = 0;
				bool endLoop = false;
				if (Math.Abs(npc.position.X - Main.player[npc.target].position.X) + Math.Abs(npc.position.Y - Main.player[npc.target].position.Y) > 9000000f)
				{ // far away from target; 4000 pixels = 250 blocks
					tp_counter = 100;
					endLoop = false; // always telleport was true for no teleport
				}
				while (!endLoop) // loop always ran full 100 time before I added "flag7 = true" below
				{
					if (tp_counter >= 100) // run 100 times
						break; //return;
					tp_counter++;

					int tp_x_target = Main.rand.Next(target_x_blockpos - tp_radius, target_x_blockpos + tp_radius);  //  pick random tp point (centered on corner)
					int tp_y_target = Main.rand.Next((target_y_blockpos - tp_radius) - 62, (target_y_blockpos + tp_radius) - 26);  //  pick random tp point (centered on corner)
					for (int m = tp_y_target; m < target_y_blockpos + tp_radius; m++) // traverse y downward to edge of radius
					{ // (tp_x_target,m) is block under its feet I think
						if ((m < target_y_blockpos - 21 || m > target_y_blockpos + 21 || tp_x_target < target_x_blockpos - 21 || tp_x_target > target_x_blockpos + 21) && (m < y_blockpos - 8 || m > y_blockpos + 8 || tp_x_target < x_blockpos - 8 || tp_x_target > x_blockpos + 8) && !Main.tile[tp_x_target, m].active())
						{ // over 21 blocks distant from player & over 5 block distant from old position & tile active(to avoid surface? want to tp onto a block?)
							bool safe_to_stand = true;
							bool dark_caster = false; // not a fighter type AI...
							if (dark_caster && Main.tile[tp_x_target, m - 1].wall == 0) // Dark Caster & ?outdoors
								safe_to_stand = false;
							else if (Main.tile[tp_x_target, m - 1].lava()) // feet submerged in lava
								safe_to_stand = false;

							if (safe_to_stand && !Collision.SolidTiles(tp_x_target - 1, tp_x_target + 1, m - 4, m - 1))
							{ //  3x4 tile region is clear; (tp_x_target,m) is below bottom middle tile
							  // safe_to_stand && Main.tileSolid[(int)Main.tile[tp_x_target, m].type] && // removed safe enviornment && solid below feet

								npc.position.X = (float)(tp_x_target * 16 - npc.width / 2); // center x at target
								npc.position.Y = (float)(m * 16 - npc.height); // y so block is under feet			
								Vector2 vector8 = new Vector2(npc.position.X + (npc.width * 0.5f), npc.position.Y - 5 + (npc.height / 2));
								float rotation = (float)Math.Atan2(vector8.Y - (Main.player[npc.target].position.Y + (Main.player[npc.target].height * 0.5f)), vector8.X - (Main.player[npc.target].position.X + (Main.player[npc.target].width * 0.5f)));
								npc.velocity.X = (float)(Math.Cos(rotation) * 1) * -1;
								npc.velocity.Y = (float)(Math.Sin(rotation) * 1) * -1;

								npc.netUpdate = true;

								//npc.ai[3] = -120f; // -120 boredom is signal to display effects & reset boredom next tick in section "teleportation particle effects"
								endLoop = true; // end the loop (after testing every lower point :/)
								TeleportTimer = 0;
							}
						} // END over 17 blocks distant from player...
					} // END traverse y down to edge of radius
				} // END try 100 times
			}
		}

		#endregion
		public override bool CheckActive()
		{
			return false;
		}
		public override void BossLoot(ref string name, ref int potionType)
		{
			potionType = ItemID.SuperHealingPotion;
		}
		public override void FindFrame(int currentFrame)
		{
			if ((npc.velocity.X > -9 && npc.velocity.X < 9) && (npc.velocity.Y > -9 && npc.velocity.Y < 9))
			{
				npc.frameCounter = 0;
				npc.frame.Y = 0;
				if (npc.position.X > Main.player[npc.target].position.X)
				{
					npc.spriteDirection = -1;
				}
				else
				{
					npc.spriteDirection = 1;
				}
			}

			int num = 1;
			if (!Main.dedServ)
			{
				num = Main.npcTexture[npc.type].Height / Main.npcFrameCount[npc.type];
			}
			if ((npc.velocity.X > -1 && npc.velocity.X < 1) && (npc.velocity.Y > -1 && npc.velocity.Y < 1))
			{
				npc.frameCounter = 0;
				npc.frame.Y = 0;
			}
			else
			{
				npc.frameCounter += 1.0;
			}
			if (npc.frameCounter >= 1.0)
			{
				npc.frame.Y = npc.frame.Y + num;
				npc.frameCounter = 0.0;
			}
			if (npc.frame.Y >= num * Main.npcFrameCount[npc.type])
			{
				npc.frame.Y = 0;
			}
		}

		#region Gore
		public override void NPCLoot()
		{
			Main.NewText("A darkness has been lifted from the world...", 150, 150, 150);
			Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Oolacile Sorcerer Gore 1"), 1f);
			Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Oolacile Sorcerer Gore 2"), 1f);
			Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Oolacile Sorcerer Gore 3"), 1f);
			Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Oolacile Sorcerer Gore 2"), 1f);
			Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Oolacile Sorcerer Gore 3"), 1f);

			if (Main.expertMode)
			{
				npc.DropBossBags();
			}
			else
			{
				Item.NewItem(npc.getRect(), ModContent.ItemType<Items.Potions.HealingElixir>(), 10);
				Item.NewItem(npc.getRect(), ModContent.ItemType<Items.DarkSoul>(), 5000);
				Item.NewItem(npc.getRect(), ModContent.ItemType<Items.Accessories.DuskCrownRing>());
				Item.NewItem(npc.getRect(), ModContent.ItemType<Items.Humanity>());
				if (Main.rand.Next(1) == 0) Item.NewItem(npc.getRect(), ModContent.ItemType<Items.PurgingStone>());
				Item.NewItem(npc.getRect(), ModContent.ItemType<Items.RedTitanite>(), 5);
			}
		}
		#endregion
	}
}
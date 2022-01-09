using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.NPCs.Bosses.SuperHardMode
{
	[AutoloadBossHead]
	class Chaos : ModNPC
	{
		public override void SetDefaults()
		{
			Main.npcFrameCount[npc.type] = 8;
			npc.width = 130;
			npc.height = 170;
			npc.aiStyle = 22;
			npc.damage = 150;
			npc.defense = 80;
			npc.HitSound = SoundID.NPCHit1;
			npc.DeathSound = SoundID.NPCDeath5;
			npc.lifeMax = 400000;
			npc.knockBackResist = 0;
			npc.noGravity = true;
			npc.noTileCollide = true;
			npc.value = 600000;
			npc.boss = true;
			npc.lavaImmune = true;

			npc.buffImmune[BuffID.Poisoned] = true;
			npc.buffImmune[BuffID.Confused] = true;
			npc.buffImmune[BuffID.CursedInferno] = true;
			npc.buffImmune[BuffID.OnFire] = true;
			bossBag = ModContent.ItemType<Items.BossBags.ChaosBag>();
			despawnHandler = new NPCDespawnHandler("Chaos tears you asunder...", Color.Yellow, DustID.GoldFlame);

		}

		int fireBreathDamage = 48;
		int iceStormDamage = 53;
		int greatFireballDamage = 49;
		int blazeBallDamage = 48;
		int purpleCrushDamage = 35;
		int meteorDamage = 55;
		int tornadoDamage = 45;
		int obscureSeekerDamage = 50;
		int crystalFireDamage = 50;
		int fireTrailsDamage = 35;
		public override void ScaleExpertStats(int numPlayers, float bossLifeScale)
		{
			npc.damage = (int)(npc.damage / 2);
		}

		int chaosHealed = 0;
		bool chargeDamageFlag = false;
		int holdTimer = 0;

		int chargeTimer = 0;

		#region AI
		NPCDespawnHandler despawnHandler;
		public override void AI()
		{
			despawnHandler.TargetAndDespawn(npc.whoAmI);
			Lighting.AddLight((int)npc.position.X / 16, (int)npc.position.Y / 16, 0.4f, 0f, 0f);

			if (holdTimer > 0)
			{
				holdTimer--;
			}
			if (Vector2.Distance(npc.Center, Main.player[npc.target].Center) > 1000)
			{
				npc.defense = 9999;
				if (holdTimer <= 0)
				{
					Main.NewText("Chaos is protected by unseen powers -- you're too far away!", 175, 75, 255);
					holdTimer = 200;
				}
				else
				{
					npc.defense = 80;
				}
			}

			ClientSideAttacks();
			if (Main.netMode != NetmodeID.MultiplayerClient)
			{
				NonClientAttacks();
			}

			if (npc.justHit)
			{
				npc.ai[2] = 0f;
			}
			if (npc.ai[2] >= 0f)
			{
				int num258 = 16;
				bool flag26 = false;
				bool flag27 = false;
				if (npc.position.X > npc.ai[0] - (float)num258 && npc.position.X < npc.ai[0] + (float)num258)
				{
					flag26 = true;
				}
				else
				{
					if ((npc.velocity.X < 0f && npc.direction > 0) || (npc.velocity.X > 0f && npc.direction < 0))
					{
						flag26 = true;
					}
				}
				num258 += 24;
				if (npc.position.Y > npc.ai[1] - (float)num258 && npc.position.Y < npc.ai[1] + (float)num258)
				{
					flag27 = true;
				}
				if (flag26 && flag27)
				{
					npc.ai[2] += 1f;
					if (npc.ai[2] >= 60f)
					{
						npc.ai[2] = -200f;
						npc.direction *= -1;
						npc.velocity.X = npc.velocity.X * -1f;
						npc.collideX = false;
					}
				}
				else
				{
					npc.ai[0] = npc.position.X;
					npc.ai[1] = npc.position.Y;
					npc.ai[2] = 0f;
				}
			}
			else
			{
				npc.ai[2] += 1f;
				if (Main.player[npc.target].position.X + (float)(Main.player[npc.target].width / 2) > npc.position.X + (float)(npc.width / 2))
				{
					npc.direction = -1;
				}
				else
				{
					npc.direction = 1;
				}
			}
			int num259 = (int)((npc.position.X + (float)(npc.width / 2)) / 16f) + npc.direction * 2;
			int num260 = (int)((npc.position.Y + (float)npc.height) / 16f);
			if (npc.position.Y > Main.player[npc.target].position.Y)
			{
				npc.velocity.Y -= .22f;
				if (npc.velocity.Y < -2)
				{
					npc.velocity.Y = -2;
				}
			}
			if (npc.position.Y < Main.player[npc.target].position.Y)
			{
				npc.velocity.Y += .22f;
				if (npc.velocity.Y > 2)
				{
					npc.velocity.Y = 2;
				}
			}
			if (npc.collideX)
			{
				npc.velocity.X = npc.oldVelocity.X * -0.4f;
				if (npc.direction == -1 && npc.velocity.X > 0f && npc.velocity.X < 1f)
				{
					npc.velocity.X = 1f;
				}
				if (npc.direction == 1 && npc.velocity.X < 0f && npc.velocity.X > -1f)
				{
					npc.velocity.X = -1f;
				}
			}
			if (npc.collideY)
			{
				npc.velocity.Y = npc.oldVelocity.Y * -0.25f;
				if (npc.velocity.Y > 0f && npc.velocity.Y < 1f)
				{
					npc.velocity.Y = 1f;
				}
				if (npc.velocity.Y < 0f && npc.velocity.Y > -1f)
				{
					npc.velocity.Y = -1f;
				}
			}
			float num270 = 2.5f;
			if (npc.direction == -1 && npc.velocity.X > -num270)
			{
				npc.velocity.X = npc.velocity.X - 0.1f;
				if (npc.velocity.X > num270)
				{
					npc.velocity.X = npc.velocity.X - 0.1f;
				}
				else
				{
					if (npc.velocity.X > 0f)
					{
						npc.velocity.X = npc.velocity.X + 0.05f;
					}
				}
				if (npc.velocity.X < -num270)
				{
					npc.velocity.X = -num270;
				}
			}
			else
			{
				if (npc.direction == 1 && npc.velocity.X < num270)
				{
					npc.velocity.X = npc.velocity.X + 0.1f;
					if (npc.velocity.X < -num270)
					{
						npc.velocity.X = npc.velocity.X + 0.1f;
					}
					else
					{
						if (npc.velocity.X < 0f)
						{
							npc.velocity.X = npc.velocity.X - 0.05f;
						}
					}
					if (npc.velocity.X > num270)
					{
						npc.velocity.X = num270;
					}
				}
			}
			if (npc.directionY == -1 && (double)npc.velocity.Y > -2.5)
			{
				npc.velocity.Y = npc.velocity.Y - 0.04f;
				if ((double)npc.velocity.Y > 2.5)
				{
					npc.velocity.Y = npc.velocity.Y - 0.05f;
				}
				else
				{
					if (npc.velocity.Y > 0f)
					{
						npc.velocity.Y = npc.velocity.Y + 0.03f;
					}
				}
				if ((double)npc.velocity.Y < -2.5)
				{
					npc.velocity.Y = -2.5f;
				}
			}
			else
			{
				if (npc.directionY == 1 && (double)npc.velocity.Y < 2.5)
				{
					npc.velocity.Y = npc.velocity.Y + 0.04f;
					if ((double)npc.velocity.Y < -2.5)
					{
						npc.velocity.Y = npc.velocity.Y + 0.05f;
					}
					else
					{
						if (npc.velocity.Y < 0f)
						{
							npc.velocity.Y = npc.velocity.Y - 0.03f;
						}
					}
					if ((double)npc.velocity.Y > 2.5)
					{
						npc.velocity.Y = 2.5f;
					}
				}
			}
		}
		#endregion


		//Projectile spawning code must not run for every single multiplayer client
		void NonClientAttacks()
        {
			npc.ai[1] += 0.35f;
			if (npc.ai[1] >= 10f)
			{
				if (Main.rand.Next(90) == 1)
				{					
					Vector2 projTarget = UsefulFunctions.GenerateTargetingVector(npc.Center, Main.player[npc.target].Center, 9);
					projTarget += Main.rand.NextVector2Circular(3, 3);
					Projectile.NewProjectile(npc.Center.X, npc.Center.Y, projTarget.X, projTarget.Y, ModContent.ProjectileType<Projectiles.Enemy.FireBreath>(), fireBreathDamage, 0f, Main.myPlayer);
					Main.PlaySound(2, (int)npc.position.X, (int)npc.position.Y, 0x11);
					npc.ai[1] = 1f;
				}
				if (Main.rand.Next(500) == 1)
				{					
					Vector2 projTarget = UsefulFunctions.GenerateTargetingVector(npc.Center, Main.player[npc.target].Center, 8);
					projTarget += Main.rand.NextVector2Circular(3, 3);
					Projectile.NewProjectile(npc.Center.X, npc.Center.Y, projTarget.X, projTarget.Y, ModContent.ProjectileType<Projectiles.Enemy.EnemySpellIcestormBall>(), iceStormDamage, 0f, Main.myPlayer);
					Main.PlaySound(2, (int)npc.position.X, (int)npc.position.Y, 0x11);
					npc.ai[1] = 1f;
				}
				if (Main.rand.Next(500) == 1)
				{
					Vector2 projTarget = UsefulFunctions.GenerateTargetingVector(npc.Center, Main.player[npc.target].Center, 8);
					projTarget += Main.rand.NextVector2Circular(3, 3);
					Projectile.NewProjectile(npc.Center.X, npc.Center.Y, projTarget.X, projTarget.Y, ModContent.ProjectileType<Projectiles.Enemy.EnemySpellGreatFireballBall>(), greatFireballDamage, 0f, Main.myPlayer);
					Main.PlaySound(2, (int)npc.position.X, (int)npc.position.Y, 0x11);
					npc.ai[1] = 1f;
				}
				if (Main.rand.Next(1000) == 1)
				{
					Vector2 projTarget = UsefulFunctions.GenerateTargetingVector(npc.Center, Main.player[npc.target].Center, 8);
					projTarget += Main.rand.NextVector2Circular(3, 3);
					Projectile.NewProjectile(npc.Center.X, npc.Center.Y, projTarget.X, projTarget.Y, ModContent.ProjectileType<Projectiles.Enemy.EnemySpellBlazeBall>(), blazeBallDamage, 0f, Main.myPlayer);
					Main.PlaySound(2, (int)npc.position.X, (int)npc.position.Y, 0x11);
					npc.ai[1] = 1f;
				}
				if (Main.rand.Next(300) == 1)
				{					
					Vector2 projTarget = UsefulFunctions.GenerateTargetingVector(npc.Center, Main.player[npc.target].Center, 11);
					projTarget += Main.rand.NextVector2Circular(3, 3);
					Projectile.NewProjectile(npc.Center.X, npc.Center.Y, projTarget.X, projTarget.Y, ModContent.ProjectileType<Projectiles.Enemy.CrazedPurpleCrush>(), purpleCrushDamage, 0f, Main.myPlayer);
					Main.PlaySound(2, (int)npc.position.X, (int)npc.position.Y, 0x11);
					npc.ai[1] = 1f;
				}

				if (Main.rand.Next(205) == 1)
				{
					Projectile.NewProjectile(Main.player[npc.target].position.X - 100 + Main.rand.Next(300), Main.player[npc.target].position.Y - 530.0f,(float)(-40 + Main.rand.Next(80)) / 10, 14.9f, ModContent.ProjectileType<Projectiles.Enemy.EnemyMeteor>(), meteorDamage, 2.0f, Main.myPlayer);
				}

				if (Main.rand.Next(1200) == 1)
				{					
					Vector2 projTarget = UsefulFunctions.GenerateTargetingVector(npc.Center, Main.player[npc.target].Center, 4);
					projTarget += Main.rand.NextVector2Circular(3, 3);
					Projectile.NewProjectile(npc.Center.X, npc.Center.Y, projTarget.X, projTarget.Y, ModContent.ProjectileType<Projectiles.Enemy.EnemySpellTornado>(), tornadoDamage, 0f, Main.myPlayer, npc.target);
					Main.PlaySound(2, (int)npc.position.X, (int)npc.position.Y, 0x11);
					npc.ai[1] = 1f;
				}
				if (Main.rand.Next(220) == 1)
				{					
					Vector2 projTarget = UsefulFunctions.GenerateTargetingVector(npc.Center, Main.player[npc.target].Center, 8);
					projTarget += Main.rand.NextVector2Circular(3, 3);
					Projectile.NewProjectile(npc.Center.X, npc.Center.Y, projTarget.X, projTarget.Y, ModContent.ProjectileType<Projectiles.Enemy.ObscureSeeker>(), obscureSeekerDamage, 0f, Main.myPlayer);
					Main.PlaySound(2, (int)npc.position.X, (int)npc.position.Y, 0x11);
					npc.ai[1] = 1f;
				}
				if (Main.rand.Next(50) == 1)
				{					
					Vector2 projTarget = UsefulFunctions.GenerateTargetingVector(npc.Center, Main.player[npc.target].Center, 12);
					projTarget += Main.rand.NextVector2Circular(3, 3);
					Projectile.NewProjectile(npc.Center.X, npc.Center.Y, projTarget.X, projTarget.Y, ModContent.ProjectileType<Projectiles.Enemy.PoisonCrystalFire>(), crystalFireDamage, 0f, Main.myPlayer);
					Main.PlaySound(2, (int)npc.position.X, (int)npc.position.Y, 0x11);
					npc.ai[1] = 1f;
				}
				if (Main.rand.Next(120) == 1)
				{
					Vector2 projTarget = UsefulFunctions.GenerateTargetingVector(npc.Center, Main.player[npc.target].Center, 5);
					projTarget += Main.rand.NextVector2Circular(3, 3);
					Projectile.NewProjectile(npc.Center.X, npc.Center.Y, projTarget.X, projTarget.Y, ModContent.ProjectileType<Projectiles.Enemy.FireTrails>(), fireTrailsDamage, 0f, Main.myPlayer);
					Main.PlaySound(2, (int)npc.position.X, (int)npc.position.Y, 0x11);
					npc.ai[1] = 1f;
				}
			}			
		}

		//Healing and dashing code must be deterministic and must run on every client
		void ClientSideAttacks()
        {
			chargeTimer++;
			if (chargeTimer >= 480)
			{
				chargeTimer = 0;
				chargeDamageFlag = true;
				npc.velocity = UsefulFunctions.GenerateTargetingVector(npc.Center, Main.player[npc.target].Center, 14) + Main.player[npc.target].velocity;
			}
			if (chargeDamageFlag == true)
			{
				npc.damage = 120;
			}
			if (Math.Abs(npc.Center.X - Main.player[npc.target].Center.X) < 20)
			{
				chargeDamageFlag = false;
				npc.damage = 80;
			}
			
			if (npc.life <= npc.lifeMax / 3)
			{				
				if (chaosHealed >= 1 && chaosHealed <= 3)
				{
					if (Main.rand.Next(500) == 1)
					{
						if (chaosHealed == 0)
						{
							UsefulFunctions.ServerText("Chaos has begun to heal itself...", Color.Yellow);
						}
						else
                        {
							UsefulFunctions.ServerText("Chaos continues to heal itself...", Color.Yellow);
						}
						npc.life += npc.lifeMax / 6;
						if (npc.life > npc.lifeMax) npc.life = npc.lifeMax;
						Projectile.NewProjectile(npc.Center.X, npc.Center.Y, 0, 0, ModContent.ProjectileType<Projectiles.Enemy.EnemySpellEffectHealing>(), 0, 0f, Main.myPlayer);
						Main.PlaySound(2, (int)npc.position.X, (int)npc.position.Y, 4);
						npc.netUpdate = true;
						chaosHealed += 1;
					}
				}
			}
		}
		#region Frames
		public override void FindFrame(int currentFrame)
		{
			int num = 1;
			if (!Main.dedServ)
			{
				num = Main.npcTexture[npc.type].Height / Main.npcFrameCount[npc.type];
			}
			if (npc.velocity.X < 0)
			{
				npc.spriteDirection = -1;
			}
			else
			{
				npc.spriteDirection = 1;
			}
			npc.rotation = npc.velocity.X * 0.08f;
			npc.frameCounter += 1.0;
			if (npc.frameCounter >= 4.0)
			{
				npc.frame.Y = npc.frame.Y + num;
				npc.frameCounter = 0.0;
			}
			if (npc.frame.Y >= num * Main.npcFrameCount[npc.type])
			{
				npc.frame.Y = 0;
			}
		}
		#endregion

		#region Gore
		public override void NPCLoot()
		{
			Projectile.NewProjectile((int)npc.position.X, (int)npc.position.Y, 0, 0, ModContent.ProjectileType<Projectiles.Enemy.ChaosDeathAnimation>(), 0, 0f, Main.myPlayer);
			if (Main.expertMode)
			{
				npc.DropBossBags();
			}
			else
			{
				Item.NewItem(npc.getRect(), ModContent.ItemType<Items.Armors.PowerArmorNUHelmet>());
				Item.NewItem(npc.getRect(), ModContent.ItemType<Items.Armors.PowerArmorNUTorso>());
				Item.NewItem(npc.getRect(), ModContent.ItemType<Items.Armors.PowerArmorNUGreaves>());
				if (Main.rand.Next(99) < 20) Item.NewItem(npc.getRect(), ModContent.ItemType<Items.Weapons.Magic.FlareTome>());
				if (Main.rand.Next(99) < 40) Item.NewItem(npc.getRect(), ModContent.ItemType<Items.Weapons.Ranged.ElfinBow>());
				Item.NewItem(npc.getRect(), ModContent.ItemType<Items.Potions.HolyWarElixir>());
				if (Main.rand.Next(99) < 50) Item.NewItem(npc.getRect(), ModContent.ItemType<Items.Weapons.Melee.HiRyuuSpear>());
				Item.NewItem(npc.getRect(), ModContent.ItemType<Items.DarkSoul>(), 3000);
				Item.NewItem(npc.getRect(), ModContent.ItemType<Items.GuardianSoul>());
				Item.NewItem(npc.getRect(), ModContent.ItemType<Items.SoulOfChaos>(), 3);
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

		#region Magic Defense
		public int MagicDefenseValue()
		{
			return 65;
		}
		#endregion
	}
}
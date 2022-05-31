using Microsoft.Xna.Framework;
using System;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items;

namespace tsorcRevamp.NPCs.Bosses.Serris
{
	[AutoloadBossHead]
	class SerrisX : ModNPC
	{
		public override void SetDefaults()
		{
			Main.npcFrameCount[NPC.type] = 16;
			drawOffsetY = 10;
			NPC.npcSlots = 5;
			NPC.width = 70;
			NPC.height = 70;
			NPC.aiStyle = 2;
			NPC.timeLeft = 22500;
			NPC.damage = 150;
			NPC.defense = 9999;
			NPC.HitSound = SoundID.NPCHit1;
			NPC.DeathSound = SoundID.NPCDeath1;
			NPC.lifeMax = 10000;
			NPC.scale = 1;
			NPC.knockBackResist = 0;
			NPC.noGravity = true;
			NPC.noTileCollide = true;
			NPC.boss = true;
			NPC.value = 500000;

			NPC.buffImmune[BuffID.Confused] = true;
			bossBag = ModContent.ItemType<Items.BossBags.SerrisBag>();

			//If one already exists, don't add text to the others despawnhandler (so it doesn't show duplicate messages if you die)
			if (NPC.AnyNPCs(ModContent.NPCType<NPCs.Bosses.Serris.SerrisHead>()) || NPC.CountNPCS(ModContent.NPCType<NPCs.Bosses.Serris.SerrisX>()) > 1)
			{
				despawnHandler = new NPCDespawnHandler(DustID.Firework_Blue);
			}
			else
			{
				despawnHandler = new NPCDespawnHandler("Serris returns to the depths of its temple...", Color.Cyan, DustID.Firework_Blue);
			}
		}

		public override void SetStaticDefaults()
		{
			DisplayName.SetDefault("Serris-X");
		}

		int plasmaOrbDamage = 100;
		public override void ScaleExpertStats(int numPlayers, float bossLifeScale)
		{
			NPC.damage = (int)(NPC.damage * 1.3 / tsorcRevampGlobalNPC.expertScale);
			plasmaOrbDamage = plasmaOrbDamage / 2;
		}

		bool immuneFlash = true;
		bool TimeLock = false;
		//Cooldown between projectiles in frames
		int projCooldown = 0;
		bool projRotate = false;
		bool extraProjs = false;
		NPCDespawnHandler despawnHandler;
		int attack = 0;
		int secondaryCooldown = 900;
		float attackCounter = 0;
		int slowdownCounter = 100;
		public override void AI()
		{
			despawnHandler.TargetAndDespawn(NPC.whoAmI);

			//Health stuff. This would go into OnHitByWhatever or ModifyHitByWhatever, but surprise: Those are fucky in multiplayer! Isn't learning fun.
			if((NPC.life % 1000) != 0 && NPC.life > 1)
            {
				NPC.life -= NPC.life % 1000;
				if(NPC.life <= 0)
                {
					NPC.life = 1;
                }

				TimeLock = false;
				NPC.ai[0] = 2;
				Main.PlaySound(15, (int)NPC.position.X, (int)NPC.position.Y, 0);
				NPC.netUpdate = true;
			}

			if(attack == 0) {
				if (secondaryCooldown > 0) {
					secondaryCooldown--;
				}
				else
                {
					attack = 1;
					secondaryCooldown = 300;
				}
				if (projCooldown > 0)
				{
					projCooldown--;
				}
				else
				{
					projCooldown = 60;
					float speed = 5f;
					Vector2 vector8 = new Vector2(NPC.position.X + (NPC.width * 0.5f), NPC.position.Y + (NPC.height / 2));
					float distanceFactor = Vector2.Distance(vector8, Main.player[NPC.target].position) / speed;
					float speedX = ((Main.player[NPC.target].position.X + (Main.player[NPC.target].width * 0.5f)) - vector8.X) / distanceFactor;
					float speedY = ((Main.player[NPC.target].position.Y + (Main.player[NPC.target].height * 0.5f)) - vector8.Y) / distanceFactor;

					if (Main.netMode != NetmodeID.MultiplayerClient)
					{
						Projectile.NewProjectile(vector8.X, vector8.Y, speedX, speedY, ModContent.ProjectileType<Projectiles.Enemy.EnemyPlasmaOrb>(), plasmaOrbDamage, 0f, Main.myPlayer);
					}
					if (extraProjs)
					{
						if (projRotate )
						{
							if (Main.netMode != NetmodeID.MultiplayerClient)
							{
								Projectile.NewProjectile(vector8.X, vector8.Y, speed, speed, ModContent.ProjectileType<Projectiles.Enemy.EnemyPlasmaOrb>(), plasmaOrbDamage, 0f, Main.myPlayer);
								Projectile.NewProjectile(vector8.X, vector8.Y, -speed, speed, ModContent.ProjectileType<Projectiles.Enemy.EnemyPlasmaOrb>(), plasmaOrbDamage, 0f, Main.myPlayer);
								Projectile.NewProjectile(vector8.X, vector8.Y, speed, -speed, ModContent.ProjectileType<Projectiles.Enemy.EnemyPlasmaOrb>(), plasmaOrbDamage, 0f, Main.myPlayer);
								Projectile.NewProjectile(vector8.X, vector8.Y, -speed, -speed, ModContent.ProjectileType<Projectiles.Enemy.EnemyPlasmaOrb>(), plasmaOrbDamage, 0f, Main.myPlayer);
							}
							projRotate = false;
						}
						else
						{
							if (Main.netMode != NetmodeID.MultiplayerClient)
							{
								Projectile.NewProjectile(vector8.X, vector8.Y, speed, 0, ModContent.ProjectileType<Projectiles.Enemy.EnemyPlasmaOrb>(), plasmaOrbDamage, 0f, Main.myPlayer);
								Projectile.NewProjectile(vector8.X, vector8.Y, -speed, 0, ModContent.ProjectileType<Projectiles.Enemy.EnemyPlasmaOrb>(), plasmaOrbDamage, 0f, Main.myPlayer);
								Projectile.NewProjectile(vector8.X, vector8.Y, 0, speed, ModContent.ProjectileType<Projectiles.Enemy.EnemyPlasmaOrb>(), plasmaOrbDamage, 0f, Main.myPlayer);
								Projectile.NewProjectile(vector8.X, vector8.Y, 0, -speed, ModContent.ProjectileType<Projectiles.Enemy.EnemyPlasmaOrb>(), plasmaOrbDamage, 0f, Main.myPlayer);
							}
							projRotate = true;
						}
						extraProjs = false;
					}
					else
					{
						extraProjs = true;
					}
				}
			} 
			if(attack == 1)
            {				
				if (projCooldown > 0 || secondaryCooldown > 0)
				{					
					for (int j = 0; j < 5; j++)
					{
						Vector2 dir = Main.rand.NextVector2CircularEdge(250, 250);
						Vector2 dustPos = NPC.Center + dir;
						Vector2 dustVel = UsefulFunctions.GenerateTargetingVector(dustPos, NPC.Center, 12);
						Dust.NewDustPerfect(dustPos, DustID.Firework_Blue, dustVel, 200).noGravity = true;
					}
					projCooldown--;
					secondaryCooldown--;
				}
				else
                {
					NPC.velocity.X *= slowdownCounter/100;
					NPC.velocity.Y *= slowdownCounter/100;

					if(slowdownCounter > 0)
                    {
						slowdownCounter--;
						//While it slows down, add some fun in
						if (Main.rand.Next(2) == 0)
						{
							if (Main.netMode != NetmodeID.MultiplayerClient)
							{
								Projectile.NewProjectile(NPC.Center.X, NPC.Center.Y, Main.rand.Next(-10, 10), Main.rand.Next(-10, 10), ModContent.ProjectileType<Projectiles.Enemy.EnemyPlasmaOrb>(), plasmaOrbDamage, 0f, Main.myPlayer);
							}
						}
					}

					if(slowdownCounter == 0 && ((Math.Abs(NPC.velocity.X) < .01) && Math.Abs(NPC.velocity.Y) < .01))
                    {
						float speed = 10f;
						int spread = 30;
						Vector2 vector8 = new Vector2(NPC.position.X + (NPC.width * 0.5f), NPC.position.Y + (NPC.height / 2));
						float distanceFactor = Vector2.Distance(vector8, Main.player[NPC.target].position) / speed;
						float speedX = ((Main.player[NPC.target].position.X + (Main.player[NPC.target].width * 0.5f)) - vector8.X) / distanceFactor;
						float speedY = ((Main.player[NPC.target].position.Y + (Main.player[NPC.target].height * 0.5f)) - vector8.Y) / distanceFactor;

						if (Main.netMode != NetmodeID.MultiplayerClient)
						{
							Projectile.NewProjectile(vector8.X, vector8.Y, speedX, speedY, ModContent.ProjectileType<Projectiles.Enemy.EnemyPlasmaOrb>(), plasmaOrbDamage, 0f, Main.myPlayer);
						}
						if (projRotate)
						{
							if (Main.netMode != NetmodeID.MultiplayerClient)
							{
								Projectile.NewProjectile(vector8.X - spread, vector8.Y - spread, speedX, speedY, ModContent.ProjectileType<Projectiles.Enemy.EnemyPlasmaOrb>(), plasmaOrbDamage, 0f, Main.myPlayer);
								Projectile.NewProjectile(vector8.X + spread, vector8.Y - spread, speedX, speedY, ModContent.ProjectileType<Projectiles.Enemy.EnemyPlasmaOrb>(), plasmaOrbDamage, 0f, Main.myPlayer);
								Projectile.NewProjectile(vector8.X - spread, vector8.Y + spread, speedX, speedY, ModContent.ProjectileType<Projectiles.Enemy.EnemyPlasmaOrb>(), plasmaOrbDamage, 0f, Main.myPlayer);
								Projectile.NewProjectile(vector8.X + spread, vector8.Y + spread, speedX, speedY, ModContent.ProjectileType<Projectiles.Enemy.EnemyPlasmaOrb>(), plasmaOrbDamage, 0f, Main.myPlayer);
							}
							projRotate = false;
						}
						else
						{
							if (Main.netMode != NetmodeID.MultiplayerClient)
							{
								Projectile.NewProjectile(vector8.X - spread, vector8.Y, speedX, speedY, ModContent.ProjectileType<Projectiles.Enemy.EnemyPlasmaOrb>(), plasmaOrbDamage, 0f, Main.myPlayer);
								Projectile.NewProjectile(vector8.X + spread, vector8.Y, speedX, speedY, ModContent.ProjectileType<Projectiles.Enemy.EnemyPlasmaOrb>(), plasmaOrbDamage, 0f, Main.myPlayer);
								Projectile.NewProjectile(vector8.X, vector8.Y - spread, speedX, speedY, ModContent.ProjectileType<Projectiles.Enemy.EnemyPlasmaOrb>(), plasmaOrbDamage, 0f, Main.myPlayer);
								Projectile.NewProjectile(vector8.X, vector8.Y + spread, speedX, speedY, ModContent.ProjectileType<Projectiles.Enemy.EnemyPlasmaOrb>(), plasmaOrbDamage, 0f, Main.myPlayer);
							}
						projRotate = true;
						}
						

						attackCounter++;
						//Every 3 shots, pause for 4 seconds
						if((attackCounter % 5) == 0)
                        {
							secondaryCooldown = 240;
							slowdownCounter = 100;
						} else
                        {
							secondaryCooldown = 20;
                        }
						//After doing this 3 times, change phases
						if (attackCounter >= 15)
						{
							attackCounter = 0;
							attack = 0;
							secondaryCooldown = 900;
						}
					}
				}
			}
			NPC.ai[0]++;
			NPC.position += NPC.velocity * 1.5f;
			if (NPC.ai[0] <= 1 || NPC.ai[0] >= 300)
			{
				immuneFlash = false;
				NPC.dontTakeDamage = false;
				TimeLock = true;
			}
			else if (NPC.ai[0] >= 2)
			{
				immuneFlash = true;
				NPC.dontTakeDamage = true;
			}
			if (TimeLock)
			{
				NPC.ai[0] = 0;
			}
		}

		
       
		public override bool CheckActive()
		{
			return false;
		}

        public override bool PreNPCLoot()
        {
			Vector2 vector8 = new Vector2(NPC.position.X + (NPC.width * 0.5f), NPC.position.Y + (NPC.height / 2));
			Gore.NewGore(NPC.GetSource_Death(), vector8, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.GetGoreSlot("Gores/Serris-X Gore 1"), 1f);
			Gore.NewGore(NPC.GetSource_Death(), vector8, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.GetGoreSlot("Gores/Serris-X Gore 2"), 1f);
			Gore.NewGore(NPC.GetSource_Death(), vector8, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.GetGoreSlot("Gores/Serris-X Gore 3"), 1f);
			for (int num36 = 0; num36 < 70; num36++)
			{
				int dust = Dust.NewDust(NPC.position, (int)(NPC.width), (int)(NPC.height), DustID.Firework_Blue, Main.rand.Next(-15, 15), Main.rand.Next(-15, 15), 100, new Color(), 9f);
				Main.dust[dust].noGravity = true;
			}

			if (Main.player[NPC.target].active)
			{
				NPC.Center = Main.player[NPC.target].Center;
			}
			return true;
        }


        public override void OnKill()
		{
			UsefulFunctions.BroadcastText("Serris falls...", Color.Cyan);

			if (!(NPC.AnyNPCs(ModContent.NPCType<NPCs.Bosses.Serris.SerrisHead>()) || (NPC.CountNPCS(ModContent.NPCType<NPCs.Bosses.Serris.SerrisX>()) > 1))) {

				if (Main.expertMode)
				{
					NPC.DropBossBags();
				}
				else
				{
					Item.NewItem(NPC.getRect(), ModContent.ItemType<Items.Potions.DemonDrugPotion>(), 3 + Main.rand.Next(4));
					Item.NewItem(NPC.getRect(), ModContent.ItemType<Items.Potions.ArmorDrugPotion>(), 3 + Main.rand.Next(4));
					Item.NewItem(NPC.getRect(), ModContent.ItemType<Items.Weapons.Magic.BarrierTome>(), 1);
					if (!tsorcRevampWorld.Slain.ContainsKey(NPC.type))
					{
						Item.NewItem(NPC.getRect(), ModContent.ItemType<Items.DarkSoul>(), 50000);
					}
				}
			}
		}
		public override void BossLoot(ref string name, ref int potionType)
		{
			potionType = ItemID.GreaterHealingPotion;
		}
		public override void FindFrame(int currentFrame)
		{
			int num = 1;
			if (!Main.dedServ)
			{
				num = Main.npcTexture[NPC.type].Height / Main.npcFrameCount[NPC.type];
			}
			NPC.frameCounter += 1.0;
			if (immuneFlash)
			{
				if (NPC.frameCounter >= 0 && NPC.frameCounter < 5)
				{
					NPC.frame.Y = num * 8;
				}
				if (NPC.frameCounter >= 5 && NPC.frameCounter < 10)
				{
					NPC.frame.Y = num * 9;
				}
				if (NPC.frameCounter >= 10 && NPC.frameCounter < 15)
				{
					NPC.frame.Y = num * 10;
				}
				if (NPC.frameCounter >= 15 && NPC.frameCounter < 20)
				{
					NPC.frame.Y = num * 11;
				}
				if (NPC.frameCounter >= 20 && NPC.frameCounter < 25)
				{
					NPC.frame.Y = num * 12;
				}
				if (NPC.frameCounter >= 25 && NPC.frameCounter < 30)
				{
					NPC.frame.Y = num * 13;
				}
				if (NPC.frameCounter >= 30 && NPC.frameCounter < 35)
				{
					NPC.frame.Y = num * 14;
				}
				if (NPC.frameCounter >= 35 && NPC.frameCounter < 40)
				{
					NPC.frame.Y = num * 15;
				}
				if (NPC.frameCounter >= 40)
				{
					NPC.frameCounter = 0;
				}
			}
			else
			{
				if (NPC.frameCounter >= 0 && NPC.frameCounter < 5)
				{
					NPC.frame.Y = 0;
				}
				if (NPC.frameCounter >= 5 && NPC.frameCounter < 10)
				{
					NPC.frame.Y = num;
				}
				if (NPC.frameCounter >= 10 && NPC.frameCounter < 15)
				{
					NPC.frame.Y = num * 2;
				}
				if (NPC.frameCounter >= 15 && NPC.frameCounter < 20)
				{
					NPC.frame.Y = num * 3;
				}
				if (NPC.frameCounter >= 20 && NPC.frameCounter < 25)
				{
					NPC.frame.Y = num * 4;
				}
				if (NPC.frameCounter >= 25 && NPC.frameCounter < 30)
				{
					NPC.frame.Y = num * 5;
				}
				if (NPC.frameCounter >= 30 && NPC.frameCounter < 35)
				{
					NPC.frame.Y = num * 6;
				}
				if (NPC.frameCounter >= 35 && NPC.frameCounter < 40)
				{
					NPC.frame.Y = num * 7;
				}
				if (NPC.frameCounter >= 45)
				{
					NPC.frameCounter = 0;
				}
			}
		}
	}
}
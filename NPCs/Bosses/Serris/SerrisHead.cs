using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items;

namespace tsorcRevamp.NPCs.Bosses.Serris
{
	[AutoloadBossHead]
	class SerrisHead : ModNPC
	{
		public override void SetDefaults()
		{
			Main.npcFrameCount[npc.type] = 3;
			npc.netAlways = true;
			npc.npcSlots = 5;
			npc.width = 38;
			npc.height = 70;
			npc.aiStyle = 6;
			npc.timeLeft = 22750;
			npc.damage = 80;
			npc.defense = 999999;
			npc.HitSound = SoundID.NPCHit1;
			npc.DeathSound = SoundID.NPCDeath5;
			npc.lavaImmune = true;
			npc.knockBackResist = 0;
			npc.lifeMax = 6000;
			npc.noGravity = true;
			npc.noTileCollide = true;
			npc.behindTiles = true;
			npc.boss = true;
			music = 12;
			npc.value = 300000;
			npc.buffImmune[BuffID.Poisoned] = true;
			npc.buffImmune[BuffID.OnFire] = true;
			npc.buffImmune[BuffID.Confused] = true;

			//If one already exists, don't add text to the others despawnhandler (so it doesn't show duplicate messages if you die)
			if (NPC.CountNPCS(ModContent.NPCType<NPCs.Bosses.Serris.SerrisHead>()) > 1)
			{
				despawnHandler = new NPCDespawnHandler(DustID.Firework_Blue);
			}
			else
			{
				despawnHandler = new NPCDespawnHandler("Serris retreats to the depths of its temple...", Color.Cyan, DustID.Firework_Blue);
			}
		}
		public override void SetStaticDefaults()
		{
			DisplayName.SetDefault("Serris");
		}
		int distortionDamage = 40;
		public override void ScaleExpertStats(int numPlayers, float bossLifeScale)
		{
			npc.damage = (int)(npc.damage * 1.3 / 2);
			npc.defense = npc.defense += 12;
			npc.lifeMax = npc.lifeMax / 2;
			distortionDamage = distortionDamage / 2;
		}


		bool tailSpawned = false;
		bool speedBoost = false;
		bool timeLock = false;
		int SoundDelay = 0;
		int srs = 0;
		int Previous = 0;
		public static int[] bodyTypes = new int[] { ModContent.NPCType<NPCs.Bosses.Serris.SerrisBody>(), ModContent.NPCType<NPCs.Bosses.Serris.SerrisBody>(), ModContent.NPCType<NPCs.Bosses.Serris.SerrisBody>(), ModContent.NPCType<NPCs.Bosses.Serris.SerrisBody>(), ModContent.NPCType<NPCs.Bosses.Serris.SerrisBody>(), ModContent.NPCType<NPCs.Bosses.Serris.SerrisBody>(), ModContent.NPCType<NPCs.Bosses.Serris.SerrisBody>(), ModContent.NPCType<NPCs.Bosses.Serris.SerrisBody>(), ModContent.NPCType<NPCs.Bosses.Serris.SerrisBody>(), ModContent.NPCType<NPCs.Bosses.Serris.SerrisBody>(), ModContent.NPCType<NPCs.Bosses.Serris.SerrisBody>(), ModContent.NPCType<NPCs.Bosses.Serris.SerrisBody>(), ModContent.NPCType<NPCs.Bosses.Serris.SerrisBody>(), ModContent.NPCType<NPCs.Bosses.Serris.SerrisBody>()};
		int projCooldown = 0;
		int nextSegment;
		NPCDespawnHandler despawnHandler;
		public override void AI()
		{
			despawnHandler.TargetAndDespawn(npc.whoAmI);

            if (!Main.npc[nextSegment].active)
            {
				tailSpawned = false;
            }

			if (Math.Abs(npc.position.Y - Main.player[npc.target].position.Y) > 1800 || Math.Abs(npc.position.Y - Main.player[npc.target].position.Y) > 3200)
            {

				float angle = Main.rand.Next(0, 360); 
				npc.position.X = Main.player[npc.target].position. X + (100 * (float)Math.Cos(angle) * 16);
				npc.position.Y = Main.player[npc.target].position.Y + (100 * (float)Math.Sin(angle) * 16);

			}

			//tsorcRevampGlobalNPC.AIWorm(npc, ModContent.NPCType<SerrisHead>(), bodyTypes, ModContent.NPCType<SerrisTail>(), 16, -1f, 12f, 0.6f, true, false, true); //3f was 6f

			if (npc.velocity.X < 0f)
			{
				npc.spriteDirection = -1;
			}
			if (npc.velocity.X > 0f)
			{
				npc.spriteDirection = 1;
			}

			int maxBoostedSpeed = 10;
			int maxNormalSpeed = 7;
			if (speedBoost)
			{
				npc.velocity = Vector2.Clamp(npc.velocity, new Vector2(-maxBoostedSpeed, -maxBoostedSpeed), new Vector2(maxBoostedSpeed, maxBoostedSpeed));
			}
			else
            {
				npc.velocity = Vector2.Clamp(npc.velocity, new Vector2(-maxNormalSpeed, -maxNormalSpeed), new Vector2(maxNormalSpeed, maxNormalSpeed));
			}


			npc.ai[0]++;
			if (npc.ai[0] <= 1 || npc.ai[0] >= 400)
			{
				speedBoost = false;
				timeLock = true;
				SoundDelay = 0;
				npc.dontTakeDamage = false;
				npc.damage = 80;
				Main.npc[srs].damage = 80;
				
				if (!tailSpawned)
				{
					Previous = npc.whoAmI;
					for (int num36 = 0; num36 < 15; num36++)
					{
						if (num36 >= 0 && num36 < 14)
						{
							srs = NPC.NewNPC((int)npc.position.X + (npc.width / 2), (int)npc.position.Y + (npc.height / 2), ModContent.NPCType<NPCs.Bosses.Serris.SerrisBody>(), npc.whoAmI);
						}
						else
						{
							srs = NPC.NewNPC((int)npc.position.X + (npc.width / 2), (int)npc.position.Y + (npc.height / 2), ModContent.NPCType<NPCs.Bosses.Serris.SerrisTail>(), npc.whoAmI);
						}
						if(num36 == 0)
                        {
							nextSegment = npc.whoAmI;
						}
						Main.npc[srs].realLife = npc.whoAmI;
						Main.npc[srs].ai[2] = (float)npc.whoAmI;
						Main.npc[srs].ai[1] = (float)Previous;
						Main.npc[Previous].ai[0] = (float)srs;
						NetMessage.SendData(23, -1, -1, null, srs, 0f, 0f, 0f, 0);
						Previous = srs;
					}
					tailSpawned = true;
				}
			}
			else if (npc.ai[0] >= 2)
			{
				npc.dontTakeDamage = true;
				npc.position += npc.velocity * 1.5f;
				speedBoost = true;
				SoundDelay++;
				npc.damage = 110;
				Main.npc[srs].damage = 110;
				if (SoundDelay > 14)
				{
					Main.PlaySound(mod.GetLegacySoundSlot(SoundType.NPCKilled, "Sounds/Custom/SpeedBooster"), (int)npc.position.X, (int)npc.position.Y);
					SoundDelay = 0;
				}
			}
			if (timeLock)
			{
				npc.ai[0] = 0;
			}

			if (projCooldown > 0)
			{
				projCooldown--;
			}
			else
			{
				//If this isn't the only one left, fire at lower speed to prevent spamming the player to death
				if (NPC.CountNPCS(ModContent.NPCType<NPCs.Bosses.Serris.SerrisHead>()) > 1)
				{
					projCooldown = 250;
				}
				else
				{
					projCooldown = 100;
				}
				float speed = 9f;
				Vector2 vector8 = new Vector2(npc.position.X + (npc.width * 0.5f), npc.position.Y + (npc.height / 2));
				float distanceFactor = Vector2.Distance(vector8, Main.player[npc.target].position) / speed;
				float speedX = ((Main.player[npc.target].position.X + (Main.player[npc.target].width * 0.5f)) - vector8.X) / distanceFactor;
				float speedY = ((Main.player[npc.target].position.Y + (Main.player[npc.target].height * 0.5f)) - vector8.Y) / distanceFactor;
				float angle = (float)Math.Atan2(speedY, speedX);
				Projectile.NewProjectile(vector8.X, vector8.Y, speedX, speedY, ModContent.ProjectileType<Projectiles.Enemy.GravityDistortion>(), distortionDamage, 0f, Main.myPlayer, 0, speed);
			}

		}

        public override void OnHitByProjectile(Projectile projectile, int damage, float knockback, bool crit)
        {
			OnHit(crit);
        }

        public override void OnHitByItem(Player player, Item item, int damage, float knockback, bool crit)
        {
			OnHit(crit);
		}

		private void OnHit(bool crit)
        {
			if (crit)
			{
				npc.life -= 998;
			}
            else
            {
				npc.life -= 999;
			}
			
			if(npc.life <= 0)
            {
				NPCLoot();
            }
			timeLock = false;
			npc.ai[0] = 2;
			Main.PlaySound(15, (int)npc.position.X, (int)npc.position.Y, 0);
		}

		public override void NPCLoot()
		{
			Vector2 vector8 = new Vector2(npc.position.X + (npc.width * 0.5f), npc.position.Y + (npc.height / 2));
			Main.npc[srs].active = false;
			Main.npc[Previous].active = false;
			if (!(NPC.CountNPCS(ModContent.NPCType<NPCs.Bosses.Serris.SerrisHead>()) > 1))
			{
				Main.NewText("Serris has transformed!", Color.Cyan);
				NPC.NewNPC((int)(npc.position.X + (float)(npc.width / 2)), (int)(npc.position.Y + (float)npc.height), ModContent.NPCType<NPCs.Bosses.Serris.SerrisX>(), 0);
			}
			Gore.NewGore(vector8, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Serris Gore 1"), 1f);
		}
		public override void FindFrame(int currentFrame)
		{
			int num = 1;
			if (!Main.dedServ)
			{
				num = Main.npcTexture[npc.type].Height / Main.npcFrameCount[npc.type];
			}
			npc.frameCounter += 1.0;
			if (speedBoost)
			{
				if (npc.frameCounter >= 0 && npc.frameCounter < 15)
				{
					npc.frame.Y = num;
				}
				if (npc.frameCounter >= 15 && npc.frameCounter < 30)
				{
					npc.frame.Y = num * 2;
				}
				if (npc.frameCounter >= 30)
				{
					npc.frameCounter = 0;
				}
			}
			else
			{
				npc.frame.Y = 0;
				npc.frameCounter = 0;
			}
		}
	}
}
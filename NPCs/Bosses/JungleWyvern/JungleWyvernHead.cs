using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items;

namespace tsorcRevamp.NPCs.Bosses.JungleWyvern {
	[AutoloadBossHead]
	class JungleWyvernHead : ModNPC {

		int breathCD = 180;
		bool breath = false;
		int juvenileSpawnTimer = 0;

		public override void SetStaticDefaults() {
            DisplayName.SetDefault("Ancient Jungle Wyvern");
        }
        public override void SetDefaults() {
			npc.aiStyle = 6;
            npc.netAlways = true;
            npc.npcSlots = 100;
            npc.width = 45;
            npc.height = 45;
            npc.timeLeft = 22000;
            npc.damage = 80;
            npc.defense = 40;
            npc.HitSound = SoundID.NPCHit7;
            npc.DeathSound = SoundID.NPCDeath8;
            npc.lifeMax = 24000;
            npc.knockBackResist = 0f;
            npc.noGravity = true;
            npc.noTileCollide = true;
            npc.boss = true;
            npc.value = 90000;
            npc.buffImmune[BuffID.Poisoned] = true;
            npc.buffImmune[BuffID.OnFire] = true;
            npc.buffImmune[BuffID.Confused] = true;
            npc.buffImmune[BuffID.CursedInferno] = true;
			bossBag = ModContent.ItemType<Items.BossBags.JungleWyvernBag>();
			despawnHandler = new NPCDespawnHandler("The Jungle Wyvern departs to seek its next prey...", Color.GreenYellow, DustID.GreenFairy);

		}

		public int CursedFlamesDamage = 22;
		public override void ScaleExpertStats(int numPlayers, float bossLifeScale)
        {
		}

        NPCDespawnHandler despawnHandler;
		public override void AI() {
			despawnHandler.TargetAndDespawn(npc.whoAmI);

			//spawn body
			if (Main.netMode != NetmodeID.MultiplayerClient && npc.ai[0] == 0f) {
				npc.ai[2] = npc.whoAmI;
				npc.realLife = npc.whoAmI;
				int num119 = npc.whoAmI;
				for (int i = 0; i < 22; i++) {
					int npcType = ModContent.NPCType<JungleWyvernBody>();
					switch (i) {
						//2 body parts (0-1)
						case 2: //legs
								//4 body parts (3-6)
						case 7: //legs etc
						case 12:
						case 17:
							npcType = ModContent.NPCType<JungleWyvernLegs>();
							break;
						case 19:
							npcType = ModContent.NPCType<JungleWyvernBody2>();
							break;
						case 20:
							npcType = ModContent.NPCType<JungleWyvernBody3>();
							break;
						case 21:
							npcType = ModContent.NPCType<JungleWyvernTail>();
							break;
					}
					int num122 = NPC.NewNPC((int)(npc.position.X + npc.width / 2), (int)(npc.position.Y + (float)npc.height), npcType, npc.whoAmI);
					Main.npc[num122].ai[2] = npc.whoAmI;
					Main.npc[num122].realLife = npc.whoAmI;
					Main.npc[num122].ai[1] = num119;
					Main.npc[num119].ai[0] = num122;
					NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, num122);
					num119 = num122;
				}
			}
			if (NPC.CountNPCS(mod.NPCType("JungleWyvernJuvenileHead")) < 2)
			{
				juvenileSpawnTimer += Main.rand.Next(1, 3);
			}


			if (juvenileSpawnTimer >= 1900 && NPC.CountNPCS(mod.NPCType("JungleWyvernJuvenileHead")) < 2) //1900 was 1200
			{
				if (Vector2.Distance(Main.player[npc.target].Center, npc.Center) > 500)
				{
					if (Main.netMode != NetmodeID.MultiplayerClient)
					{
						NPC.NewNPC((int)npc.position.X + Main.rand.Next(-20, 20), (int)npc.position.Y + Main.rand.Next(-20, 20), mod.NPCType("JungleWyvernJuvenileHead"));
					}
					juvenileSpawnTimer = 0;
				}
			}

			if (Main.rand.Next(240) == 0) //was 120
			{
				breath = true;
				Main.PlaySound(SoundID.Item, -1, -1, 20);
				npc.netUpdate = true;
			}

			if (breath)
			{

				float rotation = (float)Math.Atan2(npc.Center.Y - Main.player[npc.target].Center.Y, npc.Center.X - Main.player[npc.target].Center.X);
				if (Main.netMode != NetmodeID.MultiplayerClient)
				{
					
					int num54 = Projectile.NewProjectile(npc.Center.X + (5 * npc.direction), npc.Center.Y /*+ (5f * npc.direction)*/, npc.velocity.X * 3f + (float)Main.rand.Next(-2, 2), npc.velocity.Y * 3f + (float)Main.rand.Next(-2, 2), ModContent.ProjectileType<Projectiles.Enemy.JungleWyvernFire>(), CursedFlamesDamage, 0f, Main.myPlayer); //cursed dragons breath
					Main.projectile[num54].timeLeft = 20;//was 25
					Main.projectile[num54].scale = 0.5f;
					
				}
				npc.netUpdate = true;


				breathCD--;

			}

			if (breathCD <= 0)
			{
				breath = false;
				breathCD = 100;
				Main.PlaySound(SoundID.Item, -1, -1, 20);
			}

			if (npc.velocity.X < 0f) {
				npc.spriteDirection = 1;
			}
			if (npc.velocity.X > 0f) {
				npc.spriteDirection = -1;
			}
			float num111 = 12f;
			float Acceleration = 0.15f;
			Vector2 vector14 = new Vector2(npc.position.X + npc.width * 0.5f, npc.position.Y + npc.height * 0.5f);
			float num113 = Main.rand.Next(-500, 500) + Main.player[npc.target].position.X + Main.player[npc.target].width / 2;
			float num114 = Main.rand.Next(-500, 500) + Main.player[npc.target].position.Y + Main.player[npc.target].height / 2;
			num113 = (int)(num113 / 16f) * 16;
			num114 = (int)(num114 / 16f) * 16;
			vector14.X = (int)(vector14.X / 16f) * 16;
			vector14.Y = (int)(vector14.Y / 16f) * 16;
			num113 -= vector14.X;
			num114 -= vector14.Y;
			float num115 = (float)Math.Sqrt(num113 * num113 + num114 * num114);
			float num116 = Math.Abs(num113);
			float num117 = Math.Abs(num114);
			float num118 = num111 / num115;
			num113 *= num118;
			num114 *= num118;
			bool flee = false;
			if (((npc.velocity.X > 0f && num113 < 0f) || (npc.velocity.X < 0f && num113 > 0f) || (npc.velocity.Y > 0f && num114 < 0f) || (npc.velocity.Y < 0f && num114 > 0f)) && Math.Abs(npc.velocity.X) + Math.Abs(npc.velocity.Y) > Acceleration / 2f && num115 < 300f) {
				flee = true;
				if (Math.Abs(npc.velocity.X) + Math.Abs(npc.velocity.Y) < num111) {
					npc.velocity *= 1.1f;
				}
			}
			if (npc.position.Y > Main.player[npc.target].position.Y || Main.player[npc.target].dead) {
				flee = true;
				if (Math.Abs(npc.velocity.X) < num111 / 2f) {
					if (npc.velocity.X == 0f) {
						npc.velocity.X = npc.velocity.X - (float)npc.direction;
					}
					npc.velocity.X = npc.velocity.X * 1.1f;
				}
				else if (npc.velocity.Y > 0f - num111) {
					npc.velocity.Y = npc.velocity.Y - Acceleration;
				}
			}
			if (!flee) {
				if ((npc.velocity.X > 0f && num113 > 0f) || (npc.velocity.X < 0f && num113 < 0f) || (npc.velocity.Y > 0f && num114 > 0f) || (npc.velocity.Y < 0f && num114 < 0f)) {
					if (npc.velocity.X < num113) {
						npc.velocity.X = npc.velocity.X + Acceleration;
					}
					else if (npc.velocity.X > num113) {
						npc.velocity.X = npc.velocity.X - Acceleration;
					}
					if (npc.velocity.Y < num114) {
						npc.velocity.Y = npc.velocity.Y + Acceleration;
					}
					else if (npc.velocity.Y > num114) {
						npc.velocity.Y = npc.velocity.Y - Acceleration;
					}
					if ((double)Math.Abs(num114) < (double)num111 * 0.2 && ((npc.velocity.X > 0f && num113 < 0f) || (npc.velocity.X < 0f && num113 > 0f))) {
						if (npc.velocity.Y > 0f) {
							npc.velocity.Y = npc.velocity.Y + Acceleration * 2f;
						}
						else {
							npc.velocity.Y = npc.velocity.Y - Acceleration * 2f;
						}
					}
					if ((double)Math.Abs(num113) < (double)num111 * 0.2 && ((npc.velocity.Y > 0f && num114 < 0f) || (npc.velocity.Y < 0f && num114 > 0f))) {
						if (npc.velocity.X > 0f) {
							npc.velocity.X = npc.velocity.X + Acceleration * 2f;
						}
						else {
							npc.velocity.X = npc.velocity.X - Acceleration * 2f;
						}
					}
				}
				else if (num116 > num117) {
					if (npc.velocity.X < num113) {
						npc.velocity.X = npc.velocity.X + Acceleration * 1.1f;
					}
					else if (npc.velocity.X > num113) {
						npc.velocity.X = npc.velocity.X - Acceleration * 1.1f;
					}
					if ((double)(Math.Abs(npc.velocity.X) + Math.Abs(npc.velocity.Y)) < (double)num111 * 0.5) {
						if (npc.velocity.Y > 0f) {
							npc.velocity.Y = npc.velocity.Y + Acceleration;
						}
						else {
							npc.velocity.Y = npc.velocity.Y - Acceleration;
						}
					}
				}
				else {
					if (npc.velocity.Y < num114) {
						npc.velocity.Y = npc.velocity.Y + Acceleration * 1.1f;
					}
					else if (npc.velocity.Y > num114) {
						npc.velocity.Y = npc.velocity.Y - Acceleration * 1.1f;
					}
					if ((double)(Math.Abs(npc.velocity.X) + Math.Abs(npc.velocity.Y)) < (double)num111 * 0.5) {
						if (npc.velocity.X > 0f) {
							npc.velocity.X = npc.velocity.X + Acceleration;
						}
						else {
							npc.velocity.X = npc.velocity.X - Acceleration;
						}
					}
				}
			}
			npc.rotation = (float)Math.Atan2(npc.velocity.Y, npc.velocity.X) + 1.57f;
			if (Main.rand.Next(3) == 0) {
				int dust = Dust.NewDust(new Vector2(npc.position.X, npc.position.Y), npc.width, npc.height, 62, 0f, 0f, 100, Color.White, 2f);
				Main.dust[dust].noGravity = true;
			}
			if (npc.life <= 0) {
				npc.active = false;
			}
		}

		public override bool PreDraw(SpriteBatch spriteBatch, Color drawColor) {
			Vector2 origin = new Vector2(Main.npcTexture[npc.type].Width / 2, Main.npcTexture[npc.type].Height / Main.npcFrameCount[npc.type] / 2);
			Color alpha = Color.White;
			SpriteEffects effects = SpriteEffects.None;
			if (npc.spriteDirection == 1) {
				effects = SpriteEffects.FlipHorizontally;
			}
			spriteBatch.Draw(Main.npcTexture[npc.type], new Vector2(npc.position.X - Main.screenPosition.X + npc.width / 2 - (float)Main.npcTexture[npc.type].Width * npc.scale / 2f + origin.X * npc.scale, npc.position.Y - Main.screenPosition.Y + (float)npc.height - (float)Main.npcTexture[npc.type].Height * npc.scale / (float)Main.npcFrameCount[npc.type] + 4f + origin.Y * npc.scale + 56f), npc.frame, alpha, npc.rotation, origin, npc.scale, effects, 0f);
			npc.alpha = 255;
			return true;
		}

		private static int ClosestSegment(NPC head, params int[] segmentIDs) { 
			List<int> segmentIDList = new List<int>(segmentIDs);
			Vector2 targetPos = Main.player[head.target].Center;
			int closestSegment = head.whoAmI; //head is default, updates later
			float minDist = 1000000f; //arbitrarily large, updates later
			for (int i = 0; i < Main.npc.Length; i++) { //iterate through every NPC
				NPC npc = Main.npc[i];
				if (npc != null && npc.active && segmentIDList.Contains(npc.type)) { //if the npc is part of the wyvern
					float targetDist = (npc.Center - targetPos).Length();
					if (targetDist < minDist) { //if we're closer than the previously closer segment (or closer than 1,000,000 if it's the first iteration, so always)
						minDist = targetDist; //update minDist. future iterations will compare against the updated value
						closestSegment = i; //and set closestSegment to the whoAmI of the closest segment
                    }
                }
            }
			return closestSegment; //the whoAmI of the closest segment
		}

		public override bool SpecialNPCLoot() {
			for(int i = 0; i < Main.maxNPCs; i++)
            {
				if(Main.npc[i].active && Main.npc[i].type == ModContent.NPCType<NPCs.Enemies.JungleWyvernJuvenile.JungleWyvernJuvenileHead>())
                {
					Main.npc[i].active = false;
                }
            }
			int closestSegmentID = ClosestSegment(npc, ModContent.NPCType<JungleWyvernBody>(), ModContent.NPCType<JungleWyvernBody2>(), ModContent.NPCType<JungleWyvernBody3>(), ModContent.NPCType<JungleWyvernTail>());
			npc.position = Main.npc[closestSegmentID].position; //teleport the head to the location of the closest segment before running npcloot
			return false;
		}
		public override bool CheckActive()
		{
			return false;
		}
		public override void BossLoot(ref string name, ref int potionType)
		{
			potionType = ItemID.GreaterHealingPotion;
		}
		public override void OnHitByItem(Player player, Item item, int damage, float knockback, bool crit)
		{
			damage *= 2;
			base.OnHitByItem(player, item, damage, knockback, crit);
		}
		public override void NPCLoot() {

			if (Main.expertMode)
			{
				npc.DropBossBags();
			}
			else
			{

				Item.NewItem(npc.getRect(), ModContent.ItemType<Items.Accessories.ChloranthyRing>(), 1, false, -1);
				Item.NewItem(npc.getRect(), ItemID.Sapphire, Main.rand.Next(2, 10));
				Item.NewItem(npc.getRect(), ItemID.Ruby, Main.rand.Next(2, 10));
				Item.NewItem(npc.getRect(), ItemID.Topaz, Main.rand.Next(2, 10));
				Item.NewItem(npc.getRect(), ItemID.Diamond, Main.rand.Next(2, 10));
				Item.NewItem(npc.getRect(), ItemID.Emerald, Main.rand.Next(2, 10));
				Item.NewItem(npc.getRect(), ItemID.Amethyst, Main.rand.Next(2, 10));
				Item.NewItem(npc.getRect(), ItemID.Amethyst, Main.rand.Next(2, 10));
				Item.NewItem(npc.getRect(), ItemID.NecroHelmet);
				Item.NewItem(npc.getRect(), ItemID.NecroBreastplate);
				Item.NewItem(npc.getRect(), ItemID.NecroGreaves);
				if (!(tsorcRevampWorld.Slain.ContainsKey(ModContent.NPCType<JungleWyvernHead>())))
				{ //If the boss has not yet been killed
					Item.NewItem(npc.getRect(), ModContent.ItemType<DarkSoul>(), 9000); //Then drop the souls
					Item.NewItem(npc.getRect(), ModContent.ItemType<Items.StaminaVessel>());

				}
			}
		}
    }
}

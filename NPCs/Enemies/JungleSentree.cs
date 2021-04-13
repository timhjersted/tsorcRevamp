using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.NPCs.Enemies
{
	public class JungleSentree : ModNPC // Source of rich mahogany - drops extra wood when hit with axe - takes 2x damage from axes
	{
		public override bool Autoload(ref string name) => !ModContent.GetInstance<tsorcRevampConfig>().LegacyMode;
		public override void SetStaticDefaults()
		{
			DisplayName.SetDefault("Jungle Sentree");
			Main.npcFrameCount[npc.type] = 9;
		}

		public override void SetDefaults()
		{
			npc.width = 48;
			npc.height = 140;
			npc.aiStyle = -1;
			npc.damage = 25;
			npc.knockBackResist = 0;
			npc.defense = 12;
			npc.lifeMax = 250;
			npc.HitSound = mod.GetLegacySoundSlot(SoundType.NPCHit, "Sounds/NPCHit/Dig");
			npc.DeathSound = SoundID.NPCDeath33;
			npc.value = 250;
			npc.buffImmune[BuffID.Confused] = true;
		}

		public override float SpawnChance(NPCSpawnInfo spawnInfo)
		{
			float chance = 0;

			if (spawnInfo.player.ZoneJungle && NPC.CountNPCS(mod.NPCType("JungleSentree")) < 2
				&& TileID.Sets.Conversion.Grass[spawnInfo.spawnTileType]
				&& Main.tile[spawnInfo.spawnTileX, spawnInfo.spawnTileY].wall == WallID.None
				&& Main.tile[spawnInfo.spawnTileX - 1, spawnInfo.spawnTileY].type == TileID.JungleGrass && !Main.tile[spawnInfo.spawnTileX - 1, spawnInfo.spawnTileY].halfBrick() && !Main.tile[spawnInfo.spawnTileX - 1, spawnInfo.spawnTileY].leftSlope() //all this is to prevent the npc spawning in really odd looking places
				&& Main.tile[spawnInfo.spawnTileX + 1, spawnInfo.spawnTileY].type == TileID.JungleGrass && !Main.tile[spawnInfo.spawnTileX + 1, spawnInfo.spawnTileY].halfBrick() && !Main.tile[spawnInfo.spawnTileX + 1, spawnInfo.spawnTileY].rightSlope()//make sure block to left and right are jungle grass
				&& Main.tile[spawnInfo.spawnTileX - 1, spawnInfo.spawnTileY - 1].type != TileID.JungleGrass && Main.tile[spawnInfo.spawnTileX - 1, spawnInfo.spawnTileY - 1].type != TileID.Mud && !Main.tile[spawnInfo.spawnTileX - 1, spawnInfo.spawnTileY - 1].halfBrick() && !Main.tile[spawnInfo.spawnTileX - 1, spawnInfo.spawnTileY - 1].rightSlope() //make sure blocks to left/right and above are empty
				&& Main.tile[spawnInfo.spawnTileX + 1, spawnInfo.spawnTileY - 1].type != TileID.JungleGrass && Main.tile[spawnInfo.spawnTileX + 1, spawnInfo.spawnTileY - 1].type != TileID.Mud && !Main.tile[spawnInfo.spawnTileX + 1, spawnInfo.spawnTileY - 1].halfBrick() && !Main.tile[spawnInfo.spawnTileX + 1, spawnInfo.spawnTileY - 1].rightSlope())
			
			{
					return 0.6f; // It's high because the chance of the conditions being right is pretty low
			}
					return chance;
		}

        #region AI
        private const int AI_State_Slot = 0;
		private const int AI_Timer_Slot = 1;

		private const int State_Asleep = 0;
		private const int State_Notice = 1;
		private const int State_Angered = 2;

		public float AI_State
		{
			get => npc.ai[AI_State_Slot];
			set => npc.ai[AI_State_Slot] = value;
		}

		public float AI_Timer
		{
			get => npc.ai[AI_Timer_Slot];
			set => npc.ai[AI_Timer_Slot] = value;
		}

		public int spawntimer = 0;

		// Our AI here makes our NPC sit waiting for a player to enter range then spawns minions to attack.
		public override void AI()
		{
			npc.GivenName = "???";
			// The npc starts in the asleep state, waiting for a player to enter range
			if (AI_State == State_Asleep)
			{
				// TargetClosest sets npc.target to the player.whoAmI of the closest player. the faceTarget parameter means that npc.direction will automatically be 1 or -1 if the targeted player is to the right or left. This is also automatically flipped if npc.confused
				npc.TargetClosest(true);
				// Now we check the make sure the target is still valid and within our specified notice range (350)
				if (npc.HasValidTarget && Main.player[npc.target].Distance(npc.Center) < 400f)
				{
					// Since we have a target in range, we change to the Notice state. (and zero out the Timer for good measure)
					AI_State = State_Notice;
					AI_Timer = 0;
				}
				if ((npc.life < npc.lifeMax) && (Main.rand.Next(4) == 0))
				{
					Dust.NewDust(npc.position, npc.width, npc.height, 107, 0, 0, 0, default(Color), 1f); //regenerating hp
				}
			}
			// In this state, a player has been targeted
			else if (AI_State == State_Notice)
			{
				// If the targeted player is in attack range (250).
				if (Main.player[npc.target].Distance(npc.Center) < 300f)
				{
					// Here we use our Timer to wait a fraction of a second before spawning babies.
					AI_Timer++;
					if (AI_Timer >= 20)
					{
						AI_State = State_Angered;
						AI_Timer = 0;
					}
				}
				else
				{
					npc.TargetClosest(true);
					if (!npc.HasValidTarget || Main.player[npc.target].Distance(npc.Center) > 550f)
					{
						// Out targeted player seems to have left our range, so we'll go back to sleep.
						AI_State = State_Asleep;
						AI_Timer = 0;
					}
				}
			}
			// In this state, begin to spawn babies.
			else if (AI_State == State_Angered)
			{
				npc.GivenName = "Jungle Sentree";
				//int randomness = Main.rand.Next(3);
				spawntimer++;
				if (npc.life > npc.lifeMax / 3)
				{
					if (Main.rand.Next(20) == 0)
					{
						Dust.NewDust(npc.position - new Vector2(20, 0), npc.width / 3, npc.height / 2, 18, Main.rand.Next(-2, 0), Main.rand.Next(-2, 0), 0, default(Color), 1f); //left branch
					}
					if (Main.rand.Next(20) == 0)
					{
						Dust.NewDust(npc.position - new Vector2(-42, 0), npc.width / 3, npc.height / 2, 18, Main.rand.Next(0, 2), Main.rand.Next(-2, 0), 0, default(Color), 1f); //right branch
					}
				}
				if (npc.life <= npc.lifeMax / 3)
				{
					if (Main.rand.Next(10) == 0)
					{
						Dust.NewDust(npc.position - new Vector2(20, 0), npc.width / 3, npc.height / 2, 18, Main.rand.Next(-2, 0), Main.rand.Next(-2, 0), 0, default(Color), 1f); //left branch
					}
					if (Main.rand.Next(10) == 0)
					{
						Dust.NewDust(npc.position - new Vector2(-42, 0), npc.width / 3, npc.height / 2, 18, Main.rand.Next(0, 2), Main.rand.Next(-2, 0), 0, default(Color), 1f); //right branch
					}
				}


				if (spawntimer >= 0 && spawntimer <= 40 && (NPC.CountNPCS(NPCID.JungleBat) < 6))
				{
					if (Main.rand.Next(8) == 0)
					{
						Dust.NewDust(new Vector2((int)(npc.position.X + (float)(npc.width / 2) + npc.velocity.X), (int)(npc.position.Y + (float)(npc.height - 118) + npc.velocity.Y)), 2, 2, 18, Main.rand.NextFloat(-1.1f, 1.1f), Main.rand.NextFloat(-1.1f, 1.1f), 0, default(Color), 1f);
					}
				}
				if (spawntimer > 40 && spawntimer <= 60 && (NPC.CountNPCS(NPCID.JungleBat) < 6))
				{
					Dust.NewDust(npc.position, npc.width / 2, npc.height / 4, 18, Main.rand.NextFloat(-1.5f, 1.5f), Main.rand.NextFloat(-1.5f, 1.5f), 0, default(Color), 1f);
				}
				if (spawntimer == 60 && (NPC.CountNPCS(NPCID.JungleBat) < 6) && npc.Center.Y / 16 < Main.rockLayer) //wont spawn babies if there are already 5
				{
					int npcIndex = -1;

					npcIndex = NPC.NewNPC((int)(npc.position.X + (float)(npc.width / 2) + npc.velocity.X), (int)(npc.position.Y + (float)(npc.height - 118) + npc.velocity.Y), NPCID.JungleBat);

					if (npcIndex >= 0)
					{
						Main.npc[npcIndex].value /= 2;
					}

					//play sound, make dust
					Main.PlaySound(SoundID.Item76, npc.Center);
					for (int i = 0; i < 30; i++)
					{
						Dust dust = Main.dust[Dust.NewDust(new Vector2((int)(npc.position.X + (float)(npc.width / 2) + npc.velocity.X), (int)(npc.position.Y + (float)(npc.height - 118) + npc.velocity.Y)), 2, 2, 18, Main.rand.Next(-5, 5), Main.rand.Next(-5, 5), 0, default(Color), 1f)]; //glowy nature dust
						dust.noGravity = true;
						dust.fadeIn = .1f;
					}
				}
				if (spawntimer == 60 && (NPC.CountNPCS(NPCID.JungleBat) < 6) && npc.Center.Y / 16 >= Main.rockLayer) //wont spawn babies if there are already 5
				{
					if (Main.rand.Next(2) == 0)
					{
						Main.PlaySound(SoundID.Item97, npc.Center);
						int npcIndex = -1;
						//NPC.NewNPC((int)(npc.position.X + (float)(npc.width / 2) + npc.velocity.X), (int)(npc.position.Y + (float)(npc.height - 118) + npc.velocity.Y), NPCID.LittleHornetLeafy);
						npcIndex = NPC.NewNPC((int)(npc.position.X + (float)(npc.width / 2) + npc.velocity.X), (int)(npc.position.Y + (float)(npc.height - 118) + npc.velocity.Y), NPCID.LittleHornetLeafy);

						if (npcIndex >= 0)
						{
							Main.npc[npcIndex].value /= 2;
						}
					}
					else
					{
						Main.PlaySound(SoundID.Item76, npc.Center);
						int npcIndex2 = -1;

						npcIndex2 = NPC.NewNPC((int)(npc.position.X + (float)(npc.width / 2) + npc.velocity.X), (int)(npc.position.Y + (float)(npc.height - 118) + npc.velocity.Y), NPCID.JungleBat);

						if (npcIndex2 >= 0)
						{
							Main.npc[npcIndex2].value /= 2;
						}
					}
					//make dust
					for (int i = 0; i < 30; i++)
					{
						Dust dust = Main.dust[Dust.NewDust(new Vector2((int)(npc.position.X + (float)(npc.width / 2) + npc.velocity.X), (int)(npc.position.Y + (float)(npc.height - 118) + npc.velocity.Y)), 2, 2, 18, Main.rand.Next(-5, 5), Main.rand.Next(-5, 5), 0, default(Color), 1f)]; //glowy nature dust
						dust.noGravity = true;
						dust.fadeIn = .1f;
					}
				}
				if (spawntimer == 300 && npc.life > npc.lifeMax / 3)
				{
					spawntimer = 0;
				}
				if (spawntimer == 180 && npc.life <= npc.lifeMax / 3)
				{
					spawntimer = 0;
				}

				npc.TargetClosest(true);
				if (!npc.HasValidTarget || Main.player[npc.target].Distance(npc.Center) > 550f)
				{
					// Out targeted player seems to have left our range, so we'll go back to sleep.
					AI_State = State_Asleep;
					AI_Timer = 0;
					spawntimer = 0;
				}

			}
		}
        #endregion

        #region Animation

        private const int Frame_Asleep = 0;
		private const int Frame_Notice = 1;
		private const int Frame_Angered_1 = 2;
		private const int Frame_Angered_2 = 3;
		private const int Frame_Angered_3 = 4;
		private const int Frame_Angered_4 = 5;
		private const int Frame_Angered_5 = 6;
		private const int Frame_Angered_6 = 7;
		private const int Frame_Angered_7 = 8;


		public override void FindFrame(int frameHeight)
		{
			// This makes the sprite flip horizontally in conjunction with the npc.direction.
			//npc.spriteDirection = npc.direction;

			// For the most part, our animation matches up with our states.
			if (AI_State == State_Asleep)
			{
				// npc.frame.Y is the goto way of changing animation frames. npc.frame starts from the top left corner in pixel coordinates, so keep that in mind.
				npc.frame.Y = Frame_Asleep * frameHeight;
			}
			else if (AI_State == State_Notice)
			{
				// Going from Notice to Asleep makes our npc look like it's crouching to jump.
				if (AI_Timer < 10)
				{
					npc.frame.Y = Frame_Notice * frameHeight;
				}
				else
				{
					npc.frame.Y = Frame_Asleep * frameHeight;
				}
			}
			else if (AI_State == State_Angered)
			{
				// Cycle through all 8 frames
				if (npc.life > npc.lifeMax / 3)
				{
					npc.frameCounter += 1;
				}
				if (npc.life <= npc.lifeMax / 3)
				{
					npc.frameCounter += 2;
				}

				if (npc.frameCounter < 10)
				{
					npc.frame.Y = Frame_Notice * frameHeight;
				}
				else if (npc.frameCounter < 10)
				{
					npc.frame.Y = Frame_Angered_1 * frameHeight;
				}
				else if (npc.frameCounter < 20)
				{
					npc.frame.Y = Frame_Angered_2 * frameHeight;
				}
				else if (npc.frameCounter < 30)
				{
					npc.frame.Y = Frame_Angered_3 * frameHeight;
				}
				else if (npc.frameCounter < 40)
				{
					npc.frame.Y = Frame_Angered_4 * frameHeight;
				}
				else if (npc.frameCounter < 50)
				{
					npc.frame.Y = Frame_Angered_5 * frameHeight;
				}
				else if (npc.frameCounter < 60)
				{
					npc.frame.Y = Frame_Angered_6 * frameHeight;
				}
				else if (npc.frameCounter < 70)
				{
					npc.frame.Y = Frame_Angered_7 * frameHeight;
				}
				else
				{
					npc.frameCounter = 0;
				}
			}
		}

        #endregion

        #region Misc

        public override void UpdateLifeRegen(ref int damage)
		{
			npc.lifeRegen = 2;

			if (AI_State == State_Asleep)
			{
				 npc.lifeRegen = npc.lifeMax / 10; 
			}
		}
		public int wooddropped = 0;
		public override void ModifyHitByItem(Player player, Item item, ref int damage, ref float knockback, ref bool crit)
		{
			if ((item.type == ItemID.CopperAxe) || (item.type == ItemID.TinAxe) || (item.type == ItemID.IronAxe) || (item.type == ItemID.LeadAxe) || (item.type == ItemID.LeadAxe) || (item.type == ItemID.SilverAxe) || (item.type == ItemID.TungstenAxe) || (item.type == ItemID.GoldAxe) || (item.type == ItemID.PlatinumAxe)
				/*continued*/|| (item.type == ItemID.WarAxeoftheNight) || (item.type == ItemID.BloodLustCluster) || (item.type == ItemID.MeteorHamaxe) || (item.type == ItemID.MoltenHamaxe) || (item.type == ItemID.CobaltWaraxe) || (item.type == ItemID.CobaltChainsaw) || (item.type == ItemID.PalladiumWaraxe) || (item.type == ItemID.PalladiumChainsaw)
				/*half way ugh*/|| (item.type == ItemID.MythrilWaraxe) || (item.type == ItemID.MythrilChainsaw) || item.type == ItemID.OrichalcumWaraxe || (item.type == ItemID.OrichalcumChainsaw) || (item.type == ItemID.AdamantiteWaraxe) || (item.type == ItemID.AdamantiteChainsaw) || (item.type == ItemID.TitaniumWaraxe)
				/*regret*/|| (item.type == ItemID.TitaniumChainsaw) || (item.type == ItemID.PickaxeAxe) || (item.type == ItemID.SawtoothShark) || (item.type == ItemID.Drax) || (item.type == ItemID.ChlorophyteGreataxe) || (item.type == ItemID.ChlorophyteChainsaw) || (item.type == ItemID.ButchersChainsaw)
				/*Do ittttttttt! Kill meeeee! Aghhh agh aghh!*/|| (item.type == ItemID.TheAxe) || (item.type == ItemID.Picksaw) || (item.type == ItemID.ShroomiteDiggingClaw) || (item.type == ItemID.SpectreHamaxe) || (item.type == ItemID.SolarFlareAxe) || (item.type == ItemID.NebulaAxe) || (item.type == ItemID.StardustAxe)
				 || (item.type == ItemID.VortexAxe) || item.type == mod.ItemType("AdamantitePoleWarAxe") || item.type == mod.ItemType("AdamantiteWarAxe") || item.type == mod.ItemType("AncientFireAxe") || item.type == mod.ItemType("CobaltPoleWarAxe") || item.type == mod.ItemType("CobaltWarAxe")
				/*top 10 biggest mistakes of my life*/|| item.type == mod.ItemType("DunlendingAxe") || item.type == mod.ItemType("EphemeralThrowingAxe") || item.type == mod.ItemType("FieryPoleWarAxe") || item.type == mod.ItemType("FieryWarAxe") || item.type == mod.ItemType("HallowedGreatPoleAxe")
				/*spent more time making this list than the NPC iteself*/|| item.type == mod.ItemType("MythrilPoleWarAxe") || item.type == mod.ItemType("MythrilWarAxe") || item.type == mod.ItemType("OldAxe") || item.type == mod.ItemType("OldDoubleAxe") || item.type == mod.ItemType("OldHalberd")
				|| item.type == mod.ItemType("ReforgedOldAxe") || item.type == mod.ItemType("ReforgedOldDoubleAxe") || (item.type == mod.ItemType("ReforgedOldHalberd")) || (item.type == mod.ItemType("ForgottenAxe")) || (item.type == mod.ItemType("ForgottenGreatAxe")))
			{
				damage *= 2; //I never want to see or hear the word "axe" again in my life
				if (damage < 15)
				{
					damage = 15; //damage before defence
				}
				if (Main.rand.Next(2) == 0 && wooddropped < 5)
				{
					Item.NewItem(npc.Bottom, ItemID.RichMahogany);
					wooddropped++;
				}
			}
		}
		public override void ModifyHitByProjectile(Projectile projectile, ref int damage, ref float knockback, ref bool crit, ref int hitDirection)
		{
			damage -= 5; //because lets face it, no one ever uprooted a tree with a bullet... A missle? Perhaps
		}
		public override void HitEffect(int hitDirection, double damage)
		{
			for (int i = 0; i < 5; i++)
			{
				int dustType = 7;
				int dustIndex = Dust.NewDust(npc.position, npc.width, npc.height, dustType);
				Dust dust = Main.dust[dustIndex];

				dust.scale *= 1f + Main.rand.Next(-30, 31) * 0.01f;
				dust.velocity.Y = Main.rand.Next(-3, 0);
				dust.noGravity = false;
			}
			if (npc.life <= 0)
			{
				for (int i = 0; i < 35; i++)
				{
					Dust.NewDust(npc.position, npc.width, npc.height, 7, 0, Main.rand.Next(-3, 0), 0, default(Color), 1f);
				}
			}
		}

        #endregion

        public override void NPCLoot()
		{
			Item.NewItem(npc.getRect(), ItemID.RichMahogany, Main.rand.Next(3, 5));
		}
	}
}

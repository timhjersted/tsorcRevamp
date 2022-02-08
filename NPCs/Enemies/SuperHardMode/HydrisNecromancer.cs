using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework.Graphics;

namespace tsorcRevamp.NPCs.Enemies.SuperHardMode
{
	class HydrisNecromancer : ModNPC
	{
		public override void SetStaticDefaults()
		{			
			DisplayName.SetDefault("Hydris Necromancer");
		}
		public override void SetDefaults()
		{
			npc.npcSlots = 5;
			Main.npcFrameCount[npc.type] = 15;
			animationType = 21;
			npc.knockBackResist = 0.1f;
			npc.aiStyle = 3; //was 3
			npc.damage = 120;
			npc.defense = 75; //was 135
			npc.height = 40;
			npc.width = 20;
			npc.lifeMax = 8000;
			npc.lavaImmune = true;
			npc.HitSound = SoundID.NPCHit1;
			npc.DeathSound = SoundID.NPCDeath1;
			npc.value = 27050; //was 1600 souls
			banner = npc.type;
			bannerItem = ModContent.ItemType<Banners.HydrisNecromancerBanner>();

		}

		int deathStrikeDamage = 65;

		public override void ScaleExpertStats(int numPlayers, float bossLifeScale)
		{
			npc.lifeMax = (int)(npc.lifeMax / 2);
			npc.damage = (int)(npc.damage / 2);
			deathStrikeDamage = (int)(deathStrikeDamage * tsorcRevampWorld.SubtleSHMScale);
		}

		//Spawns in the Underground and Cavern before 3.5/10ths and after 7.5/10ths (Width). Does not Spawn in the Jungle, Meteor, or if there are Town NPCs.

		#region Spawn
		public override float SpawnChance(NPCSpawnInfo spawnInfo)
		{

			Player P = spawnInfo.player; //this shortens our code up from writing this line over and over.
			bool Hallow = P.ZoneHoly;
			bool oMagmaCavern = (spawnInfo.spawnTileY >= (Main.maxTilesY * 0.6f) && spawnInfo.spawnTileY < (Main.maxTilesY * 0.8f));
			bool oUnderworld = (spawnInfo.spawnTileY >= (Main.maxTilesY * 0.8f));

			if (tsorcRevampWorld.SuperHardMode && (P.ZoneDirtLayerHeight || P.ZoneRockLayerHeight || oMagmaCavern))
			{
				if (Hallow && Main.rand.Next(20) == 1) return 1; //was 20
				if (spawnInfo.player.ZoneGlowshroom && Main.rand.Next(20) == 1) return 1; //was 20
				if (spawnInfo.player.ZoneUndergroundDesert && Main.rand.Next(20) == 1) return 1; //was 20
				if (Hallow && Main.bloodMoon && Main.rand.Next(6) == 1) return 1;
				if ((spawnInfo.spawnTileX < Main.maxTilesX * 0.35f || spawnInfo.spawnTileX > Main.maxTilesX * 0.75f) && Main.rand.Next(20) == 1) return 1; //was 10
				if (spawnInfo.spawnTileType == TileID.BoneBlock && spawnInfo.player.ZoneDungeon && Main.rand.Next(20) == 1)
					return 0;
			}

			else if (tsorcRevampWorld.SuperHardMode && oUnderworld)
			{
				if (Main.rand.Next(60) == 1) return 1;
				if ((spawnInfo.spawnTileX < Main.maxTilesX * 0.35f || spawnInfo.spawnTileX > Main.maxTilesX * 0.75f) && Main.rand.Next(30) == 1) return 1;
				return 0;
			}
			return 0;
		}
		#endregion



		float strikeTimer = 0;
		float skeletonTimer = 0;
        public override void AI()
		{
			tsorcRevampAIs.FighterAI(npc, 1.8f, 0.05f, canTeleport: true, lavaJumping: true);

			strikeTimer++;
			skeletonTimer++;
			bool lineOfSight = Collision.CanHit(npc.position, npc.width, npc.height, Main.player[npc.target].position, Main.player[npc.target].width, Main.player[npc.target].height);
			tsorcRevampAIs.SimpleProjectile(npc, ref strikeTimer, 150, ModContent.ProjectileType<Projectiles.Enemy.EnemySpellSuddenDeathStrike>(), deathStrikeDamage, 8, lineOfSight && Main.rand.NextBool(), false, 2, 17, 0);
			if (tsorcRevampAIs.SimpleProjectile(npc, ref strikeTimer, 150, ModContent.ProjectileType<Projectiles.Enemy.EnemySpellEffectHealing>(), 0, 0, !lineOfSight, false, 2, 17, 0))
			{
				npc.life += 10;
				npc.HealEffect(10);
				if (npc.life > npc.lifeMax) npc.life = npc.lifeMax;
			}

			//IF HIT BEFORE PINK DUST TELEGRAPH, RESET TIMER, BUT CHANCE TO BREAK STUN LOCK
			//(WORKS WITH 2 TELEGRAPH DUSTS IN DRAW, AT TIMER 60 AND 110)
			if (npc.justHit && strikeTimer <= 109 )
			{
				if (Main.rand.Next(3) == 0)
				{
					strikeTimer = 110;
				}
				else
                {
					strikeTimer = 0;
				}
			}
			if (npc.justHit && Main.rand.Next(18) == 1)
			{
				tsorcRevampAIs.Teleport(npc, 20, true);
				strikeTimer = 70f;
			}



			if (skeletonTimer > 300 && lineOfSight)
			{
				skeletonTimer = 0;

				if (Main.netMode != NetmodeID.MultiplayerClient)
				{
					int spawnType;				
					if (Main.rand.NextBool())
					{
						spawnType = ModContent.NPCType<NPCs.Enemies.HollowSoldier>();
					}
					else
					{
						spawnType = ModContent.NPCType<NPCs.Enemies.SuperHardMode.HydrisElemental>(); //NPCID.ChaosElemental;
					}

					int spawnedNPC = NPC.NewNPC((int)npc.Center.X, (int)npc.Center.Y, spawnType, 0);
					if (Main.netMode == NetmodeID.Server)
					{
						NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, spawnedNPC, 0f, 0f, 0f, 0);
					}
				}
			}
		}

        #region DRAW
		public override void PostDraw(SpriteBatch spriteBatch, Color drawColor)
		{
			//BlACK DUST is used to show stunlock worked, PINK is used to show unstoppable attack incoming
			//BLACK DUST
			if (strikeTimer >= 60)
			{
				Lighting.AddLight(npc.Center, Color.WhiteSmoke.ToVector3() * 2f); //Pick a color, any color. The 0.5f tones down its intensity by 50%
				if (Main.rand.Next(2) == 1)
				{
					//Dust.NewDust(npc.position, npc.width, npc.height, 41, npc.velocity.X, npc.velocity.Y); //41 wassss weird anti-gravity blue dust but now I'm seeing grass clippings; not sure what happened
					Dust.NewDust(npc.position, npc.width, npc.height, 54, (npc.velocity.X * 0.2f), npc.velocity.Y * 0.2f, 100, default, 1f); //54 is black smoke
					Dust.NewDust(npc.position, npc.width, npc.height, 54, (npc.velocity.X * 0.2f), npc.velocity.Y * 0.2f, 100, default, 2f); 
					
				}
			}
			//PINK DUST
			if (strikeTimer >= 110)
			{
				Lighting.AddLight(npc.Center, Color.WhiteSmoke.ToVector3() * 2f); //Pick a color, any color. The 0.5f tones down its intensity by 50%
				if (Main.rand.Next(2) == 1)
				{
					int pink = Dust.NewDust(npc.position, npc.width, npc.height, DustID.CrystalSerpent, npc.velocity.X, npc.velocity.Y, Scale: 1.5f);

					Main.dust[pink].noGravity = true;
				}
			}
		}
		#endregion


		#region Gore
		public override void NPCLoot()
		{
			if (npc.life <= 0)
			{
				Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Necromancer Gore 1"), 1.1f);
				Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Necromancer Gore 2"), 1.1f);
				Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Necromancer Gore 3"), 1.1f);
				Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Necromancer Gore 2"), 1.1f);
				Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Necromancer Gore 3"), 1.1f);
			}
			Item.NewItem(npc.getRect(), ModContent.ItemType<Items.DyingWindShard>(), 8 + Main.rand.Next(8));
		}
		#endregion
	}
}
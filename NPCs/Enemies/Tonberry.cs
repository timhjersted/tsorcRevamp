using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System;
using Microsoft.Xna.Framework.Graphics;

namespace tsorcRevamp.NPCs.Enemies
{
	class Tonberry : ModNPC
	{
		public override void SetDefaults()
		{
			npc.npcSlots = 5;
			Main.npcFrameCount[npc.type] = 16;
			animationType = 28;
			npc.knockBackResist = 0.2f;
			npc.aiStyle = 3;
			npc.damage = 0;
			npc.defense = 30;
			npc.height = 40;
			npc.width = 20;
			npc.lifeMax = 3000;
			npc.HitSound = SoundID.NPCHit1;
			npc.DeathSound = SoundID.NPCDeath1;
			npc.value = 25000;
			banner = npc.type;
			bannerItem = ModContent.ItemType<Banners.TonberryBanner>();

			if (tsorcRevampWorld.SuperHardMode)
			{
				npc.lifeMax = 6660;
				npc.defense = 57;
				npc.value = 70000;
				npc.damage = 295;
			}
		}

		public override void ScaleExpertStats(int numPlayers, float bossLifeScale)
		{
			npc.lifeMax = (int)(npc.lifeMax / 2);
			npc.damage = (int)(npc.damage / 2);
		}

		// (O_O;)
		int throwingKnifeDamage = 9999;


		#region Spawn
		public override float SpawnChance(NPCSpawnInfo spawnInfo)
		{
			Player P = spawnInfo.player;
			bool InGrayLayer = spawnInfo.spawnTileY >= Main.rockLayer && spawnInfo.spawnTileY < (Main.maxTilesY - 200) * 16;
			bool FrozenOcean = spawnInfo.spawnTileX > (Main.maxTilesX - 800);

			if (spawnInfo.water) return 0f;

			if (Main.hardMode && !FrozenOcean && Main.rand.Next(200) == 1) return 1; 
			
			if (tsorcRevampWorld.SuperHardMode && P.ZoneDungeon && Main.rand.Next(30) == 1) return 1;

			if (tsorcRevampWorld.SuperHardMode && P.ZoneJungle && Main.rand.Next(75) == 1) return 1;

			if (tsorcRevampWorld.SuperHardMode && !Main.dayTime && InGrayLayer && Main.rand.Next(100) == 1) return 1;

			if (tsorcRevampWorld.SuperHardMode && !Main.dayTime && Main.rand.Next(100) == 1) return 1;

			return 0;
		}
		#endregion

		float knifeTimer = 0;
		public override void AI()
		{
			tsorcRevampAIs.FighterAI(npc, 1f, 0.07f, 0.5f, lavaJumping: true);

			bool clearShot = Collision.CanHit(npc.position, npc.width, npc.height, Main.player[npc.target].position, Main.player[npc.target].width, Main.player[npc.target].height) && Vector2.Distance(npc.Center, Main.player[npc.target].Center) <= 500;
			tsorcRevampAIs.SimpleProjectile(npc, ref knifeTimer, 180, ModContent.ProjectileType<Projectiles.Enemy.EnemyThrowingKnifeSmall>(), throwingKnifeDamage, 8, clearShot, soundType: 2, soundStyle: 17);			

			//play creature sounds
			if (Main.rand.Next(1000) == 1)
			{
				Main.PlaySound(3, (int)npc.position.X, (int)npc.position.Y, 55, 0.3f, -0.7f); // cultist
			}

			Lighting.AddLight((int)npc.position.X / 16, (int)npc.position.Y / 16, 0.5f, 0.4f, 0.4f);			
		}
		
		#region Gore
		public override void NPCLoot()
		{
			if (npc.life <= 0)
			{
				Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Tonberry Gore 1"), 1f);
				Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Tonberry Gore 2"), 1f);
				Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Tonberry Gore 3"), 1f);
			}

			if (tsorcRevampWorld.SuperHardMode)
			{
				Item.NewItem(npc.getRect(), ModContent.ItemType<Items.RedTitanite>(), 5 + Main.rand.Next(5));
				Item.NewItem(npc.getRect(), ModContent.ItemType<Items.WhiteTitanite>(), 5 + Main.rand.Next(5));
				Item.NewItem(npc.getRect(), ModContent.ItemType<Items.BlueTitanite>(), 5 + Main.rand.Next(5));
			}
		}
		#endregion

		static Texture2D spearTexture;
		public override void PostDraw(SpriteBatch spriteBatch, Color drawColor)
		{
			if (spearTexture == null || spearTexture.IsDisposed)
			{
				spearTexture = mod.GetTexture("Projectiles/Enemy/EnemyThrowingKnifeSmall");
			}
			if (knifeTimer >= 120)
			{
				SpriteEffects effects = npc.spriteDirection < 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
				if (npc.spriteDirection == -1)
				{
					spriteBatch.Draw(spearTexture, npc.Center - Main.screenPosition, new Rectangle(0, 0, spearTexture.Width, spearTexture.Height), drawColor, -MathHelper.PiOver2, new Vector2(17, 18), npc.scale, effects, 0); //was 24, 48
				}
				else
				{
					spriteBatch.Draw(spearTexture, npc.Center - Main.screenPosition, new Rectangle(0, 0, spearTexture.Width, spearTexture.Height), drawColor, MathHelper.PiOver2, new Vector2(-7, 18), npc.scale, effects, 0); //was -4, 48
				}
			}
		}
	}
}
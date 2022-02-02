using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.NPCs.Enemies
{
	class TibianAmazon : ModNPC
	{
		public override void SetStaticDefaults()
		{
			DisplayName.SetDefault("Tibian Amazon");
			Main.npcFrameCount[npc.type] = Main.npcFrameCount[NPCID.Skeleton];
		}

		public int throwingKnifeDamage = 8;

		public override void SetDefaults()
		{

			

			animationType = NPCID.Skeleton;
			npc.aiStyle = -1;
			npc.height = 40;
			npc.width = 20;
			npc.lifeMax = 85;
			npc.damage = 20;
			npc.scale = 1f;
			npc.HitSound = SoundID.NPCHit1;
			npc.DeathSound = SoundID.NPCDeath1;
			npc.knockBackResist = .6f;
			npc.value = 250;
			npc.defense = 2;
			banner = npc.type;
			bannerItem = ModContent.ItemType<Banners.TibianAmazonBanner>();

			if (Main.hardMode)
			{
				npc.lifeMax = 180;
				npc.defense = 16;
				npc.value = 320;
				npc.damage = 50;
				throwingKnifeDamage = 20;
			}
		}

		public override void NPCLoot()
		{
			Item.NewItem(npc.getRect(), ItemID.Torch);
			Item.NewItem(npc.getRect(), ItemID.ThrowingKnife, Main.rand.Next(20, 50));
			if (!Main.hardMode && Main.rand.Next(10) == 0) Item.NewItem(npc.getRect(), ModContent.ItemType<Items.Armors.RedMageTunic>());
			if (!Main.hardMode && Main.rand.Next(10) == 0) Item.NewItem(npc.getRect(), ModContent.ItemType<Items.Armors.RedMagePants>());
			if (!Main.hardMode && Main.rand.Next(10) == 0) Item.NewItem(npc.getRect(), ModContent.ItemType<Items.Armors.RedMageHat>());
			if (!Main.hardMode && Main.rand.Next(10) == 0) Item.NewItem(npc.getRect(), ModContent.ItemType<Items.Weapons.Melee.OldDoubleAxe>(), 1, false, -1);
			if (Main.rand.Next(20) == 0) Item.NewItem(npc.getRect(), ModContent.ItemType<Items.DeadChicken>());
		}




		//Spawns on the Surface and into the Underground. Does not spawn in the Dungeon, Hardmode, Meteor, or if there are Town NPCs.

		#region Spawn

		public override float SpawnChance(NPCSpawnInfo spawnInfo)
		{
			float chance = 0;

			if (spawnInfo.invasion)
			{
				chance = 0;
				return chance;
			}

			if (spawnInfo.player.townNPCs > 0f || tsorcRevampWorld.SuperHardMode || spawnInfo.player.ZoneDungeon) chance = 0f;
			if (!tsorcRevampWorld.SuperHardMode && (spawnInfo.player.ZoneOverworldHeight || spawnInfo.player.ZoneDirtLayerHeight || spawnInfo.player.ZoneRockLayerHeight))
			{
				if (!(spawnInfo.player.ZoneCorrupt || spawnInfo.player.ZoneCrimson)) return 0.05f;
				if (!(spawnInfo.player.ZoneCorrupt || spawnInfo.player.ZoneCrimson) && !Main.dayTime) return 0.055f;
				if (!(spawnInfo.player.ZoneCorrupt || spawnInfo.player.ZoneCrimson) && Main.dayTime) return 0.0534f;
				if (spawnInfo.player.ZoneMeteor && !Main.dayTime) return 0.0725f;
			}

			return chance;
		}
		#endregion

		float knifeTimer = 0;
		public override void AI()
		{
			tsorcRevampAIs.FighterAI(npc, 1.8f, 0.15f, enragePercent: 0.2f, enrageTopSpeed: 2.2f);
			tsorcRevampAIs.SimpleProjectile(npc, ref knifeTimer, 90, ModContent.ProjectileType<Projectiles.Enemy.EnemyThrowingKnife>(), throwingKnifeDamage, 8, soundType: 2, soundStyle: 17);

			if (npc.justHit)
			{
				knifeTimer = 60;
			}
		}

		public override void PostDraw(SpriteBatch spriteBatch, Color drawColor)
		{
			if (knifeTimer >= 60)
			{
				Texture2D knifeTexture = mod.GetTexture("NPCs/Enemies/TibianAmazon_Knife");
				SpriteEffects effects = npc.spriteDirection < 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
				if (npc.spriteDirection == -1)
				{
					spriteBatch.Draw(knifeTexture, npc.Center - Main.screenPosition, new Rectangle(npc.frame.X, npc.frame.Y, 60, 56), drawColor, npc.rotation, new Vector2(30, 32), npc.scale, effects, 0);
				}
				else
				{
					spriteBatch.Draw(knifeTexture, npc.Center - Main.screenPosition, new Rectangle(npc.frame.X, npc.frame.Y, 60, 56), drawColor, npc.rotation, new Vector2(30, 32), npc.scale, effects, 0);
				}
			}
		}

		#region Gore
		public override void HitEffect(int hitDirection, double damage)
		{
			for (int i = 0; i < 25; i++)
			{
				int dustType = 5;
				int dustIndex = Dust.NewDust(npc.position, npc.width, npc.height, dustType);
				Dust dust = Main.dust[dustIndex];
				dust.velocity.X = dust.velocity.X + Main.rand.Next(-50, 51) * 0.06f;
				dust.velocity.Y = dust.velocity.Y + Main.rand.Next(-50, 51) * 0.06f;
				dust.scale *= 1f + Main.rand.Next(-30, 31) * 0.01f;
				dust.noGravity = true;
			}
			if (npc.life <= 0)
			{
				for (int i = 0; i < 4; i++)
				{
					Dust.NewDust(npc.position, npc.width, npc.height, 5, Main.rand.Next(-3, 3), Main.rand.Next(-3, 3), 70, default(Color), 1f);
				}

				Gore.NewGore(npc.position, new Vector2(Main.rand.Next(-30, 31) * 0.2f, Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Tibian Amazon Gore 1"), 1f);
				Gore.NewGore(npc.position, new Vector2(Main.rand.Next(-30, 31) * 0.2f, Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Tibian Amazon Gore 2"), 1f);
				Gore.NewGore(npc.position, new Vector2(Main.rand.Next(-30, 31) * 0.2f, Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Tibian Amazon Gore 3"), 1f);
				Gore.NewGore(npc.position, new Vector2(Main.rand.Next(-30, 31) * 0.2f, Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Tibian Amazon Gore 2"), 1f);
				Gore.NewGore(npc.position, new Vector2(Main.rand.Next(-30, 31) * 0.2f, Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Tibian Amazon Gore 3"), 1f);
			}
		}
		#endregion
	}
}
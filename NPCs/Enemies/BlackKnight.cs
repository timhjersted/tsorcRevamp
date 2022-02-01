using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace tsorcRevamp.NPCs.Enemies
{
	class BlackKnight : ModNPC
	{
		public override void SetDefaults()
		{
			Main.npcFrameCount[npc.type] = 16;
			animationType = 28;
			npc.aiStyle = -1;
			//npc.aiStyle = 3;
			npc.height = 40;
			npc.width = 20;
			npc.damage = 95;
			npc.defense = 21;
			npc.lifeMax = 900;
			if (Main.hardMode) { npc.lifeMax = 1400; npc.defense = 60; }
			if (tsorcRevampWorld.SuperHardMode) { npc.lifeMax = 3000; npc.defense = 75; npc.damage = 120; npc.value = 6600; }
			npc.HitSound = SoundID.NPCHit1;
			npc.DeathSound = SoundID.NPCDeath1;
			npc.lavaImmune = true;
			npc.value = 5000;
			npc.knockBackResist = 0.15f;
			banner = npc.type;
			bannerItem = ModContent.ItemType<Banners.BlackKnightBanner>();
		}

		public override void ScaleExpertStats(int numPlayers, float bossLifeScale)
		{
			npc.lifeMax = (int)(npc.lifeMax / 2);
			npc.damage = (int)(npc.damage / 2);
			spearDamage = (int)(spearDamage / 2);
		}

		public int spearDamage = 50;

		#region Spawn
		public override float SpawnChance(NPCSpawnInfo spawnInfo)
		{
			if (spawnInfo.player.townNPCs > 1f) return 0f;
			if (!spawnInfo.player.ZoneMeteor && !spawnInfo.player.ZoneDungeon && !(spawnInfo.player.ZoneCorrupt || spawnInfo.player.ZoneCrimson) && spawnInfo.player.ZoneOverworldHeight && NPC.downedBoss3 && !Main.dayTime && Main.rand.Next(100) == 1) return 1;
			if (!Main.hardMode && spawnInfo.player.ZoneMeteor && NPC.downedBoss2 && Main.rand.Next(100) == 1) return 1;
			if (Main.hardMode && spawnInfo.player.ZoneDungeon && Main.rand.Next(100) == 1) return 1;
			if (Main.hardMode && !(spawnInfo.player.ZoneCorrupt || spawnInfo.player.ZoneCrimson) && !spawnInfo.player.ZoneBeach && !Main.dayTime && Main.rand.Next(200) == 1) return 1;
			if (Main.hardMode && spawnInfo.player.ZoneUnderworldHeight && !Main.dayTime && Main.rand.Next(60) == 1) return 1;
			if (tsorcRevampWorld.SuperHardMode && spawnInfo.player.ZoneDungeon && Main.rand.Next(50) == 1) return 1;

			return 0;
		}
		#endregion

		public override void AI()
		{
			tsorcRevampAIs.FighterAI(npc, 2.6f, 0.05f, enragePercent: 0.3f, enrageTopSpeed: 3.4f);
			bool inRange = npc.Distance(Main.player[npc.target].Center) < 300 && Collision.CanHitLine(npc.Center, 0, 0, Main.player[npc.target].Center, 0, 0);
			tsorcRevampAIs.SimpleProjectile(npc, ref npc.ai[2], 180, ModContent.ProjectileType<Projectiles.Enemy.BlackKnightSpear>(), spearDamage, 9, inRange, true, 2, 17);

			if(npc.ai[2] >= 150f && npc.justHit)
			{
				npc.ai[2] = 100f; // reset throw countdown when hit, was 150
			}
		}

		

		static Texture2D spearTexture;
		public override void PostDraw(SpriteBatch spriteBatch, Color drawColor)
		{
			if (spearTexture == null || spearTexture.IsDisposed)
			{
				//spearTexture = mod.GetTexture("Projectiles/Enemy/BlackKnightsSpear");
				spearTexture = ModContent.GetTexture("Terraria/Projectile_508");
			}
			if (npc.ai[2] >= 165)
			{
				SpriteEffects effects = npc.spriteDirection < 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
				if (npc.spriteDirection == -1)
				{
					spriteBatch.Draw(spearTexture, npc.Center - Main.screenPosition, new Rectangle(0, 0, spearTexture.Width, spearTexture.Height), drawColor, -MathHelper.PiOver2, new Vector2(8, 38), npc.scale, effects, 0); // facing left (8, 38 work)
				}
				else
				{
					spriteBatch.Draw(spearTexture, npc.Center - Main.screenPosition, new Rectangle(0, 0, spearTexture.Width, spearTexture.Height), drawColor, MathHelper.PiOver2, new Vector2(8, 38), npc.scale, effects, 0); // facing right, first value is height, higher number is higher
				}
			}
		}


		#region Gore
		public override void NPCLoot()
		{
			Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Black Knight Gore 1"), 1f);
			Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Black Knight Gore 2"), 1f);
			Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Black Knight Gore 3"), 1f);
			Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Black Knight Gore 2"), 1f);
			Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Black Knight Gore 3"), 1f);

			if (Main.rand.Next(99) < 30) Item.NewItem(npc.getRect(), ModContent.ItemType<Items.Weapons.Ranged.ThrowingSpear>(), 1 + Main.rand.Next(50));
			if (Main.rand.Next(99) < 30) Item.NewItem(npc.getRect(), ModContent.ItemType<Items.Weapons.Ranged.RoyalThrowingSpear>(), 1 + Main.rand.Next(50));
			if (Main.rand.Next(99) < 10) Item.NewItem(npc.getRect(), ModContent.ItemType<Items.Accessories.BootsOfHaste>(), 1);
			if (Main.rand.Next(99) < 5) Item.NewItem(npc.getRect(), ModContent.ItemType<Items.Weapons.Melee.AncientDragonLance>(), 1);
			if (Main.rand.Next(99) < 20) Item.NewItem(npc.getRect(), ModContent.ItemType<Items.Weapons.Melee.OldHalberd>(), 1);
			if (Main.rand.Next(99) < 40) Item.NewItem(npc.getRect(), ItemID.IronskinPotion, 1);
			Item.NewItem(npc.getRect(), ItemID.GreaterHealingPotion, 1);
			if (Main.rand.Next(99) < 40) Item.NewItem(npc.getRect(), ItemID.ArcheryPotion, 1);
			if (Main.rand.Next(99) < 40) Item.NewItem(npc.getRect(), ItemID.RegenerationPotion, 1);
		}
		#endregion
	}
}
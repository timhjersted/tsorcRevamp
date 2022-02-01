using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.NPCs.Enemies
{
	public class DworcVenomsniper : ModNPC
	{
		public override void SetDefaults()
		{
			npc.HitSound = SoundID.NPCHit26;
			npc.DeathSound = SoundID.NPCDeath29;
			npc.damage = 26;
			npc.lifeMax = 35;
			npc.defense = 8;
			npc.value = 370;
			npc.width = 18;
			npc.aiStyle = -1;
			npc.height = 40;
			npc.knockBackResist = 0.1f;
			banner = npc.type;
			bannerItem = ModContent.ItemType<Banners.DworcVenomsniperBanner>();

			animationType = NPCID.Skeleton;
			Main.npcFrameCount[npc.type] = 15;
		}

		public override void NPCLoot()
		{
			if (Main.rand.Next(50) == 0) Item.NewItem(npc.getRect(), ModContent.ItemType<Items.Potions.CrimsonPotion>());
			if (Main.rand.Next(10) == 0) Item.NewItem(npc.getRect(), ItemID.GreaterHealingPotion);
			if (Main.rand.Next(10) == 0) Item.NewItem(npc.getRect(), ItemID.GreaterHealingPotion);
			if (Main.rand.Next(8) == 0) Item.NewItem(npc.getRect(), ItemID.ManaRegenerationPotion);
			if (Main.rand.Next(50) == 0) Item.NewItem(npc.getRect(), ModContent.ItemType<Items.Potions.StrengthPotion>());
			if (Main.rand.Next(10) == 0) Item.NewItem(npc.getRect(), ModContent.ItemType<Items.CharcoalPineResin>());
			if (Main.rand.Next(50) == 0) Item.NewItem(npc.getRect(), ModContent.ItemType<Items.Potions.ShockwavePotion>());
			if (Main.rand.Next(50) == 0) Item.NewItem(npc.getRect(), ModContent.ItemType<Items.Potions.AttractionPotion>());
			if (Main.rand.Next(10) == 0) Item.NewItem(npc.getRect(), ItemID.IronskinPotion);
			if (Main.rand.Next(10) == 0) Item.NewItem(npc.getRect(), ItemID.ArcheryPotion);
			if (Main.rand.Next(100) == 0) Item.NewItem(npc.getRect(), ItemID.GillsPotion);
			if (Main.rand.Next(100) == 0) Item.NewItem(npc.getRect(), ItemID.HunterPotion);
			if (Main.rand.Next(50) == 0) Item.NewItem(npc.getRect(), ItemID.MagicPowerPotion);
			if (Main.rand.Next(50) == 0) Item.NewItem(npc.getRect(), ItemID.RegenerationPotion);
			if (Main.rand.Next(10) == 0) Item.NewItem(npc.getRect(), ItemID.ShinePotion);
			if (Main.rand.Next(20) == 0) Item.NewItem(npc.getRect(), ItemID.SpelunkerPotion);
			if (Main.rand.Next(20) == 0) Item.NewItem(npc.getRect(), ItemID.SwiftnessPotion);
			if (Main.rand.Next(20) == 0) Item.NewItem(npc.getRect(), ItemID.WaterWalkingPotion);
			if (Main.rand.Next(20) == 0) Item.NewItem(npc.getRect(), ItemID.BattlePotion);
		}

		//Spawns in the Jungle, mostly Underground and in the Cavern.

		#region Spawn
		public override float SpawnChance(NPCSpawnInfo spawnInfo)
		{
			float chance = 0f;

			if (spawnInfo.player.ZoneDungeon)
			{
				return 0f;
			}
			else if (!Main.hardMode && spawnInfo.player.ZoneJungle && spawnInfo.player.ZoneOverworldHeight)
			{
				return 0.1f;
			}
			else if (!Main.hardMode && spawnInfo.player.ZoneJungle && (spawnInfo.player.ZoneDirtLayerHeight || spawnInfo.player.ZoneRockLayerHeight))
			{
				return 0.145f;
			}

			return chance;
		}

		#endregion

		public override void AI()
		{
			tsorcRevampAIs.FighterAI(npc, 1.2f, 0.05f);

			bool readyToFire = false;
			if (npc.Distance(Main.player[npc.target].Center) < 250 && Collision.CanHit(npc.position, npc.width, npc.height, Main.player[npc.target].position, Main.player[npc.target].width, Main.player[npc.target].height))
			{
				readyToFire = true;
			}
			tsorcRevampAIs.SimpleProjectile(npc, ref npc.ai[1], 180, ModContent.ProjectileType<Projectiles.Enemy.ArcherBolt>(), 9, 8, readyToFire, true, 2, 65);
			
		}

		public override void PostDraw(SpriteBatch spriteBatch, Color drawColor)
		{
			if (npc.ai[1] >= 140)
			{
				Texture2D blowpipeTexture = mod.GetTexture("NPCs/Enemies/DworcVenomsniper_Telegraph");
				SpriteEffects effects = npc.spriteDirection < 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
				if (npc.spriteDirection == -1)
				{
					spriteBatch.Draw(blowpipeTexture, npc.Center - Main.screenPosition, new Rectangle(npc.frame.X, npc.frame.Y, 44, 56), drawColor, npc.rotation, new Vector2(22, 32), npc.scale, effects, 0);
				}
				else
				{
					spriteBatch.Draw(blowpipeTexture, npc.Center - Main.screenPosition, new Rectangle(npc.frame.X, npc.frame.Y, 44, 56), drawColor, npc.rotation, new Vector2(22, 32), npc.scale, effects, 0);
				}
			}
		}

		#region Gore
		public override void HitEffect(int hitDirection, double damage)
		{
			for (int i = 0; i < 5; i++)
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
				for (int i = 0; i < 25; i++)
				{
					Dust.NewDust(npc.position, npc.width, npc.height, 5, Main.rand.Next(-3, 3), Main.rand.Next(-3, 3), 70, default(Color), 1f);
				}

				Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Dworc Gore 1"), 1f);
				Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Dworc Gore 2"), 1f);
				Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Dworc Gore 3"), 1f);
				Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Dworc Gore 2"), 1f);
				Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Dworc Gore 3"), 1f);
			}
		}
		#endregion
	}
}

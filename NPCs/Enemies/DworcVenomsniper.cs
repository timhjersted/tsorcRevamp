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
			NPC.HitSound = SoundID.NPCHit29; //spider
			NPC.DeathSound = SoundID.NPCDeath29;//lizard
			NPC.damage = 26;
			NPC.lifeMax = 35;
			NPC.defense = 8;
			NPC.value = 370;
			NPC.width = 18;
			NPC.aiStyle = -1;
			NPC.height = 40;
			NPC.knockBackResist = 0.1f;
			banner = NPC.type;
			bannerItem = ModContent.ItemType<Banners.DworcVenomsniperBanner>();

			animationType = NPCID.Skeleton;
			Main.npcFrameCount[NPC.type] = 15;
		}

		public override void OnKill()
		{
			if (Main.rand.Next(50) == 0) Item.NewItem(NPC.getRect(), ModContent.ItemType<Items.Potions.CrimsonPotion>());
			if (Main.rand.Next(10) == 0) Item.NewItem(NPC.getRect(), ItemID.GreaterHealingPotion);
			if (Main.rand.Next(10) == 0) Item.NewItem(NPC.getRect(), ItemID.GreaterHealingPotion);
			if (Main.rand.Next(8) == 0) Item.NewItem(NPC.getRect(), ItemID.ManaRegenerationPotion);
			if (Main.rand.Next(50) == 0) Item.NewItem(NPC.getRect(), ModContent.ItemType<Items.Potions.StrengthPotion>());
			if (Main.rand.Next(10) == 0) Item.NewItem(NPC.getRect(), ModContent.ItemType<Items.CharcoalPineResin>());
			if (Main.rand.Next(50) == 0) Item.NewItem(NPC.getRect(), ModContent.ItemType<Items.Potions.ShockwavePotion>());
			if (Main.rand.Next(50) == 0) Item.NewItem(NPC.getRect(), ModContent.ItemType<Items.Potions.AttractionPotion>());
			if (Main.rand.Next(10) == 0) Item.NewItem(NPC.getRect(), ItemID.IronskinPotion);
			if (Main.rand.Next(10) == 0) Item.NewItem(NPC.getRect(), ItemID.ArcheryPotion);
			if (Main.rand.Next(100) == 0) Item.NewItem(NPC.getRect(), ItemID.GillsPotion);
			if (Main.rand.Next(100) == 0) Item.NewItem(NPC.getRect(), ItemID.HunterPotion);
			if (Main.rand.Next(50) == 0) Item.NewItem(NPC.getRect(), ItemID.MagicPowerPotion);
			if (Main.rand.Next(50) == 0) Item.NewItem(NPC.getRect(), ItemID.RegenerationPotion);
			if (Main.rand.Next(10) == 0) Item.NewItem(NPC.getRect(), ItemID.ShinePotion);
			if (Main.rand.Next(20) == 0) Item.NewItem(NPC.getRect(), ItemID.SpelunkerPotion);
			if (Main.rand.Next(20) == 0) Item.NewItem(NPC.getRect(), ItemID.SwiftnessPotion);
			if (Main.rand.Next(20) == 0) Item.NewItem(NPC.getRect(), ItemID.WaterWalkingPotion);
			if (Main.rand.Next(20) == 0) Item.NewItem(NPC.getRect(), ItemID.BattlePotion);
		}

		//Spawns in the Jungle, mostly Underground and in the Cavern.

		#region Spawn
		public override float SpawnChance(NPCSpawnInfo spawnInfo)
		{
			float chance = 0f;

			if (spawnInfo.Player.ZoneDungeon)
			{
				return 0f;
			}
			else if (!Main.hardMode && spawnInfo.Player.ZoneJungle && spawnInfo.Player.ZoneOverworldHeight)
			{
				return 0.1f;
			}
			else if (!Main.hardMode && spawnInfo.Player.ZoneJungle && (spawnInfo.Player.ZoneDirtLayerHeight || spawnInfo.Player.ZoneRockLayerHeight))
			{
				return 0.145f;
			}

			return chance;
		}

		#endregion

		public override void AI()
		{
			tsorcRevampAIs.FighterAI(NPC, 1.2f, 0.05f);

			bool readyToFire = false;
			if (NPC.Distance(Main.player[NPC.target].Center) < 250 && Collision.CanHit(NPC.position, NPC.width, NPC.height, Main.player[NPC.target].position, Main.player[NPC.target].width, Main.player[NPC.target].height))
			{
				readyToFire = true;
			}
			tsorcRevampAIs.SimpleProjectile(NPC, ref NPC.ai[1], 180, ModContent.ProjectileType<Projectiles.Enemy.ArcherBolt>(), 9, 8, readyToFire, true, 2, 63); //blowpipe
																																								 //Main.PlaySound(2, (int)npc.position.X, (int)npc.position.Y, 20, 0.2f, 0.3f); //fire

			//TELEGRAPH DUSTS
			if (NPC.ai[1] >= 150 && NPC.ai[1] <= 170)
			{
				Lighting.AddLight(NPC.Center, Color.Purple.ToVector3() * 0.5f); //Pick a color, any color. The 0.5f tones down its intensity by 50%
				if (Main.rand.Next(2) == 1)
				{
					Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Smoke, NPC.velocity.X, NPC.velocity.Y);
					//Dust.NewDust(npc.position, npc.width, npc.height, DustID.EmeraldBolt, npc.velocity.X, npc.velocity.Y);
				}
			}
		}

		public override void PostDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
		{
			if (NPC.ai[1] >= 140)
			{
				Texture2D blowpipeTexture = Mod.GetTexture("NPCs/Enemies/DworcVenomsniper_Telegraph");
				SpriteEffects effects = NPC.spriteDirection < 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
				if (NPC.spriteDirection == -1)
				{
					spriteBatch.Draw(blowpipeTexture, NPC.Center - Main.screenPosition, new Rectangle(NPC.frame.X, NPC.frame.Y, 44, 56), drawColor, NPC.rotation, new Vector2(22, 32), NPC.scale, effects, 0);
				}
				else
				{
					spriteBatch.Draw(blowpipeTexture, NPC.Center - Main.screenPosition, new Rectangle(NPC.frame.X, NPC.frame.Y, 44, 56), drawColor, NPC.rotation, new Vector2(22, 32), NPC.scale, effects, 0);
				}
			}
		}

		#region Gore
		public override void HitEffect(int hitDirection, double damage)
		{
			for (int i = 0; i < 5; i++)
			{
				int dustType = 5;
				int dustIndex = Dust.NewDust(NPC.position, NPC.width, NPC.height, dustType);
				Dust dust = Main.dust[dustIndex];
				dust.velocity.X = dust.velocity.X + Main.rand.Next(-50, 51) * 0.06f;
				dust.velocity.Y = dust.velocity.Y + Main.rand.Next(-50, 51) * 0.06f;
				dust.scale *= 1f + Main.rand.Next(-30, 31) * 0.01f;
				dust.noGravity = true;
			}
			if (NPC.life <= 0)
			{
				for (int i = 0; i < 25; i++)
				{
					Dust.NewDust(NPC.position, NPC.width, NPC.height, 5, Main.rand.Next(-3, 3), Main.rand.Next(-3, 3), 70, default(Color), 1f);
				}

				Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.GetGoreSlot("Gores/Dworc Gore 1"), 1f);
				Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.GetGoreSlot("Gores/Dworc Gore 2"), 1f);
				Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.GetGoreSlot("Gores/Dworc Gore 3"), 1f);
				Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.GetGoreSlot("Gores/Dworc Gore 2"), 1f);
				Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.GetGoreSlot("Gores/Dworc Gore 3"), 1f);
			}
		}
		#endregion
	}
}

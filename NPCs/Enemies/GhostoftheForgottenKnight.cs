using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System;
using Microsoft.Xna.Framework.Graphics;

namespace tsorcRevamp.NPCs.Enemies
{
	class GhostoftheForgottenKnight : ModNPC
	{
		public override void SetStaticDefaults()
		{
			DisplayName.SetDefault("Ghost of the Forgotten Knight");

		}

		public int spearDamage = 30;

		public override void SetDefaults()
		{
			npc.npcSlots = 3;
			Main.npcFrameCount[npc.type] = 16;
			animationType = 28;
			npc.aiStyle = -1;
			npc.height = 40;
			npc.width = 20;
			npc.damage = 60;
			npc.defense = 22;
			npc.lifeMax = 300;
			npc.HitSound = SoundID.NPCHit1;
			npc.DeathSound = SoundID.NPCDeath1;
			npc.lavaImmune = true;
			npc.value = 450;
			npc.knockBackResist = 0.1f;
			banner = npc.type;
			bannerItem = ModContent.ItemType<Banners.GhostOfTheForgottenKnightBanner>();

			if (Main.hardMode)
			{
				npc.lifeMax = 400;
				npc.defense = 32;
				npc.value = 650;
				npc.damage = 80;
				spearDamage = 50;
			}

			if (tsorcRevampWorld.SuperHardMode) 
			{ 
				npc.lifeMax = 1800; 
				npc.defense = 70; 
				npc.damage = 100; 
				npc.value = 1000;
				spearDamage = 90;
			}
		}

		public override void ScaleExpertStats(int numPlayers, float bossLifeScale)
		{
			npc.lifeMax = (int)(npc.lifeMax / 2);
			npc.damage = (int)(npc.damage / 2);
			spearDamage = (int)(spearDamage / 2);
		}

		

		public override float SpawnChance(NPCSpawnInfo spawnInfo)
		{

			if (!Main.hardMode && NPC.downedBoss3 && spawnInfo.player.ZoneDungeon)
			{
				return 0.2f; //.16 should be 8%
			}
			if (Main.hardMode && spawnInfo.player.ZoneDungeon)
			{
				return 0.17f;
			}
			if (tsorcRevampWorld.SuperHardMode && spawnInfo.player.ZoneDungeon)
			{
				return 0.08f; //.08% is 4.28%
			}

			return 0;
		}

		public override void OnHitPlayer(Player player, int damage, bool crit)
		{
			if (Main.rand.Next(2) == 0)
			{
				player.AddBuff(33, 3600, false); //weak
			}
		}

		float spearTimer = 0;
		public override void AI()
		{
			tsorcRevampAIs.FighterAI(npc, 1.8f, .05f, 0.2f, true, enragePercent: 0.2f, enrageTopSpeed: 2.4f);

			bool canFire = npc.Distance(Main.player[npc.target].Center) < 1600 && Collision.CanHitLine(npc.Center, 0, 0, Main.player[npc.target].Center, 0, 0);
			tsorcRevampAIs.SimpleProjectile(npc, ref spearTimer, 180, ModContent.ProjectileType<Projectiles.Enemy.BlackKnightSpear>(), 20, 8, canFire, true, 2, 17);

			if (npc.justHit)
			{
				spearTimer = 0f;
			}
		}

        #region Draw Spear Texture
        static Texture2D spearTexture;
		public override void PostDraw(SpriteBatch spriteBatch, Color drawColor)
		{
			if (spearTexture == null)
			{
				spearTexture = mod.GetTexture("Projectiles/Enemy/BlackKnightGhostSpear");
			}
			if (spearTimer >= 150)
			{
				Lighting.AddLight(npc.Center, Color.White.ToVector3() * 0.3f); //Pick a color, any color. The 0.5f tones down its intensity by 50%
				if (Main.rand.Next(3) == 1)
				{
					Dust.NewDust(npc.position, npc.width, npc.height, DustID.Smoke, npc.velocity.X, npc.velocity.Y);
				}
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
        #endregion

        #region Gore
        public override void NPCLoot()
		{
			Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Black Knight Gore 1"), 1f);
			Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Black Knight Gore 2"), 1f);
			Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Black Knight Gore 3"), 1f);
			Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Black Knight Gore 2"), 1f);
			Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Black Knight Gore 3"), 1f);

			if (Main.rand.Next(99) < 25) Item.NewItem(npc.getRect(), ModContent.ItemType<Items.Weapons.Ranged.RoyalThrowingSpear>(), 26 + Main.rand.Next(10));
			if (Main.rand.Next(99) < 40) Item.NewItem(npc.getRect(), ModContent.ItemType<Items.Weapons.Ranged.EphemeralThrowingSpear>(), 26 + Main.rand.Next(10));
			if (Main.rand.Next(99) < 10) Item.NewItem(npc.getRect(), ModContent.ItemType<Items.Potions.HealingElixir>(), 1);
			Item.NewItem(npc.getRect(), ModContent.ItemType<Items.EphemeralDust>(), 3 + Main.rand.Next(6));
			if (Main.rand.Next(99) < 4) Item.NewItem(npc.getRect(), ItemID.IronskinPotion, 1);
			if (Main.rand.Next(99) < 4) Item.NewItem(npc.getRect(), ItemID.GreaterHealingPotion, 1);
			if (Main.rand.Next(99) < 8) Item.NewItem(npc.getRect(), ItemID.HunterPotion, 1);
			if (Main.rand.Next(99) < 6) Item.NewItem(npc.getRect(), ItemID.RegenerationPotion, 1);
			if (Main.rand.Next(99) < 30) Item.NewItem(npc.getRect(), ItemID.ShinePotion, 1);
			if (Main.rand.Next(99) < 5) Item.NewItem(npc.getRect(), ItemID.BattlePotion, 1);
			if (Main.rand.Next(10) == 0) Item.NewItem(npc.getRect(), ItemID.GoldenKey, 1);
		}
		#endregion
	}
}
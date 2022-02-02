using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.NPCs.Enemies
{
    class TibianValkyrie : ModNPC
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Tibian Valkyrie");
            Main.npcFrameCount[npc.type] = Main.npcFrameCount[NPCID.Skeleton];
        }

        public override void SetDefaults()
        {
            animationType = NPCID.Skeleton;
            npc.aiStyle = -1;
            npc.height = 40;
            npc.width = 20;
            npc.lifeMax = 90;
            npc.damage = 28;
            npc.scale = 1f;
			npc.HitSound = SoundID.NPCHit1;
			npc.DeathSound = SoundID.NPCDeath1;
            npc.knockBackResist = .5f;
            npc.value = 250;
            npc.defense = 4;
			banner = npc.type;
			bannerItem = ModContent.ItemType<Banners.TibianValkyrieBanner>();
		}

        public override void NPCLoot()
        {
            Item.NewItem(npc.getRect(), ItemID.Torch);
            Item.NewItem(npc.getRect(), ModContent.ItemType<Items.Weapons.Ranged.ThrowingSpear>(), Main.rand.Next(20, 76));
            if (Main.rand.Next(10) == 0) Item.NewItem(npc.getRect(), ModContent.ItemType<Items.Accessories.IronShield>(), 1, false, -1);
            if (Main.rand.Next(10) == 0) Item.NewItem(npc.getRect(), ModContent.ItemType<Items.Weapons.Melee.OldHalberd>(), 1, false, -1);
            if (Main.rand.Next(20) == 0) Item.NewItem(npc.getRect(), ModContent.ItemType<Items.Weapons.Melee.OldDoubleAxe>(), 1, false, -1);
            if (Main.rand.Next(2) == 0) Item.NewItem(npc.getRect(), ItemID.Diamond);
            if (Main.rand.Next(20) == 0) Item.NewItem(npc.getRect(), ModContent.ItemType<Items.DeadChicken>());
        }


		int drownTimerMax = 2000;
		int drownTimer = 2000;
		int drowningRisk = 1200;


		#region Spawn

		public override float SpawnChance(NPCSpawnInfo spawnInfo)
		{
			float chance = 0;

			if (spawnInfo.invasion)
			{
				chance = 0;
				return chance;
			}

			if (spawnInfo.player.townNPCs > 0f) return 0f;
			if (spawnInfo.player.ZoneOverworldHeight && !Main.hardMode && !Main.dayTime) return 0.0427f;
			if (spawnInfo.player.ZoneOverworldHeight && !Main.hardMode && Main.dayTime) return 0.038f;

			if (!Main.hardMode && !spawnInfo.player.ZoneMeteor && !spawnInfo.player.ZoneJungle)
			{
				if (!spawnInfo.player.ZoneDungeon && !spawnInfo.player.ZoneCorrupt && !spawnInfo.player.ZoneCrimson && (spawnInfo.player.ZoneDirtLayerHeight || spawnInfo.player.ZoneRockLayerHeight) && Main.dayTime) return 0.0433f;
				if (!spawnInfo.player.ZoneDungeon && !spawnInfo.player.ZoneCorrupt && !spawnInfo.player.ZoneCrimson && (spawnInfo.player.ZoneDirtLayerHeight || spawnInfo.player.ZoneRockLayerHeight) && !Main.dayTime) return 0.0555f;
				if (!spawnInfo.player.ZoneDungeon && (spawnInfo.player.ZoneCorrupt || spawnInfo.player.ZoneCrimson) && (spawnInfo.player.ZoneDirtLayerHeight || spawnInfo.player.ZoneRockLayerHeight)) return 0.0655f;
				if (spawnInfo.player.ZoneDungeon && (spawnInfo.player.ZoneDirtLayerHeight || spawnInfo.player.ZoneRockLayerHeight)) return 0.03857f;
			}
			return chance;
		}
		#endregion

		float spearTimer = 0;
		public override void AI()
		{
			tsorcRevampAIs.FighterAI(npc, 1.65f, 0.11f, enragePercent: 0.5f, enrageTopSpeed: 2.4f);
			tsorcRevampAIs.SimpleProjectile(npc, ref spearTimer, 180, ModContent.ProjectileType<Projectiles.Enemy.BlackKnightSpear>(), 10, 8, Collision.CanHitLine(npc.Center, 0, 0, Main.player[npc.target].Center, 0, 0), soundType: 2, soundStyle: 17);

			if (npc.justHit)
			{
				spearTimer = 150;
			}			
		}

		public override void PostDraw(SpriteBatch spriteBatch, Color drawColor)
		{
			if (spearTimer >= 150)
			{
				Texture2D spearTexture = mod.GetTexture("NPCs/Enemies/TibianValkyrie_Spear");
				SpriteEffects effects = npc.spriteDirection < 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
				if (npc.spriteDirection == -1)
				{
					spriteBatch.Draw(spearTexture, npc.Center - Main.screenPosition, new Rectangle(npc.frame.X, npc.frame.Y, 76, 58), drawColor, npc.rotation, new Vector2(38, 34), npc.scale, effects, 0);
				}
				else
				{
					spriteBatch.Draw(spearTexture, npc.Center - Main.screenPosition, new Rectangle(npc.frame.X, npc.frame.Y, 76, 58), drawColor, npc.rotation, new Vector2(38, 34), npc.scale, effects, 0);
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

				Gore.NewGore(npc.position, new Vector2(Main.rand.Next(-30, 31) * 0.2f, Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Tibian Valkyrie Gore 1"), 1f);
				Gore.NewGore(npc.position, new Vector2(Main.rand.Next(-30, 31) * 0.2f, Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Tibian Valkyrie Gore 2"), 1f);
				Gore.NewGore(npc.position, new Vector2(Main.rand.Next(-30, 31) * 0.2f, Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Tibian Valkyrie Gore 3"), 1f);
				Gore.NewGore(npc.position, new Vector2(Main.rand.Next(-30, 31) * 0.2f, Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Tibian Valkyrie Gore 2"), 1f);
				Gore.NewGore(npc.position, new Vector2(Main.rand.Next(-30, 31) * 0.2f, Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Tibian Valkyrie Gore 3"), 1f);
			}
		}
		#endregion
	}
}
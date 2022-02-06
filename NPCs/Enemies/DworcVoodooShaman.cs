using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.Default;
using tsorcRevamp.Items.Armors;

namespace tsorcRevamp.NPCs.Enemies
{
	public class DworcVoodooShaman : ModNPC
	{
		public override void SetDefaults()
		{
			npc.HitSound = SoundID.NPCHit26;
			npc.DeathSound = SoundID.NPCDeath29;
			npc.damage = 33;
			npc.lifeMax = 1260;
			npc.defense = 28;
			npc.value = 6000;
			npc.width = 18;
			npc.aiStyle = -1;
			npc.height = 40;
			npc.knockBackResist = 0.2f;
			npc.buffImmune[BuffID.Poisoned] = true;
			npc.buffImmune[BuffID.OnFire] = true;
			banner = npc.type;
			bannerItem = ModContent.ItemType<Banners.DworcVoodooShamanBanner>();

			animationType = NPCID.Skeleton;
			Main.npcFrameCount[npc.type] = 15;
		}

		public override void NPCLoot()
		{
			if (Main.rand.Next(16) == 0) Item.NewItem(npc.getRect(), ModContent.ItemType<Items.Accessories.BandOfCosmicPower>());
			if (Main.rand.NextFloat() >= .2f) Item.NewItem(npc.getRect(), ModContent.ItemType<Items.Potions.CrimsonPotion>());
			if (Main.rand.Next(16) == 0) Item.NewItem(npc.getRect(), ModContent.ItemType<Items.Potions.StrengthPotion>());
			if (Main.rand.Next(3) == 0) Item.NewItem(npc.getRect(), ItemID.FlaskofFire);
			if (Main.rand.Next(3) == 0) Item.NewItem(npc.getRect(), ModContent.ItemType<Items.Potions.ShockwavePotion>());
			if (Main.rand.Next(50) == 0) Item.NewItem(npc.getRect(), ModContent.ItemType<Items.Potions.BattlefrontPotion>());
			if (Main.rand.Next(5) == 0) Item.NewItem(npc.getRect(), ModContent.ItemType<Items.Potions.AttractionPotion>());
			if (Main.rand.Next(2) == 0) Item.NewItem(npc.getRect(), ItemID.IronskinPotion);
			Item.NewItem(npc.getRect(), ItemID.ManaRegenerationPotion, Main.rand.Next(1, 6));
			Item.NewItem(npc.getRect(), ItemID.MagicPowerPotion, Main.rand.Next(1, 4));
			Item.NewItem(npc.getRect(), ItemID.RegenerationPotion, Main.rand.Next(1, 5));
		}

		//Spawns in the Jungle and in the Cavern in HM.

		public override float SpawnChance(NPCSpawnInfo spawnInfo)
		{
			var player = spawnInfo.player;
			bool TropicalOcean = player.position.X < 3600;

			float chance = 0f;

			if (Main.hardMode && (spawnInfo.player.ZoneMeteor || spawnInfo.player.ZoneJungle) && !spawnInfo.player.ZoneDungeon && !spawnInfo.player.ZoneCorrupt && !spawnInfo.player.ZoneCrimson)
			{
				if (spawnInfo.player.ZoneOverworldHeight && Main.dayTime) return 0.02f;
				if (spawnInfo.player.ZoneOverworldHeight && !Main.dayTime) return 0.045f;
				if (spawnInfo.player.ZoneDirtLayerHeight) return 0.035f;
				if (spawnInfo.player.ZoneRockLayerHeight) return 0.035f;
			}
			if (Main.hardMode && TropicalOcean && spawnInfo.player.ZoneJungle) return 0.045f;

			return chance;
		}

		float poisonStrikeTimer = 0;
		float poisonStormTimer = 0;
		public override void AI()
		{
			tsorcRevampAIs.FighterAI(npc, 1.5f, 0.04f, canTeleport: true, enragePercent: 0.3f, enrageTopSpeed: 3);

			bool clearLineofSight = Collision.CanHit(npc.Center, 1, 1, Main.player[npc.target].Center, 1, 1);
			tsorcRevampAIs.SimpleProjectile(npc, ref poisonStrikeTimer, 120, ModContent.ProjectileType<Projectiles.Enemy.EnemySpellGreatPoisonStrikeBall>(), 18, 8, clearLineofSight, true, 2, 17, 0);
			tsorcRevampAIs.SimpleProjectile(npc, ref poisonStormTimer, 180, ModContent.ProjectileType<Projectiles.Enemy.EnemySpellPoisonStormBall>(), 25, 0, clearLineofSight, true, 2, 17);
			if (poisonStrikeTimer >= 60)
			{
				Dust.NewDust(npc.position, npc.width, npc.height, DustID.CursedTorch, npc.velocity.X, npc.velocity.Y);
			}
			if (poisonStormTimer >= 90)
			{
				UsefulFunctions.DustRing(npc.Center, 32, DustID.CursedTorch, 12, 4);
				Lighting.AddLight(npc.Center, Color.Orange.ToVector3() * 5);
				if (Collision.CanHit(npc.position, npc.width, npc.height, Main.player[npc.target].position, Main.player[npc.target].width, Main.player[npc.target].height))
				{
					npc.velocity = Vector2.Zero;
				}
			}

			//Higher alpha = more invisible
			if (npc.justHit)
			{
				npc.alpha = 0;
			}
			if (Main.rand.Next(100) == 1)
			{
				npc.alpha = 225;
				npc.netUpdate = true;
			}
			if (Main.rand.Next(200) == 1)
			{
				npc.alpha = 0; //0 is fully visible 225 is almost invisible
				npc.netUpdate = true;
			}
			if (Main.rand.Next(250) == 1)
			{
				npc.ai[3] = 1;
				npc.life += 5;
				if (npc.life > npc.lifeMax) npc.life = npc.lifeMax;
				npc.ai[1] = 1f;
				npc.netUpdate = true;
			}
		}

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

				Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Dworc Voodoomaster Gore 1"), 1f);
				Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Dworc Gore 2"), 1f);
				Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Dworc Gore 3"), 1f);
				Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Dworc Gore 2"), 1f);
				Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Dworc Gore 3"), 1f);
			}
		}
	}
}

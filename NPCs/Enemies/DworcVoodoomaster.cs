using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Armors;

namespace tsorcRevamp.NPCs.Enemies
{
	public class DworcVoodoomaster : ModNPC
	{
		public override void SetDefaults()
		{
			npc.HitSound = SoundID.NPCHit29;
			npc.DeathSound = SoundID.NPCDeath29;
			npc.damage = 20;
			npc.lifeMax = 212;
			npc.defense = 7;
			npc.value = 4200;
			npc.width = 18;
			npc.aiStyle = -1;
			npc.height = 40;
			npc.knockBackResist = 0.3f;
			npc.buffImmune[BuffID.Poisoned] = true;
			npc.buffImmune[BuffID.OnFire] = true;
			banner = npc.type;
			bannerItem = ModContent.ItemType<Banners.DworcVoodoomasterBanner>();

			animationType = NPCID.Skeleton;
			Main.npcFrameCount[npc.type] = 15;
		}

		public override void NPCLoot()
		{
			Player player = Main.player[npc.target];

			if (Main.rand.Next(50) == 0) Item.NewItem(npc.getRect(), ModContent.ItemType<Items.Accessories.BandOfCosmicPower>());
			if (Main.rand.Next(50) == 0) Item.NewItem(npc.getRect(), ModContent.ItemType<Items.BossItems.CursedSkull>());
			//if (Main.rand.Next(20) == 0) Item.NewItem(npc.getRect(), ModContent.ItemType<Items.Armors.TibalMask>()); TO-DO
			if (Main.rand.Next(50) == 0) Item.NewItem(npc.getRect(), ModContent.ItemType<Items.Potions.CrimsonPotion>());
			if (Main.rand.Next(20) == 0) Item.NewItem(npc.getRect(), ModContent.ItemType<Items.Potions.StrengthPotion>());
			if (Main.rand.Next(10) == 0) Item.NewItem(npc.getRect(), ItemID.FlaskofFire);
			if (Main.rand.Next(12) == 0) Item.NewItem(npc.getRect(), ModContent.ItemType<Items.Potions.ShockwavePotion>());
			if (Main.rand.Next(25) == 0) Item.NewItem(npc.getRect(), ModContent.ItemType<Items.Potions.BattlefrontPotion>());
			if (Main.rand.Next(12) == 0) Item.NewItem(npc.getRect(), ModContent.ItemType<Items.Potions.AttractionPotion>());
			if (Main.rand.Next(3) == 0) Item.NewItem(npc.getRect(), ItemID.IronskinPotion);
			Item.NewItem(npc.getRect(), ItemID.ManaRegenerationPotion, Main.rand.Next(1, 3));

			if (player.GetModPlayer<tsorcRevampPlayer>().BearerOfTheCurse && Main.rand.Next(5) == 0)
			{
				Item.NewItem(npc.getRect(), ModContent.ItemType<Items.Potions.Lifegem>());
			}
			else
			{
				Item.NewItem(npc.getRect(), ItemID.HealingPotion, Main.rand.Next(3, 5));
			}

			if (Main.rand.Next(25) == 0) Item.NewItem(npc.getRect(), ItemID.GillsPotion);
			if (Main.rand.Next(25) == 0) Item.NewItem(npc.getRect(), ItemID.HunterPotion);
			if (Main.rand.Next(2) == 0) Item.NewItem(npc.getRect(), ItemID.MagicPowerPotion, Main.rand.Next(1, 3));
			if (Main.rand.Next(12) == 0) Item.NewItem(npc.getRect(), ItemID.RegenerationPotion);
			if (Main.rand.Next(10) == 0) Item.NewItem(npc.getRect(), ItemID.ShinePotion);
			if (Main.rand.Next(10) == 0) Item.NewItem(npc.getRect(), ItemID.SpelunkerPotion);
			if (Main.rand.Next(10) == 0) Item.NewItem(npc.getRect(), ItemID.SwiftnessPotion);
			if (Main.rand.Next(10) == 0) Item.NewItem(npc.getRect(), ItemID.WaterWalkingPotion);
			if (Main.rand.Next(2) == 0) Item.NewItem(npc.getRect(), ItemID.BattlePotion);
			if (Main.rand.Next(10) == 0) Item.NewItem(npc.getRect(), ItemID.SpellTome);
		}

		float poisonStrikeTimer = 0;
		float poisonStormTimer = 0;
		
		//Spawns in the Jungle Underground and in the Cavern.

		#region Spawn
		public override float SpawnChance(NPCSpawnInfo spawnInfo)
		{
			float chance = 0f;

			if ((spawnInfo.player.ZoneMeteor || spawnInfo.player.ZoneJungle) && !spawnInfo.player.ZoneDungeon && !spawnInfo.player.ZoneCorrupt && !spawnInfo.player.ZoneCrimson)
			{
				if (spawnInfo.player.ZoneOverworldHeight) return 0.005f;
				if (spawnInfo.player.ZoneDirtLayerHeight) return 0.01f;
				if (spawnInfo.player.ZoneRockLayerHeight && Main.dayTime) return 0.0143f;
				if (spawnInfo.player.ZoneRockLayerHeight && !Main.dayTime) return 0.033f;
			}
			if (Main.hardMode && spawnInfo.player.ZoneJungle && !spawnInfo.player.ZoneMeteor && !spawnInfo.player.ZoneBeach && !spawnInfo.player.ZoneCorrupt && !spawnInfo.player.ZoneCrimson) return 0.0005f;

			return chance;
		}

		#endregion

		public override void AI()
		{
			tsorcRevampAIs.FighterAI(npc, 1f, 0.02f, 0.2f, true, enragePercent: 0.3f, enrageTopSpeed: 2);

			bool clearLineofSight = Collision.CanHit(npc.position, npc.width, npc.height, Main.player[npc.target].position, Main.player[npc.target].width, Main.player[npc.target].height);

			tsorcRevampAIs.SimpleProjectile(npc, ref poisonStrikeTimer, 120, ModContent.ProjectileType<Projectiles.Enemy.EnemySpellGreatPoisonStrikeBall>(), 7, 8, clearLineofSight, true, 2, 20, 0);
			tsorcRevampAIs.SimpleProjectile(npc, ref poisonStormTimer, 300, ModContent.ProjectileType<Projectiles.Enemy.EnemySpellPoisonStormBall>(), 9, 0, clearLineofSight, true, 2, 100);

			if (poisonStrikeTimer >= 60)
			{
				Dust.NewDust(npc.position, npc.width, npc.height, DustID.CursedTorch, npc.velocity.X, npc.velocity.Y);
			}
			if (poisonStormTimer >= 240)
			{
				UsefulFunctions.DustRing(npc.Center, 32, DustID.CursedTorch, 12, 4);
				Lighting.AddLight(npc.Center, Color.Orange.ToVector3() * 5);
				if (Collision.CanHit(npc.position, npc.width, npc.height, Main.player[npc.target].position, Main.player[npc.target].width, Main.player[npc.target].height))
				{
					npc.velocity = Vector2.Zero;
				}
			}

			//Transparency. Higher alpha = more invisible
			if (npc.justHit)
			{
				npc.alpha = 0;
			}
			if (Main.rand.Next(200) == 1)
			{
				npc.alpha = 0;
			}
			if (Main.rand.Next(50) == 1)
			{
				npc.alpha = 210;
			}
			if (Main.rand.Next(250) == 1)
			{
				npc.life += 5;
				if (npc.life > npc.lifeMax) npc.life = npc.lifeMax;
				npc.netUpdate = true;
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

				Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Dworc Voodoomaster Gore 1"), 1f);
				Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Dworc Gore 2"), 1f);
				Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Dworc Gore 3"), 1f);
				Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Dworc Gore 2"), 1f);
				Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Dworc Gore 3"), 1f);
			}
		}
		#endregion
	}
}
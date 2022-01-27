using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Armors;

namespace tsorcRevamp.NPCs.Enemies
{
	public class ManHunter : ModNPC
	{
		public int archerBoltDamage = 20;
		public override void SetDefaults()
		{
			aiType = NPCID.SkeletonArcher;
			npc.HitSound = SoundID.NPCHit1;
			npc.DeathSound = SoundID.NPCDeath1;
			npc.damage = 25;
			npc.lifeMax = 250;
			npc.defense = 10;
			npc.value = 1000;
			npc.scale = 0.9f;
			npc.width = 18;
			npc.aiStyle = -1;
			npc.height = 48;
			npc.knockBackResist = 0.7f;
			banner = npc.type;
			bannerItem = ModContent.ItemType<Banners.ManHunterBanner>();

			animationType = NPCID.SkeletonArcher;
			Main.npcFrameCount[npc.type] = 20;

			if (Main.hardMode)
			{
				npc.lifeMax = 500;
				npc.defense = 14;
				npc.value = 1500;
				npc.damage = 50;
				archerBoltDamage = 30;
			}


		}

		public override void ScaleExpertStats(int numPlayers, float bossLifeScale)
		{
			npc.lifeMax = (int)(npc.lifeMax / 2);
			npc.damage = (int)(npc.damage / 2);
			npc.defense = (int)(npc.defense * (2 / 3));
			archerBoltDamage = (int)(archerBoltDamage / 2);
		}

		public override void NPCLoot()
		{
			Player player = Main.player[npc.target];

			if (Main.rand.Next(10) == 0) Item.NewItem(npc.getRect(), ModContent.ItemType<Items.Ammo.ArrowOfBard>(), Main.rand.Next(1, 3));
			if (Main.rand.Next(10) == 0) Item.NewItem(npc.getRect(), ItemID.IronskinPotion);

			if (player.GetModPlayer<tsorcRevampPlayer>().BearerOfTheCurse && Main.rand.Next(8) == 0)
			{
				Item.NewItem(npc.getRect(), ModContent.ItemType<Items.Potions.Lifegem>());
			}
			else
			{
				Item.NewItem(npc.getRect(), ItemID.HealingPotion, Main.rand.Next(1, 3));
			}

			if (Main.rand.Next(2) == 0) Item.NewItem(npc.getRect(), ItemID.HunterPotion);
			if (Main.rand.Next(2) == 0) Item.NewItem(npc.getRect(), ItemID.SwiftnessPotion);
			Item.NewItem(npc.getRect(), ItemID.ArcheryPotion);
		}



		public override float SpawnChance(NPCSpawnInfo spawnInfo)
		{
			float chance = 0f;

			if (!Main.hardMode && !spawnInfo.player.ZoneMeteor && spawnInfo.player.ZoneJungle && !spawnInfo.player.ZoneDungeon && !spawnInfo.player.ZoneCorrupt && !spawnInfo.player.ZoneCrimson)
			{
				if (spawnInfo.player.ZoneOverworldHeight) return 0.1f;
				if (spawnInfo.player.ZoneDirtLayerHeight) return 0.03f;
				if (spawnInfo.player.ZoneRockLayerHeight) return 0.04f;
			}
			if (Main.hardMode && !spawnInfo.player.ZoneMeteor && !spawnInfo.player.ZoneBeach && !spawnInfo.player.ZoneCorrupt && !spawnInfo.player.ZoneCrimson) return 0.02f;

			return chance;
		}

		public override void AI()
        {
			tsorcRevampAIs.ArcherAI(npc, ProjectileID.WoodenArrowHostile, 14, 11, 120, 1.3f, 0.08f, canTeleport: true);
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

				Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Man Hunter Gore 1"), 1f);
				Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Man Hunter Gore 2"), 1f);
				Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Man Hunter Gore 3"), 1f);
				Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Man Hunter Gore 2"), 1f);
				Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Man Hunter Gore 3"), 1f);
			}
		}
	}
}
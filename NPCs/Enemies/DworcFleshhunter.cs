using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.NPCs.Enemies
{
    public class DworcFleshhunter: ModNPC
    {
        public override void SetDefaults()
        {
			npc.HitSound = SoundID.NPCHit26;
			npc.DeathSound = SoundID.NPCDeath29;
			npc.damage = 30;
            npc.lifeMax = 25;
            npc.defense = 12;
			npc.value = 250;
			npc.width = 18;
			npc.aiStyle = -1;
			npc.height = 40;
			npc.knockBackResist = 0.1f;
			npc.buffImmune[BuffID.Poisoned] = true;
			npc.buffImmune[BuffID.OnFire] = true;
			banner = npc.type;
			bannerItem = ModContent.ItemType<Banners.DworcFleshhunterBanner>();

			animationType = NPCID.Skeleton;
            Main.npcFrameCount[npc.type] = 15;
        }

		public override void NPCLoot()
		{
			if (Main.rand.Next(100) == 0) Item.NewItem(npc.getRect(), ModContent.ItemType<Items.Potions.CrimsonPotion>());
			if (Main.rand.Next(20) == 0) Item.NewItem(npc.getRect(), ModContent.ItemType<Items.Weapons.Melee.OrcishHalberd>(), 1, false, -1);
			if (Main.rand.Next(20) == 0) Item.NewItem(npc.getRect(), ItemID.ManaRegenerationPotion);
			if (Main.rand.Next(10) == 0) Item.NewItem(npc.getRect(), ItemID.MagicPowerPotion);
			if (Main.rand.Next(20) == 0) Item.NewItem(npc.getRect(), ItemID.RegenerationPotion);
			if (Main.rand.Next(3) == 0) Item.NewItem(npc.getRect(), ItemID.ShinePotion);
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
				return 0.125f;
			}
			else if (Main.dayTime && !Main.hardMode && spawnInfo.player.ZoneJungle && (spawnInfo.player.ZoneDirtLayerHeight || spawnInfo.player.ZoneRockLayerHeight))
			{
				return 0.17f;
			}
			else if (!Main.dayTime && !Main.hardMode && spawnInfo.player.ZoneJungle && (spawnInfo.player.ZoneDirtLayerHeight || spawnInfo.player.ZoneRockLayerHeight))
			{
				return 0.2f;
			}

			return chance;
		}

		#endregion

		#region AI // code by GrtAndPwrflTrtl (http://www.terrariaonline.com/members/grtandpwrfltrtl.86018/)
		public override void AI()
		{
			tsorcRevampAIs.FighterAI(npc, 1.2f, 0.05f);
			tsorcRevampAIs.LeapAtPlayer(npc, 2, 5, 0.01f, 64);			
		}
		#endregion

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

using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System;

namespace tsorcRevamp.NPCs.Enemies
{
	class MindflayerServant : ModNPC
	{
		public override void SetDefaults()
		{
			Main.npcFrameCount[NPC.type] = 3;
			animationType = 29;
			NPC.aiStyle = 8;
			NPC.damage = 35;
			NPC.defense = 10;
			NPC.height = 44;
			NPC.lifeMax = 142;
			NPC.HitSound = SoundID.NPCHit1;
			NPC.DeathSound = SoundID.NPCDeath1;
			NPC.lavaImmune = true;
			aiType = 32;
			NPC.value = 350;
			NPC.width = 28;
			NPC.knockBackResist = 0.5f;
			banner = NPC.type;
			bannerItem = ModContent.ItemType<Banners.MindflayerServantBanner>();
		}

		public override void ScaleExpertStats(int numPlayers, float bossLifeScale)
		{
			NPC.lifeMax = (int)(NPC.lifeMax / 2);
			NPC.damage = (int)(NPC.damage / 2);
		}

		#region Spawn
		public override float SpawnChance(NPCSpawnInfo spawnInfo)
		{
			if (!spawnInfo.Player.ZoneDungeon && !spawnInfo.Player.ZoneJungle && !spawnInfo.Player.ZoneMeteor && spawnInfo.Player.position.Y < ((Main.rockLayer * 25.0)) && spawnInfo.Player.position.Y > ((Main.worldSurface * 0.44999998807907104)))
			{
				if (spawnInfo.Player.position.Y > ((Main.rockLayer * 15.0)) && spawnInfo.Player.position.X < ((Main.rockLayer * 60.0)) && Main.rand.Next(30) == 1) return 1;
				if (spawnInfo.Player.position.Y > ((Main.rockLayer * 15.0)) && spawnInfo.Player.position.X > ((Main.rockLayer * 145.0)) && Main.rand.Next(30) == 1) return 1;
			}
			return 0;
		}
		#endregion

		#region Gore
		public override void OnKill()
		{
			Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Gores/Piscodemon Gore 1").Type, 1f);
			Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Gores/Piscodemon Gore 2").Type, 1f);
			Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Gores/Piscodemon Gore 3").Type, 1f);
			Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Gores/Piscodemon Gore 2").Type, 1f);
			Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Gores/Piscodemon Gore 3").Type, 1f);

			Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ItemID.Heart, 2);
			if (Main.rand.Next(99) < 5) Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ItemID.LightsBane, 1);
		}
		#endregion
	}
}
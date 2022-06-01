using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.NPCs.Enemies.SuperHardMode
{
	class DarkBloodKnight : ModNPC
	{
		public override void SetDefaults()
		{

			NPC.npcSlots = 2;
			Main.npcFrameCount[NPC.type] = 20;
			animationType = 110;
			NPC.width = 18;
			NPC.height = 48;

			//aiType = 110;
			NPC.aiStyle = 0;
			NPC.timeLeft = 750;
			NPC.damage = 65;
			NPC.defense = 67;
			NPC.HitSound = SoundID.NPCHit1;
			NPC.DeathSound = SoundID.NPCDeath1;
			NPC.lifeMax = 8700;
			NPC.knockBackResist = 0;
			NPC.value = 5430;
			banner = NPC.type;
			bannerItem = ModContent.ItemType<Banners.DarkBloodKnightBanner>();
		}

		int blackFireDamage = 35;

		public override void ScaleExpertStats(int numPlayers, float bossLifeScale)
		{
			NPC.lifeMax = (int)(NPC.lifeMax / 2);
			blackFireDamage = (int)(blackFireDamage * tsorcRevampWorld.SubtleSHMScale);
		}


		public override float SpawnChance(NPCSpawnInfo spawnInfo)
		{
			Player player = spawnInfo.Player;
			bool FrozenOcean = spawnInfo.SpawnTileX > (Main.maxTilesX - 800);
			bool Ocean = spawnInfo.SpawnTileX < 800 || FrozenOcean;

			float chance = 0;
			if (tsorcRevampWorld.SuperHardMode && player.ZoneJungle && !player.ZoneDungeon && !(player.ZoneCorrupt || player.ZoneCrimson) && !Ocean)
            {
                if (player.ZoneOverworldHeight)
                {
					chance = 0.25f;
                }
				else
				{
					chance = 0.3f;
				}
            }

			if (!Main.dayTime)
			{
				chance *= 2;
			}
			if (Main.bloodMoon)
            {
				chance *= 2;
            }

			return chance;
		}


		public override void AI()
		{
			tsorcRevampAIs.ArcherAI(NPC, ModContent.ProjectileType<Projectiles.Enemy.EnemyBlackFire>(), blackFireDamage, 14, 110, 2f, 0.1f, 0.04f, true, lavaJumping: true, projectileGravity: 0.025f);
		}

		public override void OnKill()
		{
			if (NPC.life <= 0)
			{
				Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Gores/Man Hunter Gore 1"), 1.1f);
				Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Gores/Man Hunter Gore 2"), 1.1f);
				Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Gores/Man Hunter Gore 3"), 1.1f);
				Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Gores/Man Hunter Gore 2"), 1.1f);
				Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Gores/Man Hunter Gore 3"), 1.1f);
			}
			Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<Items.FlameOfTheAbyss>());
		}
	}
}
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

			npc.npcSlots = 2;
			Main.npcFrameCount[npc.type] = 20;
			animationType = 110;
			npc.width = 18;
			npc.height = 48;

			//aiType = 110;
			npc.aiStyle = 0;
			npc.timeLeft = 750;
			npc.damage = 65;
			npc.defense = 67;
			npc.HitSound = SoundID.NPCHit1;
			npc.DeathSound = SoundID.NPCDeath1;
			npc.lifeMax = 8700;
			npc.knockBackResist = 0;
			npc.value = 5430;
			banner = npc.type;
			bannerItem = ModContent.ItemType<Banners.DarkBloodKnightBanner>();
		}

		int blackFireDamage = 35;

		public override void ScaleExpertStats(int numPlayers, float bossLifeScale)
		{
			npc.lifeMax = (int)(npc.lifeMax / 2);
			blackFireDamage = (int)(blackFireDamage * tsorcRevampWorld.SubtleSHMScale);
		}


		public override float SpawnChance(NPCSpawnInfo spawnInfo)
		{
			Player player = spawnInfo.player;
			bool FrozenOcean = spawnInfo.spawnTileX > (Main.maxTilesX - 800);
			bool Ocean = spawnInfo.spawnTileX < 800 || FrozenOcean;

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
			tsorcRevampAIs.ArcherAI(npc, ModContent.ProjectileType<Projectiles.Enemy.EnemyBlackFire>(), blackFireDamage, 14, 110, 2f, 0.1f, 0.04f, true, lavaJumping: true, projectileGravity: 0.025f);
		}

		public override void NPCLoot()
		{
			if (npc.life <= 0)
			{
				Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Man Hunter Gore 1"), 1.1f);
				Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Man Hunter Gore 2"), 1.1f);
				Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Man Hunter Gore 3"), 1.1f);
				Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Man Hunter Gore 2"), 1.1f);
				Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Man Hunter Gore 3"), 1.1f);
			}
			Item.NewItem(npc.getRect(), ModContent.ItemType<Items.FlameOfTheAbyss>());
		}
	}
}
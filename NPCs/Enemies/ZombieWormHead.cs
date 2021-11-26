using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace tsorcRevamp.NPCs.Enemies
{
	class ZombieWormHead : ModNPC
	{
		public override void SetStaticDefaults()
		{
			DisplayName.SetDefault("Parasytic Worm");
		}

		public override void SetDefaults()
		{
			animationType = 10;
			npc.netAlways = true;
			npc.npcSlots = 5;
			npc.width = 38;
			npc.height = 32;
			npc.aiStyle = 6;
			npc.defense = 20;
			npc.timeLeft = 750;
			npc.damage = 90;
			npc.HitSound = SoundID.NPCHit1;
			npc.DeathSound = SoundID.NPCDeath5;
			npc.lifeMax = 3000;
			npc.knockBackResist = 0;
			npc.lavaImmune = true;
			npc.noGravity = true;
			npc.noTileCollide = true;
			npc.behindTiles = true;
			npc.value = 4000;
			banner = npc.type;
			bannerItem = ModContent.ItemType<Banners.ParasyticWormBanner>();

			bodyTypes = new int[13];
			int bodyID = ModContent.NPCType<ZombieWormBody>();
			for (int i = 0; i < 13; i++)
			{
				bodyTypes[i] = bodyID;
			}
		}
		int[] bodyTypes;

		public override void ScaleExpertStats(int numPlayers, float bossLifeScale)
		{
			npc.lifeMax = (int)(npc.lifeMax / 2);
			npc.damage = (int)(npc.damage / 2);
			npc.defense = (int)(npc.defense * (2 / 3));
		}

		public override float SpawnChance(NPCSpawnInfo spawnInfo)
		{
			bool nospecialbiome = !spawnInfo.player.ZoneJungle && !spawnInfo.player.ZoneCorrupt && !spawnInfo.player.ZoneCrimson && !spawnInfo.player.ZoneHoly && !spawnInfo.player.ZoneMeteor && !spawnInfo.player.ZoneDungeon; // Not necessary at all to use but needed to make all this work.

			bool sky = nospecialbiome && ((double)spawnInfo.player.position.Y < Main.worldSurface * 0.44999998807907104);
			bool surface = nospecialbiome && !sky && (spawnInfo.player.position.Y <= Main.worldSurface);
			bool underground = nospecialbiome && !surface && (spawnInfo.player.position.Y <= Main.rockLayer);
			bool cavern = nospecialbiome && (spawnInfo.player.position.Y >= Main.rockLayer) && (spawnInfo.player.position.Y <= Main.rockLayer * 25);
			bool undergroundJungle = (spawnInfo.player.position.Y >= Main.rockLayer) && (spawnInfo.player.position.Y <= Main.rockLayer * 25) && spawnInfo.player.ZoneJungle;
			bool undergroundEvil = (spawnInfo.player.position.Y >= Main.rockLayer) && (spawnInfo.player.position.Y <= Main.rockLayer * 25) && (spawnInfo.player.ZoneCorrupt || spawnInfo.player.ZoneCrimson);
			bool undergroundHoly = (spawnInfo.player.position.Y >= Main.rockLayer) && (spawnInfo.player.position.Y <= Main.rockLayer * 25) && spawnInfo.player.ZoneHoly;
			
			if (Main.hardMode)
			{
				if (spawnInfo.player.ZoneUnderworldHeight || undergroundEvil)
				{
					if (Main.rand.Next(40) == 0)
					{
						return 1;
					}
				}
			}
			return 0;
		}

		public override void AI()
		{
			tsorcRevampGlobalNPC.AIWorm(npc, ModContent.NPCType<ZombieWormHead>(), bodyTypes, ModContent.NPCType<ZombieWormTail>(), 15, .4f, 8, 0.07f, false, false, false, true, true);
		}

		private static int ClosestSegment(NPC head, params int[] segmentIDs)
		{
			List<int> segmentIDList = new List<int>(segmentIDs);
			Vector2 targetPos = Main.player[head.target].Center;
			int closestSegment = head.whoAmI; //head is default, updates later
			float minDist = 1000000f; //arbitrarily large, updates later
			for (int i = 0; i < Main.npc.Length; i++)
			{ //iterate through every NPC
				NPC npc = Main.npc[i];
				if (npc != null && npc.active && segmentIDList.Contains(npc.type))
				{ //if the npc is part of the wyvern
					float targetDist = (npc.Center - targetPos).Length();
					if (targetDist < minDist)
					{ //if we're closer than the previously closer segment (or closer than 1,000,000 if it's the first iteration, so always)
						minDist = targetDist; //update minDist. future iterations will compare against the updated value
						closestSegment = i; //and set closestSegment to the whoAmI of the closest segment
					}
				}
			}
			return closestSegment; //the whoAmI of the closest segment
		}

		public override bool SpecialNPCLoot()
		{
			//Putting this here so the gore is spawned before the head is moved
			Vector2 vector8 = new Vector2(npc.position.X + (npc.width * 0.5f), npc.position.Y + (npc.height / 2));
			Gore.NewGore(vector8, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Worm Gore 1"), 1f);
			int closestSegmentID = ClosestSegment(npc, ModContent.NPCType<ZombieWormBody>(), ModContent.NPCType<ZombieWormTail>());
			npc.position = Main.npc[closestSegmentID].position; //teleport the head to the location of the closest segment before running npcloot
			return false;
		}
	}
}
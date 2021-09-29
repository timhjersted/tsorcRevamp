using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System;

namespace tsorcRevamp.NPCs.Enemies
{
	class FlameBat : ModNPC
	{
		public override void SetDefaults()
		{
			npc.npcSlots = 1;
			npc.width = 16;
			npc.height = 18;
			npc.aiStyle = 14;
			npc.timeLeft = 750;
			npc.damage = 90;
			npc.defense = 10;
			npc.HitSound = SoundID.NPCHit1;
			npc.DeathSound = SoundID.NPCDeath4;
			npc.lifeMax = 230;
			npc.knockBackResist = 0.55f;
			npc.noGravity = true;
			npc.value = 270;
			npc.lavaImmune = true;
			Main.npcFrameCount[npc.type] = 4;
			animationType = 93;
			banner = npc.type;
			bannerItem = ModContent.ItemType<Banners.FlameBatBanner>();

			npc.buffImmune[BuffID.Confused] = true;
		}

		public override void ScaleExpertStats(int numPlayers, float bossLifeScale)
		{
			npc.lifeMax = (int)(npc.lifeMax / 2);
			npc.damage = (int)(npc.damage / 2);
		}
		
		public override float SpawnChance(NPCSpawnInfo spawnInfo)
		{
			if (spawnInfo.player.ZoneUnderworldHeight && Main.hardMode && Main.rand.Next(3) == 0)
			{
				return 1;
			}
			return 0;
		}

		public override void AI()
		{
			int num9 = Dust.NewDust(npc.position, npc.width, npc.height, 6, 0.1f, 0.1f, 100, default(Color), 2f);
			Main.dust[num9].noGravity = true;
		}
	}
}
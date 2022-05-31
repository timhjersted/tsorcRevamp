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
			NPC.npcSlots = 1;
			NPC.width = 16;
			NPC.height = 18;
			NPC.aiStyle = 14;
			NPC.timeLeft = 750;
			NPC.damage = 90;
			NPC.defense = 10;
			NPC.HitSound = SoundID.NPCHit1;
			NPC.DeathSound = SoundID.NPCDeath4;
			NPC.lifeMax = 230;
			NPC.knockBackResist = 0.55f;
			NPC.noGravity = true;
			NPC.value = 270;
			NPC.lavaImmune = true;
			Main.npcFrameCount[NPC.type] = 4;
			animationType = 93;
			banner = NPC.type;
			bannerItem = ModContent.ItemType<Banners.FlameBatBanner>();

			NPC.buffImmune[BuffID.Confused] = true;
		}

		public override void ScaleExpertStats(int numPlayers, float bossLifeScale)
		{
			NPC.lifeMax = (int)(NPC.lifeMax / 2);
			NPC.damage = (int)(NPC.damage / 2);
		}
		
		public override float SpawnChance(NPCSpawnInfo spawnInfo)
		{
			if (spawnInfo.Player.ZoneUnderworldHeight && Main.hardMode && Main.rand.Next(3) == 0)
			{
				return 1;
			}
			return 0;
		}

		public override void AI()
		{
			int num9 = Dust.NewDust(NPC.position, NPC.width, NPC.height, 6, 0.1f, 0.1f, 100, default(Color), 2f);
			Main.dust[num9].noGravity = true;
		}
	}
}
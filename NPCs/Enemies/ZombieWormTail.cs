using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;

namespace tsorcRevamp.NPCs.Enemies
{
	class ZombieWormTail : ModNPC
	{
		public override void SetStaticDefaults()
		{
			DisplayName.SetDefault("Parasytic Worm");
		}

		public override void SetDefaults()
		{
			animationType = 10;
			npc.netAlways = true;
			npc.width = 38;
			npc.height = 20;
			npc.aiStyle = 6;
			npc.timeLeft = 750;
			npc.damage = 40;
			npc.defense = 18;
			npc.HitSound = SoundID.NPCHit1;
			npc.DeathSound = SoundID.NPCDeath5;
			npc.lavaImmune = true;
			npc.knockBackResist = 0;
			npc.lifeMax = 91000000;
			npc.noGravity = true;
			npc.noTileCollide = true;
			npc.behindTiles = true;
			npc.value = 500;
			npc.buffImmune[BuffID.Confused] = true;

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

		public override bool? DrawHealthBar(byte hbPosition, ref float scale, ref Vector2 position)
		{
			return false;
		}

		public override void AI()
		{
			tsorcRevampGlobalNPC.AIWorm(npc, ModContent.NPCType<ZombieWormHead>(), bodyTypes, ModContent.NPCType<ZombieWormTail>(), 15, .4f, 8, 0.07f, false, false, false, true, true);
		}
	}
}
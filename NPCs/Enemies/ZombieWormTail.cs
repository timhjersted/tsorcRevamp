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
			AnimationType = 10;
			NPC.netAlways = true;
			NPC.width = 38;
			NPC.height = 20;
			NPC.aiStyle = 6;
			NPC.timeLeft = 750;
			NPC.damage = 40;
			NPC.defense = 18;
			NPC.HitSound = SoundID.NPCHit1;
			NPC.DeathSound = SoundID.NPCDeath5;
			NPC.lavaImmune = true;
			NPC.knockBackResist = 0;
			NPC.lifeMax = 91000000;
			NPC.noGravity = true;
			NPC.noTileCollide = true;
			NPC.behindTiles = true;
			NPC.value = 500;
			NPC.buffImmune[BuffID.Confused] = true;

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
			NPC.lifeMax = (int)(NPC.lifeMax / 2);
			NPC.damage = (int)(NPC.damage / 2);
			NPC.defense = (int)(NPC.defense * (2 / 3));
		}

		public override bool? DrawHealthBar(byte hbPosition, ref float scale, ref Vector2 position)
		{
			return false;
		}

		public override void AI()
		{
			tsorcRevampGlobalNPC.AIWorm(NPC, ModContent.NPCType<ZombieWormHead>(), bodyTypes, ModContent.NPCType<ZombieWormTail>(), 15, .4f, 8, 0.07f, false, false, false, true, true);
		}
	}
}
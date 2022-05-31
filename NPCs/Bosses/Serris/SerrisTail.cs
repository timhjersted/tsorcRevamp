using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items;

namespace tsorcRevamp.NPCs.Bosses.Serris
{
	class SerrisTail : ModNPC
	{
		public override void SetDefaults()
		{
			Main.npcFrameCount[NPC.type] = 3;
			NPC.netAlways = true;
			NPC.npcSlots = 1;
			NPC.width = 38;
			NPC.height = 46;
			NPC.aiStyle = 6;
			NPC.timeLeft = 750;
			NPC.damage = 90;
			NPC.defense = 28;
			NPC.HitSound = SoundID.NPCHit1;
			NPC.DeathSound = SoundID.NPCDeath5;
			NPC.dontTakeDamage = true;
			NPC.lavaImmune = true;
			NPC.knockBackResist = 0;
			NPC.lifeMax = 91000000;
			NPC.noGravity = true;
			NPC.noTileCollide = true;
			NPC.behindTiles = true;
			NPC.boss = true;
			NPC.value = 500;
			NPC.buffImmune[BuffID.Frozen] = true;
			NPC.buffImmune[BuffID.Confused] = true;
		}
		public override void SetStaticDefaults()
		{
			DisplayName.SetDefault("Serris");
		}
		public override void ScaleExpertStats(int numPlayers, float bossLifeScale)
		{
			NPC.damage = (int)(NPC.damage * 1.3 / tsorcRevampGlobalNPC.expertScale);
			NPC.defense = NPC.defense += 12;
		}

		public override bool? DrawHealthBar(byte hbPosition, ref float scale, ref Vector2 position)
		{
			return false;
		}

		bool SpeedBoost = false;
		public override void AI()
		{
			//tsorcRevampGlobalNPC.AIWorm(npc, ModContent.NPCType<SerrisHead>(), SerrisHead.bodyTypes, ModContent.NPCType<SerrisTail>(), 16, -2f, 12f, 0.6f, true, false, true, true, true);

			if (NPC.position.X > Main.npc[(int)NPC.ai[1]].position.X)
			{
				NPC.spriteDirection = -1;
			}
			if (NPC.position.X < Main.npc[(int)NPC.ai[1]].position.X)
			{
				NPC.spriteDirection = 1;
			}

			if (!Main.npc[(int)NPC.ai[1]].active)
			{
				NPC.life = 0;
				NPC.HitEffect(0, 10.0);
				NPC.active = false;

				Vector2 vector8 = new Vector2(NPC.position.X + (NPC.width * 0.5f), NPC.position.Y + (NPC.height / 2));
				Gore.NewGore(NPC.GetSource_Death(), vector8, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.GetGoreSlot("Gores/Serris Gore 2"), 1f);
			}
			
				if (Main.npc[(int)NPC.ai[2]].active && Main.npc[(int)NPC.ai[2]].dontTakeDamage && Main.npc[(int)NPC.ai[2]].type == ModContent.NPCType<NPCs.Bosses.Serris.SerrisHead>())
				{
					SpeedBoost = true;
					return;
				}
				else
				{
					SpeedBoost = false;
				}

		}
		public override bool CheckActive()
		{
			return false;
		}
		public override void FindFrame(int currentFrame)
		{
			int num = 1;
			if (!Main.dedServ)
			{
				num = Main.npcTexture[NPC.type].Height / Main.npcFrameCount[NPC.type];
			}
			NPC.frameCounter += 1.0;
			if (SpeedBoost)
			{
				if (NPC.frameCounter >= 0 && NPC.frameCounter < 5)
				{
					NPC.frame.Y = num;
				}
				if (NPC.frameCounter >= 5 && NPC.frameCounter < 10)
				{
					NPC.frame.Y = num * 2;
				}
				if (NPC.frameCounter >= 10)
				{
					NPC.frameCounter = 0;
				}
			}
			else
			{
				NPC.frame.Y = 0;
				NPC.frameCounter = 0;
			}
		}
	}
}
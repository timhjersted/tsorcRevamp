using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items;

namespace tsorcRevamp.NPCs.Bosses.Serris
{
	class SerrisBody : ModNPC
	{
		public override void SetDefaults()
		{
			Main.npcFrameCount[npc.type] = 3;
			npc.netAlways = true;
			npc.npcSlots = 1;
			npc.width = 38;
			npc.height = 56;
			npc.aiStyle = 6;
			npc.timeLeft = 750;
			npc.damage = 70;
			npc.defense = 28;
			npc.HitSound = SoundID.NPCHit1;
			npc.DeathSound = SoundID.NPCDeath5;
			npc.dontTakeDamage = true;
			npc.lavaImmune = true;
			npc.knockBackResist = 0;
			npc.lifeMax = 600;
			npc.noGravity = true;
			npc.noTileCollide = true;
			npc.behindTiles = true;
			npc.boss = true;
			npc.value = 460;
			npc.buffImmune[BuffID.Frozen] = true;
		}

		public override void SetStaticDefaults()
		{
			DisplayName.SetDefault("Serris");
		}

		public override void ScaleExpertStats(int numPlayers, float bossLifeScale)
		{
			npc.damage = (int)(npc.damage * 1.3 / tsorcRevampGlobalNPC.expertScale);
			npc.defense = npc.defense += 12;
		}

		public override bool? DrawHealthBar(byte hbPosition, ref float scale, ref Vector2 position)
		{
			return false;
		}

		bool SpeedBoost = false;
		public override void AI()
		{
			//tsorcRevampGlobalNPC.AIWorm(npc, ModContent.NPCType<SerrisHead>(), SerrisHead.bodyTypes, ModContent.NPCType<SerrisTail>(), 16, -2f, 12f, 0.6f, true, false, true, true, true);


			if (npc.position.X > Main.npc[(int)npc.ai[1]].position.X)
			{
				npc.spriteDirection = -1;
			}
			if (npc.position.X < Main.npc[(int)npc.ai[1]].position.X)
			{
				npc.spriteDirection = 1;
			}
			if (!Main.npc[(int)npc.ai[1]].active || Main.npc[(int)npc.ai[1]].life <= 0)
			{
				npc.life = 0;
				npc.HitEffect(0, 10.0);
				npc.active = false;

				Vector2 vector8 = new Vector2(npc.position.X + (npc.width * 0.5f), npc.position.Y + (npc.height / 2));
				Gore.NewGore(vector8, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Serris Gore 2"), 1f);
			}
			
			if (Main.npc[(int)npc.ai[2]].active && Main.npc[(int)npc.ai[2]].dontTakeDamage && Main.npc[(int)npc.ai[2]].type == ModContent.NPCType<NPCs.Bosses.Serris.SerrisHead>())
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
				num = Main.npcTexture[npc.type].Height / Main.npcFrameCount[npc.type];
			}
			npc.frameCounter += 1.0;
			if (SpeedBoost)
			{
				if (npc.frameCounter >= 0 && npc.frameCounter < 5)
				{
					npc.frame.Y = num;
				}
				if (npc.frameCounter >= 5 && npc.frameCounter < 10)
				{
					npc.frame.Y = num * 2;
				}
				if (npc.frameCounter >= 10)
				{
					npc.frameCounter = 0;
				}
			}
			else
			{
				npc.frame.Y = 0;
				npc.frameCounter = 0;
			}
		}
	}
}
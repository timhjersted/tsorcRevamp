using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items;

namespace tsorcRevamp.NPCs.Bosses.Serris
{
	[AutoloadBossHead]
	class SerrisX : ModNPC
	{
		public override void SetDefaults()
		{
			Main.npcFrameCount[npc.type] = 16;
			npc.npcSlots = 5;
			npc.width = 100;
			npc.height = 110;
			npc.aiStyle = 2;
			npc.timeLeft = 22500;
			npc.damage = 150;
			npc.defense = 0;
			npc.HitSound = SoundID.NPCHit1;
			npc.DeathSound = SoundID.NPCDeath1;
			npc.lifeMax = 5000;
			npc.scale = 1;
			npc.knockBackResist = 0;
			npc.noGravity = true;
			npc.noTileCollide = true;
			npc.boss = true;
			npc.value = 200000;

			npc.buffImmune[BuffID.Confused] = true;
		}

		public override void SetStaticDefaults()
		{
			DisplayName.SetDefault("Serris-X");
		}

		public override void ScaleExpertStats(int numPlayers, float bossLifeScale)
		{
			npc.damage = (int)(npc.damage * 1.3 / tsorcRevampGlobalNPC.expertScale);
			npc.defense = npc.defense += 12;
		}

		bool immuneFlash = true;
		bool TimeLock = false;
		public override void AI()
		{
			npc.ai[0]++;
			npc.position += npc.velocity * 1.5f;
			if (npc.ai[0] <= 1 || npc.ai[0] >= 300)
			{
				immuneFlash = false;
				npc.dontTakeDamage = false;
				TimeLock = true;
			}
			else if (npc.ai[0] >= 2)
			{
				immuneFlash = true;
				npc.dontTakeDamage = true;
			}
			if (npc.justHit)
			{
				TimeLock = false;
				npc.ai[0] = 2;
			}
			if (TimeLock)
			{
				npc.ai[0] = 0;
			}
		}

		public override void NPCLoot()
		{
			Vector2 vector8 = new Vector2(npc.position.X + (npc.width * 0.5f), npc.position.Y + (npc.height / 2));
			Gore.NewGore(vector8, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Serris-X Gore 1"), 1f);
			Gore.NewGore(vector8, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Serris-X Gore 2"), 1f);
			Gore.NewGore(vector8, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Serris-X Gore 3"), 1f);

			Item.NewItem(npc.getRect(), ModContent.ItemType<Items.Potions.DemonDrugPotion>(), 3 + Main.rand.Next(4));
			Item.NewItem(npc.getRect(), ModContent.ItemType<Items.Potions.ArmorDrugPotion>(), 3 + Main.rand.Next(4));
			Item.NewItem(npc.getRect(), ModContent.ItemType<Items.GuardianSoul>(), 1);
			Item.NewItem(npc.getRect(), ModContent.ItemType<Items.Weapons.Magic.BarrierTome>(), 1);
		}

		public override void FindFrame(int currentFrame)
		{
			int num = 1;
			if (!Main.dedServ)
			{
				num = Main.npcTexture[npc.type].Height / Main.npcFrameCount[npc.type];
			}
			npc.frameCounter += 1.0;
			if (immuneFlash)
			{
				if (npc.frameCounter >= 0 && npc.frameCounter < 5)
				{
					npc.frame.Y = num * 8;
				}
				if (npc.frameCounter >= 5 && npc.frameCounter < 10)
				{
					npc.frame.Y = num * 9;
				}
				if (npc.frameCounter >= 10 && npc.frameCounter < 15)
				{
					npc.frame.Y = num * 10;
				}
				if (npc.frameCounter >= 15 && npc.frameCounter < 20)
				{
					npc.frame.Y = num * 11;
				}
				if (npc.frameCounter >= 20 && npc.frameCounter < 25)
				{
					npc.frame.Y = num * 12;
				}
				if (npc.frameCounter >= 25 && npc.frameCounter < 30)
				{
					npc.frame.Y = num * 13;
				}
				if (npc.frameCounter >= 30 && npc.frameCounter < 35)
				{
					npc.frame.Y = num * 14;
				}
				if (npc.frameCounter >= 35 && npc.frameCounter < 40)
				{
					npc.frame.Y = num * 15;
				}
				if (npc.frameCounter >= 40)
				{
					npc.frameCounter = 0;
				}
			}
			else
			{
				if (npc.frameCounter >= 0 && npc.frameCounter < 5)
				{
					npc.frame.Y = 0;
				}
				if (npc.frameCounter >= 5 && npc.frameCounter < 10)
				{
					npc.frame.Y = num;
				}
				if (npc.frameCounter >= 10 && npc.frameCounter < 15)
				{
					npc.frame.Y = num * 2;
				}
				if (npc.frameCounter >= 15 && npc.frameCounter < 20)
				{
					npc.frame.Y = num * 3;
				}
				if (npc.frameCounter >= 20 && npc.frameCounter < 25)
				{
					npc.frame.Y = num * 4;
				}
				if (npc.frameCounter >= 25 && npc.frameCounter < 30)
				{
					npc.frame.Y = num * 5;
				}
				if (npc.frameCounter >= 30 && npc.frameCounter < 35)
				{
					npc.frame.Y = num * 6;
				}
				if (npc.frameCounter >= 35 && npc.frameCounter < 40)
				{
					npc.frame.Y = num * 7;
				}
				if (npc.frameCounter >= 45)
				{
					npc.frameCounter = 0;
				}
			}
		}
	}
}
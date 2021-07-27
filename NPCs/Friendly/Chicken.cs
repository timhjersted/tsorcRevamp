using Terraria;
using Terraria.ID;
using Terraria.ModLoader;


namespace tsorcRevamp.NPCs.Friendly {
	class Chicken : ModNPC
	{
		public override void SetStaticDefaults()
		{
			Main.npcFrameCount[npc.type] = 14;
		}
		public override void SetDefaults()
		{
			npc.knockBackResist = 0;
			npc.aiStyle = 66; //buggy ai. You are what you eat
			npc.height = 28;
			npc.width = 20;
			npc.lifeMax = 5;
			npc.damage = 0;
			npc.HitSound = SoundID.NPCHit1;
			npc.DeathSound = SoundID.NPCDeath1;
			npc.value = 30;
			banner = npc.type;
			bannerItem = ModContent.ItemType<Banners.ChickenBanner>();
		}
		public override float SpawnChance(NPCSpawnInfo spawnInfo)
		{
			return SpawnCondition.TownGeneralCritter.Chance * 0.2f;
		}

		public override void NPCLoot()
		{
			Item.NewItem(npc.getRect(), ModContent.ItemType<Items.DeadChicken>());
		}

		public override void FindFrame(int frameHeight)
		{
			npc.spriteDirection = npc.direction;
			if (npc.life > 0)
			{
				npc.frameCounter += 1;
			}
			if (npc.velocity.X != 0 && npc.velocity.Y == 0)
			{
				if (npc.frameCounter < 10)
				{
					npc.frame.Y = 0 * frameHeight;
				}
				else if (npc.frameCounter < 20)
				{
					npc.frame.Y = 1 * frameHeight;
				}
				else if (npc.frameCounter < 30)
				{
					npc.frame.Y = 2 * frameHeight;
				}
				else if (npc.frameCounter < 40)
				{
					npc.frame.Y = 3 * frameHeight;
				}
				else if (npc.frameCounter < 50)
				{
					npc.frame.Y = 4 * frameHeight;
				}
				else if (npc.frameCounter < 60)
				{
					npc.frame.Y = 5 * frameHeight;
				}
				else if (npc.frameCounter < 70)
				{
					npc.frame.Y = 6 * frameHeight;
				}
				else if (npc.frameCounter < 80)
				{
					npc.frame.Y = 7 * frameHeight;
				}
				else if (npc.frameCounter < 90)
				{
					npc.frame.Y = 8 * frameHeight;
				}
				else if (npc.frameCounter < 100)
				{
					npc.frame.Y = 9 * frameHeight;
				}
				else if (npc.frameCounter < 110)
				{
					npc.frame.Y = 10 * frameHeight;
				}
				else if (npc.frameCounter < 120)
				{
					npc.frame.Y = 11 * frameHeight;
				}
				else if (npc.frameCounter < 130)
				{
					npc.frame.Y = 12 * frameHeight;
				}
				else if (npc.frameCounter < 140)
				{
					npc.frame.Y = 13 * frameHeight;
				}
				else
				{
					npc.frameCounter = 0;
				}
			}
			else
			{
				npc.frame.Y = 0;
			}
		}
	}
}

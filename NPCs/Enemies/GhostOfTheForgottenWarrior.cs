using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.NPCs.Enemies
{
	public class GhostOfTheForgottenWarrior : ModNPC
	{
		public override void SetDefaults()
		{
			npc.HitSound = SoundID.NPCHit1;
			npc.DeathSound = SoundID.NPCDeath1;
			npc.damage = 35;
			npc.lifeMax = 95;
			npc.defense = 14;
			npc.value = 450;
			npc.width = 20;
			npc.aiStyle = 3;
			npc.height = 40;
			npc.knockBackResist = 0f;

			animationType = NPCID.GoblinWarrior;
			Main.npcFrameCount[npc.type] = 16;
		}
		public override void NPCLoot()
		{
			if (Main.rand.Next(3) == 0) Item.NewItem(npc.getRect(), ModContent.ItemType<Items.Weapons.Melee.EphemeralThrowingSpear>(), Main.rand.Next(15, 31));
			Item.NewItem(npc.getRect(), ModContent.ItemType<Items.Weapons.Magic.WallTome>());
		}

		#region Spawn
		public override float SpawnChance(NPCSpawnInfo spawnInfo)
		{
			float chance = 0f;

			if (!Main.hardMode && spawnInfo.player.ZoneDungeon)
			{
				return 0.5f; //what the hell? this is so high
			}
			else if (Main.hardMode && spawnInfo.player.ZoneDungeon)
			{
				return 0.05f;
			}
			else if (tsorcRevampWorld.SuperHardMode && spawnInfo.player.ZoneDungeon)
			{
				return 0.01f;
			}

			return chance;
		}

		#endregion

		public override void HitEffect(int hitDirection, double damage)
		{
			for (int i = 0; i < 5; i++)
			{
				int dustType = 5;
				int dustIndex = Dust.NewDust(npc.position, npc.width, npc.height, dustType);
				Dust dust = Main.dust[dustIndex];
				dust.velocity.X = dust.velocity.X + Main.rand.Next(-50, 51) * 0.06f;
				dust.velocity.Y = dust.velocity.Y + Main.rand.Next(-50, 51) * 0.06f;
				dust.scale *= 1f + Main.rand.Next(-30, 31) * 0.01f;
				dust.noGravity = true;
			}
			if (npc.life <= 0)
			{
				for (int i = 0; i < 25; i++)
				{
					Dust.NewDust(npc.position, npc.width, npc.height, 5, Main.rand.Next(-3, 3), Main.rand.Next(-3, 3), 70, default(Color), 1f);
				}

				Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Wild Warrior Gore 1"), 1f);
				Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Wild Warrior Gore 2"), 1f);
				Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Wild Warrior Gore 3"), 1f);
				Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Wild Warrior Gore 2"), 1f);
				Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Wild Warrior Gore 3"), 1f);
			}
		}
	}
}
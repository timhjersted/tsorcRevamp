using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Utilities;
using Terraria.Localization;
using System.Collections.Generic;

namespace tsorcRevamp.NPCs.Friendly
{
	[AutoloadHead]
	class TibianArcher : ModNPC
	{
		public override bool Autoload(ref string name) => true;
		public override void SetStaticDefaults()
		{
			DisplayName.SetDefault("Tibian Archer");
			Main.npcFrameCount[npc.type] = 26;
			NPCID.Sets.ExtraFramesCount[npc.type] = 10;
			NPCID.Sets.AttackFrameCount[npc.type] = 5;
			NPCID.Sets.DangerDetectRange[npc.type] = 700;
			NPCID.Sets.AttackType[npc.type] = 1; // 0 is throwing, 1 is shooting, 2 is magic, 3 is melee
			NPCID.Sets.AttackTime[npc.type] = 40;
			NPCID.Sets.AttackAverageChance[npc.type] = 10;
			NPCID.Sets.HatOffsetY[npc.type] = 4;
		}

		public static List<string> Names = new List<string> {
			"Elane", "Legola", "Galuna", "Enalea"
		};

		public override string TownNPCName()
		{
			string name = Names[Main.rand.Next(Names.Count)]; //pick a random name from the list
			return name;
		}

		public override void SetDefaults()
		{
			npc.townNPC = true;
			npc.friendly = true;
			npc.width = 18;
			npc.height = 40;
			npc.aiStyle = 7;
			npc.damage = 50;
			npc.defense = 15;
			npc.lifeMax = 1000;
			npc.HitSound = SoundID.NPCHit1;
			npc.DeathSound = SoundID.NPCDeath1;
			npc.knockBackResist = 0.3f;
			animationType = NPCID.Guide;
		}

		public override string GetChat()
		{
			WeightedRandom<string> chat = new WeightedRandom<string>();
			chat.Add("I am the local fletcher. I sell bows, crossbows and ammunition. Do you need anything?");
			chat.Add("Tibia, a green island. It is wonderful to walk into the forests and to hunt with a bow there.");
			chat.Add("I am paladin and fletcher.");
			chat.Add("We are feared warriors and good marksmen.");
			chat.Add("Hello. Would you like to buy some of my wares?");
			chat.Add("Please show respect to Eloise. I don't want to have to hurt you.");
			chat.Add("Amazons and dworcs are a real threat.");
			chat.Add("I'm far from home, but this isn't so bad.");
			return chat;
		}

		public override void SetChatButtons(ref string button, ref string button2)
		{
			button = Language.GetTextValue("LegacyInterface.28");
		}

		public override void OnChatButtonClicked(bool firstButton, ref bool shop)
		{
			if (firstButton)
			{
				shop = true;
				return;
			}
		}

		public override void SetupShop(Chest shop, ref int nextSlot)
		{
			shop.item[nextSlot].SetDefaults(ModContent.ItemType<Items.Ammo.Bolt>());
			nextSlot++;
			shop.item[nextSlot].SetDefaults(ModContent.ItemType<Items.Weapons.Melee.ThrowingAxe>());
			nextSlot++;
			shop.item[nextSlot].SetDefaults(ModContent.ItemType<Items.Weapons.Ranged.ThrowingSpear>());
			nextSlot++;
			shop.item[nextSlot].SetDefaults(ModContent.ItemType<Items.Armors.OldLeatherHelmet>());
			nextSlot++;
			shop.item[nextSlot].SetDefaults(ModContent.ItemType<Items.Armors.OldLeatherArmor>());
			nextSlot++;
			shop.item[nextSlot].SetDefaults(ModContent.ItemType<Items.Armors.OldLeatherGreaves>());
			nextSlot++;
			shop.item[nextSlot].SetDefaults(ItemID.WoodenArrow);
			nextSlot++;
			shop.item[nextSlot].SetDefaults(ItemID.LesserHealingPotion);
			nextSlot++;
			shop.item[nextSlot].SetDefaults(ItemID.Safe);
			nextSlot++;

			if (NPC.downedBoss1)
			{
				shop.item[nextSlot].SetDefaults(ModContent.ItemType<Items.Weapons.Ranged.OldCrossbow>());
				nextSlot++;
				shop.item[nextSlot].SetDefaults(ModContent.ItemType<Items.Weapons.Ranged.OldLongbow>());
				nextSlot++;
				if (!ModContent.GetInstance<tsorcRevampConfig>().LegacyMode)
				{
					shop.item[nextSlot].SetDefaults(ItemID.FrostburnArrow);
					nextSlot++;
				}
			}
			if (NPC.downedBoss2)
			{
				shop.item[nextSlot].SetDefaults(ModContent.ItemType<Items.Weapons.Ranged.RoyalThrowingSpear>());
				nextSlot++;
			}
			if (Main.hardMode)
			{
				shop.item[nextSlot].SetDefaults(ModContent.ItemType<Items.Weapons.Magic.EnchantedThrowingSpear>());
				nextSlot++;
				shop.item[nextSlot].SetDefaults(ModContent.ItemType<Items.Ammo.PowerBolt>());
				nextSlot++;
				if (!ModContent.GetInstance<tsorcRevampConfig>().LegacyMode)
                {
					shop.item[nextSlot].SetDefaults(ItemID.HellfireArrow);
					nextSlot++;
				}
			}
			if (tsorcRevampWorld.SuperHardMode)
			{
				shop.item[nextSlot].SetDefaults(ModContent.ItemType<Items.ForgottenIceBowScroll>());
				nextSlot++;
				shop.item[nextSlot].SetDefaults(ModContent.ItemType<Items.ForgottenThunderBowScroll>());
				nextSlot++;
			}
		}

		public override void HitEffect(int hitDirection, double damage)
		{
			if (base.npc.life <= 0) //even though npcs are immortal
			{
				Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Tibian Female Archer Gore 1"));
				Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Tibian Female Archer Gore 2"));
				Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Tibian Female Archer Gore 2"));
				Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Tibian Female Archer Gore 3"));
				Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Tibian Female Archer Gore 3"));
			}
		}

		public override void TownNPCAttackStrength(ref int damage, ref float knockback)
		{
			if (Main.hardMode)
            {
				damage = 44;
				knockback = 6f;
			}
			else
			{
				damage = 22;
				knockback = 5f;
			}
		}

		public override void TownNPCAttackCooldown(ref int cooldown, ref int randExtraCooldown)
		{
			cooldown = 5;
			randExtraCooldown = 20;
		}
		public override void DrawTownAttackGun(ref float scale, ref int item, ref int closeness)
		{
			item = ModContent.ItemType<Items.Weapons.Ranged.ElfinBow>();
			scale = 1f;
			closeness = 20;
		}

		public override void TownNPCAttackProj(ref int projType, ref int attackDelay)
		{
			projType = ProjectileID.HellfireArrow;
			attackDelay = 35;
		}

		public override void TownNPCAttackProjSpeed(ref float multiplier, ref float gravityCorrection, ref float randomOffset)
		{
			multiplier = 20f;
			gravityCorrection = 25f;
			randomOffset = 0f;
		}

		public override bool CanTownNPCSpawn(int numTownNPCs, int money)
		{
			foreach (Player p in Main.player)
			{
				if (!p.active)
				{
					continue;
				}
				if (p.statDefense > 0)
				{
					return true;
				}
			}
			return false;
		}

		public override bool CanGoToStatue(bool toQueenStatue)
		{
			return true;
		}
	}
}

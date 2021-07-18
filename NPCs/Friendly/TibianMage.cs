using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Utilities;
using Microsoft.Xna.Framework.Graphics;
using Terraria.Localization;
using System.Collections.Generic;

namespace tsorcRevamp.NPCs.Friendly
{
	[AutoloadHead]
	class TibianMage : ModNPC
	{
		public override bool Autoload(ref string name) => true;
		public override void SetStaticDefaults()
		{
			DisplayName.SetDefault("Tibian Mage");
			Main.npcFrameCount[npc.type] = 26;
			NPCID.Sets.ExtraFramesCount[npc.type] = 10;
			NPCID.Sets.AttackFrameCount[npc.type] = 5;
			NPCID.Sets.DangerDetectRange[npc.type] = 250; // keep this low for melee
			NPCID.Sets.AttackType[npc.type] = 1; // 0 is throwing, 1 is shooting, 2 is magic, 3 is melee
			NPCID.Sets.AttackTime[npc.type] = 25;
			NPCID.Sets.AttackAverageChance[npc.type] = 10;
			NPCID.Sets.HatOffsetY[npc.type] = 4;
		}

		public static List<string> Names = new List<string> {
			"Asima", "Lea", "Padreia", "Loria", "Lungelen", "Lily", "Sandra", "Tibra", "Astera Tiger"
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
			npc.defense = 25;
			npc.lifeMax = 1000;
			npc.HitSound = SoundID.NPCHit1;
			npc.DeathSound = SoundID.NPCDeath1;
			npc.knockBackResist = 0.3f;
			animationType = NPCID.Guide;
		}

		public override string GetChat()
		{
			WeightedRandom<string> chat = new WeightedRandom<string>();
			chat.Add("Time is a force we sorcerers will master one day.");
			chat.Add("Any sorcerer dedicates his whole life to the study of the arcane arts.");
			chat.Add("Sorry, I only sell spells to sorcerers.");
			chat.Add("I could tell you much about all sorcerer spells, but you won't understand it. Anyway, feel free to ask me.");
			chat.Add("I'll teach you a very seldom spell.");
			chat.Add("Many call themselves a sorcerer, but only a few truly understand what that means.");
			chat.Add("Sorcerers are destructive. Their power lies in destruction and pain.");
			chat.Add("Welcome to our humble guild, wanderer. May I be of any assistance to you?");
			return chat;
		}

		int weaponChoice;
		public override void AI()
		{
			if (Main.rand.Next(40) == 0) //this can unfortunately sometimes flip mid cast, causing the npc's weapon to switch in their hand
			{							//or even cause the wrong projectile to cast 

				weaponChoice = Main.rand.Next(0, 10);
				//Main.NewText(weaponChoice);
			}
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
			shop.item[nextSlot].SetDefaults(ModContent.ItemType<Items.Weapons.Magic.WandOfDarkness>());
			nextSlot++;
			shop.item[nextSlot].SetDefaults(ModContent.ItemType<Items.Accessories.CosmicWatch>());
			nextSlot++;
			shop.item[nextSlot].SetDefaults(ModContent.ItemType<Items.Armors.RedMageHat>());
			nextSlot++;
			shop.item[nextSlot].SetDefaults(ModContent.ItemType<Items.Armors.RedMageTunic>());
			nextSlot++;
			shop.item[nextSlot].SetDefaults(ModContent.ItemType<Items.Armors.RedMagePants>());
			nextSlot++;
			shop.item[nextSlot].SetDefaults(ItemID.Gel);
			nextSlot++;
			shop.item[nextSlot].SetDefaults(ItemID.SpellTome);
			nextSlot++;
			if (!ModContent.GetInstance<tsorcRevampConfig>().LegacyMode)
			{
				shop.item[nextSlot].SetDefaults(ItemID.MagicMirror);
				nextSlot++;
			}
			if (NPC.downedBoss1)
			{
				shop.item[nextSlot].SetDefaults(ModContent.ItemType<Items.Weapons.Magic.WandOfFire>());
				nextSlot++;
				shop.item[nextSlot].SetDefaults(ItemID.PiggyBank);
				nextSlot++;
				shop.item[nextSlot].SetDefaults(ModContent.ItemType<Items.Weapons.Melee.ForgottenIceRod>());
				nextSlot++;
				shop.item[nextSlot].SetDefaults(ModContent.ItemType<Items.Weapons.Melee.ForgottenThunderRod>());
				nextSlot++;
			}
			if (Main.hardMode)
			{
				shop.item[nextSlot].SetDefaults(ModContent.ItemType<Items.Weapons.Magic.FlameStrikeScroll>());
				nextSlot++;
				shop.item[nextSlot].SetDefaults(ModContent.ItemType<Items.Weapons.Magic.EnergyStrikeScroll>());
				nextSlot++;
				shop.item[nextSlot].SetDefaults(ModContent.ItemType<Items.Weapons.Magic.DeathStrikeScroll>());
				nextSlot++;
				shop.item[nextSlot].SetDefaults(ModContent.ItemType<Items.Accessories.CovetousSilverSerpentRing>());
				nextSlot++;
				shop.item[nextSlot].SetDefaults(ModContent.ItemType<Items.Weapons.Magic.ManaBomb>());
				nextSlot++;
			}
			if (NPC.downedMechBoss1) //mechboss 1 is the destroyer, 2 is the twins, 3 is skelleprime
            {
				shop.item[nextSlot].SetDefaults(ModContent.ItemType<Items.Weapons.Melee.ForgottenStardustRod>());
				nextSlot++;
			}
		}

		public override void HitEffect(int hitDirection, double damage)
		{
			if (base.npc.life <= 0) //even though npcs are immortal
			{
				Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Tibian Female Mage Gore 1"));
				Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Tibian Female Mage Gore 2"));
				Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Tibian Female Mage Gore 2"));
				Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Tibian Female Mage Gore 3"));
				Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Tibian Female Mage Gore 3"));
			}
		}

		public override void TownNPCAttackStrength(ref int damage, ref float knockback)
		{
			if (weaponChoice < 8) //More likely to use Great Soul Arrow Staff
			{
				damage = 25;
				knockback = 4f;
			}
			if (weaponChoice >= 8)
			{
				damage = 10;
				knockback = 5f;
			}
		}

		public override void TownNPCAttackCooldown(ref int cooldown, ref int randExtraCooldown)
		{
			cooldown = 30;
			randExtraCooldown = 30;
		}
		public override void DrawTownAttackGun(ref float scale, ref int item, ref int closeness)
		{
			if (weaponChoice < 8)
			{
				item = ModContent.ItemType<Items.Weapons.Magic.GreatSoulArrowStaff>();
				scale = 1f;
				closeness = 4;
			}
			if (weaponChoice >= 8)
			{
				item = ModContent.ItemType<Items.Weapons.Magic.TheBlackenedFlames>();
				scale = 1f;
				closeness = 4;
			}
		}

		public override void TownNPCAttackProj(ref int projType, ref int attackDelay)
		{
			if (weaponChoice < 8)
			{
				projType = ModContent.ProjectileType<Projectiles.GreatSoulArrow>();
				attackDelay = 8;
			}
			if (weaponChoice >= 8)
			{
				projType = ModContent.ProjectileType<Projectiles.BlackFire>();
				attackDelay = 8;
			}
		}

		public override void TownNPCAttackProjSpeed(ref float multiplier, ref float gravityCorrection, ref float randomOffset)
		{
			multiplier = 10f;
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
				if (p.statManaMax2 > 21) //was 1, but these days mana starts at 20 on fresh char.
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

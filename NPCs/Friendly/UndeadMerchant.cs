using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Utilities;
using Terraria.Localization;

namespace tsorcRevamp.NPCs.Friendly
{
	[AutoloadHead]
	class UndeadMerchant : ModNPC
	{
		public override bool Autoload(ref string name) => true;
		public override void SetStaticDefaults()
		{
			DisplayName.SetDefault("Undead Merchant");
			Main.npcFrameCount[npc.type] = 26;
			NPCID.Sets.ExtraFramesCount[npc.type] = 9;
			NPCID.Sets.AttackFrameCount[npc.type] = 4;
			NPCID.Sets.DangerDetectRange[npc.type] = 200;
			NPCID.Sets.AttackType[npc.type] = 0;
			NPCID.Sets.AttackTime[npc.type] = 18;
			NPCID.Sets.AttackAverageChance[npc.type] = 10;
			NPCID.Sets.HatOffsetY[npc.type] = 4;
		}
		public override string TownNPCName()
		{
			return "Uldred";
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
			npc.knockBackResist = 0.5f;
			animationType = NPCID.SkeletonMerchant;
		}

		public override string GetChat()
		{
			WeightedRandom<string> chat = new WeightedRandom<string>();
			chat.Add("Well, now... You seem to have your wits about you, hmm? Then you are a welcome customer! I trade for souls. Everything's for sale! Nee hee hee hee hee!", 1.5);
			chat.Add("I hope you've brought plenty of souls? Nee hee hee hee hee!");
			chat.Add("Oh, there you are. Where have you been hiding? I guessed you'd hopped the twig for certain. Bah, shows what I know! Nee hee hee hee!");
			chat.Add("Oh? Still not popped your clogs?");
			chat.Add("Oh, there you are. Still keeping your marbles all together? Then, go ahead, don't be a nitwit. Never hurts to splurge when your days are numbered! Nee hee hee hee hee!");
			chat.Add("Eh? I'm not here to chit-chat. We talk business, or we talk nothing at all.");

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
			shop.item[nextSlot].SetDefaults(ModContent.ItemType<Items.CharcoalPineResin>());
			shop.item[nextSlot].shopCustomPrice = 20;
			shop.item[nextSlot].shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId;
			nextSlot++;
			shop.item[nextSlot].SetDefaults(ModContent.ItemType<Items.Weapons.Throwing.Firebomb>());
			shop.item[nextSlot].shopCustomPrice = 5;
			shop.item[nextSlot].shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId;
			nextSlot++;
			shop.item[nextSlot].SetDefaults(ModContent.ItemType<Items.BloodredMossClump>());
			shop.item[nextSlot].shopCustomPrice = 4;
			shop.item[nextSlot].shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId;
			nextSlot++;
			shop.item[nextSlot].SetDefaults(ModContent.ItemType<Items.Potions.GlowingMushroomSkewer>());
			shop.item[nextSlot].shopCustomPrice = 5;
			shop.item[nextSlot].shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId;
			nextSlot++;
			shop.item[nextSlot].SetDefaults(ModContent.ItemType<Items.Potions.HealingElixir>());
			shop.item[nextSlot].shopCustomPrice = 25;
			shop.item[nextSlot].shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId;
			nextSlot++;
			shop.item[nextSlot].SetDefaults(ItemID.BattlePotion);
			shop.item[nextSlot].shopCustomPrice = 30;
			shop.item[nextSlot].shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId;
			nextSlot++;
			shop.item[nextSlot].SetDefaults(ModContent.ItemType<Items.Accessories.IronShield>());
			shop.item[nextSlot].shopCustomPrice = 200;
			shop.item[nextSlot].shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId;
			nextSlot++;
			shop.item[nextSlot].SetDefaults(ModContent.ItemType<Items.Armors.HollowSoldierHelmet>());
			shop.item[nextSlot].shopCustomPrice = 100;
			shop.item[nextSlot].shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId;
			nextSlot++;
			shop.item[nextSlot].SetDefaults(ModContent.ItemType<Items.Armors.HollowSoldierBreastplate>());
			shop.item[nextSlot].shopCustomPrice = 100;
			shop.item[nextSlot].shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId;
			nextSlot++;
			shop.item[nextSlot].SetDefaults(ModContent.ItemType<Items.Armors.HollowSoldierWaistcloth>());
			shop.item[nextSlot].shopCustomPrice = 100;
			shop.item[nextSlot].shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId;
			nextSlot++;

			if (NPC.downedMechBoss1) //The Destroyer
			{
				shop.item[nextSlot].SetDefaults(ModContent.ItemType<Items.Weapons.Throwing.BlackFirebomb>());
				shop.item[nextSlot].shopCustomPrice = 50;
				shop.item[nextSlot].shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId;
				nextSlot++;
			}

			if (tsorcRevampWorld.Slain.ContainsKey(ModContent.NPCType<Bosses.Okiku.FinalForm.Attraidies>()) || tsorcRevampWorld.SuperHardMode /*just in case*/)
			{
				shop.item[nextSlot].SetDefaults(ModContent.ItemType<Items.PurgingStone>());
				shop.item[nextSlot].shopCustomPrice = 9999;
				shop.item[nextSlot].shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId;
				nextSlot++;
			}

			if (Main.bloodMoon)
			{
				shop.item[nextSlot].SetDefaults(ModContent.ItemType<Items.Armors.MaskOfTheChild>());
				shop.item[nextSlot].shopCustomPrice = 1000;
				shop.item[nextSlot].shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId;
				nextSlot++;
				shop.item[nextSlot].SetDefaults(ModContent.ItemType<Items.Armors.MaskOfTheFather>());
				shop.item[nextSlot].shopCustomPrice = 1000;
				shop.item[nextSlot].shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId;
				nextSlot++;
				shop.item[nextSlot].SetDefaults(ModContent.ItemType<Items.Armors.MaskOfTheMother>());
				shop.item[nextSlot].shopCustomPrice = 1000;
				shop.item[nextSlot].shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId;
				nextSlot++;
			}
		}

		public override void TownNPCAttackStrength(ref int damage, ref float knockback)
		{
			if (Main.hardMode)
			{
				damage = 90;
				knockback = 7f;
			}
			else
			{
				damage = 60;
				knockback = 5f;
			}
		}

		public override void TownNPCAttackCooldown(ref int cooldown, ref int randExtraCooldown)
		{
			cooldown = 50;
			randExtraCooldown = 30;
		}

		public override void TownNPCAttackProj(ref int projType, ref int attackDelay)
		{
			projType = ModContent.ProjectileType<Projectiles.Firebomb>();
			attackDelay = 5;
		}

		public override void TownNPCAttackProjSpeed(ref float multiplier, ref float gravityCorrection, ref float randomOffset)
		{
			multiplier = 6.5f;
			gravityCorrection = 30f;
			randomOffset = 0f;
		}

		public override bool CanTownNPCSpawn(int numTownNPCs, int money)
		{
			int type = ModContent.ItemType<Items.SoulShekel>();

			for (int i = 0; i < Main.maxPlayers; i++)
			{
				Player player = Main.player[i];
				if (!player.active)
				{
					continue;
				}

				if (NPC.downedBoss1 || NPC.downedBoss2 || NPC.downedBoss3 || player.CountItem(type, 50) >= 50)
				{
					return true;
				}
			}
			return false;
		}

		public override bool CanGoToStatue(bool toKingStatue)
		{
			return true;
		}
	}
}

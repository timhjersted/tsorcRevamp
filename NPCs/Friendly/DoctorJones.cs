using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Utilities;
using Terraria.Localization;

namespace tsorcRevamp.NPCs.Friendly
{
	[AutoloadHead]
	class DoctorJones : ModNPC
	{
		public override bool Autoload(ref string name) => true;
		public override void SetStaticDefaults()
		{
			DisplayName.SetDefault("DoctorJones");
			Main.npcFrameCount[npc.type] = 26;
			NPCID.Sets.ExtraFramesCount[npc.type] = 10;
			NPCID.Sets.AttackFrameCount[npc.type] = 5;
			NPCID.Sets.DangerDetectRange[npc.type] = 140;
			NPCID.Sets.AttackType[npc.type] = 1; // 0 is throwing, 1 is shooting, 2 is magic, 3 is melee
			NPCID.Sets.AttackTime[npc.type] = 25;
			NPCID.Sets.AttackAverageChance[npc.type] = 10;
			NPCID.Sets.HatOffsetY[npc.type] = 4;
		}

		/*public static List<string> Names = new List<string> {
			"Harrison", "Han", "Indie", "Rick"
		};*/

		/*public override string TownNPCName()
		{
			string name = Names[Main.rand.Next(Names.Count)]; //pick a random name from the list
			return name;
		}*/

		public override void SetDefaults()
		{
			//npc.townNPC = true;
			npc.friendly = true;
			npc.width = 18;
			npc.height = 40;
			npc.aiStyle = mod.NPCType("Archeologist");
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
			chat.Add("Remember, X never marks the spot.");
			chat.Add("The report of my undeath was an exaggeration.");
			chat.Add("I *hate* snakes.");
			chat.Add("Seen any fortune or glory lately?");
			chat.Add("Trust me.");
			chat.Add("Ha! You think THESE boulder traps are bad...");
			chat.Add("I have no idea what I'm doing, but I know I'm doing it really, really well.");
			chat.Add("Having tree troubles? Try fire... Or, you know, an axe.");
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
			shop.item[nextSlot].SetDefaults(ModContent.ItemType<Items.MysteriousIdol>());
			nextSlot++;
			shop.item[nextSlot].SetDefaults(ItemID.MeteorShot);
			nextSlot++;
			shop.item[nextSlot].SetDefaults(ItemID.WoodenArrow);
			nextSlot++;
			shop.item[nextSlot].SetDefaults(ItemID.Torch);
			nextSlot++;
			shop.item[nextSlot].SetDefaults(ItemID.ManaPotion);
			nextSlot++;
			shop.item[nextSlot].SetDefaults(ItemID.HealingPotion);
			nextSlot++;
			shop.item[nextSlot].SetDefaults(ItemID.PurificationPowder);
			nextSlot++;
			shop.item[nextSlot].SetDefaults(ItemID.NightOwlPotion);
			nextSlot++;
			shop.item[nextSlot].SetDefaults(ItemID.BottledWater);
			nextSlot++;
			shop.item[nextSlot].SetDefaults(ItemID.GlowingMushroom);
			nextSlot++;
			shop.item[nextSlot].SetDefaults(ItemID.FamiliarWig);
			nextSlot++;
			shop.item[nextSlot].SetDefaults(ItemID.PoisonedKnife);
			nextSlot++;
			shop.item[nextSlot].SetDefaults(ItemID.Grenade);
			nextSlot++;
			shop.item[nextSlot].SetDefaults(ModContent.ItemType<Items.Accessories.CosmicWatch>());
			nextSlot++;
			shop.item[nextSlot].SetDefaults(ItemID.TinkerersWorkshop);
			nextSlot++;
		}

		public override void HitEffect(int hitDirection, double damage)
		{
			if (base.npc.life <= 0) //even though npcs are immortal
			{
				Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Archeologist Head Gore"));
				Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Archeologist Arm Gore"));
				Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Archeologist Arm Gore"));
				Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Archeologist Leg Gore"));
				Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Archeologist Leg Gore"));
			}
		}

		int weaponChoice;
		public override void AI()
		{

			if (Main.rand.Next(40) == 0)
			{
				weaponChoice = Main.rand.Next(0, 10);
			}
		}
		public override void TownNPCAttackStrength(ref int damage, ref float knockback)
		{
			if (Main.hardMode)
			{
				damage = 32;
				knockback = 6f;
			}
			else
			{
				damage = 16;
				knockback = 5f;
			}
		}

		public override void TownNPCAttackCooldown(ref int cooldown, ref int randExtraCooldown)
		{
			cooldown = 10;
			randExtraCooldown = 20;
		}
		public override void DrawTownAttackGun(ref float scale, ref int item, ref int closeness)
		{
			item = ItemID.RopeCoil;
			scale = .7f;
			closeness = 26;
		}

		public override void TownNPCAttackProj(ref int projType, ref int attackDelay)
		{
			projType = mod.ProjectileType("ArcheologistWhip");

			attackDelay = 1;
		}

		public override void TownNPCAttackProjSpeed(ref float multiplier, ref float gravityCorrection, ref float randomOffset)
		{
			multiplier = 1.5f;
			randomOffset = 0f;
		}

		public override float SpawnChance(NPCSpawnInfo spawnInfo)
		{
			float chance = 0;

			if (spawnInfo.player.ZoneJungle && NPC.CountNPCS(mod.NPCType("JungleSentree")) < 1 && Main.rand.Next(10) == 0)
			{
				Main.NewText("The spirit of adventure is nearby...", 255, 255, 0);
				return 1f; // It's high because the chance of the conditions being right is pretty low
			}
			return chance;
		}

		/*public override bool CanTownNPCSpawn(int numTownNPCs, int money)
		{
			foreach (Player p in Main.player)
			{
				if (!p.active)
				{
					continue;
				}
				if (p.HasItem(ModContent.ItemType<Items.MysteriousIdol>()))
				{
					return true;
				}
			}
			return false;
		}*/

		/*public override bool CanGoToStatue(bool toKingStatue)
		{
			return true;
		}*/
	}
}

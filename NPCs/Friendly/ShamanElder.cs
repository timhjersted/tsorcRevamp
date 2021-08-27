using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Utilities;
using tsorcRevamp.Items;
using tsorcRevamp.Items.Accessories;
using tsorcRevamp.Items.Weapons.Magic;
using Microsoft.Xna.Framework.Graphics;
using Terraria.Localization;
using System.Collections.Generic;

namespace tsorcRevamp.NPCs.Friendly {
	[AutoloadHead]
	class ShamanElder : ModNPC {
		public override bool Autoload(ref string name) => true;
		public static List<string> Names = new List<string> {
			"Alo", "Dakota", "Esadowa", "Kai", "Koda", "Lonato", "Micah", "Taregan"
		};
        public override void SetStaticDefaults() {
			DisplayName.SetDefault("Shaman Elder");
			Main.npcFrameCount[npc.type] = 25;
			NPCID.Sets.ExtraFramesCount[npc.type] = 10;
			NPCID.Sets.AttackFrameCount[npc.type] = 5;
			NPCID.Sets.DangerDetectRange[npc.type] = 600;
			NPCID.Sets.AttackType[npc.type] = 2; //magic
			NPCID.Sets.AttackTime[npc.type] = 22;
			NPCID.Sets.AttackAverageChance[npc.type] = 30;
			NPCID.Sets.HatOffsetY[npc.type] = 4;
		}
		public override string TownNPCName() {
			string name = Names[Main.rand.Next(Names.Count)]; //pick a random name from the list
			return name;
		}

		public override void SetDefaults() {
			npc.townNPC = true;
			npc.friendly = true;
			npc.width = 18;
			npc.height = 40;
			npc.aiStyle = 7;
			npc.damage = 90;
			npc.defense = 15;
			npc.lifeMax = 1000;
			npc.HitSound = SoundID.NPCHit1;
			npc.DeathSound = SoundID.NPCDeath1;
			npc.knockBackResist = 0.5f;
			animationType = NPCID.Guide;
			
		}

		public override string GetChat() {
			WeightedRandom<string> chat = new WeightedRandom<string>();
			chat.Add("Man and animal once lived in harmony, until one day a particular tribe grew a sickness of the mind. This tribe, known as the Takers, came to dominate the world, exterminating all other ways of being...");
			chat.Add("Arapaho once told me that all plants are our brothers and sisters. They talk to us and if we listen, we can hear them.");
			chat.Add("I am an animist, like the indigenous tribe who once lived in these lands were, before they were wiped out by the Takers.");
			chat.Add("You must never forget " + Main.LocalPlayer.name + ", you are not separate from nature. You are one with the whole universe.");
			chat.Add("The world is not a pyramid, " + Main.LocalPlayer.name + ", nor is man the top of it. The world is a web, and every strand of the web is connected.");
			chat.Add("Civilized man has grown a great sickness of the mind -- thinks he is superior to all creation. Thinks the world was made for him!");
			chat.Add("Apache said it is better to have less thunder in the mouth and more lightning in the hand.");
			chat.Add("If you are able to defeat Attraidies, come and find me. I will have something for you...");
			chat.Add("Tuscarora once said they are not dead who live in the hearts they leave behind.");
			return chat;
		}
		public override void SetChatButtons(ref string button, ref string button2) {
			button = Language.GetTextValue("LegacyInterface.28");
            if (tsorcRevampWorld.SuperHardMode)
            {
				button2 = "Ask about The Abyss";
            }
		}

		public override void OnChatButtonClicked(bool firstButton, ref bool shop) {
			if (firstButton) {
				shop = true;
				return;
			}
		}

		public override void SetupShop(Chest shop, ref int nextSlot) {
			shop.item[nextSlot].SetDefaults(ModContent.ItemType<WandOfDarkness>());
			nextSlot++;
			shop.item[nextSlot].SetDefaults(ModContent.ItemType<WandOfFire>());
			nextSlot++;
			shop.item[nextSlot].SetDefaults(ModContent.ItemType<CosmicWatch>());
			nextSlot++;
			shop.item[nextSlot].SetDefaults(ItemID.Gel);
			nextSlot++;
            if (Main.hardMode) {
                shop.item[nextSlot].SetDefaults(ModContent.ItemType<CovetousSilverSerpentRing>());
                nextSlot++; 
            }
			if (tsorcRevampWorld.Slain.ContainsKey(ModContent.NPCType<Bosses.Okiku.FinalForm.Attraidies>()) || tsorcRevampWorld.SuperHardMode /*just in case*/) {
				shop.item[nextSlot].SetDefaults(ModContent.ItemType<CovenantOfArtorias>());
				nextSlot++;
				shop.item[nextSlot].SetDefaults(ModContent.ItemType<AbyssScroll>());
				nextSlot++;
				shop.item[nextSlot].SetDefaults(ModContent.ItemType<WitchkingScroll>());
				nextSlot++;
			}
		}

		public override void HitEffect(int hitDirection, double damage) {
			if (base.npc.life <= 0) {
				Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Shaman Elder Gore 1"));
				Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Tibian Female Knight Gore 2"));
				Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Tibian Female Knight Gore 2"));
				Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Tibian Female Knight Gore 3"));
				Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Tibian Female Knight Gore 3"));
			}
		}

		public override void TownNPCAttackStrength(ref int damage, ref float knockback) {
			
			damage = 50;
			knockback = 2f;
			if (Main.hardMode)
            {
				damage = 120;
				knockback = 5f;
            }
			if (tsorcRevampWorld.SuperHardMode)
			{
				damage = 250;
				knockback = 12f;
			}
		}

		public override void TownNPCAttackCooldown(ref int cooldown, ref int randExtraCooldown) {
			cooldown = 180;
			randExtraCooldown = 60;
		}

		public override void TownNPCAttackProj(ref int projType, ref int attackDelay) {
			projType = ModContent.ProjectileType<Projectiles.APShot>();
			attackDelay = 5;
		}

		public override void TownNPCAttackProjSpeed(ref float multiplier, ref float gravityCorrection, ref float randomOffset) {
			multiplier = 2f;
		}

		public override bool CanTownNPCSpawn(int numTownNPCs, int money) {
			foreach (Player p in Main.player) {
				if (!p.active) {
					continue;
				}
				if (p.statManaMax2 > 160) { //is this the best idea? not everyone is going to mindlessly eat every mana crystal they find
					return true;
				}
			}
			return false;
		}

		public override bool CanGoToStatue(bool toKingStatue) {
			return true;
		}
	}
}

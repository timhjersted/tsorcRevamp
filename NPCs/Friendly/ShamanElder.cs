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

        public override void PostAI()
        {
            if (ModContent.GetInstance<tsorcRevampConfig>().AdventureMode) {
                Vector2 shamanWarpPoint = new Vector2(108, 524.4f) * 16;
                if (tsorcRevampWorld.SuperHardMode) {
                    if (Vector2.Distance(npc.Center, shamanWarpPoint) > 200) {
                        npc.Center = shamanWarpPoint;
                    }

                    npc.velocity.X = 0;
                } 
            }
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
			chat.Add("Tuscarora once said they are not dead who live in the hearts they leave behind.");
			if (!tsorcRevampWorld.SuperHardMode && !tsorcRevampWorld.TheEnd)
			{
				chat.Add("If you are able to defeat Attraidies, come and find me. I will have something for you...");
			}
			return chat;
		}
		public override void SetChatButtons(ref string button, ref string button2) {
			button = Language.GetTextValue("LegacyInterface.28");
            if (tsorcRevampWorld.SuperHardMode)
            {
				if (chatState == 0)
				{
					button2 = "Ask about The Abyss";
				}
				if(chatState == 1 || chatState == 2 || chatState == 3)
                {
					button2 = "Continue...";
                }
            }
		}

		int chatState = 0;
		public override void OnChatButtonClicked(bool firstButton, ref bool shop) {
			if (firstButton) {
				shop = true;
				return;
			} else
            {
				if (chatState == 0)
				{
					Main.npcChatText = "To close the seal to the Abyss and ignite the Kiln of the" +
									"\nFirst Flame, you must defeat the 6 lords of The Abyss:" +
									"\n[c/ffbf00:Artorias], [c/00ffd4:Blight], [c/aa00ff:The Wyvern Mage Shadow], [c/fcff00:Chaos], and" +
									"\n[c/18ffe2:Seath the Scaleless]. With a lord soul from each of these" +
									"\nbeings you will be able to summon the final guardian - " +
									"\n[c/ff6618:Gwyn, Lord of Cinder].";
					chatState = 1;
					return;
				}
				if (chatState == 1)
				{
					Main.npcChatText = "To craft the summoning item for each " +
									"guardian, you will need to return to eight familiar places " +
									"and collect a unique item from an enemy you will find there: " +
									"[c/424bf5:The Western Ocean], [c/888888:The Underground], [c/b942f5:The Corruption], " +
									"\n[c/42f56c:The Jungle], [c/6642f5:The Dungeon], [c/eb4034:The Underworld], and [c/42f2f5:The Eastern Ocean].";
					chatState = 2;
					return;
				}
				if (chatState == 2)
				{
					Main.npcChatText = "Defeating [c/ffbf00:Artorias] and claiming his ring should be your priority. " +
								"Without it I fear you may stand little chance against these terrors... " +
								"\nTo find him, you must seek out the [c/383838:Witchking] and restore the strange ring he drops." +
								"\nHe will appear out of the Abyss at night, and more often deeper underground, especially in dungeons." +
								"\nThe most assured way to find him, however, is to enter the Abyss yourself using the Covanent of Artorias ring.";
					chatState = 3;
					return;
				}


				if (chatState == 3)
				{
					Main.npcChatText = "Both The [c/383838:Witchking] and [c/ffbf00:Artorias] are protected by dark spells." +
								"\nHowever, certain [c/cffffa:Phantoms] that roam the skies are rumored to carry blades of fierce magic. " +
								"\nSuch a blade may just be strong enough to shatter their protection...";

					chatState = 0;
				}
			}
		}

		public override void SetupShop(Chest shop, ref int nextSlot) {
			shop.item[nextSlot].SetDefaults(ModContent.ItemType<WandOfDarkness>());
			shop.item[nextSlot].shopCustomPrice = 120;
			shop.item[nextSlot].shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId;
			nextSlot++;
			shop.item[nextSlot].SetDefaults(ModContent.ItemType<WandOfFire>());
			shop.item[nextSlot].shopCustomPrice = 550;
			shop.item[nextSlot].shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId;
			nextSlot++;
			shop.item[nextSlot].SetDefaults(ModContent.ItemType<CosmicWatch>());
			shop.item[nextSlot].shopCustomPrice = 250;
			shop.item[nextSlot].shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId;
			nextSlot++;
			shop.item[nextSlot].SetDefaults(ModContent.ItemType<Items.ItemCrates.GelCrate>());
			shop.item[nextSlot].shopCustomPrice = 8;
			shop.item[nextSlot].shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId;
			nextSlot++;
            if (Main.hardMode) {
                shop.item[nextSlot].SetDefaults(ModContent.ItemType<CovetousSilverSerpentRing>());
				shop.item[nextSlot].shopCustomPrice = 3500; //17.5k DS, 20k to craft
				shop.item[nextSlot].shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId;
				nextSlot++; 
            }
			if (tsorcRevampWorld.Slain.ContainsKey(ModContent.NPCType<Bosses.Okiku.FinalForm.Attraidies>()) || tsorcRevampWorld.SuperHardMode /*just in case*/) {
				shop.item[nextSlot].SetDefaults(ModContent.ItemType<CovenantOfArtorias>());
				shop.item[nextSlot].shopCustomPrice = 13500;
				shop.item[nextSlot].shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId;
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
			projType = ModContent.ProjectileType<Projectiles.ShamanBolt>();
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

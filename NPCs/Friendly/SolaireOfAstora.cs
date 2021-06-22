using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Utilities;
using tsorcRevamp.Items.Weapons.Melee;
using tsorcRevamp.Items.Armors;
using Microsoft.Xna.Framework.Graphics;
using Terraria.Localization;

namespace tsorcRevamp.NPCs.Friendly {
	[AutoloadHead]
    class SolaireOfAstora : ModNPC {
		public override bool Autoload(ref string name) => false;
		public override void SetStaticDefaults() {
			DisplayName.SetDefault("Warrior of Sunlight");
			Main.npcFrameCount[npc.type] = 25;
			NPCID.Sets.ExtraFramesCount[npc.type] = 9;
			NPCID.Sets.AttackFrameCount[npc.type] = 4;
			NPCID.Sets.DangerDetectRange[npc.type] = 700;
			NPCID.Sets.AttackType[npc.type] = 3;
			NPCID.Sets.AttackTime[npc.type] = 90;
			NPCID.Sets.AttackAverageChance[npc.type] = 30;
			NPCID.Sets.HatOffsetY[npc.type] = 4;
		}
        public override string TownNPCName() {
			return "Solaire";
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
			chat.Add("Dark Souls possess great powers, but also great responsibilities.");
			chat.Add("Praise the sun!");
			chat.Add("Oh, hello there. I will stay behind, to gaze at the sun. The sun is a wondrous body. Like a magnificent father! If only I could be so grossly incandescent!");
			chat.Add("Of course, we are not the only ones engaged in this.");
			chat.Add("Would you like to buy some of my wares?");
			chat.Add("The way I see it, our fates appear to be intertwined. In a land brimming with Hollows, could that really be mere chance? So, what do you say? Why not help one another on this lonely journey?");
			chat.Add("Amazons and orcs are a real threat.");
			chat.Add("I'm far from home, but this isn't so bad.");
			if (tsorcRevampWorld.TheEnd) {
				chat.Add("You have done well, indeed you have. You've a strong arm, strong faith, and most importantly, a strong heart.", 1.5);
            }
			return chat;
		}
		public override void SetChatButtons(ref string button, ref string button2) {
			button = Language.GetTextValue("LegacyInterface.28");
		}

        public override void OnChatButtonClicked(bool firstButton, ref bool shop) {
			if (firstButton) {
				shop = true;
				return;
			}
		}

        public override void SetupShop(Chest shop, ref int nextSlot) {
			shop.item[nextSlot].SetDefaults(ModContent.ItemType<OldSabre>());
			nextSlot++;
			shop.item[nextSlot].SetDefaults(ModContent.ItemType<OldRapier>());
			nextSlot++;
			shop.item[nextSlot].SetDefaults(ModContent.ItemType<OldAxe>());
			nextSlot++;
			shop.item[nextSlot].SetDefaults(ModContent.ItemType<OldSword>());
			nextSlot++;
			shop.item[nextSlot].SetDefaults(ModContent.ItemType<OldMace>());
			nextSlot++;
			shop.item[nextSlot].SetDefaults(ModContent.ItemType<OldBroadsword>());
			nextSlot++;
			shop.item[nextSlot].SetDefaults(ModContent.ItemType<OldMorningStar>());
			nextSlot++;
			shop.item[nextSlot].SetDefaults(ModContent.ItemType<OldChainCoif>());
			nextSlot++;
			shop.item[nextSlot].SetDefaults(ModContent.ItemType<OldChainArmor>());
			nextSlot++;
			shop.item[nextSlot].SetDefaults(ModContent.ItemType<OldChainGreaves>());
			nextSlot++;
			shop.item[nextSlot].SetDefaults(ModContent.ItemType<AncientBrassHelmet>());
			nextSlot++;
			shop.item[nextSlot].SetDefaults(ModContent.ItemType<AncientBrassArmor>());
			nextSlot++;
			shop.item[nextSlot].SetDefaults(ModContent.ItemType<AncientBrassGreaves>());
			nextSlot++;
			shop.item[nextSlot].SetDefaults(ModContent.ItemType<CorruptedTooth>());
			nextSlot++;
			shop.item[nextSlot].SetDefaults(ModContent.ItemType<ForgottenMetalKnuckles>());
			nextSlot++;
			shop.item[nextSlot].SetDefaults(ModContent.ItemType<Items.Weapons.Ranged.ThrowingAxe>());
			nextSlot++;
			if (NPC.downedBoss1) {
				shop.item[nextSlot].SetDefaults(ModContent.ItemType<OldTwoHandedSword>());
				nextSlot++;
			}
			if (NPC.downedBoss1) {
				shop.item[nextSlot].SetDefaults(ModContent.ItemType<ForgottenLongSword>());
				nextSlot++;
				shop.item[nextSlot].SetDefaults(ModContent.ItemType<ForgottenKaiserKnuckles>());
				nextSlot++;
			}
			if (NPC.downedBoss1) {
				shop.item[nextSlot].SetDefaults(ModContent.ItemType<ForgottenKotetsu>());
				nextSlot++;
			}
		}

		public override void HitEffect(int hitDirection, double damage) {
			if (base.npc.life <= 0) {
				Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Tibian Female Knight Gore 1"));
				Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Tibian Female Knight Gore 2"));
				Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Tibian Female Knight Gore 2"));
				Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Tibian Female Knight Gore 3"));
				Gore.NewGore(npc.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), mod.GetGoreSlot("Gores/Tibian Female Knight Gore 3"));
			}
		}

		public override void TownNPCAttackStrength(ref int damage, ref float knockback) {
			damage = 20;
			knockback = 4f;
		}

		public override void TownNPCAttackCooldown(ref int cooldown, ref int randExtraCooldown) {
			cooldown = 30;
			randExtraCooldown = 30;
		}

        public override void DrawTownAttackSwing(ref Texture2D item, ref int itemSize, ref float scale, ref Vector2 offset) {
			item = Main.itemTexture[ModContent.ItemType<OldBroadsword>()];
			scale = 1f;
			itemSize = 44;
        }

        public override void TownNPCAttackSwing(ref int itemWidth, ref int itemHeight) {
			itemWidth = 44;
			itemHeight = 44;
		}

        public override bool CanTownNPCSpawn(int numTownNPCs, int money) {
            foreach (Player p in Main.player) {
				if (!p.active) {
					continue;
                }
				if (p.statDefense > 0) {
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

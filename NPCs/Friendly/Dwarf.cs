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
using static tsorcRevamp.Tiles.SweatyCyclopsForge;

namespace tsorcRevamp.NPCs.Friendly
{
    [AutoloadHead]
    class Dwarf : ModNPC
    {
        public override bool Autoload(ref string name) => true;

        public static List<string> Names = new List<string> {
            "Unfoli", "Nollin", "Duroin", "Jloin", "Grefinnyr", "Nionwelf", "Kloni", "Fini", "Ofur", "Ofi", "Bompbi"
        };

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Dwarf");
            Main.npcFrameCount[NPC.type] = 25;
            NPCID.Sets.ExtraFramesCount[NPC.type] = 9;
            NPCID.Sets.AttackFrameCount[NPC.type] = 4;
            NPCID.Sets.DangerDetectRange[NPC.type] = 60;
            NPCID.Sets.AttackType[NPC.type] = 3;
            NPCID.Sets.AttackTime[NPC.type] = 18;
            NPCID.Sets.AttackAverageChance[NPC.type] = 30;
            NPCID.Sets.HatOffsetY[NPC.type] = 4;
        }

        public override string TownNPCName()
        {
            string name = Names[Main.rand.Next(Names.Count)]; //pick a random name from the list
            return name;
        }

        public override void SetDefaults()
        {
            NPC.townNPC = true;
            NPC.friendly = true;
            NPC.width = 18;
            NPC.height = 40;
            NPC.aiStyle = 7;
            NPC.damage = 50;
            NPC.defense = 45;
            NPC.lifeMax = 300;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.knockBackResist = 0.5f;
            animationType = NPCID.DyeTrader;
        }

        #region Town Spawn
        public override bool CanTownNPCSpawn(int numTownNPCs, int money)
        {
            foreach (Player p in Main.player)
            {
                if (!p.active)
                {
                    continue;
                }
                if (p.statDefense > 8)
                {
                    return true;
                }
            }
            return false;
        }
        #endregion

        #region Chat
        public override string GetChat()
        {
            WeightedRandom<string> chat = new WeightedRandom<string>();
            chat.Add("I've got some contracts for sale.");
            chat.Add("How are you?");
            chat.Add("I'm thirsty...");
            chat.Add("After you defeat the Eater of Worlds, I'll have cheap sticky bombs for sale!");

            if (!tsorcRevampWorld.Slain.ContainsKey(ModContent.NPCType<NPCs.Bosses.TheSorrow>()) && !NPC.downedMechBoss1)
            {
                chat.Add("If you're able to defeat The Sorrow or The Destroyer, I'll have more things to sell later...");
            }
            if (!tsorcRevampWorld.Slain.ContainsKey(ModContent.NPCType<NPCs.Bosses.TheSorrow>()) && NPC.downedMechBoss1)
            {
                chat.Add("If you're able to defeat The Sorrow, I'll have more things to sell later...");
            }
            if (tsorcRevampWorld.Slain.ContainsKey(ModContent.NPCType<NPCs.Bosses.TheSorrow>()) && !NPC.downedMechBoss1)
            {
                chat.Add("If you're able to defeat The Destroyer, I'll have more things to sell later...");
            }
            return chat;
        }
        #endregion

        public override void SetChatButtons(ref string button, ref string button2)
        {
            button = Language.GetTextValue("LegacyInterface.28");
        }

        public override void OnChatButtonClicked(bool firstButton, ref bool shop)
        {            
            shop = true;
            return;            
        }

        #region Setup Shop
        public override void SetupShop(Chest chest, ref int index)
        {
            chest.item[index].SetDefaults(ModContent.ItemType<DwarvenContract>());
            chest.item[index].shopCustomPrice = 200;
            chest.item[index].shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId;
            index++;
            chest.item[index].SetDefaults(ModContent.ItemType<SweatyCyclopsForgeItem>());
            chest.item[index].shopCustomPrice = 10;
            chest.item[index].shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId;
            index++;
            chest.item[index].SetDefaults(ItemID.Flipper);
            chest.item[index].shopCustomPrice = 200;
            chest.item[index].shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId;
            index++;
            chest.item[index].SetDefaults(ItemID.FairyBell);
            chest.item[index].shopCustomPrice = 1200;
            chest.item[index].shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId;
            index++;
            chest.item[index].SetDefaults(ItemID.Silk);
            chest.item[index].shopCustomPrice = 5;
            chest.item[index].shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId;
            index++;
            chest.item[index].SetDefaults(ItemID.LesserHealingPotion);
            chest.item[index].shopCustomPrice = 6;
            chest.item[index].shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId;
            index++;

            if (NPC.downedBoss2)
            {
                chest.item[index].SetDefaults(ItemID.DivingHelmet);
                chest.item[index].shopCustomPrice = 100;
                chest.item[index].shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId;
                index++;
                chest.item[index].SetDefaults(ItemID.StickyBomb);
                chest.item[index].shopCustomPrice = 10;
                chest.item[index].shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId;
                index++;
            }

            if (tsorcRevampWorld.Slain.ContainsKey(ModContent.NPCType<NPCs.Bosses.TheSorrow>()))
            {
                chest.item[index].SetDefaults(ModContent.ItemType<Items.Weapons.Melee.ForgottenIceBrand>());
                chest.item[index].shopCustomPrice = 4000;
                chest.item[index].shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId;
                index++;
                chest.item[index].SetDefaults(ModContent.ItemType<Items.Weapons.Melee.ForgottenMurakumo>());
                chest.item[index].shopCustomPrice = 3000;
                chest.item[index].shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId;
                index++;
                chest.item[index].SetDefaults(ModContent.ItemType<Items.Weapons.Melee.ForgottenPearlSpear>());
                chest.item[index].shopCustomPrice = 5000;
                chest.item[index].shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId;
                index++;
            }

            if (NPC.downedMechBoss1)
            {
                chest.item[index].SetDefaults(ModContent.ItemType<Items.Weapons.Melee.ForgottenPoisonAxe>());
                chest.item[index].shopCustomPrice = 6000;
                chest.item[index].shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId;
                index++;
                chest.item[index].SetDefaults(ModContent.ItemType<Items.Weapons.Melee.ForgottenSwordbreaker>());
                chest.item[index].shopCustomPrice = 7000;
                chest.item[index].shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId;
                index++;
                chest.item[index].SetDefaults(ModContent.ItemType<Items.Weapons.Melee.ForgottenImpHalberd>());
                chest.item[index].shopCustomPrice = 6000;
                chest.item[index].shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId;
                index++;
            }
        }
        #endregion

        public override void TownNPCAttackStrength(ref int damage, ref float knockback)
        {
            damage = 25;
            knockback = 4f;
        }

        public override void TownNPCAttackCooldown(ref int cooldown, ref int randExtraCooldown)
        {
            cooldown = 30;
            randExtraCooldown = 30;
        }

        public override void DrawTownAttackSwing(ref Texture2D item, ref int itemSize, ref float scale, ref Vector2 offset)
        {
            item = Main.itemTexture[ModContent.ItemType<Items.Weapons.Melee.ForgottenAxe>()];
            itemSize = 36;
        }

        public override void TownNPCAttackSwing(ref int itemWidth, ref int itemHeight)
        {
            itemWidth = 36;
            itemHeight = 30;
        }

        public override bool CanGoToStatue(bool toKingStatue)
        {
            return true;
        }
    }
}
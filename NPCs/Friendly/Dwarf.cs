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
            Main.npcFrameCount[npc.type] = 25;
            NPCID.Sets.ExtraFramesCount[npc.type] = 9;
            NPCID.Sets.AttackFrameCount[npc.type] = 4;
            NPCID.Sets.DangerDetectRange[npc.type] = 60;
            NPCID.Sets.AttackType[npc.type] = 3;
            NPCID.Sets.AttackTime[npc.type] = 18;
            NPCID.Sets.AttackAverageChance[npc.type] = 30;
            NPCID.Sets.HatOffsetY[npc.type] = 4;
        }

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
            npc.defense = 45;
            npc.lifeMax = 300;
            npc.HitSound = SoundID.NPCHit1;
            npc.DeathSound = SoundID.NPCDeath1;
            npc.knockBackResist = 0.5f;
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
            chat.Add("I've got some contracts for sell.");
            chat.Add("How are you?");
            chat.Add("I'm thristy...");
            if (NPC.downedBoss2)
            {
                chat.Add("After you defeat the Eater of Worlds, I'll have cheap sticky bombs for sale!");
            }
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
            index++;
            chest.item[index].SetDefaults(ItemID.Flipper);
            index++;
            chest.item[index].SetDefaults(ItemID.FairyBell);
            index++;
            chest.item[index].SetDefaults(ItemID.Silk);
            index++;
            chest.item[index].SetDefaults(ItemID.LesserHealingPotion);
            index++;

            if (NPC.downedBoss2)
            {
                chest.item[index].SetDefaults(ItemID.DivingHelmet);
                index++;
                chest.item[index].SetDefaults(ItemID.StickyBomb);
                index++;
            }

            if (tsorcRevampWorld.Slain.ContainsKey(ModContent.NPCType<NPCs.Bosses.TheSorrow>()))
            {
                chest.item[index].SetDefaults(ModContent.ItemType<Items.Weapons.Melee.ForgottenIceBrand>());
                index++;
                chest.item[index].SetDefaults(ModContent.ItemType<Items.Weapons.Melee.ForgottenMurakumo>());
                index++;
                chest.item[index].SetDefaults(ModContent.ItemType<Items.Weapons.Melee.ForgottenPearlSpear>());
                index++;
            }

            if (NPC.downedMechBoss1)
            {
                chest.item[index].SetDefaults(ModContent.ItemType<Items.Weapons.Melee.ForgottenPoisonAxe>());
                index++;
                chest.item[index].SetDefaults(ModContent.ItemType<Items.Weapons.Melee.ForgottenSwordbreaker>());
                index++;
                chest.item[index].SetDefaults(ModContent.ItemType<Items.Weapons.Melee.ForgottenImpHalberd>());
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
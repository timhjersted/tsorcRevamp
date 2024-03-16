using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;
using Terraria.Utilities;
using tsorcRevamp.Items.Tools;

namespace tsorcRevamp.NPCs.Friendly
{
    [AutoloadHead]
    class Dwarf : ModNPC
    {

        public static List<string> Names = new List<string> {
            Language.GetTextValue("Mods.tsorcRevamp.NPCs.Dwarf.Name1"), Language.GetTextValue("Mods.tsorcRevamp.NPCs.Dwarf.Name2"), Language.GetTextValue("Mods.tsorcRevamp.NPCs.Dwarf.Name3"), Language.GetTextValue("Mods.tsorcRevamp.NPCs.Dwarf.Name4"),
            Language.GetTextValue("Mods.tsorcRevamp.NPCs.Dwarf.Name5"), Language.GetTextValue("Mods.tsorcRevamp.NPCs.Dwarf.Name6"), Language.GetTextValue("Mods.tsorcRevamp.NPCs.Dwarf.Name7"), Language.GetTextValue("Mods.tsorcRevamp.NPCs.Dwarf.Name8"),
            Language.GetTextValue("Mods.tsorcRevamp.NPCs.Dwarf.Name9"), Language.GetTextValue("Mods.tsorcRevamp.NPCs.Dwarf.Name10"), Language.GetTextValue("Mods.tsorcRevamp.NPCs.Dwarf.Name11")
        };

        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Dwarf");
            Main.npcFrameCount[NPC.type] = 25;
            NPCID.Sets.ExtraFramesCount[NPC.type] = 9;
            NPCID.Sets.AttackFrameCount[NPC.type] = 4;
            NPCID.Sets.DangerDetectRange[NPC.type] = 60;
            NPCID.Sets.AttackType[NPC.type] = 3;
            NPCID.Sets.AttackTime[NPC.type] = 18;
            NPCID.Sets.AttackAverageChance[NPC.type] = 30;
            NPCID.Sets.HatOffsetY[NPC.type] = 4;
        }
        public override List<string> SetNPCNameList()
        {
            return Names;
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
            AnimationType = NPCID.DyeTrader;
        }

        #region Town Spawn
        public override bool CanTownNPCSpawn(int numTownNPCs)/* tModPorter Suggestion: Copy the implementation of NPC.SpawnAllowed_Merchant in vanilla if you to count money, and be sure to set a flag when unlocked, so you don't count every tick. */
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
            chat.Add(Language.GetTextValue("Mods.tsorcRevamp.NPCs.Dwarf.Quote1"));
            chat.Add(Language.GetTextValue("Mods.tsorcRevamp.NPCs.Dwarf.Quote2"));
            chat.Add(Language.GetTextValue("Mods.tsorcRevamp.NPCs.Dwarf.Quote3"));
            chat.Add(Language.GetTextValue("Mods.tsorcRevamp.NPCs.Dwarf.Quote4"));

            if (!tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<NPCs.Bosses.TheSorrow>())) && !NPC.downedMechBoss1)
            {
                chat.Add(Language.GetTextValue("Mods.tsorcRevamp.NPCs.Dwarf.Quote5"));
            }
            if (!tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<NPCs.Bosses.TheSorrow>())) && NPC.downedMechBoss1)
            {
                chat.Add(Language.GetTextValue("Mods.tsorcRevamp.NPCs.Dwarf.Quote6"));
            }
            if (tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<NPCs.Bosses.TheSorrow>())) && !NPC.downedMechBoss1)
            {
                chat.Add(Language.GetTextValue("Mods.tsorcRevamp.NPCs.Dwarf.Quote7"));
            }
            return chat;
        }
        #endregion

        public override void SetChatButtons(ref string button, ref string button2)
        {
            button = Language.GetTextValue("LegacyInterface.28");
        }

        public override void OnChatButtonClicked(bool firstButton, ref string shopName)
        {
            shopName = "Shop";
            return;
        }

        #region Setup Shop
        public override void AddShops()
        {
            NPCShop shop = new(NPC.type);

            shop.Add(new Item(ModContent.ItemType<DwarvenContract>())
            {
                shopCustomPrice = 100,
                shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
            });

            shop.Add(new Item(ItemID.Flipper)
            {
                shopCustomPrice = 100,
                shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
            });

            shop.Add(new Item(ItemID.FairyBell)
            {
                shopCustomPrice = 100,
                shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
            });

            shop.Add(new Item(ItemID.Silk)
            {
                shopCustomPrice = 1,
                shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
            });

            shop.Add(new Item(ItemID.HealingPotion)
            {
                shopCustomPrice = 1,
                shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
            });

            shop.Add(new Item(ModContent.ItemType<Items.Armors.StuddedLeatherHelmet>())
            {
                shopCustomPrice = 25,
                shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
            });

            shop.Add(new Item(ModContent.ItemType<Items.Armors.StuddedLeatherArmor>())
            {
                shopCustomPrice = 40,
                shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
            });

            shop.Add(new Item(ModContent.ItemType<Items.Armors.StuddedLeatherGreaves>())
            {
                shopCustomPrice = 33,
                shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
            });



            shop.Add(new Item(ItemID.DivingHelmet)
            {
                shopCustomPrice = 100,
                shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
            }, Condition.DownedEowOrBoc);

            shop.Add(new Item(ItemID.StickyBomb)
            {
                shopCustomPrice = 1,
                shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
            }, Condition.DownedEowOrBoc);

            shop.Add(new Item(ItemID.TungstenBar)
            {
                shopCustomPrice = 5,
                shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
            }, Condition.DownedEowOrBoc);



            shop.Add(new Item(ModContent.ItemType<Items.Weapons.Melee.Broadswords.ForgottenIceBrand>())
            {
                shopCustomPrice = 4000,
                shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
            }, new Condition(Language.GetTextValue("Mods.tsorcRevamp.Conditions.SorrowDowned"), () => tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<NPCs.Bosses.TheSorrow>()))));



            shop.Add(new Item(ModContent.ItemType<Items.Weapons.Melee.Axes.ForgottenPoisonAxe>())
            {
                shopCustomPrice = 6000,
                shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
            }, Condition.DownedMechBossAny);

            shop.Add(new Item(ModContent.ItemType<Items.Weapons.Melee.Broadswords.ForgottenSwordbreaker>())
            {
                shopCustomPrice = 6000,
                shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
            }, Condition.DownedMechBossAny);

            shop.Add(new Item(ModContent.ItemType<Items.Weapons.Summon.ForgottenImpHalberd>())
            {
                shopCustomPrice = 6000,
                shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
            }, new Condition(Language.GetTextValue("Mods.tsorcRevamp.Conditions.HunterDowned"), () => tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<NPCs.Bosses.TheHunter>()))));

            shop.Register();
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

        public override void DrawTownAttackSwing(ref Texture2D item, ref Rectangle itemFrame, ref int itemSize, ref float scale, ref Vector2 offset)
        {
            item = (Texture2D)TextureAssets.Item[ModContent.ItemType<Items.Weapons.Melee.Hammers.AncientWarhammer>()];
            itemSize = 38;
        }

        public override void TownNPCAttackSwing(ref int itemWidth, ref int itemHeight)
        {
            itemWidth = 38;
            itemHeight = 38;
        }

        public override bool CanGoToStatue(bool toKingStatue)
        {
            return true;
        }
    }
}
using Humanizer;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;
using tsorcRevamp.Items.Accessories;
using tsorcRevamp.Items.Accessories.Defensive;
using tsorcRevamp.Items.Accessories.Magic;
using tsorcRevamp.Items.Armors.Magic;
using tsorcRevamp.Items.Weapons.Summon.Whips;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.Items.VanillaItems
{
    class TooltipHelper : GlobalItem
    {

        //this method adds potentially multiple tooltip lines to the end of an item's tooltip stack 
        public static void SimpleModTooltip(Mod mod, Item item, List<TooltipLine> tooltips, int ItemID, string TipToAdd1, string TipToAdd2 = null)
        {
            if (item.type == ItemID)
            {
                int ttindex = tooltips.FindLastIndex(t => t.Mod == "Terraria" && t.Name != "ItemName" && t.Name != "Social" && t.Name != "SocialDesc" && !t.Name.Contains("Prefix"));
                if (ttindex != -1)
                {// if we find one
                    //insert the extra tooltip line
                    tooltips.Insert(ttindex + 1, new TooltipLine(mod, "", TipToAdd1));
                    if (TipToAdd2 != null)
                    {
                        tooltips.Insert(ttindex + 2, new TooltipLine(mod, "", TipToAdd2));
                    }
                }
            }
        }

        public static void SimpleGlobalModTooltip(Mod mod, List<TooltipLine> tooltips, string TipToAdd1, string TipToAdd2 = null) //Same but not linked to a specific item.
        {
            int ttindex = tooltips.FindLastIndex(t => t.Mod == "Terraria"); //find the last tooltip line
            if (ttindex != -1)
            {// if we find one
             //insert the extra tooltip line
                tooltips.Insert(ttindex + 1, new TooltipLine(mod, "", TipToAdd1));
                if (TipToAdd2 != null)
                {
                    tooltips.Insert(ttindex + 2, new TooltipLine(mod, "", TipToAdd2));
                }
            }

        }

        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            //SimpleModTooltip(mod, item, tooltips, ItemID., "a");
            //SimpleModTooltip(mod, item, tooltips, ItemID., "a", "b");
            //SimpleModTooltip(mod, item, tooltips, ItemID.FlaskofFire, "Adds 10% melee damage");  don't do this. flask of fire's tooltip goes at a specific index, not the end
            
            if (!tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<NPCs.Bosses.TheHunter>())))
            {
                SimpleModTooltip(Mod, item, tooltips, ItemID.AngelWings, Language.GetTextValue("Mods.tsorcRevamp.Items.VanillaItems.AngelWings"));
            }

            SimpleModTooltip(Mod, item, tooltips, ItemID.AdamantiteDrill, Language.GetTextValue("Mods.tsorcRevamp.Items.VanillaItems.AdamantiteDrill"));            
            SimpleModTooltip(Mod, item, tooltips, ItemID.BandofRegeneration, Language.GetTextValue("Mods.tsorcRevamp.Items.VanillaItems.BandOfTegeneration"));
            SimpleModTooltip(Mod, item, tooltips, ItemID.BandofStarpower, Language.GetTextValue("Mods.tsorcRevamp.Items.VanillaItems.BandOfStarpower"));
            SimpleModTooltip(Mod, item, tooltips, ItemID.CobaltDrill, Language.GetTextValue("Mods.tsorcRevamp.Items.VanillaItems.CobaltDrill"));
            SimpleModTooltip(Mod, item, tooltips, ItemID.DivingHelmet, Language.GetTextValue("Mods.tsorcRevamp.Items.VanillaItems.DivingHelmet"));
            SimpleModTooltip(Mod, item, tooltips, ItemID.MechanicalSkull, Language.GetTextValue("Mods.tsorcRevamp.Items.VanillaItems.MechanicalSkull"));
            SimpleModTooltip(Mod, item, tooltips, ItemID.MechanicalWorm, Language.GetTextValue("Mods.tsorcRevamp.Items.VanillaItems.MechanicalWorm"));
            SimpleModTooltip(Mod, item, tooltips, ItemID.MoltenPickaxe, Language.GetTextValue("Mods.tsorcRevamp.Items.VanillaItems.MoltenPickaxe"));
            SimpleModTooltip(Mod, item, tooltips, ItemID.SilverWatch, Language.GetTextValue("Mods.tsorcRevamp.Items.VanillaItems.SilverWatch"));
            SimpleModTooltip(Mod, item, tooltips, ItemID.StickyBomb, Language.GetTextValue("Mods.tsorcRevamp.Items.VanillaItems.StickyBomb"));
            SimpleModTooltip(Mod, item, tooltips, ItemID.WormFood, Language.GetTextValue("Mods.tsorcRevamp.Items.VanillaItems.WormFood"));
            SimpleModTooltip(Mod, item, tooltips, ItemID.CopperAxe, Language.GetTextValue("Mods.tsorcRevamp.Items.VanillaItems.CopperAxe"));
            SimpleModTooltip(Mod, item, tooltips, ItemID.Diamond, Language.GetTextValue("Mods.tsorcRevamp.Items.VanillaItems.Diamond"));
            SimpleModTooltip(Mod, item, tooltips, ItemID.IronOre, Language.GetTextValue("Mods.tsorcRevamp.Items.VanillaItems.IronOre"));
            SimpleModTooltip(Mod, item, tooltips, ItemID.MagicMirror, Language.GetTextValue("Mods.tsorcRevamp.Items.VanillaItems.RecallItem"));
            SimpleModTooltip(Mod, item, tooltips, ItemID.RecallPotion, Language.GetTextValue("Mods.tsorcRevamp.Items.VanillaItems.RecallItem"));
            SimpleModTooltip(Mod, item, tooltips, ItemID.LargeAmethyst, Language.GetTextValue("Mods.tsorcRevamp.Items.VanillaItems.LargeAmethyst"));
            SimpleModTooltip(Mod, item, tooltips, ItemID.LargeSapphire, Language.GetTextValue("Mods.tsorcRevamp.Items.VanillaItems.LargeSapphire"));
            SimpleModTooltip(Mod, item, tooltips, ItemID.ApprenticeStaffT3, Language.GetTextValue("Mods.tsorcRevamp.Items.VanillaItems.VulnerabilityHex"));
            SimpleModTooltip(Mod, item, tooltips, ItemID.MonkStaffT3, Language.GetTextValue("Mods.tsorcRevamp.Items.VanillaItems.VulnerabilityHex"));
            SimpleModTooltip(Mod, item, tooltips, ItemID.DD2SquireBetsySword, Language.GetTextValue("Mods.tsorcRevamp.Items.VanillaItems.VulnerabilityHex"));
            SimpleModTooltip(Mod, item, tooltips, ItemID.DD2BetsyBow, Language.GetTextValue("Mods.tsorcRevamp.Items.VanillaItems.VulnerabilityHex"));
            SimpleModTooltip(Mod, item, tooltips, ItemID.StarinaBottle, Language.GetTextValue("Mods.tsorcRevamp.Items.VanillaItems.StarInABottle"));
            SimpleModTooltip(Mod, item, tooltips, ItemID.ManaRegenerationBand, Language.GetTextValue("Mods.tsorcRevamp.Items.VanillaItems.ManaRegenerationBand"));


            Player player = Main.LocalPlayer;

            if (ItemID.Sets.StaffMinionSlotsRequired[item.type] > 1f)
            {
                SimpleGlobalModTooltip(Mod, tooltips, LaUtils.GetTextValue("CommonItemTooltip.Summon.SlotsRequired", ItemID.Sets.StaffMinionSlotsRequired[item.type]));
            }

            if (ItemID.Sets.StaffMinionSlotsRequired[item.type] < 1f)
            {
                SimpleGlobalModTooltip(Mod, tooltips, LaUtils.GetTextValue("CommonItemTooltip.Summon.PercentOfASlot", ItemID.Sets.StaffMinionSlotsRequired[item.type] * 100f));
            }

            if (player.GetModPlayer<tsorcRevampPlayer>().BearerOfTheCurse && player.whoAmI == Main.myPlayer && item.healLife > 0)
            {
                SimpleGlobalModTooltip(Mod, tooltips, Language.GetTextValue("Mods.tsorcRevamp.Items.VanillaItems.BotCHealingItems"));
            }

            if (player.GetModPlayer<tsorcRevampPlayer>().BearerOfTheCurse && player.whoAmI == Main.myPlayer && item.buffType == BuffID.WellFed)
            {
                SimpleGlobalModTooltip(Mod, tooltips, Language.GetTextValue("Mods.tsorcRevamp.Items.VanillaItems.WellFedItemsBotC").FormatWith(MinorEdits.BotCWellFedStaminaRegen));
            }

            if (player.GetModPlayer<tsorcRevampPlayer>().BearerOfTheCurse && player.whoAmI == Main.myPlayer && item.buffType == BuffID.WellFed2)
            {
                SimpleGlobalModTooltip(Mod, tooltips, Language.GetTextValue("Mods.tsorcRevamp.Items.VanillaItems.WellFedItemsBotC").FormatWith(MinorEdits.BotCPlentySatisfiedStaminaRegen));
            }

            if (player.GetModPlayer<tsorcRevampPlayer>().BearerOfTheCurse && player.whoAmI == Main.myPlayer && item.buffType == BuffID.WellFed3)
            {
                SimpleGlobalModTooltip(Mod, tooltips, Language.GetTextValue("Mods.tsorcRevamp.Items.VanillaItems.WellFedItemsBotC").FormatWith(MinorEdits.BotCExquisitelyStuffedStaminaRegen));
            }

            /*if (player.GetModPlayer<tsorcRevampPlayer>().BearerOfTheCurse && player.whoAmI == Main.myPlayer && item.type == ModContent.ItemType<CrystalNunchaku>())
            {
                SimpleGlobalModTooltip(Mod, tooltips, "May make the [c/6d8827:Bearer of the Curse] more vulnerable");
            }*/

            if (player.GetModPlayer<tsorcRevampPlayer>().BearerOfTheCurse && player.whoAmI == Main.myPlayer && item.type == ModContent.ItemType<SearingLash>())
            {
                SimpleGlobalModTooltip(Mod, tooltips, Language.GetTextValue("Mods.tsorcRevamp.Items.SearingLash.BotCTooltip"));
            }

            if (player.GetModPlayer<tsorcRevampPlayer>().BearerOfTheCurse && player.whoAmI == Main.myPlayer && item.type == ModContent.ItemType<NightsCracker>())
            {
                SimpleGlobalModTooltip(Mod, tooltips, Language.GetTextValue("Mods.tsorcRevamp.Items.NightsCracker.BotCTooltip"));
            }

            if (player.GetModPlayer<tsorcRevampPlayer>().BearerOfTheCurse && player.whoAmI == Main.myPlayer && item.type == ModContent.ItemType<TerraFall>())
            {
                SimpleGlobalModTooltip(Mod, tooltips, Language.GetTextValue("Mods.tsorcRevamp.Items.TerraFall.BotCTooltip"));
            }

            if (player.GetModPlayer<tsorcRevampPlayer>().BearerOfTheCurse && player.whoAmI == Main.myPlayer && item.type == ItemID.ManaRegenerationPotion)
            {
                SimpleGlobalModTooltip(Mod, tooltips, Language.GetTextValue("Mods.tsorcRevamp.Items.VanillaItems.ManaRegenerationPotionBotC").FormatWith(tsorcRevampCeruleanPlayer.ManaRegenPotRestorationTimerBonus));
            }

            if (player.GetModPlayer<tsorcRevampPlayer>().BearerOfTheCurse && player.whoAmI == Main.myPlayer && item.type == ItemID.CelestialMagnet)
            {
                SimpleGlobalModTooltip(Mod, tooltips, Language.GetTextValue("Mods.tsorcRevamp.Items.VanillaItems.CelestialMagnetBotC"));
            }

            if (player.GetModPlayer<tsorcRevampPlayer>().BearerOfTheCurse && player.whoAmI == Main.myPlayer && item.type == ItemID.ManaCloak)
            {
                SimpleGlobalModTooltip(Mod, tooltips, Language.GetTextValue("Mods.tsorcRevamp.Items.VanillaItems.CelestialMagnetBotC"));
            }

            if (player.GetModPlayer<tsorcRevampPlayer>().BearerOfTheCurse && player.whoAmI == Main.myPlayer && item.type == ItemID.CelestialEmblem)
            {
                SimpleGlobalModTooltip(Mod, tooltips, Language.GetTextValue("Mods.tsorcRevamp.Items.VanillaItems.CelestialMagnetBotC"));
            }

            if (player.GetModPlayer<tsorcRevampPlayer>().BearerOfTheCurse && player.whoAmI == Main.myPlayer && item.type == ItemID.MagicCuffs)
            {
                SimpleGlobalModTooltip(Mod, tooltips, Language.GetTextValue("Mods.tsorcRevamp.Items.VanillaItems.MagicCuffsBotC"));
            }

            if (player.GetModPlayer<tsorcRevampPlayer>().BearerOfTheCurse && player.whoAmI == Main.myPlayer && item.type == ItemID.CelestialCuffs)
            {
                SimpleGlobalModTooltip(Mod, tooltips, Language.GetTextValue("Mods.tsorcRevamp.Items.VanillaItems.CelestialMagnetBotC") + "\n" + Language.GetTextValue("Mods.tsorcRevamp.Items.VanillaItems.MagicCuffsBotC"));
            }

            if (player.GetModPlayer<tsorcRevampPlayer>().BearerOfTheCurse && player.whoAmI == Main.myPlayer && item.type == ModContent.ItemType<CelestialCloak>())
            {
                SimpleGlobalModTooltip(Mod, tooltips, Language.GetTextValue("Mods.tsorcRevamp.Items.VanillaItems.CelestialMagnetBotC") + "\n" + Language.GetTextValue("Mods.tsorcRevamp.Items.VanillaItems.MagicCuffsBotC"));
            }

        }
    }
}

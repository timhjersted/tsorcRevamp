using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;
using tsorcRevamp.Items.Accessories;
using tsorcRevamp.Items.Accessories.Defensive;
using tsorcRevamp.Items.Accessories.Magic;
using tsorcRevamp.Items.Armors.Magic;
using tsorcRevamp.Items.Weapons.Summon.Whips;

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
                SimpleModTooltip(Mod, item, tooltips, ItemID.AngelWings, "[c/ffbf00:You will discover these in time...]", "Can be upgraded with Supersonic Boots");
            }

            SimpleModTooltip(Mod, item, tooltips, ItemID.AdamantiteBreastplate, "Set can be upgraded in 3 ways with Dark Souls");
            SimpleModTooltip(Mod, item, tooltips, ItemID.AdamantiteDrill, "Use this to open the Adamantite gates in the", "Corruption Temple to the west of the village");            
            SimpleModTooltip(Mod, item, tooltips, ItemID.BandofRegeneration, "Can be upgraded with the Band of Starpower and Dark Souls");
            SimpleModTooltip(Mod, item, tooltips, ItemID.BandofStarpower, "Can be upgraded with the Band of Regeneration and Dark Souls");
            SimpleModTooltip(Mod, item, tooltips, ItemID.CobaltDrill, "[c/ffbf00:Use this to gain entry to the Wyvern Mage's]", "[c/ffbf00:Fortress above the hallowed caverns to the East]");
            SimpleModTooltip(Mod, item, tooltips, ItemID.DivingHelmet, "Can be placed in an accessory slot or in your head slot.");
            SimpleModTooltip(Mod, item, tooltips, ItemID.GoldHelmet, "Can be upgraded with Dark Souls");
            SimpleModTooltip(Mod, item, tooltips, ItemID.ManaCrystal, "Can be used with Dark Souls to create a Mana Bomb");
            SimpleModTooltip(Mod, item, tooltips, ItemID.MechanicalSkull, "Item is non-consumable.");
            SimpleModTooltip(Mod, item, tooltips, ItemID.MechanicalWorm, "It's heavier than you expected.\nYou get the feeling a way to stay in the air may be key...\nItem is non-consumable.");
            SimpleModTooltip(Mod, item, tooltips, ItemID.MeteorSuit, "Can be augmented with Souls of Light");
            SimpleModTooltip(Mod, item, tooltips, ItemID.MoltenFury, "Can be upgraded with 1 Soul of Sight and Dark Souls");
            SimpleModTooltip(Mod, item, tooltips, ItemID.MoltenPickaxe, "[c/ffbf00:Can be used to open gates made of Cobalt Ore]", "Miakoda reminds you of the Cobalt Gate to the East of Obsidian's Volcano");
            SimpleModTooltip(Mod, item, tooltips, ItemID.MythrilChainmail, "This armor set can be upgraded with Dark Souls");
            SimpleModTooltip(Mod, item, tooltips, ItemID.ShadowHelmet, "Can be upgraded with Dark Souls");
            SimpleModTooltip(Mod, item, tooltips, ItemID.SilverWatch, "[c/ffbf00:Can be upgraded with Dark Souls to change day to night.]");
            SimpleModTooltip(Mod, item, tooltips, ItemID.StickyBomb, "More fun to use than a pickaxe!");
            SimpleModTooltip(Mod, item, tooltips, ItemID.WireCutter, "[c/ffbf00:Do not use this!]");
            SimpleModTooltip(Mod, item, tooltips, ItemID.WormFood, "Item is not consumed so you can retry the fight until victory");
            SimpleModTooltip(Mod, item, tooltips, ItemID.Wrench, "Do not use this!");
            SimpleModTooltip(Mod, item, tooltips, ItemID.BlueWrench, "Do not use this!");
            SimpleModTooltip(Mod, item, tooltips, ItemID.GreenWrench, "Do not use this!");
            SimpleModTooltip(Mod, item, tooltips, ItemID.YellowWrench, "Do not use this!");
            SimpleModTooltip(Mod, item, tooltips, ItemID.MulticolorWrench, "Do not use this!");
            SimpleModTooltip(Mod, item, tooltips, ItemID.CopperAxe, "All axes do 2x damage to woody enemies");
            SimpleModTooltip(Mod, item, tooltips, ItemID.DemonBow, "Can be upgraded with Dark Souls", "and 10 Shadow Scales or 10 Tissue Samples");
            SimpleModTooltip(Mod, item, tooltips, ItemID.Diamond, "Vital ingredient in the crafting of a very powerful potion");
            SimpleModTooltip(Mod, item, tooltips, ItemID.IronOre, "Perhaps you can use these for making special arrows..?");
            SimpleModTooltip(Mod, item, tooltips, ItemID.FeralClaws, "Can be upgraded with Dark Souls, an Aglet, and an Anklet of the Wind");
            SimpleModTooltip(Mod, item, tooltips, ItemID.Revolver, "Can be upgraded with Dark Souls and 10 Souls of Light or Dark");
            SimpleModTooltip(Mod, item, tooltips, ItemID.MagicMirror, "Can not be used while a boss is alive!");
            SimpleModTooltip(Mod, item, tooltips, ItemID.RecallPotion, "Can not be used while a boss is alive!");
            SimpleModTooltip(Mod, item, tooltips, ItemID.LargeAmethyst, "[c/ffbf00:A symbol resembling a large pyramid with the moon to its right side is carved into it]");
            SimpleModTooltip(Mod, item, tooltips, ItemID.LargeSapphire, "[c/ffbf00:A symbol resembling a snow-capped mountain near a fortress is carved into it]");
            SimpleModTooltip(Mod, item, tooltips, ItemID.ApprenticeStaffT3, "Applies a vulnerability hex");
            SimpleModTooltip(Mod, item, tooltips, ItemID.MonkStaffT3, "Applies a vulnerability hex");
            SimpleModTooltip(Mod, item, tooltips, ItemID.DD2SquireBetsySword, "Applies a vulnerability hex");
            SimpleModTooltip(Mod, item, tooltips, ItemID.DD2BetsyBow, "Applies a vulnerability hex");
            SimpleModTooltip(Mod, item, tooltips, ItemID.StarinaBottle, "Increases mana regen by 10 and decreases mana regen delay by 50%");
            SimpleModTooltip(Mod, item, tooltips, ItemID.ManaRegenerationBand, "Increases mana regen by 25 and decreases mana regen delay by 100%");


            Player player = Main.LocalPlayer;

            if (player.GetModPlayer<tsorcRevampPlayer>().BearerOfTheCurse && player.whoAmI == Main.myPlayer && item.healLife > 0)
            {
                SimpleGlobalModTooltip(Mod, tooltips, "Doesn't heal the [c/6d8827:Bearer of the Curse]");
            }

            if (player.GetModPlayer<tsorcRevampPlayer>().BearerOfTheCurse && player.whoAmI == Main.myPlayer && item.buffType == BuffID.WellFed)
            {
                SimpleGlobalModTooltip(Mod, tooltips, "Increases stamina regen by 5% for the [c/6d8827:Bearer of the Curse]");
            }

            if (player.GetModPlayer<tsorcRevampPlayer>().BearerOfTheCurse && player.whoAmI == Main.myPlayer && item.buffType == BuffID.WellFed2)
            {
                SimpleGlobalModTooltip(Mod, tooltips, "Increases stamina regen by 10% for the [c/6d8827:Bearer of the Curse]");
            }

            if (player.GetModPlayer<tsorcRevampPlayer>().BearerOfTheCurse && player.whoAmI == Main.myPlayer && item.buffType == BuffID.WellFed3)
            {
                SimpleGlobalModTooltip(Mod, tooltips, "Increases stamina regen by 15% for the [c/6d8827:Bearer of the Curse]");
            }

            if (player.GetModPlayer<tsorcRevampPlayer>().BearerOfTheCurse && player.whoAmI == Main.myPlayer && item.type == ModContent.ItemType<CrystalNunchaku>())
            {
                SimpleGlobalModTooltip(Mod, tooltips, "May make the [c/6d8827:Bearer of the Curse] more vulnerable");
            }

            if (player.GetModPlayer<tsorcRevampPlayer>().BearerOfTheCurse && player.whoAmI == Main.myPlayer && item.type == ModContent.ItemType<SearingLash>())
            {
                SimpleGlobalModTooltip(Mod, tooltips, "Fully charged whip debuff counts as 2");
            }

            if (player.GetModPlayer<tsorcRevampPlayer>().BearerOfTheCurse && player.whoAmI == Main.myPlayer && item.type == ModContent.ItemType<NightsCracker>())
            {
                SimpleGlobalModTooltip(Mod, tooltips, "Fully charged whip debuff counts as 2");
            }

            if (player.GetModPlayer<tsorcRevampPlayer>().BearerOfTheCurse && player.whoAmI == Main.myPlayer && item.type == ItemID.ManaRegenerationPotion)
            {
                SimpleGlobalModTooltip(Mod, tooltips, "Increases Cerulean Flask restoration time");
            }
        }
    }
}

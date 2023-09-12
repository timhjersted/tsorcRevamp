using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.Items.VanillaItems
{
    class MinorEdits : GlobalItem
    {
        public static float BotCWellFedStaminaRegen = 5f;
        public static float BotCPlentySatisfiedStaminaRegen = 10f;
        public static float BotCExquisitelyStuffedStaminaRegen = 15f;
        public override void SetDefaults(Item item)
        {
            if (item.type == ItemID.StaffofRegrowth && ModContent.GetInstance<tsorcRevampConfig>().AdventureMode)
            {
                item.createTile = -1; //block placing grass, thus allowing use
            }
            if (item.type == ItemID.DivingHelmet)
            {
                item.accessory = true;
            }
            if (item.type == ItemID.MoltenPickaxe)
            {
                item.useTime = 15;
                item.useAnimation = 15;
            }
            if (item.type == ItemID.OasisCrate || item.type == ItemID.OasisCrateHard || item.type == ItemID.DungeonFishingCrate || item.type == ItemID.DungeonFishingCrateHard)
            {
                ItemID.Sets.OpenableBag[item.type] = false;
            }
        }
        public override void GrabRange(Item item, Player player, ref int grabRange)
        {
            if (item.type == ItemID.ManaCloakStar)
            {
                if (player.manaMagnet)
                {
                    grabRange += 100;
                }
            }
        }

        public override bool CanUseItem(Item item, Player player)
        {
            if ((item.type == ItemID.DirtRod || item.type == ItemID.BoneWand) && ModContent.GetInstance<tsorcRevampConfig>().AdventureMode)
            {
                return false;
            }
            return true;
        }

        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            if (item.type == ItemID.MechanicalEye)
            {
                int ttindex = tooltips.FindIndex(t => t.Name == "Tooltip0");
                if (ttindex != -1)
                {
                    tooltips.RemoveAt(ttindex);
                    tooltips.Insert(ttindex, new TooltipLine(Mod, "RealBossName", Language.GetTextValue("Mods.tsorcRevamp.Items.VanillaItems.MechanicalEye.Tooltip")));
                }
            }
            if (item.type == ItemID.WormScarf)
            {
                int ttindex = tooltips.FindIndex(t => t.Name == "Tooltip0");
                if (ttindex != -1)
                {
                    tooltips.RemoveAt(ttindex);
                    tooltips.Insert(ttindex, new TooltipLine(Mod, "ResistanceScarf", LangUtils.GetTextValue("CommonItemTooltip.DRStat", 17)));
                }
            }
            if (item.type == ItemID.WarmthPotion)
            {
                int ttindex = tooltips.FindIndex(t => t.Name == "Tooltip0");
                if (ttindex != -1)
                {
                    tooltips.RemoveAt(ttindex);
                    tooltips.Insert(ttindex, new TooltipLine(Mod, "ColdResistance", LangUtils.GetTextValue("Buffs.VanillaBuffs.Warmth", 30)));
                }
            }
            if (item.type == ItemID.EndurancePotion)
            {
                int ttindex = tooltips.FindIndex(t => t.Name == "Tooltip0");
                if (ttindex != -1)
                {
                    tooltips.RemoveAt(ttindex);
                    tooltips.Insert(ttindex, new TooltipLine(Mod, "ResistancePot", LangUtils.GetTextValue("CommonItemTooltip.DRStat", 10)));
                }
            }
            if (item.type == ItemID.FrozenTurtleShell)
            {
                int ttindex = tooltips.FindIndex(t => t.Name == "Tooltip0");
                if (ttindex != -1)
                {
                    tooltips.RemoveAt(ttindex);
                    tooltips.Insert(ttindex, new TooltipLine(Mod, "ResistanceIceBarrier", LangUtils.GetTextValue("Items.VanillaItems.FrozenTurtleShell", 50, 25)));
                }
            }
            if (item.type == ItemID.FrozenShield)
            {
                int ttindex = tooltips.FindIndex(t => t.Name == "Tooltip1");
                if (ttindex != -1)
                {
                    tooltips.RemoveAt(ttindex);
                    tooltips.Insert(ttindex, new TooltipLine(Mod, "ResistanceIceBarrier", LangUtils.GetTextValue("Items.VanillaItems.FrozenTurtleShell", 50, 25)));
                }
            }
        }
    }
}

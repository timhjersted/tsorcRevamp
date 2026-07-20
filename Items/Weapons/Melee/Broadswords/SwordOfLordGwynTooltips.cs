using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Items.Weapons.Magic;

namespace tsorcRevamp.Items.Weapons.Melee.Broadswords
{
    public class FourAttackWeaponTooltips : GlobalItem
    {
        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            if (item.type != ModContent.ItemType<SwordOfLordGwyn>()
                && item.type != ModContent.ItemType<HeartOfWinter>()
                && item.type != ModContent.ItemType<WrathOfGold>()
                && item.type != ModContent.ItemType<GravemawTome>())
            {
                return;
            }

            int insertionIndex = tooltips.FindIndex(IsItemTooltip);
            if (insertionIndex < 0)
            {
                insertionIndex = tooltips.Count;
            }

            tooltips.RemoveAll(IsItemTooltip);

            string tooltipName = FourAttackWeaponControls.AdvancedControlsEnabled ? "AdvancedTooltip" : "Tooltip";
            string localizationKey = $"Mods.{Mod.Name}.Items.{item.ModItem.Name}.{tooltipName}";
            string localizedTooltip = item.type == ModContent.ItemType<SwordOfLordGwyn>()
                ? Language.GetTextValue(localizationKey, SwordOfLordGwyn.SpearManaCost, SwordOfLordGwyn.DashManaCost, SwordOfLordGwyn.NovaManaCost)
                : Language.GetTextValue(localizationKey);

            string[] lines = localizedTooltip.Replace("\r", string.Empty)
                .Split('\n', StringSplitOptions.RemoveEmptyEntries);

            for (int i = 0; i < lines.Length; i++)
            {
                tooltips.Insert(insertionIndex + i,
                    new TooltipLine(Mod, $"FourAttackControl{i}", lines[i].Trim()));
            }
        }

        private bool IsItemTooltip(TooltipLine line)
        {
            return line.Name.StartsWith("Tooltip")
                && int.TryParse(line.Name.Substring("Tooltip".Length), out _);
        }
    }
}

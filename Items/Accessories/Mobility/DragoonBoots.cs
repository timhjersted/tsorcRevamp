using System.Collections.Generic;
using Terraria;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.Items.Accessories.Mobility
{
    public class DragoonBoots : ModItem
    {
        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            Item.accessory = true;
            Item.width = 32;
            Item.height = 26;
            Item.expert = true;
            Item.value = PriceByRarity.Red_10;
        }
        public override void UpdateEquip(Player player)
        {
            player.noFallDmg = true;
            Main.LocalPlayer.GetModPlayer<tsorcRevampPlayer>().DragoonBoots = true;
        }
        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            var DragoonBoots = tsorcRevamp.toggleDragoonBoots.GetAssignedKeys();
            string DragoonBootsString = DragoonBoots.Count > 0 ? DragoonBoots[0] : LangUtils.GetTextValue("Keybinds.Dragoon Boots.DisplayName") + LangUtils.GetTextValue("CommonItemTooltip.NotBound");
            int ttindex = tooltips.FindIndex(t => t.Name == "Tooltip3");
            if (ttindex != -1)
            {
                tooltips.RemoveAt(ttindex);
                tooltips.Insert(ttindex, new TooltipLine(Mod, "Keybind", Language.GetTextValue("Mods.tsorcRevamp.Items.DragoonBoots.Keybind1") + DragoonBootsString + Language.GetTextValue("Mods.tsorcRevamp.Items.DragoonBoots.Keybind2")));
            }
        }

    }
}

using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.Content.Items.Accessories.Defensive.Rings.WolfRing
{
    public class WolfRingItem : ModItem
    {
        public static int AbyssDef = 12;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(AbyssDef);

        public override void SetDefaults()
        {
            Item.width = 24;
            Item.height = 24;
            Item.defense = 18;
            Item.accessory = true;
            Item.value = PriceByRarity.Purple_11;
            Item.rare = ModContent.RarityType<DarkBlue>();
        }


        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.GetModPlayer<WolfRingPlayer>().Equipped = true;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            var WolfRingKey = tsorcRevamp.WolfRing.GetAssignedKeys();
            string WolfRingString = WolfRingKey.Count > 0 ? WolfRingKey[0] : LangUtils.GetTextValue("Keybinds.Wolf Ring.DisplayName") + LangUtils.GetTextValue("CommonItemTooltip.NotBound");
            int ttindex = tooltips.FindIndex(t => t.Name == "Tooltip1");
            if (ttindex != -1)
            {
                tooltips.RemoveAt(ttindex);
                tooltips.Insert(ttindex, new TooltipLine(Mod, "Keybind", Language.GetTextValue("Mods.tsorcRevamp.Items.WolfRing.Keybind1") + WolfRingString + Language.GetTextValue("Mods.tsorcRevamp.Items.WolfRing.Keybind2")));
            }
        }

    }
}
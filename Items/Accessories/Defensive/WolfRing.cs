using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Accessories.Defensive
{
    public class WolfRing : ModItem
    {
        public static int AbyssDef = 12;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(AbyssDef);
        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            Item.width = 24;
            Item.height = 24;
            Item.defense = 6;
            Item.accessory = true;
            Item.value = PriceByRarity.Red_10;
            Item.rare = ItemRarityID.Red;
        }


        public override void UpdateEquip(Player player)
        {
            player.GetModPlayer<tsorcRevampPlayer>().WolfRing = true;
            if (Main.bloodMoon)
            { // Apparently this is the flag used in the Abyss?
                player.statDefense += AbyssDef;
            }
        }
        public override void UpdateAccessory(Player player, bool hideVisual) {
            if (!hideVisual) player.AddBuff(BuffID.WeaponImbueVenom, 1, false);
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            var WolfRingKey = tsorcRevamp.WolfRing.GetAssignedKeys();
            string WolfRingString = WolfRingKey.Count > 0 ? WolfRingKey[0] : "Wolf Ring: <NOT BOUND>";
            int ttindex = tooltips.FindIndex(t => t.Name == "Tooltip1");
            if (ttindex != -1)
            {
                tooltips.RemoveAt(ttindex);
                tooltips.Insert(ttindex, new TooltipLine(Mod, "Keybind", Language.GetTextValue("Mods.tsorcRevamp.Items.WolfRing.Keybind1") + WolfRingString + Language.GetTextValue("Mods.tsorcRevamp.Items.WolfRing.Keybind2")));
            }
        }

    }
}
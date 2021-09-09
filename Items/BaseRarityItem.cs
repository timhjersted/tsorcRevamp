using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria.ModLoader;

public abstract class BaseRarityItem : ModItem
{
    public static class BaseColor
    {
        public static Color RarityExample => new Color(0, 255, 80);
    }
    public int DarkSoulRarity = 0;

    //custom name color
    public Color? customNameColor = null;

    public override void ModifyTooltips(List<TooltipLine> list)
    {
        if (customNameColor != null)
        {
            foreach (TooltipLine line2 in list)
            {
                if (line2.mod == "Terraria" && line2.Name == "ItemName")
                {
                    line2.overrideColor = (Color)customNameColor;
                }
            }
            return;
        }

        if (item.modItem is BaseRarityItem MyModItem && MyModItem.DarkSoulRarity != 0)
        {
            Color Rare;
            switch (MyModItem.DarkSoulRarity)
            {
                default: Rare = Color.White; break;
                case 12: Rare = BaseColor.RarityExample; break;
            }
            foreach (TooltipLine line2 in list)
            {
                if (line2.mod == "Terraria" && line2.Name == "ItemName")
                {
                    line2.overrideColor = Rare;
                }
            }
        }
    }
}
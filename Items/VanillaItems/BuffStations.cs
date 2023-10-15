using Humanizer;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.VanillaItems
{
    class BuffStations : GlobalItem
    {

        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            if (item.type == ItemID.SharpeningStation)
            {
                int ttindex = tooltips.FindIndex(t => t.Name == "Tooltip0");
                if (ttindex != -1)
                {
                    tooltips.RemoveAt(ttindex);
                    tooltips.Insert(ttindex, new TooltipLine(Mod, "Sharpened", Language.GetTextValue("Mods.tsorcRevamp.Items.VanillaItems.SharpeningStation").FormatWith(tsorcRevampPlayer.SharpenedMeleeArmorPen)));
                }
            }
            if (item.type == ItemID.AmmoBox)
            {
                tooltips.Insert(3, new TooltipLine(Mod, "QualityAmmo", Language.GetTextValue("Mods.tsorcRevamp.Items.VanillaItems.AmmoBox")));
            }
        }
        public override void AddRecipes()
        {
            Recipe recipe = Recipe.Create(ItemID.FlaskofFire, 1);
            recipe.AddIngredient(ItemID.BottledWater, 1);
            recipe.AddIngredient(ItemID.Deathweed, 1);
            recipe.AddIngredient(ItemID.Fireblossom, 4);
            recipe.AddTile(TileID.Bottles);
            recipe.Register();

        }
    }
}

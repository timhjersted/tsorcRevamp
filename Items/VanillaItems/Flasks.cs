using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.VanillaItems
{
    class Flasks : GlobalItem
    {

        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            if (item.type == ItemID.FlaskofPoison
                || item.type == ItemID.FlaskofIchor
                )
            {
                tooltips.Insert(3, new TooltipLine(Mod, "", "Increases melee and whip damage by 10%"));
            }
            if (item.type == ItemID.FlaskofGold)
            {
                tooltips.Insert(3, new TooltipLine(Mod, "", "Increases melee and whip damage by 15%"));
            }
            if (item.type == ItemID.FlaskofParty)
            {
                tooltips.Insert(3, new TooltipLine(Mod, "", "Increases melee and whip damage by 17%"));
            }
            if (item.type == ItemID.FlaskofFire)
            {
                tooltips.Insert(3, new TooltipLine(Mod, "", "Increases melee and whip damage by 12%"));
            }
            if (item.type == ItemID.FlaskofFire)
            {
                tooltips.Insert(4, new TooltipLine(Mod, "", "All fire damage does 2x damage to woody enemies"));
            }
            if (item.type == ItemID.FlaskofCursedFlames)
            {
                tooltips.Insert(3, new TooltipLine(Mod, "", "Increases melee and whip damage by 16%"));
            }
            if (item.type == ItemID.FlaskofVenom)
            {
                tooltips.Insert(3, new TooltipLine(Mod, "", "Increases melee critical strike chance by 8%, increases whip damage by 8% multiplicatively"));
            }
            if (item.type == ItemID.FlaskofNanites)
            {
                tooltips.Insert(3, new TooltipLine(Mod, "", "Increases melee critical strike chance by 14%, increases whip damage by 14% multiplicatively"));
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

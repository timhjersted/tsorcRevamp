using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors
{
    [AutoloadEquip(EquipType.Body)]
    public class AncientBrassArmor : ModItem
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Set bonus: +6 defense, +10% Move Speed, +5% ranged dmg");
        }

        public override void SetDefaults()
        {
            item.width = 18;
            item.height = 18;
            item.defense = 3;
            item.value = 27000;
            item.rare = ItemRarityID.White;
        }


        public override void AddRecipes()
        {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.IronChainmail, 1);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 200);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }
    }
}

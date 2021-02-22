using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors
{
    [AutoloadEquip(EquipType.Legs)]
    public class RedHerosPants : ModItem
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Red Hero's Pants");
            Tooltip.SetDefault("Worn by the hero himself!");
        }

        public override void SetDefaults()
        {
            item.width = 18;
            item.height = 18;
            item.defense = 13;
            item.value = 2000;
            item.rare = ItemRarityID.LightRed;
        }

        public override void AddRecipes()
        {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(mod.GetItem("BlueHerosPants"), 1);
            recipe.AddIngredient(ItemID.SoulofSight, 2);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 10000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }
    }
}


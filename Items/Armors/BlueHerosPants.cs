using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors
{
    [AutoloadEquip(EquipType.Legs)]
    public class BlueHerosPants : ModItem
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Blue Hero's Pants");
            Tooltip.SetDefault("Worn by the hero himself!");
        }

        public override void SetDefaults()
        {
            item.width = 18;
            item.height = 18;
            item.defense = 10;
            item.value = 2000;
            item.rare = ItemRarityID.Green;
        }

        public override void AddRecipes()
        {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.HerosPants, 1);
            recipe.AddIngredient(ItemID.MythrilBar, 1);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 3000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }
    }
}


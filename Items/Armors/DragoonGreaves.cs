using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors
{
    [AutoloadEquip(EquipType.Legs)]
    public class DragoonGreaves : ModItem
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Harmonized with Earth and Water");
        }

        public override void SetDefaults()
        {
            item.width = 18;
            item.height = 18;
            item.defense = 15;
            item.value = 500000;
            item.rare = ItemRarityID.Orange;
        }
        
        public override void UpdateEquip(Player player)
        {
            player.doubleJumpCloud = true;
            player.jumpBoost = true;
        }

        public override void AddRecipes()
        {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(mod.GetItem("RedHerosPants"), 1);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 10000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }
    }
}


using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Accessories
{
    public class Celestriad : ModItem
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("All spells are free to cast");
        }

        public override void SetDefaults()
        {
            Item.width = 22;
            Item.height = 26;
            Item.accessory = true;
            Item.value = PriceByRarity.Purple_11;
            Item.rare = ItemRarityID.Purple;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(Mod.Find<ModItem>("GoldenHairpin").Type, 1);
            recipe.AddIngredient(Mod.Find<ModItem>("GemBox").Type, 1);
            recipe.AddIngredient(Mod.Find<ModItem>("CursedSoul").Type, 30);
            recipe.AddIngredient(Mod.Find<ModItem>("SoulOfBlight").Type, 1);
            recipe.AddIngredient(Mod.Find<ModItem>("DarkSoul").Type, 400000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }

        public override void UpdateEquip(Player player)
        {
            if (player.inventory[player.selectedItem].magic)
            {
                player.manaCost = 1f / player.inventory[player.selectedItem].mana;
            }

        }
    }
}

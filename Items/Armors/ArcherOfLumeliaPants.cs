using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors
{
    [AutoloadEquip(EquipType.Legs)]
    public class ArcherOfLumeliaPants : ModItem
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Archer of Lumelia Pants");
            Tooltip.SetDefault("Gifted with bows, repeaters, and other long range weapons\n+13% movement speed");
        }

        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 18;
            Item.defense = 17;
            Item.value = 5000;
            Item.rare = ItemRarityID.Orange;
        }

        public override void UpdateEquip(Player player)
        {
            player.moveSpeed += 0.13f;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.AdamantiteLeggings, 1);
            recipe.AddIngredient(Mod.Find<ModItem>("DarkSoul").Type, 4000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}


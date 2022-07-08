using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors
{
    [LegacyName("OldStuddedLeatherGreaves")]
    [LegacyName("OldLeatherGreaves")]
    [AutoloadEquip(EquipType.Legs)]
    public class LeatherGreaves : ModItem
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Increases movement speed by 10%");
        }
        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 18;
            Item.defense = 2;
            Item.value = 12000;
            Item.rare = ItemRarityID.White;
        }
        public override void UpdateEquip(Player player)
        {
            player.moveSpeed += 0.1f;
        }

        public override void AddRecipes()
        {
            Terraria.Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.Leather, 5);
            recipe.AddIngredient(Mod.Find<ModItem>("DarkSoul").Type, 350);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}


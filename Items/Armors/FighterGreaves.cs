using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors
{
    [AutoloadEquip(EquipType.Legs)]
    public class FighterGreaves : ModItem
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Adept at close combat\nAdds double jump and jump boost, +12% movement speed");
        }
        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 18;
            Item.defense = 24;
            Item.value = 5000;
            Item.rare = ItemRarityID.Orange;
        }

        public override void UpdateEquip(Player player)
        {
            player.hasJumpOption_Cloud = true;
            player.moveSpeed += 0.12f;
            player.jumpBoost = true;
        }

        public override void AddRecipes()
        {
            Terraria.Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.AdamantiteLeggings, 1);
            recipe.AddIngredient(Mod.Find<ModItem>("DarkSoul").Type, 4000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}


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
            Item.width = 18;
            Item.height = 18;
            Item.defense = 15;
            Item.rare = ItemRarityID.Cyan;
            Item.value = PriceByRarity.fromItem(Item);
        }

        public override void UpdateEquip(Player player)
        {
            player.hasJumpOption_Unicorn = true;
            player.jumpBoost = true;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(Mod.Find<ModItem>("RedHerosPants").Type, 1);
            recipe.AddIngredient(ModContent.ItemType<SoulOfLife>(), 1);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 10000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}


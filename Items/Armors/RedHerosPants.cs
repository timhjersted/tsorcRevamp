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
            Item.width = 18;
            Item.height = 18;
            Item.defense = 13;
            Item.rare = ItemRarityID.Yellow;
            Item.value = PriceByRarity.fromItem(Item);
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(Mod.Find<ModItem>("BlueHerosPants").Type, 1);
            recipe.AddIngredient(ItemID.SoulofSight, 2);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 10000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}


using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Potions
{
    class SupremeManaPotion : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 20;
            Item.height = 26;
            Item.useStyle = ItemUseStyleID.EatFood;
            Item.useAnimation = 15;
            Item.useTime = 15;
            Item.maxStack = 100;
            Item.healMana = 450;
            Item.potion = true;
            Item.rare = ItemRarityID.Green;
            Item.consumable = true;
            Item.value = 10000;
            Item.UseSound = SoundID.Item3;
        }
        public override void GetHealMana(Player player, bool quickHeal, ref int healValue)
        {
            healValue = 450;
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe(2);
            recipe.AddIngredient(ItemID.SuperManaPotion, 2);
            recipe.AddIngredient(ItemID.LunarOre, 3);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 750);
            recipe.AddTile(TileID.Bottles);

            recipe.Register();
        }
    }
}

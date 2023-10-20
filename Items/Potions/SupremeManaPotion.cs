using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Potions
{
    class SupremeManaPotion : ModItem
    {
        public static int Mana = 400;
        public static int SicknessDuration = 3;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(Mana, SicknessDuration);
        public override void SetStaticDefaults()
        {
        }
        public override void SetDefaults()
        {
            Item.width = 20;
            Item.height = 26;
            Item.maxStack = 9999;
            Item.rare = ItemRarityID.Green;
            Item.consumable = true;
            Item.value = 10000;
            Item.UseSound = SoundID.Item3;
        }
        public override bool? UseItem(Player player)
        {
            player.statMana += Mana;
            return true;
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe(2);
            recipe.AddIngredient(ItemID.SuperManaPotion, 2);
            recipe.AddIngredient(ItemID.ChlorophyteOre, 3);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 650);
            recipe.AddTile(TileID.Bottles);

            recipe.Register();
        }
    }
}

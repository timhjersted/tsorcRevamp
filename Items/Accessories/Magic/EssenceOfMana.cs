using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Accessories.Magic
{
    public class EssenceOfMana : ModItem
    {
        public static float PercentMaxManaAmplifier = 50f;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(PercentMaxManaAmplifier);
        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            Item.width = 10;
            Item.accessory = true;
            Item.height = 12;
            Item.maxStack = 1;
            Item.value = PriceByRarity.Blue_1;
            Item.rare = ItemRarityID.Blue;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.ManaCrystal, 5);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 10000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
        public override void UpdateEquip(Player player)
        {
            player.GetModPlayer<tsorcRevampPlayer>().MaxManaAmplifier += PercentMaxManaAmplifier;
        }

    }
}

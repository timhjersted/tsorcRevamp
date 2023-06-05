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
        public static float ManaCostReduction = 9f;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(PercentMaxManaAmplifier, ManaCostReduction);
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Essence of Mana");
            /* Tooltip.SetDefault("Increases max mana by 50%" +
                "\nReduces mana usage by 9%"); */

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
            player.manaCost -= ManaCostReduction / 100f;
        }

    }
}

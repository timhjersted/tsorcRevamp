using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Items.Accessories.Defensive;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Accessories.Magic
{
    [AutoloadEquip(EquipType.HandsOn)]
    public class BandOfStarforging : ModItem
    {
        public static int LifeRegen = 4;
        public static float MaxManaPercentIncrease = 100f;
        public static int ManaRegen = 50;
        public static float ManaRegenDelay = 150f;
        public static float ManaCostReduction = 10f;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(LifeRegen, MaxManaPercentIncrease, ManaRegen, ManaRegenDelay, ManaCostReduction);
        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            Item.width = 28;
            Item.height = 28;
            Item.accessory = true;
            Item.value = PriceByRarity.Red_10;
            Item.rare = ItemRarityID.Red;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ModContent.ItemType<BandOfPhenomenalCosmicPower>());
            recipe.AddIngredient(ModContent.ItemType<EssenceOfMana>());
            recipe.AddIngredient(ModContent.ItemType<BlueTitanite>(), 2);
            recipe.AddIngredient(ModContent.ItemType<WhiteTitanite>());
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 66000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }

        public override void UpdateEquip(Player player)
        {
            player.lifeRegen += LifeRegen;
            player.GetModPlayer<tsorcRevampPlayer>().MaxManaAmplifier += MaxManaPercentIncrease;
            player.manaRegenBonus += ManaRegen;
            player.manaRegenDelayBonus += ManaRegenDelay / 100f;
            player.manaCost -= ManaCostReduction / 100f;
        }

    }
}

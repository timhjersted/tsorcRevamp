using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Accessories.Defensive.Bands
{
    [AutoloadEquip(EquipType.HandsOn)]

    public class BandOfGreatCosmicPower : ModItem
    {
        public static int LifeRegen = 3;
        public static int MaxManaIncrease = 60;
        public static int ManaRegen = 30;
        public static float ManaRegenDelay = 115f;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(LifeRegen, MaxManaIncrease, ManaRegen, ManaRegenDelay);
        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            Item.width = 28;
            Item.height = 28;
            Item.accessory = true;
            Item.value = PriceByRarity.Orange_3;
            Item.rare = ItemRarityID.Orange;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ModContent.ItemType<BandOfCosmicPower>());
            recipe.AddIngredient(ItemID.ShadowScale, 4);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 5000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();

            Recipe recipe2 = CreateRecipe();
            recipe2.AddIngredient(ModContent.ItemType<BandOfCosmicPower>());
            recipe2.AddIngredient(ItemID.TissueSample, 4);
            recipe2.AddIngredient(ModContent.ItemType<DarkSoul>(), 5000);
            recipe2.AddTile(TileID.DemonAltar);

            recipe2.Register();
        }

        public override void UpdateEquip(Player player)
        {
            player.lifeRegen += LifeRegen;
            player.statManaMax2 += MaxManaIncrease;
            player.manaRegenBonus += ManaRegen;
            player.manaRegenDelayBonus += ManaRegenDelay / 100f;
        }

    }
}

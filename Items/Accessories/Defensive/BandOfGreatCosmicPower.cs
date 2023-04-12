using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Accessories.Defensive
{
    [AutoloadEquip(EquipType.HandsOn)]

    public class BandOfGreatCosmicPower : ModItem
    {
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Band of Great Cosmic Power");
            // Tooltip.SetDefault("Increases life regeneration by 3 and maximum mana by 60");
        }

        public override void SetDefaults()
        {
            Item.width = 28;
            Item.height = 28;
            Item.lifeRegen = 3;
            Item.accessory = true;
            Item.value = PriceByRarity.Orange_3;
            Item.rare = ItemRarityID.Orange;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ModContent.ItemType<BandOfCosmicPower>());
            recipe.AddIngredient(ItemID.ShadowScale, 4);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 10000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();

            Recipe recipe2 = CreateRecipe();
            recipe2.AddIngredient(ModContent.ItemType<BandOfCosmicPower>());
            recipe2.AddIngredient(ItemID.TissueSample, 4);
            recipe2.AddIngredient(ModContent.ItemType<DarkSoul>(), 10000);
            recipe2.AddTile(TileID.DemonAltar);

            recipe2.Register();
        }

        public override void UpdateEquip(Player player)
        {
            player.lifeRegen += 3;
            player.statManaMax2 += 60;
        }

    }
}

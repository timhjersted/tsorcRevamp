using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Accessories.Defensive
{
    [AutoloadEquip(EquipType.HandsOn)]

    [LegacyName("BandOfSupremeCosmicPower")]
    public class BandOfPhenomenalCosmicPower : ModItem
    {
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Band of Phenomenal Cosmic Power");
            // Tooltip.SetDefault("Increases life regeneration by 4 and maximum mana by 80");
        }

        public override void SetDefaults()
        {
            Item.width = 28;
            Item.height = 28;
            Item.lifeRegen = 4;
            Item.accessory = true;
            Item.value = PriceByRarity.LightRed_4;
            Item.rare = ItemRarityID.LightRed;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ModContent.ItemType<BandOfGreatCosmicPower>());
            recipe.AddIngredient(ItemID.SoulofLight, 3);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 16000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }

        public override void UpdateEquip(Player player)
        {
            player.lifeRegen += 4;
            player.statManaMax2 += 80;
        }

    }
}

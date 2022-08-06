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
            DisplayName.SetDefault("Band of Great Cosmic Power");
            Tooltip.SetDefault("+3 life regen and increases max mana by 60");
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
            recipe.AddIngredient(Mod.Find<ModItem>("BandOfCosmicPower").Type, 1);
            recipe.AddIngredient(ItemID.ShadowScale, 10);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 10000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }

        public override void UpdateEquip(Player player)
        {
            player.statManaMax2 += 60;
        }

    }
}

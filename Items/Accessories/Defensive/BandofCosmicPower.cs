using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Accessories.Defensive
{
    [AutoloadEquip(EquipType.HandsOn)]
    public class BandOfCosmicPower : ModItem
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Band of Cosmic Power");
            Tooltip.SetDefault("Increases life regeneration by 2 and increases max mana by 40");
        }

        public override void SetDefaults()
        {
            Item.width = 28;
            Item.height = 28;
            Item.lifeRegen = 2;
            Item.accessory = true;
            Item.value = PriceByRarity.Blue_1;
            Item.rare = ItemRarityID.Blue;
        }

        public override void AddRecipes()
        {
            Terraria.Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.BandofRegeneration, 1);
            recipe.AddIngredient(ItemID.BandofStarpower, 1);
            recipe.AddIngredient(Mod.Find<ModItem>("DarkSoul").Type, 3000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();

            Recipe recipe2 = CreateRecipe();
            recipe2.AddIngredient(ItemID.ManaRegenerationBand, 1);
            recipe2.AddIngredient(Mod.Find<ModItem>("DarkSoul").Type, 3000);
            recipe2.AddTile(TileID.DemonAltar);
            recipe2.Register();
        }

        public override void UpdateEquip(Player player)
        {
            player.statManaMax2 += 40;
        }

    }
}

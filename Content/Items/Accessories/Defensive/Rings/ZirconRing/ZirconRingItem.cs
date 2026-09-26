using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items.Accessories.Defensive.RubyCrystal;
using tsorcRevamp.Content.Items.Materials.Souls.DarkSoul;

namespace tsorcRevamp.Content.Items.Accessories.Defensive.Rings.ZirconRing
{
    public class ZirconRingItem : ModItem
    {
        public static float PercentMaxLifeIncrease = 20f;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(PercentMaxLifeIncrease);
        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 32;
            Item.accessory = true;
            Item.value = PriceByRarity.LightRed_4;
            Item.rare = ItemRarityID.LightRed;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.CobaltBar, 3);
            recipe.AddIngredient(ModContent.ItemType<RubyCrystalItem>());
            recipe.AddIngredient(ItemID.SoulofNight, 6);
            recipe.AddIngredient(ModContent.ItemType<DarkSoulItem>(), 9000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.GetModPlayer<ZirconRingPlayer>().Equipped = true;
        }
    }
}


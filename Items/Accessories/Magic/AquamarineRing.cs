using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Accessories.Magic
{
    public class AquamarineRing : ModItem
    {
        public static int MagicArmorPen = 8;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(MagicArmorPen);
        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            Item.width = 24;
            Item.height = 24;
            Item.accessory = true;
            Item.value = PriceByRarity.Blue_1;
            Item.rare = ItemRarityID.Blue;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.SilverBar, 1);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 1800);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }

        public override void UpdateEquip(Player player)
        {
            player.GetArmorPenetration(DamageClass.Magic) += MagicArmorPen;
        }

    }
}

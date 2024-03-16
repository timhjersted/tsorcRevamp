using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Accessories.Ranged
{
    public class ArchmenEmblem : ModItem
    {
        public static float RangedDmg = 15f;
        public static int FlatRangedDmg = 5;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(RangedDmg, FlatRangedDmg);
        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            Item.width = 30;
            Item.height = 30;
            Item.accessory = true;
            Item.rare = ItemRarityID.Pink;
            Item.value = PriceByRarity.fromItem(Item);
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.RangerEmblem);
            recipe.AddIngredient(ItemID.MagicQuiver);
            recipe.AddIngredient(ItemID.HallowedBar, 3);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 8500);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }

        public override void UpdateEquip(Player player)
        {
            player.GetDamage(DamageClass.Ranged) += RangedDmg / 100f;
            player.GetDamage(DamageClass.Ranged).Flat = FlatRangedDmg;
            player.magicQuiver = true;
        }

    }
}
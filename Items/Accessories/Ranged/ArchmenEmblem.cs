using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Accessories.Ranged
{
    public class ArchmenEmblem : ModItem
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("15% increased ranged damage + 5 flat");
        }

        public override void SetDefaults()
        {
            Item.width = 30;
            Item.height = 30;
            Item.accessory = true;
            Item.value = PriceByRarity.LightRed_4;
            Item.rare = ItemRarityID.LightRed;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.RangerEmblem, 1);
            recipe.AddIngredient(ItemID.MagicQuiver, 1);
            recipe.AddIngredient(ItemID.SoulofLight, 5);
            recipe.AddIngredient(ItemID.HallowedBar, 5);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 5000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }

        public override void UpdateEquip(Player player)
        {
            player.GetDamage(DamageClass.Ranged) += 0.15f;
            player.GetDamage(DamageClass.Ranged).Flat = 5;
        }

    }
}
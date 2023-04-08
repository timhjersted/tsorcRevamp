using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Accessories.Mobility
{
    public class SpeedTalisman : ModItem
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("15% increased movement speed" +
                                "\n20% increased melee speed" +
                                "\nGrants autoswing to melee weapons and whips");
        }

        public override void SetDefaults()
        {
            Item.width = 28;
            Item.height = 36;
            Item.accessory = true;
            Item.value = PriceByRarity.Orange_3;
            Item.rare = ItemRarityID.Orange;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.Aglet, 1);
            recipe.AddIngredient(ItemID.AnkletoftheWind, 1);
            recipe.AddIngredient(ItemID.FeralClaws, 1);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 3000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }

        public override void UpdateEquip(Player player)
        {
            player.moveSpeed += 0.15f;
            player.GetAttackSpeed(DamageClass.Melee) += 0.2f;
            player.autoReuseGlove = true;
        }

    }
}

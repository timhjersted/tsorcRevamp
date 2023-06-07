using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Accessories.Mobility
{
    public class SpeedTalisman : ModItem
    {
        public static float AtkSpeed = 8f;
        public static float MoveSpeed = 15f;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(AtkSpeed, MoveSpeed);
        public override void SetStaticDefaults()
        {
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
            player.moveSpeed += MoveSpeed / 100f;
            player.GetAttackSpeed(DamageClass.Generic) += AtkSpeed / 100f;
            player.GetAttackSpeed(DamageClass.Melee) += AtkSpeed / 100f;
            player.autoReuseGlove = true;
        }

    }
}

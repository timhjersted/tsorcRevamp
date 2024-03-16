using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Accessories.Summon
{
    public class ShamanEmblem : ModItem
    {
        public static float SummonDamage = 12;
        public static int MaximumMinionIncrease = 1;
        public static int MaximumTurretIncrease = 1;

        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(SummonDamage, MaximumMinionIncrease, MaximumTurretIncrease);
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
            recipe.AddIngredient(ItemID.SummonerEmblem);
            recipe.AddIngredient(ItemID.PygmyNecklace);
            recipe.AddIngredient(ItemID.HallowedBar, 3);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 8500);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }

        public override void UpdateEquip(Player player)
        {
            player.GetDamage(DamageClass.Summon) += SummonDamage / 100f;
            player.maxMinions += MaximumMinionIncrease;
            player.maxTurrets += MaximumTurretIncrease;
        }
    }
}
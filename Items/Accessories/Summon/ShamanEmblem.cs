using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Accessories.Summon
{
    public class ShamanEmblem : ModItem
    {
        public const float SummonDamage = 14f;
        public const int MaximumTurretIncrease = 2;

        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(SummonDamage, MaximumTurretIncrease);
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
            recipe.AddIngredient(ItemID.ApprenticeScarf);
            recipe.AddIngredient(ItemID.HallowedBar, 3);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 8500);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();

            Recipe recipe1 = CreateRecipe();
            recipe1.AddIngredient(ItemID.SummonerEmblem);
            recipe1.AddIngredient(ItemID.HuntressBuckler);
            recipe1.AddIngredient(ItemID.HallowedBar, 3);
            recipe1.AddIngredient(ModContent.ItemType<DarkSoul>(), 8500);
            recipe1.AddTile(TileID.DemonAltar);

            recipe1.Register();

            Recipe recipe2 = CreateRecipe();
            recipe2.AddIngredient(ItemID.SummonerEmblem);
            recipe2.AddIngredient(ItemID.SquireShield);
            recipe2.AddIngredient(ItemID.HallowedBar, 3);
            recipe2.AddIngredient(ModContent.ItemType<DarkSoul>(), 8500);
            recipe2.AddTile(TileID.DemonAltar);

            recipe2.Register();

            Recipe recipe3 = CreateRecipe();
            recipe3.AddIngredient(ItemID.SummonerEmblem);
            recipe3.AddIngredient(ItemID.MonkBelt);
            recipe3.AddIngredient(ItemID.HallowedBar, 3);
            recipe3.AddIngredient(ModContent.ItemType<DarkSoul>(), 8500);
            recipe3.AddTile(TileID.DemonAltar);

            recipe3.Register();
        }

        public override void UpdateEquip(Player player)
        {
            player.GetDamage(DamageClass.Summon) += SummonDamage / 100f;
            player.maxTurrets += MaximumTurretIncrease;
        }
    }
}
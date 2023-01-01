using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Accessories.Summon
{
    public class ShamanEmblem : ModItem
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("+12% minion damage, +1 minion and turret slot");
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
            recipe.AddIngredient(ItemID.SummonerEmblem);
            recipe.AddIngredient(ItemID.PygmyNecklace);
            recipe.AddIngredient(ItemID.HallowedBar, 3);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 8500);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }

        public override void UpdateEquip(Player player)
        {
            player.GetDamage(DamageClass.Summon) += 0.12f;
            player.maxMinions += 1;
            player.maxTurrets += 1;
        }
    }
}
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Armors.Summon
{
    [AutoloadEquip(EquipType.Legs)]
    public class TarantulaLegs : ModItem
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Increases your max number of minions by 1" +
                "\nIncreases movement speed by 22%");
        }
        public override void SetDefaults()
        {
            Item.width = 22;
            Item.height = 18;
            Item.defense = 9;
            Item.rare = ItemRarityID.Pink;
            Item.value = PriceByRarity.fromItem(Item);
        }
        public override void UpdateEquip(Player player)
        {
            player.GetDamage(DamageClass.Summon) += 0.1f;
            player.maxMinions += 1;
            player.moveSpeed += 0.22f;
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.SpiderGreaves);
            recipe.AddIngredient(ItemID.MythrilBar);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 8400);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();

            Recipe recipe2 = CreateRecipe();
            recipe2.AddIngredient(ItemID.SpiderGreaves);
            recipe2.AddIngredient(ItemID.OrichalcumLeggings);
            recipe2.AddTile(TileID.DemonAltar);

            recipe2.Register();
        }
    }
}
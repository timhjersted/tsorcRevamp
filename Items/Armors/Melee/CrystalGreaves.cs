using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Armors.Melee
{
    [AutoloadEquip(EquipType.Legs)]
    public class CrystalGreaves : ModItem
    {
        public static float MoveSpeed = 21f;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(MoveSpeed);
        public override void SetStaticDefaults()
        {
        }

        public override void UpdateEquip(Player player)
        {
            player.moveSpeed += MoveSpeed / 100f;
        }

        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 18;
            Item.defense = 10;
            Item.rare = ItemRarityID.Pink;
            Item.value = PriceByRarity.fromItem(Item);
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.MythrilGreaves);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 5500);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();

            Recipe recipe2 = CreateRecipe();
            recipe2.AddIngredient(ItemID.MythrilGreaves);
            recipe2.AddIngredient(ItemID.OrichalcumLeggings);
            recipe2.AddTile(TileID.DemonAltar);

            recipe2.Register();
        }
    }
}


using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Armors.Ranged
{
    [AutoloadEquip(EquipType.Legs)]
    public class ArcherOfLumeliaPants : ModItem
    {
        public static float MoveSpeed = 13f;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(MoveSpeed);
        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 18;
            Item.defense = 17;
            Item.rare = ItemRarityID.Lime;
            Item.value = PriceByRarity.fromItem(Item);
        }

        public override void UpdateEquip(Player player)
        {
            player.moveSpeed += MoveSpeed / 100f;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.AdamantiteLeggings);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 4000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();

            Recipe recipe2 = CreateRecipe();
            recipe2.AddIngredient(ItemID.AdamantiteLeggings);
            recipe2.AddIngredient(ItemID.TitaniumLeggings);
            recipe2.AddTile(TileID.DemonAltar);

            recipe2.Register();
        }
    }
}


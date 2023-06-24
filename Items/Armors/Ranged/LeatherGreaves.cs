using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Armors.Ranged
{
    [LegacyName("OldStuddedLeatherGreaves")]
    [LegacyName("OldLeatherGreaves")]
    [AutoloadEquip(EquipType.Legs)]
    public class LeatherGreaves : ModItem
    {
        public static float MoveSpeed = 9f;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(MoveSpeed);
        public override void SetStaticDefaults()
        {
        }
        public override void SetDefaults()
        {
            Item.width = 18;
            Item.height = 18;
            Item.defense = 3;
            Item.rare = ItemRarityID.Green;
            Item.value = PriceByRarity.fromItem(Item);
        }
        public override void UpdateEquip(Player player)
        {
            player.moveSpeed += MoveSpeed / 100f;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.Leather, 3);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 150);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}


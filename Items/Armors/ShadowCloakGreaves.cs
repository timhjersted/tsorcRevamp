using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Armors
{
    [AutoloadEquip(EquipType.Legs)]
    public class ShadowCloakGreaves : ModItem
    {
        public static float MoveSpeed = 5f;
        public static float AtkSpeed = 7f;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(MoveSpeed, AtkSpeed);
        public override void SetStaticDefaults()
        {
        }
        public override void SetDefaults()
        {
            Item.width = 22;
            Item.height = 18;
            Item.defense = 7;
            Item.rare = ItemRarityID.Orange;
            Item.value = PriceByRarity.fromItem(Item);
        }

        public override void UpdateEquip(Player player)
        {
            player.moveSpeed += MoveSpeed / 100f;
            player.GetAttackSpeed(DamageClass.Generic) += AtkSpeed / 100f;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.ShadowGreaves);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 1600);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();

            Recipe recipe2 = CreateRecipe();
            recipe2.AddIngredient(ItemID.AncientShadowGreaves);
            recipe2.AddIngredient(ModContent.ItemType<DarkSoul>(), 1600);
            recipe2.AddTile(TileID.DemonAltar);

            recipe2.Register();
        }
    }
}


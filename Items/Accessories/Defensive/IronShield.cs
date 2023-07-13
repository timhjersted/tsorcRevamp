using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Accessories.Defensive
{

    [AutoloadEquip(EquipType.Shield)]

    public class IronShield : ModItem
    {
        public static float DR = 4f;
        public static float BadMoveSpeedMult = 5f;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(DR, BadMoveSpeedMult);
        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            Item.width = 28;
            Item.height = 28;
            Item.accessory = true;
            Item.defense = 2;
            Item.rare = ItemRarityID.Blue;
            Item.value = PriceByRarity.Blue_1;
        }

        public override void UpdateEquip(Player player)
        {
            player.endurance += DR / 100f;
            player.moveSpeed *= 1f - BadMoveSpeedMult / 100f;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.IronBar, 1);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 500);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}

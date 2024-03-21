using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Accessories.Mobility
{
    [AutoloadEquip(EquipType.Shoes)]
    public class BootsOfHaste : ModItem
    {
        public static float MoveSpeedMult = 25f;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(MoveSpeedMult);
        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 26;
            Item.accessory = true;
            Item.rare = ItemRarityID.Blue;
            Item.value = PriceByRarity.Blue_1;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.HermesBoots, 1);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 1000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }

        public override void UpdateEquip(Player player)
        {
            player.accRunSpeed = 6;
            player.moveSpeed *= 1f + MoveSpeedMult / 100f;
        }
    }
}

using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Accessories.Defensive.Shields
{

    [AutoloadEquip(EquipType.Shield)]
    public class SpikedIronShield : ModItem
    {
        public static float Thorns = 100f;
        public static float DR = 4f;
        public static float BadMoveSpeedMult = 5f;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(Thorns, DR, BadMoveSpeedMult);
        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            Item.width = 28;
            Item.height = 28;
            Item.defense = 3;
            Item.rare = ItemRarityID.Green;
            Item.accessory = true;
            Item.value = PriceByRarity.Green_2;
        }

        public override void UpdateEquip(Player player)
        {
            player.thorns += Thorns / 100f;
            player.endurance += DR / 100f;
            player.moveSpeed *= 1f - BadMoveSpeedMult / 100f;
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ModContent.ItemType<IronShield>());
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 2000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }

}

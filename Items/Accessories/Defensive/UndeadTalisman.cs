using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Accessories.Defensive
{
    public class UndeadTalisman : ModItem
    {
        public static int FlatDR = 15;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(FlatDR);
        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {

            Item.width = 22;
            Item.height = 32;
            Item.rare = ItemRarityID.Green;
            Item.accessory = true;
            Item.value = PriceByRarity.Green_2;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.Bone, 3);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 1000);

            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }

        public override void UpdateEquip(Player player)
        {
            Main.LocalPlayer.GetModPlayer<tsorcRevampPlayer>().UndeadTalisman = true;
        }

    }
}

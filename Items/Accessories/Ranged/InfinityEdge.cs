using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Accessories.Ranged
{
    public class InfinityEdge : ModItem
    {
        public static float CritDmgIncrease = 35f;
        public static float NonCritDmgReduction = 20f;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(CritDmgIncrease, NonCritDmgReduction);
        public override void SetStaticDefaults()
        {
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
            recipe.AddIngredient(ItemID.GoldBar, 34);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 3600);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }

        public override void UpdateEquip(Player player)
        {
            player.GetModPlayer<tsorcRevampPlayer>().InfinityEdge = true;
        }

    }
}
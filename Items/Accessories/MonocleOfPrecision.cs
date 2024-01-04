using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Accessories
{
    public class MonocleOfPrecision : ModItem
    {
        public static float CriticalStrikeChance = 7;
        public static float WhipCritHitboxSize = 3;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(CriticalStrikeChance, WhipCritHitboxSize);
        public override void SetStaticDefaults()
        {
        }
        public override void SetDefaults()
        {
            Item.width = 24;
            Item.height = 28;
            Item.accessory = true;
            Item.value = PriceByRarity.Green_2;
            Item.rare = ItemRarityID.Green;
        }
        public override void UpdateEquip(Player player)
        {
            player.GetCritChance(DamageClass.Generic) += CriticalStrikeChance;
            player.GetModPlayer<tsorcRevampPlayer>().WhipCritHitboxSize = WhipCritHitboxSize;
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.BlackLens);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 2000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}

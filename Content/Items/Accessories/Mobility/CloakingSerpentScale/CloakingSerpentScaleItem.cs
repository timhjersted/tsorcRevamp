using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items.Materials.Souls;
using tsorcRevamp.Content.Items.Materials.Souls.DarkSoul;

namespace tsorcRevamp.Content.Items.Accessories.Mobility.CloakingSerpentScale
{
    public class CloakingSerpentScaleItem : ModItem
    {
        public const float CloakDurationSeconds = 2.5f;
        public const int CloakCooldownSeconds = 8;
        public const int AmbushCritBonus = 12;
        public const int AmbushDamageBonus = 10;

        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(
            CloakDurationSeconds,
            AmbushCritBonus,
            AmbushDamageBonus,
            CloakCooldownSeconds);

        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 32;
            Item.accessory = true;
            Item.rare = ItemRarityID.Orange;
            Item.value = PriceByRarity.Orange_3;
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.GetModPlayer<tsorcRevampPlayer>().CloakingSerpentScaleEquipped = true;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ModContent.ItemType<GreatSerpentSoul>(), 1);
            recipe.AddIngredient(ModContent.ItemType<DarkSoulItem>(), 8000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.Register();
        }
    }
}

using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items.Materials.Souls.DarkSoul;

namespace tsorcRevamp.Content.Items.Accessories.Defensive.LightCloak
{
    public class LightCloakItem : ModItem
    {
        public const float LifeRegen1 = 4f;
        public const float LifeThreshold = 40f;
        public const float LifeRegen2 = 6f;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(LifeRegen1 / 2f, LifeThreshold, LifeRegen2 / 2f);

        public override void SetDefaults()
        {
            Item.width = 28;
            Item.height = 28;
            Item.accessory = true;
            Item.rare = ItemRarityID.LightRed;
            Item.value = PriceByRarity.LightRed_4;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ModContent.ItemType<DarkSoulItem>(), 7000);
            recipe.AddIngredient(ItemID.SoulofLight, 1);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            var modPlayer = player.GetModPlayer<LightCloakPlayer>();
            modPlayer.Equipped  = true;
            modPlayer.Visual = !hideVisual;
        }
    }
}

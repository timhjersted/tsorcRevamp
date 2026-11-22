using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items.Accessories.Damage.ShadowmoonCloak;
using tsorcRevamp.Content.Items.Accessories.Defensive.DarkCloak;
using tsorcRevamp.Content.Items.Accessories.Defensive.LightCloak;
using tsorcRevamp.Content.Items.Materials.Souls.DarkSoul;

namespace tsorcRevamp.Content.Items.Accessories.Other.DragoonCloak
{
    public class DragoonCloakItem : ModItem
    {
        public const float LifeThreshold = (LightCloakItem.LifeThreshold + ShadowmoonCloakItem.LifeThreshold + DarkCloakItem.LifeThreshold) / 3f;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(LifeThreshold);

        public override void SetDefaults()
        {
            Item.width = 38;
            Item.height = 36;
            Item.defense = DarkCloakItem.Defense1;
            Item.accessory = true;
            Item.value = PriceByRarity.LightPurple_6;
            Item.rare = ItemRarityID.LightPurple;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ModContent.ItemType<LightCloakItem>());
            recipe.AddIngredient(ModContent.ItemType<DarkCloakItem>());
            recipe.AddIngredient(ModContent.ItemType<ShadowmoonCloakItem>());
            recipe.AddIngredient(ItemID.ChlorophyteBar, 1);
            recipe.AddIngredient(ModContent.ItemType<DarkSoulItem>(), 70000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            var modPlayer = player.GetModPlayer<DragoonCloakPlayer>();

            modPlayer.Equipped = true;
            modPlayer.Visual = !hideVisual;
        }
    }
}

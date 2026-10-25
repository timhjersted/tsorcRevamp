using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items.Materials.Souls.DarkSoul;

namespace tsorcRevamp.Content.Items.Accessories.Defensive.DarkCloak
{
    public class DarkCloakItem : ModItem
    {
        public static int Defense1 = 7;
        public const float LifeThreshold = 40f;
        public static int Defense2 = 10;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(LifeThreshold, Defense2);
        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            Item.width = 24;
            Item.height = 24;
            Item.accessory = true;
            Item.value = PriceByRarity.LightRed_4;
            Item.rare = ItemRarityID.LightRed;
            Item.defense = Defense1; //item has defense stat by itself
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.SoulofNight, 1);
            recipe.AddIngredient(ModContent.ItemType<DarkSoulItem>(), 7000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.GetModPlayer<DarkCloakPlayer>().Equipped = true;
        }

    }
}

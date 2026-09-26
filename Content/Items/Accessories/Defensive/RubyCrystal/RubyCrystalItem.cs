using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items.Materials.Souls.DarkSoul;

namespace tsorcRevamp.Content.Items.Accessories.Defensive.RubyCrystal
{
    public class RubyCrystalItem : ModItem 
    {
        public override string Texture => UsefulFunctions.VanillaTextureFilepath(ItemID.LifeCrystal);//texture incoming
        public const int MaxLifeIncrease = 40;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(MaxLifeIncrease);

        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 32;
            Item.accessory = true;
            Item.value = PriceByRarity.Blue_1;
            Item.rare = ItemRarityID.Blue;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.Ruby, 4);
            recipe.AddIngredient(ModContent.ItemType<DarkSoulItem>(), 1000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.GetModPlayer<RubyCrystalPlayer>().Equipped =  true;
        }
    }
}
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Accessories
{
    public class EchoGoggles : ModItem
    {
        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            Item.width = 28;
            Item.height = 24;
            Item.rare = ItemRarityID.LightPurple;
            Item.accessory = true;
            Item.faceSlot = 14;
            Item.value = PriceByRarity.LightPurple_6;
            Item.vanity = true;
        }
        public override void UpdateEquip(Player player)
        {
            player.CanSeeInvisibleBlocks = true;
        }
        public override void UpdateVanity(Player player)
        {
            player.CanSeeInvisibleBlocks = true;
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.SpectreGoggles, 1);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 6000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
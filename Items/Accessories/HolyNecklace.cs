using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Accessories
{
    public class HolyNecklace : ModItem
    {
        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 32;
            Item.accessory = true;
            Item.value = PriceByRarity.LightRed_4;
            Item.rare = ItemRarityID.LightRed;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ModContent.ItemType<FrozenStarlight>(), 1);
            recipe.AddIngredient(ItemID.StarVeil);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 1000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }

        public override void UpdateEquip(Player player)
        {
            Lighting.AddLight((int)(player.position.X + (float)(player.width / 2) + (float)(8 * player.direction)) / 16, (int)(player.position.Y + 2f) / 16, 0.75f, 0.75f, 1.5f);
            player.starCloakItem = new Item(ItemID.StarCloak);
            player.longInvince = true;
        }

    }
}
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items.Materials;
using tsorcRevamp.Content.Items.Materials.Souls;
using tsorcRevamp.Content.Items.Materials.Souls.DarkSoul;

namespace tsorcRevamp.Content.Items.Accessories.Defensive
{
    public class CovenantOfArtorias : ModItem
    {
        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            Item.width = 24;
            Item.height = 26;
            Item.accessory = true;
            Item.value = PriceByRarity.Red_10;
            Item.rare = ItemRarityID.Quest;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ModContent.ItemType<SoulOfAttraidies>(), 1);
            recipe.AddIngredient(ModContent.ItemType<DarkSoulItem>(), 20000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            tsorcRevampPlayer modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            modPlayer.EnterTheAbyss = true;
            modPlayer.CovenantOfArtoriasEquipped = true;
        }

        public override void UpdateVanity(Player player)
        {
            tsorcRevampPlayer modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            modPlayer.EnterTheAbyss = true;
            modPlayer.CovenantOfArtoriasEquipped = true;
        }
    }
}

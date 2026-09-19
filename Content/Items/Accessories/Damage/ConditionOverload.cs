using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items.Materials;
using tsorcRevamp.Content.Items.Materials.Souls;
using tsorcRevamp.Content.Items.Materials.Souls.DarkSoul;

namespace tsorcRevamp.Content.Items.Accessories.Damage
{
    public class ConditionOverload : ModItem
    {
        public const float Dmg = 7f;
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
        }

        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            player.GetModPlayer<tsorcRevampPlayer>().ConditionOverload = true;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.HellstoneBar, 8);
            recipe.AddIngredient(ItemID.CursedFlame, 8);
            recipe.AddIngredient(ItemID.Ichor, 8);
            recipe.AddIngredient(ItemID.Stinger, 8);
            recipe.AddIngredient(ModContent.ItemType<DarkSoulItem>(), 12000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}

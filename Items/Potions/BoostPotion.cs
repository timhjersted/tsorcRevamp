using Terraria.ID;
using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Potions
{
    public class BoostPotion : ModItem
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Multiplies your movement speed by 20%");

        }

        public override void SetDefaults()
        {
            Item.width = 24;
            Item.height = 30;
            Item.useStyle = ItemUseStyleID.EatFood;
            Item.useAnimation = 15;
            Item.useTime = 15;
            Item.useTurn = true;
            Item.UseSound = SoundID.Item3;
            Item.maxStack = 9999;
            Item.consumable = true;
            Item.rare = ItemRarityID.Blue;
            Item.value = 5000;
            Item.buffType = ModContent.BuffType<Buffs.Boost>();
            Item.buffTime = 18000;
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe(4);
            recipe.AddIngredient(ItemID.SwiftnessPotion, 4);
            recipe.AddIngredient(ItemID.Deathweed, 4);
            recipe.AddIngredient(ItemID.Gel, 4);
            recipe.AddIngredient(ItemID.SoulofLight, 4);
            recipe.AddTile(TileID.Bottles);

            recipe.Register();
        }
    }
}

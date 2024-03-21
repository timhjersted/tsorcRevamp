using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Potions
{
    public class BoostPotion : ModItem
    {
        public static float MovementSpeedMultiplier = 20f;
        public static int Duration = 300;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(MovementSpeedMultiplier);
        public override void SetStaticDefaults()
        {
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
            Item.maxStack = Item.CommonMaxStack;
            Item.consumable = true;
            Item.rare = ItemRarityID.Blue;
            Item.value = 5000;
            Item.buffType = ModContent.BuffType<Buffs.Boost>();
            Item.buffTime = Duration * 60;
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

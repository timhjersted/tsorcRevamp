using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Potions
{
    public class BoostPotion : ModItem
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Increases critical strike chance by 5%");

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
            Item.maxStack = 30;
            Item.consumable = true;
            Item.rare = ItemRarityID.Blue;
            Item.value = 5000;
            Item.buffType = ModContent.BuffType<Buffs.Boost>();
            Item.buffTime = 14400;
        }
        public override void AddRecipes()
        {
            Terraria.Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.BattlePotion, 5);
            recipe.AddIngredient(ItemID.Deathweed, 5);
            recipe.AddIngredient(ItemID.Gel, 5);
            recipe.AddIngredient(ItemID.SoulofLight, 1);
            recipe.AddTile(TileID.Bottles);
            recipe.SetResult(this, 5);
            recipe.Register();
        }
    }
}

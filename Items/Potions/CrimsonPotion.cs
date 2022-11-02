using Terraria.ID;
using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Potions
{
    public class CrimsonPotion : ModItem
    {

        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Enemies within a ten tile radius are inflicted with \nCrimson Burn, which quickly drains life");
        }

        public override void SetDefaults()
        {
            Item.width = 14;
            Item.height = 24;
            Item.useStyle = ItemUseStyleID.EatFood;
            Item.useAnimation = 15;
            Item.useTime = 15;
            Item.useTurn = true;
            Item.UseSound = SoundID.Item3;
            Item.maxStack = 9999;
            Item.consumable = true;
            Item.rare = ItemRarityID.Blue;
            Item.value = 1000;
            Item.buffType = ModContent.BuffType<Buffs.CrimsonDrain>();
            Item.buffTime = 18000;
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.ThornsPotion, 1);
            recipe.AddIngredient(ItemID.SoulofNight, 1);
            recipe.AddTile(TileID.Bottles);

            recipe.Register();
        }
    }
}

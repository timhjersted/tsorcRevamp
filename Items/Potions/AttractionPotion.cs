using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Potions
{
    public class AttractionPotion : ModItem
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Summons a blood moon. Only usable at night." +
                             "\nAlso gives Battle Potion effect.");

        }

        public override void SetDefaults()
        {
            Item.width = 20;
            Item.height = 26;
            Item.useStyle = ItemUseStyleID.EatFood;
            Item.useAnimation = 15;
            Item.useTime = 15;
            Item.useTurn = true;
            Item.UseSound = SoundID.Item3;
            Item.maxStack = 30;
            Item.consumable = true;
            Item.rare = ItemRarityID.Blue;
            Item.value = 1000;
            Item.buffType = ModContent.BuffType<Buffs.Attraction>();
            Item.buffTime = 36000;
        }
        public override bool CanUseItem(Player player)
        {
            return !Main.dayTime;
        }
        public override void AddRecipes()
        {
            Terraria.Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.BottledWater, 5);
            recipe.AddIngredient(ItemID.Deathweed, 5);
            recipe.AddIngredient(ItemID.Vertebrae, 10);
            recipe.AddIngredient(ItemID.DeathweedSeeds, 5);
            recipe.AddIngredient(ItemID.Lens, 1);
            recipe.AddTile(TileID.Bottles);
            recipe.SetResult(this, 5);
            recipe.Register();

            Terraria.Recipe recipe2 = CreateRecipe();
            recipe2.AddIngredient(ItemID.BottledWater, 5);
            recipe2.AddIngredient(ItemID.Deathweed, 5);
            recipe2.AddIngredient(ItemID.RottenChunk, 10);
            recipe2.AddIngredient(ItemID.DeathweedSeeds, 5);
            recipe2.AddIngredient(ItemID.Lens, 1);
            recipe2.AddTile(TileID.Bottles);
            recipe2.SetResult(this, 5);
            recipe2.Register();
        }
    }
}

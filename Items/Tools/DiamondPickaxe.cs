using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Tools
{
    class DiamondPickaxe : ModItem
    {
        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            Item.width = 22;
            Item.height = 22;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useAnimation = 8;
            Item.useTime = 8;
            Item.autoReuse = true;
            Item.useTurn = true;
            Item.damage = 0;
            Item.pick = 54;
            Item.knockBack = 1;
        }

        public override bool? CanHitNPC(Player player, NPC target)
        {
            return false;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.CopperPickaxe, 1);
            recipe.AddIngredient(ItemID.Diamond, 6);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}

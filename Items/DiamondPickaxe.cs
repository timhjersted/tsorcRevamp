using Terraria.ModLoader;
using Terraria.ID;
using Terraria;

namespace tsorcRevamp.Items {
    class DiamondPickaxe : ModItem {
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("An ideal pickaxe for exploring..." +
                                "\nThe diamond tip has been imbued with magic from a celestial being." +
                                "\nBecause of this, it can do no harm to living creatures, but its tip breaks through blocks" +
                                "\nwith incredible ease... In fact, it moves so fast in your hand," +
                                "\nyou can hardly see it, yet using it feels effortless...");
        }

        public override void SetDefaults() {
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

        public override bool? CanHitNPC(Player player, NPC target) {
            return false;
        }

        public override void AddRecipes() {
            Recipe recipe = new Recipe(Mod);
            recipe.AddIngredient(ItemID.CopperPickaxe, 1);
            recipe.AddIngredient(ItemID.Diamond, 6);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }
    }
}

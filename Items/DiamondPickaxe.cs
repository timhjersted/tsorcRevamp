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
            item.width = 22;
            item.height = 22;
            item.useStyle = ItemUseStyleID.SwingThrow;
            item.useAnimation = 8;
            item.useTime = 8;
            item.autoReuse = true;
            item.useTurn = true;
            item.damage = 0;
            item.pick = 54;
            item.knockBack = 1;
        }

        public override bool? CanHitNPC(Player player, NPC target) {
            return false;
        }

        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.CopperPickaxe, 1);
            recipe.AddIngredient(ItemID.Diamond, 6);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }
    }
}

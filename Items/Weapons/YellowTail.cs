using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons {
    class YellowTail : ModItem {
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Shatters strong defenses with high penetration(multi - hit damage)");
        }

        public override void SetDefaults() {
            item.width = 35;
            item.height = 35;
            item.useStyle = ItemUseStyleID.Stabbing;
            item.useAnimation = 15;
            item.autoReuse = true;
            item.useTime = 15;
            item.maxStack = 1;
            item.damage = 17;
            item.knockBack = 4;
            item.scale = 1;
            item.UseSound = SoundID.Item1;
            item.rare = ItemRarityID.LightRed;
            item.value = 10000;
            item.melee = true;
            item.autoReuse = true;
        }

        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.GoldShortsword, 1);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 2000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }
    }
}

using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Melee {
    public class MythrilPoleWarAxe : ModItem {
        public override void SetStaticDefaults() {
            DisplayName.SetDefault("Mythril Pole War Axe");

        }

        public override void SetDefaults() {
            item.rare = ItemRarityID.LightRed;
            item.damage = 43;
            item.width = 54;
            item.height = 54;
            item.knockBack = 7;
            item.maxStack = 1;
            item.melee = true;
            item.scale = (float)1.35;
            item.useAnimation = 35;
            item.UseSound = SoundID.Item1;
            item.useStyle = ItemUseStyleID.SwingThrow;
            item.useTime = 21;
            item.value = 103500;
        }

        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);

            recipe.AddIngredient(ItemID.MythrilBar, 10);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 3000);

            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }


    }
}

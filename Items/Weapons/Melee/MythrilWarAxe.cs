using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Melee {
    public class MythrilWarAxe : ModItem {
        public override void SetStaticDefaults() {
            DisplayName.SetDefault("Mythril War Axe");

        }

        public override void SetDefaults() {
            item.rare = ItemRarityID.LightRed;
            item.damage = 39;
            item.width = 40;
            item.height = 40;
            item.knockBack = 6;
            item.maxStack = 1;
            item.melee = true;
            item.scale = (float).95;
            item.useAnimation = 26;
            item.UseSound = SoundID.Item1;
            item.useStyle = ItemUseStyleID.SwingThrow;
            item.useTime = 21;
            item.value = 93150;
        }

        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);

            recipe.AddIngredient(ItemID.MythrilBar, 9);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 2000);

            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }


    }
}

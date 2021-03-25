using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Melee {
    public class MythrilGreatWarhammer : ModItem {
        public override void SetStaticDefaults() {
            DisplayName.SetDefault("Mythril Great Warhammer");
        }

        public override void SetDefaults() {
            item.damage = 42;
            item.rare = ItemRarityID.LightRed;
            item.width = 46;
            item.height = 46;
            item.knockBack = 9;
            item.maxStack = 1;
            item.melee = true;
            item.scale = (float)1.35;
            item.useAnimation = 35;
            item.UseSound = SoundID.Item1;
            item.useStyle = ItemUseStyleID.SwingThrow;
            item.useTime = 21;
            item.value = 113850;
        }

        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);

            recipe.AddIngredient(ItemID.MythrilBar, 11);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 3000);

            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }


    }
}

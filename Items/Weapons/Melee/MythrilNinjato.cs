using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Melee {
    public class MythrilNinjato : ModItem {
        public override void SetStaticDefaults() {
            DisplayName.SetDefault("Mythril Ninjato");

        }

        public override void SetDefaults() {



            //item.prefixType=484;
            item.damage = 40;
            item.rare = ItemRarityID.LightRed;
            item.width = 52;
            item.height = 52;
            item.knockBack = 5;
            item.maxStack = 1;
            item.melee = true;
            item.scale = (float)1.15;
            item.useAnimation = 19;
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

using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Ranged {
    public class AncientGreatBow : ModItem {
        public override void SetStaticDefaults() {
            DisplayName.SetDefault("Ancient Great Bow");
        }

        public override void SetDefaults() {
            item.ranged = true;
            item.shoot = ProjectileID.PurificationPowder;


            item.damage = 25;
            item.height = 58;
            item.width = 16;
            item.knockBack = (float)5;
            item.maxStack = 1;
            item.noMelee = true;
            item.autoReuse = true;
            item.rare = ItemRarityID.Green;
            item.scale = (float)1;
            item.shootSpeed = (float)8;
            item.useAmmo = AmmoID.Arrow;
            item.useTime = 25;
            item.useAnimation = 25;
            item.UseSound = SoundID.Item5;
            item.useStyle = ItemUseStyleID.HoldingOut;
            item.value = 18000;
        }

        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);

            recipe.AddIngredient(ItemID.DemonBow, 1);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 4000);

            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }


    }
}

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


            item.damage = 24; //Demon Bow is 14
            item.height = 58;
            item.width = 16;
            item.knockBack = 1.5f; //DB is 1
            item.maxStack = 1;
            item.noMelee = true;
            item.autoReuse = true;
            item.rare = ItemRarityID.Green;
            item.scale = (float)1;
            item.shootSpeed = (float)7.5; //DB is 6.7
            item.useAmmo = AmmoID.Arrow;
            item.useTime = 25; //Same as DB
            item.useAnimation = 25;
            item.UseSound = SoundID.Item5;
            item.useStyle = ItemUseStyleID.HoldingOut;
            item.value = 18000;
        }

        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);

            recipe.AddIngredient(ItemID.DemonBow, 1);
            recipe.AddIngredient(ItemID.ShadowScale, 10);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 4000);

            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }


    }
}

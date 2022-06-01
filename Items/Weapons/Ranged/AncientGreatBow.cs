using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Ranged {
    public class AncientGreatBow : ModItem {
        public override void SetStaticDefaults() {
            DisplayName.SetDefault("Ancient Great Bow");
        }

        public override void SetDefaults() {
            Item.DamageType = DamageClass.Ranged;
            Item.shoot = ProjectileID.PurificationPowder;


            Item.damage = 24; //Demon Bow is 14
            Item.height = 58;
            Item.width = 16;
            Item.knockBack = 1.5f; //DB is 1
            Item.maxStack = 1;
            Item.noMelee = true;
            Item.autoReuse = true;
            Item.rare = ItemRarityID.Green;
            Item.scale = (float)1;
            Item.shootSpeed = (float)7.5; //DB is 6.7
            Item.useAmmo = AmmoID.Arrow;
            Item.useTime = 25; //Same as DB
            Item.useAnimation = 25;
            Item.UseSound = SoundID.Item5;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.value = PriceByRarity.Green_2;
        }

        public override void AddRecipes() {
            Recipe recipe = CreateRecipe();

            recipe.AddIngredient(ItemID.DemonBow, 1);
            recipe.AddIngredient(ItemID.ShadowScale, 10);
            recipe.AddIngredient(Mod.Find<ModItem>("DarkSoul").Type, 4000);

            recipe.AddTile(TileID.DemonAltar);
            
            recipe.Register();
        }


    }
}

using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Melee {
    public class DragoonLance : ModItem {

        public override void SetStaticDefaults() {
            DisplayName.SetDefault("Dragoon Lance");
            Tooltip.SetDefault("A spear forged from the fang of the Dragoon Serpent.");
        }

        public override void SetDefaults() {
            Item.damage = 100;
            Item.knockBack = 15f;

            Item.useStyle = ItemUseStyleID.Shoot;
            Item.useAnimation = 11;
            Item.useTime = 1;
            Item.shootSpeed = 8;
            
            Item.height = 74;
            Item.width = 74;

            Item.DamageType = DamageClass.Melee;
            Item.noMelee = true;
            Item.noUseGraphic = true;

            Item.value = PriceByRarity.Yellow_8;
            Item.rare = ItemRarityID.Yellow;
            Item.maxStack = 1;
            Item.UseSound = SoundID.Item1;
            Item.shoot = ModContent.ProjectileType<Projectiles.DragoonLance>();

        }

        public override void AddRecipes() {
            Recipe recipe = new Recipe(Mod);
            recipe.AddIngredient(Mod.GetItem("GaeBolg"), 1);
            recipe.AddIngredient(Mod.GetItem("DarkSoul"), 70000);
            recipe.SetResult(this, 1);
            recipe.AddTile(TileID.DemonAltar);
            recipe.AddRecipe();
        }
    }
}

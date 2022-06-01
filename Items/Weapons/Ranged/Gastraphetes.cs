using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Ranged {
    public class Gastraphetes : ModItem {
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Blacken the sky");
        }

        public override void SetDefaults() {
            Item.DamageType = DamageClass.Ranged;
            Item.shoot = ProjectileID.PurificationPowder;

            Item.damage = 50;
            Item.width = 50;
            Item.height = 18;
            Item.useTime = 16;
            Item.useAnimation = 16;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;
            Item.knockBack = 3;
            Item.value = PriceByRarity.LightPurple_6;
            Item.rare = ItemRarityID.LightPurple;
            Item.UseSound = SoundID.Item5;
            Item.autoReuse = true;

            Item.shootSpeed = 12f;
            Item.useAmmo = AmmoID.Arrow;
        }

        public override void AddRecipes() {
            Recipe recipe = new Recipe(Mod);
            recipe.AddIngredient(ItemID.HallowedRepeater, 1);
            recipe.AddIngredient(Mod.GetItem("DarkSoul"), 20000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this);
            recipe.AddRecipe();
        }
    }
}
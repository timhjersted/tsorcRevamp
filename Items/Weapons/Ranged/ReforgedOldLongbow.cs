using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Ranged {
    class ReforgedOldLongbow : ModItem {
        public override string Texture => "tsorcRevamp/Items/Weapons/Ranged/OldLongbow";
        public override void SetDefaults() {

            Item.damage = 16;
            Item.height = 66;
            Item.width = 16;
            Item.DamageType = DamageClass.Ranged;
            Item.knockBack = 4f;
            Item.maxStack = 1;
            Item.noMelee = true;
            Item.rare = ItemRarityID.Blue;
            Item.scale = 0.9f;
            Item.shoot = AmmoID.Arrow;
            Item.shootSpeed = 13f;
            Item.useAmmo = AmmoID.Arrow;
            Item.useAnimation = 25;
            Item.useTime = 25;
            Item.UseSound = SoundID.Item5;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.value = PriceByRarity.Blue_1;

        }
        public override void AddRecipes() {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(Mod.Find<ModItem>("OldLongbow").Type);
            recipe.AddTile(Mod.GetTile("SweatyCyclopsForge"));
            
            recipe.Register();
        }
    }
}

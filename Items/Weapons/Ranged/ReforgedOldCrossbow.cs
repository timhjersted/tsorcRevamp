using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Ranged {
    class ReforgedOldCrossbow : ModItem {
        public override string Texture => "tsorcRevamp/Items/Weapons/Ranged/Crossbow";
        public override void SetDefaults() {
            Item.damage = 19;
            Item.width = 28;
            Item.height = 14;
            Item.knockBack = 4;
            Item.maxStack = 1;
            Item.DamageType = DamageClass.Ranged;
            Item.scale = 1;
            Item.crit = 16;
            Item.useAnimation = 45;
            Item.useTime = 45;
            Item.rare = ItemRarityID.Blue;
            Item.UseSound = SoundID.Item5;
            Item.useAmmo = Mod.Find<ModItem>("Bolt").Type;
            Item.shoot = ProjectileID.PurificationPowder;
            Item.shootSpeed = 10;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.value = PriceByRarity.Blue_1;
            Item.noMelee = true;
        }
        public override void AddRecipes() {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(Mod.Find<ModItem>("OldCrossbow").Type);
            recipe.AddTile(Mod.GetTile("SweatyCyclopsForge"));
            
            recipe.Register();
        }
    }
}

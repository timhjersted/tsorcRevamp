using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Magic {
    class WoodenFlute : ModItem {
        public override void SetDefaults() {
            Item.damage = 10;
            Item.height = 10;
            Item.knockBack = 4;
            Item.maxStack = 1;
            Item.rare = ItemRarityID.White;
            Item.scale = 1;
            Item.shootSpeed = 10;
            Item.DamageType = DamageClass.Magic;
            Item.mana = 2;
            Item.useAnimation = 45;
            Item.UseSound = SoundID.Item21;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.useTime = 45;
            Item.value = PriceByRarity.White_0;
            Item.width = 34;
            Item.shoot = ModContent.ProjectileType<Projectiles.MusicalNote>();
        }
        
        public override void AddRecipes() {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.Wood, 1);
            recipe.AddIngredient(Mod.Find<ModItem>("DarkSoul").Type, 220);
            recipe.AddTile(TileID.DemonAltar);
            
            recipe.Register();
        }
    }
}

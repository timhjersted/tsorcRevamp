using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Magic {
    class ChaosTome : ModItem {
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Casts a purple flame that can pass through solid objects.");
        }
        public override void SetDefaults() {
            Item.width = 28;
            Item.height = 30;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.useAnimation = 25;
            Item.useTime = 25;
            Item.damage = 60;
            Item.knockBack = 4f;
            Item.mana = 8;
            Item.rare = ItemRarityID.Pink;
            Item.UseSound = SoundID.Item8;
            Item.DamageType = DamageClass.Magic;
            Item.noMelee = true;
            Item.useTurn = true;
            Item.autoReuse = true;
            Item.value = PriceByRarity.Pink_5;
            Item.shootSpeed = 8;
            Item.shoot = ModContent.ProjectileType<Projectiles.ChaosBall2>();
        }

        public override void AddRecipes() {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.SpellTome, 1);
            recipe.AddIngredient(ItemID.MeteoriteBar, 15);
            recipe.AddIngredient(ItemID.SoulofNight, 15);
            recipe.AddIngredient(ItemID.SoulofSight, 1);
            recipe.AddIngredient(Mod.Find<ModItem>("DarkSoul").Type, 40000);
            recipe.AddTile(TileID.DemonAltar);
            
            recipe.Register();
        }
    }
}

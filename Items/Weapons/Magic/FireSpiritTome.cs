using Terraria.ModLoader;
using Terraria.ID;

namespace tsorcRevamp.Items.Weapons.Magic {
    class FireSpiritTome : ModItem {
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Summons fire spirits at incredible speed.");
        }

        public override void SetDefaults() {
            Item.width = 28;
            Item.height = 30;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.useAnimation = 5;
            Item.useTime = 5;
            Item.maxStack = 1;
            Item.damage = 22;
            Item.knockBack = 8;
            Item.autoReuse = true;
            Item.scale = 1.3f;
            Item.UseSound = SoundID.Item9;
            Item.rare = ItemRarityID.LightRed;
            Item.shootSpeed = 11;
            Item.mana = 5;
            Item.value = PriceByRarity.LightRed_4;
            Item.DamageType = DamageClass.Magic;
            Item.shoot = ModContent.ProjectileType<Projectiles.FireSpirit2>();
        }

        public override void AddRecipes() {
            Recipe recipe = new Recipe(Mod);
            recipe.AddIngredient(ItemID.SpellTome, 1);
            recipe.AddIngredient(ItemID.Fireblossom, 50);
            recipe.AddIngredient(ItemID.AdamantiteBar, 1);
            recipe.AddIngredient(ItemID.SoulofNight, 5);
            recipe.AddIngredient(Mod.GetItem("DarkSoul"), 45000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }
    }
}

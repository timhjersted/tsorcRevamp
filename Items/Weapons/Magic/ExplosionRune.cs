using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Magic {
    class ExplosionRune : ModItem {

        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Projectile that explodes with 5 small fireballs at the point of impact.");
        }
        public override void SetDefaults() {
            item.consumable = false;
            item.damage = 40;
            item.height = 28;
            item.knockBack = 4;
            item.rare = ItemRarityID.Orange;
            item.shootSpeed = 11;
            item.autoReuse = true;
            item.magic = true;
            item.noMelee = true;
            item.useAnimation = 21;
            item.UseSound = SoundID.Item21;
            item.useStyle = ItemUseStyleID.HoldingOut;
            item.useTime = 21;
            item.value = 200000;
            item.width = 20;
            item.mana = 16;
            item.shoot = ModContent.ProjectileType<Projectiles.ExplosionBall>();
        }

        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.SpellTome, 1);
            recipe.AddIngredient(ItemID.Fireblossom, 30);
            recipe.AddIngredient(ItemID.SoulofLight, 10);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 20000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }
    }
}

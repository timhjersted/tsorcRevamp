using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Magic {
    public class FireBombRune : ModItem {
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Projectile that explodes with a wall of 9 fireballs at the point of impact");
        }
        public override void SetDefaults() {
            item.damage = 46;
            item.height = 28;
            item.knockBack = 4;
            item.rare = ItemRarityID.LightPurple;
            item.shootSpeed = 10;
            item.magic = true;
            item.noMelee = true;
            item.useAnimation = 45;
            item.UseSound = SoundID.Item21;
            item.useStyle = ItemUseStyleID.HoldingOut;
            item.useTime = 45;
            item.value = PriceByRarity.LightPurple_6;
            item.width = 20;
            item.mana = 50;
            item.shoot = ModContent.ProjectileType<Projectiles.FireBombBall>();
        }
        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.SoulofNight, 10);
            recipe.AddIngredient(ItemID.AdamantiteBar, 1);
            recipe.AddIngredient(mod.GetItem("FireFieldRune"), 1);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 15000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }
    }
}

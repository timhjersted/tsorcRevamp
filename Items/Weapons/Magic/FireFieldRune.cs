using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Magic {
    public class FireFieldRune : ModItem {
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Projectile that explodes with a sustained burning flame at the point of impact");
        }
        public override void SetDefaults() {
            item.damage = 40;
            item.height = 28;
            item.knockBack = 4;
            item.rare = ItemRarityID.Orange;
            item.shootSpeed = 6;
            item.magic = true;
            item.noMelee = true;
            item.useAnimation = 30;
            item.UseSound = SoundID.Item21;
            item.useStyle = ItemUseStyleID.HoldingOut;
            item.useTime = 30;
            item.value = 200000;
            item.width = 20;
            item.mana = 20;
            item.shoot = ModContent.ProjectileType<Projectiles.FireFieldBall>();
        }
        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.SpellTome, 1);
            recipe.AddIngredient(ItemID.SoulofNight, 8);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 12000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }
    }
}

using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Magic {
    public class EnergyBombRune : ModItem {
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Summons 9 electric energy orbs in a square at the point of impact.");
        }
        public override void SetDefaults() {
            Item.damage = 36;
            Item.height = 28;
            Item.knockBack = 4;
            Item.rare = ItemRarityID.LightPurple;
            Item.shootSpeed = 6;
            Item.DamageType = DamageClass.Magic;
            Item.noMelee = true;
            Item.useAnimation = 21;
            Item.UseSound = SoundID.Item21;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.useTime = 21;
            Item.value = PriceByRarity.LightPurple_6;
            Item.width = 20;
            Item.mana = 50;
            Item.shoot = ModContent.ProjectileType<Projectiles.EnergyBombBall>();
        }
        public override void AddRecipes() {
            Recipe recipe = new Recipe(Mod);
            recipe.AddIngredient(ItemID.SoulofLight, 10);
            recipe.AddIngredient(Mod.GetItem("EnergyFieldRune"), 1);
            recipe.AddIngredient(Mod.GetItem("DarkSoul"), 15000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }
    }
}

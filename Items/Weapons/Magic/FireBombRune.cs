using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Magic
{
    public class FireBombRune : ModItem
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Projectile that explodes with a wall of 9 fireballs at the point of impact");
        }
        public override void SetDefaults()
        {
            Item.damage = 46;
            Item.height = 28;
            Item.knockBack = 4;
            Item.rare = ItemRarityID.LightPurple;
            Item.shootSpeed = 10;
            Item.DamageType = DamageClass.Magic;
            Item.noMelee = true;
            Item.useAnimation = 45;
            Item.UseSound = SoundID.Item21;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.useTime = 45;
            Item.value = PriceByRarity.LightPurple_6;
            Item.width = 20;
            Item.mana = 50;
            Item.shoot = ModContent.ProjectileType<Projectiles.FireBombBall>();
        }
        public override void AddRecipes()
        {
            Terraria.Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.SoulofNight, 10);
            recipe.AddIngredient(ItemID.AdamantiteBar, 1);
            recipe.AddIngredient(Mod.Find<ModItem>("FireFieldRune").Type, 1);
            recipe.AddIngredient(Mod.Find<ModItem>("DarkSoul").Type, 15000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}

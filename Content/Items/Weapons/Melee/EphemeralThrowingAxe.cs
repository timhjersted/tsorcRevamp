using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items.Materials;
using tsorcRevamp.Content.Items.Materials.Souls;
using tsorcRevamp.Content.Items.Materials.Souls.DarkSoul;

namespace tsorcRevamp.Content.Items.Weapons.Melee
{
    class EphemeralThrowingAxe : ModItem
    {

        public override void SetStaticDefaults()
        {
            // Tooltip.SetDefault("Passes through solid walls");
        }

        public override void SetDefaults()
        {
            Item.rare = ItemRarityID.Green;
            Item.damage = 40;
            Item.width = 30;
            Item.height = 52;
            Item.knockBack = 3;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.DamageType = DamageClass.MeleeNoSpeed;
            Item.shootSpeed = 9;
            Item.useAnimation = 22;
            Item.UseSound = SoundID.Item1;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useTime = 22;
            Item.value = 150000;  
            Item.shoot = ModContent.ProjectileType<Projectiles.EphemeralThrowingAxeProj>();
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ModContent.ItemType<ThrowingAxe>());
            recipe.AddIngredient(ModContent.ItemType<EphemeralDust>(), 15);
            recipe.AddIngredient(ModContent.ItemType<DarkSoulItem>(), 8000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}

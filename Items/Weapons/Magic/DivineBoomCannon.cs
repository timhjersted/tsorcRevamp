using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Magic
{
    class DivineBoomCannon : ModItem
    {
        public override string Texture => "tsorcRevamp/Items/Weapons/Magic/DivineSpark";
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Obliterates everything upon contact.");
        }
        public override void SetDefaults()
        {
            Item.width = 24;
            Item.height = 28;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.useAnimation = 2;
            Item.useTime = 1;
            Item.damage = 30000;
            Item.autoReuse = true;
            Item.rare = ItemRarityID.Expert;
            Item.shootSpeed = 1;
            Item.mana = 3;
            Item.noMelee = true;
            Item.value = 20000;
            Item.DamageType = DamageClass.Magic;
            Item.channel = true;
            Item.shoot = ModContent.ProjectileType<Projectiles.MasterBuster>();
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(Mod.Find<ModItem>("DivineSpark").Type, 1);
            recipe.AddIngredient(Mod.Find<ModItem>("Epilogue").Type, 1);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}

using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Melee
{
    public class SupremeDragoonLance : ModItem
    {
        public override string Texture => "tsorcRevamp/Items/Weapons/Melee/DragoonLance";
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Supreme Dragoon Lance");
            Tooltip.SetDefault("An all-powerful spear forged from the fang of the Dragoon Serpent.");
        }

        public override void SetDefaults()
        {
            Item.damage = 300;
            Item.knockBack = 15f;

            Item.useStyle = ItemUseStyleID.Shoot;
            Item.useAnimation = 11;
            Item.useTime = 1;
            Item.shootSpeed = 8;

            Item.height = 50;
            Item.width = 50;

            Item.DamageType = DamageClass.Melee;
            Item.noMelee = true;
            Item.noUseGraphic = true;

            Item.value = PriceByRarity.Purple_11;
            Item.rare = ItemRarityID.Purple;
            Item.maxStack = 1;
            Item.UseSound = SoundID.Item1;
            Item.shoot = ModContent.ProjectileType<Projectiles.DragoonLance>();

        }

        public override void AddRecipes()
        {
            Terraria.Recipe recipe = CreateRecipe();
            recipe.AddIngredient(Mod.Find<ModItem>("DragoonLance").Type, 1);
            recipe.AddIngredient(Mod.Find<ModItem>("FlameOfTheAbyss").Type, 10);
            recipe.AddIngredient(Mod.Find<ModItem>("SoulOfArtorias").Type, 1);
            recipe.AddIngredient(Mod.Find<ModItem>("GuardianSoul").Type, 1);
            recipe.AddIngredient(Mod.Find<ModItem>("DarkSoul").Type, 170000);

            recipe.AddTile(TileID.DemonAltar);
            recipe.Register();
        }
    }
}

using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Throwing
{
    class BlackFirebomb : ModItem
    {
        public override bool Autoload(ref string name) => !ModContent.GetInstance<tsorcRevampConfig>().LegacyMode;
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Black Firebomb");
            Tooltip.SetDefault("Explodes, dealing fire damage in a small area" +
                               "\nSets the ground and enemies alight");
        }
        public override void SetDefaults()
        {
            item.rare = ItemRarityID.Blue;
            item.width = 22;
            item.damage = 200;
            item.height = 24;
            item.useStyle = ItemUseStyleID.SwingThrow;
            item.knockBack = 10f;
            item.noUseGraphic = true;
            item.noMelee = true;
            item.value = 500;
            item.shoot = mod.ProjectileType("BlackFirebomb");
            item.shootSpeed = 6.5f;
            item.useAnimation = 50;
            item.useTime = 50;
            item.UseSound = SoundID.Item1;
            item.consumable = true;
            item.maxStack = 999;
            item.thrown = true;
        }
        public override void AddRecipes()
        {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(mod.GetItem("Firebomb"), 2);
            recipe.AddIngredient(mod.GetItem("CharcoalPineResin"));
            recipe.AddIngredient(ItemID.SoulofNight);
            recipe.SetResult(this, 2);
            recipe.AddRecipe();
        }
    }
}
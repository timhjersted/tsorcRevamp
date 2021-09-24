using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Throwing
{
    class Firebomb : ModItem
    {
        public override bool Autoload(ref string name) => !ModContent.GetInstance<tsorcRevampConfig>().LegacyMode;
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Firebomb");
            Tooltip.SetDefault("Explodes, dealing damage in a small area");
        }
        public override void SetDefaults()
        {

            item.rare = ItemRarityID.Blue;
            item.width = 22;
            item.damage = 100;
            item.height = 24;
            item.useStyle = ItemUseStyleID.SwingThrow;
            item.knockBack = 8f;
            item.noUseGraphic = true;
            item.noMelee = true;
            item.value = 500;
            item.shoot = mod.ProjectileType("Firebomb");
            item.shootSpeed = 6.5f;
            item.useAnimation = 50;
            item.useTime = 50;
            item.crit = 4;
            item.UseSound = SoundID.Item1;
            item.consumable = true;
            item.maxStack = 999;
            item.thrown = true;
        }
        public override void AddRecipes()
        {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.Grenade);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 10);
            recipe.SetResult(this);
            recipe.AddRecipe();
        }
    }
}
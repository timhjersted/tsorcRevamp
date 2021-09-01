using Terraria.ModLoader;
using Terraria.ID;

namespace tsorcRevamp.Items.Weapons.Magic {
    class DivineBoomCannon : ModItem {
        public override string Texture => "tsorcRevamp/Items/Weapons/Magic/DivineSpark";
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Obliterates everything upon contact.");
        }
        public override void SetDefaults() {
            item.width = 24;
            item.height = 28;
            item.useStyle = ItemUseStyleID.HoldingOut;
            item.useAnimation = 2;
            item.useTime = 1;
            item.damage = 30000;
            item.autoReuse = true;
            item.rare = ItemRarityID.Expert;
            item.shootSpeed = 1;
            item.mana = 3;
            item.noMelee = true;
            item.value = 20000;
            item.magic = true;
            item.channel = true;
            item.shoot = ModContent.ProjectileType<Projectiles.MasterBuster>();
        }

        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(mod.GetItem("DivineSpark"), 1);
            recipe.AddIngredient(mod.GetItem("CursedSoul"), 200);
            recipe.AddIngredient(mod.GetItem("Humanity"), 100);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 600000);
            recipe.AddIngredient(mod.GetItem("Epilogue"), 1);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }
    }
}

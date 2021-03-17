using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons {
    class HighCaliberRifle : ModItem {

        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Incredible damage at the cost of 2.5 second cooldown between shots" + 
                                "\nRemember to hold your breath.");
        }

        public override void SetDefaults() {
            item.damage = 700;
            item.height = 22;
            item.noMelee = true;
            item.autoReuse = true;
            item.ranged = true;
            item.rare = ItemRarityID.Pink;
            item.scale = 1;
            item.shoot = ProjectileID.PurificationPowder;
            item.shootSpeed = 20;
            item.useAmmo = AmmoID.Bullet;
            item.useAnimation = 150;
            item.UseSound = SoundID.Item11;
            item.useStyle = ItemUseStyleID.HoldingOut;
            item.useTime = 150;
            item.value = 397000;
            item.width = 66;
        }

        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.Megashark, 1);
            recipe.AddIngredient(ItemID.IllegalGunParts, 1);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 80000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }
    }
}

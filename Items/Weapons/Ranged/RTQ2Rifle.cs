using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Ranged {
    class RTQ2Rifle : ModItem {

        public override void SetStaticDefaults() {
            DisplayName.SetDefault("RTQ2 Rifle");
        }

        public override void SetDefaults() {
            item.width = 24;
            item.height = 28;
            item.useStyle = ItemUseStyleID.HoldingOut;
            item.useAnimation = 9;
            item.useTime = 9;
            item.damage = 42;
            item.autoReuse = true;
            item.UseSound = SoundID.Item12;
            item.rare = ItemRarityID.Blue;
            item.shoot = ProjectileID.PurificationPowder;
            item.shootSpeed = 11;
            item.noMelee = true;
            item.value = 500000;
            item.useAmmo = AmmoID.Bullet;
            item.ranged = true;
        }

        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.Megashark, 1);
            recipe.AddIngredient(ItemID.SoulofMight, 1);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 20000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }
    }
}

using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Ranged {
    class Freezethrower : ModItem {

        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Uses Gel as ammo." +
                                "\nHas a chance to freeze");
        }
        public override void SetDefaults() {
            item.width = 54;
            item.height = 16;
            item.useStyle = ItemUseStyleID.HoldingOut;
            item.useAnimation = 30;
            item.useTime = 5;
            item.damage = 66;
            item.knockBack = 2;
            item.autoReuse = true;
            item.UseSound = SoundID.Item34;
            item.rare = ItemRarityID.LightPurple;
            item.shootSpeed = 9;
            item.useAmmo = AmmoID.Gel;
            item.noMelee = true;
            item.value = PriceByRarity.LightPurple_6;
            item.ranged = true;
            item.shoot = ModContent.ProjectileType<Projectiles.Freezethrower>();
        }

        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.Flamethrower, 1);
            recipe.AddIngredient(ItemID.SoulofNight, 30);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 40000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }
    }
}

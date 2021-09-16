using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Ranged {
    class Crossbow : ModItem {

        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Uses Bolts as ammo. 10 are crafted with" +
                                "\none wood and two Dark Souls at a Demon Altar" +
                                 "\nBolts pierce once");
        }
        public override void SetDefaults() {
            item.damage = 16;
            item.height = 28;
            item.knockBack = 4;
            item.crit = 6;
            item.noMelee = true;
            item.ranged = true;
            item.shoot = ProjectileID.PurificationPowder;
            item.useAmmo = mod.ItemType("Bolt");
            item.shootSpeed = 10;
            item.useAnimation = 45;
            item.UseSound = SoundID.Item5;
            item.useStyle = ItemUseStyleID.HoldingOut;
            item.useTime = 45;
            item.value = 1400;
            item.width = 12;
        }

        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.Wood, 10);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 150);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }
    }
}

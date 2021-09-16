using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Magic {
    class WandOfFire : ModItem {
        public override void SetStaticDefaults() {
            DisplayName.SetDefault("Wand of Fire");
            Item.staff[item.type] = true;
        }
        public override void SetDefaults() {
            item.autoReuse = true;
            item.width = 12;
            item.height = 17;
            item.useStyle = ItemUseStyleID.HoldingOut;
            item.useAnimation = 25;
            item.useTime = 25;
            item.maxStack = 1;
            item.damage = 20;
            item.knockBack = 1;
            item.mana = 7;
            item.UseSound = SoundID.Item20;
            item.shootSpeed = 12;
            item.noMelee = true;
            item.value = 14000;
            item.magic = true;
            item.rare = ItemRarityID.Orange;
            item.shoot = ModContent.ProjectileType<Projectiles.FireBall>();
        }

        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(mod.GetItem("WoodenWand"), 1);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 2300);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }
    }
}

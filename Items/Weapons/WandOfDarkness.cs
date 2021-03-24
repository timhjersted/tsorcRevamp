using Microsoft.Xna.Framework;

using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons {
    class WandOfDarkness : ModItem {

        public override void SetStaticDefaults() {
            DisplayName.SetDefault("Wand of Darkness");
            Item.staff[item.type] = true;
        }
        public override void SetDefaults() {
            item.autoReuse = true;
            item.width = 12;
            item.height = 17;
            item.useStyle = ItemUseStyleID.HoldingOut;
            item.useAnimation = 25;
            item.useTime = 25;
            item.damage = 11;
            item.knockBack = 1f;
            item.mana = 2;
            item.UseSound = SoundID.Item8;
            item.shootSpeed = 6;
            item.noMelee = true;
            item.value = 13000;
            item.magic = true;
            item.shoot = ModContent.ProjectileType<Projectiles.ShadowBall>();
        }
        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(mod.GetItem("WoodenWand"), 1);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 150);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }
    }
}

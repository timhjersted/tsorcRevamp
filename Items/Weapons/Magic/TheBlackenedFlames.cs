using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Magic {
    public class TheBlackenedFlames : ModItem {
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Has a chance to drain enemies' health quickly");

        }

        public override void SetDefaults() {
            item.width = 28;
            item.height = 30;
            item.useStyle = ItemUseStyleID.HoldingOut;
            item.useAnimation = 55;
            item.useTime = 55;
            item.damage = 73;
            item.knockBack = 6;
            item.scale = 0.9f;
            item.UseSound = SoundID.Item20;
            item.rare = ItemRarityID.Pink;
            item.shootSpeed = 10;
            item.crit = 6;
            item.mana = 16;
            item.noMelee = true;
            item.value = 250000;
            item.magic = true;
            item.shoot = ModContent.ProjectileType<Projectiles.BlackFire>();
        }

        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.SpellTome, 1);
            recipe.AddIngredient(ItemID.Fireblossom, 50);
            recipe.AddIngredient(ItemID.SoulofNight, 20);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 65000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }


    }
}

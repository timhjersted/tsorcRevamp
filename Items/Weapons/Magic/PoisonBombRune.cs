using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Magic {
    public class PoisonBombRune : ModItem {
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Creates 9 poison bombs on impact\n" + "Superior area denial, drains enemy life with the deadliest poison\n" + "Not necessarily a direct upgrade");
        }
        public override void SetDefaults() {
            item.damage = 16;
            item.height = 28;
            item.knockBack = 3;
            item.rare = ItemRarityID.Orange;
            item.shootSpeed = 7;
            item.magic = true;
            item.noMelee = true;
            item.autoReuse = true;
            item.mana = 70;
            item.useAnimation = 60;
            item.UseSound = SoundID.Item21;
            item.useStyle = ItemUseStyleID.HoldingOut;
            item.useTime = 60;
            item.value = 100000;
            item.width = 20;
            item.shoot = ModContent.ProjectileType<Projectiles.PoisonBombBall>();
        }
        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.Stinger, 10);
            recipe.AddIngredient(mod.GetItem("PoisonFieldRune"), 1);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 10000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }
    }
}

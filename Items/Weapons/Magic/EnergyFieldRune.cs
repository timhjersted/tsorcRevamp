using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Magic {
    public class EnergyFieldRune : ModItem {

        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Creates an electric energy orb at the point of impact." +
                                "\nLasts for a few seconds.");
        }
        public override void SetDefaults() {
            item.damage = 30;
            item.height = 28;
            item.knockBack = 4;
            item.rare = ItemRarityID.Orange;
            item.shootSpeed = 6;
            item.magic = true;
            item.noMelee = true;
            item.useAnimation = 45;
            item.UseSound = SoundID.Item21;
            item.useStyle = ItemUseStyleID.HoldingOut;
            item.useTime = 45;
            item.value = 200000;
            item.width = 20;
            item.mana = 25;
            item.shoot = ModContent.ProjectileType<Projectiles.EnergyFieldBall>();
        }

        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.SpellTome, 1);
            recipe.AddIngredient(ItemID.SoulofLight, 5);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 8000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }
    }
}

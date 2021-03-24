using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons {
    public class EnergyBombRune : ModItem {
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Summons 9 electric energy orbs in a square at the point of impact.");
        }
        public override void SetDefaults() {
            item.damage = 36;
            item.height = 28;
            item.knockBack = 4;
            item.rare = ItemRarityID.Orange;
            item.shootSpeed = 6;
            item.magic = true;
            item.noMelee = true;
            item.useAnimation = 21;
            item.UseSound = SoundID.Item21;
            item.useStyle = ItemUseStyleID.HoldingOut;
            item.useTime = 21;
            item.value = 300000;
            item.width = 20;
            item.mana = 50;
            item.shoot = ModContent.ProjectileType<Projectiles.EnergyBombBall>();
        }
        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.SoulofLight, 10);
            recipe.AddIngredient(mod.GetItem("EnergyFieldRune"), 1);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 15000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }
    }
}

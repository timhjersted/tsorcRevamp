using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Melee {

    public class EnchantedMorningStar : ModItem {

        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Enchantment does increased damage against mages and ghosts.");
        }

        public override void SetDefaults() {
            item.width = 28;
            item.height = 28;
            item.useStyle = ItemUseStyleID.HoldingOut;
            item.channel = true;
            item.scale = 0.8f;
            item.useAnimation = 44;
            item.useTime = 44;
            item.damage = 33;
            item.knockBack = 6f;
            item.UseSound = SoundID.Item1;
            item.rare = ItemRarityID.Green;
            item.shootSpeed = 12;
            item.noUseGraphic = true;
            item.noMelee = true;
            item.value = PriceByRarity.Green_2;
            item.melee = true;
            item.shoot = ModContent.ProjectileType<Projectiles.EnchantedMorningStar>();
        }

        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(mod.GetItem("OldMorningStar"), 1);
            recipe.AddIngredient(mod.GetItem("EphemeralDust"), 30);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 6000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }
    }
}

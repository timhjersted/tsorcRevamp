using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Melee {
    public class AncientHolyLance : ModItem {

        public override void SetStaticDefaults() {
            DisplayName.SetDefault("Ancient Holy Lance");
            Tooltip.SetDefault("Bright Holy Spear.");
        }

        public override void SetDefaults() {
            item.damage = 60;
            item.knockBack = 9.5f;

            item.useStyle = ItemUseStyleID.HoldingOut;
            item.useAnimation = 11;
            item.useTime = 7;
            item.shootSpeed = 8;
            //item.shoot = ProjectileID.DarkLance;
            
            item.height = 50;
            item.width = 50;

            item.melee = true;
            item.noMelee = true;
            item.noUseGraphic = true;

            item.value = 20000;
            item.rare = ItemRarityID.Pink;
            item.maxStack = 1;
            item.UseSound = SoundID.Item1;
            item.shoot = ModContent.ProjectileType<Projectiles.AncientHolyLance>();

        }

        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.MythrilHalberd);
            recipe.AddIngredient(ItemID.SoulofLight);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 6000);
            recipe.SetResult(this, 1);
            recipe.AddTile(TileID.DemonAltar);
            recipe.AddRecipe();
        }
    }
}

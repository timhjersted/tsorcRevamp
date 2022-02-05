using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Ranged {
    public class ArtemisBow : ModItem {
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("A bow forged to slay demon-gods.\nHold FIRE to charge\nArrows are faster and more accurate when the bow is charged");

        }

        public override void SetDefaults() {
            item.ranged = true;
            item.shoot = ModContent.ProjectileType<Projectiles.ArtemisBowHeld>();
            item.channel = true;

            item.damage = 220; //was 370
            item.width = 14;
            item.height = 28;
            item.useTime = 60;
            item.useAnimation = 60;
            item.useStyle = ItemUseStyleID.HoldingOut;
            item.noMelee = true;
            item.noUseGraphic = true;
            item.knockBack = 15f;
            item.value = PriceByRarity.LightRed_4;
            item.rare = ItemRarityID.LightRed;
            item.UseSound = SoundID.Item7;

            item.shootSpeed = 18f;
        }
        

        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);

            recipe.AddIngredient(ItemID.GoldBow, 1);
            recipe.AddIngredient(ItemID.AdamantiteBar, 12);
            recipe.AddIngredient(ItemID.SoulofLight, 18);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 75000);

            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }
    }
}

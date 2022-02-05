using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Ranged {
    class SagittariusBow : ModItem {

        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Fires two arrows\nHold FIRE to charge\nArrows are faster and more accurate when the bow is charged");
        }
        public override void SetDefaults() {
            item.ranged = true;
            item.shoot = ModContent.ProjectileType<Projectiles.SagittariusBowHeld>();
            item.channel = true;
            item.damage = 548;
            item.width = 14;
            item.height = 28;
            item.useTime = 60;
            item.useAnimation = 60;
            item.useStyle = ItemUseStyleID.HoldingOut;
            item.noMelee = true;
            item.noUseGraphic = true;
            item.knockBack = 5f;
            item.value = PriceByRarity.Red_10;
            item.rare = ItemRarityID.Red;
            item.UseSound = SoundID.Item7;
            item.shootSpeed = 21f;
        }

        public override bool CanUseItem(Player player) {
            return player.ownedProjectileCounts[ModContent.ProjectileType<Projectiles.SagittariusBowHeld>()] <= 0;
        }

        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(mod.GetItem("ArtemisBow"), 1);
            recipe.AddIngredient(mod.GetItem("BlueTitanite"), 5);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 90000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
        }
    }
}

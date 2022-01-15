using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Ranged {
    public class CernosPrime : ModItem {

        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Fires three arrows. \nHold FIRE to charge. \nArrows are faster and more accurate when the bow is charged.");
        }

        public override void SetDefaults() {
            item.ranged = true;
            item.shoot = ModContent.ProjectileType<Projectiles.CernosPrimeHeld>();
            item.channel = true;

            item.damage = 795; 
            item.width = 24;
            item.height = 48;
            item.useTime = 48; 
            item.useAnimation = 48;
            item.reuseDelay = 4;
            item.useStyle = ItemUseStyleID.HoldingOut;
            item.noMelee = true;
            item.noUseGraphic = true;
            item.knockBack = 4f;
            item.value = PriceByRarity.Purple_11;
            item.rare = ItemRarityID.Purple;
            item.UseSound = SoundID.Item7;

            item.shootSpeed = 24f;

            //item.useAmmo = AmmoID.Arrow; //dont do this! it'll just shoot the arrow instead of using the bow draw animation.
            //TODO investigate displaying the ammo count on the bow

        }

        public override bool CanUseItem(Player player) {
            return player.ownedProjectileCounts[ModContent.ProjectileType<Projectiles.CernosPrimeHeld>()] <= 0;
        }

        public override void AddRecipes() {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ModContent.ItemType<SagittariusBow>(), 1);
            recipe.AddIngredient(ModContent.ItemType<FlameOfTheAbyss>(), 15);
            recipe.AddIngredient(ModContent.ItemType<GhostWyvernSoul>(), 1);
            recipe.AddIngredient(ModContent.ItemType<CursedSoul>(), 30);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 300000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this, 1);
            recipe.AddRecipe();
        }

    }
}

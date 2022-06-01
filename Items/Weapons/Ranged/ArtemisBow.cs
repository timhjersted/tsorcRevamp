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
            Item.DamageType = DamageClass.Ranged;
            Item.shoot = ModContent.ProjectileType<Projectiles.ArtemisBowHeld>();
            Item.channel = true;

            Item.damage = 220; //was 370
            Item.width = 14;
            Item.height = 28;
            Item.useTime = 60;
            Item.useAnimation = 60;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.knockBack = 15f;
            Item.value = PriceByRarity.LightRed_4;
            Item.rare = ItemRarityID.LightRed;
            Item.UseSound = SoundID.Item7;

            Item.shootSpeed = 18f;
        }
        

        public override void AddRecipes() {
            Recipe recipe = CreateRecipe();

            recipe.AddIngredient(ItemID.GoldBow, 1);
            recipe.AddIngredient(ItemID.AdamantiteBar, 12);
            recipe.AddIngredient(ItemID.SoulofLight, 18);
            recipe.AddIngredient(Mod.Find<ModItem>("DarkSoul").Type, 75000);

            recipe.AddTile(TileID.DemonAltar);
            
            recipe.Register();
        }
    }
}

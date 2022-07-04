using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Ranged
{
    class SagittariusBow : ModItem
    {

        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Fires two arrows\nHold FIRE to charge\nArrows are faster and more accurate when the bow is charged");
        }
        public override void SetDefaults()
        {
            Item.DamageType = DamageClass.Ranged;
            Item.shoot = ModContent.ProjectileType<Projectiles.SagittariusBowHeld>();
            Item.channel = true;
            Item.damage = 548;
            Item.width = 14;
            Item.height = 28;
            Item.useTime = 60;
            Item.useAnimation = 60;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.knockBack = 5f;
            Item.value = PriceByRarity.Red_10;
            Item.rare = ItemRarityID.Red;
            Item.UseSound = SoundID.Item7;
            Item.shootSpeed = 21f;
            Item.useAmmo = AmmoID.Arrow;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback) {
            Projectile.NewProjectile(source, position, velocity, ModContent.ProjectileType<Projectiles.SagittariusBowHeld>(), damage, knockback, player.whoAmI, type);
            return false;
        }

        public override void AddRecipes()
        {
            Terraria.Recipe recipe = CreateRecipe();
            recipe.AddIngredient(Mod.Find<ModItem>("ArtemisBow").Type, 1);
            recipe.AddIngredient(Mod.Find<ModItem>("BlueTitanite").Type, 5);
            recipe.AddIngredient(Mod.Find<ModItem>("DarkSoul").Type, 50000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}

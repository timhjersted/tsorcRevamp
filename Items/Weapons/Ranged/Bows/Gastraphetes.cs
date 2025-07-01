using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;
using tsorcRevamp.Projectiles.Ranged.Ammo;
using Terraria.DataStructures;

namespace tsorcRevamp.Items.Weapons.Ranged.Bows
{
    public class Gastraphetes : ModItem
    {
        public override void SetStaticDefaults()
        {
            // Tooltip.SetDefault("Blacken the sky");
        }

        public override void SetDefaults()
        {
            Item.DamageType = DamageClass.Ranged;
            Item.shoot = ProjectileID.PurificationPowder;

            Item.damage = 40;
            Item.width = 50;
            Item.height = 18;
            Item.useTime = 11;
            Item.useAnimation = 11;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;
            Item.knockBack = 3;
            Item.value = PriceByRarity.LightPurple_6;
            Item.rare = ItemRarityID.LightPurple;
            Item.UseSound = SoundID.Item5;
            Item.autoReuse = true;
            Item.shootSpeed = 15f;
            Item.useAmmo = AmmoID.Arrow;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            //Shoots an additional Black Arrow above the cursor 
            Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI);

            Vector2 cursorPosition = Main.MouseWorld; 
            Vector2 spawnPosition = new Vector2(cursorPosition.X + Main.rand.Next(-200, 201), player.Center.Y - (55 * 16));

            Vector2 directionToCursor = cursorPosition - spawnPosition;
            directionToCursor.Normalize(); 
            Vector2 shootVelocity = directionToCursor * Main.rand.NextFloat(10f, 11f); 

            Projectile.NewProjectile(
                source,
                spawnPosition,
                shootVelocity,
                ModContent.ProjectileType<BlackArrow>(),
                damage / 2,
                knockback / 3,
                player.whoAmI
            );

            return false; 
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.HallowedRepeater, 1);
            recipe.AddIngredient(ItemID.SoulofNight, 10);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 20000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}
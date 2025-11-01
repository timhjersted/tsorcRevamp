using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.DataStructures;
using tsorcRevamp.Items.Materials;
using tsorcRevamp.Items.Weapons.Magic.Tomes;

namespace tsorcRevamp.Items.Weapons.Ranged.Bows
{
    class ForgottenIceBow : ModItem
    {

        public override void SetStaticDefaults()
        {
            /* Tooltip.SetDefault("Casts magic shards of ice from your bow." +
                                "\nAttuned with the greatest powers when wielded by mages." +
                                "\nEach shot can be channeled with the powers of your mind once in the air." +
                                "\nChanneling is useful for directing the shot directly above your enemies for maximum damage"); */
        }

        public override void SetDefaults()
        {
            Item.damage = 150;
            Item.height = 54;
            Item.knockBack = 4;
            Item.noMelee = true;
            Item.DamageType = DamageClass.Ranged;
            Item.rare = ModContent.RarityType<DarkBlue>();
            Item.channel = true;
            Item.autoReuse = true;
            Item.scale = 1f;
            Item.shootSpeed = 33;
            Item.useAnimation = 16;
            Item.UseSound = SoundID.Item5;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.useAmmo = AmmoID.Arrow;
            Item.useTime = 16;
            Item.value = PriceByRarity.Purple_11;
            Item.width = 28;
            Item.shoot = ModContent.ProjectileType<Projectiles.Ice5Ball>();
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
            {
                Projectile.NewProjectile(source, position, velocity, ModContent.ProjectileType<Projectiles.Ice5Ball>(), damage, knockback, player.whoAmI);

                Vector2 lowPos = position + new Vector2(0, 15);
                Projectile.NewProjectile(source, lowPos, velocity, type, damage, knockback, player.whoAmI);

                Vector2 highPos = position + new Vector2(0, -15);
                Projectile.NewProjectile(source, highPos, velocity, type, damage, knockback, player.whoAmI);

                return false; 
            }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ModContent.ItemType<ForgottenIceBowScroll>(), 1);
            recipe.AddIngredient(ModContent.ItemType<SoulOfArtorias>(), 1);
            recipe.AddIngredient(ModContent.ItemType<Humanity>(), 9);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 160000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}

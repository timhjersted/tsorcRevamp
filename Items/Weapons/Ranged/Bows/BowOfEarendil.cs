using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;
using tsorcRevamp.Projectiles.Ranged;
using Microsoft.Xna.Framework;

namespace tsorcRevamp.Items.Weapons.Ranged.Bows
{
    class BowOfEarendil : ModItem
    {
        private int useCounter = 0;

        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            Item.damage = 90;
            Item.height = 58;
            Item.width = 20;
            Item.DamageType = DamageClass.Ranged;
            Item.knockBack = 4f;
            Item.noMelee = true;
            Item.rare = ItemRarityID.Pink;
            Item.scale = 1f;
            Item.shoot = AmmoID.Arrow;
            Item.shootSpeed = 16;
            Item.useAmmo = AmmoID.Arrow;
            Item.useAnimation = 20;
            Item.useTime = 20;
            Item.UseSound = SoundID.Item5;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.value = PriceByRarity.Pink_5;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.GoldBow, 1);
            recipe.AddIngredient(ItemID.SoulofMight, 10);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 40000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }

         public override void ModifyShootStats(Player player, ref Vector2 position, ref Vector2 velocity, ref int type, ref int damage, ref float knockback)
        {
            if (type == ProjectileID.WoodenArrowFriendly)
            {
                type = ProjectileID.HolyArrow;
                velocity = velocity.SafeNormalize(Vector2.Zero) * (Item.shootSpeed + 3.8f); 
                damage = damage + 8; 
            }
        }
        public override bool Shoot(Player player, Terraria.DataStructures.EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            useCounter++;

            if (useCounter >= 4)
            {
                useCounter = 0;

                Vector2 velocityUp = velocity.RotatedBy(MathHelper.ToRadians(-8));
                Vector2 velocityDown = velocity.RotatedBy(MathHelper.ToRadians(8));

                Projectile.NewProjectile(source, position, velocityUp, ModContent.ProjectileType<ElfinArrow>(), damage, knockback, player.whoAmI);
                Projectile.NewProjectile(source, position, velocityDown, ModContent.ProjectileType<ElfinArrow>(), damage, knockback, player.whoAmI);
            }

            return true;
        }
    }
}
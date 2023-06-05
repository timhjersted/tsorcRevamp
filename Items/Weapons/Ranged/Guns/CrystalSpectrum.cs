using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Weapons.Ranged.Guns
{
    public class CrystalSpectrum : ModItem
    {
        public override void SetStaticDefaults()
        {
            // Tooltip.SetDefault("Converts bullets into a beam of crystal light, which inherits their properties");//\n" +
                //"Fire rate oscillates over time");
        }
        public override void SetDefaults()
        {
            Item.DamageType = DamageClass.Ranged;
            Item.noMelee = true;
            Item.useTime = Item.useAnimation = 20;
            Item.damage = 200;
            Item.knockBack = 1;
            Item.autoReuse = true;
            Item.shootSpeed = 16;
            Item.useAmmo = AmmoID.Bullet;
            Item.rare = ItemRarityID.Cyan;
            Item.value = PriceByRarity.Cyan_9;
            Item.UseSound = SoundID.Item12;
            Item.shoot = 10;
            Item.height = 50;
            Item.width = 32;
            Item.useStyle = ItemUseStyleID.Shoot;
        }


        public override Vector2? HoldoutOffset()
        {
            return new Vector2(-16f, 0f);
        }


        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            Projectile.NewProjectile(source, player.Center, velocity, ModContent.ProjectileType<Projectiles.Ranged.CrystalRay>(), damage, knockback, player.whoAmI, type);
            return false;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.VortexBeater);
            recipe.AddIngredient(ItemID.CrystalShard, 50);
            recipe.AddIngredient(ModContent.ItemType<GuardianSoul>());
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 60000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.AddCondition(tsorcRevampWorld.SHM3Downed);

            //recipe.Register();
        }
    }
}

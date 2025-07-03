using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;
using tsorcRevamp.Projectiles.Ranged.Ammo;
using Terraria.DataStructures;
using Microsoft.Xna.Framework;

namespace tsorcRevamp.Items.Weapons.Ranged.Bows
{
    public class ShadowFury : ModItem
    {
        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            Item.DamageType = DamageClass.Ranged;
            Item.shoot = ProjectileID.PurificationPowder; 
            Item.damage = 43;
            Item.crit = 3;
            Item.height = 46;
            Item.width = 18;
            Item.knockBack = 4.8f;
            Item.noMelee = true;
            Item.rare = ItemRarityID.LightPurple;
            Item.shootSpeed = 11f;
            Item.useAmmo = AmmoID.Arrow;
            Item.useTime = 16;
            Item.useAnimation = 16;
            Item.UseSound = SoundID.Item5;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.value = PriceByRarity.fromItem(Item);
        }

        public override void ModifyShootStats(Player player, ref Vector2 position, ref Vector2 velocity, ref int type, ref int damage, ref float knockback)
        {
            if (type == ProjectileID.WoodenArrowFriendly)
            {
                type = ProjectileID.ShadowFlameArrow; 
            }

            Projectile.NewProjectile(
                player.GetSource_ItemUse(Item), 
                position, 
                velocity.RotatedByRandom(MathHelper.ToRadians(30)) * 0.8f,
                ProjectileID.ShadowFlame, 
                damage / 2, 
                knockback, 
                player.whoAmI,
                0f, 0f, 
                DamageClass.Ranged.Type 
            );
        }

        public override bool? UseItem(Player player)
        {
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item103, player.position);
            return null;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();

            recipe.AddIngredient(ItemID.ShadowFlameBow);
            recipe.AddIngredient(ItemID.AdamantiteBar, 5);
            recipe.AddIngredient(ItemID.SoulofNight, 6);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 30000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.Register();
        }
    }
}
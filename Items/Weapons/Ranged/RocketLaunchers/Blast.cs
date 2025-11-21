using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Weapons.Ranged.RocketLaunchers
{
    class Blast : ModItem
    {
        public override void SetStaticDefaults()
        {
            ItemID.Sets.IsRangedSpecialistWeapon[Item.type] = true;
        }

    public override void SetDefaults()
        {
            Item.damage = 220;
            Item.width = 108;
            Item.height = 48;
            Item.DamageType = DamageClass.Ranged;
            Item.autoReuse = true;
            Item.knockBack = 15f;
            Item.noMelee = true;
            Item.shoot = ProjectileID.MiniNukeRocketI;
            Item.shootSpeed = 12f;
            Item.useAmmo = AmmoID.Rocket;
            Item.useAnimation = 16;
            Item.useTime = 16;
            Item.UseSound = SoundID.Item61;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.rare = ItemRarityID.Purple;
            Item.value = PriceByRarity.Purple_11;
        }

        public override void UseStyle(Player player, Rectangle rectangle)
        {
            float backX = 48f;
            float downY = 0f;
            float cosRot = (float)Math.Cos(player.itemRotation);
            float sinRot = (float)Math.Sin(player.itemRotation);
            player.itemLocation.X = player.itemLocation.X - backX * cosRot * player.direction - downY * sinRot * player.gravDir;
            player.itemLocation.Y = player.itemLocation.Y - backX * sinRot * player.direction + downY * cosRot * player.gravDir;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            int proj = Projectile.NewProjectile(source, position, velocity, type, damage, knockback, player.whoAmI);
            Main.projectile[proj].ai[1] = 1f;

            int flameDamage = (int)(damage * 0.66f);

            Vector2 lowPos = velocity.RotatedBy(MathHelper.ToRadians(10)) * (1f / 2f);
            Projectile.NewProjectile(source, position, lowPos, ProjectileID.Flames, flameDamage, knockback, player.whoAmI);

            Vector2 highPos = velocity.RotatedBy(MathHelper.ToRadians(-10)) * (1f / 2f);
            Projectile.NewProjectile(source, position, highPos, ProjectileID.Flames, flameDamage, knockback, player.whoAmI);

            return false;
        }

        public override void HoldItem(Player player)
        {
            player.scope = true;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.RocketLauncher, 1);
            recipe.AddIngredient(ItemID.ExplosivePowder, 500);
            recipe.AddIngredient(ModContent.ItemType<RedTitanite>(), 10);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 90000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.Register();
        }
    }
}
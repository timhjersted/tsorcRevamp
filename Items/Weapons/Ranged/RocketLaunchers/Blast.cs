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
            Item.damage = 500;
            Item.width = 108;
            Item.height = 48;
            Item.DamageType = DamageClass.Ranged;
            Item.autoReuse = true;
            Item.knockBack = 20f;
            Item.noMelee = true;
            Item.shoot = ProjectileID.MiniNukeRocketI;
            Item.shootSpeed = 10f;
            Item.useAmmo = AmmoID.Rocket;
            Item.useAnimation = 25;
            Item.useTime = 25;
            Item.UseSound = SoundID.Item61;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.rare = ModContent.RarityType<DarkBlue>();
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
            return false;
        }

        /*public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.RocketLauncher, 1);
            recipe.AddIngredient(ModContent.ItemType<RedTitanite>(), 5);
            recipe.AddIngredient(ModContent.ItemType<GuardianSoul>(), 1);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 70000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.Register();
        }*/
    }
}
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Weapons.Ranged.RocketLaunchers
{
    class TacticalBazooka : ModItem
    {
        public override void SetStaticDefaults()
        {
            ItemID.Sets.IsRangedSpecialistWeapon[Item.type] = true;
        }

    public override void SetDefaults()
        {
            Item.damage = 70;
            Item.width = 108;
            Item.height = 48;
            Item.DamageType = DamageClass.Ranged;
            Item.autoReuse = true;
            Item.knockBack = 7f;
            Item.noMelee = true;
            Item.shoot = ProjectileID.MiniNukeRocketI;
            Item.shootSpeed = 12f;
            Item.useAmmo = AmmoID.Rocket;
            Item.useAnimation = 25;
            Item.useTime = 25;
            Item.UseSound = SoundID.Item61;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.rare = ItemRarityID.Yellow;
            Item.value = PriceByRarity.Yellow_8;
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

        public override void ModifyShootStats(Player player, ref Vector2 position, ref Vector2 velocity, ref int type, ref int damage, ref float knockback)
        {
            switch (type)
            {
                case ProjectileID.RocketI:
                case ProjectileID.RocketII:
                case ProjectileID.RocketIII:
                case ProjectileID.RocketIV:
                    type = ProjectileID.MiniNukeRocketI;
                    break;
            }
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
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 40000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.Register();
        }
    }
}
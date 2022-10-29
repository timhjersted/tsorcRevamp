using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Ranged.Guns
{
    public class AntimatRifle : ModItem
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Antimat Rifle");
            Tooltip.SetDefault("Unbelievable damage at the cost of a 2.5 second cooldown between shots \n" +
                                "Fires piercing high-velocity rounds that punch through thin walls\n" +
                                "Damage increases with enemy armor");
        }

        public override void SetDefaults()
        {

            //item.prefixType=96;
            Item.autoReuse = true;
            Item.damage = 3000;
            Item.width = 78;
            Item.height = 26;
            Item.knockBack = 5;
            Item.maxStack = 1;
            Item.noMelee = true;
            Item.rare = ItemRarityID.Red;
            Item.scale = (float)0.9;
            Item.useAmmo = AmmoID.Bullet;
            Item.DamageType = DamageClass.Ranged;
            Item.shoot = AmmoID.Bullet;
            Item.shootSpeed = 10;
            //item.pretendType=96;
            Item.useAnimation = 150;
            Item.useTime = 150;
            Item.UseSound = SoundID.Item36;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.value = PriceByRarity.Red_10;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();

            recipe.AddIngredient(ItemID.SniperRifle, 1);
            recipe.AddIngredient(ModContent.ItemType<DestructionElement>());
            recipe.AddIngredient(ModContent.ItemType<SoulOfChaos>());
            recipe.AddIngredient(ModContent.ItemType<Humanity>(), 10);
            recipe.AddIngredient(ModContent.ItemType<CursedSoul>(), 100);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 240000);

            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
        public override void HoldItem(Player player)
        {
            player.scope = true;
        }

        public override void UseStyle(Player player, Rectangle rectangle)
        {
            float backX = 24f; // move the weapon back
            float downY = 0f; // and down
            float cosRot = (float)Math.Cos(player.itemRotation);
            float sinRot = (float)Math.Sin(player.itemRotation);
            //Align
            player.itemLocation.X = player.itemLocation.X - backX * cosRot * player.direction - downY * sinRot * player.gravDir;
            player.itemLocation.Y = player.itemLocation.Y - backX * sinRot * player.direction + downY * cosRot * player.gravDir;
        }

        public override void ModifyShootStats(Player player, ref Vector2 position, ref Vector2 velocity, ref int type, ref int damage, ref float knockback)
        {
            type = ModContent.ProjectileType<Projectiles.AntiMaterialRound>();
        }
    }
}

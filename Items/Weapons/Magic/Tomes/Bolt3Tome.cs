﻿using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Weapons.Magic.Tomes
{
    public class Bolt3Tome : ModItem
    {
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Bolt 3 Tome");
            /* Tooltip.SetDefault("A lost tome fabled to deal great damage.\n" +
                                "\nElectrifies and paralyzes enemies" +
                                "Only mages will be able to realize this tome's full potential. \n" +
                                "Can be upgraded with 85,000 Dark Souls"); */

        }

        public override void SetDefaults()
        {

            Item.damage = 120;
            Item.height = 10;
            Item.width = 34;
            Item.knockBack = 0.1f;
            Item.autoReuse = true;
            Item.rare = ItemRarityID.LightRed;
            Item.shootSpeed = 15f;
            Item.DamageType = DamageClass.Magic;
            Item.noMelee = true;
            Item.mana = 20;
            Item.useAnimation = 25;
            Item.UseSound = SoundID.Item21;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.useTime = 25;
            Item.value = PriceByRarity.LightRed_4;
            Item.shoot = ModContent.ProjectileType<Projectiles.Magic.Bolt3Lightning>();
        }
        public override bool Shoot(Player player, Terraria.DataStructures.EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 speed, int type, int damage, float knockBack)
        {
            if (player.wet)
            {
                type = ModContent.ProjectileType<Projectiles.Magic.Bolt4Lightning>();
                damage = (int)(damage * 1.3f);
            }

            return true;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.SoulofLight, 3);
            recipe.AddIngredient(ModContent.ItemType<Bolt2Tome>(), 1);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 25000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }


    }
}

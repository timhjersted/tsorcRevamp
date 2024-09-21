﻿using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Throwing
{
    class SporePowder : ModItem
    {

        public override void SetDefaults()
        {
            Item.width = 22;
            Item.height = 26;
            Item.maxStack = Item.CommonMaxStack;
            Item.damage = 13;
            Item.rare = ItemRarityID.Green;
            Item.value = 50;
            Item.consumable = true;
            Item.useTurn = true;
            Item.useAnimation = 15;
            Item.useTime = 15;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.UseSound = SoundID.Item1;
            Item.noMelee = true;
            Item.shootSpeed = 4;
            Item.shoot = ModContent.ProjectileType<Projectiles.SporePowder>();
            Item.DamageType = DamageClass.Ranged;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe(5);
            recipe.AddIngredient(ItemID.JungleSpores, 1);
            recipe.AddTile(TileID.Bottles);
            recipe.Register();
        }
    }
}

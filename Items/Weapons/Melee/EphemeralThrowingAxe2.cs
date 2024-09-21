﻿using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Weapons.Melee
{
    class EphemeralThrowingAxe2 : ModItem
    {

        public override string Texture => "tsorcRevamp/Items/Weapons/Melee/EphemeralThrowingAxe";
        public override void SetStaticDefaults()
        {
            // Tooltip.SetDefault("An enchanted melee weapon that can be thrown through walls.\n" + "It does double damage against mages and other magic users.");
        }
        public override void SetDefaults()
        {
            Item.rare = ItemRarityID.LightRed;
            Item.consumable = false;
            Item.damage = 50;
            Item.height = 34;
            Item.knockBack = 7;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.DamageType = DamageClass.MeleeNoSpeed;
            Item.shootSpeed = 10;
            Item.useAnimation = 19;
            Item.UseSound = SoundID.Item1;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.useTime = 19;
            Item.value = 150000;
            Item.width = 22;
            Item.shoot = ModContent.ProjectileType<Projectiles.EphemeralThrowingAxeProj2>();
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ModContent.ItemType<EphemeralThrowingAxe>());
            //recipe.AddIngredient(ItemID.SoulofNight, 8);
            recipe.AddIngredient(ItemID.AdamantiteBar, 1);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 12000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}

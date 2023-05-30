using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Magic
{
    public class ArcaneLightrifle : ModItem
    {
        public override void SetStaticDefaults()
        {
            /* Tooltip.SetDefault("Charges a focused beam of piercing light" +
                "\nReflects up to two times, massively amplifying its damage with each"); */
        }

        public override void SetDefaults()
        {

            Item.width = 28;
            Item.height = 30;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.useTurn = true;
            Item.useAnimation = 25;
            Item.useTime = 25;
            Item.maxStack = 1;
            Item.damage = 170;
            Item.autoReuse = true;
            Item.knockBack = (float)4;
            Item.scale = (float)1;
            Item.UseSound = SoundID.Item34;
            Item.rare = ItemRarityID.Red;
            Item.shootSpeed = (float)10;
            Item.crit = 2;
            Item.mana = 50;
            Item.noMelee = true;
            Item.value = PriceByRarity.Red_10;
            Item.DamageType = DamageClass.Magic;

            Item.channel = true;

            Item.shoot = ModContent.ProjectileType<Projectiles.Magic.ArcaneLightrifle>();
        }

        public override void ModifyManaCost(Player player, ref float reduce, ref float mult)
        {
            player.manaRegenDelay = 180;
            mult = 0;
        }

        public override Vector2? HoldoutOffset()
        {
            return new Vector2(-8, 0);
        }

        public override bool CanUseItem(Player player)
        {            
            if (player.statMana <= (int)(50 * player.manaCost) ||  player.GetModPlayer<tsorcRevampPlayer>().BearerOfTheCurse && player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceCurrent < 30)
            {
                return false;
            }
            return player.ownedProjectileCounts[ModContent.ProjectileType<Projectiles.Magic.ArcaneLightrifle>()] <= 0;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            //recipe.AddIngredient(ItemID.LaserMachinegun, 1);
            recipe.AddIngredient(ModContent.ItemType<BlueTitanite>(), 5);
            recipe.AddIngredient(ModContent.ItemType<GuardianSoul>(), 1);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 80000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.Register();
        }
    }
}

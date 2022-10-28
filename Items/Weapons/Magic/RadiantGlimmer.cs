using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Magic
{
    public class RadiantGlimmer : ModItem
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Casts a burst of radiant shards that linger, shredding enemies that move through them");

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
            //item.projectile=Sandstorm;
            Item.rare = ItemRarityID.Red;
            Item.shootSpeed = (float)10;
            Item.crit = 2;
            Item.mana = 14;
            Item.noMelee = true;
            Item.value = PriceByRarity.Red_10;
            Item.DamageType = DamageClass.Magic;

            Item.shoot = ModContent.ProjectileType<Projectiles.Magic.RadiantGlimmer>();
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.SpellTome, 1);
            //recipe.AddIngredient(ItemID.MeteoriteBar, 25);
            //recipe.AddIngredient(ItemID.SandBlock, 150);
            recipe.AddIngredient(Mod.Find<ModItem>("FlameOfTheAbyss").Type, 20);
            recipe.AddIngredient(ModContent.ItemType<WhiteTitanite>(),5);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 120000);
            recipe.AddTile(TileID.DemonAltar);

            //recipe.Register();
        }
    }
}

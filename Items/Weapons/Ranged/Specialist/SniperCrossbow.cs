using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Ammo;
using tsorcRevamp.Items.Materials;
using tsorcRevamp.Projectiles.Ranged.Ammo;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.Items.Weapons.Ranged.Specialist
{
    class SniperCrossbow : ModItem
    {
        public override void SetStaticDefaults()
        {
            ItemID.Sets.IsRangedSpecialistWeapon[Item.type] = true;
        }
        public override void SetDefaults()
        {
            Item.damage = 110;
            Item.crit = 15;
            Item.knockBack = 6;
            Item.crit = 16;
            Item.shootSpeed = 13;
            Item.useTime = 38;
            Item.useAnimation = 38;
            Item.width = 72;
            Item.height = 34;
            Item.noMelee = true;
            Item.DamageType = DamageClass.Ranged;
            Item.shoot = ProjectileID.PurificationPowder;
            Item.useAmmo = ModContent.ItemType<Bolt>();
            Item.UseSound = SoundID.Item99;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.rare = ItemRarityID.LightRed;
            Item.value = PriceByRarity.LightRed_4;
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ModContent.ItemType<HeavyCrossbow>());
            recipe.AddIngredient(ItemID.CobaltBar, 10);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 9000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
        public override void ModifyShootStats(Player player, ref Vector2 position, ref Vector2 velocity, ref int type, ref int damage, ref float knockback)
        {
			if (type == ModContent.ProjectileType<BoltProjectile>()) 
            {
				type = ModContent.ProjectileType<SniperBoltProjectile>(); 
			}
		}
        public override Vector2? HoldoutOffset() 
        {
		    return new Vector2(2f, -2f);
        }
    }
}

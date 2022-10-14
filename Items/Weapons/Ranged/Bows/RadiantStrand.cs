using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Ranged.Bows
{
    public class RadiantStrand : ModItem
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Fires arrows of light which leave a damaging trail of energy as they fly");
        }
        public override void SetDefaults()
        {
            Item.DamageType = DamageClass.Ranged;
            Item.noMelee = true;
            Item.useTime = Item.useAnimation = 2; //brrrrrr
            Item.damage = 38;
            Item.knockBack = 1;
            Item.autoReuse = true;
            Item.shootSpeed = 16;
            Item.useAmmo = AmmoID.Arrow;
            Item.rare = ItemRarityID.Cyan;
            Item.value = PriceByRarity.Cyan_9;
            Item.shoot = 10;
            Item.height = 50;
            Item.width = 32;
            Item.useStyle = ItemUseStyleID.Shoot;

        }

        public override void ModifyShootStats(Player player, ref Vector2 position, ref Vector2 velocity, ref int type, ref int damage, ref float knockback)
        {
            type = ModContent.ProjectileType<Projectiles.Ranged.RadiantStrand>();
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.VenusMagnum);
            recipe.AddIngredient(ModContent.ItemType<WhiteTitanite>());
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 90000);
            recipe.AddTile(TileID.DemonAltar);

            //recipe.Register();
        }
    }
}

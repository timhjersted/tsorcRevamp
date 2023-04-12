using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Projectiles;

namespace tsorcRevamp.Items.Weapons.Ranged
{
    public class PyroclasticFlow : ModItem
    {
        public override void SetStaticDefaults()
        {
            // Tooltip.SetDefault("Precision rifle that launches an explosive fireball every 3 shots");
        }

        public override void SetDefaults()
        {
            Item.DamageType = DamageClass.Ranged;
            Item.noMelee = true;
            Item.useTime = Item.useAnimation = 15; //brrrrrr
            Item.damage = 150;
            Item.knockBack = 1;
            Item.autoReuse = true;
            Item.shootSpeed = 16;
            Item.useAmmo = AmmoID.Bullet;
            Item.rare = ItemRarityID.Cyan;
            Item.value = PriceByRarity.Cyan_9;
            Item.shoot = 10;
            Item.height = 50;
            Item.width = 32;
            Item.useStyle = ItemUseStyleID.Shoot;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.Revolver);
            recipe.AddIngredient(ItemID.SoulofLight, 9);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 6000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }

        int tallyCounter;
        public override void ModifyShootStats(Player player, ref Vector2 position, ref Vector2 velocity, ref int type, ref int damage, ref float knockback)
        {
            if(tallyCounter == 2)
            {
                type = ModContent.ProjectileType<Projectiles.Ranged.PyroclasticFlow>();
                damage *= 2;
                tallyCounter = 0;
            }
            else
            {
                tallyCounter++;
            }
        }
    }
}

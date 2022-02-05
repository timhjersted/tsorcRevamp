using Terraria;
using Microsoft.Xna.Framework;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Ranged
{
    public class Blaster : ModItem
    {

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Blaster");
            Tooltip.SetDefault("Relatively short range but pierces once. Doesn't require ammo");
        }

        public override void SetDefaults()
        {
            item.damage = 20;
            item.ranged = true;
            item.crit = 4;
            item.width = 44;
            item.height = 24;
            item.useTime = 24;
            item.useAnimation = 24;
            item.useStyle = ItemUseStyleID.HoldingOut;
            item.noMelee = true;
            item.knockBack = 3f;
            item.value = 20000;
            item.scale = 0.7f;
            item.rare = ItemRarityID.Blue;
            item.shoot = mod.ProjectileType("BlasterShot");
            item.shootSpeed = 14f;
        }

        public override void AddRecipes()
        {


            ModRecipe recipe = new ModRecipe(mod);
            //recipe.AddIngredient(null, "oddscrapmetal", 10);
            recipe.AddIngredient(ItemID.IronBar, 5);
            recipe.AddIngredient(ItemID.Diamond, 2);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 1200);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this);
            recipe.AddRecipe();

            recipe = new ModRecipe(mod);
            //recipe.AddIngredient(null, "oddscrapmetal", 10);
            recipe.AddIngredient(ItemID.LeadBar, 5);
            recipe.AddIngredient(ItemID.Diamond, 2);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 1200);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this);
            recipe.AddRecipe();
        }

        public override Vector2? HoldoutOffset()
        {
            return new Vector2(2, 2);
        }

        public override bool Shoot(Player player, ref Vector2 position, ref float speedX, ref float speedY, ref int type, ref int damage, ref float knockBack)
        {

            Vector2 muzzleOffset = Vector2.Normalize(new Vector2(speedX, speedY)) * 20f;
            if (Collision.CanHit(position, 0, 0, position + muzzleOffset, 0, 0))
            {
                position += muzzleOffset;
            }
            return true;


        }
    }
}

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
            Item.damage = 20;
            Item.ranged = true;
            Item.crit = 4;
            Item.width = 44;
            Item.height = 24;
            Item.useTime = 24;
            Item.useAnimation = 24;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;
            Item.knockBack = 3f;
            Item.value = 20000;
            Item.scale = 0.7f;
            Item.rare = ItemRarityID.Blue;
            Item.shoot = Mod.Find<ModProjectile>("BlasterShot").Type;
            Item.shootSpeed = 14f;
        }

        public override void AddRecipes()
        {


            Recipe recipe = new Recipe(Mod);
            //recipe.AddIngredient(null, "oddscrapmetal", 10);
            recipe.AddIngredient(ItemID.IronBar, 5);
            recipe.AddIngredient(ItemID.Diamond, 2);
            recipe.AddIngredient(Mod.GetItem("DarkSoul"), 1200);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this);
            recipe.AddRecipe();

            recipe = new Recipe(Mod);
            //recipe.AddIngredient(null, "oddscrapmetal", 10);
            recipe.AddIngredient(ItemID.LeadBar, 5);
            recipe.AddIngredient(ItemID.Diamond, 2);
            recipe.AddIngredient(Mod.GetItem("DarkSoul"), 1200);
            recipe.AddTile(TileID.DemonAltar);
            recipe.SetResult(this);
            recipe.AddRecipe();
        }

        public override Vector2? HoldoutOffset()
        {
            return new Vector2(2, 2);
        }

        public override bool Shoot(Player player, Terraria.DataStructures.EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 speed, int type, int damage, float knockBack)
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

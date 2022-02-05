using Terraria;
using Microsoft.Xna.Framework;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Ranged
{
    public class SuperBlaster : ModItem
    {
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Super Blaster");
        }

        public override void SetDefaults()
        {
            item.damage = 42;
            item.ranged = true;
            item.crit = 4;
            item.width = 44;
            item.height = 24;
            item.useTime = 20;
            item.useAnimation = 20;
            item.useStyle = ItemUseStyleID.HoldingOut;
            item.noMelee = true;
            item.knockBack = 4f;
            item.value = 25000;
            item.scale = 0.7f;
            item.rare = ItemRarityID.Orange;
            item.shoot = mod.ProjectileType("SuperBlasterShot");
            item.shootSpeed = 18f;
        }

        public override void AddRecipes()
        {
            ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(null, "Blaster");
            recipe.AddIngredient(ItemID.HellstoneBar, 1);
            recipe.AddIngredient(mod.GetItem("DarkSoul"), 5000);
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

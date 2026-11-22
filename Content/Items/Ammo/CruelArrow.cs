using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items.Materials;
using tsorcRevamp.Content.Items.Materials.Souls;
using tsorcRevamp.Content.Items.Materials.Souls.DarkSoul;
using tsorcRevamp.Content.Projectiles.Ranged.Ammo;

namespace tsorcRevamp.Content.Items.Ammo
{
    public class CruelArrow : ModItem
    {
        public static int Pierce = 1;
        public static float DmgMult = 5;
        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            Item.consumable = true;
            Item.ammo = AmmoID.Arrow;
            Item.damage = 7;
            Item.height = 28;
            Item.knockBack = (float)3.5;
            Item.maxStack = Item.CommonMaxStack;
            Item.rare = ItemRarityID.Blue;
            Item.DamageType = DamageClass.Ranged;
            Item.shootSpeed = 6.5f;
            Item.value = 50;
            Item.width = 10;
            Item.shoot = ModContent.ProjectileType<CruelArrowProjectile>();
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe(30);
            recipe.AddIngredient(ItemID.WoodenArrow, 30);
            recipe.AddIngredient(ItemID.IronBar, 1);
            recipe.AddIngredient(ModContent.ItemType<DarkSoulItem>(), 60); // 2 Dark Souls per arrow
            recipe.AddTile(TileID.DemonAltar);
            recipe.Register();
        }


    }
}

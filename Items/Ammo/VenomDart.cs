using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;
using tsorcRevamp.Projectiles.Ranged.Ammo;

namespace tsorcRevamp.Items.Ammo
{
    public class VenomDart : ModItem
    {

        public override void SetDefaults()
        {
            Item.consumable = true;
            Item.DamageType = DamageClass.Ranged;
            Item.ammo = AmmoID.Dart;
            Item.damage = 18;
            Item.height = 10;
            Item.height = 20;
            Item.knockBack = 5f;
            Item.maxStack = Item.CommonMaxStack;
            Item.rare = ItemRarityID.Lime;
            Item.shootSpeed = 1f;
            Item.value = 1000;
            Item.shoot = ModContent.ProjectileType<VenomDartProjectile>();
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe(100);
            recipe.AddIngredient(ItemID.PoisonDart, 100);
            recipe.AddIngredient(ItemID.VialofVenom, 1);
            recipe.AddIngredient(ItemID.ChlorophyteBar, 1);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 150);
            recipe.AddTile(TileID.DemonAltar);
            recipe.Register();
        }
    }
}

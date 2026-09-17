/*using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;
using tsorcRevamp.Projectiles.Ranged.Ammo;

namespace tsorcRevamp.Items.Ammo
{
    public class FrigidDart : ModItem
    {

        public override void SetDefaults()
        {
            Item.consumable = true;
            Item.DamageType = DamageClass.Ranged;
            Item.ammo = AmmoID.Dart;
            Item.damage = 14;
            Item.height = 10;
            Item.height = 20;
            Item.knockBack = 4f;
            Item.maxStack = Item.CommonMaxStack;
            Item.rare = ItemRarityID.LightRed;
            Item.shootSpeed = 1.2f;
            Item.value = 1000;
            Item.shoot = ModContent.ProjectileType<FrigidDartProjectile>();
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe(999);
            recipe.AddIngredient(ItemID.MeteoriteBar, 999);
            recipe.AddIngredient(ItemID.MythrilBar, 3);
            recipe.AddIngredient(ItemID.FrostCore, 1);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 850);
            recipe.AddTile(TileID.DemonAltar);
            recipe.Register();
        }
    }
}*/

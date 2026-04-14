using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;
using tsorcRevamp.Projectiles.Ranged.Ammo;

namespace tsorcRevamp.Items.Ammo
{
    public class PowerBolt : ModItem
    {

        public override void SetDefaults()
        {
            Item.consumable = true;
            Item.DamageType = DamageClass.Ranged;
            Item.ammo = ModContent.ItemType<Bolt>();
            Item.damage = 10;
            Item.height = 10;
            Item.height = 20;
            Item.knockBack = 3f;
            Item.maxStack = Item.CommonMaxStack;
            Item.rare = ItemRarityID.Blue;
            Item.shootSpeed = 3.8f;
            Item.value = 1000;
            Item.shoot = ModContent.ProjectileType<PowerBoltProjectile>();
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe(100);
            recipe.AddIngredient(ModContent.ItemType<Bolt>(), 100);
            recipe.AddIngredient(ItemID.MeteoriteBar, 1);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 20);
            recipe.AddTile(TileID.DemonAltar);
            recipe.Register();
        }
    }
}

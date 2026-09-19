using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items.Materials;
using tsorcRevamp.Content.Items.Materials.Souls;
using tsorcRevamp.Content.Items.Materials.Souls.DarkSoul;
using tsorcRevamp.Content.Projectiles.Ranged.Ammo;

namespace tsorcRevamp.Content.Items.Ammo
{
    public class HeavyBolt : ModItem
    {

        public override void SetDefaults()
        {
            Item.consumable = true;
            Item.DamageType = DamageClass.Ranged;
            Item.ammo = ModContent.ItemType<Bolt>();
            Item.damage = 12;
            Item.height = 10;
            Item.height = 20;
            Item.knockBack = 3.5f;
            Item.maxStack = Item.CommonMaxStack;
            Item.rare = ItemRarityID.Green;
            Item.shootSpeed = 4.2f;
            Item.value = 1000;
            Item.shoot = ModContent.ProjectileType<HeavyBoltProjectile>();
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe(80);
            recipe.AddIngredient(ModContent.ItemType<Bolt>(), 80);
            recipe.AddIngredient(ItemID.Bone, 1);
            recipe.AddIngredient(ModContent.ItemType<DarkSoulItem>(), 25);
            recipe.AddTile(TileID.DemonAltar);
            recipe.Register();
        }
    }
}

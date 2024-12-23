using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;
using tsorcRevamp.Projectiles.Ranged.Ammo;

namespace tsorcRevamp.Items.Ammo
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
            recipe.AddTile(TileID.Anvils);
            recipe.Register();
        }
    }
}

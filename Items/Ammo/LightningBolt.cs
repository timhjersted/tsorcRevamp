using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;
using tsorcRevamp.Projectiles.Ranged.Ammo;

namespace tsorcRevamp.Items.Ammo
{
    public class LightningBolt : ModItem
    {

        public override void SetDefaults()
        {
            Item.consumable = true;
            Item.DamageType = DamageClass.Ranged;
            Item.ammo = ModContent.ItemType<Bolt>();
            Item.damage = 9;
            Item.height = 10;
            Item.height = 28;
            Item.knockBack = 3.5f;
            Item.maxStack = Item.CommonMaxStack;
            Item.rare = ItemRarityID.LightRed;
            Item.shootSpeed = 4.6f;
            Item.value = 1000;
            Item.shoot = ModContent.ProjectileType<LightningBoltProjectile>();
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe(200);
            recipe.AddIngredient(ModContent.ItemType<Bolt>(), 200);
            recipe.AddIngredient(ItemID.MythrilBar, 1);
            recipe.AddIngredient(ItemID.SoulofFlight, 1);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 50);
            recipe.AddTile(TileID.DemonAltar);
            recipe.Register();
        }
    }
}

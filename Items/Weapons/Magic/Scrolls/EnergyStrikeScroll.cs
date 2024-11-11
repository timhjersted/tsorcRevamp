using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;
using tsorcRevamp.Projectiles.Magic;
using tsorcRevamp.Projectiles.Magic.Scrolls;

namespace tsorcRevamp.Items.Weapons.Magic.Scrolls
{
    class EnergyStrikeScroll : ModItem
    {

        public override void SetStaticDefaults()
        {
            // Tooltip.SetDefault("The scroll reads \"Exori vis.\"");
        }
        public override void SetDefaults()
        {
            Item.width = 34;
            Item.height = 10;
            Item.damage = 40;
            Item.DamageType = DamageClass.MagicSummonHybrid;
            Item.mana = 100;
            Item.knockBack = 2f;
            Item.rare = ItemRarityID.LightRed;
            Item.value = PriceByRarity.LightRed_4;
            Item.shoot = ModContent.ProjectileType<EnergyStrikeScrollTeslaCoil>();
            Item.UseSound = SoundID.Item21;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.useTime = 60;
            Item.useAnimation = 60;
            Item.noMelee = true;
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.MagnetSphere);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 15000);

            recipe.AddTile(TileID.DemonAltar);
            recipe.Register();
        }
    }
}

using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items.Materials;
using tsorcRevamp.Content.Items.Materials.Souls;
using tsorcRevamp.Content.Items.Materials.Souls.DarkSoul;
using tsorcRevamp.Content.Items.Materials.Titanite;
using tsorcRevamp.Content.Projectiles.Melee.Flails;

namespace tsorcRevamp.Content.Items.Weapons.Melee.Flails
{
    [LegacyName("HeavensTear2")]
    public class SunderingLight : ModItem
    {
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Sundering Light");
            /* Tooltip.SetDefault("Rips apart the border of life and death" +
                "\nDeals double damage to mages and ghosts"); */

        }

        public override void SetDefaults()
        {

            Item.width = 36;
            Item.height = 32;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.channel = true;
            Item.useAnimation = 30;
            Item.useTime = 30;
            Item.damage = 600;
            Item.knockBack = 10;
            Item.UseSound = SoundID.Item1;
            Item.rare = ModContent.RarityType<OrangeRed>();
            Item.shootSpeed = 16;
            Item.noUseGraphic = true;
            Item.noMelee = true;
            Item.value = PriceByRarity.Purple_11;
            Item.DamageType = DamageClass.Melee;
            Item.shoot = ModContent.ProjectileType<SunderingLightBall>();
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();

            recipe.AddIngredient(ModContent.ItemType<HeavensTear>(), 1);
            recipe.AddIngredient(ModContent.ItemType<Humanity>(), 10);
            recipe.AddIngredient(ModContent.ItemType<GhostWyvernSoul>());
            recipe.AddIngredient(ModContent.ItemType<RedTitanite>(), 10);
            recipe.AddIngredient(ModContent.ItemType<DarkSoulItem>(), 120000);

            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }

    }
}

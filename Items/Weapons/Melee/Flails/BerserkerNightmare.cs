using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;
using tsorcRevamp.Projectiles.Melee.Flails;

namespace tsorcRevamp.Items.Weapons.Melee.Flails
{

    public class BerserkerNightmare : ModItem
    {

        public override void SetDefaults()
        {
            Item.width = 34;
            Item.height = 32;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.channel = true;
            Item.useAnimation = 44;
            Item.useTime = 44;
            Item.damage = 116;
            Item.knockBack = 8;
            Item.UseSound = SoundID.Item1;
            Item.rare = ItemRarityID.LightPurple;
            Item.shootSpeed = 13;
            Item.noUseGraphic = true;
            Item.noMelee = true;
            Item.value = PriceByRarity.LightPurple_6;
            Item.DamageType = DamageClass.Melee;
            Item.shoot = ModContent.ProjectileType<BerserkerNightmareBall>();
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.DaoofPow, 1);
            recipe.AddIngredient(ItemID.MythrilBar, 5);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 50000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }


    }
}

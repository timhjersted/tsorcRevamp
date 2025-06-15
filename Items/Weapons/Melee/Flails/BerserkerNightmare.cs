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
            Item.width = 44;
            Item.height = 42;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.channel = true;
            Item.useAnimation = 44;
            Item.useTime = 44;
            Item.damage = 135;
            Item.knockBack = 8;
            Item.UseSound = SoundID.Item1;
            Item.rare = ItemRarityID.Pink;
            Item.shootSpeed = 13;
            Item.noUseGraphic = true;
            Item.noMelee = true;
            Item.value = PriceByRarity.Pink_5;
            Item.DamageType = DamageClass.Melee;
            Item.shoot = ModContent.ProjectileType<BerserkerNightmareBall>();
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.DaoofPow, 1);
            recipe.AddIngredient(ItemID.AdamantiteBar, 5);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 20000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }


    }
}

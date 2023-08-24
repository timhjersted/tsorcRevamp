using Terraria.ID;
using Terraria.ModLoader;
using Terraria;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Weapons.Ranged.Bows
{
    public class AncientGreatBow : ModItem
    {
        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            Item.DamageType = DamageClass.Ranged;
            Item.shoot = ProjectileID.PurificationPowder;
            Item.damage = 24;
            Item.height = 58;
            Item.width = 16;
            Item.knockBack = 1.5f;
            Item.noMelee = true;
            Item.rare = ItemRarityID.Green;
            Item.shootSpeed = 7.5f;
            Item.useAmmo = AmmoID.Arrow;
            Item.useTime = 25;
            Item.useAnimation = 25;
            Item.UseSound = SoundID.Item5;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.value = PriceByRarity.Green_2;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();

            recipe.AddIngredient(ItemID.DemonBow);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 4000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }


    }
}

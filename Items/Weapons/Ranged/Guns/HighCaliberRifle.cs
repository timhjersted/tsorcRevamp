using Terraria.ID;
using Terraria.ModLoader;
using Terraria;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Weapons.Ranged.Guns
{
    class HighCaliberRifle : ModItem
    {

        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            Item.damage = 700;
            Item.height = 22;
            Item.noMelee = true;
            Item.autoReuse = true;
            Item.DamageType = DamageClass.Ranged;
            Item.rare = ItemRarityID.Lime;
            Item.shoot = ProjectileID.PurificationPowder;
            Item.shootSpeed = 20;
            Item.useAmmo = AmmoID.Bullet;
            Item.useAnimation = 120;
            Item.useTime = 120;
            Item.UseSound = SoundID.Item11;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.value = PriceByRarity.Lime_7;
            Item.width = 66;
            Item.knockBack = 50f;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.SniperRifle);
            recipe.AddIngredient(ItemID.IllegalGunParts);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 20000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}

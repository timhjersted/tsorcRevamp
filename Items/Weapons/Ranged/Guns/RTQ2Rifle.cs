using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Weapons.Ranged.Guns
{
    class RTQ2Rifle : ModItem
    {

        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("RTQ2 Rifle");
        }

        public override void SetDefaults()
        {
            Item.width = 24;
            Item.height = 28;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.useAnimation = 9;
            Item.useTime = 9;
            Item.damage = 42;
            Item.autoReuse = true;
            Item.UseSound = SoundID.Item91;
            Item.rare = ItemRarityID.LightPurple;
            Item.shoot = ProjectileID.PurificationPowder;
            Item.shootSpeed = 11;
            Item.noMelee = true;
            Item.value = PriceByRarity.LightPurple_6;
            Item.useAmmo = AmmoID.Bullet;
            Item.DamageType = DamageClass.Ranged;
            Item.knockBack = 1f;
        }

        public override Vector2? HoldoutOffset()
        {
            return new Vector2(-6, -6);
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.Megashark, 1);
            recipe.AddIngredient(ItemID.SoulofMight, 1);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 20000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}

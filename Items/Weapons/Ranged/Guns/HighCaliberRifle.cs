using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
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
            Item.damage = 400;
            Item.height = 22;
            Item.noMelee = true;
            Item.autoReuse = true;
            Item.DamageType = DamageClass.Ranged;
            Item.rare = ItemRarityID.Lime;
            Item.shoot = ProjectileID.PurificationPowder;
            Item.shootSpeed = 20;
            Item.useAmmo = AmmoID.Bullet;
            Item.useAnimation = 60;
            Item.useTime = 60;
            Item.UseSound = SoundID.Item40;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.value = PriceByRarity.Lime_7;
            Item.width = 66;
            Item.knockBack = 50f;
        }
        public override void HoldItem(Player player)
        {
            player.scope = true;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.SniperRifle);
            recipe.AddIngredient(ItemID.IllegalGunParts);
            recipe.AddIngredient(ItemID.Cog, 80);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 25000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}

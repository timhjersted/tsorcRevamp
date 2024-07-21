using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Weapons.Magic.Tomes
{
    class FireSpiritTome : ModItem
    {
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Fire Spirit Tome");
            // Tooltip.SetDefault("Summons fire spirits at incredible speed");
        }

        public override void SetDefaults()
        {
            Item.width = 28;
            Item.height = 32;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.useAnimation = 5;
            Item.useTime = 5;
            Item.damage = 22;
            Item.knockBack = 8;
            Item.autoReuse = true;
            Item.scale = 1f;
            Item.UseSound = SoundID.Item9;
            Item.rare = ItemRarityID.LightRed;
            Item.shootSpeed = 11;
            Item.mana = 5;
            Item.value = PriceByRarity.LightRed_4;
            Item.DamageType = DamageClass.Magic;
            Item.shoot = ModContent.ProjectileType<Projectiles.Fireball1>();
            Item.noMelee = true;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.SpellTome, 1);
            //recipe.AddIngredient(ItemID.Fireblossom, 5);
            recipe.AddIngredient(ItemID.AdamantiteBar, 1);
            //recipe.AddIngredient(ItemID.SoulofNight, 5);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 18000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}

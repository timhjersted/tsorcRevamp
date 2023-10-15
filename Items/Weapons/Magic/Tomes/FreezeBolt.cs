using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Weapons.Magic.Tomes
{
    class FreezeBolt : ModItem
    {
        public override void SetStaticDefaults()
        {
            // Tooltip.SetDefault("Casts a fast-moving bolt of ice");
        }

        public override void SetDefaults()
        {
            Item.width = 24;
            Item.height = 28;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.useAnimation = 17;
            Item.useTime = 17;
            Item.damage = 20;
            Item.knockBack = 5;
            Item.autoReuse = true;
            Item.UseSound = SoundID.Item21;
            Item.rare = ItemRarityID.Orange;
            Item.shootSpeed = 7;
            Item.mana = 12;
            Item.value = PriceByRarity.Orange_3;
            Item.DamageType = DamageClass.Magic;
            Item.shoot = ModContent.ProjectileType<Projectiles.FreezeBolt>();
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.WaterBolt, 1);
            recipe.AddIngredient(ModContent.ItemType<EphemeralDust>(), 12);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 12000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}

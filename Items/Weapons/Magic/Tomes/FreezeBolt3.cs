using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Weapons.Magic.Tomes
{
    class FreezeBolt3 : ModItem
    {

        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Freeze Bolt III");
            // Tooltip.SetDefault("Casts a super fast-moving bolt of ice");
        }

        public override void SetDefaults()
        {
            Item.width = 28;
            Item.height = 32;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.useAnimation = 20;
            Item.useTime = 20;
            Item.damage = 235;
            Item.knockBack = 8;
            Item.autoReuse = true;
            Item.UseSound = SoundID.Item21;
            Item.rare = ItemRarityID.Purple;
            Item.shootSpeed = 16;
            Item.mana = 21;
            Item.value = PriceByRarity.Purple_11;
            Item.DamageType = DamageClass.Magic;
            Item.shoot = ModContent.ProjectileType<Projectiles.FreezeBolt3>();
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ModContent.ItemType<FreezeBolt2>(), 1);
            //recipe.AddIngredient(ModContent.ItemType<SoulOfAttraidies>(), 1);
            recipe.AddIngredient(ModContent.ItemType<BlueTitanite>(), 10);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 110000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}

using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Weapons.Magic
{
    class UltimaTome : ModItem
    {
        public override void SetStaticDefaults()
        {
            // Tooltip.SetDefault("Ultimate tome guarded by the Omega Weapon.");
        }
        public override void SetDefaults()
        {
            Item.damage = 245;
            Item.height = 10;
            Item.knockBack = 4;
            Item.rare = ModContent.RarityType<DarkBlue>();
            Item.shootSpeed = 9;
            Item.noMelee = true;
            Item.DamageType = DamageClass.Magic;
            Item.mana = 18;
            Item.UseSound = SoundID.Item21;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.useTime = 10;
            Item.useAnimation = 10;
            Item.value = 5000000;
            Item.width = 34;
            Item.shoot = ModContent.ProjectileType<Projectiles.Ultima>();
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.SpellTome, 1);
            recipe.AddIngredient(ModContent.ItemType<SoulOfArtorias>(), 1);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 100000);
            recipe.AddTile(TileID.DemonAltar);
            recipe.Register();
        }
    }
}

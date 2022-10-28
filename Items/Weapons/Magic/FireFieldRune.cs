using Terraria.ID;
using Terraria.ModLoader;
using Terraria;

namespace tsorcRevamp.Items.Weapons.Magic
{
    public class FireFieldRune : ModItem
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Projectile that explodes with a sustained burning flame at the point of impact");
        }
        public override void SetDefaults()
        {
            Item.damage = 40;
            Item.height = 28;
            Item.knockBack = 4;
            Item.rare = ItemRarityID.LightRed;
            Item.shootSpeed = 21;
            Item.DamageType = DamageClass.Magic;
            Item.noMelee = true;
            Item.autoReuse = true;
            Item.useAnimation = 30;
            Item.UseSound = SoundID.Item21;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.useTime = 30;
            Item.value = PriceByRarity.LightRed_4;
            Item.width = 20;
            Item.mana = 20;
            Item.shoot = ModContent.ProjectileType<Projectiles.FireFieldBall>();
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.SpellTome, 1);
            recipe.AddIngredient(ItemID.AdamantiteBar, 1);
            //recipe.AddIngredient(ItemID.SoulofNight, 8);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 12000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}

using Terraria.ID;
using Terraria.ModLoader;
using Terraria;

namespace tsorcRevamp.Items.Weapons.Magic
{
    public class PoisonBombRune : ModItem
    {
        public override void SetStaticDefaults()
        {
            // Tooltip.SetDefault("Creates 9 poison bombs on impact\n" + "Superior area denial, drains enemy life with the deadliest poison\n" + "Not necessarily a direct upgrade");
        }
        public override void SetDefaults()
        {
            Item.damage = 15;
            Item.height = 28;
            Item.knockBack = 3;
            Item.rare = ItemRarityID.Orange;
            Item.shootSpeed = 7;
            Item.DamageType = DamageClass.Magic;
            Item.noMelee = true;
            Item.autoReuse = true;
            Item.mana = 70;
            Item.useAnimation = 60;
            Item.UseSound = SoundID.Item21;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.useTime = 60;
            Item.value = PriceByRarity.Orange_3;
            Item.width = 20;
            Item.shoot = ModContent.ProjectileType<Projectiles.PoisonBombBall>();
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.Stinger, 3);
            recipe.AddIngredient(ModContent.ItemType<PoisonFieldRune>(), 1);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 8000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}

using Terraria.ID;
using Terraria.ModLoader;
using Terraria;

namespace tsorcRevamp.Items.Weapons.Magic
{
    public class EnergyBombRune : ModItem
    {
        public override void SetStaticDefaults()
        {
            Tooltip.SetDefault("Summons 9 electric energy orbs in a square at the point of impact.");
        }
        public override void SetDefaults()
        {
            Item.damage = 30;
            Item.height = 28;
            Item.knockBack = 4;
            Item.rare = ItemRarityID.LightPurple;
            Item.shootSpeed = 6;
            Item.DamageType = DamageClass.Magic;
            Item.noMelee = true;
            Item.useAnimation = 50;
            Item.UseSound = SoundID.Item21;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.useTime = 50;
            Item.value = PriceByRarity.LightPurple_6;
            Item.width = 20;
            Item.mana = 50;
            Item.shoot = ModContent.ProjectileType<Projectiles.EnergyBombBall>();
        }
        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            //recipe.AddIngredient(ItemID.SoulofLight, 3);
            recipe.AddIngredient(ModContent.ItemType<EnergyFieldRune>(), 1);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 16000);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}

using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;
using tsorcRevamp.Projectiles.Melee.Shortswords;

namespace tsorcRevamp.Items.Weapons.Melee.Shortswords
{
    class YellowTail : ModItem
    {
        public const int BaseDamage = 14;
        public static int FishboneDmg = (int)(BaseDamage * 1.25f);
        public static float ArmorPen = 50f;
        public static int Heal = 1;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(FishboneDmg, ArmorPen);
        public override void SetStaticDefaults()
        {
        }

        public override void SetDefaults()
        {
            Item.damage = BaseDamage;
            Item.DamageType = DamageClass.Melee;
            Item.width = 46;
            Item.height = 46;
            Item.useTime = 15;
            Item.useAnimation = 15;
            Item.useStyle = ItemUseStyleID.Rapier;
            Item.noUseGraphic = true;
            Item.noMelee = true;
            Item.shoot = ModContent.ProjectileType<YellowTailProjectile>();
            Item.knockBack = 2.6f;
            Item.UseSound = SoundID.Item1;
            Item.rare = ItemRarityID.Blue;
            Item.value = PriceByRarity.Blue_1;
            Item.shootSpeed = 2.1f; // This value bleeds into the behavior of the projectile as velocity, keep that in mind when tweaking values
        }
        public override bool MeleePrefix()
        {
            return true;
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.GoldShortsword);
            recipe.AddIngredient(ItemID.TissueSample, 4);
            recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 3300);
            recipe.AddTile(TileID.DemonAltar);

            recipe.Register();
        }
    }
}

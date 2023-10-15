using Terraria;
using Terraria.GameContent.Creative;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Weapons.Summon.Whips
{
    public class PolarisLeash : ModItem
	{
		public const int BaseDamage = 66;
		public static float SummonTagDamage = 6;
		public static float PolarisDamageScaling = 75;
		public static float StarDamageScaling = 44;
		public const int BuffDuration = 10;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(SummonTagDamage, PolarisDamageScaling, StarDamageScaling);
        public override void SetStaticDefaults()
		{
			CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 1;  //journey mode lmao
		}

		public override void SetDefaults()
		{
			Item.height = 42;
			Item.width = 42;

			Item.DamageType = DamageClass.SummonMeleeSpeed;
			Item.damage = BaseDamage;
			Item.knockBack = 3.5f;
			Item.rare = ItemRarityID.LightPurple;
			Item.value = Item.buyPrice(0, 45, 0, 0);

			Item.shoot = ModContent.ProjectileType<Projectiles.Summon.Whips.PolarisLeashProjectile>();
			Item.shootSpeed = 4;

			Item.useStyle = ItemUseStyleID.Swing;
			Item.useTime = 50; // for some reason a lower use speed gives it increased range....
			Item.useAnimation = 50;
			Item.noMelee = true;
			Item.noUseGraphic = true;
		}
        public override bool MeleePrefix()
        {
            return true;
        }
        public override void AddRecipes()
		{
			Recipe recipe = CreateRecipe();
			recipe.AddIngredient(ModContent.ItemType<EnchantedWhip>());
			recipe.AddIngredient(ItemID.CoolWhip);
			recipe.AddIngredient(ItemID.SoulofMight, 20);
			recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 30000);

			recipe.AddTile(TileID.DemonAltar);
			recipe.Register();
		}
	}
}
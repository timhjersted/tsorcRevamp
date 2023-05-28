using Terraria;
using Terraria.GameContent.Creative;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Summon.Whips
{
	public class SearingLash : ModItem
	{
		public const int BaseDamage = 30;
		public static float DamageScaling = 66;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(DamageScaling);
        public override void SetStaticDefaults()
		{
			CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 1;  //journey mode lmao
		}

		public override void SetDefaults()
		{
			Item.height = 84;
			Item.width = 88;

			Item.DamageType = DamageClass.SummonMeleeSpeed;
			Item.damage = BaseDamage;
			Item.knockBack = 3;
			Item.rare = ItemRarityID.Orange;
			Item.value = Item.buyPrice(0, 8, 50, 0);

			Item.shoot = ModContent.ProjectileType<Projectiles.Summon.Whips.SearingLashProjectile>();
			Item.shootSpeed = 4;
			Item.channel = true;

			Item.useStyle = ItemUseStyleID.Swing;
			Item.useTime = 30;
			Item.useAnimation = 30;
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
			recipe.AddIngredient(ItemID.HellstoneBar, 3);
			recipe.AddIngredient(ItemID.MeteoriteBar, 3);
			recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 6000);

			recipe.AddTile(TileID.DemonAltar);
			recipe.Register();
		}
	}
}
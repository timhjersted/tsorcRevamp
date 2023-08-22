using Terraria;
using Terraria.GameContent.Creative;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Weapons.Summon.Whips
{
    public class DetonationSignal : ModItem
    {
        public static float SummonTagScalingDamage = 300f;
        public static float BonusContactDamage = 300f;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(SummonTagScalingDamage, BonusContactDamage);
        public override void SetStaticDefaults()
		{
			CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 1;  //journey mode lmao
		}
		public override void SetDefaults()
		{

			Item.height = 84;
			Item.width = 88;

			Item.DamageType = DamageClass.SummonMeleeSpeed;
			Item.damage = 120;
			Item.knockBack = 6;
			Item.rare = ItemRarityID.Red;
			Item.value = Item.buyPrice(2, 40, 0, 0);

			Item.shoot = ModContent.ProjectileType<Projectiles.Summon.Whips.DetonationSignalProjectile>();
			Item.shootSpeed = 4;

			Item.useStyle = ItemUseStyleID.Swing;
			Item.useTime = 25;
			Item.useAnimation = 25;
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
			recipe.AddIngredient(ItemID.FireWhip);
			recipe.AddIngredient(ModContent.ItemType<SoulOfChaos>());
			recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 135000);

			recipe.AddTile(TileID.DemonAltar);
			recipe.Register();
		}
	}
}
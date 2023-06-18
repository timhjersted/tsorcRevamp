using Terraria;
using Terraria.GameContent.Creative;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using tsorcRevamp.Items.Materials;

namespace tsorcRevamp.Items.Weapons.Summon.Whips
{
    public class Pyrosulfate : ModItem
	{
		public static float SummonTagDamage = 8;
		public static float SummonTagCrit = 7;
        public override LocalizedText Tooltip => base.Tooltip.WithFormatArgs(SummonTagDamage, SummonTagCrit);
        public override void SetStaticDefaults()
		{
			CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 1;  //journey mode lmao
		}

		public override void SetDefaults()
		{
			Item.height = 70;
			Item.width = 74;

			Item.DamageType = DamageClass.SummonMeleeSpeed;
			Item.damage = 51;
			Item.knockBack = 2.5f;
			Item.rare = ItemRarityID.LightRed;
			Item.value = Item.buyPrice(0, 14, 50, 0);

			Item.shoot = ModContent.ProjectileType<Projectiles.Summon.Whips.PyrosulfateProjectile>();
			Item.shootSpeed = 4;

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
			recipe.AddIngredient(ItemID.DemoniteBar, 3);
			recipe.AddIngredient(ItemID.CursedFlame, 14);
			recipe.AddIngredient(ItemID.SoulofNight, 9);
			recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 16000);

			recipe.AddTile(TileID.DemonAltar);
			recipe.Register();
		}
	}
}
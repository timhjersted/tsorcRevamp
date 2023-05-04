using Terraria;
using Terraria.GameContent.Creative;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Summon.Whips
{
	public class DragoonLash: ModItem
	{
		public override void SetStaticDefaults()
		{
			CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 1;  //journey mode lmao
		}

		public override void SetDefaults()
		{
			Item.height = 48;
			Item.width = 60;

			Item.DamageType = DamageClass.SummonMeleeSpeed;
			Item.damage = 90;
			Item.knockBack = 3.5f;
			Item.rare = ItemRarityID.Lime;
			Item.value = Item.buyPrice(0, 70, 0, 0);

			Item.shoot = ModContent.ProjectileType<Projectiles.Summon.Whips.DragoonLashProjectile>();
			Item.shootSpeed = 4;

			Item.useStyle = ItemUseStyleID.Swing;
			Item.useTime = 27;
			Item.useAnimation = 27;
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
			recipe.AddIngredient(ItemID.ChlorophyteBar, 3);
			recipe.AddIngredient(ModContent.ItemType<SoulOfLife>(), 1);
			recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 50000);

			recipe.AddTile(TileID.DemonAltar);
			recipe.Register();
		}
	}
}
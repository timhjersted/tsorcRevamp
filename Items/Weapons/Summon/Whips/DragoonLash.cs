/*
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
			DisplayName.SetDefault("Dragoon Lash");
			Tooltip.SetDefault("Breathe fire at enemies struck by this whip and a minion on a cooldown" +
                "\nRefresh the cooldown by whipping an enemy" +
                "\nFire breath scales with this whips base damage" +
                "\nStriking Enemies with this whip increases your whip attack speed drastically" +
				"\nYour summons will focus struck enemies");
		}

		public override void SetDefaults()
		{

			Item.height = 48;
			Item.width = 60;

			Item.DamageType = DamageClass.SummonMeleeSpeed;
			Item.damage = 56;
			Item.knockBack = 10;
			Item.rare = ItemRarityID.Cyan;
			Item.value = Item.buyPrice(0, 90, 0, 0);

			Item.shoot = ModContent.ProjectileType<Projectiles.Summon.Whips.DragoonLashProjectile>();
			Item.shootSpeed = 4;

			Item.useStyle = ItemUseStyleID.Swing;
			Item.useTime = 25; // for some reason a lower use speed gives it increased range....
			Item.useAnimation = 25;
			Item.noMelee = true;
			Item.noUseGraphic = true;

		}
		public override void AddRecipes()
		{
			
			Recipe recipe = CreateRecipe();
			recipe.AddIngredient(ItemID.ChlorophyteBar, 13);
			recipe.AddIngredient(ModContent.ItemType<SoulOfLife>(), 1);
			recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 76000);

			recipe.AddTile(TileID.DemonAltar);
			recipe.Register();
		}
	}
}*/
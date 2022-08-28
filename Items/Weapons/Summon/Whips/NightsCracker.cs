
using Terraria;
using Terraria.GameContent.Creative;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Summon.Whips
{
	public class NightsCracker : ModItem
	{
		public override void SetStaticDefaults()
		{
			CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 1;  //journey mode lmao
			DisplayName.SetDefault("Night's Cracker");
			Tooltip.SetDefault("\n4 summon tag damage" +
                "\nInherits Searing Lash's effect at half of its strength" +
                "\n2% summon tag critical strike chance" +
                "\nGain 18% whip attack speed upon striking an enemy" +
                "\nPerforms better against multiple targets than most whips" +
				"\nYour summons will focus struck enemies");
		}

		public override void SetDefaults()
		{

			Item.height = 66;
			Item.width = 60;

			Item.DamageType = DamageClass.SummonMeleeSpeed;
			Item.damage = 42;
			Item.knockBack = 2;
			Item.rare = ItemRarityID.Pink;
			Item.value = Item.buyPrice(0, 30, 0, 0);

			Item.shoot = ModContent.ProjectileType<Projectiles.Summon.Whips.NightsCrackerProjectile>();
			Item.shootSpeed = 4;

			Item.useStyle = ItemUseStyleID.Swing;
			Item.useTime = 30; // for some reason a lower use speed gives it increased range....
			Item.useAnimation = 30;
			Item.noMelee = true;
			Item.noUseGraphic = true;

		}
		public override void AddRecipes()
		{
			
			Recipe recipe = CreateRecipe();
			recipe.AddIngredient(ModContent.ItemType<Dominatrix>());
			recipe.AddIngredient(ItemID.ThornWhip);
			recipe.AddIngredient(ItemID.BoneWhip);
			recipe.AddIngredient(ModContent.ItemType<SearingLash>());
			recipe.AddIngredient(ItemID.SoulofNight, 15);
			recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 16000);

			recipe.AddTile(TileID.DemonAltar);
			recipe.Register();
		}
	}
}
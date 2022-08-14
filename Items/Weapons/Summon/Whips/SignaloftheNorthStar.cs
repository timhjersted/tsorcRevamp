using Terraria;
using Terraria.GameContent.Creative;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Summon.Whips
{
	public class SignaloftheNorthStar : ModItem
	{
		public override void SetStaticDefaults()
		{
			CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 1;  //journey mode lmao
			DisplayName.SetDefault("Signal of the North Star");
			Tooltip.SetDefault("8 summon tag damage" +
                "\n6% summon tag critical strike chance" +
				"\nStriking enemies will create the North Star, which scales with 66% of this whips damage" +
				"\nThe North Star stays on your cursor and hits enemies with a frigid enchantment" +
                "\nEnchanted Enemies will also be showered by polar stars when struck by minions" +
                "\nThese scale with half of this whips damage" +
				"\nYour summons will focus enemies struck by the North Star" +
                "\nThis whip also inflicts Frostbite");
		}

		public override void SetDefaults()
		{

			Item.height = 42;
			Item.width = 42;

			Item.DamageType = DamageClass.SummonMeleeSpeed;
			Item.damage = 100;
			Item.knockBack = 2;
			Item.rare = ItemRarityID.Red;
			Item.value = Item.buyPrice(3, 5, 0, 0);

			Item.shoot = ModContent.ProjectileType<Projectiles.Summon.Whips.SignaloftheNorthStarProjectile>();
			Item.shootSpeed = 4;

			Item.useStyle = ItemUseStyleID.Swing;
			Item.useTime = 50; // for some reason a lower use speed gives it increased range....
			Item.useAnimation = 50;
			Item.noMelee = true;
			Item.noUseGraphic = true;

		}
		public override void AddRecipes()
		{
			
			Recipe recipe = CreateRecipe();
			recipe.AddIngredient(ModContent.ItemType<PolarisLeash>(), 1);
			recipe.AddIngredient(ItemID.LunarBar, 50);
			recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 300000);

			recipe.AddTile(TileID.DemonAltar);
			recipe.Register();
		}
	}
}
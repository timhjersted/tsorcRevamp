
using Terraria;
using Terraria.GameContent.Creative;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Summon.Whips
{
	public class TerraFall : ModItem
	{
		public override void SetStaticDefaults()
		{
			CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 1;  //journey mode lmao
			Tooltip.SetDefault("Stats of this whip scale with how long you've charged it" +
                "\n6-20 summon tag damage" +
                "\nInherits Searing Lash's effect at an eighth of it's strength" + //8% effectiveness rounded down
                "\n3-10% summon tag crit" +
				"\nSummons a Terraprisma after striking an enemy" +
                "\nGain 13-40% whip attack speed upon striking an enemy" +
                "\nThis whip performs better against multiple targets than most whips" +
				"\nYour summons will focus struck enemies");
		}

		public override void SetDefaults()
		{

			Item.height = 40;
			Item.width = 40;

			Item.DamageType = DamageClass.SummonMeleeSpeed;
			Item.damage = 115;
			Item.knockBack = 2;
			Item.rare = ItemRarityID.Red;
			Item.value = Item.buyPrice(3, 33, 33, 33);

			Item.channel = true;

			Item.shoot = ModContent.ProjectileType<Projectiles.Summon.Whips.TerraFallProjectile>();
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
			recipe.AddIngredient(ModContent.ItemType<NightsCracker>());
			recipe.AddIngredient(ItemID.SwordWhip);
			recipe.AddIngredient(ItemID.RainbowWhip);
			recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 100000);

			recipe.AddTile(TileID.DemonAltar);
			recipe.Register();
		}
	}
}
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
			DisplayName.SetDefault("Terra Fall");
            Tooltip.SetDefault("Stats of this whip scale with how long you've charged it" +
                "\nSummons a Terraprisma after striking an enemy(Unimplemented)" +
                "\n5-20 summon tag damage" +
                "\nInherits Searing Lash's effect at up to an eighth of it's strength" + //8% effectiveness rounded down
                "\n4-12% summon tag crit" +
                "\nGain 12-48% summon attack speed upon striking an enemy" +
                "\nThis whip performs better against multiple targets than most whips" + //make this scale slightly too
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
		public override bool MeleePrefix()
		{
			return true;
		}
		public override void AddRecipes()
		{
			
			Recipe recipe = CreateRecipe();
			recipe.AddIngredient(ModContent.ItemType<NightsCracker>());
			recipe.AddIngredient(ItemID.SwordWhip);
			recipe.AddIngredient(ItemID.RainbowWhip);
			recipe.AddIngredient(ModContent.ItemType<SoulOfArtorias>());
			recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 100000);

			recipe.AddTile(TileID.DemonAltar);
			recipe.Register();
		}
	}
}
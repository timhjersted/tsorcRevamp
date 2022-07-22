/*
using Terraria;
using Terraria.GameContent.Creative;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Summon.Whips
{
	public class SearingLash : ModItem
	{
		public override void SetStaticDefaults()
		{
			CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 1;  //journey mode lmao
			Tooltip.SetDefault("Enemies struck by this whip will burn horribly" +
                "\nand increase minion damage by 56% of this whips damage in +%" +
                "\nThis stacks on top of other whip tag dmg" +
                "\nYour minions will focus struck enemies");
		}

		public override void SetDefaults()
		{

			Item.height = 42;
			Item.width = 42;

			Item.DamageType = DamageClass.SummonMeleeSpeed;
			Item.damage = 30;
			Item.knockBack = 2;
			Item.rare = ItemRarityID.Orange;

			Item.shoot = ModContent.ProjectileType<Projectiles.Summon.Whips.SearingLashProjectile>();
			Item.shootSpeed = 4;

			Item.useStyle = ItemUseStyleID.Swing;
			Item.useTime = 30; // for some reason a lower use speed gives it increased range....
			Item.useAnimation = 30;
			Item.UseSound = SoundID.Item152;
			Item.noMelee = true;
			Item.noUseGraphic = true;

		}
		public override void AddRecipes()
		{
			/*
			Terraria.Recipe recipe = CreateRecipe();
			recipe.AddIngredient(ItemID.HellstoneBar, 10);
			recipe.AddIngredient(ItemID.MeteoriteBar, 10);
			recipe.AddIngredient(Mod.Find<ModItem>("DarkSoul").Type, 8000);

			recipe.AddTile(TileID.DemonAltar);
			recipe.Register();
		}
	}
}
*/
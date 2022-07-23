using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent.Creative;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Summon.Whips
{
	public class EnchantedWhip : ModItem
	{
		public override void SetStaticDefaults()
		{
			CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 1;  //journey mode lmao
			Tooltip.SetDefault("3 summon tag damage" +
                "\n1% summon tag critical strike chance" +
                "\nYour summons will focus struck enemies" +
                "\nStrike enemies with an enchantment" +
                "\nEnchanted enemies will be showered by stars upon minion hits" +
                "\nStar damage scales with this whips damage");
		}

		public override void SetDefaults()
		{

			Item.height = 60;
			Item.width = 52;

			Item.DamageType = DamageClass.SummonMeleeSpeed;
			Item.damage = 20;
			Item.knockBack = 2;
			Item.rare = ItemRarityID.Blue;

			Item.shoot = ModContent.ProjectileType<Projectiles.Summon.Whips.EnchantedWhipProjectile>();
			Item.shootSpeed = 4;

			Item.useStyle = ItemUseStyleID.Swing;
			Item.useTime = 30; // for some reason a lower use speed gives it increased range....
			Item.useAnimation = 30;
			Item.noMelee = true;
			Item.noUseGraphic = true;

		}


		public override void AddRecipes()
		{
			Terraria.Recipe recipe = CreateRecipe();
			recipe.AddIngredient(ItemID.BlandWhip, 1);
			recipe.AddIngredient(ItemID.FallenStar, 20);
			recipe.AddIngredient(Mod.Find<ModItem>("DarkSoul").Type, 5000);

			recipe.AddTile(TileID.DemonAltar);
			recipe.Register();
		}
	}
}

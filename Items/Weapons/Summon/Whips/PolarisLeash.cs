/*
using Terraria;
using Terraria.GameContent.Creative;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Summon.Whips
{
	public class PolarisLeash : ModItem
	{
		public override void SetStaticDefaults()
		{
			CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 1;  //journey mode lmao
			Tooltip.SetDefault("5 summon tag damage" +
                "\n2% summon tag critical strike chance" +
				"\nStriking enemies will create Polaris, which scales with this whips damage" +
				"\nPolaris stays on your cursor and hits enemies with a frigid enchantment" +
                "\nEnchanted Enemies will also be showered by polar stars when struck by minions" +
				"\nYour summons will focus enemies struck by Polaris" +
                "\nThis whip also inflicts Frostbite");
		}

		public override void SetDefaults()
		{

			Item.height = 42;
			Item.width = 42;

			Item.DamageType = DamageClass.SummonMeleeSpeed;
			Item.damage = 52;
			Item.knockBack = 2;
			Item.rare = ItemRarityID.Green;

			Item.shoot = ModContent.ProjectileType<Projectiles.Summon.Whips.PolarisLeashProjectile>();
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
			recipe.AddIngredient(ModContent.ItemType<EnchantedWhip>(), 1);
			recipe.AddIngredient(ItemID.CoolWhip, 1);
			recipe.AddIngredient(ModContent.ItemType<SoulOfLife>(), 5);
			recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 60000);

			recipe.AddTile(TileID.DemonAltar);
			recipe.Register();
		}
	}
}
*/
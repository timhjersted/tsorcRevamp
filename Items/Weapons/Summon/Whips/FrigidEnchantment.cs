/*
using Terraria;
using Terraria.GameContent.Creative;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Summon.Whips
{
	public class FrigidEnchantment : ModItem
	{
		public override void SetStaticDefaults()
		{
			CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 1;  //journey mode lmao
			Tooltip.SetDefault("5 summon tag damage" +
                "2% summon tag critical strike chance" +
                "Strike enemies with a frigid enchantment" +
                "This will create Polaris" +
                "Enchanted Enemies will also be showered by polar stars when struck by minions" +
				"\nYour summons will focus struck enemies");
		}

		public override void SetDefaults()
		{

			Item.height = 42;
			Item.width = 42;

			Item.DamageType = DamageClass.SummonMeleeSpeed;
			Item.damage = 23;
			Item.knockBack = 2;
			Item.rare = ItemRarityID.Green;

			Item.shoot = ModContent.ProjectileType<Projectiles.Summon.Whips.FrigidEnchantmentProjectile>();
			Item.shootSpeed = 4;

			Item.useStyle = ItemUseStyleID.Swing;
			Item.useTime = 30; // for some reason a lower use speed gives it increased range....
			Item.useAnimation = 30;
			Item.UseSound = SoundID.Item152;
			Item.noMelee = true;
			Item.noUseGraphic = true;

		}
		/*
		public override void AddRecipes()
		{
			
			Terraria.Recipe recipe = CreateRecipe();
			recipe.AddIngredient(ModContent.ItemType<EnchantedWhip>(), 1);
			recipe.AddIngredient(ItemID.CoolWhip, 1);
			recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 10000);

			recipe.AddTile(TileID.DemonAltar);
			recipe.Register();
		}
		*/
	}
}

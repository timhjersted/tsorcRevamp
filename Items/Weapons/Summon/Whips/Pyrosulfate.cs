using Terraria;
using Terraria.GameContent.Creative;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Summon.Whips
{
	public class Pyrosulfate : ModItem
	{
		public override void SetStaticDefaults()
		{
			CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 1;  //journey mode lmao
			Tooltip.SetDefault("8 summon tag damage" +
                "\n3% summon tag critical strike chance" +
                "\nYour minions will focus struck enemies" +
                "\nThis whip also inflicts Cursed Inferno");
		}

		public override void SetDefaults()
		{

			Item.height = 70;
			Item.width = 74;

			Item.DamageType = DamageClass.SummonMeleeSpeed;
			Item.damage = 52;
			Item.knockBack = 2;
			Item.rare = ItemRarityID.LightRed;
			Item.value = Item.buyPrice(0, 14, 50, 0);

			Item.shoot = ModContent.ProjectileType<Projectiles.Summon.Whips.PyrosulfateProjectile>();
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
			recipe.AddIngredient(ItemID.DemoniteBar, 4);
			recipe.AddIngredient(ItemID.CursedFlame, 20);
			recipe.AddIngredient(ItemID.SoulofNight, 5);
			recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 6900);

			recipe.AddTile(TileID.DemonAltar);
			recipe.Register();
		}
	}
}
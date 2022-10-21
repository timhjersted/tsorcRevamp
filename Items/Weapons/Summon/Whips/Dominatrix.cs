using Terraria;
using Terraria.GameContent.Creative;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Summon.Whips
{
	public class Dominatrix : ModItem
	{
		public override void SetStaticDefaults()
		{
			CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 1;  //journey mode lmao
			Tooltip.SetDefault("7% summon tag critical strike chance" +
                "\nInflicts bleeding debuff" +
				"\nYour summons will focus struck enemies");
		}

		public override void SetDefaults()
		{

			Item.height = 66;
			Item.width = 60;

			Item.DamageType = DamageClass.SummonMeleeSpeed;
			Item.damage = 23;
			Item.knockBack = 2;
			Item.rare = ItemRarityID.Green;
			Item.value = Item.buyPrice(0, 6, 0, 0);

			Item.shoot = ModContent.ProjectileType<Projectiles.Summon.Whips.DominatrixProjectile>();
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
			recipe.AddIngredient(ItemID.DemoniteBar, 10);
			recipe.AddIngredient(ItemID.CrimtaneBar, 10);
			recipe.AddIngredient(ItemID.ShadowScale, 5);
			recipe.AddIngredient(ItemID.TissueSample, 5);
			recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 4700);

			recipe.AddTile(TileID.DemonAltar);
			recipe.Register();
		}
	}
}
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
			Tooltip.SetDefault("6 summon tag damage" +
                "\nStriking enemies will create Polaris, which scales with 66% of this whips damage" +
				"\nPolaris stays on your cursor and hits enemies with a frigid enchantment" +
                "\nEnchanted Enemies will also be showered by polar stars when struck by minions" +
                "\nThese scale with half of this whips damage" +
				"\nYour summons will focus enemies struck by Polaris" +
                "\nThis whip also inflicts Frostbite");
		}

		public override void SetDefaults()
		{

			Item.height = 42;
			Item.width = 42;

			Item.DamageType = DamageClass.SummonMeleeSpeed;
			Item.damage = 60;
			Item.knockBack = 2;
			Item.rare = ItemRarityID.LightPurple;
			Item.value = Item.buyPrice(0, 45, 0, 0);

			Item.shoot = ModContent.ProjectileType<Projectiles.Summon.Whips.PolarisLeashProjectile>();
			Item.shootSpeed = 4;

			Item.useStyle = ItemUseStyleID.Swing;
			Item.useTime = 50; // for some reason a lower use speed gives it increased range....
			Item.useAnimation = 50;
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
			recipe.AddIngredient(ModContent.ItemType<EnchantedWhip>(), 1);
			recipe.AddIngredient(ItemID.CoolWhip, 1);
			recipe.AddIngredient(ItemID.SoulofMight, 20);
			recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 30000);

			recipe.AddTile(TileID.DemonAltar);
			recipe.Register();
		}
	}
}
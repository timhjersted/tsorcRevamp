using Terraria;
using Terraria.GameContent.Creative;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Summon.Whips
{
	public class DetonationSignal : ModItem
	{
		public override void SetStaticDefaults()
		{
			CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 1;  //journey mode lmao
            DisplayName.SetDefault("Detonation Signal");
            Tooltip.SetDefault("Enemies struck by this whip will explode once on minion hit" +
                "\nScales with double the minion damage" +
                "\nInflicts Hellfire" +
                "\nYour minions will focus struck enemies");
		}

		public override void SetDefaults()
		{

			Item.height = 84;
			Item.width = 88;

			Item.DamageType = DamageClass.SummonMeleeSpeed;
			Item.damage = 120;
			Item.knockBack = 2;
			Item.rare = ItemRarityID.Red;
			Item.value = Item.buyPrice(2, 40, 0, 0);

			Item.shoot = ModContent.ProjectileType<Projectiles.Summon.Whips.DetonationSignalProjectile>();
			Item.shootSpeed = 4;

			Item.useStyle = ItemUseStyleID.Swing;
			Item.useTime = 23; // for some reason a lower use speed gives it increased range....
			Item.useAnimation = 23;
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
			recipe.AddIngredient(ItemID.FireWhip);
			recipe.AddIngredient(ModContent.ItemType<SoulOfChaos>());
			recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 105000);

			recipe.AddTile(TileID.DemonAltar);
			recipe.Register();
		}
	}
}
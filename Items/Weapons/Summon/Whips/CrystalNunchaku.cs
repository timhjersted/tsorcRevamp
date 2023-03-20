using Terraria;
using Terraria.GameContent.Creative;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Summon.Whips
{
	public class CrystalNunchaku : ModItem
	{
		public override void SetStaticDefaults()
		{
			CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 1;  //journey mode lmao
            DisplayName.SetDefault("Crystal Nunchaku");
            Tooltip.SetDefault("Tags hit enemies with a parasytic crystal for 15 seconds" +
				"\nAfter 5 seconds this will either increase your own defense or increase the enemies damage taken from all sources" +
				"\nHitting the enemy during infection will decrease the damage bonus and increase your defensive bonus" +
				"\nDamage bonus ranges from 25% to 0%, Defense bonus ranges from 0 to 15" +
				"\nYour summons will focus struck enemies" +
				"\n'The best defense is a good offense, or was it the other way around?'");
		}

		public override void SetDefaults()
		{

			Item.height = 36;
			Item.width = 32;

			Item.DamageType = DamageClass.SummonMeleeSpeed;
			Item.damage = 51;
			Item.knockBack = 2;
			Item.rare = ItemRarityID.LightRed;
			Item.value = Item.buyPrice(0, 6, 0, 0);

			Item.shoot = ModContent.ProjectileType<Projectiles.Summon.Whips.CrystalNunchakuProjectile>();
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
			recipe.AddIngredient(ItemID.CrystalShard, 5);
			recipe.AddIngredient(ItemID.SoulofLight, 4);
			recipe.AddIngredient(ItemID.SoulofFright, 20);
			recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 20000);
			recipe.AddTile(TileID.DemonAltar);
			recipe.AddCondition(tsorcRevampWorld.AdventureModeEnabled);
			recipe.Register();
		}
	}
}
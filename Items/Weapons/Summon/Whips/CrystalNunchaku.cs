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
		}

		public override void SetDefaults()
		{
			Item.height = 36;
			Item.width = 32;

			Item.DamageType = DamageClass.SummonMeleeSpeed;
			Item.damage = 51;
			Item.knockBack = 5f;
			Item.rare = ItemRarityID.LightPurple;
			Item.value = Item.buyPrice(0, 20, 0, 0);

			Item.shoot = ModContent.ProjectileType<Projectiles.Summon.Whips.CrystalNunchakuProjectile>();
			Item.shootSpeed = 4;

			Item.useStyle = ItemUseStyleID.Swing;
			Item.useTime = 30;
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
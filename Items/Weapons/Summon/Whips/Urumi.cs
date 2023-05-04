using Terraria;
using Terraria.GameContent.Creative;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons.Summon.Whips
{
	public class Urumi : ModItem
	{
		public override void SetStaticDefaults()
		{
			CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 1;  //journey mode lmao
		}

		public override void SetDefaults()
		{
			Item.height = 66;
			Item.width = 60;

			Item.DamageType = DamageClass.SummonMeleeSpeed;
			Item.damage = 13;
			Item.knockBack = 0.5f;
			Item.rare = ItemRarityID.Blue;
			Item.value = Item.buyPrice(0, 2, 0, 0);

			Item.shoot = ModContent.ProjectileType<Projectiles.Summon.Whips.UrumiProjectile>();
			Item.shootSpeed = 4;

			Item.useStyle = ItemUseStyleID.Swing;
			Item.useTime = 23;
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
			recipe.AddIngredient(ItemID.ChainKnife);
			recipe.AddIngredient(ModContent.ItemType<DarkSoul>(), 1000);
			recipe.AddTile(TileID.DemonAltar);
			recipe.Register();

            Recipe recipe2 = CreateRecipe();
            recipe2.AddIngredient(ItemID.Chain, 5);
            recipe2.AddIngredient(ItemID.Hook);
            recipe2.AddIngredient(ModContent.ItemType<DarkSoul>(), 800);
            recipe2.AddTile(TileID.DemonAltar);
            recipe2.Register();
			
			Recipe recipe3 = CreateRecipe();
            recipe3.AddIngredient(ItemID.Chain, 5);
            recipe3.AddIngredient(ItemID.ThrowingKnife, 5);
            recipe3.AddIngredient(ModContent.ItemType<DarkSoul>(), 1200);
            recipe3.AddTile(TileID.DemonAltar);
            recipe3.Register();
        }
	}
}
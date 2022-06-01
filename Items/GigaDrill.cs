using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items {
	public class GigaDrill : ModItem {
		public override void SetStaticDefaults() {
			Tooltip.SetDefault("Pierce the Heavens");
		}

		public override void SetDefaults() {
			Item.width = 20;
			Item.height = 12;
			Item.useStyle = ItemUseStyleID.Shoot;
			Item.channel = true;
			Item.useAnimation = 25;
			Item.useTime = 7;
			Item.pick = 200;
			Item.axe = 100;
			Item.damage = 42;
			Item.knockBack = 5;
			Item.UseSound = SoundID.Item23;
			Item.rare = ItemRarityID.Pink;
			Item.shootSpeed = 36;
			Item.noUseGraphic = true;
			Item.noMelee = true;
			Item.value = 250000;
			Item.DamageType = DamageClass.Melee;
			Item.shoot = ModContent.ProjectileType<Projectiles.GigaDrill>();
		}
		public override void AddRecipes() {
			Recipe recipe = CreateRecipe();
			recipe.AddIngredient(ItemID.Drax, 1);
			recipe.AddIngredient(Mod.Find<ModItem>("DarkSoul").Type, 10000);
			recipe.AddTile(TileID.DemonAltar);
			
			recipe.Register();
		}
	}
}
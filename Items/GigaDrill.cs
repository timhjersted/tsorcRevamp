using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items {
	public class GigaDrill : ModItem {
		public override void SetStaticDefaults() {
			Tooltip.SetDefault("Pierce the Heavens");
		}

		public override void SetDefaults() {
			item.width = 20;
			item.height = 12;
			item.useStyle = ItemUseStyleID.HoldingOut;
			item.channel = true;
			item.useAnimation = 25;
			item.useTime = 7;
			item.pick = 220;
			item.axe = 100;
			item.damage = 42;
			item.knockBack = 5;
			item.UseSound = SoundID.Item23;
			item.rare = ItemRarityID.Pink;
			item.shootSpeed = 36;
			item.noUseGraphic = true;
			item.noMelee = true;
			item.value = 250000;
			item.melee = true;
			item.shoot = ModContent.ProjectileType<Projectiles.GigaDrill>();
		}
		public override void AddRecipes() {
			ModRecipe recipe = new ModRecipe(mod);
			recipe.AddIngredient(ItemID.Drax, 1);
			recipe.AddIngredient(mod.GetItem("DarkSoul"), 10000);
			recipe.AddTile(TileID.DemonAltar);
			recipe.SetResult(this, 1);
			recipe.AddRecipe();
		}
	}
}
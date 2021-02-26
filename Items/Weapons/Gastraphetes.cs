using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons {
	public class Gastraphetes : ModItem {
		public override void SetStaticDefaults() {
			Tooltip.SetDefault("Blacken the sky");
		}

		public override void SetDefaults() {
			item.ranged = true;
			item.shoot = ProjectileID.PurificationPowder;

			item.damage = 20;
			item.width = 50;
			item.height = 18;
			item.useTime = 16;
			item.useAnimation = 16;
			item.useStyle = ItemUseStyleID.HoldingOut;
			item.noMelee = true;
			item.knockBack = 3;
			item.value = 200000;
			item.rare = ItemRarityID.Pink;
			item.UseSound = SoundID.Item5;
			item.autoReuse = true;
			
			item.shootSpeed = 12f;
			item.useAmmo = AmmoID.Arrow;
		}

		public override void AddRecipes() {
			ModRecipe recipe = new ModRecipe(mod);
			recipe.AddIngredient(ItemID.HallowedRepeater, 1);
			recipe.AddIngredient(mod.GetItem("DarkSoul"), 20000);
			recipe.AddTile(TileID.DemonAltar);
			recipe.SetResult(this);
			recipe.AddRecipe();
		}
	}
}
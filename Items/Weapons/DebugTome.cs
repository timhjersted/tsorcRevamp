using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.Weapons {
	public class DebugTome : ModItem {
		public override void SetStaticDefaults() {
			Tooltip.SetDefault("You should not have this.");
		}

		public override void SetDefaults() {
			item.damage = 999999;
			item.knockBack = 4;
			item.crit = 4;
			item.width = 30;
			item.height = 30;
			item.useTime = 20;
			item.useAnimation = 20;
			item.UseSound = SoundID.Item11;
			item.useTurn = true;
			item.noMelee = true;
			item.magic = true;
			item.autoReuse = true;
			item.value = 10000;
			item.useStyle = ItemUseStyleID.HoldingOut;
			item.rare = ItemRarityID.Green;
			item.shootSpeed = 24f;
			item.shoot = ModContent.ProjectileType<Projectiles.BlackFirelet>();
		}

		public override void AddRecipes() {
			ModRecipe recipe = new ModRecipe(mod);
			recipe.AddIngredient(ItemID.MagicMirror, 50);
			recipe.AddIngredient(mod.GetItem("DarkSoul"), 100);
			recipe.AddTile(TileID.DemonAltar);
			recipe.SetResult(this);
			recipe.AddRecipe();
		}
    }
}
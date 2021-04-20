using Terraria;
using Terraria.ModLoader;
using Terraria.ID;

namespace tsorcRevamp.Items.BossItems {
	public class PottedPlanteraBulb : ModItem {

        public override void SetStaticDefaults() {
			Tooltip.SetDefault("Summons Plantera");
		}

		public override void SetDefaults() {
			item.width = 26;
			item.height = 32;
			item.maxStack = 1;
			item.useAnimation = 45;
			item.useTime = 45;
			item.useStyle = ItemUseStyleID.HoldingOut;
			item.rare = ItemRarityID.Lime;
			item.consumable = false;
		}

		public override bool CanUseItem(Player player) {
			if (player.ZoneJungle) {
				return !NPC.AnyNPCs(NPCID.Plantera);
			}
			return false;
		}

		public override bool UseItem(Player player) {
			Main.PlaySound(SoundID.Roar, player.position, 0);
			if (Main.netMode != NetmodeID.MultiplayerClient) {
				NPC.SpawnOnPlayer(player.whoAmI, NPCID.Plantera);
			}
			else {
				NetMessage.SendData(MessageID.SpawnBoss, -1, -1, null, player.whoAmI, 262f);
			}
			return true;
		}

		public override void AddRecipes() {
			ModRecipe recipe = new ModRecipe(mod);
            recipe.AddIngredient(ItemID.JungleSpores, 99);
			recipe.AddIngredient(ItemID.Vine, 99);
			recipe.AddTile(TileID.DemonAltar);
			recipe.SetResult(this, 1);
			recipe.AddRecipe();
		}
	}
}

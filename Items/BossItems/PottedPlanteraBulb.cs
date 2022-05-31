using Terraria;
using Terraria.ModLoader;
using Terraria.ID;

namespace tsorcRevamp.Items.BossItems {
	public class PottedPlanteraBulb : ModItem {

        public override void SetStaticDefaults() {
			Tooltip.SetDefault("Summons Plantera");
		}

		public override void SetDefaults() {
			Item.width = 26;
			Item.height = 32;
			Item.maxStack = 1;
			Item.useAnimation = 45;
			Item.useTime = 45;
			Item.useStyle = ItemUseStyleID.Shoot;
			Item.rare = ItemRarityID.Lime;
			Item.consumable = false;
		}

		public override bool CanUseItem(Player player) {
			if (player.ZoneJungle) {
				return !NPC.AnyNPCs(NPCID.Plantera);
			}
			return false;
		}

		public override bool? UseItem(Player player) {
			Terraria.Audio.SoundEngine.PlaySound(SoundID.Roar, player.position, 0);
			if (Main.netMode != NetmodeID.MultiplayerClient) {
				NPC.SpawnOnPlayer(player.whoAmI, NPCID.Plantera);
			}
			else {
				NetMessage.SendData(MessageID.SpawnBoss, -1, -1, null, player.whoAmI, 262f);
			}
			return true;
		}

		public override void AddRecipes() {
			Recipe recipe = new Recipe(Mod);
            recipe.AddIngredient(ItemID.JungleSpores, 99);
			recipe.AddIngredient(ItemID.Vine, 99);
			recipe.AddTile(TileID.DemonAltar);
			recipe.SetResult(this, 1);
			recipe.AddRecipe();
		}
	}
}

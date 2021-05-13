using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.NPCs.Bosses;

namespace tsorcRevamp.Items.BossBags {
	
	public abstract class BossBag : ModItem {
		
		public override void SetStaticDefaults() {
			DisplayName.SetDefault("Treasure Bag");
			Tooltip.SetDefault("{$CommonItemTooltip.RightClickToOpen}");
			
		}

		public override void SetDefaults() {
			item.maxStack = 999;
			item.consumable = true;
			item.width = 24;
			item.height = 24;
			item.rare = ItemRarityID.Cyan;
			item.expert = true;
		}

		public override bool CanRightClick() {
			return true;
		}
    }
	public class TheHunterBag : BossBag {
		public override int BossBagNPC => ModContent.NPCType<TheHunter>();
        public override void OpenBossBag(Player player) {
			if (tsorcRevampWorld.Slain.ContainsKey(ModContent.NPCType<TheHunter>())) { //if the boss has been killed
				if (tsorcRevampWorld.Slain[ModContent.NPCType<TheHunter>()] == 0) { //and the key value is 0
					tsorcGlobalItem.BossBagSouls(ModContent.NPCType<TheHunter>(), player); //give the player souls
					tsorcRevampWorld.Slain[ModContent.NPCType<TheHunter>()] = 1; //set the value to 1
				}
			}
			player.QuickSpawnItem(ModContent.ItemType<Accessories.WaterShoes>());
            player.QuickSpawnItem(ModContent.ItemType<CrestOfEarth>(), 2);
			player.QuickSpawnItem(ItemID.Drax);
		}
    }
	public class TheRageBag : BossBag {
		public override int BossBagNPC => ModContent.NPCType<TheRage>();
		public override void OpenBossBag(Player player) {
			if (tsorcRevampWorld.Slain.ContainsKey(ModContent.NPCType<TheRage>())) {
				if (tsorcRevampWorld.Slain[ModContent.NPCType<TheRage>()] == 0) {
					tsorcGlobalItem.BossBagSouls(ModContent.NPCType<TheRage>(), player);
					tsorcRevampWorld.Slain[ModContent.NPCType<TheRage>()] = 1;
				}
			}
			player.QuickSpawnItem(ModContent.ItemType<CrestOfFire>(), 2);
			player.QuickSpawnItem(ItemID.CobaltDrill);
		}
	}
	public class TheSorrowBag : BossBag {
		public override int BossBagNPC => ModContent.NPCType<TheSorrow>();
		public override void OpenBossBag(Player player) {
			if (tsorcRevampWorld.Slain.ContainsKey(ModContent.NPCType<TheSorrow>())) {
				if (tsorcRevampWorld.Slain[ModContent.NPCType<TheSorrow>()] == 0) {
					tsorcGlobalItem.BossBagSouls(ModContent.NPCType<TheSorrow>(), player);
					tsorcRevampWorld.Slain[ModContent.NPCType<TheSorrow>()] = 1;
				}
			}
			player.QuickSpawnItem(ModContent.ItemType<CrestOfWater>(), 2);
			player.QuickSpawnItem(ItemID.AdamantiteDrill);
		}
	}
}

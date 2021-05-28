using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items.BossItems {
    class MindflayerIllusionRelic : ModItem {

        public override void SetStaticDefaults() {
            Tooltip.SetDefault("The final battle with Attraidies. \n" +
                "No more illusions.");
        }

        public override void SetDefaults() {
            item.rare = ItemRarityID.LightRed;
            item.width = 38;
            item.height = 34;
            item.useStyle = ItemUseStyleID.HoldingUp;
            item.useAnimation = 45;
            item.useTime = 45;
            item.maxStack = 1;
            item.consumable = false;
        }


        public override bool UseItem(Player player) {
            NPC.SpawnOnPlayer(player.whoAmI, ModContent.NPCType<NPCs.Bosses.Okiku.FinalForm.Attraidies>());
            Main.NewText("I am impressed you've made it this far, Red. But I'm done playing games. It's time to end this...", 175, 75, 255);
            return true;
        }
        public override bool CanUseItem(Player player) {
            if (NPC.AnyNPCs(NPCID.CorruptBunny)) {
                //(NPC.AnyNPCs(ModContent.NPCType<NPCs.Bosses.Attraidies>()))
                return false;
            }
            return true;
        }
    }
}

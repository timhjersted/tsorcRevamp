using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items {
    class FairyInABottle : ModItem {

        public override void SetStaticDefaults() {
            Tooltip.SetDefault("A fairy can be seen trapped in the bottle.\n" + "Using this will free the fairy.");
        }
        public override void SetDefaults() {
            item.width = 18;
            item.height = 18;
            item.consumable = true;
            item.maxStack = 1;
            item.value = 0;
            item.rare = ItemRarityID.Pink;
            item.useTime = 5;
            item.useAnimation = 5;
            item.scale = 1f;
            item.useStyle = ItemUseStyleID.HoldingUp;
        }
        public override bool UseItem(Player player) {
            NPC.SpawnOnPlayer(player.whoAmI, ModContent.NPCType<NPCs.Friendly.FreedFairy>());
            Main.NewText("Check your minimap to find them!", Color.HotPink);
            return true;
        }
    }
}
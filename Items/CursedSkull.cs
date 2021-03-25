using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items {
    class CursedSkull : ModItem {
        public override void SetStaticDefaults() {
            Tooltip.SetDefault("Summons Gravelord Nito, the First of the Dead." +
                                "\nYou must use this at the demon altar in the ancient temple ruins" +
                                "\nBut be warned, this battle will not be easy..." +
                                "\nItem is not consumed so you can retry the fight until victory.");

        }
        public override void SetDefaults() {
            item.width = 12;
            item.height = 12;
            item.useStyle = ItemUseStyleID.HoldingUp;
            item.useAnimation = 5;
            item.useTime = 5;
        }

        public override bool UseItem(Player player) {
            Main.NewText("Gravelord Nito has awoken! ", 175, 75, 255);
            NPC.NewNPC((int)Main.player[Main.myPlayer].position.X - (1070), (int)Main.player[Main.myPlayer].position.Y - 150, NPCID.SkeletronHead, 1);
            return true;
        }
    }
}

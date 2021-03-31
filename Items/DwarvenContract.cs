using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Items {
    class DwarvenContract : ModItem {

        public override void SetStaticDefaults() {
            Tooltip.SetDefault("A contract for a dwarf guard.\n" + "Will summon a dwarf to guard a piece of property.");
        }
        public override void SetDefaults() {
            item.width = 18;
            item.height = 18;
            item.consumable = true;
            item.maxStack = 1;
            item.value = 10000;
            item.rare = ItemRarityID.Pink;
            item.useTime = 5;
            item.useAnimation = 5;
            item.scale = 1f;
            item.useStyle = ItemUseStyleID.HoldingUp;
        }
        public override bool UseItem(Player player) {
            NPC.SpawnOnPlayer(Main.myPlayer, NPCID.PartyBunny); //placeholder
            //NPC.SpawnOnPlayer(player.whoAmI, ModContent.NPCType<NPCs.Friendly.DwarvenGuard>());
            return true;
        }
    }
}
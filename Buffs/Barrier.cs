using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs {
    class Barrier : ModBuff {
        public override void SetDefaults() {
            DisplayName.SetDefault("Barrier");
            Description.SetDefault("Defense is increased by 20!");
            Main.debuff[Type] = false;
            Main.buffNoTimeDisplay[Type] = false;
        }

        public override void Update(Player player, ref int buffIndex) {
            player.statDefense += 20;
        }
    }
}

using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs {
    class Protect : ModBuff {
        public override void SetStaticDefaults() {
            DisplayName.SetDefault("Protect");
            Description.SetDefault("Defense is increased by 30!");
            Main.debuff[Type] = false;
            Main.buffNoTimeDisplay[Type] = false;
        }

        public override void Update(Player player, ref int buffIndex) {
            player.statDefense += 30;
        }
    }
}

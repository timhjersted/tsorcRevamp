using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs {
    class Boost : ModBuff {
        public override void SetStaticDefaults() {
            DisplayName.SetDefault("Boost");
            Description.SetDefault("Increased critical strike chance");
            Main.debuff[Type] = false;
            Main.buffNoTimeDisplay[Type] = false;
        }

        public override void Update(Player player, ref int buffIndex) {
            player.magicCrit += 5;
            player.meleeCrit += 5;
            player.rangedCrit += 5;
        }
    }
}

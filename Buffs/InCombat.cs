using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs {
    class InCombat : ModBuff {
        public override void SetDefaults() {
            DisplayName.SetDefault("In Combat");
            Description.SetDefault("You are in combat.");
            Main.debuff[Type] = true;
            Main.buffNoTimeDisplay[Type] = false;
            canBeCleared = false;
        }
    }
}

using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs {
    class ShieldCooldown : ModBuff {

        public override void SetDefaults() {
            DisplayName.SetDefault("Shield Cooldown");
            Description.SetDefault("You cannot use wall tomes!");
            Main.debuff[Type] = true;
            Main.buffNoTimeDisplay[Type] = false;
            canBeCleared = false; //prevents nurse clearing
        }
    }
}

using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs {
    class InCombat : ModBuff {
        public override bool Autoload(ref string name, ref string texture) {
            texture = "tsorcRevamp/Buffs/CurseBuildup";
            return base.Autoload(ref name, ref texture);
        }

        public override void SetDefaults() {
            DisplayName.SetDefault("In Combat");
            Description.SetDefault("Great Magic Mirror and Village Mirror are disabled!");
            Main.debuff[Type] = true;
            Main.buffNoTimeDisplay[Type] = false;
            canBeCleared = false;
        }

    }
}

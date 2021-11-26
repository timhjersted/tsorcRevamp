using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs {
    class DispelShadow : ModBuff {

        public override bool Autoload(ref string name, ref string texture) {
            texture = "tsorcRevamp/Buffs/CurseBuildup"; //enemy only buff, so it doesnt need a real icon
            return base.Autoload(ref name, ref texture);
        }

        public override void SetDefaults() {
            DisplayName.SetDefault("Dispel Shadow");
            Description.SetDefault("Your defense has been dispelled");
        }
    }
}

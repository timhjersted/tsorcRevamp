using Terraria;
using Terraria.ModLoader;
using tsorcRevamp.NPCs;

namespace tsorcRevamp.Buffs {
    class CrimsonBurn : ModBuff {

        public override bool Autoload(ref string name, ref string texture) {
            texture = "tsorcRevamp/Buffs/CurseBuildup"; //enemy only buff, so it doesnt need a real icon
            return base.Autoload(ref name, ref texture);
        }


        public override void SetStaticDefaults() {
            DisplayName.SetDefault("Crimson Burn");
            Description.SetDefault("Your flesh is burning");
        }

        public override void Update(NPC npc, ref int buffIndex) {
            npc.GetGlobalNPC<tsorcRevampGlobalNPC>().CrimsonBurn = true;
        }
    }
}

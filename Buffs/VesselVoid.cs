using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Buffs
{
    // Purely-visual arena flag for the Vessel of Souls' phase-2 "inside the vessel" void. Sets the
    // per-player EnterTheAbyss render flag (the black/purple-mist void that hides the world) with NONE of
    // the Abyss debuff's penalties (no Weak / defense loss / TornWings). Refreshed each second by the boss
    // while phase 2 is active; expires — restoring the normal world — when the boss dies or despawns.
    public class VesselVoid : ModBuff
    {
        // Reuse the Abyss buff icon; a bespoke icon can drop in later by changing this path.
        public override string Texture => "tsorcRevamp/Buffs/Debuffs/Abyss";

        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = false;
            Main.buffNoTimeDisplay[Type] = true;
            Main.buffNoSave[Type] = true;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            player.GetModPlayer<tsorcRevampPlayer>().EnterTheAbyss = true;
        }
    }
}

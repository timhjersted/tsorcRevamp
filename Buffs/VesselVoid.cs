using Terraria;
using Terraria.ModLoader;
using VesselBoss = tsorcRevamp.NPCs.Bosses.VesselOfSouls.VesselOfSouls;

namespace tsorcRevamp.Buffs
{
    // Per-player flag for the Vessel of Souls' phase-2 "inside the vessel" void. Sets the EnterTheAbyss render flag (the
    // black/purple-mist void that hides the world) with NONE of the Abyss debuff's penalties (no Weak / defense loss /
    // TornWings). The boss grants it to players standing inside its arena; once you have it, it is yours until the void
    // ends (the boss enters its death spectacle, dies or despawns) or you die — leaving the arena does NOT drop it.
    // Fleeing the arena is punished separately, by the boss (a pull and then a fatal swallow — see VesselOfSouls.TickFleeBoundary).
    public class VesselVoid : ModBuff
    {
        // The boss refreshes it for players inside the arena every 40 ticks; this is the timer it sets, and the one the
        // buff keeps topping itself back up to so it never lapses on someone outside.
        const int HeldTicks = 80;

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
            // The boss clears this itself, but only while its AI is ticking: a despawn, or a server-side ClearBuff
            // (not networked), can leave it behind. No Vessel, or the void no longer active = drop it. Dying needs no
            // code here: vanilla strips non-persistent buffs on death.
            bool vesselFound = VesselBoss.TryGetActiveVessel(out VesselBoss vessel);

            if (!vesselFound || !vessel.VoidActive)
            {
                player.DelBuff(buffIndex);
                buffIndex--;
                return;
            }

            player.buffTime[buffIndex] = HeldTicks;
            player.GetModPlayer<tsorcRevampPlayer>().EnterTheAbyss = true;
        }
    }
}

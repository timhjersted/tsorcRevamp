using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;
using tsorcRevamp.NPCs.Bosses.SuperHardMode.Fiends;

namespace tsorcRevamp.Utilities
{
    // Diagnostic for "the Marilith intro event ring is in the right place but nothing spawns". The gate
    // (tsorcScriptedEvents.MarilithCustomCondition) has NO SuperHardMode check at all — it blocks only on
    // RemixMap, FireFiendMarilith already recorded as slain, or a FireFiendMarilith/MarilithIntro NPC already
    // existing somewhere in the world (MarilithIntro never auto-despawns via distance: CheckActive() => false,
    // alpha=100 + behindTiles=true, so a leftover one from an earlier test can sit invisibly forever and block
    // new spawns). This prints each sub-condition directly instead of guessing.
    public class MarilithCheckCommand : ModCommand
    {
        public override CommandType Type => CommandType.Chat;
        public override string Command => "marilithcheck";
        public override string Description => "Dump why the Marilith intro event will/won't spawn.";

        public override void Action(CommandCaller caller, string input, string[] args)
        {
            bool remix = tsorcRevampWorld.RemixMap;
            bool slain = tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<FireFiendMarilith>()));
            bool bossActive = NPC.AnyNPCs(ModContent.NPCType<FireFiendMarilith>());
            bool introActive = NPC.AnyNPCs(ModContent.NPCType<MarilithIntro>());
            bool result = tsorcScriptedEvents.MarilithCustomCondition();

            caller.Reply("===== Marilith event check =====", Color.Cyan);
            caller.Reply($"RemixMap: {remix}", Color.Gray);
            caller.Reply($"FireFiendMarilith in NewSlain (recorded defeated): {slain}", slain ? Color.Red : Color.Gray);
            caller.Reply($"FireFiendMarilith NPC currently exists: {bossActive}", bossActive ? Color.Red : Color.Gray);
            caller.Reply($"MarilithIntro NPC currently exists: {introActive}", introActive ? Color.Red : Color.Gray);
            caller.Reply($"MarilithCustomCondition() = {result}  ({(result ? "event WILL spawn" : "event is BLOCKED")})",
                result ? Color.Lime : Color.OrangeRed);

            if (!result)
            {
                if (slain) caller.Reply("-> Fix: this world already has FireFiendMarilith marked as slain. Use the event-reset tome (clears NewSlain) or a fresh world copy.", Color.Yellow);
                if (bossActive || introActive) caller.Reply("-> Fix: kill/despawn the existing FireFiendMarilith/MarilithIntro NPC (it may be off-screen or invisible), or reload a fresh world copy.", Color.Yellow);
                if (remix) caller.Reply("-> This is the Remix map; the adventure Marilith event never runs here (RemixMarilithCustomCondition governs remix instead).", Color.Yellow);
            }
        }
    }
}

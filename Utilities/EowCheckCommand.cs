using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Utilities
{
    // Diagnostic for "the EoW boss circle is missing on the expanded map". Checks the gate (PreEoWCustomCondition),
    // whether the hardcoded event's transformed centerpoint and the dynamic-JSON twin's normalized centerpoint
    // actually land at the SAME tile (a mismatch here would explain a missing/duplicate ring, same failure class as
    // the earlier WitchKing duplicate-ring bug), and whether an EoW1 event is actually present in EnabledEvents
    // (the list that gets rendered/walked into).
    public class EowCheckCommand : ModCommand
    {
        public override CommandType Type => CommandType.Chat;
        public override string Command => "eowcheck";
        public override string Description => "Dump why the Eater of Worlds intro event ring will/won't show.";

        public override void Action(CommandCaller caller, string input, string[] args)
        {
            bool downed = NPC.downedBoss2;
            bool headAlive = NPC.AnyNPCs(NPCID.EaterofWorldsHead);
            bool bodyAlive = NPC.AnyNPCs(NPCID.EaterofWorldsBody);
            bool tailAlive = NPC.AnyNPCs(NPCID.EaterofWorldsTail);
            bool result = tsorcScriptedEvents.PreEoWCustomCondition();

            caller.Reply("===== EoW event check =====", Color.Cyan);
            caller.Reply($"NPC.downedBoss2: {downed}", downed ? Color.Red : Color.Gray);
            caller.Reply($"EaterofWorlds Head/Body/Tail alive: {headAlive}/{bodyAlive}/{tailAlive}", (headAlive || bodyAlive || tailAlive) ? Color.Red : Color.Gray);
            caller.Reply($"PreEoWCustomCondition() = {result}  ({(result ? "gate is OPEN" : "gate is BLOCKED")})", result ? Color.Lime : Color.OrangeRed);

            // Hardcoded event's transformed centerpoint (tile space).
            if (tsorcScriptedEvents.ScriptedEventDict != null &&
                tsorcScriptedEvents.ScriptedEventDict.TryGetValue(tsorcScriptedEvents.ScriptedEventType.EoW1, out var hardcoded))
            {
                Vector2 hcTile = hardcoded.centerpoint / 16f;
                caller.Reply($"Hardcoded EoW1Event centerpoint (tile): ({hcTile.X},{hcTile.Y})", Color.White);
            }
            else
            {
                caller.Reply("Hardcoded EoW1Event not found in ScriptedEventDict.", Color.OrangeRed);
            }

            // Dynamic-JSON twin's normalized (runtime-space) centerpoint, if present.
            bool foundDynamic = false;
            foreach (var dEvent in tsorcScriptedEvents.DynamicEvents)
            {
                if (dEvent.MapCondition == "PreEoWCustomCondition")
                {
                    caller.Reply($"Dynamic JSON twin centerpoint (tile, runtime-space): ({dEvent.CenterX},{dEvent.CenterY})", Color.White);
                    foundDynamic = true;
                }
            }
            if (!foundDynamic)
                caller.Reply("No dynamic-JSON entry with MapCondition=PreEoWCustomCondition found.", Color.Gray);

            // Is an EoW1 event actually walkable/visible right now (present in EnabledEvents)?
            bool inEnabled = false;
            foreach (var ev in tsorcScriptedEvents.EnabledEvents)
            {
                if (ev.eventNPCs != null)
                {
                    foreach (var npc in ev.eventNPCs)
                    {
                        if (npc.type == NPCID.EaterofWorldsHead) { inEnabled = true; break; }
                    }
                }
                if (inEnabled) break;
            }
            caller.Reply($"An EoW1 event is present in EnabledEvents (walkable/visible): {inEnabled}", inEnabled ? Color.Lime : Color.OrangeRed);

            // ---- TwinEoWFight ("double EoW" rematch) — a SEPARATE event, different location/gate/mechanism ----
            caller.Reply("----- Twin Eaters (rematch) check -----", Color.Cyan);
            bool twinResult = tsorcScriptedEvents.TwinEoWCustomCondition();
            caller.Reply($"TwinEoWCustomCondition() = {twinResult}  ({(twinResult ? "gate is OPEN" : "gate is BLOCKED")})", twinResult ? Color.Lime : Color.OrangeRed);

            if (tsorcScriptedEvents.ScriptedEventDict != null &&
                tsorcScriptedEvents.ScriptedEventDict.TryGetValue(tsorcScriptedEvents.ScriptedEventType.TwinEoWFight, out var twinHardcoded))
            {
                Vector2 twinTile = twinHardcoded.centerpoint / 16f;
                caller.Reply($"Hardcoded TwinEoWFight centerpoint (tile): ({twinTile.X},{twinTile.Y})", Color.White);
            }
            else
            {
                caller.Reply("Hardcoded TwinEoWFight not found in ScriptedEventDict.", Color.OrangeRed);
            }

            bool foundTwinDynamic = false;
            foreach (var dEvent in tsorcScriptedEvents.DynamicEvents)
            {
                if (dEvent.MapCondition == "TwinEoWCustomCondition")
                {
                    caller.Reply($"Dynamic JSON twin centerpoint (tile, runtime-space): ({dEvent.CenterX},{dEvent.CenterY})", Color.White);
                    foundTwinDynamic = true;
                }
            }
            if (!foundTwinDynamic)
                caller.Reply("No dynamic-JSON entry with MapCondition=TwinEoWCustomCondition found.", Color.Gray);

            // TwinEoWFight has NO eventNPCs (it spawns both heads itself via TwinEoWAction), so match by CustomAction
            // delegate instead of by NPC type like the EoW1 check above.
            bool twinInEnabled = false;
            foreach (var ev in tsorcScriptedEvents.EnabledEvents)
            {
                if (ev.CustomAction == tsorcScriptedEvents.TwinEoWAction) { twinInEnabled = true; break; }
            }
            caller.Reply($"A TwinEoWFight event is present in EnabledEvents (walkable/visible): {twinInEnabled}", twinInEnabled ? Color.Lime : Color.OrangeRed);

            caller.Reply("=================================", Color.Cyan);
        }
    }
}

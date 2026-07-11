using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Utilities
{
    // Debug-only progression skips. Set the same two flags the real progression triggers set
    // (see e.g. Attraidies.cs's post-fight hardmode/SHM transitions) WITHOUT running WorldGen.StartHardmode(),
    // so ore conversion / hallow-corruption spread / meteor do NOT happen — just the bool gates that scripted
    // events, spawn pools, and boss-only code (like the SHM pyramid arenas) check. Intended for testing on a
    // disposable world copy; going "backwards" (e.g. /prehardmode after ore is already converted) does not
    // undo world-gen, same limitation as vanilla.
    public class PreHardmodeCommand : ModCommand
    {
        public override CommandType Type => CommandType.Chat;
        public override string Command => "prehardmode";
        public override string Description => "Debug: set world state to pre-hardmode (Main.hardMode=false, SuperHardMode=false). Flags only, no world-gen.";

        public override void Action(CommandCaller caller, string input, string[] args)
        {
            Main.hardMode = false;
            tsorcRevampWorld.SuperHardMode = false;
            caller.Reply("World state set to PRE-HARDMODE. (Flags only — existing ore/hallow conversion is not undone.)", Color.Cyan);
        }
    }

    public class HardmodeCommand : ModCommand
    {
        public override CommandType Type => CommandType.Chat;
        public override string Command => "hardmode";
        public override string Description => "Debug: set world state to hardmode (Main.hardMode=true, SuperHardMode=false). Flags only, no world-gen.";

        public override void Action(CommandCaller caller, string input, string[] args)
        {
            Main.hardMode = true;
            tsorcRevampWorld.SuperHardMode = false;
            caller.Reply("World state set to HARDMODE. (Flags only — no ore conversion / hallow spread / meteor.)", Color.Yellow);
        }
    }

    public class SuperHardmodeCommand : ModCommand
    {
        public override CommandType Type => CommandType.Chat;
        public override string Command => "superhardmode";
        public override string Description => "Debug: set world state to Super Hard Mode (Main.hardMode=true, SuperHardMode=true). Flags only, no world-gen.";

        public override void Action(CommandCaller caller, string input, string[] args)
        {
            Main.hardMode = true;
            tsorcRevampWorld.SuperHardMode = true;
            caller.Reply("World state set to SUPER HARD MODE.", Color.Red);
        }
    }

    // Toggles ExpandedWorldTransform's opt-in per-coordinate log (see ExpandedWorldTransform.DebugLog). Each unique
    // legacy coordinate it transforms is written once to client.log tagged "[ExpandedTransform]", so a silent-if-wrong
    // value (a boss teleport point, an arena anchor computed once at AI-init) leaves a paper trail even when nothing
    // about it is visually distinguishable in-game.
    public class ExpandedLogCommand : ModCommand
    {
        public override CommandType Type => CommandType.Chat;
        public override string Command => "expandedlog";
        public override string Description => "Toggle logging of expanded-world coordinate transforms to client.log.";

        public override void Action(CommandCaller caller, string input, string[] args)
        {
            bool on = args.Length > 0 ? args[0].ToLowerInvariant() != "off" : !ExpandedWorldTransform.DebugLog;
            ExpandedWorldTransform.DebugLog = on;
            caller.Reply($"Expanded-transform coordinate logging: {(on ? "ON" : "OFF")} (tagged [ExpandedTransform] in client.log)",
                on ? Color.Lime : Color.Gray);
        }
    }
}

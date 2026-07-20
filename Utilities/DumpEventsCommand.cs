using Terraria.ModLoader;
using Microsoft.Xna.Framework;

namespace tsorcRevamp.Utilities
{
    public class DumpEventsCommand : ModCommand
    {
        public override CommandType Type => CommandType.Chat;

        public override string Command => "dumpevents";

        public override string Description => "Dumps hardcoded events to DynamicEvents.json for migration";

        public override void Action(CommandCaller caller, string input, string[] args)
        {
            try
            {
                tsorcScriptedEvents.DumpOldEventsToJson();
                caller.Reply("Dumped old events to Content/DynamicEvents.json!", Color.Lime);
            }
            catch (System.Exception ex)
            {
                caller.Reply($"Dump failed: {ex.Message}", Color.Red);
            }
        }
    }
}

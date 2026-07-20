using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using System.IO;
using Terraria;

namespace tsorcRevamp.Utilities
{
    public class ResetDynamicEventsCommand : ModCommand
    {
        public override CommandType Type => CommandType.Chat;

        public override string Command => "resetdynamicevents";

        public override string Description => "Wipes all dynamic events and starts fresh with an empty file";

        public override void Action(CommandCaller caller, string input, string[] args)
        {
            try
            {
                string relativePath = "Content/DynamicEvents.json";
                string fullPath = Path.Combine(Main.SavePath, "ModSources", "tsorcRevamp", relativePath);
                
                // Write empty array to DynamicEvents.json
                File.WriteAllText(fullPath, "[]");
                
                // Reload dynamic events in the game
                tsorcScriptedEvents.LoadDynamicEvents();
                
                caller.Reply("Cleared all dynamic events. Start fresh!", Color.Lime);
            }
            catch (System.Exception ex)
            {
                caller.Reply($"Reset failed: {ex.Message}", Color.Red);
            }
        }
    }
}

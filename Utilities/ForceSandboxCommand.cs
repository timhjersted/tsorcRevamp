using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Utilities
{
    // Manual escape hatch for worlds that get misdetected as the custom adventure / remix map.
    // Detection is tile-signature + worldID based, so a normal world that happens to contain pasted
    // custom-map tiles (or that ever had CustomMap saved into its world_state) gets permanently treated
    // as the custom map, which force-enables Adventure Mode and runs the heavy custom-map init.
    // "/forcesandbox" flags the current world as a plain sandbox world, overriding all of that.
    public class ForceSandboxCommand : ModCommand
    {
        public override CommandType Type => CommandType.Chat;

        public override string Command => "forcesandbox";

        public override string Description => "Toggle whether this world is forced to be a normal sandbox world (overrides custom/remix/adventure map detection). Add 'off' to clear.";

        public override void Action(CommandCaller caller, string input, string[] args)
        {
            bool turningOff = args.Length > 0 && (args[0].ToLowerInvariant() == "off" || args[0].ToLowerInvariant() == "false");

            tsorcRevampWorld.ForceSandbox = !turningOff;

            if (tsorcRevampWorld.ForceSandbox)
            {
                // Immediately clear the sticky detection flags and disable Adventure Mode so it takes
                // effect this session without needing a reload.
                tsorcRevampWorld.CustomMap = false;
                tsorcRevampWorld.RemixMap = false;
                tsorcRevampWorld.OnlyAdventureMap = false;

                var config = ModContent.GetInstance<tsorcRevampConfig>();
                if (config.AdventureMode)
                {
                    config.AdventureMode = false;
                }

                caller.Reply("ForceSandbox ENABLED. This world is now treated as a normal sandbox world. " +
                             "Save and reload to fully clear all custom-map state.", Color.Lime);
            }
            else
            {
                caller.Reply("ForceSandbox DISABLED. This world will use normal custom/remix/adventure map detection again. " +
                             "Save and reload for detection to re-run.", Color.Orange);
            }

            // Handy for capturing the official maps' GUIDs if you later switch detection to a GUID whitelist.
            if (Main.ActiveWorldFileData != null)
            {
                caller.Reply($"This world's GUID: {Main.ActiveWorldFileData.UniqueId}", Color.Gray);
            }
        }
    }
}

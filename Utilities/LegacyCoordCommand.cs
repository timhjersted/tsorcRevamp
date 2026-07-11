using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Utilities
{
    // Authoring helper for the expanded (2400-tall) world. The soapstone JSON (and legacy hardcoded content) are
    // stored in LEGACY 2000-space and transformed at load, so when you want to add a NEW soapstone/entry at a spot
    // you see on the EXPANDED world, you must write its LEGACY coordinate. This prints the legacy coordinate for
    // your current position (and the mouse tile), ready to paste into the JSON's tileX/tileY.
    public class LegacyCoordCommand : ModCommand
    {
        public override CommandType Type => CommandType.Chat;

        public override string Command => "legacycoord";

        public override string Description => "Print the LEGACY (2000-space) tile coords for your position + the mouse tile, for authoring JSON on the expanded world.";

        public override void Action(CommandCaller caller, string input, string[] args)
        {
            Player p = caller.Player;
            Point playerTile = p.Center.ToTileCoordinates();
            Point mouseTile = Main.MouseWorld.ToTileCoordinates();

            Point playerLegacy = ExpandedWorldTransform.InverseMapTile(playerTile.X, playerTile.Y);
            Point mouseLegacy = ExpandedWorldTransform.InverseMapTile(mouseTile.X, mouseTile.Y);

            if (!ExpandedWorldTransform.Active)
            {
                caller.Reply("Not on the expanded world (transform inactive) — legacy coords equal the current coords.", Color.Gray);
            }

            caller.Reply($"Player: current ({playerTile.X},{playerTile.Y})  ->  LEGACY ({playerLegacy.X},{playerLegacy.Y})", Color.Lime);
            caller.Reply($"Mouse:  current ({mouseTile.X},{mouseTile.Y})  ->  LEGACY ({mouseLegacy.X},{mouseLegacy.Y})", Color.Cyan);
            caller.Reply("Use the LEGACY values for tileX/tileY when adding a new soapstone to the JSON.", Color.Gray);
        }
    }
}

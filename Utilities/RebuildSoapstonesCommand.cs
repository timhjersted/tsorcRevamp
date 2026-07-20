using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Tiles;

namespace tsorcRevamp.Utilities
{
    // Clears every placed soapstone tile/entity, then re-runs BuildSoapstones() from the JSON.
    // Needed after the world-expansion transform changed where soapstones should sit: worlds that were
    // loaded before the transform existed have soapstones saved at the OLD (legacy) coordinates, and the
    // normal re-sync path won't relocate them. This wipes them so BuildSoapstones re-places at the
    // transformed positions. Safe to run repeatedly. Singleplayer / listen-server host only.
    public class RebuildSoapstonesCommand : ModCommand
    {
        public override CommandType Type => CommandType.Chat;

        public override string Command => "rebuildsoapstones";

        public override string Description => "Wipe all placed soapstones and re-place them from the JSON at their (transformed) positions.";

        public override void Action(CommandCaller caller, string input, string[] args)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                caller.Reply("Run this on the server/host, not a client.", Color.OrangeRed);
                return;
            }

            // Collect first (can't mutate TileEntity.ByID while iterating).
            List<Point> positions = new List<Point>();
            foreach (KeyValuePair<int, TileEntity> kv in TileEntity.ByID)
            {
                if (kv.Value is SoapstoneTileEntity se)
                    positions.Add(new Point(se.Position.X, se.Position.Y));
            }

            foreach (Point p in positions)
            {
                // KillTile removes the tile and triggers the ModTileEntity cleanup hook.
                WorldGen.KillTile(p.X, p.Y, noItem: true);
            }

            int cleared = positions.Count;
            caller.Reply($"Cleared {cleared} soapstone(s). Rebuilding from JSON (transform Active={ExpandedWorldTransform.Active})...", Color.Yellow);

            tsorcRevampWorld.BuildSoapstones();

            caller.Reply("Soapstone rebuild complete. Save + reload to persist the new placement.", Color.Lime);
        }
    }
}

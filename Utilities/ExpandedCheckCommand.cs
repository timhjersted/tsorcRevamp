using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Utilities
{
    // Diagnostic for the 2400-tall Expanded Adventure world detection + coordinate transform (Chunk 1).
    // Dumps world identity, the detection flags, and a couple of sample transforms both to chat and to the
    // mod log (client.log) so the output can be shared. Run it on the expanded world and on the old/remix
    // world to compare.
    public class ExpandedCheckCommand : ModCommand
    {
        public override CommandType Type => CommandType.Chat;

        public override string Command => "expandedcheck";

        public override string Description => "Dump Expanded Adventure detection + transform state to chat and the log.";

        public override void Action(CommandCaller caller, string input, string[] args)
        {
            var log = ModContent.GetInstance<tsorcRevamp>().Logger;

            void Line(string s, Color color)
            {
                caller.Reply(s, color);
                log.Info("[expandedcheck] " + s);
            }

            string guid = Main.ActiveWorldFileData != null ? Main.ActiveWorldFileData.UniqueId.ToString() : "(null)";

            // Sample transforms:
            //  - WitchKing event anchor (2484,1795) is a +400 case on the expanded world -> expect tile Y 2195.
            //  - A shallow marker (4209,944) is a +200 case -> expect tile Y 1144.
            //  - The one override (1553,1778) -> expect tile Y 2178.
            // On any non-expanded world all three are identity.
            Vector2 witchTile = new Vector2(2484, 1795);
            Vector2 witchWorldMapped = ExpandedWorldTransform.MapWorld(witchTile * 16f);
            Vector2 shallowMapped = ExpandedWorldTransform.MapTile(new Vector2(4209, 944));
            Vector2 overrideMapped = ExpandedWorldTransform.MapTile(new Vector2(1553, 1778));

            Line("===== tsorcRevamp Expanded Adventure check =====", Color.Cyan);
            Line($"World title/id: {Main.worldName} / worldID={Main.worldID} (custom={VariousConstants.CUSTOM_MAP_WORLD_ID})", Color.Gray);
            Line($"GUID: {guid}", Color.Gray);
            Line($"maxTilesY={Main.maxTilesY}  maxTilesX={Main.maxTilesX}", Color.Gray);
            Line($"CheckForExpandedAdventure()={tsorcRevampWorld.CheckForExpandedAdventure()}", Color.White);
            Line($"Flags: ExpandedAdventure={tsorcRevampWorld.ExpandedAdventure}  OnlyAdventureMap={tsorcRevampWorld.OnlyAdventureMap}  RemixMap={tsorcRevampWorld.RemixMap}  CustomMap={tsorcRevampWorld.CustomMap}", Color.Yellow);
            Line($"Transform.Active={ExpandedWorldTransform.Active}  fold={ExpandedWorldTransform.FoldThreshold}  above=+{ExpandedWorldTransform.AboveFoldOffset}  below=+{ExpandedWorldTransform.BelowFoldOffset}", Color.White);
            Line($"Sample +400 WitchKing (2484,1795) -> tileY {witchWorldMapped.Y / 16f} (expect 2195 expanded, 1795 else)", Color.Lime);
            Line($"Sample +200 marker   (4209,944)  -> tileY {shallowMapped.Y} (expect 1144 expanded, 944 else)", Color.Lime);
            Line($"Sample override      (1553,1778) -> tileY {overrideMapped.Y} (expect 2178 expanded, 1778 else)", Color.Lime);
            Line("================================================", Color.Cyan);
        }
    }
}

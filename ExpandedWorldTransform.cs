using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp
{
    /// <summary>
    /// Runtime coordinate transform for the 2400-tall "Expanded Adventure" world.
    /// <para>
    /// The classic adventure map was expanded 2000 → 2400 tiles tall. All legacy content shifted
    /// <b>+200</b> (top insertion), except the dragged underworld which shifted <b>+400</b>. Rather than
    /// rewriting the ~175 hardcoded event literals / boss dicts / arena anchors (which are shared with the
    /// still-2000-tall Remix map and must stay in legacy space), every <b>legacy</b> world coordinate is routed
    /// through this transform at runtime. It is the identity on any non-expanded world (legacy adventure, remix,
    /// sandbox), so those maps are unaffected.
    /// </para>
    /// <para>
    /// Content authored natively in 2400-space (e.g. new events gated by an ExpandedAdventure condition) must
    /// <b>not</b> be routed through here — it is already in final space and would be double-shifted.
    /// </para>
    /// Verified offset (from the final 1.18.43 TEdit analysis): <c>newY = oldY + (oldY &gt;= 1791 ? 400 : 200)</c>,
    /// X unchanged, plus a small override table for the one structure that broke the formula. See
    /// <c>Documentation/WORLD_EXPANSION_PLAN.md</c> and <c>WorldData/ExpandedCoordOverrides.json</c>.
    /// </summary>
    public static class ExpandedWorldTransform
    {
        // Piecewise-formula constants. Defaults match the verified 1.18.43 offset; overridable from the JSON.
        public static int FoldThreshold { get; private set; } = 1791;
        public static int AboveFoldOffset { get; private set; } = 200;
        public static int BelowFoldOffset { get; private set; } = 400;

        // Manual overrides: legacy (tileX, tileY) -> final (tileX, tileY). Checked before the formula.
        private static Dictionary<(int, int), (int, int)> _overrides = new();
        // Reverse of _overrides: final (tileX, tileY) -> legacy (tileX, tileY). Used by InverseMapTile.
        private static Dictionary<(int, int), (int, int)> _reverseOverrides = new();

        private const string RelativePath = "WorldData/ExpandedCoordOverrides.json";

        /// <summary>True only when the currently-loaded world is the expanded 2400-tall adventure map.</summary>
        public static bool Active => tsorcRevampWorld.ExpandedAdventure;

        #region Loading

        // Mirrors the schema of WorldData/ExpandedCoordOverrides.json (unknown members like _comment are ignored).
        private class OverrideFile
        {
            public int? foldThreshold;
            public int? aboveFoldOffset;
            public int? belowFoldOffset;
            public List<OverrideEntry> overrides;
        }

        private class OverrideEntry
        {
            public int legacyX;
            public int legacyY;
            public int newX;
            public int newY;
            public string type;
            public string note;
        }

        /// <summary>Called once from <c>tsorcRevamp.Load()</c>. Parses the override table + formula constants.</summary>
        public static void Load()
        {
            _overrides = new Dictionary<(int, int), (int, int)>();
            _reverseOverrides = new Dictionary<(int, int), (int, int)>();

            string json = null;
            try
            {
                // Prefer a local ModSources copy so the file can be hot-edited without a rebuild (matches LoadDynamicEvents).
                string localPath = Path.Combine(Main.SavePath, "ModSources", "tsorcRevamp", RelativePath);
                if (File.Exists(localPath))
                {
                    json = File.ReadAllText(localPath);
                }
                else
                {
                    byte[] bytes = ModContent.GetInstance<tsorcRevamp>().GetFileBytes(RelativePath);
                    if (bytes != null)
                        json = System.Text.Encoding.UTF8.GetString(bytes);
                }
            }
            catch
            {
                // File missing/unreadable -> fall back to the built-in formula defaults silently.
                return;
            }

            if (string.IsNullOrWhiteSpace(json))
                return;

            OverrideFile data;
            try
            {
                data = JsonConvert.DeserializeObject<OverrideFile>(json);
            }
            catch
            {
                ModContent.GetInstance<tsorcRevamp>().Logger.Warn("ExpandedCoordOverrides.json failed to parse; using formula defaults.");
                return;
            }
            if (data == null)
                return;

            if (data.foldThreshold.HasValue) FoldThreshold = data.foldThreshold.Value;
            if (data.aboveFoldOffset.HasValue) AboveFoldOffset = data.aboveFoldOffset.Value;
            if (data.belowFoldOffset.HasValue) BelowFoldOffset = data.belowFoldOffset.Value;

            if (data.overrides != null)
            {
                foreach (OverrideEntry e in data.overrides)
                {
                    _overrides[(e.legacyX, e.legacyY)] = (e.newX, e.newY);
                    _reverseOverrides[(e.newX, e.newY)] = (e.legacyX, e.legacyY);
                }
            }
        }

        public static void Unload()
        {
            _overrides = null;
            _reverseOverrides = null;
            FoldThreshold = 1791;
            AboveFoldOffset = 200;
            BelowFoldOffset = 400;
        }

        #endregion

        #region Transform

        /// <summary>
        /// Returns the tile-space delta (dx, dy) to add to a legacy coordinate to land it on the current world.
        /// (0,0) on any non-expanded world.
        /// </summary>
        // Opt-in diagnostic (default off — most call sites are every-frame, so this would spam the log if it always
        // ran). Toggle with /expandedlog. Each unique legacy coord is only logged once per session (see _loggedCoords)
        // so a value computed once (e.g. Prime's teleport points, Gwyn's vision-chamber anchors) still leaves a
        // paper trail even though nothing else about it is visible in-game.
        public static bool DebugLog = false;
        private static readonly HashSet<(int, int)> _loggedCoords = new();

        private static (int dx, int dy) TileDelta(int legacyTileX, int legacyTileY)
        {
            if (!Active)
                return (0, 0);

            if (_overrides != null && _overrides.TryGetValue((legacyTileX, legacyTileY), out (int nx, int ny) ov))
            {
                LogOnce(legacyTileX, legacyTileY, ov.nx, ov.ny, "override");
                return (ov.nx - legacyTileX, ov.ny - legacyTileY);
            }

            int dy = legacyTileY >= FoldThreshold ? BelowFoldOffset : AboveFoldOffset;
            LogOnce(legacyTileX, legacyTileY, legacyTileX, legacyTileY + dy, "formula");
            return (0, dy);
        }

        private static void LogOnce(int legacyX, int legacyY, int newX, int newY, string source)
        {
            if (!DebugLog) return;
            if (!_loggedCoords.Add((legacyX, legacyY))) return; // already logged this exact legacy coord
            ModContent.GetInstance<tsorcRevamp>().Logger.Info(
                $"[ExpandedTransform] legacy ({legacyX},{legacyY}) -> ({newX},{newY}) [{source}]");
        }

        /// <summary>Maps an integer legacy tile coordinate to the current world's tile space.</summary>
        public static Point MapTile(int legacyTileX, int legacyTileY)
        {
            (int dx, int dy) = TileDelta(legacyTileX, legacyTileY);
            return new Point(legacyTileX + dx, legacyTileY + dy);
        }

        /// <summary>
        /// Maps a legacy tile-Y to the current world's tile space. X is passed for override lookups but is never
        /// changed by the transform, so most call sites only need the mapped Y. Identity on non-expanded worlds.
        /// </summary>
        public static int MapTileY(int legacyTileX, int legacyTileY)
        {
            return MapTile(legacyTileX, legacyTileY).Y;
        }

        /// <summary>
        /// Maps a legacy <b>tile-space</b> <see cref="Vector2"/> (as used by ScriptedEvent anchors) to the current
        /// world. The delta is computed from the floored tile but applied to the original vector, preserving any
        /// sub-tile fraction. Identity on non-expanded worlds.
        /// </summary>
        public static Vector2 MapTile(Vector2 legacyTilePos)
        {
            (int dx, int dy) = TileDelta((int)legacyTilePos.X, (int)legacyTilePos.Y);
            return new Vector2(legacyTilePos.X + dx, legacyTilePos.Y + dy);
        }

        /// <summary>
        /// Maps a legacy <b>world/pixel-space</b> <see cref="Vector2"/> (as used by <c>new Vector2(x, y) * 16</c>
        /// boss-arena anchors) to the current world. Identity on non-expanded worlds.
        /// </summary>
        public static Vector2 MapWorld(Vector2 legacyWorldPos)
        {
            if (!Active)
                return legacyWorldPos;

            (int dx, int dy) = TileDelta((int)(legacyWorldPos.X / 16f), (int)(legacyWorldPos.Y / 16f));
            return new Vector2(legacyWorldPos.X + dx * 16f, legacyWorldPos.Y + dy * 16f);
        }

        /// <summary>
        /// Inverse of <see cref="MapTile(int,int)"/>: current-world tile coord -> legacy tile coord. Identity on
        /// non-expanded worlds. Exact for all real content (override reverse-map + the injective piecewise formula).
        /// The only imprecise region is the unused "fold gap" (final-Y 1991–2190), which no legacy coord maps into;
        /// values there fall back to the +200 branch. Used when SAVING tome-authored (runtime-space) coords back to
        /// the legacy-space JSON so they don't double-shift on reload.
        /// </summary>
        public static Point InverseMapTile(int finalTileX, int finalTileY)
        {
            if (!Active)
                return new Point(finalTileX, finalTileY);

            if (_reverseOverrides != null && _reverseOverrides.TryGetValue((finalTileX, finalTileY), out (int lx, int ly) ov))
                return new Point(ov.lx, ov.ly);

            // Formula inverse. Forward is injective: legacyY < fold -> +Above (image .. fold+Above-1),
            // legacyY >= fold -> +Below (image fold+Below ..). Prefer the deep-underworld branch when it lands
            // at/after the fold; otherwise the shallow (+Above) branch.
            int candDeep = finalTileY - BelowFoldOffset;
            if (candDeep >= FoldThreshold)
                return new Point(finalTileX, candDeep);
            return new Point(finalTileX, finalTileY - AboveFoldOffset);
        }

        /// <summary>Inverse of <see cref="MapTile(Vector2)"/> for tile-space vectors. Preserves sub-tile fraction.</summary>
        public static Vector2 InverseMapTile(Vector2 finalTilePos)
        {
            if (!Active)
                return finalTilePos;

            Point legacy = InverseMapTile((int)finalTilePos.X, (int)finalTilePos.Y);
            return new Vector2(finalTilePos.X + (legacy.X - (int)finalTilePos.X), finalTilePos.Y + (legacy.Y - (int)finalTilePos.Y));
        }

        #endregion
    }
}

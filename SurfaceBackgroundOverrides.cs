using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp
{
    // Overrides Main.GetPreferredBGStyleForPlayer's surface background choice. Two independent
    // fixes share this hook because they run through the same ref-int result each frame and the
    // second depends on the first's output:
    //   - Shoreline zones (adventure map only): east reskins the ocean as snow/ice, west keeps the
    //     ocean backdrop showing past vanilla's fixed WorldGen.beachDistance (380 tile) cutoff.
    //   - Sticky biome (all worlds): vanilla falls back to plain Forest the instant you fly out of
    //     tile-detection range of a biome (e.g. ~20 tiles above desert sand). This remembers the
    //     last real biome style and keeps showing it until a genuinely different biome is detected,
    //     so flying straight up keeps the biome backdrop instead of losing it to open sky.
    public class SurfaceBackgroundOverrides : GlobalBackgroundStyle
    {
        public const int WesternOceanZoneWidth = 950;
        public const int EasternIceZoneWidth = 1900;
        public const int OceanSurfaceBackgroundStyle = 4;
        public const int SnowSurfaceBackgroundStyle = 7;
        public const int DesertSurfaceBackgroundStyle = 2;
        public const int JungleSurfaceBackgroundStyle = 3;
        public const int DefaultSurfaceBackgroundStyle = 0;
        public const int ForestTreeVariantStyle1 = 10;
        public const int ForestTreeVariantStyle2 = 11;
        public const int ForestTreeVariantStyle3 = 12;
        public const int GroundScanDepth = 200;

        private const int ForestProbeColumnsPerSide = 2;
        private const int ForestProbeSpacing = 4;

        private static int stickyBiomeStyle = DefaultSurfaceBackgroundStyle;

        public override void ChooseSurfaceBackgroundStyle(ref int style)
        {
            if (Main.gameMenu)
            {
                stickyBiomeStyle = DefaultSurfaceBackgroundStyle;
                return;
            }

            //Main.screenPosition can still be sitting at its pre-spawn default (near world origin) for a
            //few frames while the camera pans/fades onto a freshly-spawned player. Reading it before the
            //local player is active momentarily reported "near tile 0", which falsely landed inside the
            //western ocean zone and locked the sticky system onto Ocean until the player walked far enough
            //for the neutral-ground check to notice real forest ground and correct it.
            if (Main.myPlayer < 0 || Main.myPlayer >= Main.maxPlayers || !Main.player[Main.myPlayer].active)
            {
                return;
            }

            ApplyShorelineOverrides(ref style);
            ApplyStickyBiomePersistence(ref style);
        }

        private static void ApplyShorelineOverrides(ref int style)
        {
            if (!tsorcRevampWorld.ExpandedAdventure || tsorcRevampWorld.RemixMap)
            {
                return;
            }

            //Uses the player's own position rather than Main.screenPosition - always valid once the
            //player is active, unlike the camera which can lag behind on spawn (see the guard above).
            int centerTileX = (int)(Main.LocalPlayer.Center.X / 16f);
            int centerTileY = (int)(Main.LocalPlayer.Center.Y / 16f);
            bool aboveOceanDepth = centerTileY <= WorldGen.oceanLevel;

            if (!aboveOceanDepth)
            {
                return;
            }

            bool inEasternIceZone = centerTileX > Main.maxTilesX - EasternIceZoneWidth;
            if (inEasternIceZone)
            {
                style = SnowSurfaceBackgroundStyle;
                return;
            }

            // Fill in the gap left by vanilla's short ocean cutoff, and also claim jungle/desert here -
            // this far west the ocean is the dominant coastline even where jungle or desert tiles reach
            // the beach. Other real biomes (corruption/crimson/hallow/snow/etc.) are left untouched.
            bool inWesternOceanZone = centerTileX < WesternOceanZoneWidth;
            bool oceanOverridesHere = IsGenericSurfaceStyle(style)
                || style == DesertSurfaceBackgroundStyle
                || style == JungleSurfaceBackgroundStyle;

            if (inWesternOceanZone && oceanOverridesHere)
            {
                style = OceanSurfaceBackgroundStyle;
            }
        }

        private static bool IsGenericSurfaceStyle(int style)
        {
            return style == DefaultSurfaceBackgroundStyle
                || style == ForestTreeVariantStyle1
                || style == ForestTreeVariantStyle2
                || style == ForestTreeVariantStyle3;
        }

        private static void ApplyStickyBiomePersistence(ref int style)
        {
            bool isGenericFallback = IsGenericSurfaceStyle(style);

            if (!isGenericFallback)
            {
                stickyBiomeStyle = style;
                return;
            }

            if (stickyBiomeStyle == DefaultSurfaceBackgroundStyle)
            {
                return;
            }

            var centerTile = Main.LocalPlayer.Center.ToTileCoordinates();
            if (IsOverForestTerrain(centerTile.X, centerTile.Y))
            {
                stickyBiomeStyle = DefaultSurfaceBackgroundStyle;
                return;
            }

            style = stickyBiomeStyle;
        }

        private static bool IsOverForestTerrain(int centerTileX, int centerTileY)
        {
            int scanTopY = Math.Max(0, centerTileY);
            int scanBottomY = (int)Math.Min(Main.worldSurface, centerTileY + GroundScanDepth);
            int forestColumns = 0;

            // Confirm a forest transition with three of five nearby surface hits. Empty columns,
            // buildings, and other biome tiles leave the remembered background in place.
            for (int offset = -ForestProbeColumnsPerSide; offset <= ForestProbeColumnsPerSide; offset++)
            {
                int x = centerTileX + offset * ForestProbeSpacing;

                if (x < 0 || x >= Main.maxTilesX)
                {
                    continue;
                }

                for (int y = scanTopY; y <= scanBottomY; y++)
                {
                    Tile tile = Main.tile[x, y];

                    if (!tile.HasTile || tile.IsActuated || !Main.tileSolid[tile.TileType])
                    {
                        continue;
                    }

                    if (tile.TileType is TileID.Grass or TileID.Dirt or TileID.Stone)
                    {
                        forestColumns++;
                        if (forestColumns > ForestProbeColumnsPerSide)
                        {
                            return true;
                        }
                    }

                    break;
                }
            }

            return false;
        }
    }
}

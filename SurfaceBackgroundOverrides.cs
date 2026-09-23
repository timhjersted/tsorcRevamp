using System;
using System.Collections.Generic;
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

        // Tiles that mark ground as belonging to a specific biome rather than plain forest. Used to
        // tell "flying above a special biome, out of vanilla's detection range" (keep the sticky
        // style) apart from "actually standing over neutral dirt/stone ground" (a real forest).
        private static readonly HashSet<ushort> SpecialBiomeSurfaceTiles = new HashSet<ushort>
        {
            TileID.CorruptGrass, TileID.Ebonstone, TileID.Ebonsand, TileID.CorruptSandstone, TileID.CorruptHardenedSand, TileID.CorruptIce,
            TileID.CrimsonGrass, TileID.Crimstone, TileID.Crimsand, TileID.CrimsonSandstone, TileID.CrimsonHardenedSand, TileID.FleshIce,
            TileID.HallowedGrass, TileID.Pearlstone, TileID.Pearlsand, TileID.HallowSandstone, TileID.HallowHardenedSand, TileID.HallowedIce,
            TileID.Sand, TileID.Sandstone, TileID.HardenedSand,
            TileID.SnowBlock, TileID.IceBlock, TileID.BreakableIce,
            TileID.JungleGrass, TileID.Mud,
            TileID.MushroomGrass,
        };

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

            int centerTileX = (int)((Main.screenPosition.X + Main.screenWidth / 2f) / 16f);
            int centerTileY = (int)((Main.screenPosition.Y + Main.screenHeight / 2f) / 16f);

            if (IsNearNeutralGround(centerTileX, centerTileY))
            {
                stickyBiomeStyle = DefaultSurfaceBackgroundStyle;
                return;
            }

            if (stickyBiomeStyle != DefaultSurfaceBackgroundStyle)
            {
                style = stickyBiomeStyle;
            }
        }

        // Scans straight down from the screen center, bounded by the surface line so we never peek
        // into a biome's underground stone. A neutral tile (dirt/stone/forest grass) confirms real
        // ground-level forest; a special-biome tile or nothing solid within range means we're still
        // over/above a different biome, or too high to tell, so the sticky style is left alone.
        private static bool IsNearNeutralGround(int centerTileX, int centerTileY)
        {
            if (centerTileX < 0 || centerTileX >= Main.maxTilesX)
            {
                return false;
            }

            int scanBottomY = (int)Math.Min(Main.worldSurface, centerTileY + GroundScanDepth);

            for (int y = Math.Max(0, centerTileY); y <= scanBottomY; y++)
            {
                Tile tile = Main.tile[centerTileX, y];

                if (!tile.HasTile || tile.IsActuated || !Main.tileSolid[tile.TileType])
                {
                    continue;
                }

                return !SpecialBiomeSurfaceTiles.Contains(tile.TileType);
            }

            return false;
        }
    }
}

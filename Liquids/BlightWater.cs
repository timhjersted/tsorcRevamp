using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent.Liquid;
using Terraria.Graphics;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Liquids
{
    // Water and shimmer pools become Blight Water when any tile in the connected pool has a shadow-painted
    // background wall. The underlying liquid is unchanged - only its look and the buildup on contact differ.
    // Streams falling out of a blighted pool are blighted too (drawn dark, cause buildup on touch).
    internal static class BlightWaterCondition
    {
        private const ulong CacheTicks = 60;
        private static readonly Dictionary<int, bool> PoolResults = new();
        private static ulong cacheStartTick = ulong.MaxValue;

        internal static void ClearCache()
        {
            PoolResults.Clear();
            cacheStartTick = ulong.MaxValue;
        }

        internal static bool IsBlightableLiquid(Tile tile)
        {
            if (tile.LiquidAmount == 0)
            {
                return false;
            }

            return tile.LiquidType == LiquidID.Water || tile.LiquidType == LiquidID.Shimmer;
        }

        private static bool HasLiquidOfType(int x, int y, int liquidType)
        {
            if (!WorldGen.InWorld(x, y))
            {
                return false;
            }

            Tile tile = Main.tile[x, y];
            return tile.LiquidAmount > 0 && tile.LiquidType == liquidType;
        }

        // Flood-fills the connected pool (same liquid type only, so a water pool touching shimmer stays separate)
        // and caches every tile's result for CacheTicks (60 ticks = 1s) - painting a wall shows up within a second
        // without re-filling the pool on every draw call.
        internal static bool IsInfected(int x, int y)
        {
            if (!tsorcRevampWorld.SuperHardMode)
            {
                return false;
            }

            if (y < Main.worldSurface || y >= Main.UnderworldLayer || !WorldGen.InWorld(x, y))
            {
                return false;
            }

            Tile startTile = Main.tile[x, y];
            if (!IsBlightableLiquid(startTile))
            {
                return false;
            }

            ulong now = (ulong)Main.GameUpdateCount;
            if (cacheStartTick == ulong.MaxValue || now - cacheStartTick >= CacheTicks)
            {
                PoolResults.Clear();
                cacheStartTick = now;
            }

            int start = x + y * Main.maxTilesX;
            if (PoolResults.TryGetValue(start, out bool cachedResult))
            {
                return cachedResult;
            }

            int liquidType = startTile.LiquidType;
            Queue<int> queue = new();
            HashSet<int> component = new();
            queue.Enqueue(start);
            component.Add(start);
            bool infected = false;

            while (queue.Count > 0)
            {
                int index = queue.Dequeue();
                int tileX = index % Main.maxTilesX;
                int tileY = index / Main.maxTilesX;
                Tile tile = Main.tile[tileX, tileY];

                if (tile.WallType != WallID.None && tile.WallColor == PaintID.ShadowPaint)
                {
                    infected = true;
                }

                AddConnected(tileX - 1, tileY, liquidType, queue, component);
                AddConnected(tileX + 1, tileY, liquidType, queue, component);
                AddConnected(tileX, tileY - 1, liquidType, queue, component);
                AddConnected(tileX, tileY + 1, liquidType, queue, component);
            }

            foreach (int index in component)
            {
                PoolResults[index] = infected;
            }

            return infected;
        }

        private static void AddConnected(int x, int y, int liquidType, Queue<int> queue, HashSet<int> component)
        {
            if (!HasLiquidOfType(x, y, liquidType))
            {
                return;
            }

            int index = x + y * Main.maxTilesX;
            if (component.Add(index))
            {
                queue.Enqueue(index);
            }
        }

        // Liquid depth (in tiles, capped at 3) of the same liquid stacked above (x, y) - drives the surface-to-deep fade.
        internal static int DepthBelowSurface(int x, int y)
        {
            int liquidType = Main.tile[x, y].LiquidType;
            int depth = 0;

            while (depth < 3 && HasLiquidOfType(x, y - depth - 1, liquidType))
            {
                depth++;
            }

            return depth;
        }

        // A vanilla stream starts on a half-brick at (x, y) with liquid spilling over it from above or either side
        // (WaterfallManager.FindWaterfalls). The stream is blighted when that source liquid belongs to a blighted pool.
        internal static bool IsBlightedStreamSource(int x, int y)
        {
            if (!WorldGen.InWorld(x, y, 1))
            {
                return false;
            }

            Tile sourceTile = Main.tile[x, y];
            if (!sourceTile.HasTile || !sourceTile.IsHalfBlock)
            {
                return false;
            }

            bool aboveInfected = IsInfected(x, y - 1);
            bool leftInfected = IsInfected(x - 1, y);
            bool rightInfected = IsInfected(x + 1, y);

            return aboveInfected || leftInfected || rightInfected;
        }

        internal static bool TouchesInfectedPool(Player player)
        {
            if (!tsorcRevampWorld.SuperHardMode || !player.active || player.dead)
            {
                return false;
            }

            Rectangle hitbox = player.Hitbox;
            int left = Math.Max(0, hitbox.Left / 16);
            int right = Math.Min(Main.maxTilesX - 1, (hitbox.Right - 1) / 16);
            int top = Math.Max(0, hitbox.Top / 16);
            int bottom = Math.Min(Main.maxTilesY - 1, (hitbox.Bottom - 1) / 16);

            for (int x = left; x <= right; x++)
            {
                for (int y = top; y <= bottom; y++)
                {
                    Tile tile = Main.tile[x, y];
                    if (!IsBlightableLiquid(tile))
                    {
                        continue;
                    }

                    // Liquid fills a tile from the bottom up; LiquidAmount 255 = full 16px.
                    float liquidTop = (y + 1) * 16f - tile.LiquidAmount / 255f * 16f;
                    if (hitbox.Bottom > liquidTop && IsInfected(x, y))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }

    public class BlightWaterSystem : ModSystem
    {
        // Brightness multiplier for blighted stream segments (pools fade 0.30 -> 0.035 with depth; streams are thin, so one flat value).
        private const float StreamDarkness = 0.2f;

        // Water has no per-tile colour hook like shimmer, so pool darkening rides Lighting.GetCornerColors - but that's
        // called for every tile/wall draw too. This flag limits it to calls made inside LiquidRenderer.DrawNormalLiquids.
        private static bool drawingNormalLiquids;

        // Stream tracking. Vanilla doesn't pass a segment's source pool to the per-segment draw, so each stream is
        // identified by its first segment: a segment that doesn't continue on from the previous one (more than 1 tile
        // sideways, or not 0-1 tiles below) starts a new stream, and that tile is checked as the stream's source.
        private static bool drawingWaterfalls;
        private static bool currentStreamBlighted;
        private static int lastSegmentX = int.MinValue;
        private static int lastSegmentY = int.MinValue;

        // Tiles covered by blighted stream segments in the last waterfall draw - read by BlightWaterPlayer for contact
        // buildup. Draw-side only, so it exists on clients only, and only for on-screen streams.
        internal static readonly HashSet<Point> BlightedStreamTiles = new();

        public override void Load()
        {
            if (Main.dedServ)
            {
                return;
            }

            On_LiquidRenderer.SetShimmerVertexColors += DarkenShimmer;
            On_LiquidRenderer.SetShimmerVertexColors_Sparkle += DarkenShimmerSparkle;
            On_LiquidRenderer.DrawNormalLiquids += FlagNormalLiquidDraw;
            On_Lighting.GetCornerColors += DarkenBlightWater;
            On_WaterfallManager.Draw += TrackWaterfallDraw;
            On_WaterfallManager.DrawWaterfall_int_int_int_float_Vector2_Rectangle_Color_SpriteEffects += DarkenBlightStreamSegment;
        }

        public override void OnWorldUnload()
        {
            BlightWaterCondition.ClearCache();
            BlightedStreamTiles.Clear();
        }

        private static void FlagNormalLiquidDraw(On_LiquidRenderer.orig_DrawNormalLiquids orig, LiquidRenderer self,
            SpriteBatch spriteBatch, Vector2 drawOffset, int waterStyle, float globalAlpha, bool isBackgroundDraw)
        {
            drawingNormalLiquids = true;

            try
            {
                orig(self, spriteBatch, drawOffset, waterStyle, globalAlpha, isBackgroundDraw);
            }
            finally
            {
                drawingNormalLiquids = false;
            }
        }

        private static void DarkenBlightWater(On_Lighting.orig_GetCornerColors orig, int centerX, int centerY, out VertexColors vertices, float scale)
        {
            orig(centerX, centerY, out vertices, scale);

            if (!drawingNormalLiquids)
            {
                return;
            }

            float factor = PoolDarknessAt(centerX, centerY);
            DarkenVertices(ref vertices, factor);
        }

        // Rebuilds BlightedStreamTiles from scratch each waterfall draw and resets stream tracking.
        private static void TrackWaterfallDraw(On_WaterfallManager.orig_Draw orig, WaterfallManager self, SpriteBatch spriteBatch)
        {
            BlightedStreamTiles.Clear();
            drawingWaterfalls = true;
            currentStreamBlighted = false;
            lastSegmentX = int.MinValue;
            lastSegmentY = int.MinValue;

            try
            {
                orig(self, spriteBatch);
            }
            finally
            {
                drawingWaterfalls = false;
            }
        }

        private static void DarkenBlightStreamSegment(On_WaterfallManager.orig_DrawWaterfall_int_int_int_float_Vector2_Rectangle_Color_SpriteEffects orig,
            WaterfallManager self, int waterfallType, int x, int y, float opacity, Vector2 position, Rectangle sourceRect, Color color, SpriteEffects effects)
        {
            int sidewaysStep = Math.Abs(x - lastSegmentX);
            int downwardStep = y - lastSegmentY;
            bool continuesPreviousSegment = sidewaysStep <= 1 && downwardStep >= 0 && downwardStep <= 1;

            if (!continuesPreviousSegment)
            {
                currentStreamBlighted = BlightWaterCondition.IsBlightedStreamSource(x, y);
            }

            lastSegmentX = x;
            lastSegmentY = y;

            if (currentStreamBlighted)
            {
                BlightedStreamTiles.Add(new Point(x, y));
                color = Darken(color, StreamDarkness);
            }

            // Shimmer streams (type 25) ignore the color arg and recolour through SetShimmerVertexColors,
            // which DarkenShimmer handles via currentStreamBlighted.
            orig(self, waterfallType, x, y, opacity, position, sourceRect, color, effects);
        }

        private static void DarkenShimmer(On_LiquidRenderer.orig_SetShimmerVertexColors orig, ref VertexColors colors, float opacity, int x, int y)
        {
            orig(ref colors, opacity, x, y);

            float factor = ShimmerDarknessAt(x, y);
            DarkenVertices(ref colors, factor);
        }

        private static VertexColors DarkenShimmerSparkle(On_LiquidRenderer.orig_SetShimmerVertexColors_Sparkle orig,
            ref VertexColors colors, float opacity, int x, int y, bool top)
        {
            VertexColors sparkle = orig(ref colors, opacity, x, y, top);

            float factor = ShimmerDarknessAt(x, y);
            DarkenVertices(ref sparkle, factor);
            return sparkle;
        }

        // Shimmer recolouring runs for both pools and shimmer streams; inside a stream draw the stream's state decides.
        private static float ShimmerDarknessAt(int x, int y)
        {
            if (!drawingWaterfalls)
            {
                return PoolDarknessAt(x, y);
            }

            if (currentStreamBlighted)
            {
                return StreamDarkness;
            }

            return 1f;
        }

        // Brightness multiplier for a blighted pool tile: 0.30 at the surface fading to 0.035 three tiles down, so
        // pools read as a black mass with a faintly visible skin. 1 = not blighted, draw untouched.
        private static float PoolDarknessAt(int x, int y)
        {
            if (!BlightWaterCondition.IsInfected(x, y))
            {
                return 1f;
            }

            int depth = BlightWaterCondition.DepthBelowSurface(x, y);
            return MathHelper.Lerp(0.30f, 0.035f, depth / 3f);
        }

        private static void DarkenVertices(ref VertexColors vertices, float factor)
        {
            if (factor >= 1f)
            {
                return;
            }

            vertices.TopLeftColor = Darken(vertices.TopLeftColor, factor);
            vertices.TopRightColor = Darken(vertices.TopRightColor, factor);
            vertices.BottomLeftColor = Darken(vertices.BottomLeftColor, factor);
            vertices.BottomRightColor = Darken(vertices.BottomRightColor, factor);
        }

        // Desaturate to luminance (Rec. 709 weights), then scale by factor - turns blue/pink into grey-black.
        private static Color Darken(Color color, float factor)
        {
            float luminance = color.R * 0.2126f + color.G * 0.7152f + color.B * 0.0722f;
            byte shade = (byte)(luminance * factor);
            return new Color(shade, shade, shade, color.A);
        }
    }

    public class BlightWaterPlayer : ModPlayer
    {
        // 1 Blight buildup per TicksPerBuildup ticks spent touching blighted water/shimmer, pool or stream.
        // Leftover contact ticks carry over (not reset on leaving), so brief repeated dips still add up.
        private const int TicksPerBuildup = 5;
        private int poolContactTicks;

        // Stream contact is only known client-side (streams exist only in the draw code), so the owning client
        // batches it and reports through BlightPlayer.RequestBuildup every StreamReportInterval ticks (one packet
        // per 1/4s in MP instead of one per tick).
        private const int StreamReportInterval = 15;
        private int streamTicksPending;
        private int streamReportTimer;

        public override void PostUpdate()
        {
            bool touchingPool = BlightWaterCondition.TouchesInfectedPool(Player);

            // Pools: server (or single player) decides from synced tiles.
            if (Main.netMode != NetmodeID.MultiplayerClient && touchingPool)
            {
                poolContactTicks++;

                if (poolContactTicks >= TicksPerBuildup)
                {
                    poolContactTicks = 0;
                    Player.GetModPlayer<BlightPlayer>().ApplyBuildupAuthoritative(1, false);
                }
            }

            if (Main.dedServ || Player.whoAmI != Main.myPlayer)
            {
                return;
            }

            // Streams: owning client only. Skipped while in a pool so the two never stack.
            if (!touchingPool && TouchesBlightedStream())
            {
                streamTicksPending++;
            }

            streamReportTimer++;
            if (streamReportTimer < StreamReportInterval)
            {
                return;
            }

            streamReportTimer = 0;

            // Whole buildup points only; the remainder (< TicksPerBuildup) waits for the next report.
            int streamBuildup = streamTicksPending / TicksPerBuildup;
            if (streamBuildup > 0)
            {
                Player.GetModPlayer<BlightPlayer>().RequestBuildup(streamBuildup);
                streamTicksPending %= TicksPerBuildup;
            }
        }

        private bool TouchesBlightedStream()
        {
            if (!tsorcRevampWorld.SuperHardMode || !Player.active || Player.dead)
            {
                return false;
            }

            if (BlightWaterSystem.BlightedStreamTiles.Count == 0)
            {
                return false;
            }

            Rectangle hitbox = Player.Hitbox;
            int left = hitbox.Left / 16;
            int right = (hitbox.Right - 1) / 16;
            int top = hitbox.Top / 16;
            int bottom = (hitbox.Bottom - 1) / 16;

            for (int x = left; x <= right; x++)
            {
                for (int y = top; y <= bottom; y++)
                {
                    if (BlightWaterSystem.BlightedStreamTiles.Contains(new Point(x, y)))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}

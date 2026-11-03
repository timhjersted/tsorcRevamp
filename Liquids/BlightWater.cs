using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent.Liquid;
using Terraria.Graphics;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;

namespace tsorcRevamp.Liquids
{
    // Shimmer pools become Blight Water when any shimmer tile in the connected pool
    // has a shadow-painted background wall. The underlying liquid remains shimmer.
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

        private static bool IsShimmer(int x, int y)
        {
            if (!WorldGen.InWorld(x, y)) return false;
            Tile tile = Main.tile[x, y];
            return tile.LiquidAmount > 0 && tile.LiquidType == LiquidID.Shimmer;
        }

        internal static bool IsInfected(int x, int y)
        {
            if (!tsorcRevampWorld.SuperHardMode || y < Main.worldSurface || y >= Main.UnderworldLayer
                || !IsShimmer(x, y)) return false;

            ulong now = (ulong)Main.GameUpdateCount;
            if (cacheStartTick == ulong.MaxValue || now - cacheStartTick >= CacheTicks)
            {
                PoolResults.Clear();
                cacheStartTick = now;
            }

            int start = x + y * Main.maxTilesX;
            if (PoolResults.TryGetValue(start, out bool result)) return result;

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
                if (tile.WallType != 0 && tile.WallColor == PaintID.ShadowPaint)
                    infected = true;

                AddConnected(tileX - 1, tileY, queue, component);
                AddConnected(tileX + 1, tileY, queue, component);
                AddConnected(tileX, tileY - 1, queue, component);
                AddConnected(tileX, tileY + 1, queue, component);
            }

            foreach (int index in component)
                PoolResults[index] = infected;

            return infected;
        }

        private static void AddConnected(int x, int y, Queue<int> queue, HashSet<int> component)
        {
            if (!IsShimmer(x, y)) return;
            int index = x + y * Main.maxTilesX;
            if (component.Add(index)) queue.Enqueue(index);
        }

        internal static bool TouchesInfectedPool(Player player)
        {
            if (!tsorcRevampWorld.SuperHardMode || !player.active || player.dead) return false;

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
                    if (tile.LiquidAmount == 0 || tile.LiquidType != LiquidID.Shimmer) continue;

                    float liquidTop = (y + 1) * 16f - tile.LiquidAmount / 255f * 16f;
                    if (hitbox.Bottom > liquidTop && IsInfected(x, y)) return true;
                }
            }
            return false;
        }
    }

    public class BlightWaterSystem : ModSystem
    {
        public override void Load()
        {
            if (Main.dedServ) return;
            On_LiquidRenderer.SetShimmerVertexColors += DarkenShimmer;
            On_LiquidRenderer.SetShimmerVertexColors_Sparkle += DarkenShimmerSparkle;
        }

        public override void OnWorldUnload() => BlightWaterCondition.ClearCache();

        private static float DarknessAt(int x, int y)
        {
            if (!BlightWaterCondition.IsInfected(x, y)) return 1f;

            int depth = 0;
            while (depth < 3 && WorldGen.InWorld(x, y - depth - 1))
            {
                Tile above = Main.tile[x, y - depth - 1];
                if (above.LiquidAmount == 0 || above.LiquidType != LiquidID.Shimmer) break;
                depth++;
            }
            return MathHelper.Lerp(0.30f, 0.035f, depth / 3f);
        }

        private static Color Darken(Color color, float factor)
        {
            if (factor >= 1f) return color;
            byte shade = (byte)((color.R * 0.2126f + color.G * 0.7152f + color.B * 0.0722f) * factor);
            return new Color(shade, shade, shade, color.A);
        }

        private static void DarkenShimmer(On_LiquidRenderer.orig_SetShimmerVertexColors orig,
            ref VertexColors colors, float opacity, int x, int y)
        {
            orig(ref colors, opacity, x, y);
            float factor = DarknessAt(x, y);
            if (factor >= 1f) return;
            colors.TopLeftColor = Darken(colors.TopLeftColor, factor);
            colors.TopRightColor = Darken(colors.TopRightColor, factor);
            colors.BottomLeftColor = Darken(colors.BottomLeftColor, factor);
            colors.BottomRightColor = Darken(colors.BottomRightColor, factor);
        }

        private static VertexColors DarkenShimmerSparkle(On_LiquidRenderer.orig_SetShimmerVertexColors_Sparkle orig,
            ref VertexColors colors, float opacity, int x, int y, bool top)
        {
            VertexColors sparkle = orig(ref colors, opacity, x, y, top);
            float factor = DarknessAt(x, y);
            if (factor >= 1f) return sparkle;
            sparkle.TopLeftColor = Darken(sparkle.TopLeftColor, factor);
            sparkle.TopRightColor = Darken(sparkle.TopRightColor, factor);
            sparkle.BottomLeftColor = Darken(sparkle.BottomLeftColor, factor);
            sparkle.BottomRightColor = Darken(sparkle.BottomRightColor, factor);
            return sparkle;
        }
    }

    public class BlightWaterPlayer : ModPlayer
    {
        private const int BuildupPerTick = 3;

        public override void PostUpdate()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient || !BlightWaterCondition.TouchesInfectedPool(Player))
                return;

            Player.GetModPlayer<BlightPlayer>().ApplyBuildupAuthoritative(BuildupPerTick, false);
        }
    }
}

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.GameContent;
using Terraria.Graphics;
using Terraria.Graphics.Effects;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Backgrounds.AncientSpires
{
    [Autoload(Side = ModSide.Client)]
    public sealed class AncientSpires : ModSystem
    {
        private const int DrawScale = 2;
        private const int WaterlineRow = 346;
        private const int RippleBandHeight = 6;
        // Abyss vista shade: tiles of open air over which the vista fades from black at a dark wall/block edge up
        // to full art, and the light multiplier that lets real light (0.4 of 1 = full) override that fade.
        private const float EdgeFadeTiles = 4f;
        private const float VistaLightGain = 2.5f;
        private static readonly Color MistColor = new Color(55, 122, 121);
        private static readonly Color FogColor = new Color(111, 180, 167);

        // Stepped strands taper at both ends and stay above the close sheet's waterline.
        private static readonly (Rectangle Shape, float Coverage)[] FogStrokes =
        {
            (new Rectangle(126, -30, 74, 2), 0.07f),
            (new Rectangle(102, -28, 146, 2), 0.13f),
            (new Rectangle(78, -26, 166, 4), 0.17f),
            (new Rectangle(110, -22, 94, 2), 0.10f),
            (new Rectangle(388, -40, 70, 2), 0.07f),
            (new Rectangle(362, -38, 128, 2), 0.14f),
            (new Rectangle(350, -36, 120, 2), 0.12f),
            (new Rectangle(416, -34, 40, 2), 0.06f),
            (new Rectangle(756, -18, 104, 2), 0.07f),
            (new Rectangle(730, -16, 172, 2), 0.13f),
            (new Rectangle(704, -14, 226, 4), 0.18f),
            (new Rectangle(740, -10, 148, 2), 0.10f),
            (new Rectangle(786, -8, 70, 2), 0.06f)
        };

        private static readonly (float Parallax, float Period, int Direction, int OffsetX, int OffsetY, float Strength)[] FogCurrents =
        {
            (0.20f, 180f, -1, 316, -10, 0.58f),
            (0.37f, 120f, 1, 0, 0, 1f)
        };

        private (Asset<Texture2D> Texture, float Parallax, bool HasWater)[] layers;
        private readonly List<Rectangle> openRegions = new List<Rectangle>();
        private float[] vistaLitGrid = Array.Empty<float>();

        public override void Load()
        {
            string assetPath = UsefulFunctions.RefactorableFilepath(typeof(AncientSpires));
            var farLayer = ModContent.Request<Texture2D>(assetPath + "_Far", AssetRequestMode.ImmediateLoad);
            var middleLayer = ModContent.Request<Texture2D>(assetPath + "_Middle", AssetRequestMode.ImmediateLoad);
            var closeLayer = ModContent.Request<Texture2D>(assetPath + "_Close", AssetRequestMode.ImmediateLoad);

            layers = new[] { (farLayer, 0.15f, false), (middleLayer, 0.15f, false), (closeLayer, 0.37f, true) };
            On_OverlayManager.Draw += DrawBackground;
        }

        public override void Unload()
        {
            On_OverlayManager.Draw -= DrawBackground;
            layers = null;
        }

        private void DrawBackground(On_OverlayManager.orig_Draw original, OverlayManager manager,
            SpriteBatch spriteBatch, RenderLayers layer, bool beginSpriteBatch)
        {
            if (manager != Overlays.Scene || layer != RenderLayers.Background || beginSpriteBatch
                || Main.gameMenu || !Main.BackgroundEnabled || !tsorcRevampWorld.SuperHardMode || Main.shimmerAlpha >= 1f)
            {
                original(manager, spriteBatch, layer, beginSpriteBatch);
                return;
            }

            Player player = Main.LocalPlayer;
            bool underground = player.ZoneDirtLayerHeight || player.ZoneRockLayerHeight;
            if (!player.active || !player.ZoneGlowshroom || !underground)
            {
                original(manager, spriteBatch, layer, beginSpriteBatch);
                return;
            }

            Viewport viewport = spriteBatch.GraphicsDevice.Viewport;
            Matrix zoomMatrix = Main.GameViewMatrix.ZoomMatrix;
            float surfaceOffset = (float)(Main.worldSurface * 16) - Main.screenPosition.Y;
            Vector2 surfacePosition = new Vector2(0f, surfaceOffset);
            Vector2 zoomedSurface = Vector2.Transform(surfacePosition, zoomMatrix);
            int visibleTop = Math.Max(0, (int)Math.Ceiling(zoomedSurface.Y / DrawScale));
            int viewWidth = (viewport.Width + DrawScale - 1) / DrawScale;
            int viewHeight = (viewport.Height + DrawScale - 1) / DrawScale;
            if (visibleTop >= viewHeight)
            {
                original(manager, spriteBatch, layer, beginSpriteBatch);
                return;
            }

            openRegions.Clear();
            Rectangle visibleArea = new Rectangle(0, visibleTop, viewWidth, viewHeight - visibleTop);
            if (MethodSwaps.IsAbyssVisualActive())
            {
                FindOpenRegions(visibleArea, zoomMatrix);
            }
            else
            {
                openRegions.Add(visibleArea);
            }

            // Draw at the live background pass before walls and scene overlays. Use only the
            // gravity flip so gameplay zoom doesn't stretch the artwork's integer pixels.
            float opacity = 1f - Main.shimmerAlpha;
            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp,
                DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.EffectMatrix);

            foreach (Rectangle region in openRegions)
            {
                Rectangle mistArea = new Rectangle(region.X * DrawScale, region.Y * DrawScale,
                    region.Width * DrawScale, region.Height * DrawScale);
                spriteBatch.Draw(TextureAssets.MagicPixel.Value, mistArea, MistColor * opacity);
            }

            float visualTime = Main.GlobalTimeWrappedHourly;
            Color layerColor = Color.White * opacity;
            foreach (var (asset, parallax, hasWater) in layers)
            {
                Texture2D texture = asset.Value;
                int top = viewHeight - texture.Height;
                int cameraOffset = (int)Math.Floor(Main.screenPosition.X * parallax / DrawScale);
                int staticHeight = texture.Height;
                if (hasWater)
                {
                    staticHeight = WaterlineRow;
                }

                Rectangle staticStrip = new Rectangle(0, 0, texture.Width, staticHeight);
                DrawTiledStrip(spriteBatch, texture, staticStrip, top, cameraOffset, viewWidth, layerColor);
                if (!hasWater)
                {
                    continue;
                }

                // Shear the reflections in two-source-pixel steps while the waterline stays fixed.
                // The 4-second wave travels down the water band and weakens with depth.
                int waterHeight = texture.Height - WaterlineRow;
                float waveTime = visualTime % 4f * MathHelper.TwoPi / 4f;
                for (int row = WaterlineRow; row < texture.Height; row += RippleBandHeight)
                {
                    int bandDepth = row - WaterlineRow;
                    float depthRatio = bandDepth / (float)waterHeight;
                    float amplitude = MathHelper.Lerp(2.25f, 0.65f, depthRatio);
                    float phase = waveTime - bandDepth * 0.23f;
                    float wave = MathF.Sin(phase) * amplitude;
                    int rippleOffset = (int)MathF.Round(wave) * 2;
                    int height = Math.Min(RippleBandHeight, texture.Height - row);
                    Rectangle waterStrip = new Rectangle(0, row, texture.Width, height);
                    DrawTiledStrip(spriteBatch, texture, waterStrip, top, cameraOffset + rippleOffset, viewWidth, layerColor);
                }

                DrawFog(spriteBatch, viewWidth, texture.Width, top + WaterlineRow, visualTime, opacity);
            }

            spriteBatch.End();

            // Under the abyss, fade the vista into dark walls and blocks instead of cutting off full-bright at their
            // edge, and cover unlit fences (vanilla skips both the wall draw and DrawBlack on a dark light-passing wall).
            float abyssOpacity = MethodSwaps.AbyssVisualOpacity;
            if (abyssOpacity > 0f)
            {
                DrawVistaEdgeShade(abyssOpacity);
            }

            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
                DepthStencilState.None, Main.Rasterizer, null, Main.Transform);

            original(manager, spriteBatch, layer, beginSpriteBatch);
        }

        private void DrawTiledStrip(SpriteBatch spriteBatch, Texture2D texture, Rectangle strip,
            int textureTop, int offset, int viewWidth, Color color)
        {
            int startX = -(offset % strip.Width);
            if (startX > 0)
            {
                startX -= strip.Width;
            }

            // Slice the artwork on its source pixel grid, including the animated water bands.
            int top = textureTop + strip.Y;
            for (int x = startX; x < viewWidth; x += strip.Width)
            {
                Rectangle imageArea = new Rectangle(x, top, strip.Width, strip.Height);
                foreach (Rectangle region in openRegions)
                {
                    Rectangle clipped = Rectangle.Intersect(region, imageArea);
                    if (clipped.IsEmpty)
                    {
                        continue;
                    }

                    Rectangle source = new Rectangle(clipped.X - x, clipped.Y - textureTop, clipped.Width, clipped.Height);
                    Rectangle destination = new Rectangle(clipped.X * DrawScale, clipped.Y * DrawScale,
                        clipped.Width * DrawScale, clipped.Height * DrawScale);
                    spriteBatch.Draw(texture, destination, source, color);
                }
            }
        }

        private void DrawFog(SpriteBatch spriteBatch, int viewWidth, int repeatWidth, int waterline,
            float visualTime, float opacity)
        {
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            foreach (var (parallax, period, direction, offsetX, offsetY, strength) in FogCurrents)
            {
                // Two currents cross at different speeds, looping once per texture width.
                float cycle = visualTime % period / period;
                int drift = (int)(cycle * repeatWidth) * direction;
                int cameraOffset = (int)MathF.Floor(Main.screenPosition.X * parallax / DrawScale);
                int startX = (offsetX + drift - cameraOffset) % repeatWidth;
                if (startX > 0)
                {
                    startX -= repeatWidth;
                }

                for (int x = startX; x < viewWidth; x += repeatWidth)
                {
                    foreach (var (shape, coverage) in FogStrokes)
                    {
                        Rectangle wisp = shape;
                        wisp.Offset(x, waterline + offsetY);
                        Color tint = FogColor * (coverage * strength * opacity);
                        foreach (Rectangle region in openRegions)
                        {
                            Rectangle clipped = Rectangle.Intersect(region, wisp);
                            if (clipped.IsEmpty)
                            {
                                continue;
                            }

                            Rectangle destination = new Rectangle(clipped.X * DrawScale, clipped.Y * DrawScale,
                                clipped.Width * DrawScale, clipped.Height * DrawScale);
                            spriteBatch.Draw(pixel, destination, tint);
                        }
                    }
                }
            }
        }

        private void DrawVistaEdgeShade(float abyssOpacity)
        {
            // One black quad per on-screen tile below the surface line, its alpha set per corner from the lit grid,
            // so the GPU blends a gradient instead of a tile-edge step. Blocked cells get covered by the walls and
            // tiles drawn after this.
            int surfaceRow = (int)Math.Ceiling(Main.worldSurface);
            int firstTileX = Math.Max(1, (int)(Main.screenPosition.X / 16f) - 1);
            int firstTileY = Math.Max(surfaceRow, (int)(Main.screenPosition.Y / 16f) - 1);
            int endTileX = Math.Min(Main.maxTilesX - 2, (int)((Main.screenPosition.X + Main.screenWidth) / 16f) + 2);
            int endTileY = Math.Min(Main.maxTilesY - 2, (int)((Main.screenPosition.Y + Main.screenHeight) / 16f) + 2);
            if (endTileX <= firstTileX || endTileY <= firstTileY)
            {
                return;
            }

            // The grid adds one fade-width of margin so walls just off screen still fade the vista on screen.
            int margin = (int)Math.Ceiling(EdgeFadeTiles) + 1;
            int gridLeft = Math.Max(0, firstTileX - margin);
            int gridTop = Math.Max(0, firstTileY - margin);
            int gridRight = Math.Min(Main.maxTilesX, endTileX + margin);
            int gridBottom = Math.Min(Main.maxTilesY, endTileY + margin);
            int gridWidth = gridRight - gridLeft;
            int gridHeight = gridBottom - gridTop;
            BuildVistaLitGrid(gridLeft, gridTop, gridWidth, gridHeight);

            Texture2D pixel = TextureAssets.MagicPixel.Value;
            Rectangle pixelSource = new Rectangle(0, 0, 1, 1);
            Main.tileBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp,
                DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            for (int y = firstTileY; y < endTileY; y++)
            {
                for (int x = firstTileX; x < endTileX; x++)
                {
                    int gridX = x - gridLeft;
                    int gridY = y - gridTop;

                    VertexColors shade = new VertexColors();
                    shade.TopLeftColor = VistaShadeCorner(gridX, gridY, gridWidth, abyssOpacity);
                    shade.TopRightColor = VistaShadeCorner(gridX + 1, gridY, gridWidth, abyssOpacity);
                    shade.BottomLeftColor = VistaShadeCorner(gridX, gridY + 1, gridWidth, abyssOpacity);
                    shade.BottomRightColor = VistaShadeCorner(gridX + 1, gridY + 1, gridWidth, abyssOpacity);

                    // Fully lit tile: nothing to darken.
                    bool anyShade = shade.TopLeftColor.A > 0 || shade.TopRightColor.A > 0
                        || shade.BottomLeftColor.A > 0 || shade.BottomRightColor.A > 0;
                    if (!anyShade)
                    {
                        continue;
                    }

                    Vector2 tileScreenPosition = new Vector2(x * 16f, y * 16f) - Main.screenPosition;
                    Vector4 destination = new Vector4(tileScreenPosition.X, tileScreenPosition.Y, 16f, 16f);
                    Main.tileBatch.Draw(pixel, destination, pixelSource, shade);
                }
            }

            Main.tileBatch.End();
        }

        // Fills vistaLitGrid with how visible the vista is per tile: 0 = black, 1 = full art. Open air one fade-width
        // from any wall or block is always 1; it ramps down to 0 at the blocked cell. Real light also counts (max of
        // the two), so a wall lit by a lantern doesn't darken the vista beside it.
        private void BuildVistaLitGrid(int gridLeft, int gridTop, int gridWidth, int gridHeight)
        {
            int cellCount = gridWidth * gridHeight;
            if (vistaLitGrid.Length < cellCount)
            {
                vistaLitGrid = new float[cellCount];
            }

            // Seed distances: 0 on cells that hide the vista - a light-blocking solid tile or any wall, fences
            // included, because an unlit fence is skipped by vanilla and would leak the vista. "Far" elsewhere.
            bool showInvisible = Main.ShouldShowInvisibleWalls();
            float farDistance = EdgeFadeTiles + 1f;
            for (int y = 0; y < gridHeight; y++)
            {
                for (int x = 0; x < gridWidth; x++)
                {
                    Tile tile = Main.tile[gridLeft + x, gridTop + y];
                    bool blocksTile = WorldGen.SolidTile(tile) && Main.tileBlockLight[tile.TileType]
                        && (!tile.IsTileInvisible || showInvisible);
                    bool hasWall = tile.WallType != WallID.None && (!tile.IsWallInvisible || showInvisible);

                    float distance = farDistance;
                    if (blocksTile || hasWall)
                    {
                        distance = 0f;
                    }

                    vistaLitGrid[y * gridWidth + x] = distance;
                }
            }

            // Two-pass chamfer distance transform, in tiles (1 per straight step, 1.41 per diagonal).
            // Forward pass pulls from the left and the row above, backward pass from the right and the row below.
            const float DiagonalStep = 1.41f;
            for (int y = 0; y < gridHeight; y++)
            {
                for (int x = 0; x < gridWidth; x++)
                {
                    int index = y * gridWidth + x;
                    float best = vistaLitGrid[index];

                    if (x > 0)
                    {
                        best = Math.Min(best, vistaLitGrid[index - 1] + 1f);
                    }

                    if (y > 0)
                    {
                        best = Math.Min(best, vistaLitGrid[index - gridWidth] + 1f);

                        if (x > 0)
                        {
                            best = Math.Min(best, vistaLitGrid[index - gridWidth - 1] + DiagonalStep);
                        }

                        if (x < gridWidth - 1)
                        {
                            best = Math.Min(best, vistaLitGrid[index - gridWidth + 1] + DiagonalStep);
                        }
                    }

                    vistaLitGrid[index] = best;
                }
            }

            for (int y = gridHeight - 1; y >= 0; y--)
            {
                for (int x = gridWidth - 1; x >= 0; x--)
                {
                    int index = y * gridWidth + x;
                    float best = vistaLitGrid[index];

                    if (x < gridWidth - 1)
                    {
                        best = Math.Min(best, vistaLitGrid[index + 1] + 1f);
                    }

                    if (y < gridHeight - 1)
                    {
                        best = Math.Min(best, vistaLitGrid[index + gridWidth] + 1f);

                        if (x < gridWidth - 1)
                        {
                            best = Math.Min(best, vistaLitGrid[index + gridWidth + 1] + DiagonalStep);
                        }

                        if (x > 0)
                        {
                            best = Math.Min(best, vistaLitGrid[index + gridWidth - 1] + DiagonalStep);
                        }
                    }

                    vistaLitGrid[index] = best;
                }
            }

            // Distance -> visibility. SmoothStep eases both ends of the ramp so neither end shows a crease.
            for (int y = 0; y < gridHeight; y++)
            {
                for (int x = 0; x < gridWidth; x++)
                {
                    int index = y * gridWidth + x;
                    float edgeRamp = MathHelper.Clamp(vistaLitGrid[index] / EdgeFadeTiles, 0f, 1f);
                    float edgeLit = MathHelper.SmoothStep(0f, 1f, edgeRamp);

                    Color light = Lighting.GetColor(gridLeft + x, gridTop + y);
                    float brightness = Math.Max(light.R, Math.Max(light.G, light.B)) / 255f;
                    float lightLit = MathHelper.Clamp(brightness * VistaLightGain, 0f, 1f);

                    vistaLitGrid[index] = Math.Max(edgeLit, lightLit);
                }
            }
        }

        // Darkness at one tile corner: the average visibility of the four cells sharing it (corner = top-left of
        // cell cornerX/cornerY). Scaled by the abyss fade so the shade eases in on entry. Returns premultiplied black.
        private Color VistaShadeCorner(int cornerX, int cornerY, int gridWidth, float abyssOpacity)
        {
            int belowRight = cornerY * gridWidth + cornerX;
            int aboveRight = belowRight - gridWidth;
            float lit = (vistaLitGrid[aboveRight - 1] + vistaLitGrid[aboveRight]
                + vistaLitGrid[belowRight - 1] + vistaLitGrid[belowRight]) * 0.25f;

            float darkness = (1f - lit) * abyssOpacity;
            return Color.Black * darkness;
        }

        private void FindOpenRegions(Rectangle visibleArea, Matrix zoomMatrix)
        {
            // Preserve the earlier Abyss draw by collecting gaps between opaque blocks and walls.
            // Project world tiles onto the same 2x screen grid as the artwork before clipping.
            Vector2 origin = Vector2.Transform(-Main.screenPosition, zoomMatrix) / DrawScale;
            float tileWidth = 16f * zoomMatrix.M11 / DrawScale;
            float tileHeight = 16f * zoomMatrix.M22 / DrawScale;
            int firstTileX = Math.Max(0, (int)Math.Floor((visibleArea.Left - origin.X) / tileWidth));
            int firstTileY = Math.Max(0, (int)Math.Floor((visibleArea.Top - origin.Y) / tileHeight));
            int endTileX = Math.Min(Main.maxTilesX, (int)Math.Ceiling((visibleArea.Right - origin.X) / tileWidth));
            int endTileY = Math.Min(Main.maxTilesY, (int)Math.Ceiling((visibleArea.Bottom - origin.Y) / tileHeight));
            bool showInvisibleTiles = Main.ShouldShowInvisibleWalls();

            for (int y = firstTileY; y < endTileY; y++)
            {
                int rowTop = (int)Math.Round(origin.Y + y * tileHeight);
                int rowBottom = (int)Math.Round(origin.Y + (y + 1) * tileHeight);
                rowTop = Math.Max(rowTop, visibleArea.Top);
                rowBottom = Math.Min(rowBottom, visibleArea.Bottom);
                if (rowBottom <= rowTop)
                {
                    continue;
                }

                int openStart = visibleArea.Left;
                for (int x = firstTileX; x < endTileX; x++)
                {
                    Tile tile = Main.tile[x, y];
                    bool blocksTile = WorldGen.SolidTile(tile) && Main.tileBlockLight[tile.TileType]
                        && (!tile.IsTileInvisible || showInvisibleTiles);
                    bool blocksWall = !Main.WallLightAt(x, y, showInvisibleTiles);
                    if (!blocksTile && !blocksWall)
                    {
                        continue;
                    }

                    int tileLeft = (int)Math.Round(origin.X + x * tileWidth);
                    tileLeft = Math.Min(tileLeft, visibleArea.Right);
                    if (tileLeft > openStart)
                    {
                        openRegions.Add(new Rectangle(openStart, rowTop, tileLeft - openStart, rowBottom - rowTop));
                    }

                    int tileRight = (int)Math.Round(origin.X + (x + 1) * tileWidth);
                    openStart = Math.Clamp(tileRight, visibleArea.Left, visibleArea.Right);
                }

                if (openStart < visibleArea.Right)
                {
                    openRegions.Add(new Rectangle(openStart, rowTop, visibleArea.Right - openStart, rowBottom - rowTop));
                }
            }
        }
    }
}

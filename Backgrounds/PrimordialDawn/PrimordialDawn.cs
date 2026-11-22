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

namespace tsorcRevamp.Backgrounds.PrimordialDawn
{
    [Autoload(Side = ModSide.Client)]
    public sealed class PrimordialDawn : ModSystem
    {
        private const int DrawScale = 2;
        private const int HorizonRow = 346;
        private const float VerticalParallax = 0.06f;
        private const float EdgeFadeTiles = 4f;
        private const float VistaLightGain = 2.5f;

        private static readonly Color[] NightSky =
        {
            new Color(30, 14, 34),
            new Color(58, 20, 42),
            new Color(98, 30, 44),
            new Color(150, 50, 44),
            new Color(208, 86, 50),
            new Color(238, 132, 62)
        };

        private static readonly Color[] DawnSky =
        {
            new Color(60, 48, 92),
            new Color(122, 70, 104),
            new Color(196, 100, 92),
            new Color(240, 150, 92),
            new Color(255, 198, 120),
            new Color(255, 232, 176)
        };

        private (Asset<Texture2D> Texture, float Parallax)[] layers;
        private Asset<Texture2D> skyVeil;
        private readonly List<Rectangle> openRegions = new List<Rectangle>();
        private float[] vistaLitGrid = Array.Empty<float>();

        public override void Load()
        {
            string assetPath = UsefulFunctions.RefactorableFilepath(typeof(PrimordialDawn));
            Asset<Texture2D> farLayer = ModContent.Request<Texture2D>(assetPath + "_Far", AssetRequestMode.ImmediateLoad);
            Asset<Texture2D> middleLayer = ModContent.Request<Texture2D>(assetPath + "_Middle", AssetRequestMode.ImmediateLoad);
            Asset<Texture2D> closeLayer = ModContent.Request<Texture2D>(assetPath + "_Close", AssetRequestMode.ImmediateLoad);
            skyVeil = ModContent.Request<Texture2D>(assetPath + "_SkyVeil", AssetRequestMode.ImmediateLoad);
            layers = new[]
            {
                (farLayer, 0.08f),
                (middleLayer, 0.15f),
                (closeLayer, 0.37f)
            };

            On_OverlayManager.Draw += DrawBackground;
        }

        public override void Unload()
        {
            On_OverlayManager.Draw -= DrawBackground;
            layers = null;
            skyVeil = null;
            openRegions.Clear();
            vistaLitGrid = Array.Empty<float>();
        }

        private void DrawBackground(On_OverlayManager.orig_Draw original, OverlayManager manager,
            SpriteBatch spriteBatch, RenderLayers layer, bool beginSpriteBatch)
        {
            if (manager != Overlays.Scene || layer != RenderLayers.Background || beginSpriteBatch
                || Main.gameMenu || !Main.BackgroundEnabled || !tsorcRevampWorld.PrimordialAge
                || Main.shimmerAlpha >= 1f || Main.myPlayer < 0 || Main.myPlayer >= Main.maxPlayers)
            {
                original(manager, spriteBatch, layer, beginSpriteBatch);
                return;
            }

            Player player = Main.LocalPlayer;
            if (!player.active)
            {
                original(manager, spriteBatch, layer, beginSpriteBatch);
                return;
            }

            Viewport viewport = spriteBatch.GraphicsDevice.Viewport;
            Matrix zoomMatrix = Main.GameViewMatrix.ZoomMatrix;
            int viewWidth = (viewport.Width + DrawScale - 1) / DrawScale;
            int viewHeight = (viewport.Height + DrawScale - 1) / DrawScale;
            float surfaceOffset = (float)(Main.worldSurface * 16) - Main.screenPosition.Y;
            Vector2 zoomedSurface = Vector2.Transform(new Vector2(0f, surfaceOffset), zoomMatrix);
            int visibleBottom = Math.Clamp((int)Math.Ceiling(zoomedSurface.Y / DrawScale), 0, viewHeight);
            if (visibleBottom == 0)
            {
                original(manager, spriteBatch, layer, beginSpriteBatch);
                return;
            }

            // Combat voids retain their priority. The renderer follows the synced world flag locally.
            float opacity = (1f - Main.shimmerAlpha) * (1f - MethodSwaps.AbyssVisualOpacity);
            if (opacity <= 0f)
            {
                original(manager, spriteBatch, layer, beginSpriteBatch);
                return;
            }

            openRegions.Clear();
            FindOpenRegions(new Rectangle(0, 0, viewWidth, visibleBottom), zoomMatrix);

            // The horizon follows vertical travel gently while the sheets retain their native 2x pixels.
            // High in space the terrain slips below the screen, leaving the primordial sky visible.
            int cameraHeight = (int)Math.Round((surfaceOffset - Main.screenHeight) * VerticalParallax / DrawScale);
            int terrainTop = viewHeight - layers[0].Texture.Value.Height + cameraHeight;
            int skyTop = viewHeight - skyVeil.Value.Height;
            int veilTop = Math.Min(0, skyTop);
            float daylight = 0f;
            if (Main.dayTime)
            {
                double distanceFromNight = Math.Min(Main.time, 54000d - Main.time);
                float sunriseProgress = MathHelper.Clamp((float)(distanceFromNight / 3600d), 0f, 1f);
                daylight = MathHelper.SmoothStep(0f, 1f, sunriseProgress);
            }

            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp,
                DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.EffectMatrix);

            DrawSky(spriteBatch, viewWidth, visibleBottom, skyTop, daylight, opacity);

            int skyOffset = (int)Math.Floor(Main.screenPosition.X * 0.025f / DrawScale);
            Color veilColor = Color.White * (opacity * MathHelper.Lerp(0.39f, 0.20f, daylight));
            Texture2D veil = skyVeil.Value;
            DrawTiledStrip(spriteBatch, veil, new Rectangle(0, 0, veil.Width, veil.Height),
                veilTop, skyOffset, viewWidth, veilColor);

            Color layerColor = Color.White * opacity;
            foreach (var (asset, parallax) in layers)
            {
                Texture2D texture = asset.Value;
                int cameraOffset = (int)Math.Floor(Main.screenPosition.X * parallax / DrawScale);
                Rectangle textureArea = new Rectangle(0, 0, texture.Width, texture.Height);
                DrawTiledStrip(spriteBatch, texture, textureArea, terrainTop, cameraOffset, viewWidth, layerColor);
            }

            // Extend the rock plain when vertical travel lifts the bottom of the sheet above the viewport.
            int plainBottom = terrainTop + layers[2].Texture.Value.Height;
            if (plainBottom < visibleBottom)
            {
                Color deepRock = new Color(27, 23, 35) * opacity;
                DrawClippedFill(spriteBatch, new Rectangle(0, plainBottom, viewWidth, visibleBottom - plainBottom), deepRock);
            }

            spriteBatch.End();
            DrawVistaEdgeShade(opacity);
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
                DepthStencilState.None, Main.Rasterizer, null, Main.Transform);

            original(manager, spriteBatch, layer, beginSpriteBatch);
        }

        private void DrawSky(SpriteBatch spriteBatch, int viewWidth, int visibleBottom, int skyTop,
            float daylight, float opacity)
        {
            // Match the approved stepped ramps, covering the vanilla sky on the same 2x pixel grid.
            if (skyTop > 0)
            {
                Color upperSky = Color.Lerp(NightSky[0], DawnSky[0], daylight) * opacity;
                DrawClippedFill(spriteBatch, new Rectangle(0, 0, viewWidth, Math.Min(skyTop, visibleBottom)), upperSky);
            }

            for (int row = Math.Max(0, skyTop); row < visibleBottom; row++)
            {
                float depth = MathHelper.Clamp((row - skyTop) / (float)HorizonRow, 0f, 1f);
                float rampPosition = (float)Math.Round(Math.Pow(depth, 1.5f) * 15f) / 3f;
                int band = Math.Min(4, (int)rampPosition);
                float fraction = rampPosition - band;
                Color night = Color.Lerp(NightSky[band], NightSky[band + 1], fraction);
                Color dawn = Color.Lerp(DawnSky[band], DawnSky[band + 1], fraction);
                Color skyColor = Color.Lerp(night, dawn, daylight) * opacity;
                DrawClippedFill(spriteBatch, new Rectangle(0, row, viewWidth, 1), skyColor);
            }

            if (Main.dayTime && daylight > 0f)
            {
                float dayProgress = MathHelper.Clamp((float)(Main.time / 54000d), 0f, 1f);
                float sunArc = (float)Math.Sin(dayProgress * MathHelper.Pi);
                int centerX = (int)Math.Round(viewWidth * MathHelper.Lerp(0.15f, 0.85f, dayProgress));
                int centerY = skyTop + HorizonRow - (int)Math.Round(88f * sunArc);
                const int Radius = 62;
                Color sunColor = new Color(255, 246, 214) * (opacity * daylight);
                for (int row = -Radius + 1; row < Radius; row++)
                {
                    int halfWidth = (int)Math.Ceiling(Math.Sqrt(Radius * Radius - row * row)) - 1;
                    Rectangle sunStrip = new Rectangle(centerX - halfWidth, centerY + row, halfWidth * 2 + 1, 1);
                    DrawClippedFill(spriteBatch, sunStrip, sunColor);
                }
            }
        }

        private void DrawClippedFill(SpriteBatch spriteBatch, Rectangle area, Color color)
        {
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            Rectangle pixelSource = new Rectangle(0, 0, 1, 1);
            foreach (Rectangle region in openRegions)
            {
                Rectangle clipped = Rectangle.Intersect(region, area);
                if (clipped.IsEmpty)
                {
                    continue;
                }

                Rectangle destination = new Rectangle(clipped.X * DrawScale, clipped.Y * DrawScale,
                    clipped.Width * DrawScale, clipped.Height * DrawScale);
                spriteBatch.Draw(pixel, destination, pixelSource, color);
            }
        }

        private void DrawTiledStrip(SpriteBatch spriteBatch, Texture2D texture, Rectangle strip,
            int textureTop, int offset, int viewWidth, Color color)
        {
            int startX = -(offset % strip.Width);
            if (startX > 0)
            {
                startX -= strip.Width;
            }

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

        private void DrawVistaEdgeShade(float opacity)
        {
            // Match Ancient Spires: one black quad per visible surface tile, with four smoothly interpolated corners.
            // The four-tile wall fade yields to actual local light at 0.4 brightness. Tiles and walls draw afterward.
            int surfaceBottom = Math.Min(Main.maxTilesY - 1, (int)Math.Ceiling(Main.worldSurface));
            int firstTileX = Math.Max(1, (int)(Main.screenPosition.X / 16f) - 1);
            int firstTileY = Math.Max(1, (int)(Main.screenPosition.Y / 16f) - 1);
            int endTileX = Math.Min(Main.maxTilesX - 2, (int)((Main.screenPosition.X + Main.screenWidth) / 16f) + 2);
            int endTileY = Math.Min(surfaceBottom, (int)((Main.screenPosition.Y + Main.screenHeight) / 16f) + 2);
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
                    shade.TopLeftColor = VistaShadeCorner(gridX, gridY, gridWidth, opacity);
                    shade.TopRightColor = VistaShadeCorner(gridX + 1, gridY, gridWidth, opacity);
                    shade.BottomLeftColor = VistaShadeCorner(gridX, gridY + 1, gridWidth, opacity);
                    shade.BottomRightColor = VistaShadeCorner(gridX + 1, gridY + 1, gridWidth, opacity);

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
        private Color VistaShadeCorner(int cornerX, int cornerY, int gridWidth, float opacity)
        {
            int belowRight = cornerY * gridWidth + cornerX;
            int aboveRight = belowRight - gridWidth;
            float lit = (vistaLitGrid[aboveRight - 1] + vistaLitGrid[aboveRight]
                + vistaLitGrid[belowRight - 1] + vistaLitGrid[belowRight]) * 0.25f;

            float darkness = (1f - lit) * opacity;
            return Color.Black * darkness;
        }

        private void FindOpenRegions(Rectangle visibleArea, Matrix zoomMatrix)
        {
            // Clip the backdrop to open surface air, respecting echo coating and light-passing walls.
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

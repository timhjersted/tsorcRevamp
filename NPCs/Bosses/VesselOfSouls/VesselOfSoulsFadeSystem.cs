using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.Graphics.Effects;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs;

namespace tsorcRevamp.NPCs.Bosses.VesselOfSouls
{
    ///<summary>
    ///Client-side presentation for the Vessel of Souls' phase 2. The world-hide state is derived from the
    ///local player's VesselVoid buff, so it follows the same authoritative lifetime as the phase itself.
    ///No tile or wall data is changed.
    ///</summary>
    public class VesselOfSoulsFadeSystem : ModSystem
    {
        private const string InteriorOverlayKey = "tsorcRevamp:VesselInterior";
        public static float FadeAlpha;

        // Cached once per update; GlobalTile/GlobalWall query this for every visible block while drawing.
        public static bool WorldHidden { get; private set; }

        public override void Load()
        {
            if (!Main.dedServ)
                Overlays.Scene[InteriorOverlayKey] = new VesselInteriorOverlay();
        }

        public override void PostUpdateEverything()
        {
            WorldHidden = !Main.dedServ && !Main.gameMenu && Main.LocalPlayer != null
                && Main.LocalPlayer.active && Main.LocalPlayer.HasBuff(ModContent.BuffType<VesselVoid>());

            Overlay overlay = Main.dedServ ? null : Overlays.Scene[InteriorOverlayKey];
            if (overlay != null)
            {
                if (WorldHidden && (overlay.Mode == OverlayMode.Inactive || overlay.Mode == OverlayMode.FadeOut))
                    Overlays.Scene.Activate(InteriorOverlayKey);
                else if (!WorldHidden && overlay.Mode != OverlayMode.Inactive && overlay.Mode != OverlayMode.FadeOut)
                    Overlays.Scene.Deactivate(InteriorOverlayKey);
            }

            if (!WorldHidden)
                return;

            // Hiding tiles does not change Terraria's light map. Seed neutral light across the visible
            // screen so players, NPCs, projectiles, items, and the retained platforms remain readable.
            const int spacing = 8;
            int left = Utils.Clamp((int)(Main.screenPosition.X / 16f) - spacing, 1, Main.maxTilesX - 2);
            int right = Utils.Clamp((int)((Main.screenPosition.X + Main.screenWidth) / 16f) + spacing, 1, Main.maxTilesX - 2);
            int top = Utils.Clamp((int)(Main.screenPosition.Y / 16f) - spacing, 1, Main.maxTilesY - 2);
            int bottom = Utils.Clamp((int)((Main.screenPosition.Y + Main.screenHeight) / 16f) + spacing, 1, Main.maxTilesY - 2);

            for (int x = left; x <= right; x += spacing)
                for (int y = top; y <= bottom; y += spacing)
                    Lighting.AddLight(x, y, 0.9f, 0.9f, 0.9f);
        }

        public override void OnWorldUnload()
        {
            Overlay overlay = Main.dedServ ? null : Overlays.Scene[InteriorOverlayKey];
            if (overlay != null && overlay.Mode != OverlayMode.Inactive && overlay.Mode != OverlayMode.FadeOut)
                Overlays.Scene.Deactivate(InteriorOverlayKey);
            WorldHidden = false;
            FadeAlpha = 0f;
        }

        public override void Unload()
        {
            WorldHidden = false;
            FadeAlpha = 0f;
        }

        public override void PostDrawInterface(SpriteBatch spriteBatch)
        {
            if (Main.gameMenu) return;
            Texture2D px = TextureAssets.MagicPixel.Value;
            Rectangle full = new Rectangle(0, 0, Main.screenWidth, Main.screenHeight);
            Rectangle src = new Rectangle(0, 0, 1, 1);

            // Black swallow fade. The old phase-2 wash was removed because PostDrawInterface tinted the
            // UI as well as the world instead of behaving like Artorias' in-world abyss presentation.
            float a = MathHelper.Clamp(FadeAlpha, 0f, 1f);
            if (a > 0.001f)
                spriteBatch.Draw(px, full, src, Color.Black * a);
        }
    }

    /// <summary>
    /// Drawn at Terraria's wall layer: after the interior background but before tiles, NPCs, players,
    /// and projectiles. VesselVoidSpace supplies slow organic currents without tinting the UI.
    /// </summary>
    internal sealed class VesselInteriorOverlay : Overlay
    {
        private static bool backgroundFailureLogged;
        private static bool fogFailureLogged;

        public VesselInteriorOverlay() : base(EffectPriority.VeryHigh, RenderLayers.Walls) { }

        public override void Draw(SpriteBatch spriteBatch)
        {
            float opacity = MathHelper.Clamp(Opacity, 0f, 1f);
            if (opacity > 0.001f)
            {
                try
                {
                    // Surface backgrounds are followed by Terraria's cavern/underworld layers, which
                    // can cover a custom image completely. Reapply this optional illustration here at
                    // the wall layer, immediately before fog and all gameplay entities.
                    global::tsorcRevamp.MethodSwaps.DrawVesselInteriorBackground(opacity);
                }
                catch (System.Exception exception)
                {
                    if (!backgroundFailureLogged)
                    {
                        backgroundFailureLogged = true;
                        ModContent.GetInstance<VesselOfSoulsFadeSystem>().Mod.Logger.Warn(
                            "Vessel interior background failed; continuing with the star field.", exception);
                    }
                }

                try
                {
                    DrawVesselVoidSpace(spriteBatch, opacity);
                }
                catch (System.Exception exception)
                {
                    // OverlayManager otherwise lets this abort Terraria's combined wall/non-solid-tile
                    // pass. Disable only this visual for the frame and leave the world renderer intact.
                    if (!fogFailureLogged)
                    {
                        fogFailureLogged = true;
                        ModContent.GetInstance<VesselOfSoulsFadeSystem>().Mod.Logger.Warn(
                            "Vessel ringless fog failed; continuing without the fog overlay.", exception);
                    }
                }
            }

            // Terraria's full tile pass is suppressed while the Vessel void is active so cached solid,
            // vine, and biome tiles cannot leak through. Platforms are the one intentional exception.
            if (VesselOfSoulsFadeSystem.WorldHidden)
                DrawVisiblePlatforms(spriteBatch);
        }

        private static void DrawVisiblePlatforms(SpriteBatch spriteBatch)
        {
            const int padding = 2;
            int left = Utils.Clamp((int)(Main.screenPosition.X / 16f) - padding, 1, Main.maxTilesX - 2);
            int right = Utils.Clamp((int)((Main.screenPosition.X + Main.screenWidth) / 16f) + padding, 1, Main.maxTilesX - 2);
            int top = Utils.Clamp((int)(Main.screenPosition.Y / 16f) - padding, 1, Main.maxTilesY - 2);
            int bottom = Utils.Clamp((int)((Main.screenPosition.Y + Main.screenHeight) / 16f) + padding, 1, Main.maxTilesY - 2);

            for (int x = left; x <= right; x++)
            {
                for (int y = top; y <= bottom; y++)
                {
                    Tile tile = Main.tile[x, y];
                    if (!tile.HasTile || tile.IsActuated || !TileID.Sets.Platforms[tile.TileType])
                        continue;

                    Main.instance.LoadTiles(tile.TileType);
                    Texture2D texture = TextureAssets.Tile[tile.TileType].Value;
                    Rectangle source = new Rectangle(tile.TileFrameX, tile.TileFrameY, 16, 16);
                    Vector2 position = new Vector2(x * 16f, y * 16f) - Main.screenPosition;
                    spriteBatch.Draw(texture, position, source, Lighting.GetColor(x, y));
                }
            }
        }

        private static void DrawVesselVoidSpace(SpriteBatch spriteBatch, float opacity)
        {
            // Full opacity: the shader now owns the whole density profile (near-transparent at the
            // centre so the interior background reads through, weight carried at the edges and
            // corners). The old 0.76 scale-down existed only because the fog was a flat wash.
            Projectiles.Enemy.VesselOfSouls.VesselVFX.DrawVoidSpace(
                spriteBatch, MathHelper.Clamp(opacity, 0f, 1f));
        }

        public override void Update(GameTime gameTime)
        {
        }

        public override void Activate(Vector2 position, params object[] args)
        {
            Mode = OverlayMode.FadeIn;
        }

        public override void Deactivate(params object[] args)
        {
            Mode = OverlayMode.FadeOut;
        }

        public override bool IsVisible()
        {
            return Opacity > 0.001f;
        }
    }
}

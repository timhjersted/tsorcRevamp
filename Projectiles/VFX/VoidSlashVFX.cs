using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.VFX
{
    /// <summary>
    /// Shared procedural sword-slash shader, both flavors driven by Effects/NitoReaperTrail.fx:
    /// <see cref="DrawSweep"/> is the broad rotating blade-shaped crescent for overhead/horizontal
    /// swings, <see cref="DrawThrust"/> the narrower straight sheath for stabs/lunges. Originated as
    /// Gravelord Nito's "Reaper Sweep" (the .fx's internal technique names still say so - renaming
    /// them would mean recompiling the shipped .xnb, not worth it for an invisible implementation
    /// detail), but the construction itself is boss-agnostic: every caller supplies its own size
    /// (scale it to the wielder's actual blade reach), palette, and swing progress, so a puppet with
    /// a bigger or smaller sword never has to fight another boss's tuning. Nito's own NitoVFX.DrawSlash
    /// and Artorias (NPCs/Bosses/SuperHardMode/Artorias.cs) are the two current callers - copy either
    /// call shape for a third.
    /// </summary>
    internal static class VoidSlashVFX
    {
        const string EffectPath = "tsorcRevamp/Effects/NitoReaperTrail";
        const string NoiseRoot = "tsorcRevamp/Textures/Noise/";

        static Asset<Effect> effect;
        static Asset<Texture2D> sweepShape; // Turbulence_06 - macro sweep silhouette (DrawSweep only)
        static Asset<Texture2D> flow;       // Turbulence_07 - fine directional detail, both techniques
        static Asset<Texture2D> glint;      // SplotchyNoise - thrust sheath's detail texture

        static void LoadAssets()
        {
            effect ??= ModContent.Request<Effect>(EffectPath, AssetRequestMode.ImmediateLoad);
            sweepShape ??= ModContent.Request<Texture2D>(NoiseRoot + "Turbulence_06-512x512", AssetRequestMode.ImmediateLoad);
            flow ??= ModContent.Request<Texture2D>(NoiseRoot + "Turbulence_07-512x512", AssetRequestMode.ImmediateLoad);
            glint ??= ModContent.Request<Texture2D>(NoiseRoot + "SplotchyNoise", AssetRequestMode.ImmediateLoad);
        }

        /// <summary>The broad rotating sweep crescent used for overhead/horizontal swings.
        /// <paramref name="size"/> is the full quad in pixels - scale it to the wielder's blade reach
        /// (Nito's greatsword uses 255x323 at a ~170px forward reach). <paramref name="reverseSweep"/>
        /// mirrors the crescent for a swing that travels the opposite way around the body (e.g. an
        /// underhand rise instead of an overhead chop).</summary>
        internal static void DrawSweep(Vector2 center, float rotation, Vector2 size, float progress,
            float opacity, Color darkColor, Color midColor, Color coreColor, bool reverseSweep = false)
        {
            LoadAssets();
            Draw("NitoReaperSweep", sweepShape, flow, center, size, rotation, darkColor, midColor,
                coreColor, opacity, progress, 0f,
                reverseSweep ? SpriteEffects.FlipVertically : SpriteEffects.None);
        }

        /// <summary>The narrow straight-line thrust sheath used for stabs/lunges, where the broad
        /// sweep crescent would falsely promise danger to the sides instead of straight ahead.
        /// <paramref name="phase"/> varies the noise sample per swing so consecutive thrusts don't
        /// stamp the identical pattern.</summary>
        internal static void DrawThrust(Vector2 center, float rotation, Vector2 size, float progress,
            float opacity, Color darkColor, Color midColor, Color coreColor, float phase = 0f)
        {
            LoadAssets();
            Draw("NitoReaperTrail", flow, glint, center, size, rotation, darkColor, midColor,
                coreColor, opacity, progress, phase, SpriteEffects.None);
        }

        static void Draw(string techniqueName, Asset<Texture2D> primaryAsset, Asset<Texture2D> detailAsset,
            Vector2 worldCenter, Vector2 drawSize, float rotation, Color darkColor, Color midColor,
            Color coreColor, float opacity, float progress, float phase, SpriteEffects spriteEffects)
        {
            Texture2D primary = primaryAsset.Value;
            Texture2D detail = detailAsset.Value;
            Vector2 actualSize = primary.Size();
            Vector2 scale = drawSize / actualSize;

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearWrap,
                DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            Effect fx = effect.Value;
            GraphicsDevice graphicsDevice = Main.instance.GraphicsDevice;
            Texture previousTexture = graphicsDevice.Textures[1];
            SamplerState previousSampler = graphicsDevice.SamplerStates[1];

            try
            {
                graphicsDevice.Textures[1] = detail;
                graphicsDevice.SamplerStates[1] = SamplerState.LinearWrap;

                fx.CurrentTechnique = fx.Techniques[techniqueName];
                fx.Parameters["DarkColor"]?.SetValue(darkColor.ToVector3());
                fx.Parameters["MidColor"]?.SetValue(midColor.ToVector3());
                fx.Parameters["CoreColor"]?.SetValue(coreColor.ToVector3());
                fx.Parameters["Opacity"]?.SetValue(opacity);
                fx.Parameters["Time"]?.SetValue(Main.GlobalTimeWrappedHourly);
                fx.Parameters["Progress"]?.SetValue(progress);
                fx.Parameters["Active"]?.SetValue(1f);
                fx.Parameters["Direction"]?.SetValue(phase);
                fx.Parameters["DrawSize"]?.SetValue(actualSize);
                fx.Parameters["PrimaryTextureSize"]?.SetValue(primary.Size());
                Vector2 pixelBlocks = Vector2.Max(drawSize, Vector2.One) * 0.5f;
                fx.Parameters["PixelGrid"]?.SetValue(
                    new Vector4(pixelBlocks.X, pixelBlocks.Y, 1f / pixelBlocks.X, 1f / pixelBlocks.Y));
                fx.CurrentTechnique.Passes[0].Apply();

                Main.EntitySpriteDraw(primary, worldCenter - Main.screenPosition, null, Color.White,
                    rotation, actualSize * 0.5f, scale, spriteEffects, 0);
            }
            finally
            {
                graphicsDevice.Textures[1] = previousTexture;
                graphicsDevice.SamplerStates[1] = previousSampler;
                UsefulFunctions.RestartSpritebatch(ref Main.spriteBatch);
            }
        }
    }
}

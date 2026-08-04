using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp
{
    /// <summary>
    /// Draws a held weapon or prop with the wielder's own sprite punched out of it, so the shaft
    /// disappears where the body covers it and the prop reads as gripped rather than pasted on top.
    /// </summary>
    /// <remarks>
    /// Draw order alone cannot express this: the shaft has to pass in FRONT of the torso (it is held
    /// in the near hand) but BEHIND the fist, so neither drawing the prop before the body nor after
    /// it is correct. Slicing a band out of the prop in its own space is close but is a straight cut
    /// rather than the arm's outline. Masking against the wielder's real alpha is the general answer.
    /// </remarks>
    public static class HeldPropDraw
    {
        private static Effect OcclusionEffect;

        /// <summary>
        /// Draws <paramref name="propTexture"/> at the hand, hiding wherever <paramref name="maskTexture"/>'s
        /// current frame is opaque.
        /// </summary>
        /// <param name="screenPosition">Where the grip sits on screen (the SpriteBatch position).</param>
        /// <param name="gripOrigin">The prop pixel that sits at the hand (the SpriteBatch origin).</param>
        /// <param name="propScale">The prop's draw scale, exactly as passed to SpriteBatch.Draw.</param>
        /// <param name="maskFrame">The wielder's current animation frame within the mask sheet.</param>
        /// <param name="handFramePixel">The hand's position inside that frame, in frame pixels.</param>
        /// <param name="facingDirection">The wielder's sprite direction, for mirroring the lookup.</param>
        /// <param name="wielderScale">The wielder's draw scale (NPC.scale), so the prop offset can be
        /// converted into the frame-pixel space the mask is addressed in.</param>
        /// <param name="maskStrength">0 disables the mask entirely — useful for A/B in game.</param>
        public static void DrawOccluded(
            SpriteBatch spriteBatch,
            Texture2D propTexture,
            Vector2 screenPosition,
            Color drawColor,
            float rotation,
            Vector2 gripOrigin,
            float propScale,
            Texture2D maskTexture,
            Rectangle maskFrame,
            Vector2 handFramePixel,
            int facingDirection,
            float wielderScale = 1f,
            float maskStrength = 1f,
            float maskSoftness = 0.35f)
        {
            if (Main.dedServ || propTexture == null || propTexture.IsDisposed)
            {
                return;
            }

            // No mask available, or the caller turned it off: fall back to an ordinary draw rather
            // than silently skipping the prop.
            if (maskTexture == null || maskTexture.IsDisposed || maskStrength <= 0f || wielderScale <= 0f)
            {
                spriteBatch.Draw(propTexture, screenPosition, null, drawColor, rotation,
                    gripOrigin, propScale, SpriteEffects.None, 0f);
                return;
            }

            if (OcclusionEffect == null || OcclusionEffect.IsDisposed)
            {
                OcclusionEffect = ModContent.Request<Effect>(
                    "tsorcRevamp/Effects/HeldPropOcclusion",
                    AssetRequestMode.ImmediateLoad).Value;
            }

            GraphicsDevice graphicsDevice = Main.instance.GraphicsDevice;
            Texture previousTexture = graphicsDevice.Textures[1];
            SamplerState previousSampler = graphicsDevice.SamplerStates[1];

            UsefulFunctions.StartShaderSpritebatch(ref spriteBatch);
            try
            {
                graphicsDevice.Textures[1] = maskTexture;
                graphicsDevice.SamplerStates[1] = SamplerState.LinearClamp;

                OcclusionEffect.Parameters["PropSize"]?.SetValue(propTexture.Size());
                OcclusionEffect.Parameters["GripOrigin"]?.SetValue(gripOrigin);
                OcclusionEffect.Parameters["RotCos"]?.SetValue((float)Math.Cos(rotation));
                OcclusionEffect.Parameters["RotSin"]?.SetValue((float)Math.Sin(rotation));
                // Expressed in WIELDER FRAME PIXELS, not screen pixels: the body sprite is drawn at
                // wielderScale, so dividing here lets the shader skip a per-pixel divide.
                OcclusionEffect.Parameters["PropScale"]?.SetValue(propScale / wielderScale);
                // Matches the -spriteDirection factor the hand-position maths already uses, which is
                // what pairs with SpriteEffects.FlipHorizontally on the body.
                OcclusionEffect.Parameters["FlipSign"]?.SetValue(-facingDirection);
                OcclusionEffect.Parameters["HandFramePixel"]?.SetValue(handFramePixel);
                OcclusionEffect.Parameters["MaskFrameRect"]?.SetValue(
                    new Vector4(maskFrame.X, maskFrame.Y, maskFrame.Width, maskFrame.Height));
                OcclusionEffect.Parameters["MaskTextureSize"]?.SetValue(maskTexture.Size());
                OcclusionEffect.Parameters["MaskSoftness"]?.SetValue(maskSoftness);
                OcclusionEffect.Parameters["MaskStrength"]?.SetValue(MathHelper.Clamp(maskStrength, 0f, 1f));
                OcclusionEffect.CurrentTechnique.Passes[0].Apply();

                spriteBatch.Draw(propTexture, screenPosition, null, drawColor, rotation,
                    gripOrigin, propScale, SpriteEffects.None, 0f);
            }
            finally
            {
                graphicsDevice.Textures[1] = previousTexture;
                graphicsDevice.SamplerStates[1] = previousSampler;
                UsefulFunctions.RestartSpritebatch(ref spriteBatch);
            }
        }
    }
}

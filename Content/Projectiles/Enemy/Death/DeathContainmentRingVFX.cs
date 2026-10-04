using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.Content.Projectiles.Enemy.Death
{
    internal static class DeathContainmentRingVFX
    {
        const string BoundaryEffectPath = "tsorcRevamp/Effects/ArtoriasAbyssBoundary";
        const string DominionEffectPath = "tsorcRevamp/Effects/GreatRedKnightDominion";
        const string TurbulentNoisePath = "tsorcRevamp/Textures/Noise/TurbulentNoise";
        const string MarbleNoisePath = "tsorcRevamp/Textures/Noise/T_MarbleNoise_tiled";
        const string DominionTurbulencePath = "tsorcRevamp/Textures/Noise/Turbulence_05-512x512";
        const string VeinNoisePath = "tsorcRevamp/Textures/Noise/Vein_04-512x512";

        static Asset<Effect> boundaryEffect;
        static Asset<Effect> dominionEffect;
        static Asset<Texture2D> turbulentNoise;
        static Asset<Texture2D> marbleNoise;
        static Asset<Texture2D> dominionTurbulence;
        static Asset<Texture2D> veinNoise;

        static void LoadAssets()
        {
            boundaryEffect ??= ModContent.Request<Effect>(BoundaryEffectPath, AssetRequestMode.ImmediateLoad);
            dominionEffect ??= ModContent.Request<Effect>(DominionEffectPath, AssetRequestMode.ImmediateLoad);
            turbulentNoise ??= ModContent.Request<Texture2D>(TurbulentNoisePath, AssetRequestMode.ImmediateLoad);
            marbleNoise ??= ModContent.Request<Texture2D>(MarbleNoisePath, AssetRequestMode.ImmediateLoad);
            dominionTurbulence ??= ModContent.Request<Texture2D>(DominionTurbulencePath, AssetRequestMode.ImmediateLoad);
            veinNoise ??= ModContent.Request<Texture2D>(VeinNoisePath, AssetRequestMode.ImmediateLoad);
        }

        internal static void DrawBoundaryFog(Vector2 center, float radius, float opacity,
            Color darkColor, Color midColor, Color coreColor)
        {
            LoadAssets();

            float coverage = 2f * (radius + System.Math.Max(Main.screenWidth, Main.screenHeight));
            DrawDominionFog(
                center,
                Vector2.One * coverage,
                opacity,
                radius / coverage,
                darkColor,
                midColor,
                coreColor);
        }

        static void DrawDominionFog(Vector2 center, Vector2 size, float opacity,
            float radiusRatio, Color darkColor, Color midColor, Color coreColor)
        {
            if (Main.dedServ || dominionEffect == null || marbleNoise == null
                || dominionTurbulence == null || veinNoise == null
                || opacity <= 0f || size.X <= 0f || size.Y <= 0f)
            {
                return;
            }

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(
                SpriteSortMode.Immediate,
                BlendState.NonPremultiplied,
                SamplerState.LinearWrap,
                DepthStencilState.None,
                RasterizerState.CullNone,
                null,
                Main.GameViewMatrix.TransformationMatrix);

            GraphicsDevice graphicsDevice = Main.instance.GraphicsDevice;
            Texture previousTexture1 = graphicsDevice.Textures[1];
            Texture previousTexture2 = graphicsDevice.Textures[2];
            Texture previousTexture3 = graphicsDevice.Textures[3];
            SamplerState previousSampler1 = graphicsDevice.SamplerStates[1];
            SamplerState previousSampler2 = graphicsDevice.SamplerStates[2];
            SamplerState previousSampler3 = graphicsDevice.SamplerStates[3];

            try
            {
                graphicsDevice.Textures[1] = marbleNoise.Value;
                graphicsDevice.Textures[2] = dominionTurbulence.Value;
                graphicsDevice.Textures[3] = veinNoise.Value;
                graphicsDevice.SamplerStates[1] = SamplerState.LinearWrap;
                graphicsDevice.SamplerStates[2] = SamplerState.LinearWrap;
                graphicsDevice.SamplerStates[3] = SamplerState.LinearWrap;

                Effect effect = dominionEffect.Value;
                effect.CurrentTechnique = effect.Techniques["DominionFieldEven"]
                    ?? effect.Techniques["DominionField"];
                effect.Parameters["DarkColor"].SetValue(darkColor.ToVector3());
                effect.Parameters["MidColor"].SetValue(midColor.ToVector3());
                effect.Parameters["CoreColor"].SetValue(coreColor.ToVector3());
                effect.Parameters["Opacity"].SetValue(opacity);
                effect.Parameters["Time"].SetValue(Main.GlobalTimeWrappedHourly);
                effect.Parameters["Progress"].SetValue(1f);
                effect.Parameters["Intensity"].SetValue(0.88f);
                effect.Parameters["Active"].SetValue(0f);
                effect.Parameters["RadiusRatio"].SetValue(radiusRatio);
                effect.Parameters["Direction"].SetValue(0f);
                effect.Parameters["DrawSize"]?.SetValue(size);
                effect.CurrentTechnique.Passes[0].Apply();

                Texture2D pixel = TextureAssets.MagicPixel.Value;
                Main.EntitySpriteDraw(
                    pixel,
                    center - Main.screenPosition,
                    null,
                    Color.White,
                    0f,
                    pixel.Size() * 0.5f,
                    size / pixel.Size(),
                    SpriteEffects.None,
                    0f);
            }
            finally
            {
                graphicsDevice.Textures[1] = previousTexture1;
                graphicsDevice.Textures[2] = previousTexture2;
                graphicsDevice.Textures[3] = previousTexture3;
                graphicsDevice.SamplerStates[1] = previousSampler1;
                graphicsDevice.SamplerStates[2] = previousSampler2;
                graphicsDevice.SamplerStates[3] = previousSampler3;
                UsefulFunctions.RestartSpritebatch(ref Main.spriteBatch);
            }
        }

        internal static void DrawBoundaryEdge(Vector2 center, float radius, float halfWidth,
            float opacity, Color midColor, Color coreColor)
        {
            LoadAssets();

            float outerReach = System.Math.Max(radius + 800f, 1800f);
            Vector2 size = Vector2.One * outerReach * 2f;

            Draw(
                boundaryEffect,
                "ArtoriasAbyssBoundaryEdge",
                turbulentNoise,
                turbulentNoise,
                center,
                size,
                0f,
                new Color(9, 2, 20),
                midColor,
                coreColor,
                opacity * 0.82f,
                0f,
                radius + halfWidth,
                halfWidth,
                BlendState.Additive);
        }

        static void Draw(
            Asset<Effect> effectAsset,
            string techniqueName,
            Asset<Texture2D> primaryAsset,
            Asset<Texture2D> detailAsset,
            Vector2 worldCenter,
            Vector2 drawSize,
            float rotation,
            Color darkColor,
            Color midColor,
            Color coreColor,
            float opacity,
            float progress,
            float active,
            float direction,
            BlendState blendState)
        {
            Texture2D primary = primaryAsset.Value;
            Texture2D detail = detailAsset.Value;
            int sourceWidth = System.Math.Clamp((int)drawSize.X, 1, primary.Width);
            int sourceHeight = System.Math.Clamp((int)drawSize.Y, 1, primary.Height);
            Rectangle source = new(0, 0, sourceWidth, sourceHeight);
            Vector2 actualSize = source.Size();
            Vector2 scale = drawSize / actualSize;

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(
                SpriteSortMode.Immediate,
                blendState,
                SamplerState.LinearWrap,
                DepthStencilState.None,
                RasterizerState.CullNone,
                null,
                Main.GameViewMatrix.TransformationMatrix);

            GraphicsDevice graphicsDevice = Main.instance.GraphicsDevice;
            Texture previousTexture = graphicsDevice.Textures[1];
            SamplerState previousSampler = graphicsDevice.SamplerStates[1];
            Effect effect = effectAsset.Value;

            try
            {
                graphicsDevice.Textures[1] = detail;
                graphicsDevice.SamplerStates[1] = SamplerState.LinearWrap;
                effect.CurrentTechnique = effect.Techniques[techniqueName];
                effect.Parameters["DarkColor"]?.SetValue(darkColor.ToVector3());
                effect.Parameters["MidColor"]?.SetValue(midColor.ToVector3());
                effect.Parameters["CoreColor"]?.SetValue(coreColor.ToVector3());
                effect.Parameters["Opacity"]?.SetValue(opacity);
                effect.Parameters["Time"]?.SetValue(Main.GlobalTimeWrappedHourly);
                effect.Parameters["Progress"]?.SetValue(progress);
                effect.Parameters["Active"]?.SetValue(active);
                effect.Parameters["Direction"]?.SetValue(direction);
                effect.Parameters["DrawSize"]?.SetValue(actualSize);
                effect.Parameters["PrimaryTextureSize"]?.SetValue(primary.Size());
                effect.Parameters["WorldDrawSize"]?.SetValue(drawSize);
                effect.CurrentTechnique.Passes[0].Apply();

                Main.EntitySpriteDraw(
                    primary,
                    worldCenter - Main.screenPosition,
                    source,
                    Color.White,
                    rotation,
                    actualSize * 0.5f,
                    scale,
                    SpriteEffects.None,
                    0f);
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

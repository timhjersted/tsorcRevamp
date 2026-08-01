using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    internal static class ArtoriasVFX
    {
        const string EffectRoot = "tsorcRevamp/Effects/";
        const string NoiseRoot = "tsorcRevamp/Textures/Noise/";

        static Asset<Effect> boundaryEffect;
        static Asset<Effect> detonationEffect;
        static Asset<Effect> greatswordEffect;
        static Asset<Effect> eruptionEffect;
        static Asset<Effect> projectileEffect;
        static Asset<Effect> tendrilEffect;
        static Asset<Effect> mantleEffect;

        static Asset<Texture2D> smoothNoise;
        static Asset<Texture2D> brokenNoise;
        static Asset<Texture2D> circleGradient;
        static Asset<Texture2D> cracks;
        static Asset<Texture2D> windstreak;
        static Asset<Texture2D> smoke;
        static Asset<Texture2D> aura;
        static Asset<Texture2D> spiral;
        static Asset<Texture2D> flare;
        static Asset<Texture2D> abyssFog;

        static readonly Color VoidBlack = new(7, 5, 15);
        static readonly Color AbyssIndigo = new(53, 36, 111);
        static readonly Color AbyssViolet = new(112, 62, 164);
        static readonly Color KnightSilver = new(205, 224, 235);
        static readonly Color DangerMagenta = new(226, 64, 162);

        static void LoadAssets()
        {
            boundaryEffect ??= ModContent.Request<Effect>(EffectRoot + "ArtoriasAbyssBoundary", AssetRequestMode.ImmediateLoad);
            detonationEffect ??= ModContent.Request<Effect>(EffectRoot + "ArtoriasAbyssDetonation", AssetRequestMode.ImmediateLoad);
            greatswordEffect ??= ModContent.Request<Effect>(EffectRoot + "ArtoriasGreatswordWake", AssetRequestMode.ImmediateLoad);
            eruptionEffect ??= ModContent.Request<Effect>(EffectRoot + "ArtoriasAbyssEruption", AssetRequestMode.ImmediateLoad);
            projectileEffect ??= ModContent.Request<Effect>(EffectRoot + "ArtoriasAbyssProjectile", AssetRequestMode.ImmediateLoad);
            tendrilEffect ??= ModContent.Request<Effect>(EffectRoot + "ArtoriasAbyssTendril", AssetRequestMode.ImmediateLoad);
            mantleEffect ??= ModContent.Request<Effect>(EffectRoot + "ArtoriasAbyssMantle", AssetRequestMode.ImmediateLoad);

            smoothNoise ??= ModContent.Request<Texture2D>(NoiseRoot + "T_VFX_NoiseF1", AssetRequestMode.ImmediateLoad);
            brokenNoise ??= ModContent.Request<Texture2D>(NoiseRoot + "T_VFX_Noise41", AssetRequestMode.ImmediateLoad);
            circleGradient ??= ModContent.Request<Texture2D>(NoiseRoot + "T_Gradient_circle22", AssetRequestMode.ImmediateLoad);
            cracks ??= ModContent.Request<Texture2D>(NoiseRoot + "T_Cracks336", AssetRequestMode.ImmediateLoad);
            windstreak ??= ModContent.Request<Texture2D>(NoiseRoot + "T_Windstreak3", AssetRequestMode.ImmediateLoad);
            smoke ??= ModContent.Request<Texture2D>(NoiseRoot + "T_smoke_b7", AssetRequestMode.ImmediateLoad);
            aura ??= ModContent.Request<Texture2D>(NoiseRoot + "T_Aurax44", AssetRequestMode.ImmediateLoad);
            spiral ??= ModContent.Request<Texture2D>(NoiseRoot + "T_VFX_Spiral07", AssetRequestMode.ImmediateLoad);
            flare ??= ModContent.Request<Texture2D>(NoiseRoot + "T_VFX_Flare_666", AssetRequestMode.ImmediateLoad);
            abyssFog ??= ModContent.Request<Texture2D>("tsorcRevamp/Textures/AbyssFog", AssetRequestMode.ImmediateLoad);
        }

        internal static void DrawBoundary(Vector2 center, float radius, float halfWidth, float opacity, bool warning)
        {
            LoadAssets();
            float padding = halfWidth * 2.3f;
            Vector2 size = Vector2.One * (radius + padding) * 2f;
            Draw(boundaryEffect, "ArtoriasAbyssBoundary", circleGradient, brokenNoise,
                center, size, 0f, VoidBlack, AbyssViolet, KnightSilver, opacity,
                warning ? 1f : 0f, radius / (radius + padding), halfWidth / size.X, BlendState.Additive);
        }

        internal static void DrawDetonation(Vector2 center, float radius, float progress, float opacity, bool active)
        {
            LoadAssets();
            Draw(detonationEffect, "ArtoriasAbyssDetonation", circleGradient, brokenNoise,
                center, Vector2.One * radius * 2f, 0f, VoidBlack, AbyssViolet, KnightSilver,
                opacity, progress, 1f, active ? 1f : 0f, BlendState.Additive);
        }

        internal static void DrawGreatswordWake(Vector2 center, float rotation, Vector2 size,
            float age, float opacity, bool piercing)
        {
            LoadAssets();
            Draw(greatswordEffect, "ArtoriasGreatswordWake", windstreak, brokenNoise,
                center, size, rotation, VoidBlack, AbyssIndigo, KnightSilver, opacity,
                age, 1f, piercing ? 1f : 0f, BlendState.Additive);
        }

        internal static void DrawGroundRift(Vector2 center, Vector2 size, float progress, float opacity)
        {
            LoadAssets();
            Draw(eruptionEffect, "ArtoriasGroundRift", cracks, brokenNoise,
                center, size, 0f, VoidBlack, AbyssViolet, KnightSilver, opacity,
                progress, 1f, 1f, BlendState.Additive);
        }

        internal static void DrawEruption(Vector2 center, Vector2 size, float progress, float opacity)
        {
            LoadAssets();
            Draw(eruptionEffect, "ArtoriasAbyssEruption", smoke, brokenNoise,
                center, size, 0f, VoidBlack, AbyssViolet, KnightSilver, opacity,
                progress, 1f, 1f, BlendState.Additive);
        }

        internal static void DrawCrescent(Vector2 center, float rotation, Vector2 size, bool returning, float opacity)
        {
            LoadAssets();
            Color mid = returning ? DangerMagenta : AbyssViolet;
            Draw(projectileEffect, "ArtoriasCrescent", circleGradient, brokenNoise,
                center, size, rotation, VoidBlack, mid, KnightSilver, opacity,
                0f, returning ? 1f : 0f, returning ? -1f : 1f, BlendState.Additive);
        }

        internal static void DrawOrb(Vector2 center, Vector2 size, float rotation,
            float stateIntensity, float opacity)
        {
            LoadAssets();
            Draw(projectileEffect, "ArtoriasOrb", spiral, brokenNoise,
                center, size, rotation, VoidBlack, AbyssViolet, KnightSilver, opacity,
                0f, stateIntensity, 1f, BlendState.Additive);
        }

        internal static void DrawProjectileTrail(Vector2 center, float rotation, Vector2 size,
            float stateIntensity, float opacity)
        {
            LoadAssets();
            Draw(projectileEffect, "ArtoriasProjectileTrail", windstreak, smoothNoise,
                center, size, rotation, VoidBlack, AbyssViolet, KnightSilver, opacity,
                0f, stateIntensity, 1f, BlendState.Additive);
        }

        internal static void DrawTransitionFlash(Vector2 center, Vector2 size, float progress, float opacity)
        {
            LoadAssets();
            Draw(projectileEffect, "ArtoriasTurnFlash", flare, brokenNoise,
                center, size, 0f, VoidBlack, DangerMagenta, KnightSilver, opacity,
                progress, 1f, 1f, BlendState.Additive);
        }

        internal static void DrawTendril(Vector2 start, Vector2 end, float tension, float opacity, bool hostileTip)
        {
            LoadAssets();
            Vector2 delta = end - start;
            float length = delta.Length();
            if (length < 2f)
                return;

            float rotation = delta.ToRotation();
            Draw(tendrilEffect, "ArtoriasAbyssTendril", abyssFog, brokenNoise,
                Vector2.Lerp(start, end, 0.5f), new Vector2(length, 58f), rotation,
                VoidBlack, AbyssViolet, KnightSilver, opacity, tension,
                tension, 1f, BlendState.Additive);

            Draw(tendrilEffect, "ArtoriasTendrilTip", aura, brokenNoise,
                end, Vector2.One * (hostileTip ? 54f : 38f), 0f,
                VoidBlack, hostileTip ? DangerMagenta : AbyssViolet, KnightSilver,
                hostileTip ? 0.9f : 0.42f, tension, hostileTip ? 1f : 0f, 1f, BlendState.Additive);
        }

        internal static void DrawMantle(Vector2 center, Vector2 size, float opacity, float intensity, float flowDirection)
        {
            LoadAssets();
            Draw(mantleEffect, "ArtoriasMantleBody", abyssFog, brokenNoise,
                center, size, 0f, VoidBlack, AbyssIndigo, KnightSilver,
                opacity, 0f, intensity, flowDirection, BlendState.AlphaBlend);
            Draw(mantleEffect, "ArtoriasMantleEdge", aura, brokenNoise,
                center, size, 0f, VoidBlack, AbyssViolet, KnightSilver,
                opacity, 0f, intensity, flowDirection, BlendState.Additive);
        }

        static void Draw(Asset<Effect> effectAsset, string techniqueName,
            Asset<Texture2D> primaryAsset, Asset<Texture2D> detailAsset,
            Vector2 worldCenter, Vector2 drawSize, float rotation,
            Color darkColor, Color midColor, Color coreColor,
            float opacity, float progress, float active, float direction, BlendState blendState)
        {
            Texture2D primary = primaryAsset.Value;
            Texture2D detail = detailAsset.Value;
            int sourceWidth = System.Math.Clamp((int)drawSize.X, 1, primary.Width);
            int sourceHeight = System.Math.Clamp((int)drawSize.Y, 1, primary.Height);
            Rectangle source = new(0, 0, sourceWidth, sourceHeight);
            Vector2 actualSize = source.Size();
            Vector2 scale = drawSize / actualSize;

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, blendState, SamplerState.LinearWrap,
                DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

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
                effect.CurrentTechnique.Passes[0].Apply();

                Main.EntitySpriteDraw(primary, worldCenter - Main.screenPosition, source, Color.White,
                    rotation, actualSize * 0.5f, scale, SpriteEffects.None, 0f);
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

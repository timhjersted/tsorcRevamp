using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    internal static class NitoVFX
    {
        const string TextureRoot = "tsorcRevamp/Textures/Noise/";

        static Asset<Effect> deathNovaEffect;
        static Asset<Effect> graveEruptionEffect;
        static Asset<Effect> reaperTrailEffect;
        static Asset<Effect> miasmaVolumeEffect;
        static Asset<Effect> gravefallEffect;
        static Asset<Effect> soulMantleEffect;
        static Asset<Texture2D> smoothNoise;
        static Asset<Texture2D> brokenNoise;
        static Asset<Texture2D> cloudNoise;
        static Asset<Texture2D> circleTexture;
        static Asset<Texture2D> smokeTexture;
        static Asset<Texture2D> crackTexture;
        static Asset<Texture2D> trailTexture;
        static Asset<Texture2D> windstreakTexture;
        static Asset<Texture2D> auraTexture;
        static Asset<Texture2D> flareTexture;
        static Asset<Texture2D> graveHandTexture;

        static readonly Color DeathDark = new(30, 12, 44);
        static readonly Color DeathMid = new(115, 58, 154);
        static readonly Color BoneCore = new(226, 231, 210);
        static readonly Color MiasmaDark = new(18, 30, 17);
        static readonly Color MiasmaMid = new(82, 126, 64);
        static readonly Color MiasmaCore = new(180, 210, 118);

        static void LoadAssets()
        {
            deathNovaEffect ??= ModContent.Request<Effect>("tsorcRevamp/Effects/NitoDeathNova", AssetRequestMode.ImmediateLoad);
            graveEruptionEffect ??= ModContent.Request<Effect>("tsorcRevamp/Effects/NitoGraveEruption", AssetRequestMode.ImmediateLoad);
            reaperTrailEffect ??= ModContent.Request<Effect>("tsorcRevamp/Effects/NitoReaperTrail", AssetRequestMode.ImmediateLoad);
            miasmaVolumeEffect ??= ModContent.Request<Effect>("tsorcRevamp/Effects/NitoMiasmaVolume", AssetRequestMode.ImmediateLoad);
            gravefallEffect ??= ModContent.Request<Effect>("tsorcRevamp/Effects/NitoGravefall", AssetRequestMode.ImmediateLoad);
            soulMantleEffect ??= ModContent.Request<Effect>("tsorcRevamp/Effects/NitoSoulMantle", AssetRequestMode.ImmediateLoad);
            smoothNoise ??= ModContent.Request<Texture2D>(TextureRoot + "T_VFX_NoiseF1", AssetRequestMode.ImmediateLoad);
            brokenNoise ??= ModContent.Request<Texture2D>(TextureRoot + "T_VFX_Noise41", AssetRequestMode.ImmediateLoad);
            cloudNoise ??= ModContent.Request<Texture2D>(TextureRoot + "T_CloudNoise_Tiled", AssetRequestMode.ImmediateLoad);
            circleTexture ??= ModContent.Request<Texture2D>(TextureRoot + "T_VFX_CircleFit1", AssetRequestMode.ImmediateLoad);
            smokeTexture ??= ModContent.Request<Texture2D>(TextureRoot + "T_smoke_b7", AssetRequestMode.ImmediateLoad);
            crackTexture ??= ModContent.Request<Texture2D>(TextureRoot + "T_Cracks336", AssetRequestMode.ImmediateLoad);
            trailTexture ??= ModContent.Request<Texture2D>(TextureRoot + "T_trail12", AssetRequestMode.ImmediateLoad);
            windstreakTexture ??= ModContent.Request<Texture2D>(TextureRoot + "T_Windstreak3", AssetRequestMode.ImmediateLoad);
            auraTexture ??= ModContent.Request<Texture2D>(TextureRoot + "T_Aurax44", AssetRequestMode.ImmediateLoad);
            flareTexture ??= ModContent.Request<Texture2D>(TextureRoot + "T_VFX_Flare_666", AssetRequestMode.ImmediateLoad);
            graveHandTexture ??= ModContent.Request<Texture2D>(TextureRoot + "T_NitoGraveHand", AssetRequestMode.ImmediateLoad);
        }

        internal static void DrawDeathRing(Vector2 center, float radius, float halfThickness, float opacity)
        {
            LoadAssets();
            float padding = halfThickness * 2.4f;
            Vector2 size = Vector2.One * (radius + padding) * 2f;
            Draw(deathNovaEffect, "NitoDeathRing", circleTexture, brokenNoise, center, size, 0f,
                DeathDark, DeathMid, BoneCore, opacity, 0f,
                radius / (radius + padding), halfThickness / size.X);
        }

        internal static void DrawDeathFlash(Vector2 center, Vector2 size, float progress, float opacity)
        {
            LoadAssets();
            Texture2D texture = flareTexture.Value;
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.LinearClamp,
                DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            Vector2 scale = size / texture.Size();
            Color color = Color.Lerp(new Color(104, 48, 148), BoneCore, 0.42f) * opacity * (1f - progress);
            Main.EntitySpriteDraw(texture, center - Main.screenPosition, null, color, 0f,
                texture.Size() * 0.5f, scale, SpriteEffects.None, 0);
            UsefulFunctions.RestartSpritebatch(ref Main.spriteBatch);
        }

        internal static void DrawMiasma(Vector2 center, Vector2 size, float rotation, float progress, float opacity)
        {
            LoadAssets();
            Draw(miasmaVolumeEffect, "NitoMiasmaVolume", smokeTexture, cloudNoise, center, size, rotation,
                MiasmaDark, MiasmaMid, MiasmaCore, opacity, progress, 1f, 1f);
        }

        internal static void DrawGroundRift(Vector2 center, Vector2 size, float progress, float opacity)
        {
            LoadAssets();
            Draw(graveEruptionEffect, "NitoGroundRift", crackTexture, brokenNoise, center, size, 0f,
                DeathDark, DeathMid, BoneCore, opacity, progress, 1f, 1f);
        }

        internal static void DrawSlash(Vector2 center, float rotation, Vector2 size, float progress, float opacity)
        {
            LoadAssets();
            Draw(reaperTrailEffect, "NitoReaperTrail", trailTexture, brokenNoise, center, size, rotation,
                DeathDark, DeathMid, BoneCore, opacity, progress, 1f, 1f);
        }

        internal static void DrawAura(Vector2 center, Vector2 size, float opacity, bool phaseTwo,
            float pulseProgress, float flowDirection)
        {
            LoadAssets();
            Color mid = phaseTwo ? new Color(133, 55, 168) : new Color(104, 99, 132);
            Draw(soulMantleEffect, "NitoSoulMantle", auraTexture, brokenNoise, center, size, 0f,
                DeathDark, mid, BoneCore, opacity, pulseProgress, phaseTwo ? 1f : 0f, flowDirection);
        }

        internal static void DrawGraveEruption(Vector2 center, Vector2 size, float progress, float opacity)
        {
            LoadAssets();
            Draw(graveEruptionEffect, "NitoGravePlume", smokeTexture, brokenNoise, center, size, 0f,
                DeathDark, DeathMid, BoneCore, opacity, progress, 1f, 1f);
        }

        internal static void DrawGraveHand(Vector2 center, Vector2 size, float progress, float opacity)
        {
            LoadAssets();
            Draw(graveEruptionEffect, "NitoGraveHand", graveHandTexture, brokenNoise, center, size, 0f,
                DeathDark, DeathMid, BoneCore, opacity, progress, 1f, 1f);
        }

        internal static void DrawRainPortal(Vector2 center, Vector2 size, float progress, float opacity)
        {
            LoadAssets();
            Draw(gravefallEffect, "NitoGraveSky", circleTexture, brokenNoise, center, size, 0f,
                DeathDark, new Color(132, 126, 158), BoneCore, opacity, progress, 1f, -1f);
        }

        internal static void DrawSoulTrail(Vector2 center, float rotation, Vector2 size, float progress, float opacity)
        {
            LoadAssets();
            Draw(gravefallEffect, "NitoGravefallTrail", windstreakTexture, smoothNoise, center, size, rotation,
                DeathDark, new Color(122, 112, 146), BoneCore, opacity, progress, 1f, 1f);
        }

        static void Draw(Asset<Effect> effectAsset, string techniqueName,
            Asset<Texture2D> primaryAsset, Asset<Texture2D> detailAsset,
            Vector2 worldCenter, Vector2 drawSize, float rotation, Color darkColor, Color midColor,
            Color coreColor, float opacity, float progress, float active, float direction)
        {
            LoadAssets();
            Texture2D primary = primaryAsset.Value;
            Texture2D detail = detailAsset.Value;
            int sourceWidth = System.Math.Clamp((int)drawSize.X, 1, primary.Width);
            int sourceHeight = System.Math.Clamp((int)drawSize.Y, 1, primary.Height);
            Rectangle source = new(0, 0, sourceWidth, sourceHeight);
            Vector2 actualSize = source.Size();
            Vector2 scale = new(drawSize.X / actualSize.X, drawSize.Y / actualSize.Y);
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearWrap,
                DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            Effect effect = effectAsset.Value;
            GraphicsDevice graphicsDevice = Main.instance.GraphicsDevice;
            Texture previousTexture = graphicsDevice.Textures[1];
            SamplerState previousSampler = graphicsDevice.SamplerStates[1];

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
                    rotation, actualSize * 0.5f, scale, SpriteEffects.None, 0);
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

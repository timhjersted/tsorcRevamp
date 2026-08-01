using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy.VesselOfSouls
{
    internal static class VesselVFX
    {
        const string EffectRoot = "tsorcRevamp/Effects/";
        const string NoiseRoot = "tsorcRevamp/Textures/Noise/";

        static Asset<Effect> mawEffect;
        static Asset<Effect> novaEffect;
        static Asset<Effect> trailEffect;
        static Asset<Effect> gazeEffect;
        static Asset<Effect> rammingEffect;
        static Asset<Effect> ruptureEffect;
        static Asset<Effect> voidEffect;

        static Asset<Texture2D> spiral;
        static Asset<Texture2D> aura;
        static Asset<Texture2D> noise;
        static Asset<Texture2D> circle;
        static Asset<Texture2D> windstreak;
        static Asset<Texture2D> flare;
        static Asset<Texture2D> rupture;
        static Asset<Texture2D> smoke;
        static Asset<Texture2D> roundSmoke;
        static ulong trailBudgetTick;
        static int shaderTrailsDrawn;

        static readonly Color HollowBlack = new(6, 2, 10);
        static readonly Color WineRed = new(94, 18, 57);
        static readonly Color SoulMagenta = new(170, 38, 132);
        static readonly Color SoulPale = new(232, 211, 255);
        static readonly Color CommitmentPink = new(255, 66, 174);

        static void LoadAssets()
        {
            mawEffect ??= ModContent.Request<Effect>(EffectRoot + "VesselSoulMaw", AssetRequestMode.ImmediateLoad);
            novaEffect ??= ModContent.Request<Effect>(EffectRoot + "VesselSoulNova", AssetRequestMode.ImmediateLoad);
            trailEffect ??= ModContent.Request<Effect>(EffectRoot + "VesselSoulTrail", AssetRequestMode.ImmediateLoad);
            gazeEffect ??= ModContent.Request<Effect>(EffectRoot + "VesselWatcherGaze", AssetRequestMode.ImmediateLoad);
            rammingEffect ??= ModContent.Request<Effect>(EffectRoot + "VesselRammingWake", AssetRequestMode.ImmediateLoad);
            ruptureEffect ??= ModContent.Request<Effect>(EffectRoot + "VesselSoulRupture", AssetRequestMode.ImmediateLoad);
            voidEffect ??= ModContent.Request<Effect>(EffectRoot + "VesselVoidSpace", AssetRequestMode.ImmediateLoad);

            spiral ??= ModContent.Request<Texture2D>(NoiseRoot + "T_VFX_Spiral07", AssetRequestMode.ImmediateLoad);
            aura ??= ModContent.Request<Texture2D>(NoiseRoot + "T_Aurax44", AssetRequestMode.ImmediateLoad);
            noise ??= ModContent.Request<Texture2D>(NoiseRoot + "T_VFX_NoiseF1", AssetRequestMode.ImmediateLoad);
            circle ??= ModContent.Request<Texture2D>(NoiseRoot + "T_VFX_CircleFit1", AssetRequestMode.ImmediateLoad);
            windstreak ??= ModContent.Request<Texture2D>(NoiseRoot + "T_Windstreak3", AssetRequestMode.ImmediateLoad);
            flare ??= ModContent.Request<Texture2D>(NoiseRoot + "T_VFX_Flare_666", AssetRequestMode.ImmediateLoad);
            rupture ??= ModContent.Request<Texture2D>(NoiseRoot + "T_VFX_Ex1d", AssetRequestMode.ImmediateLoad);
            smoke ??= ModContent.Request<Texture2D>(NoiseRoot + "T_smoke_b7", AssetRequestMode.ImmediateLoad);
            roundSmoke ??= ModContent.Request<Texture2D>(NoiseRoot + "T_VFX_RoundSmoke71", AssetRequestMode.ImmediateLoad);
        }

        internal static void DrawMaw(Vector2 center, float radius, float progress, bool committed, float innerDeadzone)
        {
            LoadAssets();
            float normalizedDeadzone = radius > 0f ? MathHelper.Clamp(innerDeadzone / radius, 0.02f, 0.42f) : 0.1f;
            Draw(mawEffect, "VesselSoulMaw", spiral, aura, center, Vector2.One * radius * 2f, 0f,
                HollowBlack, WineRed, SoulMagenta, committed ? 0.56f : 0.38f,
                progress, committed ? 1f : 0f, normalizedDeadzone, BlendState.AlphaBlend);
            Draw(mawEffect, "VesselSoulMaw", spiral, aura, center, Vector2.One * radius * 2f, 0f,
                HollowBlack, SoulMagenta, committed ? CommitmentPink : SoulPale,
                committed ? 0.58f : 0.40f, progress, committed ? 1f : 0f, normalizedDeadzone, BlendState.Additive);
        }

        internal static void DrawNova(Vector2 center, float radius, float halfWidth, float opacity)
        {
            LoadAssets();
            float padding = halfWidth * 2.3f;
            Vector2 size = Vector2.One * (radius + padding) * 2f;
            Draw(novaEffect, "VesselSoulNova", circle, noise, center, size, 0f,
                HollowBlack, SoulMagenta, SoulPale, opacity, 0f,
                radius / (radius + padding), halfWidth / size.X, BlendState.Additive);
        }

        internal static void DrawSoulTrail(Projectile projectile, float opacity)
        {
            LoadAssets();
            Vector2 end = projectile.Center;
            Vector2 start = end - projectile.velocity.SafeNormalize(Vector2.UnitX) * 62f;
            for (int i = projectile.oldPos.Length - 1; i >= 0; i--)
            {
                if (projectile.oldPos[i] != Vector2.Zero)
                {
                    start = projectile.oldPos[i] + projectile.Size * 0.5f;
                    break;
                }
            }
            Vector2 delta = end - start;
            if (delta.LengthSquared() < 4f)
                delta = projectile.velocity.SafeNormalize(Vector2.UnitX) * 24f;

            // The seven-second death fountain can leave well over a hundred skulls alive. Preserve
            // the silhouette for all of them while capping expensive SpriteBatch state switches.
            if (trailBudgetTick != Main.GameUpdateCount)
            {
                trailBudgetTick = Main.GameUpdateCount;
                shaderTrailsDrawn = 0;
            }
            if (shaderTrailsDrawn++ >= 28)
            {
                Texture2D texture = windstreak.Value;
                Rectangle source = texture.Bounds;
                Vector2 size = new(delta.Length() + 28f, 24f);
                Main.EntitySpriteDraw(texture, Vector2.Lerp(start, end, 0.5f) - Main.screenPosition,
                    source, WineRed * 0.24f, delta.ToRotation(), source.Size() * 0.5f,
                    size / source.Size(), SpriteEffects.None, 0f);
                return;
            }
            Draw(trailEffect, "VesselSoulTrail", windstreak, noise,
                Vector2.Lerp(start, end, 0.5f), new Vector2(delta.Length() + 28f, 28f), delta.ToRotation(),
                HollowBlack, WineRed, SoulPale, opacity, 0f,
                projectile.ai[0] > 0f ? 1f : 0f, 1f, BlendState.Additive);
        }

        internal static void DrawWatcherGaze(Vector2 center, Vector2 target, float chargeProgress, bool detonating)
        {
            LoadAssets();
            float active = detonating ? 1f : chargeProgress;
            Color mid = detonating ? WineRed : SoulMagenta;
            Color core = detonating ? CommitmentPink : SoulPale;
            Draw(gazeEffect, "VesselWatcherIris", flare, noise, center,
                Vector2.One * (detonating ? 104f : 72f), 0f,
                HollowBlack, mid, core, detonating ? 0.90f : 0.70f,
                chargeProgress, active, 1f, BlendState.Additive);

            if (!detonating && chargeProgress > 0.05f)
            {
                Vector2 delta = target - center;
                float length = MathHelper.Min(delta.Length(), 620f);
                Vector2 direction = delta.SafeNormalize(Vector2.UnitY);
                Draw(gazeEffect, "VesselWatcherLine", windstreak, noise,
                    center + direction * length * 0.5f, new Vector2(length, 10f), direction.ToRotation(),
                    HollowBlack, WineRed, SoulPale, 0.18f + chargeProgress * 0.30f,
                    chargeProgress, chargeProgress, 1f, BlendState.Additive);
            }
        }

        internal static void DrawRammingWake(Vector2 center, Vector2 velocity, Vector2 hitboxSize, bool plunge)
        {
            LoadAssets();
            Vector2 direction = velocity.SafeNormalize(Vector2.UnitY);
            float trailLength = plunge ? 310f : 440f;
            Draw(rammingEffect, "VesselRammingWake", windstreak, noise,
                center - direction * trailLength * 0.46f, new Vector2(trailLength, plunge ? 230f : 270f),
                direction.ToRotation(), HollowBlack, WineRed, SoulPale,
                plunge ? 0.62f : 0.72f, 0f, plunge ? 0.4f : 1f, plunge ? -1f : 1f, BlendState.Additive);

            // Axis-aligned because NPC contact collision is the unrotated 200x300 AABB.
            Draw(rammingEffect, "VesselRammingCore", TextureAssets.MagicPixel, noise,
                center, hitboxSize, 0f, HollowBlack, SoulMagenta, CommitmentPink,
                0.82f, 0f, 1f, plunge ? -1f : 1f, BlendState.Additive);
        }

        internal static void DrawRupture(Vector2 center, float radius, float progress, bool exploding, float opacity)
        {
            LoadAssets();
            float shaderProgress = exploding ? progress : 1f - progress;
            Draw(ruptureEffect, "VesselSoulRupture", rupture, roundSmoke,
                center, Vector2.One * radius * 2f, 0f, HollowBlack, SoulMagenta, SoulPale,
                opacity, shaderProgress, exploding ? 1f : 0f, exploding ? 1f : 0f, BlendState.Additive);
        }

        internal static void DrawVoidSpace(SpriteBatch spriteBatch, float opacity)
        {
            LoadAssets();
            Effect effect = voidEffect.Value;
            GraphicsDevice graphicsDevice = Main.instance.GraphicsDevice;
            Texture previousTexture = graphicsDevice.Textures[1];
            SamplerState previousSampler = graphicsDevice.SamplerStates[1];
            spriteBatch.End();
            try
            {
                spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearWrap,
                    DepthStencilState.None, RasterizerState.CullNone, null, Main.Transform);
                graphicsDevice.Textures[1] = noise.Value;
                graphicsDevice.SamplerStates[1] = SamplerState.LinearWrap;
                effect.CurrentTechnique = effect.Techniques["VesselVoidSpace"];
                SetParameters(effect, HollowBlack, WineRed, SoulPale, opacity, 0f, 1f, 1f,
                    new Vector2(Main.screenWidth, Main.screenHeight), smoke.Value.Size());
                effect.CurrentTechnique.Passes[0].Apply();
                spriteBatch.Draw(smoke.Value, new Rectangle(0, 0, Main.screenWidth, Main.screenHeight), Color.White);
            }
            finally
            {
                graphicsDevice.Textures[1] = previousTexture;
                graphicsDevice.SamplerStates[1] = previousSampler;
                spriteBatch.End();
                spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
                    DepthStencilState.None, Main.Rasterizer, null, Main.Transform);
            }
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
                SetParameters(effect, darkColor, midColor, coreColor, opacity, progress, active, direction,
                    actualSize, primary.Size());
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

        static void SetParameters(Effect effect, Color dark, Color mid, Color core,
            float opacity, float progress, float active, float direction,
            Vector2 drawSize, Vector2 primarySize)
        {
            effect.Parameters["DarkColor"]?.SetValue(dark.ToVector3());
            effect.Parameters["MidColor"]?.SetValue(mid.ToVector3());
            effect.Parameters["CoreColor"]?.SetValue(core.ToVector3());
            effect.Parameters["Opacity"]?.SetValue(opacity);
            effect.Parameters["Time"]?.SetValue(Main.GlobalTimeWrappedHourly);
            effect.Parameters["Progress"]?.SetValue(progress);
            effect.Parameters["Active"]?.SetValue(active);
            effect.Parameters["Direction"]?.SetValue(direction);
            effect.Parameters["DrawSize"]?.SetValue(drawSize);
            effect.Parameters["PrimaryTextureSize"]?.SetValue(primarySize);
        }
    }
}

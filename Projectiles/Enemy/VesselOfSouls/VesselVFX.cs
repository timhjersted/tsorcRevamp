using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
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

        // Fresh set, deliberately disjoint from the Black Knight plague kit (Vein_07, Turbulence_05,
        // T_CloudNoise_Tiled, T_PerlinNoise_Tiled, T_Noise_Wf4) so the two bosses do not share a look.
        static Asset<Texture2D> spiral;      // T_VFX_Spiral07 — an actual spiral galaxy; kept because
                                             // nothing else in the pack fits a throat vortex as well
        static Asset<Texture2D> flow;        // Turbulence_06 — directional flow streaks
        static Asset<Texture2D> gullet;      // T_Noise_HU85k — molten flowing tissue
        static Asset<Texture2D> swirl;       // SwirlyNoise — large soft swirls, nebula underlayer
        static Asset<Texture2D> wisp;        // T_Aurax44 — soft vertical soul ribbons
        static Asset<Texture2D> filament;    // T_VFX_Noise_44xainv — fine thread detail
        static Asset<Texture2D> burst;       // T_texr41 — radial spike burst
        static Asset<Texture2D> dissipate;   // T_VFX_exp_dissapear — swirling break-up
        static Asset<Texture2D> compound;    // T_Noise_Wfk14 — dark cellular web, reads as a compound eye
        static Asset<Texture2D> crackle;     // Vein_10 — fine bright soul motes
        static ulong trailBudgetTick;
        static int shaderTrailsDrawn;

        static readonly Color HollowBlack = new(6, 2, 10);
        static readonly Color WineRed = new(94, 18, 57);
        static readonly Color SoulMagenta = new(170, 38, 132);
        // Was (232, 211, 255) — effectively white, which is what turned the rupture and the eye rings
        // into flat white discs once it was driven additively.
        static readonly Color SoulPale = new(214, 196, 232);
        static readonly Color SoulBone = new(198, 176, 186);
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
            flow ??= ModContent.Request<Texture2D>(NoiseRoot + "Turbulence_06-512x512", AssetRequestMode.ImmediateLoad);
            gullet ??= ModContent.Request<Texture2D>(NoiseRoot + "T_Noise_HU85k", AssetRequestMode.ImmediateLoad);
            swirl ??= ModContent.Request<Texture2D>(NoiseRoot + "SwirlyNoise", AssetRequestMode.ImmediateLoad);
            wisp ??= ModContent.Request<Texture2D>(NoiseRoot + "T_Aurax44", AssetRequestMode.ImmediateLoad);
            filament ??= ModContent.Request<Texture2D>(NoiseRoot + "T_VFX_Noise_44xainv", AssetRequestMode.ImmediateLoad);
            burst ??= ModContent.Request<Texture2D>(NoiseRoot + "T_texr41", AssetRequestMode.ImmediateLoad);
            dissipate ??= ModContent.Request<Texture2D>(NoiseRoot + "T_VFX_exp_dissapear", AssetRequestMode.ImmediateLoad);
            compound ??= ModContent.Request<Texture2D>(NoiseRoot + "T_Noise_Wfk14", AssetRequestMode.ImmediateLoad);
            crackle ??= ModContent.Request<Texture2D>(NoiseRoot + "Vein_10-512x512", AssetRequestMode.ImmediateLoad);
        }

        internal static void DrawMaw(Vector2 center, float radius, float progress, bool committed, float innerDeadzone)
        {
            LoadAssets();
            float normalizedDeadzone = radius > 0f ? MathHelper.Clamp(innerDeadzone / radius, 0.02f, 0.42f) : 0.1f;
            // Two passes as before: an alpha-blended body that darkens the world (the vessel is
            // pulling light in) and an additive pass for the hot streamlines on top. Opacity raised
            // on both — this is the fight's read-at-a-glance tell and it was nearly invisible.
            Draw(mawEffect, "VesselSoulMaw", spiral, flow, center, Vector2.One * radius * 2f, 0f,
                HollowBlack, WineRed, SoulMagenta, committed ? 0.78f : 0.58f,
                progress, committed ? 1f : 0f, normalizedDeadzone, BlendState.AlphaBlend);
            Draw(mawEffect, "VesselSoulMaw", spiral, flow, center, Vector2.One * radius * 2f, 0f,
                HollowBlack, SoulMagenta, committed ? CommitmentPink : SoulPale,
                committed ? 0.80f : 0.56f, progress, committed ? 1f : 0f, normalizedDeadzone, BlendState.Additive);
        }

        internal static void DrawNova(Vector2 center, float radius, float halfWidth, float opacity)
        {
            LoadAssets();
            float padding = halfWidth * 2.3f;
            Vector2 size = Vector2.One * (radius + padding) * 2f;
            Draw(novaEffect, "VesselSoulNova", burst, dissipate, center, size, 0f,
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
                // Degraded path for extreme skull counts. Soft-edged aurora ribbon rather than the old
                // T_Windstreak3 blob, and much fainter, so the budget cut-off is not a visible seam
                // between "good trail" and "grey lozenge".
                Texture2D texture = wisp.Value;
                Rectangle source = texture.Bounds;
                Vector2 size = new(delta.Length() + 28f, 24f);
                Main.EntitySpriteDraw(texture, Vector2.Lerp(start, end, 0.5f) - Main.screenPosition,
                    source, WineRed * 0.16f, delta.ToRotation(), source.Size() * 0.5f,
                    size / source.Size(), SpriteEffects.None, 0f);
                return;
            }
            // Direction carries a per-skull noise phase. Every instance otherwise samples the identical
            // pattern in local UV space, and 100+ skulls then read as one stamp repeated rather than as
            // a swarm. projectile.identity is stable and network-synced, so it needs no new state.
            float instancePhase = (projectile.identity % 32) / 32f;
            Draw(trailEffect, "VesselSoulTrail", wisp, filament,
                Vector2.Lerp(start, end, 0.5f), new Vector2(delta.Length() + 28f, 30f), delta.ToRotation(),
                HollowBlack, WineRed, SoulBone, opacity, 0f,
                projectile.ai[0] > 0f ? 1f : 0f, instancePhase, BlendState.Additive);
        }

        internal static void DrawWatcherGaze(Vector2 center, Vector2 target, float chargeProgress, bool detonating)
        {
            LoadAssets();
            float active = detonating ? 1f : chargeProgress;
            Color mid = detonating ? WineRed : SoulMagenta;
            Color core = detonating ? CommitmentPink : SoulPale;
            // Ring shader 20% smaller (0.8x scale)
            Draw(gazeEffect, "VesselWatcherIris", compound, crackle, center,
                Vector2.One * (detonating ? 150f : 96f) * 0.8f, 0f,
                HollowBlack, mid, core, detonating ? 0.90f : 0.70f,
                chargeProgress, active, 1f, BlendState.Additive);

            if (!detonating && chargeProgress > 0.05f)
            {
                Vector2 delta = target - center;
                // Line 80% shorter (0.2x length) and 20% more transparent (0.8x opacity)
                float length = MathHelper.Min(delta.Length(), 620f) * 0.2f;
                Vector2 direction = delta.SafeNormalize(Vector2.UnitY);
                Draw(gazeEffect, "VesselWatcherLine", filament, crackle,
                    center + direction * length * 0.5f, new Vector2(length, 16f), direction.ToRotation(),
                    HollowBlack, WineRed, SoulPale, (0.22f + chargeProgress * 0.38f) * 0.8f,
                    chargeProgress, chargeProgress, 1f, BlendState.Additive);
            }
        }

        internal static void DrawRammingWake(Vector2 center, Vector2 velocity, Vector2 hitboxSize, bool plunge)
        {
            LoadAssets();
            Vector2 direction = velocity.SafeNormalize(Vector2.UnitY);
            float trailLength = plunge ? 310f : 440f;
            Draw(rammingEffect, "VesselRammingWake", flow, filament,
                center - direction * trailLength * 0.46f, new Vector2(trailLength, plunge ? 230f : 270f),
                direction.ToRotation(), HollowBlack, WineRed, SoulMagenta,
                plunge ? 0.62f : 0.72f, 0f, plunge ? 0.4f : 1f, plunge ? -1f : 1f, BlendState.Additive);

            // Free-form compression front, rotated to the charge axis and pushed slightly ahead of the
            // body. It is NO LONGER tied to the 200x300 contact AABB — the old version drew that box
            // literally, as a hard pink wireframe. Deliberate trade approved by the owner: this is
            // atmosphere now and no longer advertises exactly where contact damage begins. The hitbox
            // itself is unchanged.
            Draw(rammingEffect, "VesselRammingCore", flow, gullet,
                center + direction * 40f,
                new Vector2(hitboxSize.Y * 1.40f, hitboxSize.Y * 1.02f), direction.ToRotation(),
                HollowBlack, SoulMagenta, CommitmentPink,
                0.82f, 0f, 1f, plunge ? -1f : 1f, BlendState.Additive);
        }

        internal static void DrawRupture(Vector2 center, float radius, float progress, bool exploding, float opacity)
        {
            LoadAssets();
            float shaderProgress = exploding ? progress : 1f - progress;
            Draw(ruptureEffect, "VesselSoulRupture", burst, dissipate,
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
                graphicsDevice.Textures[1] = swirl.Value;
                graphicsDevice.SamplerStates[1] = SamplerState.LinearWrap;
                effect.CurrentTechnique = effect.Techniques["VesselVoidSpace"];
                // DrawSize carries the screen dimensions so the shader can aspect-correct its
                // vignette — on a wide screen the corners must actually be the corners.
                SetParameters(effect, HollowBlack, WineRed, CommitmentPink, opacity, 0f, 1f, 1f,
                    new Vector2(Main.screenWidth, Main.screenHeight), gullet.Value.Size());
                effect.CurrentTechnique.Passes[0].Apply();
                spriteBatch.Draw(gullet.Value, new Rectangle(0, 0, Main.screenWidth, Main.screenHeight), Color.White);
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
            // No source rectangle. The old code cropped the primary to a drawSize-sized rectangle at
            // the texture's TOP-LEFT corner whenever the draw was smaller than the texture — so, for
            // example, the 72px Watcher iris took a 72x72 corner out of a 512px centred flare, which
            // is almost entirely black. Sampling the whole texture also makes the shaders' LocalUV()
            // helper an identity, so it was deleted from all of them (~4 slots each).
            Vector2 actualSize = primary.Size();
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
                Main.EntitySpriteDraw(primary, worldCenter - Main.screenPosition, null, Color.White,
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

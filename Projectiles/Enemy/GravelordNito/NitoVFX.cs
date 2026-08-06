using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ID;
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

        // Fresh kit, deliberately disjoint from the Black Knight set (Vein_07, Turbulence_05,
        // T_CloudNoise_Tiled, T_PerlinNoise_Tiled, T_Noise_Wf4) and the Vessel set (T_VFX_Spiral07,
        // Turbulence_06, T_Noise_HU85k, SwirlyNoise, T_Aurax44, T_VFX_Noise_44xainv, T_texr41,
        // T_VFX_exp_dissapear, T_Noise_Wfk14, Vein_10) so the three bosses do not share a look.
        //
        // Every pick below was checked as a grayscale R*A/255 contact sheet AND as a 2x2 tile —
        // all of them wrap seamlessly, which matters because every one of these shaders scrolls its
        // samples. The old kit's shape sources (T_Windstreak3 = a vertical teardrop blob, T_trail12 =
        // a 4-point star flare, T_VFX_CircleFit1 = a solid white disc) are gone entirely; silhouettes
        // are procedural now.
        static Asset<Texture2D> flow;        // Turbulence_07 — fibrous directional turbulence; the workhorse
        static Asset<Texture2D> billow;      // T_NKQ443 — soft round cell pockets, smoke/fire billows
        static Asset<Texture2D> macroFog;    // T_Noise_6Yu1 — big smooth worm-maze shapes, macro cloud bodies
        static Asset<Texture2D> grit;        // Vein_04 — dark grain with bright specks, ash and ember motes
        static Asset<Texture2D> bands;       // T_Wave66 — soft horizontal bands, streak modulation
        static Asset<Texture2D> glint;       // SplotchyNoise — dark with bright cell edges, bone glints
        static Asset<Texture2D> veins;       // T_Noise56ko — fibrous with bright veins, miasma capillaries
        static Asset<Texture2D> flareTexture;// T_VFX_Flare_666 — the (non-shader) nova birth flash
        static Asset<Texture2D> graveHandTexture; // bespoke hand silhouette; a correct, intentional shape source

        static readonly Color DeathDark = new(30, 12, 44);   // deep grave violet
        static readonly Color DeathMid = new(115, 58, 154);  // death magic purple
        // Was (226, 231, 210) — effectively white, and driven additively, which is exactly what turned
        // every highlight into a flat white disc. Real bone: desaturated warm grey.
        static readonly Color BoneCore = new(198, 192, 168);
        static readonly Color MiasmaDark = new(18, 30, 17);  // poison fog, its own green family
        static readonly Color MiasmaMid = new(82, 126, 64);
        static readonly Color MiasmaCore = new(180, 210, 118);
        // NEW third family: the grave-pyre. Nito's blade sprite is crimson and the ground he tears open
        // should burn, so the eruption / rift / hand / sky-portal rim run red-and-black fire rather than
        // the purple death magic. Purple stays the *magic*, green stays the *poison*, red is the *pyre*.
        static readonly Color PyreBlack = new(10, 3, 6);
        static readonly Color PyreRed = new(152, 22, 28);
        static readonly Color PyreEmber = new(240, 122, 52);

        static void LoadAssets()
        {
            deathNovaEffect ??= ModContent.Request<Effect>("tsorcRevamp/Effects/NitoDeathNova", AssetRequestMode.ImmediateLoad);
            graveEruptionEffect ??= ModContent.Request<Effect>("tsorcRevamp/Effects/NitoGraveEruption", AssetRequestMode.ImmediateLoad);
            reaperTrailEffect ??= ModContent.Request<Effect>("tsorcRevamp/Effects/NitoReaperTrail", AssetRequestMode.ImmediateLoad);
            miasmaVolumeEffect ??= ModContent.Request<Effect>("tsorcRevamp/Effects/NitoMiasmaVolume", AssetRequestMode.ImmediateLoad);
            gravefallEffect ??= ModContent.Request<Effect>("tsorcRevamp/Effects/NitoGravefall", AssetRequestMode.ImmediateLoad);
            soulMantleEffect ??= ModContent.Request<Effect>("tsorcRevamp/Effects/NitoSoulMantle", AssetRequestMode.ImmediateLoad);
            flow ??= ModContent.Request<Texture2D>(TextureRoot + "Turbulence_07-512x512", AssetRequestMode.ImmediateLoad);
            billow ??= ModContent.Request<Texture2D>(TextureRoot + "T_NKQ443", AssetRequestMode.ImmediateLoad);
            macroFog ??= ModContent.Request<Texture2D>(TextureRoot + "T_Noise_6Yu1", AssetRequestMode.ImmediateLoad);
            grit ??= ModContent.Request<Texture2D>(TextureRoot + "Vein_04-512x512", AssetRequestMode.ImmediateLoad);
            bands ??= ModContent.Request<Texture2D>(TextureRoot + "T_Wave66", AssetRequestMode.ImmediateLoad);
            glint ??= ModContent.Request<Texture2D>(TextureRoot + "SplotchyNoise", AssetRequestMode.ImmediateLoad);
            veins ??= ModContent.Request<Texture2D>(TextureRoot + "T_Noise56ko", AssetRequestMode.ImmediateLoad);
            flareTexture ??= ModContent.Request<Texture2D>(TextureRoot + "T_VFX_Flare_666", AssetRequestMode.ImmediateLoad);
            graveHandTexture ??= ModContent.Request<Texture2D>(TextureRoot + "T_NitoGraveHand", AssetRequestMode.ImmediateLoad);
        }

        internal static void DrawDeathRing(Vector2 center, float radius, float halfThickness, float opacity)
        {
            LoadAssets();
            // 4.5x, was 2.4x. At 2.4x the ring's own outer lip landed within ~3px of the quad edge,
            // leaving the shader physically nowhere to feather an outer halo — every softening term
            // got sliced flat by the boundary. The hitbox is unaffected (Colliding() works off the
            // raw radius and half thickness, not off this quad).
            float padding = halfThickness * 4.5f;
            Vector2 size = Vector2.One * (radius + padding) * 2f;
            // Active = the ring radius in UV units (x2), Direction = the half thickness in UV units.
            Draw(deathNovaEffect, "NitoDeathRing", flow, grit, center, size, 0f,
                DeathDark, DeathMid, BoneCore, opacity, 0f,
                radius / (radius + padding), halfThickness / size.X, BlendState.AlphaBlend);
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

        /// <param name="phase">Per-cloud phase so a bank of clouds does not read as one stamp repeated.</param>
        internal static void DrawMiasma(Vector2 center, Vector2 size, float rotation, float progress, float opacity,
            float phase = 0f)
        {
            LoadAssets();
            // AlphaBlend (premultiplied): this is SMOKE. Additive poison fog over a bright sky can never
            // read as dark or occluding, which is why it used to vanish into the background.
            Draw(miasmaVolumeEffect, "NitoMiasmaVolume", macroFog, veins, center, size, rotation,
                MiasmaDark, MiasmaMid, MiasmaCore, opacity, progress, 1f, phase, BlendState.AlphaBlend);
        }

        internal static void DrawGroundRift(Vector2 center, Vector2 size, float progress, float opacity)
        {
            LoadAssets();
            // Side-view fissure, not a top-down oval: the quad straddles the ground line and the effect
            // is allowed to clip into the tiles below it, so uneven terrain never reveals a floating disc.
            Draw(graveEruptionEffect, "NitoGroundRift", macroFog, flow, center, size, 0f,
                PyreBlack, PyreRed, PyreEmber, opacity, progress, 1f, 1f, BlendState.AlphaBlend);
        }

        /// <param name="phase">Per-swing phase so consecutive swings in a combo do not sample identically.</param>
        internal static void DrawSlash(Vector2 center, float rotation, Vector2 size, float progress, float opacity,
            float phase = 0f)
        {
            LoadAssets();
            Draw(reaperTrailEffect, "NitoReaperTrail", flow, glint, center, size, rotation,
                DeathDark, DeathMid, BoneCore, opacity, progress, 1f, phase, BlendState.AlphaBlend);
        }

        internal static void DrawAura(Vector2 center, Vector2 size, float opacity, bool phaseTwo,
            float pulseProgress, float flowDirection)
        {
            LoadAssets();
            Color mid = phaseTwo ? new Color(133, 55, 168) : new Color(96, 88, 118);
            // AlphaBlend: the mantle of corpses should sit him in a pool of gloom, not halo him in light.
            Draw(soulMantleEffect, "NitoSoulMantle", billow, glint, center, size, 0f,
                DeathDark, mid, BoneCore, opacity, pulseProgress, phaseTwo ? 1f : 0f, flowDirection,
                BlendState.AlphaBlend);
        }

        internal static void DrawGraveEruption(Vector2 center, Vector2 size, float progress, float opacity)
        {
            LoadAssets();
            Draw(graveEruptionEffect, "NitoGravePlume", billow, flow, center, size, 0f,
                PyreBlack, PyreRed, PyreEmber, opacity, progress, 1f, 1f, BlendState.AlphaBlend);
        }

        internal static void DrawGraveHand(Vector2 center, Vector2 size, float progress, float opacity)
        {
            LoadAssets();
            // Bone stays bone (BoneCore); the fire wreathing it is the pyre family, so the two colour
            // stories read separately instead of the hand glowing purple like everything else.
            Draw(graveEruptionEffect, "NitoGraveHand", graveHandTexture, flow, center, size, 0f,
                PyreBlack, PyreRed, BoneCore, opacity, progress, 1f, 1f, BlendState.AlphaBlend);
        }

        internal static void DrawRainPortal(Vector2 center, Vector2 size, float progress, float opacity)
        {
            LoadAssets();
            // The oval comes from the QUAD's own aspect now (the shader works in a plain UV circle), so
            // the portal fills its draw instead of being a thin lens floating in a mostly-empty box.
            // Dark = the hole, Mid = the galaxy arms (death-magic purple), Core = ember tips; the deep
            // red of the flame crown is a constant inside the shader.
            Draw(gravefallEffect, "NitoGraveSky", flow, macroFog, center, size, 0f,
                new Color(6, 3, 12), DeathMid, PyreEmber, opacity, progress, 1f, -1f, BlendState.AlphaBlend);
        }

        /// <param name="phase">Per-projectile phase; without it every shard in a fan samples the same pattern.</param>
        internal static void DrawSoulTrail(Vector2 center, float rotation, Vector2 size, float progress, float opacity,
            float phase = 0f)
        {
            LoadAssets();
            Draw(gravefallEffect, "NitoGravefallTrail", bands, flow, center, size, rotation,
                // Mid was (122, 112, 146) — a pale desaturated grey that, lerped toward BoneCore for
                // the glints, produced exactly the white blobs in the screenshots. Deep violet.
                DeathDark, new Color(98, 74, 136), BoneCore, opacity, progress, 1f, phase,
                BlendState.AlphaBlend);
        }

        ///<summary>The red-and-black grave-fire dust burst that punctuates every pyre-family effect —
        ///blades tearing out of / sinking back into the ground, hands erupting, fire guttering out.
        ///Deliberately plain Dust (not a shader) so it layers on top of whatever shader quad spawned
        ///it and survives the spritebatch juggling those do.</summary>
        internal static void PyreBurst(Vector2 position, int count, float speed, float scale = 1f,
            float spreadX = 10f, float spreadY = 10f)
        {
            if (Main.netMode == NetmodeID.Server) return;
            for (int i = 0; i < count; i++)
            {
                Vector2 offset = Main.rand.NextVector2Circular(spreadX, spreadY);
                Vector2 velocity = Main.rand.NextVector2Circular(speed, speed) - new Vector2(0f, speed * 0.35f);
                // Two thirds red flame, one third black smoke — the PyreRed/PyreBlack pairing the
                // shaders use, expressed in dust.
                bool ember = !Main.rand.NextBool(3);
                Dust d = Dust.NewDustPerfect(position + offset,
                    ember ? DustID.RedTorch : DustID.Smoke, velocity, ember ? 80 : 140,
                    ember ? default : new Color(24, 10, 12),
                    Main.rand.NextFloat(0.85f, 1.35f) * scale);
                d.noGravity = true;
            }
        }

        static void Draw(Asset<Effect> effectAsset, string techniqueName,
            Asset<Texture2D> primaryAsset, Asset<Texture2D> detailAsset,
            Vector2 worldCenter, Vector2 drawSize, float rotation, Color darkColor, Color midColor,
            Color coreColor, float opacity, float progress, float active, float direction,
            BlendState blendState)
        {
            LoadAssets();
            Texture2D primary = primaryAsset.Value;
            Texture2D detail = detailAsset.Value;
            // No source rectangle. The old code cropped the primary to a drawSize-sized rectangle at the
            // texture's TOP-LEFT corner whenever the draw was smaller than the texture — so e.g. a 74px
            // miasma puff took a 74x74 corner out of a 512px noise sheet. Sampling the whole texture also
            // makes the shaders' LocalUV() helper an identity, so it was deleted from all of them.
            Vector2 actualSize = primary.Size();
            Vector2 scale = drawSize / actualSize;
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, blendState, SamplerState.LinearWrap,
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

                Main.EntitySpriteDraw(primary, worldCenter - Main.screenPosition, null, Color.White,
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

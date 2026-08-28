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
        static Asset<Effect> swordSwipeEffect;
        static Asset<Effect> detonationEffect;
        static Asset<Effect> impactEffect;
        static Asset<Effect> impactCoreEffect;
        static Asset<Effect> eruptionEffect;
        static Asset<Effect> projectileEffect;
        static Asset<Effect> boomerangEffect;
        static Asset<Effect> boomerangOrbitEffect;
        static Asset<Effect> boomerangTrailEffect;
        static Asset<Effect> tendrilEffect;
        static Asset<Effect> tendrilHandEffect;
        static Asset<Effect> mantleEffect;
        static Asset<Effect> purpleFireEffect;
        static Asset<Effect> floorFireEffect;

        static Asset<Texture2D> smoothNoise;
        static Asset<Texture2D> brokenNoise;
        static Asset<Texture2D> circleGradient;
        static Asset<Texture2D> cracks;
        static Asset<Texture2D> windstreak;
        static Asset<Texture2D> smoke;
        static Asset<Texture2D> roundSmoke;
        static Asset<Texture2D> aura;
        static Asset<Texture2D> spiral;
        static Asset<Texture2D> flare;
        static Asset<Texture2D> abyssFog;
        static Asset<Texture2D> turbulentNoise;
        static Asset<Texture2D> marbleNoise;
        static Asset<Texture2D> slashFibers;
        static Asset<Texture2D> particleFire1;
        static Asset<Texture2D> particleFire2;
        static Asset<Texture2D> particleFlame1;
        static Asset<Texture2D> particleFlame3;
        static Asset<Texture2D> particleTrace3;
        static Asset<Texture2D> particleTrace7;

        static readonly Color VoidBlack = new(7, 5, 15);
        static readonly Color AbyssIndigo = new(53, 36, 111);
        static readonly Color AbyssViolet = new(112, 62, 164);
        static readonly Color KnightSilver = new(205, 224, 235);
        static readonly Color DangerMagenta = new(226, 64, 162);
        static readonly float[] ImpaleTendrilAngles =
        {
            -2.82f, -1.88f, -0.92f, 0.18f, 1.25f
        };

        static void LoadAssets()
        {
            boundaryEffect ??= ModContent.Request<Effect>(EffectRoot + "ArtoriasAbyssBoundary", AssetRequestMode.ImmediateLoad);
            swordSwipeEffect ??= ModContent.Request<Effect>(EffectRoot + "ArtoriasSwordSwipe", AssetRequestMode.ImmediateLoad);
            detonationEffect ??= ModContent.Request<Effect>(EffectRoot + "ArtoriasAbyssDetonation", AssetRequestMode.ImmediateLoad);
            impactEffect ??= ModContent.Request<Effect>(EffectRoot + "ArtoriasAbyssImpact", AssetRequestMode.ImmediateLoad);
            impactCoreEffect ??= ModContent.Request<Effect>(EffectRoot + "ArtoriasAbyssImpactCore", AssetRequestMode.ImmediateLoad);
            eruptionEffect ??= ModContent.Request<Effect>(EffectRoot + "ArtoriasAbyssEruption", AssetRequestMode.ImmediateLoad);
            projectileEffect ??= ModContent.Request<Effect>(EffectRoot + "ArtoriasAbyssProjectile", AssetRequestMode.ImmediateLoad);
            boomerangEffect ??= ModContent.Request<Effect>(EffectRoot + "ArtoriasBoomerang", AssetRequestMode.ImmediateLoad);
            boomerangOrbitEffect ??= ModContent.Request<Effect>(EffectRoot + "ArtoriasBoomerangOrbit", AssetRequestMode.ImmediateLoad);
            boomerangTrailEffect ??= ModContent.Request<Effect>(EffectRoot + "ArtoriasBoomerangTrail", AssetRequestMode.ImmediateLoad);
            tendrilEffect ??= ModContent.Request<Effect>(EffectRoot + "ArtoriasAbyssTendril", AssetRequestMode.ImmediateLoad);
            tendrilHandEffect ??= ModContent.Request<Effect>(EffectRoot + "ArtoriasAbyssHand", AssetRequestMode.ImmediateLoad);
            mantleEffect ??= ModContent.Request<Effect>(EffectRoot + "ArtoriasAbyssMantle", AssetRequestMode.ImmediateLoad);
            purpleFireEffect ??= ModContent.Request<Effect>(EffectRoot + "ArtoriasPurpleFire", AssetRequestMode.ImmediateLoad);
            floorFireEffect ??= ModContent.Request<Effect>(EffectRoot + "ArtoriasFloorFire", AssetRequestMode.ImmediateLoad);

            smoothNoise ??= ModContent.Request<Texture2D>(NoiseRoot + "T_VFX_NoiseF1", AssetRequestMode.ImmediateLoad);
            brokenNoise ??= ModContent.Request<Texture2D>(NoiseRoot + "T_VFX_Noise41", AssetRequestMode.ImmediateLoad);
            circleGradient ??= ModContent.Request<Texture2D>(NoiseRoot + "T_Gradient_circle22", AssetRequestMode.ImmediateLoad);
            cracks ??= ModContent.Request<Texture2D>(NoiseRoot + "T_Cracks336", AssetRequestMode.ImmediateLoad);
            windstreak ??= ModContent.Request<Texture2D>(NoiseRoot + "T_Windstreak3", AssetRequestMode.ImmediateLoad);
            smoke ??= ModContent.Request<Texture2D>(NoiseRoot + "T_smoke_b7", AssetRequestMode.ImmediateLoad);
            roundSmoke ??= ModContent.Request<Texture2D>(NoiseRoot + "T_VFX_RoundSmoke71", AssetRequestMode.ImmediateLoad);
            aura ??= ModContent.Request<Texture2D>(NoiseRoot + "T_Aurax44", AssetRequestMode.ImmediateLoad);
            spiral ??= ModContent.Request<Texture2D>(NoiseRoot + "T_VFX_Spiral07", AssetRequestMode.ImmediateLoad);
            flare ??= ModContent.Request<Texture2D>(NoiseRoot + "T_VFX_Flare_666", AssetRequestMode.ImmediateLoad);
            abyssFog ??= ModContent.Request<Texture2D>("tsorcRevamp/Textures/AbyssFog", AssetRequestMode.ImmediateLoad);
            turbulentNoise ??= ModContent.Request<Texture2D>(NoiseRoot + "TurbulentNoise", AssetRequestMode.ImmediateLoad);
            marbleNoise ??= ModContent.Request<Texture2D>(NoiseRoot + "T_MarbleNoise_tiled", AssetRequestMode.ImmediateLoad);
            slashFibers ??= ModContent.Request<Texture2D>(NoiseRoot + "Vein_04-512x512", AssetRequestMode.ImmediateLoad);
            particleFire1 ??= ModContent.Request<Texture2D>("tsorcRevamp/Textures/Particles/fire_01_a", AssetRequestMode.ImmediateLoad);
            particleFire2 ??= ModContent.Request<Texture2D>("tsorcRevamp/Textures/Particles/fire_02_a", AssetRequestMode.ImmediateLoad);
            particleFlame1 ??= ModContent.Request<Texture2D>("tsorcRevamp/Textures/Particles/flame_01_a", AssetRequestMode.ImmediateLoad);
            particleFlame3 ??= ModContent.Request<Texture2D>("tsorcRevamp/Textures/Particles/flame_03_a", AssetRequestMode.ImmediateLoad);
            particleTrace3 ??= ModContent.Request<Texture2D>("tsorcRevamp/Textures/Particles/trace_03_a", AssetRequestMode.ImmediateLoad);
            particleTrace7 ??= ModContent.Request<Texture2D>("tsorcRevamp/Textures/Particles/trace_07_a", AssetRequestMode.ImmediateLoad);
        }

        internal static void DrawBoundary(Vector2 center, float radius, float halfWidth, float opacity, bool warning)
        {
            LoadAssets();
            // Extend well beyond the 800px arena so the forbidden side reads as a continuous
            // abyss field rather than a detached tire-shaped strip. The bright inner seam still
            // follows the exact lethal ring center and 40px half-width.
            float outerReach = System.Math.Max(radius + 1600f, 2400f);
            Vector2 size = Vector2.One * outerReach * 2f;
            if (!warning)
            {
                Draw(boundaryEffect, "ArtoriasAbyssBoundaryShroud", turbulentNoise, turbulentNoise,
                    center, size, 0f, new Color(1, 0, 5), new Color(42, 10, 70),
                    new Color(128, 40, 164), opacity,
                    0f, radius, halfWidth, BlendState.AlphaBlend);
            }

            Draw(boundaryEffect, "ArtoriasAbyssBoundaryInner", turbulentNoise, turbulentNoise,
                center, size, 0f, new Color(7, 1, 18),
                warning ? new Color(190, 34, 142) : new Color(108, 24, 174),
                warning ? new Color(244, 82, 184) : new Color(210, 72, 214),
                warning ? opacity * 0.52f : opacity * 0.64f,
                warning ? 1f : 0f, radius, halfWidth, BlendState.Additive);

            Draw(boundaryEffect, "ArtoriasAbyssBoundaryEdge", turbulentNoise, turbulentNoise,
                center, size, 0f, new Color(9, 2, 20), warning ? DangerMagenta : new Color(142, 34, 190),
                warning ? new Color(255, 94, 190) : new Color(226, 106, 238), warning ? opacity * 0.72f : opacity * 0.82f,
                warning ? 1f : 0f, radius, halfWidth, BlendState.Additive);
        }

        internal static void DrawDetonation(Vector2 center, float radius, float progress, float opacity, bool active)
        {
            LoadAssets();
            Draw(detonationEffect, "ArtoriasAbyssDetonation", circleGradient, brokenNoise,
                center, Vector2.One * radius * 2f, 0f, VoidBlack, AbyssViolet, KnightSilver,
                opacity, progress, 1f, active ? 1f : 0f, BlendState.Additive);
        }

        internal static void DrawImpactBlast(Vector2 center, float radius, float progress, float opacity)
        {
            LoadAssets();
            float bloom = MathHelper.SmoothStep(0f, 1f,
                MathHelper.Clamp(progress / 0.30f, 0f, 1f));
            float pulse = 0.96f + 0.04f * (float)System.Math.Sin(
                Main.GlobalTimeWrappedHourly * 11f);
            Vector2 exactSize = Vector2.One * radius * 2f;

            // The subdued full-radius layer is present from the first hostile frame. Brighter
            // inner layers expand through it, so the move reads as an explosion without drawing
            // a geometric ring around its circular collision volume.
            Draw(impactEffect, "ArtoriasAbyssImpactBody", roundSmoke, brokenNoise,
                center, exactSize * pulse, 0f, VoidBlack, new Color(48, 20, 88), KnightSilver,
                opacity * 0.52f, progress, 1f, 1f, BlendState.AlphaBlend);
            Draw(impactEffect, "ArtoriasAbyssImpactBody", smoke, brokenNoise,
                center - new Vector2(0f, 3f),
                exactSize * MathHelper.Lerp(0.68f, 1.04f, bloom), -0.16f,
                VoidBlack, AbyssViolet, KnightSilver,
                opacity * 0.68f, progress, 1f, 1f, BlendState.AlphaBlend);
            Draw(impactCoreEffect, "ArtoriasAbyssImpactCore", roundSmoke, brokenNoise,
                center, exactSize * MathHelper.Lerp(0.38f, 0.82f, bloom), 0.10f,
                VoidBlack, AbyssViolet, DangerMagenta,
                opacity * 0.72f, progress, 1f, 1f, BlendState.Additive);
            Draw(impactCoreEffect, "ArtoriasAbyssImpactCore", flare, brokenNoise,
                center, exactSize * MathHelper.Lerp(0.24f, 0.70f, bloom), -0.08f,
                VoidBlack, DangerMagenta, KnightSilver,
                opacity * MathHelper.Lerp(1f, 0.56f, progress),
                progress, 1f, 1f, BlendState.Additive);
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

        internal static void DrawSwordSwipe(Vector2 center, float rotation, Vector2 size,
            float progress, float opacity)
        {
            LoadAssets();
            Draw(swordSwipeEffect, "ArtoriasSwordSwipe", marbleNoise, slashFibers,
                center, size, rotation, new Color(8, 2, 20), new Color(102, 32, 176),
                new Color(232, 68, 198), opacity, progress, 1f, 1f,
                BlendState.AlphaBlend, fullTexture: true);
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

        internal static void DrawTravelingPurpleFire(Vector2 center, Vector2 size,
            float direction, float progress, float opacity)
        {
            LoadAssets();
            float time = Main.GlobalTimeWrappedHourly;
            float sway = (float)System.Math.Sin(time * 5.8f + center.X * 0.018f) * 0.10f;
            Color shadow = new(10, 2, 22);
            Color violet = new(112, 28, 190);
            Color magenta = new(224, 44, 190);
            Color hot = new(232, 196, 255);

            Draw(purpleFireEffect, "ArtoriasPurpleFireBody", particleFire2, turbulentNoise,
                center - new Vector2(direction * 5f, 4f), size * new Vector2(1.22f, 1.10f),
                sway * direction, shadow, violet, magenta, opacity * 0.76f,
                progress, 1f, direction, BlendState.AlphaBlend, fullTexture: true);
            Draw(purpleFireEffect, "ArtoriasPurpleFireBody", particleFire1, turbulentNoise,
                center + new Vector2(direction * 3f, 1f), size,
                -sway * 0.72f, shadow, magenta, hot, opacity * 0.62f,
                progress, 1f, -direction, BlendState.AlphaBlend, fullTexture: true);
            Draw(purpleFireEffect, "ArtoriasPurpleFireCore", particleFlame3, turbulentNoise,
                center - new Vector2(direction * 2f, 2f), size * new Vector2(0.68f, 0.92f),
                sway * 0.55f, shadow, magenta, hot, opacity * 0.86f,
                progress, 1f, direction, BlendState.Additive, fullTexture: true);
            Draw(purpleFireEffect, "ArtoriasPurpleFireCore", particleFlame1, turbulentNoise,
                center + new Vector2(direction * 4f, 5f), size * new Vector2(0.48f, 0.70f),
                -sway * 0.35f, shadow, new Color(174, 58, 230), hot, opacity * 0.58f,
                progress, 1f, -direction, BlendState.Additive, fullTexture: true);
        }

        internal static void DrawHomingFlameWisp(Vector2 center, Vector2 direction,
            float intensity, float opacity, float width, float length)
        {
            LoadAssets();
            direction = direction.SafeNormalize(Vector2.UnitX);
            float rotation = direction.ToRotation() - MathHelper.PiOver2;
            Draw(purpleFireEffect, "ArtoriasPurpleFireBody", particleTrace7, turbulentNoise,
                center, new Vector2(width, length), rotation,
                new Color(7, 1, 18), new Color(96, 24, 168), new Color(210, 52, 204),
                opacity * 0.78f, intensity, intensity, 1f,
                BlendState.AlphaBlend, fullTexture: true);
            Draw(purpleFireEffect, "ArtoriasPurpleFireCore", particleFlame1, turbulentNoise,
                center + direction * System.Math.Min(8f, length * 0.08f),
                new Vector2(width * 0.52f, length * 0.72f), rotation,
                new Color(7, 1, 18), new Color(180, 42, 226), new Color(236, 220, 255),
                opacity, intensity, intensity, -1f,
                BlendState.Additive, fullTexture: true);
        }

        internal static void DrawFloorFlame(Texture2D texture, Rectangle frame,
            Vector2 center, Vector2 size, float direction, float progress,
            float opacity, float seed)
        {
            LoadAssets();
            DrawBoomerang(floorFireEffect, "ArtoriasFloorFire", texture, frame,
                turbulentNoise.Value, center, size, 0f,
                new Color(11, 2, 24), new Color(132, 26, 206),
                new Color(244, 116, 226), opacity, progress, 1f,
                direction, seed, SamplerState.LinearClamp,
                direction < 0f ? SpriteEffects.FlipHorizontally : SpriteEffects.None);
        }

        internal static void DrawBoomerangCore(Texture2D texture, Rectangle frame,
            Vector2 center, float rotation, Vector2 size, bool returning,
            float curveDirection, float opacity, bool aura)
        {
            LoadAssets();
            Color mid = returning ? new Color(232, 54, 186) : new Color(152, 72, 242);
            DrawBoomerang(boomerangEffect, "ArtoriasBoomerangCore", texture, frame, brokenNoise.Value,
                center, size, rotation, new Color(30, 6, 58), mid, new Color(242, 234, 255),
                opacity, 0f, returning ? 1f : 0f, curveDirection, aura ? 0f : 1f,
                aura ? SamplerState.LinearClamp : SamplerState.PointClamp);
        }

        internal static void DrawBoomerangOrbit(Vector2 center, Vector2 size, float rotation,
            bool returning, float curveDirection, float opacity)
        {
            LoadAssets();
            Color mid = returning ? new Color(232, 44, 174) : new Color(138, 54, 232);
            DrawBoomerang(boomerangOrbitEffect, "ArtoriasBoomerangOrbit", spiral.Value, spiral.Value.Bounds,
                brokenNoise.Value, center, size, rotation, new Color(4, 1, 12), mid,
                new Color(210, 196, 255), opacity, 0f, returning ? 1f : 0f,
                curveDirection, 0f, SamplerState.LinearWrap);
        }

        internal static void DrawBoomerangRibbon(Vector2 center, float rotation, Vector2 size,
            bool returning, float curveDirection, float opacity)
        {
            LoadAssets();
            Color mid = returning ? new Color(222, 42, 180) : new Color(126, 50, 218);
            DrawBoomerang(boomerangTrailEffect, "ArtoriasBoomerangRibbon", windstreak.Value, windstreak.Value.Bounds,
                brokenNoise.Value, center, size, rotation, new Color(3, 1, 10), mid,
                new Color(196, 180, 255), opacity, 0f, returning ? 1f : 0f,
                curveDirection, 0f, SamplerState.LinearWrap);
        }

        internal static void DrawBoomerangPulse(Vector2 center, Vector2 size, float progress,
            float opacity)
        {
            LoadAssets();
            // The turn cue is an expanding magenta vortex rather than another generic white flare.
            DrawBoomerang(boomerangOrbitEffect, "ArtoriasBoomerangOrbit", spiral.Value, spiral.Value.Bounds,
                brokenNoise.Value, center, size, 0f, new Color(4, 1, 12),
                new Color(220, 38, 166), new Color(238, 226, 255), opacity * (1f - progress),
                progress, 1f, 1f, 0f, SamplerState.LinearClamp);
        }

        internal static void DrawBoomerangCharge(Vector2 bladeTip, Vector2 bladeDirection,
            float progress, float opacity)
        {
            LoadAssets();
            bladeDirection = bladeDirection.SafeNormalize(Vector2.UnitY);
            float rotation = bladeDirection.ToRotation();
            float pulse = 0.92f + 0.08f * (float)System.Math.Sin(
                Main.GlobalTimeWrappedHourly * 8.0f + progress * 5.0f);
            float gathered = MathHelper.SmoothStep(0f, 1f, progress);
            float turn = progress * 2.2f;

            // Counter-rotating torn wisps collect around the blade tip. Neither orbit contains
            // an opaque backing quad, so the telegraph remains attached to the weapon silhouette.
            DrawBoomerangOrbit(bladeTip,
                Vector2.One * MathHelper.Lerp(48f, 86f, gathered) * pulse,
                rotation + turn, returning: false, curveDirection: 1f,
                opacity: opacity * MathHelper.Lerp(0.48f, 0.96f, gathered));
            DrawBoomerangOrbit(bladeTip - bladeDirection * 6f,
                Vector2.One * MathHelper.Lerp(34f, 58f, gathered),
                rotation - turn * 0.72f, returning: true, curveDirection: -1f,
                opacity: opacity * MathHelper.Lerp(0.30f, 0.68f, gathered));
            DrawBoomerangRibbon(bladeTip - bladeDirection * 24f,
                rotation + MathHelper.PiOver2,
                new Vector2(MathHelper.Lerp(28f, 42f, gathered),
                    MathHelper.Lerp(62f, 104f, gathered)),
                returning: false, curveDirection: 1f,
                opacity: opacity * MathHelper.Lerp(0.40f, 0.88f, gathered));
        }

        internal static void DrawTendril(Vector2 start, Vector2 end, float tension, float opacity, bool hostileTip)
        {
            LoadAssets();
            Vector2 delta = end - start;
            float length = delta.Length();
            if (length < 2f)
                return;

            float rotation = delta.ToRotation();
            Draw(tendrilEffect, "ArtoriasTendrilShadow", abyssFog, brokenNoise,
                Vector2.Lerp(start, end, 0.5f), new Vector2(length, 76f), rotation,
                VoidBlack, AbyssIndigo, KnightSilver, opacity * 0.86f, tension,
                tension, 1f, BlendState.AlphaBlend);
            Draw(tendrilEffect, "ArtoriasTendrilCore", windstreak, brokenNoise,
                Vector2.Lerp(start, end, 0.5f), new Vector2(length, 46f), rotation,
                VoidBlack, AbyssViolet, KnightSilver, opacity, tension,
                tension, 1f, BlendState.Additive);

            Draw(tendrilEffect, "ArtoriasTendrilTip", aura, brokenNoise,
                end, hostileTip ? new Vector2(62f, 54f) : new Vector2(46f, 38f), rotation,
                VoidBlack, hostileTip ? DangerMagenta : AbyssViolet, KnightSilver,
                hostileTip ? 0.94f : 0.50f, tension, hostileTip ? 1f : 0f, 1f,
                BlendState.Additive);
        }

        internal static void DrawImpaleTendrils(Vector2 center, float raiseProgress, float opacity)
        {
            LoadAssets();
            float time = Main.GlobalTimeWrappedHourly;
            float pulse = 0.86f + 0.14f * (float)System.Math.Sin(time * 8.4f);
            for (int i = 0; i < ImpaleTendrilAngles.Length; i++)
            {
                float angle = ImpaleTendrilAngles[i]
                    + (float)System.Math.Sin(time * (3.2f + i * 0.17f) + i * 1.7f) * 0.18f;
                float length = (34f + i * 4f) * pulse * MathHelper.Lerp(0.82f, 1.08f, raiseProgress);
                Vector2 start = center + angle.ToRotationVector2() * 4f;
                Vector2 end = center + angle.ToRotationVector2() * length;
                Vector2 midpoint = Vector2.Lerp(start, end, 0.5f);

                Draw(tendrilEffect, "ArtoriasTendrilShadow", smoothNoise, brokenNoise,
                    midpoint, new Vector2(length, 26f), angle,
                    VoidBlack, AbyssIndigo, KnightSilver,
                    opacity * 0.62f, raiseProgress, 0.74f, 1f,
                    BlendState.AlphaBlend);
                Draw(tendrilEffect, "ArtoriasTendrilCore", windstreak, brokenNoise,
                    midpoint, new Vector2(length, 17f), angle,
                    VoidBlack, DangerMagenta, KnightSilver,
                    opacity * 0.80f, raiseProgress, 0.86f, 1f,
                    BlendState.Additive);
            }
        }

        internal static void DrawTendrilHand(Vector2 center, Vector2 size, float progress,
            bool active, float opacity)
        {
            LoadAssets();
            Draw(tendrilHandEffect, "ArtoriasAbyssHandShadow", aura, brokenNoise,
                center, size, 0f, VoidBlack, AbyssIndigo, KnightSilver,
                opacity, progress, active ? 1f : 0f, 1f, BlendState.AlphaBlend);
            Draw(tendrilHandEffect, "ArtoriasAbyssHandCore", aura, brokenNoise,
                center, size * 0.82f, 0f, VoidBlack, DangerMagenta, KnightSilver,
                opacity, progress, active ? 1f : 0f, 1f, BlendState.Additive);
        }

        internal static void DrawMantle(Vector2 center, Vector2 size, float opacity, float intensity, float flowDirection)
        {
            LoadAssets();
            Draw(mantleEffect, "ArtoriasMantleBody", roundSmoke, brokenNoise,
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
            float opacity, float progress, float active, float direction, BlendState blendState,
            bool fullTexture = false)
        {
            Texture2D primary = primaryAsset.Value;
            Texture2D detail = detailAsset.Value;
            int sourceWidth = fullTexture ? primary.Width : System.Math.Clamp((int)drawSize.X, 1, primary.Width);
            int sourceHeight = fullTexture ? primary.Height : System.Math.Clamp((int)drawSize.Y, 1, primary.Height);
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
                effect.Parameters["WorldDrawSize"]?.SetValue(drawSize);
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

        static void DrawBoomerang(Asset<Effect> effectAsset, string techniqueName,
            Texture2D primary, Rectangle source,
            Texture2D detail, Vector2 worldCenter, Vector2 drawSize, float rotation,
            Color darkColor, Color midColor, Color coreColor, float opacity,
            float progress, float active, float direction, float layer,
            SamplerState primarySampler, SpriteEffects spriteEffects = SpriteEffects.None)
        {
            Vector2 actualSize = source.Size();
            Vector2 scale = drawSize / actualSize;
            Vector2 textureSize = primary.Size();
            Vector2 frameUVOrigin = new Vector2(source.X, source.Y) / textureSize;
            Vector2 frameUVScale = source.Size() / textureSize;

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, primarySampler,
                DepthStencilState.None, RasterizerState.CullNone, null,
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
                effect.Parameters["Layer"]?.SetValue(layer);
                effect.Parameters["FrameUVOrigin"]?.SetValue(frameUVOrigin);
                effect.Parameters["FrameUVScale"]?.SetValue(frameUVScale);
                effect.CurrentTechnique.Passes[0].Apply();

                Main.EntitySpriteDraw(primary, worldCenter - Main.screenPosition, source,
                    Color.White, rotation, actualSize * 0.5f, scale, spriteEffects, 0f);
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

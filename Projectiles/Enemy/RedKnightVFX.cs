using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Projectiles.Enemy.Weapons;

namespace tsorcRevamp.Projectiles.Enemy
{
    /// <summary>
    /// Red Knight presentation built from the same pixel-measured effect routes used by Gwyn.
    /// Every shader draw supplies a real pixel rectangle rather than scaling a white mask into geometry.
    /// Small held-object accents deliberately use sprites and dust instead of radial shaders.
    /// </summary>
    internal static class RedKnightVFX
    {
        const string EffectRoot = "tsorcRevamp/Effects/";
        const string NoiseRoot = "tsorcRevamp/Textures/Noise/";
        const string ParticleRoot = "tsorcRevamp/Textures/Particles/";

        static Asset<Effect> cinderTrailEffect;
        static Asset<Effect> cinderNovaEffect;
        static Asset<Effect> judgmentEffect;
        static Asset<Effect> crimsonEffect;
        static Asset<Effect> dominionEffect;
        static Asset<Texture2D> auraNoise;
        static Asset<Texture2D> smoothNoise;
        static Asset<Texture2D> brokenNoise;
        static Asset<Texture2D> turbulentNoise;
        static Asset<Texture2D> grainNoise;
        static Asset<Texture2D> marbleNoise;
        static Asset<Texture2D> veinNoise;
        static Asset<Texture2D> flare;
        static Asset<Texture2D> flameOne;
        static Asset<Texture2D> flameTwo;
        static Asset<Texture2D> flameThree;
        static Asset<Texture2D> muzzleTwo;

        static readonly Color CrimsonCinder = new(38, 1, 5);
        static readonly Color CrimsonFlame = new(208, 22, 28);
        static readonly Color CrimsonCore = new(255, 112, 48);
        static readonly Color GreatCinder = new(48, 1, 3);
        static readonly Color GreatFlame = new(244, 42, 25);
        static readonly Color GreatCore = new(255, 164, 78);
        static readonly Color StormCinder = new(3, 15, 36);
        static readonly Color StormFlame = new(38, 132, 226);
        static readonly Color StormCore = new(126, 218, 255);
        static readonly Color PoisonCinder = new(15, 25, 2);
        static readonly Color PoisonBody = new(118, 174, 12);
        static readonly Color PoisonCore = new(226, 255, 102);

        static void LoadAssets()
        {
            if (Main.dedServ)
            {
                return;
            }

            cinderTrailEffect ??= ModContent.Request<Effect>(EffectRoot + "GwynCinderTrail", AssetRequestMode.ImmediateLoad);
            cinderNovaEffect ??= ModContent.Request<Effect>(EffectRoot + "GwynCinderNova", AssetRequestMode.ImmediateLoad);
            judgmentEffect ??= ModContent.Request<Effect>(EffectRoot + "GwynSunlightJudgment", AssetRequestMode.ImmediateLoad);
            crimsonEffect ??= ModContent.Request<Effect>(EffectRoot + "RedKnightCrimsonVFX", AssetRequestMode.ImmediateLoad);
            dominionEffect ??= ModContent.Request<Effect>(EffectRoot + "GreatRedKnightDominion", AssetRequestMode.ImmediateLoad);
            auraNoise ??= ModContent.Request<Texture2D>(NoiseRoot + "T_Aurax44", AssetRequestMode.ImmediateLoad);
            smoothNoise ??= ModContent.Request<Texture2D>(NoiseRoot + "T_VFX_NoiseF1", AssetRequestMode.ImmediateLoad);
            brokenNoise ??= ModContent.Request<Texture2D>(NoiseRoot + "T_VFX_Noise41", AssetRequestMode.ImmediateLoad);
            turbulentNoise ??= ModContent.Request<Texture2D>(NoiseRoot + "Turbulence_05-512x512", AssetRequestMode.ImmediateLoad);
            grainNoise ??= ModContent.Request<Texture2D>(NoiseRoot + "Grainy_07-512x512", AssetRequestMode.ImmediateLoad);
            marbleNoise ??= ModContent.Request<Texture2D>(NoiseRoot + "T_MarbleNoise_tiled", AssetRequestMode.ImmediateLoad);
            veinNoise ??= ModContent.Request<Texture2D>(NoiseRoot + "Vein_04-512x512", AssetRequestMode.ImmediateLoad);
            flare ??= ModContent.Request<Texture2D>(NoiseRoot + "T_VFX_Flare_666", AssetRequestMode.ImmediateLoad);
            flameOne ??= ModContent.Request<Texture2D>(ParticleRoot + "flame_01_a", AssetRequestMode.ImmediateLoad);
            flameTwo ??= ModContent.Request<Texture2D>(ParticleRoot + "flame_02_a", AssetRequestMode.ImmediateLoad);
            flameThree ??= ModContent.Request<Texture2D>(ParticleRoot + "flame_03_a", AssetRequestMode.ImmediateLoad);
            muzzleTwo ??= ModContent.Request<Texture2D>(ParticleRoot + "muzzle_02_a", AssetRequestMode.ImmediateLoad);
        }

        internal static void DrawSpearWake(Vector2 center, float rotation, Vector2 size, float opacity, bool empowered)
        {
            Vector2 wakeSize = new(Math.Min(size.X, 82f), Math.Min(size.Y, 18f));
            DrawCrimsonQuad("RedKnightSpearWake", center, rotation, wakeSize,
                Math.Min(opacity, empowered ? 0.58f : 0.48f),
                new Color(20, 23, 28),
                empowered ? new Color(176, 188, 202) : new Color(142, 151, 162),
                empowered ? new Color(248, 252, 255) : new Color(224, 231, 238),
                0.45f, empowered ? 0.92f : 0.72f, 1f, 1f);
        }

        internal static void DrawGroundWave(Vector2 groundPoint, Vector2 velocity, Vector2 size, float opacity)
        {
            int direction = velocity.X < 0f ? -1 : 1;
            DrawGroundMiasma(groundPoint, direction,
                Math.Min(size.X, 58f), Math.Min(size.Y + 14f, 36f),
                Math.Min(opacity * 0.28f, 0.25f), 0.92f, 0.72f, empowered: false);
            DrawCrimsonFlameCluster(groundPoint + new Vector2(0f, 6f),
                0.76f, opacity * 0.94f, 0.72f, 11f, direction);
        }

        internal static void DrawStandardCharge(Vector2 groundPoint, float progress, KnightStandardMode mode)
        {
            bool empowered = mode != KnightStandardMode.RedKnight;
            float length = MathHelper.Lerp(34f, empowered ? 82f : 68f, progress);
            float height = MathHelper.Lerp(24f, empowered ? 44f : 36f, progress);
            float opacity = MathHelper.Lerp(0.14f, empowered ? 0.66f : 0.54f, progress);

            if (mode != KnightStandardMode.GreatRight)
            {
                DrawGroundMiasma(groundPoint, 1, length, height, opacity * 0.28f,
                    progress, 0.48f + progress * 0.32f, empowered);
            }
            if (mode != KnightStandardMode.GreatLeft)
            {
                DrawGroundMiasma(groundPoint, -1, length, height, opacity * 0.28f,
                    progress, 0.48f + progress * 0.32f, empowered);
            }
            DrawCrimsonFlameCluster(groundPoint + new Vector2(0f, 7f), progress,
                opacity * 1.18f, empowered ? 1.22f : 0.96f,
                empowered ? 24f : 17f, mode == KnightStandardMode.GreatLeft ? 1 : -1);
        }

        /// <summary>Emit a simple flame dust from the bomb sprite's fixed top anchor.</summary>
        internal static void DrawBombFuse(Vector2 fusePoint, float progress, bool planted)
        {
            if (Main.dedServ || !Main.rand.NextBool(planted ? 2 : 3))
            {
                return;
            }

            Dust spark = Dust.NewDustPerfect(fusePoint, DustID.Torch,
                new Vector2(Main.rand.NextFloat(-0.25f, 0.25f), Main.rand.NextFloat(-0.9f, -0.45f)),
                40, default, MathHelper.Lerp(0.65f, 0.9f, progress));
            spark.noGravity = true;
            spark.fadeIn = 0.25f;
        }

        internal static void DrawLightningLane(Vector2 start, Vector2 velocity, float length,
            float progress, bool active, float fade, float activeWidth = 15f)
        {
            Vector2 direction = velocity.SafeNormalize(Vector2.UnitX);
            DrawJudgmentLane(start, direction, length, active ? activeWidth : 8f,
                progress, active, fade);
        }

        internal static void DrawCrimsonDominion(Vector2 center, int age, float baseRotation,
            int rotationDirection, float sweepAngle, int movementDirection, bool sweepActive)
        {
            LoadAssets();
            if (Main.dedServ || dominionEffect == null)
            {
                return;
            }

            const float arenaRadius = CrimsonDominionController.Radius;
            float coverage = 2f * (arenaRadius + Math.Max(Main.screenWidth, Main.screenHeight));
            float radiusRatio = arenaRadius / coverage;
            float fieldOpacity;
            float edgeOpacity;

            if (age < CrimsonDominionController.BuildTicks)
            {
                float build = MathHelper.Clamp(age / (float)CrimsonDominionController.BuildTicks, 0f, 1f);
                fieldOpacity = MathHelper.Lerp(0f, 0.78f, build);
                edgeOpacity = MathHelper.Lerp(0.08f, 0.95f, build);
                float gatherSize = MathHelper.Lerp(96f, 188f, build);
                DrawDominionQuad("DominionNova", center, Vector2.One * gatherSize, 0f,
                    0.22f * build, build, 0f, 0.78f, 0.5f, rotationDirection, BlendState.Additive);
            }
            else if (age < CrimsonDominionController.EscapeStart)
            {
                fieldOpacity = 0.8f;
                edgeOpacity = 1f;
            }
            else
            {
                float escape = MathHelper.Clamp((age - CrimsonDominionController.EscapeStart) /
                    (float)CrimsonDominionController.EscapeTicks, 0f, 1f);
                fieldOpacity = 0.8f * (1f - escape);
                edgeOpacity = MathHelper.Lerp(0.9f, 0.22f, escape);
            }

            if (age < CrimsonDominionController.NovaStart)
            {
                DrawDominionQuad("DominionField", center, Vector2.One * coverage, 0f,
                    fieldOpacity, 1f, 0f, 0.88f, radiusRatio, rotationDirection, BlendState.NonPremultiplied);
                DrawDominionQuad("DominionEdge", center, Vector2.One * coverage, 0f,
                    edgeOpacity, 1f, 0f, 1.08f, radiusRatio, rotationDirection, BlendState.Additive);
            }

            if (age < CrimsonDominionController.BuildTicks)
            {
                float build = MathHelper.Clamp(age / (float)CrimsonDominionController.BuildTicks, 0f, 1f);
                DrawDominionPair(center, baseRotation, rotationDirection, active: false,
                    MathHelper.Lerp(0.08f, 0.62f, build), 42f);
            }

            if (age >= CrimsonDominionController.BuildTicks && age < CrimsonDominionController.EscapeStart
                && sweepActive)
            {
                DrawDominionPair(center, sweepAngle, movementDirection, active: true, 0.9f, 54f);
                float previewAngle = sweepAngle + movementDirection * MathHelper.Pi / 10f;
                DrawDominionPair(center, previewAngle, movementDirection, active: false, 0.28f, 34f);
            }

            if (age >= CrimsonDominionController.EscapeStart && age < CrimsonDominionController.NovaStart)
            {
                float charge = MathHelper.Clamp((age - CrimsonDominionController.EscapeStart) /
                    (float)CrimsonDominionController.EscapeTicks, 0f, 1f);
                Vector2 novaSize = Vector2.One * arenaRadius * 2f;
                DrawDominionQuad("DominionNova", center, novaSize, 0f,
                    MathHelper.Lerp(0.12f, 0.58f, charge), charge, 0f,
                    MathHelper.Lerp(0.55f, 0.92f, charge), 0.5f, rotationDirection,
                    BlendState.NonPremultiplied);
            }
            else if (age >= CrimsonDominionController.NovaStart)
            {
                float fade = age < CrimsonDominionController.FadeStart ? 1f
                    : MathHelper.Clamp(1f - (age - CrimsonDominionController.FadeStart) /
                        (float)CrimsonDominionController.FadeTicks, 0f, 1f);
                Vector2 novaSize = Vector2.One * arenaRadius * 2f;
                DrawDominionQuad("DominionNova", center, novaSize, 0f,
                    0.72f * fade, 1f, 1f, 0.96f, 0.5f, rotationDirection,
                    BlendState.NonPremultiplied);
                DrawDominionQuad("DominionNova", center, novaSize, 0f,
                    0.92f * fade, 1f, 1f, 1.34f, 0.5f, rotationDirection,
                    BlendState.Additive);
            }
        }

        static void DrawDominionPair(Vector2 center, float angle, int movementDirection,
            bool active, float opacity, float width)
        {
            Vector2 direction = angle.ToRotationVector2();
            float length = CrimsonDominionController.Radius - CrimsonDominionController.InnerRadius;
            for (int sign = -1; sign <= 1; sign += 2)
            {
                Vector2 signedDirection = direction * sign;
                DrawDominionLance(center + signedDirection * CrimsonDominionController.InnerRadius,
                    signedDirection, length, width, active ? 1f : 0.68f, active,
                    opacity, movementDirection * sign);
            }
        }

        internal static void DrawDominionExecution(Vector2 center, Vector2 direction,
            float progress, bool active, float fade)
        {
            direction = direction.SafeNormalize(Vector2.UnitX);
            float length = CrimsonDominionController.Radius - CrimsonDominionController.InnerRadius;
            DrawDominionLance(center + direction * CrimsonDominionController.InnerRadius,
                direction, length, active ? 76f : 58f, progress, active,
                fade * (active ? 1f : 0.82f), direction.X < 0f ? -1 : 1);
        }

        static void DrawDominionLance(Vector2 start, Vector2 direction, float length,
            float width, float progress, bool active, float opacity, int movementDirection)
        {
            direction = direction.SafeNormalize(Vector2.UnitX);
            Vector2 center = start + direction * length * 0.5f;
            DrawDominionQuad("DominionLance", center, new Vector2(length, width), direction.ToRotation(),
                opacity, progress, active ? 1f : 0f, active ? 1.25f : 0.82f, 0.5f,
                movementDirection, BlendState.Additive);
        }

        internal static void DrawToxicMotes(Vector2 center, int count, float progress, float radius)
        {
            DrawPoisonOrb(center, Vector2.Zero, 0.72f + progress * 0.18f,
                0.86f + progress * 0.18f);

            if (Main.dedServ || !Main.rand.NextBool(4))
            {
                return;
            }

            Dust mote = Dust.NewDustPerfect(
                center + Main.rand.NextVector2Circular(Math.Min(radius, 8f), Math.Min(radius, 8f)),
                DustID.CursedTorch, Main.rand.NextVector2Circular(0.22f, 0.22f), 120,
                new Color(135, 196, 24), Main.rand.NextFloat(0.55f, 0.8f));
            mote.noGravity = true;
        }

        internal static void DrawPoisonOrb(Vector2 center, Vector2 velocity, float opacity, float scale = 1f)
        {
            bool moving = velocity.LengthSquared() > 0.04f;
            Vector2 direction = moving ? velocity.SafeNormalize(Vector2.UnitX) : Vector2.UnitX;
            Vector2 size = Vector2.One * 30f * scale * (moving ? 0.9f : 1f);
            float pulse = 0.92f + 0.08f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 8f);
            DrawCrimsonQuad("RedKnightPoisonOrb", center, 0f, size, opacity * pulse,
                PoisonCinder, PoisonBody, PoisonCore, 0.72f, 0.8f,
                direction.X < 0f ? -1f : 1f, 0f);
        }

        internal static void DrawUltrakillSeal(Vector2 center, float progress)
        {
            float eased = progress * progress * (3f - 2f * progress);
            float gatherRadius = MathHelper.Lerp(128f, 34f, eased);
            float pulse = 0.92f + 0.08f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 9f);
            DrawCrimsonQuad("RedKnightUltrakillGather", center, 0f,
                new Vector2(270f, 205f), (0.62f + progress * 0.18f) * pulse,
                new Color(22, 0, 22), new Color(176, 10, 46), new Color(255, 118, 92),
                progress, 1.1f, 1f, 0f);

            for (int i = 0; i < 3; i++)
            {
                float angle = i switch
                {
                    0 => -0.18f,
                    1 => 2.18f,
                    _ => 4.42f
                };
                Vector2 outward = angle.ToRotationVector2();
                Vector2 inward = -outward;
                DrawCrimsonQuad("RedKnightSpearWake", center + outward * gatherRadius * 0.52f,
                    inward.ToRotation(), new Vector2(Math.Max(26f, gatherRadius * 0.92f), 11f + i * 2f),
                    (0.24f + progress * 0.16f) * pulse,
                    GreatCinder, GreatFlame, GreatCore, progress, 0.72f, 1f, 1f);
            }

            if (!Main.dedServ && Main.rand.NextBool(2))
            {
                Vector2 dustPosition = center + Main.rand.NextVector2CircularEdge(gatherRadius, gatherRadius * 0.72f);
                Dust mote = Dust.NewDustPerfect(dustPosition,
                    Main.rand.NextBool(4) ? DustID.Shadowflame : DustID.RedTorch,
                    (center - dustPosition).SafeNormalize(Vector2.Zero) * Main.rand.NextFloat(1.2f, 2.1f),
                    120, new Color(208, 28, 56), Main.rand.NextFloat(0.65f, 0.95f));
                mote.noGravity = true;
            }
        }

        internal static void DrawHerald(Vector2 center, float progress, bool storm)
        {
            float envelope = (float)Math.Sin(progress * MathHelper.Pi);

            if (storm)
            {
                float radius = MathHelper.Lerp(42f, 148f, MathHelper.Clamp(progress * 1.1f, 0f, 1f));
                DrawRadialBand(center, radius, 4f, 18f, envelope * 0.38f,
                    StormCinder, StormFlame, StormCore);
                for (int i = 0; i < 3; i++)
                {
                    Vector2 direction = (MathHelper.TwoPi * i / 3f - MathHelper.PiOver2
                        - progress * 0.45f).ToRotationVector2();
                    DrawJudgmentLane(center + direction * 48f, direction, 92f, 7f,
                        progress, active: false, envelope * 0.18f);
                }
            }
            else
            {
                Vector2 furnaceBase = center + new Vector2(0f, 30f);
                DrawCrimsonFlameCluster(furnaceBase, progress, envelope * 0.96f,
                    MathHelper.Lerp(1.05f, 1.72f, progress),
                    MathHelper.Lerp(22f, 38f, progress), progress < 0.5f ? -1 : 1);
                if (!Main.dedServ && envelope > 0.08f && Main.rand.NextBool(2))
                {
                    Vector2 dustPosition = furnaceBase + new Vector2(
                        Main.rand.NextFloat(-46f, 46f), Main.rand.NextFloat(-12f, 4f));
                    Dust ember = Dust.NewDustPerfect(dustPosition,
                        Main.rand.NextBool(3) ? DustID.Shadowflame : DustID.RedTorch,
                        new Vector2(Main.rand.NextFloat(-0.45f, 0.45f), Main.rand.NextFloat(-3.2f, -1.1f)),
                        110, new Color(205, 18, 42), Main.rand.NextFloat(0.72f, 1.12f));
                    ember.noGravity = true;
                }
            }
        }

        internal static void DrawBurst(RedKnightBurstKind kind, Vector2 center, float progress, float scale)
        {
            if (kind != RedKnightBurstKind.BombExplosion)
            {
                bool empowered = kind == RedKnightBurstKind.StandardImpact || scale >= 1f;
                float envelope = (1f - progress) * (empowered ? 0.96f : 0.78f);
                DrawCrimsonFlameCluster(center + new Vector2(0f, 8f), progress,
                    envelope, (empowered ? 1.08f : 0.82f) * scale,
                    (empowered ? 18f : 12f) * scale, progress < 0.5f ? -1 : 1);
                return;
            }

            float diameter = 132f * scale;
            DrawCrimsonQuad("RedKnightBombBlast", center, 0f,
                new Vector2(diameter, diameter), 0.96f,
                new Color(30, 0, 12), GreatFlame, GreatCore,
                progress, 1f, 1f, 0f);

            if (progress < 0.16f)
            {
                DrawSmallFlare(center, GreatCore, (1f - progress / 0.16f) * 0.075f * scale);
            }
        }

        static void DrawGroundMiasma(Vector2 groundOrigin, int direction, float length, float height,
            float opacity, float progress, float intensity, bool empowered)
        {
            direction = direction < 0 ? -1 : 1;
            Vector2 size = new(Math.Max(4f, length), Math.Max(4f, height));
            Vector2 center = groundOrigin + new Vector2(direction * size.X * 0.5f, -size.Y * 0.5f + 2f);
            DrawCrimsonQuad("RedKnightGroundMiasma", center, 0f, size, opacity,
                empowered ? new Color(38, 0, 28) : new Color(28, 0, 20),
                empowered ? new Color(218, 18, 52) : new Color(174, 10, 40),
                empowered ? new Color(255, 124, 82) : new Color(246, 72, 58),
                progress, intensity, direction, 0f);
        }

        static void DrawCrimsonFlameCluster(Vector2 groundPoint, float progress,
            float opacity, float scale, float spread, int direction)
        {
            LoadAssets();
            if (Main.dedServ || flameOne == null || flameTwo == null || flameThree == null
                || muzzleTwo == null || opacity <= 0f)
            {
                return;
            }

            direction = direction < 0 ? -1 : 1;
            float growth = MathHelper.Lerp(0.62f, 1.12f,
                MathHelper.Clamp(progress, 0f, 1f));
            SpriteEffects facing = direction < 0 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            DrawCrimsonFlameTongue(flameTwo.Value,
                groundPoint + new Vector2(-spread, 0f), new Vector2(40f, 58f) * scale * growth,
                -0.09f * direction, opacity * 0.68f, progress, 0.86f, direction,
                facing, BlendState.NonPremultiplied);
            DrawCrimsonFlameTongue(flameThree.Value,
                groundPoint + new Vector2(spread, 1f), new Vector2(35f, 52f) * scale * growth,
                0.11f * direction, opacity * 0.62f, progress, 0.9f, -direction,
                facing, BlendState.NonPremultiplied);
            DrawCrimsonFlameTongue(flameOne.Value,
                groundPoint + new Vector2(-spread * 0.18f, 2f), new Vector2(31f, 49f) * scale * growth,
                -0.035f * direction, opacity * 0.72f, progress, 1.02f, direction,
                facing, BlendState.Additive);
            DrawCrimsonFlameTongue(muzzleTwo.Value,
                groundPoint + new Vector2(spread * 0.18f, 3f), new Vector2(23f, 41f) * scale * growth,
                0.04f * direction, opacity * 0.62f, progress, 1.08f, -direction,
                facing, BlendState.Additive);
        }

        static void DrawDominionQuad(string techniqueName, Vector2 center, Vector2 size, float rotation,
            float opacity, float progress, float active, float intensity, float radiusRatio,
            float direction, BlendState blendState)
        {
            LoadAssets();
            if (Main.dedServ || dominionEffect == null || marbleNoise == null || turbulentNoise == null
                || veinNoise == null || opacity <= 0f || size.X <= 0f || size.Y <= 0f)
            {
                return;
            }

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, blendState, SamplerState.LinearWrap,
                DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

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
                graphicsDevice.Textures[2] = turbulentNoise.Value;
                graphicsDevice.Textures[3] = veinNoise.Value;
                graphicsDevice.SamplerStates[1] = SamplerState.LinearWrap;
                graphicsDevice.SamplerStates[2] = SamplerState.LinearWrap;
                graphicsDevice.SamplerStates[3] = SamplerState.LinearWrap;

                Effect effect = dominionEffect.Value;
                effect.CurrentTechnique = effect.Techniques[techniqueName];
                effect.Parameters["DarkColor"].SetValue(new Color(17, 0, 14).ToVector3());
                effect.Parameters["MidColor"].SetValue(new Color(178, 9, 48).ToVector3());
                effect.Parameters["CoreColor"].SetValue(new Color(255, 122, 108).ToVector3());
                effect.Parameters["Opacity"].SetValue(opacity);
                effect.Parameters["Time"].SetValue(Main.GlobalTimeWrappedHourly);
                effect.Parameters["Progress"].SetValue(progress);
                effect.Parameters["Intensity"].SetValue(intensity);
                effect.Parameters["Active"].SetValue(active);
                effect.Parameters["RadiusRatio"].SetValue(radiusRatio);
                effect.Parameters["Direction"].SetValue(direction);
                effect.Parameters["DrawSize"]?.SetValue(size);
                effect.CurrentTechnique.Passes[0].Apply();

                Texture2D pixel = TextureAssets.MagicPixel.Value;
                Main.EntitySpriteDraw(pixel, center - Main.screenPosition, null, Color.White,
                    rotation, pixel.Size() * 0.5f, size / pixel.Size(), SpriteEffects.None, 0f);
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

        static void DrawCrimsonFlameTongue(Texture2D texture, Vector2 groundPoint,
            Vector2 size, float rotation, float opacity, float progress, float intensity,
            float direction, SpriteEffects effects, BlendState blendState)
        {
            LoadAssets();
            if (Main.dedServ || crimsonEffect == null || turbulentNoise == null || grainNoise == null
                || texture == null || opacity <= 0f || size.X <= 0f || size.Y <= 0f)
            {
                return;
            }

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, blendState, SamplerState.LinearClamp,
                DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            GraphicsDevice graphicsDevice = Main.instance.GraphicsDevice;
            Texture previousTexture1 = graphicsDevice.Textures[1];
            Texture previousTexture2 = graphicsDevice.Textures[2];
            SamplerState previousSampler1 = graphicsDevice.SamplerStates[1];
            SamplerState previousSampler2 = graphicsDevice.SamplerStates[2];
            try
            {
                graphicsDevice.Textures[1] = turbulentNoise.Value;
                graphicsDevice.Textures[2] = grainNoise.Value;
                graphicsDevice.SamplerStates[1] = SamplerState.LinearWrap;
                graphicsDevice.SamplerStates[2] = SamplerState.LinearWrap;

                Effect effect = crimsonEffect.Value;
                effect.CurrentTechnique = effect.Techniques["RedKnightCrimsonFlame"];
                effect.Parameters["DarkColor"].SetValue(new Color(12, 0, 8).ToVector3());
                effect.Parameters["MidColor"].SetValue(new Color(205, 8, 38).ToVector3());
                effect.Parameters["CoreColor"].SetValue(new Color(255, 104, 68).ToVector3());
                effect.Parameters["Opacity"].SetValue(opacity);
                effect.Parameters["Time"].SetValue(Main.GlobalTimeWrappedHourly);
                effect.Parameters["Progress"].SetValue(progress);
                effect.Parameters["Intensity"].SetValue(intensity);
                effect.Parameters["Direction"].SetValue(direction);
                effect.Parameters["TailStrength"].SetValue(0f);
                effect.Parameters["DrawSize"]?.SetValue(size);
                effect.CurrentTechnique.Passes[0].Apply();

                Main.EntitySpriteDraw(texture, groundPoint - Main.screenPosition, null, Color.White,
                    rotation, new Vector2(texture.Width * 0.5f, texture.Height),
                    size / texture.Size(), effects, 0f);
            }
            finally
            {
                graphicsDevice.Textures[1] = previousTexture1;
                graphicsDevice.Textures[2] = previousTexture2;
                graphicsDevice.SamplerStates[1] = previousSampler1;
                graphicsDevice.SamplerStates[2] = previousSampler2;
                UsefulFunctions.RestartSpritebatch(ref Main.spriteBatch);
            }
        }

        static void DrawCrimsonQuad(string techniqueName, Vector2 center, float rotation, Vector2 size,
            float opacity, Color darkColor, Color midColor, Color coreColor,
            float progress, float intensity, float direction, float tailStrength)
        {
            LoadAssets();
            if (Main.dedServ || crimsonEffect == null || turbulentNoise == null || grainNoise == null
                || opacity <= 0f || size.X <= 0f || size.Y <= 0f)
            {
                return;
            }

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearWrap,
                DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            GraphicsDevice graphicsDevice = Main.instance.GraphicsDevice;
            Texture previousTexture1 = graphicsDevice.Textures[1];
            Texture previousTexture2 = graphicsDevice.Textures[2];
            SamplerState previousSampler1 = graphicsDevice.SamplerStates[1];
            SamplerState previousSampler2 = graphicsDevice.SamplerStates[2];
            try
            {
                graphicsDevice.Textures[1] = turbulentNoise.Value;
                graphicsDevice.Textures[2] = grainNoise.Value;
                graphicsDevice.SamplerStates[1] = SamplerState.LinearWrap;
                graphicsDevice.SamplerStates[2] = SamplerState.LinearWrap;

                Effect effect = crimsonEffect.Value;
                effect.CurrentTechnique = effect.Techniques[techniqueName];
                effect.Parameters["DarkColor"].SetValue(darkColor.ToVector3());
                effect.Parameters["MidColor"].SetValue(midColor.ToVector3());
                effect.Parameters["CoreColor"].SetValue(coreColor.ToVector3());
                effect.Parameters["Opacity"].SetValue(opacity);
                effect.Parameters["Time"].SetValue(Main.GlobalTimeWrappedHourly);
                effect.Parameters["Progress"].SetValue(progress);
                effect.Parameters["Intensity"].SetValue(intensity);
                effect.Parameters["Direction"].SetValue(direction);
                effect.Parameters["TailStrength"].SetValue(tailStrength);
                effect.Parameters["DrawSize"]?.SetValue(size);
                effect.CurrentTechnique.Passes[0].Apply();

                Texture2D pixel = TextureAssets.MagicPixel.Value;
                Main.EntitySpriteDraw(pixel, center - Main.screenPosition, null, Color.White,
                    rotation, pixel.Size() * 0.5f, size / pixel.Size(), SpriteEffects.None, 0f);
            }
            finally
            {
                graphicsDevice.Textures[1] = previousTexture1;
                graphicsDevice.Textures[2] = previousTexture2;
                graphicsDevice.SamplerStates[1] = previousSampler1;
                graphicsDevice.SamplerStates[2] = previousSampler2;
                UsefulFunctions.RestartSpritebatch(ref Main.spriteBatch);
            }
        }

        static void DrawCinderArc(Vector2 center, float rotation, Vector2 size,
            float opacity, bool empowered, float progress)
        {
            LoadAssets();
            if (Main.dedServ || cinderTrailEffect == null || auraNoise == null || smoothNoise == null || opacity <= 0f)
            {
                return;
            }

            Rectangle source = new(0, 0, Math.Max(2, (int)Math.Ceiling(size.X)), Math.Max(2, (int)Math.Ceiling(size.Y)));
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearWrap,
                DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            GraphicsDevice graphicsDevice = Main.instance.GraphicsDevice;
            Texture previousTexture = graphicsDevice.Textures[1];
            SamplerState previousSampler = graphicsDevice.SamplerStates[1];
            try
            {
                graphicsDevice.Textures[1] = smoothNoise.Value;
                graphicsDevice.SamplerStates[1] = SamplerState.LinearWrap;
                Effect effect = cinderTrailEffect.Value;
                effect.CurrentTechnique = effect.Techniques["GwynCinderArc"];
                effect.Parameters["CinderColor"].SetValue((empowered ? GreatCinder : CrimsonCinder).ToVector3());
                effect.Parameters["FlameColor"].SetValue((empowered ? GreatFlame : CrimsonFlame).ToVector3());
                effect.Parameters["CoreColor"].SetValue((empowered ? GreatCore : CrimsonCore).ToVector3());
                effect.Parameters["Opacity"].SetValue(opacity);
                effect.Parameters["Time"].SetValue(Main.GlobalTimeWrappedHourly);
                effect.Parameters["DrawSize"].SetValue(source.Size());
                effect.Parameters["PrimaryTextureSize"].SetValue(auraNoise.Value.Size());
                effect.Parameters["Progress"].SetValue(progress);
                effect.CurrentTechnique.Passes[0].Apply();

                Main.EntitySpriteDraw(auraNoise.Value, center - Main.screenPosition, source, Color.White,
                    rotation, source.Size() * 0.5f, 1f, SpriteEffects.None, 0f);
            }
            finally
            {
                graphicsDevice.Textures[1] = previousTexture;
                graphicsDevice.SamplerStates[1] = previousSampler;
                UsefulFunctions.RestartSpritebatch(ref Main.spriteBatch);
            }
        }

        static void DrawRadialBand(Vector2 center, float radius, float halfThickness, float trailLength,
            float opacity, Color outerColor, Color flameColor, Color coreColor)
        {
            LoadAssets();
            if (Main.dedServ || cinderNovaEffect == null || smoothNoise == null || brokenNoise == null
                || opacity <= 0f || radius <= 0f)
            {
                return;
            }

            float drawRadius = Math.Max(2f, radius + halfThickness + 5f);
            int diameter = Math.Max(2, (int)Math.Ceiling(drawRadius * 2f));
            Rectangle source = new(0, 0, diameter, diameter);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearWrap,
                DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            GraphicsDevice graphicsDevice = Main.instance.GraphicsDevice;
            Texture previousTexture = graphicsDevice.Textures[1];
            SamplerState previousSampler = graphicsDevice.SamplerStates[1];
            try
            {
                graphicsDevice.Textures[1] = brokenNoise.Value;
                graphicsDevice.SamplerStates[1] = SamplerState.LinearWrap;
                Effect effect = cinderNovaEffect.Value;
                effect.CurrentTechnique = effect.Techniques["GwynCinderNova"];
                effect.Parameters["OuterColor"].SetValue(outerColor.ToVector3());
                effect.Parameters["FlameColor"].SetValue(flameColor.ToVector3());
                effect.Parameters["CoreColor"].SetValue(coreColor.ToVector3());
                effect.Parameters["Opacity"].SetValue(opacity);
                effect.Parameters["Time"].SetValue(Main.GlobalTimeWrappedHourly);
                effect.Parameters["DrawSize"].SetValue(source.Size());
                effect.Parameters["PrimaryTextureSize"].SetValue(smoothNoise.Value.Size());
                effect.Parameters["RingRadius"].SetValue(radius);
                effect.Parameters["RingHalfThickness"].SetValue(Math.Max(2f, halfThickness));
                effect.Parameters["TrailLength"].SetValue(Math.Max(4f, trailLength));
                effect.CurrentTechnique.Passes[0].Apply();

                Main.EntitySpriteDraw(smoothNoise.Value, center - Main.screenPosition, source, Color.White,
                    0f, source.Size() * 0.5f, 1f, SpriteEffects.None, 0f);
            }
            finally
            {
                graphicsDevice.Textures[1] = previousTexture;
                graphicsDevice.SamplerStates[1] = previousSampler;
                UsefulFunctions.RestartSpritebatch(ref Main.spriteBatch);
            }
        }

        static void DrawJudgmentLane(Vector2 start, Vector2 direction, float length, float width,
            float progress, bool active, float opacity)
        {
            LoadAssets();
            if (Main.dedServ || judgmentEffect == null || smoothNoise == null || brokenNoise == null
                || opacity <= 0f)
            {
                return;
            }

            direction = direction.SafeNormalize(Vector2.UnitX);
            Rectangle source = new(0, 0, Math.Max(4, (int)Math.Ceiling(width)), Math.Max(8, (int)Math.Ceiling(length)));
            Vector2 center = start + direction * length * 0.5f;
            float rotation = direction.ToRotation() - MathHelper.PiOver2;

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearWrap,
                DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            GraphicsDevice graphicsDevice = Main.instance.GraphicsDevice;
            Texture previousTexture = graphicsDevice.Textures[1];
            SamplerState previousSampler = graphicsDevice.SamplerStates[1];
            try
            {
                graphicsDevice.Textures[1] = brokenNoise.Value;
                graphicsDevice.SamplerStates[1] = SamplerState.LinearWrap;
                Effect effect = judgmentEffect.Value;
                effect.CurrentTechnique = effect.Techniques["GwynSunlightJudgmentColumn"];
                effect.Parameters["GoldColor"].SetValue(StormCinder.ToVector3());
                effect.Parameters["HotColor"].SetValue(StormFlame.ToVector3());
                effect.Parameters["CoreColor"].SetValue(StormCore.ToVector3());
                effect.Parameters["Opacity"].SetValue(opacity);
                effect.Parameters["Time"].SetValue(Main.GlobalTimeWrappedHourly);
                effect.Parameters["DrawSize"].SetValue(source.Size());
                effect.Parameters["PrimaryTextureSize"].SetValue(smoothNoise.Value.Size());
                effect.Parameters["Progress"].SetValue(progress);
                effect.Parameters["Active"].SetValue(active ? 1f : 0f);
                effect.Parameters["Direction"]?.SetValue(Math.Sign(direction.X));
                effect.CurrentTechnique.Passes[0].Apply();

                Main.EntitySpriteDraw(smoothNoise.Value, center - Main.screenPosition, source, Color.White,
                    rotation, source.Size() * 0.5f, 1f, SpriteEffects.None, 0f);
            }
            finally
            {
                graphicsDevice.Textures[1] = previousTexture;
                graphicsDevice.SamplerStates[1] = previousSampler;
                UsefulFunctions.RestartSpritebatch(ref Main.spriteBatch);
            }
        }

        static void DrawSmallFlare(Vector2 center, Color color, float scale)
        {
            LoadAssets();
            if (Main.dedServ || flare == null || scale <= 0f)
            {
                return;
            }
            Texture2D texture = flare.Value;
            Main.EntitySpriteDraw(texture, center - Main.screenPosition, null, color * 0.5f,
                Main.GlobalTimeWrappedHourly, texture.Size() * 0.5f, scale, SpriteEffects.None, 0f);
        }
    }
}

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using Terraria;
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

        static Asset<Effect> cinderTrailEffect;
        static Asset<Effect> cinderNovaEffect;
        static Asset<Effect> judgmentEffect;
        static Asset<Texture2D> auraNoise;
        static Asset<Texture2D> smoothNoise;
        static Asset<Texture2D> brokenNoise;
        static Asset<Texture2D> flare;

        static readonly Color CrimsonCinder = new(38, 1, 5);
        static readonly Color CrimsonFlame = new(208, 22, 28);
        static readonly Color CrimsonCore = new(255, 112, 48);
        static readonly Color GreatCinder = new(48, 1, 3);
        static readonly Color GreatFlame = new(244, 42, 25);
        static readonly Color GreatCore = new(255, 164, 78);
        static readonly Color StormCinder = new(3, 15, 36);
        static readonly Color StormFlame = new(38, 132, 226);
        static readonly Color StormCore = new(126, 218, 255);

        static void LoadAssets()
        {
            if (Main.dedServ)
            {
                return;
            }

            cinderTrailEffect ??= ModContent.Request<Effect>(EffectRoot + "GwynCinderTrail", AssetRequestMode.ImmediateLoad);
            cinderNovaEffect ??= ModContent.Request<Effect>(EffectRoot + "GwynCinderNova", AssetRequestMode.ImmediateLoad);
            judgmentEffect ??= ModContent.Request<Effect>(EffectRoot + "GwynSunlightJudgment", AssetRequestMode.ImmediateLoad);
            auraNoise ??= ModContent.Request<Texture2D>(NoiseRoot + "T_Aurax44", AssetRequestMode.ImmediateLoad);
            smoothNoise ??= ModContent.Request<Texture2D>(NoiseRoot + "T_VFX_NoiseF1", AssetRequestMode.ImmediateLoad);
            brokenNoise ??= ModContent.Request<Texture2D>(NoiseRoot + "T_VFX_Noise41", AssetRequestMode.ImmediateLoad);
            flare ??= ModContent.Request<Texture2D>(NoiseRoot + "T_VFX_Flare_666", AssetRequestMode.ImmediateLoad);
        }

        internal static void DrawSpearWake(Vector2 center, float rotation, Vector2 size, float opacity, bool empowered)
        {
            Vector2 restrainedSize = new(
                Math.Min(size.X, empowered ? 62f : 54f),
                Math.Min(size.Y, empowered ? 16f : 12f));
            DrawCinderArc(center, rotation, restrainedSize,
                Math.Min(opacity, empowered ? 0.48f : 0.38f), empowered, 0.32f);
        }

        internal static void DrawGroundWave(Vector2 center, Vector2 velocity, Vector2 size, float opacity)
        {
            Vector2 direction = velocity.SafeNormalize(Vector2.UnitX);
            DrawCinderArc(center - direction * 5f, direction.ToRotation(),
                new Vector2(Math.Min(size.X, 58f), Math.Min(size.Y, 18f)),
                Math.Min(opacity * 0.55f, 0.5f), empowered: false, progress: 0.48f);
        }

        internal static void DrawStandardCharge(Vector2 groundPoint, float progress, KnightStandardMode mode)
        {
            float length = MathHelper.Lerp(26f, mode == KnightStandardMode.RedKnight ? 48f : 58f, progress);
            float opacity = MathHelper.Lerp(0.1f, mode == KnightStandardMode.RedKnight ? 0.3f : 0.38f, progress);

            if (mode != KnightStandardMode.GreatRight)
            {
                Vector2 right = Vector2.UnitX;
                DrawCinderArc(groundPoint + new Vector2(length * 0.32f, -6f), right.ToRotation(),
                    new Vector2(length, 14f), opacity, mode != KnightStandardMode.RedKnight, progress);
            }
            if (mode != KnightStandardMode.GreatLeft)
            {
                Vector2 left = -Vector2.UnitX;
                DrawCinderArc(groundPoint + new Vector2(-length * 0.32f, -6f), left.ToRotation(),
                    new Vector2(length, 14f), opacity, mode != KnightStandardMode.RedKnight, progress);
            }
        }

        /// <summary>Draw and emit a compact physical fuse at the supplied fuse point. No radial telegraph is implied.</summary>
        internal static void DrawBombFuse(Vector2 fusePoint, float progress, bool planted)
        {
            LoadAssets();
            if (Main.dedServ || flare == null)
            {
                return;
            }

            float pulse = 0.78f + 0.22f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 18f);
            float scale = (planted ? 0.055f : 0.045f) + progress * 0.018f;
            Texture2D texture = flare.Value;
            Main.EntitySpriteDraw(texture, fusePoint - Main.screenPosition, null,
                new Color(255, 104, 24) * (0.52f + progress * 0.3f) * pulse,
                Main.GlobalTimeWrappedHourly * 1.8f, texture.Size() * 0.5f,
                scale, SpriteEffects.None, 0f);

            if (Main.rand.NextBool(planted ? 2 : 3))
            {
                Dust spark = Dust.NewDustPerfect(fusePoint + Main.rand.NextVector2Circular(2f, 2f),
                    DustID.Torch, new Vector2(Main.rand.NextFloat(-0.35f, 0.35f), Main.rand.NextFloat(-1.2f, -0.55f)),
                    80, new Color(255, 105, 24), Main.rand.NextFloat(0.65f, 0.95f));
                spark.noGravity = true;
            }
            if (Main.rand.NextBool(10))
            {
                Dust smoke = Dust.NewDustPerfect(fusePoint, DustID.Smoke,
                    new Vector2(Main.rand.NextFloat(-0.2f, 0.2f), -0.45f), 140, default,
                    Main.rand.NextFloat(0.45f, 0.7f));
                smoke.noGravity = true;
            }
        }

        internal static void DrawLightningLane(Vector2 start, Vector2 velocity, float length,
            float progress, bool active, float fade, float activeWidth = 15f)
        {
            Vector2 direction = velocity.SafeNormalize(Vector2.UnitX);
            DrawJudgmentLane(start, direction, length, active ? activeWidth : 8f,
                progress, active, fade);
        }

        internal static void DrawCrimsonDominion(Vector2 center, int age, float baseRotation, int rotationDirection)
        {
            const float arenaRadius = 420f;
            float boundaryRadius = arenaRadius;
            float boundaryOpacity;
            bool ringActive = age >= 450 && age < 490;

            if (age < 30)
            {
                boundaryOpacity = age / 30f * 0.3f;
            }
            else if (age < 390)
            {
                boundaryOpacity = 0.3f;
            }
            else if (age < 450)
            {
                float collapse = (age - 390f) / 60f;
                boundaryRadius = MathHelper.Lerp(arenaRadius, 70f, collapse);
                boundaryOpacity = MathHelper.Lerp(0.32f, 0.48f, collapse);
            }
            else if (age < 490)
            {
                float expansion = (age - 450f) / 40f;
                boundaryRadius = MathHelper.Lerp(70f, arenaRadius, expansion);
                boundaryOpacity = 0.72f;
            }
            else
            {
                boundaryOpacity = MathHelper.Clamp(1f - (age - 490f) / 50f, 0f, 1f) * 0.22f;
            }

            DrawRadialBand(center, boundaryRadius, ringActive ? 8f : 4f,
                ringActive ? 28f : 16f, boundaryOpacity,
                GreatCinder, GreatFlame, GreatCore);
            DrawDominionTicks(center, arenaRadius, baseRotation, rotationDirection, age);

            int judgmentAge = age - 30;
            if (judgmentAge < 0 || judgmentAge >= 360)
            {
                return;
            }

            int beat = judgmentAge / 60;
            int phase = judgmentAge % 60;
            float angle = baseRotation + rotationDirection * beat * MathHelper.Pi / 6f;
            float chargeProgress = MathHelper.Clamp(phase / 45f, 0f, 1f);
            bool active = phase >= 45 && phase < 55;
            float fade = phase < 55 ? 1f : 1f - (phase - 55f) / 5f;
            DrawDominionPair(center, angle, arenaRadius, chargeProgress, active, fade);

            if (beat < 5)
            {
                float nextAngle = baseRotation + rotationDirection * (beat + 1) * MathHelper.Pi / 6f;
                DrawDominionPair(center, nextAngle, arenaRadius, phase / 60f, active: false, 0.16f);
            }
        }

        static void DrawDominionPair(Vector2 center, float angle, float radius,
            float progress, bool active, float opacityMultiplier)
        {
            Vector2 direction = angle.ToRotationVector2();
            const float innerRadius = 62f;
            float length = radius - innerRadius;
            for (int sign = -1; sign <= 1; sign += 2)
            {
                Vector2 signedDirection = direction * sign;
                DrawLightningLane(center + signedDirection * innerRadius, signedDirection,
                    length, progress, active, opacityMultiplier, 18f);
            }
        }

        static void DrawDominionTicks(Vector2 center, float radius, float baseRotation,
            int rotationDirection, int age)
        {
            LoadAssets();
            if (Main.dedServ || flare == null)
            {
                return;
            }

            Texture2D texture = flare.Value;
            float fade = age < 30 ? age / 30f
                : age >= 490 ? MathHelper.Clamp(1f - (age - 490f) / 50f, 0f, 1f) : 1f;
            for (int i = 0; i < 12; i++)
            {
                float angle = baseRotation + rotationDirection * i * MathHelper.Pi / 6f;
                Vector2 position = center + angle.ToRotationVector2() * radius - Main.screenPosition;
                Main.EntitySpriteDraw(texture, position, null,
                    new Color(205, 28, 35) * (0.16f * fade), angle,
                    texture.Size() * 0.5f, new Vector2(0.055f, 0.12f), SpriteEffects.None, 0f);
            }
        }

        internal static void DrawToxicMotes(Vector2 center, int count, float progress, float radius)
        {
            LoadAssets();
            if (Main.dedServ || flare == null)
            {
                return;
            }

            Texture2D texture = flare.Value;
            float pulse = 0.7f + 0.3f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 7f);
            Main.EntitySpriteDraw(texture, center - Main.screenPosition, null,
                new Color(126, 210, 32) * (0.2f + progress * 0.18f) * pulse,
                0f, texture.Size() * 0.5f, 0.028f + progress * 0.008f,
                SpriteEffects.None, 0f);

            if (Main.rand.NextBool(3))
            {
                Dust mote = Dust.NewDustPerfect(center + Main.rand.NextVector2Circular(Math.Min(radius, 7f), Math.Min(radius, 7f)),
                    DustID.YellowTorch, Main.rand.NextVector2Circular(0.25f, 0.25f), 120,
                    new Color(150, 220, 35), Main.rand.NextFloat(0.65f, 0.9f));
                mote.noGravity = true;
            }
        }

        internal static void DrawUltrakillSeal(Vector2 center, float progress)
        {
            float radius = MathHelper.Lerp(130f, 32f, progress);
            float opacity = 0.1f + (float)Math.Sin(progress * MathHelper.Pi) * 0.28f;
            DrawRadialBand(center, radius, 4f, 16f, opacity,
                GreatCinder, GreatFlame, GreatCore);
        }

        internal static void DrawHerald(Vector2 center, float progress, bool storm)
        {
            float envelope = (float)Math.Sin(progress * MathHelper.Pi);
            float radius = MathHelper.Lerp(42f, 148f, MathHelper.Clamp(progress * 1.1f, 0f, 1f));
            DrawRadialBand(center, radius, 4f, 18f, envelope * 0.38f,
                storm ? StormCinder : GreatCinder,
                storm ? StormFlame : GreatFlame,
                storm ? StormCore : GreatCore);

            if (storm)
            {
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
                LoadAssets();
                if (!Main.dedServ && flare != null)
                {
                    Texture2D texture = flare.Value;
                    for (int i = 0; i < 3; i++)
                    {
                        float angle = MathHelper.TwoPi * i / 3f + progress * 0.7f;
                        Vector2 position = center + angle.ToRotationVector2()
                            * MathHelper.Lerp(38f, 82f, progress) - Main.screenPosition;
                        Main.EntitySpriteDraw(texture, position, null,
                            new Color(245, 61, 20) * (envelope * 0.26f), angle,
                            texture.Size() * 0.5f, 0.045f, SpriteEffects.None, 0f);
                    }
                }
            }
        }

        internal static void DrawBurst(RedKnightBurstKind kind, Vector2 center, float progress, float scale)
        {
            if (kind == RedKnightBurstKind.StandardImpact)
            {
                float envelope = (1f - progress) * 0.44f;
                float length = MathHelper.Lerp(18f, 52f, progress) * scale;
                DrawCinderArc(center + new Vector2(length * 0.28f, -5f), 0f,
                    new Vector2(length, 15f * scale), envelope, empowered: scale >= 1f, progress);
                DrawCinderArc(center + new Vector2(-length * 0.28f, -5f), MathHelper.Pi,
                    new Vector2(length, 15f * scale), envelope, empowered: scale >= 1f, progress);
                if (progress < 0.25f)
                {
                    DrawSmallFlare(center, CrimsonCore, (1f - progress / 0.25f) * 0.045f * scale);
                }
                return;
            }

            float targetRadius = kind switch
            {
                RedKnightBurstKind.BombExplosion => 60f,
                _ => 25f
            } * scale;
            float radius = MathHelper.Lerp(5f, targetRadius, MathHelper.SmoothStep(0f, 1f, progress));
            float opacity = (1f - progress) * (kind == RedKnightBurstKind.BombExplosion ? 0.72f : 0.52f);
            DrawRadialBand(center, radius, MathHelper.Lerp(8f, 3f, progress), 18f, opacity,
                CrimsonCinder, kind == RedKnightBurstKind.BombExplosion ? GreatFlame : CrimsonFlame,
                kind == RedKnightBurstKind.BombExplosion ? GreatCore : CrimsonCore);

            if (progress < 0.35f)
            {
                DrawSmallFlare(center, GreatCore, (1f - progress / 0.35f) * 0.07f * scale);
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

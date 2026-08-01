using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using Terraria;
using Terraria.ModLoader;
using tsorcRevamp.Projectiles.Enemy.Weapons;

namespace tsorcRevamp.Projectiles.Enemy
{
    internal static class RedKnightVFX
    {
        const string EffectPath = "tsorcRevamp/Effects/RedKnightVFX";
        const string NoiseRoot = "tsorcRevamp/Textures/Noise/";

        static Asset<Effect> effect;
        static Asset<Texture2D> circle;
        static Asset<Texture2D> windstreak;
        static Asset<Texture2D> trail;
        static Asset<Texture2D> brokenNoise;
        static Asset<Texture2D> smoothNoise;
        static Asset<Texture2D> flare;
        static Asset<Texture2D> gradient;

        static readonly Color CrimsonDark = new(28, 2, 6);
        static readonly Color CrimsonMid = new(196, 28, 35);
        static readonly Color CrimsonCore = new(255, 229, 205);
        static readonly Color GreatDark = new(14, 1, 5);
        static readonly Color GreatMid = new(230, 35, 48);
        static readonly Color GreatCore = new(255, 247, 229);
        static readonly Color StormDark = new(5, 13, 30);
        static readonly Color StormMid = new(58, 150, 235);
        static readonly Color StormCore = new(225, 252, 255);
        static readonly Color ToxicDark = new(10, 28, 4);
        static readonly Color ToxicMid = new(116, 205, 38);
        static readonly Color ToxicCore = new(234, 255, 135);

        static void LoadAssets()
        {
            if (Main.dedServ)
            {
                return;
            }
            effect ??= ModContent.Request<Effect>(EffectPath, AssetRequestMode.ImmediateLoad);
            circle ??= ModContent.Request<Texture2D>(NoiseRoot + "T_VFX_CircleFit1", AssetRequestMode.ImmediateLoad);
            windstreak ??= ModContent.Request<Texture2D>(NoiseRoot + "T_Windstreak3", AssetRequestMode.ImmediateLoad);
            trail ??= ModContent.Request<Texture2D>(NoiseRoot + "T_trail12", AssetRequestMode.ImmediateLoad);
            brokenNoise ??= ModContent.Request<Texture2D>(NoiseRoot + "T_VFX_Noise41", AssetRequestMode.ImmediateLoad);
            smoothNoise ??= ModContent.Request<Texture2D>(NoiseRoot + "T_VFX_NoiseF1", AssetRequestMode.ImmediateLoad);
            flare ??= ModContent.Request<Texture2D>(NoiseRoot + "T_VFX_Flare_666", AssetRequestMode.ImmediateLoad);
            gradient ??= ModContent.Request<Texture2D>(NoiseRoot + "T_Gradient_circle22", AssetRequestMode.ImmediateLoad);
        }

        internal static void DrawSpearWake(Vector2 center, float rotation, Vector2 size, float opacity, bool empowered)
        {
            LoadAssets();
            Draw("RedKnightStreak", windstreak, brokenNoise, center, size, rotation,
                empowered ? GreatDark : CrimsonDark, empowered ? GreatMid : CrimsonMid,
                empowered ? GreatCore : CrimsonCore, opacity, 0.5f, empowered ? 1f : 0.65f, 1f);
        }

        internal static void DrawGroundWave(Vector2 center, Vector2 velocity, Vector2 size, float opacity)
        {
            LoadAssets();
            Vector2 direction = velocity.SafeNormalize(Vector2.UnitX);
            Draw("RedKnightStreak", trail, brokenNoise, center - direction * 10f, size,
                direction.ToRotation(), CrimsonDark, CrimsonMid, CrimsonCore,
                opacity, 0.5f, 1f, Math.Sign(direction.X));
        }

        internal static void DrawStandardCharge(Vector2 groundPoint, float progress, bool empowered)
        {
            LoadAssets();
            float size = empowered ? 98f : 70f;
            Draw("RedKnightSeal", circle, brokenNoise, groundPoint - new Vector2(0f, 3f),
                new Vector2(size, size * 0.42f), 0f,
                empowered ? GreatDark : CrimsonDark, empowered ? GreatMid : CrimsonMid,
                empowered ? GreatCore : CrimsonCore, 0.35f + progress * 0.42f,
                progress, progress > 0.88f ? 1f : 0f, 1f);
        }

        internal static void DrawBombFuse(Vector2 center, float progress, bool planted)
        {
            LoadAssets();
            // The ring shader's bright band sits at 43% of the quad size: 140px gives a truthful 60px blast radius.
            float size = planted ? 140f : 44f;
            Draw("RedKnightRing", circle, brokenNoise, center,
                Vector2.One * size, 0f,
                CrimsonDark, new Color(238, 64, 25), new Color(255, 238, 172),
                0.25f + progress * 0.55f, progress, progress > 0.88f ? 1f : 0f, 1f);
        }

        internal static void DrawLightningLane(Vector2 start, Vector2 velocity, float length,
            float progress, bool active, float fade, float activeWidth = 15f)
        {
            LoadAssets();
            Vector2 direction = velocity.SafeNormalize(Vector2.UnitX);
            Draw("RedKnightStreak", trail, smoothNoise, start + direction * length * 0.5f,
                new Vector2(length, active ? activeWidth : 8f), direction.ToRotation(),
                StormDark, StormMid, StormCore, (active ? 0.95f : 0.22f + progress * 0.28f) * fade,
                progress, active ? 1f : 0f, Math.Sign(direction.X));
        }

        internal static void DrawCrimsonDominion(Vector2 center, int age, float baseRotation, int rotationDirection)
        {
            LoadAssets();
            const float radius = 420f;
            float circleOpacity;
            float circleSize = radius / 0.43f;

            if (age < 30)
            {
                circleOpacity = MathHelper.Clamp(age / 30f, 0f, 1f) * 0.55f;
            }
            else if (age < 390)
            {
                circleOpacity = 0.55f;
            }
            else if (age < 450)
            {
                float collapse = (age - 390f) / 60f;
                circleSize = MathHelper.Lerp(radius / 0.43f, 70f / 0.43f, collapse);
                circleOpacity = MathHelper.Lerp(0.62f, 0.85f, collapse);
            }
            else if (age < 490)
            {
                float expansion = (age - 450f) / 40f;
                circleSize = MathHelper.Lerp(70f / 0.43f, radius / 0.43f, expansion);
                circleOpacity = 1f;
            }
            else
            {
                circleOpacity = MathHelper.Clamp(1f - (age - 490f) / 50f, 0f, 1f) * 0.35f;
            }

            Draw("RedKnightRing", circle, brokenNoise, center, Vector2.One * circleSize, 0f,
                GreatDark, GreatMid, GreatCore, circleOpacity,
                age < 490 ? MathHelper.Clamp(age / 490f, 0f, 1f) : 1f,
                age >= 450 && age < 490 ? 1f : 0f, rotationDirection);

            DrawDominionTicks(center, radius, baseRotation, rotationDirection, age);

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
            DrawDominionPair(center, angle, radius, chargeProgress, active, fade);

            int nextBeat = Math.Min(5, beat + 1);
            if (nextBeat != beat)
            {
                float nextAngle = baseRotation + rotationDirection * nextBeat * MathHelper.Pi / 6f;
                DrawDominionPair(center, nextAngle, radius, phase / 60f, false, 0.2f);
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
                Vector2 start = center + signedDirection * innerRadius;
                DrawLightningLane(start, signedDirection, length, progress, active, opacityMultiplier, 22f);
            }
        }

        static void DrawDominionTicks(Vector2 center, float radius, float baseRotation,
            int rotationDirection, int age)
        {
            if (Main.dedServ || flare == null)
            {
                return;
            }
            Texture2D texture = flare.Value;
            float fade = age < 30 ? age / 30f : age >= 490 ? MathHelper.Clamp(1f - (age - 490f) / 50f, 0f, 1f) : 1f;
            for (int i = 0; i < 12; i++)
            {
                float angle = baseRotation + rotationDirection * i * MathHelper.Pi / 6f;
                Vector2 direction = angle.ToRotationVector2();
                Vector2 position = center + direction * radius - Main.screenPosition;
                Main.EntitySpriteDraw(texture, position, null, new Color(220, 35, 45) * (0.32f * fade),
                    angle, texture.Size() * 0.5f, new Vector2(0.16f, 0.34f), SpriteEffects.None, 0f);
            }
        }

        internal static void DrawUnblockableSpearBands(Vector2 handWorld, float rotation,
            float progress, float scale)
        {
            LoadAssets();
            if (Main.dedServ || flare == null)
            {
                return;
            }
            Texture2D texture = flare.Value;
            Vector2 forward = (rotation - MathHelper.PiOver2).ToRotationVector2();
            for (int i = 0; i < 4; i++)
            {
                float lane = i / 3f;
                float collapse = MathHelper.Lerp(42f - i * 9f, 48f, progress);
                Vector2 position = handWorld + forward * collapse - Main.screenPosition;
                float pulse = 0.6f + 0.4f * (float)Math.Sin((Main.GlobalTimeWrappedHourly * 7f) + i * 0.8f);
                Main.EntitySpriteDraw(texture, position, null,
                    new Color(255, 25, 35) * (0.4f + progress * 0.45f) * pulse,
                    rotation, texture.Size() * 0.5f,
                    new Vector2((0.12f + lane * 0.025f) * scale, 0.26f * scale), SpriteEffects.None, 0f);
            }
        }

        internal static void DrawToxicMotes(Vector2 center, int count, float progress, float radius)
        {
            LoadAssets();
            if (Main.dedServ || gradient == null)
            {
                return;
            }
            Texture2D texture = gradient.Value;
            for (int i = 0; i < count; i++)
            {
                float angle = Main.GlobalTimeWrappedHourly * 2.2f + MathHelper.TwoPi * i / count;
                Vector2 offset = angle.ToRotationVector2() * MathHelper.Lerp(radius * 0.45f, radius, progress);
                Main.EntitySpriteDraw(texture, center + offset - Main.screenPosition, null,
                    ToxicCore * (0.5f + progress * 0.35f), 0f, texture.Size() * 0.5f,
                    0.12f + progress * 0.04f, SpriteEffects.None, 0f);
            }
        }

        internal static void DrawUltrakillSeal(Vector2 center, float progress)
        {
            LoadAssets();
            float size = MathHelper.Lerp(480f, 92f, progress);
            Draw("RedKnightSeal", circle, brokenNoise, center, Vector2.One * size, 0f,
                GreatDark, GreatMid, GreatCore, 0.65f, progress, progress > 0.9f ? 1f : 0f, 1f);
        }

        internal static void DrawHerald(Vector2 center, float progress, bool storm)
        {
            LoadAssets();
            float size = MathHelper.Lerp(110f, 360f, MathHelper.Clamp(progress * 1.15f, 0f, 1f));
            Draw("RedKnightRing", circle, storm ? smoothNoise : brokenNoise, center, Vector2.One * size, 0f,
                storm ? StormDark : GreatDark, storm ? StormMid : GreatMid,
                storm ? StormCore : GreatCore, (float)Math.Sin(progress * MathHelper.Pi) * 0.72f,
                progress, progress > 0.55f ? 1f : 0f, storm ? -1f : 1f);

            float motifOpacity = (float)Math.Sin(progress * MathHelper.Pi) * 0.32f;
            if (storm)
            {
                for (int i = 0; i < 3; i++)
                {
                    Vector2 direction = (MathHelper.TwoPi * i / 3f - MathHelper.PiOver2
                        - progress * 0.45f).ToRotationVector2();
                    DrawLightningLane(center + direction * 54f, direction, 125f,
                        progress, active: false, motifOpacity);
                }
            }
            else
            {
                for (int i = 0; i < 3; i++)
                {
                    float angle = MathHelper.TwoPi * i / 3f + progress * 0.7f;
                    DrawBombFuse(center + angle.ToRotationVector2() * MathHelper.Lerp(48f, 92f, progress),
                        progress, planted: false);
                }
            }
        }

        internal static void DrawBurst(RedKnightBurstKind kind, Vector2 center, float progress, float scale)
        {
            LoadAssets();
            Vector2 size = kind switch
            {
                RedKnightBurstKind.BombExplosion => Vector2.One * 140f * scale,
                RedKnightBurstKind.StandardImpact => new Vector2(86f, 44f) * scale,
                _ => Vector2.One * 78f * scale
            };
            Draw("RedKnightRing", circle, brokenNoise, center, size, 0f,
                CrimsonDark, kind == RedKnightBurstKind.BombExplosion ? new Color(241, 68, 22) : CrimsonMid,
                CrimsonCore, (1f - progress) * 0.9f, progress, 1f, 1f);
        }

        static void Draw(string techniqueName, Asset<Texture2D> primaryAsset, Asset<Texture2D> detailAsset,
            Vector2 worldCenter, Vector2 drawSize, float rotation,
            Color darkColor, Color midColor, Color coreColor,
            float opacity, float progress, float active, float direction)
        {
            if (Main.dedServ || effect == null || primaryAsset == null || detailAsset == null || opacity <= 0f)
            {
                return;
            }

            Texture2D primaryTexture = primaryAsset.Value;
            Texture2D detailTexture = detailAsset.Value;
            int sourceWidth = Math.Clamp((int)drawSize.X, 1, primaryTexture.Width);
            int sourceHeight = Math.Clamp((int)drawSize.Y, 1, primaryTexture.Height);
            Rectangle source = new(0, 0, sourceWidth, sourceHeight);
            Vector2 actualSize = source.Size();
            Vector2 scale = drawSize / actualSize;

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearWrap,
                DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            GraphicsDevice graphicsDevice = Main.instance.GraphicsDevice;
            Texture previousTexture = graphicsDevice.Textures[1];
            SamplerState previousSampler = graphicsDevice.SamplerStates[1];
            Effect shader = effect.Value;
            try
            {
                graphicsDevice.Textures[1] = detailTexture;
                graphicsDevice.SamplerStates[1] = SamplerState.LinearWrap;
                shader.CurrentTechnique = shader.Techniques[techniqueName];
                shader.Parameters["DarkColor"]?.SetValue(darkColor.ToVector3());
                shader.Parameters["MidColor"]?.SetValue(midColor.ToVector3());
                shader.Parameters["CoreColor"]?.SetValue(coreColor.ToVector3());
                shader.Parameters["Opacity"]?.SetValue(opacity);
                shader.Parameters["Time"]?.SetValue(Main.GlobalTimeWrappedHourly);
                shader.Parameters["Progress"]?.SetValue(progress);
                shader.Parameters["Active"]?.SetValue(active);
                shader.Parameters["Direction"]?.SetValue(direction);
                shader.Parameters["DrawSize"]?.SetValue(actualSize);
                shader.Parameters["PrimaryTextureSize"]?.SetValue(primaryTexture.Size());
                shader.CurrentTechnique.Passes[0].Apply();

                Main.EntitySpriteDraw(primaryTexture, worldCenter - Main.screenPosition, source, Color.White,
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

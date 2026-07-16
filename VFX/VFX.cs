using Microsoft.Xna.Framework;
using System;
using Terraria;

namespace tsorcRevamp.VFX
{
    /// <summary>
    /// High-level, client-only effect emitters. Attacks should call these on every peer at deterministic moments;
    /// the damaging hitbox remains a separately synchronized projectile or NPC.
    /// </summary>
    public static class VFX
    {
        public static void Strip(Vector2[] points, float[] widths, Color startColor, Color endColor,
            VFXMaterial material = VFXMaterial.Solid, int lifetime = 2, float uvScale = 1f,
            float scrollSpeed = 0f, float wobble = 0f, float wobbleSpeed = 0f)
        {
            VFXSystem.AddStrip(points, widths, startColor, endColor, material, lifetime, uvScale,
                scrollSpeed, wobble, wobbleSpeed);
        }

        public static void Beam(Vector2 start, Vector2 end, float width, Color outerColor, Color coreColor,
            int lifetime = 2, float turbulence = 8f)
        {
            const int pointCount = 10;
            Vector2[] points = BuildLine(start, end, pointCount);
            float[] auraWidths = ConstantWidths(pointCount, width * 1.4f);
            float[] bodyWidths = ConstantWidths(pointCount, width);
            float[] coreWidths = ConstantWidths(pointCount, Math.Max(3f, width * 0.24f));

            Strip(points, auraWidths, outerColor * 0.28f, outerColor * 0.08f, VFXMaterial.Wavy,
                lifetime, 2.5f, 0.012f, turbulence, 0.14f);
            Strip(points, bodyWidths, outerColor * 0.8f, outerColor * 0.45f, VFXMaterial.Turbulent,
                lifetime, 3.5f, -0.018f, turbulence * 0.45f, 0.2f);
            Strip(points, coreWidths, coreColor, coreColor * 0.8f, VFXMaterial.Solid, lifetime);

            Burst(end, outerColor, 10, 4f, VFXParticleTexture.Spark, VFXBlend.Additive);
        }

        public static void FlamePillar(Vector2 basePosition, float height, float width, Color flameColor,
            Color coreColor, int lifetime = 90)
        {
            const int pointCount = 14;
            Vector2[] points = new Vector2[pointCount];
            float[] auraWidths = new float[pointCount];
            float[] bodyWidths = new float[pointCount];
            float[] coreWidths = new float[pointCount];

            for (int i = 0; i < pointCount; i++)
            {
                float progress = i / (float)(pointCount - 1);
                points[i] = basePosition - Vector2.UnitY * height * progress;
                float taper = (float)Math.Pow(1f - progress, 0.38f);
                float pulse = 0.86f + 0.14f * (float)Math.Sin(progress * MathHelper.TwoPi * 3f);
                auraWidths[i] = Math.Max(2f, width * 1.55f * taper * pulse);
                bodyWidths[i] = Math.Max(2f, width * taper * pulse);
                coreWidths[i] = Math.Max(1f, width * 0.30f * taper);
            }

            Strip(points, auraWidths, flameColor * 0.22f, flameColor * 0.04f, VFXMaterial.Wavy,
                lifetime, 4f, 0.018f, width * 0.08f, 0.17f);
            Strip(points, bodyWidths, flameColor * 0.95f, flameColor * 0.35f, VFXMaterial.Turbulent,
                lifetime, 5f, -0.025f, width * 0.045f, 0.24f);
            Strip(points, coreWidths, coreColor, coreColor * 0.55f, VFXMaterial.Solid, lifetime);

            for (int i = 0; i < 34; i++)
            {
                Vector2 position = basePosition + new Vector2(Main.rand.NextFloat(-width * 0.5f, width * 0.5f), Main.rand.NextFloat(-30f, 15f));
                VFXSystem.AddParticle(new VFXParticleData
                {
                    Position = position,
                    Velocity = new Vector2(Main.rand.NextFloat(-1.5f, 1.5f), Main.rand.NextFloat(-8f, -3f)),
                    Acceleration = new Vector2(0f, -0.03f),
                    StartColor = coreColor,
                    EndColor = flameColor * 0.2f,
                    StartScale = Main.rand.NextFloat(0.25f, 0.65f),
                    EndScale = 0.05f,
                    Rotation = Main.rand.NextFloat(MathHelper.TwoPi),
                    AngularVelocity = Main.rand.NextFloat(-0.08f, 0.08f),
                    Damping = 0.985f,
                    Lifetime = Main.rand.Next(35, 70),
                    Texture = VFXParticleTexture.Spark,
                    Blend = VFXBlend.Additive
                });
            }
        }

        public static void Ring(Vector2 center, float startRadius, float endRadius, float width,
            Color color, int lifetime = 45, VFXMaterial material = VFXMaterial.Wavy)
        {
            const int pointCount = 65;
            Vector2[] points = new Vector2[pointCount];
            float[] widths = ConstantWidths(pointCount, width);
            float radius = endRadius;
            for (int i = 0; i < pointCount; i++)
            {
                float progress = i / (float)(pointCount - 1);
                float angle = progress * MathHelper.TwoPi;
                points[i] = center + new Vector2(radius, 0f).RotatedBy(angle);
            }
            Strip(points, widths, color, color, material, lifetime, 5f, 0.015f, 2f, 0.12f);

            if (Math.Abs(endRadius - startRadius) > 1f)
            {
                // A second, shorter-lived ring at the start radius makes the intended expansion direction readable.
                Vector2[] originPoints = new Vector2[pointCount];
                for (int i = 0; i < pointCount; i++)
                {
                    float angle = i / (float)(pointCount - 1) * MathHelper.TwoPi;
                    originPoints[i] = center + new Vector2(startRadius, 0f).RotatedBy(angle);
                }
                Strip(originPoints, ConstantWidths(pointCount, Math.Max(1f, width * 0.35f)), color * 0.35f,
                    color * 0.35f, VFXMaterial.Solid, Math.Max(8, lifetime / 3));
            }
        }

        public static void SmokeVent(Vector2 position, Color smokeColor, int count = 8,
            float spread = 28f, float riseSpeed = 3f)
        {
            for (int i = 0; i < count; i++)
            {
                VFXSystem.AddParticle(new VFXParticleData
                {
                    Position = position + Main.rand.NextVector2Circular(spread, spread * 0.35f),
                    Velocity = new Vector2(Main.rand.NextFloat(-0.8f, 0.8f), Main.rand.NextFloat(-riseSpeed * 1.25f, -riseSpeed * 0.65f)),
                    Acceleration = new Vector2(Main.rand.NextFloat(-0.006f, 0.006f), -0.002f),
                    StartColor = smokeColor * Main.rand.NextFloat(0.45f, 0.75f),
                    EndColor = Color.Black * 0.05f,
                    StartScale = Main.rand.NextFloat(0.35f, 0.65f),
                    EndScale = Main.rand.NextFloat(1.1f, 1.9f),
                    Rotation = Main.rand.NextFloat(MathHelper.TwoPi),
                    AngularVelocity = Main.rand.NextFloat(-0.01f, 0.01f),
                    Damping = 0.992f,
                    Lifetime = Main.rand.Next(90, 160),
                    Texture = Main.rand.NextBool() ? VFXParticleTexture.SmokeA : VFXParticleTexture.SmokeB,
                    Blend = VFXBlend.Alpha
                });
            }
        }

        public static void GroundEruption(Vector2 position, Color earthColor, Color energyColor,
            float radius = 90f, int shardCount = 28)
        {
            Ring(position, radius * 0.25f, radius, 14f, energyColor, 34, VFXMaterial.Turbulent);
            Burst(position, energyColor, shardCount, 8f, VFXParticleTexture.Spark, VFXBlend.Additive);
            SmokeVent(position, earthColor, Math.Max(5, shardCount / 3), radius * 0.35f, 4.5f);
        }

        public static void SoulSpiral(Vector2 center, Color color, int count = 18,
            float radius = 150f, int lifetime = 90)
        {
            for (int i = 0; i < count; i++)
            {
                float angle = i / (float)count * MathHelper.TwoPi;
                VFXSystem.AddParticle(new VFXParticleData
                {
                    Position = center + new Vector2(radius, 0f).RotatedBy(angle),
                    Velocity = new Vector2(0f, -0.25f),
                    StartColor = color,
                    EndColor = Color.White * 0.15f,
                    StartScale = 0.7f,
                    EndScale = 0.12f,
                    Rotation = angle,
                    AngularVelocity = 0.05f,
                    Damping = 1f,
                    Lifetime = lifetime + Main.rand.Next(-12, 13),
                    Texture = VFXParticleTexture.Spark,
                    Blend = VFXBlend.Additive,
                    Motion = VFXParticleMotion.Spiral,
                    OrbitCenter = center,
                    OrbitRadius = radius,
                    OrbitAngle = angle,
                    OrbitAngularVelocity = 0.035f + Main.rand.NextFloat(-0.006f, 0.006f),
                    OrbitRadiusVelocity = -radius / Math.Max(1, lifetime)
                });
            }
        }

        public static void WorldspineArc(Vector2 start, Vector2 end, float arcHeight, float width,
            Color boneColor, Color soulColor, int lifetime = 120)
        {
            const int pointCount = 24;
            Vector2[] points = new Vector2[pointCount];
            float[] auraWidths = new float[pointCount];
            float[] bodyWidths = new float[pointCount];
            for (int i = 0; i < pointCount; i++)
            {
                float progress = i / (float)(pointCount - 1);
                Vector2 point = Vector2.Lerp(start, end, progress);
                point.Y -= (float)Math.Sin(progress * MathHelper.Pi) * arcHeight;
                points[i] = point;
                float taper = 0.45f + 0.55f * (float)Math.Sin(progress * MathHelper.Pi);
                auraWidths[i] = width * 1.45f * taper;
                bodyWidths[i] = width * taper;
            }
            Strip(points, auraWidths, soulColor * 0.32f, soulColor * 0.08f, VFXMaterial.Wavy,
                lifetime, 5f, 0.012f, 6f, 0.1f);
            Strip(points, bodyWidths, boneColor, boneColor * 0.5f, VFXMaterial.Splotchy,
                lifetime, 3f, -0.008f, 2.5f, 0.13f);
        }

        public static void Burst(Vector2 position, Color color, int count, float speed,
            VFXParticleTexture texture = VFXParticleTexture.Spark, VFXBlend blend = VFXBlend.Additive)
        {
            for (int i = 0; i < count; i++)
            {
                Vector2 velocity = Main.rand.NextVector2CircularEdge(speed, speed) * Main.rand.NextFloat(0.45f, 1f);
                VFXSystem.AddParticle(new VFXParticleData
                {
                    Position = position,
                    Velocity = velocity,
                    Acceleration = new Vector2(0f, 0.025f),
                    StartColor = color,
                    EndColor = color * 0.05f,
                    StartScale = Main.rand.NextFloat(0.35f, 0.9f),
                    EndScale = 0.05f,
                    Rotation = velocity.ToRotation(),
                    AngularVelocity = Main.rand.NextFloat(-0.12f, 0.12f),
                    Damping = 0.975f,
                    Lifetime = Main.rand.Next(24, 52),
                    Texture = texture,
                    Blend = blend
                });
            }
        }

        private static Vector2[] BuildLine(Vector2 start, Vector2 end, int pointCount)
        {
            Vector2[] points = new Vector2[pointCount];
            for (int i = 0; i < pointCount; i++)
            {
                points[i] = Vector2.Lerp(start, end, i / (float)(pointCount - 1));
            }
            return points;
        }

        private static float[] ConstantWidths(int pointCount, float width)
        {
            float[] widths = new float[pointCount];
            for (int i = 0; i < pointCount; i++)
            {
                widths[i] = width;
            }
            return widths;
        }
    }
}

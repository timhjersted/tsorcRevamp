using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Projectiles.Enemy.Darkmoon
{
    public static class DarkmoonVisuals
    {
        static Asset<Effect> moon, explosion;
        static Asset<Texture2D> swirl, fractures, orb;
        public static void Motes(Vector2 position, Color color, int count, float radius)
        {
            if (Main.dedServ) return;
            for (int i = 0; i < count; i++)
            {
                Vector2 offset = Main.rand.NextVector2Circular(radius, radius);
                Dust d = Dust.NewDustPerfect(position + offset, DustID.AncientLight, offset.SafeNormalize(Vector2.UnitY) * Main.rand.NextFloat(0.5f, 3f), 100, color, Main.rand.NextFloat(0.45f, 0.85f));
                d.noGravity = true;
            }
        }
        public static void Line(Vector2 from, Vector2 to, Color color, float width)
        {
            if (Main.dedServ) return;
            Vector2 delta = to - from;
            float length = delta.Length();
            Vector2 direction = delta.SafeNormalize(Vector2.UnitX);
            for (float t = 0; t < length; t += 16)
                Utils.DrawLine(Main.spriteBatch, from + direction * t, from + direction * Math.Min(t + 7, length), color * (1 - t / Math.Max(1, length) * 0.65f), color * 0.2f, width);
        }
        public static void DrawHandOrb(Vector2 center, float amount, bool flying = false)
        {
            if (Main.dedServ) return;
            orb ??= ModContent.Request<Texture2D>("tsorcRevamp/Content/Projectiles/Enemy/ShadowShot");
            Texture2D tex = orb.Value;
            Rectangle frame = new Rectangle(0, (int)(Main.GameUpdateCount / 6 % 6) * 16, 16, 16);
            Main.EntitySpriteDraw(tex, center - Main.screenPosition, frame, Color.White * amount, 0, new Vector2(8), flying ? 1f : amount, SpriteEffects.None, 0);
        }
        public static void DrawMoon(Vector2 center, float growth, float opacity, float charge, int slot)
        {
            if (Main.dedServ || growth <= 0) return;
            moon ??= ModContent.Request<Effect>("tsorcRevamp/Effects/DarkmoonMoonlight", AssetRequestMode.ImmediateLoad);
            Draw(moon.Value, "DarkmoonMoonlight", center, new Vector2(160), 0, growth, opacity, charge, slot);
        }
        public static void DrawWarning(Vector2 center, float progress)
        {
            if (Main.dedServ) return;
            moon ??= ModContent.Request<Effect>("tsorcRevamp/Effects/DarkmoonMoonlight", AssetRequestMode.ImmediateLoad);
            Draw(moon.Value, "DarkmoonWarning", center, new Vector2(360), 0, progress, 0.6f, 0, 0);
        }
        /// <summary>
        /// One tick of a moonlight beam, drawn as three noise-textured VFX strips (wide soft glow, flowing body,
        /// white-hot core) from the tail up to the head along direction. Brightest and widest just behind the head,
        /// tapering to nothing at both ends, fading toward the tail. Call every tick from update code on clients
        /// (VFX strips added in update live exactly one frame at lifetime 2).
        /// </summary>
        public static void BeamStrip(Vector2 head, Vector2 direction, float length, float width, float opacity)
        {
            if (Main.dedServ || opacity <= 0.01f) return;
            const int PointCount = 12;
            Vector2[] points = new Vector2[PointCount];
            float[] glowWidths = new float[PointCount];
            float[] bodyWidths = new float[PointCount];
            float[] coreWidths = new float[PointCount];
            for (int i = 0; i < PointCount; i++)
            {
                // u: 0 at the tail, 1 at the head. The profile peaks at u = 0.8 and pinches to a point at the tip.
                float u = i / (float)(PointCount - 1);
                points[i] = head - direction * length * (1f - u);
                float rise = MathHelper.Clamp(u / 0.8f, 0f, 1f);
                float tipPinch = MathHelper.Clamp((1f - u) / 0.2f, 0f, 1f);
                float profile = (float)Math.Sqrt(rise) * (float)Math.Sqrt(tipPinch);
                glowWidths[i] = width * 2.2f * profile;
                bodyWidths[i] = width * profile;
                coreWidths[i] = Math.Max(1f, width * 0.32f * profile);
            }

            Color glowTail = new Color(60, 110, 200) * 0f;
            Color glowHead = new Color(120, 180, 255) * (0.45f * opacity);
            Color bodyTail = new Color(150, 200, 255) * (0.1f * opacity);
            Color bodyHead = new Color(190, 225, 255) * (0.9f * opacity);
            Color coreTail = Color.White * (0.15f * opacity);
            Color coreHead = Color.White * opacity;
            global::tsorcRevamp.VFX.VFX.Strip(points, glowWidths, glowTail, glowHead, global::tsorcRevamp.VFX.VFXMaterial.Wavy, 2, 2.5f, 0.03f, 1.5f, 0.2f);
            global::tsorcRevamp.VFX.VFX.Strip(points, bodyWidths, bodyTail, bodyHead, global::tsorcRevamp.VFX.VFXMaterial.Turbulent, 2, 3.5f, -0.05f);
            global::tsorcRevamp.VFX.VFX.Strip(points, coreWidths, coreTail, coreHead, global::tsorcRevamp.VFX.VFXMaterial.Solid, 2);
        }

        public static void DrawExplosion(Vector2 center, float progress, float arrivalAngle, float scale = 1f, float opacity = 1f)
        {
            if (Main.dedServ) return;
            explosion ??= ModContent.Request<Effect>("tsorcRevamp/Effects/DarkmoonExplosion", AssetRequestMode.ImmediateLoad);
            Draw(explosion.Value, "DarkmoonExplosion", center, new Vector2(512) * scale, arrivalAngle, progress, opacity, progress >= 0.1f && progress < 0.35f ? 1 : 0, 0);
        }

        /// <summary>
        /// A burst of white light: rayCount straight rays of white dust shot out radially (fast at the tip, slow at
        /// the root, so each reads as a streak out to about rayLength), a soft white dust cloud, and additive white
        /// VFX sparks. strength (0..1) scales counts, sizes and speeds.
        /// </summary>
        public static void LightBurst(Vector2 center, int rayCount, float rayLength, float strength)
        {
            if (Main.dedServ) return;
            float spin = Main.rand.NextFloat(MathHelper.TwoPi);
            for (int ray = 0; ray < rayCount; ray++)
            {
                Vector2 direction = (spin + MathHelper.TwoPi * ray / rayCount).ToRotationVector2();
                for (int i = 0; i < 6; i++)
                {
                    float along = (i + 1) / 6f;
                    Vector2 velocity = direction * rayLength / 14f * along;
                    float size = (1.5f - along * 0.5f) * strength;
                    Dust light = Dust.NewDustPerfect(center + direction * 6f, DustID.AncientLight, velocity, 60, Color.White, size);
                    light.noGravity = true;
                }
            }

            int cloudCount = (int)(30 * strength);
            for (int i = 0; i < cloudCount; i++)
            {
                Vector2 position = center + Main.rand.NextVector2Circular(24f, 24f);
                Vector2 velocity = Main.rand.NextVector2Circular(5f, 5f) * strength;
                Dust cloud = Dust.NewDustPerfect(position, DustID.AncientLight, velocity, 100, Color.White, Main.rand.NextFloat(0.9f, 1.6f) * strength);
                cloud.noGravity = true;
            }

            // Far-flung light: no-gravity white motes thrown well past the blast radius (visual only - dust never
            // damages), slowing as they go so they read as sparks of light flying out.
            int farCount = (int)(36 * strength);
            for (int i = 0; i < farCount; i++)
            {
                Vector2 velocity = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(10f, 18f) * strength;
                Dust far = Dust.NewDustPerfect(center, DustID.AncientLight, velocity, 40, Color.White, Main.rand.NextFloat(1f, 1.6f));
                far.noGravity = true;
            }

            global::tsorcRevamp.VFX.VFX.Burst(center, Color.White, (int)(28 * strength), 9f * strength);
        }
        static void Draw(Effect effect, string technique, Vector2 center, Vector2 size, float rotation, float progress, float opacity, float active, float direction)
        {
            swirl ??= ModContent.Request<Texture2D>("tsorcRevamp/Textures/Noise/SwirlyNoise");
            fractures ??= ModContent.Request<Texture2D>("tsorcRevamp/Textures/Noise/VoronoiNoise");
            var gd = Main.instance.GraphicsDevice;
            Texture previousTexture = gd.Textures[1];
            SamplerState previousSampler = gd.SamplerStates[1];
            Main.spriteBatch.End();
            try
            {
                gd.Textures[1] = fractures.Value; gd.SamplerStates[1] = SamplerState.LinearWrap;
                effect.CurrentTechnique = effect.Techniques[technique];
                effect.Parameters["Time"]?.SetValue(Main.GlobalTimeWrappedHourly);
                effect.Parameters["Progress"]?.SetValue(progress); effect.Parameters["Opacity"]?.SetValue(opacity);
                effect.Parameters["Active"]?.SetValue(active); effect.Parameters["Direction"]?.SetValue(direction);
                effect.Parameters["Growth"]?.SetValue(MathHelper.Clamp(progress * 10f, 0, 1));
                effect.Parameters["Fade"]?.SetValue(MathHelper.Clamp((1 - progress) * 1.54f, 0, 1));
                effect.Parameters["PixelGrid"]?.SetValue(new Vector4(size.X / 2, size.Y / 2, 2 / size.X, 2 / size.Y));
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearWrap,
                    DepthStencilState.None, RasterizerState.CullNone, effect, Main.GameViewMatrix.TransformationMatrix);
                Main.spriteBatch.Draw(swirl.Value, center - Main.screenPosition, null, Color.White, rotation, swirl.Value.Size() * 0.5f, size / swirl.Value.Size(), SpriteEffects.None, 0);
                Main.spriteBatch.End();
            }
            finally
            {
                gd.Textures[1] = previousTexture; gd.SamplerStates[1] = previousSampler;
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
                    DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.GameViewMatrix.TransformationMatrix);
            }
        }
    }
}

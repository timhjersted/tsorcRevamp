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
        static Asset<Effect> moon, sliver, explosion;
        static Asset<Texture2D> swirl, fractures, orb, crescent;
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
        public static void DrawCrescent(Vector2 center, float rotation, float opacity)
        {
            if (Main.dedServ) return;
            crescent ??= ModContent.Request<Texture2D>("tsorcRevamp/Content/Projectiles/Melee/MLGSCrescent");
            Main.EntitySpriteDraw(crescent.Value, center - Main.screenPosition, null, new Color(190, 150, 255) * opacity, rotation, crescent.Value.Size() * 0.5f, 0.7f, SpriteEffects.None, 0);
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
        public static void DrawSliver(Vector2 center, float rotation, float opacity)
        {
            if (Main.dedServ) return;
            sliver ??= ModContent.Request<Effect>("tsorcRevamp/Effects/DarkmoonSliver", AssetRequestMode.ImmediateLoad);
            Draw(sliver.Value, "DarkmoonSliver", center, new Vector2(128, 32), rotation, 0, opacity, 1, 0);
        }
        public static void DrawExplosion(Vector2 center, float progress, float arrivalAngle)
        {
            if (Main.dedServ) return;
            explosion ??= ModContent.Request<Effect>("tsorcRevamp/Effects/DarkmoonExplosion", AssetRequestMode.ImmediateLoad);
            Draw(explosion.Value, "DarkmoonExplosion", center, new Vector2(512), arrivalAngle, progress, 1, progress >= 0.1f && progress < 0.35f ? 1 : 0, 0);
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

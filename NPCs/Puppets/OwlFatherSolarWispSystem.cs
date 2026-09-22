using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.NPCs.Puppets
{
    /// <summary>
    /// Client-only solar flame particles for Owl Father's phase two. Each particle uses an organic
    /// flame alpha mask as geometry, with separately scrolling flow and erosion textures inside the
    /// pixel shader. They are decorative and intentionally extend beyond the NPC and axe hitboxes.
    /// </summary>
    [Autoload(Side = ModSide.Client)]
    public sealed class OwlFatherSolarWispSystem : ModSystem
    {
        private const int WispLimit = 96;

        private sealed class SolarWisp
        {
            public Vector2 Position;
            public Vector2 Velocity;
            public Vector2 StartSize;
            public Vector2 EndSize;
            public float Rotation;
            public float AngularVelocity;
            public float Seed;
            public int Age;
            public int Lifetime;
            public int Variant;

            public bool Update()
            {
                Age++;
                if (Age >= Lifetime)
                    return false;

                float progress = Age / (float)Math.Max(1, Lifetime);
                Velocity += new Vector2(0f, -0.012f);
                Velocity *= 0.986f;
                Position += Velocity;
                Position.X += (float)Math.Sin(Seed + Age * 0.17f) * (0.10f + progress * 0.12f);
                Rotation += AngularVelocity;
                return true;
            }
        }

        private static readonly List<SolarWisp> Wisps = new List<SolarWisp>(WispLimit);
        private static Asset<Effect> solarWispEffect;
        private static Asset<Texture2D> flowNoise;
        private static Asset<Texture2D> erosionNoise;
        private static readonly Asset<Texture2D>[] flameMasks = new Asset<Texture2D>[3];

        public override void Load()
        {
            if (Main.dedServ)
                return;

            solarWispEffect = ModContent.Request<Effect>(
                "tsorcRevamp/Effects/OwlFatherSolarWisp", AssetRequestMode.ImmediateLoad);
            flowNoise = ModContent.Request<Texture2D>(
                "tsorcRevamp/Textures/Noise/SmoothNoise", AssetRequestMode.ImmediateLoad);
            erosionNoise = ModContent.Request<Texture2D>(
                "tsorcRevamp/Textures/Noise/Turbulence_05-512x512", AssetRequestMode.ImmediateLoad);
            flameMasks[0] = ModContent.Request<Texture2D>(
                "tsorcRevamp/Textures/Particles/flame_01_a", AssetRequestMode.ImmediateLoad);
            flameMasks[1] = ModContent.Request<Texture2D>(
                "tsorcRevamp/Textures/Particles/flame_02_a", AssetRequestMode.ImmediateLoad);
            flameMasks[2] = ModContent.Request<Texture2D>(
                "tsorcRevamp/Textures/Particles/flame_03_a", AssetRequestMode.ImmediateLoad);
        }

        public override void Unload()
        {
            Wisps.Clear();
            solarWispEffect = null;
            flowNoise = null;
            erosionNoise = null;
            for (int i = 0; i < flameMasks.Length; i++)
                flameMasks[i] = null;
        }

        internal static void Spawn(Vector2 position, Vector2 velocity,
            Vector2 startSize, Vector2 endSize, float rotation, float angularVelocity,
            int lifetime, int variant)
        {
            if (Main.dedServ || lifetime <= 0 || Wisps.Count >= WispLimit)
                return;

            Wisps.Add(new SolarWisp
            {
                Position = position,
                Velocity = velocity,
                StartSize = Vector2.Max(startSize, Vector2.One * 4f),
                EndSize = Vector2.Max(endSize, Vector2.One * 4f),
                Rotation = rotation,
                AngularVelocity = angularVelocity,
                Seed = Main.rand.NextFloat(0f, 20f),
                Lifetime = lifetime,
                Variant = Math.Clamp(variant, 0, flameMasks.Length - 1),
            });
        }

        public override void PostUpdateEverything()
        {
            if (Main.dedServ || Main.gamePaused)
                return;

            for (int i = Wisps.Count - 1; i >= 0; i--)
            {
                if (!Wisps[i].Update())
                    Wisps.RemoveAt(i);
            }
        }

        public override void PostDrawTiles()
        {
            if (Main.dedServ || Wisps.Count == 0 || solarWispEffect?.Value == null)
                return;

            Effect effect = solarWispEffect.Value;
            GraphicsDevice device = Main.instance.GraphicsDevice;
            Texture previousTexture1 = device.Textures[1];
            Texture previousTexture2 = device.Textures[2];
            SamplerState previousSampler1 = device.SamplerStates[1];
            SamplerState previousSampler2 = device.SamplerStates[2];
            bool batchBegun = false;

            try
            {
                device.Textures[1] = flowNoise.Value;
                device.Textures[2] = erosionNoise.Value;
                device.SamplerStates[1] = SamplerState.LinearWrap;
                device.SamplerStates[2] = SamplerState.LinearWrap;

                effect.CurrentTechnique = effect.Techniques["OwlFatherSolarWisp"];
                effect.Parameters["DarkColor"]?.SetValue(new Color(132, 24, 2).ToVector3());
                effect.Parameters["MidColor"]?.SetValue(new Color(255, 105, 8).ToVector3());
                effect.Parameters["CoreColor"]?.SetValue(new Color(255, 222, 72).ToVector3());

                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend,
                    SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone,
                    effect, Main.GameViewMatrix.TransformationMatrix);
                batchBegun = true;

                foreach (SolarWisp wisp in Wisps)
                {
                    float progress = wisp.Age / (float)Math.Max(1, wisp.Lifetime);
                    float fadeIn = Utils.GetLerpValue(0f, 0.14f, progress, true);
                    float fadeOut = Utils.GetLerpValue(1f, 0.56f, progress, true);
                    float opacity = fadeIn * fadeOut * 0.78f;
                    Vector2 sizeProgress = Vector2.Lerp(wisp.StartSize, wisp.EndSize,
                        progress * (2f - progress));
                    Texture2D texture = flameMasks[wisp.Variant].Value;

                    effect.Parameters["Opacity"]?.SetValue(opacity);
                    effect.Parameters["Time"]?.SetValue(Main.GlobalTimeWrappedHourly);
                    effect.Parameters["Progress"]?.SetValue(progress);
                    effect.Parameters["Seed"]?.SetValue(wisp.Seed);
                    effect.CurrentTechnique.Passes[0].Apply();

                    Main.spriteBatch.Draw(texture, wisp.Position - Main.screenPosition, null,
                        Color.White, wisp.Rotation, texture.Size() * 0.5f,
                        sizeProgress / texture.Size(), SpriteEffects.None, 0f);
                }
            }
            finally
            {
                if (batchBegun)
                    Main.spriteBatch.End();
                device.Textures[1] = previousTexture1;
                device.Textures[2] = previousTexture2;
                device.SamplerStates[1] = previousSampler1;
                device.SamplerStates[2] = previousSampler2;
            }
        }
    }
}

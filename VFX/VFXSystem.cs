using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace tsorcRevamp.VFX
{
    public enum VFXMaterial : byte
    {
        Solid,
        Turbulent,
        Wavy,
        Splotchy
    }

    public enum VFXParticleTexture : byte
    {
        Spark,
        SmokeA,
        SmokeB,
        BeamCap
    }

    public enum VFXBlend : byte
    {
        Alpha,
        Additive
    }

    public enum VFXParticleMotion : byte
    {
        Linear,
        Spiral
    }

    /// <summary>
    /// Lightweight, client-only particle description. Gameplay collision must live on an NPC or projectile;
    /// particles are intentionally visual-only and are never synchronized over the network.
    /// </summary>
    public struct VFXParticleData
    {
        public Vector2 Position;
        public Vector2 Velocity;
        public Vector2 Acceleration;
        public Color StartColor;
        public Color EndColor;
        public float StartScale;
        public float EndScale;
        public float Rotation;
        public float AngularVelocity;
        public float Damping;
        public int Lifetime;
        public VFXParticleTexture Texture;
        public VFXBlend Blend;
        public VFXParticleMotion Motion;
        public Vector2 OrbitCenter;
        public float OrbitRadius;
        public float OrbitAngle;
        public float OrbitAngularVelocity;
        public float OrbitRadiusVelocity;
    }

    internal sealed class VFXParticleInstance
    {
        public VFXParticleData Data;
        public int Age;

        public bool Update()
        {
            Age++;
            if (Age >= Data.Lifetime)
            {
                return false;
            }

            if (Data.Motion == VFXParticleMotion.Spiral)
            {
                Data.OrbitAngle += Data.OrbitAngularVelocity;
                Data.OrbitRadius += Data.OrbitRadiusVelocity;
                Data.OrbitCenter += Data.Velocity;
                Data.Position = Data.OrbitCenter + new Vector2(Data.OrbitRadius, 0f).RotatedBy(Data.OrbitAngle);
            }
            else
            {
                Data.Velocity += Data.Acceleration;
                Data.Velocity *= Data.Damping;
                Data.Position += Data.Velocity;
            }

            Data.Rotation += Data.AngularVelocity;
            return true;
        }
    }

    internal sealed class VFXStripInstance
    {
        public Vector2[] Points;
        public float[] Widths;
        public Color StartColor;
        public Color EndColor;
        public VFXMaterial Material;
        public int Lifetime;
        public int Age;
        public float UVScale;
        public float ScrollSpeed;
        public float Wobble;
        public float WobbleSpeed;
        public float Seed;

        public bool Update()
        {
            Age++;
            return Age < Lifetime;
        }

        public float Opacity
        {
            get
            {
                float progress = Age / (float)Math.Max(1, Lifetime);
                float fadeIn = Utils.GetLerpValue(0f, 0.08f, progress, true);
                float fadeOut = Utils.GetLerpValue(1f, 0.72f, progress, true);
                return fadeIn * fadeOut;
            }
        }
    }

    /// <summary>
    /// Shared client-side VFX renderer. It batches every tsorc VFX strip into one primitive pass, followed by
    /// one alpha-particle pass and one additive-particle pass. Existing projectile renderers are unaffected.
    /// </summary>
    public sealed class VFXSystem : ModSystem
    {
        private const int ParticleLimit = 2400;
        private const int StripLimit = 256;

        private static readonly List<VFXParticleInstance> Particles = new List<VFXParticleInstance>(ParticleLimit);
        private static readonly List<VFXStripInstance> Strips = new List<VFXStripInstance>(StripLimit);

        private BasicEffect stripEffect;
        private Texture2D sparkTexture;
        private Texture2D smokeTextureA;
        private Texture2D smokeTextureB;
        private Texture2D beamCapTexture;

        public override void Load()
        {
            if (Main.dedServ)
            {
                return;
            }

            sparkTexture = ModContent.Request<Texture2D>("tsorcRevamp/Projectiles/VFX/DynamicTrail").Value;
            smokeTextureA = ModContent.Request<Texture2D>("tsorcRevamp/Textures/Clouds/Cloud_0").Value;
            smokeTextureB = ModContent.Request<Texture2D>("tsorcRevamp/Textures/Clouds/Cloud_3").Value;
            beamCapTexture = ModContent.Request<Texture2D>("tsorcRevamp/Projectiles/GenericLaser").Value;
        }

        public override void Unload()
        {
            Particles.Clear();
            Strips.Clear();
            BasicEffect effectToDispose = stripEffect;
            stripEffect = null;

            // Mod content unloads on a worker thread, while FNA requires graphics resources to be
            // disposed on the main thread. Capture this instance so a freshly loaded VFXSystem can
            // create its own effect without the queued cleanup touching it.
            if (effectToDispose != null && !effectToDispose.IsDisposed)
            {
                Main.QueueMainThreadAction(effectToDispose.Dispose);
            }

            sparkTexture = null;
            smokeTextureA = null;
            smokeTextureB = null;
            beamCapTexture = null;
        }

        internal static void AddParticle(VFXParticleData data)
        {
            if (Main.dedServ || data.Lifetime <= 0 || Particles.Count >= ParticleLimit)
            {
                return;
            }

            data.Damping = data.Damping <= 0f ? 1f : data.Damping;
            data.StartScale = Math.Max(0.01f, data.StartScale);
            data.EndScale = Math.Max(0.01f, data.EndScale);
            Particles.Add(new VFXParticleInstance { Data = data });
        }

        internal static void AddStrip(Vector2[] points, float[] widths, Color startColor, Color endColor,
            VFXMaterial material, int lifetime, float uvScale, float scrollSpeed, float wobble, float wobbleSpeed)
        {
            if (Main.dedServ || points == null || widths == null || points.Length < 2 || points.Length != widths.Length
                || lifetime <= 0 || Strips.Count >= StripLimit)
            {
                return;
            }

            Strips.Add(new VFXStripInstance
            {
                Points = points,
                Widths = widths,
                StartColor = startColor,
                EndColor = endColor,
                Material = material,
                Lifetime = lifetime,
                UVScale = Math.Max(0.01f, uvScale),
                ScrollSpeed = scrollSpeed,
                Wobble = wobble,
                WobbleSpeed = wobbleSpeed,
                Seed = Main.rand.NextFloat(MathHelper.TwoPi)
            });
        }

        public override void PostUpdateEverything()
        {
            if (Main.dedServ || Main.gamePaused)
            {
                return;
            }

            for (int i = Particles.Count - 1; i >= 0; i--)
            {
                if (!Particles[i].Update())
                {
                    Particles.RemoveAt(i);
                }
            }

            for (int i = Strips.Count - 1; i >= 0; i--)
            {
                if (!Strips[i].Update())
                {
                    Strips.RemoveAt(i);
                }
            }
        }

        public override void PostDrawTiles()
        {
            if (Main.dedServ || (Particles.Count == 0 && Strips.Count == 0))
            {
                return;
            }

            DrawStrips();
            DrawParticles(VFXBlend.Alpha, BlendState.AlphaBlend);
            DrawParticles(VFXBlend.Additive, BlendState.Additive);
        }

        private void EnsureStripEffect(GraphicsDevice device)
        {
            if (stripEffect != null && !stripEffect.IsDisposed)
            {
                return;
            }

            stripEffect = new BasicEffect(device)
            {
                VertexColorEnabled = true,
                TextureEnabled = true,
                FogEnabled = false,
                LightingEnabled = false
            };
        }

        private void DrawStrips()
        {
            if (Strips.Count == 0)
            {
                return;
            }

            GraphicsDevice device = Main.instance.GraphicsDevice;
            EnsureStripEffect(device);

            BlendState oldBlend = device.BlendState;
            DepthStencilState oldDepth = device.DepthStencilState;
            RasterizerState oldRasterizer = device.RasterizerState;
            SamplerState oldSampler = device.SamplerStates[0];

            device.BlendState = BlendState.Additive;
            device.DepthStencilState = DepthStencilState.None;
            device.RasterizerState = RasterizerState.CullNone;

            stripEffect.World = Matrix.CreateTranslation(-Main.screenPosition.X, -Main.screenPosition.Y, 0f);
            stripEffect.View = Main.GameViewMatrix.TransformationMatrix;
            Viewport viewport = device.Viewport;
            stripEffect.Projection = Matrix.CreateOrthographicOffCenter(0f, viewport.Width, viewport.Height, 0f, -1f, 1f);

            foreach (VFXStripInstance strip in Strips)
            {
                DrawStrip(device, strip);
            }

            device.BlendState = oldBlend;
            device.DepthStencilState = oldDepth;
            device.RasterizerState = oldRasterizer;
            device.SamplerStates[0] = oldSampler;
        }

        private void DrawStrip(GraphicsDevice device, VFXStripInstance strip)
        {
            int count = Math.Min(strip.Points.Length, 128);
            if (count < 2)
            {
                return;
            }

            VertexPositionColorTexture[] vertices = new VertexPositionColorTexture[count * 2];
            short[] indices = new short[(count - 1) * 6];
            float time = strip.Age * strip.WobbleSpeed + strip.Seed;
            float opacity = strip.Opacity;

            for (int i = 0; i < count; i++)
            {
                float progress = i / (float)(count - 1);
                Vector2 tangent;
                if (i == 0)
                {
                    tangent = strip.Points[1] - strip.Points[0];
                }
                else if (i == count - 1)
                {
                    tangent = strip.Points[count - 1] - strip.Points[count - 2];
                }
                else
                {
                    tangent = strip.Points[i + 1] - strip.Points[i - 1];
                }

                if (tangent.LengthSquared() < 0.001f)
                {
                    tangent = Vector2.UnitY;
                }
                tangent.Normalize();
                Vector2 normal = new Vector2(-tangent.Y, tangent.X);
                float wave = (float)Math.Sin(time + progress * MathHelper.TwoPi * 2f) * strip.Wobble;
                Vector2 center = strip.Points[i] + normal * wave;
                float halfWidth = Math.Max(0.5f, strip.Widths[i] * 0.5f);
                Color color = Color.Lerp(strip.StartColor, strip.EndColor, progress) * opacity;
                float u = progress * strip.UVScale + strip.Age * strip.ScrollSpeed;

                vertices[i * 2] = new VertexPositionColorTexture(new Vector3(center - normal * halfWidth, 0f), color, new Vector2(u, 0f));
                vertices[i * 2 + 1] = new VertexPositionColorTexture(new Vector3(center + normal * halfWidth, 0f), color, new Vector2(u, 1f));
            }

            for (int i = 0; i < count - 1; i++)
            {
                int vertex = i * 2;
                int index = i * 6;
                indices[index] = (short)vertex;
                indices[index + 1] = (short)(vertex + 1);
                indices[index + 2] = (short)(vertex + 2);
                indices[index + 3] = (short)(vertex + 2);
                indices[index + 4] = (short)(vertex + 1);
                indices[index + 5] = (short)(vertex + 3);
            }

            stripEffect.Texture = GetMaterialTexture(strip.Material);
            device.SamplerStates[0] = strip.Material == VFXMaterial.Solid ? SamplerState.LinearClamp : SamplerState.LinearWrap;
            foreach (EffectPass pass in stripEffect.CurrentTechnique.Passes)
            {
                pass.Apply();
                device.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, vertices, 0, vertices.Length,
                    indices, 0, indices.Length / 3);
            }
        }

        private static Texture2D GetMaterialTexture(VFXMaterial material)
        {
            return material switch
            {
                VFXMaterial.Turbulent => tsorcRevamp.NoiseTurbulent,
                VFXMaterial.Wavy => tsorcRevamp.NoiseWavy,
                VFXMaterial.Splotchy => tsorcRevamp.NoiseSplotchy,
                _ => TextureAssets.MagicPixel.Value,
            };
        }

        private void DrawParticles(VFXBlend blend, BlendState blendState)
        {
            bool any = false;
            for (int i = 0; i < Particles.Count; i++)
            {
                if (Particles[i].Data.Blend == blend)
                {
                    any = true;
                    break;
                }
            }
            if (!any)
            {
                return;
            }

            Main.spriteBatch.Begin(SpriteSortMode.Deferred, blendState, SamplerState.LinearClamp,
                DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            foreach (VFXParticleInstance particle in Particles)
            {
                if (particle.Data.Blend != blend)
                {
                    continue;
                }

                float progress = particle.Age / (float)Math.Max(1, particle.Data.Lifetime);
                float opacity = Utils.GetLerpValue(1f, 0.68f, progress, true);
                float scale = MathHelper.Lerp(particle.Data.StartScale, particle.Data.EndScale, progress);
                Color color = Color.Lerp(particle.Data.StartColor, particle.Data.EndColor, progress) * opacity;
                Texture2D texture = GetParticleTexture(particle.Data.Texture);
                Main.spriteBatch.Draw(texture, particle.Data.Position - Main.screenPosition, null, color,
                    particle.Data.Rotation, texture.Size() * 0.5f, scale, SpriteEffects.None, 0f);
            }

            Main.spriteBatch.End();
        }

        private Texture2D GetParticleTexture(VFXParticleTexture texture)
        {
            return texture switch
            {
                VFXParticleTexture.SmokeA => smokeTextureA,
                VFXParticleTexture.SmokeB => smokeTextureB,
                VFXParticleTexture.BeamCap => beamCapTexture,
                _ => sparkTexture,
            };
        }
    }
}

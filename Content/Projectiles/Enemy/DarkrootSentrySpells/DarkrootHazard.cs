using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.NPCs.Enemies.SuperHardMode;

namespace tsorcRevamp.Content.Projectiles.Enemy.DarkrootSentrySpells
{
    /// <summary>
    /// Shared ownership and age clock for the Darkroot Sentry's thorns, roots and vines.
    /// ai[0] = owner NPC index, ai[1] = owner encounter serial, ai[2] = age in ticks. All three are synced
    /// projectile fields, so a late joiner receives the same phase of the hazard as everyone else.
    /// A recycled NPC slot cannot adopt a stale hazard: the serial must match too.
    /// </summary>
    public abstract class DarkrootHazard : ModProjectile
    {
        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(InvisibleNothingProj));

        protected float Age
        {
            get => Projectile.ai[2];
            set => Projectile.ai[2] = value;
        }

        /// <summary>True while this hazard can still hurt a player. The owner keeps offense and guard off until
        /// every one of its hazards reports false (plus a grace period).</summary>
        public abstract bool IsLive { get; }

        /// <summary>Can hurt a player now or later (an unerupted root bud counts). Read by the owner's grace gate.</summary>
        public virtual bool Threatening => IsLive;

        /// <summary>Server-side cancellation: make the hazard harmless and let it fade. Never spawns damage.</summary>
        public abstract void Disarm();

        public bool OwnedBy(int npcIndex, int serial)
        {
            bool sameNpc = (int)Projectile.ai[0] == npcIndex;
            bool sameSerial = (int)Projectile.ai[1] == serial;
            return sameNpc && sameSerial;
        }

        public override bool? CanDamage() => IsLive;

        /// <summary>Server: disarm when the owner died, despawned or its slot now holds a different encounter.</summary>
        protected void DisarmIfOrphaned()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            int ownerIndex = (int)Projectile.ai[0];
            bool ownerValid = ownerIndex >= 0 && ownerIndex < Main.maxNPCs && Main.npc[ownerIndex].active;

            if (ownerValid && Main.npc[ownerIndex].ModNPC is DarkrootSentry sentry && sentry.EncounterSerial == (int)Projectile.ai[1])
            {
                return;
            }

            Disarm();
        }
    }

    /// <summary>One quad of procedural wood: world centre, size in pixels, rotation, the pivot as a 0..1 fraction of
    /// the quad, and (vine only) the arc-length range this segment covers.</summary>
    public struct WoodQuad
    {
        public Vector2 Center;
        public Vector2 Size;
        public float Rotation;
        public Vector2 Origin;
        public Vector2 SegmentRange;
        // Vine only: the whole vine's arc length in px, so the shader sizes bark detail in pixels. 0 elsewhere.
        public float ArcPixels;
    }

    /// <summary>
    /// Draws the DarkrootWood.fx material (Thorn / Root / Vine / Bud techniques). Geometry is always supplied by the
    /// caller from the same cached path its collision uses; the shader only paints bark, sap and growth inside it.
    /// Premultiplied AlphaBlend throughout (vfx-shader-tips §43): rgb is multiplied by coverage in the shader.
    /// </summary>
    public static class DarkrootWoodVFX
    {
        public static readonly Color Bark = new Color(74, 48, 30);
        public static readonly Color Moss = new Color(118, 168, 70);
        public static readonly Color Sap = new Color(226, 232, 190);

        static Asset<Effect> woodEffect;
        static Asset<Texture2D> grainNoise;
        static Asset<Texture2D> plateNoise;

        public static void Draw(string technique, Vector2 center, Vector2 size, float rotation, Vector2 origin,
            float progress, float active, float opacity)
        {
            WoodQuad quad = new WoodQuad { Center = center, Size = size, Rotation = rotation, Origin = origin, SegmentRange = new Vector2(0f, 1f) };
            Draw(technique, new[] { quad }, progress, active, opacity);
        }

        static readonly RasterizerState CullNoneScissor = new RasterizerState { CullMode = CullMode.None, ScissorTestEnable = true };

        /// <summary>Draws every quad with the same technique inside one Immediate batch. Per-quad uniforms
        /// (SegmentRange, PixelGrid) are set before each Draw; Immediate mode applies the effect per draw call.
        /// clipBelowWorldY: when set, nothing below that world Y is drawn (a scissor at its on-screen line), so a
        /// shape rising out of the floor never shows its buried part, even through gaps in the floor.</summary>
        public static void Draw(string technique, WoodQuad[] quads, float progress, float active, float opacity, float? clipBelowWorldY = null)
        {
            if (Main.dedServ || opacity <= 0f || quads.Length == 0)
            {
                return;
            }

            woodEffect ??= ModContent.Request<Effect>("tsorcRevamp/Effects/DarkrootWood", AssetRequestMode.ImmediateLoad);
            grainNoise ??= ModContent.Request<Texture2D>("tsorcRevamp/Textures/Noise/T_PerlinNoise_Tiled", AssetRequestMode.ImmediateLoad);
            plateNoise ??= ModContent.Request<Texture2D>("tsorcRevamp/Textures/Noise/Voronoi_10-512x512", AssetRequestMode.ImmediateLoad);

            Effect effect = woodEffect.Value;
            Texture2D grain = grainNoise.Value;
            GraphicsDevice device = Main.instance.GraphicsDevice;
            Texture previousTexture = device.Textures[1];
            SamplerState previousSampler = device.SamplerStates[1];

            RasterizerState rasterizer = RasterizerState.CullNone;
            Rectangle previousScissor = device.ScissorRectangle;
            if (clipBelowWorldY.HasValue)
            {
                // World Y -> back-buffer Y through the same matrix the batch draws with (zoom included).
                Vector2 clipLine = Vector2.Transform(new Vector2(0f, clipBelowWorldY.Value) - Main.screenPosition, Main.GameViewMatrix.TransformationMatrix);
                int visibleHeight = (int)MathHelper.Clamp(clipLine.Y, 0f, device.Viewport.Height);
                if (visibleHeight <= 0)
                {
                    return;
                }

                rasterizer = CullNoneScissor;
                device.ScissorRectangle = new Rectangle(0, 0, device.Viewport.Width, visibleHeight);
            }

            Main.spriteBatch.End();
            try
            {
                device.Textures[1] = plateNoise.Value;
                device.SamplerStates[1] = SamplerState.LinearWrap;
                effect.CurrentTechnique = effect.Techniques[technique];
                effect.Parameters["Time"]?.SetValue(Main.GlobalTimeWrappedHourly);
                effect.Parameters["Progress"]?.SetValue(MathHelper.Clamp(progress, 0f, 1f));
                effect.Parameters["Active"]?.SetValue(MathHelper.Clamp(active, 0f, 1f));
                effect.Parameters["Opacity"]?.SetValue(MathHelper.Clamp(opacity, 0f, 1f));
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearWrap,
                    DepthStencilState.None, rasterizer, effect, Main.GameViewMatrix.TransformationMatrix);

                foreach (WoodQuad quad in quads)
                {
                    // 2px blocks across the real on-screen quad, pre-divided here (ps_2_0 has no preshader).
                    // Clamped to 2 blocks so a tiny quad never collapses to one sample (vfx-shader-tips §51g).
                    float blocksX = Math.Max(2f, quad.Size.X / 2f);
                    float blocksY = Math.Max(2f, quad.Size.Y / 2f);
                    effect.Parameters["PixelGrid"]?.SetValue(new Vector4(blocksX, blocksY, 1f / blocksX, 1f / blocksY));
                    effect.Parameters["SegmentRange"]?.SetValue(quad.SegmentRange);
                    effect.Parameters["ArcPixels"]?.SetValue(quad.ArcPixels);

                    Vector2 scale = quad.Size / grain.Size();
                    Vector2 pivot = quad.Origin * grain.Size();
                    Main.spriteBatch.Draw(grain, quad.Center - Main.screenPosition, null, Color.White, quad.Rotation, pivot, scale, SpriteEffects.None, 0f);
                }

                Main.spriteBatch.End();
            }
            finally
            {
                device.Textures[1] = previousTexture;
                device.SamplerStates[1] = previousSampler;
                device.ScissorRectangle = previousScissor;
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
                    DepthStencilState.None, RasterizerState.CullCounterClockwise, null, Main.GameViewMatrix.TransformationMatrix);
            }
        }

        /// <summary>Dim dashed telegraph line (thorn fan rays, advance lane, lash corridor). 9px dashes every 16px,
        /// fading toward the far end so the near end reads as the origin.</summary>
        public static void DashedLine(Vector2 from, Vector2 to, Color color, float width)
        {
            if (Main.dedServ)
            {
                return;
            }

            Vector2 delta = to - from;
            float length = delta.Length();
            Vector2 direction = delta.SafeNormalize(Vector2.UnitX);

            for (float distance = 0f; distance < length; distance += 16f)
            {
                float dashEnd = Math.Min(distance + 9f, length);
                float farFade = 1f - distance / Math.Max(1f, length) * 0.6f;
                Utils.DrawLine(Main.spriteBatch, from + direction * distance, from + direction * dashEnd, color * farFade, color * (farFade * 0.6f), width);
            }
        }

        /// <summary>Leaf / sap / bark motes. Inward motion when <paramref name="inward"/> (gathering before a cast),
        /// outward otherwise (impacts, eruptions). Client only.</summary>
        public static void Motes(Vector2 center, int count, float radius, bool inward, int dustType = DustID.GrassBlades)
        {
            if (Main.dedServ)
            {
                return;
            }

            for (int i = 0; i < count; i++)
            {
                Vector2 offset = Main.rand.NextVector2Circular(radius, radius);
                Vector2 outward = offset.SafeNormalize(Vector2.UnitY) * Main.rand.NextFloat(0.6f, 2.4f);
                Vector2 velocity = inward ? -outward * 0.6f : outward;
                Dust dust = Dust.NewDustPerfect(center + offset, dustType, velocity, 80, default, Main.rand.NextFloat(0.7f, 1.1f));
                dust.noGravity = inward;
            }
        }
    }
}

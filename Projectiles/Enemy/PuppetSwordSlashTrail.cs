using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using tsorcRevamp.NPCs.Puppets;
using tsorcRevamp.Projectiles.VFX;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.Projectiles.Enemy
{
    /// <summary>
    /// Generic visual-only sword ribbon for any PuppetNPC melee swing, driven by the puppet's own
    /// live hand position / blade direction / reach via <see cref="PuppetNPC.TryGetMeleeSlashTrailPose"/>.
    /// Same shader and VertexStrip architecture as <see cref="ArtoriasSwordSlashTrail"/> — reuses
    /// ArtoriasSwordTrail.fx directly, since its palette is entirely uniform-driven (slashDark/
    /// slashCenter/slashEdge), so no new shader was needed — generalized off PuppetNPC instead of
    /// the concrete Artorias type so any puppet can opt in via HasSlashTrailVFX.
    /// </summary>
    public sealed class PuppetSwordSlashTrail : DynamicTrail
    {
        public override string Texture => "tsorcRevamp/Projectiles/VFX/DynamicTrail";

        private int _trackedSequence = -1;
        private bool _tracking;
        private float _lastAngle;
        private float _lastRadius;
        private float _halfWidth;
        private Vector2 _lastPivot;
        private Vector2 _lightingSample;
        private float _noiseOffset;
        private Color _dark;
        private Color _center;
        private Color _edge;

        private int SourceNPCIndex => (int)Projectile.ai[0];

        public override void SetDefaults()
        {
            base.SetDefaults();
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.hide = true;
            Projectile.netImportant = true;
            Projectile.timeLeft = 2;

            ScreenSpace = true;
            noFadeOut = true;
            trailCollision = false;
            normalCollision = false;
            trailPointLimit = 96;
            newPointDistance = 0f;
            fadeOut = 0f;

            if (!Main.dedServ)
            {
                customEffect = ModContent.Request<Effect>("tsorcRevamp/Effects/ArtoriasSwordTrail",
                    ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
            }
        }

        public override void DrawBehind(int index, List<int> behindNPCsAndTiles,
            List<int> behindNPCs, List<int> behindProjectiles,
            List<int> overPlayers, List<int> overWiresUI)
        {
            behindNPCs.Add(index);
        }

        public override void AI()
        {
            if (SourceNPCIndex < 0 || SourceNPCIndex >= Main.maxNPCs)
            {
                Projectile.Kill();
                return;
            }

            NPC source = Main.npc[SourceNPCIndex];
            if (!source.active || source.ModNPC is not PuppetNPC puppet)
            {
                Projectile.Kill();
                return;
            }

            Projectile.Center = source.Center;
            Projectile.timeLeft = 2;
            EnsureHistory();

            bool active = puppet.TryGetMeleeSlashTrailPose(
                out Vector2 pivot, out Vector2 direction, out float reach, out _, out int sequence,
                out _dark, out _center, out _edge);

            if (!active || direction.LengthSquared() < 0.001f || reach <= 1f)
            {
                FadeHistory();
                return;
            }

            direction.Normalize();
            float angle = direction.ToRotation();
            float centerRadius = reach * 0.64f;
            _halfWidth = reach * 0.36f;
            _lightingSample = pivot + direction * reach * 0.5f;

            if (!_tracking || _trackedSequence != sequence)
            {
                trailPositions.Clear();
                trailRotations.Clear();
                _trackedSequence = sequence;
                _tracking = true;
                _lastAngle = angle;
                _lastRadius = centerRadius;
                _lastPivot = pivot;
                fadeOut = 1f;
                AppendPoint(pivot, angle, centerRadius);
                AppendPoint(pivot, angle, centerRadius);
                return;
            }

            float delta = MathHelper.WrapAngle(angle - _lastAngle);
            float pivotTravel = Vector2.Distance(_lastPivot, pivot);
            if (Math.Abs(delta) > 0.001f || pivotTravel > 0.35f)
            {
                int subdivisions = Math.Clamp((int)Math.Ceiling(Math.Abs(delta) * 28f), 1, 14);
                for (int i = 1; i <= subdivisions; i++)
                {
                    float t = i / (float)subdivisions;
                    AppendPoint(Vector2.Lerp(_lastPivot, pivot, t),
                        _lastAngle + delta * t, MathHelper.Lerp(_lastRadius, centerRadius, t));
                }
            }

            while (trailPositions.Count > trailPointLimit)
            {
                trailPositions.RemoveAt(0);
                trailRotations.RemoveAt(0);
            }

            _lastAngle = angle;
            _lastRadius = centerRadius;
            _lastPivot = pivot;
            fadeOut = 1f;
        }

        private void EnsureHistory()
        {
            if (trailPositions != null && trailRotations != null)
                return;

            trailPositions = new List<Vector2>(trailPointLimit);
            trailRotations = new List<float>(trailPointLimit);
            initialized = true;
        }

        private void AppendPoint(Vector2 pivot, float angle, float radius)
        {
            trailPositions.Add(pivot + angle.ToRotationVector2() * radius - Main.screenPosition);
            // VertexStrip expands perpendicular to this value. A tangent rotation therefore makes
            // the ribbon's width radial: its bright outer edge ends at the authored blade reach.
            trailRotations.Add(angle + MathHelper.PiOver2);
        }

        private void FadeHistory()
        {
            _tracking = false;
            if (trailPositions.Count == 0)
            {
                fadeOut = 0f;
                return;
            }

            fadeOut = Math.Max(0f, fadeOut - 0.13f);
            if (fadeOut <= 0f)
            {
                trailPositions.Clear();
                trailRotations.Clear();
            }
        }

        public override float WidthFunction(float progress)
        {
            float historyTaper = MathHelper.SmoothStep(0f, 1f,
                MathHelper.Clamp(progress * 3.5f, 0f, 1f));
            return _halfWidth * historyTaper * (0.82f + progress * 0.18f);
        }

        public override Color ColorFunction(float progress)
            => Color.White * MathHelper.Clamp(fadeOut, 0f, 1f);

        public override void SetEffectParameters(Effect effect)
        {
            if (_noiseOffset == 0f)
                _noiseOffset = Main.rand.NextFloat(0.01f, 0.99f);

            Color worldLight = Lighting.GetColor(_lightingSample.ToTileCoordinates());
            Color readableLight = Color.Lerp(worldLight, Color.White, 0.42f);

            effect.Parameters["baseNoise"].SetValue(tsorcRevamp.NoiseSmooth);
            effect.Parameters["secondaryNoise"].SetValue(tsorcRevamp.NoiseWavy);
            effect.Parameters["baseNoiseUOffset"].SetValue(_noiseOffset);
            effect.Parameters["fadeOut"].SetValue(MathHelper.Clamp(fadeOut, 0f, 1f));
            effect.Parameters["time"].SetValue(Main.GlobalTimeWrappedHourly);
            effect.Parameters["slashDark"].SetValue(_dark.MultiplyRGB(readableLight).ToVector4());
            effect.Parameters["slashCenter"].SetValue(_center.MultiplyRGB(readableLight).ToVector4());
            effect.Parameters["slashEdge"].SetValue(_edge.MultiplyRGB(readableLight).ToVector4());
            effect.Parameters["WorldViewProjection"].SetValue(GetWorldViewProjectionMatrix());
        }
    }
}

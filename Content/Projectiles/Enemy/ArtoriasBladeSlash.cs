using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
using tsorcRevamp.Content.Projectiles.VFX;
using tsorcRevamp.NPCs.Bosses.SuperHardMode;

namespace tsorcRevamp.Content.Projectiles.Enemy
{
    /// <summary>
    /// Artorias's sword-swing trail: the player Artorias Greatsword's Slash.fx ribbon (same shader, smooth + voronoi
    /// noise, white core / DarkViolet edge, the same band proportions as a projectile-driven player Slash), but driven
    /// by an ArtoriasSwordsman's live hand and blade instead of a held item. One per sweep, visual only.
    /// ai[0] = NPC index, ai[1] = sweep length in ticks (new points are added this long), ai[2] = tick the fade starts.
    /// Spawned server-side; every client tracks the blade itself, so nothing but the ai[] needs to sync.
    /// </summary>
    class ArtoriasBladeSlash : DynamicTrail
    {
        // Ticks the finished ribbon lingers after the sweep while Slash.fx eats it from the old end.
        const int TailTicks = 4;
        // The player's driven Slash puts its points at 0.75 x blade length with a half-width of a third of that, so
        // the glow spans 0.5-1.0 of the blade and its outer edge sits on the tip. Same proportions here.
        const float BandCentreFraction = 0.75f;
        const float BandHalfWidthFraction = 1f / 3f;
        // Up to 14 sub-points per tick, ~28 per radian, so a 44 deg/t peak cut still draws a smooth curve.
        const float SubdivisionsPerRadian = 28f;
        const int MaxSubdivisions = 14;
        // Artorias's arena is dark; pull the world light 42% toward white so the ribbon stays readable (PuppetSwordSlashTrail does the same).
        const float ReadableLightLerp = 0.42f;

        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(DynamicTrail));

        bool _started;
        int _age;
        float _halfWidth;
        float _lastAngle;
        Vector2 _lastPivot;
        Vector2 _lastBodyCenter;
        bool _hasLastPose;
        Vector2 _lightingSample;
        float _intensity = 1f;
        float _noiseOffset;

        public override void SetDefaults()
        {
            Projectile.width = 1;
            Projectile.height = 1;
            Projectile.tileCollide = false;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.damage = 0;
            Projectile.penetrate = -1;
            Projectile.hide = true;
            Projectile.timeLeft = 2;

            trailPointLimit = 400;
            trailCollision = false;
            normalCollision = false;
            noFadeOut = true;
            // The sub-points are a few px apart by construction; the base 60px "teleported" check would only misfire.
            noDiscontinuityCheck = true;
            // World space, carried with the body below, so camera movement can't separate the ribbon from the blade.
            ScreenSpace = false;
            newPointDistance = 0f;

            if (!Main.dedServ)
            {
                customEffect = ModContent.Request<Effect>("tsorcRevamp/Effects/Slash", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
            }
        }

        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs,
            List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
        {
            behindNPCs.Add(index);
        }

        public override void AI()
        {
            int sweepTicks = Math.Max(1, (int)Projectile.ai[1]);
            int lifeTicks = sweepTicks + TailTicks;

            if (!_started)
            {
                _started = true;
                Projectile.timeLeft = lifeTicks;
                trailPositions = new List<Vector2>();
                trailRotations = new List<float>();
                initialized = true;
            }

            _age++;

            int npcIndex = (int)Projectile.ai[0];
            if (npcIndex < 0 || npcIndex >= Main.maxNPCs)
            {
                Projectile.Kill();
                return;
            }

            NPC source = Main.npc[npcIndex];
            if (!source.active || source.ModNPC is not ArtoriasSwordsman swordsman)
            {
                Projectile.Kill();
                return;
            }

            Projectile.Center = source.Center;

            if (Main.dedServ)
            {
                return;
            }

            // Fade: full strength until ai[2], then linear to 0 at the end of the tail. Slash.fx reads this as fadeOut,
            // which both dims the ribbon and dissolves it from its oldest end.
            int fadeStartTick = Math.Clamp((int)Projectile.ai[2], 0, lifeTicks - 1);
            _intensity = 1f;
            if (_age > fadeStartTick)
            {
                _intensity = 1f - (_age - fadeStartTick) / (float)(lifeTicks - fadeStartTick);
                _intensity = MathHelper.Clamp(_intensity, 0f, 1f);
            }

            // Carry every stored point with the body, so a swing made mid-dash or mid-leap stays on the sword
            // instead of smearing out behind it (the player Slash does the same with its owner's centre).
            if (_hasLastPose)
            {
                Vector2 bodyMovement = source.Center - _lastBodyCenter;
                for (int i = 0; i < trailPositions.Count; i++)
                {
                    trailPositions[i] += bodyMovement;
                }

                _lastPivot += bodyMovement;
            }

            _lastBodyCenter = source.Center;

            bool sweepOver = _age > sweepTicks;
            if (sweepOver)
            {
                trailCurrentLength = CalculateLength();
                return;
            }

            swordsman.GetBladeSlashPose(out Vector2 pivot, out Vector2 direction, out float bladeReach);
            float angle = direction.ToRotation();
            float radius = bladeReach * BandCentreFraction;
            _halfWidth = radius * BandHalfWidthFraction;
            _lightingSample = pivot + direction * radius;

            if (!_hasLastPose)
            {
                AppendPoint(pivot, angle, radius);
                _hasLastPose = true;
            }
            else
            {
                // Sub-step from last tick's pose to this one so fast cuts stay curved instead of drawing straight chords.
                float angleDelta = MathHelper.WrapAngle(angle - _lastAngle);
                int subdivisions = (int)Math.Ceiling(Math.Abs(angleDelta) * SubdivisionsPerRadian);
                subdivisions = Math.Clamp(subdivisions, 1, MaxSubdivisions);

                for (int i = 1; i <= subdivisions; i++)
                {
                    float step = i / (float)subdivisions;
                    Vector2 stepPivot = Vector2.Lerp(_lastPivot, pivot, step);
                    float stepAngle = _lastAngle + angleDelta * step;
                    AppendPoint(stepPivot, stepAngle, radius);
                }
            }

            while (trailPositions.Count > trailPointLimit)
            {
                trailPositions.RemoveAt(0);
                trailRotations.RemoveAt(0);
            }

            _lastAngle = angle;
            _lastPivot = pivot;
            trailCurrentLength = CalculateLength();
        }

        void AppendPoint(Vector2 pivot, float angle, float radius)
        {
            trailPositions.Add(pivot + angle.ToRotationVector2() * radius);
            // Same strip orientation the player Slash stores (point angle + 3pi/2): VertexStrip widens perpendicular to
            // this, so the band is radial, and Slash.fx's asymmetric uv.y falloff lands on the same side as the player's.
            trailRotations.Add(angle - MathHelper.PiOver2);
        }

        public override float WidthFunction(float progress)
        {
            return _halfWidth;
        }

        public override void SetEffectParameters(Effect effect)
        {
            if (_noiseOffset == 0f)
            {
                _noiseOffset = Main.rand.NextFloat(0.01f, 0.99f);
            }

            // Mirrors Slash.SetEffectParameters for the Artorias Greatsword (slashStyle Metal = voronoi secondary noise).
            effect.Parameters["baseNoise"].SetValue(tsorcRevamp.NoiseSmooth);
            effect.Parameters["baseNoiseUOffset"].SetValue(_noiseOffset);
            effect.Parameters["secondaryNoise"].SetValue(tsorcRevamp.NoiseVoronoi);

            const float pixelBlockSize = 2f;
            Vector2 pixelGridSize = new Vector2(
                Math.Max(trailCurrentLength / pixelBlockSize, 1f),
                Math.Max(_halfWidth * 2f / pixelBlockSize, 1f));
            effect.Parameters["PixelGrid"].SetValue(new Vector4(
                pixelGridSize.X,
                pixelGridSize.Y,
                1f / pixelGridSize.X,
                1f / pixelGridSize.Y));

            Color worldLight = Lighting.GetColor(_lightingSample.ToTileCoordinates());
            Color readableLight = Color.Lerp(worldLight, Color.White, ReadableLightLerp);

            effect.Parameters["fadeOut"].SetValue(_intensity);
            effect.Parameters["time"].SetValue(Main.GlobalTimeWrappedHourly);
            effect.Parameters["slashCenter"].SetValue(Color.White.MultiplyRGB(readableLight).ToVector4());
            effect.Parameters["slashEdge"].SetValue(Color.DarkViolet.MultiplyRGB(readableLight).ToVector4());
            effect.Parameters["WorldViewProjection"].SetValue(GetWorldViewProjectionMatrix());
        }
    }
}

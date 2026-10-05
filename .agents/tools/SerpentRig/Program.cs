using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Terraria;
using tsorcRevamp.NPCs;
using SD = System.Drawing;

namespace SerpentRig
{
    /// <summary>
    /// Offline Great Serpent chain rig. Drives the chain with scripted head paths (the head is a given, exactly like in
    /// the game where locomotion owns it) and runs EITHER the old rate-limited-rotation body model ("legacy", ported
    /// verbatim from the pre-2026-10-04 RunBodyFollow + ApplyGroundSnap) or the live SerpentChain solver ("new") over the
    /// same path, then measures bend, clipping and wiggle and writes side-by-side renders.
    ///
    ///   dotnet run --project .agents/tools/SerpentRig -- [scenario|all] [--out DIR]
    ///
    /// Everything marked LEGACY or MIRROR is a hand copy of game code that cannot be called headless (it needs NPC /
    /// Main.tile); keep those in step with SerpentAI.cs when it changes. The solver itself is never copied.
    /// </summary>
    static class Program
    {
        const int TileSize = 16;
        const float LegacyTurnRate = 0.05f;
        const int FrontFreeSegments = 14; // GreatSerpentHead.FrontFreeSegmentCount: body index >= this is ground-hugging
        const int BodySegments = 48;      // GreatSerpentHead.BodySegmentCount
        const int WarmupTicks = 160;
        static float Flexibility = 1f; // the head's flexibility multiplier (relaxed 1.35, lunge 0.7); --flex sets it

        sealed class Spec
        {
            public float Width;
            public float Height;
        }

        sealed class HeadState
        {
            public Vector2 Center;
            public float? HeadingOverride;
        }

        sealed class Terrain
        {
            public bool[,] Solid;
            public int Columns;
            public int Rows;

            public bool IsSolid(int tileX, int tileY)
            {
                if (tileX < 0 || tileY < 0 || tileX >= Columns || tileY >= Rows)
                {
                    return false;
                }

                return Solid[tileX, tileY];
            }

            // MIRROR of SerpentAI.IsBodyCenterInsideTerrain: a small box at the piece centre against the tile grid.
            public bool BoxBlocked(Vector2 center, int size)
            {
                int minX = (int)Math.Floor((center.X - size / 2f) / TileSize);
                int maxX = (int)Math.Floor((center.X + size / 2f - 0.001f) / TileSize);
                int minY = (int)Math.Floor((center.Y - size / 2f) / TileSize);
                int maxY = (int)Math.Floor((center.Y + size / 2f - 0.001f) / TileSize);

                for (int x = minX; x <= maxX; x++)
                {
                    for (int y = minY; y <= maxY; y++)
                    {
                        if (IsSolid(x, y))
                        {
                            return true;
                        }
                    }
                }

                return false;
            }

            // MIRROR of RunBodyFollow's ground lookup (FindGroundSurfaceTileYSmoothed: deepest of three columns,
            // searching 8 tiles down from 4 above the piece's bottom). NaN = nothing in reach.
            public float GroundCenterY(Vector2 center, float height)
            {
                int centerTileX = (int)(center.X / TileSize);
                int bottomTileY = (int)((center.Y - height / 2f + height) / TileSize);
                int startY = bottomTileY - 4;
                int deepest = -1;

                for (int column = centerTileX - 1; column <= centerTileX + 1; column++)
                {
                    for (int depth = 0; depth <= 8; depth++)
                    {
                        if (IsSolid(column, startY + depth))
                        {
                            deepest = Math.Max(deepest, startY + depth);
                            break;
                        }
                    }
                }

                if (deepest < 0)
                {
                    return float.NaN;
                }

                return deepest * TileSize - height * 0.5f;
            }
        }

        sealed class Scenario
        {
            public string Name;
            public string Description;
            public Terrain Terrain;
            public List<HeadState> Head = new List<HeadState>();
        }

        sealed class Run
        {
            public string Model;
            public List<Vector2[]> Centers = new List<Vector2[]>();
            public List<float[]> Headings = new List<float[]>();
            public Metrics Metrics;
        }

        sealed class Metrics
        {
            public float MaxBendDeg;
            public float P99BendDeg;
            public int BackwardBendFrames;
            public int SelfOverlapFrames;
            public int ClipPieceTicks;
            public float MaxHeadingStepRad;
            public int WiggleMax;
            public float MaxPieceStep;
            public string MaxPieceStepAt = "";
            public float MaxSpacingError;
            public float MaxSpriteSkewDeg;
        }

        static Spec[] BuildSpecs()
        {
            List<Spec> specs = new List<Spec>();
            specs.Add(new Spec { Width = 50, Height = 64 }); // head

            // Mirror of SerpentAI.BuildBodyTypes: mostly fat Body, Body2 x2, Body3 x2 near the tail.
            for (int i = 0; i < BodySegments; i++)
            {
                if (i >= BodySegments - 2)
                {
                    specs.Add(new Spec { Width = 18, Height = 44 });
                }
                else if (i >= BodySegments - 4)
                {
                    specs.Add(new Spec { Width = 22, Height = 44 });
                }
                else
                {
                    specs.Add(new Spec { Width = 26, Height = 28 });
                }
            }

            specs.Add(new Spec { Width = 10, Height = 98 }); // tail

            return specs.ToArray();
        }

        static float LinkLength(Spec[] specs, int index)
        {
            int bodyIndex = index - 1;
            float overlap = bodyIndex < SerpentChain.HeadGlueSegments ? SerpentChain.HeadGlueOverlap : SerpentChain.SegmentOverlap;
            float length = (specs[index].Height + specs[index - 1].Height) * 0.5f - overlap;

            return Math.Max(length, 2f);
        }

        // ---------------------------------------------------------------- scenarios

        static Scenario Straight()
        {
            Scenario scenario = new Scenario { Name = "straight", Description = "Head runs straight at 4 px/tick. Sanity: nothing should bend or wiggle." };
            Vector2 position = new Vector2(1400, 600);
            for (int tick = 0; tick < 400; tick++)
            {
                scenario.Head.Add(new HeadState { Center = position });
                position.X += 4f;
            }

            return scenario;
        }

        static Scenario Sine()
        {
            Scenario scenario = new Scenario { Name = "sine", Description = "Gentle sine wander, 3 px/tick. A hose should trace the same wave." };
            for (int tick = 0; tick < 500; tick++)
            {
                float x = 1400 + tick * 3f;
                float y = 600 + 70f * (float)Math.Sin(x / 130f);
                scenario.Head.Add(new HeadState { Center = new Vector2(x, y) });
            }

            return scenario;
        }

        static Scenario Noisy()
        {
            Scenario scenario = new Scenario { Name = "noisy", Description = "Straight at 3 px/tick with +-1.5 px head Y jitter and the odd 4 px spike (ground-snap noise). The 'wiggles rapidly' test." };
            Random random = new Random(7);
            Vector2 position = new Vector2(1400, 600);
            for (int tick = 0; tick < 500; tick++)
            {
                position.X += 3f;
                float jitter = (float)(random.NextDouble() * 3.0 - 1.5);
                if (random.Next(40) == 0)
                {
                    jitter += random.Next(2) == 0 ? 4f : -4f;
                }

                scenario.Head.Add(new HeadState { Center = new Vector2(position.X, position.Y + jitter) });
            }

            return scenario;
        }

        static Scenario UTurn(float radius)
        {
            Scenario scenario = new Scenario
            {
                Name = "uturn" + (int)radius,
                Description = "Passive U-turn: walk right, half circle of radius " + (int)radius + " px over the neck (the game's Turn phase), walk back left. Open air."
            };

            Vector2 position = new Vector2(1400, 700);
            for (int tick = 0; tick < 160; tick++)
            {
                scenario.Head.Add(new HeadState { Center = position });
                position.X += 1.5f;
            }

            float arcSpeed = 1.6f;
            int arcTicks = (int)Math.Ceiling(Math.PI * radius / arcSpeed);
            for (int tick = 0; tick < arcTicks; tick++)
            {
                float progress = tick / (float)arcTicks;
                float arcAngle = MathHelper.Pi * progress;
                Vector2 heading = new Vector2((float)Math.Cos(arcAngle), -(float)Math.Sin(arcAngle));
                position += heading * arcSpeed;
                scenario.Head.Add(new HeadState { Center = position, HeadingOverride = heading.ToRotation() });
            }

            for (int tick = 0; tick < 260; tick++)
            {
                position.X -= 1.5f;
                scenario.Head.Add(new HeadState { Center = position });
            }

            return scenario;
        }

        static Scenario Lunge()
        {
            Scenario scenario = new Scenario { Name = "lunge", Description = "Slow approach, 15 px/tick lunge for 26 ticks, dead stop, then run back the other way at 4 px/tick (the hard direction flip)." };
            Vector2 position = new Vector2(1400, 600);
            for (int tick = 0; tick < 100; tick++)
            {
                scenario.Head.Add(new HeadState { Center = position });
                position.X += 2f;
            }

            for (int tick = 0; tick < 26; tick++)
            {
                scenario.Head.Add(new HeadState { Center = position });
                position.X += 15f;
            }

            for (int tick = 0; tick < 40; tick++)
            {
                scenario.Head.Add(new HeadState { Center = position });
            }

            for (int tick = 0; tick < 200; tick++)
            {
                position.X -= 4f;
                scenario.Head.Add(new HeadState { Center = position });
            }

            return scenario;
        }

        static Terrain StepTerrain()
        {
            // Floor top at tile row 60 from x=0; a 9-tile step up at column 70 (top at row 51), flat beyond.
            Terrain terrain = new Terrain { Columns = 200, Rows = 90 };
            terrain.Solid = new bool[terrain.Columns, terrain.Rows];
            for (int x = 0; x < terrain.Columns; x++)
            {
                int top = x >= 70 ? 51 : 60;
                for (int y = top; y < terrain.Rows; y++)
                {
                    terrain.Solid[x, y] = true;
                }
            }

            return terrain;
        }

        static Scenario WallClimb()
        {
            Scenario scenario = new Scenario
            {
                Name = "wallclimb",
                Description = "Ground-hugging walk into a 9-tile wall, climb it at 1.2 px/tick (the game's ClimbRiseSpeed), carry on over the ledge. The clip test.",
                Terrain = StepTerrain()
            };

            float floorTop = 60 * TileSize;
            float ledgeTop = 51 * TileSize;
            float wallX = 70 * TileSize;
            float headHalfWidth = 25f;
            float headHalfHeight = 32f;

            Vector2 position = new Vector2(30 * TileSize, floorTop - headHalfHeight);
            float stopX = wallX - headHalfWidth - 1f;
            while (position.X < stopX)
            {
                scenario.Head.Add(new HeadState { Center = position });
                position.X = Math.Min(position.X + 3f, stopX);
            }

            float ledgeCenterY = ledgeTop - headHalfHeight;
            while (position.Y > ledgeCenterY)
            {
                scenario.Head.Add(new HeadState { Center = position });
                position.Y = Math.Max(position.Y - 1.2f, ledgeCenterY);
            }

            for (int tick = 0; tick < 360; tick++)
            {
                scenario.Head.Add(new HeadState { Center = position });
                position.X += 3f;
            }

            return scenario;
        }

        // ---------------------------------------------------------------- simulation

        static Run Simulate(Scenario scenario, Spec[] specs, bool legacy, string model)
        {
            int count = specs.Length;
            Vector2[] centers = new Vector2[count];
            float[] headings = new float[count]; // heading = rotation - 90 deg, the direction toward the piece ahead

            HeadState start = scenario.Head[0];
            float headHeading = start.HeadingOverride ?? 0f;
            headings[0] = headHeading;
            centers[0] = start.Center;
            for (int i = 1; i < count; i++)
            {
                headings[i] = headHeading;
                centers[i] = centers[i - 1] - SerpentChain.HeadingToDirection(headHeading) * LinkLength(specs, i);
            }

            Func<Vector2, bool> probe = null;
            if (scenario.Terrain != null)
            {
                Terrain terrain = scenario.Terrain;
                probe = center => terrain.BoxBlocked(center, 12);
            }

            Run run = new Run { Model = model };
            Vector2 previousHead = start.Center;
            int lastFace = 1;

            // Warm-up with the head parked on its first position so the body settles onto the ground before recording.
            int totalTicks = WarmupTicks + scenario.Head.Count;
            for (int tick = 0; tick < totalTicks; tick++)
            {
                HeadState state = scenario.Head[Math.Max(tick - WarmupTicks, 0)];
                Vector2 velocity = state.Center - previousHead;
                previousHead = state.Center;
                centers[0] = state.Center;

                // MIRROR of AimHead.
                if (state.HeadingOverride.HasValue)
                {
                    headings[0] = state.HeadingOverride.Value;
                }
                else
                {
                    float lookX = Math.Abs(velocity.X) > 0.4f ? velocity.X : lastFace;
                    int face = lookX >= 0f ? 1 : -1;
                    lastFace = face;
                    float xMag = Math.Max(Math.Abs(lookX), 1f);
                    float maxRise = xMag * (float)Math.Tan(0.5f);
                    float rise = MathHelper.Clamp(velocity.Y, -maxRise, maxRise);
                    float target = (float)Math.Atan2(rise, face * xMag);
                    float delta = MathHelper.WrapAngle(target - headings[0]);
                    headings[0] += MathHelper.Clamp(delta, -LegacyTurnRate, LegacyTurnRate);
                }

                for (int i = 1; i < count; i++)
                {
                    int bodyIndex = i - 1;
                    bool hugs = scenario.Terrain != null && bodyIndex >= FrontFreeSegments;

                    if (legacy)
                    {
                        StepLegacy(scenario.Terrain, specs, centers, headings, i, hugs);
                    }
                    else
                    {
                        float groundCenterY = float.NaN;
                        if (hugs)
                        {
                            groundCenterY = scenario.Terrain.GroundCenterY(centers[i], specs[i].Height);
                        }

                        Vector2 newCenter;
                        float newHeading = SerpentChain.SolveBodyJoint(
                            centers[i - 1], specs[i - 1].Height, specs[i - 1].Width, headings[i - 1],
                            centers[i], specs[i].Height, specs[i].Width, headings[i],
                            bodyIndex, Flexibility, false, 0f, groundCenterY, probe, out newCenter);

                        headings[i] = newHeading;
                        centers[i] = newCenter;
                    }
                }

                if (tick >= WarmupTicks)
                {
                    run.Centers.Add((Vector2[])centers.Clone());
                    run.Headings.Add((float[])headings.Clone());
                }
            }

            run.Metrics = Measure(run, specs, scenario.Terrain);
            return run;
        }

        // LEGACY: the old RunBodyFollow (rotation chases the piece ahead at 0.05 rad/tick, position derived from the
        // rotation) followed by the old ApplyGroundSnap (direct Y lerp), ported from git history before 2026-10-04.
        static void StepLegacy(Terrain terrain, Spec[] specs, Vector2[] centers, float[] headings, int i, bool hugs)
        {
            Vector2 offset = centers[i - 1] - centers[i];
            if (offset.LengthSquared() >= 0.01f)
            {
                float desired = (float)Math.Atan2(offset.Y, offset.X);
                float delta = MathHelper.WrapAngle(desired - headings[i]);
                headings[i] += MathHelper.Clamp(delta, -LegacyTurnRate, LegacyTurnRate);

                centers[i] = centers[i - 1] - SerpentChain.HeadingToDirection(headings[i]) * LinkLength(specs, i);
            }

            if (hugs)
            {
                float groundCenterY = terrain.GroundCenterY(centers[i], specs[i].Height);
                if (!float.IsNaN(groundCenterY))
                {
                    centers[i].Y = MathHelper.Lerp(centers[i].Y, groundCenterY, 0.1f);
                }
            }
        }

        // ---------------------------------------------------------------- metrics

        static Metrics Measure(Run run, Spec[] specs, Terrain terrain)
        {
            Metrics metrics = new Metrics();
            int count = specs.Length;
            List<float> bends = new List<float>();
            int frames = run.Centers.Count;

            // wiggle: per piece, direction reversals of the heading's per-tick change within a 40-tick window.
            int[][] reversals = new int[count][];
            for (int i = 0; i < count; i++)
            {
                reversals[i] = new int[frames];
            }

            for (int frame = 0; frame < frames; frame++)
            {
                Vector2[] centers = run.Centers[frame];
                float[] headings = run.Headings[frame];
                bool backward = false;

                for (int i = 2; i < count; i++)
                {
                    Vector2 linkA = centers[i - 1] - centers[i];
                    Vector2 linkB = centers[i - 2] - centers[i - 1];
                    float bend = Math.Abs(MathHelper.WrapAngle(linkA.ToRotation() - linkB.ToRotation()));
                    bends.Add(bend);
                    if (i >= 3)
                    {
                        metrics.MaxBendDeg = Math.Max(metrics.MaxBendDeg, MathHelper.ToDegrees(bend));
                    }

                    if (bend > MathHelper.PiOver2 && i >= 3)
                    {
                        backward = true;
                    }
                }

                if (backward)
                {
                    metrics.BackwardBendFrames++;
                }

                bool overlap = false;
                for (int i = 1; i < count && !overlap; i++)
                {
                    for (int j = i + 5; j < count; j++)
                    {
                        float threshold = 0.6f * (specs[i].Width + specs[j].Width) * 0.5f;
                        if (Vector2.Distance(centers[i], centers[j]) < threshold)
                        {
                            overlap = true;
                            break;
                        }
                    }
                }

                if (overlap)
                {
                    metrics.SelfOverlapFrames++;
                }

                for (int i = 1; i < count; i++)
                {
                    if (terrain != null && terrain.BoxBlocked(centers[i], 12))
                    {
                        metrics.ClipPieceTicks++;
                    }

                    float spacing = Vector2.Distance(centers[i], centers[i - 1]);
                    metrics.MaxSpacingError = Math.Max(metrics.MaxSpacingError, Math.Abs(spacing - LinkLength(specs, i)));

                    // How far the sprite's facing (heading) disagrees with the real link direction toward the piece ahead.
                    Vector2 toAhead = centers[i - 1] - centers[i];
                    if (toAhead.LengthSquared() > 0.01f)
                    {
                        float skew = Math.Abs(MathHelper.WrapAngle(headings[i] - toAhead.ToRotation()));
                        metrics.MaxSpriteSkewDeg = Math.Max(metrics.MaxSpriteSkewDeg, MathHelper.ToDegrees(skew));
                    }
                }

                if (frame > 0)
                {
                    Vector2[] previous = run.Centers[frame - 1];
                    float[] previousHeadings = run.Headings[frame - 1];
                    for (int i = 1; i < count; i++)
                    {
                        float pieceStep = Vector2.Distance(centers[i], previous[i]);
                        if (pieceStep > metrics.MaxPieceStep)
                        {
                            metrics.MaxPieceStep = pieceStep;
                            metrics.MaxPieceStepAt = $"piece {i} frame {frame}";
                        }

                        float headingStep = Math.Abs(MathHelper.WrapAngle(headings[i] - previousHeadings[i]));
                        metrics.MaxHeadingStepRad = Math.Max(metrics.MaxHeadingStepRad, headingStep);

                        if (frame > 1)
                        {
                            float stepNow = MathHelper.WrapAngle(headings[i] - previousHeadings[i]);
                            float stepBefore = MathHelper.WrapAngle(previousHeadings[i] - run.Headings[frame - 2][i]);
                            bool significant = Math.Abs(stepNow) > 0.015f && Math.Abs(stepBefore) > 0.015f;
                            if (significant && Math.Sign(stepNow) != Math.Sign(stepBefore))
                            {
                                reversals[i][frame] = 1;
                            }
                        }
                    }
                }
            }

            const int window = 40;
            for (int i = 1; i < count; i++)
            {
                int running = 0;
                for (int frame = 0; frame < frames; frame++)
                {
                    running += reversals[i][frame];
                    if (frame >= window)
                    {
                        running -= reversals[i][frame - window];
                    }

                    metrics.WiggleMax = Math.Max(metrics.WiggleMax, running);
                }
            }

            bends.Sort();
            if (bends.Count > 0)
            {
                metrics.P99BendDeg = MathHelper.ToDegrees(bends[(int)(bends.Count * 0.99f)]);
            }

            return metrics;
        }

        // ---------------------------------------------------------------- tail stab pose

        // MIRROR of PoseTailStabArc's curve set-up and the UpdateTailStab tip lerp. The solver is the real one.
        static void TailStab(bool sMode, string outDir, StringBuilder report)
        {
            Spec[] specs = BuildSpecs();
            int count = specs.Length;
            int attackStart = BodySegments - 16;           // GreatSerpentHead.TailAttackSegmentCount
            int anchorIndex = 1 + attackStart - 1;         // spec index of the anchor body piece
            int firstRear = anchorIndex + 1;
            int rearCount = count - firstRear;

            // Static grounded body: pieces 0..anchor laid straight along the floor, head at the right.
            float groundY = 700f;
            Vector2[] baseCenters = new Vector2[count];
            baseCenters[0] = new Vector2(2000, groundY);
            for (int i = 1; i < count; i++)
            {
                baseCenters[i] = baseCenters[i - 1] - new Vector2(LinkLength(specs, i), 0f);
            }

            Vector2 anchor = baseCenters[anchorIndex];
            Vector2 headCenter = baseCenters[0];
            Vector2 playerSpot = headCenter + new Vector2(260, 20);

            for (int pass = 0; pass < 2; pass++)
            {
                bool legacy = pass == 0;
                Vector2[] centers = (Vector2[])baseCenters.Clone();
                float[] headings = new float[count];
                for (int i = 0; i < count; i++)
                {
                    headings[i] = 0f;
                }

                float[] heights = new float[rearCount];
                float[] widths = new float[rearCount];
                for (int i = 0; i < rearCount; i++)
                {
                    heights[i] = specs[firstRear + i].Height;
                    widths[i] = specs[firstRear + i].Width;
                }

                Vector2 tip = centers[count - 1];
                Run run = new Run { Model = legacy ? "legacy" : "new" };
                float[] legacyRotation = new float[count];
                float maxTipMiss = 0f;
                float maxSkew = 0f;
                float maxHeadingStep = 0f;
                float maxBend = 0f;

                int[] phaseLengths = { 70, 48, 28, 34, 36 }; // Coiling, Aiming, Stabbing, Recover, Retracting
                int tickInPhase = 0;
                int phase = 0;
                int totalTicks = phaseLengths.Sum();

                for (int tick = 0; tick < totalTicks; tick++)
                {
                    Vector2 perch = headCenter + new Vector2(-24f, -170f);
                    if (sMode)
                    {
                        perch = anchor + new Vector2(-100f, -46f); // S cocks away from the player (player is to the right)
                    }

                    Vector2 tipTarget = perch;
                    if (phase == 2)
                    {
                        tipTarget = playerSpot;
                    }
                    else if (phase == 4)
                    {
                        tipTarget = anchor + new Vector2(-700f, 0f); // MIRROR of TailStabRetractReach
                    }

                    tip = Vector2.Lerp(tip, tipTarget, 0.11f);

                    float totalHeight = heights.Sum();
                    float linkLen = totalHeight / rearCount - SerpentChain.SegmentOverlap;
                    float maxChord = rearCount * linkLen * 0.9f;
                    Vector2 clampedTip = tip;
                    Vector2 toTip = tip - anchor;
                    if (toTip.Length() > maxChord)
                    {
                        clampedTip = anchor + Vector2.Normalize(toTip) * maxChord;
                    }

                    Vector2 axis = clampedTip - anchor;
                    Vector2 c1;
                    Vector2 c2;
                    if (sMode)
                    {
                        Vector2 perp = new Vector2(-axis.Y, axis.X);
                        perp = perp.LengthSquared() > 0.01f ? Vector2.Normalize(perp) : new Vector2(0f, -1f);
                        c1 = anchor + axis * 0.33f + perp * 64f;
                        c2 = anchor + axis * 0.66f - perp * 64f;
                    }
                    else
                    {
                        float bow = MathHelper.Clamp(Math.Abs(axis.X) * 0.7f, 40f, 170f);
                        c1 = anchor + new Vector2(axis.X * 0.2f, -bow * 0.2f);
                        c2 = anchor + new Vector2(axis.X * 0.75f, -bow);
                    }

                    Vector2[] posed = new Vector2[rearCount];
                    float[] posedHeadings = new float[rearCount];
                    for (int i = 0; i < rearCount; i++)
                    {
                        posed[i] = centers[firstRear + i];
                        posedHeadings[i] = headings[firstRear + i];
                    }

                    float[] previousHeadings = (float[])headings.Clone();

                    if (legacy)
                    {
                        // LEGACY pose: Bezier point written straight to the position, rotation eased 3x toward the
                        // tangent toward the NEXT point (forward, away from the head) + 90 deg.
                        for (int i = 0; i < rearCount; i++)
                        {
                            float t = (i + 1) / (float)rearCount;
                            float tNext = (i + 2) / (float)rearCount;
                            Vector2 pos = SerpentChain.CubicBezier(anchor, c1, c2, clampedTip, t);
                            Vector2 nextPos = i + 1 < rearCount ? SerpentChain.CubicBezier(anchor, c1, c2, clampedTip, tNext) : clampedTip + (clampedTip - c2);
                            Vector2 segDir = nextPos - pos;
                            float rotation = legacyRotation[firstRear + i];
                            if (segDir.LengthSquared() > 0.01f)
                            {
                                float desired = segDir.ToRotation() + 1.57f;
                                float delta = MathHelper.WrapAngle(desired - rotation);
                                rotation += MathHelper.Clamp(delta, -0.15f, 0.15f);
                            }

                            legacyRotation[firstRear + i] = rotation;
                            posed[i] = pos;
                            posedHeadings[i] = rotation - MathHelper.PiOver2;
                        }
                    }
                    else
                    {
                        SerpentChain.PoseAlongCurve(anchor, specs[anchorIndex].Height, specs[anchorIndex].Width, headings[anchorIndex], heights, widths, posed, posedHeadings, Flexibility, c1, c2, clampedTip, null);
                    }

                    for (int i = 0; i < rearCount; i++)
                    {
                        centers[firstRear + i] = posed[i];
                        headings[firstRear + i] = posedHeadings[i];
                    }

                    for (int i = firstRear; i < count; i++)
                    {
                        maxHeadingStep = Math.Max(maxHeadingStep, Math.Abs(MathHelper.WrapAngle(headings[i] - previousHeadings[i])));
                        Vector2 towardAhead = centers[i - 1] - centers[i];
                        if (towardAhead.LengthSquared() > 0.01f)
                        {
                            maxSkew = Math.Max(maxSkew, Math.Abs(MathHelper.WrapAngle(headings[i] - towardAhead.ToRotation())));
                        }

                        if (i >= firstRear + 1)
                        {
                            Vector2 linkA = centers[i - 1] - centers[i];
                            Vector2 linkB = centers[i - 2] - centers[i - 1];
                            maxBend = Math.Max(maxBend, Math.Abs(MathHelper.WrapAngle(linkA.ToRotation() - linkB.ToRotation())));
                        }
                    }

                    if (phase == 2)
                    {
                        maxTipMiss = Math.Max(maxTipMiss, Vector2.Distance(centers[count - 1], playerSpot));
                    }

                    run.Centers.Add((Vector2[])centers.Clone());
                    run.Headings.Add((float[])headings.Clone());

                    tickInPhase++;
                    if (tickInPhase >= phaseLengths[phase] && phase < phaseLengths.Length - 1)
                    {
                        phase++;
                        tickInPhase = 0;
                    }
                }

                Scenario view = new Scenario { Name = sMode ? "tailstab_s" : "tailstab_c" };
                run.Metrics = new Metrics
                {
                    MaxBendDeg = MathHelper.ToDegrees(maxBend),
                    MaxHeadingStepRad = maxHeadingStep,
                    MaxSpriteSkewDeg = MathHelper.ToDegrees(maxSkew)
                };

                report.AppendLine($"| {view.Name} | {run.Model} | {run.Metrics.MaxBendDeg:F1} | {run.Metrics.MaxHeadingStepRad:F2} | {run.Metrics.MaxSpriteSkewDeg:F0} | tip miss at strike {maxTipMiss:F0} px |");
                Console.WriteLine($"  {view.Name,-12} {run.Model,-7} maxBend={run.Metrics.MaxBendDeg,6:F1}deg  maxHeadingStep={run.Metrics.MaxHeadingStepRad,5:F2}rad/tick  spriteSkew={run.Metrics.MaxSpriteSkewDeg,5:F0}deg  tipMissAtStrike={maxTipMiss,6:F0}px");

                if (outDir != null)
                {
                    RenderPanelPair(view.Name + "_" + run.Model, run, specs, null, outDir, rearOnlyFrom: firstRear - 4);
                }
            }
        }

        // ---------------------------------------------------------------- rendering

        static void RenderPanelPair(string name, Run run, Spec[] specs, Terrain terrain, string outDir, int rearOnlyFrom = 0)
        {
            // Bounds over all recorded frames (and the terrain, if any).
            float minX = float.MaxValue;
            float minY = float.MaxValue;
            float maxX = float.MinValue;
            float maxY = float.MinValue;
            foreach (Vector2[] frame in run.Centers)
            {
                for (int i = rearOnlyFrom; i < frame.Length; i++)
                {
                    minX = Math.Min(minX, frame[i].X);
                    maxX = Math.Max(maxX, frame[i].X);
                    minY = Math.Min(minY, frame[i].Y);
                    maxY = Math.Max(maxY, frame[i].Y);
                }
            }

            float pad = 80f;
            minX -= pad;
            maxX += pad;
            minY -= pad;
            maxY += pad;

            float scale = Math.Min(1.6f, 1500f / (maxX - minX));
            int width = (int)((maxX - minX) * scale);
            int height = (int)((maxY - minY) * scale);
            height = Math.Min(height, 1100);

            using (SD.Bitmap bitmap = new SD.Bitmap(width, height))
            using (SD.Graphics graphics = SD.Graphics.FromImage(bitmap))
            {
                graphics.Clear(SD.Color.FromArgb(24, 24, 28));
                graphics.SmoothingMode = SD.Drawing2D.SmoothingMode.AntiAlias;

                Func<Vector2, SD.PointF> toScreen = point => new SD.PointF((point.X - minX) * scale, (point.Y - minY) * scale);

                if (terrain != null)
                {
                    using (SD.SolidBrush terrainBrush = new SD.SolidBrush(SD.Color.FromArgb(70, 70, 76)))
                    {
                        for (int x = 0; x < terrain.Columns; x++)
                        {
                            for (int y = 0; y < terrain.Rows; y++)
                            {
                                if (!terrain.Solid[x, y])
                                {
                                    continue;
                                }

                                SD.PointF corner = toScreen(new Vector2(x * TileSize, y * TileSize));
                                if (corner.X < -TileSize * scale || corner.X > width || corner.Y < -TileSize * scale || corner.Y > height)
                                {
                                    continue;
                                }

                                graphics.FillRectangle(terrainBrush, corner.X, corner.Y, TileSize * scale + 0.5f, TileSize * scale + 0.5f);
                            }
                        }
                    }
                }

                int frames = run.Centers.Count;
                int onionStep = Math.Max(frames / 7, 1);
                for (int frame = 0; frame < frames; frame += onionStep)
                {
                    int alpha = 40 + (int)(150f * frame / Math.Max(frames - 1, 1));
                    DrawChain(graphics, toScreen, scale, specs, run.Centers[frame], run.Headings[frame], alpha, false, rearOnlyFrom);
                }

                DrawChain(graphics, toScreen, scale, specs, run.Centers[frames - 1], run.Headings[frames - 1], 255, true, rearOnlyFrom);

                using (SD.Font font = new SD.Font("Consolas", 11f))
                using (SD.SolidBrush textBrush = new SD.SolidBrush(SD.Color.White))
                {
                    Metrics m = run.Metrics;
                    string label = $"{name}   maxBend {m.MaxBendDeg:F0} deg   p99 {m.P99BendDeg:F0}   backward {m.BackwardBendFrames}f   overlap {m.SelfOverlapFrames}f   clip {m.ClipPieceTicks}   wiggle {m.WiggleMax}   maxHdgStep {m.MaxHeadingStepRad:F2}";
                    graphics.DrawString(label, font, textBrush, 8, 6);
                }

                Directory.CreateDirectory(outDir);
                bitmap.Save(Path.Combine(outDir, name + ".png"), SD.Imaging.ImageFormat.Png);
            }
        }

        static void DrawChain(SD.Graphics graphics, Func<Vector2, SD.PointF> toScreen, float scale, Spec[] specs, Vector2[] centers, float[] headings, int alpha, bool filled, int fromIndex)
        {
            for (int i = centers.Length - 1; i >= Math.Max(fromIndex, 0); i--)
            {
                float length = specs[i].Height * scale;
                float thickness = specs[i].Width * scale;
                // Sprite rotation = heading + 90 deg: its height axis runs along the link, +Y in sprite space = away from the ahead piece.
                float rotation = headings[i] + MathHelper.PiOver2;

                SD.PointF center = toScreen(centers[i]);
                SD.Color color = i == 0 ? SD.Color.FromArgb(alpha, 80, 160, 255) : SD.Color.FromArgb(alpha, 190, 90, 190);

                if (i >= 2 && filled)
                {
                    Vector2 linkA = centers[i - 1] - centers[i];
                    Vector2 linkB = centers[i - 2] - centers[i - 1];
                    float bend = Math.Abs(MathHelper.WrapAngle(linkA.ToRotation() - linkB.ToRotation()));
                    float danger = MathHelper.Clamp(bend / MathHelper.ToRadians(30f), 0f, 1f);
                    color = SD.Color.FromArgb(alpha, (int)(80 + 175 * danger), (int)(210 - 150 * danger), 90);
                }

                SD.Drawing2D.GraphicsState state = graphics.Save();
                graphics.TranslateTransform(center.X, center.Y);
                graphics.RotateTransform(MathHelper.ToDegrees(rotation));

                using (SD.Pen pen = new SD.Pen(SD.Color.FromArgb(alpha, 230, 230, 230), 1f))
                using (SD.SolidBrush brush = new SD.SolidBrush(SD.Color.FromArgb(filled ? 150 : 0, color)))
                {
                    if (filled)
                    {
                        graphics.FillRectangle(brush, -thickness / 2f, -length / 2f, thickness, length);
                    }

                    graphics.DrawRectangle(pen, -thickness / 2f, -length / 2f, thickness, length);
                }

                graphics.Restore(state);
            }
        }

        // ---------------------------------------------------------------- main

        static int Main(string[] args)
        {
            string only = "all";
            string outDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "..", "tsorcDocs", "SerpentReports"));

            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "--radius" && i + 1 < args.Length)
                {
                    SerpentChain.MinBendRadiusInWidths = float.Parse(args[++i], CultureInfo.InvariantCulture);
                }
                else if (args[i] == "--hard" && i + 1 < args.Length)
                {
                    SerpentChain.HardBendFactor = float.Parse(args[++i], CultureInfo.InvariantCulture);
                }
                else if (args[i] == "--flex" && i + 1 < args.Length)
                {
                    Flexibility = float.Parse(args[++i], CultureInfo.InvariantCulture);
                }
                else if (args[i] == "--out" && i + 1 < args.Length)
                {
                    outDir = Path.GetFullPath(args[++i]);
                }
                else
                {
                    only = args[i];
                }
            }

            Spec[] specs = BuildSpecs();
            List<Scenario> scenarios = new List<Scenario>
            {
                Straight(), Sine(), Noisy(), UTurn(56f), UTurn(80f), UTurn(110f), Lunge(), WallClimb()
            };

            Console.WriteLine("Serpent rig -> " + outDir);
            Console.WriteLine($"Min bend radius {SerpentChain.MinBendRadiusInWidths:F2} x width, hard limit {SerpentChain.HardBendFactor:F2} x comfort, flexibility {Flexibility:F2}");
            Console.WriteLine("Comfort bend per joint, spec index=deg (1 = neck, 2-3 = glue, then fat body, Body2, Body3, tail): " + string.Join("  ", new[] { 1, 2, 4, 20, BodySegments - 3, BodySegments - 1, BodySegments + 1 }.Select(index => $"#{index}={MathHelper.ToDegrees(SerpentChain.ComfortBend(LinkLength(specs, index), specs[index].Width, specs[index - 1].Width, index - 1, Flexibility)):F0}")));
            Console.WriteLine();

            StringBuilder report = new StringBuilder();
            report.AppendLine("| scenario | model | maxBend deg | p99 bend deg | backward frames | self-overlap frames | clip piece-ticks | wiggle (reversals/40t) | max heading step rad/tick | max piece step px |");
            report.AppendLine("|---|---|---|---|---|---|---|---|---|---|");

            foreach (Scenario scenario in scenarios)
            {
                if (only != "all" && only != scenario.Name)
                {
                    continue;
                }

                Console.WriteLine($"{scenario.Name}: {scenario.Description}");
                foreach (bool legacy in new[] { true, false })
                {
                    string model = legacy ? "legacy" : "new";
                    Run run = Simulate(scenario, specs, legacy, model);
                    Metrics m = run.Metrics;
                    Console.WriteLine($"  {model,-7} maxBend={m.MaxBendDeg,6:F1}deg p99={m.P99BendDeg,5:F1} backward={m.BackwardBendFrames,4}f overlap={m.SelfOverlapFrames,4}f clip={m.ClipPieceTicks,5} wiggle={m.WiggleMax,3} hdgStep={m.MaxHeadingStepRad,5:F2} pieceStep={m.MaxPieceStep,5:F1}px@{m.MaxPieceStepAt} spacingErr={m.MaxSpacingError,4:F2}");
                    report.AppendLine($"| {scenario.Name} | {model} | {m.MaxBendDeg:F1} | {m.P99BendDeg:F1} | {m.BackwardBendFrames} | {m.SelfOverlapFrames} | {m.ClipPieceTicks} | {m.WiggleMax} | {m.MaxHeadingStepRad:F2} | {m.MaxPieceStep:F1} |");
                    RenderPanelPair(scenario.Name + "_" + model, run, specs, scenario.Terrain, outDir);
                }

                Console.WriteLine();
            }

            if (only == "all" || only.StartsWith("tailstab"))
            {
                Console.WriteLine("Tail-stab pose (rear 17 pieces, anchor fixed):");
                StringBuilder poseReport = new StringBuilder();
                TailStab(false, outDir, poseReport);
                TailStab(true, outDir, poseReport);
                Console.WriteLine();
            }

            Directory.CreateDirectory(outDir);
            File.WriteAllText(Path.Combine(outDir, "SerpentRig_metrics.md"), report.ToString());
            return 0;
        }
    }
}

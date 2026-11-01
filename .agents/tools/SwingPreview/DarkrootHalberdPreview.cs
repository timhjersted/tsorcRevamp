using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using tsorcRevamp.Content.Items.Weapons.Melee.Halberds;

namespace SwingPreview
{
    internal static class DarkrootHalberdPreview
    {
        internal static int Run(string[] args)
        {
            Options options = Options.Parse(args.Where(a => a != "--darkroot-halberd").ToArray());
            if (options == null) return 1;
            string root = options.RepoRoot;
            var art = new PuppetArt {
                Name = "DarkrootHalberd-" + (options.Direction < 0 ? "left" : "right"),
                BodySheet = Path.Combine(root, "Content/Items/Armor/Enemy/DarkrootSentrySet/DarkrootSentryBreastplate_Body.png"),
                LegsSheet = Path.Combine(root, "Content/Items/Armor/Enemy/DarkrootSentrySet/DarkrootSentryGreaves_Legs.png"),
                HeadSheet = Path.Combine(root, "Content/Items/Armor/Enemy/DarkrootSentrySet/DarkrootSentryHelmet_Head.png"),
                WeaponSprite = Path.Combine(root, "Content/Items/Weapons/Melee/Halberds/DarkrootHalberd.png"),
                WeaponScale = DarkrootHalberdMotion.DrawScale,
                WeaponRotationOffset = -DarkrootHalberdMotion.NativeAngle,
                HandleNormX = DarkrootHalberdMotion.Grip.X / 74, HandleNormY = DarkrootHalberdMotion.Grip.Y / 74,
                SpearHeadNorm = new PointF(DarkrootHalberdMotion.Grip.X / 74, DarkrootHalberdMotion.Grip.Y / 74),
                SpearBaseNorm = new PointF(DarkrootHalberdMotion.GripAt(16).X / 74, DarkrootHalberdMotion.GripAt(16).Y / 74)
            };
            Console.WriteLine("PLAYER weapon curves from compiled mod. Darkroot Sentry vanity armor is a body-fit reference, not the NPC moveset.");
            Console.WriteLine("No walking, terrain, real collision, input/stamina hooks, network or gravity inversion simulated.");
            int[][] sequences = { new[] { 0, 1, 2 }, new[] { 3, 4, 4, 5 } };
            foreach (int[] sequence in sequences)
            {
                var frames = new List<PoseFrame>();
                float entry = DarkrootHalberdMotion.Carry;
                foreach (int step in sequence)
                {
                    int ticks = DarkrootHalberdMotion.BaseDuration(step);
                    int live = 0; float peak = 0, previous = entry, maxError = 0;
                    for (int i = 0; i < ticks; i++)
                    {
                        float angle = DarkrootHalberdMotion.Angle(step, i + 1, entry);
                        float extension = DarkrootHalberdMotion.Extension(step, i + 1);
                        float arm = angle - MathHelper.PiOver2;
                        Vector2 hand = new Vector2((float)Math.Cos(angle) * 10 - 4 - (float)Math.Sin(angle) * 3,
                            (float)Math.Sin(angle) * 10 - 2 + (float)Math.Cos(angle) * 3);
                        float rear = DarkrootHalberdMotion.RearArm(hand, angle, 1, extension, out float error);
                        bool active = DarkrootHalberdMotion.Live(step, i);
                        if (active) live++;
                        maxError = Math.Max(error, maxError);
                        peak = Math.Max(peak, Math.Abs(angle - previous) * 180f / MathHelper.Pi);
                        previous = angle;
                        frames.Add(new PoseFrame { Tick = frames.Count, Motion = "Player halberd",
                            Phase = active ? "MeleeComboAttack" : i < DarkrootHalberdMotion.Windup(step) ? "MeleeComboTelegraph" : "MeleeComboRecovery",
                            Stage = step switch { 0 => "Overhand", 1 => "Underhand", 2 => "Pierce", 3 => "Spin windup", 4 => "Paid full rotation", _ => "Spin recovery" },
                            Armed = active, Direction = options.Direction,
                            CompositeArmRotation = arm * options.Direction, BackArmFree = rear,
                            WeaponRotation = angle, BodyRow = 3, LegRow = 0, SpearGrip = extension / 16f });
                    }
                    float end = DarkrootHalberdMotion.Angle(step, ticks, entry);
                    Console.WriteLine($"step {step}: {ticks}t, live {live}t, travel {MathHelper.ToDegrees(end - entry):0.00}deg, peak {peak:0.00}deg/t, rear hand error <= {maxError:0.00}px");
                    entry = step == 4 ? MathHelper.WrapAngle(end) : end;
                    // Verify fast-tempo windows still have a real start/end and finite, continuously sampled geometry.
                    int fast = DarkrootHalberdMotion.MinimumDuration(step);
                    for (int i = 0; i < fast; i++)
                    {
                        float value = DarkrootHalberdMotion.Angle(step, DarkrootHalberdMotion.Time(i + 1, fast, step), entry);
                        if (!float.IsFinite(value)) throw new InvalidOperationException("Nonfinite fast pose");
                    }
                }
                BodyRenderer.Render(art, frames, options.BodyOutDir, sequence[0] == 0 ? "Primary combo" : "Two paid rotations", options.Zoom);
            }
            // The declared sprite transform must mirror exactly in both directions, including the changing thrust grip.
            foreach (float angle in new[] { -2.3f, -0.5f, 0f, 1.57f, 3.2f })
            foreach (float extension in new[] { 0f, 8f, 16f })
            {
                Vector2 right = DarkrootHalberdMotion.Offset(DarkrootHalberdMotion.Point, angle, 1, extension);
                Vector2 left = DarkrootHalberdMotion.Offset(DarkrootHalberdMotion.Point, MathHelper.Pi - angle, -1, extension);
                if (Vector2.Distance(new Vector2(-right.X, right.Y), left) > 0.001f) throw new InvalidOperationException("Mirroring mismatch");
            }
            Console.WriteLine("Sprite/collider transform mirror checks passed.");
            foreach (int duration in new[] { 44, 56, 112 })
            {
                float distance = 0;
                for (int tick = 0; tick < duration; tick++)
                    distance += DarkrootHalberdMotion.PushDistance(DarkrootHalberdMotion.Time(tick, duration, 2), DarkrootHalberdMotion.Time(tick + 1, duration, 2));
                if (Math.Abs(distance - 32) > 0.001f) throw new InvalidOperationException("Thrust travel drift");
            }
            Console.WriteLine("32px thrust at fast/base/slow tempos passed. Loot requires a loaded tModLoader world and is not exercised by this pose preview.");
            return 0;
        }
    }
}

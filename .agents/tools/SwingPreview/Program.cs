using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using tsorcRevamp.NPCs.Puppets;
using tsorcRevamp.Utilities;

namespace SwingPreview
{
    /// <summary>
    /// Simulates a puppet melee swing without launching Terraria and writes telemetry-schema JSONL,
    /// so swing-analyze.ps1 renders a previewed swing exactly like a logged one.
    ///
    /// The rotation curve comes from the mod's own SwingEase and WeaponArchetypeTables via an
    /// assembly reference, so a tuning change in the mod shows up here on the next -t:Compile with
    /// nothing to re-sync by hand.
    ///
    /// What it CANNOT show: sprites, the composite arm pose, terrain, AI decisions, or hit
    /// registration. It previews the motion curve and its timing, which is what most of the
    /// "does this swing read right" question actually is. Still playtest before shipping.
    /// </summary>
    internal static class Program
    {
        private static int Main(string[] args)
        {
            var options = Options.Parse(args);
            if (options == null)
            {
                Options.PrintUsage();
                return 1;
            }

            List<SwingSpec> specs = options.BuildSpecs();
            if (specs.Count == 0)
            {
                Console.WriteLine("Nothing to preview. Check --archetype / --combo spelling, or use --list.");
                return 1;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(options.OutFile)));
            using var writer = new StreamWriter(options.OutFile, append: false);

            PuppetArt art = options.Body ? PuppetArtLibrary.Resolve(options.Puppet, options.RepoRoot) : null;
            if (art != null && options.WeaponScale > 0f) { art.WeaponScale = options.WeaponScale; }
            if (options.Body && art == null)
            {
                Console.WriteLine($"No art profile for --puppet {options.Puppet}. Known: {PuppetArtLibrary.Known}");
                return 1;
            }

            int run = 0;
            foreach (SwingSpec spec in specs)
            {
                List<PoseFrame> poses = Simulate(spec, run, writer);
                Console.WriteLine($"  run {run,-3} {spec.Label}   telegraph={spec.TelegraphTicks} attack={spec.AttackTicks} recovery={spec.RecoveryTicks} ease={spec.Ease}");

                if (art != null)
                {
                    BodyRenderer.Render(art, poses, options.BodyOutDir, spec.Label, options.Zoom);
                }
                run++;
            }

            Console.WriteLine();
            Console.WriteLine($"wrote {run} run(s) -> {options.OutFile}");
            Console.WriteLine("render with:");
            Console.WriteLine($"  powershell -File .agents/tools/swing-analyze.ps1 -LogFile \"{options.OutFile}\" -Top {run}");
            return 0;
        }

        /// <summary>
        /// Walks one swing tick by tick and emits a render_frame per tick.
        ///
        /// Phase clock mirrors PuppetNPC: the telegraph runs LogicalSwingWindup (settle toward the
        /// opposite end for the first quarter, then SmoothStep up to the arc's start, landing exactly
        /// on it), the attack runs the step's ease across AttackTicks/SwingSpeedMult, and the recovery
        /// holds the finished pose for the linger before easing back to the carry angle.
        /// </summary>
        private static List<PoseFrame> Simulate(SwingSpec spec, int run, StreamWriter writer)
        {
            var poses = new List<PoseFrame>();
            (float start, float end) = WeaponArchetypeTables.SwingArcEndpoints(spec.Motion, spec.OverheadWindupOvershoot);
            if (spec.FlipArc)
            {
                (start, end) = (end, start);
            }

            int attackTicks = Math.Max(6, (int)Math.Round(spec.AttackTicks / Math.Max(0.01f, spec.SwingSpeedMult)));
            int total = spec.TelegraphTicks + attackTicks + spec.RecoveryTicks;
            float rotation = spec.HoldRotation;

            for (int tick = 0; tick < total; tick++)
            {
                string phase;
                bool armed = false;

                if (tick < spec.TelegraphTicks)
                {
                    phase = "MeleeComboTelegraph";
                    float progress = spec.TelegraphTicks <= 1 ? 1f : tick / (float)(spec.TelegraphTicks - 1);
                    rotation = spec.UseLogicalTelegraph
                        ? LogicalSwingWindup(end, start, progress, spec.HoldRotation)
                        : MathHelper.Lerp(rotation, start, 0.30f);
                }
                else if (tick < spec.TelegraphTicks + attackTicks)
                {
                    phase = "MeleeComboAttack";
                    int elapsed = tick - spec.TelegraphTicks;
                    float t = attackTicks <= 1 ? 1f : elapsed / (float)(attackTicks - 1);

                    rotation = spec.Ease == SwingEaseStyle.Trapezoidal
                        ? SwingEase.ApplyTrapezoidal(start, end, elapsed, attackTicks)
                        : SwingEase.Apply(start, end, t, spec.Ease);

                    // Damage window as a fraction of the active swing, matching how the tracked blade
                    // check is armed for the sweep rather than the whole phase.
                    armed = t >= spec.ArmFrom && t <= spec.ArmTo;
                }
                else
                {
                    phase = "MeleeComboRecovery";
                    int elapsed = tick - spec.TelegraphTicks - attackTicks;
                    if (elapsed >= spec.RecoveryLingerTicks)
                    {
                        rotation = MathHelper.Lerp(rotation, spec.HoldRotation, 0.10f);
                    }
                }

                WriteFrame(writer, spec, run, tick, phase, rotation, armed);

                // Vanilla composite-arm space: 0 = arm hanging straight down, -PI/2 = level forward.
                // The mod's swing space differs by exactly -PI/2, then mirrors with facing.
                float compositeArmRotation = (rotation - MathHelper.PiOver2) * spec.Direction;

                // Airborne uses body row 5, which is also where vanilla suppresses the shoulder caps
                // (CreateCompositeData case 5) unless the armour opts into showsShouldersWhileJumping.
                int bodyRow = spec.Airborne
                    ? 5
                    : PuppetNPC.BodyRowFromWeaponRotation(rotation, spec.Direction, spec.WeaponRotationOffset);

                poses.Add(new PoseFrame
                {
                    Tick = tick,
                    Phase = phase,
                    Armed = armed,
                    Airborne = spec.Airborne,
                    WeaponRotation = rotation,
                    CompositeArmRotation = compositeArmRotation,
                    Direction = spec.Direction,
                    BodyRow = bodyRow,
                    LegRow = spec.Airborne ? 5 : 0,
                });
            }

            return poses;
        }

        /// <summary>Port of PuppetNPC.LogicalSwingWindup — settle toward the opposite end for the
        /// first quarter, then SmoothStep to the arc start, arriving exactly on it. Kept here because
        /// the original is a private instance method; the constant is the one thing to re-sync.</summary>
        private static float LogicalSwingWindup(float oppositeEnd, float attackStart, float progress, float holdRotation)
        {
            progress = MathHelper.Clamp(progress, 0f, 1f);
            const float settleFraction = 0.25f;

            if (progress < settleFraction)
            {
                float settle = MathHelper.SmoothStep(0f, 1f, progress / settleFraction);
                return MathHelper.Lerp(holdRotation, oppositeEnd, settle);
            }

            float raise = MathHelper.SmoothStep(0f, 1f, (progress - settleFraction) / (1f - settleFraction));
            return MathHelper.Lerp(oppositeEnd, attackStart, raise);
        }

        /// <summary>
        /// Emits the subset of PuppetAttackTelemetry's render_frame schema that swing-analyze.ps1
        /// consumes. Hand and tip are synthesised from the rotation at a fixed reach: the analyzer
        /// derives its angle from that geometry, which is what makes a previewed run and a logged run
        /// directly comparable.
        /// </summary>
        private static void WriteFrame(StreamWriter writer, SwingSpec spec, int run, int tick,
            string phase, float rotation, bool armed)
        {
            var culture = CultureInfo.InvariantCulture;
            Vector2 npc = new Vector2(0f, 0f);
            Vector2 hand = new Vector2(0f, -10f);
            Vector2 tip = hand + new Vector2((float)Math.Cos(rotation), (float)Math.Sin(rotation)) * spec.Reach;

            string F(float value) => value.ToString("0.###", culture);
            string armedText = armed ? "true" : "false";

            writer.WriteLine(
                $"{{\"event\":\"render_frame\",\"session\":\"preview\",\"run\":{run},\"tick\":{tick}," +
                $"\"puppet\":\"{spec.Puppet}\",\"puppetDisplayName\":\"{spec.Puppet}\"," +
                $"\"attack\":\"{spec.Label}\",\"phase\":\"{phase}\",\"phaseTimer\":0," +
                $"\"motion\":\"{spec.Motion}\",\"dir\":1,\"spriteDir\":1,\"lockedDir\":1," +
                $"\"npc\":[{F(npc.X)},{F(npc.Y)}],\"hand\":[{F(hand.X)},{F(hand.Y)}]," +
                $"\"visualTip\":[{F(tip.X)},{F(tip.Y)}],\"collisionTip\":[{F(tip.X)},{F(tip.Y)}]," +
                $"\"rawRotDeg\":{F(MathHelper.ToDegrees(rotation))}," +
                $"\"drawRotDeg\":{F(MathHelper.ToDegrees(rotation))}," +
                $"\"armWeaponErrorDeg\":0,\"bladeArmed\":{armedText}}}");
        }
    }
}

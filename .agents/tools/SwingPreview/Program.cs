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
    /// Simulates a puppet melee combo without launching Terraria and writes telemetry-schema JSONL,
    /// so swing-analyze.ps1 renders a previewed swing exactly like a logged one.
    ///
    /// Arc endpoints, easing and V2 clips come from the mod's own code via an assembly reference, and
    /// the puppet's flags come from PuppetProfile (reflection), so a tuning change in the mod shows up
    /// here on the next -t:Compile. The one hand-port is ComboPose below: PuppetNPC.TickWeaponAnim's
    /// combo branch is private and reads live NPC state, so it cannot be called headlessly.
    ///
    /// What it CANNOT show: terrain, AI decisions, leap airtime (leaps run their full timer here but
    /// end on landing in game), aim bias (the target is always level), or hit registration.
    /// </summary>
    internal static class Program
    {
        private const string PhaseTelegraph = "MeleeComboTelegraph";
        private const string PhaseAttack = "MeleeComboAttack";
        private const string PhasePause = "MeleeComboPause";
        private const string PhaseRecovery = "MeleeComboRecovery";

        // A combo that never reaches recovery is a simulation bug; stop rather than spin forever.
        private const int MaxTicks = 2000;

        private static int Main(string[] args)
        {
            var options = Options.Parse(args);
            if (options == null)
            {
                Options.PrintUsage();
                return 1;
            }

            PuppetProfile profile = options.List ? null : options.BuildProfile();
            List<SwingSpec> specs = options.BuildSpecs(profile);
            if (specs.Count == 0)
            {
                if (!options.List)
                {
                    Console.WriteLine("Nothing to preview. Check --puppet / --archetype / --combo spelling, or use --list.");
                }
                return options.List ? 0 : 1;
            }

            PrintProfile(profile);
            if (options.CompareEases && !profile.AuthoredClock)
            {
                Console.WriteLine("  NOTE --compare-eases with the authored clock OFF: the game ignores step Ease, so every");
                Console.WriteLine("       variant below swings identically. Pass --clock on to see the curves.");
            }

            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(options.OutFile)));
            using var writer = new StreamWriter(options.OutFile, append: false);

            PuppetArt art = options.Body ? PuppetArtLibrary.Resolve(options.Puppet, options.RepoRoot) : null;
            if (art != null)
            {
                // Grip point and scale come from the puppet, exactly as DrawWeaponToLayer reads them.
                art.HandleNormX = profile.HandleNormX;
                art.HandleNormY = profile.HandleNormY;
                art.WeaponScale = profile.WeaponDrawScale;
                if (options.WeaponScale > 0f)
                {
                    art.WeaponScale = options.WeaponScale;
                }
            }

            BodyRenderer.VanillaShoulderOrder = options.VanillaShoulderOrder;
            if (options.Body && art == null)
            {
                Console.WriteLine($"No art profile for --puppet {options.Puppet}. Known: {PuppetArtLibrary.Known}");
                return 1;
            }

            int run = 0;
            foreach (SwingSpec spec in specs)
            {
                // Re-bind: ModifyMeleeArcEndpoints is evaluated during the simulation and may key on
                // the live combo, and the specs were all built (each binding its own) before this loop.
                spec.Profile.Bind(spec.Combo);

                var notes = new SortedSet<string>();
                List<PoseFrame> poses = spec.V2Clip != null
                    ? SimulateV2(spec, run, writer)
                    : SimulateCombo(spec, run, writer, notes);

                Console.WriteLine();
                Console.WriteLine($"  run {run,-3} {spec.Label}   {poses.Count} frames   step linger {spec.InterStepLingerTicks}" +
                                  $"   recovery {spec.RecoveryTicks} (linger {spec.RecoveryLingerTicks})");
                PrintSteps(spec);
                foreach (string note in notes)
                {
                    Console.WriteLine($"        ! {note}");
                }
                if (options.Profile)
                {
                    PrintSpeedProfile(poses);
                }

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

        private static void PrintProfile(PuppetProfile profile)
        {
            Console.WriteLine($"profile {profile.Name}:");
            Console.WriteLine($"  authored clock {OnOff(profile.AuthoredClock)}   UseSwingEasing {OnOff(profile.UseSwingEasing)}" +
                              $"   logical telegraphs {OnOff(profile.LogicalTelegraphs)}   aim swing {OnOff(profile.AimSwingActive)}");
            Console.WriteLine($"  telegraph = max({profile.MinComboTelegraphTicks}, tel x {profile.ComboTelegraphMultiplier:0.##})" +
                              $"   weapon useAnimation {profile.WeaponUseAnimation}   hold {profile.HoldRotation:0.##} rad" +
                              "   (lingers are per combo, shown per run)");
            foreach (string note in profile.Notes)
            {
                Console.WriteLine($"  - {note}");
            }
        }

        private static string OnOff(bool value) => value ? "ON" : "off";

        /// <summary>
        /// Per-phase speed profile, the numbers a swing's feel is judged by. One row per phase
        /// segment (telegraph, each step's attack and pause, recovery):
        ///   sweep      net degrees travelled
        ///   peak       fastest single tick, and which tick of the segment it lands on
        ///   first      the jump INTO this segment on its first frame - the between-phase snap
        ///   armed      ticks the blade was live
        /// followed by the deg/tick of every frame, which shows the curve's shape directly.
        /// </summary>
        private static void PrintSpeedProfile(List<PoseFrame> poses)
        {
            int start = 0;
            while (start < poses.Count)
            {
                int end = start;
                while (end + 1 < poses.Count
                       && poses[end + 1].Phase == poses[start].Phase
                       && poses[end + 1].StepIndex == poses[start].StepIndex)
                {
                    end++;
                }

                float firstJump = 0f;
                if (start > 0)
                {
                    firstJump = Math.Abs(MathHelper.ToDegrees(poses[start].WeaponRotation - poses[start - 1].WeaponRotation));
                }

                float peak = 0f;
                int peakAt = 0;
                int armedTicks = 0;
                var perTick = new List<string>();
                for (int i = start; i <= end; i++)
                {
                    float speed = 0f;
                    if (i > start)
                    {
                        speed = Math.Abs(MathHelper.ToDegrees(poses[i].WeaponRotation - poses[i - 1].WeaponRotation));
                    }
                    if (speed > peak)
                    {
                        peak = speed;
                        peakAt = i - start;
                    }
                    if (poses[i].Armed)
                    {
                        armedTicks++;
                    }
                    perTick.Add(speed.ToString("0", CultureInfo.InvariantCulture));
                }

                float sweep = Math.Abs(MathHelper.ToDegrees(poses[end].WeaponRotation - poses[start].WeaponRotation));
                string phase = poses[start].Phase.Replace("MeleeCombo", "");
                string label = $"{poses[start].StepIndex + 1} {phase} {poses[start].Motion}";
                if (poses[start].WeaponHidden)
                {
                    label += " (sheathed)";
                }
                Console.WriteLine($"        {label,-32} {end - start + 1,3}t  sweep {sweep,4:0}°  peak {peak,5:0.0}°/t @{peakAt,-3}" +
                                  $" first {firstJump,5:0.0}°  armed {armedTicks}");
                Console.WriteLine($"            deg/tick: {string.Join(" ", perTick)}");

                start = end + 1;
            }
        }

        /// <summary>Per-step readout of the three ease gates plus the swing clock, so a run states
        /// what the game actually does with each authored value instead of leaving it to be inferred.</summary>
        private static void PrintSteps(SwingSpec spec)
        {
            if (spec.V2Clip != null)
            {
                PuppetAttackClip clip = spec.V2Clip;
                Console.WriteLine($"        V2 clip '{clip.Name}': windup {clip.WindupTicks}, active {clip.ActiveTicks}, recovery {clip.RecoveryTicks}," +
                                  $" ease {clip.SwingEase}, hit {clip.HitWindowStart:0.##}-{clip.HitWindowEnd:0.##}");
                Console.WriteLine("        (steps' Motion/Ease/ticks are unused while a RuntimeV2Clip is set)");
                return;
            }

            PuppetProfile profile = spec.Profile;
            for (int i = 0; i < spec.Steps.Length; i++)
            {
                MeleeComboStep step = spec.Steps[i];
                bool authoredClock = profile.AuthoredClock && IsArcSwingMotion(step.Motion);
                int attackTicks = AttackPhaseTicks(profile, step, spec.WeaponUseAnimation);
                int sweepTicks = attackTicks;
                if (!authoredClock)
                {
                    sweepTicks = spec.WeaponUseAnimation;
                }

                string telegraph = "";
                if (i == 0)
                {
                    telegraph = $"tel {ComboTelegraphTicks(profile, step)}  ";
                }

                // Only motions that sample the 0..1 sweep clock can be cut short by it. Spin turns at
                // a fixed rate and the carry/hold motions lerp toward a pose, so the clock is moot.
                string clock = "no sweep clock (fixed-rate / pose motion)";
                if (IsLandingTimedLeap(profile, step))
                {
                    attackTicks = Math.Min(attackTicks, profile.LeapAirtimeTicks);
                    clock = $"landing-timed leap: carried {profile.LeapCarryRotation:0.00} rad, {LeapSlamDownswingTicks}-tick" +
                            $" downswing to {profile.LeapImpactRotation:0.00}";
                    if (step.LeapStrikeRange > 0f)
                    {
                        clock += $", or in the air within {step.LeapStrikeRange:0}px";
                    }
                }
                else if (ReadsStepEase(profile, step.Motion) || step.Motion == ComboMotion.DoubleSpinSlam)
                {
                    clock = $"sweep over {sweepTicks}";
                    if (!authoredClock)
                    {
                        clock += " (weapon useAnimation)";
                    }
                    if (sweepTicks > attackTicks)
                    {
                        clock += $" -> reaches t={attackTicks / (float)sweepTicks:0.00} before the step ends";
                    }
                }

                string pause = "";
                if (i + 1 < spec.Steps.Length)
                {
                    pause = $"  pause {Math.Max(1, step.PostStepPause)}";
                }

                Console.WriteLine($"        {i + 1} {step.Motion,-15} {telegraph}atk {attackTicks}  {clock}  ease {EffectiveEase(profile, step)}{pause}");
            }
        }

        /// <summary>The ease the game really applies to a step. Gate 2: only motions that call
        /// ApplySwingEase read Ease at all. Gate 1: without the authored clock, those fall back to
        /// UseSwingEasing. Gate 3 (V2 clips) is handled before this is reached.</summary>
        private static string EffectiveEase(PuppetProfile profile, MeleeComboStep step)
        {
            if (!ReadsStepEase(profile, step.Motion))
            {
                return $"n/a (authored {step.Ease} ignored: {step.Motion} never reads Ease)";
            }
            if (step.Ease == SwingEaseStyle.Trapezoidal)
            {
                return "Trapezoidal";
            }
            if (step.Ease == SwingEaseStyle.Weighted && profile.AuthoredClock)
            {
                string window = "";
                if (step.HitWindowEnd > 0f)
                {
                    window = $", armed to {step.HitWindowEnd:P0}";
                }
                return $"Weighted (in {step.EaseInTicks}, out {step.EaseOutTicks}{window})";
            }
            if (profile.AuthoredClock)
            {
                return step.Ease.ToString();
            }

            string fallback = profile.UseSwingEasing ? "Smooth" : "Linear";
            if (step.Ease.ToString() == fallback)
            {
                return fallback;
            }
            return $"{fallback} (authored {step.Ease} ignored: clock off)";
        }

        private static bool ReadsStepEase(PuppetProfile profile, ComboMotion motion)
        {
            switch (motion)
            {
                case ComboMotion.OverheadArc:
                case ComboMotion.UnderhandArc:
                case ComboMotion.HorizontalSweep:
                case ComboMotion.VerticalChop:
                case ComboMotion.IaidoDraw:
                case ComboMotion.GroundSlam:
                case ComboMotion.JoustDash:
                case ComboMotion.LeapThrust:
                    return true;
                case ComboMotion.LeapSlam:
                    return !profile.LandingTimedLeapSlam;
                default:
                    return false;
            }
        }

        // ── Legacy MeleeCombo timeline ─────────────────────────────────────────────

        /// <summary>
        /// Walks a combo tick by tick in the game's order: PuppetAttackAI's phase logic first, then
        /// TickWeaponAnim's pose. Mirrors PuppetNPC exactly where it matters for the read:
        ///   - only step 1 telegraphs; later steps start straight out of the pause
        ///   - the sweep clock is item.useAnimation unless the authored clock covers the motion
        ///   - pauses run the shared handoff (step linger) or the per-motion drift toward the
        ///     OUTGOING arc, which is what makes a chain snap between hits
        ///   - recovery only after the last step, holding for MeleeRecoveryLingerTicks
        /// </summary>
        private static List<PoseFrame> SimulateCombo(SwingSpec spec, int run, StreamWriter writer, SortedSet<string> notes)
        {
            PuppetProfile profile = spec.Profile;
            MeleeComboStep[] steps = spec.Steps;
            var poses = new List<PoseFrame>();

            string phase = PhaseTelegraph;
            int stepIndex = 0;
            int phaseTimer = ComboTelegraphTicks(profile, steps[0]);
            int weaponAnimMax = spec.WeaponUseAnimation;
            int weaponAnim = 0;
            int activeStepTotalTicks = 0;
            bool armed = false;
            float rotation = profile.HoldRotation;

            // BeginComboStepAttack + BeginComboAttackTicks. SetDisplayWeapon(swing: true) restarts the
            // clock at useAnimation; only an authored-clock arc motion then resizes it to the step.
            void BeginStep(MeleeComboStep step)
            {
                int ticks = AttackPhaseTicks(profile, step, spec.WeaponUseAnimation);
                if (IsLandingTimedLeap(profile, step))
                {
                    // Ends on landing in game; flat-ground airtime is the best stand-in for that.
                    ticks = Math.Min(ticks, profile.LeapAirtimeTicks);
                }
                weaponAnimMax = spec.WeaponUseAnimation;
                if (profile.AuthoredClock && IsArcSwingMotion(step.Motion))
                {
                    weaponAnimMax = ticks;
                }
                weaponAnim = weaponAnimMax;
                activeStepTotalTicks = ticks;
                phase = PhaseAttack;
                phaseTimer = ticks;
                armed = ArmsAtStepStart(step.Motion);
            }

            for (int tick = 0; tick < MaxTicks; tick++)
            {
                // ---- PuppetAttackAI. Tick 0 is the tick the telegraph was entered on. ----
                if (tick > 0)
                {
                    if (phase == PhaseTelegraph)
                    {
                        weaponAnimMax = spec.WeaponUseAnimation;   // SetDisplayWeapon(swing: false)
                        phaseTimer--;
                        if (phaseTimer <= 0)
                        {
                            BeginStep(steps[stepIndex]);
                        }
                    }
                    else if (phase == PhaseAttack)
                    {
                        // Per-step HitWindowEnd: measured before the decrement, as in the game.
                        MeleeComboStep attacking = steps[stepIndex];
                        float stepProgress = (activeStepTotalTicks - phaseTimer) / (float)Math.Max(1, activeStepTotalTicks);
                        if (attacking.HitWindowEnd > 0f && stepProgress > attacking.HitWindowEnd)
                        {
                            armed = false;
                        }

                        phaseTimer--;
                        if (phaseTimer <= 0)
                        {
                            if (steps[stepIndex].Motion == ComboMotion.DoubleSpinSlam)
                            {
                                rotation = 1.4f;
                            }

                            armed = false;
                            if (stepIndex + 1 < steps.Length)
                            {
                                phase = PhasePause;
                                phaseTimer = Math.Max(1, steps[stepIndex].PostStepPause);
                            }
                            else
                            {
                                phase = PhaseRecovery;
                                phaseTimer = spec.RecoveryTicks;
                            }
                        }
                    }
                    else if (phase == PhasePause)
                    {
                        phaseTimer--;
                        if (phaseTimer <= 0)
                        {
                            stepIndex++;
                            BeginStep(steps[stepIndex]);
                        }
                    }
                    else
                    {
                        phaseTimer--;
                        if (phaseTimer <= 0)
                        {
                            break;
                        }
                    }
                }

                // ---- TickWeaponAnim ----
                if (weaponAnim > 0)
                {
                    weaponAnim--;
                }
                float t = 1f;
                if (weaponAnimMax > 0)
                {
                    t = 1f - weaponAnim / (float)weaponAnimMax;
                }

                MeleeComboStep current = steps[stepIndex];
                if (phase == PhaseRecovery)
                {
                    int lingerTicks = Math.Min(spec.RecoveryLingerTicks, Math.Max(1, spec.RecoveryTicks));
                    bool holding = spec.RecoveryLingerTicks > 0
                        && phaseTimer > Math.Max(0, spec.RecoveryTicks - lingerTicks);
                    if (!holding)
                    {
                        rotation = MathHelper.Lerp(rotation, profile.HoldRotation, 0.10f);
                    }
                }
                else
                {
                    bool hasNext = stepIndex + 1 < steps.Length;
                    MeleeComboStep next = hasNext ? steps[stepIndex + 1] : current;
                    rotation = ComboPose(spec, phase, current, next, hasNext, rotation, t,
                        phaseTimer, activeStepTotalTicks, notes);
                }

                // Leaps arm only on the landing tick (DoComboMeleeHit at endStep), i.e. the last
                // attack frame here since airtime is not simulated.
                bool frameArmed = armed;
                bool isLeap = current.Motion == ComboMotion.LeapSlam || current.Motion == ComboMotion.LeapThrust;
                if (phase == PhaseAttack && IsLandingTimedLeap(profile, current))
                {
                    // An in-air strike (LeapStrikeRange) sweeps through its downswing; otherwise the
                    // hit resolves once, on the landing frame.
                    frameArmed = phaseTimer == 1;
                    if (current.LeapStrikeRange > 0f)
                    {
                        frameArmed = phaseTimer <= LeapSlamDownswingTicks;
                        notes.Add($"leap strike: swings in the air once past the apex with the player within {current.LeapStrikeRange:0}px;" +
                                  " shown at the latest it can happen (the last 10 ticks before landing)");
                    }
                    notes.Add($"leap: lands after {profile.LeapAirtimeTicks} ticks here (flat-ground airtime); in game it ends on the real landing");
                }
                else if (phase == PhaseAttack && isLeap)
                {
                    frameArmed = phaseTimer == 1;
                    notes.Add($"{current.Motion}: runs its full {activeStepTotalTicks}-tick timer here; in game it ends on landing");
                }

                // Dodge roll through the puppet: MeleeComboPause re-faces the target on its first
                // tick, so the flip lands in the pause before the roll-through step and holds after.
                int direction = spec.Direction;
                if (spec.RollThroughStep > 1)
                {
                    int upcomingStep = stepIndex + 1;
                    if (phase == PhasePause)
                    {
                        upcomingStep = stepIndex + 2;
                    }
                    if (upcomingStep >= spec.RollThroughStep)
                    {
                        direction = -spec.Direction;
                    }
                }

                // Leaps pose on the jump frame while aloft; the last attack frame is the landing.
                bool airborne = spec.Airborne || (phase == PhaseAttack && isLeap && phaseTimer > 1);

                // WeaponSheathed (Gwyn: Wrath Flurry's recovery after the landing beat) hides the
                // weapon and the arm pose. Movement during it is not simulated - the preview stands still.
                bool weaponHidden = phase == PhaseRecovery && profile.IsSheathed(PhaseRecovery, phaseTimer);
                if (weaponHidden)
                {
                    notes.Add("recovery: weapon sheathed from here (WeaponSheathed); any walking it does in game is not simulated");
                }

                EmitFrame(spec, run, writer, poses, tick, phase, current.Motion, stepIndex, steps.Length, rotation, frameArmed,
                    direction, airborne, weaponHidden);
            }

            return poses;
        }

        /// <summary>
        /// Port of the MeleeComboTelegraph/Attack/Pause branch of PuppetNPC.TickWeaponAnim: the shared
        /// pause handoff first, then the per-motion switch. Keep the branch order and the magic
        /// fractions identical to the game - they ARE the behaviour being previewed.
        /// </summary>
        private static float ComboPose(SwingSpec spec, string phase, MeleeComboStep step, MeleeComboStep next,
            bool hasNext, float rotation, float t, int phaseTimer, int activeStepTotalTicks, SortedSet<string> notes)
        {
            PuppetProfile profile = spec.Profile;
            bool inTel = phase == PhaseTelegraph;
            bool inPause = phase == PhasePause;
            float telegraphT = 1f;
            if (inTel)
            {
                telegraphT = 1f - phaseTimer / (float)Math.Max(1, ComboTelegraphTicks(profile, step));
            }

            // Shared inter-step handoff. With a step linger: hold the landed pose, then SmoothStep
            // toward where the NEXT step starts. Aim-swing pilots without one: lerp 0.22 toward it.
            if (inPause && spec.InterStepLingerTicks > 0)
            {
                int pauseTotal = Math.Max(1, step.PostStepPause);
                int elapsedPause = Math.Max(0, pauseTotal - phaseTimer);
                int lingerTicks = Math.Min(spec.InterStepLingerTicks, pauseTotal);
                if (elapsedPause < lingerTicks)
                {
                    return rotation;
                }
                if (hasNext)
                {
                    float target = StepStartRotation(spec, next, notes);
                    int transitionTicks = Math.Max(1, pauseTotal - lingerTicks);
                    float transition = MathHelper.Clamp((elapsedPause - lingerTicks + 1f) / transitionTicks, 0f, 1f);
                    return MathHelper.SmoothStep(rotation, target, transition);
                }
            }
            else if (inPause && profile.AimSwingActive && hasNext)
            {
                float target = StepStartRotation(spec, next, notes);
                return MathHelper.Lerp(rotation, target, 0.22f);
            }

            (float a0, float a1) = Endpoints(spec, step.Motion,
                inTel ? "MeleeComboTelegraph" : "MeleeComboAttack");

            switch (step.Motion)
            {
                case ComboMotion.OverheadArc:
                case ComboMotion.UnderhandArc:
                {
                    float pauseFraction = step.Motion == ComboMotion.OverheadArc ? 0.130f : 0.150f;
                    if (inTel)
                    {
                        if (profile.LogicalTelegraphs)
                        {
                            return LogicalSwingWindup(a1, a0, telegraphT, profile);
                        }
                        return MathHelper.Lerp(rotation, a0, 0.30f);
                    }
                    if (inPause)
                    {
                        return MathHelper.Lerp(rotation, MathHelper.Lerp(a0, a1, pauseFraction), 0.20f);
                    }
                    return ApplySwingEase(profile, a0, a1, t, step);
                }
                case ComboMotion.HorizontalSweep:
                    if (inTel)
                    {
                        return MathHelper.Lerp(rotation, a0, 0.25f);
                    }
                    if (inPause)
                    {
                        return MathHelper.Lerp(rotation, MathHelper.Lerp(a0, a1, 0.700f), 0.18f);
                    }
                    return ApplySwingEase(profile, a0, a1, t, step);
                case ComboMotion.VerticalChop:
                    if (inTel)
                    {
                        return MathHelper.Lerp(rotation, a0, 0.32f);
                    }
                    if (inPause)
                    {
                        return MathHelper.Lerp(rotation, MathHelper.Lerp(a0, a1, 0.119f), 0.18f);
                    }
                    return ApplySwingEase(profile, a0, a1, t, step);
                case ComboMotion.Thrust:
                    if (inTel)
                    {
                        return MathHelper.Lerp(rotation, MathHelper.PiOver2, 0.20f);
                    }
                    if (inPause)
                    {
                        return MathHelper.Lerp(rotation, MathHelper.PiOver2 * 0.8f, 0.18f);
                    }
                    return MathHelper.Lerp(rotation, MathHelper.PiOver4, 0.42f);
                case ComboMotion.JoustDash:
                    if (inTel)
                    {
                        if (profile.LogicalTelegraphs)
                        {
                            return LogicalSwingWindup(a1, a0, telegraphT, profile);
                        }
                        return MathHelper.Lerp(rotation, a0, 0.18f);
                    }
                    if (inPause)
                    {
                        return a1;   // the game snaps straight to the landed pose
                    }
                    return ApplySwingEase(profile, a0, a1, t, step);
                case ComboMotion.Spin:
                {
                    // Fixed rate in EVERY phase, telegraph and pause included.
                    float spun = rotation + 0.28f;
                    if (spun > MathHelper.TwoPi)
                    {
                        spun -= MathHelper.TwoPi;
                    }
                    return spun;
                }
                case ComboMotion.IaidoDraw:
                    if (inTel)
                    {
                        return MathHelper.Lerp(rotation, a0, 0.15f);
                    }
                    return ApplySwingEase(profile, a0, a1, t, step);
                case ComboMotion.GroundSlam:
                    if (inTel)
                    {
                        return MathHelper.Lerp(rotation, a0, 0.25f);
                    }
                    return ApplySwingEase(profile, a0, a1, t, step);
                case ComboMotion.LeapSlam:
                    if (profile.LandingTimedLeapSlam)
                    {
                        // Port of the landing-timed branch + UpdateLeapSlamPose: carried through the
                        // flight, then SmoothStep carry -> impact over the last LeapSlamDownswingTicks,
                        // arriving at impact on the landing frame. Pauses hold the planted pose.
                        if (inTel)
                        {
                            return LogicalSwingWindup(profile.LeapImpactRotation, profile.LeapCarryRotation, telegraphT, profile);
                        }
                        if (inPause)
                        {
                            return rotation;
                        }
                        int elapsedInLeap = activeStepTotalTicks - phaseTimer;
                        float downswing = (elapsedInLeap + 1f - (activeStepTotalTicks - LeapSlamDownswingTicks)) / LeapSlamDownswingTicks;
                        downswing = MathHelper.Clamp(downswing, 0f, 1f);
                        return MathHelper.SmoothStep(profile.LeapCarryRotation, profile.LeapImpactRotation, downswing);
                    }
                    if (inTel)
                    {
                        if (profile.LogicalTelegraphs)
                        {
                            return LogicalSwingWindup(a1, a0, telegraphT, profile);
                        }
                        return MathHelper.Lerp(rotation, a0, 0.30f);
                    }
                    return ApplySwingEase(profile, a0, a1, t, step);
                case ComboMotion.LeapThrust:
                    if (inTel)
                    {
                        if (profile.LogicalTelegraphs)
                        {
                            return LogicalSwingWindup(a1, a0, telegraphT, profile);
                        }
                        return MathHelper.Lerp(rotation, a0, 0.22f);
                    }
                    return ApplySwingEase(profile, a0, a1, t, step);
                case ComboMotion.ChargeChop:
                    return MathHelper.Lerp(rotation, -0.95f, 0.20f);
                case ComboMotion.Feint:
                    if (inTel && profile.LogicalTelegraphs)
                    {
                        return LogicalSwingWindup(a1, a0, telegraphT, profile);
                    }
                    return MathHelper.Lerp(rotation, a0, 0.30f);
                case ComboMotion.DoubleSpinSlam:
                {
                    if (inTel)
                    {
                        if (profile.LogicalTelegraphs)
                        {
                            return LogicalSwingWindup(a1, a0, telegraphT, profile);
                        }
                        return MathHelper.Lerp(rotation, a0, 0.30f);
                    }
                    if (inPause)
                    {
                        return MathHelper.Lerp(rotation, a1, 0.30f);
                    }
                    const float circlePortion = 0.78f;
                    float twoCircleEnd = a0 + MathHelper.TwoPi * 2f;
                    float groundEnd = a1 + MathHelper.TwoPi * 2f;
                    if (t < circlePortion)
                    {
                        return MathHelper.Lerp(a0, twoCircleEnd, t / circlePortion);
                    }
                    return MathHelper.Lerp(twoCircleEnd, groundEnd,
                        MathHelper.SmoothStep(0f, 1f, (t - circlePortion) / (1f - circlePortion)));
                }
                case ComboMotion.ThrownWeaponRetrieve:
                    if (inTel)
                    {
                        return profile.HoldRotation + MathHelper.TwoPi * MathHelper.SmoothStep(0f, 1f, telegraphT);
                    }
                    return MathHelper.Lerp(rotation, 0.45f, 0.20f);
                case ComboMotion.LowAxeRun:
                    if (inTel)
                    {
                        return MathHelper.SmoothStep(profile.HoldRotation, 1.9f, telegraphT);
                    }
                    notes.Add("LowAxeRun ends when the target is in range in game: full timer shown");
                    return MathHelper.Lerp(rotation, 1.9f, 0.35f);
                case ComboMotion.RisingUppercutLeap:
                {
                    int elapsed = Math.Max(0, step.AttackTicks - phaseTimer);
                    float rise = MathHelper.Clamp(elapsed / (float)Math.Max(1, spec.WeaponUseAnimation), 0f, 1f);
                    return MathHelper.Lerp(1.9f, -1.0f, MathHelper.SmoothStep(0f, 1f, rise));
                }
                case ComboMotion.BackstepRaise:
                {
                    if (inTel)
                    {
                        return MathHelper.Lerp(rotation, 1.0f, 0.24f);
                    }
                    float elapsed = Math.Max(0, activeStepTotalTicks - phaseTimer);
                    float raise = MathHelper.Clamp(elapsed / 22f, 0f, 1f);
                    return MathHelper.SmoothStep(1.0f, -1.3f, raise);
                }
                case ComboMotion.ApexDiveCleave:
                    if (inTel)
                    {
                        return LogicalSwingWindup(1.0f, -1.3f, telegraphT, profile);
                    }
                    notes.Add("ApexDiveCleave's dive swing needs fall ticks: only the ascent carry is shown");
                    return MathHelper.Lerp(rotation, -1.05f, 0.24f);
                default:
                    notes.Add($"{step.Motion} has no combo pose in the game either: rotation held");
                    return rotation;
            }
        }

        /// <summary>Port of PuppetNPC.ApplySwingEase. Trapezoidal needs raw ticks, so it is honoured
        /// even with the clock off; everything else reads step.Ease only under the authored clock.</summary>
        private static float ApplySwingEase(PuppetProfile profile, float a0, float a1, float t, MeleeComboStep step)
        {
            if (step.Ease == SwingEaseStyle.Trapezoidal)
            {
                int totalTicks = Math.Max(1, step.AttackTicks);
                int elapsedTicks = (int)Math.Round(t * totalTicks);
                return SwingEase.ApplyTrapezoidal(a0, a1, elapsedTicks, totalTicks);
            }

            if (step.Ease == SwingEaseStyle.Weighted && profile.AuthoredClock)
            {
                int totalTicks = Math.Max(1, step.AttackTicks);
                return SwingEase.ApplyWeighted(a0, a1, t * totalTicks, totalTicks,
                    step.EaseInTicks, step.EaseOutTicks, step.EaseOutDecay);
            }

            if (profile.AuthoredClock)
            {
                return SwingEase.Apply(a0, a1, t, step.Ease);
            }
            return SwingEase.Apply(a0, a1, t, profile.UseSwingEasing);
        }

        private static (float, float) Endpoints(SwingSpec spec, ComboMotion motion, string phaseName = null)
        {
            (float a0, float a1) = WeaponArchetypeTables.SwingArcEndpoints(motion, spec.Profile.OverheadWindupOvershoot);
            spec.Profile.ModifyEndpoints(motion, ref a0, ref a1, phaseName);
            if (spec.FlipArc)
            {
                (a0, a1) = (a1, a0);
            }
            return (a0, a1);
        }

        /// <summary>Port of PuppetNPC.ComboStepStartRotation - deliberately a different table from
        /// SwingArcEndpoints (see the note on that method). JoustDash and Spin have no entry, so a
        /// handoff INTO them eases toward HoldRotation.</summary>
        private static float StepStartRotation(SwingSpec spec, MeleeComboStep step, SortedSet<string> notes)
        {
            PuppetProfile profile = spec.Profile;
            float overshoot = profile.OverheadWindupOvershoot;
            float hold = profile.HoldRotation;

            if (profile.LandingTimedLeapSlam && step.Motion == ComboMotion.LeapSlam)
            {
                return profile.LeapCarryRotation;
            }

            float a0;
            float a1;
            switch (step.Motion)
            {
                case ComboMotion.OverheadArc: a0 = -1.3f - overshoot; a1 = 1.0f; break;
                case ComboMotion.UnderhandArc: a0 = 1.0f; a1 = -1.0f; break;
                case ComboMotion.HorizontalSweep: a0 = -0.4f; a1 = 0.6f; break;
                case ComboMotion.VerticalChop: a0 = -1.55f - overshoot; a1 = 1.4f; break;
                case ComboMotion.GroundSlam: a0 = -1.55f - overshoot; a1 = 1.5f; break;
                case ComboMotion.IaidoDraw: a0 = 1.2f; a1 = -0.5f; break;
                case ComboMotion.Feint: a0 = -1.3f; a1 = -1.3f; break;
                case ComboMotion.ChargeChop: a0 = -0.95f; a1 = -0.95f; break;
                case ComboMotion.DoubleSpinSlam: a0 = -1.3f - overshoot; a1 = 1.4f; break;
                case ComboMotion.LeapSlam: a0 = -1.45f - overshoot; a1 = 1.4f; break;
                case ComboMotion.LeapThrust: a0 = MathHelper.PiOver2 * 0.8f; a1 = MathHelper.PiOver4; break;
                case ComboMotion.LowAxeRun: a0 = 1.9f; a1 = 1.9f; break;
                case ComboMotion.RisingUppercutLeap: a0 = 1.9f; a1 = -1.0f; break;
                case ComboMotion.BackstepRaise: a0 = 1.0f; a1 = -1.3f; break;
                case ComboMotion.ApexDiveCleave: a0 = -1.3f; a1 = 1.25f; break;
                default: a0 = hold; a1 = hold; break;
            }

            profile.ModifyEndpoints(step.Motion, ref a0, ref a1);
            if (spec.FlipArc)
            {
                (a0, a1) = (a1, a0);
            }
            return a0;
        }

        /// <summary>PuppetNPC.LeapSlamDownswingTicks (private const).</summary>
        private const int LeapSlamDownswingTicks = 10;

        private static bool IsLandingTimedLeap(PuppetProfile profile, MeleeComboStep step)
        {
            return profile.LandingTimedLeapSlam && step.Motion == ComboMotion.LeapSlam;
        }

        /// <summary>PuppetNPC.GetComboTelegraphTicks.</summary>
        private static int ComboTelegraphTicks(PuppetProfile profile, MeleeComboStep step)
        {
            return Math.Max(profile.MinComboTelegraphTicks, (int)(step.TelegraphTicks * profile.ComboTelegraphMultiplier));
        }

        /// <summary>Attack-phase length: BeginComboAttackTicks. GetMeleeSwingTicks returns the step's
        /// AttackTicks unchanged when positive, else the weapon's useAnimation.</summary>
        private static int AttackPhaseTicks(PuppetProfile profile, MeleeComboStep step, int weaponUseAnimation)
        {
            if (profile.AuthoredClock && IsArcSwingMotion(step.Motion))
            {
                float mult = step.SwingSpeedMult > 0f ? step.SwingSpeedMult : 1f;
                return Math.Max(6, (int)Math.Round(step.AttackTicks / mult));
            }
            if (step.AttackTicks > 0)
            {
                return step.AttackTicks;
            }
            return weaponUseAnimation;
        }

        /// <summary>PuppetNPC.IsArcSwingMotion - the motions the authored clock resizes.</summary>
        private static bool IsArcSwingMotion(ComboMotion motion)
        {
            return motion == ComboMotion.OverheadArc || motion == ComboMotion.UnderhandArc
                || motion == ComboMotion.HorizontalSweep || motion == ComboMotion.VerticalChop
                || motion == ComboMotion.GroundSlam || motion == ComboMotion.IaidoDraw
                || motion == ComboMotion.DoubleSpinSlam;
        }

        /// <summary>BeginComboStepAttack's DoComboMeleeHit exclusions: these either hit later (leaps,
        /// apex dive) or never (carries, feints). Armed steps stay armed for the whole attack phase.</summary>
        private static bool ArmsAtStepStart(ComboMotion motion)
        {
            switch (motion)
            {
                case ComboMotion.LeapSlam:
                case ComboMotion.LeapThrust:
                case ComboMotion.BackstepRaise:
                case ComboMotion.ApexDiveCleave:
                case ComboMotion.ThrownWeaponRetrieve:
                case ComboMotion.ChargeChop:
                case ComboMotion.Feint:
                case ComboMotion.LowAxeRun:
                    return false;
                default:
                    return true;
            }
        }

        // ── V2 clip ───────────────────────────────────────────────────────────────

        /// <summary>
        /// Runs a RuntimeV2Clip through the mod's own PuppetAttackRuntime - the real class, not a
        /// port - in TickAttackRuntimeV2's order: sample, then advance. No aim update, which is the
        /// level-target case (correction stays 0). The clip owns recovery too, so
        /// MeleeRecoveryLingerTicks never applies (TickWeaponAnim returns early while V2 is active).
        /// </summary>
        private static List<PoseFrame> SimulateV2(SwingSpec spec, int run, StreamWriter writer)
        {
            var poses = new List<PoseFrame>();
            var runtime = new PuppetAttackRuntime();
            runtime.Start(spec.V2Clip, spec.Direction);
            ComboMotion motion = spec.Steps[0].Motion;

            for (int tick = 0; tick < MaxTicks && runtime.Active; tick++)
            {
                float rotation = runtime.SampleRotation();

                string phase = PhaseRecovery;
                if (runtime.Stage == PuppetAttackStage.Windup)
                {
                    phase = PhaseTelegraph;
                }
                else if (runtime.Stage == PuppetAttackStage.Active)
                {
                    phase = PhaseAttack;
                }

                EmitFrame(spec, run, writer, poses, tick, phase, motion, 0, 1, rotation, runtime.HitWindowOpen,
                    spec.Direction, spec.Airborne, false);
                runtime.Advance(out bool _);
            }

            return poses;
        }

        // ── Shared ────────────────────────────────────────────────────────────────

        private static void EmitFrame(SwingSpec spec, int run, StreamWriter writer, List<PoseFrame> poses,
            int tick, string phase, ComboMotion motion, int stepIndex, int stepCount, float rotation, bool armed,
            int direction, bool airborne, bool weaponHidden)
        {
            WriteFrame(writer, spec, run, tick, phase, motion, rotation, armed);

            // Vanilla composite-arm space: 0 = arm hanging straight down, -PI/2 = level forward.
            // The mod's swing space differs by exactly -PI/2, then mirrors with facing.
            float compositeArmRotation = (rotation - MathHelper.PiOver2) * direction;

            // Airborne uses body row 5, which is also where vanilla suppresses the shoulder caps
            // (CreateCompositeData case 5) unless the armour opts into showsShouldersWhileJumping.
            int bodyRow = 5;
            if (!airborne)
            {
                bodyRow = PuppetNPC.BodyRowFromWeaponRotation(rotation, direction, spec.WeaponRotationOffset);
            }

            poses.Add(new PoseFrame
            {
                Tick = tick,
                Phase = phase,
                Armed = armed,
                Airborne = airborne,
                WeaponRotation = rotation,
                CompositeArmRotation = compositeArmRotation,
                Direction = direction,
                BodyRow = bodyRow,
                LegRow = airborne ? 5 : 0,
                StepIndex = stepIndex,
                StepCount = stepCount,
                Motion = motion.ToString(),
                WeaponHidden = weaponHidden,
            });
        }

        /// <summary>Port of PuppetNPC.LogicalSwingWindup - settle from the carry pose toward the
        /// opposite end for LogicalWindupSettleFraction of the telegraph (0.25 by default, read per
        /// combo), then SmoothStep to the arc start, arriving exactly on it. Kept here because the
        /// original is a private instance method.</summary>
        private static float LogicalSwingWindup(float oppositeEnd, float attackStart, float progress, PuppetProfile profile)
        {
            progress = MathHelper.Clamp(progress, 0f, 1f);
            float settleFraction = MathHelper.Clamp(profile.WindupSettleFraction, 0.05f, 0.95f);

            if (progress < settleFraction)
            {
                float settle = MathHelper.SmoothStep(0f, 1f, progress / settleFraction);
                return MathHelper.Lerp(profile.HoldRotation, oppositeEnd, settle);
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
            string phase, ComboMotion motion, float rotation, bool armed)
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
                $"\"motion\":\"{motion}\",\"dir\":1,\"spriteDir\":1,\"lockedDir\":1," +
                $"\"npc\":[{F(npc.X)},{F(npc.Y)}],\"hand\":[{F(hand.X)},{F(hand.Y)}]," +
                $"\"visualTip\":[{F(tip.X)},{F(tip.Y)}],\"collisionTip\":[{F(tip.X)},{F(tip.Y)}]," +
                $"\"rawRotDeg\":{F(MathHelper.ToDegrees(rotation))}," +
                $"\"drawRotDeg\":{F(MathHelper.ToDegrees(rotation))}," +
                $"\"armWeaponErrorDeg\":0,\"bladeArmed\":{armedText}}}");
        }
    }
}

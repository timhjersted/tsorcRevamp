using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using tsorcRevamp.NPCs.Puppets;
using tsorcRevamp.Utilities;

namespace SwingPreview
{
    /// <summary>One run to simulate: a whole combo chained the way the game chains it (default), a
    /// single step in isolation (--per-step), or a bare motion built from CLI args (--motion).</summary>
    internal sealed class SwingSpec
    {
        public string Puppet = "Preview";
        public string Label = "Swing";

        /// <summary>The combo this run came from, bound onto the puppet instance before simulating so
        /// overrides keyed on ActiveMeleeComboName resolve for it (see PuppetProfile.Bind).</summary>
        public MeleeCombo Combo;
        public MeleeComboStep[] Steps;

        /// <summary>Set when the combo carries a RuntimeV2Clip. The game then ignores Steps' motion,
        /// ease and timing entirely and plays the clip on its own clock, so the preview does too.</summary>
        public PuppetAttackClip V2Clip;

        public PuppetProfile Profile;
        public int RecoveryTicks = 30;
        public int RecoveryLingerTicks;
        public int InterStepLingerTicks;
        public int WeaponUseAnimation;
        public float Reach = 60f;
        public bool FlipArc;
        public int Direction = 1;

        /// <summary>1-based step the target is behind for (a dodge roll through the puppet). The
        /// game re-faces during MeleeComboPause, so facing flips in the pause BEFORE this step.
        /// 0 = the target never moves.</summary>
        public int RollThroughStep;
        public bool Airborne;
        public float WeaponRotationOffset;

        public string StepSummary()
        {
            if (V2Clip != null)
            {
                return $"V2 clip windup={V2Clip.WindupTicks} active={V2Clip.ActiveTicks} recovery={V2Clip.RecoveryTicks} ease={V2Clip.SwingEase}";
            }
            return string.Join(" -> ", Steps.Select(step =>
                $"{step.Motion}({step.TelegraphTicks}/{step.AttackTicks}/{step.PostStepPause},{step.Ease})"));
        }
    }

    internal sealed class Options
    {
        public string OutFile = Path.Combine(Path.GetTempPath(), "swing-preview.jsonl");
        public string Archetype;
        public string Combo;
        public string Motion;
        public string Ease;
        public string Puppet = "Preview";
        public int Telegraph = -1;
        public int Attack = -1;
        public int Recovery = -1;
        public int Linger = -1;
        public int StepLinger = -1;
        public int UseAnim = -1;
        public int RollThrough;
        public string Clock;
        public float SwingMult = -1f;
        public float Overshoot = float.NaN;
        public bool List;
        public bool CompareEases;
        public bool PerStep;
        public bool Profile;
        public bool Body;
        public bool Airborne;
        public bool VanillaShoulderOrder;
        public int Zoom = 3;
        public string BodyOutDir;
        public string RepoRoot;
        public float WeaponScale = -1f;

        public static void PrintUsage()
        {
            Console.WriteLine(@"
SwingPreview - run the mod's real swing maths headless and emit telemetry JSONL.

  --list                          list archetypes and their combo names, then exit
  --puppet <class>                a PuppetNPC subclass (Gwyn, Artorias, ...): its pool AND its real
                                  flags (authored clock, easing, telegraph scaling, lingers, weapon
                                  useAnimation, arc overrides) are read from the compiled mod
  --archetype <name>              Greatsword | Broadsword | Axe | ... (base PuppetNPC flags unless
                                  --puppet is also given)
  --combo <substring>             only combos whose name contains this
  --per-step                      render each step alone with its own telegraph instead of the chain
  --profile                       print every phase's speed profile: sweep, peak deg/tick, the first-
                                  frame jump into it, armed ticks, and the deg/tick of every frame
  --motion <ComboMotion>          prototype a single motion instead of a table combo
  --ease <Linear|Smooth|Snap|Whip|Trapezoidal>   override every step's authored Ease
  --compare-eases                 emit the same run once per ease style, to A/B curves
  --clock <on|off>                override UseAuthoredComboSwingClock (gate 1 of 3)
  --step-linger <ticks>           override MeleeComboInterStepLingerTicks (the between-hits hold)
  --roll-through <step>           the player rolls behind before this step (1-based): the puppet
                                  re-faces in the preceding pause, as the game does
  --telegraph / --attack / --recovery / --linger <ticks>
  --useanim <ticks>               override the weapon's useAnimation
  --swingmult <float>             AttackTicks are divided by this, as the authored clock does
  --overshoot <float>             override OverheadWindupOvershoot
  --body                          also composite the real sprite sheets -> strip + contact sheet + HTML player
  --airborne                      pose on the jump body frame (row 5), where vanilla hides the shoulder caps
  --vanilla-shoulder-order        draw the UNFIXED shoulder order (arm flips in front of the pauldron on
                                  body rows 1/2/5) instead of the order PuppetShoulderCapLayer forces
  --zoom <int>                    body render scale, default 3
  --weaponscale <float>           weapon sprite scale calibration, default 0.45
  --bodyout <path>                default: tsorcDocs\SwingReports
  --out <path>                    default: %TEMP%\swing-preview.jsonl

Examples:
  SwingPreview --list
  SwingPreview --puppet Gwyn --combo ""Wrath Flurry"" --body
  SwingPreview --puppet Gwyn --combo ""Wrath Flurry"" --step-linger 6 --body
  SwingPreview --archetype Greatsword --combo ""Heavy Chop"" --compare-eases
  SwingPreview --motion OverheadArc --telegraph 45 --attack 26 --ease Whip --overshoot 0.18
");
        }

        public static Options Parse(string[] args)
        {
            var options = new Options();

            for (int i = 0; i < args.Length; i++)
            {
                string key = args[i].ToLowerInvariant();
                string Next()
                {
                    if (i + 1 >= args.Length)
                    {
                        throw new ArgumentException($"{key} needs a value");
                    }
                    return args[++i];
                }

                switch (key)
                {
                    case "--list": options.List = true; break;
                    case "--compare-eases": options.CompareEases = true; break;
                    case "--per-step": options.PerStep = true; break;
                    case "--profile": options.Profile = true; break;
                    case "--body": options.Body = true; break;
                    case "--airborne": options.Airborne = true; break;
                    case "--vanilla-shoulder-order": options.VanillaShoulderOrder = true; break;
                    case "--zoom": options.Zoom = int.Parse(Next(), CultureInfo.InvariantCulture); break;
                    case "--bodyout": options.BodyOutDir = Next(); break;
                    case "--weaponscale": options.WeaponScale = float.Parse(Next(), CultureInfo.InvariantCulture); break;
                    case "--archetype": options.Archetype = Next(); break;
                    case "--combo": options.Combo = Next(); break;
                    case "--motion": options.Motion = Next(); break;
                    case "--ease": options.Ease = Next(); break;
                    case "--clock": options.Clock = Next(); break;
                    case "--puppet": options.Puppet = Next(); break;
                    case "--out": options.OutFile = Next(); break;
                    case "--telegraph": options.Telegraph = int.Parse(Next(), CultureInfo.InvariantCulture); break;
                    case "--attack": options.Attack = int.Parse(Next(), CultureInfo.InvariantCulture); break;
                    case "--recovery": options.Recovery = int.Parse(Next(), CultureInfo.InvariantCulture); break;
                    case "--linger": options.Linger = int.Parse(Next(), CultureInfo.InvariantCulture); break;
                    case "--step-linger": options.StepLinger = int.Parse(Next(), CultureInfo.InvariantCulture); break;
                    case "--useanim": options.UseAnim = int.Parse(Next(), CultureInfo.InvariantCulture); break;
                    case "--roll-through": options.RollThrough = int.Parse(Next(), CultureInfo.InvariantCulture); break;
                    case "--swingmult": options.SwingMult = float.Parse(Next(), CultureInfo.InvariantCulture); break;
                    case "--overshoot": options.Overshoot = float.Parse(Next(), CultureInfo.InvariantCulture); break;
                    case "-h":
                    case "--help": return null;
                    default:
                        Console.WriteLine($"unknown option: {args[i]}");
                        return null;
                }
            }

            // Walk up from the built binary (bin/Debug/net8.0) to the repo root.
            options.RepoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", ".."));
            if (string.IsNullOrWhiteSpace(options.BodyOutDir))
            {
                options.BodyOutDir = Path.Combine(options.RepoRoot, "tsorcDocs", "SwingReports");
            }

            return options;
        }

        /// <summary>The profile every spec in this invocation shares, with CLI overrides applied on
        /// top of what was read from the mod. Printed once so a run always says which gates it used.</summary>
        public PuppetProfile BuildProfile()
        {
            PuppetProfile profile = PuppetProfile.For(Puppet);

            // An explicit archetype replaces the puppet's own pool but keeps its flags, so a boss
            // can be previewed swinging a generic table.
            if (!string.IsNullOrWhiteSpace(Archetype)
                && Enum.TryParse(Archetype, ignoreCase: true, out WeaponArchetype parsed))
            {
                profile.Pool = WeaponArchetypeTables.GetMeleeCombos(parsed);
            }

            if (!string.IsNullOrWhiteSpace(Clock))
            {
                profile.AuthoredClock = Clock.Equals("on", StringComparison.OrdinalIgnoreCase);
            }
            if (!float.IsNaN(Overshoot))
            {
                profile.OverheadWindupOvershoot = Overshoot;
            }
            if (UseAnim > 0)
            {
                profile.WeaponUseAnimation = UseAnim;
            }

            // --step-linger / --linger are applied per spec in NewSpec, after Bind re-reads the
            // puppet's own values for that combo.
            return profile;
        }

        public List<SwingSpec> BuildSpecs(PuppetProfile profile)
        {
            var specs = new List<SwingSpec>();

            if (List)
            {
                foreach (WeaponArchetype archetype in Enum.GetValues(typeof(WeaponArchetype)).Cast<WeaponArchetype>())
                {
                    MeleeCombo[] table = WeaponArchetypeTables.GetMeleeCombos(archetype);
                    if (table == null) { continue; }
                    Console.WriteLine($"{archetype}:");
                    PrintPool(table);
                }

                // Bosses carrying their own pool instead of a generic archetype table. Without this
                // their combos are invisible to --list and look like they do not exist.
                foreach (string name in PuppetArtLibrary.Known.Split(',').Select(part => part.Trim()))
                {
                    PuppetProfile bespoke = PuppetProfile.For(name);
                    if (bespoke.Pool == null) { continue; }

                    Console.WriteLine($"{name} (use --puppet {name}):");
                    PrintPool(bespoke.Pool);
                }

                return specs;
            }

            // Prototyping path: a bare motion, no combo table involved.
            if (!string.IsNullOrWhiteSpace(Motion))
            {
                if (!Enum.TryParse(Motion, ignoreCase: true, out ComboMotion motion))
                {
                    Console.WriteLine($"unknown motion: {Motion}");
                    return specs;
                }
                var step = new MeleeComboStep
                {
                    Motion = motion,
                    TelegraphTicks = 30,
                    AttackTicks = 26,
                    Ease = SwingEaseStyle.Smooth,
                };
                var prototype = new MeleeCombo { Name = motion.ToString(), Steps = new[] { step } };
                SwingSpec spec = NewSpec(profile, prototype, prototype.Name, prototype.Steps);
                AddWithEaseVariants(specs, spec);
                return specs;
            }

            if (profile.Pool == null)
            {
                Console.WriteLine("Pass --archetype (see --list), --puppet <boss with its own pool>, --motion, or --list.");
                return specs;
            }

            foreach (MeleeCombo authored in profile.Pool)
            {
                if (!string.IsNullOrWhiteSpace(Combo) &&
                    authored.Name.IndexOf(Combo, StringComparison.OrdinalIgnoreCase) < 0)
                {
                    continue;
                }

                MeleeCombo combo = profile.Customize(authored);

                if (!PerStep || combo.RuntimeV2Clip != null || combo.Steps.Length == 1)
                {
                    SwingSpec spec = NewSpec(profile, combo, combo.Name, combo.Steps);
                    AddWithEaseVariants(specs, spec);
                    continue;
                }

                // Isolated view: each step with its own telegraph and recovery, which the game never
                // plays (steps 2+ start straight from the pause). Useful for clean per-arc metrics.
                for (int stepIndex = 0; stepIndex < combo.Steps.Length; stepIndex++)
                {
                    MeleeComboStep step = combo.Steps[stepIndex];
                    step.PostStepPause = 0;
                    string label = $"{combo.Name} [{stepIndex + 1}of{combo.Steps.Length}]";
                    var isolated = combo;
                    isolated.RuntimeV2Clip = null;
                    SwingSpec spec = NewSpec(profile, isolated, label, new[] { step });
                    AddWithEaseVariants(specs, spec);
                }
            }

            return specs;
        }

        private static void PrintPool(MeleeCombo[] pool)
        {
            foreach (MeleeCombo combo in pool)
            {
                string steps = string.Join(" -> ", combo.Steps.Select(s =>
                    $"{s.Motion}({s.TelegraphTicks}/{s.AttackTicks},{s.Ease})"));
                string v2 = combo.RuntimeV2Clip != null ? "  [V2 clip - steps unused]" : "";
                Console.WriteLine($"    {combo.Name,-18} {steps}{v2}");
            }
        }

        private SwingSpec NewSpec(PuppetProfile profile, MeleeCombo combo, string label, MeleeComboStep[] steps)
        {
            // Lingers can be keyed on the live combo (Gwyn's Wrath Flurry), so read them bound.
            profile.Bind(combo);

            var spec = new SwingSpec
            {
                Puppet = Puppet,
                Label = label,
                Combo = combo,
                Steps = (MeleeComboStep[])steps.Clone(),
                V2Clip = combo.RuntimeV2Clip,
                Profile = profile,
                RecoveryTicks = combo.RecoveryTicks > 0 ? combo.RecoveryTicks : profile.DefaultRecoveryTicks,
                RecoveryLingerTicks = profile.RecoveryLingerTicks,
                InterStepLingerTicks = profile.InterStepLingerTicks,
                WeaponUseAnimation = profile.WeaponUseAnimation,
                RollThroughStep = RollThrough,
                Airborne = Airborne,
            };

            if (Recovery >= 0) { spec.RecoveryTicks = Recovery; }
            if (StepLinger >= 0) { spec.InterStepLingerTicks = StepLinger; }
            if (Linger >= 0) { spec.RecoveryLingerTicks = Linger; }

            for (int i = 0; i < spec.Steps.Length; i++)
            {
                MeleeComboStep step = spec.Steps[i];
                if (Telegraph >= 0 && i == 0) { step.TelegraphTicks = Telegraph; }
                if (Attack >= 0) { step.AttackTicks = Attack; }
                if (SwingMult > 0f) { step.SwingSpeedMult = SwingMult; }
                if (!string.IsNullOrWhiteSpace(Ease) &&
                    Enum.TryParse(Ease, ignoreCase: true, out SwingEaseStyle parsed))
                {
                    step.Ease = parsed;
                }
                spec.Steps[i] = step;
            }

            return spec;
        }

        private void AddWithEaseVariants(List<SwingSpec> specs, SwingSpec spec)
        {
            if (!CompareEases || spec.V2Clip != null)
            {
                specs.Add(spec);
                return;
            }

            foreach (SwingEaseStyle style in new[]
                     {
                         SwingEaseStyle.Linear, SwingEaseStyle.Smooth,
                         SwingEaseStyle.Snap, SwingEaseStyle.Whip, SwingEaseStyle.Trapezoidal
                     })
            {
                MeleeComboStep[] steps = (MeleeComboStep[])spec.Steps.Clone();
                for (int i = 0; i < steps.Length; i++)
                {
                    steps[i].Ease = style;
                }

                specs.Add(new SwingSpec
                {
                    Puppet = spec.Puppet,
                    Label = $"{spec.Label} [{style}]",
                    Combo = spec.Combo,
                    Steps = steps,
                    Profile = spec.Profile,
                    RecoveryTicks = spec.RecoveryTicks,
                    RecoveryLingerTicks = spec.RecoveryLingerTicks,
                    InterStepLingerTicks = spec.InterStepLingerTicks,
                    WeaponUseAnimation = spec.WeaponUseAnimation,
                    Reach = spec.Reach,
                    FlipArc = spec.FlipArc,
                    Direction = spec.Direction,
                    RollThroughStep = spec.RollThroughStep,
                    Airborne = spec.Airborne,
                    WeaponRotationOffset = spec.WeaponRotationOffset,
                });
            }
        }
    }
}

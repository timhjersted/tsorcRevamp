using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using tsorcRevamp.NPCs.Puppets;
using tsorcRevamp.Utilities;

namespace SwingPreview
{
    /// <summary>One swing to simulate. Either pulled from a real combo table or built from CLI args
    /// when prototyping a move that does not exist yet.</summary>
    internal sealed class SwingSpec
    {
        public string Puppet = "Preview";
        public string Label = "Swing";
        public ComboMotion Motion = ComboMotion.OverheadArc;
        public SwingEaseStyle Ease = SwingEaseStyle.Smooth;
        public int TelegraphTicks = 30;
        public int AttackTicks = 26;
        public int RecoveryTicks = 30;
        public int RecoveryLingerTicks = 30;
        public float SwingSpeedMult = 1f;
        public float OverheadWindupOvershoot;
        public float HoldRotation = 0.78f;   // PuppetNPC's default carried broadsword angle
        public float Reach = 60f;
        public bool UseLogicalTelegraph = true;
        public bool FlipArc;
        public float ArmFrom = 0.15f;
        public float ArmTo = 0.75f;
        public int Direction = 1;
        public bool Airborne;
        public float WeaponRotationOffset;
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
        public float SwingMult = -1f;
        public float Overshoot = 0f;
        public bool List;
        public bool CompareEases;
        public bool Body;
        public bool Airborne;
        public int Zoom = 3;
        public string BodyOutDir;
        public string RepoRoot;
        public float WeaponScale = -1f;

        public static void PrintUsage()
        {
            Console.WriteLine(@"
SwingPreview - run the mod's real swing maths headless and emit telemetry JSONL.

  --list                          list archetypes and their combo names, then exit
  --archetype <name>              Greatsword | Broadsword | Axe | Hammer | Katana | ...
  --combo <substring>             only combos whose name contains this
  --motion <ComboMotion>          prototype a single motion instead of a table combo
  --ease <Linear|Smooth|Snap|Whip|Trapezoidal>
  --compare-eases                 emit the same swing once per ease style, to A/B curves
  --telegraph / --attack / --recovery / --linger <ticks>
  --swingmult <float>             AttackTicks are divided by this, as the authored clock does
  --overshoot <float>             OverheadWindupOvershoot for this puppet
  --puppet <name>                 label; with --body it also picks the sprite set (Gwyn, Artorias)
  --body                          also composite the real sprite sheets -> strip + contact sheet + HTML player
  --airborne                      pose on the jump body frame (row 5), where vanilla hides the shoulder caps
  --zoom <int>                    body render scale, default 3
  --weaponscale <float>           weapon sprite scale calibration, default 0.45
  --bodyout <path>                default: tsorcDocs\SwingReports
  --out <path>                    default: %TEMP%\swing-preview.jsonl

Examples:
  SwingPreview --list
  SwingPreview --archetype Greatsword
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
                    case "--body": options.Body = true; break;
                    case "--airborne": options.Airborne = true; break;
                    case "--zoom": options.Zoom = int.Parse(Next(), CultureInfo.InvariantCulture); break;
                    case "--bodyout": options.BodyOutDir = Next(); break;
                    case "--weaponscale": options.WeaponScale = float.Parse(Next(), CultureInfo.InvariantCulture); break;
                    case "--archetype": options.Archetype = Next(); break;
                    case "--combo": options.Combo = Next(); break;
                    case "--motion": options.Motion = Next(); break;
                    case "--ease": options.Ease = Next(); break;
                    case "--puppet": options.Puppet = Next(); break;
                    case "--out": options.OutFile = Next(); break;
                    case "--telegraph": options.Telegraph = int.Parse(Next(), CultureInfo.InvariantCulture); break;
                    case "--attack": options.Attack = int.Parse(Next(), CultureInfo.InvariantCulture); break;
                    case "--recovery": options.Recovery = int.Parse(Next(), CultureInfo.InvariantCulture); break;
                    case "--linger": options.Linger = int.Parse(Next(), CultureInfo.InvariantCulture); break;
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

        private static MeleeCombo[] TableFor(string archetype)
        {
            if (string.IsNullOrWhiteSpace(archetype)) { return null; }
            if (!Enum.TryParse(archetype, ignoreCase: true, out WeaponArchetype parsed)) { return null; }
            return WeaponArchetypeTables.GetMeleeCombos(parsed);
        }

        public List<SwingSpec> BuildSpecs()
        {
            var specs = new List<SwingSpec>();

            if (List)
            {
                foreach (WeaponArchetype archetype in Enum.GetValues(typeof(WeaponArchetype)).Cast<WeaponArchetype>())
                {
                    MeleeCombo[] table = WeaponArchetypeTables.GetMeleeCombos(archetype);
                    if (table == null) { continue; }
                    Console.WriteLine($"{archetype}:");
                    foreach (MeleeCombo combo in table)
                    {
                        string steps = string.Join(" -> ", combo.Steps.Select(s =>
                            $"{s.Motion}({s.TelegraphTicks}/{s.AttackTicks},{s.Ease})"));
                        Console.WriteLine($"    {combo.Name,-18} {steps}");
                    }
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
                var spec = new SwingSpec
                {
                    Puppet = Puppet,
                    Label = motion.ToString(),
                    Motion = motion,
                    OverheadWindupOvershoot = Overshoot,
                };
                ApplyOverrides(spec);
                AddWithEaseVariants(specs, spec);
                return specs;
            }

            MeleeCombo[] pool = TableFor(Archetype);
            if (pool == null)
            {
                Console.WriteLine("Pass --archetype (see --list), --motion, or --list.");
                return specs;
            }

            foreach (MeleeCombo combo in pool)
            {
                if (!string.IsNullOrWhiteSpace(Combo) &&
                    combo.Name.IndexOf(Combo, StringComparison.OrdinalIgnoreCase) < 0)
                {
                    continue;
                }

                for (int stepIndex = 0; stepIndex < combo.Steps.Length; stepIndex++)
                {
                    MeleeComboStep step = combo.Steps[stepIndex];
                    var spec = new SwingSpec
                    {
                        Puppet = Puppet,
                        Label = combo.Steps.Length > 1
                            ? $"{combo.Name} [{stepIndex + 1}/{combo.Steps.Length}]"
                            : combo.Name,
                        Motion = step.Motion,
                        Ease = step.Ease,
                        TelegraphTicks = step.TelegraphTicks,
                        AttackTicks = step.AttackTicks,
                        RecoveryTicks = combo.RecoveryTicks > 0 ? combo.RecoveryTicks : 30,
                        SwingSpeedMult = step.SwingSpeedMult > 0f ? step.SwingSpeedMult : 1f,
                        OverheadWindupOvershoot = Overshoot,
                    };
                    ApplyOverrides(spec);
                    AddWithEaseVariants(specs, spec);
                }
            }

            return specs;
        }

        private void ApplyOverrides(SwingSpec spec)
        {
            if (Telegraph >= 0) { spec.TelegraphTicks = Telegraph; }
            if (Attack >= 0) { spec.AttackTicks = Attack; }
            if (Recovery >= 0) { spec.RecoveryTicks = Recovery; }
            if (Linger >= 0) { spec.RecoveryLingerTicks = Linger; }
            if (SwingMult > 0f) { spec.SwingSpeedMult = SwingMult; }
            spec.Airborne = Airborne;
            if (!string.IsNullOrWhiteSpace(Ease) &&
                Enum.TryParse(Ease, ignoreCase: true, out SwingEaseStyle parsed))
            {
                spec.Ease = parsed;
            }
            spec.RecoveryLingerTicks = Math.Min(spec.RecoveryLingerTicks, spec.RecoveryTicks);
        }

        private void AddWithEaseVariants(List<SwingSpec> specs, SwingSpec spec)
        {
            if (!CompareEases)
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
                specs.Add(new SwingSpec
                {
                    Puppet = spec.Puppet,
                    Label = $"{spec.Label} [{style}]",
                    Motion = spec.Motion,
                    Ease = style,
                    Direction = spec.Direction,
                    Airborne = spec.Airborne,
                    WeaponRotationOffset = spec.WeaponRotationOffset,
                    TelegraphTicks = spec.TelegraphTicks,
                    AttackTicks = spec.AttackTicks,
                    RecoveryTicks = spec.RecoveryTicks,
                    RecoveryLingerTicks = spec.RecoveryLingerTicks,
                    SwingSpeedMult = spec.SwingSpeedMult,
                    OverheadWindupOvershoot = spec.OverheadWindupOvershoot,
                    HoldRotation = spec.HoldRotation,
                    Reach = spec.Reach,
                    UseLogicalTelegraph = spec.UseLogicalTelegraph,
                    FlipArc = spec.FlipArc,
                    ArmFrom = spec.ArmFrom,
                    ArmTo = spec.ArmTo,
                });
            }
        }
    }
}

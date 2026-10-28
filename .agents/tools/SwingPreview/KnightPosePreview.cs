using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Xna.Framework;
using Terraria;
using tsorcRevamp;
using tsorcRevamp.NPCs;
using tsorcRevamp.NPCs.Enemies;

namespace SwingPreview
{
    // --knight-poses [all | <KnightSpecialAttack> | DominionFinale] [--great] [--profile] [--body] [--zoom N] [--left]
    //               [--target X,Y]
    //
    // Renders the Red Knight family's controller-attack poses (throws, bomb, thrust, palm casts; with --great the Great
    // Red Knight's two-handed heave, Dominion plant and finale) through the game's own
    // RedKnightPuppet.ResolveControllerPose. The pose cues come from a real RedKnightAttackController with its Attack and
    // Timer set by reflection (the class is internal), so the preview reads the same release ticks the game does.
    //
    // NOT the game: the knight stands still at the origin (no hops, dashes or teleports), every aim is solved from
    // NPC.Center to a fixed target, dust/VFX (motes, glints, fuse flame, engulf) are not drawn - the fuse is a dot -
    // and armour dyes are not applied (GRK renders in the undyed RedKnightSet).
    internal static class KnightPosePreview
    {
        // Preview window per attack: long enough to show the last cue plus its lower/retract. Not gameplay timing.
        private static readonly (KnightSpecialAttack Attack, int Ticks)[] GreatAttacks =
        {
            (KnightSpecialAttack.RoyalStandard, 80),
            (KnightSpecialAttack.CrimsonStandard, 80),
            (KnightSpecialAttack.CrimsonDominion, 300),
            (KnightSpecialAttack.CrimsonAdvance, 170),
            (KnightSpecialAttack.FurnacePincer, 210),
            (KnightSpecialAttack.StormbreakerEdict, 125),
            (KnightSpecialAttack.FurnaceHerald, 150),
            (KnightSpecialAttack.CinderRain, 180),
            (KnightSpecialAttack.RedCourtProcession, 180),
            (KnightSpecialAttack.SpearThrow, 70),
        };

        // The death finale has no controller attack; it is the Dominion plant on GreatRedKnight's finale clock.
        private const string FinaleName = "DominionFinale";
        private const int FinaleTicks = 150;

        private static readonly (KnightSpecialAttack Attack, int Ticks)[] Attacks =
        {
            (KnightSpecialAttack.SpearThrow, 70),
            (KnightSpecialAttack.FirebombThrow, 80),
            (KnightSpecialAttack.FirebombReversal, 190),
            (KnightSpecialAttack.PoisonSpearTether, 185),
            (KnightSpecialAttack.PoisonArcVolley, 100),
            (KnightSpecialAttack.PoisonRain, 100),
            (KnightSpecialAttack.PoisonCurtain, 100),
            (KnightSpecialAttack.SpectralHandBarrage, 130),
        };

        // Ember Reversal's return dash waits on its bomb; pin it here so the second thrust shows.
        private const int PreviewEmberReturnDashTick = 150;

        internal static int Run(string[] args)
        {
            string repo = FindRepoRoot();
            int at = Array.IndexOf(args, "--knight-poses");
            string which = "all";
            if (at + 1 < args.Length && !args[at + 1].StartsWith("--"))
            {
                which = args[at + 1];
            }

            int direction = 1;
            if (args.Contains("--left"))
            {
                direction = -1;
            }

            Vector2 targetOffset = new Vector2(240f, 0f);
            int targetAt = Array.IndexOf(args, "--target");
            if (targetAt >= 0)
            {
                string[] parts = args[targetAt + 1].Split(',');
                targetOffset = new Vector2(float.Parse(parts[0]), float.Parse(parts[1]));
            }

            int zoom = 2;
            int zoomAt = Array.IndexOf(args, "--zoom");
            if (zoomAt >= 0)
            {
                zoom = int.Parse(args[zoomAt + 1]);
            }

            bool great = args.Contains("--great");
            var table = great ? GreatAttacks : Attacks;
            var selected = table.Where(entry => which == "all"
                || entry.Attack.ToString().Equals(which, StringComparison.OrdinalIgnoreCase)).ToList();
            bool finale = great && (which == "all" || which.Equals(FinaleName, StringComparison.OrdinalIgnoreCase));
            if (selected.Count == 0 && !finale)
            {
                Console.WriteLine($"Unknown attack '{which}'. Known: {string.Join(", ", table.Select(entry => entry.Attack))}"
                    + (great ? $", {FinaleName}" : ""));
                return 1;
            }

            PuppetArt art = null;
            string labelSuffix = "";
            if (args.Contains("--body"))
            {
                PuppetProfile profile = PuppetProfile.For("RedKnight");
                art = PuppetArtLibrary.Resolve("RedKnight", repo);
                art.HandleNormX = profile.HandleNormX;
                art.HandleNormY = profile.HandleNormY;
                art.WeaponScale = profile.WeaponDrawScale;
                art.BombSprite = Path.Combine(repo, "Content", "Projectiles", "Enemy", "EnemyFirebomb.png");
                if (great)
                {
                    // GreatRedKnight's lance overrides: 108x108 head-top-left diagonal, +90 degrees, 0.75 draw scale,
                    // rear hand 5px up the shaft (TwoHandedBackGripOffset).
                    PuppetProfile greatProfile = PuppetProfile.For("GreatRedKnight");
                    art.Name = "GreatRedKnight";
                    art.WeaponSprite = Path.Combine(repo, "Content", "Projectiles", "Enemy", "EnemyAncientBloodLanceProj.png");
                    art.WeaponRotationOffset = MathF.PI / 2f;
                    art.SpearHeadNorm = new System.Drawing.PointF(0.5f / 108f, 0.5f / 108f);
                    art.SpearBaseNorm = new System.Drawing.PointF(107.5f / 108f, 107.5f / 108f);
                    art.WeaponScale = greatProfile.WeaponDrawScale;
                    art.TwoHandedGripOffset = -5f;
                }

                // --weaponscale tries another MeleeWeaponDrawScale; the renders get a suffix so they don't replace the real ones.
                int weaponScaleAt = Array.IndexOf(args, "--weaponscale");
                if (weaponScaleAt >= 0)
                {
                    art.WeaponScale = float.Parse(args[weaponScaleAt + 1], System.Globalization.CultureInfo.InvariantCulture);
                    labelSuffix = $"-scale{art.WeaponScale:0.00}";
                }
            }

            Vector2 center = Vector2.Zero;
            Vector2 target = center + new Vector2(targetOffset.X * direction, targetOffset.Y);
            string knightName = great ? "Great Red Knight" : "Red Knight";
            Console.WriteLine($"{knightName} controller poses, facing {(direction == 1 ? "right" : "left")}, target {targetOffset.X},{targetOffset.Y} px from centre.");

            var runs = selected.Select(entry => (Name: entry.Attack.ToString(), entry.Attack, entry.Ticks, Finale: false)).ToList();
            if (finale)
            {
                runs.Add((FinaleName, KnightSpecialAttack.None, FinaleTicks, true));
            }

            foreach (var run in runs)
            {
                List<PoseFrame> frames = Simulate(run.Attack, run.Ticks, direction, center, target, great, run.Finale);
                if (args.Contains("--profile"))
                {
                    PrintProfile(run.Name, frames);
                }
                if (art != null)
                {
                    string label = $"{run.Name}-{(direction == 1 ? "Right" : "Left")}{labelSuffix}";
                    BodyRenderer.Render(art, frames, Path.Combine(repo, "tsorcDocs", "SwingReports", "RedKnightPoses"), label, zoom);
                    PrintGripErrors(run.Name, frames);
                }
            }
            return 0;
        }

        private static List<PoseFrame> Simulate(KnightSpecialAttack attack, int ticks, int direction, Vector2 center, Vector2 target,
            bool great, bool finale)
        {
            Type controllerType = typeof(RedKnight).Assembly.GetType("tsorcRevamp.NPCs.RedKnightAttackController", throwOnError: true);
            object controller = Activator.CreateInstance(controllerType, nonPublic: true);
            Set(controller, "Attack", attack);
            Set(controller, "Direction", direction);
            Set(controller, "LockedTarget", target);
            Set(controller, "AuxiliaryTargetA", target);
            if (attack == KnightSpecialAttack.FirebombReversal)
            {
                SetField(controller, "emberReturnDashTimer", PreviewEmberReturnDashTick);
            }

            float overshoot = (float)controllerType.GetField("FirebombOvershootPixels", BindingFlags.NonPublic | BindingFlags.Static).GetRawConstantValue();
            Vector2 throwVelocity = UsefulFunctions.BallisticTrajectory(center, target, 12.5f, fallback: true);
            Vector2 bombVelocity = UsefulFunctions.BallisticTrajectory(center, target + new Vector2(direction * overshoot, 0f),
                15f, 0.2f, highAngle: false, fallback: true);
            Vector2 castDirection = target - center;
            if (attack == KnightSpecialAttack.PoisonArcVolley)
            {
                castDirection = UsefulFunctions.BallisticTrajectory(center, target, 4.6f, 1.1f, highAngle: true, fallback: true);
            }

            // SpearThrow and the Adder Spear aim GetSpearRotation off LockedVelocity once it is set.
            if (attack == KnightSpecialAttack.SpearThrow || attack == KnightSpecialAttack.PoisonSpearTether)
            {
                Set(controller, "LockedVelocity", throwVelocity);
            }

            // A standard's heave aims down RedKnightStandard.InitialFlightDirection (internal), as UpdateControllerPose does.
            Type standardType = typeof(RedKnight).Assembly.GetType("tsorcRevamp.Content.Projectiles.Enemy.Weapons.RedKnightStandard", throwOnError: true);
            MethodInfo initialFlight = standardType.GetMethod("InitialFlightDirection", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);
            bool royal = attack == KnightSpecialAttack.RoyalStandard;
            Vector2 standardFlight = (Vector2)initialFlight.Invoke(null, new object[] { center, target, royal });

            MethodInfo getSpearRotation = controllerType.GetMethod("GetSpearRotation");
            var frames = new List<PoseFrame>();
            for (int timer = 0; timer < ticks; timer++)
            {
                Set(controller, "Timer", timer);

                var input = new RedKnightPuppet.ControllerPoseInput
                {
                    Attack = attack,
                    HeldProp = Get<KnightHeldProp>(controller, "HeldProp"),
                    Timer = timer,
                    ThrowRelease = Get<int>(controller, "SpearThrowReleaseTick"),
                    BombRelease = Get<int>(controller, "BombReleaseTick"),
                    ThrustStrike = Get<int>(controller, "SpearThrustStrikeTick"),
                    PalmRelease = Get<int>(controller, "PalmCastReleaseTick"),
                    PalmEnd = Get<int>(controller, "PalmCastEndTick"),
                    StandardRelease = Get<int>(controller, "StandardReleaseTick"),
                    HeraldEnd = Get<int>(controller, "HeraldEndTick"),
                    GreatKnight = great,
                    DominionTimer = -1,
                };
                if (attack == KnightSpecialAttack.CrimsonDominion || finale)
                {
                    input.DominionTimer = timer;
                    input.DominionFinale = finale;
                }
                input.ThrowAim = RedKnightPuppet.FacingAngleOf(throwVelocity, direction);
                if (input.StandardRelease >= 0)
                {
                    input.ThrowAim = RedKnightPuppet.FacingAngleOf(standardFlight, direction);
                }
                input.BombAim = RedKnightPuppet.FacingAngleOf(bombVelocity, direction);
                input.CastAim = RedKnightPuppet.FacingAngleOf(castDirection, direction);
                float spearSpriteRotation = (float)getSpearRotation.Invoke(controller, new object[] { center, center });
                Vector2 spearDirection = (spearSpriteRotation - MathHelper.PiOver2).ToRotationVector2();
                input.SpearAim = RedKnightPuppet.FacingAngleOf(spearDirection, direction);

                RedKnightPuppet.ControllerPose pose = RedKnightPuppet.ResolveControllerPose(input);
                frames.Add(ToFrame(attack, timer, direction, input, pose));
            }
            return frames;
        }

        private static PoseFrame ToFrame(KnightSpecialAttack attack, int timer, int direction,
            RedKnightPuppet.ControllerPoseInput input, RedKnightPuppet.ControllerPose pose)
        {
            var frame = new PoseFrame
            {
                Tick = timer,
                Phase = "Custom",
                Direction = direction,
                Motion = attack.ToString(),
                WeaponHidden = true,
            };

            bool release = timer == input.ThrowRelease || timer == input.BombRelease
                || timer == input.ThrustStrike || timer == input.PalmRelease;
            frame.Armed = release;

            string stage = "empty";
            if (pose.FrontProp == KnightHeldProp.Spear)
            {
                // CustomWeaponRotation drives both the spear and the composite arm (FrontHandPoseRotation - 90 deg);
                // MeleeCompositeArmRotationOffset turns the arm alone, and the spear follows the hand.
                float swingPose = RedKnightPuppet.SwingPoseFor(pose.Facing);
                frame.WeaponHidden = false;
                frame.WeaponRotation = swingPose;
                frame.SpearGrip = pose.Grip;
                frame.CompositeArmRotation = (swingPose - MathHelper.PiOver2 + pose.ArmOffset) * direction;
                frame.TwoHanded = pose.TwoHanded;
                stage = $"spear {Degrees(pose.Facing):+0;-0}d g{pose.Grip:0.00}";
                if (pose.TwoHanded)
                {
                    stage += " 2h";
                }
            }
            else if (pose.FrontProp == KnightHeldProp.Bomb)
            {
                // The bomb rides the sprite-hidden spear identity: the arm still poses from the swing pose.
                float swingPose = RedKnightPuppet.SwingPoseFor(pose.Facing);
                frame.FrontArmFree = swingPose - MathHelper.PiOver2;
                frame.HeldProp = "bomb";
                frame.HeldPropFacing = pose.Facing;
                frame.FuseLit = pose.FuseLit;
                stage = $"bomb {Degrees(pose.Facing):+0;-0}d";
            }
            else if (pose.FrontFreeFacing.HasValue)
            {
                frame.FrontArmFree = pose.FrontFreeFacing.Value - MathHelper.PiOver2;
                stage = $"palms {Degrees(pose.FrontFreeFacing.Value):+0;-0}d";
            }

            if (pose.BackPalmFacing.HasValue)
            {
                frame.BackArmFree = pose.BackPalmFacing.Value - MathHelper.PiOver2;
                if (pose.FrontProp == KnightHeldProp.None && !pose.FrontFreeFacing.HasValue)
                {
                    stage = $"palm {Degrees(pose.BackPalmFacing.Value):+0;-0}d";
                }
            }

            if (release)
            {
                stage += " REL";
            }
            frame.Stage = stage;
            return frame;
        }

        // The two-handed solve's worst miss: how far the rear hand ends from the shaft point it reached for.
        private static void PrintGripErrors(string name, List<PoseFrame> frames)
        {
            var twoHanded = frames.Where(frame => frame.TwoHanded).ToList();
            if (twoHanded.Count == 0)
            {
                return;
            }

            PoseFrame worst = twoHanded.OrderByDescending(frame => frame.TwoHandedGripError).First();
            float mean = twoHanded.Average(frame => frame.TwoHandedGripError);
            Console.WriteLine($"    {name}: two-handed {twoHanded.Count} ticks, rear-hand grip error mean {mean:0.0}px, worst {worst.TwoHandedGripError:0.0}px at t{worst.Tick}");
        }

        private static void PrintProfile(string name, List<PoseFrame> frames)
        {
            Console.WriteLine();
            Console.WriteLine($"  {name}   {frames.Count} ticks");
            string previous = null;
            int runStart = 0;
            for (int i = 0; i <= frames.Count; i++)
            {
                string kind = null;
                if (i < frames.Count)
                {
                    kind = frames[i].Stage.Split(' ')[0];
                }
                if (kind == previous && i < frames.Count && !frames[i].Armed)
                {
                    continue;
                }
                if (previous != null)
                {
                    Console.WriteLine($"    t{runStart,-4}-t{i - 1,-4} {frames[runStart].Stage,-26} -> {frames[i - 1].Stage}");
                }
                previous = kind;
                runStart = i;
            }
        }

        private static float Degrees(float radians) => radians * 180f / MathF.PI;

        private static void Set(object target, string property, object value)
        {
            PropertyInfo info = target.GetType().GetProperty(property, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            info.GetSetMethod(nonPublic: true).Invoke(target, new[] { value });
        }

        private static void SetField(object target, string field, object value)
        {
            FieldInfo info = target.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance);
            info.SetValue(target, value);
        }

        private static T Get<T>(object target, string property)
        {
            PropertyInfo info = target.GetType().GetProperty(property, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            return (T)info.GetValue(target);
        }

        // The tool runs from its own folder or the repo root; the repo root is where tsorcRevamp.csproj lives.
        private static string FindRepoRoot()
        {
            string directory = Directory.GetCurrentDirectory();
            while (directory != null && !File.Exists(Path.Combine(directory, "tsorcRevamp.csproj")))
            {
                directory = Path.GetDirectoryName(directory);
            }
            return directory ?? Directory.GetCurrentDirectory();
        }
    }
}

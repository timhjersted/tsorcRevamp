using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using tsorcRevamp.NPCs.Puppets;

namespace SwingPreview
{
    internal static class AbyssalPosePreview
    {
        internal static int Run(string[] args)
        {
            Options options = Options.Parse(args.Where(a => a != "--abyssal-poses").Concat(new[] { "--puppet", "AbyssalNinja" }).ToArray());
            if (options == null) return 1;
            PuppetProfile profile = options.BuildProfile();
            PuppetArt art = PuppetArtLibrary.Resolve("AbyssalNinja", options.RepoRoot);
            art.HandleNormX = profile.HandleNormX; art.HandleNormY = profile.HandleNormY; art.WeaponScale = profile.WeaponDrawScale;
            Console.WriteLine("Abyssal Ninja custom poses: functions shared with the actual NPC; pose-only preview.");
            Console.WriteLine("Terrain, actual travel, aim lock and hit registration are NOT simulated. Timed airborne stages are flat-ground examples.");
            foreach (string move in new[] { "Blackstep Return", "Rising Eclipse", "Black Meridian", "Nightfall" })
            {
                var frames = new List<PoseFrame>();
                float last = -0.3f;
                void Slice(string stage, string phase, int ticks, Func<int, float> pose, Func<int, bool> live = null,
                    Func<int, float> spin = null, Func<int, bool> airborne = null, bool hidden = false)
                {
                    for (int i = 0; i < ticks; i++)
                    {
                        float rotation = pose(i);
                        frames.Add(new PoseFrame { Tick = frames.Count, Phase = phase, Stage = stage,
                            Armed = live?.Invoke(i) ?? false, Airborne = airborne?.Invoke(i) ?? false,
                            WeaponRotation = rotation, CompositeArmRotation = (rotation - MathHelper.PiOver2) * options.Direction,
                            Direction = options.Direction, BodyRow = PuppetNPC.BodyRowFromWeaponRotation(rotation, 1, 0),
                            LegRow = 0, Motion = move, WeaponHidden = hidden, OwnerHidden = hidden,
                            BodySpinRotation = spin?.Invoke(i) ?? 0 });
                        last = rotation;
                    }
                }
                void Tell(int ticks, float end, string stage) => Slice(stage, "MeleeComboTelegraph", ticks,
                    i => MathHelper.SmoothStep(-0.3f, end, (i + 1f) / ticks));
                void Recover(int ticks)
                {
                    float carried = last;
                    Slice("Recovery (harmless)", "MeleeComboRecovery", ticks, i => {
                        if (i > 8) carried = MathHelper.Lerp(carried, -0.3f, 0.1f);
                        return carried;
                    });
                }
                if (move == "Blackstep Return")
                {
                    Tell(28, AbyssalNinjaMotion.Low, "Backstep tell");
                    Slice("Undercut then backward flip", "MeleeComboAttack", 40, AbyssalNinjaMotion.BackCut,
                        i => i <= 14, i => AbyssalNinjaMotion.BackSpin(i, options.Direction), i => i >= 15);
                    float carried = last;
                    Slice("Harmless retreat / landing", "MeleeComboPause", 23, i => carried = MathHelper.Lerp(carried, -0.3f, 0.1f), airborne: i => true);
                    Recover(30);
                }
                else if (move == "Rising Eclipse")
                {
                    Tell(30, AbyssalNinjaMotion.Low, "Anti-air tell / lock t18");
                    Slice("Committed airborne interception", "MeleeComboAttack", 64,
                        i => AbyssalNinjaMotion.RisingCut(i, 26), i => i >= 26 && i < 36,
                        i => AbyssalNinjaMotion.RisingSpin(i, 26, options.Direction), i => true);
                    Recover(32);
                }
                else if (move == "Black Meridian")
                {
                    Tell(44, AbyssalNinjaMotion.High, "Meridian tell / lock t32");
                    Slice("Passing cut / 32t dash", "MeleeComboAttack", 32,
                        i => AbyssalNinjaMotion.PassingCut(i, 18), i => i >= 18 && i < 28);
                    float carried = last;
                    Slice("Seam charge (32t)", "MeleeComboPause", 32, i => carried = MathHelper.Lerp(carried, -0.3f, 0.1f));
                    Slice("Seam ACTIVE (8t); sword harmless", "MeleeComboPause", 8, i => carried = MathHelper.Lerp(carried, -0.3f, 0.1f));
                    Recover(42);
                }
                else
                {
                    Tell(32, AbyssalNinjaMotion.High, "Departure / destination lock t20");
                    Slice("Vanish (12t)", "MeleeComboPause", 12, i => AbyssalNinjaMotion.High, hidden: true);
                    Slice("Arrival fade + visible tell (42t)", "MeleeComboTelegraph", 42, i => AbyssalNinjaMotion.High, airborne: i => true);
                    Slice("Committed descent / real landing", "MeleeComboAttack", 66,
                        i => AbyssalNinjaMotion.FallingCut(i - 28), i => i >= 28 && i < 38, airborne: i => i < 36);
                    Recover(48);
                }
                int armed = frames.Count(f => f.Armed);
                float peak = 0, first = 0;
                for (int i = 1; i < frames.Count; i++)
                {
                    float delta = Math.Abs(MathHelper.ToDegrees(MathHelper.WrapAngle(
                        (frames[i].WeaponRotation - frames[i - 1].WeaponRotation) * options.Direction
                        + frames[i].BodySpinRotation - frames[i - 1].BodySpinRotation)));
                    if (frames[i].Armed) peak = Math.Max(peak, delta);
                    if (frames[i].Phase == "MeleeComboAttack" && frames[i - 1].Phase != frames[i].Phase) first = delta;
                }
                Console.WriteLine($"{move}: {frames.Count}t, blade armed {armed}t, first strike handoff {first:0.0} degrees, peak world blade travel {peak:0.0} degrees/t");
                BodyRenderer.Render(art, frames, options.BodyOutDir, move + " custom poses", options.Zoom);
            }
            return 0;
        }
    }
}

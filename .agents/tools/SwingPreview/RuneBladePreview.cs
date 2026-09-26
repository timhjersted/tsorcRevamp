using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using tsorcRevamp.Utilities;

namespace SwingPreview
{
    // --rune-demo --profile --body [--count 1..4] [--underhand] [--left]
    // Shares the game's exact bespoke magic-channel poses; no surrogate melee combo clock.
    internal static class RuneBladePreview
    {
        internal static int Run(string[] args)
        {
            string repo = Directory.GetCurrentDirectory();
            int countAt = Array.IndexOf(args, "--count");
            int count = countAt >= 0 ? Math.Clamp(int.Parse(args[countAt + 1]), 1, 4) : 4;
            bool firstUnder = args.Contains("--underhand");
            int facing = args.Contains("--left") ? -1 : 1;
            string label = $"Rune-{(firstUnder ? "Underhand" : "Overhand")}-{count}-{(facing == 1 ? "Right" : "Left")}";
            var frames = new List<PoseFrame>();
            bool advancing = args.Contains("--advance");
            if (advancing) label += "-Advancing";
            float ownerX = 0f, velocity = 0f, firstReleaseGap = 0f, liveTravel = 0f;
            void Add(float rotation, string phase, int step, bool visible = false, float age = 0f, float fade = 1f)
            {
                float targetX = RuneBladeConjuration.StartDistance + frames.Count * 3f;
                if (advancing)
                {
                    bool preparing = phase != "Recovery";
                    bool live = visible && RuneBladeConjuration.Live(age);
                    float desired = (preparing && targetX - ownerX > 72f || live) ? RuneBladeConjuration.CastSpeed : 0f;
                    velocity += (desired - velocity) * 0.3f;
                    ownerX += velocity;
                    if (visible && step == 0 && age == 1f) firstReleaseGap = targetX - ownerX;
                    if (live && step == 0) liveTravel += velocity;
                }
                frames.Add(new PoseFrame { Tick = frames.Count, Phase = phase, Direction = facing,
                    WeaponRotation = rotation, CompositeArmRotation = (rotation - MathF.PI / 2f) * facing,
                    StepIndex = step, StepCount = count, Motion = label, BodyRow = 0, LegRow = 0,
                    RuneVisible = visible, RuneUnderhand = RuneBladeConjuration.Underhand(firstUnder, step),
                    RuneAge = age, RuneFade = fade, Armed = visible && RuneBladeConjuration.Live(age),
                    FlailHasTarget = advancing, FlailTargetX = targetX - ownerX,
                    FlailTargetY = 16f, FlailTargetLabel = "walk 3px/t (camera follows caster)" });
            }
            for (int tick = 0; tick <= RuneBladeConjuration.TellTicks; tick++)
                Add(RuneBladeConjuration.Tell(firstUnder, tick / (float)RuneBladeConjuration.TellTicks), "Windup", 0);
            int active = RuneBladeConjuration.ActiveTicks(count);
            for (int tick = 1; tick <= active; tick++)
            {
                int step = Math.Min(count - 1, tick / RuneBladeConjuration.StepTicks);
                int local = tick - step * RuneBladeConjuration.StepTicks;
                bool visible = local >= 40 && local < 96;
                float age = Math.Clamp(local - 40 + 1, 0, 40);
                string phase = local < 40 ? "StaffSweep" : local < 80 ? "RuneSweep" : local < 96 ? "Dissolve" : "RepeatWindup";
                Add(RuneBladeConjuration.StaffPose(firstUnder, tick, count), phase, step, visible, age,
                    Math.Clamp((96 - local) / 16f, 0f, 1f));
            }
            for (int tick = 0; tick < 60; tick++) Add(RuneBladeConjuration.Carry, "Recovery", count - 1);
            if (args.Contains("--profile"))
            {
                float peak = 0f;
                for (int tick = 1; tick <= 40; tick++)
                    peak = Math.Max(peak, Math.Abs(RuneBladeConjuration.Sweep(false, tick) - RuneBladeConjuration.Sweep(false, tick - 1)) * 180f / MathF.PI);
                float liveSweep = Math.Abs(RuneBladeConjuration.Sweep(false, 15) - RuneBladeConjuration.Sweep(false, 0)) * 180f / MathF.PI;
                float firstDelta = Math.Abs(RuneBladeConjuration.Sweep(false, 1) - RuneBladeConjuration.Sweep(false, 0)) * 180f / MathF.PI;
                Console.WriteLine($"{label}: envelope 220.02deg; peak {peak:F2}deg/t; armed ages 1..15 (15t), swept live {liveSweep:F2}deg.");
                Console.WriteLine("Weighted: 8t acceleration /32t decay k6. Staff is harmless; sword repeats the same curve 40t later.");
                Console.WriteLine($"First-frame step rotation delta {firstDelta:F2}deg; repeat holds finish, settles44t to carry, then32t fresh windup.");
                Console.WriteLine("Next blade starts128t after prior start:113t after prior live end. Final punish window >=101t (41t settle/fade +60t recovery), then30t neutral.");
                Console.WriteLine("Reach128px +24px forward, +/-30px vertical. Alpha50, 16t dissolve. Facing/stride lock during live sweep.");
                Console.WriteLine($"Approach{RuneBladeConjuration.ApproachSpeed}px/t to{RuneBladeConjuration.StartDistance}px; cast{RuneBladeConjuration.CastSpeed}px/t; maximum live travel48px (<roll travel100-120px), live15t (<roll immunity22t).");
                if (advancing) Console.WriteLine($"FLAT-GROUND MODEL: target walks3px/t, gap at first blade release{firstReleaseGap:F2}px; caster live travel{liveTravel:F2}px. Camera follows caster; excludes floor checks, authority and actual hit registration.");
                Console.WriteLine("BODY FIDELITY: real staff/blade/slash and exact poses; red cloth armor is a proxy for packed vanilla mask/robe. Dust/collision/MP not rendered.");
            }
            if (args.Contains("--body"))
            {
                var art = PuppetArtLibrary.Resolve("OolacileCultist", repo);
                art.Name = "AttraidiesIllusion";
                art.OffHandWeaponSprite = null;
                art.WeaponSprite = Path.Combine(repo, "Content/Items/Weapons/Magic/SoulArrowStaff.png");
                art.HandleNormX = 0.16f; art.HandleNormY = 0.84f;
                art.WeaponScale = 0.9f; art.WeaponRotationOffset = MathF.PI / 4f;
                art.RuneSprite = Path.Combine(repo, "Content/Items/Weapons/Melee/Broadswords/RuneBlade.png");
                art.RuneSlashSprite = Path.Combine(repo, "Content/Items/Weapons/Melee/Broadswords/BroadswordRework/Common/Melee/Slash.png");
                BodyRenderer.Render(art, frames, Path.Combine(repo, "tsorcDocs/SwingReports/AttraidiesRuneBlade"), label, 1);
            }
            return 0;
        }
    }
}

using System;
using System.Collections.Generic;
using tsorcRevamp.Utilities;

namespace SwingPreview
{
    /// <summary>A preview-only projectile sequence. These definitions are intentionally independent
    /// from the mod's attack tables: they let us judge a proposal before committing gameplay code.</summary>
    internal sealed class FlailPreviewDefinition
    {
        public string Name;
        public int TelegraphTicks;
        public int AttackTicks;
        public int RecoveryTicks;
        public string TimingSummary;
        public Func<int, FlailPreviewSample> TelegraphSample;
        public Func<int, FlailPreviewSample> AttackSample;
        public Func<int, FlailPreviewSample> RecoverySample;
    }

    internal readonly struct FlailPreviewSample
    {
        public readonly bool Visible;
        public readonly float X;
        public readonly float Y;
        public readonly float Rotation;
        public readonly bool DamageActive;
        public readonly string Stage;
        public readonly bool ArmOverride;
        public readonly float ArmRotation;
        public readonly bool HasTarget;
        public readonly float TargetX;
        public readonly float TargetY;
        public readonly string TargetLabel;
        /// <summary>Horizontal world travel of the owner from its pose at tick zero. Most flails
        /// leave this at zero; Chainstorm uses it so the body, chain and fixed target read together.</summary>
        public readonly float OwnerOffsetX;

        public FlailPreviewSample(float x, float y, float rotation, bool damageActive, string stage,
            float armRotation, bool visible = true,
            bool hasTarget = false, float targetX = 0f, float targetY = 0f, string targetLabel = null,
            float ownerOffsetX = 0f)
        {
            Visible = visible;
            X = x;
            Y = y;
            Rotation = rotation;
            DamageActive = damageActive;
            Stage = stage;
            ArmOverride = true;
            ArmRotation = armRotation;
            HasTarget = hasTarget;
            TargetX = targetX;
            TargetY = targetY;
            TargetLabel = targetLabel;
            OwnerOffsetX = ownerOffsetX;
        }
    }

    internal static class FlailPreviewLibrary
    {
        private const int StandardTell = 40;
        private const float CloseRadius = 60f;
        private const float CarryArm = -1.87f;
        private const float FrontTargetX = 200f;

        /* Preview spec cards
         *
         * Chain Cross: one real head/chain, present from tell start through an 18t retract. 40t
         * counter-clockwise orbit; 16t weighted lash at target lock 1; 30t harmless carry/reacquire;
         * 16t weighted lash at target lock 2; 90t recovery. Each live window is 14t, their starts
         * are 46t apart, and the second branch is conditional on a valid player still in front.
         * The arm now points along the chain's actual radial direction through the orbit, both lashes
         * and the carry between them; its unwrapped angle keeps those large motions continuous.
         * Demonstrated reach 200px (12.5 tiles). Target markers stand in for server-captured aim.
         *
         * Reverse Halo: the former Ankle Reaper preview. One real head/chain; 40t backward orbit,
         * 16t level release to 208px (13 tiles), 18t retract, 60t recovery.
         *
         * Ankle Reaper: the original vision restored. One continuous 40t counter-clockwise loop
         * from 3 -> 12 -> 9 -> 6 -> 3 o'clock. The loop accelerates for 8t and then cruises so it
         * carries nonzero velocity into an immediate 16t level extension, rather than easing to a
         * stop at the phase boundary. The extension starts fast and eases out at maximum reach, then
         * an 18t retract and 60t recovery. The arm follows the complete orbit, not a bounded pump.
         * Selection gate: only eligible while |player.Center.Y - NPC.Center.Y| <= 48px.
         *
         * Backlash Reversal: conditional follow-up only when the player is behind while facing is
         * still locked away. 40t ground-load tell, then a 6t zero-speed load and an 18t weighted
         * reverse whip to 192px (12 tiles) behind; 18t retract, 60t recovery. Its 15t live window
         * is rollable. No stand-alone selection is implied by this preview.
         *
         * VFX/dust, tile contact, damage, multiplayer target capture and projectile death are n/a
         * here because these are offline proposal tracks, not gameplay attacks.
         */
        private static readonly FlailPreviewDefinition[] Definitions =
        {
            new FlailPreviewDefinition
            {
                Name = "Chainfall Overhead",
                TelegraphTicks = StandardTell,
                AttackTicks = 18 + 18,
                RecoveryTicks = 60,
                TimingSummary = "One 60px harmless orbit; 18t Smoother expanding 90-degree overhead arc (60->240px) live; 18t retract; filtered chain-following arm",
                TelegraphSample = StandardOrbitTell,
                AttackSample = Chainfall,
                RecoverySample = RecoveryPose,
            },
            new FlailPreviewDefinition
            {
                Name = "Chainrise Underhand",
                TelegraphTicks = StandardTell,
                AttackTicks = 18 + 18,
                RecoveryTicks = 60,
                TimingSummary = "One 60px harmless orbit; 18t Smoother expanding 90-degree underhand arc (60->240px) live; 18t retract; filtered chain-following arm",
                TelegraphSample = StandardOrbitTell,
                AttackSample = Chainrise,
                RecoverySample = RecoveryPose,
            },
            new FlailPreviewDefinition
            {
                Name = "Advancing Chainstorm",
                TelegraphTicks = StandardTell,
                AttackTicks = 36 * 4 + 18,
                RecoveryTicks = 120,
                TimingSummary = "One 60px harmless orbit; 36t Smoother expansion to 240px, then three 36t live rotations; owner advances 1.596px/t while live; 18t retract; filtered chain-following arm",
                TelegraphSample = StandardOrbitTell,
                AttackSample = Chainstorm,
                RecoverySample = RecoveryPose,
            },
            new FlailPreviewDefinition
            {
                Name = "Chain Cross",
                TelegraphTicks = StandardTell,
                AttackTicks = 16 + 30 + 16 + 18,
                RecoveryTicks = 90,
                TimingSummary = "Chain-aligned full arm; Smoother orbit; two Weighted lashes (6t in / 10t out / k6), 30t Smoother reacquire, 18t retract",
                TelegraphSample = ChainCrossTell,
                AttackSample = ChainCross,
                RecoverySample = RecoveryPose,
            },
            new FlailPreviewDefinition
            {
                Name = "Reverse Halo",
                TelegraphTicks = StandardTell,
                AttackTicks = 16 + 18,
                RecoveryTicks = 60,
                TimingSummary = "Smoother backward orbit; level-only Weighted lash (6t in / 10t out / k6), 18t retract",
                TelegraphSample = ReverseHaloOrbit,
                AttackSample = ReverseHalo,
                RecoverySample = RecoveryPose,
            },
            new FlailPreviewDefinition
            {
                Name = "Ankle Reaper",
                TelegraphTicks = StandardTell,
                AttackTicks = 16 + 18,
                RecoveryTicks = 60,
                TimingSummary = "3->12->9->6->3 counter-clockwise loop; 8t acceleration then cruise into immediate 16t EaseOut extension; full-orbit arm; 18t retract",
                TelegraphSample = AnkleReaperOrbit,
                AttackSample = AnkleReaperExtension,
                RecoverySample = AnkleReaperRecovery,
            },
            new FlailPreviewDefinition
            {
                Name = "Backlash Reversal",
                TelegraphTicks = StandardTell,
                AttackTicks = 6 + 18 + 18,
                RecoveryTicks = 60,
                TimingSummary = "Smoother ground load; 6t planted hold, Weighted reverse (7t in / 11t out / k7), 18t retract",
                TelegraphSample = BacklashGroundLoad,
                AttackSample = BacklashReversal,
                RecoverySample = RecoveryPose,
            },
        };

        internal const string Known = "Chainfall Overhead, Chainrise Underhand, Advancing Chainstorm, Chain Cross, Reverse Halo, Ankle Reaper, Backlash Reversal";

        internal static IReadOnlyList<FlailPreviewDefinition> Resolve(string query)
        {
            var matches = new List<FlailPreviewDefinition>();
            bool all = query.Equals("all", StringComparison.OrdinalIgnoreCase);
            foreach (FlailPreviewDefinition definition in Definitions)
            {
                if (all || definition.Name.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    matches.Add(definition);
                }
            }
            return matches;
        }

        internal static FlailPreviewSample Sample(FlailPreviewDefinition definition, int globalTick, string phase)
        {
            if (phase.IndexOf("Telegraph", StringComparison.Ordinal) >= 0)
            {
                return definition.TelegraphSample(Math.Clamp(globalTick, 0, definition.TelegraphTicks - 1));
            }
            if (phase.IndexOf("Attack", StringComparison.Ordinal) >= 0)
            {
                int attackTick = Math.Clamp(globalTick - definition.TelegraphTicks, 0, definition.AttackTicks - 1);
                return definition.AttackSample(attackTick);
            }
            if (phase.IndexOf("Recovery", StringComparison.Ordinal) >= 0)
            {
                int recoveryTick = Math.Max(0, globalTick - definition.TelegraphTicks - definition.AttackTicks);
                return definition.RecoverySample(recoveryTick);
            }
            return default;
        }

        // Matches the common authored-pattern tell in EnemyFlailProjectileBase: facing right begins
        // at 9 o'clock, makes one full clockwise screen-space turn, and returns to the release side.
        private static FlailPreviewSample StandardOrbitTell(int tick)
        {
            float progress = tick / (float)StandardTell;
            float angle = MathF.PI + MathF.Tau * progress;
            return Polar(CloseRadius, angle, tick * 0.25f, false, "harmless 60px orbit",
                GenericArmAfterTellTick(tick));
        }

        private static FlailPreviewSample Chainfall(int tick) => DirectionalChainSwing(tick, overhead: true);

        private static FlailPreviewSample Chainrise(int tick) => DirectionalChainSwing(tick, overhead: false);

        private static FlailPreviewSample DirectionalChainSwing(int tick, bool overhead)
        {
            const int swingTicks = 18;
            const int retractTicks = 18;
            float direction = overhead ? 1f : -1f;

            if (tick < swingTicks)
            {
                float progress = (tick + 1f) / swingTicks;
                float eased = Smoother(progress);
                float angle = MathF.PI + direction * MathF.PI * 0.5f * eased;
                float radius = Lerp(CloseRadius, 240f, eased);
                return Polar(radius, angle, tick * direction * 0.35f, true,
                    overhead ? "overhead expansion live" : "underhand expansion live",
                    GenericArmAfterDirectionalTick(tick, overhead));
            }

            float retract = Smoother((tick - swingTicks + 1f) / retractTicks);
            float finalAngle = MathF.PI + direction * MathF.PI * 0.5f;
            float finalRadius = Lerp(240f, 0f, retract);
            return Polar(finalRadius, finalAngle, tick * direction * 0.28f, false, "retract",
                GenericArmAfterDirectionalTick(tick, overhead));
        }

        private static FlailPreviewSample Chainstorm(int tick)
        {
            const int rotationTicks = 36;
            const int liveTicks = rotationTicks * 4;
            const int retractTicks = 18;
            const float advancePerTick = 2.85f * 0.56f; // Black Ninja's actual TopSpeed * ForwardPushMult.
            const float targetWorldX = 330f;

            if (tick < liveTicks)
            {
                float completedRotations = (tick + 1f) / rotationTicks;
                float angle = MathF.PI + MathF.Tau * completedRotations;
                float expansion = Math.Clamp((tick + 1f) / (float)rotationTicks, 0f, 1f);
                float radius = Lerp(CloseRadius, 240f, Smoother(expansion));
                float ownerOffset = (tick + 1f) * advancePerTick;
                return Polar(radius, angle, tick * 0.40f, true,
                    tick < rotationTicks ? "expanding rotation live / advance" : "max-length rotation live / advance",
                    GenericArmAfterChainstormTick(tick), hasTarget: true,
                    targetX: targetWorldX - ownerOffset, targetY: 0f, targetLabel: "fixed player lane",
                    ownerOffsetX: ownerOffset);
            }

            float retract = Smoother((tick - liveTicks + 1f) / retractTicks);
            float retractAngle = MathF.PI + MathF.Tau * (4f + (tick - liveTicks + 1f) / rotationTicks);
            // ForwardPushMult is held for the combo's full active phase in PuppetNPC, so the owner
            // carries forward through the harmless retract before its 120t recovery begins.
            float ownerOffsetDuringRetract = (tick + 1f) * advancePerTick;
            return Polar(Lerp(240f, 0f, retract), retractAngle, tick * 0.35f, false,
                "retract / carried advance", GenericArmAfterChainstormTick(tick),
                hasTarget: true, targetX: targetWorldX - ownerOffsetDuringRetract, targetY: 0f,
                targetLabel: "fixed player lane", ownerOffsetX: ownerOffsetDuringRetract);
        }

        // The three original patterns rely on PuppetNPC.UpdateFlailProjectileArmPose rather than
        // an authored direct arm angle. Replaying its 0.28 wrapped follow here lets their previews
        // show the same restrained, natural shoulder response as runtime instead of a stiff arm.
        private static float GenericArmAfterTellTick(int tick)
        {
            float arm = CarryArm;
            for (int current = 0; current <= tick; current++)
            {
                float angle = MathF.PI + MathF.Tau * current / StandardTell;
                arm = FollowGenericArm(arm, angle);
            }
            return arm;
        }

        private static float GenericArmAfterDirectionalTick(int tick, bool overhead)
        {
            float arm = GenericArmAfterTellTick(StandardTell - 1);
            float direction = overhead ? 1f : -1f;
            for (int current = 0; current <= tick; current++)
            {
                float angle;
                if (current < 18)
                {
                    float progress = Smoother((current + 1f) / 18f);
                    angle = MathF.PI + direction * MathF.PI * 0.5f * progress;
                }
                else
                {
                    angle = MathF.PI + direction * MathF.PI * 0.5f;
                }
                arm = FollowGenericArm(arm, angle);
            }
            return arm;
        }

        private static float GenericArmAfterChainstormTick(int tick)
        {
            const int liveTicks = 36 * 4;
            float arm = GenericArmAfterTellTick(StandardTell - 1);
            for (int current = 0; current <= tick; current++)
            {
                float angle = current < liveTicks
                    ? MathF.PI + MathF.Tau * (current + 1f) / 36f
                    : MathF.PI + MathF.Tau * (4f + (current - liveTicks + 1f) / 36f);
                arm = FollowGenericArm(arm, angle);
            }
            return arm;
        }

        private static float FollowGenericArm(float current, float flailAngle)
        {
            float desired = OrbitArm(flailAngle);
            return WrapRadians(current + WrapRadians(desired - current) * 0.28f);
        }

        private static float WrapRadians(float value)
        {
            while (value > MathF.PI) value -= MathF.Tau;
            while (value < -MathF.PI) value += MathF.Tau;
            return value;
        }

        private static FlailPreviewSample ChainCrossTell(int tick)
        {
            float progress = (tick + 1f) / StandardTell;
            // Start at 3 o'clock and finish at 12 after one full counter-clockwise turn. That gives
            // the first lash a high origin instead of pretending a level player is above the NPC.
            float orbitalProgress = Smoother(progress);
            float angle = -MathF.Tau * orbitalProgress - MathF.PI * 0.5f * orbitalProgress;
            // Keep the arm on the chain itself rather than pumping through the restrained generic
            // orbit pose. This is intentionally unwrapped: the 1.25-turn tell produces equally
            // substantial shoulder motion and hands off continuously into the first lash.
            float arm = angle - MathF.PI / 2f;
            return Polar(CloseRadius, angle, tick * -0.25f, false, "orbit / lock target 1", arm,
                hasTarget: true, targetX: FrontTargetX, targetY: 0f, targetLabel: "lock 1");
        }

        private static FlailPreviewSample ChainCross(int tick)
        {
            const int lashTicks = 16;
            const int reacquireTicks = 30;
            const int secondStart = lashTicks + reacquireTicks;
            const int retractStart = secondStart + lashTicks;

            if (tick < lashTicks)
            {
                float p = Weighted(tick, lashTicks, 6, 10, 6f);
                (float x, float y) = QuadraticBezier(0f, -CloseRadius, 118f, -68f, FrontTargetX, 0f, p);
                float arm = ChainAlignedArm(x, y, -MathF.Tau);
                return Targeted(x, y, tick * -0.34f, tick >= 2, "lash 1 live", arm,
                    FrontTargetX, 0f, "lock 1");
            }

            if (tick < secondStart)
            {
                // The same head travels harmlessly around the far side to the low origin. The full
                // 30t gap gives a late roller another roll and is the branch's reacquisition window.
                float p = Smoother((tick - lashTicks + 1f) / reacquireTicks);
                (float x, float y) = CubicBezier(
                    FrontTargetX, 0f,
                    145f, 78f,
                    38f, 94f,
                    0f, CloseRadius,
                    p);
                float targetY = Lerp(0f, 18f, p);
                float arm = ChainAlignedArm(x, y, -MathF.Tau);
                return Targeted(x, y, tick * -0.30f, false, "30t reacquire / conditional", arm,
                    190f, targetY, "lock 2");
            }

            if (tick < retractStart)
            {
                int local = tick - secondStart;
                float p = Weighted(local, lashTicks, 6, 10, 6f);
                (float x, float y) = QuadraticBezier(0f, CloseRadius, 112f, 74f, 190f, 18f, p);
                float arm = ChainAlignedArm(x, y, -MathF.Tau);
                return Targeted(x, y, tick * 0.34f, local >= 2, "lash 2 live", arm,
                    190f, 18f, "lock 2");
            }

            float retract = Smoother((tick - retractStart + 1f) / 18f);
            float retractStartArm = ChainAlignedArm(190f, 18f, -MathF.Tau);
            return Sample(Lerp(190f, 0f, retract), Lerp(18f, 0f, retract), tick * 0.28f,
                false, "retract", Lerp(retractStartArm, CarryArm - MathF.Tau, retract));
        }

        private static FlailPreviewSample ReverseHaloOrbit(int tick)
        {
            float progress = (tick + 1f) / StandardTell;
            float orbitalProgress = Smoother(progress);
            float radius = Lerp(CloseRadius, 96f, orbitalProgress);
            // Positive screen-space rotation gives the requested backward path: 3 -> 6 -> 9 -> 12 -> 3.
            float angle = MathF.Tau * orbitalProgress;
            return Polar(radius, angle, tick * 0.28f, false, "backward orbit | level-only", OrbitArm(angle),
                hasTarget: true, targetX: 208f, targetY: 0f, targetLabel: "level gate +/-48px");
        }

        private static FlailPreviewSample ReverseHalo(int tick)
        {
            if (tick < 16)
            {
                float p = Weighted(tick, 16, 6, 10, 6f);
                float radius = Lerp(96f, 208f, p);
                float arm = Lerp(ArmFromFlail(radius, 0f), -MathF.PI / 2f, p);
                return Polar(radius, 0f, tick * 0.38f, tick >= 2, "level release live", arm,
                    hasTarget: true, targetX: 208f, targetY: 0f, targetLabel: "level gate +/-48px");
            }

            float retract = Smoother((tick - 16f + 1f) / 18f);
            return Sample(Lerp(208f, 0f, retract), 0f, tick * 0.32f,
                false, "retract", Lerp(-MathF.PI / 2f, CarryArm, retract));
        }

        private static FlailPreviewSample AnkleReaperOrbit(int tick)
        {
            // Screen-space Y grows downward, so a negative angle is the requested visual
            // counter-clockwise path: 3 -> 12 -> 9 -> 6 -> 3 o'clock.
            float progress = tick / (StandardTell - 1f);
            float orbitalProgress = AccelerateThenCruise(progress, 8f / (StandardTell - 1f));
            float radius = Lerp(CloseRadius, 96f, orbitalProgress);
            float angle = -MathF.Tau * orbitalProgress;
            return Polar(radius, angle, tick * -0.28f, false, "counter-clockwise full loop | level-only",
                FullOrbitArm(angle), hasTarget: true, targetX: 208f, targetY: 0f,
                targetLabel: "level gate +/-48px");
        }

        private static FlailPreviewSample AnkleReaperExtension(int tick)
        {
            if (tick < 16)
            {
                float t = (tick + 1f) / 16f;
                // Ease-out has a nonzero starting slope. The first attack frame therefore moves
                // immediately instead of stacking another zero-velocity ease after the orbit.
                float p = 1f - (1f - t) * (1f - t);
                float angle = -MathF.Tau;
                float radius = Lerp(96f, 208f, p);
                return Polar(radius, angle, tick * -0.38f, tick >= 2,
                    "continuous level extension live", FullOrbitArm(angle),
                    hasTarget: true, targetX: 208f, targetY: 0f, targetLabel: "level gate +/-48px");
            }

            float retract = Smoother((tick - 16f + 1f) / 18f);
            float retractAngle = -MathF.Tau;
            return Polar(Lerp(208f, 0f, retract), retractAngle, tick * -0.32f,
                false, "retract", FullOrbitArm(retractAngle));
        }

        private static FlailPreviewSample AnkleReaperRecovery(int tick)
        {
            // Keep the full-orbit arm numerically continuous while the hidden recovery settles.
            // Without this, the profile reports a false 360-degree snap back to the carry pose.
            float progress = Smoother((tick + 1f) / 60f);
            float start = FullOrbitArm(-MathF.Tau);
            float target = CarryArm - MathF.Tau;
            return new FlailPreviewSample(0f, 0f, 0f, false, null,
                Lerp(start, target, progress), visible: false);
        }

        private static FlailPreviewSample BacklashGroundLoad(int tick)
        {
            float p = Smoother((tick + 1f) / StandardTell);
            // Decelerate all the way to zero at ground contact so the reversal has no direction snap.
            float x = Lerp(CloseRadius, 76f, p);
            float y = Lerp(0f, 42f, p);
            float arm = Lerp(-1.30f, -0.62f, p);
            return Targeted(x, y, tick * 0.16f, false, "ground load / behind-only branch", arm,
                -192f, 8f, "player behind");
        }

        private static FlailPreviewSample BacklashReversal(int tick)
        {
            if (tick < 6)
            {
                // A visible planted beat: the head is stopped, the shoulder loads, then both reverse.
                float load = Smoother((tick + 1f) / 6f);
                return Targeted(76f, 42f, tick * 0.08f, false, "6t planted reversal load",
                    Lerp(-0.62f, -0.92f, load), -192f, 8f, "player behind");
            }

            if (tick < 24)
            {
                int local = tick - 6;
                float p = Weighted(local, 18, 7, 11, 7f);
                (float x, float y) = CubicBezier(76f, 42f, 30f, 54f, -108f, 43f, -192f, 8f, p);
                float arm = Lerp(-0.92f, -2.42f, p);
                return Targeted(x, y, tick * -0.40f, local >= 2 && local < 17,
                    "weighted reverse whip live", arm, -192f, 8f, "player behind");
            }

            float retract = Smoother((tick - 24f + 1f) / 18f);
            return Sample(Lerp(-192f, 0f, retract), Lerp(8f, 0f, retract), tick * -0.30f,
                false, "retract", Lerp(-2.42f, CarryArm, retract));
        }

        private static FlailPreviewSample RecoveryPose(int tick)
            => new FlailPreviewSample(0f, 0f, 0f, false, null, CarryArm, visible: false);

        private static FlailPreviewSample Targeted(float x, float y, float rotation, bool damageActive,
            string stage, float armRotation, float targetX, float targetY, string targetLabel)
            => new FlailPreviewSample(x, y, rotation, damageActive, stage, armRotation,
                hasTarget: true, targetX: targetX, targetY: targetY, targetLabel: targetLabel);

        private static FlailPreviewSample Sample(float x, float y, float rotation, bool damageActive,
            string stage, float armRotation)
            => new FlailPreviewSample(x, y, rotation, damageActive, stage, armRotation);

        private static FlailPreviewSample Polar(float radius, float angle, float rotation,
            bool damageActive, string stage, float armRotation,
            bool hasTarget = false, float targetX = 0f, float targetY = 0f, string targetLabel = null,
            float ownerOffsetX = 0f)
            => new FlailPreviewSample(MathF.Cos(angle) * radius, MathF.Sin(angle) * radius,
                rotation, damageActive, stage, armRotation,
                hasTarget: hasTarget, targetX: targetX, targetY: targetY, targetLabel: targetLabel,
                ownerOffsetX: ownerOffsetX);

        /// <summary>The wrist owns the circle; the shoulder pumps high/low with it instead of trying
        /// to rotate through 360 degrees. This keeps a continuous human arm pose around the orbit.</summary>
        private static float OrbitArm(float angle)
            => -1.23f + 0.72f * MathF.Sin(angle) - 0.14f * MathF.Cos(angle);

        private static float ArmFromFlail(float x, float y)
            => OrbitArm(MathF.Atan2(y, x));

        private static float ChainAlignedArm(float x, float y, float revolutionOffset)
            => MathF.Atan2(y, x) - MathF.PI / 2f + revolutionOffset;

        private static float FullOrbitArm(float angle)
            => angle - MathF.PI / 2f;

        private static float Weighted(int tick, int totalTicks, int easeInTicks, int easeOutTicks, float decay)
            => SwingEase.ApplyWeighted(0f, 1f, tick + 1f, totalTicks, easeInTicks, easeOutTicks, decay);

        private static float Smoother(float value)
        {
            value = Math.Clamp(value, 0f, 1f);
            return value * value * value * (value * (value * 6f - 15f) + 10f);
        }

        private static float AccelerateThenCruise(float value, float accelerationFraction)
        {
            value = Math.Clamp(value, 0f, 1f);
            accelerationFraction = Math.Clamp(accelerationFraction, 0.01f, 0.99f);
            float cruiseSpeed = 1f / (1f - accelerationFraction * 0.5f);
            if (value < accelerationFraction)
            {
                return cruiseSpeed * value * value / (2f * accelerationFraction);
            }
            return cruiseSpeed * (value - accelerationFraction * 0.5f);
        }

        private static float Lerp(float from, float to, float amount) => from + (to - from) * amount;

        private static (float x, float y) QuadraticBezier(
            float x0, float y0, float x1, float y1, float x2, float y2, float t)
        {
            t = Math.Clamp(t, 0f, 1f);
            float u = 1f - t;
            return (u * u * x0 + 2f * u * t * x1 + t * t * x2,
                u * u * y0 + 2f * u * t * y1 + t * t * y2);
        }

        private static (float x, float y) CubicBezier(
            float x0, float y0, float x1, float y1, float x2, float y2, float x3, float y3, float t)
        {
            t = Math.Clamp(t, 0f, 1f);
            float u = 1f - t;
            float uu = u * u;
            float tt = t * t;
            float x = uu * u * x0 + 3f * uu * t * x1 + 3f * u * tt * x2 + tt * t * x3;
            float y = uu * u * y0 + 3f * uu * t * y1 + 3f * u * tt * y2 + tt * t * y3;
            return (x, y);
        }
    }
}

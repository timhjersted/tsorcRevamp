using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Terraria;

namespace tsorcRevamp.NPCs.Puppets
{
    /// <summary>One post-AI sample from a naturally executing puppet attack.</summary>
    internal sealed class PuppetAttackAiSample
    {
        public int NpcId;
        public int NpcType;
        public string NpcName;
        public string AttackName;
        public string Phase;
        public int PhaseTimer;
        public string Motion;
        public int ComboStep;
        public int ComboStepCount;
        public int Direction;
        public int SpriteDirection;
        public int LockedDirection;
        public int TargetPlayerId;
        public int TargetSide;
        public Vector2 NpcCenter;
        public Vector2 NpcVelocity;
        public Vector2 TargetCenter;
        public Vector2 TargetVelocity;
        public bool HasLineOfSight;
        public float RawWeaponRotationDeg;
        public float DrawWeaponRotationDeg;
        public float CompositeArmRotationDeg;
        public float AimCorrectionDeg;
        public bool AimLocked;
        public string CompositeStretch;
        public bool CompositeArmActive;
        public bool WeaponVisible;
        public int HeldItemType;
        public bool BladeArmed;
        public float BladeReach;
        public bool HitWindowOpen;
        public bool BladeOverlapsTarget;
        public bool HitConnected;
        public bool AttackCommitted;
        public bool AttackTelegraphing;
        public string NavAction;
        public string NavReason;
        public string NavPlan;
        public bool ThrownWeaponPresent;
        public bool ThrownWeaponEmbedded;
        public int ThrownWeaponStage;
        public Vector2 ThrownWeaponCenter;
        public Vector2 ThrownWeaponVelocity;
        public float TargetDistance;
        public bool SwingArmEnabled;
        public bool BladeFlipEnabled;
        public bool AimSwingEnabled;
        public float ArmRotationOffset;
    }

    /// <summary>Exact values passed to DrawData for a visible weapon frame.</summary>
    internal sealed class PuppetAttackRenderSample
    {
        public int NpcId;
        public long Tick;
        public string Phase;
        public int PhaseTimer;
        public string Motion;
        public int Direction;
        public int SpriteDirection;
        public int LockedDirection;
        public Vector2 NpcCenter;
        public Vector2 HandWorld;
        public Vector2 VisualTipWorld;
        public Vector2 CollisionTipWorld;
        public Vector2 WeaponDirection;
        public Vector2 DrawOrigin;
        public int TextureWidth;
        public int TextureHeight;
        public float DrawScale;
        public float VisualReach;
        public float CollisionReach;
        public float RawWeaponRotationDeg;
        public float DrawWeaponRotationDeg;
        public float CompositeArmRotationDeg;
        public float ArmWorldRotationDeg;
        public float ArmWeaponErrorDeg;
        public bool CompositeArmActive;
        public string CompositeStretch;
        public bool BladeArmed;
        public string SpriteEffects;
        public bool BladeFlipActive;
    }

    /// <summary>
    /// Buffered JSON-lines telemetry for puppet attacks. Each attack is a self-contained run:
    /// run_start -> ai_frame/render_frame records -> run_summary with derived anomaly flags.
    /// </summary>
    internal static class PuppetAttackTelemetry
    {
        internal const string FileName = "tsorcRevamp-puppet-attack.jsonl";

        private sealed class RunState
        {
            public int RunId;
            public int NpcId;
            public int NpcType;
            public string NpcName;
            public string AttackName;
            public long StartTick;
            public int StartDirection;
            public Vector2 StartNpcCenter;
            public Vector2 StartTargetCenter;
            public int AiFrames;
            public int RenderFrames;
            public int FacingChanges;
            public int SpriteDirectionMismatches;
            public int LockMismatches;
            public int RotationSnaps;
            public int TipSnaps;
            public float MaxRotationStepDeg;
            public float MaxLocalTipStep;
            public float MaxArmWeaponErrorDeg;
            public float MinTargetDistance = float.MaxValue;
            public float MaxNpcSpeed;
            public int HitWindowFrames;
            public int BladeOverlapFrames;
            public bool HitConnected;
            public bool HasLastRender;
            public long LastRenderTick = -1;
            public float LastDrawRotationDeg;
            public Vector2 LastLocalTip;
            public int LastDirection;
            public bool ThrownWeaponSeen;
            public bool RetrievalStarted;
            public float RetrievalStartDistance;
            public float ClosestRetrieveDistance = float.MaxValue;
            public float LastRetrieveSignedDx;
            public bool HasLastRetrieveDx;
            public int RetrieveCrossings;
            public int RetrieveIncreasingStreak;
            public int MaxRetrieveIncreasingStreak;
            public float MaxRetrieveSingleIncrease;
            public int EmbeddedFrames;
            public string LastPhase;
            public int PhaseTransitions;
        }

        private static readonly Dictionary<int, RunState> ActiveRuns = new();
        private static readonly StringBuilder Buffer = new(64 * 1024);
        private static int _nextRunId = 1;

        internal static bool IsActive(int npcId) => ActiveRuns.ContainsKey(npcId);

        internal static void RecordAI(PuppetAttackAiSample sample)
        {
            if (!PuppetNPC.SwingDebugLog || Main.dedServ)
                return;

            PruneInactiveRuns();
            if (!ActiveRuns.TryGetValue(sample.NpcId, out RunState run))
            {
                run = BeginRun(sample);
                ActiveRuns[sample.NpcId] = run;
            }

            run.AiFrames++;
            run.MinTargetDistance = Math.Min(run.MinTargetDistance, sample.TargetDistance);
            run.MaxNpcSpeed = Math.Max(run.MaxNpcSpeed, sample.NpcVelocity.Length());
            if (sample.HitWindowOpen)
                run.HitWindowFrames++;
            if (sample.BladeOverlapsTarget)
                run.BladeOverlapFrames++;
            run.HitConnected |= sample.HitConnected;
            if (run.LastPhase != sample.Phase)
            {
                if (run.LastPhase != null)
                    run.PhaseTransitions++;
                run.LastPhase = sample.Phase;
            }
            if (sample.SpriteDirection != sample.Direction)
                run.SpriteDirectionMismatches++;

            if (sample.ThrownWeaponPresent)
            {
                run.ThrownWeaponSeen = true;
                if (sample.ThrownWeaponEmbedded)
                    run.EmbeddedFrames++;

                if (sample.ThrownWeaponEmbedded && sample.ThrownWeaponStage > 0)
                {
                    float signedDx = sample.ThrownWeaponCenter.X - sample.NpcCenter.X;
                    float absDx = Math.Abs(signedDx);
                    if (!run.RetrievalStarted)
                    {
                        run.RetrievalStarted = true;
                        run.RetrievalStartDistance = absDx;
                    }
                    run.ClosestRetrieveDistance = Math.Min(run.ClosestRetrieveDistance, absDx);

                    if (run.HasLastRetrieveDx)
                    {
                        float increase = absDx - Math.Abs(run.LastRetrieveSignedDx);
                        if (increase > 1.5f)
                        {
                            run.RetrieveIncreasingStreak++;
                            run.MaxRetrieveSingleIncrease = Math.Max(run.MaxRetrieveSingleIncrease, increase);
                        }
                        else
                        {
                            run.RetrieveIncreasingStreak = 0;
                        }
                        run.MaxRetrieveIncreasingStreak = Math.Max(
                            run.MaxRetrieveIncreasingStreak, run.RetrieveIncreasingStreak);

                        if (Math.Sign(signedDx) != Math.Sign(run.LastRetrieveSignedDx)
                            && Math.Abs(signedDx) > 2f && Math.Abs(run.LastRetrieveSignedDx) > 2f)
                        {
                            run.RetrieveCrossings++;
                        }
                    }
                    run.LastRetrieveSignedDx = signedDx;
                    run.HasLastRetrieveDx = true;
                }
            }

            AppendLine(
                $"{{\"event\":\"ai_frame\",\"run\":{run.RunId},\"tick\":{Main.GameUpdateCount}," +
                $"\"npcId\":{sample.NpcId},\"phase\":{Q(sample.Phase)},\"phaseTimer\":{sample.PhaseTimer}," +
                $"\"motion\":{Q(sample.Motion)},\"step\":{sample.ComboStep},\"stepCount\":{sample.ComboStepCount}," +
                $"\"dir\":{sample.Direction},\"spriteDir\":{sample.SpriteDirection},\"lockedDir\":{sample.LockedDirection}," +
                $"\"targetPlayer\":{sample.TargetPlayerId},\"targetSide\":{sample.TargetSide}," +
                $"\"npc\":{V(sample.NpcCenter)},\"npcVel\":{V(sample.NpcVelocity)}," +
                $"\"target\":{V(sample.TargetCenter)},\"targetVel\":{V(sample.TargetVelocity)}," +
                $"\"targetDistance\":{F(sample.TargetDistance)},\"los\":{B(sample.HasLineOfSight)}," +
                $"\"rawRotDeg\":{F(sample.RawWeaponRotationDeg)},\"drawRotDeg\":{F(sample.DrawWeaponRotationDeg)}," +
                $"\"compositeRotDeg\":{N(sample.CompositeArmRotationDeg)},\"composite\":{B(sample.CompositeArmActive)}," +
                $"\"aimCorrectionDeg\":{F(sample.AimCorrectionDeg)},\"aimLocked\":{B(sample.AimLocked)}," +
                $"\"stretch\":{Q(sample.CompositeStretch)},\"weaponVisible\":{B(sample.WeaponVisible)}," +
                $"\"heldItem\":{sample.HeldItemType},\"bladeArmed\":{B(sample.BladeArmed)},\"bladeReach\":{F(sample.BladeReach)}," +
                $"\"hitWindowOpen\":{B(sample.HitWindowOpen)},\"bladeOverlapsTarget\":{B(sample.BladeOverlapsTarget)}," +
                $"\"hitConnected\":{B(sample.HitConnected)}," +
                $"\"committed\":{B(sample.AttackCommitted)},\"telegraphing\":{B(sample.AttackTelegraphing)}," +
                $"\"navAction\":{Q(sample.NavAction)},\"navReason\":{Q(sample.NavReason)},\"navPlan\":{Q(sample.NavPlan)}," +
                $"\"thrownPresent\":{B(sample.ThrownWeaponPresent)},\"thrownEmbedded\":{B(sample.ThrownWeaponEmbedded)}," +
                $"\"thrownStage\":{sample.ThrownWeaponStage},\"thrown\":{V(sample.ThrownWeaponCenter)}," +
                $"\"thrownVel\":{V(sample.ThrownWeaponVelocity)}}}");

            // Preserve useful partial traces if playtesting is interrupted mid-attack, while
            // keeping disk traffic far below the old one-write-per-frame logger.
            if (run.AiFrames % 60 == 0)
                Flush();
        }

        internal static void RecordRender(PuppetAttackRenderSample sample)
        {
            if (!PuppetNPC.SwingDebugLog || Main.dedServ
                || !ActiveRuns.TryGetValue(sample.NpcId, out RunState run)
                || run.LastRenderTick == sample.Tick)
            {
                return;
            }

            long renderGap = run.LastRenderTick >= 0 ? sample.Tick - run.LastRenderTick : long.MaxValue;
            run.LastRenderTick = sample.Tick;
            run.RenderFrames++;
            run.MaxArmWeaponErrorDeg = Math.Max(run.MaxArmWeaponErrorDeg, sample.ArmWeaponErrorDeg);
            if (sample.LockedDirection != 0 && sample.Direction != sample.LockedDirection)
                run.LockMismatches++;
            if (run.RenderFrames > 1 && renderGap <= 2 && sample.Direction != run.LastDirection)
                run.FacingChanges++;

            Vector2 localTip = sample.VisualTipWorld - sample.NpcCenter;
            // Only compare consecutive visible frames. A thrown weapon can intentionally disappear
            // for many ticks and reappear in a completely different follow-up pose.
            if (run.HasLastRender && renderGap <= 2)
            {
                float rotationStep = AngularDistanceDeg(sample.DrawWeaponRotationDeg, run.LastDrawRotationDeg);
                float tipStep = Vector2.Distance(localTip, run.LastLocalTip);
                run.MaxRotationStepDeg = Math.Max(run.MaxRotationStepDeg, rotationStep);
                run.MaxLocalTipStep = Math.Max(run.MaxLocalTipStep, tipStep);
                if (rotationStep > 45f)
                    run.RotationSnaps++;
                if (tipStep > 48f)
                    run.TipSnaps++;
            }
            run.HasLastRender = true;
            run.LastDrawRotationDeg = sample.DrawWeaponRotationDeg;
            run.LastLocalTip = localTip;
            run.LastDirection = sample.Direction;

            AppendLine(
                $"{{\"event\":\"render_frame\",\"run\":{run.RunId},\"tick\":{sample.Tick}," +
                $"\"phase\":{Q(sample.Phase)},\"phaseTimer\":{sample.PhaseTimer},\"motion\":{Q(sample.Motion)}," +
                $"\"dir\":{sample.Direction},\"spriteDir\":{sample.SpriteDirection}," +
                $"\"lockedDir\":{sample.LockedDirection},\"npc\":{V(sample.NpcCenter)}," +
                $"\"hand\":{V(sample.HandWorld)},\"visualTip\":{V(sample.VisualTipWorld)}," +
                $"\"collisionTip\":{V(sample.CollisionTipWorld)},\"weaponDir\":{V(sample.WeaponDirection)}," +
                $"\"origin\":{V(sample.DrawOrigin)},\"texture\":[{sample.TextureWidth},{sample.TextureHeight}]," +
                $"\"scale\":{F(sample.DrawScale)},\"visualReach\":{F(sample.VisualReach)},\"collisionReach\":{F(sample.CollisionReach)}," +
                $"\"rawRotDeg\":{F(sample.RawWeaponRotationDeg)},\"drawRotDeg\":{F(sample.DrawWeaponRotationDeg)}," +
                $"\"compositeRotDeg\":{N(sample.CompositeArmRotationDeg)},\"armWorldDeg\":{N(sample.ArmWorldRotationDeg)}," +
                $"\"armWeaponErrorDeg\":{F(sample.ArmWeaponErrorDeg)},\"composite\":{B(sample.CompositeArmActive)}," +
                $"\"stretch\":{Q(sample.CompositeStretch)},\"bladeArmed\":{B(sample.BladeArmed)}," +
                $"\"effects\":{Q(sample.SpriteEffects)},\"bladeFlip\":{B(sample.BladeFlipActive)}}}");
        }

        internal static void End(int npcId, string reason)
        {
            if (!ActiveRuns.TryGetValue(npcId, out RunState run))
                return;
            WriteSummary(run, reason);
            ActiveRuns.Remove(npcId);
        }

        internal static void StopAll(string reason)
        {
            foreach (RunState run in ActiveRuns.Values.ToArray())
                WriteSummary(run, reason);
            ActiveRuns.Clear();
            Flush();
        }

        internal static void Clear()
        {
            ActiveRuns.Clear();
            Buffer.Clear();
            _nextRunId = 1;
            Directory.CreateDirectory(Path.GetDirectoryName(LogPath));
            File.WriteAllText(LogPath, string.Empty);
        }

        private static RunState BeginRun(PuppetAttackAiSample sample)
        {
            var run = new RunState
            {
                RunId = _nextRunId++,
                NpcId = sample.NpcId,
                NpcType = sample.NpcType,
                NpcName = sample.NpcName,
                AttackName = sample.AttackName,
                StartTick = (long)Main.GameUpdateCount,
                StartDirection = sample.Direction,
                LastDirection = sample.Direction,
                StartNpcCenter = sample.NpcCenter,
                StartTargetCenter = sample.TargetCenter,
                LastPhase = sample.Phase,
            };

            Vector2 relativeTarget = sample.TargetCenter - sample.NpcCenter;
            AppendLine(
                $"{{\"event\":\"run_start\",\"schema\":1,\"run\":{run.RunId},\"tick\":{run.StartTick}," +
                $"\"time\":{Q(DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fff", CultureInfo.InvariantCulture))}," +
                $"\"npcId\":{sample.NpcId},\"npcType\":{sample.NpcType},\"npc\":{Q(sample.NpcName)}," +
                $"\"attack\":{Q(sample.AttackName)},\"startPhase\":{Q(sample.Phase)},\"dir\":{sample.Direction}," +
                $"\"npcStart\":{V(sample.NpcCenter)},\"targetStart\":{V(sample.TargetCenter)}," +
                $"\"targetRelative\":{V(relativeTarget)},\"targetDistance\":{F(sample.TargetDistance)}," +
                $"\"swingArm\":{B(sample.SwingArmEnabled)},\"stretch\":{Q(sample.CompositeStretch)}," +
                $"\"armOffset\":{F(sample.ArmRotationOffset)},\"bladeFlip\":{B(sample.BladeFlipEnabled)}," +
                $"\"aimSwing\":{B(sample.AimSwingEnabled)}}}", flush: true);
            return run;
        }

        private static void WriteSummary(RunState run, string reason)
        {
            var anomalies = new List<string>();
            if (run.FacingChanges > 0)
                anomalies.Add($"facing_changed_during_visible_attack:{run.FacingChanges}");
            if (run.LockMismatches > 0)
                anomalies.Add($"facing_lock_mismatch_frames:{run.LockMismatches}");
            if (run.SpriteDirectionMismatches > 0)
                anomalies.Add($"sprite_direction_mismatch_frames:{run.SpriteDirectionMismatches}");
            if (run.RotationSnaps > 0)
                anomalies.Add($"weapon_rotation_snaps_over_45deg:{run.RotationSnaps}");
            if (run.TipSnaps > 0)
                anomalies.Add($"local_weapon_tip_snaps_over_48px:{run.TipSnaps}");
            if (run.MaxArmWeaponErrorDeg > 30f)
                anomalies.Add($"arm_weapon_angle_error:{run.MaxArmWeaponErrorDeg:F1}deg");
            if (run.MaxRetrieveIncreasingStreak >= 4)
                anomalies.Add($"retrieval_moved_away_for_ticks:{run.MaxRetrieveIncreasingStreak}");
            if (run.RetrieveCrossings > 1)
                anomalies.Add($"retrieval_crossed_axe_multiple_times:{run.RetrieveCrossings}");
            if (run.RetrievalStarted && run.ClosestRetrieveDistance > 70f)
                anomalies.Add($"retrieval_never_reached_axe:{run.ClosestRetrieveDistance:F1}px");

            long duration = Math.Max(0L, (long)Main.GameUpdateCount - run.StartTick);
            string anomalyJson = "[" + string.Join(",", anomalies.Select(Q)) + "]";
            AppendLine(
                $"{{\"event\":\"run_summary\",\"run\":{run.RunId},\"tick\":{Main.GameUpdateCount}," +
                $"\"npc\":{Q(run.NpcName)},\"attack\":{Q(run.AttackName)},\"endReason\":{Q(reason)}," +
                $"\"durationTicks\":{duration},\"aiFrames\":{run.AiFrames},\"renderFrames\":{run.RenderFrames}," +
                $"\"phaseTransitions\":{run.PhaseTransitions},\"startDir\":{run.StartDirection}," +
                $"\"hitWindowFrames\":{run.HitWindowFrames},\"bladeOverlapFrames\":{run.BladeOverlapFrames}," +
                $"\"hitConnected\":{B(run.HitConnected)}," +
                $"\"minTargetDistance\":{N(run.MinTargetDistance)},\"maxNpcSpeed\":{F(run.MaxNpcSpeed)}," +
                $"\"maxRotationStepDeg\":{F(run.MaxRotationStepDeg)},\"maxLocalTipStep\":{F(run.MaxLocalTipStep)}," +
                $"\"maxArmWeaponErrorDeg\":{F(run.MaxArmWeaponErrorDeg)},\"facingChanges\":{run.FacingChanges}," +
                $"\"lockMismatchFrames\":{run.LockMismatches},\"spriteDirectionMismatchFrames\":{run.SpriteDirectionMismatches}," +
                $"\"thrownSeen\":{B(run.ThrownWeaponSeen)}," +
                $"\"embeddedFrames\":{run.EmbeddedFrames},\"retrievalStarted\":{B(run.RetrievalStarted)}," +
                $"\"retrievalStartDistance\":{N(run.RetrievalStarted ? run.RetrievalStartDistance : float.NaN)}," +
                $"\"closestRetrieveDistance\":{N(run.RetrievalStarted ? run.ClosestRetrieveDistance : float.NaN)}," +
                $"\"retrieveCrossings\":{run.RetrieveCrossings},\"maxRetrieveAwayStreak\":{run.MaxRetrieveIncreasingStreak}," +
                $"\"maxRetrieveSingleIncrease\":{F(run.MaxRetrieveSingleIncrease)}," +
                $"\"result\":{Q(anomalies.Count == 0 ? "PASS" : "REVIEW")},\"anomalies\":{anomalyJson}}}", flush: true);
        }

        private static void PruneInactiveRuns()
        {
            foreach (int npcId in ActiveRuns.Keys.ToArray())
            {
                if (npcId < 0 || npcId >= Main.maxNPCs || !Main.npc[npcId].active)
                    End(npcId, "npc-inactive");
            }
        }

        private static float AngularDistanceDeg(float a, float b)
        {
            float delta = Math.Abs(MathHelper.WrapAngle(MathHelper.ToRadians(a - b)));
            return MathHelper.ToDegrees(delta);
        }

        private static string LogPath => Path.Combine(Main.SavePath, "Logs", FileName);
        private static string Q(string value) => "\"" + Escape(value ?? string.Empty) + "\"";
        private static string B(bool value) => value ? "true" : "false";
        private static string F(float value) => value.ToString("0.###", CultureInfo.InvariantCulture);
        private static string N(float value) => float.IsFinite(value) ? F(value) : "null";
        private static string V(Vector2 value) => "[" + F(value.X) + "," + F(value.Y) + "]";

        private static string Escape(string value)
        {
            return value.Replace("\\", "\\\\").Replace("\"", "\\\"")
                .Replace("\r", "\\r").Replace("\n", "\\n");
        }

        private static void AppendLine(string line, bool flush = false)
        {
            Buffer.AppendLine(line);
            if (flush || Buffer.Length >= 64 * 1024)
                Flush();
        }

        private static void Flush()
        {
            if (Buffer.Length == 0)
                return;
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(LogPath));
                File.AppendAllText(LogPath, Buffer.ToString());
                Buffer.Clear();
            }
            catch
            {
                // Debug telemetry must never break combat or rendering.
            }
        }
    }
}

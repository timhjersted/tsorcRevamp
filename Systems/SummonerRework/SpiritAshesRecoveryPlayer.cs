using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.Systems.SummonerRework
{
    /// <summary>
    /// Per-slot resummon cooldown for Spirit Ashes. Each individual slot gets its own 90-second warranty
    /// STAMPED AT SPAWN (see SpiritAshesMinions.OnSpawn/RecordNewSlot), not created when it dies - dying
    /// never "resets" anything, it just checks whatever warranty was already running. Die before it
    /// matures and resummon is blocked until it does; die after and it costs nothing, since there is
    /// nothing left pending. Independent of every other slot of the same type - losing your only minion
    /// and losing one of three both cost the same 90 seconds per loss; a bigger army just has more
    /// redundancy cushioning the gap, the rule itself does not treat builds differently.
    ///
    /// Deliberately does NOT gate every cast, only ones that would climb back toward a previously-proven
    /// army size - see CanSummon. That is what keeps repositioning an already-alive, already-full army
    /// (recasting a summon weapon while nothing is missing) completely free regardless of what is sitting
    /// in the recovery queue below, without ever needing to tell "summon" and "resummon" apart.
    /// </summary>
    public class SpiritAshesRecoveryPlayer : ModPlayer
    {
        /// <summary>Source-level switch for this system's debug logging, independent of the mod-wide
        /// DebugMode config toggle - see RegainPlayer.ShowDebugMessages for why. ON right now while
        /// chasing the reported auto-respawn bug; flip back to false once that is confirmed fixed.</summary>
        internal const bool ShowDebugMessages = true;

        /// <summary>Writes to Logs/spirit-ashes-recovery.log instead of the chat window - see
        /// ArcherSpirit.cs for the same file-logging pattern. A screenshot of chat loses context and
        /// mixes lines from before and after a rebuild; a plain file can be read directly, in full, in
        /// order. Truncated once at the start of each play session (first write) so old runs never bleed
        /// into a new test.</summary>
        private static bool logFileClearedThisSession = false;

        internal static void DebugLog(string message)
        {
            if (!ShowDebugMessages)
            {
                return;
            }

            try
            {
                string logPath = System.IO.Path.Combine(Main.SavePath, "Logs", "spirit-ashes-recovery.log");
                string line = $"[{System.DateTime.Now:HH:mm:ss}] tick={Main.GameUpdateCount} {message}\r\n";

                if (!logFileClearedThisSession)
                {
                    System.IO.File.WriteAllText(logPath, line);
                    logFileClearedThisSession = true;
                }
                else
                {
                    System.IO.File.AppendAllText(logPath, line);
                }
            }
            catch (System.Exception ex)
            {
                Main.NewText($"[SpiritAshes] Failed to write debug log: {ex.Message}", 255, 100, 100);
            }
        }

        /// <summary>How long a single lost slot takes to recover, in ticks. 90 seconds.</summary>
        public const int RecoveryDelayTicks = 90 * 60;

        /// <summary>
        /// A PLAYER death already costs real ground in this mod - a respawn, lost distance back to a
        /// bonfire, whatever else Souls Mode charges for dying. Carrying a pre-death ash lockout through
        /// that respawn on top would double-punish the same event for no reason, so every pending
        /// recovery is wiped the moment the player dies.
        ///
        /// UpdateDead(), NOT PostUpdate() - vanilla's Player.Update(int) checks `if (dead) { UpdateDead();
        /// ...; return; }` BEFORE the line that calls PostUpdate, so PostUpdate never runs at all while
        /// Player.dead is true. Putting this in PostUpdate (the first attempt) meant it silently never
        /// fired in exactly the situation it exists to handle.
        /// </summary>
        public override void UpdateDead()
        {
            bool hasPending = pendingRecoveriesByType.Count > 0;
            bool hasPeaks = peakCountByType.Count > 0;

            if (!hasPending && !hasPeaks)
            {
                return;
            }

            // Clear all three, not just the queue - a peak with no matching queue entry is exactly the
            // state that used to permanently lock a type out (see CanSummon's fail-safe below for the
            // other half of that fix), and a stale alive-state snapshot would let a death-triggered
            // resummon inherit a pre-death health value instead of the fresh start a player death is
            // supposed to buy. A full reset is also just the more honest "clean slate": the player's
            // ashes are gone with them, there is no "best army fielded" or "last known health" worth
            // remembering through a death.
            DebugLog($"Player death (UpdateDead) at tick={Main.GameUpdateCount} - clearing all pending recoveries, peaks, and alive-state snapshots");
            pendingRecoveriesByType.Clear();
            peakCountByType.Clear();
            lastKnownAliveStateByType.Clear();
        }

        /// <summary>
        /// Self-heals a rare buff/data desync: SpiritAshRecoveryBuff.Update() can only correct its own
        /// displayed time while it still exists, but vanilla decrements every buff's time by 1, GENERICALLY,
        /// every tick BEFORE calling that Update() hook - if our last-set value was already down to 1 on
        /// the same tick a new, longer-pending entry should have kept the buff alive, vanilla's own
        /// decrement can hit 0 and remove it first, and nothing else re-adds it except a fresh spawn.
        /// Checked here, independently of whether the buff currently exists, so a desync like that
        /// corrects itself within a tick instead of sitting wrong until the next summon.
        /// </summary>
        public override void PostUpdate()
        {
            int remaining = GetLongestRemainingTicks();

            if (remaining > 0 && !Player.HasBuff(ModContent.BuffType<SpiritAshRecoveryBuff>()))
            {
                DebugLog($"PostUpdate: buff missing but remaining={remaining} at tick={Main.GameUpdateCount} - re-adding (desync self-heal)");
                Player.AddBuff(ModContent.BuffType<SpiritAshRecoveryBuff>(), remaining);
            }
        }

        /// <summary>Per projectile type, the highest number of simultaneously-alive instances ever
        /// recorded. The "proven" army size a gap is measured against. Updated from NotifyAlive, called
        /// the moment a new instance actually spawns - see that call site for why this cannot instead be
        /// left to update lazily whenever the player happens to cast again.</summary>
        private readonly Dictionary<int, int> peakCountByType = new();

        /// <summary>Per projectile type, one pending recovery deadline (a Main.GameUpdateCount tick) for
        /// each death that has not yet had its slot refilled. Appended in death order, so the oldest
        /// death's deadline always sits first.</summary>
        private readonly Dictionary<int, List<uint>> pendingRecoveriesByType = new();

        /// <summary>
        /// Per projectile type, a continuously-refreshed snapshot of the last living instance's health,
        /// health-bar timer, and recovery deadline - written every tick from SpiritAshesMinions.AI while
        /// an instance is alive, and cleared from TakeDamage the moment one genuinely dies (0 HP).
        ///
        /// Exists so OnSpawn can still find "what it was doing a moment ago" for an instance that vanished
        /// WITHOUT dying - right-clicking its buff icon to cancel it outright is the confirmed case, and
        /// vanilla's own per-tick slot enforcement (see SpiritAshesItem.CanUseItem) is another. A live
        /// projectile-array scan (FindReplacedInstance) sees nothing in either case, since the projectile
        /// is genuinely gone, not merely about to retire - without this fallback, resummoning right after
        /// was a free, instant, unlimited full heal that bypassed Estus entirely, since a fresh spawn with
        /// no live sibling to inherit from always defaults to full health.
        /// </summary>
        private readonly Dictionary<int, (float Health, int TicksSinceLastHit, uint RecoveryDeadline)> lastKnownAliveStateByType = new();

        /// <summary>Overwrites `projectileType`'s snapshot with its current state - called every tick an
        /// instance is alive, so the snapshot is always "a moment ago", never stale by more than a tick.</summary>
        public void RecordAliveState(int projectileType, float health, int ticksSinceLastHit, uint recoveryDeadline)
        {
            lastKnownAliveStateByType[projectileType] = (health, ticksSinceLastHit, recoveryDeadline);
        }

        /// <summary>Reads back `projectileType`'s last snapshot - see lastKnownAliveStateByType. False if
        /// none is on record (never summoned this session, or its last instance genuinely died).</summary>
        public bool TryGetAliveState(int projectileType, out float health, out int ticksSinceLastHit, out uint recoveryDeadline)
        {
            if (lastKnownAliveStateByType.TryGetValue(projectileType, out (float Health, int TicksSinceLastHit, uint RecoveryDeadline) state))
            {
                health = state.Health;
                ticksSinceLastHit = state.TicksSinceLastHit;
                recoveryDeadline = state.RecoveryDeadline;
                return true;
            }

            health = 0f;
            ticksSinceLastHit = 0;
            recoveryDeadline = 0;
            return false;
        }

        /// <summary>Drops `projectileType`'s snapshot - called from TakeDamage on a genuine death, so the
        /// NEXT spawn correctly defaults to full health instead of inheriting a dead instance's 0 HP.</summary>
        public void ClearAliveState(int projectileType)
        {
            lastKnownAliveStateByType.Remove(projectileType);
        }

        /// <summary>
        /// Called from SpiritAshesMinions.OnSpawn when a TRUE new slot is born (not a reposition - those
        /// inherit an existing deadline instead of calling this). Opens one 90-second recovery entry for
        /// that projectile type, independent of any others already pending - losing two slots costs two
        /// separate 90-second waits, not one shared one.
        ///
        /// Deliberately fires at SPAWN, not at death: the warranty exists and counts down from the
        /// moment the slot is born, whether or not it ever dies. This is what makes the countdown visible
        /// immediately (rather than only appearing after a death) and what makes a death after the
        /// warranty has already matured free - there is nothing left pending to block it with.
        /// </summary>
        public void RecordNewSlot(int projectileType, uint deadline)
        {
            if (!pendingRecoveriesByType.TryGetValue(projectileType, out List<uint> deadlines))
            {
                deadlines = new List<uint>();
                pendingRecoveriesByType[projectileType] = deadlines;
            }

            deadlines.Add(deadline);
            DebugLog($"RecordNewSlot type={projectileType} at tick={Main.GameUpdateCount}, deadline={deadline}, pendingCount={deadlines.Count}");

            // Applied immediately at spawn, not at death - see SpiritAshRecoveryBuff, whose own Update()
            // recalculates the displayed time from the recovery queue every tick, so this only needs to
            // guarantee the buff exists while ANY slot's warranty is still running, not carry the number.
            Player.AddBuff(ModContent.BuffType<SpiritAshRecoveryBuff>(), (int)(deadline - Main.GameUpdateCount));
        }

        /// <summary>
        /// Records that `currentAliveCount` instances of `projectileType` are alive right now, raising
        /// its recorded peak if this is a new high. Called from SpiritAshesMinions.OnSpawn the moment a
        /// new instance is confirmed alive - see the call site for why cast-time tracking alone is not
        /// enough.
        /// </summary>
        public void NotifyAlive(int projectileType, int currentAliveCount)
        {
            int existingPeak = peakCountByType.GetValueOrDefault(projectileType);

            if (currentAliveCount > existingPeak)
            {
                peakCountByType[projectileType] = currentAliveCount;
                DebugLog($"NotifyAlive type={projectileType} raised peak {existingPeak} -> {currentAliveCount}");
            }
            else
            {
                DebugLog($"NotifyAlive type={projectileType} current={currentAliveCount}, peak stays {existingPeak}");
            }
        }

        /// <summary>
        /// True if `projectileType` has already proven it can run more than one simultaneously-alive
        /// instance. Used to gate health-inheritance on a reposition recast (see
        /// SpiritAshesMinions.OnSpawn) - a type that has only ever had one alive at a time is
        /// unambiguously being replaced when another instance is found alive, but a type that has already
        /// shown it can field several should not inherit a coexisting sibling's health just because one
        /// happens to still be up when a new one spawns.
        /// </summary>
        public bool HasProvenMultipleInstances(int projectileType)
        {
            return peakCountByType.GetValueOrDefault(projectileType) > 1;
        }

        /// <summary>
        /// True if casting a summon weapon that produces `projectileType` should be allowed right now,
        /// given the player currently owns `currentAliveCount` living instances of it.
        /// </summary>
        public bool CanSummon(int projectileType, int currentAliveCount)
        {
            int peak = peakCountByType.GetValueOrDefault(projectileType);

            // Nothing missing relative to the best army this type has ever fielded - always allow. This
            // is the case that covers repositioning (recasting while everything is still alive) and
            // ordinary army-building (NotifyAlive keeps peak in step with count as each new one spawns,
            // so this is never false while purely building up).
            if (currentAliveCount >= peak)
            {
                DebugLog($"CanSummon type={projectileType} ALLOWED - current={currentAliveCount} >= peak={peak} (nothing missing)");
                return true;
            }

            // Something LOOKS missing relative to peak, but only an ACTUAL recorded entry may ever block
            // a cast - fail open if there is not one. peak and the queue are maintained separately (see
            // NotifyAlive vs RecordNewSlot), so nothing guarantees they can never drift apart - a death
            // reset that only clears one, a future bug, anything. Refusing on a bare count mismatch with
            // no backing entry is exactly how that turns into a PERMANENT lockout instead of a temporary
            // one: nothing would ever add a fresh entry to eventually expire and unblock it.
            bool hasPending = pendingRecoveriesByType.TryGetValue(projectileType, out List<uint> deadlines) && deadlines.Count > 0;

            if (!hasPending)
            {
                DebugLog($"CanSummon type={projectileType} ALLOWED - current={currentAliveCount} < peak={peak} but no recorded entry (fail-safe)");
                return true;
            }

            bool hasExpired = deadlines.Exists(deadline => Main.GameUpdateCount >= deadline);
            string deadlineSummary = string.Join(",", deadlines.ConvertAll(d => (d > Main.GameUpdateCount ? "+" + (d - Main.GameUpdateCount) : "EXPIRED")));

            DebugLog($"CanSummon type={projectileType} current={currentAliveCount} < peak={peak} - deadlines=[{deadlineSummary}] -> {(hasExpired ? "ALLOWED" : "REFUSED")}");

            return hasExpired;
        }

        /// <summary>
        /// Called once a cast CanSummon allowed has actually gone through. Consumes the oldest expired
        /// recovery slot for `projectileType` - a no-op if the cast was pure repositioning/growth rather
        /// than filling a gap, so it never eats a slot it did not need.
        /// </summary>
        public void ConsumeRecoveryIfFillingGap(int projectileType, int currentAliveCount)
        {
            if (currentAliveCount >= peakCountByType.GetValueOrDefault(projectileType))
            {
                return;
            }

            if (!pendingRecoveriesByType.TryGetValue(projectileType, out List<uint> deadlines))
            {
                return;
            }

            int oldestExpiredIndex = deadlines.FindIndex(deadline => Main.GameUpdateCount >= deadline);

            if (oldestExpiredIndex >= 0)
            {
                deadlines.RemoveAt(oldestExpiredIndex);
            }
        }

        /// <summary>Ticks until the SOONEST currently-locked recovery slot for `projectileType` opens up
        /// - only one needs to expire for CanSummon to allow a cast again, so this is "how long until you
        /// can act", not how long the whole backlog for this type takes to clear.</summary>
        private int TicksUntilNextRecovery(int projectileType)
        {
            if (!pendingRecoveriesByType.TryGetValue(projectileType, out List<uint> deadlines) || deadlines.Count == 0)
            {
                return 0;
            }

            uint now = Main.GameUpdateCount;
            uint soonest = uint.MaxValue;

            foreach (uint deadline in deadlines)
            {
                if (deadline < soonest)
                {
                    soonest = deadline;
                }
            }

            if (soonest <= now)
            {
                return 0;
            }

            return (int)(soonest - now);
        }

        /// <summary>Ticks until every pending recovery, across every summon type this player has lost
        /// recently, has expired. Drives SpiritAshRecoveryBuff's displayed countdown - using the LONGEST
        /// rather than the soonest so the buff stays up until nothing is left waiting, instead of
        /// disappearing while a different type is still locked out.</summary>
        public int GetLongestRemainingTicks()
        {
            uint now = Main.GameUpdateCount;
            long longest = 0;

            foreach (List<uint> deadlines in pendingRecoveriesByType.Values)
            {
                foreach (uint deadline in deadlines)
                {
                    if (deadline > now)
                    {
                        long remaining = deadline - now;

                        if (remaining > longest)
                        {
                            longest = remaining;
                        }
                    }
                }
            }

            return (int)longest;
        }

        /// <summary>
        /// Every individual pending recovery slot, across every summon type, as (projectile type,
        /// ticks remaining) - one entry per unresolved death, not one per type. Drives
        /// SpiritAshRecoveryBuff's tooltip breakdown, since the buff icon itself can only show one
        /// combined number.
        /// </summary>
        public List<(int ProjectileType, int RemainingTicks)> GetAllPendingRecoveries()
        {
            var result = new List<(int, int)>();
            uint now = Main.GameUpdateCount;

            foreach (KeyValuePair<int, List<uint>> entry in pendingRecoveriesByType)
            {
                foreach (uint deadline in entry.Value)
                {
                    if (deadline > now)
                    {
                        result.Add((entry.Key, (int)(deadline - now)));
                    }
                }
            }

            result.Sort((a, b) => a.Item2.CompareTo(b.Item2));
            return result;
        }

        /// <summary>Debounces the "still recovering" floating text so holding a refused summon weapon
        /// down does not spam a new message every single tick the cast keeps failing.</summary>
        private const int RefusalMessageCooldownTicks = 60;
        private uint lastRefusalMessageTick = uint.MaxValue - RefusalMessageCooldownTicks;

        /// <summary>Shows a floating "still recovering, Ns" message above the player for
        /// `projectileType`, called whenever CanSummon has just refused a cast for that type.</summary>
        public void NotifyRefusal(int projectileType)
        {
            if (Main.GameUpdateCount - lastRefusalMessageTick < RefusalMessageCooldownTicks)
            {
                return;
            }

            lastRefusalMessageTick = Main.GameUpdateCount;

            int ticksForThisType = TicksUntilNextRecovery(projectileType);
            int secondsRemaining = (int)System.Math.Ceiling(ticksForThisType / 60.0);
            DebugLog($"NotifyRefusal type={projectileType} ticksUntilNextRecovery={ticksForThisType} vs GetLongestRemainingTicks={GetLongestRemainingTicks()} at tick={Main.GameUpdateCount}");
            CombatText.NewText(Player.Hitbox, Color.Crimson,
                LangUtils.GetTextValue("CommonItemTooltip.SpiritAshResummonCooldown", secondsRemaining));
        }
    }
}

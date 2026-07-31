using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Utilities.Balance
{
    /// <summary>
    /// Automatic weapon benchmarking against target dummies.
    ///
    /// No arming step: hit a dummy with anything and a run opens for that weapon, closing on its own
    /// once the weapon stops landing hits. Swap weapon, keep swinging, and you get one record per
    /// weapon — the point being to grind through a large chunk of the roster in one sitting.
    ///
    /// Runs are tracked **per source item, concurrently**, not one at a time. That matters for summons:
    /// minions keep hitting long after you've swapped to a sword, and their damage still belongs to the
    /// staff. One-run-at-a-time keyed on the held item would have mangled every summon weapon.
    ///
    /// This is the companion to <see cref="BalanceLog"/>. Boss encounters can't isolate a weapon — their
    /// DPS is the product of weapon, gear, skill, arena and boss mechanics. A dummy isolates the weapon.
    /// </summary>
    internal static class WeaponBench
    {
        internal const string FileName = "tsorcRevamp-weaponbench.jsonl";

        /// <summary>On by default. Target dummies effectively only exist where someone deliberately
        /// placed one, so this can't fire during ordinary play.</summary>
        internal static bool AutoEnabled = true;

        /// <summary>A run closes this long after its last landed hit.</summary>
        internal static int IdleTimeoutSeconds = 4;

        /// <summary>
        /// Length of one sample. Continuous attacking is chopped into repeated samples of this length
        /// rather than producing one long run, so each weapon ends up with several independent numbers
        /// to take a median and a spread from — one measurement can't tell you whether it was typical.
        /// A segment is only cut once it has at least <see cref="MinHits"/> hits, so slow weapons
        /// produce longer samples instead of a stream of undersized ones. 0 disables segmenting.
        /// </summary>
        internal static int SampleSeconds = 10;

        /// <summary>Runs smaller than this are discarded rather than written — an accidental clip of a
        /// dummy in passing is not a measurement.</summary>
        internal static int MinHits = 5;
        internal static float MinSpanSeconds = 2f;

        private sealed class BenchRun
        {
            public AttackKey key;
            public int itemType;
            public string item;
            public string itemMod;
            public int prefix;
            public string prefixName;
            public int baseDamage;
            public int baseCrit;
            public int useTime;
            public DamageClass damageClass;

            public long firstHitTick;
            public long lastHitTick;
            public long damage;
            public int hits;
            public int crits;

            public int dummyType;
            public string dummyName;
            public int dummyDefense;
            public int dummiesPresent;

            /// <summary>Damage per individual dummy (keyed by NPC slot), which is what turns a run
            /// against several dummies into a piercing/cleave measurement.</summary>
            public readonly Dictionary<int, long> perTarget = new();

            public int projectilesSpawned;

            public int maxTargetsPerAttack;
            public int attackCount;
            public int attackTargetSum;

            // Running aggregates rather than a per-hit list — a fast weapon over a long run would
            // otherwise accumulate thousands of entries for no benefit.
            public int minHit = int.MaxValue;
            public int maxHit;
            public double damageSum;
            public double damageSumSquares;

            public float rangeSum;
            public float maxRange;

            public readonly List<long> damageTimeline = new();

            public int startMana;
            public float manaSpent;
            public float staminaSpent;
            public int staminaTicks;
            public int staminaStarvedTicks;
            public float minStamina = float.MaxValue;
            public readonly List<AmmoUsage> ammo = new();
        }

        /// <summary>
        /// Runs are keyed by weapon *and* which of its attacks fired, because a single item here can
        /// carry up to four distinct attacks (see <see cref="Items.Weapons.FourAttackWeaponControls"/>).
        /// Keying on item alone would average a light poke together with a committed heavy swing and
        /// report a number that describes neither.
        /// </summary>
        private readonly record struct AttackKey(int ItemType, int Mode, int Alt);

        private static readonly Dictionary<AttackKey, BenchRun> ActiveRuns = new();
        private static readonly List<string> SessionResults = new();
        private static readonly List<AttackKey> Expired = new();
        private static float _lastMana;
        private static float _lastStamina;
        private static readonly Dictionary<AttackKey, int> SampleCounts = new();
        private static readonly Dictionary<AttackKey, (int Count, long LastTick)> PendingSpawns = new();
        private static readonly HashSet<int> MeleeTickTargets = new();
        private static long _meleeTick = -1;
        private static AttackKey _meleeKey;

        internal static int ActiveRunCount => ActiveRuns.Count;
        internal static IReadOnlyList<string> Results => SessionResults;

        // ---------------------------------------------------------------- capture

        /// <summary>
        /// Melee swings have no projectile to hang a target count on, so same-tick hits are treated as
        /// one attack — Terraria resolves a single swing's hits against every overlapping NPC within
        /// one update. Returns the 1-based index of this target within the current swing.
        /// </summary>
        internal static int MeleeAttackIndex(int itemType, int mode, int alt, int npcId)
        {
            long now = Main.GameUpdateCount;
            var key = new AttackKey(itemType, mode, alt);
            if (now != _meleeTick || !key.Equals(_meleeKey))
            {
                _meleeTick = now;
                _meleeKey = key;
                MeleeTickTargets.Clear();
            }

            MeleeTickTargets.Add(npcId);
            return MeleeTickTargets.Count;
        }

        /// <summary>Reads the mod's own multi-attack control state, so the bench splits a weapon's
        /// attacks exactly where its designer split them rather than inventing its own boundary.</summary>
        internal static int AttackModeOf(Player player)
        {
            try
            {
                return Items.Weapons.FourAttackWeaponControls.GetAttackMode(player);
            }
            catch
            {
                return 0;
            }
        }

        /// <param name="attackTargetIndex">1-based position of this target within the attack that hit
        /// it (one projectile's lifetime, or one melee swing).</param>
        internal static void RecordHit(NPC npc, Player player, int itemType, int ammoType, int damageDone,
            bool crit, int attackMode, int altFunction, int attackTargetIndex = 1)
        {
            if (!AutoEnabled || damageDone <= 0 || itemType <= 0
                || npc == null || npc.type != NPCID.TargetDummy
                || player == null || player.whoAmI != Main.myPlayer
                || Main.dedServ)
            {
                return;
            }

            long now = Main.GameUpdateCount;

            var key = new AttackKey(itemType, attackMode, altFunction);
            if (!ActiveRuns.TryGetValue(key, out BenchRun run))
            {
                run = BeginRun(player, npc, key, now);
                if (run == null)
                    return;

                if (ActiveRuns.Count == 0)
                {
                    // First run of a batch: prime the resource baselines so the first tick doesn't
                    // register a phantom spend.
                    _lastMana = player.statMana;
                    _lastStamina = CurrentStamina(player);
                }

                ActiveRuns[key] = run;
            }

            run.lastHitTick = now;
            run.damage += damageDone;
            run.hits++;
            if (crit)
                run.crits++;

            run.perTarget.TryGetValue(npc.whoAmI, out long soFar);
            run.perTarget[npc.whoAmI] = soFar + damageDone;

            if (attackTargetIndex <= 1)
                run.attackCount++;
            run.attackTargetSum++;
            run.maxTargetsPerAttack = Math.Max(run.maxTargetsPerAttack, attackTargetIndex);

            run.minHit = Math.Min(run.minHit, damageDone);
            run.maxHit = Math.Max(run.maxHit, damageDone);
            run.damageSum += damageDone;
            run.damageSumSquares += (double)damageDone * damageDone;

            float range = Vector2.Distance(player.Center, npc.Center);
            run.rangeSum += range;
            run.maxRange = Math.Max(run.maxRange, range);

            int second = (int)((now - run.firstHitTick) / 60L);
            while (run.damageTimeline.Count <= second)
                run.damageTimeline.Add(0);
            run.damageTimeline[second] += damageDone;

            if (ammoType > 0)
                AddAmmo(run.ammo, ammoType, damageDone);
        }

        /// <summary>
        /// Counted from projectile spawn, not from hits, so that shots which miss entirely still show
        /// up. Without this a weapon that sprays and misses reads identically to a precise one.
        /// </summary>
        internal static void NotifyProjectileSpawned(int itemType, int attackMode, int altFunction)
        {
            if (!AutoEnabled || itemType <= 0)
                return;

            var key = new AttackKey(itemType, attackMode, altFunction);
            if (ActiveRuns.TryGetValue(key, out BenchRun run))
            {
                run.projectilesSpawned++;
                return;
            }

            // A run only opens on the first *hit*, which is necessarily after the projectile that
            // caused it spawned. Counting only into open runs therefore missed the opening shot — and
            // for a weapon whose damage all comes from one persistent projectile (flails, the
            // long-lived magic projectiles) it missed every shot, reporting 0 spawns forever.
            // The first real test session showed exactly that for Blood Lust Cluster and DivineAfflicter.
            // Only a recent burst is kept. This fires for every projectile the player spawns anywhere,
            // so without expiry a session spent shooting enemies would hand the next dummy run a
            // spawn count in the hundreds and report a nonsense hit rate.
            long now = Main.GameUpdateCount;
            PendingSpawns.TryGetValue(key, out (int Count, long LastTick) pending);
            int carried = now - pending.LastTick > PendingSpawnMemoryTicks ? 0 : pending.Count;
            PendingSpawns[key] = (carried + 1, now);
        }

        private const long PendingSpawnMemoryTicks = 120;   // 2 seconds

        /// <summary>Hands a newly opened run the spawns that preceded its first landed hit.</summary>
        private static int ClaimPendingSpawns(AttackKey key)
        {
            if (!PendingSpawns.TryGetValue(key, out (int Count, long LastTick) pending))
                return 0;
            PendingSpawns.Remove(key);
            return Main.GameUpdateCount - pending.LastTick > PendingSpawnMemoryTicks ? 0 : pending.Count;
        }

        internal static void Update()
        {
            if (ActiveRuns.Count == 0)
                return;

            if (Main.gameMenu || Main.LocalPlayer == null)
            {
                ActiveRuns.Clear();
                return;
            }

            AccumulateResourceSpend(Main.LocalPlayer);

            long now = Main.GameUpdateCount;
            long timeout = IdleTimeoutSeconds * 60L;

            Expired.Clear();
            foreach (KeyValuePair<AttackKey, BenchRun> pair in ActiveRuns)
            {
                BenchRun run = pair.Value;

                // Idle: the weapon stopped landing hits.
                if (now - run.lastHitTick >= timeout)
                {
                    Expired.Add(pair.Key);
                    continue;
                }

                // Segment: still going, but this sample is long enough to stand on its own. The next
                // hit opens a fresh sample for the same weapon.
                if (SampleSeconds > 0
                    && now - run.firstHitTick >= SampleSeconds * 60L
                    && run.hits >= MinHits)
                {
                    Expired.Add(pair.Key);
                }
            }

            foreach (AttackKey key in Expired)
            {
                BenchRun run = ActiveRuns[key];
                ActiveRuns.Remove(key);
                Finish(run, Main.LocalPlayer);
            }
        }

        /// <summary>
        /// Credits mana and stamina drops to the run for the weapon currently in hand.
        ///
        /// Sampled per tick as *decreases* rather than measured end-minus-start, because both
        /// resources regenerate (and Cerulean refills mana outright) — a start/end difference would
        /// report a weapon that drained and refilled its whole mana pool as having spent nothing.
        /// </summary>
        private static void AccumulateResourceSpend(Player player)
        {
            float mana = player.statMana;
            float stamina = CurrentStamina(player);

            float manaDrop = _lastMana - mana;
            float staminaDrop = _lastStamina - stamina;
            _lastMana = mana;
            _lastStamina = stamina;

            // Attribute to the held weapon: concurrent runs are almost always "minions still ticking
            // while I swing something else", and the resource cost belongs to what's being used now.
            Item held = player.HeldItem;
            if (held == null || held.IsAir)
                return;

            // The held weapon may have several attacks in flight at once; charge the cost to whichever
            // of its attacks landed most recently.
            BenchRun run = null;
            foreach (KeyValuePair<AttackKey, BenchRun> pair in ActiveRuns)
            {
                if (pair.Key.ItemType == held.type
                    && (run == null || pair.Value.lastHitTick > run.lastHitTick))
                {
                    run = pair.Value;
                }
            }

            if (run == null)
                return;

            // Sampled every tick, not only on a drop, so the starvation fraction reflects real time
            // spent empty rather than time spent spending.
            run.staminaTicks++;
            run.minStamina = Math.Min(run.minStamina, stamina);
            float starvedThreshold = Math.Max(1f, CurrentStaminaMax(player) * 0.02f);
            if (stamina <= starvedThreshold)
                run.staminaStarvedTicks++;

            if (manaDrop > 0f)
                run.manaSpent += manaDrop;
            if (staminaDrop > 0f)
                run.staminaSpent += staminaDrop;
        }

        internal static float CurrentStamina(Player player)
        {
            try
            {
                return player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceCurrent;
            }
            catch
            {
                return 0f;
            }
        }

        internal static float CurrentStaminaMax(Player player)
        {
            try
            {
                return player.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceMax2;
            }
            catch
            {
                return 0f;
            }
        }

        /// <summary>Drops any in-flight runs without writing them — a run interrupted by leaving the
        /// world has no meaningful duration.</summary>
        internal static void Abort()
        {
            ActiveRuns.Clear();
            PendingSpawns.Clear();
        }

        internal static void ClearSessionResults()
        {
            SessionResults.Clear();
            SampleCounts.Clear();
        }

        private static int NextSampleIndex(AttackKey key)
        {
            SampleCounts.TryGetValue(key, out int count);
            count++;
            SampleCounts[key] = count;
            return count;
        }

        /// <summary>Human-readable name for which attack this was, so records are interpretable without
        /// knowing how the control scheme maps to the raw mode/alt numbers.</summary>
        private static string VariantName(AttackKey key)
        {
            bool advanced = false;
            try
            {
                advanced = Items.Weapons.FourAttackWeaponControls.AdvancedControlsEnabled;
            }
            catch
            {
                // Config unavailable; fall back to the raw description below.
            }

            if (advanced)
            {
                string aim = key.Mode == 1 ? "low" : "high";
                return key.Alt == 1 ? $"{aim}+alt" : aim;
            }

            return key.Alt == 1 || key.Mode == 1 ? "alt" : "primary";
        }

        // ---------------------------------------------------------------- lifecycle

        private static BenchRun BeginRun(Player player, NPC dummy, AttackKey key, long now)
        {
            var probe = new Item();
            probe.SetDefaults(key.ItemType);
            if (probe.IsAir)
                return null;

            Item live = FindItem(player, key.ItemType);

            return new BenchRun
            {
                key = key,
                itemType = key.ItemType,
                item = probe.ModItem?.Name ?? probe.Name,
                itemMod = probe.ModItem?.Mod?.Name ?? "Terraria",
                prefix = live?.prefix ?? 0,
                prefixName = BalanceLog.PrefixName(live?.prefix ?? 0),
                baseDamage = probe.damage,
                baseCrit = probe.crit,
                useTime = probe.useTime,
                damageClass = probe.DamageType ?? DamageClass.Default,
                firstHitTick = now,
                lastHitTick = now,
                dummyType = dummy.type,
                dummyName = dummy.ModNPC?.Name ?? dummy.TypeName,
                // Recorded rather than assumed — the mod edits a lot of vanilla NPCs, and a nonzero
                // dummy defense would quietly skew every number in the file.
                dummyDefense = dummy.defense,
                dummiesPresent = CountDummies(player),
                projectilesSpawned = ClaimPendingSpawns(key),
                startMana = player.statMana,
            };
        }

        /// <summary>Dummies within reach of the test, not every dummy in the world — a forgotten dummy
        /// in a distant base would otherwise make every weapon look like it was missing targets.</summary>
        private static int CountDummies(Player player)
        {
            const float radius = 1600f;
            int count = 0;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (npc != null && npc.active && npc.type == NPCID.TargetDummy
                    && Vector2.Distance(player.Center, npc.Center) <= radius)
                {
                    count++;
                }
            }
            return count;
        }

        private static void Finish(BenchRun run, Player player)
        {
            float seconds = EffectiveSeconds(run);
            if (run.hits < MinHits || seconds < MinSpanSeconds)
                return;   // too small to mean anything; discarded silently so batch testing stays quiet

            try
            {
                float dps = run.damage / seconds;
                float classMultiplier = player.GetDamage(run.damageClass).ApplyTo(1f);
                GearSnapshot gear = BalanceLog.CaptureGear(player);

                List<long> perTarget = run.perTarget.Values.OrderByDescending(d => d).ToList();
                long bestTarget = perTarget.Count > 0 ? perTarget[0] : 0;

                float mean = (float)(run.damageSum / run.hits);
                float variance = (float)(run.damageSumSquares / run.hits) - mean * mean;

                var record = new WeaponBenchmark
                {
                    session = BalanceLog.SessionId,
                    modVersion = BalanceLog.ModVersion(),
                    startedAt = DateTimeOffset.Now.ToString("O", CultureInfo.InvariantCulture),
                    itemType = run.itemType,
                    item = run.item,
                    itemMod = run.itemMod,
                    prefix = run.prefix,
                    prefixName = run.prefixName,
                    baseDamage = run.baseDamage,
                    baseCrit = run.baseCrit,
                    useTime = run.useTime,
                    damageClass = run.damageClass.Name,
                    windowSeconds = (int)MathF.Round(seconds),
                    variant = VariantName(run.key),
                    attackMode = run.key.Mode,
                    altFunction = run.key.Alt,
                    sampleIndex = NextSampleIndex(run.key),
                    dummyType = run.dummyType,
                    dummy = run.dummyName,
                    dummyDefense = run.dummyDefense,
                    totalDamage = run.damage,
                    hits = run.hits,
                    crits = run.crits,
                    rawDps = dps,
                    classMultiplier = classMultiplier,
                    classCrit = player.GetCritChance(run.damageClass),
                    normalizedDps = classMultiplier > 0.01f ? dps / classMultiplier : dps,
                    gearNeutral = IsGearNeutral(gear),
                    manaSpent = (long)run.manaSpent,
                    gear = gear,

                    targetsHit = run.perTarget.Count,
                    dummiesPresent = run.dummiesPresent,
                    singleTargetDps = bestTarget / seconds,
                    crowdMultiplier = bestTarget > 0 ? run.damage / (float)bestTarget : 1f,
                    maxTargetsPerAttack = run.maxTargetsPerAttack,
                    avgTargetsPerAttack = run.attackCount > 0
                        ? run.attackTargetSum / (float)run.attackCount
                        : 0f,

                    projectilesSpawned = run.projectilesSpawned,
                    hitsPerProjectile = run.projectilesSpawned > 0
                        ? run.hits / (float)run.projectilesSpawned
                        : 0f,
                    hitsPerSecond = run.hits / seconds,
                    critRate = run.hits > 0 ? run.crits / (float)run.hits : 0f,

                    avgDamagePerHit = mean,
                    minHit = run.minHit == int.MaxValue ? 0 : run.minHit,
                    maxHit = run.maxHit,
                    damageStdDev = variance > 0f ? MathF.Sqrt(variance) : 0f,

                    avgHitRange = run.hits > 0 ? run.rangeSum / run.hits : 0f,
                    maxHitRange = run.maxRange,
                    staminaSpent = run.staminaSpent,
                    staminaStarvedFraction = run.staminaTicks > 0
                        ? run.staminaStarvedTicks / (float)run.staminaTicks
                        : 0f,
                    minStamina = run.minStamina == float.MaxValue ? 0f : run.minStamina,
                };
                record.ammo.AddRange(run.ammo);
                record.perTargetDamage.AddRange(perTarget);
                record.damageTimeline.AddRange(run.damageTimeline);

                BalanceLog.Append(FileName, record);

                string label = run.item + (string.IsNullOrEmpty(run.prefixName) ? "" : $" ({run.prefixName})")
                    + $" [{record.variant}] #{record.sampleIndex}";
                // Distinguishing these two out loud matters: they produce near-identical crowd numbers
                // and mean opposite things.
                string crowd = string.Empty;
                if (record.targetsHit > 1 && record.maxTargetsPerAttack > 1)
                {
                    crowd = $" | CLEAVE {record.maxTargetsPerAttack} max / {record.avgTargetsPerAttack:0.0} avg per attack" +
                            $", {record.crowdMultiplier:0.00}x total, {record.singleTargetDps:0.0} single-target";
                }
                else if (record.targetsHit > 1)
                {
                    crowd = $" | SCATTER across {record.targetsHit} targets (1 per attack) — " +
                            $"single-target DPS is the {dps:0.0} raw figure, not {record.singleTargetDps:0.0}";
                }

                SessionResults.Add(
                    $"{label}: {dps:0.0} raw / {record.normalizedDps:0.0} norm " +
                    $"({run.hits} hits, {seconds:0.0}s, {record.avgDamagePerHit:0} avg/hit){crowd}");

                Main.NewText(
                    $"[Bench] {label}: {dps:0.0} DPS raw / {record.normalizedDps:0.0} normalized " +
                    $"({run.hits} hits, {run.crits} crits, {seconds:0.0}s){crowd}",
                    record.gearNeutral ? Color.Lime : Color.Yellow);

                if (record.staminaStarvedFraction > 0.25f)
                {
                    Main.NewText(
                        $"[Bench] STAMINA-GATED: empty {record.staminaStarvedFraction:P0} of the sample — " +
                        "this DPS is limited by stamina regen, not by the weapon.",
                        Color.Orange);
                }
            }
            catch
            {
                // Never let a benchmark write take the game down.
            }
        }

        /// <summary>
        /// Duration for the DPS divisor. Using raw first-hit-to-last-hit span systematically inflates
        /// DPS, because it counts the gaps *between* hits but not the cooldown after the final one.
        /// Scaling by hits/(hits-1) adds one mean inter-hit interval back, which is the standard
        /// correction and matters most for slow, hard-hitting weapons.
        /// </summary>
        private static float EffectiveSeconds(BenchRun run)
        {
            float span = (run.lastHitTick - run.firstHitTick) / 60f;
            if (run.hits < 2 || span <= 0f)
                return 0f;
            return span * run.hits / (run.hits - 1);
        }

        /// <summary>
        /// A run needs no normalization when the worn gear contributes nothing to damage.
        ///
        /// Tests stat neutrality, NOT an empty equipment list: the first real test session ran in a
        /// Capricorn Helmet and Cultist Robe — zero defense, zero damage bonus, purely cosmetic — and
        /// every one of its 55 records got flagged as needing correction when 52 of them were already
        /// clean. Flagging clean data as dirty is the failure mode that gets a flag ignored.
        /// </summary>
        private static bool IsGearNeutral(GearSnapshot gear)
        {
            return Near1(gear.meleeDamage) && Near1(gear.rangedDamage) && Near1(gear.magicDamage)
                && Near1(gear.summonDamage) && Near1(gear.genericDamage)
                && Near0(gear.meleeCrit) && Near0(gear.rangedCrit) && Near0(gear.magicCrit)
                && Near1(gear.meleeSpeed);
        }

        private static bool Near0(float value) => Math.Abs(value) < 0.005f;

        private static bool Near1(float value) => Math.Abs(value - 1f) < 0.005f;

        private static Item FindItem(Player player, int itemType)
        {
            if (player.HeldItem != null && player.HeldItem.type == itemType)
                return player.HeldItem;
            for (int i = 0; i < player.inventory.Length; i++)
            {
                if (player.inventory[i] != null && player.inventory[i].type == itemType)
                    return player.inventory[i];
            }
            return null;
        }

        private static void AddAmmo(List<AmmoUsage> list, int ammoType, int damageDone)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].type == ammoType)
                {
                    list[i].damage += damageDone;
                    list[i].hits++;
                    return;
                }
            }

            var probe = new Item();
            probe.SetDefaults(ammoType);
            list.Add(new AmmoUsage
            {
                type = ammoType,
                name = probe.ModItem?.Name ?? probe.Name,
                damage = damageDone,
                hits = 1,
            });
        }
    }

    /// <summary>
    /// Usage:
    ///   /weaponbench             → status
    ///   /weaponbench on | off    → enable/disable automatic dummy benchmarking
    ///   /weaponbench sample &lt;sec&gt; → split continuous attacking into samples this long (default 10, 0 = off)
    ///   /weaponbench idle &lt;sec&gt;   → seconds of no hits before a run closes (default 4)
    ///   /weaponbench min &lt;hits&gt;  → minimum hits for a run to be recorded (default 5)
    ///   /weaponbench summary     → list every weapon benchmarked this session
    /// </summary>
    public class WeaponBenchCommand : ModCommand
    {
        public override CommandType Type => CommandType.Chat;

        public override string Command => "weaponbench";

        public override string Description => "Automatic weapon DPS benchmarking against target dummies";

        public override void Action(CommandCaller caller, string input, string[] args)
        {
            string sub = args.Length > 0 ? args[0].ToLowerInvariant() : "status";

            switch (sub)
            {
                case "on":
                    WeaponBench.AutoEnabled = true;
                    ReplyStatus(caller);
                    break;

                case "off":
                    WeaponBench.AutoEnabled = false;
                    WeaponBench.Abort();
                    ReplyStatus(caller);
                    break;

                case "idle":
                    if (args.Length >= 2 && int.TryParse(args[1], out int idle) && idle >= 1 && idle <= 60)
                    {
                        WeaponBench.IdleTimeoutSeconds = idle;
                        caller.Reply($"Runs now close after {idle}s without a hit.", Color.Lime);
                    }
                    else
                    {
                        caller.Reply("Usage: /weaponbench idle <seconds 1-60>", Color.Orange);
                    }
                    break;

                case "sample":
                    if (args.Length >= 2 && int.TryParse(args[1], out int sample) && sample >= 0 && sample <= 300)
                    {
                        WeaponBench.SampleSeconds = sample;
                        caller.Reply(
                            sample == 0
                                ? "Segmenting off — continuous attacking is one long run."
                                : $"Continuous attacking now splits into ~{sample}s samples.",
                            Color.Lime);
                    }
                    else
                    {
                        caller.Reply("Usage: /weaponbench sample <seconds, 0 to disable>", Color.Orange);
                    }
                    break;

                case "min":
                    if (args.Length >= 2 && int.TryParse(args[1], out int min) && min >= 1 && min <= 1000)
                    {
                        WeaponBench.MinHits = min;
                        caller.Reply($"Runs now need at least {min} hits to be recorded.", Color.Lime);
                    }
                    else
                    {
                        caller.Reply("Usage: /weaponbench min <hits>", Color.Orange);
                    }
                    break;

                case "summary":
                    Summary(caller);
                    break;

                case "status":
                    ReplyStatus(caller);
                    break;

                default:
                    caller.Reply(
                        "Usage: /weaponbench [on|off|sample <sec>|idle <sec>|min <hits>|summary|status]",
                        Color.Orange);
                    break;
            }
        }

        private static void ReplyStatus(CommandCaller caller)
        {
            caller.Reply(
                $"Auto weapon bench: {(WeaponBench.AutoEnabled ? "ON" : "OFF")} — " +
                $"hit any Target Dummy and a run opens for that weapon.",
                WeaponBench.AutoEnabled ? Color.Lime : Color.Gray);
            caller.Reply(
                $"Sample {(WeaponBench.SampleSeconds > 0 ? WeaponBench.SampleSeconds + "s" : "off")} | " +
                $"closes after {WeaponBench.IdleTimeoutSeconds}s idle | min {WeaponBench.MinHits} hits | " +
                $"{WeaponBench.ActiveRunCount} run(s) in progress | {WeaponBench.Results.Count} recorded this session",
                Color.LightBlue);
            // Surfaced up front because it silently determines whether stamina costs are recorded at
            // all — a whole session benched in Classic yields staminaSpent ~0 for every weapon.
            GearSnapshot gear = BalanceLog.CaptureGear(caller.Player);
            caller.Reply(
                $"Souls mode: {gear.soulsMode} | weapon stamina x{gear.weaponStaminaMult:0.00}" +
                $"{(gear.tired ? " (TIRED +25%)" : "")} | stamina {gear.staminaCurrent:N0}/{gear.staminaMax:N0}",
                gear.usesWeaponStamina ? Color.LightBlue : Color.Orange);

            caller.Reply($"File: Logs/{WeaponBench.FileName}", Color.LightBlue);
        }

        private static void Summary(CommandCaller caller)
        {
            if (WeaponBench.Results.Count == 0)
            {
                caller.Reply("No weapons benchmarked yet this session.", Color.Gray);
                return;
            }

            caller.Reply($"--- {WeaponBench.Results.Count} weapon(s) benchmarked this session ---", Color.Yellow);
            foreach (string line in WeaponBench.Results)
                caller.Reply(line, Color.White);
        }
    }
}

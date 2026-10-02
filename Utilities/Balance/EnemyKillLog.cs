using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using tsorcRevamp.Systems;

namespace tsorcRevamp.Utilities.Balance
{
    /// <summary>How many kills of one enemy in one world stage a character has made, and how long they took, as a histogram.</summary>
    internal sealed class KillStat
    {
        public int Count;
        public long TotalTicks;
        public int[] Buckets = new int[EnemyKillLog.BucketEdgesSeconds.Length + 1];
    }

    /// <summary>
    /// A character's kill counters, saved with the character so "the fifth Dunlending kill in Hardmode" still means the
    /// fifth after a restart. Keyed "EnemyName|PHM", "EnemyName|HM" or "EnemyName|SHM" - each stage counts from zero.
    /// </summary>
    internal sealed class EnemyKillStatsPlayer : ModPlayer
    {
        internal readonly Dictionary<string, KillStat> Stats = new();

        public override void SaveData(TagCompound tag)
        {
            List<TagCompound> entries = new();

            foreach (KeyValuePair<string, KillStat> pair in Stats)
            {
                entries.Add(new TagCompound
                {
                    ["key"] = pair.Key,
                    ["count"] = pair.Value.Count,
                    ["ticks"] = pair.Value.TotalTicks,
                    ["buckets"] = pair.Value.Buckets,
                });
            }

            tag["killStats"] = entries;
        }

        public override void LoadData(TagCompound tag)
        {
            Stats.Clear();

            foreach (TagCompound entry in tag.GetList<TagCompound>("killStats"))
            {
                KillStat stat = new KillStat
                {
                    Count = entry.GetInt("count"),
                    TotalTicks = entry.GetLong("ticks"),
                };

                int[] savedBuckets = entry.GetIntArray("buckets");

                for (int i = 0; i < savedBuckets.Length && i < stat.Buckets.Length; i++)
                {
                    stat.Buckets[i] = savedBuckets[i];
                }

                Stats[entry.GetString("key")] = stat;
            }
        }
    }

    /// <summary>
    /// Enemy time-to-kill sampling for the balance log. Every kill of an enemy in the HP registry is counted (and put in a
    /// histogram) in the character's save, but only the 1st, 5th, 10th and 20th kill per enemy per world stage is written
    /// to the log file, as a small record - so the file stays small however long the run is.
    ///
    /// Time to kill is the span from the first player hit on an enemy to its death. Damage is credited to the item that
    /// spawned the projectile (see <see cref="BalanceSourceProjectile"/>), so minions, sentries and whips show up as their
    /// own weapons in the record. Single-player only, like the boss log.
    /// </summary>
    internal static class EnemyKillLog
    {
        /// <summary>Histogram edges in seconds: under 1, 1-2, 2-4, 4-8, 8-15, 15-30, 30-60, 60 and over.</summary>
        internal static readonly int[] BucketEdgesSeconds = { 1, 2, 4, 8, 15, 30, 60 };

        private static readonly int[] SampledKillNumbers = { 1, 5, 10, 20 };
        private static readonly string[] StageCodes = { "PHM", "HM", "SHM" };

        // A pause this long between hits ends an engagement: an enemy that was hit once, left, and killed later
        // should not read as a minutes-long kill.
        private const int EngagementGapTicks = 60 * 10;

        private const int PruneIntervalTicks = 60 * 10;
        private const int MaxTrackers = 512;
        private const int WeaponsPerSample = 3;

        private sealed class WeaponTally
        {
            public long Damage;
            public int Hits;
        }

        private sealed class KillTracker
        {
            public int NpcType;
            public int FirstHitTick;
            public int LastHitTick;
            public int Hits;
            public long Damage;
            public int MaxHit;
            public readonly Dictionary<int, WeaponTally> Weapons = new();
        }

        private static readonly Dictionary<int, KillTracker> Trackers = new();
        private static uint _nextPruneTick;

        /// <param name="itemType">The item credited with the hit, or 0 when it came from nothing the player used.</param>
        internal static void RecordHit(NPC npc, Player player, int itemType, int damageDone)
        {
            if (!BalanceLog.Active || damageDone <= 0 || npc == null || player == null || player.whoAmI != Main.myPlayer)
            {
                return;
            }

            if (BalanceLog.IsBossAnchor(npc) || npc.friendly || npc.townNPC || !EnemyBalance.IsRegisteredEnemy(npc.type))
            {
                return;
            }

            int now = (int)Main.GameUpdateCount;

            bool hasTracker = Trackers.TryGetValue(npc.whoAmI, out KillTracker tracker);
            bool trackerIsStale = hasTracker && (tracker.NpcType != npc.type || now - tracker.LastHitTick > EngagementGapTicks);

            if (!hasTracker || trackerIsStale)
            {
                if (Trackers.Count >= MaxTrackers)
                {
                    Trackers.Clear();
                }

                tracker = new KillTracker { NpcType = npc.type, FirstHitTick = now };
                Trackers[npc.whoAmI] = tracker;
            }

            tracker.LastHitTick = now;
            tracker.Hits++;
            tracker.Damage += damageDone;
            tracker.MaxHit = Math.Max(tracker.MaxHit, damageDone);

            int weaponKey = Math.Max(0, itemType);

            if (!tracker.Weapons.TryGetValue(weaponKey, out WeaponTally tally))
            {
                tally = new WeaponTally();
                tracker.Weapons[weaponKey] = tally;
            }

            tally.Damage += damageDone;
            tally.Hits++;
        }

        /// <summary>The enemy died. Counts the kill, and writes a sample when it is the 1st, 5th, 10th or 20th at this stage.</summary>
        internal static void NotifyKilled(NPC npc)
        {
            if (!Trackers.TryGetValue(npc.whoAmI, out KillTracker tracker))
            {
                return;
            }

            Trackers.Remove(npc.whoAmI);

            if (tracker.NpcType != npc.type || !BalanceLog.Active)
            {
                return;
            }

            try
            {
                Player player = Main.LocalPlayer;
                int ticks = Math.Max(1, (int)Main.GameUpdateCount - tracker.FirstHitTick);
                string stageCode = StageCodes[EnemySpawns.CurrentStageIndex()];
                string enemyName = npc.ModNPC?.Name ?? npc.TypeName;
                string statKey = enemyName + "|" + stageCode;

                EnemyKillStatsPlayer statsPlayer = player.GetModPlayer<EnemyKillStatsPlayer>();

                if (!statsPlayer.Stats.TryGetValue(statKey, out KillStat stat))
                {
                    stat = new KillStat();
                    statsPlayer.Stats[statKey] = stat;
                }

                stat.Count++;
                stat.TotalTicks += ticks;

                float seconds = ticks / 60f;
                int bucket = 0;

                while (bucket < BucketEdgesSeconds.Length && seconds >= BucketEdgesSeconds[bucket])
                {
                    bucket++;
                }

                stat.Buckets[bucket]++;

                if (Array.IndexOf(SampledKillNumbers, stat.Count) >= 0)
                {
                    WriteSample(npc, player, tracker, enemyName, stageCode, stat.Count, ticks);
                }
            }
            catch
            {
                // Telemetry must never take the game down with it.
            }
        }

        private static void WriteSample(NPC npc, Player player, KillTracker tracker, string enemyName, string stageCode, int killNumber, int ticks)
        {
            GearSnapshot gear = BalanceLog.CaptureGear(player);
            string characterTag = BalanceLog.CharacterTag;
            string session = BalanceLog.SessionId;
            string path = BalanceLog.PrepareCharacterFile(characterTag, BalanceLog.ModVersion(), Main.GameMode, gear.soulsMode);

            BalanceLog.WriteLoadoutLine(path, gear, characterTag, session, out string loadoutId);

            // Damage by class, and the weapons that did the most of it. The item's own name and class come from the
            // content sample so nothing here allocates an item per kill.
            var damageByClass = new Dictionary<string, long>();
            var weaponRows = new List<(string Name, DamageClass Class, long Damage, int Hits)>();

            foreach (KeyValuePair<int, WeaponTally> pair in tracker.Weapons)
            {
                string itemName = "other";
                DamageClass itemClass = DamageClass.Generic;

                if (pair.Key > 0 && ContentSamples.ItemsByType.TryGetValue(pair.Key, out Item sample))
                {
                    itemName = sample.ModItem?.Name ?? sample.Name;
                    itemClass = sample.DamageType ?? DamageClass.Generic;
                }

                string className = itemClass.Name.Replace("DamageClass", string.Empty);
                damageByClass.TryGetValue(className, out long classDamage);
                damageByClass[className] = classDamage + pair.Value.Damage;
                weaponRows.Add((itemName, itemClass, pair.Value.Damage, pair.Value.Hits));
            }

            weaponRows = weaponRows.OrderByDescending(row => row.Damage).ToList();

            DamageClass primaryClass = DamageClass.Generic;
            string primaryClassName = "other";

            if (weaponRows.Count > 0)
            {
                primaryClass = weaponRows[0].Class;
                primaryClassName = primaryClass.Name.Replace("DamageClass", string.Empty);
            }

            var sampleWeapons = new List<KillWeapon>();

            foreach (var row in weaponRows.Take(WeaponsPerSample))
            {
                sampleWeapons.Add(new KillWeapon
                {
                    item = row.Name,
                    damageClass = row.Class.Name.Replace("DamageClass", string.Empty),
                    damage = row.Damage,
                    hits = row.Hits,
                });
            }

            var killSample = new KillSample
            {
                session = session,
                characterTag = characterTag,
                modVersion = BalanceLog.ModVersion(),
                contentFingerprint = BalanceLog.ContentFingerprint,
                enemy = enemyName,
                enemyType = npc.type,
                stage = stageCode,
                killNumber = killNumber,
                gameMode = Main.GameMode,
                newEnemyBalance = EnemyBalance.Enabled,
                remixMap = tsorcRevampWorld.RemixMap,
                shmScale = tsorcRevampWorld.SHMScale,
                soulsMode = gear.soulsMode,
                loadoutId = loadoutId,
                playerMaxLife = gear.maxLife,
                playerDefense = gear.defense,
                damageMult = player.GetTotalDamage(primaryClass).ApplyTo(1f),
                lifeMax = npc.lifeMax,
                defense = npc.defense,
                ttkTicks = ticks,
                ttkSeconds = ticks / 60f,
                hits = tracker.Hits,
                damage = tracker.Damage,
                maxHit = tracker.MaxHit,
                primaryClass = primaryClassName,
                damageByClass = damageByClass,
                weapons = sampleWeapons,
            };

            BalanceLog.AppendLine(path, killSample);
        }

        /// <summary>Drops trackers for enemies that despawned or whose slot was reused, so a long session cannot accumulate them.</summary>
        internal static void Update()
        {
            if (Trackers.Count == 0 || Main.GameUpdateCount < _nextPruneTick)
            {
                return;
            }

            _nextPruneTick = Main.GameUpdateCount + PruneIntervalTicks;

            var stale = new List<int>();

            foreach (KeyValuePair<int, KillTracker> pair in Trackers)
            {
                NPC npc = Main.npc[pair.Key];

                if (npc == null || !npc.active || npc.type != pair.Value.NpcType)
                {
                    stale.Add(pair.Key);
                }
            }

            foreach (int slot in stale)
            {
                Trackers.Remove(slot);
            }
        }

        internal static void Reset()
        {
            Trackers.Clear();
        }

        /// <summary>
        /// The character's whole kill table as JSON: every enemy and stage with its kill count, mean time to kill and the
        /// histogram of kill times. Written into the zip by /balancelog zip - it is not appended to the log file, so it
        /// never grows the file.
        /// </summary>
        internal static string BuildSummaryJson(Player player)
        {
            EnemyKillStatsPlayer statsPlayer = player.GetModPlayer<EnemyKillStatsPlayer>();

            var enemies = statsPlayer.Stats
                .OrderBy(pair => pair.Key)
                .Select(pair =>
                {
                    string[] parts = pair.Key.Split('|');
                    double meanSeconds = pair.Value.TotalTicks / 60.0 / Math.Max(1, pair.Value.Count);

                    return new
                    {
                        enemy = parts[0],
                        stage = parts.Length > 1 ? parts[1] : string.Empty,
                        kills = pair.Value.Count,
                        meanTtkSeconds = Math.Round(meanSeconds, 2),
                        buckets = pair.Value.Buckets,
                    };
                })
                .ToList();

            return JsonConvert.SerializeObject(new
            {
                characterTag = BalanceLog.CharacterTag,
                note = "buckets[i] counts kills faster than bucketEdgesSeconds[i]; the last bucket counts kills at or over the final edge.",
                bucketEdgesSeconds = BucketEdgesSeconds,
                enemies,
            }, Formatting.Indented);
        }
    }
}

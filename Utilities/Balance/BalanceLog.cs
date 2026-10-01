using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;
using Terraria.ModLoader.Default;
using tsorcRevamp.Systems;

namespace tsorcRevamp.Utilities.Balance
{
    /// <summary>
    /// Boss-encounter balance telemetry: one self-contained JSON record per boss fight, appended to a
    /// single long-lived file so a player can hand over one attachment after weeks of play.
    ///
    /// Deliberately NOT the same thing as <see cref="NPCs.Puppets.PuppetAttackTelemetry"/> — that one is
    /// per-frame AI debugging that rolls a new file per session. This one is low-volume balance data
    /// (~2-4 KB per boss kill) meant to survive across sessions and be shared.
    ///
    /// Single-player only for now: in multiplayer, damage resolves across client/server boundaries and
    /// per-weapon attribution silently stops being trustworthy, which is worse than having no data.
    /// </summary>
    internal static class BalanceLog
    {
        internal const string FileName = "tsorcRevamp-balance.jsonl";

        /// <summary>Runtime master switch. Default on — the file is local, tiny, and only leaves the
        /// machine if the player uploads it themselves.</summary>
        internal static bool Enabled = true;

        /// <summary>Roll the file before it can ever approach a Discord attachment limit.</summary>
        private const long MaxFileBytes = 8L * 1024L * 1024L;

        /// <summary>Bump when a field's meaning changes, so analysis can split data taken before and after.
        /// 2 = group encounters (boss bag / participant kill detection), per-attempt and engagement fields.
        /// 3 = per-projectile damage split, automatic loadout description, and bench normalizedDps now divides by
        /// total (class + generic) damage; the old class-only figure is classOnlyNormalizedDps.</summary>
        internal const int LoggerRevision = 3;

        private const int SampleIntervalTicks = 60;
        private const int MaxEncounterTicks = 60 * 60 * 30;   // 30 minutes, runaway guard

        // A boss with no living participant for this long has really left (despawned or died). Shorter gaps are
        // phase changes and spawn handoffs - the Twins becoming Cataluminance, a worm's next segment - and the
        // fight carries on as one encounter.
        private const int ParticipantGraceTicks = 60 * 5;

        // After an encounter ends, hits on its former participants cannot open a new one for this long. Without
        // it a lingering projectile or a worm segment still wriggling after the kill logs a junk encounter.
        private const int EchoSuppressionTicks = 60 * 5;

        // Kill-time target the fights are designed around. A kill under 60% of the minimum is flagged as bypassed.
        private const int TargetTtkMinSeconds = 120;
        private const int TargetTtkMaxSeconds = 180;
        private const float BypassedFightFraction = 0.6f;

        private const int MaxHitsTakenKept = 300;
        private const int TopHitsTakenReported = 15;

        // Items that are not on the progression curve. Fights using one are flagged, not dropped.
        private const int OutlierBaseDamage = 10000;
        private static readonly HashSet<string> OutlierItemNames = new() { "DivineBoomCannon" };

        private static BalanceEncounter _current;
        private static readonly Dictionary<int, WeaponUsage> Weapons = new();
        private static readonly Dictionary<int, NpcDamage> NpcDamageByType = new();

        // Every NPC that is part of the current boss fight, keyed by slot with the type kept to detect a recycled slot.
        private static readonly Dictionary<int, int> Participants = new();
        private static readonly Dictionary<int, int> RecentlyEndedParticipants = new();
        private static long _lastEndTick = -1;
        private static long _emptySinceTick = -1;
        private static bool _participantDied;
        private static bool _bagDropped;
        private static int _damageThisSecond;
        private static long _lastBossLifeSum;
        private static long _lowestBossLifeSum;

        private static readonly List<HitTaken> HitsTaken = new();
        private static readonly Dictionary<string, DamageSourceUsage> DamageSources = new();
        private static readonly Dictionary<string, HealingUsage> HealingSources = new();
        private static readonly Dictionary<string, AttackUsage> AttackStats = new();
        private static readonly Dictionary<int, (string Attack, int StartTick)> OpenAttackByNpc = new();

        // Attempt bookkeeping, per boss type for the life of the game session.
        private static readonly Dictionary<int, int> AttemptCountByBoss = new();
        private static readonly Dictionary<int, string> FightGroupByBoss = new();
        private static readonly HashSet<int> WeaponsUsedThisSession = new();

        private static string _sessionId;
        private static long _startTick;
        private static long _nextSampleTick;
        private static bool _lastPlayerDead;
        private static string _startGearFingerprint;
        private static int _lastCeruleanCharges = -1;
        private static readonly HashSet<int> LiveProjectileItems = new();

        private static string _contentFingerprint;

        /// <summary>
        /// Hash of every weapon's and boss's balance numbers, computed once on first use from ContentSamples
        /// (no SetDefaults calls, so it is cheap). See <see cref="BalanceEncounter.contentFingerprint"/>.
        /// </summary>
        internal static string ContentFingerprint
        {
            get
            {
                if (_contentFingerprint != null)
                {
                    return _contentFingerprint;
                }

                var builder = new StringBuilder();

                foreach (KeyValuePair<int, Item> entry in ContentSamples.ItemsByType.OrderBy(pair => pair.Key))
                {
                    Item sample = entry.Value;
                    if (sample.damage > 0 && sample.ammo == 0)
                    {
                        builder.Append($"i{entry.Key}:{sample.damage}:{sample.useTime}:{sample.mana}:{sample.crit};");
                    }
                }

                foreach (KeyValuePair<int, NPC> entry in ContentSamples.NpcsByNetId.OrderBy(pair => pair.Key))
                {
                    NPC sample = entry.Value;
                    if (sample.boss)
                    {
                        builder.Append($"n{entry.Key}:{sample.lifeMax}:{sample.defense}:{sample.damage};");
                    }
                }

                byte[] hash = SHA1.HashData(Encoding.UTF8.GetBytes(builder.ToString()));
                _contentFingerprint = Convert.ToHexString(hash, 0, 6);
                return _contentFingerprint;
            }
        }

        internal static bool EncounterActive => _current != null;
        internal static string ActiveBossName => _current?.boss;
        internal static int EncountersThisSession { get; private set; }
        internal static string LogPath => Path.Combine(Main.SavePath, "Logs", FileName);

        internal static string SessionId => _sessionId ??=
            DateTimeOffset.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);

        // ---------------------------------------------------------------- capture

        /// <summary>True when telemetry should be recording at all. Kept in one place so every hook
        /// agrees on the gate.</summary>
        private static bool Active =>
            Enabled && !Main.dedServ && Main.netMode == NetmodeID.SinglePlayer && Main.gameMenu == false;

        internal static bool IsBossAnchor(NPC npc) =>
            npc.boss || NPCID.Sets.ShouldBeCountedAsBoss[npc.type];

        /// <summary>Display name for a projectile type for the logs; type -1 is a melee swing, which has no projectile.</summary>
        internal static string ProjectileName(int projectileType)
        {
            if (projectileType < 0)
            {
                return "melee";
            }

            return ProjectileLoader.GetProjectile(projectileType)?.Name ?? Lang.GetProjectileName(projectileType).Value;
        }

        /// <param name="projectileType">The projectile that dealt the hit, or -1 for a melee swing.</param>
        internal static void RecordHit(NPC npc, Player player, int itemType, int ammoType, int damageDone, bool crit, int projectileType = -1)
        {
            if (!Active || damageDone <= 0 || npc == null || player == null || player.whoAmI != Main.myPlayer)
                return;

            // A boss part is anything boss-flagged, or a linked body segment of a part already in the fight
            // (the Destroyer's body is not boss-flagged but shares the head's life through realLife).
            bool isBossPart = IsBossAnchor(npc) || (npc.realLife >= 0 && Participants.ContainsKey(npc.realLife));

            if (_current == null)
            {
                // Only a boss opens an encounter. Trash-mob damage outside a boss fight is deliberately
                // not logged — it isn't what the balance pass needs and it would bloat the file.
                if (!isBossPart)
                {
                    return;
                }

                // A dead player's lingering projectiles still land hits. Opening an encounter for each one
                // produced ~90 junk zero-second records per Moon Lord death.
                if (player.dead)
                {
                    return;
                }

                bool wasJustInFight = RecentlyEndedParticipants.TryGetValue(npc.whoAmI, out int endedType)
                    && endedType == npc.type
                    && Main.GameUpdateCount - _lastEndTick < EchoSuppressionTicks;
                if (wasJustInFight)
                {
                    return;
                }

                Begin(npc, player);
            }

            bool alreadyJoined = Participants.TryGetValue(npc.whoAmI, out int joinedType) && joinedType == npc.type;
            if (isBossPart && !alreadyJoined)
            {
                Participants[npc.whoAmI] = npc.type;
                _current.participantCount++;

                // A linked body segment mirrors its head's life, so adding it would count that life N times. A head links to itself.
                if (npc.realLife < 0 || npc.realLife == npc.whoAmI)
                {
                    _current.bossMaxLifeTotal += npc.lifeMax;
                }
            }

            if (isBossPart)
            {
                _emptySinceTick = -1;
            }

            int tick = (int)(Main.GameUpdateCount - (uint)_startTick);

            NpcDamage bucket = GetNpcBucket(npc, isBossPart);
            bucket.damage += damageDone;
            bucket.hits++;

            WeaponUsage weapon = GetWeapon(itemType);
            if (weapon != null)
            {
                if (ammoType > 0)
                {
                    AddAmmo(weapon.ammo, ammoType, damageDone);
                }

                if (isBossPart)
                {
                    weapon.damageToBoss += damageDone;
                    weapon.hitsOnBoss++;

                    if (crit)
                    {
                        weapon.critsOnBoss++;
                    }

                    if (weapon.firstHitTick < 0)
                    {
                        weapon.firstHitTick = tick;
                    }

                    weapon.lastHitTick = tick;

                    // Per-attack split: one weapon can field several projectile types with very different hits.
                    ProjectileUsage projectileUsage = null;
                    foreach (ProjectileUsage existing in weapon.projectiles)
                    {
                        if (existing.type == projectileType)
                        {
                            projectileUsage = existing;
                            break;
                        }
                    }

                    if (projectileUsage == null)
                    {
                        projectileUsage = new ProjectileUsage { type = projectileType, name = ProjectileName(projectileType) };
                        weapon.projectiles.Add(projectileUsage);
                    }

                    projectileUsage.hits++;
                    projectileUsage.damage += damageDone;

                    if (crit)
                    {
                        projectileUsage.crits++;
                    }
                }
                else
                {
                    weapon.damageToOthers += damageDone;
                }
            }

            if (isBossPart)
            {
                _current.totalDamageToBoss += damageDone;
                _damageThisSecond += damageDone;
            }
            else
            {
                _current.totalDamageToOthers += damageDone;
            }
        }

        /// <summary>Logs one hit the player took: applied vs raw damage, and who dealt it. Keeps every hit up to a cap
        /// (a handful per fight in practice) so the largest can be reported at the end.</summary>
        internal static void RecordDamageTaken(Player.HurtInfo info)
        {
            if (_current == null || info.Damage <= 0)
            {
                return;
            }

            string sourceName = "other";
            NPC sourceNpc = null;

            if (info.DamageSource.SourceNPCIndex >= 0 && info.DamageSource.SourceNPCIndex < Main.maxNPCs)
            {
                sourceNpc = Main.npc[info.DamageSource.SourceNPCIndex];
            }

            if (sourceNpc != null && sourceNpc.active)
            {
                sourceName = "npc:" + (sourceNpc.ModNPC?.Name ?? sourceNpc.TypeName);
            }
            else if (info.DamageSource.SourceProjectileType > 0)
            {
                sourceName = "proj:" + ProjectileName(info.DamageSource.SourceProjectileType);
            }

            _current.damageTaken += info.Damage;
            _current.damageTakenRaw += info.SourceDamage;
            _current.hitsTaken++;
            _current.largestHitTaken = Math.Max(_current.largestHitTaken, info.Damage);

            if (!DamageSources.TryGetValue(sourceName, out DamageSourceUsage usage))
            {
                usage = new DamageSourceUsage { source = sourceName };
                DamageSources[sourceName] = usage;
            }

            usage.hits++;
            usage.damage += info.Damage;
            usage.rawDamage += info.SourceDamage;
            usage.largestHit = Math.Max(usage.largestHit, info.Damage);

            if (HitsTaken.Count < MaxHitsTakenKept)
            {
                HitsTaken.Add(new HitTaken
                {
                    tick = (int)(Main.GameUpdateCount - (uint)_startTick),
                    damage = info.Damage,
                    rawDamage = info.SourceDamage,
                    source = sourceName,
                });
            }
        }

        /// <summary>Logs one use of a healing source (Estus, a potion). Silent outside a boss fight.</summary>
        internal static void RecordHealing(string source, int healed)
        {
            if (_current == null || healed <= 0)
            {
                return;
            }

            if (!HealingSources.TryGetValue(source, out HealingUsage usage))
            {
                usage = new HealingUsage { source = source };
                HealingSources[source] = usage;
            }

            usage.uses++;
            usage.healed += healed;
        }

        /// <summary>Credits mana a weapon actually consumed. Read from the OnConsumeMana hook rather than by
        /// sampling statMana, which regeneration masks entirely once the pool is large.</summary>
        internal static void RecordManaSpent(int itemType, int manaConsumed)
        {
            if (_current == null || manaConsumed <= 0)
            {
                return;
            }

            WeaponUsage weapon = GetWeapon(itemType);
            if (weapon != null)
            {
                weapon.manaSpent += manaConsumed;
            }
        }

        /// <summary>A puppet boss entered an attack state. Counts entries and time-in-state per attack name, so a
        /// 30-second kill can be told apart from a 2-minute one by how many of the boss's attacks were seen.</summary>
        internal static void NotifyBossAttack(NPC npc, string attackName)
        {
            if (_current == null)
            {
                return;
            }

            bool isParticipant = Participants.TryGetValue(npc.whoAmI, out int participantType) && participantType == npc.type;
            if (!isParticipant)
            {
                return;
            }

            int tick = (int)(Main.GameUpdateCount - (uint)_startTick);

            if (OpenAttackByNpc.TryGetValue(npc.whoAmI, out (string Attack, int StartTick) previous))
            {
                AttackStats[previous.Attack].ticks += tick - previous.StartTick;
            }

            if (!AttackStats.TryGetValue(attackName, out AttackUsage usage))
            {
                usage = new AttackUsage { attack = attackName };
                AttackStats[attackName] = usage;
            }

            usage.entries++;
            OpenAttackByNpc[npc.whoAmI] = (attackName, tick);
        }

        /// <summary>A boss bag spawned from loot. That only ever happens on a real kill, so it ends the encounter as
        /// one - including bosses that hand off to a final form and never fire OnKill on the anchor.</summary>
        internal static void NotifyBossBag()
        {
            if (_current == null)
            {
                return;
            }

            _bagDropped = true;
            End("kill");
        }

        /// <summary>Logs the iframe duration one hit actually granted (already includes iframe-extending
        /// effects like Cross Necklace's longInvince) - see BalanceLogPlayer.PostUpdate for why this is
        /// captured a tick after OnHurt rather than during it. Feeds the question of whether longer
        /// iframes correlate with a higher Regain healed/damageTaken ratio.</summary>
        internal static void RecordHurtImmuneTime(int ticks)
        {
            if (_current != null && ticks > 0)
                _current.hurtImmuneTimes.Add(ticks);
        }

        /// <summary>Logs one successful Regain credit against the current boss encounter, if one is
        /// open. Deliberately silent outside a boss fight - see the comment in RecordHit on why
        /// trash-mob activity is not logged at all.</summary>
        internal static void RecordRegainCredit(int itemType, int healed, float distanceTiles)
        {
            if (!Active || _current == null || healed <= 0)
                return;

            WeaponUsage weapon = GetWeapon(itemType);
            if (weapon == null)
                return;

            weapon.regainHealed += healed;
            weapon.regainCredits++;
            weapon.regainDistanceSumTiles += distanceTiles;
            _current.totalRegainHealed += healed;
        }

        /// <summary>Logs one credit the swing-window gate refused. A weapon racking these up fast
        /// relative to regainCredits is trying to credit multiple times off a single activation -
        /// exactly the shape a multishot/piercing/beam exploit takes.</summary>
        internal static void RecordRegainGateRefusal(int itemType)
        {
            if (!Active || _current == null)
                return;

            WeaponUsage weapon = GetWeapon(itemType);
            if (weapon != null)
                weapon.regainGateRefusals++;
        }

        /// <summary>A participant died. This does not end the encounter - another part may be alive, or the boss may
        /// hand off to a final form - it only records that the fight produced a kill, so that once every part is
        /// gone the outcome reads kill and not despawn.</summary>
        internal static void NotifyKilled(NPC npc)
        {
            if (_current == null)
            {
                return;
            }

            bool isParticipant = Participants.TryGetValue(npc.whoAmI, out int participantType) && participantType == npc.type;
            if (isParticipant)
            {
                _participantDied = true;
            }
        }

        /// <summary>Per-tick bookkeeping: weapon uptime, 1 Hz life sampling, and end-condition checks.</summary>
        internal static void Update()
        {
            if (_current == null)
                return;

            if (!Active)
            {
                End("abandoned");
                return;
            }

            Player player = Main.LocalPlayer;

            // Held-item uptime, so per-weapon DPS can be computed over the window the weapon was
            // actually in hand rather than over the whole fight.
            Item held = player.HeldItem;
            if (held != null && !held.IsAir && held.damage > 0)
            {
                WeaponUsage weapon = GetWeapon(held.type);
                if (weapon != null)
                    weapon.heldTicks++;
            }

            // Projectile uptime. Summons and sentries do all their damage while a completely
            // different item is held, so heldTicks alone would report them as infinite DPS over a
            // near-zero window. This is the window that actually applies to them.
            AccumulateProjectileUptime(player);

            int charges = CeruleanCharges(player);
            if (charges >= 0 && _lastCeruleanCharges >= 0 && charges < _lastCeruleanCharges)
                _current.ceruleanChargesUsed += _lastCeruleanCharges - charges;
            _lastCeruleanCharges = charges;

            if (player.dead && !_lastPlayerDead)
                _current.playerDeaths++;
            _lastPlayerDead = player.dead;

            // Mana-starved ticks: the held weapon costs more than the player has left, so the mana economy is gating it.
            if (held != null && !held.IsAir && held.mana > 0 && player.statMana < held.mana)
            {
                WeaponUsage manaGatedWeapon = GetWeapon(held.type);
                if (manaGatedWeapon != null)
                {
                    manaGatedWeapon.manaStarvedTicks++;
                }
            }

            // Every living participant's life, summed. A linked segment mirrors its head's life, so it is skipped.
            int aliveCount = 0;
            long lifeSum = 0;

            foreach (KeyValuePair<int, int> pair in Participants)
            {
                NPC participant = Main.npc[pair.Key];
                bool alive = participant != null && participant.active && participant.type == pair.Value;
                if (!alive)
                {
                    continue;
                }

                aliveCount++;

                if (participant.realLife < 0 || participant.realLife == participant.whoAmI)
                {
                    lifeSum += Math.Max(0, participant.life);
                }
            }

            _lastBossLifeSum = lifeSum;
            _lowestBossLifeSum = Math.Min(_lowestBossLifeSum, lifeSum);

            if (Main.GameUpdateCount >= (uint)_nextSampleTick)
            {
                _nextSampleTick = Main.GameUpdateCount + SampleIntervalTicks;
                _current.bossLifeTimeline.Add((int)Math.Min(lifeSum, int.MaxValue));
                _current.playerLifeTimeline.Add(Math.Max(0, player.statLife));
                _current.manaTimeline.Add(Math.Max(0, player.statMana));
                _current.damageTimeline.Add(_damageThisSecond);
                _damageThisSecond = 0;
            }

            if (player.dead)
            {
                End("player_death");
                return;
            }

            // No participant alive: wait out the grace window before calling it. A handoff to another boss
            // form re-fills Participants through RecordHit and clears the timer.
            if (aliveCount == 0)
            {
                if (_emptySinceTick < 0)
                {
                    _emptySinceTick = Main.GameUpdateCount;
                }

                if (Main.GameUpdateCount - _emptySinceTick >= ParticipantGraceTicks)
                {
                    string finishedOutcome = "despawn";
                    if (_participantDied || _bagDropped)
                    {
                        finishedOutcome = "kill";
                    }

                    End(finishedOutcome, _emptySinceTick);
                    return;
                }
            }

            if (Main.GameUpdateCount - (uint)_startTick > MaxEncounterTicks)
            {
                End("abandoned");
            }
        }

        internal static void AbortForWorldChange() => End("abandoned");

        // ---------------------------------------------------------------- lifecycle

        private static void Begin(NPC boss, Player player)
        {
            _startTick = Main.GameUpdateCount;
            _nextSampleTick = Main.GameUpdateCount;
            _lastPlayerDead = player.dead;
            _lastCeruleanCharges = CeruleanCharges(player);
            _emptySinceTick = -1;
            _participantDied = false;
            _bagDropped = false;
            _damageThisSecond = 0;
            _lastBossLifeSum = boss.life;
            _lowestBossLifeSum = boss.life;

            Weapons.Clear();
            NpcDamageByType.Clear();
            Participants.Clear();
            HitsTaken.Clear();
            DamageSources.Clear();
            HealingSources.Clear();
            AttackStats.Clear();
            OpenAttackByNpc.Clear();

            // The ContentSamples copy is the NPC as defined, before difficulty, SHM and world scaling, so the live
            // lifeMax over it is the total multiplier applied to this boss whatever applied it.
            int baseLifeMax = boss.lifeMax;
            int baseContactDamage = boss.damage;
            if (ContentSamples.NpcsByNetId.TryGetValue(boss.netID, out NPC sample))
            {
                baseLifeMax = sample.lifeMax;
                baseContactDamage = sample.damage;
            }

            float lifeMaxMultVsBase = 1f;
            if (baseLifeMax > 0)
            {
                lifeMaxMultVsBase = boss.lifeMax / (float)baseLifeMax;
            }

            var downedBossTypes = new List<int>();
            if (tsorcRevampWorld.NewSlain != null)
            {
                foreach (NPCDefinition slain in tsorcRevampWorld.NewSlain.Keys)
                {
                    downedBossTypes.Add(slain.Type);
                }
            }

            _current = new BalanceEncounter
            {
                session = SessionId,
                modVersion = ModVersion(),
                contentFingerprint = ContentFingerprint,
                startedAt = DateTimeOffset.Now.ToString("O", CultureInfo.InvariantCulture),
                gameMode = Main.GameMode,
                playerCount = 1,
                superHardMode = tsorcRevampWorld.SuperHardMode,
                remixMap = tsorcRevampWorld.RemixMap,
                customMap = tsorcRevampWorld.CustomMap,
                shmDowned = SafeShmDowned(),
                shmScale = tsorcRevampWorld.SHMScale,
                subtleShmScale = tsorcRevampWorld.SubtleSHMScale,
                gameModeLifeMult = Main.GameModeInfo.EnemyMaxLifeMultiplier,
                enemyDamageMult = Main.GameModeInfo.EnemyDamageMultiplier,
                downedBossCount = downedBossTypes.Count,
                downedBossTypes = downedBossTypes,
                bossType = boss.type,
                boss = boss.ModNPC?.Name ?? boss.TypeName,
                bossMod = boss.ModNPC?.Mod?.Name ?? "Terraria",
                bossMaxLife = boss.lifeMax,
                bossDefense = boss.defense,
                baseLifeMax = baseLifeMax,
                lifeMaxMultVsBase = lifeMaxMultVsBase,
                bossContactDamage = boss.damage,
                baseContactDamage = baseContactDamage,
                targetTtkMinSeconds = TargetTtkMinSeconds,
                targetTtkMaxSeconds = TargetTtkMaxSeconds,
                gear = CaptureGear(player),
            };

            _startGearFingerprint = GearFingerprint(player);
        }

        /// <param name="endTick">Tick the fight really ended, for endings detected late (the grace window). -1 means now.</param>
        private static void End(string outcome, long endTick = -1)
        {
            if (_current == null)
            {
                return;
            }

            BalanceEncounter encounter = _current;
            _current = null;   // cleared first so a throw below can never wedge the tracker on

            RecentlyEndedParticipants.Clear();
            foreach (KeyValuePair<int, int> pair in Participants)
            {
                RecentlyEndedParticipants[pair.Key] = pair.Value;
            }

            _lastEndTick = Main.GameUpdateCount;

            try
            {
                // A world change or a runaway guard that lands after the boss already died is still a kill.
                string finalOutcome = outcome;
                if (outcome == "abandoned" && (_participantDied || _bagDropped))
                {
                    finalOutcome = "kill";
                }

                if (endTick < 0)
                {
                    endTick = Main.GameUpdateCount;
                }

                int ticks = (int)(endTick - _startTick);
                encounter.outcome = finalOutcome;
                encounter.durationTicks = ticks;
                encounter.durationSeconds = ticks / 60f;
                encounter.bagDropped = _bagDropped;

                // An ending found late (the grace window) leaves a few seconds of samples after the fight really
                // stopped. Trim them so the timelines and the engagement numbers describe the fight only.
                int maxSamples = ticks / SampleIntervalTicks + 1;
                List<int>[] timelines = { encounter.bossLifeTimeline, encounter.playerLifeTimeline, encounter.manaTimeline, encounter.damageTimeline };
                foreach (List<int> timeline in timelines)
                {
                    if (timeline.Count > maxSamples)
                    {
                        timeline.RemoveRange(maxSamples, timeline.Count - maxSamples);
                    }
                }

                if (encounter.durationSeconds > 0.5f)
                {
                    encounter.bossDps = encounter.totalDamageToBoss / encounter.durationSeconds;
                }

                encounter.gearChangedMidFight =
                    Main.LocalPlayer != null && GearFingerprint(Main.LocalPlayer) != _startGearFingerprint;

                encounter.totalDamageToAllNpcs = 0;
                foreach (NpcDamage npcDamage in NpcDamageByType.Values)
                {
                    encounter.totalDamageToAllNpcs += npcDamage.damage;
                }

                // A death that dealt almost nothing in a couple of seconds is not an attempt worth recording.
                bool trivialAttempt = finalOutcome != "kill"
                    && encounter.durationSeconds < 3f
                    && encounter.totalDamageToBoss < 0.02f * Math.Max(1L, encounter.bossMaxLifeTotal);
                if (trivialAttempt)
                {
                    return;
                }

                // Snapshot what the output-based stamina system learned about each weapon, taken here at the
                // end of the fight so the EMA is as converged as it's going to get for this encounter.
                CaptureStaminaBeliefs(Main.LocalPlayer);

                // Close the attack state each puppet was still in when the fight stopped.
                foreach (KeyValuePair<int, (string Attack, int StartTick)> open in OpenAttackByNpc)
                {
                    AttackStats[open.Value.Attack].ticks += ticks - open.Value.StartTick;
                }

                // Attempts: how many tries at this boss since its last kill, linked by one group id.
                AttemptCountByBoss.TryGetValue(encounter.bossType, out int attemptsSoFar);
                attemptsSoFar++;
                AttemptCountByBoss[encounter.bossType] = attemptsSoFar;
                encounter.attemptIndex = attemptsSoFar;

                if (!FightGroupByBoss.TryGetValue(encounter.bossType, out string fightGroupId))
                {
                    fightGroupId = $"{SessionId}-{encounter.bossType}-{EncountersThisSession}";
                    FightGroupByBoss[encounter.bossType] = fightGroupId;
                }

                encounter.fightGroupId = fightGroupId;

                if (finalOutcome == "kill")
                {
                    AttemptCountByBoss.Remove(encounter.bossType);
                    FightGroupByBoss.Remove(encounter.bossType);
                }

                // Boss life left, as a fraction of everything that joined the fight.
                float totalLife = Math.Max(1L, encounter.bossMaxLifeTotal);
                encounter.bossLifeFractionAtEnd = _lastBossLifeSum / totalLife;
                encounter.lowestBossLifeFraction = _lowestBossLifeSum / totalLife;
                if (finalOutcome == "kill")
                {
                    encounter.bossLifeFractionAtEnd = 0f;
                    encounter.lowestBossLifeFraction = 0f;
                }

                // Flush the partial second still sitting in the accumulator.
                if (_damageThisSecond > 0)
                {
                    encounter.damageTimeline.Add(_damageThisSecond);
                    _damageThisSecond = 0;
                }

                // Engagement: seconds with a hit in them, the longest dry spell between hits, and the best 10 s burst.
                int engagedSeconds = 0;
                int firstHitSecond = -1;
                int lastHitSecond = -1;
                for (int second = 0; second < encounter.damageTimeline.Count; second++)
                {
                    if (encounter.damageTimeline[second] <= 0)
                    {
                        continue;
                    }

                    engagedSeconds++;

                    if (firstHitSecond < 0)
                    {
                        firstHitSecond = second;
                    }

                    lastHitSecond = second;
                }

                int longestGap = 0;
                int currentGap = 0;
                for (int second = firstHitSecond; second >= 0 && second <= lastHitSecond; second++)
                {
                    if (encounter.damageTimeline[second] > 0)
                    {
                        currentGap = 0;
                    }
                    else
                    {
                        currentGap++;
                        longestGap = Math.Max(longestGap, currentGap);
                    }
                }

                long burstWindowDamage = 0;
                long bestBurstDamage = 0;
                for (int second = 0; second < encounter.damageTimeline.Count; second++)
                {
                    burstWindowDamage += encounter.damageTimeline[second];
                    if (second >= 10)
                    {
                        burstWindowDamage -= encounter.damageTimeline[second - 10];
                    }

                    bestBurstDamage = Math.Max(bestBurstDamage, burstWindowDamage);
                }

                encounter.longestNoDamageGapSec = longestGap;
                encounter.burst10sDps = bestBurstDamage / 10f;

                if (engagedSeconds > 0)
                {
                    encounter.engagedDps = encounter.totalDamageToBoss / (float)engagedSeconds;
                    encounter.uptimeFraction = engagedSeconds / (float)encounter.damageTimeline.Count;
                }

                // Time-to-kill against the design target. Only meaningful for a finished fight.
                if (finalOutcome == "kill" && encounter.durationSeconds > 0f)
                {
                    float targetMidpoint = (TargetTtkMinSeconds + TargetTtkMaxSeconds) * 0.5f;
                    encounter.ttkRatio = encounter.durationSeconds / targetMidpoint;
                    encounter.bypassedFight = encounter.durationSeconds < TargetTtkMinSeconds * BypassedFightFraction;
                    encounter.hpForTargetAtObservedDps = (long)(encounter.bossDps * targetMidpoint);
                }

                // Per-weapon derived numbers, the build summary, and the outlier / new-weapon flags.
                foreach (WeaponUsage weapon in Weapons.Values)
                {
                    if (weapon.hitsOnBoss > 0)
                    {
                        weapon.damagePerHitMean = weapon.damageToBoss / (float)weapon.hitsOnBoss;
                    }

                    if (weapon.heldTicks > 0)
                    {
                        float heldSeconds = weapon.heldTicks / 60f;
                        weapon.hitsPerHeldSec = weapon.hitsOnBoss / heldSeconds;
                        weapon.damagePerHeldSec = weapon.damageToBoss / heldSeconds;
                    }

                    if (weapon.activeTicks > 0)
                    {
                        weapon.damagePerActiveSec = weapon.damageToBoss / (weapon.activeTicks / 60f);
                    }

                    if (weapon.damageToBoss > 0)
                    {
                        weapon.firstUseThisSession = WeaponsUsedThisSession.Add(weapon.itemType);

                        encounter.damageByClass.TryGetValue(weapon.damageClass, out long classDamage);
                        encounter.damageByClass[weapon.damageClass] = classDamage + weapon.damageToBoss;

                        if (weapon.outlier)
                        {
                            encounter.outlierWeapons.Add(weapon.item);
                        }
                    }
                }

                encounter.usedOutlierWeapon = encounter.outlierWeapons.Count > 0;

                long primaryClassDamage = -1;
                foreach (KeyValuePair<string, long> classEntry in encounter.damageByClass)
                {
                    if (classEntry.Value > primaryClassDamage)
                    {
                        primaryClassDamage = classEntry.Value;
                        encounter.primaryClass = classEntry.Key;
                    }
                }

                // Damage taken: biggest hits first, plus how the largest compares with what the player could survive.
                HitsTaken.Sort((leftHit, rightHit) => rightHit.damage.CompareTo(leftHit.damage));
                int topHitCount = Math.Min(TopHitsTakenReported, HitsTaken.Count);
                encounter.topHitsTaken.AddRange(HitsTaken.GetRange(0, topHitCount));
                encounter.largestHitVsMaxLife = encounter.largestHitTaken / (float)Math.Max(1, encounter.gear.maxLife);
                encounter.damageTakenBySource.AddRange(DamageSources.Values);

                foreach (HealingUsage healing in HealingSources.Values)
                {
                    if (healing.source == "estus")
                    {
                        encounter.estusDrinks += healing.uses;
                        encounter.estusHealed += healing.healed;
                    }
                    else
                    {
                        encounter.potionUses += healing.uses;
                        encounter.potionHealed += healing.healed;
                    }
                }

                encounter.healing.AddRange(HealingSources.Values);
                encounter.attacks.AddRange(AttackStats.Values);

                encounter.weapons.AddRange(Weapons.Values);
                encounter.npcDamage.AddRange(NpcDamageByType.Values);

                Write(encounter);
                EncountersThisSession++;
            }
            catch
            {
                // Telemetry must never take the game down with it.
            }
            finally
            {
                Weapons.Clear();
                NpcDamageByType.Clear();
                Participants.Clear();
                HitsTaken.Clear();
                DamageSources.Clear();
                HealingSources.Clear();
                AttackStats.Clear();
                OpenAttackByNpc.Clear();
            }
        }

        /// <summary>
        /// Fill each WeaponUsage with the output-based stamina system's current belief about that weapon.
        /// Pairing "what the system thinks this weapon outputs" with "what it actually did to the boss" in the
        /// same record is what makes the log usable to validate (or retune) the stamina curve.
        /// </summary>
        private static void CaptureStaminaBeliefs(Player player)
        {
            if (player == null)
            {
                return;
            }
            try
            {
                var stamina = player.GetModPlayer<tsorcRevampStaminaPlayer>();
                foreach (WeaponUsage weapon in Weapons.Values)
                {
                    if (weapon.itemType <= 0 || weapon.itemType >= ItemLoader.ItemCount)
                    {
                        continue;
                    }
                    var probe = new Item();
                    probe.SetDefaults(weapon.itemType);
                    if (!tsorcRevampStaminaPlayer.UsesOutputBasedWeaponCost(probe))
                    {
                        continue;
                    }
                    int scaledUseAnimation = (int)(probe.useAnimation / Math.Max(0.05f, player.GetAttackSpeed(probe.DamageType)));
                    weapon.staminaDamagePerUseEma = stamina.GetWeaponDamagePerUseEma(weapon.itemType);
                    weapon.staminaCostPerUse = stamina.GetExpectedOutputBasedWeaponCost(probe, scaledUseAnimation);
                }
            }
            catch
            {
                // Telemetry must never break a fight.
            }
        }

        private static void Write(BalanceEncounter encounter) => Append(FileName, encounter);

        /// <summary>Appends one JSON record as a line, rolling the file if it ever grows large enough
        /// to become awkward to share.</summary>
        internal static void Append(string fileName, object record)
        {
            string path = Path.Combine(Main.SavePath, "Logs", fileName);
            Directory.CreateDirectory(Path.GetDirectoryName(path));

            var info = new FileInfo(path);
            if (info.Exists && info.Length > MaxFileBytes)
            {
                string stem = Path.GetFileNameWithoutExtension(fileName);
                File.Move(path, Path.Combine(
                    Path.GetDirectoryName(path),
                    $"{stem}-{DateTimeOffset.Now:yyyyMMdd-HHmmss}.jsonl"));
            }

            File.AppendAllText(path, JsonConvert.SerializeObject(record, Formatting.None) + Environment.NewLine);
        }

        // ---------------------------------------------------------------- snapshots

        /// <summary>Credits one tick of uptime to every item that currently has a projectile alive.</summary>
        private static void AccumulateProjectileUptime(Player player)
        {
            LiveProjectileItems.Clear();
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile projectile = Main.projectile[i];
                if (projectile == null || !projectile.active || projectile.owner != player.whoAmI)
                    continue;

                int sourceItem = projectile.GetGlobalProjectile<BalanceSourceProjectile>().SourceItemType;
                if (sourceItem > 0)
                    LiveProjectileItems.Add(sourceItem);
            }

            foreach (int itemType in LiveProjectileItems)
            {
                WeaponUsage weapon = GetWeapon(itemType);
                if (weapon != null)
                    weapon.activeTicks++;
            }
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

        internal static int CeruleanCharges(Player player)
        {
            try
            {
                return player.GetModPlayer<CeruleanFlaskPlayer>().CeruleanChargesCurrent;
            }
            catch
            {
                return -1;
            }
        }

        private static NpcDamage GetNpcBucket(NPC npc, bool isBossPart)
        {
            if (!NpcDamageByType.TryGetValue(npc.type, out NpcDamage bucket))
            {
                bucket = new NpcDamage
                {
                    type = npc.type,
                    name = npc.ModNPC?.Name ?? npc.TypeName,
                    isBossAnchor = isBossPart,
                };
                NpcDamageByType[npc.type] = bucket;
            }
            return bucket;
        }

        private static WeaponUsage GetWeapon(int itemType)
        {
            if (itemType <= 0 || itemType >= ItemLoader.ItemCount)
                return null;

            if (!Weapons.TryGetValue(itemType, out WeaponUsage weapon))
            {
                var probe = new Item();
                probe.SetDefaults(itemType);
                weapon = new WeaponUsage
                {
                    itemType = itemType,
                    item = probe.ModItem?.Name ?? probe.Name,
                    itemMod = probe.ModItem?.Mod?.Name ?? "Terraria",
                    baseDamage = probe.damage,
                    damageClass = probe.DamageType?.Name ?? "Default",
                    useTime = probe.useTime,
                    outlier = OutlierItemNames.Contains(probe.ModItem?.Name ?? probe.Name) || probe.damage >= OutlierBaseDamage,
                };

                // Prefer the live inventory copy's prefix — a reforge is a real slice of the DPS
                // being measured, and the freshly-defaulted probe has none.
                Item live = FindHeldOrInventory(itemType);
                if (live != null)
                {
                    weapon.prefix = live.prefix;
                    weapon.prefixName = PrefixName(live.prefix);
                }

                // What one use really deals with the player's current multipliers and this item's prefix.
                weapon.effectiveDamage = Main.LocalPlayer.GetWeaponDamage(live ?? probe);

                Weapons[itemType] = weapon;
            }
            return weapon;
        }

        private static Item FindHeldOrInventory(int itemType)
        {
            Player player = Main.LocalPlayer;
            if (player == null)
                return null;
            if (player.HeldItem != null && player.HeldItem.type == itemType)
                return player.HeldItem;
            for (int i = 0; i < player.inventory.Length; i++)
            {
                if (player.inventory[i] != null && player.inventory[i].type == itemType)
                    return player.inventory[i];
            }
            return null;
        }

        /// <summary>One equipped item flattened for the log. Shared by the armor/accessory loop and the
        /// Right-Click (2nd) slot capture so both record the same fields.</summary>
        private static EquipSlot MakeEquipSlot(Item item)
        {
            return new EquipSlot
            {
                type = item.type,
                name = item.ModItem?.Name ?? item.Name,
                mod = item.ModItem?.Mod?.Name ?? "Terraria",
                prefix = item.prefix,
                prefixName = PrefixName(item.prefix),
                defense = item.defense,
            };
        }

        internal static GearSnapshot CaptureGear(Player player)
        {
            var snapshot = new GearSnapshot
            {
                maxLife = player.statLifeMax2,
                maxMana = player.statManaMax2,
                defense = player.statDefense,
                meleeDamage = player.GetDamage(DamageClass.Melee).ApplyTo(1f),
                rangedDamage = player.GetDamage(DamageClass.Ranged).ApplyTo(1f),
                magicDamage = player.GetDamage(DamageClass.Magic).ApplyTo(1f),
                summonDamage = player.GetDamage(DamageClass.Summon).ApplyTo(1f),
                genericDamage = player.GetDamage(DamageClass.Generic).ApplyTo(1f),
                meleeCrit = player.GetCritChance(DamageClass.Melee),
                rangedCrit = player.GetCritChance(DamageClass.Ranged),
                magicCrit = player.GetCritChance(DamageClass.Magic),
                meleeSpeed = player.GetAttackSpeed(DamageClass.Melee),
                minionSlots = player.maxMinions,
            };

            CaptureSoulsMode(player, snapshot);

            // The multiplier pieces behind the floats above - see DamageModifierBreakdown for why sources aren't here.
            DamageClass[] trackedClasses = { DamageClass.Melee, DamageClass.Ranged, DamageClass.Magic, DamageClass.Summon, DamageClass.Generic };
            foreach (DamageClass trackedClass in trackedClasses)
            {
                StatModifier modifier = player.GetDamage(trackedClass);
                snapshot.damageModifiers[trackedClass.Name.Replace("DamageClass", string.Empty)] = new DamageModifierBreakdown
                {
                    additive = modifier.Additive,
                    multiplicative = modifier.Multiplicative,
                    flat = modifier.Flat,
                    baseValue = modifier.Base,
                };
            }

            // 0-2 are armor, 3-9 are the accessory slots (including the Demon Heart and Journey slots).
            // 10+ are vanity and are irrelevant to balance.
            for (int i = 0; i < player.armor.Length && i < 10; i++)
            {
                Item item = player.armor[i];
                if (item == null || item.IsAir)
                {
                    continue;
                }

                if (i < 3)
                {
                    snapshot.armor.Add(MakeEquipSlot(item));
                }
                else
                {
                    snapshot.accessories.Add(MakeEquipSlot(item));
                }
            }

            // Mod accessory slots (Bearer of the Curse slot, Supersonic wing slot) are stored by ModAccessorySlotPlayer,
            // not Player.armor, so the loop above never sees them. SlotCount is the number of functional slots.
            ModAccessorySlotPlayer modSlotPlayer = player.GetModPlayer<ModAccessorySlotPlayer>();
            var slotLoader = LoaderManager.Get<AccessorySlotLoader>();
            for (int slotIndex = 0; slotIndex < modSlotPlayer.SlotCount; slotIndex++)
            {
                ModAccessorySlot modSlot = slotLoader.Get(slotIndex, player);
                Item slotItem = modSlot?.FunctionalItem;
                if (slotItem == null || slotItem.IsAir)
                {
                    continue;
                }

                snapshot.moddedAccessories.Add(new ModdedEquipSlot
                {
                    slot = modSlot.Name,
                    item = MakeEquipSlot(slotItem),
                });
            }

            // The Active Shields Right-Click (2nd) slot, which the armor array does not cover. A shield here
            // grants its passives without costing an accessory slot, so without this the log undercounts shields
            // and can't answer whether 2nd-slot placement is simply the correct play for a shield build.
            // The slot exists for every player (built in Initialize, net-synced), so no local-player guard.
            Item secondSlotItem = player.GetModPlayer<tsorcRevampPlayer>().RightClickSlot?.Item;
            if (secondSlotItem != null && !secondSlotItem.IsAir)
            {
                snapshot.secondSlot = MakeEquipSlot(secondSlotItem);
            }

            for (int i = 0; i < player.buffType.Length; i++)
            {
                int buff = player.buffType[i];
                if (buff > 0 && player.buffTime[i] > 0)
                    snapshot.buffs.Add(BuffLoader.GetBuff(buff)?.Name ?? Lang.GetBuffName(buff));
            }

            // Automatic loadout description, read from what is on the player right now. Prefixes are counted across
            // every equipped piece, so "2 Warding, 2 Menacing, 2 Arcane" is in the record and not in a note.
            snapshot.armorPieces = snapshot.armor.Count;
            snapshot.armorSet = string.Join("/", snapshot.armor.Select(piece => piece.name));
            snapshot.accessoryCount = snapshot.accessories.Count + snapshot.moddedAccessories.Count;
            snapshot.buffCount = snapshot.buffs.Count;
            snapshot.noGearEquipped = snapshot.armorPieces == 0 && snapshot.accessoryCount == 0 && snapshot.secondSlot == null;

            var equippedPieces = new List<EquipSlot>(snapshot.armor);
            equippedPieces.AddRange(snapshot.accessories);
            equippedPieces.AddRange(snapshot.moddedAccessories.Select(moddedSlot => moddedSlot.item));
            if (snapshot.secondSlot != null)
            {
                equippedPieces.Add(snapshot.secondSlot);
            }

            foreach (EquipSlot piece in equippedPieces)
            {
                if (string.IsNullOrEmpty(piece.prefixName))
                {
                    continue;
                }

                snapshot.prefixCounts.TryGetValue(piece.prefixName, out int prefixCount);
                snapshot.prefixCounts[piece.prefixName] = prefixCount + 1;
            }

            if (snapshot.noGearEquipped)
            {
                snapshot.loadoutTag = snapshot.buffCount == 0 ? "naked" : $"naked + {snapshot.buffCount} buffs";
            }
            else
            {
                string armorLabel = snapshot.armorPieces == 0 ? "no armor" : snapshot.armorSet;
                snapshot.loadoutTag = $"{armorLabel} + {snapshot.accessoryCount} acc + {snapshot.buffCount} buffs";
            }

            return snapshot;
        }

        /// <summary>
        /// Records which Souls class is active and what it does to stamina costs.
        ///
        /// Read off the live properties rather than re-derived here, so this can't drift from the real
        /// rule in <c>tsorcRevampPlayer.WeaponStaminaMult</c> (Classic 0x, Unkindled 0.85x or 1x,
        /// Bearer of the Curse 1.15x or 1.5x depending on the Experimental Stamina Values config,
        /// all times Tired).
        /// </summary>
        private static void CaptureSoulsMode(Player player, GearSnapshot snapshot)
        {
            try
            {
                var souls = player.GetModPlayer<tsorcRevampPlayer>();
                snapshot.soulsMode = souls.BearerOfTheCurse ? "BearerOfTheCurse"
                    : souls.Unkindled ? "Unkindled"
                    : "Classic";
                snapshot.usesWeaponStamina = souls.UsesWeaponStamina;
                snapshot.playerDamagePerSecondEma = player.GetModPlayer<tsorcRevampStaminaPlayer>().PlayerDamagePerSecondEma;
                snapshot.weaponStaminaMult = souls.WeaponStaminaMult(player.HeldItem);
                snapshot.tired = souls.Tired;

                var stamina = player.GetModPlayer<tsorcRevampStaminaPlayer>();
                snapshot.staminaMax = stamina.staminaResourceMax2;
                snapshot.staminaCurrent = stamina.staminaResourceCurrent;
            }
            catch
            {
                snapshot.soulsMode = "unknown";
            }
        }

        /// <summary>Cheap equality key for "did the player swap gear mid-fight", which would otherwise
        /// silently invalidate the start-of-fight gear snapshot.</summary>
        internal static string GearFingerprint(Player player)
        {
            var parts = new List<string>();
            for (int i = 0; i < player.armor.Length && i < 10; i++)
            {
                Item item = player.armor[i];
                parts.Add(item == null || item.IsAir ? "-" : item.type + ":" + item.prefix);
            }

            // Mod accessory slots count as gear too, or swapping the Bearer of the Curse slot mid-fight goes unflagged.
            ModAccessorySlotPlayer modSlotPlayer = player.GetModPlayer<ModAccessorySlotPlayer>();
            for (int slotIndex = 0; slotIndex < modSlotPlayer.SlotCount; slotIndex++)
            {
                Item slotItem = LoaderManager.Get<AccessorySlotLoader>().Get(slotIndex, player)?.FunctionalItem;
                parts.Add(slotItem == null || slotItem.IsAir ? "-" : slotItem.type + ":" + slotItem.prefix);
            }

            return string.Join(",", parts);
        }

        internal static string PrefixName(int prefix)
        {
            if (prefix <= 0 || Lang.prefix == null || prefix >= Lang.prefix.Length)
                return string.Empty;
            return Lang.prefix[prefix]?.Value ?? string.Empty;
        }

        private static int SafeShmDowned()
        {
            try
            {
                return tsorcRevampWorld.SHMDowned;
            }
            catch
            {
                return 0;
            }
        }

        internal static string ModVersion()
        {
            try
            {
                return ModContent.GetInstance<global::tsorcRevamp.tsorcRevamp>().Version.ToString();
            }
            catch
            {
                return "unknown";
            }
        }
    }
}

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
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

        private const int SampleIntervalTicks = 60;
        private const int MaxEncounterTicks = 60 * 60 * 30;   // 30 minutes, runaway guard

        private static BalanceEncounter _current;
        private static readonly Dictionary<int, WeaponUsage> Weapons = new();
        private static readonly Dictionary<int, NpcDamage> NpcDamageByType = new();

        private static string _sessionId;
        private static int _anchorNpcId = -1;
        private static int _anchorNpcType = -1;
        private static long _startTick;
        private static long _nextSampleTick;
        private static bool _lastPlayerDead;
        private static string _startGearFingerprint;
        private static int _lastCeruleanCharges = -1;
        private static readonly HashSet<int> LiveProjectileItems = new();

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

        internal static void RecordHit(NPC npc, Player player, int itemType, int ammoType, int damageDone, bool crit)
        {
            if (!Active || damageDone <= 0 || npc == null || player == null || player.whoAmI != Main.myPlayer)
                return;

            if (_current == null)
            {
                // Only a boss opens an encounter. Trash-mob damage outside a boss fight is deliberately
                // not logged — it isn't what the balance pass needs and it would bloat the file.
                if (!IsBossAnchor(npc))
                    return;
                Begin(npc, player);
            }

            bool isAnchor = npc.whoAmI == _anchorNpcId && npc.type == _anchorNpcType;
            int tick = (int)(Main.GameUpdateCount - (uint)_startTick);

            NpcDamage bucket = GetNpcBucket(npc, isAnchor);
            bucket.damage += damageDone;
            bucket.hits++;

            WeaponUsage weapon = GetWeapon(itemType);
            if (weapon != null)
            {
                if (ammoType > 0)
                    AddAmmo(weapon.ammo, ammoType, damageDone);

                if (isAnchor)
                {
                    weapon.damageToBoss += damageDone;
                    weapon.hitsOnBoss++;
                    if (crit)
                        weapon.critsOnBoss++;
                    if (weapon.firstHitTick < 0)
                        weapon.firstHitTick = tick;
                    weapon.lastHitTick = tick;
                }
                else
                {
                    weapon.damageToOthers += damageDone;
                }
            }

            if (isAnchor)
                _current.totalDamageToBoss += damageDone;
            else
                _current.totalDamageToOthers += damageDone;
        }

        internal static void RecordDamageTaken(int amount)
        {
            if (_current != null && amount > 0)
                _current.damageTaken += amount;
        }

        internal static void NotifyKilled(NPC npc)
        {
            if (_current != null && npc.whoAmI == _anchorNpcId && npc.type == _anchorNpcType)
                End("kill");
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

            if (Main.GameUpdateCount >= (uint)_nextSampleTick)
            {
                _nextSampleTick = Main.GameUpdateCount + SampleIntervalTicks;
                NPC anchor = AnchorNpc();
                _current.bossLifeTimeline.Add(anchor?.life ?? 0);
                _current.playerLifeTimeline.Add(Math.Max(0, player.statLife));
                _current.manaTimeline.Add(Math.Max(0, player.statMana));
            }

            if (player.dead)
            {
                End("player_death");
                return;
            }

            if (AnchorNpc() == null)
            {
                End("despawn");
                return;
            }

            if (Main.GameUpdateCount - (uint)_startTick > MaxEncounterTicks)
                End("abandoned");
        }

        internal static void AbortForWorldChange() => End("abandoned");

        // ---------------------------------------------------------------- lifecycle

        private static void Begin(NPC boss, Player player)
        {
            _anchorNpcId = boss.whoAmI;
            _anchorNpcType = boss.type;
            _startTick = Main.GameUpdateCount;
            _nextSampleTick = Main.GameUpdateCount;
            _lastPlayerDead = player.dead;
            _lastCeruleanCharges = CeruleanCharges(player);
            Weapons.Clear();
            NpcDamageByType.Clear();

            _current = new BalanceEncounter
            {
                session = SessionId,
                modVersion = ModVersion(),
                startedAt = DateTimeOffset.Now.ToString("O", CultureInfo.InvariantCulture),
                gameMode = Main.GameMode,
                playerCount = 1,
                superHardMode = tsorcRevampWorld.SuperHardMode,
                remixMap = tsorcRevampWorld.RemixMap,
                customMap = tsorcRevampWorld.CustomMap,
                shmDowned = SafeShmDowned(),
                shmScale = tsorcRevampWorld.SHMScale,
                subtleShmScale = tsorcRevampWorld.SubtleSHMScale,
                bossType = boss.type,
                boss = boss.ModNPC?.Name ?? boss.TypeName,
                bossMod = boss.ModNPC?.Mod?.Name ?? "Terraria",
                bossMaxLife = boss.lifeMax,
                bossDefense = boss.defense,
                gear = CaptureGear(player),
            };

            _startGearFingerprint = GearFingerprint(player);
        }

        private static void End(string outcome)
        {
            if (_current == null)
                return;

            BalanceEncounter encounter = _current;
            _current = null;   // cleared first so a throw below can never wedge the tracker on

            try
            {
                int ticks = (int)(Main.GameUpdateCount - (uint)_startTick);
                encounter.outcome = outcome;
                encounter.durationTicks = ticks;
                encounter.durationSeconds = ticks / 60f;
                encounter.bossDps = encounter.durationSeconds > 0.5f
                    ? encounter.totalDamageToBoss / encounter.durationSeconds
                    : 0f;
                encounter.gearChangedMidFight =
                    Main.LocalPlayer != null && GearFingerprint(Main.LocalPlayer) != _startGearFingerprint;

                // Snapshot what the output-based stamina system learned about each weapon, taken here at the
                // end of the fight so the EMA is as converged as it's going to get for this encounter.
                CaptureStaminaBeliefs(Main.LocalPlayer);

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
                _anchorNpcId = -1;
                _anchorNpcType = -1;
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

        private static NPC AnchorNpc()
        {
            if (_anchorNpcId < 0 || _anchorNpcId >= Main.maxNPCs)
                return null;
            NPC npc = Main.npc[_anchorNpcId];
            // The slot can be recycled by an unrelated NPC, so the type has to match too.
            return npc != null && npc.active && npc.type == _anchorNpcType ? npc : null;
        }

        private static NpcDamage GetNpcBucket(NPC npc, bool isAnchor)
        {
            if (!NpcDamageByType.TryGetValue(npc.type, out NpcDamage bucket))
            {
                bucket = new NpcDamage
                {
                    type = npc.type,
                    name = npc.ModNPC?.Name ?? npc.TypeName,
                    isBossAnchor = isAnchor,
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
                };

                // Prefer the live inventory copy's prefix — a reforge is a real slice of the DPS
                // being measured, and the freshly-defaulted probe has none.
                Item live = FindHeldOrInventory(itemType);
                if (live != null)
                {
                    weapon.prefix = live.prefix;
                    weapon.prefixName = PrefixName(live.prefix);
                }

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

            // 0-2 are armor, 3-9 are the accessory slots (including the Demon Heart and Journey slots).
            // 10+ are vanity and are irrelevant to balance.
            for (int i = 0; i < player.armor.Length && i < 10; i++)
            {
                Item item = player.armor[i];
                if (item == null || item.IsAir)
                    continue;

                var slot = new EquipSlot
                {
                    type = item.type,
                    name = item.ModItem?.Name ?? item.Name,
                    mod = item.ModItem?.Mod?.Name ?? "Terraria",
                    prefix = item.prefix,
                    prefixName = PrefixName(item.prefix),
                    defense = item.defense,
                };

                if (i < 3)
                    snapshot.armor.Add(slot);
                else
                    snapshot.accessories.Add(slot);
            }

            for (int i = 0; i < player.buffType.Length; i++)
            {
                int buff = player.buffType[i];
                if (buff > 0 && player.buffTime[i] > 0)
                    snapshot.buffs.Add(BuffLoader.GetBuff(buff)?.Name ?? Lang.GetBuffName(buff));
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
                snapshot.weaponStaminaMult = souls.WeaponStaminaMult;
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
        private static string GearFingerprint(Player player)
        {
            var parts = new List<string>();
            for (int i = 0; i < player.armor.Length && i < 10; i++)
            {
                Item item = player.armor[i];
                parts.Add(item == null || item.IsAir ? "-" : item.type + ":" + item.prefix);
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

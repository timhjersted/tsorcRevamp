using System.Collections.Generic;

namespace tsorcRevamp.Utilities.Balance
{
    /// <summary>
    /// One weapon's contribution inside a single boss encounter. Damage is split boss-vs-everything-else
    /// so that add-heavy arenas don't inflate a weapon's apparent boss DPS.
    /// </summary>
    internal sealed class WeaponUsage
    {
        public int itemType;
        public string item;
        public string itemMod;
        public int prefix;
        public string prefixName;
        public int baseDamage;
        public string damageClass;
        public int useTime;

        // What the output-based stamina system BELIEVES about this weapon at the moment the encounter is
        // written, so the log can be used to check the system against measured reality: staminaDamagePerUseEma
        // is its learned damage-per-use, staminaCostPerUse the cost that produces. Compare against
        // damageToBoss/hitsOnBoss in the same record — if the EMA is converging correctly they should agree.
        public float staminaDamagePerUseEma;
        public float staminaCostPerUse;

        public long damageToBoss;
        public long damageToOthers;
        public int hitsOnBoss;
        public int critsOnBoss;

        /// <summary>Ticks this item was the held item during the encounter. Per-weapon DPS must be
        /// computed over this window, not the whole fight — players swap mid-fight constantly.</summary>
        public int heldTicks;

        /// <summary>Ticks at least one projectile attributed to this item was alive. This — not
        /// <see cref="heldTicks"/> — is the correct DPS window for summons and sentries, whose staff
        /// is almost never the held item while the minions do the damage.</summary>
        public int activeTicks;

        public int firstHitTick = -1;
        public int lastHitTick = -1;

        /// <summary>Populated for ranged weapons. Ammo choice is a large, invisible slice of practical
        /// ranged DPS, so a weapon's numbers can't be compared without it.</summary>
        public List<AmmoUsage> ammo = new();
    }

    internal sealed class AmmoUsage
    {
        public int type;
        public string name;
        public long damage;
        public int hits;
    }

    /// <summary>Damage dealt to one NPC type during an encounter. Segmented bosses (Hellkite, Seath,
    /// Destroyer) show up here as separate rows, which is how piercing multipliers become measurable.</summary>
    internal sealed class NpcDamage
    {
        public int type;
        public string name;
        public bool isBossAnchor;
        public long damage;
        public int hits;
    }

    internal sealed class EquipSlot
    {
        public int type;
        public string name;
        public string mod;
        public int prefix;
        public string prefixName;
        public int defense;
    }

    /// <summary>Everything on the player that multiplies weapon output, captured at encounter start.</summary>
    internal sealed class GearSnapshot
    {
        public List<EquipSlot> armor = new();
        public List<EquipSlot> accessories = new();
        public List<string> buffs = new();

        public int maxLife;
        public int maxMana;
        public int defense;

        // --- Souls Mode ---------------------------------------------------------------------
        // Without these, a staminaSpent figure means nothing: Classic pays no weapon stamina at all,
        // and the two Souls classes pay different multipliers that the stamina-system config changes
        // again. Samples from different modes are simply not comparable.
        /// <summary>"Classic", "Unkindled" or "BearerOfTheCurse".</summary>
        public string soulsMode;
        public bool usesWeaponStamina;
        /// <summary>The player-wide damage-per-SECOND EMA that the output-based stamina system normalises every
        /// weapon's cost against. Logged because it's the denominator behind every staminaCostPerUse above.</summary>
        public float playerDamagePerSecondEma;
        /// <summary>Effective cost multiplier on everything that spends stamina, already including the
        /// stamina-system config and the Tired debuff. Divide staminaSpent by this to compare across
        /// modes.</summary>
        public float weaponStaminaMult;
        /// <summary>Tired applies +25% to every stamina cost. A sample taken while Tired is inflated,
        /// so it's flagged rather than silently folded into the multiplier alone.</summary>
        public bool tired;
        public float staminaMax;
        public float staminaCurrent;

        public float meleeDamage;
        public float rangedDamage;
        public float magicDamage;
        public float summonDamage;
        public float genericDamage;

        public float meleeCrit;
        public float rangedCrit;
        public float magicCrit;

        public float meleeSpeed;
        public int minionSlots;
    }

    /// <summary>
    /// One boss fight. This is the unit of analysis: practical DPS, kill time, and the build that
    /// produced them all live in the same record, so no cross-referencing is needed downstream.
    /// </summary>
    internal sealed class BalanceEncounter
    {
        public string @event = "encounter";
        public int schema = 1;
        public string session;
        public string modVersion;
        public string startedAt;

        // Context that changes what the numbers mean.
        public int gameMode;          // 0 classic, 1 expert, 2 master, 3 journey
        public int playerCount;
        public bool superHardMode;
        public bool remixMap;
        public bool customMap;
        public int shmDowned;
        public float shmScale;
        public float subtleShmScale;

        // The boss.
        public int bossType;
        public string boss;
        public string bossMod;
        public int bossMaxLife;
        public int bossDefense;

        // Result.
        public string outcome;        // kill | player_death | despawn | abandoned
        public int durationTicks;
        public float durationSeconds;
        public long totalDamageToBoss;
        public long totalDamageToOthers;
        public float bossDps;         // totalDamageToBoss / durationSeconds
        public int playerDeaths;
        public long damageTaken;
        public bool gearChangedMidFight;

        public GearSnapshot gear;
        public List<WeaponUsage> weapons = new();
        public List<NpcDamage> npcDamage = new();

        /// <summary>Boss life sampled once per second. Reconstructs instantaneous DPS, invulnerability
        /// windows and phase gates without needing to read any boss's AI source.</summary>
        public List<int> bossLifeTimeline = new();

        /// <summary>Player life sampled on the same 1 Hz cadence as <see cref="bossLifeTimeline"/>.</summary>
        public List<int> playerLifeTimeline = new();

        /// <summary>Player mana on the same 1 Hz cadence. Magic weapons' practical DPS is gated by the
        /// mana economy, not by their on-paper damage — this plus <see cref="ceruleanChargesUsed"/>
        /// makes that gate visible instead of hiding it inside the DPS average.</summary>
        public List<int> manaTimeline = new();

        /// <summary>Cerulean Tear charges spent during the fight (SoulsMode mana refills).</summary>
        public int ceruleanChargesUsed;
    }

    /// <summary>
    /// One controlled target-dummy run. Deliberately a different record type from
    /// <see cref="BalanceEncounter"/>: this measures the weapon, that measures the encounter.
    /// </summary>
    internal sealed class WeaponBenchmark
    {
        public string @event = "benchmark";
        public int schema = 1;
        public string session;
        public string modVersion;
        public string startedAt;

        public int itemType;
        public string item;
        public string itemMod;
        public int prefix;
        public string prefixName;
        public int baseDamage;
        public int baseCrit;
        public int useTime;
        public string damageClass;

        public List<AmmoUsage> ammo = new();

        public int windowSeconds;

        /// <summary>Which of the weapon's attacks this sample measures. Weapons here can map up to four
        /// attacks to one item, and they are benchmarked separately — merging them would average a
        /// light attack with a heavy one and describe neither.</summary>
        public string variant;
        public int attackMode;
        public int altFunction;

        /// <summary>1-based sample number for this weapon within the session. Several samples of the
        /// same weapon give a median and a spread instead of one number that might have been a fluke.</summary>
        public int sampleIndex;

        public int dummyType;
        public string dummy;
        public int dummyDefense;

        public long totalDamage;
        public int hits;
        public int crits;
        public float rawDps;

        // --- crowd / piercing ---------------------------------------------------------------
        /// <summary>Distinct dummies this weapon actually landed on.</summary>
        public int targetsHit;
        /// <summary>Dummies alive in the world when the run opened. Compared against
        /// <see cref="targetsHit"/> this shows reach and spread, not just raw multi-hit.</summary>
        public int dummiesPresent;
        /// <summary>Damage to each dummy, descending. The shape of this list is the AoE profile —
        /// flat means true cleave, steeply falling means incidental splash.</summary>
        public List<long> perTargetDamage = new();
        /// <summary>DPS against the single dummy that took the most damage. This is the honest
        /// single-target number even when the run happened against a crowd.</summary>
        public float singleTargetDps;
        /// <summary>totalDamage / best single target's damage. WARNING: on its own this cannot tell
        /// cleave from scatter — a non-piercing sword swinging into a pack spreads its hits across
        /// targets and scores just as high as a true cleave weapon. Always read it together with
        /// <see cref="maxTargetsPerAttack"/>.</summary>
        public float crowdMultiplier;

        /// <summary>Most targets ever struck by a single attack — one projectile's whole lifetime, or
        /// one melee swing. This is the metric that actually separates piercing/cleave (&gt;1) from a
        /// single-target weapon whose damage merely scattered across a pack (=1).</summary>
        public int maxTargetsPerAttack;
        /// <summary>Mean targets struck per landed attack. Between 1 and
        /// <see cref="maxTargetsPerAttack"/>; how reliably the cleave actually connects.</summary>
        public float avgTargetsPerAttack;

        // --- accuracy / cadence -------------------------------------------------------------
        /// <summary>Projectiles attributed to this weapon that spawned during the run.</summary>
        public int projectilesSpawned;
        /// <summary>hits / projectilesSpawned. Below 1 means shots are missing (accuracy); above 1
        /// means each projectile lands repeatedly (piercing or multi-hit).</summary>
        public float hitsPerProjectile;
        public float hitsPerSecond;
        public float critRate;

        // --- per-hit distribution -----------------------------------------------------------
        /// <summary>Average damage per landed hit. Critical for the defense-scaling question: flat
        /// defense reduction guts many-small-hits weapons far harder than few-big-hits weapons at
        /// identical DPS, and that difference is invisible in a DPS number alone.</summary>
        public float avgDamagePerHit;
        public int minHit;
        public int maxHit;
        /// <summary>Standard deviation of per-hit damage — the reliability dimension.</summary>
        public float damageStdDev;

        // --- range ------------------------------------------------------------------------
        public float avgHitRange;
        public float maxHitRange;

        // --- sustain ----------------------------------------------------------------------
        /// <summary>Damage per second of the run. Burst-then-collapse weapons (mana or stamina
        /// limited) look identical to sustained ones in an averaged DPS figure; here they don't.</summary>
        public List<long> damageTimeline = new();
        public float staminaSpent;

        /// <summary>
        /// Fraction of the sample spent at effectively zero stamina.
        ///
        /// This matters more than <see cref="staminaSpent"/>. Once the bar bottoms out the player can
        /// only attack as fast as stamina regenerates, so total spend stops measuring the weapon and
        /// starts measuring the regen rate — several samples in the first real session logged an
        /// identical 406 despite hit counts ranging from 38 to 98. A high value here means the weapon's
        /// sustained DPS is regen-gated rather than weapon-gated, which is the actual balance finding.
        /// </summary>
        public float staminaStarvedFraction;
        public float minStamina;

        /// <summary>The class damage multiplier that was in effect. Dividing it out of
        /// <see cref="rawDps"/> is what makes a geared run comparable to a naked one.</summary>
        public float classMultiplier;
        public float classCrit;

        /// <summary>Gear-normalized DPS: <see cref="rawDps"/> with the class damage multiplier removed.
        /// Still imperfect — crit chance, attack-speed and set/proc effects are not a single
        /// multiplier — so <see cref="gearNeutral"/> runs remain the gold standard.</summary>
        public float normalizedDps;

        /// <summary>True when no armor or accessories were equipped and every class multiplier was
        /// ~1.0, meaning rawDps needs no correction at all.</summary>
        public bool gearNeutral;

        public long manaSpent;
        public GearSnapshot gear;
    }
}

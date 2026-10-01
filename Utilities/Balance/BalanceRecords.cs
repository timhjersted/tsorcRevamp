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

        /// <summary>Boss damage split by the projectile type that dealt it (type -1 is a melee swing). See
        /// <see cref="ProjectileUsage"/> for why a weapon's total can't stand in for any one of its attacks.</summary>
        public List<ProjectileUsage> projectiles = new();

        // --- Regain system (Systems/Regain) --------------------------------------------------
        // Exists to catch the shape a busted weapon takes: a huge regainHealed relative to
        // damageTaken on the encounter means this weapon is draintanking; a high regainGateRefusals
        // relative to regainCredits means it is repeatedly trying to credit off ONE activation
        // (multishot/piercing/beam) and the swing-window gate is the only thing stopping it.
        /// <summary>Total HP this weapon recovered via Regain during the encounter.</summary>
        public long regainHealed;
        /// <summary>Successful Regain credits earned by this weapon.</summary>
        public int regainCredits;
        /// <summary>Credits refused by the swing-window gate (Regain.CreditGateFraction) because
        /// another credit from this same weapon had already landed too recently.</summary>
        public int regainGateRefusals;
        /// <summary>Sum of the distance-to-target, in tiles, at each successful credit. Divide by
        /// regainCredits for the average - a weapon credited almost entirely from far outside melee
        /// range gives itself away here.</summary>
        public float regainDistanceSumTiles;

        /// <summary>Boss damage this weapon dealt while Mana Burn was active.</summary>
        public long damageWhileManaBurn;

        // --- Derived at the end of the encounter, so the log carries the weapon's own numbers and
        // analysis never has to divide them back out of the totals. -------------------------------

        /// <summary>What the weapon deals per use right now: <c>Player.GetWeaponDamage</c> folds in the class
        /// multiplier and prefix. <see cref="baseDamage"/> alone is the tooltip number, which can sit far from
        /// the real hit (Fire Spirit Tome 4 lists 2000 and lands about 490).</summary>
        public int effectiveDamage;

        /// <summary>damageToBoss / hitsOnBoss. Compare with <see cref="effectiveDamage"/> to see how much of a use
        /// actually lands, and with other weapons for the flat-defense question.</summary>
        public float damagePerHitMean;

        public float hitsPerHeldSec;

        /// <summary>damageToBoss over the seconds the item was in hand. Boss-independent for a given weapon and
        /// gear, which is what separates "this weapon is strong" from "this boss is soft". 0 for summons and
        /// sentries, whose staff is never held while they work - read <see cref="damagePerActiveSec"/> instead.</summary>
        public float damagePerHeldSec;

        /// <summary>damageToBoss over the seconds a projectile from this item was alive.</summary>
        public float damagePerActiveSec;

        /// <summary>Mana actually consumed by this weapon, from the OnConsumeMana hook. Net-sampling statMana
        /// reads zero once regeneration outpaces the cost, which it does at the mana pools late game reaches.</summary>
        public int manaSpent;

        /// <summary>Ticks this weapon was held with less mana than one use costs - the mana economy gating it.</summary>
        public int manaStarvedTicks;

        /// <summary>True the first time this item dealt boss damage in the current game session, so a DPS jump
        /// can be tied to the weapon that was just acquired.</summary>
        public bool firstUseThisSession;

        /// <summary>The item is on the outlier list (joke / endgame-novelty weapons such as Divine Boom Cannon).
        /// Fights that use one are not representative of the progression curve.</summary>
        public bool outlier;
    }

    /// <summary>
    /// Damage dealt by one projectile type (or a melee swing, type -1). One weapon can field several of these -
    /// the Runeterra orbs have a held orb, a thrown orb, a flame and a charm, all benched under one "primary" attack -
    /// and they differ by an order of magnitude per hit (about 300 versus about 4,000 on Orb of Spirituality), so
    /// averaging them describes none of them.
    /// </summary>
    internal sealed class ProjectileUsage
    {
        public int type;
        public string name;
        public int hits;
        public int crits;
        public long damage;

        /// <summary>Projectiles of this type that spawned (bench only). hits / spawned below 1 is misses, above 1 is
        /// piercing or repeat hits.</summary>
        public int spawned;

        public float avgDamagePerHit;
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

    /// <summary>Damage the player took from one source (an NPC type or a projectile type) over an encounter.</summary>
    internal sealed class DamageSourceUsage
    {
        public string source;
        public int hits;
        public long damage;
        public long rawDamage;
        public int largestHit;
    }

    /// <summary>One hit the player took. Only the largest few survive into the record - see
    /// <see cref="BalanceEncounter.topHitsTaken"/>.</summary>
    internal sealed class HitTaken
    {
        public int tick;
        public int damage;
        public int rawDamage;
        public string source;
    }

    /// <summary>One healing source (Estus, a potion type) and how often it was used in an encounter.</summary>
    internal sealed class HealingUsage
    {
        public string source;
        public int uses;
        public long healed;
    }

    /// <summary>One boss attack state: how many times it was entered and how long the boss spent in it.</summary>
    internal sealed class AttackUsage
    {
        public string attack;
        public int entries;
        public int ticks;
    }

    /// <summary>The pieces of one class's damage multiplier, straight from <c>StatModifier</c>. Which buff or
    /// accessory contributed each piece is not recoverable - tModLoader does not track sources - but the
    /// additive / multiplicative split already shows whether a high multiplier is stacked or compounded.</summary>
    internal sealed class DamageModifierBreakdown
    {
        public float additive;
        public float multiplicative;
        public float flat;
        public float baseValue;
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

    /// <summary>An item in a tModLoader ModAccessorySlot (the Bearer of the Curse slot, the Supersonic wing slot).
    /// These live outside <c>Player.armor</c>, so the armor/accessory loop never sees them.</summary>
    internal sealed class ModdedEquipSlot
    {
        public string slot;
        public EquipSlot item;
    }

    /// <summary>Everything on the player that multiplies weapon output, captured at encounter start.</summary>
    internal sealed class GearSnapshot
    {
        public List<EquipSlot> armor = new();
        public List<EquipSlot> accessories = new();

        /// <summary>Functional items in mod accessory slots, which <see cref="accessories"/> does not cover.</summary>
        public List<ModdedEquipSlot> moddedAccessories = new();

        // --- Automatic loadout description. Taken from what is actually on the player when the record is written, so
        // a test's setup (naked, partial, endgame) is in the record itself and never depends on notes or a command. ---
        /// <summary>Armor item names joined with "/" - the set, in effect. Empty when no armor is worn.</summary>
        public string armorSet = string.Empty;
        public int armorPieces;

        /// <summary>Filled accessory slots across vanilla and mod slots, not counting the right-click slot.</summary>
        public int accessoryCount;

        /// <summary>No armor, no accessory in any slot and nothing in the right-click slot.</summary>
        public bool noGearEquipped;
        public int buffCount;

        /// <summary>Short human label such as "naked" or "DragoonHelmet2/DragoonArmor2/DragoonGreaves2 + 6 acc + 4 buffs".</summary>
        public string loadoutTag = string.Empty;

        /// <summary>How many equipped pieces (armor, accessories, mod slots, right-click slot) carry each prefix, e.g.
        /// Warding 2, Menacing 2, Arcane 2. Prefixes move damage, crit, defense and mana, so they belong in the loadout.</summary>
        public Dictionary<string, int> prefixCounts = new();
        /// <summary>The Active Shields Right-Click (2nd) slot item, or null when the slot is empty. Kept out of
        /// <see cref="accessories"/> because it is NOT an accessory slot: a shield parked here grants its passives
        /// while leaving every accessory slot free, so it is the one loadout choice the accessory list can't show.</summary>
        public EquipSlot secondSlot;
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

        /// <summary>Per-class damage multiplier components, keyed melee / ranged / magic / summon / generic.
        /// Early magic runs at 3x and later magic at about 1.8x with nothing in the flat floats to explain it.</summary>
        public Dictionary<string, DamageModifierBreakdown> damageModifiers = new();
    }

    /// <summary>
    /// One boss fight. This is the unit of analysis: practical DPS, kill time, and the build that
    /// produced them all live in the same record, so no cross-referencing is needed downstream.
    /// </summary>
    internal sealed class BalanceEncounter
    {
        public string @event = "encounter";
        public int schema = 2;
        public int loggerRevision = BalanceLog.LoggerRevision;
        public string session;
        public string modVersion;

        /// <summary>Hash of every item's and boss's balance-relevant stats at load. Two records with the same value
        /// were taken against identical weapon and boss numbers, so data survives a mod update only when
        /// this still matches (or the analysis knows to split it).</summary>
        public string contentFingerprint;
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

        /// <summary>Vanilla's per-difficulty multipliers (<c>Main.GameModeInfo</c>). The mod's own scaling on top
        /// (x1.5 on its bosses in Master, x1.275 on vanilla ones) is captured by <see cref="lifeMaxMultVsBase"/>.</summary>
        public float gameModeLifeMult;
        public float enemyDamageMult;

        /// <summary>The New Enemy Balance health retune was on for this fight. Records with it off or absent use the original boss health.</summary>
        public bool newEnemyBalance;

        /// <summary>Boss and difficulty progress at the start, so a DPS jump can be tied to how far along the
        /// player was. <see cref="downedBossTypes"/> is the mod's own NewSlain key list.</summary>
        public int downedBossCount;
        public List<int> downedBossTypes = new();

        // The boss.
        public int bossType;
        public string boss;
        public string bossMod;
        public int bossMaxLife;

        /// <summary>The anchor's lifeMax at fight start, then one entry each time a boss head raises its own
        /// lifeMax mid-fight (Pinwheel: 2000, then 5000). More than one entry means a multi-bar fight whose
        /// bossMaxLifeTotal is the sum of the bars.</summary>
        public List<int> lifeBars = new();
        public int bossDefense;

        /// <summary>The anchor's lifeMax in ContentSamples (before difficulty / SHM / world scaling) and the
        /// live lifeMax over it. Divide bossMaxLife by this multiplier to get comparable base HP across
        /// Expert, Master and SHM, whatever applied the scaling.</summary>
        public int baseLifeMax;
        public float lifeMaxMultVsBase;
        public int bossContactDamage;
        public int baseContactDamage;

        /// <summary>Sum of lifeMax over every boss part that joined the fight (worm segments and linked body parts
        /// count once). The denominator for the life fractions below.</summary>
        public long bossMaxLifeTotal;
        public int participantCount;

        // Result.
        public string outcome;        // kill | player_death | despawn | abandoned
        public int durationTicks;
        public float durationSeconds;

        /// <summary>Damage to every boss part in the group, not just the anchor.</summary>
        public long totalDamageToBoss;
        public long totalDamageToOthers;

        /// <summary>Sum of npcDamage, parts and minions alike. Parts that are not boss-flagged (some segmented
        /// bosses) land here rather than in totalDamageToBoss.</summary>
        public long totalDamageToAllNpcs;
        public float bossDps;         // totalDamageToBoss / durationSeconds
        public int playerDeaths;
        public bool bagDropped;
        public bool gearChangedMidFight;

        // --- Attempts: a death log is only a difficulty signal if you can see how far it got. ---
        /// <summary>1 for the first try at this boss since its last kill (or since the session began), 2 for the
        /// next, and so on.</summary>
        public int attemptIndex;
        public string fightGroupId;
        public float bossLifeFractionAtEnd;
        public float lowestBossLifeFraction;

        // --- Damage taken. damageTaken is applied damage (after defense); raw is before it. ---
        public long damageTaken;
        public long damageTakenRaw;
        public int hitsTaken;
        public int largestHitTaken;

        /// <summary>largestHitTaken / max life. Well above 1 means the boss one-shots the player outright, which
        /// is the signature of fighting it before the intended tier.</summary>
        public float largestHitVsMaxLife;
        public List<DamageSourceUsage> damageTakenBySource = new();

        /// <summary>The biggest hits taken, largest first (capped at 15).</summary>
        public List<HitTaken> topHitsTaken = new();

        // --- Healing, so survival from healing and from defense can be told apart. ---
        public int estusDrinks;
        public long estusHealed;
        public int potionUses;
        public long potionHealed;
        public List<HealingUsage> healing = new();

        // --- Time-to-kill against the design target. ---
        public int targetTtkMinSeconds;
        public int targetTtkMaxSeconds;

        /// <summary>duration / the target midpoint. Below 1 is faster than designed. Set on kills only.</summary>
        public float ttkRatio;

        /// <summary>A kill under 60% of the target minimum - the fight was bypassed rather than played.</summary>
        public bool bypassedFight;

        /// <summary>The boss HP that would have made this kill take the target midpoint at the DPS actually
        /// achieved. Set on kills only.</summary>
        public long hpForTargetAtObservedDps;

        // --- Engagement: how much of the fight the boss could actually be hurt. ---
        /// <summary>Boss damage per second, on the same 1 Hz cadence as bossLifeTimeline.</summary>
        public List<int> damageTimeline = new();

        /// <summary>totalDamageToBoss over the seconds that had a hit in them. Separates "boss is evasive" from
        /// "weapon is weak": a low bossDps with a high engagedDps is the boss, not the weapon.</summary>
        public float engagedDps;
        public float uptimeFraction;
        public int longestNoDamageGapSec;
        public float burst10sDps;

        // --- Build and outlier flags. ---
        public string primaryClass;
        public Dictionary<string, long> damageByClass = new();
        public bool usedOutlierWeapon;
        public List<string> outlierWeapons = new();

        /// <summary>Boss attack states entered (puppet bosses report these; other bosses leave it empty).</summary>
        public List<AttackUsage> attacks = new();

        /// <summary>Sum of every weapon's regainHealed - the encounter-level "how much of damageTaken
        /// did Regain erase" figure without having to sum the per-weapon list by hand.</summary>
        public long totalRegainHealed;

        /// <summary>Player.immuneTime actually granted by each hit taken this encounter, in ticks, one
        /// entry per hit, in order. Already includes iframe-extending effects like Cross Necklace's
        /// longInvince - lets a Regain balance pass check whether longer iframes correlate with a higher
        /// totalRegainHealed/damageTaken ratio (the "stand in the boss and tank" concern) instead of
        /// guessing from the ratio alone.</summary>
        public List<int> hurtImmuneTimes = new();

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

        /// <summary>Stamina on the same 1 Hz cadence. Mana Burn switches on when stamina falls under a third of
        /// its maximum, so this is the line that shows what triggers it.</summary>
        public List<int> staminaTimeline = new();

        // --- Cerulean flask (Souls-mode mana refill). Counted at the moment a drink completes, so a charge restored
        // at a bonfire mid-fight cannot cancel a drink out of the count. ---
        /// <summary>Flasks drunk during the fight.</summary>
        public int ceruleanChargesUsed;

        /// <summary>Mana the drinks restored, as the flask itself computes it (flat gain, max-mana bonus, regen and
        /// restoration-time bonuses). This is the refill the mage's mana economy actually got.</summary>
        public long ceruleanManaRestored;
        public int ceruleanChargesAtStart;
        public int ceruleanChargesMax;

        // --- Mana. maxMana is in <see cref="gear"/> at the start of the fight. ---
        /// <summary>Max mana when the fight ended. It can differ from the start value when a buff or accessory
        /// swap changes the pool mid-fight.</summary>
        public int maxManaAtEnd;

        // --- Mana Burn (Arcane Sorcery): with stamina under 33%, a Bearer of the Curse holding a magic weapon gets
        // +25% magic damage and +20% magic attack speed for 5 s, at double mana cost and -40% damage resistance.
        // The gear snapshot only says whether the buff was up when the fight began; these say how much of the fight
        // it was up for and what it did. ---
        public int manaBurnTicks;

        /// <summary>manaBurnTicks over the fight's length.</summary>
        public float manaBurnUptimeFraction;

        /// <summary>Times the buff switched on during the fight.</summary>
        public int manaBurnActivations;

        /// <summary>Boss damage dealt while the buff was active, and its share of totalDamageToBoss.</summary>
        public long damageDuringManaBurn;
        public float manaBurnDamageFraction;
    }

    /// <summary>
    /// One controlled target-dummy run. Deliberately a different record type from
    /// <see cref="BalanceEncounter"/>: this measures the weapon, that measures the encounter.
    /// </summary>
    internal sealed class WeaponBenchmark
    {
        public string @event = "benchmark";
        public int schema = 2;
        public int loggerRevision = BalanceLog.LoggerRevision;
        public string session;
        public string modVersion;
        public string contentFingerprint;
        public string startedAt;

        // Difficulty and world state. A bench run has no boss to carry these, but the player's damage
        // multipliers and the cost of mana / stamina still depend on them.
        public int gameMode;
        public bool superHardMode;
        public bool remixMap;
        public float shmScale;

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

        /// <summary>The class's own damage multiplier. It leaves out generic damage, which armor sets and accessories
        /// also feed - on one endgame mage the class figure was 1.43 and the generic 1.79 - so dividing by this alone
        /// cannot make a geared run comparable to a naked one. See <see cref="totalDamageMultiplier"/>.</summary>
        public float classMultiplier;
        public float classCrit;

        /// <summary>Class plus generic damage, i.e. <c>Player.GetTotalDamage(class)</c>: everything the player's damage
        /// stats do to this weapon.</summary>
        public float totalDamageMultiplier;

        /// <summary>Crit damage multiplier assumed when removing crit from the DPS. Vanilla crits deal 2x; a weapon
        /// or accessory that changes that is not reflected.</summary>
        public float critDamageMultiplierAssumed;

        /// <summary><see cref="rawDps"/> with the total damage multiplier removed. This is the number to compare
        /// across loadouts; the class-only version is kept as <see cref="classOnlyNormalizedDps"/> so older figures
        /// stay comparable.</summary>
        public float normalizedDps;

        /// <summary>The old normalization: rawDps over the class multiplier alone. Under-corrects whenever generic
        /// damage or crit differ between samples.</summary>
        public float classOnlyNormalizedDps;

        /// <summary>
        /// DPS with the total damage multiplier and the crits taken back out - what the weapon does per second as if
        /// no hit crit. On a naked and an endgame Orb of Spirituality sample the damage per hit differed by 4.6x:
        /// about 2.6x from damage multipliers and about 1.7x from crit going 20% to 100%, so removing both is what
        /// puts them on the same footing. Weapons with crit built in lose it here, which is the point.
        /// </summary>
        public float neutralDps;

        /// <summary>The player's loadout changed (armor, accessories or a mod slot) while this run was open, so the
        /// gear snapshot may not describe every hit in it.</summary>
        public bool gearChangedDuringRun;

        /// <summary>Damage split by the projectile type that dealt it (type -1 is a melee swing). The orbs' held,
        /// thrown, flame and charm attacks all benched as one "primary" attack, and the split is what shows which
        /// of them a sample actually measured.</summary>
        public List<ProjectileUsage> damageByProjectile = new();

        /// <summary>True when no armor or accessories were equipped and every class multiplier was
        /// ~1.0, meaning rawDps needs no correction at all.</summary>
        public bool gearNeutral;

        /// <summary>Mana consumed during the run, from the OnConsumeMana hook (not net statMana).</summary>
        public long manaSpent;
        public float manaPerSecond;

        /// <summary>Fraction of the run with the Mana Burn buff active (+25% magic damage, +20% magic attack speed).
        /// The buff is already in <c>gear.buffs</c> as a start-of-run snapshot; this is the time it was really up.</summary>
        public float manaBurnFraction;

        // --- Prefix. Every bench sample so far carried a prefix (Mythical, Legendary, Godly...), which
        // inflates DPS with no way to tell by how much. These put the prefix back on a level footing. ---
        /// <summary>Damage of the prefixed item over the unprefixed one, from SetDefaults + Prefix(). 1 with no prefix.</summary>
        public float prefixDamageMult;

        /// <summary>Attack speed of the prefixed item over the unprefixed one (useTime ratio). 1 with no prefix.</summary>
        public float prefixSpeedMult;

        /// <summary>rawDps with the prefix's damage and speed removed. An estimate - crit and mana prefix
        /// effects are not folded in - but it separates a weapon from its reforge.</summary>
        public float prefixNeutralDps;

        public GearSnapshot gear;
    }
}

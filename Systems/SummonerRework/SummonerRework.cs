using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Systems.SummonerRework
{
    /// <summary>
    /// The gate for Spirit Ashes (mortal, mana-summoned minions/sentries) and their Drawn Ire companion
    /// mechanic, plus every number those two tune. Regain used to share this flag but now has its own -
    /// see Systems/Regain/Regain.cs - since it applies more broadly and has nothing to do with minions.
    ///
    /// Deliberately does NOT touch Lethal Tempo or Conqueror. Those stay exactly as they are everywhere,
    /// including Bearer of the Curse - Conqueror stacking on top of mortal minions is intentional
    /// friction that keeps BotC the harder tier; Unkindled never had Conqueror to begin with, so a
    /// summoner there gets Spirit Ashes at full strength with nothing to fight against.
    ///
    /// Active() reads a config bool today, but nothing else depends on that - if this ever becomes
    /// default behaviour instead of an opt-in toggle, only this method changes.
    /// </summary>
    public static class SummonerRework
    {
        /// <summary>Fraction of the summoner's max HP a fresh minion or sentry spawns with. 1.0 of a
        /// 500 HP ceiling is 500 - deliberately sturdy, since ashes are meant to hold a line rather
        /// than pop to a stray hit, and losing one costs mana to replace. Snapshot at summon time; a
        /// minion does not re-scale if the player's max HP changes while it is out.</summary>
        public const float MinionHealthFraction = 1f;

        /// <summary>Fraction of incoming damage a minion takes from an absorbed hostile PROJECTILE (see
        /// TryAbsorbHostileProjectile) - whether that projectile is a genuinely ranged attack or a melee
        /// weapon's hitbox represented as one; deliberately not split further, since telling those apart
        /// reliably is not worth the exception list it would take. Raised from 0.5 specifically so a
        /// ranged-style ash, which is hit far less often than a melee-contact one, takes a real bite on
        /// the rarer occasions it IS caught - see MinionContactDamageMult for the melee side of this same
        /// rebalance.</summary>
        public const float MinionDamageTakenMult = 0.6f;

        /// <summary>Ticks before an ash can block another hostile projectile. Also applied on spawn, so
        /// resummoning into an active AoE is not instant death.
        ///
        /// Tracked SEPARATELY from contact below. A single shared timer meant a minion parked inside an
        /// enemy was permanently occupied by contact and could never body-block a shot - which quietly
        /// removed the more valuable of its two defensive jobs, and removed it specifically from the
        /// melee summons that are always in contact.</summary>
        public const int MinionProjectileBlockCooldownTicks = 60;

        /// <summary>Ticks before body contact can hurt an ash again. Independent of the projectile timer,
        /// so standing in an enemy never costs a minion its ability to block.</summary>
        public const int MinionContactCooldownTicks = 60;

        /// <summary>How long after its last hit a minion still draws its health bar, in ticks.
        /// Matches the vanilla convention of a bar that appears when something is taking damage and
        /// fades once it stops.</summary>
        public const int MinionHealthBarLingerTicks = 180;

        /// <summary>Fraction of an enemy's contact damage a minion takes for standing inside it. Applied
        /// INSTEAD of MinionDamageTakenMult, not on top of it, so this is the whole reduction. It runs
        /// AFTER the player's defense has been subtracted from the raw hit.
        ///
        /// Still well below the rate for real attacks, because melee summons like Abigail fight by
        /// parking inside the enemy's hitbox - contact is their permanent working state, not a mistake.
        /// At 0.25, a ~150-damage boss costs an ash roughly 30 per second (one bite per i-frame window)
        /// against moderate defense, so a full-health one holds an enemy for about fifteen seconds before
        /// it needs Estus or replacing.
        ///
        /// This is the lever for army attrition - reach for it before touching MinionHealthFraction,
        /// since health also governs how much an Estus charge repairs.
        ///
        /// Lowered from 0.25 to 0.2 as the melee-contact half of the same ranged/melee rebalance that
        /// raised MinionDamageTakenMult - a melee-contact ash is already hit far more often than a
        /// ranged one, so it keeps slightly more protection per hit rather than also taking a bigger bite
        /// on top of its existing higher frequency.</summary>
        public const float MinionContactDamageMult = 0.2f;

        /// <summary>How much of an enemy's damage to the PLAYER is removed while it is marked by Drawn
        /// Ire's MELEE tier - i.e. while one of the player's ashes is in physical contact with it. See
        /// DrawnIreRangedDamageReduction for the separate, weaker tier a minion's own ranged attacks
        /// apply instead.
        ///
        /// This is the upside that pays for contact damage: an ash bleeding for standing inside an enemy
        /// is buying something concrete, and the cost and the benefit come from the same event.
        ///
        /// Deliberately a reduction on the enemy rather than a reassignment of the hit - damage never
        /// teleports to a minion standing elsewhere, and no number of ashes can make the player immune
        /// the way an all-or-nothing block would.</summary>
        public const float DrawnIreDamageReduction = 0.4f;

        /// <summary>
        /// The WEAKER Drawn Ire tier: applied while an enemy has recently been hit by one of the
        /// player's ashes from range - a projectile that is summon-classed, is not itself a tracked
        /// minion/sentry body, and is not a whip (see SpiritAshesMinions.OnHitNPC for the exact check) -
        /// rather than by physical contact.
        ///
        /// Deliberately much lower than DrawnIreDamageReduction: a ranged ash is already far safer than a
        /// melee-contact one (it rarely takes the real hits MinionDamageTakenMult/MinionContactDamageMult
        /// price), so its reward for landing hits stays proportionately smaller. If both tiers are active
        /// on the same enemy at once, only the stronger (melee) one applies - see DrawnIre.ModifyHitPlayer
        /// - rather than stacking, matching the existing "no number of ashes reaches immunity" rule.
        /// </summary>
        public const float DrawnIreRangedDamageReduction = 0.1f;

        /// <summary>How strongly Drawn Ire lifts an affected enemy's sprite toward white. This changes
        /// RGB only; the enemy's existing alpha is preserved so translucent enemies remain translucent.</summary>
        public const float DrawnIreWhiteTintStrength = 0.7f;

        /// <summary>How long the Drawn Ire mark lasts after an ash stops touching (melee) or hitting
        /// (ranged) the enemy. Refreshed every tick either tier is re-triggered, so it only counts down
        /// once an ash disengages, stops landing hits, or dies.
        ///
        /// This is the knob for how forgiving the protection is. It does NOT shorten the reduction during
        /// a sustained engagement - contact/hits refresh the mark every time - it only governs the grace
        /// period after a grip is broken, so a shorter window makes losing the target cost something.</summary>
        public const int DrawnIreDurationTicks = 300;

        /// <summary>Fraction of max mana charged per cast of a summon weapon. Vanilla summon staves cost
        /// 0 mana, so this is the whole upkeep cost of an army. Charged on every cast rather than only
        /// when something died - recasting replaces a staff's entire line in vanilla, so there is no
        /// "refill one dead slot" event to hook, and checking which minions are missing would mean
        /// tracking state this system deliberately avoids.
        ///
        /// A FRACTION of the pool rather than a flat number, so the cost stays proportionate across the
        /// mode-specific pool sizes this now runs in (BotC ~1000, Unkindled ~200) without needing a
        /// separate constant per mode that could drift out of sync the next time a pool size changes.</summary>
        public const float SummonManaCostFraction = 0.05f;

        /// <summary>**Currently off, not deleted.** Vanilla is moving summoner to 0 mana entirely (1.4.5
        /// removes it from every summon weapon and sentry - see SummonerEdits.cs, which already matches
        /// that for 1.4.4.9), and summoner is heading toward a clean split from magic/mana generally.
        /// Charging mana for Spirit Ashes on top of that would cut against both. Flip this to bring the
        /// cost back; SummonManaCost/SpiritAshesItem still compute and gate on the real number either way.</summary>
        public const bool SummonManaCostEnabled = false;

        /// <summary>Mana charged for one summon-weapon cast, scaled to THIS player's actual max mana.
        /// See SummonManaCostFraction for why this is a fraction rather than a flat number, and
        /// SummonManaCostEnabled for why callers should treat a cost of 0 as "no charge, no tooltip"
        /// rather than "charge zero".</summary>
        public static int SummonManaCost(Player player)
        {
            if (!SummonManaCostEnabled)
            {
                return 0;
            }

            return (int)System.MathF.Round(player.statManaMax2 * SummonManaCostFraction);
        }

        /// <summary>
        /// A handful of vanilla summon items shoot a lightweight, invisible "spawner" projectile that in
        /// turn creates the real, visible, damageable minion as a child - Abigail's Flower is the
        /// confirmed case: Item.shoot is AbigailCounter, which spawns AbigailMinion. Peak/recovery
        /// tracking has to key off the real minion, not the spawner, which never takes combat damage and
        /// so never reflects an actual loss. SpiritAshesMinions also excludes these types from tracking
        /// entirely, so they never grow their own separate (bogus) ash slot.
        ///
        /// New entries go here as they are found - there is no general way to detect this pattern from
        /// an item alone, so this stays an explicit, per-item list rather than a heuristic.
        /// </summary>
        private static readonly Dictionary<int, int> SpawnerToRealAshType = new()
        {
            { ProjectileID.AbigailCounter, ProjectileID.AbigailMinion },
        };

        /// <summary>True if `projectileType` is one of the invisible spawner types above, and should
        /// never be tracked as an ash in its own right.</summary>
        public static bool IsUntrackedSpawner(int projectileType)
        {
            return SpawnerToRealAshType.ContainsKey(projectileType);
        }

        /// <summary>The real, damageable ash type that summoning `itemShootType` actually produces -
        /// itself, unless it is one of the spawner types above.</summary>
        public static int ResolveTrackedAshType(int itemShootType)
        {
            return SpawnerToRealAshType.GetValueOrDefault(itemShootType, itemShootType);
        }

        private static readonly Dictionary<int, int> RealAshTypeToSpawner = BuildReverseSpawnerMap();

        private static Dictionary<int, int> BuildReverseSpawnerMap()
        {
            var reverse = new Dictionary<int, int>();

            foreach (KeyValuePair<int, int> pair in SpawnerToRealAshType)
            {
                reverse[pair.Value] = pair.Key;
            }

            return reverse;
        }

        /// <summary>
        /// The spawner type (if any) that maintains `realAshType` - see SpawnerToRealAshType. A spawner
        /// like AbigailCounter is not a one-shot "create it once" projectile: it persists for as long as
        /// the buff is active and its own vanilla AI silently respawns its child whenever that child is
        /// missing, entirely inside Projectile.AI() - a path that never touches Item.UseItem, so our
        /// cooldown gate has no way to see or stop it. Killing the spawner alongside a genuine combat
        /// death (see SpiritAshesMinions.TakeDamage) is what actually stops that respawn loop.
        /// </summary>
        public static bool TryGetSpawnerType(int realAshType, out int spawnerType)
        {
            return RealAshTypeToSpawner.TryGetValue(realAshType, out spawnerType);
        }

        /// <summary>
        /// Vanilla summon types that can only ever have exactly ONE living instance, by vanilla's own
        /// design - not something SpiritAshesMinions.OnSpawn can infer from a live-projectile snapshot,
        /// since an ordinary multi-instance summon (Blade Staff's Enchanted Sword) looks IDENTICAL at
        /// spawn time to a single-instance one mid-reposition: both show exactly one live sibling of the
        /// same type. AbigailMinion is the confirmed case - Player.UpdateAbigailStatus force-kills any
        /// second copy on sight every tick, so a live sibling found here is always the same slot being
        /// renewed, never a second one, and OnSpawn can safely inherit from it unconditionally.
        ///
        /// New entries go here as they are found - there is no general way to detect this pattern from a
        /// projectile alone, so this stays an explicit list rather than a heuristic (same reasoning as
        /// SpawnerToRealAshType above).
        /// </summary>
        private static readonly HashSet<int> SingleInstanceAshTypes = new()
        {
            ProjectileID.AbigailMinion,
        };

        /// <summary>True if `projectileType` can only ever have one living instance - see
        /// SingleInstanceAshTypes.</summary>
        public static bool IsSingleInstanceAshType(int projectileType)
        {
            return SingleInstanceAshTypes.Contains(projectileType);
        }

        /// <summary>True when Spirit Ashes should apply to this player: the config toggle is on AND
        /// they are Unkindled or a Bearer of the Curse. Every subsystem in this folder gates on this.</summary>
        public static bool Active(Player player)
        {
            if (player == null || !player.active)
            {
                return false;
            }

            if (!ModContent.GetInstance<tsorcRevampConfig>().SummonerRework)
            {
                return false;
            }

            tsorcRevampPlayer soulsPlayer = player.GetModPlayer<tsorcRevampPlayer>();
            return soulsPlayer.Unkindled || soulsPlayer.BearerOfTheCurse;
        }
    }
}

using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Projectiles; // tsorcGlobalProjectile.SetDefenseTraits

namespace tsorcRevamp.NPCs.Enemies
{
    /// <summary>Which attack the knight is currently running. Stored in NPC.ai[2].</summary>
    internal enum BlackKnightAttack : byte
    {
        None = 0,
        SpearThrow,
        HomingVolley,
        BombThrow,
        BlackRain,
        DeathSeal,
        PlagueAmbush,
        LeapStrike,     // ballistic leap in, spear strike, backward leap, throw at the apex
        CounterCheck,   // proximity-triggered unblockable punish for closing on him
        CurseWard,      // frontal projectile ward — makes his slowness stop mattering
        PlagueSeals,    // placed columns of black death; answers a player who stands still
    }

    /// <summary>
    /// How the knight moves while committing a throw. Ported from RedKnight's RedThrowMovement: the point is
    /// that a thrown weapon does NOT require standing still, so the knight can back off, close, or pop
    /// straight up while releasing it.
    /// </summary>
    internal enum BlackThrowMovement : byte
    {
        None = 0,
        GroundHold,
        RetreatHop,
        AdvanceHop,
        VerticalHop,
    }

    partial class BlackKnight
    {
        // ── Attack state ────────────────────────────────────────────────────────────────────────────────
        // Deliberately stored in NPC.ai[] so it rides the vanilla NPC sync for free rather than needing more
        // SendExtraAI payload:
        //   ai[1] = the general cadence clock. Semantics UNCHANGED from before the bag: the HumanoidMelee
        //           opener still gates on 60..90 (see ConfigureHumanoidMelee's openerCondition) and combo
        //           followups still reset it to 60. It simply no longer doubles as an attack conveyor.
        //   ai[2] = current BlackKnightAttack.
        //   ai[3] = tick counter WITHIN the current attack.
        private BlackKnightAttack CurrentAttack => (BlackKnightAttack)(int)NPC.ai[2];
        private int AttackTimer => (int)NPC.ai[3];
        private bool AttackActive => CurrentAttack != BlackKnightAttack.None;

        // Server-only selection state; never drawn, so it needs no sync.
        private readonly List<BlackKnightAttack> openerBag = new();
        private bool openerBagBelowHalf;
        private BlackThrowMovement throwMovement;
        private int attackCooldown;

        // ── Timings ─────────────────────────────────────────────────────────────────────────────────────
        // Preserved from the conveyor windows they replace, so the feel of each attack is unchanged; only the
        // SELECTION in front of them is new. (Old window -> new duration: spear 120-180 -> 60, homing
        // 265-375 -> 110, bomb 865-925 -> 60, death seal ai[2] 100-246 -> 146.)
        private const int SpearWindupTicks = 60;
        private const int SpearWindupTicksUnblockable = 90; // the shield-bypass variant telegraphs longer
        private const int SpearFlashLead = 25;              // flash this many ticks before release
        private const int SpearRecoveryTicks = 10;

        private const int HomingFirstBurstTick = 60;
        private const int HomingSecondBurstTick = 110;
        private const int HomingRecoveryTicks = 10;

        private const int BombWindupTicks = 60;
        private const int BombFlashLead = 25;
        private const int BombRecoveryTicks = 10;

        private const int BlackRainTicks = 30;

        private const int DeathSealTelegraphTicks = 100;
        private const int DeathSealFlashTick = 65;
        private const int DeathSealFireTicks = 35;
        private const int DeathSealRecoveryTicks = 11;

        private const int PlagueAmbushVanishTick = 12;
        private const int PlagueAmbushTicks = 150;

        // ── LeapStrike ──────────────────────────────────────────────────────────────────────────────────
        // Deliberately TIMER-driven end to end rather than event-driven (e.g. "when he lands"). Clients
        // derive the held-spear draw and the hit window from ai[3] alone, so every phase boundary has to be
        // a tick number both sides can compute. Landing is still detected server-side, but only to damp
        // horizontal drift — never to advance the phase.
        private const int LeapWindupTicks = 18;      // crouch + telegraph before committing
        private const int LeapFlashTick = 8;
        private const int LeapStrikeEndTick = 48;    // outbound arc: hitbox live from LeapWindupTicks
        private const int LeapRecoverTick = 62;      // planted, spear still out
        private const int LeapBackTick = 62;         // backward leap launches
        private const int LeapThrowTick = 78;        // apex of the backward leap -> release
        private const int LeapStrikeTicks = 92;
        private const float LeapOutSpeed = 12.5f;    // ballistic solve speed toward the player
        private const float LeapBackSpeedX = 6.2f;
        private const float LeapBackSpeedY = -8.4f;
        private const float LeapSpearReach = 78f;    // hitbox width during the outbound arc
        private const float LeapSpearHeight = 44f;

        // ── CounterCheck ────────────────────────────────────────────────────────────────────────────────
        // Not a bag attack: proximity-triggered, so closing on him is what causes it.
        // Deliberately short — this is a punish, not a telegraph. The windup is the accelerate-in ramp.
        private const int CounterStrikeTick = 12;
        private const int CounterHitTicks = 12;
        private const int CounterTicks = 44;
        private const float CounterTriggerRange = 112f;
        private const float CounterReach = 86f;
        private const float CounterHeight = 42f;
        // The counter DASHES. Its hitbox is source-anchored (it rides the knight), so a jab thrown from a
        // standing stop only connects if the player is already touching him — which is why it read as "he
        // holds the spear out and nothing happens". Leonhard-style commit: wind up by accelerating in, then
        // burst forward with a small ballistic hop so it has to be dodged rather than walked out of.
        private const float CounterWindupSpeed = 3.4f;
        private const float CounterDashSpeed = 10f;
        private const float CounterDashHop = -3f;
        private const int CounterCooldownTicks = 210;
        private int counterCooldown;

        // ── CurseWard ───────────────────────────────────────────────────────────────────────────────────
        // The answer to "a slow enemy just gets kited". It does not make him faster; it makes range stop
        // working, so the player has to solve him positionally instead of out-DPSing him.
        //
        // The 120-tick facing LOCK is the counterplay and the whole reason this is fair: while warding he
        // physically cannot turn, so flanking beats it. Without the lock a frontal ward is just a damage
        // immunity that follows you around.
        private const int WardFacingLockTicks = 120;
        private const int WardTicks = 152;                 // lock + a visible lower/recover tail
        private const float WardAdvanceSpeed = 1.15f;      // he walks INTO you behind it, slowly
        private const float WardMinRange = 200f;
        private const float WardMaxRange = 900f;
        private int wardFacing;                            // direction he committed to at cast time

        // Sparse rain DURING the lock, so the ward is never 2.5s of dead air. Vertical on purpose: it must
        // not cover his flanks, or the facing lock stops being counterplay. The pressure herds the player
        // sideways, which is exactly where they should be going.
        private const int WardRainTickA = 30;
        private const int WardRainTickB = 80;
        private const int WardRainCount = 2;               // vs 4-6 for the standalone barrage
        private const float WardRainSpread = 34f;
        private const float WardRainRadius = 240f;

        // The ward FEEDS on what it absorbs, and pays it back when it drops. This is what makes shooting
        // into it actively wrong rather than merely wasteful — the wrong play visibly loads his counter.
        private const int WardReleaseBaseCount = 3;
        private const int WardReleaseMaxBonus = 6;
        private const float WardReleaseFullChargeFraction = 0.10f; // absorbed / lifeMax that maxes the bonus
        private float wardAbsorbedDamage;

        /// <summary>Damage the ward has eaten this cast. Added by ModifyHitByProjectile.</summary>
        internal void RegisterWardAbsorption(int damage) => wardAbsorbedDamage += Math.Max(0, damage);

        /// <summary>True while the ward is up and still facing-locked — read by ModifyHitByProjectile.</summary>
        internal bool CurseWardActive => CurrentAttack == BlackKnightAttack.CurseWard
            && AttackTimer < WardFacingLockTicks;

        /// <summary>The side the ward covers. Hits from here are absorbed; anything else is not.</summary>
        internal int CurseWardFacing => wardFacing;

        private const int WardRiseTicks = 16;
        private const int WardFadeTicks = 20;

        /// <summary>
        /// 0..1 bloom of the ward plane: rises quickly on cast, holds through the lock, then collapses as
        /// it drops. Derived from ai[3] so clients animate it without any extra sync.
        /// </summary>
        internal float CurseWardRise
        {
            get
            {
                if (CurrentAttack != BlackKnightAttack.CurseWard)
                {
                    return 0f;
                }
                int t = AttackTimer;
                if (t < WardRiseTicks)
                {
                    return MathHelper.Clamp(t / (float)WardRiseTicks, 0f, 1f);
                }
                if (t < WardFacingLockTicks)
                {
                    return 1f;
                }
                return MathHelper.Clamp(1f - (t - WardFacingLockTicks) / (float)WardFadeTicks, 0f, 1f);
            }
        }

        // ── PlagueSeals ─────────────────────────────────────────────────────────────────────────────────
        // 90, up from 60: this is his slowest, most committed cast and the payoff removes a chunk of the
        // arena, so the read on it should be long and unmistakable. Everything below derives from it —
        // the staff-hold window, the committed-attack window and the charge ramp on the casting dust.
        private const int SealCastTicks = 90;              // caster telegraph before the seals are placed
        private const int SealCastTotalTicks = 114;
        private const int SealFirstTelegraphTicks = 26;    // floor warning on the column under the player
        private const int SealDelayedTelegraphTicks = 180; // 3s, per design — the follow-up pair
        // The delayed pair FLANKS the player, one column each side at this distance. It was previously both
        // columns on the far side 44px apart, which at a 32px column width put them practically touching and
        // read as a single doubled pillar rather than a pincer.
        private const float SealFlankOffset = 500f;
        private const float SealMinRange = 180f;
        private const float SealMaxRange = 950f;

        /// <summary>Staff is raised: drives the held-staff draw and its casting dust.</summary>
        internal bool CastingStaff => CurrentAttack == BlackKnightAttack.PlagueSeals
            && AttackTimer < SealCastTicks + 12;

        // Held 45 degrees up from the grip, per design. Length is the distance from grip to staff head,
        // used both to place the casting dust and to draw the sprite.
        internal const float StaffAngleFromHorizontal = MathHelper.PiOver4;
        internal const float StaffLength = 34f;

        /// <summary>World position of the staff's head (the gem end), 45 degrees up from the hand.</summary>
        internal Vector2 StaffHeadWorld()
        {
            Vector2 grip = CurrentHandWorld();
            return grip + new Vector2(NPC.spriteDirection, 0f).RotatedBy(-StaffAngleFromHorizontal * NPC.spriteDirection) * StaffLength;
        }

        // ── Plague trail ────────────────────────────────────────────────────────────────────────────────
        // The fix for "kiting reads as skittish": ground he gives up becomes ground you cannot take.
        // Reuses PlagueTeleportCloud, which is stationary, deals NO damage, and applies stacking
        // CurseBuildup — denial pressure rather than a punishing damage puddle.
        private const int PlagueTrailCooldownTicks = 45;
        private const float PlagueTrailRadius = 52f;
        private const int PlagueTrailLifetime = 150;
        private const float PlagueTrailMinRetreatSpeed = 0.9f;
        private int plagueTrailCooldown;

        /// <summary>Spear is out: the LeapStrike arc through to the throw, plus the counter jab.</summary>
        public bool SpearMeleeActive => (CurrentAttack == BlackKnightAttack.LeapStrike && AttackTimer < LeapThrowTick)
            || CurrentAttack == BlackKnightAttack.CounterCheck;

        /// <summary>Strike is live and should deal damage.</summary>
        public bool SpearMeleeHitWindow
        {
            get
            {
                int t = AttackTimer;
                return CurrentAttack switch
                {
                    BlackKnightAttack.LeapStrike => t >= LeapWindupTicks && t < LeapStrikeEndTick,
                    BlackKnightAttack.CounterCheck => t >= CounterStrikeTick && t < CounterStrikeTick + CounterHitTicks,
                    _ => false,
                };
            }
        }

        /// <summary>Cooldown between OPENERS. Combo followups bypass this entirely — they are paced by
        /// CombatTempo's own continue-chance, which is the system that makes strings feel adaptive.</summary>
        private const int OpenerCooldownTicks = 45;
        private const int OpenerFailCooldownTicks = 20;

        // ── Spacing duelist tuning ──────────────────────────────────────────────────────────────────────
        // BlackKnight kites (KiteRangeMin 2 / Max 15 tiles). These bands partition the bag around that band
        // so the kiting reads as a deliberate preferred engagement range rather than as him being skittish:
        // he wants to sit mid-range, punishes anything that closes, and answers a runaway with rain/seal.
        // Kept in step with the CombatComboMove bands in SetDefaults, so an attack cannot be a legal opener
        // at a range where it would be an illegal followup (which read as the knight opening with something
        // it then immediately refused to chain).
        private const float SpearMinRange = 120f;
        private const float HomingMinRange = 300f;
        private const float HomingMaxRange = 900f;
        private const float BombMinRange = 190f;
        private const float BombMaxRange = 620f;
        private const float BlackRainMinRange = 250f;   // matches the old ai[2] gate
        private const float DeathSealMaxRange = 900f;
        private const float PlagueAmbushMinRange = 260f; // only worth vanishing if there is ground to cover
        private const float PlagueAmbushMaxRange = 1100f;
        // LeapStrike deliberately overlaps the band the spear throw owns. That overlap is the point: at
        // mid range the player should not know whether the answer is a thrown spear or him closing it
        // himself, which is what stops the spacing reading as a safe, predictable wall.
        private const float LeapMinRange = 150f;
        private const float LeapMaxRange = 430f;

        /// <summary>Human-readable current attack, for the above-head debug readout.</summary>
        internal string CurrentAttackDebugName
        {
            get
            {
                if (!AttackActive)
                {
                    return null;
                }
                string movement = throwMovement switch
                {
                    BlackThrowMovement.RetreatHop => " — Retreat Hop",
                    BlackThrowMovement.AdvanceHop => " — Advance Hop",
                    BlackThrowMovement.VerticalHop => " — Vertical Hop",
                    _ => string.Empty,
                };
                return CurrentAttack switch
                {
                    BlackKnightAttack.SpearThrow => $"Spear Throw{movement}",
                    BlackKnightAttack.HomingVolley => "Homing Volley",
                    BlackKnightAttack.BombThrow => $"Bomb Throw{movement}",
                    BlackKnightAttack.BlackRain => "Black Rain",
                    BlackKnightAttack.DeathSeal => "Death Seal",
                    BlackKnightAttack.PlagueAmbush => "Plague Ambush",
                    BlackKnightAttack.LeapStrike => AttackTimer >= LeapBackTick ? "Leap Strike — Backleap Throw" : "Leap Strike",
                    BlackKnightAttack.CounterCheck => "Counter Check",
                    BlackKnightAttack.CurseWard => "Curse Ward",
                    BlackKnightAttack.PlagueSeals => "Plague Seals",
                    _ => null,
                };
            }
        }

        /// <summary>Total ticks this attack runs for, including its recovery tail.</summary>
        private int AttackDuration(BlackKnightAttack attack, tsorcRevampGlobalNPC globalNPC) => attack switch
        {
            BlackKnightAttack.SpearThrow => SpearWindup(globalNPC) + SpearRecoveryTicks,
            BlackKnightAttack.HomingVolley => HomingSecondBurstTick + HomingRecoveryTicks,
            BlackKnightAttack.BombThrow => BombWindupTicks + BombRecoveryTicks,
            BlackKnightAttack.BlackRain => BlackRainTicks,
            BlackKnightAttack.DeathSeal => DeathSealTelegraphTicks + DeathSealFireTicks + DeathSealRecoveryTicks,
            BlackKnightAttack.PlagueAmbush => PlagueAmbushTicks,
            BlackKnightAttack.LeapStrike => LeapStrikeTicks,
            BlackKnightAttack.CounterCheck => CounterTicks,
            BlackKnightAttack.CurseWard => WardTicks,
            BlackKnightAttack.PlagueSeals => SealCastTotalTicks,
            _ => 1,
        };

        private int SpearWindup(tsorcRevampGlobalNPC globalNPC)
            => globalNPC.ActiveAttackBypassesShield ? SpearWindupTicksUnblockable : SpearWindupTicks;

        /// <summary>True while the attack is committed enough to be hyper-armoured and to survive a teleport
        /// or dodge trying to seize the body. Replaces the old hardcoded inProtectedAttack tick ranges.</summary>
        internal bool InCommittedAttack(tsorcRevampGlobalNPC globalNPC)
        {
            int t = AttackTimer;
            return CurrentAttack switch
            {
                BlackKnightAttack.SpearThrow => t >= SpearWindup(globalNPC) - SpearFlashLead,
                BlackKnightAttack.HomingVolley => t >= HomingFirstBurstTick,
                BlackKnightAttack.BombThrow => t >= BombWindupTicks - BombFlashLead,
                BlackKnightAttack.DeathSeal => t >= DeathSealFlashTick,
                BlackKnightAttack.PlagueAmbush => true, // a teleport in flight must never be cancelled
                // Hyper-armoured from launch: an airborne leap cannot be staggered mid-arc without
                // dumping him on the floor in a broken pose, and the whole point of the move is that
                // committing to it is safe for him and dangerous for you.
                BlackKnightAttack.LeapStrike => t >= LeapWindupTicks,
                BlackKnightAttack.CounterCheck => t >= CounterStrikeTick,
                // The ward IS the commitment: he is locked facing one way and cannot be shoved out of it,
                // which is exactly what makes flanking the answer.
                BlackKnightAttack.CurseWard => t < WardFacingLockTicks,
                BlackKnightAttack.PlagueSeals => t >= SealCastTicks - 20,
                _ => false,
            };
        }

        /// <summary>True during the readable windup, before the attack is locked in.</summary>
        internal bool InAttackTelegraph(tsorcRevampGlobalNPC globalNPC)
            => AttackActive && !InCommittedAttack(globalNPC);

        // ── Selection ───────────────────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Picks and starts an OPENER from the bag. Followups are NOT chosen here — those still go through
        /// CombatTempo's weighted, damage-urgency-scaled chooser (TryQueueComboFollowup), which is the system
        /// that gives BlackKnight its adaptive combo strings. The bag exists only to fix the opener, which
        /// used to be a fixed spear -> homing -> bomb rotation baked into the conveyor's tick numbers.
        /// </summary>
        private void TryStartOpener(tsorcRevampGlobalNPC globalNPC, bool hasPlayerLOS)
        {
            if (attackCooldown > 0)
            {
                attackCooldown--;
                return;
            }
            if (!CanStartAttack(globalNPC))
            {
                return;
            }

            float distance = NPC.Distance(player.Center);
            bool belowHalf = NPC.life <= NPC.lifeMax / 2;

            Span<BlackKnightAttack> candidates = stackalloc BlackKnightAttack[8];
            int count = 0;
            if (hasPlayerLOS && distance >= SpearMinRange)
            {
                candidates[count++] = BlackKnightAttack.SpearThrow;
            }
            if (hasPlayerLOS && distance >= HomingMinRange && distance <= HomingMaxRange)
            {
                candidates[count++] = BlackKnightAttack.HomingVolley;
            }
            if (hasPlayerLOS && distance >= BombMinRange && distance <= BombMaxRange)
            {
                candidates[count++] = BlackKnightAttack.BombThrow;
            }
            if (hasPlayerLOS && distance >= BlackRainMinRange)
            {
                candidates[count++] = BlackKnightAttack.BlackRain;
            }
            if (belowHalf && hasPlayerLOS && distance <= DeathSealMaxRange)
            {
                candidates[count++] = BlackKnightAttack.DeathSeal;
            }
            // The ambush deliberately does NOT require line of sight — closing a broken sight line is the
            // entire point of it, and it is the knight's answer to a player who has run behind cover.
            if (belowHalf && globalNPC.TeleportCooldownTimer <= 0
                && distance >= PlagueAmbushMinRange && distance <= PlagueAmbushMaxRange)
            {
                candidates[count++] = BlackKnightAttack.PlagueAmbush;
            }

            // The gap-closer: needs room to actually arc, and a clear line so he doesn't leap into a wall.
            if (hasPlayerLOS && distance >= LeapMinRange && distance <= LeapMaxRange
                && MathF.Abs(player.Center.Y - NPC.Center.Y) <= 96f)
            {
                candidates[count++] = BlackKnightAttack.LeapStrike;
            }

            // Ward wants range to actually cover ground behind it, and a target to walk at.
            if (hasPlayerLOS && distance >= WardMinRange && distance <= WardMaxRange)
            {
                candidates[count++] = BlackKnightAttack.CurseWard;
            }
            // Seals are placed, not aimed, so they do NOT need line of sight — sealing the floor around
            // someone hiding behind cover is exactly what they are for.
            if (distance >= SealMinRange && distance <= SealMaxRange)
            {
                candidates[count++] = BlackKnightAttack.PlagueSeals;
            }

            if (count == 0)
            {
                attackCooldown = OpenerFailCooldownTicks;
                return;
            }

            BeginAttack(ChooseFromBag(candidates[..count], belowHalf), globalNPC);
        }

        /// <summary>
        /// Draws one token from the opener bag, restricted to what is currently in range. Bag semantics (draw
        /// without replacement, refill when empty) are the actual upgrade over the old fixed rotation: every
        /// attack is guaranteed an outing before any repeats, instead of the cycle always running in the same
        /// order or a pure weighted roll letting one attack dominate a stretch of the fight.
        /// </summary>
        private BlackKnightAttack ChooseFromBag(ReadOnlySpan<BlackKnightAttack> candidates, bool belowHalf)
        {
            if (openerBag.Count == 0 || openerBagBelowHalf != belowHalf)
            {
                FillOpenerBag(belowHalf);
            }

            Span<BlackKnightAttack> eligible = stackalloc BlackKnightAttack[candidates.Length];
            int eligibleCount = CollectBagEligible(candidates, eligible);
            if (eligibleCount == 0)
            {
                // Everything in range has already been drawn this cycle — refill rather than stall, which is
                // what keeps a cornered knight (only one or two attacks ever in range) still attacking.
                FillOpenerBag(belowHalf);
                eligibleCount = CollectBagEligible(candidates, eligible);
            }
            if (eligibleCount == 0)
            {
                return candidates[Main.rand.Next(candidates.Length)];
            }

            BlackKnightAttack selected = eligible[Main.rand.Next(eligibleCount)];
            openerBag.Remove(selected);
            return selected;
        }

        private int CollectBagEligible(ReadOnlySpan<BlackKnightAttack> candidates, Span<BlackKnightAttack> destination)
        {
            int result = 0;
            for (int i = 0; i < candidates.Length; i++)
            {
                if (openerBag.Contains(candidates[i]))
                {
                    destination[result++] = candidates[i];
                }
            }
            return result;
        }

        private void FillOpenerBag(bool belowHalf)
        {
            openerBag.Clear();
            openerBagBelowHalf = belowHalf;
            openerBag.Add(BlackKnightAttack.SpearThrow);
            openerBag.Add(BlackKnightAttack.HomingVolley);
            openerBag.Add(BlackKnightAttack.BombThrow);
            openerBag.Add(BlackKnightAttack.BlackRain);
            openerBag.Add(BlackKnightAttack.LeapStrike);
            openerBag.Add(BlackKnightAttack.PlagueSeals);
            openerBag.Add(BlackKnightAttack.CurseWard);
            if (belowHalf)
            {
                openerBag.Add(BlackKnightAttack.DeathSeal);
                openerBag.Add(BlackKnightAttack.PlagueAmbush);
                // A second spear token below half: the throw is his bread-and-butter spacing tool and the
                // bag would otherwise dilute it exactly when the fight gets busiest.
                openerBag.Add(BlackKnightAttack.SpearThrow);
            }
        }

        /// <summary>Shared gate for starting anything — mirrors RedKnightAttackController.CanStart.</summary>
        private bool CanStartAttack(tsorcRevampGlobalNPC globalNPC)
        {
            return Main.netMode != NetmodeID.MultiplayerClient
                && !AttackActive
                && player.active && !player.dead
                && NPC.velocity.Y == 0f
                && !globalNPC.CombatMeleeActive
                && !globalNPC.HasPendingCombatComboMove
                && !globalNPC.InCombatComboRecovery
                && globalNPC.StaggerTimer <= 0
                && globalNPC.TeleportCountdown <= 0
                && globalNPC.TeleportAppearanceTimer <= 0
                && globalNPC.DodgeTimer <= 0
                && globalNPC.DodgeRecoveryTimer <= 0
                && globalNPC.PounceTimer <= 0
                && !globalNPC.Fleeing;
        }

        /// <summary>
        /// Starts an attack. Also reachable from the combo system: a queued CombatComboMove maps to an attack
        /// and lands here, which is what replaced the old "set ai[1] = 124/269/869 to address an attack".
        /// </summary>
        private void BeginAttack(BlackKnightAttack attack, tsorcRevampGlobalNPC globalNPC)
        {
            NPC.ai[2] = (float)attack;
            NPC.ai[3] = 0f;
            NPC.TargetClosest(true);
            int facing = player.Center.X >= NPC.Center.X ? 1 : -1;
            NPC.direction = facing;
            NPC.spriteDirection = facing;

            throwMovement = attack is BlackKnightAttack.SpearThrow or BlackKnightAttack.BombThrow
                ? ChooseThrowMovement(facing)
                : BlackThrowMovement.None;

            // The ward locks to whichever way he was facing when it went up; it must NOT track the player.
            wardFacing = facing;
            if (attack == BlackKnightAttack.CurseWard)
            {
                wardAbsorbedDamage = 0f; // each cast is charged only by what IT absorbs
            }

            if (attack != BlackKnightAttack.DeathSeal)
            {
                storedPlayerPosition = player.Center;
                framesSinceStoredPosition = 0;
            }
            NPC.netUpdate = true;
        }

        /// <summary>
        /// Poise stagger: cancel whatever attack was winding up and drop back to neutral.
        /// </summary>
        /// <remarks>
        /// Replaces the generic PoiseStaggerResetsAI path, which sets ai[1]=60 / ai[2]=-100. That was correct
        /// while ai[2] was a free-running timer, but it now holds the attack ENUM — -100 is not a valid
        /// BlackKnightAttack, so the knight would spend a tick in a garbage attack state that draws no held
        /// weapon and matches no case in the pump.
        ///
        /// Only ever reached from a windup: a committed attack is hyper-armoured and cannot be staggered at
        /// all (GlobalNPC gates poise accumulation and stagger on AttackCommitted), so cancelling
        /// unconditionally here is safe.
        /// </remarks>
        public void OnStagger(NPC npc)
        {
            npc.ai[1] = 60f; // back to the neutral cadence window the melee opener reads
            EndAttack(cooldown: OpenerFailCooldownTicks);
            npc.GetGlobalNPC<tsorcRevampGlobalNPC>().ResetCombatTempoSequence(clearRecovery: true);
        }

        /// <summary>
        /// Cancels the current attack from OUTSIDE the pump — a teleport, dodge, pounce, flee or patrol
        /// seizing the body. Only ever called after InCommittedAttack has been checked, so this cannot
        /// interrupt a committed attack.
        /// </summary>
        internal void EndAttackExternally()
        {
            if (AttackActive)
            {
                EndAttack(cooldown: OpenerFailCooldownTicks);
            }
        }

        private void EndAttack(int cooldown = OpenerCooldownTicks)
        {
            NPC.ai[2] = (float)BlackKnightAttack.None;
            NPC.ai[3] = 0f;
            throwMovement = BlackThrowMovement.None;
            attackCooldown = cooldown;
            NPC.netUpdate = true;
        }

        // ── Throw hop movement (ported from RedKnight via KnightHopPlanner) ─────────────────────────────
        private const float ThrowHopSpeedY = 5.2f;
        private const float ThrowHopGravity = 0.35f;
        private const float ThrowRetreatSpeed = 3.2f;
        private const float ThrowAdvanceSpeed = 3f;
        private const float ThrowApproachSpeed = 1.4f;
        private const float ThrowRecoverySpeed = 2f;
        private const float ThrowAcceleration = 0.1f;
        // Red Knight biases retreat at 0.72. BlackKnight ALSO kites (KiteRangeMin/Max), so at 0.72 the two
        // compound and he never closes — pulled down so the hop is a flourish on top of the kite, not a
        // second independent reason to back off.
        private const float RetreatHopChance = 0.45f;
        private const float AdvanceHopChance = 0.72f;

        private BlackThrowMovement ChooseThrowMovement(int facing)
        {
            float signedDistance = (player.Center.X - NPC.Center.X) * facing;
            bool canRetreat = KnightHopPlanner.HasSafeHop(NPC, player.Center, facing, -facing,
                ThrowRetreatSpeed, advancing: false, ThrowHopSpeedY, ThrowHopGravity);
            bool canAdvance = KnightHopPlanner.HasSafeHop(NPC, player.Center, facing, facing,
                ThrowAdvanceSpeed, advancing: true, ThrowHopSpeedY, ThrowHopGravity);
            bool canRise = KnightHopPlanner.HasSafeHop(NPC, player.Center, facing, facing, 0.8f,
                advancing: false, ThrowHopSpeedY, ThrowHopGravity);

            if (signedDistance < 240f && canRetreat && Main.rand.NextFloat() < RetreatHopChance)
            {
                return BlackThrowMovement.RetreatHop;
            }
            if (signedDistance > 360f && canAdvance && Main.rand.NextFloat() < AdvanceHopChance)
            {
                return BlackThrowMovement.AdvanceHop;
            }

            return Main.rand.Next(4) switch
            {
                0 when canRise => BlackThrowMovement.VerticalHop,
                1 when canRetreat => BlackThrowMovement.RetreatHop,
                2 when canAdvance => BlackThrowMovement.AdvanceHop,
                _ => BlackThrowMovement.GroundHold,
            };
        }

        /// <summary>
        /// Drives the body through a throw. Air control is intentionally absent after takeoff: the hop is a
        /// commitment, so a knight that lands early keeps coasting rather than snapping around mid-throw.
        /// </summary>
        private void RunThrowMovement(int hopTick, int releaseTick)
        {
            if (throwMovement is BlackThrowMovement.None or BlackThrowMovement.GroundHold)
            {
                KnightHopPlanner.ApproachHorizontalSpeed(NPC, NPC.direction,
                    AttackTimer < releaseTick ? ThrowApproachSpeed : ThrowRecoverySpeed, ThrowAcceleration);
                return;
            }

            int travelDirection = throwMovement == BlackThrowMovement.RetreatHop ? -NPC.direction : NPC.direction;
            float travelSpeed = throwMovement switch
            {
                BlackThrowMovement.RetreatHop => ThrowRetreatSpeed,
                BlackThrowMovement.AdvanceHop => ThrowAdvanceSpeed,
                _ => 0.8f,
            };

            if (AttackTimer < hopTick)
            {
                KnightHopPlanner.ApproachHorizontalSpeed(NPC, NPC.direction, ThrowApproachSpeed, ThrowAcceleration);
                return;
            }
            if (AttackTimer == hopTick)
            {
                // Re-validate at takeoff: the knight has moved since the hop was chosen, so the ground it was
                // planned against may no longer be the ground under its feet.
                if (NPC.velocity.Y != 0f || !KnightHopPlanner.HasSafeHop(NPC, player.Center, NPC.direction,
                        travelDirection, travelSpeed, throwMovement == BlackThrowMovement.AdvanceHop,
                        ThrowHopSpeedY, ThrowHopGravity))
                {
                    throwMovement = BlackThrowMovement.GroundHold;
                    KnightHopPlanner.ApproachHorizontalSpeed(NPC, NPC.direction, ThrowApproachSpeed, ThrowAcceleration);
                    NPC.netUpdate = true;
                    return;
                }

                NPC.velocity = new Vector2(travelDirection * travelSpeed, -ThrowHopSpeedY);
                NPC.netUpdate = true;
                return;
            }
            if (AttackTimer <= releaseTick)
            {
                if (NPC.velocity.Y == 0f)
                {
                    KnightHopPlanner.ApproachHorizontalSpeed(NPC, travelDirection,
                        Math.Max(0.8f, travelSpeed * 0.5f), ThrowAcceleration * 0.5f);
                }
                return;
            }
            KnightHopPlanner.ApproachHorizontalSpeed(NPC, NPC.direction, ThrowRecoverySpeed, ThrowAcceleration);
        }

        // ── Execution ───────────────────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Server-side attack pump. Replaces the old conveyor: instead of every attack watching a free-running
        /// ai[1]/ai[2] for its magic tick number, exactly one attack is active at a time and owns its own
        /// zero-based clock in ai[3].
        /// </summary>
        internal void TickAttacks(tsorcRevampGlobalNPC globalNPC, bool hasPlayerLOS)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            if (counterCooldown > 0)
            {
                counterCooldown--;
            }
            RunPlagueTrail(globalNPC);
            RunGravefallWaves();

            if (!AttackActive)
            {
                // The counter is checked BEFORE the bag and ignores the opener cooldown: it is not one of
                // his choices, it is what happens to you for stepping inside his guard.
                if (TryStartCounter(globalNPC))
                {
                    return;
                }
                TryStartOpener(globalNPC, hasPlayerLOS);
                return;
            }

            // A stagger cancels a windup but never a committed attack — same carve-out the old
            // inProtectedAttack ranges expressed, now asked of the state machine instead of hardcoded ticks.
            if (!player.active || player.dead
                || (globalNPC.StaggerTimer > 0 && !InCommittedAttack(globalNPC)))
            {
                globalNPC.EndCombatTempoSequenceWithoutFollowup(NPC);
                EndAttack();
                return;
            }

            int t = AttackTimer;
            switch (CurrentAttack)
            {
                case BlackKnightAttack.SpearThrow: TickSpearThrow(t, globalNPC, hasPlayerLOS); break;
                case BlackKnightAttack.HomingVolley: TickHomingVolley(t, globalNPC, hasPlayerLOS); break;
                case BlackKnightAttack.BombThrow: TickBombThrow(t, globalNPC, hasPlayerLOS); break;
                case BlackKnightAttack.BlackRain: TickBlackRain(t); break;
                case BlackKnightAttack.DeathSeal: TickDeathSeal(t, globalNPC); break;
                case BlackKnightAttack.PlagueAmbush: TickPlagueAmbush(t, globalNPC); break;
                case BlackKnightAttack.LeapStrike: TickLeapStrike(t, globalNPC); break;
                case BlackKnightAttack.CounterCheck: TickCounterCheck(t, globalNPC); break;
                case BlackKnightAttack.CurseWard: TickCurseWard(t, globalNPC); break;
                case BlackKnightAttack.PlagueSeals: TickPlagueSeals(t, globalNPC); break;
            }

            if (!AttackActive)
            {
                return; // the body already finished it (fired, or abandoned on lost line of sight)
            }

            NPC.ai[3] = t + 1;
            if (AttackTimer >= AttackDuration(CurrentAttack, globalNPC))
            {
                EndAttack();
            }
        }

        /// <summary>Aim point: the ~25-tick-old predicted position, nudged 10px past the player so the arc
        /// lands on them rather than short. Unchanged from the conveyor version.</summary>
        private Vector2 PredictedAimPoint()
        {
            int direction = storedPlayerPosition.X > NPC.Center.X ? 1 : -1;
            return new Vector2(storedPlayerPosition.X + 10f * direction, storedPlayerPosition.Y);
        }

        private void SpawnAttackFlash(Color color)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }
            Vector2 spawnPosition = NPC.position;
            if (NPC.direction == 1)
            {
                spawnPosition.X += NPC.width;
            }
            Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), spawnPosition, NPC.velocity,
                ModContent.ProjectileType<Projectiles.VFX.TelegraphFlash>(), 0, 0, Main.myPlayer,
                UsefulFunctions.ColorToFloat(color));
        }

        /// <summary>Refreshes the predicted aim point mid-windup, on the same 25-tick cadence as before.</summary>
        private void RefreshStoredPosition()
        {
            if (framesSinceStoredPosition < 25 || !player.active || player.dead)
            {
                return;
            }
            framesSinceStoredPosition = 0;
            storedPlayerPosition = player.Center;
            NPC.netUpdate = true; // SendExtraAI only rides along with a netUpdate; clients draw against this
        }

        private void TickSpearThrow(int t, tsorcRevampGlobalNPC globalNPC, bool hasPlayerLOS)
        {
            int windup = SpearWindup(globalNPC);
            RunThrowMovement(windup - SpearFlashLead, windup);

            if (t >= windup - SpearFlashLead)
            {
                NPC.knockBackResist = 0f;
            }
            if (t == windup - SpearFlashLead)
            {
                SpawnAttackFlash(globalNPC.ActiveAttackBypassesShield ? Color.Red : Color.OrangeRed);
                RefreshStoredPosition();
            }
            if (t != windup)
            {
                return;
            }

            if (!hasPlayerLOS)
            {
                globalNPC.EndCombatTempoSequenceWithoutFollowup(NPC);
                EndAttack();
                return;
            }

            NPC.TargetClosest(true);
            // Close throws are slower so they arc more visibly; far throws are flatter and faster.
            float speed = NPC.Distance(player.Center) > 400f
                ? Main.rand.NextFloat(16f, 18f)
                : Main.rand.NextFloat(12f, 14f);
            Vector2 velocity = UsefulFunctions.BallisticTrajectory(NPC.Center, PredictedAimPoint(), speed, fallback: true)
                + player.velocity;
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                SpawnSpearProjectile(velocity, globalNPC);
            }
            SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.8f, PitchVariance = 0.1f }, NPC.Center);
            CompleteSpearAttack(globalNPC);
            if (globalNPC.HasPendingCombatComboMove)
            {
                EndAttack(cooldown: 0); // a followup owns the pacing from here
            }
        }

        private void TickHomingVolley(int t, tsorcRevampGlobalNPC globalNPC, bool hasPlayerLOS)
        {
            if (t <= HomingFirstBurstTick && Main.rand.NextBool(2))
            {
                int dust = Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.ShadowbeamStaff,
                    NPC.velocity.X - 6f, NPC.velocity.Y, 6, Color.DarkSlateGray, 0.5f);
                Main.dust[dust].noGravity = true;
            }
            if (t == HomingFirstBurstTick - 25 || t == HomingSecondBurstTick - 25)
            {
                SpawnAttackFlash(Color.Orange);
            }

            bool firstBurst = t == HomingFirstBurstTick;
            bool secondBurst = t == HomingSecondBurstTick;
            if (!firstBurst && !secondBurst)
            {
                return;
            }
            if (!hasPlayerLOS)
            {
                globalNPC.EndCombatTempoSequenceWithoutFollowup(NPC);
                EndAttack();
                return;
            }

            // Burst 1 is a single lofted crystal; burst 2 is a faster, flatter pair.
            int projectiles = firstBurst ? 1 : 2;
            float projectileSpeed = firstBurst ? 15f : 18f;
            float arc = firstBurst ? 2.1f : 1.1f;
            const float spread = MathHelper.Pi / 6f;
            for (int i = 0; i < projectiles; i++)
            {
                float angle = i * spread - spread * (projectiles - 1) / 2f;
                if (angle > MathHelper.PiOver2)
                {
                    angle = MathHelper.Pi - angle;
                }
                Vector2 velocity = UsefulFunctions.BallisticTrajectory(NPC.Center, player.Center,
                    projectileSpeed, arc, highAngle: true, fallback: true);
                if (firstBurst)
                {
                    velocity += player.velocity;
                }
                velocity = velocity.RotatedBy(angle);
                // Never fire backwards through the knight's own body.
                if (Math.Sign(velocity.X) != NPC.direction)
                {
                    continue;
                }
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, velocity,
                        ModContent.ProjectileType<Projectiles.Enemy.EnemyBlackKnightHomingCrystal>(),
                        redMagicDamage, 0f, Main.myPlayer);
                }
            }

            if (secondBurst)
            {
                TryQueueComboFollowup(globalNPC, HomingComboMoveKey);
                if (globalNPC.HasPendingCombatComboMove)
                {
                    EndAttack(cooldown: 0);
                }
            }
        }

        private void TickBombThrow(int t, tsorcRevampGlobalNPC globalNPC, bool hasPlayerLOS)
        {
            RunThrowMovement(BombWindupTicks - BombFlashLead, BombWindupTicks);

            if (t >= BombWindupTicks - BombFlashLead)
            {
                NPC.knockBackResist = 0f;
            }
            if (t == BombWindupTicks - BombFlashLead)
            {
                SoundEngine.PlaySound(UsefulFunctions.BombFuse with { Volume = 0.6f }, NPC.Center);
                SpawnAttackFlash(Color.OrangeRed);
                Lighting.AddLight(NPC.Center, Color.OrangeRed.ToVector3() * 3f);
                RefreshStoredPosition();
            }
            if (t != BombWindupTicks)
            {
                return;
            }

            if (!hasPlayerLOS)
            {
                globalNPC.EndCombatTempoSequenceWithoutFollowup(NPC);
                EndAttack();
                return;
            }

            bool far = NPC.Distance(player.Center) > 400f;
            Vector2 velocity = UsefulFunctions.BallisticTrajectory(NPC.Center, PredictedAimPoint(),
                far ? 8f : 5f, fallback: true);
            if (far)
            {
                velocity += player.velocity;
            }
            else
            {
                velocity.Y += Main.rand.NextFloat(-1f, -2f); // a little extra loft on the short lob
            }
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, velocity,
                    ModContent.ProjectileType<Projectiles.Enemy.EnemyMoonfuryBomb>(),
                    redKnightsSpearDamage, 0f, Main.myPlayer);
            }
            SoundEngine.PlaySound(SoundID.Item1 with { Volume = 1f, Pitch = -0.5f }, NPC.Center);
            TryQueueComboFollowup(globalNPC, BombComboMoveKey);
            if (globalNPC.HasPendingCombatComboMove)
            {
                EndAttack(cooldown: 0);
            }
        }

        /// <summary>
        /// Advances the 2-3 wave barrage StartGravefallBarrage schedules.
        /// </summary>
        /// <remarks>
        /// Runs OUTSIDE the attack state machine on purpose: waves are GravefallWaveCooldown (90t) apart but
        /// the BlackRain attack itself is only 30t, so tying this to the attack would silently drop waves 2
        /// and 3. It previously lived in the ai[2] conveyor block and was lost when that was spliced out —
        /// caught by the resulting "gravefallWaveTimer assigned but never used" warning.
        /// </remarks>
        private void RunGravefallWaves()
        {
            if (gravefallWavesRemaining <= 0)
            {
                return;
            }
            gravefallWaveTimer++;
            if (gravefallWaveTimer < GravefallWaveCooldown)
            {
                return;
            }
            gravefallWaveTimer = 0;
            gravefallWaveIndex++;
            gravefallWavesRemaining--;
            FireGravefallWave();
            SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.5f, Pitch = -0.01f }, NPC.Center);
        }

        private void TickBlackRain(int t)
        {
            if (t != 0)
            {
                return;
            }
            // Wide waves are the bigger, more spread-out variant the old ai[2] == 97/547/597 ticks fired.
            StartGravefallBarrage(Main.rand.NextBool());
            SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.5f, Pitch = -0.01f }, NPC.Center);
            NPC.netUpdate = true;
        }

        private void TickDeathSeal(int t, tsorcRevampGlobalNPC globalNPC)
        {
            if (t == 0)
            {
                globalNPC.ResetCombatTempoSequence(clearRecovery: true);
            }

            if (t < DeathSealTelegraphTicks)
            {
                NPC.knockBackResist = 0f;
                NPC.velocity.X *= 0.85f;
                if (Main.rand.NextBool(4))
                {
                    // Ring of motes collapsing inward, so the telegraph reads as power gathering.
                    float collapse = MathHelper.Clamp(1f - t / (float)DeathSealTelegraphTicks, 0f, 1f);
                    Vector2 offset = Main.rand.NextVector2CircularEdge(240f, 240f) * collapse;
                    Dust mote = Dust.NewDustPerfect(NPC.Center + offset, DustID.BoneTorch,
                        -offset * 0.025f, 120, default, 0.9f);
                    mote.noGravity = true;
                }
                Lighting.AddLight(NPC.Center, Color.WhiteSmoke.ToVector3() * 1.2f);

                if (t == DeathSealFlashTick)
                {
                    SpawnAttackFlash(Color.OrangeRed);
                    RefreshStoredPosition();
                }
                return;
            }

            if (t >= DeathSealTelegraphTicks + DeathSealFireTicks)
            {
                return; // recovery tail
            }

            NPC.velocity.X *= 0.25f;
            Vector2 aim = PredictedAimPoint();
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                Vector2 skullVelocity = UsefulFunctions.BallisticTrajectory(NPC.Center, aim, 2f, fallback: true)
                    + Main.rand.NextVector2Circular(1, 5);
                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, skullVelocity,
                    ModContent.ProjectileType<Projectiles.Enemy.EnemySpellSuddenDeathStrike>(),
                    redKnightsGreatDamage, 0f, Main.myPlayer, 1f);

                Vector2 breathVelocity = UsefulFunctions.BallisticTrajectory(NPC.Center, aim, 2f, fallback: true)
                    + Main.rand.NextVector2Circular(-5, 5);
                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, breathVelocity,
                    ModContent.ProjectileType<Projectiles.Enemy.EnemyBlackCursedBreath>(),
                    redKnightsGreatDamage, 0f, Main.myPlayer, 2f);
            }
            SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.8f, PitchVariance = 1f }, NPC.Center);
            SoundEngine.PlaySound(SoundID.Item69 with { Volume = 0.9f, PitchVariance = 2f }, NPC.Center);
            NPC.netUpdate = true;
        }

        /// <summary>
        /// The signature move, and what most separates this knight from the Red family: rather than teleport
        /// only as a reactive escape (TryTeleportReacquire), he chooses to vanish OFFENSIVELY, reappear on top
        /// of the player, and immediately buy a melee punish out of the arrival.
        /// </summary>
        private void TickPlagueAmbush(int t, tsorcRevampGlobalNPC globalNPC)
        {
            if (t < PlagueAmbushVanishTick)
            {
                // Brief tell before the vanish so it is reactable rather than instant.
                NPC.velocity.X *= 0.8f;
                if (Main.rand.NextBool(2))
                {
                    Vector2 offset = Main.rand.NextVector2Circular(28f, 28f);
                    Dust mote = Dust.NewDustPerfect(NPC.Center + offset, DustID.PurpleTorch,
                        -offset * 0.08f, 150, new Color(120, 45, 170), 1.1f);
                    mote.noGravity = true;
                }
                return;
            }

            if (t == PlagueAmbushVanishTick)
            {
                // Reuses the shared plague teleport wholesale, including the retimed clouds — he emerges with
                // ~2s of cloud still billowing, which is exactly the cover an ambush wants.
                tsorcRevampAIs.QueueTeleport(NPC, 50, requireLineofSight: false,
                    globalNPC.TeleportTelegraphTime, globalNPC.PrefersHighGround, minRange: 5);
                return;
            }

            // Wait out the vanish. If the teleport never took (no valid destination), just abandon rather
            // than stand invisible doing nothing.
            if (globalNPC.TeleportCountdown > 0 || globalNPC.TeleportAppearanceTimer > 0)
            {
                return;
            }
            if (t < PlagueAmbushVanishTick + 4)
            {
                return; // give QueueTeleport a frame to take effect before concluding it failed
            }

            // Arrived. Cash the ambush in for an immediate melee punish if one is available at this range;
            // otherwise the reposition alone was the payoff.
            NPC.TargetClosest(true);
            TryQueueComboFollowup(globalNPC, ModContent.ProjectileType<Projectiles.Enemy.BlackThrowingSpear>());
            EndAttack(cooldown: globalNPC.HasPendingCombatComboMove ? 0 : OpenerCooldownTicks);
        }

        /// <summary>
        /// Ground he gives up becomes ground you cannot take. Drops a stationary curse patch whenever he is
        /// genuinely retreating on foot — the kite band's back-off, a LeapStrike backleap, an evasive hop.
        /// </summary>
        /// <remarks>
        /// Detected from VELOCITY rather than hooked into SmartFighter4AI's kite code on purpose: this is a
        /// BlackKnight-only behaviour and the mover is shared with every other kiting enemy in the mod.
        /// Reading his own retreat here catches every source of it and cannot regress anything else.
        ///
        /// PlagueTeleportCloud deals no damage — it only builds CurseBuildup. Retreat should cost the player
        /// TEMPO and space, not chip damage they cannot answer.
        /// </remarks>
        private void RunPlagueTrail(tsorcRevampGlobalNPC globalNPC)
        {
            if (plagueTrailCooldown > 0)
            {
                plagueTrailCooldown--;
                return;
            }
            if (!player.active || player.dead || NPC.velocity.Y != 0f
                || globalNPC.TeleportCountdown > 0 || globalNPC.TeleportAppearanceTimer > 0)
            {
                return;
            }

            // Retreating = moving away from the player at a real pace, not just drifting or being shoved.
            int awayDirection = player.Center.X >= NPC.Center.X ? -1 : 1;
            if (NPC.velocity.X * awayDirection < PlagueTrailMinRetreatSpeed)
            {
                return;
            }

            plagueTrailCooldown = PlagueTrailCooldownTicks;
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }
            var cloud = Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), NPC.Bottom, Vector2.Zero,
                ModContent.ProjectileType<Projectiles.VFX.PlagueTeleportCloud>(), 0, 0f, Main.myPlayer,
                1f, PlagueTrailRadius);
            cloud.timeLeft = PlagueTrailLifetime;
            NPC.netUpdate = true;
        }

        /// <summary>
        /// Punish for closing on him. Fires on PROXIMITY rather than from the bag, so stepping inside his
        /// guard is what causes it — the band becomes a threat instead of a preference.
        /// </summary>
        private bool TryStartCounter(tsorcRevampGlobalNPC globalNPC)
        {
            if (counterCooldown > 0 || !CanStartAttack(globalNPC)
                || NPC.Distance(player.Center) > CounterTriggerRange
                || MathF.Abs(player.Center.Y - NPC.Center.Y) > 64f)
            {
                return false;
            }

            counterCooldown = CounterCooldownTicks;
            // Unblockable: the counter is the answer to someone who has already committed to being close,
            // so hiding behind a shield should not be the free out.
            globalNPC.SetActiveAttackDefenseTraits(NPC, AttackDefenseTraits.BypassesActiveShield);
            BeginAttack(BlackKnightAttack.CounterCheck, globalNPC);
            SpawnAttackFlash(Color.Red);
            SoundEngine.PlaySound(SoundID.Item71 with { Volume = 0.7f, Pitch = 0.35f }, NPC.Center);
            return true;
        }

        private void TickCounterCheck(int t, tsorcRevampGlobalNPC globalNPC)
        {
            NPC.knockBackResist = 0f;

            if (t < CounterStrikeTick)
            {
                // Wind up by CLOSING, not by planting: the ramp is the tell, and it means the dash launches
                // from a body already moving toward the player rather than from a standstill.
                int facing = player.Center.X >= NPC.Center.X ? 1 : -1;
                NPC.direction = facing;
                NPC.spriteDirection = facing;
                float ramp = t / (float)MathF.Max(1f, CounterStrikeTick);
                KnightHopPlanner.ApproachHorizontalSpeed(NPC, facing,
                    MathHelper.Lerp(1.2f, CounterWindupSpeed, ramp), 0.34f);
                return;
            }

            if (t == CounterStrikeTick)
            {
                int direction = player.Center.X >= NPC.Center.X ? 1 : -1;
                NPC.direction = direction;
                NPC.spriteDirection = direction;
                // Committed burst with a small hop, mirroring RedKnightAttackController.LeonhardDashVelocity.
                // The hop is what makes it read as a lunge rather than a slide, and it carries the anchored
                // hitbox onto the player instead of waiting for them to walk into it.
                NPC.velocity = new Vector2(direction * CounterDashSpeed, CounterDashHop);
                SpawnSpearHitbox(direction, CounterReach, CounterHeight, globalNPC);
                SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.95f, Pitch = 0.15f }, NPC.Center);
                NPC.netUpdate = true;
                return;
            }

            // Committed: no air control through the strike, then shed the dash on landing.
            if (t > CounterStrikeTick + CounterHitTicks && NPC.velocity.Y == 0f)
            {
                NPC.velocity.X *= 0.80f;
            }
        }

        /// <summary>
        /// Ballistic leap in with the spear, then a backward leap that throws it at the apex.
        /// </summary>
        /// <remarks>
        /// The retreat is the attack. He only backleaps AFTER committing a strike, and the spear leaves his
        /// hand on the way out — so following him is punished and the disengage is a threat rather than an
        /// escape. This is the move that is supposed to make the spacing read as intent rather than fear.
        ///
        /// Every phase boundary is a fixed tick so clients can derive the held-spear draw and hit window
        /// from ai[3]; see the LeapStrike constants.
        /// </remarks>
        private void TickLeapStrike(int t, tsorcRevampGlobalNPC globalNPC)
        {
            if (t < LeapWindupTicks)
            {
                NPC.velocity.X *= 0.82f;             // coil
                if (t == LeapFlashTick)
                {
                    SpawnAttackFlash(Color.OrangeRed);
                    storedPlayerPosition = player.Center;
                    framesSinceStoredPosition = 0;
                    NPC.netUpdate = true;
                }
                return;
            }

            if (t == LeapWindupTicks)
            {
                // Ballistic solve so the arc actually lands on the player rather than a flat lunge.
                int direction = player.Center.X >= NPC.Center.X ? 1 : -1;
                NPC.direction = direction;
                NPC.spriteDirection = direction;
                NPC.velocity = UsefulFunctions.BallisticTrajectory(NPC.Center, player.Center,
                    LeapOutSpeed, fallback: true);
                SpawnSpearHitbox(direction, LeapSpearReach, LeapSpearHeight, globalNPC);
                SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.9f, Pitch = -0.1f }, NPC.Center);
                NPC.netUpdate = true;
                return;
            }

            if (t < LeapStrikeEndTick)
            {
                return; // committed arc — no air control, the leap is a commitment
            }

            if (t < LeapRecoverTick)
            {
                if (NPC.velocity.Y == 0f)
                {
                    NPC.velocity.X *= 0.80f;         // landed: shed the slide
                }
                return;
            }

            if (t >= LeapBackTick)
            {
                // Keep him FACING the player for the whole retreat. He is leaping backwards with the spear
                // cocked and about to throw it — turning his back would read as fleeing, and the apex throw
                // would appear to come out of the back of his head. Both direction and spriteDirection are
                // set: vanilla's AnimationType re-derives spriteDirection from direction on any grounded
                // frame, so setting only the sprite would snap back the moment he lands.
                int facePlayer = player.Center.X >= NPC.Center.X ? 1 : -1;
                NPC.direction = facePlayer;
                NPC.spriteDirection = facePlayer;
            }

            if (t == LeapBackTick)
            {
                // Backleap, spear still in hand. Away from the player, not away from his facing, so a
                // player who has run past him does not get chased by the disengage.
                int away = player.Center.X >= NPC.Center.X ? -1 : 1;
                NPC.velocity = new Vector2(away * LeapBackSpeedX, LeapBackSpeedY);
                SoundEngine.PlaySound(SoundID.Item8 with { Volume = 0.6f, Pitch = 0.4f }, NPC.Center);
                NPC.netUpdate = true;
                return;
            }

            if (t == LeapThrowTick)
            {
                // Apex throw. Aimed live rather than at the stored prediction: he can see the player from
                // up here, and a lead-predicted throw from mid-air reads as a miss the player did not cause.
                NPC.TargetClosest(true); // facing is already held on the player by the backleap block above
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Vector2 velocity = UsefulFunctions.BallisticTrajectory(NPC.Center, player.Center,
                        14f, fallback: true) + player.velocity * 0.5f;
                    SpawnSpearProjectile(velocity, globalNPC);
                }
                SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.85f, PitchVariance = 0.1f }, NPC.Center);
                NPC.netUpdate = true;
            }
        }

        /// <summary>
        /// Raises a frontal ward that eats projectiles, and walks slowly forward behind it.
        /// </summary>
        /// <remarks>
        /// He commits to ONE facing for WardFacingLockTicks and cannot turn, which is the counterplay:
        /// getting behind him beats it outright. He also keeps advancing, so ignoring it is not free —
        /// the ward is how a slow enemy closes on someone who would otherwise just walk backwards shooting.
        /// </remarks>
        private void TickCurseWard(int t, tsorcRevampGlobalNPC globalNPC)
        {
            if (t < WardFacingLockTicks)
            {
                // Hard facing lock. Re-asserted every tick because FindFrame/FaceAttackAim and the mover
                // would otherwise turn him toward the player, which would defeat the entire move.
                NPC.direction = wardFacing;
                NPC.spriteDirection = wardFacing;
                globalNPC.ShieldGuarding = true;      // front hits also build reduced poise
                KnightHopPlanner.ApproachHorizontalSpeed(NPC, wardFacing, WardAdvanceSpeed, 0.06f);

                if (!Main.dedServ && Main.rand.NextBool(2))
                {
                    // The ward plane itself: motes hanging in a vertical sheet on the warded side.
                    Vector2 position = NPC.Center + new Vector2(wardFacing * Main.rand.NextFloat(20f, 32f),
                        Main.rand.NextFloat(-26f, 22f));
                    Dust mote = Dust.NewDustPerfect(position, DustID.ShadowbeamStaff,
                        new Vector2(wardFacing * 0.25f, -Main.rand.NextFloat(0.2f, 0.8f)), 180,
                        new Color(104, 38, 160), Main.rand.NextFloat(1.1f, 1.8f));
                    mote.noGravity = true;
                }
                if (t == 0)
                {
                    SoundEngine.PlaySound(SoundID.Item29 with { Volume = 0.6f, Pitch = -0.5f }, NPC.Center);
                }
                // Sparse rain while he holds. Deliberately small waves — this is pressure to make standing
                // still and backing off both wrong, not a curtain that overrides the ward's own read.
                if (t == WardRainTickA || t == WardRainTickB)
                {
                    FireGravefallWave(WardRainCount, WardRainSpread, WardRainRadius, 2f);
                    SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.4f, Pitch = 0.15f }, NPC.Center);
                }
                return;
            }

            // Lock released: he may face the player again and the ward drops.
            globalNPC.ShieldGuarding = false;
            NPC.velocity.X *= 0.85f;
            if (t == WardFacingLockTicks)
            {
                ReleaseWard();
                NPC.netUpdate = true;
            }
        }

        /// <summary>
        /// The ward collapses and gives back what it drank: a rain volley scaled by how much damage was
        /// poured into the front of it.
        /// </summary>
        /// <remarks>
        /// Scaled against lifeMax rather than a flat damage number so it behaves the same across the
        /// normal / hardmode / SuperHardMode stat tiers instead of trivially maxing out in the late game.
        /// A player who correctly stopped shooting and went around still eats the base volley, so the ward
        /// always ends on a beat — it just ends on a much bigger one if they fought it head-on.
        /// </remarks>
        private void ReleaseWard()
        {
            float charge = NPC.lifeMax > 0
                ? MathHelper.Clamp(wardAbsorbedDamage / (NPC.lifeMax * WardReleaseFullChargeFraction), 0f, 1f)
                : 0f;
            int bonus = (int)MathF.Round(charge * WardReleaseMaxBonus);
            wardAbsorbedDamage = 0f;

            SoundEngine.PlaySound(SoundID.Item78 with { Volume = 0.5f, Pitch = -0.3f }, NPC.Center);
            if (bonus > 0)
            {
                SoundEngine.PlaySound(SoundID.Item122 with { Volume = 0.75f, Pitch = -0.35f }, NPC.Center);
            }

            if (!Main.dedServ)
            {
                // The collapse reads as the absorbed energy venting outward, so the size of the burst is
                // itself the feedback for how much was shot into it.
                int burst = 18 + bonus * 9;
                for (int i = 0; i < burst; i++)
                {
                    Vector2 velocity = Main.rand.NextVector2CircularEdge(3.2f, 3.2f)
                        * Main.rand.NextFloat(0.4f, 1f) + new Vector2(wardFacing * 1.4f, 0f);
                    Dust mote = Dust.NewDustPerfect(NPC.Center + new Vector2(wardFacing * 18f, 0f),
                        Main.rand.NextBool(3) ? DustID.Smoke : DustID.ShadowbeamStaff, velocity, 175,
                        new Color(112, 40, 168), Main.rand.NextFloat(1.2f, 2.2f));
                    mote.noGravity = true;
                }
            }

            FireGravefallWave(WardReleaseBaseCount + bonus, 48f + bonus * 5f, 250f, 3f);
        }

        /// <summary>
        /// Seals three columns of black death into the floor: one under the player now, and a pair beyond
        /// them that erupts three seconds later.
        /// </summary>
        /// <remarks>
        /// The pair is placed PAST the player, away from the knight, so the natural retreat direction is the
        /// one that becomes unavailable. That is the whole idea — he does not chase you, he removes the
        /// places you wanted to go. The follow-up gets no caster animation (he has already moved on) but
        /// still telegraphs on the floor for its full delay.
        /// </remarks>
        private void TickPlagueSeals(int t, tsorcRevampGlobalNPC globalNPC)
        {
            if (t < SealCastTicks)
            {
                NPC.velocity.X *= 0.88f;              // plants to cast
                if (t == 0)
                {
                    SoundEngine.PlaySound(SoundID.Item8 with { Volume = 0.6f, Pitch = -0.2f }, NPC.Center);
                }
                if (!Main.dedServ)
                {
                    SpawnStaffCastDust(t);
                }
                return;
            }
            if (t != SealCastTicks)
            {
                return;
            }

            SpawnAttackFlash(new Color(96, 34, 148));
            SoundEngine.PlaySound(SoundID.Item104 with { Volume = 0.8f, Pitch = -0.4f }, NPC.Center);
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            int columnType = ModContent.ProjectileType<Projectiles.Enemy.BlackKnightPlagueColumn>();

            // 1) Under the player, on a short floor warning so it is still dodgeable.
            SpawnPlagueColumn(columnType, player.Bottom.X, SealFirstTelegraphTicks);

            // 2+3) The pincer: one column each side, offset in time. Flanking rather than stacking on the
            // far side means BOTH horizontal escapes get sealed, so the answer is to commit to a side early
            // (while only the centre column is live) rather than drift — which is the whole point of a
            // placed attack on a slow caster.
            SpawnPlagueColumn(columnType, player.Center.X - SealFlankOffset, SealDelayedTelegraphTicks);
            SpawnPlagueColumn(columnType, player.Center.X + SealFlankOffset, SealDelayedTelegraphTicks);
            NPC.netUpdate = true;
        }

        /// <summary>
        /// Drops a column onto the standable ground nearest the given X, so a seal always sits ON a floor
        /// rather than floating where the player happened to be mid-jump.
        /// </summary>
        private void SpawnPlagueColumn(int columnType, float worldX, int telegraphTicks)
        {
            Vector2 probe = new Vector2(worldX, player.Bottom.Y);
            if (!KnightHopPlanner.TryFindGround(probe, 6, 14, out Vector2 surface))
            {
                surface = probe;    // open air: seal it where it was aimed rather than dropping the cast
            }

            var column = Projectile.NewProjectileDirect(NPC.GetSource_FromThis(),
                new Vector2(surface.X, surface.Y - Projectiles.Enemy.BlackKnightPlagueColumn.ColumnHeight * 0.5f),
                Vector2.Zero, columnType, 0, 0f, Main.myPlayer,
                telegraphTicks, redMagicDamage);
            column.timeLeft = Projectiles.Enemy.BlackKnightPlagueColumn.ActiveTicks;
        }

        /// <summary>Casting motes streaming off the staff head while the seals are being drawn.</summary>
        private void SpawnStaffCastDust(int t)
        {
            if (!Main.rand.NextBool(2))
            {
                return;
            }
            float charge = MathHelper.Clamp(t / (float)SealCastTicks, 0f, 1f);
            Vector2 tip = StaffHeadWorld();
            Vector2 offset = Main.rand.NextVector2Circular(7f, 7f);
            Dust mote = Dust.NewDustPerfect(tip + offset,
                Main.rand.NextBool(3) ? DustID.Smoke : DustID.ShadowbeamStaff,
                new Vector2(0f, -Main.rand.NextFloat(0.3f, 1.2f)) - offset * 0.08f, 175,
                new Color(112, 40, 168), Main.rand.NextFloat(0.9f, 1.7f) * (0.5f + charge));
            mote.noGravity = true;
        }

        /// <summary>Source-anchored spear hitbox (GreatBlackKnightSpearHitbox, gated on ISpearMeleeWielder).</summary>
        private void SpawnSpearHitbox(int direction, float reach, float height, tsorcRevampGlobalNPC globalNPC)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }
            int index = Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, new Vector2(direction, 0f),
                ModContent.ProjectileType<Projectiles.Enemy.Weapons.GreatBlackKnightSpearHitbox>(),
                redKnightsGreatDamage, 4f, Main.myPlayer, reach, height);
            tsorcGlobalProjectile.SetDefenseTraits(index, globalNPC.ActiveAttackDefenseTraits);
        }
    }
}

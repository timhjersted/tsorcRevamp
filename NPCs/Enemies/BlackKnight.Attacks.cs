using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Content.Projectiles;
using tsorcRevamp.Content.Projectiles.Enemy;
using tsorcRevamp.Content.Projectiles.Enemy.Weapons;
using tsorcRevamp.Content.Projectiles.VFX;
using tsorcRevamp.Utilities;

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
        LeapStrike,     // ballistic sword leap, then a backward leap that throws a spear at the apex
        CounterCheck,   // proximity-triggered unblockable sword stab for closing on him
        CurseWard,      // frontal projectile ward — makes his slowness stop mattering
        PlagueSeals,    // placed columns of black death; answers a player who stands still
        CrystalStorm,   // below 1/3 HP: a five-crystal homing fan from the palms
    }

    /// <summary>
    /// One token in the opener bag: a single attack, or a fixed two-attack chain whose second attack starts the
    /// tick the first ends (proposal section 0, Q3). Melee chains are multi-step MeleeCombos instead.
    /// </summary>
    internal enum BlackKnightCard : byte
    {
        SpearThrow,
        HomingVolley,
        BombThrow,
        BlackRain,
        LeapStrike,
        PlagueSeals,
        CurseWard,
        DeathSeal,
        PlagueAmbush,
        CrystalStorm,
        SpearIntoBomb,
        HomingIntoRain,
        BombIntoLeap,
    }

    /// <summary>
    /// How the knight moves while committing a throw. Ported from RedKnight's RedThrowMovement: a thrown weapon does
    /// NOT require standing still, so the knight can back off, close, or pop straight up while releasing it.
    /// </summary>
    internal enum BlackThrowMovement : byte
    {
        None = 0,
        GroundHold,
        RetreatHop,
        AdvanceHop,
        VerticalHop,
    }

    public partial class BlackKnight
    {
        // ── Attack state ────────────────────────────────────────────────────────────────────────────────
        // Stored in NPC.ai[] so it rides the NPC sync (and every puppet phase snapshot) for free:
        //   ai[2] = current BlackKnightAttack.
        //   ai[3] = tick counter WITHIN the current attack. Every machine advances it while the attack runs, so poses
        //           and tell dust are smooth on clients; the server's snapshots correct any drift.
        // Puppets never use NPC.ai[] themselves.
        private BlackKnightAttack CurrentAttack => (BlackKnightAttack)(int)NPC.ai[2];
        private int AttackTimer => (int)NPC.ai[3];
        private bool AttackActive => CurrentAttack != BlackKnightAttack.None;

        // Server-only selection state; never drawn, so it needs no sync.
        private readonly List<BlackKnightCard> openerBag = new();
        private int openerBagHealthStage = -1;
        private int attackCooldown;
        // The second half of a chain card, started when the first attack completes. Server only.
        private BlackKnightAttack chainedAttack;
        private bool chainCardPlaying;
        // Synced (SendExtraAI): the throw hop picked at the start of a throw, read by the debug label.
        private BlackThrowMovement throwMovement;

        // ── Timings ─────────────────────────────────────────────────────────────────────────────────────
        private const int SpearWindupTicks = 60;
        private const int SpearWindupTicksUnblockable = 90; // the shield-bypass variant telegraphs longer
        private const int SpearFlashLead = 25;              // flash this many ticks before release
        private const int SpearThrowRecoveryTicks = 10;

        private const int HomingFirstBurstTick = 60;
        private const int HomingSecondBurstTick = 110;
        private const int HomingRecoveryTicks = 26;         // covers the palm's 14t hold + 12t lower

        private const int BombWindupTicks = 60;
        private const int BombFlashLead = 25;
        private const int BombRecoveryTicks = 10;

        // Both arms rise over 20t; wave 1 drops at the raise peak (it used to fire on tick 0 with no gesture).
        private const int BlackRainWaveTick = 20;
        private const int BlackRainTicks = 44;

        private const int DeathSealTelegraphTicks = 100;
        private const int DeathSealFlashTick = 65;
        private const int DeathSealFireTicks = 35;
        private const int DeathSealRecoveryTicks = 12;

        private const int PlagueAmbushVanishTick = 12;
        private const int PlagueAmbushTicks = 150;

        // ── Shadow Crystal Storm (below 1/3 HP) ─────────────────────────────────────────────────────────
        // Was a global-clock side effect (GameUpdateCount % 420) that fired mid-attack and mid-stagger from his
        // centre. Now a bag attack: both palms gather a hex crystal for 40t (commit at 20), the back palm pushes
        // five homing crystals out in a 60-degree fan at 40, then 26t of hold and lowering.
        private const int CrystalStormReleaseTick = 40;
        private const int CrystalStormCommitTick = 20;
        private const int CrystalStormTicks = 66;
        private const int CrystalStormCount = 5;
        private const float CrystalStormSpread = MathHelper.Pi / 3f;
        private const float CrystalStormMinRange = 200f;
        private const float CrystalStormMaxRange = 900f;

        // ── LeapStrike ──────────────────────────────────────────────────────────────────────────────────
        // A sword leap. Timer-driven end to end: every phase boundary is a tick number all machines can compute.
        // The strike tick is solved at launch from the leap's flight time and synced (leapStrikeStartTick), so the
        // downswing pose and the hitbox's live window agree everywhere.
        //   0-24 tell: sword raised overhead while braking, flash at 8 | 24 launch (ballistic 12.5 px/t at the target)
        //   strike: live for 20t starting 16t before the predicted landing (ends ~4t after it), never before launch
        //   plant until 74 | 74 backleap, sword away and spear drawn | 90 apex throw | 104 end.
        private const int LeapWindupTicks = 24;
        private const int LeapFlashTick = 8;
        private const int LeapLiveTicks = 20;
        private const int LeapLiveBeforeLandingTicks = 16;
        private const int LeapBackTick = 74;
        private const int LeapThrowTick = 90;
        private const int LeapStrikeTicks = 104;
        private const float LeapOutSpeed = 12.5f;    // ballistic solve speed toward the player
        private const float LeapBackSpeedX = 6.2f;
        private const float LeapBackSpeedY = -8.4f;
        private const float LeapSwordReach = 78f;    // hitbox size during the strike
        private const float LeapSwordHeight = 44f;
        // Synced: tick the leap's downswing and live window start; -1 until the launch solves it.
        private int leapStrikeStartTick = -1;

        // ── CounterCheck ────────────────────────────────────────────────────────────────────────────────
        // Not a bag attack: proximity-triggered, so closing on him is what causes it. A 20t tell (the reaction
        // floor; it was 12t), then a sword stab dash with a small hop, unblockable, live 12t (rollable).
        private const int CounterStrikeTick = 20;
        private const int CounterHitTicks = 12;
        private const int CounterTicks = 52;
        private const float CounterTriggerRange = 112f;
        private const float CounterReach = 86f;
        private const float CounterHeight = 42f;
        // The stab DASHES: its hitbox rides the knight, so a stab from a standing stop only connects if the player
        // is already touching him. Wind up by accelerating in, then burst forward with a small ballistic hop.
        private const float CounterWindupSpeed = 3.4f;
        private const float CounterDashSpeed = 10f;
        private const float CounterDashHop = -3f;
        private const int CounterCooldownTicks = 210;
        private int counterCooldown;

        // ── CurseWard ───────────────────────────────────────────────────────────────────────────────────
        // The answer to "a slow enemy just gets kited". It makes range stop working, so the player has to solve him
        // positionally. The 120-tick facing LOCK is the counterplay: while warding he cannot turn, so flanking beats it.
        private const int WardFacingLockTicks = 120;
        private const int WardTicks = 152;                 // lock + a visible lower/recover tail
        private const float WardAdvanceSpeed = 1.15f;      // he walks INTO you behind it, slowly
        private const float WardMinRange = 200f;
        private const float WardMaxRange = 900f;
        private int wardFacing;                            // direction he committed to at cast time (synced)

        // Sparse vertical rain DURING the lock, so the ward is never 2.5s of dead air; it herds the player sideways.
        private const int WardRainTickA = 30;
        private const int WardRainTickB = 80;
        private const int WardRainCount = 2;
        private const float WardRainSpread = 34f;
        private const float WardRainRadius = 240f;

        // The ward FEEDS on what it absorbs and pays it back when it drops, so shooting into it is actively wrong.
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

        /// <summary>0..1 bloom of the ward plane: rises on cast, holds through the lock, collapses as it drops.</summary>
        internal float CurseWardRise
        {
            get
            {
                if (CurrentAttack != BlackKnightAttack.CurseWard)
                {
                    return 0f;
                }
                int timer = AttackTimer;
                if (timer < WardRiseTicks)
                {
                    return MathHelper.Clamp(timer / (float)WardRiseTicks, 0f, 1f);
                }
                if (timer < WardFacingLockTicks)
                {
                    return 1f;
                }
                return MathHelper.Clamp(1f - (timer - WardFacingLockTicks) / (float)WardFadeTicks, 0f, 1f);
            }
        }

        // ── PlagueSeals ─────────────────────────────────────────────────────────────────────────────────
        // His slowest, most committed cast; the payoff removes a chunk of the arena, so the read is long.
        private const int SealCastTicks = 90;              // caster telegraph before the seals are placed
        private const int SealCastTotalTicks = 114;
        private const int SealFirstTelegraphTicks = 26;    // floor warning on the column under the player
        private const int SealDelayedTelegraphTicks = 180; // 3s — the follow-up pair
        // The delayed pair FLANKS the player, one column each side, so both horizontal escapes get sealed.
        private const float SealFlankOffset = 500f;
        private const float SealMinRange = 180f;
        private const float SealMaxRange = 950f;
        // Grip to staff head along the held staff at draw scale 1 (Amethyst Staff icon diagonal, grip ~30% up).
        private const float StaffLength = 34f;

        /// <summary>World position of the staff's head (the gem end), along the posed staff from the hand.</summary>
        private Vector2 StaffHeadWorld()
        {
            return PuppetHandPosition + PuppetWeaponDirection * StaffLength;
        }

        // ── Plague trail ────────────────────────────────────────────────────────────────────────────────
        // Ground he gives up becomes ground you cannot take. PlagueTeleportCloud is stationary, deals NO damage, and
        // applies stacking CurseBuildup — denial pressure rather than a damage puddle.
        private const int PlagueTrailCooldownTicks = 45;
        private const float PlagueTrailRadius = 52f;
        private const int PlagueTrailLifetime = 150;
        private const float PlagueTrailMinRetreatSpeed = 0.9f;
        private int plagueTrailCooldown;

        /// <summary>The sword hitbox exists: the Leap Strike from launch to the backleap, plus the counter stab.
        /// Read by GreatBlackKnightSpearHitbox (ISpearMeleeWielder).</summary>
        public bool SpearMeleeActive => (CurrentAttack == BlackKnightAttack.LeapStrike
                && AttackTimer >= LeapWindupTicks && AttackTimer < LeapBackTick)
            || CurrentAttack == BlackKnightAttack.CounterCheck;

        /// <summary>Strike is live and should deal damage.</summary>
        public bool SpearMeleeHitWindow
        {
            get
            {
                int timer = AttackTimer;
                if (CurrentAttack == BlackKnightAttack.LeapStrike)
                {
                    bool solved = leapStrikeStartTick >= 0;
                    return solved && timer >= leapStrikeStartTick && timer < leapStrikeStartTick + LeapLiveTicks;
                }
                if (CurrentAttack == BlackKnightAttack.CounterCheck)
                {
                    return timer >= CounterStrikeTick && timer < CounterStrikeTick + CounterHitTicks;
                }
                return false;
            }
        }

        /// <summary>Cooldown between bag cards. A chain card's second attack bypasses it; the card then pays
        /// ChainExtraCooldownTicks on top when it finishes.</summary>
        private const int OpenerCooldownTicks = 45;
        private const int OpenerFailCooldownTicks = 20;
        private const int ChainExtraCooldownTicks = 15;

        // ── Range bands ─────────────────────────────────────────────────────────────────────────────────
        // Partitioned around the kite band (KiteRangeMin/Max = 2..15 tiles = ~32..240px): melee (the sword combos and
        // the counter) owns inside 120px, the spear is his only ranged option close in, the bomb is the tool for the
        // band edge, homing / rain / seals punish disengaging.
        private const float SpearMinRange = 120f;
        private const float HomingMinRange = 300f;
        private const float HomingMaxRange = 900f;
        private const float BombMinRange = 190f;
        private const float BombMaxRange = 620f;
        private const float BlackRainMinRange = 250f;
        private const float DeathSealMaxRange = 900f;
        private const float PlagueAmbushMinRange = 260f; // only worth vanishing if there is ground to cover
        private const float PlagueAmbushMaxRange = 1100f;
        // LeapStrike deliberately overlaps the spear throw's band: at mid range the player should not know whether
        // the answer is a thrown spear or him closing the gap himself.
        private const float LeapMinRange = 150f;
        private const float LeapMaxRange = 430f;
        private const float LeapMaxHeightDifference = 96f;
        // Spear and bomb aim this far past the target so arcs never land short (it was 10px).
        private const float AimOvershootPixels = 45f;

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
                string name = CurrentAttack switch
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
                    BlackKnightAttack.CrystalStorm => "Shadow Crystal Storm",
                    _ => null,
                };
                if (InCommittedAttack(NPC.GetGlobalNPC<tsorcRevampGlobalNPC>()))
                {
                    return name;
                }
                return $"{name} (Windup)";
            }
        }

        /// <summary>Total ticks this attack runs for, including its recovery tail.</summary>
        private int AttackDuration(BlackKnightAttack attack) => attack switch
        {
            BlackKnightAttack.SpearThrow => SpearReleaseTick + SpearThrowRecoveryTicks,
            BlackKnightAttack.HomingVolley => HomingSecondBurstTick + HomingRecoveryTicks,
            BlackKnightAttack.BombThrow => BombWindupTicks + BombRecoveryTicks,
            BlackKnightAttack.BlackRain => BlackRainTicks,
            BlackKnightAttack.DeathSeal => DeathSealTelegraphTicks + DeathSealFireTicks + DeathSealRecoveryTicks,
            BlackKnightAttack.PlagueAmbush => PlagueAmbushTicks,
            BlackKnightAttack.LeapStrike => LeapStrikeTicks,
            BlackKnightAttack.CounterCheck => CounterTicks,
            BlackKnightAttack.CurseWard => WardTicks,
            BlackKnightAttack.PlagueSeals => SealCastTotalTicks,
            BlackKnightAttack.CrystalStorm => CrystalStormTicks,
            _ => 1,
        };

        /// <summary>The spear throw's release tick: the unblockable variant telegraphs 30t longer. Read from the
        /// synced defence traits, so it agrees on every machine.</summary>
        private int SpearReleaseTick
        {
            get
            {
                if (NPC.GetGlobalNPC<tsorcRevampGlobalNPC>().ActiveAttackBypassesShield)
                {
                    return SpearWindupTicksUnblockable;
                }
                return SpearWindupTicks;
            }
        }

        /// <summary>True while the attack is committed enough to be hyper-armoured. Before this, a poise break
        /// cancels the windup.</summary>
        internal bool InCommittedAttack(tsorcRevampGlobalNPC globalNPC)
        {
            int timer = AttackTimer;
            return CurrentAttack switch
            {
                BlackKnightAttack.SpearThrow => timer >= SpearReleaseTick - SpearFlashLead,
                BlackKnightAttack.HomingVolley => timer >= HomingFirstBurstTick,
                BlackKnightAttack.BombThrow => timer >= BombWindupTicks - BombFlashLead,
                BlackKnightAttack.DeathSeal => timer >= DeathSealFlashTick,
                BlackKnightAttack.PlagueAmbush => true, // a teleport in flight must never be cancelled
                // Hyper-armoured from launch: an airborne leap staggered mid-arc would dump him in a broken pose.
                BlackKnightAttack.LeapStrike => timer >= LeapWindupTicks,
                BlackKnightAttack.CounterCheck => timer >= CounterStrikeTick,
                // The ward IS the commitment: he is locked facing one way and cannot be shoved out of it.
                BlackKnightAttack.CurseWard => timer < WardFacingLockTicks,
                BlackKnightAttack.PlagueSeals => timer >= SealCastTicks - 20,
                BlackKnightAttack.CrystalStorm => timer >= CrystalStormCommitTick,
                _ => false,
            };
        }

        /// <summary>True during the readable windup, before the attack is locked in.</summary>
        internal bool InAttackTelegraph(tsorcRevampGlobalNPC globalNPC)
            => AttackActive && !InCommittedAttack(globalNPC);

        // ── Selection ───────────────────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Server, from neutral: the proximity counter, then a bag card. True when an attack started (the caller hosts
        /// it in a Custom phase). The guard-pressure finisher needs nothing here: entering the Custom phase with the
        /// pressure ready makes the attack unblockable (PuppetNPC's shared finisher), and the spear throw reads that
        /// trait to take its 30t-longer tell.
        /// </summary>
        private bool TryStartAttack(tsorcRevampGlobalNPC globalNPC, bool hasPlayerLOS)
        {
            if (!CanStartAttack(globalNPC))
            {
                return false;
            }
            // The counter ignores the card cooldown: it is not one of his choices, it is what happens to you for
            // stepping inside his guard.
            if (TryStartCounter(globalNPC))
            {
                return true;
            }
            if (attackCooldown > 0)
            {
                return false;
            }
            return TryStartCard(hasPlayerLOS);
        }

        /// <summary>True when <paramref name="attack"/> may start at this distance and health. Shared by the bag (a
        /// card's opener) and the chain (its second attack, re-checked when the first one ends).</summary>
        private bool IsAttackInRange(BlackKnightAttack attack, float distance, bool hasPlayerLOS)
        {
            bool belowHalf = NPC.life <= NPC.lifeMax / 2;
            bool belowThird = NPC.life <= NPC.lifeMax / 3;
            tsorcRevampGlobalNPC globalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            switch (attack)
            {
                case BlackKnightAttack.SpearThrow:
                    return hasPlayerLOS && distance >= SpearMinRange;
                case BlackKnightAttack.HomingVolley:
                    return hasPlayerLOS && distance >= HomingMinRange && distance <= HomingMaxRange;
                case BlackKnightAttack.BombThrow:
                    return hasPlayerLOS && distance >= BombMinRange && distance <= BombMaxRange;
                case BlackKnightAttack.BlackRain:
                    return hasPlayerLOS && distance >= BlackRainMinRange;
                case BlackKnightAttack.DeathSeal:
                    return belowHalf && hasPlayerLOS && distance <= DeathSealMaxRange;
                case BlackKnightAttack.PlagueAmbush:
                    // No line of sight needed: closing a broken sight line is the point of it.
                    return belowHalf && globalNPC.TeleportCooldownTimer <= 0
                        && distance >= PlagueAmbushMinRange && distance <= PlagueAmbushMaxRange;
                case BlackKnightAttack.LeapStrike:
                {
                    // Needs room to arc, and a clear line so he doesn't leap into a wall.
                    float heightDifference = MathF.Abs(player.Center.Y - NPC.Center.Y);
                    return hasPlayerLOS && distance >= LeapMinRange && distance <= LeapMaxRange
                        && heightDifference <= LeapMaxHeightDifference;
                }
                case BlackKnightAttack.CurseWard:
                    return hasPlayerLOS && distance >= WardMinRange && distance <= WardMaxRange;
                case BlackKnightAttack.PlagueSeals:
                    // Placed, not aimed: sealing the floor around someone behind cover is what they are for.
                    return distance >= SealMinRange && distance <= SealMaxRange;
                case BlackKnightAttack.CrystalStorm:
                    return belowThird && hasPlayerLOS && distance >= CrystalStormMinRange && distance <= CrystalStormMaxRange;
            }
            return false;
        }

        private static BlackKnightAttack CardOpener(BlackKnightCard card) => card switch
        {
            BlackKnightCard.SpearThrow => BlackKnightAttack.SpearThrow,
            BlackKnightCard.HomingVolley => BlackKnightAttack.HomingVolley,
            BlackKnightCard.BombThrow => BlackKnightAttack.BombThrow,
            BlackKnightCard.BlackRain => BlackKnightAttack.BlackRain,
            BlackKnightCard.LeapStrike => BlackKnightAttack.LeapStrike,
            BlackKnightCard.PlagueSeals => BlackKnightAttack.PlagueSeals,
            BlackKnightCard.CurseWard => BlackKnightAttack.CurseWard,
            BlackKnightCard.DeathSeal => BlackKnightAttack.DeathSeal,
            BlackKnightCard.PlagueAmbush => BlackKnightAttack.PlagueAmbush,
            BlackKnightCard.CrystalStorm => BlackKnightAttack.CrystalStorm,
            BlackKnightCard.SpearIntoBomb => BlackKnightAttack.SpearThrow,
            BlackKnightCard.HomingIntoRain => BlackKnightAttack.HomingVolley,
            BlackKnightCard.BombIntoLeap => BlackKnightAttack.BombThrow,
            _ => BlackKnightAttack.None,
        };

        private static BlackKnightAttack CardFollowUp(BlackKnightCard card) => card switch
        {
            BlackKnightCard.SpearIntoBomb => BlackKnightAttack.BombThrow,
            BlackKnightCard.HomingIntoRain => BlackKnightAttack.BlackRain,
            BlackKnightCard.BombIntoLeap => BlackKnightAttack.LeapStrike,
            _ => BlackKnightAttack.None,
        };

        /// <summary>
        /// Draws one card from the bag, restricted to cards whose opener is in range now. Bag semantics (draw without
        /// replacement, refill when empty or when the health stage changes) guarantee every card an outing before any
        /// repeats. When everything in range has been drawn this cycle the bag refills rather than stalls, which keeps
        /// a cornered knight with one or two options still attacking.
        /// </summary>
        private bool TryStartCard(bool hasPlayerLOS)
        {
            float distance = NPC.Distance(player.Center);
            int healthStage = 0;
            if (NPC.life <= NPC.lifeMax / 3)
            {
                healthStage = 2;
            }
            else if (NPC.life <= NPC.lifeMax / 2)
            {
                healthStage = 1;
            }
            if (openerBag.Count == 0 || openerBagHealthStage != healthStage)
            {
                FillOpenerBag(healthStage);
            }

            List<BlackKnightCard> eligible = CollectEligibleCards(distance, hasPlayerLOS);
            if (eligible.Count == 0)
            {
                FillOpenerBag(healthStage);
                eligible = CollectEligibleCards(distance, hasPlayerLOS);
            }
            if (eligible.Count == 0)
            {
                attackCooldown = OpenerFailCooldownTicks;
                return false;
            }

            BlackKnightCard card = eligible[Main.rand.Next(eligible.Count)];
            openerBag.Remove(card);
            chainedAttack = CardFollowUp(card);
            chainCardPlaying = chainedAttack != BlackKnightAttack.None;
            BeginAttack(CardOpener(card));
            return true;
        }

        private List<BlackKnightCard> CollectEligibleCards(float distance, bool hasPlayerLOS)
        {
            List<BlackKnightCard> eligible = new List<BlackKnightCard>();
            foreach (BlackKnightCard card in openerBag)
            {
                if (IsAttackInRange(CardOpener(card), distance, hasPlayerLOS))
                {
                    eligible.Add(card);
                }
            }
            return eligible;
        }

        private void FillOpenerBag(int healthStage)
        {
            openerBag.Clear();
            openerBagHealthStage = healthStage;
            openerBag.Add(BlackKnightCard.SpearThrow);
            openerBag.Add(BlackKnightCard.HomingVolley);
            openerBag.Add(BlackKnightCard.BombThrow);
            openerBag.Add(BlackKnightCard.BlackRain);
            openerBag.Add(BlackKnightCard.LeapStrike);
            openerBag.Add(BlackKnightCard.PlagueSeals);
            openerBag.Add(BlackKnightCard.CurseWard);
            openerBag.Add(BlackKnightCard.SpearIntoBomb);
            openerBag.Add(BlackKnightCard.HomingIntoRain);
            openerBag.Add(BlackKnightCard.BombIntoLeap);
            if (healthStage >= 1)
            {
                openerBag.Add(BlackKnightCard.DeathSeal);
                openerBag.Add(BlackKnightCard.PlagueAmbush);
                // A second spear token below half: the throw is his bread-and-butter spacing tool and the bag would
                // otherwise dilute it exactly when the fight gets busiest.
                openerBag.Add(BlackKnightCard.SpearThrow);
            }
            if (healthStage >= 2)
            {
                openerBag.Add(BlackKnightCard.CrystalStorm);
            }
        }

        /// <summary>Shared gate for starting anything.</summary>
        private bool CanStartAttack(tsorcRevampGlobalNPC globalNPC)
        {
            return Main.netMode != NetmodeID.MultiplayerClient
                && !AttackActive
                && player.active && !player.dead
                && NPC.velocity.Y == 0f
                && globalNPC.StaggerTimer <= 0
                && globalNPC.TeleportCountdown <= 0
                && globalNPC.TeleportAppearanceTimer <= 0
                && globalNPC.DodgeTimer <= 0
                && globalNPC.DodgeRecoveryTimer <= 0
                && !globalNPC.Fleeing;
        }

        /// <summary>Server: starts an attack on tick 0, facing the target. The caller hosts it in a Custom phase (or
        /// it already runs in one, for a chained attack).</summary>
        private void BeginAttack(BlackKnightAttack attack)
        {
            NPC.ai[2] = (float)attack;
            NPC.ai[3] = 0f;
            NPC.TargetClosest(true);
            int facing = 1;
            if (player.Center.X < NPC.Center.X)
            {
                facing = -1;
            }
            NPC.direction = facing;
            NPC.spriteDirection = facing;

            throwMovement = BlackThrowMovement.None;
            if (attack == BlackKnightAttack.SpearThrow || attack == BlackKnightAttack.BombThrow)
            {
                throwMovement = ChooseThrowMovement(facing);
            }

            // The ward locks to whichever way he was facing when it went up; it must NOT track the player.
            wardFacing = facing;
            if (attack == BlackKnightAttack.CurseWard)
            {
                wardAbsorbedDamage = 0f; // each cast is charged only by what IT absorbs
            }
            leapStrikeStartTick = -1;

            if (attack != BlackKnightAttack.DeathSeal)
            {
                storedPlayerPosition = player.Center;
                framesSinceStoredPosition = 0;
            }
            NPC.netUpdate = true;
        }

        /// <summary>
        /// Server: the attack ran its full length. A chain card starts its second attack on this same tick (re-checked
        /// against range; a chain whose second half is out of range just ends), otherwise back to neutral.
        /// </summary>
        private void CompleteAttack()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            BlackKnightAttack next = chainedAttack;
            chainedAttack = BlackKnightAttack.None;
            if (next != BlackKnightAttack.None && player.active && !player.dead)
            {
                bool hasPlayerLOS = Collision.CanHitLine(NPC.Center, 1, 1, player.Center, 1, 1);
                if (IsAttackInRange(next, NPC.Distance(player.Center), hasPlayerLOS))
                {
                    BeginAttack(next);
                    return;
                }
            }

            int cooldown = OpenerCooldownTicks;
            if (chainCardPlaying)
            {
                cooldown += ChainExtraCooldownTicks;
            }
            EndAttack(cooldown);
        }

        /// <summary>Server: back to neutral (also a cancel: a lost sight line, a stagger, the phase ending under it).
        /// Clients follow the snapshot this marks.</summary>
        private void EndAttack(int cooldown = OpenerCooldownTicks)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }
            NPC.ai[2] = (float)BlackKnightAttack.None;
            NPC.ai[3] = 0f;
            throwMovement = BlackThrowMovement.None;
            leapStrikeStartTick = -1;
            chainCardPlaying = false;
            attackCooldown = cooldown;
            // The counter's unblockable trait is not a pressure sequence, so the puppet's neutral entry never clears it;
            // left set, every later attack would be unblockable (and the spear throw 30t longer).
            NPC.GetGlobalNPC<tsorcRevampGlobalNPC>().SetActiveAttackDefenseTraits(NPC, AttackDefenseTraits.None);
            NPC.netUpdate = true;
        }

        /// <summary>Server: abandon the attack and any chain behind it.</summary>
        private void CancelAttack()
        {
            chainedAttack = BlackKnightAttack.None;
            EndAttack();
        }

        // ── Throw hop movement (ported from RedKnight via KnightHopPlanner) ─────────────────────────────
        private const float ThrowHopSpeedY = 5.2f;
        private const float ThrowHopGravity = 0.35f;
        private const float ThrowRetreatSpeed = 3.2f;
        private const float ThrowAdvanceSpeed = 3f;
        private const float ThrowApproachSpeed = 1.4f;
        private const float ThrowRecoverySpeed = 2f;
        private const float ThrowAcceleration = 0.1f;
        // He ALSO kites, so a Red Knight-level retreat bias (0.72) compounds with it and he never closes.
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
        /// Drives the body through a throw. Air control is intentionally absent after takeoff: the hop is a commitment.
        /// Runs on every machine (client-side movement prediction); only the server downgrades the hop.
        /// </summary>
        private void RunThrowMovement(int hopTick, int releaseTick)
        {
            if (throwMovement == BlackThrowMovement.None || throwMovement == BlackThrowMovement.GroundHold)
            {
                float speed = ThrowRecoverySpeed;
                if (AttackTimer < releaseTick)
                {
                    speed = ThrowApproachSpeed;
                }
                KnightHopPlanner.ApproachHorizontalSpeed(NPC, NPC.direction, speed, ThrowAcceleration);
                return;
            }

            int travelDirection = NPC.direction;
            float travelSpeed = 0.8f;
            if (throwMovement == BlackThrowMovement.RetreatHop)
            {
                travelDirection = -NPC.direction;
                travelSpeed = ThrowRetreatSpeed;
            }
            else if (throwMovement == BlackThrowMovement.AdvanceHop)
            {
                travelSpeed = ThrowAdvanceSpeed;
            }

            if (AttackTimer < hopTick)
            {
                KnightHopPlanner.ApproachHorizontalSpeed(NPC, NPC.direction, ThrowApproachSpeed, ThrowAcceleration);
                return;
            }
            if (AttackTimer == hopTick)
            {
                if (Main.netMode == NetmodeID.MultiplayerClient)
                {
                    return; // the server decides the takeoff; its snapshot carries the velocity
                }

                // Re-validate at takeoff: the ground it was planned against may no longer be under its feet.
                bool advancing = throwMovement == BlackThrowMovement.AdvanceHop;
                bool safe = NPC.velocity.Y == 0f && KnightHopPlanner.HasSafeHop(NPC, player.Center, NPC.direction,
                    travelDirection, travelSpeed, advancing, ThrowHopSpeedY, ThrowHopGravity);
                if (!safe)
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
        /// One tick of the running attack, from DoCustomTick on EVERY machine: poses, tell dust and sounds play
        /// everywhere (they used to be server-only, so clients saw no tells), while decisions and spawns stay behind
        /// Main.netMode checks inside each attack. ai[3] advances everywhere; only the server ends an attack.
        /// </summary>
        internal void TickAttacks(tsorcRevampGlobalNPC globalNPC, bool hasPlayerLOS)
        {
            if (!AttackActive)
            {
                return;
            }

            // A stagger cancels a windup but never a committed attack.
            bool staggeredInWindup = globalNPC.StaggerTimer > 0 && !InCommittedAttack(globalNPC);
            if (!player.active || player.dead || staggeredInWindup)
            {
                CancelAttack();
                return;
            }

            int timer = AttackTimer;
            int duration = AttackDuration(CurrentAttack);
            if (timer >= duration)
            {
                // A client that ran ahead of the server holds the last tick until the snapshot ends or chains it.
                CompleteAttack();
                return;
            }

            switch (CurrentAttack)
            {
                case BlackKnightAttack.SpearThrow: TickSpearThrow(timer, globalNPC, hasPlayerLOS); break;
                case BlackKnightAttack.HomingVolley: TickHomingVolley(timer, hasPlayerLOS); break;
                case BlackKnightAttack.BombThrow: TickBombThrow(timer, hasPlayerLOS); break;
                case BlackKnightAttack.BlackRain: TickBlackRain(timer); break;
                case BlackKnightAttack.DeathSeal: TickDeathSeal(timer); break;
                case BlackKnightAttack.PlagueAmbush: TickPlagueAmbush(timer, globalNPC); break;
                case BlackKnightAttack.LeapStrike: TickLeapStrike(timer, globalNPC); break;
                case BlackKnightAttack.CounterCheck: TickCounterCheck(timer, globalNPC); break;
                case BlackKnightAttack.CurseWard: TickCurseWard(timer, globalNPC); break;
                case BlackKnightAttack.PlagueSeals: TickPlagueSeals(timer); break;
                case BlackKnightAttack.CrystalStorm: TickCrystalStorm(timer, hasPlayerLOS); break;
            }

            if (!AttackActive)
            {
                return; // the attack abandoned itself (lost line of sight)
            }

            // An attack that ended a chain-free cancel above already returned; a chained BeginAttack resets ai[3] to 0.
            if ((int)NPC.ai[3] == timer)
            {
                NPC.ai[3] = timer + 1;
            }
        }

        /// <summary>Aim point: the ~25-tick-old predicted position, AimOvershootPixels past the player so arcs land on
        /// and beyond them rather than short.</summary>
        private Vector2 PredictedAimPoint()
        {
            int direction = 1;
            if (storedPlayerPosition.X < NPC.Center.X)
            {
                direction = -1;
            }
            return new Vector2(storedPlayerPosition.X + AimOvershootPixels * direction, storedPlayerPosition.Y);
        }

        /// <summary>The bomb's launch velocity from <paramref name="source"/>: a 5 px/t lob with extra loft up close,
        /// 8 px/t plus lead far out. The loft is fixed (no random) so the pose's aim and the throw agree.</summary>
        private Vector2 BombLaunchVelocity(Vector2 source)
        {
            bool far = NPC.Distance(storedPlayerPosition) > 400f;
            float speed = 5f;
            if (far)
            {
                speed = 8f;
            }

            Vector2 velocity = UsefulFunctions.BallisticTrajectory(source, PredictedAimPoint(), speed, fallback: true);
            if (far && NPC.HasValidTarget)
            {
                velocity += player.velocity;
            }
            else
            {
                velocity.Y -= 1.5f; // a little extra loft on the short lob
            }
            return velocity;
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
                ModContent.ProjectileType<TelegraphFlash>(), 0, 0, Main.myPlayer,
                UsefulFunctions.ColorToFloat(color));
        }

        /// <summary>Server: refreshes the predicted aim point mid-windup, on a 25-tick cadence. Clients draw against
        /// this, so it marks a sync.</summary>
        private void RefreshStoredPosition()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }
            if (framesSinceStoredPosition < 25 || !player.active || player.dead)
            {
                return;
            }
            framesSinceStoredPosition = 0;
            storedPlayerPosition = player.Center;
            NPC.netUpdate = true;
        }

        private void SpawnSpearProjectile(Vector2 source, Vector2 velocity, tsorcRevampGlobalNPC globalNPC)
        {
            int projectileIndex = Projectile.NewProjectile(NPC.GetSource_FromThis(), source, velocity,
                ModContent.ProjectileType<BlackThrowingSpear>(), EnemyDamage.Projectile(redKnightsSpearDamage), 0f, Main.myPlayer);
            tsorcGlobalProjectile.SetDefenseTraits(projectileIndex, globalNPC.ActiveAttackDefenseTraits);
        }

        private void TickSpearThrow(int timer, tsorcRevampGlobalNPC globalNPC, bool hasPlayerLOS)
        {
            int release = SpearReleaseTick;
            RunThrowMovement(release - SpearFlashLead, release);

            if (timer >= release - SpearFlashLead)
            {
                NPC.knockBackResist = 0f;
            }
            if (timer == release - SpearFlashLead)
            {
                Color flashColor = Color.OrangeRed;
                if (globalNPC.ActiveAttackBypassesShield)
                {
                    flashColor = Color.Red;
                }
                SpawnAttackFlash(flashColor);
                RefreshStoredPosition();
            }
            if (timer != release)
            {
                return;
            }

            SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.8f, PitchVariance = 0.1f }, NPC.Center);
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }
            if (!hasPlayerLOS)
            {
                CancelAttack();
                return;
            }

            // Close throws are slower so they arc more visibly; far throws are flatter and faster.
            NPC.TargetClosest(true);
            float speed = Main.rand.NextFloat(12f, 14f);
            if (NPC.Distance(player.Center) > 400f)
            {
                speed = Main.rand.NextFloat(16f, 18f);
            }
            Vector2 velocity = UsefulFunctions.BallisticTrajectory(AttackHandWorld, PredictedAimPoint(), speed, fallback: true)
                + player.velocity;
            SpawnSpearProjectile(AttackHandWorld, velocity, globalNPC);
        }

        private void TickHomingVolley(int timer, bool hasPlayerLOS)
        {
            // Shadow motes gather IN the back palm through the raise.
            if (!Main.dedServ && timer <= HomingFirstBurstTick && Main.rand.NextBool(2))
            {
                Vector2 offset = Main.rand.NextVector2Circular(14f, 14f);
                Dust mote = Dust.NewDustPerfect(CastHandWorld + offset, DustID.ShadowbeamStaff, -offset * 0.08f, 6,
                    Color.DarkSlateGray, 0.7f);
                mote.noGravity = true;
            }
            if (timer == HomingFirstBurstTick - 25 || timer == HomingSecondBurstTick - 25)
            {
                SpawnAttackFlash(Color.Orange);
            }

            bool firstBurst = timer == HomingFirstBurstTick;
            bool secondBurst = timer == HomingSecondBurstTick;
            if (!firstBurst && !secondBurst)
            {
                return;
            }
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }
            if (!hasPlayerLOS)
            {
                CancelAttack();
                return;
            }

            // Burst 1 is a single lofted crystal; burst 2 is a faster, flatter pair. Both leave the back palm.
            int projectiles = 1;
            float projectileSpeed = 15f;
            float arc = 2.1f;
            if (secondBurst)
            {
                projectiles = 2;
                projectileSpeed = 18f;
                arc = 1.1f;
            }
            const float spread = MathHelper.Pi / 6f;
            Vector2 source = CastHandWorld;
            for (int i = 0; i < projectiles; i++)
            {
                float angle = i * spread - spread * (projectiles - 1) / 2f;
                Vector2 velocity = UsefulFunctions.BallisticTrajectory(source, player.Center,
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
                Projectile.NewProjectile(NPC.GetSource_FromThis(), source, velocity,
                    ModContent.ProjectileType<EnemyBlackKnightHomingCrystal>(), EnemyDamage.Projectile(redMagicDamage), 0f, Main.myPlayer);
            }
        }

        private void TickBombThrow(int timer, bool hasPlayerLOS)
        {
            RunThrowMovement(BombWindupTicks - BombFlashLead, BombWindupTicks);

            // The fuse is lit on camera the moment the bomb comes out (pose appear tick), not at the flash.
            int appearTick = BombWindupTicks - ThrowWhipTicks - ThrowWindupTicks - PropShowAheadTicks;
            if (timer == appearTick)
            {
                SoundEngine.PlaySound(UsefulFunctions.BombFuse with { Volume = 0.6f }, NPC.Center);
            }
            if (timer >= BombWindupTicks - BombFlashLead)
            {
                NPC.knockBackResist = 0f;
            }
            if (timer == BombWindupTicks - BombFlashLead)
            {
                SpawnAttackFlash(Color.OrangeRed);
                Lighting.AddLight(NPC.Center, Color.OrangeRed.ToVector3() * 3f);
                RefreshStoredPosition();
            }
            if (timer != BombWindupTicks)
            {
                return;
            }

            SoundEngine.PlaySound(SoundID.Item1 with { Volume = 1f, Pitch = -0.5f }, NPC.Center);
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }
            if (!hasPlayerLOS)
            {
                CancelAttack();
                return;
            }

            Projectile.NewProjectile(NPC.GetSource_FromThis(), AttackHandWorld, BombLaunchVelocity(AttackHandWorld),
                ModContent.ProjectileType<EnemyMoonfuryBomb>(), EnemyDamage.Projectile(redKnightsSpearDamage), 0f, Main.myPlayer);
        }

        /// <summary>
        /// Black rain barrage: each drop carries its OWN telegraph (EnemyBlackCursedBreath freezes and spins in place
        /// for 45 ticks before falling), so one call fires wave 1 and schedules 1-2 more, GravefallWaveCooldown apart,
        /// ticked from AI(). Up to 2 nearby players each get their own batch. Server only.
        /// </summary>
        private void StartGravefallBarrage(bool wideWave)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            gravefallWide = wideWave;
            gravefallWaveIndex = 0;
            gravefallWaveTimer = 0;
            gravefallWavesRemaining = Main.rand.Next(1, 3); // 1 or 2 MORE waves after this one = 2-3 total
            FireGravefallWave();
        }

        /// <summary>Up to 2 nearby players, closest first.</summary>
        private List<Player> GetGravefallTargets()
        {
            List<Player> targets = new List<Player>();
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player candidate = Main.player[i];
                if (candidate.active && !candidate.dead && NPC.Distance(candidate.Center) < 2000f)
                {
                    targets.Add(candidate);
                }
            }
            targets.Sort((first, second) => NPC.Distance(first.Center).CompareTo(NPC.Distance(second.Center)));
            if (targets.Count > 2)
            {
                targets.RemoveRange(2, targets.Count - 2);
            }
            return targets;
        }

        /// <summary>One wave of the barrage, escalating a little each wave (more drops, wider spread).</summary>
        private void FireGravefallWave()
        {
            int count = 4 + gravefallWaveIndex;
            float knockback = 3f;
            if (gravefallWide)
            {
                count = 6 + gravefallWaveIndex;
                knockback = 4f;
            }
            FireGravefallWave(count, 45f + gravefallWaveIndex * 12f, 260f + gravefallWaveIndex * 25f, knockback);
        }

        /// <summary>
        /// Explicitly-sized rain wave, so the Curse Ward can fire deliberately sparse waves without touching the
        /// barrage's escalation state. Server only: the spawn burst dust plays from the telegraph projectile.
        /// </summary>
        private void FireGravefallWave(int count, float spreadDeg, float radius, float knockback)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            List<Player> targets = GetGravefallTargets();
            if (targets.Count == 0 || count <= 0)
            {
                return;
            }

            foreach (Player target in targets)
            {
                for (int i = 0; i < count; i++)
                {
                    // Fan above AND to the sides rather than a uniform scatter directly overhead. -0.5..0.5 across the fan.
                    float fanPosition = 0f;
                    if (count > 1)
                    {
                        fanPosition = (i / (float)(count - 1)) - 0.5f;
                    }
                    float angleRad = MathHelper.ToRadians(fanPosition * spreadDeg + Main.rand.NextFloat(-6f, 6f));
                    Vector2 offset = new Vector2((float)Math.Sin(angleRad), -(float)Math.Cos(angleRad))
                        * (radius + Main.rand.NextFloat(-20f, 20f));
                    Vector2 spawnPosition = target.Center + offset;

                    // Fan outward as well as down: left drops drift left, the centre falls straight, right drifts right.
                    Vector2 fallDirection = new Vector2(fanPosition * 0.75f, 1f).SafeNormalize(Vector2.UnitY);
                    Vector2 fallVelocity = fallDirection * Main.rand.NextFloat(4.5f, 6.5f);

                    if (!Main.dedServ)
                    {
                        for (int dustIndex = 0; dustIndex < 30; dustIndex++)
                        {
                            Vector2 dustVelocity = Main.rand.NextVector2Circular(3.5f, 3.5f);
                            Dust dust = Dust.NewDustPerfect(spawnPosition, DustID.Wraith, dustVelocity, 100, default, Main.rand.NextFloat(1.0f, 1.5f));
                            dust.noGravity = true;
                        }
                    }

                    Projectile.NewProjectile(NPC.GetSource_FromThis(), spawnPosition, fallVelocity,
                        ModContent.ProjectileType<EnemyBlackCursedBreath>(), EnemyDamage.Projectile(redMagicDamage), knockback, Main.myPlayer, 1f);

                    // Brief "portal opening" flash at the same spawn point, on top of the drop's own frozen-spin windup.
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), spawnPosition, Vector2.Zero,
                        ModContent.ProjectileType<BlackKnightGravefallTelegraph>(), 0, 0f, Main.myPlayer);
                }
            }
        }

        /// <summary>
        /// Advances the 2-3 wave barrage StartGravefallBarrage schedules. Runs OUTSIDE the attack state machine on
        /// purpose: waves are 90t apart but the BlackRain attack itself is only 44t. Server only.
        /// </summary>
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

        private void TickBlackRain(int timer)
        {
            NPC.velocity.X *= 0.85f;
            // Wraith motes streaming off both palms as the arms rise.
            if (!Main.dedServ && timer < BlackRainWaveTick && Main.rand.NextBool(2))
            {
                Vector2 palm = PuppetHandPosition;
                if (Main.rand.NextBool())
                {
                    palm = PuppetBackHandPosition;
                }
                Dust mote = Dust.NewDustPerfect(palm, DustID.Wraith, new Vector2(0f, -Main.rand.NextFloat(0.5f, 1.5f)),
                    100, default, Main.rand.NextFloat(0.9f, 1.3f));
                mote.noGravity = true;
            }
            if (timer != BlackRainWaveTick)
            {
                return;
            }

            SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.5f, Pitch = -0.01f }, NPC.Center);
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }
            // Wide waves are the bigger, more spread-out variant.
            StartGravefallBarrage(Main.rand.NextBool());
        }

        private void TickDeathSeal(int timer)
        {
            if (timer < DeathSealTelegraphTicks)
            {
                NPC.knockBackResist = 0f;
                NPC.velocity.X *= 0.85f;
                if (!Main.dedServ && Main.rand.NextBool(4))
                {
                    // Ring of motes collapsing inward, so the telegraph reads as power gathering.
                    float collapse = MathHelper.Clamp(1f - timer / (float)DeathSealTelegraphTicks, 0f, 1f);
                    Vector2 offset = Main.rand.NextVector2CircularEdge(240f, 240f) * collapse;
                    Dust mote = Dust.NewDustPerfect(NPC.Center + offset, DustID.BoneTorch, -offset * 0.025f, 120, default, 0.9f);
                    mote.noGravity = true;
                }
                Lighting.AddLight(NPC.Center, Color.WhiteSmoke.ToVector3() * 1.2f);

                if (timer == DeathSealFlashTick)
                {
                    SpawnAttackFlash(Color.OrangeRed);
                    RefreshStoredPosition();
                }
                return;
            }

            if (timer >= DeathSealTelegraphTicks + DeathSealFireTicks)
            {
                return; // recovery tail
            }

            // The stream leaves the front palm, which the pose has swung onto the locked aim.
            NPC.velocity.X *= 0.25f;
            SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.8f, PitchVariance = 1f }, NPC.Center);
            SoundEngine.PlaySound(SoundID.Item69 with { Volume = 0.9f, PitchVariance = 2f }, NPC.Center);
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            Vector2 source = AttackHandWorld;
            Vector2 aim = PredictedAimPoint();
            Vector2 skullVelocity = UsefulFunctions.BallisticTrajectory(source, aim, 2f, fallback: true)
                + Main.rand.NextVector2Circular(1, 5);
            Projectile.NewProjectile(NPC.GetSource_FromThis(), source, skullVelocity,
                ModContent.ProjectileType<EnemySpellSuddenDeathStrike>(), EnemyDamage.Projectile(redKnightsGreatDamage), 0f, Main.myPlayer, 1f);

            Vector2 breathVelocity = UsefulFunctions.BallisticTrajectory(source, aim, 2f, fallback: true)
                + Main.rand.NextVector2Circular(-5, 5);
            Projectile.NewProjectile(NPC.GetSource_FromThis(), source, breathVelocity,
                ModContent.ProjectileType<EnemyBlackCursedBreath>(), EnemyDamage.Projectile(redKnightsGreatDamage), 0f, Main.myPlayer, 2f);
        }

        /// <summary>
        /// The signature move: rather than teleport only as a reactive escape, he vanishes OFFENSIVELY and reappears on
        /// top of the player; the sword combos then pick up at that range on their own.
        /// </summary>
        private void TickPlagueAmbush(int timer, tsorcRevampGlobalNPC globalNPC)
        {
            if (timer < PlagueAmbushVanishTick)
            {
                // Brief tell before the vanish so it is reactable rather than instant; the motes gather at the palm.
                NPC.velocity.X *= 0.8f;
                if (!Main.dedServ && Main.rand.NextBool(2))
                {
                    Vector2 offset = Main.rand.NextVector2Circular(28f, 28f);
                    Dust mote = Dust.NewDustPerfect(CastHandWorld + offset, DustID.PurpleTorch,
                        -offset * 0.08f, 150, new Color(120, 45, 170), 1.1f);
                    mote.noGravity = true;
                }
                return;
            }

            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            if (timer == PlagueAmbushVanishTick)
            {
                // The shared plague teleport, including the retimed clouds: he emerges with ~2s of cloud still billowing.
                tsorcRevampAIs.QueueTeleport(NPC, 50, requireLineofSight: false,
                    globalNPC.TeleportTelegraphTime, globalNPC.PrefersHighGround, minRange: 5);
                return;
            }

            // The puppet freezes the Custom phase while a teleport is in flight; once he is back (or the teleport never
            // took, after a frame's grace) the reposition was the payoff.
            if (globalNPC.TeleportCountdown > 0 || globalNPC.TeleportAppearanceTimer > 0)
            {
                return;
            }
            if (timer < PlagueAmbushVanishTick + 4)
            {
                return;
            }

            NPC.TargetClosest(true);
            CompleteAttack();
        }

        /// <summary>
        /// Ground he gives up becomes ground you cannot take. Drops a stationary curse patch whenever he is genuinely
        /// retreating on foot. Detected from VELOCITY rather than hooked into SF4's kite code: BlackKnight-only, and it
        /// catches every source of retreat. Server only.
        /// </summary>
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
            int awayDirection = 1;
            if (player.Center.X >= NPC.Center.X)
            {
                awayDirection = -1;
            }
            if (NPC.velocity.X * awayDirection < PlagueTrailMinRetreatSpeed)
            {
                return;
            }

            plagueTrailCooldown = PlagueTrailCooldownTicks;
            Projectile cloud = Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), NPC.Bottom, Vector2.Zero,
                ModContent.ProjectileType<PlagueTeleportCloud>(), 0, 0f, Main.myPlayer, 1f, PlagueTrailRadius);
            cloud.timeLeft = PlagueTrailLifetime;
            NPC.netUpdate = true;
        }

        /// <summary>
        /// Punish for closing on him. Fires on PROXIMITY rather than from the bag, so stepping inside his guard is what
        /// causes it — the band becomes a threat instead of a preference. Server only.
        /// </summary>
        private bool TryStartCounter(tsorcRevampGlobalNPC globalNPC)
        {
            float heightDifference = MathF.Abs(player.Center.Y - NPC.Center.Y);
            if (counterCooldown > 0 || NPC.Distance(player.Center) > CounterTriggerRange || heightDifference > 64f)
            {
                return false;
            }

            counterCooldown = CounterCooldownTicks;
            // Unblockable: the counter answers someone already committed to being close; a shield is not the free out.
            globalNPC.SetActiveAttackDefenseTraits(NPC, AttackDefenseTraits.BypassesActiveShield);
            chainedAttack = BlackKnightAttack.None;
            BeginAttack(BlackKnightAttack.CounterCheck);
            SpawnAttackFlash(Color.Red);
            return true;
        }

        private void TickCounterCheck(int timer, tsorcRevampGlobalNPC globalNPC)
        {
            NPC.knockBackResist = 0f;
            if (timer == 0)
            {
                SoundEngine.PlaySound(SoundID.Item71 with { Volume = 0.7f, Pitch = 0.35f }, NPC.Center);
            }

            if (timer < CounterStrikeTick)
            {
                // Wind up by CLOSING, not by planting: the ramp is part of the tell, and the dash launches from a body
                // already moving toward the player.
                int facing = 1;
                if (player.Center.X < NPC.Center.X)
                {
                    facing = -1;
                }
                NPC.direction = facing;
                NPC.spriteDirection = facing;
                float ramp = timer / (float)CounterStrikeTick;
                KnightHopPlanner.ApproachHorizontalSpeed(NPC, facing, MathHelper.Lerp(1.2f, CounterWindupSpeed, ramp), 0.34f);
                return;
            }

            if (timer == CounterStrikeTick)
            {
                SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.95f, Pitch = 0.15f }, NPC.Center);
                if (Main.netMode == NetmodeID.MultiplayerClient)
                {
                    return;
                }

                int direction = 1;
                if (player.Center.X < NPC.Center.X)
                {
                    direction = -1;
                }
                NPC.direction = direction;
                NPC.spriteDirection = direction;
                // Committed burst with a small hop: it reads as a lunge rather than a slide, and carries the anchored
                // hitbox onto the player.
                NPC.velocity = new Vector2(direction * CounterDashSpeed, CounterDashHop);
                SpawnSwordHitbox(direction, CounterReach, CounterHeight, globalNPC);
                NPC.netUpdate = true;
                return;
            }

            // Committed: no air control through the strike, then shed the dash on landing.
            if (timer > CounterStrikeTick + CounterHitTicks && NPC.velocity.Y == 0f)
            {
                NPC.velocity.X *= 0.80f;
            }
        }

        /// <summary>
        /// Ballistic sword leap in, then a backward leap that throws a spear at the apex. The retreat is the attack:
        /// he only backleaps after committing a strike, and the spear leaves his hand on the way out.
        /// </summary>
        private void TickLeapStrike(int timer, tsorcRevampGlobalNPC globalNPC)
        {
            if (timer < LeapWindupTicks)
            {
                NPC.velocity.X *= 0.82f;             // coil, sword rising overhead
                if (timer == LeapFlashTick)
                {
                    SpawnAttackFlash(Color.OrangeRed);
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        storedPlayerPosition = player.Center;
                        framesSinceStoredPosition = 0;
                        NPC.netUpdate = true;
                    }
                }
                return;
            }

            if (timer == LeapWindupTicks)
            {
                SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.9f, Pitch = -0.1f }, NPC.Center);
                if (Main.netMode == NetmodeID.MultiplayerClient)
                {
                    return;
                }

                // Ballistic solve so the arc lands on the player rather than a flat lunge.
                int direction = 1;
                if (player.Center.X < NPC.Center.X)
                {
                    direction = -1;
                }
                NPC.direction = direction;
                NPC.spriteDirection = direction;
                NPC.velocity = UsefulFunctions.BallisticTrajectory(NPC.Center, player.Center, LeapOutSpeed, fallback: true);

                // Flight time ~ horizontal gap / horizontal speed. The 20t live window starts 16t before that landing
                // (never before launch) so it covers the descent and ~4t of the touchdown; the downswing pose keys off it.
                float horizontalSpeed = Math.Max(1f, Math.Abs(NPC.velocity.X));
                int flightTicks = (int)(Math.Abs(player.Center.X - NPC.Center.X) / horizontalSpeed);
                int strikeStart = LeapWindupTicks + flightTicks - LeapLiveBeforeLandingTicks;
                int latestStart = LeapBackTick - LeapLiveTicks;
                leapStrikeStartTick = Math.Clamp(strikeStart, LeapWindupTicks, latestStart);

                SpawnSwordHitbox(direction, LeapSwordReach, LeapSwordHeight, globalNPC);
                NPC.netUpdate = true;
                return;
            }

            if (timer < LeapBackTick)
            {
                // Committed arc: no air control. Landed: shed the slide and hold the planted blade.
                if (NPC.velocity.Y == 0f)
                {
                    NPC.velocity.X *= 0.80f;
                }
                return;
            }

            // Keep him FACING the player for the whole retreat: turning his back would read as fleeing, and the apex
            // throw would appear to come out of the back of his head.
            int facePlayer = 1;
            if (player.Center.X < NPC.Center.X)
            {
                facePlayer = -1;
            }
            NPC.direction = facePlayer;
            NPC.spriteDirection = facePlayer;

            if (timer == LeapBackTick)
            {
                SoundEngine.PlaySound(SoundID.Item8 with { Volume = 0.6f, Pitch = 0.4f }, NPC.Center);
                if (Main.netMode == NetmodeID.MultiplayerClient)
                {
                    return;
                }

                // Away from the player, not away from his facing, so a player who ran past him is not chased by it.
                NPC.velocity = new Vector2(-facePlayer * LeapBackSpeedX, LeapBackSpeedY);
                NPC.netUpdate = true;
                return;
            }

            if (timer == LeapThrowTick)
            {
                SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.85f, PitchVariance = 0.1f }, NPC.Center);
                if (Main.netMode == NetmodeID.MultiplayerClient)
                {
                    return;
                }

                // Apex throw, from the hand, aimed live: he can see the player from up here, and a lead-predicted throw
                // from mid-air reads as a miss the player did not cause.
                NPC.TargetClosest(true);
                Vector2 velocity = UsefulFunctions.BallisticTrajectory(AttackHandWorld, player.Center, 14f, fallback: true)
                    + player.velocity * 0.5f;
                SpawnSpearProjectile(AttackHandWorld, velocity, globalNPC);
                NPC.netUpdate = true;
            }
        }

        /// <summary>
        /// Raises a frontal ward that eats projectiles, and walks slowly forward behind it. He commits to ONE facing
        /// for WardFacingLockTicks and cannot turn: getting behind him beats it outright.
        /// </summary>
        private void TickCurseWard(int timer, tsorcRevampGlobalNPC globalNPC)
        {
            if (timer < WardFacingLockTicks)
            {
                // Hard facing lock, re-asserted every tick so the puppet's facing never turns him toward the player.
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
                if (timer == 0)
                {
                    SoundEngine.PlaySound(SoundID.Item29 with { Volume = 0.6f, Pitch = -0.5f }, NPC.Center);
                }
                // Sparse rain while he holds: pressure that makes standing still and backing off both wrong.
                if (timer == WardRainTickA || timer == WardRainTickB)
                {
                    FireGravefallWave(WardRainCount, WardRainSpread, WardRainRadius, 2f);
                    SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.4f, Pitch = 0.15f }, NPC.Center);
                }
                return;
            }

            // Lock released: he may face the player again and the ward drops.
            globalNPC.ShieldGuarding = false;
            NPC.velocity.X *= 0.85f;
            if (timer == WardFacingLockTicks)
            {
                ReleaseWard();
                NPC.netUpdate = true;
            }
        }

        /// <summary>
        /// The ward collapses and gives back what it drank: a rain volley scaled by the damage poured into its front,
        /// against lifeMax so it behaves the same across the stat tiers. Runs on every machine: the vent dust and sound
        /// play everywhere, the wave spawns server-side (the absorbed total is only exact on the server and the
        /// shooters' machines, so clients may vent a slightly different burst size).
        /// </summary>
        private void ReleaseWard()
        {
            float charge = 0f;
            if (NPC.lifeMax > 0)
            {
                charge = MathHelper.Clamp(wardAbsorbedDamage / (NPC.lifeMax * WardReleaseFullChargeFraction), 0f, 1f);
            }
            int bonus = (int)MathF.Round(charge * WardReleaseMaxBonus);
            wardAbsorbedDamage = 0f;

            SoundEngine.PlaySound(SoundID.Item78 with { Volume = 0.5f, Pitch = -0.3f }, NPC.Center);
            if (bonus > 0)
            {
                SoundEngine.PlaySound(SoundID.Item122 with { Volume = 0.75f, Pitch = -0.35f }, NPC.Center);
            }

            if (!Main.dedServ)
            {
                // The size of the burst is itself the feedback for how much was shot into it.
                int burst = 18 + bonus * 9;
                for (int i = 0; i < burst; i++)
                {
                    Vector2 velocity = Main.rand.NextVector2CircularEdge(3.2f, 3.2f)
                        * Main.rand.NextFloat(0.4f, 1f) + new Vector2(wardFacing * 1.4f, 0f);
                    int dustType = DustID.ShadowbeamStaff;
                    if (Main.rand.NextBool(3))
                    {
                        dustType = DustID.Smoke;
                    }
                    Dust mote = Dust.NewDustPerfect(NPC.Center + new Vector2(wardFacing * 18f, 0f), dustType, velocity, 175,
                        new Color(112, 40, 168), Main.rand.NextFloat(1.2f, 2.2f));
                    mote.noGravity = true;
                }
            }

            FireGravefallWave(WardReleaseBaseCount + bonus, 48f + bonus * 5f, 250f, 3f);
        }

        /// <summary>
        /// Seals three columns of black death into the floor: one under the player now, and a flanking pair that
        /// erupts three seconds later. He does not chase you; he removes the places you wanted to go.
        /// </summary>
        private void TickPlagueSeals(int timer)
        {
            if (timer < SealCastTicks)
            {
                NPC.velocity.X *= 0.88f;              // plants to cast
                if (timer == 0)
                {
                    SoundEngine.PlaySound(SoundID.Item8 with { Volume = 0.6f, Pitch = -0.2f }, NPC.Center);
                }
                if (!Main.dedServ)
                {
                    SpawnStaffCastDust(timer);
                }
                return;
            }
            if (timer != SealCastTicks)
            {
                NPC.velocity.X *= 0.88f;
                return;
            }

            SpawnAttackFlash(new Color(96, 34, 148));
            SoundEngine.PlaySound(SoundID.Item104 with { Volume = 0.8f, Pitch = -0.4f }, NPC.Center);
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            int columnType = ModContent.ProjectileType<BlackKnightPlagueColumn>();

            // 1) Under the player, on a short floor warning so it is still dodgeable.
            SpawnPlagueColumn(columnType, player.Bottom.X, SealFirstTelegraphTicks);

            // 2+3) The pincer: one column each side, offset in time, so the answer is to commit to a side early.
            SpawnPlagueColumn(columnType, player.Center.X - SealFlankOffset, SealDelayedTelegraphTicks);
            SpawnPlagueColumn(columnType, player.Center.X + SealFlankOffset, SealDelayedTelegraphTicks);
            NPC.netUpdate = true;
        }

        /// <summary>Drops a column onto the standable ground nearest the given X, so a seal always sits ON a floor.</summary>
        private void SpawnPlagueColumn(int columnType, float worldX, int telegraphTicks)
        {
            Vector2 probe = new Vector2(worldX, player.Bottom.Y);
            if (!KnightHopPlanner.TryFindGround(probe, 6, 14, out Vector2 surface))
            {
                surface = probe;    // open air: seal it where it was aimed rather than dropping the cast
            }

            Projectile column = Projectile.NewProjectileDirect(NPC.GetSource_FromThis(),
                new Vector2(surface.X, surface.Y - BlackKnightPlagueColumn.ColumnHeight * 0.5f),
                Vector2.Zero, columnType, 0, 0f, Main.myPlayer, telegraphTicks, EnemyDamage.Projectile(redMagicDamage));
            column.timeLeft = BlackKnightPlagueColumn.ActiveTicks;
        }

        /// <summary>Casting motes streaming off the staff head while the seals are being drawn.</summary>
        private void SpawnStaffCastDust(int timer)
        {
            if (!Main.rand.NextBool(2))
            {
                return;
            }
            float charge = MathHelper.Clamp(timer / (float)SealCastTicks, 0f, 1f);
            Vector2 tip = StaffHeadWorld();
            Vector2 offset = Main.rand.NextVector2Circular(7f, 7f);
            int dustType = DustID.ShadowbeamStaff;
            if (Main.rand.NextBool(3))
            {
                dustType = DustID.Smoke;
            }
            Dust mote = Dust.NewDustPerfect(tip + offset, dustType,
                new Vector2(0f, -Main.rand.NextFloat(0.3f, 1.2f)) - offset * 0.08f, 175,
                new Color(112, 40, 168), Main.rand.NextFloat(0.9f, 1.7f) * (0.5f + charge));
            mote.noGravity = true;
        }

        /// <summary>
        /// Shadow Crystal Storm: a hex crystal gathers between the raised palms (drawn in PostDraw), shadow motes spiral
        /// in, then five homing crystals leave the back palm in a 60-degree fan aimed at the target.
        /// </summary>
        private void TickCrystalStorm(int timer, bool hasPlayerLOS)
        {
            NPC.velocity.X *= 0.85f;
            if (timer < CrystalStormReleaseTick)
            {
                if (!Main.dedServ && Main.rand.NextBool(2))
                {
                    Vector2 palms = (PuppetHandPosition + PuppetBackHandPosition) * 0.5f;
                    Vector2 offset = Main.rand.NextVector2Circular(64f, 64f);
                    Dust mote = Dust.NewDustPerfect(palms + offset, DustID.ShadowbeamStaff, -offset * 0.1f, 150, default, 1.2f);
                    mote.noGravity = true;
                }
                if (timer == CrystalStormCommitTick)
                {
                    SpawnAttackFlash(new Color(96, 34, 148));
                }
                return;
            }
            if (timer != CrystalStormReleaseTick)
            {
                return;
            }

            SoundEngine.PlaySound(SoundID.Item69 with { Volume = 1f, Pitch = -0.3f, PitchVariance = 0.2f }, NPC.Center);
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }
            if (!hasPlayerLOS)
            {
                CancelAttack();
                return;
            }

            Vector2 source = CastHandWorld;
            Vector2 baseVelocity = UsefulFunctions.BallisticTrajectory(source, player.Center, 10f, fallback: true)
                + player.velocity / 2f;
            for (int i = 0; i < CrystalStormCount; i++)
            {
                float angle = (i - (CrystalStormCount - 1) / 2f) * (CrystalStormSpread / (CrystalStormCount - 1));
                Projectile.NewProjectile(NPC.GetSource_FromThis(), source, baseVelocity.RotatedBy(angle),
                    ModContent.ProjectileType<EnemyBlackKnightHomingCrystal>(), EnemyDamage.Projectile(redMagicDamage), 0f, Main.myPlayer);
            }
            NPC.netUpdate = true;
        }

        /// <summary>Source-anchored sword hitbox (GreatBlackKnightSpearHitbox, gated on ISpearMeleeWielder: it lives
        /// while SpearMeleeActive and deals damage while SpearMeleeHitWindow). Server only.</summary>
        private void SpawnSwordHitbox(int direction, float reach, float height, tsorcRevampGlobalNPC globalNPC)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }
            int index = Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, new Vector2(direction, 0f),
                ModContent.ProjectileType<GreatBlackKnightSpearHitbox>(), EnemyDamage.Projectile(redKnightsGreatDamage), 4f, Main.myPlayer, reach, height);
            tsorcGlobalProjectile.SetDefenseTraits(index, globalNPC.ActiveAttackDefenseTraits);
        }
    }
}

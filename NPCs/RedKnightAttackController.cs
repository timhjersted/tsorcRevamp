using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Projectiles;
using tsorcRevamp.Projectiles.Enemy;
using tsorcRevamp.Projectiles.Enemy.Weapons;
using tsorcRevamp.Projectiles.VFX;

namespace tsorcRevamp.NPCs
{
    internal enum KnightSpecialAttack : byte
    {
        None,
        FirebombReversal,
        PoisonSpearTether,
        CrimsonStandard,
        SpearThrow,
        PoisonArcVolley,
        FirebombThrow,
        SpectralHandBarrage,
        PoisonRain,
        PoisonCurtain,
        CrimsonTeleportAmbush,
        CrimsonAdvance,
        FurnacePincer,
        RoyalStandard,
        StormbreakerEdict,
        RedCourtProcession,
        CrimsonDominion,
        FurnaceHerald,
        StormHerald,
        CinderRain,
        StormPursuit
    }

    internal enum KnightAttackDeck : byte
    {
        Great,
        Dominion
    }

    internal enum KnightHeldProp : byte
    {
        None,
        Spear,
        Bomb,
        Magic,
        Spectral
    }

    internal enum RedTeleportPattern : byte
    {
        None,
        Pincer,
        RetreatThrow,
        GapClose,
        FeintPincer
    }

    internal enum RedThrowMovement : byte
    {
        None,
        GroundAdvance,
        RetreatHop,
        AdvanceHop,
        VerticalHop
    }

    internal readonly struct KnightAttackStats
    {
        public readonly int SpearDamage;
        public readonly int MagicDamage;
        public readonly int GreatDamage;
        public readonly int BombDamage;

        public KnightAttackStats(int spearDamage, int magicDamage, int greatDamage, int bombDamage)
        {
            SpearDamage = spearDamage;
            MagicDamage = magicDamage;
            GreatDamage = greatDamage;
            BombDamage = bombDamage;
        }
    }

    /// <summary>
    /// One server-authored state clock for the Red Knight family's new multi-step attacks. Presentation reads this
    /// same clock, while damage is delegated to synchronized projectiles with explicit geometry.
    /// </summary>
    internal sealed class RedKnightAttackController
    {
        public KnightSpecialAttack Attack { get; private set; }
        public int Timer { get; private set; }
        public int Direction { get; private set; } = 1;
        public Vector2 LockedTarget { get; private set; }
        public Vector2 AuxiliaryTargetA { get; private set; }
        public Vector2 AuxiliaryTargetB { get; private set; }
        public Vector2 LockedVelocity { get; private set; }
        public float ArenaBaseRotation { get; private set; }
        public int ArenaRotationDirection { get; private set; } = 1;
        public bool HalfHeraldComplete { get; private set; }
        public bool ThirdHeraldComplete { get; private set; }
        int emberBombProjectileIndex = -1;
        int emberReturnDashTimer = -1;
        RedTeleportPattern redTeleportPattern;
        RedThrowMovement redThrowMovement;
        int redTeleportFakeCount;
        bool redTeleportHidden;
        Vector2 redTeleportDestination;
        Vector2 redTeleportFakeA;
        Vector2 redTeleportFakeB;
        const int RedTeleportEntryTellTicks = 20;
        const int RedTeleportCloseArrivalTellTicks = 60;
        const int RedTeleportRetreatArrivalTellTicks = 24;
        const int RedTeleportFeintIntervalTicks = 45;
        const float RedTeleportFeintOffset = 68f;
        const float RedTeleportFeintMinimumSeparation = 120f;
        const int RedTeleportPincerActiveTicks = 18;
        const float RedThrowHopSpeedY = 5.2f;
        const float RedThrowGravity = 0.35f;
        const float RedThrowRetreatSpeed = 3.2f;
        const float RedThrowAdvanceSpeed = 3f;
        const float RedThrowMinimumForwardClearance = 72f;
        readonly List<KnightSpecialAttack> redAttackBag = new();
        readonly List<KnightSpecialAttack> greatAttackBag = new();
        readonly List<KnightSpecialAttack> dominionAttackBag = new();
        KnightSpecialAttack lastRedAttack;
        KnightSpecialAttack lastGreatAttack;
        KnightSpecialAttack lastDominionAttack;
        bool redBagBelowHalf;
        int greatBagPhase = -1;
        bool royalSpearThrow;

        // ---------------------------------------------------------------------------------------
        // CRIMSON DOMINION — no longer a timed attack, it is a one-way PHASE.
        //
        // Round 3 rework. Dominion used to be a 720-tick special that ran a containment ring, twelve
        // arena-edge bolts and a finishing nova the player had to escape, then handed control back
        // with a cooldown. It is now a permanent phase transition at 30% health:
        //
        //   Phase 1  "Plant & hold"  — DominionHoldTicks (300t) as a normal blocking special attack:
        //                              the knight plants its spear, is engulfed, and cannot move.
        //                              The visible containment geometry remains dangerous; the
        //                              humanoid body itself never deals passive contact damage.
        //   Phase 2  "Retract & fight" — the special attack ENDS and normal AI resumes, but
        //                              DominionEngaged stays true forever: faster, more evasive,
        //                              and the lightning sequence below runs on top of combat.
        //   Finale   — the seal + nova is now GRK's DEATH animation, fired from CheckDead(), not
        //                              on a timer. See GreatRedKnight.CheckDead.
        //
        // DominionEngaged is never cleared, so Dominion can never re-trigger and needs no cooldown.
        // ---------------------------------------------------------------------------------------
        public const int DominionHoldTicks = 300;
        public const float DominionHealthGate = 0.30f;

        /// <summary>True from the moment Dominion begins until the knight dies. Never cleared.</summary>
        public bool DominionEngaged { get; private set; }

        /// <summary>Ticks since Dominion began. Drives the A→D lightning loop; keeps running through
        /// the plant-and-hold, which is why the first two or three Stage A bolts land during it.</summary>
        public int DominionSequenceTimer { get; private set; }

        int attackCooldown = 240;
        int crimsonClearanceDelay;

        public bool Active => Attack != KnightSpecialAttack.None;

        public string DebugAttackName => Attack switch
        {
            KnightSpecialAttack.FirebombReversal => "Ember Reversal",
            KnightSpecialAttack.PoisonSpearTether => "Poison Spear Tether",
            KnightSpecialAttack.CrimsonStandard => "Bloodlance Eruption",
            KnightSpecialAttack.SpearThrow => $"{(royalSpearThrow ? "Royal Spear Throw" : "Spear Throw")} — {RedThrowMovementName}",
            KnightSpecialAttack.PoisonArcVolley => "Poison Arc Volley",
            KnightSpecialAttack.FirebombThrow => $"Firebomb Throw — {RedThrowMovementName}",
            KnightSpecialAttack.SpectralHandBarrage => "Spectral Hand Barrage",
            KnightSpecialAttack.PoisonRain => "Poison Rain",
            KnightSpecialAttack.PoisonCurtain => "Poison Curtain",
            KnightSpecialAttack.CrimsonTeleportAmbush => redTeleportPattern switch
            {
                RedTeleportPattern.Pincer => "Crimson Teleport Ambush — Pincer",
                RedTeleportPattern.RetreatThrow => "Crimson Teleport Ambush — Retreat Throw",
                RedTeleportPattern.GapClose => "Crimson Teleport Ambush — Gap Close",
                RedTeleportPattern.FeintPincer => $"Crimson Teleport Ambush — {redTeleportFakeCount} Feint",
                _ => "Crimson Teleport Ambush"
            },
            KnightSpecialAttack.CrimsonAdvance => "Crimson Advance",
            KnightSpecialAttack.FurnacePincer => "Furnace Pincer",
            KnightSpecialAttack.RoyalStandard => "Royal Standard",
            KnightSpecialAttack.StormbreakerEdict => "Stormbreaker Edict",
            KnightSpecialAttack.RedCourtProcession => "Red Court Procession",
            KnightSpecialAttack.CrimsonDominion => "Crimson Dominion",
            KnightSpecialAttack.FurnaceHerald => "Furnace Herald",
            KnightSpecialAttack.StormHerald => "Storm Herald",
            KnightSpecialAttack.CinderRain => "Cinder Rain",
            KnightSpecialAttack.StormPursuit => "Storm Pursuit",
            _ => null
        };

        public bool IsHerald => Attack == KnightSpecialAttack.FurnaceHerald || Attack == KnightSpecialAttack.StormHerald;

        /// <summary>These attacks hold a melee weapon while moving and therefore use the stable
        /// mid-stride body frame instead of allowing vanilla airborne animation to select frame 1.</summary>
        public bool UsesStableMeleeFrame => Attack switch
        {
            KnightSpecialAttack.FirebombReversal => true,
            KnightSpecialAttack.CrimsonTeleportAmbush when redTeleportPattern != RedTeleportPattern.RetreatThrow
                && Timer >= RedTeleportSnapTick
                && Timer < RedTeleportSnapTick + RedTeleportPincerActiveTicks => true,
            KnightSpecialAttack.CrimsonAdvance => true,
            KnightSpecialAttack.FurnacePincer when Timer >= 105 => true,
            KnightSpecialAttack.StormbreakerEdict => true,
            // FindFrame only consults this while airborne. A throw hop freezes whichever walking
            // frame was current at takeoff, then the walking cycle resumes naturally on landing.
            KnightSpecialAttack.SpearThrow when IsRedThrowHop && Timer >= 15 && Timer <= 45 => true,
            KnightSpecialAttack.FirebombThrow when IsRedThrowHop && Timer >= 30 && Timer <= 60 => true,
            _ => false
        };

        bool IsRedThrowHop => redThrowMovement is RedThrowMovement.RetreatHop
            or RedThrowMovement.AdvanceHop or RedThrowMovement.VerticalHop;

        string RedThrowMovementName => redThrowMovement switch
        {
            RedThrowMovement.GroundAdvance => "Ground Advance",
            RedThrowMovement.RetreatHop => "Retreat Hop",
            RedThrowMovement.AdvanceHop => "Advance Hop",
            RedThrowMovement.VerticalHop => "Vertical Hop",
            _ => "Ground"
        };

        /// <summary>Which stage of the permanent Dominion lightning loop is running, for the
        /// above-head debug readout (vfx-shader-tips §41).</summary>
        public string DominionStageName
        {
            get
            {
                if (!DominionEngaged)
                {
                    return "Inactive";
                }
                int t = DominionSequenceTimer % DomLoopTicks;
                if (t < DomStageABolts * DomStageAInterval)
                {
                    return $"Stage A ({t / DomStageAInterval + 1}/{DomStageABolts})";
                }
                if (t < DomStageBStart) return "Pause A";
                if (t < DomStageBStart + DomStageBBolts * DomStageBInterval)
                {
                    return $"Stage B ({(t - DomStageBStart) / DomStageBInterval + 1}/{DomStageBBolts})";
                }
                if (t < DomStageCStart) return "Pause B";
                if (t < DomStageCStart + DomStageCPairs * DomStageCInterval)
                {
                    return $"Stage C ({(t - DomStageCStart) / DomStageCInterval + 1}/{DomStageCPairs})";
                }
                if (t < DomStageDStart) return "Pause C";
                return "Stage D (sky)";
            }
        }

        public void TickCooldowns()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }
            if (attackCooldown > 0)
            {
                attackCooldown--;
            }
        }

        public bool TryStartRed(NPC npc, Player target, tsorcRevampGlobalNPC globalNPC)
        {
            if (!CanStart(npc, target, globalNPC) || attackCooldown > 0)
            {
                return false;
            }

            bool hasLineOfSight = Collision.CanHitLine(npc.Center, 1, 1, target.Center, 1, 1);
            float distance = npc.Distance(target.Center);
            bool belowHalf = npc.life <= npc.lifeMax / 2;
            KnightSpecialAttack[] candidates = new KnightSpecialAttack[9];
            int count = 0;

            if (hasLineOfSight && distance <= 210f)
            {
                candidates[count++] = KnightSpecialAttack.FirebombReversal;
            }
            if (hasLineOfSight && distance >= 120f && distance <= 820f)
            {
                candidates[count++] = KnightSpecialAttack.SpearThrow;
            }
            if (hasLineOfSight && distance >= 140f && distance <= 650f)
            {
                candidates[count++] = KnightSpecialAttack.PoisonArcVolley;
            }
            if (hasLineOfSight && distance >= 160f && distance <= 850f)
            {
                candidates[count++] = KnightSpecialAttack.FirebombThrow;
            }
            int targetDirection = target.Center.X >= npc.Center.X ? 1 : -1;
            Vector2 threadPoint = target.Bottom + new Vector2(targetDirection * 165f, 0f);
            if (belowHalf && hasLineOfSight && distance >= 150f && distance <= 620f
                && TryFindGround(threadPoint, 10, 30, out Vector2 threadGround))
            {
                candidates[count++] = KnightSpecialAttack.PoisonSpearTether;
                AuxiliaryTargetA = threadGround;
            }
            if (belowHalf && hasLineOfSight && distance >= 100f && distance <= 720f)
            {
                candidates[count++] = KnightSpecialAttack.SpectralHandBarrage;
            }
            if (belowHalf && distance >= 360f && distance <= 900f
                && RedKnightPoisonRainController.HasUsableLane(npc.Center))
            {
                candidates[count++] = KnightSpecialAttack.PoisonRain;
            }
            if (belowHalf && distance <= 900f
                && RedKnightPoisonCurtainController.HasUsableCurtain(target.Center))
            {
                candidates[count++] = KnightSpecialAttack.PoisonCurtain;
            }
            bool teleportRange = distance <= 260f || (distance >= 420f && distance <= 900f);
            if (belowHalf && teleportRange
                && TryPlanRedTeleport(npc, target, out RedTeleportPattern teleportPattern,
                    out Vector2 teleportDestination, out Vector2 fakeA, out Vector2 fakeB,
                    out int fakeCount))
            {
                candidates[count++] = KnightSpecialAttack.CrimsonTeleportAmbush;
                redTeleportPattern = teleportPattern;
                redTeleportDestination = teleportDestination;
                redTeleportFakeA = fakeA;
                redTeleportFakeB = fakeB;
                redTeleportFakeCount = fakeCount;
            }

            if (count == 0)
            {
                attackCooldown = 30;
                return false;
            }

            KnightSpecialAttack selected = ChooseSimpleRedAttack(candidates, count, belowHalf);
            if (selected == KnightSpecialAttack.PoisonSpearTether)
            {
                if (!TryFindGround(threadPoint, 10, 30, out Vector2 confirmedThreadGround))
                {
                    attackCooldown = 30;
                    return false;
                }
                AuxiliaryTargetA = confirmedThreadGround;
            }
            else if (selected == KnightSpecialAttack.CrimsonTeleportAmbush
                && redTeleportPattern == RedTeleportPattern.None)
            {
                attackCooldown = 30;
                return false;
            }

            Begin(npc, target, globalNPC, selected);
            return true;
        }

        KnightSpecialAttack ChooseSimpleRedAttack(KnightSpecialAttack[] candidates, int count, bool belowHalf)
        {
            if (redAttackBag.Count == 0 || redBagBelowHalf != belowHalf)
            {
                FillSimpleRedBag(belowHalf);
            }

            Span<KnightSpecialAttack> eligible = stackalloc KnightSpecialAttack[count];
            int eligibleCount = CollectRedEligible(candidates, count, eligible);
            if (eligibleCount == 0)
            {
                FillSimpleRedBag(belowHalf);
                eligibleCount = CollectRedEligible(candidates, count, eligible);
            }

            Span<KnightSpecialAttack> nonRepeat = stackalloc KnightSpecialAttack[eligibleCount];
            int nonRepeatCount = 0;
            for (int i = 0; i < eligibleCount; i++)
            {
                if (eligible[i] != lastRedAttack)
                {
                    nonRepeat[nonRepeatCount++] = eligible[i];
                }
            }
            Span<KnightSpecialAttack> selection = nonRepeatCount > 0 ? nonRepeat[..nonRepeatCount] : eligible[..eligibleCount];
            KnightSpecialAttack selected = selection[Main.rand.Next(selection.Length)];
            redAttackBag.Remove(selected);
            lastRedAttack = selected;
            return selected;
        }

        int CollectRedEligible(KnightSpecialAttack[] candidates, int count, Span<KnightSpecialAttack> destination)
        {
            int result = 0;
            for (int i = 0; i < count; i++)
            {
                if (redAttackBag.Contains(candidates[i]))
                {
                    destination[result++] = candidates[i];
                }
            }
            return result;
        }

        void FillSimpleRedBag(bool belowHalf)
        {
            redAttackBag.Clear();
            redBagBelowHalf = belowHalf;
            redAttackBag.Add(KnightSpecialAttack.FirebombReversal);
            redAttackBag.Add(KnightSpecialAttack.SpearThrow);
            redAttackBag.Add(KnightSpecialAttack.PoisonArcVolley);
            redAttackBag.Add(KnightSpecialAttack.FirebombThrow);
            if (belowHalf)
            {
                redAttackBag.Add(KnightSpecialAttack.PoisonSpearTether);
                redAttackBag.Add(KnightSpecialAttack.SpectralHandBarrage);
                redAttackBag.Add(KnightSpecialAttack.CrimsonTeleportAmbush);
                redAttackBag.Add(KnightSpecialAttack.PoisonRain);
                redAttackBag.Add(KnightSpecialAttack.PoisonCurtain);
            }
        }

        public bool TryStartGreat(NPC npc, Player target, tsorcRevampGlobalNPC globalNPC)
        {
            if (!CanStart(npc, target, globalNPC))
            {
                return false;
            }

            // Furnace Herald ("Half Herald") — 80%. Head of the herald chain and the first phase
            // marker in the fight: it fires early, then Storm Herald follows at its own lower
            // number (60%, below). The two are deliberately NOT the same threshold any more.
            //
            // IMPORTANT: HalfHeraldComplete no longer implies "the boss is at or below 50% HP".
            // Every reader that wants the 50% behaviour must carry its own explicit life check —
            // see the audit note in GreatRedKnight.AI's Ultrakill telegraph.
            if (!HalfHeraldComplete && npc.life <= npc.lifeMax * 0.8f)
            {
                Begin(npc, target, globalNPC, KnightSpecialAttack.FurnaceHerald);
                return true;
            }
            // Storm Herald ("Third Herald") moved 33% -> 70%. It is the unlock for everything gated
            // behind ThirdHeraldComplete — Stormbreaker Edict as a candidate below, and Rain of
            // Cursed Flame / Jellyfish Lightning in GreatRedKnight's AI — so pulling it earlier is
            // what makes those reachable sooner. Storm Herald sits at 60%, a full 20% below Furnace
            // Herald's 80% — the two heralds are separate beats, not one. Ordering is safe by
            // construction: it still requires HalfHeraldComplete, and since Furnace becomes
            // eligible at 80% it will always have had its chance long before HP reaches 60%.
            //
            // Knock-on: because Storm cannot COMPLETE until HP is at or below 60%, everything
            // chained behind ThirdHeraldComplete (Rain of Cursed Flame, Stormbreaker Edict) is now
            // realistically a ~60% unlock rather than the ~70% it was.
            if (HalfHeraldComplete && !ThirdHeraldComplete && npc.life <= npc.lifeMax * 0.6f)
            {
                Begin(npc, target, globalNPC, KnightSpecialAttack.StormHerald);
                return true;
            }
            // CRIMSON DOMINION — a FORCED one-way transition at 30% health, checked with the
            // heralds rather than rolled from the random candidate pool below. It is a phase
            // change, and as a candidate it could simply never come up.
            //
            // The gate is now `life <= 30%` alone; it is deliberately NOT tied to
            // HalfHeraldComplete any more. That flag still has plenty of work — it gates
            // FurnacePincer here, and the Ultrakill / DD2 / bomb windows in GreatRedKnight's normal
            // AI (GreatRedKnight.cs:549, 562, 715, 723, 753, 963) — so decoupling Dominion from it
            // breaks nothing. Ordering is safe by construction: FurnaceHerald (50%) and StormHerald
            // (33%) both sit above 30%, so they always fire first even on a burst-damage spike.
            if (!DominionEngaged
                && npc.life <= npc.lifeMax * DominionHealthGate
                && npc.velocity.Y == 0f
                && npc.Distance(target.Center) <= 360f)
            {
                Begin(npc, target, globalNPC, KnightSpecialAttack.CrimsonDominion);
                return true;
            }
            if (attackCooldown > 0)
            {
                return false;
            }

            float distance = npc.Distance(target.Center);
            bool hasLineOfSight = Collision.CanHitLine(npc.Center, 1, 1, target.Center, 1, 1);
            KnightSpecialAttack[] candidates = new KnightSpecialAttack[10];
            int count = 0;

            // Firebomb Reversal is a core Great Red Knight move, not a Dominion-only move. Dominion
            // retains it in its narrow pool below, but phase one may select it at close range too.
            if (hasLineOfSight && distance <= 230f)
            {
                candidates[count++] = KnightSpecialAttack.FirebombReversal;
            }
            if (distance <= 330f)
            {
                candidates[count++] = KnightSpecialAttack.CrimsonAdvance;
            }
            if (hasLineOfSight && distance >= 120f && distance <= 900f)
            {
                candidates[count++] = KnightSpecialAttack.SpearThrow;
            }

            // Royal Standard now throws one centre lance. Requiring two obsolete side-lance
            // landing points made a valid centre throw unavailable near arena walls.
            if (TryFindGround(target.Bottom, 10, 30, out Vector2 royalCenter))
            {
                candidates[count++] = KnightSpecialAttack.RoyalStandard;
            }

            int targetDirection = target.Center.X >= npc.Center.X ? 1 : -1;
            Vector2 behindPoint = target.Bottom + new Vector2(targetDirection * 165f, 0f);
            if (HalfHeraldComplete && hasLineOfSight && distance >= 150f && distance <= 720f
                && TryFindGround(behindPoint, 10, 30, out Vector2 furnaceGround))
            {
                candidates[count++] = KnightSpecialAttack.FurnacePincer;
                AuxiliaryTargetA = furnaceGround;
            }
            if (ThirdHeraldComplete && hasLineOfSight && distance >= 110f && distance <= 560f)
            {
                candidates[count++] = KnightSpecialAttack.StormbreakerEdict;
            }
            if (HalfHeraldComplete && distance >= 140f && distance <= 760f
                && TryFindCourtPortalOrigin(target.Center, 0f, out _))
            {
                candidates[count++] = KnightSpecialAttack.RedCourtProcession;
            }
            if (ThirdHeraldComplete && HasUsableCinderRainLane(target.Center))
            {
                candidates[count++] = KnightSpecialAttack.CinderRain;
            }
            if (npc.life <= npc.lifeMax * 0.5f && hasLineOfSight
                && distance >= 100f && distance <= 760f)
            {
                candidates[count++] = KnightSpecialAttack.SpectralHandBarrage;
            }
            if (ThirdHeraldComplete && npc.life <= npc.lifeMax * 0.4f
                && hasLineOfSight && distance <= 900f)
            {
                candidates[count++] = KnightSpecialAttack.StormPursuit;
            }
            // (CrimsonDominion is no longer a candidate — it is a forced phase transition above.)

            if (count == 0)
            {
                attackCooldown = 30;
                return false;
            }

            KnightSpecialAttack selected = ChooseFromBag(npc, candidates, count, KnightAttackDeck.Great);
            if (selected == KnightSpecialAttack.RoyalStandard)
            {
                LockedTarget = royalCenter;
            }
            else if (selected == KnightSpecialAttack.FurnacePincer)
            {
                if (!TryFindGround(behindPoint, 10, 30, out Vector2 confirmedFurnaceGround))
                {
                    attackCooldown = 30;
                    return false;
                }
                AuxiliaryTargetA = confirmedFurnaceGround;
            }

            Begin(npc, target, globalNPC, selected);
            return true;
        }

        /// <summary>
        /// Dominion's deliberately narrow authored pool. Crimson Advance has two tokens while
        /// Royal Spear Throw and Firebomb Reversal each have one, keeping the final phase melee-led
        /// without reintroducing dynamic weights.
        /// </summary>
        public bool TryStartDominion(NPC npc, Player target, tsorcRevampGlobalNPC globalNPC)
        {
            if (!DominionEngaged || !CanStart(npc, target, globalNPC) || attackCooldown > 0)
            {
                return false;
            }

            float distance = npc.Distance(target.Center);
            bool hasLineOfSight = Collision.CanHitLine(npc.Center, 1, 1, target.Center, 1, 1);
            KnightSpecialAttack[] candidates = new KnightSpecialAttack[3];
            int count = 0;

            if (hasLineOfSight && distance <= 620f)
            {
                candidates[count++] = KnightSpecialAttack.CrimsonAdvance;
            }
            if (hasLineOfSight && distance <= 230f)
            {
                candidates[count++] = KnightSpecialAttack.FirebombReversal;
            }
            if (hasLineOfSight && distance >= 120f && distance <= 900f)
            {
                candidates[count++] = KnightSpecialAttack.SpearThrow;
            }

            if (count == 0)
            {
                attackCooldown = 30;
                return false;
            }

            Begin(npc, target, globalNPC,
                ChooseFromBag(npc, candidates, count, KnightAttackDeck.Dominion));
            return true;
        }

        KnightSpecialAttack ChooseFromBag(NPC npc, KnightSpecialAttack[] candidates, int count,
            KnightAttackDeck deck)
        {
            List<KnightSpecialAttack> bag = deck switch
            {
                KnightAttackDeck.Great => greatAttackBag,
                _ => dominionAttackBag
            };
            int phase = deck == KnightAttackDeck.Great ? GreatBagPhase(npc) : 0;
            if (deck == KnightAttackDeck.Great && phase != greatBagPhase)
            {
                greatBagPhase = phase;
                bag.Clear();
            }

            if (bag.Count == 0)
            {
                FillAttackBag(bag, deck, phase);
            }

            KnightSpecialAttack lastAttack = deck == KnightAttackDeck.Great
                ? lastGreatAttack
                : lastDominionAttack;
            Span<int> availableIndices = stackalloc int[bag.Count];
            int availableCount = CollectEligibleBagIndices(bag, candidates, count, lastAttack,
                availableIndices, avoidImmediateRepeat: true);
            if (availableCount == 0)
            {
                availableCount = CollectEligibleBagIndices(bag, candidates, count, lastAttack,
                    availableIndices, avoidImmediateRepeat: false);
            }
            if (availableCount == 0)
            {
                bag.Clear();
                FillAttackBag(bag, deck, phase);
                availableIndices = stackalloc int[bag.Count];
                availableCount = CollectEligibleBagIndices(bag, candidates, count, lastAttack,
                    availableIndices, avoidImmediateRepeat: true);
            }
            if (availableCount == 0)
            {
                availableCount = CollectEligibleBagIndices(bag, candidates, count, lastAttack,
                    availableIndices, avoidImmediateRepeat: false);
            }

            int selectedIndex = availableIndices[Main.rand.Next(availableCount)];
            KnightSpecialAttack selected = bag[selectedIndex];
            bag.RemoveAt(selectedIndex);
            if (deck == KnightAttackDeck.Great)
            {
                lastGreatAttack = selected;
            }
            else
            {
                lastDominionAttack = selected;
            }
            return selected;
        }

        static int CollectEligibleBagIndices(List<KnightSpecialAttack> bag,
            KnightSpecialAttack[] candidates, int candidateCount, KnightSpecialAttack lastAttack,
            Span<int> destination, bool avoidImmediateRepeat)
        {
            int result = 0;
            for (int bagIndex = 0; bagIndex < bag.Count; bagIndex++)
            {
                KnightSpecialAttack token = bag[bagIndex];
                if (avoidImmediateRepeat && token == lastAttack)
                {
                    continue;
                }
                for (int candidateIndex = 0; candidateIndex < candidateCount; candidateIndex++)
                {
                    if (candidates[candidateIndex] == token)
                    {
                        destination[result++] = bagIndex;
                        break;
                    }
                }
            }
            return result;
        }

        int GreatBagPhase(NPC npc)
        {
            if (ThirdHeraldComplete && npc.life <= npc.lifeMax * 0.4f) return 4;
            if (npc.life <= npc.lifeMax * 0.5f) return 3;
            if (ThirdHeraldComplete) return 2;
            if (HalfHeraldComplete) return 1;
            return 0;
        }

        static void FillAttackBag(List<KnightSpecialAttack> bag, KnightAttackDeck deck, int phase)
        {
            if (deck == KnightAttackDeck.Dominion)
            {
                bag.Add(KnightSpecialAttack.CrimsonAdvance);
                bag.Add(KnightSpecialAttack.CrimsonAdvance);
                bag.Add(KnightSpecialAttack.SpearThrow);
                bag.Add(KnightSpecialAttack.FirebombReversal);
                return;
            }
            bag.Add(KnightSpecialAttack.FirebombReversal);
            bag.Add(KnightSpecialAttack.CrimsonAdvance);
            bag.Add(KnightSpecialAttack.SpearThrow);
            bag.Add(KnightSpecialAttack.RoyalStandard);
            if (phase >= 1)
            {
                bag.Add(KnightSpecialAttack.FurnacePincer);
                bag.Add(KnightSpecialAttack.RedCourtProcession);
            }
            if (phase >= 2)
            {
                bag.Add(KnightSpecialAttack.StormbreakerEdict);
                bag.Add(KnightSpecialAttack.CinderRain);
            }
            if (phase >= 3) bag.Add(KnightSpecialAttack.SpectralHandBarrage);
            if (phase >= 4) bag.Add(KnightSpecialAttack.StormPursuit);
        }

        static bool CanStart(NPC npc, Player target, tsorcRevampGlobalNPC globalNPC)
        {
            return Main.netMode != NetmodeID.MultiplayerClient
                && target.active && !target.dead
                && npc.velocity.Y == 0f
                && npc.ai[1] >= 60f && npc.ai[1] < 120f
                && !globalNPC.CombatMeleeActive
                && !globalNPC.HasPendingCombatComboMove
                && !globalNPC.InCombatComboRecovery
                && !globalNPC.AttackTelegraphing
                && !globalNPC.AttackCommitted
                && globalNPC.StaggerTimer <= 0
                && globalNPC.TeleportCountdown <= 0
                && globalNPC.TeleportAppearanceTimer <= 0
                && globalNPC.DodgeTimer <= 0
                && globalNPC.DodgeRecoveryTimer <= 0
                && globalNPC.PounceTimer <= 0
                && !globalNPC.Fleeing;
        }

        void Begin(NPC npc, Player target, tsorcRevampGlobalNPC globalNPC, KnightSpecialAttack attack)
        {
            if (attack != KnightSpecialAttack.CrimsonTeleportAmbush)
            {
                ClearRedTeleportState(npc, restoreVisibility: false);
            }
            Attack = attack;
            Timer = 0;
            Direction = target.Center.X >= npc.Center.X ? 1 : -1;
            royalSpearThrow = npc.ModNPC is Bosses.SuperHardMode.GreatRedKnight
                && attack == KnightSpecialAttack.SpearThrow;
            redThrowMovement = attack switch
            {
                KnightSpecialAttack.SpearThrow when royalSpearThrow
                    => ChooseGreatThrowMovement(npc, target, Direction),
                KnightSpecialAttack.SpearThrow when npc.ModNPC is Enemies.RedKnight
                    => ChooseRedThrowMovement(npc, target, Direction),
                KnightSpecialAttack.FirebombThrow when npc.ModNPC is Enemies.RedKnight
                    => ChooseRedThrowMovement(npc, target, Direction),
                _ => RedThrowMovement.None
            };
            LockedTarget = attack == KnightSpecialAttack.CrimsonStandard || attack == KnightSpecialAttack.RoyalStandard
                ? LockedTarget
                : target.Center;
            AuxiliaryTargetB = attack == KnightSpecialAttack.RoyalStandard ? AuxiliaryTargetB : Vector2.Zero;
            LockedVelocity = Vector2.Zero;
            emberBombProjectileIndex = -1;
            emberReturnDashTimer = -1;
            crimsonClearanceDelay = 0;
            ArenaBaseRotation = Main.rand.NextFloat(MathHelper.Pi / 12f);
            ArenaRotationDirection = Main.rand.NextBool() ? 1 : -1;
            npc.direction = Direction;
            npc.spriteDirection = Direction;
            npc.ai[1] = 60f;
            npc.ai[2] = -100f;
            if (attack != KnightSpecialAttack.CrimsonStandard && attack != KnightSpecialAttack.RoyalStandard)
            {
                npc.velocity.X *= 0.25f;
            }
            if (IsMobileMeleeAttack(attack))
            {
                float entrySpeed = npc.ModNPC is Bosses.SuperHardMode.GreatRedKnight ? 1.5f : 1f;
                if (npc.velocity.X * Direction < entrySpeed)
                {
                    npc.velocity.X = Direction * entrySpeed;
                }
            }
            globalNPC.ResetCombatTempoSequence(clearRecovery: true);
            globalNPC.AttackTelegraphing = true;
            globalNPC.AttackCommitted = false;
            if (attack == KnightSpecialAttack.CrimsonDominion)
            {
                // One-way latch. From here the knight is in Dominion for the rest of the fight, and
                // the lightning loop's clock starts NOW — which is what puts the first Stage A
                // bolts inside the 300t plant-and-hold.
                DominionEngaged = true;
                DominionSequenceTimer = 0;
            }
            npc.netUpdate = true;
        }

        static Color TelegraphColor(KnightSpecialAttack attack)
        {
            return attack switch
            {
                KnightSpecialAttack.PoisonSpearTether => Color.GreenYellow,
                KnightSpecialAttack.PoisonArcVolley => Color.GreenYellow,
                KnightSpecialAttack.PoisonRain => Color.GreenYellow,
                KnightSpecialAttack.PoisonCurtain => Color.GreenYellow,
                KnightSpecialAttack.SpectralHandBarrage => Color.White,
                // Stormbreaker and the Storm Herald both used a cyan/ice-blue accent that no longer
                // matches anything they draw — every lightning in the kit is crimson now.
                KnightSpecialAttack.StormbreakerEdict => new Color(226, 40, 52),
                KnightSpecialAttack.StormHerald => new Color(206, 16, 34),
                KnightSpecialAttack.CinderRain => new Color(245, 52, 108),
                KnightSpecialAttack.StormPursuit => new Color(226, 40, 52),
                _ => Color.OrangeRed
            };
        }

        public bool Tick(NPC npc, Player target, KnightAttackStats stats)
        {
            if (!Active)
            {
                return false;
            }

            tsorcRevampGlobalNPC globalNPC = npc.GetGlobalNPC<tsorcRevampGlobalNPC>();
            if (!target.active || target.dead || globalNPC.StaggerTimer > 0)
            {
                Cancel(npc, globalNPC);
                return true;
            }

            npc.direction = Direction;
            npc.spriteDirection = Direction;
            npc.knockBackResist = 0f;
            npc.velocity.Y = Math.Min(npc.velocity.Y + 0.35f, 10f);
            SetCombatFlags(npc, globalNPC);
            npc.knockBackResist = globalNPC.AttackCommitted ? 0f : globalNPC.BaseKnockBackResist;
            if (Timer == CommitStart(Attack))
            {
                tsorcRevampAIs.SpawnTelegraphFlash(npc, TelegraphColor(Attack));
            }

            switch (Attack)
            {
                case KnightSpecialAttack.FirebombReversal:
                    TickFirebombReversal(npc, target, stats);
                    break;
                case KnightSpecialAttack.PoisonSpearTether:
                    TickPoisonSpearTether(npc, target, stats);
                    break;
                case KnightSpecialAttack.SpearThrow:
                    TickSpearThrow(npc, target, stats);
                    break;
                case KnightSpecialAttack.PoisonArcVolley:
                    TickPoisonArcVolley(npc, target, stats);
                    break;
                case KnightSpecialAttack.FirebombThrow:
                    TickFirebombThrow(npc, target, stats);
                    break;
                case KnightSpecialAttack.SpectralHandBarrage:
                    TickSpectralHandBarrage(npc, target, stats);
                    break;
                case KnightSpecialAttack.PoisonRain:
                    TickPoisonRain(npc, stats);
                    break;
                case KnightSpecialAttack.PoisonCurtain:
                    TickPoisonCurtain(npc, target, stats);
                    break;
                case KnightSpecialAttack.CrimsonTeleportAmbush:
                    TickCrimsonTeleportAmbush(npc, target, stats);
                    break;
                case KnightSpecialAttack.CrimsonStandard:
                    TickCrimsonStandard(npc, stats);
                    break;
                case KnightSpecialAttack.CrimsonAdvance:
                    TickCrimsonAdvance(npc, target, stats);
                    break;
                case KnightSpecialAttack.FurnacePincer:
                    TickFurnacePincer(npc, target, stats);
                    break;
                case KnightSpecialAttack.RoyalStandard:
                    TickRoyalStandard(npc, target, stats);
                    break;
                case KnightSpecialAttack.StormbreakerEdict:
                    TickStormbreaker(npc, target, stats);
                    break;
                case KnightSpecialAttack.RedCourtProcession:
                    TickRedCourtProcession(npc, target, stats);
                    break;
                case KnightSpecialAttack.CrimsonDominion:
                    TickCrimsonDominion(npc, stats);
                    break;
                case KnightSpecialAttack.FurnaceHerald:
                case KnightSpecialAttack.StormHerald:
                    TickHerald(npc, Attack == KnightSpecialAttack.StormHerald, stats);
                    break;
                case KnightSpecialAttack.CinderRain:
                    TickCinderRain(npc, target, stats);
                    break;
                case KnightSpecialAttack.StormPursuit:
                    TickStormPursuit(npc, target);
                    break;
            }

            // Standards own the knight only through the throw. Their planted charge and delayed
            // projectiles are self-contained, so the knight can immediately resume normal movement
            // and attack selection while the standard resolves independently.
            if (!Active)
            {
                return true;
            }

            Timer++;
            if (Main.netMode != NetmodeID.MultiplayerClient && Timer % 60 == 0
                && !(Attack == KnightSpecialAttack.CrimsonAdvance && crimsonClearanceDelay > 0))
            {
                npc.netUpdate = true;
            }

            // The bomb impact owns the return dash. Once its 16-tick hit window and a short
            // punish window have elapsed, release the knight instead of idling until the safety cap.
            if (Attack == KnightSpecialAttack.FirebombReversal
                && emberReturnDashTimer >= 0
                && Timer >= emberReturnDashTimer + 28)
            {
                Finish(npc, globalNPC);
                return true;
            }

            int duration = Duration(Attack);
            if (Timer >= duration)
            {
                Finish(npc, globalNPC);
            }
            return true;
        }

        void TickFirebombReversal(NPC npc, Player target, KnightAttackStats stats)
        {
            if (Timer < 45)
            {
                float telegraphSpeed = npc.ModNPC is Bosses.SuperHardMode.GreatRedKnight ? 2f : 1.35f;
                ApproachHorizontalSpeed(npc, Direction, telegraphSpeed, 0.1f);
            }
            if (Timer == 45)
            {
                SpawnLunge(npc, stats.SpearDamage, 70f, 48f, 12, 3.5f);
                npc.velocity = new Vector2(Direction * 4.2f, -2.2f);
                PlaySound(SoundID.Item1 with { Volume = 0.8f }, npc.Center);
            }
            if (Timer == 65)
            {
                npc.velocity = new Vector2(-Direction * 4.1f, -4.8f);
                LockedTarget = target.Center;
                npc.netUpdate = true;
            }
            if (Timer > 78 && Timer < 120)
            {
                ApproachHorizontalSpeed(npc, -Direction, 1.2f, 0.16f);
            }
            if (Timer == 120 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Vector2 velocity = UsefulFunctions.BallisticTrajectory(npc.Center, LockedTarget, 15f, 0.2f, highAngle: false, fallback: true);
                emberBombProjectileIndex = Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, velocity,
                    ModContent.ProjectileType<EnemyFirebomb>(), stats.BombDamage, 0f, Main.myPlayer, ai2: 1f);
                PlaySound(SoundID.Item1 with { Volume = 0.9f, Pitch = -0.35f }, npc.Center);
            }

            if (Timer > 120 && emberReturnDashTimer < 0)
            {
                ApproachHorizontalSpeed(npc, -Direction, 0.8f, 0.12f);
            }
            if (Main.netMode != NetmodeID.MultiplayerClient && Timer > 120 && emberReturnDashTimer < 0)
            {
                bool validBomb = emberBombProjectileIndex >= 0 && emberBombProjectileIndex < Main.maxProjectiles
                    && Main.projectile[emberBombProjectileIndex].active
                    && Main.projectile[emberBombProjectileIndex].type == ModContent.ProjectileType<EnemyFirebomb>()
                    && Main.projectile[emberBombProjectileIndex].GetGlobalProjectile<tsorcGlobalProjectile>()
                        .TryGetSourceNPC(out NPC bombSource)
                    && bombSource.whoAmI == npc.whoAmI;
                bool bombDetonating = validBomb && Main.projectile[emberBombProjectileIndex].timeLeft <= 2;
                bool fallbackDetonation = Timer >= 165;
                if (bombDetonating || !validBomb || fallbackDetonation)
                {
                    Vector2 detonationPoint = validBomb
                        ? Main.projectile[emberBombProjectileIndex].Center
                        : LockedTarget;
                    if (validBomb && fallbackDetonation && Main.projectile[emberBombProjectileIndex].timeLeft > 2)
                    {
                        Main.projectile[emberBombProjectileIndex].timeLeft = 2;
                        Main.projectile[emberBombProjectileIndex].netUpdate = true;
                    }
                    TriggerEmberReturnDash(npc, detonationPoint, stats.SpearDamage);
                }
            }
        }

        void TriggerEmberReturnDash(NPC npc, Vector2 detonationPoint, int damage)
        {
            Direction = detonationPoint.X >= npc.Center.X ? 1 : -1;
            npc.direction = Direction;
            npc.spriteDirection = Direction;
            emberReturnDashTimer = Timer;
            bool greatKnight = npc.ModNPC is Bosses.SuperHardMode.GreatRedKnight;
            float dashSpeed = greatKnight ? 10f : 7.5f;
            npc.velocity = new Vector2(Direction * dashSpeed, -2.8f);
            SpawnLunge(npc, damage, greatKnight ? 88f : 76f, 50f, 16, 4f);
            npc.GetGlobalNPC<tsorcRevampGlobalNPC>().AttackCommitted = true;
            PlaySound(SoundID.Item1 with { Volume = 0.95f, Pitch = 0.05f }, npc.Center);
            npc.netUpdate = true;
        }

        void TickPoisonSpearTether(NPC npc, Player target, KnightAttackStats stats)
        {
            if (Timer < 55)
            {
                ApproachHorizontalSpeed(npc, Direction, 2.4f, 0.12f);
            }
            else if (Timer < 120)
            {
                ApproachHorizontalSpeed(npc, Direction, 1.1f, 0.08f);
            }
            else if (Timer < 148)
            {
                float gather = (Timer - 120f) / 28f;
                ApproachHorizontalSpeed(npc, Direction, MathHelper.Lerp(1.6f, 5.2f, gather), 0.22f);
            }

            if (Timer == 30)
            {
                const float gravity = 0.26f;
                const int flightTicks = 36;
                Vector2 displacement = AuxiliaryTargetA - npc.Center;
                LockedVelocity = new Vector2(displacement.X / flightTicks,
                    (displacement.Y - 0.5f * gravity * flightTicks * flightTicks) / flightTicks);
                npc.netUpdate = true;
            }
            if (Timer == 55)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, LockedVelocity,
                        ModContent.ProjectileType<RedKnightAdderSpear>(), stats.MagicDamage, 0f, Main.myPlayer,
                        npc.whoAmI, AuxiliaryTargetA.X, AuxiliaryTargetA.Y);
                }
                PlaySound(SoundID.Item1 with { Volume = 0.88f, Pitch = -0.15f }, npc.Center);
                npc.netUpdate = true;
            }
            if (Timer >= 92 && Timer < 148 && Main.rand.NextBool(3))
            {
                Dust mote = Dust.NewDustPerfect(npc.Center + new Vector2(Direction * 22f, -8f),
                    DustID.CursedTorch, Main.rand.NextVector2Circular(0.4f, 0.4f), 120,
                    new Color(142, 210, 24), Main.rand.NextFloat(0.65f, 0.9f));
                mote.noGravity = true;
            }
            if (Timer == 148)
            {
                npc.velocity = new Vector2(Direction * 7.5f, -2.4f);
                SpawnLunge(npc, stats.SpearDamage, 84f, 50f, 18, 4f);
                npc.GetGlobalNPC<tsorcRevampGlobalNPC>().AttackCommitted = true;
                PlaySound(SoundID.Item1 with { Volume = 0.95f, Pitch = 0.08f }, npc.Center);
                npc.netUpdate = true;
            }
        }

        void TickSpearThrow(NPC npc, Player target, KnightAttackStats stats)
        {
            if (Timer == 15)
            {
                LockedTarget = target.Center;
                Direction = LockedTarget.X >= npc.Center.X ? 1 : -1;
                npc.netUpdate = true;
            }
            RunRedThrowMovement(npc, LockedTarget, 15, 45, 1.5f, 2.1f, 0.1f);
            if (Timer >= 15 && Timer <= 45)
            {
                // The knight is allowed to move after committing its aim. Re-solving from its
                // actual position keeps both the held-spear angle and the release trajectory exact.
                Vector2 launchSource = SpearLaunchSource(npc, Direction);
                float speed = royalSpearThrow
                    ? (Vector2.Distance(launchSource, LockedTarget) > 440f ? 13.25f : 9.5f)
                    : 12.5f;
                LockedVelocity = UsefulFunctions.BallisticTrajectory(launchSource, LockedTarget,
                    speed, fallback: true);
            }
            if (Timer == 45 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Vector2 launchSource = SpearLaunchSource(npc, Direction);
                int projectileType = royalSpearThrow
                    ? ModContent.ProjectileType<EnemyAncientBloodLanceProj>()
                    : ModContent.ProjectileType<BlackKnightSpear>();
                Projectile.NewProjectile(npc.GetSource_FromThis(), launchSource, LockedVelocity,
                    projectileType, stats.SpearDamage, 0f, Main.myPlayer, ai2: 1f);
                tsorcRevampAIs.RegisterFighterAttack(npc);
                PlaySound(SoundID.Item1 with { Volume = 0.82f, PitchVariance = 0.08f }, npc.Center);
            }
        }

        void TickPoisonArcVolley(NPC npc, Player target, KnightAttackStats stats)
        {
            ApproachHorizontalSpeed(npc, Direction, Timer < 60 ? 1.15f : 2f, 0.08f);
            if (Timer == 30)
            {
                LockedTarget = target.Center;
                Direction = LockedTarget.X >= npc.Center.X ? 1 : -1;
                npc.netUpdate = true;
            }
            if (Timer == 60 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                const int projectileCount = 4;
                const float spread = MathHelper.Pi / 6f;
                Vector2 baseVelocity = UsefulFunctions.BallisticTrajectory(npc.Center, LockedTarget,
                    4.6f, 1.1f, highAngle: true, fallback: true);
                for (int i = 0; i < projectileCount; i++)
                {
                    float angle = i * spread - spread * (projectileCount - 1) * 0.5f;
                    Vector2 velocity = baseVelocity.RotatedBy(angle);
                    if (Math.Sign(velocity.X) == Direction)
                    {
                        Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, velocity,
                            ModContent.ProjectileType<EnemySpellAbyssPoisonStrikeBall>(), stats.MagicDamage,
                            0f, Main.myPlayer, ai2: 1f);
                    }
                }
                tsorcRevampAIs.RegisterFighterAttack(npc);
                PlaySound(SoundID.Item20 with { Volume = 0.62f, Pitch = 0.08f }, npc.Center);
            }
        }

        void TickFirebombThrow(NPC npc, Player target, KnightAttackStats stats)
        {
            if (Timer == 30)
            {
                LockedTarget = target.Center;
                Direction = LockedTarget.X >= npc.Center.X ? 1 : -1;
                PlaySound(UsefulFunctions.BombFuse with { Volume = 0.6f }, npc.Center);
                npc.netUpdate = true;
            }
            RunRedThrowMovement(npc, LockedTarget, 30, 60, 1.3f, 2.2f, 0.09f);
            if (Timer >= 30 && Timer <= 60)
            {
                LockedVelocity = UsefulFunctions.BallisticTrajectory(npc.Center, LockedTarget,
                    15f, 0.2f, highAngle: false, fallback: true);
            }
            if (Timer == 60 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, LockedVelocity,
                    ModContent.ProjectileType<EnemyFirebomb>(), stats.BombDamage, 0f,
                    Main.myPlayer, ai2: 1f);
                tsorcRevampAIs.RegisterFighterAttack(npc);
                PlaySound(SoundID.Item1 with { Volume = 0.9f, Pitch = -0.4f }, npc.Center);
            }
        }

        void TickSpectralHandBarrage(NPC npc, Player target, KnightAttackStats stats)
        {
            ApproachHorizontalSpeed(npc, Direction, Timer < 90 ? 0.9f : 1.8f, 0.08f);
            Lighting.AddLight(npc.Center, Color.WhiteSmoke.ToVector3() * 1.25f);
            if (Timer == 30)
            {
                LockedTarget = target.Center;
                Direction = LockedTarget.X >= npc.Center.X ? 1 : -1;
                npc.netUpdate = true;
            }
            bool greatKnight = npc.ModNPC is Bosses.SuperHardMode.GreatRedKnight;
            bool spawnHand = Timer >= 60 && Timer < 90
                && (!greatKnight || (Timer - 60) % 2 == 0);
            if (spawnHand && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Vector2 velocity = UsefulFunctions.BallisticTrajectory(npc.Center, LockedTarget, 6f,
                    fallback: true) + Main.rand.NextVector2Circular(4f, 4f);
                int projectileType = greatKnight
                    ? ModContent.ProjectileType<GreatRedKnightUltrakillHand>()
                    : ProjectileID.InsanityShadowHostile;
                float handIndex = greatKnight ? (Timer - 60) / 2f : 0f;
                Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, velocity,
                    projectileType, stats.GreatDamage, 0f, Main.myPlayer, ai0: handIndex);
                if (Timer == 60)
                {
                    tsorcRevampAIs.RegisterFighterAttack(npc);
                }
                if (Timer % 5 == 0)
                {
                    PlaySound(SoundID.Item69 with { Volume = 0.45f, PitchVariance = 0.35f }, npc.Center);
                }
            }
        }

        void TickPoisonRain(NPC npc, KnightAttackStats stats)
        {
            ApproachHorizontalSpeed(npc, Direction, Timer < 60 ? 1f : 1.8f, 0.08f);

            if (Timer < 60 && Main.rand.NextBool(3))
            {
                Vector2 gatherPoint = npc.Center + new Vector2(Direction * 18f, -8f);
                Dust mote = Dust.NewDustPerfect(gatherPoint + Main.rand.NextVector2Circular(10f, 10f),
                    DustID.CursedTorch, Main.rand.NextVector2Circular(0.3f, 0.3f), 120,
                    new Color(132, 196, 24), Main.rand.NextFloat(0.65f, 0.95f));
                mote.noGravity = true;
            }

            if (Timer == 60)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, Vector2.Zero,
                        ModContent.ProjectileType<RedKnightPoisonRainController>(), stats.MagicDamage,
                        0f, Main.myPlayer);
                    tsorcRevampAIs.RegisterFighterAttack(npc);
                }
                PlaySound(SoundID.Item20 with { Volume = 0.72f, Pitch = -0.18f }, npc.Center);
                npc.netUpdate = true;
            }
        }

        const int CinderRainFirstWaveTick = 60;
        const int CinderRainWaveInterval = 30;
        const int CinderRainWaveCount = 4;
        const float CinderRainSpawnHeight = 550f;
        const float CinderRainExpireHeight = 104f;

        void TickCinderRain(NPC npc, Player target, KnightAttackStats stats)
        {
            ApproachHorizontalSpeed(npc, Direction, Timer < CinderRainFirstWaveTick ? 1.15f : 2f,
                0.09f);

            if (Timer == 30)
            {
                // Prefer the fire-time position, but retain the selection-time validated point if
                // the player crosses under a low ceiling during the windup.
                if (HasUsableCinderRainLane(target.Center))
                {
                    LockedTarget = target.Center;
                }
                Direction = LockedTarget.X >= npc.Center.X ? 1 : -1;
                npc.netUpdate = true;
            }

            if (Timer < CinderRainFirstWaveTick && !Main.dedServ && Main.rand.NextBool(2))
            {
                Vector2 gatherPoint = npc.Center + new Vector2(Direction * 18f, -8f);
                Dust mote = Dust.NewDustPerfect(
                    gatherPoint + Main.rand.NextVector2CircularEdge(14f, 14f),
                    Main.rand.NextBool(4) ? DustID.Shadowflame : DustID.RedTorch,
                    Main.rand.NextVector2Circular(0.25f, 0.25f), 110,
                    new Color(232, 38, 92), Main.rand.NextFloat(0.55f, 0.9f));
                mote.noGravity = true;
                mote.noLight = mote.type == DustID.Shadowflame;
            }

            if (Timer >= CinderRainFirstWaveTick
                && Timer < CinderRainFirstWaveTick + CinderRainWaveCount * CinderRainWaveInterval
                && (Timer - CinderRainFirstWaveTick) % CinderRainWaveInterval == 0)
            {
                int wave = (Timer - CinderRainFirstWaveTick) / CinderRainWaveInterval;
                SpawnCinderRainWave(npc, stats.MagicDamage, wave);
                PlaySound(SoundID.Item34 with
                {
                    Volume = 0.35f,
                    Pitch = -0.18f + wave * 0.06f
                }, npc.Center);
                if (wave == 0)
                {
                    tsorcRevampAIs.RegisterFighterAttack(npc);
                }
            }
        }

        void SpawnCinderRainWave(NPC npc, int damage, int wave)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            float[] laneOffsets = { -150f, 0f, 150f };
            for (int lane = 0; lane < laneOffsets.Length; lane++)
            {
                float offset = laneOffsets[lane];
                if (!TryFindCinderRainOrigin(LockedTarget, offset, out Vector2 origin))
                {
                    continue;
                }
                float waveDrift = (wave - (CinderRainWaveCount - 1) * 0.5f) * 0.12f;
                Vector2 velocity = new(Main.rand.NextFloat(-0.65f, 0.65f) + waveDrift, 7.1f);
                Projectile.NewProjectile(npc.GetSource_FromThis(), origin, velocity,
                    ModContent.ProjectileType<CinderRainDrop>(), damage, 2f, Main.myPlayer,
                    ai0: LockedTarget.Y - CinderRainExpireHeight);
            }
        }

        void TickStormPursuit(NPC npc, Player target)
        {
            ApproachHorizontalSpeed(npc, Direction, Timer < 90 ? 1.4f : 2.35f, 0.1f);
            if (Timer == 30)
            {
                LockedTarget = target.Center;
                Direction = LockedTarget.X >= npc.Center.X ? 1 : -1;
                LockedVelocity = (LockedTarget - npc.Center).SafeNormalize(new Vector2(Direction, 0f));
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, LockedVelocity,
                        ModContent.ProjectileType<JellyfishLightning>(), 30, 1f, Main.myPlayer,
                        ai1: npc.whoAmI);
                    tsorcRevampAIs.RegisterFighterAttack(npc);
                }
                npc.netUpdate = true;
            }
        }

        void TickPoisonCurtain(NPC npc, Player target, KnightAttackStats stats)
        {
            ApproachHorizontalSpeed(npc, Direction, Timer < 60 ? 1.1f : 1.9f, 0.08f);

            if (Timer == 30)
            {
                LockedTarget = target.Center;
                Direction = LockedTarget.X >= npc.Center.X ? 1 : -1;
                npc.netUpdate = true;
            }

            if (Timer < 60 && Main.rand.NextBool(3))
            {
                Vector2 gatherPoint = npc.Center + new Vector2(Direction * 18f, -8f);
                Dust mote = Dust.NewDustPerfect(gatherPoint + Main.rand.NextVector2Circular(10f, 10f),
                    DustID.CursedTorch, Main.rand.NextVector2Circular(0.35f, 0.35f), 120,
                    new Color(146, 204, 26), Main.rand.NextFloat(0.7f, 1f));
                mote.noGravity = true;
            }

            if (Timer == 60)
            {
                // Fire-time lock: the sweep is thereafter fixed and cannot follow the player.
                LockedTarget = target.Center;
                if (Main.netMode != NetmodeID.MultiplayerClient
                    && RedKnightPoisonCurtainController.HasUsableCurtain(LockedTarget))
                {
                    int sweepDirection = Main.rand.NextBool() ? 1 : -1;
                    Projectile.NewProjectile(npc.GetSource_FromThis(), LockedTarget, Vector2.Zero,
                        ModContent.ProjectileType<RedKnightPoisonCurtainController>(), stats.MagicDamage,
                        0f, Main.myPlayer, ai0: sweepDirection);
                    tsorcRevampAIs.RegisterFighterAttack(npc);
                }
                PlaySound(SoundID.Item20 with { Volume = 0.74f, Pitch = -0.08f }, npc.Center);
                npc.netUpdate = true;
            }
        }

        void TickCrimsonTeleportAmbush(NPC npc, Player target, KnightAttackStats stats)
        {
            int actualMarkerTick = RedTeleportActualMarkerTick;
            int snapTick = RedTeleportSnapTick;

            if (Timer == 0)
            {
                // Departure is deliberately brief. The destination supplies the meaningful dodge
                // information, especially for a melee arrival beside the player.
                SpawnRedTeleportMarker(npc, npc.Center, RedTeleportEntryTellTicks, burst: true);
                PlaySound(SoundID.Item74 with { Volume = 0.7f, Pitch = -0.45f }, npc.Center);
            }

            if (Timer == RedTeleportEntryTellTicks)
            {
                redTeleportHidden = true;
                npc.alpha = 255;
                npc.dontTakeDamage = true;
                npc.velocity = Vector2.Zero;
                npc.netUpdate = true;
            }

            if (redTeleportHidden)
            {
                npc.velocity = Vector2.Zero;
            }

            if (Timer >= RedTeleportEntryTellTicks
                && (Timer - RedTeleportEntryTellTicks) % RedTeleportFeintIntervalTicks == 0)
            {
                int markerIndex = (Timer - RedTeleportEntryTellTicks) / RedTeleportFeintIntervalTicks;
                if (markerIndex <= redTeleportFakeCount)
                {
                    bool actual = markerIndex == redTeleportFakeCount;
                    Vector2 markerPosition = actual
                        ? redTeleportDestination
                        : markerIndex == 0 ? redTeleportFakeA : redTeleportFakeB;
                    int markerLifetime = actual ? RedTeleportArrivalTellTicks : RedTeleportFeintIntervalTicks;
                    SpawnRedTeleportMarker(npc, markerPosition, markerLifetime, burst: actual);
                    if (!actual && Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Projectile.NewProjectile(npc.GetSource_FromThis(), markerPosition, Vector2.Zero,
                            ModContent.ProjectileType<RedKnightTeleportFeintBlast>(),
                            Math.Max(1, (int)(stats.MagicDamage * 0.75f)), 2f, Main.myPlayer);
                    }
                    PlaySound(SoundID.Item74 with
                    {
                        Volume = actual ? 0.82f : 0.58f,
                        Pitch = actual ? -0.1f : 0.18f
                    }, markerPosition);

                    if (actual)
                    {
                        // Aim is committed when the true destination appears, not when RK vanishes.
                        // Close arrivals therefore expose the complete 60-tick attack line.
                        LockedTarget = target.Center;
                        Direction = LockedTarget.X >= redTeleportDestination.X ? 1 : -1;
                        npc.netUpdate = true;
                    }
                }
            }

            if (Timer == snapTick)
            {
                Vector2 oldCenter = npc.Center;
                npc.Center = redTeleportDestination;
                redTeleportHidden = false;
                npc.alpha = 0;
                npc.dontTakeDamage = false;
                Direction = LockedTarget.X >= npc.Center.X ? 1 : -1;
                npc.direction = Direction;
                npc.spriteDirection = Direction;
                SpawnRedTeleportMarker(npc, oldCenter, 14, burst: false);
                SpawnRedTeleportMarker(npc, npc.Center, 18, burst: false);
                PlaySound(SoundID.Item8 with { Volume = 0.9f, Pitch = -0.15f }, npc.Center);

                if (redTeleportPattern == RedTeleportPattern.RetreatThrow)
                {
                    LockedVelocity = UsefulFunctions.BallisticTrajectory(npc.Center, LockedTarget,
                        12.5f, fallback: true);
                    npc.velocity = new Vector2(Direction * 0.8f, -1.4f);
                }
                else
                {
                    npc.velocity = new Vector2(Direction * 7.5f, -2.6f);
                    SpawnLunge(npc, stats.SpearDamage, 82f, 50f,
                        RedTeleportPincerActiveTicks, 4f);
                    tsorcRevampAIs.RegisterFighterAttack(npc);
                    PlaySound(SoundID.Item1 with { Volume = 0.95f, Pitch = 0.08f }, npc.Center);
                }
                npc.netUpdate = true;
            }

            if (redTeleportPattern == RedTeleportPattern.RetreatThrow
                && Timer == RedTeleportDamageTick
                && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, LockedVelocity,
                    ModContent.ProjectileType<BlackKnightSpear>(), stats.SpearDamage, 0f,
                    Main.myPlayer, ai2: 1f);
                tsorcRevampAIs.RegisterFighterAttack(npc);
                PlaySound(SoundID.Item1 with { Volume = 0.88f, Pitch = -0.08f }, npc.Center);
            }
        }

        void TickCrimsonStandard(NPC npc, KnightAttackStats stats)
        {
            if (Timer == 45)
            {
                float horizontalVelocity = Direction * npc.velocity.X >= 0.8f
                    ? npc.velocity.X
                    : Direction * 0.8f;
                npc.velocity = new Vector2(horizontalVelocity, -5.2f);
                npc.netUpdate = true;
            }
            if (Timer == 75)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    SpawnStandard(npc, LockedTarget, stats.MagicDamage, KnightStandardMode.RedKnight);
                }
                PlaySound(SoundID.Item1 with { Volume = 0.85f, Pitch = -0.1f }, npc.Center);
                ReleaseDetachedStandard(npc);
            }
        }

        void TickCrimsonAdvance(NPC npc, Player target, KnightAttackStats stats)
        {
            if (Timer < 60)
            {
                float progress = MathHelper.Clamp(Timer / 59f, 0f, 1f);
                ApproachHorizontalSpeed(npc, Direction, MathHelper.Lerp(1.5f, 5.5f, progress),
                    MathHelper.Lerp(0.1f, 0.24f, progress));
            }
            if (Timer == 40)
            {
                LockedTarget = target.Center;
                Direction = LockedTarget.X >= npc.Center.X ? 1 : -1;
                npc.netUpdate = true;
            }
            if (Timer < 60)
            {
                TryGainCrimsonAdvanceClearance(npc, Direction);
            }
            if (Timer == 60)
            {
                if (!HasLungeClearance(npc, Direction, 108f))
                {
                    ApproachHorizontalSpeed(npc, Direction, 4.5f, 0.24f);
                    TryGainCrimsonAdvanceClearance(npc, Direction);
                    if (++crimsonClearanceDelay <= 45)
                    {
                        // Tick() increments after this method; stepping back holds the strike frame
                        // until the body-width corridor clears without freezing locomotion.
                        Timer--;
                    }
                    else
                    {
                        // An actual wall is not a valid lunge. Skip this hit and let the combo move on.
                        Timer = 79;
                        crimsonClearanceDelay = 0;
                    }
                    return;
                }

                crimsonClearanceDelay = 0;
                // Leonhard-style dash: a committed horizontal burst with a small hop, rather than
                // a ballistic solve that continuously changes shape with target height/distance.
                LockedVelocity = LeonhardDashVelocity(npc.Center, LockedTarget);
                SpawnLunge(npc, stats.SpearDamage, 108f, 52f, 20, 5f);
                npc.velocity = LockedVelocity;
                PlaySound(SoundID.Item1 with { Volume = 1f, Pitch = -0.15f }, npc.Center);
                npc.netUpdate = true;
            }
            if (Timer > 80 && Timer < 105)
            {
                ApproachHorizontalSpeed(npc, Direction, 1.2f, 0.28f);
            }
            if (Timer == 105)
            {
                Direction = target.Center.X >= npc.Center.X ? 1 : -1;
                LockedTarget = target.Center;
                crimsonClearanceDelay = 0;
                npc.netUpdate = true;
            }
            if (Timer > 105 && Timer < 135)
            {
                float progress = MathHelper.Clamp((Timer - 105f) / 29f, 0f, 1f);
                ApproachHorizontalSpeed(npc, Direction, MathHelper.Lerp(1.2f, 4.5f, progress),
                    MathHelper.Lerp(0.12f, 0.22f, progress));
                TryGainCrimsonAdvanceClearance(npc, Direction);
            }
            if (Timer == 135)
            {
                if (!HasLungeClearance(npc, Direction, 96f))
                {
                    ApproachHorizontalSpeed(npc, Direction, 4.2f, 0.22f);
                    TryGainCrimsonAdvanceClearance(npc, Direction);
                    if (++crimsonClearanceDelay <= 45)
                    {
                        Timer--;
                    }
                    else
                    {
                        Timer = 156;
                        crimsonClearanceDelay = 0;
                    }
                    return;
                }

                crimsonClearanceDelay = 0;
                LockedVelocity = LeonhardDashVelocity(npc.Center, LockedTarget);
                SpawnLunge(npc, stats.GreatDamage, 96f, 54f, 22, 5.5f);
                npc.velocity = LockedVelocity;
                PlaySound(SoundID.Item1 with { Volume = 1f, Pitch = 0.05f }, npc.Center);
                npc.netUpdate = true;
            }
            if (Timer > 157)
            {
                ApproachHorizontalSpeed(npc, Direction, 1.2f, 0.22f);
            }
        }

        void TickFurnacePincer(NPC npc, Player target, KnightAttackStats stats)
        {
            if (Timer < 60)
            {
                ApproachHorizontalSpeed(npc, -Direction, 0.85f, 0.1f);
            }
            if (Timer == 60 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, Vector2.Zero,
                    ModContent.ProjectileType<RedKnightDelayedBomb>(), stats.GreatDamage, 0f, Main.myPlayer,
                    AuxiliaryTargetA.X, AuxiliaryTargetA.Y, 1f);
                PlaySound(UsefulFunctions.BombFuse with { Volume = 0.75f }, npc.Center);
            }
            if (Timer == 125)
            {
                LockedTarget = target.Center;
                Direction = LockedTarget.X >= npc.Center.X ? 1 : -1;
                npc.netUpdate = true;
            }
            if (Timer > 125 && Timer < 165)
            {
                float progress = MathHelper.Clamp((Timer - 125f) / 39f, 0f, 1f);
                ApproachHorizontalSpeed(npc, Direction, MathHelper.Lerp(1.2f, 4.2f, progress),
                    MathHelper.Lerp(0.1f, 0.2f, progress));
            }
            if (Timer == 165)
            {
                LockedVelocity = GuidedLeapVelocity(npc.Center, LockedTarget, 36);
                SpawnLunge(npc, stats.GreatDamage, 112f, 54f, 38, 6f);
                npc.velocity = LockedVelocity;
                PlaySound(SoundID.Item1 with { Volume = 1f, Pitch = -0.25f }, npc.Center);
                npc.netUpdate = true;
            }
            if (Timer > 203)
            {
                ApproachHorizontalSpeed(npc, Direction, 1.1f, 0.2f);
            }
        }

        const int RoyalFirstThrow = 75;
        const int StormHeraldLaneCountPerSide = 8;
        const int StormHeraldLaneSpacing = 48;
        const int StormHeraldInnermostOffset = 72;
        const int StormHeraldFirstPairTick = 20;
        const int StormHeraldPairInterval = 20;
        const int StormHeraldLaneTelegraphTicks = 20;
        const int StormHeraldLaneActiveTicks = 12;
        const int RedCourtLanceCount = 7;
        const int RedCourtLaneSpacing = 72;
        const int RedCourtFirstPortalTick = 24;
        const int RedCourtPortalInterval = 18;

        void TickRoyalStandard(NPC npc, Player target, KnightAttackStats stats)
        {
            if (Timer == 45)
            {
                float horizontalVelocity = Direction * npc.velocity.X >= 0.6f
                    ? npc.velocity.X
                    : Direction * 0.6f;
                npc.velocity = new Vector2(horizontalVelocity, -6f);
                npc.netUpdate = true;
            }

            if (Timer == RoyalFirstThrow)
            {
                // Capture the landing point on the actual fire tick. RedKnightStandard owns a
                // deterministic ballistic parabola, so this exact point is also its impact.
                if (!TryFindGround(target.Bottom, 10, 30, out Vector2 fireTarget))
                {
                    fireTarget = target.Bottom;
                }
                else
                {
                    // Ground search supplies only the plant height; do not quantize the fire-time
                    // player X coordinate to the centre of a 16px tile.
                    fireTarget.X = target.Bottom.X;
                }
                LockedTarget = fireTarget;
                Direction = LockedTarget.X >= npc.Center.X ? 1 : -1;
                ThrowRoyalSpear(npc, LockedTarget, stats.GreatDamage, KnightStandardMode.GreatCenter, -0.05f);
                ReleaseDetachedStandard(npc);
            }
            else
            {
                // The held lance tracks during windup, but this is presentation-only; the
                // authoritative target is captured above when the projectile is released.
                if (!TryFindGround(target.Bottom, 10, 30, out Vector2 telegraphTarget))
                {
                    telegraphTarget = target.Bottom;
                }
                else
                {
                    telegraphTarget.X = target.Bottom.X;
                }
                LockedTarget = telegraphTarget;
                Direction = LockedTarget.X >= npc.Center.X ? 1 : -1;
            }
        }

        void ThrowRoyalSpear(NPC npc, Vector2 target, int damage, KnightStandardMode mode, float pitch)
        {
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                SpawnStandard(npc, target, damage, mode);
            }
            PlaySound(SoundID.Item1 with { Volume = 1f, Pitch = pitch }, npc.Center);
        }

        void ReleaseDetachedStandard(NPC npc)
        {
            KnightSpecialAttack completed = Attack;
            tsorcRevampGlobalNPC globalNPC = npc.GetGlobalNPC<tsorcRevampGlobalNPC>();
            Attack = KnightSpecialAttack.None;
            Timer = 0;
            LockedVelocity = Vector2.Zero;
            globalNPC.AttackTelegraphing = false;
            globalNPC.AttackCommitted = false;
            globalNPC.SetActiveAttackDefenseTraits(npc, AttackDefenseTraits.None);
            npc.ai[1] = 60f;
            npc.ai[2] = -100f;
            attackCooldown = IsGreatAttack(completed) ? Main.rand.Next(260, 401) : Main.rand.Next(320, 481);
            npc.netUpdate = true;
        }

        // Ground positions for the Stormbreaker lightning strikes, offset from the locked target
        // point. 212px between adjacent marks (was 180) — wide enough for a full dodge-roll-and-stand
        // safe lane between any two bolts, with open space beyond the outer pair. Replaces the old
        // narrow +-20 degree fan of 5 lines radiating from the knight's own body (no room to move).
        static readonly float[] StormboltOffsets = { -318f, -106f, 106f, 318f };

        void TickStormbreaker(NPC npc, Player target, KnightAttackStats stats)
        {
            if (Timer < 60)
            {
                float progress = MathHelper.Clamp(Timer / 59f, 0f, 1f);
                ApproachHorizontalSpeed(npc, Direction, MathHelper.Lerp(1.4f, 4f, progress),
                    MathHelper.Lerp(0.1f, 0.2f, progress));
                if (Timer == 30)
                {
                    LockedTarget = target.Center;
                    Direction = LockedTarget.X >= npc.Center.X ? 1 : -1;
                    npc.netUpdate = true;
                }
            }
            if (Timer == 60)
            {
                SpawnLunge(npc, stats.GreatDamage, 120f, 58f, 16, 6.5f);
                npc.velocity = new Vector2(Direction * 8f, -2.2f);
                PlaySound(SoundID.Item1 with { Volume = 1f, Pitch = -0.35f }, npc.Center);
            }
            if (Timer == 82)
            {
                // Reuses RedKnightLightningLane's existing telegraph/active/fade timeline, spawned
                // vertically at spaced-apart ground marks. The lane now draws the family's own
                // crimson bolt technique instead of Gwyn's storm-BLUE SunlightJudgment column, so
                // this is the standard red lightning the rest of the kit uses.
                SpawnStormboltVolley(npc, LockedTarget, stats.MagicDamage);
                PlaySound(SoundID.Item74 with { Volume = 0.85f, Pitch = -0.2f }, npc.Center);
                ApproachHorizontalSpeed(npc, -Direction, 0.9f, 0.24f);
                npc.netUpdate = true;
            }
            if (Timer > 82)
            {
                ApproachHorizontalSpeed(npc, -Direction, 0.9f, 0.14f);
            }
        }

        static void SpawnStormboltVolley(NPC npc, Vector2 targetPoint, int damage)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }
            foreach (float offset in StormboltOffsets)
            {
                if (!TryFindGround(targetPoint + new Vector2(offset, 0f), 12, 30, out Vector2 groundPoint))
                {
                    continue;
                }
                Projectile.NewProjectile(npc.GetSource_FromThis(), groundPoint, new Vector2(0f, -1f),
                    ModContent.ProjectileType<RedKnightLightningLane>(), damage, 0f, Main.myPlayer,
                    ai0: 46f, ai1: 12f, ai2: 620f);
            }
        }

        /// <summary>
        /// Dominion PHASE 1 — plant &amp; hold, <see cref="DominionHoldTicks"/> (300t / 5s).
        ///
        /// The knight hops, slams its spear into the ground and holds it there, engulfed. On the
        /// beat the plant lands it spawns <see cref="CrimsonDominionController"/> in its CONTAINMENT
        /// mode: the arena-wide damaging "stay inside the safe zone" field builds in and then holds
        /// INDEFINITELY — through the rest of this hold and through all of phase 2's melee. It is
        /// not scoped to phase 1 and has no end time; the death finale is the only thing that takes
        /// it down (the projectile polls GreatRedKnight.InDominionDeathSequence and fades itself).
        /// That mode deliberately does NOT fire the old twelve arena-edge strikes (the Stage A-D
        /// lightning loop already running underneath supersedes them, and both together would
        /// double up the lightning) and does NOT run the escape / nova / fade tail — that drama is
        /// now the death animation (GreatRedKnight.CheckDead). The lightning sequence is already
        /// running by this point (it starts at Begin), so the first two or three Stage A bolts land
        /// during the hold.
        /// </summary>
        void TickCrimsonDominion(NPC npc, KnightAttackStats stats)
        {
            npc.velocity.X = 0f;
            if (Timer == 30)
            {
                npc.velocity.Y = -4.5f;
                npc.netUpdate = true;
            }
            if (Timer == 52)
            {
                npc.velocity.Y = Math.Max(npc.velocity.Y, 4.5f);
            }
            if (Timer == CrimsonDominionController.Phase1SpawnBeat)
            {
                PlaySound(SoundID.Item74 with { Volume = 0.7f, Pitch = -0.65f }, npc.Center);
                tsorcRevampAIs.SpawnTelegraphFlash(npc, new Color(206, 16, 34));
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    // |ai[0]| == 1 selects containment mode (2 would be the finale); its sign
                    // carries the ring's rotation direction, ai[1] the base rotation, and ai[2] the
                    // host index — which containment MUST have, because it polls the host for
                    // InDominionDeathSequence to know when to fade. It runs until then.
                    Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, Vector2.Zero,
                        ModContent.ProjectileType<CrimsonDominionController>(),
                        stats.GreatDamage, 0f, Main.myPlayer,
                        ArenaRotationDirection, ArenaBaseRotation, npc.whoAmI);
                }
            }
            if (Timer >= 60)
            {
                npc.velocity.X = 0f;
            }
            // RK/GRK bodies no longer deal passive contact damage. Dominion's visible containment
            // geometry is the trap; touching the planted humanoid sprite itself is safe and honest.
        }

        // -------------------------------------------------------------------------------------
        // Dominion PHASE 2 — the lightning sequence. Runs every tick from the moment Dominion
        // begins until the knight dies, ALONGSIDE normal combat (it is not a blocking attack).
        //
        // Two deliberately different lightning looks, which is a design requirement, not an
        // oversight:
        //   Stages A/B/C — the "hunting" bolts — use SmallRedLightning, the OLD branching red
        //                  lightning the regular RedKnight already fires at 1/3 health
        //                  (NPCs/Enemies/RedKnight.cs:940). It is a GenericLaser with a texture
        //                  and no custom .fx at all.
        //   Stage D      — the round-1 shader lightning, RedKnightLightningLane (which draws with
        //                  the RedKnightLightningBolt technique in RedKnightDestinedDeath.fx) —
        //                  reserved for the telegraphed straight-down strikes.
        //
        // Loop layout (ticks from Dominion start), repeating forever:
        //   0    .. 1439  Stage A : 12 bolts, one every 120t (2s), each aimed at a player
        //   1440 .. 1619  pause 180t (3s)
        //   1620 .. 1799  Stage B : 3 bolts, one every 60t (1s)
        //   1800 .. 2099  pause 300t (5s)
        //   2100 .. 2639  Stage C : 3 PAIRS, one pair every 180t (3s); one bolt at a player, the
        //                           other at a second player if there is one, else elsewhere
        //   2640 .. 2759  pause 120t (2s)
        //   2760 .. 2859  Stage D : 3 shader bolts straight down, 40t telegraph, uneven X
        // -------------------------------------------------------------------------------------
        public const int DomStageABolts = 12;
        public const int DomStageAInterval = 120;
        public const int DomStageAStart = 0;
        public const int DomPauseA = 180;
        public const int DomStageBBolts = 3;
        public const int DomStageBInterval = 60;
        public const int DomStageBStart = DomStageAStart + DomStageABolts * DomStageAInterval + DomPauseA;   // 1620
        public const int DomPauseB = 300;
        public const int DomStageCPairs = 3;
        public const int DomStageCInterval = 180;
        public const int DomStageCStart = DomStageBStart + DomStageBBolts * DomStageBInterval + DomPauseB;   // 2100
        public const int DomPauseC = 120;
        public const int DomStageDStart = DomStageCStart + DomStageCPairs * DomStageCInterval + DomPauseC;   // 2760
        public const int DomStageDBolts = 3;
        public const int DomStageDTelegraph = 40;
        public const int DomStageDTail = 60;
        public const int DomLoopTicks = DomStageDStart + DomStageDTelegraph + DomStageDTail;                 // 2860

        /// <summary>Ticks the ongoing Dominion lightning loop. Server-authoritative; call every
        /// frame from GreatRedKnight.AI once <see cref="DominionEngaged"/>, whether or not a
        /// blocking special attack happens to be running.</summary>
        public void TickDominionSequence(NPC npc, Player target, KnightAttackStats stats)
        {
            if (!DominionEngaged)
            {
                return;
            }

            // The clock advances on EVERY machine so the debug readout and any client-side visuals
            // stay in step between netUpdates; only the spawns below are server-authoritative.
            int t = DominionSequenceTimer % DomLoopTicks;
            DominionSequenceTimer++;

            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            int damage = stats.GreatDamage;

            // --- Stage A: 12 single hunting bolts, 2s apart -------------------------------------
            if (t < DomStageABolts * DomStageAInterval)
            {
                if (t % DomStageAInterval == 0)
                {
                    SpawnHuntingBolt(npc, PickBoltTarget(npc, target, null), damage);
                }
                return;
            }

            // --- Stage B: 3 single hunting bolts, 1s apart ---------------------------------------
            if (t >= DomStageBStart && t < DomStageBStart + DomStageBBolts * DomStageBInterval)
            {
                if ((t - DomStageBStart) % DomStageBInterval == 0)
                {
                    SpawnHuntingBolt(npc, PickBoltTarget(npc, target, null), damage);
                }
                return;
            }

            // --- Stage C: 3 pairs, 3s apart. Second bolt must land somewhere ELSE ---------------
            if (t >= DomStageCStart && t < DomStageCStart + DomStageCPairs * DomStageCInterval)
            {
                if ((t - DomStageCStart) % DomStageCInterval == 0)
                {
                    Player firstPlayer = target;
                    Vector2 firstPoint = PickBoltTarget(npc, firstPlayer, null);
                    SpawnHuntingBolt(npc, firstPoint, damage);
                    SpawnHuntingBolt(npc, PickBoltTarget(npc, firstPlayer, firstPlayer), damage);
                }
                return;
            }

            // --- Stage D: 3 telegraphed straight-down shader bolts, uneven X ---------------------
            if (t == DomStageDStart)
            {
                SpawnDominionSkyVolley(npc, target, damage);
            }
        }

        /// <summary>
        /// Picks where a hunting bolt should land. With <paramref name="excludePlayer"/> set it
        /// tries to find a DIFFERENT living player first (so a pair in Stage C splits across two
        /// targets in multiplayer); failing that — i.e. solo — it picks a spot elsewhere in the
        /// arena, deliberately offset well clear of the excluded player.
        /// </summary>
        static Vector2 PickBoltTarget(NPC npc, Player primary, Player excludePlayer)
        {
            if (excludePlayer != null)
            {
                int candidateCount = 0;
                Span<int> candidateIndices = stackalloc int[Main.maxPlayers];
                for (int i = 0; i < Main.maxPlayers; i++)
                {
                    Player other = Main.player[i];
                    if (other.active && !other.dead && other.whoAmI != excludePlayer.whoAmI
                        && npc.Distance(other.Center) <= 1400f)
                    {
                        candidateIndices[candidateCount++] = i;
                    }
                }
                if (candidateCount > 0)
                {
                    return Main.player[candidateIndices[Main.rand.Next(candidateCount)]].Center;
                }

                // Solo: land it elsewhere. At least 220px from the player, up to 620px out.
                float offset = Main.rand.NextFloat(220f, 620f) * (Main.rand.NextBool() ? 1f : -1f);
                Vector2 elsewhere = excludePlayer.Center + new Vector2(offset, Main.rand.NextFloat(-80f, 40f));
                return elsewhere;
            }

            return primary.Center;
        }

        /// <summary>
        /// One OLD-style bolt: SmallRedLightning, fired from the knight toward a point, exactly as
        /// NPCs/Enemies/RedKnight.cs:940 already fires it. FollowHost is baked into that class's
        /// SetDefaults, so ai1 must carry the host NPC index; the velocity supplies the aim.
        /// </summary>
        static void SpawnHuntingBolt(NPC npc, Vector2 point, int damage)
        {
            Vector2 aim = UsefulFunctions.Aim(npc.Center, point, 1);
            Projectile.NewProjectileDirect(npc.GetSource_FromThis(), npc.Center, aim,
                ModContent.ProjectileType<Projectiles.Enemy.WyvernMage.SmallRedLightning>(),
                damage, 1f, Main.myPlayer, 0, npc.whoAmI);
        }

        /// <summary>
        /// Stage D: three of the round-1 shader bolts striking straight down, with a 40t telegraph
        /// and DELIBERATELY uneven X positions (a random offset per bolt within its own band, not
        /// an even fan) so the volley never reads as a metronome.
        /// </summary>
        static void SpawnDominionSkyVolley(NPC npc, Player target, int damage)
        {
            // Three overlapping bands across ~1000px of arena, each sampled randomly inside itself.
            // Bands keep the bolts from stacking; the intra-band randomness keeps them irregular.
            Span<float> bandCentres = stackalloc float[DomStageDBolts] { -330f, 20f, 360f };
            for (int i = 0; i < DomStageDBolts; i++)
            {
                float offsetX = bandCentres[i] + Main.rand.NextFloat(-140f, 140f);
                Vector2 probe = target.Bottom + new Vector2(offsetX, 0f);
                if (!TryFindGround(probe, 12, 34, out Vector2 groundPoint))
                {
                    continue;
                }
                // Same spawn form as Stormbreaker Edict's bolts: the lane points UP from the ground
                // point and is drawn as a vertical column, so a "sky strike" is a ground anchor plus
                // an upward direction and a length.
                Projectile.NewProjectile(npc.GetSource_FromThis(), groundPoint, new Vector2(0f, -1f),
                    ModContent.ProjectileType<RedKnightLightningLane>(), damage, 0f, Main.myPlayer,
                    ai0: DomStageDTelegraph, ai1: 12f, ai2: 640f);
            }
            PlaySound(SoundID.Item122 with { Volume = 0.7f, Pitch = -0.2f }, npc.Center);
        }

        void TickHerald(NPC npc, bool storm, KnightAttackStats stats)
        {
            npc.velocity.X *= 0.82f;
            Lighting.AddLight(npc.Center,
                (storm ? new Color(226, 40, 52) : new Color(255, 55, 20)).ToVector3()
                * (0.45f + TelegraphProgress * 0.55f));

            if (Timer == 0)
            {
                PlaySound(SoundID.Item74 with { Volume = 0.72f, Pitch = storm ? 0.25f : -0.55f }, npc.Center);
                if (storm)
                {
                    // Lock the pincer grid at cast start so slowing or knockback cannot slide an
                    // already-readable sequence under the player.
                    AuxiliaryTargetA = TryFindGround(npc.Bottom, 8, 30, out Vector2 stormGround)
                        ? stormGround
                        : npc.Bottom;
                    npc.netUpdate = true;
                }
            }
            if (!storm && Timer >= 30 && Timer <= 120 && (Timer - 30) % 45 == 0)
            {
                int wave = (Timer - 30) / 45;
                SpawnFurnaceHeraldWave(npc, stats.GreatDamage, wave);
            }
            if (storm && Timer >= StormHeraldFirstPairTick
                && Timer < StormHeraldFirstPairTick + StormHeraldLaneCountPerSide * StormHeraldPairInterval
                && (Timer - StormHeraldFirstPairTick) % StormHeraldPairInterval == 0)
            {
                int pair = (Timer - StormHeraldFirstPairTick) / StormHeraldPairInterval;
                SpawnStormHeraldLanePair(npc, AuxiliaryTargetA, stats.MagicDamage, pair);
            }
            if ((!storm && Timer == 75) || (storm && Timer == 80))
            {
                if (storm)
                {
                    PlaySound(SoundID.Item122 with { Volume = 0.72f, Pitch = -0.15f }, npc.Center);
                }
                tsorcRevampAIs.SpawnTelegraphFlash(npc,
                    storm ? new Color(226, 40, 52) : new Color(255, 75, 25));
            }
            else if ((!storm && Timer == 135) || (storm && Timer == 160))
            {
                PlaySound(SoundID.Item1 with { Volume = 0.95f, Pitch = -0.45f }, npc.Center);
            }
        }

        void TickRedCourtProcession(NPC npc, Player target, KnightAttackStats stats)
        {
            // Keep the cast mobile. The fixed rig can carry the magic overlay while walking, and
            // the procession owns its sky geometry independently once it is locked.
            ApproachHorizontalSpeed(npc, Direction, Timer < 145 ? 1.35f : 2.2f, 0.09f);

            if (Timer == 0)
            {
                LockedTarget = target.Center;
                int sweepDirection = Math.Abs(target.velocity.X) >= 1.2f
                    ? Math.Sign(target.velocity.X)
                    : Direction;
                AuxiliaryTargetB = new Vector2(sweepDirection, 0f);
                PlaySound(SoundID.Item74 with { Volume = 0.7f, Pitch = -0.32f }, npc.Center);
                npc.netUpdate = true;
            }

            if (Timer >= RedCourtFirstPortalTick
                && Timer < RedCourtFirstPortalTick + RedCourtLanceCount * RedCourtPortalInterval
                && (Timer - RedCourtFirstPortalTick) % RedCourtPortalInterval == 0)
            {
                int index = (Timer - RedCourtFirstPortalTick) / RedCourtPortalInterval;
                int sweepDirection = AuxiliaryTargetB.X < 0f ? -1 : 1;
                float laneOffset = sweepDirection * (index - (RedCourtLanceCount - 1) * 0.5f)
                    * RedCourtLaneSpacing;
                if (TryFindCourtPortalOrigin(LockedTarget, laneOffset, out Vector2 origin)
                    && Main.netMode != NetmodeID.MultiplayerClient)
                {
                    // A slight drift in the procession direction stops the pattern reading as rain
                    // while retaining a clear vertical dodge lane.
                    Vector2 lanceVelocity = new Vector2(sweepDirection * 0.45f, 10.5f);
                    Projectile.NewProjectile(npc.GetSource_FromThis(), origin, lanceVelocity,
                        ModContent.ProjectileType<RedCourtLancePortal>(), stats.GreatDamage, 0f,
                        Main.myPlayer, ai0: npc.whoAmI, ai1: index);
                }
            }

            if (Timer == RedCourtFirstPortalTick + RedCourtLanceCount * RedCourtPortalInterval)
            {
                PlaySound(SoundID.Item1 with { Volume = 0.78f, Pitch = -0.48f }, npc.Center);
            }
        }

        void SetCombatFlags(NPC npc, tsorcRevampGlobalNPC globalNPC)
        {
            if (npc.ModNPC is Enemies.RedKnight)
            {
                int commitStart = CommitStart(Attack);
                int commitEnd = RedCommitEnd(Attack);
                globalNPC.AttackTelegraphing = Timer < commitStart;
                globalNPC.AttackCommitted = Timer >= commitStart && Timer < commitEnd;
            }
            else
            {
                int commitStart = CommitStart(Attack);
                int commitEnd = GreatCommitEnd(Attack);
                globalNPC.AttackTelegraphing = Timer < commitStart;
                globalNPC.AttackCommitted = Timer >= commitStart && Timer < commitEnd;
            }
        }

        int CommitStart(KnightSpecialAttack attack)
        {
            int firstDamageTick = attack switch
            {
                KnightSpecialAttack.FirebombReversal => 45,
                KnightSpecialAttack.PoisonSpearTether => 55,
                KnightSpecialAttack.SpearThrow => 45,
                KnightSpecialAttack.PoisonArcVolley => 60,
                KnightSpecialAttack.FirebombThrow => 60,
                KnightSpecialAttack.SpectralHandBarrage => 60,
                KnightSpecialAttack.PoisonRain => 60,
                KnightSpecialAttack.PoisonCurtain => 60,
                KnightSpecialAttack.CrimsonTeleportAmbush => RedTeleportDamageTick,
                KnightSpecialAttack.CrimsonAdvance => 60,
                KnightSpecialAttack.FurnacePincer => 60,
                KnightSpecialAttack.RoyalStandard => RoyalFirstThrow,
                KnightSpecialAttack.StormbreakerEdict => 60,
                KnightSpecialAttack.RedCourtProcession => RedCourtFirstPortalTick + 30,
                KnightSpecialAttack.CrimsonDominion => 60,
                KnightSpecialAttack.FurnaceHerald => 30,
                KnightSpecialAttack.StormHerald => 40,
                KnightSpecialAttack.CinderRain => CinderRainFirstWaveTick,
                KnightSpecialAttack.StormPursuit => 90,
                _ => 30
            };
            return Math.Max(0, firstDamageTick - 30);
        }

        int RedCommitEnd(KnightSpecialAttack attack) => attack switch
        {
            KnightSpecialAttack.FirebombReversal => emberReturnDashTimer >= 0
                ? emberReturnDashTimer + 16
                : 190,
            KnightSpecialAttack.PoisonSpearTether => 166,
            KnightSpecialAttack.SpearThrow => 46,
            KnightSpecialAttack.PoisonArcVolley => 61,
            KnightSpecialAttack.FirebombThrow => 61,
            KnightSpecialAttack.SpectralHandBarrage => 90,
            KnightSpecialAttack.PoisonRain => 61,
            KnightSpecialAttack.PoisonCurtain => 61,
            KnightSpecialAttack.CrimsonTeleportAmbush => RedTeleportDamageTick + RedTeleportPincerActiveTicks,
            _ => CommitStart(attack) + 1
        };

        int GreatCommitEnd(KnightSpecialAttack attack) => attack switch
        {
            KnightSpecialAttack.FirebombReversal => emberReturnDashTimer >= 0
                ? emberReturnDashTimer + 16
                : 190,
            KnightSpecialAttack.SpearThrow => 46,
            KnightSpecialAttack.SpectralHandBarrage => 90,
            KnightSpecialAttack.CrimsonAdvance => 157,
            KnightSpecialAttack.FurnacePincer => 203,
            KnightSpecialAttack.RoyalStandard => 78,
            KnightSpecialAttack.StormbreakerEdict => 76,
            KnightSpecialAttack.RedCourtProcession => 170,
            KnightSpecialAttack.CrimsonDominion => DominionHoldTicks - 50,
            KnightSpecialAttack.FurnaceHerald => 130,
            KnightSpecialAttack.StormHerald => 190,
            KnightSpecialAttack.CinderRain => CinderRainFirstWaveTick
                + CinderRainWaveCount * CinderRainWaveInterval,
            KnightSpecialAttack.StormPursuit => 91,
            _ => CommitStart(attack) + 1
        };

        void Finish(NPC npc, tsorcRevampGlobalNPC globalNPC)
        {
            KnightSpecialAttack completed = Attack;
            if (completed == KnightSpecialAttack.FurnaceHerald)
            {
                HalfHeraldComplete = true;
            }
            else if (completed == KnightSpecialAttack.StormHerald)
            {
                ThirdHeraldComplete = true;
            }

            ClearRedTeleportState(npc, restoreVisibility: true);
            redThrowMovement = RedThrowMovement.None;
            royalSpearThrow = false;

            Attack = KnightSpecialAttack.None;
            Timer = 0;
            LockedVelocity = Vector2.Zero;
            emberBombProjectileIndex = -1;
            emberReturnDashTimer = -1;
            crimsonClearanceDelay = 0;
            globalNPC.AttackTelegraphing = false;
            globalNPC.AttackCommitted = false;
            globalNPC.SetActiveAttackDefenseTraits(npc, AttackDefenseTraits.None);
            npc.ai[1] = 60f;
            npc.ai[2] = -100f;
            npc.velocity.X *= 0.35f;
            bool greatKnight = npc.ModNPC is Bosses.SuperHardMode.GreatRedKnight;
            attackCooldown = greatKnight || IsGreatAttack(completed) ? Main.rand.Next(260, 401) : Main.rand.Next(320, 481);
            if (completed == KnightSpecialAttack.CrimsonDominion)
            {
                // PHASE 1 -> PHASE 2. The hold is over: the knight retracts its spear and goes back
                // to its narrow Dominion pool. GreatRedKnight gates the full special set and permits
                // only Crimson Advance, Firebomb Reversal and its ordinary spear throw.
                attackCooldown = 90;
            }
            else if (DominionEngaged)
            {
                // Dominion is an aggressive closing phase. Keep its narrow authored pool cycling
                // without inheriting the much longer phase-one special cooldowns.
                attackCooldown = Main.rand.Next(90, 151);
            }
            npc.netUpdate = true;
        }

        void Cancel(NPC npc, tsorcRevampGlobalNPC globalNPC)
        {
            ClearRedTeleportState(npc, restoreVisibility: true);
            redThrowMovement = RedThrowMovement.None;
            royalSpearThrow = false;
            Attack = KnightSpecialAttack.None;
            Timer = 0;
            LockedVelocity = Vector2.Zero;
            emberBombProjectileIndex = -1;
            emberReturnDashTimer = -1;
            crimsonClearanceDelay = 0;
            globalNPC.AttackTelegraphing = false;
            globalNPC.AttackCommitted = false;
            globalNPC.SetActiveAttackDefenseTraits(npc, AttackDefenseTraits.None);
            attackCooldown = 120;
            npc.ai[1] = 60f;
            npc.ai[2] = -100f;
            npc.netUpdate = true;
        }

        void ClearRedTeleportState(NPC npc, bool restoreVisibility)
        {
            if (restoreVisibility && redTeleportHidden)
            {
                npc.alpha = 0;
                npc.dontTakeDamage = false;
                npc.netUpdate = true;
            }
            redTeleportPattern = RedTeleportPattern.None;
            redTeleportFakeCount = 0;
            redTeleportHidden = false;
            redTeleportDestination = Vector2.Zero;
            redTeleportFakeA = Vector2.Zero;
            redTeleportFakeB = Vector2.Zero;
        }

        static bool IsGreatAttack(KnightSpecialAttack attack)
        {
            return attack >= KnightSpecialAttack.CrimsonAdvance;
        }

        int RedTeleportActualMarkerTick => RedTeleportEntryTellTicks
            + redTeleportFakeCount * RedTeleportFeintIntervalTicks;

        int RedTeleportArrivalTellTicks => redTeleportPattern == RedTeleportPattern.RetreatThrow
            ? RedTeleportRetreatArrivalTellTicks
            : RedTeleportCloseArrivalTellTicks;

        int RedTeleportSnapTick => RedTeleportActualMarkerTick + RedTeleportArrivalTellTicks;

        int RedTeleportDamageTick => redTeleportPattern == RedTeleportPattern.RetreatThrow
            ? RedTeleportSnapTick + 18
            : RedTeleportSnapTick;

        int RedTeleportFinishTick => RedTeleportDamageTick + 34;

        int Duration(KnightSpecialAttack attack)
        {
            return attack switch
            {
                KnightSpecialAttack.FirebombReversal => 220,
                KnightSpecialAttack.PoisonSpearTether => 190,
                KnightSpecialAttack.SpearThrow => 75,
                KnightSpecialAttack.PoisonArcVolley => 95,
                KnightSpecialAttack.FirebombThrow => 100,
                KnightSpecialAttack.SpectralHandBarrage => 120,
                KnightSpecialAttack.PoisonRain => 90,
                KnightSpecialAttack.PoisonCurtain => 90,
                KnightSpecialAttack.CrimsonTeleportAmbush => RedTeleportFinishTick,
                KnightSpecialAttack.CrimsonStandard => 210,
                KnightSpecialAttack.CrimsonAdvance => 195,
                KnightSpecialAttack.FurnacePincer => 230,
                KnightSpecialAttack.RoyalStandard => 200,
                KnightSpecialAttack.StormbreakerEdict => 200,
                KnightSpecialAttack.RedCourtProcession => 220,
                // Dominion's BLOCKING part is now only the plant-and-hold. Everything that used to
                // fill the other 420 ticks (containment ring, arena-edge barrage, escape nova) is
                // either gone or moved: the lightning is an ongoing loop and the nova is the death
                // animation. See the phase notes at the top of this class.
                KnightSpecialAttack.CrimsonDominion => DominionHoldTicks,
                KnightSpecialAttack.FurnaceHerald => 150,
                // 20t opening charge; pair telegraphs begin every 20t through tick 160, the last
                // pair detonates at 180, then the boss has 30t of readable recovery.
                KnightSpecialAttack.StormHerald => 210,
                KnightSpecialAttack.CinderRain => 200,
                KnightSpecialAttack.StormPursuit => 125,
                _ => 1
            };
        }

        public KnightHeldProp HeldProp => Attack switch
        {
            KnightSpecialAttack.FirebombReversal when Timer < 65 => KnightHeldProp.Spear,
            KnightSpecialAttack.FirebombReversal when Timer <= 120 => KnightHeldProp.Bomb,
            KnightSpecialAttack.FirebombReversal => KnightHeldProp.Spear,
            KnightSpecialAttack.PoisonSpearTether when Timer <= 55 => KnightHeldProp.Spear,
            KnightSpecialAttack.PoisonSpearTether when Timer < 148 => KnightHeldProp.Magic,
            KnightSpecialAttack.PoisonSpearTether => KnightHeldProp.Spear,
            KnightSpecialAttack.SpearThrow when Timer <= 45 => KnightHeldProp.Spear,
            KnightSpecialAttack.PoisonArcVolley when Timer <= 60 => KnightHeldProp.Magic,
            KnightSpecialAttack.FirebombThrow when Timer <= 60 => KnightHeldProp.Bomb,
            KnightSpecialAttack.SpectralHandBarrage when Timer < 90 => KnightHeldProp.Spectral,
            KnightSpecialAttack.PoisonRain when Timer <= 60 => KnightHeldProp.Magic,
            KnightSpecialAttack.PoisonCurtain when Timer <= 60 => KnightHeldProp.Magic,
            KnightSpecialAttack.CrimsonTeleportAmbush when Timer >= RedTeleportActualMarkerTick
                && Timer <= (redTeleportPattern == RedTeleportPattern.RetreatThrow
                    ? RedTeleportDamageTick
                    : RedTeleportDamageTick + RedTeleportPincerActiveTicks) => KnightHeldProp.Spear,
            // Tick() processes the attack before incrementing Timer, so state 75 is still the
            // final pre-release draw frame. Keep the spear visible until that fire tick executes.
            KnightSpecialAttack.CrimsonStandard when Timer <= 75 => KnightHeldProp.Spear,
            KnightSpecialAttack.CrimsonAdvance => KnightHeldProp.Spear,
            KnightSpecialAttack.FurnacePincer when Timer < 60 => KnightHeldProp.Bomb,
            KnightSpecialAttack.FurnacePincer when Timer >= 105 && Timer < 205 => KnightHeldProp.Spear,
            KnightSpecialAttack.RoyalStandard when Timer <= RoyalFirstThrow => KnightHeldProp.Spear,
            KnightSpecialAttack.StormbreakerEdict when Timer < 122 => KnightHeldProp.Spear,
            KnightSpecialAttack.RedCourtProcession when Timer < 170 => KnightHeldProp.Magic,
            KnightSpecialAttack.CinderRain when Timer <= CinderRainFirstWaveTick => KnightHeldProp.Magic,
            KnightSpecialAttack.StormPursuit when Timer <= 90 => KnightHeldProp.Spear,
            KnightSpecialAttack.CrimsonDominion => KnightHeldProp.Spear,
            _ => KnightHeldProp.None
        };

        public float GetSpearRotation(Vector2 handWorld, Vector2 launchSource)
        {
            Vector2 direction = new Vector2(Direction, 0f);
            if ((Attack == KnightSpecialAttack.CrimsonStandard || Attack == KnightSpecialAttack.RoyalStandard)
                    && Timer >= 45)
            {
                direction = RedKnightStandard.InitialFlightDirection(
                    launchSource, LockedTarget, Attack == KnightSpecialAttack.RoyalStandard);
            }
            else if (Attack == KnightSpecialAttack.CrimsonDominion)
            {
                direction = Vector2.UnitY;
            }
            else if (Attack == KnightSpecialAttack.PoisonSpearTether && AuxiliaryTargetA != Vector2.Zero && Timer >= 20)
            {
                direction = LockedVelocity.LengthSquared() > 0.1f
                    ? LockedVelocity
                    : AuxiliaryTargetA - handWorld;
            }
            else if (Attack == KnightSpecialAttack.SpearThrow && Timer >= 15)
            {
                direction = LockedVelocity.LengthSquared() > 0.1f
                    ? LockedVelocity
                    : LockedTarget - handWorld;
            }
            else if (Attack == KnightSpecialAttack.StormPursuit && Timer >= 30)
            {
                direction = LockedVelocity.LengthSquared() > 0.1f
                    ? LockedVelocity
                    : LockedTarget - handWorld;
            }
            else if (Attack == KnightSpecialAttack.CrimsonTeleportAmbush
                && Timer >= RedTeleportActualMarkerTick)
            {
                direction = LockedVelocity.LengthSquared() > 0.1f
                    ? LockedVelocity
                    : LockedTarget - handWorld;
            }
            return direction.SafeNormalize(new Vector2(Direction, 0f)).ToRotation() + MathHelper.PiOver2;
        }

        public float SpearGripSlide
        {
            get
            {
                if (Attack == KnightSpecialAttack.CrimsonAdvance)
                {
                    return PulseWindow(Timer, 60, 80, 20f) + PulseWindow(Timer, 135, 157, 18f);
                }
                if (Attack == KnightSpecialAttack.FurnacePincer)
                {
                    return PulseWindow(Timer, 165, 203, 24f);
                }
                if (Attack == KnightSpecialAttack.StormbreakerEdict)
                {
                    return PulseWindow(Timer, 60, 76, 24f);
                }
                if (Attack == KnightSpecialAttack.FirebombReversal)
                {
                    float openingThrust = PulseWindow(Timer, 45, 57, 14f);
                    float returnThrust = emberReturnDashTimer >= 0
                        ? PulseWindow(Timer, emberReturnDashTimer, emberReturnDashTimer + 16, 18f)
                        : 0f;
                    return openingThrust + returnThrust;
                }
                if (Attack == KnightSpecialAttack.CrimsonDominion)
                {
                    // Plant over the first 60t, hold, then RETRACT over the last 50t of the 300t
                    // hold — the retract is the visual handoff into phase 2's melee.
                    if (Timer < 60)
                    {
                        return MathHelper.Lerp(0f, 20f, MathHelper.Clamp(Timer / 60f, 0f, 1f));
                    }
                    if (Timer >= DominionHoldTicks - 50)
                    {
                        return MathHelper.Lerp(20f, 0f,
                            MathHelper.Clamp((Timer - (DominionHoldTicks - 50)) / 50f, 0f, 1f));
                    }
                    return 20f;
                }
                return 0f;
            }
        }

        public bool SpearDamageWake => Attack switch
        {
            KnightSpecialAttack.FirebombReversal => (Timer >= 45 && Timer < 57)
                || (emberReturnDashTimer >= 0 && Timer >= emberReturnDashTimer && Timer < emberReturnDashTimer + 16),
            KnightSpecialAttack.CrimsonAdvance => (Timer >= 60 && Timer < 80) || (Timer >= 135 && Timer < 157),
            KnightSpecialAttack.FurnacePincer => Timer >= 165 && Timer < 203,
            KnightSpecialAttack.StormbreakerEdict => Timer >= 60 && Timer < 76,
            KnightSpecialAttack.CrimsonTeleportAmbush when redTeleportPattern != RedTeleportPattern.RetreatThrow
                => Timer >= RedTeleportSnapTick
                    && Timer < RedTeleportSnapTick + RedTeleportPincerActiveTicks,
            _ => false
        };

        public bool IsSpectralHandBarrage => Attack == KnightSpecialAttack.SpectralHandBarrage;
        public float SpectralGatherProgress => IsSpectralHandBarrage
            ? MathHelper.Clamp(Timer / 60f, 0f, 1f)
            : 0f;

        public float TelegraphProgress
        {
            get
            {
                int telegraph = Attack switch
                {
                    KnightSpecialAttack.FirebombReversal => 45,
                    KnightSpecialAttack.PoisonSpearTether => Timer < 55 ? 55 : 148,
                    KnightSpecialAttack.SpearThrow => 45,
                    KnightSpecialAttack.PoisonArcVolley => 60,
                    KnightSpecialAttack.FirebombThrow => 60,
                    KnightSpecialAttack.SpectralHandBarrage => 60,
                    KnightSpecialAttack.PoisonRain => 60,
                    KnightSpecialAttack.PoisonCurtain => 60,
                    KnightSpecialAttack.CrimsonTeleportAmbush => RedTeleportDamageTick,
                    KnightSpecialAttack.CrimsonStandard => 75,
                    KnightSpecialAttack.CrimsonAdvance => Timer < 60 ? 60 : 135,
                    KnightSpecialAttack.FurnacePincer => Timer < 60 ? 60 : 165,
                    KnightSpecialAttack.RoyalStandard => 75,
                    KnightSpecialAttack.StormbreakerEdict => 60,
                    KnightSpecialAttack.RedCourtProcession => RedCourtFirstPortalTick,
                    KnightSpecialAttack.CrimsonDominion => 90,
                    KnightSpecialAttack.FurnaceHerald => 150,
                    KnightSpecialAttack.StormHerald => 210,
                    KnightSpecialAttack.CinderRain => CinderRainFirstWaveTick,
                    KnightSpecialAttack.StormPursuit => 90,
                    _ => 1
                };
                int start = Attack switch
                {
                    KnightSpecialAttack.PoisonSpearTether when Timer >= 55 => 55,
                    KnightSpecialAttack.CrimsonAdvance when Timer >= 60 => 105,
                    KnightSpecialAttack.FurnacePincer when Timer >= 60 => 105,
                    _ => 0
                };
                return MathHelper.Clamp((Timer - start) / (float)Math.Max(1, telegraph - start), 0f, 1f);
            }
        }

        static float PulseWindow(int timer, int start, int end, float maximum)
        {
            if (timer < start || timer >= end)
            {
                return 0f;
            }
            float progress = (timer - start) / (float)Math.Max(1, end - start - 1);
            return (float)Math.Sin(progress * MathHelper.Pi) * maximum;
        }

        static Vector2 GuidedLeapVelocity(Vector2 source, Vector2 target, int flightTicks)
        {
            const float gravityPerTick = 0.35f;
            float ticks = Math.Max(1, flightTicks);
            Vector2 delta = target - source;
            float gravityDrop = gravityPerTick * (ticks - 1f) * ticks * 0.5f;
            return new Vector2(delta.X / ticks,
                MathHelper.Clamp((delta.Y - gravityDrop) / ticks, -14f, 8f));
        }

        static Vector2 LeonhardDashVelocity(Vector2 source, Vector2 target)
        {
            int direction = target.X >= source.X ? 1 : -1;
            // Leonhard Phase 2 uses 16 px/t beyond 26 tiles and 10 px/t for the shorter hop-dash.
            float speed = Math.Abs(target.X - source.X) >= 26f * 16f ? 16f : 10f;
            return new Vector2(direction * speed, -3f);
        }

        /// <summary>
        /// Crimson Advance owns movement while active, so the shared pathfinder cannot supply its
        /// usual step jump. Measure a short obstacle directly in the committed direction and use
        /// v = sqrt(2gh) to request only enough lift to clear it under this controller's 0.35 gravity.
        /// </summary>
        static void TryGainCrimsonAdvanceClearance(NPC npc, int direction)
        {
            // Tick() has already applied this attack controller's 0.35 gravity, so a grounded NPC
            // arrives here with +0.35 Y velocity and collideY still carrying the ground contact.
            if (!npc.collideY || npc.velocity.Y < -0.01f)
            {
                return;
            }

            int frontTileX = (int)((direction > 0 ? npc.Right.X + 6f : npc.Left.X - 6f) / 16f);
            int feetTileY = (int)((npc.Bottom.Y - 2f) / 16f);
            int obstacleTiles = 0;
            for (int y = feetTileY; y >= feetTileY - 2; y--)
            {
                if (!WorldGen.InWorld(frontTileX, y) || !WorldGen.SolidTile(frontTileX, y))
                {
                    break;
                }
                obstacleTiles++;
            }

            if (obstacleTiles == 0)
            {
                return;
            }

            const float gravityPerTick = 0.35f;
            float clearanceHeight = obstacleTiles * 16f + 8f;
            float requiredSpeed = MathF.Sqrt(2f * gravityPerTick * clearanceHeight) + gravityPerTick;
            float maxJumpPower = npc.GetGlobalNPC<tsorcRevampGlobalNPC>().MaxJumpPower;
            if (requiredSpeed <= maxJumpPower)
            {
                npc.velocity.Y = -requiredSpeed;
                npc.netUpdate = true;
            }
        }

        static bool HasLungeClearance(NPC npc, int direction, float distance)
        {
            for (float offset = 8f; offset <= distance; offset += 8f)
            {
                // Lift the probe two pixels so ordinary floor contact is not mistaken for a wall.
                Vector2 probe = npc.position + new Vector2(direction * offset, -2f);
                if (Collision.SolidCollision(probe, npc.width, npc.height))
                {
                    return false;
                }
            }
            return true;
        }

        bool TryPlanRedTeleport(NPC npc, Player target, out RedTeleportPattern pattern,
            out Vector2 destination, out Vector2 fakeA, out Vector2 fakeB, out int fakeCount)
        {
            pattern = RedTeleportPattern.None;
            destination = Vector2.Zero;
            fakeA = Vector2.Zero;
            fakeB = Vector2.Zero;
            fakeCount = 0;

            float distance = npc.Distance(target.Center);
            int currentSide = npc.Center.X < target.Center.X ? -1 : 1;
            if (distance >= 420f)
            {
                pattern = RedTeleportPattern.GapClose;
                return TryFindAuthoredTeleportLanding(npc, target,
                    target.Center.X + currentSide * 84f, out destination);
            }

            float roll = Main.rand.NextFloat();
            if (roll < 0.14f)
            {
                pattern = RedTeleportPattern.FeintPincer;
                int arrivalSide = -currentSide;
                if (!TryFindAuthoredTeleportLanding(npc, target,
                    target.Center.X + arrivalSide * 78f, out destination))
                {
                    pattern = RedTeleportPattern.None;
                    return false;
                }

                // Fake markers alternate sides and are themselves terrain-valid, so the visual
                // never promises an impossible emergence inside a wall. The true marker still
                // receives the full close-arrival tell after the second 45-tick blast cadence.
                fakeCount = 2;
                int firstFakeSide = currentSide;
                if (!TryFindAuthoredTeleportLanding(npc, target,
                    target.Center.X + firstFakeSide * RedTeleportFeintOffset, out fakeA)
                    || Vector2.Distance(fakeA, target.Center) > RedKnightTeleportFeintBlast.Radius)
                {
                    fakeCount = 0;
                    pattern = RedTeleportPattern.Pincer;
                    return true;
                }
                if (!TryFindAuthoredTeleportLanding(npc, target,
                    target.Center.X - firstFakeSide * RedTeleportFeintOffset, out fakeB)
                    || Vector2.Distance(fakeB, target.Center) > RedKnightTeleportFeintBlast.Radius
                    || Vector2.Distance(fakeA, fakeB) < RedTeleportFeintMinimumSeparation)
                {
                    fakeCount = 0;
                    pattern = RedTeleportPattern.Pincer;
                }
                return true;
            }

            if (roll < 0.46f)
            {
                pattern = RedTeleportPattern.RetreatThrow;
                if (TryFindAuthoredTeleportLanding(npc, target,
                    target.Center.X + currentSide * 270f, out destination))
                {
                    return true;
                }
            }

            pattern = RedTeleportPattern.Pincer;
            if (TryFindAuthoredTeleportLanding(npc, target,
                target.Center.X - currentSide * 78f, out destination))
            {
                return true;
            }

            // If the arena cannot support a safe crossover, retain the authored card but turn it
            // into a same-side gap close. Never materialize inside tiles or overlap the player.
            pattern = RedTeleportPattern.GapClose;
            return TryFindAuthoredTeleportLanding(npc, target,
                target.Center.X + currentSide * 84f, out destination);
        }

        static bool TryFindAuthoredTeleportLanding(NPC npc, Player target, float desiredX,
            out Vector2 destination)
        {
            int desiredSide = desiredX < target.Center.X ? -1 : 1;
            for (int radius = 0; radius <= 4; radius++)
            {
                for (int signIndex = 0; signIndex < (radius == 0 ? 1 : 2); signIndex++)
                {
                    int sign = signIndex == 0 ? 1 : -1;
                    float x = desiredX + sign * radius * 16f;
                    if ((x - target.Center.X) * desiredSide < 52f)
                    {
                        continue;
                    }
                    if (!TryFindGround(new Vector2(x, target.Bottom.Y), 10, 26,
                        out Vector2 surface))
                    {
                        continue;
                    }

                    Vector2 center = new Vector2(surface.X, surface.Y - npc.height * 0.5f);
                    Vector2 topLeft = center - new Vector2(npc.width, npc.height) * 0.5f;
                    if (Collision.SolidCollision(topLeft, npc.width, npc.height)
                        || Math.Abs(center.Y - target.Center.Y) > 128f
                        || !Collision.CanHitLine(center, 2, 2, target.Center, 2, 2))
                    {
                        continue;
                    }
                    destination = center;
                    return true;
                }
            }
            destination = Vector2.Zero;
            return false;
        }

        static void SpawnRedTeleportMarker(NPC npc, Vector2 position, int lifetime, bool burst)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            float radius = Math.Max(npc.width, npc.height) * 0.75f;
            Projectile mist = Projectile.NewProjectileDirect(npc.GetSource_FromThis(), position,
                Vector2.Zero, ModContent.ProjectileType<TeleportMistLinger>(),
                0, 0f, Main.myPlayer, 1f, radius);
            mist.timeLeft = Math.Max(4, lifetime);
            mist.netUpdate = true;
            if (burst)
            {
                Projectile.NewProjectile(npc.GetSource_FromThis(), position, Vector2.Zero,
                    ModContent.ProjectileType<FireTeleportBlast>(),
                    0, 0f, Main.myPlayer);
            }
        }

        void SpawnFurnaceHeraldWave(NPC npc, int damage, int wave)
        {
            const int projectileCount = 16;
            const float activeTravelTicks = 42f;
            float travelDistance = 150f * (wave + 1);
            float speed = travelDistance / activeTravelTicks;
            float angleOffset = ArenaBaseRotation + wave * (MathHelper.Pi / projectileCount);

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                for (int i = 0; i < projectileCount; i++)
                {
                    float angle = angleOffset + i * MathHelper.TwoPi / projectileCount;
                    Vector2 velocity = angle.ToRotationVector2() * speed;
                    int spinDirection = (i + wave) % 2 == 0 ? 1 : -1;
                    Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, velocity,
                        ModContent.ProjectileType<DestinedDeathBlaze>(), damage, 1.5f, Main.myPlayer,
                        ai0: spinDirection, ai1: 2f, ai2: travelDistance);
                }
            }

            if (!Main.dedServ)
            {
                for (int i = 0; i < 24; i++)
                {
                    float angle = angleOffset + i * MathHelper.TwoPi / 24f;
                    Vector2 outward = angle.ToRotationVector2();
                    Dust dust = Dust.NewDustPerfect(npc.Center + outward * 24f,
                        i % 2 == 0 ? DustID.Blood : DustID.Wraith,
                        outward * Main.rand.NextFloat(2.2f, 4.4f), 100,
                        new Color(198, 14, 30), Main.rand.NextFloat(0.9f, 1.35f));
                    dust.noGravity = true;
                }
            }

            PlaySound(SoundID.Item74 with
            {
                Volume = 0.72f,
                Pitch = -0.35f + wave * 0.12f
            }, npc.Center);
        }

        static void SpawnStormHeraldLanePair(NPC npc, Vector2 gridCenter, int damage, int pair)
        {
            int stepsFromCenter = StormHeraldLaneCountPerSide - 1 - pair;
            float offset = StormHeraldInnermostOffset + stepsFromCenter * StormHeraldLaneSpacing;
            Vector2 leftProbe = gridCenter - new Vector2(offset, 0f);
            Vector2 rightProbe = gridCenter + new Vector2(offset, 0f);

            // The pair is one combat beat. If terrain cannot support both halves, omit both rather
            // than presenting an asymmetric pincer or anchoring a damaging line in mid-air.
            if (!TryFindGround(leftProbe, 12, 30, out Vector2 leftGround)
                || !TryFindGround(rightProbe, 12, 30, out Vector2 rightGround))
            {
                return;
            }

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                Projectile.NewProjectile(npc.GetSource_FromThis(), leftGround, new Vector2(0f, -1f),
                    ModContent.ProjectileType<RedKnightLightningLane>(), damage, 0f, Main.myPlayer,
                    ai0: StormHeraldLaneTelegraphTicks, ai1: StormHeraldLaneActiveTicks, ai2: 620f);
                // Negative active duration means "silent twin" to RedKnightLightningLane. Collision
                // still uses its absolute value, but the simultaneous pair produces one impact cue.
                Projectile.NewProjectile(npc.GetSource_FromThis(), rightGround, new Vector2(0f, -1f),
                    ModContent.ProjectileType<RedKnightLightningLane>(), damage, 0f, Main.myPlayer,
                    ai0: StormHeraldLaneTelegraphTicks, ai1: -StormHeraldLaneActiveTicks, ai2: 620f);
            }
        }

        static RedThrowMovement ChooseRedThrowMovement(NPC npc, Player target, int facing)
        {
            float signedDistance = (target.Center.X - npc.Center.X) * facing;
            bool canRetreat = HasSafeRedThrowHop(npc, target.Center, facing, -facing,
                RedThrowRetreatSpeed, advancing: false);
            bool canAdvance = HasSafeRedThrowHop(npc, target.Center, facing, facing,
                RedThrowAdvanceSpeed, advancing: true);
            bool canRise = HasSafeRedThrowHop(npc, target.Center, facing, facing, 0.8f,
                advancing: false);

            // Distance supplies the tactical bias, while the random branch keeps the same range
            // from always announcing the same answer. Every airborne option has already passed a
            // full predicted-body path and landing test.
            if (signedDistance < 240f && canRetreat && Main.rand.NextFloat() < 0.72f)
            {
                return RedThrowMovement.RetreatHop;
            }
            if (signedDistance > 360f && canAdvance && Main.rand.NextFloat() < 0.72f)
            {
                return RedThrowMovement.AdvanceHop;
            }

            int airborneChoice = Main.rand.Next(4);
            if (airborneChoice == 0 && canRise)
            {
                return RedThrowMovement.VerticalHop;
            }
            if (airborneChoice == 1 && canRetreat)
            {
                return RedThrowMovement.RetreatHop;
            }
            if (airborneChoice == 2 && canAdvance)
            {
                return RedThrowMovement.AdvanceHop;
            }
            return RedThrowMovement.GroundAdvance;
        }

        static RedThrowMovement ChooseGreatThrowMovement(NPC npc, Player target, int facing)
        {
            float distance = Math.Abs(target.Center.X - npc.Center.X);
            bool canRetreat = HasSafeRedThrowHop(npc, target.Center, facing, -facing,
                RedThrowRetreatSpeed, advancing: false);
            bool canAdvance = HasSafeRedThrowHop(npc, target.Center, facing, facing,
                RedThrowAdvanceSpeed, advancing: true);
            bool canRise = HasSafeRedThrowHop(npc, target.Center, facing, facing, 0.8f,
                advancing: false);

            // GRK's variation is distance-authored rather than randomly selected: close pressure
            // creates space, middle range rises in place, and long range closes distance.
            if (distance < 240f && canRetreat) return RedThrowMovement.RetreatHop;
            if (distance > 440f && canAdvance) return RedThrowMovement.AdvanceHop;
            if (canRise) return RedThrowMovement.VerticalHop;
            if (distance < 240f && canAdvance) return RedThrowMovement.AdvanceHop;
            if (distance > 440f && canRetreat) return RedThrowMovement.RetreatHop;
            return RedThrowMovement.GroundAdvance;
        }

        void RunRedThrowMovement(NPC npc, Vector2 target, int hopTick, int releaseTick,
            float approachSpeed, float recoverySpeed, float acceleration)
        {
            if (redThrowMovement == RedThrowMovement.None
                || redThrowMovement == RedThrowMovement.GroundAdvance)
            {
                ApproachHorizontalSpeed(npc, Direction,
                    Timer < releaseTick ? approachSpeed : recoverySpeed, acceleration);
                return;
            }

            int travelDirection = redThrowMovement == RedThrowMovement.RetreatHop ? -Direction : Direction;
            float travelSpeed = redThrowMovement switch
            {
                RedThrowMovement.RetreatHop => RedThrowRetreatSpeed,
                RedThrowMovement.AdvanceHop => RedThrowAdvanceSpeed,
                _ => 0.8f
            };

            if (Timer < hopTick)
            {
                ApproachHorizontalSpeed(npc, Direction, approachSpeed, acceleration);
                return;
            }
            if (Timer == hopTick)
            {
                if (npc.velocity.Y != 0f || !HasSafeRedThrowHop(npc, target, Direction,
                    travelDirection, travelSpeed, redThrowMovement == RedThrowMovement.AdvanceHop))
                {
                    redThrowMovement = RedThrowMovement.GroundAdvance;
                    ApproachHorizontalSpeed(npc, Direction, approachSpeed, acceleration);
                    npc.netUpdate = true;
                    return;
                }

                npc.velocity = new Vector2(travelDirection * travelSpeed, -RedThrowHopSpeedY);
                npc.netUpdate = true;
                return;
            }

            if (Timer <= releaseTick)
            {
                // Air control is intentionally absent: takeoff fixes the facing and horizontal
                // commitment. If the terrain makes it land a beat early, it keeps coasting in the
                // original direction instead of snapping around before release.
                if (npc.velocity.Y == 0f)
                {
                    ApproachHorizontalSpeed(npc, travelDirection, Math.Max(0.8f, travelSpeed * 0.5f),
                        acceleration * 0.5f);
                }
                return;
            }

            ApproachHorizontalSpeed(npc, Direction, recoverySpeed, acceleration);
        }

        // See TryFindGround above — implementation moved to KnightHopPlanner. The red tuning constants are
        // passed explicitly so this stays byte-for-byte the same arc it always was.
        static bool HasSafeRedThrowHop(NPC npc, Vector2 target, int facing, int travelDirection,
            float horizontalSpeed, bool advancing)
            => KnightHopPlanner.HasSafeHop(npc, target, facing, travelDirection, horizontalSpeed, advancing,
                RedThrowHopSpeedY, RedThrowGravity, RedThrowMinimumForwardClearance);

        static bool IsMobileMeleeAttack(KnightSpecialAttack attack)
        {
            return attack == KnightSpecialAttack.FirebombReversal
                || attack == KnightSpecialAttack.CrimsonAdvance
                || attack == KnightSpecialAttack.FurnacePincer
                || attack == KnightSpecialAttack.StormbreakerEdict;
        }

        // See TryFindGround above — implementation moved to KnightHopPlanner, forwarder kept so no call site
        // here changes.
        static void ApproachHorizontalSpeed(NPC npc, int direction, float speed, float acceleration)
            => KnightHopPlanner.ApproachHorizontalSpeed(npc, direction, speed, acceleration);

        static void SpawnLunge(NPC npc, int damage, float reach, float height, int duration, float knockback)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }
            Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, new Vector2(npc.direction, 0f),
                ModContent.ProjectileType<RedKnightLungeHitbox>(), damage, knockback, Main.myPlayer,
                reach, height, duration);
        }

        static Vector2 SpearLaunchSource(NPC npc, int direction)
        {
            if (npc.ModNPC is Bosses.SuperHardMode.GreatRedKnight greatKnight)
            {
                return greatKnight.GetAttackSpearLaunchSource(direction);
            }
            return npc.Center;
        }

        static void SpawnStandard(NPC npc, Vector2 target, int damage, KnightStandardMode mode)
        {
            Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, Vector2.Zero,
                ModContent.ProjectileType<RedKnightStandard>(), damage, 0f, Main.myPlayer,
                target.X, target.Y, (float)mode);
        }

        static void PlaySound(SoundStyle sound, Vector2 position)
        {
            if (!Main.dedServ)
            {
                SoundEngine.PlaySound(sound, position);
            }
        }

        static bool HasUsableCinderRainLane(Vector2 targetCenter)
        {
            int usable = 0;
            float[] probes = { -150f, 0f, 150f };
            for (int i = 0; i < probes.Length; i++)
            {
                if (TryFindCinderRainOrigin(targetCenter, probes[i], out _))
                {
                    usable++;
                }
            }
            return usable >= 2;
        }

        static bool TryFindCinderRainOrigin(Vector2 targetCenter, float horizontalOffset,
            out Vector2 origin)
        {
            float expiryY = targetCenter.Y - CinderRainExpireHeight;
            Vector2 preferred = targetCenter + new Vector2(horizontalOffset, -CinderRainSpawnHeight);
            for (int step = 0; step <= 22; step++)
            {
                Vector2 candidate = preferred + new Vector2(0f, step * 16f);
                // Twelve clear tiles above the player are the minimum for this airborne-only rain.
                if (candidate.Y > targetCenter.Y - 12f * 16f)
                {
                    break;
                }
                if (!Collision.SolidCollision(candidate - new Vector2(8f), 16, 16)
                    && Collision.CanHitLine(candidate, 2, 2,
                        new Vector2(candidate.X, expiryY), 2, 2))
                {
                    origin = candidate;
                    return true;
                }
            }
            origin = Vector2.Zero;
            return false;
        }

        static bool TryFindCourtPortalOrigin(Vector2 lockedCenter, float horizontalOffset, out Vector2 origin)
        {
            Vector2 laneTarget = lockedCenter + new Vector2(horizontalOffset, 0f);
            // Start high and walk downward until the entire lane to the locked player position is
            // open. This puts the portal below low ceilings instead of materializing a lance in tile.
            for (int step = 0; step <= 11; step++)
            {
                // About 300px above the locked player is still visibly "sky" at gameplay zoom,
                // but unlike the old rain height it keeps the portal itself inside the camera.
                Vector2 candidate = new Vector2(laneTarget.X, lockedCenter.Y - 300f + step * 16f);
                if (candidate.Y > lockedCenter.Y - 120f)
                {
                    break;
                }
                if (!Collision.SolidCollision(candidate - new Vector2(10f), 20, 20)
                    && Collision.CanHitLine(candidate, 2, 2, laneTarget, 2, 2))
                {
                    origin = candidate;
                    return true;
                }
            }
            origin = Vector2.Zero;
            return false;
        }

        // Implementation now lives in KnightHopPlanner so the Black Knight family can share it. Kept as a
        // forwarder rather than updating the ~15 call sites here (and SurfaceFlameHelper's), so this
        // extraction cannot change Red Knight's behaviour.
        internal static bool TryFindGround(Vector2 around, int searchUpTiles, int searchDownTiles, out Vector2 surface)
            => KnightHopPlanner.TryFindGround(around, searchUpTiles, searchDownTiles, out surface);

        public void Send(BinaryWriter writer)
        {
            writer.Write((byte)Attack);
            writer.Write(Timer);
            writer.Write((sbyte)Direction);
            WriteVector(writer, LockedTarget);
            WriteVector(writer, AuxiliaryTargetA);
            WriteVector(writer, AuxiliaryTargetB);
            WriteVector(writer, LockedVelocity);
            writer.Write(ArenaBaseRotation);
            writer.Write((sbyte)ArenaRotationDirection);
            writer.Write(HalfHeraldComplete);
            writer.Write(ThirdHeraldComplete);
            writer.Write(attackCooldown);
            // Dominion is a permanent phase, so a resyncing or late-joining client MUST learn both
            // the latch and where the lightning loop is, or its visuals desync from the server.
            writer.Write(DominionEngaged);
            writer.Write(DominionSequenceTimer);
            writer.Write(emberReturnDashTimer);
            writer.Write((byte)redTeleportPattern);
            writer.Write((byte)redThrowMovement);
            writer.Write(royalSpearThrow);
            writer.Write((byte)redTeleportFakeCount);
            writer.Write(redTeleportHidden);
            WriteVector(writer, redTeleportDestination);
            WriteVector(writer, redTeleportFakeA);
            WriteVector(writer, redTeleportFakeB);
        }

        public void Receive(BinaryReader reader)
        {
            Attack = (KnightSpecialAttack)reader.ReadByte();
            Timer = reader.ReadInt32();
            Direction = reader.ReadSByte();
            LockedTarget = ReadVector(reader);
            AuxiliaryTargetA = ReadVector(reader);
            AuxiliaryTargetB = ReadVector(reader);
            LockedVelocity = ReadVector(reader);
            ArenaBaseRotation = reader.ReadSingle();
            ArenaRotationDirection = reader.ReadSByte();
            HalfHeraldComplete = reader.ReadBoolean();
            ThirdHeraldComplete = reader.ReadBoolean();
            attackCooldown = reader.ReadInt32();
            DominionEngaged = reader.ReadBoolean();
            DominionSequenceTimer = reader.ReadInt32();
            emberReturnDashTimer = reader.ReadInt32();
            redTeleportPattern = (RedTeleportPattern)reader.ReadByte();
            redThrowMovement = (RedThrowMovement)reader.ReadByte();
            royalSpearThrow = reader.ReadBoolean();
            redTeleportFakeCount = reader.ReadByte();
            redTeleportHidden = reader.ReadBoolean();
            redTeleportDestination = ReadVector(reader);
            redTeleportFakeA = ReadVector(reader);
            redTeleportFakeB = ReadVector(reader);
        }

        static void WriteVector(BinaryWriter writer, Vector2 value)
        {
            writer.Write(value.X);
            writer.Write(value.Y);
        }

        static Vector2 ReadVector(BinaryReader reader)
        {
            return new Vector2(reader.ReadSingle(), reader.ReadSingle());
        }
    }
}

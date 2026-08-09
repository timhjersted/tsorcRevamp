using Microsoft.Xna.Framework;
using System;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Projectiles.Enemy;
using tsorcRevamp.Projectiles.Enemy.Weapons;

namespace tsorcRevamp.NPCs
{
    internal enum KnightSpecialAttack : byte
    {
        None,
        EmberReversal,
        PoisonWake,
        CrimsonStandard,
        CrimsonAdvance,
        FurnacePincer,
        RoyalStandard,
        StormbreakerEdict,
        CrimsonDominion,
        FurnaceHerald,
        StormHerald
    }

    internal enum KnightHeldProp : byte
    {
        None,
        Spear,
        Bomb,
        Magic
    }

    internal readonly struct KnightAttackStats
    {
        public readonly int SpearDamage;
        public readonly int MagicDamage;
        public readonly int GreatDamage;

        public KnightAttackStats(int spearDamage, int magicDamage, int greatDamage)
        {
            SpearDamage = spearDamage;
            MagicDamage = magicDamage;
            GreatDamage = greatDamage;
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

        // ---------------------------------------------------------------------------------------
        // CRIMSON DOMINION — no longer a timed attack, it is a one-way PHASE.
        //
        // Round 3 rework. Dominion used to be a 720-tick special that ran a containment ring, twelve
        // arena-edge bolts and a finishing nova the player had to escape, then handed control back
        // with a cooldown. It is now a permanent phase transition at 30% health:
        //
        //   Phase 1  "Plant & hold"  — DominionHoldTicks (300t) as a normal blocking special attack:
        //                              the knight plants its spear, is engulfed, and cannot move.
        //                              Contact damage stays ON (this is a trap, not a free hit).
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

        public bool Active => Attack != KnightSpecialAttack.None;

        public string DebugAttackName => Attack switch
        {
            KnightSpecialAttack.EmberReversal => "Ember Reversal",
            KnightSpecialAttack.PoisonWake when Timer < 60 => "Spear Throw",
            KnightSpecialAttack.PoisonWake => "Poison Wake",
            KnightSpecialAttack.CrimsonStandard => "Crimson Standard",
            KnightSpecialAttack.CrimsonAdvance => "Crimson Advance",
            KnightSpecialAttack.FurnacePincer => "Furnace Pincer",
            KnightSpecialAttack.RoyalStandard => "Royal Standard",
            KnightSpecialAttack.StormbreakerEdict => "Stormbreaker Edict",
            KnightSpecialAttack.CrimsonDominion => "Crimson Dominion",
            KnightSpecialAttack.FurnaceHerald => "Furnace Herald",
            KnightSpecialAttack.StormHerald => "Storm Herald",
            _ => null
        };

        public bool IsHerald => Attack == KnightSpecialAttack.FurnaceHerald || Attack == KnightSpecialAttack.StormHerald;

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
            KnightSpecialAttack[] candidates = new KnightSpecialAttack[3];
            int count = 0;

            if (distance <= 210f)
            {
                candidates[count++] = KnightSpecialAttack.EmberReversal;
            }
            if (hasLineOfSight && distance >= 150f && distance <= 620f)
            {
                candidates[count++] = KnightSpecialAttack.PoisonWake;
            }
            if (tsorcRevampWorld.SuperHardMode && distance >= 120f && TryFindGround(target.Bottom, 10, 28, out Vector2 standardGround))
            {
                candidates[count++] = KnightSpecialAttack.CrimsonStandard;
                LockedTarget = standardGround;
            }

            if (count == 0)
            {
                attackCooldown = 30;
                return false;
            }

            KnightSpecialAttack selected = candidates[Main.rand.Next(count)];
            if (selected == KnightSpecialAttack.CrimsonStandard)
            {
                if (!TryFindGround(target.Bottom, 10, 28, out Vector2 lockedGround))
                {
                    attackCooldown = 30;
                    return false;
                }
                LockedTarget = lockedGround;
            }

            Begin(npc, target, globalNPC, selected);
            return true;
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
            KnightSpecialAttack[] candidates = new KnightSpecialAttack[5];
            int count = 0;

            if (distance <= 330f)
            {
                candidates[count++] = KnightSpecialAttack.CrimsonAdvance;
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
            // (CrimsonDominion is no longer a candidate — it is a forced phase transition above.)

            if (count == 0)
            {
                attackCooldown = 30;
                return false;
            }

            KnightSpecialAttack selected = candidates[Main.rand.Next(count)];
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
        /// Dominion's deliberately narrow authored pool. Crimson Advance is entered three times
        /// so it remains the phase's primary answer; Ember Reversal is the occasional close-range
        /// mix-up. The ordinary spear throw remains on GreatRedKnight's legacy ai[1] clock.
        /// </summary>
        public bool TryStartDominion(NPC npc, Player target, tsorcRevampGlobalNPC globalNPC)
        {
            if (!DominionEngaged || !CanStart(npc, target, globalNPC) || attackCooldown > 0)
            {
                return false;
            }

            float distance = npc.Distance(target.Center);
            bool hasLineOfSight = Collision.CanHitLine(npc.Center, 1, 1, target.Center, 1, 1);
            KnightSpecialAttack[] candidates = new KnightSpecialAttack[4];
            int count = 0;

            if (hasLineOfSight && distance <= 620f)
            {
                candidates[count++] = KnightSpecialAttack.CrimsonAdvance;
                candidates[count++] = KnightSpecialAttack.CrimsonAdvance;
                candidates[count++] = KnightSpecialAttack.CrimsonAdvance;
            }
            if (hasLineOfSight && distance <= 230f)
            {
                candidates[count++] = KnightSpecialAttack.EmberReversal;
            }

            if (count == 0)
            {
                attackCooldown = 30;
                return false;
            }

            Begin(npc, target, globalNPC, candidates[Main.rand.Next(count)]);
            return true;
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
                && globalNPC.PounceTimer <= 0
                && !globalNPC.Fleeing;
        }

        void Begin(NPC npc, Player target, tsorcRevampGlobalNPC globalNPC, KnightSpecialAttack attack)
        {
            Attack = attack;
            Timer = 0;
            Direction = target.Center.X >= npc.Center.X ? 1 : -1;
            LockedTarget = attack == KnightSpecialAttack.CrimsonStandard || attack == KnightSpecialAttack.RoyalStandard
                ? LockedTarget
                : target.Center;
            AuxiliaryTargetB = attack == KnightSpecialAttack.RoyalStandard ? AuxiliaryTargetB : Vector2.Zero;
            LockedVelocity = Vector2.Zero;
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
            tsorcRevampAIs.SpawnTelegraphFlash(npc, TelegraphColor(attack));
            npc.netUpdate = true;
        }

        static Color TelegraphColor(KnightSpecialAttack attack)
        {
            return attack switch
            {
                KnightSpecialAttack.PoisonWake => Color.GreenYellow,
                // Stormbreaker and the Storm Herald both used a cyan/ice-blue accent that no longer
                // matches anything they draw — every lightning in the kit is crimson now.
                KnightSpecialAttack.StormbreakerEdict => new Color(226, 40, 52),
                KnightSpecialAttack.StormHerald => new Color(206, 16, 34),
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
            SetCombatFlags(globalNPC);

            switch (Attack)
            {
                case KnightSpecialAttack.EmberReversal:
                    TickEmberReversal(npc, target, stats);
                    break;
                case KnightSpecialAttack.PoisonWake:
                    TickPoisonWake(npc, target, stats);
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
                case KnightSpecialAttack.CrimsonDominion:
                    TickCrimsonDominion(npc, stats);
                    break;
                case KnightSpecialAttack.FurnaceHerald:
                case KnightSpecialAttack.StormHerald:
                    TickHerald(npc, Attack == KnightSpecialAttack.StormHerald);
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
            if (Main.netMode != NetmodeID.MultiplayerClient && Timer % 60 == 0)
            {
                npc.netUpdate = true;
            }

            int duration = Duration(Attack);
            if (Timer >= duration)
            {
                Finish(npc, globalNPC);
            }
            return true;
        }

        void TickEmberReversal(NPC npc, Player target, KnightAttackStats stats)
        {
            if (Timer < 45)
            {
                npc.velocity.X *= 0.72f;
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
                npc.velocity.X *= 0.94f;
            }
            if (Timer == 120 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Vector2 velocity = UsefulFunctions.BallisticTrajectory(npc.Center, LockedTarget, 15f, 0.2f, highAngle: false, fallback: true);
                Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, velocity,
                    ModContent.ProjectileType<EnemyFirebomb>(), stats.SpearDamage, 0f, Main.myPlayer, ai2: 1f);
                PlaySound(SoundID.Item1 with { Volume = 0.9f, Pitch = -0.35f }, npc.Center);
            }
        }

        void TickPoisonWake(NPC npc, Player target, KnightAttackStats stats)
        {
            npc.velocity.X *= 0.8f;
            if (Timer == 30)
            {
                LockedTarget = target.Center;
                Direction = LockedTarget.X >= npc.Center.X ? 1 : -1;
                npc.netUpdate = true;
            }
            if (Timer == 60)
            {
                LockedVelocity = UsefulFunctions.BallisticTrajectory(npc.Center, LockedTarget, 13f, fallback: true);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, LockedVelocity,
                        ModContent.ProjectileType<BlackKnightSpear>(), stats.SpearDamage, 0f, Main.myPlayer, ai2: 1f);
                }
                PlaySound(SoundID.Item1 with { Volume = 0.8f }, npc.Center);
                npc.netUpdate = true;
            }
            if (Timer == 120 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Vector2 baseVelocity = LockedVelocity.SafeNormalize(new Vector2(Direction, 0f)) * 7f;
                for (int side = -1; side <= 1; side += 2)
                {
                    Vector2 velocity = baseVelocity.RotatedBy(side * 0.22f);
                    Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, velocity,
                        ModContent.ProjectileType<EnemySpellAbyssPoisonStrikeBall>(), stats.MagicDamage,
                        0f, Main.myPlayer, ai2: 1f);
                }
                PlaySound(SoundID.Item20 with { Volume = 0.65f, Pitch = 0.15f }, npc.Center);
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
                npc.velocity.X *= 0.7f;
            }
            if (Timer == 40)
            {
                LockedTarget = target.Center;
                Direction = LockedTarget.X >= npc.Center.X ? 1 : -1;
                npc.netUpdate = true;
            }
            if (Timer == 60)
            {
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
                npc.velocity.X *= 0.72f;
            }
            if (Timer == 105)
            {
                Direction = target.Center.X >= npc.Center.X ? 1 : -1;
                LockedTarget = target.Center;
                npc.netUpdate = true;
            }
            if (Timer == 135)
            {
                LockedVelocity = LeonhardDashVelocity(npc.Center, LockedTarget);
                SpawnLunge(npc, stats.GreatDamage, 96f, 54f, 22, 5.5f);
                npc.velocity = LockedVelocity;
                PlaySound(SoundID.Item1 with { Volume = 1f, Pitch = 0.05f }, npc.Center);
                npc.netUpdate = true;
            }
            if (Timer > 157)
            {
                npc.velocity.X *= 0.78f;
            }
        }

        void TickFurnacePincer(NPC npc, Player target, KnightAttackStats stats)
        {
            if (Timer < 60)
            {
                npc.velocity.X *= 0.76f;
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
                npc.velocity.X *= 0.75f;
            }
        }

        const int RoyalFirstThrow = 75;

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
                npc.velocity.X *= 0.7f;
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
                npc.velocity.X *= 0.35f;
                npc.netUpdate = true;
            }
            if (Timer > 82)
            {
                npc.velocity.X *= 0.8f;
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
            // The spear is planted, but the knight is NOT a free hit: touching it during the hold
            // has to hurt. GlobalNPC.CanHitPlayer (NPCs/GlobalNPC.cs:2645) is the ONLY thing that
            // suppresses contact damage, via DodgeTimer / QuickStepTimer / CombatMeleeActive /
            // InGuardPressureRecovery. Nothing zeroes NPC.damage anywhere in this kit, so contact
            // damage just needs those four to be clear.
            //   - CombatMeleeActive and the guard-pressure recovery are both cleared by the
            //     ResetCombatTempoSequence(clearRecovery: true) that Begin() already performs, and
            //     neither can restart while a special attack owns the knight. They are also both
            //     read-only properties, so they cannot be forced from here.
            //   - DodgeTimer / QuickStepTimer are settable, so clear them defensively every tick:
            //     an evasive reaction landing on the same frame as the plant would otherwise make
            //     the knight briefly intangible in the middle of its own trap.
            tsorcRevampGlobalNPC globalNPC = npc.GetGlobalNPC<tsorcRevampGlobalNPC>();
            globalNPC.DodgeTimer = 0;
            globalNPC.QuickStepTimer = 0;
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

        void TickHerald(NPC npc, bool storm)
        {
            npc.velocity.X *= 0.82f;
            Lighting.AddLight(npc.Center,
                (storm ? new Color(226, 40, 52) : new Color(255, 55, 20)).ToVector3()
                * (0.45f + TelegraphProgress * 0.55f));

            if (Timer == 0)
            {
                PlaySound(SoundID.Item74 with { Volume = 0.72f, Pitch = storm ? 0.25f : -0.55f }, npc.Center);
            }
            else if (Timer == 75)
            {
                PlaySound(storm
                    ? SoundID.Item122 with { Volume = 0.72f, Pitch = -0.15f }
                    : SoundID.Item20 with { Volume = 0.78f, Pitch = -0.35f }, npc.Center);
                tsorcRevampAIs.SpawnTelegraphFlash(npc,
                    storm ? new Color(226, 40, 52) : new Color(255, 75, 25));
            }
            else if (Timer == 135)
            {
                PlaySound(SoundID.Item1 with { Volume = 0.95f, Pitch = -0.45f }, npc.Center);
            }
        }

        void SetCombatFlags(tsorcRevampGlobalNPC globalNPC)
        {
            bool committed = Attack switch
            {
                KnightSpecialAttack.EmberReversal => Timer >= 45 && Timer < 57,
                KnightSpecialAttack.PoisonWake => (Timer >= 60 && Timer < 67) || (Timer >= 120 && Timer < 126),
                KnightSpecialAttack.CrimsonStandard => Timer >= 69 && Timer < 81,
                KnightSpecialAttack.CrimsonAdvance => (Timer >= 60 && Timer < 80) || (Timer >= 135 && Timer < 157),
                KnightSpecialAttack.FurnacePincer => Timer >= 165 && Timer < 203,
                // Brief release commitment around the single fire tick at 75.
                KnightSpecialAttack.RoyalStandard => Timer >= 69 && Timer < 78,
                KnightSpecialAttack.StormbreakerEdict => Timer >= 60 && Timer < 76,
                // Hyper-armored from the moment the spear is planted until it starts retracting,
                // so the hold cannot simply be staggered out of. Rescaled to the 300t hold.
                KnightSpecialAttack.CrimsonDominion => Timer >= 60 && Timer < DominionHoldTicks - 50,
                _ => false
            };
            bool dominionRecovery = Attack == KnightSpecialAttack.CrimsonDominion
                && Timer >= DominionHoldTicks - 50;
            globalNPC.AttackCommitted = committed;
            globalNPC.AttackTelegraphing = !committed && !dominionRecovery && !IsHerald && Timer < Duration(Attack) - 20;
        }

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

            Attack = KnightSpecialAttack.None;
            Timer = 0;
            LockedVelocity = Vector2.Zero;
            globalNPC.AttackTelegraphing = false;
            globalNPC.AttackCommitted = false;
            globalNPC.SetActiveAttackDefenseTraits(npc, AttackDefenseTraits.None);
            npc.ai[1] = 60f;
            npc.ai[2] = -100f;
            npc.velocity.X *= 0.35f;
            attackCooldown = IsGreatAttack(completed) ? Main.rand.Next(260, 401) : Main.rand.Next(320, 481);
            if (completed == KnightSpecialAttack.CrimsonDominion)
            {
                // PHASE 1 -> PHASE 2. The hold is over: the knight retracts its spear and goes back
                // to its narrow Dominion pool. GreatRedKnight gates the full special set and permits
                // only Crimson Advance, Ember Reversal and its ordinary spear throw.
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
            Attack = KnightSpecialAttack.None;
            Timer = 0;
            LockedVelocity = Vector2.Zero;
            globalNPC.AttackTelegraphing = false;
            globalNPC.AttackCommitted = false;
            globalNPC.SetActiveAttackDefenseTraits(npc, AttackDefenseTraits.None);
            attackCooldown = 120;
            npc.ai[1] = 60f;
            npc.ai[2] = -100f;
            npc.netUpdate = true;
        }

        static bool IsGreatAttack(KnightSpecialAttack attack)
        {
            return attack >= KnightSpecialAttack.CrimsonAdvance;
        }

        static int Duration(KnightSpecialAttack attack)
        {
            return attack switch
            {
                KnightSpecialAttack.EmberReversal => 155,
                KnightSpecialAttack.PoisonWake => 170,
                KnightSpecialAttack.CrimsonStandard => 210,
                KnightSpecialAttack.CrimsonAdvance => 195,
                KnightSpecialAttack.FurnacePincer => 230,
                KnightSpecialAttack.RoyalStandard => 200,
                KnightSpecialAttack.StormbreakerEdict => 200,
                // Dominion's BLOCKING part is now only the plant-and-hold. Everything that used to
                // fill the other 420 ticks (containment ring, arena-edge barrage, escape nova) is
                // either gone or moved: the lightning is an ongoing loop and the nova is the death
                // animation. See the phase notes at the top of this class.
                KnightSpecialAttack.CrimsonDominion => DominionHoldTicks,
                KnightSpecialAttack.FurnaceHerald => 150,
                KnightSpecialAttack.StormHerald => 150,
                _ => 1
            };
        }

        public KnightHeldProp HeldProp => Attack switch
        {
            KnightSpecialAttack.EmberReversal when Timer < 65 => KnightHeldProp.Spear,
            KnightSpecialAttack.EmberReversal when Timer < 120 => KnightHeldProp.Bomb,
            KnightSpecialAttack.PoisonWake when Timer < 60 => KnightHeldProp.Spear,
            KnightSpecialAttack.PoisonWake when Timer < 120 => KnightHeldProp.Magic,
            // Tick() processes the attack before incrementing Timer, so state 75 is still the
            // final pre-release draw frame. Keep the spear visible until that fire tick executes.
            KnightSpecialAttack.CrimsonStandard when Timer <= 75 => KnightHeldProp.Spear,
            KnightSpecialAttack.CrimsonAdvance => KnightHeldProp.Spear,
            KnightSpecialAttack.FurnacePincer when Timer < 60 => KnightHeldProp.Bomb,
            KnightSpecialAttack.FurnacePincer when Timer >= 105 && Timer < 205 => KnightHeldProp.Spear,
            KnightSpecialAttack.RoyalStandard when Timer <= RoyalFirstThrow => KnightHeldProp.Spear,
            KnightSpecialAttack.StormbreakerEdict when Timer < 122 => KnightHeldProp.Spear,
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
            else if (Attack == KnightSpecialAttack.PoisonWake && LockedTarget != Vector2.Zero && Timer >= 20)
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
                if (Attack == KnightSpecialAttack.EmberReversal)
                {
                    return PulseWindow(Timer, 45, 57, 14f);
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
            KnightSpecialAttack.EmberReversal => Timer >= 45 && Timer < 57,
            KnightSpecialAttack.CrimsonAdvance => (Timer >= 60 && Timer < 80) || (Timer >= 135 && Timer < 157),
            KnightSpecialAttack.FurnacePincer => Timer >= 165 && Timer < 203,
            KnightSpecialAttack.StormbreakerEdict => Timer >= 60 && Timer < 76,
            _ => false
        };

        public float TelegraphProgress
        {
            get
            {
                int telegraph = Attack switch
                {
                    KnightSpecialAttack.EmberReversal => 45,
                    KnightSpecialAttack.PoisonWake => Timer < 60 ? 60 : 120,
                    KnightSpecialAttack.CrimsonStandard => 75,
                    KnightSpecialAttack.CrimsonAdvance => Timer < 60 ? 60 : 135,
                    KnightSpecialAttack.FurnacePincer => Timer < 60 ? 60 : 165,
                    KnightSpecialAttack.RoyalStandard => 75,
                    KnightSpecialAttack.StormbreakerEdict => 60,
                    KnightSpecialAttack.CrimsonDominion => 90,
                    KnightSpecialAttack.FurnaceHerald or KnightSpecialAttack.StormHerald => 150,
                    _ => 1
                };
                int start = Attack switch
                {
                    KnightSpecialAttack.PoisonWake when Timer >= 60 => 60,
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

        internal static bool TryFindGround(Vector2 around, int searchUpTiles, int searchDownTiles, out Vector2 surface)
        {
            int tileX = Utils.Clamp((int)(around.X / 16f), 2, Main.maxTilesX - 3);
            int originY = Utils.Clamp((int)(around.Y / 16f), 5, Main.maxTilesY - 10);
            int startY = Utils.Clamp(originY - searchUpTiles, 5, Main.maxTilesY - 10);
            int endY = Utils.Clamp(originY + searchDownTiles, 5, Main.maxTilesY - 5);
            for (int tileY = startY; tileY <= endY; tileY++)
            {
                Tile tile = Framing.GetTileSafely(tileX, tileY);
                bool standable = tile.HasTile && !tile.IsActuated
                    && (Main.tileSolid[tile.TileType] || Main.tileSolidTop[tile.TileType]);
                Tile above = Framing.GetTileSafely(tileX, tileY - 1);
                bool blockedAbove = above.HasTile && !above.IsActuated
                    && Main.tileSolid[above.TileType] && !Main.tileSolidTop[above.TileType];
                if (standable && !blockedAbove)
                {
                    surface = new Vector2(tileX * 16f + 8f, tileY * 16f - 2f);
                    return true;
                }
            }
            surface = Vector2.Zero;
            return false;
        }

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

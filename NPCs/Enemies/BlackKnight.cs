using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;
using tsorcRevamp.Buffs.Debuffs;
using tsorcRevamp.Items;
using tsorcRevamp.Items.Weapons.Enemy;
using tsorcRevamp.Items.Weapons.Throwing;
using tsorcRevamp.Projectiles;

namespace tsorcRevamp.NPCs.Enemies
{
    // Split across BlackKnight.Attacks.cs, which owns the bag-driven attack state machine that replaced the
    // old ai[1]/ai[2] conveyor. Partial rather than a standalone controller (the Red Knight pattern) because
    // the attacks need direct access to this class's gravefall wave state, SpawnSpearProjectile and the
    // combo-followup helpers — a separate class would need a large callback surface to reach all of it, and
    // unlike RedKnightAttackController it would only ever serve one enemy.
    partial class BlackKnight : ModNPC, IHumanoidMeleeHitEffects, IDebugAttackLabel, IStaggerable, Projectiles.Enemy.Weapons.ISpearMeleeWielder
    {
        public int redKnightsSpearDamage = 15;
        const int HomingComboMoveKey = -10;
        const int BombComboMoveKey = -11;

        /// <summary>
        /// DebugMode above-head readout (see IDebugAttackLabel). Now reads the attack state machine directly
        /// instead of mirroring conveyor tick ranges that had to be hand-kept in sync with AI().
        /// </summary>
        public string DebugAttackLabel
        {
            get
            {
                tsorcRevampGlobalNPC globalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
                if (globalNPC.StaggerTimer > 0)
                {
                    return "Staggered";
                }
                if (globalNPC.CombatMeleeActive)
                {
                    return "Melee Combo";
                }
                string attackName = CurrentAttackDebugName;
                if (attackName != null)
                {
                    return InCommittedAttack(globalNPC) ? attackName : $"{attackName} (Windup)";
                }
                return "Idle";
            }
        }
        public int redMagicDamage = 14;
        public int redKnightsGreatDamage = 18;
        Vector2 storedPlayerPosition = Vector2.Zero;
        public int framesSinceStoredPosition = 0;

        // Black rain barrage state — see StartGravefallBarrage. Self-contained on its own 90-tick
        // timer rather than the big ai[2] numeric cycle, so a barrage can run 2-3 waves independent
        // of whatever else that cycle is doing.
        int gravefallWavesRemaining;
        int gravefallWaveTimer;
        int gravefallWaveIndex;
        bool gravefallWide;
        const int GravefallWaveCooldown = 90;


        NPCDespawnHandler despawnHandler;

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 16;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Confused] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.OnFire] = true;
        }
        public override void SetDefaults()
        {
            AnimationType = 28;
            NPC.aiStyle = -1;
            NPC.height = 40;
            NPC.width = 20;
            NPC.damage = 50;
            NPC.defense = 21;
            NPC.lifeMax = 1000;
            NPC.value = 5000;

            if (Main.hardMode)
            {
                NPC.lifeMax = 2000;
                NPC.damage = 65;
                NPC.defense = 50;
                NPC.value = 16000; // life / 1.25
                redKnightsGreatDamage = 21;
                redKnightsSpearDamage = 23;
                redMagicDamage = 19;
            }
            if (tsorcRevampWorld.SuperHardMode)
            {
                NPC.lifeMax = 4000;
                NPC.defense = 75;
                NPC.damage = 120;
                NPC.value = 16000; // life / 2.5
                redKnightsGreatDamage = 38;
                redKnightsSpearDamage = 32;
                redMagicDamage = 26;
            }

            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.lavaImmune = true;

            NPC.knockBackResist = 0.25f; // poise flinch dial: × PoiseFlinchFactor(0.4) ≈ 0.14 of full knockback per ordinary hit
            Banner = NPC.type;
            BannerItem = ModContent.ItemType<Banners.BlackKnightBanner>();
            //UsefulFunctions.AddAttack(NPC, 180, ModContent.ProjectileType<Projectiles.Enemy.BlackKnightSpear>(), spearDamage, 9, SoundID.Item17);

            // Identity: a slow, hulking plague-mage knight whose threat is commitment and area denial, not
            // speed — with rare, surprising bursts of agility rather than constant nimbleness. The settings
            // below used to describe an agile duelist (double jump, hop/dash evasion, a fast direct pounce),
            // which fought that read.
            tsorcRevampGlobalNPC blackKnightGlobalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            blackKnightGlobalNPC.Agility = 0.25f; // halved: he still dodges, just rarely enough to surprise
            blackKnightGlobalNPC.Aggression = 1f;
            blackKnightGlobalNPC.Patience = 2f;
            // Heavy slam landing instead of a nimble tackle: the leap is slow and honest, the payoff is the
            // impact shockwave (see PounceStyle.HeavyPounce / LandHeavyPounce).
            blackKnightGlobalNPC.PounceStyle = NPCs.PounceStyle.HeavyPounce;
            blackKnightGlobalNPC.PounceTelegraphColor = new Color(24, 10, 34); // plague black
            blackKnightGlobalNPC.HeavyPounceSlamDamage = redKnightsGreatDamage;
            blackKnightGlobalNPC.CanTeleport = true;
            blackKnightGlobalNPC.TeleportStyle = NPCs.TeleportStyle.Aggressive;
            blackKnightGlobalNPC.TeleportVisualStyle = NPCs.TeleportVisualStyle.Plague;
            // See GreatBlackKnight: 30 + SmokeFireTeleportSnapTicks (30) hidden, then he emerges with ~2s of
            // the 180-tick plague cloud still going, rather than after it has already finished.
            blackKnightGlobalNPC.TeleportTelegraphTime = 30;
            // NOT EvasiveProfile.RedKnight: that grants hop + dash + blink, which is duelist footwork. A
            // hulking mage keeps only the blink — a heavy thing does not scramble out of the way, it
            // removes itself by magic. Set directly rather than via a profile since no other enemy wants
            // this combination yet.
            blackKnightGlobalNPC.EvasiveTeleportAway = true;

            // Poise: sturdier than the Red Knight (40) — takes more to stagger. Tunable lever.
            blackKnightGlobalNPC.PoiseMax = 60f;
            // NOT PoiseStaggerResetsAI: that generic path writes ai[2] = -100, and ai[2] now holds the attack
            // ENUM rather than a free-running timer, so it would leave the knight holding an invalid attack.
            // IStaggerable.OnStagger below clears exactly this enemy's own state instead, and takes priority
            // when both are present. Same reason GreatBlackKnight implements the interface.

            // Navigation tuning: high jumps, double jump, and ledge routing
            blackKnightGlobalNPC.MaxJumpPower = 11f;
            blackKnightGlobalNPC.NavSearchRadius = 80; // Phase 2: SmartFighter4AI movement
            blackKnightGlobalNPC.CanUseRopes = true;
            blackKnightGlobalNPC.MaxJumpBoost = 7f;
            // No double jump. Nothing reads as heavy while it is kicking off thin air mid-leap; he routes
            // terrain with one honest jump or he goes around.
            blackKnightGlobalNPC.CanDoubleJump = false;
            // Spacing band. Posture itself is handled by the shared kite-threat system (GlobalNPC.KiteThreat):
            // aggressive by DEFAULT, a melee hit opens a ~4s kite window, and a distant projectile hit clears
            // that window and snaps him back to pursuit. So he already chases players who answer him at range.
            //
            // Looseness went 0.28 -> 0.9 because retreating read as skittish, but 0.9 meant he declined to back
            // off on ~90% of re-rolls — the band existed on paper only. The real problem was that retreating
            // cost the player nothing. Now every retreat drops a curse patch (RunPlagueTrail), so backing off
            // is zoning rather than fleeing, and the frequency can come back up to something meaningful.
            blackKnightGlobalNPC.KiteRangeMin = 2f;
            blackKnightGlobalNPC.KiteRangeMax = 15f;
            blackKnightGlobalNPC.KiteLooseness = 0.6f;

            int spearProjectileType = ModContent.ProjectileType<Projectiles.Enemy.BlackThrowingSpear>();
            HumanoidMeleeProfile meleeProfile = HumanoidMeleeProfile.Elite(
                redKnightsSpearDamage,
                (int)(redKnightsSpearDamage * 1.3f),
                closeTelegraphTicks: 45,
                longTelegraphTicks: 60,
                guardPressureUnblockable: true,
                guardPressureTelegraphTicks: 90,
                openerCondition: npc => npc.ai[1] >= 60f && npc.ai[1] < 90f);
            blackKnightGlobalNPC.ConfigureHumanoidMelee(meleeProfile);
            // Spacing duelist. The bands used to overlap heavily (spear/homing both 120+, bomb 160+), so at
            // most ranges every option was live and the choice was pure weight — which made the kiting read
            // as him being skittish rather than as a preferred engagement range.
            //
            // Now they partition around the kite band (KiteRangeMin/Max = 2..15 tiles = ~32..240px):
            //   inside the band  -> melee answers, and the spear is his only ranged option
            //   at the band edge -> bomb, the tool for someone sitting just outside his reach
            //   beyond the band  -> homing volley, the punish for trying to disengage
            // Weights shifted with them so each range genuinely has a signature answer.
            CombatTempoProfile.Elite(blackKnightGlobalNPC,
                new CombatComboMove(spearProjectileType, 120f, float.MaxValue, canRepeat: true, weight: 1.2f),
                new CombatComboMove(HomingComboMoveKey, 300f, 900f, canRepeat: false, weight: 1.25f),
                new CombatComboMove(BombComboMoveKey, 190f, 620f, canRepeat: false, weight: 1.15f),
                new CombatComboMove(CombatComboMoveKey.CloseHopMelee, 0f, 100f, canRepeat: true, weight: 1.1f),
                new CombatComboMove(CombatComboMoveKey.LongHopMelee, 100f, 300f, canRepeat: false, weight: 0.95f));
        }
        #region Spawn
        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            if (spawnInfo.Player.townNPCs > 1f) return 0f;
            if (!spawnInfo.Player.ZoneMeteor && !spawnInfo.Player.ZoneDungeon && !(spawnInfo.Player.ZoneCorrupt || spawnInfo.Player.ZoneCrimson) && spawnInfo.Player.ZoneOverworldHeight && NPC.downedBoss3 && !Main.dayTime && Main.rand.NextBool(250)) return 1;
            if (Main.hardMode && spawnInfo.Player.ZoneDungeon && Main.rand.NextBool(100)) return 1;
            if (Main.hardMode && !(spawnInfo.Player.ZoneCorrupt || spawnInfo.Player.ZoneCrimson) && !spawnInfo.Player.ZoneBeach && !Main.dayTime && Main.rand.NextBool(250)) return 1;
            if (Main.hardMode && spawnInfo.Player.ZoneUnderworldHeight && !Main.dayTime && Main.rand.NextBool(160)) return 1;
            if (tsorcRevampWorld.SuperHardMode && spawnInfo.Player.ZoneDungeon && Main.rand.NextBool(100)) return 1;

            return 0;
        }
        #endregion

        /// <summary>
        /// Curse Ward absorption: projectiles arriving from the warded side are eaten almost entirely.
        /// </summary>
        /// <remarks>
        /// Sidedness is taken from where the projectile CAME FROM (its previous position), not from its
        /// current one — by the time a hit registers it is usually already overlapping him, so the current
        /// position tells you nothing about which side it approached from.
        ///
        /// This is deliberately not total immunity: chip damage still lands, so a player who has no way to
        /// flank is slowed rather than hard-walled.
        /// </remarks>
        public override void ModifyHitByProjectile(Projectile projectile, ref NPC.HitModifiers modifiers)
        {
            if (!CurseWardActive)
            {
                return;
            }
            bool fromWardedSide = (projectile.oldPosition.X + projectile.width * 0.5f - NPC.Center.X) * CurseWardFacing > 0f;
            if (!fromWardedSide)
            {
                return; // flanked — the ward does nothing, which is the intended counterplay
            }

            modifiers.FinalDamage *= 0.12f;
            modifiers.Knockback *= 0f;
            // The ward drinks it. Charges the release volley in ReleaseWard, so shooting the front of it is
            // actively wrong rather than merely wasted DPS. Accumulated from the projectile's base damage
            // rather than the post-mitigation result, since what matters is how hard the player committed.
            RegisterWardAbsorption(projectile.damage);
            if (Main.rand.NextBool(3))
            {
                Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCHit4 with { PitchVariance = 0.3f, Volume = 0.6f }, NPC.Center);
            }
        }

        #region Hit Logic
        // Hit logic is stored in GlobalNPC
        public override void OnHitByItem(Player player, Item item, NPC.HitInfo hit, int damageDone)
        {
            tsorcRevampAIs.EvasiveOnHit(NPC, true);
        }
        public override void OnHitByProjectile(Projectile projectile, NPC.HitInfo hit, int damageDone)
        {
            tsorcRevampAIs.EvasiveOnHit(NPC, projectile.DamageType == DamageClass.Melee);
        }

        public Player player
        {
            get => Main.player[NPC.target];
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(storedPlayerPosition.X);
            writer.Write(storedPlayerPosition.Y);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            storedPlayerPosition = new Vector2(reader.ReadSingle(), reader.ReadSingle());
        }
        #endregion

        private void SpawnSpearProjectile(Vector2 velocity, tsorcRevampGlobalNPC globalNPC)
        {
            int projectileIndex = Projectile.NewProjectile(
                NPC.GetSource_FromThis(), NPC.Center, velocity,
                ModContent.ProjectileType<Projectiles.Enemy.BlackThrowingSpear>(),
                redKnightsSpearDamage, 0f, Main.myPlayer);
            tsorcGlobalProjectile.SetDefenseTraits(projectileIndex, globalNPC.ActiveAttackDefenseTraits);
        }

        private void CompleteSpearAttack(tsorcRevampGlobalNPC globalNPC)
        {
            if (globalNPC.ActiveAttackBypassesShield)
            {
                globalNPC.EndCombatTempoSequenceWithoutFollowup(NPC);
            }
            else
            {
                TryQueueComboFollowup(globalNPC, ModContent.ProjectileType<Projectiles.Enemy.BlackThrowingSpear>());
            }
        }

        /// <summary>
        /// Black rain barrage. Replaces the old two-step Prepare(ground-tear)/Release(drop) dance: each
        /// drop now carries its OWN telegraph (EnemyBlackCursedBreath freezes and spins in place for
        /// 45 ticks before falling), so a single call fires wave 1 immediately and schedules 1-2 more,
        /// GravefallWaveCooldown ticks apart, ticked from AI(). Up to 2 nearby players each get their
        /// own batch when more than one is around.
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
            targets.Sort((a, b) => NPC.Distance(a.Center).CompareTo(NPC.Distance(b.Center)));
            if (targets.Count > 2)
            {
                targets.RemoveRange(2, targets.Count - 2);
            }
            return targets;
        }

        /// <summary>
        /// One wave of the black rain barrage, sized by the barrage's own escalation state.
        /// </summary>
        /// <remarks>
        /// Escalates a little each wave — more drops, wider spread — so a 2-3 wave barrage doesn't just
        /// repeat the same shape.
        /// </remarks>
        private void FireGravefallWave() => FireGravefallWave(
            count: (gravefallWide ? 6 : 4) + gravefallWaveIndex,
            spreadDeg: 45f + gravefallWaveIndex * 12f,
            radius: 260f + gravefallWaveIndex * 25f,
            knockback: gravefallWide ? 4f : 3f);

        /// <summary>
        /// Explicitly-sized rain wave. Split out from the barrage so other attacks can borrow the rain
        /// without inheriting its escalation state — the Curse Ward fires deliberately sparse waves, and
        /// fudging gravefallWaveIndex to get them would corrupt any barrage already in flight.
        /// </summary>
        private void FireGravefallWave(int count, float spreadDeg, float radius, float knockback)
        {
            List<Player> targets = GetGravefallTargets();
            if (targets.Count == 0 || count <= 0)
            {
                return;
            }

            foreach (Player target in targets)
            {
                for (int i = 0; i < count; i++)
                {
                    // Fan above AND to the sides rather than a uniform scatter directly overhead.
                    float t = count == 1 ? 0f : (i / (float)(count - 1)) - 0.5f; // -0.5..0.5
                    float angleRad = MathHelper.ToRadians(t * spreadDeg + Main.rand.NextFloat(-6f, 6f));
                    Vector2 offset = new Vector2((float)Math.Sin(angleRad), -(float)Math.Cos(angleRad))
                        * (radius + Main.rand.NextFloat(-20f, 20f));
                    Vector2 spawnPosition = target.Center + offset;

                    // Fan outward as well as straight down (left projectiles drift left, center fall straight down, right drift right).
                    Vector2 dir = new Vector2(t * 0.75f, 1f).SafeNormalize(Vector2.UnitY);
                    Vector2 fallVelocity = dir * Main.rand.NextFloat(4.5f, 6.5f);

                    if (!Main.dedServ)
                    {
                        for (int d = 0; d < 30; d++)
                        {
                            Vector2 vel = Main.rand.NextVector2Circular(3.5f, 3.5f);
                            Dust dust = Dust.NewDustPerfect(spawnPosition, DustID.Wraith, vel, 100, default, Main.rand.NextFloat(1.0f, 1.5f));
                            dust.noGravity = true;
                        }
                    }

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), spawnPosition, fallVelocity,
                            ModContent.ProjectileType<Projectiles.Enemy.EnemyBlackCursedBreath>(), redMagicDamage,
                            knockback, Main.myPlayer, 1f);

                        // Brief "portal opening" flash at the same spawn point — a fast pre-cue riding
                        // on top of the drop's own longer frozen-spin windup.
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), spawnPosition, Vector2.Zero,
                            ModContent.ProjectileType<Projectiles.Enemy.BlackKnightGravefallTelegraph>(), 0, 0f, Main.myPlayer);
                    }
                }
            }
        }

        private void TryCompressGuardPressureNeutral(tsorcRevampGlobalNPC globalNPC)
        {
            if (globalNPC.ActiveAttackBypassesShield)
            {
                return;
            }

            // Shared melee claims the fourth stack first when it is executable. This is the ranged spear fallback
            // when Black Knight cannot currently reach with either distance-aware melee move.
            if (Main.netMode != NetmodeID.MultiplayerClient && globalNPC.IsMaximumGuardPressureReady(NPC))
            {
                globalNPC.TryBeginGuardPressureSequence(NPC);
                if (globalNPC.GetActiveGuardPressureSequenceStacks(NPC) >= tsorcRevampGlobalNPC.GuardPressureMaxBlocks)
                {
                    globalNPC.SetActiveAttackDefenseTraits(NPC, AttackDefenseTraits.BypassesActiveShield);
                    // Forced unblockable spear. Used to be addressed by dropping the conveyor to ai[1] = 89
                    // so it landed in the long-windup branch; now it just starts the attack, which reads
                    // ActiveAttackBypassesShield itself and picks the 90-tick tell (SpearWindup).
                    BeginAttack(BlackKnightAttack.SpearThrow, globalNPC);
                    globalNPC.AttackTelegraphing = true;
                    globalNPC.AttackCommitted = false;
                    tsorcRevampAIs.SpawnTelegraphFlash(NPC, Color.Red);
                    NPC.netUpdate = true;
                    return;
                }
            }

            // Guard pressure used to buy the knight a fast-forward through the conveyor's dead air so his
            // next attack arrived sooner. With attacks bag-selected there is no dead air to skip — the
            // equivalent reward is simply shortening the cooldown before the next opener.
            if (AttackActive || attackCooldown <= 0)
            {
                return;
            }
            int skippedTicks = globalNPC.TryGetGuardPressureNeutralSkip(NPC, attackCooldown, attackCooldown);
            if (skippedTicks > 0)
            {
                attackCooldown = Math.Max(0, attackCooldown - skippedTicks);
                NPC.netUpdate = true;
            }
        }
        private bool TryQueueComboFollowup(tsorcRevampGlobalNPC globalNPC, int completedMoveKey)
        {
            bool continueCombo = globalNPC.TryChooseCombatComboFollowup(
                NPC,
                completedMoveKey,
                attackEndsCombo: false,
                moveKey => !globalNPC.CanHumanoidMeleeHandleMove(moveKey) || globalNPC.CanExecuteHumanoidMeleeMove(NPC, moveKey),
                out int followupMoveKey,
                out _,
                out _);
            if (!continueCombo)
            {
                return false;
            }

            globalNPC.QueueCombatComboMove(followupMoveKey, globalNPC.GetCombatComboGapTicks(NPC));
            NPC.ai[1] = 60f;
            NPC.netUpdate = true;
            return true;
        }

        private bool HoldForOrStartQueuedComboMove(tsorcRevampGlobalNPC globalNPC)
        {
            if (!globalNPC.HasPendingCombatComboMove)
            {
                return false;
            }

            NPC.ai[1] = 60f;
            if (globalNPC.PendingCombatMoveGapTimer > 0 || Main.netMode == NetmodeID.MultiplayerClient)
            {
                return true;
            }

            int moveKey = globalNPC.PendingCombatMoveKey;
            if (!globalNPC.TryGetCombatComboMove(moveKey, out CombatComboMove move) || !move.IsInRange(NPC))
            {
                globalNPC.EndInvalidQueuedCombatMove(NPC);
                return true;
            }

            // Maps a queued CombatComboMove onto a real attack. This is the seam between the two systems:
            // CombatTempo still chooses FOLLOWUPS (weighted, range-banded, damage-urgency scaled), the bag
            // only chooses openers. Previously this "addressed" an attack by dropping the conveyor to the
            // tick just before its window (124 / 269 / 869) and letting the belt run into it.
            BlackKnightAttack queuedAttack;
            if (moveKey == ModContent.ProjectileType<Projectiles.Enemy.BlackThrowingSpear>())
            {
                queuedAttack = BlackKnightAttack.SpearThrow;
            }
            else if (moveKey == HomingComboMoveKey)
            {
                queuedAttack = BlackKnightAttack.HomingVolley;
            }
            else if (moveKey == BombComboMoveKey)
            {
                queuedAttack = BlackKnightAttack.BombThrow;
            }
            else
            {
                globalNPC.EndInvalidQueuedCombatMove(NPC);
                return true;
            }

            globalNPC.TryConsumePendingCombatComboMove(moveKey);
            BeginAttack(queuedAttack, globalNPC);
            return false;
        }

        public override void AI()
        {
            tsorcRevampAIs.FighterAI(NPC, 1.5f, 0.05f, enragePercent: 0.5f, enrageTopSpeed: 2.9f, canTeleport: true, canDodgeroll: true);
            Lighting.AddLight(NPC.Center, Color.GhostWhite.ToVector3() * 2f);

            //if (NPC.GetGlobalNPC<tsorcRevampGlobalNPC>().ProjectileTimer >= 150f && NPC.justHit)
            //{
            //    NPC.GetGlobalNPC<tsorcRevampGlobalNPC>().ProjectileTimer = 100f; // reset throw countdown when hit, was 150
            //}

            Vector2 targetPosition = Vector2.Zero;

            // Block firing and reset cooldowns if it's busy doing other things that it shouldn't be able to shoot during
            tsorcRevampGlobalNPC globalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();

            if (globalNPC.CombatMeleeActive)
            {
                // Shared melee owns the body while active. Pause this bespoke attack clock so finishing a jab or
                // leap resumes the authored ranged cycle instead of restarting it and starving later attacks.
                return;
            }

            if (HoldForOrStartQueuedComboMove(globalNPC))
            {
                return;
            }

            // Hyper-armor / telegraph windows. These used to be hardcoded ai[1] tick ranges (spear 155→180,
            // poison 300→375, bomb 900→925 and so on) that had to be kept in sync by hand with the conveyor;
            // they are now asked of the attack state machine, which owns the real answer. See
            // BlackKnight.Attacks.cs — InCommittedAttack is deliberately the FLASH→fire span only, so a
            // stagger can still interrupt a windup.
            globalNPC.AttackCommitted = InCommittedAttack(globalNPC);
            globalNPC.AttackTelegraphing = InAttackTelegraph(globalNPC);
            if (!globalNPC.AttackTelegraphing && !globalNPC.AttackCommitted
                && !globalNPC.HasPendingCombatComboMove && !globalNPC.InCombatComboRecovery)
            {
                TryCompressGuardPressureNeutral(globalNPC);
            }

            if (globalNPC.TeleportCountdown > 0 || globalNPC.TeleportAppearanceTimer > 0 || globalNPC.PursuitState == NPCs.PursuitState.Patrol || globalNPC.Fleeing || globalNPC.DodgeTimer > 0 || globalNPC.PounceTimer > 0 || globalNPC.DirectPounceAfterimageTimer > 0 || globalNPC.DirectPounceRecoveryTimer > 0)
            {
                // A busy state cancels a windup but never a committed attack — same carve-out as before,
                // minus the magic numbers. PlagueAmbush reports committed for its whole duration precisely so
                // its own teleport cannot cancel it here.
                if (!InCommittedAttack(globalNPC))
                {
                    NPC.ai[1] = 60f;
                    EndAttackExternally();
                    globalNPC.ResetCombatTempoSequence(clearRecovery: true);
                }
            }

            if (Main.netMode != 1 && !Main.player[NPC.target].dead)
            {
                // ai[1] is now ONLY the cadence clock the melee opener and combo followups read (60..90 opener
                // window; followups reset it to 60). It no longer addresses attacks, so it just needs to keep
                // ticking and wrap rather than climbing forever.
                if (globalNPC.StaggerTimer <= 0)
                {
                    NPC.ai[1]++;
                    if (NPC.ai[1] > 1200f)
                    {
                        NPC.ai[1] = 60f;
                    }
                }
                NPC.knockBackResist = globalNPC.BaseKnockBackResist; // restore the SetDefaults value; poise scales it to a light flinch

                // Gate all projectile firing on LOS — prevents shooting through floors/ceilings
                bool hasPlayerLOS = Collision.CanHitLine(NPC.Center, 1, 1, player.Center, 1, 1);

                #region Sounds & Jumps
                // Play creature sounds
                if (Main.rand.NextBool(1500))
                {
                    Terraria.Audio.SoundEngine.PlaySound(new Terraria.Audio.SoundStyle("tsorcRevamp/Sounds/DarkSouls/ominous-creature2") with { Volume = 0.8f }, NPC.Center);
                }
                // These three jump/dash flourishes set NPC.velocity directly and had no idea a teleport
                // could be in flight, so they could fire mid-hold and overwrite the velocity-freeze the
                // teleport code applies every tick — a real position kick, read as the knight vibrating
                // a few pixels back and forth while it should be sitting perfectly still and invisible.
                bool teleportHold = globalNPC.TeleportCountdown > 0 || globalNPC.TeleportAppearanceTimer > 0;

                // The ai[1] range checks these used to carry ("only flourish in the conveyor's dead zones")
                // are now just "don't flourish during an attack" — the attack state machine knows, and the
                // throws drive their own movement anyway (RunThrowMovement), which these would fight.
                bool attackBusy = AttackActive || globalNPC.HasPendingCombatComboMove || globalNPC.CombatMeleeActive;

                // Chance to jump forward
                if (!teleportHold && !attackBusy && NPC.Distance(player.Center) > 250 && NPC.velocity.Y == 0f && Main.rand.NextBool(300))
                {
                    NPC.velocity.Y = Main.rand.NextFloat(-4, -8f);
                    NPC.TargetClosest(true);
                    NPC.velocity.X = NPC.velocity.X + (float)NPC.direction * 2f;
                    if ((float)NPC.direction * NPC.velocity.X > 2)
                        NPC.velocity.X = (float)NPC.direction * 2;
                    NPC.netUpdate = true;
                }
                // Chance to dash step forward
                if (!teleportHold && !attackBusy && NPC.Distance(player.Center) > 200 && NPC.velocity.Y == 0f && Main.rand.NextBool(140))
                {
                    NPC.velocity.Y = -4f;
                    NPC.velocity.X = NPC.velocity.X * 4f; // burst forward

                    if ((float)NPC.direction * NPC.velocity.X > 4)
                        NPC.velocity.X = (float)NPC.direction * 4;

                    // Chance to jump after dash
                    if (Main.rand.NextBool(6))
                    {
                        NPC.velocity.Y = -8f;
                    }
                    NPC.netUpdate = true;
                }
                #endregion
                // (The old "offensive jump before 3 attacks" flourish is gone: it keyed off the conveyor ticks
                // 145/275/890, and the throws now choose a real, terrain-validated hop instead — see
                // ChooseThrowMovement. The "skip spear inside 120px" guard is likewise now a range band on
                // the bag candidate itself, SpearMinRange.)

                // Increment the frames since we stored the player's position
                framesSinceStoredPosition++;

                // ── Attacks ──────────────────────────────────────────────────────────────────────
                // Everything that used to live here as four #regions of tick-window checks (spear at
                // ai[1] 120-200, homing at 265-375, bomb at 865-925, and black rain / death seal on
                // the separate ai[2] belt) is now an explicit state machine with bag-selected openers
                // in BlackKnight.Attacks.cs. See that file's header for the ai[] layout.
                TickAttacks(globalNPC, hasPlayerLOS);

                // Shadow Crystal Storm — Phase 3 at 1/3 health
                // Telegraph: shadow dust spirals inward for 20 frames before the burst
                if (NPC.life <= NPC.lifeMax / 3 && Main.GameUpdateCount % 420 >= 400 && Main.rand.NextBool(2))
                {
                    Vector2 dustOffset = Main.rand.NextVector2Circular(64, 64);
                    int dustIdx = Dust.NewDust(NPC.Center + dustOffset - new Vector2(4), 8, 8, DustID.ShadowbeamStaff,
                        -dustOffset.X * 0.1f, -dustOffset.Y * 0.1f, 150, default, 1.2f);
                    Main.dust[dustIdx].noGravity = true;
                }
                // Attack: 5 homing crystals in a 60° spread
                if (NPC.life <= NPC.lifeMax / 3 && Main.GameUpdateCount % 420 == 0 && Main.netMode != NetmodeID.MultiplayerClient && hasPlayerLOS)
                {
                    int numCrystals = 5;
                    float totalSpread = MathHelper.Pi / 3f; // 60 degrees total
                    for (int i = 0; i < numCrystals; i++)
                    {
                        float angle = (i - (numCrystals - 1) / 2f) * (totalSpread / (numCrystals - 1));
                        Vector2 speed = UsefulFunctions.BallisticTrajectory(NPC.Center, player.Center, 10f, fallback: true);
                        speed += player.velocity / 2f;
                        speed = speed.RotatedBy(angle);
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y, speed.X, speed.Y, ModContent.ProjectileType<Projectiles.Enemy.EnemyBlackKnightHomingCrystal>(), redMagicDamage, 0f, Main.myPlayer);
                    }
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item69 with { Volume = 1f, Pitch = -0.3f, PitchVariance = 0.2f }, NPC.Center);
                    NPC.netUpdate = true;
                }

            }
        }


        /*
        static Texture2D spearTexture;
        public override void PostDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (spearTexture == null || spearTexture.IsDisposed)
            {
                spearTexture = (Texture2D)Mod.Assets.Request<Texture2D>("Projectiles/Enemy/BlackKnightGhostSpear");
            }
            if (NPC.GetGlobalNPC<tsorcRevampGlobalNPC>().ProjectileTimer >= NPC.GetGlobalNPC<tsorcRevampGlobalNPC>().ProjectileTelegraphStart)
            {
                float rotation = UsefulFunctions.Aim(NPC.Center, Main.player[NPC.target].Center, 1).ToRotation() + MathHelper.PiOver2;
                SpriteEffects effects = NPC.spriteDirection < 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally; 
                spriteBatch.Draw(spearTexture, NPC.Center - Main.screenPosition, new Rectangle(0, 0, spearTexture.Width, spearTexture.Height), drawColor, rotation, spearTexture.Size() / 2, 1, SpriteEffects.None, 0);
            }
        }
        */
        #region Debuffs
        public override void OnHitPlayer(Player target, Player.HurtInfo hurtInfo)
        {
            ApplyHitDebuffs(target);
        }

        public void OnHumanoidMeleeHit(Player target)
        {
            ApplyHitDebuffs(target);
        }

        private static void ApplyHitDebuffs(Player target)
        {
            target.AddBuff(ModContent.BuffType<BrokenSpirit>(), 600, false);
            target.AddBuff(36, 600, false); //broken armor
            target.AddBuff(ModContent.BuffType<CurseBuildup>(), 18000, false);
            target.AddBuff(ModContent.BuffType<GrappleMalfunction>(), 30 * 60, false);
        }
        #endregion

        #region Draw Attack Sprites
        static Texture2D spearTexture;
        static Texture2D bombTexture;
        static Texture2D handTexture;
        const float FrameW = 70f;
        const float FrameH = 56f;
        static readonly Vector2[] HandPixel = new Vector2[16]
        {
            new Vector2(47, 30), // 0 idle
            new Vector2(47, 25), // 1 jump
            new Vector2(48, 33), // 2
            new Vector2(50, 31), // 3
            new Vector2(50, 31), // 4
            new Vector2(50, 31), // 5
            new Vector2(50, 33), // 6
            new Vector2(48, 33), // 7
            new Vector2(48, 33), // 8
            new Vector2(48, 33), // 9
            new Vector2(46, 31), // 10
            new Vector2(44, 31), // 11
            new Vector2(44, 31), // 12
            new Vector2(45, 32), // 13
            new Vector2(48, 33), // 14
            new Vector2(48, 33), // 15
        };
        // BlackThrowingSpear is 14px wide, so x=7 is its exact rotation center. The old x=8
        // became a vertical offset once the upright texture was rotated horizontal: 0.8px high
        // while facing right (and low while facing left), plus the melee-only -3px lift below.
        static readonly Vector2 SpearGripOrigin = new Vector2(7f, 38f);
        static readonly Vector2 BombGripOrigin = new Vector2(14f, 4f);
        const float PreviousHeldSpearScale = 0.8f;
        const float HeldSpearScale = 0.9f;
        const float SpearTipForwardCorrection = 5f;

        // Lowered to 0f so the spear sits dead center in the hand for the throw telegraph.
        const float IdleGripLift = 0f;

        // One frame-index helper so the hand and the spear can never disagree about which frame
        // they are on.
        int CurrentFrameIndex()
        {
            int frame = NPC.frame.Height > 0 ? NPC.frame.Y / NPC.frame.Height : 0;
            return frame < 0 || frame >= HandPixel.Length ? 0 : frame;
        }

        // Single frame-pixel -> world mapping. Everything that attaches to the body goes through it,
        // so a held prop's draw position and the occlusion mask's lookup cannot drift apart.
        // facingDirection is explicit because a melee attack locks its own direction: rotating the
        // spear by the locked direction while mirroring its anchor by NPC.spriteDirection put the
        // shaft on the far side of the body whenever the two disagreed.
        Vector2 FramePixelToWorld(Vector2 fp, int facingDirection)
        {
            float x = NPC.Center.X + (fp.X - FrameW / 2f) * NPC.scale * -facingDirection;
            float y = NPC.Center.Y + 24f + NPC.gfxOffY + (fp.Y - FrameH) * NPC.scale;
            return new Vector2(x, y);
        }

        // The RANGED-THROW grip only: a fixed idle-based Y so the overhead cocked pose reads
        // consistently regardless of which walk frame happens to be active (it must not pop when the
        // frame changes mid-windup). Do NOT use this for melee — see CurrentHandWorld for that.
        Vector2 CurrentThrowSpearFramePixel()
        {
            return new Vector2(HandPixel[CurrentFrameIndex()].X, HandPixel[0].Y - IdleGripLift / NPC.scale);
        }

        Vector2 CurrentHandWorld(int facingDirection) => FramePixelToWorld(HandPixel[CurrentFrameIndex()], facingDirection);

        Vector2 CurrentHandWorld() => CurrentHandWorld(NPC.spriteDirection);

        // Ranged spear-throw telegraph/hold only (overhead cocked pose).
        Vector2 CurrentThrowSpearWorld(int facingDirection) => FramePixelToWorld(CurrentThrowSpearFramePixel(), facingDirection);

        Vector2 CurrentThrowSpearWorld() => CurrentThrowSpearWorld(NPC.spriteDirection);

        // 0 disables the body mask and draws the spear on top of the body NPC sheet.
        const float SpearMaskStrength = 0f;

        // Changing 0.8 -> 0.9 already lengthens the grip-to-tip distance. Convert only the
        // REMAINDER of the requested 5-world-pixel correction back into spear-texture pixels so
        // the fixed hand anchor stays put and the shaft slides through the fist by exactly 5px.
        float BaseSpearGripSlide(float spriteScale)
        {
            float drawScale = NPC.scale * spriteScale;
            float oldTipReach = SpearGripOrigin.Y * NPC.scale * PreviousHeldSpearScale;
            float targetTipReach = oldTipReach + SpearTipForwardCorrection;
            return targetTipReach / drawScale - SpearGripOrigin.Y;
        }

        // gripFramePixel MUST be whichever frame-pixel the caller used to compute screenPosition
        // (CurrentHandWorld for melee, CurrentThrowSpearWorld for the ranged throw) — the occlusion
        // mask samples the body sheet at that same pixel, and a mismatch desyncs the mask from
        // where the spear actually sits.
        void DrawHeldSpear(SpriteBatch spriteBatch, Vector2 screenPosition, float rotation, Color drawColor,
            tsorcRevampGlobalNPC globalNPC, float spriteScale, Vector2 gripFramePixel,
            float gripSlide = 0f, int facingDirection = 0)
        {
            Vector2 gripOrigin = SpearGripOrigin + new Vector2(0f, BaseSpearGripSlide(spriteScale) + gripSlide);
            if (globalNPC.ActiveAttackBypassesShield)
            {
                Vector2 forward = (rotation - MathHelper.PiOver2).ToRotationVector2();
                Vector2 auraCenter = screenPosition + forward * gripSlide;
                // The aura is a silhouette glow, deliberately drawn unmasked so the unblockable tell
                // stays legible even where the body would cover the shaft.
                AttackTelegraphDraw.DrawUnblockableWeaponAura(
                    spriteBatch, spearTexture, screenPosition, null, rotation, gripOrigin, NPC.scale * spriteScale);
                drawColor = Color.Lerp(drawColor, new Color(255, 55, 55), 0.8f);
                Lighting.AddLight(auraCenter + Main.screenPosition, Color.Red.ToVector3() * 0.75f);
            }

            if (facingDirection == 0)
            {
                facingDirection = NPC.spriteDirection;
            }

            // Punch the knight's own silhouette out of the shaft so it reads as gripped rather than
            // laid over the sprite. The mask is the body sheet, not BlackKnight_Hand: that overlay is
            // only a 14x8 fist on the walk frames and hides almost nothing.
            HeldPropDraw.DrawOccluded(
                spriteBatch, spearTexture, screenPosition, drawColor, rotation, gripOrigin,
                NPC.scale * spriteScale,
                Terraria.GameContent.TextureAssets.Npc[NPC.type].Value,
                new Rectangle(0, NPC.frame.Y, (int)FrameW, (int)FrameH),
                gripFramePixel, facingDirection, NPC.scale, SpearMaskStrength);
        }
        void DrawHandOverlay(SpriteBatch spriteBatch, Color drawColor) => DrawHandOverlay(spriteBatch, drawColor, NPC.spriteDirection);

        void DrawHandOverlay(SpriteBatch spriteBatch, Color drawColor, int facingDirection)
        {
            if (handTexture == null)
            {
                return;
            }

            SpriteEffects effects = facingDirection < 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
            Rectangle sourceRectangle = new Rectangle(0, NPC.frame.Y, (int)FrameW, (int)FrameH);
            Vector2 drawPosition = NPC.Center + new Vector2(0f, 24f + NPC.gfxOffY) - Main.screenPosition;
            spriteBatch.Draw(handTexture, drawPosition, sourceRectangle, drawColor, NPC.rotation, new Vector2(FrameW / 2f, FrameH), NPC.scale, effects, 0f);
        }

        void DrawBlackKnightMagicOverlays()
        {
            // Death Seal's gathering sigil + aim thread. These read the attack's own clock now; ai[2] holds
            // the attack enum, so the old `ai[2] >= 100` window would never be true again.
            if (CurrentAttack == BlackKnightAttack.DeathSeal)
            {
                int t = AttackTimer;
                Projectiles.Enemy.EnemyVFX.DrawBlackKnightDeathSeal(NPC.Center,
                    MathHelper.Clamp(t / (float)DeathSealTelegraphTicks, 0f, 1f));
                if (t >= DeathSealFlashTick && storedPlayerPosition != Vector2.Zero)
                {
                    Projectiles.Enemy.EnemyVFX.DrawBlackKnightAimThread(NPC.Center, storedPlayerPosition,
                        MathHelper.Clamp((t - DeathSealFlashTick) / (float)(DeathSealTelegraphTicks - DeathSealFlashTick), 0f, 1f));
                }
            }

            ulong stormCycle = Main.GameUpdateCount % 420;
            if (NPC.life <= NPC.lifeMax / 3 && stormCycle >= 400)
            {
                float stormProgress = MathHelper.Clamp((stormCycle - 400f) / 20f, 0f, 1f);
                Projectiles.Enemy.EnemyVFX.DrawBlackKnightHexCrystal(NPC.Center, Vector2.Zero, stormProgress, false);
            }
        }

        public override void PostDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (spearTexture == null || spearTexture.IsDisposed)
            {
                spearTexture = (Texture2D)Mod.Assets.Request<Texture2D>("Projectiles/Enemy/BlackThrowingSpear");
            }

            if (bombTexture == null || bombTexture.IsDisposed)
            {
                bombTexture = (Texture2D)Mod.Assets.Request<Texture2D>("Projectiles/Enemy/EnemyMoonfuryBomb");
            }

            if (handTexture == null || handTexture.IsDisposed)
            {
                handTexture = ModContent.Request<Texture2D>("tsorcRevamp/NPCs/Enemies/BlackKnight_Hand", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
            }

            tsorcRevampGlobalNPC globalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            if (globalNPC.CombatMeleeActive)
            {
                const float spriteScale = HeldSpearScale;
                int meleeDirection = globalNPC.ActiveCombatMeleeDirection;
                float maximumExtension = globalNPC.ActiveCombatMeleeKey == CombatComboMoveKey.LongHopMelee ? 22f : 14f;
                float thrustOffset = globalNPC.CombatMeleeThrustProgress * maximumExtension;
                // Melee grips at the RAW per-frame hand pixel (same as the bomb/hand overlay), not
                // the ranged-throw's overhead-cocked lift — that lift was designed for a stand-and-
                // throw pose and put the jab up near the knight's head when applied to a melee swing.
                Vector2 handWorld = CurrentHandWorld(meleeDirection) - Main.screenPosition;
                float rotation = new Vector2(meleeDirection, 0f).ToRotation() + MathHelper.PiOver2;
                if (globalNPC.InCombatMeleeHitWindow)
                {
                    Vector2 forward = new Vector2(meleeDirection, 0f);
                    float tipReach = (SpearGripOrigin.Y + BaseSpearGripSlide(spriteScale) + thrustOffset)
                        * NPC.scale * spriteScale;
                    Projectiles.Enemy.EnemyVFX.DrawBlackKnightSpearWake(
                        handWorld + Main.screenPosition + forward * tipReach,
                        forward.ToRotation(), new Vector2(82f, 18f), 0.62f);
                }
                DrawHeldSpear(spriteBatch, handWorld, rotation, drawColor, globalNPC, spriteScale,
                    HandPixel[CurrentFrameIndex()], thrustOffset, meleeDirection);
                DrawHandOverlay(spriteBatch, drawColor, meleeDirection);
                DrawBlackKnightMagicOverlays();
                return;
            }

            // Held props are now driven by the attack state machine rather than by conveyor tick ranges.
            // Aim snaps to the predicted point once the attack commits (the flash), matching what the throw
            // itself will actually use; before that the weapon just tracks the body's facing.
            bool committed = InCommittedAttack(globalNPC);

            // Spear. SpearMeleeActive covers LeapStrike (from launch until the apex throw) and the counter
            // check, so the weapon is visibly in hand for the whole arc rather than appearing from nowhere
            // when it is released. Those two aim along the body; only the throw leads a predicted point.
            if (CurrentAttack == BlackKnightAttack.SpearThrow || SpearMeleeActive)
            {
                float spriteScale = HeldSpearScale;
                bool leadsAim = CurrentAttack == BlackKnightAttack.SpearThrow && committed;
                Vector2 spearAim = leadsAim ? UsefulFunctions.Aim(NPC.Center, storedPlayerPosition, 1) : new Vector2(NPC.spriteDirection, 0f);
                float rotation = spearAim.ToRotation() + MathHelper.PiOver2;
                Vector2 handWorld = CurrentThrowSpearWorld() - Main.screenPosition;
                DrawHeldSpear(spriteBatch, handWorld, rotation, drawColor, globalNPC, spriteScale,
                    CurrentThrowSpearFramePixel());
                DrawHandOverlay(spriteBatch, drawColor);
            }
            // Bomb
            if (CurrentAttack == BlackKnightAttack.BombThrow)
            {
                Vector2 bombAim = committed ? UsefulFunctions.Aim(NPC.Center, storedPlayerPosition, 1) : new Vector2(NPC.spriteDirection, 0f);
                float rotation = bombAim.ToRotation() + MathHelper.PiOver2;
                Vector2 handWorld = CurrentHandWorld() - Main.screenPosition;
                float fuseProgress = MathHelper.Clamp(AttackTimer / (float)BombWindupTicks, 0f, 1f);
                // The Moonfury shader used to be drawn here and sat visibly offset from the bomb
                // sprite. Replaced with a red fuse spark, which cannot drift out of alignment.
                spriteBatch.Draw(bombTexture, handWorld, new Rectangle(0, 0, bombTexture.Width, bombTexture.Height), drawColor, rotation, BombGripOrigin, NPC.scale, SpriteEffects.None, 0);
                Projectiles.Enemy.EnemyVFX.SpawnBombFuseSparks(handWorld + Main.screenPosition, fuseProgress);
                DrawHandOverlay(spriteBatch, drawColor);
            }

            // Staff, for the Plague Seals cast. Vanilla's Amethyst Staff rather than a bespoke sprite: it is
            // a wooden shaft with a purple head, which is exactly the brief, and vanilla magic-staff item
            // sprites are already drawn on the up-right diagonal — so rotation 0 with the grip as the origin
            // IS the 45-degree hold, with no per-frame angle maths to drift out of sync with the hand.
            if (CastingStaff)
            {
                Texture2D staff = Terraria.GameContent.TextureAssets.Item[ItemID.AmethystStaff].Value;
                Vector2 grip = CurrentHandWorld() - Main.screenPosition;
                SpriteEffects staffFlip = NPC.spriteDirection < 0 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
                spriteBatch.Draw(staff, grip, null, drawColor, 0f,
                    new Vector2(0f, staff.Height), NPC.scale, staffFlip, 0f);
                DrawHandOverlay(spriteBatch, drawColor);
            }

            // Curse Ward plane. Drawn AFTER the body so the barrier reads as standing between him and the
            // player rather than being worn; it is anchored to the locked facing (CurseWardFacing), NOT to
            // spriteDirection, so it visibly stays put on the committed side — which is the whole tell that
            // flanking works.
            float wardRise = CurseWardRise;
            if (wardRise > 0.001f)
            {
                Projectiles.Enemy.EnemyVFX.DrawBlackKnightCurseWard(NPC.Center, CurseWardFacing, wardRise, 0.9f * wardRise);
            }

            DrawBlackKnightMagicOverlays();

        }
        #endregion

        #region Gore
        public override void OnKill()
        {
            // create unknown embers that fill the explosion's radius
            for (int i = 0; i < 30; i++)
            {
                float velX = 1f - ((float)Main.rand.Next(20)) / 5f;
                float velY = 1f - ((float)Main.rand.Next(20)) / 5f;
                velX *= 2f;
                velY *= 2f;
                Dust.NewDust(new Vector2(NPC.position.X - (float)(NPC.width / 2), NPC.position.Y - (float)(NPC.height / 2)), NPC.width, NPC.height, DustID.EnchantedNightcrawler, velX, velY, 160, default, 1.5f);
                Dust.NewDust(new Vector2(NPC.position.X - (float)(NPC.width / 2), NPC.position.Y - (float)(NPC.height / 2)), NPC.width, NPC.height, DustID.CosmicEmber, velX, velY, 160, default, 1.5f);
            }

            if (!Main.dedServ)
            {
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Black Knight Gore 1").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Black Knight Gore 2").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Black Knight Gore 3").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Black Knight Gore 2").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Black Knight Gore 3").Type, 1f);
            }
        }
        #endregion

        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<OilPot>(), 1, 2, 6));
            npcLoot.Add(ItemDropRule.Common(ItemID.GreaterHealingPotion, 1));
            npcLoot.Add(new CommonDrop(ModContent.ItemType<Items.Weapons.Throwing.ThrowingSpear>(), 100, 1, 50, 30));
            npcLoot.Add(new CommonDrop(ModContent.ItemType<Items.Weapons.Throwing.RoyalThrowingSpear>(), 100, 1, 50, 30));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<Items.Accessories.Mobility.BootsOfHaste>(), 10));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<Items.Weapons.Melee.Spears.AncientDragonLance>(), 20));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<Items.Weapons.Melee.Spears.OldHalberd>(), 5));
            npcLoot.Add(new CommonDrop(ItemID.IronskinPotion, 5, 1, 50, 2));
            npcLoot.Add(new CommonDrop(ItemID.ArcheryPotion, 5, 1, 50, 2));
            npcLoot.Add(new CommonDrop(ItemID.RegenerationPotion, 5, 1, 50, 2));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<Humanity>(), 1, 1, 2));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<PurgingStone>(), 1, 0, 1));
        }
    }
}

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
    class BlackKnight : ModNPC, IHumanoidMeleeHitEffects, IDebugAttackLabel
    {
        public int redKnightsSpearDamage = 15;
        const int HomingComboMoveKey = -10;
        const int BombComboMoveKey = -11;

        /// <summary>
        /// DebugMode above-head readout (see IDebugAttackLabel). This enemy has no attack enum - its
        /// moves are windows on the ai[1]/ai[2] clocks - so the ranges below mirror the firing points
        /// in AI(). Keep them in sync if those timings move.
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
                // ai[2] clock: the half-health Ultrakill channel and the air-dropped death waves.
                if (NPC.life <= NPC.lifeMax / 2 && NPC.ai[2] >= 100f && NPC.ai[2] <= 235f)
                {
                    return NPC.ai[2] < 200f ? "Ultrakill (Telegraph)" : "Ultrakill Barrage";
                }
                if ((NPC.ai[2] >= 70f && NPC.ai[2] <= 105f) || (NPC.ai[2] >= 520f && NPC.ai[2] <= 605f))
                {
                    return "Death From Above";
                }
                // ai[1] clock: spear 155->180, homing 300->375, bomb 900->925 (windups precede each).
                if (NPC.ai[1] >= 120f && NPC.ai[1] <= 200f)
                {
                    return NPC.ai[1] < 155f ? "Spear Throw (Windup)" : "Spear Throw";
                }
                if (NPC.ai[1] >= 270f && NPC.ai[1] <= 400f)
                {
                    return NPC.ai[1] < 300f ? "Homing Volley (Windup)" : "Homing Volley";
                }
                if (NPC.ai[1] >= 865f && NPC.ai[1] <= 950f)
                {
                    return NPC.ai[1] < 900f ? "Bomb Throw (Windup)" : "Bomb Throw";
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

            tsorcRevampGlobalNPC blackKnightGlobalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            blackKnightGlobalNPC.Agility = 0.5f;
            blackKnightGlobalNPC.Aggression = 1f;
            blackKnightGlobalNPC.Patience = 2f;
            blackKnightGlobalNPC.PounceStyle = NPCs.PounceStyle.DirectPounce;
            blackKnightGlobalNPC.CanTeleport = true;
            blackKnightGlobalNPC.TeleportStyle = NPCs.TeleportStyle.Aggressive;
            blackKnightGlobalNPC.TeleportVisualStyle = NPCs.TeleportVisualStyle.Plague;
            EvasiveProfile.RedKnight(blackKnightGlobalNPC); // shared knight-family evasion: hop/leap/dash/blink away

            // Poise: sturdier than the Red Knight (40) — takes more to stagger. Tunable lever.
            blackKnightGlobalNPC.PoiseMax = 60f;
            blackKnightGlobalNPC.PoiseStaggerResetsAI = true; // a stagger cancels a windup attack → neutral

            // Navigation tuning: high jumps, double jump, and ledge routing
            blackKnightGlobalNPC.MaxJumpPower = 11f;
            blackKnightGlobalNPC.NavSearchRadius = 80; // Phase 2: SmartFighter4AI movement
            blackKnightGlobalNPC.CanUseRopes = true;
            blackKnightGlobalNPC.MaxJumpBoost = 7f;
            blackKnightGlobalNPC.CanDoubleJump = true;
            blackKnightGlobalNPC.DoubleJumpPower = 7f;
            // Item/close-range hits restore this anti-melee spacing band. Distant projectile hits temporarily
            // collapse it to zero so the knight closes in; the attack pool remains distance-aware in both postures.
            // Min dropped 8->2 and looseness pushed way up (0.28->0.9): the old values read as rigidly
            // holding a fixed distance band. At 0.9 the knight backs off far less often per re-roll
            // window, so melee can actually close and the spacing feels like a preference, not a wall.
            blackKnightGlobalNPC.KiteRangeMin = 2f;
            blackKnightGlobalNPC.KiteRangeMax = 15f;
            blackKnightGlobalNPC.KiteLooseness = 0.9f;

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
            CombatTempoProfile.Elite(blackKnightGlobalNPC,
                new CombatComboMove(spearProjectileType, 120f, float.MaxValue, canRepeat: true, weight: 1.2f),
                new CombatComboMove(HomingComboMoveKey, 120f, 900f, canRepeat: false, weight: 1f),
                new CombatComboMove(BombComboMoveKey, 160f, 900f, canRepeat: false, weight: 1.05f),
                new CombatComboMove(CombatComboMoveKey.CloseHopMelee, 0f, 90f, canRepeat: true, weight: 0.8f),
                new CombatComboMove(CombatComboMoveKey.LongHopMelee, 112f, 280f, canRepeat: false, weight: 0.75f));
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

        private void FireGravefallWave()
        {
            List<Player> targets = GetGravefallTargets();
            if (targets.Count == 0)
            {
                return;
            }

            // Escalates a little each wave — more drops, wider spread — so a 2-3 wave barrage doesn't
            // just repeat the same shape.
            int count = (gravefallWide ? 6 : 4) + gravefallWaveIndex;
            float spreadDeg = 45f + gravefallWaveIndex * 12f;
            float radius = 260f + gravefallWaveIndex * 25f;

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
                            gravefallWide ? 4f : 3f, Main.myPlayer, 1f);

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
                    NPC.ai[1] = 89f; // increments to 90 below: 90 held-spear ticks before release at 180
                    globalNPC.AttackTelegraphing = true;
                    globalNPC.AttackCommitted = false;
                    tsorcRevampAIs.SpawnTelegraphFlash(NPC, Color.Red);
                    NPC.netUpdate = true;
                    return;
                }
            }

            float timer = NPC.ai[1];
            float telegraphBoundary;
            float nextExactEvent;

            if (timer < 125f)
            {
                telegraphBoundary = 125f;
                nextExactEvent = timer < 120f ? 120f : 125f;
            }
            else if (timer > 180f && timer < 270f)
            {
                telegraphBoundary = 270f;
                nextExactEvent = timer < 265f ? 265f : 270f;
            }
            else if (timer > 375f && timer < 870f)
            {
                telegraphBoundary = 870f;
                nextExactEvent = 870f;
            }
            else
            {
                return;
            }

            int remainingNeutralTicks = Math.Max(0, (int)Math.Floor(telegraphBoundary - timer));
            int maximumSafeSkip = Math.Max(0, (int)Math.Floor(nextExactEvent - timer) - 1);
            int skippedTicks = globalNPC.TryGetGuardPressureNeutralSkip(NPC, remainingNeutralTicks, maximumSafeSkip);
            if (skippedTicks > 0)
            {
                NPC.ai[1] += skippedTicks;
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

            int timerBeforeWindup;
            if (moveKey == ModContent.ProjectileType<Projectiles.Enemy.BlackThrowingSpear>())
            {
                timerBeforeWindup = 124;
            }
            else if (moveKey == HomingComboMoveKey)
            {
                timerBeforeWindup = 269;
            }
            else if (moveKey == BombComboMoveKey)
            {
                timerBeforeWindup = 869;
            }
            else
            {
                globalNPC.EndInvalidQueuedCombatMove(NPC);
                return true;
            }

            globalNPC.TryConsumePendingCombatComboMove(moveKey);
            NPC.ai[1] = timerBeforeWindup;
            NPC.netUpdate = true;
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

            // Hyper-armor window: FLASH telegraph → fire only. Windup excluded so a stagger can interrupt it.
            // Spear 155→180, poison 300→375, bomb 900→925.
            bool spearUnblockable = globalNPC.ActiveAttackBypassesShield;
            globalNPC.AttackCommitted = (NPC.ai[1] >= 155f && NPC.ai[1] <= 180f) ||
                                        (NPC.ai[1] >= 300f && NPC.ai[1] <= 375f) ||
                                        (NPC.ai[1] >= 900f && NPC.ai[1] <= 925f);

            // The unblockable spear adds a full 90-tick held-weapon tell without shortening the ordinary tell.
            globalNPC.AttackTelegraphing = (spearUnblockable
                                               ? NPC.ai[1] >= 89f && NPC.ai[1] < 155f
                                               : NPC.ai[1] >= 125f && NPC.ai[1] < 155f) ||
                                           (NPC.ai[1] >= 270f && NPC.ai[1] < 300f) ||
                                           (NPC.ai[1] >= 870f && NPC.ai[1] < 900f);
            if (!globalNPC.AttackTelegraphing && !globalNPC.AttackCommitted
                && !globalNPC.HasPendingCombatComboMove && !globalNPC.InCombatComboRecovery
                && (NPC.ai[2] < 100f || NPC.ai[2] > 235f))
            {
                TryCompressGuardPressureNeutral(globalNPC);
            }

            if (globalNPC.TeleportCountdown > 0 || globalNPC.TeleportAppearanceTimer > 0 || globalNPC.PursuitState == NPCs.PursuitState.Patrol || globalNPC.Fleeing || globalNPC.DodgeTimer > 0 || globalNPC.PounceTimer > 0 || globalNPC.DirectPounceAfterimageTimer > 0 || globalNPC.DirectPounceRecoveryTimer > 0)
            {
                bool inProtectedAttack = (NPC.ai[1] >= 155f && NPC.ai[1] <= 180f) ||
                                          (NPC.ai[1] >= 300f && NPC.ai[1] <= 375f) ||
                                          (NPC.ai[1] >= 900f && NPC.ai[1] <= 925f) ||
                                          (NPC.ai[2] >= 165f && NPC.ai[2] <= 235f);
                if (!inProtectedAttack)
                {
                    NPC.ai[1] = 60f;
                    NPC.ai[2] = -100f;
                    globalNPC.ResetCombatTempoSequence(clearRecovery: true);
                }
            }

            if (Main.netMode != 1 && !Main.player[NPC.target].dead)
            {
                // Freeze the attack timer while staggered so the ~1s stun holds (and a reset windup stays neutral).
                if (globalNPC.StaggerTimer <= 0)
                {
                    NPC.ai[1]++;
                    NPC.ai[2]++;
                }
                NPC.knockBackResist = globalNPC.BaseKnockBackResist; // restore the SetDefaults value; poise scales it to a light flinch

                bool inActiveAttack = (NPC.ai[1] >= 155f && NPC.ai[1] <= 180f) ||
                                       (NPC.ai[1] >= 300f && NPC.ai[1] <= 375f) ||
                                       (NPC.ai[1] >= 900f && NPC.ai[1] <= 925f) ||
                                       (NPC.ai[2] >= 165f && NPC.ai[2] <= 235f);

                // Gate all projectile firing on LOS — prevents shooting through floors/ceilings
                bool hasPlayerLOS = Collision.CanHitLine(NPC.Center, 1, 1, player.Center, 1, 1);

                #region Sounds & Jumps
                // Play creature sounds
                if (Main.rand.NextBool(1500))
                {
                    Terraria.Audio.SoundEngine.PlaySound(new Terraria.Audio.SoundStyle("tsorcRevamp/Sounds/DarkSouls/ominous-creature2") with { Volume = 0.8f }, NPC.Center);
                }
                // Chance to jump forward
                if (NPC.Distance(player.Center) > 250 && NPC.velocity.Y == 0f && Main.rand.NextBool(300) && (NPC.ai[1] <= 150f || NPC.ai[1] >= 476f))
                {
                    NPC.velocity.Y = Main.rand.NextFloat(-4, -8f);
                    NPC.TargetClosest(true);
                    NPC.velocity.X = NPC.velocity.X + (float)NPC.direction * 2f;
                    if ((float)NPC.direction * NPC.velocity.X > 2)
                        NPC.velocity.X = (float)NPC.direction * 2;
                    NPC.netUpdate = true;
                }
                // Chance to dash step forward
                if (NPC.Distance(player.Center) > 200 && NPC.velocity.Y == 0f && Main.rand.NextBool(140) && (NPC.ai[1] <= 220f || NPC.ai[1] >= 276f))
                {
                    NPC.velocity.Y = -4f;
                    NPC.velocity.X = NPC.velocity.X * 4f; // burst forward

                    if ((float)NPC.direction * NPC.velocity.X > 4)
                        NPC.velocity.X = (float)NPC.direction * 4;

                    // Chance to jump after dash
                    if (Main.rand.NextBool(6) && (NPC.ai[1] <= 150f || NPC.ai[1] >= 476f))
                    {
                        NPC.velocity.Y = -8f;
                    }
                    NPC.netUpdate = true;
                }
                // Offensive jump before 3 attacks
                if ((NPC.ai[1] == 145 || NPC.ai[1] == 275 || NPC.ai[1] == 890) && NPC.velocity.Y <= 0f && Main.rand.NextBool(4))
                {
                    NPC.velocity.Y = Main.rand.NextFloat(-6, -10f);
                    NPC.netUpdate = true;
                }
                #endregion

                // Skip spear if player is at melee range — it's a ranged weapon
                if (NPC.ai[1] == 120f && NPC.Distance(player.Center) < 120f && !globalNPC.ActiveAttackBypassesShield)
                {
                    NPC.ai[1] = 200f;
                    NPC.netUpdate = true;
                }

                // Increment the frames since we stored the player's position
                framesSinceStoredPosition++;

                #region Spear Attack
                // Spear Attack: Get targetPosition 
                if (NPC.ai[1] >= 155f && NPC.ai[1] <= 180f)
                {
                    NPC.knockBackResist = 0f;
                    // Calculate the direction towards the stored player position.
                    int direction = (storedPlayerPosition.X > NPC.Center.X) ? 1 : -1;

                    // Use the stored player's position from 25 frames ago to calculate the targetPosition.
                    targetPosition = new Vector2(storedPlayerPosition.X + 10f * direction, storedPlayerPosition.Y);
                }

                // Spear Telegraph
                if (NPC.ai[1] == 155f)
                {
                    Vector2 spawnPosition = NPC.position;
                    if (NPC.direction == 1)
                    {
                        spawnPosition.X += NPC.width;
                    }
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), spawnPosition, NPC.velocity, ModContent.ProjectileType<Projectiles.VFX.TelegraphFlash>(), 0, 0, Main.myPlayer, UsefulFunctions.ColorToFloat(globalNPC.ActiveAttackBypassesShield ? Color.Red : Color.OrangeRed));
                    }

                    // Store the player's center
                    if (framesSinceStoredPosition >= 25)
                    {
                        framesSinceStoredPosition = 0;
                        int targetPlayer = NPC.target;
                        if (Main.player[targetPlayer].active && !Main.player[targetPlayer].dead)
                        {
                            storedPlayerPosition = Main.player[targetPlayer].Center;
                            NPC.netUpdate = true;
                            NPC.netUpdate = true;
                            NPC.netUpdate = true;
                        }
                    }
                }

                // Spear Attack Far
                if (NPC.ai[1] == 180f && NPC.Distance(player.Center) > 400 && hasPlayerLOS)
                {
                    NPC.TargetClosest(true);
                    float spearProjectileSpeed = Main.rand.NextFloat(16, 18f);

                    Vector2 speed = UsefulFunctions.BallisticTrajectory(NPC.Center, targetPosition, spearProjectileSpeed, fallback: true);
                    //speed += Main.rand.NextVector2Circular(-6, -2);
                    //speed.Y += Main.rand.NextFloat(-2f, 2f); //adds random variation from -1 to 2
                    speed += Main.player[NPC.target].velocity;
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        SpawnSpearProjectile(speed, globalNPC);
                    }
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.8f, PitchVariance = 0.1f }, NPC.Center);

                    // Reset the targetPosition 
                    targetPosition = Vector2.Zero;

                    // Move closer to next attack
                    NPC.ai[1] = 200f;

                    CompleteSpearAttack(globalNPC);
                }
                // Spear Attack Close
                if (NPC.ai[1] == 180f && NPC.Distance(player.Center) <= 400 && hasPlayerLOS)
                {
                    NPC.TargetClosest(true);
                    float spearProjectileSpeed = Main.rand.NextFloat(12, 14f);

                    Vector2 speed = UsefulFunctions.BallisticTrajectory(NPC.Center, targetPosition, spearProjectileSpeed, fallback: true);
                    //speed += Main.rand.NextVector2Circular(-6, -2);
                    //speed.Y += Main.rand.NextFloat(-1f, 1f); //adds random variation from -1 to 2
                    speed += Main.player[NPC.target].velocity;
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        SpawnSpearProjectile(speed, globalNPC);
                    }
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.8f, PitchVariance = 0.1f }, NPC.Center);

                    // Reset the targetPosition 
                    targetPosition = Vector2.Zero;

                    // Move closer to next attack
                    NPC.ai[1] = 200f;

                    CompleteSpearAttack(globalNPC);
                }
                if (NPC.ai[1] == 180f && !hasPlayerLOS)
                {
                    globalNPC.EndCombatTempoSequenceWithoutFollowup(NPC);
                    // STUCK-HELD-WEAPON FIX: this used to leave ai[1] sitting at 180 with nothing to
                    // reset it. Since ai[1] then just kept climbing past every range check below
                    // (they're all exact-tick `==` matches), the knight stayed frozen holding the
                    // spear pose indefinitely — only rescued by an unrelated teleport/dodge/flee
                    // event, which is why it eventually "resolved" after several random seconds.
                    NPC.ai[1] = 0f;
                    NPC.netUpdate = true;
                }

                #endregion

                #region Homing Attack
                // Poison Attack 1 Telegraph 
                // Part 1: Dusts
                if (NPC.ai[1] >= 265 && NPC.ai[1] <= 325)
                {
                    if (Main.rand.NextBool(2))
                    {
                        int dust2 = Dust.NewDust(new Vector2((float)NPC.position.X, (float)NPC.position.Y), NPC.width, NPC.height, 6, NPC.velocity.X - 6f, NPC.velocity.Y, DustID.ShadowbeamStaff, Color.DarkSlateGray, 0.5f);
                        Main.dust[dust2].noGravity = true;
                    }
                }

                // Part 2: Flash 
                if (NPC.ai[1] == 300)
                {
                    Vector2 spawnPosition = NPC.position;
                    if (NPC.direction == 1)
                    {
                        spawnPosition.X += NPC.width;
                    }
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), spawnPosition, NPC.velocity, ModContent.ProjectileType<Projectiles.VFX.TelegraphFlash>(), 0, 0, Main.myPlayer, UsefulFunctions.ColorToFloat(Color.Orange));
                    }
                }

                // Homing Attack 1
                if (NPC.ai[1] == 325 && hasPlayerLOS)
                {
                    float projectileSpeed = 15f;
                    float projectileSpread = MathHelper.Pi / 6f; // Angle between each projectile (30 degrees in radians)
                    int numProjectiles = 1; // Number of projectiles to shoot

                    for (int i = 0; i < numProjectiles; i++)
                    {
                        float angle = i * projectileSpread - (projectileSpread * (numProjectiles - 1)) / 2f;

                        // Adjust the angle to cover only the upward half of the circle (from 0 to 180 degrees)
                        if (angle > MathHelper.PiOver2)
                        {
                            angle = MathHelper.Pi - angle;
                        }

                        Vector2 speed2 = UsefulFunctions.BallisticTrajectory(NPC.Center, Main.player[NPC.target].Center, projectileSpeed, 2.1f, highAngle: true, fallback: true);
                        speed2 += Main.player[NPC.target].velocity; //was 4
                        speed2 = speed2.RotatedBy(angle); // Rotate the projectile speed vector by the angle

                        if (((speed2.X < 0f) && (NPC.direction < 0)) || ((speed2.X > 0f) && (NPC.direction > 0)))
                        {
                            if (Main.netMode != NetmodeID.MultiplayerClient)
                            {
                                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y, speed2.X, speed2.Y, ModContent.ProjectileType<Projectiles.Enemy.EnemyBlackKnightHomingCrystal>(), redMagicDamage, 0f, Main.myPlayer);
                            }
                        }
                    }

                    // Reset the targetPosition
                    targetPosition = Vector2.Zero;

                }

                // Homing Attack 2 Telegraph
                if (NPC.ai[1] == 350)
                {
                    Vector2 spawnPosition = NPC.position;
                    if (NPC.direction == 1)
                    {
                        spawnPosition.X += NPC.width;
                    }
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), spawnPosition, NPC.velocity, ModContent.ProjectileType<Projectiles.VFX.TelegraphFlash>(), 0, 0, Main.myPlayer, UsefulFunctions.ColorToFloat(Color.Orange));
                    }
                }

                // Homing Attack 2
                if (NPC.ai[1] == 375 && hasPlayerLOS)
                {
                    float projectileSpeed = 18f;
                    float projectileSpread = MathHelper.Pi / 6f; // Angle between each projectile (30 degrees in radians)
                    int numProjectiles = 2; // Number of projectiles to shoot

                    for (int i = 0; i < numProjectiles; i++)
                    {
                        float angle = i * projectileSpread - (projectileSpread * (numProjectiles - 1)) / 2f;

                        // Adjust the angle to cover only the upward half of the circle (from 0 to 180 degrees)
                        if (angle > MathHelper.PiOver2)
                        {
                            angle = MathHelper.Pi - angle;
                        }

                        Vector2 speed2 = UsefulFunctions.BallisticTrajectory(NPC.Center, Main.player[NPC.target].Center, projectileSpeed, 1.1f, highAngle: true, fallback: true);
                        //speed2 += Main.player[NPC.target].velocity / 2; //was 4
                        speed2 = speed2.RotatedBy(angle); // Rotate the projectile speed vector by the angle

                        if (((speed2.X < 0f) && (NPC.direction < 0)) || ((speed2.X > 0f) && (NPC.direction > 0)))
                        {
                            if (Main.netMode != NetmodeID.MultiplayerClient)
                            {
                                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y, speed2.X, speed2.Y, ModContent.ProjectileType<Projectiles.Enemy.EnemyBlackKnightHomingCrystal>(), redMagicDamage, 0f, Main.myPlayer);
                            }
                        }
                    }

                    if (!TryQueueComboFollowup(globalNPC, HomingComboMoveKey))
                    {
                        // Preserve the authored variable pause when the combo ends here.
                        if (Main.rand.NextBool(2))
                        {
                            NPC.ai[1] = 700f;
                            NPC.netUpdate = true;
                        }
                        else
                        {
                            NPC.ai[1] = 800f;
                            NPC.netUpdate = true;
                        }
                    }
                }
                if (NPC.ai[1] == 375f && !hasPlayerLOS)
                {
                    globalNPC.EndCombatTempoSequenceWithoutFollowup(NPC);
                    // Same stuck-timer fix as the spear/bomb LOS-abandon branches — see that comment.
                    NPC.ai[1] = 0f;
                    NPC.netUpdate = true;
                }

                #endregion

                #region Bomb Attack
                // Code for Bomb Telegraph & Attack: 
                if (NPC.ai[1] >= 900f && NPC.ai[1] <= 925f)
                {
                    NPC.knockBackResist = 0f;
                    // Calculate the direction towards the stored player position.
                    int direction = (storedPlayerPosition.X > NPC.Center.X) ? 1 : -1;

                    targetPosition = new Vector2(storedPlayerPosition.X + 10f * direction, storedPlayerPosition.Y); //trying + 10 again

                }

                // Bomb Telegraph
                if (NPC.ai[1] == 900f)
                {
                    Terraria.Audio.SoundEngine.PlaySound(UsefulFunctions.BombFuse with { Volume = 0.6f }, NPC.Center); // lit fuse
                    Vector2 spawnPosition = NPC.position;
                    if (NPC.direction == 1)
                    {
                        spawnPosition.X += NPC.width;
                    }
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), spawnPosition, NPC.velocity, ModContent.ProjectileType<Projectiles.VFX.TelegraphFlash>(), 0, 0, Main.myPlayer, UsefulFunctions.ColorToFloat(Color.OrangeRed));
                        Lighting.AddLight(NPC.Center, Color.OrangeRed.ToVector3() * 3f);
                    }

                    // Store the player's center
                    if (framesSinceStoredPosition >= 25)
                    {
                        framesSinceStoredPosition = 0;
                        int targetPlayer = NPC.target;
                        if (Main.player[targetPlayer].active && !Main.player[targetPlayer].dead)
                        {
                            storedPlayerPosition = Main.player[targetPlayer].Center;
                        }
                    }

                }
                // Bomb Attack Far
                if (NPC.ai[1] == 925f && NPC.Distance(player.Center) > 400 && hasPlayerLOS)
                {
                    float bombProjectileSpeed = 8f;

                    Vector2 speed = UsefulFunctions.BallisticTrajectory(NPC.Center, targetPosition, bombProjectileSpeed, fallback: true);

                    //speed.Y += Main.rand.NextFloat(-1f, -2f); //adds random variation from -1 to 2
                    speed += Main.player[NPC.target].velocity;

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y, speed.X, speed.Y, ModContent.ProjectileType<Projectiles.Enemy.EnemyMoonfuryBomb>(), redKnightsSpearDamage, 0f, Main.myPlayer);
                    }
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item1 with { Volume = 1f, Pitch = -0.5f }, NPC.Center);

                    // Reset targetPosition 
                    targetPosition = Vector2.Zero;

                    // Reset attack counter
                    NPC.ai[1] = 0f;

                    TryQueueComboFollowup(globalNPC, BombComboMoveKey);
                }
                // Bomb Attack Close
                if (NPC.ai[1] == 925f && NPC.Distance(player.Center) <= 400 && hasPlayerLOS)
                {
                    float bombProjectileSpeed = 5f;
                    Vector2 speed = UsefulFunctions.BallisticTrajectory(NPC.Center, targetPosition, bombProjectileSpeed, fallback: true);

                    speed.Y += Main.rand.NextFloat(-1f, -2f); //adds random variation from -1 to 2

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y, speed.X, speed.Y, ModContent.ProjectileType<Projectiles.Enemy.EnemyMoonfuryBomb>(), redKnightsSpearDamage, 0f, Main.myPlayer);
                    }
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item1 with { Volume = 1f, Pitch = -0.5f }, NPC.Center);

                    // Reset targetPosition 
                    targetPosition = Vector2.Zero;

                    NPC.ai[1] = 0f;

                    TryQueueComboFollowup(globalNPC, BombComboMoveKey);
                }
                if (NPC.ai[1] == 925f && !hasPlayerLOS)
                {
                    globalNPC.EndCombatTempoSequenceWithoutFollowup(NPC);
                    // STUCK-BOMB FIX. Same class of bug as the spear/homing LOS-abandon branches
                    // above: this used to leave ai[1] sitting at 925 with nothing to reset it, and
                    // the bomb draw condition (`NPC.ai[1] >= 865`) has no upper bound, so the knight
                    // kept climbing ai[1] forever while still visibly holding the bomb. The only thing
                    // that ever rescued it was an unrelated teleport/dodge/flee event resetting ai[1]
                    // to 60 as a side effect — which is why it "eventually cycled" after several
                    // random seconds instead of never.
                    NPC.ai[1] = 0f;
                    NPC.netUpdate = true;
                }

                #endregion

                #region AI 2 Attacks
                // Black rain barrage. Each drop now carries its own frozen-and-spinning windup
                // (EnemyBlackCursedBreath), so a single call here fires wave 1 immediately and
                // schedules 1-2 more waves internally (ticked below, GravefallWaveCooldown apart) —
                // the separate "release 3-25 ticks later" event this used to need is gone.
                if ((NPC.ai[2] == 72 || NPC.ai[2] == 97 || NPC.ai[2] == 522 || NPC.ai[2] == 547 || NPC.ai[2] == 572 || NPC.ai[2] == 597) && !inActiveAttack && NPC.Distance(player.Center) > 250 && hasPlayerLOS)
                {
                    bool wideWave = NPC.ai[2] == 97 || NPC.ai[2] == 547 || NPC.ai[2] == 597;
                    StartGravefallBarrage(wideWave);
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.5f, Pitch = -0.01f }, NPC.Center);
                    NPC.netUpdate = true;
                }

                // Scheduled follow-up waves for whatever barrage StartGravefallBarrage began above.
                if (gravefallWavesRemaining > 0)
                {
                    gravefallWaveTimer++;
                    if (gravefallWaveTimer >= GravefallWaveCooldown)
                    {
                        gravefallWaveTimer = 0;
                        gravefallWaveIndex++;
                        gravefallWavesRemaining--;
                        FireGravefallWave();
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.5f, Pitch = -0.01f }, NPC.Center);
                    }
                }

                // Breather before reset
                if (NPC.ai[2] >= 1200)
                {
                    NPC.ai[2] = 0;
                }

                // Ultrakill Telegraph: Shrinking Dust Circle
                if (NPC.life <= NPC.lifeMax / 2 && NPC.ai[2] >= 100f && NPC.ai[2] <= 200f)
                {
                    if (NPC.ai[2] == 100f)
                    {
                        globalNPC.ResetCombatTempoSequence(clearRecovery: true);
                    }
                    NPC.knockBackResist = 0f;
                    NPC.ai[1] = -130;
                    if (Main.rand.NextBool(4))
                    {
                        float collapse = MathHelper.Clamp((200f - NPC.ai[2]) / 100f, 0f, 1f);
                        Vector2 offset = Main.rand.NextVector2CircularEdge(240f, 240f) * collapse;
                        Dust mote = Dust.NewDustPerfect(NPC.Center + offset, DustID.BoneTorch, -offset * 0.025f, 120, default, 0.9f);
                        mote.noGravity = true;
                    }
                    Lighting.AddLight(NPC.Center, Color.WhiteSmoke.ToVector3() * 1.2f);
                    NPC.velocity.X *= 0.85f;
                }
                // Ultrakill Telegraph: Flash
                if (NPC.ai[2] == 165f)
                {
                    Vector2 spawnPosition = NPC.position;
                    if (NPC.direction == 1)
                    {
                        spawnPosition.X += NPC.width;
                    }
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), spawnPosition, NPC.velocity, ModContent.ProjectileType<Projectiles.VFX.TelegraphFlash>(), 0, 0, Main.myPlayer, UsefulFunctions.ColorToFloat(Color.OrangeRed));

                    }
                    // Store the player's center
                    if (framesSinceStoredPosition >= 25)
                    {
                        framesSinceStoredPosition = 0;
                        int targetPlayer = NPC.target;
                        if (Main.player[targetPlayer].active && !Main.player[targetPlayer].dead)
                        {
                            storedPlayerPosition = Main.player[targetPlayer].Center;
                        }
                    }

                }
                // Ultrakill Attack
                if (NPC.life <= NPC.lifeMax / 2 && NPC.ai[2] >= 200f && NPC.ai[2] <= 235f)
                {
                    NPC.velocity.X *= 0.25f;

                    // Calculate the direction towards the stored player position.
                    int direction = (storedPlayerPosition.X > NPC.Center.X) ? 1 : -1;

                    // Set targetPosition with an offset of 10f * direction units from the storedPlayerPosition along the X-axis.
                    targetPosition = new Vector2(storedPlayerPosition.X + 10f * direction, storedPlayerPosition.Y);

                    // Death Skulls
                    Vector2 speed = UsefulFunctions.BallisticTrajectory(NPC.Center, targetPosition, 2f, fallback: true);
                    speed += Main.rand.NextVector2Circular(1, 5);//was -12, -16
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y, speed.X, speed.Y, ModContent.ProjectileType<Projectiles.Enemy.EnemySpellSuddenDeathStrike>(), redKnightsGreatDamage, 0f, Main.myPlayer, 1f);
                    }
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.8f, PitchVariance = 1f }, NPC.Center); //Play flame sound

                    // Black Breath
                    Vector2 speed2 = UsefulFunctions.BallisticTrajectory(NPC.Center, targetPosition, 2, fallback: true);
                    speed2 += Main.rand.NextVector2Circular(-5, 5);//was -4, -2, then -12, -16
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y, speed2.X, speed2.Y, ModContent.ProjectileType<Projectiles.Enemy.EnemyBlackCursedBreath>(), redKnightsGreatDamage, 0f, Main.myPlayer, 2f);
                    }
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item69 with { Volume = 0.9f, PitchVariance = 2f }, NPC.Center);
                    NPC.netUpdate = true;
                }
                // After Ultrakill attack completes
                if (NPC.ai[2] == 246f)
                {
                    // Reset the targetPosition 
                    targetPosition = Vector2.Zero;
                }

                #endregion

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
            if (NPC.life <= NPC.lifeMax / 2 && NPC.ai[2] >= 100f && NPC.ai[2] <= 200f)
            {
                float progress = MathHelper.Clamp((NPC.ai[2] - 100f) / 100f, 0f, 1f);
                Projectiles.Enemy.EnemyVFX.DrawBlackKnightDeathSeal(NPC.Center, progress);
                if (NPC.ai[2] >= 165f && storedPlayerPosition != Vector2.Zero)
                {
                    Projectiles.Enemy.EnemyVFX.DrawBlackKnightAimThread(NPC.Center, storedPlayerPosition,
                        MathHelper.Clamp((NPC.ai[2] - 165f) / 35f, 0f, 1f));
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

            // Spear
            if (NPC.ai[1] >= (globalNPC.ActiveAttackBypassesShield ? 90f : 120f) && NPC.ai[1] <= 180f)
            {
                float spriteScale = HeldSpearScale;
                Vector2 spearAim = NPC.ai[1] >= 155f ? UsefulFunctions.Aim(NPC.Center, storedPlayerPosition, 1) : new Vector2(NPC.spriteDirection, 0f);
                float rotation = spearAim.ToRotation() + MathHelper.PiOver2;
                Vector2 handWorld = CurrentThrowSpearWorld() - Main.screenPosition;
                DrawHeldSpear(spriteBatch, handWorld, rotation, drawColor, globalNPC, spriteScale,
                    CurrentThrowSpearFramePixel());
                DrawHandOverlay(spriteBatch, drawColor);
            }
            // Bomb
            if (NPC.ai[1] >= 865)
            {
                Vector2 bombAim = NPC.ai[1] >= 900f ? UsefulFunctions.Aim(NPC.Center, storedPlayerPosition, 1) : new Vector2(NPC.spriteDirection, 0f);
                float rotation = bombAim.ToRotation() + MathHelper.PiOver2;
                Vector2 handWorld = CurrentHandWorld() - Main.screenPosition;
                float fuseProgress = MathHelper.Clamp((NPC.ai[1] - 865f) / 60f, 0f, 1f);
                // The Moonfury shader used to be drawn here and sat visibly offset from the bomb
                // sprite. Replaced with a red fuse spark, which cannot drift out of alignment.
                spriteBatch.Draw(bombTexture, handWorld, new Rectangle(0, 0, bombTexture.Width, bombTexture.Height), drawColor, rotation, BombGripOrigin, NPC.scale, SpriteEffects.None, 0);
                Projectiles.Enemy.EnemyVFX.SpawnBombFuseSparks(handWorld + Main.screenPosition, fuseProgress);
                DrawHandOverlay(spriteBatch, drawColor);
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

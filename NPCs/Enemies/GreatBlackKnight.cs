using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;
using tsorcRevamp.Buffs.Debuffs;
using tsorcRevamp.Items;
using tsorcRevamp.Items.Weapons.Throwing;

namespace tsorcRevamp.NPCs.Enemies
{
    public class GreatBlackKnight : ModNPC, IStaggerable, IFlailAnchor, IDebugAttackLabel
    {
        public int redKnightsSpearDamage = 45;
        public int redMagicDamage = 40;
        public int redKnightsGreatDamage = 50;
        public int redFlailDamage = 55;
        Vector2 storedPlayerPosition = Vector2.Zero;
        public int framesSinceStoredPosition = 0;

        // Ticks spent frozen at a LOS-gated fire tick (spear/homing/bomb) waiting for a clear shot. Without this,
        // losing LOS on the exact fire tick would let the attack timer sail past the window forever with nothing
        // left able to fire it (the old "stuck holding the bomb sprite" bug).
        public int losStuckTimer = 0;

        NPCDespawnHandler despawnHandler;

        #region Attack state machine
        // ── State machine ──────────────────────────────────────────────────────────────────────────────────────
        // Replaces the old flat ai[1]/ai[2] numeric-timeline design (every attack's windup/fire frame was a magic
        // number, and a LOS miss on a fire frame had nowhere to go — see the stuck-bomb fix). Now:
        //   ai[0] = Phase (Neutral/Telegraph/Committed/Recovery)   ai[1] = ticks elapsed in the current phase
        //   ai[2] = the AttackKind currently telegraphing/firing   ai[3] = attacks landed so far this combo
        // All four live in NPC.ai[], so they ride along on the vanilla NPC sync same as before.
        //
        // Neutral picks a combo length + first attack and enters Telegraph. Telegraph counts down to a flash and
        // commits. Committed fires (waiting out LOS with a timeout, same fix as before) then either chains into
        // another Telegraph (if more attacks remain in the combo) or drops into Recovery. Recovery is a pure
        // cooldown — movement/pathing (FighterAI, called unconditionally at the top of AI()) keeps running, it
        // just can't start a new attack — before returning to Neutral.
        private enum Phase { Neutral = 0, Telegraph = 1, Committed = 2, Recovery = 3 }
        private enum AttackKind { Spear = 0, Homing = 1, Bomb = 2, Ultrakill = 3, Flail = 4 }

        /// <summary>
        /// DebugMode above-head readout (see IDebugAttackLabel). Includes the phase because the
        /// telegraph/committed/recovery split is what decides whether a hit staggers or bounces.
        /// </summary>
        public string DebugAttackLabel
        {
            get
            {
                if (NPC.GetGlobalNPC<tsorcRevampGlobalNPC>().StaggerTimer > 0)
                {
                    return "Staggered";
                }
                Phase phase = (Phase)(int)NPC.ai[0];
                if (phase == Phase.Neutral)
                {
                    return "Idle";
                }
                AttackKind attack = (AttackKind)(int)NPC.ai[2];
                return $"{attack} ({phase})";
            }
        }

        private static readonly int[] TelegraphTicksByAttack = { 30, 30, 30, 65, 20 };   // Spear, Homing, Bomb, Ultrakill, Flail
        private static readonly int[] CommitTicksByAttack = { 25, 25, 25, 70, 20 };
        private static readonly int[] BaseWeightByAttack = { 40, 35, 25, 30, 35 };
        private const int UltrakillChannelTicks = 35; // trailing slice of Ultrakill's commit window that actually fires

        // Combo/recovery tuning (see the AskUserQuestion-approved design): rolls 1-3 attacks back to back at full
        // health; the CEILING rises smoothly to 8 as health drops, so low-health fights can chain much longer
        // strings without a table of hand-picked per-health-bracket combo lengths.
        private const int MinComboCeiling = 3;
        private const int MaxComboCeiling = 8;
        private const int BaseRecoveryTicks = 60;
        private const int RecoveryPerExtraAttack = 15;
        private const int LosGiveUpTicks = 120; // ~2s waiting on a clear shot before abandoning the attack

        private int comboLength = 1;          // rolled once per combo, at the Neutral -> Telegraph transition
        private int currentRecoveryTicks = BaseRecoveryTicks;
        #endregion

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
            NPC.damage = 100;
            NPC.defense = 61;
            NPC.lifeMax = 30000;
            NPC.value = 5000;

            if (Main.hardMode)
            {
                NPC.lifeMax = 30000;
                NPC.damage = 100;
                NPC.defense = 61;
                NPC.value = 16000; // life / 1.25
                redKnightsGreatDamage = 50;
                redKnightsSpearDamage = 45;
                redMagicDamage = 40;
                redFlailDamage = 55;
            }
            if (tsorcRevampWorld.SuperHardMode)
            {
                NPC.lifeMax = 30000;
                NPC.defense = 61;
                NPC.damage = 100;
                NPC.value = 16000; // life / 2.5
                redKnightsGreatDamage = 50;
                redKnightsSpearDamage = 45;
                redMagicDamage = 40;
                redFlailDamage = 55;
            }

            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.lavaImmune = true;

            NPC.knockBackResist = 0.25f; // poise flinch dial: × PoiseFlinchFactor(0.4) ≈ 0.14 of full knockback per ordinary hit
            Banner = NPC.type;
            BannerItem = ModContent.ItemType<Banners.BlackKnightBanner>();

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
            // A stagger cancels the in-progress attack via IStaggerable.OnStagger below (bespoke state machine,
            // not the generic ai[1]=60/ai[2]=-100 reset — see PoiseStaggerResetsAI's doc comment).

            // Navigation tuning: high jumps, double jump, and ledge routing
            blackKnightGlobalNPC.MaxJumpPower = 11f;
            blackKnightGlobalNPC.NavSearchRadius = 80;
            blackKnightGlobalNPC.CanUseRopes = true;
            blackKnightGlobalNPC.MaxJumpBoost = 7f;
            blackKnightGlobalNPC.CanDoubleJump = true;
            blackKnightGlobalNPC.DoubleJumpPower = 7f;
        }
        #region Spawn
        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            if (spawnInfo.Player.townNPCs > 1f) return 0f;
            if (Main.hardMode && !spawnInfo.Player.ZoneMeteor && !spawnInfo.Player.ZoneDungeon && !(spawnInfo.Player.ZoneCorrupt || spawnInfo.Player.ZoneCrimson) && spawnInfo.Player.ZoneOverworldHeight && NPC.downedBoss3 && !Main.dayTime && Main.rand.NextBool(250)) return 1;
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
        #endregion

        // IFlailAnchor: the flail projectile's chain draws to (and its head orbits/launches from) the same
        // walk-cycle hand rig the spear/bomb overlays already use.
        public Vector2 GetFlailAnchor() => CurrentHandWorld();

        // IStaggerable: a poise break cancels whatever's telegraphing/firing and drops straight into a short
        // recovery (rather than instantly re-engaging), matching how a stagger reads for every other knight here.
        public void OnStagger(NPC npc)
        {
            npc.ai[0] = (float)Phase.Recovery;
            npc.ai[1] = 0f;
            npc.ai[3] = 0f;
            currentRecoveryTicks = BaseRecoveryTicks;
            losStuckTimer = 0;
        }

        public override void AI()
        {
            tsorcRevampAIs.FighterAI(NPC, 1.5f, 0.05f, enragePercent: 0.5f, enrageTopSpeed: 2.9f, canTeleport: true, canDodgeroll: true);
            Lighting.AddLight(NPC.Center, Color.GhostWhite.ToVector3() * 2f);

            tsorcRevampGlobalNPC globalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            Phase phase = (Phase)(int)NPC.ai[0];

            // Hyper-armor (Committed) / poise-can-break-but-suppress-evasion (Telegraph) — driven directly by
            // phase now instead of a pile of magic ai[1]/ai[2] ranges.
            globalNPC.AttackTelegraphing = phase == Phase.Telegraph;
            globalNPC.AttackCommitted = phase == Phase.Committed;

            // A teleport/dodge/pounce seizing the body cancels a windup (Telegraph) or an idle Recovery, but never
            // a Committed (hyper-armored) attack — mirrors the old inProtectedAttack carve-out.
            if (globalNPC.TeleportCountdown > 0 || globalNPC.TeleportAppearanceTimer > 0 || globalNPC.PursuitState == NPCs.PursuitState.Patrol || globalNPC.Fleeing || globalNPC.DodgeTimer > 0 || globalNPC.PounceTimer > 0 || globalNPC.DirectPounceAfterimageTimer > 0 || globalNPC.DirectPounceRecoveryTimer > 0)
            {
                if (phase != Phase.Committed)
                {
                    NPC.ai[0] = (float)Phase.Neutral;
                    NPC.ai[1] = 0f;
                    NPC.ai[3] = 0f;
                    phase = Phase.Neutral;
                }
            }

            if (Main.netMode == NetmodeID.MultiplayerClient || Main.player[NPC.target].dead)
            {
                return;
            }

            NPC.knockBackResist = globalNPC.BaseKnockBackResist; // restore the SetDefaults value; poise scales it to a light flinch
            bool hasPlayerLOS = Collision.CanHitLine(NPC.Center, 1, 1, player.Center, 1, 1);
            framesSinceStoredPosition++;

            // Ambient sound + free-roam jump/dash flourishes. Suppressed only while Committed (hyper-armored fire),
            // so they don't fight the attack's own velocity control — Telegraph/Recovery/Neutral all allow them,
            // same as "recovery can walk and navigate" just being ordinary FighterAI pathing underneath.
            if (globalNPC.StaggerTimer <= 0 && phase != Phase.Committed)
            {
                RunMovementFlourishes();
            }

            if (globalNPC.StaggerTimer <= 0)
            {
                switch (phase)
                {
                    case Phase.Neutral:
                        StartNewCombo();
                        break;
                    case Phase.Telegraph:
                        RunTelegraph((AttackKind)(int)NPC.ai[2]);
                        break;
                    case Phase.Committed:
                        RunCommitted((AttackKind)(int)NPC.ai[2], hasPlayerLOS);
                        break;
                    case Phase.Recovery:
                        RunRecovery();
                        break;
                }
            }

            RunShadowCrystalStorm(hasPlayerLOS); // passive sub-1/3-health background attack; independent of the FSM above
        }

        #region Movement flourishes (idle jump/dash flavor + ambient sound)
        private void RunMovementFlourishes()
        {
            if (Main.rand.NextBool(1500))
            {
                Terraria.Audio.SoundEngine.PlaySound(new Terraria.Audio.SoundStyle("tsorcRevamp/Sounds/DarkSouls/ominous-creature2") with { Volume = 0.8f }, NPC.Center);
            }
            // Chance to jump forward
            if (NPC.Distance(player.Center) > 250 && NPC.velocity.Y == 0f && Main.rand.NextBool(300))
            {
                NPC.velocity.Y = Main.rand.NextFloat(-4, -8f);
                NPC.TargetClosest(true);
                NPC.velocity.X = NPC.velocity.X + (float)NPC.direction * 2f;
                if ((float)NPC.direction * NPC.velocity.X > 2)
                    NPC.velocity.X = (float)NPC.direction * 2;
                NPC.netUpdate = true;
            }
            // Chance to dash step forward
            if (NPC.Distance(player.Center) > 200 && NPC.velocity.Y == 0f && Main.rand.NextBool(140))
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
        }
        #endregion

        #region Combo control
        /// <summary>
        /// Rolls the combo length for a fresh combo and starts its first attack. The ceiling scales smoothly with
        /// missing health (3 at full HP -> 8 at 0 HP) via a single lerp, so there's no hand-rolled table of
        /// per-health-bracket combo lengths to keep in sync as balance changes.
        /// </summary>
        private void StartNewCombo()
        {
            float hpFrac = NPC.lifeMax > 0 ? NPC.life / (float)NPC.lifeMax : 1f;
            float lowHealthT = MathHelper.Clamp(1f - hpFrac, 0f, 1f);
            int comboCeiling = (int)Math.Round(MathHelper.Lerp(MinComboCeiling, MaxComboCeiling, lowHealthT));
            comboLength = Main.rand.Next(1, comboCeiling + 1); // 1..ceiling inclusive
            NPC.ai[3] = 0f;
            BeginAttack(PickNextAttack(NPC.Distance(player.Center)));
        }

        /// <summary>Weighted pick among currently-eligible attacks: spear needs range (it's a thrown weapon, not a
        /// melee swing), flail needs to be within chain reach, Ultrakill needs sub-50% health. Called both to start
        /// a combo and to pick each chained attack, so eligibility is re-checked every step (e.g. the player closing
        /// distance mid-combo naturally steers away from Spear and toward Flail on the next link).</summary>
        private AttackKind PickNextAttack(float distanceToPlayer)
        {
            bool spearEligible = distanceToPlayer >= 120f;
            bool flailEligible = distanceToPlayer <= 260f; // the chain has real reach, but it's still a melee weapon
            bool ultrakillEligible = NPC.life <= NPC.lifeMax / 2;

            int[] weights = new int[BaseWeightByAttack.Length];
            weights[(int)AttackKind.Homing] = BaseWeightByAttack[(int)AttackKind.Homing];
            weights[(int)AttackKind.Bomb] = BaseWeightByAttack[(int)AttackKind.Bomb];
            if (spearEligible) weights[(int)AttackKind.Spear] = BaseWeightByAttack[(int)AttackKind.Spear];
            if (flailEligible) weights[(int)AttackKind.Flail] = BaseWeightByAttack[(int)AttackKind.Flail];
            if (ultrakillEligible) weights[(int)AttackKind.Ultrakill] = BaseWeightByAttack[(int)AttackKind.Ultrakill];

            int total = 0;
            for (int i = 0; i < weights.Length; i++) total += weights[i];
            if (total <= 0) return AttackKind.Homing;

            int roll = Main.rand.Next(total);
            int cumulative = 0;
            for (int i = 0; i < weights.Length; i++)
            {
                cumulative += weights[i];
                if (roll < cumulative) return (AttackKind)i;
            }
            return AttackKind.Homing;
        }

        private void BeginAttack(AttackKind kind)
        {
            NPC.ai[2] = (float)kind;
            NPC.ai[0] = (float)Phase.Telegraph;
            NPC.ai[1] = 0f;
            losStuckTimer = 0;
            NPC.netUpdate = true;
        }

        /// <summary>Called when an attack finishes firing. Chains into the next attack if the combo isn't done yet,
        /// otherwise drops into Recovery — base 60 ticks, +15 per additional attack thrown in the combo (a 1-hit
        /// combo recovers in 60, an 8-hit string in 165), during which FighterAI keeps moving/pathing normally but
        /// nothing here starts a new attack.</summary>
        private void EndAttack()
        {
            int attacksCompleted = (int)NPC.ai[3] + 1;
            NPC.ai[3] = attacksCompleted;

            if (attacksCompleted < comboLength)
            {
                BeginAttack(PickNextAttack(NPC.Distance(player.Center)));
            }
            else
            {
                EnterRecovery(BaseRecoveryTicks + RecoveryPerExtraAttack * (comboLength - 1));
            }
        }

        /// <summary>Gave up waiting on a clear shot (LosGiveUpTicks elapsed with no LOS). Abandons the rest of the
        /// combo rather than chaining blind, and takes only the base recovery — it never actually landed a hit.</summary>
        private void EndAttackCancelled()
        {
            losStuckTimer = 0;
            EnterRecovery(BaseRecoveryTicks);
        }

        private void EnterRecovery(int ticks)
        {
            NPC.ai[0] = (float)Phase.Recovery;
            NPC.ai[1] = 0f;
            currentRecoveryTicks = ticks;
            NPC.netUpdate = true;
        }

        private void RunRecovery()
        {
            int t = (int)NPC.ai[1] + 1;
            if (t >= currentRecoveryTicks)
            {
                NPC.ai[0] = (float)Phase.Neutral;
                NPC.ai[1] = 0f;
            }
            else
            {
                NPC.ai[1] = t;
            }
        }
        #endregion

        #region Telegraph
        private void RunTelegraph(AttackKind kind)
        {
            int t = (int)NPC.ai[1];
            int duration = TelegraphTicksByAttack[(int)kind];

            // Offensive pre-jump cue ~10 ticks before every attack's flash (previously only 3 of the 4 attacks had this).
            if (t == Math.Max(0, duration - 10) && NPC.velocity.Y <= 0f && Main.rand.NextBool(4))
            {
                NPC.velocity.Y = Main.rand.NextFloat(-6, -10f);
                NPC.netUpdate = true;
            }

            if (kind == AttackKind.Ultrakill)
            {
                NPC.knockBackResist = 0f;
                float ringRadius = 240f * (duration - t) / duration; // shrinks to 0 right as the flash lands
                UsefulFunctions.DustRing(NPC.Center, ringRadius, DustID.BoneTorch, 48, 4);
                Lighting.AddLight(NPC.Center * 2, Color.WhiteSmoke.ToVector3() * 5);
                NPC.velocity.X *= 0.85f;
            }

            // Spear/Bomb/Ultrakill all aim at a ~25-tick-old predicted player position rather than tracking live;
            // Homing and Flail aim live in their own commit steps, so they're excluded here.
            if (kind != AttackKind.Homing && kind != AttackKind.Flail && framesSinceStoredPosition >= 25)
            {
                framesSinceStoredPosition = 0;
                if (player.active && !player.dead) storedPlayerPosition = player.Center;
            }

            if (t >= duration - 1)
            {
                SpawnTelegraphFlash(kind);
                NPC.ai[0] = (float)Phase.Committed;
                NPC.ai[1] = 0f;
                NPC.netUpdate = true;
                return;
            }
            NPC.ai[1] = t + 1;
        }

        private void SpawnTelegraphFlash(AttackKind kind)
        {
            Vector2 spawnPosition = NPC.position;
            if (NPC.direction == 1)
            {
                spawnPosition.X += NPC.width;
            }
            Color color = kind == AttackKind.Homing ? Color.Orange : Color.OrangeRed;
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), spawnPosition, NPC.velocity, ModContent.ProjectileType<Projectiles.VFX.TelegraphFlash>(), 0, 0, Main.myPlayer, UsefulFunctions.ColorToFloat(color));
                if (kind == AttackKind.Bomb)
                {
                    Lighting.AddLight(NPC.Center, Color.OrangeRed.ToVector3() * 3f);
                }
            }
        }
        #endregion

        #region Committed (fire)
        private void RunCommitted(AttackKind kind, bool hasPlayerLOS)
        {
            int t = (int)NPC.ai[1];
            int duration = CommitTicksByAttack[(int)kind];
            float distanceToPlayer = NPC.Distance(player.Center);

            switch (kind)
            {
                case AttackKind.Spear:
                    RunSpearCommit(t, duration, hasPlayerLOS, distanceToPlayer);
                    break;
                case AttackKind.Homing:
                    RunHomingCommit(t, duration, hasPlayerLOS);
                    break;
                case AttackKind.Bomb:
                    RunBombCommit(t, duration, hasPlayerLOS, distanceToPlayer);
                    break;
                case AttackKind.Ultrakill:
                    RunUltrakillCommit(t, duration);
                    break;
                case AttackKind.Flail:
                    RunFlailCommit(t, duration, hasPlayerLOS);
                    break;
            }
        }

        /// <summary>Holds at the final tick waiting for LOS instead of firing blind through a wall, retrying every
        /// tick until it clears or LosGiveUpTicks elapses (then the whole combo is abandoned). This is the fix for
        /// the enemy getting stuck holding an attack sprite forever: the old code fired on a single absolute frame
        /// with no retry, so a LOS miss on that exact frame meant the attack (and every attack after it) never fired again.</summary>
        private bool WaitOnLos(bool hasPlayerLOS)
        {
            if (hasPlayerLOS)
            {
                losStuckTimer = 0;
                return false;
            }
            losStuckTimer++;
            if (losStuckTimer > LosGiveUpTicks)
            {
                EndAttackCancelled();
            }
            return true;
        }

        private void RunSpearCommit(int t, int duration, bool hasPlayerLOS, float distanceToPlayer)
        {
            NPC.knockBackResist = 0f;
            if (t < duration - 1)
            {
                NPC.ai[1] = t + 1;
                return;
            }
            if (WaitOnLos(hasPlayerLOS)) return;

            NPC.TargetClosest(true);
            int direction = (storedPlayerPosition.X > NPC.Center.X) ? 1 : -1;
            Vector2 targetPosition = new Vector2(storedPlayerPosition.X + 10f * direction, storedPlayerPosition.Y);

            bool far = distanceToPlayer > 400;
            float speed = far ? Main.rand.NextFloat(16, 18f) : Main.rand.NextFloat(12, 14f);
            Vector2 vel = UsefulFunctions.BallisticTrajectory(NPC.Center, targetPosition, speed, fallback: true) + player.velocity;
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y, vel.X, vel.Y, ModContent.ProjectileType<Projectiles.Enemy.BlackThrowingSpear>(), redKnightsSpearDamage, 0f, Main.myPlayer);
            }
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.8f, PitchVariance = 0.1f }, NPC.Center);
            EndAttack();
        }

        private void RunHomingCommit(int t, int duration, bool hasPlayerLOS)
        {
            if (t < duration - 1)
            {
                NPC.ai[1] = t + 1;
                return;
            }
            if (WaitOnLos(hasPlayerLOS)) return;

            NPC.TargetClosest(true);
            float speed = 15f;
            Vector2 vel = UsefulFunctions.BallisticTrajectory(NPC.Center, player.Center, speed, 2.1f, highAngle: true, fallback: true) + player.velocity;
            if (((vel.X < 0f) && (NPC.direction < 0)) || ((vel.X > 0f) && (NPC.direction > 0)))
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y, vel.X, vel.Y, ModContent.ProjectileType<Projectiles.Enemy.EnemyBlackKnightHomingCrystal>(), redMagicDamage, 0f, Main.myPlayer);
                }
            }
            EndAttack();
        }

        private void RunBombCommit(int t, int duration, bool hasPlayerLOS, float distanceToPlayer)
        {
            NPC.knockBackResist = 0f;
            if (t < duration - 1)
            {
                NPC.ai[1] = t + 1;
                return;
            }
            if (WaitOnLos(hasPlayerLOS)) return;

            int direction = (storedPlayerPosition.X > NPC.Center.X) ? 1 : -1;
            Vector2 targetPosition = new Vector2(storedPlayerPosition.X + 10f * direction, storedPlayerPosition.Y);

            bool far = distanceToPlayer > 400;
            float speed = far ? 8f : 5f;
            Vector2 vel = UsefulFunctions.BallisticTrajectory(NPC.Center, targetPosition, speed, fallback: true);
            if (far)
            {
                vel += player.velocity;
            }
            else
            {
                vel.Y += Main.rand.NextFloat(-1f, -2f);
            }
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y, vel.X, vel.Y, ModContent.ProjectileType<Projectiles.Enemy.EnemyMoonfuryBomb>(), redKnightsSpearDamage, 0f, Main.myPlayer);
            }
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item1 with { Volume = 1f, Pitch = -0.5f }, NPC.Center);
            EndAttack();
        }

        /// <summary>Ball-and-chain flail (see Projectiles.Enemy.Weapons.EnemyFlailProjectileBase) — anchored to
        /// this NPC's rigged hand via IFlailAnchor, so no arm-swing animation is needed: the chain+head projectile
        /// owns the entire visual. Aims live (no predicted position) and occasionally spins in place instead of
        /// launching, for a little variety within a combo that rolls Flail more than once in a row.</summary>
        private void RunFlailCommit(int t, int duration, bool hasPlayerLOS)
        {
            if (t < duration - 1)
            {
                NPC.ai[1] = t + 1;
                return;
            }
            if (WaitOnLos(hasPlayerLOS)) return;

            NPC.TargetClosest(true);
            Vector2 anchor = CurrentHandWorld();
            Vector2 toPlayer = player.Center - anchor;
            if (toPlayer == Vector2.Zero)
            {
                toPlayer = new Vector2(NPC.direction, 0f);
            }
            toPlayer.Normalize();

            bool spin = Main.rand.NextBool(3); // occasional windmill instead of a straight throw
            Vector2 velocity = spin ? Vector2.Zero : toPlayer * 11f;

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                Projectile.NewProjectile(NPC.GetSource_FromThis(), anchor, velocity, ModContent.ProjectileType<Projectiles.Enemy.Weapons.GreatBlackKnightFlail>(), redFlailDamage, 3f, Main.myPlayer, NPC.whoAmI, spin ? 1f : 0f);
            }
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.7f, PitchVariance = 0.2f }, NPC.Center);
            EndAttack();
        }

        /// <summary>Channel attack: no LOS gate (matches the original), fires every tick through the trailing
        /// UltrakillChannelTicks of the commit window instead of a single shot at the end.</summary>
        private void RunUltrakillCommit(int t, int duration)
        {
            if (t >= duration - UltrakillChannelTicks)
            {
                NPC.velocity.X *= 0.25f;

                int direction = (storedPlayerPosition.X > NPC.Center.X) ? 1 : -1;
                Vector2 targetPosition = new Vector2(storedPlayerPosition.X + 10f * direction, storedPlayerPosition.Y);

                // Death Skulls
                Vector2 speed = UsefulFunctions.BallisticTrajectory(NPC.Center, targetPosition, 2f, fallback: true) + Main.rand.NextVector2Circular(1, 5);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y, speed.X, speed.Y, ModContent.ProjectileType<Projectiles.Enemy.EnemySpellSuddenDeathStrike>(), redKnightsGreatDamage, 0f, Main.myPlayer);
                }
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.8f, PitchVariance = 1f }, NPC.Center);

                // Black Breath
                Vector2 speed2 = UsefulFunctions.BallisticTrajectory(NPC.Center, targetPosition, 2f, fallback: true) + Main.rand.NextVector2Circular(-5, 5);
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center.X, NPC.Center.Y, speed2.X, speed2.Y, ModContent.ProjectileType<Projectiles.Enemy.EnemyBlackCursedBreath>(), redKnightsGreatDamage, 0f, Main.myPlayer);
                }
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item69 with { Volume = 0.9f, PitchVariance = 2f }, NPC.Center);
                NPC.netUpdate = true;
            }

            if (t >= duration - 1)
            {
                EndAttack();
                return;
            }
            NPC.ai[1] = t + 1;
        }
        #endregion

        #region Shadow Crystal Storm (passive sub-1/3-health background attack, independent of the combo FSM)
        private void RunShadowCrystalStorm(bool hasPlayerLOS)
        {
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
        #endregion

        #region Debuffs
        public override void OnHitPlayer(Player target, Player.HurtInfo hurtInfo)
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
        static readonly Vector2 SpearGripOrigin = new Vector2(8f, 38f);
        static readonly Vector2 BombGripOrigin = new Vector2(14f, 4f);

        Vector2 CurrentHandWorld()
        {
            int frame = NPC.frame.Height > 0 ? NPC.frame.Y / NPC.frame.Height : 0;
            if (frame < 0 || frame >= HandPixel.Length)
            {
                frame = 0;
            }

            Vector2 fp = HandPixel[frame];
            float x = NPC.Center.X + (fp.X - FrameW / 2f) * NPC.scale * -NPC.spriteDirection;
            float y = NPC.Center.Y + 24f + NPC.gfxOffY + (fp.Y - FrameH) * NPC.scale;
            return new Vector2(x, y);
        }

        Vector2 CurrentSpearWorld()
        {
            Vector2 handWorld = CurrentHandWorld();
            int frame = NPC.frame.Height > 0 ? NPC.frame.Y / NPC.frame.Height : 0;
            if (frame == 0)
            {
                handWorld.Y -= 21f;
            }
            return handWorld;
        }

        void DrawHandOverlay(SpriteBatch spriteBatch, Color drawColor)
        {
            if (handTexture == null)
            {
                return;
            }

            SpriteEffects effects = NPC.spriteDirection < 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
            Rectangle sourceRectangle = new Rectangle(0, NPC.frame.Y, (int)FrameW, (int)FrameH);
            Vector2 drawPosition = NPC.Center + new Vector2(0f, 24f + NPC.gfxOffY) - Main.screenPosition;
            spriteBatch.Draw(handTexture, drawPosition, sourceRectangle, drawColor, NPC.rotation, new Vector2(FrameW / 2f, FrameH), NPC.scale, effects, 0f);
        }

        void DrawGreatBlackKnightMagicOverlays(Phase phase, AttackKind currentAttack)
        {
            bool active = phase == Phase.Telegraph || phase == Phase.Committed;
            if (active)
            {
                int duration = phase == Phase.Telegraph
                    ? TelegraphTicksByAttack[(int)currentAttack]
                    : CommitTicksByAttack[(int)currentAttack];
                float progress = MathHelper.Clamp(NPC.ai[1] / Math.Max(1f, duration - 1f), 0f, 1f);
                Vector2 hand = CurrentHandWorld();

                if (currentAttack == AttackKind.Homing)
                {
                    Projectiles.Enemy.EnemyVFX.DrawBlackKnightHexCrystal(hand, Vector2.Zero, progress, phase == Phase.Committed);
                }
                else if (currentAttack == AttackKind.Ultrakill)
                {
                    Projectiles.Enemy.EnemyVFX.DrawBlackKnightDeathSeal(NPC.Center,
                        phase == Phase.Telegraph ? progress : 1f);
                    if (phase == Phase.Committed && storedPlayerPosition != Vector2.Zero)
                    {
                        Projectiles.Enemy.EnemyVFX.DrawBlackKnightAimThread(NPC.Center, storedPlayerPosition, progress);
                    }
                }
                else if (currentAttack == AttackKind.Flail)
                {
                    Projectiles.Enemy.EnemyVFX.DrawGreatBlackKnightFlail(hand, Vector2.Zero, phase == Phase.Committed);
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

            Phase phase = (Phase)(int)NPC.ai[0];
            AttackKind currentAttack = (AttackKind)(int)NPC.ai[2];
            bool active = phase == Phase.Telegraph || phase == Phase.Committed;

            // Spear
            if (active && currentAttack == AttackKind.Spear)
            {
                float spriteScale = 0.8f;
                Vector2 spearAim = phase == Phase.Committed ? UsefulFunctions.Aim(NPC.Center, storedPlayerPosition, 1) : new Vector2(NPC.spriteDirection, 0f);
                float rotation = spearAim.ToRotation() + MathHelper.PiOver2;
                Vector2 handWorld = CurrentSpearWorld() - Main.screenPosition;
                if (phase == Phase.Committed)
                {
                    Vector2 forward = spearAim.SafeNormalize(new Vector2(NPC.spriteDirection, 0f));
                    Projectiles.Enemy.EnemyVFX.DrawBlackKnightSpearWake(
                        handWorld + Main.screenPosition + forward * 34f,
                        forward.ToRotation(), new Vector2(74f, 16f), 0.52f);
                }
                spriteBatch.Draw(spearTexture, handWorld, new Rectangle(0, 0, spearTexture.Width, spearTexture.Height), drawColor, rotation, SpearGripOrigin, NPC.scale * spriteScale, SpriteEffects.None, 0);
                DrawHandOverlay(spriteBatch, drawColor);
            }
            // Bomb
            if (active && currentAttack == AttackKind.Bomb)
            {
                Vector2 bombAim = phase == Phase.Committed ? UsefulFunctions.Aim(NPC.Center, storedPlayerPosition, 1) : new Vector2(NPC.spriteDirection, 0f);
                float rotation = bombAim.ToRotation() + MathHelper.PiOver2;
                Vector2 handWorld = CurrentHandWorld() - Main.screenPosition;
                float fuseProgress = phase == Phase.Telegraph
                    ? MathHelper.Clamp(NPC.ai[1] / TelegraphTicksByAttack[(int)AttackKind.Bomb], 0f, 1f)
                    : 1f;
                Projectiles.Enemy.EnemyVFX.DrawBlackKnightMoonfury(
                    handWorld + Main.screenPosition, Vector2.Zero, fuseProgress, phase == Phase.Committed);
                spriteBatch.Draw(bombTexture, handWorld, new Rectangle(0, 0, bombTexture.Width, bombTexture.Height), drawColor, rotation, BombGripOrigin, NPC.scale, SpriteEffects.None, 0);
                DrawHandOverlay(spriteBatch, drawColor);
            }

            DrawGreatBlackKnightMagicOverlays(phase, currentAttack);
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

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.IO;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;
using tsorcRevamp.Buffs.Debuffs;
using tsorcRevamp.Items;
using tsorcRevamp.Items.Weapons.Throwing;
using tsorcRevamp.Projectiles;

namespace tsorcRevamp.NPCs.Enemies
{
    public class GreatBlackKnight : ModNPC, IStaggerable, IFlailAnchor, IDebugAttackLabel, IHumanoidMeleeHitEffects, Projectiles.Enemy.Weapons.ISpearMeleeWielder
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
        private enum AttackKind { Spear = 0, Homing = 1, Bomb = 2, Ultrakill = 3, Flail = 4, SpearMelee = 5 }

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

        // All three are indexed by AttackKind and MUST stay the same length as the enum.
        private static readonly int[] TelegraphTicksByAttack = { 30, 30, 30, 65, 20, 40 };   // Spear, Homing, Bomb, Ultrakill, Flail, SpearMelee
        private static readonly int[] CommitTicksByAttack = { 25, 25, 25, 70, 20, 26 };
        // Flail is this knight's signature weapon and was barely showing up: it needed melee range
        // AND had to win a roll against four other attacks. Now it is the heaviest weight in the
        // table and eligible out to 30 tiles, so it reads as the thing he actually fights with.
        private static readonly int[] BaseWeightByAttack = { 30, 25, 20, 30, 110, 40 };

        // Spear jab. The 40-tick telegraph sits inside the 30-60 tick band that matches the player's
        // dodge-roll window, so the windup is something to react to rather than a surprise.
        private const int SpearMeleeStrikeTick = 6;     // commit tick the hitbox arms on
        private const int SpearMeleeHitTicks = 10;      // how long it stays live
        private const float SpearMeleeExtension = 26f;  // how far the shaft slides forward on the thrust
        private const float SpearMeleeWindup = 7f;      // pull-back during the telegraph
        private const float SpearMeleeReach = 92f;      // hitbox width; the visual thrust must not outrun it
        private const float SpearMeleeHeight = 40f;
        // Selection range for the jab. This was 110f — sized so the jab exactly filled the sub-120f gap where
        // the spear THROW is ineligible, on the assumption that the knight could only stab what was already
        // next to it. Now that the windup runs and jumps the knight into range (RunSpearMeleeApproach), it can
        // be picked from a genuine distance and close the gap itself, so the band deliberately overlaps the
        // throw's and the two compete on weight rather than on range alone.
        private const float SpearMeleeRange = 220f;
        // 30 tiles. Must stay in step with GreatBlackKnightFlail's OutwardTicks * the launch speed
        // in RunFlailCommit, or he throws the flail at players it cannot physically reach.
        private const float FlailReach = 480f;
        private const float FlailLaunchSpeed = 16f;
        private const int UltrakillChannelTicks = 35; // trailing slice of Ultrakill's commit window that actually fires

        // Combo/recovery tuning (see the AskUserQuestion-approved design): rolls 1-3 attacks back to back at full
        // health; the CEILING rises smoothly to 8 as health drops, so low-health fights can chain much longer
        // strings without a table of hand-picked per-health-bracket combo lengths.
        private const int MinComboCeiling = 3;
        private const int MaxComboCeiling = 8;
        private const int BaseRecoveryTicks = 60;
        private const int RecoveryPerExtraAttack = 15;
        private const int LosGiveUpTicks = 120; // ~2s waiting on a clear shot before abandoning the attack
        // Safety cap on AttackGeometryStillActive (see below). GreatBlackKnightFlail.Lifetime is 110,
        // so a healthy flail always clears well inside this; the cap only exists so a flail that somehow
        // fails to despawn can never soft-lock the knight out of attacking for the rest of the fight.
        private const int GeometryWaitCapTicks = 180;

        private int comboLength = 1;          // rolled once per combo, at the Neutral -> Telegraph transition
        private int currentRecoveryTicks = BaseRecoveryTicks;
        private int geometryWaitTimer;        // ticks spent blocked by still-live geometry from a finished attack
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
            // Hidden window = this + SmokeFireTeleportSnapTicks (30), so 30 here means he is invisible for
            // 60 ticks and then steps out of a cloud that runs for PlagueTeleportCloud.LifetimeTicks (180) —
            // i.e. he reappears with ~2s of cloud still billowing around him and is immediately free to
            // attack again, instead of the old 170-tick blackout that outlasted the whole effect.
            blackKnightGlobalNPC.TeleportTelegraphTime = 30;
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

        // storedPlayerPosition is the ~25-tick-old predicted aim point, and it is NOT part of the vanilla
        // ai[] sync. Without this it stayed Vector2.Zero forever on multiplayer clients (only the server ever
        // assigns it, in RunTelegraph), so every consumer that runs client-side aimed at the world origin:
        // the held spear and bomb rotate toward it in PostDraw, FaceAttackAim turns the body to match, and
        // DrawBlackKnightAimThread draws the Ultrakill tell along it. Net effect on a client was a knight
        // pointing its weapon at the top-left corner of the map through every aimed attack.
        // Matches BlackKnight, which already syncs the same field.
        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(storedPlayerPosition.X);
            writer.Write(storedPlayerPosition.Y);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            storedPlayerPosition = new Vector2(reader.ReadSingle(), reader.ReadSingle());
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

            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            // STUCK-BOMB FIX. This used to be `|| Main.player[NPC.target].dead` on the return above,
            // which froze the ENTIRE state machine: ai[0]/ai[1] stopped advancing, so a knight that
            // was mid-Bomb stayed in Committed forever, holding the bomb sprite in its hand and
            // never firing or moving on. A stale NPC.target (e.g. the 255 sentinel, whose dummy
            // player reads as dead) made that permanent rather than lasting until a respawn.
            // Now: re-acquire first, and if there is genuinely no target, abandon the attack into
            // Recovery instead of holding the pose.
            if (NPC.target < 0 || NPC.target >= Main.maxPlayers || !player.active || player.dead)
            {
                NPC.TargetClosest(true);
            }
            if (!player.active || player.dead)
            {
                if (phase != Phase.Neutral)
                {
                    NPC.ai[0] = (float)Phase.Recovery;
                    NPC.ai[1] = 0f;
                    NPC.ai[3] = 0f;
                    currentRecoveryTicks = BaseRecoveryTicks;
                    losStuckTimer = 0;
                    NPC.netUpdate = true;
                }
                return;
            }

            NPC.knockBackResist = globalNPC.BaseKnockBackResist; // restore the SetDefaults value; poise scales it to a light flinch
            bool hasPlayerLOS = Collision.CanHitLine(NPC.Center, 1, 1, player.Center, 1, 1);
            framesSinceStoredPosition++;

            // Ambient sound + free-roam jump/dash flourishes. Suppressed while Committed (hyper-armored fire)
            // so they don't fight the attack's own velocity control, and suppressed while a teleport is in
            // flight — this was missing, and it's why the knight could visibly vibrate a few pixels mid-
            // teleport: FighterAI (above) zeroes velocity to hold the NPC still during the hold, but this ran
            // AFTER that every tick with no idea a teleport was happening, and its random jump/dash burst
            // (velocity.Y up to -8, a real position kick) directly overwrote the hold. Telegraph/Recovery/
            // Neutral otherwise still allow it, same as "recovery can walk and navigate" being ordinary
            // FighterAI pathing underneath.
            bool teleportHold = globalNPC.TeleportCountdown > 0 || globalNPC.TeleportAppearanceTimer > 0;
            if (globalNPC.StaggerTimer <= 0 && phase != Phase.Committed && !teleportHold)
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
        /// True while an attack the knight ALREADY finished still has damaging geometry physically out in the
        /// world. An attack's logical duration (its Committed window) and its physical duration are not the same
        /// thing: Flail commits for only 20 ticks and throws on the last one, but GreatBlackKnightFlail lives for
        /// 110 — so without this gate the FSM chained straight into the next link and ran a whole Bomb or Spear
        /// (telegraph + commit) while the ball-and-chain was still swinging. Blocking on it here keeps one attack
        /// on screen at a time. Deliberately NOT cached per tick: RunFlailCommit spawns the flail and calls
        /// EndAttack in the same tick, and EndAttack must observe the flail it just threw.
        /// </summary>
        private bool AttackGeometryStillActive => HasActiveFlail();

        /// <summary>
        /// Rolls the combo length for a fresh combo and starts its first attack. The ceiling scales smoothly with
        /// missing health (3 at full HP -> 8 at 0 HP) via a single lerp, so there's no hand-rolled table of
        /// per-health-bracket combo lengths to keep in sync as balance changes.
        /// </summary>
        private void StartNewCombo()
        {
            // Hold in Neutral (still walking/pathing normally under FighterAI — Neutral is not a seizing phase)
            // until the previous combo's geometry clears, rather than opening a new combo on top of it.
            if (WaitingOnAttackGeometry())
            {
                return;
            }

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
            // 30 tiles — the chain genuinely reaches that far now. !HasActiveFlail() enforces one
            // ball-and-chain out at a time: with 30-tile reach and a fast retract, back-to-back
            // throws otherwise read as the knight juggling two flails at once.
            bool flailEligible = distanceToPlayer <= FlailReach && !HasActiveFlail();
            bool ultrakillEligible = NPC.life <= NPC.lifeMax / 2;
            // Complements the throw rather than competing with it: the jab covers exactly the band
            // where the spear throw is ineligible, so closing the distance steers the knight into
            // melee instead of leaving it with nothing to do with the weapon it is holding.
            bool spearMeleeEligible = distanceToPlayer <= SpearMeleeRange;

            int[] weights = new int[BaseWeightByAttack.Length];
            weights[(int)AttackKind.Homing] = BaseWeightByAttack[(int)AttackKind.Homing];
            weights[(int)AttackKind.Bomb] = BaseWeightByAttack[(int)AttackKind.Bomb];
            if (spearEligible) weights[(int)AttackKind.Spear] = BaseWeightByAttack[(int)AttackKind.Spear];
            if (flailEligible) weights[(int)AttackKind.Flail] = BaseWeightByAttack[(int)AttackKind.Flail];
            if (ultrakillEligible) weights[(int)AttackKind.Ultrakill] = BaseWeightByAttack[(int)AttackKind.Ultrakill];
            if (spearMeleeEligible) weights[(int)AttackKind.SpearMelee] = BaseWeightByAttack[(int)AttackKind.SpearMelee];

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

            // Geometry from the attack that just ended (currently only the flail) is still live: end the combo
            // here instead of chaining. Recovery ticks down normally and StartNewCombo re-checks the gate, so
            // the knight resumes as soon as the chain retracts rather than stacking a second attack on top.
            if (attacksCompleted < comboLength && !AttackGeometryStillActive)
            {
                BeginAttack(PickNextAttack(NPC.Distance(player.Center)));
            }
            else
            {
                EnterRecovery(BaseRecoveryTicks + RecoveryPerExtraAttack * (comboLength - 1));
            }
        }

        /// <summary>
        /// Ticks the block on still-live attack geometry and reports whether the knight must keep waiting.
        /// Returns false (proceed anyway) once GeometryWaitCapTicks is exceeded, so a projectile that fails to
        /// despawn degrades into the old overlapping behaviour instead of silently ending the fight.
        /// </summary>
        private bool WaitingOnAttackGeometry()
        {
            if (!AttackGeometryStillActive)
            {
                geometryWaitTimer = 0;
                return false;
            }
            if (++geometryWaitTimer >= GeometryWaitCapTicks)
            {
                geometryWaitTimer = 0;
                return false;
            }
            return true;
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

            // The jab drives its own approach (see RunSpearMeleeApproach) rather than waiting on FighterAI's
            // slow pursuit — this is what stops it reading as "he halts, winds up, then stabs at nothing".
            if (kind == AttackKind.SpearMelee)
            {
                RunSpearMeleeApproach(t, duration);
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
                if (player.active && !player.dead)
                {
                    storedPlayerPosition = player.Center;
                    // SendExtraAI only rides along with a netUpdate, and this field is what every aimed
                    // attack is drawn against on clients. Cheap: telegraphs are 20-65 ticks, so this is at
                    // most a couple of updates per attack.
                    NPC.netUpdate = true;
                }
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
                case AttackKind.SpearMelee:
                    RunSpearMeleeCommit(t, duration);
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

        /// <summary>True while the spear jab is committed — the hitbox stays alive for exactly this window.</summary>
        public bool SpearMeleeActive =>
            (Phase)(int)NPC.ai[0] == Phase.Committed && (AttackKind)(int)NPC.ai[2] == AttackKind.SpearMelee;

        /// <summary>True only during the strike itself, so the hitbox cannot damage on the wind-up or the recovery.</summary>
        public bool SpearMeleeHitWindow =>
            SpearMeleeActive && NPC.ai[1] >= SpearMeleeStrikeTick && NPC.ai[1] < SpearMeleeStrikeTick + SpearMeleeHitTicks;

        /// <summary>
        /// How far along its own axis the shaft sits: pulled back through the telegraph, driven out
        /// fast, held, then withdrawn. Shared by the draw so the visual thrust and the live hitbox
        /// window describe the same motion.
        /// </summary>
        private float SpearMeleeGripSlide(Phase phase, int t)
        {
            if (phase == Phase.Telegraph)
            {
                float windupDuration = Math.Max(1, TelegraphTicksByAttack[(int)AttackKind.SpearMelee] - 1);
                return -SpearMeleeWindup * MathHelper.Clamp(t / windupDuration, 0f, 1f);
            }

            float commitDuration = Math.Max(1, CommitTicksByAttack[(int)AttackKind.SpearMelee]);
            float p = MathHelper.Clamp(t / commitDuration, 0f, 1f);
            if (p < 0.30f)
            {
                return MathHelper.Lerp(-SpearMeleeWindup, SpearMeleeExtension, p / 0.30f);
            }
            if (p < 0.62f)
            {
                return SpearMeleeExtension;
            }
            return MathHelper.Lerp(SpearMeleeExtension, 0f, (p - 0.62f) / 0.38f);
        }

        /// <summary>
        /// The jab. Unlike the ranged attacks this does NOT wait on line of sight: it is only ever
        /// selected inside SpearMeleeRange, and freezing mid-thrust for up to two seconds waiting for
        /// a clear shot would read as a bug rather than as a held attack.
        /// </summary>
        private void RunSpearMeleeCommit(int t, int duration)
        {
            NPC.knockBackResist = 0f;

            if (t == SpearMeleeStrikeTick && Main.netMode != NetmodeID.MultiplayerClient)
            {
                int direction = Math.Sign(player.Center.X - NPC.Center.X);
                if (direction == 0)
                {
                    direction = NPC.spriteDirection;
                }
                int projectileIndex = Projectile.NewProjectile(
                    NPC.GetSource_FromThis(), NPC.Center, new Vector2(direction, 0f),
                    ModContent.ProjectileType<Projectiles.Enemy.Weapons.GreatBlackKnightSpearHitbox>(),
                    redKnightsGreatDamage, 4f, Main.myPlayer, SpearMeleeReach, SpearMeleeHeight);
                tsorcGlobalProjectile.SetDefenseTraits(projectileIndex,
                    NPC.GetGlobalNPC<tsorcRevampGlobalNPC>().ActiveAttackDefenseTraits);
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.9f, PitchVariance = 0.1f }, NPC.Center);

                // Commit the body into the thrust. Without this the jab fired from a standing stop the moment
                // FighterAI's pursuit happened to stall, which is what made it read as "he stops to attack".
                NPC.velocity.X = direction * SpearMeleeLungeSpeed;
                NPC.netUpdate = true;
            }

            if (t >= duration - 1)
            {
                EndAttack();
                return;
            }
            NPC.ai[1] = t + 1;
        }

        #region Position-aware melee approach (ported from GreatRedKnight)
        // FighterAI runs this knight at a 1.5 top speed with 0.05 acceleration — a slow walk that cannot
        // close on a player who is backing away during a 40-tick windup, so the jab used to whiff or simply
        // never become eligible. GreatRedKnight's melee attacks solve this by DRIVING the body themselves for
        // the duration (RedKnightAttackController.TickCrimsonAdvance / ApproachHorizontalSpeed) instead of
        // deferring to the shared mover. These are the equivalent levers for the spear jab.
        //
        // Ordering makes this work: AI() calls FighterAI FIRST and then runs this state machine, so anything
        // written here lands on top of the mover's velocity for the same tick.
        private const float SpearMeleeApproachSpeed = 4.2f;    // run-down speed by the end of the windup
        private const float SpearMeleeApproachAccel = 0.22f;
        private const float SpearMeleeLungeSpeed = 6.5f;       // forward burst on the strike tick
        private const float SpearMeleeJumpPower = -7.5f;
        private const float SpearMeleeJumpTriggerHeight = 48f; // player at least this far above -> hop

        /// <summary>
        /// Accelerates toward a target horizontal speed without ever snapping to it, so the approach reads as
        /// the knight breaking into a run rather than jumping to speed. Mirrors
        /// RedKnightAttackController.ApproachHorizontalSpeed.
        /// </summary>
        private void ApproachHorizontalSpeed(int direction, float speed, float acceleration)
        {
            float target = direction * speed;
            if (NPC.velocity.X < target)
            {
                NPC.velocity.X = Math.Min(NPC.velocity.X + acceleration, target);
            }
            else if (NPC.velocity.X > target)
            {
                NPC.velocity.X = Math.Max(NPC.velocity.X - acceleration, target);
            }
        }

        /// <summary>
        /// Runs the knight at the player through the jab's windup, hopping over ledges and up to a player
        /// standing above it. Called every tick of the SpearMelee telegraph.
        /// </summary>
        private void RunSpearMeleeApproach(int t, int duration)
        {
            if (!player.active || player.dead)
            {
                return;
            }

            int toPlayer = Math.Sign(player.Center.X - NPC.Center.X);
            if (toPlayer == 0)
            {
                toPlayer = NPC.direction;
            }

            // Ramp rather than a flat sprint: the windup still opens at a walking pace, so the tell reads as
            // "he starts running at you" instead of an instant speed change.
            float progress = duration > 1 ? MathHelper.Clamp(t / (float)(duration - 1), 0f, 1f) : 1f;
            ApproachHorizontalSpeed(toPlayer, MathHelper.Lerp(1.5f, SpearMeleeApproachSpeed, progress),
                SpearMeleeApproachAccel);

            // "Run AND jump to reach": hop for a player standing above, or when a step/ledge has stalled the
            // run outright. Grounded-only, so this can never turn into a hover.
            if (NPC.velocity.Y == 0f)
            {
                bool playerAbove = player.Center.Y < NPC.Center.Y - SpearMeleeJumpTriggerHeight;
                bool runStalled = NPC.collideX && Math.Abs(NPC.velocity.X) < 0.35f;
                if (playerAbove || runStalled)
                {
                    NPC.velocity.Y = SpearMeleeJumpPower;
                    NPC.netUpdate = true;
                }
            }
        }
        #endregion

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

        /// <summary>True while this knight already has a GreatBlackKnightFlail head out. Gates both
        /// selection (PickNextAttack) and the actual throw below — only one ball-and-chain at a time;
        /// the next one can't launch until this one retracts to the hand and self-destructs.</summary>
        private bool HasActiveFlail()
        {
            int flailType = ModContent.ProjectileType<Projectiles.Enemy.Weapons.GreatBlackKnightFlail>();
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p.active && p.type == flailType && (int)p.ai[0] == NPC.whoAmI)
                {
                    return true;
                }
            }
            return false;
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

            // Defensive: PickNextAttack already excludes Flail while one is active, so this should
            // never actually trigger — but if it somehow does, abandon the throw rather than let two
            // heads exist at once.
            if (HasActiveFlail())
            {
                EndAttack();
                return;
            }

            NPC.TargetClosest(true);
            Vector2 anchor = CurrentHandWorld();
            Vector2 toPlayer = player.Center - anchor;
            if (toPlayer == Vector2.Zero)
            {
                toPlayer = new Vector2(NPC.direction, 0f);
            }
            toPlayer.Normalize();

            bool spin = Main.rand.NextBool(3); // occasional windmill instead of a straight throw
            Vector2 velocity = spin ? Vector2.Zero : toPlayer * FlailLaunchSpeed;

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
            ApplyHitDebuffs(target);
        }

        /// <summary>Spear-jab hits route here so the jab applies the same debuffs as a body hit.</summary>
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
        // GreatBlackKnight_Arm.png now exists (90x928, same 16x 90x58 grid as the body) — same
        // layering pattern as GreatRedKnight: body (default draw, before PostDraw runs) < held prop
        // (drawn here) < arm (drawn here, on top), so the arm appears to grip the weapon instead of
        // the weapon floating in front of a bare hand. Frame 0 = idle, frame 1 = jump, 2-15 = walk.
        static Texture2D armOverlayTexture;
        const float FrameW = 90f;
        const float FrameH = 58f;
        // Measured off GreatBlackKnight.png: the flail handle meets the body at roughly (40, 37) in
        // frame space. The art is 2x-doubled (45x29 logical), and the gripping arm barely moves
        // across the walk cycle, so a 16-entry table would encode noise rather than motion — one
        // constant is the honest representation. Frame 1 (the crouched jump) is the only real
        // outlier and the spear is never thrown mid-jump.
        static readonly Vector2 HandPixel = new Vector2(40f, 37f);
        // The spear now sits AT the measured hand. This was 22f — a lift into a cocked overhand
        // pose copied from the Black Knight — which put the shaft up by his head instead of in his
        // fist, ~20px too high. The Black Knight needs that lift because its HandPixel table maps
        // the fist at chest height; this knight's (40, 37) is already the grip.
        const float SpearGripLift = 0f;
        static readonly Vector2 SpearGripOrigin = new Vector2(8f, 38f);
        static readonly Vector2 BombGripOrigin = new Vector2(14f, 4f);

        // facingDirection is explicit so a held prop always mirrors to the same side of the body as
        // the rotation it is drawn with.
        // 0 disables the body mask and falls back to a plain draw. This used to punch the body's
        // silhouette out of the shaft so the spear read as gripped, but that's now the arm overlay's
        // job (drawn on top of the weapon in PostDraw below) — leaving the mask on just hid most of
        // the shaft wherever it crossed the body, making the spear look like it sat behind the knight.
        const float SpearMaskStrength = 0f;

        // Single frame-pixel -> world mapping, so a held prop's draw position and the occlusion
        // mask's lookup are derived from the same definition and cannot drift apart.
        Vector2 FramePixelToWorld(Vector2 fp, int facingDirection)
        {
            float x = NPC.Center.X + (fp.X - FrameW / 2f) * NPC.scale * -facingDirection;
            float y = NPC.Center.Y + 24f + NPC.gfxOffY + (fp.Y - FrameH) * NPC.scale;
            return new Vector2(x, y);
        }

        /// <summary>
        /// Index of the body frame currently showing. Frame 0 is idle, frame 1 is the crouched jump pose,
        /// 2+ are the walk cycle.
        /// </summary>
        int CurrentFrameIndex() => FrameH > 0 ? (int)(NPC.frame.Y / FrameH) : 0;

        /// <summary>
        /// Per-frame grip correction, in frame pixels. HandPixel is the walk-cycle average (the gripping arm
        /// barely moves across frames 2+), but the two non-walk poses are real outliers: the idle stance drops
        /// the fist lower, and the crouched jump tucks it higher. Only matters now that the spear is held
        /// through melee — the ranged throw is never released from these frames.
        /// </summary>
        static float FrameGripYOffset(int frameIndex) => frameIndex switch
        {
            0 => 5f,    // idle — grip sits lower
            1 => -10f,  // crouched jump — grip rides higher
            _ => 0f,    // walk cycle — HandPixel is already the fit
        };

        Vector2 CurrentSpearFramePixel() => new Vector2(
            HandPixel.X,
            HandPixel.Y + FrameGripYOffset(CurrentFrameIndex()) - SpearGripLift / NPC.scale);

        Vector2 CurrentHandWorld(int facingDirection) => FramePixelToWorld(HandPixel, facingDirection);

        Vector2 CurrentHandWorld() => CurrentHandWorld(NPC.spriteDirection);

        Vector2 CurrentSpearWorld(int facingDirection) => FramePixelToWorld(CurrentSpearFramePixel(), facingDirection);

        Vector2 CurrentSpearWorld() => CurrentSpearWorld(NPC.spriteDirection);

        /// <summary>
        /// Selects the jump pose while airborne, which vanilla never does for this knight.
        /// </summary>
        /// <remarks>
        /// Vanilla's AnimationType 28 frame logic lives ENTIRELY inside `if (velocity.Y == 0f)` — see
        /// Terraria.NPC.FindFrame. Grounded, it shows frame 0 when velocity.X is 0 and otherwise steps the
        /// walk cycle from frame 2 up. Airborne, it does nothing at all: whatever frame was showing at
        /// takeoff stays frozen for the whole arc, and the jump pose at frame 1 is never selected by
        /// anything.
        ///
        /// That is the "moves forward while stuck in the idle animation frame" bug. RunTelegraph fires a
        /// pre-attack hop ~10 ticks before EVERY attack, so if velocity.X happened to be 0 on the takeoff
        /// tick (vanilla had just picked frame 0) the knight sailed forward through the entire hop holding
        /// the idle pose. Fixing it here covers hops from any source — the telegraph cue, the movement
        /// flourishes, and ordinary SF4 ledge jumps — rather than patching each one.
        ///
        /// The airborne test matches GreatRedKnight.FindFrame rather than vanilla's exact `!= 0f`: a bare
        /// inequality also trips on the tiny vertical velocities of walking down a slope, which would flicker
        /// the jump pose mid-stride. Those few ticks keep vanilla's frozen-frame behaviour instead.
        /// </remarks>
        public override void FindFrame(int frameHeight)
        {
            if (frameHeight <= 0)
            {
                return;
            }

            bool airborne = NPC.velocity.Y < -0.01f || (!NPC.collideY
                && (Math.Abs(NPC.velocity.Y) > 0.01f || Math.Abs(NPC.oldVelocity.Y) > 0.01f));
            if (airborne)
            {
                NPC.frame.Y = frameHeight; // frame 1 — the jump pose
                NPC.frameCounter = 0d;
            }

            FaceAttackAim();
        }

        /// <summary>
        /// While an attack is winding up or firing, points the BODY at whatever the weapon is pointing at.
        /// </summary>
        /// <remarks>
        /// The held spear and bomb rotate toward the aim point independently of the body (see PostDraw), but
        /// vanilla re-derives spriteDirection from NPC.direction on every grounded frame — so a knight that
        /// was walking away when the attack committed got drawn facing backwards with its spear pointed
        /// forwards at the player.
        ///
        /// Called from FindFrame specifically because that is the one hook that runs AFTER vanilla's
        /// spriteDirection assignment; setting this in AI() would just be overwritten. Only spriteDirection
        /// is touched, never NPC.direction — direction drives movement, and it has to stay free so the knight
        /// can keep walking and kiting through its own windup instead of stopping to face the player.
        /// </remarks>
        void FaceAttackAim()
        {
            Phase phase = (Phase)(int)NPC.ai[0];
            if (phase != Phase.Telegraph && phase != Phase.Committed)
            {
                return;
            }

            AttackKind kind = (AttackKind)(int)NPC.ai[2];
            // Spear/Bomb/Ultrakill lead a ~25-tick-old predicted point (RunTelegraph); the rest aim live.
            // Match whichever one the weapon is actually drawn against so body and weapon cannot disagree.
            bool usesPredictedAim = kind == AttackKind.Spear || kind == AttackKind.Bomb || kind == AttackKind.Ultrakill;
            Vector2 aimPoint;
            if (usesPredictedAim && storedPlayerPosition != Vector2.Zero)
            {
                aimPoint = storedPlayerPosition;
            }
            else if (player.active && !player.dead)
            {
                aimPoint = player.Center;
            }
            else
            {
                return; // nothing trustworthy to face (e.g. an MP client that never received a target)
            }

            NPC.spriteDirection = aimPoint.X >= NPC.Center.X ? 1 : -1;
        }

        void DrawArmOverlay(SpriteBatch spriteBatch, Color drawColor, int facingDirection)
        {
            if (armOverlayTexture == null)
            {
                return;
            }

            SpriteEffects effects = facingDirection < 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
            Rectangle sourceRectangle = new Rectangle(0, NPC.frame.Y, (int)FrameW, (int)FrameH);
            Vector2 drawPosition = NPC.Center + new Vector2(0f, 24f + NPC.gfxOffY) - Main.screenPosition;
            spriteBatch.Draw(armOverlayTexture, drawPosition, sourceRectangle, drawColor, NPC.rotation, new Vector2(FrameW / 2f, FrameH), NPC.scale, effects, 0f);
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

            if (armOverlayTexture == null || armOverlayTexture.IsDisposed)
            {
                armOverlayTexture = ModContent.Request<Texture2D>("tsorcRevamp/NPCs/Enemies/GreatBlackKnight_Arm", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
            }

            Phase phase = (Phase)(int)NPC.ai[0];
            AttackKind currentAttack = (AttackKind)(int)NPC.ai[2];
            bool active = phase == Phase.Telegraph || phase == Phase.Committed;

            // Spear jab: held through the whole telegraph AND the strike, sliding along its own axis
            // so the thrust is the weapon moving rather than the knight teleporting it forward.
            if (active && currentAttack == AttackKind.SpearMelee)
            {
                const float spriteScale = 0.8f;
                int facing = NPC.spriteDirection;
                float gripSlide = SpearMeleeGripSlide(phase, (int)NPC.ai[1]);
                float rotation = new Vector2(facing, 0f).ToRotation() + MathHelper.PiOver2;
                Vector2 handWorld = CurrentSpearWorld(facing) - Main.screenPosition;
                if (SpearMeleeHitWindow)
                {
                    Vector2 forward = new Vector2(facing, 0f);
                    Projectiles.Enemy.EnemyVFX.DrawBlackKnightSpearWake(
                        handWorld + Main.screenPosition + forward * (34f + gripSlide),
                        forward.ToRotation(), new Vector2(86f, 18f), 0.6f);
                }
                HeldPropDraw.DrawOccluded(
                    spriteBatch, spearTexture, handWorld, drawColor, rotation,
                    SpearGripOrigin + new Vector2(0f, gripSlide), NPC.scale * spriteScale,
                    Terraria.GameContent.TextureAssets.Npc[NPC.type].Value,
                    new Rectangle(0, NPC.frame.Y, (int)FrameW, (int)FrameH),
                    CurrentSpearFramePixel(), facing, NPC.scale, SpearMaskStrength);
                DrawArmOverlay(spriteBatch, drawColor, facing);
            }
            // Spear (thrown)
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
                // Body-masked so the shaft disappears where the knight covers it (see HeldPropDraw).
                HeldPropDraw.DrawOccluded(
                    spriteBatch, spearTexture, handWorld, drawColor, rotation, SpearGripOrigin,
                    NPC.scale * spriteScale,
                    Terraria.GameContent.TextureAssets.Npc[NPC.type].Value,
                    new Rectangle(0, NPC.frame.Y, (int)FrameW, (int)FrameH),
                    CurrentSpearFramePixel(), NPC.spriteDirection, NPC.scale, SpearMaskStrength);
                DrawArmOverlay(spriteBatch, drawColor, NPC.spriteDirection);
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
                // The Moonfury shader used to be drawn here and sat visibly offset from the bomb
                // sprite. Replaced with a red fuse spark at the bomb's own fuse, which needs no
                // alignment between a quad and a sprite to read correctly.
                spriteBatch.Draw(bombTexture, handWorld, new Rectangle(0, 0, bombTexture.Width, bombTexture.Height), drawColor, rotation, BombGripOrigin, NPC.scale, SpriteEffects.None, 0);
                Projectiles.Enemy.EnemyVFX.SpawnBombFuseSparks(handWorld + Main.screenPosition, fuseProgress);
                // NPC.spriteDirection, not bombAim's sign: CurrentHandWorld() (used for handWorld above)
                // anchors on spriteDirection, and the arm must mirror the same way as the hand it grips.
                DrawArmOverlay(spriteBatch, drawColor, NPC.spriteDirection);
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

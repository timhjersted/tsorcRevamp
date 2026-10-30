using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;
using tsorcRevamp.Content.Items;
using tsorcRevamp.Content.Items.Armor.GreatBlackKnightSet;
using tsorcRevamp.Content.Projectiles;
using tsorcRevamp.Content.Projectiles.Enemy;
using tsorcRevamp.Content.Projectiles.Enemy.Weapons;
using tsorcRevamp.Content.Projectiles.VFX;
using tsorcRevamp.NPCs.Puppets;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.NPCs.Enemies
{
    /// <summary>
    /// The Great Black Knight as a standalone PuppetNPC: the GreatBlackKnightSet armour (no dye), a flail as the main
    /// weapon, and its own attack brain hosted one attack per AttackPhase.Custom (the Red Knight / Black Knight
    /// convention). Close range is the shared puppet flail combos; everything else is the brain below, which plays
    /// attacks in short momentum chains. Below 50% it turns spectral: ghost echoes, Blight on touch, a black trail.
    /// See tsorcDocs/GreatBlackKnightPuppet_Proposal.md (decisions in §0, §0b, §0c).
    /// </summary>
    public class GreatBlackKnight : PuppetNPC, IFlailAnchor, IHumanoidMeleeHitEffects
    {
        #region Defaults
        // Declared Expert Mode damage before player defense and damage variation (EnemyDamage.Projectile).
        public int redKnightsSpearDamage = 150;  // Spear throw & Moonfury bomb (real expert damage)
        public int redMagicDamage = 125;         // Homing crystals & Shadow Crystal Storm (real expert damage)
        public int redKnightsGreatDamage = 175;  // Death Seal stream & Skyfall burst (real expert damage)
        public int redFlailDamage = 180;         // Flail head, thrown and in the combos (real expert damage)

        // The predicted aim point, refreshed on a 25-tick cadence during tells. Synced: every client draws against it.
        Vector2 storedPlayerPosition = Vector2.Zero;
        int framesSinceStoredPosition;

        // A brain attack ends itself; this cap only stops a stuck Custom phase from living forever.
        const int AttackPhaseCapTicks = 3600;

        public Player player => Main.player[NPC.target];

        protected override string InvaderTitle => "Great Black Knight";
        public override bool UsesPuppetDifficultyScaling => false;
        protected override bool AnnounceInvasion => false;
        protected override bool AnnounceInvaderDefeat => false;
        protected override int EstusChargesMax => 0;
        protected override int CasualStrollChance => 0;

        // Pure black under the armour, so any skin pixel the sheets leave uncovered reads as shadow.
        protected override Color PuppetSkinColor => Color.Black;

        // GreatBlackKnightSet is painted in its own colours, so no dye (a dye would remap that palette).
        protected override int HeadArmorItemType => ModContent.ItemType<GreatBlackKnightHelm>();
        protected override int BodyArmorItemType => ModContent.ItemType<GreatBlackKnightArmor>();
        protected override int LegsArmorItemType => ModContent.ItemType<GreatBlackKnightGreaves>();

        // The flail item is only the held identity (Dread Wraith's precedent); its icon is hidden and the
        // GreatBlackKnightFlail projectile draws the head and chain.
        protected override int MeleeWeaponItemType => ItemID.FlamingMace;
        protected override int RangedWeaponItemType => -1;
        protected override int MeleeDamage => EnemyDamage.Projectile(redFlailDamage);
        protected override int RangedDamage => 0;
        protected override void DoMeleeAttack() { } // All melee is the flail combo pool below.
        protected override void DoRangedAttack() { }
        protected override WeaponArchetype MeleeArchetype => WeaponArchetype.Flail;
        protected override bool HideHeldMeleeSprite => true;

        // Heavy, but quicker than the Black Knight (1.5); phase 2 gives it a longer stride (was an enrage to 2.9).
        protected override float TopSpeed => _phaseTwo ? 2.0f : 1.6f;
        protected override float Acceleration => 0.05f;
        protected override float PuppetJumpPower => 11f;
        protected override float PuppetJumpBoost => 7f;
        protected override bool PuppetCanDoubleJump => true;
        protected override float PuppetDoubleJumpPower => 7f;

        // Brain attacks own velocity (the Skyfall leap, the tells' slow approach); the puppet must not brake them.
        protected override bool SlowDownDuringCustom => false;
        protected override bool TeleportCancelsAttack => false;

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[Type] = 1;
            // 8 cached positions feed the phase-2 ghost echoes (PuppetNPC's fractal afterimage draw).
            NPCID.Sets.TrailCacheLength[Type] = 8;
            NPCID.Sets.TrailingMode[Type] = 1;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Confused] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.OnFire] = true;
        }

        public override void SetDefaults()
        {
            NPC.npcSlots = 10;
            NPC.aiStyle = -1;
            NPC.width = 20;
            NPC.height = 46;
            NPC.damage = 0; // puppet convention: only the flail, the spear, the bomb and the spells hurt
            NPC.defense = 61;
            NPC.lifeMax = 20000;
            NPC.value = 156000;
            if (tsorcRevampWorld.SuperHardMode)
            {
                NPC.lifeMax = 50000;
                NPC.value = 157000;
            }

            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.lavaImmune = true;
            NPC.knockBackResist = 0.2f; // poise flinch dial, boss tier like the Great Red Knight
            Banner = NPC.type;
            BannerItem = ModContent.ItemType<Banners.BlackKnightBanner>();

            tsorcRevampGlobalNPC globalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            globalNPC.Agility = 0.45f;
            globalNPC.Aggression = 1f;
            globalNPC.Patience = 2f;
            globalNPC.DirectionalDodgeRolls = true;
            globalNPC.PreserveJumpFacingUntilLanding = true;
            // Great Red Knight's trims: an occasional dodge roll is the only on-hit reaction (1 in 6, 120t apart).
            EvasiveProfile.RedKnight(globalNPC);
            globalNPC.EvasiveRetreatJump = false;
            globalNPC.EvasiveRetreatDash = false;
            globalNPC.EvasiveDodgeRoll = true;
            globalNPC.EvasiveTeleportAway = false;
            globalNPC.EvasiveOnHitChanceDenominator = 6;
            globalNPC.EvasiveOnHitCooldownTicks = 120;

            globalNPC.CanTeleport = true;
            globalNPC.TeleportStyle = TeleportStyle.Aggressive;
            globalNPC.TeleportVisualStyle = TeleportVisualStyle.Blight;
            // 30 + SmokeFireTeleportSnapTicks (30) hidden, then he steps out of a cloud with ~2s left to run.
            globalNPC.TeleportTelegraphTime = 30;

            globalNPC.PoiseMax = 80f;
            globalNPC.PoiseStaggerResetsAI = true;

            globalNPC.MaxJumpPower = 11f;
            globalNPC.MaxJumpBoost = 7f;
            globalNPC.CanDoubleJump = true;
            globalNPC.DoubleJumpPower = 7f;
            globalNPC.NavSearchRadius = 80;
            globalNPC.CanUseRopes = true;
        }
        #endregion

        #region Spawn
        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            if (ModContent.GetInstance<tsorcRevampConfig>().AdventureMode) return 0f;
            if (!tsorcRevampWorld.SuperHardMode) return 0f;
            if (spawnInfo.Player.townNPCs > 1f) return 0f;
            if (!spawnInfo.Player.ZoneMeteor && !spawnInfo.Player.ZoneDungeon && !(spawnInfo.Player.ZoneCorrupt || spawnInfo.Player.ZoneCrimson) && spawnInfo.Player.ZoneOverworldHeight && !Main.dayTime && Main.rand.NextBool(650)) return 1;
            if (spawnInfo.Player.ZoneDungeon && Main.rand.NextBool(450)) return 1;
            if (!(spawnInfo.Player.ZoneCorrupt || spawnInfo.Player.ZoneCrimson) && !spawnInfo.Player.ZoneBeach && Main.rand.NextBool(600)) return 1;
            if (spawnInfo.Player.ZoneUnderworldHeight && Main.rand.NextBool(500)) return 1;

            return 0;
        }
        #endregion

        #region Flail combos
        // ── Close range: the shared puppet flail patterns on GBK's own head ──────────────────────────────
        // GreatBlackKnightFlail derives from EnemyFlailProjectileBase and falls through to it for every authored
        // pattern, so these use GBK's head, chain and Blight shader. Timings live in EnemyFlailAttackPatterns:
        //   40t harmless 60px orbit tell | 18t directional live window (< 22: rollable) | 18t retract | recovery.
        // Chainrise Underhand (close) and Chainfall Overhead (mid, 240px) are the bread and butter; Advancing
        // Chainstorm (four 240px revolutions walking forward) is phase 2 only; Backlash Reversal answers a target
        // who ends up behind him after a flail move. One head at a time, shared with the Flail Hurl.
        static readonly MeleeCombo[] FlailCombos = BuildFlailComboPool();

        static MeleeCombo[] BuildFlailComboPool()
        {
            List<MeleeCombo> pool = new();
            foreach (MeleeCombo combo in WeaponArchetypeTables.Flail)
            {
                bool wanted = combo.Name == EnemyFlailAttackPatterns.OverheadName
                    || combo.Name == EnemyFlailAttackPatterns.UnderhandName
                    || combo.Name == EnemyFlailAttackPatterns.CycloneName;
                if (wanted)
                {
                    pool.Add(combo);
                }
            }
            foreach (MeleeCombo combo in WeaponArchetypeTables.FlailReactiveFollowups)
            {
                if (combo.Name == EnemyFlailAttackPatterns.BacklashName)
                {
                    pool.Add(combo);
                }
            }
            return pool.ToArray();
        }

        protected override MeleeCombo[] MeleeComboPoolOverride => FlailCombos;
        // No legacy slash path: MeleeRange 0 leaves the combo pool as the only melee.
        protected override float MeleeRange => 0f;
        protected override int MeleeComboChance => 100;
        // Chainfall reaches 240px from the hand; start inside 200 and close in from up to 220.
        protected override float MeleeEngageRange => 200f;
        protected override float ComboMaxStartRange => 220f;
        // Flail tells are authored on-screen ticks: the projectile's 40t orbit IS the tell.
        protected override float ComboTelegraphMultiplier => 1f;
        protected override int MinComboTelegraphTicks => 40;

        // Server-only Backlash memory (Black Ninja's rule): after a flail move, a target behind the committed facing
        // unlocks the rear whip for 120t.
        ulong _backlashReadyUntil;
        int _backlashFollowupFacing;
        int _backlashAttackFacing;

        /// <summary>IFlailAnchor: the chain draws to, and the head orbits and launches from, the puppet's front hand.</summary>
        public Vector2 GetFlailAnchor() => PuppetHandPosition;

        bool TryGetActiveFlail(out Projectile activeFlail)
        {
            int flailType = ModContent.ProjectileType<GreatBlackKnightFlail>();
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile projectile = Main.projectile[i];
                if (projectile.active && projectile.type == flailType && (int)projectile.ai[0] == NPC.whoAmI)
                {
                    activeFlail = projectile;
                    return true;
                }
            }
            activeFlail = null;
            return false;
        }

        bool HasActiveFlail()
        {
            return TryGetActiveFlail(out _);
        }

        protected override bool CanSelectMeleeCombo(MeleeCombo combo, float distance, float healthFraction)
        {
            if (HasActiveFlail() || !base.CanSelectMeleeCombo(combo, distance, healthFraction))
            {
                return false;
            }
            if (combo.Name == EnemyFlailAttackPatterns.CycloneName)
            {
                return _phaseTwo;
            }
            if (combo.Name == EnemyFlailAttackPatterns.BacklashName)
            {
                return BacklashFollowupStillValid();
            }
            return true;
        }

        protected override void OnMeleeComboStarted(MeleeCombo combo)
        {
            base.OnMeleeComboStarted(combo);
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            // A flail combo picked up during a push-in is the chain's next link.
            if (_linksLeft > 0 && _pushInTimer > 0)
            {
                _linksLeft--;
                _pushInTimer = PushInWindowTicks;
                NPC.netUpdate = true;
            }

            if (!EnemyFlailAttackPatterns.TryResolve(combo.Name, out EnemyFlailAttackPattern pattern) || HasActiveFlail())
            {
                return;
            }

            int facing = NPC.direction;
            if (facing == 0)
            {
                facing = 1;
            }
            _backlashReadyUntil = 0;
            if (pattern == EnemyFlailAttackPattern.BacklashReversal)
            {
                if (_backlashFollowupFacing != 0)
                {
                    facing = _backlashFollowupFacing;
                }
                _backlashAttackFacing = facing;
            }
            else if (NPC.HasValidTarget)
            {
                facing = 1;
                if (player.Center.X < NPC.Center.X)
                {
                    facing = -1;
                }
            }

            int damage = EnemyDamage.Projectile((int)(redFlailDamage * combo.Steps[0].DamageMult));
            Projectile projectile = Projectile.NewProjectileDirect(NPC.GetSource_FromThis(), GetFlailAnchor(),
                new Vector2(facing, 0f), ModContent.ProjectileType<GreatBlackKnightFlail>(), damage, 3.5f, Main.myPlayer,
                NPC.whoAmI, (float)pattern, EnemyFlailAttackPatterns.ReachFor(pattern));
            projectile.timeLeft = EnemyFlailAttackPatterns.MaximumLifetime;
            projectile.netUpdate = true;
        }

        // The head was created at combo start so its orbit IS the tell; the strike releases that same head.
        protected override void DoComboMeleeHit(MeleeComboStep step)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }
            if (TryGetActiveFlail(out Projectile activeFlail) && activeFlail.ModProjectile is EnemyFlailProjectileBase flail)
            {
                flail.ReleaseAuthoredPattern();
            }
        }

        protected override bool ShouldContinueMeleeCombo(string comboName, int nextStepIndex, Player target, bool previousStepHit)
        {
            return !HasActiveFlail() && base.ShouldContinueMeleeCombo(comboName, nextStepIndex, target, previousStepHit);
        }

        protected override bool TrackMeleeComboFacingDuringTelegraph(MeleeCombo combo)
        {
            return !EnemyFlailAttackPatterns.LocksFacingDuringTell(combo.Name);
        }

        protected override int GetMeleeComboStartFacing(MeleeCombo combo, Player target)
        {
            if (combo.Name == EnemyFlailAttackPatterns.BacklashName && _backlashAttackFacing != 0)
            {
                return _backlashAttackFacing;
            }
            return base.GetMeleeComboStartFacing(combo, target);
        }

        protected override int ReactiveComboIndex(float dist, ComboRangeBand band, int[] ready)
        {
            if (!BacklashFollowupStillValid())
            {
                return base.ReactiveComboIndex(dist, band, ready);
            }
            for (int i = 0; i < FlailCombos.Length && i < ready.Length; i++)
            {
                if (ready[i] > 0 && FlailCombos[i].Name == EnemyFlailAttackPatterns.BacklashName)
                {
                    return i;
                }
            }
            return base.ReactiveComboIndex(dist, band, ready);
        }

        protected override void OnComboStepCompleted(MeleeComboStep step)
        {
            base.OnComboStepCompleted(step);
            bool flailMove = EnemyFlailAttackPatterns.TryResolve(ActiveMeleeComboName, out EnemyFlailAttackPattern completed);
            if (Main.netMode == NetmodeID.MultiplayerClient || !NPC.HasValidTarget || !flailMove
                || completed == EnemyFlailAttackPattern.BacklashReversal)
            {
                return;
            }

            int facing = NPC.direction;
            if (facing == 0)
            {
                facing = 1;
            }
            _backlashReadyUntil = 0;
            if (TargetQualifiesForBacklash(player, facing))
            {
                _backlashFollowupFacing = facing;
                _backlashReadyUntil = Main.GameUpdateCount + 120UL;
            }
        }

        bool BacklashFollowupStillValid()
        {
            if (_backlashReadyUntil == 0 || Main.GameUpdateCount > _backlashReadyUntil || !NPC.HasValidTarget)
            {
                return false;
            }
            return TargetQualifiesForBacklash(player, _backlashFollowupFacing);
        }

        bool TargetQualifiesForBacklash(Player target, int facing)
        {
            if (!target.active || target.dead || facing == 0)
            {
                return false;
            }
            float horizontalBehind = (target.Center.X - NPC.Center.X) * facing;
            bool level = Math.Abs(target.Center.Y - NPC.Center.Y) <= EnemyFlailAttackPatterns.BacklashTargetVerticalTolerance;
            bool inReach = Vector2.Distance(target.Center, NPC.Center) <= EnemyFlailAttackPatterns.BacklashReach + 48f;
            return horizontalBehind < -24f && level && inReach;
        }
        #endregion

        #region Brain: state, timings, gates
        // ── Attack state ────────────────────────────────────────────────────────────────────────────────
        //   ai[2] = current GreatBlackKnightAttack, ai[3] = tick within it. Both ride the NPC sync and the puppet's phase
        //   snapshots; every machine advances ai[3] while an attack runs so poses and tell dust are smooth on clients.
        GreatBlackKnightAttack CurrentAttack => (GreatBlackKnightAttack)(int)NPC.ai[2];
        int AttackTimer => (int)NPC.ai[3];
        bool AttackActive => CurrentAttack != GreatBlackKnightAttack.None;

        // ── Momentum chains (proposal §4) ───────────────────────────────────────────────────────────────
        // Each attack is self-contained with its own recovery. A chain is only a budget of follow-up links: halfway
        // through a recovery, if a link is valid right now it starts at once (no idle); if nothing is valid he
        // pushes in at a faster stride for up to 90t and takes the first link that becomes valid, a flail combo
        // included; the last link plays its full recovery + 15t per extra link (the "spent" punish window).
        const int ChainCap = 3;                     // links per chain, rolled 1..3 (round 3 decision)
        const int PushInWindowTicks = 90;
        const float PushInSpeedMult = 1.35f;
        const int SpentRecoveryPerLink = 15;
        const int SpentRecoveryCap = 45;
        const int NeutralCooldownTicks = 45;        // walking time between chains
        const int CancelCooldownTicks = 20;
        int _linksLeft;                             // synced: drives the push-in stride on every machine
        int _pushInTimer;                           // synced
        int _chainLength = 1;                       // server only
        bool _chainUsedDeathSeal;                   // server only: at most one Death Seal per chain
        int _attackCooldown;                        // server only
        GreatBlackKnightAttack _lastAttack;         // server only: no immediate repeat inside a chain

        // ── Timings (ticks) ─────────────────────────────────────────────────────────────────────────────
        // Spear Throw: release 60, flash 25 before it (commit), 24t recovery. Bomb Throw: the same clock.
        const int SpearReleaseTick = 60;
        const int BombReleaseTick = 60;
        const int ThrowFlashLead = 25;
        const int ThrowRecoveryTicks = 24;
        // Homing Volley (Black Knight's): palm raise, bursts at 60 and 110, flash 25 before each, 26t recovery.
        const int HomingFirstBurstTick = 60;
        const int HomingSecondBurstTick = 110;
        const int HomingRecoveryTicks = 26;
        // Death Seal (ex-Ultrakill): 100t sigil with the converging ring, flash/commit at 65, 35t stream, 30t recovery.
        //   The first 65t are the staggerable channel; after the flash it is hyper-armoured.
        const int DeathSealTelegraphTicks = 100;
        const int DeathSealFlashTick = 65;
        const int DeathSealFireTicks = 35;
        const int DeathSealRecoveryTicks = 30;
        // Shadow Crystal Storm (below 1/3): two palms gather 40t (commit at 20), five crystals at 40, 26t recovery.
        const int CrystalStormCommitTick = 20;
        const int CrystalStormReleaseTick = 40;
        const int CrystalStormRecoveryTicks = 26;
        const int CrystalStormCount = 5;
        const float CrystalStormSpread = MathHelper.Pi / 3f;
        // Flail Hurl: 40t harmless orbit (one revolution, the tell), then released the first tick the chain points at
        //   the target, at most 45t later; 16 px/t for 26t (476px), reels back. 40t planted recovery from release.
        const int HurlOrbitTicks = 40;
        const int HurlAlignWaitTicks = 45;
        const int HurlCommitTicks = 20;
        const int HurlRecoveryTicks = 40;
        const float HurlLaunchSpeed = 16f;
        // Skyfall Flail: 30t tell (flash at 20) | solved leap | one apex re-aim (+/-160px) | Blight burst on landing,
        //   live 10t, 96px | 45t planted recovery. Bails out after 150t airborne.
        const int SkyfallTellTicks = 30;
        const int SkyfallFlashTick = 20;
        const int SkyfallMaxAirTicks = 150;
        const int SkyfallRecoveryTicks = 45;
        const float SkyfallOvershootPixels = 40f;
        const float SkyfallApexCorrection = 160f;
        const float NpcGravity = 0.3f;              // vanilla NPC gravity per tick (terminal 10 px/t)
        // Phase-2 transition: 40t, hyper-armoured, Blight burst + screen shake, no roar (round 3).
        const int TransitionTicks = 40;

        // Synced: tick the hurl released (-1 = still orbiting) and the tick Skyfall landed (-1 = not yet).
        int _hurlReleaseTick = -1;
        int _skyfallLandTick = -1;
        bool _skyfallApexDone;                      // server only

        // ── Range gates (px) ────────────────────────────────────────────────────────────────────────────
        // Every attack has one (Homing included): with nothing valid he pushes in instead of firing blind.
        const float SpearMinRange = 140f;
        const float SpearMaxRange = 900f;
        const float BombMinRange = 160f;
        const float BombMaxRange = 800f;
        const float HomingMinRange = 240f;
        const float HomingMaxRange = 700f;
        const float HurlMinRange = 240f;
        const float HurlMaxRange = 480f;
        const float DeathSealMinRange = 100f;
        const float DeathSealMaxRange = 700f;
        const float CrystalStormMinRange = 200f;
        const float CrystalStormMaxRange = 900f;
        const float SkyfallMinRange = 300f;
        const float SkyfallMaxRange = 640f;
        const float SkyfallAboveTrigger = 64f;      // or: target this far above and within SkyfallAboveRange
        const float SkyfallAboveRange = 400f;
        const float SkyfallMinCeilingTiles = 6f;
        const float AimOvershootPixels = 45f;       // spear and bomb land on and past the target, never short

        // Weights for each pick (the flail stays the most common).
        const int HurlWeight = 110;
        const int SpearWeight = 30;
        const int HomingWeight = 25;
        const int BombWeight = 20;
        const int DeathSealWeight = 30;
        const int CrystalStormWeight = 25;
        const int SkyfallWeight = 30;

        /// <summary>Tick the attack's recovery starts (its last action), or -1 while that is not known yet.</summary>
        int RecoveryStartTick(GreatBlackKnightAttack attack)
        {
            switch (attack)
            {
                case GreatBlackKnightAttack.SpearThrow: return SpearReleaseTick;
                case GreatBlackKnightAttack.BombThrow: return BombReleaseTick;
                case GreatBlackKnightAttack.HomingVolley: return HomingSecondBurstTick;
                case GreatBlackKnightAttack.DeathSeal: return DeathSealTelegraphTicks + DeathSealFireTicks;
                case GreatBlackKnightAttack.CrystalStorm: return CrystalStormReleaseTick;
                case GreatBlackKnightAttack.FlailHurl: return _hurlReleaseTick;
                case GreatBlackKnightAttack.Skyfall: return _skyfallLandTick;
                case GreatBlackKnightAttack.PhaseTwoTransition: return TransitionTicks;
            }
            return 0;
        }

        static int RecoveryTicksOf(GreatBlackKnightAttack attack)
        {
            switch (attack)
            {
                case GreatBlackKnightAttack.SpearThrow: return ThrowRecoveryTicks;
                case GreatBlackKnightAttack.BombThrow: return ThrowRecoveryTicks;
                case GreatBlackKnightAttack.HomingVolley: return HomingRecoveryTicks;
                case GreatBlackKnightAttack.DeathSeal: return DeathSealRecoveryTicks;
                case GreatBlackKnightAttack.CrystalStorm: return CrystalStormRecoveryTicks;
                case GreatBlackKnightAttack.FlailHurl: return HurlRecoveryTicks;
                case GreatBlackKnightAttack.Skyfall: return SkyfallRecoveryTicks;
            }
            return 0;
        }

        /// <summary>True while the attack is locked in (hyper-armour). Before this a poise break cancels the windup;
        /// the recovery after it has no armour at all (the punish window).</summary>
        bool InCommittedAttack()
        {
            int timer = AttackTimer;
            switch (CurrentAttack)
            {
                case GreatBlackKnightAttack.SpearThrow: return timer >= SpearReleaseTick - ThrowFlashLead && timer <= SpearReleaseTick;
                case GreatBlackKnightAttack.BombThrow: return timer >= BombReleaseTick - ThrowFlashLead && timer <= BombReleaseTick;
                case GreatBlackKnightAttack.HomingVolley: return timer >= HomingFirstBurstTick && timer <= HomingSecondBurstTick;
                case GreatBlackKnightAttack.DeathSeal: return timer >= DeathSealFlashTick && timer < DeathSealTelegraphTicks + DeathSealFireTicks;
                case GreatBlackKnightAttack.CrystalStorm: return timer >= CrystalStormCommitTick && timer <= CrystalStormReleaseTick;
                case GreatBlackKnightAttack.FlailHurl: return _hurlReleaseTick >= 0 && timer < _hurlReleaseTick + HurlCommitTicks;
                case GreatBlackKnightAttack.Skyfall: return timer >= SkyfallTellTicks && _skyfallLandTick < 0;
                case GreatBlackKnightAttack.PhaseTwoTransition: return true;
            }
            return false;
        }

        /// <summary>The readable windup: active, not committed, and not yet in the recovery.</summary>
        bool InAttackTelegraph()
        {
            if (!AttackActive || InCommittedAttack())
            {
                return false;
            }
            int recoveryStart = RecoveryStartTick(CurrentAttack);
            return recoveryStart < 0 || AttackTimer < recoveryStart;
        }

        /// <summary>FlailHurl's head orbits until released; GreatBlackKnightFlail kills an orbiting head once this goes false
        /// (a stagger or cancel), so the head retracts with the attack.</summary>
        public bool FlailWindupActive => CurrentAttack == GreatBlackKnightAttack.FlailHurl && _hurlReleaseTick < 0;

        // Phase 2 (below 50%): latched server-side and synced. The transition beat plays once, at the next free moment.
        bool _phaseTwo;
        bool _phaseTwoTransitionDone;               // server only

        string CurrentAttackDebugName
        {
            get
            {
                string name = CurrentAttack switch
                {
                    GreatBlackKnightAttack.FlailHurl => "Flail Hurl",
                    GreatBlackKnightAttack.SpearThrow => "Spear Throw",
                    GreatBlackKnightAttack.HomingVolley => "Homing Volley",
                    GreatBlackKnightAttack.BombThrow => "Bomb Throw",
                    GreatBlackKnightAttack.DeathSeal => "Death Seal",
                    GreatBlackKnightAttack.CrystalStorm => "Shadow Crystal Storm",
                    GreatBlackKnightAttack.Skyfall => "Skyfall Flail",
                    GreatBlackKnightAttack.PhaseTwoTransition => "Phase 2",
                    _ => null,
                };
                if (name == null)
                {
                    return null;
                }
                if (InAttackTelegraph())
                {
                    name += " (Windup)";
                }
                else if (!InCommittedAttack())
                {
                    name += " (Recovery)";
                }
                if (_linksLeft > 0)
                {
                    name += $" [+{_linksLeft}]";
                }
                return name;
            }
        }
        #endregion

        #region Brain: selection
        /// <summary>Shared gate for starting anything (server).</summary>
        bool CanStartAttack(tsorcRevampGlobalNPC globalNPC)
        {
            return Main.netMode != NetmodeID.MultiplayerClient
                && player.active && !player.dead
                && NPC.velocity.Y == 0f
                && globalNPC.StaggerTimer <= 0
                && globalNPC.TeleportCountdown <= 0
                && globalNPC.TeleportAppearanceTimer <= 0
                && globalNPC.DodgeTimer <= 0
                && globalNPC.DodgeRecoveryTimer <= 0
                && !globalNPC.Fleeing;
        }

        bool IsAttackValid(GreatBlackKnightAttack attack, float distance, bool hasPlayerLOS)
        {
            // One attack's geometry on screen at a time: nothing starts while a flail head is still out.
            if (HasActiveFlail())
            {
                return false;
            }

            switch (attack)
            {
                case GreatBlackKnightAttack.FlailHurl:
                    return hasPlayerLOS && distance >= HurlMinRange && distance <= HurlMaxRange;
                case GreatBlackKnightAttack.SpearThrow:
                    return hasPlayerLOS && distance >= SpearMinRange && distance <= SpearMaxRange;
                case GreatBlackKnightAttack.HomingVolley:
                    return hasPlayerLOS && distance >= HomingMinRange && distance <= HomingMaxRange;
                case GreatBlackKnightAttack.BombThrow:
                    return hasPlayerLOS && distance >= BombMinRange && distance <= BombMaxRange;
                case GreatBlackKnightAttack.DeathSeal:
                    return _phaseTwo && !_chainUsedDeathSeal && hasPlayerLOS
                        && distance >= DeathSealMinRange && distance <= DeathSealMaxRange;
                case GreatBlackKnightAttack.CrystalStorm:
                    return NPC.life <= NPC.lifeMax / 3 && hasPlayerLOS
                        && distance >= CrystalStormMinRange && distance <= CrystalStormMaxRange;
                case GreatBlackKnightAttack.Skyfall:
                {
                    // Two cases: a target 300-640px away roughly level, or one standing well above him nearby.
                    float horizontal = Math.Abs(player.Center.X - NPC.Center.X);
                    float rise = NPC.Bottom.Y - player.Bottom.Y;
                    bool levelAndFar = horizontal >= SkyfallMinRange && horizontal <= SkyfallMaxRange && Math.Abs(rise) <= 200f;
                    bool above = rise >= SkyfallAboveTrigger && distance <= SkyfallAboveRange;
                    bool roomToLeap = CeilingClearance() >= SkyfallMinCeilingTiles * 16f;
                    return hasPlayerLOS && (levelAndFar || above) && roomToLeap;
                }
            }
            return false;
        }

        // Open air above the head, in px (scans up to 20 tiles). Caps the Skyfall apex and gates it under low roofs.
        float CeilingClearance()
        {
            int tileX = (int)(NPC.Center.X / 16f);
            int startY = (int)(NPC.position.Y / 16f) - 1;
            for (int i = 0; i < 20; i++)
            {
                int tileY = startY - i;
                if (WorldGen.SolidTile(tileX, tileY))
                {
                    return i * 16f;
                }
            }
            return 20 * 16f;
        }

        /// <summary>Server: weighted pick among the attacks valid right now, never repeating the last one inside a
        /// chain. None when nothing qualifies — the caller then pushes in instead of firing blind.</summary>
        GreatBlackKnightAttack PickAttack(bool continuingChain)
        {
            float distance = NPC.Distance(player.Center);
            bool hasPlayerLOS = Collision.CanHitLine(NPC.Center, 1, 1, player.Center, 1, 1);
            Span<(GreatBlackKnightAttack Attack, int Weight)> table = stackalloc (GreatBlackKnightAttack, int)[]
            {
                (GreatBlackKnightAttack.FlailHurl, HurlWeight),
                (GreatBlackKnightAttack.SpearThrow, SpearWeight),
                (GreatBlackKnightAttack.HomingVolley, HomingWeight),
                (GreatBlackKnightAttack.BombThrow, BombWeight),
                (GreatBlackKnightAttack.DeathSeal, DeathSealWeight),
                (GreatBlackKnightAttack.CrystalStorm, CrystalStormWeight),
                (GreatBlackKnightAttack.Skyfall, SkyfallWeight),
            };

            int total = 0;
            for (int i = 0; i < table.Length; i++)
            {
                bool repeat = continuingChain && table[i].Attack == _lastAttack;
                if (repeat || !IsAttackValid(table[i].Attack, distance, hasPlayerLOS))
                {
                    table[i].Weight = 0;
                }
                total += table[i].Weight;
            }
            if (total <= 0)
            {
                return GreatBlackKnightAttack.None;
            }

            int roll = Main.rand.Next(total);
            for (int i = 0; i < table.Length; i++)
            {
                roll -= table[i].Weight;
                if (roll < 0)
                {
                    return table[i].Attack;
                }
            }
            return GreatBlackKnightAttack.None;
        }

        /// <summary>Server, from neutral: the phase-2 beat, a push-in link, or a fresh chain. True when an attack
        /// started (the caller hosts it in a Custom phase).</summary>
        bool TryStartFromNeutral(tsorcRevampGlobalNPC globalNPC)
        {
            if (!CanStartAttack(globalNPC))
            {
                return false;
            }

            if (_phaseTwo && !_phaseTwoTransitionDone)
            {
                _phaseTwoTransitionDone = true;
                _linksLeft = 0;
                _pushInTimer = 0;
                _chainLength = 1;
                BeginAttack(GreatBlackKnightAttack.PhaseTwoTransition);
                return true;
            }

            if (_linksLeft > 0)
            {
                GreatBlackKnightAttack link = PickAttack(continuingChain: true);
                if (link == GreatBlackKnightAttack.None)
                {
                    return false;
                }
                _linksLeft--;
                BeginAttack(link);
                return true;
            }

            if (_attackCooldown > 0)
            {
                return false;
            }

            // A fresh chain: the one-Death-Seal limit resets before the opener is picked.
            _chainUsedDeathSeal = false;
            GreatBlackKnightAttack opener = PickAttack(continuingChain: false);
            if (opener == GreatBlackKnightAttack.None)
            {
                return false;
            }
            _chainLength = Main.rand.Next(1, ChainCap + 1);
            _linksLeft = _chainLength - 1;
            BeginAttack(opener);
            return true;
        }
        #endregion

        #region Brain: lifecycle
        /// <summary>Server: starts an attack on tick 0, facing the target.</summary>
        void BeginAttack(GreatBlackKnightAttack attack)
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

            _lastAttack = attack;
            _hurlReleaseTick = -1;
            _skyfallLandTick = -1;
            _skyfallApexDone = false;
            _pushInTimer = 0;
            if (attack == GreatBlackKnightAttack.DeathSeal)
            {
                _chainUsedDeathSeal = true;
            }
            if (attack != GreatBlackKnightAttack.DeathSeal)
            {
                storedPlayerPosition = player.Center;
                framesSinceStoredPosition = 0;
            }

            // The hurl's head is out from tick 0: its harmless orbit is the tell.
            if (attack == GreatBlackKnightAttack.FlailHurl)
            {
                Vector2 hand = GetFlailAnchor();
                Vector2 aim = (player.Center - hand).SafeNormalize(new Vector2(facing, 0f));
                Projectile.NewProjectile(NPC.GetSource_FromThis(), hand + aim * GreatBlackKnightFlail.OrbitRadius,
                    Vector2.Zero, ModContent.ProjectileType<GreatBlackKnightFlail>(), EnemyDamage.Projectile(redFlailDamage),
                    3f, Main.myPlayer, NPC.whoAmI, GreatBlackKnightFlail.OrbitMode, aim.ToRotation());
            }

            NPC.netUpdate = true;
            RequestNetworkSnapshot();
        }

        /// <summary>Server: back to neutral (also a cancel). Clients follow the snapshot this marks.</summary>
        void EndAttack(int cooldown)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }
            NPC.ai[2] = (float)GreatBlackKnightAttack.None;
            NPC.ai[3] = 0f;
            _hurlReleaseTick = -1;
            _skyfallLandTick = -1;
            _attackCooldown = cooldown;
            // A guard-pressure finisher trait set on entry must not leak into later attacks.
            NPC.GetGlobalNPC<tsorcRevampGlobalNPC>().SetActiveAttackDefenseTraits(NPC, AttackDefenseTraits.None);
            NPC.netUpdate = true;
            RequestNetworkSnapshot();
        }

        /// <summary>Server: abandon the attack and the rest of the chain.</summary>
        void CancelAttack()
        {
            _linksLeft = 0;
            _pushInTimer = 0;
            EndAttack(CancelCooldownTicks);
        }

        /// <summary>
        /// One tick of the running attack, from DoCustomTick on EVERY machine: poses, tell dust and sounds play
        /// everywhere; decisions and spawns sit behind netMode checks inside each attack. Only the server ends one.
        /// </summary>
        void TickAttacks(tsorcRevampGlobalNPC globalNPC, bool hasPlayerLOS)
        {
            if (!AttackActive)
            {
                return;
            }
            bool isServer = Main.netMode != NetmodeID.MultiplayerClient;

            // A stagger cancels a windup but never a committed attack.
            bool staggeredInWindup = globalNPC.StaggerTimer > 0 && !InCommittedAttack();
            if (isServer && (!player.active || player.dead || staggeredInWindup))
            {
                CancelAttack();
                return;
            }

            GreatBlackKnightAttack attack = CurrentAttack;
            int timer = AttackTimer;
            switch (attack)
            {
                case GreatBlackKnightAttack.FlailHurl: TickFlailHurl(timer, hasPlayerLOS); break;
                case GreatBlackKnightAttack.SpearThrow: TickSpearThrow(timer, globalNPC, hasPlayerLOS); break;
                case GreatBlackKnightAttack.HomingVolley: TickHomingVolley(timer, hasPlayerLOS); break;
                case GreatBlackKnightAttack.BombThrow: TickBombThrow(timer, hasPlayerLOS); break;
                case GreatBlackKnightAttack.DeathSeal: TickDeathSeal(timer); break;
                case GreatBlackKnightAttack.CrystalStorm: TickCrystalStorm(timer, hasPlayerLOS); break;
                case GreatBlackKnightAttack.Skyfall: TickSkyfall(timer); break;
                case GreatBlackKnightAttack.PhaseTwoTransition: TickPhaseTwoTransition(timer); break;
            }

            // The attack cancelled itself (lost line of sight, nothing to release).
            if (CurrentAttack != attack || AttackTimer != timer)
            {
                return;
            }
            NPC.ai[3] = timer + 1;

            if (!isServer)
            {
                return; // a client that runs ahead holds the recovery pose until the snapshot ends or chains it
            }

            // Recovery: halfway through it a chain with links left moves on; a spent chain plays it out in full.
            int recoveryStart = RecoveryStartTick(attack);
            if (recoveryStart < 0)
            {
                return;
            }
            int recovery = RecoveryTicksOf(attack);
            if (_linksLeft > 0)
            {
                if (timer < recoveryStart + recovery / 2)
                {
                    return;
                }
                // Take a valid link now (no idle), or end the attack and push in.
                GreatBlackKnightAttack link = PickAttack(continuingChain: true);
                if (link != GreatBlackKnightAttack.None)
                {
                    _linksLeft--;
                    BeginAttack(link);
                    return;
                }
                EndAttack(0);
                _pushInTimer = PushInWindowTicks;
                return;
            }

            int spentBonus = Math.Min(SpentRecoveryCap, SpentRecoveryPerLink * (_chainLength - 1));
            if (timer >= recoveryStart + recovery + spentBonus)
            {
                EndAttack(NeutralCooldownTicks);
            }
        }
        #endregion

        #region Brain: attacks
        /// <summary>Aim point: the ~25-tick-old position, AimOvershootPixels past the player so arcs land on and beyond them.</summary>
        Vector2 PredictedAimPoint()
        {
            int direction = 1;
            if (storedPlayerPosition.X < NPC.Center.X)
            {
                direction = -1;
            }
            return new Vector2(storedPlayerPosition.X + AimOvershootPixels * direction, storedPlayerPosition.Y);
        }

        /// <summary>Server: refreshes the predicted aim point mid-tell, on a 25-tick cadence. Clients draw against it.</summary>
        void RefreshStoredPosition()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient || framesSinceStoredPosition < 25 || !player.active || player.dead)
            {
                return;
            }
            framesSinceStoredPosition = 0;
            storedPlayerPosition = player.Center;
            NPC.netUpdate = true;
        }

        void SpawnAttackFlash(Color color)
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
                ModContent.ProjectileType<TelegraphFlash>(), 0, 0, Main.myPlayer, UsefulFunctions.ColorToFloat(color));
        }

        // Blight motes converging on a point: the GBK palette (Wraith with a yellow ember now and then).
        void GatherBlightMotes(Vector2 center, float radius, int chanceDenominator)
        {
            if (Main.dedServ || !Main.rand.NextBool(chanceDenominator))
            {
                return;
            }
            Vector2 offset = Main.rand.NextVector2Circular(radius, radius);
            int dustType = DustID.Wraith;
            if (Main.rand.NextBool(6))
            {
                dustType = DustID.YellowTorch;
            }
            Dust mote = Dust.NewDustPerfect(center + offset, dustType, -offset * 0.08f, 120, default, 1.1f);
            mote.noGravity = true;
        }

        /// <summary>
        /// Flail Hurl (the signature, 240-480px): the head orbits 60px for one 40t revolution, then flies the first
        /// tick its chain points at the target (16 px/t, 476px) and reels back. The arm follows the chain
        /// (GreatBlackKnightFlail drives UpdateFlailProjectileArmPose). Empowered heads pulse and shed embers.
        /// </summary>
        void TickFlailHurl(int timer, bool hasPlayerLOS)
        {
            NPC.velocity.X *= 0.85f;
            if (_hurlReleaseTick >= 0)
            {
                return; // recovery: planted while the head is out
            }
            if (Main.netMode == NetmodeID.MultiplayerClient || timer < HurlOrbitTicks - 1)
            {
                return;
            }

            bool orbiting = TryGetActiveFlail(out Projectile activeFlail)
                && activeFlail.ModProjectile is GreatBlackKnightFlail flail
                && activeFlail.ai[1] == GreatBlackKnightFlail.OrbitMode;
            bool waitedTooLong = timer > HurlOrbitTicks + HurlAlignWaitTicks;
            if (!orbiting || waitedTooLong)
            {
                CancelAttack();
                return;
            }
            if (!hasPlayerLOS)
            {
                return; // keep swinging; the wait cap above gives up
            }

            NPC.TargetClosest(true);
            GreatBlackKnightFlail orbitingFlail = (GreatBlackKnightFlail)activeFlail.ModProjectile;
            if (orbitingFlail.TryReleaseToward(player.Center, HurlLaunchSpeed))
            {
                _hurlReleaseTick = timer;
                SpawnAttackFlash(Color.OrangeRed);
                NPC.netUpdate = true;
                RequestNetworkSnapshot();
            }
        }

        // Owl Father's bow model (the GBK's old throw): arrow-flight spear, gravity 0.1 after 15t, iterative lead.
        const float SpearThrowSpeed = 13f;
        const float SpearGravityDelayTicks = 15f;
        const float SpearGravity = 0.1f;
        const float SpearVerticalLeadFraction = 0.5f;
        const int InterceptIterations = 4;

        void TickSpearThrow(int timer, tsorcRevampGlobalNPC globalNPC, bool hasPlayerLOS)
        {
            // Walks in slowly through the tell, brakes after the release.
            float approach = 1.4f;
            if (timer > SpearReleaseTick)
            {
                approach = 0f;
            }
            KnightHopPlanner.ApproachHorizontalSpeed(NPC, NPC.direction, approach, 0.1f);

            if (timer >= SpearReleaseTick - ThrowFlashLead)
            {
                NPC.knockBackResist = 0f;
            }
            if (timer == SpearReleaseTick - ThrowFlashLead)
            {
                Color flashColor = Color.OrangeRed;
                if (globalNPC.ActiveAttackBypassesShield)
                {
                    flashColor = Color.Red;
                }
                SpawnAttackFlash(flashColor);
                RefreshStoredPosition();
            }
            if (timer != SpearReleaseTick)
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

            NPC.TargetClosest(true);
            Vector2 origin = AttackHandWorld;
            int direction = 1;
            if (player.Center.X < NPC.Center.X)
            {
                direction = -1;
            }
            Vector2 aimAt = player.Center;
            for (int i = 0; i < InterceptIterations; i++)
            {
                float flightTicks = Vector2.Distance(origin, aimAt) / SpearThrowSpeed;
                Vector2 lead = new Vector2(player.velocity.X, player.velocity.Y * SpearVerticalLeadFraction) * flightTicks;
                float fallingTicks = Math.Max(0f, flightTicks - SpearGravityDelayTicks);
                float gravityDrop = SpearGravity * fallingTicks * (fallingTicks + 1f) * 0.5f;
                aimAt = player.Center + lead - new Vector2(0f, gravityDrop);
            }
            aimAt.X += AimOvershootPixels * direction;

            Vector2 velocity = (aimAt - origin).SafeNormalize(new Vector2(direction, 0f)) * SpearThrowSpeed;
            int index = Projectile.NewProjectile(NPC.GetSource_FromThis(), origin, velocity,
                ModContent.ProjectileType<BlackThrowingSpear>(), EnemyDamage.Projectile(redKnightsSpearDamage), 0f,
                Main.myPlayer, ai2: BlackThrowingSpear.ArrowFlightMode);
            tsorcGlobalProjectile.SetDefenseTraits(index, globalNPC.ActiveAttackDefenseTraits);
        }

        // EnemyMoonfuryBomb flies straight for 5t, then gains 0.2 px/t of fall per tick.
        const float BombGravity = 0.2f;
        const float BombGravityDelayTicks = 5f;
        const float BombVerticalLeadFraction = 0.5f;

        /// <summary>The bomb's launch velocity from <paramref name="source"/>: 9-16 px/t by distance, solved against
        /// its real gravity with lead, aimed 45px past. Deterministic, so the pose and the throw agree.</summary>
        Vector2 BombLaunchVelocity(Vector2 source)
        {
            Vector2 target = storedPlayerPosition;
            Vector2 targetVelocity = Vector2.Zero;
            if (NPC.HasValidTarget)
            {
                target = player.Center;
                targetVelocity = player.velocity;
            }
            int direction = 1;
            if (target.X < NPC.Center.X)
            {
                direction = -1;
            }

            float speed = MathHelper.Clamp(8f + Vector2.Distance(source, target) * 0.01f, 9f, 16f);
            Vector2 aimAt = target;
            for (int i = 0; i < InterceptIterations; i++)
            {
                float flightTicks = Vector2.Distance(source, aimAt) / speed;
                Vector2 lead = new Vector2(targetVelocity.X, targetVelocity.Y * BombVerticalLeadFraction) * flightTicks;
                float fallingTicks = Math.Max(0f, flightTicks - BombGravityDelayTicks);
                float gravityDrop = BombGravity * fallingTicks * (fallingTicks + 1f) * 0.5f;
                aimAt = target + lead - new Vector2(0f, gravityDrop);
            }
            aimAt.X += AimOvershootPixels * direction;
            return (aimAt - source).SafeNormalize(new Vector2(direction, 0f)) * speed;
        }

        void TickBombThrow(int timer, bool hasPlayerLOS)
        {
            float approach = 1.4f;
            if (timer > BombReleaseTick)
            {
                approach = 0f;
            }
            KnightHopPlanner.ApproachHorizontalSpeed(NPC, NPC.direction, approach, 0.1f);

            // The fuse is lit on camera the moment the bomb comes out (the pose's appear tick).
            int appearTick = BombReleaseTick - ThrowWhipTicks - ThrowWindupTicks - PropShowAheadTicks;
            if (timer == appearTick)
            {
                SoundEngine.PlaySound(UsefulFunctions.BombFuse with { Volume = 0.6f }, NPC.Center);
            }
            if (timer >= BombReleaseTick - ThrowFlashLead)
            {
                NPC.knockBackResist = 0f;
            }
            if (timer == BombReleaseTick - ThrowFlashLead)
            {
                SpawnAttackFlash(Color.OrangeRed);
                RefreshStoredPosition();
            }
            if (timer != BombReleaseTick)
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

            NPC.TargetClosest(true);
            Projectile.NewProjectile(NPC.GetSource_FromThis(), AttackHandWorld, BombLaunchVelocity(AttackHandWorld),
                ModContent.ProjectileType<EnemyMoonfuryBomb>(), EnemyDamage.Projectile(redKnightsSpearDamage), 0f, Main.myPlayer);
        }

        /// <summary>Black Knight's Homing Volley: a single lofted crystal at 60, a faster flatter pair at 110, both
        /// from the back palm. Blight palette by source type.</summary>
        void TickHomingVolley(int timer, bool hasPlayerLOS)
        {
            if (timer <= HomingFirstBurstTick)
            {
                GatherBlightMotes(CastHandWorld, 14f, 2);
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
                Vector2 velocity = UsefulFunctions.BallisticTrajectory(source, player.Center, projectileSpeed, arc,
                    highAngle: true, fallback: true);
                if (firstBurst)
                {
                    velocity += player.velocity;
                }
                velocity = velocity.RotatedBy(angle);
                // Never fire backwards through his own body.
                if (Math.Sign(velocity.X) != NPC.direction)
                {
                    continue;
                }
                Projectile.NewProjectile(NPC.GetSource_FromThis(), source, velocity,
                    ModContent.ProjectileType<EnemyBlackKnightHomingCrystal>(), EnemyDamage.Projectile(redMagicDamage), 0f, Main.myPlayer);
            }
        }

        /// <summary>Death Seal (ex-Ultrakill): a 240px ring of bone-white motes collapsing inward for 100t, the front
        /// palm swings onto the locked aim at the flash, then a 35t stream of death skulls and black breath from it.</summary>
        void TickDeathSeal(int timer)
        {
            if (timer < DeathSealTelegraphTicks)
            {
                NPC.knockBackResist = 0f;
                NPC.velocity.X *= 0.85f;
                if (!Main.dedServ)
                {
                    float collapse = MathHelper.Clamp(1f - timer / (float)DeathSealTelegraphTicks, 0f, 1f);
                    UsefulFunctions.DustRing(NPC.Center, 240f * collapse, DustID.BoneTorch, 48, 4);
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
                return; // recovery
            }

            NPC.velocity.X *= 0.25f;
            SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.8f, PitchVariance = 1f }, NPC.Center);
            SoundEngine.PlaySound(SoundID.Item69 with { Volume = 0.9f, PitchVariance = 2f }, NPC.Center);
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            // ai[0] stays 0: the GBK's own projectile modes (the Black Knight fires 1 / 2).
            Vector2 source = AttackHandWorld;
            Vector2 aim = PredictedAimPoint();
            Vector2 skullVelocity = UsefulFunctions.BallisticTrajectory(source, aim, 2f, fallback: true)
                + Main.rand.NextVector2Circular(1, 5);
            Projectile.NewProjectile(NPC.GetSource_FromThis(), source, skullVelocity,
                ModContent.ProjectileType<EnemySpellSuddenDeathStrike>(), EnemyDamage.Projectile(redKnightsGreatDamage), 0f, Main.myPlayer);
            Vector2 breathVelocity = UsefulFunctions.BallisticTrajectory(source, aim, 2f, fallback: true)
                + Main.rand.NextVector2Circular(-5, 5);
            Projectile.NewProjectile(NPC.GetSource_FromThis(), source, breathVelocity,
                ModContent.ProjectileType<EnemyBlackCursedBreath>(), EnemyDamage.Projectile(redKnightsGreatDamage), 0f, Main.myPlayer);
        }

        /// <summary>Shadow Crystal Storm (below 1/3): both palms gather, five homing crystals in a 60-degree fan from the
        /// back palm. Replaces the old global-clock volley that fired mid-anything.</summary>
        void TickCrystalStorm(int timer, bool hasPlayerLOS)
        {
            NPC.velocity.X *= 0.85f;
            if (timer < CrystalStormReleaseTick)
            {
                Vector2 palms = (PuppetHandPosition + PuppetBackHandPosition) * 0.5f;
                GatherBlightMotes(palms, 64f, 2);
                if (timer == CrystalStormCommitTick)
                {
                    SpawnAttackFlash(new Color(150, 150, 120));
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
            Vector2 baseVelocity = UsefulFunctions.BallisticTrajectory(source, player.Center, 10f, fallback: true) + player.velocity / 2f;
            for (int i = 0; i < CrystalStormCount; i++)
            {
                float angle = (i - (CrystalStormCount - 1) / 2f) * (CrystalStormSpread / (CrystalStormCount - 1));
                Projectile.NewProjectile(NPC.GetSource_FromThis(), source, baseVelocity.RotatedBy(angle),
                    ModContent.ProjectileType<EnemyBlackKnightHomingCrystal>(), EnemyDamage.Projectile(redMagicDamage), 0f, Main.myPlayer);
            }
        }

        // Client-side one-shots for Skyfall's apex puff and landing impact (the server's flags arrive later).
        bool _skyfallApexShown;
        bool _skyfallImpactShown;

        /// <summary>
        /// Skyfall Flail: a 30t tell with the fist raised and Blight gathering at the feet, a leap solved to land 40px
        /// past the target (airtime 36-60t, apex capped under the ceiling), one apex re-aim of up to 160px toward
        /// where the target is now (the double-jump puff is its cue), and a 96px Blight burst on landing.
        /// </summary>
        void TickSkyfall(int timer)
        {
            bool isServer = Main.netMode != NetmodeID.MultiplayerClient;
            if (timer < SkyfallTellTicks)
            {
                NPC.velocity.X *= 0.82f;
                GatherBlightMotes(NPC.Bottom, 40f, 1);
                if (timer == 0)
                {
                    _skyfallApexShown = false;
                    _skyfallImpactShown = false;
                    SoundEngine.PlaySound(SoundID.Item7 with { Volume = 0.8f, Pitch = -0.4f }, NPC.Center);
                }
                if (timer == SkyfallFlashTick)
                {
                    SpawnAttackFlash(Color.OrangeRed);
                }
                return;
            }

            if (timer == SkyfallTellTicks)
            {
                SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.9f, Pitch = -0.2f }, NPC.Center);
                if (isServer)
                {
                    LaunchSkyfall();
                }
                return;
            }

            if (_skyfallLandTick >= 0)
            {
                NPC.velocity.X *= 0.8f; // planted recovery
                return;
            }

            bool airborne = NPC.velocity.Y != 0f;
            int airTicks = timer - SkyfallTellTicks;

            // Apex: one re-aim toward where the target is now, with a double-jump puff under the feet as its cue.
            if (airborne && NPC.velocity.Y >= 0f && !_skyfallApexShown)
            {
                _skyfallApexShown = true;
                if (!Main.dedServ)
                {
                    for (int i = 0; i < 20; i++)
                    {
                        Vector2 puff = new Vector2(Main.rand.NextFloat(-3f, 3f), Main.rand.NextFloat(0.5f, 2f));
                        Dust dust = Dust.NewDustPerfect(NPC.Bottom, DustID.Smoke, puff, 100, Color.Black, 1.3f);
                        dust.noGravity = true;
                    }
                }
                // Server: the fall time from the apex to the target's floor sets how much horizontal speed the
                // +/-160px correction needs.
                if (isServer && !_skyfallApexDone && player.active && !player.dead)
                {
                    _skyfallApexDone = true;
                    int direction = 1;
                    if (player.Center.X < NPC.Center.X)
                    {
                        direction = -1;
                    }
                    float fallDistance = Math.Max(16f, player.Bottom.Y - NPC.Bottom.Y);
                    float fallTicks = Math.Max(8f, (float)Math.Sqrt(2f * fallDistance / NpcGravity));
                    float predictedLandingX = NPC.Center.X + NPC.velocity.X * fallTicks;
                    float desiredLandingX = player.Center.X + SkyfallOvershootPixels * direction;
                    float correction = MathHelper.Clamp(desiredLandingX - predictedLandingX, -SkyfallApexCorrection, SkyfallApexCorrection);
                    NPC.velocity.X += correction / fallTicks;
                    NPC.netUpdate = true;
                    RequestNetworkSnapshot();
                }
            }

            bool landed = airTicks > 3 && !airborne;
            bool bail = airTicks > SkyfallMaxAirTicks;
            if (!landed && !bail)
            {
                return;
            }

            if (!_skyfallImpactShown)
            {
                _skyfallImpactShown = true;
                SoundEngine.PlaySound(SoundID.DeerclopsRubbleAttack with { Volume = 0.9f }, NPC.Center);
                UsefulFunctions.ScreenShake(NPC.Center, 8f, 18, distanceFalloff: 900f);
            }
            if (isServer)
            {
                _skyfallLandTick = timer;
                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Bottom - new Vector2(0f, 8f), Vector2.Zero,
                    ModContent.ProjectileType<GreatBlackKnightSkyfallBurst>(), EnemyDamage.Projectile(redKnightsGreatDamage), 4f, Main.myPlayer);
                NPC.netUpdate = true;
                RequestNetworkSnapshot();
            }
        }

        // Server: airtime grows with distance (36-60t) so far leaps arc higher, and is shortened until the apex fits
        // under the ceiling. Solved against vanilla NPC gravity: vy = (dy - g*T^2/2) / T.
        void LaunchSkyfall()
        {
            int direction = 1;
            if (player.Center.X < NPC.Center.X)
            {
                direction = -1;
            }
            NPC.direction = direction;
            NPC.spriteDirection = direction;

            Vector2 landing = new Vector2(player.Center.X + SkyfallOvershootPixels * direction, player.Bottom.Y);
            float dx = landing.X - NPC.Center.X;
            float dy = landing.Y - NPC.Bottom.Y;
            float airTicks = MathHelper.Clamp(Math.Abs(dx) / 9f, 36f, 60f);

            float clearance = Math.Max(48f, CeilingClearance() - 8f);
            for (int i = 0; i < 6; i++)
            {
                float verticalSpeed = (dy - 0.5f * NpcGravity * airTicks * airTicks) / airTicks;
                float apexHeight = verticalSpeed * verticalSpeed / (2f * NpcGravity);
                if (verticalSpeed >= 0f || apexHeight <= clearance)
                {
                    break;
                }
                airTicks *= 0.85f;
            }

            float vx = dx / airTicks;
            float vy = (dy - 0.5f * NpcGravity * airTicks * airTicks) / airTicks;
            NPC.velocity = new Vector2(vx, MathHelper.Clamp(vy, -14f, -3f));
            NPC.netUpdate = true;
            RequestNetworkSnapshot();
        }

        /// <summary>Phase 2's 40t beat: a Blight burst and a screen shake (no roar), hyper-armoured, no damage.</summary>
        void TickPhaseTwoTransition(int timer)
        {
            NPC.velocity.X *= 0.8f;
            if (timer != 0 || Main.dedServ)
            {
                return;
            }
            for (int i = 0; i < 50; i++)
            {
                int dustType = DustID.Wraith;
                if (i % 2 == 0)
                {
                    dustType = DustID.Smoke;
                }
                Vector2 velocity = Main.rand.NextVector2CircularEdge(7f, 7f) * Main.rand.NextFloat(0.5f, 1f);
                Dust dust = Dust.NewDustPerfect(NPC.Center, dustType, velocity, 100, Color.Black, 1.6f);
                dust.noGravity = true;
            }
            UsefulFunctions.ScreenShake(NPC.Center, 6f, 20, distanceFalloff: 900f);
        }
        #endregion

        #region Attack poses
        // ── Brain attack poses (Black Knight's, retimed to the GBK clock) ─────────────────────────────────
        // Angles are FACING space: 0 = ahead, negative = up, mirrored for a left-facing puppet; RedKnightPuppet.SwingPoseFor
        // turns a held prop's facing into the swing pose. Every held prop is in the FRONT hand; the back arm is a palm.
        //   Spear Throw (R 60): spear appears R-40 | carry | 24t javelin draw-back from R-30 | 6t cubic whip | thrown at R.
        //   Bomb Throw (R 60): bomb at the hip from R-40, fuse lit | 24t wind-up, overhand when the solved arc climbs
        //     steeper than 25 degrees, otherwise underhand | 6t whip | thrown at R.
        //   Homing Volley / Crystal Storm / Death Seal: Black Knight's palm stages (raise 24t, push 6t, hold, lower 12t).
        //   Flail Hurl: no prop pose; the flail projectile drives the composite arm along its chain.
        //   Skyfall: the empty front fist raised overhead over 14t and carried through the leap; slammed down-forward
        //     over 6t on landing; lowered through the recovery.
        //   Phase 2 beat: both arms flung out wide for the burst, lowered over the last 16t.
        const int PropShowAheadTicks = 10;
        const int ThrowWindupTicks = 24;
        const int ThrowWhipTicks = 6;
        const float SpearCarryFacing = 0.2f;
        const float ThrowArmDrawBack = 2.565f;
        const float ThrowSpearLift = 0.35f;
        const float ThrowGrip = 0.6f;
        const float ThrowCockRadians = 1.6f;
        const float BombHipFacing = 1.0f;
        const float BombUnderhandFacing = 2.1f;
        const float BombOverhandAboveFacing = -0.45f;
        const int PalmRaiseTicks = 24;
        const int PalmPushTicks = 6;
        const int PalmHoldTicks = 14;
        const int PalmLowerTicks = 12;
        const float PalmHangFacing = 1.4f;
        const float PalmReadyLift = 0.45f;
        const float PalmMinimumFacing = -1.5f;
        const float PalmMaximumFacing = 1.0f;
        const float FrontOverheadFacing = -0.8f;
        const float BackOverheadFacing = -0.95f;
        const float SkyfallRaisedFacing = -1.3f;
        const float SkyfallSlamFacing = 0.9f;
        const int SkyfallRaiseTicks = 14;
        const int SkyfallSlamTicks = 6;
        const float TransitionSpreadFacing = -0.4f;

        BlackKnightProp _frontProp;
        float _poseFacing;
        float _poseArmOffset;
        float _poseGrip = ThrowGrip;
        bool _bombFuseLit;
        float? _backPalmFacing;
        float? _frontFreeFacing;
        PuppetWeapon _spearPropWeapon;
        PuppetWeapon _bombPropWeapon;

        /// <summary>World position of the front hand, captured before each brain tick. Throws leave from here.</summary>
        Vector2 AttackHandWorld { get; set; }

        /// <summary>World position of the back palm, captured with AttackHandWorld. Palm spells leave from here.</summary>
        Vector2 CastHandWorld { get; set; }

        // The thrown spear is drawn from its own projectile sprite (SpearDrawTexturePath), so hand and flight match.
        PuppetWeapon SpearPropWeapon => _spearPropWeapon ??= new PuppetWeapon(ItemID.Spear, archetype: WeaponArchetype.Halberd);
        // The bomb is drawn in PostDraw; a sprite-hidden identity keeps the composite arm posed under it.
        PuppetWeapon BombPropWeapon => _bombPropWeapon ??= new PuppetWeapon(ItemID.Grenade, hideHeldSprite: true);

        bool PoseActive => Phase == AttackPhase.Custom && AttackActive;
        bool FrontHandHoldsProp => PoseActive && _frontProp != BlackKnightProp.None;
        // During the hurl the arm follows the chain (UpdateFlailProjectileArmPose), which needs the composite arm path.
        bool HurlArmActive => PoseActive && CurrentAttack == GreatBlackKnightAttack.FlailHurl;
        protected override bool UseCompositeArmForAdditionalPhase => FrontHandHoldsProp || HurlArmActive;
        // Props use the composite arm; the flail combos keep Black Ninja's proven settings (both off).
        protected override bool UseCompositeArmSwing => FrontHandHoldsProp;
        protected override bool MirrorMeleeSwingRotationByFacing => FrontHandHoldsProp;
        // The Custom phase is entered with the flail as its pose weapon; it stays hidden while the hand is empty.
        protected override bool WeaponSheathed => PoseActive && !FrontHandHoldsProp && !HurlArmActive;
        protected override float? FreeBackArmFacingAngle => PoseActive ? _backPalmFacing : null;
        protected override float? FreeFrontArmFacingAngle => PoseActive && !FrontHandHoldsProp && !HurlArmActive ? _frontFreeFacing : null;
        protected override float MeleeCompositeArmRotationOffset => FrontHandHoldsProp ? _poseArmOffset : 0f;

        protected override float? CustomWeaponRotation
        {
            get
            {
                if (!FrontHandHoldsProp)
                {
                    return null;
                }
                return RedKnightPuppet.SwingPoseFor(_poseFacing);
            }
        }

        protected override bool DrawWeaponAsSpear => FrontHandHoldsProp && _frontProp == BlackKnightProp.Spear;
        protected override bool UseAuthoredSpearGrip => true;
        protected override bool MirrorSpearRotationByFacing => true;
        protected override string SpearDrawTexturePath => UsefulFunctions.RefactorableFilepath(typeof(BlackThrowingSpear));
        // BlackThrowingSpear.png measured (Black Knight): 14x84, tip straight up, rows 0 and 83 at x 6-7.
        protected override Vector2 SpearTextureSize => new Vector2(14f, 84f);
        protected override Vector2 SpearHeadNorm => new Vector2(0.5f, 0.5f / 84f);
        protected override Vector2 SpearBaseNorm => new Vector2(0.5f, 83.5f / 84f);
        protected override float SpearDrawRotationOffset => MathHelper.PiOver4;

        protected override float? OverrideSpearGrip(float swingClockT)
        {
            if (DrawWeaponAsSpear)
            {
                return _poseGrip;
            }
            return null;
        }

        /// <summary>Everything the brain-attack pose reads, as plain synced values, so SwingPreview can drive the same
        /// function offline. Aims are facing-space radians.</summary>
        public struct GreatBlackKnightPoseInput
        {
            public int Attack;
            public int Timer;
            public int SkyfallLandTick;
            public float ThrowAim;
            public float BombAim;
            public float CastAim;
        }

        /// <summary>One tick of the brain-attack pose (the Black Knight's shape).</summary>
        public struct GreatBlackKnightPose
        {
            public BlackKnightProp FrontProp;
            public float Facing;
            public float ArmOffset;
            public float Grip;
            public bool FuseLit;
            public bool ThrowDrawBack;
            public float? BackPalmFacing;
            public float? FrontFreeFacing;
        }

        /// <summary>World-facing half of the pose: solves this tick's aims from synced state. Runs on every machine.</summary>
        void UpdateAttackPose()
        {
            GreatBlackKnightPoseInput input = new GreatBlackKnightPoseInput
            {
                Attack = (int)CurrentAttack,
                Timer = AttackTimer,
                SkyfallLandTick = _skyfallLandTick,
            };

            // NPC.Center stands in for the hand so the aim does not feed back through the arm pose it sets.
            input.ThrowAim = RedKnightPuppet.FacingAngleOf(PredictedAimPoint() - NPC.Center, NPC.direction);
            input.BombAim = RedKnightPuppet.FacingAngleOf(BombLaunchVelocity(NPC.Center), NPC.direction);
            Vector2 castTarget = storedPlayerPosition;
            if (castTarget == Vector2.Zero && NPC.HasValidTarget)
            {
                castTarget = player.Center;
            }
            input.CastAim = RedKnightPuppet.FacingAngleOf(castTarget - NPC.Center, NPC.direction);

            GreatBlackKnightPose pose = ResolveAttackPose(input);
            _frontProp = pose.FrontProp;
            _poseFacing = pose.Facing;
            _poseArmOffset = pose.ArmOffset;
            _poseGrip = pose.Grip;
            _bombFuseLit = pose.FuseLit;
            _backPalmFacing = pose.BackPalmFacing;
            _frontFreeFacing = pose.FrontFreeFacing;

            // Blight glints at the raised spear tip through the draw-back, every other tick.
            if (pose.ThrowDrawBack && !Main.dedServ && input.Timer % 2 == 0)
            {
                Dust glint = Dust.NewDustPerfect(PuppetSpearTipPosition, DustID.Wraith,
                    Main.rand.NextVector2Circular(0.5f, 0.5f), 80, default, Main.rand.NextFloat(0.9f, 1.3f));
                glint.noGravity = true;
            }
        }

        // Hang -> ready (SmoothStep) | cubic push onto the aim at release | hold to holdEnd | SmoothStep back down.
        static float StagePalm(int timer, int release, int holdEnd, float ready, float aim)
        {
            int pushStart = release - PalmPushTicks;
            int raiseStart = pushStart - PalmRaiseTicks;
            int lowerStart = holdEnd + PalmHoldTicks;
            if (timer < raiseStart)
            {
                return PalmHangFacing;
            }
            if (timer < pushStart)
            {
                float raise = (timer - raiseStart + 1f) / PalmRaiseTicks;
                return MathHelper.SmoothStep(PalmHangFacing, ready, raise);
            }
            if (timer < release)
            {
                float push = (timer - pushStart + 1f) / PalmPushTicks;
                return MathHelper.Lerp(ready, aim, push * push * push);
            }
            if (timer < lowerStart)
            {
                return aim;
            }
            float lower = MathHelper.Clamp((timer - lowerStart + 1f) / PalmLowerTicks, 0f, 1f);
            return MathHelper.SmoothStep(aim, PalmHangFacing, lower);
        }

        /// <summary>
        /// Picks the front-hand prop with its facing angle and spear grip, plus the free arms' palm angles, from the
        /// attack and its clock. Timings are in the block comment above the pose constants. Pure: public and static
        /// so SwingPreview renders exactly the poses the game plays (--gbk-poses).
        /// </summary>
        public static GreatBlackKnightPose ResolveAttackPose(GreatBlackKnightPoseInput input)
        {
            int timer = input.Timer;
            GreatBlackKnightAttack attack = (GreatBlackKnightAttack)input.Attack;
            GreatBlackKnightPose pose = new GreatBlackKnightPose
            {
                FrontProp = BlackKnightProp.None,
                Facing = SpearCarryFacing,
                Grip = ThrowGrip,
            };
            float castAim = MathHelper.Clamp(input.CastAim, PalmMinimumFacing, PalmMaximumFacing);

            switch (attack)
            {
                case GreatBlackKnightAttack.SpearThrow:
                {
                    int release = SpearReleaseTick;
                    int whipStart = release - ThrowWhipTicks;
                    int windupStart = whipStart - ThrowWindupTicks;
                    int appearTick = windupStart - PropShowAheadTicks;
                    if (timer < appearTick || timer >= release)
                    {
                        return pose;
                    }

                    float aim = input.ThrowAim;
                    float cocked = aim - ThrowSpearLift;
                    pose.FrontProp = BlackKnightProp.Spear;
                    if (timer < windupStart)
                    {
                        return pose;
                    }
                    if (timer < whipStart)
                    {
                        float windup = (timer - windupStart + 1f) / ThrowWindupTicks;
                        pose.Facing = MathHelper.SmoothStep(SpearCarryFacing, cocked, windup);
                        pose.ArmOffset = MathHelper.SmoothStep(0f, -ThrowArmDrawBack, windup);
                        pose.ThrowDrawBack = true;
                        return pose;
                    }

                    // Cubic ease-in: slow out of the cock and fastest on the release frame.
                    float whip = (timer - whipStart + 1f) / ThrowWhipTicks;
                    float eased = whip * whip * whip;
                    pose.Facing = MathHelper.Lerp(cocked, aim, eased);
                    pose.ArmOffset = MathHelper.Lerp(-ThrowArmDrawBack, 0f, eased);
                    return pose;
                }

                case GreatBlackKnightAttack.BombThrow:
                {
                    int release = BombReleaseTick;
                    int whipStart = release - ThrowWhipTicks;
                    int windupStart = whipStart - ThrowWindupTicks;
                    int appearTick = windupStart - PropShowAheadTicks;
                    if (timer < appearTick || timer >= release)
                    {
                        return pose;
                    }

                    float aim = input.BombAim;
                    float cocked = BombUnderhandFacing;
                    if (aim < BombOverhandAboveFacing)
                    {
                        cocked = aim - ThrowCockRadians;
                    }
                    pose.FrontProp = BlackKnightProp.Bomb;
                    pose.FuseLit = true;
                    if (timer < windupStart)
                    {
                        pose.Facing = BombHipFacing;
                    }
                    else if (timer < whipStart)
                    {
                        float windup = (timer - windupStart + 1f) / ThrowWindupTicks;
                        pose.Facing = MathHelper.SmoothStep(BombHipFacing, cocked, windup);
                    }
                    else
                    {
                        float whip = (timer - whipStart + 1f) / ThrowWhipTicks;
                        float eased = whip * whip * whip;
                        pose.Facing = MathHelper.Lerp(cocked, aim, eased);
                    }
                    return pose;
                }

                case GreatBlackKnightAttack.HomingVolley:
                {
                    pose.BackPalmFacing = StagePalm(timer, HomingFirstBurstTick, HomingSecondBurstTick,
                        castAim - PalmReadyLift, castAim);
                    return pose;
                }

                case GreatBlackKnightAttack.CrystalStorm:
                {
                    // Both palms gather overhead, then the back palm pushes the volley out while the front stays up.
                    pose.BackPalmFacing = StagePalm(timer, CrystalStormReleaseTick, CrystalStormReleaseTick,
                        BackOverheadFacing, castAim);
                    pose.FrontFreeFacing = StagePalm(timer, CrystalStormReleaseTick, CrystalStormReleaseTick,
                        FrontOverheadFacing, FrontOverheadFacing);
                    return pose;
                }

                case GreatBlackKnightAttack.DeathSeal:
                {
                    // Both palms gather overhead; the front palm swings onto the locked aim at the flash and streams.
                    int streamEnd = DeathSealTelegraphTicks + DeathSealFireTicks;
                    float gather = MathHelper.SmoothStep(0f, 1f, (timer + 1f) / PalmRaiseTicks);
                    float lower = MathHelper.SmoothStep(0f, 1f, (timer - streamEnd + 1f) / PalmLowerTicks);
                    pose.BackPalmFacing = MathHelper.Lerp(PalmHangFacing, BackOverheadFacing, gather * (1f - lower));
                    float frontRaised = MathHelper.Lerp(PalmHangFacing, FrontOverheadFacing, gather);
                    if (timer >= DeathSealFlashTick)
                    {
                        float swing = MathHelper.SmoothStep(0f, 1f, (timer - DeathSealFlashTick + 1f) / 12f);
                        frontRaised = MathHelper.Lerp(FrontOverheadFacing, castAim, swing);
                    }
                    pose.FrontFreeFacing = MathHelper.Lerp(frontRaised, PalmHangFacing, lower);
                    return pose;
                }

                case GreatBlackKnightAttack.Skyfall:
                {
                    // Fist raised over the tell and carried through the leap; slammed down-forward on landing.
                    float raise = MathHelper.SmoothStep(0f, 1f, (timer + 1f) / SkyfallRaiseTicks);
                    float front = MathHelper.Lerp(PalmHangFacing, SkyfallRaisedFacing, raise);
                    int land = input.SkyfallLandTick;
                    if (land >= 0 && timer >= land)
                    {
                        float slam = MathHelper.Clamp((timer - land + 1f) / SkyfallSlamTicks, 0f, 1f);
                        front = MathHelper.Lerp(SkyfallRaisedFacing, SkyfallSlamFacing, slam * slam * slam);
                        float lowerProgress = MathHelper.SmoothStep(0f, 1f, (timer - land - SkyfallSlamTicks) / (float)(SkyfallRecoveryTicks - SkyfallSlamTicks));
                        front = MathHelper.Lerp(front, PalmHangFacing, lowerProgress);
                    }
                    pose.FrontFreeFacing = front;
                    pose.BackPalmFacing = MathHelper.Lerp(PalmHangFacing, 0.6f, raise);
                    return pose;
                }

                case GreatBlackKnightAttack.PhaseTwoTransition:
                {
                    // Arms flung out wide on the burst, lowered over the last 16t.
                    float open = MathHelper.SmoothStep(0f, 1f, (timer + 1f) / 6f);
                    float close = MathHelper.SmoothStep(0f, 1f, (timer - (TransitionTicks - 16f)) / 16f);
                    float spread = open * (1f - close);
                    pose.FrontFreeFacing = MathHelper.Lerp(PalmHangFacing, TransitionSpreadFacing, spread);
                    pose.BackPalmFacing = MathHelper.Lerp(PalmHangFacing, -TransitionSpreadFacing - 1.6f, spread);
                    return pose;
                }
            }

            return pose;
        }
        #endregion

        #region Hit logic and sync
        // Evasion only from neutral: a dodge roll must never cut a tell or a recovery (the punish window) short.
        public override void OnHitByItem(Player player, Item item, NPC.HitInfo hit, int damageDone)
        {
            base.OnHitByItem(player, item, hit, damageDone);
            if (!AttackActive)
            {
                tsorcRevampAIs.EvasiveOnHit(NPC, true);
            }
        }

        public override void OnHitByProjectile(Projectile projectile, NPC.HitInfo hit, int damageDone)
        {
            base.OnHitByProjectile(projectile, hit, damageDone);
            if (!AttackActive)
            {
                tsorcRevampAIs.EvasiveOnHit(NPC, projectile.DamageType == DamageClass.Melee);
            }
        }

        /// <summary>Poise stagger: the base dropped the Custom phase; drop the brain attack and the rest of the chain.
        /// Only reachable from a windup — a committed attack is hyper-armoured.</summary>
        public override void OnStagger(NPC npc)
        {
            base.OnStagger(npc);
            if (Main.netMode != NetmodeID.MultiplayerClient && AttackActive)
            {
                CancelAttack();
            }
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            base.SendExtraAI(writer);
            writer.Write(storedPlayerPosition.X);
            writer.Write(storedPlayerPosition.Y);
            writer.Write((short)_hurlReleaseTick);
            writer.Write((short)_skyfallLandTick);
            writer.Write(_phaseTwo);
            writer.Write((byte)_linksLeft);
            writer.Write((short)_pushInTimer);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            base.ReceiveExtraAI(reader);
            storedPlayerPosition = new Vector2(reader.ReadSingle(), reader.ReadSingle());
            _hurlReleaseTick = reader.ReadInt16();
            _skyfallLandTick = reader.ReadInt16();
            _phaseTwo = reader.ReadBoolean();
            _linksLeft = reader.ReadByte();
            _pushInTimer = reader.ReadInt16();
        }
        #endregion

        #region AI
        public override void AI()
        {
            tsorcRevampGlobalNPC globalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            framesSinceStoredPosition++;
            bool isServer = Main.netMode != NetmodeID.MultiplayerClient;

            // Phase 2 latches once, server-side; HP only falls, and the flag rides SendExtraAI.
            if (isServer && !_phaseTwo && NPC.life <= NPC.lifeMax / 2)
            {
                _phaseTwo = true;
                NPC.netUpdate = true;
            }

            // Server, from neutral: a chain link, a push-in pick, or a fresh chain, hosted in a Custom phase. The flail
            // combos are picked by the base from the same neutral, inside 200px.
            bool neutral = Phase == AttackPhase.Idle || Phase == AttackPhase.CasualStroll || Phase == AttackPhase.ClosingDistance;
            if (isServer && neutral && !AttackActive)
            {
                if (_attackCooldown > 0)
                {
                    _attackCooldown--;
                }
                // Momentum runs out if the push-in finds nothing in its window.
                if (_linksLeft > 0)
                {
                    _pushInTimer--;
                    if (_pushInTimer <= 0)
                    {
                        _linksLeft = 0;
                        _pushInTimer = 0;
                        _attackCooldown = Math.Max(_attackCooldown, CancelCooldownTicks);
                        NPC.netUpdate = true;
                    }
                }

                if (NPC.HasValidTarget && !InFinisherPunishWindow && TryStartFromNeutral(globalNPC))
                {
                    StartCustomAttack(AttackPhaseCapTicks, MeleeWeaponItemType, swingPose: false);
                }
            }

            base.AI();

            // A stagger or the safety cap ended the Custom phase under a live attack: drop the attack.
            if (isServer && AttackActive && Phase != AttackPhase.Custom)
            {
                CancelAttack();
            }

            // A throw swapped the front hand; give the flail combos their flail back.
            if (!PoseActive)
            {
                _frontProp = BlackKnightProp.None;
                EquipWeapon(PuppetHandSlot.Front, DefaultFrontWeapon);
            }

            if (PoseActive)
            {
                // The base cleared both flags for Custom at the end of its tick; the brain's tell/commit split wins.
                globalNPC.AttackCommitted = InCommittedAttack();
                globalNPC.AttackTelegraphing = InAttackTelegraph();
                DebugAttackLabel = CurrentAttackDebugName;
            }
            else
            {
                DebugAttackLabel = null;
                if (_linksLeft > 0)
                {
                    DebugAttackLabel = $"Push-in [+{_linksLeft}] {_pushInTimer}t";
                }
            }

            TickPhaseTwoPresence();
            Lighting.AddLight(NPC.Center, new Color(150, 150, 170).ToVector3() * 0.35f);
            if (!Main.dedServ && Main.rand.NextBool(1500))
            {
                SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/DarkSouls/ominous-creature2") with { Volume = 0.8f }, NPC.Center);
            }
        }

        protected override void RunMovementAI(float speedMult)
        {
            // The brain drives velocity for the whole attack.
            if (Phase == AttackPhase.Custom)
            {
                return;
            }

            // Push-in: a longer stride while a chain hunts for its next valid link.
            float stride = speedMult;
            if (_linksLeft > 0 && _pushInTimer > 0)
            {
                stride *= PushInSpeedMult;
            }
            // attackRange is SF4's engage/re-aggro radius, not a weapon reach: he throws and casts out to ~900px.
            SmartFighter4AI.Run(NPC, topSpeed: TopSpeed * stride, acceleration: Acceleration,
                doorBreakingDamage: 4, attackRange: 700f);
        }

        protected override void DoCustomTick(int ticksRemaining)
        {
            if (!NPC.HasValidTarget)
            {
                return;
            }

            // Pose from the same clock the brain is about to process, so a release tick shows the release pose. The hand
            // positions are last tick's (the arm is posed after this); one tick is invisible.
            AttackHandWorld = PuppetHandPosition;
            CastHandWorld = PuppetBackHandPosition;
            UpdateAttackPose();

            // This tick's prop in the front hand, as the displayed item. The base re-asserts the Custom pose weapon (the
            // flail) before DoCustomTick every tick, so this runs after it and wins.
            PuppetWeapon frontWeapon = DefaultFrontWeapon;
            int displayType = MeleeWeaponItemType;
            if (_frontProp == BlackKnightProp.Spear)
            {
                frontWeapon = SpearPropWeapon;
                displayType = ItemID.Spear;
            }
            else if (_frontProp == BlackKnightProp.Bomb)
            {
                frontWeapon = BombPropWeapon;
                displayType = ItemID.Grenade;
            }
            EquipWeapon(PuppetHandSlot.Front, frontWeapon);
            SetDisplayWeapon(displayType, swing: false);

            tsorcRevampGlobalNPC globalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            bool hasPlayerLOS = Collision.CanHitLine(NPC.Center, 1, 1, player.Center, 1, 1);
            bool netUpdateBefore = NPC.netUpdate;
            TickAttacks(globalNPC, hasPlayerLOS);
            if (!netUpdateBefore && NPC.netUpdate)
            {
                RequestNetworkSnapshot();
            }

            // EndAttack ran inside the tick: end the Custom phase on this same tick (the base decrements to 0).
            if (!AttackActive)
            {
                PhaseTimer = 1;
            }
        }
        #endregion

        #region Phase 2 presence
        // ── Phase 2 (proposal §6j) ──────────────────────────────────────────────────────────────────────
        // Ghost echoes: the puppet's own fractal afterimages of the armoured body (NOT HasGhostAfterimages, which draws
        // the old NPC sprite sheet and changes knockback), held on every tick at low opacity and faded in over 30t,
        // with spectral wisps drifting off the body. The main body is drawn untouched, so it stays fully opaque.
        // Blight touch: 15 buildup per contact, checked on the touched player's own machine, once per 60t, never while
        // the player is immune (a roll through him is safe). Black dust trails behind him while he moves.
        const float GhostEchoOpacity = 0.16f;
        const int GhostFadeInTicks = 30;
        const int BlightTouchBuildup = 15;
        const int BlightTouchCooldownTicks = 60;
        int _phaseTwoVisibleTicks;                  // local, all machines
        int _blightTouchCooldown;                   // local: guards this machine's own player

        protected override float AfterimageOpacity
        {
            get
            {
                if (!_phaseTwo)
                {
                    return base.AfterimageOpacity;
                }
                float fadeIn = MathHelper.Clamp(_phaseTwoVisibleTicks / (float)GhostFadeInTicks, 0f, 1f);
                return GhostEchoOpacity * fadeIn;
            }
        }

        void TickPhaseTwoPresence()
        {
            if (!_phaseTwo)
            {
                return;
            }
            _phaseTwoVisibleTicks++;
            AfterimageTicks = Math.Max(AfterimageTicks, 2);

            if (_blightTouchCooldown > 0)
            {
                _blightTouchCooldown--;
            }
            if (Main.dedServ)
            {
                return;
            }

            Player local = Main.LocalPlayer;
            bool transitionBeat = CurrentAttack == GreatBlackKnightAttack.PhaseTwoTransition;
            bool touching = local.active && !local.dead && !local.immune && local.Hitbox.Intersects(NPC.Hitbox);
            if (touching && !transitionBeat && _blightTouchCooldown <= 0)
            {
                BlightBuildup.Apply(local, BlightTouchBuildup);
                _blightTouchCooldown = BlightTouchCooldownTicks;
            }

            // Spectral wisps: pale blue motes lifting off the body.
            if (Main.rand.NextBool(4))
            {
                Vector2 position = NPC.position + new Vector2(Main.rand.NextFloat(NPC.width), Main.rand.NextFloat(NPC.height));
                Dust wisp = Dust.NewDustPerfect(position, DustID.DungeonSpirit, new Vector2(0f, -0.6f), 150, default, 0.8f);
                wisp.noGravity = true;
            }

            // Black dust trailing behind him while he moves; doubled in the Skyfall leap.
            bool moving = Math.Abs(NPC.velocity.X) > 0.5f || NPC.velocity.Y != 0f;
            int trailInterval = 3;
            if (CurrentAttack == GreatBlackKnightAttack.Skyfall && NPC.velocity.Y != 0f)
            {
                trailInterval = 1;
            }
            if (moving && _phaseTwoVisibleTicks % trailInterval == 0)
            {
                Vector2 back = new Vector2(NPC.Center.X - NPC.direction * 8f, NPC.Bottom.Y - Main.rand.NextFloat(4f, 30f));
                Vector2 drift = -NPC.velocity * 0.25f + new Vector2(0f, -0.3f);
                Dust trail = Dust.NewDustPerfect(back, DustID.Smoke, drift, 60, Color.Black, Main.rand.NextFloat(1.1f, 1.5f));
                trail.noGravity = true;
            }
        }
        #endregion

        #region Debuffs
        /// <summary>Puppet melee and the flail route here, on the hit player's machine after Hurt.</summary>
        public void OnHumanoidMeleeHit(Player target)
        {
            target.AddBuff(ModContent.BuffType<BrokenSpirit>(), 600, false);
            target.AddBuff(BuffID.BrokenArmor, 600, false);
            BlightBuildup.Apply(target);
            target.AddBuff(ModContent.BuffType<GrappleMalfunction>(), 30 * 60, false);
        }
        #endregion

        #region Draw
        static Texture2D bombTexture;
        // EnemyMoonfuryBomb is 34x34; held at 0.6 in the fist, fuse along the arm.
        const float HeldBombScale = 0.6f;

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            // The Blight teleport hides him with alpha 255; the puppet draw ignores alpha.
            if (NPC.alpha >= 255)
            {
                return false;
            }
            return base.PreDraw(spriteBatch, screenPos, drawColor);
        }

        public override void PostDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            base.PostDraw(spriteBatch, screenPos, drawColor);
            if (NPC.alpha >= 255 || !PoseActive)
            {
                return;
            }

            // Death Seal's gathering sigil, and the aim thread from the front palm once it is locked.
            if (CurrentAttack == GreatBlackKnightAttack.DeathSeal)
            {
                int timer = AttackTimer;
                EnemyVFX.DrawBlackKnightDeathSeal(NPC.Center, MathHelper.Clamp(timer / (float)DeathSealTelegraphTicks, 0f, 1f), true);
                if (timer >= DeathSealFlashTick && storedPlayerPosition != Vector2.Zero)
                {
                    float threadProgress = (timer - DeathSealFlashTick) / (float)(DeathSealTelegraphTicks - DeathSealFlashTick);
                    EnemyVFX.DrawBlackKnightAimThread(PuppetHandPosition, storedPlayerPosition, MathHelper.Clamp(threadProgress, 0f, 1f), true);
                }
            }

            // Homing Volley and Crystal Storm: the hex crystal forms at the casting palm through the tell.
            if (CurrentAttack == GreatBlackKnightAttack.HomingVolley && AttackTimer < HomingFirstBurstTick)
            {
                float progress = MathHelper.Clamp(AttackTimer / (float)HomingFirstBurstTick, 0f, 1f);
                EnemyVFX.DrawBlackKnightHexCrystal(PuppetBackHandPosition, Vector2.Zero, progress, false);
            }
            if (CurrentAttack == GreatBlackKnightAttack.CrystalStorm && AttackTimer < CrystalStormReleaseTick)
            {
                Vector2 palms = (PuppetHandPosition + PuppetBackHandPosition) * 0.5f;
                float progress = MathHelper.Clamp(AttackTimer / (float)CrystalStormReleaseTick, 0f, 1f);
                EnemyVFX.DrawBlackKnightHexCrystal(palms, Vector2.Zero, progress, false);
            }

            if (_frontProp == BlackKnightProp.Bomb)
            {
                bombTexture ??= ModContent.Request<Texture2D>(UsefulFunctions.RefactorableFilepath(typeof(EnemyMoonfuryBomb)),
                    ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;

                // The arm's world angle: facing space mirrored back for a left-facing puppet.
                float armWorldAngle = _poseFacing;
                if (NPC.direction < 0)
                {
                    armWorldAngle = MathHelper.Pi - _poseFacing;
                }
                Vector2 handWorld = PuppetHandPosition;
                float rotation = armWorldAngle + MathHelper.PiOver2;
                Vector2 origin = bombTexture.Size() * 0.5f;
                spriteBatch.Draw(bombTexture, handWorld - Main.screenPosition, null, drawColor, rotation, origin,
                    HeldBombScale, SpriteEffects.None, 0f);
                if (_bombFuseLit)
                {
                    float fuseProgress = MathHelper.Clamp(AttackTimer / (float)BombReleaseTick, 0f, 1f);
                    EnemyVFX.SpawnBombFuseSparks(handWorld, fuseProgress);
                }
            }
        }
        #endregion

        #region Gore and loot
        public override void OnKill()
        {
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

        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<Humanity>(), 1, 1, 2));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<PurgingStone>()));
        }
        #endregion
    }

    /// <summary>Which attack the Great Black Knight's brain is running. Stored in NPC.ai[2].</summary>
    public enum GreatBlackKnightAttack : byte
    {
        None = 0,
        FlailHurl,
        SpearThrow,
        HomingVolley,
        BombThrow,
        DeathSeal,
        CrystalStorm,
        Skyfall,
        PhaseTwoTransition,
    }
}

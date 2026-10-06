using Microsoft.Xna.Framework;
using System;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items.Armor;
using tsorcRevamp.Content.Items.Materials.Titanite;
using tsorcRevamp.Content.Projectiles.Enemy;
using tsorcRevamp.Content.Projectiles.Enemy.Weapons;
using tsorcRevamp.NPCs.AI;
using tsorcRevamp.NPCs.Puppets;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.NPCs.Enemies.SuperHardMode
{
    /// <summary>SHM spear-and-crystal-hammer elite. See tsorcDocs/CrystalKnightRevampPlan.md.</summary>
    public class CrystalKnight : PuppetNPC, IHumanoidMeleeHitEffects, IHitReactor
    {
        private const string Measure = "Glacial Measure", Reaping = "Rime Reaping", Advance = "Icebound Advance";
        private const string Check = "Butt-End Check", Vault = "Crystal Vault", Slam = "Permafrost Descent";
        private const int Preparation = 30;
        private int _meleeBag = 63, _spellBag = 31, _lastMelee = -1, _lastSpell = -1;
        private bool _slamComboActive;
        private readonly int[] _spellCooldowns = new int[5];
        public int SentryGeneration { get; private set; }
        private int _spell, _castSequence, _dashDirection, _castTarget = -1, _crossfireSequence = -1;
        private bool _warningsSpawned, _preferSpell;
        private float _dashDistance;
        private Vector2 _dashVelocity;
        private Vector2 _vaultOrigin, _vaultTravel, _vaultVelocity;
        private bool _vaultValid;
        private int _backstepTicks, _backstepDirection;

        public int CastSequence => _castSequence;
        public bool IsCasting => Phase == AttackPhase.MagicTelegraph || Phase == AttackPhase.MagicAttack;
        public bool HasLiveCast(int sequence) => IsCasting && sequence == _castSequence
            && CastTargetAlive && NPC.GetGlobalNPC<tsorcRevampGlobalNPC>().StaggerTimer <= 0;
        private bool CastTargetAlive => _castTarget >= 0 && _castTarget < Main.maxPlayers
            && Main.player[_castTarget].active && !Main.player[_castTarget].dead;

        protected override string InvaderTitle => "Crystal Knight";
        protected override bool AnnounceInvasion => false;
        protected override bool AnnounceInvaderDefeat => false;
        protected override bool DespawnsOnPartyWipe => false;
        protected override int EstusChargesMax => 0;
        protected override int CasualStrollChance => 0;
        protected override int HeadArmorItemType => ModContent.ItemType<AncientHornedHelmet>();
        protected override int BodyArmorItemType => ModContent.ItemType<AncientMagicPlateArmor>();
        protected override int LegsArmorItemType => ModContent.ItemType<AncientMagicPlateGreaves>();
        protected override int MeleeWeaponItemType => _slamComboActive
            ? ModContent.ItemType<Content.Items.Weapons.Enemy.CrystalKnightHammer>() : ItemID.NorthPole;
        protected override int RangedWeaponItemType => -1;
        protected override int RangedDamage => 0;
        protected override void DoRangedAttack() { }
        protected override void DoMeleeAttack() { } // All melee is owned by the bespoke combo pool.
        protected override int MagicWeaponItemType => ModContent.ItemType<Content.Items.Weapons.Enemy.CrystalKnightHammer>();
        // Raw hostile hitbox damage, not the old contact damage.
        // SHM progress no longer multiplies these: ProgressionScalingPlayer scales every enemy hit on the player.
        protected override int MeleeDamage => (int)(34f * 1.3f);
        protected override int MagicDamage
        {
            get
            {
                if (_spell == 1)
                {
                    return 28;
                }

                if (_spell == 2)
                {
                    return 34;
                }

                return 30;
            }
        }
        protected override float TopSpeed => 2.5f;
        protected override float Acceleration => 0.09f;
        protected override bool HasWings => true;
        protected override int WingsAccessoryItemType => ItemID.FrozenWings;
        protected override bool ShowWingsWhenGrounded => false;
        protected override bool CanUseAerialMelee => false;
        protected override bool UseLandingTimedLeapSlam => true;
        protected override bool BrakeDuringMagicCast => Flight?.IsAirborne != true;
        protected override EnemyFlightConfig FlightConfig
        {
            get
            {
                EnemyFlightConfig config = EnemyFlightConfig.Default;
                config.HoverAltitude = 120f;
                // The flank is far enough out to meet Crystal Pages' 260px cast gate.
                config.HoverSideOffset = 290f;
                config.HoverTopSpeed = 5.5f;
                config.MaxFlightTicks = 360;
                config.CooldownTicks = 360;
                config.StrafeArcHeight = 25f;
                return config;
            }
        }
        protected override bool HoldAttackSelection => _backstepTicks > 0;
        // Own selection; disable the legacy slash/magic fallback, not the authored pool.
        protected override float MeleeRange => 0f;
        protected override float MagicRange => 0f;
        protected override int MeleeComboChance => 0;
        protected override float ComboReachBase => 225f;
        protected override float MeleeEngageRange => 155f;
        protected override float ComboMaxStartRange => 155f;
        protected override float RangedStartComboMaxRange => 360f;
        protected override float ClosingDistanceSpeedMult => 1.5f;
        protected override bool ShowWeaponDuringNeutral => true;
        protected override WeaponArchetype MeleeArchetype => WeaponArchetype.Halberd;
        protected override bool UseCompositeArmSwing => true;
        protected override bool MirrorMeleeSwingRotationByFacing => true;
        protected override bool UseSwingEasing => true;
        protected override bool UseAuthoredComboSwingClock => true;
        protected override bool AuthoredClockCoversJoustDash => true;
        protected override bool UseLogicalMeleeTelegraphs => true;
        protected override float LogicalWindupSettleFraction => 0.43f;
        protected override bool HasSlashVFX => false;
        protected override float ComboTelegraphMultiplier => 1f;
        protected override int MinComboTelegraphTicks => 20;
        protected override int TelegraphFacingCommitTicks => 10;
        protected override int MeleeComboInterStepLingerTicks => 5;
        protected override int MeleeRecoveryLingerTicks => 8;
        protected override float MeleeBladeWidth => 12f;
        protected override bool DrawWeaponAsSpear => true;
        protected override int AfterimageSampleStep => 1;
        protected override int AfterimageSampleLimit => 12;
        protected override bool UseAuthoredSpearGrip => true;
        protected override bool MirrorSpearRotationByFacing => true;
        protected override string SpearDrawTexturePath => "Terraria/Images/Projectile_" + ProjectileID.NorthPoleWeapon;
        // Measured installed 116x116 holdout: head (1,1), butt (115,115), natural head angle -135 degrees.
        protected override Vector2 SpearTextureSize => new Vector2(116f);
        protected override Vector2 SpearHeadNorm => new Vector2(1f / 116f);
        protected override Vector2 SpearBaseNorm => new Vector2(115f / 116f);
        protected override Vector2 MeleeHandleNorm => _slamComboActive ? MagicGripNorm : new Vector2(0.85f);
        protected override float SpearDrawRotationOffset => MathHelper.PiOver2
            + (ActiveMeleeComboName == Check ? CheckTurn(Phase.ToString(), PhaseTimer) : 0f);

        private string SpellName => _spell == 0 ? "Crystal Pages" : _spell == 1 ? "Rime Script"
            : _spell == 2 ? "Winter Margin" : _spell == 3 ? "Frozen Crossfire" : "Crystal Sentry";
        private int SpellTell => _spell == 1 || _spell == 4 ? 48 : _spell == 2 ? 54 : _spell == 3 ? 46 : 40;
        protected override int MagicTelegraphTicks => Preparation + SpellTell;
        protected override int MagicTelegraphFlashLeadTicks => SpellTell;
        // Crossfire only holds the cast until both cores launch; they charge independently afterward.
        protected override int MagicAttackTicks => _spell == 4 ? 24 : _spell == 1 ? 28 : _spell == 2 ? 72 : _spell == 3 ? 60 : 120;
        protected override int MagicRecoveryTicks => _spell == 0 ? 34 : _spell == 3 ? 42 : 36;
        protected override int MagicCooldownAfterUse => 0;
        protected override Color MagicTelegraphFlashColor => new Color(120, 218, 255);
        protected override bool UseAuthoredMagicCastPose => true;
        protected override bool MirrorMagicWeaponRotationByFacing => true;
        protected override float MagicCastStartRotation => 0.4f;
        protected override float MagicCastEndRotation => -0.5f;
        protected override int MagicWeaponRecoveryHoldTicks => 16;
        // 58x54 hammer: butt near (3,51), head near (46,10); grip on the lower shaft at (12,43).
        protected override Vector2 MagicGripNorm => new Vector2(12f / 58f, 43f / 54f);
        protected override float GetHeldRangedDrawScale(int itemType) => itemType == MagicWeaponItemType ? 0.9f : base.GetHeldRangedDrawScale(itemType);
        protected override bool UseCompositeArmForAdditionalPhase => IsCasting || IsHoldingMagicWeaponDuringRecovery
            || ActiveMeleeComboName == Vault && Phase == AttackPhase.MeleeComboAttack;
        protected override void ModifyAdditionalPhaseWeaponRotation(ref float rotation)
        {
            if (ActiveMeleeComboName == Vault && Phase == AttackPhase.MeleeComboAttack)
            { rotation = VaultWeaponRotation((32 - PhaseTimer) / 32f); return; }
            if (Phase != AttackPhase.MagicTelegraph) return;
            rotation = MathHelper.SmoothStep(MagicCastStartRotation, MagicCastEndRotation,
                MathHelper.Clamp((MagicTelegraphTicks - PhaseTimer - Preparation) / (float)SpellTell, 0f, 1f));
        }

        // On-screen timing: Measure 28 tell | 24 thrust (live 6..15) | 26 reset | 24 thrust | 36 recovery.
        // Reaping 36 tell | 225-degree envelope, Weighted 8/32 k7, ~14 live | 34 recovery.
        // Advance 44 tell (tip frost for final 30) | 30 travel (live 6..19, <=408px,
        // 12 cached body echoes) | 42 recovery. Eligible from 150..360px; 120t cooldown.
        private static MeleeComboStep Thrust(int tell, int ticks, int pause, float damage) => new MeleeComboStep
        {
            Motion = ComboMotion.JoustDash, TelegraphTicks = tell, AttackTicks = ticks,
            PostStepPause = pause, DamageMult = damage, ReachMult = 1f, SwingSpeedMult = 1f,
            Ease = SwingEaseStyle.Smooth, HitWindowEnd = ticks == 24 ? 15f / 24f : 19f / 30f,
        };
        private static readonly MeleeCombo[] CrystalCombos =
        {
            new MeleeCombo { Name = Measure, BaseWeight = 100, Preferred = ComboRangeBand.Close,
                InitialFlashColor = Color.LightCyan, CooldownAfterUse = 75, RecoveryTicks = 36,
                Steps = new[] { Thrust(28, 24, 26, 1f), Thrust(0, 24, 0, 0.9f) } },
            new MeleeCombo { Name = Reaping, BaseWeight = 100, Preferred = ComboRangeBand.Close,
                InitialFlashColor = Color.White, CooldownAfterUse = 110, RecoveryTicks = 34,
                Steps = new[] { new MeleeComboStep { Motion = ComboMotion.OverheadArc,
                    TelegraphTicks = 36, AttackTicks = 40, DamageMult = 1.1f, ReachMult = 1f,
                    SwingSpeedMult = 1f, Ease = SwingEaseStyle.Weighted, EaseInTicks = 8,
                    EaseOutTicks = 32, EaseOutDecay = 7f, HitWindowEnd = 0.338f } } },
            new MeleeCombo { Name = Advance, BaseWeight = 100, Preferred = ComboRangeBand.Mid,
                InitialFlashColor = Color.White, RangedStartOnly = true, CooldownAfterUse = 120,
                RecoveryTicks = 42, Steps = new[] { Thrust(44, 30, 0, 1.2f) } },
            // Butt contact is8t (6..13),65px,24t tell/24t strike/30t recovery; no follow-up.
            new MeleeCombo { Name = Check, BaseWeight = 100, Preferred = ComboRangeBand.Close,
                InitialFlashColor = Color.LightCyan, CooldownAfterUse = 90, RecoveryTicks = 30,
                Steps = new[] { new MeleeComboStep { Motion = ComboMotion.JoustDash,
                    TelegraphTicks = 24, AttackTicks = 24, DamageMult = 0.65f, ReachMult = 1f,
                    SwingSpeedMult = 1f, Ease = SwingEaseStyle.Smooth, HitWindowEnd = 13f / 24f } } },
            // Validated32t/64px-high arc;10t descent live21..30, final landing tick harmless.
            new MeleeCombo { Name = Vault, BaseWeight = 100, Preferred = ComboRangeBand.Mid,
                InitialFlashColor = Color.White, RangedStartOnly = true, CooldownAfterUse = 210,
                RecoveryTicks = 46, Steps = new[] { new MeleeComboStep { Motion = ComboMotion.JoustDash,
                    TelegraphTicks = 44, AttackTicks = 32, DamageMult = 1.1f, ReachMult = 1f,
                    SwingSpeedMult = 1f, Ease = SwingEaseStyle.Smooth, HitWindowEnd = 30f / 32f } } },
            // Permafrost Descent: 42t raised-hammer tell, 16..40t committed dive, impact-only
            // downswing, then 50t recovery. The landing pulse Cripples within 600px for 120t;
            // fifteen delayed frost columns per side follow exposed solid tiles and rise to 240px.
            // Grounded players can jump clear of the wave, while the dive can be rolled through.
            new MeleeCombo { Name = Slam, BaseWeight = 100, Preferred = ComboRangeBand.Any,
                InitialFlashColor = Color.LightCyan, AirborneStartOnly = true,
                HeavyCommit = true, HyperArmor = true, CooldownAfterUse = 300,
                RecoveryTicks = 50, Steps = new[] { new MeleeComboStep {
                    Motion = ComboMotion.LeapSlam, TelegraphTicks = 42, AttackTicks = 90,
                    DamageMult = 0f, ReachMult = 1f, SwingSpeedMult = 1f,
                    Ease = SwingEaseStyle.Smooth } } },
        };
        protected override MeleeCombo[] MeleeComboPoolOverride => CrystalCombos;

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[Type] = 1;
            NPCID.Sets.TrailCacheLength[Type] = 12;
            NPCID.Sets.TrailingMode[Type] = 1;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Frostburn] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Frostburn2] = true;
        }
        public override void SetDefaults()
        {
            NPC.npcSlots = 3f; NPC.width = 20; NPC.height = 48; NPC.timeLeft = 750;
            NPC.damage = 0; NPC.defense = 50; NPC.lifeMax = 2800;
            NPC.HitSound = SoundID.NPCHit1; NPC.DeathSound = SoundID.NPCDeath1;
            NPC.lavaImmune = true; NPC.knockBackResist = 0.25f; NPC.value = 8000; NPC.aiStyle = -1;
            Banner = Type; BannerItem = ModContent.ItemType<Banners.CrystalKnightBanner>();
            var global = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            global.PoiseStaggerResetsAI = true; global.NavSearchRadius = 40;
            global.RemembersLastKnownPos = true; global.CanUseRopes = true;
            global.CanTeleport = true;
            global.TeleportStyle = TeleportStyle.RecoveryOnly;
            global.TeleportVisualStyle = TeleportVisualStyle.MagicIllusion;
            global.EvasiveOnHitCooldownTicks = 240;
        }
        protected override void RunMovementAI(float speedMult)
        {
            // Do not let navigation jump or replace the target during a committed spell/lane.
            if (_backstepTicks > 0 || Phase != AttackPhase.Idle && Phase != AttackPhase.CasualStroll
                && Phase != AttackPhase.ClosingDistance) return;
            SmartFighter4AI.Run(NPC, topSpeed: TopSpeed * speedMult,
                acceleration: Acceleration, doorBreakingDamage: 4, attackRange: MeleeEngageRange);
        }
        public override void AI()
        {
            _dashVelocity = Vector2.Zero;
            _vaultVelocity = Vector2.Zero;
            if (Flight?.IsAirborne != true) NPC.noGravity = false;
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                for (int i = 0; i < _spellCooldowns.Length; i++) if (_spellCooldowns[i] > 0) _spellCooldowns[i]--;
                tsorcRevampGlobalNPC global = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
                if (global.StaggerTimer <= 0)
                {
                    if (_backstepTicks == 0 && global.TeleportCountdown == 0 && global.TeleportAppearanceTimer == 0
                        && (Phase == AttackPhase.Idle || Phase == AttackPhase.CasualStroll))
                    {
                        NPC.TargetClosest(false);
                        if (NPC.HasValidTarget)
                        {
                            Player target = Main.player[NPC.target];
                            bool canTakeOff = Flight != null && !Flight.IsAirborne && Flight.CooldownRemaining == 0
                                && NPC.Distance(target.Center) <= 800f;
                            if (canTakeOff && (target.Center.Y < NPC.Center.Y - FlightHeightTrigger
                                || Main.GameUpdateCount % 60 == 0 && Main.rand.Next(100) < 10))
                            {
                                if (Flight.RequestTakeoff()) NPC.netUpdate = true;
                            }
                            if (Flight?.IsAirborne != true || Flight.Mode == FlightMode.Hover || Flight.Mode == FlightMode.Strafe)
                                SelectAttack(target);
                        }
                    }
                    if (IsCasting && !CastTargetAlive) CancelCast();
                    if (Phase == AttackPhase.MagicTelegraph)
                    {
                        if (!NPC.HasValidTarget) CancelCast();
                        else if (!_warningsSpawned && PhaseTimer <= (_spell == 1 ? 45 : 30))
                            SpawnCastWarnings(Main.player[_castTarget]);
                    }
                }
            }
            base.AI();
            if (!Main.dedServ && Flight?.Mode == FlightMode.TakeOff && Main.GameUpdateCount % 3 == 0)
                CrystalKnightShard.FrostDust(NPC.Bottom + Main.rand.NextVector2Circular(9f, 4f),
                    new Vector2(0f, 1.2f), 0.8f);
            if (_backstepTicks > 0)
            {
                NPC.velocity.X = _backstepDirection * (3.5f + 3.5f * _backstepTicks / 24f);
                if (!Main.dedServ && Main.GameUpdateCount % 2 == 0)
                    CrystalKnightShard.FrostDust(NPC.Bottom + Main.rand.NextVector2Circular(8f, 3f),
                        new Vector2(-_backstepDirection, -1f), 0.75f);
                _backstepTicks--;
                if (_backstepTicks == 0 && Main.netMode != NetmodeID.MultiplayerClient) NPC.netUpdate = true;
            }
            if (_dashVelocity != Vector2.Zero) NPC.velocity.X = _dashVelocity.X;
            if (_vaultValid && ActiveMeleeComboName == Vault && Phase == AttackPhase.MeleeComboAttack)
            { NPC.noGravity = true; NPC.velocity = _vaultVelocity; }
            if ((Phase == AttackPhase.MeleeComboRecovery || Phase == AttackPhase.MagicRecovery)
                && Flight?.IsAirborne != true && _backstepTicks == 0)
                NPC.velocity.X *= 0.65f;
            UpdateCrystalPoise();
            if (Phase == AttackPhase.Idle)
            {
                DebugAttackLabel = null;
                if (_slamComboActive)
                {
                    _slamComboActive = false;
                    base.OnMeleeComboStarted(default);
                }
            }
        }
        private void SelectAttack(Player target)
        {
            // Released crystals survive a stagger, but do not overlap the next committed attack.
            if (HasCastProjectiles()) return;
            if (Flight?.IsAirborne == true && Flight.Mode != FlightMode.Hover && Flight.Mode != FlightMode.Strafe) return;
            float distance = NPC.Distance(target.Center);
            int spell = FindSpell(target, distance);
            if (Flight?.IsAirborne == true && !_preferSpell && CanSlam(target, distance)
                && TryStartMeleeCombo(distance, airborneStart: true))
            { _preferSpell = true; return; }
            bool melee = NPC.velocity.Y == 0f && Math.Abs(target.Center.Y - NPC.Center.Y) < 48f && distance <= 360f;
            if (spell >= 0 && (_preferSpell || !melee)) { StartSpell(spell); return; }
            if (melee && (distance <= 155f && TryStartMeleeCombo(distance)
                || distance >= 140f && TryStartMeleeCombo(distance, rangedStartOnly: true)))
            { _preferSpell = true; return; }
            if (spell >= 0) StartSpell(spell);
        }
        private int FindSpell(Player target, float distance)
        {
            if (HasCastProjectiles()) return -1;
            int eligible = 0;
            if (distance >= 260f && distance <= 800f && Collision.CanHitLine(NPC.Center, 1, 1, target.Center, 1, 1)) eligible |= 1;
            if (distance >= 180f && distance <= 500f && CountSpellPoints(target, 1) >= 2) eligible |= 2;
            if (distance >= 300f && distance <= 700f && CountSpellPoints(target, 2) >= 2) eligible |= 4;
            if (distance >= 240f && distance <= 640f && TryCrossfireSites(target, out _, out _, out _)) eligible |= 8;
            if (distance >= 120f && distance <= 640f && !CrystalSentry.HasOwnedSentry(NPC)
                && CrystalSentry.TrySite(NPC, target, out _)) eligible |= 16;
            // Airborne casts use open sightlines; floor and sentry placement stay grounded.
            if (Flight?.IsAirborne == true) eligible &= 1 | 4;
            for (int i = 0; i < _spellCooldowns.Length; i++) if (_spellCooldowns[i] > 0) eligible &= ~(1 << i);
            if (eligible == 0) return -1;
            if ((_spellBag & eligible) == 0) _spellBag |= eligible;
            int choices = _spellBag & eligible;
            if (_lastSpell >= 0 && (choices & ~(1 << _lastSpell)) != 0) choices &= ~(1 << _lastSpell);
            return PickBit(choices);
        }
        private static int PickBit(int mask)
        {
            int count = 0;
            for (int i = 0; i < 6; i++) if ((mask & (1 << i)) != 0) count++;
            if (count == 0) return -1;
            int roll = Main.rand.Next(count);
            for (int i = 0; i < 6; i++) if ((mask & (1 << i)) != 0 && roll-- == 0) return i;
            return -1;
        }
        private bool CanSlam(Player target, float distance)
        {
            if (Flight?.IsAirborne != true || target.velocity.Y != 0f
                || distance < 120f || distance > 480f || NPC.Bottom.Y > target.Bottom.Y - 56f)
                return false;
            float predictedX = target.Center.X + MathHelper.Clamp(target.velocity.X * 12f, -96f, 96f);
            return PuppetGroundDustWave.TryFindGroundY(predictedX, target.Bottom.Y, out float groundY)
                && Math.Abs(groundY - target.Bottom.Y) <= 32f
                && !Collision.SolidCollision(new Vector2(predictedX - NPC.width / 2f,
                    groundY - NPC.height), NPC.width, NPC.height)
                && Collision.CanHitLine(NPC.Center, 1, 1,
                    new Vector2(predictedX, groundY - NPC.height / 2f), 1, 1);
        }
        private void StartSpell(int spell)
        {
            _spell = spell; _lastSpell = spell; _spellBag &= ~(1 << spell);
            _castTarget = NPC.target;
            _warningsSpawned = false; _castSequence++; _preferSpell = false;
            if (spell == 3) _crossfireSequence = _castSequence;
            if (spell == 4) SentryGeneration = _castSequence;
            DebugAttackLabel = SpellName;
            EnterPhase(AttackPhase.MagicTelegraph, MagicTelegraphTicks);
            NPC.netUpdate = true;
        }
        protected override bool CanSelectMeleeCombo(MeleeCombo combo, float distance, float healthFraction)
            => combo.Name == Measure ? distance <= 155f
                : combo.Name == Reaping ? distance <= 130f
                : combo.Name == Check ? distance <= 85f
                : combo.Name == Vault ? distance >= 140f && distance <= 240f && TryVault(Main.player[NPC.target], out _)
                : combo.Name == Slam ? CanSlam(Main.player[NPC.target], distance)
                : distance >= 150f && distance <= 360f && CanDashTo(Main.player[NPC.target]);
        protected override int ReactiveComboIndex(float dist, ComboRangeBand band, int[] ready)
        {
            int eligible = 0;
            for (int i = 0; i < ready.Length; i++) if (ready[i] > 0) eligible |= 1 << i;
            if ((_meleeBag & eligible) == 0) _meleeBag |= eligible;
            int choices = _meleeBag & eligible;
            if (_lastMelee >= 0 && (choices & ~(1 << _lastMelee)) != 0) choices &= ~(1 << _lastMelee);
            return PickBit(choices);
        }
        protected override void OnMeleeComboStarted(MeleeCombo combo)
        {
            _slamComboActive = combo.Name == Slam;
            base.OnMeleeComboStarted(combo);
            DebugAttackLabel = combo.Name;
            if (Main.netMode == NetmodeID.MultiplayerClient) return;
            int index = combo.Name == Measure ? 0 : combo.Name == Reaping ? 1 : combo.Name == Advance ? 2
                : combo.Name == Check ? 3 : combo.Name == Vault ? 4 : 5;
            _meleeBag &= ~(1 << index); _lastMelee = index;
            if (combo.Name == Advance) LockDash();
            if (combo.Name == Vault) LockVault();
        }
        protected override bool ShouldContinueMeleeCombo(string comboName, int nextStepIndex, Player target, bool previousStepHit)
            => comboName != Measure || (nextStepIndex == 1 && target.active && !target.dead
                && Math.Sign(target.Center.X - NPC.Center.X) == NPC.direction && NPC.Distance(target.Center) <= 155f
                && Collision.CanHitLine(PuppetHandPosition, 1, 1, target.Center, 1, 1));
        protected override void ModifyMeleeArcEndpoints(ComboMotion motion, ref float start, ref float end)
        {
            if (motion == ComboMotion.JoustDash)
            { start = ActiveMeleeComboName == Vault ? -MathHelper.PiOver4 : MathHelper.PiOver4; end = start; }
            else if (motion == ComboMotion.OverheadArc) { start = -1.55f; end = 2.377f; }
        }

        // Shared with the offline renderer: stable point with authored grip translation.
        public static float ThrustGrip(float progress, int ticks)
        {
            float elapsed = progress * ticks;
            int end = ticks == 30 ? 20 : 16;
            if (elapsed < 6f) return MathHelper.SmoothStep(0.45f, 0.97f, elapsed / 6f);
            if (elapsed < end) return 0.97f;
            return MathHelper.SmoothStep(0.97f, 0.45f, (elapsed - end) / (ticks - end));
        }
        public static float CheckGrip(float progress)
        {
            float elapsed = progress * 24f;
            return elapsed < 6f ? MathHelper.SmoothStep(0.75f, 0.60f, elapsed / 6f)
                : elapsed < 14f ? 0.60f : MathHelper.SmoothStep(0.60f, 0.75f, (elapsed - 14f) / 10f);
        }
        public static float CheckTurn(string phase, int remaining)
            => phase == "MeleeComboTelegraph" ? MathHelper.SmoothStep(0f, MathHelper.Pi, (24f - remaining) / 24f)
                : phase == "MeleeComboAttack" ? MathHelper.Pi
                : phase == "MeleeComboRecovery" ? MathHelper.SmoothStep(MathHelper.Pi, 0f,
                    MathHelper.Clamp((30f - remaining - 8f) / 22f, 0f, 1f)) : 0f;
        public static Vector2 VaultOffset(Vector2 travel, float progress)
            => travel * progress - new Vector2(0f, 256f * progress * (1f - progress));
        public static float VaultWeaponRotation(float progress)
            => MathHelper.SmoothStep(-MathHelper.PiOver4, MathHelper.PiOver2,
                MathHelper.Clamp((progress * 32f - 16f) / 6f, 0f, 1f));
        protected override float? OverrideSpearGrip(float progress)
        {
            if (ActiveMeleeComboName == Check && IsMeleeComboPhase)
                return Phase == AttackPhase.MeleeComboAttack ? CheckGrip(progress) : 0.75f;
            if (ActiveMeleeComboName == Vault && IsMeleeComboPhase) return 0.9f;
            if (Phase == AttackPhase.MeleeComboAttack)
                return ActiveMeleeComboMotion == ComboMotion.JoustDash
                    ? ThrustGrip(progress, ActiveMeleeComboName == Advance ? 30 : 24) : 0.85f;
            if (Phase == AttackPhase.MeleeComboTelegraph || Phase == AttackPhase.MeleeComboPause
                || Phase == AttackPhase.MeleeComboRecovery)
                return ActiveMeleeComboName == Reaping ? 0.85f : 0.45f;
            return 0.5f;
        }
        protected override bool CanDamageWithMeleeStep(MeleeComboStep step, float progress)
            => ActiveMeleeComboName != Slam
                && (ActiveMeleeComboName != Advance || _dashDistance > 0f)
                && (ActiveMeleeComboName != Vault || _vaultValid && progress >= 21f / 32f)
                && (step.Motion != ComboMotion.JoustDash || progress >= 6f / step.AttackTicks);
        private bool IsMeleeComboPhase => Phase == AttackPhase.MeleeComboTelegraph
            || Phase == AttackPhase.MeleeComboAttack || Phase == AttackPhase.MeleeComboRecovery;
        private Vector2 ActiveSpearPoint => ActiveMeleeComboName == Check && IsMeleeComboPhase
            ? PuppetHandPosition - PuppetWeaponDirection * (114f * MathF.Sqrt(2f)
                * (1f - OverrideSpearGrip(Phase == AttackPhase.MeleeComboAttack ? (24 - PhaseTimer) / 24f : 0f).Value) * NPC.scale)
            : PuppetSpearTipPosition;
        protected override void ModifyFrontBladeCapsule(ref Vector2 origin, ref Vector2 tip) => tip = ActiveSpearPoint;
        protected override void OnMeleeComboTelegraphTick(MeleeCombo combo, MeleeComboStep step, int elapsed, int total)
        {
            if (combo.Name == Slam && !Main.dedServ)
            {
                if (elapsed % 2 == 0)
                {
                    Vector2 hammerHead = PuppetHandPosition + PuppetWeaponDirection * 42f;
                    CrystalKnightShard.GatherDust(hammerHead, elapsed / (float)total, 18f);
                    CrystalKnightShard.FrostDust(hammerHead,
                        -PuppetWeaponDirection * 0.8f, 1.1f);
                }
                if (elapsed >= total - 30 && elapsed % 3 == 0 && NPC.HasValidTarget)
                {
                    Player target = Main.player[NPC.target];
                    if (PuppetGroundDustWave.TryFindGroundY(target.Center.X,
                        target.Bottom.Y, out float groundY))
                        CrystalKnightShard.FrostDust(new Vector2(target.Center.X, groundY - 3f)
                            + Main.rand.NextVector2Circular(18f, 2f),
                            -Vector2.UnitY * 1.2f, 0.8f);
                }
                return;
            }
            if (combo.Name == Vault && elapsed == total - 10 && Main.netMode != NetmodeID.MultiplayerClient)
            { LockVault(); NPC.netUpdate = true; RequestNetworkSnapshot(); }
            if (combo.Name == Advance && elapsed == total - 10 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                // Abort into harmless recovery; never swap to a shorter, untelegraphed dash.
                if (!CanDashTo(Main.player[NPC.target])) { _dashDistance = 0f; }
                else LockDash();
                NPC.netUpdate = true; RequestNetworkSnapshot();
            }
            if (combo.Name == Advance && _dashDistance > 0f && elapsed >= total - 30 && !Main.dedServ)
                for (int i = 0; i < 2; i++)
                    CrystalKnightShard.FrostDust(ActiveSpearPoint + Main.rand.NextVector2Circular(5f, 5f),
                        -PuppetWeaponDirection * Main.rand.NextFloat(0.3f, 1.5f), 0.9f);
            if (!Main.dedServ && elapsed % 3 == 0)
            {
                CrystalKnightShard.GatherDust(ActiveSpearPoint, elapsed / (float)total, 14f);
                if (combo.Name == Vault && _vaultValid)
                    CrystalKnightShard.GatherDust(_vaultOrigin + _vaultTravel + new Vector2(NPC.width / 2f, NPC.height), elapsed / (float)total, 18f);
                if (combo.Name == Advance)
                {
                    Player target = Main.player[NPC.target];
                    int direction = elapsed < total - 10 ? Math.Sign(target.Center.X - NPC.Center.X) : _dashDirection;
                    float distance = elapsed < total - 10 ? Math.Min(408f, Math.Abs(target.Center.X - NPC.Center.X) + 48f) : _dashDistance;
                    for (float x = 24f; x <= distance; x += 24f)
                        CrystalKnightShard.FrostDust(PuppetHandPosition + new Vector2(direction * x, 0f), Vector2.Zero, 0.45f);
                }
            }
        }
        protected override void OnMeleeComboAttackTick(MeleeCombo combo, MeleeComboStep step, int elapsed, int total)
        {
            if (combo.Name == Vault && _vaultValid)
            {
                Vector2 next = _vaultOrigin + VaultOffset(_vaultTravel, (elapsed + 1f) / 32f);
                Vector2 desired = next - NPC.position;
                _vaultVelocity = Collision.TileCollision(NPC.position, desired, NPC.width, NPC.height);
                if (Main.netMode != NetmodeID.MultiplayerClient && Vector2.DistanceSquared(desired, _vaultVelocity) > 0.01f)
                { _vaultValid = false; PhaseTimer = 1; NPC.netUpdate = true; RequestNetworkSnapshot(); }
                NPC.velocity = _vaultVelocity;
            }
            if (combo.Name == Advance)
            {
                float speed = _dashDistance * (DashProgress(elapsed + 1) - DashProgress(elapsed));
                _dashVelocity = Collision.TileCollision(NPC.position, new Vector2(_dashDirection * speed, 0f), NPC.width, NPC.height);
                NPC.velocity.X = _dashVelocity.X;
                if (_dashDistance > 0f && _dashVelocity.X != 0f) AfterimageTicks = 3;
            }
            if (!Main.dedServ && CanDamageWithMeleeStep(step, elapsed / (float)total)
                && elapsed / (float)total <= step.HitWindowEnd && elapsed % 2 == 0)
                for (int i = 0; i < 2; i++)
                    CrystalKnightShard.FrostDust(ActiveSpearPoint + Main.rand.NextVector2Circular(4f, 4f),
                        -PuppetWeaponDirection * 1.5f, 0.9f);
        }
        protected override void OnComboStepCompleted(MeleeComboStep step)
        {
            base.OnComboStepCompleted(step);
            if (ActiveMeleeComboName != Vault) return;
            // The engine has not integrated this tick's final, tile-clipped displacement yet.
            NPC.position += _vaultVelocity;
            NPC.noGravity = false;
            NPC.velocity = _vaultVelocity = Vector2.Zero;
            if (!Main.dedServ) CrystalKnightShard.Shatter(NPC.Bottom, 20);
        }
        private void LockVault()
        {
            _vaultOrigin = NPC.position;
            _vaultValid = TryVault(Main.player[NPC.target], out _vaultTravel);
        }
        private bool TryVault(Player target, out Vector2 travel)
        {
            int direction = target.Center.X >= NPC.Center.X ? 1 : -1;
            travel = new Vector2(target.Center.X - NPC.Center.X - direction * 32f, 0f);
            Vector2 landing = NPC.position + travel;
            if (!Collision.SolidCollision(landing + new Vector2(0f, NPC.height), NPC.width, 16)) return false;
            for (int i = 0; i <= 32; i++)
            {
                Vector2 position = NPC.position + VaultOffset(travel, i / 32f);
                if (Collision.SolidCollision(position, NPC.width, NPC.height)
                    || i < 22 && Collision.SolidCollision(position - new Vector2(0f, 130f), NPC.width, 130)) return false;
            }
            return true;
        }
        public static float DashProgress(float tick)
        {
            tick = MathHelper.Clamp(tick, 0f, 30f);
            const float integral = 6f / 3f + 14f + 10f / 3f;
            float distance = tick <= 6f ? tick * tick * tick / 108f
                : tick <= 20f ? 2f + tick - 6f
                : 16f + 10f / 3f * (1f - MathF.Pow((30f - tick) / 10f, 3f));
            return distance / integral;
        }
        private void LockDash()
        {
            Player target = Main.player[NPC.target];
            _dashDirection = Math.Sign(target.Center.X - NPC.Center.X);
            if (_dashDirection == 0) _dashDirection = NPC.direction;
            _dashDistance = MathHelper.Clamp(Math.Abs(target.Center.X - NPC.Center.X) + 48f, 72f, 408f);
        }
        private bool CanDashTo(Player target)
        {
            float travel = Math.Abs(target.Center.X - NPC.Center.X) + 48f;
            int direction = Math.Sign(target.Center.X - NPC.Center.X);
            for (float x = 16f; x <= travel; x += 16f)
            {
                Vector2 position = NPC.position + new Vector2(direction * x, 0f);
                if (Collision.SolidCollision(position, NPC.width, NPC.height)
                    || !Collision.SolidCollision(position + new Vector2(0f, NPC.height), NPC.width, 20)) return false;
            }
            return true;
        }
        private void UpdateCrystalPoise()
        {
            var global = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            bool heavy = ActiveMeleeComboName == Reaping || ActiveMeleeComboName == Advance
                || ActiveMeleeComboName == Vault || ActiveMeleeComboName == Slam;
            bool live = Phase == AttackPhase.MeleeComboAttack && (ActiveMeleeComboName == Reaping
                ? PhaseTimer >= 26 : ActiveMeleeComboName == Vault ? _vaultValid && PhaseTimer <= 11 && PhaseTimer >= 2
                : ActiveMeleeComboName == Slam || PhaseTimer <= 24 && PhaseTimer > 10);
            global.AttackCommitted = heavy && (live || Phase == AttackPhase.MeleeComboTelegraph && PhaseTimer <= 8);
            global.AttackTelegraphing = !global.AttackCommitted && (IsCasting
                || Phase == AttackPhase.MeleeComboTelegraph || Phase == AttackPhase.MeleeComboAttack);
        }

        private int CountSpellPoints(Player target, int spell)
        {
            int count = 0;
            for (int i = -1; i <= 1; i++) if (TrySpellPoint(target, spell, i, out _)) count++;
            return count;
        }
        private bool TryCrossfireSites(Player target, out Vector2 floor, out Vector2 second, out bool ceiling)
        {
            ceiling = false; second = Vector2.Zero;
            Vector2 origin = NPC.Center - new Vector2(0f, 68f);
            if (Collision.SolidCollision(origin - new Vector2(24f, 18f), 48, 36)
                || !FindCrossfireSurface(target, -128f, false, origin - new Vector2(18f, 0f), out floor))
            { floor = Vector2.Zero; return false; }
            ceiling = FindCrossfireSurface(target, 128f, true, origin + new Vector2(18f, 0f), out second);
            if (ceiling) return true;
            // Without a usable ceiling the second core hovers 12 tiles over the player.
            // The projectile refreshes this position at launch, then locks it for the charge.
            second = target.Center - new Vector2(0f, 12f * 16f);
            return second.Y > 32f && second.Y < Main.maxTilesY * 16f - 32f
                && !Collision.SolidCollision(second - new Vector2(10f), 20, 20)
                && Collision.CanHitLine(origin + new Vector2(8f, -10f), 20, 20,
                    second - new Vector2(10f), 20, 20);
        }
        private bool FindCrossfireSurface(Player target, float offset, bool ceiling, Vector2 origin, out Vector2 surface)
        {
            surface = Vector2.Zero;
            int x = (int)(target.Center.X + offset) / 16;
            if (x < 2 || x >= Main.maxTilesX - 2) return false;
            int startY = (int)(target.Center.Y / 16f);
            int direction = ceiling ? -1 : 1;
            for (int i = 1; i <= (ceiling ? 30 : 12); i++)
            {
                int y = startY + direction * i;
                if (y < 2 || y >= Main.maxTilesY - 2) return false;
                Tile tile = Main.tile[x, y];
                if (!tile.HasUnactuatedTile || !Main.tileSolid[tile.TileType] || Main.tileSolidTop[tile.TileType]) continue;
                // Flat full tiles give an unambiguous outward fan; slopes/half blocks defer this site.
                if (tile.IsHalfBlock || tile.Slope != SlopeType.Solid) return false;
                surface = new Vector2(x * 16f + 8f, (y + (ceiling ? 1 : 0)) * 16f);
                Vector2 normal = ceiling ? Vector2.UnitY : -Vector2.UnitY;
                Vector2 endpoint = surface + normal * 11f;
                return (ceiling || Math.Abs(surface.Y - target.Bottom.Y) <= 160f)
                    && !Collision.SolidCollision(endpoint - new Vector2(10f), 20, 20)
                    && Collision.CanHitLine(origin - new Vector2(10f), 20, 20, endpoint - new Vector2(10f), 20, 20);
            }
            return false;
        }
        private bool TrySpellPoint(Player target, int spell, int lane, out Vector2 point)
        {
            point = target.Center + new Vector2(lane * 96f, -240f);
            if (spell == 2) return !Collision.SolidCollision(point - new Vector2(12f), 24, 24)
                && Collision.CanHitLine(point, 1, 1, point + new Vector2(0f, 240f), 1, 1);
            int tileX = (int)(target.Center.X + lane * 96f) / 16;
            int firstY = (int)target.Bottom.Y / 16 - 2;
            if (tileX < 2 || tileX >= Main.maxTilesX - 2) return false;
            for (int y = Math.Max(2, firstY); y <= Math.Min(Main.maxTilesY - 2, firstY + 12); y++)
            {
                Tile tile = Main.tile[tileX, y];
                if (!tile.HasUnactuatedTile || !Main.tileSolid[tile.TileType] || Main.tileSolidTop[tile.TileType]) continue;
                point = new Vector2(tileX * 16f + 8f, y * 16f);
                return Math.Abs(point.Y - target.Bottom.Y) <= 96f
                    && !Collision.SolidCollision(point - new Vector2(36f, 64f), 72, 64)
                    && Collision.CanHitLine(NPC.Center, 1, 1, point - new Vector2(0f, 32f), 1, 1);
            }
            return false;
        }
        private void SpawnCastWarnings(Player target)
        {
            _warningsSpawned = true;
            if (_spell == 4)
            {
                if (!CrystalSentry.Spawn(NPC, target, _castSequence, PhaseTimer)) { CancelCast(); return; }
            }
            else if (_spell == 3)
            {
                if (!TryCrossfireSites(target, out Vector2 floor, out Vector2 second, out bool ceiling))
                { CancelCast(); return; }
                Vector2 origin = NPC.Center - new Vector2(0f, 68f);
                CrystalKnightAnchor.Spawn(NPC, origin - new Vector2(18f, 0f), floor, -Vector2.UnitY, MagicDamage, PhaseTimer, _castSequence);
                CrystalKnightAnchor.Spawn(NPC, origin + new Vector2(18f, 0f), second,
                    Vector2.UnitY, MagicDamage, PhaseTimer, _castSequence,
                    airborne: !ceiling, targetIndex: _castTarget);
            }
            else if (_spell == 0)
            {
                Vector2 origin = NPC.Center + new Vector2(0f, -68f);
                if (Vector2.Distance(origin, target.Center) < 240f
                    || !Collision.CanHitLine(origin, 1, 1, target.Center, 1, 1)) { CancelCast(); return; }
                Vector2 aim = (target.Center - origin).SafeNormalize(Vector2.UnitX);
                // Three complete fans, released at cast +0, +30 and +60 ticks.
                for (int wave = 0; wave < 3; wave++)
                    for (int i = -1; i <= 1; i++)
                        CrystalKnightShard.Spawn(NPC, origin + new Vector2(i * 24f, -wave * 12f),
                            aim.RotatedBy(MathHelper.ToRadians(i * 12f)) * 10f, MagicDamage,
                            PhaseTimer + wave * 30, _castSequence, false);
            }
            else
            {
                if (CountSpellPoints(target, _spell) < 2) { CancelCast(); return; }
                for (int i = -1; i <= 1; i++)
                {
                    if (!TrySpellPoint(target, _spell, i, out Vector2 point)) continue;
                    if (_spell == 1) Projectile.NewProjectile(NPC.GetSource_FromAI(), point - new Vector2(0f, 6f), Vector2.Zero,
                        ModContent.ProjectileType<CrystalKnightFloorSpike>(), MagicDamage, 2f, Main.myPlayer,
                        NPC.whoAmI, PhaseTimer, _castSequence);
                    else CrystalKnightShard.Spawn(NPC, point, new Vector2(0f, 8f), MagicDamage, PhaseTimer + 12, _castSequence, true);
                }
            }
            NPC.netUpdate = true; RequestNetworkSnapshot();
        }
        protected override void DoMagicAttack()
        {
            _magicAttackTicksOverride = MagicAttackTicks;
            if (!Main.dedServ) SoundEngine.PlaySound(SoundID.Item30 with { Pitch = 0.35f, Volume = 0.65f }, NPC.Center);
        }
        protected override void DoMagicTick(int ticksRemaining)
        {
            if (!Main.dedServ) DrawHammerDust();
            // The first hail releases 12 ticks into the attack. Begin the second tell then
            // so its release follows exactly 40 ticks later.
            if (_spell == 2 && ticksRemaining == MagicAttackTicks - 12
                && Main.netMode != NetmodeID.MultiplayerClient && CastTargetAlive)
            {
                Player target = Main.player[_castTarget];
                for (int i = -1; i <= 1; i++)
                    if (TrySpellPoint(target, _spell, i, out Vector2 point))
                        CrystalKnightShard.Spawn(NPC, point, new Vector2(0f, 8f),
                            MagicDamage, 40, _castSequence, true);
            }
            bool castFinished = _spell == 3 ? !HasWaitingCrossfireShots() || ticksRemaining == 1
                : _spell == 4 ? ticksRemaining == 1 : !HasCastProjectiles() || ticksRemaining == 1;
            if (Main.netMode != NetmodeID.MultiplayerClient && castFinished)
            {
                _spellCooldowns[_spell] = _spell == 4 ? 4200 : _spell == 0 ? 150 : _spell == 1 ? 240 : _spell == 2 ? 360 : 480;
                PhaseTimer = 1;
            }
        }
        protected override void OnLeapSlamLanded(MeleeComboStep step)
        {
            if (ActiveMeleeComboName != Slam) return;
            Vector2 impact = NPC.Bottom;
            if (!Main.dedServ)
            {
                CrystalKnightShard.Shatter(impact, 40);
                UsefulFunctions.ScreenShake(impact, 7f, 18, distanceFalloff: 900f);
                SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.7f, Pitch = -0.25f }, impact);
                Player local = Main.LocalPlayer;
                if (local.active && !local.dead && Vector2.DistanceSquared(local.Center, impact) <= 600f * 600f)
                    local.AddBuff(ModContent.BuffType<Buffs.Debuffs.Crippled>(), 120);
            }
            if (Main.netMode == NetmodeID.MultiplayerClient) return;
            Projectile.NewProjectile(NPC.GetSource_FromThis(), impact - Vector2.UnitY * 24f,
                Vector2.Zero, ModContent.ProjectileType<PuppetMeleeHitbox>(),
                (int)(MeleeDamage * 1.2f), 4f, Main.myPlayer, 64f, 48f);
            int waveDamage = 30;
            for (int direction = -1; direction <= 1; direction += 2)
            {
                float previousGroundY = impact.Y;
                for (int column = 1; column <= 15; column++)
                {
                    float x = impact.X + direction * column * 16f;
                    if (!PuppetGroundDustWave.TryFindGroundY(x, previousGroundY, out float groundY)
                        || Math.Abs(groundY - previousGroundY) > 24f) break;
                    float height = MathHelper.Lerp(72f, 240f,
                        MathHelper.Clamp((column - 1f) / 5f, 0f, 1f));
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), new Vector2(x, groundY),
                        Vector2.Zero, ModContent.ProjectileType<CrystalKnightFrostWave>(),
                        waveDamage, 2f, Main.myPlayer, column * 2f, height);
                    previousGroundY = groundY;
                }
            }
        }
        private bool HasWaitingCrossfireShots()
        {
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p.active && p.type == ModContent.ProjectileType<CrystalKnightAnchor>()
                    && (int)p.ai[0] == NPC.whoAmI && (int)p.ai[2] == _castSequence
                    && p.ModProjectile is CrystalKnightAnchor anchor && anchor.IsWaitingToFire) return true;
            }
            return false;
        }
        private bool HasCastProjectiles()
        {
            // Launched Crossfire cores and their later shards do not reserve the knight's attack slot.
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p.active && (p.type == ModContent.ProjectileType<CrystalKnightShard>()
                    || p.type == ModContent.ProjectileType<CrystalKnightFloorSpike>()
                    || p.type == ModContent.ProjectileType<CrystalKnightAnchor>())
                    && (int)p.ai[0] == NPC.whoAmI)
                {
                    if ((int)p.ai[2] == _crossfireSequence && (p.type == ModContent.ProjectileType<CrystalKnightAnchor>()
                        || p.type == ModContent.ProjectileType<CrystalKnightShard>())) continue;
                    return true;
                }
            }
            return false;
        }
        protected override void DoMagicTelegraphVFX(float progress)
        {
            if (!Main.dedServ && MagicTelegraphTicks - PhaseTimer >= Preparation) DrawHammerDust();
        }
        private void DrawHammerDust()
        {
            if (Main.dedServ || Main.GameUpdateCount % 3 != 0) return;
            // Rotating five-point glyph at the hammer head, separate from the spawn-point warnings.
            float angle = (float)Main.GameUpdateCount * 0.04f;
            float hammerRotation = Phase == AttackPhase.MagicTelegraph
                ? MathHelper.SmoothStep(MagicCastStartRotation, MagicCastEndRotation,
                    MathHelper.Clamp((MagicTelegraphTicks - PhaseTimer - Preparation) / (float)SpellTell, 0f, 1f))
                : MagicCastEndRotation;
            Vector2 hammerHead = PuppetHandPosition + new Vector2(34f * NPC.direction, -33f)
                .RotatedBy(hammerRotation * NPC.direction) * 0.9f;
            for (int i = 0; i < 5; i++)
                CrystalKnightShard.FrostDust(hammerHead
                    + (Vector2.UnitX * 7f).RotatedBy(angle + i * MathHelper.TwoPi / 5f), Vector2.Zero, 0.65f);
            if (_spell == 3)
                for (int i = -1; i <= 1; i += 2)
                    CrystalKnightShard.FrostDust(hammerHead + new Vector2(0f, i * 9f), new Vector2(0f, i * 0.8f), 0.75f);
        }
        private void CancelCast()
        {
            _castSequence++; _spellCooldowns[_spell] = 90;
            EnterPhase(AttackPhase.MagicRecovery, 24); NPC.netUpdate = true;
        }
        public override void OnHitByItem(Player player, Item item, NPC.HitInfo hit, int damageDone)
        {
            base.OnHitByItem(player, item, hit, damageDone);
            NPC.GetGlobalNPC<tsorcRevampGlobalNPC>().RequestHitReaction(NPC, true);
        }
        public override void OnHitByProjectile(Projectile projectile, NPC.HitInfo hit, int damageDone)
        {
            base.OnHitByProjectile(projectile, hit, damageDone);
            NPC.GetGlobalNPC<tsorcRevampGlobalNPC>().RequestHitReaction(NPC,
                projectile.DamageType == DamageClass.Melee);
        }
        void IHitReactor.OnServerHit(NPC npc, bool melee)
        {
            tsorcRevampGlobalNPC global = npc.GetGlobalNPC<tsorcRevampGlobalNPC>();
            if (Main.netMode == NetmodeID.MultiplayerClient || global.IsTeleportIllusion
                || global.FighterEvasionCooldown > 0 || global.StaggerTimer > 0
                || global.InAttack || global.InEvasion || global.TeleportCountdown > 0
                || global.TeleportAppearanceTimer > 0 || _backstepTicks > 0
                || Phase != AttackPhase.Idle && Phase != AttackPhase.CasualStroll
                || !npc.HasValidTarget || !Main.rand.NextBool(3)) return;

            Player target = Main.player[npc.target];
            bool evaded = false;
            int choice = Main.rand.Next(3);
            if (choice == 0)
            {
                // MagicIllusion leaves an attacking doppelganger at the departure point.
                tsorcRevampAIs.QueueTeleport(npc, 20, requireLineofSight: false,
                    TeleportTelegraphTime: 30, minRange: 6);
                evaded = global.TeleportCountdown > 0;
                if (evaded) Flight?.EndFlightNow();
            }
            else if (choice == 2 && Flight != null)
            {
                evaded = Flight.IsAirborne
                    ? Flight.RequestStrafe(target.Center + new Vector2(
                        npc.Center.X < target.Center.X ? 180f : -180f, -120f))
                    : Flight.RequestTakeoff();
            }
            if (!evaded)
            {
                Flight?.EndFlightNow();
                _backstepDirection = npc.Center.X < target.Center.X ? -1 : 1;
                npc.velocity = new Vector2(_backstepDirection * 7f, -8f);
                _backstepTicks = 24;
            }
            global.FighterEvasionCooldown = global.EvasiveOnHitCooldownTicks;
            npc.netUpdate = true;
            RequestNetworkSnapshot();
        }
        public override void OnStagger(NPC npc)
        {
            base.OnStagger(npc);
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                _castSequence++; _spellCooldowns[_spell] = Math.Max(90, _spellCooldowns[_spell]);
                NPC.netUpdate = true; RequestNetworkSnapshot();
            }
            _dashVelocity = Vector2.Zero;
            _backstepTicks = 0;
            Flight?.EndFlightNow();
            _vaultValid = false; _vaultVelocity = Vector2.Zero; NPC.noGravity = false;
        }
        // PuppetMeleeHitbox calls this on the hit player's machine after Hurt, respecting roll immunity.
        public void OnHumanoidMeleeHit(Player player)
        {
            player.AddBuff(BuffID.Frostburn, 180);
            player.AddBuff(ModContent.BuffType<Buffs.Debuffs.TornWings>(), 540);
            Buffs.Debuffs.FrostBuildup.Apply(player);
        }
        public override void SendExtraAI(BinaryWriter writer)
        {
            base.SendExtraAI(writer);
            writer.Write((byte)_spell); writer.Write(_castSequence); writer.Write((short)_castTarget); writer.Write(_warningsSpawned);
            writer.Write(_crossfireSequence);
            writer.Write(_dashDistance); writer.Write((sbyte)_dashDirection);
            writer.Write(_vaultOrigin.X); writer.Write(_vaultOrigin.Y);
            writer.Write(_vaultTravel.X); writer.Write(_vaultTravel.Y); writer.Write(_vaultValid);
            writer.Write((byte)_meleeBag); writer.Write((byte)_spellBag);
            writer.Write((sbyte)_lastMelee); writer.Write((sbyte)_lastSpell); writer.Write(_preferSpell);
            writer.Write((byte)_backstepTicks); writer.Write((sbyte)_backstepDirection);
            writer.Write(SentryGeneration);
            for (int i = 0; i < _spellCooldowns.Length; i++) writer.Write((short)_spellCooldowns[i]);
        }
        public override void ReceiveExtraAI(BinaryReader reader)
        {
            base.ReceiveExtraAI(reader);
            _spell = reader.ReadByte(); _castSequence = reader.ReadInt32(); _castTarget = reader.ReadInt16(); _warningsSpawned = reader.ReadBoolean();
            _crossfireSequence = reader.ReadInt32();
            _dashDistance = reader.ReadSingle(); _dashDirection = reader.ReadSByte();
            _vaultOrigin = new Vector2(reader.ReadSingle(), reader.ReadSingle());
            _vaultTravel = new Vector2(reader.ReadSingle(), reader.ReadSingle()); _vaultValid = reader.ReadBoolean();
            _meleeBag = reader.ReadByte(); _spellBag = reader.ReadByte();
            _lastMelee = reader.ReadSByte(); _lastSpell = reader.ReadSByte(); _preferSpell = reader.ReadBoolean();
            _backstepTicks = reader.ReadByte(); _backstepDirection = reader.ReadSByte();
            SentryGeneration = reader.ReadInt32();
            for (int i = 0; i < _spellCooldowns.Length; i++) _spellCooldowns[i] = reader.ReadInt16();
            if (IsCasting || Phase == AttackPhase.MagicRecovery)
                DebugAttackLabel = SpellName;
        }
        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            Player player = spawnInfo.Player;
            if (NPC.AnyNPCs(Type) || !tsorcRevampWorld.SuperHardMode || !(player.ZoneSnow || player.ZoneHallow)) return 0f;
            float chance = player.ZoneOverworldHeight ? 0.2f : 0.36f;
            if (player.ZoneSnow && player.ZoneHallow) chance *= 2f;
            if (Main.bloodMoon) chance *= 2f;
            return chance;
        }
        public override void OnKill()
        {
            if (!Main.dedServ) CrystalKnightShard.Shatter(NPC.Center, 70);
        }
        public override void ModifyNPCLoot(NPCLoot npcLoot)
            => npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<BlueTitanite>(), 1, 2, 3));
    }
}

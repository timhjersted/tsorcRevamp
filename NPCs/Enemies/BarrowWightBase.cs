using Microsoft.Xna.Framework;
using System;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;
using tsorcRevamp.Content.Items.Armor.BarrowWightPhantomSet;
using tsorcRevamp.Content.Items.Weapons.Enemy;
using tsorcRevamp.Content.Projectiles.Enemy;
using tsorcRevamp.NPCs.Invaders;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.NPCs.Enemies
{
    // The two world-spawning wights share traversal and sword vocabulary. The Witchking's Phantom
    // keeps its own encounter lifecycle, hovering kit and void-hit effects.
    public abstract class BarrowWightBase : PuppetNPC, IHumanoidMeleeHitEffects
    {
        private enum TravelMode : byte
        {
            Walk, Rise, Drift, Settle
        }
        private enum WightMove : byte
        {
            Cut, Lunge, Plunge, Ambush, Breath
        }
        private enum MoveStage : byte
        {
            None, LungeTell, LungeDash, PlungeApproach, PlungeTell, PlungeDrop,
            PlungeLanding, WallSink, WallTravel, WallTell, WallEmerge, BreathTell, BreathStream, Recovery
        }

        private const string CutName = "Barrow Cut";
        private const string PassingName = "Passing Cut";
        private const string AmbushName = "Wall Ambush Cut";
        private const int CutAttackTicks = 50;
        private const int LungeDashTicks = 42;
        private const int LungeLiveTicks = 21;
        private const float BladeReach = 63f;
        private const float LandingRadius = 48f;

        // Plunge positioning: the dive starts PlungeHeight px above the player's floor (a clear column is required).
        // The approach re-aims above the player every tick, flying at DriftSpeed x PlungeApproachSpeedScale; if it
        // has not arrived by PlungeApproachTeleportTick it blinks there (a flash at both ends) instead of giving up.
        private const float PlungeHeight = 160f;
        private const float PlungeApproachSpeedScale = 2.2f;
        private const int PlungeApproachTeleportTick = 40;
        private static readonly Color SpiritColor = new Color(185, 165, 230);

        private TravelMode _mode;
        private int _modeTimer, _modeDwell, _playerAirTicks, _stuckTicks;
        private Vector2 _stuckAnchor;
        private int _hoverSide = 1;
        private bool _initialized;
        private MoveStage _stage;
        private int _stageDuration;
        private bool _stageComplete;
        private Vector2 _aim, _landingPosition, _approachPoint, _wallExit, _wallInside;
        private float _dashDistance;
        private int _lockedFacing = 1;
        private int _cutCooldown, _lungeCooldown, _plungeCooldown, _ambushCooldown, _barrowBreathCooldown;
        private int _selectionTimer;
        private string _requestedCombo;
        private string _pendingCombo;
        private bool _bodyStrikeHit, _passingCutQueued;
        private AttackBag<WightMove> _moves;

        public int StrikeSequence
        {
            get; private set;
        }
        public bool BodyStrikeActive => Phase == AttackPhase.Custom
            && ((_stage == MoveStage.LungeDash && _stageDuration - PhaseTimer < LungeLiveTicks)
                || _stage == MoveStage.PlungeDrop || _stage == MoveStage.PlungeLanding);
        public bool LandingStrikeActive => Phase == AttackPhase.Custom && _stage == MoveStage.PlungeLanding;
        public Vector2 LandingStrikeCenter => _landingPosition + new Vector2(NPC.width * 0.5f, NPC.height);
        public Vector2 StrikeHand => PuppetHandPosition;
        public Vector2 StrikeTip => PuppetWeaponTipPosition(BladeReach);

        protected virtual bool IsNemesis => false;
        protected virtual float DriftSpeed => 2.8f;
        protected virtual float LungeDistanceCap => 320f;
        protected virtual int CutExpertDamage => 60; // Legacy contact 30, doubled by Expert before defense.
        protected override string InvaderTitle => "Barrow Wight";
        protected override bool AnnounceInvasion => false;
        protected override bool AnnounceInvaderDefeat => false;
        public override bool UsesPuppetDifficultyScaling => false;
        protected override int EstusChargesMax => 0;
        protected override int CasualStrollChance => 0;
        protected override bool DespawnsOnPartyWipe => false;
        protected override Color PuppetSkinColor => Color.Black;
        protected override int HeadArmorItemType => ModContent.ItemType<BarrowWightPhantomHelm>();
        protected override int BodyArmorItemType => ModContent.ItemType<BarrowWightPhantomArmor>();
        protected override int LegsArmorItemType => ModContent.ItemType<BarrowWightPhantomShroud>();
        protected override int MeleeWeaponItemType => ModContent.ItemType<EnemyBarrowWightSword>();
        protected override int RangedWeaponItemType => -1;
        protected override int RangedDamage => 0;
        protected override int MeleeDamage
        {
            get
            {
                float multiplier = 1f;
                if (_stage == MoveStage.LungeDash)
                {
                    multiplier = 1.3f;
                }
                if (_stage == MoveStage.PlungeDrop || _stage == MoveStage.PlungeLanding)
                {
                    multiplier = 1.4f;
                }
                return EnemyDamage.Projectile((int)MathF.Round(CutExpertDamage * multiplier));
            }
        }
        protected override WeaponArchetype MeleeArchetype => WeaponArchetype.Broadsword;
        // Same measured grip as the Phantom: (6,45) on the 52x52 diagonal sword, ~64px to its tip.
        protected override Vector2 MeleeHandleNorm => new Vector2(0.115f, 0.875f);
        protected override float ComboReachBase => 90f;
        protected override float MeleeBladeWidth => 16f;
        protected override bool ShowWeaponDuringNeutral => true;
        protected override float TopSpeed => 3f;
        protected override float Acceleration => 0.08f;
        protected override bool WalkCycleWhileAirborne => true;
        protected override bool WalkCycleUsesVerticalSpeed => true;
        protected override float MeleeRange => 0f;
        protected override float MeleeEngageRange => 72f;
        protected override float ComboMaxStartRange => 140f;
        // One server-owned bag picks both scripted moves and cuts. The shared sequencer executes cuts.
        protected override bool HoldAttackSelection => true;
        protected override bool UseSwingEasing => true;
        protected override bool UseAuthoredComboSwingClock => true;
        protected override bool UseLogicalMeleeTelegraphs => true;
        protected override bool UseCompositeArmSwing => true;
        protected override bool MirrorMeleeSwingRotationByFacing => true;
        protected override float ComboTelegraphMultiplier => 1f;
        protected override int MinComboTelegraphTicks => 20;
        protected override int MeleeComboInterStepLingerTicks => 3;
        protected override int MeleeRecoveryLingerTicks => 8;
        protected override float LogicalWindupSettleFraction => 0.25f;
        protected override bool SlowDownBeforeMelee => false;
        protected override bool SlowDownDuringCustom => false;
        protected override bool UseCompositeArmForAdditionalPhase => Phase == AttackPhase.Custom;
        protected override bool HasSlashVFX => true;
        protected override Color SlashVFXColor => SpiritColor;
        protected override float SlashVFXOpacity => 0.22f;
        protected override int OnKillDustType => DustID.DungeonSpirit;
        protected override Color OnKillDustColor => SpiritColor;
        protected override int OnKillDustCount => 45;

        protected override bool HasSpectralOverlay => true;
        protected override bool SpectralKeepsArmorDyes => true;
        protected override float SpectralOverlayScale => 1f;
        protected override Color SpectralOverlayColor => SpiritColor;
        protected override float SpectralCoreTintStrength => 0.12f;
        protected override float SpectralCoreOpacity
        {
            get
            {
                if (InsideTiles || _stage == MoveStage.WallSink || _stage == MoveStage.WallTravel || _stage == MoveStage.WallTell)
                {
                    return 0.2f;
                }
                return _mode == TravelMode.Walk ? 0.85f : 0.6f;
            }
        }
        protected override Color SpectralHaloColor => new Color(130, 160, 205);
        protected override int SpectralHaloCopyCount => 6;
        protected override float SpectralHaloRadius => 2f;
        protected override float SpectralHaloOpacity => 0.1f;
        protected override float SpectralHaloScale => 1f;
        protected override float SpectralTrailOpacity => IsNemesis ? 0.28f : 0.16f;

        // Timing: 220-degree envelope, ~177 degrees live; Weighted 10/0/40 k7 peaks at ~24 deg/t.
        // Damage ends at t17 (<22t roll). The 33t tail + 16t pause leave >30t before the second cut.
        // Both cuts share endpoints; the last has 33t tail + 36t recovery. No hyper armor.
        private static MeleeComboStep CutStep(ComboMotion motion, int tell, int pause = 0)
        {
            return new MeleeComboStep
            {
                Motion = motion,
                TelegraphTicks = tell,
                AttackTicks = CutAttackTicks,
                PostStepPause = pause,
                DamageMult = 1f,
                ReachMult = 1f,
                SwingSpeedMult = 1f,
                Ease = SwingEaseStyle.Weighted,
                EaseInTicks = 10,
                EaseOutTicks = 40,
                EaseOutDecay = 7f,
                HitWindowEnd = (10f + 40f * MathF.Log(1f / 0.3f) / 7f) / CutAttackTicks,
                LeapHeightMult = 1f,
                LeapForwardSpeedMult = 1f
            };
        }

        private static readonly MeleeCombo[] WightCombos =
        {
            new MeleeCombo
            {
                Name = CutName, BaseWeight = 100, Preferred = ComboRangeBand.Close,
                InitialFlashColor = SpiritColor, RecoveryTicks = 36, MoveBrake = 0.3f,
                Steps = new[] { CutStep(ComboMotion.UnderhandArc, 24, 16), CutStep(ComboMotion.OverheadArc, 0) }
            },
            new MeleeCombo
            {
                Name = PassingName, BaseWeight = 100, Preferred = ComboRangeBand.Any,
                InitialFlashColor = SpiritColor, RecoveryTicks = 40, MoveBrake = 0.3f,
                Steps = new[] { CutStep(ComboMotion.HorizontalSweep, 30) }
            },
            new MeleeCombo
            {
                Name = AmbushName, BaseWeight = 100, Preferred = ComboRangeBand.Any,
                InitialFlashColor = SpiritColor, RecoveryTicks = 45, MoveBrake = 0.3f,
                Steps = new[] { CutStep(ComboMotion.UnderhandArc, 30) }
            }
        };
        private static readonly MeleeCombo[] BasicCombos = { WightCombos[0] };
        protected override MeleeCombo[] MeleeComboPoolOverride => IsNemesis ? WightCombos : BasicCombos;

        protected override bool CanSelectMeleeCombo(MeleeCombo combo, float distance, float healthFraction)
        {
            return combo.Name == _requestedCombo && !InsideTiles
                && Collision.CanHitLine(NPC.Center, 1, 1, Main.player[NPC.target].Center, 1, 1);
        }

        protected override void CustomizeMeleeCombo(ref MeleeCombo combo, float healthFraction)
        {
            if (combo.Name == AmbushName)
            {
                MeleeComboStep step = combo.Steps[0];
                step.StepInDistance = 100f;
                combo.Steps[0] = step;
            }
            else if (combo.Name == PassingName)
            {
                MeleeComboStep step = combo.Steps[0];
                step.StepInDistance = 64f;
                combo.Steps[0] = step;
            }
            else if (combo.Name == CutName && _mode == TravelMode.Walk)
            {
                for (int i = 0; i < combo.Steps.Length; i++)
                {
                    MeleeComboStep step = combo.Steps[i];
                    step.StepInDistance = 25f;
                    combo.Steps[i] = step;
                }
            }
        }

        protected override bool ShouldContinueMeleeCombo(string comboName, int nextStepIndex, Player target, bool previousStepHit)
        {
            return comboName != CutName || NPC.Distance(target.Center) <= BladeReach * 1.5f;
        }

        protected override void ModifyMeleeArcEndpoints(ComboMotion motion, ref float startRotation, ref float endRotation)
        {
            startRotation = -1.64f;
            endRotation = 2.20f;
            if (motion == ComboMotion.UnderhandArc)
            {
                startRotation = 2.20f;
                endRotation = -1.64f;
            }
        }

        protected override void OnMeleeComboTelegraphTick(MeleeCombo combo, MeleeComboStep step, int elapsed, int total)
        {
            if (!Main.dedServ)
            {
                if (elapsed == 0)
                {
                    SoundEngine.PlaySound(SoundID.Item30 with
                    {
                        Pitch = -0.3f
                    }, NPC.Center);
                }
                Dust dust = Dust.NewDustPerfect(PuppetWeaponTipPosition(BladeReach), DustID.Frost,
                    Main.rand.NextVector2Circular(0.6f, 0.6f), 100, SpiritColor, 0.9f);
                dust.noGravity = true;
            }
        }

        protected override void DoComboMeleeHit(MeleeComboStep step)
        {
            if (!Main.dedServ)
            {
                SoundEngine.PlaySound(SoundID.Item1 with
                {
                    Pitch = -0.2f
                }, NPC.Center);
            }
            base.DoComboMeleeHit(step);
        }

        protected override void OnMeleeComboAttackTick(MeleeCombo combo, MeleeComboStep step, int elapsed, int total)
        {
            if (!Main.dedServ && elapsed < total * step.HitWindowEnd)
            {
                Dust dust = Dust.NewDustPerfect(PuppetHandPosition + PuppetWeaponDirection * Main.rand.NextFloat(16f, BladeReach),
                    DustID.Frost, NPC.velocity * 0.1f, 100, SpiritColor, 0.9f);
                dust.noGravity = true;
            }
        }

        protected override void DoMeleeAttack()
        {
        }
        protected override void DoRangedAttack()
        {
        }

        public virtual void OnHumanoidMeleeHit(Player target)
        {
            if (Main.rand.NextBool(2))
            {
                target.AddBuff(BuffID.BrokenArmor, 20 * 60);
                target.AddBuff(BuffID.Chilled, 20 * 60);
                target.AddBuff(ModContent.BuffType<CurseBuildup>(), 600 * 60);
            }
        }

        private bool InsideTiles => Collision.SolidCollision(NPC.position, NPC.width, NPC.height);
        private bool FreeToAct => Phase == AttackPhase.Idle || Phase == AttackPhase.CasualStroll || Phase == AttackPhase.ClosingDistance;

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[Type] = 1;
            NPCID.Sets.TrailCacheLength[Type] = 8;
            NPCID.Sets.TrailingMode[Type] = 1;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Poisoned] = true;
            if (IsNemesis)
            {
                NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Venom] = true;
            }
            else
            {
                NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Confused] = true;
                NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.OnFire] = true;
                NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.CursedInferno] = true;
            }
        }

        public override void SetDefaults()
        {
            NPC.aiStyle = -1;
            NPC.width = 20;
            NPC.height = 46;
            NPC.damage = 0; // Only authored sword/body-strike windows and breath damage the player.
            NPC.knockBackResist = 0.35f;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath6;
            NPC.GetGlobalNPC<tsorcRevampGlobalNPC>().CanPassThroughWalls = false;
        }

        public override void AI()
        {
            NPC.TargetClosest(false);
            if (!NPC.HasValidTarget)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient && (_stage != MoveStage.None || !FreeToAct))
                {
                    OnStagger(NPC);
                }
                NPC.velocity *= 0.95f;
                NPC.EncourageDespawn(10);
                ApplyTravelPhysics();
                NPC.hide = InsideTiles;
                return;
            }
            if (!_initialized && Main.netMode != NetmodeID.MultiplayerClient)
            {
                _initialized = true;
                _stuckAnchor = NPC.Center;
                _hoverSide = NPC.HasValidTarget && NPC.Center.X < Main.player[NPC.target].Center.X ? -1 : 1;
                TravelMode startingMode = TravelMode.Walk;
                if (InsideTiles || !TryFindFloor(NPC.Bottom, 96, out _))
                {
                    startingMode = TravelMode.Drift;
                }
                ChangeMode(startingMode);
            }

            _modeTimer++;
            _modeDwell++;
            if (_cutCooldown > 0)
            {
                _cutCooldown--;
            }
            if (_lungeCooldown > 0)
            {
                _lungeCooldown--;
            }
            if (_plungeCooldown > 0)
            {
                _plungeCooldown--;
            }
            if (_ambushCooldown > 0)
            {
                _ambushCooldown--;
            }
            if (_barrowBreathCooldown > 0)
            {
                _barrowBreathCooldown--;
            }
            ApplyTravelPhysics();

            if (Main.netMode != NetmodeID.MultiplayerClient && NPC.HasValidTarget)
            {
                Player target = Main.player[NPC.target];
                if (PlayerGrounded(target))
                {
                    _playerAirTicks = 0;
                }
                else
                {
                    _playerAirTicks++;
                }
                if (FreeToAct && _stage == MoveStage.None && !InFinisherPunishWindow
                    && NPC.GetGlobalNPC<tsorcRevampGlobalNPC>().StaggerTimer <= 0)
                {
                    UpdateTravelMode(target);
                    if (--_selectionTimer <= 0)
                    {
                        _selectionTimer = 15;
                        SelectMove(target);
                    }
                }
            }

            MoveStage stageBeforeTick = _stage;
            base.AI();
            if (Main.netMode != NetmodeID.MultiplayerClient && _stageComplete && _stage == stageBeforeTick)
            {
                _stageComplete = false;
                AdvanceStage();
            }
            ApplyTravelPhysics(); // Base movement/attack paths may write gravity; drift must never inherit it.
            NPC.hide = InsideTiles;

            if (!Main.dedServ)
            {
                Lighting.AddLight(NPC.Center, 0.4f, 0f, 0.25f);
                if (_mode != TravelMode.Walk && Main.GameUpdateCount % 4 == 0)
                {
                    Dust mist = Dust.NewDustPerfect(NPC.Bottom, DustID.Frost, new Vector2(0f, 0.5f), 140, SpiritColor, 0.8f);
                    mist.noGravity = true;
                }
            }
        }

        private void ChangeMode(TravelMode mode)
        {
            SmartFighter4AI.ReleaseRopeTraversal(NPC);
            _mode = mode;
            _modeTimer = 0;
            _modeDwell = 0;
            _stuckTicks = 0;
            _stuckAnchor = NPC.Center;
            NPC.GetGlobalNPC<tsorcRevampGlobalNPC>().UnreachableFrames = 0;
            ApplyTravelPhysics();
            NPC.netUpdate = true;
            RequestNetworkSnapshot();
        }

        private void ApplyTravelPhysics()
        {
            bool ghost = _mode != TravelMode.Walk;
            NPC.noTileCollide = ghost;
            NPC.noGravity = ghost;
        }

        private void UpdateTravelMode(Player target)
        {
            if (_mode == TravelMode.Rise && _modeTimer >= 24)
            {
                ChangeMode(TravelMode.Drift);
            }
            if (_mode == TravelMode.Settle && _modeTimer >= 90)
            {
                ChangeMode(TravelMode.Drift);
            }
            if (_modeTimer % 15 != 0)
            {
                return;
            }

            if (_mode == TravelMode.Walk)
            {
                _stuckTicks += 15;
                if (Vector2.DistanceSquared(NPC.Center, _stuckAnchor) >= 16f * 16f)
                {
                    _stuckAnchor = NPC.Center;
                    _stuckTicks = 0;
                }
                bool hasPlan = SmartFighter4AI.HasActiveMovementPlan(NPC);
                bool unreachable = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>().UnreachableFrames >= 45;
                bool highTarget = Math.Abs(target.Center.Y - NPC.Center.Y) > 112f && !hasPlan;
                bool airborneTarget = _playerAirTicks > 90 && target.Center.Y < NPC.Center.Y - 80f;
                bool restless = _modeDwell > 360 && !Collision.CanHitLine(NPC.Center, 1, 1, target.Center, 1, 1) && Main.rand.NextBool(6);
                if (InsideTiles || (_modeDwell >= 150 && (unreachable || highTarget || airborneTarget
                    || _stuckTicks >= 120 || NPC.Distance(target.Center) > 720f || restless)))
                {
                    ChangeMode(TravelMode.Rise);
                }
            }
            else if (_mode == TravelMode.Drift && _modeDwell >= 120 && PlayerGrounded(target)
                && Math.Abs(target.Center.X - NPC.Center.X) < 224f && !InsideTiles
                && TryFindFloor(NPC.Bottom, 32, out Vector2 floorPosition)
                && Math.Abs(floorPosition.Y + NPC.height - target.Bottom.Y) <= 48f)
            {
                ChangeMode(TravelMode.Settle);
            }
        }

        protected override void RunMovementAI(float speedMult)
        {
            if (Phase == AttackPhase.Custom)
            {
                return;
            }
            if (!FreeToAct)
            {
                NPC.velocity.X *= 0.8f;
                if (_mode != TravelMode.Walk)
                {
                    NPC.velocity.Y = 0f;
                }
                return;
            }
            if (_mode == TravelMode.Walk)
            {
                base.RunMovementAI(speedMult);
                return;
            }
            if (_mode == TravelMode.Rise)
            {
                NPC.velocity = Vector2.Lerp(NPC.velocity, new Vector2(0f, -1.5f), 0.15f);
                if (!Main.dedServ)
                {
                    if (_modeTimer == 1)
                    {
                        SoundEngine.PlaySound(SoundID.NPCDeath6 with
                        {
                            Volume = 0.4f
                        }, NPC.Center);
                    }
                    Dust.NewDustPerfect(NPC.Bottom, DustID.Frost, new Vector2(0f, 2f), 120, SpiritColor, 1.2f);
                }
                return;
            }
            if (_mode == TravelMode.Settle)
            {
                NPC.velocity = new Vector2(NPC.velocity.X * 0.8f, 1.5f);
                if (Main.netMode != NetmodeID.MultiplayerClient && !InsideTiles
                    && TryFindFloor(NPC.Bottom, 2, out Vector2 floorPosition)
                    && !Collision.SolidCollision(floorPosition, NPC.width, NPC.height))
                {
                    NPC.position = floorPosition;
                    NPC.velocity.Y = 0f;
                    ChangeMode(TravelMode.Walk);
                }
                return;
            }
            if (!NPC.HasValidTarget)
            {
                NPC.velocity *= 0.95f;
                return;
            }
            Player target = Main.player[NPC.target];
            float standoff = 120f;
            if (_cutCooldown <= 0 && NPC.Distance(target.Center) < 192f)
            {
                standoff = 56f;
            }
            Vector2 desired = target.Bottom + new Vector2(_hoverSide * standoff, -32f);
            desired.Y += MathF.Sin(_modeTimer * 0.06f) * 8f;
            SteerTo(desired, DriftSpeed);
            int facing = Math.Sign(target.Center.X - NPC.Center.X);
            if (facing != 0)
            {
                NPC.direction = NPC.spriteDirection = facing;
            }
        }

        private void SteerTo(Vector2 destination, float speed)
        {
            Vector2 difference = destination - NPC.Center;
            float arrivalSpeed = speed * Math.Min(1f, difference.Length() / 64f);
            Vector2 desiredVelocity = difference.SafeNormalize(Vector2.Zero) * arrivalSpeed;
            Vector2 correction = desiredVelocity - NPC.velocity;
            if (correction.Length() > 0.08f)
            {
                correction = correction.SafeNormalize(Vector2.Zero) * 0.08f;
            }
            NPC.velocity += correction;
        }

        // Collision.TileCollision includes one-way platforms, half blocks and slopes. Test the entire
        // destination body before returning: enabling tiles while embedded is never allowed.
        private bool TryFindFloor(Vector2 feet, int maxPixels, out Vector2 floorPosition)
        {
            floorPosition = Vector2.Zero;
            for (int i = 0; i <= maxPixels; i++)
            {
                Vector2 candidate = new Vector2(feet.X - NPC.width * 0.5f, feet.Y + i - NPC.height);
                if (candidate.X < 32f || candidate.Y < 32f || candidate.X + NPC.width > Main.maxTilesX * 16f - 32f
                    || candidate.Y + NPC.height > Main.maxTilesY * 16f - 32f
                    || Collision.SolidCollision(candidate, NPC.width, NPC.height))
                {
                    continue;
                }
                Vector2 allowed = Collision.TileCollision(candidate, new Vector2(0f, 2f), NPC.width, NPC.height);
                if (allowed.Y < 2f)
                {
                    candidate.Y += allowed.Y;
                    if (!Collision.SolidCollision(candidate, NPC.width, NPC.height))
                    {
                        floorPosition = candidate;
                        return true;
                    }
                }
            }
            return false;
        }

        private static bool PlayerGrounded(Player target)
        {
            return Math.Abs(target.velocity.Y) < 0.1f
                && Collision.TileCollision(target.position, new Vector2(0f, 2f), target.width, target.height).Y < 2f;
        }

        private void SelectMove(Player target)
        {
            if (_mode == TravelMode.Rise || _mode == TravelMode.Settle || InsideTiles)
            {
                return;
            }
            if (_pendingCombo != null)
            {
                _requestedCombo = _pendingCombo;
                _pendingCombo = null;
                if (NPC.Distance(target.Center) <= 140f && TryStartMeleeCombo(NPC.Distance(target.Center)))
                {
                    return;
                }
            }
            _moves ??= new AttackBag<WightMove>(cards =>
            {
                cards.Add(WightMove.Cut);
                cards.Add(WightMove.Cut);
                cards.Add(WightMove.Lunge);
                cards.Add(WightMove.Plunge);
                if (IsNemesis)
                {
                    cards.Add(WightMove.Ambush);
                    cards.Add(WightMove.Breath);
                }
            });
            float distance = NPC.Distance(target.Center);
            bool lineOfSight = Collision.CanHitLine(NPC.Center, 1, 1, target.Center, 1, 1);
            bool Eligible(WightMove move)
            {
                switch (move)
                {
                    case WightMove.Cut:
                        return _cutCooldown <= 0 && distance <= 72f && lineOfSight;
                    case WightMove.Lunge:
                        return _lungeCooldown <= 0 && distance >= 80f && distance <= 192f && lineOfSight;
                    case WightMove.Plunge:
                        return _plungeCooldown <= 0 && _mode == TravelMode.Drift && distance <= 240f
                        && PlayerGrounded(target) && FindPlunge(target);
                    case WightMove.Ambush:
                        return _ambushCooldown <= 0 && _mode == TravelMode.Drift && distance >= 96f
                        && distance <= 256f && (!lineOfSight || NearWall(target)) && FindWallExit(target);
                    case WightMove.Breath:
                        return _barrowBreathCooldown <= 0 && distance >= 96f && distance <= 320f && lineOfSight;
                    default:
                        return false;
                }
            }
            if (!_moves.TryDraw(Eligible, out WightMove chosen))
            {
                return;
            }
            _lockedFacing = target.Center.X < NPC.Center.X ? -1 : 1;
            _aim = (target.Center - NPC.Center).SafeNormalize(new Vector2(_lockedFacing, 0f));
            switch (chosen)
            {
                case WightMove.Cut:
                    _requestedCombo = CutName;
                    if (TryStartMeleeCombo(distance))
                    {
                        _cutCooldown = 24 + 2 * CutAttackTicks + 16 + 36 + 90;
                    }
                    break;
                case WightMove.Lunge:
                    _lungeCooldown = Main.rand.Next(300, 421);
                    ChangeMode(TravelMode.Rise);
                    BeginStage(MoveStage.LungeTell, 40);
                    break;
                case WightMove.Plunge:
                    FindPlunge(target);
                    _plungeCooldown = 480;
                    BeginStage(MoveStage.PlungeApproach, PlungeApproachTeleportTick + 5);
                    break;
                case WightMove.Ambush:
                    FindWallExit(target);
                    _ambushCooldown = 600;
                    BeginStage(MoveStage.WallSink, 20);
                    break;
                case WightMove.Breath:
                    _barrowBreathCooldown = 420;
                    BeginStage(MoveStage.BreathTell, 40);
                    break;
            }
        }

        private bool FindPlunge(Player target)
        {
            Vector2 above = new Vector2(target.Center.X - NPC.width * 0.5f, target.Bottom.Y - PlungeHeight - NPC.height);
            Vector2 descent = Collision.TileCollision(above, new Vector2(0f, PlungeHeight), NPC.width, NPC.height);
            if (Collision.SolidCollision(above, NPC.width, NPC.height + (int)PlungeHeight) || descent.Y < PlungeHeight - 1f
                || !TryFindFloor(target.Bottom, 16, out Vector2 landing))
            {
                return false;
            }
            _landingPosition = landing;
            _approachPoint = landing + new Vector2(NPC.width * 0.5f, NPC.height * 0.5f - PlungeHeight);
            return true;
        }

        private static bool NearWall(Player target)
        {
            return Collision.SolidCollision(target.position + new Vector2(-48f, 0f), 48, target.height)
                || Collision.SolidCollision(target.position + new Vector2(target.width, 0f), 48, target.height);
        }

        private bool FindWallExit(Player target)
        {
            for (int i = 0; i < 2; i++)
            {
                int side = _hoverSide;
                if (i == 1)
                {
                    side = -side;
                }
                for (int offset = 48; offset <= 96; offset += 16)
                {
                    Vector2 exit = target.Center + new Vector2(side * offset, -8f);
                    Vector2 position = exit - NPC.Size * 0.5f;
                    Vector2 inside = exit + new Vector2(side * 32f, 0f);
                    if (!Collision.SolidCollision(position, NPC.width, NPC.height)
                        && Collision.SolidCollision(inside - NPC.Size * 0.5f, NPC.width, NPC.height)
                        && Collision.CanHitLine(exit, 1, 1, target.Center, 1, 1))
                    {
                        _wallExit = exit;
                        _wallInside = inside;
                        return true;
                    }
                }
            }
            return false;
        }

        private void BeginStage(MoveStage stage, int duration)
        {
            _stage = stage;
            _stageDuration = duration;
            _stageComplete = false;
            NPC.direction = NPC.spriteDirection = _lockedFacing;
            StartCustomAttack(duration, MeleeWeaponItemType);
            DebugAttackLabel = stage.ToString();
            if (Main.netMode != NetmodeID.MultiplayerClient && (stage == MoveStage.LungeDash || stage == MoveStage.PlungeDrop))
            {
                StrikeSequence++;
                _bodyStrikeHit = false;
                Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center, Vector2.Zero,
                    ModContent.ProjectileType<BarrowWightStrike>(), MeleeDamage, 3f, Main.myPlayer,
                    NPC.whoAmI, StrikeSequence, NPC.type);
            }
            NPC.netUpdate = true;
            RequestNetworkSnapshot();
        }

        protected override float? CustomWeaponRotation
        {
            get
            {
                if (_stage == MoveStage.PlungeTell || _stage == MoveStage.PlungeDrop || _stage == MoveStage.PlungeLanding)
                {
                    return MathHelper.PiOver4 + MathHelper.PiOver2;
                }
                if (_stage == MoveStage.LungeTell || _stage == MoveStage.LungeDash)
                {
                    float angle = MathF.Atan2(_aim.Y, _aim.X * _lockedFacing) + MathHelper.PiOver4;
                    return angle;
                }
                if (_stage == MoveStage.BreathTell || _stage == MoveStage.BreathStream)
                {
                    return -1.55f;
                }
                if (_stage == MoveStage.Recovery)
                {
                    return 2.20f;
                }
                return -0.3f;
            }
        }

        protected override void DoCustomAttack()
        {
            if (!Main.dedServ && (_stage == MoveStage.LungeTell || _stage == MoveStage.PlungeTell || _stage == MoveStage.BreathTell))
            {
                SoundStyle cue = SoundID.NPCDeath6 with
                {
                    Volume = 0.55f,
                    Pitch = -0.25f
                };
                if (_stage == MoveStage.BreathTell)
                {
                    cue = SoundID.Item20;
                }
                SoundEngine.PlaySound(cue, NPC.Center);
            }
            if (!Main.dedServ && _stage == MoveStage.PlungeLanding)
            {
                SoundEngine.PlaySound(SoundID.DeerclopsStep, NPC.Bottom);
                UsefulFunctions.ScreenShake(NPC.Bottom, 3f, 12, distanceFalloff: 600f);
                for (int i = 0; i < 24; i++)
                {
                    Vector2 direction = Vector2.UnitX.RotatedBy(MathHelper.TwoPi * i / 24f);
                    Dust.NewDustPerfect(LandingStrikeCenter + direction * LandingRadius, DustID.Frost,
                        direction, 100, SpiritColor, 1.1f).noGravity = true;
                }
            }
        }

        protected override void DoCustomTick(int ticksRemaining)
        {
            int elapsed = _stageDuration - ticksRemaining;
            bool authority = Main.netMode != NetmodeID.MultiplayerClient;
            NPC.direction = NPC.spriteDirection = _lockedFacing;
            switch (_stage)
            {
                // Lunge: 40t tell, lock at t28; 6t cubic acceleration + 6t cruise + 30t k3 decay.
                // The first 21t are live, then 21t tail + 40t recovery. Distance is solved, never a fixed shove.
                case MoveStage.LungeTell:
                    NPC.velocity = new Vector2(NPC.velocity.X * 0.8f, elapsed < 24 ? -0.8f : 0f);
                    if (authority && elapsed <= 28 && NPC.HasValidTarget)
                    {
                        Vector2 difference = Main.player[NPC.target].Center - NPC.Center;
                        _aim = difference.SafeNormalize(new Vector2(_lockedFacing, 0f));
                        _lockedFacing = _aim.X < 0f ? -1 : 1;
                        _dashDistance = Math.Min(LungeDistanceCap, difference.Length() + 120f);
                        if (elapsed % 6 == 0)
                        {
                            RequestNetworkSnapshot();
                        }
                        if (elapsed == 28)
                        {
                            SpawnTelegraphFlash(SpiritColor);
                            RequestNetworkSnapshot();
                        }
                    }
                    break;
                case MoveStage.LungeDash:
                    float progressBefore = DashIntegral(elapsed);
                    float progressAfter = DashIntegral(elapsed + 1);
                    NPC.velocity = _aim * (_dashDistance * (progressAfter - progressBefore) / DashIntegral(LungeDashTicks));
                    if (elapsed >= LungeLiveTicks)
                    {
                        StopMeleeHit();
                    }
                    if (elapsed == 6 && !Main.dedServ)
                    {
                        SoundEngine.PlaySound(SoundID.Item1, NPC.Center);
                    }
                    break;
                case MoveStage.PlungeApproach:
                    // Keep re-aiming above where the player is now. A blocked column this tick keeps the last good point.
                    if (authority && NPC.HasValidTarget)
                    {
                        FindPlunge(Main.player[NPC.target]);
                    }

                    SteerTo(_approachPoint, DriftSpeed * PlungeApproachSpeedScale);
                    if (authority && NPC.Distance(_approachPoint) <= 8f)
                    {
                        PhaseTimer = 1;
                    }
                    else if (authority && elapsed >= PlungeApproachTeleportTick)
                    {
                        // Too slow to get there: blink above the player. The flashes mark both ends for every client.
                        SpawnTelegraphFlash(SpiritColor);
                        NPC.Center = _approachPoint;
                        NPC.velocity = Vector2.Zero;
                        SpawnTelegraphFlash(SpiritColor);
                        NPC.netUpdate = true;
                        RequestNetworkSnapshot();
                        PhaseTimer = 1;
                    }
                    break;
                // Plunge: 36t tell, tracks over the player until t24 then locks, PlungeHeight (160px) fall ~20t, then 4t live landing and 45t recovery.
                // The 48px landing radius is marked for the entire tell; a ceiling/platform cancels the approach.
                case MoveStage.PlungeTell:
                    NPC.velocity = Vector2.Zero;
                    if (authority && elapsed <= 24 && NPC.HasValidTarget)
                    {
                        Player target = Main.player[NPC.target];
                        // Stay over the player until the lock at t24 (35% of the gap per tick); after that the
                        // marked landing is final, leaving 12 tell ticks plus the fall to get out from under it.
                        if (FindPlunge(target))
                        {
                            NPC.Center = Vector2.Lerp(NPC.Center, _approachPoint, 0.35f);
                            if (elapsed % 6 == 0)
                            {
                                NPC.netUpdate = true;
                            }
                        }
                        if (elapsed == 24)
                        {
                            SpawnTelegraphFlash(SpiritColor);
                            RequestNetworkSnapshot();
                        }
                    }
                    break;
                case MoveStage.PlungeDrop:
                    NPC.velocity = new Vector2(0f, Math.Min(14f, NPC.velocity.Y + 0.8f));
                    if (authority && NPC.position.Y + NPC.velocity.Y >= _landingPosition.Y)
                    {
                        if (!Collision.SolidCollision(_landingPosition, NPC.width, NPC.height)
                            && TryFindFloor(_landingPosition + new Vector2(NPC.width * 0.5f, NPC.height), 2, out Vector2 floor))
                        {
                            _landingPosition = floor;
                            NPC.position = floor;
                            NPC.velocity = Vector2.Zero;
                            ChangeMode(TravelMode.Walk);
                            PhaseTimer = 1;
                        }
                        else
                        {
                            PhaseTimer = 1;
                            ChangeMode(TravelMode.Drift);
                        }
                    }
                    break;
                case MoveStage.PlungeLanding:
                    NPC.velocity = Vector2.Zero;
                    break;
                case MoveStage.WallSink:
                    NPC.velocity = (_wallInside - NPC.Center).SafeNormalize(Vector2.Zero) * Math.Min(3f, NPC.Distance(_wallInside));
                    break;
                case MoveStage.WallTravel:
                    NPC.velocity = (_wallInside - NPC.Center).SafeNormalize(Vector2.Zero) * Math.Min(8f, NPC.Distance(_wallInside));
                    if (authority && NPC.Distance(_wallInside) < 4f)
                    {
                        PhaseTimer = 1;
                    }
                    break;
                // Ambush: a fixed, server-chosen wall exit leaks mist for 36t before the harmless emergence.
                // The rising cut has its own 30t pose tell and <=140px total step, then 45t in-open recovery.
                case MoveStage.WallTell:
                    NPC.velocity = Vector2.Zero;
                    break;
                case MoveStage.WallEmerge:
                    NPC.velocity = (_wallExit - NPC.Center).SafeNormalize(Vector2.Zero) * Math.Min(4f, NPC.Distance(_wallExit));
                    break;
                // Breath: 40t inhale, aim locks at t30, then 45 puffs at 1/t and 15px/t in a 30-degree cone.
                // 30t particle lifetime = 450px; last 10t fade. Exposure adds exactly one Frost/t, no direct debuffs.
                case MoveStage.BreathTell:
                    NPC.velocity = Vector2.Zero;
                    if (authority && elapsed <= 30 && NPC.HasValidTarget)
                    {
                        _aim = (Main.player[NPC.target].Center - BreathOrigin).SafeNormalize(new Vector2(_lockedFacing, 0f));
                        _lockedFacing = _aim.X < 0f ? -1 : 1;
                        if (elapsed % 6 == 0)
                        {
                            RequestNetworkSnapshot();
                        }
                        if (elapsed == 30)
                        {
                            SpawnTelegraphFlash(Color.LightBlue);
                            RequestNetworkSnapshot();
                        }
                    }
                    break;
                case MoveStage.BreathStream:
                    NPC.velocity = Vector2.Zero;
                    if (authority)
                    {
                        float spread = MathHelper.ToRadians(-15f + elapsed % 15 * (30f / 14f));
                        Projectile.NewProjectile(NPC.GetSource_FromAI(), BreathOrigin, _aim.RotatedBy(spread) * 15f,
                            ModContent.ProjectileType<BarrowWightBreath>(), EnemyDamage.Projectile(140), 0f, Main.myPlayer);
                    }
                    break;
                case MoveStage.Recovery:
                    NPC.velocity.X *= 0.8f;
                    if (_mode != TravelMode.Walk)
                    {
                        NPC.velocity.Y = 0f;
                    }
                    break;
            }
            NPC.direction = NPC.spriteDirection = _lockedFacing;
            if (!Main.dedServ)
            {
                DrawStageDust(elapsed);
            }
            if (PhaseTimer == 1)
            {
                _stageComplete = true;
            }
        }

        private Vector2 BreathOrigin => NPC.Center + new Vector2(_lockedFacing * 6f, -14f);

        private static float DashIntegral(float ticks)
        {
            if (ticks <= 6f)
            {
                return ticks * ticks * ticks / 108f;
            }
            if (ticks <= 12f)
            {
                return 2f + ticks - 6f;
            }
            return 8f + 10f * (1f - MathF.Exp(-3f * (ticks - 12f) / 30f));
        }

        private void AdvanceStage()
        {
            switch (_stage)
            {
                case MoveStage.LungeTell:
                    ChangeMode(TravelMode.Drift);
                    BeginStage(MoveStage.LungeDash, LungeDashTicks);
                    break;
                case MoveStage.LungeDash:
                    _passingCutQueued = false;
                    if (IsNemesis && !_bodyStrikeHit && NPC.HasValidTarget)
                    {
                        Player target = Main.player[NPC.target];
                        _passingCutQueued = NPC.Distance(target.Center) <= 140f
                            && (target.Center.X - NPC.Center.X) * _lockedFacing < 0f;
                    }
                    BeginStage(MoveStage.Recovery, 40);
                    break;
                case MoveStage.PlungeApproach:
                    if (NPC.Distance(_approachPoint) <= 12f)
                    {
                        BeginStage(MoveStage.PlungeTell, 36);
                    }
                    else
                    {
                        BeginStage(MoveStage.Recovery, 20);
                    }
                    break;
                case MoveStage.PlungeTell:
                    NPC.velocity = Vector2.Zero;
                    BeginStage(MoveStage.PlungeDrop, 28);
                    break;
                case MoveStage.PlungeDrop:
                    if (_mode == TravelMode.Walk)
                    {
                        BeginStage(MoveStage.PlungeLanding, 4);
                    }
                    else
                    {
                        BeginStage(MoveStage.Recovery, 45);
                    }
                    break;
                case MoveStage.PlungeLanding:
                    BeginStage(MoveStage.Recovery, 45);
                    break;
                case MoveStage.WallSink:
                    BeginStage(MoveStage.WallTravel, 60);
                    break;
                case MoveStage.WallTravel:
                    if (NPC.Distance(_wallInside) <= 8f)
                    {
                        BeginStage(MoveStage.WallTell, 36);
                    }
                    else
                    {
                        BeginStage(MoveStage.Recovery, 45);
                    }
                    break;
                case MoveStage.WallTell:
                    BeginStage(MoveStage.WallEmerge, 12);
                    break;
                case MoveStage.WallEmerge:
                    _pendingCombo = AmbushName;
                    _stage = MoveStage.None;
                    _selectionTimer = 0;
                    RequestNetworkSnapshot();
                    break;
                case MoveStage.BreathTell:
                    BeginStage(MoveStage.BreathStream, 45);
                    break;
                case MoveStage.BreathStream:
                    BeginStage(MoveStage.Recovery, 40);
                    break;
                case MoveStage.Recovery:
                    _stage = MoveStage.None;
                    if (_passingCutQueued)
                    {
                        _pendingCombo = PassingName;
                    }
                    _passingCutQueued = false;
                    _selectionTimer = 0;
                    RequestNetworkSnapshot();
                    break;
            }
        }

        private void DrawStageDust(int elapsed)
        {
            if (_stage == MoveStage.LungeTell || (_stage == MoveStage.LungeDash && elapsed < LungeLiveTicks))
            {
                Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, DustID.Frost, -NPC.velocity.X * 0.1f,
                    -NPC.velocity.Y * 0.1f, 100, SpiritColor, 1.1f).noGravity = true;
            }
            if (_stage == MoveStage.PlungeTell)
            {
                Vector2 marker = LandingStrikeCenter + new Vector2(Main.rand.NextFloat(-LandingRadius, LandingRadius), -2f);
                Dust.NewDustPerfect(marker, DustID.Frost, new Vector2(0f, -0.8f), 90, SpiritColor, 1f).noGravity = true;
                Dust.NewDustPerfect(PuppetWeaponTipPosition(BladeReach), DustID.Frost, new Vector2(0f, 1.5f), 100).noGravity = true;
            }
            if (_stage == MoveStage.WallTell)
            {
                Dust.NewDustPerfect(_wallExit + Main.rand.NextVector2Circular(10f, 20f), DustID.Frost,
                    new Vector2(0f, -1.2f), 60, SpiritColor, 1.2f).noGravity = true;
            }
            if (_stage == MoveStage.BreathTell)
            {
                Vector2 offset = Main.rand.NextVector2CircularEdge(22f, 22f);
                Dust.NewDustPerfect(BreathOrigin + offset, DustID.Frost, -offset * 0.08f, 80, Color.LightBlue, 1.2f).noGravity = true;
                if (elapsed % 4 == 0)
                {
                    for (int i = 0; i < 2; i++)
                    {
                        Vector2 edge = _aim.RotatedBy(MathHelper.ToRadians(-15f + i * 30f));
                        for (int distance = 32; distance <= 128; distance += 32)
                        {
                            Dust.NewDustPerfect(BreathOrigin + edge * distance, DustID.Frost, Vector2.Zero,
                                170, Color.LightBlue, 0.65f).noGravity = true;
                        }
                    }
                }
            }
        }

        protected override void OnAttackHitReported()
        {
            if (_stage == MoveStage.LungeDash || _stage == MoveStage.Recovery)
            {
                _bodyStrikeHit = true;
                _passingCutQueued = false;
            }
        }

        public override void OnStagger(NPC npc)
        {
            _stage = MoveStage.None;
            _stageComplete = false;
            _pendingCombo = null;
            _passingCutQueued = false;
            StrikeSequence++;
            base.OnStagger(npc);
            ApplyTravelPhysics();
        }

        public override void DrawBehind(int index)
        {
            // Draw the actual puppet over tiles while embedded, rather than resurrecting its obsolete NPC sheet.
            if (InsideTiles)
            {
                Main.instance.DrawCacheNPCsOverPlayers.Add(index);
            }
        }

        public override void HitEffect(NPC.HitInfo hit)
        {
            base.HitEffect(hit);
            if (NPC.life > 0 || Main.dedServ)
            {
                return;
            }
            Gore.NewGore(NPC.GetSource_Death(), NPC.position, Main.rand.NextVector2Circular(4f, 4f), Mod.Find<ModGore>("Barrow Wight Gore 1").Type);
            for (int i = 0; i < 2; i++)
            {
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, Main.rand.NextVector2Circular(4f, 4f), Mod.Find<ModGore>("Barrow Wight Gore 2").Type);
            }
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            base.SendExtraAI(writer);
            writer.Write(_initialized);
            writer.Write((byte)_mode);
            writer.Write(_modeTimer);
            writer.Write(_modeDwell);
            writer.Write(_playerAirTicks);
            writer.Write(_stuckTicks);
            writer.WriteVector2(_stuckAnchor);
            writer.Write((sbyte)_hoverSide);
            writer.Write((byte)_stage);
            writer.Write(_stageDuration);
            writer.Write((sbyte)_lockedFacing);
            writer.WriteVector2(_aim);
            writer.Write(_dashDistance);
            writer.WriteVector2(_landingPosition);
            writer.WriteVector2(_approachPoint);
            writer.WriteVector2(_wallExit);
            writer.WriteVector2(_wallInside);
            writer.Write(StrikeSequence);
            writer.Write(_cutCooldown);
            writer.Write(_lungeCooldown);
            writer.Write(_plungeCooldown);
            writer.Write(_ambushCooldown);
            writer.Write(_barrowBreathCooldown);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            TravelMode previousMode = _mode;
            base.ReceiveExtraAI(reader);
            _initialized = reader.ReadBoolean();
            _mode = (TravelMode)reader.ReadByte();
            _modeTimer = reader.ReadInt32();
            _modeDwell = reader.ReadInt32();
            _playerAirTicks = reader.ReadInt32();
            _stuckTicks = reader.ReadInt32();
            _stuckAnchor = reader.ReadVector2();
            _hoverSide = reader.ReadSByte();
            _stage = (MoveStage)reader.ReadByte();
            _stageDuration = reader.ReadInt32();
            _lockedFacing = reader.ReadSByte();
            _aim = reader.ReadVector2();
            _dashDistance = reader.ReadSingle();
            _landingPosition = reader.ReadVector2();
            _approachPoint = reader.ReadVector2();
            _wallExit = reader.ReadVector2();
            _wallInside = reader.ReadVector2();
            StrikeSequence = reader.ReadInt32();
            _cutCooldown = reader.ReadInt32();
            _lungeCooldown = reader.ReadInt32();
            _plungeCooldown = reader.ReadInt32();
            _ambushCooldown = reader.ReadInt32();
            _barrowBreathCooldown = reader.ReadInt32();
            _stageComplete = false;
            if (previousMode != _mode)
            {
                SmartFighter4AI.ReleaseRopeTraversal(NPC);
            }
            ApplyTravelPhysics();
        }
    }
}

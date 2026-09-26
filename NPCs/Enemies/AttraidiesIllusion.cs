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
using tsorcRevamp.Content.Items.Accessories.Magic;
using tsorcRevamp.Content.Items.Potions;
using tsorcRevamp.Content.Items.Potions.RadiantLifegem;
using tsorcRevamp.Content.Items.Potions.StarlightShard;
using tsorcRevamp.Content.Items.Weapons.Magic;
using tsorcRevamp.Content.Projectiles.Enemy.Attraidies;
using tsorcRevamp.NPCs.AI;
using tsorcRevamp.NPCs.Puppets;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.NPCs.Enemies
{
    class AttraidiesIllusion : PuppetNPC
    {
        // Attack spec: staff-only caster. Oracle Volley 40t tell / 5x12t shots / 32t recovery;
        // Oracle Fan 36t / 3 simultaneous shots over 40 degrees / 40t recovery;
        // Crystal 60t / 30t harmless portal formation + bounded pursuit / 48t recovery;
        // Mirrorstep 40t departure + fresh 40t tell / volley + one delayed echo shot / 60t recovery.
        // Raw projectile damage stays 7 (Oracle) and 12 (crystal), with the original debuffs.
        // The dedicated projectiles reuse existing art; their full lifecycles are independent of shared spells.
        private enum Spell : byte { OracleVolley, OracleFan, SuspendedCrystal, Mirrorstep, EchoShot, RuneBlade }
        private readonly struct SpellSpec
        {
            public readonly string Name;
            public readonly int Tell, Active, Recovery;
            public SpellSpec(string name, int tell, int active, int recovery)
            { Name = name; Tell = tell; Active = active; Recovery = recovery; }
        }
        // New ranged staff attacks belong here, in the bag, and in the release dispatcher.
        private static readonly SpellSpec[] Spells = {
            new SpellSpec("Oracle Volley", 40, 49, 32),
            new SpellSpec("Oracle Fan", 36, 1, 40),
            new SpellSpec("Suspended Crystal", 60, 1, 48),
            new SpellSpec("Mirrorstep Volley", 40, 49, 60),
            new SpellSpec("Mirror Echo", 40, 1, 24),
            new SpellSpec("Rune Blade Conjuration", RuneBladeConjuration.TellTicks, 96, 60),
        };
        private readonly List<Spell> _spellBag = new List<Spell>();
        private Spell _spell, _lastSpell = Spell.EchoShot;
        private Vector2 _aim = Vector2.UnitX, _portal, _blinkDestination, _echoAnchor;
        private bool _aimLocked, _blinkPending, _initialized, _echoFired;
        private int _facing = 1, _neutralDelay = 30, _blinkCooldown = 180, _crystalCooldown;
        private int _ownerIndex = -1, _echoIndex = -1, _releasedShots, _flightFailureTicks, _echoAge;
        private Player _staffPose;
        private bool _runeFirstUnderhand;
        private int _runeCount = 1, _runeCooldown;
        // Rune approach: low hover over real floor, 5px/t closing; 3.2px/t advancing cast.
        // Start tells at <=96px, re-check the actual 128px blade sweep on EVERY release.
        // Live cut remains15t (<22 roll i-frames), with locked facing/stride and no vertical homing.
        private const float RuneApproachSpeed = RuneBladeConjuration.ApproachSpeed;
        private const float RuneCastSpeed = RuneBladeConjuration.CastSpeed;
        private const float RuneStartDistance = RuneBladeConjuration.StartDistance;
        private const int RuneApproachTimeout = 180;
        private bool _runeApproach, _runeLowHover;
        private int _runeApproachTicks;
        private float _runeHoverY, _runeStride, _runeDesiredX, _runeRecoveryPose = RuneBladeConjuration.Carry;
        private static readonly Color RuneColor = new Color(65, 165, 255);
        private bool IsRune => _spell == Spell.RuneBlade;
        private static readonly Color OracleColor = new Color(135, 230, 65);
        private static readonly Color CrystalColor = new Color(180, 80, 240);
        private bool IsEcho => NPC.GetGlobalNPC<tsorcRevampGlobalNPC>().IsTeleportIllusion;
        private SpellSpec CurrentSpell => Spells[(int)_spell];
        private bool IsNeutral => Phase == AttackPhase.Idle || Phase == AttackPhase.CasualStroll;
        public override bool UsesPuppetDifficultyScaling => false;
        protected override bool AnnounceInvasion => false;
        protected override bool DespawnsOnPartyWipe => false;
        protected override string InvaderTitle => "Attraidies Illusion";
        protected override int HeadArmorItemType => ItemID.BossMaskMoonlord;
        protected override int BodyArmorItemType => ItemID.WhiteLunaticRobe;
        // Solar Cultist Robe supplies its own skirt; a separate leg item would cover it.
        protected override int LegsArmorItemType => 0;
        protected override int MeleeWeaponItemType => -1;
        protected override int RangedWeaponItemType => -1;
        protected override int MagicWeaponItemType => ModContent.ItemType<FarronDart>();
        protected override int MeleeDamage => 0;
        protected override int RangedDamage => 0;
        protected override int MagicDamage => 7;
        protected override int EstusChargesMax => 0;
        protected override int OnKillDustCount => 0; // retain the existing salmon death burst below
        protected override int CasualStrollChance => 0;
        // Subclass owns neutral selection only; the base owns every cast phase and its network clock.
        protected override bool HoldAttackSelection => true;
        protected override bool ShowWeaponDuringNeutral => true;
        protected override float TeleportIllusionOpacity => MathHelper.Clamp(_echoAge / 15f, 0f, 1f) * 0.45f
            * (_echoFired ? MathHelper.Clamp(_neutralDelay / 20f, 0f, 1f) : 1f);
        protected override float TopSpeed => 1.8f;
        protected override float Acceleration => 0.08f;
        protected override float RunSpeedMult => 1f;
        protected override float PuppetJumpPower => 7.5f;
        protected override float PuppetJumpBoost => 3f;
        protected override float MagicRange => 620f;
        protected override int MagicTelegraphTicks => CurrentSpell.Tell;
        protected override int MagicAttackTicks => IsRune ? RuneBladeConjuration.ActiveTicks(_runeCount) : CurrentSpell.Active;
        protected override int MagicRecoveryTicks => CurrentSpell.Recovery;
        protected override int MagicCooldownAfterUse => 0;
        protected override bool BrakeDuringMagicCast => !IsRune && (_spell == Spell.SuspendedCrystal || IsEcho);
        protected override bool UseAuthoredMagicCastPose => true;
        protected override float MagicCastStartRotation => _spell == Spell.Mirrorstep ? -1.2f : -0.3f;
        protected override float MagicCastEndRotation => CastRotation;
        protected override float MagicWeaponRotationOffset => MathHelper.PiOver4;
        protected override bool MirrorMagicWeaponRotationByFacing => true;
        protected override int MagicWeaponRecoveryHoldTicks => MagicRecoveryTicks;
        protected override Color MagicTelegraphFlashColor => IsRune ? RuneColor : _spell == Spell.SuspendedCrystal ? CrystalColor : OracleColor;
        protected override int MagicTelegraphFlashLeadTicks => MagicTelegraphTicks;
        protected override bool UseCompositeArmForAdditionalPhase => true;
        protected override bool UseCompositeArmSwing => true;
        protected override Vector2 MagicGripNorm => new Vector2(0.16f, 0.84f);
        protected override float GetHeldRangedDrawScale(int itemType)
            => itemType == MagicWeaponItemType ? 0.9f : base.GetHeldRangedDrawScale(itemType);
        protected override bool HasWings => !IsEcho;
        // Reuse the flight controller without adding visible wings to the requested armor set.
        protected override int WingsAccessoryItemType => 0;
        protected override bool ShowWingsWhenGrounded => false;
        protected override int RandomTakeoffChance => 0;
        protected override float FlightHeightTrigger => float.MaxValue;
        protected override bool CanUseAerialMelee => false;
        protected override EnemyFlightConfig FlightConfig {
            get {
                EnemyFlightConfig config = EnemyFlightConfig.Default;
                config.HoverAltitude = 128f; config.HoverSideOffset = 176f;
                config.HoverTopSpeed = 3f; config.TakeOffSpeed = 5f;
                config.MaxFlightTicks = 180; config.CooldownTicks = 360;
                config.TakeOffTicks = 24; config.HoverDwellTicks = 180;
                config.LandTicks = 60; config.LandSpeed = 3f;
                return config;
            }
        }
        protected override int TeleportTelegraphTicks => 40;
        protected override int TeleportDustTypeId => DustID.ShadowbeamStaff;
        protected override Color TeleportDustTint => CrystalColor;

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[Type] = 1;
            NPCID.Sets.NeedsExpertScaling[Type] = false;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Confused] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Poisoned] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.OnFire] = true;
        }
        public override void SetDefaults()
        {
            NPC.aiStyle = -1;
            NPC.npcSlots = 5; NPC.lifeMax = 400; NPC.damage = 0; NPC.scale = 1f;
            NPC.knockBackResist = 0.3f; NPC.value = 5000; NPC.defense = 10;
            NPC.height = 44; NPC.width = 28;
            NPC.HitSound = SoundID.NPCHit48; NPC.DeathSound = SoundID.NPCDeath58;
            NPC.noGravity = false; NPC.noTileCollide = false; NPC.lavaImmune = true;
            Banner = Type; BannerItem = ModContent.ItemType<Banners.AttraidiesIllusionBanner>();
            tsorcRevampGlobalNPC g = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            g.PoiseStaggerResetsAI = true;
            // Poise 30 and the unchanged 0.3 knockback resistance are registered centrally.
            g.NavSearchRadius = 50; g.RemembersLastKnownPos = true;
            g.CanWalkBackwards = true; g.KiteRangeMin = 8f; g.KiteRangeMax = 18f; g.KiteLooseness = 0.25f;
            // Blink recovery is scheduled below, after jumps and flight have had a chance to solve terrain.
            g.CanTeleport = false; g.TeleportStyle = TeleportStyle.RecoveryOnly;
            g.TeleportVisualStyle = TeleportVisualStyle.MagicIllusion;
            g.TeleportAppearanceDelay = 0; g.TeleportArrivalMistTime = 20;
        }

        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            float chance = 0;
            if (spawnInfo.Player.ZoneDungeon && NPC.CountNPCS(ModContent.NPCType<AttraidiesIllusion>()) < 1 && NPC.CountNPCS(ModContent.NPCType<AttraidiesManifestation>()) < 1
                && NPC.CountNPCS(ModContent.NPCType<JungleWyvernJuvenile.JungleWyvernJuvenileHead>()) < 1 && NPC.CountNPCS(ModContent.NPCType<DungeonMage>()) < 1)
            {
                if (!Main.hardMode) { chance = .02f; }
                else chance = .0125f;

            }

            if (spawnInfo.Player.ZoneUnderworldHeight && !Main.hardMode && !NPC.AnyNPCs(ModContent.NPCType<AttraidiesIllusion>()))
            {
                chance = .033f;
            }

            if (spawnInfo.Player.ZoneRockLayerHeight && Main.hardMode && !tsorcRevampWorld.SuperHardMode) // was jungle only
            {
                chance = .00525f;
            }

            return chance;
        }




        protected override void RunMovementAI(float speedMult)
        {
            if (_runeApproach || IsRune && (Phase == AttackPhase.MagicTelegraph || Phase == AttackPhase.MagicAttack)) return;
            if (IsEcho && Vector2.Distance(NPC.Center, _echoAnchor) > 64f)
            { NPC.velocity.X *= 0.7f; return; }
            if (Phase == AttackPhase.MagicAttack || (_aimLocked && Phase == AttackPhase.MagicTelegraph)
                || (_spell == Spell.SuspendedCrystal && Phase == AttackPhase.MagicTelegraph))
            { NPC.velocity.X *= 0.7f; return; }
            SmartFighter4AI.Run(NPC, TopSpeed * speedMult * (IsNeutral ? 1f : 0.6f),
                Acceleration, doorBreakingDamage: 0, attackRange: MagicRange);
        }

        public override void AI()
        {
            tsorcRevampGlobalNPC g = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            if (!_initialized)
            {
                _initialized = true;
                SetDisplayWeapon(MagicWeaponItemType, false);
                if (Main.netMode != NetmodeID.MultiplayerClient) NPC.netUpdate = true;
            }
            if (_neutralDelay > 0 && g.StaggerTimer <= 0) _neutralDelay--;
            if (_blinkCooldown > 0) _blinkCooldown--;
            if (_crystalCooldown > 0) _crystalCooldown--;
            if (_runeCooldown > 0) _runeCooldown--;
            bool server = Main.netMode != NetmodeID.MultiplayerClient;
            if (IsEcho) _echoAge++;
            if (server && IsEcho && (!OwnerAlive || _echoFired && _neutralDelay <= 0))
            { DespawnEcho(); return; }
            if (server && _blinkPending && g.TeleportCountdown == 1
                && !SafeDestination(_blinkDestination, requireGround: true))
            {
                CancelBlink();
                EnterPhase(AttackPhase.MagicRecovery, 40);
                NPC.netUpdate = true;
            }
            if (server && Phase == AttackPhase.MagicTelegraph && !_blinkPending)
            {
                if (!_aimLocked && NPC.HasValidTarget)
                {
                    Player target = Main.player[NPC.target];
                    _facing = target.Center.X >= NPC.Center.X ? 1 : -1;
                    _aim = (target.Center + target.velocity * 9f - NPC.Center).SafeNormalize(Vector2.UnitX * _facing);
                    if (PhaseTimer <= 12) { _aimLocked = true; NPC.netUpdate = true; }
                    else if (PhaseTimer % 6 == 0) NPC.netUpdate = true;
                }
            }
            if (!IsNeutral) NPC.direction = NPC.spriteDirection = _facing;
            AttackPhase previousPhase = Phase;
            base.AI();
            if (!NPC.active) return;
            UpdateRuneMovement(server, g);
            SetDisplayWeapon(MagicWeaponItemType, false);
            if (!IsNeutral) NPC.direction = NPC.spriteDirection = _facing;
            if (Flight != null && Flight.Mode == FlightMode.Hover
                && (Phase == AttackPhase.MagicAttack || Phase == AttackPhase.MagicTelegraph
                    && (_aimLocked || _spell == Spell.SuspendedCrystal))) NPC.velocity = Vector2.Zero;
            if (server && _blinkPending && g.TeleportCountdown == 0 && g.TeleportAppearanceTimer == 0)
            { _blinkPending = false; _aimLocked = false; NPC.netUpdate = true; }
            if (server && previousPhase == AttackPhase.MagicRecovery && IsNeutral)
            {
                _neutralDelay = IsEcho ? 20 : 30;
                if (IsEcho) _echoFired = true;
                if (Flight != null && Flight.IsAirborne) Flight.RequestLand();
                NPC.netUpdate = true;
            }
            // Keep tell cancellation open, then protect the final lock and release. Recovery is always vulnerable.
            bool runeRepeatTell = IsRune && Phase == AttackPhase.MagicAttack
                && (MagicAttackTicks - PhaseTimer) % RuneBladeConjuration.StepTicks >= 96;
            bool runeLiveBeat = !IsRune || (MagicAttackTicks - PhaseTimer) % RuneBladeConjuration.StepTicks < 55;
            g.AttackCommitted = (Phase == AttackPhase.MagicTelegraph && PhaseTimer <= 8 && !_blinkPending)
                || Phase == AttackPhase.MagicAttack && !runeRepeatTell && runeLiveBeat;
            g.AttackTelegraphing = (Phase == AttackPhase.MagicTelegraph || _blinkPending || runeRepeatTell) && !g.AttackCommitted;
            if (g.StaggerTimer > 0) { g.AttackCommitted = g.AttackTelegraphing = false; }
            if (server && IsNeutral && g.StaggerTimer <= 0 && !_blinkPending)
                SelectNeutralAction();
            if (!Main.dedServ) Decorate(g);
        }

        private bool OwnerAlive => _ownerIndex >= 0 && _ownerIndex < Main.maxNPCs
            && Main.npc[_ownerIndex].active && Main.npc[_ownerIndex].ModNPC is AttraidiesIllusion owner && !owner.IsEcho;
        private void DespawnEcho()
        {
            Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center, Vector2.Zero,
                ModContent.ProjectileType<Content.Projectiles.VFX.TeleportIllusionDissolve>(), 0, 0f, Main.myPlayer);
            NPC.active = false;
            if (Main.netMode == NetmodeID.Server) NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, NPC.whoAmI);
        }

        private void SelectNeutralAction()
        {
            if (_neutralDelay > 0 || !NPC.HasValidTarget) return;
            if (IsEcho)
            {
                if (!_echoFired) BeginSpell(Spell.EchoShot);
                return;
            }
            Player target = Main.player[NPC.target];
            if (!target.active || target.dead) return;
            if (_runeApproach) return;
            if (_echoIndex >= 0 && _echoIndex < Main.maxNPCs && Main.npc[_echoIndex].active
                && Main.npc[_echoIndex].ModNPC is AttraidiesIllusion echo && echo._ownerIndex == NPC.whoAmI) return;
            bool clear = Collision.CanHitLine(NPC.Center, 1, 1, target.Center, 1, 1);
            if (!clear) _flightFailureTicks++; else _flightFailureTicks = 0;
            bool canFly = Flight != null && !Flight.IsAirborne && Flight.CooldownRemaining <= 0;
            if (canFly && (target.Center.Y < NPC.Center.Y - 96f || _flightFailureTicks >= 90)
                && !Collision.SolidCollision(NPC.position - new Vector2(0f, 64f), NPC.width, NPC.height + 64))
            {
                if (Flight.RequestTakeoff()) { NPC.netUpdate = true; return; }
            }
            if (Flight != null && Flight.IsAirborne && Flight.Mode != FlightMode.Hover) return;
            if (!clear)
            {
                if (_flightFailureTicks >= 180 && _blinkCooldown == 0 && (Flight == null || !Flight.IsAirborne)
                    && TryFindBlinkDestination(target, out Vector2 destination))
                {
                    // Harmless navigation recovery: same departure/arrival ceremony, no combat duplicate.
                    NPC.GetGlobalNPC<tsorcRevampGlobalNPC>().TeleportVisualStyle = TeleportVisualStyle.GreySmoke;
                    if (tsorcRevampAIs.QueueTeleportToDestination(NPC, destination, 40))
                    { _blinkCooldown = 600; _neutralDelay = 80; NPC.netUpdate = true; }
                }
                return;
            }
            float distance = NPC.Distance(target.Center);
            if (distance > MagicRange) return;
            if (_spellBag.Count == 0)
            {
                _spellBag.AddRange(new[] { Spell.OracleVolley, Spell.OracleFan, Spell.SuspendedCrystal, Spell.Mirrorstep, Spell.RuneBlade });
                for (int i = _spellBag.Count - 1; i > 0; i--)
                { int j = Main.rand.Next(i + 1); Spell swap = _spellBag[i]; _spellBag[i] = _spellBag[j]; _spellBag[j] = swap; }
            }
            for (int i = _spellBag.Count - 1; i >= 0; i--)
            {
                Spell candidate = _spellBag[i];
                if (candidate == _lastSpell) continue;
                if (candidate == Spell.RuneBlade && (_runeCooldown > 0 || distance > 480f
                    || Math.Abs(target.Center.Y - NPC.Center.Y) > 80f
                    || Flight != null && Flight.IsAirborne)) continue;
                if (candidate == Spell.OracleFan && distance > 240f) continue;
                if ((candidate == Spell.OracleVolley || candidate == Spell.Mirrorstep) && distance < 128f) continue;
                if (candidate == Spell.SuspendedCrystal && (_crystalCooldown > 0 || distance < 160f
                    || HasCrystal() || !TryFindPortal(target, out _portal))) continue;
                if (candidate == Spell.Mirrorstep && (_blinkCooldown > 0 || NPC.velocity.Y != 0f
                    || (Flight != null && Flight.IsAirborne) || !TryFindBlinkDestination(target, out _blinkDestination))) continue;
                _spellBag.RemoveAt(i); _lastSpell = candidate;
                if (candidate == Spell.RuneBlade)
                {
                    _runeApproach = true; _runeApproachTicks = 0;
                    _runeLowHover = TryRuneHoverFloor(NPC.Center.X, out _runeHoverY);
                    _facing = target.Center.X >= NPC.Center.X ? 1 : -1;
                    DebugAttackLabel = "Rune Blade: closing distance";
                    NPC.netUpdate = true; return;
                }
                BeginSpell(candidate); return;
            }
            // Retain gated entries without starving the ordinary casts. Prefer the other basic
            // formation; repeat only when spacing leaves a single viable answer (e.g. point blank).
            Spell fallback = distance < 128f || distance <= 240f && _lastSpell == Spell.OracleVolley
                ? Spell.OracleFan : Spell.OracleVolley;
            _spellBag.Remove(fallback); _lastSpell = fallback; BeginSpell(fallback);
        }

        private void BeginSpell(Spell spell)
        {
            _spell = spell; _aimLocked = false; _releasedShots = 0;
            Player target = Main.player[NPC.target];
            _facing = target.Center.X >= NPC.Center.X ? 1 : -1;
            NPC.direction = NPC.spriteDirection = _facing;
            _aim = (target.Center + target.velocity * 9f - NPC.Center).SafeNormalize(Vector2.UnitX * _facing);
            DebugAttackLabel = CurrentSpell.Name;
            SetDisplayWeapon(MagicWeaponItemType, false);
            if (spell == Spell.Mirrorstep)
            {
                tsorcRevampGlobalNPC g = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
                g.TeleportVisualStyle = TeleportVisualStyle.MagicIllusion;
                _blinkPending = tsorcRevampAIs.QueueTeleportToDestination(NPC, _blinkDestination, 40);
                _blinkCooldown = NPC.life <= NPC.lifeMax * 0.33f ? 360 : NPC.life <= NPC.lifeMax * 0.75f ? 480 : 600;
                if (!_blinkPending) _spell = Spell.OracleVolley;
            }
            if (spell == Spell.SuspendedCrystal) _crystalCooldown = 300;
            if (spell == Spell.RuneBlade)
            {
                _runeRecoveryPose = RuneBladeConjuration.Carry;
                _runeFirstUnderhand = Main.rand.NextBool();
                _runeCount = Main.rand.Next(1, NPC.life <= NPC.lifeMax * 0.33f ? 5 : 3);
                _runeCooldown = 300;
            }
            EnterPhase(AttackPhase.MagicTelegraph, MagicTelegraphTicks);
            NPC.netUpdate = true;
        }

        protected override void DoMagicAttack()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient) return;
            if (!NPC.HasValidTarget) return;
            _aimLocked = true;
            if (IsRune) { NPC.netUpdate = true; return; } // staff gesture first; the blade follows at tick 40
            if (_spell == Spell.SuspendedCrystal)
            {
                if (PortalSafe(_portal))
                    Projectile.NewProjectile(NPC.GetSource_FromAI(), _portal,
                        (Main.player[NPC.target].Center - _portal).SafeNormalize(Vector2.UnitY) * 2f,
                        ModContent.ProjectileType<IllusionCrystal>(), 12, 0f, Main.myPlayer, NPC.whoAmI, NPC.target);
            }
            else if (_spell == Spell.OracleFan)
            { for (int i = -1; i <= 1; i++) FireOracle(_aim.RotatedBy(MathHelper.ToRadians(i * 20f)) * 5f); }
            else { FireOracle(_aim * 6f); _releasedShots = 1; }
            NPC.netUpdate = true;
        }
        protected override void DoMagicTick(int ticksRemaining)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient) return;
            int elapsed = MagicAttackTicks - ticksRemaining;
            if (IsRune)
            {
                int step = elapsed / RuneBladeConjuration.StepTicks;
                int tick = elapsed % RuneBladeConjuration.StepTicks;
                if (tick == 96 && NPC.HasValidTarget)
                {
                    // Re-face only in the harmless repeat wind-up, never while a blade is live.
                    _facing = Main.player[NPC.target].Center.X >= NPC.Center.X ? 1 : -1;
                    NPC.direction = NPC.spriteDirection = _facing;
                    NPC.netUpdate = true;
                }
                if (tick == RuneBladeConjuration.SweepTicks && step < _runeCount)
                {
                    bool underhand = RuneBladeConjuration.Underhand(_runeFirstUnderhand, step);
                    if (!RuneCanReach(underhand))
                    {
                        // Never conjure a guaranteed whiff; disengagement can cancel the remaining cuts.
                        _runeLowHover = false; NPC.noGravity = false;
                        _runeRecoveryPose = RuneBladeConjuration.End(underhand);
                        EnterPhase(AttackPhase.MagicRecovery, MagicRecoveryTicks);
                        NPC.netUpdate = true; return;
                    }
                    _runeStride = (Main.player[NPC.target].Center.X - NPC.Center.X) * _facing > 72f ? RuneCastSpeed * _facing : 0f;
                    NPC.netUpdate = true;
                    Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center + RuneBladeConjuration.Offset(underhand, _facing),
                        Vector2.Zero, ModContent.ProjectileType<IllusionRuneBlade>(), 12, 0f, Main.myPlayer,
                        NPC.whoAmI, _facing * (underhand ? 2 : 1));
                }
                return;
            }
            if ((_spell == Spell.OracleVolley || _spell == Spell.Mirrorstep)
                && elapsed > 0 && elapsed % 12 == 0 && _releasedShots < 5)
            { FireOracle(_aim * 6f); _releasedShots++; NPC.netUpdate = true; }
        }
        private void FireOracle(Vector2 velocity)
        {
            // A projectile is born exactly at the displayed tip; one shared pure pose computation is used on all peers.
            Projectile.NewProjectile(NPC.GetSource_FromAI(), StaffTipWorld,
                velocity, ModContent.ProjectileType<IllusionOracle>(), 7, 0f, Main.myPlayer,
                IsEcho ? _ownerIndex : NPC.whoAmI);
        }
        protected override void DoMeleeAttack() { }
        protected override void DoRangedAttack() { }

        private float CastRotation => _spell == Spell.SuspendedCrystal ? -MathHelper.PiOver2
            : MathHelper.WrapAngle(_facing == 1 ? _aim.ToRotation() : MathHelper.Pi - _aim.ToRotation());
        private float StaffRotation
        {
            get
            {
                if (IsRune)
                {
                    if (Phase == AttackPhase.MagicTelegraph)
                        return RuneBladeConjuration.Tell(_runeFirstUnderhand, 1f - PhaseTimer / (float)MagicTelegraphTicks);
                    if (Phase == AttackPhase.MagicAttack)
                        return RuneBladeConjuration.StaffPose(_runeFirstUnderhand, MagicAttackTicks - PhaseTimer, _runeCount);
                    if (Phase == AttackPhase.MagicRecovery)
                        return MathHelper.Lerp(_runeRecoveryPose, RuneBladeConjuration.Carry,
                            MathHelper.SmoothStep(0f, 1f, MathHelper.Clamp((MagicRecoveryTicks - PhaseTimer) / 24f, 0f, 1f)));
                    return RuneBladeConjuration.Carry;
                }
                if (_blinkPending) return MathHelper.Lerp(-0.3f, -1.2f,
                    MathHelper.SmoothStep(0f, 1f, 1f - NPC.GetGlobalNPC<tsorcRevampGlobalNPC>().TeleportCountdown / 40f));
                if (Phase == AttackPhase.MagicTelegraph)
                    return MathHelper.Lerp(MagicCastStartRotation, CastRotation,
                        MathHelper.SmoothStep(0f, 1f, MathHelper.Clamp((MagicTelegraphTicks - PhaseTimer) / (float)(MagicTelegraphTicks - 12), 0f, 1f)));
                if (Phase == AttackPhase.MagicAttack) return CastRotation;
                if (Phase == AttackPhase.MagicRecovery)
                    return MathHelper.Lerp(CastRotation, -0.3f,
                        MathHelper.SmoothStep(0f, 1f, MathHelper.Clamp((MagicRecoveryTicks - PhaseTimer - 12) / (float)Math.Max(1, MagicRecoveryTicks - 12), 0f, 1f)));
                return -0.3f;
            }
        }
        protected override void ModifyAdditionalPhaseWeaponRotation(ref float weaponRotation) => weaponRotation = StaffRotation;
        internal bool RuneBladeChannelActive => IsRune && Phase == AttackPhase.MagicAttack
            && NPC.GetGlobalNPC<tsorcRevampGlobalNPC>().StaggerTimer <= 0;

        private bool RuneCanReach(bool underhand)
        {
            if (!NPC.HasValidTarget) return false;
            Player target = Main.player[NPC.target];
            if (!target.active || target.dead || (target.Center.X - NPC.Center.X) * _facing < 0f
                || !Collision.CanHitLine(NPC.Center, 1, 1, target.Center, 1, 1)) return false;
            Vector2 pivot = NPC.Center + RuneBladeConjuration.Offset(underhand, _facing);
            float start = RuneBladeConjuration.Sweep(underhand, 0f), end = RuneBladeConjuration.Sweep(underhand, 15f);
            int samples = (int)Math.Ceiling(Math.Abs(end - start) / MathHelper.ToRadians(4f));
            for (int i = 0; i <= samples; i++)
            {
                Vector2 direction = RuneBladeConjuration.WorldAngle(MathHelper.Lerp(start, end, i / (float)samples), _facing).ToRotationVector2();
                float point = 0f;
                if (Collision.CheckAABBvLineCollision(target.Hitbox.TopLeft(), target.Hitbox.Size(),
                    pivot + direction * 14f, pivot + direction * RuneBladeConjuration.Reach, 10f, ref point)) return true;
            }
            return false;
        }

        private bool TryRuneHoverFloor(float x, out float centerY)
        {
            centerY = NPC.Center.Y;
            int tileX = (int)(x / 16f);
            int from = Math.Max(1, (int)(NPC.Bottom.Y / 16f) - 3);
            int to = Math.Min(Main.maxTilesY - 2, from + 13);
            if (tileX < 2 || tileX >= Main.maxTilesX - 2) return false;
            for (int y = from; y <= to; y++)
            {
                Tile tile = Framing.GetTileSafely(tileX, y);
                if (!tile.HasUnactuatedTile || !(Main.tileSolid[tile.TileType] || Main.tileSolidTop[tile.TileType])) continue;
                float bottom = y * 16f - 16f;
                Vector2 position = new Vector2(x - NPC.width * 0.5f, bottom - NPC.height);
                if (Math.Abs(bottom - NPC.Bottom.Y) > 64f || Collision.SolidCollision(position, NPC.width, NPC.height)
                    || Collision.LavaCollision(position, NPC.width, NPC.height)) continue;
                centerY = bottom - NPC.height * 0.5f; return true;
            }
            return false;
        }

        private void UpdateRuneMovement(bool server, tsorcRevampGlobalNPC g)
        {
            bool casting = IsRune && (Phase == AttackPhase.MagicTelegraph || Phase == AttackPhase.MagicAttack);
            if ((!_runeApproach && !casting) || g.StaggerTimer > 0)
            {
                if (_runeLowHover) { _runeLowHover = false; NPC.noGravity = false; if (server) NPC.netUpdate = true; }
                return;
            }
            if (!NPC.HasValidTarget || !Main.player[NPC.target].active || Main.player[NPC.target].dead)
            {
                if (server) CancelRuneApproach();
                return;
            }
            Player target = Main.player[NPC.target];
            int tick = Phase == AttackPhase.MagicAttack ? (MagicAttackTicks - PhaseTimer) % RuneBladeConjuration.StepTicks : -1;
            bool live = casting && tick >= 40 && tick < 55;
            bool preparing = _runeApproach || Phase == AttackPhase.MagicTelegraph || tick < 40 || tick >= 96;
            bool advancing = preparing || casting && tick >= 55; // keep pressure through the harmless settle, too
            if (server)
            {
                _runeApproachTicks++; // also supplies the six-tick movement snapshot cadence during casts
                if (_runeApproach)
                {
                    _facing = target.Center.X >= NPC.Center.X ? 1 : -1;
                    if (_runeApproachTicks > RuneApproachTimeout || NPC.Distance(target.Center) > MagicRange)
                    { CancelRuneApproach(); return; }
                }
                if (advancing)
                {
                    float ahead = NPC.Center.X + _facing * 24f;
                    bool clear = !Collision.SolidCollision(NPC.position + new Vector2(_facing * 16f, 0f), NPC.width, NPC.height);
                    _runeLowHover = clear && TryRuneHoverFloor(ahead, out _runeHoverY);
                }
                if (_runeApproach && Math.Abs(target.Center.X - NPC.Center.X) <= RuneStartDistance
                    && Math.Abs(target.Center.Y - NPC.Center.Y) <= 56f
                    && Collision.CanHitLine(NPC.Center, 1, 1, target.Center, 1, 1))
                { _runeApproach = false; BeginSpell(Spell.RuneBlade); }
                float desired = 0f;
                if (_runeApproach) desired = RuneApproachSpeed * _facing;
                else if (live) desired = _runeStride;
                else if (advancing && (target.Center.X - NPC.Center.X) * _facing > 72f) desired = RuneCastSpeed * _facing;
                if (desired != _runeDesiredX || _runeApproachTicks % 6 == 0) NPC.netUpdate = true;
                _runeDesiredX = desired;
            }
            NPC.direction = NPC.spriteDirection = _facing;
            NPC.velocity.X = MathHelper.Lerp(NPC.velocity.X, _runeDesiredX, 0.3f);
            NPC.noGravity = _runeLowHover;
            if (_runeLowHover) NPC.velocity.Y = MathHelper.Clamp((_runeHoverY - NPC.Center.Y) * 0.25f, -2.5f, 2.5f);
        }

        private void CancelRuneApproach()
        {
            if (IsRune && !IsNeutral) _runeRecoveryPose = StaffRotation;
            _runeApproach = _runeLowHover = false; NPC.noGravity = false; _neutralDelay = 30;
            _runeDesiredX = _runeStride = 0f;
            if (IsRune && !IsNeutral) EnterPhase(AttackPhase.MagicRecovery, MagicRecoveryTicks);
            _runeCooldown = Math.Max(_runeCooldown, 120); NPC.netUpdate = true;
        }

        internal Vector2 StaffTipWorld
        {
            get
            {
                // Mirrors SyncPuppet + vanilla GetFrontHandPosition. No render cache or dedicated-server approximation.
                _staffPose ??= new Player();
                _staffPose.position = NPC.Bottom - new Vector2(PuppetVisualWidth, PuppetVisualHeight) + PuppetVisualOffset;
                _staffPose.width = PuppetVisualWidth; _staffPose.height = PuppetVisualHeight;
                _staffPose.direction = NPC.direction; _staffPose.gravDir = 1f;
                float angle = StaffRotation;
                float arm = (angle - MathHelper.PiOver2 + CompositeArmRotationOffset) * NPC.direction;
                Vector2 hand = _staffPose.GetFrontHandPosition(CompositeArmStretch, arm);
                Vector2 direction = (NPC.direction == 1 ? angle : MathHelper.Pi - angle).ToRotationVector2();
                return hand + direction * 43f;
            }
        }

        public override void InitializeTeleportIllusion(NPC illusion)
        {
            if (illusion.ModNPC is not AttraidiesIllusion echo) return;
            echo._ownerIndex = NPC.whoAmI; echo._initialized = true;
            // 82t wait + 40t tell: the last real shot occurs ~89t after warp; echo release is
            // at least 31t later even when the newly spawned NPC starts AI in the same world tick.
            echo._neutralDelay = 82; echo._blinkCooldown = int.MaxValue;
            echo._echoAnchor = illusion.Center;
            echo._spell = Spell.EchoShot; echo._facing = illusion.direction;
            echo.SetDisplayWeapon(echo.MagicWeaponItemType, false);
            illusion.npcSlots = 0; illusion.netUpdate = true;
            _echoIndex = illusion.whoAmI; NPC.netUpdate = true;
        }

        private bool HasCrystal()
        {
            int type = ModContent.ProjectileType<IllusionCrystal>();
            for (int i = 0; i < Main.maxProjectiles; i++)
                if (Main.projectile[i].active && Main.projectile[i].type == type && (int)Main.projectile[i].ai[0] == NPC.whoAmI) return true;
            return false;
        }
        private bool TryFindPortal(Player target, out Vector2 portal)
        {
            for (int h = 240; h >= 160; h -= 16)
            {
                Vector2 candidate = target.Center - new Vector2(0f, h);
                if (PortalSafe(candidate) && Collision.CanHitLine(candidate, 1, 1, target.Center, 1, 1))
                { portal = candidate; return true; }
            }
            portal = Vector2.Zero; return false;
        }
        private static bool PortalSafe(Vector2 position)
        {
            if (position.X < 32 || position.Y < 32 || position.X > Main.maxTilesX * 16 - 32 || position.Y > Main.maxTilesY * 16 - 32
                || Collision.SolidCollision(position - new Vector2(18f), 36, 36)) return false;
            for (int i = 0; i < Main.maxPlayers; i++)
                if (Main.player[i].active && !Main.player[i].dead && Vector2.Distance(position, Main.player[i].Center) < 160f) return false;
            return true;
        }
        private bool TryFindBlinkDestination(Player target, out Vector2 destination)
        {
            int side = Main.rand.NextBool() ? 1 : -1;
            for (int n = 0; n < 2; n++, side = -side)
                for (int xOffset = 12; xOffset <= 20; xOffset += 2)
                {
                    int tileX = (int)(target.Center.X / 16f) + side * xOffset;
                    for (int tileY = (int)(target.Bottom.Y / 16f) - 8; tileY <= (int)(target.Bottom.Y / 16f) + 8; tileY++)
                    {
                        Vector2 candidate = new Vector2(tileX * 16 + 8, tileY * 16 - NPC.height / 2f);
                        if (SafeDestination(candidate, true) && Collision.CanHitLine(candidate, 1, 1, target.Center, 1, 1))
                        { destination = candidate; return true; }
                    }
                }
            destination = Vector2.Zero; return false;
        }
        private bool SafeDestination(Vector2 center, bool requireGround)
        {
            if (NPC.HasValidTarget && Vector2.Distance(center, Main.player[NPC.target].Center) > 320f) return false;
            Vector2 topLeft = center - NPC.Size * 0.5f;
            if (topLeft.X < 32 || topLeft.Y < 32 || center.X + NPC.width > Main.maxTilesX * 16 - 32
                || center.Y + NPC.height > Main.maxTilesY * 16 - 32 || Collision.SolidCollision(topLeft, NPC.width, NPC.height)) return false;
            int x = (int)(center.X / 16f), y = (int)((center.Y + NPC.height / 2f) / 16f);
            Tile floor = Framing.GetTileSafely(x, y);
            if (requireGround && (!floor.HasUnactuatedTile || !Main.tileSolid[floor.TileType] || Main.tileSolidTop[floor.TileType])) return false;
            if (Framing.GetTileSafely(x, y - 1).LiquidAmount > 0 && Framing.GetTileSafely(x, y - 1).LiquidType == LiquidID.Lava) return false;
            for (int i = 0; i < Main.maxPlayers; i++)
                if (Main.player[i].active && !Main.player[i].dead && Vector2.Distance(center, Main.player[i].Center) < 192f) return false;
            return true;
        }
        private void CancelBlink()
        {
            tsorcRevampGlobalNPC g = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            g.TeleportCountdown = g.TeleportAppearanceTimer = 0; g.TeleportTelegraph = Vector2.Zero;
            _blinkPending = false; NPC.alpha = 0;
        }
        public override void OnStagger(NPC npc)
        {
            _runeApproach = _runeLowHover = false; NPC.noGravity = false;
            base.OnStagger(npc); CancelBlink(); _neutralDelay = 60; _aimLocked = false;
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                int crystalType = ModContent.ProjectileType<IllusionCrystal>();
                for (int i = 0; i < Main.maxProjectiles; i++)
                {
                    Projectile crystal = Main.projectile[i];
                    if (crystal.active && crystal.type == crystalType && (int)crystal.ai[0] == NPC.whoAmI && crystal.ai[2] < 30f)
                        crystal.Kill();
                }
            }
            if (Main.netMode != NetmodeID.MultiplayerClient && Flight != null && Flight.IsAirborne)
            { Flight.EndFlightNow(); NPC.noGravity = false; }
            NPC.netUpdate = true;
        }

        private void Decorate(tsorcRevampGlobalNPC g)
        {
            if (IsEcho)
            {
                float opacity = MathHelper.Clamp(_echoAge / 15f, 0f, 1f) * 0.45f;
                if (_echoFired) opacity *= MathHelper.Clamp(_neutralDelay / 20f, 0f, 1f);
                NPC.alpha = (int)(255 * (1f - opacity));
            }
            else if (g.TeleportCountdown == 0 && g.TeleportAppearanceTimer == 0) NPC.alpha = 0;
            if (_blinkPending)
            {
                GatherDust(StaffTipWorld, CrystalColor, 16f);
                GatherDust(_blinkDestination, CrystalColor, 24f);
            }
            if (Phase == AttackPhase.MagicAttack) GatherDust(StaffTipWorld, MagicTelegraphFlashColor, 5f);
            if (IsRune && Phase == AttackPhase.MagicAttack)
            {
                int elapsed = MagicAttackTicks - PhaseTimer;
                int tick = elapsed % RuneBladeConjuration.StepTicks;
                int step = elapsed / RuneBladeConjuration.StepTicks;
                // Repeat tells form the next spawn site; the staff gesture then traces that same arc.
                bool underhand = RuneBladeConjuration.Underhand(_runeFirstUnderhand, tick >= 96 ? step + 1 : step);
                if (tick < RuneBladeConjuration.SweepTicks || tick >= 96)
                    GatherDust(NPC.Center + RuneBladeConjuration.Offset(underhand, _facing), RuneColor, 18f);
            }
            if (Flight != null && Flight.IsAirborne && Main.GameUpdateCount % 3 == 0)
            {
                Dust dust = Dust.NewDustPerfect(NPC.Bottom + Main.rand.NextVector2Circular(12f, 4f),
                    DustID.TintableDustLighted, new Vector2(0f, 1.2f), 80, CrystalColor, 1f);
                dust.noGravity = true;
            }
            if (_runeLowHover && Main.GameUpdateCount % 3 == 0)
            {
                Dust lift = Dust.NewDustPerfect(NPC.Bottom + Main.rand.NextVector2Circular(12f, 3f),
                    DustID.TintableDustLighted, new Vector2(-NPC.velocity.X * 0.2f, 1f), 80, RuneColor, 0.9f);
                lift.noGravity = true;
            }
            if (!IsEcho && IsNeutral && NPC.life <= NPC.lifeMax * 0.33f && Main.GameUpdateCount % 5 == 0)
                GatherDust(NPC.Center, CrystalColor, 18f);
            if (Main.rand.NextBool(360)) SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Custom/EvilLaugh2") with { Volume = 1.1f }, NPC.Center);
        }
        protected override void DoMagicTelegraphVFX(float progress)
        {
            if (Main.dedServ) return;
            GatherDust(StaffTipWorld, MagicTelegraphFlashColor, MathHelper.Lerp(20f, 5f, progress));
            if (IsRune) GatherDust(NPC.Center + RuneBladeConjuration.Offset(_runeFirstUnderhand, _facing), RuneColor, 20f);
            if (_spell == Spell.SuspendedCrystal) GatherDust(_portal, CrystalColor, MathHelper.Lerp(28f, 10f, progress));
        }
        private static void GatherDust(Vector2 point, Color color, float radius)
        {
            Vector2 offset = Main.rand.NextVector2CircularEdge(radius, radius);
            Dust dust = Dust.NewDustPerfect(point + offset, DustID.TintableDustLighted,
                -offset.SafeNormalize(Vector2.UnitY) * 1.8f, 70, color, 0.8f);
            dust.noGravity = true;
            Lighting.AddLight(point, color.ToVector3() * 0.25f);
        }
        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            tsorcRevampGlobalNPC g = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            if (g.TeleportVisualStyle == TeleportVisualStyle.GreySmoke && g.TeleportCountdown > 0) return false;
            bool charging = Phase == AttackPhase.MagicTelegraph && !_blinkPending;
            bool streaming = Phase == AttackPhase.MagicAttack && (_spell == Spell.OracleVolley || _spell == Spell.Mirrorstep);
            if ((charging || streaming) && !IsRune)
            {
                float progress = charging ? 1f - PhaseTimer / (float)MagicTelegraphTicks : 1f;
                Vector2 tip = StaffTipWorld - screenPos;
                if (_spell != Spell.SuspendedCrystal)
                {
                    Texture2D orb = ModContent.Request<Texture2D>("tsorcRevamp/Content/Projectiles/Enemy/TheOracle").Value;
                    float growth = charging ? MathHelper.Clamp((20f - PhaseTimer) / 20f, 0f, 1f)
                        : (MagicAttackTicks - PhaseTimer) % 12 / 12f;
                    spriteBatch.Draw(orb, tip, null, Color.White * growth, Main.GameUpdateCount * 0.1f,
                        orb.Size() * 0.5f, growth, SpriteEffects.None, 0f);
                    int rays = charging ? (_spell == Spell.OracleFan ? 3 : 1) : 0;
                    for (int i = 0; i < rays; i++)
                    {
                        Vector2 ray = _aim.RotatedBy(MathHelper.ToRadians((i - (rays - 1) * 0.5f) * 20f));
                        Utils.DrawLine(spriteBatch, StaffTipWorld, StaffTipWorld + ray * 200f, OracleColor * (0.15f + 0.3f * progress),
                            OracleColor * 0f, 2f);
                    }
                }
            }
            return base.PreDraw(spriteBatch, screenPos, drawColor);
        }
        public override void SendExtraAI(BinaryWriter writer)
        {
            base.SendExtraAI(writer);
            writer.Write((byte)_spell); writer.Write((sbyte)_facing);
            writer.Write(_aim.X); writer.Write(_aim.Y); writer.Write(_aimLocked);
            writer.Write(_portal.X); writer.Write(_portal.Y);
            writer.Write(_blinkDestination.X); writer.Write(_blinkDestination.Y); writer.Write(_blinkPending);
            writer.Write(_neutralDelay); writer.Write(_blinkCooldown); writer.Write(_crystalCooldown);
            writer.Write((short)_ownerIndex); writer.Write((short)_echoIndex); writer.Write(_echoFired);
            writer.Write((byte)_releasedShots);
            writer.Write((short)_echoAge);
            writer.Write(_echoAnchor.X); writer.Write(_echoAnchor.Y);
            writer.Write(_runeFirstUnderhand); writer.Write((byte)_runeCount); writer.Write(_runeCooldown);
            writer.Write(_runeApproach); writer.Write(_runeLowHover); writer.Write(_runeApproachTicks);
            writer.Write(_runeHoverY); writer.Write(_runeStride);
            writer.Write(_runeDesiredX);
            writer.Write(_runeRecoveryPose);
        }
        public override void ReceiveExtraAI(BinaryReader reader)
        {
            base.ReceiveExtraAI(reader);
            _spell = (Spell)reader.ReadByte(); _facing = reader.ReadSByte();
            _aim = new Vector2(reader.ReadSingle(), reader.ReadSingle()); _aimLocked = reader.ReadBoolean();
            _portal = new Vector2(reader.ReadSingle(), reader.ReadSingle());
            _blinkDestination = new Vector2(reader.ReadSingle(), reader.ReadSingle()); _blinkPending = reader.ReadBoolean();
            _neutralDelay = reader.ReadInt32(); _blinkCooldown = reader.ReadInt32(); _crystalCooldown = reader.ReadInt32();
            _ownerIndex = reader.ReadInt16(); _echoIndex = reader.ReadInt16(); _echoFired = reader.ReadBoolean();
            _releasedShots = reader.ReadByte(); DebugAttackLabel = CurrentSpell.Name;
            _echoAge = reader.ReadInt16();
            _echoAnchor = new Vector2(reader.ReadSingle(), reader.ReadSingle());
            _runeFirstUnderhand = reader.ReadBoolean(); _runeCount = reader.ReadByte(); _runeCooldown = reader.ReadInt32();
            _runeApproach = reader.ReadBoolean(); _runeLowHover = reader.ReadBoolean(); _runeApproachTicks = reader.ReadInt32();
            _runeHoverY = reader.ReadSingle(); _runeStride = reader.ReadSingle();
            _runeDesiredX = reader.ReadSingle();
            _runeRecoveryPose = reader.ReadSingle();
            if (_runeApproach) DebugAttackLabel = "Rune Blade: closing distance";
        }

        public override void OnKill()
        {
            if (IsEcho) return;
            if (!Main.dedServ) for (int j = 0; j < 30; j++)
            {
                int dust = Dust.NewDust(new Vector2(NPC.position.X, NPC.position.Y), NPC.width, NPC.height, 16, Main.rand.Next(-4, 4), Main.rand.Next(-4, 4), 200, Color.Salmon, 2.5f);
                Main.dust[dust].noGravity = true;
            }

            if (Main.netMode == NetmodeID.MultiplayerClient) return;
            UsefulFunctions.BroadcastText(LangUtils.GetTextValue("NPCs.AttraidiesIllusion.Death"), 190, 140, 150);
        }
        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            npcLoot.Add(ItemDropRule.Common(ItemID.ShinePotion, 55));
            npcLoot.Add(new CommonDrop(ItemID.RegenerationPotion, 35, 1, 1, 4));
            npcLoot.Add(new CommonDrop(ItemID.MagicPowerPotion, 35, 1, 1, 3));
            npcLoot.Add(ItemDropRule.Common(ItemID.HunterPotion, 35));
            npcLoot.Add(ItemDropRule.Common(ItemID.GillsPotion, 75));
            npcLoot.Add(new CommonDrop(ItemID.IronskinPotion, 35, 1, 1, 2));
            npcLoot.Add(new CommonDrop(ItemID.ManaRegenerationPotion, 30, 1, 1, 9));
            npcLoot.Add(new CommonDrop(ItemID.GoldenKey, 10, 1, 1, 3));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<AquamarineRing>(), 20));
            npcLoot.Add(new CommonDrop(ItemID.GreaterHealingPotion, 10, 2, 2));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<HealingElixir>()));
            npcLoot.Add(ItemDropRule.ByCondition(tsorcRevamp.tsorcItemDropRuleConditions.CursedRule, ModContent.ItemType<RadiantLifegemItem>(), 2));
            npcLoot.Add(ItemDropRule.ByCondition(tsorcRevamp.tsorcItemDropRuleConditions.CursedRule, ModContent.ItemType<StarlightShardItem>(), 6));
        }

    }
}

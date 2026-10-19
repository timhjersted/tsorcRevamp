using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;
using tsorcRevamp.Content.Items.Potions;
using tsorcRevamp.Content.Items.Potions.StarlightShard;
using tsorcRevamp.Content.Items.Weapons.Enemy;
using Terraria.DataStructures;
using tsorcRevamp.Content.Projectiles.Enemy.Triad;
using tsorcRevamp.Content.Projectiles.Enemy.Archdeacon;
using tsorcRevamp.NPCs.AI;
using tsorcRevamp.NPCs.Puppets;
using tsorcRevamp.Utilities;
using tsorcRevamp.Content.Projectiles.Enemy;

namespace tsorcRevamp.NPCs.Enemies
{
    // Wyvern Fortress glintstone sorcerer on the puppet system (Spectre set + ArchdeaconStaff). Walks and kites like the
    // Attraidies Illusion, and often takes off to cast from a hover point beside and above the player.
    //
    // Every spell is pure blue glintstone magic built from ArchdeaconGlintStar: stars bloom in a mist puff, hang still,
    // then launch from rest and accelerate (the slow start is what keeps close-range stars readable).
    // Every hit (and every Snare hold) applies Mana Sickness + Withered Armor: 4s Normal, scaled by mode
    // (ArchdeaconGlintStar.ApplyHitDebuffs).
    // Attack spec (ticks at 60/s; damage is the raw value passed to NewProjectile):
    //   Glintstone Phalanx  30t staff-raised tell / 4 stars (6 below 50%) bloom in an arc around its head, launching
    //                       10t apart, each re-aiming at the nearest player as it launches / 40t recovery. Dmg 20.
    //   Great Glintstone Orb 50t channel (the stagger window: interruptible for 2/3, then a commit cue) / a slow orb that
    //                       sheds 3 rings of 6 resting stars (4 of 8 below 50%) 24t apart, gaps rotating, then a final ring
    //                       / 50t recovery. Cooldown 480t. Dmg 26.
    //   Starfall            36t raised tell / 7 stars (9) at varied heights 200-320px above the player, falling straight
    //                       down in a staggered sweep from one side, each with its own acceleration / 40t recovery.
    //                       Needs 160px of open air above the player. Cooldown 420t. Dmg 20.
    //   Star Burst          30t aimed tell / a small orb lobbed to the player's position: 40t travel, 30t flash, then a
    //                       ring of 10 stars from rest / 40t recovery. Dmg 20.
    //   Glintstone Comet    hover-only, 40t aimed tell + aim line / one large comet, homing gently for 30t, then straight
    //                       and accelerating to 12px/t / 44t recovery. Cooldown 360t. Dmg 30.
    //   Star Prison         36t raised tell / 8 stars in a 240px ring around the player, the slot facing the Archdeacon
    //                       left open as the exit; they hang 40t then converge / 44t recovery. Never right after a Snare.
    //                       Cooldown 600t. Dmg 20.
    //   Glintstone Snare    36t aimed tell / a lobbed glint lands on the player's floor and plants a 6-tile field for 6s.
    //                       Entering it holds a player in place for 2s (attacks still work), once per field; dodge-rolling
    //                       across it is safe / 40t recovery. At most 2 fields at once. Cooldown 300t.
    //   Glintstone Eruption 30t raised tell / a rune under every player in range: 40t floor telegraph, then the centre
    //                       lance, side lances 12t later (each a 264px / ~16-tile needle) / 44t recovery. Dmg 22.
    //   Blink               40t teleport telegraph to a spot 12-20 tiles beside the player with line of sight, then it
    //                       opens with the next spell after 12t. Also the navigation fallback after 3s without line of sight.
    // Selection: a shuffled bag (Phalanx x2 + one of everything else), server-only, never the same spell twice in a row;
    // cooldowns and spacing gates skip entries without starving them (they stay in the bag).
    // Hover-casting: takes off first for HoverCastChance% of casts, and always when the player is 6+ tiles above.
    // Below 50% life: more stars, shorter pause between casts, shorter Blink cooldown.
    class Archdeacon : PuppetNPC
    {
        private enum Spell : byte
        {
            Phalanx,
            GreatOrb,
            Starfall,
            StarBurst,
            Comet,
            Prison,
            Snare,
            Eruption,
            Blink,
        }

        private readonly struct SpellSpec
        {
            public readonly string Name;
            public readonly int Tell;
            public readonly int Active;
            public readonly int Recovery;

            public SpellSpec(string name, int tell, int active, int recovery)
            {
                Name = name;
                Tell = tell;
                Active = active;
                Recovery = recovery;
            }
        }

        // Indexed by Spell. Every cast releases on its first active tick; the projectiles own their own sequencing.
        // Blink has no cast phase (the teleport telegraph is its tell); its entry only names it.
        private static readonly SpellSpec[] Spells =
        {
            new SpellSpec("Glintstone Phalanx", 30, 1, 40),
            new SpellSpec("Great Glintstone Orb", 50, 1, 50),
            new SpellSpec("Starfall", 36, 1, 40),
            new SpellSpec("Star Burst", 30, 1, 40),
            new SpellSpec("Glintstone Comet", 40, 1, 44),
            new SpellSpec("Star Prison", 36, 1, 44),
            new SpellSpec("Glintstone Snare", 36, 1, 40),
            new SpellSpec("Glintstone Eruption", 30, 1, 44),
            new SpellSpec("Blink", 0, 1, 1),
        };

        private const int StarDamage = 20;
        private const int GreatOrbDamage = 26;
        private const int CometDamage = 30;
        private const int LanceDamage = 22;

        // Phalanx: stars on an arc of PhalanxRadius around the head, first launch PhalanxFirstHang after release.
        private const float PhalanxRadius = 64f;
        private const int PhalanxFirstHang = 24;
        private const int PhalanxLaunchSpacing = 10;
        private const float PhalanxAcceleration = 0.22f;

        private const int GreatOrbCooldownTicks = 480;
        private const float GreatOrbMinRange = 160f;
        private const float GreatOrbReleaseSpeed = 0.5f;

        private const int StarfallCooldownTicks = 420;
        private const float StarfallSpacing = 44f;
        private const float StarfallMinHeight = 200f;
        private const float StarfallMaxHeight = 320f;
        private const float StarfallNeededAir = 160f;
        private const int StarfallFirstHang = 30;
        private const int StarfallSweepSpacing = 7;

        private const float StarBurstMinRange = 128f;
        private const float StarBurstMaxRange = 480f;

        private const int CometCooldownTicks = 360;
        private const float CometMinRange = 160f;
        private const float CometAcceleration = 0.3f;

        private const int PrisonCooldownTicks = 600;
        private const int PrisonStarCount = 8;
        private const float PrisonRadius = 240f;
        private const int PrisonHangTicks = 40;
        private const float PrisonAcceleration = 0.2f;
        private const float PrisonMaxRange = 520f;

        private const int SnareCooldownTicks = 300;
        private const int MaxActiveSnares = 2;
        private const float SnareMinRange = 96f;

        private const int FloorSearchTiles = 12;
        // Closest a star may bloom to any player. From rest at 0.2-0.26 px/t^2, 96px still takes ~28t to cover.
        private const float StarMinPlayerDistance = 96f;
        private const float PrisonMinPlayerDistance = 160f;

        private const int BlinkTelegraphTicks = 40;
        private const int BlinkCooldownTicks = 480;
        private const int BlinkCooldownLowLifeTicks = 360;
        private const int BlinkFollowUpDelayTicks = 12;
        private const int NavigationBlinkTicks = 180;

        private const int NeutralDelayTicks = 50;
        private const int NeutralDelayLowLifeTicks = 30;
        private const int HoverCastChance = 35;
        private const float HoverForcedHeight = 96f;
        private const int HoverApproachTimeout = 180;
        // Open space needed above the head to take off. Under a ceiling the hover keeps a third of the room clear,
        // so 112px (7 tiles) still leaves ~55px of real lift; anything lower and it just casts from the ground.
        private const float MinHoverHeadroom = 112f;

        // ArchdeaconStaff grip is 30% up the 56px shaft, so the gem sits ~40px from the hand.
        private const float StaffTipReach = 40f;

        private static readonly Color AuraColor = ArchdeaconGlintStar.GlintColor;

        private readonly List<Spell> _spellBag = new List<Spell>();
        private Spell _spell;
        private Spell _pendingSpell;
        private Spell _lastSpell = Spell.Blink;
        private Vector2 _aim = Vector2.UnitX;
        private Vector2 _blinkDestination;
        private bool _aimLocked;
        private bool _blinkPending;
        private bool _blinkFollowUp;
        private bool _hoverApproach;
        private bool _initialized;
        private int _facing = 1;
        private int _neutralDelay = 30;
        private int _blinkCooldown = 180;
        // Server-only spell cooldowns (they gate selection, which only the server runs, so they are never synced).
        private int _greatOrbCooldown;
        private int _starfallCooldown;
        private int _cometCooldown;
        private int _prisonCooldown;
        private int _snareCooldown;
        private int _hoverApproachTicks;
        private int _flightFailureTicks;
        private Player _staffPose;

        private SpellSpec CurrentSpell => Spells[(int)_spell];
        private bool IsNeutral => Phase == AttackPhase.Idle || Phase == AttackPhase.CasualStroll;
        private bool LowLife => NPC.life <= NPC.lifeMax * 0.5f;

        public override bool UsesPuppetDifficultyScaling => false;
        protected override bool AnnounceInvasion => false;
        protected override bool DespawnsOnPartyWipe => false;
        protected override string InvaderTitle => "Archdeacon";
        protected override int HeadArmorItemType => ItemID.SpectreHood;
        protected override int BodyArmorItemType => ItemID.SpectreRobe;
        protected override int LegsArmorItemType => ItemID.SpectrePants;
        protected override int MeleeWeaponItemType => -1;
        protected override int RangedWeaponItemType => -1;
        protected override int MagicWeaponItemType => ModContent.ItemType<ArchdeaconStaff>();
        protected override int MeleeDamage => 0;
        protected override int RangedDamage => 0;
        protected override int MagicDamage => StarDamage;
        protected override int EstusChargesMax => 0;
        protected override int CasualStrollChance => 0;
        // The subclass owns neutral selection; the base owns every cast phase and its network clock.
        protected override bool HoldAttackSelection => true;
        protected override bool ShowWeaponDuringNeutral => true;
        protected override float TopSpeed => 1.8f;
        protected override float Acceleration => 0.08f;
        protected override float RunSpeedMult => 1f;
        protected override float PuppetJumpPower => 7.5f;
        protected override float PuppetJumpBoost => 3f;
        protected override float MagicRange => 640f;
        protected override int MagicTelegraphTicks => CurrentSpell.Tell;
        protected override int MagicAttackTicks => CurrentSpell.Active;
        protected override int MagicRecoveryTicks => CurrentSpell.Recovery;
        protected override int MagicCooldownAfterUse => 0;
        protected override bool BrakeDuringMagicCast => _spell == Spell.GreatOrb || _spell == Spell.Prison;
        protected override bool UseAuthoredMagicCastPose => true;
        protected override float MagicCastStartRotation => -0.3f;
        protected override float MagicCastEndRotation => CastRotation;
        protected override float MagicWeaponRotationOffset => MathHelper.PiOver4;
        protected override bool MirrorMagicWeaponRotationByFacing => true;
        protected override int MagicWeaponRecoveryHoldTicks => MagicRecoveryTicks;
        protected override int MagicTelegraphFlashLeadTicks => MagicTelegraphTicks;
        protected override bool UseCompositeArmForAdditionalPhase => true;
        protected override bool UseCompositeArmSwing => true;
        // ArchdeaconStaff.png: butt (0,39) to gem (41,0); 30% up from the butt.
        protected override Vector2 MagicGripNorm => new Vector2(0.30f, 0.70f);

        // Every spell is glintstone blue; the tell dust and flash share the stars' colour.
        protected override Color MagicTelegraphFlashColor => ArchdeaconGlintStar.GlintColor;

        // The flight controller without visible wings: the Spectre set "levitates".
        protected override bool HasWings => true;
        protected override int WingsAccessoryItemType => 0;
        protected override bool ShowWingsWhenGrounded => false;
        protected override int RandomTakeoffChance => 0;
        protected override float FlightHeightTrigger => float.MaxValue;
        protected override bool CanUseAerialMelee => false;

        protected override EnemyFlightConfig FlightConfig
        {
            get
            {
                EnemyFlightConfig config = EnemyFlightConfig.Default;
                config.HoverAltitude = 128f;
                config.HoverSideOffset = 192f;
                config.HoverTopSpeed = 3f;
                config.TakeOffSpeed = 5f;
                config.MaxFlightTicks = 240;
                config.CooldownTicks = 240;
                config.TakeOffTicks = 24;
                config.HoverDwellTicks = 180;
                config.LandTicks = 60;
                config.LandSpeed = 3f;
                // Keep the top third of a low room empty so it never bobs against the ceiling.
                config.CeilingClearanceFraction = 0.33f;
                return config;
            }
        }

        protected override int TeleportTelegraphTicks => BlinkTelegraphTicks;
        protected override int TeleportDustTypeId => DustID.TintableDustLighted;
        protected override Color TeleportDustTint => AuraColor;

        public override void SetStaticDefaults()
        {
            // The puppet body is drawn by the base; Archdeacon.png is only the bestiary/placeholder sprite.
            Main.npcFrameCount[Type] = 1;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Confused] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Poisoned] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.OnFire] = true;
        }

        public override void SetDefaults()
        {
            NPC.aiStyle = -1;
            NPC.npcSlots = 5;
            NPC.damage = 0;
            NPC.defense = 20;
            NPC.width = 28;
            NPC.height = 44;
            // Authored 500; the EnemyBalance registry sets the real Hardmode / Super Hardmode values.
            NPC.lifeMax = 500;
            NPC.scale = 1f;
            NPC.knockBackResist = 0.1f; // overridden by the PoiseProfiles entry
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.noGravity = false;
            NPC.noTileCollide = false;
            NPC.lavaImmune = true;
            NPC.value = 4000;
            Banner = Type;
            BannerItem = ModContent.ItemType<Banners.ArchdeaconBanner>();

            tsorcRevampGlobalNPC global = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            global.PoiseStaggerResetsAI = true;
            global.NavSearchRadius = 50;
            global.RemembersLastKnownPos = true;
            global.CanWalkBackwards = true;
            global.KiteRangeMin = 9f;
            global.KiteRangeMax = 20f;
            global.KiteLooseness = 0.25f;
            // Blink is scheduled by this class (bag move + navigation fallback), not by FighterAI.
            global.CanTeleport = false;
            global.TeleportStyle = TeleportStyle.RecoveryOnly;
            global.TeleportVisualStyle = TeleportVisualStyle.Default;
            global.TeleportAppearanceDelay = 0;
            global.TeleportArrivalMistTime = 20;
        }

        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            Tile spawnTile = Main.tile[spawnInfo.SpawnTileX, spawnInfo.SpawnTileY];
            bool fortressWall = spawnTile.WallType == WallID.MeteoriteBrick || spawnTile.WallType == WallID.Glass;
            bool inFortress = spawnInfo.Player.ZoneSkyHeight && fortressWall;

            if (!inFortress)
            {
                return 0f;
            }

            bool wyvernMageSlain = tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<Bosses.WyvernMage.WyvernMage>()));
            bool wyvernMageShadowSlain = tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<Bosses.SuperHardMode.GhostWyvernMage.WyvernMageShadow>()));

            // Super Hardmode: common until the Wyvern Mage Shadow falls (the old check was inverted: AFTER it fell).
            if (tsorcRevampWorld.SuperHardMode)
            {
                if (!wyvernMageShadowSlain)
                {
                    return 0.2f;
                }

                return 0f;
            }

            if (!Main.hardMode)
            {
                return 0f;
            }

            // Hardmode: common before the Wyvern Mage falls, rare after.
            if (!wyvernMageSlain)
            {
                return 0.3f;
            }

            return 0.01f;
        }

        protected override void RunMovementAI(float speedMult)
        {
            // Plant for the blink telegraph, the release, and the locked end of an aimed tell.
            bool aimedTellLocked = _aimLocked && Phase == AttackPhase.MagicTelegraph;
            bool planted = _blinkPending || Phase == AttackPhase.MagicAttack || aimedTellLocked;

            if (planted)
            {
                NPC.velocity.X *= 0.7f;
                return;
            }

            float castSlowdown = 1f;
            if (!IsNeutral)
            {
                castSlowdown = 0.6f;
            }

            SmartFighter4AI.Run(NPC, TopSpeed * speedMult * castSlowdown, Acceleration, doorBreakingDamage: 0, attackRange: MagicRange);
        }

        public override void AI()
        {
            tsorcRevampGlobalNPC global = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            bool server = Main.netMode != NetmodeID.MultiplayerClient;

            if (!_initialized)
            {
                _initialized = true;
                SetDisplayWeapon(MagicWeaponItemType, false);

                if (server)
                {
                    NPC.netUpdate = true;
                }
            }

            if (_neutralDelay > 0 && global.StaggerTimer <= 0)
            {
                _neutralDelay--;
            }

            if (_blinkCooldown > 0)
            {
                _blinkCooldown--;
            }

            if (server)
            {
                _greatOrbCooldown = Math.Max(0, _greatOrbCooldown - 1);
                _starfallCooldown = Math.Max(0, _starfallCooldown - 1);
                _cometCooldown = Math.Max(0, _cometCooldown - 1);
                _prisonCooldown = Math.Max(0, _prisonCooldown - 1);
                _snareCooldown = Math.Max(0, _snareCooldown - 1);
            }

            // Cancel a blink whose destination became unsafe (a player walked onto it) on its last tick.
            if (server && _blinkPending && global.TeleportCountdown == 1 && !SafeDestination(_blinkDestination))
            {
                CancelBlink();
                _neutralDelay = NeutralDelayTicks;
                NPC.netUpdate = true;
            }

            // Aimed tells track the target until 12t before release, then lock. Server-only: clients follow _aim via sync.
            if (server && Phase == AttackPhase.MagicTelegraph && !_aimLocked && NPC.HasValidTarget)
            {
                Player target = Main.player[NPC.target];
                _facing = 1;
                if (target.Center.X < NPC.Center.X)
                {
                    _facing = -1;
                }

                Vector2 aimPoint = target.Center;
                _aim = (aimPoint - NPC.Center).SafeNormalize(Vector2.UnitX * _facing);

                if (PhaseTimer <= 12)
                {
                    _aimLocked = true;
                    NPC.netUpdate = true;
                }
                else if (PhaseTimer % 6 == 0)
                {
                    NPC.netUpdate = true;
                }
            }

            if (!IsNeutral)
            {
                NPC.direction = _facing;
                NPC.spriteDirection = _facing;
            }

            AttackPhase previousPhase = Phase;
            base.AI();

            if (!NPC.active)
            {
                return;
            }

            SetDisplayWeapon(MagicWeaponItemType, false);

            if (!IsNeutral)
            {
                NPC.direction = _facing;
                NPC.spriteDirection = _facing;
            }

            // Hover-casts hold still in the air from the aim lock through the release.
            bool hovering = Flight != null && Flight.Mode == FlightMode.Hover;
            bool castingStill = Phase == AttackPhase.MagicAttack || (Phase == AttackPhase.MagicTelegraph && (_aimLocked || StaffRaisedSpell));
            if (hovering && castingStill)
            {
                NPC.velocity = Vector2.Zero;
            }

            // Blink arrival: the shared teleport has finished, so open with a spell almost at once.
            if (server && _blinkPending && global.TeleportCountdown == 0 && global.TeleportAppearanceTimer == 0)
            {
                _blinkPending = false;
                _blinkFollowUp = true;
                _neutralDelay = BlinkFollowUpDelayTicks;
                NPC.netUpdate = true;
            }

            // Cast finished: pause, and come down if it was a hover-cast.
            if (server && previousPhase == AttackPhase.MagicRecovery && IsNeutral)
            {
                _neutralDelay = NeutralDelayTicks;
                if (LowLife)
                {
                    _neutralDelay = NeutralDelayLowLifeTicks;
                }

                if (Flight != null && Flight.IsAirborne)
                {
                    Flight.RequestLand();
                }

                NPC.netUpdate = true;
            }

            // Poise: tells and the blink are interruptible; the last 8t of a tell and the release are hyper-armored.
            // The Great Orb's 50t channel is the stagger window: interruptible for 2/3, then committed with a cue.
            // Recovery has no armour: it is the punish window.
            int commitTicks = 8;
            if (_spell == Spell.GreatOrb)
            {
                commitTicks = MagicTelegraphTicks / 3;
            }

            bool tellLocked = Phase == AttackPhase.MagicTelegraph && PhaseTimer <= commitTicks;
            bool commitCue = _spell == Spell.GreatOrb && Phase == AttackPhase.MagicTelegraph && PhaseTimer == commitTicks;
            if (commitCue && !Main.dedServ)
            {
                SoundEngine.PlaySound(SoundID.Item29 with { Volume = 0.7f, Pitch = 0.6f }, NPC.Center);
            }

            global.AttackCommitted = tellLocked || Phase == AttackPhase.MagicAttack;
            global.AttackTelegraphing = (Phase == AttackPhase.MagicTelegraph || _blinkPending) && !global.AttackCommitted;

            if (global.StaggerTimer > 0)
            {
                global.AttackCommitted = false;
                global.AttackTelegraphing = false;
            }

            if (server && IsNeutral && global.StaggerTimer <= 0 && !_blinkPending)
            {
                SelectNeutralAction();
            }

            if (!Main.dedServ)
            {
                Decorate();
            }
        }

        // Server-only. Picks the next spell from the bag, or handles the hover approach / navigation blink first.
        private void SelectNeutralAction()
        {
            if (_neutralDelay > 0 || !NPC.HasValidTarget)
            {
                return;
            }

            Player target = Main.player[NPC.target];
            if (!target.active || target.dead)
            {
                return;
            }

            bool clear = Collision.CanHitLine(NPC.Center, 1, 1, target.Center, 1, 1);
            if (clear)
            {
                _flightFailureTicks = 0;
            }
            else
            {
                _flightFailureTicks++;
            }

            bool hoveringNow = Flight != null && Flight.Mode == FlightMode.Hover;
            bool hasHeadroom = !Collision.SolidCollision(NPC.position - new Vector2(0f, MinHoverHeadroom), NPC.width, NPC.height + (int)MinHoverHeadroom);
            bool canTakeOff = Flight != null && !Flight.IsAirborne && Flight.CooldownRemaining <= 0 && hasHeadroom;

            // Hover approach: the chosen spell waits until the flight controller reaches its hover point.
            if (_hoverApproach)
            {
                _hoverApproachTicks++;

                bool timedOut = _hoverApproachTicks > HoverApproachTimeout;
                bool neverTookOff = Flight == null || (!Flight.IsAirborne && _hoverApproachTicks > 30);

                if (timedOut || neverTookOff)
                {
                    _hoverApproach = false;
                    _neutralDelay = 20;
                    NPC.netUpdate = true;
                    return;
                }

                if (hoveringNow && clear)
                {
                    _hoverApproach = false;
                    BeginSpell(_pendingSpell);
                }

                return;
            }

            // Mid takeoff or landing: let the flight controller finish.
            if (Flight != null && Flight.IsAirborne && !hoveringNow)
            {
                return;
            }

            if (!clear)
            {
                bool grounded = Flight == null || !Flight.IsAirborne;

                if (_flightFailureTicks >= NavigationBlinkTicks && _blinkCooldown == 0 && grounded && TryFindBlinkDestination(target, out Vector2 destination))
                {
                    BeginBlink(destination);
                    return;
                }

                if (_flightFailureTicks >= 90 && canTakeOff)
                {
                    Flight.RequestTakeoff();
                    NPC.netUpdate = true;
                }

                return;
            }

            float distance = NPC.Distance(target.Center);
            if (distance > MagicRange)
            {
                return;
            }

            if (_spellBag.Count == 0)
            {
                _spellBag.AddRange(new[]
                {
                    Spell.Phalanx, Spell.Phalanx, Spell.GreatOrb, Spell.Starfall, Spell.StarBurst,
                    Spell.Comet, Spell.Prison, Spell.Snare, Spell.Eruption, Spell.Blink,
                });

                for (int i = _spellBag.Count - 1; i > 0; i--)
                {
                    int j = Main.rand.Next(i + 1);
                    Spell swap = _spellBag[i];
                    _spellBag[i] = _spellBag[j];
                    _spellBag[j] = swap;
                }
            }

            bool targetHasFloor = TryFindFloorUnder(target, out _);
            bool airborne = Flight != null && Flight.IsAirborne;

            for (int i = _spellBag.Count - 1; i >= 0; i--)
            {
                Spell candidate = _spellBag[i];

                if (candidate == _lastSpell)
                {
                    continue;
                }

                if (candidate == Spell.GreatOrb && (_greatOrbCooldown > 0 || distance < GreatOrbMinRange))
                {
                    continue;
                }

                if (candidate == Spell.Starfall && (_starfallCooldown > 0 || OpenAirAbove(target) < StarfallNeededAir))
                {
                    continue;
                }

                if (candidate == Spell.StarBurst && (distance < StarBurstMinRange || distance > StarBurstMaxRange))
                {
                    continue;
                }

                // Comet is hover-only: it needs to already be hovering, or be able to take off now.
                bool cometPossible = hoveringNow || canTakeOff;
                if (candidate == Spell.Comet && (_cometCooldown > 0 || distance < CometMinRange || !cometPossible))
                {
                    continue;
                }

                // A held player can't leave the ring, so Prison never follows a Snare.
                if (candidate == Spell.Prison && (_prisonCooldown > 0 || distance > PrisonMaxRange || _lastSpell == Spell.Snare))
                {
                    continue;
                }

                bool snareRoom = ActiveSnareCount() < MaxActiveSnares;
                if (candidate == Spell.Snare && (_snareCooldown > 0 || distance < SnareMinRange || !targetHasFloor || !snareRoom))
                {
                    continue;
                }

                if (candidate == Spell.Eruption && !targetHasFloor)
                {
                    continue;
                }

                if (candidate == Spell.Blink)
                {
                    bool blinkReady = _blinkCooldown == 0 && !airborne && NPC.velocity.Y == 0f && !_blinkFollowUp;
                    if (!blinkReady || !TryFindBlinkDestination(target, out _blinkDestination))
                    {
                        continue;
                    }

                    _spellBag.RemoveAt(i);
                    _lastSpell = candidate;
                    BeginBlink(_blinkDestination);
                    return;
                }

                _spellBag.RemoveAt(i);
                _lastSpell = candidate;
                StartCastOrHoverApproach(candidate, target, canTakeOff, hoveringNow);
                return;
            }

            // Nothing in the bag fits right now: the Phalanx works at any spacing (its stars form beside the caster).
            Spell fallback = Spell.Phalanx;
            if (_lastSpell == Spell.Phalanx && targetHasFloor)
            {
                fallback = Spell.Eruption;
            }

            _spellBag.Remove(fallback);
            _lastSpell = fallback;
            StartCastOrHoverApproach(fallback, target, canTakeOff, hoveringNow);
        }

        // Hover-cast HoverCastChance% of the time, always when the player is well above, and always for the Comet;
        // otherwise cast from here. Never right after a blink, which opens straight into its follow-up cast.
        private void StartCastOrHoverApproach(Spell spell, Player target, bool canTakeOff, bool hoveringNow)
        {
            bool targetAbove = target.Center.Y < NPC.Center.Y - HoverForcedHeight;
            bool rolledHover = Main.rand.Next(100) < HoverCastChance;
            bool wantsHover = (targetAbove || rolledHover) && !_blinkFollowUp;
            if (spell == Spell.Comet)
            {
                wantsHover = true;
            }

            _blinkFollowUp = false;

            if (wantsHover && !hoveringNow && canTakeOff && Flight.RequestTakeoff())
            {
                _pendingSpell = spell;
                _hoverApproach = true;
                _hoverApproachTicks = 0;
                DebugAttackLabel = Spells[(int)spell].Name + ": taking position";
                NPC.netUpdate = true;
                return;
            }

            BeginSpell(spell);
        }

        private void BeginSpell(Spell spell)
        {
            _spell = spell;
            _aimLocked = false;

            if (spell == Spell.GreatOrb)
            {
                _greatOrbCooldown = GreatOrbCooldownTicks;
            }

            if (spell == Spell.Starfall)
            {
                _starfallCooldown = StarfallCooldownTicks;
            }

            if (spell == Spell.Comet)
            {
                _cometCooldown = CometCooldownTicks;
            }

            if (spell == Spell.Prison)
            {
                _prisonCooldown = PrisonCooldownTicks;
            }

            if (spell == Spell.Snare)
            {
                _snareCooldown = SnareCooldownTicks;
            }

            Player target = Main.player[NPC.target];
            _facing = 1;
            if (target.Center.X < NPC.Center.X)
            {
                _facing = -1;
            }

            NPC.direction = _facing;
            NPC.spriteDirection = _facing;
            _aim = (target.Center - NPC.Center).SafeNormalize(Vector2.UnitX * _facing);
            DebugAttackLabel = CurrentSpell.Name;
            SetDisplayWeapon(MagicWeaponItemType, false);
            EnterPhase(AttackPhase.MagicTelegraph, MagicTelegraphTicks);
            NPC.netUpdate = true;
        }

        private void BeginBlink(Vector2 destination)
        {
            _spell = Spell.Blink;
            _blinkDestination = destination;
            _blinkPending = tsorcRevampAIs.QueueTeleportToDestination(NPC, destination, BlinkTelegraphTicks);

            _blinkCooldown = BlinkCooldownTicks;
            if (LowLife)
            {
                _blinkCooldown = BlinkCooldownLowLifeTicks;
            }

            if (!_blinkPending)
            {
                _neutralDelay = 20;
            }

            DebugAttackLabel = CurrentSpell.Name;
            NPC.netUpdate = true;
        }

        // Server-only release. Every spell is spawned here in one go; the stars' hang times do the sequencing.
        protected override void DoMagicAttack()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient || !NPC.HasValidTarget)
            {
                return;
            }

            _aimLocked = true;
            Player target = Main.player[NPC.target];
            IEntitySource source = NPC.GetSource_FromAI();
            int starType = ModContent.ProjectileType<ArchdeaconGlintStar>();
            float fixedMode = (float)ArchdeaconGlintStar.Mode.Fixed;

            if (_spell == Spell.Phalanx)
            {
                // Arc over the head from the back side to the front, so launches sweep forward.
                int starCount = 4;
                if (LowLife)
                {
                    starCount = 6;
                }

                Vector2 head = NPC.Center + new Vector2(0f, -12f);
                for (int i = 0; i < starCount; i++)
                {
                    float progress = i / (float)(starCount - 1);
                    float angleDegrees = MathHelper.Lerp(-160f, -20f, progress);
                    if (_facing == -1)
                    {
                        angleDegrees = MathHelper.Lerp(-20f, -160f, progress);
                    }

                    Vector2 desired = head + MathHelper.ToRadians(angleDegrees).ToRotationVector2() * PhalanxRadius;
                    if (!ArchdeaconGlintStar.TryFindSpawnSpot(desired, target.Center, StarMinPlayerDistance, out Vector2 point))
                    {
                        continue;
                    }

                    Vector2 facingTarget = (target.Center - point).SafeNormalize(Vector2.UnitX * _facing);
                    int hang = PhalanxFirstHang + i * PhalanxLaunchSpacing;

                    Projectile.NewProjectile(source, point, facingTarget, starType, StarDamage, 0f, Main.myPlayer,
                        hang, (float)ArchdeaconGlintStar.Mode.ReaimAtLaunch, PhalanxAcceleration);
                }
            }
            else if (_spell == Spell.GreatOrb)
            {
                int rings = 3;
                int starsPerRing = 6;
                if (LowLife)
                {
                    rings = 4;
                    starsPerRing = 8;
                }

                Projectile.NewProjectile(source, SafeCastOrigin(), _aim * GreatOrbReleaseSpeed, ModContent.ProjectileType<ArchdeaconGlintOrb>(),
                    GreatOrbDamage, 0f, Main.myPlayer, (float)ArchdeaconGlintOrb.Mode.Great, rings * 100 + starsPerRing);
            }
            else if (_spell == Spell.Starfall)
            {
                // Varied heights, a staggered sweep from one side, and per-star acceleration: it should read as falling stars.
                int starCount = 7;
                if (LowLife)
                {
                    starCount = 9;
                }

                bool sweepFromLeft = Main.rand.NextBool();
                float airAbove = OpenAirAbove(target);

                for (int i = 0; i < starCount; i++)
                {
                    float offsetX = (i - (starCount - 1) * 0.5f) * StarfallSpacing + Main.rand.NextFloat(-10f, 10f);
                    float height = Math.Min(Main.rand.NextFloat(StarfallMinHeight, StarfallMaxHeight), airAbove - 24f);
                    Vector2 desired = target.Center + new Vector2(offsetX, -height);
                    if (!ArchdeaconGlintStar.TryFindSpawnSpot(desired, target.Center, StarMinPlayerDistance, out Vector2 point))
                    {
                        continue;
                    }

                    int sweepOrder = i;
                    if (!sweepFromLeft)
                    {
                        sweepOrder = starCount - 1 - i;
                    }

                    int hang = StarfallFirstHang + sweepOrder * StarfallSweepSpacing + Main.rand.Next(0, 7);
                    Vector2 fallDirection = Vector2.UnitY.RotatedBy(MathHelper.ToRadians(Main.rand.NextFloat(-6f, 6f)));
                    float acceleration = Main.rand.NextFloat(0.16f, 0.26f);

                    Projectile.NewProjectile(source, point, fallDirection, starType, StarDamage, 0f, Main.myPlayer,
                        hang, fixedMode, acceleration);
                }
            }
            else if (_spell == Spell.StarBurst)
            {
                // Launched at v0 = 2d / TravelTicks; the orb decelerates to a stop exactly on the point.
                Vector2 tip = SafeCastOrigin();
                Vector2 toPoint = target.Center - tip;
                if (toPoint.Length() > StarBurstMaxRange)
                {
                    toPoint = toPoint.SafeNormalize(Vector2.UnitX) * StarBurstMaxRange;
                }

                Vector2 launch = toPoint * 2f / ArchdeaconGlintOrb.TravelTicks;
                Projectile.NewProjectile(source, tip, launch, ModContent.ProjectileType<ArchdeaconGlintOrb>(),
                    StarDamage, 0f, Main.myPlayer, (float)ArchdeaconGlintOrb.Mode.Burst, 0f);
            }
            else if (_spell == Spell.Comet)
            {
                Vector2 cometOrigin = SafeCastOrigin();
                Vector2 cometDirection = (target.Center - cometOrigin).SafeNormalize(_aim);
                Projectile.NewProjectile(source, cometOrigin, cometDirection, starType, CometDamage, 0f, Main.myPlayer,
                    0f, (float)ArchdeaconGlintStar.Mode.Comet, CometAcceleration);
            }
            else if (_spell == Spell.Prison)
            {
                // The slot facing the Archdeacon is the exit. Each slot is placed in open air with a clear line to the player;
                // a slot with no fair spot is skipped (more exits, never fewer). Stars always point at the player.
                float gapAngle = (NPC.Center - target.Center).ToRotation();

                for (int k = 1; k < PrisonStarCount; k++)
                {
                    Vector2 outward = (gapAngle + MathHelper.TwoPi * k / PrisonStarCount).ToRotationVector2();
                    Vector2 desired = target.Center + outward * PrisonRadius;
                    if (!ArchdeaconGlintStar.TryFindSpawnSpot(desired, target.Center, PrisonMinPlayerDistance, out Vector2 point))
                    {
                        continue;
                    }

                    Vector2 inward = (target.Center - point).SafeNormalize(-outward);
                    Projectile.NewProjectile(source, point, inward, starType, StarDamage, 0f, Main.myPlayer,
                        PrisonHangTicks, fixedMode, PrisonAcceleration);
                }
            }
            else if (_spell == Spell.Snare)
            {
                // Ballistic lob onto the player's floor point; the drop after T ticks is Gravity * T(T+1)/2.
                if (TryFindFloorUnder(target, out Vector2 floorPoint))
                {
                    Vector2 tip = SafeCastOrigin();
                    Vector2 toLanding = floorPoint - new Vector2(0f, 8f) - tip;
                    int flightTicks = (int)MathHelper.Clamp(toLanding.Length() / 6f, 30f, 70f);
                    float gravityDrop = ArchdeaconGlintSnare.Gravity * flightTicks * (flightTicks + 1) / 2f;
                    Vector2 launch = new Vector2(toLanding.X / flightTicks, (toLanding.Y - gravityDrop) / flightTicks);

                    Projectile.NewProjectile(source, tip, launch, ModContent.ProjectileType<ArchdeaconGlintSnare>(), 0, 0f, Main.myPlayer);
                }
            }
            else if (_spell == Spell.Eruption)
            {
                // One rune under every living player in range (per-player attack), each on that player's floor.
                for (int i = 0; i < Main.maxPlayers; i++)
                {
                    Player player = Main.player[i];
                    bool inRange = player.active && !player.dead && NPC.Distance(player.Center) <= MagicRange;

                    if (inRange && TryFindFloorUnder(player, out Vector2 floorPoint))
                    {
                        Projectile.NewProjectile(source, floorPoint, Vector2.Zero,
                            ModContent.ProjectileType<ArchdeaconGlintRune>(), LanceDamage, 0f, Main.myPlayer);
                    }
                }
            }

            NPC.netUpdate = true;
        }

        // Open air straight above the player's head, in px, up to 22 tiles (352px).
        private static float OpenAirAbove(Player player)
        {
            const int ScanTiles = 22;
            int tileX = (int)(player.Center.X / 16f);
            int headRow = (int)(player.position.Y / 16f);

            for (int row = headRow - 1; row > headRow - ScanTiles && row > 1; row--)
            {
                Tile tile = Framing.GetTileSafely(tileX, row);
                if (tile.HasUnactuatedTile && Main.tileSolid[tile.TileType] && !Main.tileSolidTop[tile.TileType])
                {
                    float tileBottom = (row + 1) * 16f;
                    return player.position.Y - tileBottom;
                }
            }

            return ScanTiles * 16f;
        }

        // The staff tip, unless it is inside a wall or blocked from the body (pressed against a wall); then the body centre.
        private Vector2 SafeCastOrigin()
        {
            Vector2 tip = StaffTipWorld;
            bool tipUsable = ArchdeaconGlintStar.SpotIsOpen(tip) && Collision.CanHitLine(NPC.Center, 1, 1, tip, 1, 1);

            if (tipUsable)
            {
                return tip;
            }

            return NPC.Center;
        }

        private static int ActiveSnareCount()
        {
            int snareType = ModContent.ProjectileType<ArchdeaconGlintSnare>();
            int count = 0;

            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                if (Main.projectile[i].active && Main.projectile[i].type == snareType)
                {
                    count++;
                }
            }

            return count;
        }

        protected override void DoMeleeAttack()
        {
        }

        protected override void DoRangedAttack()
        {
        }

        // First solid floor at or below the player's feet, within FloorSearchTiles. Point = top surface of that tile.
        private static bool TryFindFloorUnder(Player player, out Vector2 floorPoint)
        {
            floorPoint = Vector2.Zero;
            int tileX = (int)(player.Center.X / 16f);
            int startY = (int)(player.Bottom.Y / 16f);
            int endY = Math.Min(Main.maxTilesY - 2, startY + FloorSearchTiles);

            if (tileX < 2 || tileX >= Main.maxTilesX - 2 || startY < 2)
            {
                return false;
            }

            for (int y = startY; y <= endY; y++)
            {
                Tile tile = Framing.GetTileSafely(tileX, y);
                bool standable = tile.HasUnactuatedTile && (Main.tileSolid[tile.TileType] || Main.tileSolidTop[tile.TileType]);
                Tile above = Framing.GetTileSafely(tileX, y - 1);
                bool openAbove = !above.HasUnactuatedTile || !Main.tileSolid[above.TileType];

                if (standable && openAbove)
                {
                    floorPoint = new Vector2(player.Center.X, y * 16f);
                    return true;
                }
            }

            return false;
        }

        // Staff overhead for spells conjured away from the tip (and the blink); tip-fired spells point it along _aim.
        private bool StaffRaisedSpell => _spell == Spell.Phalanx || _spell == Spell.GreatOrb || _spell == Spell.Starfall
            || _spell == Spell.Prison || _spell == Spell.Eruption || _spell == Spell.Blink;

        private float CastRotation
        {
            get
            {
                if (StaffRaisedSpell)
                {
                    return -MathHelper.PiOver2;
                }

                float aimAngle = _aim.ToRotation();
                if (_facing != 1)
                {
                    aimAngle = MathHelper.Pi - aimAngle;
                }

                return MathHelper.WrapAngle(aimAngle);
            }
        }

        // Pure function of synced state, so server and clients agree on the tip. Neutral carry is -0.3 rad.
        private float StaffRotation
        {
            get
            {
                const float carry = -0.3f;

                if (_blinkPending)
                {
                    float blinkProgress = 1f - NPC.GetGlobalNPC<tsorcRevampGlobalNPC>().TeleportCountdown / (float)BlinkTelegraphTicks;
                    return MathHelper.Lerp(carry, -MathHelper.PiOver2, MathHelper.SmoothStep(0f, 1f, blinkProgress));
                }

                if (Phase == AttackPhase.MagicTelegraph)
                {
                    // Raise/aim over all but the last 12t of the tell, then hold.
                    float raiseTicks = Math.Max(1, MagicTelegraphTicks - 12);
                    float raiseProgress = MathHelper.Clamp((MagicTelegraphTicks - PhaseTimer) / raiseTicks, 0f, 1f);
                    return MathHelper.Lerp(MagicCastStartRotation, CastRotation, MathHelper.SmoothStep(0f, 1f, raiseProgress));
                }

                if (Phase == AttackPhase.MagicAttack)
                {
                    return CastRotation;
                }

                if (Phase == AttackPhase.MagicRecovery)
                {
                    // Hold the cast pose for 12t, then lower back to the carry over the rest of the recovery.
                    float lowerTicks = Math.Max(1, MagicRecoveryTicks - 12);
                    float lowerProgress = MathHelper.Clamp((MagicRecoveryTicks - PhaseTimer - 12) / lowerTicks, 0f, 1f);
                    return MathHelper.Lerp(CastRotation, carry, MathHelper.SmoothStep(0f, 1f, lowerProgress));
                }

                return carry;
            }
        }

        protected override void ModifyAdditionalPhaseWeaponRotation(ref float weaponRotation)
        {
            weaponRotation = StaffRotation;
        }

        private Vector2 StaffTipWorld
        {
            get
            {
                // Mirrors SyncPuppet + vanilla GetFrontHandPosition with a throwaway Player, so a dedicated server
                // (which never builds the drawn puppet) gets the same tip as the clients. Same method as AttraidiesIllusion.
                _staffPose ??= new Player();
                _staffPose.position = NPC.Bottom - new Vector2(PuppetVisualWidth, PuppetVisualHeight) + PuppetVisualOffset;
                _staffPose.width = PuppetVisualWidth;
                _staffPose.height = PuppetVisualHeight;
                _staffPose.direction = NPC.direction;
                _staffPose.gravDir = 1f;

                float angle = StaffRotation;
                float armRotation = (angle - MathHelper.PiOver2 + CompositeArmRotationOffset) * NPC.direction;
                Vector2 hand = _staffPose.GetFrontHandPosition(CompositeArmStretch, armRotation);

                float worldAngle = angle;
                if (NPC.direction != 1)
                {
                    worldAngle = MathHelper.Pi - angle;
                }

                return hand + worldAngle.ToRotationVector2() * StaffTipReach;
            }
        }

        private bool TryFindBlinkDestination(Player target, out Vector2 destination)
        {
            int side = 1;
            if (Main.rand.NextBool())
            {
                side = -1;
            }

            for (int attempt = 0; attempt < 2; attempt++)
            {
                for (int xOffset = 12; xOffset <= 20; xOffset += 2)
                {
                    int tileX = (int)(target.Center.X / 16f) + side * xOffset;
                    int targetFloorY = (int)(target.Bottom.Y / 16f);

                    for (int tileY = targetFloorY - 8; tileY <= targetFloorY + 8; tileY++)
                    {
                        Vector2 candidate = new Vector2(tileX * 16 + 8, tileY * 16 - NPC.height / 2f);
                        bool lineOfSight = Collision.CanHitLine(candidate, 1, 1, target.Center, 1, 1);

                        if (SafeDestination(candidate) && lineOfSight)
                        {
                            destination = candidate;
                            return true;
                        }
                    }
                }

                side = -side;
            }

            destination = Vector2.Zero;
            return false;
        }

        // Grounded, inside the world, not in lava, within 320px of the target and at least 192px from every player.
        private bool SafeDestination(Vector2 center)
        {
            if (NPC.HasValidTarget && Vector2.Distance(center, Main.player[NPC.target].Center) > 320f)
            {
                return false;
            }

            Vector2 topLeft = center - NPC.Size * 0.5f;
            bool outsideWorld = topLeft.X < 32 || topLeft.Y < 32 || center.X + NPC.width > Main.maxTilesX * 16 - 32 || center.Y + NPC.height > Main.maxTilesY * 16 - 32;
            if (outsideWorld || Collision.SolidCollision(topLeft, NPC.width, NPC.height))
            {
                return false;
            }

            int tileX = (int)(center.X / 16f);
            int floorY = (int)((center.Y + NPC.height / 2f) / 16f);
            Tile floor = Framing.GetTileSafely(tileX, floorY);
            bool solidFloor = floor.HasUnactuatedTile && Main.tileSolid[floor.TileType] && !Main.tileSolidTop[floor.TileType];
            if (!solidFloor)
            {
                return false;
            }

            Tile feet = Framing.GetTileSafely(tileX, floorY - 1);
            if (feet.LiquidAmount > 0 && feet.LiquidType == LiquidID.Lava)
            {
                return false;
            }

            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player player = Main.player[i];
                if (player.active && !player.dead && Vector2.Distance(center, player.Center) < 192f)
                {
                    return false;
                }
            }

            return true;
        }

        private void CancelBlink()
        {
            tsorcRevampGlobalNPC global = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            global.TeleportCountdown = 0;
            global.TeleportAppearanceTimer = 0;
            global.TeleportTelegraph = Vector2.Zero;
            _blinkPending = false;
            NPC.alpha = 0;
        }

        public override void OnStagger(NPC npc)
        {
            _hoverApproach = false;
            _blinkFollowUp = false;
            base.OnStagger(npc);
            CancelBlink();
            _neutralDelay = 60;
            _aimLocked = false;

            if (Main.netMode != NetmodeID.MultiplayerClient && Flight != null && Flight.IsAirborne)
            {
                Flight.EndFlightNow();
                NPC.noGravity = false;
            }

            NPC.netUpdate = true;
        }

        private void Decorate()
        {
            tsorcRevampGlobalNPC global = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            if (global.TeleportCountdown == 0 && global.TeleportAppearanceTimer == 0)
            {
                NPC.alpha = 0;
            }

            // Pale glintstone aura; below 30% life a heavier blue mist sinks off the body.
            Lighting.AddLight(NPC.Center, AuraColor.ToVector3() * 0.5f);
            if (NPC.life <= NPC.lifeMax * 0.3f)
            {
                if (Main.GameUpdateCount % 2 == 0)
                {
                    Dust mist = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, DustID.Smoke, 0f, -0.6f, 140, new Color(120, 160, 230), 1.3f);
                    mist.noGravity = true;
                }
            }
            else if (Main.GameUpdateCount % 4 == 0)
            {
                Dust glint = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, DustID.TintableDustLighted, 0f, -0.4f, 80, AuraColor, 0.8f);
                glint.noGravity = true;
            }

            if (_blinkPending)
            {
                GatherDust(StaffTipWorld, AuraColor, 16f);
                GatherDust(_blinkDestination, AuraColor, 24f);
            }

            if (Phase == AttackPhase.MagicAttack)
            {
                GatherDust(StaffTipWorld, MagicTelegraphFlashColor, 5f);
            }

            // Levitation: frost drifting down from the feet while airborne.
            if (Flight != null && Flight.IsAirborne && Main.GameUpdateCount % 3 == 0)
            {
                Dust lift = Dust.NewDustPerfect(NPC.Bottom + Main.rand.NextVector2Circular(12f, 4f), DustID.TintableDustLighted, new Vector2(0f, 1.2f), 80, AuraColor, 1f);
                lift.noGravity = true;
            }
        }

        protected override void DoMagicTelegraphVFX(float progress)
        {
            if (Main.dedServ)
            {
                return;
            }

            GatherDust(StaffTipWorld, MagicTelegraphFlashColor, MathHelper.Lerp(20f, 5f, progress));
        }

        private static void GatherDust(Vector2 point, Color color, float radius)
        {
            Vector2 offset = Main.rand.NextVector2CircularEdge(radius, radius);
            Vector2 inward = -offset.SafeNormalize(Vector2.UnitY) * 1.8f;
            Dust dust = Dust.NewDustPerfect(point + offset, DustID.TintableDustLighted, inward, 70, color, 0.8f);
            dust.noGravity = true;
            Lighting.AddLight(point, color.ToVector3() * 0.25f);
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            // Spells fired from the tip grow their real art there over the tell: the great orb's sphere for Great Orb and
            // Star Burst, a star glint for the Comet and Snare. Spells conjured elsewhere only gather light at the tip.
            bool charging = Phase == AttackPhase.MagicTelegraph;
            bool tipSpell = _spell == Spell.GreatOrb || _spell == Spell.StarBurst || _spell == Spell.Comet || _spell == Spell.Snare;

            if (charging && tipSpell)
            {
                float tellProgress = 1f - PhaseTimer / (float)MagicTelegraphTicks;
                Vector2 tip = StaffTipWorld;
                Vector2 tipOnScreen = tip - screenPos;
                Color glow = new Color(255, 255, 255, 0) * tellProgress;
                bool orbSpell = _spell == Spell.GreatOrb || _spell == Spell.StarBurst;

                if (!GlintLegacyCommand.UseLegacySprites)
                {
                    // Default look: the same shaders the projectiles use, growing at the tip over the tell.
                    if (orbSpell)
                    {
                        float fullDiameter = 100f;
                        if (_spell == Spell.StarBurst)
                        {
                            fullDiameter = 64f;
                        }

                        EnemyVFX.DrawArchdeaconGlintOrb(tip, fullDiameter * tellProgress, tellProgress);
                    }
                    else
                    {
                        float glintHeight = 24f;
                        if (_spell == Spell.Comet)
                        {
                            glintHeight = 40f;
                        }

                        EnemyVFX.DrawArchdeaconGlintComet(tip, _aim.ToRotation(), glintHeight * tellProgress, 0f, tellProgress);
                    }
                }
                else if (orbSpell)
                {
                    Texture2D sheet = TextureAssets.Projectile[ModContent.ProjectileType<ArchdeaconGlintOrb>()].Value;
                    Rectangle sphereFrame = new Rectangle(0, 0, sheet.Width, sheet.Height / 5);
                    float fullScale = 0.9f;
                    if (_spell == Spell.StarBurst)
                    {
                        fullScale = 0.55f;
                    }

                    spriteBatch.Draw(sheet, tipOnScreen, sphereFrame, glow, 0f, sphereFrame.Size() * 0.5f, fullScale * tellProgress, SpriteEffects.None, 0f);
                }
                else
                {
                    Texture2D star = ModContent.Request<Texture2D>(UsefulFunctions.RefactorableFilepath(typeof(HomingStarStar))).Value;
                    float starScale = tellProgress;
                    if (_spell == Spell.Comet)
                    {
                        starScale *= 1.4f;
                    }

                    spriteBatch.Draw(star, tipOnScreen, null, glow, Main.GameUpdateCount * 0.1f, star.Size() * 0.5f, starScale, SpriteEffects.None, 0f);
                }

                // The comet starts straight along the aim, so its tell carries an aim line.
                if (_spell == Spell.Comet)
                {
                    Color lineColor = ArchdeaconGlintStar.GlintColor * (0.15f + 0.3f * tellProgress);
                    Utils.DrawLine(spriteBatch, tip, tip + _aim * 260f, lineColor, ArchdeaconGlintStar.GlintColor * 0f, 2f);
                }
            }

            return base.PreDraw(spriteBatch, screenPos, drawColor);
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            base.SendExtraAI(writer);
            writer.Write((byte)_spell);
            writer.Write((byte)_pendingSpell);
            writer.Write((sbyte)_facing);
            writer.Write(_aim.X);
            writer.Write(_aim.Y);
            writer.Write(_aimLocked);
            writer.Write(_blinkDestination.X);
            writer.Write(_blinkDestination.Y);
            writer.Write(_blinkPending);
            writer.Write(_blinkFollowUp);
            writer.Write(_hoverApproach);
            writer.Write((short)_hoverApproachTicks);
            writer.Write((short)_neutralDelay);
            writer.Write((short)_blinkCooldown);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            base.ReceiveExtraAI(reader);
            _spell = (Spell)reader.ReadByte();
            _pendingSpell = (Spell)reader.ReadByte();
            _facing = reader.ReadSByte();
            _aim = new Vector2(reader.ReadSingle(), reader.ReadSingle());
            _aimLocked = reader.ReadBoolean();
            _blinkDestination = new Vector2(reader.ReadSingle(), reader.ReadSingle());
            _blinkPending = reader.ReadBoolean();
            _blinkFollowUp = reader.ReadBoolean();
            _hoverApproach = reader.ReadBoolean();
            _hoverApproachTicks = reader.ReadInt16();
            _neutralDelay = reader.ReadInt16();
            _blinkCooldown = reader.ReadInt16();

            DebugAttackLabel = CurrentSpell.Name;
            if (_hoverApproach)
            {
                DebugAttackLabel = Spells[(int)_pendingSpell].Name + ": taking position";
            }
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            npcLoot.Add(new CommonDrop(ModContent.ItemType<HealingElixir>(), 10, 1, 1, 3));
            npcLoot.Add(new CommonDrop(ItemID.ManaRegenerationPotion, 25));
            npcLoot.Add(new CommonDrop(ItemID.GreaterHealingPotion, 10));
            npcLoot.Add(ItemDropRule.ByCondition(tsorcRevamp.tsorcItemDropRuleConditions.CursedRule, ModContent.ItemType<StarlightShardItem>(), 20));
        }

        public override void HitEffect(NPC.HitInfo hit)
        {
            if (NPC.life > 0 || Main.dedServ)
            {
                return;
            }

            Vector2 center = NPC.Center;
            string[] goreNames = { "Undead Caster Gore 1", "Undead Caster Gore 2", "Undead Caster Gore 2", "Undead Caster Gore 3", "Undead Caster Gore 3" };

            foreach (string goreName in goreNames)
            {
                Vector2 goreVelocity = new Vector2(Main.rand.Next(-30, 31) * 0.2f, Main.rand.Next(-30, 31) * 0.2f);
                Gore.NewGore(NPC.GetSource_Death(), center, goreVelocity, Mod.Find<ModGore>(goreName).Type, 1f);
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items;
using tsorcRevamp.Content.Items.Materials;
using tsorcRevamp.Content.Items.Materials.Souls;
using tsorcRevamp.Content.Items.Materials.Titanite;
using tsorcRevamp.Content.Items.Potions;
using tsorcRevamp.Content.Items.Potions.StarlightShard;
using tsorcRevamp.Content.Items.Weapons.Enemy;
using tsorcRevamp.Content.Projectiles.Enemy;
using tsorcRevamp.Content.Projectiles.Enemy.Acolyte;
using tsorcRevamp.Content.Projectiles.Enemy.OolacileSorcerer;
using tsorcRevamp.NPCs.Puppets;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.NPCs.Enemies.SuperHardMode
{
    // Acolyte of Grand Occultist: a rare Super Hardmode puppet caster in the Grand Occultist's robe and dyes (the minion read
    // is visual only: no boss tie-in, no summons). Grounded; it kites at 10-20 tiles and blinks often.
    // Plan: tsorcDocs/EnemyRevampPlans/OolacileAcolyte_Puppet_Plan.md (revision 4).
    //
    // Above 50% it casts the order's dark-red Abyss spells, with Madness leaking through in a few yellow casts.
    // At 50% it CRACKS: the red spells are gone, everything is yellow-white Madness, the cast pace drops 40 -> 25t and its
    // blinks chain. Almost every Madness hit adds a small amount to the player's Madness meter (AcolyteMadness table).
    //
    // Attack spec (ticks at 60/s; damage is the raw value passed to NewProjectile):
    //   Frenzied Step   reactive blink: 10t departure flicker / swap / 8t arrival flash / 10t unarmored recovery. Lands
    //                   12-20 tiles from the player with line of sight. Below 50%: a chain of 2-3 hops 14t apart, each
    //                   departure leaving a Hallucination. Cooldown 150t (90t below 50%).
    //   Shriek          reactive, player within 4 tiles: 16t tell / a 160px Madness ring over 14t / 20t recovery. Dmg 30.
    //   Bolt Volley     (>50%) 30t aimed tell / 3 OolacileBolts 10t apart, each re-aimed with 8t lead / 36t. Dmg 38.
    //   Seeker Pair     (>50%) 36t raised tell / two seekers swing wide then curve in / 36t. Cooldown 300t. Dmg 32.
    //   Abyss Drop      (>50%) 40t tell under a ring 320px over the player / a falling orb that bursts into beads /
    //                   40t. Cooldown 420t. Dmg 40 (beads 24).
    //   Gaze of Madness 30t aimed tell / 3 eyes (5) that track, lock and beam / 40t. Cooldown 360t. Dmg 34.
    //   Mad Scrawl      24t tell (30t base floor) / a written line that arms then ripples / 36t. X below 50%. Cd 300t. Dmg 36.
    //   Babbling Spiral 30t tell + 90t channel (the stagger window: 60t interruptible, then a commit cue) / 50t. Cd 540t.
    //   Frenzied Volley (<50%) 26t tell (30t floor) / 6 jittering motes 7t apart / 30t. Dmg 30.
    //   Cat's Cradle    zone: 30t tell / 5 (7) threads strung wall to wall, slack 40t, taut 10t, slice 10t / 36t. Cd 420t.
    //   Mirror of Madness zone (<50%): 36t tell / two echoes replay the player's own path 1s and 2s behind for 5s / 40t.
    //   Madness Clock   zone (<50%): 30t tell / a 12-eye ring fires across itself as a clock hand sweeps twice / 40t.
    //   Mark of Madness (<50%) 40t tether tell / 8s Marked: Madness stops decaying, this acolyte's Madness hits add double.
    // Selection: server-only shuffled bag, no immediate repeats; cooldown-gated entries are skipped but kept. Only one zone
    // spell alive at a time, and Mad Scrawl / Cat's Cradle are never dealt back to back.
    [LegacyName("OolacileSorcerer")]
    class OolacileAcolyte : PuppetNPC
    {
        private enum Spell : byte
        {
            BoltVolley,
            SeekerPair,
            AbyssDrop,
            Gaze,
            Scrawl,
            Spiral,
            Cradle,
            FrenziedVolley,
            Mirror,
            Clock,
            Mark,
            Count,
        }

        private enum CustomKind : byte
        {
            None,
            Step,
            Shriek,
            Crack,
            Dissolve,
        }

        private readonly struct SpellSpec
        {
            public readonly string Name;
            public readonly int Tell;
            public readonly int Active;
            public readonly int Recovery;
            public readonly int Cooldown;
            public readonly bool IsZone;

            public SpellSpec(string name, int tell, int active, int recovery, int cooldown, bool isZone)
            {
                Name = name;
                Tell = tell;
                Active = active;
                Recovery = recovery;
                Cooldown = cooldown;
                IsZone = isZone;
            }
        }

        // Indexed by Spell. Active = the magic-phase length; channels emit from DoMagicTick across it.
        // Tells under 30t are floored to 30 by PuppetNPC.EnterPhase (Scrawl 24, Frenzied Volley 26).
        private static readonly SpellSpec[] Spells =
        {
            new SpellSpec("Bolt Volley", 30, 21, 36, 0, false),
            new SpellSpec("Seeker Pair", 36, 1, 36, 300, false),
            new SpellSpec("Abyss Drop", 40, 1, 40, 420, false),
            new SpellSpec("Gaze of Madness", 30, 1, 40, 360, false),
            new SpellSpec("Mad Scrawl", 24, 11, 36, 300, false),
            new SpellSpec("Babbling Spiral", 30, 90, 50, 540, false),
            new SpellSpec("Cat's Cradle", 30, 1, 36, 420, true),
            new SpellSpec("Frenzied Volley", 26, 36, 30, 0, false),
            new SpellSpec("Mirror of Madness", 36, 1, 40, 600, true),
            new SpellSpec("Madness Clock", 30, 1, 40, 540, true),
            new SpellSpec("Mark of Madness", 40, 1, 40, 720, false),
        };

        private static readonly Spell[] UpperBag =
        {
            Spell.BoltVolley, Spell.BoltVolley, Spell.SeekerPair, Spell.AbyssDrop, Spell.Gaze, Spell.Scrawl, Spell.Spiral, Spell.Cradle,
        };

        private static readonly Spell[] LowerBag =
        {
            Spell.FrenziedVolley, Spell.FrenziedVolley, Spell.Gaze, Spell.Scrawl, Spell.Spiral, Spell.Cradle, Spell.Mirror, Spell.Clock, Spell.Mark,
        };

        public static readonly Color MadnessYellow = new Color(255, 220, 45);
        public static readonly Color AbyssRed = new Color(190, 20, 45);

        // Cast pace: the breathing gap between two spells (the plan's MagicCooldownAfterUse).
        private const int CastPaceTicks = 40;
        private const int CastPaceCrackedTicks = 25;

        private const float MagicReach = 640f;
        private const float KiteMinTiles = 10f;
        private const float KiteMaxTiles = 20f;

        // Grip 15% up the 83.6px butt-to-gem diagonal at 0.65 draw scale: 83.6 * 0.85 * 0.65 = 46px from hand to gem.
        private const float StaffDrawScale = 0.65f;
        private const float StaffTipReach = 46f;
        // Magic-pose space: 0 = level forward, -PiOver2 = straight up. Neutral carry sits slightly raised.
        private const float StaffCarryRotation = -0.3f;
        private const float StaffRaisedRotation = -MathHelper.PiOver2;

        // Frenzied Step. Destination: 12-20 tiles from the player, >= 8 tiles from the previous spot, LOS + floor.
        private const int StepDepartureTicks = 10;
        private const int StepHopSpacingTicks = 14;
        private const int StepArrivalFlashTicks = 8;
        private const int StepRecoveryTicks = 10;
        private const int StepCooldownTicks = 150;
        private const int StepCooldownCrackedTicks = 90;
        private const int StepRetryTicks = 60;
        private const int StepSearchTries = 100;
        private const int StepMinTiles = 12;
        private const int StepMaxTiles = 20;
        private const float StepMinMovePx = 8f * 16f;
        private const int MaxStepHops = 3;
        private const float StepCloseRangePx = 5f * 16f;
        private const int StepCloseTriggerTicks = 45;
        private const int StepNoSightTriggerTicks = 120;
        private const int StepEverySpells = 2;

        // Shriek: 16t tell, ring over 14t, 20t recovery.
        private const int ShriekTellTicks = 16;
        private const int ShriekRingTicks = 14;
        private const int ShriekRecoveryTicks = 20;
        private const float ShriekTriggerRangePx = 64f;
        private const int ShriekCooldownTicks = 180;
        private const int ShriekDamage = 30;

        // Bolt Volley: 3 bolts 10t apart, 8 -> 10 px/t, each re-aimed at where the player will be in 8t.
        private const int BoltDamage = 38;
        private const int BoltCount = 3;
        private const int BoltSpacingTicks = 10;
        private const float BoltSpeed = 8f;
        private const float BoltTopSpeed = 10f;
        private const float BoltLeadTicks = 8f;

        private const int SeekerDamage = 32;

        // Gaze of Madness: 3 eyes (5 cracked) 220-260px from the player across the upper half-circle, opening 16t (12t) apart.
        private const int GazeDamage = 34;
        private const int GazeEyes = 3;
        private const int GazeEyesCracked = 5;
        private const int GazeSpacingTicks = 16;
        private const int GazeSpacingCrackedTicks = 12;
        private const float GazeMinRadius = 220f;
        private const float GazeMaxRadius = 260f;

        // Mad Scrawl: aimed to end 160px past the player (capped at 720px); the cracked X's second line starts 10t later.
        private const int ScrawlDamage = 36;
        private const float ScrawlOvershoot = 160f;
        private const float ScrawlMaxLength = 720f;
        private const int ScrawlSecondDelay = 10;

        // Babbling Spiral: one mote per arm every 6t at 5 px/t, the emitter turning 9 degrees per emission.
        private const int SpiralDamage = 26;
        private const int SpiralEmitTicks = 6;
        private const float SpiralTurnDegrees = 9f;
        private const float SpiralMoteSpeed = 5f;
        private const float SpiralOrbHeight = 32f;

        // Frenzied Volley: 6 motes 7t apart at 9 px/t, each re-aimed 8t ahead.
        private const int VolleyDamage = 30;
        private const int VolleyCount = 6;
        private const int VolleySpacingTicks = 7;
        private const float VolleySpeed = 9f;

        private const int MaxHallucinations = 3;

        // Cat's Cradle: 5 threads (7 cracked), each a chord through a point within 96px of the player at evenly spread angles
        // (+-10 degrees), anchored at the first solid tile each way, capped at 30 tiles. Threads start 6t apart.
        private const int CradleDamage = 34;
        private const int CradleThreads = 5;
        private const int CradleThreadsCracked = 7;
        private const int CradleThreadsOpenAir = 4;
        private const float CradleCenterSpread = 96f;
        private const float CradleMaxReach = 30f * 16f;
        private const float CradleMinSpan = 48f;
        private const int CradleSpacingTicks = 6;

        // Mirror of Madness: two echoes, 1s and 2s behind the player.
        private const int MirrorDamage = 34;
        private const int MirrorFirstDelay = 60;
        private const int MirrorSecondDelay = 120;

        // Babbling Spiral: the last 30t of the 90t channel are committed (hyper-armored), with a cue at the flip.
        private const int SpiralCommitTicks = 30;

        // Abyss Drop is skipped when there is less than this much open air over the player's head.
        private const float AbyssDropMinAir = 160f;

        // Death: the body dissolves over 20t into yellow and black dust while the staff falls, then the real kill.
        private const int DissolveTicks = 20;

        // The Crack: a 50t beat; the white ring goes off at tick 20.
        private const int CrackTicks = 50;
        private const int CrackRingTick = 20;

        private readonly List<Spell> _spellBag = new List<Spell>();
        private readonly int[] _spellCooldowns = new int[(int)Spell.Count];
        private readonly Vector2[] _stepHops = new Vector2[MaxStepHops];
        private Spell _spell;
        private Spell _lastSpell = Spell.Count;
        private CustomKind _customKind;
        private Vector2 _aim = Vector2.UnitX;
        private Vector2 _stepOrigin;
        private bool _aimLocked;
        private bool _cracked;
        private bool _dissolving;
        private bool _dissolveComplete;
        private bool _initialized;
        private int _stepHopCount;
        private int _facing = 1;
        private int _neutralDelay = 40;
        private int _stepCooldown = 90;
        private int _shriekCooldown;
        private int _spellsSinceStep;
        // Server-only trigger clocks; they only feed the server's selection.
        private int _closeTicks;
        private int _noSightTicks;
        private Player _staffPose;
        // Server-only: the Babbling Spiral's starting emitter angle for this cast (only the server spawns its motes).
        private float _spiralBaseAngle;
        // Client-side: the current tell's sound cue has played (reset whenever the acolyte is not in a tell).
        private bool _tellCuePlayed;

        private SpellSpec CurrentSpell => Spells[(int)_spell];

        // Read by MarkGlyph: the glyph fizzles if the cast was interrupted, and its tether starts at the staff tip.
        internal bool IsCastingMark
        {
            get
            {
                bool castPhase = Phase == AttackPhase.MagicTelegraph || Phase == AttackPhase.MagicAttack || Phase == AttackPhase.MagicRecovery;
                return castPhase && _spell == Spell.Mark;
            }
        }

        // Cosmetic anchor for projectiles that start at the staff (the Mark tether, Cat's Cradle's first leg).
        internal Vector2 StaffTipForVisuals => StaffTipWorld;

        private Vector2 SpiralOrbPosition => NPC.Top - new Vector2(0f, SpiralOrbHeight);

        // Read by AbyssDropPortal: the portal collapses without dropping if the cast was interrupted.
        internal bool IsCastingAbyssDrop
        {
            get
            {
                bool castPhase = Phase == AttackPhase.MagicTelegraph || Phase == AttackPhase.MagicAttack || Phase == AttackPhase.MagicRecovery;
                return castPhase && _spell == Spell.AbyssDrop;
            }
        }
        private bool IsNeutral => Phase == AttackPhase.Idle || Phase == AttackPhase.CasualStroll;
        private bool InCustom(CustomKind kind) => Phase == AttackPhase.Custom && _customKind == kind;

        public override bool UsesPuppetDifficultyScaling => false;
        protected override bool AnnounceInvasion => false;
        protected override bool DespawnsOnPartyWipe => false;
        protected override string InvaderTitle => "Acolyte of Grand Occultist";

        // The Grand Occultist's robe and dyes, with a lesser boss mask in the same Hades flame.
        protected override int HeadArmorItemType => ItemID.EaterMask;
        protected override int BodyArmorItemType => ItemID.SpectreRobe;
        protected override int LegsArmorItemType => ItemID.SpectrePants;
        protected override int HeadArmorDyeItemType => ItemID.BurningHadesDye;
        protected override int BodyArmorDyeItemType => ItemID.BrownAndBlackDye;
        protected override int LegsArmorDyeItemType => ItemID.BrownAndBlackDye;
        protected override Color PuppetSkinColor => new Color(38, 22, 26);

        protected override int MeleeWeaponItemType => -1;
        protected override int RangedWeaponItemType => -1;
        protected override int MagicWeaponItemType => ModContent.ItemType<EnemyOolacileAcolyteStaff>();
        protected override int MeleeDamage => 0;
        protected override int RangedDamage => 0;
        protected override int MagicDamage => BoltDamage;
        protected override int EstusChargesMax => 0;
        protected override int CasualStrollChance => 0;
        // The subclass owns neutral selection (bag, reactive moves); the base owns every cast phase and its network clock.
        protected override bool HoldAttackSelection => true;
        protected override bool ShowWeaponDuringNeutral => true;
        protected override float TopSpeed => 1.6f;
        protected override float Acceleration => 0.07f;
        protected override float RunSpeedMult => 1f;
        protected override float PuppetJumpPower => 7.5f;
        protected override float PuppetJumpBoost => 3f;
        protected override float MagicRange => MagicReach;
        protected override int MagicTelegraphTicks => CurrentSpell.Tell;
        protected override int MagicAttackTicks => CurrentSpell.Active;
        protected override int MagicRecoveryTicks => CurrentSpell.Recovery;
        protected override int MagicCooldownAfterUse => 0;
        protected override bool BrakeDuringMagicCast => _spell == Spell.Spiral;
        protected override bool UseAuthoredMagicCastPose => true;
        protected override float MagicCastStartRotation => StaffCarryRotation;
        protected override float MagicCastEndRotation => CastRotation;
        protected override float MagicWeaponRotationOffset => MathHelper.PiOver4;
        protected override bool MirrorMagicWeaponRotationByFacing => true;
        protected override int MagicWeaponRecoveryHoldTicks => MagicRecoveryTicks;
        protected override int MagicTelegraphFlashLeadTicks => MagicTelegraphTicks;
        protected override bool UseCompositeArmForAdditionalPhase => true;
        protected override bool UseCompositeArmSwing => true;
        // GrandOolacileStaff.png: butt (0,63) to gem (55,0); grip 15% up from the butt (the Grand Occultist's grip).
        protected override Vector2 MagicGripNorm => new Vector2(0.14f, 0.84f);
        protected override Color MagicTelegraphFlashColor => SpellIsMadness(_spell) ? MadnessYellow : AbyssRed;

        protected override float GetHeldRangedDrawScale(int itemType)
        {
            if (itemType == ModContent.ItemType<EnemyOolacileAcolyteStaff>())
            {
                return StaffDrawScale;
            }

            return base.GetHeldRangedDrawScale(itemType);
        }

        public override void SetStaticDefaults()
        {
            // The puppet body is drawn by the base; OolacileAcolyte.png is only the bestiary/placeholder sprite.
            Main.npcFrameCount[Type] = 3;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Confused] = true;
        }

        public override void SetDefaults()
        {
            NPC.aiStyle = -1;
            NPC.npcSlots = 5;
            NPC.damage = 0;
            NPC.defense = 92;
            NPC.width = 28;
            NPC.height = 44;
            // Authored 5500; the EnemyBalance registry sets the real Super Hardmode value (15 000).
            NPC.lifeMax = 5500;
            NPC.scale = 1f;
            NPC.knockBackResist = 0f; // overridden by the PoiseProfiles entry
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath6;
            NPC.noGravity = false;
            NPC.noTileCollide = false;
            NPC.lavaImmune = true;
            NPC.timeLeft = 22500;
            NPC.value = 8000;
            Banner = Type;
            BannerItem = ModContent.ItemType<Banners.OolacileAcolyteBanner>();

            tsorcRevampGlobalNPC global = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            global.PoiseStaggerResetsAI = true;
            global.NavSearchRadius = 50;
            global.RemembersLastKnownPos = true;
            global.CanWalkBackwards = true;
            global.KiteRangeMin = KiteMinTiles;
            global.KiteRangeMax = KiteMaxTiles;
            global.KiteLooseness = 0.2f;
            // Frenzied Step is scheduled by this class, never by FighterAI's stuck-recovery teleport.
            global.CanTeleport = false;
        }

        // Used only while New Enemy Spawns is off; the EnemySpawns registry replaces it otherwise. One alive at a time, SHM only.
        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            if (!tsorcRevampWorld.SuperHardMode || NPC.AnyNPCs(Type))
            {
                return 0f;
            }

            Player player = spawnInfo.Player;
            bool corruption = player.ZoneCorrupt || player.ZoneCrimson;
            bool aboveEarth = spawnInfo.SpawnTileY < Main.worldSurface;
            bool caverns = spawnInfo.SpawnTileY >= Main.rockLayer && spawnInfo.SpawnTileY < Main.maxTilesY - 200;
            bool ocean = spawnInfo.SpawnTileX < 250 || spawnInfo.SpawnTileX > Main.maxTilesX - 250;

            if (player.ZoneJungle && !player.ZoneDungeon && !corruption && aboveEarth && !ocean)
            {
                return 1f / 70f;
            }

            if (ocean && !aboveEarth)
            {
                return 1f / 25f;
            }

            if (Main.bloodMoon && player.ZoneJungle && !player.ZoneDungeon && !corruption && !aboveEarth && !ocean)
            {
                return 1f / 30f;
            }

            if (player.ZoneCrimson)
            {
                return 1f / 20f;
            }

            if (caverns)
            {
                return 1f / 50f;
            }

            if (Main.bloodMoon && player.ZoneDungeon)
            {
                return 1f / 8f;
            }

            return 0f;
        }

        protected override void RunMovementAI(float speedMult)
        {
            // Plant for set-pieces, the release, and the locked end of an aimed tell; walk slower through the rest of a cast.
            bool aimedTellLocked = _aimLocked && Phase == AttackPhase.MagicTelegraph;
            bool planted = Phase == AttackPhase.Custom || Phase == AttackPhase.MagicAttack || aimedTellLocked;

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

            if (_stepCooldown > 0)
            {
                _stepCooldown--;
            }

            if (_shriekCooldown > 0)
            {
                _shriekCooldown--;
            }

            if (server)
            {
                for (int i = 0; i < _spellCooldowns.Length; i++)
                {
                    _spellCooldowns[i] = Math.Max(0, _spellCooldowns[i] - 1);
                }
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

                _aim = (target.Center - NPC.Center).SafeNormalize(Vector2.UnitX * _facing);

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

            // A cast or set-piece just finished: breathe for the cast pace, then pick again.
            bool finishedCast = previousPhase == AttackPhase.MagicRecovery && IsNeutral;
            bool finishedCustom = previousPhase == AttackPhase.Custom && IsNeutral;
            if (server && finishedCast)
            {
                _neutralDelay = CastPaceTicks;
                if (_cracked)
                {
                    _neutralDelay = CastPaceCrackedTicks;
                }

                _spellsSinceStep++;
                NPC.netUpdate = true;
            }

            if (server && finishedCustom)
            {
                FinishCustom();
            }

            ApplyPoiseWindows(global);

            if (server && global.StaggerTimer <= 0)
            {
                TrackStepTriggers();

                if (IsNeutral)
                {
                    SelectNeutralAction();
                }
            }

            if (!Main.dedServ)
            {
                if (Phase != AttackPhase.MagicTelegraph)
                {
                    _tellCuePlayed = false;
                }

                Decorate();
            }
        }

        // Poise: most tells are interruptible except their last 8t; releases and set-piece actives are hyper-armored;
        // recoveries (spell, Shriek, blink arrival) have no armour. The Babbling Spiral channel is the stagger window.
        private void ApplyPoiseWindows(tsorcRevampGlobalNPC global)
        {
            bool tellLocked = Phase == AttackPhase.MagicTelegraph && PhaseTimer <= 8;
            bool releasing = Phase == AttackPhase.MagicAttack;
            bool spiralInterruptible = releasing && _spell == Spell.Spiral && PhaseTimer > SpiralCommitTicks;

            int customElapsed = CustomElapsed();
            bool stepDeparting = InCustom(CustomKind.Step) && customElapsed < StepDepartureTicks;
            bool stepHopping = InCustom(CustomKind.Step) && customElapsed >= StepDepartureTicks && customElapsed < StepLastSwapTick();
            bool shriekTell = InCustom(CustomKind.Shriek) && customElapsed < ShriekTellTicks;
            bool shriekActive = InCustom(CustomKind.Shriek) && customElapsed >= ShriekTellTicks && customElapsed < ShriekTellTicks + ShriekRingTicks;
            bool cracking = InCustom(CustomKind.Crack) || InCustom(CustomKind.Dissolve);

            global.AttackCommitted = tellLocked || (releasing && !spiralInterruptible) || stepHopping || shriekActive || cracking;
            global.AttackTelegraphing = !global.AttackCommitted
                && (Phase == AttackPhase.MagicTelegraph || spiralInterruptible || stepDeparting || shriekTell);

            if (global.StaggerTimer > 0)
            {
                global.AttackCommitted = false;
                global.AttackTelegraphing = false;
            }
        }

        // Server-only. Counts how long the player has been close / out of sight, for Frenzied Step's triggers.
        private void TrackStepTriggers()
        {
            if (!NPC.HasValidTarget)
            {
                _closeTicks = 0;
                _noSightTicks = 0;
                return;
            }

            Player target = Main.player[NPC.target];
            float distance = NPC.Distance(target.Center);

            if (distance <= StepCloseRangePx)
            {
                _closeTicks++;
            }
            else
            {
                _closeTicks = 0;
            }

            bool clear = Collision.CanHitLine(NPC.Center, 1, 1, target.Center, 1, 1);
            if (clear)
            {
                _noSightTicks = 0;
            }
            else
            {
                _noSightTicks++;
            }
        }

        // Server-only. Order of precedence: the Crack, Shriek, Frenzied Step, then a spell from the bag.
        private void SelectNeutralAction()
        {
            if (!NPC.HasValidTarget)
            {
                return;
            }

            Player target = Main.player[NPC.target];
            if (!target.active || target.dead)
            {
                return;
            }

            bool grounded = NPC.velocity.Y == 0f;
            float distance = NPC.Distance(target.Center);

            // The Crack fires once, the moment it is free after dropping to half life. Flagging on start means burst damage
            // during the beat can never trigger it twice.
            if (!_cracked && NPC.life <= NPC.lifeMax / 2)
            {
                _cracked = true;
                _spellBag.Clear();
                _customKind = CustomKind.Crack;
                DebugAttackLabel = "The Crack";
                FaceTarget(target);
                StartCustomAttack(CrackTicks, MagicWeaponItemType);
                NPC.netUpdate = true;
                return;
            }

            if (grounded && _shriekCooldown == 0 && distance <= ShriekTriggerRangePx)
            {
                _shriekCooldown = ShriekCooldownTicks;
                _customKind = CustomKind.Shriek;
                DebugAttackLabel = "Shriek";
                FaceTarget(target);
                StartCustomAttack(ShriekTellTicks + ShriekRingTicks + ShriekRecoveryTicks, MagicWeaponItemType);
                NPC.netUpdate = true;
                return;
            }

            bool crowded = _closeTicks >= StepCloseTriggerTicks;
            bool lostSight = _noSightTicks >= StepNoSightTriggerTicks;
            bool stepDue = _spellsSinceStep >= StepEverySpells;
            if (grounded && _stepCooldown == 0 && (crowded || lostSight || stepDue))
            {
                if (TryBeginStep(target))
                {
                    return;
                }

                // Every search failed (cramped terrain): no blink, and retry after a short wait instead of every tick.
                _stepCooldown = StepRetryTicks;
                NPC.netUpdate = true;
            }

            if (_neutralDelay > 0)
            {
                return;
            }

            bool clear = Collision.CanHitLine(NPC.Center, 1, 1, target.Center, 1, 1);
            if (!clear || distance > MagicRange)
            {
                return;
            }

            BeginSpell(DrawSpell(target));
        }

        // Server-only. Deals from the phase's bag; an entry whose gate is closed is skipped but stays in the bag.
        private Spell DrawSpell(Player target)
        {
            for (int attempt = 0; attempt < 2; attempt++)
            {
                for (int i = _spellBag.Count - 1; i >= 0; i--)
                {
                    Spell candidate = _spellBag[i];
                    if (!SpellAllowed(candidate, target))
                    {
                        continue;
                    }

                    _spellBag.RemoveAt(i);
                    return candidate;
                }

                RefillSpellBag();
            }

            // Everything is gated out right now: the phase's bread-and-butter volley always works.
            if (_cracked)
            {
                return Spell.FrenziedVolley;
            }

            return Spell.BoltVolley;
        }

        private bool SpellAllowed(Spell candidate, Player target)
        {
            if (candidate == _lastSpell || _spellCooldowns[(int)candidate] > 0)
            {
                return false;
            }

            // Bag left over from before the Crack: the order's red spells are gone.
            bool redSpell = candidate == Spell.BoltVolley || candidate == Spell.SeekerPair || candidate == Spell.AbyssDrop;
            if (_cracked && redSpell)
            {
                return false;
            }

            if (Spells[(int)candidate].IsZone && ZoneSpellActive())
            {
                return false;
            }

            // Scrawl is one line drawn through you; Cradle is a web around you. Back to back they blur into one attack.
            bool scrawlAfterCradle = candidate == Spell.Scrawl && _lastSpell == Spell.Cradle;
            bool cradleAfterScrawl = candidate == Spell.Cradle && _lastSpell == Spell.Scrawl;
            if (scrawlAfterCradle || cradleAfterScrawl)
            {
                return false;
            }

            if (candidate == Spell.AbyssDrop && OpenAirAbove(target) < AbyssDropMinAir)
            {
                return false;
            }

            return true;
        }

        private void RefillSpellBag()
        {
            _spellBag.Clear();

            if (_cracked)
            {
                _spellBag.AddRange(LowerBag);
            }
            else
            {
                _spellBag.AddRange(UpperBag);
            }

            for (int i = _spellBag.Count - 1; i > 0; i--)
            {
                int swapIndex = Main.rand.Next(i + 1);
                Spell swapped = _spellBag[i];
                _spellBag[i] = _spellBag[swapIndex];
                _spellBag[swapIndex] = swapped;
            }
        }

        // True while any acolyte's Mirror echo, Cradle thread or Clock is still running.
        private static bool ZoneSpellActive()
        {
            int echoType = ModContent.ProjectileType<MirrorEcho>();
            int threadType = ModContent.ProjectileType<MadnessThread>();
            int clockType = ModContent.ProjectileType<MadnessClock>();

            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile projectile = Main.projectile[i];
                if (!projectile.active)
                {
                    continue;
                }

                if (projectile.type == echoType || projectile.type == threadType || projectile.type == clockType)
                {
                    return true;
                }
            }

            return false;
        }

        private void BeginSpell(Spell spell)
        {
            _spell = spell;
            _lastSpell = spell;
            _aimLocked = false;
            _spellCooldowns[(int)spell] = Spells[(int)spell].Cooldown;

            Player target = Main.player[NPC.target];
            FaceTarget(target);
            _aim = (target.Center - NPC.Center).SafeNormalize(Vector2.UnitX * _facing);
            DebugAttackLabel = CurrentSpell.Name;
            SetDisplayWeapon(MagicWeaponItemType, false);
            EnterPhase(AttackPhase.MagicTelegraph, MagicTelegraphTicks);
            OnSpellTellStarted(target);
            NPC.netUpdate = true;
        }

        private void FaceTarget(Player target)
        {
            _facing = 1;
            if (target.Center.X < NPC.Center.X)
            {
                _facing = -1;
            }

            NPC.direction = _facing;
            NPC.spriteDirection = _facing;
        }

        // Server-only. Spells that put a telegraph in the world at the START of the tell (rather than at the staff) spawn it here.
        private void OnSpellTellStarted(Player target)
        {
            if (_spell == Spell.AbyssDrop)
            {
                // 320px over the player, clamped to stay 24px under the first ceiling (SpellAllowed already needs 160px of air).
                float ceilingY = target.position.Y - OpenAirAbove(target);
                float portalY = Math.Max(target.Center.Y - 320f, ceilingY + 24f + AbyssDropPortal.RingRadius);
                Vector2 portal = new Vector2(target.Center.X, portalY);

                Projectile.NewProjectile(NPC.GetSource_FromAI(), portal, Vector2.Zero, ModContent.ProjectileType<AbyssDropPortal>(),
                    0, 0f, Main.myPlayer, NPC.whoAmI, target.whoAmI);
            }

            if (_spell == Spell.Mark)
            {
                Projectile.NewProjectile(NPC.GetSource_FromAI(), target.Top, Vector2.Zero, ModContent.ProjectileType<MarkGlyph>(),
                    0, 0f, Main.myPlayer, NPC.whoAmI, target.whoAmI);
            }

            if (_spell == Spell.Spiral)
            {
                _spiralBaseAngle = Main.rand.NextFloat(MathHelper.TwoPi);
            }
        }

        // Server-only release; channels set _magicAttackTicksOverride-free fixed Active lengths and emit from DoMagicTick.
        protected override void DoMagicAttack()
        {
            _aimLocked = true;

            if (!Main.dedServ)
            {
                PlaySpellReleaseSound();
            }

            if (Main.netMode == NetmodeID.MultiplayerClient || !NPC.HasValidTarget)
            {
                return;
            }

            Player target = Main.player[NPC.target];
            IEntitySource source = NPC.GetSource_FromAI();

            if (_spell == Spell.SeekerPair)
            {
                // Both form at the tip, then launch +-70 degrees off the aim line (OolacileSeeker pair mode).
                Vector2 tip = SafeCastOrigin();
                Vector2 aimDirection = (target.Center - tip).SafeNormalize(Vector2.UnitX * _facing);
                for (int side = -1; side <= 1; side += 2)
                {
                    Projectile.NewProjectile(source, tip, aimDirection * 0.01f, ModContent.ProjectileType<OolacileSeeker>(),
                        SeekerDamage, 0f, Main.myPlayer, target.whoAmI, 1f, side);
                }
            }

            if (_spell == Spell.Gaze)
            {
                CastGaze(target, source);
            }

            if (_spell == Spell.Scrawl)
            {
                CastScrawl(target, source);
            }

            if (_spell == Spell.Cradle)
            {
                CastCradle(target, source);
            }

            if (_spell == Spell.Mirror)
            {
                Projectile.NewProjectile(source, target.Center, Vector2.Zero, ModContent.ProjectileType<MirrorEcho>(), MirrorDamage, 0f, Main.myPlayer,
                    target.whoAmI, MirrorFirstDelay);
                Projectile.NewProjectile(source, target.Center, Vector2.Zero, ModContent.ProjectileType<MirrorEcho>(), MirrorDamage, 0f, Main.myPlayer,
                    target.whoAmI, MirrorSecondDelay);
            }

            if (_spell == Spell.Clock)
            {
                Projectile.NewProjectile(source, target.Center, Vector2.Zero, ModContent.ProjectileType<MadnessClock>(), 0, 0f, Main.myPlayer);
            }

            NPC.netUpdate = true;
        }

        // Server-only: the anchor raycasts (tiles are identical everywhere) and one thread projectile per chord, spawned at
        // its first anchor. In open air, where more than half the ends hang capped, it strings only 4 threads (plan risk).
        private void CastCradle(Player target, IEntitySource source)
        {
            int threadCount = CradleThreads;
            if (_cracked)
            {
                threadCount = CradleThreadsCracked;
            }

            Vector2[] startAnchors = new Vector2[threadCount];
            Vector2[] endAnchors = new Vector2[threadCount];
            bool[] startCapped = new bool[threadCount];
            bool[] endCapped = new bool[threadCount];
            int cappedEnds = 0;
            float baseAngle = Main.rand.NextFloat(MathHelper.Pi);

            for (int thread = 0; thread < threadCount; thread++)
            {
                float angle = baseAngle + thread * MathHelper.Pi / threadCount + MathHelper.ToRadians(Main.rand.NextFloat(-10f, 10f));
                Vector2 direction = angle.ToRotationVector2();
                Vector2 through = target.Center + Main.rand.NextVector2Circular(CradleCenterSpread, CradleCenterSpread);
                Point throughTile = through.ToTileCoordinates();
                if (WorldGen.SolidTile(throughTile.X, throughTile.Y))
                {
                    through = target.Center;
                }

                endAnchors[thread] = CradleRaycast(through, direction, out endCapped[thread]);
                startAnchors[thread] = CradleRaycast(through, -direction, out startCapped[thread]);

                if (startCapped[thread])
                {
                    cappedEnds++;
                }

                if (endCapped[thread])
                {
                    cappedEnds++;
                }
            }

            int strung = threadCount;
            if (cappedEnds > threadCount)
            {
                strung = Math.Min(threadCount, CradleThreadsOpenAir);
            }

            int threadType = ModContent.ProjectileType<MadnessThread>();
            int order = 0;
            for (int thread = 0; thread < strung; thread++)
            {
                Vector2 span = endAnchors[thread] - startAnchors[thread];
                if (span.Length() < CradleMinSpan)
                {
                    continue;
                }

                float flags = MadnessThread.PackFlags(startCapped[thread], endCapped[thread], NPC.whoAmI);
                Projectile.NewProjectile(source, startAnchors[thread], span.SafeNormalize(Vector2.UnitX) * 0.01f, threadType, CradleDamage, 0f, Main.myPlayer,
                    span.Length(), order * CradleSpacingTicks, flags);
                order++;
            }
        }

        // The last open point before the first solid tile along `direction`, in 8px steps up to 30 tiles; capped = no tile.
        private static Vector2 CradleRaycast(Vector2 from, Vector2 direction, out bool capped)
        {
            for (float distance = 8f; distance <= CradleMaxReach; distance += 8f)
            {
                Vector2 point = from + direction * distance;
                Point tile = point.ToTileCoordinates();
                if (WorldGen.SolidTile(tile.X, tile.Y))
                {
                    capped = false;
                    return from + direction * (distance - 8f);
                }
            }

            capped = true;
            return from + direction * CradleMaxReach;
        }

        // Server-only. Eyes across the player's upper half-circle; a spot inside tiles steps inward (to 160px) or is skipped.
        private void CastGaze(Player target, IEntitySource source)
        {
            int eyeCount = GazeEyes;
            int spacing = GazeSpacingTicks;
            if (_cracked)
            {
                eyeCount = GazeEyesCracked;
                spacing = GazeSpacingCrackedTicks;
            }

            int openOrder = 0;
            for (int eye = 0; eye < eyeCount; eye++)
            {
                float spread = eye / (float)(eyeCount - 1);
                float angleDegrees = MathHelper.Lerp(-155f, -25f, spread) + Main.rand.NextFloat(-10f, 10f);
                Vector2 outward = MathHelper.ToRadians(angleDegrees).ToRotationVector2();
                float radius = Main.rand.NextFloat(GazeMinRadius, GazeMaxRadius);

                bool placed = false;
                Vector2 spot = Vector2.Zero;
                for (float tryRadius = radius; tryRadius >= 160f; tryRadius -= 20f)
                {
                    spot = target.Center + outward * tryRadius;
                    if (!Collision.SolidCollision(spot - new Vector2(16f), 32, 32))
                    {
                        placed = true;
                        break;
                    }
                }

                if (!placed)
                {
                    continue;
                }

                Projectile.NewProjectile(source, spot, Vector2.Zero, ModContent.ProjectileType<MadnessEye>(), GazeDamage, 0f, Main.myPlayer,
                    target.whoAmI, openOrder * spacing);
                openOrder++;
            }
        }

        // Server-only. One scrawl from the staff tip to 160px past the player; cracked, a second one crossing it in an X over
        // the player, started from a point swung 70 degrees around the player (whichever side has open air).
        private void CastScrawl(Player target, IEntitySource source)
        {
            Vector2 tip = SafeCastOrigin();
            SpawnScrawl(source, tip, target.Center, 0);

            if (!_cracked)
            {
                return;
            }

            Vector2 fromPlayer = tip - target.Center;
            for (int side = 1; side >= -1; side -= 2)
            {
                Vector2 secondStart = target.Center + fromPlayer.RotatedBy(MathHelper.ToRadians(70f) * side);
                Point startTile = secondStart.ToTileCoordinates();
                bool openStart = !WorldGen.SolidTile(startTile.X, startTile.Y);
                if (openStart)
                {
                    SpawnScrawl(source, secondStart, target.Center, ScrawlSecondDelay);
                    return;
                }
            }
        }

        private void SpawnScrawl(IEntitySource source, Vector2 start, Vector2 through, int delay)
        {
            Vector2 toPlayer = through - start;
            float length = Math.Min(ScrawlMaxLength, toPlayer.Length() + ScrawlOvershoot);
            float packed = MadnessScrawl.PackLengthAndAngle(length, toPlayer.ToRotation());
            Projectile.NewProjectile(source, start, Vector2.Zero, ModContent.ProjectileType<MadnessScrawl>(), ScrawlDamage, 0f, Main.myPlayer,
                Main.rand.Next(1, int.MaxValue), packed, delay);
        }

        // Channels: ticksRemaining counts down from the spell's Active length, so elapsed 0 is the first active tick.
        protected override void DoMagicTick(int ticksRemaining)
        {
            int elapsed = CurrentSpell.Active - ticksRemaining;

            if (_spell == Spell.BoltVolley && elapsed % BoltSpacingTicks == 0 && elapsed / BoltSpacingTicks < BoltCount)
            {
                FireBolt();
            }

            if (_spell == Spell.FrenziedVolley && elapsed % VolleySpacingTicks == 0 && elapsed / VolleySpacingTicks < VolleyCount)
            {
                FireVolleyMote();
            }

            if (_spell == Spell.Spiral)
            {
                // The commit cue: from here the channel is hyper-armored (ApplyPoiseWindows).
                if (ticksRemaining == SpiralCommitTicks && !Main.dedServ)
                {
                    SoundEngine.PlaySound(SoundID.Item29 with { Volume = 0.8f, Pitch = 0.5f }, NPC.Center);
                }

                if (ticksRemaining == SpiralCommitTicks && Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Projectile.NewProjectile(NPC.GetSource_FromAI(), SpiralOrbPosition, Vector2.Zero, ModContent.ProjectileType<Content.Projectiles.VFX.TelegraphFlash>(),
                        0, 0f, Main.myPlayer, UsefulFunctions.ColorToFloat(MadnessYellow));
                }

                if (elapsed % SpiralEmitTicks == 0)
                {
                    EmitSpiral(elapsed / SpiralEmitTicks);
                }
            }
        }

        // Server-only. Above 50%: two counter-rotating arms. Cracked: three arms 120 degrees apart that reverse their spin
        // once at the channel's midpoint (emission 7 of 15).
        private void EmitSpiral(int emission)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            IEntitySource source = NPC.GetSource_FromAI();
            int moteType = ModContent.ProjectileType<MadnessMote>();
            float turn = MathHelper.ToRadians(SpiralTurnDegrees);
            Vector2 orb = SpiralOrbPosition;
            float spiralMode = (float)MadnessMote.Mode.Spiral;

            if (!_cracked)
            {
                float clockwise = _spiralBaseAngle + emission * turn;
                float counter = _spiralBaseAngle + MathHelper.Pi - emission * turn;
                Projectile.NewProjectile(source, orb, clockwise.ToRotationVector2() * SpiralMoteSpeed, moteType, SpiralDamage, 0f, Main.myPlayer, spiralMode);
                Projectile.NewProjectile(source, orb, counter.ToRotationVector2() * SpiralMoteSpeed, moteType, SpiralDamage, 0f, Main.myPlayer, spiralMode);
                return;
            }

            int midpoint = CurrentSpell.Active / SpiralEmitTicks / 2;
            int spinSteps = emission;
            if (emission > midpoint)
            {
                spinSteps = midpoint - (emission - midpoint);
            }

            for (int arm = 0; arm < 3; arm++)
            {
                float angle = _spiralBaseAngle + arm * MathHelper.TwoPi / 3f + spinSteps * turn;
                Projectile.NewProjectile(source, orb, angle.ToRotationVector2() * SpiralMoteSpeed, moteType, SpiralDamage, 0f, Main.myPlayer, spiralMode);
            }
        }

        // Server-only. Each mote re-aims at the player's position 8 ticks ahead; the staff follows the shot.
        private void FireVolleyMote()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient || !NPC.HasValidTarget)
            {
                return;
            }

            Player target = Main.player[NPC.target];
            Vector2 tip = SafeCastOrigin();
            Vector2 aimPoint = target.Center + target.velocity * BoltLeadTicks;
            Vector2 direction = (aimPoint - tip).SafeNormalize(Vector2.UnitX * _facing);
            _aim = direction;

            Projectile.NewProjectile(NPC.GetSource_FromAI(), tip, direction * VolleySpeed, ModContent.ProjectileType<MadnessMote>(),
                VolleyDamage, 0f, Main.myPlayer, (float)MadnessMote.Mode.Volley);
            NPC.netUpdate = true;
        }

        // Server-only. Each bolt re-aims at the player's position 8 ticks ahead, and the staff follows the shot.
        private void FireBolt()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient || !NPC.HasValidTarget)
            {
                return;
            }

            Player target = Main.player[NPC.target];
            Vector2 tip = SafeCastOrigin();
            Vector2 aimPoint = target.Center + target.velocity * BoltLeadTicks;
            Vector2 direction = (aimPoint - tip).SafeNormalize(Vector2.UnitX * _facing);
            _aim = direction;

            Projectile.NewProjectile(NPC.GetSource_FromAI(), tip, direction * BoltSpeed, ModContent.ProjectileType<OolacileBolt>(),
                BoltDamage, 0f, Main.myPlayer, 0f, 0f, BoltTopSpeed);
            NPC.netUpdate = true;
        }

        private void PlaySpellReleaseSound()
        {
            if (SpellIsMadness(_spell))
            {
                SoundEngine.PlaySound(SoundID.Item103 with { Volume = 0.7f, Pitch = Main.rand.NextFloat(0.4f, 0.8f) }, NPC.Center);
                return;
            }

            SoundEngine.PlaySound(SoundID.Item8 with { Volume = 0.8f, Pitch = Main.rand.NextFloat(-0.2f, 0.3f) }, NPC.Center);
        }

        // Client-side, once per tell: the order's red spells keep Item8, the Madness spells the one Item103 "frenzied" cue.
        private void PlayTellCueOnce()
        {
            if (_tellCuePlayed)
            {
                return;
            }

            _tellCuePlayed = true;
            if (SpellIsMadness(_spell))
            {
                float pitch = 0.4f;
                if (_spell == Spell.Cradle)
                {
                    pitch = 0.5f;
                }
                else if (_spell == Spell.Mirror)
                {
                    pitch = 0.6f;
                }

                SoundEngine.PlaySound(SoundID.Item103 with { Volume = 0.5f, Pitch = pitch }, NPC.Center);
                return;
            }

            SoundEngine.PlaySound(SoundID.Item8 with { Volume = 0.6f, Pitch = 0.3f }, NPC.Center);
        }

        private static bool SpellIsMadness(Spell spell)
        {
            bool red = spell == Spell.BoltVolley || spell == Spell.SeekerPair || spell == Spell.AbyssDrop;
            return !red;
        }

        protected override void DoMeleeAttack()
        {
        }

        protected override void DoRangedAttack()
        {
        }

        #region Set-pieces (Frenzied Step, Shriek, the Crack)
        private int CustomElapsed()
        {
            if (Phase != AttackPhase.Custom)
            {
                return -1;
            }

            return CustomDuration() - PhaseTimer;
        }

        private int CustomDuration()
        {
            if (_customKind == CustomKind.Step)
            {
                return StepLastSwapTick() + StepArrivalFlashTicks + StepRecoveryTicks;
            }

            if (_customKind == CustomKind.Shriek)
            {
                return ShriekTellTicks + ShriekRingTicks + ShriekRecoveryTicks;
            }

            if (_customKind == CustomKind.Dissolve)
            {
                return DissolveTicks;
            }

            return CrackTicks;
        }

        // Swap tick of the final hop: hop i swaps at 10 + 14i.
        private int StepLastSwapTick()
        {
            int hops = Math.Max(1, _stepHopCount);
            return StepDepartureTicks + StepHopSpacingTicks * (hops - 1);
        }

        protected override bool SlowDownDuringCustom => true;

        protected override float? CustomWeaponRotation => StaffRotation;

        // Server-only. Picks every hop's landing spot now (each must be a fresh spot >= 8 tiles from the last), then starts it.
        private bool TryBeginStep(Player target)
        {
            int wantedHops = 1;
            if (_cracked)
            {
                wantedHops = Main.rand.Next(2, MaxStepHops + 1);
            }

            Vector2 previousSpot = NPC.Center;
            _stepHopCount = 0;

            for (int hop = 0; hop < wantedHops; hop++)
            {
                if (!TryFindStepDestination(target, previousSpot, out Vector2 destination))
                {
                    break;
                }

                _stepHops[hop] = destination;
                _stepHopCount++;
                previousSpot = destination;
            }

            if (_stepHopCount == 0)
            {
                return false;
            }

            _stepOrigin = NPC.Center;
            _spellsSinceStep = 0;
            _closeTicks = 0;
            _noSightTicks = 0;
            _stepCooldown = StepCooldownTicks;
            if (_cracked)
            {
                _stepCooldown = StepCooldownCrackedTicks;
            }

            _customKind = CustomKind.Step;
            DebugAttackLabel = "Frenzied Step";
            if (_stepHopCount > 1)
            {
                DebugAttackLabel = "Frenzied Step x" + _stepHopCount;
            }

            FaceTarget(target);
            StartCustomAttack(CustomDuration(), MagicWeaponItemType);
            NPC.netUpdate = true;
            return true;
        }

        // Up to 100 random tries: 12-20 tiles to either side of the player, standing on a floor within 8 tiles of the player's
        // feet level, with line of sight to them and at least 8 tiles from the spot it leaves.
        private bool TryFindStepDestination(Player target, Vector2 previousSpot, out Vector2 destination)
        {
            int targetTileX = (int)(target.Center.X / 16f);
            int targetFloorY = (int)(target.Bottom.Y / 16f);

            for (int attempt = 0; attempt < StepSearchTries; attempt++)
            {
                int side = 1;
                if (Main.rand.NextBool())
                {
                    side = -1;
                }

                int tileX = targetTileX + side * Main.rand.Next(StepMinTiles, StepMaxTiles + 1);
                int startY = targetFloorY - 8;

                // Walk down the column to the first floor tile under open air.
                for (int tileY = startY; tileY <= targetFloorY + 8; tileY++)
                {
                    Vector2 candidate = new Vector2(tileX * 16 + 8, tileY * 16 - NPC.height / 2f);
                    if (!StepSpotSafe(candidate))
                    {
                        continue;
                    }

                    bool farFromPrevious = Vector2.Distance(candidate, previousSpot) >= StepMinMovePx;
                    bool lineOfSight = Collision.CanHitLine(candidate, 1, 1, target.Center, 1, 1);
                    if (farFromPrevious && lineOfSight)
                    {
                        destination = candidate;
                        return true;
                    }

                    break;
                }
            }

            destination = Vector2.Zero;
            return false;
        }

        // Body fits, solid floor directly underneath, no lava at the feet, inside the world, and 12+ tiles from every player.
        private bool StepSpotSafe(Vector2 center)
        {
            Vector2 topLeft = center - NPC.Size * 0.5f;
            bool outsideWorld = topLeft.X < 640 || topLeft.Y < 640 || center.X > Main.maxTilesX * 16 - 640 || center.Y > Main.maxTilesY * 16 - 640;
            if (outsideWorld || Collision.SolidCollision(topLeft, NPC.width, NPC.height))
            {
                return false;
            }

            int tileX = (int)(center.X / 16f);
            int floorY = (int)((center.Y + NPC.height / 2f + 1f) / 16f);
            Tile floor = Framing.GetTileSafely(tileX, floorY);
            bool solidFloor = floor.HasUnactuatedTile && (Main.tileSolid[floor.TileType] || Main.tileSolidTop[floor.TileType]);
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
                if (player.active && !player.dead && Vector2.Distance(center, player.Center) < StepMinTiles * 16f)
                {
                    return false;
                }
            }

            return true;
        }

        protected override void DoCustomTick(int ticksRemaining)
        {
            int elapsed = CustomDuration() - ticksRemaining;

            if (_customKind == CustomKind.Step)
            {
                TickStep(elapsed);
            }
            else if (_customKind == CustomKind.Shriek)
            {
                TickShriek(elapsed);
            }
            else if (_customKind == CustomKind.Crack)
            {
                TickCrack(elapsed);
            }
            else if (_customKind == CustomKind.Dissolve)
            {
                TickDissolve(elapsed, ticksRemaining);
            }
        }

        // The killing blow is held at 1 HP for the 20t dissolve, then StrikeInstantKill takes that last HP itself, so OnKill,
        // the loot and the Defeated broadcast all run (never set life = 0 by hand: vanilla would delete the NPC silently).
        public override bool CheckDead()
        {
            if (_dissolveComplete)
            {
                return true;
            }

            NPC.life = 1;
            NPC.dontTakeDamage = true;

            if (_dissolving)
            {
                return false;
            }

            _dissolving = true;
            _customKind = CustomKind.Dissolve;
            DebugAttackLabel = "Dissolving";
            StartCustomAttack(DissolveTicks);
            NPC.netUpdate = true;
            return false;
        }

        // Yellow and black dust eats the body from the head down while the staff falls as a streak.
        private void TickDissolve(int elapsed, int ticksRemaining)
        {
            if (!Main.dedServ)
            {
                float sweep = elapsed / (float)DissolveTicks;
                float rowY = NPC.position.Y + NPC.height * sweep;
                for (int i = 0; i < 4; i++)
                {
                    int dustType = DustID.YellowTorch;
                    Color tint = default;
                    if (i % 2 == 0)
                    {
                        dustType = DustID.Wraith;
                        tint = Color.Black;
                    }

                    Vector2 spot = new Vector2(NPC.position.X + Main.rand.NextFloat(NPC.width), rowY + Main.rand.NextFloat(-6f, 6f));
                    Dust ash = Dust.NewDustPerfect(spot, dustType, new Vector2(Main.rand.NextFloat(-1f, 1f), -Main.rand.NextFloat(0.5f, 2f)), 60, tint, 1.4f);
                    ash.noGravity = true;
                }

                if (elapsed == 0)
                {
                    SoundEngine.PlaySound(SoundID.Item103 with { Volume = 0.7f, Pitch = -0.2f }, NPC.Center);
                }
            }

            bool lastTick = ticksRemaining == 1;
            if (lastTick && Main.netMode != NetmodeID.MultiplayerClient)
            {
                _dissolveComplete = true;
                NPC.dontTakeDamage = false;
                NPC.StrikeInstantKill();
            }
        }

        // Every peer runs the swaps from the synced hop list, so the body lands at the same spot everywhere on the same tick.
        private void TickStep(int elapsed)
        {
            for (int hop = 0; hop < _stepHopCount; hop++)
            {
                int swapTick = StepDepartureTicks + StepHopSpacingTicks * hop;
                int departureStart = swapTick - StepDepartureTicks;
                bool departing = elapsed >= departureStart && elapsed < swapTick;

                if (departing && !Main.dedServ)
                {
                    DepartureFlicker(elapsed == departureStart);
                }

                if (elapsed != swapTick)
                {
                    continue;
                }

                Vector2 leaving = NPC.Center;
                Vector2 arriving = _stepHops[hop];

                // Below 50% every departure spot leaves a Hallucination (capped at 3 alive), spawned by the server.
                if (_cracked && Main.netMode != NetmodeID.MultiplayerClient)
                {
                    SpawnHallucination(leaving);
                }

                NPC.Center = arriving;
                NPC.velocity = Vector2.Zero;
                NPC.oldPosition = NPC.position;

                if (NPC.HasValidTarget)
                {
                    FaceTarget(Main.player[NPC.target]);
                }

                if (!Main.dedServ)
                {
                    SwapBurst(leaving);
                    SwapBurst(arriving);
                    SoundEngine.PlaySound(SoundID.Item8 with { Volume = 0.8f, Pitch = 0.3f }, arriving);
                }

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    NPC.netUpdate = true;
                }
            }
        }

        private void DepartureFlicker(bool firstTick)
        {
            if (firstTick)
            {
                SoundEngine.PlaySound(SoundID.Item79 with { Volume = 0.6f, Pitch = 0.4f }, NPC.Center);
            }

            for (int i = 0; i < 3; i++)
            {
                Vector2 spawn = NPC.position + new Vector2(Main.rand.NextFloat(NPC.width), Main.rand.NextFloat(NPC.height));
                Dust spark = Dust.NewDustPerfect(spawn, DustID.WhiteTorch, Main.rand.NextVector2Circular(2.5f, 2.5f), 0, default, 1.2f);
                spark.noGravity = true;
            }

            // The body flickers yellow-white: the puppet is lit by its own pulse, so the whole robe flashes.
            float flicker = 0.4f;
            if (Main.GameUpdateCount % 4 < 2)
            {
                flicker = 1.1f;
            }

            Lighting.AddLight(NPC.Center, MadnessYellow.ToVector3() * flicker);
        }

        private static void SwapBurst(Vector2 point)
        {
            for (int i = 0; i < 20; i++)
            {
                int dustType = DustID.YellowTorch;
                if (i % 2 == 0)
                {
                    dustType = DustID.WhiteTorch;
                }

                Dust burst = Dust.NewDustPerfect(point, dustType, Main.rand.NextVector2Circular(5f, 5f), 0, default, 1.4f);
                burst.noGravity = true;
            }
        }

        // Server-only. A translucent afterimage at a blink departure spot (at most 3 alive at once, plan risk #3).
        private void SpawnHallucination(Vector2 position)
        {
            int hallucinationType = ModContent.ProjectileType<AcolyteHallucination>();
            int alive = 0;
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                if (Main.projectile[i].active && Main.projectile[i].type == hallucinationType)
                {
                    alive++;
                }
            }

            if (alive >= MaxHallucinations || !NPC.HasValidTarget)
            {
                return;
            }

            Player target = Main.player[NPC.target];
            int facing = 1;
            if (target.Center.X < position.X)
            {
                facing = -1;
            }

            Projectile.NewProjectile(NPC.GetSource_FromAI(), position, Vector2.Zero, hallucinationType, 0, 0f, Main.myPlayer, target.whoAmI, facing);
        }

        private void TickShriek(int elapsed)
        {
            if (elapsed < ShriekTellTicks)
            {
                if (!Main.dedServ)
                {
                    // The hood flares white: motes flicker at the head and the body lights up.
                    Vector2 head = NPC.Top + new Vector2(0f, 8f);
                    Dust mote = Dust.NewDustPerfect(head + Main.rand.NextVector2Circular(12f, 8f), DustID.WhiteTorch, new Vector2(0f, -1f), 0, default, 1.3f);
                    mote.noGravity = true;
                    Lighting.AddLight(head, Vector3.One * (0.4f + elapsed / (float)ShriekTellTicks));

                    if (elapsed == 0)
                    {
                        SoundEngine.PlaySound(SoundID.Item103 with { Volume = 0.6f, Pitch = 0.6f }, NPC.Center);
                    }
                }

                return;
            }

            if (elapsed == ShriekTellTicks)
            {
                ReleaseMadnessRing();
            }
        }

        // Shared by Shriek and the Crack: the jagged ring (true annulus, 160px over 14t) centred on the body.
        private void ReleaseMadnessRing()
        {
            if (!Main.dedServ)
            {
                SoundEngine.PlaySound(SoundID.Item103 with { Volume = 0.9f, Pitch = 0.8f }, NPC.Center);
                SoundEngine.PlaySound(SoundID.NPCDeath6 with { Volume = 0.5f, Pitch = 0.6f }, NPC.Center);
            }

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                Projectile.NewProjectile(NPC.GetSource_FromAI(), NPC.Center, Vector2.Zero, ModContent.ProjectileType<AcolyteShriekRing>(),
                    ShriekDamage, 0f, Main.myPlayer);
            }
        }

        private void TickCrack(int elapsed)
        {
            if (!Main.dedServ)
            {
                // The robe is overwritten by flickering yellow-white: the puppet is lit by its own stuttering pulse.
                float flicker = 0.3f;
                if (Main.GameUpdateCount % 6 < 3)
                {
                    flicker = 1.2f;
                }

                Lighting.AddLight(NPC.Center, MadnessYellow.ToVector3() * flicker);

                for (int i = 0; i < 2; i++)
                {
                    int dustType = DustID.YellowTorch;
                    if (i == 0)
                    {
                        dustType = DustID.WhiteTorch;
                    }

                    Vector2 spawn = NPC.position + new Vector2(Main.rand.NextFloat(NPC.width), Main.rand.NextFloat(NPC.height));
                    Dust crackle = Dust.NewDustPerfect(spawn, dustType, Main.rand.NextVector2Circular(3f, 3f), 0, default, 1.3f);
                    crackle.noGravity = true;
                }

                if (elapsed == 0)
                {
                    SoundEngine.PlaySound(SoundID.Item103 with { Volume = 0.8f, Pitch = 0.2f }, NPC.Center);
                    UsefulFunctions.ScreenShake(NPC.Center, 3f, 20, distanceFalloff: 700f);
                }
            }

            if (elapsed == CrackRingTick)
            {
                ReleaseMadnessRing();
            }
        }

        // Server-only. A finished blink opens straight into a spell (its own full tell is still there); everything else breathes.
        private void FinishCustom()
        {
            if (_customKind == CustomKind.Step)
            {
                _neutralDelay = 0;
            }
            else
            {
                _neutralDelay = CastPaceTicks;
                if (_cracked)
                {
                    _neutralDelay = CastPaceCrackedTicks;
                }
            }

            _customKind = CustomKind.None;
            NPC.netUpdate = true;
        }
        #endregion

        #region Staff pose
        // Spells conjured away from the tip (and the set-pieces) raise the staff overhead; tip-fired spells point it along _aim.
        private bool StaffRaisedSpell => _spell == Spell.SeekerPair || _spell == Spell.AbyssDrop || _spell == Spell.Spiral
            || _spell == Spell.Clock || _spell == Spell.Cradle;

        private float CastRotation
        {
            get
            {
                if (StaffRaisedSpell)
                {
                    return StaffRaisedRotation;
                }

                float aimAngle = _aim.ToRotation();
                if (_facing != 1)
                {
                    aimAngle = MathHelper.Pi - aimAngle;
                }

                return MathHelper.WrapAngle(aimAngle);
            }
        }

        // Pure function of synced state, so the server (which never builds the drawn puppet) and clients agree on the tip.
        private float StaffRotation
        {
            get
            {
                if (Phase == AttackPhase.Custom)
                {
                    return CustomStaffRotation();
                }

                if (Phase == AttackPhase.MagicTelegraph)
                {
                    // Cat's Cradle sweeps the staff in a circle overhead; Mad Scrawl draws it back across the body.
                    float tellProgress = 1f - PhaseTimer / (float)Math.Max(1, MagicTelegraphTicks);
                    if (_spell == Spell.Cradle)
                    {
                        return StaffRaisedRotation + MathF.Sin(tellProgress * MathHelper.TwoPi * 2f) * 0.7f;
                    }

                    if (_spell == Spell.Scrawl)
                    {
                        return MathHelper.Lerp(StaffCarryRotation, -2.3f, MathHelper.SmoothStep(0f, 1f, Math.Min(1f, tellProgress * 1.5f)));
                    }

                    // Raise/aim over all but the last 12t of the tell, then hold.
                    float raiseTicks = Math.Max(1, MagicTelegraphTicks - 12);
                    float raiseProgress = MathHelper.Clamp((MagicTelegraphTicks - PhaseTimer) / raiseTicks, 0f, 1f);
                    return MathHelper.Lerp(StaffCarryRotation, CastRotation, MathHelper.SmoothStep(0f, 1f, raiseProgress));
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
                    return MathHelper.Lerp(CastRotation, StaffCarryRotation, MathHelper.SmoothStep(0f, 1f, lowerProgress));
                }

                return StaffCarryRotation;
            }
        }

        // Shriek and the Crack throw the staff up overhead; the blink lifts it as it departs.
        private float CustomStaffRotation()
        {
            int elapsed = CustomElapsed();

            if (_customKind == CustomKind.Step)
            {
                float lift = MathHelper.Clamp(elapsed / (float)StepDepartureTicks, 0f, 1f);
                return MathHelper.Lerp(StaffCarryRotation, StaffRaisedRotation, lift);
            }

            float throwProgress = MathHelper.Clamp(elapsed / 8f, 0f, 1f);
            return MathHelper.Lerp(StaffCarryRotation, StaffRaisedRotation, MathHelper.SmoothStep(0f, 1f, throwProgress));
        }

        protected override void ModifyAdditionalPhaseWeaponRotation(ref float weaponRotation)
        {
            weaponRotation = StaffRotation;
        }

        private Vector2 StaffTipWorld
        {
            get
            {
                // Mirrors SyncPuppet + vanilla GetFrontHandPosition with a throwaway Player, so a dedicated server gets the
                // same tip as the clients (the Archdeacon's method).
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

        // The staff tip, unless it is inside a wall or blocked from the body; then the body centre.
        private Vector2 SafeCastOrigin()
        {
            Vector2 tip = StaffTipWorld;
            Point tipTile = tip.ToTileCoordinates();
            bool tipInWall = WorldGen.SolidTile(tipTile.X, tipTile.Y);
            bool tipUsable = !tipInWall && Collision.CanHitLine(NPC.Center, 1, 1, tip, 1, 1);

            if (tipUsable)
            {
                return tip;
            }

            return NPC.Center;
        }
        #endregion

        // Open air straight above the player's head, in px, up to 25 tiles (400px).
        private static float OpenAirAbove(Player player)
        {
            const int ScanTiles = 25;
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

        public override void OnStagger(NPC npc)
        {
            // A poise break inside the Spiral's interruptible window pops its orb in a burst of motes.
            bool spiralCasting = _spell == Spell.Spiral && (Phase == AttackPhase.MagicTelegraph || Phase == AttackPhase.MagicAttack);
            if (spiralCasting && !Main.dedServ)
            {
                AcolyteMadness.SparkBurst(SpiralOrbPosition, 30, 4f);
                SoundEngine.PlaySound(SoundID.Item103 with { Volume = 0.6f, Pitch = 0.9f }, SpiralOrbPosition);
            }

            base.OnStagger(npc);
            _customKind = CustomKind.None;
            _aimLocked = false;
            _neutralDelay = 60;
            NPC.netUpdate = true;
        }

        private void Decorate()
        {
            Lighting.AddLight(NPC.Center, AbyssRed.ToVector3() * 0.3f);

            // Before the Crack: dark-red motes; after: yellow and white motes jitter off the hood, 1 every 3 ticks.
            if (_cracked)
            {
                if (Main.GameUpdateCount % 3 == 0)
                {
                    int dustType = DustID.YellowTorch;
                    if (Main.rand.NextBool())
                    {
                        dustType = DustID.WhiteTorch;
                    }

                    Vector2 hood = NPC.Top + new Vector2(Main.rand.NextFloat(-8f, 8f), 6f);
                    Dust mote = Dust.NewDustPerfect(hood, dustType, Main.rand.NextVector2Circular(1.6f, 1.6f), 0, default, 1f);
                    mote.noGravity = true;
                }

                Lighting.AddLight(NPC.Top, MadnessYellow.ToVector3() * 0.35f);
            }
            else if (Main.GameUpdateCount % 4 == 0)
            {
                Dust mote = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, DustID.CrimsonSpray, 0f, -0.5f, 80, default, 0.9f);
                mote.noGravity = true;
            }
        }

        protected override void DoMagicTelegraphVFX(float progress)
        {
            if (Main.dedServ)
            {
                return;
            }

            Vector2 tip = StaffTipWorld;
            Color tellColor = MagicTelegraphFlashColor;

            if (_spell == Spell.SeekerPair)
            {
                // Two black-red wisps orbit the tip and tighten.
                float orbitRadius = MathHelper.Lerp(30f, 6f, progress);
                for (int wisp = 0; wisp < 2; wisp++)
                {
                    float angle = progress * MathHelper.TwoPi * 3f + wisp * MathHelper.Pi;
                    Vector2 orbit = angle.ToRotationVector2() * orbitRadius;
                    int wispDust = DustID.Wraith;
                    if (wisp == 1)
                    {
                        wispDust = DustID.CrimsonSpray;
                    }

                    Dust wispMote = Dust.NewDustPerfect(tip + orbit, wispDust, Vector2.Zero, 90, Color.Black, 1.3f);
                    wispMote.noGravity = true;
                }
            }
            float gatherRadius = MathHelper.Lerp(36f, 6f, progress);
            Vector2 offset = Main.rand.NextVector2CircularEdge(gatherRadius, gatherRadius);
            Vector2 inward = -offset.SafeNormalize(Vector2.UnitY) * MathHelper.Lerp(1.2f, 3f, progress);

            if (_spell == Spell.FrenziedVolley && Main.rand.NextBool(2))
            {
                // The tip spits white sparks along the aim.
                Vector2 spit = _aim.RotatedByRandom(0.5f) * Main.rand.NextFloat(2f, 4.5f);
                Dust spark = Dust.NewDustPerfect(tip, DustID.WhiteTorch, spit, 0, default, 1.1f);
                spark.noGravity = true;
            }

            if (_spell == Spell.Gaze)
            {
                // The gem pulses yellow.
                float pulse = 0.5f + 0.5f * MathF.Sin(progress * MathHelper.TwoPi * 3f);
                Lighting.AddLight(tip, MadnessYellow.ToVector3() * (0.4f + 0.6f * pulse));
            }

            if (_spell == Spell.Spiral)
            {
                // Motes converge on the forming orb above the head instead of the tip.
                tip = SpiralOrbPosition;
            }

            if (_spell == Spell.Mirror && NPC.HasValidTarget)
            {
                // "Your madness is leaving you": motes peel off THE PLAYER and drift back along their path.
                Player target = Main.player[NPC.target];
                Vector2 backward = (-target.velocity).SafeNormalize(Main.rand.NextVector2Unit()) * Main.rand.NextFloat(1f, 2.5f);
                for (int i = 0; i < 2; i++)
                {
                    Vector2 peel = target.position + new Vector2(Main.rand.NextFloat(target.width), Main.rand.NextFloat(target.height));
                    Dust peeled = Dust.NewDustPerfect(peel, DustID.YellowTorch, backward, 0, default, 1.1f);
                    peeled.noGravity = true;
                }
            }

            if (_spell == Spell.Cradle)
            {
                // Yellow threads coil around the tip.
                float coilAngle = progress * MathHelper.TwoPi * 4f;
                for (int strand = 0; strand < 2; strand++)
                {
                    Vector2 coil = (coilAngle + strand * MathHelper.Pi).ToRotationVector2() * 12f;
                    Dust thread = Dust.NewDustPerfect(tip + coil, DustID.YellowTorch, Vector2.Zero, 0, default, 0.9f);
                    thread.noGravity = true;
                }
            }

            if (_spell == Spell.Clock)
            {
                // A soft clock tick that speeds up toward the release (gaps 12, 8, 5, 3 ticks).
                bool tick = PhaseTimer == 30 || PhaseTimer == 18 || PhaseTimer == 10 || PhaseTimer == 5 || PhaseTimer == 2;
                if (tick)
                {
                    SoundEngine.PlaySound(SoundID.MenuTick with { Volume = 0.5f }, NPC.Center);
                }
            }

            PlayTellCueOnce();

            int dustType = DustID.CrimsonSpray;
            if (SpellIsMadness(_spell))
            {
                dustType = DustID.YellowTorch;
            }

            Dust mote = Dust.NewDustPerfect(tip + offset, dustType, inward, 60, default, MathHelper.Lerp(0.9f, 1.5f, progress));
            mote.noGravity = true;
            Lighting.AddLight(tip, tellColor.ToVector3() * (0.25f + 0.4f * progress));
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (InCustom(CustomKind.Dissolve))
            {
                int dissolveElapsed = CustomElapsed();
                DrawFallingStaff(dissolveElapsed);
                if (dissolveElapsed >= DissolveTicks - 4)
                {
                    return false;
                }
            }

            // Frenzied Step arrival: a white crack-flash (MadnessGlyph spiral) on the landing spot for 8 ticks after the last swap.
            int customElapsed = CustomElapsed();
            int sinceArrival = customElapsed - StepLastSwapTick();
            bool arriving = InCustom(CustomKind.Step) && sinceArrival >= 0 && sinceArrival < StepArrivalFlashTicks;
            if (arriving)
            {
                float flash = 1f - sinceArrival / (float)StepArrivalFlashTicks;
                AcolyteMadnessDraw.Quad("MadnessGlyph", NPC.Center, new Vector2(72f), sinceArrival * 0.3f, AcolyteMadnessDraw.Madness,
                    flash, 1f, Vector4.Zero, AcolyteMadnessDraw.Flicker(NPC.whoAmI) + 0.2f);
            }

            // Babbling Spiral's emitter: a Madness mote at 2x scale growing over the tell, then pulsing through the channel.
            bool spiralTell = Phase == AttackPhase.MagicTelegraph && _spell == Spell.Spiral;
            bool spiralChannel = Phase == AttackPhase.MagicAttack && _spell == Spell.Spiral;
            if (spiralTell || spiralChannel)
            {
                float orbGrowth = 1f;
                if (spiralTell)
                {
                    orbGrowth = 1f - PhaseTimer / (float)Math.Max(1, MagicTelegraphTicks);
                }

                float orbSize = 72f * orbGrowth * (0.92f + 0.08f * MathF.Sin(Main.GameUpdateCount * 0.4f));
                AcolyteMadnessDraw.Quad("MadnessMote", SpiralOrbPosition, new Vector2(orbSize), 0f, AcolyteMadnessDraw.Madness, 1f, 0f,
                    AcolyteMadnessDraw.FlareArms(NPC.whoAmI), AcolyteMadnessDraw.Flicker(NPC.whoAmI));
            }

            return base.PreDraw(spriteBatch, screenPos, drawColor);
        }

        // The staff drops out of the dissolving hand and falls (accelerating), leaving a short yellow streak.
        private void DrawFallingStaff(int elapsed)
        {
            Texture2D staff = Terraria.GameContent.TextureAssets.Item[MagicWeaponItemType].Value;
            float fall = 0.35f * elapsed * elapsed;
            Vector2 start = NPC.Center + new Vector2(_facing * 10f, -6f);
            float spin = _facing * elapsed * 0.08f;

            for (int streak = 3; streak >= 0; streak--)
            {
                int pastTick = Math.Max(0, elapsed - streak * 2);
                Vector2 position = start + new Vector2(0f, 0.35f * pastTick * pastTick);
                Color color = Lighting.GetColor(position.ToTileCoordinates());
                if (streak > 0)
                {
                    color = MadnessYellow * (0.35f / streak);
                }

                Main.EntitySpriteDraw(staff, position - Main.screenPosition, null, color, MathHelper.PiOver4 + spin, staff.Size() * 0.5f,
                    StaffDrawScale, SpriteEffects.None, 0);
            }

            Lighting.AddLight(start + new Vector2(0f, fall), MadnessYellow.ToVector3() * 0.4f);
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            base.SendExtraAI(writer);
            writer.Write((byte)_spell);
            writer.Write((byte)_lastSpell);
            writer.Write((byte)_customKind);
            writer.Write((sbyte)_facing);
            writer.Write(_aim.X);
            writer.Write(_aim.Y);
            writer.Write(_aimLocked);
            writer.Write(_cracked);
            writer.Write(_dissolving);
            writer.Write((byte)_stepHopCount);
            for (int hop = 0; hop < MaxStepHops; hop++)
            {
                writer.Write(_stepHops[hop].X);
                writer.Write(_stepHops[hop].Y);
            }

            writer.Write(_stepOrigin.X);
            writer.Write(_stepOrigin.Y);
            writer.Write((short)_neutralDelay);
            writer.Write((short)_stepCooldown);
            writer.Write((short)_shriekCooldown);
            for (int i = 0; i < _spellCooldowns.Length; i++)
            {
                writer.Write((short)_spellCooldowns[i]);
            }
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            base.ReceiveExtraAI(reader);
            _spell = (Spell)reader.ReadByte();
            _lastSpell = (Spell)reader.ReadByte();
            _customKind = (CustomKind)reader.ReadByte();
            _facing = reader.ReadSByte();
            _aim = new Vector2(reader.ReadSingle(), reader.ReadSingle());
            _aimLocked = reader.ReadBoolean();
            _cracked = reader.ReadBoolean();
            _dissolving = reader.ReadBoolean();
            _stepHopCount = reader.ReadByte();
            for (int hop = 0; hop < MaxStepHops; hop++)
            {
                _stepHops[hop] = new Vector2(reader.ReadSingle(), reader.ReadSingle());
            }

            _stepOrigin = new Vector2(reader.ReadSingle(), reader.ReadSingle());
            _neutralDelay = reader.ReadInt16();
            _stepCooldown = reader.ReadInt16();
            _shriekCooldown = reader.ReadInt16();
            for (int i = 0; i < _spellCooldowns.Length; i++)
            {
                _spellCooldowns[i] = reader.ReadInt16();
            }

            DebugAttackLabel = DebugLabelForState();
        }

        private string DebugLabelForState()
        {
            if (Phase == AttackPhase.Custom)
            {
                if (_customKind == CustomKind.Step)
                {
                    return "Frenzied Step";
                }

                if (_customKind == CustomKind.Shriek)
                {
                    return "Shriek";
                }

                if (_customKind == CustomKind.Crack)
                {
                    return "The Crack";
                }

                if (_customKind == CustomKind.Dissolve)
                {
                    return "Dissolving";
                }
            }

            if (_spell == Spell.Count)
            {
                return "";
            }

            return CurrentSpell.Name;
        }

        public override void HitEffect(NPC.HitInfo hit)
        {
            if (NPC.life > 0 || Main.dedServ)
            {
                return;
            }

            for (int i = 0; i < 50; i++)
            {
                int dustType = DustID.YellowTorch;
                Color tint = default;
                if (i % 3 == 0)
                {
                    dustType = DustID.Wraith;
                    tint = Color.Black;
                }
                else if (i % 3 == 1)
                {
                    dustType = DustID.WhiteTorch;
                }

                Dust burst = Dust.NewDustPerfect(NPC.Center, dustType, Main.rand.NextVector2Circular(7f, 7f), 40, tint, Main.rand.NextFloat(1.3f, 2.2f));
                burst.noGravity = true;
            }

            SoundEngine.PlaySound(SoundID.Item8 with { Volume = 0.9f, Pitch = -0.2f }, NPC.Center);
            UsefulFunctions.ScreenShake(NPC.Center, 4f, 18, distanceFalloff: 700f);
        }

        public override void OnKill()
        {
            UsefulFunctions.BroadcastText(LangUtils.GetTextValue("NPCs.OolacileAcolyte.Defeated"), 150, 150, 150);
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<HealingElixir>()));
            npcLoot.Add(ItemDropRule.ByCondition(tsorcRevamp.tsorcItemDropRuleConditions.AbyssRule, ModContent.ItemType<RedTitanite>()));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<PurgingStone>(), 30));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<CursedSoul>(), 2));
            npcLoot.Add(new CommonDrop(ModContent.ItemType<CrimsonPotion>(), 100, 1, 1, 5));
            npcLoot.Add(new CommonDrop(ModContent.ItemType<StrengthPotion>(), 100, 1, 1, 5));
            npcLoot.Add(new CommonDrop(ModContent.ItemType<ShockwavePotion>(), 100, 1, 1, 5));
            npcLoot.Add(new CommonDrop(ModContent.ItemType<BattlefrontPotion>(), 100, 1, 1, 5));
            npcLoot.Add(new CommonDrop(ItemID.BloodMoonStarter, 100, 1, 1, 5));
            npcLoot.Add(ItemDropRule.ByCondition(tsorcRevamp.tsorcItemDropRuleConditions.CursedRule, ModContent.ItemType<StarlightShardItem>(), 4));
            npcLoot.Add(ItemDropRule.ByCondition(tsorcRevamp.tsorcItemDropRuleConditions.AbyssRule, ModContent.ItemType<FlameOfTheAbyss>()));
        }
    }
}

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameContent.Drawing;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;
using Terraria.ModLoader.IO;
using tsorcRevamp.Buffs;
using tsorcRevamp.Buffs.Debuffs;
using tsorcRevamp.Buffs.Runeterra.Magic;
using tsorcRevamp.Buffs.Runeterra.Summon;
using tsorcRevamp.Buffs.Weapons;
using tsorcRevamp.Buffs.Weapons.Summon;
using tsorcRevamp.Buffs.Weapons.Summon.WhipDebuffs;
using tsorcRevamp.Items;
using tsorcRevamp.Items.Accessories.Defensive;
using tsorcRevamp.Items.Accessories.Damage;
using tsorcRevamp.Items.Armors.Melee;
using tsorcRevamp.Items.Debug;
using tsorcRevamp.Items.ItemCrates;
using tsorcRevamp.Items.Materials;
using tsorcRevamp.Items.Potions;
using tsorcRevamp.Items.VanillaItems;
using tsorcRevamp.Items.Weapons.Magic.Runeterra;
using tsorcRevamp.Items.Weapons.Ranged;
using tsorcRevamp.Items.Weapons.Ranged.Runeterra;
using tsorcRevamp.Items.Weapons.Ranged.Specialist;
using tsorcRevamp.Items.Weapons.Summon;
using tsorcRevamp.Items.Weapons.Summon.Runeterra;
using tsorcRevamp.Items.Weapons.Summon.Whips;
using tsorcRevamp.Items.Weapons.Enemy;
using tsorcRevamp.NPCs.Bosses.SuperHardMode.Fiends;
using tsorcRevamp.NPCs.Puppets;
using tsorcRevamp.Projectiles;
using tsorcRevamp.Projectiles.Ranged;
using tsorcRevamp.Projectiles.Summon;
using tsorcRevamp.Projectiles.Summon.Archer;
using tsorcRevamp.Projectiles.Summon.SamuraiBeetle;
using tsorcRevamp.Projectiles.Summon.Whips;
using tsorcRevamp.Projectiles.Summon.Whips.Dominatrix;
using tsorcRevamp.Projectiles.Summon.Whips.EnchantedWhip;
using tsorcRevamp.Projectiles.Summon.Whips.PolarisLeash;
using tsorcRevamp.Projectiles.VFX;
using tsorcRevamp.Utilities;
using tsorcRevamp;
using tsorcRevamp.Items.Weapons.Throwing;

namespace tsorcRevamp.NPCs
{
    // === Patrol/Pursue FSM enums (namespace-scoped so the matching fields on tsorcRevampGlobalNPC
    // can share the type name via C#'s "Color Color" rule). See
    // Documentation/PatrolPursue_and_NavTier_Removal.md. ===
    public enum PursuitState
    {
        Pursue, // has LOS, or a valid target + making progress
        Search, // lost LOS: move to LastKnownPlayerPos and look around (gated by RemembersLastKnownPos)
        Patrol, // gave up: calm patrol around an anchor, no pursuit jumps
        Flee    // hit by a player it has no A* path to (and can't blink to): run to a safe distance, then Patrol
    }

    public enum PatrolMode
    {
        Idle,          // mostly stand; occasional short walk routine
        Pace,          // walk back and forth within a leg around the anchor
        Wander,        // commit to a direction for many tiles before reconsidering
        ReturnToSpawn  // walk back to the spawn anchor, then idle there
    }

    public enum PatrolAnchorSource
    {
        SpawnPoint,    // patrol around where the NPC spawned (recorded on first tick)
        GiveUpLocation // patrol around where pursuit was abandoned
    }

    // When the disengage resolver decides to blink to re-acquire the player (see 4b in the doc).
    public enum TeleportStyle
    {
        Relaxed,    // gives up, wanders into Patrol a few seconds, THEN blinks. Long cooldown.
        Normal,     // blinks at the disengage point instead of patrolling (≈ legacy canTeleport). Medium cooldown.
        Aggressive  // blinks the moment LOS is lost (short debounce), bypassing Search/Patrol. Short cooldown.
    }

    public enum TeleportVisualStyle
    {
        Default,    // one burst of white smoke + shockwave flash (current behavior)
        GreySmoke,  // heavy grey smoke cloud lingering ~1s at both exit and entry
        Fire,       // fire + dark smoke cloud lingering ~1s at both exit and entry
        Plague,     // black/purple lingering cloud; origin cloud applies controlled curse buildup
        MagicIllusion, // leaves a translucent, invulnerable combat duplicate at the exit for four seconds
    }

    public enum InvisibilityStyle
    {
        Random,    // ambient per-frame flicker between visible/invisible; any hit snaps fully visible
        Predator,  // invisible while moving (and not attack-telling); a hit reveals for ~1.5s
        Evasive    // threat-reactive cloak: a hit or a nearby threat rolls the cloak; lingers, then fades
    }

    public enum PounceStyle
    {
        HighArcPounce,
        DirectPounce,
        None
    }

    public enum PreAttackJumpBehavior
    {
        VerticalPop,
        MainAttackHop,
        ShortForwardHop,
        HighForwardHop,
        OffensiveLeap,
        AttackDashHop,
        DesperateHighHop,
        DesperateSprayHop,
        ErraticFinalHover
    }

    public partial class tsorcRevampGlobalNPC : GlobalNPC
    {
        public override bool InstancePerEntity => true;
        private static readonly SoundStyle DefeatBannerSound = new SoundStyle("tsorcRevamp/Sounds/DarkSouls/boss-defeated") with { Volume = 1f };
        private const int GhostWallTeleportSmokeTicks = 30;
        private const int GhostWallTeleportSnapTicks = 15;
        private const int GhostWallMaxThicknessTiles = 4;
        private const float GhostWallTeleportMistRadiusScale = 1.25f;
        private const float GhostMagicKnockbackResist = 0.5f;
        private const float GhostMagicPoiseKnockbackFloor = 0.5f;
        private const float GhostMagicPoiseMax = 12f;
        // Magic weapons usually have a 0 knockback STAT, and no HitModifier can inject knockback into a 0-knockback hit
        // (the multiply yields 0). So the ghost's magic flinch is applied as a direct velocity impulse instead. Tune here.
        private const float GhostMagicFlinchVelocity = 2.5f;

        float enemyValue;
        float multiplier = 1f;
        float divisorMultiplier = 1f;
        int DarkSoulQuantity;
        /// <summary>Opt-out for staged encounter actors whose intermediate death must produce no global drops.</summary>
        public bool SuppressGlobalOnKillDrops;
        /// <summary>True for a temporary combat duplicate left behind by a MagicIllusion teleport.</summary>
        public bool IsTeleportIllusion;
        public int TeleportIllusionTimeLeft;
        public Player lastHitPlayerSummoner = Main.LocalPlayer;
        public Player lastHitPlayerRanger = Main.LocalPlayer;
        public Player lastHitPlayerShadowSickle = Main.LocalPlayer;

        public float SummonTagFlatDamage;
        public float SummonTagCriticalStrikeChance;
        public float FinalSummonCriticalStrikeChance;
        public float SummonTagScalingDamage;
        public float SummonTagArmorPenetration;
        public bool markedByCrystalNunchaku;
        public bool markedByDetonationSignal;
        public bool markedByDominatrix;
        public bool markedByDragoonLash;
        public bool markedBySupremeDragoonLash;
        public bool markedByEnchantedWhip;
        public bool markedByNightsCracker;
        public bool markedByPolarisLeash;
        public bool markedByPyrosulfate;
        public bool markedByPyromethane;
        public bool markedBySearingLash;
        public bool markedByTerraFall;
        public bool markedByTerraFallShard;
        public bool markedByUrumi;
        public bool markedByRustedChain;
        public bool markedByLeatherWhip;
        public bool markedByWitchkingMace;
        public bool markedBySnapthorn;
        public bool markedBySpinalTap;
        public bool markedByFirecracker;
        public bool markedByCoolWhip;
        public bool markedByDurendal;
        public bool markedByMorningStar;
        public bool markedByDarkHarvest;
        public bool markedByKaleidoscope;
        public bool Insane;

        public float CrystalNunchakuStacks = 10;
        public bool CrystalNunchakuProc = false;
        public int CrystalNunchakuUpdateTick = 0;
        public Player CrystalNunchakuWielder;

        public int NightsCrackerStacks = 0;
        public int TerraFallStacks = 0;

        public bool Scorched;
        public int ScorchMarks = 0;
        public float SuperScorchDuration = 0f;
        public bool Shocked;
        public int ShockMarks = 0;
        public float SuperShockDuration = 0f;
        public bool Sunburnt;
        public bool Awestruck;
        public int SunburnMarks = 0;
        public float SuperSunburnDuration = 0f;

        //Stores the event this NPC belongs to
        public ScriptedEvent ScriptedEventOwner;

        //Stores which NPC in that event this is
        public int ScriptedEventIndex;

        //Whatever custom expert scaling we want goes here. For reference 1 eliminates all expert mode doubling, and 2 is normal expert mode scaling.
        public static double expertScale = 2;

        public bool DarkInferno;
        public bool AbyssInferno;
        public bool MorgulPoisoning;
        public bool WitchkingCurse;
        public bool AbyssalSinking;
        public bool CCShocked;
        public bool Ignited;
        public bool CrimsonBurn;
        public bool ToxicCatDrain;
        public bool ResetToxicCatBlobs;
        public bool ViruCatDrain;
        public bool ResetViruCatBlobs;
        public bool BiohazardDrain;
        public bool ResetBiohazardBlobs;
        public bool ElectrocutedEffect;
        public bool ElectrocutedEffect2;
        public bool ElectrocutedEffect3;
        public bool PolarisElectrocutedEffect;
        public bool CrescentMoonlight;
        public bool Soulstruck;
        public bool PhazonCorruption;
        public bool PlaguesmithBuff;
        // Set true each tick by SorrowfulCurseBuff (the Cleric of Sorrow's Last Rites dying curse). While active,
        // the afflicted enemy's attacks inflict Cursed Inferno on the player it hits. Mirrors PlaguesmithBuff.
        public bool SorrowfulCurse;

        public int LionheartMarks = 0;

        public bool Sundered;

        public bool Venomized;
        public bool Electrified;
        public bool Irradiated;
        public bool IrradiatedByShroom;

        public int CritColorTier = 0;


        //Custom AI personality paramaters

        /// <summary>
        /// How likely it is to dash at the player if it is far away.
        /// Range: 0.00001 - 2.5
        /// </summary>
        public float Aggression = -1;

        /// <summary>
        /// Controls how quickly it gets bored (and thus how long it waits before teleporting, if it has that ability).
        /// Range: 0.5 - 2
        /// </summary>
        public float Patience = -1;

        /// <summary>
        /// How likely it is to try and run if it is low on health.
        /// Range: 0 - 0.3
        /// </summary>
        public float Cowardice = -1;

        /// <summary>
        /// Improves the likelihood of performing low-weighted attacks.
        /// Range: 0 - 0.3
        /// </summary>
        public float Adeptness = -1;

        /// <summary>
        /// Modifies movement speed and acceleration.
        /// Range: 0.7 - 1.3
        /// </summary>
        public float Swiftness = -1;

        /// <summary>
        /// Modifies how often it fires projectiles.
        /// Range: 0.6 - 1.4
        /// </summary>
        public float CastingSpeed = -1;

        /// <summary>
        /// Modifies base health, size, and contact damage.
        /// Range: 0.7 - 1.3
        /// </summary>
        public float Strength = -1;

        /// <summary>
        /// Controls how often it tries to roll through or jumps over projectiles.
        /// Range: 0.2 - 0.6
        /// </summary>
        public float Agility = -1;


        //Custom AI execution values
        public int DoorBreakProgress;
        public bool Fleeing;
        public bool Initialized;
        public int PounceTimer;
        public int PounceCooldown;
        public PounceStyle PounceStyle = PounceStyle.HighArcPounce;
        public Vector2 PounceTarget;
        public int DirectPounceAfterimageTimer;
        public int DirectPounceRecoveryTimer;
        public bool DirectPounceAfterimages = true;
        public int DodgeTimer;
        public int DodgeCooldown;
        public int FighterPostAttackPauseTimer;
        public int FighterAttacksSincePause;
        public bool FighterRangedHitInterruptedPause;
        public int FighterRangedStandShotsRemaining;  // >0 = standing-fire mode; decrement each shot, exit when zero
        public int FighterNoLosPursuitBoostTimer;

        // Single source of truth for "this enemy is committed to an attack and cannot be interrupted/knocked back".
        // Each enemy sets this every AI tick from its own flash-telegraph→fire windows (windup ≈25t before the flash
        // through the fire frame). EvasiveOnHit reads it (via InAttack) to gate the on-hit reaction. The poise system below
        // treats AttackCommitted as hyper-armor. See memory: project_poise_stagger_system.
        public bool AttackCommitted;

        // Windup phase: from when an attack begins through the flash telegraph (pre-AttackCommitted). Poise CAN build
        // here (a stagger interrupts a windup), but ordinary hits do NOT — the evasive on-hit reaction is suppressed
        // during any attack. Each enemy sets this each AI tick alongside AttackCommitted.
        public bool AttackTelegraphing;

        /// <summary>True during a windup, committed attack, or shared melee follow-through. Evasive reactions only fire when this is
        /// false (pure neutral); attack interruption is handled solely by the poise stagger.</summary>
        public bool InAttack => AttackTelegraphing || AttackCommitted || CombatMeleeActive;

        // === Guard pressure (Combat Balance B0/B1) ===
        // Block attribution/state lives here; the independent cadence snapshot and recovery live in
        // GlobalNPC.GuardPressure.cs. CombatTempo never requires this state to run.
        public const int GuardPressureMaxBlocks = 4;
        public const int GuardPressureFirstDecayTicks = 480;
        public const int GuardPressureStackDecayTicks = 240;
        public int BlockedRecently;
        public int BlockedDamageRecently;
        public int BlockedRecentlyByPlayer = -1;
        public int BlockedRecentlyDecayTimer;

        public void RegisterBlockedAttack(NPC npc, int playerIndex, int blockedDamage, bool perfectParry)
        {
            // A perfect parry is the player's successful counterplay, so it must not feed the anti-turtle ramp.
            if (Main.netMode == NetmodeID.MultiplayerClient || perfectParry
                || playerIndex < 0 || playerIndex >= Main.maxPlayers || !Main.player[playerIndex].active)
            {
                return;
            }

            // Keep one pressure target per enemy. A different player's block starts their own sequence instead of
            // inheriting a nearly-complete ramp built against somebody else.
            if (BlockedRecentlyByPlayer != playerIndex)
            {
                BlockedRecently = 0;
                BlockedDamageRecently = 0;
                BlockedRecentlyByPlayer = playerIndex;
            }

            BlockedRecently = Math.Min(GuardPressureMaxBlocks, BlockedRecently + 1);
            BlockedDamageRecently = (int)Math.Min(1000000L, (long)BlockedDamageRecently + Math.Max(0, blockedDamage));
            // Every new block restores the full grace period. If the defender disengages from blocking after that,
            // pressure bleeds away one stack at a time rather than forgetting the entire exchange at once.
            BlockedRecentlyDecayTimer = GuardPressureFirstDecayTicks;
            npc.netUpdate = true;
        }

        private void DecayGuardPressureStack(NPC npc)
        {
            if (BlockedRecently <= 1)
            {
                ClearGuardPressure(npc);
                return;
            }

            int previousStacks = BlockedRecently;
            BlockedRecently--;
            // Individual blocked-damage samples are not stored, so decay the aggregate proportionally with the
            // stack count. This keeps the dormant telemetry representative if it is used for tuning later.
            BlockedDamageRecently = (int)((long)BlockedDamageRecently * BlockedRecently / previousStacks);
            BlockedRecentlyDecayTimer = GuardPressureStackDecayTicks;
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                npc.netUpdate = true;
            }
        }

        private void ClearGuardPressure(NPC npc)
        {
            bool hadPressure = BlockedRecently != 0 || BlockedDamageRecently != 0 || BlockedRecentlyByPlayer != -1;
            BlockedRecently = 0;
            BlockedDamageRecently = 0;
            BlockedRecentlyByPlayer = -1;
            BlockedRecentlyDecayTimer = 0;
            if (hadPressure && Main.netMode != NetmodeID.MultiplayerClient)
            {
                npc.netUpdate = true;
            }
        }

        // === Pre-attack jump (global attack variation) ===
        // When an attack COMMITS (hyperarmor begins), optionally launch the enemy upward so the shot — fired later in
        // the committed window — goes off mid-air. Requires attack-phase tagging (it keys off AttackCommitted). The
        // SERVER rolls + applies the launch and netUpdates, so MP stays consistent (clients just receive the velocity).
        // Chance 1f = deterministic (every attack); <1f = a random variation. NOTE: incompatible with CanStopToFire
        // (its stop-to-fire zeroes velocity.Y each tick and cancels the jump) — use on enemies that don't stand to fire.
        public bool CanJumpBeforeAttack = false;
        public float JumpBeforeAttackChance = 0.5f;
        public float JumpBeforeAttackPower = 8f;   // upward launch velocity
        public int JumpBeforeAttackDelay = 10;     // frames after commit before the launch (so it's rising at the shot)
        public bool JumpBeforeAttackRequiresGrounded = true;
        public float JumpBeforeAttackDesperateLifeFraction = 0.5f;
        public bool JumpBeforeAttackVerticalPop = false;
        public bool JumpBeforeAttackMainAttackHop = false;
        public bool JumpBeforeAttackShortForwardHop = false;
        public bool JumpBeforeAttackHighForwardHop = false;
        public bool JumpBeforeAttackOffensiveLeap = false;
        public bool JumpBeforeAttackDashHop = false;
        public bool JumpBeforeAttackDesperateHighHop = false;
        public bool JumpBeforeAttackDesperateSprayHop = false;
        public bool JumpBeforeAttackErraticFinalHover = false;
        // Per-tick veto: an enemy sets this true (in its AI) during attacks that should NOT pre-jump (e.g. a stationary
        // channel like the basilisk magic-ring breath). Checked at the commit edge below. Defaults false each tick.
        public bool SuppressPreAttackJump = false;
        private bool wasAttackCommitted = false;   // rising-edge detector for AttackCommitted
        private int preAttackJumpTimer = -1;        // >0 counting down to the launch; <=0 inactive
        private PreAttackJumpBehavior queuedPreAttackJump = PreAttackJumpBehavior.VerticalPop;
        private static readonly List<PreAttackJumpBehavior> PreAttackJumpPool = new List<PreAttackJumpBehavior>(8);

        // Shared cooldown for the on-hit evasive reaction + wall-pin escape, so an enemy doesn't react every frame of a combo.
        public int FighterEvasionCooldown;

        // === Evasive on-hit capability flags (see EvasiveProfile + tsorcRevampAIs.EvasiveOnHit) ===
        // Opt-in per enemy in SetDefaults (directly, or via an EvasiveProfile.* bundle). The shared EvasiveOnHit
        // selector builds a weighted pool from whichever of these are set and samples one when the enemy is hit in
        // neutral. Each behavior carries its own melee/ranged/both affinity inside the selector, not here.
        public bool EvasiveRetreatJump;     // hop / leap / high-arc drift away (instant)
        public bool EvasiveRetreatDash;     // low fast dash away (instant)
        public bool EvasiveTeleportAway;    // blink away if CanTeleport, else leap (instant)
        public bool EvasiveLeapForward;     // lunge toward the player to close the gap (instant, ranged-hit counter)
        public bool EvasiveRunningDash;     // flash telegraph → grounded top-speed burst toward the player (sustained)
        public bool EvasiveRetreatAndShoot; // forced-flee back-off, then resume firing (sustained, melee-hit counter)
        public bool EvasiveQuickStep;       // i-frame quick step that passes through the player (sustained)
        public bool EvasiveBasiliskWalkerCloseBackhop;
        public bool EvasiveBasiliskWalkerFarScrambleHop;
        public bool EvasiveBasiliskShifterCloseBackhop;
        public bool EvasiveBasiliskShifterFarForwardHop;

        // === Sustained evasion state (driven each AI tick by UpdateEvasion; armed by EvasiveOnHit) ===
        // Only RunningDash / RetreatAndShoot live here; QuickStep has its own standing-dash timer, and
        // the instant behaviors don't need state. CurrentEvasion is only meaningful while InSustainedEvasion.
        public bool InSustainedEvasion;
        public EvasiveBehavior CurrentEvasion;
        public int EvasiveTimer;            // ticks left in the current phase
        public bool EvasiveTelegraphing;    // true during a windup/telegraph phase (RunningDash's flash)
        public float EvasiveDashSpeedMult = 2.2f;   // RunningDash: multiplier on top speed during the burst
        public int EvasiveDashTelegraphTicks = 20;  // RunningDash: flash/windup length before the burst
        public int EvasiveDashTicks = 110;          // RunningDash: burst length (~1.8s)
        public int EvasiveRetreatTicks = 80;        // RetreatAndShoot: back-off length (~1.3s)
        public float EvasiveRetreatSpeed = 3.5f;    // RetreatAndShoot: grounded back-off speed (away from player)
        public int QuickStepTimer;          // >0 = mid quick-step: i-frames + passes through player (standing dash, no roll)
        public int QuickStepRecoveryTimer;  // short post-step pause before pursuit resumes
        public int QuickStepDir;            // horizontal direction of the active quick step
        public float QuickStepSpeed = 5f;   // horizontal speed of the quick step
        public int QuickStepTicks = 16;     // quick-step duration
        public int QuickStepRecoveryTicks = 30;   // post-step recovery before attacks/pursuit resume (tunable)
        public float QuickStepForwardRoom = 16f;  // extra px past the player when stepping THROUGH them
        public float QuickStepForwardChance = 0.5f; // chance to cross instead of backstep when a forward landing is reachable
        public int QuickStepMaxForwardTicks = 30;   // longer than the ordinary backstep so a configured flank can start farther away
        // True while a behavior should draw the ghost afterimage trail (quick step, or the running-dash burst).
        public bool EvasiveAfterimagesActive => QuickStepTimer > 0 || (InSustainedEvasion && CurrentEvasion == EvasiveBehavior.RunningDash && !EvasiveTelegraphing);
        /// <summary>True during any multi-tick evasive action — a parallel to <see cref="InAttack"/> for deference.</summary>
        public bool InEvasion => InSustainedEvasion || QuickStepTimer > 0 || QuickStepRecoveryTimer > 0;
        /// <summary>True during a RetreatAndShoot back-off — seizes the body so the driver can drive the retreat velocity.</summary>
        public bool EvasiveRetreating => InSustainedEvasion && CurrentEvasion == EvasiveBehavior.RetreatAndShoot;

        // AI-tick (NOT on-hit) capability: jump straight up to dodge an incoming aimed projectile. Split out of the
        // dodgeroll scan (RunFighterCombatTriggers) so a non-rolling enemy can still pre-jump. canDodgeroll already
        // grants the jump option on its own, so Red/Black Knights keep today's roll-or-jump without setting this.
        public bool CanJumpToEvade = false;

        // The enemy's SetDefaults knockBackResist, captured once by BasicAI before any AI tick mutates it. Enemies whose
        // AI zeroes knockBackResist during attacks restore to THIS each tick, so the SetDefaults value (the per-enemy
        // "weight") stays the source of truth that shapes the poise flinch. -1 = not yet captured.
        public float BaseKnockBackResist = -1f;

        #region Poise / Stagger (Dark-Souls-style)
        // Deterministic stagger system (see project_poise_stagger_system).
        //  • Ordinary hits apply a LIGHT flinch knockback (PoiseFlinchFactor) so weapons feel like they connect, and
        //    accumulate poise damage = weapon knockback × PoiseDamageMultiplier (global stagger-rate knob).
        //  • AttackCommitted (post-flash) = hyper-armor: zero knockback, no poise. Windup is NOT committed, so a stagger
        //    there cancels the attack.
        //  • When poise fills → STAGGER: launch + ~2s freeze (movement + attack timer) + cancel windup.
        //  • Anti-stunlock: the break cooldown OUTLASTS the stun, giving a recovery window where the enemy can't be
        //    re-staggered and takes REDUCED (but non-zero) flinch knockback. Escalation stacks on top — each stagger
        //    within a long (~120s) window raises EffectivePoiseMax by 60%, so chained re-staggers get progressively harder.
        // Set PoiseMax > 0 in an enemy's SetDefaults to opt in; PoiseMax <= 0 keeps vanilla knockback behaviour.
        public float PoiseMax = 0f;               // per-enemy lever; <=0 disables the whole system for that NPC
        public float Poise = 0f;                  // current accumulated poise damage (0 → EffectivePoiseMax)
        public int StaggerTimer = 0;              // >0 = currently staggered (frozen; bar shows full + flashes)
        public int PoiseBreakCooldown = 0;        // >0 = i-frames after a break: zero knockback + immune to poise damage
        public int PoiseRegenDelay = 0;           // ticks remaining before accumulated poise starts decaying
        public float StaggerKnockback = 6f;       // horizontal launch magnitude applied on a poise break
        public float PoiseFlinchFactor = 0.4f;    // fraction of normal knockback applied on ordinary (non-breaking) hits
        public float PoiseCooldownFlinchFactor = 0.5f; // extra multiplier on flinch during the post-stagger recovery window (still registers, just sturdier)
        // GLOBAL stagger-rate knob: poise damage per hit = weapon knockback × this. Lower = more hits to stagger across
        // ALL enemies (doesn't touch flinch). Tune this rather than re-editing every PoiseMax.
        public static float PoiseDamageMultiplier = 0.5f;
        public bool PoiseStaggerResetsAI = false; // opt-in: a stagger sets ai[1]=60/ai[2]=-100 (cancels a windup attack)
        // Set true per-tick by an enemy holding a directional block (shield up). A FRONT hit then builds reduced poise
        // (ShieldGuardPoiseMult); backstabs are unaffected. The matching damage reduction lives in the enemy's own
        // front-facing ModifyHitBy block. The enemy re-sets this each frame, so it self-clears when not guarding.
        public bool ShieldGuarding = false;
        public const float ShieldGuardPoiseMult = 0.5f; // front hits build half poise while guarding

        // Reactive shield capability (shared by the shield enemies — HollowSoldier/Spearman + the Lothric knights —
        // configured via ShieldProfile). Layers on TOP of each enemy's own autonomous shield-timer rhythm:
        //  • PreemptiveBlockChance: per-tick roll, when a threat is detected (incoming projectile, or the player within
        //    ShieldThreatRange), to raise the guard BEFORE the hit lands.
        //  • OnHitBlockChance: roll when actually hit to snap the guard up and catch the rest of a combo.
        //  • ShieldedWalkSpeed: > 0 → advance toward the player at this speed while guarding instead of planting.
        // A successful reactive block sets ReactiveBlockTimer; while it is > 0 the enemy holds its guard regardless of
        // its autonomous timer / level gate (decremented centrally in PostAI), so it works against ranged too.
        public float PreemptiveBlockChance = 0f;
        public float OnHitBlockChance = 0f;
        public int ShieldThreatRange = 0;       // px; player within this range counts as a melee threat to guard against
        public float ShieldedWalkSpeed = 0f;    // 0 = plant in place (legacy); > 0 = slow shielded advance
        public int ReactiveBlockTimer = 0;      // > 0 = forced guard from a reactive block; ticks down in PostAI
        // Tunable durations (ticks). Defaults: ~2s stagger, then the cooldown OUTLASTS it (6s total from trigger → ~4s
        // reduced-flinch + no-re-stagger recovery after the stun), ~3s decay grace.
        public int StaggerDurationTicks = 120;
        public int PoiseBreakCooldownTicks = 360;
        public int PoiseRegenDelayTicks = 180;
        // Escalation ("the enemy learns"): each recent stagger adds a stack (raising the threshold by 60%) and refreshes
        // the window. Stacks reset only after the full window elapses with NO new stagger — so chaining staggers makes
        // each one progressively much harder over a long fight.
        public int PoiseEscalationStacks = 0;
        public int PoiseEscalationTimer = 0;       // ticks until stacks reset
        public const int PoiseEscalationMaxStacks = 8;        // up to +480% threshold (×5.8) for a much-staggered enemy
        public const float PoiseEscalationPerStack = 0.6f;    // +60% EffectivePoiseMax per stack
        public const int PoiseEscalationDurationTicks = 7200; // ~120s window (refreshed by each stagger)
        // Set in ModifyHitBy*, consumed in OnHitBy*: did the pre-strike calc decide this hit breaks poise?
        internal bool PoiseWillBreakThisHit = false;
        private float staggerSlideVelocity = 0f;  // horizontal knockback slide held across the stun
        private bool ghostKnockbackRestorePending = false;
        private float ghostKnockbackRestoreValue = 0f;

        /// <summary>PoiseMax scaled up by current escalation stacks — the actual threshold a hit must cross.</summary>
        public float EffectivePoiseMax => PoiseMax * (1f + PoiseEscalationPerStack * PoiseEscalationStacks);

        /// <summary>True when the poise bar should be drawn (and the stamina bar suppressed).</summary>
        public bool PoiseBarVisible => PoiseMax > 0f && (Poise > 0f || StaggerTimer > 0 || PoiseBreakCooldown > 0);

        private bool IsMagicKnockbackGhost(NPC npc)
            => HasGhostAfterimages && (npc.knockBackResist <= 0f || ghostKnockbackRestorePending);

        private bool IsPhysicalGhost(NPC npc)
            => HasGhostAfterimages && npc.knockBackResist <= 0f;

        private static bool HasMagicWeaponBuff(Player player)
            => player.HasBuff(ModContent.BuffType<Buffs.MagicWeapon>())
            || player.HasBuff(ModContent.BuffType<Buffs.GreatMagicWeapon>())
            || player.HasBuff(ModContent.BuffType<Buffs.CrystalMagicWeapon>());

        private bool ShouldMagicKnockbackGhost(NPC npc, Player player, Item item)
            => IsMagicKnockbackGhost(npc)
            && (item.CountsAsClass(DamageClass.Magic)
                || (item.CountsAsClass(DamageClass.Melee) && HasMagicWeaponBuff(player)));

        private bool ShouldMagicKnockbackGhost(NPC npc, Projectile projectile)
            => IsMagicKnockbackGhost(npc)
            && projectile.DamageType == DamageClass.Magic;

        private void EnableMagicGhostPoise(NPC npc)
        {
            if (PoiseMax <= 0f)
            {
                PoiseMax = GhostMagicPoiseMax;
            }

            if (!ghostKnockbackRestorePending)
            {
                ghostKnockbackRestoreValue = npc.knockBackResist;
                ghostKnockbackRestorePending = true;
            }

            npc.knockBackResist = GhostMagicKnockbackResist;
        }

        private void RestoreMagicGhostKnockback(NPC npc)
        {
            if (!ghostKnockbackRestorePending)
            {
                return;
            }

            npc.knockBackResist = ghostKnockbackRestoreValue;
            ghostKnockbackRestorePending = false;
        }

        /// <summary>Magic flinch for the pass-through-walls ghosts: a manual horizontal nudge away from the attacker,
        /// since their magic weapons usually have 0 knockback stat (so the normal multiplicative flinch can't move them).
        /// Skipped if this hit already staggered (that launch wins); reduced during the post-stagger recovery window.</summary>
        private void ApplyGhostMagicFlinch(NPC npc, Vector2 sourceCenter)
        {
            // No flinch while staggered (the launch wins) OR during the post-stagger recovery i-frames (white bar = 0
            // knockback, matching ApplyPoiseToKnockback).
            if (StaggerTimer > 0 || PoiseBreakCooldown > 0)
            {
                return;
            }
            float speed = GhostMagicFlinchVelocity;
            int dir = npc.Center.X >= sourceCenter.X ? 1 : -1;
            npc.velocity.X = dir * speed;
            npc.netUpdate = true;
        }

        /// <summary>Central poise tuning table (NPC type → knockback-resist flinch dial + PoiseMax), applied in
        /// SetDefaults. Single source of truth for the rollout — retune here. The Red Knight family configure themselves
        /// in-file; the lore ghosts and anything not listed are left out of the system. See project_poise_stagger_system.</summary>
        public static readonly Dictionary<int, (float knockBackResist, float poiseMax)> PoiseProfiles = new();

        public static void PopulatePoiseProfiles()
        {
            PoiseProfiles.Clear();
            static void Add<T>(float kbResist, float poiseMax) where T : ModNPC => PoiseProfiles[ModContent.NPCType<T>()] = (kbResist, poiseMax);

            // Bosses
            Add<Bosses.SuperHardMode.Gwyn>(0.15f, 150f); // the final boss — sturdiest poise in the game
            Add<Bosses.SuperHardMode.Artorias>(0.15f, 120f);
            Add<Bosses.SuperHardMode.Witchking>(0.15f, 120f);
            Add<Bosses.SuperHardMode.OolacileSerpent.GreatSerpentHead>(0.15f, 150f); // sturdier than the other bosses -- a lot of boss to stagger
            Add<Bosses.Slogra>(0.4f, 90f);   // kept at its already-tuned 0.4
            Add<Bosses.HeroofLumelia>(0.15f, 120f);
            Add<Enemies.SuperHardMode.SlograII>(0.4f, 50f);
            // Heavy giants / demons
            Add<Bosses.AncientDemon>(0.15f, 80f);
            Add<Bosses.AncientOolacileDemon>(0.15f, 80f);
            Add<Enemies.SuperHardMode.AncientDemonOfTheAbyss>(0.15f, 80f);
            Add<Enemies.Gigas>(0.15f, 120f); // caster-giant mini-boss: boss-tier poise; its long Wrath of Gold telegraph is the stagger window
            Add<Enemies.IceGigas>(0.15f, 120f); // frost terrain-controller mini-boss: boss-tier poise; Absolute Zero's channel is the stagger window
            Add<Bosses.GravelordNito.GravelordNito>(0.15f, 120f); // Skeletron-tier boss: harmless contact body, sword/projectile damage, Death Nova channel is the stagger window
            Add<Bosses.VesselOfSouls.VesselOfSouls>(0.15f, 120f); // EoC-slot flying eye boss: boss-tier poise; its gravity-well channel (phase 2) will be the stagger window
            Add<Enemies.DemonLordApocalypse>(0.15f, 80f);
            // Big bruisers
            Add<Enemies.Hydra>(0.2f, 60f);
            Add<Enemies.SuperHardMode.Massacre>(0.2f, 60f);
            // Elite / heavy knight
            Add<Enemies.SuperHardMode.TaurusKnight>(0.2f, 55f);
            Add<Enemies.SuperHardMode.CrystalKnight>(0.25f, 45f);
            // Standard fighters
            Add<Enemies.MinotaurMage>(0.3f, 35f); // beefy caster; its Ring of Cinders cast is the stagger window
            Add<Enemies.SuperHardMode.DarkBloodKnight>(0.3f, 35f);
            Add<Enemies.SuperHardMode.OolacileKnight>(0.3f, 35f);
            Add<Enemies.Basilisk.BasiliskHunter>(0.3f, 35f);
            Add<Enemies.Dworc.DworcAbysswalker>(0.3f, 35f);
            Add<Enemies.SuperHardMode.HydrisElemental>(0.3f, 35f);
            Add<Enemies.SuperHardMode.CorruptedElemental>(0.3f, 35f);
            // Standard+
            Add<Enemies.SuperHardMode.DarkKnight>(0.35f, 30f);
            Add<Enemies.Warlock>(0.35f, 30f);
            Add<Enemies.FireLurker>(0.35f, 30f);
            Add<Enemies.Tonberry>(0.35f, 30f);
            Add<Enemies.QuaraHydromancer>(0.35f, 30f);
            Add<Enemies.QuaraPincher>(0.3f, 60f); // brood-leader bruiser: Brood Call is the interruptible window
            Add<Enemies.QuaraClutchCrab>(0.5f, 18f); // summoned support kiter: fragile, easily staggered
            // Human / skilled
            Add<Enemies.FallenNecromancer>(0.4f, 26f);
            Add<Enemies.Necromancer>(0.4f, 26f);
            Add<Enemies.SuperHardMode.HydrisNecromancer>(0.4f, 26f);
            Add<Enemies.SuperHardMode.IceSkeleton>(0.4f, 26f);
            Add<Enemies.FirebombHollow>(0.4f, 26f);
            Add<Enemies.Basilisk.BasiliskShifter>(0.4f, 26f);
            // Light / agile
            Add<Enemies.ClericOfSorrow>(0.4f, 26f); // amphibious frost ritual-caster: Necromancer tier; its Communion/Undertow channels are the stagger windows
            Add<Enemies.Assassin>(0.45f, 20f);
            Add<Enemies.TibianAmazon>(0.45f, 20f);
            Add<Enemies.TibianValkyrie>(0.45f, 20f);
            // Lighter / hunters
            Add<Enemies.Basilisk.BasiliskWalker>(0.5f, 18f);
            Add<Enemies.ManHunter>(0.5f, 18f);
            Add<Enemies.RedCloudHunter>(0.5f, 18f);
            // Small
            Add<Enemies.Sandsprog>(0.55f, 15f);
            Add<Enemies.SandsprogMage>(0.55f, 15f);
            // Lightest
            Add<Enemies.Dworc.DworcFleshhunter>(0.6f, 13f);
            Add<Enemies.Dworc.DworcVenomsniper>(0.6f, 13f);
            Add<Enemies.Dunlending>(0.6f, 13f);   // small
            // Light to knock, but higher HP → sturdier to actually stagger
            Add<Enemies.Dworc.DworcAlchemist>(0.6f, 25f);
            Add<Enemies.Dworc.DworcVoodooShaman>(0.6f, 25f);
        }

        /// <summary>Pre-strike: hyper-armor and active stagger take zero vanilla knockback; everything else gets a light
        /// flinch (including the post-stagger cooldown). Also decides (deterministically) whether this hit breaks poise,
        /// so OnHit and the knockback agree.</summary>
        private void ApplyPoiseToKnockback(NPC npc, float poiseDamage, ref NPC.HitModifiers modifiers, Vector2 sourceCenter)
        {
            if (PoiseMax <= 0f)
            {
                return;
            }
            poiseDamage *= PoiseDamageMultiplier; // global stagger-rate knob (affects meter only, not the flinch below)
            // Shield guard: a FRONT block soaks stagger buildup (backstabs unaffected). Keep in sync with HandlePoiseOnHit.
            if (ShieldGuarding && Math.Sign(sourceCenter.X - npc.Center.X) == npc.direction)
            {
                poiseDamage *= ShieldGuardPoiseMult;
            }
            // ZERO knockback whenever the enemy can't take stagger damage = the white-bar states: hyper-armor
            // (AttackCommitted), already-staggered (manual slide drives it), OR the post-stagger recovery i-frames
            // (PoiseBreakCooldown). Previously the recovery window took REDUCED flinch; now it's fully immune.
            if (AttackCommitted || StaggerTimer > 0 || PoiseBreakCooldown > 0)
            {
                modifiers.DisableKnockback();
                PoiseWillBreakThisHit = false;
                return;
            }
            // Neutral / windup: light flinch so the hit always reads.
            modifiers.Knockback *= PoiseFlinchFactor;
            PoiseWillBreakThisHit = (Poise + poiseDamage) >= EffectivePoiseMax;
        }

        /// <summary>Post-strike: accumulate poise and trigger the stagger if it broke. Source is the attacker's center,
        /// used to pick the knockback direction.</summary>
        private void HandlePoiseOnHit(NPC npc, float poiseDamage, Vector2 sourceCenter)
        {
            if (PoiseMax <= 0f || AttackCommitted)
            {
                PoiseWillBreakThisHit = false;
                return; // hyper-armor: no poise, no knockback
            }
            poiseDamage *= PoiseDamageMultiplier; // global stagger-rate knob — keep in sync with ApplyPoiseToKnockback
            // Shield guard: a FRONT block soaks stagger buildup (backstabs unaffected). Mirrors ApplyPoiseToKnockback.
            if (ShieldGuarding && Math.Sign(sourceCenter.X - npc.Center.X) == npc.direction)
            {
                poiseDamage *= ShieldGuardPoiseMult;
            }
            if (StaggerTimer > 0)
            {
                ApplyStaggerImpulse(npc, sourceCenter, 0.5f); // already broken — a lighter follow-up shove
                PoiseWillBreakThisHit = false;
                return;
            }
            if (PoiseBreakCooldown > 0)
            {
                PoiseWillBreakThisHit = false;
                return; // post-stagger i-frames: immune to further poise damage (anti-stunlock)
            }

            Poise += poiseDamage;
            PoiseRegenDelay = PoiseRegenDelayTicks;

            if (PoiseWillBreakThisHit || Poise >= EffectivePoiseMax)
            {
                TriggerStagger(npc, sourceCenter);
            }
            PoiseWillBreakThisHit = false;
        }

        /// <summary>
        /// Riposte: poise damage dealt by a player's perfect parry, as a fraction of the enemy's current poise bar.
        ///
        /// Deliberately IGNORES AttackCommitted (hyper-armor), unlike every other poise source. A parry is by
        /// definition timed against an incoming attack, so the enemy is nearly always mid-commit when it lands —
        /// respecting hyper-armor here would mean parries essentially never staggered anything, which is the exact
        /// opposite of the intent. Anti-stunlock (PoiseBreakCooldown) and the already-staggered case are still
        /// honoured, so a parry can't chain-lock an enemy that's already down.
        /// </summary>
        public void ApplyParryPoise(NPC npc, float poiseFraction, Vector2 sourceCenter)
        {
            if (PoiseMax <= 0f || poiseFraction <= 0f)
            {
                return;
            }
            if (PoiseBreakCooldown > 0)
            {
                return; // post-stagger i-frames — same anti-stunlock rule as ordinary hits
            }
            if (StaggerTimer > 0)
            {
                ApplyStaggerImpulse(npc, sourceCenter, 0.5f); // already down: just a shove, no re-break
                return;
            }

            Poise += EffectivePoiseMax * poiseFraction;
            PoiseRegenDelay = PoiseRegenDelayTicks;
            if (Poise >= EffectivePoiseMax)
            {
                TriggerStagger(npc, sourceCenter);
            }
            npc.netUpdate = true;
        }

        /// <summary>Enter the staggered state: launch, freeze, cancel a windup attack, and escalate.</summary>
        private void TriggerStagger(NPC npc, Vector2 sourceCenter)
        {
            Poise = 0f;
            StaggerTimer = StaggerDurationTicks;
            SoundEngine.PlaySound(SoundID.Item27 with { Volume = 0.6f, PitchVariance = 0.15f }, npc.Center); // stagger cue
            // Escalation: add a stack and refresh the (long) window. The rising EffectivePoiseMax is what makes re-staggers
            // progressively harder — the recovery cooldown stays a flat ~recovery window (not stack-scaled, or it'd balloon).
            PoiseEscalationStacks = Math.Min(PoiseEscalationStacks + 1, PoiseEscalationMaxStacks);
            PoiseEscalationTimer = PoiseEscalationDurationTicks;
            PoiseBreakCooldown = PoiseBreakCooldownTicks;
            ApplyStaggerImpulse(npc, sourceCenter, 1f);
            // Cancel a windup attack. Enemies with bespoke attack STATE (C# flags / custom timer slots) implement
            // IStaggerable to clear exactly their own state; simple ai[1]/ai[2]-timer enemies use PoiseStaggerResetsAI.
            // The interface wins when both are present — it's strictly more capable.
            if (npc.ModNPC is IStaggerable staggerable)
            {
                staggerable.OnStagger(npc); // bespoke-state enemies clear exactly their own attack state
            }
            else
            {
                if (PoiseStaggerResetsAI)
                {
                    npc.ai[1] = 60f;   // simple ai[1]-timer enemies (Red Knight family): cancel a windup attack → neutral
                    npc.ai[2] = -100f;
                }
                // Universal AddAttack reset: the shared projectile system (Dworcs, casters, archers) drives its windup
                // off ProjectileTimer, not ai[1]. Resetting it cancels a telegraphing shot and restarts the cooldown.
                if (AttackList.Count > 0)
                {
                    ProjectileTimer = 0f;
                    AttackTelegraphing = false;
                    AttackCommitted = false;
                    FighterRangedStandShotsRemaining = 0;
                }
            }
            ResetCombatTempoSequence(clearRecovery: true);
            Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCHit4 with { Pitch = -0.3f, Volume = 0.8f }, npc.Center);
            npc.netUpdate = true;
        }

        // Pre-attack jump driver: on the rising edge of AttackCommitted, roll the jump and schedule the launch a few
        // frames later, so the shot (fired later in the committed window) goes off while the enemy is airborne. Server-
        // only; the launch netUpdates so MP clients receive the velocity. See CanJumpBeforeAttack.
        private void UpdatePreAttackJump(NPC npc)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                wasAttackCommitted = AttackCommitted;
                return;
            }

            if (CanJumpBeforeAttack && AttackCommitted && !wasAttackCommitted && !SuppressPreAttackJump
                && (!JumpBeforeAttackRequiresGrounded || npc.velocity.Y == 0f) && Main.rand.NextFloat() < JumpBeforeAttackChance)
            {
                queuedPreAttackJump = ChoosePreAttackJump(npc);
                preAttackJumpTimer = Math.Max(1, JumpBeforeAttackDelay);
            }
            wasAttackCommitted = AttackCommitted;

            if (preAttackJumpTimer > 0)
            {
                preAttackJumpTimer--;
                if (preAttackJumpTimer == 0)
                {
                    preAttackJumpTimer = -1;
                    // Headroom check: don't launch into a ceiling. Scan up to the full-power apex; if a solid tile is
                    // closer, cap the launch so the apex tucks ~1 tile under it (or skip entirely if there's < 2 tiles —
                    // the attack then just fires grounded). apex(tiles) = power² / (2·gravity·16).
                    ApplyPreAttackJump(npc, queuedPreAttackJump);
                }
            }
        }

        private PreAttackJumpBehavior ChoosePreAttackJump(NPC npc)
        {
            PreAttackJumpPool.Clear();
            bool desperate = npc.life <= npc.lifeMax * JumpBeforeAttackDesperateLifeFraction;

            if (desperate)
            {
                AddPreAttackJump(JumpBeforeAttackDesperateHighHop, PreAttackJumpBehavior.DesperateHighHop);
                AddPreAttackJump(JumpBeforeAttackDesperateSprayHop, PreAttackJumpBehavior.DesperateSprayHop);
                AddPreAttackJump(JumpBeforeAttackErraticFinalHover, PreAttackJumpBehavior.ErraticFinalHover);
            }

            if (PreAttackJumpPool.Count == 0)
            {
                AddPreAttackJump(JumpBeforeAttackVerticalPop, PreAttackJumpBehavior.VerticalPop);
                AddPreAttackJump(JumpBeforeAttackMainAttackHop, PreAttackJumpBehavior.MainAttackHop);
                AddPreAttackJump(JumpBeforeAttackShortForwardHop, PreAttackJumpBehavior.ShortForwardHop);
                AddPreAttackJump(JumpBeforeAttackHighForwardHop, PreAttackJumpBehavior.HighForwardHop);
                AddPreAttackJump(JumpBeforeAttackOffensiveLeap, PreAttackJumpBehavior.OffensiveLeap);
                AddPreAttackJump(JumpBeforeAttackDashHop, PreAttackJumpBehavior.AttackDashHop);
            }

            return PreAttackJumpPool.Count == 0 ? PreAttackJumpBehavior.VerticalPop : PreAttackJumpPool[Main.rand.Next(PreAttackJumpPool.Count)];
        }

        private static void AddPreAttackJump(bool enabled, PreAttackJumpBehavior behavior)
        {
            if (enabled)
            {
                PreAttackJumpPool.Add(behavior);
            }
        }

        private void ApplyPreAttackJump(NPC npc, PreAttackJumpBehavior behavior)
        {
            float velocityY;
            float velocityX = 0f;

            switch (behavior)
            {
                case PreAttackJumpBehavior.ShortForwardHop:
                    velocityY = Main.rand.NextFloat(-4f, -2f);
                    velocityX = npc.direction * Main.rand.NextFloat(1f, 2f);
                    break;
                case PreAttackJumpBehavior.MainAttackHop:
                    velocityY = Main.rand.NextFloat(-10f, -4f);
                    break;
                case PreAttackJumpBehavior.HighForwardHop:
                    velocityY = Main.rand.NextFloat(-10f, -3f);
                    velocityX = npc.direction * Main.rand.NextFloat(1f, 3f);
                    break;
                case PreAttackJumpBehavior.OffensiveLeap:
                    Lighting.AddLight(npc.Center, Color.OrangeRed.ToVector3());
                    if (Main.rand.NextBool(2))
                    {
                        Dust.NewDust(npc.position, npc.width, npc.height, DustID.Torch, npc.velocity.X, npc.velocity.Y);
                    }
                    velocityY = -8f;
                    velocityX = npc.direction * Main.rand.NextFloat(1f, 5f);
                    break;
                case PreAttackJumpBehavior.AttackDashHop:
                    velocityY = Main.rand.NextFloat(-10f, -2f);
                    velocityX = npc.direction * Main.rand.NextFloat(2f, 5f);
                    break;
                case PreAttackJumpBehavior.DesperateHighHop:
                    velocityY = Main.rand.NextFloat(-11f, -4f);
                    velocityX = npc.direction * Main.rand.NextFloat(0f, 1f);
                    break;
                case PreAttackJumpBehavior.DesperateSprayHop:
                    Lighting.AddLight(npc.Center, Color.OrangeRed.ToVector3() * 2f);
                    if (Main.rand.NextBool(2))
                    {
                        int dust = Dust.NewDust(npc.position, npc.width, npc.height, 6, npc.velocity.X - 6f, npc.velocity.Y, 150, Color.OrangeRed);
                        Main.dust[dust].noGravity = true;
                    }
                    velocityY = Main.rand.NextFloat(-7f, -3f);
                    break;
                case PreAttackJumpBehavior.ErraticFinalHover:
                    velocityY = Main.rand.NextFloat(-10f, 3f);
                    break;
                default:
                    velocityY = -JumpBeforeAttackPower;
                    break;
            }

            if (velocityY < 0f)
            {
                velocityY = -CapPreAttackJumpPower(npc, -velocityY);
            }

            if (velocityY != 0f || velocityX != 0f)
            {
                npc.velocity.Y = velocityY;
                npc.velocity.X += velocityX;
                npc.netUpdate = true; // sync the launch to MP clients
            }
        }

        private static float CapPreAttackJumpPower(NPC npc, float requestedPower)
        {
            // Headroom check: don't launch into a ceiling. Scan up to the full-power apex; if a solid tile is
            // closer, cap the launch so the apex tucks about 1 tile under it.
            float gravity = npc.gravity > 0f ? npc.gravity : 0.3f;
            int wantTiles = (int)Math.Ceiling(requestedPower * requestedPower / (2f * gravity * 16f));
            int clearTiles = 0;
            for (int i = 1; i <= wantTiles; i++)
            {
                if (UsefulFunctions.IsTileReallySolid(npc.Center + new Vector2(0f, -npc.height / 2f - i * 16f))) break;
                clearTiles = i;
            }
            if (clearTiles >= wantTiles)
            {
                return requestedPower;
            }
            return clearTiles < 2 ? 0f : (float)Math.Sqrt(2f * gravity * (clearTiles - 1) * 16f);
        }

        private void ApplyStaggerImpulse(NPC npc, Vector2 sourceCenter, float scale)
        {
            if (npc.boss)
            {
                scale *= 0.5f; // bosses shrug off some of the launch
            }
            int dir = npc.Center.X >= sourceCenter.X ? 1 : -1;
            staggerSlideVelocity = dir * StaggerKnockback * scale;
            npc.velocity.X = staggerSlideVelocity; // ApplyStaggerMovement carries/decays it across the stun
            npc.netUpdate = true;
        }

        /// <summary>Per-tick poise bookkeeping (timers, escalation reset, poise decay). Called at the start of PostAI.</summary>
        private void UpdatePoise(NPC npc)
        {
            if (PoiseMax <= 0f)
            {
                return;
            }
            if (StaggerTimer > 0)
            {
                StaggerTimer--;
            }
            if (PoiseBreakCooldown > 0)
            {
                PoiseBreakCooldown--;
            }
            if (PoiseEscalationTimer > 0)
            {
                PoiseEscalationTimer--;
                if (PoiseEscalationTimer == 0)
                {
                    PoiseEscalationStacks = 0;
                }
            }
            if (PoiseRegenDelay > 0)
            {
                PoiseRegenDelay--;
            }
            else if (Poise > 0f && StaggerTimer <= 0)
            {
                Poise -= PoiseMax / 240f; // full decay in ~4s once the grace window elapses
                if (Poise < 0f)
                {
                    Poise = 0f;
                }
            }
        }

        /// <summary>While staggered, override the AI's pursuit: hold the enemy in place (sliding from the knockback) and
        /// stop it jumping. Called at the END of PostAI so it wins over the movement the AI set this tick.</summary>
        private void ApplyStaggerMovement(NPC npc)
        {
            if (PoiseMax <= 0f || StaggerTimer <= 0)
            {
                return;
            }
            npc.velocity.X = staggerSlideVelocity;
            staggerSlideVelocity *= 0.72f;        // knockback slide bleeds off quickly — a staggered enemy is near-rooted
            if (Math.Abs(staggerSlideVelocity) < 0.1f)
            {
                staggerSlideVelocity = 0f;        // settle fully so it stands stunned rather than drifting
            }
            if (npc.velocity.Y < 0f)
            {
                npc.velocity.Y = 0f;              // cancel any jump the AI tried to start while stunned
            }
        }
        #endregion

        // Vertical jump power ceiling. Default 9f (apex ≈ 8.4 tiles) so a base-stat enemy can clear a
        // typical 6-7 tile ledge: ComputeJumpArc requires apex = rise + 1-tile lip, which for a 6-tile
        // rise needs power ≈ 8.2 — the old default of 8 fell just short and made the jump "infeasible",
        // so default enemies could never be issued a 6-tile climb. Per-enemy SetDefaults still overrides
        // (e.g. Assassin at 10f) for nimbler or heavier jumpers.
        public float MaxJumpPower = 9f;
        // Horizontal momentum added when jumping a gap
        public float MaxJumpBoost = 4f;
        // Whether this NPC can perform a mid-air second jump
        public bool CanDoubleJump = false;
        // Tracks whether the double jump has been used this airborne phase (reset on landing)
        public bool UsedDoubleJump = false;
        // Strength of the mid-air second jump
        public float DoubleJumpPower = 6f;
        // Counts consecutive frames the NPC has barely moved while pursuing; the position-immobility
        // anti-stuck uses it to disengage a walled/unreachable chase to the FSM (Search/Patrol/teleport).
        public int StuckTimer = 0;
        public bool CanStopToFire = false;
        // === Ranged kiting (smart positioning) ===
        // When KiteRangeMax > 0, a ranged enemy maintains a preferred DISTANCE BAND from a same-level, visible player
        // instead of pathing into melee. Read by SmartFighter4AI.TryRangedKite. KiteRangeMax <= 0 = off (normal walk-in
        // fighter). Closer than Min → back off; in [Min,Max] → slow drift + fire; farther than Max → let pursuit close in.
        public float KiteRangeMin = 0f;    // tiles
        public float KiteRangeMax = 0f;    // tiles (0 = kiting off)
        public float KiteLooseness = 0.3f; // 0..1 — chance per re-roll window to NOT back off, so melee can sometimes close
        // Flat-ground navigation for large enemies (avoid slopes / narrow ledges where a big sprite hangs off).
        public int MinSurfaceWidth = 0;                        // 0 = off; >=2 = only stand/walk/jump on flat ground that wide
        public bool RequiresFlatGround => MinSurfaceWidth > 0; // read-only; single source of truth is MinSurfaceWidth
        // How many tiles of unevenness the SUPPORT CORE (MinSurfaceWidth tiles) may straddle before a surface is
        // refused — i.e. how steep a slope/step the core can stand on. 2 lets it walk normal hill slopes; 0 = strictly
        // flat. The wider sprite EDGES clip/sink separately (BeastSinkMaxTiles). Only consulted when MinSurfaceWidth
        // > 0, so non-beasts are unaffected.
        public int MaxSurfaceStep = 2;
        // === Large-beast positioning (SF4 beasts only — all gated on RequiresFlatGround) ===
        // A giant should NEVER freeze: when it can't path to the player it falls back to a large preferred band
        // (KiteRangeMin/Max above) and oscillates in/out instead of standing; it bobs out of melee after each touch;
        // and it loses interest (wanders) if it can't reach the player AND hasn't been hit for a while.
        public int FramesSinceHit = 100000;     // ticked in PostAI, reset to 0 on any hit (OnHitByItem/Projectile)
        public bool BeastStale = false;          // gave up to wander (suppresses LOS re-aggro until hit re-engages)
        public int BeastUnreachableFrames = 0;   // frames it has had no path to the player (maintained by SF4 positioner)
        public int BeastContactBackoffTicks = 60;        // post-touch retreat window (anti sprite-center glitch)
        public float BeastRetreatSpeedHit = 4f;          // backpedal speed when freshly hit (px/frame)
        public float BeastRetreatSpeedCalm = 1.2f;       // backpedal/drift speed when not recently hit
        public int BeastStaleWanderTicks = 600;          // ~10s unreachable + unhit → wander off
        // How many tiles wide an obstruction may be for a beast to bull straight through it (Phase 2 phasing).
        public int BeastPhaseMaxWidth = 4;
        // Phase 3 ground-clip: MinSurfaceWidth is now the SUPPORT CORE (the center 2-4 tiles that must be on solid
        // ground); the wider sprite EDGES are allowed to clip into terrain. This is how far (in tiles) the sprite may
        // sink to follow the ground under its full width, so the overhang buries into a slope/hill instead of
        // floating in air. Drawn on top (gfxOffY). ~1/3 of the sprite height is a good ceiling.
        public int BeastSinkMaxTiles = 3;
        // Backwards Walking (Moonwalk) fields
        public bool CanWalkBackwards = false;
        // Frames spent voluntarily halted at a ledge; used to cap ledge-camping.
        public int LedgeHaltTimer = 0;
        // When true, FighterAI will halt at the edge of a significant drop when it already has
        // line of sight to the player.  Disabled by default — only opt in for enemies that are
        // supposed to hold the high ground (e.g. ranged enemies that shouldn't charge off ledges).
        public bool HaltAtLedge = false;
        // When true, the NPC teleports through solid walls it cannot navigate around.
        public bool CanPassThroughWalls = false;
        // Counts ticks the NPC has been grounded and blocked by a wall; triggers teleport at threshold.
        public int GhostWallTimer = 0;
        // Wall-phasing ghosts that prove the target is unreachable spend a short stint drifting away instead
        // of immediately re-fixating on the player's X column.
        public int GhostUnreachableWanderTimer = 0;
        // Set true each frame the NPC runs the mod's custom BasicAI/Fighter/Archer AI. Lets PostAI apply
        // confusion (reversed movement) only to these NPCs — vanilla-AI NPCs already handle Confused themselves,
        // so we must not double-flip them. Consumed (reset) in PostAI.
        public bool RunningCustomFighterAI = false;
        // Teleport visual tuning. Defaults reproduce the current smoke-flash behavior.
        public int TeleportTelegraphTime = 140;
        public int TeleportDustType = DustID.Smoke;
        public Color TeleportDustColor = Color.White;
        public float TeleportDustScale = 0.8f;
        public int TeleportDustCount = 20;
        // Throttle tick for the LogFighterNavDebug file logger.
        public int LastNavDebugLogTick = 0;

        // === Patrol/Pursue FSM state (Phase 1 — see Documentation/PatrolPursue_and_NavTier_Removal.md) ===
        // Defaults reproduce a dumb chase-on-sight enemy. Per-enemy levers override in SetDefaults.
        public PursuitState PursuitState = PursuitState.Pursue;
        // Give-up clock: ticks of FAILED pursuit (no LOS AND not progressing toward the player/last-known).
        // Resets on LOS or clear progress. Disengages when it exceeds NavGiveUpTicks. (Replaces BoredTimer.)
        public int DisengageTimer = 0;
        // Last position the NPC had line of sight to the player; target for the Search state.
        public Vector2 LastKnownPlayerPos = Vector2.Zero;
        // Generalizes BeastUnreachableFrames to ANY SF4 (NavSearchRadius > 0) enemy: consecutive frames spent
        // Pursuing with no A* plan and unable to engage (maintained by SmartFighter4AI, alongside its own
        // StuckGiveUpFrames give-up counter). Used as the "genuinely can't reach this player" signal for Flee.
        public int UnreachableFrames = 0;
        // === Flee-to-safety state ===
        // Entered when hit while UnreachableFrames shows the attacker is genuinely unreachable (and this NPC
        // can't just blink to them). Runs away from the attacker, gently speeding up, until FleeMaxTiles or a
        // wall/cliff, then settles into a normal Patrol/Wander. Beasts (RequiresFlatGround) keep their own
        // BeastStale stale-wander instead — Flee doesn't apply to them.
        public float FleeOriginX = 0f;
        public int FleeDirection = 0;
        public int FleeElapsedFrames = 0;

        // -- Deterministic intelligence levers --
        // SF4 A* search-window radius in tiles. 0 = no global pathfinding (chase-on-sight + give up);
        // ~16-24 = routes around local dead ends (needs radius >= dead-end depth); 40-50 = solves mazes.
        // Smaller is both dumber and cheaper. NOTE: size != intelligence (size -> clearance, handled automatically).
        public int NavSearchRadius = 0;
        // May this enemy climb ropes (SF4 only)? Default OFF — opt-in per enemy. Puppets default it ON, and the
        // TibianValkyrieSmart4 rope testbed sets it. A giant beast grabbing a rope looks wrong, hence default off.
        public bool CanUseRopes = false;
        // DisengageTimer threshold. Short = skittish (gives up fast); long = relentless hunter.
        public int NavGiveUpTicks = 600;
        // When true, on losing LOS the NPC investigates LastKnownPlayerPos (Search) before patrolling.
        public bool RemembersLastKnownPos = false;

        // -- Patrol configuration --
        public PatrolMode PatrolMode = PatrolMode.Idle;
        public PatrolAnchorSource PatrolAnchorSource = PatrolAnchorSource.SpawnPoint;
        // Leash radius (tiles) for Pace/Wander around PatrolAnchor.
        public int PatrolRange = 30;
        // World-space anchor the patrol centres on; set per PatrolAnchorSource.
        public Vector2 PatrolAnchor = Vector2.Zero;
        public bool PatrolAnchorSet = false;

        // -- Patrol working state (managed by NavBehavior) --
        public int PatrolDirection = 0;     // current patrol facing (-1 / +1)
        public int PatrolLegRemaining = 0;  // frames left on the current walking leg before pausing/reconsidering
        public int PatrolIdleTimer = 0;     // sub-timer driving the idle stand/walk routine
        public int PatrolElapsed = 0;       // frames spent in the current Patrol stint (drives Relaxed-teleport delay)
        // Last frame's distance to the pursuit target; drives the FSM "made progress" (closing distance) test.
        public float LastPursuitDist = 0f;
        // Reference position for the position-immobility anti-stuck (robust to wall-bouncing / LOS flicker).
        public Vector2 StuckCheckPos = Vector2.Zero;

        // === Unified teleport (Phase 1 — 4b; see Documentation/PatrolPursue_and_NavTier_Removal.md) ===
        // Merges the legacy `canTeleport` AI param and the WeakTeleport system into one re-acquire-on-give-up
        // blink (reusing TeleportCountdown / QueueTeleport / ExecuteQueuedTeleport for the actual smoke + warp).
        public bool CanTeleport = false;
        public bool CanDodgeroll = false; // mirrors the FighterAI canDodgeroll param; read by the wall-pin escape
        public TeleportStyle TeleportStyle = TeleportStyle.Normal;
        // -1 = unlimited (legacy canTeleport). A positive value = limited charges that DO NOT recharge
        // (legacy WeakTeleport = 2). Spent down via TeleportChargesRemaining.
        public int TeleportMaxCharges = -1;
        public int TeleportChargesRemaining = -1; // initialised from TeleportMaxCharges on first AI tick
        public int TeleportCooldownTimer = 0;     // frames until the next blink is allowed (per-style cooldown)
        // Bias teleport (and, later, patrol/standoff) destinations toward elevated spots with LOS — for archers
        // and high-ground hunters.
        public bool PrefersHighGround = false;
        // Set once TeleportChargesRemaining has been seeded from TeleportMaxCharges (so SetDefaults overrides win).
        public bool TeleportChargesInitialized = false;

        // === Health-scaled top speed ===
        // When HealthScaledSpeedBase >= 0, BasicAI replaces the topSpeed parameter each frame with:
        //   speed = (life/lifeMax) * HealthScaledSpeedMultiplier + HealthScaledSpeedBase
        // The multiplier uses the 0.0–1.0 life fraction (NOT 0–100 integer percent).
        // Negative multiplier → enemy speeds up as it loses HP (most common usage).
        // Example: base=2, multiplier=-1.5 → 0.5 at full HP, 2.0 near death.
        //   (Equivalent to the legacy inline: intPercent(0-100) * -0.015 + 2)
        // HealthScaledSpeedBase = -1 (default) disables the feature; the FighterAI topSpeed param is used as-is.
        public float HealthScaledSpeedBase = -1f;
        public float HealthScaledSpeedMultiplier = -1.5f;

        // Convenience method for enemies not yet on FighterAI that still want the formula.
        public float ComputeHealthScaledSpeed(NPC npc, float fallback) =>
            HealthScaledSpeedBase >= 0
                ? (npc.life / (float)npc.lifeMax) * HealthScaledSpeedMultiplier + HealthScaledSpeedBase
                : fallback;

        // === Standoff distance ===
        // When > 0, PostAI zeroes horizontal velocity while the NPC is grounded, has LOS to its
        // target, and is within this many tiles horizontally.  Use for ranged enemies that should
        // stop advancing once in range rather than walking into the player.
        public int StandoffDistance = 0;

        // === Teleport visual style ===
        public TeleportVisualStyle TeleportVisualStyle = TeleportVisualStyle.Default;
        // Frames remaining before NPC position snaps to TeleportTelegraph (smoke/fire styles only).
        // Set to 30 by ExecuteQueuedTeleport so the NPC moves halfway through the 1s smoke cloud.
        public int TeleportAppearanceTimer = 0;

        // === Ally heal aura ===
        // When true, PostAI has a 1-in-HealAlliesChance chance each frame to restore
        // HealAlliesPercent% of max HP to every enemy NPC within HealAlliesRange tiles.
        public bool CanHealAllies = false;
        public int HealAlliesChance = 500;   // 1-in-N per frame
        public int HealAlliesRange = 50;     // tile radius
        public int HealAlliesPercent = 10;   // % of lifeMax restored per proc

        // === Passive random self-heal ===
        // When true, PostAI has a 1-in-SelfHealChance chance per frame to restore SelfHealAmount HP,
        // capped at lifeMax.  Fires netUpdate so MP clients stay in sync.
        public bool CanSelfHeal = false;
        // HP restored per proc (default 5).
        public int SelfHealAmount = 5;
        // 1-in-N chance per frame to proc (default 360 ≈ once every ~6 seconds — raised from 250 to cut heal
        // frequency ~30% across all self-heal enemies; no enemy overrides this).
        public int SelfHealChance = 360;

        // === Random-flicker invisibility ===
        // When true, PostAI randomly flickers the NPC between visible and nearly-transparent each frame,
        // and snaps it fully visible on hit.  All alpha changes fire netUpdate so MP clients stay in sync.
        public bool CanGoInvisible = false;
        // Target alpha when hidden (0 = opaque, 255 = invisible; ~205-230 = ghost-like but still targetable).
        public int InvisibleAlpha = 210;
        // 1-in-N chance per frame to go invisible (lower = hides more often).
        public int GoInvisibleChance = 100;
        // 1-in-N chance per frame to snap back to fully visible (lower = reappears more often).
        public int GoVisibleChance = 200;
        public InvisibilityStyle InvisibilityStyle = InvisibilityStyle.Random;
        // === Evasive style tuning (only used when InvisibilityStyle == Evasive; see EvasiveProfile.EvasiveCloak) ===
        public float EvasiveCloakChance = 0.3f;        // probability to cloak WHEN a hit/threat triggers it (sneakier enemy = higher)
        public float InvisibleHitRevealChance = 0.7f;  // chance a hit taken while cloaked ends it early
        public int EvasiveThreatRange = 0;             // >0 = also cloak when the player is within this many px (melee approach)
        public int EvasiveCloakDurationTicks = 300;    // how long each cloak lasts (set equal to the cooldown by EvasiveCloak)
        public int EvasiveCloakCooldownTicks = 300;    // downtime after a reveal before it can cloak again (balances easy cloakers)
        private int EvasiveCloakTimer = 0;             // ticks left of the current cloak (server-only)
        private int EvasiveCloakCooldown = 0;          // ticks left of post-reveal downtime (server-only)
        public bool IsInvisible = false;
        // Draws the same ghostly afterimage/shimmer used while invisible, without changing alpha or targeting.
        public bool HasGhostAfterimages = false;
        private const int HunterVisionInvisibleAlpha = 50;
        private int InvisibilityRevealTimer = 0;
        private readonly Vector2[] InvisibilityTrailPositions = new Vector2[8];
        private bool InvisibilityTrailInitialized = false;

        public bool needsNetUpdate;
        public float ProjectileTimer;
        public float ProjectileTimerCap
        {
            get
            {
                return CurrentAttack.timerCap;
            }
        }
        public float ProjectileTelegraphStart
        {
            get
            {
                return ProjectileTimerCap - CurrentAttack.telegraphTime;
            }
        }
        public float ArcherAimDirection;
        public Vector2 LockedShotVector;
        public Vector2 LockedShotTargetPosition;
        public int LockedShotFacingDirection;
        public int TeleportCountdown;
        public List<tsorcRevampAIs.ProjectileData> AttackList = new List<tsorcRevampAIs.ProjectileData>();
        public int AttackIndex;
        public int AttackSucceeded = -1;
        public tsorcRevampAIs.ProjectileData CurrentAttack
        {
            get
            {
                return AttackList[AttackIndex];
            }
        }
        public int NextAttackIndex;
        private Vector2 TeleportTelegraphInternal;
        public Vector2 TeleportTelegraph
        {
            get
            {
                return TeleportTelegraphInternal;
            }
            set
            {
                //This lets it automatically trigger a netupdate whenever this variable is set to something new
                needsNetUpdate = true;
                TeleportTelegraphInternal = value;
            }
        }

        //Stores the targeting, tracking, and despawning information for a NPC
        public NPCDespawnHandler DespawnHandler;
        private static HashSet<int> defeatedPillars = new HashSet<int>();

        public override void ResetEffects(NPC npc)
        {
            DarkInferno = false;
            AbyssInferno = false;
            MorgulPoisoning = false;
            WitchkingCurse = false;
            AbyssalSinking = false;
            CCShocked = false;
            Ignited = false;
            CrimsonBurn = false;
            ToxicCatDrain = false;
            ResetToxicCatBlobs = false;
            ViruCatDrain = false;
            ResetViruCatBlobs = false;
            BiohazardDrain = false;
            ResetBiohazardBlobs = false;
            ElectrocutedEffect = false;
            ElectrocutedEffect2 = false;
            ElectrocutedEffect3 = false;
            PolarisElectrocutedEffect = false;
            CrescentMoonlight = false;
            Soulstruck = false;
            PhazonCorruption = false;
            PlaguesmithBuff = false;
            SorrowfulCurse = false;
            Venomized = false;
            Electrified = false;
            Irradiated = false;
            IrradiatedByShroom = false;
            markedByCrystalNunchaku = false;
            markedByDetonationSignal = false;
            markedByDominatrix = false;
            markedByDragoonLash = false;
            markedBySupremeDragoonLash = false;
            markedByEnchantedWhip = false;
            markedByNightsCracker = false;
            markedByPolarisLeash = false;
            markedByPyrosulfate = false;
            markedByPyromethane = false;
            markedBySearingLash = false;
            markedByTerraFall = false;
            markedByTerraFallShard = false;
            markedByUrumi = false;
            markedByLeatherWhip = false;
            markedBySnapthorn = false;
            markedBySpinalTap = false;
            markedByFirecracker = false;
            markedByCoolWhip = false;
            markedByDurendal = false;
            markedByMorningStar = false;
            markedByDarkHarvest = false;
            markedByKaleidoscope = false;
            Sundered = false;
            Scorched = false;
            Shocked = false;
            Sunburnt = false;
            Insane = false;
        }

        public override bool PreAI(NPC npc)
        {
            if (needsNetUpdate)
            {
                needsNetUpdate = false;
                npc.netUpdate = true;
            }

            // Keep dynamic-event NPCs from despawning on their own. CheckActive blocks the distance despawn, but
            // many enemies (e.g. dungeon skeletons placed outside the dungeon) call EncourageDespawn every tick,
            // which caps timeLeft back down — so a one-time set isn't enough. Re-pin it each tick. Without this,
            // one self-despawning NPC fails the event's all-or-nothing alive check and tears the whole event down.
            if (ScriptedEventOwner != null && !string.IsNullOrEmpty(ScriptedEventOwner.DynamicEventID))
            {
                npc.timeLeft = int.MaxValue;
            }

            return base.PreAI(npc);
        }

        public override bool CheckActive(NPC npc)
        {
            // NPCs spawned by a player-placed dynamic event are positioned deliberately in the editor; don't let
            // them despawn the vanilla "no players nearby / off-screen" way. Otherwise a freshly-spawned event NPC
            // that lands just off-screen vanishes a tick after spawning, which fails the event's all-or-nothing
            // despawn check and tears the whole event down (seen as warning dust but no NPCs, esp. multi-NPC events).
            // Scoped to dynamic (editor) events so existing hardcoded encounters keep their original despawn behavior.
            if (ScriptedEventOwner != null && !string.IsNullOrEmpty(ScriptedEventOwner.DynamicEventID))
            {
                return false;
            }
            return base.CheckActive(npc);
        }

        private void UpdateInvisibility(NPC npc)
        {
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                if (npc.justHit && InvisibilityStyle != InvisibilityStyle.Evasive)
                {
                    // Random/Predator: any hit snaps fully visible. Evasive owns its own hit handling (a hit is a
                    // cloak TRIGGER when visible, and only a CHANCE to reveal when already cloaked) — see below.
                    if (InvisibilityStyle == InvisibilityStyle.Predator)
                    {
                        InvisibilityRevealTimer = 90;
                    }
                    SetInvisible(npc, false);
                }
                else if (InvisibilityStyle == InvisibilityStyle.Predator && InvisibilityRevealTimer > 0)
                {
                    InvisibilityRevealTimer--;
                    SetInvisible(npc, false);
                }
                else if (InvisibilityStyle == InvisibilityStyle.Predator)
                {
                    SetInvisible(npc, ShouldPredatorBeInvisible(npc));
                }
                else if (InvisibilityStyle == InvisibilityStyle.Evasive)
                {
                    UpdateEvasiveInvisibility(npc);
                }
                else if (Main.rand.NextBool(GoVisibleChance))
                {
                    SetInvisible(npc, false);
                }
                else if (Main.rand.NextBool(GoInvisibleChance))
                {
                    SetInvisible(npc, true);
                }
                else
                {
                    ApplyInvisibilityAlpha(npc, false);
                }
            }
            else
            {
                ApplyInvisibilityAlpha(npc, false);
            }
        }

        private void SetInvisible(NPC npc, bool invisible)
        {
            if (IsInvisible != invisible)
            {
                IsInvisible = invisible;
                npc.netUpdate = true;
            }

            ApplyInvisibilityAlpha(npc, true);
        }

        private void ApplyInvisibilityAlpha(NPC npc, bool syncAlphaChanges)
        {
            int targetAlpha = IsInvisible ? (LocalPlayerRevealsInvisibleNPCs() ? HunterVisionInvisibleAlpha : InvisibleAlpha) : 0;

            if (npc.alpha != targetAlpha)
            {
                npc.alpha = targetAlpha;
                if (syncAlphaChanges)
                {
                    npc.netUpdate = true;
                }
            }
        }

        private bool LocalPlayerRevealsInvisibleNPCs()
        {
            if (Main.netMode == NetmodeID.Server)
            {
                return false;
            }

            Player player = Main.LocalPlayer;
            return player != null && player.active && !player.dead && (player.detectCreature || player.HasBuff(BuffID.Hunter));
        }

        private bool ShouldPredatorBeInvisible(NPC npc)
        {
            if (IsUsingAttackTell())
            {
                return false;
            }

            return Math.Abs(npc.velocity.X) > 0.25f || Math.Abs(npc.velocity.Y) > 0.25f;
        }

        // Evasive style: cloak is a reaction to danger, not an ambient flicker. Runs only when not justHit-handled
        // above (which it isn't, since Evasive is excluded there) — this method handles hits itself.
        //  • Cloaked: count the guaranteed-cloak floor down; a hit rolls InvisibleHitRevealChance to drop the cloak,
        //    and once past the floor GoVisibleChance is the per-frame reveal roll (so it tunes cloak duration).
        //  • Visible: a hit OR a nearby threat rolls GoInvisibleChance to cloak (and arms the floor).
        private void UpdateEvasiveInvisibility(NPC npc)
        {
            if (IsInvisible)
            {
                if (EvasiveCloakTimer > 0)
                {
                    EvasiveCloakTimer--;
                }

                // A poise break always rips the cloak off (the two systems talk via the shared StaggerTimer). An
                // ordinary hit only has a chance — and higher-tier cloakers (see EvasiveCloak) resist it more.
                bool staggerReveal = StaggerTimer > 0;
                bool hitReveal = npc.justHit && Main.rand.NextFloat() < InvisibleHitRevealChance;
                if (staggerReveal || hitReveal || EvasiveCloakTimer == 0) // staggered, shot out early, or full duration elapsed
                {
                    SetInvisible(npc, false);
                    EvasiveCloakCooldown = EvasiveCloakCooldownTicks; // downtime before it can cloak again
                }
                else
                {
                    ApplyInvisibilityAlpha(npc, false);
                }
            }
            else
            {
                if (EvasiveCloakCooldown > 0)
                {
                    EvasiveCloakCooldown--;
                }

                bool triggered = (npc.justHit || EvasiveThreatNearby(npc)) && EvasiveCloakCooldown == 0;
                if (triggered && Main.rand.NextFloat() < EvasiveCloakChance)
                {
                    SetInvisible(npc, true);
                    EvasiveCloakTimer = EvasiveCloakDurationTicks;
                }
                else
                {
                    ApplyInvisibilityAlpha(npc, false);
                }
            }
        }

        // A friendly projectile heading roughly at this NPC, OR (when EvasiveThreatRange > 0) the target player within
        // that range. Mirrors the incoming-projectile predicate used by the dodge/jump-to-evade scan.
        private bool EvasiveThreatNearby(NPC npc)
        {
            if (EvasiveThreatRange > 0 && npc.HasValidTarget &&
                npc.DistanceSQ(Main.player[npc.target].Center) < EvasiveThreatRange * EvasiveThreatRange)
            {
                return true;
            }

            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile proj = Main.projectile[i];
                if (proj.active && proj.friendly && proj.damage > 0 && proj.DistanceSQ(npc.Center) < 40000 &&
                    UsefulFunctions.CompareAngles(proj.velocity, UsefulFunctions.Aim(proj.Center, npc.Center, 1)) < 0.3f)
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsUsingAttackTell()
        {
            if (ArcherAimDirection != 0f || FighterRangedStandShotsRemaining > 0 || TeleportCountdown > 0 || TeleportAppearanceTimer > 0)
            {
                return true;
            }

            if (AttackList.Count > 0 && AttackIndex >= 0 && AttackIndex < AttackList.Count)
            {
                int telegraphStart = Math.Max(0, CurrentAttack.timerCap - 24);
                return ProjectileTimer >= telegraphStart;
            }

            return false;
        }

        private void UpdateInvisibilityTrail(NPC npc)
        {
            if (!InvisibilityTrailInitialized)
            {
                for (int i = 0; i < InvisibilityTrailPositions.Length; i++)
                {
                    InvisibilityTrailPositions[i] = npc.Center;
                }
                InvisibilityTrailInitialized = true;
                return;
            }

            if (Vector2.DistanceSquared(InvisibilityTrailPositions[0], npc.Center) < 4f)
            {
                return;
            }

            for (int i = InvisibilityTrailPositions.Length - 1; i > 0; i--)
            {
                InvisibilityTrailPositions[i] = InvisibilityTrailPositions[i - 1];
            }
            InvisibilityTrailPositions[0] = npc.Center;
        }

        public override void PostAI(NPC npc)
        {
            // Anti-despawn for dynamic-event NPCs. Some enemies (e.g. dungeon BoneThrowingSkeleton variants placed
            // outside the dungeon) set active=false directly the tick they spawn — neither CheckActive nor a timeLeft
            // pin stops that. PostAI runs after the vanilla AI, so revive them here as long as they're still alive
            // (life > 0). A player kill drops life to 0, so we never resurrect something that was legitimately killed.
            // Without this, one self-despawning NPC fails the event's all-or-nothing alive check and tears it down.
            // EXCLUDED: SelfDeactivatingNPCs (Marilith/Prime intros, Gwyn vision, portals, etc.) deliberately set
            // active=false mid-AI to transform into the real boss or vanish — reviving them here would fight that
            // and break the whole encounter (this broke TheMachine/Marilith when the blanket revival first shipped).
            if (ScriptedEventOwner != null && !string.IsNullOrEmpty(ScriptedEventOwner.DynamicEventID) && !npc.active && npc.life > 0
                && !tsorcRevamp.SelfDeactivatingNPCs.Contains(npc.type))
            {
                npc.active = true;
                npc.timeLeft = int.MaxValue;
            }

            if (ReactiveBlockTimer > 0)
            {
                ReactiveBlockTimer--;
            }

            if (BlockedRecentlyDecayTimer > 0)
            {
                BlockedRecentlyDecayTimer--;
                if (BlockedRecentlyDecayTimer == 0)
                {
                    DecayGuardPressureStack(npc);
                }
            }

            UpdateGuardPressureBehavior(npc);
            UpdateKiteThreatResponse(npc);
            UpdateCombatTempo(npc);

            // Hit-recency clock (drives the large-beast retreat-speed scaling + stale-wander give-up). The hooks
            // below reset it to 0 on a real hit; here it just ages. Capped so it never overflows on a long-lived NPC.
            if (FramesSinceHit < 1000000) FramesSinceHit++;

            UpdatePoise(npc);
            UpdatePreAttackJump(npc);

            // Confusion: the mod's custom AI computes movement toward the player and ignores npc.confused, so a
            // confused enemy still walked straight at you (only showing the "?" emote). Here, after the AI has
            // run, we reverse a confused custom-AI enemy's horizontal movement so it stumbles AWAY instead.
            // Vertical velocity (jumps) is left intact so it still hops terrain — just in the wrong direction.
            // Only applies to NPCs that ran our BasicAI this frame; vanilla-AI NPCs handle Confused on their own.
            if (RunningCustomFighterAI && npc.confused && npc.target >= 0 && npc.target < Main.maxPlayers)
            {
                int away = npc.Center.X < Main.player[npc.target].Center.X ? -1 : 1;
                float speed = Math.Max(Math.Abs(npc.velocity.X), 1.5f);
                npc.velocity.X = away * speed;
                npc.direction = away;
                npc.spriteDirection = away;
            }
            RunningCustomFighterAI = false;

            if (StandoffDistance > 0 && npc.HasValidTarget && npc.velocity.Y == 0)
            {
                Player standoffTarget = Main.player[npc.target];
                if (Collision.CanHitLine(npc.position, npc.width, npc.height, standoffTarget.position, standoffTarget.width, standoffTarget.height)
                    && Math.Abs(npc.Center.X - standoffTarget.Center.X) < StandoffDistance * 16f)
                {
                    npc.velocity.X = 0;
                }
            }

            if (CanHealAllies && Main.rand.NextBool(HealAlliesChance) && Main.netMode != NetmodeID.MultiplayerClient)
            {
                float healRange = HealAlliesRange * 16f;
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    NPC ally = Main.npc[i];
                    if (!ally.active || ally.whoAmI == npc.whoAmI || ally.friendly || ally.life <= 0 || ally.life >= ally.lifeMax)
                        continue;
                    if (ally.Distance(npc.Center) > healRange)
                        continue;
                    int heal = Math.Max(1, (int)(ally.lifeMax * (HealAlliesPercent / 100f)));
                    ally.life = Math.Min(ally.life + heal, ally.lifeMax);
                    ally.HealEffect(heal);
                    ally.netUpdate = true;
                    if (Main.netMode != NetmodeID.Server)
                        Projectile.NewProjectile(npc.GetSource_FromAI(), ally.Center, Microsoft.Xna.Framework.Vector2.Zero, ModContent.ProjectileType<Projectiles.VFX.HealSpriteVFX>(), 0, 0);
                }
            }

            if (CanSelfHeal && Main.rand.NextBool(SelfHealChance))
            {
                npc.life = Math.Min(npc.life + SelfHealAmount, npc.lifeMax);
                npc.HealEffect(SelfHealAmount);
                npc.netUpdate = true;
                if (Main.netMode != NetmodeID.Server)
                    Projectile.NewProjectile(npc.GetSource_FromAI(), npc.Center, Microsoft.Xna.Framework.Vector2.Zero, ModContent.ProjectileType<Projectiles.VFX.HealSpriteVFX>(), 0, 0);
            }

            if (CanGoInvisible)
            {
                UpdateInvisibility(npc);
            }

            if (CanGoInvisible || HasGhostAfterimages || EvasiveAfterimagesActive)
            {
                UpdateInvisibilityTrail(npc);
            }

            // Committed jumps keep their takeoff facing and travel direction through the first landing,
            // then decelerate before normal pursuit is permitted to turn the fighter around.
            UpdateCommittedJumpFacing(npc);

            // Stagger freeze: override the AI's movement so a staggered enemy is stunned in place. Last write to
            // velocity in the common PostAI path so it wins over pursuit/confusion/standoff set above.
            ApplyStaggerMovement(npc);

            if (!CanPassThroughWalls)
                return;

            // ── Ghost wall teleport ───────────────────────────────────────────────
            // When blocked by a solid wall for ~25 frames, scan for a valid landing
            // spot on the far side and instantly relocate there.
            //
            // BUG FIX: the original code required onGround (velocity.Y == 0) before
            // accumulating GhostWallTimer.  FighterAI's jump code fires every few
            // frames against an impassable wall, setting velocity.Y < 0, which made
            // onGround false and reset the timer to 0 every cycle — the threshold was
            // never reached and the teleport (and its dust) never fired.
            // Fix: accumulate whenever a wall tile is present in the forward column,
            // regardless of vertical velocity.  The timer drains twice as fast when
            // the column is clear so normal single-frame wall-grazes don't trigger.
            //
            // Additional guard: require the NPC to actually be moving forward (velocity in
            // its facing direction > 0.2 px/tick).  Without this, enemies with custom AI
            // that decelerate to 0 during attacks (e.g. GhostOfAHollowWarrior's slash) could
            // silently accumulate the timer and teleport mid-animation.
            float _ghostFwdVel = npc.direction * npc.velocity.X;
            bool wallBlocked = _ghostFwdVel > 0.2f && IsWallBlockingAhead(npc);

            if (wallBlocked)
                GhostWallTimer++;
            else
                GhostWallTimer = Math.Max(0, GhostWallTimer - 2);

            if (GhostWallTimer >= 1)
            {
                // Only queue if no teleport is already in progress.
                if (TeleportCountdown == 0 && TeleportAppearanceTimer == 0 && TryGhostWallTeleport(npc))
                    ProjectileTimer = 0f;
                // Reset regardless — let FighterAI's direction-flip move the NPC
                // away before the next accumulation attempt starts.
                GhostWallTimer = 0;
            }
        }

        // ── Ghost wall teleport helpers ───────────────────────────────────────────

        /// <summary>
        /// Returns true when the tile column immediately in front of the NPC (in its
        /// current facing direction) contains at least one solid tile at body level —
        /// i.e. the NPC is walking into a wall it cannot step over normally.
        /// </summary>
        private static bool IsWallBlockingAhead(NPC npc)
        {
            int dir = npc.direction == 0 ? 1 : npc.direction;
            int frontTileX = dir == -1
                ? (int)(npc.position.X / 16f) - 1
                : (int)((npc.position.X + npc.width) / 16f);
            int feetTileY   = (int)((npc.position.Y + npc.height) / 16f);
            int bodyHtTiles = (int)Math.Ceiling(npc.height / 16.0);

            for (int row = feetTileY - bodyHtTiles; row < feetTileY; row++)
            {
                if (UsefulFunctions.IsTileReallySolid(frontTileX, row))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Scans ahead for a valid open landing spot on the far side of the wall,
        /// teleports the NPC there, and spawns grey smoke at both positions.
        ///
        /// Valid landing: solid floor, PLUS at least 2 tiles wide × 3 tiles tall of clear
        /// air space (landing column AND the next column in the direction of travel).
        /// The 2-wide requirement prevents the NPC landing on a 1-tile ledge next to a
        /// slope or wall where it immediately gets stuck again.
        ///
        /// Scans up to 4 tiles wide (wall thickness), then up to 4 tiles beyond,
        /// checking ±3 tile Y variation to handle steps/ramps.
        ///
        /// Returns false if the wall leads into solid earth (no teleport occurs).
        /// </summary>
        private static bool TryGhostWallTeleport(NPC npc)
        {
            int dir = npc.direction == 0 ? 1 : npc.direction;
            int frontTileX  = dir == -1
                ? (int)(npc.position.X / 16f) - 1
                : (int)((npc.position.X + npc.width) / 16f);
            int feetTileY   = (int)((npc.position.Y + npc.height) / 16f);
            int bodyHtTiles = (int)Math.Ceiling(npc.height / 16.0);

            // Minimum vertical clearance: always at least 3 tiles even for short NPCs.
            int minClearance = Math.Max(bodyHtTiles, 3);

            // ── Phase 1: find where the wall ends ────────────────────────────────
            // Scan forward until we find a column where the NPC's body would fit.
            int wallEndX = -1;
            for (int i = 0; i <= GhostWallMaxThicknessTiles; i++)
            {
                int tx = frontTileX + dir * i;
                bool columnClear = true;
                for (int row = feetTileY - bodyHtTiles; row < feetTileY; row++)
                {
                    if (UsefulFunctions.IsTileReallySolid(tx, row))
                    {
                        columnClear = false;
                        break;
                    }
                }
                if (columnClear)
                {
                    if (i == 0) return false; // no actual wall in front (shouldn't happen)
                    wallEndX = tx;
                    break;
                }
            }
            if (wallEndX == -1) return false; // wall > max thickness / solid earth

            // ── Phase 2: find a valid floor at or near wall exit ─────────────────
            // Try the same Y level first, then offset up/down to handle steps/ramps.
            int[] yOffsets = { 0, -1, 1, -2, 2, -3, 3 };
            for (int xi = 0; xi <= 4; xi++)
            {
                int tx = wallEndX + dir * xi;
                foreach (int yOff in yOffsets)
                {
                    int groundTile = feetTileY + yOff;

                    // Floor must be solid at the landing column.
                    if (!UsefulFunctions.IsTileReallySolid(tx, groundTile))
                        continue;

                    // ── 2-wide × 3-tall clearance check ──────────────────────────
                    // Both the landing column (tx) and the adjacent column in the travel
                    // direction (tx + dir) must have minClearance rows of clear air above
                    // the floor.  This stops the NPC landing on a 1-tile ledge next to a
                    // slope/wall where it would immediately get wedged.
                    bool bodyFits = true;
                    int adjX = tx + dir;

                    for (int col = 0; col < 2 && bodyFits; col++)
                    {
                        int checkX = col == 0 ? tx : adjX;
                        for (int row = groundTile - minClearance; row < groundTile; row++)
                        {
                            if (UsefulFunctions.IsTileReallySolid(checkX, row))
                            {
                                bodyFits = false;
                                break;
                            }
                        }
                    }
                    if (!bodyFits) continue;

                    // ── Valid spot found — queue a fast smoke teleport ───────────
                    // Spawn 0.5s smoke at both ends immediately, then let FighterAI's
                    // TeleportAppearanceTimer freeze the NPC and snap it halfway through.
                    Vector2 destPos = new Vector2(
                        tx * 16f + 8f - npc.width  / 2f,
                        groundTile * 16f - npc.height
                    );
                    Vector2 destCenter = destPos + new Vector2(npc.width / 2f, npc.height / 2f);

                    tsorcRevampGlobalNPC gNpc = npc.GetGlobalNPC<tsorcRevampGlobalNPC>();
                    gNpc.TeleportTelegraph = destCenter;
                    gNpc.TeleportCountdown = 0;
                    gNpc.TeleportAppearanceTimer = GhostWallTeleportSnapTicks;
                    npc.netUpdate = true;

                    SoundEngine.PlaySound(SoundID.Item8, npc.Center);

                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        float radius = Math.Max(npc.width, npc.height) * GhostWallTeleportMistRadiusScale;
                        Projectile srcMist = Projectile.NewProjectileDirect(npc.GetSource_FromThis(), npc.Center, Vector2.Zero,
                            ModContent.ProjectileType<TeleportMistLinger>(), 0, 0, Main.myPlayer, 0f, radius);
                        srcMist.timeLeft = GhostWallTeleportSmokeTicks;
                        Projectile dstMist = Projectile.NewProjectileDirect(npc.GetSource_FromThis(), destCenter, Vector2.Zero,
                            ModContent.ProjectileType<TeleportMistLinger>(), 0, 0, Main.myPlayer, 0f, radius);
                        dstMist.timeLeft = GhostWallTeleportSmokeTicks;
                    }

                    return true;
                }
            }

            return false; // far side is solid earth
        }

        public override void SendExtraAI(NPC npc, BitWriter bitWriter, BinaryWriter binaryWriter)
        {
            binaryWriter.Write(DoorBreakProgress);
            binaryWriter.Write(DodgeTimer);
            binaryWriter.Write(ProjectileTimer);
            binaryWriter.Write(TeleportCountdown);
            binaryWriter.Write(TeleportAppearanceTimer);
            binaryWriter.WriteVector2(TeleportTelegraph);
            binaryWriter.Write(IsInvisible);

            binaryWriter.Write(Aggression);
            binaryWriter.Write(Patience);
            binaryWriter.Write(Cowardice);
            binaryWriter.Write(Adeptness);
            binaryWriter.Write(Swiftness);
            binaryWriter.Write(CastingSpeed);
            binaryWriter.Write(Strength);
            binaryWriter.Write(Agility);

            // FSM state — divergence here causes lasting behavioral differences on clients
            binaryWriter.Write((byte)PursuitState);
            binaryWriter.Write(DisengageTimer);
            binaryWriter.WriteVector2(LastKnownPlayerPos);
            binaryWriter.Write(FleeOriginX);
            binaryWriter.Write(FleeDirection);
            binaryWriter.Write(FleeElapsedFrames);
            // Permanent resources — charge counts deplete and never refill, so they must stay in sync
            binaryWriter.Write(TeleportChargesRemaining);
            binaryWriter.Write(AttackIndex);
            binaryWriter.Write(IsTeleportIllusion);
            binaryWriter.Write(TeleportIllusionTimeLeft);
            binaryWriter.Write(BlockedRecently);
            binaryWriter.Write(BlockedDamageRecently);
            binaryWriter.Write(BlockedRecentlyByPlayer);
            binaryWriter.Write(BlockedRecentlyDecayTimer);
            SendGuardPressureBehavior(binaryWriter);
            SendKiteThreat(binaryWriter);
            SendCombatTempo(binaryWriter);
        }

        public override void ReceiveExtraAI(NPC npc, BitReader bitReader, BinaryReader binaryReader)
        {
            DoorBreakProgress = binaryReader.ReadInt32();
            DodgeTimer = binaryReader.ReadInt32();
            ProjectileTimer = binaryReader.ReadSingle();
            TeleportCountdown = binaryReader.ReadInt32();
            TeleportAppearanceTimer = binaryReader.ReadInt32();
            TeleportTelegraph = binaryReader.ReadVector2();
            IsInvisible = binaryReader.ReadBoolean();

            Aggression = binaryReader.ReadSingle();
            Patience = binaryReader.ReadSingle();
            Cowardice = binaryReader.ReadSingle();
            Adeptness = binaryReader.ReadSingle();
            Swiftness = binaryReader.ReadSingle();
            CastingSpeed = binaryReader.ReadSingle();
            Strength = binaryReader.ReadSingle();
            Agility = binaryReader.ReadSingle();

            PursuitState = (PursuitState)binaryReader.ReadByte();
            DisengageTimer = binaryReader.ReadInt32();
            LastKnownPlayerPos = binaryReader.ReadVector2();
            FleeOriginX = binaryReader.ReadSingle();
            FleeDirection = binaryReader.ReadInt32();
            FleeElapsedFrames = binaryReader.ReadInt32();
            TeleportChargesRemaining = binaryReader.ReadInt32();
            AttackIndex = binaryReader.ReadInt32();
            IsTeleportIllusion = binaryReader.ReadBoolean();
            TeleportIllusionTimeLeft = binaryReader.ReadInt32();
            BlockedRecently = binaryReader.ReadInt32();
            BlockedDamageRecently = binaryReader.ReadInt32();
            BlockedRecentlyByPlayer = binaryReader.ReadInt32();
            BlockedRecentlyDecayTimer = binaryReader.ReadInt32();
            ReceiveGuardPressureBehavior(binaryReader);
            ReceiveKiteThreat(binaryReader);
            ReceiveCombatTempo(binaryReader);
        }

        public override void ModifyNPCLoot(NPC npc, NPCLoot npcLoot)
        {
            if (npc.type == NPCID.KingSlime)
            {
                npcLoot.RemoveWhere(rule => rule is CommonDrop drop && (drop.itemId == ItemID.NinjaHood || drop.itemId == ItemID.NinjaShirt || drop.itemId == ItemID.NinjaPants));
                npcLoot.Add(ItemDropRule.ByCondition(tsorcRevamp.tsorcItemDropRuleConditions.NonExpertFirstKillRule, ModContent.ItemType<StaminaVessel>()));
                npcLoot.Add(ItemDropRule.ByCondition(tsorcRevamp.tsorcItemDropRuleConditions.CursedRule, ModContent.ItemType<Lifegem>()));
            }
            if (npc.type == NPCID.EyeofCthulhu)
            {
                npcLoot.Add(ItemDropRule.ByCondition(tsorcRevamp.tsorcItemDropRuleConditions.NonExpertFirstKillRule, ModContent.ItemType<StaminaVessel>()));
            }
            if (npc.type == NPCID.BrainofCthulhu)
            {
                npcLoot.Add(ItemDropRule.ByCondition(tsorcRevamp.tsorcItemDropRuleConditions.NonExpertFirstKillRule, ModContent.ItemType<StaminaVessel>()));
                npcLoot.Add(ItemDropRule.ByCondition(tsorcRevamp.tsorcItemDropRuleConditions.CursedRule, ModContent.ItemType<Lifegem>(), 1, 1, 2));
            }
            if (npc.type == NPCID.QueenSlimeBoss)
            {
                npcLoot.Add(ItemDropRule.ByCondition(tsorcRevamp.tsorcItemDropRuleConditions.CursedRule, ModContent.ItemType<Lifegem>(), 1, 4, 8));
            }
            if (npc.type == NPCID.Plantera)
            {
                npcLoot.Add(ItemDropRule.ByCondition(tsorcRevamp.tsorcItemDropRuleConditions.CursedRule, ModContent.ItemType<RadiantLifegem>(), 1, 1, 2));
            }
            if (npc.type == NPCID.DukeFishron)
            {
                npcLoot.Add(ItemDropRule.ByCondition(tsorcRevamp.tsorcItemDropRuleConditions.NonExpertFirstKillRule, ModContent.ItemType<StaminaVessel>()));
                npcLoot.Add(ItemDropRule.ByCondition(tsorcRevamp.tsorcItemDropRuleConditions.CursedRule, ModContent.ItemType<RadiantLifegem>(), 1, 3, 6));
            }
            if (npc.type == NPCID.Golem)
            {
                npcLoot.Add(ItemDropRule.ByCondition(tsorcRevamp.tsorcItemDropRuleConditions.CursedRule, ModContent.ItemType<RadiantLifegem>(), 1, 2, 4));
            }
            if (npc.type == NPCID.HallowBoss)
            {
                npcLoot.Add(ItemDropRule.ByCondition(tsorcRevamp.tsorcItemDropRuleConditions.CursedRule, ModContent.ItemType<RadiantLifegem>(), 1, 3, 6));
                npcLoot.Add(ItemDropRule.ByCondition(tsorcRevamp.tsorcItemDropRuleConditions.CursedRule, ModContent.ItemType<StarlightShard>(), 1, 3, 6));
            }
            if (npc.type == NPCID.CultistBoss)
            {
                npcLoot.Add(ItemDropRule.ByCondition(tsorcRevamp.tsorcItemDropRuleConditions.CursedRule, ModContent.ItemType<StarlightShard>(), 1, 4, 8));
            }
            if (npc.type == NPCID.MoonLordCore)
            {
                npcLoot.Add(ItemDropRule.ByCondition(tsorcRevamp.tsorcItemDropRuleConditions.CursedRule, ModContent.ItemType<RadiantLifegem>(), 1, 5, 10));
                npcLoot.Add(ItemDropRule.ByCondition(tsorcRevamp.tsorcItemDropRuleConditions.CursedRule, ModContent.ItemType<StarlightShard>(), 1, 5, 10));
            }
        }
        public override void EditSpawnPool(IDictionary<int, float> pool, NPCSpawnInfo spawnInfo)
        {
            int playerX = (int)(Main.LocalPlayer.Center.X / 16f);
            int playerY = (int)(Main.LocalPlayer.Center.Y / 16f);
            Player player = spawnInfo.Player;

            //Human enemies can't swim — never let them spawn in water, regardless of their own SpawnChance.
            if (spawnInfo.Water)
            {
                foreach (int humanType in tsorcRevamp.HumanNPCs)
                {
                    pool.Remove(humanType);
                }
            }

            //VANILLA AND SOME MOD NPC SPAWN EDITS

            //PRE-HARD MODE

            // Arazium's Mountain Caverns (not in water)
            if (!spawnInfo.Water && (spawnInfo.Player.ZoneSkyHeight || spawnInfo.Player.ZoneOverworldHeight) && (Main.tile[spawnInfo.SpawnTileX, spawnInfo.SpawnTileY].WallType == WallID.DirtUnsafe1 || Main.tile[spawnInfo.SpawnTileX, spawnInfo.SpawnTileY].WallType == WallID.DirtUnsafe2) && !Main.hardMode)
            {
                pool.Add(NPCID.Skeleton, 0.02f);
            }

            //jungle
            if (spawnInfo.Player.ZoneJungle && !Main.hardMode)
            {
                //pool.Add(the type of the npc, what chance you want it to spawn with);
                pool.Add(NPCID.LostGirl, 0.005f);
                pool.Add(NPCID.Salamander2, 0.03f);
            }
            //corrupt (not in water)
            if (spawnInfo.Player.ZoneCorrupt && !spawnInfo.Water && !Main.hardMode)
            {
                pool.Add(NPCID.CochinealBeetle, 0.02f);
                pool.Add(NPCID.GiantShelly, 0.02f);
            }
            //corrupt (in water)
            if (spawnInfo.Player.ZoneCorrupt && spawnInfo.Water && !Main.hardMode)
            {
                pool.Add(NPCID.Squid, 0.02f);
            }
            //crimson
            if (spawnInfo.Player.ZoneCrimson && !Main.hardMode)
            {
                pool.Add(NPCID.LacBeetle, 0.02f);
                pool.Add(NPCID.Drippler, 0.2f);
                pool.Add(NPCID.BloodCrawler, 0.002f);
                pool.Add(NPCID.BloodCrawlerWall, 0.002f);
            }
            //meteor
            if (spawnInfo.Player.ZoneMeteor && !Main.hardMode)
            {
                pool.Add(NPCID.GraniteFlyer, 0.4f);
                pool.Add(NPCID.Salamander4, 0.4f);
                pool.Add(NPCID.MeteorHead, 0.01f);
            }
            //graveyard
            if (spawnInfo.Player.ZoneGraveyard && !Main.hardMode)
            {
                pool.Add(NPCID.BigMisassembledSkeleton, 0.03f);
                pool.Add(NPCID.BoneThrowingSkeleton2, 0.03f);
            }

            //HARD MODE SECTION

            //golem temple
            if (spawnInfo.SpawnTileType == TileID.LihzahrdBrick && spawnInfo.Lihzahrd && Main.hardMode)
            {
                pool.Add(NPCID.DesertDjinn, 0.075f);
                pool.Add(NPCID.DiabolistWhite, 0.02f); //was 0.1
                pool.Add(ModContent.NPCType<Enemies.LothricSpearKnight>(), 0.08f);
                pool.Add(ModContent.NPCType<Enemies.LothricKnight>(), 0.08f);

            }

            //desert or underground desert and dungeon(shadow temple)
            if ((spawnInfo.Player.ZoneDesert || spawnInfo.Player.ZoneUndergroundDesert) && spawnInfo.Player.ZoneDungeon && Main.hardMode)
            {
                pool.Add(NPCID.DiabolistRed, 0.01f);
            }

            //machine temple (in water)
            //(depth gate is legacy 2000-space tile-Y; MapTileY shifts it on the expanded world, identity elsewhere)
            if (spawnInfo.Water && playerY < ExpandedWorldTransform.MapTileY(4615, 1430) && Main.tile[spawnInfo.SpawnTileX, spawnInfo.SpawnTileY].WallType == 98 && Main.hardMode)
            {            
                // 98 = WallID.GreenDungeonSlabUnsafe
                
                    // Clear the existing pool to ensure no other NPCs can spawn
                    pool.Clear();

                    // Add specific NPCs back into the pool with their spawn weights
                    pool.Add(NPCID.GreenJellyfish, 10f);                  
                    pool.Add(ModContent.NPCType<Enemies.MutantToad>(), 2f);
                    pool.Add(ModContent.NPCType<Enemies.GhostFighter.GhostOfTheDrowned>(), 2f);



            }
            //machine temple (not in water)
            if (!spawnInfo.Water && playerY < ExpandedWorldTransform.MapTileY(4615, 1430) && Main.tile[spawnInfo.SpawnTileX, spawnInfo.SpawnTileY].WallType == 98 && Main.hardMode)
            {
                pool.Clear();
                pool.Add(ModContent.NPCType<Enemies.GhostFighter.GhostOfTheDrowned>(), 3f);
                pool.Add(ModContent.NPCType<Enemies.MutantToad>(), 1f);

            }
            //sky
            if (spawnInfo.Player.ZoneSkyHeight && Main.hardMode)
            {
                pool.Add(NPCID.GoblinSummoner, 0.01f);
            }
            if (spawnInfo.Player.ZoneHallow && (spawnInfo.Player.ZoneDirtLayerHeight || spawnInfo.Player.ZoneRockLayerHeight) && Main.hardMode)
            {
                pool.Add(NPCID.Gastropod, 0.18f);
            }
            if (spawnInfo.Player.ZoneHallow && (spawnInfo.Player.ZoneDirtLayerHeight || spawnInfo.Player.ZoneRockLayerHeight) && Main.hardMode && !tsorcRevampWorld.SuperHardMode)
            {
                pool.Add(NPCID.Pixie, 0.40f);
                pool.Add(NPCID.Unicorn, 0.09f);
                pool.Add(NPCID.RainbowSlime, 0.01f);
            }
            //ocean water (outer thirds of the map)
            if (spawnInfo.Water && Main.hardMode && (Math.Abs(spawnInfo.SpawnTileX - Main.spawnTileX) > Main.maxTilesX / 3))
            {
                pool.Add(NPCID.SandsharkHallow, 0.3f);
            }

            if (spawnInfo.Player.ZoneJungle && spawnInfo.Player.ZoneRockLayerHeight && Main.hardMode)
            {
                pool.Add(NPCID.Derpling, 0.25f);
                pool.Add(NPCID.GiantFlyingFox, 0.25f);
            }

            //SUPER HARD MODE SECTION
            if (spawnInfo.Player.ZoneJungle && tsorcRevampWorld.SuperHardMode)
            {
                pool.Add(NPCID.BoneLee, 0.05f);
            }

            //mushroom
            if (spawnInfo.Player.ZoneGlowshroom && tsorcRevampWorld.SuperHardMode) 
            {
                pool.Add(NPCID.StardustWormHead, 0.1f); //.1 is 3% 
                pool.Add(NPCID.StardustCellBig, 0.02f); //.5 is 16%
                pool.Add(NPCID.StardustJellyfishBig, 0.3f);
                pool.Add(NPCID.StardustSpiderBig, 0.6f);
                pool.Add(NPCID.StardustSoldier, 1f);
                pool.Add(NPCID.ShimmerSlime, 0.25f);
            }
            //underground 
            if (spawnInfo.Player.ZoneUnderworldHeight && !spawnInfo.Player.ZoneDungeon && tsorcRevampWorld.SuperHardMode)
            {
                pool.Add(NPCID.SolarCrawltipedeHead, 0.002f);
                pool.Add(NPCID.SolarSroller, 0.4f); //.5 is 16%
                pool.Add(NPCID.SolarCorite, 0.01f);
                pool.Add(NPCID.SolarSpearman, 0.4f);
                pool.Add(NPCID.SolarDrakomire, 0.4f);
                pool.Add(NPCID.SolarSolenian, 0.6f); 
            }
            //dungeon (rare)
            if (spawnInfo.Player.ZoneDungeon && tsorcRevampWorld.SuperHardMode)
            {
                pool.Add(ModContent.NPCType<Enemies.SuperHardMode.KnightOfGwyn>(), 0.01f);
            }
            //catacombs
            if (spawnInfo.SpawnTileType == TileID.BoneBlock && tsorcRevampWorld.SuperHardMode)
            {
                pool.Add(NPCID.NebulaBrain, 0.2f); //.1 is 3%
                pool.Add(NPCID.NebulaHeadcrab, 0.4f); //.1 is 3%
                pool.Add(NPCID.NebulaSoldier, 0.4f); //.1 is 3%
            }
            //spaceships or flesh background of crimson biome
            if ((spawnInfo.SpawnTileType == TileID.MartianConduitPlating || Main.tile[spawnInfo.SpawnTileX, spawnInfo.SpawnTileY].WallType == WallID.Flesh || spawnInfo.Player.ZoneCrimson) && tsorcRevampWorld.SuperHardMode)
            {
                pool.Add(NPCID.VortexLarva, 2f); //.1 is 3%
            }
            // molten sky temple
            if (spawnInfo.Player.ZoneUnderworldHeight && (spawnInfo.SpawnTileType == TileID.MeteoriteBrick || spawnInfo.SpawnTileType == TileID.HeavenforgeBrick || Main.tile[spawnInfo.SpawnTileX, spawnInfo.SpawnTileY].WallType == WallID.HeavenforgeBrickWall) && tsorcRevampWorld.SuperHardMode)
            {
                pool.Add(NPCID.VortexRifleman, 0.1f); //.1 is 3%
                pool.Add(NPCID.VortexHornet, 0.02f); //.5 is 16%
                pool.Add(NPCID.VortexSoldier, 0.3f);
                pool.Add(NPCID.Paladin, 0.6f);
            }
            if (tsorcRevampWorld.RemixMap) // If it is Remix Map
            {
                // wyvern mage prison (remix map)
                if (spawnInfo.Player.ZoneMeteor && (spawnInfo.Player.ZoneSkyHeight || spawnInfo.Player.ZoneOverworldHeight) && spawnInfo.Player.ZoneCorrupt && tsorcRevampWorld.SuperHardMode)
                {
                    pool.Add(NPCID.SolarCorite, 0.35f);
                }
                // great foundry (remix map)
                if ((spawnInfo.SpawnTileType == TileID.Cog || Main.tile[spawnInfo.SpawnTileX, spawnInfo.SpawnTileY].WallType == WallID.TinPlating) && tsorcRevampWorld.SuperHardMode)
                {
                    pool.Add(NPCID.SolarSolenian, 0.2f);
                    pool.Add(NPCID.HellArmoredBones, 0.1f);
                    pool.Add(NPCID.HellArmoredBonesSpikeShield, 0.1f);
                    pool.Add(NPCID.HellArmoredBonesMace, 0.1f);
                    pool.Add(NPCID.HellArmoredBonesSword, 0.1f);
                }

                if ((spawnInfo.Water && spawnInfo.SpawnTileType == TileID.Coralstone || spawnInfo.SpawnTileType == TileID.ReefBlock || Main.tile[spawnInfo.SpawnTileX, spawnInfo.SpawnTileY].WallType == WallID.HallowUnsafe2) && Main.hardMode)
                {
                    pool.Add(NPCID.CreatureFromTheDeep, 0.6f);
                    pool.Add(NPCID.Shark, 0.6f);
                }
            }            

            bool invasion = Main.invasionType != 0;
            if (!tsorcRevampWorld.SuperHardMode && (player.Center.X > 82016 || player.Center.X < 74560 || player.Center.Y > 16000))
            {
                invasion = false;
            }

            if (spawnInfo.Player.ZoneTowerSolar || spawnInfo.Player.ZoneTowerNebula || spawnInfo.Player.ZoneTowerStardust || spawnInfo.Player.ZoneTowerVortex || spawnInfo.Player.ZoneOldOneArmy || invasion)
            {
                List<int> blockedNPCs = new List<int>();

                foreach (int id in pool.Keys)
                {
                    ModNPC modNPC = NPCLoader.GetNPC(id);

                    if (modNPC != null && modNPC.Mod == ModLoader.GetMod("tsorcRevamp"))
                    {
                        blockedNPCs.Add(id);
                    }
                }

                foreach (int id in blockedNPCs)
                {
                    pool.Remove(id);
                }
            }

            if (Main.tile[(int)player.position.X / 16, (int)player.position.Y / 16].WallType == WallID.StarlitHeavenWallpaper)
            {
                pool.Clear();
                pool.Add(ModContent.NPCType<Enemies.HumanityPhantom>(), 10f);
            }

            if ((playerX > 6083 && playerX < 6847 && playerY > 1664 && playerY < 1999) && Main.tile[(int)player.position.X / 16, (int)player.position.Y / 16].WallType == WallID.ObsidianBrickUnsafe && tsorcRevampWorld.RemixMap)
            {
                pool.Clear();
                pool.Add(ModContent.NPCType<Enemies.HumanityPhantom>(), 10f);
            }

            if (tsorcRevampWorld.TheEnd)
            {
                pool.Clear(); //stop NPC spawns in The End 
            }
        }

        public override void EditSpawnRate(Player player, ref int spawnRate, ref int maxSpawns)
        {
            // Added these intermediate variables to fix the spawn rate
            // Without them, the spawn rate changes will mostly be ignored by the conversion back to int
            // on every if statement assignment
            float trueSpawnRate = (float)spawnRate;
            float trueMaxSpawns = (float)maxSpawns;

            //reduces max spawns by 30% and rate by 50% until player exceeds 160 health
            if (player.statLifeMax2 <= 160)
            {
                trueSpawnRate = trueSpawnRate * 1.5f;
                trueMaxSpawns = trueMaxSpawns * 0.7f;
            }
            //reduces max spawns by 20% and spawn rate by 40% after 160 health
            if (player.statLifeMax2 > 160 && player.statLifeMax2 <= 200)
            {
                trueSpawnRate = trueSpawnRate * 1.4f;
                trueMaxSpawns = trueMaxSpawns * 0.8f;
            }
            //reduces max spawns by 10% and spawn rate by 30% from 200-400 health
            if (player.statLifeMax2 > 200 && player.statLifeMax2 <= 400)
            {
                trueSpawnRate = trueSpawnRate * 1.3f;
                trueMaxSpawns = trueMaxSpawns * 0.9f;
            }
            //only reduces spawn rate by 20% above 400 health
            if (player.statLifeMax2 > 400)
            {
                trueSpawnRate = trueSpawnRate * 1.2f;
            }

            if (player.GetModPlayer<tsorcRevampPlayer>().BossZenBuff || player.HasBuff(ModContent.BuffType<Buffs.Bonfire>()))
            {
                trueSpawnRate = 9999999;//Higher is less spawns
                trueMaxSpawns = 0;
            }

            //Peace candles do not activate if there is a) an invasion and b) the player is near the center of the world.
            if ((Main.invasionType == 0 || player.Center.X > 82016 || player.Center.X < 74560 || player.Center.Y > 16000))
            {
                if (player.HasBuff(BuffID.PeaceCandle))
                {
                    trueSpawnRate = 9999999;
                    trueMaxSpawns = 0;
                }
            }
            else
            {
                if (Main.invasionType == 1)
                {
                    player.buffImmune[BuffID.PeaceCandle] = true;
                    player.ZonePeaceCandle = false;
                    trueSpawnRate /= 2;
                    trueMaxSpawns *= 3;
                }
            }

            if (player.ZoneTowerSolar || player.ZoneTowerNebula || player.ZoneTowerStardust || player.ZoneTowerVortex)
            {
                trueSpawnRate /= 2;
                trueMaxSpawns = trueMaxSpawns * 1.5f;
            }

            if (Main.tile[(int)player.position.X / 16, (int)player.position.Y / 16].WallType == WallID.StarlitHeavenWallpaper)
            {
                trueSpawnRate /= 10; //Origin of the Abyss. All spawns blocked other than Humanity Phantoms
            }

            spawnRate = (int)Math.Round(trueSpawnRate);
            maxSpawns = (int)Math.Round(trueMaxSpawns);
        }

        //vanilla npc changes moved to separate file

        public override void OnKill(NPC npc)
        {
            Player LocalPlayer = Main.LocalPlayer;

            if (Main.netMode != NetmodeID.Server && !IsTeleportIllusion)
            {
                if (EncounterPresentationRegistry.TryGetCompletedEncounter(npc, out BossEncounterPresentation encounter))
                {
                    if (encounter.ShowsDefeatBanner)
                    {
                        tsorcRevamp.ShowAnnouncementBanner(
                            EncounterPresentationRegistry.GetBannerText(encounter.DefeatBanner),
                            encounter.TomeColor);
                        SoundEngine.PlaySound(DefeatBannerSound);
                    }
                }
                else if (npc.ModNPC is PuppetNPC puppet && puppet.ShouldAnnounceInvaderDefeat)
                {
                    EncounterBannerStyle style = puppet.InvaderDefeatBanner;
                    Color bannerColor = style == EncounterBannerStyle.GreatInvaderVanquished
                        ? new Color(182, 92, 224)
                        : new Color(238, 143, 55);
                    tsorcRevamp.ShowAnnouncementBanner(EncounterPresentationRegistry.GetBannerText(style), bannerColor);
                    SoundEngine.PlaySound(DefeatBannerSound);
                }
            }

            if (npc.type == NPCID.Golem && ModContent.GetInstance<tsorcRevampConfig>().AdventureMode)
            {
                UsefulFunctions.BroadcastText(LangUtils.GetTextValue("NPCs.EmpressOfLight.Forcefield"), Color.Cyan);
            }
            if (tsorcRevampWorld.RemixMap)
            {
                if ((npc.type == NPCID.LunarTowerVortex || npc.type == NPCID.LunarTowerStardust ||
                    npc.type == NPCID.LunarTowerNebula || npc.type == NPCID.LunarTowerSolar) &&
                    ModContent.GetInstance<tsorcRevampConfig>().AdventureMode)
                {
                    defeatedPillars.Add(npc.type);

                    if (defeatedPillars.Contains(NPCID.LunarTowerVortex) &&
                        defeatedPillars.Contains(NPCID.LunarTowerStardust) &&
                        defeatedPillars.Contains(NPCID.LunarTowerNebula) &&
                        defeatedPillars.Contains(NPCID.LunarTowerSolar))
                    {
                        UsefulFunctions.BroadcastText(LangUtils.GetTextValue("NPCs.PillarsForcefield"), Color.Teal);
                    }
                }
            }
            if (npc.boss)
                {                  
                    foreach (Player player in Main.player)
                    {
                        if (!player.active) { continue; }
                        player.GetModPlayer<tsorcRevampPlayer>().bossMagnet = true;
                        player.GetModPlayer<tsorcRevampPlayer>().bossMagnetTimer = 300; //5 seconds of increased grab range, check GlobalItem::GrabStyle and GrabRange
                    }
                }

            if (!SuppressGlobalOnKillDrops && Main.LocalPlayer.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceCurrent < Main.LocalPlayer.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceMax2)
            {
                if (Main.rand.NextBool(2))
                {
                    Item.NewItem(npc.GetSource_Loot(), npc.getRect(), ModContent.ItemType<Items.StaminaDroplet>(), 1);
                }

                if (Main.rand.NextBool(12))
                {
                    Item.NewItem(npc.GetSource_Loot(), npc.getRect(), ModContent.ItemType<Items.StaminaDroplet>(), 1);
                }
            }

            #region Dark Souls & Consumable Souls Drops

            if (Soulstruck)
            {
                divisorMultiplier = 0.9f; //10% increase
            }

            if (npc.lifeMax > 5 && npc.value >= 10f || npc.boss)
            { //stop zero-value souls from dropping (the 'or boss' is for expert mode support)
                if (Main.masterMode)
                {
                    enemyValue = (int)npc.value / (divisorMultiplier * 20);
                }
                else
                if (Main.expertMode)
                { //npc.value is the amount of coins they drop
                    enemyValue = (int)npc.value / (divisorMultiplier * 25); //all enemies drop more money in expert mode, so the divisor is larger to compensate
                }
                else
                {
                    enemyValue = (int)npc.value / (divisorMultiplier * 15);
                }


                multiplier = tsorcRevampPlayer.CheckSoulsMultiplier(Main.LocalPlayer);

                DarkSoulQuantity = (int)(multiplier * enemyValue);

                #region Bosses drop souls once
                if (npc.boss)
                {
                    if (tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(npc.type)))
                    {
                        DarkSoulQuantity = 0;
                    }
                    else
                    {
                        // check whether the SHM boss was killed
                        if (npc.type == ModContent.NPCType<NPCs.Bosses.SuperHardMode.Fiends.WaterFiendKraken>() || npc.type == ModContent.NPCType<NPCs.Bosses.SuperHardMode.Fiends.FireFiendMarilith>() || npc.type == ModContent.NPCType<NPCs.Bosses.SuperHardMode.Fiends.EarthFiendLich>()
                            || npc.type == ModContent.NPCType<NPCs.Bosses.SuperHardMode.GhostWyvernMage.WyvernMageShadow>() || npc.type == ModContent.NPCType<NPCs.Bosses.SuperHardMode.HellkiteDragon.HellkiteDragonHead>()
                            || npc.type == ModContent.NPCType<NPCs.Bosses.SuperHardMode.OolacileSerpent.GreatSerpentHead>()
                            || npc.type == ModContent.NPCType<NPCs.Bosses.SuperHardMode.Seath.SeathTheScalelessHead>() || npc.type == ModContent.NPCType<NPCs.Bosses.SuperHardMode.AbysmalOolacileSorcerer>()
                            || npc.type == ModContent.NPCType<NPCs.Bosses.SuperHardMode.Artorias>() || npc.type == ModContent.NPCType<NPCs.Bosses.SuperHardMode.Blight>() || npc.type == ModContent.NPCType<NPCs.Bosses.SuperHardMode.Chaos>()
                            || npc.type == ModContent.NPCType<NPCs.Bosses.SuperHardMode.DarkCloud>() || npc.type == ModContent.NPCType<NPCs.Bosses.SuperHardMode.Witchking>()) /*|| npc.type == ModContent.NPCType<NPCs.Bosses.SuperHardMode.Gwyn>()) gwyn CLOSES the abyss portal!*/
                        {
                            UsefulFunctions.BroadcastText(LangUtils.GetTextValue("NPCs.SHM.BossDeath"), Color.Orange); 
                        }

                        if (npc.type == ModContent.NPCType<NPCs.Bosses.SuperHardMode.HellkiteDragon.HellkiteDragonHead>())
                        {
                            tsorcRevampWorld.isHellkiteDragonDead = true;
                        }

                        if (npc.type == ModContent.NPCType<NPCs.Bosses.SuperHardMode.OolacileSerpent.GreatSerpentHead>())
                        {
                            tsorcRevampWorld.isOolacileSerpentDead = true;
                        }

                        if (((npc.type == NPCID.EaterofWorldsHead) || (npc.type == NPCID.EaterofWorldsBody) || (npc.type == NPCID.EaterofWorldsTail)) && Main.invasionType == 0)
                        {
                            Main.StartInvasion();
                        }

                        if ((npc.type == ModContent.NPCType<NPCs.Bosses.TheSorrow>()) && Main.invasionType == 0)
                        {
                            Main.StartInvasion(3);
                        }

                        tsorcRevampWorld.PopulatePairedBosses();
                        //Paired bosses have to have their slain entries work different
                        if (tsorcRevampWorld.PairedBosses.Contains(npc.type))
                        {
                            for (int i = 0; i < tsorcRevampWorld.PairedBosses.Count; i++)
                            {
                                if (tsorcRevampWorld.PairedBosses[i] == npc.type)
                                {
                                    int pairedNPCOffset = -1;
                                    if (i % 2 == 0)
                                    {
                                        pairedNPCOffset = 1;
                                    }

                                    //If the other boss is not alive, then add them both. If not, don't.
                                    if (!NPC.AnyNPCs(tsorcRevampWorld.PairedBosses[i + pairedNPCOffset]))
                                    {
                                        tsorcRevampWorld.NewSlain.Add(new NPCDefinition(npc.type), 1);
                                        tsorcRevampWorld.NewSlain.Add(new NPCDefinition(tsorcRevampWorld.PairedBosses[i + pairedNPCOffset]), 1);
                                    }

                                    break;
                                }
                            }
                        }
                        else
                        {
                            tsorcRevampWorld.NewSlain.Add(new NPCDefinition(npc.type), 1);
                        }

                        if (Main.netMode == NetmodeID.Server)
                        {
                            NetMessage.SendData(MessageID.WorldData); //Slain only exists on the server. This tells the server to run NetSend(), which syncs this data with clients
                        }
                    }
                }
                #endregion

                #region EoW drops souls in a unique way
                if (((npc.type == NPCID.EaterofWorldsHead) || (npc.type == NPCID.EaterofWorldsBody) || (npc.type == NPCID.EaterofWorldsTail)))
                {

                    DarkSoulQuantity = 24; //*72 for soul drops per eater, 1728 souls per one whole eater

                    Item.NewItem(npc.GetSource_Loot(), npc.getRect(), ModContent.ItemType<DarkSoul>(), DarkSoulQuantity);
                }
                #endregion

                if (DarkSoulQuantity > 0)
                {
                    Item.NewItem(npc.GetSource_Loot(), npc.getRect(), ModContent.ItemType<DarkSoul>(), DarkSoulQuantity);
                }


                // Consumable Soul drops ahead - Current numbers give aprox. +20% souls

                float chance = 0.01f + (0.0005f * Main.LocalPlayer.GetModPlayer<tsorcRevampPlayer>().ConsSoulChanceMult);
                //Main.NewText(chance);

                if (!(npc.type == NPCID.EaterofWorldsBody || npc.type == NPCID.EaterofWorldsTail || npc.type == NPCID.EaterofWorldsHead || npc.type == NPCID.Creeper))
                {

                    if ((enemyValue >= 1) && (enemyValue <= 200) && (Main.rand.NextFloat() < chance)) // 1% chance of all enemies between enemyValue 1 and 200 dropping FadingSoul aka 1/75
                    {
                        Item.NewItem(npc.GetSource_Loot(), npc.getRect(), ModContent.ItemType<FadingSoul>(), 1); // Zombies and eyes are 6 and 7 enemyValue, so will only drop FadingSoul
                    }

                    if ((enemyValue >= 15) && (enemyValue <= 2000) && (Main.rand.NextFloat() < chance)) // 1% chance of all enemies between enemyValue 10 and 2000 dropping LostUndeadSoul aka 1/75
                    {
                        Item.NewItem(npc.GetSource_Loot(), npc.getRect(), ModContent.ItemType<LostUndeadSoul>(), 1); // Most pre-HM enemies fall into this category
                    }

                    if ((enemyValue >= 55) && (enemyValue <= 10000) && (Main.rand.NextFloat() < chance)) // 1% chance of all enemies between enemyValue 50 and 10000 dropping NamelessSoldierSoul aka 1/75
                    {
                        Item.NewItem(npc.GetSource_Loot(), npc.getRect(), ModContent.ItemType<NamelessSoldierSoul>(), 1); // Most HM enemies fall into this category
                    }

                    if ((enemyValue >= 150) && (enemyValue <= 10000) && (Main.rand.NextFloat() < chance) && Main.hardMode) // 1% chance of all enemies between enemyValue 150 and 10000 dropping ProudKnightSoul aka 1/75
                    {
                        Item.NewItem(npc.GetSource_Loot(), npc.getRect(), ModContent.ItemType<ProudKnightSoul>(), 1);
                    }
                }
                //End consumable souls drops
            }
            #endregion


            #region Event saving and custom drops code

            if (ScriptedEventOwner != null && ScriptedEventOwner.eventNPCs[ScriptedEventIndex].type == npc.type)
            {
                ScriptedEventOwner.eventNPCs[ScriptedEventIndex].killed = true;

                if (ScriptedEventOwner.eventNPCs[ScriptedEventIndex].extraLootItems != null)
                {
                    for (int i = 0; i < ScriptedEventOwner.eventNPCs[ScriptedEventIndex].extraLootItems.Count; i++)
                    {
                        Item.NewItem(npc.GetSource_Loot(), npc.Center, ScriptedEventOwner.eventNPCs[ScriptedEventIndex].extraLootItems[i], ScriptedEventOwner.eventNPCs[ScriptedEventIndex].extraLootAmounts[i]);
                    }
                }

                bool oneAlive = false;
                foreach (EventNPC eventNPC in ScriptedEventOwner.eventNPCs)
                {
                    if (!eventNPC.killed)
                    {
                        oneAlive = true;
                    }
                }

                if (!oneAlive)
                {
                    if (ScriptedEventOwner.FinalNPCCustomDrops != null)
                    {
                        for (int i = 0; i < ScriptedEventOwner.FinalNPCCustomDrops.Count; i++)
                        {
                            Item.NewItem(npc.GetSource_Loot(), npc.Center, ScriptedEventOwner.FinalNPCCustomDrops[i], ScriptedEventOwner.FinalNPCDropAmounts[i]);
                        }
                    }
                }
            }
            #endregion
        }

        public override bool CanHitPlayer(NPC npc, Player target, ref int cooldownSlot)
        {
            if (DodgeTimer > 0 || QuickStepTimer > 0 || CombatMeleeActive || InGuardPressureRecovery) // shared melee/recovery cannot deal passive body-contact damage
            {
                return false;
            }
            return base.CanHitPlayer(npc, target, ref cooldownSlot);
        }

        public override void ModifyIncomingHit(NPC npc, ref NPC.HitModifiers modifiers)
        {
            if (npc.HasBuff(BuffID.BetsysCurse) && tsorcRevampPlayer.DragonStonePotency)
            {
                modifiers.Defense -= 40 * DragonStone.Potency - 40;
            }
            if (npc.HasBuff(BuffID.Ichor) && tsorcRevampPlayer.DragonStonePotency)
            {
                modifiers.Defense -= 15 * DragonStone.Potency - 15;
            }
            if (Sundered && modifiers.DamageType == DamageClass.Magic)
            {
                modifiers.FinalDamage *= 1f + OrbOfFlame.MagicSunder / 100f;
            }
            if (AbyssalSinking)
            {
                modifiers.FinalDamage *= 1.09f;
            }
            if (Ignited)
            {
                modifiers.FlatBonusDamage += MagmaBreastplate.OnHitDmg;
            }
            if (PlaguesmithBuff)
            {
                modifiers.FinalDamage *= 0.65f; 
            }
            if (Main.player[Main.myPlayer].GetModPlayer<tsorcRevampPlayer>().ConditionOverload)
            {
                float debuffCounter = 0;
                foreach (int buffType in npc.buffType)
                {
                    if (Main.debuff[buffType] && !(BuffID.Sets.IsATagBuff[buffType]))
                    {
                        debuffCounter++;
                    }
                    if (buffType == ModContent.BuffType<MythrilRamDebuff>())
                    {
                        debuffCounter++;
                    }
                    if (buffType == ModContent.BuffType<ScorchingDebuff>())
                    {
                        debuffCounter++;
                    }
                    if (buffType == ModContent.BuffType<ShockedDebuff>())
                    {
                        debuffCounter++;
                    }
                    if (buffType == ModContent.BuffType<SunburnDebuff>())
                    {
                        debuffCounter++;
                    }
                    if (buffType == ModContent.BuffType<Heatstroke>())
                    {
                        debuffCounter++;
                    }
                    if (buffType == ModContent.BuffType<Charmed>())
                    {
                        debuffCounter++;
                    }
                }
                if (debuffCounter > 1)
                {
                    modifiers.FinalDamage += debuffCounter * ConditionOverload.Dmg / 100f;
                }
            }
        }
        public override void ModifyHitByProjectile(NPC npc, Projectile projectile, ref NPC.HitModifiers modifiers)
        {
            // Poise damage scales with the projectile's knockback stat × its per-projectile ProjectilePoiseMultiplier
            // lever (see project_poise_stagger_system).
            float poiseKnockback = projectile.knockBack * projectile.GetGlobalProjectile<tsorcGlobalProjectile>().ProjectilePoiseMultiplier;
            bool magicGhostHit = ShouldMagicKnockbackGhost(npc, projectile);
            if (magicGhostHit)
            {
                poiseKnockback = Math.Max(poiseKnockback, GhostMagicPoiseKnockbackFloor);
                EnableMagicGhostPoise(npc);
            }
            else if (IsPhysicalGhost(npc))
            {
                return;
            }
            ApplyPoiseToKnockback(npc, poiseKnockback, ref modifiers, projectile.Center);

            Player projectileOwner = Main.player[projectile.owner];
            var modPlayerProjectileOwner = Main.player[projectile.owner].GetModPlayer<tsorcRevampPlayer>();
            float SummonTagDamageMultiplier = ProjectileID.Sets.SummonTagDamageMultiplier[projectile.type];
            SummonTagFlatDamage = 0f;
            SummonTagCriticalStrikeChance = 0;
            SummonTagScalingDamage = 0f;
            SummonTagArmorPenetration = 0f;
            #region Individual Whip debuff effects
            #region Modded Whips
            if (markedByRustedChain)
            {
                SummonTagArmorPenetration += RustedChain.SummonTagArmorPen;
            }
            //if(markedByUrumi) has no effect anymore, the whip is focused on dealing damage by itself and does not buff minions
            //if(markedByEnchantedWhip) only has a special effect
            //if(markedByDominatrix) only has a special effect
            //if(markedBySearingLash) has no effect anymore, the whip is focused on dealing damage by itself and does not buff minions
            if (markedByCrystalNunchaku && CrystalNunchakuProc)
            {
                SummonTagScalingDamage += CrystalNunchakuStacks * (CrystalNunchaku.MaxSummonTagScalingDamage / 10f) / 100f;
            }
            if (markedByPyrosulfate)
            {
                SummonTagFlatDamage += Pyrosulfate.SummonTagDamage;
            }
            if (markedByPyromethane)
            {
                SummonTagCriticalStrikeChance += Pyromethane.SummonTagCrit;
            }
            //if(markedByNightsCracker) is used by the whip itself
            //if (markedByPolarisLeash) only has a special effect
            //if (markedByDragoonLash) only has a special effect
            //if(markedByTerraFall) is used by the whip itself
            if (markedByTerraFallShard)
            {
                SummonTagFlatDamage += TerraFallItem.TagDmg;
                SummonTagCriticalStrikeChance += TerraFallItem.TagCrit;
            }
            if (markedByDetonationSignal)
            {
                SummonTagScalingDamage += DetonationSignal.SummonTagScalingDamage / 100f;
            }
            #endregion
            #region Vanilla Whips
            if (markedByLeatherWhip)
            {
                SummonTagFlatDamage += SummonerEdits.LeatherWhipTagDmg;
            }
            if (markedBySnapthorn)
            {
                SummonTagFlatDamage += SummonerEdits.SnapthornTagDmg;
            }
            if (markedBySpinalTap)
            {
                SummonTagFlatDamage += SummonerEdits.SpinalTapTagDmg;
            }
            if (markedByFirecracker)
            {
                SummonTagScalingDamage += SummonerEdits.FirecrackerScalingDmg / 100f;
            }
            if (markedByCoolWhip)
            {
                SummonTagFlatDamage += SummonerEdits.CoolWhipTagDmg;
            }
            if (markedByDurendal)
            {
                SummonTagFlatDamage += SummonerEdits.DurendalTagDmg;
            }
            if (markedByMorningStar)
            {
                SummonTagFlatDamage += SummonerEdits.MorningStarTagDmg;
                SummonTagCriticalStrikeChance += SummonerEdits.MorningStarTagCritChance;
            }
            if (markedByDarkHarvest)
            {
                SummonTagFlatDamage += SummonerEdits.DarkHarvestTagDmg;
            }
            if (markedByKaleidoscope)
            {
                SummonTagFlatDamage += SummonerEdits.KaleidoscopeTagDmg;
                SummonTagCriticalStrikeChance += SummonerEdits.KaleidoscopeTagCritChance;
            }
            #endregion
            #endregion
            #region Summon Tag Damage Calculation and Special Effects
            if (projectile.IsMinionOrSentryRelated)
            {
                #region Minion effects
                foreach (int buffType in npc.buffType)
                {
                    List<int> NonWhipTagBuffs = new List<int>()
                    {
                        ModContent.BuffType<MythrilRamDebuff>(),
                        ModContent.BuffType<ScorchingDebuff>(),
                        ModContent.BuffType<ShockedDebuff>(),
                        ModContent.BuffType<SunburnDebuff>(),
                        ModContent.BuffType<Heatstroke>(),
                        ModContent.BuffType<Charmed>()
                    };
                    if (BuffID.Sets.IsATagBuff[buffType] && !NonWhipTagBuffs.Contains(buffType))
                    {
                        SummonTagFlatDamage += modPlayerProjectileOwner.EncouragingSummonTagDmg;
                        break;
                    }
                }
                /*if (((Scorched || Shocked || Sunburnt) && (SuperScorchDuration > 0 || SuperShockDuration > 0 || SuperSunburnDuration > 0)) || Awestruck)
                {
                    SummonTagCriticalStrikeChance += ScorchingPoint.SummonTagCrit;
                }*/
                #endregion
                #region Modded Whip Special Effects
                //Crystal Nunchaku Effect located in ModifyIncomingHit
                //Dragoon Lash effect at the bottom
                if (markedByEnchantedWhip && Main.myPlayer == projectileOwner.whoAmI)
                {
                    int StarDamage = (int)projectileOwner.GetTotalDamage(DamageClass.SummonMeleeSpeed).ApplyTo(EnchantedWhip.BaseDamage * EnchantedWhip.StarDamageScaling / 100f);
                    Vector2 StarVector1 = new Vector2(Main.rand.Next(641), -800) + npc.Center;
                    Vector2 StarVector2 = new Vector2(-Main.rand.Next(641), -800) + npc.Center;
                    int Move = Main.rand.Next(2);
                    switch (Move)
                    {
                        case 0:
                            {
                                Projectile.NewProjectile(Projectile.GetSource_None(), StarVector1, Vector2.One, ModContent.ProjectileType<EnchantedWhipFallingStar>(), StarDamage, 0, Main.myPlayer, npc.whoAmI);
                                break;
                            }
                            case 1:
                            {
                                Projectile.NewProjectile(Projectile.GetSource_None(), StarVector2, Vector2.One, ModContent.ProjectileType<EnchantedWhipFallingStar>(), StarDamage, 0, Main.myPlayer, npc.whoAmI);
                                break;
                            }
                    }
                }     
                if (markedByDominatrix && Main.myPlayer == projectileOwner.whoAmI)
                {
                    int ThornDamage = (int)projectileOwner.GetTotalDamage(DamageClass.SummonMeleeSpeed).ApplyTo(DominatrixItem.BaseDmg * DominatrixItem.ThornDmgScaling / 100f);
                    Vector2 ThornMovement1 = new Vector2(2, 0);
                    Vector2 ThornMovement2 = new Vector2(-2, 0);
                    Vector2 ThornMovement3 = new Vector2(0, 2);
                    Vector2 ThornMovement4 = new Vector2(0, -2);
                    Vector2 ThornMovement5 = new Vector2(2, 2);
                    Vector2 ThornMovement6 = new Vector2(-2, -2);
                    Vector2 ThornMovement7 = new Vector2(2, -2);
                    Vector2 ThornMovement8 = new Vector2(-2, 2);
                    Projectile.NewProjectile(Projectile.GetSource_None(), npc.Center, ThornMovement1, ModContent.ProjectileType<DominatrixThorn>(), ThornDamage, 0, Main.myPlayer);
                    Projectile.NewProjectile(Projectile.GetSource_None(), npc.Center, ThornMovement2, ModContent.ProjectileType<DominatrixThorn>(), ThornDamage, 0, Main.myPlayer);
                    Projectile.NewProjectile(Projectile.GetSource_None(), npc.Center, ThornMovement3, ModContent.ProjectileType<DominatrixThorn>(), ThornDamage, 0, Main.myPlayer);
                    Projectile.NewProjectile(Projectile.GetSource_None(), npc.Center, ThornMovement4, ModContent.ProjectileType<DominatrixThorn>(), ThornDamage, 0, Main.myPlayer);
                    Projectile.NewProjectile(Projectile.GetSource_None(), npc.Center, ThornMovement5, ModContent.ProjectileType<DominatrixThorn>(), ThornDamage, 0, Main.myPlayer);
                    Projectile.NewProjectile(Projectile.GetSource_None(), npc.Center, ThornMovement6, ModContent.ProjectileType<DominatrixThorn>(), ThornDamage, 0, Main.myPlayer);
                    Projectile.NewProjectile(Projectile.GetSource_None(), npc.Center, ThornMovement7, ModContent.ProjectileType<DominatrixThorn>(), ThornDamage, 0, Main.myPlayer);
                    Projectile.NewProjectile(Projectile.GetSource_None(), npc.Center, ThornMovement8, ModContent.ProjectileType<DominatrixThorn>(), ThornDamage, 0, Main.myPlayer);
                }
                if (markedByPolarisLeash && Main.myPlayer == projectileOwner.whoAmI)
                {
                    int StarDamage = (int)projectileOwner.GetTotalDamage(DamageClass.SummonMeleeSpeed).ApplyTo(PolarisLeashItem.BaseDamage * PolarisLeashItem.StarDamageScaling / 100f);
                    Vector2 StarVector1 = new Vector2(Main.rand.Next(641), -800) + npc.Center;
                    Vector2 StarVector2 = new Vector2(-Main.rand.Next(641), -800) + npc.Center;
                    int Move = Main.rand.Next(2);
                    switch (Move)
                    {
                        case 0:
                            {
                                Projectile.NewProjectile(Projectile.GetSource_None(), StarVector1, Vector2.One, ModContent.ProjectileType<PolarisLeashFallingStar>(), StarDamage, 0, Main.myPlayer, npc.whoAmI);
                                break;
                            }
                        case 1:
                            {
                                Projectile.NewProjectile(Projectile.GetSource_None(), StarVector2, Vector2.One, ModContent.ProjectileType<PolarisLeashFallingStar>(), StarDamage, 0, Main.myPlayer, npc.whoAmI);
                                break;
                            }
                    }
                }
                if (markedByDetonationSignal) //Detonation Signal effect
                {
                    int buffIndex = 0;
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Projectile.NewProjectile(Projectile.GetSource_None(), npc.Top, Vector2.Zero, ProjectileID.DD2ExplosiveTrapT2Explosion, 0, 0, Main.myPlayer);
                    }
                    SoundEngine.PlaySound(SoundID.DD2_ExplosiveTrapExplode with { Volume = 1.5f, PitchVariance = 0.3f }, npc.Top);
                    npc.AddBuff(ModContent.BuffType<DetonationSignalBuff>(), DetonationSignal.BonusContactDamageDuration * 60);
                    foreach (int buffType in npc.buffType)
                    {
                        if (buffType == ModContent.BuffType<DetonationSignalDebuff>())
                        {
                            npc.DelBuff(buffIndex);
                        }
                        buffIndex++;
                    }
                }
                #endregion
                #region Vanilla Whip Special Effects
                if (markedByFirecracker)
                {
                    int buffIndex = 0;
                    if (Main.myPlayer == projectileOwner.whoAmI)
                    {
                        Projectile.NewProjectile(projectile.GetSource_FromThis(), npc.Center, Vector2.Zero, ProjectileID.FireWhipProj, 0, 0f, projectile.owner);
                    }
                    foreach (int buffType in npc.buffType)
                    {
                        if (buffType == ModContent.BuffType<FirecrackerDebuff>())
                        {
                            npc.DelBuff(buffIndex);
                        }
                        buffIndex++;
                    }
                }
                if (markedByDarkHarvest)
                {
                    if (Main.myPlayer == projectileOwner.whoAmI)
                    {
                        Projectile DarkHarvestProj = Projectile.NewProjectileDirect(projectile.GetSource_FromThis(), npc.Center, Vector2.Zero, ProjectileID.ScytheWhipProj, (int)(20f * SummonTagDamageMultiplier), 0f, projectile.owner, 1f, npc.whoAmI);
                    }
                    Projectile.EmitBlackLightningParticles(npc);
                }
                if (markedByKaleidoscope)
                {
                    ParticleOrchestrator.RequestParticleSpawn(clientOnly: false, ParticleOrchestraType.RainbowRodHit, new ParticleOrchestraSettings
                    {
                        PositionInWorld = npc.Center
                    });
                }
                #endregion

                modifiers.FlatBonusDamage += SummonTagFlatDamage * SummonTagDamageMultiplier * modPlayerProjectileOwner.SummonTagStrength;
                modifiers.ScalingBonusDamage += SummonTagScalingDamage * SummonTagDamageMultiplier;
                modifiers.ArmorPenetration += SummonTagArmorPenetration * modPlayerProjectileOwner.SummonTagStrength;
                FinalSummonCriticalStrikeChance = projectile.CritChance + (SummonTagCriticalStrikeChance * modPlayerProjectileOwner.SummonTagStrength);

                modPlayerProjectileOwner.OverCrit((int)FinalSummonCriticalStrikeChance, projectile.DamageType, ref modifiers, out CritColorTier);

            }
            if (markedByDragoonLash && (projectile.IsMinionOrSentryRelated || ProjectileID.Sets.IsAWhip[projectile.type])) //has to be outside of the main if since this is supposed to also be procced on whip-hit
            {
                int WhipDamage = (int)projectileOwner.GetTotalDamage(DamageClass.SummonMeleeSpeed).ApplyTo(DragoonLash.BaseDamage);
                if (projectileOwner.GetModPlayer<tsorcRevampPlayer>().DragoonLashFireBreathTimer >= 1 && Main.myPlayer == projectileOwner.whoAmI)
                {
                    Projectile Fireball = Projectile.NewProjectileDirect(Projectile.GetSource_None(), projectileOwner.Center, (npc.Center - projectileOwner.Center) * 0.1f, ProjectileID.Flamelash, WhipDamage, 1f, Main.myPlayer, 1);
                }
            }
            if (markedBySupremeDragoonLash && (projectile.IsMinionOrSentryRelated || ProjectileID.Sets.IsAWhip[projectile.type])) //has to be outside of the main if since this is supposed to also be procced on whip-hit
            {
                int WhipDamage = (int)projectileOwner.GetTotalDamage(DamageClass.SummonMeleeSpeed).ApplyTo(SupremeDragoonLash.BaseDamage);
                if (projectileOwner.GetModPlayer<tsorcRevampPlayer>().SupremeDragoonLashFireBreathTimer >= 1 && Main.myPlayer == projectileOwner.whoAmI)
                {
                    Projectile RgbFireball = Projectile.NewProjectileDirect(Projectile.GetSource_None(), projectileOwner.Center, (npc.Center - projectileOwner.Center) * 0.1f, ProjectileID.RainbowRodBullet, WhipDamage, 1f, Main.myPlayer, 1);
                }
            }
            #endregion

            #region BotC Whip Debuff Damage Scaling (disabled)
            /*int WhipDebuffCounter = 0;
            if (projectile.IsMinionOrSentryRelated && Main.player[Main.myPlayer].GetModPlayer<tsorcRevampPlayer>().BearerOfTheCurse &&
                  projectile.type != ProjectileID.DD2BallistraProj && projectile.type != ProjectileID.DD2ExplosiveTrapT1Explosion && projectile.type != ProjectileID.DD2ExplosiveTrapT2Explosion
                && projectile.type != ProjectileID.DD2ExplosiveTrapT3Explosion && projectile.type != ProjectileID.DD2FlameBurstTowerT1Shot && projectile.type != ProjectileID.DD2FlameBurstTowerT2Shot
                && projectile.type != ProjectileID.DD2FlameBurstTowerT3Shot | projectile.type != ProjectileID.DD2LightningAuraT1 && projectile.type != ProjectileID.DD2LightningAuraT2
                && projectile.type != ProjectileID.DD2LightningAuraT3 && projectile.type != ProjectileID.HoundiusShootiusFireball && projectile.type != ProjectileID.SpiderEgg && projectile.type != ProjectileID.BabySpider
                && projectile.type != ProjectileID.FrostBlastFriendly && projectile.type != ProjectileID.MoonlordTurretLaser && projectile.type != ProjectileID.RainbowCrystalExplosion
                && projectile.type != ModContent.ProjectileType<GaleForceProjectile>())
            {
                foreach (int buffType in npc.buffType)
                {
                    if (BuffID.Sets.IsAnNPCWhipDebuff[buffType])
                    {
                        WhipDebuffCounter++;
                    }
                }
                if (markedBySearingLash && modPlayerProjectileOwner.SearingLashStacks >= 4f)
                {
                    WhipDebuffCounter++;
                }
                if (markedByNightsCracker && modPlayerProjectileOwner.NightsCrackerStacks >= 4f)
                {
                    WhipDebuffCounter++;
                }
                if (markedByTerraFall && modPlayerProjectileOwner.TerraFallStacks >= 4f)
                {
                    WhipDebuffCounter++;
                }
                if (npc.HasBuff(ModContent.BuffType<ScorchingDebuff>()))
                {
                    WhipDebuffCounter--;
                }
                if (npc.HasBuff(ModContent.BuffType<ShockedDebuff>()))
                {
                    WhipDebuffCounter--;
                }
                if (npc.HasBuff(ModContent.BuffType<SunburnDebuff>()))
                {
                    WhipDebuffCounter--;
                }
                if (WhipDebuffCounter > Darksign.WhipDebuffCounterCap)
                {
                    WhipDebuffCounter = Darksign.WhipDebuffCounterCap;
                }
                modifiers.FinalDamage *= 0.1f + (WhipDebuffCounter * Darksign.MinionDamageReductionDecrease / 100f);
            }*/
            #endregion
            if (npc.type == NPCID.DukeFishron && projectileOwner.wet)
            {
                modifiers.FinalDamage *= 2;
            }
            if (npc.type == NPCID.DukeFishron && projectileOwner.wet && projectile.DamageType == DamageClass.Melee)
            {
                modifiers.FinalDamage *= 2;
            }
        }
        public override void ModifyHitByItem(NPC npc, Player player, Item item, ref NPC.HitModifiers modifiers)
        {
            // Poise damage scales with the weapon's knockback stat × its per-weapon WeaponPoiseMultiplier lever
            // (see project_poise_stagger_system).
            float poiseKnockback = item.knockBack * item.GetGlobalItem<tsorcInstancedGlobalItem>().WeaponPoiseMultiplier;
            bool magicGhostHit = ShouldMagicKnockbackGhost(npc, player, item);
            if (magicGhostHit)
            {
                poiseKnockback = Math.Max(poiseKnockback, GhostMagicPoiseKnockbackFloor);
                EnableMagicGhostPoise(npc);
            }
            else if (IsPhysicalGhost(npc))
            {
                return;
            }
            ApplyPoiseToKnockback(npc, poiseKnockback, ref modifiers, player.Center);

            if (npc.type == NPCID.DukeFishron && player.wet)
            {
                modifiers.FinalDamage *= 4;
            }
            /*if (item.DamageType == DamageClass.SummonMeleeSpeed)
            {
                int critLevel = (int)(Math.Floor(player.GetWeaponCrit(player.HeldItem) / 100f));
                if (Main.rand.Next(1, 101) <= player.GetWeaponCrit(player.HeldItem) - (100 * critLevel))
                {
                    modifiers.SetCrit();
                }
                if (critLevel >= 1)
                {
                    modifiers.SetCrit();
                    if (Main.rand.Next(1, 101) <= player.GetWeaponCrit(player.HeldItem) - (100 * critLevel))
                    {
                        modifiers.CritDamage += 1;
                    }
                }
                if (critLevel > 1)
                {
                    for (int i = 1; i < critLevel; i++)
                    {
                        modifiers.CritDamage += 1;
                    }
                }
            }*/
        }
        private static void TriggerNoLosPursuitBoost(NPC npc, Player player)
        {
            if (player == null || !player.active || player.dead)
            {
                return;
            }

            if (!Collision.CanHitLine(npc.Center, 1, 1, player.Center, 1, 1))
            {
                tsorcRevampGlobalNPC globalNPC = npc.GetGlobalNPC<tsorcRevampGlobalNPC>();
                globalNPC.FighterNoLosPursuitBoostTimer = 180;
            }
        }

        // Any hit refreshes the hit-recency clock and, for a large beast, breaks it out of a stale wander back into
        // pursuit ("if it's hit it should re-engage"). Called from both OnHitBy hooks.
        private void RegisterHitForBeast(NPC npc)
        {
            FramesSinceHit = 0;
            if (RequiresFlatGround)
            {
                BeastStale = false;
                BeastUnreachableFrames = 0;
                if (PursuitState == PursuitState.Patrol)
                {
                    PursuitState = PursuitState.Pursue;
                    DisengageTimer = 0;
                }
            }
        }

        public override void OnHitByItem(NPC npc, Player player, Item item, NPC.HitInfo hit, int damageDone)
        {
            // Must match the poise damage computed in ModifyHitByItem above.
            float poiseKnockback = item.knockBack * item.GetGlobalItem<tsorcInstancedGlobalItem>().WeaponPoiseMultiplier;
            bool magicGhostHit = ShouldMagicKnockbackGhost(npc, player, item);
            if (magicGhostHit)
            {
                poiseKnockback = Math.Max(poiseKnockback, GhostMagicPoiseKnockbackFloor);
            }
            if (magicGhostHit || !IsPhysicalGhost(npc))
            {
                HandlePoiseOnHit(npc, poiseKnockback, player.Center);
            }
            if (magicGhostHit)
            {
                ApplyGhostMagicFlinch(npc, player.Center);
            }
            RestoreMagicGhostKnockback(npc);

            TriggerNoLosPursuitBoost(npc, player);
            RegisterHitForBeast(npc);
            RegisterKiteThreatFromItem(npc, player);
            RegisterCombatTempoDamage(npc, damageDone);

            //If this hit takes it below 1/5th health, roll a chance to flee based on its Cowardice trait
            if (npc.life > npc.lifeMax / 5 && npc.life - damageDone < npc.lifeMax / 5)
            {
                if (Main.rand.NextFloat() < npc.GetGlobalNPC<tsorcRevampGlobalNPC>().Cowardice && !npc.boss)
                {
                    Fleeing = true;
                }
            }

            if (!CrystalNunchakuProc && CrystalNunchakuStacks > 0 && markedByCrystalNunchaku)
            {
                CrystalNunchakuStacks -= 1;
            }
        }
        public override void OnHitByProjectile(NPC npc, Projectile projectile, NPC.HitInfo hit, int damageDone)
        {
            // Must match the poise damage computed in ModifyHitByProjectile above.
            float poiseKnockback = projectile.knockBack * projectile.GetGlobalProjectile<tsorcGlobalProjectile>().ProjectilePoiseMultiplier;
            bool magicGhostHit = ShouldMagicKnockbackGhost(npc, projectile);
            if (magicGhostHit)
            {
                poiseKnockback = Math.Max(poiseKnockback, GhostMagicPoiseKnockbackFloor);
            }
            if (magicGhostHit || !IsPhysicalGhost(npc))
            {
                HandlePoiseOnHit(npc, poiseKnockback, projectile.Center);
            }
            if (magicGhostHit)
            {
                ApplyGhostMagicFlinch(npc, projectile.Center);
            }
            RestoreMagicGhostKnockback(npc);

            if (projectile.owner >= 0 && projectile.owner < Main.maxPlayers)
            {
                TriggerNoLosPursuitBoost(npc, Main.player[projectile.owner]);
            }
            RegisterHitForBeast(npc);
            bool distantProjectileThreat = RegisterKiteThreatFromProjectile(npc, projectile);
            RegisterCombatTempoDamage(npc, damageDone);

            if (projectile.friendly && (projectile.DamageType != DamageClass.Melee || distantProjectileThreat))
            {
                tsorcRevampGlobalNPC hitGlobalNPC = npc.GetGlobalNPC<tsorcRevampGlobalNPC>();
                // Preserve the legacy ranged-evasion hand-off flag for non-melee DamageClass hits. A distant
                // melee-class projectile still breaks the pause so pursuit can begin, but remains a melee hit for
                // the enemy's separately-authored EvasiveOnHit response.
                if (projectile.DamageType != DamageClass.Melee)
                {
                    hitGlobalNPC.FighterRangedHitInterruptedPause = hitGlobalNPC.FighterPostAttackPauseTimer > 0
                                                                  || hitGlobalNPC.FighterRangedStandShotsRemaining > 0;
                }
                hitGlobalNPC.FighterPostAttackPauseTimer = 0;
                hitGlobalNPC.FighterRangedStandShotsRemaining = 0;
            }

            Player player = Main.player[projectile.owner];
            var modPlayer = Main.player[projectile.owner].GetModPlayer<tsorcRevampPlayer>();
            if (projectile.IsMinionOrSentryRelated)
            {
                modPlayer.CustomCombatText(npc.Hitbox, damageDone, CritColorTier, hit.Crit);
            }
            //If this hit takes it below 1/5th health, roll a chance to flee based on its Cowardice trait
            if (npc.life > npc.lifeMax / 5 && npc.life - damageDone < npc.lifeMax / 5)
            {
                if (Main.rand.NextFloat() < npc.GetGlobalNPC<tsorcRevampGlobalNPC>().Cowardice && !npc.boss)
                {
                    Fleeing = true;
                }
            }

            #region Vanilla Whips applying their modded counterparts
            if (projectile.type == ProjectileID.BlandWhip)
            {
                npc.AddBuff(ModContent.BuffType<LeatherWhipDebuff>(), (int)(ModdedWhipProjectile.DefaultWhipDebuffDuration * 60 * modPlayer.SummonTagDuration));
            }
            if (projectile.type == ProjectileID.ThornWhip)
            {
                npc.AddBuff(ModContent.BuffType<SnapthornDebuff>(), (int)(ModdedWhipProjectile.DefaultWhipDebuffDuration * 60 * modPlayer.SummonTagDuration));
                player.AddBuff(BuffID.ThornWhipPlayerBuff, (int)(ModdedWhipProjectile.DefaultWhipBuffDuration * 60 * modPlayer.SummonTagDuration));
            }
            if (projectile.type == ProjectileID.BoneWhip)
            {
                npc.AddBuff(ModContent.BuffType<SpinalTapDebuff>(), (int)(ModdedWhipProjectile.DefaultWhipDebuffDuration * 60 * modPlayer.SummonTagDuration));
            }
            if (projectile.type == ProjectileID.FireWhip)
            {
                npc.AddBuff(ModContent.BuffType<FirecrackerDebuff>(), (int)(ModdedWhipProjectile.DefaultWhipDebuffDuration * 60 * modPlayer.SummonTagDuration));
            }
            if (projectile.type == ProjectileID.CoolWhip)
            {
                npc.AddBuff(ModContent.BuffType<CoolWhipDebuff>(), (int)(ModdedWhipProjectile.DefaultWhipDebuffDuration * 60 * modPlayer.SummonTagDuration));
                player.AddBuff(BuffID.CoolWhipPlayerBuff, (int)(ModdedWhipProjectile.DefaultWhipBuffDuration * 60 * modPlayer.SummonTagDuration));
            }
            if (projectile.type == ProjectileID.SwordWhip)
            {
                npc.AddBuff(ModContent.BuffType<DurendalDebuff>(), (int)(ModdedWhipProjectile.DefaultWhipDebuffDuration * 60 * modPlayer.SummonTagDuration));
                player.AddBuff(BuffID.SwordWhipPlayerBuff, (int)(ModdedWhipProjectile.DefaultWhipBuffDuration * 60 * modPlayer.SummonTagDuration));
            }
            if (projectile.type == ProjectileID.MaceWhip)
            {
                npc.AddBuff(ModContent.BuffType<MorningStarDebuff>(), (int)(ModdedWhipProjectile.DefaultWhipDebuffDuration * 60 * modPlayer.SummonTagDuration));
            }
            if (projectile.type == ProjectileID.ScytheWhip)
            {
                npc.AddBuff(ModContent.BuffType<DarkHarvestDebuff>(), (int)(ModdedWhipProjectile.DefaultWhipDebuffDuration * 60 * modPlayer.SummonTagDuration));
                player.AddBuff(BuffID.ScytheWhipPlayerBuff, (int)(ModdedWhipProjectile.DefaultWhipBuffDuration * 60 * modPlayer.SummonTagDuration));
            }
            if (projectile.type == ProjectileID.RainbowWhip)
            {
                npc.AddBuff(ModContent.BuffType<KaleidoscopeDebuff>(), (int)(ModdedWhipProjectile.DefaultWhipDebuffDuration * 60 * modPlayer.SummonTagDuration));
            }
            #endregion
            #region Crystal Nunchaku effects
            if (!CrystalNunchakuProc && !(CrystalNunchakuStacks == 0) && !projectile.npcProj && !projectile.trap && markedByCrystalNunchaku && !projectile.IsMinionOrSentryRelated)
            {
                CrystalNunchakuStacks -= 1;
            }
            #endregion
            if (hit.DamageType == DamageClass.Ranged && damageDone > npc.life && modPlayer.BoneRing)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Projectile.NewProjectileDirect(projectile.GetSource_FromThis(), Main.rand.NextVector2FromRectangle(npc.Hitbox), projectile.velocity, ProjectileID.Bone, projectile.damage / 3, projectile.knockBack, projectile.owner, ai2: 1);
                    Projectile.NewProjectileDirect(projectile.GetSource_FromThis(), Main.rand.NextVector2FromRectangle(npc.Hitbox), projectile.velocity, ProjectileID.Bone, projectile.damage / 3, projectile.knockBack, projectile.owner, ai2: 1);
                    Projectile.NewProjectileDirect(projectile.GetSource_FromThis(), Main.rand.NextVector2FromRectangle(npc.Hitbox), projectile.velocity, ProjectileID.Bone, projectile.damage / 3, projectile.knockBack, projectile.owner, ai2: 1);
                    Projectile.NewProjectileDirect(projectile.GetSource_FromThis(), Main.rand.NextVector2FromRectangle(npc.Hitbox), projectile.velocity, ProjectileID.Bone, projectile.damage / 3, projectile.knockBack, projectile.owner, ai2: 1);
                }
            }
            #region Ranged Weapons
            if (npc.GetGlobalNPC<tsorcRevampGlobalNPC>().ToxicCatDrain && (projectile.type == ModContent.ProjectileType<ToxicCatDetonator>() || projectile.type == ModContent.ProjectileType<ToxicCatExplosion>()))
            {
                npc.GetGlobalNPC<tsorcRevampGlobalNPC>().ResetToxicCatBlobs = true;
                int tags;

                bool shockwaveCreated = false;
                for (int i = 0; i < 1000; i++)
                {
                    tags = 0;
                    Projectile p = Main.projectile[i];
                    if (p.active && p.type == ModContent.ProjectileType<ToxicCatShot>() && p.ai[0] == 1f && p.timeLeft > 2 && p.ai[1] == npc.whoAmI)
                    {
                        for (int q = 0; q < 1000; q++)
                        {
                            Projectile ñ = Main.projectile[q];
                            if (ñ.active && ñ.type == ModContent.ProjectileType<ToxicCatShot>() && ñ.ai[0] == 1f && ñ.ai[1] == npc.whoAmI)
                            {
                                tags++;
                            }
                        }
                        float volume = (tags * 0.3f) + 0.7f;
                        float pitch = tags * 0.08f;
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item74 with { Volume = volume, Pitch = -pitch }, projectile.Center);

                        p.timeLeft = 2;
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            Projectile.NewProjectile(player.GetSource_FromThis(), p.Center, npc.velocity, ModContent.ProjectileType<ToxicCatExplosion>(), (int)(projectile.damage * 1.8f), tags + 3, projectile.owner, tags, 0);
                        }
                        int buffindex = npc.FindBuffIndex(ModContent.BuffType<Buffs.ToxicCatDrain>());

                        if (buffindex != -1)
                        {
                            npc.DelBuff(buffindex);
                        }
                    }

                    if (tags > 0 && !shockwaveCreated)
                    {
                        shockwaveCreated = true;
                        if (projectile.type == ModContent.ProjectileType<ToxicCatDetonator>() && Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, Vector2.Zero, ModContent.ProjectileType<Projectiles.VFX.ShockwaveEffect>(), 0, 0, Main.myPlayer, 300 * (tags / 12f), 45 * (tags / 12f));
                        }
                    }

                }
            }

            if (npc.GetGlobalNPC<tsorcRevampGlobalNPC>().ViruCatDrain && (projectile.type == ModContent.ProjectileType<VirulentCatDetonator>() || projectile.type == ModContent.ProjectileType<VirulentCatExplosion>()))
            {
                npc.GetGlobalNPC<tsorcRevampGlobalNPC>().ResetViruCatBlobs = true;
                int tags;

                bool shockwaveCreated = false;
                for (int i = 0; i < 1000; i++)
                {
                    tags = 0;
                    Projectile p = Main.projectile[i];
                    if (p.active && p.type == ModContent.ProjectileType<VirulentCatShot>() && p.ai[0] == 1f && p.timeLeft > 2 && p.ai[1] == npc.whoAmI)
                    {
                        for (int q = 0; q < 1000; q++)
                        {
                            Projectile ñ = Main.projectile[q];
                            if (ñ.active && ñ.type == ModContent.ProjectileType<VirulentCatShot>() && ñ.ai[0] == 1f && ñ.ai[1] == npc.whoAmI)
                            {
                                tags++;
                            }
                        }
                        float volume = (tags * 0.3f) + 0.7f;
                        float pitch = tags * 0.08f;
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item74 with { Volume = volume, Pitch = -pitch }, projectile.Center);

                        //Main.NewText(pitch);
                        p.timeLeft = 2;
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            Projectile.NewProjectile(player.GetSource_FromThis(), p.Center, npc.velocity, ModContent.ProjectileType<VirulentCatExplosion>(), (projectile.damage * 2), tags + 3, projectile.owner, tags, 0);
                        }
                        int buffindex = npc.FindBuffIndex(ModContent.BuffType<Buffs.ViruCatDrain>());

                        if (buffindex != -1)
                        {
                            npc.DelBuff(buffindex);
                        }
                    }
                    if (tags > 0 && !shockwaveCreated)
                    {
                        shockwaveCreated = true;
                        if (projectile.type == ModContent.ProjectileType<VirulentCatDetonator>())
                        {
                            if (Main.netMode != NetmodeID.MultiplayerClient)
                            {
                                Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, Vector2.Zero, ModContent.ProjectileType<Projectiles.VFX.ShockwaveEffect>(), 0, 0, Main.myPlayer, 400 * (tags / 12f), 50 * (tags / 12f));
                            }
                        }
                    }
                }
            }

            if (npc.GetGlobalNPC<tsorcRevampGlobalNPC>().BiohazardDrain && (projectile.type == ModContent.ProjectileType<BiohazardDetonator>() || projectile.type == ModContent.ProjectileType<BiohazardExplosion>()))
            {
                npc.GetGlobalNPC<tsorcRevampGlobalNPC>().ResetBiohazardBlobs = true;
                int tags;


                bool shockwaveCreated = false;
                for (int i = 0; i < 1000; i++)
                {
                    tags = 0;
                    Projectile p = Main.projectile[i];
                    if (p.active && p.type == ModContent.ProjectileType<BiohazardShot>() && p.ai[0] == 1f && p.timeLeft > 2 && p.ai[1] == npc.whoAmI)
                    {
                        for (int q = 0; q < 1000; q++)
                        {
                            Projectile ñ = Main.projectile[q];
                            if (ñ.active && ñ.type == ModContent.ProjectileType<BiohazardShot>() && ñ.ai[0] == 1f && ñ.ai[1] == npc.whoAmI)
                            {
                                tags++;
                            }
                        }
                        float volume = (tags * 0.3f) + 0.7f;
                        float pitch = tags * 0.08f;

                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Item74 with { Volume = volume, Pitch = -pitch }, projectile.Center);

                        p.timeLeft = 2;
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            Projectile.NewProjectile(player.GetSource_FromThis(), p.Center, npc.velocity, ModContent.ProjectileType<BiohazardExplosion>(), (projectile.damage * 2), tags + 3, projectile.owner, tags, 0);
                        }
                        int buffindex = npc.FindBuffIndex(ModContent.BuffType<Buffs.BiohazardDrain>());

                        if (buffindex != -1)
                        {
                            npc.DelBuff(buffindex);
                        }
                    }
                    if (tags > 0 && !shockwaveCreated)
                    {
                        shockwaveCreated = true;
                        if (projectile.type == ModContent.ProjectileType<BiohazardDetonator>() && Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            Projectile.NewProjectile(npc.GetSource_FromThis(), npc.Center, Vector2.Zero, ModContent.ProjectileType<Projectiles.VFX.ShockwaveEffect>(), 0, 0, Main.myPlayer, 500 * (tags / 12f), 60 * (tags / 12f));
                        }
                    }
                }
            }
            #endregion
        }
        public override void ModifyHitPlayer(NPC npc, Player target, ref Player.HurtModifiers modifiers)
        {
            if (PlaguesmithBuff)
            {
                modifiers.FinalDamage *= 1.25f;
            }
        }

        public override bool? CanBeHitByItem(NPC npc, Player player, Item item)
        {
            if (DodgeTimer > 0 || QuickStepTimer > 0) // i-frames during a dodgeroll / standing quick step
            {
                return false;
            }
            return base.CanBeHitByItem(npc, player, item);
        }

        public override bool? CanBeHitByProjectile(NPC npc, Projectile projectile)
        {
            if (DodgeTimer > 0 || QuickStepTimer > 0) // i-frames during a dodgeroll / standing quick step
            {
                return false;
            }
            return base.CanBeHitByProjectile(npc, projectile);
        }
        Texture2D LionheartMarksSprite;
        Texture2D ScorchMarksSprite;
        Texture2D ShockMarksSprite;
        Texture2D SunburnMarksSprite;
        public override bool? DrawHealthBar(NPC npc, byte hbPosition, ref float scale, ref Vector2 position)
        {
            if (!ModContent.GetInstance<tsorcRevampVisualConfig>().EnemyHealthBars)
            {
                return false;
            }

            if (CanGoInvisible && IsInvisible && !LocalPlayerRevealsInvisibleNPCs())
            {
                return false;
            }

            return base.DrawHealthBar(npc, hbPosition, ref scale, ref position);
        }

        public override bool PreDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (npc.HasBuff(ModContent.BuffType<LionheartMark>()) && npc.GetGlobalNPC<tsorcRevampGlobalNPC>().LionheartMarks > 0)
            {
                LionheartMarksSprite = (Texture2D)ModContent.Request<Texture2D>("tsorcRevamp/Buffs/Weapons/LionheartMarkVisual");
                Rectangle LionheartSourceRectangle = new Rectangle(0, 0 * LionheartMarksSprite.Height / 5, LionheartMarksSprite.Width, LionheartMarksSprite.Height / 5);
                switch (npc.GetGlobalNPC<tsorcRevampGlobalNPC>().LionheartMarks)
                {
                    case 1:
                        {
                            LionheartSourceRectangle = new Rectangle(0, 4 * LionheartMarksSprite.Height / 5, LionheartMarksSprite.Width, LionheartMarksSprite.Height / 5);
                            break;
                        }
                    case 2:
                        {
                            LionheartSourceRectangle = new Rectangle(0, 3 * LionheartMarksSprite.Height / 5, LionheartMarksSprite.Width, LionheartMarksSprite.Height / 5);
                            break;
                        }
                    case 3:
                        {
                            LionheartSourceRectangle = new Rectangle(0, 2 * LionheartMarksSprite.Height / 5, LionheartMarksSprite.Width, LionheartMarksSprite.Height / 5);
                            break;
                        }
                    case 4:
                        {
                            LionheartSourceRectangle = new Rectangle(0, 1 * LionheartMarksSprite.Height / 5, LionheartMarksSprite.Width, LionheartMarksSprite.Height / 5);
                            break;
                        }
                    case 5:
                        {
                            LionheartSourceRectangle = new Rectangle(0, 0 * LionheartMarksSprite.Height / 5, LionheartMarksSprite.Width, LionheartMarksSprite.Height / 5);
                            break;
                        }
                }
                Main.EntitySpriteDraw(LionheartMarksSprite, npc.Center - Main.screenPosition - new Vector2(0, npc.GetGlobalNPC<tsorcRevampGlobalNPC>().LionheartMarks * LionheartMarksSprite.Height / 5 - 100), LionheartSourceRectangle, Color.White, 0, LionheartSourceRectangle.Center.ToVector2(), 1, SpriteEffects.None, 0);
            }
            if (npc.HasBuff(ModContent.BuffType<ScorchingDebuff>()) && npc.GetGlobalNPC<tsorcRevampGlobalNPC>().ScorchMarks > 0)
            {
                ScorchMarksSprite = (Texture2D)ModContent.Request<Texture2D>("tsorcRevamp/Buffs/Runeterra/Summon/ScorchingMarkVisual");
                Rectangle ScorchingMarkSourceRectangle = new Rectangle(0, 0 * ScorchMarksSprite.Height / 6, ScorchMarksSprite.Width, ScorchMarksSprite.Height / 6);
                switch (npc.GetGlobalNPC<tsorcRevampGlobalNPC>().ScorchMarks)
                {
                    case 1:
                        {
                            ScorchingMarkSourceRectangle = new Rectangle(0, 5 * ScorchMarksSprite.Height / 6, ScorchMarksSprite.Width, ScorchMarksSprite.Height / 6);
                            break;
                        }
                    case 2:
                        {
                            ScorchingMarkSourceRectangle = new Rectangle(0, 4 * ScorchMarksSprite.Height / 6, ScorchMarksSprite.Width, ScorchMarksSprite.Height / 6);
                            break;
                        }
                    case 3:
                        {
                            ScorchingMarkSourceRectangle = new Rectangle(0, 3 * ScorchMarksSprite.Height / 6, ScorchMarksSprite.Width, ScorchMarksSprite.Height / 6);
                            break;
                        }
                    case 4:
                        {
                            ScorchingMarkSourceRectangle = new Rectangle(0, 2 * ScorchMarksSprite.Height / 6, ScorchMarksSprite.Width, ScorchMarksSprite.Height / 6);
                            break;
                        }
                    case 5:
                        {
                            ScorchingMarkSourceRectangle = new Rectangle(0, 1 * ScorchMarksSprite.Height / 6, ScorchMarksSprite.Width, ScorchMarksSprite.Height / 6);
                            break;
                        }
                    case 6:
                        {
                            ScorchingMarkSourceRectangle = new Rectangle(0, 0 * ScorchMarksSprite.Height / 6, ScorchMarksSprite.Width, ScorchMarksSprite.Height / 6);
                            break;
                        }
                }
                Main.EntitySpriteDraw(ScorchMarksSprite, npc.Center - Main.screenPosition - new Vector2(0, ScorchMarksSprite.Height / 6 * ScorchMarks - 100), ScorchingMarkSourceRectangle, Color.White, 0, ScorchingMarkSourceRectangle.Center.ToVector2(), 1, SpriteEffects.None, 0);
            }
            if (npc.HasBuff(ModContent.BuffType<ShockedDebuff>()) && npc.GetGlobalNPC<tsorcRevampGlobalNPC>().ShockMarks > 0)
            {
                ShockMarksSprite = (Texture2D)ModContent.Request<Texture2D>("tsorcRevamp/Buffs/Runeterra/Summon/ShockedMarkVisual");
                Rectangle ShockedMarkSourceRectangle = new Rectangle(0, 0 * ShockMarksSprite.Height / 6, ShockMarksSprite.Width, ShockMarksSprite.Height / 6);
                switch (npc.GetGlobalNPC<tsorcRevampGlobalNPC>().ShockMarks)
                {
                    case 1:
                        {
                            ShockedMarkSourceRectangle = new Rectangle(0, 5 * ShockMarksSprite.Height / 6, ShockMarksSprite.Width, ShockMarksSprite.Height / 6);
                            break;
                        }
                    case 2:
                        {
                            ShockedMarkSourceRectangle = new Rectangle(0, 4 * ShockMarksSprite.Height / 6, ShockMarksSprite.Width, ShockMarksSprite.Height / 6);
                            break;
                        }
                    case 3:
                        {
                            ShockedMarkSourceRectangle = new Rectangle(0, 3 * ShockMarksSprite.Height / 6, ShockMarksSprite.Width, ShockMarksSprite.Height / 6);
                            break;
                        }
                    case 4:
                        {
                            ShockedMarkSourceRectangle = new Rectangle(0, 2 * ShockMarksSprite.Height / 6, ShockMarksSprite.Width, ShockMarksSprite.Height / 6);
                            break;
                        }
                    case 5:
                        {
                            ShockedMarkSourceRectangle = new Rectangle(0, 1 * ShockMarksSprite.Height / 6, ShockMarksSprite.Width, ShockMarksSprite.Height / 6);
                            break;
                        }
                    case 6:
                        {
                            ShockedMarkSourceRectangle = new Rectangle(0, 0 * ShockMarksSprite.Height / 6, ShockMarksSprite.Width, ShockMarksSprite.Height / 6);
                            break;
                        }
                }
                Main.EntitySpriteDraw(ShockMarksSprite, npc.Center - Main.screenPosition - new Vector2(0, ShockMarksSprite.Height / 6 * ShockMarks - 100), ShockedMarkSourceRectangle, Color.White, 0, ShockedMarkSourceRectangle.Center.ToVector2(), 1, SpriteEffects.None, 0);
            }
            if (npc.HasBuff(ModContent.BuffType<SunburnDebuff>()) && npc.GetGlobalNPC<tsorcRevampGlobalNPC>().SunburnMarks > 0)
            {
                SunburnMarksSprite = (Texture2D)ModContent.Request<Texture2D>("tsorcRevamp/Buffs/Runeterra/Summon/SunburntMarkVisual");
                Rectangle SunburnMarkSourceRectangle = new Rectangle(0, 0 * SunburnMarksSprite.Height / 6, SunburnMarksSprite.Width, SunburnMarksSprite.Height / 6);
                switch (npc.GetGlobalNPC<tsorcRevampGlobalNPC>().SunburnMarks)
                {
                    case 1:
                        {
                            SunburnMarkSourceRectangle = new Rectangle(0, 5 * SunburnMarksSprite.Height / 6, SunburnMarksSprite.Width, SunburnMarksSprite.Height / 6);
                            break;
                        }
                    case 2:
                        {
                            SunburnMarkSourceRectangle = new Rectangle(0, 4 * SunburnMarksSprite.Height / 6, SunburnMarksSprite.Width, SunburnMarksSprite.Height / 6);
                            break;
                        }
                    case 3:
                        {
                            SunburnMarkSourceRectangle = new Rectangle(0, 3 * SunburnMarksSprite.Height / 6, SunburnMarksSprite.Width, SunburnMarksSprite.Height / 6);
                            break;
                        }
                    case 4:
                        {
                            SunburnMarkSourceRectangle = new Rectangle(0, 2 * SunburnMarksSprite.Height / 6, SunburnMarksSprite.Width, SunburnMarksSprite.Height / 6);
                            break;
                        }
                    case 5:
                        {
                            SunburnMarkSourceRectangle = new Rectangle(0, 1 * SunburnMarksSprite.Height / 6, SunburnMarksSprite.Width, SunburnMarksSprite.Height / 6);
                            break;
                        }
                    case 6:
                        {
                            SunburnMarkSourceRectangle = new Rectangle(0, 0 * SunburnMarksSprite.Height / 6, SunburnMarksSprite.Width, SunburnMarksSprite.Height / 6); 
                            break;
                        }
                }
                Main.EntitySpriteDraw(SunburnMarksSprite, npc.Center - Main.screenPosition - new Vector2(0, SunburnMarksSprite.Height / 6 * SunburnMarks - 100), SunburnMarkSourceRectangle, Color.White, 0, SunburnMarkSourceRectangle.Center.ToVector2(), 1, SpriteEffects.None, 0);
            }

            if (DodgeTimer > 0 && Main.GameUpdateCount % 10 < 5)
            {
                return false;
            }

            bool preDraw = base.PreDraw(npc, spriteBatch, screenPos, drawColor);

            tsorcRevampGlobalNPC globalNPC = npc.GetGlobalNPC<tsorcRevampGlobalNPC>();

            // Poise bar takes priority over the stamina bar — they share the same container so only one shows at a time.
            if (globalNPC.PoiseBarVisible)
            {
                // The bar's COLOUR tells you whether the enemy can be staggered right now:
                //   orange = vulnerable (poise filling toward a break);
                //   white  = can't take stagger damage — hyperarmor (committed attack) OR the post-stagger recovery i-frames;
                //   yellow (flashing) = actively staggered/broken.
                // The "can't stagger" states show a full/locked bar so the white reads as a shield, not an empty sliver.
                bool cantStagger = globalNPC.AttackCommitted || globalNPC.PoiseBreakCooldown > 0;
                float poisePercentage = (globalNPC.StaggerTimer > 0 || globalNPC.PoiseBreakCooldown > 0) ? 1f : globalNPC.Poise / globalNPC.EffectivePoiseMax;
                Color poiseColor;
                if (globalNPC.StaggerTimer > 0)
                {
                    poiseColor = Main.GameUpdateCount % 10 < 5 ? Color.Yellow : Color.Orange; // flash while broken
                }
                else if (cantStagger)
                {
                    poiseColor = Color.White; // hyperarmor / recovery i-frames
                }
                else
                {
                    poiseColor = Color.Orange;
                }
                DrawResourceBar(npc, poisePercentage, poiseColor);
            }
            else if (globalNPC.DodgeCooldown != 0)
            {
                float staminaMax = (int)(300 * (1 - globalNPC.Agility));
                float staminaCurrent = staminaMax - globalNPC.DodgeCooldown;
                float staminaPercentage = staminaCurrent / staminaMax;
                DrawResourceBar(npc, staminaPercentage, new Color(40, 190, 80)); // matches the player's green stamina bar
            }
            return preDraw;
        }

        /// <summary>Draws the enemy stat bar above the NPC using the shared StaminaBar container. The fill area is split
        /// horizontally: a thin red HEALTH bar on top, and the active resource (green stamina / orange poise) on the
        /// bottom. Only called while the bar should be visible (stagger or dodge active).</summary>
        private static void DrawResourceBar(NPC npc, float resourcePercentage, Color resourceColor)
        {
            resourcePercentage = MathHelper.Clamp(resourcePercentage, 0f, 1f);
            float healthPercentage = MathHelper.Clamp(npc.lifeMax > 0 ? (float)npc.life / npc.lifeMax : 0f, 0f, 1f);

            const float abovePlayer = 45f; // how far above the NPC the bar sits
            Texture2D barFill = (Texture2D)ModContent.Request<Texture2D>("tsorcRevamp/Textures/StaminaBar_full");
            Texture2D barEmpty = (Texture2D)ModContent.Request<Texture2D>("tsorcRevamp/Textures/StaminaBar_empty");

            Point barOrigin = (npc.Center - new Vector2(barEmpty.Width / 2, abovePlayer) - Main.screenPosition).ToPoint();
            Rectangle emptyDestination = new Rectangle(barOrigin.X, barOrigin.Y, barEmpty.Width, barEmpty.Height);

            // The StaminaBar_full sprite is only solid in its bottom rows; use that solid region as the source so the
            // fills stay crisp instead of squishing the (mostly transparent) sprite into a thin sliver.
            Rectangle fillSource = new Rectangle(0, 6, barFill.Width, barFill.Height - 6);

            // Keep the fills strictly inside the container's recessed track so the (drawn-on-top) fills tuck BEHIND the
            // frame's top/left/right border rows. The 39x12 container's track is rows 6-10, cols 6-34 (1px in from the
            // left/right frame and from the track's top border row 5). Health on top, resource below, no gap.
            int fillX = barOrigin.X + 6;          // 1px past the left frame
            int fillWidth = 35 - 6;               // right edge at col 35 = just inside the right cap
            int trackTop = barOrigin.Y + 6;       // leaves row 5 (track top border) uncovered
            const int healthHeight = 2;           // rows 6-7
            const int resourceHeight = 3;         // rows 8-10

            Rectangle healthDestination = new Rectangle(fillX, trackTop, (int)(healthPercentage * fillWidth), healthHeight);
            Rectangle resourceDestination = new Rectangle(fillX, trackTop + healthHeight, (int)(resourcePercentage * fillWidth), resourceHeight);

            Main.spriteBatch.Draw(barEmpty, emptyDestination, Color.White);
            Main.spriteBatch.Draw(barFill, healthDestination, fillSource, Color.Red);
            Main.spriteBatch.Draw(barFill, resourceDestination, fillSource, resourceColor);
        }

        public override void PostDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            bool drawInvisibleAfterimages = CanGoInvisible && IsInvisible && !LocalPlayerRevealsInvisibleNPCs();
            if (!drawInvisibleAfterimages && !HasGhostAfterimages && !EvasiveAfterimagesActive)
            {
                return;
            }

            Texture2D texture = TextureAssets.Npc[npc.type].Value;
            SpriteEffects effects = npc.spriteDirection < 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
            Vector2 drawPosition = npc.Center - screenPos + new Vector2(0f, npc.gfxOffY);
            Vector2 origin = npc.frame.Size() / 2f;
            float time = (float)Main.timeForVisualEffects;
            float baseOpacity = drawInvisibleAfterimages
                ? MathHelper.Clamp((255 - InvisibleAlpha) / 255f, 0.06f, 0.35f)
                : 0.16f;
            float endOpacity = drawInvisibleAfterimages
                ? MathHelper.Clamp((255 - Math.Min(InvisibleAlpha + 20, 250)) / 255f, 0.02f, baseOpacity)
                : 0.03f;

            int trailCount = InvisibilityTrailPositions.Length;
            for (int k = trailCount - 1; k >= 0; k--)
            {
                if (InvisibilityTrailPositions[k] == Vector2.Zero)
                {
                    continue;
                }

                float fade = (trailCount - k) / (float)trailCount;
                Vector2 trailPosition = InvisibilityTrailPositions[k] - screenPos + new Vector2(0f, npc.gfxOffY);
                Color trailColor = Color.White * MathHelper.Lerp(endOpacity, baseOpacity, fade);
                spriteBatch.Draw(texture, trailPosition, npc.frame, trailColor, npc.rotation, origin, npc.scale * (1f - k * 0.035f), effects, 0f);
            }

            UsefulFunctions.StartAdditiveSpritebatch(ref spriteBatch);

            float waveX = (float)Math.Sin(time * 0.16f + npc.whoAmI) * 2.2f;
            float waveY = (float)Math.Cos(time * 0.11f + npc.whoAmI * 0.7f) * 1.4f;
            Color coolEdge = Color.Cyan * (0.5f * baseOpacity);
            Color paleEdge = new Color(150, 230, 255) * (0.32f * baseOpacity);
            Color blueEdge = new Color(80, 180, 255) * (0.45f * baseOpacity);

            spriteBatch.Draw(texture, drawPosition + new Vector2(waveX, waveY), npc.frame, coolEdge, npc.rotation, origin, npc.scale, effects, 0f);
            spriteBatch.Draw(texture, drawPosition - new Vector2(waveY, waveX) * 0.75f, npc.frame, blueEdge, npc.rotation, origin, npc.scale, effects, 0f);
            spriteBatch.Draw(texture, drawPosition + new Vector2((float)Math.Sin(time * 0.31f) * 1.1f, -waveY), npc.frame, paleEdge, npc.rotation, origin, npc.scale, effects, 0f);
            UsefulFunctions.RestartSpritebatch(ref spriteBatch);
        }

        public override void ModifyGlobalLoot(GlobalLoot globalLoot)
        {

            if (ModContent.GetInstance<tsorcRevampConfig>().AdventureMode)
            {
                List<IItemDropRule> ruleList = globalLoot.Get();
                for (int i = 0; i < ruleList.Count; i++)
                {
                    string s = ruleList[i].ToString();
                    if (s == "Terraria.GameContent.ItemDropRules.MechBossSpawnersDropRule")
                    {
                        globalLoot.Remove(ruleList[i]);
                    }
                }
            }
        }

        public override bool PreKill(NPC npc)
        {
            for (int i = 0; i < tsorcRevamp.BannedItems.Count; i++)
            {
                NPCLoader.blockLoot.Add(tsorcRevamp.BannedItems[i]);
            }

            return base.PreKill(npc);
        }

        public override void UpdateLifeRegen(NPC npc, ref int damage)
        {
            #region Dragon Stone Potency for Vanilla Buffs
            if (npc.HasBuff(BuffID.OnFire) && tsorcRevampPlayer.DragonStonePotency)
            {
                if (npc.lifeRegen > 0)
                {
                    npc.lifeRegen = 0;
                }
                int DoTPerS = 4 * DragonStone.Potency - 4;
                if (npc.HasBuff(BuffID.Oiled))
                {
                    DoTPerS += 25 * DragonStone.Potency;
                }
                npc.lifeRegen -= DoTPerS * 2;
                damage += DoTPerS;
            }
            if (npc.HasBuff(BuffID.OnFire3) && tsorcRevampPlayer.DragonStonePotency)
            {
                if (npc.lifeRegen > 0)
                {
                    npc.lifeRegen = 0;
                }
                int DoTPerS = 15 * DragonStone.Potency - 15;
                if (npc.HasBuff(BuffID.Oiled))
                {
                    DoTPerS += 25 * DragonStone.Potency;
                }
                npc.lifeRegen -= DoTPerS * 2;
                damage += DoTPerS;
            }
            if (npc.HasBuff(BuffID.CursedInferno) && tsorcRevampPlayer.DragonStonePotency)
            {
                if (npc.lifeRegen > 0)
                {
                    npc.lifeRegen = 0;
                }
                int DoTPerS = 24 * DragonStone.Potency - 24;
                if (npc.HasBuff(BuffID.Oiled))
                {
                    DoTPerS += 25 * DragonStone.Potency;
                }
                npc.lifeRegen -= DoTPerS * 2;
                damage += DoTPerS;
            }
            if (npc.HasBuff(BuffID.Frostburn) && tsorcRevampPlayer.DragonStonePotency)
            {
                if (npc.lifeRegen > 0)
                {
                    npc.lifeRegen = 0;
                }
                int DoTPerS = 8 * DragonStone.Potency - 8;
                if (npc.HasBuff(BuffID.Oiled))
                {
                    DoTPerS += 25 * DragonStone.Potency;
                }
                npc.lifeRegen -= DoTPerS * 2;
                damage += DoTPerS;
            }
            if (npc.HasBuff(BuffID.Frostburn2) && tsorcRevampPlayer.DragonStonePotency)
            {
                if (npc.lifeRegen > 0)
                {
                    npc.lifeRegen = 0;
                }
                int DoTPerS = 25 * DragonStone.Potency - 25;
                if (npc.HasBuff(BuffID.Oiled))
                {
                    DoTPerS += 25 * DragonStone.Potency;
                }
                npc.lifeRegen -= DoTPerS * 2;
                damage += DoTPerS;
            }
            if (npc.HasBuff(BuffID.ShadowFlame) && tsorcRevampPlayer.DragonStonePotency)
            {
                if (npc.lifeRegen > 0)
                {
                    npc.lifeRegen = 0;
                }
                int DoTPerS = 15 * DragonStone.Potency - 15;
                if (npc.HasBuff(BuffID.Oiled))
                {
                    DoTPerS += 25 * DragonStone.Potency;
                }
                npc.lifeRegen -= DoTPerS * 2;
                damage += DoTPerS;
            }
            if (npc.HasBuff(BuffID.Poisoned) && tsorcRevampPlayer.DragonStonePotency)
            {
                if (npc.lifeRegen > 0)
                {
                    npc.lifeRegen = 0;
                }
                int DoTPerS = 6 * DragonStone.Potency - 6;
                npc.lifeRegen -= DoTPerS * 2;
                damage += DoTPerS;
            }
            if (npc.HasBuff(BuffID.Venom) && tsorcRevampPlayer.DragonStonePotency)
            {
                if (npc.lifeRegen > 0)
                {
                    npc.lifeRegen = 0;
                }
                int DoTPerS = 30 * DragonStone.Potency - 30;
                npc.lifeRegen -= DoTPerS * 2;
                damage += DoTPerS;
            }
            if (npc.HasBuff(BuffID.Daybreak) && tsorcRevampPlayer.DragonStonePotency)
            {
                if (npc.lifeRegen > 0)
                {
                    npc.lifeRegen = 0;
                }
                int DoTPerS = (100 * DragonStone.Potency - 100) / 2; //2x weaker with Daybreak
                npc.lifeRegen -= DoTPerS * 2;
                damage += DoTPerS;
            }
            #endregion
            if (Ignited)
            {
                int DoTPerS = 20;
                if (tsorcRevampPlayer.DragonStonePotency)
                {
                    DoTPerS *= DragonStone.Potency;
                }
                if (npc.HasBuff(BuffID.Oiled))
                {
                    if (tsorcRevampPlayer.DragonStonePotency)
                    {
                        DoTPerS += 25 * DragonStone.Potency;
                    }
                    else
                    {
                        DoTPerS += 25;
                    }
                }
                if (npc.lifeRegen > 0)
                {
                    npc.lifeRegen = 0;
                }
                npc.lifeRegen -= DoTPerS * 2;
                damage += DoTPerS;
            }
            if (Venomized)
            {
                int DoTPerS = (int)lastHitPlayerRanger.GetTotalDamage(DamageClass.Ranged).ApplyTo((float)ToxicShot.BaseDamage * 1.5f) + (int)(lastHitPlayerRanger.GetTotalCritChance(DamageClass.Ranged) / 100f * (float)ToxicShot.BaseDamage * 1.5f);
                npc.lifeRegen -= DoTPerS * 2;
                damage += DoTPerS;
            }

            if (Electrified)
            {
                int DoTPerS = (int)lastHitPlayerRanger.GetTotalDamage(DamageClass.Ranged).ApplyTo((float)AlienGun.BaseDamage * 1.5f) + (int)(lastHitPlayerRanger.GetTotalCritChance(DamageClass.Ranged) / 100f * (float)AlienGun.BaseDamage * 1.5f);
                npc.lifeRegen -= DoTPerS * 2;
                damage += DoTPerS;
            }

            if (Irradiated)
            {
                int DoTPerS = (int)lastHitPlayerRanger.GetTotalDamage(DamageClass.Ranged).ApplyTo((float)OmegaSquadRifle.BaseDamage * 1.5f) + (int)(lastHitPlayerRanger.GetTotalCritChance(DamageClass.Ranged) / 100f * (float)OmegaSquadRifle.BaseDamage * 1.5f);
                npc.lifeRegen -= DoTPerS * 2;
                damage += DoTPerS;
            }

            if (IrradiatedByShroom)
            {
                int DoTPerS = (int)lastHitPlayerRanger.GetTotalDamage(DamageClass.Ranged).ApplyTo((float)OmegaSquadRifle.BaseDamage * 2.28f) + (int)(lastHitPlayerRanger.GetTotalCritChance(DamageClass.Ranged) / 100f * (float)OmegaSquadRifle.BaseDamage * 2.28f);
                npc.lifeRegen -= DoTPerS * 2;
                damage += DoTPerS;
            }

            if (Scorched && !NPCID.Sets.ImmuneToRegularBuffs[npc.type])
            {
                float DoTPerS = lastHitPlayerSummoner.GetTotalDamage(DamageClass.Summon).ApplyTo((float)ScorchingPoint.BaseDmg / 3f);
                if (SuperScorchDuration > 0)
                {
                    DoTPerS *= 1f + RuneterraGauntlets.SuperBurnDmgAmp / 100f;
                }
                if (npc.HasBuff(BuffID.Oiled))
                {
                    DoTPerS += 25 * DragonStone.Potency;
                }
                npc.lifeRegen -= (int)DoTPerS * 2;
                damage += (int)DoTPerS;
            }

            if (Shocked && !NPCID.Sets.ImmuneToRegularBuffs[npc.type])
            {
                float DoTPerS = lastHitPlayerSummoner.GetTotalDamage(DamageClass.Summon).ApplyTo((float)InterstellarVesselGauntlet.BaseDmg / 3f);
                if (SuperShockDuration > 0)
                {
                    DoTPerS *= 1f + RuneterraGauntlets.SuperBurnDmgAmp / 100f;
                }
                npc.lifeRegen -= (int)DoTPerS * 2;
                damage += (int)DoTPerS;
            }

            if (Sunburnt && !NPCID.Sets.ImmuneToRegularBuffs[npc.type])
            {
                float DoTPerS = lastHitPlayerSummoner.GetTotalDamage(DamageClass.Summon).ApplyTo((float)CenterOfTheUniverse.BaseDmg / 3f);
                if (SuperSunburnDuration > 0)
                {
                    DoTPerS *= 1f + RuneterraGauntlets.SuperBurnDmgAmp / 100f;
                }
                if (npc.HasBuff(BuffID.Oiled))
                {
                    DoTPerS += 25 * DragonStone.Potency;
                }
                npc.lifeRegen -= (int)DoTPerS * 2;
                damage += (int)DoTPerS;
            }

            if (Awestruck && !NPCID.Sets.ImmuneToRegularBuffs[npc.type])
            {
                float DoTPerS = lastHitPlayerSummoner.GetTotalDamage(DamageClass.Summon).ApplyTo((float)CenterOfTheUniverse.BaseDmg);
                if (npc.HasBuff(BuffID.Oiled))
                {
                    DoTPerS += 25 * DragonStone.Potency;
                }
                npc.lifeRegen -= (int)DoTPerS * 2;
                damage += (int)DoTPerS;
            }

            if (DarkInferno)
            {
                int DoTPerS = 30;
				if (npc.lifeRegen > 0)
                {
					npc.lifeRegen = 0;
                }
				npc.lifeRegen -= (int)DoTPerS * 2; 

				if (damage < 10)
                {
					damage = 10; 
				}

				if (tsorcRevampPlayer.DragonStonePotency)
				{
					npc.lifeRegen *= DragonStone.Potency;
					damage = 40;
				}
                if (npc.HasBuff(BuffID.Oiled))
                {
                    if (tsorcRevampPlayer.DragonStonePotency)
                    {
                        npc.lifeRegen += 25 * DragonStone.Potency;
                    }
                    else
                    {
                        npc.lifeRegen += 25;
                    }
                }

                var N = npc;
                for (int j = 0; j < 6; j++)
                {
                    int dust = Dust.NewDust(N.position, N.width / 2, N.height / 2, 54, (N.velocity.X * 0.2f), N.velocity.Y * 0.2f, 100, default, 1f);
                    Main.dust[dust].noGravity = true;

                    int dust2 = Dust.NewDust(N.position, N.width / 2, N.height / 2, 58, (N.velocity.X * 0.2f), N.velocity.Y * 0.2f, 100, default, 1f);
                    Main.dust[dust2].noGravity = true;
                }
            }

            if (AbyssInferno)
            {
                int DoTPerS = 150;
				if (npc.lifeRegen > 0)
                {
					npc.lifeRegen = 0;
                }
				npc.lifeRegen -= (int)DoTPerS * 2; 

				if (damage < 75)
                {
					damage = 75; 
				}

				if (tsorcRevampPlayer.DragonStonePotency)
				{
					npc.lifeRegen *= DragonStone.Potency / 2;
					damage = 150;
				}
                if (npc.HasBuff(BuffID.Oiled))
                {
                    if (tsorcRevampPlayer.DragonStonePotency)
                    {
                        npc.lifeRegen += 25 * DragonStone.Potency;
                    }
                    else
                    {
                        npc.lifeRegen += 25;
                    }
                }

                var N = npc;
                if (Main.rand.NextBool(7))
                {
                    int dustIndex = Dust.NewDust(npc.position, npc.width, npc.height, DustID.PinkTorch, 0f, 0f, 100, Color.Pink, 2f);
                    Main.dust[dustIndex].noGravity = false; 
                    Main.dust[dustIndex].velocity *= 1.05f;
                    Main.dust[dustIndex].fadeIn = 1.2f;
                }
                if (Main.rand.NextBool(8)) 
                { 
                    int dustIndex = Dust.NewDust(npc.position, npc.width, npc.height, 223, 0f, 0f, 100, Color.Pink, 1.6f);
                    Main.dust[dustIndex].noGravity = true; 
                    Main.dust[dustIndex].velocity *= 0.99f;
                    Main.dust[dustIndex].fadeIn = 1.2f;
                }
            }

            if (MorgulPoisoning)
            {
                int DoTPerS = 200;
				if (npc.lifeRegen > 0)
					npc.lifeRegen = 0;

				npc.lifeRegen -= (int)DoTPerS * 2; 

				if (damage < 30)
                {
					damage = 30; 
				}

				if (tsorcRevampPlayer.DragonStonePotency)
				{
					npc.lifeRegen *= DragonStone.Potency / 2;
					damage = 60;
				}

                if (Main.rand.NextBool(10))
                { 
                    Dust.NewDust(npc.position, npc.width, npc.height, DustID.ToxicBubble, npc.velocity.X * 0.2f, npc.velocity.Y * 0.2f, 100, Color.Green, 1.5f);
                }
            }

            if (WitchkingCurse)
            {
                int DoTPerS = 400;
				if (npc.lifeRegen > 0)
                {
					npc.lifeRegen = 0;
                }
				npc.lifeRegen -= (int)DoTPerS * 2; 

				if (damage < 100)
                {
					damage = 100; 
				}

				if (tsorcRevampPlayer.DragonStonePotency)
				{
					npc.lifeRegen *= DragonStone.Potency / 2;
					damage = 200;
				}

                npc.damage = (int)(npc.damage * 0.8f);
                npc.defense = Math.Max(0, npc.defDefense - 40);
                npc.velocity *= 0.95f;

                var N = npc;
            }

            if (AbyssalSinking)
            {
                npc.defense = Math.Max(0, npc.defDefense - 24);
                npc.velocity *= 0.98f;
            }

            if (CCShocked)
            {
                int DoTPerS = (int)lastHitPlayerSummoner.GetTotalDamage(DamageClass.Summon).ApplyTo(100);
                if (npc.lifeRegen > 0)
                {
                    npc.lifeRegen = 0;
                }
                if (tsorcRevampPlayer.DragonStonePotency)
                {
                    DoTPerS *= DragonStone.Potency;
                }
                npc.lifeRegen -= DoTPerS * 2;
                damage += DoTPerS;

                /*var N = npc;
                for (int j = 0; j < 6; j++)
                {
                    int dust = Dust.NewDust(N.position, N.width / 2, N.height / 2, 54, (N.velocity.X * 0.2f), N.velocity.Y * 0.2f, 100, default, 1f);
                    Main.dust[dust].noGravity = true;

                    int dust2 = Dust.NewDust(N.position, N.width / 2, N.height / 2, 58, (N.velocity.X * 0.2f), N.velocity.Y * 0.2f, 100, default, 1f);
                    Main.dust[dust2].noGravity = true;
                }*/
            }

            if (PhazonCorruption)
            {
                int DoTPerS = 21;
                if (npc.lifeRegen > 0)
                    {
                        npc.lifeRegen = 0;
                    }
                if (tsorcRevampPlayer.DragonStonePotency)
                {
                    DoTPerS *= DragonStone.Potency;
                }
                npc.lifeRegen -= DoTPerS * (tsorcRevampWorld.SuperHardMode ? 4 : 1);

                damage += DoTPerS * (tsorcRevampWorld.SuperHardMode ? 4 : 1); 

                int dust = Dust.NewDust(npc.position, npc.width, npc.height, 185, (npc.velocity.X * 0.2f), npc.velocity.Y * 0.2f, 100, default, 1f);
                Main.dust[dust].noGravity = true;

                int dust2 = Dust.NewDust(npc.position, npc.width, npc.height, DustID.FireworkFountain_Blue, (npc.velocity.X * 0.2f), npc.velocity.Y * 0.2f, 100, default, 1f);
                Main.dust[dust2].noGravity = true;
            }

            if (CrimsonBurn)
            {
                int DoTPerS = 40;
				if (npc.lifeRegen > 0)
                {
					npc.lifeRegen = 0;
                }
				npc.lifeRegen -= (int) DoTPerS * 2 * (Main.hardMode ? 2 : 1);

				if (damage < 10)
                {
					damage = 10 * (Main.hardMode ? 2 : 1); 
				}

				if (tsorcRevampPlayer.DragonStonePotency)
				{
					npc.lifeRegen *= DragonStone.Potency / 2;
					damage = 20 * (Main.hardMode ? 2 : 1);
				}
                if (npc.HasBuff(BuffID.Oiled))
                {
                    if (tsorcRevampPlayer.DragonStonePotency)
                    {
                        npc.lifeRegen += 25 * DragonStone.Potency;
                    }
                    else
                    {
                        npc.lifeRegen += 25;
                    }
                }

                npc.defense = Math.Max(0, npc.defDefense - 5);

                var N = npc;
                for (int j = 0; j < 5; j++)
                {
                    int dust = Dust.NewDust(N.position, N.width / 2, N.height / 2, 5, (N.velocity.X * 0.2f), N.velocity.Y * 0.2f, 100, default, 1f);
                    Main.dust[dust].noGravity = false;
                }
            }

            if (ToxicCatDrain)
            {
                if (npc.lifeRegen > 0)
                {
                    npc.lifeRegen = 0;
                }

                int ToxicCatShotCount = 0;

                for (int i = 0; i < 1000; i++)
                {
                    Projectile p = Main.projectile[i];
                    if (p.active && p.type == ModContent.ProjectileType<ToxicCatShot>() && p.ai[0] == 1f && p.ai[1] == npc.whoAmI)
                    {
                        ToxicCatShotCount++;
                    }
                }
                if (ToxicCatShotCount >= 4)
                { //this is to make it worth the players time stickying more than 3 times
                    npc.lifeRegen -= ToxicCatShotCount * 3 * 3; //Use 1st N for damage, second N can be used to make it tick faster.
                    if (damage < ToxicCatShotCount * 1)
                    {
                        damage = ToxicCatShotCount * 1;
                    }
                }
                else
                {
                    npc.lifeRegen -= ToxicCatShotCount * 2 * 3;
                    if (damage < ToxicCatShotCount * 1)
                    {
                        damage = ToxicCatShotCount * 1;
                    }
                }
            }

            if (ViruCatDrain)
            {
                if (npc.lifeRegen > 0)
                {
                    npc.lifeRegen = 0;
                }

                int ViruCatShotCount = 0;

                for (int i = 0; i < 1000; i++)
                {
                    Projectile p = Main.projectile[i];
                    if (p.active && p.type == ModContent.ProjectileType<VirulentCatShot>() && p.ai[0] == 1f && p.ai[1] == npc.whoAmI)
                    {
                        ViruCatShotCount++;
                    }
                }
                if (ViruCatShotCount >= 4)
                {
                    npc.lifeRegen -= ViruCatShotCount * 3 * 5; //I use 1st N for damage, second N can be used to make it tick faster.
                    if (damage < ViruCatShotCount * 1)
                    {
                        damage = ViruCatShotCount * 1;
                    }
                }
                else
                {
                    npc.lifeRegen -= ViruCatShotCount * 2 * 5;
                    if (damage < ViruCatShotCount * 1)
                    {
                        damage = ViruCatShotCount * 1;
                    }
                }
            }

            if (BiohazardDrain)
            {
                if (npc.lifeRegen > 0)
                {
                    npc.lifeRegen = 0;
                }

                int BiohazardShotCount = 0;

                for (int i = 0; i < 1000; i++)
                {
                    Projectile p = Main.projectile[i];
                    if (p.active && p.type == ModContent.ProjectileType<BiohazardShot>() && p.ai[0] == 1f && p.ai[1] == npc.whoAmI)
                    {
                        BiohazardShotCount++;
                    }
                }
                if (BiohazardShotCount >= 4)
                {
                    npc.lifeRegen -= BiohazardShotCount * 12 * 2; //I use 1st N for damage, second N can be used to make it tick faster.
                    if (damage < BiohazardShotCount * 1)
                    {
                        damage = BiohazardShotCount * 1;
                    }
                }
                else
                {
                    npc.lifeRegen -= BiohazardShotCount * 9 * 2;
                    if (damage < BiohazardShotCount * 1)
                    {
                        damage = BiohazardShotCount * 1;
                    }
                }
            }

            if (ElectrocutedEffect)
            {
                int DoTPerS = 6;
                if (npc.lifeRegen > 0)
                {
                    npc.lifeRegen = 0;
                }
                if (tsorcRevampPlayer.DragonStonePotency)
                {
                    DoTPerS *= DragonStone.Potency;
                }
                npc.lifeRegen -= DoTPerS * 2;
                damage += DoTPerS;
            }

            if (ElectrocutedEffect2)
            {
                int DoTPerS = 36;
                if (npc.lifeRegen > 0)
                {
                    npc.lifeRegen = 0;
                }
                if (tsorcRevampPlayer.DragonStonePotency)
                {
                    DoTPerS *= (DragonStone.Potency / 2);
                }
                npc.lifeRegen -= DoTPerS * 2;
                damage += DoTPerS;
            }
            
            if (ElectrocutedEffect3)
            {
                int DoTPerS = 116;
                if (npc.lifeRegen > 0)
                {
                    npc.lifeRegen = 0;
                }
                if (tsorcRevampPlayer.DragonStonePotency)
                {
                    DoTPerS *= (DragonStone.Potency / 2);
                }
                npc.lifeRegen -= DoTPerS * 2;
                damage += DoTPerS;
            }

            if (PolarisElectrocutedEffect)
            {
                int DoTPerS = 35;
                if (npc.lifeRegen > 0)
                {
                    npc.lifeRegen = 0;
                }
                if (tsorcRevampPlayer.DragonStonePotency)
                {
                    DoTPerS *= DragonStone.Potency;
                }
                npc.lifeRegen -= DoTPerS * 2;
                damage += DoTPerS;
            }

            if (CrescentMoonlight)
            {
                int DoTPerS = 26;
                if (npc.lifeRegen > 0)
                {
                    npc.lifeRegen = 0;
                }
                npc.lifeRegen -= DoTPerS * 2;
                damage += DoTPerS;
                if (Main.hardMode)
                {
                    npc.lifeRegen -= DoTPerS * 2;
                    damage += DoTPerS;
                }
            }

            if (PlaguesmithBuff)
            {
                if (Main.rand.NextBool(4)) 
                { 
                    int dustIndex = Dust.NewDust(npc.position, npc.width, npc.height, 298, 0f, 0f, 100, default, 1.6f);
                    Main.dust[dustIndex].noGravity = true; 
                    Main.dust[dustIndex].velocity *= 0.99f;
                    Main.dust[dustIndex].fadeIn = 1.2f;
                }
            }
        }
        public override void OnHitPlayer(NPC npc, Player target, Player.HurtInfo hurtInfo)
        {
            if (PlaguesmithBuff)
            {
                target.AddBuff(BuffID.Venom, 120);
            }
            if (SorrowfulCurse)
            {
                target.AddBuff(BuffID.CursedInferno, 300);
            }
        }

        public override void ModifyShop(NPCShop shop)
        {

            switch (shop.NpcType)
            {
                case NPCID.Merchant:
                    {
                        shop.Add(ItemID.Bottle);
                        break;
                    }
                case NPCID.DyeTrader:
                    {
                        //Basic dyes (most others can be crafted from a combination of these)
                        int price = 5;
                        shop.Add(new Item(ItemID.RedDye)
                        {
                            shopCustomPrice = price,
                            shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
                        });
                        shop.Add(new Item(ItemID.OrangeDye)
                        {
                            shopCustomPrice = price,
                            shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
                        });
                        shop.Add(new Item(ItemID.YellowDye)
                        {
                            shopCustomPrice = price,
                            shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
                        });
                        shop.Add(new Item(ItemID.LimeDye)
                        {
                            shopCustomPrice = price,
                            shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
                        });
                        shop.Add(new Item(ItemID.GreenDye)
                        {
                            shopCustomPrice = price,
                            shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
                        });
                        shop.Add(new Item(ItemID.TealDye)
                        {
                            shopCustomPrice = price,
                            shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
                        });
                        shop.Add(new Item(ItemID.CyanDye)
                        {
                            shopCustomPrice = price,
                            shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
                        });
                        shop.Add(new Item(ItemID.SkyBlueDye)
                        {
                            shopCustomPrice = price,
                            shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
                        });
                        shop.Add(new Item(ItemID.BlueDye)
                        {
                            shopCustomPrice = price,
                            shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
                        });
                        shop.Add(new Item(ItemID.PurpleDye)
                        {
                            shopCustomPrice = price,
                            shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
                        });
                        shop.Add(new Item(ItemID.VioletDye)
                        {
                            shopCustomPrice = price,
                            shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
                        });
                        shop.Add(new Item(ItemID.PinkDye)
                        {
                            shopCustomPrice = price,
                            shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
                        });
                        shop.Add(new Item(ItemID.BlackDye)
                        {
                            shopCustomPrice = price,
                            shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
                        });

                        price = 25;

                        //Special Dyes (Aka the cool ones)
                        shop.Add(new Item(ItemID.FogboundDye)
                        {
                            shopCustomPrice = price,
                            shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
                        });
                        shop.Add(new Item(ItemID.MushroomDye)
                        {
                            shopCustomPrice = price,
                            shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
                        });
                        shop.Add(new Item(ItemID.PurpleOozeDye)
                        {
                            shopCustomPrice = price,
                            shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
                        });

                        shop.Add(new Item(ItemID.ReflectiveDye)
                        {
                            shopCustomPrice = price,
                            shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
                        });

                        shop.Add(new Item(ItemID.ReflectiveObsidianDye)
                        {
                            shopCustomPrice = price,
                            shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
                        });

                        shop.Add(new Item(ItemID.ShadowDye)
                        {
                            shopCustomPrice = price,
                            shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
                        });

                        shop.Add(new Item(ItemID.MirageDye)
                        {
                            shopCustomPrice = price,
                            shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
                        });

                        shop.Add(new Item(ItemID.TwilightDye)
                        {
                            shopCustomPrice = price,
                            shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
                        });
                        shop.Add(new Item(ItemID.BurningHadesDye)
                        {
                            shopCustomPrice = price,
                            shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
                        });
                        shop.Add(new Item(ItemID.ShadowflameHadesDye)
                        {
                            shopCustomPrice = price,
                            shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
                        });

                        shop.Add(new Item(ItemID.PhaseDye)
                        {
                            shopCustomPrice = price,
                            shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
                        });
                        shop.Add(new Item(ItemID.ShiftingSandsDye)
                        {
                            shopCustomPrice = price,
                            shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
                        });
                        shop.Add(new Item(ItemID.GelDye)
                        {
                            shopCustomPrice = price,
                            shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
                        });

                        shop.Add(new Item(ItemID.LivingFlameDye)
                        {
                            shopCustomPrice = price,
                            shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
                        });

                        shop.Add(new Item(ItemID.LivingRainbowDye)
                        {
                            shopCustomPrice = price,
                            shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
                        });

                        shop.Add(new Item(ItemID.LivingOceanDye)
                        {
                            shopCustomPrice = price,
                            shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
                        });

                        shop.Add(new Item(ItemID.MidnightRainbowDye)
                        {
                            shopCustomPrice = price,
                            shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
                        });


                        break;
                    }
                case NPCID.SkeletonMerchant:
                    {
                        shop.Add(new Item(ModContent.ItemType<Firebomb>())
                        {
                            shopCustomPrice = 5,
                            shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
                        });

                        shop.Add(new Item(ModContent.ItemType<EternalCrystal>())
                        {
                            shopCustomPrice = 2000,
                            shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
                        });
                        break;
                    }
                case NPCID.GoblinTinkerer:
                    {
                        shop.Add(new Item(ModContent.ItemType<Pulsar>())
                        {
                            shopCustomPrice = 800,
                            shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
                        });


                        shop.Add(new Item(ModContent.ItemType<ToxicCatalyzer>())
                        {
                            shopCustomPrice = 800,
                            shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
                        });
                        break;
                    }
                case NPCID.Dryad:
                    {
                        shop.Add(new Item(ModContent.ItemType<SeedBag>())
                        {
                            shopCustomPrice = 5,
                            shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
                        },
                        new Condition("", () => Main.LocalPlayer.HasItem(ItemID.Blowpipe) || Main.LocalPlayer.HasItem(ItemID.Blowgun) || Main.LocalPlayer.HasItem(ModContent.ItemType<ToxicShot>()) || Main.LocalPlayer.HasItem(ModContent.ItemType<AlienGun>()) || Main.LocalPlayer.HasItem(ModContent.ItemType<OmegaSquadRifle>())));
                        break;
                    }
                case NPCID.Mechanic:
                    {
                        foreach (NPCShop.Entry item in shop.ActiveEntries)
                        {
                            item.AddCondition(new Condition("", () => !ModContent.GetInstance<tsorcRevampConfig>().AdventureMode));
                        }

                        shop.Add(new Item(ModContent.ItemType<LoveRing>())
                        {
                            shopCustomPrice = 5000, 
                            shopSpecialCurrency = tsorcRevamp.DarkSoulCustomCurrencyId
                        });
                        break;
                    }
                case NPCID.Cyborg:
                    {
                        foreach (NPCShop.Entry item in shop.ActiveEntries)
                        {
                            if (item.Item.type == ItemID.DryRocket)
                            {
                                item.AddCondition(new Condition("", () => !ModContent.GetInstance<tsorcRevampConfig>().AdventureMode));
                            }
                            if (item.Item.type == ItemID.WetRocket)
                            {
                                item.AddCondition(new Condition("", () => !ModContent.GetInstance<tsorcRevampConfig>().AdventureMode));
                            }
                            if (item.Item.type == ItemID.LavaRocket)
                            {
                                item.AddCondition(new Condition("", () => !ModContent.GetInstance<tsorcRevampConfig>().AdventureMode));
                            }
                            if (item.Item.type == ItemID.HoneyRocket)
                            {
                                item.AddCondition(new Condition("", () => !ModContent.GetInstance<tsorcRevampConfig>().AdventureMode));
                            }
                        }
                        break;
                    }
                default: break;
            }
        }

        public override void SetDefaults(NPC npc)
        {
            //Set the default value of each if it has not been custom-tuned
            if (npc.boss)
            {
                //Disables all of them by default for bosses
                //Can be re-enabled in that specific bosses SetDefaults by giving these another value there
                if (Aggression == -1)
                {
                    Aggression = 0.0000001f;
                }
                if (Patience == -1)
                {
                    Patience = 1;
                }
                if (Cowardice == -1)
                {
                    Cowardice = 0;
                }
                if (Adeptness == -1)
                {
                    Adeptness = 0;
                }
                if (Swiftness == -1)
                {
                    Swiftness = 1;
                }
                if (CastingSpeed == -1)
                {
                    CastingSpeed = 1;
                }
                if (Strength == -1)
                {
                    Strength = 1;
                }
                if (Agility == -1)
                {
                    Agility = 0;
                }
            }
            else
            {
                if (Aggression == -1)
                {
                    Aggression = Main.rand.NextFloat(0.00001f, 2.5f);
                }
                if (Patience == -1)
                {
                    Patience = Main.rand.NextFloat(0.5f, 2);
                }
                if (Cowardice == -1)
                {
                    Cowardice = Main.rand.NextFloat(0, 0.3f);
                }
                if (Adeptness == -1)
                {
                    Adeptness = Main.rand.NextFloat(0, 0.3f);
                }
                if (Swiftness == -1)
                {
                    Swiftness = Main.rand.NextFloat(0.7f, 1.3f);
                }
                if (CastingSpeed == -1)
                {
                    CastingSpeed = Main.rand.NextFloat(0.6f, 1.4f);
                }
                if (Strength == -1)
                {
                    Strength = Main.rand.NextFloat(0.85f, 1.2f);
                }
                if (Agility == -1)
                {
                    Agility = Main.rand.NextFloat(0.2f, 0.6f);
                }
            }

            //Only mess with it if it's one of our bosses
            if (npc.ModNPC != null && npc.ModNPC.Mod == ModLoader.GetMod("tsorcRevamp"))
            {
                if (npc.boss && !Main.expertMode)
                {
                    //Bosses are 1.3x weaker in normal mode
                    //Doing it like this means we can simply set npc.lifeMax to exactly value we want their expert mode health to be, saving us a headache.
                    //Rounded, because casting to an int truncates it which causes slight inaccuracies later on
                    npc.lifeMax = (int)Math.Round(npc.lifeMax / 1.3f);
                }
                else
                {
                    if (npc.ModNPC.GetType().Namespace.Contains("SuperHardMode") && (npc.ModNPC.GetType() != typeof(NPCs.Bosses.SuperHardMode.Gwyn))
)
                    {
                        base.SetDefaults(npc);
                        npc.lifeMax = (int)(tsorcRevampWorld.SHMScale * npc.lifeMax);
                        npc.defense = (int)(tsorcRevampWorld.SubtleSHMScale * npc.defense);
                        npc.damage = (int)(tsorcRevampWorld.SubtleSHMScale * npc.damage);
                    }
                }
            }

            // Poise/stagger rollout: apply the central tuning table LAST so it's the source of truth for these enemies'
            // knockback-resist (flinch dial) + PoiseMax. Knights configure themselves in-file and aren't listed here.
            if (PoiseProfiles.TryGetValue(npc.type, out var poiseProfile))
            {
                npc.knockBackResist = poiseProfile.knockBackResist;
                PoiseMax = poiseProfile.poiseMax;
            }
            else if (npc.ModNPC is Puppets.PuppetNPC)
            {
                // Every puppet-style invader can channel Estus and must therefore have a poise
                // meter so the player can interrupt that channel. Per-NPC tuning set in the
                // content class remains authoritative; this only fills missing profiles.
                if (PoiseMax <= 0f)
                {
                    PoiseMax = npc.boss ? 120f : 30f;
                }
                if (npc.knockBackResist <= 0f)
                {
                    npc.knockBackResist = npc.boss ? 0.15f : 0.35f;
                }
            }
        }

        //This method lets us scale the stats of NPC's in expert mode.
        public override void ApplyDifficultyAndPlayerScaling(NPC npc, int numPlayers, float balance, float bossAdjustment)/* tModPorter Note: bossLifeScale -> balance (bossAdjustment is different, see the docs for details) */
        {
            if (Main.player[1] != null && Main.player[1].active && Main.player[1].name == "MPTestDummy")
            {
                numPlayers -= 1;
            }

            if (npc.ModNPC != null && npc.ModNPC.Mod == ModLoader.GetMod("tsorcRevamp") && npc.boss)
            {
                //Counter expert mode automatic scaling
                npc.lifeMax = (int)Math.Round(npc.lifeMax / 2f);

                //Add 70% to the boss's health per extra player
                //npc.lifeMax = (int)Math.Round(npc.lifeMax * (1f + (0.7f * ((float)bossLifeScale - 1f))));

                //Add our scaling
                npc.lifeMax = (int)(npc.lifeMax * (1f + ((numPlayers - 1f) * .4f))); // was .5
                return;
            }
        }
        public override void HitEffect(NPC npc, NPC.HitInfo hit)
        {
            Player LocalPlayer = Main.LocalPlayer;
            if (npc.active && !npc.friendly && Main.rand.NextBool((int)(100f / OrbOfDeception.EssenceThiefOnKillChance)) && npc.life <= 0)
            {
                if (LocalPlayer.HeldItem.type == ModContent.ItemType<OrbOfDeception>())
                {
                    Projectile.NewProjectile(Projectile.GetSource_None(), npc.Center, Vector2.Zero, ModContent.ProjectileType<StackDelivery>(), 0, 0, LocalPlayer.whoAmI, 0, 1);
                }
                else if (LocalPlayer.HeldItem.type == ModContent.ItemType<OrbOfFlame>())
                {
                    Projectile.NewProjectile(Projectile.GetSource_None(), npc.Center, Vector2.Zero, ModContent.ProjectileType<StackDelivery>(), 0, 0, LocalPlayer.whoAmI, 1, 1);
                }
                else if (LocalPlayer.HeldItem.type == ModContent.ItemType<OrbOfSpirituality>())
                {
                    Projectile.NewProjectile(Projectile.GetSource_None(), npc.Center, Vector2.Zero, ModContent.ProjectileType<StackDelivery>(), 0, 0, LocalPlayer.whoAmI, 2, 1);
                }
            }
        }

        public override void DrawEffects(NPC npc, ref Color drawColor)
        {
            if (ElectrocutedEffect)
            {
                int dust = Dust.NewDust(npc.position, npc.width, npc.height, 226, npc.velocity.X * 0f, npc.velocity.Y * 0f, 100, default(Color), .4f);
                Main.dust[dust].noGravity = true;
            }

            if (ElectrocutedEffect2)
            {
                int dust = Dust.NewDust(npc.position, npc.width, npc.height, 226, npc.velocity.X * 0f, npc.velocity.Y * 0f, 100, default(Color), .45f);
                Main.dust[dust].noGravity = true;
            }

            if (ElectrocutedEffect3)
            {
                int dust = Dust.NewDust(npc.position, npc.width, npc.height, 226, npc.velocity.X * 0f, npc.velocity.Y * 0f, 100, default(Color), .5f);
                Main.dust[dust].noGravity = true;
            }

            if (MorgulPoisoning)
            {
                drawColor = Color.Lerp(drawColor, Color.Green, 0.25f);

                if (Main.rand.NextBool(5)) 
                { 
                    int dustIndex = Dust.NewDust(npc.position, npc.width, npc.height, DustID.ToxicBubble, 0f, 0f, 100, Color.Green, 2f);
                    Main.dust[dustIndex].noGravity = true; 
                    Main.dust[dustIndex].velocity *= 0.75f;
                    Main.dust[dustIndex].fadeIn = 1.3f; 
                }
            }

            if (PolarisElectrocutedEffect)
            {
                for (int i = 0; i < 2; i++)
                {
                    int dust = Dust.NewDust(npc.position, npc.width, npc.height, 226, npc.velocity.X * 0f, npc.velocity.Y * 0f, 100, default(Color), .4f);
                    Main.dust[dust].noGravity = true;
                }
                if (Main.rand.NextBool(2))
                {
                    int dust = Dust.NewDust(npc.position, npc.width, npc.height, 226, npc.velocity.X * 0f, npc.velocity.Y * 0f, 100, default(Color), .4f);
                    Main.dust[dust].noGravity = false;
                }
            }

            if (ToxicCatDrain)
            {
                drawColor = Color.LimeGreen;
                Lighting.AddLight(npc.position, 0.125f, 0.23f, 0.065f);

                if (Main.rand.NextBool(10))
                {
                    int dust = Dust.NewDust(npc.position, npc.width, npc.height, 74, npc.velocity.X * 0f, npc.velocity.Y * 0f, 100, default(Color), .8f); ;
                    Main.dust[dust].velocity *= 0f;
                    Main.dust[dust].noGravity = true;
                    Main.dust[dust].velocity += npc.velocity;
                    Main.dust[dust].fadeIn = 1f;
                }
            }

            if (ViruCatDrain)
            {
                drawColor = Color.LimeGreen;
                Lighting.AddLight(npc.position, 0.125f, 0.23f, 0.065f);

                if (Main.rand.NextBool(6))
                {
                    int dust = Dust.NewDust(npc.position, npc.width, npc.height, 74, npc.velocity.X * 0f, npc.velocity.Y * 0f, 100, default(Color), .8f); ;
                    Main.dust[dust].velocity *= 0f;
                    Main.dust[dust].noGravity = true;
                    Main.dust[dust].velocity += npc.velocity;
                    Main.dust[dust].fadeIn = 1f;
                }
            }

            if (BiohazardDrain)
            {
                drawColor = Color.LimeGreen;
                Lighting.AddLight(npc.position, 0.125f, 0.23f, 0.065f);

                if (Main.rand.NextBool(2))
                {
                    int dust = Dust.NewDust(npc.position, npc.width, npc.height, 74, npc.velocity.X * 0f, -2f, 100, default(Color), .8f); ;
                    Main.dust[dust].velocity *= 0f;
                    Main.dust[dust].noGravity = true;
                    Main.dust[dust].velocity += npc.velocity;
                    Main.dust[dust].fadeIn = 1f;
                }
            }

            if (WitchkingCurse)
            {
                drawColor = Color.OrangeRed;
                Lighting.AddLight(npc.position, 0.23f, 0.125f, 0.065f);

                if (Main.rand.NextBool(6))
                {
                    int dust = Dust.NewDust(npc.position, npc.width, npc.height, 5, npc.velocity.X * 0f, npc.velocity.Y * 0f, 100, default(Color), 1f); ;
                    Main.dust[dust].velocity *= 0f;
                    Main.dust[dust].noGravity = true;
                    Main.dust[dust].velocity += npc.velocity;
                    Main.dust[dust].fadeIn = 1f;
                }
            }

            if (AbyssalSinking)
            {
                Lighting.AddLight(npc.position, 0.015f, 0.155f, 0.165f);
                if (Main.rand.NextBool(6))
                {
                    int dust = Dust.NewDust(npc.position, npc.width, npc.height, 217, npc.velocity.X * 0f, npc.velocity.Y * 0f, 100, default(Color), 1.8f); ;
                    Main.dust[dust].velocity *= 0.5f;
                    Main.dust[dust].noGravity = false;
                    Main.dust[dust].velocity += npc.velocity;
                    Main.dust[dust].fadeIn = 1f;
                }
            }

            if (CrescentMoonlight)
            {
                drawColor = Color.White;

                int dust = Dust.NewDust(npc.position, npc.width, npc.height, 164, npc.velocity.X * 0f, 0f, 100, default(Color), 1f); ;
                Main.dust[dust].velocity *= 0f;
                Main.dust[dust].noGravity = false;
                Main.dust[dust].velocity += npc.velocity;
            }

            if (Soulstruck)
            {
                Lighting.AddLight(npc.Center, .4f, .4f, .850f);

                if (Main.rand.NextBool(6))
                {
                    int dust = Dust.NewDust(npc.position, npc.width, npc.height, 68, 0, 0, 30, default(Color), 1.25f);
                    Main.dust[dust].velocity *= 0f;
                    Main.dust[dust].noGravity = true;
                    Main.dust[dust].velocity += npc.velocity;
                }
            }
        }

        //AIWorm(NPC npc, int headType, int[] bodyTypes, int tailType, int wormLength = 3, float partDistanceAddon = 0f, float maxSpeed = 8f, float gravityResist = 0.07f, bool fly = false, bool split = false, bool ignoreTiles = false, bool spawnTileDust = true, bool soundEffects = true)

        /*
                 * A cleaned up (and edited) copy of Worm AI.

                 * headType/tailType : the type of the head, body, and tail of the worm, respectively.
                 * bodyTypes: An array of the body types. NOTE: Array must at least be as long as the body length - 2!
                 * wormLength : the total length of the worm.
                 * partDistanceAddon : and addon to the distance between parts of the worm.
                 * maxSpeed : the fastest the worm can accellerate to.
                 * gravityResist : how much resistance on the X axis the worm has when it is out of tiles. was 0.07f
                    //higher values cause the wvyern's 'gravity' towards the player to increase
                    //lower values basically == longer passes
                 * fly : If true, acts like a Wvyern.
                 * split : If true, worm will split when parts of it die.
                 * ignoreTiles : If true, Allows the worm to move outside of tiles as if it were in them. (ignored if fly is true)
                 * spawnTileDust : If true, worm will spawn tile dust when it digs through tiles.
                 * soundEffects : If true, will produce a digging sound when nearing the player.

                 * that array works like this: say you have a worm that is 5 segments long
                 * you would make the body array have 3 ids in it and they would go in order they would appear on the worm from the head
                 * the array *must* be 2 less than the total length of the worm or it will not work
        */


        //ai[0] = ID of piece behind it
        //ai[1] = ID of piece ahead of it
        //ai[2] = Relates to length of worms
        //ai[3] = ID of worm head
        //npc.localAI[0] = place in the queue to sync itself, used to spread the syncing out
        #region AIWorm
        public static void AIWorm(NPC npc, int headType, int[] bodyTypes, int tailType, int wormLength = 3, float partDistanceAddon = 0f, float maxSpeed = 8f, float gravityResist = 0.07f, bool fly = false, bool split = false, bool ignoreTiles = false, bool spawnTileDust = true, bool soundEffects = true)
        {
            //Flip sprite so it's always facing the right way            
            if (npc.type == headType)
            {
                if (npc.velocity.X < 0f || Math.Abs(npc.velocity.X) < 0.1f)
                {
                    npc.spriteDirection = 1;
                }
                else if (npc.velocity.X > 0f)
                {
                    npc.spriteDirection = -1;
                }
            }
            else
            {
                if (npc.position.X > Main.npc[(int)npc.ai[1]].position.X || Math.Abs(npc.position.X - Main.npc[(int)npc.ai[1]].position.X) < 0.1f)
                {
                    npc.spriteDirection = 1;
                }
                if (npc.position.X < Main.npc[(int)npc.ai[1]].position.X)
                {
                    npc.spriteDirection = -1;
                }
            }
            //If it splits, ignore the health of the head and keep its own healthbar
            //If it doesn't, set its real health to the health of the head
            if (split)
            {
                npc.realLife = -1;
            }
            else if (npc.ai[3] > 0f)
            {
                npc.realLife = (int)npc.ai[3];
            }

            //Don't do *any* spawning if we're a multiplayer client
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                //Tick down the sync counter, and if it hits 1 then sync them.
                if (npc.localAI[0] == 1 && npc.localAI[0] > 0)
                {
                    npc.netUpdate = true;
                    npc.localAI[0] = -1;
                }
                else
                {
                    npc.localAI[0]--;
                }

                //And the piece behind it does not exist
                if (npc.ai[0] == 0f)
                {
                    //If we're a head and flying type, spawn the rest of the worm
                    if (fly && npc.type == headType)
                    {
                        //Set its the head's head id, actual health, and ID to itself
                        npc.ai[3] = (float)npc.whoAmI;
                        npc.realLife = npc.whoAmI;

                        //Store the head's index in npcID. This will get updated as we go through each piece.
                        int npcID = npc.whoAmI;

                        //Spawn the rest of the worm. For each piece...
                        for (int m = 0; m < wormLength - 1; m++)
                        {
                            //If we're the last piece, make the worm type the tail. If not, make it the body type corrosponding to its position on the list
                            int npcType = (m == wormLength - 2 ? tailType : bodyTypes[m]);

                            //Spawn the npc
                            int newnpcID = NPC.NewNPC(npc.GetSource_FromAI(), (int)(npc.Center.X), (int)(npc.Center.Y), npcType, npc.whoAmI);

                            //Set the new piece's Head ID to the head
                            Main.npc[newnpcID].ai[3] = (float)npc.whoAmI;

                            //Set its real health to the head's
                            Main.npc[newnpcID].realLife = npc.whoAmI;

                            //Set its "previous piece id" to the id of the previous spawned piece
                            Main.npc[newnpcID].ai[1] = (float)npcID;

                            //Set the previous piece's "next piece id" to the id of the newly spawned piece
                            Main.npc[npcID].ai[0] = (float)newnpcID;

                            //Set their localAI to a number that grows as each segment is spawned
                            Main.npc[npcID].localAI[0] = 2 + (m * 2);

                            //Ask the server to sync it right away (might be triggering the net spam limit and causing the issues!!)
                            //NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, newnpcID);

                            //Store the current piece's ID in npcID, so that the next piece can use it
                            npcID = newnpcID;
                        }
                        //Immediately update
                        npc.netUpdate = true;
                    }
                    //If we're a grounded type and not the tail, just spawn the piece behind itself
                    else if (npc.type != tailType)
                    {
                        if (npc.type == headType)
                        {
                            if (!split)
                            {
                                npc.ai[3] = (float)npc.whoAmI;
                                npc.realLife = npc.whoAmI;
                            }
                            npc.ai[2] = (float)(wormLength - 2);
                            int nextPiece = (bodyTypes.Length == 0 ? tailType : bodyTypes[0]);
                            npc.ai[0] = (float)NPC.NewNPC(npc.GetSource_FromAI(), (int)(npc.Center.X), (int)(npc.Center.Y), nextPiece, npc.whoAmI);
                        }
                        else
                        if ((npc.type != headType && npc.type != tailType) && npc.ai[2] > 0f)
                        {
                            npc.ai[0] = (float)NPC.NewNPC(npc.GetSource_FromAI(), (int)(npc.Center.X), (int)(npc.Center.Y), bodyTypes[wormLength - 3 - (int)npc.ai[2]], npc.whoAmI);
                        }
                        else
                        {
                            npc.ai[0] = (float)NPC.NewNPC(npc.GetSource_FromAI(), (int)(npc.Center.X), (int)(npc.Center.Y), tailType, npc.whoAmI);
                        }
                        if (!split)
                        {
                            Main.npc[(int)npc.ai[0]].ai[3] = npc.ai[3];
                            Main.npc[(int)npc.ai[0]].realLife = npc.realLife;
                        }
                        Main.npc[(int)npc.ai[0]].ai[1] = (float)npc.whoAmI;
                        Main.npc[(int)npc.ai[0]].ai[2] = npc.ai[2] - 1f;
                        npc.netUpdate = true;
                    }
                }

                //if npc can split, check if pieces are dead and if so split.
                if (split)
                {
                    //If the piece in front and behind it are dead, then die too
                    if (!Main.npc[(int)npc.ai[1]].active && !Main.npc[(int)npc.ai[0]].active)
                    {
                        npc.life = 0;
                        npc.HitEffect(0, 10.0);
                        npc.active = false;
                    }

                    //If it's a head and the piece behind it dies, then die
                    if (npc.type == headType && !Main.npc[(int)npc.ai[0]].active)
                    {
                        npc.life = 0;
                        npc.HitEffect(0, 10.0);
                        npc.active = false;
                    }

                    //If it's a tail and the piece in front of it dies, then die
                    if (npc.type == tailType && !Main.npc[(int)npc.ai[1]].active)
                    {
                        npc.life = 0;
                        npc.HitEffect(0, 10.0);
                        npc.active = false;
                    }

                    //If the piece isn't a head or tail, and the piece in front of it dies, then become a head
                    if ((npc.type != headType && npc.type != tailType) && !Main.npc[(int)npc.ai[1]].active)
                    {
                        npc.type = headType;
                        int npcID = npc.whoAmI;
                        float lifePercent = (float)npc.life / (float)npc.lifeMax;
                        float lastPiece = npc.ai[0];
                        npc.SetDefaults(npc.type);
                        npc.life = (int)((float)npc.lifeMax * lifePercent);
                        npc.ai[0] = lastPiece;
                        npc.netUpdate = true;
                        npc.whoAmI = npcID;
                    }

                    //If the piece isn't a head or tail, and the piece behind it dies, then become a head
                    else if ((npc.type != headType && npc.type != tailType) && !Main.npc[(int)npc.ai[0]].active)
                    {
                        npc.type = tailType;
                        int npcID = npc.whoAmI;
                        float lifePercent = (float)npc.life / (float)npc.lifeMax;
                        float lastPiece = npc.ai[1];
                        npc.SetDefaults(npc.type);
                        npc.life = (int)((float)npc.lifeMax * lifePercent);
                        npc.ai[1] = lastPiece;
                        npc.netUpdate = true;
                        npc.whoAmI = npcID;
                    }
                }

                //If it can't split, die if it is incomplete 
                else
                {
                    //If it's not a head and the piece in front of it is dead (or the wrong aiStyle, just in-case a new npc took its slot) then die
                    if (npc.type != headType && (!Main.npc[(int)npc.ai[1]].active || Main.npc[(int)npc.ai[1]].aiStyle != npc.aiStyle))
                    {
                        npc.life = 0;
                        npc.HitEffect(0, 10.0);

                        npc.active = false;
                    }

                    //If it's not a tail and the piece behind it is dead then die
                    if (npc.type != tailType && (!Main.npc[(int)npc.ai[0]].active || Main.npc[(int)npc.ai[0]].aiStyle != npc.aiStyle))
                    {
                        npc.life = 0;
                        npc.HitEffect(0, 10.0);

                        npc.active = false;
                    }
                }
                /**
                if (!npc.active && Main.netMode == NetmodeID.Server) 
                { 
                    NetMessage.SendData(28, -1, -1, "", npc.whoAmI, 1, 0f, 0f, -1); 
                }**/
            }
            int tileX = (int)(npc.position.X / 16f) - 1;
            int tileCenterX = (int)((npc.Center.X) / 16f) + 2;
            int tileY = (int)(npc.position.Y / 16f) - 1;
            int tileCenterY = (int)((npc.Center.Y) / 16f) + 2;
            if (tileX < 0) { tileX = 0; }
            if (tileCenterX > Main.maxTilesX) { tileCenterX = Main.maxTilesX; }
            if (tileY < 0) { tileY = 0; }
            if (tileCenterY > Main.maxTilesY) { tileCenterY = Main.maxTilesY; }
            bool canMove = false;
            if (fly || ignoreTiles) { canMove = true; }


            if (!canMove || spawnTileDust)
            {
                for (int tX = tileX; tX < tileCenterX; tX++)
                {
                    for (int tY = tileY; tY < tileCenterY; tY++)
                    {
                        if (Main.tile[tX, tY] != null && ((Main.tile[tX, tY].HasTile && (Main.tileSolid[(int)Main.tile[tX, tY].TileType] || (Main.tileSolidTop[(int)Main.tile[tX, tY].TileType] && Main.tile[tX, tY].TileFrameY == 0))) || Main.tile[tX, tY].LiquidAmount > 64))
                        {
                            Vector2 tPos;
                            tPos.X = (float)(tX * 16);
                            tPos.Y = (float)(tY * 16);
                            if (npc.position.X + (float)npc.width > tPos.X && npc.position.X < tPos.X + 16f && npc.position.Y + (float)npc.height > tPos.Y && npc.position.Y < tPos.Y + 16f)
                            {
                                canMove = true;
                                if (spawnTileDust && (Main.rand.Next(100)) == 0 && Main.tile[tX, tY].HasTile)
                                {
                                    WorldGen.KillTile(tX, tY, true, true, false);
                                }
                            }
                        }
                    }
                }
            }


            if (!canMove && npc.type == headType)
            {
                Rectangle rectangle = new Rectangle((int)npc.position.X, (int)npc.position.Y, npc.width, npc.height);
                int playerCheckDistance = 1000;
                bool canMove2 = true;
                for (int m3 = 0; m3 < 255; m3++)
                {
                    if (Main.player[m3].active)
                    {
                        Rectangle rectangle2 = new Rectangle((int)Main.player[m3].position.X - playerCheckDistance, (int)Main.player[m3].position.Y - playerCheckDistance, playerCheckDistance * 2, playerCheckDistance * 2);
                        if (rectangle.Intersects(rectangle2))
                        {
                            canMove2 = false;
                            break;
                        }
                    }
                }
                if (canMove2) { canMove = true; }
            }



            Vector2 npcCenter = npc.Center;
            float playerCenterX = Main.player[npc.target].Center.X;
            float playerCenterY = Main.player[npc.target].Center.Y;
            playerCenterX = (float)((int)(playerCenterX / 16f) * 16); playerCenterY = (float)((int)(playerCenterY / 16f) * 16);
            npcCenter.X = (float)((int)(npcCenter.X / 16f) * 16); npcCenter.Y = (float)((int)(npcCenter.Y / 16f) * 16);
            playerCenterX -= npcCenter.X; playerCenterY -= npcCenter.Y;
            float dist = (float)Math.Sqrt((double)(playerCenterX * playerCenterX + playerCenterY * playerCenterY));
            if (npc.ai[1] > 0f && npc.ai[1] < (float)Main.npc.Length)
            {

                npcCenter = npc.Center;
                float offsetX = Main.npc[(int)npc.ai[1]].Center.X - npcCenter.X;
                float offsetY = Main.npc[(int)npc.ai[1]].Center.Y - npcCenter.Y;

                npc.rotation = (float)Math.Atan2((double)offsetY, (double)offsetX) + 1.57f;
                dist = (float)Math.Sqrt((double)(offsetX * offsetX + offsetY * offsetY));
                dist = (dist - (float)npc.width - (float)partDistanceAddon) / dist;
                offsetX *= dist;
                offsetY *= dist;
                npc.velocity = default(Vector2);
                npc.position.X += offsetX;
                npc.position.Y += offsetY;
            }
            else
            {
                if (!canMove)
                {
                    npc.velocity.Y += 0.11f;
                    if (npc.velocity.Y > maxSpeed) { npc.velocity.Y = maxSpeed; }
                    if ((double)(Math.Abs(npc.velocity.X) + Math.Abs(npc.velocity.Y)) < (double)maxSpeed * 0.4)
                    {
                        if (npc.velocity.X < 0f) { npc.velocity.X -= gravityResist * 1.1f; } else { npc.velocity.X += gravityResist * 1.1f; }
                    }
                    else
                    if (npc.velocity.Y == maxSpeed)
                    {
                        if (npc.velocity.X < playerCenterX) { npc.velocity.X += gravityResist; }
                        else
                        if (npc.velocity.X > playerCenterX) { npc.velocity.X -= gravityResist; }
                    }
                    else
                    if (npc.velocity.Y > 4f)
                    {
                        if (npc.velocity.X < 0f) { npc.velocity.X += gravityResist * 0.9f; } else { npc.velocity.X -= gravityResist * 0.9f; }
                    }
                }
                else
                {
                    if (soundEffects && npc.soundDelay == 0)
                    {
                        float distSoundDelay = dist / 40f;
                        if (distSoundDelay < 10f) { distSoundDelay = 10f; }
                        if (distSoundDelay > 20f) { distSoundDelay = 20f; }
                        npc.soundDelay = (int)distSoundDelay;
                        Terraria.Audio.SoundEngine.PlaySound(SoundID.Roar, npc.Center);
                    }
                    dist = (float)Math.Sqrt((double)(playerCenterX * playerCenterX + playerCenterY * playerCenterY));
                    float absPlayerCenterX = Math.Abs(playerCenterX);
                    float absPlayerCenterY = Math.Abs(playerCenterY);
                    float newSpeed = maxSpeed / dist;
                    playerCenterX *= newSpeed;
                    playerCenterY *= newSpeed;
                    bool dontFall = false;
                    if (fly)
                    {
                        if (((npc.velocity.X > 0f && playerCenterX < 0f) || (npc.velocity.X < 0f && playerCenterX > 0f) || (npc.velocity.Y > 0f && playerCenterY < 0f) || (npc.velocity.Y < 0f && playerCenterY > 0f)) && Math.Abs(npc.velocity.X) + Math.Abs(npc.velocity.Y) > gravityResist / 2f && dist < 300f)
                        {
                            dontFall = true;
                            if (Math.Abs(npc.velocity.X) + Math.Abs(npc.velocity.Y) < maxSpeed) { npc.velocity *= 1.1f; }
                        }
                    }
                    if (!dontFall)
                    {
                        if ((npc.velocity.X > 0f && playerCenterX > 0f) || (npc.velocity.X < 0f && playerCenterX < 0f) || (npc.velocity.Y > 0f && playerCenterY > 0f) || (npc.velocity.Y < 0f && playerCenterY < 0f))
                        {
                            if (npc.velocity.X < playerCenterX) { npc.velocity.X += gravityResist; }
                            else
                            if (npc.velocity.X > playerCenterX) { npc.velocity.X -= gravityResist; }
                            if (npc.velocity.Y < playerCenterY) { npc.velocity.Y += gravityResist; }
                            else
                            if (npc.velocity.Y > playerCenterY) { npc.velocity.Y -= gravityResist; }
                            if ((double)Math.Abs(playerCenterY) < (double)maxSpeed * 0.2 && ((npc.velocity.X > 0f && playerCenterX < 0f) || (npc.velocity.X < 0f && playerCenterX > 0f)))
                            {
                                if (npc.velocity.Y > 0f) { npc.velocity.Y += gravityResist * 2f; } else { npc.velocity.Y -= gravityResist * 2f; }
                            }
                            if ((double)Math.Abs(playerCenterX) < (double)maxSpeed * 0.2 && ((npc.velocity.Y > 0f && playerCenterY < 0f) || (npc.velocity.Y < 0f && playerCenterY > 0f)))
                            {
                                if (npc.velocity.X > 0f) { npc.velocity.X += gravityResist * 2f; } else { npc.velocity.X -= gravityResist * 2f; }
                            }
                        }
                        else
                        if (absPlayerCenterX > absPlayerCenterY)
                        {
                            if (npc.velocity.X < playerCenterX) { npc.velocity.X += gravityResist * 1.1f; }
                            else
                            if (npc.velocity.X > playerCenterX) { npc.velocity.X -= gravityResist * 1.1f; }

                            if ((double)(Math.Abs(npc.velocity.X) + Math.Abs(npc.velocity.Y)) < (double)maxSpeed * 0.5)
                            {
                                if (npc.velocity.Y > 0f) { npc.velocity.Y += gravityResist; } else { npc.velocity.Y -= gravityResist; }
                            }
                        }
                        else
                        {
                            if (npc.velocity.Y < playerCenterY) { npc.velocity.Y += gravityResist * 1.1f; }
                            else
                            if (npc.velocity.Y > playerCenterY) { npc.velocity.Y -= gravityResist * 1.1f; }
                            if ((double)(Math.Abs(npc.velocity.X) + Math.Abs(npc.velocity.Y)) < (double)maxSpeed * 0.5)
                            {
                                if (npc.velocity.X > 0f) { npc.velocity.X += gravityResist; } else { npc.velocity.X -= gravityResist; }
                            }
                        }
                    }
                }
                npc.rotation = (float)Math.Atan2((double)npc.velocity.Y, (double)npc.velocity.X) + 1.57f;
                if (npc.type == headType)
                {
                    if (canMove)
                    {
                        if (npc.localAI[0] != 1f) { npc.netUpdate = true; }
                        npc.localAI[0] = 1f;
                    }
                    else
                    {
                        if (npc.localAI[0] != 0f) { npc.netUpdate = true; }
                        npc.localAI[0] = 0f;
                    }
                    if (((npc.velocity.X > 0f && npc.oldVelocity.X < 0f) || (npc.velocity.X < 0f && npc.oldVelocity.X > 0f) || (npc.velocity.Y > 0f && npc.oldVelocity.Y < 0f) || (npc.velocity.Y < 0f && npc.oldVelocity.Y > 0f)) && !npc.justHit)
                    {
                        npc.netUpdate = true;
                        return;
                    }
                }
            }
        }

        #endregion
        /*public override void PostDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            var TownAnimals = new HashSet<int> {NPCID.TownDog, NPCID.TownCat, NPCID.TownBunny};
            Texture2D Hat = null;
            if (Main.xMas)
            {
                Hat = Main.Assets.Request<Texture2D>("Images/Item_588").Value;
            }
            if (npc.townNPC && !TownAnimals.Contains(npc.type))
            {
                Vector2 position = npc.Center - screenPos - new Vector2(0f, (npc.height / 2));//
                position -= new Vector2(0f, 4f);
                SpriteEffects spriteEffects = SpriteEffects.None;
                if (npc.direction == -1)
                {
                    spriteEffects = SpriteEffects.FlipHorizontally;
                }
                spriteBatch.Draw(Hat, position, null, Color.White, 0f, new Vector2(Hat.Width / 2, Hat.Height / 2), npc.scale, spriteEffects, 0f);
            }
        }*/
    }
}

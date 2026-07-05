using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.NPCs.AI;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.NPCs.Invaders
{
    /// <summary>
    /// Abstract base for human-sized invader enemies rendered via the player draw pipeline.
    ///
    /// WEAPON NOTES
    /// ─────────────────────────────────────────────────────────────────
    /// • The puppet Player drives arm animation (inventory[0] holds weapon, noUseGraphic=true).
    ///   The actual weapon sprite is drawn manually so we control its exact screen position.
    /// • Hand position is calculated from the current swing/stab arc so the sprite
    ///   tracks the animated arm naturally.
    ///
    /// PROJECTILE NOTES
    /// ─────────────────────────────────────────────────────────────────
    /// • Any projectile fired BY an invader must be hostile=true, friendly=false.
    ///   Player-friendly projectiles will damage the invader itself and not the player.
    ///   Create a Projectiles/Enemy/ copy for every weapon the invader uses at range.
    /// </summary>
    public abstract class InvaderNPC : ModNPC
    {
        public override string Texture => "tsorcRevamp/NPCs/Invaders/InvaderPlaceholder";

        // ── Invasion banner ───────────────────────────────────────────────────────
        /// <summary>
        /// The name shown in the "INVADED BY ____" banner when this invader spawns.
        /// Override in each concrete class.  Rendered in uppercase automatically.
        /// </summary>
        protected virtual string InvaderTitle => "UNKNOWN INVADER";

        // ── Layer coordination ────────────────────────────────────────────────────
        /// <summary>
        /// Set to <c>this</c> for the exact duration of a <see cref="Main.PlayerRenderer.DrawPlayer"/>
        /// call so that <see cref="InvaderWeaponDrawLayer"/> knows which invader to draw for.
        /// Null at all other times — the layer is a no-op for normal players.
        /// </summary>
        internal static InvaderNPC DrawingPuppetFor;

        /// <summary>Draw color captured in PreDraw, read by <see cref="InvaderWeaponDrawLayer"/>.</summary>
        internal Color _layerDrawColor;

        // ── Puppet ────────────────────────────────────────────────────────────────
        private Player _puppet;
        private Item _meleeItemCache;
        private Item _rangedItemCache;
        private Item _secondaryRangedItemCache;
        private Item _magicItemCache;
        private int  _cachedMeleeType  = -1;
        private int  _cachedRangedType = -1;
        private int  _cachedSecondaryRangedType = -1;
        private int  _cachedMagicType = -1;

        // ── Loadout ───────────────────────────────────────────────────────────────
        protected abstract int HeadArmorItemType  { get; }
        protected abstract int BodyArmorItemType  { get; }
        protected abstract int LegsArmorItemType  { get; }
        protected abstract int MeleeWeaponItemType  { get; }   // -1 = none
        protected abstract int RangedWeaponItemType { get; }   // -1 = none
        protected virtual  int MagicWeaponItemType  => -1;
        /// <summary>Item type of the polearm/spear used for the SpearAttack phase.  -1 = no spear attack.</summary>
        protected virtual  int SpearWeaponItemType  => -1;
        protected abstract int MeleeDamage  { get; }
        protected abstract int RangedDamage { get; }
        protected virtual  int SpearDamage  => 0;
        protected virtual  int MagicDamage  => 0;

        // ── Tuning ────────────────────────────────────────────────────────────────
        protected virtual float TopSpeed       => 2.5f;
        protected virtual float Acceleration   => 0.09f;
        protected virtual float BrakingPower   => 0.22f;
        protected virtual float RunDistance    => 420f;
        protected virtual float RunSpeedMult   => 1.35f;
        protected virtual int   TeleportTelegraphTicks => 140;
        protected virtual int   TeleportDustTypeId     => DustID.Smoke;
        protected virtual Color TeleportDustTint       => Color.White;
        protected virtual float TeleportDustScale      => 0.8f;
        protected virtual int   TeleportDustCount      => 20;

        protected virtual float MeleeRange     => 80f;
        protected virtual float StabRange      => 160f;   // distance at which stab is preferred
        protected virtual float RangedRange    => 520f;
        protected virtual float MinRangedRange => 200f;   // won't use ranged if closer than this

        /// <summary>Thickness (px) of the tracked melee "blade" capsule used by <see cref="TickBladeHit"/>.
        /// Chosen generously enough that the fastest continuous motion (Spin, +0.28 rad/tick) can't
        /// tunnel a stationary target between ticks at typical combo reach — override per weapon
        /// for a visibly thinner/wider blade once playtested.</summary>
        protected virtual float MeleeBladeWidth => 48f;

        // ── Swing-polish opt-ins (2026-07) ──────────────────────────────────────────
        // All default OFF so every existing invader keeps its exact current swing behavior.
        // An invader flips these on individually to pilot the new shared swing-arc math
        // (ported from the BroadswordRework player-weapon system) without touching anyone else.

        /// <summary>Ease combo-swing rotation through <see cref="SwingEase.DefaultCurve"/> (slow
        /// wind-up, fast strike, slight settle) instead of a constant-speed linear lerp. Purely
        /// reshapes the timing of the (already tracked) blade capsule — no reach/damage change.</summary>
        protected virtual bool UseSwingEasing => false;

        /// <summary>Flip which direction a combo's arc runs on alternating swings (same arc shape,
        /// mirrored start/end) so the same combo doesn't read identically every single time.</summary>
        protected virtual bool UseAlternateFlip => false;

        /// <summary>Bias a combo's arc center toward the target's relative height, captured once
        /// per swing at combo start — so the invader's swing actually reaches for an airborne or
        /// crouched target instead of always arcing level with the ground.</summary>
        protected virtual bool UseAimAdaptiveArc => false;

        /// <summary>Draw an arc-shaped slash swoosh (see <see cref="DrawSlashToLayer"/>) tinted to
        /// <see cref="SlashVFXColor"/>, tracking the same angle/reach TickBladeHit uses for real hit
        /// detection — so the VFX is an honest preview of the actual hitbox, not just decoration.</summary>
        protected virtual bool HasSlashVFX => false;

        /// <summary>Tint for the slash swoosh when <see cref="HasSlashVFX"/> is true.</summary>
        protected virtual Color SlashVFXColor => Color.White;

        // Telegraph minimum: 30 ticks = 0.5 s.  Long enough for the player to read the
        // wind-up arc (sword raises, holds at apex, then swings).  Heavier attacks override
        // upward; lighter attacks should not go below this floor.
        protected virtual int MeleeTelegraphTicks  => 35;
        // MeleeAttackTicks is the fallback swing duration when item useAnimation is unavailable
        // within the attack phase rather than bleeding into recovery.
        protected virtual int MeleeAttackTicks     => DefaultWeaponAnimMax;
        protected virtual int MeleeRecoveryTicks   => 25;

        protected virtual int StabTelegraphTicks   => 38;
        protected virtual int StabAttackTicks      => 8;
        protected virtual int StabRecoveryTicks    => 32;

        protected virtual int RangedTelegraphTicks => 38;
        protected virtual int RangedAttackTicks    => 10;
        protected virtual int RangedRecoveryTicks  => 55;

        /// How many ticks must pass after a ranged burst before ranged is allowed again.
        /// Gives the invader time to close distance and fight in melee first.
        protected virtual int RangedCooldownAfterUse => 300;
        /// How many ticks must pass after a stab before another stab is allowed.
        /// Prevents instant re-stab when the player dodgerolls through.
        protected virtual int StabCooldownAfterUse   => 120;
        /// Velocity multiplier applied during the stab lunge (StabAttack phase).
        /// Base × TopSpeed px/frame.  Lower values = shorter lunge; higher = more aggressive dash.
        protected virtual float StabLungeSpeedMult => 2.0f;
        /// Maximum number of ranged shots per engagement burst (rolls 1–N each time).
        protected virtual int MaxRangedBurst         => 3;
        protected virtual int SingleRangedBurstChance => 0;

        protected virtual bool CanStab => false;

        // ── Ranged animation style ────────────────────────────────────────────────
        /// <summary>
        /// Controls body-frame poses and weapon-rotation arc during the Ranged phases.
        /// Override per subclass to match the weapon being used.
        ///   Throw    — overhand throw (shurikens, knives): arm lifts up then swings forward.
        ///   Crossbow — horizontal aim: arm extends forward at Use3, quick click/fire (useAnimation ≈ 22).
        ///   Bow      — diagonal draw: arm rises from Use3 → Use1 over the telegraph, snaps forward at release (useAnimation ≈ 60).
        /// </summary>
        protected enum RangedStyle { Throw, Crossbow, Bow }
        /// <summary>Animation style for the primary ranged weapon.  Override per subclass.</summary>
        protected virtual RangedStyle RangedAnimStyle => RangedStyle.Throw;

        // ── Secondary ranged weapon (optional) ───────────────────────────────────
        // When set, each burst randomly picks primary OR secondary based on range bands and chance.
        // Typical use: close/medium throw (primary) + long-range crossbow or bow (secondary).
        protected virtual int         SecondaryRangedWeaponItemType  => -1;    // -1 = no secondary
        protected virtual int         SecondaryRangedDamage          => 0;
        protected virtual RangedStyle SecondaryRangedAnimStyle       => RangedStyle.Crossbow;
        protected virtual float       SecondaryRangedRange           => 700f;
        protected virtual float       SecondaryRangedMinRange        => 300f;
        protected virtual int         SecondaryRangedTelegraphTicks  => 25;
        protected virtual int         SecondaryRangedAttackTicks     => 10;
        protected virtual int         SecondaryRangedRecoveryTicks   => 50;
        protected virtual int         SecondaryRangedCooldownAfterUse => 300;
        /// <summary>Max shots in a secondary burst (rolls 1–N).  Default 1 — suits crossbow/sniper.</summary>
        protected virtual int         SecondaryMaxRangedBurst        => 1;
        /// <summary>Chance (0–100) of using secondary when both primary and secondary are in range.</summary>
        protected virtual int         SecondaryRangedChance          => 50;
        /// <summary>Chance (0–100) of planting feet during secondary ranged attack.</summary>
        protected virtual int         SecondaryStandingRangedChance  => 70;
        protected virtual Color       SecondaryRangedFlashColor      => Color.White;

        /// <summary>Gate for using the secondary ranged weapon at all (both the normal band and the
        /// panic backhop).  Override to unlock the secondary conditionally — e.g. only below half HP.
        /// Default true.</summary>
        protected virtual bool SecondaryRangedAvailable => true;

        // ── Secondary-ranged "panic backhop" (optional) ───────────────────────────
        // A proactive spacing tool: when the player closes to within SecondaryRangedBackhopRange and
        // the secondary weapon is off cooldown, the invader hops AWAY (still facing the player) and
        // fires a single secondary shot — independent of the normal SecondaryRangedMinRange band, so it
        // works point-blank.  Default range 0 = disabled (no behavior change for other invaders).
        protected virtual float SecondaryRangedBackhopRange   => 0f;
        /// <summary>Chance (0–100) to backhop when the player is inside <see cref="SecondaryRangedBackhopRange"/>.</summary>
        protected virtual int   SecondaryRangedBackhopChance  => 50;
        /// <summary>Horizontal hop-away speed (px/frame).</summary>
        protected virtual float SecondaryRangedBackhopSpeed   => 6f;
        /// <summary>Upward hop speed (px/frame) for the little arc.</summary>
        protected virtual float SecondaryRangedBackhopUpSpeed => 5f;

        // ── Secondary ranged burst patterns (optional) ────────────────────────────
        // When non-null, replaces the simple SecondaryMaxRangedBurst random-shot system.
        // Each entry is an array of inter-shot pause durations (ticks).
        //   total shots in that pattern = pauses.Length + 1
        //   e.g. new int[]{30,60} → 3 shots: fire, wait 30, fire, wait 60, fire.
        //        new int[]{}      → single shot.
        // Leave as null (default) to keep the classic random-burst system.
        protected virtual int[][]  SecondaryRangedBurstPatterns        => null;
        /// <summary>Extra telegraph ticks added to the base secondary telegraph for each pattern.  Indexed by pattern.</summary>
        protected virtual int[]    SecondaryRangedBurstTelegraphExtras  => null;
        /// <summary>Flash color used for each burst pattern (overrides SecondaryRangedFlashColor).  Indexed by pattern.</summary>
        protected virtual Color[]  SecondaryRangedBurstFlashColors      => null;
        /// <summary>Relative selection weights for each burst pattern.  null = uniform random.</summary>
        protected virtual int[]    SecondaryRangedBurstChances          => null;
        /// <summary>Optional primary ranged burst patterns. Same format as SecondaryRangedBurstPatterns.</summary>
        protected virtual int[][]  PrimaryRangedBurstPatterns          => null;
        protected virtual int[]    PrimaryRangedBurstTelegraphExtras    => null;
        protected virtual Color[]  PrimaryRangedBurstFlashColors        => null;
        protected virtual int[]    PrimaryRangedBurstChances            => null;
        // ── Spear / polearm ───────────────────────────────────────────────────────
        protected virtual float SpearRange          => 200f;   // max distance for spear poke
        protected virtual int   SpearTelegraphTicks => 35;
        protected virtual int   SpearAttackTicks    => 12;
        protected virtual int   SpearRecoveryTicks  => 30;
        protected virtual int   SpearCooldownAfterUse => 90;
        /// <summary>
        /// Forward velocity during SpearAttack, as a multiplier of TopSpeed.
        /// 0 = fully stationary poke; 0.5–1.0 = short hop; > 1.0 = short lunge.
        /// </summary>
        protected virtual float SpearPushSpeedMult  => 0f;

        // ── Magic / spellcast ─────────────────────────────────────────────────────
        protected virtual float MagicRange          => 700f;
        protected virtual float MinMagicRange       => 0f;
        protected virtual int   MagicTelegraphTicks => 50;
        protected virtual int   MagicAttackTicks    => 15;
        protected virtual int   MagicRecoveryTicks  => 70;
        protected virtual int   MagicCooldownAfterUse => 300;
        /// <summary>Chance (0–100) to choose magic over a ranged attack when BOTH are in range.  0 (default)
        /// keeps magic as a gap-filler (only when no ranged option is available).</summary>
        protected virtual int   MagicPreferenceChance => 0;
        /// <summary>Flash color for the spear-poke and magic-cast telegraphs.</summary>
        protected virtual Color SpearTelegraphFlashColor => Color.LightYellow;
        protected virtual Color MagicTelegraphFlashColor => Color.MediumPurple;

        // ── Fire breath (optional, opt-in per subclass) ───────────────────────────
        /// <summary>Master toggle: when true this invader can perform a sustained fire-breath attack
        /// (BreathTelegraph → Breathing → BreathRecovery).  Override <see cref="DoBreathWindup"/> and
        /// <see cref="DoBreathTick"/> to supply the telegraph VFX and the breath projectiles.</summary>
        protected virtual bool  CanBreathe            => false;
        /// <summary>Max distance at which breath can begin.</summary>
        protected virtual float BreathRange           => 560f;
        /// <summary>Won't breathe if the player is closer than this (point-blank).</summary>
        protected virtual float MinBreathRange        => 0f;
        /// <summary>Charge-up duration (the swelling-ember telegraph) before the stream releases.</summary>
        protected virtual int   BreathTelegraphTicks  => 150;
        /// <summary>Length of the active breath stream.</summary>
        protected virtual int   BreathDurationTicks   => 70;
        /// <summary>Recovery after the stream ends, before <see cref="BreathCooldownAfterUse"/> applies.</summary>
        protected virtual int   BreathRecoveryTicks   => 40;
        /// <summary>Ticks that must pass after a breath before another is allowed.</summary>
        protected virtual int   BreathCooldownAfterUse => 330;
        /// <summary>Chance (0–100) per eligible Idle tick to begin a breath when in range and off cooldown.</summary>
        protected virtual int   BreathChance          => 3;
        /// <summary>Flash color for the breath telegraph.</summary>
        protected virtual Color BreathTelegraphFlashColor => Color.OrangeRed;
        /// <summary>When true the invader may begin a breath while airborne (and will strafe across to
        /// sweep the stream).  When false breath is grounded-only.</summary>
        protected virtual bool  BreathAllowedAirborne => false;

        /// <summary>True for the full duration of the entire telegraph+attack window (not just the
        /// post-flash commit).  Set true to give this invader hyper-armor — zero knockback, no poise
        /// build, can't be staggered — from the moment an attack telegraph starts until recovery.
        /// Default false preserves the original behavior (interruptible during the wind-up).</summary>
        protected virtual bool  HyperArmorDuringTelegraph => false;

        // ── Proactive projectile evasion (optional) ───────────────────────────────
        // SF4-mover invaders don't run the legacy BasicAI Agility dodge scan, so this is a self-
        // contained version: each idle tick, scan for an incoming aimed friendly projectile and (rolling
        // the GlobalNPC Agility stat) either jump over it or i-frame dodge in place.  Reuses the existing
        // mover-agnostic DodgeTimer i-frames.  Default off; set Agility on the NPC to tune frequency.
        protected virtual bool EvadesProjectiles => false;
        /// <summary>Detection radius (px) for an incoming aimed projectile.</summary>
        protected virtual float ProjectileEvadeRange => 200f;

        // ── Quick-step dodge (shared with FighterAI via tsorcRevampAIs.TickQuickStep) ──────
        // ON-HIT quick-step works automatically for any invader whose EvasiveProfile sets EvasiveQuickStep
        // (InvaderNPC always ticks the shared executor, so the old "invader can't tick QuickStepTimer" bug
        // is gone).  PREEMPTIVE quick-step is opt-in: when the player is mid-swing in melee range, roll
        // PreemptiveQuickStepChance to dash THROUGH them (i-frames, can't hurt them) and land past them.
        /// <summary>Chance (0–100) to preemptively quick-step when the player is mid-swing in melee range.
        /// 0 = no proactive quick-step (on-hit still works via EvasiveProfile).</summary>
        protected virtual int   PreemptiveQuickStepChance => 0;
        /// <summary>Post-step recovery (ticks) before attacks/pursuit resume — the "can't instantly re-attack
        /// after the dash" window.  Tune with <see cref="QuickStepForwardRoom"/> for the spacing feel.</summary>
        protected virtual int   QuickStepRecoveryTicks => 30;
        /// <summary>Extra px past the player when dashing THROUGH them, so the step lands with room.</summary>
        protected virtual float QuickStepForwardRoom => 16f;

        // ── Cursed Knives (optional sustained throw attack) ────────────────────────
        // Hold a knife (telegraph) → throw 1–3 volleys of 3 knives that stick in tiles, swell with
        // black/purple dust for ~1.5 s, then burst into a lingering plague cloud.  Volley count + gap
        // scale with range.  Subclass supplies the projectile + held-knife sprite via the hooks below.
        protected virtual bool  CanThrowCursedKnives        => false;
        protected virtual int   CursedKnivesWeaponItemType  => -1;   // held-knife display sprite (-1 = none)
        protected virtual float CursedKnivesRange           => 760f;
        protected virtual float CursedKnivesMinRange        => 0f;
        protected virtual float CursedKnivesCloseRange      => 220f;  // ≤ this: 1 volley, tight spread
        protected virtual float CursedKnivesMidRange        => 470f;  // ≤ this: 2 volleys back-to-back
        protected virtual int   CursedKnivesChance          => 4;     // % per eligible idle tick
        protected virtual int   CursedKnivesTelegraphTicks  => 45;
        protected virtual int   CursedKnivesThrowTicks      => 12;    // active frames per volley
        protected virtual int   CursedKnivesBackToBackGap   => 10;    // pause between mid-range volleys
        protected virtual int   CursedKnivesFarGap          => 30;    // pause between far-range volleys
        protected virtual int   CursedKnivesRecoveryTicks   => 40;
        protected virtual int   CursedKnivesCooldownAfterUse => 420;
        protected virtual bool  CursedKnivesAllowedAirborne => true;
        protected virtual Color CursedKnivesTelegraphFlashColor => new Color(150, 60, 210);

        // ── Piercing Dash (optional medium/long-range sword-lunge attack) ──────────
        // Hold the weapon out toward the player (telegraph, sprite jitters in place) → dash straight
        // through them (a fresh melee hitbox each tick so a fast target can still be caught) → brief
        // recovery.  A stab variant may be rolled at telegraph start: on the first connecting hit it
        // stops the dash, holds the target impaled while the weapon arm slowly raises, then flicks them
        // away.  Subclass supplies all VFX/damage/heal specifics via the hooks below.
        protected virtual bool  CanPierce                  => false;
        protected virtual float PierceRange                => 700f;
        protected virtual float MinPierceRange             => 250f;
        protected virtual int   PierceChance                => 4;     // % per eligible idle tick
        protected virtual int   PierceTelegraphTicks       => 60;
        protected virtual int   PierceDashTicks            => 40;     // travel duration of the lunge
        protected virtual float PierceDashSpeed            => 15f;
        protected virtual int   PierceRecoveryTicks        => 90;
        /// <summary>Chance (0–100), rolled once at telegraph start, that this pierce becomes the
        /// grab-and-impale variant instead of a simple pass-through lunge.</summary>
        protected virtual int   PierceStabChance           => 50;
        /// <summary>Ticks spent holding the impaled target while the sword arm raises 0→90°.</summary>
        protected virtual int   PierceStabRaiseTicks       => 180;
        /// <summary>Ticks spent rotating back down and flicking the target away.</summary>
        protected virtual int   PierceStabFlickTicks       => 20;
        /// <summary>Cooldown after the whole sequence (dash or stab) ends before another can begin.</summary>
        protected virtual int   PierceCooldownAfterUse     => 480;

        /// <summary>Called every tick of the PierceTelegraph phase, with elapsed ticks counting up from 0.
        /// Override to spawn the wind-up VFX (dust color etc. — vary by <see cref="IsPierceStab"/>).</summary>
        protected virtual void DoPierceWindup(int elapsed) { }
        /// <summary>Called every tick of the active dash, after the hitbox/impale logic has run for
        /// that tick.  Override for trail dust / sound.</summary>
        protected virtual void DoPierceDashTick() { }
        /// <summary>Fired once the instant the dash connects (before contact damage is applied).
        /// <paramref name="isStab"/> is <see cref="IsPierceStab"/> at the moment of the hit.</summary>
        protected virtual void OnPierceContact(Player target, bool isStab) { }
        /// <summary>Fired once per tick while the target is held impaled (PierceStabHold phase),
        /// with the current raise progress 0–1.  Override to keep the impaling weapon's visual in sync.</summary>
        protected virtual void DoPierceStabHoldTick(Player target, float raiseProgress01) { }
        /// <summary>Fired once when the flick releases the target (start of PierceStabFlick).
        /// Override for the heal / screenshake / launch — the base class does not apply these itself
        /// since the exact numbers are boss-specific.</summary>
        protected virtual void OnPierceFlick(Player target) { }

        /// <summary>True from the moment a pierce telegraph rolls the stab variant until the whole
        /// pierce sequence ends.  Read this from the VFX hooks above to branch color/behavior.</summary>
        protected bool IsPierceStab => _pierceIsStab;
        private bool  _pierceIsStab;
        private bool  _pierceHitConnected;
        private int   _pierceDir;
        private Vector2 _pierceAnchorPos;
        private Player _pierceTarget;
        protected int  _pierceCooldown;

        // ── Jumping Downward Slash (optional jump attack, any range within a min/max band) ──
        // A genuine dodgeroll (backward movement + real i-frames via the shared DodgeTimer mechanism,
        // not a cosmetic hop) leads into a jump toward the player holding the sword cocked up-and-back,
        // then a half-circle downward swipe once close or on landing.
        protected virtual bool  CanJumpSlash              => false;
        protected virtual float JumpSlashMinRange         => 200f;
        protected virtual float JumpSlashMaxRange         => 500f;
        protected virtual int   JumpSlashChance           => 4;      // % per eligible idle tick
        protected virtual int   JumpSlashCooldownAfterUse => 420;

        /// <summary>Ticks of the backward dodgeroll (also the i-frame window).</summary>
        protected virtual int   JumpSlashDodgebackTicks    => 24;
        protected virtual float JumpSlashDodgebackSpeed    => 6f;
        /// <summary>Max airborne ticks before the swipe fires regardless (normally cut short by
        /// <see cref="JumpSlashTriggerRange"/> or landing).</summary>
        protected virtual int   JumpSlashRiseTicks         => 60;
        protected virtual float JumpSlashLaunchUpSpeed     => 9f;
        protected virtual float JumpSlashLaunchForwardSpeed => 6f;
        /// <summary>Distance at which the swipe triggers mid-air, before landing.</summary>
        protected virtual float JumpSlashTriggerRange      => 90f;
        protected virtual int   JumpSlashAttackTicks       => 18;
        protected virtual int   JumpSlashRecoveryTicks     => 70;

        /// <summary>Called every tick of the backward dodgeroll. Override for VFX/sound.</summary>
        protected virtual void DoJumpSlashDodgebackTick() { }
        /// <summary>Called every tick while airborne and rising/traveling toward the player.</summary>
        protected virtual void DoJumpSlashRiseTick() { }
        /// <summary>Fired once, the instant the half-circle swipe begins. Spawn the hit here
        /// (e.g. <c>TryMeleeHit()</c>) - the base class doesn't apply damage itself.</summary>
        protected virtual void DoJumpSlashAttack() { }

        private int _jumpSlashDir;
        protected int _jumpSlashCooldown;

        // ── Forward Flip Slash (optional spin-through jump attack) ─────────────────
        // A forward mid-air roll — genuinely i-framed via DodgeTimer, same as JumpSlashDodgeback —
        // held out and spinning continuously with damage active, closing distance THROUGH the target.
        // Lands into a held slam pose (screenshake + dirt) rather than an immediate recovery.
        protected virtual bool  CanFlipSlash               => false;
        protected virtual float FlipSlashMinRange          => 150f;
        protected virtual float FlipSlashMaxRange          => 450f;
        protected virtual int   FlipSlashChance            => 4;      // % per eligible idle tick
        protected virtual int   FlipSlashCooldownAfterUse  => 420;

        protected virtual float FlipSlashLaunchUpSpeed      => 8f;
        protected virtual float FlipSlashLaunchForwardSpeed => 7f;
        /// <summary>Safety cap on airborne ticks in case it never reads a clean landing.</summary>
        protected virtual int   FlipSlashRiseMaxTicks       => 70;
        /// <summary>Radians/tick the sword spins while airborne — tuned so a typical flight completes
        /// roughly one full revolution before landing.</summary>
        protected virtual float FlipSlashSpinSpeed          => 0.15f;
        /// <summary>Ticks the sword is held in the landing slam pose before returning to normal behavior.</summary>
        protected virtual int   FlipSlashLandHoldTicks      => 30;

        /// <summary>Called every tick while airborne and spinning. Override for trail VFX/sound.</summary>
        protected virtual void DoFlipSlashRiseTick() { }
        /// <summary>Fired once, the instant the spinning sword connects with the target. Spawn the
        /// hit here (e.g. <c>TryMeleeHit()</c>) - the base class doesn't apply damage itself.</summary>
        protected virtual void DoFlipSlashHit() { }
        /// <summary>Fired once on landing, before the slam pose hold begins. Override for the
        /// screenshake / dirt dust — the base class does not apply these itself.</summary>
        protected virtual void OnFlipSlashLand() { }

        private int  _flipSlashDir;
        private bool _flipHitConnected;
        protected int _flipSlashCooldown;

        // ── Abyss Slash (optional ranged sword-projectile chain) ───────────────────
        // Underhand → 170° arc → held "post" pose (still walking) → a swipe that fires a projectile
        // (via DoAbyssSlashFire) → NextAbyssSlashDelay decides whether another swipe follows and after
        // how long, so a subclass can chain any number of swipes with per-gap timing (or none at all).
        protected virtual bool  CanAbyssSlash              => false;
        protected virtual float AbyssSlashMinRange         => 250f;
        protected virtual float AbyssSlashMaxRange         => 900f;
        protected virtual int   AbyssSlashChance           => 4;      // % per eligible idle tick
        protected virtual int   AbyssSlashCooldownAfterUse => 300;

        /// <summary>Duration of the 170° arc portion of the wind-up.</summary>
        protected virtual int   AbyssSlashArcTicks         => 40;
        /// <summary>Ticks the "post" pose is held (still walking) after the arc completes.</summary>
        protected virtual int   AbyssSlashHoldTicks         => 20;
        protected virtual int   AbyssSlashSwipeTicks        => 16;
        protected virtual int   AbyssSlashRecoveryTicks     => 60;

        /// <summary>Fired once, the instant a swipe begins (index 0 = the first swipe out of the
        /// wind-up). Spawn the projectile / finisher here - the base class fires nothing itself.</summary>
        protected virtual void DoAbyssSlashFire(int swipeIndex) { }
        /// <summary>Called right after swipe <paramref name="completedSwipeIndex"/> finishes. Return
        /// the tick gap before the next swipe, or a negative value to end the sequence (→ recovery).
        /// Default -1 = always just one swipe.</summary>
        protected virtual int   NextAbyssSlashDelay(int completedSwipeIndex) => -1;

        private int   _abyssSlashIndex;
        protected int _abyssSlashCooldown;

        // ── Abyss Tendril Grab (optional grab-and-punish attack) ───────────────────
        // Arm dissolves into shadow, a tendril projectile reaches out and (on contact) gently yanks
        // the target closer; once the tendril's self-contained fly/yank/retract sequence finishes
        // (timed via TendrilReachTicks - the projectile doesn't report back), the arm returns and
        // throws the SAME 180°→10°→170° swing shape as Abyss Slash's wind-up/release, punishing
        // whoever just got pulled in. All VFX/spawn specifics are subclass hooks; the base class only
        // owns timing and the melee-swing rotation.
        protected virtual bool  CanTendrilGrab              => false;
        protected virtual float TendrilMinRange             => 150f;
        protected virtual float TendrilMaxRange             => 500f;
        protected virtual int   TendrilChance               => 4;
        protected virtual int   TendrilCooldownAfterUse     => 480;

        protected virtual int   TendrilTelegraphTicks       => 24;
        /// <summary>Covers the tendril projectile's whole fly+yank+retract lifecycle. Not
        /// synchronized with the projectile (which is self-contained) - just long enough to
        /// comfortably outlast it.</summary>
        protected virtual int   TendrilReachTicks           => 90;
        protected virtual int   TendrilSwingArcTicks        => 40;   // 180° -> 10°
        protected virtual int   TendrilSwingHoldTicks       => 12;   // held at 10° before releasing
        protected virtual int   TendrilSwingTicks           => 16;   // 10° -> 170° release
        protected virtual int   TendrilRecoveryTicks        => 60;

        /// <summary>Called every tick of the arm-dissolve wind-up, elapsed counting up from 0.</summary>
        protected virtual void DoTendrilTelegraphTick(int elapsed) { }
        /// <summary>Fired once when the wind-up ends - spawn the tendril projectile here.</summary>
        protected virtual void DoTendrilLaunch() { }
        /// <summary>Called every tick while the tendril is out (whole TendrilReach phase).</summary>
        protected virtual void DoTendrilReachTick() { }
        /// <summary>Fired once, the instant the finishing swing releases. Apply the hit here
        /// (e.g. <c>TryMeleeHit()</c>) - the base class doesn't apply damage itself.</summary>
        protected virtual void DoTendrilSwing() { }

        protected int _tendrilCooldown;

        // ── Dodge-punish chain (generic) ────────────────────────────────────────────
        // Tracks whether the CURRENT committed swing/volley actually connected. Reset to false
        // whenever a chain-eligible attack begins (JumpSlashDodgeback / AbyssSlashTelegraph /
        // TendrilTelegraph); set true by TickBladeHit's real per-tick overlap check for melee
        // swings/combos, or by ranged projectiles reporting back via ReportAttackHit() on hit. A
        // clean dodge (this stays false) can then be punished by chaining straight into a different
        // attack instead of handing the player a free breather in Recovery.
        private bool _lastAttackHitConnected;

        /// <summary>% chance, on a fully whiffed JumpingDownwardSlash or Abyss Slash, to chain
        /// straight into the other one (if in range and off cooldown) instead of returning to
        /// neutral recovery.</summary>
        protected virtual int DodgePunishChainChance => 50;

        /// <summary>Called by a hostile projectile that hit its target, to credit this invader's
        /// current attack as having connected (see <see cref="_lastAttackHitConnected"/>).</summary>
        public void ReportAttackHit() => _lastAttackHitConnected = true;

        /// <summary>
        /// If the swing that just ended fully whiffed, rolls <see cref="DodgePunishChainChance"/> and
        /// (on success) immediately re-enters whichever of {AbyssSlash, JumpingDownwardSlash} is off
        /// cooldown and in range, skipping the normal recovery. Caller must set the cooldown of the
        /// attack that just ended BEFORE calling this (so it can't immediately chain into itself).
        /// </summary>
        private bool TryDodgePunishChain(float dist)
        {
            if (_lastAttackHitConnected || Main.rand.Next(100) >= DodgePunishChainChance)
                return false;

            if (CanAbyssSlash && _abyssSlashCooldown <= 0
                && dist >= AbyssSlashMinRange && dist <= AbyssSlashMaxRange)
            {
                _lastAttackHitConnected = false;
                EnterPhase(AttackPhase.AbyssSlashTelegraph, AbyssSlashArcTicks + AbyssSlashHoldTicks);
                return true;
            }

            if (CanJumpSlash && _jumpSlashCooldown <= 0
                && dist >= JumpSlashMinRange && dist <= JumpSlashMaxRange)
            {
                _lastAttackHitConnected = false;
                EnterPhase(AttackPhase.JumpSlashDodgeback, JumpSlashDodgebackTicks);
                return true;
            }

            return false;
        }

        // ── Charge-up Nova (health-threshold interrupt) ─────────────────────────────
        // Highest-priority interrupt, checked alongside Healing: the invader roots in place while
        // dust gathers and thickens around it and an outer telegraph ring races out ahead over
        // NovaChargeTicks, then the whole radius detonates in one huge blast. Trigger conditions
        // (e.g. specific HP thresholds) and exact numbers are entirely subclass-owned - the base
        // class just owns the three-phase timing skeleton.
        protected virtual bool  CanNova            => false;
        protected virtual int   NovaChargeTicks     => 4 * 60;
        protected virtual int   NovaBlastHoldTicks  => 24;
        protected virtual int   NovaRecoveryTicks   => 90;

        /// <summary>Return true (once) when the invader should interrupt whatever it's doing to
        /// charge up a nova. Treat this like <c>ShouldHeal</c> - track your own one-shot state.</summary>
        protected virtual bool ShouldTriggerNova() => false;
        /// <summary>Called every tick of the charge-up, elapsed/total counting the wind-up progress.</summary>
        protected virtual void DoNovaChargeTick(int elapsed, int total) { }
        /// <summary>Fired once when the charge completes - spawn the actual blast here.</summary>
        protected virtual void DoNovaBlast() { }

        // ── Abyss Shard (ground-spike combo attack, optional) ───────────────────────
        // Same shape as Abyss Slash: a short wind-up, then a subclass-defined sequence of "fire
        // events" spaced by NextAbyssShardDelay - each event calls DoAbyssShardFire once. Deliberately
        // has NO recovery phase: the moment the sequence ends (negative delay), control returns
        // straight to Idle/CasualStroll so a different attack can follow immediately.
        protected virtual bool  CanAbyssShard             => false;
        protected virtual float AbyssShardMinRange        => 80f;
        protected virtual float AbyssShardMaxRange        => 900f;
        protected virtual int   AbyssShardChance          => 10;
        protected virtual int   AbyssShardCooldownAfterUse => 420;
        protected virtual int   AbyssShardTelegraphTicks  => 30;
        protected virtual int   AbyssShardFireTicks       => 6;

        /// <summary>Returns the delay (ticks) before the next fire event, or a negative value to end
        /// the sequence (with no recovery phase). completedFireIndex is the index that just fired.</summary>
        protected virtual int  NextAbyssShardDelay(int completedFireIndex) => -1;
        /// <summary>Fired once per event (index 0, 1, 2...) - spawn ground shard(s) here.</summary>
        protected virtual void DoAbyssShardFire(int fireIndex) { }

        protected int _abyssShardIndex;
        protected int _abyssShardCooldown;

        // ── Homing Volley (dodgeback + overhead swing + delayed-homing orb volley) ──
        // Backs away with a real dodgeroll (i-frames), then raises the sword fully overhead and
        // brings it down in one big chop; partway through that chop (HomingVolleyFireProgress) it
        // fires a subclass-chosen pattern of delayed-homing orbs. No inter-attack chaining - a fixed
        // recovery always follows.
        protected virtual bool  CanHomingVolley                => false;
        protected virtual float HomingVolleyMinRange            => 280f;
        protected virtual float HomingVolleyMaxRange            => 650f;
        protected virtual int   HomingVolleyChance              => 10;
        protected virtual int   HomingVolleyCooldownAfterUse    => 300;
        protected virtual int   HomingVolleyDodgebackTicks      => 24;
        protected virtual float HomingVolleyDodgebackSpeed      => 6f;
        protected virtual int   HomingVolleySwingTelegraphTicks => 20;
        protected virtual int   HomingVolleySwingTicks          => 30;
        /// <summary>Fraction (0-1) through the swing at which <see cref="DoHomingVolleyFire"/> fires.</summary>
        protected virtual float HomingVolleyFireProgress        => 0.5f;
        protected virtual int   HomingVolleyRecoveryTicks       => 120;

        /// <summary>Called every tick of the overhead chop, elapsed/total counting swing progress -
        /// override to spawn dust along the blade's current arc position.</summary>
        protected virtual void DoHomingVolleySwingTick(int elapsed, int total) { }
        /// <summary>Fired once, partway through the swing - spawn the orb volley here.</summary>
        protected virtual void DoHomingVolleyFire() { }

        private int  _homingVolleyDir = 1;
        private bool _homingVolleyFired;
        protected int _homingVolleyCooldown;

        // ── Sword Launch Reposition (shared by Boomerang / Spiral Fan) ──────────────
        // A mostly-horizontal backward hop, facing the player, used ONLY when they're closer than
        // SwordLaunchRepositionTooCloseRange when one of those attacks is selected - creates room
        // for the overhead chop + projectile(s) instead of letting them fire from point-blank.
        // Unlike Homing Volley's dodgeback this is a plain hop (no i-frames): it's a spacing
        // correction, not a defensive dodge.
        protected virtual float SwordLaunchRepositionTooCloseRange => 150f;
        protected virtual int   SwordLaunchRepositionTicks         => 20;
        protected virtual float SwordLaunchRepositionBackSpeed     => 7.5f;
        protected virtual float SwordLaunchRepositionUpSpeed       => 4.5f;

        private AttackPhase _swordLaunchNextPhase;
        private int         _swordLaunchNextTicks;
        private int         _swordLaunchDir = 1;

        // ── Boomerang Crescent (dodge-free overhead swing + curving/returning crescent(s)) ──
        // Reuses the same overhead-chop shape as Homing Volley. Fires partway through the chop;
        // the crescent(s) arc out in a wide curl, then home back toward the invader's OWN current
        // position for a second, separately-timed pass.
        protected virtual bool  CanBoomerang                => false;
        protected virtual float BoomerangMinRange           => 60f;
        protected virtual float BoomerangMaxRange           => 650f;
        protected virtual int   BoomerangChance             => 9;
        protected virtual int   BoomerangCooldownAfterUse   => 330;
        protected virtual int   BoomerangSwingTelegraphTicks => 20;
        protected virtual int   BoomerangSwingTicks         => 30;
        protected virtual float BoomerangFireProgress       => 0.5f;
        protected virtual int   BoomerangRecoveryTicks      => 110;

        protected virtual void DoBoomerangSwingTick(int elapsed, int total) { }
        protected virtual void DoBoomerangFire() { }

        private bool _boomerangFired;
        protected int _boomerangCooldown;

        // ── Spiral Fan (dodge-free overhead swing + a rotating-angle burst of shots) ──────
        // Same overhead-chop wind-up as the others, but the swing itself doesn't fire anything -
        // once it completes, a dedicated burst sequence (SpiralFanBurst/Pause, mirroring Abyss
        // Shard's fire-event loop) fires each shot with an incrementing launch angle over time,
        // producing a rotating stream instead of a static simultaneous fan.
        protected virtual bool  CanSpiralFan                => false;
        protected virtual float SpiralFanMinRange           => 60f;
        protected virtual float SpiralFanMaxRange           => 650f;
        protected virtual int   SpiralFanChance             => 8;
        protected virtual int   SpiralFanCooldownAfterUse   => 360;
        protected virtual int   SpiralFanSwingTelegraphTicks => 20;
        protected virtual int   SpiralFanSwingTicks         => 30;
        protected virtual int   SpiralFanFireTicks          => 4;
        protected virtual int   SpiralFanRecoveryTicks      => 90;

        protected virtual void DoSpiralFanSwingTick(int elapsed, int total) { }
        /// <summary>Returns the delay (ticks) before the next shot, or negative to end the burst.</summary>
        protected virtual int  NextSpiralFanDelay(int completedShotIndex) => -1;
        /// <summary>Fired once per shot (index 0, 1, 2...) - spawn that shot here.</summary>
        protected virtual void DoSpiralFanFire(int shotIndex) { }
        /// <summary>Fired once, right when a FRESH (non-chained) Spiral Fan sequence is chosen from
        /// the attack-selection decision tree - override to reset any chain-tracking state (e.g.
        /// <see cref="TryContinueSpiralFanChain"/>'s escalation counter) before the first volley.</summary>
        protected virtual void OnSpiralFanSequenceStart() { }

        protected int _spiralFanIndex;
        protected int _spiralFanCooldown;

        // ── Fire Volley Chain (reposition + arc-jump escalation after a Spiral Fan volley) ──
        // Hooked into SpiralFanRecovery: instead of always returning to idle, an override of
        // TryContinueSpiralFanChain can take over and re-enter SpiralFanSwingTelegraph (via one of
        // the reposition phases below) for a second volley, then optionally escalate into a big
        // arc-jump-over for a third. All no-ops/false by default - zero effect unless overridden.
        protected virtual bool TryContinueSpiralFanChain() => false;

        protected virtual int   FireVolleyBackLeapTicks       => 26;
        protected virtual float FireVolleyBackLeapSpeed       => 5.5f; // ~8 tiles over the leap's airtime
        protected virtual float FireVolleyBackLeapUpSpeed     => 6f;
        protected virtual float FireVolleyDodgeThroughUpSpeed => 7f;
        /// <summary>Safety-net cap on FireVolleyDodgeThrough — the real duration comes from
        /// ArmQuickStep/TickQuickStep's own timers, this just guarantees the phase can't get stuck.</summary>
        protected virtual int   FireVolleyDodgeThroughTimeoutTicks => 90;
        protected virtual float FireVolleyArcJumpUpSpeed      => 13f;
        protected virtual float FireVolleyArcJumpForwardSpeed => 6f;

        /// <summary>Fired once, right when NPC.velocity.Y crosses from rising to falling (the jump's
        /// apex) during <see cref="AttackPhase.FireVolleyArcJump"/> - spawn the mid-air volley here.</summary>
        protected virtual void DoFireVolleyArcFire() { }
        /// <summary>Fired when <see cref="AttackPhase.FireVolleyBackLeap"/> or
        /// <see cref="AttackPhase.FireVolleyDodgeThrough"/> finishes (landed / quick-step fully
        /// resolved) - override to re-enter SpiralFanSwingTelegraph for another volley.</summary>
        protected virtual void OnFireVolleyRepositionLanded() { }
        /// <summary>Fired when <see cref="AttackPhase.FireVolleyArcJump"/> lands.</summary>
        protected virtual void OnFireVolleyArcJumpLanded() { }

        private int   _fireVolleyDir = 1;
        private bool  _fireVolleyArcFired;
        private float _fireVolleyArcVx;

        // ── Estus healing ─────────────────────────────────────────────────────────
        /// <summary>How many estus drinks the invader starts with.</summary>
        protected virtual int   EstusChargesMax         => 10;
        /// <summary>Fraction of max HP restored per drink.</summary>
        protected virtual float EstusHealFraction       => 0.20f;
        /// <summary>Ticks of cooldown between any two heals (prevents spam).</summary>
        protected virtual int   HealCooldownTicks       => 300;
        /// <summary>Ticks the drinking animation lasts after HP is restored.</summary>
        protected virtual int   HealAnimationTicks      => 70;
        /// <summary>
        /// Accumulated recent damage (as a fraction of max HP) that triggers an emergency heal.
        /// E.g. 0.15 = trigger heal if more than 15 % max HP taken in the recent burst window.
        /// </summary>
        protected virtual float RecentDamageThreshold   => 0.18f;
        /// <summary>How fast recent-damage memory decays per tick (fraction of max HP).</summary>
        protected virtual float RecentDamageDecayRate   => 0.0025f;
        /// <summary>Minimum pixel distance from the player before the invader stops fleeing and drinks.</summary>
        protected virtual float FleeToHealDistance      => 40 * 16f;  // 40 tiles
        /// <summary>Max ticks spent fleeing before drinking anyway (in case player is chasing).</summary>
        protected virtual int   FleeToHealMaxTicks      => 150;

        // ── Navigation (SF4 A* pathfinding; jump tuning below) ───────────────────
        protected virtual float InvaderJumpPower      => 10f;
        protected virtual float InvaderJumpBoost      => 6f;
        protected virtual bool  InvaderCanDoubleJump  => false;
        protected virtual float InvaderDoubleJumpPower => 6f;

        // ── Gap-closer attack tuning (LeapSlam / ChargeChop combo motions) ──────────
        /// <summary>Horizontal speed held during a LeapSlam's airborne arc (px/frame).</summary>
        protected virtual float LeapAttackForwardSpeed => TopSpeed * 1.7f;
        /// <summary>Upward launch speed at the start of a LeapSlam (px/frame).</summary>
        protected virtual float LeapAttackUpSpeed      => 9.5f;
        /// <summary>Run speed during a ChargeChop, as a multiplier of TopSpeed.</summary>
        protected virtual float ChargeAttackSpeedMult  => 1.85f;

        // ── Shield (optional, opt-in per subclass) ──────────────────────────────────
        /// <summary>Item type whose sprite is drawn as this invader's off-hand shield.  -1 = no
        /// shield.  Override in a subclass to give an invader a reactive guard (also call a
        /// <c>ShieldProfile.*</c> in SetDefaults to set the block chances).</summary>
        protected virtual int ShieldItemType => -1;
        /// <summary>True when this invader carries a shield.</summary>
        protected bool HasShield => ShieldItemType > 0;
        /// <summary>Damage multiplier applied to FRONT hits while actively guarding (0.5 = take half).
        /// Backstabs bypass the block entirely.</summary>
        protected virtual float ShieldDamageReduction => 0.5f;
        /// <summary>Path to the held-shield draw sprite — a 20-frame vertical strip sharing the
        /// player body's frame layout (frame N overlays body frame N), like vanilla equip shields.
        /// Defaults to the shield item's texture + "_Shield"; override to point elsewhere.</summary>
        protected virtual string ShieldDrawTexturePath =>
            HasShield ? TextureAssets.Item[ShieldItemType].Name + "_Shield" : null;
        private Texture2D _shieldDrawTex;
        private bool _shieldDrawTexLoaded;
        /// <summary>Minimum guard hold (ticks) when a block is raised away from melee range — long
        /// enough to catch a ranged shot, short enough to stay mobile.  90 ticks = 1.5 s.</summary>
        protected virtual int ShieldGuardTicksRanged => 90;
        /// <summary>Minimum guard hold (ticks) once the guard is committed in melee range.  The
        /// invader plants + locks facing for this whole window, so the player can dodgeroll through
        /// and backstab.  150 ticks = 2.5 s.</summary>
        protected virtual int ShieldGuardTicksMelee => 150;
        /// <summary>Slow advance speed (px/frame) toward the player while guarding OUT of melee
        /// range.  In melee range the invader plants instead.</summary>
        protected virtual float ShieldAdvanceSpeed => 0.9f;
        /// <summary>True this frame while the guard is up (drives the raised-shield pose + the
        /// front damage / poise reduction).  Driven by the reactive block timer.</summary>
        private bool _shielding;
        /// <summary>Facing captured when the guard goes up and held through a melee commit, so the
        /// invader doesn't track a player dodgerolling through to its back.</summary>
        private int  _shieldLockedDir;
        /// <summary>Rising-edge tracker for the guard (false→true = just raised).</summary>
        private bool _shieldWasGuarding;
        /// <summary>True once the current guard has had its hold bumped to the melee minimum (so the
        /// extension is applied once, not re-applied every tick the player stays adjacent).</summary>
        private bool _shieldMeleeExtended;

        /// <summary>
        /// Run the ground-movement AI for this invader.  All invaders navigate with
        /// <c>SmartFighter4AI</c> (A* pathfinding + ropes + stuck/teleport recovery); subclasses
        /// override to tune search radius / door-break / attack range.  Called once per tick from
        /// <see cref="AI"/> while grounded.
        /// </summary>
        protected virtual void RunMovementAI(float speedMult)
        {
            var g = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            g.RemembersLastKnownPos = true;
            SmartFighter4AI.Run(NPC,
                topSpeed:           TopSpeed * speedMult,
                acceleration:       Acceleration,
                doorBreakingDamage: 4,
                attackRange:        RangedRange);
        }

        // ── State machine ─────────────────────────────────────────────────────────
        protected enum AttackPhase
        {
            Idle,
            MeleeTelegraph, MeleeAttack, MeleeRecovery,
            StabTelegraph,  StabAttack,  StabRecovery,
            RangedTelegraph, RangedAttack, RangedRecovery,
            SpearTelegraph,  SpearAttack,  SpearRecovery,
            MagicTelegraph,  MagicAttack,  MagicRecovery,
            /// <summary>Charge-up before a sustained fire-breath stream: the invader plants (or, when
            /// airborne, keeps flying) and emits a swelling mouth ember via <see cref="DoBreathWindup"/>.</summary>
            BreathTelegraph,
            /// <summary>Active fire-breath stream: <see cref="DoBreathTick"/> spawns the breath projectiles
            /// each tick for <see cref="BreathDurationTicks"/>.  When airborne the dragon strafes to sweep.</summary>
            Breathing,
            /// <summary>Recovery after the breath stream ends; applies <see cref="BreathCooldownAfterUse"/>.</summary>
            BreathRecovery,
            /// <summary>Hold-the-knife charge before the first Cursed Knives volley.</summary>
            KnivesTelegraph,
            /// <summary>Active throw frame: <see cref="DoCursedKnivesThrow"/> spawns a 3-knife volley.</summary>
            KnivesThrow,
            /// <summary>Pause between Cursed Knives volleys (next volley fires when it expires — no re-telegraph).</summary>
            KnivesThrowPause,
            /// <summary>Recovery after the last Cursed Knives volley; applies <see cref="CursedKnivesCooldownAfterUse"/>.</summary>
            KnivesRecovery,
            /// <summary>Piercing Dash wind-up: weapon held out toward the player, sprite jitters in place.</summary>
            PierceTelegraph,
            /// <summary>Active lunge: a fresh melee hitbox is spawned every tick so the fast-moving dash
            /// can still catch the target.  A stab-variant hit breaks out into <see cref="PierceStabHold"/>.</summary>
            PierceDash,
            /// <summary>Stab variant only: the target is held impaled while the sword arm raises 0→90°
            /// over <see cref="PierceStabRaiseTicks"/>.</summary>
            PierceStabHold,
            /// <summary>Stab variant only: rotates back down and releases/launches the target.</summary>
            PierceStabFlick,
            /// <summary>Shared recovery after either pierce variant ends — can walk, can't attack yet.</summary>
            PierceRecovery,
            /// <summary>Jumping Downward Slash wind-up: a backward roll WITH i-frames (a genuine
            /// dodgeroll, not a hop) before the jump. See <see cref="JumpSlashDodgebackTicks"/>.</summary>
            JumpSlashDodgeback,
            /// <summary>Airborne: launches toward the player holding the cocked "up and back" pose;
            /// breaks into <see cref="JumpSlashAttack"/> once close or on landing.</summary>
            JumpSlashRise,
            /// <summary>The half-circle downward swipe itself (fires once on entry via <see cref="DoJumpSlashAttack"/>).</summary>
            JumpSlashAttack,
            /// <summary>Recovery after landing/swiping.</summary>
            JumpSlashRecovery,
            /// <summary>Forward flipping spin-through: airborne, i-framed (a real dodgeroll, not just
            /// hyper-armor), sword held out and spinning continuously with damage active. Breaks into
            /// <see cref="FlipSlashLand"/> on landing.</summary>
            FlipSlashRise,
            /// <summary>The sword snaps into the landing slam pose and is held there for
            /// <see cref="FlipSlashLandHoldTicks"/> (screenshake + dirt dust fire once on entry).</summary>
            FlipSlashLand,
            /// <summary>Abyss Slash wind-up: underhand → 170° arc → held "sword post" pose for the
            /// last <see cref="AbyssSlashHoldTicks"/>. Deliberately does NOT stop movement (SF4's
            /// normal pursuit from earlier this tick is left untouched) so the invader keeps walking.</summary>
            AbyssSlashTelegraph,
            /// <summary>One quick swing that fires <see cref="DoAbyssSlashFire"/> once on entry.</summary>
            AbyssSlashSwipe,
            /// <summary>Inter-swipe gap; duration comes from <see cref="NextAbyssSlashDelay"/>.</summary>
            AbyssSlashPause,
            /// <summary>Recovery once <see cref="NextAbyssSlashDelay"/> returns a negative delay.</summary>
            AbyssSlashRecovery,
            /// <summary>Abyss Tendril Grab wind-up: the arm dissolves into shadow dust before reaching out.</summary>
            TendrilTelegraph,
            /// <summary>The tendril projectile is out (flying/yanking/retracting is entirely self-contained
            /// on the projectile); the arm holds an outstretched, dust-covered pose for the duration.</summary>
            TendrilReach,
            /// <summary>The arm/sword winds back up for the finishing swing (same 180°→10° arc as
            /// <see cref="AbyssSlashTelegraph"/>) once the tendril sequence is done.</summary>
            TendrilSwingTelegraph,
            /// <summary>The finishing underhand release (same 10°→170° arc as <see cref="AbyssSlashSwipe"/>).</summary>
            TendrilSwing,
            /// <summary>Recovery after the finishing swing.</summary>
            TendrilRecovery,
            /// <summary>A melee attack was chosen but the player is out of hittable reach: sprint toward
            /// them (no swing) until in range, then start the attack.  Prevents whiffing at distance.</summary>
            ClosingDistance,
            /// <summary>
            /// Inter-shot pause during a crossbow burst pattern.
            /// The invader holds horizontal aim and waits for the timer; the next shot fires
            /// automatically when it expires (no new telegraph phase).
            /// </summary>
            CrossbowBurstPause,
            /// <summary>
            /// Slow walk toward player after a recovery phase; no attacks allowed.
            /// Gives variety: the invader advances calmly before re-engaging.
            /// </summary>
            CasualStroll,
            /// <summary>
            /// Sprint away from the player until far enough to drink safely, then enter Healing.
            /// Interrupted if the invader can't reach safe distance within <see cref="FleeToHealMaxTicks"/>.
            /// </summary>
            FleeToHeal,
            /// <summary>
            /// Stand still and play the estus-drinking animation.  HP is restored at the start
            /// of this phase.  Transitions back to Idle when the animation finishes.
            /// </summary>
            Healing,
            /// <summary>Initial telegraph for a multi-step melee combo (step 0).  Fires the
            /// combo's initial flash, locks direction for the entire combo duration.</summary>
            MeleeComboTelegraph,
            /// <summary>Active hitbox frame of the current combo step.  Applies per-step
            /// forward push and damage scaling.</summary>
            MeleeComboAttack,
            /// <summary>Inter-step pause within a combo.  Next step's attack fires when timer expires —
            /// no per-step telegraph or flash.</summary>
            MeleeComboPause,
            /// <summary>Recovery after the last step of a combo.  Applies per-combo cooldown.</summary>
            MeleeComboRecovery,
            /// <summary>Charge-up Nova wind-up: rooted in place, engulfed in thickening dust with an
            /// outer telegraph ring racing ahead of it. Fires <see cref="DoNovaBlast"/> once on exit.</summary>
            NovaCharge,
            /// <summary>Brief hold at the moment of detonation.</summary>
            NovaBlast,
            /// <summary>Recovery after the nova.</summary>
            NovaRecovery,
            /// <summary>Abyss Shard wind-up: a short ground-facing gesture before the first fire event.</summary>
            AbyssShardTelegraph,
            /// <summary>Brief active frame for a fire event - fires <see cref="DoAbyssShardFire"/> once on entry.</summary>
            AbyssShardFire,
            /// <summary>Inter-event gap; duration comes from <see cref="NextAbyssShardDelay"/>. When that
            /// returns negative there is NO recovery phase - control returns straight to Idle/CasualStroll.</summary>
            AbyssShardPause,
            /// <summary>Homing Volley wind-up: a backward roll WITH i-frames (a genuine dodgeroll)
            /// before the overhead chop.</summary>
            HomingVolleyDodgeback,
            /// <summary>Sword raises fully overhead, cocked back.</summary>
            HomingVolleySwingTelegraph,
            /// <summary>The overhead chop itself - fires <see cref="DoHomingVolleyFire"/> once,
            /// partway through, via <see cref="HomingVolleyFireProgress"/>.</summary>
            HomingVolleySwing,
            /// <summary>Fixed recovery after the chop - no chaining into another attack.</summary>
            HomingVolleyRecovery,
            /// <summary>Shared wind-up for any "sword launch" attack: a mostly-horizontal backward
            /// hop, facing the player, used ONLY when they're too close for the swing that follows.
            /// Transitions to whichever telegraph phase was queued via <see cref="_swordLaunchNextPhase"/>.</summary>
            SwordLaunchReposition,
            /// <summary>Boomerang Crescent wind-up: sword raised fully overhead (same shape as
            /// Homing Volley's).</summary>
            BoomerangSwingTelegraph,
            /// <summary>The overhead chop - fires <see cref="DoBoomerangFire"/> once, partway
            /// through, via <see cref="BoomerangFireProgress"/>.</summary>
            BoomerangSwing,
            /// <summary>Fixed recovery after the chop.</summary>
            BoomerangRecovery,
            /// <summary>Spiral Fan wind-up: sword raised fully overhead (same shape as the others).</summary>
            SpiralFanSwingTelegraph,
            /// <summary>The overhead chop itself - purely a visual wind-up, no firing (the burst
            /// that follows carries the actual sequence).</summary>
            SpiralFanSwing,
            /// <summary>Active frame for one burst event - fires <see cref="DoSpiralFanFire"/> once
            /// on entry.</summary>
            SpiralFanBurst,
            /// <summary>Inter-shot gap; duration comes from <see cref="NextSpiralFanDelay"/>.</summary>
            SpiralFanPause,
            /// <summary>Fixed recovery after the whole spiral sequence ends.</summary>
            SpiralFanRecovery,
            /// <summary>Fire Volley chain: backward hop (facing the player) before re-entering
            /// SpiralFanSwingTelegraph for another volley. See <see cref="TryContinueSpiralFanChain"/>.</summary>
            FireVolleyBackLeap,
            /// <summary>Fire Volley chain: leaping dodge-roll THROUGH the player (i-frames, no
            /// contact damage - reuses ArmQuickStep/TickQuickStep) landing on their far side before
            /// re-entering SpiralFanSwingTelegraph for another volley.</summary>
            FireVolleyDodgeThrough,
            /// <summary>Fire Volley chain: a big jump arc over the player; fires
            /// <see cref="DoFireVolleyArcFire"/> once at the apex, mid-air.</summary>
            FireVolleyArcJump,
        }
        protected AttackPhase Phase = AttackPhase.Idle;
        protected int PhaseTimer;

        // Counts down; ranged is blocked while > 0
        protected int _rangedCooldown;
        // Counts down; stab is blocked while > 0 (prevents instant re-stab after dodgeroll)
        protected int _stabCooldown;
        // Counts down; spear poke is blocked while > 0
        protected int _spearCooldown;
        // Counts down; magic cast is blocked while > 0
        protected int _magicCooldown;
        // Counts down; secondary ranged is blocked while > 0
        private int _secondaryRangedCooldown;
        // Counts down; any heal is blocked while > 0
        private int _healCooldown;
        // Counts down; fire breath is blocked while > 0
        protected int _breathCooldown;
        // Counts down; Cursed Knives is blocked while > 0
        protected int _cursedKnivesCooldown;

        // Cursed Knives volley state (set at telegraph start from range).
        private int _cursedKnivesVolleysLeft;
        private int _cursedKnivesGap;

        // Per-cast override for the MagicAttack phase duration (-1 = use MagicAttackTicks).  A subclass
        // sets this inside DoMagicAttack to channel a sustained cast (e.g. a multi-second meteor rain),
        // during which DoMagicTick is called every MagicAttack tick.  Reset to -1 in MagicRecovery.
        protected int _magicAttackTicksOverride = -1;

        // Index of the active ranged burst pattern (into the pattern pool), or -1 when not using a
        // pattern.  Exposed via ActiveBurstPatternIndex so a subclass can vary the projectile per shot
        // (e.g. a cross-weapon "finisher" on the last shot of a specific pattern).
        private int _activeBurstPatternIndex = -1;

        // Secondary-ranged panic-backhop momentum.  Counts down; while > 0, AI() re-asserts an away-from-
        // player X velocity each tick (overriding the SF4 mover's pursuit, same way FleeToHeal does) so the
        // hop actually carries instead of being cancelled by pursuit on the next tick.
        private int _backhopTicks;
        private int _backhopDir;
        private const int BackhopMomentumTicks = 16;

        // ── Active ranged burst context ───────────────────────────────────────────
        // Set at the start of each burst (primary OR secondary) so all phases in the
        // same burst use consistent timing, display, and animation — without having to
        // re-query virtual properties on every tick.
        private bool        _usingSecondaryRanged;
        private RangedStyle _activeRangedStyle;
        private int         _activeRangedItemType;
        private Color       _activeRangedFlashColor;
        private int         _activeRangedTelegraphTicks;
        private int         _activeRangedAttackTicks;
        private int         _activeRangedRecoveryTicks;

        /// <summary>
        /// True while the current ranged burst is using the secondary weapon.
        /// Read this in <see cref="DoRangedAttack"/> to decide which projectile to fire.
        /// </summary>
        protected bool IsSecondaryRangedActive => _usingSecondaryRanged;

        /// <summary>Index of the active ranged burst pattern (into the active weapon's pattern pool),
        /// or -1 when this burst isn't using a pattern.  Read in <see cref="DoRangedAttack"/> to fire a
        /// different projectile on a specific pattern.</summary>
        protected int ActiveBurstPatternIndex => _activeBurstPatternIndex;

        /// <summary>True when the shot currently being fired in <see cref="DoRangedAttack"/> is the LAST
        /// shot of the active burst pattern — lets a subclass fire a cross-weapon "finisher" projectile
        /// on the final shot of a pattern.</summary>
        protected bool IsFinalBurstShot => _interShotPauses != null && _interShotPauseIndex >= _interShotPauses.Length;

        // ── Healing state ─────────────────────────────────────────────────────────
        // -1 = uninitialized (set to EstusChargesMax on first AI tick)
        protected int  _estusCharges      = -1;
        // Rolling damage accumulator (fraction of max HP); decays each tick.
        private float  _recentDamage;
        // One-shot flags: true once healed at that threshold; reset when HP rises back above it.
        private bool   _halfHpHealed;
        private bool   _quarterHpHealed;

        // ── Behaviour variety ─────────────────────────────────────────────────────
        /// <summary>Chance (0–100) of entering a casual stroll after any recovery phase.</summary>
        protected virtual int   CasualStrollChance    => 30;
        protected virtual int   CasualStrollMinTicks  => 60;   // 1 second
        protected virtual int   CasualStrollMaxTicks  => 120;  // 2 seconds
        protected virtual float CasualStrollSpeedMult => 0.35f;
        /// <summary>Chance (0–100) of planting feet and firing ranged vs. slow-approach firing.</summary>
        protected virtual int   StandingRangedChance  => 33;

        // ── Telegraph flash colors ────────────────────────────────────────────────
        /// <summary>Color of the ring-flash VFX spawned ~25 frames before a melee or stab attack.</summary>
        protected virtual Color MeleeTelegraphFlashColor  => Color.White;
        /// <summary>Color of the ring-flash VFX spawned ~25 frames before a ranged attack.</summary>
        protected virtual Color RangedTelegraphFlashColor => Color.White;

        private bool _standingShot;        // decided at start of each ranged burst
        private int  _rangedShotsRemaining; // shots left in current burst (classic mode only)
        private bool _weaponVisible;        // true only during telegraphs and attack frames

        // Crossbow burst-pattern state (set at burst start; null = classic multi-shot mode).
        // Each element is the pause duration (ticks) before the NEXT shot.
        // Total shots in the pattern = _interShotPauses.Length + 1.
        private int[] _interShotPauses;
        private int   _interShotPauseIndex;

        // ── Weapon visual ─────────────────────────────────────────────────────────
        private int   _heldItemType;
        private float _weaponRotation;    // direction-neutral draw angle
        private int   _weaponAnim;        // counts down current weapon use animation during swings
        private float _prevWeaponRotation; // last tick's _weaponRotation, for swing-speed-gated VFX
        private float _spearGrip = 0.5f;  // 0=grip at head, 1=grip at base; animated for spear extend/retract
        private int   _weaponAnimMax = DefaultWeaponAnimMax;
        private const int DefaultWeaponAnimMax = 22;

        // ── Tracked blade hit detection ───────────────────────────────────────────
        // Set once per swing/step by ArmBladeHit (via TryMeleeHit / DoComboMeleeHit); consumed
        // every tick by TickBladeHit while the swing is active.  _bladeArmed is explicitly reset
        // to false right before each DoMeleeAttack/DoStabAttack/DoSpearAttack/DoComboMeleeHit call
        // so an override that skips ArmBladeHit (e.g. BlackNinja's flail, which fires a real
        // projectile instead of a blade sweep) correctly leaves TickBladeHit a no-op that tick.
        private bool  _bladeArmed;
        private float _activeBladeReach;
        private float _activeBladeKnockback;
        private int   _activeBladeDamage;
        /// <summary>
        /// Resting hold angle when not attacking (≈ −17° — arm slightly raised, weapon pointing
        /// diagonally forward). The weapon smoothly eases to this during walk / jump / idle.
        /// </summary>
        private const float HoldRotation  = -0.30f;

        // ── Debug instrumentation (DebugMode config only) ───────────────────────────
        // Snapshot of the last DrawWeaponToLayer call's rotation/position math, so the
        // DebugMode overlay (lower-left HUD text) and the weapon-debug log file can report
        // exactly what angle/position decisions produced the on-screen sprite, instead of
        // having to be reverse-engineered from a screenshot.
        internal float   DebugWeaponRotationDeg;
        internal float   DebugDrawRotationDeg;
        internal bool    DebugHoldingSpearNow;
        internal bool    DebugHeldRangedLike;
        internal int     DebugHeldItemType;
        internal float   DebugSpearGrip;
        internal Vector2 DebugHandPos;
        internal Vector2 DebugOrigin;
        internal int     DebugDirection;
        internal string  DebugPhaseName => Phase.ToString();
        internal int     DebugPhaseTimer => PhaseTimer;
        /// <summary>Named combo + motion currently playing, e.g. "Charged Chop/OverheadArc (step 0/1)" —
        /// empty outside combo phases.  Every combo shares the same Phase enum value, so this is the
        /// only way to tell which specific attack is on screen from the overlay/log.</summary>
        internal string DebugComboTag
        {
            get
            {
                bool inCombo = Phase == AttackPhase.MeleeComboTelegraph || Phase == AttackPhase.MeleeComboAttack
                            || Phase == AttackPhase.MeleeComboPause    || Phase == AttackPhase.MeleeComboRecovery;
                if (!inCombo || _activeMeleeComboIndex < 0 || _activeMeleeCombo.Steps == null
                    || _meleeComboStepIndex >= _activeMeleeCombo.Steps.Length)
                    return "";
                var step = _activeMeleeCombo.Steps[_meleeComboStepIndex];
                return $"{_activeMeleeCombo.Name}/{step.Motion} (step {_meleeComboStepIndex}/{_activeMeleeCombo.Steps.Length})";
            }
        }

        private bool IsWeaponVisiblePhase =>
            Phase == AttackPhase.MeleeTelegraph || Phase == AttackPhase.MeleeAttack ||
            Phase == AttackPhase.StabTelegraph  || Phase == AttackPhase.StabAttack  ||
            Phase == AttackPhase.RangedTelegraph || Phase == AttackPhase.RangedAttack ||
            Phase == AttackPhase.CrossbowBurstPause ||
            Phase == AttackPhase.SpearTelegraph  || Phase == AttackPhase.SpearAttack  ||
            Phase == AttackPhase.MagicTelegraph  || Phase == AttackPhase.MagicAttack  ||
            Phase == AttackPhase.MeleeComboTelegraph || Phase == AttackPhase.MeleeComboAttack ||
            Phase == AttackPhase.MeleeComboPause ||
            Phase == AttackPhase.KnivesTelegraph || Phase == AttackPhase.KnivesThrow ||
            Phase == AttackPhase.KnivesThrowPause ||
            Phase == AttackPhase.PierceTelegraph || Phase == AttackPhase.PierceDash ||
            Phase == AttackPhase.PierceStabHold  || Phase == AttackPhase.PierceStabFlick ||
            Phase == AttackPhase.JumpSlashDodgeback || Phase == AttackPhase.JumpSlashRise ||
            Phase == AttackPhase.JumpSlashAttack ||
            Phase == AttackPhase.FlipSlashRise || Phase == AttackPhase.FlipSlashLand ||
            Phase == AttackPhase.AbyssSlashTelegraph || Phase == AttackPhase.AbyssSlashSwipe ||
            Phase == AttackPhase.AbyssSlashPause ||
            Phase == AttackPhase.TendrilTelegraph || Phase == AttackPhase.TendrilReach ||
            Phase == AttackPhase.TendrilSwingTelegraph || Phase == AttackPhase.TendrilSwing ||
            Phase == AttackPhase.HomingVolleyDodgeback || Phase == AttackPhase.HomingVolleySwingTelegraph ||
            Phase == AttackPhase.HomingVolleySwing ||
            Phase == AttackPhase.SwordLaunchReposition ||
            Phase == AttackPhase.BoomerangSwingTelegraph || Phase == AttackPhase.BoomerangSwing ||
            Phase == AttackPhase.SpiralFanSwingTelegraph || Phase == AttackPhase.SpiralFanSwing ||
            Phase == AttackPhase.SpiralFanBurst || Phase == AttackPhase.SpiralFanPause ||
            Phase == AttackPhase.FireVolleyBackLeap || Phase == AttackPhase.FireVolleyDodgeThrough ||
            Phase == AttackPhase.FireVolleyArcJump ||
            (_flight != null && _flight.IsDiving && MeleeWeaponItemType >= 0);

        private bool IsMeleeComboPhase =>
            Phase == AttackPhase.MeleeComboTelegraph || Phase == AttackPhase.MeleeComboAttack ||
            Phase == AttackPhase.MeleeComboPause || Phase == AttackPhase.MeleeComboRecovery;

        private bool IsWeaponPosePhase => IsWeaponVisiblePhase;

        // ── Direction hold (anti-bounce) ──────────────────────────────────────────
        /// <summary>
        /// Ticks remaining during which the current facing direction is locked.
        /// Prevents rapid flip-flopping when the player dodgerolls past the invader —
        /// the navigator would otherwise flip direction every single tick.
        /// </summary>
        private int _directionHoldTicks;

        // ── Telegraph flash ───────────────────────────────────────────────────────
        /// <summary>True once the flash VFX for the current telegraph phase has been spawned.</summary>
        private bool _flashFired;

        // ── Melee combo state ────────────────────────────────────────────────────
        /// <summary>Pool of combos for the current melee archetype.  Cached at first use.</summary>
        private MeleeCombo[] _meleeComboPool;
        /// <summary>Pool of combos for the current ranged archetype.  Cached at first use.</summary>
        private RangedCombo[] _rangedComboPool;
        /// <summary>Per-combo cooldown counters (sized to pool length on first use).</summary>
        private int[] _meleeComboCooldowns;
        private int[] _rangedComboCooldowns;
        /// <summary>The combo currently executing (only valid in MeleeCombo* phases).</summary>
        private MeleeCombo _activeMeleeCombo;
        /// <summary>Index of the active combo within its pool.  -1 = no combo active.</summary>
        private int _activeMeleeComboIndex = -1;
        /// <summary>Which step (0-indexed) of the active combo we're currently executing.</summary>
        private int _meleeComboStepIndex;
        /// <summary>Direction locked for the entire active combo.  Player can dodgeroll through
        /// and end up safely behind the swing arc — FighterAI's direction flips are overridden
        /// across all four MeleeCombo* phases.</summary>
        private int _comboLockedDir;
        /// <summary>Toggled every combo start; when <see cref="UseAlternateFlip"/> is on, the arc
        /// case in TickWeaponAnim swaps its start/end endpoints on alternating swings.</summary>
        private bool _comboSwingFlipped;
        /// <summary>Captured once at combo start from the target's relative height; when
        /// <see cref="UseAimAdaptiveArc"/> is on, added to both arc endpoints so the swing biases
        /// toward an airborne/crouched target instead of always arcing level with the ground.</summary>
        private float _comboAimBias;

        /// <summary>Toggles the alternate-flip parity and (re)computes the aim-adaptive bias for the
        /// swing that's about to play. Called at the start of every swing/combo — both the combo
        /// system (TryStartMeleeCombo) and the plain one-shot MeleeAttack path — so an invader that
        /// opts into UseAlternateFlip/UseAimAdaptiveArc gets consistent behavior regardless of which
        /// path picked the attack. heightDiff: NPC.Center.Y - target.Center.Y (positive = target
        /// above).</summary>
        private void ArmSwingVariation(float heightDiff)
        {
            _comboSwingFlipped = !_comboSwingFlipped;
            _comboAimBias = MathHelper.Clamp(-heightDiff / 260f, -0.35f, 0.35f);
        }
        /// <summary>True while a LeapSlam step is mid-air (set at launch, cleared when it lands or
        /// the airtime cap expires).  Gates the landing check that fires the slam hit.</summary>
        private bool _comboLeapLaunched;
        /// <summary>Horizontal velocity locked in at LeapSlam launch (sized to land near the player),
        /// re-asserted each airborne tick so SF4 doesn't steer the arc.</summary>
        private float _comboLeapVx;

        // ── Ranged combo state ────────────────────────────────────────────────────
        /// <summary>The ranged combo currently executing.</summary>
        private RangedCombo _activeRangedCombo;
        /// <summary>Index of the active ranged combo within its pool.  -1 = not using combo system.</summary>
        private int _activeRangedComboIndex = -1;
        /// <summary>Which shot (0-indexed) we're firing.</summary>
        private int _rangedComboShotIndex;

        // ── Archetype detection (lazy-cached) ─────────────────────────────────────
        private WeaponArchetype? _autoMeleeArchetype;
        private WeaponArchetype? _autoRangedArchetype;

        /// <summary>
        /// Weapon archetype that determines the melee combo pool for this invader.
        /// Auto-detected from the item's useStyle/damage class; override to force a specific archetype.
        /// </summary>
        protected virtual WeaponArchetype MeleeArchetype
        {
            get
            {
                if (_autoMeleeArchetype.HasValue) return _autoMeleeArchetype.Value;
                if (MeleeWeaponItemType <= 0) { _autoMeleeArchetype = WeaponArchetype.None; return WeaponArchetype.None; }
                var probe = new Item();
                probe.SetDefaults(MeleeWeaponItemType);
                var a = WeaponArchetypeTables.DetectMelee(probe);
                _autoMeleeArchetype = a;
                return a;
            }
        }

        /// <summary>
        /// Weapon archetype that determines the ranged combo pool for this invader.
        /// Auto-detected from the primary ranged item; override to force a specific archetype.
        /// </summary>
        protected virtual WeaponArchetype RangedArchetype
        {
            get
            {
                if (_autoRangedArchetype.HasValue) return _autoRangedArchetype.Value;
                if (RangedWeaponItemType <= 0) { _autoRangedArchetype = WeaponArchetype.None; return WeaponArchetype.None; }
                var probe = new Item();
                probe.SetDefaults(RangedWeaponItemType);
                var a = WeaponArchetypeTables.DetectRanged(probe);
                _autoRangedArchetype = a;
                return a;
            }
        }

        /// <summary>Chance (0-100) of preferring a combo over the legacy slash/stab/spear path
        /// when both are available.  Set to 0 to disable melee combos entirely.</summary>
        protected virtual int MeleeComboChance => 65;

        /// <summary>When true (default), the invader brakes during melee/spear/combo telegraphs so the
        /// wind-up reads as a planted swing.  Set false to keep pursuing through the wind-up so the player
        /// can't simply walk out of range during the telegraph.</summary>
        protected virtual bool SlowDownBeforeMelee => true;

        /// <summary>Base reach (px) for melee COMBO hitboxes before the step's ReachMult.  Default MeleeRange;
        /// override larger for long weapons (spears) so combo swings connect at their visual reach.</summary>
        protected virtual float ComboReachBase => MeleeRange;

        // ── Closing distance (anti-whiff for melee combos) ────────────────────────
        /// <summary>Distance (px) at which a chosen melee combo is close enough to actually connect.  If a
        /// combo is rolled while farther than this (but within <see cref="ComboMaxStartRange"/>), the invader
        /// first sprints in via <see cref="AttackPhase.ClosingDistance"/> and only swings once inside it.</summary>
        protected virtual float MeleeEngageRange => MeleeRange + 24f;
        /// <summary>Speed multiplier while closing distance to a melee target.</summary>
        protected virtual float ClosingDistanceSpeedMult => 1.4f;
        /// <summary>Max ticks spent closing before giving up and re-deciding (so it can switch to ranged).</summary>
        protected virtual int ClosingDistanceMaxTicks => 80;
        /// <summary>Chance (0-100) of preferring a ranged combo over the legacy random-burst path.
        /// Set to 0 to disable ranged combos entirely.</summary>
        protected virtual int RangedComboChance => 50;

        /// <summary>Multiplier applied to a melee combo's first-step TelegraphTicks at runtime.
        /// Default 1.35 = ~35% longer telegraph windows than the raw data table values, giving
        /// the player more time to read the incoming combo.  Lower for faster, more aggressive
        /// invaders.</summary>
        protected virtual float ComboTelegraphMultiplier => 1.35f;

        /// <summary>Hard floor on a combo's first-step telegraph duration (ticks).  Even after
        /// applying <see cref="ComboTelegraphMultiplier"/>, no combo can start with less than
        /// this many ticks of wind-up — guarantees the player sees the sword-raise / hold-at-apex
        /// arc clearly before damage is delivered.  30 ticks = 0.5 s minimum.</summary>
        protected virtual int MinComboTelegraphTicks => 30;

        /// <summary>Max distance (px) at which a melee combo can begin.  Beyond this, the
        /// invader falls through to ranged/legacy attack paths.  Default = StabRange + 80
        /// covers thrusts + most dash lunges.</summary>
        protected virtual float ComboMaxStartRange => StabRange + 80f;

        // ── Wings / flight ────────────────────────────────────────────────────────
        /// <summary>Master toggle: when true, this invader can take off, hover, dive, and land.
        /// Subclasses can wire this to a static config flag or a ModConfig field.</summary>
        protected virtual bool HasWings => false;

        /// <summary>Vanilla wing item type whose sprite + wingSlot is used for the puppet draw.
        /// Default is AngelWings; subclasses override for thematic fit.</summary>
        protected virtual int WingsAccessoryItemType => ItemID.AngelWings;

        /// <summary>Flight tuning.  Override to customize hover altitude / dive speed / cooldowns.</summary>
        protected virtual EnemyFlightConfig FlightConfig => EnemyFlightConfig.Default;

        /// <summary>Chance per second of triggering a random idle takeoff burst (0-100).
        /// Set to 0 to only fly when tactically necessary (player above, blocked, low HP).</summary>
        protected virtual int RandomTakeoffChance => 8;

        /// <summary>Height differential (player above invader) above which the invader
        /// will preferentially take off rather than try to navigate up on foot.</summary>
        protected virtual float FlightHeightTrigger => 120f;

        /// <summary>HP fraction at or below which the invader gains an aerial-phase preference
        /// (random takeoff chance is amplified, dive attacks more likely).</summary>
        protected virtual float FlightHpEscalationFrac => 0.50f;

        private EnemyFlightController _flight;
        private Item _wingsItemCache;
        private int  _cachedWingsType = -1;
        /// <summary>Cooldown between consecutive aerial-dive hits.  Ticks down each AI frame.</summary>
        private int  _aerialHitCooldown;

        // ── Stab / spear lunge direction locks ───────────────────────────────────
        /// <summary>NPC.direction captured when StabAttack begins. Using this instead of the
        /// live NPC.direction prevents rubber-banding when the player dodgerolls through during
        /// the lunge and FighterAI flips direction toward the new player position.</summary>
        private int _stabLungeDir;
        /// <summary>Same lock for SpearAttack — captures facing at the start of the poke so the
        /// weapon sprite doesn't snap to a new direction if the player moves slightly.</summary>
        private int _spearLungeDir;

        // ── Frame animation ───────────────────────────────────────────────────────
        private float _frameCounter;
        private const int FrameHeight = 56;

        // ── Dash afterimage trail (opt-in) ────────────────────────────────────────
        // Sparse translucent echoes of the puppet at recent NPC.oldPos[] positions, drawn via
        // vanilla's own fractal-afterimage fields (Player.isFirstFractalAfterImage /
        // firstFractalAfterImageOpacity — the same mechanism vanilla dash accessories use).  Set
        // AfterimageTicks > 0 (e.g. at the start of a dash) to show the trail for that many ticks;
        // it counts itself down in AI() and is a no-op for any invader that never sets it.
        protected int AfterimageTicks;
        /// <summary>Draw an echo every Nth cached old-position sample (NPC.oldPos has 10 slots by default).</summary>
        protected virtual int AfterimageSampleStep => 2;
        protected virtual float AfterimageOpacity => 0.4f;

        // ── Umbral Echo Step (optional dash follow-up) ─────────────────────────────
        // Rides along on any dash-type attack (Piercing Dash, Forward Flip Slash): on a chance roll
        // at the end of the dash, the last afterimage "solidifies" at a fixed point in space and
        // swings once more 45-60 ticks later, aimed at wherever the target is BY THEN (not where they
        // were during the original dash) - a genuine second dodge, not a cosmetic copy of the first.
        // A short local tell (dark motes gathering at the echo point) precedes a real swing: the
        // puppet's arm/body pose through the same Use1-Use4 rows and the held weapon sprite both
        // animate through the swing, semi-transparent but mostly visible - not just a dust effect.
        protected virtual bool  CanEchoStep        => false;
        protected virtual int   EchoStepChance     => 40;    // % rolled once per eligible dash-end
        protected virtual int   EchoStepDelayMin   => 45;
        protected virtual int   EchoStepDelayMax   => 60;
        protected virtual float EchoStepDamageMult => 0.65f; // fraction of MeleeDamage
        protected virtual int   EchoStepTellTicks  => 26;    // dust starts gathering this far out
        protected virtual int   EchoStepSwingTicks => 16;    // the visible swing itself, at the tail of the tell
        protected virtual float EchoStepReach      => 90f;
        protected virtual float EchoStepOpacity    => 0.75f; // mostly visible, not a faint afterimage

        private Vector2 _echoStepPos;
        private int     _echoStepDamage;
        private int     _echoStepDelayTimer = -1; // -1 = inactive
        private float   _echoStepRotation;
        private int     _echoStepSwingDir = 1;

        /// <summary>True while the echo's swing animation should be drawn this frame.</summary>
        private bool IsEchoStepSwinging => _echoStepDelayTimer > 0 && _echoStepDelayTimer <= EchoStepSwingTicks;

        /// <summary>Rolls <see cref="EchoStepChance"/>; on success, arms an echo swing at the
        /// invader's CURRENT position, <see cref="EchoStepDelayMin"/>-<see cref="EchoStepDelayMax"/>
        /// ticks from now. Call this from the end of a dash-type attack (hit or miss - the echo is a
        /// residual trace of the motion, not tied to whether the real swing connected).</summary>
        protected void TryArmEchoStep()
        {
            if (!CanEchoStep || Main.rand.Next(100) >= EchoStepChance)
                return;

            _echoStepPos = NPC.Center;
            _echoStepDamage = (int)(MeleeDamage * EchoStepDamageMult);
            _echoStepDelayTimer = Main.rand.Next(EchoStepDelayMin, EchoStepDelayMax + 1);
        }

        /// <summary>Called every AI tick regardless of Phase - the echo can resolve while the
        /// invader is doing something else entirely by the time it goes off.</summary>
        private void TickEchoStep()
        {
            if (_echoStepDelayTimer < 0)
                return;

            if (!NPC.active)
            {
                _echoStepDelayTimer = -1;
                return;
            }

            if (_echoStepDelayTimer <= EchoStepTellTicks && !Main.dedServ && Main.rand.NextBool(2))
            {
                Dust d = Dust.NewDustPerfect(_echoStepPos + Main.rand.NextVector2Circular(16f, 16f),
                    DustID.PurpleTorch, Vector2.Zero, 150, default, 0.7f);
                d.noGravity = true;
            }

            if (_echoStepDelayTimer <= EchoStepSwingTicks)
            {
                // Lock the facing the instant the visible swing starts, so the rendered pose and
                // the eventual hitbox always agree on which way it's swinging.
                if (_echoStepDelayTimer == EchoStepSwingTicks && NPC.HasValidTarget)
                {
                    Player target = Main.player[NPC.target];
                    _echoStepSwingDir = target.Center.X < _echoStepPos.X ? -1 : 1;
                }

                // Same swing arc as a basic MeleeAttack (-1.3 -> 1.0 rad) so it reads as the same
                // arm animation, just delayed and displaced.
                float swingT = 1f - _echoStepDelayTimer / (float)EchoStepSwingTicks;
                _echoStepRotation = MathHelper.Lerp(-1.3f, 1.0f, swingT);
            }

            _echoStepDelayTimer--;
            if (_echoStepDelayTimer == 0)
            {
                ResolveEchoStep();
                _echoStepDelayTimer = -1;
            }
        }

        private void ResolveEchoStep()
        {
            SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.45f, Pitch = -0.35f, PitchVariance = 0.1f }, _echoStepPos);

            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            int boxW = (int)Math.Max(100f, EchoStepReach * 1.4f);
            int boxH = 60;
            Vector2 center = _echoStepPos + new Vector2(_echoStepSwingDir * EchoStepReach * 0.5f, -8f);
            Vector2 topLeft = center - new Vector2(boxW / 2f, boxH / 2f);
            Projectile.NewProjectile(NPC.GetSource_FromThis(), topLeft, Vector2.Zero,
                ModContent.ProjectileType<Projectiles.Enemy.Weapons.InvaderMeleeHitbox>(),
                _echoStepDamage, 3f, Main.myPlayer, boxW, boxH);
        }

        /// <summary>
        /// Draws the echo mid-swing: the puppet posed to the same Use1-Use4 row a real swing would
        /// use (via <see cref="BodyRowFromWeaponRotation"/>), plus the held weapon drawn by hand using
        /// the same offset table as <see cref="GetHandPosition"/>. Deliberately does NOT go through
        /// <see cref="DrawingPuppetFor"/>/InvaderWeaponDrawLayer - that layer reads the invader's LIVE
        /// position/rotation/Phase, which would put the weapon back at the real Artorias instead of at
        /// the fixed echo point. Drawn at high opacity - a visible second swordsman, not a faint trail.
        /// </summary>
        private void DrawEchoStepPuppet(SpriteBatch spriteBatch)
        {
            if (_puppet == null || Main.dedServ)
                return;

            int bodyRow = BodyRowFromWeaponRotation(_echoStepRotation, _echoStepSwingDir);
            _puppet.bodyFrame = new Rectangle(0, FrameHeight * bodyRow, 40, FrameHeight);
            _puppet.legFrame  = new Rectangle(0, 0, 40, FrameHeight); // standing still, not walking
            _puppet.direction = _echoStepSwingDir;

            bool wasFractal = _puppet.isFirstFractalAfterImage;
            float wasOpacity = _puppet.firstFractalAfterImageOpacity;
            _puppet.isFirstFractalAfterImage = true;
            _puppet.firstFractalAfterImageOpacity = EchoStepOpacity;

            Vector2 echoTopLeft = _echoStepPos - new Vector2(NPC.width / 2f, NPC.height / 2f);
            Main.PlayerRenderer.DrawPlayer(Main.Camera, _puppet, echoTopLeft, 0f, Vector2.Zero);

            _puppet.isFirstFractalAfterImage = wasFractal;
            _puppet.firstFractalAfterImageOpacity = wasOpacity;

            DrawEchoStepWeapon(spriteBatch, bodyRow);
        }

        private void DrawEchoStepWeapon(SpriteBatch spriteBatch, int bodyRow)
        {
            if (_heldItemType <= 0)
                return;

            var texAsset = TextureAssets.Item[_heldItemType];
            if (texAsset?.Value == null)
                return;
            Texture2D tex = texAsset.Value;

            // Same arm-tip offset table as GetHandPosition(), just anchored to the echo's fixed
            // position/direction instead of the invader's live NPC.Center/direction.
            Vector2 offset = bodyRow switch
            {
                1 => new Vector2(-8f, -9f),
                2 => new Vector2(4f, -8f),
                3 => new Vector2(4f, 2f),
                4 => new Vector2(4f, 7f),
                _ => new Vector2(4f, 2f),
            };
            Vector2 handWorldPos = _echoStepPos + new Vector2(offset.X * _echoStepSwingDir, offset.Y);
            Vector2 drawPos = handWorldPos - Main.screenPosition;

            float visualRotation = _echoStepSwingDir * (_echoStepRotation + MeleeWeaponRotationOffset * _echoStepSwingDir);
            Vector2 hn = MeleeHandleNorm;
            float originX = _echoStepSwingDir == 1 ? tex.Width * hn.X : tex.Width * (1f - hn.X);
            Vector2 origin = new Vector2(originX, tex.Height * hn.Y);
            SpriteEffects effects = _echoStepSwingDir == 1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally;
            Color color = Lighting.GetColor(handWorldPos.ToTileCoordinates()) * EchoStepOpacity;

            spriteBatch.Draw(tex, drawPos, null, color, visualRotation, origin, MeleeWeaponDrawScale, effects, 0f);
        }

        // ─────────────────────────────────────────────────────────────────────────
        // AI
        // ─────────────────────────────────────────────────────────────────────────

        public override void AI()
        {
            // ── First-spawn invasion banner ────────────────────────────────────────
            // localAI[0] is not synced across the network, so every client (and singleplayer)
            // initialises it at 0 independently.  On the very first tick we fire the banner
            // exactly once per NPC instance on each client — no packet required.
            if (Main.netMode != NetmodeID.Server && NPC.localAI[0] == 0f)
            {
                NPC.localAI[0] = 1f;
                tsorcRevamp.LocationBannerText  = "INVADED BY " + InvaderTitle.ToUpperInvariant();
                tsorcRevamp.LocationBannerTimer = tsorcRevamp.LOCATION_BANNER_TOTAL;
            }

            // ── Lazy estus init ────────────────────────────────────────────────────
            if (_estusCharges < 0)
                _estusCharges = EstusChargesMax;

            if (_rangedCooldown          > 0) _rangedCooldown--;
            if (_secondaryRangedCooldown > 0) _secondaryRangedCooldown--;
            if (_stabCooldown            > 0) _stabCooldown--;
            if (_spearCooldown           > 0) _spearCooldown--;
            if (_magicCooldown           > 0) _magicCooldown--;
            if (_healCooldown            > 0) _healCooldown--;
            if (_breathCooldown          > 0) _breathCooldown--;
            if (_cursedKnivesCooldown    > 0) _cursedKnivesCooldown--;
            if (_pierceCooldown          > 0) _pierceCooldown--;
            if (_jumpSlashCooldown       > 0) _jumpSlashCooldown--;
            if (_flipSlashCooldown       > 0) _flipSlashCooldown--;
            if (_abyssSlashCooldown      > 0) _abyssSlashCooldown--;
            if (_tendrilCooldown         > 0) _tendrilCooldown--;
            if (_abyssShardCooldown      > 0) _abyssShardCooldown--;
            if (_homingVolleyCooldown    > 0) _homingVolleyCooldown--;
            if (_boomerangCooldown       > 0) _boomerangCooldown--;
            if (_spiralFanCooldown       > 0) _spiralFanCooldown--;
            if (AfterimageTicks          > 0) AfterimageTicks--;
            TickEchoStep(); // independent of Phase - can resolve while the invader is doing anything else
            if (_meleeComboCooldowns != null)
                for (int i = 0; i < _meleeComboCooldowns.Length; i++)
                    if (_meleeComboCooldowns[i] > 0) _meleeComboCooldowns[i]--;
            if (_rangedComboCooldowns != null)
                for (int i = 0; i < _rangedComboCooldowns.Length; i++)
                    if (_rangedComboCooldowns[i] > 0) _rangedComboCooldowns[i]--;
            if (_aerialHitCooldown > 0) _aerialHitCooldown--;

            // Decay recent-damage memory so old hits don't keep triggering heals forever.
            _recentDamage = Math.Max(0f, _recentDamage - RecentDamageDecayRate);

            // Reset HP-threshold flags once the invader's HP rises back above each level
            // (heal restored it, or it just started above those values).
            float hpFrac = (float)NPC.life / NPC.lifeMax;
            if (_halfHpHealed    && hpFrac > 0.55f) _halfHpHealed    = false;
            if (_quarterHpHealed && hpFrac > 0.30f) _quarterHpHealed = false;

            if (_directionHoldTicks > 0)
                _directionHoldTicks--;

            // Push navigation settings onto the GlobalNPC instance so FighterAI uses them.
            // Tier 2 enables waypoint routing: when stuck, it scans for a horizontal opening,
            // an elevated ledge to jump to, or a platform edge to drop off.
            var gnpc = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            gnpc.CanUseRopes     = true; // Invaders default to rope-climbing (only takes effect on SF4-mover invaders)
            gnpc.MaxJumpPower    = InvaderJumpPower;
            gnpc.MaxJumpBoost    = InvaderJumpBoost;
            gnpc.CanDoubleJump   = InvaderCanDoubleJump;
            gnpc.DoubleJumpPower = InvaderDoubleJumpPower;
            gnpc.TeleportTelegraphTime = TeleportTelegraphTicks;
            gnpc.TeleportDustType      = TeleportDustTypeId;
            gnpc.TeleportDustColor     = TeleportDustTint;
            gnpc.TeleportDustScale     = TeleportDustScale;
            gnpc.TeleportDustCount     = TeleportDustCount;

            // Quick-step tuning (used by both on-hit and preemptive quick-steps via the shared executor).
            gnpc.QuickStepRecoveryTicks = QuickStepRecoveryTicks;
            gnpc.QuickStepForwardRoom   = QuickStepForwardRoom;

            // SF4 invaders don't run BasicAI, which is what normally winds these down — so when this
            // invader uses a proactive dodge (jump/roll or preemptive quick-step) we tick the timers here.
            if (EvadesProjectiles || PreemptiveQuickStepChance > 0)
            {
                if (gnpc.DodgeTimer    > 0) gnpc.DodgeTimer--;
                if (gnpc.DodgeCooldown > 0) gnpc.DodgeCooldown--;
            }

            Player target = Main.player[NPC.target];
            float distToTarget = NPC.Distance(target.Center);

            // ── Flight tick (before ground movement) ──────────────────────────────
            // When airborne, the flight controller fully owns velocity / noGravity / direction.
            // We skip the ground navigator (SmartFighter4) entirely so it doesn't fight the
            // flight controller's intent.
            if (HasWings)
            {
                _flight ??= new EnemyFlightController(FlightConfig);
                _flight.Tick(NPC, target);
                if (_flight.IsAirborne)
                {
                    // Advance a quick-step (e.g. armed by an on-hit reaction) AFTER the flight tick so it
                    // overrides flight velocity; skip attacks while stepping / mid-dodge.
                    tsorcRevampAIs.TickQuickStep(NPC, gnpc);
                    if (gnpc.DodgeTimer <= 0 && gnpc.QuickStepTimer <= 0 && gnpc.QuickStepRecoveryTimer <= 0)
                        InvaderAttackAI();
                    UpdateAttackCommitFlags();
                    TickWeaponAnim();
                    return;
                }
            }

            float speedMult = (Phase == AttackPhase.CasualStroll || Phase == AttackPhase.Healing)
                ? CasualStrollSpeedMult : 1f;
            if (Phase == AttackPhase.Idle && distToTarget > RunDistance)
                speedMult *= RunSpeedMult;
            if (Phase == AttackPhase.ClosingDistance)
                speedMult *= ClosingDistanceSpeedMult;

            // Capture direction before the movement AI might change it.
            int dirBefore = NPC.direction;

            RunMovementAI(speedMult);

            // Anti-bounce: if FighterAI just reversed direction but the hold timer is still
            // running (e.g. the player dodgerolled past), revert to the previous direction so
            // the invader doesn't flip back and forth every tick.
            if (NPC.direction != dirBefore && _directionHoldTicks > 0)
                NPC.direction = dirBefore;
            else if (NPC.direction != dirBefore)
                _directionHoldTicks = 30; // lock new direction for ~½ s before allowing another flip

            // ── Flee-to-heal movement override ────────────────────────────────────
            // FighterAI always moves TOWARD the player.  When fleeing we invert velocity.X
            // so the invader sprints away instead.  Y velocity (jumping) from FighterAI is
            // preserved so it can still clear obstacles while fleeing.
            if (Phase == AttackPhase.FleeToHeal)
            {
                Player fleeFrom      = Main.player[NPC.target];
                float  awayDir       = Math.Sign(NPC.Center.X - fleeFrom.Center.X);
                if (awayDir == 0) awayDir = -NPC.direction;
                NPC.velocity.X       = (float)awayDir * TopSpeed * 2.0f;
                NPC.direction        = (int)awayDir;
                _directionHoldTicks  = 5; // allow quick correction while fleeing
            }

            // ── Secondary-ranged backhop momentum ─────────────────────────────────
            // Re-assert the hop-away velocity for a short window so the SF4 mover's pursuit (which ran
            // just above) doesn't cancel it on the very next tick.  Decays linearly to 0.
            if (_backhopTicks > 0)
            {
                NPC.velocity.X = _backhopDir * SecondaryRangedBackhopSpeed * (_backhopTicks / (float)BackhopMomentumTicks);
                _backhopTicks--;
            }

            // Rope climbing, X-centering, stuck detection, and teleport recovery are all owned by
            // SmartFighter4 (RunMovementAI).  The old FighterAI-era rope-climb and wall-blocked
            // overrides that used to live here fought the navigator and have been removed.

            // Advance a quick-step (on-hit or preemptive) AFTER the mover so it overrides pursuit velocity.
            tsorcRevampAIs.TickQuickStep(NPC, gnpc);

            // ── Proactive evasion (neutral, off cooldown, not mid-backhop/quick-step) ─────────────
            if (gnpc.DodgeTimer <= 0 && gnpc.DodgeCooldown <= 0 && _backhopTicks <= 0
                && gnpc.QuickStepTimer <= 0 && gnpc.QuickStepRecoveryTimer <= 0
                && (Phase == AttackPhase.Idle || Phase == AttackPhase.CasualStroll))
            {
                // Preemptive quick-step THROUGH a player who's mid-swing in melee range.
                if (PreemptiveQuickStepChance > 0)
                    TryPreemptiveQuickStep(gnpc, target, distToTarget);

                // Jump / i-frame roll at incoming RANGED shots (gated >160px so melee swing hitboxes —
                // also "friendly projectiles" — don't trigger it; the legacy BasicAI scan uses the same gate).
                if (EvadesProjectiles && gnpc.DodgeTimer <= 0 && gnpc.QuickStepTimer <= 0 && distToTarget > 160f)
                    TryEvadeIncomingProjectile(gnpc, target);
            }

            // Reactive shield: raise/plant the guard after movement so it overrides pursuit velocity.
            UpdateShield();

            // While the guard is up the invader commits to it — no attack starts until it drops
            // (the minimum hold), so the player gets a real block/backstab window.

            // ── Dodge-roll movement guard ──────────────────────────────────────────
            // When the GlobalNPC dodge system fires it sets velocity.X = 5 * direction and
            // grants invulnerability for DodgeTimer ticks.  If we then run InvaderAttackAI
            // it would call SlowDown() or set a stab-lunge velocity, overriding the dash.
            // Instead we skip attack AI entirely for those ticks so the dodge movement lands.
            // Also skip while the shield guard is committed so it holds its minimum window.
            if (gnpc.DodgeTimer <= 0 && gnpc.QuickStepTimer <= 0 && gnpc.QuickStepRecoveryTimer <= 0 && !_shielding)
                InvaderAttackAI();

            UpdateAttackCommitFlags();
            TickWeaponAnim();
        }

        /// <summary>
        /// Returns true when the invader should interrupt whatever it is doing to flee and heal.
        /// Checked at the top of InvaderAttackAI every tick.
        /// </summary>
        private bool ShouldHeal()
        {
            if (_estusCharges <= 0 || _healCooldown > 0) return false;
            // Don't interrupt an already-running heal sequence.
            if (Phase == AttackPhase.FleeToHeal || Phase == AttackPhase.Healing) return false;

            float hp = (float)NPC.life / NPC.lifeMax;

            // Threshold 1: first time HP falls at or below 50 %.
            if (!_halfHpHealed && hp <= 0.50f) return true;
            // Threshold 2: first time HP falls at or below 25 %.
            if (!_quarterHpHealed && hp <= 0.25f) return true;
            // Threshold 3: burst damage — took a lot of damage in a short window.
            if (_recentDamage >= RecentDamageThreshold) return true;

            return false;
        }

        private void InvaderAttackAI()
        {
            Player target = Main.player[NPC.target];
            float  dist   = NPC.Distance(target.Center);
            bool   hasLOS = Collision.CanHitLine(NPC.Center, 1, 1, target.Center, 1, 1);

            // ── Healing intercept (highest priority) ──────────────────────────────
            // Check before the main switch so any phase can be interrupted to flee and heal.
            if (ShouldHeal())
            {
                EnterPhase(AttackPhase.FleeToHeal, FleeToHealMaxTicks);
                return;
            }

            // ── Charge-up Nova intercept (health-threshold set-piece) ─────────────
            // Also checked before the main switch: a nova can interrupt any in-progress attack.
            if (CanNova && Phase != AttackPhase.NovaCharge && Phase != AttackPhase.NovaBlast
                && Phase != AttackPhase.NovaRecovery && ShouldTriggerNova())
            {
                EnterPhase(AttackPhase.NovaCharge, NovaChargeTicks);
                return;
            }

            // Weapon only exists visually during telegraphs and attack frames.
            _weaponVisible = IsWeaponVisiblePhase;

            switch (Phase)
            {
                case AttackPhase.Idle:
                    if (_heldItemType <= 0)
                        SetDisplayWeapon(MeleeWeaponItemType >= 0 ? MeleeWeaponItemType : RangedWeaponItemType, swing: false);

                    // ── Wings: airborne behavior + takeoff triggers ───────────────
                    // Checked BEFORE the LOS gate below: a flying invader must still be able to
                    // reposition (dive/strafe variety roll) and eventually land even when a single-ray
                    // LOS check is briefly blocked by minor terrain (a corner, an overhang).  Without
                    // this, losing LOS while airborne parks the invader in Hover — doing nothing but
                    // the idle bob — until the flight time budget forces a landing, and it can read as
                    // a total freeze.  The individual attack triggers inside this block (breath/knives/
                    // ranged) already re-check hasLOS themselves before firing.
                    if (HasWings && _flight != null)
                    {
                        if (_flight.IsAirborne)
                        {
                            // While airborne, choose aerial actions instead of ground combos.
                            // The flight controller's Hover mode auto-refreshes itself; we
                            // periodically interrupt with a strafe or dive for variety/damage.
                            if (_flight.Mode == FlightMode.Hover && Main.rand.Next(90) == 0)
                            {
                                if (Main.rand.Next(2) == 0)
                                {
                                    _flight.RequestDive(target.Center);
                                    DoAerialDiveTelegraph();
                                }
                                else
                                {
                                    float side = Math.Sign(NPC.Center.X - target.Center.X);
                                    if (side == 0) side = -NPC.direction;
                                    _flight.RequestStrafe(target.Center + new Vector2(-side * 240f, 0f));
                                }
                            }
                            else if (_flight.IsDiving && _aerialHitCooldown <= 0)
                            {
                                // Dive contact: drop a hitbox at NPC center.
                                DoAerialDiveHit();
                                _aerialHitCooldown = 8; // brief inter-hit cooldown during dive
                            }
                            else if (_flight.Mode == FlightMode.Hover
                                  && CanBreathe && BreathAllowedAirborne
                                  && _breathCooldown <= 0
                                  && Main.rand.Next(130) == 0
                                  && NPC.Distance(target.Center) <= BreathRange
                                  && hasLOS)
                            {
                                // Aerial breath: charge while hovering, then the BreathTelegraph→Breathing
                                // transition requests a strafe so the stream sweeps across as it flies past.
                                EnterPhase(AttackPhase.BreathTelegraph, BreathTelegraphTicks);
                            }
                            else if (_flight.Mode == FlightMode.Hover
                                  && CanThrowCursedKnives && CursedKnivesAllowedAirborne
                                  && _cursedKnivesCooldown <= 0
                                  && Main.rand.Next(140) == 0
                                  && NPC.Distance(target.Center) <= CursedKnivesRange
                                  && hasLOS)
                            {
                                StartCursedKnives(NPC.Distance(target.Center), hasLOS);
                            }
                            else if (_flight.Mode == FlightMode.Hover
                                  && Main.rand.Next(150) == 0
                                  && RangedWeaponItemType >= 0
                                  && _rangedCooldown <= 0
                                  && hasLOS)
                            {
                                // Aerial ranged: route through the full RangedTelegraph phase
                                // so the player sees the weapon visibly held in hand (stars
                                // raised, crossbow extended) and the aim animation completes
                                // before firing — same wind-up convention as ground shots.
                                // ForceStanding = true because the flight controller fully owns
                                // velocity while airborne; the telegraph's velocity damping is
                                // gated on !IsAirborne and would otherwise no-op anyway.
                                SetupRangedBurst(useSecondary: false, shotsOverride: 1, forceStanding: true);
                                EnterPhase(AttackPhase.RangedTelegraph, _activeRangedTelegraphTicks);
                            }
                            break;
                        }
                        else
                        {
                            // Grounded: roll for tactical takeoff.
                            bool playerAbove = target.Center.Y < NPC.Center.Y - FlightHeightTrigger;
                            float hpFrac     = (float)NPC.life / NPC.lifeMax;
                            int   randChance = hpFrac <= FlightHpEscalationFrac
                                             ? RandomTakeoffChance * 3
                                             : RandomTakeoffChance;
                            bool randomBurst = randChance > 0
                                             && Main.GameUpdateCount % 60 == 0
                                             && Main.rand.Next(100) < randChance;

                            if (playerAbove || randomBurst)
                            {
                                if (_flight.RequestTakeoff())
                                    break;
                            }
                        }
                    }

                    if (!hasLOS)
                        break;

                    // ── Fire breath intercept (grounded) ──────────────────────────
                    // Reached only when grounded (the airborne block above breaks first).  Rolls
                    // BreathChance to plant and charge a sustained breath stream.
                    if (CanBreathe && _breathCooldown <= 0 && NPC.velocity.Y == 0f
                        && dist <= BreathRange && dist >= MinBreathRange
                        && Main.rand.Next(100) < BreathChance)
                    {
                        EnterPhase(AttackPhase.BreathTelegraph, BreathTelegraphTicks);
                        break;
                    }

                    // ── Cursed Knives intercept (grounded) ────────────────────────
                    if (CanThrowCursedKnives && _cursedKnivesCooldown <= 0 && NPC.velocity.Y == 0f
                        && dist <= CursedKnivesRange && dist >= CursedKnivesMinRange
                        && Main.rand.Next(100) < CursedKnivesChance)
                    {
                        StartCursedKnives(dist, hasLOS);
                        break;
                    }

                    // ── Piercing Dash intercept (grounded) ────────────────────────
                    if (CanPierce && _pierceCooldown <= 0 && NPC.velocity.Y == 0f
                        && dist <= PierceRange && dist >= MinPierceRange
                        && Main.rand.Next(100) < PierceChance)
                    {
                        _pierceIsStab = Main.rand.Next(100) < PierceStabChance;
                        _pierceHitConnected = false;
                        EnterPhase(AttackPhase.PierceTelegraph, PierceTelegraphTicks);
                        break;
                    }

                    // ── Jumping Downward Slash intercept (grounded) ───────────────
                    if (CanJumpSlash && _jumpSlashCooldown <= 0 && NPC.velocity.Y == 0f
                        && dist >= JumpSlashMinRange && dist <= JumpSlashMaxRange
                        && Main.rand.Next(100) < JumpSlashChance)
                    {
                        _lastAttackHitConnected = false;
                        EnterPhase(AttackPhase.JumpSlashDodgeback, JumpSlashDodgebackTicks);
                        break;
                    }

                    // ── Forward Flip Slash intercept (grounded) ───────────────────
                    if (CanFlipSlash && _flipSlashCooldown <= 0 && NPC.velocity.Y == 0f
                        && dist >= FlipSlashMinRange && dist <= FlipSlashMaxRange
                        && Main.rand.Next(100) < FlipSlashChance)
                    {
                        _flipHitConnected = false;
                        EnterPhase(AttackPhase.FlipSlashRise, FlipSlashRiseMaxTicks);
                        break;
                    }

                    // ── Abyss Slash intercept (grounded) ──────────────────────────
                    if (CanAbyssSlash && _abyssSlashCooldown <= 0 && NPC.velocity.Y == 0f
                        && dist >= AbyssSlashMinRange && dist <= AbyssSlashMaxRange
                        && Main.rand.Next(100) < AbyssSlashChance)
                    {
                        _lastAttackHitConnected = false;
                        EnterPhase(AttackPhase.AbyssSlashTelegraph, AbyssSlashArcTicks + AbyssSlashHoldTicks);
                        break;
                    }

                    // ── Abyss Tendril Grab intercept (grounded) ───────────────────
                    if (CanTendrilGrab && _tendrilCooldown <= 0 && NPC.velocity.Y == 0f
                        && dist >= TendrilMinRange && dist <= TendrilMaxRange
                        && Main.rand.Next(100) < TendrilChance)
                    {
                        _lastAttackHitConnected = false;
                        EnterPhase(AttackPhase.TendrilTelegraph, TendrilTelegraphTicks);
                        break;
                    }

                    // ── Abyss Shard intercept (grounded) ──────────────────────────
                    if (CanAbyssShard && _abyssShardCooldown <= 0 && NPC.velocity.Y == 0f
                        && dist >= AbyssShardMinRange && dist <= AbyssShardMaxRange
                        && Main.rand.Next(100) < AbyssShardChance)
                    {
                        EnterPhase(AttackPhase.AbyssShardTelegraph, AbyssShardTelegraphTicks);
                        break;
                    }

                    // ── Homing Volley intercept (grounded, midrange) ──────────────
                    if (CanHomingVolley && _homingVolleyCooldown <= 0 && NPC.velocity.Y == 0f
                        && dist >= HomingVolleyMinRange && dist <= HomingVolleyMaxRange
                        && Main.rand.Next(100) < HomingVolleyChance)
                    {
                        EnterPhase(AttackPhase.HomingVolleyDodgeback, HomingVolleyDodgebackTicks);
                        break;
                    }

                    // ── Boomerang Crescent intercept (grounded) ───────────────────
                    // Wide range band (down to point-blank) since SwordLaunchReposition handles
                    // spacing itself - hops back first only if the player is too close.
                    if (CanBoomerang && _boomerangCooldown <= 0 && NPC.velocity.Y == 0f
                        && dist >= BoomerangMinRange && dist <= BoomerangMaxRange
                        && Main.rand.Next(100) < BoomerangChance)
                    {
                        if (dist < SwordLaunchRepositionTooCloseRange)
                        {
                            _swordLaunchNextPhase = AttackPhase.BoomerangSwingTelegraph;
                            _swordLaunchNextTicks = BoomerangSwingTelegraphTicks;
                            EnterPhase(AttackPhase.SwordLaunchReposition, SwordLaunchRepositionTicks);
                        }
                        else
                        {
                            EnterPhase(AttackPhase.BoomerangSwingTelegraph, BoomerangSwingTelegraphTicks);
                        }
                        break;
                    }

                    // ── Spiral Fan intercept (grounded) ───────────────────────────
                    if (CanSpiralFan && _spiralFanCooldown <= 0 && NPC.velocity.Y == 0f
                        && dist >= SpiralFanMinRange && dist <= SpiralFanMaxRange
                        && Main.rand.Next(100) < SpiralFanChance)
                    {
                        OnSpiralFanSequenceStart();
                        if (dist < SwordLaunchRepositionTooCloseRange)
                        {
                            _swordLaunchNextPhase = AttackPhase.SpiralFanSwingTelegraph;
                            _swordLaunchNextTicks = SpiralFanSwingTelegraphTicks;
                            EnterPhase(AttackPhase.SwordLaunchReposition, SwordLaunchRepositionTicks);
                        }
                        else
                        {
                            EnterPhase(AttackPhase.SpiralFanSwingTelegraph, SpiralFanSwingTelegraphTicks);
                        }
                        break;
                    }

                    // ── Secondary-ranged panic backhop ────────────────────────────
                    // Player got too close: hop away (still facing them) and fire a single secondary
                    // shot to reset spacing.  Bypasses the normal secondary min-range band.
                    if (SecondaryRangedWeaponItemType >= 0
                        && SecondaryRangedAvailable
                        && SecondaryRangedBackhopRange > 0f
                        && _secondaryRangedCooldown <= 0
                        && NPC.velocity.Y == 0f
                        && dist <= SecondaryRangedBackhopRange
                        && Main.rand.Next(100) < SecondaryRangedBackhopChance)
                    {
                        int away = NPC.Center.X < target.Center.X ? -1 : 1; // direction AWAY from player
                        _backhopDir   = away;
                        _backhopTicks = BackhopMomentumTicks; // AI() carries the away-velocity over this window
                        NPC.velocity.Y = -SecondaryRangedBackhopUpSpeed; // one-time upward arc
                        NPC.direction = NPC.spriteDirection = -away; // keep facing the player to aim
                        _directionHoldTicks = Math.Max(_directionHoldTicks, BackhopMomentumTicks);
                        SetupRangedBurst(useSecondary: true, shotsOverride: 1, forceStanding: false);
                        _standingShot = false; // mobile shot so the telegraph doesn't hard-brake the hop
                        EnterPhase(AttackPhase.RangedTelegraph, _activeRangedTelegraphTicks);
                        break;
                    }

                    // ── Melee combo intercept ─────────────────────────────────────
                    // Roll first: combo system competes with the legacy slash/stab path.
                    // Combos require grounded, height-accessible target, archetype set, AND
                    // player within effective combo reach so the swings actually connect.
                    if (MeleeArchetype != WeaponArchetype.None
                        && MeleeWeaponItemType >= 0
                        && NPC.velocity.Y == 0f
                        && NPC.Center.Y - target.Center.Y < 48f
                        && dist <= ComboMaxStartRange
                        && Main.rand.Next(100) < MeleeComboChance)
                    {
                        // Out of hittable reach → close the gap first (no swing until in range).
                        if (dist > MeleeEngageRange)
                        {
                            EnterPhase(AttackPhase.ClosingDistance, ClosingDistanceMaxTicks);
                            break;
                        }
                        if (TryStartMeleeCombo(dist))
                            break;
                    }

                    // Gate melee/stab on vertical accessibility: if the player is standing on a
                    // ledge above the invader, skip attack phases and let FighterAI navigate up
                    // first.  Without this the invader telegraphs but can never connect.
                    // Also gate on grounded (velocity.Y == 0) — mid-air attacks cause the invader
                    // to mid-air flip direction after overshooting and attack in the wrong direction.
                    float heightDiff = NPC.Center.Y - target.Center.Y; // positive = player above

                    bool wantSlash  = MeleeWeaponItemType  >= 0 && dist <= MeleeRange  && heightDiff < 36f
                                      && NPC.velocity.Y == 0f;
                    bool wantStab   = CanStab               && dist <= StabRange && dist > MeleeRange
                                      && _stabCooldown <= 0 && heightDiff < 48f && NPC.velocity.Y == 0f;
                    // Spear poke: medium-to-long melee reach, grounded, no lunge required.
                    bool wantSpear  = SpearWeaponItemType >= 0 && dist <= SpearRange
                                      && _spearCooldown <= 0 && heightDiff < 48f && NPC.velocity.Y == 0f;
                    bool wantPrimary   = RangedWeaponItemType >= 0 && dist <= RangedRange
                                         && dist >= MinRangedRange && _rangedCooldown <= 0;
                    bool wantSecondary = SecondaryRangedWeaponItemType >= 0 && SecondaryRangedAvailable
                                         && dist <= SecondaryRangedRange
                                         && dist >= SecondaryRangedMinRange && _secondaryRangedCooldown <= 0;
                    // Magic: fires from any elevation (no velocity.Y check), blocked at very close range.
                    bool wantMagic     = MagicWeaponItemType >= 0 && dist <= MagicRange
                                         && dist >= MinMagicRange && _magicCooldown <= 0;

                    if (wantSlash)
                    {
                        ArmSwingVariation(heightDiff);
                        EnterPhase(AttackPhase.MeleeTelegraph, MeleeTelegraphTicks);
                    }
                    else if (wantStab)
                        EnterPhase(AttackPhase.StabTelegraph, StabTelegraphTicks);
                    else if (wantSpear)
                    {
                        _spearLungeDir = NPC.direction;
                        EnterPhase(AttackPhase.SpearTelegraph, SpearTelegraphTicks);
                    }
                    else if (wantPrimary || wantSecondary || wantMagic)
                    {
                        // Magic competes with ranged: always when it's the only option in range, else on a
                        // MagicPreferenceChance roll (default 0 = magic only fills gaps, original behavior).
                        bool useMagic = wantMagic && (!(wantPrimary || wantSecondary)
                                        || Main.rand.Next(100) < MagicPreferenceChance);
                        if (useMagic)
                        {
                            EnterPhase(AttackPhase.MagicTelegraph, MagicTelegraphTicks);
                        }
                        else
                        {
                            // Pick secondary when both are available (SecondaryRangedChance roll),
                            // or always if only secondary is in range.
                            bool useSecondary = wantSecondary &&
                                (!wantPrimary || Main.rand.Next(100) < SecondaryRangedChance);
                            SetupRangedBurst(useSecondary, shotsOverride: -1, forceStanding: false);
                            EnterPhase(AttackPhase.RangedTelegraph, _activeRangedTelegraphTicks);
                        }
                    }
                    break;

                // ── Melee slash ───────────────────────────────────────────────
                case AttackPhase.MeleeTelegraph:
                    if (SlowDownBeforeMelee) SlowDown();
                    SetDisplayWeapon(MeleeWeaponItemType, swing: false);
                    CheckAndFireFlash(MeleeTelegraphFlashColor);
                    if (--PhaseTimer <= 0)
                    {
                        SetDisplayWeapon(MeleeWeaponItemType, swing: true);
                        _bladeArmed = false;
                        DoMeleeAttack();
                        EnterPhase(AttackPhase.MeleeAttack, GetMeleeSwingTicks(MeleeAttackTicks));
                    }
                    break;

                case AttackPhase.MeleeAttack:
                    TickBladeHit();
                    if (--PhaseTimer <= 0)
                        EnterPhase(AttackPhase.MeleeRecovery, MeleeRecoveryTicks);
                    break;

                case AttackPhase.MeleeRecovery:
                    if (--PhaseTimer <= 0)
                        EnterCasualOrIdle();
                    break;

                // ── Stab / lunge ──────────────────────────────────────────────
                case AttackPhase.StabTelegraph:
                    if (SlowDownBeforeMelee) SlowDown();
                    SetDisplayWeapon(MeleeWeaponItemType, swing: false);
                    SpawnTelegraphDust();
                    CheckAndFireFlash(MeleeTelegraphFlashColor);
                    if (--PhaseTimer <= 0)
                    {
                        _stabLungeDir = NPC.direction; // lock direction so lunge can't rubber-band
                        _bladeArmed = false;
                        DoStabAttack();
                        EnterPhase(AttackPhase.StabAttack, StabAttackTicks);
                    }
                    break;

                case AttackPhase.StabAttack:
                    TickBladeHit();
                    // Use the direction locked at lunge start — not the live NPC.direction which
                    // FighterAI may flip if the player dodgerolls through to the other side.
                    // Lunge speed = TopSpeed × StabLungeSpeedMult.  Default 2.0 is firm but not
                    // overshooting; override per-subclass for more/less aggressive dashes.
                    NPC.velocity.X      = _stabLungeDir * (TopSpeed * StabLungeSpeedMult);
                    NPC.direction       = _stabLungeDir;
                    NPC.spriteDirection = _stabLungeDir;
                    _directionHoldTicks = Math.Max(_directionHoldTicks, 5);
                    if (--PhaseTimer <= 0)
                        EnterPhase(AttackPhase.StabRecovery, StabRecoveryTicks);
                    break;

                case AttackPhase.StabRecovery:
                    if (--PhaseTimer <= 0)
                    {
                        _stabCooldown = StabCooldownAfterUse; // prevent instant re-stab after dodgeroll
                        EnterCasualOrIdle();
                    }
                    break;

                // ── Ranged ────────────────────────────────────────────────────
                // Telegraph: raise arm and aim.  Always decelerate so the invader
                // clearly "prepares to throw".  Standing shots brake harder to a full
                // stop; moving shots just slow — this still reads as deliberate aiming.
                case AttackPhase.RangedTelegraph:
                    // Skip ground velocity damping while airborne — flight controller owns velocity.
                    if (_flight == null || !_flight.IsAirborne)
                    {
                        if (_standingShot)
                            NPC.velocity.X *= 0.10f; // planted shot — brake to nearly stopped
                        else
                            SlowDown();               // mobile shot — decelerate but still drifting
                    }
                    SetDisplayWeapon(_activeRangedItemType, swing: false);
                    CheckAndFireFlash(_activeRangedFlashColor);
                    if (--PhaseTimer <= 0)
                    {
                        SetDisplayWeapon(_activeRangedItemType, swing: true);
                        DoRangedAttack();
                        EnterPhase(AttackPhase.RangedAttack, _activeRangedAttackTicks);
                    }
                    break;

                case AttackPhase.RangedAttack:
                    if (_flight == null || !_flight.IsAirborne)
                    {
                        if (_standingShot)
                            NPC.velocity.X *= 0.25f; // planted shot — bleed off any residual momentum
                        else
                            SlowDown();               // mobile shot — continue decelerating through follow-through
                    }
                    if (--PhaseTimer <= 0)
                    {
                        if (_interShotPauses != null)
                        {
                            // ── Crossbow burst-pattern mode ───────────────────────────────────
                            // Each entry in _interShotPauses is a pause before the next shot.
                            // Exhausting all pauses means the final shot just fired → recovery.
                            if (_interShotPauseIndex < _interShotPauses.Length)
                            {
                                // Hold aim, wait the specified duration, then fire next shot.
                                EnterPhase(AttackPhase.CrossbowBurstPause,
                                           _interShotPauses[_interShotPauseIndex++]);
                            }
                            else
                            {
                                // All pattern shots fired — burst complete.
                                SetDisplayWeapon(MeleeWeaponItemType >= 0 ? MeleeWeaponItemType : _activeRangedItemType, swing: false);
                                EnterPhase(AttackPhase.RangedRecovery, _activeRangedRecoveryTicks);
                            }
                        }
                        else
                        {
                            // ── Classic multi-shot mode (throwing stars, primary ranged) ───────
                            _rangedShotsRemaining--;
                            if (_rangedShotsRemaining > 0)
                            {
                                // More shots left in the burst — each gets a full telegraph wind-up.
                                _weaponAnim = 0; // reset swing counter so telegraph starts clean
                                EnterPhase(AttackPhase.RangedTelegraph, _activeRangedTelegraphTicks);
                            }
                            else
                            {
                                // Burst finished — switch back to melee sprite.
                                SetDisplayWeapon(MeleeWeaponItemType >= 0 ? MeleeWeaponItemType : _activeRangedItemType, swing: false);
                                EnterPhase(AttackPhase.RangedRecovery, _activeRangedRecoveryTicks);
                            }
                        }
                    }
                    break;

                case AttackPhase.RangedRecovery:
                    if (--PhaseTimer <= 0)
                    {
                        // Apply the cooldown to the weapon that was just used.
                        if (_usingSecondaryRanged)
                            _secondaryRangedCooldown = SecondaryRangedCooldownAfterUse;
                        else
                            _rangedCooldown = RangedCooldownAfterUse;
                        EnterCasualOrIdle();
                    }
                    break;

                // ── Crossbow burst pause ──────────────────────────────────────
                // Hold horizontal aim between shots in a pattern burst.
                // When the timer expires the next shot fires immediately — no re-telegraph.
                // This gives a deliberate "controlled volley" feel without extra wind-up.
                case AttackPhase.CrossbowBurstPause:
                    if (_flight == null || !_flight.IsAirborne)
                    {
                        if (_standingShot)
                            NPC.velocity.X *= 0.10f; // stay planted during the pause
                        else
                            SlowDown();
                    }
                    SetDisplayWeapon(_activeRangedItemType, swing: false);
                    if (--PhaseTimer <= 0)
                    {
                        // Fire next shot immediately — the arm is already at horizontal aim.
                        SetDisplayWeapon(_activeRangedItemType, swing: true);
                        DoRangedAttack();
                        EnterPhase(AttackPhase.RangedAttack, _activeRangedAttackTicks);
                    }
                    break;

                // ── Spear / polearm poke ──────────────────────────────────────
                // A stationary or short-push reach attack for long polearms.
                // SpearPushSpeedMult = 0 → pure stationary poke (no movement);
                // > 0 → small forward hop scaled by TopSpeed (less than stab lunge).
                case AttackPhase.SpearTelegraph:
                    if (SlowDownBeforeMelee) SlowDown();
                    SetDisplayWeapon(SpearWeaponItemType, swing: false);
                    SpawnTelegraphDust();
                    CheckAndFireFlash(SpearTelegraphFlashColor);
                    if (--PhaseTimer <= 0)
                    {
                        SetDisplayWeapon(SpearWeaponItemType, swing: true);
                        _bladeArmed = false;
                        DoSpearAttack();
                        EnterPhase(AttackPhase.SpearAttack, SpearAttackTicks);
                    }
                    break;

                case AttackPhase.SpearAttack:
                    TickBladeHit();
                    // Lock to direction captured at telegraph start — prevents sprite snap if
                    // player moves to the other side while the arm is extending.
                    NPC.direction       = _spearLungeDir;
                    NPC.spriteDirection = _spearLungeDir;
                    _directionHoldTicks = Math.Max(_directionHoldTicks, 5);
                    if (SpearPushSpeedMult > 0f)
                        NPC.velocity.X = _spearLungeDir * (TopSpeed * SpearPushSpeedMult);
                    else
                        NPC.velocity.X *= 0.25f; // stationary poke — bleed off momentum
                    if (--PhaseTimer <= 0)
                        EnterPhase(AttackPhase.SpearRecovery, SpearRecoveryTicks);
                    break;

                case AttackPhase.SpearRecovery:
                    if (--PhaseTimer <= 0)
                    {
                        _spearCooldown = SpearCooldownAfterUse;
                        EnterCasualOrIdle();
                    }
                    break;

                // ── Magic / spellcast ─────────────────────────────────────────
                // Charge-up then fire a spell projectile.  The invader brakes to a
                // halt during the telegraph so it reads as a deliberate cast.
                case AttackPhase.MagicTelegraph:
                    SlowDown();
                    SetDisplayWeapon(MagicWeaponItemType, swing: false);
                    CheckAndFireFlash(MagicTelegraphFlashColor);
                    if (--PhaseTimer <= 0)
                    {
                        SetDisplayWeapon(MagicWeaponItemType, swing: true);
                        DoMagicAttack();
                        // A subclass can set _magicAttackTicksOverride inside DoMagicAttack to channel a
                        // sustained cast (DoMagicTick runs each tick below); -1 keeps the instant cast.
                        EnterPhase(AttackPhase.MagicAttack,
                                   _magicAttackTicksOverride > 0 ? _magicAttackTicksOverride : MagicAttackTicks);
                    }
                    break;

                case AttackPhase.MagicAttack:
                    NPC.velocity.X *= 0.5f; // momentum bleeds off during cast follow-through
                    DoMagicTick(PhaseTimer);
                    if (--PhaseTimer <= 0)
                        EnterPhase(AttackPhase.MagicRecovery, MagicRecoveryTicks);
                    break;

                case AttackPhase.MagicRecovery:
                    if (--PhaseTimer <= 0)
                    {
                        _magicCooldown = MagicCooldownAfterUse;
                        _magicAttackTicksOverride = -1; // clear any sustained-cast override
                        EnterCasualOrIdle();
                    }
                    break;

                // ── Fire breath ───────────────────────────────────────────────
                // Telegraph: charge the mouth ember (DoBreathWindup).  Grounded → plant; airborne →
                // keep hovering (flight controller owns velocity).  Stream: DoBreathTick spawns the
                // breath each tick; airborne it strafes to sweep the fire across the player.
                case AttackPhase.BreathTelegraph:
                {
                    bool airborneBreath = _flight != null && _flight.IsAirborne;
                    if (!airborneBreath)
                        SlowDown();
                    int faceB = target.Center.X < NPC.Center.X ? -1 : 1;
                    NPC.direction = faceB; NPC.spriteDirection = faceB;
                    DoBreathWindup(BreathTelegraphTicks - PhaseTimer);
                    CheckAndFireFlash(BreathTelegraphFlashColor);
                    if (--PhaseTimer <= 0)
                    {
                        OnBreathStart();
                        if (airborneBreath && _flight != null)
                        {
                            // Sweep: strafe to the far side of the player so the stream rakes across.
                            int sweepDir = NPC.Center.X < target.Center.X ? 1 : -1;
                            _flight.RequestStrafe(target.Center + new Vector2(sweepDir * 420f, 0f));
                        }
                        EnterPhase(AttackPhase.Breathing, BreathDurationTicks);
                    }
                    break;
                }

                case AttackPhase.Breathing:
                {
                    bool airborneBreath = _flight != null && _flight.IsAirborne;
                    if (!airborneBreath)
                    {
                        NPC.velocity.X *= 0.85f;
                        int faceB = target.Center.X < NPC.Center.X ? -1 : 1;
                        NPC.direction = faceB; NPC.spriteDirection = faceB;
                    }
                    DoBreathTick(PhaseTimer);
                    if (--PhaseTimer <= 0)
                        EnterPhase(AttackPhase.BreathRecovery, BreathRecoveryTicks);
                    break;
                }

                case AttackPhase.BreathRecovery:
                    if (_flight == null || !_flight.IsAirborne)
                        SlowDown();
                    if (--PhaseTimer <= 0)
                    {
                        _breathCooldown = BreathCooldownAfterUse;
                        EnterCasualOrIdle();
                    }
                    break;

                // ── Cursed Knives ─────────────────────────────────────────────
                // Telegraph (hold knife) → 1–3 throw volleys with inter-volley pauses → recovery.
                case AttackPhase.KnivesTelegraph:
                {
                    bool airK = _flight != null && _flight.IsAirborne;
                    if (!airK) SlowDown();
                    int faceK = target.Center.X < NPC.Center.X ? -1 : 1;
                    NPC.direction = faceK; NPC.spriteDirection = faceK;
                    SetDisplayWeapon(CursedKnivesWeaponItemType, swing: false);
                    CheckAndFireFlash(CursedKnivesTelegraphFlashColor);
                    if (--PhaseTimer <= 0)
                    {
                        SetDisplayWeapon(CursedKnivesWeaponItemType, swing: true);
                        DoCursedKnivesThrow();
                        _cursedKnivesVolleysLeft--;
                        EnterPhase(AttackPhase.KnivesThrow, CursedKnivesThrowTicks);
                    }
                    break;
                }

                case AttackPhase.KnivesThrow:
                {
                    bool airK = _flight != null && _flight.IsAirborne;
                    if (!airK) NPC.velocity.X *= 0.8f;
                    if (--PhaseTimer <= 0)
                    {
                        if (_cursedKnivesVolleysLeft > 0)
                            EnterPhase(AttackPhase.KnivesThrowPause, Math.Max(1, _cursedKnivesGap));
                        else
                            EnterPhase(AttackPhase.KnivesRecovery, CursedKnivesRecoveryTicks);
                    }
                    break;
                }

                case AttackPhase.KnivesThrowPause:
                {
                    bool airK = _flight != null && _flight.IsAirborne;
                    if (!airK) SlowDown();
                    int faceK = target.Center.X < NPC.Center.X ? -1 : 1;
                    NPC.direction = faceK; NPC.spriteDirection = faceK;
                    if (--PhaseTimer <= 0)
                    {
                        SetDisplayWeapon(CursedKnivesWeaponItemType, swing: true);
                        DoCursedKnivesThrow();
                        _cursedKnivesVolleysLeft--;
                        EnterPhase(AttackPhase.KnivesThrow, CursedKnivesThrowTicks);
                    }
                    break;
                }

                case AttackPhase.KnivesRecovery:
                    if (_flight == null || !_flight.IsAirborne)
                        SlowDown();
                    if (--PhaseTimer <= 0)
                    {
                        _cursedKnivesCooldown = CursedKnivesCooldownAfterUse;
                        EnterCasualOrIdle();
                    }
                    break;

                // ── Piercing Dash ──────────────────────────────────────────────
                // Telegraph holds a fixed anchor position (weapon out toward the player) with a small
                // random jitter each tick for the "sprite vibrates" tell, then launches into the dash.
                case AttackPhase.PierceTelegraph:
                {
                    int faceP = target.Center.X < NPC.Center.X ? -1 : 1;
                    NPC.direction = faceP; NPC.spriteDirection = faceP;
                    _pierceDir = faceP;
                    if (PhaseTimer == PierceTelegraphTicks)
                    {
                        _pierceAnchorPos = NPC.position;
                        NPC.velocity.X = 0f;
                    }
                    NPC.position = _pierceAnchorPos + Main.rand.NextVector2Circular(1.5f, 1.5f);
                    SetDisplayWeapon(MeleeWeaponItemType, swing: false);
                    DoPierceWindup(PierceTelegraphTicks - PhaseTimer);
                    if (--PhaseTimer <= 0)
                    {
                        NPC.position = _pierceAnchorPos;
                        NPC.velocity.X = _pierceDir * PierceDashSpeed;
                        SetDisplayWeapon(MeleeWeaponItemType, swing: true);
                        EnterPhase(AttackPhase.PierceDash, PierceDashTicks);
                    }
                    break;
                }

                // Active lunge: fresh reach check every tick (rather than a spawned hitbox) so a stab-variant
                // connect can immediately break out into PierceStabHold the instant it touches the target.
                // A plain (non-stab) hit just marks _pierceHitConnected and keeps carrying through.
                case AttackPhase.PierceDash:
                {
                    NPC.velocity.X = _pierceDir * PierceDashSpeed;
                    DoPierceDashTick();

                    if (!_pierceHitConnected)
                    {
                        Rectangle reach = NPC.Hitbox;
                        reach.Inflate(24, 12);
                        if (reach.Intersects(target.Hitbox))
                        {
                            _pierceHitConnected = true;
                            _pierceTarget = target;
                            OnPierceContact(target, _pierceIsStab);

                            if (_pierceIsStab)
                            {
                                NPC.velocity.X = 0f;
                                EnterPhase(AttackPhase.PierceStabHold, PierceStabRaiseTicks);
                                break;
                            }
                        }
                    }

                    if (--PhaseTimer <= 0)
                    {
                        NPC.velocity.X *= 0.4f;
                        if (CanEchoStep) TryArmEchoStep();
                        EnterPhase(AttackPhase.PierceRecovery, PierceRecoveryTicks);
                    }
                    break;
                }

                // Stab variant only: target is held impaled while the sword arm raises 0→90°.
                case AttackPhase.PierceStabHold:
                {
                    NPC.velocity.X = 0f;
                    float raiseProgress = 1f - PhaseTimer / (float)PierceStabRaiseTicks;
                    if (_pierceTarget != null && _pierceTarget.active)
                        DoPierceStabHoldTick(_pierceTarget, raiseProgress);
                    if (--PhaseTimer <= 0)
                        EnterPhase(AttackPhase.PierceStabFlick, PierceStabFlickTicks);
                    break;
                }

                // Stab variant only: rotate back down and release/launch the target (OnPierceFlick fires
                // exactly once, on the phase's first tick).
                case AttackPhase.PierceStabFlick:
                {
                    if (PhaseTimer == PierceStabFlickTicks && _pierceTarget != null && _pierceTarget.active)
                        OnPierceFlick(_pierceTarget);
                    if (--PhaseTimer <= 0)
                    {
                        _pierceTarget = null;
                        EnterPhase(AttackPhase.PierceRecovery, PierceRecoveryTicks);
                    }
                    break;
                }

                // Shared recovery after either pierce variant ends — can walk, can't attack yet.
                case AttackPhase.PierceRecovery:
                    SlowDown();
                    if (--PhaseTimer <= 0)
                    {
                        _pierceCooldown = PierceCooldownAfterUse;
                        _pierceTarget = null;
                        EnterCasualOrIdle();
                    }
                    break;

                // ── Jumping Downward Slash ─────────────────────────────────────
                // Backward roll WITH real i-frames (DodgeTimer) — a genuine dodgeroll, not a cosmetic hop.
                case AttackPhase.JumpSlashDodgeback:
                {
                    if (PhaseTimer == JumpSlashDodgebackTicks)
                    {
                        int faceJ = target.Center.X < NPC.Center.X ? -1 : 1;
                        NPC.direction = faceJ; NPC.spriteDirection = faceJ;
                        _jumpSlashDir = faceJ;
                        NPC.GetGlobalNPC<tsorcRevampGlobalNPC>().DodgeTimer = JumpSlashDodgebackTicks + 5;
                    }
                    NPC.velocity.X = -_jumpSlashDir * JumpSlashDodgebackSpeed; // away from the player
                    DoJumpSlashDodgebackTick();
                    if (--PhaseTimer <= 0)
                    {
                        EnterPhase(AttackPhase.JumpSlashRise, JumpSlashRiseTicks);
                    }
                    break;
                }

                // Launch toward the player holding the cocked pose; breaks into the swipe once close
                // or on landing (same leap-physics shape as the ComboMotion.LeapSlam launch).
                case AttackPhase.JumpSlashRise:
                {
                    if (PhaseTimer == JumpSlashRiseTicks)
                    {
                        int faceJ = target.Center.X < NPC.Center.X ? -1 : 1;
                        NPC.direction = faceJ; NPC.spriteDirection = faceJ; _jumpSlashDir = faceJ;
                        float dx = Math.Abs(target.Center.X - NPC.Center.X);
                        float airtime = 2f * JumpSlashLaunchUpSpeed / 0.3f;
                        float vx = MathHelper.Clamp(dx / airtime, 2f, JumpSlashLaunchForwardSpeed);
                        NPC.velocity = new Vector2(_jumpSlashDir * vx, -JumpSlashLaunchUpSpeed);
                        NPC.netUpdate = true;
                    }
                    else
                    {
                        NPC.velocity.X = _jumpSlashDir * JumpSlashLaunchForwardSpeed;
                    }

                    DoJumpSlashRiseTick();

                    bool nearPlayer = NPC.Distance(target.Center) <= JumpSlashTriggerRange;
                    bool landed = PhaseTimer < JumpSlashRiseTicks && NPC.velocity.Y == 0f;
                    if (nearPlayer || landed || --PhaseTimer <= 0)
                    {
                        DoJumpSlashAttack();
                        EnterPhase(AttackPhase.JumpSlashAttack, JumpSlashAttackTicks);
                    }
                    break;
                }

                case AttackPhase.JumpSlashAttack:
                    if (--PhaseTimer <= 0)
                    {
                        // Punish the dodge: a clean whiff chains straight into Abyss Slash instead
                        // of a free recovery window, if that's off cooldown and in range.
                        _jumpSlashCooldown = JumpSlashCooldownAfterUse;
                        if (!TryDodgePunishChain(dist))
                            EnterPhase(AttackPhase.JumpSlashRecovery, JumpSlashRecoveryTicks);
                    }
                    break;

                case AttackPhase.JumpSlashRecovery:
                    SlowDown();
                    if (--PhaseTimer <= 0)
                    {
                        _jumpSlashCooldown = JumpSlashCooldownAfterUse;
                        EnterCasualOrIdle();
                    }
                    break;

                // ── Forward Flip Slash ─────────────────────────────────────────
                // Airborne, genuinely i-framed (DodgeTimer refreshed every tick - a real dodgeroll,
                // not just hyper-armor), spinning continuously with a one-shot contact check.
                case AttackPhase.FlipSlashRise:
                {
                    if (PhaseTimer == FlipSlashRiseMaxTicks)
                    {
                        int faceF = target.Center.X < NPC.Center.X ? -1 : 1;
                        NPC.direction = faceF; NPC.spriteDirection = faceF; _flipSlashDir = faceF;
                        float dx = Math.Abs(target.Center.X - NPC.Center.X);
                        float airtime = 2f * FlipSlashLaunchUpSpeed / 0.3f;
                        float vx = MathHelper.Clamp(dx / airtime, 2f, FlipSlashLaunchForwardSpeed);
                        NPC.velocity = new Vector2(_flipSlashDir * vx, -FlipSlashLaunchUpSpeed);
                        NPC.netUpdate = true;
                    }
                    else
                    {
                        NPC.velocity.X = _flipSlashDir * FlipSlashLaunchForwardSpeed;
                    }

                    // Refreshed every tick so the i-frame window always covers the whole flip
                    // regardless of exact air time.
                    NPC.GetGlobalNPC<tsorcRevampGlobalNPC>().DodgeTimer = 5;

                    DoFlipSlashRiseTick();

                    if (!_flipHitConnected)
                    {
                        Rectangle reach = NPC.Hitbox;
                        reach.Inflate(20, 10);
                        if (reach.Intersects(target.Hitbox))
                        {
                            _flipHitConnected = true;
                            DoFlipSlashHit();
                        }
                    }

                    bool landedFlip = PhaseTimer < FlipSlashRiseMaxTicks && NPC.velocity.Y == 0f;
                    if (landedFlip || --PhaseTimer <= 0)
                    {
                        OnFlipSlashLand();
                        if (CanEchoStep) TryArmEchoStep();
                        EnterPhase(AttackPhase.FlipSlashLand, FlipSlashLandHoldTicks);
                    }
                    break;
                }

                // Sword snaps into the slam pose (handled in the rotation table below) and is held
                // there for the full duration - no easing, a deliberate static pose.
                case AttackPhase.FlipSlashLand:
                    NPC.velocity.X *= 0.5f;
                    if (--PhaseTimer <= 0)
                    {
                        _flipSlashCooldown = FlipSlashCooldownAfterUse;
                        EnterCasualOrIdle();
                    }
                    break;

                // ── Abyss Slash ─────────────────────────────────────────────────
                // Deliberately does not touch NPC.velocity.X - RunMovementAI's normal pursuit
                // (already applied earlier this tick, before InvaderAttackAI runs) carries through
                // untouched, so the invader keeps walking during the whole wind-up.
                case AttackPhase.AbyssSlashTelegraph:
                    if (--PhaseTimer <= 0)
                    {
                        _abyssSlashIndex = 0;
                        DoAbyssSlashFire(0);
                        EnterPhase(AttackPhase.AbyssSlashSwipe, AbyssSlashSwipeTicks);
                    }
                    break;

                case AttackPhase.AbyssSlashSwipe:
                    if (--PhaseTimer <= 0)
                    {
                        int delay = NextAbyssSlashDelay(_abyssSlashIndex);
                        if (delay < 0)
                        {
                            // Punish the dodge: if the whole swipe combo whiffed, chain straight
                            // into Jumping Downward Slash instead of a free recovery window.
                            _abyssSlashCooldown = AbyssSlashCooldownAfterUse;
                            if (!TryDodgePunishChain(dist))
                                EnterPhase(AttackPhase.AbyssSlashRecovery, AbyssSlashRecoveryTicks);
                        }
                        else
                            EnterPhase(AttackPhase.AbyssSlashPause, delay);
                    }
                    break;

                case AttackPhase.AbyssSlashPause:
                    if (--PhaseTimer <= 0)
                    {
                        _abyssSlashIndex++;
                        DoAbyssSlashFire(_abyssSlashIndex);
                        EnterPhase(AttackPhase.AbyssSlashSwipe, AbyssSlashSwipeTicks);
                    }
                    break;

                case AttackPhase.AbyssSlashRecovery:
                    SlowDown();
                    if (--PhaseTimer <= 0)
                    {
                        _abyssSlashCooldown = AbyssSlashCooldownAfterUse;
                        EnterCasualOrIdle();
                    }
                    break;

                // ── Abyss Tendril Grab ──────────────────────────────────────────
                case AttackPhase.TendrilTelegraph:
                    SlowDown();
                    DoTendrilTelegraphTick(TendrilTelegraphTicks - PhaseTimer);
                    if (--PhaseTimer <= 0)
                    {
                        DoTendrilLaunch();
                        EnterPhase(AttackPhase.TendrilReach, TendrilReachTicks);
                    }
                    break;

                // The tendril projectile is fully self-contained (fly/yank/retract); this phase
                // just holds the arm-out pose for as long as that sequence should take.
                case AttackPhase.TendrilReach:
                    SlowDown();
                    DoTendrilReachTick();
                    if (--PhaseTimer <= 0)
                        EnterPhase(AttackPhase.TendrilSwingTelegraph, TendrilSwingArcTicks + TendrilSwingHoldTicks);
                    break;

                case AttackPhase.TendrilSwingTelegraph:
                    SlowDown();
                    if (--PhaseTimer <= 0)
                    {
                        DoTendrilSwing();
                        EnterPhase(AttackPhase.TendrilSwing, TendrilSwingTicks);
                    }
                    break;

                case AttackPhase.TendrilSwing:
                    if (--PhaseTimer <= 0)
                    {
                        // If the finishing swing whiffed too, a delayed echo picks up the same
                        // swing shape a beat later - a second, separately-timed threat instead of
                        // a free breather after the grab sequence.
                        if (!_lastAttackHitConnected && CanEchoStep)
                            TryArmEchoStep();
                        EnterPhase(AttackPhase.TendrilRecovery, TendrilRecoveryTicks);
                    }
                    break;

                case AttackPhase.TendrilRecovery:
                    SlowDown();
                    if (--PhaseTimer <= 0)
                    {
                        _tendrilCooldown = TendrilCooldownAfterUse;
                        EnterCasualOrIdle();
                    }
                    break;

                // ── Charge-up Nova ──────────────────────────────────────────────
                case AttackPhase.NovaCharge:
                {
                    SlowDown();
                    int elapsed = NovaChargeTicks - PhaseTimer;
                    DoNovaChargeTick(elapsed, NovaChargeTicks);
                    if (--PhaseTimer <= 0)
                    {
                        DoNovaBlast();
                        EnterPhase(AttackPhase.NovaBlast, NovaBlastHoldTicks);
                    }
                    break;
                }

                case AttackPhase.NovaBlast:
                    if (--PhaseTimer <= 0)
                        EnterPhase(AttackPhase.NovaRecovery, NovaRecoveryTicks);
                    break;

                case AttackPhase.NovaRecovery:
                    SlowDown();
                    if (--PhaseTimer <= 0)
                        EnterCasualOrIdle();
                    break;

                // ── Abyss Shard ───────────────────────────────────────────────
                case AttackPhase.AbyssShardTelegraph:
                    SlowDown();
                    if (--PhaseTimer <= 0)
                    {
                        _abyssShardIndex = 0;
                        DoAbyssShardFire(0);
                        EnterPhase(AttackPhase.AbyssShardFire, AbyssShardFireTicks);
                    }
                    break;

                case AttackPhase.AbyssShardFire:
                    SlowDown();
                    if (--PhaseTimer <= 0)
                    {
                        int delay = NextAbyssShardDelay(_abyssShardIndex);
                        if (delay < 0)
                        {
                            // Deliberately no recovery phase - the sequence just ends and control
                            // returns immediately, letting a different attack follow right away.
                            _abyssShardCooldown = AbyssShardCooldownAfterUse;
                            EnterCasualOrIdle();
                        }
                        else
                        {
                            EnterPhase(AttackPhase.AbyssShardPause, delay);
                        }
                    }
                    break;

                case AttackPhase.AbyssShardPause:
                    SlowDown();
                    if (--PhaseTimer <= 0)
                    {
                        _abyssShardIndex++;
                        DoAbyssShardFire(_abyssShardIndex);
                        EnterPhase(AttackPhase.AbyssShardFire, AbyssShardFireTicks);
                    }
                    break;

                // ── Homing Volley ─────────────────────────────────────────────
                // Backward roll WITH real i-frames (DodgeTimer) - same technique as Jumping
                // Downward Slash's wind-up, but leads into an overhead chop instead of a jump.
                case AttackPhase.HomingVolleyDodgeback:
                {
                    if (PhaseTimer == HomingVolleyDodgebackTicks)
                    {
                        int faceHv = target.Center.X < NPC.Center.X ? -1 : 1;
                        NPC.direction = faceHv; NPC.spriteDirection = faceHv;
                        _homingVolleyDir = faceHv;
                        NPC.GetGlobalNPC<tsorcRevampGlobalNPC>().DodgeTimer = HomingVolleyDodgebackTicks + 5;
                    }
                    NPC.velocity.X = -_homingVolleyDir * HomingVolleyDodgebackSpeed;
                    if (--PhaseTimer <= 0)
                    {
                        _homingVolleyFired = false;
                        EnterPhase(AttackPhase.HomingVolleySwingTelegraph, HomingVolleySwingTelegraphTicks);
                    }
                    break;
                }

                case AttackPhase.HomingVolleySwingTelegraph:
                    SlowDown();
                    if (--PhaseTimer <= 0)
                    {
                        EnterPhase(AttackPhase.HomingVolleySwing, HomingVolleySwingTicks);
                    }
                    break;

                case AttackPhase.HomingVolleySwing:
                {
                    SlowDown();
                    int elapsedSwing = HomingVolleySwingTicks - PhaseTimer;
                    DoHomingVolleySwingTick(elapsedSwing, HomingVolleySwingTicks);
                    if (!_homingVolleyFired && elapsedSwing >= HomingVolleySwingTicks * HomingVolleyFireProgress)
                    {
                        _homingVolleyFired = true;
                        DoHomingVolleyFire();
                    }
                    if (--PhaseTimer <= 0)
                    {
                        _homingVolleyCooldown = HomingVolleyCooldownAfterUse;
                        EnterPhase(AttackPhase.HomingVolleyRecovery, HomingVolleyRecoveryTicks);
                    }
                    break;
                }

                case AttackPhase.HomingVolleyRecovery:
                    SlowDown();
                    if (--PhaseTimer <= 0)
                        EnterCasualOrIdle();
                    break;

                // ── Sword Launch Reposition (shared) ──────────────────────────
                // Mostly-horizontal backward hop, facing the player - same leap-physics shape as
                // Jumping Downward Slash's dodgeback, but no i-frames (a spacing hop, not a dodge).
                case AttackPhase.SwordLaunchReposition:
                {
                    if (PhaseTimer == SwordLaunchRepositionTicks)
                    {
                        int faceSl = target.Center.X < NPC.Center.X ? -1 : 1;
                        NPC.direction = faceSl; NPC.spriteDirection = faceSl;
                        _swordLaunchDir = faceSl;
                        NPC.velocity = new Vector2(-faceSl * SwordLaunchRepositionBackSpeed, -SwordLaunchRepositionUpSpeed);
                        NPC.netUpdate = true;
                    }
                    else
                    {
                        NPC.velocity.X = -_swordLaunchDir * SwordLaunchRepositionBackSpeed;
                    }

                    bool landedSl = PhaseTimer < SwordLaunchRepositionTicks && NPC.velocity.Y == 0f;
                    if (landedSl || --PhaseTimer <= 0)
                    {
                        EnterPhase(_swordLaunchNextPhase, _swordLaunchNextTicks);
                    }
                    break;
                }

                // ── Boomerang Crescent ─────────────────────────────────────────
                case AttackPhase.BoomerangSwingTelegraph:
                    SlowDown();
                    if (--PhaseTimer <= 0)
                    {
                        _boomerangFired = false;
                        EnterPhase(AttackPhase.BoomerangSwing, BoomerangSwingTicks);
                    }
                    break;

                case AttackPhase.BoomerangSwing:
                {
                    SlowDown();
                    int elapsedBoom = BoomerangSwingTicks - PhaseTimer;
                    DoBoomerangSwingTick(elapsedBoom, BoomerangSwingTicks);
                    if (!_boomerangFired && elapsedBoom >= BoomerangSwingTicks * BoomerangFireProgress)
                    {
                        _boomerangFired = true;
                        DoBoomerangFire();
                    }
                    if (--PhaseTimer <= 0)
                    {
                        _boomerangCooldown = BoomerangCooldownAfterUse;
                        EnterPhase(AttackPhase.BoomerangRecovery, BoomerangRecoveryTicks);
                    }
                    break;
                }

                case AttackPhase.BoomerangRecovery:
                    SlowDown();
                    if (--PhaseTimer <= 0)
                        EnterCasualOrIdle();
                    break;

                // ── Spiral Fan ─────────────────────────────────────────────────
                case AttackPhase.SpiralFanSwingTelegraph:
                    SlowDown();
                    if (--PhaseTimer <= 0)
                    {
                        EnterPhase(AttackPhase.SpiralFanSwing, SpiralFanSwingTicks);
                    }
                    break;

                case AttackPhase.SpiralFanSwing:
                {
                    SlowDown();
                    DoSpiralFanSwingTick(SpiralFanSwingTicks - PhaseTimer, SpiralFanSwingTicks);
                    if (--PhaseTimer <= 0)
                    {
                        _spiralFanIndex = 0;
                        DoSpiralFanFire(0);
                        EnterPhase(AttackPhase.SpiralFanBurst, SpiralFanFireTicks);
                    }
                    break;
                }

                case AttackPhase.SpiralFanBurst:
                    SlowDown();
                    if (--PhaseTimer <= 0)
                    {
                        int delaySf = NextSpiralFanDelay(_spiralFanIndex);
                        if (delaySf < 0)
                        {
                            _spiralFanCooldown = SpiralFanCooldownAfterUse;
                            EnterPhase(AttackPhase.SpiralFanRecovery, SpiralFanRecoveryTicks);
                        }
                        else
                        {
                            EnterPhase(AttackPhase.SpiralFanPause, delaySf);
                        }
                    }
                    break;

                case AttackPhase.SpiralFanPause:
                    SlowDown();
                    if (--PhaseTimer <= 0)
                    {
                        _spiralFanIndex++;
                        DoSpiralFanFire(_spiralFanIndex);
                        EnterPhase(AttackPhase.SpiralFanBurst, SpiralFanFireTicks);
                    }
                    break;

                case AttackPhase.SpiralFanRecovery:
                    SlowDown();
                    if (--PhaseTimer <= 0 && !TryContinueSpiralFanChain())
                        EnterCasualOrIdle();
                    break;

                // ── Fire Volley chain: backward hop, facing the player ────────
                case AttackPhase.FireVolleyBackLeap:
                {
                    if (PhaseTimer == FireVolleyBackLeapTicks)
                    {
                        int faceFv = target.Center.X < NPC.Center.X ? -1 : 1;
                        NPC.direction = faceFv; NPC.spriteDirection = faceFv;
                        _fireVolleyDir = faceFv;
                        NPC.velocity = new Vector2(-faceFv * FireVolleyBackLeapSpeed, -FireVolleyBackLeapUpSpeed);
                        NPC.netUpdate = true;
                    }
                    else
                    {
                        NPC.velocity.X = -_fireVolleyDir * FireVolleyBackLeapSpeed;
                    }

                    bool landedFv = PhaseTimer < FireVolleyBackLeapTicks && NPC.velocity.Y == 0f;
                    if (landedFv || --PhaseTimer <= 0)
                        OnFireVolleyRepositionLanded();
                    break;
                }

                // ── Fire Volley chain: leaping dodge-roll through the player ──
                case AttackPhase.FireVolleyDodgeThrough:
                {
                    var globalNpcFv = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
                    if (PhaseTimer == FireVolleyDodgeThroughTimeoutTicks)
                    {
                        int faceFv = target.Center.X < NPC.Center.X ? -1 : 1;
                        NPC.direction = faceFv; NPC.spriteDirection = faceFv;
                        NPC.velocity.Y = -FireVolleyDodgeThroughUpSpeed;
                        tsorcRevampAIs.ArmQuickStep(NPC, globalNpcFv, allowForward: true);
                        NPC.netUpdate = true;
                    }
                    tsorcRevampAIs.TickQuickStep(NPC, globalNpcFv);
                    bool resolvedFv = globalNpcFv.QuickStepTimer <= 0 && globalNpcFv.QuickStepRecoveryTimer <= 0;
                    if (resolvedFv || --PhaseTimer <= 0)
                        OnFireVolleyRepositionLanded();
                    break;
                }

                // ── Fire Volley chain: big arc jump over the player ───────────
                case AttackPhase.FireVolleyArcJump:
                {
                    NPC.velocity.X = _fireVolleyArcVx;
                    if (!_fireVolleyArcFired && NPC.velocity.Y >= 0f)
                    {
                        _fireVolleyArcFired = true;
                        DoFireVolleyArcFire();
                    }
                    bool landedArc = _fireVolleyArcFired && NPC.velocity.Y == 0f && PhaseTimer < 100;
                    if (landedArc || --PhaseTimer <= 0)
                        OnFireVolleyArcJumpLanded();
                    break;
                }

                // ── Closing distance ──────────────────────────────────────────
                // A melee combo was chosen out of reach: the mover sprints in (boosted speedMult in AI);
                // start the combo once inside MeleeEngageRange, or give up on timeout / if the player flies
                // out of combo range (re-decide → may go ranged).
                case AttackPhase.ClosingDistance:
                {
                    int faceC = target.Center.X < NPC.Center.X ? -1 : 1;
                    NPC.direction = faceC; NPC.spriteDirection = faceC;
                    bool inReach = dist <= MeleeEngageRange && NPC.velocity.Y == 0f
                                   && NPC.Center.Y - target.Center.Y < 48f;
                    if (inReach)
                    {
                        if (!TryStartMeleeCombo(dist))
                            EnterPhase(AttackPhase.Idle, 0);
                        break;
                    }
                    if (dist > ComboMaxStartRange + 80f || --PhaseTimer <= 0)
                        EnterPhase(AttackPhase.Idle, 0);
                    break;
                }

                // ── Casual stroll ─────────────────────────────────────────────
                // Slow walk toward player with no attacks — gives post-attack breathing room
                // and variation before the next engagement.
                case AttackPhase.CasualStroll:
                    if (--PhaseTimer <= 0)
                        EnterPhase(AttackPhase.Idle, 0);
                    break;

                // ── Flee to heal ──────────────────────────────────────────────
                // Sprint away until at safe distance (or timer expires), then drink.
                case AttackPhase.FleeToHeal:
                    // Velocity is already set in AI() above; just check distance / timer.
                    if (dist >= FleeToHealDistance || --PhaseTimer <= 0)
                    {
                        // Apply the heal NOW (at the moment of drinking).
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            int healAmount = (int)(NPC.lifeMax * EstusHealFraction);
                            NPC.life = Math.Min(NPC.lifeMax, NPC.life + healAmount);
                            NPC.HealEffect(healAmount);
                        }
                        _estusCharges--;
                        _healCooldown  = HealCooldownTicks;
                        _recentDamage  = 0f;

                        // Mark whichever threshold(s) we're healing past so they don't re-fire
                        // immediately.  They'll reset once HP climbs back above the level.
                        float hp = (float)NPC.life / NPC.lifeMax;
                        if (hp > 0.50f) _halfHpHealed    = true;
                        if (hp > 0.25f) _quarterHpHealed = true;

                        EnterPhase(AttackPhase.Healing, HealAnimationTicks);
                    }
                    break;

                // ── Healing ───────────────────────────────────────────────────
                // Stand still and play the drinking animation for HealAnimationTicks.
                case AttackPhase.Healing:
                    NPC.velocity.X *= 0.6f; // brake to a halt
                    if (--PhaseTimer <= 0)
                        EnterCasualOrIdle();
                    break;

                // ── Melee combo: telegraph (step 0) ───────────────────────────
                case AttackPhase.MeleeComboTelegraph:
                    LockComboDirection();
                    if (SlowDownBeforeMelee) SlowDown();
                    SetDisplayWeapon(MeleeWeaponItemType, swing: false);
                    CheckAndFireFlash(_activeMeleeCombo.InitialFlashColor);
                    if (--PhaseTimer <= 0)
                    {
                        var step0 = _activeMeleeCombo.Steps[_meleeComboStepIndex];
                        SetDisplayWeapon(MeleeWeaponItemType, swing: true);
                        if (IsSwingingMotion(step0.Motion))
                            PlayMeleeSwingSound();
                        // Deferred / no-hit motions don't strike on entry: LeapSlam launches now and
                        // connects on landing; ChargeChop runs (next step chops); Feint just holds
                        // the bait pose (next step is the delayed chop).
                        _bladeArmed = false;
                        if (step0.Motion == ComboMotion.LeapSlam || step0.Motion == ComboMotion.LeapThrust)
                            BeginLeapAttack();
                        else if (step0.Motion == ComboMotion.ChargeChop || step0.Motion == ComboMotion.Feint)
                        { /* no hit this step — movement / bait only */ }
                        else
                            DoComboMeleeHit(step0);
                        EnterPhase(AttackPhase.MeleeComboAttack, GetMeleeSwingTicks(step0.AttackTicks));
                    }
                    break;

                // ── Melee combo: active hitbox frame ──────────────────────────
                case AttackPhase.MeleeComboAttack:
                {
                    LockComboDirection();
                    var step = _activeMeleeCombo.Steps[_meleeComboStepIndex];
                    bool endStep;

                    if (step.Motion == ComboMotion.LeapSlam || step.Motion == ComboMotion.LeapThrust)
                    {
                        // Hold the launch velocity (SF4 ran earlier this tick and would otherwise
                        // steer X); gravity provides the downward arc.  The hit connects when we
                        // land, or when the airtime cap (AttackTicks) expires.
                        NPC.velocity.X = _comboLeapVx;
                        // velocity.Y only returns to exactly 0 via a vertical collision (landing).
                        // Require a few ticks of airtime first so a launch that can't clear a low
                        // ceiling doesn't read as an instant landing.
                        bool landed = _comboLeapLaunched && NPC.velocity.Y == 0f && PhaseTimer < 86;
                        endStep = (--PhaseTimer <= 0) || landed;
                        if (endStep)
                        {
                            // Deferred single-hit: arm the blade check, then resolve it the same
                            // tick against the weapon's actual landing pose — a slam/thrust still
                            // only connects if the sprite is really overlapping the target here,
                            // it just doesn't need to be checked every tick like a sweeping arc.
                            DoComboMeleeHit(step);
                            TickBladeHit();
                            _comboLeapLaunched = false;
                        }
                    }
                    else if (step.Motion == ComboMotion.ChargeChop)
                    {
                        // Run straight at the player, re-facing each tick.  No hit here — the
                        // following OverheadArc step is the chop.  Ends on reach OR the timeout
                        // (PhaseTimer = AttackTicks) so it always completes and resets to idle.
                        int faceDir = target.Center.X < NPC.Center.X ? -1 : 1;
                        NPC.direction = faceDir; NPC.spriteDirection = faceDir; _comboLockedDir = faceDir;
                        NPC.velocity.X = faceDir * (TopSpeed * ChargeAttackSpeedMult);
                        bool inReach = NPC.Distance(target.Center) <= MeleeRange * 0.9f;
                        endStep = (--PhaseTimer <= 0) || inReach;
                    }
                    else if (step.Motion == ComboMotion.Feint)
                    {
                        // Hold the raised bait pose without swinging; the real chop is the next step.
                        NPC.velocity.X *= 0.6f;
                        endStep = (--PhaseTimer <= 0);
                    }
                    else
                    {
                        TickBladeHit();
                        if (step.ForwardPushMult > 0f)
                            NPC.velocity.X = _comboLockedDir * (TopSpeed * step.ForwardPushMult);
                        else
                            NPC.velocity.X *= 0.65f;
                        endStep = (--PhaseTimer <= 0);
                    }

                    if (endStep)
                    {
                        int nextIdx = _meleeComboStepIndex + 1;
                        if (nextIdx < _activeMeleeCombo.Steps.Length)
                            EnterPhase(AttackPhase.MeleeComboPause, Math.Max(1, step.PostStepPause));
                        else
                            EnterPhase(AttackPhase.MeleeComboRecovery, MeleeRecoveryTicks);
                    }
                    break;
                }

                // ── Melee combo: inter-step pause ─────────────────────────────
                // Direction UNLOCKED during the pause: if the player dodgerolled through,
                // the invader can re-face them so the next step swings the correct way.
                // The next step's lock direction is captured just before its attack fires.
                case AttackPhase.MeleeComboPause:
                    SlowDown();
                    // Re-face the player during the pause window.
                    {
                        int faceDir = target.Center.X < NPC.Center.X ? -1 : 1;
                        NPC.direction       = faceDir;
                        NPC.spriteDirection = faceDir;
                    }
                    if (--PhaseTimer <= 0)
                    {
                        _meleeComboStepIndex++;
                        // Recapture the lock direction for the upcoming step.
                        _comboLockedDir = NPC.direction;
                        var nextStep = _activeMeleeCombo.Steps[_meleeComboStepIndex];
                        SetDisplayWeapon(MeleeWeaponItemType, swing: true);
                        if (IsSwingingMotion(nextStep.Motion))
                            PlayMeleeSwingSound();
                        _bladeArmed = false;
                        DoComboMeleeHit(nextStep);
                        _weaponAnim = _weaponAnimMax;
                        EnterPhase(AttackPhase.MeleeComboAttack, GetMeleeSwingTicks(nextStep.AttackTicks));
                    }
                    break;

                // ── Melee combo: final recovery ───────────────────────────────
                // Direction unlocked during recovery — invader can re-orient toward player.
                case AttackPhase.MeleeComboRecovery:
                    if (--PhaseTimer <= 0)
                    {
                        if (_meleeComboCooldowns != null
                            && _activeMeleeComboIndex >= 0
                            && _activeMeleeComboIndex < _meleeComboCooldowns.Length)
                        {
                            _meleeComboCooldowns[_activeMeleeComboIndex] = _activeMeleeCombo.CooldownAfterUse;
                        }
                        _activeMeleeComboIndex = -1;
                        EnterCasualOrIdle();
                    }
                    break;
            }
        }

        /// <summary>
        /// Force direction to the value captured at combo start.  Called every tick
        /// across all four MeleeCombo* phases so a player who dodgerolls through ends up
        /// behind the swing arc — FighterAI's natural facing-flip is overridden.
        /// </summary>
        private void LockComboDirection()
        {
            NPC.direction       = _comboLockedDir;
            NPC.spriteDirection = _comboLockedDir;
            _directionHoldTicks = Math.Max(_directionHoldTicks, 5);
        }

        /// <summary>
        /// Configure all ranged-burst state for the current weapon trigger.  Used by both the
        /// ground Idle path and the airborne hover-shot path so they share one canonical setup.
        ///   <paramref name="useSecondary"/>: pick secondary (crossbow) vs primary (stars/bow).
        ///   <paramref name="shotsOverride"/>: -1 = roll randomly via MaxRangedBurst; else use this exact count.
        ///   <paramref name="forceStanding"/>: bypass StandingRangedChance roll (used by aerial — controller owns velocity).
        /// </summary>
        private void SetupRangedBurst(bool useSecondary, int shotsOverride, bool forceStanding)
        {
            _usingSecondaryRanged = useSecondary;

            _activeRangedItemType       = useSecondary ? SecondaryRangedWeaponItemType : RangedWeaponItemType;
            _activeRangedStyle          = useSecondary ? SecondaryRangedAnimStyle      : RangedAnimStyle;
            _activeRangedFlashColor     = useSecondary ? SecondaryRangedFlashColor     : RangedTelegraphFlashColor;
            _activeRangedTelegraphTicks = useSecondary ? SecondaryRangedTelegraphTicks : RangedTelegraphTicks;
            _activeRangedAttackTicks    = useSecondary ? SecondaryRangedAttackTicks    : RangedAttackTicks;
            _activeRangedRecoveryTicks  = useSecondary ? SecondaryRangedRecoveryTicks  : RangedRecoveryTicks;

            if (forceStanding)
            {
                _standingShot = true;
            }
            else
            {
                int standingChance = useSecondary ? SecondaryStandingRangedChance : StandingRangedChance;
                _standingShot = Main.rand.Next(100) < standingChance;
            }

            // Shot count: explicit override (aerial = 1) takes precedence; otherwise roll.
            if (shotsOverride > 0)
            {
                _rangedShotsRemaining = shotsOverride;
            }
            else
            {
                int burstMax = useSecondary ? SecondaryMaxRangedBurst : MaxRangedBurst;
                _rangedShotsRemaining = !useSecondary && SingleRangedBurstChance > 0
                                        && Main.rand.Next(100) < SingleRangedBurstChance
                    ? 1
                    : Math.Max(1, Main.rand.Next(1, burstMax + 1));
            }

            // ── Ranged burst-pattern selection (primary or secondary) ────────────
            // When a ranged burst pattern is defined the classic _rangedShotsRemaining
            // counter is replaced by a pause-array that drives the shot sequence.
            _interShotPauses     = null;
            _interShotPauseIndex = 0;
            _activeBurstPatternIndex = -1;

            int[][] patternPool = useSecondary ? SecondaryRangedBurstPatterns : PrimaryRangedBurstPatterns;
            int[] patternChances = useSecondary ? SecondaryRangedBurstChances : PrimaryRangedBurstChances;
            int[] telegraphExtras = useSecondary ? SecondaryRangedBurstTelegraphExtras : PrimaryRangedBurstTelegraphExtras;
            Color[] flashColors = useSecondary ? SecondaryRangedBurstFlashColors : PrimaryRangedBurstFlashColors;

            if (shotsOverride < 0   // override skips patterns (aerial single-shot)
                && patternPool != null
                && patternPool.Length > 0)
            {
                int patIdx = PickPatternByChance(patternChances, patternPool.Length);
                _interShotPauses = patternPool[patIdx];
                _activeBurstPatternIndex = patIdx;

                if (telegraphExtras != null && patIdx < telegraphExtras.Length)
                    _activeRangedTelegraphTicks += telegraphExtras[patIdx];

                if (flashColors != null && patIdx < flashColors.Length)
                    _activeRangedFlashColor = flashColors[patIdx];
            }

            OnRangedBurstStarted(useSecondary); // e.g. lit-fuse cue when the weapon appears in hand
        }

        /// <summary>
        /// Lazy-init the melee combo pool and per-combo cooldown array for the active archetype.
        /// </summary>
        private void EnsureMeleeComboPool()
        {
            if (_meleeComboPool != null) return;
            _meleeComboPool = WeaponArchetypeTables.GetMeleeCombos(MeleeArchetype);
            if (_meleeComboPool != null)
                _meleeComboCooldowns = new int[_meleeComboPool.Length];
        }

        private void EnsureRangedComboPool()
        {
            if (_rangedComboPool != null) return;
            _rangedComboPool = WeaponArchetypeTables.GetRangedCombos(RangedArchetype);
            if (_rangedComboPool != null)
                _rangedComboCooldowns = new int[_rangedComboPool.Length];
        }

        /// <summary>
        /// Try to start a melee combo from the active archetype's pool.  Selection is
        /// range-aware (close/mid/far bands) and HP-aware (heavy combos weighted up as HP drops).
        /// Returns true if a combo started; false if no eligible combo (cooldowns/weights).
        /// </summary>
        private bool TryStartMeleeCombo(float dist)
        {
            EnsureMeleeComboPool();
            if (_meleeComboPool == null || _meleeComboPool.Length == 0) return false;

            // Filter pool to combos whose cooldown is ready; rebuild weights array.
            float hpFrac = (float)NPC.life / NPC.lifeMax;
            float closeMax = MeleeRange + 30f;
            float midMax   = StabRange + 60f;

            // Walk the picker but with cooldown filter: temporarily zero the BaseWeight
            // of any combo on cooldown.  We use a stack array of effective weights.
            int total = 0;
            int[] effective = new int[_meleeComboPool.Length];
            ComboRangeBand band = dist <= closeMax ? ComboRangeBand.Close
                               : dist <= midMax   ? ComboRangeBand.Mid
                               : ComboRangeBand.Far;
            float heavyMult = hpFrac <= 0.33f ? 2.5f : hpFrac <= 0.66f ? 1.5f : 1.0f;

            for (int i = 0; i < _meleeComboPool.Length; i++)
            {
                if (_meleeComboCooldowns[i] > 0) { effective[i] = 0; continue; }
                float w = _meleeComboPool[i].BaseWeight;
                w *= (_meleeComboPool[i].Preferred == band
                      || _meleeComboPool[i].Preferred == ComboRangeBand.Any) ? 2.0f : 0.4f;
                if (_meleeComboPool[i].HeavyCommit) w *= heavyMult;
                effective[i] = (int)w;
                total += effective[i];
            }
            if (total <= 0) return false;

            int roll = Main.rand.Next(total);
            int cumulative = 0;
            int chosen = -1;
            for (int i = 0; i < _meleeComboPool.Length; i++)
            {
                cumulative += effective[i];
                if (effective[i] > 0 && roll < cumulative) { chosen = i; break; }
            }
            if (chosen < 0) return false;

            _activeMeleeComboIndex = chosen;
            _activeMeleeCombo      = _meleeComboPool[chosen];
            _meleeComboStepIndex   = 0;
            _comboLockedDir        = NPC.direction;
            ArmSwingVariation(NPC.HasValidTarget ? NPC.Center.Y - Main.player[NPC.target].Center.Y : 0f);
            // Minimum 30 ticks (0.5 s) so the wind-up arc has visible time to play.
            // Lighter combos that have raw values below 22 (×1.35 → ~30) get floored here.
            int telegraphTicks = Math.Max(MinComboTelegraphTicks,
                                          (int)(_activeMeleeCombo.Steps[0].TelegraphTicks * ComboTelegraphMultiplier));
            EnterPhase(AttackPhase.MeleeComboTelegraph, telegraphTicks);
            return true;
        }

        /// <summary>
        /// Spawn the damage hitbox for the current combo step.  Default applies the step's
        /// DamageMult and ReachMult on top of the invader's MeleeRange × 0.7 base reach.
        /// Subclasses can override for per-step VFX/sounds without losing the scaling.
        /// </summary>
        protected virtual void DoComboMeleeHit(MeleeComboStep step)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient) return;
            float reach = ComboReachBase * 0.7f * step.ReachMult;
            int   dmg   = (int)(MeleeDamage * step.DamageMult);
            // Arms the tracked blade check (see TickBladeHit) instead of hitting immediately —
            // the step now only connects on the tick(s) its weapon sprite actually sweeps over
            // the target, matching TryMeleeHit's one-shot swings.
            ArmBladeHit(reach, dmg, knockback: 3f);
        }

        /// <summary>
        /// Launches a LeapSlam: face the player, drop gravity back on, and fling up-and-forward.
        /// The arc closes distance; the slam hit fires when the invader lands (handled in the
        /// MeleeComboAttack phase).
        /// </summary>
        /// <summary>Below this horizontal gap, a LeapSlam jumps high and drops straight onto the
        /// player instead of lunging forward (avoids overshooting at close range).</summary>
        protected virtual float LeapMinLungeDistance => 80f;

        private void BeginLeapAttack()
        {
            Player target = Main.player[NPC.target];
            int dir = target.Center.X < NPC.Center.X ? -1 : 1;
            _comboLockedDir = dir;
            NPC.direction = dir;
            NPC.spriteDirection = dir;
            NPC.noGravity = false;

            float dx = Math.Abs(target.Center.X - NPC.Center.X);
            if (dx < LeapMinLungeDistance)
            {
                // Close: jump high and fall onto them — minimal horizontal travel so it lands ON
                // the player instead of sailing past.
                _comboLeapVx = dir * 1.5f;
                NPC.velocity = new Vector2(_comboLeapVx, -(LeapAttackUpSpeed + 2f));
            }
            else
            {
                // Far: arc toward the player, horizontal speed sized to land near them (roughly
                // dx / airtime) and capped so it never overshoots.
                float airtime = 2f * LeapAttackUpSpeed / 0.3f; // ticks aloft ≈ 2·Vy/g
                float vx = MathHelper.Clamp(dx / airtime, 1.5f, LeapAttackForwardSpeed);
                _comboLeapVx = dir * vx;
                NPC.velocity = new Vector2(_comboLeapVx, -LeapAttackUpSpeed);
            }
            _comboLeapLaunched = true;
        }

        /// <summary>Launches a big jump arc over the player, facing them. Call once, then
        /// EnterPhase(AttackPhase.FireVolleyArcJump, someTicks); the phase's own case body fires
        /// <see cref="DoFireVolleyArcFire"/> at the apex and <see cref="OnFireVolleyArcJumpLanded"/>
        /// on landing.</summary>
        protected void BeginFireVolleyArcJump()
        {
            Player target = Main.player[NPC.target];
            int dir = target.Center.X < NPC.Center.X ? -1 : 1;
            NPC.direction = dir;
            NPC.spriteDirection = dir;
            NPC.noGravity = false;
            _fireVolleyArcVx = dir * FireVolleyArcJumpForwardSpeed;
            NPC.velocity = new Vector2(_fireVolleyArcVx, -FireVolleyArcJumpUpSpeed);
            _fireVolleyArcFired = false;
            NPC.netUpdate = true;
        }

        /// <summary>Motions that actually swing the weapon (so they get a swing whoosh + arc VFX).
        /// Feint (bait hold) and ChargeChop (run-in) don't swing, so they're excluded.</summary>
        private static bool IsSwingingMotion(ComboMotion m)
            => m != ComboMotion.Feint && m != ComboMotion.ChargeChop;

        /// <summary>
        /// Drives the reactive shield each AI tick (no-op without a shield): raise the guard from
        /// the reactive-block timer, plant + face the player while guarding, and roll a pre-emptive
        /// block in neutral.  The block timer itself ticks down in GlobalNPC.PostAI (mover-agnostic);
        /// damage / poise reduction is applied in ModifyHitBy* + the poise system via ShieldGuarding.
        /// Call AFTER the movement AI so the plant overrides pursuit velocity.
        /// </summary>
        private void UpdateShield()
        {
            if (!HasShield) return;
            var g = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();

            bool freePhase = Phase == AttackPhase.Idle || Phase == AttackPhase.CasualStroll;
            bool grounded  = NPC.velocity.Y == 0f;

            // Guard only in neutral — committed attacks can't also block (you trade blows or you swing).
            _shielding = g.ReactiveBlockTimer > 0 && freePhase && grounded;
            g.ShieldGuarding = _shielding;

            if (!_shielding)
            {
                _shieldWasGuarding   = false;
                _shieldMeleeExtended = false;
                // Neutral: a chance to raise the guard before an incoming threat lands.
                if (freePhase && grounded)
                    tsorcRevampAIs.TryPreemptiveBlock(NPC, g, ShieldGuardTicksRanged);
                return;
            }

            Player target = Main.player[NPC.target];
            int dirToPlayer = target.Center.X < NPC.Center.X ? -1 : 1;

            // Rising edge: capture the facing the guard went up with.
            if (!_shieldWasGuarding)
            {
                _shieldLockedDir   = dirToPlayer;
                _shieldWasGuarding = true;
            }

            bool inMelee = NPC.Distance(target.Center) <= MeleeRange + 8f;
            if (inMelee)
            {
                // Commit the guard in melee: bump the hold to the melee minimum ONCE, LOCK facing so a
                // dodgeroll-through exposes the back for a backstab, and plant.
                if (!_shieldMeleeExtended)
                {
                    g.ReactiveBlockTimer = Math.Max(g.ReactiveBlockTimer, ShieldGuardTicksMelee);
                    _shieldMeleeExtended = true;
                }
                NPC.direction       = _shieldLockedDir;
                NPC.spriteDirection = _shieldLockedDir;
                _directionHoldTicks = Math.Max(_directionHoldTicks, 5);
                NPC.velocity.X *= 0.5f; // plant
            }
            else
            {
                // Out of melee range: track the player and advance slowly behind the shield.
                _shieldLockedDir    = dirToPlayer;
                NPC.direction       = dirToPlayer;
                NPC.spriteDirection = dirToPlayer;
                NPC.velocity.X      = dirToPlayer * ShieldAdvanceSpeed;
            }
        }

        /// <summary>Plays a throwing-release sound, rotating across Item7/18/19 so a rapid volley of
        /// thrown things (stars, flasks, caltrops, bombs) doesn't repeat the exact same clip.</summary>
        protected void PlayThrowSound()
        {
            if (Main.dedServ) return;
            SoundStyle s = Main.rand.Next(3) switch
            {
                0 => SoundID.Item7,
                1 => SoundID.Item18,
                _ => SoundID.Item19,
            };
            SoundEngine.PlaySound(s with { Volume = 0.5f, PitchVariance = 0.2f }, NPC.Center);
        }

        /// <summary>Fired once when a ranged burst's telegraph begins (the weapon first appears in
        /// hand).  <paramref name="secondary"/> = the secondary ranged weapon.  Override to play a
        /// per-weapon "winding up" cue — e.g. a lit fuse on a smoke bomb.</summary>
        protected virtual void OnRangedBurstStarted(bool secondary) { }

        /// <summary>Plays the held weapon's own swing sound (the axe's UseSound) at a swing's start.</summary>
        protected void PlayMeleeSwingSound()
        {
            if (Main.dedServ) return;
            Item w = GetCachedWeaponItem(MeleeWeaponItemType);
            SoundStyle snd = w?.UseSound ?? SoundID.Item1;
            SoundEngine.PlaySound(snd with { Volume = 0.55f, PitchVariance = 0.3f }, NPC.Center);
        }

        /// <summary>
        /// Subtle blade-trail VFX: a few faint smoke wisps near the weapon tip, emitted only while
        /// the blade is actually sweeping (gated on per-tick rotation speed) so it reads as a swing
        /// without cluttering the telegraph or idle hold.
        /// </summary>
        private void SpawnSwingVFX(float rotDelta)
        {
            if (Main.dedServ || !IsWeaponVisiblePhase) return;
            if (Math.Abs(rotDelta) < 0.07f) return;   // only during a real sweep, not a slow lerp
            if (!Main.rand.NextBool(2)) return;        // sparse → subtle
            Vector2 outward = new Vector2(NPC.direction, 0f).RotatedBy(_weaponRotation);
            Vector2 tip     = GetHandPosition() + outward * 20f;
            Dust d = Dust.NewDustPerfect(tip, DustID.Smoke,
                outward.RotatedBy(MathHelper.PiOver2 * Math.Sign(rotDelta)) * 0.5f,
                200, Color.LightGray, 0.55f);
            d.noGravity = true;
        }

        /// <summary>
        /// Publishes this invader's attack state to the poise/hyper-armor system each AI tick.
        /// AttackTelegraphing = windup (still interruptible); AttackCommitted = post-flash swing
        /// (hyper-armor: zero knockback, no poise build).  HyperArmor combos additionally hold the
        /// commit through inter-step pauses, so once they start swinging they can't be staggered
        /// until recovery — only the opening windup leaves a punish window.
        /// </summary>
        private void UpdateAttackCommitFlags()
        {
            var g = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();

            bool telegraph =
                Phase == AttackPhase.MeleeTelegraph  || Phase == AttackPhase.StabTelegraph  ||
                Phase == AttackPhase.RangedTelegraph || Phase == AttackPhase.SpearTelegraph ||
                Phase == AttackPhase.MagicTelegraph  || Phase == AttackPhase.MeleeComboTelegraph ||
                Phase == AttackPhase.BreathTelegraph || Phase == AttackPhase.KnivesTelegraph ||
                Phase == AttackPhase.PierceTelegraph || Phase == AttackPhase.JumpSlashRise ||
                Phase == AttackPhase.AbyssSlashTelegraph ||
                Phase == AttackPhase.TendrilTelegraph || Phase == AttackPhase.TendrilReach ||
                Phase == AttackPhase.TendrilSwingTelegraph ||
                Phase == AttackPhase.AbyssShardTelegraph ||
                Phase == AttackPhase.HomingVolleySwingTelegraph ||
                Phase == AttackPhase.BoomerangSwingTelegraph ||
                Phase == AttackPhase.SpiralFanSwingTelegraph;

            bool committed =
                Phase == AttackPhase.MeleeAttack  || Phase == AttackPhase.StabAttack  ||
                Phase == AttackPhase.RangedAttack || Phase == AttackPhase.SpearAttack ||
                Phase == AttackPhase.MagicAttack  || Phase == AttackPhase.MeleeComboAttack ||
                Phase == AttackPhase.Breathing ||
                Phase == AttackPhase.KnivesThrow || Phase == AttackPhase.KnivesThrowPause ||
                // The whole active dash + (for the stab variant) the impale hold/flick must be
                // uninterruptible - staggering Artorias mid-impale would leave the target frozen
                // with nothing driving the sequence.
                Phase == AttackPhase.PierceDash || Phase == AttackPhase.PierceStabHold ||
                Phase == AttackPhase.PierceStabFlick ||
                Phase == AttackPhase.JumpSlashAttack ||
                // FlipSlashRise already has real i-frames (DodgeTimer); FlipSlashLand is a deliberate
                // static held pose that shouldn't stagger out mid-hold.
                Phase == AttackPhase.FlipSlashRise || Phase == AttackPhase.FlipSlashLand ||
                // Once the first swipe fires, the whole chain (including inter-swipe pauses) is
                // committed - only the initial wind-up is punishable.
                Phase == AttackPhase.AbyssSlashSwipe || Phase == AttackPhase.AbyssSlashPause ||
                Phase == AttackPhase.TendrilSwing ||
                // The whole nova (charge + blast) is a scripted health-threshold set-piece -
                // shouldn't be stagger-cancelled out of mid-charge.
                Phase == AttackPhase.NovaCharge || Phase == AttackPhase.NovaBlast ||
                // Once the first shard event fires, the whole sequence (including inter-event
                // pauses) is committed - only the initial wind-up is punishable.
                Phase == AttackPhase.AbyssShardFire || Phase == AttackPhase.AbyssShardPause ||
                // The overhead chop itself is committed - only the raised wind-up is punishable.
                Phase == AttackPhase.HomingVolleySwing ||
                Phase == AttackPhase.BoomerangSwing ||
                // Once the swing completes and the burst starts, the whole sequence (including
                // inter-shot pauses) is committed - only the raised wind-up is punishable.
                Phase == AttackPhase.SpiralFanSwing ||
                Phase == AttackPhase.SpiralFanBurst || Phase == AttackPhase.SpiralFanPause;

            if (_activeMeleeComboIndex >= 0 && _activeMeleeCombo.HyperArmor
                && Phase == AttackPhase.MeleeComboPause)
                committed = true;

            // Full-window hyper-armor: fold the telegraph into the committed (un-staggerable) window so
            // the whole attack — wind-up included — has hyper-armor, instead of only the post-flash swing.
            if (HyperArmorDuringTelegraph && telegraph)
                committed = true;

            g.AttackCommitted    = committed;
            g.AttackTelegraphing = telegraph && !committed;
        }

        // ── Aerial actions (default implementations) ──────────────────────────────
        /// <summary>Fire a telegraph flash + dust burst when a dive begins.  Subclass override
        /// for thematic dive markers.  Also equips the melee weapon for the dive visual.</summary>
        protected virtual void DoAerialDiveTelegraph()
        {
            if (MeleeWeaponItemType >= 0)
                SetDisplayWeapon(MeleeWeaponItemType, swing: false);
            if (Main.netMode == NetmodeID.MultiplayerClient) return;
            SpawnTelegraphFlash(Color.OrangeRed);
        }

        /// <summary>Spawn the dive's damage hitbox.  Called repeatedly while in DiveAttack mode
        /// to give the dive a sustained contact-damage feel.</summary>
        protected virtual void DoAerialDiveHit()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient) return;
            // Dive: 120 px wide box centered on the NPC so a player directly under the dive lands a hit.
            int boxW = 120;
            int boxH = 80;
            Vector2 topLeft = NPC.Center - new Vector2(boxW / 2f, boxH / 2f);
            Projectile.NewProjectile(
                NPC.GetSource_FromThis(), topLeft, Vector2.Zero,
                ModContent.ProjectileType<Projectiles.Enemy.Weapons.InvaderMeleeHitbox>(),
                (int)(MeleeDamage * 1.2f), 4f, Main.myPlayer, boxW, boxH);
        }

        /// <summary>Fire a single aerial ranged shot.  Default delegates to DoRangedAttack
        /// so subclass spawns its existing projectile + sound.  Override for true aerial-only
        /// projectiles (e.g. bombs dropped straight down).</summary>
        protected virtual void DoAerialRangedShot(Player target)
        {
            DoRangedAttack();
        }

        /// <summary>
        /// After any recovery phase, roll for a casual stroll.  If the roll fails
        /// (or the subclass sets CasualStrollChance to 0), go straight back to Idle.
        /// </summary>
        protected void EnterCasualOrIdle()
        {
            if (NPC.HasValidTarget && NPC.Distance(Main.player[NPC.target].Center) <= StabRange + 40f)
            {
                EnterPhase(AttackPhase.Idle, 0);
                return;
            }

            if (CasualStrollChance > 0 && Main.rand.Next(100) < CasualStrollChance)
                EnterPhase(AttackPhase.CasualStroll,
                    Main.rand.Next(CasualStrollMinTicks, CasualStrollMaxTicks + 1));
            else
                EnterPhase(AttackPhase.Idle, 0);
        }

        /// <summary>
        /// Picks an index into a pattern array using weighted chances, or uniformly at random
        /// when <paramref name="chances"/> is null or empty.
        /// </summary>
        /// <param name="chances">Relative weights for each index.  Does not need to sum to 100.</param>
        /// <param name="count">Number of valid patterns to pick from.</param>
        private static int PickPatternByChance(int[] chances, int count)
        {
            if (count <= 1) return 0;
            if (chances == null || chances.Length == 0)
                return Main.rand.Next(count);

            int limit = Math.Min(chances.Length, count);
            int total = 0;
            for (int i = 0; i < limit; i++) total += chances[i];
            if (total <= 0) return Main.rand.Next(count);

            int roll = Main.rand.Next(total);
            int cumulative = 0;
            for (int i = 0; i < limit; i++)
            {
                cumulative += chances[i];
                if (roll < cumulative) return i;
            }
            return 0;
        }

        // ── Attack implementations (subclass) ────────────────────────────────────
        protected abstract void DoMeleeAttack();
        protected abstract void DoRangedAttack();
        protected virtual  void DoStabAttack()  { }
        /// <summary>
        /// Spawn the spear hitbox or projectile.  Called at the moment SpearAttack begins.
        /// Use <see cref="SpearWeaponItemType"/>, <see cref="SpearDamage"/>, and <see cref="SpearRange"/>.
        /// </summary>
        protected virtual  void DoSpearAttack() { }
        /// <summary>
        /// Fire the magic projectile.  Called at the moment MagicAttack begins.
        /// Use <see cref="MagicWeaponItemType"/> and <see cref="MagicDamage"/>.
        /// For a sustained cast, set <see cref="_magicAttackTicksOverride"/> here and emit per-tick in
        /// <see cref="DoMagicTick"/>.
        /// </summary>
        protected virtual  void DoMagicAttack() { }

        /// <summary>Called every tick of the MagicAttack phase with the ticks remaining.  Default no-op;
        /// override (together with <see cref="_magicAttackTicksOverride"/>) for channeled casts such as a
        /// timed meteor rain.</summary>
        protected virtual  void DoMagicTick(int ticksRemaining) { }

        /// <summary>Called every tick of the BreathTelegraph phase, with elapsed ticks counting up from 0.
        /// Emit the swelling mouth-ember / charge VFX here.</summary>
        protected virtual  void DoBreathWindup(int elapsed) { }

        /// <summary>Called every tick of the active Breathing phase, with the breath ticks remaining.
        /// Spawn the breath projectile(s) here (typically gated on a tick interval).</summary>
        protected virtual  void DoBreathTick(int ticksRemaining) { }

        /// <summary>Fired once when the breath stream releases (telegraph → Breathing).  Override for a
        /// flamethrower whoosh / roar cue.</summary>
        protected virtual  void OnBreathStart() { }

        /// <summary>Spawn one Cursed Knives volley (typically 3 knives).  Called at each throw frame.</summary>
        protected virtual  void DoCursedKnivesThrow() { }

        /// <summary>Pick the volley count + inter-volley gap for a Cursed Knives attack from the range to
        /// the player, then enter the telegraph.  Close = 1 volley; mid = 2 back-to-back; far+LOS = 3.</summary>
        private void StartCursedKnives(float dist, bool hasLOS)
        {
            if (dist <= CursedKnivesCloseRange)      { _cursedKnivesVolleysLeft = 1; _cursedKnivesGap = 0; }
            else if (dist <= CursedKnivesMidRange)   { _cursedKnivesVolleysLeft = 2; _cursedKnivesGap = CursedKnivesBackToBackGap; }
            else if (hasLOS)                         { _cursedKnivesVolleysLeft = 3; _cursedKnivesGap = CursedKnivesFarGap; }
            else                                     { _cursedKnivesVolleysLeft = 1; _cursedKnivesGap = 0; }
            EnterPhase(AttackPhase.KnivesTelegraph, CursedKnivesTelegraphTicks);
        }

        /// <summary>
        /// Minimum tick count a telegraph phase is allowed to last. <see cref="CheckAndFireFlash"/>
        /// fires the warning flash when <c>PhaseTimer ≤ 30</c>, so any telegraph shorter than this
        /// would fire the flash AT (or very near) the attack moment, defeating the purpose of the
        /// warning. Clamping here lets subclasses set whatever telegraph value they want without
        /// having to remember the floor — the player always gets a real 30-tick warning.
        /// </summary>
        private const int MinTelegraphTicks = 30;

        protected void EnterPhase(AttackPhase phase, int duration)
        {
            bool isTelegraph = phase == AttackPhase.MeleeTelegraph
                            || phase == AttackPhase.StabTelegraph
                            || phase == AttackPhase.RangedTelegraph
                            || phase == AttackPhase.SpearTelegraph
                            || phase == AttackPhase.MagicTelegraph
                            || phase == AttackPhase.MeleeComboTelegraph
                            || phase == AttackPhase.BreathTelegraph
                            || phase == AttackPhase.KnivesTelegraph;
            Phase      = phase;
            // Guarantee at least 30 ticks of telegraph so the flash always leads the attack by 30.
            PhaseTimer = isTelegraph ? Math.Max(duration, MinTelegraphTicks) : duration;
            // Each new telegraph phase gets its own flash.
            if (isTelegraph) _flashFired = false;
        }

        private void SlowDown() => NPC.velocity.X *= 0.80f;

        /// <summary>
        /// Preemptive quick-step: when the player is mid-swing within melee reach, roll
        /// <see cref="PreemptiveQuickStepChance"/> to dash THROUGH them (i-frames + pass-through, can't
        /// damage them) and land past them.  The shared <see cref="tsorcRevampAIs.ArmQuickStep"/> picks the
        /// forward step when there's room.  Uses the proactive-evasion cooldown so it can't spam.
        /// </summary>
        private void TryPreemptiveQuickStep(tsorcRevampGlobalNPC gnpc, Player target, float dist)
        {
            if (NPC.velocity.Y != 0f) return;                 // grounded only
            if (dist > MeleeRange + 56f) return;              // must be in melee threat range
            if (target.itemAnimation <= 0) return;            // player must be actively swinging/using
            if (Main.rand.Next(100) >= PreemptiveQuickStepChance) return;

            NPC.TargetClosest(true);
            tsorcRevampAIs.ArmQuickStep(NPC, gnpc, allowForward: true);
            gnpc.DodgeCooldown = 120; // shared proactive-evasion cooldown (~2 s)
            NPC.netUpdate = true;
        }

        /// <summary>
        /// Proactive projectile dodge for SF4 invaders (the legacy BasicAI Agility scan doesn't run for
        /// them).  Scans for an incoming friendly projectile roughly aimed at this invader; on a hit,
        /// rolls the GlobalNPC <c>Agility</c> stat and either jumps over it (with headroom) or triggers
        /// the mover-agnostic DodgeTimer i-frames.  Mirrors the RunFighterCombatTriggers dodge logic.
        /// </summary>
        private void TryEvadeIncomingProjectile(tsorcRevampGlobalNPC gnpc, Player target)
        {
            if (gnpc.Agility <= 0f) return; // Agility unset/zero → no dodging
            float rangeSq = ProjectileEvadeRange * ProjectileEvadeRange;

            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (!p.active || !p.friendly || p.damage <= 0) continue;
                if (p.DistanceSQ(NPC.Center) >= rangeSq) continue;
                // Roughly aimed at us?  Compare the projectile's heading to the bearing toward this NPC.
                if (UsefulFunctions.CompareAngles(p.velocity, UsefulFunctions.Aim(p.Center, NPC.Center, 1f)) >= 0.35f) continue;

                if (Main.rand.NextFloat() < gnpc.Agility)
                {
                    bool headroom = true;
                    for (int j = 1; j <= 8; j++)
                    {
                        if (UsefulFunctions.IsTileReallySolid(NPC.Center + new Vector2(0f, -16f * j)))
                        {
                            headroom = false;
                            break;
                        }
                    }
                    // Grounded + headroom → preemptive jump; otherwise an in-place i-frame roll.
                    if (headroom && NPC.velocity.Y == 0f && Main.rand.NextBool())
                        NPC.velocity.Y -= 8f;
                    else
                        gnpc.DodgeTimer = 30;

                    gnpc.DodgeCooldown = (int)(300 * (1f - gnpc.Agility));
                    NPC.netUpdate = true;
                }
                break; // react to at most one projectile per tick
            }
        }

        // ── Damage tracking for emergency heal + reactive shield ──────────────────
        public override void OnHitByItem(Player player, Item item, NPC.HitInfo hit, int damageDone)
        {
            _recentDamage += (float)damageDone / NPC.lifeMax;
            // Only a FRONT hit can snap the guard up — a backstab must not re-raise it.
            if (HasShield && Math.Sign(player.Center.X - NPC.Center.X) == NPC.direction)
                tsorcRevampAIs.TryOnHitBlock(NPC, NPC.GetGlobalNPC<tsorcRevampGlobalNPC>(), true, ShieldGuardTicksRanged);
        }

        public override void OnHitByProjectile(Projectile projectile, NPC.HitInfo hit, int damageDone)
        {
            _recentDamage += (float)damageDone / NPC.lifeMax;
            if (HasShield && Math.Sign(projectile.Center.X - NPC.Center.X) == NPC.direction)
                tsorcRevampAIs.TryOnHitBlock(NPC, NPC.GetGlobalNPC<tsorcRevampGlobalNPC>(), projectile.DamageType == DamageClass.Melee, ShieldGuardTicksRanged);
        }

        // ── Reactive shield: reduce FRONT damage while guarding (backstabs bypass) ──
        public override void ModifyHitByItem(Player player, Item item, ref NPC.HitModifiers modifiers)
        {
            if (_shielding && Math.Sign(player.Center.X - NPC.Center.X) == NPC.direction)
                modifiers.FinalDamage *= ShieldDamageReduction;
        }

        public override void ModifyHitByProjectile(Projectile projectile, ref NPC.HitModifiers modifiers)
        {
            if (_shielding && Math.Sign(projectile.Center.X - NPC.Center.X) == NPC.direction)
                modifiers.FinalDamage *= ShieldDamageReduction;
        }

        // ── Melee hitbox helper ───────────────────────────────────────────────────
        // Arms the tracked blade check (see ArmBladeHit/TickBladeHit) with this swing's reach
        // instead of hitting immediately — the hit only actually lands on the tick(s) the weapon
        // sprite's real hand→tip line genuinely overlaps a player, matching the drawn swing arc.
        protected void TryMeleeHit(float reach = -1f)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;
            float r = reach < 0 ? MeleeRange * 0.7f : reach;
            ArmBladeHit(r, MeleeDamage, knockback: 3f);
        }

        /// <summary>
        /// Arms the tracked blade check for the swing/step that's about to play: TickBladeHit will
        /// test every subsequent tick (until the phase ends) and only actually connect on the
        /// tick(s) where the weapon sprite's real swept position overlaps a player. Called by
        /// TryMeleeHit and the base DoComboMeleeHit; an override that skips this entirely (e.g.
        /// BlackNinja's flail combo, which fires a real physical projectile instead of a blade
        /// sweep) correctly leaves TickBladeHit a no-op for that swing.
        /// </summary>
        private void ArmBladeHit(float reach, int damage, float knockback)
        {
            _bladeArmed           = true;
            _activeBladeReach     = reach;
            _activeBladeDamage    = damage;
            _activeBladeKnockback = knockback;
        }

        /// <summary>
        /// Per-tick "is the blade actually on the player" check. Call every tick while a melee
        /// swing/step is live (MeleeAttack / StabAttack / SpearAttack / MeleeComboAttack). Traces a
        /// capsule from the hand to the weapon tip along <see cref="GetWeaponWorldDirection"/> — the
        /// exact angle actually rendered — so a hit can only land on the tick(s) the sprite is
        /// genuinely overlapping the target, instead of for the swing's entire duration regardless
        /// of where the blade is pointing (the old static-box behavior).
        /// </summary>
        private void TickBladeHit()
        {
            if (!_bladeArmed || _activeBladeReach <= 0f || Main.netMode == NetmodeID.MultiplayerClient)
                return;

            Vector2 origin = GetHandPosition();
            Vector2 tip    = origin + GetWeaponWorldDirection() * _activeBladeReach;
            float earlyOutRange = _activeBladeReach + MeleeBladeWidth + 40f;

            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player player = Main.player[i];
                if (!player.active || player.dead) continue;
                if (Vector2.DistanceSquared(player.Center, NPC.Center) > earlyOutRange * earlyOutRange) continue;

                if (!MeleeBladeCollision.SegmentIntersectsRect(origin, tip, MeleeBladeWidth, player.getRect()))
                    continue;

                Vector2 hitPoint = MeleeBladeCollision.ClosestPointOnSegment(origin, tip, player.Center);
                SpawnMeleeHitbox(hitPoint, MeleeBladeWidth, _activeBladeDamage, _activeBladeKnockback);
                _lastAttackHitConnected = true;
                OnBladeHit(player);
            }
        }

        /// <summary>Fires once per confirmed real blade-overlap hit (see <see cref="TickBladeHit"/>),
        /// right after the damage/knockback hitbox is spawned. Override for per-invader on-hit
        /// effects (debuffs, extra VFX, etc.) without touching the shared hit-detection code.</summary>
        protected virtual void OnBladeHit(Player player) { }

        /// <summary>
        /// Spawn a small, momentary hostile hitbox at a confirmed blade-overlap point. Geometry is
        /// already resolved by the caller (TickBladeHit) — this is just the trigger that hands
        /// damage/knockback/immune-frame handling to vanilla's existing hostile-projectile pipeline.
        /// </summary>
        private void SpawnMeleeHitbox(Vector2 center, float size, int damage, float knockback)
        {
            int box = (int)size;
            Vector2 topLeft = center - new Vector2(box / 2f, box / 2f);
            Projectile.NewProjectile(
                NPC.GetSource_FromThis(), topLeft, Vector2.Zero,
                ModContent.ProjectileType<Projectiles.Enemy.Weapons.InvaderMeleeHitbox>(),
                damage, knockback, Main.myPlayer, box, box);
        }

        // ── Telegraph dust ────────────────────────────────────────────────────────
        private void SpawnTelegraphDust()
        {
            if (Main.rand.NextBool(3))
            {
                Dust d = Dust.NewDustDirect(NPC.Center - new Vector2(8f), 16, 16, 89,
                    Main.rand.NextFloat(-1f, 1f), Main.rand.NextFloat(-2f, -0.5f),
                    0, default, 1.2f);
                d.noGravity = true;
            }
        }

        // ── Telegraph flash VFX ───────────────────────────────────────────────────
        /// <summary>
        /// Fires the ring-flash VFX once the attack is within 30 frames.
        /// Call each tick during a telegraph phase; self-gates via <see cref="_flashFired"/>.
        /// Short telegraphs (< 30 ticks) fire immediately on the first call.
        /// </summary>
        private void CheckAndFireFlash(Color color)
        {
            if (_flashFired || PhaseTimer > 30) return;
            SpawnTelegraphFlash(color);
            _flashFired = true;
        }

        private void SpawnTelegraphFlash(Color color)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient) return;
            Projectile.NewProjectileDirect(
                NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero,
                ModContent.ProjectileType<Projectiles.VFX.TelegraphFlash>(),
                0, 0, Main.myPlayer,
                UsefulFunctions.ColorToFloat(color));
        }

        // ─────────────────────────────────────────────────────────────────────────
        // Weapon visual
        // ─────────────────────────────────────────────────────────────────────────

        protected void SetDisplayWeapon(int itemType, bool swing)
        {
            _heldItemType = itemType;
            _weaponAnimMax = GetWeaponUseAnimation(itemType);
            if (swing)
                _weaponAnim = _weaponAnimMax;
        }

        private int GetWeaponUseAnimation(int itemType)
        {
            if (itemType <= 0)
            {
                return DefaultWeaponAnimMax;
            }

            Item item = GetCachedWeaponItem(itemType);
            int useAnimation = item?.useAnimation ?? 0;
            return Math.Max(1, useAnimation > 0 ? useAnimation : DefaultWeaponAnimMax);
        }

        private int GetMeleeSwingTicks(int requestedTicks)
        {
            return Math.Max(requestedTicks, GetWeaponUseAnimation(MeleeWeaponItemType));
        }

        private void TickWeaponAnim()
        {
            if (_weaponAnim > 0)
                _weaponAnim--;

            float t = _weaponAnimMax > 0 ? 1f - (float)_weaponAnim / _weaponAnimMax : 1f;

            if (Phase == AttackPhase.StabTelegraph)
            {
                // Telegraph: dip sword downward — at +π/2 the sprite (naturally diagonal at 0)
                // appears 45° below horizontal, giving a clear "cocked for thrust" read.
                _weaponRotation = MathHelper.Lerp(_weaponRotation, MathHelper.PiOver2, 0.18f);
            }
            else if (Phase == AttackPhase.StabAttack)
            {
                // Thrust: snap to horizontal — at +π/4 the sprite appears perfectly flat,
                // pointing straight at the player (the "90° / flat" the user asked for).
                _weaponRotation = MathHelper.Lerp(_weaponRotation, MathHelper.PiOver4, 0.42f);
            }
            else if (Phase == AttackPhase.RangedTelegraph)
            {
                float rangedT = RangedTelegraphTicks > 0
                    ? 1f - (float)PhaseTimer / RangedTelegraphTicks
                    : 1f;
                switch (_activeRangedStyle)
                {
                    case RangedStyle.Crossbow:
                        // Arm extends forward to a horizontal aim and holds — no arc.
                        _weaponRotation = MathHelper.Lerp(_weaponRotation, 0.05f, 0.22f);
                        break;
                    case RangedStyle.Bow:
                        // Arm gradually rises from hold → overhead as the bow draws back.
                        _weaponRotation = MathHelper.Lerp(-0.30f, -1.10f, rangedT);
                        break;
                    default: // Throw
                        // Hold the hand high while lining up the throw.
                        _weaponRotation = MathHelper.Lerp(-0.75f, -0.10f, rangedT);
                        break;
                }
            }
            else if (Phase == AttackPhase.RangedAttack)
            {
                float rangedT = RangedAttackTicks > 0
                    ? 1f - (float)PhaseTimer / RangedAttackTicks
                    : 1f;
                switch (_activeRangedStyle)
                {
                    case RangedStyle.Crossbow:
                        // Small forward snap / click — arm barely moves, just a quick jolt.
                        _weaponRotation = MathHelper.Lerp(0.05f, 0.18f, rangedT);
                        break;
                    case RangedStyle.Bow:
                        // Snap the arm forward from overhead as the arrow releases.
                        _weaponRotation = MathHelper.Lerp(-1.10f, 0.35f, rangedT);
                        break;
                    default: // Throw
                        // Swing the arm forward as the projectile leaves the hand.
                        _weaponRotation = MathHelper.Lerp(-0.10f, 0.45f, rangedT);
                        break;
                }
            }
            else if (Phase == AttackPhase.CrossbowBurstPause)
            {
                // Hold horizontal aim between shots — same target angle as Crossbow telegraph.
                _weaponRotation = MathHelper.Lerp(_weaponRotation, 0.05f, 0.22f);
            }
            else if (Phase == AttackPhase.SpearTelegraph)
            {
                // Dip the spear down slightly — same "cocked for thrust" read as StabTelegraph.
                _weaponRotation = MathHelper.Lerp(_weaponRotation, MathHelper.PiOver2, 0.18f);
            }
            else if (Phase == AttackPhase.SpearAttack)
            {
                // Snap to horizontal for the poke — slower ease than stab since it's a stationary reach.
                _weaponRotation = MathHelper.Lerp(_weaponRotation, MathHelper.PiOver4, 0.30f);
            }
            else if (Phase == AttackPhase.MagicTelegraph)
            {
                float magicT = MagicTelegraphTicks > 0
                    ? 1f - (float)PhaseTimer / MagicTelegraphTicks
                    : 1f;
                // Arm rises overhead during the charge (Use1 / raised overhead pose).
                _weaponRotation = MathHelper.Lerp(_weaponRotation, -1.40f, 0.12f);
            }
            else if (Phase == AttackPhase.MagicAttack)
            {
                float magicT = MagicAttackTicks > 0
                    ? 1f - (float)PhaseTimer / MagicAttackTicks
                    : 1f;
                // Thrust forward as the spell fires.
                _weaponRotation = MathHelper.Lerp(-1.40f, 0.20f, magicT);
            }
            else if (Phase == AttackPhase.PierceTelegraph)
            {
                // Same "cocked, arm extended toward the player" pose as JoustDash's telegraph.
                _weaponRotation = MathHelper.Lerp(_weaponRotation, MathHelper.PiOver2, 0.22f);
            }
            else if (Phase == AttackPhase.PierceDash)
            {
                // Held straight forward, tip leading — same active-thrust angle as JoustDash.
                _weaponRotation = MathHelper.Lerp(_weaponRotation, MathHelper.PiOver4, 0.35f);
            }
            else if (Phase == AttackPhase.PierceStabHold)
            {
                // Raise 0→90°: from straight-forward (PiOver4, "3 o'clock") up to overhead-vertical
                // (-PiOver4, "12 o'clock") as the impaled target is lifted.
                float raiseT = PierceStabRaiseTicks > 0 ? 1f - (float)PhaseTimer / PierceStabRaiseTicks : 1f;
                _weaponRotation = MathHelper.Lerp(MathHelper.PiOver4, -MathHelper.PiOver4, raiseT);
            }
            else if (Phase == AttackPhase.PierceStabFlick)
            {
                // Snap back down and past horizontal — the flick that launches the target away.
                float flickT = PierceStabFlickTicks > 0 ? 1f - (float)PhaseTimer / PierceStabFlickTicks : 1f;
                _weaponRotation = MathHelper.Lerp(-MathHelper.PiOver4, MathHelper.PiOver2, flickT);
            }
            else if (Phase == AttackPhase.JumpSlashDodgeback)
            {
                // No special pose during the backward roll — ease to the normal carried hold.
                _weaponRotation = MathHelper.Lerp(_weaponRotation, HoldRotation, 0.15f);
            }
            else if (Phase == AttackPhase.JumpSlashRise)
            {
                // Cocked "up and back" — 15° past straight-up toward the rear (12 o'clock is -45°;
                // 15° further back is -60°).
                _weaponRotation = MathHelper.Lerp(_weaponRotation, MathHelper.ToRadians(-60f), 0.30f);
            }
            else if (Phase == AttackPhase.JumpSlashAttack)
            {
                // The half-circle downward swipe: -60° (cocked) sweeping through vertical/forward to
                // +55° (past horizontal, angled down-forward) — a ~115° arc.
                float slashT = JumpSlashAttackTicks > 0 ? 1f - (float)PhaseTimer / JumpSlashAttackTicks : 1f;
                _weaponRotation = MathHelper.Lerp(MathHelper.ToRadians(-60f), MathHelper.ToRadians(55f), slashT);
            }
            else if (Phase == AttackPhase.FlipSlashRise)
            {
                // Continuous spin while airborne (same free-running style as ComboMotion.Spin) —
                // no fixed start/end, just a steady rotation for as long as the flip lasts.
                _weaponRotation += FlipSlashSpinSpeed;
                if (_weaponRotation > MathHelper.TwoPi) _weaponRotation -= MathHelper.TwoPi;
            }
            else if (Phase == AttackPhase.FlipSlashLand)
            {
                // Snaps to the landing slam pose (195° clockwise from straight-up, i.e. past 6 o'clock
                // toward 7) and holds there for the whole phase — a deliberate static pose, not eased.
                _weaponRotation = MathHelper.ToRadians(195f - 45f);
            }
            else if (Phase == AttackPhase.AbyssSlashTelegraph)
            {
                // Underhand, facing down (180°) arcing 170° to the held "sword post" pose (10°, blade
                // near-vertical, grip low) — held there once the arc portion of the timer runs out.
                int totalAbyssTicks = AbyssSlashArcTicks + AbyssSlashHoldTicks;
                int elapsedAbyss    = totalAbyssTicks - PhaseTimer;
                if (elapsedAbyss < AbyssSlashArcTicks)
                {
                    float armT = AbyssSlashArcTicks > 0 ? elapsedAbyss / (float)AbyssSlashArcTicks : 1f;
                    _weaponRotation = MathHelper.Lerp(MathHelper.ToRadians(180f - 45f), MathHelper.ToRadians(10f - 45f), armT);
                }
                else
                {
                    _weaponRotation = MathHelper.ToRadians(10f - 45f);
                }
            }
            else if (Phase == AttackPhase.AbyssSlashSwipe)
            {
                // Quick release swing: from the held "post" pose back down through to a forward-down
                // follow-through, matching where the projectile fires from.
                float swipeT = AbyssSlashSwipeTicks > 0 ? 1f - (float)PhaseTimer / AbyssSlashSwipeTicks : 1f;
                _weaponRotation = MathHelper.Lerp(MathHelper.ToRadians(10f - 45f), MathHelper.ToRadians(170f - 45f), swipeT);
            }
            else if (Phase == AttackPhase.TendrilTelegraph || Phase == AttackPhase.TendrilReach)
            {
                // Arm reaches out toward the target - same "cocked, extended forward" pose as
                // Piercing Dash's telegraph.
                _weaponRotation = MathHelper.Lerp(_weaponRotation, MathHelper.PiOver2, 0.20f);
            }
            else if (Phase == AttackPhase.TendrilSwingTelegraph)
            {
                // The arm returns and winds up the finishing swing: same 180°→10° arc as Abyss
                // Slash's wind-up, held at 10° once the arc portion completes.
                int totalTendrilTicks = TendrilSwingArcTicks + TendrilSwingHoldTicks;
                int elapsedTendril    = totalTendrilTicks - PhaseTimer;
                if (elapsedTendril < TendrilSwingArcTicks)
                {
                    float armT = TendrilSwingArcTicks > 0 ? elapsedTendril / (float)TendrilSwingArcTicks : 1f;
                    _weaponRotation = MathHelper.Lerp(MathHelper.ToRadians(180f - 45f), MathHelper.ToRadians(10f - 45f), armT);
                }
                else
                {
                    _weaponRotation = MathHelper.ToRadians(10f - 45f);
                }
            }
            else if (Phase == AttackPhase.TendrilSwing)
            {
                // Same 10°→170° release as Abyss Slash's swipe - the "standard" underhand swing shape.
                float swingT = TendrilSwingTicks > 0 ? 1f - (float)PhaseTimer / TendrilSwingTicks : 1f;
                _weaponRotation = MathHelper.Lerp(MathHelper.ToRadians(10f - 45f), MathHelper.ToRadians(170f - 45f), swingT);
            }
            else if (Phase == AttackPhase.HomingVolleySwingTelegraph)
            {
                // Raise the sword fully overhead, cocked back further than Jump Slash's cocked pose -
                // a bigger, more deliberate wind-up for the volley's release chop.
                _weaponRotation = MathHelper.Lerp(_weaponRotation, MathHelper.ToRadians(-100f), 0.25f);
            }
            else if (Phase == AttackPhase.HomingVolleySwing)
            {
                // Full overhead chop: cocked back down through vertical to a down-forward
                // follow-through - the volley fires partway through this arc.
                float swingT = HomingVolleySwingTicks > 0 ? 1f - (float)PhaseTimer / HomingVolleySwingTicks : 1f;
                _weaponRotation = MathHelper.Lerp(MathHelper.ToRadians(-100f), MathHelper.ToRadians(70f), swingT);
            }
            else if (Phase == AttackPhase.BoomerangSwingTelegraph || Phase == AttackPhase.SpiralFanSwingTelegraph)
            {
                // Same overhead cocked-back wind-up as Homing Volley - all three "sword launch"
                // attacks share one visual identity for the raise.
                _weaponRotation = MathHelper.Lerp(_weaponRotation, MathHelper.ToRadians(-100f), 0.25f);
            }
            else if (Phase == AttackPhase.BoomerangSwing)
            {
                // Same overhead chop shape as Homing Volley - the crescent(s) fire partway through.
                float swingT = BoomerangSwingTicks > 0 ? 1f - (float)PhaseTimer / BoomerangSwingTicks : 1f;
                _weaponRotation = MathHelper.Lerp(MathHelper.ToRadians(-100f), MathHelper.ToRadians(70f), swingT);
            }
            else if (Phase == AttackPhase.SpiralFanSwing)
            {
                // Same overhead chop shape again, but purely a visual wind-up here - the burst that
                // follows (SpiralFanBurst/Pause) carries the actual firing sequence.
                float swingT = SpiralFanSwingTicks > 0 ? 1f - (float)PhaseTimer / SpiralFanSwingTicks : 1f;
                _weaponRotation = MathHelper.Lerp(MathHelper.ToRadians(-100f), MathHelper.ToRadians(70f), swingT);
            }
            else if (Phase == AttackPhase.SpiralFanBurst || Phase == AttackPhase.SpiralFanPause)
            {
                // Hold the follow-through pose for the whole burst rather than resetting to neutral
                // between shots.
                _weaponRotation = MathHelper.ToRadians(70f);
            }
            else if (Phase == AttackPhase.FireVolleyBackLeap || Phase == AttackPhase.FireVolleyDodgeThrough)
            {
                // Just carry the axe through the reposition move — no swing here.
                _weaponRotation = MathHelper.Lerp(_weaponRotation, HoldRotation, 0.15f);
            }
            else if (Phase == AttackPhase.FireVolleyArcJump)
            {
                // Same shape as LeapSlam: raise overhead on the way up, chop down through the apex.
                if (NPC.velocity.Y < 0f) _weaponRotation = MathHelper.Lerp(_weaponRotation, -1.3f, 0.20f);
                else                     _weaponRotation = MathHelper.Lerp(_weaponRotation, 1.3f, 0.18f);
            }
            else if (Phase == AttackPhase.KnivesTelegraph || Phase == AttackPhase.KnivesThrowPause)
            {
                // Hold the knife high, ready to throw (same read as the throwing-star telegraph).
                _weaponRotation = MathHelper.Lerp(_weaponRotation, -0.75f, 0.20f);
            }
            else if (Phase == AttackPhase.KnivesThrow)
            {
                // Swing the arm forward as the knives leave the hand.
                float kt = CursedKnivesThrowTicks > 0 ? 1f - (float)PhaseTimer / CursedKnivesThrowTicks : 1f;
                _weaponRotation = MathHelper.Lerp(-0.75f, 0.45f, kt);
            }
            else if (Phase == AttackPhase.MeleeTelegraph || Phase == AttackPhase.MeleeAttack)
            {
                // Same OverheadArc shape as the combo system's OverheadArc case, so the plain
                // one-shot swing (DoMeleeAttack/TryMeleeHit, used outside the combo system) gets
                // the same easing/flip/aim-bias opt-ins instead of only combos getting them.
                float a0 = -1.3f, a1 = 1.0f;
                if (UseAlternateFlip && _comboSwingFlipped) (a0, a1) = (a1, a0);
                if (UseAimAdaptiveArc) { a0 += _comboAimBias; a1 += _comboAimBias; }

                if (Phase == AttackPhase.MeleeTelegraph)
                    // Wind-up: weapon rises from hold angle to the top of the arc quickly
                    _weaponRotation = MathHelper.Lerp(_weaponRotation, a0, 0.30f);
                else
                    // Downswing: full broadsword arc, raised behind head → slash down-forward
                    // t runs 0->1 over the held item useAnimation window (reset when swing begins)
                    _weaponRotation = SwingEase.Apply(a0, a1, t, UseSwingEasing);
            }
            else if (Phase == AttackPhase.MeleeComboTelegraph
                  || Phase == AttackPhase.MeleeComboAttack
                  || Phase == AttackPhase.MeleeComboPause)
            {
                // Drive rotation from the current step's ComboMotion.
                var step = _activeMeleeCombo.Steps[_meleeComboStepIndex];
                bool inTel = Phase == AttackPhase.MeleeComboTelegraph;
                bool inPause = Phase == AttackPhase.MeleeComboPause;

                // Applies the alternating-flip / aim-adaptive-bias opt-ins (both default off) to a
                // motion's fixed (start, end) arc endpoints. Telegraph/pause poses below are all
                // expressed as a Lerp fraction of these same endpoints (not separate hardcoded
                // angles), so flip/bias stay consistent across wind-up -> strike with no snap.
                (float, float) Endpoints(float a0, float a1)
                {
                    if (UseAlternateFlip && _comboSwingFlipped) (a0, a1) = (a1, a0);
                    if (UseAimAdaptiveArc) { a0 += _comboAimBias; a1 += _comboAimBias; }
                    return (a0, a1);
                }

                switch (step.Motion)
                {
                    case ComboMotion.OverheadArc:
                    {
                        var (a0, a1) = Endpoints(-1.3f, 1.0f);
                        if (inTel)        _weaponRotation = MathHelper.Lerp(_weaponRotation, a0, 0.30f);
                        else if (inPause) _weaponRotation = MathHelper.Lerp(_weaponRotation, MathHelper.Lerp(a0, a1, 0.130f), 0.20f);
                        else              _weaponRotation = SwingEase.Apply(a0, a1, t, UseSwingEasing);
                        break;
                    }
                    case ComboMotion.UnderhandArc:
                    {
                        // Rising cut: dipped low-forward (+1.0) → up-FORWARD (-1.0).  Ending at
                        // -1.3 would map to body row 1, whose hand offset sits BEHIND the head
                        // (X=-8) — making the swing finish over the shoulder.  -1.0 keeps it in
                        // row 2 (hand up-forward, X=+4) for a clean rising slash.
                        var (a0, a1) = Endpoints(1.0f, -1.0f);
                        if (inTel)        _weaponRotation = MathHelper.Lerp(_weaponRotation, a0, 0.30f);
                        else if (inPause) _weaponRotation = MathHelper.Lerp(_weaponRotation, MathHelper.Lerp(a0, a1, 0.150f), 0.20f);
                        else              _weaponRotation = SwingEase.Apply(a0, a1, t, UseSwingEasing);
                        break;
                    }
                    case ComboMotion.HorizontalSweep:
                    {
                        // Flat side-to-side: arm extends, weapon held near horizontal
                        var (a0, a1) = Endpoints(-0.4f, 0.6f);
                        if (inTel)        _weaponRotation = MathHelper.Lerp(_weaponRotation, a0, 0.25f);
                        else if (inPause) _weaponRotation = MathHelper.Lerp(_weaponRotation, MathHelper.Lerp(a0, a1, 0.700f), 0.18f);
                        else              _weaponRotation = SwingEase.Apply(a0, a1, t, UseSwingEasing);
                        break;
                    }
                    case ComboMotion.VerticalChop:
                    {
                        // Straight overhead → straight down (hammer)
                        var (a0, a1) = Endpoints(-1.55f, 1.4f);
                        if (inTel)        _weaponRotation = MathHelper.Lerp(_weaponRotation, a0, 0.32f);
                        else if (inPause) _weaponRotation = MathHelper.Lerp(_weaponRotation, MathHelper.Lerp(a0, a1, 0.119f), 0.18f);
                        else              _weaponRotation = SwingEase.Apply(a0, a1, t, UseSwingEasing);
                        break;
                    }
                    case ComboMotion.Thrust:
                        if (inTel)        _weaponRotation = MathHelper.Lerp(_weaponRotation, MathHelper.PiOver2, 0.20f);
                        else if (inPause) _weaponRotation = MathHelper.Lerp(_weaponRotation, MathHelper.PiOver2 * 0.8f, 0.18f);
                        else              _weaponRotation = MathHelper.Lerp(_weaponRotation, MathHelper.PiOver4, 0.42f);
                        break;
                    case ComboMotion.JoustDash:
                        if (inTel)        _weaponRotation = MathHelper.Lerp(_weaponRotation, MathHelper.PiOver2, 0.18f);
                        else              _weaponRotation = MathHelper.Lerp(_weaponRotation, MathHelper.PiOver4, 0.45f);
                        break;
                    case ComboMotion.Spin:
                        // Continuous rotation; 1 full revolution per held item useAnimation-ish window
                        _weaponRotation += 0.28f;
                        if (_weaponRotation > MathHelper.TwoPi) _weaponRotation -= MathHelper.TwoPi;
                        break;
                    case ComboMotion.IaidoDraw:
                    {
                        var (a0, a1) = Endpoints(1.2f, -0.5f);
                        if (inTel) _weaponRotation = MathHelper.Lerp(_weaponRotation, a0, 0.15f); // weapon held low/behind
                        else       _weaponRotation = SwingEase.Apply(a0, a1, t, UseSwingEasing);   // fast snap forward
                        break;
                    }
                    case ComboMotion.GroundSlam:
                    {
                        var (a0, a1) = Endpoints(-1.55f, 1.5f);
                        if (inTel) _weaponRotation = MathHelper.Lerp(_weaponRotation, a0, 0.25f);
                        else       _weaponRotation = SwingEase.Apply(a0, a1, t, UseSwingEasing);
                        break;
                    }
                    case ComboMotion.LeapSlam:
                        // Wind up overhead, then carry the axe up-and-FORWARD (toward the player,
                        // ~1 o'clock facing right / ~11 facing left) through the airborne arc, and
                        // slam down hard as it descends / lands.
                        if (inTel)                    _weaponRotation = MathHelper.Lerp(_weaponRotation, -1.45f, 0.30f);
                        else if (NPC.velocity.Y < 0f) _weaponRotation = MathHelper.Lerp(_weaponRotation, -0.9f, 0.20f);
                        else                          _weaponRotation = MathHelper.Lerp(_weaponRotation, 1.4f, 0.22f);
                        break;
                    case ComboMotion.LeapThrust:
                        // Telegraph: dip the spear low-forward ("cocked" for the upcoming poke).
                        // Airborne rising: snap to horizontal — spear leveled at the player.
                        // Falling/landing: hold horizontal for the thrust contact moment.
                        if (inTel)                    _weaponRotation = MathHelper.Lerp(_weaponRotation, MathHelper.PiOver2 * 0.8f, 0.22f);
                        else if (NPC.velocity.Y < 0f) _weaponRotation = MathHelper.Lerp(_weaponRotation, MathHelper.PiOver4, 0.28f);
                        else                          _weaponRotation = MathHelper.Lerp(_weaponRotation, MathHelper.PiOver4, 0.50f);
                        break;
                    case ComboMotion.ChargeChop:
                        // Carry the axe cocked back/up while charging; the chop is the next step.
                        _weaponRotation = MathHelper.Lerp(_weaponRotation, -0.95f, 0.20f);
                        break;
                    case ComboMotion.Feint:
                        // Hold the raised "about to chop" bait pose; the real chop is the next step.
                        _weaponRotation = MathHelper.Lerp(_weaponRotation, -1.3f, 0.30f);
                        break;
                }
            }
            else if (_flight != null && _flight.IsDiving && MeleeWeaponItemType >= 0)
            {
                // Dive thrust pose: weapon held forward+down toward the player, locked.
                // PiOver4 (~45°) reads as a downward-forward stab when paired with the
                // dive's downward velocity and the puppet's Use3 forward-arm pose.
                _weaponRotation = MathHelper.Lerp(_weaponRotation, MathHelper.PiOver4, 0.35f);
            }
            else
            {
                // Idle / walking / jumping / recovery:
                // Ease the weapon back to the natural hold angle so it always looks carried.
                _weaponRotation = MathHelper.Lerp(_weaponRotation, HoldRotation, 0.10f);
            }

            UpdateSpearGrip();
            SpawnSwingVFX(_weaponRotation - _prevWeaponRotation);
            _prevWeaponRotation = _weaponRotation;

            if (SwingDebugLog && IsWeaponVisiblePhase)
                LogSwingFrame();
        }

        /// <summary>
        /// Slides the spear grip along the shaft so the weapon extends/retracts like the player's spear.
        /// _spearGrip 0 = gripped at the head (compact), 1 = gripped at the base (head thrust far forward).
        /// A poke pulses to the base and back (extend → retract); spins/swings hold it out for more reach.
        /// No-op unless <see cref="DrawWeaponAsSpear"/>.
        /// </summary>
        private void UpdateSpearGrip()
        {
            if (!DrawWeaponAsSpear) return;

            float target = 0.5f; // idle: gripped in the middle
            float ease = 0.25f;

            switch (Phase)
            {
                case AttackPhase.SpearTelegraph:
                    target = 0.4f; ease = 0.22f; // pull the head back a touch, ready to thrust
                    break;
                case AttackPhase.SpearAttack:
                {
                    float p = SpearAttackTicks > 0 ? 1f - (float)PhaseTimer / SpearAttackTicks : 1f;
                    target = MathHelper.Lerp(0.45f, 0.97f, (float)Math.Sin(p * Math.PI)); // extend then retract
                    ease = 0.55f;
                    break;
                }
                case AttackPhase.MeleeComboTelegraph:
                    target = 0.45f; ease = 0.22f;
                    break;
                case AttackPhase.MeleeComboPause:
                    target = 0.5f; ease = 0.25f;
                    break;
                case AttackPhase.RangedTelegraph:
                case AttackPhase.RangedAttack:
                case AttackPhase.CrossbowBurstPause:
                    // Ranged/throw poses that still use the spear texture (primary ranged casting
                    // off the same weapon) previously fell through to the neutral 0.5 grip, which
                    // puts the draw origin at the shaft's midpoint — the plain butt then sticks out
                    // exactly as far as the tip on the OPPOSITE side, through the body, reading as
                    // "the spear is pointing the wrong way." Pull the grip toward the base (like
                    // SpearAttack's extended pose) so the tip clearly leads toward the aim direction
                    // and the butt tucks in near the hand instead.
                    target = 0.85f; ease = 0.25f;
                    break;
                case AttackPhase.MeleeComboAttack:
                {
                    ComboMotion motion = _activeMeleeCombo.Steps[_meleeComboStepIndex].Motion;
                    float p = _weaponAnimMax > 0 ? 1f - (float)_weaponAnim / _weaponAnimMax : 1f;
                    if (motion == ComboMotion.Thrust || motion == ComboMotion.JoustDash || motion == ComboMotion.LeapThrust)
                    {
                        target = MathHelper.Lerp(0.45f, 0.97f, (float)Math.Sin(p * Math.PI)); // poke extend/retract
                        ease = 0.55f;
                    }
                    else if (motion == ComboMotion.Spin)
                    {
                        target = 0.9f; ease = 0.3f; // held out near the base so the spear sweeps wide
                    }
                    else
                    {
                        // Overhead / sweep / chop: extend through the swing, peaking at the apex.
                        target = MathHelper.Lerp(0.6f, 0.85f, (float)Math.Sin(p * Math.PI));
                        ease = 0.3f;
                    }
                    break;
                }
                default:
                    target = 0.5f; // idle / walking / ranged / etc.
                    break;
            }

            _spearGrip = MathHelper.Lerp(_spearGrip, target, ease);
        }

        /// <summary>
        /// Diagnostic: append one line per weapon-visible swing frame to
        /// <c>tsorcRevamp-invader-swing.log</c>.  Captures the values that drive both the manual
        /// (legacy 4-frame) and composite-arm swing so misaligned per-motion swings can be read
        /// directly: phase + combo motion, the swing progress <c>t</c>, <c>_weaponRotation</c> and
        /// the per-weapon draw offset, the resolved body row + hand offset (legacy path), and the
        /// composite-arm rotation/hand when the experiment is active.
        /// </summary>
        private void LogSwingFrame()
        {
            try
            {
                string sep = System.IO.Path.DirectorySeparatorChar.ToString();
                string dir = Main.SavePath + sep + "Logs";
                System.IO.Directory.CreateDirectory(dir);
                string path = dir + sep + "tsorcRevamp-invader-swing.log";

                string motion = "-";
                if (IsMeleeComboPhase && _activeMeleeComboIndex >= 0 && _activeMeleeCombo.Steps != null
                    && _meleeComboStepIndex >= 0 && _meleeComboStepIndex < _activeMeleeCombo.Steps.Length)
                {
                    var step = _activeMeleeCombo.Steps[_meleeComboStepIndex];
                    motion = $"{step.Motion}#{_meleeComboStepIndex}";
                }

                float t = _weaponAnimMax > 0 ? 1f - (float)_weaponAnim / _weaponAnimMax : 1f;
                int bodyRow = _puppet != null ? _puppet.bodyFrame.Y / FrameHeight : -1;
                Vector2 handOff = _puppet != null ? (GetHandPosition() - NPC.Center) : Vector2.Zero;
                bool composite = CompositeArmActive;
                float drawRot = _weaponRotation + (composite ? 0f : MeleeWeaponRotationOffset);

                string line = $"[{System.DateTime.Now:HH:mm:ss.fff}] {NPC.TypeName}#{NPC.whoAmI}"
                    + $" phase={Phase} motion={motion}"
                    + $" dir={NPC.direction} t={t:F2} anim={_weaponAnim}/{_weaponAnimMax}"
                    + $" weaponRot={_weaponRotation:F3} drawRot={drawRot:F3} offset={MeleeWeaponRotationOffset:F3}"
                    + $" bodyRow={bodyRow} handOff=({handOff.X:F1},{handOff.Y:F1})"
                    + $" composite={composite} compRot={(composite ? CompositeArmRotation : 0f):F3}";

                System.IO.File.AppendAllText(path, line + System.Environment.NewLine);
            }
            catch { }
        }

        /// <summary>
        /// Maps a weapon-draw angle to a Use1-Use4 body row via the same pitch formula
        /// <c>(1 - sin(angle)) / 2</c> used throughout this file (and in BroadswordRework's
        /// MeleeAnimation.cs, where it was calibrated).  Takes rotation/direction explicitly
        /// (rather than reading <see cref="_weaponRotation"/>/NPC.direction directly) so it can
        /// also pose a one-off echo/duplicate swinging independently of the invader's own state.
        /// </summary>
        private int BodyRowFromWeaponRotation(float weaponRotation, int direction)
        {
            float visualAngle = weaponRotation + MeleeWeaponRotationOffset * direction;
            float pitch = (1f - (float)Math.Sin(visualAngle)) / 2f;
            if (pitch > 0.95f) return 1;
            if (pitch > 0.70f) return 2;
            if (pitch > 0.30f) return 3;
            return 4;
        }

        /// <summary>
        /// Returns the world-space position of the front hand by reading the current body-frame
        /// row and applying the known arm-tip offsets from the vanilla player sprite sheet.
        ///
        /// These offsets were taken directly from <c>MeleeAnimation.cs</c> (BroadswordRework)
        /// where they were calibrated against the vanilla player sprite, so they exactly match
        /// where the rendered arm ends for each Use row.  Using a direct lookup here is more
        /// reliable than <c>Player.GetFrontHandPosition</c> on a puppet, because that API
        /// depends on composite arm state that the puppet draw path doesn't fully initialise.
        /// </summary>
        private Vector2 GetHandPosition()
        {
            if (_puppet == null)
                return NPC.Center;

            // ── Composite-arm experiment ───────────────────────────────────────────
            // When the new path is active the front arm is a continuously-rotated composite
            // arm, so the authoritative hand is whatever vanilla reports for that rotation —
            // not the 4-row offset table.  (This is the call the old comment flagged as
            // unreliable on a puppet; the experiment exists to find out whether the puppet is
            // sufficiently initialised here.  If it returns garbage, fall back to the table.)
            if (CompositeArmActive)
            {
                Vector2 composite = _puppet.GetFrontHandPosition(CompositeArmStretch, CompositeArmRotation);
                if (composite != Vector2.Zero)
                    return composite;
            }

            // Row of the body frame in the player sprite sheet (Use1=1 … Use4=4, walk/idle=other).
            int bodyRow = _puppet.bodyFrame.Y / FrameHeight;

            // Arm-tip offsets relative to NPC.Center, in direction-neutral space.
            // X is given for facing-right; multiply by NPC.direction for the actual side.
            Vector2 offset = bodyRow switch
            {
                1 => new Vector2(-8f, -9f),  // Use1 — arm fully raised, weapon overhead
                2 => new Vector2( 4f, -8f),  // Use2 — arm raised (ranged telegraph / ranged throw)
                3 => new Vector2( 4f,  2f),  // Use3 — arm level / forward (stab attack)
                4 => new Vector2( 4f,  7f),  // Use4 — arm dipped (stab telegraph)
                _ => new Vector2( 4f,  2f),  // fallback — level arm
            };

            return NPC.Center + new Vector2(offset.X * NPC.direction, offset.Y);
        }

        /// <summary>
        /// World-space unit direction the currently-drawn melee weapon sprite points, matching
        /// <see cref="DrawWeaponToLayer"/>'s actual render rotation exactly — including the
        /// per-weapon <see cref="MeleeWeaponRotationOffset"/> / <see cref="SpearDrawRotationOffset"/>
        /// corrections — so it's the single source of truth for "where the sprite actually is,"
        /// shared by decorative tip-position code (<see cref="GetSpearTipWorldPosition"/>) and real
        /// hit detection (<see cref="TickBladeHit"/>) rather than two approximations that could
        /// drift apart. Assumes the sprite is calibrated so the tip sits at -45° when
        /// _weaponRotation=0, dir=1 (the standard broadsword convention used across all invaders).
        /// </summary>
        private Vector2 GetWeaponWorldDirection()
        {
            float drawRotation = _weaponRotation;
            if (DrawWeaponAsSpear)
                drawRotation += SpearDrawRotationOffset;
            else
                drawRotation += MeleeWeaponRotationOffset * NPC.direction;

            const float correctedNaturalDeg = -45f;
            float rotDeg = MathHelper.ToDegrees(drawRotation);
            float angleDeg = NPC.direction == 1
                ? correctedNaturalDeg + rotDeg
                : 180f - (correctedNaturalDeg + rotDeg); // mirror across vertical axis (FlipHorizontally)
            float rad = MathHelper.ToRadians(angleDeg);
            return new Vector2((float)Math.Cos(rad), (float)Math.Sin(rad));
        }

        /// <summary>World-space position of the spear's TIP, derived from the current weapon-draw
        /// rotation.  Decorative use only (telegraph VFX placement) — not pixel-exact, but tracks the
        /// visible tip as the weapon swings/thrusts/aims.</summary>
        protected Vector2 GetSpearTipWorldPosition(float reachPx = 90f)
            => GetHandPosition() + GetWeaponWorldDirection() * reachPx;

        // ── Debug: weapon-rotation log (DebugMode config only) ──────────────────────
        // Dumps everything relevant to diagnosing hand placement / spear rotation bugs:
        // Phase, raw _weaponRotation, the final drawRotation actually passed to DrawData,
        // holdingSpearNow/heldRangedLike routing, held item type, spear grip, and positions.
        // Throttled per-NPC so a telegraph doesn't spam thousands of near-identical lines.
        private int _lastWeaponDebugLogTick = -9999;
        private void LogWeaponDebug()
        {
            int now = (int)Main.GameUpdateCount;
            if (now - _lastWeaponDebugLogTick < 6) return; // ~10/sec — enough resolution, not spam
            _lastWeaponDebugLogTick = now;
            try
            {
                string sep = Path.DirectorySeparatorChar.ToString();
                string dir = Main.SavePath + sep + "Logs";
                Directory.CreateDirectory(dir);
                string path = dir + sep + "tsorcRevamp-invader-weapon.log";
                // For combo phases, "phase=MeleeComboAttack" alone doesn't say WHICH named combo/motion
                // is playing (Charged Chop vs Forward Thrust vs Leaping Lunge, etc.) — every combo shares
                // the same phase enum, so without this the log can't tell them apart.
                string comboTag = "";
                bool inCombo = Phase == AttackPhase.MeleeComboTelegraph || Phase == AttackPhase.MeleeComboAttack
                            || Phase == AttackPhase.MeleeComboPause    || Phase == AttackPhase.MeleeComboRecovery;
                if (inCombo && _activeMeleeComboIndex >= 0 && _activeMeleeCombo.Steps != null && _meleeComboStepIndex < _activeMeleeCombo.Steps.Length)
                    comboTag = $" combo=\"{_activeMeleeCombo.Name}\" motion={_activeMeleeCombo.Steps[_meleeComboStepIndex].Motion}"
                             + $" step={_meleeComboStepIndex}/{_activeMeleeCombo.Steps.Length}";
                string line = $"[{DateTime.Now:HH:mm:ss}] {NPC.TypeName}#{NPC.whoAmI}"
                    + $" phase={Phase}{comboTag} phaseTimer={PhaseTimer}"
                    + $" heldItem={_heldItemType} holdingSpear={DebugHoldingSpearNow} rangedLike={DebugHeldRangedLike}"
                    + $" dir={DebugDirection} spearGrip={DebugSpearGrip:F2}"
                    + $" weaponRotDeg={DebugWeaponRotationDeg:F1} drawRotDeg={DebugDrawRotationDeg:F1}"
                    + $" hand=({DebugHandPos.X:F0},{DebugHandPos.Y:F0}) origin=({DebugOrigin.X:F0},{DebugOrigin.Y:F0})"
                    + $" npcPos=({NPC.Center.X:F0},{NPC.Center.Y:F0}) airborne={_flight?.IsAirborne ?? false}"
                    + $" flightMode={(_flight != null ? _flight.Mode.ToString() : "n/a")}";
                File.AppendAllText(path, line + Environment.NewLine);
            }
            catch { /* best-effort debug log; never let logging break gameplay */ }
        }

        // ─────────────────────────────────────────────────────────────────────────
        // Puppet
        // ─────────────────────────────────────────────────────────────────────────

        private Item GetCachedWeaponItem(int itemType)
        {
            if (itemType == MeleeWeaponItemType)
            {
                if (_cachedMeleeType != itemType)
                {
                    _meleeItemCache = new Item();
                    _meleeItemCache.SetDefaults(itemType);
                    _meleeItemCache.noUseGraphic = true; // arm animates; sprite is drawn manually
                    _cachedMeleeType = itemType;
                }
                return _meleeItemCache;
            }
            if (itemType == RangedWeaponItemType)
            {
                if (_cachedRangedType != itemType)
                {
                    _rangedItemCache = new Item();
                    _rangedItemCache.SetDefaults(itemType);
                    _rangedItemCache.noUseGraphic = true;
                    _cachedRangedType = itemType;
                }
                return _rangedItemCache;
            }
            if (itemType == SecondaryRangedWeaponItemType)
            {
                if (_cachedSecondaryRangedType != itemType)
                {
                    _secondaryRangedItemCache = new Item();
                    _secondaryRangedItemCache.SetDefaults(itemType);
                    _secondaryRangedItemCache.noUseGraphic = true;
                    _cachedSecondaryRangedType = itemType;
                }
                return _secondaryRangedItemCache;
            }
            if (itemType == MagicWeaponItemType)
            {
                if (_cachedMagicType != itemType)
                {
                    _magicItemCache = new Item();
                    _magicItemCache.SetDefaults(itemType);
                    _magicItemCache.noUseGraphic = true;
                    _cachedMagicType = itemType;
                }
                return _magicItemCache;
            }
            return new Item(); // air
        }

        private void InitPuppet()
        {
            _puppet = new Player();
            _puppet.active  = true;
            _puppet.whoAmI  = 0;
            _puppet.gravDir = 1f;
            _puppet.Male    = true;

            _puppet.armor[0] = new Item();  _puppet.armor[0].SetDefaults(HeadArmorItemType);
            _puppet.armor[1] = new Item();  _puppet.armor[1].SetDefaults(BodyArmorItemType);
            _puppet.armor[2] = new Item();  _puppet.armor[2].SetDefaults(LegsArmorItemType);

            // These equip-slot indices are what the renderer reads to pick armor textures
            _puppet.head = _puppet.armor[0].headSlot;
            _puppet.body = _puppet.armor[1].bodySlot;
            _puppet.legs = _puppet.armor[2].legSlot;

            // Wings — populated only when HasWings, so wing layer renders behind the body
            if (HasWings && WingsAccessoryItemType > 0)
            {
                _wingsItemCache = new Item();
                _wingsItemCache.SetDefaults(WingsAccessoryItemType);
                _cachedWingsType = WingsAccessoryItemType;
                _puppet.wings = _wingsItemCache.wingSlot;
                _puppet.wingTimeMax = 200; // arbitrary > 0 so wing layer treats it as a real wing
            }

            // Pre-set default weapon so it shows from the first frame
            _heldItemType = MeleeWeaponItemType >= 0 ? MeleeWeaponItemType : RangedWeaponItemType;
        }

        private void SyncPuppet()
        {
            _puppet.position  = NPC.position;
            _puppet.velocity  = NPC.velocity;
            _puppet.direction = NPC.direction;
            _puppet.width     = NPC.width;
            _puppet.height    = NPC.height;
            _puppet.gravDir   = 1f;

            // Wing flap state: drive vanilla wing draw layer's animation.
            // controlJump=true + wingTime>0 makes the wing layer pick the flap frames;
            // false + wingTime=0 makes it pick the glide/closed frame.
            if (HasWings && _flight != null)
            {
                bool airborne = _flight.IsAirborne;
                _puppet.wings = _wingsItemCache?.wingSlot ?? 0;
                _puppet.wingTime = airborne ? _puppet.wingTimeMax : 0;
                _puppet.controlJump = _flight.WingsActiveThisTick;
                // During idle hover drift the wings spread open (glide pose = frame 1) rather
                // than folding shut.  Active flapping and non-hover flight use the normal
                // animated phase so the wing-beat loop runs correctly.
                bool gliding = airborne && !_flight.WingsActiveThisTick
                    && (_flight.Mode == FlightMode.Hover || _flight.Mode == FlightMode.Strafe);
                _puppet.wingFrame = gliding ? 1 : (int)(_flight.WingAnimPhase * 4f) % 4;
            }
            else
            {
                _puppet.wings = 0;
                _puppet.wingTime = 0;
                _puppet.controlJump = false;
            }

            // Only real combat poses keep the weapon in hand; recovery, idle, healing,
            // and casual movement fall back to the natural arm/leg draw.
            bool inAttackPhase = IsWeaponPosePhase;
            _puppet.itemAnimationMax = _weaponAnimMax;
            _puppet.selectedItem     = 0;

            if (inAttackPhase)
            {
                // Put weapon in hand so the arm extends to the correct Use1–Use4 pose.
                // noUseGraphic=true prevents DrawPlayer from rendering the sprite itself —
                // InvaderWeaponDrawLayer handles that at the correct layer depth.
                _puppet.inventory[0]  = _heldItemType > 0 ? GetCachedWeaponItem(_heldItemType) : new Item();
                // Keep itemAnimation >= 1 so the arm stays in extended-hold pose between swings.
                _puppet.itemAnimation = Math.Max(_weaponAnim, 1);
                _puppet.itemRotation  = NPC.direction * _weaponRotation;
            }
            else
            {
                // Idle / casual stroll: no weapon in hand — arm hangs naturally in the
                // walk/idle pose without extending as if gripping something.
                _puppet.inventory[0]  = new Item();
                _puppet.itemAnimation = 0;
                _puppet.itemRotation  = 0f;
            }

            SyncFrames();

            // ── Composite-arm swing experiment ──────────────────────────────────────
            // Set AFTER SyncFrames so it overrides the front-arm portion of the chosen body
            // frame.  We bypass Player.PlayerFrame() (which would normally drive this), so the
            // value we set here persists straight into DrawPlayer.  Disabled → explicitly clear
            // it so a stale composite pose never leaks into the legacy 4-frame path.
            if (CompositeArmActive)
                _puppet.SetCompositeArmFront(true, CompositeArmStretch, CompositeArmRotation);
            else
                _puppet.SetCompositeArmFront(false, Player.CompositeArmStretchAmount.Full, 0f);
        }

        private void SyncFrames()
        {
            bool onGround = NPC.velocity.Y == 0f;
            bool moving   = Math.Abs(NPC.velocity.X) > 0.15f;

            // Always advance walk counter when moving on ground — legs animate
            // even during attacks so the invader doesn't glide with frozen feet.
            // Guard against Main.gamePaused so the animation freezes with every other NPC.
            if (onGround && moving && !Main.gamePaused)
            {
                _frameCounter += Math.Abs(NPC.velocity.X) * 0.55f;
                if (_frameCounter >= 14f) _frameCounter = 0f;
            }

            // ── Body frame ────────────────────────────────────────────────────────
            // Attack phases drive the upper body (Use1–Use4).  The arm pose tracks
            // the weapon angle via the pitch formula so the shoulder stays consistent
            // with the visual weapon rotation throughout the full swing arc.
            //   Use1 (row 1) = arm fully raised (weapon behind/above head, -1.3 rad)
            //   Use2 (row 2) = arm raised
            //   Use3 (row 3) = arm level / forward
            //   Use4 (row 4) = arm lowered  (weapon pointing down-forward, +1.0 rad)
            // Pitch formula: (1 - sin(weaponAngle)) / 2  →  1 = up, 0 = down.

            int bodyRow;
            bool isMeleeSwing = Phase == AttackPhase.MeleeTelegraph || Phase == AttackPhase.MeleeAttack;

            if (Phase == AttackPhase.Healing)
            {
                // Arm raised to drink — Use2 matches the "arm held up" pose
                bodyRow = 2;
            }
            else if (isMeleeSwing)
            {
                bodyRow = BodyRowFromWeaponRotation(_weaponRotation, NPC.direction);
            }
            else if (Phase == AttackPhase.StabTelegraph)
            {
                bodyRow = 4; // Use4 — arm dipped down to match sword dip telegraph
            }
            else if (Phase == AttackPhase.StabAttack)
            {
                bodyRow = 3; // Use3 — arm level/forward for the horizontal thrust
            }
            else if (Phase == AttackPhase.RangedTelegraph)
            {
                // Body row depends on animation style so the arm matches the weapon arc.
                bodyRow = _activeRangedStyle switch
                {
                    RangedStyle.Crossbow => 3, // Use3 — arm level/forward for horizontal aim
                    RangedStyle.Bow      =>    // Bow draws: arm rises from Use3 → Use2 → Use1 over the telegraph
                        PhaseTimer > _activeRangedTelegraphTicks * 0.60f ? 3 :
                        PhaseTimer > _activeRangedTelegraphTicks * 0.25f ? 2 : 1,
                    _                    => 2, // Throw default — raised arm
                };
            }
            else if (Phase == AttackPhase.RangedAttack)
            {
                bodyRow = _activeRangedStyle switch
                {
                    RangedStyle.Crossbow => 3, // Use3 — arm stays level, small click jolt
                    RangedStyle.Bow      => 3, // Use3 — arm snaps forward at release
                    _                    => 3, // Throw — arm forward at release
                };
            }
            else if (Phase == AttackPhase.CrossbowBurstPause)
            {
                bodyRow = 3; // Use3 — arm level/forward, holding aim during inter-shot pause
            }
            else if (Phase == AttackPhase.SpearTelegraph)
            {
                bodyRow = 4; // Use4 — arm dipped, spear angled down in "ready to poke" read
            }
            else if (Phase == AttackPhase.SpearAttack)
            {
                bodyRow = 3; // Use3 — arm level/forward for the reach poke
            }
            else if (Phase == AttackPhase.MagicTelegraph)
            {
                // Arm rises overhead as the charge builds.
                bodyRow = PhaseTimer > MagicTelegraphTicks * 0.50f ? 2 : 1;
            }
            else if (Phase == AttackPhase.MagicAttack)
            {
                bodyRow = 3; // Use3 — arm thrusts forward as the spell fires
            }
            else if (Phase == AttackPhase.KnivesTelegraph || Phase == AttackPhase.KnivesThrowPause)
            {
                bodyRow = 2; // Use2 — arm raised, knife held ready
            }
            else if (Phase == AttackPhase.KnivesThrow)
            {
                bodyRow = 3; // Use3 — arm forward at release
            }
            else if (Phase == AttackPhase.MeleeComboTelegraph
                  || Phase == AttackPhase.MeleeComboAttack
                  || Phase == AttackPhase.MeleeComboPause)
            {
                // Body row depends on the current step's motion to keep the arm pose
                // aligned with the weapon-rotation lerp from TickWeaponAnim.
                bool inTel = Phase == AttackPhase.MeleeComboTelegraph;
                var motion = _activeMeleeCombo.Steps[_meleeComboStepIndex].Motion;
                switch (motion)
                {
                    case ComboMotion.OverheadArc:
                    case ComboMotion.VerticalChop:
                    case ComboMotion.GroundSlam:
                    case ComboMotion.UnderhandArc:
                    case ComboMotion.HorizontalSweep:
                    case ComboMotion.LeapSlam:
                    case ComboMotion.ChargeChop:
                    case ComboMotion.Feint:
                        bodyRow = BodyRowFromWeaponRotation(_weaponRotation, NPC.direction);
                        break;
                    case ComboMotion.Thrust:
                    case ComboMotion.JoustDash:
                    case ComboMotion.LeapThrust:
                        bodyRow = inTel ? 4 : 3;
                        break;
                    case ComboMotion.Spin:
                        // Rotate body row to suggest motion; cycle Use1→Use3 by rotation phase
                        bodyRow = 1 + ((int)(_weaponRotation / MathHelper.PiOver2) & 3);
                        if (bodyRow > 4) bodyRow = 4;
                        break;
                    case ComboMotion.IaidoDraw:
                        bodyRow = inTel ? 4 : 3;
                        break;
                    default:
                        bodyRow = 3;
                        break;
                }
            }
            else if (_flight != null && _flight.IsDiving && MeleeWeaponItemType >= 0)
            {
                // Dive: arm forward, weapon thrusting toward the dive target.
                bodyRow = 3; // Use3 — arm level/forward
            }
            else if (!onGround)
            {
                bodyRow = 5; // Jump
            }
            else if (moving)
            {
                bodyRow = 6 + (int)_frameCounter; // Walk1–Walk14
            }
            else
            {
                bodyRow = 0; // Idle
            }

            // ── Leg frame ─────────────────────────────────────────────────────────
            // Legs ALWAYS follow movement — never locked to the attack state.
            // The body (Use) frames carry the attack animation; the legs stay natural.
            int legRow;
            if (!onGround)
                legRow = 5; // Jump
            else if (moving)
                legRow = 6 + (int)_frameCounter; // Walk1–Walk14 (shared counter with body)
            else
                legRow = 0; // Idle

            _puppet.bodyFrame = new Rectangle(0, FrameHeight * bodyRow, 40, FrameHeight);
            _puppet.legFrame  = new Rectangle(0, FrameHeight * legRow,  40, FrameHeight);
        }

        // ─────────────────────────────────────────────────────────────────────────
        // Draw
        // ─────────────────────────────────────────────────────────────────────────

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (_puppet == null)
                InitPuppet();

            SyncPuppet();

            // Store draw color so InvaderWeaponDrawLayer can read it during the pipeline below.
            _layerDrawColor = drawColor;

            // InvaderWeaponDrawLayer is registered at AfterParent(HeldItem) in tModLoader's pipeline.
            // By setting DrawingPuppetFor = this for exactly the duration of DrawPlayer, that layer
            // wakes up and calls DrawWeaponToLayer — inserting the weapon sprite after body/legs
            // but before the front arm, which gives the correct "hand gripping the sword" look.
            DrawingPuppetFor = this;

            // Dash afterimages: sparse translucent echoes at cached NPC.oldPos[] positions, drawn
            // BEFORE the real puppet so the solid sprite always renders on top.  Uses vanilla's own
            // fractal-afterimage fields (see AfterimageTicks doc) — no bespoke alpha-blend plumbing.
            if (AfterimageTicks > 0)
            {
                _puppet.isFirstFractalAfterImage = true;
                for (int k = 0; k < NPC.oldPos.Length; k += AfterimageSampleStep)
                {
                    if (NPC.oldPos[k] == Vector2.Zero) continue;
                    _puppet.firstFractalAfterImageOpacity = AfterimageOpacity * (1f - (float)k / NPC.oldPos.Length);
                    Main.PlayerRenderer.DrawPlayer(Main.Camera, _puppet, NPC.oldPos[k], 0f, Vector2.Zero);
                }
                _puppet.isFirstFractalAfterImage = false;
            }

            Main.PlayerRenderer.DrawPlayer(Main.Camera, _puppet, NPC.position, 0f, Vector2.Zero);
            DrawingPuppetFor = null;

            // Umbral Echo Step: drawn AFTER the real puppet (and outside DrawingPuppetFor, since
            // InvaderWeaponDrawLayer reads the invader's LIVE state, not this fixed echo point) so it
            // never clobbers the real draw and its weapon renders in the right place.
            if (IsEchoStepSwinging)
                DrawEchoStepPuppet(spriteBatch);

            return false;
        }

        /// <summary>
        /// Called by <see cref="InvaderWeaponDrawLayer"/> during the <c>DrawPlayer</c> pipeline.
        /// Adds the weapon sprite to <paramref name="drawInfo"/>'s draw-data cache at the correct
        /// layer depth — after body/legs but before the front arm — so the hand appears to grip it.
        /// </summary>
        /// <summary>
        /// Draws the off-hand shield sprite on the puppet (no-op without a shield).  Held lowered at
        /// the back hip in neutral, raised in front of the chest while guarding.  Called by
        /// <see cref="InvaderWeaponDrawLayer"/> just before the weapon so the front arm/weapon layer
        /// over it.  Override <see cref="ShieldItemType"/> to pick the shield sprite.
        /// </summary>
        internal void DrawShieldToLayer(ref PlayerDrawSet drawInfo)
        {
            if (!HasShield || _puppet == null
                || Phase == AttackPhase.Healing || Phase == AttackPhase.FleeToHeal)
                return;

            if (!_shieldDrawTexLoaded)
            {
                _shieldDrawTexLoaded = true;
                string path = ShieldDrawTexturePath;
                if (!string.IsNullOrEmpty(path) && ModContent.HasAsset(path))
                    _shieldDrawTex = ModContent.Request<Texture2D>(path,
                        ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
            }
            if (_shieldDrawTex == null)
                return;

            // The strip shares the player body's 20-frame layout: index it by the current puppet
            // body row so the shield pose tracks the animation, and draw it body-aligned so it
            // overlays the body 1:1 (each frame already positions the shield for that pose).
            const int shieldFrames = 20;
            int row    = Math.Clamp(_puppet.bodyFrame.Y / FrameHeight, 0, shieldFrames - 1);
            int frameH = _shieldDrawTex.Height / shieldFrames;
            Rectangle src = new Rectangle(0, row * frameH, _shieldDrawTex.Width, frameH);

            Vector2 origin  = new Vector2(_shieldDrawTex.Width / 2f, frameH / 2f);
            Vector2 drawPos = new Vector2(
                NPC.position.X + NPC.width / 2f - _shieldDrawTex.Width / 2f,
                NPC.position.Y + NPC.height - frameH + 4f) - Main.screenPosition + origin;
            SpriteEffects fx = NPC.direction == -1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

            drawInfo.DrawDataCache.Add(new DrawData(
                _shieldDrawTex, drawPos, src, _layerDrawColor, 0f, origin, NPC.scale, fx, 0));
        }

        private static Texture2D _slashVFXTex;
        private static bool _slashVFXTexLoadAttempted;

        /// <summary>
        /// Draws an arc-shaped slash swoosh along the same angle/reach <see cref="TickBladeHit"/>
        /// uses for real hit detection (<see cref="GetWeaponWorldDirection"/> + the active blade
        /// reach) — an honest preview of the actual hitbox rather than disconnected decoration.
        /// Reuses the generic Slash.png frame-strip asset from the player-weapon BroadswordRework
        /// system (same visual language as the real player slash VFX). No-op unless
        /// <see cref="HasSlashVFX"/> and a trackable swing is actually live.
        /// </summary>
        internal void DrawSlashToLayer(ref PlayerDrawSet drawInfo)
        {
            if (!HasSlashVFX || !_bladeArmed || _activeBladeReach <= 0f)
                return;
            if (Phase != AttackPhase.MeleeAttack && Phase != AttackPhase.StabAttack
                && Phase != AttackPhase.SpearAttack && Phase != AttackPhase.MeleeComboAttack)
                return;

            if (!_slashVFXTexLoadAttempted)
            {
                _slashVFXTexLoadAttempted = true;
                const string path = "tsorcRevamp/Items/Weapons/Melee/Broadswords/BroadswordRework/Common/Melee/Slash";
                if (ModContent.HasAsset(path))
                    _slashVFXTex = ModContent.Request<Texture2D>(path, ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
            }
            if (_slashVFXTex == null)
                return;

            float t = _weaponAnimMax > 0 ? 1f - (float)_weaponAnim / _weaponAnimMax : 1f;

            var frame = new SpriteFrame(1, 3) { CurrentRow = (byte)Math.Min(2, (int)(t * 3f)) };

            Vector2 direction = GetWeaponWorldDirection();
            float rotation = direction.ToRotation();
            Vector2 position = GetHandPosition() + direction * 2f;
            Rectangle sourceRectangle = frame.GetSourceRectangle(_slashVFXTex);
            Vector2 origin = sourceRectangle.Size() * 0.5f;
            float scale = _activeBladeReach / 30f;
            bool flippedSwing = UseAlternateFlip && _comboSwingFlipped;
            SpriteEffects fx = (NPC.direction > 0) ^ flippedSwing ? SpriteEffects.FlipVertically : SpriteEffects.None;

            const float MaxAlpha = 0.4f;
            var alphaGradient = new Gradient<float>(
                (0.00f, 0f),
                (0.25f, MaxAlpha),
                (0.75f, MaxAlpha),
                (1.00f, 0f)
            );
            Color color = Lighting.GetColor(position.ToTileCoordinates()).MultiplyRGB(SlashVFXColor) * alphaGradient.GetValue(t);

            drawInfo.DrawDataCache.Add(new DrawData(
                _slashVFXTex, position - Main.screenPosition, sourceRectangle, color, rotation, origin, scale, fx, 0));
        }

        internal void DrawWeaponToLayer(ref PlayerDrawSet drawInfo)
        {
            // Healing: draw the estus flask instead of the combat weapon.
            if (Phase == AttackPhase.Healing)
            {
                DrawEstusFlaskToLayer(ref drawInfo);
                return;
            }

            if (Phase == AttackPhase.FleeToHeal || !_weaponVisible || _heldItemType <= 0)
                return;

            // Flails (and similar) render their own projectile visual (ball + chain), so the held
            // item icon shouldn't be drawn in the hand at all.
            if (HideHeldMeleeSprite && _heldItemType == MeleeWeaponItemType)
                return;

            // Only draw as spear when actually holding the polearm — not the magic staff, secondary ranged,
            // or cursed knives (those are separate weapons that use normal centred draw).
            bool holdingSpearNow = DrawWeaponAsSpear
                && _heldItemType != MagicWeaponItemType
                && _heldItemType != SecondaryRangedWeaponItemType
                && (CursedKnivesWeaponItemType < 0 || _heldItemType != CursedKnivesWeaponItemType);

            // When drawing as a spear, prefer the holdout-projectile texture (the full shaft+head sprite)
            // over the item icon (which is just a small inventory tile).
            Texture2D tex;
            if (holdingSpearNow && SpearDrawTexturePath != null && ModContent.HasAsset(SpearDrawTexturePath))
            {
                tex = ModContent.Request<Texture2D>(SpearDrawTexturePath,
                    ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
            }
            else
            {
                var texAsset = TextureAssets.Item[_heldItemType];
                if (texAsset?.Value == null) return;
                tex = texAsset.Value;
            }
            // A weapon that doubles as the melee weapon (e.g. a spear that also casts the primary-ranged
            // projectile) draws as MELEE — handle-anchored at full melee scale — not as a centred thrown item.
            bool heldRangedLike = (_heldItemType == RangedWeaponItemType && RangedWeaponItemType != MeleeWeaponItemType)
                               || _heldItemType == SecondaryRangedWeaponItemType
                               || _heldItemType == MagicWeaponItemType;
            bool heldCrossbowLike = _heldItemType == _activeRangedItemType
                                 && _activeRangedStyle == RangedStyle.Crossbow;
            float scale = heldCrossbowLike ? 0.8f : (heldRangedLike ? GetHeldRangedDrawScale(_heldItemType) : MeleeWeaponDrawScale);
            Vector2 drawPos = GetHandPosition() - Main.screenPosition;
            if (heldCrossbowLike)
            {
                Item heldItem = GetCachedWeaponItem(_heldItemType);
                Vector2? holdoutOffset = heldItem.ModItem?.HoldoutOffset();
                if (holdoutOffset.HasValue)
                {
                    drawPos += new Vector2(holdoutOffset.Value.X * NPC.direction, holdoutOffset.Value.Y) * NPC.scale;
                }
            }

            // ── Origin: anchor the HANDLE (not centre) at the animated hand position ──
            //
            // Terraria sword sprites run diagonally in their texture — handle at lower-left,
            // blade tip at upper-right.  We normalise that corner as MeleeHandleNorm (0.10, 0.85).
            //
            // With SpriteEffects.FlipHorizontally the texture is mirrored, but the origin
            // parameter remains in pre-flip texture space.  The pixel rendered at drawPos is
            // the one whose mirrored X equals originX, i.e. originalX = texWidth − originX.
            // Mirroring origin.X keeps the handle pixel at the hand for both facing directions:
            //   Right (no flip) → originX = width * handleNorm.X        (lower-left corner)
            //   Left  (flip)    → originX = width * (1 − handleNorm.X)  (lower-right, which
            //                     after the flip maps to the handle side)
            Vector2 origin;
            if (heldCrossbowLike)
            {
                float hx = NPC.direction == 1 ? tex.Width * 0.22f : tex.Width * 0.78f;
                origin = new Vector2(hx, tex.Height * 0.58f);
            }
            else if (heldRangedLike)
            {
                if (_heldItemType == MagicWeaponItemType)
                {
                    // Staves grip lower on the shaft (not centred) so the hand holds near the base.
                    Vector2 mg = MagicGripNorm;
                    float mgx = NPC.direction == 1 ? tex.Width * mg.X : tex.Width * (1f - mg.X);
                    origin = new Vector2(mgx, tex.Height * mg.Y);
                }
                else
                {
                    origin = tex.Bounds.Center.ToVector2(); // symmetric throwing items - keep centred
                }
            }
            else if (holdingSpearNow)
            {
                // Grip slides along the shaft (head→base) so the spear extends/retracts.
                Vector2 gn = Vector2.Lerp(SpearHeadNorm, SpearBaseNorm, _spearGrip);
                float gx = NPC.direction == 1 ? tex.Width * gn.X : tex.Width * (1f - gn.X);
                origin = new Vector2(gx, tex.Height * gn.Y);
            }
            else
            {
                Vector2 hn = MeleeHandleNorm;
                float   hx = NPC.direction == 1
                    ? tex.Width * hn.X
                    : tex.Width * (1f - hn.X);
                origin = new Vector2(hx, tex.Height * hn.Y);
            }

            SpriteEffects fx = NPC.direction == -1
                ? SpriteEffects.FlipHorizontally
                : SpriteEffects.None;

            // Per-weapon angular correction for melee sprites whose blade/head diagonal doesn't
            // match the broadsword convention (handle lower-left → blade upper-right).  Applied to
            // the drawn sprite only — the arm pose and swing arc still run off raw _weaponRotation.
            float drawRotation = _weaponRotation;
            // Spears use their own SpearDrawRotationOffset to correct for the sprite's natural
            // orientation — MeleeWeaponRotationOffset is a sword-only fine-tune and would double
            // up with (and fight) that correction, so it's excluded here.
            if (!heldRangedLike && !holdingSpearNow)
                drawRotation += MeleeWeaponRotationOffset * NPC.direction; // mirror per facing
            if (holdingSpearNow)
                drawRotation += SpearDrawRotationOffset; // draw-only correction, direction-neutral (FlipH handles facing)

            drawInfo.DrawDataCache.Add(new DrawData(
                tex,
                drawPos,
                null,
                _layerDrawColor,
                drawRotation,
                origin,
                NPC.scale * scale,
                fx,
                0));

            // ── Debug snapshot + log (DebugMode only) ───────────────────────────────
            DebugWeaponRotationDeg = MathHelper.ToDegrees(_weaponRotation);
            DebugDrawRotationDeg   = MathHelper.ToDegrees(drawRotation);
            DebugHoldingSpearNow   = holdingSpearNow;
            DebugHeldRangedLike    = heldRangedLike;
            DebugHeldItemType      = _heldItemType;
            DebugSpearGrip         = _spearGrip;
            DebugHandPos           = drawPos + Main.screenPosition;
            DebugOrigin            = origin;
            DebugDirection         = NPC.direction;
            if (ModContent.GetInstance<tsorcRevampConfig>().DebugMode)
                LogWeaponDebug();

            // Lit-fuse: red sparks off the top of an in-hand bomb-like ranged item.
            if (heldRangedLike && HeldRangedFuseSparks(_heldItemType) && !Main.dedServ && Main.rand.NextBool(3))
            {
                Vector2 topWorld = GetHandPosition() - new Vector2(0f, tex.Height * NPC.scale * scale * 0.5f);
                Dust spark = Dust.NewDustPerfect(topWorld, DustID.RedTorch,
                    new Vector2(Main.rand.NextFloat(-0.5f, 0.5f), Main.rand.NextFloat(-1.2f, -0.3f)),
                    0, default, Main.rand.NextFloat(0.6f, 1.1f));
                spark.noGravity = true;
            }
        }

        /// <summary>
        /// Draws the estus flask sprite at the hand during the Healing animation phase.
        /// Reuses the same <c>EstusFlask_drinking</c> texture the player uses, centred on
        /// the Use2 hand position and rotated to look like it's being raised to the mouth.
        /// </summary>
        private void DrawEstusFlaskToLayer(ref PlayerDrawSet drawInfo)
        {
            var textures = TransparentTextureHandler.TransparentTextures;
            var key      = TransparentTextureHandler.TransparentTextureType.EstusFlask;
            if (!textures.TryGetValue(key, out Texture2D tex) || tex == null)
                return;

            // The drinking texture is a 3-frame vertical strip (full / half / near-empty).
            int   frameCount  = 3;
            int   frameHeight = tex.Height / frameCount;
            // Pick frame based on remaining charges.
            int frame = _estusCharges >= (int)(EstusChargesMax * 0.6f) ? 0
                      : _estusCharges >= (int)(EstusChargesMax * 0.3f) ? 1
                      : 2;
            Rectangle src = new Rectangle(0, frame * frameHeight, tex.Width, frameHeight);

            // Position: Use2 hand offset is (4, -8) from NPC.Center, scaled by direction.
            // Add a small nudge toward the mouth (a few pixels up).
            Vector2 handWorld = NPC.Center + new Vector2(10f * NPC.direction, -14f);
            Vector2 drawPos   = handWorld - Main.screenPosition;
            Vector2 origin    = new Vector2(src.Width / 2f, src.Height / 2f);

            // Rotate the flask so it looks like it's tipped toward the mouth.
            // -π/2 (= −90°) points the sprite's "top" to the right for a right-facing invader.
            float rotation = MathHelper.PiOver2 * -NPC.direction;

            SpriteEffects fx = NPC.direction == -1
                ? SpriteEffects.FlipHorizontally
                : SpriteEffects.None;

            drawInfo.DrawDataCache.Add(new DrawData(tex, drawPos, src,
                _layerDrawColor * 0.85f, rotation, origin, 0.75f, fx, 0));
        }

        /// <summary>
        /// Normalised (0–1) handle position within the melee-weapon texture.
        /// Default (0.10, 0.85) anchors near the lower-left corner — correct for most
        /// Terraria broadsword / katana sprites whose grip occupies that region.
        /// Override in a subclass to fine-tune the grip point for a specific weapon.
        /// </summary>
        protected virtual Vector2 MeleeHandleNorm => new Vector2(0.10f, 0.85f);

        // ── Per-weapon melee draw tuning ────────────────────────────────────────────
        /// <summary>Draw scale for the melee weapon sprite.  Default 1f (full size); override per
        /// invader if a particular sprite should read larger or smaller.</summary>
        protected virtual float MeleeWeaponDrawScale => 1f;

        // ── Spear-style draw (grip slides along the shaft = extend/retract like the player's spear) ──
        /// <summary>When true, the melee weapon is drawn as a SPEAR: the grip point slides along the shaft
        /// (<see cref="_spearGrip"/>) so pokes thrust the head forward and retract, and swings/spins hold it
        /// out for reach — instead of the static handle-anchored sword draw.</summary>
        protected virtual bool DrawWeaponAsSpear => false;
        /// <summary>Normalized texture coords of the spear's HEAD tip (the pointy end).  Tune to the sprite.</summary>
        protected virtual Vector2 SpearHeadNorm => new Vector2(0.82f, 0.18f);
        /// <summary>Normalized texture coords of the spear's BASE (butt of the shaft).  Tune to the sprite.</summary>
        protected virtual Vector2 SpearBaseNorm => new Vector2(0.12f, 0.88f);
        /// <summary>When <see cref="DrawWeaponAsSpear"/> is true, use this texture instead of the item icon.
        /// The item sprite is a small inventory icon; the spear's holdout-projectile sprite is the full
        /// shaft-and-head weapon the player sees.  Override to point at the projectile texture path.
        /// Null (default) falls back to the item sprite.</summary>
        protected virtual string SpearDrawTexturePath => null;
        /// <summary>Extra draw-only rotation applied after <see cref="MeleeWeaponRotationOffset"/> when drawing the spear.
        /// Use this to correct a sprite whose tip isn't in the standard orientation without affecting the arm-pose rows
        /// (which read <see cref="MeleeWeaponRotationOffset"/> via <see cref="BodyRowFromWeaponRotation"/>).
        /// Direction-neutral — SpriteBatch FlipHorizontally already handles left/right facing.</summary>
        protected virtual float SpearDrawRotationOffset => 0f;

        /// <summary>Normalized grip point for a held MAGIC staff (where the hand holds it).  Default centred;
        /// override lower (larger Y) so a tall staff is gripped near its base.</summary>
        protected virtual Vector2 MagicGripNorm => new Vector2(0.5f, 0.5f);

        /// <summary>When true, the held melee item icon is NOT drawn in the hand — for weapons whose
        /// visual is a separate projectile (e.g. a flail's ball + chain).  Default false.</summary>
        protected virtual bool HideHeldMeleeSprite => false;

        /// <summary>Draw scale for a held ranged/thrown/magic item.  Default 1f (full size); override
        /// per item type for sprites that should read smaller (e.g. tiny throwing stars).</summary>
        protected virtual float GetHeldRangedDrawScale(int itemType) => 1f;

        /// <summary>When true for the given held ranged item type, red "lit fuse" sparks are emitted
        /// off the top of it while it's in hand (e.g. a smoke bomb).  Default false.</summary>
        protected virtual bool HeldRangedFuseSparks(int itemType) => false;

        /// <summary>Per-weapon angular correction (radians) added to the drawn melee sprite ONLY
        /// (it does not change the arm pose or the swing arc).  Use it when a weapon texture's
        /// diagonal doesn't follow the broadsword convention this system assumes
        /// (handle lower-left → blade upper-right) — e.g. an axe whose head sits on the
        /// opposite diagonal and would otherwise appear to swing backwards.</summary>
        protected virtual float MeleeWeaponRotationOffset => 0f;

        // ── Composite-arm swing experiment (opt-in, single-enemy safe A/B) ──────────
        /// <summary>EXPERIMENTAL.  When true (and <see cref="CompositeArmSwingMasterEnable"/> is on),
        /// this invader's melee swings drive the puppet's vanilla composite FRONT arm so the arm
        /// rotates continuously with the blade — a genuine player-style swing — instead of the
        /// 4-frame Use1–Use4 approximation.  Opt-in per subclass so it can be tested on one enemy
        /// before any wider rollout.  See the notes in <c>EnemySpriteRenderer</c> for why the
        /// project previously avoided composite arms (rotation-convention mismatch + unreliable
        /// <c>GetFrontHandPosition</c> on a puppet) — both are what this experiment validates.</summary>
        protected virtual bool UseCompositeArmSwing => false;

        /// <summary>Runtime master kill-switch for the composite-arm experiment.  Lets you flip the
        /// new arm path off globally (e.g. from a debug command) for instant A/B without a rebuild.</summary>
        internal static bool CompositeArmSwingMasterEnable = true;

        /// <summary>Tunable: radians added to <c>_weaponRotation</c> before it drives the composite arm.
        /// Static so it can be nudged live while comparing against the legacy path.</summary>
        internal static float CompositeArmRotationOffset = 0f;

        /// <summary>Tunable: how far the composite front arm extends from the shoulder.</summary>
        internal static Player.CompositeArmStretchAmount CompositeArmStretch = Player.CompositeArmStretchAmount.Full;

        /// <summary>When true, every weapon-visible swing frame is written to
        /// <c>tsorcRevamp-invader-swing.log</c> for diagnosing arm/weapon alignment.
        /// Toggle in-game with <c>/swingarm log</c>.</summary>
        internal static bool SwingDebugLog = false;

        /// <summary>Phases whose <c>_weaponRotation</c> represents an actual swinging-arm motion
        /// (as opposed to a held-aim pose).  Only these drive the composite arm experiment.</summary>
        private bool IsMeleeSwingPosePhase =>
            Phase == AttackPhase.MeleeTelegraph || Phase == AttackPhase.MeleeAttack ||
            Phase == AttackPhase.MeleeComboTelegraph || Phase == AttackPhase.MeleeComboAttack ||
            Phase == AttackPhase.MeleeComboPause;

        /// <summary>True when the composite-arm swing path should be active this frame.</summary>
        private bool CompositeArmActive =>
            UseCompositeArmSwing && CompositeArmSwingMasterEnable && IsMeleeSwingPosePhase;

        /// <summary>Rotation handed to the composite arm / <c>GetFrontHandPosition</c>.  Mirrored by
        /// facing so the arm swings symmetrically on both sides.</summary>
        private float CompositeArmRotation =>
            (_weaponRotation + CompositeArmRotationOffset) * NPC.direction;
    }
}

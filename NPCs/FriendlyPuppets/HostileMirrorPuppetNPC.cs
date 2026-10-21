using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace tsorcRevamp.NPCs.FriendlyPuppets
{
    /// <summary>
    /// The hostile layer on top of <see cref="MirrorPuppetNPC"/>: the mirror invader's damage path (design §12.3–12.4).
    /// The attack is still 100% the player's weapon run through the swap; this layer only redirects who it hurts.
    ///
    ///   Projectiles — MirrorPuppetProjectile flips everything this puppet's swap spawns to hostile (synced through its
    ///                 extra AI), so vanilla tests every client's own player against the real projectile.
    ///   Melee       — the blade as DRAWN (grip→tip), swept across each tick's Strike-segment runs of the eased clock
    ///                 (MirrorPuppetNPC.StrikeSweep*), is tested against the host's own player here and streamed to the other clients
    ///                 in the owner-state extras, where each tests ITS own player (victim-authoritative).
    ///   Vanilla     — the borrowed ItemCheck lands no NPC or PvP hits (<see cref="SwapHitsAsOwner"/> = false).
    ///   Damage      — base weapon damage (item damage incl. prefix, none of the host's bonuses), reduced by the victim's
    ///                 defense, then capped at 15–25% of the victim's max HP by weapon weight (HostileMirrorPuppetPlayer).
    /// </summary>
    public abstract class HostileMirrorPuppetNPC : MirrorPuppetNPC
    {
        /// <summary>Per-hit cap (§12.4): 15% of the victim's max HP for a light weapon, up to 25% for a heavy one.</summary>
        internal const float HitCapLightFraction = 0.15f;
        internal const float HitCapHeavyExtraFraction = 0.10f;

        /// <summary>Weight from the weapon's native use time: 10 ticks (dagger) = 0, 40 ticks (greatsword) = 1.</summary>
        internal const float WeightLightUseTime = 10f;
        internal const float WeightHeavyUseTime = 40f;

        /// <summary>How long the last strike hitbox stays readable for debug drawing (test rig), in ticks.</summary>
        internal const int StrikeDebugShowTicks = 6;

        public override void SetDefaults()
        {
            base.SetDefaults();

            NPC.friendly = false;
        }

        // Hostile puppets swing on the eased clock (§12.6) so every melee strike is rollable.
        protected override bool UsesEasedMeleeClock => true;

        internal override bool SwapHitsAsOwner => false;

        // Item speed, not the host's: a host change must not change how fast it swings (§12.2).
        internal override bool UsesOwnerAttackSpeed => false;

        internal override void ModifySwapWeaponDamage(Item item, Player owner, ref StatModifier damage)
        {
            // Base weapon damage: drop the host's class/armor bonuses so a host change doesn't change how hard it hits.
            // Hooks that run after this one (later GlobalItems) can still add item-specific modifiers.
            damage = StatModifier.Default;
        }

        /// <summary>Cap fraction for hits from <paramref name="weapon"/>, from item stats only (useAnimation, not the
        /// host's attack speed). Callers: projectile tagging (MirrorPuppetProjectile) and the melee strike below.</summary>
        internal static float GetHitCapFraction(Item weapon)
        {
            if (weapon == null || weapon.IsAir)
            {
                return HitCapLightFraction;
            }

            float weight = MathHelper.Clamp((weapon.useAnimation - WeightLightUseTime) / (WeightHeavyUseTime - WeightLightUseTime), 0f, 1f);
            return HitCapLightFraction + HitCapHeavyExtraFraction * weight;
        }

        // ── Melee strike: host computes it, every client tests its own player ────────────────────────

        /// <summary>Half-thickness-ish of the blade line for hit tests (px), so a blade grazing the hitbox edge counts.</summary>
        internal const float BladeHitThickness = 10f;

        // This tick's strike sweep (host) or the last streamed one (other clients): the blade at the start and end of
        // the sweep, each point kept relative to NPC.position so on remote screens it stays attached to the body they see.
        private bool _strikeActive;
        private Vector2 _sweepStartHiltOffset;
        private Vector2 _sweepStartTipOffset;
        private Vector2 _sweepEndHiltOffset;
        private Vector2 _sweepEndTipOffset;
        private int _strikeDamage;

        /// <summary>Cap of the current strike's weapon; read by HostileMirrorPuppetPlayer.ModifyHurt on the victim.</summary>
        internal float StrikeCapFraction;

        /// <summary>Last strike sweep in world coords and how many more ticks to show it (debug drawing).</summary>
        internal Vector2 LastSweepStartHilt;
        internal Vector2 LastSweepStartTip;
        internal Vector2 LastSweepEndHilt;
        internal Vector2 LastSweepEndTip;
        internal int LastStrikeShowTicks;

        public override void AI()
        {
            if (LastStrikeShowTicks > 0)
            {
                LastStrikeShowTicks--;
            }

            SpawnTellDust();
        }

        // ── Use wrapper (§12.5–12.8): the attack is the player's; this only decides WHEN a use starts, how its tell
        // plays, WHERE it aims and when it stops. Owner (host) side, deterministic — no rolls — so nothing to sync but
        // the visible state (phase, aim) in the owner extras. ────────────────────────────────────────────────

        public enum WeaponCategory
        {
            Unusable,
            SwingMelee,
            HeldProjectileMelee,
            Whip,
            Shot,
            Channelled,
            NearInstantShot,
            SummonStaff
        }

        public enum UsePhase
        {
            Ready,
            Tell,
            Using,
            Recovery
        }

        /// <summary>Melee tell = max(20, 0.8 x useAnimation) ticks on screen; 40 for a heavy (useAnimation >= 35).
        /// Measured from the held start pose, before the 8-tick ease-in (attack-timing-design §2 rule 7).</summary>
        internal const int MeleeTellMinTicks = 20;
        internal const float MeleeTellUseTimeFraction = 0.8f;
        internal const int HeavyUseTime = 35;
        internal const int HeavyTellTicks = 40;

        /// <summary>Ranged/magic tell (§12.7, 16–20): weapon pointed + aim line + muzzle dust; aim locks for the last 8.
        /// Near-instant shots: 30, locked for the last 12.</summary>
        internal const int ShotTellTicks = 18;
        internal const int ShotAimLockTicks = 8;
        internal const int NearInstantTellTicks = 30;
        internal const int NearInstantAimLockTicks = 12;

        /// <summary>Effective projectile speed (shootSpeed x (1 + extraUpdates), px/tick) at which a shot counts as
        /// near-instant (§12.5, ESTIMATE).</summary>
        internal const float NearInstantShotSpeed = 40f;

        /// <summary>Minimum firing distance = effective speed x this many ticks of flight (attack-quality-pass §1).
        /// Near-instant shots skip it: their reaction cue is the locked aim line, not the flight.</summary>
        internal const int ShotReactionTicks = 20;

        /// <summary>Aim turn rate while threatening (deg/tick): 4 for shots and between melee swings, 2 for channels
        /// (§12.7, ESTIMATE). Outside a tell/use the aim snaps to the target — nothing is threatening yet.</summary>
        internal const float AimTurnDegreesPerTick = 4f;
        internal const float ChannelAimTurnDegreesPerTick = 2f;

        /// <summary>Uses per burst before a recovery: 3 swings (§12.6), 4 shots (§12.7), 1 channel. Held-projectile
        /// melee and whips run their own projectile timers the clock can't space out, so each gets its own tell.</summary>
        internal const int MeleeBurstMax = 3;
        internal const int ShotBurstMax = 4;

        /// <summary>Recovery after a burst, weapon put away (ESTIMATE): melee and channels 45 (§12.6 / §12.7), shots 30.
        /// A use that failed to start (no mana/ammo, CanUseItem false) waits the retry time before trying again.</summary>
        internal const int MeleeRecoveryTicks = 45;
        internal const int ShotRecoveryTicks = 30;
        internal const int ChannelRecoveryTicks = 45;
        internal const int FailedUseRetryTicks = 30;

        /// <summary>Longest a channelled weapon is held (§12.7, ESTIMATE).</summary>
        internal const int ChannelMaxTicks = 60;

        /// <summary>Aim point is at least this far out, so cursor-distance weapons never get a zero-length aim.</summary>
        internal const float MinAimDistance = 64f;

        /// <summary>Explicit outliers the automatic rules don't catch (§12.8): sky-rain and arena-wide attacks. Keep it
        /// short — the categories do the real filtering. Add modded outliers here.</summary>
        internal static readonly HashSet<int> BannedItemTypes = new HashSet<int>
        {
            ItemID.Starfury,
            ItemID.StarWrath,
            ItemID.LunarFlareBook,
            ItemID.BlizzardStaff,
            ItemID.MeteorStaff,
            ItemID.Zenith
        };

        /// <summary>Owner side, once per tick: whom to attack. False = nobody (the wrapper idles). The subclass owns
        /// targeting; the wrapper owns everything about the use itself.</summary>
        protected abstract bool ChooseTarget(Player owner, out Vector2 targetCenter);

        /// <summary>The wrapper's phase. Owner side it's authoritative; other clients get it in the owner extras.</summary>
        internal UsePhase CurrentUsePhase;

        /// <summary>Owner side: the held weapon is a shot and the target is inside its minimum firing distance. The
        /// invader AI (step 5) backs off or switches to a melee slot on it.</summary>
        internal bool TooCloseToFire;

        /// <summary>Owner side: the last use the wrapper tried didn't start. The invader AI switches weapon on it.</summary>
        internal bool LastUseFailed;

        // Aim (angle from NPC.Center, turn-rate capped) and the category the current tell/use was started with.
        private float _aimAngle;
        private float _aimDistance = MinAimDistance;
        private bool _aimInitialized;
        private bool _aimLocked;
        private WeaponCategory _useCategory;

        private int _phaseTicks;
        private int _tellTicks;
        private int _recoveryTicks;
        private int _burstStartUses;
        private int _channelTicks;
        private Item _phaseWeapon;

        // Deferred to release: the shot the tell's start run skipped, and the use sound it muted (played at release).
        private bool _shotSuppressed;
        private SoundStyle? _tellUseSound;
        private bool _restoreUseSound;
        private bool _restoreShootsEveryUse;

        // Every client: where this tick's tell dust/aim line start (the weapon tip), for PostDraw.
        private Vector2 _tellTip;
        private bool _hasTellTip;

        /// <summary>Weapon colour per item type (average opaque texture pixel), for colour-matched tell dust and lines.</summary>
        private static readonly Dictionary<int, Color> WeaponTellColors = new Dictionary<int, Color>();

        protected override bool HoldUsePose => CurrentUsePhase == UsePhase.Tell;

        internal override bool AllowSwapShot()
        {
            // The tell's start run must not fire (sword beams fire on a use's first run). Vanilla returns before
            // ApplyItemTime, so itemTime stays 0 and release fires it (see the release block in ChooseAttack).
            if (CurrentUsePhase == UsePhase.Tell)
            {
                _shotSuppressed = true;
                return false;
            }

            return true;
        }

        /// <summary>Which rules a weapon gets, from item stats only (§12.5), with the §12.8 filter folded in as
        /// Unusable. Callers: the wrapper, and the invader's loadout filter (step 5).</summary>
        internal static WeaponCategory Categorize(Item item)
        {
            if (item == null || item.IsAir || item.damage <= 0)
            {
                return WeaponCategory.Unusable;
            }

            if (BannedItemTypes.Contains(item.type))
            {
                return WeaponCategory.Unusable;
            }

            // Tools mine/chop the HOST's world at the puppet's cursor; consumables would eat the puppet's 1-stack copy;
            // mounts, fishing poles and healing items aren't attacks.
            bool isTool = item.pick > 0 || item.axe > 0 || item.hammer > 0;
            bool isUtility = item.mountType != -1 || item.fishingPole > 0 || item.healLife > 0 || item.healMana > 0;

            if (isTool || isUtility || item.consumable || item.sentry)
            {
                return WeaponCategory.Unusable;
            }

            int shoot = item.shoot;
            bool shoots = shoot > ProjectileID.None && shoot < ProjectileLoader.ProjectileCount;

            if (shoots && Main.projHook[shoot])
            {
                return WeaponCategory.Unusable;
            }

            if (shoots && ProjectileID.Sets.IsAWhip[shoot])
            {
                return WeaponCategory.Whip;
            }

            if (item.CountsAsClass(DamageClass.Summon))
            {
                return WeaponCategory.SummonStaff;
            }

            // Homing and cursor-steered shots (Magic Missile, Rainbow Rod…) — vanilla's own "homing" list. Cursor-
            // steered ones would follow the host's real mouse once their AI runs outside the swap's aim.
            if (shoots && ProjectileID.Sets.CultistIsResistantTo[shoot])
            {
                return WeaponCategory.Unusable;
            }

            if (IsSwingOrThrustMelee(item))
            {
                return WeaponCategory.SwingMelee;
            }

            if (!shoots)
            {
                return WeaponCategory.Unusable;
            }

            if (item.CountsAsClass(DamageClass.Melee) && item.noMelee)
            {
                return WeaponCategory.HeldProjectileMelee;
            }

            if (item.channel)
            {
                return WeaponCategory.Channelled;
            }

            if (EffectiveShotSpeed(item) >= NearInstantShotSpeed)
            {
                return WeaponCategory.NearInstantShot;
            }

            return WeaponCategory.Shot;
        }

        /// <summary>px/tick the weapon's own projectile covers: shootSpeed x (1 + extraUpdates). ESTIMATE for ammo
        /// weapons — the ammo's speed bonus and projectile aren't known until PickAmmo.</summary>
        internal static float EffectiveShotSpeed(Item item)
        {
            int extraUpdates = 0;

            if (ContentSamples.ProjectilesByType.TryGetValue(item.shoot, out Projectile sample))
            {
                extraUpdates = sample.extraUpdates;
            }

            return item.shootSpeed * (1 + extraUpdates);
        }

        /// <summary>
        /// The fairness wrapper (§12.6–12.7). Phases:
        ///   Ready    — aim snaps to the target; start a use when allowed (shots: not inside the minimum distance).
        ///   Tell     — the use's first run happens (shot and sound held back), then its pose HOLDS (0 runs) for the tell.
        ///              Melee aim is locked from the start (the held pose already encodes it; tracking would snap the
        ///              swing on release). Shots trail at the capped turn rate and lock for the last 8 / 12 ticks.
        ///   Using    — release: fire the held shot, play the sound, let the use play. Bursts up to 3 swings / 4 shots
        ///              (the eased clock spaces swings ≥ 32 ticks live-to-live); channels hold ≤ 60 ticks.
        ///   Recovery — weapon away for 30–45 ticks.
        /// </summary>
        protected sealed override bool ChooseAttack(Player owner, out Vector2 aimWorld)
        {
            Item weapon = ActiveWeapon;
            WeaponCategory category = Categorize(weapon);
            bool hasTarget = ChooseTarget(owner, out Vector2 targetCenter);
            bool heldProjectileActive = UseState.HeldProj >= 0 && UseState.HeldProj < Main.maxProjectiles && Main.projectile[UseState.HeldProj].active;
            bool handsFree = UseState.ItemAnimation == 0 && !heldProjectileActive;

            // A different weapon in hand between uses (the invader AI switches weapons) starts the wrapper over — except
            // a recovery, which still runs out (switching weapons must not skip the punish window).
            bool weaponChanged = weapon != _phaseWeapon;

            if (weaponChanged && handsFree)
            {
                _phaseWeapon = weapon;

                if (CurrentUsePhase != UsePhase.Recovery)
                {
                    CurrentUsePhase = UsePhase.Ready;
                    _phaseTicks = 0;
                }
            }

            _phaseTicks++;

            // ── Aim ──
            // Which category's rules: the one the tell/use started with (it can't change mid-use), else the held one.
            WeaponCategory rulesCategory = category;

            if (CurrentUsePhase == UsePhase.Tell || CurrentUsePhase == UsePhase.Using)
            {
                rulesCategory = _useCategory;
            }

            bool meleeRules = rulesCategory == WeaponCategory.SwingMelee || rulesCategory == WeaponCategory.HeldProjectileMelee || rulesCategory == WeaponCategory.Whip;
            int aimLockTicks = ShotAimLockTicks;

            if (rulesCategory == WeaponCategory.NearInstantShot)
            {
                aimLockTicks = NearInstantAimLockTicks;
            }

            _aimLocked = false;

            if (CurrentUsePhase == UsePhase.Tell)
            {
                int ticksToRelease = _tellTicks - _phaseTicks;
                _aimLocked = meleeRules || ticksToRelease < aimLockTicks;
            }
            else if (CurrentUsePhase == UsePhase.Using)
            {
                // Melee: locked for the whole use, re-aims only in the holds between swings. Shots: locked while the next
                // shot is due within the lock window (itemTime counts down to the next shot). Channels never lock.
                bool midUse = UseState.ItemAnimation > 0;
                bool shotRules = rulesCategory == WeaponCategory.Shot || rulesCategory == WeaponCategory.NearInstantShot;
                bool nextShotDue = UseState.ItemTime <= aimLockTicks;
                _aimLocked = (meleeRules && midUse) || (shotRules && midUse && nextShotDue);
            }

            if (hasTarget && !_aimLocked)
            {
                Vector2 toTarget = targetCenter - NPC.Center;
                float desiredAngle = toTarget.ToRotation();
                bool threatening = CurrentUsePhase == UsePhase.Tell || CurrentUsePhase == UsePhase.Using;

                if (!_aimInitialized || !threatening)
                {
                    _aimAngle = desiredAngle;
                    _aimInitialized = true;
                }
                else
                {
                    float turnDegrees = AimTurnDegreesPerTick;

                    if (rulesCategory == WeaponCategory.Channelled)
                    {
                        turnDegrees = ChannelAimTurnDegreesPerTick;
                    }

                    _aimAngle = _aimAngle.AngleTowards(desiredAngle, MathHelper.ToRadians(turnDegrees));
                }

                _aimDistance = Math.Max(MinAimDistance, toTarget.Length());
            }

            Vector2 aimDirection = _aimAngle.ToRotationVector2();
            aimWorld = NPC.Center + aimDirection * _aimDistance;

            // ── Phases ──
            TooCloseToFire = false;

            if (CurrentUsePhase == UsePhase.Ready)
            {
                bool usable = category != WeaponCategory.Unusable && category != WeaponCategory.SummonStaff;

                if (!hasTarget || !usable)
                {
                    return false;
                }

                bool shotRules = category == WeaponCategory.Shot || category == WeaponCategory.Channelled;

                if (shotRules)
                {
                    float minimumDistance = EffectiveShotSpeed(weapon) * ShotReactionTicks;
                    float targetDistance = Vector2.Distance(NPC.Center, targetCenter);

                    if (targetDistance < minimumDistance)
                    {
                        TooCloseToFire = true;
                        return false;
                    }
                }

                // Start the use THIS tick (its first run), then hold it as the tell. Tell length from item stats only.
                _useCategory = category;
                _tellTicks = ShotTellTicks;

                if (category == WeaponCategory.NearInstantShot)
                {
                    _tellTicks = NearInstantTellTicks;
                }
                else if (category == WeaponCategory.SwingMelee || category == WeaponCategory.HeldProjectileMelee || category == WeaponCategory.Whip)
                {
                    int scaledTell = (int)Math.Round(weapon.useAnimation * MeleeTellUseTimeFraction);
                    _tellTicks = Math.Max(MeleeTellMinTicks, scaledTell);

                    if (weapon.useAnimation >= HeavyUseTime)
                    {
                        _tellTicks = HeavyTellTicks;
                    }
                }

                CurrentUsePhase = UsePhase.Tell;
                _phaseTicks = 0;
                _burstStartUses = UsesStartedTotal;
                _channelTicks = 0;
                _shotSuppressed = false;
                LastUseFailed = false;

                // Mute the start run's use sound (ItemCheck_StartActualUse plays it); release plays it. Our own item copy,
                // restored in AfterWeaponUse right after the run.
                _tellUseSound = null;
                bool playsInitialSound = weapon.UseSound.HasValue && !ItemID.Sets.SkipsInitialUseSound[weapon.type];

                if (playsInitialSound)
                {
                    _tellUseSound = weapon.UseSound;
                    weapon.UseSound = null;
                    _restoreUseSound = true;
                }

                return true;
            }

            if (CurrentUsePhase == UsePhase.Tell)
            {
                // The start run didn't start a use (no mana or ammo, CanUseItem false): give up on it for now.
                if (_phaseTicks == 1 && UseState.ItemAnimation == 0)
                {
                    LastUseFailed = true;
                    CurrentUsePhase = UsePhase.Recovery;
                    _recoveryTicks = FailedUseRetryTicks;
                    _phaseTicks = 0;
                    return false;
                }

                // Guns and staffs point along the (trailing) aim while held: vanilla only sets itemRotation when it shoots.
                // Same formula as ItemCheck_Shoot (atan2 of the aim scaled by facing).
                bool pointsAtAim = weapon.useStyle == ItemUseStyleID.Shoot && !meleeRules;

                if (pointsAtAim)
                {
                    int facing = 1;

                    if (aimDirection.X < 0f)
                    {
                        facing = -1;
                    }

                    UseState.Direction = facing;
                    UseState.ItemRotation = (float)Math.Atan2(aimDirection.Y * facing, aimDirection.X * facing);
                }

                if (_phaseTicks < _tellTicks)
                {
                    return true;
                }

                // Release: this tick's run plays the use from where it was held. The skipped shot fires on it — itemTime
                // forced to 0, and shootsEveryUse weapons (which only shoot on the start run) told to shoot this once.
                CurrentUsePhase = UsePhase.Using;
                _phaseTicks = 0;

                if (_shotSuppressed)
                {
                    UseState.ItemTime = 0;

                    if (weapon.shootsEveryUse)
                    {
                        weapon.shootsEveryUse = false;
                        _restoreShootsEveryUse = true;
                    }
                }

                if (_tellUseSound.HasValue)
                {
                    SoundEngine.PlaySound(_tellUseSound.Value, NPC.Center);
                }

                if (LogsMeleeClock)
                {
                    Mod.Logger.Info($"[MirrorUse] '{weapon.Name}' {_useCategory}: tell {_tellTicks}t held, released (shot deferred: {_shotSuppressed})");
                }

                return true;
            }

            if (CurrentUsePhase == UsePhase.Using)
            {
                // Uses started since the tell (the tell's own use included). Held-projectile melee, whips and channels: 1.
                int usesThisBurst = UsesStartedTotal - _burstStartUses;
                int burstMax = 1;

                if (_useCategory == WeaponCategory.SwingMelee)
                {
                    burstMax = MeleeBurstMax;
                }
                else if (_useCategory == WeaponCategory.Shot || _useCategory == WeaponCategory.NearInstantShot)
                {
                    burstMax = ShotBurstMax;
                }

                bool burstCountReached = usesThisBurst >= burstMax;
                bool staminaLeft = StaminaCurrent > 0f;
                bool isChannel = _useCategory == WeaponCategory.Channelled;
                bool wantsUse;

                if (isChannel)
                {
                    if (UseState.ItemAnimation > 0)
                    {
                        _channelTicks++;
                    }

                    wantsUse = _channelTicks < ChannelMaxTicks;
                }
                else
                {
                    wantsUse = !burstCountReached && staminaLeft;

                    // Non-autoReuse weapons need a released button between uses, like a player clicking again.
                    bool needsRelease = !weapon.autoReuse && UseState.ItemAnimation == 0 && UseState.PreviousControlUseItem;

                    if (needsRelease)
                    {
                        wantsUse = false;
                    }
                }

                if (!hasTarget)
                {
                    wantsUse = false;
                }

                // Ends once the last use has finished AND nothing more will start (a one-tick release between uses or the
                // eased clock's chain hold also look like "hands free, not using").
                bool nothingMoreToUse = burstCountReached || !staminaLeft || !hasTarget || isChannel;

                if (handsFree && usesThisBurst >= 1 && nothingMoreToUse)
                {
                    CurrentUsePhase = UsePhase.Recovery;
                    _phaseTicks = 0;
                    _recoveryTicks = ShotRecoveryTicks;

                    if (meleeRules)
                    {
                        _recoveryTicks = MeleeRecoveryTicks;
                    }
                    else if (_useCategory == WeaponCategory.Channelled)
                    {
                        _recoveryTicks = ChannelRecoveryTicks;
                    }

                    if (LogsMeleeClock)
                    {
                        Mod.Logger.Info($"[MirrorUse] '{weapon?.Name}' {_useCategory}: burst of {usesThisBurst}, recovery {_recoveryTicks}t (stamina left {StaminaCurrent:0})");
                    }
                }

                return wantsUse;
            }

            // Recovery.
            if (_phaseTicks >= _recoveryTicks)
            {
                CurrentUsePhase = UsePhase.Ready;
                _phaseTicks = 0;
            }

            return false;
        }

        protected override void AfterWeaponUse(Player owner, Item weapon)
        {
            // Undo the per-run item edits the tell made on our own item copy (muted sound, one-off shootsEveryUse).
            bool weaponPresent = weapon != null && !weapon.IsAir;

            if (_restoreUseSound && weaponPresent)
            {
                weapon.UseSound = _tellUseSound;
            }

            _restoreUseSound = false;

            // shootsEveryUse waits for the shot itself: the eased clock may do 0 runs on the release tick. ApplyItemTime
            // after the shot makes itemTime > 0; a use that ended without shooting gives up on it.
            bool deferredShotDone = UseState.ItemTime > 0 || UseState.ItemAnimation == 0;

            if (_restoreShootsEveryUse && (deferredShotDone || !weaponPresent))
            {
                if (weaponPresent)
                {
                    weapon.shootsEveryUse = true;
                }

                _restoreShootsEveryUse = false;
            }

            _strikeActive = HasStrikeSweep && weapon != null && !weapon.IsAir;

            if (!_strikeActive)
            {
                return;
            }

            _sweepStartHiltOffset = StrikeSweepStartHilt - NPC.position;
            _sweepStartTipOffset = StrikeSweepStartTip - NPC.position;
            _sweepEndHiltOffset = StrikeSweepEndHilt - NPC.position;
            _sweepEndTipOffset = StrikeSweepEndTip - NPC.position;
            _strikeDamage = weapon.damage;
            StrikeCapFraction = GetHitCapFraction(weapon);

            // The host's own player is a victim like any other. The swap has restored the host's real position by now.
            TryStrikeLocalPlayer();
        }

        // ── Tell visuals: every client, from the wrapper state (owner) or the streamed copy (others) ─────

        /// <summary>Colour-matched tip dust while the tell holds: 1 per tick, 2 in its last third, growing with it.</summary>
        private void SpawnTellDust()
        {
            _hasTellTip = false;

            if (Main.dedServ || DisplayClone == null || CurrentUsePhase != UsePhase.Tell)
            {
                return;
            }

            Item weapon = DisplayClone.inventory[0];

            if (weapon == null || weapon.IsAir)
            {
                return;
            }

            // Tip: the drawn blade's tip for a visible swing/thrust (the held start pose), else the hand plus the
            // weapon's length along the aim (guns, staffs, and the hand-drawn noUseGraphic weapons in PostDraw).
            Vector2 aimDirection = _aimAngle.ToRotationVector2();
            float drawScale = DisplayClone.GetAdjustedItemScale(weapon);
            bool bladeDrawn = IsSwingOrThrustMelee(weapon) && !weapon.noUseGraphic;

            if (bladeDrawn)
            {
                ComputeHeldBlade(weapon, DisplayClone.itemLocation, DisplayClone.itemRotation, DisplayClone.direction, drawScale, out _, out _tellTip);
            }
            else
            {
                Main.instance.LoadItem(weapon.type);
                Rectangle frame = Main.player[Main.myPlayer].GetItemDrawFrame(weapon.type);
                float weaponLength = Math.Max(frame.Width, frame.Height) * drawScale;
                _tellTip = DisplayClone.MountedCenter + aimDirection * weaponLength;
            }

            _hasTellTip = true;

            float progress = MathHelper.Clamp(_phaseTicks / (float)Math.Max(1, _tellTicks), 0f, 1f);
            int dustCount = 1;

            if (progress > 0.66f)
            {
                dustCount = 2;
            }

            Color tellColor = GetWeaponTellColor(weapon.type);
            float dustScale = 0.7f + 0.6f * progress;

            for (int i = 0; i < dustCount; i++)
            {
                Vector2 dustPosition = _tellTip + Main.rand.NextVector2Circular(4f, 4f);
                Vector2 dustVelocity = Main.rand.NextVector2Circular(0.6f, 0.6f);
                Dust dust = Dust.NewDustPerfect(dustPosition, DustID.RainbowMk2, dustVelocity, 0, tellColor, dustScale);
                dust.noGravity = true;
            }
        }

        /// <summary>Average opaque pixel of the item sprite, lifted 25% toward white so dark weapons still read. Cached per
        /// type; white (uncached) until the texture has loaded. Callers: tell dust and the aim line.</summary>
        private static Color GetWeaponTellColor(int itemType)
        {
            if (WeaponTellColors.TryGetValue(itemType, out Color cachedColor))
            {
                return cachedColor;
            }

            Main.instance.LoadItem(itemType);
            Asset<Texture2D> textureAsset = TextureAssets.Item[itemType];

            if (!textureAsset.IsLoaded)
            {
                return Color.White;
            }

            Texture2D texture = textureAsset.Value;
            Color[] pixels = new Color[texture.Width * texture.Height];
            texture.GetData(pixels);

            long redTotal = 0;
            long greenTotal = 0;
            long blueTotal = 0;
            long opaqueCount = 0;

            for (int i = 0; i < pixels.Length; i++)
            {
                if (pixels[i].A <= 128)
                {
                    continue;
                }

                redTotal += pixels[i].R;
                greenTotal += pixels[i].G;
                blueTotal += pixels[i].B;
                opaqueCount++;
            }

            Color averageColor = Color.White;

            if (opaqueCount > 0)
            {
                averageColor = new Color((int)(redTotal / opaqueCount), (int)(greenTotal / opaqueCount), (int)(blueTotal / opaqueCount));
            }

            Color tellColor = Color.Lerp(averageColor, Color.White, 0.25f);
            WeaponTellColors[itemType] = tellColor;
            return tellColor;
        }

        /// <summary>Tell overlays: the weapon itself for noUseGraphic weapons (spears, yoyos, flails, whips draw nothing
        /// in hand), and for shots an aim line from the tip that brightens and thickens once the aim locks.
        /// Subclasses that override PostDraw must call base.</summary>
        public override void PostDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (CurrentUsePhase != UsePhase.Tell || !_hasTellTip || DisplayClone == null)
            {
                return;
            }

            Item weapon = DisplayClone.inventory[0];

            if (weapon == null || weapon.IsAir)
            {
                return;
            }

            Vector2 aimDirection = _aimAngle.ToRotationVector2();
            Color tellColor = GetWeaponTellColor(weapon.type);

            if (weapon.noUseGraphic)
            {
                // Grip at the sprite's bottom-left corner; item sprites are drawn diagonally (tip up-right), so +45° puts
                // the tip along the aim.
                Main.instance.LoadItem(weapon.type);
                Texture2D texture = TextureAssets.Item[weapon.type].Value;
                Rectangle frame = Main.player[Main.myPlayer].GetItemDrawFrame(weapon.type);
                Vector2 grip = DisplayClone.MountedCenter;
                Vector2 gripOrigin = new Vector2(0f, frame.Height);
                float rotation = _aimAngle + MathHelper.PiOver4;
                float drawScale = DisplayClone.GetAdjustedItemScale(weapon);
                Color litColor = Lighting.GetColor(grip.ToTileCoordinates());
                spriteBatch.Draw(texture, grip - screenPos, frame, litColor, rotation, gripOrigin, drawScale, SpriteEffects.None, 0f);
            }

            bool drawsAimLine = _useCategory == WeaponCategory.Shot || _useCategory == WeaponCategory.Channelled || _useCategory == WeaponCategory.NearInstantShot;

            if (!drawsAimLine)
            {
                return;
            }

            // Utils.DrawLine takes world coords. Fades in over the tell; locked = near-white and 3 px, the "dodge now" cue.
            float progress = MathHelper.Clamp(_phaseTicks / (float)Math.Max(1, _tellTicks), 0f, 1f);
            float lineLength = Math.Min(_aimDistance + 48f, 1000f);
            Vector2 lineEnd = _tellTip + aimDirection * lineLength;
            Color lineColor = tellColor * (0.25f + 0.45f * progress);
            float lineWidth = 2f;

            if (_aimLocked)
            {
                lineColor = Color.Lerp(tellColor, Color.White, 0.6f) * 0.9f;
                lineWidth = 3f;
            }

            Utils.DrawLine(spriteBatch, _tellTip, lineEnd, lineColor, lineColor, lineWidth);
        }

        /// <summary>Subclasses that add their own extras must call base first (and in ReadOwnerExtras).</summary>
        protected override void WriteOwnerExtras(BinaryWriter writer)
        {
            // Wrapper state for the other clients' tell visuals (the pose itself is in the base pose stream: a tell is a
            // held use, so it streams every tick).
            writer.Write((byte)CurrentUsePhase);

            if (CurrentUsePhase == UsePhase.Tell)
            {
                writer.Write((byte)_useCategory);
                writer.Write(_aimAngle);
                writer.Write((short)_aimDistance);
                writer.Write((byte)Math.Min(255, _phaseTicks));
                writer.Write((byte)Math.Min(255, _tellTicks));
                writer.Write(_aimLocked);
            }

            writer.Write(_strikeActive);

            if (!_strikeActive)
            {
                return;
            }

            writer.Write((short)_sweepStartHiltOffset.X);
            writer.Write((short)_sweepStartHiltOffset.Y);
            writer.Write((short)_sweepStartTipOffset.X);
            writer.Write((short)_sweepStartTipOffset.Y);
            writer.Write((short)_sweepEndHiltOffset.X);
            writer.Write((short)_sweepEndHiltOffset.Y);
            writer.Write((short)_sweepEndTipOffset.X);
            writer.Write((short)_sweepEndTipOffset.Y);
            writer.Write(_strikeDamage);
            writer.Write((byte)Math.Round(StrikeCapFraction * 100f));
        }

        protected override void ReadOwnerExtras(BinaryReader reader)
        {
            CurrentUsePhase = (UsePhase)reader.ReadByte();

            if (CurrentUsePhase == UsePhase.Tell)
            {
                _useCategory = (WeaponCategory)reader.ReadByte();
                _aimAngle = reader.ReadSingle();
                _aimDistance = reader.ReadInt16();
                _phaseTicks = reader.ReadByte();
                _tellTicks = reader.ReadByte();
                _aimLocked = reader.ReadBoolean();
            }

            _strikeActive = reader.ReadBoolean();

            if (!_strikeActive)
            {
                return;
            }

            float startHiltX = reader.ReadInt16();
            float startHiltY = reader.ReadInt16();
            float startTipX = reader.ReadInt16();
            float startTipY = reader.ReadInt16();
            float endHiltX = reader.ReadInt16();
            float endHiltY = reader.ReadInt16();
            float endTipX = reader.ReadInt16();
            float endTipY = reader.ReadInt16();
            _sweepStartHiltOffset = new Vector2(startHiltX, startHiltY);
            _sweepStartTipOffset = new Vector2(startTipX, startTipY);
            _sweepEndHiltOffset = new Vector2(endHiltX, endHiltY);
            _sweepEndTipOffset = new Vector2(endTipX, endTipY);
            _strikeDamage = reader.ReadInt32();
            StrikeCapFraction = reader.ReadByte() / 100f;

            // The server only forwards; each client tests its own player against the sweep on its own copy of the body.
            if (Main.dedServ)
            {
                return;
            }

            TryStrikeLocalPlayer();
        }

        /// <summary>Victim-authoritative melee hit: this machine's own player against the blade's whole sweep this tick
        /// (start segment → end segment, interpolated so a fast swing can't skip past). Player.Hurt respects i-frames
        /// (rolls, post-hit immunity), so a strike lands at most once per 40 ticks; the cap and the death message are
        /// applied in HostileMirrorPuppetPlayer.ModifyHurt (source = this NPC).</summary>
        private void TryStrikeLocalPlayer()
        {
            Vector2 startHilt = NPC.position + _sweepStartHiltOffset;
            Vector2 startTip = NPC.position + _sweepStartTipOffset;
            Vector2 endHilt = NPC.position + _sweepEndHiltOffset;
            Vector2 endTip = NPC.position + _sweepEndTipOffset;

            LastSweepStartHilt = startHilt;
            LastSweepStartTip = startTip;
            LastSweepEndHilt = endHilt;
            LastSweepEndTip = endTip;
            LastStrikeShowTicks = StrikeDebugShowTicks;

            Player victim = Main.LocalPlayer;

            if (!victim.active || victim.dead || victim.immune)
            {
                return;
            }

            bool bladeTouches = Puppets.MeleeBladeCollision.SweptSegmentIntersectsRect(
                startHilt, startTip, endHilt, endTip, BladeHitThickness, victim.Hitbox, out _, out _);

            if (!bladeTouches)
            {
                return;
            }

            int hitDirection = victim.Center.X < NPC.Center.X ? -1 : 1;
            victim.Hurt(PlayerDeathReason.ByNPC(NPC.whoAmI), _strikeDamage, hitDirection);
        }
    }

    /// <summary>
    /// Victim side of every hostile mirror hit (melee strike or flipped projectile): undo vanilla's ×2 on hostile
    /// projectiles, cap the hit at the weapon's fraction of max HP AFTER defense, and name the invader as the killer.
    /// </summary>
    public class HostileMirrorPuppetPlayer : ModPlayer
    {
        public override void ModifyHurt(ref Player.HurtModifiers modifiers)
        {
            PlayerDeathReason source = modifiers.DamageSource;

            if (source == null)
            {
                return;
            }

            float capFraction = 0f;
            int puppetIndex = -1;
            bool fromProjectile = false;
            int projectileIndex = source.SourceProjectileLocalIndex;
            int npcIndex = source.SourceNPCIndex;
            bool projectileIndexValid = projectileIndex >= 0 && projectileIndex < Main.maxProjectiles;
            bool npcIndexValid = npcIndex >= 0 && npcIndex < Main.maxNPCs;

            if (projectileIndexValid && Main.projectile[projectileIndex].active)
            {
                MirrorPuppetProjectile tag = Main.projectile[projectileIndex].GetGlobalProjectile<MirrorPuppetProjectile>();

                if (tag.HostileToPlayers)
                {
                    capFraction = tag.HitCapFraction;
                    puppetIndex = tag.PuppetNpcIndex;
                    fromProjectile = true;
                }
            }
            else if (npcIndexValid && Main.npc[npcIndex].active && Main.npc[npcIndex].ModNPC is HostileMirrorPuppetNPC hostilePuppet)
            {
                capFraction = hostilePuppet.StrikeCapFraction;
                puppetIndex = npcIndex;
            }

            if (capFraction <= 0f)
            {
                return;
            }

            // Projectile.Damage hurts players for DamageVar(damage) * 2 on hostile projectiles (vanilla's convention
            // for NPC shots); the invader's shots should deal base weapon damage like its blade.
            if (fromProjectile)
            {
                modifiers.SourceDamage *= 0.5f;
            }

            int maxDamage = Math.Max(1, (int)(capFraction * Player.statLifeMax2));
            modifiers.SetMaxDamage(maxDamage);

            // Killer = the invader, not "<host>'s <projectile>". CustomReason is synced with the death packet.
            bool puppetValid = puppetIndex >= 0 && puppetIndex < Main.maxNPCs && Main.npc[puppetIndex].active;

            if (puppetValid)
            {
                NetworkText killerName = Main.npc[puppetIndex].GetTypeNetName();
                source.CustomReason = NetworkText.FromKey("Mods.tsorcRevamp.NPCs.HostileMirrorPuppetNPC.DeathMessage", Player.name, killerName);
            }
        }
    }
}

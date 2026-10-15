using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.IO;
using System.Reflection;
using Terraria;
using Terraria.DataStructures;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using tsorcRevamp.Content.Items.Weapons.Melee.Broadswords.BroadswordRework.Common.Movement;
using tsorcRevamp.Content.Projectiles;

namespace tsorcRevamp.NPCs.FriendlyPuppets
{
    /// <summary>
    /// Shared base for every NPC that copies a player and fights with that player's real weapons: the friendly
    /// summons (<see cref="FriendlyPuppetNPC"/> → Soul Mimic, swap test dummy) and, later, the hostile mirror invader.
    /// Design: tsorcDocs/MimicTear_FriendlyPuppet_Design.md (§3, §12.2).
    ///
    /// Weapon use is NOT re-implemented: on one client (the "owner" — the summoner, or the invader's host) the
    /// puppet borrows that player's own <see cref="Player.ItemCheck"/> through <see cref="MirrorPuppetSwap"/>, so
    /// every weapon swings, shoots, costs stamina and draws exactly as it does for the player. A separate display
    /// <see cref="Player"/> clone is what actually gets drawn. Friend-or-foe policy (hit rules, damage, minion
    /// slots, aggro, look) lives in the subclass layers.
    ///
    /// NPC.ai[0] = owner player index (set at spawn, synced by vanilla).
    /// </summary>
    public abstract class MirrorPuppetNPC : ModNPC
    {
        // Dust-free, sprite-free NPC texture: the visible body is the display clone drawn in PreDraw.
        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(InvisibleNothingProj));

        /// <summary>Vanilla stamina regen applies its gain once every 4th tick (staminaResourceRegenTimer > 3).</summary>
        private const int StaminaRegenTickInterval = 4;

        /// <summary>The client that runs this puppet's weapon use (the summoner; for the invader, its host).</summary>
        public int OwnerIndex => (int)NPC.ai[0];

        /// <summary>Drawn instead of the NPC. Built lazily on every client from the owner's resolved equipment.</summary>
        protected Player DisplayClone;

        /// <summary>The puppet's own per-use state, loaded into the owner for the duration of each swap.</summary>
        internal MirrorPuppetUseState UseState;

        /// <summary>The puppet's own stamina pool. Max/regen are copied from the owner; spending happens
        /// through the owner's real stamina code while the pool is swapped in.</summary>
        internal float StaminaCurrent;
        internal bool StaminaInitialized;
        private int _staminaRegenDelay;
        private int _staminaRegenTimer;

        /// <summary>The puppet's own mana pool (max = the owner's statManaMax2). Weapons spend it through the
        /// owner's real mana code while it's swapped into owner.statMana. Owner-client only.</summary>
        internal int ManaCurrent;
        private int _manaRegenDelay;
        private float _manaRegenCount;

        /// <summary>Vanilla Player.UpdateManaRegen adds manaRegen to manaRegenCount each tick and grants 1 mana
        /// per 120 accumulated.</summary>
        private const float VanillaManaRegenCountPerPoint = 120f;

        /// <summary>The item the puppet is holding right now. Null or air = nothing to use.</summary>
        protected abstract Item ActiveWeapon { get; }

        /// <summary>Read access for the projectile-AI swap, which runs outside this class.</summary>
        internal Item CurrentWeapon => ActiveWeapon;

        /// <summary>
        /// Owner-client only, once per tick. Return true to hold "use" this tick and set where it aims.
        /// Runs before the swap, so <see cref="Main.MouseWorld"/> here is still the owner's real cursor.
        /// </summary>
        protected abstract bool ChooseAttack(Player owner, out Vector2 aimWorld);

        /// <summary>Damage rule for this puppet's weapon uses, applied by <see cref="MirrorPuppetGlobalItem"/> while
        /// the swap is active. Default: the owner's normal weapon damage.</summary>
        internal virtual void ModifySwapWeaponDamage(Item item, Player owner, ref StatModifier damage)
        {
        }

        /// <summary>Crit rule for this puppet's weapon uses (see <see cref="ModifySwapWeaponDamage"/>).</summary>
        internal virtual void ModifySwapWeaponCrit(Item item, Player owner, ref float crit)
        {
        }

        public override void SetStaticDefaults()
        {
            NPCID.Sets.NPCBestiaryDrawOffset.Add(Type, new NPCID.Sets.NPCBestiaryDrawModifiers() { Hide = true });
        }

        public override void SetDefaults()
        {
            // Player-sized hitbox so the display clone's feet sit on NPC.Bottom.
            NPC.width = 20;
            NPC.height = 42;
            NPC.aiStyle = -1;
            NPC.lifeMax = 100;
            NPC.damage = 0;
            NPC.knockBackResist = 0f;
            NPC.npcSlots = 0f;
            NPC.netAlways = true;
        }

        public override bool CheckActive()
        {
            // Lifetime is owned by the subclass (summon / encounter), never by vanilla's off-screen despawn.
            return false;
        }

        /// <summary>
        /// Owner-client only. Called from <see cref="MirrorPuppetOwnerPlayer.PostUpdate"/>, i.e. after the
        /// owner's own ItemCheck and before anything is drawn.
        /// </summary>
        internal void RunOwnerTick(Player owner)
        {
            EnsureDisplayClone(owner);

            tsorcRevampStaminaPlayer ownerStamina = owner.GetModPlayer<tsorcRevampStaminaPlayer>();

            if (!StaminaInitialized)
            {
                StaminaCurrent = ownerStamina.staminaResourceMax2;
                ManaCurrent = owner.statManaMax2;
                StaminaInitialized = true;
            }

            // Captured BEFORE the subclass hook so stamina it spends there (the Mimic Tear's dodge roll) also
            // counts as "spent this tick" and pauses regen, exactly like a weapon swing.
            float staminaBefore = StaminaCurrent;

            UpdateOwnerSide(owner);

            // Between uses the puppet faces the way it moves (NPC.direction, set by the movement AI); during a
            // use, the borrowed ItemCheck turns it toward its aim and the result is kept until the use ends.
            if (UseState.ItemAnimation == 0 && NPC.direction != 0)
            {
                UseState.Direction = NPC.direction;
            }

            // Decide (owner's real cursor is still in place here), then run the borrowed ItemCheck.
            Item weapon = ActiveWeapon;
            bool hasWeapon = weapon != null && !weapon.IsAir;
            Vector2 aimWorld = NPC.Center + new Vector2(NPC.direction * 100f, 0f);
            bool wantsUse = false;

            if (hasWeapon)
            {
                wantsUse = ChooseAttack(owner, out aimWorld);
            }

            int manaBefore = ManaCurrent;

            if (hasWeapon)
            {
                RunWeaponClock(owner, weapon, wantsUse, aimWorld);
            }
            else
            {
                _clockTracking = false;
                HasStrikeHitbox = false;
                CurrentMeleeSegment = MeleeClockSegment.None;
            }

            AfterWeaponUse(owner, weapon);

            // Mana regen mirrors the owner: the puppet regenerates at the owner's CURRENT vanilla regen rate, and
            // only while the owner's own regen is running — so the mod's class rules (Bearer of the Curse pinned
            // to zero outside bonfires, Unkindled's weak regen) carry over without being re-implemented. Casting
            // starts the puppet's own vanilla-length delay (owner.maxRegenDelay). APPROXIMATION: the owner casting
            // also pauses the puppet's regen, because the owner's rate reads 0 during the owner's delay.
            bool spentMana = ManaCurrent < manaBefore;

            if (spentMana)
            {
                _manaRegenDelay = (int)owner.maxRegenDelay;
            }
            else if (_manaRegenDelay > 0)
            {
                _manaRegenDelay--;
            }
            else if (owner.manaRegenDelay <= 0f && ManaCurrent < owner.statManaMax2)
            {
                _manaRegenCount += owner.manaRegen;

                while (_manaRegenCount >= VanillaManaRegenCountPerPoint)
                {
                    _manaRegenCount -= VanillaManaRegenCountPerPoint;
                    ManaCurrent = Math.Min(owner.statManaMax2, ManaCurrent + 1);
                }
            }

            // Mimic stamina regen: same "spending pauses regen" rule as the player (SpendRegenDelay),
            // gain = the owner's computed per-interval gain, no regen mid-swing.
            bool spentThisTick = StaminaCurrent < staminaBefore;

            if (spentThisTick)
            {
                _staminaRegenDelay = ownerStamina.SpendRegenDelay;
            }
            else if (_staminaRegenDelay > 0)
            {
                _staminaRegenDelay--;
            }
            else if (UseState.ItemAnimation == 0)
            {
                _staminaRegenTimer++;

                if (_staminaRegenTimer >= StaminaRegenTickInterval)
                {
                    _staminaRegenTimer = 0;
                    StaminaCurrent = Math.Min(ownerStamina.staminaResourceMax2, StaminaCurrent + ownerStamina.staminaResourceGain);
                }
            }

            // Pose the clone from the copied-out use state, then let vanilla pick body/leg frames for it.
            // Velocity drives PlayerFrame's walk/jump frames.
            DisplayClone.position = NPC.position;
            DisplayClone.velocity = NPC.velocity;
            DisplayClone.direction = UseState.Direction;
            DisplayClone.inventory[0] = hasWeapon ? weapon : new Item();
            DisplayClone.selectedItem = 0;
            DisplayClone.lastVisualizedSelectedItem = UseState.LastVisualizedItem ?? DisplayClone.inventory[0];
            DisplayClone.itemAnimation = UseState.ItemAnimation;
            DisplayClone.itemAnimationMax = UseState.ItemAnimationMax;
            DisplayClone.itemTime = UseState.ItemTime;
            DisplayClone.itemTimeMax = UseState.ItemTimeMax;
            DisplayClone.itemRotation = UseState.ItemRotation;
            DisplayClone.itemLocation = UseState.ItemLocation;
            DisplayClone.itemWidth = UseState.ItemWidth;
            DisplayClone.itemHeight = UseState.ItemHeight;
            DisplayClone.compositeFrontArm = UseState.CompositeFrontArm;
            DisplayClone.compositeBackArm = UseState.CompositeBackArm;
            DisplayClone.heldProj = UseState.HeldProj;
            DisplayClone.channel = UseState.Channel;
            DisplayClone.PlayerFrame();
            DecorateDisplayClone(DisplayClone);

            SendOwnerStateIfNeeded();
        }

        // ── Eased melee clock (design §12.6) ────────────────────────────────────────────────────────
        // A swing's pose is a pure function of its use timer (ItemCheck_ApplyUseStyle + PlayerFrame), so running
        // the borrowed ItemCheck 0, 1 or 2 times per real tick replays ANY swing/thrust weapon on a different clock
        // (0 runs = the pose holds). Segments are measured in NATIVE use-timer ticks n = itemAnimationMax - itemAnimation.

        public enum MeleeClockSegment
        {
            None,
            EaseIn,
            Strike,
            FollowThrough
        }

        /// <summary>Ease-in = the first 15% of the native timer; strike = up to 80%; follow-through = the rest.</summary>
        internal const float EaseInNativeFraction = 0.15f;
        internal const float StrikeEndNativeFraction = 0.80f;

        /// <summary>Real ticks for each eased segment. Strike plays at native speed, compressed to at most 18 ticks
        /// (under the 22-tick roll, attack-timing-design §2 rule 1).</summary>
        internal const int EaseInTicks = 8;
        internal const int StrikeMaxTicks = 18;
        internal const int FollowThroughTicks = 20;

        /// <summary>A new eased swing may only start once its strike (EaseInTicks after the start) would go live at
        /// least this long after the last live window ended: 20 follow-through + 4 hold + 8 ease-in = 32 (≥ 30, §2 rule 3).</summary>
        internal const int ChainLiveGapTicks = 32;

        /// <summary>Most borrowed ItemChecks in one real tick. 2 caps the strike compression: a strike longer than
        /// 36 native ticks (A > ~55) can't fit in 18 real ticks and runs long — logged when it happens.</summary>
        internal const int MaxItemChecksPerTick = 2;

        /// <summary>Owner side: drive swing/thrust melee on the eased clock instead of the native one.</summary>
        protected virtual bool UsesEasedMeleeClock => false;

        /// <summary>Owner side: write one tModLoader log line per tracked swing (the test dummy's A/B readout).</summary>
        protected virtual bool LogsMeleeClock => false;

        /// <summary>Owner side: which part of the current swing/thrust use the pose is in. None when not mid-use or
        /// for weapons the clock doesn't shape. The future hostile damage path arms hits only in Strike.</summary>
        internal MeleeClockSegment CurrentMeleeSegment { get; private set; }

        /// <summary>Owner side, rebuilt every tick: the melee hitbox (world coords) of this tick's Strike-segment runs.
        /// Only the hostile layer uses it (victim-side player hits); vanilla NPC hits are separate.</summary>
        internal Rectangle StrikeHitbox;
        internal bool HasStrikeHitbox;

        private int _ownerTickCount;
        private int _lastLiveEndTick = int.MinValue / 2;

        // The use being tracked (any swing/thrust melee use; eased or native).
        private bool _clockTracking;
        private bool _clockEased;
        private string _clockItemName;
        private int _clockNativeLength;
        private int _clockEaseInEnd;
        private int _clockStrikeEnd;
        private int _clockStrikeRealTicks;
        private int _clockRealTicks;
        private int _clockEaseInCount;
        private int _clockStrikeCount;
        private int _clockFollowCount;
        private int _clockGapFromPreviousLive;
        private bool _clockLiveStarted;
        private int _clockFirstShotRealTick;
        private int _clockFirstShotNative;

        /// <summary>The weapons the eased clock shapes: real swing/thrust melee (the blade IS the hitbox). Held-projectile
        /// melee (spears, yoyos, flails), whips and channelled weapons run their own timers in projectile AI (§12.5).</summary>
        internal static bool IsSwingOrThrustMelee(Item item)
        {
            if (item == null || item.IsAir)
            {
                return false;
            }

            if (item.noMelee || item.channel || item.damage <= 0)
            {
                return false;
            }

            return item.useStyle == ItemUseStyleID.Swing || item.useStyle == ItemUseStyleID.Thrust;
        }

        /// <summary>
        /// Runs the borrowed ItemCheck for this tick and tracks the segment of every swing/thrust use. Native: once
        /// per tick, exactly as the player. Eased (<see cref="UsesEasedMeleeClock"/>): as many runs as the §12.6
        /// schedule asks for — ease-in 8 ticks, strike ≤ 18, follow-through 20 — and no new swing until the chain gap.
        /// </summary>
        private void RunWeaponClock(Player owner, Item weapon, bool wantsUse, Vector2 aimWorld)
        {
            _ownerTickCount++;
            HasStrikeHitbox = false;
            int runs = 1;

            if (_clockTracking)
            {
                _clockRealTicks++;
            }

            // Eased schedule: native progress the pose should have reached by this real tick (piecewise linear,
            // integer division = floor), minus what's done = runs this tick. Past the end it just finishes the use.
            if (_clockTracking && _clockEased)
            {
                int realTick = _clockRealTicks;
                int strikeNative = _clockStrikeEnd - _clockEaseInEnd;
                int followNative = _clockNativeLength - _clockStrikeEnd;
                int strikeStartTick = EaseInTicks;
                int followStartTick = EaseInTicks + _clockStrikeRealTicks;
                int nativeTarget = _clockNativeLength;

                if (realTick <= strikeStartTick)
                {
                    nativeTarget = _clockEaseInEnd * realTick / EaseInTicks;
                }
                else if (realTick <= followStartTick)
                {
                    nativeTarget = _clockEaseInEnd + strikeNative * (realTick - strikeStartTick) / _clockStrikeRealTicks;
                }
                else if (realTick <= followStartTick + FollowThroughTicks)
                {
                    nativeTarget = _clockStrikeEnd + followNative * (realTick - followStartTick) / FollowThroughTicks;
                }

                int nativeDone = _clockNativeLength - UseState.ItemAnimation;
                runs = Math.Clamp(nativeTarget - nativeDone, 0, MaxItemChecksPerTick);
            }

            for (int run = 0; run < runs; run++)
            {
                int animationBefore = UseState.ItemAnimation;
                bool runWantsUse = wantsUse;

                // Chain gap: vanilla starts the next use inside the same ItemCheck that ends the last one (itemAnimation
                // 1 -> 0), so any run from itemAnimation <= 1 can start a swing. Hold "use" off until its strike —
                // EaseInTicks after the start — would go live >= ChainLiveGapTicks after the last live window ended.
                bool runCanStartUse = animationBefore <= 1;

                if (runCanStartUse && UsesEasedMeleeClock && IsSwingOrThrustMelee(weapon))
                {
                    int ticksSinceLiveEnded = _ownerTickCount - _lastLiveEndTick;

                    if (ticksSinceLiveEnded + EaseInTicks < ChainLiveGapTicks)
                    {
                        runWantsUse = false;
                    }
                }

                int shotsBefore = MirrorPuppetSwap.ProjectilesSpawned;
                MirrorPuppetSwap.HasCapturedMeleeHitbox = false;

                MirrorPuppetSwap.RunAsPuppet(owner, this, weapon, runWantsUse, aimWorld, owner.ItemCheck);

                bool shotThisRun = MirrorPuppetSwap.ProjectilesSpawned > shotsBefore;

                // Strike hitbox: the run's hitbox counts only if the pose it was built from is in the Strike segment
                // (native n in [ease-in end, strike end)). Two runs in one tick union into one swept box.
                bool runHitboxInStrike = false;

                if (MirrorPuppetSwap.HasCapturedMeleeHitbox && _clockTracking && UseState.ItemAnimation > 0)
                {
                    int runNativeProgress = _clockNativeLength - UseState.ItemAnimation;
                    runHitboxInStrike = runNativeProgress >= _clockEaseInEnd && runNativeProgress < _clockStrikeEnd;
                }

                if (runHitboxInStrike && HasStrikeHitbox)
                {
                    StrikeHitbox = Rectangle.Union(StrikeHitbox, MirrorPuppetSwap.CapturedMeleeHitbox);
                }
                else if (runHitboxInStrike)
                {
                    StrikeHitbox = MirrorPuppetSwap.CapturedMeleeHitbox;
                    HasStrikeHitbox = true;
                }

                // A use timer can only go UP by a use starting (ItemCheck otherwise only counts it down).
                bool useStarted = UseState.ItemAnimation > animationBefore;
                bool useEnded = UseState.ItemAnimation == 0;

                if (_clockTracking && (useStarted || useEnded))
                {
                    _clockTracking = false;

                    if (LogsMeleeClock)
                    {
                        int strikeLimitOverrun = Math.Max(0, _clockStrikeCount - StrikeMaxTicks);
                        string gapText = "n/a (first swing)";

                        if (_clockGapFromPreviousLive >= 0)
                        {
                            gapText = _clockGapFromPreviousLive + "t";
                        }

                        string shotText = "none";

                        if (_clockFirstShotRealTick >= 0)
                        {
                            shotText = $"real tick {_clockFirstShotRealTick} (native n={_clockFirstShotNative})";
                        }

                        string clockName = _clockEased ? "EASED" : "native";
                        int totalTicks = _clockEaseInCount + _clockStrikeCount + _clockFollowCount;
                        string overrunText = "";

                        if (_clockEased && strikeLimitOverrun > 0)
                        {
                            overrunText = $" STRIKE OVER {StrikeMaxTicks}t BY {strikeLimitOverrun}t";
                        }

                        Mod.Logger.Info($"[MeleeClock] {clockName} '{_clockItemName}' A={_clockNativeLength}t native (ease-in n<{_clockEaseInEnd}, strike n<{_clockStrikeEnd}) | real ticks: ease-in {_clockEaseInCount}, strike {_clockStrikeCount} (= live window), follow-through {_clockFollowCount}, total {totalTicks} | gap from previous live window to this one {gapText} | first projectile {shotText}{overrunText}");
                    }
                }

                // Start tracking a new swing/thrust use. Its native length A is fixed at start (melee speed included).
                if (useStarted && IsSwingOrThrustMelee(weapon))
                {
                    _clockTracking = true;
                    _clockEased = UsesEasedMeleeClock;
                    _clockItemName = weapon.Name;
                    _clockNativeLength = Math.Max(1, UseState.ItemAnimationMax);
                    _clockEaseInEnd = (int)Math.Round(_clockNativeLength * EaseInNativeFraction);
                    _clockStrikeEnd = Math.Max(_clockEaseInEnd, (int)Math.Round(_clockNativeLength * StrikeEndNativeFraction));
                    _clockRealTicks = 0;
                    _clockEaseInCount = 0;
                    _clockStrikeCount = 0;
                    _clockFollowCount = 0;
                    _clockLiveStarted = false;
                    _clockGapFromPreviousLive = -1;
                    _clockFirstShotRealTick = -1;
                    _clockFirstShotNative = -1;

                    // Strike: native speed (1 run per tick) unless that's over StrikeMaxTicks, then compressed — but
                    // never past MaxItemChecksPerTick runs per tick.
                    int strikeNative = _clockStrikeEnd - _clockEaseInEnd;
                    int strikeAtMaxRuns = (strikeNative + MaxItemChecksPerTick - 1) / MaxItemChecksPerTick;
                    _clockStrikeRealTicks = Math.Max(1, Math.Max(Math.Min(strikeNative, StrikeMaxTicks), strikeAtMaxRuns));
                }

                if (_clockTracking && shotThisRun && _clockFirstShotRealTick < 0)
                {
                    _clockFirstShotRealTick = _clockRealTicks;
                    _clockFirstShotNative = _clockNativeLength - UseState.ItemAnimation;
                }

                // An eased use that just ended doesn't take a second run: the gap rule would block a restart anyway.
                if (useEnded && _clockEased)
                {
                    break;
                }
            }

            // Segment of the pose this tick ends on (what gets drawn), counted once per real tick.
            if (!_clockTracking)
            {
                CurrentMeleeSegment = MeleeClockSegment.None;
                return;
            }

            int nativeProgress = _clockNativeLength - UseState.ItemAnimation;

            if (nativeProgress < _clockEaseInEnd)
            {
                CurrentMeleeSegment = MeleeClockSegment.EaseIn;
                _clockEaseInCount++;
            }
            else if (nativeProgress < _clockStrikeEnd)
            {
                CurrentMeleeSegment = MeleeClockSegment.Strike;
                _clockStrikeCount++;

                // Gap = real ticks from the previous swing's first non-live tick to this swing's first live tick.
                if (!_clockLiveStarted)
                {
                    _clockLiveStarted = true;
                    bool hadPreviousLive = _lastLiveEndTick > int.MinValue / 2;

                    if (hadPreviousLive)
                    {
                        _clockGapFromPreviousLive = _ownerTickCount - _lastLiveEndTick;
                    }
                }

                _lastLiveEndTick = _ownerTickCount + 1;
            }
            else
            {
                CurrentMeleeSegment = MeleeClockSegment.FollowThrough;
                _clockFollowCount++;
            }
        }

        /// <summary>Owner-client only, once per tick before the attack decision. For owner-side state such as
        /// the mimic's Cerulean drinking (its mana pool lives on the owner's client).</summary>
        protected virtual void UpdateOwnerSide(Player owner)
        {
        }

        /// <summary>Owner-client only, right after this tick's weapon use (segment and <see cref="StrikeHitbox"/> are
        /// current), before the owner-state packet is written. The hostile layer's host-side melee hit test.</summary>
        protected virtual void AfterWeaponUse(Player owner, Item weapon)
        {
        }

        /// <summary>Whether the borrowed ItemCheck may land vanilla melee hits as the owner (on NPCs and PvP players).
        /// A hostile puppet's blade must not hit enemies, itself, or players through vanilla PvP — its player hits
        /// come only from its own strike test.</summary>
        internal virtual bool SwapHitsAsOwner => true;

        /// <summary>Server (and single player): remove the puppet once its owner has left the world. Returns true if
        /// it was removed. Callers: friendly puppets and the hostile test rig.</summary>
        protected bool DespawnIfOwnerGone()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return false;
            }

            bool ownerIndexValid = OwnerIndex >= 0 && OwnerIndex < Main.maxPlayers;

            if (ownerIndexValid && Main.player[OwnerIndex].active)
            {
                return false;
            }

            NPC.active = false;

            // A deactivated NPC never reaches the netUpdate send, so sync the removal directly.
            if (Main.netMode == NetmodeID.Server)
            {
                NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, NPC.whoAmI);
            }

            return true;
        }

        /// <summary>Every client, after PlayerFrame has posed the clone: per-subclass overlays such as the flask
        /// in hand and the drinking arm pose.</summary>
        protected virtual void DecorateDisplayClone(Player clone)
        {
        }

        /// <summary>
        /// Builds the display clone once from the owner's RESOLVED equip slots (vanity already applied by
        /// vanilla) with the owner's real colours and dyes, then lets the subclass restyle it
        /// (<see cref="StyleDisplayClone"/>). Frozen after that (design: visuals frozen at summon).
        /// </summary>
        protected void EnsureDisplayClone(Player owner)
        {
            if (DisplayClone != null)
            {
                return;
            }

            Player clone = new Player();
            clone.active = true;
            clone.whoAmI = 0;
            clone.gravDir = 1f;
            clone.Male = owner.Male;
            clone.skinVariant = owner.skinVariant;
            clone.hair = owner.hair;
            clone.hairDye = owner.hairDye;
            clone.wearsRobe = owner.wearsRobe;

            clone.skinColor = owner.skinColor;
            clone.hairColor = owner.hairColor;
            clone.eyeColor = owner.eyeColor;
            clone.shirtColor = owner.shirtColor;
            clone.underShirtColor = owner.underShirtColor;
            clone.pantsColor = owner.pantsColor;
            clone.shoeColor = owner.shoeColor;

            clone.head = owner.head;
            clone.body = owner.body;
            clone.legs = owner.legs;
            clone.handon = owner.handon;
            clone.handoff = owner.handoff;
            clone.back = owner.back;
            clone.front = owner.front;
            clone.shoe = owner.shoe;
            clone.waist = owner.waist;
            clone.shield = owner.shield;
            clone.neck = owner.neck;
            clone.face = owner.face;
            clone.faceHead = owner.faceHead;
            clone.faceFlower = owner.faceFlower;
            clone.balloon = owner.balloon;
            clone.balloonFront = owner.balloonFront;
            clone.backpack = owner.backpack;
            clone.tail = owner.tail;
            clone.beard = owner.beard;
            clone.wings = owner.wings;
            clone.wingTimeMax = Math.Max(1, owner.wingTimeMax);

            // The owner's resolved dye shaders (vanilla UpdateDyes output), so the copy wears their real colours.
            clone.cHead = owner.cHead;
            clone.cBody = owner.cBody;
            clone.cLegs = owner.cLegs;
            clone.cHandOn = owner.cHandOn;
            clone.cHandOff = owner.cHandOff;
            clone.cBack = owner.cBack;
            clone.cFront = owner.cFront;
            clone.cShoe = owner.cShoe;
            clone.cWaist = owner.cWaist;
            clone.cShield = owner.cShield;
            clone.cShieldFallback = owner.cShieldFallback;
            clone.cNeck = owner.cNeck;
            clone.cFace = owner.cFace;
            clone.cFaceHead = owner.cFaceHead;
            clone.cFaceFlower = owner.cFaceFlower;
            clone.cBalloon = owner.cBalloon;
            clone.cBalloonFront = owner.cBalloonFront;
            clone.cBackpack = owner.cBackpack;
            clone.cTail = owner.cTail;
            clone.cBeard = owner.cBeard;
            clone.cWings = owner.cWings;
            clone.skinDyePacked = owner.skinDyePacked;

            StyleDisplayClone(clone);

            clone.position = NPC.position;
            clone.direction = NPC.direction == 0 ? 1 : NPC.direction;
            UseState.Direction = clone.direction;
            UseState.HeldProj = -1;

            DisplayClone = clone;
        }

        /// <summary>Once, when the clone is built: the subclass's look on top of the owner's real one
        /// (the Soul Mimic's Reflective Obsidian shell). Default: a true copy.</summary>
        protected virtual void StyleDisplayClone(Player clone)
        {
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            bool ownerIndexValid = OwnerIndex >= 0 && OwnerIndex < Main.maxPlayers;

            if (!ownerIndexValid)
            {
                return false;
            }

            EnsureDisplayClone(Main.player[OwnerIndex]);

            // Non-owner clients never run the swap. They walk the clone from NPC.velocity and, while the owner is
            // streaming a pose (mid-use / drinking), copy that pose exactly — the owner already ran the real
            // ItemCheck + PlayerFrame for it.
            if (OwnerIndex != Main.myPlayer)
            {
                DisplayClone.position = NPC.position;
                DisplayClone.velocity = NPC.velocity;

                if (NPC.direction != 0)
                {
                    DisplayClone.direction = NPC.direction;
                }

                DisplayClone.itemAnimation = 0;
                DisplayClone.heldProj = -1;
                DisplayClone.compositeFrontArm.enabled = false;
                DisplayClone.compositeBackArm.enabled = false;
                DisplayClone.PlayerFrame();

                if (_remotePoseActive)
                {
                    ApplyRemotePose(DisplayClone);
                }

                DecorateDisplayClone(DisplayClone);
            }

            Vector2 drawPosition = DisplayClone.position + DisplayDrawOffset;
            Main.PlayerRenderer.DrawPlayer(Main.Camera, DisplayClone, drawPosition, 0f, Vector2.Zero, 0f, 1f);
            return false;
        }

        /// <summary>Offset the clone is drawn at (the Mimic Tear's rise out of its pool). Position only — the
        /// NPC's hitbox doesn't move.</summary>
        protected virtual Vector2 DisplayDrawOffset => Vector2.Zero;

        // ── Owner → server → other clients: status flags, the streamed pose, subclass extras ────────

        /// <summary>Owner-side status bits the SERVER needs for decisions it can't make itself (the owner holds the
        /// stamina/mana pools). Meaning is defined by the subclass. Set on the owner client; in single player the
        /// server reads it from this same instance, in multiplayer it arrives via <see cref="ReceiveOwnerState"/>.</summary>
        internal byte OwnerFlags;

        /// <summary>Subclass hook: something worth streaming besides a weapon use (e.g. a Cerulean drink).</summary>
        protected virtual bool HasOwnerActivity => false;

        /// <summary>Subclass hook: extra owner-side state written after the pose. Must be read back in the same order
        /// by <see cref="ReadOwnerExtras"/> — on the server (decisions) and on other clients (display).</summary>
        protected virtual void WriteOwnerExtras(BinaryWriter writer)
        {
        }

        protected virtual void ReadOwnerExtras(BinaryReader reader)
        {
        }

        /// <summary>Owner state packets go out every tick while a pose is active, once more when it ends, when
        /// the flags change, and otherwise every this many ticks.</summary>
        private const int OwnerStateHeartbeatTicks = 60;

        private byte _lastSentFlags;
        private bool _lastSentPoseActive;
        private int _ownerStateHeartbeat;

        // Remote (non-owner) copy of the owner's streamed pose.
        private bool _remotePoseActive;
        private int _remoteDirection;
        private int _remoteItemType;
        private Item _remoteItem;
        private int _remoteItemAnimation;
        private int _remoteItemAnimationMax;
        private float _remoteItemRotation;
        private Vector2 _remoteItemLocationOffset;
        private int _remoteBodyFrameY;
        private Player.CompositeArmData _remoteFrontArm;
        private Player.CompositeArmData _remoteBackArm;
        private int _remoteHeldProjIndex = -1;

        /// <summary>
        /// Non-owner clients: projectile index → (puppet NPC index + 1) for held projectiles a puppet is holding,
        /// so their AI can run attached to the puppet instead of the owner (see MirrorPuppetSwapSystem).
        /// </summary>
        internal static readonly int[] RemoteHeldProjectilePuppet = new int[Main.maxProjectiles];

        /// <summary>Owner client, multiplayer only: stream status + pose (see <see cref="OwnerStateHeartbeatTicks"/>).</summary>
        private void SendOwnerStateIfNeeded()
        {
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                return;
            }

            bool poseActive = UseState.ItemAnimation > 0 || UseState.HeldProj >= 0 || HasOwnerActivity;
            bool flagsChanged = OwnerFlags != _lastSentFlags;
            _ownerStateHeartbeat++;
            bool heartbeatDue = _ownerStateHeartbeat >= OwnerStateHeartbeatTicks;
            bool shouldSend = poseActive || _lastSentPoseActive || flagsChanged || heartbeatDue;

            if (!shouldSend)
            {
                return;
            }

            // The payload goes in a length-prefixed blob, so the server can forward it (or skip it, if the NPC is
            // gone) without understanding the subclass extras.
            MemoryStream payloadStream = new MemoryStream();
            BinaryWriter payload = new BinaryWriter(payloadStream);
            payload.Write(OwnerFlags);
            payload.Write(poseActive);

            if (poseActive)
            {
                Player clone = DisplayClone;
                Vector2 locationOffset = clone.itemLocation - NPC.position;
                int heldProjIdentity = -1;

                if (UseState.HeldProj >= 0 && UseState.HeldProj < Main.maxProjectiles && Main.projectile[UseState.HeldProj].active)
                {
                    heldProjIdentity = Main.projectile[UseState.HeldProj].identity;
                }

                payload.Write((sbyte)clone.direction);
                payload.Write(clone.inventory[0].type);
                payload.Write((short)clone.itemAnimation);
                payload.Write((short)clone.itemAnimationMax);
                payload.Write(clone.itemRotation);
                payload.Write((short)locationOffset.X);
                payload.Write((short)locationOffset.Y);
                payload.Write((short)clone.bodyFrame.Y);
                payload.Write(clone.compositeFrontArm.enabled);
                payload.Write((byte)clone.compositeFrontArm.stretch);
                payload.Write(clone.compositeFrontArm.rotation);
                payload.Write(clone.compositeBackArm.enabled);
                payload.Write((byte)clone.compositeBackArm.stretch);
                payload.Write(clone.compositeBackArm.rotation);
                payload.Write(heldProjIdentity);
            }

            WriteOwnerExtras(payload);
            payload.Flush();
            byte[] payloadBytes = payloadStream.ToArray();

            ModPacket packet = ModContent.GetInstance<MirrorPuppetSwapSystem>().Mod.GetPacket();
            packet.Write(tsorcPacketID.SyncFriendlyPuppetOwnerState);
            packet.Write((short)NPC.whoAmI);
            packet.Write((ushort)payloadBytes.Length);
            packet.Write(payloadBytes);
            packet.Send();

            _lastSentFlags = OwnerFlags;
            _lastSentPoseActive = poseActive;
            _ownerStateHeartbeat = 0;
        }

        /// <summary>Packet <c>SyncFriendlyPuppetOwnerState</c>: on the server, apply then forward to the other
        /// clients; on a client, apply (flags, pose for display, extras).</summary>
        public static void ReceiveOwnerState(BinaryReader reader, int sender)
        {
            int npcIndex = reader.ReadInt16();
            int length = reader.ReadUInt16();
            byte[] payloadBytes = reader.ReadBytes(length);

            bool npcIndexValid = npcIndex >= 0 && npcIndex < Main.maxNPCs;
            MirrorPuppetNPC puppet = null;

            if (npcIndexValid && Main.npc[npcIndex].active)
            {
                puppet = Main.npc[npcIndex].ModNPC as MirrorPuppetNPC;
            }

            // Only the owner may speak for its puppet.
            bool fromOwner = puppet != null && (Main.netMode != NetmodeID.Server || puppet.OwnerIndex == sender);

            if (fromOwner)
            {
                puppet.ApplyOwnerState(payloadBytes);
            }

            if (Main.netMode == NetmodeID.Server && fromOwner)
            {
                ModPacket forward = ModContent.GetInstance<MirrorPuppetSwapSystem>().Mod.GetPacket();
                forward.Write(tsorcPacketID.SyncFriendlyPuppetOwnerState);
                forward.Write((short)npcIndex);
                forward.Write((ushort)payloadBytes.Length);
                forward.Write(payloadBytes);
                forward.Send(-1, sender);
            }
        }

        private void ApplyOwnerState(byte[] payloadBytes)
        {
            BinaryReader payload = new BinaryReader(new MemoryStream(payloadBytes));
            OwnerFlags = payload.ReadByte();
            bool poseActive = payload.ReadBoolean();

            // Forget the previous held-projectile mapping; a new one is set below if the pose still holds one.
            bool hadHeldProj = _remoteHeldProjIndex >= 0 && _remoteHeldProjIndex < Main.maxProjectiles;

            if (hadHeldProj && RemoteHeldProjectilePuppet[_remoteHeldProjIndex] == NPC.whoAmI + 1)
            {
                RemoteHeldProjectilePuppet[_remoteHeldProjIndex] = 0;
            }

            _remoteHeldProjIndex = -1;
            _remotePoseActive = poseActive;

            if (poseActive)
            {
                _remoteDirection = payload.ReadSByte();
                _remoteItemType = payload.ReadInt32();
                _remoteItemAnimation = payload.ReadInt16();
                _remoteItemAnimationMax = payload.ReadInt16();
                _remoteItemRotation = payload.ReadSingle();
                float offsetX = payload.ReadInt16();
                float offsetY = payload.ReadInt16();
                _remoteItemLocationOffset = new Vector2(offsetX, offsetY);
                _remoteBodyFrameY = payload.ReadInt16();
                bool frontEnabled = payload.ReadBoolean();
                Player.CompositeArmStretchAmount frontStretch = (Player.CompositeArmStretchAmount)payload.ReadByte();
                float frontRotation = payload.ReadSingle();
                bool backEnabled = payload.ReadBoolean();
                Player.CompositeArmStretchAmount backStretch = (Player.CompositeArmStretchAmount)payload.ReadByte();
                float backRotation = payload.ReadSingle();
                int heldProjIdentity = payload.ReadInt32();

                _remoteFrontArm = new Player.CompositeArmData(frontEnabled, frontStretch, frontRotation);
                _remoteBackArm = new Player.CompositeArmData(backEnabled, backStretch, backRotation);

                if (heldProjIdentity >= 0)
                {
                    int projectileIndex = Projectile.GetByUUID(OwnerIndex, heldProjIdentity);
                    bool projectileIndexValid = projectileIndex >= 0 && projectileIndex < Main.maxProjectiles;

                    if (projectileIndexValid)
                    {
                        _remoteHeldProjIndex = projectileIndex;
                        RemoteHeldProjectilePuppet[projectileIndex] = NPC.whoAmI + 1;
                    }
                }
            }

            ReadOwnerExtras(payload);
        }

        /// <summary>Non-owner clients: copy the owner's streamed pose onto the clone after PlayerFrame.</summary>
        private void ApplyRemotePose(Player clone)
        {
            if (_remoteItem == null || _remoteItem.type != _remoteItemType)
            {
                _remoteItem = new Item();

                if (_remoteItemType > 0)
                {
                    _remoteItem.SetDefaults(_remoteItemType);
                }
            }

            clone.direction = _remoteDirection == 0 ? 1 : _remoteDirection;
            clone.inventory[0] = _remoteItem;
            clone.selectedItem = 0;
            clone.lastVisualizedSelectedItem = _remoteItem;
            clone.itemAnimation = _remoteItemAnimation;
            clone.itemAnimationMax = _remoteItemAnimationMax;
            clone.itemRotation = _remoteItemRotation;
            clone.itemLocation = NPC.position + _remoteItemLocationOffset;
            clone.bodyFrame.Y = _remoteBodyFrameY;
            clone.compositeFrontArm = _remoteFrontArm;
            clone.compositeBackArm = _remoteBackArm;
            clone.heldProj = _remoteHeldProjIndex;
        }
    }

    /// <summary>
    /// A mirror puppet's persistent per-use state: everything <see cref="Player.ItemCheck"/> reads or
    /// advances between ticks. Lives on the puppet; loaded into the owner during a swap, copied back after.
    /// </summary>
    internal struct MirrorPuppetUseState
    {
        public int Direction;
        public int ItemAnimation;
        public int ItemAnimationMax;
        public int ItemTime;
        public int ItemTimeMax;
        public int ReuseDelay;
        public int AttackCD;
        public int ToolTime;
        public float ItemRotation;
        public Vector2 ItemLocation;
        public int ItemWidth;
        public int ItemHeight;
        public bool Channel;
        public int HeldProj;
        public int ItemUsesThisAnimation;
        public Item LastVisualizedItem;
        public Player.CompositeArmData CompositeFrontArm;
        public Player.CompositeArmData CompositeBackArm;

        /// <summary>Last tick's "use" input, so non-autoReuse weapons see a release between uses.</summary>
        public bool PreviousControlUseItem;

        /// <summary>Where the last ItemCheck aimed; reused when the puppet's projectiles run their AI later in the tick.</summary>
        public Vector2 AimWorld;

        /// <summary>The stamina code detects a new use by itemAnimation rising above this (tsorcRevampPlayer.PreItemCheck).</summary>
        public int StaminaOldItemAnimation;

        /// <summary>The puppet's own per-NPC melee hit cooldowns (vanilla Player.meleeNPCHitCooldown, 200 entries), swapped
        /// in by reference. Shared with the owner, a puppet's swing start would wipe the owner's cooldowns (letting the
        /// owner's swing re-hit) and every extra ItemCheck would tick them down again. Null until the first swap.</summary>
        public int[] MeleeNpcHitCooldown;
    }

    /// <summary>
    /// Runs a mirror puppet's weapon use through the owner's real <see cref="Player.ItemCheck"/>.
    ///
    /// Vanilla ItemCheck only fully runs for Main.myPlayer (67 gates), so a fake Player can't call it. Instead,
    /// on the owner's client: save the owner's per-use state, load the puppet's, run ItemCheck, copy the result
    /// back to the puppet, restore the owner — all inside one update tick, before draw, so the player never
    /// sees it. <see cref="Active"/> is what every leak guard keys on.
    /// </summary>
    internal static class MirrorPuppetSwap
    {
        /// <summary>True only for the duration of a borrowed ItemCheck. Guards: ammo/mana (MirrorPuppetGlobalItem),
        /// packets 13/41 (MirrorPuppetSwapSystem), projectile tagging, the stamina EMA, and the per-tick
        /// timers in the mod's PreItemCheck hooks (dodge roll, Estus, Cerulean).</summary>
        public static bool Active;

        /// <summary>The puppet whose use is running (for projectile tagging / debugging).</summary>
        public static MirrorPuppetNPC Current;

        /// <summary>Running count of projectiles spawned inside a swap (MirrorPuppetProjectile.OnSpawn). The melee clock
        /// diffs it around each run to log where in a swing a sword beam fires.</summary>
        public static int ProjectilesSpawned;

        /// <summary>The melee hitbox vanilla built during the current run (MirrorPuppetGlobalItem.MeleeEffects receives
        /// the final itemRectangle). The melee clock clears it before each run and keeps it only for Strike runs.</summary>
        public static Rectangle CapturedMeleeHitbox;
        public static bool HasCapturedMeleeHitbox;

        // Player.ItemUsesThisAnimation has a private setter; restoring it needs reflection.
        private static readonly Action<Player, int> SetItemUsesThisAnimation = CreateItemUsesSetter();

        private static Action<Player, int> CreateItemUsesSetter()
        {
            PropertyInfo property = typeof(Player).GetProperty(nameof(Player.ItemUsesThisAnimation));
            MethodInfo setter = property.GetSetMethod(true);
            return (Action<Player, int>)Delegate.CreateDelegate(typeof(Action<Player, int>), setter);
        }

        /// <summary>
        /// Everything about the owner that a borrowed ItemCheck could change, captured before and put back
        /// after. Life/mana/buffs/immunity/lifeSteal are restored wholesale: nothing during a puppet's use may
        /// benefit (or cost) the player — weapon debuffs on enemies are unaffected because they live on the NPC.
        /// </summary>
        private struct OwnerSnapshot
        {
            public Vector2 Position;
            public Vector2 Velocity;
            public int Direction;
            public float GravDir;
            public int SelectedItem;
            public Item SlotItem;
            public Item LastVisualizedItem;
            public int ItemAnimation;
            public int ItemAnimationMax;
            public int ItemTime;
            public int ItemTimeMax;
            public int ReuseDelay;
            public int AttackCD;
            public int ToolTime;
            public float ItemRotation;
            public Vector2 ItemLocation;
            public int ItemWidth;
            public int ItemHeight;
            public bool Channel;
            public int HeldProj;
            public int ItemUsesThisAnimation;
            public Player.CompositeArmData CompositeFrontArm;
            public Player.CompositeArmData CompositeBackArm;
            public Rectangle BodyFrame;
            public Rectangle LegFrame;
            public Rectangle HeadFrame;
            public bool ControlUseItem;
            public bool ReleaseUseItem;
            public bool ControlUseTile;
            public bool ReleaseUseTile;
            public bool MouseInterface;
            public int StatLife;
            public int StatMana;
            public float ManaRegenDelay;
            public bool Immune;
            public bool ImmuneNoBlink;
            public int ImmuneTime;
            public float LifeSteal;
            public int[] BuffType;
            public int[] BuffTime;
            public float StaminaCurrent;
            public int StaminaOldItemAnimation;
            public int[] MeleeNpcHitCooldown;
            public Vector2 BroadswordMouseWorld;
            public int MouseX;
            public int MouseY;
            public bool MouseLeft;
            public bool MouseLeftRelease;

            public static OwnerSnapshot Capture(Player owner)
            {
                OwnerSnapshot snapshot = new OwnerSnapshot();
                snapshot.Position = owner.position;
                snapshot.Velocity = owner.velocity;
                snapshot.Direction = owner.direction;
                snapshot.GravDir = owner.gravDir;
                snapshot.SelectedItem = owner.selectedItem;
                snapshot.SlotItem = owner.inventory[0];
                snapshot.LastVisualizedItem = owner.lastVisualizedSelectedItem;
                snapshot.ItemAnimation = owner.itemAnimation;
                snapshot.ItemAnimationMax = owner.itemAnimationMax;
                snapshot.ItemTime = owner.itemTime;
                snapshot.ItemTimeMax = owner.itemTimeMax;
                snapshot.ReuseDelay = owner.reuseDelay;
                snapshot.AttackCD = owner.attackCD;
                snapshot.ToolTime = owner.toolTime;
                snapshot.ItemRotation = owner.itemRotation;
                snapshot.ItemLocation = owner.itemLocation;
                snapshot.ItemWidth = owner.itemWidth;
                snapshot.ItemHeight = owner.itemHeight;
                snapshot.Channel = owner.channel;
                snapshot.HeldProj = owner.heldProj;
                snapshot.ItemUsesThisAnimation = owner.ItemUsesThisAnimation;
                snapshot.CompositeFrontArm = owner.compositeFrontArm;
                snapshot.CompositeBackArm = owner.compositeBackArm;
                snapshot.BodyFrame = owner.bodyFrame;
                snapshot.LegFrame = owner.legFrame;
                snapshot.HeadFrame = owner.headFrame;
                snapshot.ControlUseItem = owner.controlUseItem;
                snapshot.ReleaseUseItem = owner.releaseUseItem;
                snapshot.ControlUseTile = owner.controlUseTile;
                snapshot.ReleaseUseTile = owner.releaseUseTile;
                snapshot.MouseInterface = owner.mouseInterface;
                snapshot.StatLife = owner.statLife;
                snapshot.StatMana = owner.statMana;
                snapshot.ManaRegenDelay = owner.manaRegenDelay;
                snapshot.Immune = owner.immune;
                snapshot.ImmuneNoBlink = owner.immuneNoBlink;
                snapshot.ImmuneTime = owner.immuneTime;
                snapshot.LifeSteal = owner.lifeSteal;
                snapshot.BuffType = (int[])owner.buffType.Clone();
                snapshot.BuffTime = (int[])owner.buffTime.Clone();
                snapshot.StaminaCurrent = owner.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceCurrent;
                snapshot.StaminaOldItemAnimation = owner.GetModPlayer<tsorcRevampPlayer>().oldItemAnimation;
                snapshot.MeleeNpcHitCooldown = owner.meleeNPCHitCooldown;
                snapshot.BroadswordMouseWorld = owner.GetModPlayer<BroadswordReworkPlayer>().MouseWorld;
                snapshot.MouseX = Main.mouseX;
                snapshot.MouseY = Main.mouseY;
                snapshot.MouseLeft = Main.mouseLeft;
                snapshot.MouseLeftRelease = Main.mouseLeftRelease;
                return snapshot;
            }

            public void Restore(Player owner)
            {
                owner.position = Position;
                owner.velocity = Velocity;
                owner.direction = Direction;
                owner.gravDir = GravDir;
                owner.selectedItem = SelectedItem;
                owner.inventory[0] = SlotItem;
                owner.lastVisualizedSelectedItem = LastVisualizedItem;
                owner.itemAnimation = ItemAnimation;
                owner.itemAnimationMax = ItemAnimationMax;
                owner.itemTime = ItemTime;
                owner.itemTimeMax = ItemTimeMax;
                owner.reuseDelay = ReuseDelay;
                owner.attackCD = AttackCD;
                owner.toolTime = ToolTime;
                owner.itemRotation = ItemRotation;
                owner.itemLocation = ItemLocation;
                owner.itemWidth = ItemWidth;
                owner.itemHeight = ItemHeight;
                owner.channel = Channel;
                owner.heldProj = HeldProj;
                SetItemUsesThisAnimation(owner, ItemUsesThisAnimation);
                owner.compositeFrontArm = CompositeFrontArm;
                owner.compositeBackArm = CompositeBackArm;
                owner.bodyFrame = BodyFrame;
                owner.legFrame = LegFrame;
                owner.headFrame = HeadFrame;
                owner.controlUseItem = ControlUseItem;
                owner.releaseUseItem = ReleaseUseItem;
                owner.controlUseTile = ControlUseTile;
                owner.releaseUseTile = ReleaseUseTile;
                owner.mouseInterface = MouseInterface;
                owner.statLife = StatLife;
                owner.statMana = StatMana;
                owner.manaRegenDelay = ManaRegenDelay;
                owner.immune = Immune;
                owner.immuneNoBlink = ImmuneNoBlink;
                owner.immuneTime = ImmuneTime;
                owner.lifeSteal = LifeSteal;
                Array.Copy(BuffType, owner.buffType, BuffType.Length);
                Array.Copy(BuffTime, owner.buffTime, BuffTime.Length);
                owner.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceCurrent = StaminaCurrent;
                owner.GetModPlayer<tsorcRevampPlayer>().oldItemAnimation = StaminaOldItemAnimation;
                owner.meleeNPCHitCooldown = MeleeNpcHitCooldown;
                owner.GetModPlayer<BroadswordReworkPlayer>().MouseWorld = BroadswordMouseWorld;
                Main.mouseX = MouseX;
                Main.mouseY = MouseY;
                Main.mouseLeft = MouseLeft;
                Main.mouseLeftRelease = MouseLeftRelease;
            }
        }

        /// <summary>
        /// Runs <paramref name="body"/> with the owner temporarily "being" <paramref name="puppet"/>: holding
        /// <paramref name="weapon"/>, standing at the puppet, aiming at <paramref name="aimWorld"/>. Owner client only.
        /// Two callers: the puppet's per-tick ItemCheck (<see cref="MirrorPuppetNPC.RunOwnerTick"/>) and the AI of
        /// projectiles the puppet owns (<see cref="MirrorPuppetSwapSystem"/>) — held-projectile weapons (Phantasm,
        /// spears, yoyos, channelled guns) re-read <c>Main.player[owner]</c> every tick, so their AI needs the same swap.
        /// Slot 0 of the owner's inventory temporarily holds the puppet's weapon (ItemCheck reads the held item from the
        /// inventory) — the finally block guarantees the player's real item goes back even if a weapon's hook throws.
        /// </summary>
        public static void RunAsPuppet(Player owner, MirrorPuppetNPC puppet, Item weapon, bool wantsUse, Vector2 aimWorld, Action body)
        {
            if (owner.whoAmI != Main.myPlayer || owner.dead || Active)
            {
                return;
            }

            OwnerSnapshot ownerState = OwnerSnapshot.Capture(owner);
            ref MirrorPuppetUseState useState = ref puppet.UseState;

            Active = true;
            Current = puppet;

            try
            {
                // ── Load the puppet into the owner ─────────────────────────────────────────────
                // Position: ItemCheck places itemLocation, hitboxes, projectiles, sounds and dust relative
                // to the player, so moving the owner onto the puppet is what makes all of those land there.
                owner.position = puppet.NPC.Bottom - new Vector2(owner.width * 0.5f, owner.height);
                owner.velocity = Vector2.Zero;
                owner.direction = useState.Direction == 0 ? 1 : useState.Direction;
                owner.gravDir = 1f;
                owner.selectedItem = 0;
                owner.inventory[0] = weapon;
                owner.lastVisualizedSelectedItem = useState.LastVisualizedItem ?? weapon;
                owner.itemAnimation = useState.ItemAnimation;
                owner.itemAnimationMax = useState.ItemAnimationMax;
                owner.itemTime = useState.ItemTime;
                owner.itemTimeMax = useState.ItemTimeMax;
                owner.reuseDelay = useState.ReuseDelay;
                owner.attackCD = useState.AttackCD;
                owner.toolTime = useState.ToolTime;
                owner.itemRotation = useState.ItemRotation;
                owner.itemLocation = useState.ItemLocation;
                owner.itemWidth = useState.ItemWidth;
                owner.itemHeight = useState.ItemHeight;
                owner.channel = useState.Channel;
                owner.heldProj = useState.HeldProj;
                SetItemUsesThisAnimation(owner, useState.ItemUsesThisAnimation);

                if (useState.MeleeNpcHitCooldown == null)
                {
                    useState.MeleeNpcHitCooldown = new int[owner.meleeNPCHitCooldown.Length];
                }

                owner.meleeNPCHitCooldown = useState.MeleeNpcHitCooldown;

                // Use input: hold while wanted; "release" whenever last tick wasn't held, so
                // non-autoReuse weapons can start a new use. Tile/right-click input is always off.
                owner.controlUseItem = wantsUse;
                owner.releaseUseItem = !useState.PreviousControlUseItem;
                owner.controlUseTile = false;
                owner.releaseUseTile = false;
                owner.mouseInterface = false;
                Main.mouseLeft = wantsUse;
                Main.mouseLeftRelease = !useState.PreviousControlUseItem;

                // Aim: ItemCheck and the broadsword rework read the cursor. These are the game's copy of the
                // mouse coordinates, not the OS cursor, and are restored before draw.
                Main.mouseX = (int)(aimWorld.X - Main.screenPosition.X);
                Main.mouseY = (int)(aimWorld.Y - Main.screenPosition.Y);
                owner.GetModPlayer<BroadswordReworkPlayer>().MouseWorld = aimWorld;

                // Stamina and mana: the puppet's own pools, charged by the owner's real per-item cost code.
                // (Both are restored to the owner's values by the snapshot afterwards.)
                owner.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceCurrent = puppet.StaminaCurrent;
                owner.statMana = puppet.ManaCurrent;
                owner.GetModPlayer<tsorcRevampPlayer>().oldItemAnimation = useState.StaminaOldItemAnimation;

                // ── Run ───────────────────────────────────────────────────────────────────────
                body();

                // ── Copy the result back to the puppet ────────────────────────────────────────
                useState.Direction = owner.direction;
                useState.ItemAnimation = owner.itemAnimation;
                useState.ItemAnimationMax = owner.itemAnimationMax;
                useState.ItemTime = owner.itemTime;
                useState.ItemTimeMax = owner.itemTimeMax;
                useState.ReuseDelay = owner.reuseDelay;
                useState.AttackCD = owner.attackCD;
                useState.ToolTime = owner.toolTime;
                useState.ItemRotation = owner.itemRotation;
                useState.ItemLocation = owner.itemLocation;
                useState.ItemWidth = owner.itemWidth;
                useState.ItemHeight = owner.itemHeight;
                useState.Channel = owner.channel;
                useState.HeldProj = owner.heldProj;
                useState.ItemUsesThisAnimation = owner.ItemUsesThisAnimation;
                useState.LastVisualizedItem = owner.lastVisualizedSelectedItem;
                useState.CompositeFrontArm = owner.compositeFrontArm;
                useState.CompositeBackArm = owner.compositeBackArm;
                useState.PreviousControlUseItem = wantsUse;
                useState.AimWorld = aimWorld;
                useState.StaminaOldItemAnimation = owner.GetModPlayer<tsorcRevampPlayer>().oldItemAnimation;
                puppet.StaminaCurrent = owner.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceCurrent;
                puppet.ManaCurrent = owner.statMana;
            }
            finally
            {
                ownerState.Restore(owner);
                Active = false;
                Current = null;
            }
        }
    }

    /// <summary>Drives the owner's puppets once per tick, after the owner's own ItemCheck, before draw.</summary>
    public class MirrorPuppetOwnerPlayer : ModPlayer
    {
        public override void PostUpdate()
        {
            if (Player.whoAmI != Main.myPlayer || Main.dedServ)
            {
                return;
            }

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];

                if (!npc.active || npc.ModNPC is not MirrorPuppetNPC puppet)
                {
                    continue;
                }

                if (puppet.OwnerIndex != Player.whoAmI)
                {
                    continue;
                }

                // Minion slots (summons only): vanilla zeroes slotsMinions late in Player.Update (just before
                // ItemCheck) and each minion projectile re-adds its own slots when IT updates, killing itself if
                // there's no room. A puppet is an NPC, so its owner reserves its slots here — after the reset,
                // before the projectiles run — and ordinary minions then fit themselves into what's left.
                if (puppet is FriendlyPuppetNPC summon)
                {
                    Player.slotsMinions += summon.GetMinionSlotsUsed(Player);
                }

                puppet.RunOwnerTick(Player);
            }
        }
    }

    /// <summary>
    /// Multiplayer leak guard. Aimed shots (useStyle 5) send packet 13 (owner position + controls) and 41
    /// (item animation) for the owner from INSIDE ItemCheck_Shoot. During a swap those would show the owner
    /// jumping to the puppet and aiming for a frame on other screens, so they're dropped; the owner's real
    /// state syncs normally after the restore. Any other self-packet sent mid-swap is logged once.
    /// </summary>
    public class MirrorPuppetSwapSystem : ModSystem
    {
        private static readonly bool[] LoggedPacketTypes = new bool[256];

        public override void Load()
        {
            On_NetMessage.SendData += FilterSwapPackets;
            On_Projectile.AI += RunPuppetProjectileAI;
        }

        /// <summary>
        /// Held-projectile weapons (Phantasm, spears, yoyos, channelled guns) re-attach to <c>Main.player[owner]</c>,
        /// check <c>player.channel</c> and aim at the mouse inside their AI every tick. For a projectile the puppet owns,
        /// that AI runs inside the same swap as the puppet's ItemCheck, so it follows the puppet instead of the player.
        /// Wraps <see cref="Projectile.AI"/> (PreAI + AI + PostAI) so the restore can't be skipped.
        /// </summary>
        private static void RunPuppetProjectileAI(On_Projectile.orig_AI orig, Projectile projectile)
        {
            bool ownedByLocalPlayer = projectile.owner == Main.myPlayer && !Main.dedServ;

            // ── Other clients: a held projectile the owner's pose stream says a puppet is holding ──
            // Its AI here only needs to stay attached to the puppet for display, so a light swap of the owner's
            // position/facing/use flags is enough (the real swap only exists on the owner's client).
            int remotePuppetSlot = 0;

            if (!ownedByLocalPlayer && !Main.dedServ)
            {
                remotePuppetSlot = MirrorPuppetNPC.RemoteHeldProjectilePuppet[projectile.whoAmI];
            }

            if (remotePuppetSlot > 0)
            {
                NPC remotePuppetNpc = Main.npc[remotePuppetSlot - 1];
                Player remoteOwner = Main.player[projectile.owner];
                // Projectile indices get reused, so also require the same owner as the puppet.
                bool remotePuppetValid = remotePuppetNpc.active
                    && remotePuppetNpc.ModNPC is MirrorPuppetNPC remotePuppet
                    && remotePuppet.OwnerIndex == projectile.owner;

                if (!remotePuppetValid)
                {
                    MirrorPuppetNPC.RemoteHeldProjectilePuppet[projectile.whoAmI] = 0;
                    orig(projectile);
                    return;
                }

                Vector2 savedPosition = remoteOwner.position;
                Vector2 savedVelocity = remoteOwner.velocity;
                int savedDirection = remoteOwner.direction;
                int savedItemAnimation = remoteOwner.itemAnimation;
                int savedItemTime = remoteOwner.itemTime;
                bool savedChannel = remoteOwner.channel;
                int savedHeldProj = remoteOwner.heldProj;

                try
                {
                    remoteOwner.position = remotePuppetNpc.Bottom - new Vector2(remoteOwner.width * 0.5f, remoteOwner.height);
                    remoteOwner.velocity = remotePuppetNpc.velocity;
                    remoteOwner.direction = remotePuppetNpc.direction == 0 ? 1 : remotePuppetNpc.direction;
                    remoteOwner.itemAnimation = Math.Max(2, remoteOwner.itemAnimation);
                    remoteOwner.itemTime = Math.Max(2, remoteOwner.itemTime);
                    remoteOwner.channel = true;
                    remoteOwner.heldProj = projectile.whoAmI;
                    orig(projectile);
                }
                finally
                {
                    remoteOwner.position = savedPosition;
                    remoteOwner.velocity = savedVelocity;
                    remoteOwner.direction = savedDirection;
                    remoteOwner.itemAnimation = savedItemAnimation;
                    remoteOwner.itemTime = savedItemTime;
                    remoteOwner.channel = savedChannel;
                    remoteOwner.heldProj = savedHeldProj;
                }

                return;
            }

            int puppetIndex = -1;

            if (ownedByLocalPlayer && !MirrorPuppetSwap.Active)
            {
                puppetIndex = projectile.GetGlobalProjectile<MirrorPuppetProjectile>().PuppetNpcIndex;
            }

            bool puppetIndexValid = puppetIndex >= 0 && puppetIndex < Main.maxNPCs;
            MirrorPuppetNPC puppet = null;

            if (puppetIndexValid && Main.npc[puppetIndex].active)
            {
                puppet = Main.npc[puppetIndex].ModNPC as MirrorPuppetNPC;
            }

            if (puppet == null || puppet.OwnerIndex != projectile.owner)
            {
                orig(projectile);
                return;
            }

            Item weapon = puppet.CurrentWeapon;
            Player owner = Main.player[projectile.owner];

            // RunAsPuppet refuses to swap a dead owner, and would then skip the body — the AI must still run.
            if (weapon == null || weapon.IsAir || owner.dead)
            {
                orig(projectile);
                return;
            }

            // Same input the puppet's ItemCheck used this tick, so channel stays held while it's attacking.
            bool wantsUse = puppet.UseState.PreviousControlUseItem;
            Vector2 aimWorld = puppet.UseState.AimWorld;
            MirrorPuppetSwap.RunAsPuppet(owner, puppet, weapon, wantsUse, aimWorld, () => orig(projectile));
        }

        private static void FilterSwapPackets(On_NetMessage.orig_SendData orig, int msgType, int remoteClient, int ignoreClient, NetworkText text, int number, float number2, float number3, float number4, int number5, int number6, int number7)
        {
            bool aboutOwner = number == Main.myPlayer;

            if (MirrorPuppetSwap.Active && aboutOwner)
            {
                bool positionOrAnimationPacket = msgType == MessageID.PlayerControls || msgType == MessageID.ShotAnimationAndSound;

                if (positionOrAnimationPacket)
                {
                    return;
                }

                bool knownType = msgType >= 0 && msgType < LoggedPacketTypes.Length;

                if (knownType && !LoggedPacketTypes[msgType])
                {
                    LoggedPacketTypes[msgType] = true;
                    ModContent.GetInstance<MirrorPuppetSwapSystem>().Mod.Logger.Warn($"[MirrorPuppetSwap] packet {msgType} about the owner was sent during a puppet swap (not filtered).");
                }
            }

            orig(msgType, remoteClient, ignoreClient, text, number, number2, number3, number4, number5, number6, number7);
        }
    }

    /// <summary>
    /// Puppet-use rules applied while <see cref="MirrorPuppetSwap.Active"/>: damage and crit come from the running
    /// puppet's own rule (<see cref="MirrorPuppetNPC.ModifySwapWeaponDamage"/>; the summons' best-of × share lives in
    /// FriendlyPuppetNPC.cs); ammo is free (ammo TYPE still comes from the owner's inventory). Projectile damage
    /// derives from weapon damage at spawn, so the damage rule covers melee and projectiles alike.
    /// </summary>
    public class MirrorPuppetGlobalItem : GlobalItem
    {
        public override void ModifyWeaponDamage(Item item, Player player, ref StatModifier damage)
        {
            if (!MirrorPuppetSwap.Active || MirrorPuppetSwap.Current == null)
            {
                return;
            }

            MirrorPuppetSwap.Current.ModifySwapWeaponDamage(item, player, ref damage);
        }

        public override void ModifyWeaponCrit(Item item, Player player, ref float crit)
        {
            if (!MirrorPuppetSwap.Active || MirrorPuppetSwap.Current == null)
            {
                return;
            }

            MirrorPuppetSwap.Current.ModifySwapWeaponCrit(item, player, ref crit);
        }

        public override void MeleeEffects(Item item, Player player, Rectangle hitbox)
        {
            // The final melee hitbox of this ItemCheck (called from ItemCheck_EmitUseVisuals, only when vanilla would
            // attack with it). The melee clock decides whether it counts as a strike.
            if (MirrorPuppetSwap.Active)
            {
                MirrorPuppetSwap.CapturedMeleeHitbox = hitbox;
                MirrorPuppetSwap.HasCapturedMeleeHitbox = true;
            }
        }

        public override bool? CanHitNPC(Item item, Player player, NPC target)
        {
            bool blockedBySwap = MirrorPuppetSwap.Active && MirrorPuppetSwap.Current != null && !MirrorPuppetSwap.Current.SwapHitsAsOwner;

            if (blockedBySwap)
            {
                return false;
            }

            return null;
        }

        public override bool CanHitPvp(Item item, Player player, Player target)
        {
            bool blockedBySwap = MirrorPuppetSwap.Active && MirrorPuppetSwap.Current != null && !MirrorPuppetSwap.Current.SwapHitsAsOwner;

            if (blockedBySwap)
            {
                return false;
            }

            return true;
        }

        public override bool CanConsumeAmmo(Item weapon, Item ammo, Player player)
        {
            if (MirrorPuppetSwap.Active)
            {
                return false;
            }

            return base.CanConsumeAmmo(weapon, ammo, player);
        }

    }

    /// <summary>
    /// Tags every projectile a puppet creates — directly during a swap, or as a child/on-hit spawn of a
    /// tagged projectile — so later phases can override its damage and keep it out of the player's EMA.
    /// Healing projectiles (vampire/spirit heal) from tagged sources are removed: puppet hits never heal the player.
    /// </summary>
    public class MirrorPuppetProjectile : GlobalProjectile
    {
        public override bool InstancePerEntity => true;

        public bool FromMirrorPuppet;

        /// <summary>NPC index of the puppet this projectile belongs to, or -1. Lets its AI run inside that puppet's
        /// swap. Local to the owner's client (OnSpawn only runs where the projectile was created).</summary>
        public int PuppetNpcIndex = -1;

        public override void OnSpawn(Projectile projectile, IEntitySource source)
        {
            bool spawnedDuringSwap = MirrorPuppetSwap.Active;
            Entity parentEntity = null;

            // EntitySource_OnHit derives from EntitySource_Parent (Entity = the attacker), so this also
            // catches on-hit spawns such as vampire-heal projectiles.
            if (source is EntitySource_Parent parentSource)
            {
                parentEntity = parentSource.Entity;
            }

            MirrorPuppetProjectile parentTag = null;

            if (parentEntity is Projectile parentProjectile)
            {
                parentTag = parentProjectile.GetGlobalProjectile<MirrorPuppetProjectile>();
            }

            bool parentIsTagged = parentTag != null && parentTag.FromMirrorPuppet;

            if (!spawnedDuringSwap && !parentIsTagged)
            {
                return;
            }

            FromMirrorPuppet = true;

            if (spawnedDuringSwap)
            {
                MirrorPuppetSwap.ProjectilesSpawned++;
            }

            // Spawned inside a swap → the puppet running it; spawned by a tagged projectile → inherit its puppet.
            if (spawnedDuringSwap && MirrorPuppetSwap.Current != null)
            {
                PuppetNpcIndex = MirrorPuppetSwap.Current.NPC.whoAmI;
            }
            else if (parentIsTagged)
            {
                PuppetNpcIndex = parentTag.PuppetNpcIndex;
            }

            bool isHealProjectile = projectile.type == ProjectileID.VampireHeal || projectile.type == ProjectileID.SpiritHeal;

            if (isHealProjectile)
            {
                projectile.active = false;
                return;
            }

            // Hostile mirror puppets: the real projectile, flipped to hurt players instead of NPCs. Children of a
            // flipped projectile inherit the flip and its per-hit cap.
            bool fromHostileSwap = spawnedDuringSwap && MirrorPuppetSwap.Current is HostileMirrorPuppetNPC;

            if (fromHostileSwap)
            {
                HostileToPlayers = true;
                HitCapFraction = HostileMirrorPuppetNPC.GetHitCapFraction(MirrorPuppetSwap.Current.CurrentWeapon);
            }
            else if (parentIsTagged && parentTag.HostileToPlayers)
            {
                HostileToPlayers = true;
                HitCapFraction = parentTag.HitCapFraction;
            }

            if (HostileToPlayers)
            {
                projectile.hostile = true;
                projectile.friendly = false;
            }
        }

        // ── Hostile flip (HostileMirrorPuppetNPC.cs) ──────────────────────────────────────────────────

        /// <summary>Flipped hostile: hurts players (vanilla checks every client's own player against hostile
        /// projectiles, whoever owns them), never NPCs. Synced in <see cref="SendExtraAI"/>.</summary>
        public bool HostileToPlayers;

        /// <summary>Per-hit cap as a fraction of the victim's max HP, from the firing weapon's weight (§12.4).</summary>
        public float HitCapFraction;

        public override void PostAI(Projectile projectile)
        {
            // hostile/friendly are NOT synced by vanilla and some AIs rewrite them, so every machine re-asserts the
            // flip each tick. PostAI runs right before Projectile.Damage in the same update.
            if (HostileToPlayers)
            {
                projectile.hostile = true;
                projectile.friendly = false;
            }
        }

        public override void SendExtraAI(Projectile projectile, BitWriter bitWriter, BinaryWriter binaryWriter)
        {
            bitWriter.WriteBit(HostileToPlayers);

            if (HostileToPlayers)
            {
                // The puppet index lets the victim's client name the killer; the cap travels as whole percent.
                binaryWriter.Write((short)PuppetNpcIndex);
                binaryWriter.Write((byte)Math.Round(HitCapFraction * 100f));
            }
        }

        public override void ReceiveExtraAI(Projectile projectile, BitReader bitReader, BinaryReader binaryReader)
        {
            bool hostileToPlayers = bitReader.ReadBit();

            if (!hostileToPlayers)
            {
                return;
            }

            HostileToPlayers = true;
            FromMirrorPuppet = true;
            PuppetNpcIndex = binaryReader.ReadInt16();
            HitCapFraction = binaryReader.ReadByte() / 100f;
            projectile.hostile = true;
            projectile.friendly = false;
        }
    }
}

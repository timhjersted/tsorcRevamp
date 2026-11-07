using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Content.Projectiles;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.Content.Items.Weapons.Melee.Broadswords
{
    /// <summary>
    /// One cut of a greatsword combo. Angles are facing-space radians: 0 = level forward, negative = raised,
    /// positive = lowered. Each step starts where the previous step finished, so the combo flows without snaps.
    /// </summary>
    public struct GreatswordStep
    {
        public float LoadedAngle;     // where the windup ends and the cut begins
        public float FinishAngle;     // where the cut settles; also the next step's starting angle
        public float WindupFraction;  // share of the step spent winding up
        public float AccelFraction;   // share of the cut spent speeding up before the long deceleration
        public float StartSpeed;      // cut speed on its first tick as a fraction of peak (0 = from rest)
        public float DecelPower;      // higher = sharper drop from peak speed into a longer, gentler settle
        public float DurationScale;   // multiplies the weapon's base swing ticks
        public float DamageMultiplier;
        public bool Slam;             // finisher that meets the ground: impact effects + Greatsword.OnSlamImpact
    }

    /// <summary>The three cuts a greatsword combo is built from. The values index a weapon's ComboSteps array.</summary>
    public enum GreatswordCut
    {
        Overhead = 0,
        Rising = 1,
        Slam = 2
    }

    /// <summary>
    /// Pure swing maths shared by GreatswordSwing and the offline SwingPreview harness (--greatsword).
    /// </summary>
    public static class GreatswordMotion
    {
        /// <summary>Longest combo: rising, overhead, rising, slam. One opened with an overhead is three swings.</summary>
        public const int MaxComboSwings = 4;

        /// <summary>The slam can land from this combo position on (0-based: the third swing).</summary>
        public const int FirstSlamPosition = 2;

        /// <summary>Swing codes packed beside the weapon type in GreatswordSwing ai[0]: position (4) x cut (3) x entry cut (3).</summary>
        public const int SwingCodeCount = MaxComboSwings * 3 * 3;

        public const int MinimumSwingTicks = 8;

        // Damage stops once this share of the cut's arc is covered; the rest is the slow settle into the finish.
        public const float LiveEndTravel = 0.9f;

        // A slam only checks for the ground once the blade points forward at about level or lower (facing-space
        // radians; -0.35 is ~20 deg above level). A long blade overhead or behind otherwise clips the ceiling or
        // a back wall at the start of the cut and reads as an instant landing.
        public const float SlamGroundCheckFromAngle = -0.35f;

        public const float Carry = 0.25f;
        public const float Low = 0.18f;
        public const float High = -2.10f;

        // [0] overhead cut, [1] rising cut, [2] heavy slam (indexed by GreatswordCut). [0] and [1] give their cut's
        // ANGLES; their TIMING belongs to the combo position instead (see BuildSwing). The slam has the longest windup
        // and the softest start.
        public static readonly GreatswordStep[] DefaultSteps =
        {
            new GreatswordStep
            {
                LoadedAngle = High,
                FinishAngle = Low,
                WindupFraction = 0.30f,
                AccelFraction = 0.12f,
                StartSpeed = 0.25f,
                DecelPower = 2.5f,
                DurationScale = 1f,
                DamageMultiplier = 1f
            },
            new GreatswordStep
            {
                LoadedAngle = 0.36f,
                FinishAngle = High,
                WindupFraction = 0.20f,
                AccelFraction = 0.12f,
                StartSpeed = 0.25f,
                DecelPower = 2.5f,
                DurationScale = 1f,
                DamageMultiplier = 1f
            },
            new GreatswordStep
            {
                LoadedAngle = -2.88f,
                FinishAngle = Carry,
                WindupFraction = 0.36f,
                AccelFraction = 0.30f,
                StartSpeed = 0.05f,
                DecelPower = 2f,
                DurationScale = 1.1f,
                DamageMultiplier = 2f,
                Slam = true
            }
        };

        /// <summary>
        /// Which cut comes next. Overhead and rising cuts alternate, and a combo opens opposite to the last swing made
        /// (a slam counts as overhead), so the blade always carries on from where it stopped. The first overhead at the
        /// third swing or later is the slam, ending the combo: opened overhead = overhead, rising, slam; opened rising =
        /// rising, overhead, rising, slam. After a slam the next combo opens rising, so chained combos are four swings.
        /// </summary>
        public static GreatswordCut NextCut(GreatswordCut lastCut, int position)
        {
            bool lastWasRising = lastCut == GreatswordCut.Rising;

            if (!lastWasRising)
            {
                return GreatswordCut.Rising;
            }

            if (position >= FirstSlamPosition)
            {
                return GreatswordCut.Slam;
            }

            return GreatswordCut.Overhead;
        }

        /// <summary>
        /// The step data for one swing. A slam is steps[2] as authored. A cut takes its arc (loaded and finish angles)
        /// from its own entry, steps[0] overhead or steps[1] rising, but its timing and damage from the combo position:
        /// the opener plays steps[0]'s cadence, every later cut steps[1]'s, whichever way it swings.
        /// </summary>
        public static GreatswordStep BuildSwing(GreatswordStep[] steps, GreatswordCut cut, int position)
        {
            if (cut == GreatswordCut.Slam)
            {
                return steps[(int)GreatswordCut.Slam];
            }

            int timingIndex = 1;

            if (position == 0)
            {
                timingIndex = 0;
            }

            GreatswordStep arc = steps[(int)cut];
            GreatswordStep swing = steps[timingIndex];
            swing.LoadedAngle = arc.LoadedAngle;
            swing.FinishAngle = arc.FinishAngle;
            swing.Slam = false;
            return swing;
        }

        /// <summary>
        /// Share of the cut's arc covered at cutProgress (0..1). Speed ramps linearly from StartSpeed to peak over
        /// AccelFraction, then falls as (1 - u)^DecelPower, so the blade decelerates all the way into its finish
        /// instead of stopping dead and holding. This is the integral of that speed curve, normalised to end at 1.
        /// </summary>
        public static float CutTravel(GreatswordStep step, float cutProgress)
        {
            float time = MathHelper.Clamp(cutProgress, 0f, 1f);
            float accel = MathHelper.Clamp(step.AccelFraction, 0.01f, 0.95f);
            float start = MathHelper.Clamp(step.StartSpeed, 0f, 1f);
            float power = Math.Max(0.5f, step.DecelPower);

            float accelArea = accel * (1f + start) / 2f;
            float decelArea = (1f - accel) / (power + 1f);
            float totalArea = accelArea + decelArea;

            float covered;
            if (time < accel)
            {
                covered = start * time + (1f - start) * time * time / (2f * accel);
            }
            else
            {
                float decelProgress = (time - accel) / (1f - accel);
                float remaining = MathF.Pow(1f - decelProgress, power + 1f);
                covered = accelArea + (1f - accel) * (1f - remaining) / (power + 1f);
            }

            return covered / totalArea;
        }

        /// <summary>Facing-space blade angle at progress (0..1) through a swing that starts from entry (the previous
        /// swing's finish angle).</summary>
        public static float Angle(GreatswordStep current, float entry, float progress)
        {
            // Windup: smooth in-and-out into the loaded pose, so the swing starts with a little acceleration.
            if (progress < current.WindupFraction)
            {
                float windupProgress = progress / current.WindupFraction;
                return MathHelper.SmoothStep(entry, current.LoadedAngle, windupProgress);
            }

            float cutProgress = (progress - current.WindupFraction) / (1f - current.WindupFraction);
            float travel = CutTravel(current, cutProgress);
            return MathHelper.Lerp(current.LoadedAngle, current.FinishAngle, travel);
        }

        public static bool Live(GreatswordStep step, float progress)
        {
            if (progress <= step.WindupFraction)
            {
                return false;
            }

            float cutProgress = (progress - step.WindupFraction) / (1f - step.WindupFraction);
            return CutTravel(step, cutProgress) < LiveEndTravel;
        }

        /// <summary>
        /// Back-arm composite rotation that puts the rear hand on the handle above the front hand (two-handed grip:
        /// front fist at the pommel, rear fist up by the guard). Inverts vanilla's full-stretch back-hand ellipse
        /// (10 x 12 px around the shoulder at (6 * direction, -2) from MountedCenter) and picks the reachable handle
        /// point nearest the preferred spot. Offsets are world px from MountedCenter; bladeAxis is the unit
        /// grip-to-tip direction; handleLength is grip-to-guard in world px; error is the leftover gap in px.
        /// </summary>
        public static float BackArmRotation(Vector2 frontHandOffset, Vector2 bladeAxis, int direction, float handleLength, out float error)
        {
            Vector2 shoulder = new Vector2(6f * direction, -2f);
            float preferredDistance = handleLength * 0.85f;
            float bestCost = float.MaxValue;
            float chosenRotation = 0f;
            error = float.MaxValue;

            // Distance slides from the front fist up the handle toward the guard.
            for (float distance = 0f; distance <= handleLength; distance += 0.5f)
            {
                Vector2 desired = frontHandOffset + bladeAxis * distance;
                Vector2 gap = desired - shoulder;
                float theta = MathF.Atan2(gap.Y / 12f, gap.X / 10f);
                Vector2 reached = shoulder + new Vector2(MathF.Cos(theta) * 10f, MathF.Sin(theta) * 12f);
                float miss = Vector2.Distance(reached, desired);
                float cost = miss * 100f + Math.Abs(distance - preferredDistance) * 0.01f;

                if (cost < bestCost)
                {
                    bestCost = cost;
                    chosenRotation = theta - MathHelper.PiOver2;
                    error = miss;
                }
            }

            return chosenRotation;
        }
    }

    public abstract class Greatsword : ModItem
    {
        // Coordinates in the inventory sprite, before the item scale is applied. Grip is the front fist, at the
        // bottom of the handle so the whole blade extends beyond the hand (vanilla's swing pins the pommel end
        // there too); HiltTop is the top of the handle under the guard, where the rear hand reaches.
        protected internal abstract Vector2 Grip { get; }
        protected internal abstract Vector2 HiltTop { get; }
        protected internal abstract Vector2 BladeHeel { get; }
        protected internal abstract Vector2 CuttingEdge { get; }
        protected internal abstract Vector2 BladeTip { get; }

        /// <summary>Overhead cut, rising cut, slam (indexed by GreatswordCut; see GreatswordMotion.BuildSwing for how a
        /// combo combines them). Override to give a sword its own arcs, easing and timings.</summary>
        public virtual GreatswordStep[] ComboSteps => GreatswordMotion.DefaultSteps;

        /// <summary>Ticks of a DurationScale 1 step before melee speed.</summary>
        public virtual int BaseSwingTicks => Item.useAnimation;

        /// <summary>Use Moonlight's Slash shader with the rendered heel/tip and a short active-cut
        /// lifetime. Set the item's slashColor; blade geometry and player scale are automatic.</summary>
        public virtual bool UsesBladeSlash => false;
        public virtual bool UsesSlashTrail => UsesBladeSlash;

        /// <summary>Owner only. The slam's blade met the ground (or finished its cut in the air).</summary>
        public virtual void OnSlamImpact(Projectile swing, Player player, Vector2 impact, bool hitGround)
        {
        }

        /// <summary>Every client except the server, every tick of a swing.</summary>
        public virtual void SwingVisuals(Projectile swing, Player player, Vector2 hand, Vector2 tip, bool live)
        {
        }

        public virtual void OnSwingHitNPC(Projectile swing, NPC target, int position)
        {
        }

        /// <summary>Owner only. The cut has just become able to hit (once per swing). position = place in the combo, 0-based.</summary>
        public virtual void OnStrikeStart(Projectile swing, Player player, int position)
        {
        }

        /// <summary>
        /// Ticks the use button must stay held from the swing's first tick to trade the swing for StartHoldAction.
        /// 0 = no hold action (holding just plays the combo). Keep it within the steps' windups so the cut never
        /// lands first.
        /// </summary>
        public virtual int HoldActionTicks => 0;

        /// <summary>Owner only. Replaces the swing in progress, which is killed straight after; the combo resets.</summary>
        public virtual void StartHoldAction(Projectile swing, Player player)
        {
        }

        public override void SetDefaults()
        {
            Item.DamageType = DamageClass.Melee;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.useTime = 40;
            Item.useAnimation = 40;
            Item.useLimitPerAnimation = 1;
            Item.autoReuse = true;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.shoot = ModContent.ProjectileType<GreatswordSwing>();
            Item.shootSpeed = 1f;
        }

        public override bool CanUseItem(Player player)
        {
            return player.ownedProjectileCounts[ModContent.ProjectileType<GreatswordSwing>()] == 0;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            if (player.whoAmI != Main.myPlayer)
            {
                return false;
            }

            int facing = player.direction;
            if (velocity.X != 0f)
            {
                facing = Math.Sign(velocity.X);
            }

            StartSwing(player, source, damage, knockback, facing);
            return false;
        }

        /// <summary>
        /// Owner only. Spawns the next swing of the combo. Its position, cut and entry cut (the previous swing, whose
        /// finish it winds up from) and its duration ride in the projectile's ai[], so every client plays the same
        /// swing from the spawn packet.
        /// </summary>
        internal void StartSwing(Player player, IEntitySource source, int damage, float knockback, int facing)
        {
            GreatswordComboPlayer combo = player.GetModPlayer<GreatswordComboPlayer>();
            int position = combo.NextPosition;
            GreatswordCut entryCut = combo.LastCut;
            GreatswordCut cut = GreatswordMotion.NextCut(entryCut, position);
            GreatswordStep swing = GreatswordMotion.BuildSwing(ComboSteps, cut, position);

            float scaledTicks = BaseSwingTicks * swing.DurationScale;
            int duration = Math.Max(GreatswordMotion.MinimumSwingTicks, CombinedHooks.TotalAnimationTime(scaledTicks, player, Item));

            // ai[0] = weapon type * SwingCodeCount + swing code; the code is position + 4 * (cut + 3 * entry cut).
            int cutCode = (int)cut + 3 * (int)entryCut;
            int swingCode = position + GreatswordMotion.MaxComboSwings * cutCode;
            int packedSwing = Item.type * GreatswordMotion.SwingCodeCount + swingCode;

            int projectileIndex = Projectile.NewProjectile(source, player.MountedCenter, Vector2.UnitX * facing,
                ModContent.ProjectileType<GreatswordSwing>(), damage, knockback, player.whoAmI, packedSwing, 0f, duration);

            if (projectileIndex >= 0 && projectileIndex < Main.maxProjectiles)
            {
                Main.projectile[projectileIndex].CritChance = (int)player.GetWeaponCrit(Item);
                combo.ResetCombo();
                combo.LastCut = cut;
            }
        }
    }

    public sealed class GreatswordComboPlayer : ModPlayer
    {
        internal struct ComboState
        {
            internal int WeaponType;
            internal int WeaponSlot;
            internal int NextPosition;
            internal ulong Expires;
            internal bool SwingQueued;
            internal GreatswordCut LastCut;
        }

        private int weaponType;
        private int weaponSlot;
        private int nextPosition;
        private ulong comboExpires;
        private bool swingQueued;

        /// <summary>The last swing started, kept across combos (a dropped combo, a dodge, a weapon swap): the next swing
        /// is its opposite. Starts as Slam, as if the blade rests low, so the first swing ever rises.</summary>
        internal GreatswordCut LastCut { get; set; } = GreatswordCut.Slam;

        // The mimic borrows this ModPlayer too. Save all combo state so its cuts and queued inputs cannot
        // advance or consume the real player's combo (or another puppet's combo).
        internal ComboState CaptureCombo()
        {
            ComboState state = new ComboState();
            state.WeaponType = weaponType;
            state.WeaponSlot = weaponSlot;
            state.NextPosition = nextPosition;
            state.Expires = comboExpires;
            state.SwingQueued = swingQueued;
            state.LastCut = LastCut;
            return state;
        }

        internal void RestoreCombo(ComboState state)
        {
            weaponType = state.WeaponType;
            weaponSlot = state.WeaponSlot;
            nextPosition = state.NextPosition;
            comboExpires = state.Expires;
            swingQueued = state.SwingQueued;
            LastCut = state.LastCut;
        }

        internal int NextPosition
        {
            get
            {
                if (Player.HeldItem.type == weaponType && Player.selectedItem == weaponSlot && Main.GameUpdateCount <= comboExpires)
                {
                    return nextPosition;
                }

                return 0;
            }
        }

        /// <summary>A swing played to its end: the next tap within 45 ticks continues the combo, unless it was the
        /// slam, which ends it (position back to 0).</summary>
        internal void CompleteSwing(int position, GreatswordCut cut)
        {
            weaponType = Player.HeldItem.type;
            weaponSlot = Player.selectedItem;
            nextPosition = position + 1;

            if (cut == GreatswordCut.Slam)
            {
                nextPosition = 0;
            }

            comboExpires = Main.GameUpdateCount + 45;
        }

        /// <summary>Owner only. A paid item use arrived mid-swing: start the next step the moment this one ends.</summary>
        internal void QueueSwing()
        {
            swingQueued = true;
        }

        internal bool ConsumeQueuedSwing()
        {
            bool queued = swingQueued;
            swingQueued = false;
            return queued;
        }

        internal void ResetCombo()
        {
            weaponType = 0;
            nextPosition = 0;
        }

        public override void ResetEffects()
        {
            bool dodging = Player.GetModPlayer<tsorcRevampPlayer>().isDodging;
            if (dodging)
            {
                swingQueued = false;
            }

            if (nextPosition != 0 && (NextPosition == 0 || dodging))
            {
                ResetCombo();
            }
        }

        public override void UpdateDead()
        {
            swingQueued = false;
            ResetCombo();
        }
    }

    public sealed class GreatswordSwing : ModProjectile, Projectiles.VFX.IBladeSlashSource
    {
        private static readonly SoundStyle SlamSound = new("tsorcRevamp/Sounds/HollowKnight/false_knight_land_1st_time")
        {
            Volume = 0.45f
        };

        // ai[0] packs weapon type and swing code (Greatsword.StartSwing), ai[1] counts elapsed ticks, ai[2] is the
        // swing's duration. Facing rides in velocity.X. Everything a client needs to replay the swing is in the spawn packet.
        private int WeaponType => (int)Projectile.ai[0] / GreatswordMotion.SwingCodeCount;
        private int SwingCode => (int)Projectile.ai[0] % GreatswordMotion.SwingCodeCount;
        private int Position => SwingCode % GreatswordMotion.MaxComboSwings;
        private GreatswordCut Cut => (GreatswordCut)(SwingCode / GreatswordMotion.MaxComboSwings % 3);
        private GreatswordCut EntryCut => (GreatswordCut)(SwingCode / GreatswordMotion.MaxComboSwings / 3);

        private Vector2 hand;
        private Vector2 heel;
        private Vector2 edge;
        private Vector2 tip;
        private Vector2 previousHeel;
        private Vector2 previousEdge;
        private Vector2 previousTip;
        private Vector2 grip;
        private float drawRotation;
        private float drawScale;
        private bool flipSprite;
        private bool poseReady;
        private bool strikeActive;
        private bool playedCut;
        private bool slamResolved;
        // Facing-space angle the slam's blade met the ground at; NaN until then. The rest of the step stays planted.
        private float slamStopAngle = float.NaN;
        private bool spawnedTrail;
        private bool strikeStarted;
        private bool holdResolved;
        public Color SwordSlashColor { get; private set; }
        public int SwordSlashSequence => 0;

        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(InvisibleNothingProj));

        public override void SetDefaults()
        {
            Projectile.width = 320;
            Projectile.height = 320;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.ownerHitCheck = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override bool ShouldUpdatePosition() => false;

        public override bool? CanCutTiles() => false;

        public override bool? CanDamage()
        {
            if (!strikeActive)
            {
                return false;
            }

            return null;
        }

        public override void AI()
        {
            Player player = Main.player[Projectile.owner];
            bool isOwner = Projectile.owner == Main.myPlayer;
            if (!player.active || player.dead)
            {
                Projectile.Kill();
                return;
            }

            Item swingItem = player.HeldItem;
            if (isOwner)
            {
                tsorcRevampPlayer modPlayer = player.GetModPlayer<tsorcRevampPlayer>();
                if (swingItem.type != WeaponType || player.noItems || player.CCed || modPlayer.isDodging || modPlayer.isSwordflipping)
                {
                    Projectile.Kill();
                    return;
                }
            }
            else if (swingItem.type != WeaponType)
            {
                // A swing packet can arrive before the remote player's selected slot is updated.
                ContentSamples.ItemsByType.TryGetValue(WeaponType, out swingItem);
            }

            if (swingItem?.ModItem is not Greatsword weapon)
            {
                Projectile.Kill();
                return;
            }

            GreatswordStep[] steps = weapon.ComboSteps;
            SwordSlashColor = swingItem.GetGlobalItem<tsorcInstancedGlobalItem>().slashColor;
            GreatswordStep step = GreatswordMotion.BuildSwing(steps, Cut, Position);
            float entryAngle = steps[(int)EntryCut].FinishAngle;
            float duration = Math.Max(GreatswordMotion.MinimumSwingTicks, Projectile.ai[2]);
            float age = Projectile.ai[1];

            // Hold action: letting go at any point makes this a plain swing; still holding once HoldActionTicks
            // have passed trades the swing for the weapon's hold move (Artorias's flip leap).
            if (isOwner && weapon.HoldActionTicks > 0 && !holdResolved)
            {
                if (!player.controlUseItem)
                {
                    holdResolved = true;
                }
                else if (age >= weapon.HoldActionTicks)
                {
                    holdResolved = true;
                    player.GetModPlayer<GreatswordComboPlayer>().ResetCombo();
                    weapon.StartHoldAction(Projectile, player);
                    Projectile.Kill();
                    return;
                }
            }
            if (age + 1f >= duration)
            {
                FinishSwing(player, weapon, isOwner);
                return;
            }

            Projectile.timeLeft = 2;
            Projectile.Center = player.MountedCenter;
            player.heldProj = Projectile.whoAmI;

            int facing = 1;
            if (Projectile.velocity.X < 0f)
            {
                facing = -1;
            }

            player.ChangeDir(facing);
            float progress = (age + 1f) / duration;
            float angle = GreatswordMotion.Angle(step, entryAngle, progress);

            // A slam that has met the ground stops there (slams cut toward larger angles) instead of sweeping on
            // through the floor to its authored finish.
            if (step.Slam && !float.IsNaN(slamStopAngle) && angle > slamStopAngle)
            {
                angle = slamStopAngle;
            }

            Vector2 axis = new(MathF.Cos(angle) * facing, MathF.Sin(angle) * player.gravDir);
            float worldAngle = axis.ToRotation();
            Projectile.rotation = worldAngle;

            // Front arm held straight out along the blade: vanilla composite space has 0 = arm straight down.
            float armRotation = worldAngle - MathHelper.PiOver2;
            player.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, armRotation);

            Vector2 lastHand = hand;
            hand = player.GetFrontHandPosition(Player.CompositeArmStretchAmount.Full, armRotation);
            grip = weapon.Grip;
            drawScale = player.GetAdjustedItemScale(swingItem);
            flipSprite = facing * player.gravDir < 0f;

            // Rear hand on the handle above the front fist. GetBackHandPosition has no gravity flip, so an
            // upside-down player swings one-handed rather than reaching for a mirrored hilt.
            if (player.gravDir == 1f)
            {
                float handleLength = Vector2.Distance(weapon.Grip, weapon.HiltTop) * drawScale;
                Vector2 frontHandOffset = hand - player.MountedCenter;
                float backArmRotation = GreatswordMotion.BackArmRotation(frontHandOffset, axis, facing, handleLength, out _);
                player.SetCompositeArmBack(true, Player.CompositeArmStretchAmount.Full, backArmRotation);
            }

            Vector2 bladeAxis = weapon.BladeTip - grip;
            if (flipSprite)
            {
                bladeAxis.Y = -bladeAxis.Y;
            }

            drawRotation = worldAngle - bladeAxis.ToRotation();
            previousHeel = heel;
            previousEdge = edge;
            previousTip = tip;
            heel = BladePosition(weapon.BladeHeel);
            edge = BladePosition(weapon.CuttingEdge);
            tip = BladePosition(weapon.BladeTip);
            if (!poseReady || Vector2.DistanceSquared(lastHand, hand) > 64f * 64f)
            {
                previousHeel = heel;
                previousEdge = edge;
                previousTip = tip;
            }

            poseReady = true;
            bool wasActive = strikeActive;
            strikeActive = GreatswordMotion.Live(step, progress);

            if (strikeActive && !strikeStarted)
            {
                strikeStarted = true;
                if (isOwner)
                {
                    weapon.OnStrikeStart(Projectile, player, Position);
                }
            }

            if (strikeActive && !spawnedTrail && isOwner && weapon.UsesSlashTrail)
            {
                // ai[0] identifies this swing; Slash reads IBladeSlashSource for exact blade geometry/lifetime.
                spawnedTrail = true;
                float bladeDriven = weapon.UsesBladeSlash ? 1f : 0f;
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), player.Center, Vector2.Zero,
                    ModContent.ProjectileType<Projectiles.VFX.Slash>(), 0, 0f, Projectile.owner,
                    Projectile.identity + 1, bladeDriven);
            }

            if (!Main.dedServ)
            {
                if (strikeActive && !playedCut)
                {
                    playedCut = true;
                    SoundEngine.PlaySound(SoundID.Item1 with { Pitch = -0.32f }, hand);
                }

                weapon.SwingVisuals(Projectile, player, hand, tip, strikeActive);
            }

            // The slam lands on the first tick its tip touches the ground. If it is still airborne when the strike
            // window closes, the finisher resolves at the tip instead so the payoff never silently goes missing.
            bool bladeForwardAndDown = angle >= GreatswordMotion.SlamGroundCheckFromAngle;
            if (step.Slam && strikeActive && !slamResolved && bladeForwardAndDown)
            {
                TryResolveSlam(player, weapon, isOwner, true, angle);
            }
            else if (step.Slam && wasActive && !strikeActive && !slamResolved)
            {
                TryResolveSlam(player, weapon, isOwner, false, angle);
            }

            Projectile.ai[1]++;
        }

        private void FinishSwing(Player player, Greatsword weapon, bool isOwner)
        {
            if (!isOwner)
            {
                // Hold the last pose; the owner's kill or next swing packet replaces this one.
                return;
            }

            GreatswordComboPlayer combo = player.GetModPlayer<GreatswordComboPlayer>();
            combo.CompleteSwing(Position, Cut);
            bool queued = combo.ConsumeQueuedSwing();
            Projectile.Kill();

            if (queued)
            {
                int facing = player.direction;
                if (Main.MouseWorld.X != player.Center.X)
                {
                    facing = Math.Sign(Main.MouseWorld.X - player.Center.X);
                }

                int damage = player.GetWeaponDamage(weapon.Item);
                float knockback = player.GetWeaponKnockback(weapon.Item);
                weapon.StartSwing(player, Projectile.GetSource_FromThis(), damage, knockback, facing);
            }
        }

        public override void OnKill(int timeLeft)
        {
            // A swing cut short (dodge, weapon swap, dash) drops any tap queued behind it; a finished swing has
            // already consumed its queue in FinishSwing, so this is a no-op there.
            if (Projectile.owner == Main.myPlayer)
            {
                Main.player[Projectile.owner].GetModPlayer<GreatswordComboPlayer>().ConsumeQueuedSwing();
            }
        }

        private Vector2 BladePosition(Vector2 pixel)
        {
            Vector2 offset = (pixel - grip) * drawScale;
            if (flipSprite)
            {
                offset.Y = -offset.Y;
            }

            return hand + offset.RotatedBy(drawRotation);
        }

        public bool TryGetSwordSlashPose(out Vector2 bladeHand, out Vector2 bladeHeel, out Vector2 bladeTip)
        {
            bladeHand = hand;
            bladeHeel = heel;
            bladeTip = tip;
            return poseReady && strikeActive && !holdResolved && float.IsNaN(slamStopAngle);
        }

        private void TryResolveSlam(Player player, Greatsword weapon, bool isOwner, bool requireGround, float currentAngle)
        {
            // Test the last few pixels under the tip for terrain or a platform, including a tip that is already
            // touching solid ground.
            float gravity = player.gravDir;
            Vector2 probeStart = tip - new Vector2(1f, 5f * gravity);
            Vector2 probe = new(0f, 14f * gravity);
            Vector2 travel = Collision.TileCollision(probeStart, probe, 2, 2, gravDir: (int)gravity);
            Vector2 impact = probeStart + travel;
            bool hitGround = travel.Y != probe.Y;

            if (!hitGround && Collision.SolidCollision(tip - Vector2.One, 2, 2))
            {
                impact = tip;
                hitGround = true;
            }

            if (!hitGround && requireGround)
            {
                return;
            }

            if (!hitGround)
            {
                impact = tip;
            }

            slamResolved = true;
            if (hitGround)
            {
                slamStopAngle = currentAngle;
            }

            if (!Main.dedServ && hitGround)
            {
                SoundEngine.PlaySound(SlamSound, impact);
                UsefulFunctions.ScreenShake(impact, 3.5f, 10, distanceFalloff: 440f);
                for (int i = 0; i < 14; i++)
                {
                    Vector2 velocity = new(Main.rand.NextFloat(-3.4f, 3.4f), Main.rand.NextFloat(-3.8f, -0.9f) * gravity);
                    Dust.NewDustPerfect(impact, DustID.Stone, velocity, Scale: Main.rand.NextFloat(0.8f, 1.2f));
                }

                for (int i = 0; i < 8; i++)
                {
                    Vector2 velocity = new(Main.rand.NextFloat(-4.6f, 4.6f), Main.rand.NextFloat(-2.8f, -0.6f) * gravity);
                    Dust spark = Dust.NewDustPerfect(impact, DustID.BoneTorch, velocity, Scale: Main.rand.NextFloat(0.65f, 1f));
                    spark.noGravity = true;
                }
            }

            if (isOwner)
            {
                weapon.OnSlamImpact(Projectile, player, impact, hitGround);
            }
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            if (Main.player[Projectile.owner].HeldItem.ModItem is Greatsword weapon)
            {
                GreatswordStep swing = GreatswordMotion.BuildSwing(weapon.ComboSteps, Cut, Position);
                modifiers.SourceDamage *= swing.DamageMultiplier;
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Main.player[Projectile.owner].HeldItem.ModItem is Greatsword weapon)
            {
                weapon.OnSwingHitNPC(Projectile, target, Position);
            }
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (!strikeActive)
            {
                return false;
            }

            // Sweep both segments of the curved blade across the space between the two poses.
            Vector2 targetPosition = targetHitbox.TopLeft();
            Vector2 targetSize = targetHitbox.Size();
            float collisionPoint = 0f;
            for (int i = 0; i <= 8; i++)
            {
                float fraction = i / 8f;
                Vector2 sweptHeel = Vector2.Lerp(previousHeel, heel, fraction);
                Vector2 sweptEdge = Vector2.Lerp(previousEdge, edge, fraction);
                Vector2 sweptTip = Vector2.Lerp(previousTip, tip, fraction);
                if (Collision.CheckAABBvLineCollision(targetPosition, targetSize, sweptHeel, sweptEdge, 12f, ref collisionPoint)
                    || Collision.CheckAABBvLineCollision(targetPosition, targetSize, sweptEdge, sweptTip, 8f, ref collisionPoint))
                {
                    return true;
                }
            }

            return false;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (!poseReady)
            {
                return false;
            }

            Main.instance.LoadItem(WeaponType);
            Texture2D texture = TextureAssets.Item[WeaponType].Value;
            SpriteEffects effects = SpriteEffects.None;
            Vector2 origin = grip;
            if (flipSprite)
            {
                effects = SpriteEffects.FlipVertically;
                origin.Y = texture.Height - origin.Y;
            }

            Main.EntitySpriteDraw(texture, hand - Main.screenPosition, null, lightColor, drawRotation, origin, drawScale, effects);
            return false;
        }
    }
}

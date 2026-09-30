using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.GameContent.ItemDropRules;
using Terraria.Graphics.Effects;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;
using tsorcRevamp.Content.Items.Accessories.Defensive.Rings;
using tsorcRevamp.Content.Items.Accessories.Defensive.Rings.WolfRing;
using tsorcRevamp.Content.Items.Armor.Melee;
using tsorcRevamp.Content.Items.BossBags;
using tsorcRevamp.Content.Items.Materials;
using tsorcRevamp.Content.Items.Materials.Souls;
using tsorcRevamp.Content.Items.Weapons.Enemy;
using tsorcRevamp.Content.Items.Weapons.Melee.Broadswords;
using tsorcRevamp.Content.Projectiles;
using tsorcRevamp.Content.Projectiles.Enemy;
using tsorcRevamp.Content.Projectiles.Enemy.Weapons;
using tsorcRevamp.Content.Projectiles.Melee.Shortswords;
using tsorcRevamp.Content.Projectiles.VFX;
using tsorcRevamp.NPCs.AI;
using tsorcRevamp.NPCs.Puppets;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.NPCs.Bosses.SuperHardMode
{
    /// <summary>
    /// What Artorias and his spectral phantom (<see cref="ArtoriasPhantom"/>) share: the armour and greatsword
    /// loadout, movement speed, the swing tuning (arc endpoints, combo customisation, landing-timed slam) and
    /// the tracked void sword crescents. Boss-only systems (ring, novas, shield, special attacks) stay on
    /// <see cref="Artorias"/>. Abstract, so tModLoader never loads it as an NPC of its own.
    /// </summary>
    abstract class ArtoriasSwordsman : PuppetNPC
    {
        // ── Loadout: original dark-blue Artorias armor set ──────────────────────────
        protected override int HeadArmorItemType => ModContent.ItemType<ArtoriasHelmet>();
        protected override int BodyArmorItemType => ModContent.ItemType<ArtoriasArmor>();
        protected override int LegsArmorItemType => ModContent.ItemType<ArtoriasGreaves>();
        protected override float PuppetDrawScale => 1.1f;
        protected override Color PuppetSkinColor => new Color(20, 23, 38);

        protected override int MeleeWeaponItemType => ModContent.ItemType<EnemyArtoriasGreatsword>();
        protected override int RangedWeaponItemType => -1; // melee-only
        protected override int RangedDamage => 0; // unused, no ranged weapon

        protected override float TopSpeed => 2.4f;
        protected override float Acceleration => 0.12f;

        protected override WeaponArchetype MeleeArchetype => WeaponArchetype.Greatsword;
        protected override bool UseCompositeArmSwing => true;
        protected override bool MirrorMeleeSwingRotationByFacing => true;
        protected override bool UseSwingEasing => true;
        protected override bool UseLogicalMeleeTelegraphs => true;
        protected override bool UseAuthoredComboSwingClock => true;
        protected override float OverheadWindupOvershoot => 0.18f;
        // Plain one-shot overhead (an in-reach swing that isn't a combo): in 9 / out 21, k 8 -> 38°/t, live 12.2t
        // (~186°). 30t total on purpose: it must end with the greatsword's 30t useAnimation, which drives the Use frames.
        protected override WeightedSwing MeleeAttackCurve => new WeightedSwing(9, 21, 8f);
        protected override int MeleeAttackTicks => MeleeAttackCurve.TotalTicks;
        protected override int MeleeRecoveryTicks => 30;
        protected override int MeleeComboInterStepLingerTicks => 15;
        protected override int MeleeRecoveryLingerTicks => 30;
        // Hyper armor on every attack state, tells included (the base commits only post-flash strikes). Combo pauses are
        // covered in CustomizeMeleeCombo; Artorias.AI adds his bespoke states and then carves the one exception back
        // out: an attack's opening tell from neutral, up to the flash. Recoveries stay staggerable as punish windows.
        protected override bool HyperArmorDuringTelegraph => true;
        // Ground Pound resolves on real ground contact instead of a tick countdown: this enables the
        // predictive downswing (UpdateLeapSlamPose projects the landing a few frames ahead so the
        // blade arrives flat as the feet touch) and the OnLeapSlamLanded hook below.
        protected override bool UseLandingTimedLeapSlam => true;

        // Artorias never disengages to drink; zero charges prevents the healing intercept from
        // entering either FleeToHeal or Healing.
        protected override int EstusChargesMax => 0;

        // He keeps pursuing through a sword wind-up instead of planting: the engine's telegraph brake (0.8x/tick) is
        // skipped while the target is still ahead of his locked facing and more than WindupPursuitStopDistance away,
        // so the navigator's normal chase carries on. It still brakes once the target is behind him (a roll through
        // after the flash - no moonwalking) or already inside that gap (no walking into them). Every other melee
        // phase keeps the brake.
        const float WindupPursuitStopDistance = 48f;
        protected override bool SlowDownBeforeMelee
        {
            get
            {
                bool inSwordWindup = Phase == AttackPhase.MeleeComboTelegraph || Phase == AttackPhase.MeleeTelegraph;
                if (!inSwordWindup || !NPC.HasValidTarget)
                {
                    return true;
                }

                Player target = Main.player[NPC.target];
                float horizontalGap = target.Center.X - NPC.Center.X;
                bool targetAhead = Math.Sign(horizontalGap) == NPC.direction;
                bool roomToClose = Math.Abs(horizontalGap) > WindupPursuitStopDistance;
                bool keepPursuing = targetAhead && roomToClose;
                return !keepPursuing;
            }
        }

        /// <summary>Swing tempo for this wielder's combos (1 = Artorias). Multiplies every step's
        /// SwingSpeedMult (the authored arc clock) and divides the recovery and inter-step pause floor, so a
        /// faster wielder is faster everywhere without re-authoring the shared Greatsword table.</summary>
        protected virtual float ComboTempoMult => 1f;

        protected override void ModifyMeleeArcEndpoints(
            ComboMotion motion, ref float startRotation, ref float endRotation)
        {
            switch (motion)
            {
                // Weighted swings disarm under 30% of peak speed, so only ~0.8x of the envelope is live. Both
                // arcs are widened toward the player's broadsword (~224° envelope = ~180° live): the overhead at
                // its END (2.29, near the player's straight-down finish), the underhand at its low START, since
                // its end can't go past about -1.7 before the arm folds behind the head.
                case ComboMotion.OverheadArc:
                    startRotation = Math.Min(startRotation, -1.48f);
                    endRotation = 2.29f;
                    break;
                case ComboMotion.UnderhandArc:
                    startRotation = 2.05f;
                    endRotation = -1.55f;
                    break;
                case ComboMotion.HorizontalSweep:
                    startRotation = -1.08f;
                    endRotation = 1.18f;
                    break;
                case ComboMotion.VerticalChop:
                case ComboMotion.GroundSlam:
                    startRotation = Math.Min(startRotation, -1.58f);
                    endRotation = Math.Max(endRotation, 1.48f);
                    break;
            }
        }

        // ── Swing curves (attack-timing-design §3) ────────────────────────────────────────────────
        // Every Artorias sword swing is a Weighted strike: a short cubic ease-in, a fast peak, then an exponential
        // settle that stops being a hitbox under 30% of peak speed. Each has its own in/out/decay, so no two read
        // alike and none copy Gwyn's flurry (10/45 k7, 19°/t). peak = sweep / (in/3 + out * (1 - e^-k) / k).
        //                    envelope  in/out  k     peak    live
        //   Heavy Chop       216°      13/26   9     30°/t   16.5t ~190°  longest hang, heaviest drop
        //   Rising Slash     206°       6/22   5.5   34°/t   10.8t ~165°  snaps up, long soft settle
        //   Running Cleave   216°       5/26   6     36°/t   10.2t ~170°  lands straight off the dash
        //   plain overhead   216°       9/21   8     38°/t   12.2t ~186°  MeleeAttackCurve, above
        // Every live window is under the 22t roll, so each swing is separately rollable, and the tail is harmless
        // (punish window = tail + recovery). The phantom's 1.4x tempo shortens each step and HitWindowEnd is a
        // fraction of it, so the whole curve compresses proportionally.
        static readonly WeightedSwing HeavyChopCurve = new WeightedSwing(13, 26, 9f);
        static readonly WeightedSwing RisingSlashCurve = new WeightedSwing(6, 22, 5.5f);
        static readonly WeightedSwing RunningCleaveCurve = new WeightedSwing(5, 26, 6f);

        // Step-in per cut (MeleeComboStep.StepInDistance): px travelled across the step, peaking on the strike and
        // decaying with the blade, so he drives into each cut instead of planting (the engine's 0.65x/tick brake
        // stopped him dead within ~5t). Peak = px / (in/3 + out*(1-e^-k)/k) at 1x tempo:
        //   Heavy Chop      56px  -> 7.8 px/t    Rising Slash  40px -> 6.7 px/t    Running Cleave cut  48px -> 8.0 px/t
        // Most of it lands inside the live window, adding ~40-50px of reach; a roll away (~110px) still clears it.
        const float HeavyChopStepIn = 56f;
        const float RisingSlashStepIn = 40f;
        const float RunningCleaveStepIn = 48f;

        protected override void CustomizeMeleeCombo(ref MeleeCombo combo, float healthFraction)
        {
            // Artorias's committed rhythm (36t heavy / 30t light recovery, 30t inter-step pause floor),
            // divided by the tempo so a faster wielder recovers and re-engages proportionally sooner.
            int heavyRecoveryTicks = (int)Math.Round(36f / ComboTempoMult);
            int lightRecoveryTicks = (int)Math.Round(30f / ComboTempoMult);
            int pauseFloorTicks = (int)Math.Round(30f / ComboTempoMult);

            // Hyper armor holds through the inter-step pauses too, so a multi-step combo can't be staggered between cuts.
            combo.HyperArmor = true;

            combo.RecoveryTicks = lightRecoveryTicks;
            if (combo.HeavyCommit)
            {
                combo.RecoveryTicks = heavyRecoveryTicks;
            }

            if (combo.Steps == null)
            {
                return;
            }

            for (int i = 0; i < combo.Steps.Length; i++)
            {
                MeleeComboStep step = combo.Steps[i];

                // Only a non-1 tempo touches the swing clock, so Artorias's own authored values stay as-is.
                // An authored 0 means 1x, so it is folded in before scaling.
                if (ComboTempoMult != 1f)
                {
                    float authoredSwingSpeed = step.SwingSpeedMult > 0f ? step.SwingSpeedMult : 1f;
                    step.SwingSpeedMult = authoredSwingSpeed * ComboTempoMult;
                }

                // Every step but the last: the first non-damaging frames stay planted at contact, and the
                // rest of the pause cocks the sword into the next authored starting pose instead of snapping.
                if (i < combo.Steps.Length - 1)
                {
                    step.PostStepPause = Math.Max(step.PostStepPause, pauseFloorTicks);
                }

                combo.Steps[i] = step;
            }

            // "Ground Pound" (shared Greatsword combo table) plays as GroundSlam, which is just the
            // OverheadArc/VerticalChop pose with no jump - it read as a plain swing, not a pound, and
            // its 2.0x DamageMult was landing for ~380-400 against 80 defense. Retarget it onto
            // LeapSlam (jump toward the player, overhead pose, hit fires on landing) so it actually
            // pounds the ground, and cut the damage 60% (2.0x -> 0.8x) to match.
            if (combo.Name == "Ground Pound" && combo.Steps.Length > 0)
            {
                MeleeComboStep slam = combo.Steps[0];
                slam.Motion = ComboMotion.LeapSlam;
                slam.DamageMult = 0.8f;
                // GroundSlam's authored 24 ticks were sized for a chop from a standing pose. A leap
                // is airborne for 2 * LeapAttackUpSpeed / gravity = 2 * 9.5 / 0.3 = ~63 ticks before
                // it can touch down at all, so the step's timer used to expire roughly 39 ticks short
                // and resolve the "pound" in mid-air. 90 matches every other authored LeapSlam in the
                // repo and leaves headroom for the horizontal travel on top of the arc.
                slam.AttackTicks = 90;
                combo.Steps[0] = slam;
            }

            // The shared table's sword arcs, retimed onto Artorias's own Weighted curves (table above). The Motion
            // checks stop a later edit of the shared table from putting a sword curve on a thrust or a spin.
            if (combo.Name == "Heavy Chop" && combo.Steps.Length > 0 && combo.Steps[0].Motion == ComboMotion.OverheadArc)
            {
                ApplySwingCurve(ref combo.Steps[0], HeavyChopCurve);
                combo.Steps[0].StepInDistance = HeavyChopStepIn;
            }
            else if (combo.Name == "Rising Slash" && combo.Steps.Length > 0 && combo.Steps[0].Motion == ComboMotion.UnderhandArc)
            {
                ApplySwingCurve(ref combo.Steps[0], RisingSlashCurve);
                combo.Steps[0].StepInDistance = RisingSlashStepIn;
            }
            else if (combo.Name == "Running Cleave" && combo.Steps.Length > 1 && combo.Steps[1].Motion == ComboMotion.OverheadArc)
            {
                ApplySwingCurve(ref combo.Steps[1], RunningCleaveCurve);
                combo.Steps[1].StepInDistance = RunningCleaveStepIn;
            }
        }

        /// <summary>Puts a combo step on a Weighted curve: its length becomes the curve's, and the blade disarms once
        /// speed drops under 30% of peak (HitWindowEnd, which also fades the sword arc).</summary>
        static void ApplySwingCurve(ref MeleeComboStep step, WeightedSwing curve)
        {
            step.Ease = SwingEaseStyle.Weighted;
            step.EaseInTicks = curve.EaseInTicks;
            step.EaseOutTicks = curve.EaseOutTicks;
            step.EaseOutDecay = curve.EaseOutDecay;
            step.AttackTicks = curve.TotalTicks;
            step.HitWindowEnd = curve.HitWindowEnd;
        }

        // Ground Pound is the only LeapSlam-motion step in Artorias's moveset, so this fires exactly
        // once per use: a shockwave-style impact under the wielder the moment the slam actually touches
        // down. It hangs off OnLeapSlamLanded rather than DoComboMeleeHit because that hook only runs
        // on a real landing - a leap that times out airborne (blocked, or the player ran out of
        // reach) now ends with no ground effect instead of detonating a shockwave in open sky.
        protected override void OnLeapSlamLanded(MeleeComboStep step)
        {
            SpawnLandingImpactVFX(NPC.Bottom, 86f, 68f);
            base.OnLeapSlamLanded(step);
        }

        /// <summary>Ground fracture at <paramref name="position"/> (the feet). Damage 0 = cosmetic; above 0 the
        /// projectile's hitbox covers exactly the eruption it draws.</summary>
        protected void SpawnLandingImpactVFX(Vector2 position, float width, float height, int damage = 0)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }
            Projectile.NewProjectile(NPC.GetSource_FromThis(), position, Vector2.Zero,
                ModContent.ProjectileType<ArtoriasLandingImpactVFX>(), damage, 0f,
                Main.myPlayer, width, height);
        }

        protected override void DoMeleeAttack()
        {
            SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.6f, PitchVariance = 0.2f }, NPC.Center);
            TryMeleeHit();
            SpawnArtoriasSwordArc(GetMeleeSwingTicks(MeleeAttackTicks), hitWindowEnd: MeleeAttackCurve.HitWindowEnd);
        }

        protected override void DoRangedAttack()
        {
            // No ranged weapon; never invoked (RangedWeaponItemType is -1).
        }

        // Authored holds (Artorias idling while his phantom arrives; the phantom's own fade in and out) stand
        // the puppet still and facing its target instead of walking. Gravity still applies to velocity.Y.
        protected override void RunMovementAI(float speedMult)
        {
            if (!HoldAttackSelection)
            {
                base.RunMovementAI(speedMult);
                return;
            }

            NPC.velocity.X *= 0.8f;
            if (NPC.HasValidTarget)
            {
                int facing = 1;
                if (Main.player[NPC.target].Center.X < NPC.Center.X)
                {
                    facing = -1;
                }

                NPC.direction = facing;
                NPC.spriteDirection = facing;
            }
        }

        // ── Melee sweep crescents (Gwyn's VanillaSwordArc pattern) ───────────────────────────────
        // Artorias's greatsword art is 70x70 with the default (0.10, 0.85) grip, so its farthest corner is
        // sqrt(63^2 + 59.5^2) = 86.7px from the hand, x1.1 PuppetDrawScale = ~95px on screen. VanillaSwordArc
        // puts its crescent's outer rim at Radius (94px reference x1.1 internal scale), so the cutting edge
        // rides the real blade tip instead of stopping at the blade's midpoint.
        protected const float ArtoriasSwordArcRadius = 95f;
        // Mirrors PuppetNPC.LeapSlamDownswingTicks (private there): Ground Pound's landing downswing length.
        const int LandingSwordArcTicks = 10;
        static readonly Color SwordArcDark = new(24, 8, 48);
        static readonly Color SwordArcBody = new(120, 52, 210);
        static readonly Color SwordArcCore = new(210, 170, 255);
        // Artorias's void palette: the crescent's shader overlay and his thrust sheath.
        protected static readonly Color SlashDark = new(22, 6, 36);
        protected static readonly Color SlashMid = new(143, 42, 190);
        protected static readonly Color SlashCore = new(220, 166, 236);
        bool _landingSwordArcSpawned;

        /// <summary>Spawns the purple four-frame VanillaSwordArc for ONE real sword sweep, with the
        /// VoidSlashCrescent shader drawn through the crescent sprite. Call it on the first tick the blade
        /// sweeps; TrackPuppetBlade then pins it to the live hand and blade angle every frame, so it cannot
        /// show outside the swing or drift off the sword.</summary>
        /// <param name="duration">The sweep's own tick count; the arc fades out as it ends.</param>
        /// <param name="reverse">True for sweeps that travel against the overhead direction (underhand).</param>
        /// <param name="hitWindowEnd">Step fraction after which the blade is harmless; 0 = live all step.</param>
        protected void SpawnArtoriasSwordArc(int duration, bool reverse = false, float hitWindowEnd = 0f)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            duration = Math.Max(1, duration);

            // Every Artorias sweep raises its weapon rotation (the overhead-chop direction) except an
            // underhand rise. The sign only flips the sheet vertically so the crescent trails the blade.
            int sweepDirection = NPC.direction;
            if (reverse)
            {
                sweepDirection = -sweepDirection;
            }

            // Fade over the last 5 ticks, or from the step's HitWindowEnd onward when it has one, so the
            // crescent is gone once the blade stops being a hitbox. 2-tick fade-in stops a first-frame pop.
            float fadeOutFraction = Math.Min(0.35f, 5f / duration);
            if (hitWindowEnd > 0f)
            {
                fadeOutFraction = 1f - hitWindowEnd;
            }

            VanillaSwordArcSettings settings = new VanillaSwordArcSettings
            {
                Texture = VanillaSwordArcTexture.NightsEdge,
                Easing = VanillaSwordArcEasing.Linear,
                Duration = duration,
                StartAngle = PuppetWeaponDirection.ToRotation(),
                SweepAngle = sweepDirection * MathHelper.Pi,
                Radius = ArtoriasSwordArcRadius,
                Opacity = 0.66f,
                FadeInFraction = Math.Min(0.12f, 2f / duration),
                FadeOutFraction = fadeOutFraction,
                AfterimageLag = MathHelper.PiOver4,
                AfterimageOpacity = 0.58f,
                BodyOpacity = 0.82f,
                CoreOpacity = 0.2f,
                DrawTipSparkle = false,
                TintWithWorldLighting = true,
                DarkColor = SwordArcDark,
                BodyColor = SwordArcBody,
                CoreColor = SwordArcCore,
                DrawCinderOverlay = true,
                CinderOverlayStyle = VanillaSwordArcCinderStyle.Void,
                CinderOverlayOpacity = 0.8f,
                CinderOverlayDarkColor = SlashDark,
                CinderOverlayFlameColor = SlashMid,
                CinderOverlayCoreColor = SlashCore,
                DustType = -1,
                DustCount = 0,
                TrackPuppetBlade = true,
                EnableCollision = false,
            };

            VanillaSwordArc.SpawnForNPC(NPC.GetSource_FromAI(), NPC, 0, 0f, Main.myPlayer,
                settings, Vector2.Zero, hostile: false);
        }

        protected override void OnMeleeComboStarted(MeleeCombo combo)
        {
            base.OnMeleeComboStarted(combo);
            _landingSwordArcSpawned = false;
        }

        protected override void OnMeleeComboAttackTick(MeleeCombo combo, MeleeComboStep step, int elapsed, int total)
        {
            base.OnMeleeComboAttackTick(combo, step, elapsed, total);

            // One crescent per damaging sweep, on the step's first tick (elapsed reads 0 once per step).
            // Thrusts (JoustDash) keep the sheath in Artorias.DrawSwordSlashVFX, and Ground Pound's LeapSlam
            // waits for its landing downswing below, because the jump itself is a harmless carry.
            bool sweepMotion = step.Motion == ComboMotion.OverheadArc || step.Motion == ComboMotion.UnderhandArc
                || step.Motion == ComboMotion.HorizontalSweep || step.Motion == ComboMotion.VerticalChop
                || step.Motion == ComboMotion.GroundSlam || step.Motion == ComboMotion.Spin;
            if (elapsed != 0 || step.DamageMult <= 0f || !sweepMotion)
            {
                return;
            }

            bool underhand = step.Motion == ComboMotion.UnderhandArc;
            SpawnArtoriasSwordArc(total, underhand, step.HitWindowEnd);
        }

        protected override void OnLandingTimedLeapSlamSwingTick(MeleeComboStep step, float progress)
        {
            base.OnLandingTimedLeapSlamSwingTick(step, progress);
            if (_landingSwordArcSpawned || step.DamageMult <= 0f)
            {
                return;
            }

            // The downswing can begin a few frames before touchdown (predicted landing), so the arc only
            // covers what is left of it. The 5-tick floor keeps a same-frame landing visible.
            float remainingFraction = 1f - MathHelper.Clamp(progress, 0f, 1f);
            int remainingSwingTicks = Math.Max(5, (int)Math.Ceiling(remainingFraction * LandingSwordArcTicks));
            SpawnArtoriasSwordArc(remainingSwingTicks);
            _landingSwordArcSpawned = true;
        }
    }

    [AutoloadBossHead]
    class Artorias : ArtoriasSwordsman
    {
        // PuppetNPC overrides Texture to a shared puppet placeholder, so the default
        // Texture + "_Head_Boss" convention [AutoloadBossHead] relies on would look for
        // "PuppetPlaceholder_Head_Boss" instead of this boss's actual head icon.
        public override string BossHeadTexture => "tsorcRevamp/NPCs/Bosses/SuperHardMode/Artorias_Head_Boss";

        protected override string InvaderTitle => "Artorias";

        protected override bool UseCompositeArmForAdditionalPhase =>
            Phase == AttackPhase.StabTelegraph || Phase == AttackPhase.StabAttack ||
            Phase == AttackPhase.StabRecovery || Phase == AttackPhase.PierceTelegraph ||
            Phase == AttackPhase.PierceDash || Phase == AttackPhase.PierceStabHold ||
            Phase == AttackPhase.PierceStabFlick || Phase == AttackPhase.JumpSlashDodgeback ||
            Phase == AttackPhase.JumpSlashRise || Phase == AttackPhase.JumpSlashAttack ||
            Phase == AttackPhase.FlipSlashRise || Phase == AttackPhase.FlipSlashLand ||
            Phase == AttackPhase.AbyssSlashTelegraph || Phase == AttackPhase.AbyssSlashSwipe ||
            Phase == AttackPhase.AbyssSlashPause || Phase == AttackPhase.HomingVolleyDodgeback ||
            Phase == AttackPhase.HomingVolleySwingTelegraph || Phase == AttackPhase.HomingVolleySwing ||
            Phase == AttackPhase.SwordLaunchReposition || Phase == AttackPhase.SpiralFanSwingTelegraph ||
            Phase == AttackPhase.SpiralFanSwing || Phase == AttackPhase.SpiralFanBurst ||
            Phase == AttackPhase.SpiralFanPause ||
            // Recoveries too: MeleeRecoveryLingerTicks keeps the greatsword drawn at its finished
            // pose for the first 30 ticks of these, and without the composite arm the shoulder would
            // snap back to a 4-frame Use pose underneath a blade that hasn't moved.
            Phase == AttackPhase.JumpSlashRecovery || Phase == AttackPhase.AbyssSlashRecovery ||
            Phase == AttackPhase.TendrilRecovery || Phase == AttackPhase.HomingVolleyRecovery ||
            Phase == AttackPhase.BoomerangRecovery ||
            Phase == AttackPhase.PierceRecovery || Phase == AttackPhase.SpiralFanRecovery ||
            // Rising Uppercut / Skyward Lunge run on the generic Custom phase and drive the blade themselves.
            (Phase == AttackPhase.Custom && _aerialStage != AerialStage.None);

        protected override int MeleeDamage => EnemyDamage.Projectile(120);

        // ── Piercing Dash ────────────────────────────────────────────────────────
        protected override bool  CanPierce            => true;
        protected override float PierceRange          => 700f;
        protected override float MinPierceRange       => 250f;
        protected override int   PierceChance          => 4;
        protected override int   PierceTelegraphTicks => 60;
        protected override int   PierceDashTicks      => 40;
        protected override float PierceDashSpeed      => 16f;
        protected override int   PierceRecoveryTicks  => 90;
        // Every connecting Piercing Dash impales. At 50% the plain lunge (damage + knockback, no hold) looked almost
        // identical to the stab, so the hold read as randomly failing to trigger.
        protected override int   PierceStabChance     => 100;
        protected override int   PierceStabRaiseTicks => 180;
        // The pose reaches vertical in a quick 12-tick snap instead of drifting there across the
        // whole 180-tick hold - the target reads as centered/impaled immediately, and PierceStabHold
        // just holds them there for the rest of the sequence instead of visibly still adjusting.
        protected override int   PierceStabRaiseAnimTicks => 12;
        // Flick = a slow 40t wind-back (vertical -> cocked behind the head, smoothstepped), a 10t hold at that apex,
        // then an 8t snap forward that releases the target on its first tick. -1.6 rad is the same cocked pose his
        // Vertical Chop winds up to (ModifyMeleeArcEndpoints), so the composite arm is known to reach it.
        const int PierceFlickWindBackTicks = 40;
        const int PierceFlickApexHoldTicks = 10;
        const int PierceFlickSnapTicks = 8;
        protected override int   PierceStabFlickWindBackTicks => PierceFlickWindBackTicks;
        protected override float PierceStabFlickWindBackRotation => -1.6f;
        protected override int   PierceStabFlickDelayTicks => PierceFlickWindBackTicks + PierceFlickApexHoldTicks;
        protected override int   PierceStabFlickTicks => PierceStabFlickDelayTicks + PierceFlickSnapTicks;
        protected override int   PierceCooldownAfterUse => 480;
        // A whiffed or rolled-through dash re-aims and goes again, up to twice. The repeat tell is 40t (the
        // heavy-tell floor): the next dash goes live >= 40t after the last one ended, clear of the 30t
        // post-roll gap, so every dash in the chain is separately rollable.
        protected override int   PierceWhiffRepeatCount     => 2;
        protected override int   PierceRepeatTelegraphTicks => 40;

        // ── Jumping Downward Slash ───────────────────────────────────────────────
        protected override bool  CanJumpSlash          => true;
        protected override float JumpSlashMinRange      => 0f;
        protected override float JumpSlashMaxRange      => 50f * 16f;
        protected override float JumpSlashMaxForwardSpeed => 8.5f;
        protected override float JumpSlashMaxUpSpeed    => 18f;
        // Leap solve: lead a running target by up to 160px (a 6px/t sprint over the ~60t airtime would be 360px, so
        // this chases without letting a juke drag him across the arena), and land 64px past them so the 95px blade
        // sweeps through them on the way down instead of the leap dropping short.
        protected override float JumpSlashMaxLead       => 160f;
        protected override float JumpSlashAimPastTarget => 64f;
        protected override int   JumpSlashChance        => 5;
        protected override int   JumpSlashCooldownAfterUse => 420;
        // Swipe: -60° cocked -> 110° (170° envelope; was 55°, widened at the END so ~142° stays live past the 30%
        // disarm and still reaches a player at his feet). in 8 / out 22, k 7 -> 29°/t, live 11.8t. The 12t longer
        // phase comes out of the recovery (70 -> 58): tail + recovery = ~76t punish window, was 70.
        protected override WeightedSwing JumpSlashCurve   => new WeightedSwing(8, 22, 7f);
        protected override int   JumpSlashAttackTicks     => JumpSlashCurve.TotalTicks;
        protected override float JumpSlashEndRotation     => MathHelper.ToRadians(110f);
        protected override int   JumpSlashRecoveryTicks   => 58;

        // ── Forward Flip Slash ───────────────────────────────────────────────────
        protected override bool  CanFlipSlash              => true;
        protected override float FlipSlashMinRange         => 150f;
        protected override float FlipSlashMaxRange         => 450f;
        protected override int   FlipSlashChance           => 4;
        protected override int   FlipSlashCooldownAfterUse => 420;

        // Landing strike timing sheet (attack-timing-design §3):
        // Poses:   -1.62 (cocked behind the head) -> 2.36 (straight down): 228° envelope, ~182° live.
        // Tell:    the whole ~53t somersault; the spin is phase-locked to arrive at -1.62 on touchdown.
        // Strike:  in 7 / cruise 0 / out 30, k 9 -> peak 228 / (7/3 + 30 * 0.9999 / 9) = 40°/t on tick 7 (contact FX),
        //          the fastest blade in his kit.
        // Live:    7 + 30 * 1.204 / 9 = 11t from touchdown (< 22, rollable). The spin blade is live all flight.
        // Open:    52t hold - 11t live = 41t planted punish window.
        // Counter: roll through the spin as it crosses (~13t at roll speed), then space out of or roll the
        //          slam. A spin hit puts the slam inside the 40t post-hit immunity, so the two never both land.
        // Reach:   95px (ArtoriasSwordArcRadius) = the visible grip -> tip distance.
        protected override bool  UseFlipSlashLandingStrike    => true;
        protected override float FlipSlashStrikeStartRotation => -1.62f;
        protected override float FlipSlashStrikeEndRotation   => 2.36f;
        protected override int   FlipSlashLandHoldTicks       => 52;
        protected override int   FlipSlashStrikeEaseInTicks   => 7;
        protected override int   FlipSlashStrikeEaseOutTicks  => 30;
        protected override float FlipSlashStrikeEaseOutDecay  => 9f;

        // ── Abyss Slash ──────────────────────────────────────────────────────────
        protected override bool  CanAbyssSlash              => true;
        protected override float AbyssSlashMinRange         => 250f;
        protected override float AbyssSlashMaxRange         => 900f;
        protected override int   AbyssSlashChance           => 5;
        protected override int   AbyssSlashCooldownAfterUse => 300;
        // Release swipe: in 4 / out 18, k 6 -> 37°/t over 160°, a whip-crack off the held post (was a 16t Snap).
        // No blade hit; the crescent leaves on swipe entry, 4t before the peak. The swipe is 6t longer, so the
        // gap tables and the recovery (60 -> 54) are 6t shorter and the crescents keep their release spacing.
        protected override WeightedSwing AbyssSlashSwipeCurve => new WeightedSwing(4, 18, 6f);
        protected override int   AbyssSlashSwipeTicks       => AbyssSlashSwipeCurve.TotalTicks;
        protected override int   AbyssSlashRecoveryTicks    => 54;

        // ── Spectral phantom (below 50% HP) ──────────────────────────────────────
        // A real, invulnerable, melee-only Artorias (ArtoriasPhantom) that fights beside him. It replaced the
        // scripted Umbral Echo Step afterimage, which floated, could not chase, and whiffed. One at a time:
        // summoned from a free grounded moment, again PhantomSummonCooldownTicks after the last one fades.
        // Artorias stands idle for PhantomOwnerIdleTicks so the arrival owns the threat, then fights freely.
        const float PhantomHealthThreshold = 0.5f;
        const int PhantomSummonCooldownTicks = 240;
        const int PhantomOwnerIdleTicks = 120;
        const float PhantomSpawnOffset = 56f; // px beside Artorias, on the target's side
        int _phantomIndex = -1;
        int _phantomSummonCooldown;
        int _phantomOwnerIdleTimer;

        protected override bool HoldAttackSelection => _phantomOwnerIdleTimer > 0;

        void TickSpectralPhantom()
        {
            if (_phantomOwnerIdleTimer > 0)
            {
                _phantomOwnerIdleTimer--;
            }

            // Summoning is server-side; clients get the NPC and the idle timer through normal sync.
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            bool phantomAlive = _phantomIndex >= 0
                && Main.npc[_phantomIndex].active
                && Main.npc[_phantomIndex].type == ModContent.NPCType<ArtoriasPhantom>();
            if (phantomAlive)
            {
                return;
            }

            // The cooldown only runs while no phantom is out, so it measures from the last one fading.
            _phantomIndex = -1;
            if (_phantomSummonCooldown > 0)
            {
                _phantomSummonCooldown--;
                return;
            }

            bool belowThreshold = NPC.life <= NPC.lifeMax * PhantomHealthThreshold;
            bool freePhase = Phase == AttackPhase.Idle || Phase == AttackPhase.CasualStroll
                || Phase == AttackPhase.ClosingDistance;
            bool grounded = NPC.velocity.Y == 0f;
            if (!belowThreshold || !freePhase || !grounded || !NPC.HasValidTarget)
            {
                return;
            }

            // Stand it on Artorias's own footing, a step toward the player; if that spot is inside a wall,
            // use his exact spot instead. NewNPC places the NPC's bottom-centre on (x, y).
            Player target = Main.player[NPC.target];
            int sideTowardTarget = 1;
            if (target.Center.X < NPC.Center.X)
            {
                sideTowardTarget = -1;
            }

            Vector2 spawnBottom = NPC.Bottom + new Vector2(sideTowardTarget * PhantomSpawnOffset, 0f);
            Vector2 spawnTopLeft = spawnBottom - new Vector2(NPC.width * 0.5f, NPC.height);
            if (Collision.SolidCollision(spawnTopLeft, NPC.width, NPC.height))
            {
                spawnBottom = NPC.Bottom;
            }

            int phantomIndex = NPC.NewNPC(NPC.GetSource_FromAI(), (int)spawnBottom.X, (int)spawnBottom.Y,
                ModContent.NPCType<ArtoriasPhantom>(), 0, NPC.whoAmI);
            if (phantomIndex >= Main.maxNPCs)
            {
                return;
            }

            _phantomIndex = phantomIndex;
            _phantomSummonCooldown = PhantomSummonCooldownTicks;
            _phantomOwnerIdleTimer = PhantomOwnerIdleTicks;
            EnterPhase(AttackPhase.Idle, 0);
            NPC.netUpdate = true;
        }

        // ── Abyss Tendril Grab ───────────────────────────────────────────────────
        protected override bool  CanTendrilGrab          => true;
        protected override float TendrilMinRange         => 150f;
        protected override float TendrilMaxRange          => 500f;
        protected override int   TendrilChance            => NPC.life <= NPC.lifeMax * 0.50f ? 8 : 4;
        protected override int   TendrilCooldownAfterUse  => 480;
        // Finishing swing: -35° post -> 135° straight down (170° envelope; 10° wider at the end, the Flip Slash
        // strike's end pose) so ~146° stays live past the 30% disarm. in 10 / out 22, k 7.5 -> 27°/t: his slowest-
        // building blade, a deliberate cut at a player just yanked in. Live 13.5t. Tail +16t comes out of the
        // recovery (60 -> 44), so tail + recovery stays ~62t.
        protected override WeightedSwing TendrilSwingCurve => new WeightedSwing(10, 22, 7.5f);
        protected override int   TendrilSwingTicks        => TendrilSwingCurve.TotalTicks;
        protected override float TendrilSwingEndRotation  => MathHelper.ToRadians(180f - 45f);
        protected override int   TendrilRecoveryTicks     => 44;

        const int TendrilGrabDamage = 140;
        const float TendrilLaunchSpeed = 12f;
        const float TendrilTopSpeed = TendrilLaunchSpeed * 2f;
        internal Vector2 TendrilHandPosition => PuppetHandPosition;

        protected override bool DrawSpecialHeldWeapon(ref PlayerDrawSet drawInfo)
        {
            // During the grab Artorias's sword arm is replaced by the abyss-charged bare hand.
            // Returning true suppresses the ordinary greatsword without disturbing its later
            // reappearance for the authored finishing swing.
            return Phase == AttackPhase.TendrilTelegraph || Phase == AttackPhase.TendrilReach;
        }

        const int PierceContactDamage   = 150;  // plain (non-impaling) pierce dash contact
        const int PierceStabDamage      = 150; // stab variant's initial impale hit
        const int PierceFlickDamage     = 175; // second hit dealt as the target is flicked off the blade
        const int PierceStabHealAmount  = 5000;
        const float PierceFlickDistance = 10 * 16f; // 10 tiles
        const float ImpaleSwordReach    = 70f;

        // The whole Piercing Dash sequence cripples its target: Crippled (no wings/extra jumps, -10% speed, rolls
        // still work) kept on for as long as this is true, under the same abyss mantle Tendril Reach wears.
        // Recovery is excluded - that is the player's punish window.
        bool PierceCrippleActive =>
            Phase == AttackPhase.PierceTelegraph || Phase == AttackPhase.PierceDash
            || Phase == AttackPhase.PierceStabHold || Phase == AttackPhase.PierceStabFlick;
        const int PierceCrippleBuffTicks = 2;      // refreshed every tick, so it ends with the attack
        const int PierceHazeFadeInTicks = 15;
        int _pierceHazeTicks;                      // ticks the haze has been up; drives its fade-in

        private int _impaleSwordProjIndex = -1;
        private int _impaleTargetIndex = -1;
        private int _swordSlashSequence; // bumped per fresh thrust; feeds VoidSlashVFX's per-thrust noise phase
        private bool _swordSlashWasActive;
        NPCDespawnHandler despawnHandler;

        // Only a fabled blade can pierce Artorias's protective shield: the Barrow Blade
        // (via its projectile, since the item itself only damages through it) or the
        // Forgotten Gaia Sword, or the DispelShadow debuff those weapons apply.
        bool defenseBroken = false;
        int textCooldown;

        // Fixed-center arena boundary: captured at spawn and contracts during phase pressure events.
        // Crossing into the visible exterior inflicts one RingEdgeDamage pulse per
        // second and pushes the player inward, rather than delivering the old unavoidable instant kill.
        public const float RingRadius = 50 * 16f;      // 100-tile diameter
        const float RingBandHalfWidth = 40f;
        // Direct Player.Hurt, not a projectile: no difficulty scaling, so 120 before defense in every mode.
        const int RingEdgeDamage = 120;
        const float RingBossSafetyPadding = 10f;
        const float RingBossSteeringWidth = 48f;
        Vector2 _ringCenter;
        public Vector2 RingCenter => _ringCenter;
        public float RingBandHalfWidthPixels => RingBandHalfWidth;
        int _ringVfxTimer;
        readonly int[] _ringDamageCooldown = new int[Main.maxPlayers];

        // Live effective radius. Everything that used to read RingRadius directly (damage, dust,
        // boundary shader, and phase presentation) reads this value so visuals and gameplay
        // remain locked together while the ring contracts, holds, and expands.
        float _currentRingRadius = RingRadius;
        public float EffectiveRingRadius => _currentRingRadius;

        // ── Ring Collapse: at 50% the ring contracts temporarily; at 30% it contracts permanently.
        // A preview dust/fire ring appears at the new radius before it moves, giving the player a
        // readable two-stage warning instead of silently shrinking the safe area under them.
        enum RingCollapseState { Inactive, Telegraph, Contracting, Holding, Expanding }
        RingCollapseState _ringCollapseState = RingCollapseState.Inactive;
        // Last state this machine played a cue for. Local and unsynced on purpose: cues fire on the state change each
        // machine sees, whether it ran the transition itself or received it in ReceiveExtraAI.
        RingCollapseState _ringCollapseCueState = RingCollapseState.Inactive;
        int _ringCollapseTimer;
        float _ringCollapseFrom;
        float _ringCollapseTo;
        bool _ringCollapseDone50;
        bool _ringCollapseDone30;

        const float PhaseTwoRingRadius = RingRadius * 0.70f;
        const float FinalPhaseRingRadius = RingRadius * 0.50f;
        const int   RingCollapseTelegraphTicks = 30;
        const int   RingCollapseMoveTicks      = 120;       // 2s each way - "slowly"
        const int   PhaseTwoRingHoldTicks      = 12 * 60;
        public bool RingCollapseWarningActive => _ringCollapseState == RingCollapseState.Telegraph;
        public float RingCollapseWarningRadius => _ringCollapseTo > 0f ? _ringCollapseTo : _currentRingRadius;

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 1;
            // Artorias normally uses the same short puppet cache as other humanoids. Pierce Dash
            // deliberately keeps twenty poses so its body reads as a sustained, lethal thrust.
            NPCID.Sets.TrailCacheLength[NPC.type] = 20;
            NPCID.Sets.TrailingMode[NPC.type] = 0;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Confused] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.CursedInferno] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Ichor] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.OnFire] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.OnFire3] = true;
        }

        public override void SetDefaults()
        {
            NPC.aiStyle = -1;
            NPC.knockBackResist = 0;
            EnemyDamage.SetContact(NPC, 0); // all damage via weapon hitboxes
            NPC.defense = 75;
            NPC.height = 40;
            NPC.width = 30;
            NPC.lifeMax = 250000;
            NPC.HitSound = SoundID.NPCHit4;
            NPC.DeathSound = SoundID.NPCDeath6;
            NPC.value = 750000;
            NPC.rarity = 39;
            NPC.boss = true;
            NPC.lavaImmune = true;
            NPC.coldDamage = true;
            despawnHandler = new NPCDespawnHandler(LangUtils.GetTextValue("NPCs.Artorias.DespawnHandler"), Color.Gold, DustID.GoldFlame);

            tsorcRevampGlobalNPC artoriasGlobalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            artoriasGlobalNPC.Agility = 0.4f; // frequent proactive dodges (see EvadesProjectiles below)
            artoriasGlobalNPC.CanTeleport = true;
            artoriasGlobalNPC.TeleportStyle = NPCs.TeleportStyle.Aggressive;
            artoriasGlobalNPC.TeleportVisualStyle = NPCs.TeleportVisualStyle.Plague;
            artoriasGlobalNPC.NavSearchRadius = 80;

            // On-hit dodgeroll: hop/leap/dash away, or blink away (using the same plague-style
            // teleport set above) when able. Same bundle as the Red Knight family.
            EvasiveProfile.RedKnight(artoriasGlobalNPC);
        }

        // Proactive dodge: scans for an incoming aimed projectile and jumps/i-frame rolls it away
        // (rolls Agility above), same mechanism CursedDragon uses - evasion BEFORE getting hit.
        protected override bool EvadesProjectiles => true;

        protected override int TeleportTelegraphTicks => 30;

        public override void OnSpawn(IEntitySource source)
        {
            _ringCenter = NPC.Center;
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                Projectile.NewProjectile(NPC.GetSource_FromThis(), _ringCenter, Vector2.Zero,
                    ModContent.ProjectileType<ArtoriasBoundaryVFX>(), 0, 0f,
                    Main.myPlayer, NPC.whoAmI);
            }
            NPC.netUpdate = true;
        }

        // Hyper-armor bookkeeping (end of AI): the phase seen last tick, and whether the current state is the opening tell
        // of an attack started from neutral. The lead mirrors PuppetNPC.CheckAndFireFlash's 30-tick flash lead.
        const int OpeningTellFlashLeadTicks = 30;
        AttackPhase _lastTickPhase = AttackPhase.Idle;
        bool _inOpeningTell;

        public override void AI()
        {
            despawnHandler.TargetAndDespawn(NPC.whoAmI);
            if (!NPC.active)
            {
                return;
            }

            if (despawnHandler.IsDespawning)
            {
                NPC.dontTakeDamage = true;
                NPC.velocity *= 0.85f;
                _abyssSurgeTimer = 0;
                _currentRingRadius = RingRadius;

                if (_impaleSwordProjIndex >= 0 && _impaleSwordProjIndex < Main.maxProjectiles
                    && Main.projectile[_impaleSwordProjIndex].active)
                {
                    Main.projectile[_impaleSwordProjIndex].Kill();
                    _impaleSwordProjIndex = -1;
                }

                return;
            }

            base.AI();
            UpdateSwordSlashSequence();
            TickProjectileSwordTelegraphs();

            // The puppet body and hand-drawn greatsword both sample the normal light map, so this
            // shared white light keeps the whole silhouette readable when the abyss-space scene is up.
            Lighting.AddLight(NPC.Center, Color.White.ToVector3() * 1.5f);

            if (NPC.HasBuff(ModContent.BuffType<Buffs.DispelShadow>()))
            {
                defenseBroken = true;
            }

            TickAbyssRing();
            TickRingCollapse();
            ConstrainArtoriasToAbyssRing();
            TickAbyssSurges();
            TickSpectralPhantom();
            TickAerialTriggers();

            // ── Hyper armor ──────────────────────────────────────────────────────────────────────
            // Every attack state is armoured EXCEPT the opening tell of an attack started from neutral, up to its flash.
            // Runs after base.AI() (which already published this tick's flags) and TickAerialTriggers, so this tick's
            // phase changes are visible; hits between ticks read the result.

            // Opening tell = the first state entered straight from Idle / CasualStroll / ClosingDistance. Any other
            // transition (a combo pause, the step after a dodgeback, a dodge-punish or aerial chain) ends it.
            if (Phase != _lastTickPhase)
            {
                bool cameFromNeutral = _lastTickPhase == AttackPhase.Idle || _lastTickPhase == AttackPhase.CasualStroll
                    || _lastTickPhase == AttackPhase.ClosingDistance;
                _inOpeningTell = cameFromNeutral;
                _lastTickPhase = Phase;
            }

            // Armour the attack states PuppetNPC's commit lists don't cover: the i-framed dodgebacks, the sword-launch
            // hop (armoured even as an opener) and every aerial stage but the landing beat.
            tsorcRevampGlobalNPC attackFlags = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            bool aerialAttacking = Phase == AttackPhase.Custom && _aerialStage != AerialStage.None
                && _aerialStage != AerialStage.Landing;
            bool attackReposition = Phase == AttackPhase.JumpSlashDodgeback || Phase == AttackPhase.HomingVolleyDodgeback
                || Phase == AttackPhase.SwordLaunchReposition;
            if (aerialAttacking || attackReposition)
            {
                attackFlags.AttackCommitted = true;
                attackFlags.AttackTelegraphing = false;
            }

            // Pre-flash window. The base flash fires once 30 ticks of a telegraph remain (CheckAndFireFlash), or on the
            // first tick of a shorter one, so a tell of 30t or less is armoured throughout. Artorias's bespoke tells have
            // no flash and use the same 30t lead. The uppercut's run and the Skyward Lunge jump have no fixed end (the
            // jump ends on reaching its apex, not a timer), so all of either counts; the 30t apex pause that follows the
            // jump is entirely inside the lead and stays armoured throughout - the jump itself is now the punishable
            // opener window instead. Telegraphs that only ever follow another state (JumpSlashRise, TendrilReach, ...)
            // are not listed: they can never be an opening tell.
            bool openingTelegraph = Phase == AttackPhase.MeleeTelegraph || Phase == AttackPhase.StabTelegraph
                || Phase == AttackPhase.MeleeComboTelegraph || Phase == AttackPhase.PierceTelegraph
                || Phase == AttackPhase.AbyssSlashTelegraph || Phase == AttackPhase.TendrilTelegraph
                || Phase == AttackPhase.AbyssShardTelegraph || Phase == AttackPhase.BoomerangSwingTelegraph
                || Phase == AttackPhase.SpiralFanSwingTelegraph;
            bool telegraphPreFlash = openingTelegraph && PhaseTimer > OpeningTellFlashLeadTicks;
            bool uppercutRunTell = _aerialStage == AerialStage.UppercutRun;
            bool skywardJumpTell = _aerialStage == AerialStage.SkywardJump;
            bool aerialTellPreFlash = Phase == AttackPhase.Custom && (uppercutRunTell || skywardJumpTell);
            if (_inOpeningTell && (telegraphPreFlash || aerialTellPreFlash))
            {
                attackFlags.AttackCommitted = false;
                attackFlags.AttackTelegraphing = true;
            }

            // Pierce cripple: every machine runs the pierce phases, so the local player applies the debuff to
            // themselves (player buffs are client-owned) and every client spawns the light purple motes.
            if (!PierceCrippleActive)
            {
                _pierceHazeTicks = 0;
            }

            if (PierceCrippleActive && NPC.HasValidTarget)
            {
                _pierceHazeTicks = Math.Min(_pierceHazeTicks + 1, PierceHazeFadeInTicks);
                Player crippledTarget = Main.player[NPC.target];

                if (crippledTarget.whoAmI == Main.myPlayer)
                {
                    crippledTarget.AddBuff(ModContent.BuffType<Crippled>(), PierceCrippleBuffTicks);
                }

                if (!Main.dedServ && Main.rand.NextBool(2))
                {
                    Vector2 motePosition = crippledTarget.position
                        + new Vector2(Main.rand.NextFloat(crippledTarget.width), Main.rand.NextFloat(crippledTarget.height));
                    Vector2 moteVelocity = new Vector2(Main.rand.NextFloat(-0.4f, 0.4f), Main.rand.NextFloat(-1.4f, -0.5f));
                    Dust mote = Dust.NewDustPerfect(motePosition, DustID.WhiteTorch, moteVelocity, 90,
                        new Color(206, 168, 255), Main.rand.NextFloat(0.8f, 1.15f));
                    mote.noGravity = true;
                }
            }

            // Jump Slash ground AOE: spawn at the feet on the first grounded tick of the slash or its recovery. If the
            // attack leaves those phases first (dodge-punish chain, stagger) the AOE is dropped rather than spawned late.
            if (_jumpSlashImpactPending)
            {
                bool inJumpSlash = Phase == AttackPhase.JumpSlashAttack || Phase == AttackPhase.JumpSlashRecovery;
                bool grounded = NPC.velocity.Y == 0f;

                if (!inJumpSlash)
                {
                    _jumpSlashImpactPending = false;
                }
                else if (grounded)
                {
                    SpawnLandingImpactVFX(NPC.Bottom, JumpSlashImpactWidth, JumpSlashImpactHeight, EnemyDamage.Projectile(JumpSlashImpactDamage));
                    _jumpSlashImpactPending = false;
                }
            }

            if (!_abyssShardUnlocked && NPC.life <= NPC.lifeMax * 0.6f)
            {
                _abyssShardUnlocked = true;
            }
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            DrawArtoriasAttackVFX();
            return base.PreDraw(spriteBatch, screenPos, drawColor);
        }

        public override void PostDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            // This layer must sit over the puppet arm so it genuinely looks consumed by the
            // Abyss instead of reading as a detached aura behind Artorias.
            DrawTendrilHandVFX();
            base.PostDraw(spriteBatch, screenPos, drawColor);
            DrawPierceUnblockableWeaponAura(spriteBatch);
        }

        void DrawArtoriasAttackVFX()
        {
            DrawSwordSlashVFX();

            if (_novaStageIndex >= 0 && Phase == AttackPhase.NovaCharge)
            {
                float chargeProgress = MathHelper.Clamp(1f - PhaseTimer / (float)NovaChargeTicks, 0f, 1f);
                float radius = NovaStages[_novaStageIndex].radius;
                float chargeOpacity = MathHelper.Lerp(0.20f, 0.62f, chargeProgress);
                ArtoriasVFX.DrawDetonation(NPC.Center, radius, chargeProgress, chargeOpacity, active: false);

                // A second, fainter copy turning the opposite way over the first, so the two swirls shear
                // against each other. NovaCounterSpinPerSecond rad/s, wrapped so the float never grows.
                float counterRotation = -(Main.GlobalTimeWrappedHourly * NovaCounterSpinPerSecond) % MathHelper.TwoPi;
                ArtoriasVFX.DrawDetonation(NPC.Center, radius, chargeProgress,
                    chargeOpacity * NovaCounterLayerOpacity, active: false, rotation: counterRotation);

                ArtoriasVFX.DrawMantle(NPC.Center + new Vector2(0f, -16f),
                    new Vector2(190f, 240f), 0.40f + chargeProgress * 0.32f,
                    0.8f + chargeProgress * 0.55f, -1f);
            }

            // Abyss Slash is a physical crescent throw, not a magic cast - the ghostly mantle shroud
            // (pale KnightSilver core, same look as the spectral phantom) doesn't belong on it.
            bool majorCast = Phase == AttackPhase.TendrilTelegraph
                || Phase == AttackPhase.TendrilReach
                || Phase == AttackPhase.HomingVolleySwingTelegraph
                || Phase == AttackPhase.SpiralFanSwingTelegraph;
            if (majorCast && !AbyssSurgeActive)
            {
                ArtoriasVFX.DrawMantle(NPC.Center + new Vector2(0f, -14f),
                    new Vector2(150f, 205f), 0.34f, 0.72f, 1f);
            }
            if (Phase == AttackPhase.BoomerangSwingTelegraph)
            {
                float progress = MathHelper.Clamp(
                    1f - PhaseTimer / (float)Math.Max(1, BoomerangSwingTelegraphTicks), 0f, 1f);
                Vector2 hand = PuppetHandPosition;
                Vector2 tip = PuppetWeaponTipPosition(62f);
                Vector2 bladeDirection = (tip - hand).SafeNormalize(new Vector2(NPC.direction, -1f));
                ArtoriasVFX.DrawBoomerangCharge(
                    tip, bladeDirection, progress, MathHelper.Lerp(0.66f, 1f, progress));
            }

            // Pierce cripple haze: the Tendril Reach mantle (majorCast above), sized down to a player and faded
            // in over PierceHazeFadeInTicks. Drawn from the NPC pass, so it sits behind the player sprite exactly
            // like Artorias's own mantle sits behind him. Not while impaled: the target then hangs ~70px off his
            // chest, so this pale shroud swallowed Artorias too (the "turns white" look meant only for his phantom).
            // The cripple buff itself still runs through the hold and flick.
            bool targetImpaled = Phase == AttackPhase.PierceStabHold || Phase == AttackPhase.PierceStabFlick;
            if (PierceCrippleActive && !targetImpaled && NPC.HasValidTarget)
            {
                Player hazeTarget = Main.player[NPC.target];
                float hazeFade = _pierceHazeTicks / (float)PierceHazeFadeInTicks;
                ArtoriasVFX.DrawMantle(hazeTarget.Center + new Vector2(0f, -8f),
                    new Vector2(112f, 150f), 0.34f * hazeFade, 0.72f, 1f);
            }

            if (Phase == AttackPhase.PierceStabHold && _impaleTargetIndex >= 0
                && _impaleTargetIndex < Main.maxPlayers)
            {
                Player impaled = Main.player[_impaleTargetIndex];
                if (impaled.active && !impaled.dead)
                {
                    float raise = GetImpaleRaiseProgress01();
                    // The blade points from Artorias THROUGH the impaled target - the wind wisps
                    // stream on out that far side, not radially.
                    Vector2 windDirection = (impaled.Center - NPC.Center).SafeNormalize(new Vector2(NPC.direction, 0f));
                    ArtoriasVFX.DrawImpaleTendrils(
                        impaled.Center, windDirection, raise, 0.86f);
                }
            }

        }

        void DrawPierceUnblockableWeaponAura(SpriteBatch spriteBatch)
        {
            if ((Phase != AttackPhase.PierceTelegraph && Phase != AttackPhase.PierceDash)
                || DebugHeldItemType < 0 || DebugHeldItemType >= TextureAssets.Item.Length)
            {
                return;
            }

            Texture2D weapon = TextureAssets.Item[DebugHeldItemType].Value;
            SpriteEffects effects = DebugDirection == -1
                ? SpriteEffects.FlipHorizontally
                : SpriteEffects.None;
            AttackTelegraphDraw.DrawUnblockableWeaponAura(
                spriteBatch, weapon, DebugHandPos - Main.screenPosition, null,
                MathHelper.ToRadians(DebugDrawRotationDeg), DebugOrigin,
                NPC.scale * MeleeWeaponDrawScale, effects);
            Lighting.AddLight(DebugHandPos, new Vector3(0.78f, 0.035f, 0.02f));
        }

        void DrawTendrilHandVFX()
        {
            if (Phase != AttackPhase.TendrilTelegraph && Phase != AttackPhase.TendrilReach)
                return;

            float charge = Phase == AttackPhase.TendrilTelegraph
                ? MathHelper.Clamp(1f - PhaseTimer / (float)TendrilTelegraphTicks, 0f, 1f)
                : 1f;
            Vector2 hand = PuppetHandPosition;
            ArtoriasVFX.DrawTendrilHand(
                hand - new Vector2(NPC.direction * 8f, 1f), new Vector2(94f, 108f),
                charge, Phase == AttackPhase.TendrilReach, 0.92f);
        }

        void UpdateSwordSlashSequence()
        {
            bool active = IsMainSwordThrust;
            if (active && !_swordSlashWasActive)
            {
                _swordSlashSequence++;
            }

            _swordSlashWasActive = active;
        }

        // A stab reads as danger straight ahead; sweeping the same broad crescent used for chops
        // over it would falsely promise danger to the sides, so those phases get the narrow thrust
        // sheath instead (mirrors GravelordNito.NitoVFX.DrawSlash's kind==2 special case).
        bool IsMainSwordThrust =>
            Phase == AttackPhase.StabAttack || Phase == AttackPhase.PierceDash ||
            (Phase == AttackPhase.MeleeComboAttack && (ActiveMeleeComboMotion == ComboMotion.Thrust
                || ActiveMeleeComboMotion == ComboMotion.JoustDash
                || ActiveMeleeComboMotion == ComboMotion.LeapThrust));

        // Envelope ratios lifted from Nito's authored 255x62 thrust quad at his ~170px blade reach
        // (255/170 = 1.5, 62/170 = 0.365) — see VoidSlashVFX. Multiplying by each wielder's OWN live
        // reach instead of hardcoding pixels is what makes the shader read as "this sword's length".
        const float SlashQuadWidthMult = 1.5f;
        const float SlashQuadThrustHeightMult = 0.365f;

        /// <summary>Draws the VoidSlashVFX thrust sheath over the real sword during stabs. Sweeps are NOT
        /// drawn here: they are tracked VanillaSwordArc projectiles (ArtoriasSwordsman.SpawnArtoriasSwordArc).
        /// Purely visual — TickBladeHit carries the actual hitbox regardless of what this draws.</summary>
        void DrawSwordSlashVFX()
        {
            if (Main.dedServ)
            {
                return;
            }

            // A stab keeps the narrow sheath: a crescent would falsely promise danger to the sides.
            // IsMainSwordThrust is only true during the thrust's own attack phase.
            if (IsMainSwordThrust)
            {
                Vector2 pivot = PuppetHandPosition;
                Vector2 direction = PuppetWeaponDirection.SafeNormalize(new Vector2(NPC.direction, 0f));
                // The 70x70 greatsword's authored handle-to-tip diagonal is about 87px; ordinary
                // collision reach can be shorter, but the sheath must still meet the visible blade.
                float reach = Math.Max(86f, PuppetActiveBladeReach);
                float progress = PuppetWeaponAnimationProgress;
                Vector2 center = pivot + direction * (reach * 0.55f);
                float rotation = direction.ToRotation();

                VoidSlashVFX.DrawThrust(center, rotation,
                    new Vector2(reach * SlashQuadWidthMult, reach * SlashQuadThrustHeightMult),
                    progress, 0.9f, SlashDark, SlashMid, SlashCore, _swordSlashSequence * 0.31f);
            }
        }

        void TickProjectileSwordTelegraphs()
        {
            if (Main.dedServ)
            {
                return;
            }

            bool chargingProjectile = Phase == AttackPhase.AbyssSlashTelegraph
                || Phase == AttackPhase.AbyssSlashPause
                || Phase == AttackPhase.HomingVolleySwingTelegraph
                || Phase == AttackPhase.BoomerangSwingTelegraph
                || Phase == AttackPhase.SpiralFanSwingTelegraph;

            if (!chargingProjectile)
            {
                return;
            }

            Vector2 hand = PuppetHandPosition;
            Vector2 tip = PuppetWeaponTipPosition(54f);
            if (Phase == AttackPhase.BoomerangSwingTelegraph)
            {
                Vector2 bladeDirection = (tip - hand).SafeNormalize(new Vector2(NPC.direction, -1f));
                Vector2 tangent = bladeDirection.RotatedBy(MathHelper.PiOver2);
                for (int i = 0; i < 2; i++)
                {
                    Vector2 position = tip + Main.rand.NextVector2Circular(32f, 32f);
                    Vector2 velocity = (tip - position).SafeNormalize(Vector2.Zero)
                        * Main.rand.NextFloat(0.9f, 2.1f)
                        + tangent * Main.rand.NextFloat(-0.45f, 0.45f);
                    bool silver = Main.rand.NextBool(6);
                    Dust dust = Dust.NewDustPerfect(position,
                        silver ? DustID.SilverFlame : DustID.ShadowbeamStaff,
                        velocity, 100,
                        silver ? new Color(230, 220, 255) : new Color(144, 50, 225),
                        Main.rand.NextFloat(0.68f, 1.02f));
                    dust.noGravity = true;
                }
                return;
            }

            if (Phase == AttackPhase.SpiralFanSwingTelegraph)
            {
                // Spiral Fan fires in every direction, so on top of the blade dust below its tell gathers
                // inward from a ring at SpiralFanTelegraphDustRadius (2x the old spread) around his body.
                for (int i = 0; i < 2; i++)
                {
                    float ringAngle = Main.rand.NextFloat(MathHelper.TwoPi);
                    float ringRadius = SpiralFanTelegraphDustRadius
                        + Main.rand.NextFloat(-SpiralFanTelegraphDustJitter, SpiralFanTelegraphDustJitter);
                    Vector2 ringPosition = NPC.Center + ringAngle.ToRotationVector2() * ringRadius;
                    Vector2 inward = (NPC.Center - ringPosition).SafeNormalize(Vector2.Zero);
                    Vector2 ringVelocity = inward * Main.rand.NextFloat(1.2f, 2.4f);
                    bool silver = Main.rand.NextBool(4);
                    int ringDustType = DustID.ShadowbeamStaff;
                    Color ringTint = new Color(150, 60, 232);
                    if (silver)
                    {
                        ringDustType = DustID.SilverFlame;
                        ringTint = new Color(230, 220, 255);
                    }

                    Dust ringDust = Dust.NewDustPerfect(ringPosition, ringDustType, ringVelocity, 100,
                        ringTint, Main.rand.NextFloat(0.8f, 1.2f));
                    ringDust.noGravity = true;
                }
            }

            for (int i = 0; i < 1; i++)
            {
                Vector2 position = Vector2.Lerp(hand, tip, Main.rand.NextFloat(0.45f, 1f))
                    + Main.rand.NextVector2Circular(5f, 5f);
                bool white = Main.rand.NextBool(3);
                Dust dust = Dust.NewDustPerfect(position,
                    white ? DustID.SilverFlame : DustID.ShadowbeamStaff,
                    Main.rand.NextVector2Circular(0.7f, 0.7f), 90,
                    white ? Color.White : Color.DarkViolet,
                    Main.rand.NextFloat(0.9f, 1.35f));
                dust.noGravity = true;
            }
        }

        // At half health Artorias tears open the Abyss for the remainder of the fight.
        const int PersistentAbyssSurge = -1;
        const int AbyssSurgeTendrilCount = 10;
        const float AbyssSurgeTendrilSpeed = 11f;
        bool _abyssSurgeDone50;
        int _abyssSurgeTimer;
        bool _abyssSurgeWasActive; // local, unsynced: last tick's AbyssSurgeActive, for the one-shot surge cue
        public bool AbyssSurgeActive => _abyssSurgeTimer == PersistentAbyssSurge;

        void TickAbyssSurges()
        {
            // Tendril-flail cue for the 50% surge's tendril burst. StartAbyssSurge runs on the server only, so every machine
            // plays it on the tick it first sees the synced timer flip to active.
            bool surgeActive = AbyssSurgeActive;
            if (surgeActive && !_abyssSurgeWasActive && !Main.dedServ)
            {
                SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Custom/Artorias/Artorias_TendrilFlail") with { Volume = 0.7f },
                    NPC.Center);
            }
            _abyssSurgeWasActive = surgeActive;

            if (surgeActive)
            {
                // Once a second is enough: the debuff lasts 2s, so it never lapses, and it still expires within 2s
                // of the surge ending (kill, despawn, wipe) with no explicit clear. Re-applying every tick sent a
                // buff packet per player per tick from the server.
                if (Main.GameUpdateCount % 60 == 0)
                {
                    MaintainAbyssDebuff();
                }

                return;
            }

            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            float healthFraction = (float)NPC.life / NPC.lifeMax;
            if (!_abyssSurgeDone50 && healthFraction <= 0.50f)
            {
                _abyssSurgeDone50 = true;
                StartAbyssSurge();
            }
        }

        void MaintainAbyssDebuff()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player player = Main.player[i];
                if (player.active && !player.dead)
                {
                    player.AddBuff(ModContent.BuffType<Abyss>(), 2 * 60);
                }
            }
        }

        void StartAbyssSurge()
        {
            SoundEngine.PlaySound(SoundID.Item74 with { Volume = 0.8f, Pitch = -0.35f }, NPC.Center);
            UsefulFunctions.ScreenShake(NPC.Center, strength: 6f, frames: 20);
            _abyssSurgeTimer = PersistentAbyssSurge;
            MaintainAbyssDebuff();

            Projectile.NewProjectile(
                NPC.GetSource_FromThis(),
                NPC.Center,
                Vector2.Zero,
                ModContent.ProjectileType<ArtoriasSurgeAura>(),
                0,
                0f,
                Main.myPlayer,
                NPC.whoAmI);

            for (int i = 0; i < AbyssSurgeTendrilCount; i++)
            {
                float angle = MathHelper.TwoPi * i / AbyssSurgeTendrilCount;
                Vector2 velocity = angle.ToRotationVector2() * AbyssSurgeTendrilSpeed;
                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, velocity,
                    ModContent.ProjectileType<ArtoriasAbyssTendril>(), EnemyDamage.Projectile(TendrilGrabDamage), 0f,
                    Main.myPlayer, NPC.whoAmI);
            }

            NPC.netUpdate = true;
        }

        // ── Fixed abyss ring: a permanent lethal boundary at the spot Artorias first spawned ──
        /// <summary>
        /// Treats the live player-damage threshold as a circular wall for Artorias without teaching
        /// every dash, leap, dodge, and navigation state about the arena. The soft band removes only
        /// outward speed (so he naturally turns/slides along the edge), while the predicted-position
        /// clamp catches committed high-speed moves before a single tick can carry his hitbox through.
        /// A current-position clamp is still required for teleports and for a contracting ring moving
        /// past him. The half-diagonal keeps every corner of his rectangular hitbox inside the pink ring.
        /// </summary>
        void ConstrainArtoriasToAbyssRing()
        {
            float hitboxExtent = NPC.Size.Length() * 0.5f;
            float maxCenterDistance = Math.Max(0f,
                _currentRingRadius - RingBandHalfWidth - hitboxExtent - RingBossSafetyPadding);

            Vector2 fromRingCenter = NPC.Center - _ringCenter;
            float distance = fromRingCenter.Length();
            if (distance <= 0.001f)
            {
                return;
            }

            Vector2 outward = fromRingCenter / distance;
            if (distance > maxCenterDistance)
            {
                NPC.Center = _ringCenter + outward * maxCenterDistance;
                distance = maxCenterDistance;

                float outwardSpeed = Vector2.Dot(NPC.velocity, outward);
                if (outwardSpeed > 0f)
                {
                    NPC.velocity -= outward * outwardSpeed;
                }

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    NPC.netUpdate = true;
                }
            }

            float steeringStart = Math.Max(0f, maxCenterDistance - RingBossSteeringWidth);
            float radialSpeed = Vector2.Dot(NPC.velocity, outward);
            if (radialSpeed > 0f && distance > steeringStart)
            {
                float steeringProgress = MathHelper.Clamp(
                    (distance - steeringStart) / Math.Max(1f, maxCenterDistance - steeringStart), 0f, 1f);
                NPC.velocity -= outward * radialSpeed * MathHelper.SmoothStep(0f, 1f, steeringProgress);
            }

            Vector2 predictedOffset = NPC.Center + NPC.velocity - _ringCenter;
            float predictedDistance = predictedOffset.Length();
            if (predictedDistance > maxCenterDistance && predictedDistance > 0.001f)
            {
                Vector2 allowedCenter = _ringCenter + predictedOffset / predictedDistance * maxCenterDistance;
                NPC.velocity = allowedCenter - NPC.Center;
            }
        }

        void TickAbyssRing()
        {
            if (_ringVfxTimer > 0)
            {
                _ringVfxTimer--;
            }
            else
            {
                _ringVfxTimer = 10;
                UsefulFunctions.DustRingPrecise(_ringCenter,
                    _currentRingRadius - RingBandHalfWidth,
                    DustID.ShadowbeamStaff, 28, alpha: 105, scale: 0.86f);
                SpawnAbyssRingFlames();
            }

            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player player = Main.player[i];
                if (!player.active || player.dead)
                {
                    continue;
                }

                float dist = Vector2.Distance(player.Center, _ringCenter);
                if (_ringDamageCooldown[i] > 0)
                    _ringDamageCooldown[i]--;

                float safeRadius = _currentRingRadius - RingBandHalfWidth;
                if (dist >= safeRadius && _ringDamageCooldown[i] <= 0)
                {
                    Vector2 inward = (_ringCenter - player.Center).SafeNormalize(Vector2.UnitY);
                    int hitDir = inward.X < 0f ? -1 : 1;
                    player.Hurt(PlayerDeathReason.ByNPC(NPC.whoAmI), RingEdgeDamage, hitDir, dodgeable: false);
                    player.velocity = Vector2.Lerp(player.velocity,
                        inward * 7f + new Vector2(0f, -2.5f), 0.45f);
                    _ringDamageCooldown[i] = 60;
                }

                if (dist <= _currentRingRadius)
                {
                    player.AddBuff(ModContent.BuffType<TornWings>(), 120, false);
                }
            }
        }

        void SpawnAbyssRingFlames()
        {
            if (Main.dedServ)
            {
                return;
            }

            const int flameCount = 4;
            for (int i = 0; i < flameCount; i++)
            {
                float angle = Main.rand.NextFloat(MathHelper.TwoPi);
                float visibleDamageRadius = _currentRingRadius - RingBandHalfWidth;
                Vector2 ringPosition = _ringCenter + angle.ToRotationVector2() * visibleDamageRadius;
                Vector2 velocity = new Vector2(Main.rand.NextFloat(-0.35f, 0.35f),
                    Main.rand.NextFloat(-2.1f, -0.8f));
                bool pale = Main.rand.NextBool(7);
                Dust flame = Dust.NewDustPerfect(ringPosition,
                    pale ? DustID.SilverFlame : DustID.ShadowbeamStaff, velocity, 80,
                    pale ? new Color(218, 210, 242) : new Color(102, 34, 164),
                    Main.rand.NextFloat(0.72f, 1.08f));
                flame.noGravity = true;
            }
        }

        // ── Ring Collapse state machine ─────────────────────────────────────────────
        void TickRingCollapse()
        {
            // Collapse sound covers the 30t telegraph + 120t contraction (2.5s, thud as it stops); expand covers the 120t release.
            // Non-positional: the wall surrounds the player, and at 800px from the centre a positional cue would fade out.
            bool ringStateChanged = _ringCollapseState != _ringCollapseCueState;
            if (ringStateChanged && !Main.dedServ)
            {
                if (_ringCollapseState == RingCollapseState.Telegraph)
                {
                    SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Custom/Artorias/Artorias_RingCollapse") with { Volume = 0.7f });
                }
                else if (_ringCollapseState == RingCollapseState.Expanding)
                {
                    SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Custom/Artorias/Artorias_RingExpand") with { Volume = 0.7f });
                }
            }
            _ringCollapseCueState = _ringCollapseState;

            float hpFrac = (float)NPC.life / NPC.lifeMax;

            if (_ringCollapseState == RingCollapseState.Inactive)
            {
                if (!_ringCollapseDone50 && hpFrac <= 0.50f)
                {
                    _ringCollapseDone50 = true;
                    StartRingCollapse(PhaseTwoRingRadius);
                }
                else if (!_ringCollapseDone30 && hpFrac <= 0.30f)
                {
                    _ringCollapseDone30 = true;
                    StartRingCollapse(FinalPhaseRingRadius);
                }
                return;
            }

            switch (_ringCollapseState)
            {
                case RingCollapseState.Telegraph:
                    if (!Main.dedServ && Main.rand.NextBool(6))
                    {
                        UsefulFunctions.DustRingPrecise(_ringCenter,
                            _ringCollapseTo - RingBandHalfWidth,
                            DustID.PurpleTorch, 20, alpha: 100, scale: 1.1f);
                    }
                    if (--_ringCollapseTimer <= 0)
                    {
                        _ringCollapseState = RingCollapseState.Contracting;
                        _ringCollapseTimer = RingCollapseMoveTicks;
                    }
                    break;

                case RingCollapseState.Contracting:
                {
                    float t = 1f - _ringCollapseTimer / (float)RingCollapseMoveTicks;
                    _currentRingRadius = MathHelper.Lerp(_ringCollapseFrom, _ringCollapseTo, EaseInOut(t));
                    if (--_ringCollapseTimer <= 0)
                    {
                        _currentRingRadius = _ringCollapseTo;
                        if (_ringCollapseTo == PhaseTwoRingRadius)
                        {
                            _ringCollapseState = RingCollapseState.Holding;
                            _ringCollapseTimer = PhaseTwoRingHoldTicks;
                        }
                        else
                        {
                            _ringCollapseState = RingCollapseState.Inactive;
                        }
                        NPC.netUpdate = true;
                    }
                    break;
                }

                case RingCollapseState.Holding:
                    if (--_ringCollapseTimer <= 0)
                    {
                        _ringCollapseFrom = _currentRingRadius;
                        _ringCollapseTo = RingRadius;
                        _ringCollapseState = RingCollapseState.Expanding;
                        _ringCollapseTimer = RingCollapseMoveTicks;
                        NPC.netUpdate = true;
                    }
                    break;

                case RingCollapseState.Expanding:
                {
                    float t = 1f - _ringCollapseTimer / (float)RingCollapseMoveTicks;
                    _currentRingRadius = MathHelper.Lerp(_ringCollapseFrom, _ringCollapseTo, EaseInOut(t));
                    if (--_ringCollapseTimer <= 0)
                    {
                        _currentRingRadius = RingRadius;
                        _ringCollapseState = RingCollapseState.Inactive;
                        NPC.netUpdate = true;
                    }
                    break;
                }
            }
        }

        void StartRingCollapse(float targetRadius)
        {
            _ringCollapseFrom = _currentRingRadius;
            _ringCollapseTo = targetRadius;
            _ringCollapseState = RingCollapseState.Telegraph;
            _ringCollapseTimer = RingCollapseTelegraphTicks;
            NPC.netUpdate = true;
        }

        static float EaseInOut(float t) => t * t * (3f - 2f * t);

        public override void OnHitByItem(Player player, Item item, NPC.HitInfo hit, int damageDone)
        {
            base.OnHitByItem(player, item, hit, damageDone);
            tsorcRevampAIs.EvasiveOnHit(NPC, true);
        }

        public override void OnHitByProjectile(Projectile projectile, NPC.HitInfo hit, int damageDone)
        {
            base.OnHitByProjectile(projectile, hit, damageDone);
            tsorcRevampAIs.EvasiveOnHit(NPC, projectile.DamageType == DamageClass.Melee);
        }

        // ── Piercing Dash / Stabbing Piercing Dash hooks ────────────────────────────
        protected override void DoPierceWindup(int elapsed)
        {
            if (Main.dedServ)
            {
                return;
            }

            if (!Main.rand.NextBool(2))
            {
                return;
            }

            Vector2 pos = NPC.Center + new Vector2(NPC.direction * 22f, -6f);
            if (IsPierceStab)
            {
                Dust d = Dust.NewDustPerfect(pos, DustID.PurpleTorch, Vector2.Zero, 100, new Color(160, 40, 220), 1.1f);
                d.noGravity = true;
            }
            else
            {
                Dust d = Dust.NewDustPerfect(pos, DustID.SilverFlame, Vector2.Zero, 100, default, 0.9f);
                d.noGravity = true;
            }
        }

        protected override void DoPierceDashTick()
        {
            // Lunge cue on the dash's first tick: thorn counter for the red-sword stab, hornet dash for the plain one. The hook
            // runs before PhaseTimer counts down, and a client adopting PierceDash gets the full timer and the synced
            // _pierceIsStab, so it fires once everywhere with the right variant.
            if (PhaseTimer == PierceDashTicks && !Main.dedServ)
            {
                string dashSoundPath = "tsorcRevamp/Sounds/HollowKnight/hornet_dash";
                if (IsPierceStab)
                {
                    dashSoundPath = "tsorcRevamp/Sounds/HollowKnight/hero_thorn_counter";
                }

                SoundEngine.PlaySound(new SoundStyle(dashSoundPath) with { Volume = 0.7f }, NPC.Center);
            }

            if (Main.dedServ || !Main.rand.NextBool(3))
            {
                return;
            }

            Dust d = Dust.NewDustPerfect(NPC.Center, IsPierceStab ? DustID.PurpleTorch : DustID.SilverFlame,
                -NPC.velocity * 0.3f, 100, default, 1f);
            d.noGravity = true;
        }

        protected override void OnPierceContact(Player target, bool isStab)
        {
            // Impale cue, ahead of the client return below: PuppetNPC replays this hook on clients that adopt PierceStabHold.
            if (isStab && !Main.dedServ)
            {
                SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/DarkSouls/flesh-stab") with { Volume = 0.7f },
                    target.Center);
            }

            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            int damage = EnemyDamage.Projectile(isStab ? PierceStabDamage : PierceContactDamage);
            int hitDir = NPC.direction;
            int hitboxIndex = Projectile.NewProjectile(
                NPC.GetSource_FromThis(),
                target.Center,
                new Vector2(hitDir * 0.01f, 0f),
                ModContent.ProjectileType<PuppetMeleeHitbox>(),
                damage,
                isStab ? 0f : 6f,
                Main.myPlayer,
                target.width + 12f,
                target.height + 8f);
            tsorcGlobalProjectile.SetDefenseTraits(
                hitboxIndex, AttackDefenseTraits.BypassesActiveShield);

            if (!isStab)
            {
                return;
            }

            NPC.life = Math.Min(NPC.lifeMax, NPC.life + PierceStabHealAmount);
            NPC.HealEffect(PierceStabHealAmount);
            UsefulFunctions.ScreenShake(target.Center, strength: 6f, frames: 12);

            _impaleSwordProjIndex = Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero,
                ModContent.ProjectileType<ArtoriasImpalingSword>(), 0, 0f, Main.myPlayer, NPC.whoAmI, target.whoAmI);

            var modPlayer = target.GetModPlayer<tsorcRevampPlayer>();
            modPlayer.ImpaleFreezeTimer = 10;
            modPlayer.ImpaleWorldPosition = GetSwordTipWorldPosition();
        }

        protected override void DoPierceStabHoldTick(Player target, float raiseProgress01)
        {
            var modPlayer = target.GetModPlayer<tsorcRevampPlayer>();
            modPlayer.ImpaleFreezeTimer = 10;
            modPlayer.ImpaleWorldPosition = GetSwordTipWorldPosition();

            if (!Main.dedServ && Main.GameUpdateCount % 3 == 0)
            {
                // Along the actual blade line (Artorias -> through the target -> out the far side),
                // not just NPC.direction, so it stays aligned as the sword raises from horizontal to
                // vertical. The EXIT-side spray (front) is plain Dust, which already draws over the
                // player - the mirrored ENTRY-side spray (back, into Artorias) needs to draw BEHIND
                // the player instead, which Dust cannot do, so that half lives in
                // ArtoriasImpalingSword's own behind-the-player draw pass (see its _backBlood list).
                Vector2 bladeDir = (target.Center - NPC.Center).SafeNormalize(new Vector2(NPC.direction, 0f));
                Dust blood = Dust.NewDustPerfect(target.Center + Main.rand.NextVector2Circular(7f, 10f),
                    DustID.Blood, bladeDir.RotatedByRandom(0.4f) * Main.rand.NextFloat(1.8f, 4.8f),
                    70, new Color(120, 10, 24), Main.rand.NextFloat(0.85f, 1.25f));
                blood.noGravity = false;

                Dust abyss = Dust.NewDustPerfect(target.Center + Main.rand.NextVector2Circular(10f, 14f),
                    Main.rand.NextBool(4) ? DustID.SilverFlame : DustID.ShadowbeamStaff,
                    Main.rand.NextVector2Circular(1.6f, 1.6f), 100,
                    new Color(154, 48, 218), Main.rand.NextFloat(0.72f, 1.02f));
                abyss.noGravity = true;
            }

            _impaleTargetIndex = target.whoAmI;
        }

        protected override void OnPierceFlick(Player target)
        {
            var modPlayer = target.GetModPlayer<tsorcRevampPlayer>();
            modPlayer.ImpaleFreezeTimer = 0;

            if (!Main.dedServ)
            {
                SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/DarkSouls/player-flick") with { Volume = 0.7f },
                    target.Center);
            }

            // The flick's own hit - separate from the initial stab (PierceStabDamage) that landed back
            // at OnPierceContact. Server/singleplayer only, matching that hit's netMode gate.
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                int flickHitboxIndex = Projectile.NewProjectile(
                    NPC.GetSource_FromThis(),
                    target.Center,
                    new Vector2(NPC.direction * 0.01f, 0f),
                    ModContent.ProjectileType<PuppetMeleeHitbox>(),
                    EnemyDamage.Projectile(PierceFlickDamage),
                    6f,
                    Main.myPlayer,
                    target.width + 12f,
                    target.height + 8f);
                tsorcGlobalProjectile.SetDefenseTraits(
                    flickHitboxIndex, AttackDefenseTraits.BypassesActiveShield);
            }

            Vector2 away = new Vector2(NPC.direction, -0.3f);
            away.Normalize();
            target.Center += away * PierceFlickDistance;
            target.velocity = away * 24f;

            if (_impaleSwordProjIndex >= 0 && _impaleSwordProjIndex < Main.maxProjectiles)
            {
                Main.projectile[_impaleSwordProjIndex].Kill();
                _impaleSwordProjIndex = -1;
            }
            _impaleTargetIndex = -1;
        }

        /// <summary>Progress (0-1) through the PierceStabHold raise, or 1 once past it; 0 outside the
        /// sequence. Reaches 1 after PierceStabRaiseAnimTicks (a quick snap), not the whole hold -
        /// see that property's doc comment. Both the impale world position (GetSwordTipWorldPosition)
        /// and the held weapon's own rotation (PuppetNPC's PierceStabHold case) key off this, so they
        /// stay in lockstep: the target is centered on the raised blade immediately, not part-way
        /// through a slow multi-second drift.</summary>
        public float GetImpaleRaiseProgress01()
        {
            if (Phase == AttackPhase.PierceStabHold)
            {
                int elapsedTicks = PierceStabRaiseTicks - PhaseTimer;
                return PierceStabRaiseAnimTicks > 0
                    ? MathHelper.Clamp(elapsedTicks / (float)PierceStabRaiseAnimTicks, 0f, 1f)
                    : 1f;
            }
            if (Phase == AttackPhase.PierceStabFlick)
            {
                return 1f;
            }
            return 0f;
        }

        /// <summary>World position of the impaling sword's tip, read every tick by ArtoriasImpalingSword
        /// and used to anchor the frozen target. Sword starts pointed straight at the target (horizontal),
        /// raises to vertical over PierceStabHold, winds back behind his head, then flicks forward-and-down.</summary>
        public Vector2 GetSwordTipWorldPosition()
        {
            Vector2 dir;
            if (Phase == AttackPhase.PierceStabFlick)
            {
                // Same rotation the held blade is drawn at. Held-weapon convention -> world: PiOver4 is straight
                // ahead, so worldAngle = rotation - PiOver4, mirrored in X by facing.
                float flickRotation = GetPierceStabFlickRotation();
                float worldAngle = flickRotation - MathHelper.PiOver4;
                dir = new Vector2((float)Math.Cos(worldAngle) * NPC.direction, (float)Math.Sin(worldAngle));
            }
            else
            {
                dir = Vector2.Lerp(new Vector2(NPC.direction, 0f), new Vector2(0f, -1f), GetImpaleRaiseProgress01());
            }
            if (dir != Vector2.Zero)
            {
                dir.Normalize();
            }
            return NPC.Center + dir * ImpaleSwordReach;
        }

        // ── Jumping Downward Slash hooks ─────────────────────────────────────────
        protected override void DoJumpSlashDodgebackTick()
        {
            if (Main.dedServ)
            {
                return;
            }
            if (Main.rand.NextBool(2))
            {
                Dust d = Dust.NewDustPerfect(NPC.Center, DustID.SilverFlame, -NPC.velocity * 0.4f, 100, default, 0.8f);
                d.noGravity = true;
            }
        }

        protected override void DoJumpSlashRiseTick()
        {
            if (Main.dedServ || !Main.rand.NextBool(2))
            {
                return;
            }
            Vector2 pos = NPC.Center + new Vector2(NPC.direction * 20f, -10f);
            Dust d = Dust.NewDustPerfect(pos, DustID.SilverFlame, Vector2.Zero, 100, default, 1f);
            d.noGravity = true;
        }

        protected override void DoJumpSlashAttack()
        {
            SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.7f, PitchVariance = 0.15f }, NPC.Center);
            TryMeleeHit(reach: 100f);
            SpawnArtoriasSwordArc(JumpSlashAttackTicks, hitWindowEnd: JumpSlashCurve.HitWindowEnd);
            // The swing often starts mid-air (target in reach), so the ground AOE is queued for real touchdown in AI.
            _jumpSlashImpactPending = true;
        }

        // Jump Slash's ground AOE: 3x the base landing impact (86x68), damaging over exactly the eruption it draws.
        const float JumpSlashImpactWidth = 86f * 3f;
        const float JumpSlashImpactHeight = 68f * 3f;
        const int JumpSlashImpactDamage = 140;
        bool _jumpSlashImpactPending;

        // ── Forward Flip Slash hooks ─────────────────────────────────────────────
        // The somersault trails the same violet as the Homing Volley orbs its landing releases.
        protected override void DoFlipSlashRiseTick()
        {
            if (Main.dedServ || !Main.rand.NextBool(2))
            {
                return;
            }

            Dust trail = Dust.NewDustPerfect(NPC.Center, DustID.PurpleTorch, -NPC.velocity * 0.3f, 100,
                new Color(160, 40, 220), 1.1f);
            trail.noGravity = true;
        }

        // Fires as the somersault launches: arm the spinning blade (the base tests it every airborne tick)
        // and track one crescent on it for the flight. 2 * launch speed / 0.3 gravity = flat-ground airtime.
        protected override void DoFlipSlashHit()
        {
            SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.7f, PitchVariance = 0.15f }, NPC.Center);
            TryMeleeHit(reach: ArtoriasSwordArcRadius);

            int flightTicks = (int)Math.Ceiling(2f * FlipSlashLaunchUpSpeed / 0.3f);
            SpawnArtoriasSwordArc(flightTicks);
        }

        protected override void OnFlipSlashLand()
        {
            // FlipSlashRise refreshes the shared dodge timer every airborne tick. Artorias's
            // planted landing is a punish window, so discard the final refresh immediately:
            // DodgeTimer otherwise grants i-frames and makes the global draw hook blink him.
            NPC.GetGlobalNPC<tsorcRevampGlobalNPC>().DodgeTimer = 0;

            // Re-arm for the overhead into the ground (live FlipSlashStrikeLiveTicks from touchdown), and
            // give it its own crescent that fades once the blade stops being a hitbox.
            SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.8f, Pitch = -0.25f, PitchVariance = 0.1f }, NPC.Center);
            TryMeleeHit(reach: ArtoriasSwordArcRadius);

            int strikeTicks = FlipSlashStrikeEaseInTicks + FlipSlashStrikeEaseOutTicks;
            float strikeHitWindowEnd = FlipSlashStrikeLiveTicks / (float)strikeTicks;
            SpawnArtoriasSwordArc(strikeTicks, hitWindowEnd: strikeHitWindowEnd);
        }

        // The blade meets the ground on the strike's peak tick, so the impact and the orb release fire
        // there instead of at touchdown, where they used to precede the swing.
        // Lifts the release point this far above the feet: the blade tip is buried in the floor at contact.
        const float FlipSlashOrbReleaseHeight = 12f;
        const int FlipBlastDamage = 110;
        const int FlipPillarDamage = 130;
        const int FlipBlazeDamage = 150;
        const float FlipBlazeSpeed = 5f;

        protected override void OnFlipSlashStrikeContact()
        {
            SpawnLandingImpactVFX(NPC.Bottom, 96f, 78f);
            UsefulFunctions.ScreenShake(NPC.Center, strength: 5f, frames: 11);
            SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.65f, Pitch = -0.1f }, NPC.Center);

            if (Main.netMode == NetmodeID.MultiplayerClient || !NPC.HasValidTarget)
            {
                return;
            }

            // Ground AOE at the feet, a coin flip per landing: the circular abyss blast (lifted 0.35 of its radius so
            // most of it sits above the floor) or the bottom-anchored eruption pillar.
            if (Main.rand.NextBool())
            {
                Vector2 blastCenter = NPC.Bottom - new Vector2(0f, ArtoriasAbyssBlast.Radius * 0.35f);
                Projectile.NewProjectile(NPC.GetSource_FromThis(), blastCenter, Vector2.Zero,
                    ModContent.ProjectileType<ArtoriasAbyssBlast>(), EnemyDamage.Projectile(FlipBlastDamage), 0f, Main.myPlayer);
            }
            else
            {
                Vector2 pillarCenter = NPC.Bottom - new Vector2(0f, ArtoriasAbyssPillar.Height * 0.5f);
                Projectile.NewProjectile(NPC.GetSource_FromThis(), pillarCenter, Vector2.Zero,
                    ModContent.ProjectileType<ArtoriasAbyssPillar>(), EnemyDamage.Projectile(FlipPillarDamage), 0f, Main.myPlayer);

                // The pillar's partner: two purple flame walls peel off left and right along the ground from the feet,
                // growing tall as they travel (ArtoriasAbyssBlaze pins its feet to the spawn Y).
                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Bottom, new Vector2(-FlipBlazeSpeed, 0f),
                    ModContent.ProjectileType<ArtoriasAbyssBlaze>(), EnemyDamage.Projectile(FlipBlazeDamage), 0f, Main.myPlayer);
                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Bottom, new Vector2(FlipBlazeSpeed, 0f),
                    ModContent.ProjectileType<ArtoriasAbyssBlaze>(), EnemyDamage.Projectile(FlipBlazeDamage), 0f, Main.myPlayer);
            }

            // The slam releases a Homing Volley from where the blade struck: the same HomingAbyssOrb (HomingVolleyOrbDamage,
            // passes through tiles, straight flight then a late bend onto the player), in one of the volley's
            // three patterns. The orbs' own pre-bend flash is the telegraph.
            Player target = Main.player[NPC.target];
            Vector2 releasePoint = PuppetWeaponTipPosition(54f);
            releasePoint.Y = Math.Min(releasePoint.Y, NPC.Bottom.Y - FlipSlashOrbReleaseHeight);

            int pattern = Main.rand.Next(3);
            if (pattern == 0)
            {
                FireStaggeredFan(releasePoint, target);
            }
            else if (pattern == 1)
            {
                FirePincerSplit(releasePoint, target);
            }
            else
            {
                FireLatticeSnap(releasePoint, target);
            }
        }

        // ── Abyss Slash hooks ─────────────────────────────────────────────────────
        // Three swipe-count/timing variants, rolled once at the very first swipe:
        //   0: one slash, then an overhead swing that fans 3 seeking orbs upper-left/up/upper-right
        //   1: three slashes, 60 ticks apart
        //   2: two slashes 30 ticks apart, then a third 60 ticks later, then two more 30 ticks apart
        const int AbyssSlashDamage = 120;
        const int AbyssOrbFinisherDamage = 130;
        const float AbyssSlashSpeed = 9f;

        int _abyssSlashVariant;
        static readonly int[][] AbyssSlashGapTables = new int[][]
        {
            // Gaps count from the END of a swipe. The labelled 40/60/30 are the spacing after the old 16t swipe;
            // every value is 6t under its label because the Weighted swipe is 22t, keeping the release spacing.
            new int[] { 34 },              // variant 0: swipe0 -> 40 ticks -> orb finisher (swipe1)
            new int[] { 54, 54 },          // variant 1: swipe0 -> 60 -> swipe1 -> 60 -> swipe2
            new int[] { 24, 54, 24, 24 },  // variant 2: swipe0 ->30-> swipe1 ->60-> swipe2 ->30-> swipe3 ->30-> swipe4
        };

        protected override int NextAbyssSlashDelay(int completedSwipeIndex)
        {
            int[] gaps = AbyssSlashGapTables[_abyssSlashVariant];
            return completedSwipeIndex < gaps.Length ? gaps[completedSwipeIndex] : -1;
        }

        protected override void DoAbyssSlashFire(int swipeIndex)
        {
            // Both variants release on a real AbyssSlashSwipe sweep, the orb finisher included.
            SpawnArtoriasSwordArc(AbyssSlashSwipeTicks, hitWindowEnd: AbyssSlashSwipeCurve.HitWindowEnd);

            if (swipeIndex == 0)
            {
                _abyssSlashVariant = Main.rand.Next(AbyssSlashGapTables.Length);
            }

            bool isOrbFinisher = _abyssSlashVariant == 0 && swipeIndex == 1;
            if (isOrbFinisher)
            {
                FireAbyssOrbFinisher();
            }
            else
            {
                FireAbyssSlashProjectile();
            }
        }

        void FireAbyssSlashProjectile()
        {
            SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.6f, Pitch = -0.1f }, NPC.Center);

            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            // Aims at the player's current position, so an airborne/jumping target naturally gets an
            // angled shot rather than a flat horizontal one.
            Player target = Main.player[NPC.target];
            Vector2 vel = UsefulFunctions.Aim(NPC.Center, target.Center, AbyssSlashSpeed);
            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, vel,
                ModContent.ProjectileType<AbyssSlash>(), EnemyDamage.Projectile(AbyssSlashDamage), 0f, Main.myPlayer, NPC.whoAmI + 1);
        }

        void FireAbyssOrbFinisher()
        {
            SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.6f, Pitch = -0.2f }, NPC.Center);

            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            // Upper-left, straight up, upper-right.
            float[] angles = { -MathHelper.PiOver2 - MathHelper.PiOver4, -MathHelper.PiOver2, -MathHelper.PiOver2 + MathHelper.PiOver4 };
            foreach (float angle in angles)
            {
                Vector2 vel = angle.ToRotationVector2() * 4f;
                // ai[1] is offset by +1 (0 = "no owner") since ArtoriasAbyssBlast's own orb-fan
                // spawns this same projectile without an owner - see ArtoriasFlameOrb.OnHitPlayer.
                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, vel,
                    ModContent.ProjectileType<ArtoriasFlameOrb>(), EnemyDamage.Projectile(AbyssOrbFinisherDamage), 0f, Main.myPlayer, 0f, NPC.whoAmI + 1);
            }
        }

        // ── Abyss Tendril Grab hooks ─────────────────────────────────────────────
        protected override void DoTendrilTelegraphTick(int elapsed)
        {
            if (Main.dedServ)
            {
                return;
            }

            // Wind-up cue: the 0.4s peak of TendrilFlail, cut to the 24-tick telegraph so it ends as the tendril launches.
            if (elapsed == 0)
            {
                SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Custom/Artorias/Artorias_TendrilFlail_Windup") with { Volume = 0.7f },
                    NPC.Center);
            }

            // The shader supplies the mass; these few particles provide physical edge breakup.
            Vector2 handPos = PuppetHandPosition;
            int count = elapsed > TendrilTelegraphTicks * 0.65f ? 2 : 1;
            for (int i = 0; i < count; i++)
            {
                bool bright = Main.rand.NextBool(8);
                int type = bright ? DustID.SilverFlame
                    : Main.rand.NextBool(3) ? DustID.ShadowbeamStaff : DustID.Smoke;
                Color tint = bright ? new Color(230, 224, 255)
                    : type == DustID.Smoke ? new Color(8, 5, 14) : new Color(108, 34, 172);
                Vector2 offset = Main.rand.NextVector2Circular(18f, 24f);
                Dust d = Dust.NewDustPerfect(handPos + offset, type,
                    -offset * Main.rand.NextFloat(0.035f, 0.075f), 130, tint,
                    Main.rand.NextFloat(0.68f, 1f));
                d.noGravity = true;
            }
        }

        protected override void DoTendrilLaunch()
        {
            SoundEngine.PlaySound(SoundID.NPCDeath6 with { Volume = 0.4f, Pitch = -0.4f }, NPC.Center);

            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            Player target = Main.player[NPC.target];
            Vector2 origin = PuppetHandPosition;
            Vector2 vel = UsefulFunctions.Aim(origin, target.Center, TendrilLaunchSpeed);
            Projectile.NewProjectile(NPC.GetSource_FromThis(), origin, vel,
                ModContent.ProjectileType<ArtoriasAbyssTendril>(), EnemyDamage.Projectile(TendrilGrabDamage), 0f,
                Main.myPlayer, NPC.whoAmI, 0f, TendrilTopSpeed);
        }

        protected override void DoTendrilReachTick()
        {
            if (Main.dedServ || !Main.rand.NextBool(3))
            {
                return;
            }

            Vector2 handPos = PuppetHandPosition;
            int type = Main.rand.NextBool(4) ? DustID.ShadowbeamStaff : DustID.Smoke;
            Color tint = type == DustID.Smoke ? new Color(7, 5, 13) : new Color(104, 34, 170);
            Dust d = Dust.NewDustPerfect(handPos + Main.rand.NextVector2Circular(12f, 16f),
                type, Main.rand.NextVector2Circular(0.4f, 0.4f), 130, tint, 0.78f);
            d.noGravity = true;
        }

        protected override void DoTendrilSwing()
        {
            SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.7f, PitchVariance = 0.15f }, NPC.Center);
            TryMeleeHit(reach: 100f);
            SpawnArtoriasSwordArc(TendrilSwingTicks, hitWindowEnd: TendrilSwingCurve.HitWindowEnd);
        }

        // ── Aerial answers: Rising Uppercut + Skyward Lunge ──────────────────────────────────────
        // Both punish a target that stays airborne. They run on PuppetNPC's generic Custom phase (DoCustomTick below)
        // with one shared stage machine, and alternate like a two-entry bag.
        //
        // Trigger:  target airborne >= 30t (hook-hanging counts), Artorias grounded, within 600px, shared 360t cooldown.
        //           Opener: 6% per neutral tick. Follow-up: straight out of a melee recovery, skipping it. The ending
        //           swing's tail plus either move's own tell keeps the next hit >= 30t after its live window.
        //
        // Rising Uppercut
        // Tell:     a sprint (TopSpeed x 2.2 = 5.3 px/t), blade dragged down-back into the ground (2.75) throwing
        //           sparks, >= 20t on screen, up to 150t.
        // Takeoff:  rise h = target height gap + 24px, clamped 48..320px (20 tiles); vy = sqrt(2 * 0.3 * h) <= 14.4 px/t,
        //           rise time T = vy / 0.3 <= 48t. He takes off once dx / T <= 9 px/t, then steers 10%/tick on the ascent.
        // Strike:   when the target is within 120px (95px blade + bodies): 2.75 -> -1.35, 235° envelope,
        //           in 4 / out 30, k 7.5 -> 44°/t peak (fastest cut in his kit), live 8.8t ~182°. No one in reach by the
        //           apex = no swing; he falls with the blade low.
        // Open:     25t harmless tail + 40t planted landing.
        //
        // Skyward Lunge
        // Jump:     a fixed vertical hop (rise = SkywardJumpRise, ~28t to the apex under AerialGravity) - straight up,
        //           no horizontal steering; the dash afterward covers the ground.
        // Tell:     30t paused at the jump's apex (gravity suspended): blade points at the solved intercept point,
        //           body and sword shake 1 -> 3px, void motes converge on the tip, and the last 20t draw a dust line
        //           along the dash path.
        // Dash:     straight line, gravity off, at a point SkywardAimPastTarget px beyond the target's position (led by
        //           distance / 16, <= 70t), so the blade passes through them and he lands past them. Speed ramps
        //           0 -> 16 px/t with the square of time over 5t. Ends on arrival + 8t overshoot, 70t max, a tile or a
        //           blade hit; the blade is live the whole dash.
        // Miss:     momentum x0.35 and a normal fall. 50% roll: double jump as the fall starts (cloud ring at the feet),
        //           ballistic arc solved at the target (vy <= 11, vx <= 8 px/t), carrying the pose of the swing its height
        //           calls for. Within 120px: target above his centre -> underhand 2.05 -> -1.55, in 6 / out 20, k 6
        //           (~39°/t); else overhand -1.48 -> 2.29, in 8 / out 22, k 6.5 (~36°/t). Cut starts from the carried pose.
        // Open:     50t planted landing.
        // Counter:  air-roll through the 8.8t uppercut, or across the drawn dash line as it launches (it covers 100+px
        //           inside the 22t roll); landing before 30t airborne never arms either move. Hyper-armoured until the
        //           landing beat (Artorias.AI), except as an opener from neutral: the uppercut's whole run and the
        //           whole ~28t Skyward Lunge jump (not the apex pause after it) are staggerable. As a follow-up
        //           chain, no part of the tell is.
        enum AerialStage { None, UppercutRun, UppercutRise, SkywardJump, SkywardAim, SkywardDash, SkywardFall, SkywardArc, Strike, Falling, Landing }

        const int AerialTargetAirborneTicks = 30;
        const int AerialCooldownTicks = 360;
        const int AerialOpenerChance = 6;
        const float AerialMaxRange = 600f;
        // Keeps the base Custom case from timing the phase out; every stage ends itself explicitly.
        const int AerialPhaseSafetyTicks = 600;
        // Jump (~28t) + aim pause (30t) + dash (<= 70t) + a fall/landing margin, with room to spare.
        const int AerialMaxAirTicks = 200;
        const float AerialStrikeRange = 120f;
        const float AerialGravity = 0.3f;

        const float UppercutRunSpeedMult = 2.2f;
        const int UppercutRunMinTicks = 20;
        const int UppercutRunMaxTicks = 150;
        const float UppercutMinRise = 48f;
        const float UppercutMaxRise = 320f;
        const float UppercutRiseOvershoot = 24f;
        const float UppercutMaxForwardSpeed = 9f;
        const float UppercutAscentTracking = 0.10f;
        const float UppercutCarryRotation = 2.75f;
        const float UppercutFinishRotation = -1.35f;
        const int UppercutLandingTicks = 40;
        static readonly WeightedSwing UppercutCurve = new WeightedSwing(4, 30, 7.5f);

        // Fixed vertical hop before the aim/dash - straight up, no target-relative steering (contrast
        // UppercutRun, which solves its jump to meet the target's height). ~28t to the apex under
        // AerialGravity: launchSpeed = sqrt(2 * 0.3 * 120) = 8.49, riseTicks = 8.49 / 0.3.
        const float SkywardJumpRise = 120f;
        const int SkywardAimTicks = 30;
        const int SkywardAimLineTicks = 20;
        const float SkywardDashSpeed = 16f;
        const int SkywardDashAccelTicks = 5;
        // Must be able to cover AerialMaxRange (600px) plus drift during the 30t apex pause, or the dash
        // times out and stops short of a target who never moved. 600px alone already needs ~49t (600/16
        // path + 3.33 ramp-loss + 8 overshoot) - the old fixed 40 whiffed even a stationary max-range target.
        const int SkywardDashMaxTicks = 70;
        const int SkywardOvershootTicks = 8;
        // Horizontal distance past the intercept point the dash aims at. Aiming AT the target's centre from the apex
        // put his feet on the floor (collideY -> dash blocked) just before his centre arrived, stopping him short.
        // Moving the aim point beyond them flattens the dive: at a typical 300px x 120px drop the line crosses the
        // target ~30px above their centre (well inside the 95px blade) and meets the floor past them.
        const float SkywardAimPastTarget = 96f;
        const float SkywardMissMomentum = 0.35f;
        const int SkywardDoubleJumpChance = 50;
        const float SkywardDoubleJumpMaxRise = 11f;
        const float SkywardDoubleJumpMaxForward = 8f;
        const float SkywardArcApexMargin = 60f;
        const int SkywardLandingTicks = 50;
        const float AirUnderhandStartRotation = 2.05f;
        const float AirUnderhandEndRotation = -1.55f;
        const float AirOverhandStartRotation = -1.48f;
        const float AirOverhandEndRotation = 2.29f;
        static readonly WeightedSwing AirUnderhandCurve = new WeightedSwing(6, 20, 6f);
        static readonly WeightedSwing AirOverhandCurve = new WeightedSwing(8, 22, 6.5f);

        AerialStage _aerialStage;
        int _aerialStageTicks;
        int _aerialStageSequence; // bumped by every EnterAerialStage on every machine; clients reconcile against it
        int _aerialAirTicks;
        int _aerialLandingTicks;
        int _aerialCooldown;
        int _targetAirborneTicks;
        float _aerialRotation;
        float _aerialVelocityX;
        bool _aerialBladeHit;
        bool _lastAerialWasLunge;
        bool _aerialDoubleJumpPending;
        Vector2 _skywardDirection;
        int _skywardDashTicks;
        float _strikeStartRotation;
        float _strikeEndRotation;
        WeightedSwing _strikeCurve;

        protected override bool SlowDownDuringCustom => _aerialStage == AerialStage.None;

        protected override float? CustomWeaponRotation => _aerialStage == AerialStage.None ? null : _aerialRotation;

        // Skyward Lunge's tell: the whole rig (body and sword) shakes, 1 -> 3px across the aim. Two sines at unrelated
        // rates instead of Main.rand, so every draw call in a frame agrees. Visual only; the hitbox stays put.
        protected override Vector2 PuppetVisualOffset
        {
            get
            {
                if (_aerialStage != AerialStage.SkywardAim)
                {
                    return Vector2.Zero;
                }

                float aimProgress = MathHelper.Clamp(_aerialStageTicks / (float)SkywardAimTicks, 0f, 1f);
                float amplitude = MathHelper.Lerp(1f, 3f, aimProgress);
                float time = Main.GameUpdateCount;
                float shakeX = (float)Math.Sin(time * 2.7f) * amplitude;
                float shakeY = (float)Math.Sin(time * 3.9f) * amplitude * 0.5f;
                return new Vector2(shakeX, shakeY);
            }
        }

        protected override void OnBladeHit(Player player)
        {
            base.OnBladeHit(player);
            _aerialBladeHit = true;
        }

        /// <summary>Counts how long the target has been off the ground and, past 30 ticks, starts a Rising Uppercut or a
        /// Skyward Lunge: as an opener from neutral, or straight out of a melee recovery.</summary>
        void TickAerialTriggers()
        {
            if (_aerialCooldown > 0)
            {
                _aerialCooldown--;
            }

            // Starting, cancelling and the target-airborne count are server decisions (the opener rolls). Clients follow the
            // stage from the snapshot EnterAerialStage requests; a client-side cancel here could drop a stage whose Custom
            // phase simply hasn't been adopted yet.
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            // Something else (a stagger, the nova) took the Custom phase away mid-attack: drop the state so the next
            // one starts clean. SmartFighter4 hands gravity back by itself on its next tick.
            if (_aerialStage != AerialStage.None && Phase != AttackPhase.Custom)
            {
                EnterAerialStage(AerialStage.None);
                DebugAttackLabel = null;
            }

            if (!NPC.HasValidTarget)
            {
                _targetAirborneTicks = 0;
                return;
            }

            // Airborne = not standing on anything, hook-hanging included. Grounded needs two still ticks in a row, so
            // the zero-velocity apex of a jump doesn't reset the count.
            Player target = Main.player[NPC.target];
            bool targetGrounded = target.velocity.Y == 0f && target.oldVelocity.Y == 0f && target.grappling[0] < 0;
            if (targetGrounded)
            {
                _targetAirborneTicks = 0;
            }
            else
            {
                _targetAirborneTicks++;
            }

            bool canStart = _aerialStage == AerialStage.None && _aerialCooldown <= 0 && !HoldAttackSelection
                && NPC.velocity.Y == 0f && _targetAirborneTicks >= AerialTargetAirborneTicks;
            if (!canStart)
            {
                return;
            }

            float distance = NPC.Distance(target.Center);
            if (distance > AerialMaxRange)
            {
                return;
            }

            // Follow-up windows: every melee recovery, plus the Flip Slash landing hold once its strike is harmless.
            int flipHoldElapsed = FlipSlashLandHoldTicks - PhaseTimer;
            bool flipStrikeSpent = Phase == AttackPhase.FlipSlashLand && flipHoldElapsed > FlipSlashStrikeLiveTicks;
            bool inMeleeRecovery = Phase == AttackPhase.MeleeRecovery || Phase == AttackPhase.MeleeComboRecovery
                || Phase == AttackPhase.JumpSlashRecovery || Phase == AttackPhase.TendrilRecovery || flipStrikeSpent;
            bool neutral = Phase == AttackPhase.Idle || Phase == AttackPhase.CasualStroll
                || Phase == AttackPhase.ClosingDistance;
            bool openerRoll = neutral && Main.rand.Next(100) < AerialOpenerChance;
            if (!inMeleeRecovery && !openerRoll)
            {
                return;
            }

            // Alternate the two moves, except the uppercut can't reach a target more than 20 tiles above him.
            float riseNeeded = NPC.Center.Y - target.Center.Y;
            bool uppercutReachable = riseNeeded <= UppercutMaxRise;
            bool useLunge = true;
            if (_lastAerialWasLunge && uppercutReachable)
            {
                useLunge = false;
            }

            _lastAerialWasLunge = useLunge;
            AerialStage openingStage = AerialStage.UppercutRun;
            if (useLunge)
            {
                openingStage = AerialStage.SkywardJump;
            }

            EnterAerialStage(openingStage);
            PlayAerialStageStartCue(openingStage);
            _aerialAirTicks = 0;
            _aerialBladeHit = false;
            _aerialDoubleJumpPending = false;
            _aerialCooldown = AerialCooldownTicks;
            // Start from where the blade is actually drawn, so entering the move never snaps the sword.
            _aerialRotation = BladeRotationToward(PuppetWeaponDirection);
            StartCustomAttack(AerialPhaseSafetyTicks, MeleeWeaponItemType, swingPose: false);
        }

        protected override void DoCustomTick(int ticksRemaining)
        {
            if (_aerialStage == AerialStage.None)
            {
                return;
            }

            PhaseTimer = AerialPhaseSafetyTicks;
            _aerialStageTicks++;

            Player target = Main.player[NPC.target];
            bool grounded = NPC.velocity.Y == 0f;
            if (!grounded)
            {
                _aerialAirTicks++;
            }

            // Stuck in the air far longer than any arc takes: end the attack and let the navigator recover.
            if (_aerialAirTicks > AerialMaxAirTicks)
            {
                NPC.noGravity = false;
                EnterAerialStage(AerialStage.None);
                DebugAttackLabel = null;
                PhaseTimer = 1;
                return;
            }

            switch (_aerialStage)
            {
                case AerialStage.UppercutRun:
                {
                    FaceTarget(target);
                    NPC.velocity.X = NPC.direction * TopSpeed * UppercutRunSpeedMult;
                    _aerialRotation = MathHelper.Lerp(_aerialRotation, UppercutCarryRotation, 0.25f);

                    // Sparks where the dragged tip meets the floor, thrown back behind the run.
                    if (!Main.dedServ && Main.rand.NextBool(2))
                    {
                        Vector2 scrapePoint = PuppetWeaponTipPosition(ArtoriasSwordArcRadius);
                        scrapePoint.Y = Math.Min(scrapePoint.Y, NPC.Bottom.Y - 2f);
                        Vector2 sparkVelocity = new Vector2(-NPC.direction * Main.rand.NextFloat(1.5f, 3.5f),
                            Main.rand.NextFloat(-2.5f, -0.5f));
                        Dust spark = Dust.NewDustPerfect(scrapePoint, DustID.SilverFlame, sparkVelocity, 100, default,
                            Main.rand.NextFloat(0.8f, 1.2f));
                        spark.noGravity = false;
                    }

                    // Jump solve: rise to the target's height + 24px (48..320px), vy = sqrt(2 g h), rise time T = vy / g.
                    // Take off once the forward speed that meets the target's drifting X at the apex is <= 9 px/t.
                    float riseHeight = MathHelper.Clamp(NPC.Center.Y - target.Center.Y + UppercutRiseOvershoot,
                        UppercutMinRise, UppercutMaxRise);
                    float launchSpeed = (float)Math.Sqrt(2f * AerialGravity * riseHeight);
                    float riseTicks = launchSpeed / AerialGravity;
                    float predictedTargetX = target.Center.X + target.velocity.X * riseTicks * 0.5f;
                    float horizontalGap = predictedTargetX - NPC.Center.X;
                    float neededForwardSpeed = Math.Abs(horizontalGap) / riseTicks;
                    bool jumpReaches = neededForwardSpeed <= UppercutMaxForwardSpeed;
                    bool tellShown = _aerialStageTicks >= UppercutRunMinTicks;

                    if (tellShown && jumpReaches && grounded)
                    {
                        _aerialVelocityX = horizontalGap / riseTicks;
                        NPC.velocity = new Vector2(_aerialVelocityX, -launchSpeed);
                        NPC.netUpdate = true;
                        EnterAerialStage(AerialStage.UppercutRise);
                        PlayAerialStageStartCue(AerialStage.UppercutRise);
                    }
                    else if (_aerialStageTicks >= UppercutRunMaxTicks)
                    {
                        // Never found a reachable jump (walled off, or the target kept its distance): stand down.
                        BeginAerialLanding();
                    }
                    break;
                }

                case AerialStage.UppercutRise:
                {
                    // Steer 10%/tick toward the forward speed that still meets the target at the apex, blade held low
                    // and back. The cut starts the moment the target is inside the blade's reach.
                    FaceTarget(target);
                    float remainingRiseTicks = Math.Max(1f, -NPC.velocity.Y / AerialGravity);
                    float desiredVelocityX = (target.Center.X - NPC.Center.X) / remainingRiseTicks;
                    desiredVelocityX = MathHelper.Clamp(desiredVelocityX, -UppercutMaxForwardSpeed, UppercutMaxForwardSpeed);
                    _aerialVelocityX = MathHelper.Lerp(_aerialVelocityX, desiredVelocityX, UppercutAscentTracking);
                    NPC.velocity.X = _aerialVelocityX;
                    _aerialRotation = MathHelper.Lerp(_aerialRotation, UppercutCarryRotation, 0.25f);

                    float targetDistance = NPC.Distance(target.Center);
                    bool pastApex = NPC.velocity.Y >= 0f && _aerialStageTicks > 2;
                    if (targetDistance <= AerialStrikeRange)
                    {
                        BeginAerialStrike(UppercutFinishRotation, UppercutCurve);
                    }
                    else if (pastApex)
                    {
                        EnterAerialStage(AerialStage.Falling);
                    }
                    break;
                }

                case AerialStage.SkywardJump:
                {
                    FaceTarget(target);

                    if (_aerialStageTicks == 1)
                    {
                        float launchSpeed = (float)Math.Sqrt(2f * AerialGravity * SkywardJumpRise);
                        NPC.velocity = new Vector2(0f, -launchSpeed);
                        NPC.netUpdate = true;
                    }

                    // Past the apex (velocity has turned downward): freeze him there, gravity suspended,
                    // for the aim/pause instead of letting the arc carry straight into a fall.
                    if (NPC.velocity.Y >= 0f && _aerialStageTicks > 1)
                    {
                        NPC.noGravity = true;
                        NPC.velocity = Vector2.Zero;
                        EnterAerialStage(AerialStage.SkywardAim);
                        PlayAerialStageStartCue(AerialStage.SkywardAim);
                    }
                    break;
                }

                case AerialStage.SkywardAim:
                {
                    // SmartFighter4 hands gravity back every tick (see the SkywardDash case) - re-assert
                    // it here too, or the apex pause silently turns into a fall partway through.
                    NPC.noGravity = true;
                    NPC.velocity = Vector2.Zero;
                    FaceTarget(target);

                    // Intercept: lead the target's velocity by the dash's flight time (distance / 16, capped 40t),
                    // refined once against the led position.
                    float leadTicks = Math.Min(NPC.Distance(target.Center) / SkywardDashSpeed, SkywardDashMaxTicks);
                    Vector2 ledPosition = target.Center + target.velocity * leadTicks;
                    float refinedLeadTicks = Math.Min(Vector2.Distance(NPC.Center, ledPosition) / SkywardDashSpeed,
                        SkywardDashMaxTicks);
                    Vector2 interceptPoint = target.Center + target.velocity * refinedLeadTicks;

                    // Aim past the intercept on the far side, so the dash carries through the target (see SkywardAimPastTarget).
                    // Straight below him (no horizontal gap) keeps his facing as the "far side".
                    int pastSide = Math.Sign(interceptPoint.X - NPC.Center.X);
                    if (pastSide == 0)
                    {
                        pastSide = NPC.direction;
                    }

                    Vector2 aimPoint = interceptPoint + new Vector2(pastSide * SkywardAimPastTarget, 0f);
                    Vector2 aimDirection = (aimPoint - NPC.Center).SafeNormalize(new Vector2(NPC.direction, 0f));

                    // AngleLerp takes the short way round, so the blade swings onto the aim instead of snapping.
                    float aimRotation = BladeRotationToward(aimDirection);
                    _aerialRotation = _aerialRotation.AngleLerp(aimRotation, 0.35f);

                    if (!Main.dedServ)
                    {
                        // Motes converge on the tip from a ring that tightens 60 -> 20px over the aim.
                        Vector2 bladeTip = PuppetWeaponTipPosition(ArtoriasSwordArcRadius);
                        float ringRadius = MathHelper.Lerp(60f, 20f, _aerialStageTicks / (float)SkywardAimTicks);
                        Vector2 moteStart = bladeTip + Main.rand.NextVector2CircularEdge(ringRadius, ringRadius);
                        Vector2 moteVelocity = (bladeTip - moteStart) * 0.12f;
                        Dust mote = Dust.NewDustPerfect(moteStart, DustID.ShadowbeamStaff, moteVelocity, 100, SlashMid, 1f);
                        mote.noGravity = true;

                        // The path line: 8 motes from his centre to the aim point every 4t across the last 20t.
                        int aimTicksLeft = SkywardAimTicks - _aerialStageTicks;
                        bool drawPathLine = aimTicksLeft <= SkywardAimLineTicks && _aerialStageTicks % 4 == 0;
                        if (drawPathLine)
                        {
                            float pathLength = Vector2.Distance(NPC.Center, aimPoint);
                            for (int i = 1; i <= 8; i++)
                            {
                                Vector2 linePoint = NPC.Center + aimDirection * (pathLength * i / 8f);
                                Dust lineDust = Dust.NewDustPerfect(linePoint, DustID.PurpleTorch, Vector2.Zero, 100,
                                    SlashCore, 0.9f);
                                lineDust.noGravity = true;
                            }
                        }
                    }

                    if (_aerialStageTicks >= SkywardAimTicks)
                    {
                        // The ramp (speed grows with t² over 5t) covers a third of full-speed distance, so it adds 2/3 of
                        // its ticks to the flight; then the 8t overshoot so an on-time dash always reaches the aim point.
                        float pathTicks = Vector2.Distance(NPC.Center, aimPoint) / SkywardDashSpeed;
                        float rampLossTicks = SkywardDashAccelTicks * 2f / 3f;
                        int flightTicks = (int)Math.Ceiling(pathTicks + rampLossTicks) + SkywardOvershootTicks;
                        _skywardDashTicks = Math.Min(flightTicks, SkywardDashMaxTicks);
                        _skywardDirection = aimDirection;
                        _aerialRotation = aimRotation;
                        _aerialBladeHit = false;
                        TryMeleeHit(reach: ArtoriasSwordArcRadius);
                        EnterAerialStage(AerialStage.SkywardDash);
                        PlayAerialStageStartCue(AerialStage.SkywardDash);
                    }
                    break;
                }

                case AerialStage.SkywardDash:
                {
                    // Straight-line lunge with gravity off. SmartFighter4 hands gravity back every tick, so re-assert it.
                    NPC.noGravity = true;
                    float rampProgress = Math.Min(1f, _aerialStageTicks / (float)SkywardDashAccelTicks);
                    float dashSpeed = SkywardDashSpeed * rampProgress * rampProgress;
                    NPC.velocity = _skywardDirection * dashSpeed;
                    if (_skywardDirection.X != 0f)
                    {
                        NPC.direction = Math.Sign(_skywardDirection.X);
                        NPC.spriteDirection = NPC.direction;
                    }

                    _aerialRotation = BladeRotationToward(_skywardDirection);
                    TickBladeHit();

                    if (!Main.dedServ)
                    {
                        Vector2 trailPoint = NPC.Center + Main.rand.NextVector2Circular(12f, 12f);
                        Dust trail = Dust.NewDustPerfect(trailPoint, DustID.PurpleTorch, -NPC.velocity * 0.2f, 100,
                            SlashMid, Main.rand.NextFloat(1f, 1.4f));
                        trail.noGravity = true;
                    }

                    // collideX/Y come from last tick's physics step; skip the first ticks, when the feet still touch the floor.
                    bool blocked = _aerialStageTicks > 2 && (NPC.collideX || NPC.collideY);
                    bool dashOver = _aerialStageTicks >= _skywardDashTicks;
                    if (_aerialBladeHit || blocked || dashOver)
                    {
                        NPC.noGravity = false;
                        NPC.velocity *= SkywardMissMomentum;
                        NPC.netUpdate = true;
                        // The double-jump roll is the server's; a client gets the result with this stage's snapshot.
                        // _aerialBladeHit is server-only too, so a client ends the dash on a hit only by adopting the stage.
                        if (Main.netMode != NetmodeID.MultiplayerClient)
                        {
                            bool missed = !_aerialBladeHit;
                            _aerialDoubleJumpPending = missed && Main.rand.Next(100) < SkywardDoubleJumpChance;
                        }
                        EnterAerialStage(AerialStage.SkywardFall);
                    }
                    break;
                }

                case AerialStage.SkywardFall:
                {
                    // A normal fall from wherever the dash ended. A passed double-jump roll fires as the fall begins.
                    _aerialRotation = MathHelper.Lerp(_aerialRotation, AirOverhandStartRotation, 0.12f);
                    bool falling = NPC.velocity.Y > 0f;

                    if (grounded && _aerialStageTicks > 1)
                    {
                        BeginAerialLanding();
                    }
                    else if (_aerialDoubleJumpPending && falling)
                    {
                        // Ballistic arc at the target: rise to its height + 60px (vy <= 11), forward speed = dx / rise
                        // time (<= 8). Past the apex the arc keeps carrying him toward where the target was.
                        float arcRise = Math.Max(0f, NPC.Center.Y - target.Center.Y) + SkywardArcApexMargin;
                        float arcLaunchSpeed = Math.Min((float)Math.Sqrt(2f * AerialGravity * arcRise), SkywardDoubleJumpMaxRise);
                        float arcRiseTicks = arcLaunchSpeed / AerialGravity;
                        float arcVelocityX = MathHelper.Clamp((target.Center.X - NPC.Center.X) / arcRiseTicks,
                            -SkywardDoubleJumpMaxForward, SkywardDoubleJumpMaxForward);
                        NPC.velocity = new Vector2(arcVelocityX, -arcLaunchSpeed);
                        NPC.netUpdate = true;
                        _aerialDoubleJumpPending = false;
                        EnterAerialStage(AerialStage.SkywardArc);
                        PlayAerialStageStartCue(AerialStage.SkywardArc);
                    }
                    break;
                }

                case AerialStage.SkywardArc:
                {
                    // Carry the pose of the swing the target's height currently calls for, so the cut starts from where
                    // the blade already is: above his centre -> low underhand wind-up, level or below -> raised overhand.
                    FaceTarget(target);
                    bool targetAbove = target.Center.Y < NPC.Center.Y;
                    float carryPose = AirOverhandStartRotation;
                    float strikeEnd = AirOverhandEndRotation;
                    WeightedSwing strikeCurve = AirOverhandCurve;
                    if (targetAbove)
                    {
                        carryPose = AirUnderhandStartRotation;
                        strikeEnd = AirUnderhandEndRotation;
                        strikeCurve = AirUnderhandCurve;
                    }

                    _aerialRotation = MathHelper.Lerp(_aerialRotation, carryPose, 0.2f);

                    float targetDistance = NPC.Distance(target.Center);
                    if (targetDistance <= AerialStrikeRange)
                    {
                        BeginAerialStrike(strikeEnd, strikeCurve);
                    }
                    else if (grounded && _aerialStageTicks > 2)
                    {
                        BeginAerialLanding();
                    }
                    break;
                }

                case AerialStage.Strike:
                {
                    // Weighted cut from the carried pose; the body stays ballistic. Live until blade speed < 30% of peak.
                    int elapsedStrikeTicks = _aerialStageTicks - 1;
                    _aerialRotation = _strikeCurve.Apply(_strikeStartRotation, _strikeEndRotation, elapsedStrikeTicks);
                    if (elapsedStrikeTicks <= _strikeCurve.LiveTicks)
                    {
                        TickBladeHit();
                    }

                    if (grounded)
                    {
                        NPC.velocity.X *= 0.8f;
                    }

                    bool strikeDone = elapsedStrikeTicks >= _strikeCurve.TotalTicks;
                    if (strikeDone && grounded)
                    {
                        BeginAerialLanding();
                    }
                    else if (strikeDone)
                    {
                        EnterAerialStage(AerialStage.Falling);
                    }
                    break;
                }

                case AerialStage.Falling:
                {
                    // Harmless descent holding the finished pose, then the planted landing beat.
                    if (grounded && _aerialStageTicks > 1)
                    {
                        BeginAerialLanding();
                    }
                    break;
                }

                case AerialStage.Landing:
                {
                    // The punish window: planted, blade held at its follow-through. PhaseTimer = 1 lets the base Custom
                    // case end the phase into Idle this same tick.
                    NPC.velocity.X *= 0.8f;
                    if (_aerialStageTicks >= _aerialLandingTicks)
                    {
                        EnterAerialStage(AerialStage.None);
                        DebugAttackLabel = null;
                        PhaseTimer = 1;
                    }
                    break;
                }
            }
        }

        /// <summary>Starts a Weighted aerial cut from the blade's current pose to <paramref name="endRotation"/>: arms the
        /// blade and tracks one crescent that fades with the hit window.</summary>
        void BeginAerialStrike(float endRotation, WeightedSwing curve)
        {
            _strikeStartRotation = _aerialRotation;
            _strikeEndRotation = endRotation;
            _strikeCurve = curve;
            _aerialBladeHit = false;
            EnterAerialStage(AerialStage.Strike);
            PlayAerialStageStartCue(AerialStage.Strike);
            TryMeleeHit(reach: ArtoriasSwordArcRadius);

            // A rising cut travels against the overhead direction, so its crescent trails the other way.
            bool risingCut = endRotation < _strikeStartRotation;
            SpawnArtoriasSwordArc(curve.TotalTicks, risingCut, curve.HitWindowEnd);
        }

        void BeginAerialLanding()
        {
            EnterAerialStage(AerialStage.Landing);
            _aerialLandingTicks = UppercutLandingTicks;
            if (_lastAerialWasLunge)
            {
                _aerialLandingTicks = SkywardLandingTicks;
            }
        }

        /// <summary>Every aerial stage change goes through here: restarts the stage clock, bumps _aerialStageSequence (clients
        /// skip snapshots older than a change they already made) and, on the server, flushes a snapshot so clients follow
        /// within network latency instead of waiting on the netSpam throttle.</summary>
        void EnterAerialStage(AerialStage stage)
        {
            _aerialStage = stage;
            _aerialStageTicks = 0;
            _aerialStageSequence++;
            RequestNetworkSnapshot();
        }

        /// <summary>Label, sound and dust for a stage's start. Called where the stage begins and when a client adopts it from a
        /// snapshot, since the transition code that would have played it only ran on the server.</summary>
        void PlayAerialStageStartCue(AerialStage stage)
        {
            switch (stage)
            {
                case AerialStage.UppercutRun:
                    DebugAttackLabel = "Rising Uppercut";
                    break;

                case AerialStage.UppercutRise:
                    SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.8f, Pitch = -0.35f }, NPC.Center);
                    break;

                case AerialStage.SkywardJump:
                    DebugAttackLabel = "Skyward Lunge";
                    SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.8f, Pitch = -0.35f }, NPC.Center);
                    break;

                case AerialStage.SkywardAim:
                    SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.55f, Pitch = -0.4f }, NPC.Center);
                    break;

                case AerialStage.SkywardDash:
                    SoundEngine.PlaySound(SoundID.Item74 with { Volume = 0.6f, Pitch = 0.2f }, NPC.Center);
                    break;

                case AerialStage.SkywardArc:
                    SoundEngine.PlaySound(SoundID.DoubleJump, NPC.Center);
                    if (!Main.dedServ)
                    {
                        // Cloud ring puffed out flat from the feet, like a Cloud in a Bottle jump.
                        for (int i = 0; i < 20; i++)
                        {
                            float cloudAngle = MathHelper.TwoPi * i / 20f;
                            Vector2 cloudVelocity = new Vector2((float)Math.Cos(cloudAngle) * 3f,
                                (float)Math.Sin(cloudAngle) + 1.5f);
                            Dust cloud = Dust.NewDustPerfect(NPC.Bottom, DustID.Cloud, cloudVelocity, 100, default,
                                Main.rand.NextFloat(1.2f, 1.6f));
                            cloud.noGravity = true;
                        }
                    }
                    break;

                case AerialStage.Strike:
                    SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.75f, PitchVariance = 0.15f }, NPC.Center);
                    break;

                case AerialStage.None:
                    DebugAttackLabel = null;
                    break;
            }
        }

        void FaceTarget(Player target)
        {
            int facing = 1;
            if (target.Center.X < NPC.Center.X)
            {
                facing = -1;
            }

            NPC.direction = facing;
            NPC.spriteDirection = facing;
        }

        /// <summary>Swing-space rotation that draws the blade along <paramref name="worldDirection"/> at the current facing.
        /// Inverse of PuppetNPC.GetWeaponWorldDirection's mirrored path (no blade flip): facing right the drawn angle is
        /// rest-offset (-45°) + rotation + draw offset; facing left it is -(180° - rest) - rotation - draw offset.</summary>
        float BladeRotationToward(Vector2 worldDirection)
        {
            float worldAngle = worldDirection.ToRotation();
            float drawOffset = FrontHandWeapon.RotationOffset;
            float restRadians = MathHelper.ToRadians(MeleeNaturalRestAngleDeg);
            if (NPC.direction == 1)
            {
                return MathHelper.WrapAngle(worldAngle + restRadians) - drawOffset;
            }

            float mirroredRestRadians = MathHelper.Pi - restRadians;
            return -MathHelper.WrapAngle(worldAngle + mirroredRestRadians) - drawOffset;
        }

        // ── Charge-up Nova: one-shot set-piece at 50% / 20% / 10% HP ────────────────
        // Assumed reading of the brief: the three trigger thresholds are 50/20/10% HP, and blast
        // sizes escalate 500/600/700px in that same order (the "final AOE" is the 10% one).
        static readonly (float hpFrac, float radius, int damage)[] NovaStages =
        {
            (0.50f, 500f, 200),
            (0.20f, 600f, 250),
            (0.10f, 700f, 300),
        };
        readonly bool[] _novaStageDone = new bool[NovaStages.Length];
        int _novaStageIndex = -1;

        // Space-distortion telegraph (reuses the TriadShockwave refraction shader the boss-despawn
        // shockwave uses - see UsefulFunctions.DespawnFlash). Grows linearly from the boss out to the
        // FINAL blast radius, arriving NovaDistortionFullRadiusLeadTicks before the blast, then holds
        // there - so for the last second its boundary is an honest, stationary preview of the edge.
        const string NovaDistortionFilter = "tsorcRevamp:ArtoriasNovaDistortion";
        const int NovaDistortionFullRadiusLeadTicks = 60;
        const float NovaDistortionMaxOpacity = 0.55f;
        // The charge fireball's counter-rotating second layer: 0.5 rad/s (~12.5s a turn), at 45% of the main
        // layer's opacity so it reads as a sheer veil turning over it rather than a second fireball.
        const float NovaCounterSpinPerSecond = 0.5f;
        const float NovaCounterLayerOpacity = 0.45f;

        protected override bool CanNova => true;
        protected override int NovaChargeTicks => 4 * 60;
        protected override int NovaBlastHoldTicks => 24;
        protected override int NovaRecoveryTicks => 90;

        protected override bool ShouldTriggerNova()
        {
            float hp = (float)NPC.life / NPC.lifeMax;
            for (int i = 0; i < NovaStages.Length; i++)
            {
                if (!_novaStageDone[i] && hp <= NovaStages[i].hpFrac)
                {
                    _novaStageDone[i] = true;
                    _novaStageIndex = i;
                    return true;
                }
            }
            return false;
        }

        protected override void DoNovaChargeTick(int elapsed, int total)
        {
            if (Main.dedServ)
            {
                return;
            }

            // Wraith build-up, 4.0s to match NovaChargeTicks; it hard-stops where the blast sound takes over. Played ahead
            // of the _novaStageIndex check because that index is picked on the server and never synced to clients.
            if (elapsed == 0)
            {
                SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Custom/Artorias/Artorias_NovaCharge") with { Volume = 0.7f },
                    NPC.Center);
            }

            if (_novaStageIndex < 0)
            {
                return;
            }

            float innerT = elapsed / (float)total;                     // 0 -> 1 over the full charge
            float radius = NovaStages[_novaStageIndex].radius;

            if (Main.netMode != NetmodeID.Server)
            {
                if (!Filters.Scene[NovaDistortionFilter].IsActive())
                {
                    Filters.Scene.Activate(NovaDistortionFilter, NPC.Center);
                }
                // 0 -> 1 over the charge minus the lead, then pinned at 1 (full radius) until the blast.
                int growTicks = Math.Max(1, total - NovaDistortionFullRadiusLeadTicks);
                float growT = MathHelper.Clamp(elapsed / (float)growTicks, 0f, 1f);
                float ringRadius = radius * growT;
                float ringOpacity = MathHelper.Lerp(0f, NovaDistortionMaxOpacity, growT);

                // TriadShockwave.fx puts its ring at uProgress * uScreenResolution.x / 2, and ScreenShaderData passes
                // uScreenResolution as the VISIBLE WORLD size (screen / zoom). So a ring of ringRadius world px needs
                // uProgress = 2 * ringRadius * zoom / screenWidth. A fixed radius/800 only matched a 1600px-wide
                // screen at zoom 1, and drew the ring 1.2x the blast at 1920 and 1.6x at 2560.
                float zoom = Main.GameViewMatrix.Zoom.X;
                float ringProgress = 2f * ringRadius * zoom / Main.screenWidth;
                Filters.Scene[NovaDistortionFilter].GetShader().UseTargetPosition(NPC.Center)
                    .UseProgress(ringProgress).UseOpacity(ringOpacity).UseIntensity(1f);
            }

            // Sparse physical motes complement the shader without obscuring its exact disc.
            int count = elapsed % 5 == 0 ? 2 : 0;
            for (int i = 0; i < count; i++)
            {
                float r = MathHelper.Lerp(20f, radius * 0.24f, innerT);
                Vector2 pos = NPC.Center + Main.rand.NextVector2Circular(r, r);
                Color tint = Main.rand.NextBool(3) ? (Main.rand.NextBool() ? Color.Black : Color.White) : default;
                Dust d = Dust.NewDustPerfect(pos, DustID.PurpleTorch, Vector2.Zero, 60, tint, Main.rand.NextFloat(1.3f, 2f));
                d.noGravity = true;
            }

        }

        protected override void DoNovaBlast()
        {
            // Wraith release. Ahead of the _novaStageIndex check for the same reason as the charge cue: clients never
            // receive the index. Its six baked-in pulses line up with ArtoriasChargeNova's 9-tick burst train.
            if (!Main.dedServ)
            {
                SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/Custom/Artorias/Artorias_NovaBlast_release") with { Volume = 0.7f },
                    NPC.Center);
            }

            if (_novaStageIndex < 0)
            {
                return;
            }

            var (_, radius, damage) = NovaStages[_novaStageIndex];

            if (Main.netMode != NetmodeID.Server && Filters.Scene[NovaDistortionFilter].IsActive())
            {
                Filters.Scene[NovaDistortionFilter].Deactivate();
            }

            UsefulFunctions.ScreenShake(NPC.Center, strength: 10f, frames: 20);

            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero,
                ModContent.ProjectileType<ArtoriasChargeNova>(), EnemyDamage.Projectile(damage), 6f, Main.myPlayer, radius);
        }

        // ── Abyss Shard: ground-spike combos, unlocked permanently at 60% HP ────────
        // Each individual shard (spawned via SpawnShardAt) carries its own 40-tick ground telegraph
        // and pop - the boss side only decides WHERE and WHEN to spawn one. No recovery phase per the
        // brief: the moment a sequence ends, control returns immediately so another attack can follow.
        enum AbyssShardVariant { Burst3, Domino6, DominoPulse4x2, Escalating357 }
        bool _abyssShardUnlocked;
        AbyssShardVariant _abyssShardVariant;
        int _abyssShardDominoDir;
        Vector2 _abyssShardAnchor;

        const int AbyssShardDamage = 170;
        const int AbyssShardDominoGapTicks = 10;
        const int AbyssShardWaveGapTicks = 60;
        const float AbyssShardSpacing = 48f;

        protected override bool CanAbyssShard => _abyssShardUnlocked;
        protected override float AbyssShardMinRange => 80f;
        protected override float AbyssShardMaxRange => 900f;
        protected override int AbyssShardChance => 12;
        protected override int AbyssShardCooldownAfterUse => 420;
        protected override int AbyssShardTelegraphTicks => 30;

        protected override void DoAbyssShardFire(int fireIndex)
        {
            Player target = Main.player[NPC.target];

            if (fireIndex == 0)
            {
                _abyssShardVariant = (AbyssShardVariant)Main.rand.Next(4);
                _abyssShardDominoDir = Main.rand.NextBool() ? -1 : 1;
                _abyssShardAnchor = target.Center;
            }

            switch (_abyssShardVariant)
            {
                // 3 shards centered on the player, 4 tiles apart, all at once.
                case AbyssShardVariant.Burst3:
                    if (fireIndex == 0)
                        SpawnShardCluster(target.Center, 3, AbyssShardSpacing);
                    break;

                // 6 shards marching left-or-right from the player, one every 10 ticks.
                case AbyssShardVariant.Domino6:
                    SpawnShardAt(_abyssShardAnchor + new Vector2(_abyssShardDominoDir * AbyssShardSpacing * fireIndex, 0f));
                    break;

                // 4 marching one way from a fixed point, then 4 more marching back the other way.
                case AbyssShardVariant.DominoPulse4x2:
                    if (fireIndex < 4)
                        SpawnShardAt(_abyssShardAnchor + new Vector2(AbyssShardSpacing * fireIndex, 0f));
                    else
                        SpawnShardAt(_abyssShardAnchor - new Vector2(AbyssShardSpacing * (fireIndex - 4), 0f));
                    break;

                // Escalating spaced volleys: 3, then 5, then 7 - each re-centered on the player.
                case AbyssShardVariant.Escalating357:
                    int count = fireIndex switch { 0 => 3, 1 => 5, _ => 7 };
                    SpawnShardCluster(target.Center, count, AbyssShardSpacing);
                    break;
            }
        }

        protected override int NextAbyssShardDelay(int completedFireIndex)
        {
            switch (_abyssShardVariant)
            {
                case AbyssShardVariant.Burst3:
                    return -1; // single simultaneous burst

                case AbyssShardVariant.Domino6:
                    return completedFireIndex < 5 ? AbyssShardDominoGapTicks : -1;

                case AbyssShardVariant.DominoPulse4x2:
                    return completedFireIndex < 7 ? AbyssShardDominoGapTicks : -1;

                case AbyssShardVariant.Escalating357:
                    return completedFireIndex < 2 ? AbyssShardWaveGapTicks : -1;

                default:
                    return -1;
            }
        }

        void SpawnShardCluster(Vector2 center, int count, float spacing)
        {
            float startOffset = -(count - 1) / 2f * spacing;
            for (int i = 0; i < count; i++)
            {
                SpawnShardAt(center + new Vector2(startOffset + i * spacing, 0f));
            }
        }

        void SpawnShardAt(Vector2 worldPos)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }
            Projectile.NewProjectile(NPC.GetSource_FromThis(), worldPos, Vector2.Zero,
                ModContent.ProjectileType<AbyssShard>(), EnemyDamage.Projectile(AbyssShardDamage), 0f, Main.myPlayer);
        }

        // ── Homing Volley: dodgeback + overhead chop that fires one of 3 delayed-homing patterns ──
        enum HomingVolleyVariant { StaggeredFan, PincerSplit, LatticeSnap }
        HomingVolleyVariant _homingVolleyVariant;

        const int HomingVolleyOrbDamage = 150;
        const float HomingVolleyOrbSpeed = 8.5f;
        const int HomingVolleyCurveTicks = 16;

        protected override bool CanHomingVolley => true;
        protected override float HomingVolleyMinRange => 280f;
        protected override float HomingVolleyMaxRange => 650f;
        protected override int HomingVolleyChance => 10;
        protected override int HomingVolleyCooldownAfterUse => 300;
        // Volley chop: -100° -> 70°, in 11 / out 24, k 6 -> 22°/t, the heaviest-looking launch (was Smooth 30t,
        // ~8.5°/t). The orbs leave on the peak tick (11 of 35) instead of mid-arc, where a Weighted blade is already
        // settling. Half a tick under the peak so float rounding can't push the release a tick late. Tail +5t comes
        // out of the recovery (120 -> 115).
        protected override WeightedSwing HomingVolleySwingCurve => new WeightedSwing(11, 24, 6f);
        protected override int HomingVolleySwingTicks => HomingVolleySwingCurve.TotalTicks;
        protected override float HomingVolleyFireProgress =>
            (HomingVolleySwingCurve.EaseInTicks - 0.5f) / HomingVolleySwingCurve.TotalTicks;
        protected override int HomingVolleyRecoveryTicks => 115;

        protected override void DoHomingVolleySwingTick(int elapsed, int total)
        {
            if (elapsed == 0)
            {
                SpawnArtoriasSwordArc(total);
            }

            if (Main.dedServ)
            {
                return;
            }

            // Same overhead-chop angle range and Weighted curve the rotation sync uses, recomputed here purely
            // for the dust position - the sword itself is driven independently in PuppetNPC.cs.
            float angle = HomingVolleySwingCurve.Apply(MathHelper.ToRadians(-100f), MathHelper.ToRadians(70f), elapsed);
            Vector2 dir = new Vector2(NPC.direction, 0f).RotatedBy(angle);
            Vector2 bladePos = NPC.Center + dir * 46f;

            if (Main.rand.NextBool(2))
            {
                Color tint = Main.rand.NextBool() ? new Color(190, 90, 255) : new Color(255, 140, 210);
                Dust d = Dust.NewDustPerfect(bladePos + Main.rand.NextVector2Circular(6f, 6f), DustID.PurpleTorch,
                    Vector2.Zero, 100, tint, Main.rand.NextFloat(1f, 1.6f));
                d.noGravity = true;
            }
        }

        protected override void DoHomingVolleyFire()
        {
            _homingVolleyVariant = (HomingVolleyVariant)Main.rand.Next(3);

            SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.65f, Pitch = -0.1f }, NPC.Center);

            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            Player target = Main.player[NPC.target];
            Vector2 origin = PuppetWeaponTipPosition(54f);

            switch (_homingVolleyVariant)
            {
                case HomingVolleyVariant.StaggeredFan:
                    FireStaggeredFan(origin, target);
                    break;
                case HomingVolleyVariant.PincerSplit:
                    FirePincerSplit(origin, target);
                    break;
                case HomingVolleyVariant.LatticeSnap:
                    FireLatticeSnap(origin, target);
                    break;
            }
        }

        // 5 orbs in a tight forward spread, each with its OWN curve-start delay (staggered 8 ticks
        // apart) so they bend onto the player one after another instead of all at once.
        void FireStaggeredFan(Vector2 origin, Player target)
        {
            float baseAngle = (target.Center - origin).ToRotation();
            float[] spreadDeg = { -16f, -8f, 0f, 8f, 16f };
            for (int i = 0; i < spreadDeg.Length; i++)
            {
                float angle = baseAngle + MathHelper.ToRadians(spreadDeg[i]);
                Vector2 vel = angle.ToRotationVector2() * HomingVolleyOrbSpeed;
                int straightTicks = 18 + i * 8;
                SpawnHomingOrb(origin, vel, straightTicks);
            }
        }

        // 2 orbs launched wide of the player on diverging paths - they look like a clean miss on
        // both sides, then curve inward at the SAME moment, converging from opposite sides.
        void FirePincerSplit(Vector2 origin, Player target)
        {
            float baseAngle = (target.Center - origin).ToRotation();
            float[] spreadDeg = { -30f, 30f };
            foreach (float deg in spreadDeg)
            {
                float angle = baseAngle + MathHelper.ToRadians(deg);
                Vector2 vel = angle.ToRotationVector2() * HomingVolleyOrbSpeed;
                SpawnHomingOrb(origin, vel, 32);
            }
        }

        // 6 orbs launched in a parallel wall (same heading, offset perpendicular to it) so they
        // travel straight in formation, then ALL curve at once toward wherever the player then is -
        // the wall "snaps" onto the player's position rather than converging gradually.
        void FireLatticeSnap(Vector2 origin, Player target)
        {
            float baseAngle = (target.Center - origin).ToRotation();
            Vector2 aimDir = baseAngle.ToRotationVector2();
            Vector2 perp = aimDir.RotatedBy(MathHelper.PiOver2);
            Vector2 vel = aimDir * HomingVolleyOrbSpeed;

            const int count = 6;
            const float spacing = 40f;
            float startOffset = -(count - 1) / 2f * spacing;
            for (int i = 0; i < count; i++)
            {
                Vector2 spawnPos = origin + perp * (startOffset + i * spacing);
                SpawnHomingOrb(spawnPos, vel, 34);
            }
        }

        void SpawnHomingOrb(Vector2 position, Vector2 velocity, int straightTicks)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }
            Projectile.NewProjectile(NPC.GetSource_FromThis(), position, velocity,
                ModContent.ProjectileType<HomingAbyssOrb>(), EnemyDamage.Projectile(HomingVolleyOrbDamage), 0f,
                Main.myPlayer, straightTicks, HomingVolleyCurveTicks);
        }

        // ── Boomerang Crescent: 2 variants, both using the shared overhead-chop launch ──────────
        const int BoomerangDamage = 120;
        const float BoomerangSpeed = 7f;

        protected override bool CanBoomerang => true;
        protected override float BoomerangMinRange => 60f;
        protected override float BoomerangMaxRange => 1300f;
        protected override int BoomerangChance => 9;
        protected override int BoomerangCooldownAfterUse => 330;
        // Chop: -100° -> 70°, in 6 / out 20, k 5 -> 28.5°/t, a quick throw (was Smooth 30t). The crescents fire on
        // the peak tick (6 of 26; half a tick under so float rounding can't push it late). The recovery still
        // expires 110 ticks after firing (20t settle + 90t), preserving the old budget while the independently
        // returning projectile overlaps Artorias's next move.
        protected override WeightedSwing BoomerangSwingCurve => new WeightedSwing(6, 20, 5f);
        protected override int BoomerangSwingTicks => BoomerangSwingCurve.TotalTicks;
        protected override float BoomerangFireProgress =>
            (BoomerangSwingCurve.EaseInTicks - 0.5f) / BoomerangSwingCurve.TotalTicks;
        protected override int BoomerangRecoveryTicks => 90;

        protected override void DoBoomerangSwingTick(int elapsed, int total)
        {
            if (elapsed == 0)
            {
                SpawnArtoriasSwordArc(total);
            }

            if (Main.dedServ)
            {
                return;
            }

            float angle = BoomerangSwingCurve.Apply(MathHelper.ToRadians(-100f), MathHelper.ToRadians(70f), elapsed);
            Vector2 dir = new Vector2(NPC.direction, 0f).RotatedBy(angle);
            Vector2 bladePos = NPC.Center + dir * 46f;

            if (Main.rand.NextBool(2))
            {
                Color tint = Main.rand.NextBool() ? new Color(190, 90, 255) : new Color(255, 140, 210);
                Dust d = Dust.NewDustPerfect(bladePos + Main.rand.NextVector2Circular(6f, 6f), DustID.PurpleTorch,
                    Vector2.Zero, 100, tint, Main.rand.NextFloat(1f, 1.6f));
                d.noGravity = true;
            }
        }

        protected override void DoBoomerangFire()
        {
            SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.65f, Pitch = -0.15f }, NPC.Center);

            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            Player target = Main.player[NPC.target];
            Vector2 origin = PuppetWeaponTipPosition(54f);
            float baseAngle = (target.Center - origin).ToRotation();

            if (Main.rand.NextBool())
            {
                // Mirrored Twin Loops: launched wide of the player on both sides, curling INWARD
                // across each other, then both homing back to the caster - a converging double
                // return that crosses near the player on the way back.
                float leftAngle = baseAngle - MathHelper.ToRadians(35f);
                float rightAngle = baseAngle + MathHelper.ToRadians(35f);
                SpawnBoomerang(origin, leftAngle.ToRotationVector2() * BoomerangSpeed, 1f);
                SpawnBoomerang(origin, rightAngle.ToRotationVector2() * BoomerangSpeed, -1f);
            }
            else
            {
                // Wide Solo Loop: one big crescent, curl direction random, sweeping a wide arc
                // across and past the player before returning.
                float sign = Main.rand.NextBool() ? 1f : -1f;
                float launchAngle = baseAngle - sign * MathHelper.ToRadians(45f);
                SpawnBoomerang(origin, launchAngle.ToRotationVector2() * BoomerangSpeed, sign);
            }
        }

        void SpawnBoomerang(Vector2 position, Vector2 velocity, float curveDir)
        {
            Projectile.NewProjectile(NPC.GetSource_FromThis(), position, velocity,
                ModContent.ProjectileType<BoomerangCrescent>(), EnemyDamage.Projectile(BoomerangDamage), 0f,
                Main.myPlayer, curveDir, NPC.whoAmI);
            Projectile.NewProjectile(NPC.GetSource_FromThis(), position, Vector2.Zero,
                ModContent.ProjectileType<ArtoriasFanSourceVFX>(), 0, 0f,
                Main.myPlayer, velocity.ToRotation(), curveDir);
        }

        // ── Spiral Fan: 3 variants, a rotating-angle burst reusing the AbyssSlash crescent ───────
        enum SpiralFanVariant { SingleStream, DoubleCounterSpiral, FullRotationSweep }
        SpiralFanVariant _spiralFanVariant;
        float _spiralFanBaseAngle;
        float _spiralFanDir;

        const int SpiralFanShotDamage = 110;
        const float SpiralFanShotSpeed = 8.5f;
        // Telegraph dust distance from Artorias's centre, doubled from the old 46px blade-hugging arc
        // (read as too small for a volley that fills the screen). Jitter doubled with it (6 -> 12).
        const float SpiralFanTelegraphDustRadius = 92f;
        const float SpiralFanTelegraphDustJitter = 12f;

        protected override bool CanSpiralFan => true;
        protected override float SpiralFanMinRange => 60f;
        protected override float SpiralFanMaxRange => 650f;
        protected override int SpiralFanChance => 8;
        protected override int SpiralFanCooldownAfterUse => 360;
        // Wind-up chop before the burst: -100° -> 70°, in 12 / out 18, k 4.5 -> 21°/t with the softest settle in his
        // kit, easing into the pose the burst holds. Kept at 30t so the burst starts exactly when it always did.
        protected override WeightedSwing SpiralFanSwingCurve => new WeightedSwing(12, 18, 4.5f);
        protected override int SpiralFanSwingTicks => SpiralFanSwingCurve.TotalTicks;

        protected override void DoSpiralFanSwingTick(int elapsed, int total)
        {
            if (elapsed == 0)
            {
                SpawnArtoriasSwordArc(total);
            }

            if (Main.dedServ)
            {
                return;
            }

            float angle = SpiralFanSwingCurve.Apply(MathHelper.ToRadians(-100f), MathHelper.ToRadians(70f), elapsed);
            Vector2 dir = new Vector2(NPC.direction, 0f).RotatedBy(angle);
            Vector2 arcPosition = NPC.Center + dir * SpiralFanTelegraphDustRadius;

            // One dust per tick (was 1 in 2): at double the radius the arc is twice as long, so the
            // old rate would have left it half as dense.
            Color tint = new Color(190, 90, 255);
            if (Main.rand.NextBool())
            {
                tint = new Color(255, 140, 210);
            }

            Vector2 jitter = Main.rand.NextVector2Circular(SpiralFanTelegraphDustJitter, SpiralFanTelegraphDustJitter);
            Dust dust = Dust.NewDustPerfect(arcPosition + jitter, DustID.PurpleTorch,
                Vector2.Zero, 100, tint, Main.rand.NextFloat(1f, 1.6f));
            dust.noGravity = true;
        }

        protected override void DoSpiralFanFire(int shotIndex)
        {
            Vector2 origin = NPC.Center + new Vector2(NPC.direction * 30f, -20f);

            if (shotIndex == 0)
            {
                _spiralFanVariant = (SpiralFanVariant)Main.rand.Next(3);
                _spiralFanDir = Main.rand.NextBool() ? 1f : -1f;
                Player target = Main.player[NPC.target];
                _spiralFanBaseAngle = (target.Center - origin).ToRotation();
            }

            SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.5f, Pitch = 0.1f, PitchVariance = 0.1f }, NPC.Center);

            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            switch (_spiralFanVariant)
            {
                case SpiralFanVariant.SingleStream:
                {
                    float angleStep = MathHelper.ToRadians(18f);
                    float angle = _spiralFanBaseAngle + shotIndex * angleStep * _spiralFanDir;
                    FireSpiralShot(origin, angle);
                    break;
                }
                case SpiralFanVariant.DoubleCounterSpiral:
                {
                    float angleStep = MathHelper.ToRadians(16f);
                    float angleA = _spiralFanBaseAngle + shotIndex * angleStep * _spiralFanDir;
                    float angleB = _spiralFanBaseAngle - shotIndex * angleStep * _spiralFanDir;
                    FireSpiralShot(origin, angleA);
                    FireSpiralShot(origin, angleB);
                    break;
                }
                case SpiralFanVariant.FullRotationSweep:
                {
                    // 18 steps of 20° = a full 360° rotation, so this variant also threatens
                    // whoever's beside/behind the invader, not just the forward arc.
                    float angleStep = MathHelper.ToRadians(20f);
                    float angle = _spiralFanBaseAngle + shotIndex * angleStep * _spiralFanDir;
                    FireSpiralShot(origin, angle);
                    break;
                }
            }
        }

        protected override int NextSpiralFanDelay(int completedShotIndex)
        {
            switch (_spiralFanVariant)
            {
                case SpiralFanVariant.SingleStream:
                    return completedShotIndex < 9 ? 5 : -1;
                case SpiralFanVariant.DoubleCounterSpiral:
                    return completedShotIndex < 7 ? 5 : -1;
                case SpiralFanVariant.FullRotationSweep:
                    return completedShotIndex < 17 ? 6 : -1;
                default:
                    return -1;
            }
        }

        void FireSpiralShot(Vector2 origin, float angle)
        {
            Vector2 vel = angle.ToRotationVector2() * SpiralFanShotSpeed;
            Projectile.NewProjectile(NPC.GetSource_FromThis(), origin, vel,
                ModContent.ProjectileType<AbyssSlash>(), EnemyDamage.Projectile(SpiralFanShotDamage), 0f,
                Main.myPlayer, 0f, 1f, _spiralFanDir);
            Projectile.NewProjectile(NPC.GetSource_FromThis(), origin, Vector2.Zero,
                ModContent.ProjectileType<ArtoriasFanSourceVFX>(), 0, 0f,
                Main.myPlayer, angle, _spiralFanDir);
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            base.SendExtraAI(writer);
            writer.Write((short)_phantomOwnerIdleTimer);
            if (NPC.HasBuff(ModContent.BuffType<Buffs.DispelShadow>()))
            {
                defenseBroken = true;
            }
            writer.Write(defenseBroken);
            writer.Write(_ringCenter.X);
            writer.Write(_ringCenter.Y);
            writer.Write(_abyssSurgeTimer);
            writer.Write(_currentRingRadius);
            writer.Write((byte)_ringCollapseState);
            writer.Write(_ringCollapseTimer);
            writer.Write(_ringCollapseFrom);
            writer.Write(_ringCollapseTo);
            writer.Write(_ringCollapseDone50);
            writer.Write(_ringCollapseDone30);

            // Aerial attack (Rising Uppercut / Skyward Lunge): the stage machine runs on every machine, started and branched
            // by the server; every stage change flushes a snapshot (EnterAerialStage). _aerialBladeHit and the cooldown stay
            // server-only.
            writer.Write(_aerialStageSequence);
            writer.Write((byte)_aerialStage);
            writer.Write((short)Math.Clamp(_aerialStageTicks, 0, short.MaxValue));
            writer.Write((short)Math.Clamp(_aerialAirTicks, 0, short.MaxValue));
            writer.Write((byte)Math.Clamp(_aerialLandingTicks, 0, byte.MaxValue));
            writer.Write(_aerialRotation);
            writer.Write(_aerialVelocityX);
            writer.Write(_lastAerialWasLunge);
            writer.Write(_aerialDoubleJumpPending);
            writer.WriteVector2(_skywardDirection);
            writer.Write((byte)Math.Clamp(_skywardDashTicks, 0, byte.MaxValue));
            writer.Write(_strikeStartRotation);
            writer.Write(_strikeEndRotation);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            base.ReceiveExtraAI(reader);
            _phantomOwnerIdleTimer = reader.ReadInt16();
            bool receivedBrokenDef = reader.ReadBoolean();
            if (receivedBrokenDef)
            {
                defenseBroken = true;
                NPC.defense = 0;
            }
            _ringCenter = new Vector2(reader.ReadSingle(), reader.ReadSingle());
            _abyssSurgeTimer = reader.ReadInt32();
            _currentRingRadius = reader.ReadSingle();
            _ringCollapseState = (RingCollapseState)reader.ReadByte();
            _ringCollapseTimer = reader.ReadInt32();
            _ringCollapseFrom = reader.ReadSingle();
            _ringCollapseTo = reader.ReadSingle();
            _ringCollapseDone50 = reader.ReadBoolean();
            _ringCollapseDone30 = reader.ReadBoolean();

            int aerialStageSequence = reader.ReadInt32();
            AerialStage aerialStage = (AerialStage)reader.ReadByte();
            int aerialStageTicks = reader.ReadInt16();
            int aerialAirTicks = reader.ReadInt16();
            int aerialLandingTicks = reader.ReadByte();
            float aerialRotation = reader.ReadSingle();
            float aerialVelocityX = reader.ReadSingle();
            bool lastAerialWasLunge = reader.ReadBoolean();
            bool aerialDoubleJumpPending = reader.ReadBoolean();
            Vector2 skywardDirection = reader.ReadVector2();
            int skywardDashTicks = reader.ReadByte();
            float strikeStartRotation = reader.ReadSingle();
            float strikeEndRotation = reader.ReadSingle();

            // Skip a snapshot older than a stage change this client already made on its own, unless this client has dropped
            // out of the attack while the server is still in it.
            bool olderThanPrediction = aerialStageSequence < _aerialStageSequence;
            bool droppedOut = _aerialStage == AerialStage.None && aerialStage != AerialStage.None;
            if (olderThanPrediction && !droppedOut)
            {
                return;
            }

            // A different stage is adopted with the server's clock and its start cue; the same stage keeps this client's clock.
            if (aerialStage != _aerialStage)
            {
                _aerialStage = aerialStage;
                _aerialStageTicks = aerialStageTicks;
                PlayAerialStageStartCue(aerialStage);
            }
            _aerialStageSequence = aerialStageSequence;
            _aerialAirTicks = aerialAirTicks;
            _aerialLandingTicks = aerialLandingTicks;
            _aerialRotation = aerialRotation;
            _aerialVelocityX = aerialVelocityX;
            _lastAerialWasLunge = lastAerialWasLunge;
            _aerialDoubleJumpPending = aerialDoubleJumpPending;
            _skywardDirection = skywardDirection;
            _skywardDashTicks = skywardDashTicks;
            _strikeStartRotation = strikeStartRotation;
            _strikeEndRotation = strikeEndRotation;

            // The strike curve is one of three table constants, identified by the end pose BeginAerialStrike was given.
            _strikeCurve = AirOverhandCurve;
            if (strikeEndRotation == UppercutFinishRotation)
            {
                _strikeCurve = UppercutCurve;
            }
            else if (strikeEndRotation == AirUnderhandEndRotation)
            {
                _strikeCurve = AirUnderhandCurve;
            }
        }

        public override void ModifyHitByItem(Player player, Item item, ref NPC.HitModifiers modifiers)
        {
            //item.type == ModContent.ItemType<Items.Weapons.Melee.Shortswords.BarrowBlade>() doesn't work since Barrow Blade only damages with its projectile now, put that into its projectile below
            if (item.type == ModContent.ItemType<ForgottenGaiaSword>())
            {
                defenseBroken = true;
            }
            if (!defenseBroken)
            {
                if (textCooldown == 0)
                {
                    UsefulFunctions.BroadcastText(LangUtils.GetTextValue("NPCs.Artorias.BarrowBladeHint"));
                    textCooldown = 5;
                }
                else
                {
                    textCooldown--;
                }
                CombatText.NewText(new Rectangle((int)NPC.Center.X, (int)NPC.Bottom.Y, 10, 10), Color.Crimson, LangUtils.GetTextValue("NPCs.Artorias.Immune"), true, false);
                modifiers.SetMaxDamage(1);
            }
        }

        public override void ModifyHitByProjectile(Projectile projectile, ref NPC.HitModifiers modifiers)
        {
            if (projectile.type == ModContent.ProjectileType<BarrowBladeProjectile>())
            {
                defenseBroken = true;
            }
            if (!defenseBroken)
            {
                if (textCooldown == 0)
                {
                    UsefulFunctions.BroadcastText(LangUtils.GetTextValue("NPCs.Artorias.BarrowBladeHint"));
                    textCooldown = 5;
                }
                else
                {
                    textCooldown--;
                }
                CombatText.NewText(new Rectangle((int)NPC.Center.X, (int)NPC.Bottom.Y, 10, 10), Color.Crimson, LangUtils.GetTextValue("NPCs.Artorias.Immune"), true, false);
                modifiers.SetMaxDamage(1);
            }
        }

        public override bool CheckActive()
        {
            return false;
        }

        public override void BossLoot(ref string name, ref int potionType)
        {
            potionType = ItemID.SuperHealingPotion;
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            npcLoot.Add(ItemDropRule.BossBag(ModContent.ItemType<ArtoriasBag>()));
            npcLoot.Add(ItemDropRule.ByCondition(tsorcRevamp.tsorcItemDropRuleConditions.AdventureModeRule, ItemID.LargeAmethyst));
            npcLoot.Add(ItemDropRule.ByCondition(tsorcRevamp.tsorcItemDropRuleConditions.NonExpertFirstKillRule, ModContent.ItemType<GuardianSoul>()));
            IItemDropRule notExpertCondition = new LeadingConditionRule(new Conditions.NotExpert());
            notExpertCondition.OnSuccess(ItemDropRule.Common(ModContent.ItemType<WolfRingItem>()));
            notExpertCondition.OnSuccess(ItemDropRule.Common(ModContent.ItemType<SoulOfArtorias>(), 1, 6, 6));
            npcLoot.Add(notExpertCondition);
        }

        #region Gore
        public override void OnKill()
        {
            // Safety net: dying mid-charge (Nova is uninterruptible by AI, but damage can still kill
            // outright) must not leave the distortion filter permanently bound to the scene.
            if (Main.netMode != NetmodeID.Server && Filters.Scene[NovaDistortionFilter].IsActive())
            {
                Filters.Scene[NovaDistortionFilter].Deactivate();
            }

            if (!Main.dedServ)
            {
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Easterling Gore 1").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Easterling Gore 2").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Easterling Gore 3").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Easterling Gore 2").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Easterling Gore 3").Type, 1f);
            }
        }
        #endregion
    }
}

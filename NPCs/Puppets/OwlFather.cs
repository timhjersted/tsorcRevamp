using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items.Armor;
using tsorcRevamp.Content.Items.Materials;
using tsorcRevamp.Content.Items.Materials.Souls;
using tsorcRevamp.Content.Items.Materials.Souls.DarkSoul;
using tsorcRevamp.Content.Items.Weapons.Enemy;
using tsorcRevamp.Content.Items.Weapons.Melee.Axes;
using tsorcRevamp.Content.Items.Weapons.Ranged.Bows;
using tsorcRevamp.Content.Projectiles.Enemy.Weapons;
using tsorcRevamp.Content.Projectiles.VFX;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.NPCs.Puppets
{
    /// <summary>
    /// Great-fire-axe boss built on the puppet V2 swing foundation. Its melee pool is deliberately
    /// local: Owl Father uses heavy full swings, measured reversals, and high descending attacks,
    /// while Studded Leather Warrior keeps the agile dual-axe throws, running uppercuts, and spins.
    /// Wields EnemyGreatFireAxe (own enemy-only sprite/dimensions, not the player AncientFireAxe
    /// item directly). Loot still drops the real player AncientFireAxe.
    ///
    /// The companion owns the airborne molten-orb volley. Owl Father retains axe swings,
    /// greatfire crescents and the final-phase firefall array.
    /// </summary>
    [AutoloadBossHead]
    public class OwlFather : PuppetNPC
    {
        private const string HighLeapingSlamName = "High Leaping Slam";
        private const string HighLeapFollowUpName = "High Leaping Slam - Rising Follow-Up";
        private const string GreatfirePursuitSlamName = "Greatfire Pursuit Slam";
        private const string DownUpReversalName = "Down-Up Reversal";
        private const string GreatfireBreakerName = "Greatfire Breaker";
        private const string GreatfireCrescentName = "Greatfire Crescent";
        private const string BackstepReentryName = "Backstep Re-entry Chop";
        private const string FirefallArrayName = "Firefall Array";
        private const string ClosingLeapName = "Closing Leap";
        private const string FireOwlBombardmentName = "Greatfire Owl Bombardment";
        private const int FireOwlSummonIntervalTicks = 27;
        private const float FireOwlMinimumHeightAbovePlayer = 250f;
        private const float GreatfireCrescentStartRaiseRadians = 0.2617994f; // 15 degrees
        // Every overhand windup (legacy OverheadArc-family swings via OverheadWindupOvershoot below,
        // plus the V2 Greatfire Downward Swing clip's AttackStartRotation) was stopping about this
        // much short of a real raised-behind-the-head cock-back. V2 clips are static data and can't
        // read the virtual property, so its rotation is offset by this same constant by hand.
        private const float OverheadWindupOvershootRadians = 0.6109f; // 35 degrees
        private const int FireColumnBladeChargeTicks = 60;
        private const float RangedSwingMaxTriggerRange = 460f;

        public override string BossHeadTexture => "tsorcRevamp/NPCs/Puppets/OwlFather_Head_Boss";

        protected override string InvaderTitle => "Owl Father";

        // ── Companion owl + spectral form ────────────────────────────────────────
        // The owl is killed outright (StrikeInstantKill) the instant Owl Father's health crosses
        // 50%, which replaces the small puppet with the authored 2x armor-template form for the
        // rest of the fight. Sync the latched state so joining clients also see it after Owl heals above 50%.
        private const float SpectralFormHealthThreshold = 0.5f;
        private const float SpectralReachMultiplier = 2f;
        private const int SpectralHitboxWidth = 40;
        private const int SpectralHitboxHeight = 84;
        private int _companionOwlIndex = -1;
        private bool _spectralFormActive;
        private bool _spectralTransitionBurstPlayed;
        private int _fireOwlBombardmentCount;
        private int _fireOwlBombardmentSpawned;
        private int _fireOwlBombardmentTimer;
        private int _fireOwlBombardmentTarget = -1;
        private bool _fireOwlBombardmentActive;
        private Vector2 _fireOwlBombardmentAnchor;
        // Counts down from the moment the attack STARTS (telegraph, not recovery-end like the
        // combo's own CooldownAfterUse), so a bombardment that lingers spawning up to 16 owls can't
        // effectively re-arm itself the instant it finishes.
        private const int FireOwlBombardmentCooldownTicks = 25 * 60;
        private int _fireOwlBombardmentCooldown;
        // Landing-timed LeapSlam has no real blade pose until impact. This latch defers its
        // cosmetic crescent until TryGetMeleeSlashTrailPose can track the actual downswing.
        private bool _landingAxeCrescentSpawned;
        // Phase one's ranged-start attacks are dealt without replacement. The old weighted picker
        // could alternate the two high-leap variants and make the opening half of the fight look
        // like one attack on repeat even though each individual combo was on cooldown.
        private readonly List<int> _phaseOneRangedComboBag = new List<int>();
        private int _lastPhaseOneRangedComboIndex = -1;

        public override void OnSpawn(IEntitySource source)
        {
            base.OnSpawn(source);

            _phaseOneRangedComboBag.Clear();
            _lastPhaseOneRangedComboIndex = -1;

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                _companionOwlIndex = NPC.NewNPC(source, (int)NPC.Center.X, (int)NPC.Center.Y - 80,
                    ModContent.NPCType<OwlCompanion>(), ai0: NPC.whoAmI);
                NPC.netUpdate = true;
            }
        }

        public override void AI()
        {
            CheckSpectralFormTrigger();
            base.AI();
            if (_fireOwlBombardmentCooldown > 0)
                _fireOwlBombardmentCooldown--;
            TickFireOwlBombardment();
            EmitSpectralSolarWisps();
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            base.SendExtraAI(writer);
            writer.Write(_spectralFormActive);
            writer.Write(_fireOwlBombardmentActive);
            writer.Write(_fireOwlBombardmentCount);
            writer.Write(_fireOwlBombardmentSpawned);
            writer.Write(_fireOwlBombardmentTimer);
            writer.Write(_fireOwlBombardmentTarget);
            writer.WriteVector2(_fireOwlBombardmentAnchor);
            writer.Write(_fireOwlBombardmentCooldown);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            base.ReceiveExtraAI(reader);
            bool wasSpectral = _spectralFormActive;
            _spectralFormActive = reader.ReadBoolean();
            _fireOwlBombardmentActive = reader.ReadBoolean();
            _fireOwlBombardmentCount = reader.ReadInt32();
            _fireOwlBombardmentSpawned = reader.ReadInt32();
            _fireOwlBombardmentTimer = reader.ReadInt32();
            _fireOwlBombardmentTarget = reader.ReadInt32();
            _fireOwlBombardmentAnchor = reader.ReadVector2();
            _fireOwlBombardmentCooldown = reader.ReadInt32();
            if (_spectralFormActive)
            {
                ApplySpectralBodyHitbox();
                if (!wasSpectral)
                    SpawnSpectralTransitionBurst();
            }
        }

        /// <summary>Edge-triggers once, the first tick NPC.life crosses the 50% threshold: kills the
        /// companion owl (if it's still alive) and permanently flips on the spectral overlay + the
        /// 2x reach that goes with it.</summary>
        private void CheckSpectralFormTrigger()
        {
            if (_spectralFormActive || NPC.lifeMax <= 0)
            {
                return;
            }

            float healthFraction = NPC.life / (float)NPC.lifeMax;
            if (healthFraction > SpectralFormHealthThreshold)
            {
                return;
            }

            _spectralFormActive = true;
            ApplySpectralBodyHitbox();
            SpawnSpectralTransitionBurst();
            NPC.netUpdate = true;

            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            if (_companionOwlIndex >= 0 && _companionOwlIndex < Main.maxNPCs)
            {
                NPC companion = Main.npc[_companionOwlIndex];
                if (companion.active && companion.ModNPC is OwlCompanion)
                {
                    companion.StrikeInstantKill();
                }
            }

            UsefulFunctions.ScreenShake(NPC.Bottom, 5f, 16, distanceFalloff: 700f);
            SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/HollowKnight/dream_enter_pt_2")
                with { Volume = 0.8f, Pitch = -0.3f }, NPC.Center);
        }

        private void SpawnSpectralTransitionBurst()
        {
            if (_spectralTransitionBurstPlayed || Main.dedServ)
                return;

            _spectralTransitionBurstPlayed = true;
            for (int i = 0; i < 400; i++)
            {
                Vector2 direction = Main.rand.NextVector2CircularEdge(1f, 1f);
                Vector2 position = NPC.Center + Main.rand.NextVector2Circular(
                    NPC.width * 0.48f, NPC.height * 0.48f);
                Dust gold = Dust.NewDustPerfect(
                    position,
                    DustID.GoldFlame,
                    direction * Main.rand.NextFloat(2.5f, 11f),
                    45,
                    new Color(255, 215, 65),
                    Main.rand.NextFloat(0.85f, 1.85f));
                gold.noGravity = true;
                gold.fadeIn = Main.rand.NextFloat(0.75f, 1.3f);
            }

            Lighting.AddLight(NPC.Center, new Vector3(1f, 0.72f, 0.12f) * 2.2f);
        }

        /// <summary>
        /// Decorative phase-two solar flame that is intentionally not clipped by an armor or axe
        /// alpha mask. The masked shader provides animated material inside the sprites; these short
        /// turbulent ribbons start on the body/head/axe and travel beyond their silhouettes.
        /// Cosmetic randomness is client-local and never changes combat state or collision.
        /// </summary>
        private void EmitSpectralSolarWisps()
        {
            if (!_spectralFormActive || Main.dedServ || Main.gamePaused)
                return;

            Lighting.AddLight(NPC.Center, new Vector3(1f, 0.48f, 0.06f) * 0.72f);

            // The dedicated flame sprites overlap for continuity, but remain sparse enough for the
            // armor and weapon to stay legible. Offset by whoAmI so instances do not pulse together.
            if ((Main.GameUpdateCount + (ulong)(NPC.whoAmI * 3)) % 6UL != 0UL)
                return;

            float side = Main.rand.NextBool() ? -1f : 1f;
            Vector2 bodyOrigin = NPC.Center + new Vector2(
                side * Main.rand.NextFloat(NPC.width * 0.18f, NPC.width * 0.55f),
                Main.rand.NextFloat(-NPC.height * 0.38f, NPC.height * 0.32f));
            EmitSolarWisp(bodyOrigin,
                new Vector2(side * Main.rand.NextFloat(9f, 20f), -Main.rand.NextFloat(34f, 56f)),
                Main.rand.NextFloat(11f, 17f), Main.rand.Next(22, 34));

            // A less frequent crown strand keeps the head alive without turning every frame into a
            // symmetric torch. It begins inside the helmet and clears its upper edge as it rises.
            if (Main.rand.NextBool(2))
            {
                Vector2 crownOrigin = NPC.Top + new Vector2(Main.rand.NextFloat(-13f, 13f), 13f);
                EmitSolarWisp(crownOrigin,
                    new Vector2(Main.rand.NextFloat(-13f, 13f), -Main.rand.NextFloat(42f, 64f)),
                    Main.rand.NextFloat(9f, 14f), Main.rand.Next(24, 38));
            }

            // The weapon's own mask now has the same moving solar material. Give its axe head a
            // separate escaping strand so the flame continues outside the rectangular item quad.
            if (Phase != AttackPhase.Healing && Phase != AttackPhase.FleeToHeal)
            {
                Vector2 weaponDirection = PuppetWeaponDirection.SafeNormalize(
                    new Vector2(NPC.direction, 0f));
                Vector2 bladeNormal = new Vector2(-weaponDirection.Y, weaponDirection.X);
                float axeReach = Math.Max(54f, PuppetActiveBladeReach * 0.82f);
                Vector2 axeHead = PuppetHandPosition + weaponDirection * axeReach
                    + bladeNormal * Main.rand.NextFloat(-7f, 7f);
                EmitSolarWisp(axeHead,
                    -Vector2.UnitY * Main.rand.NextFloat(28f, 46f)
                        + weaponDirection * Main.rand.NextFloat(5f, 16f),
                    Main.rand.NextFloat(9f, 15f), Main.rand.Next(18, 30));
            }
        }

        private static void EmitSolarWisp(Vector2 origin, Vector2 travel, float width, int lifetime)
        {
            Vector2 direction = travel.SafeNormalize(-Vector2.UnitY);
            float travelLength = Math.Max(18f, travel.Length());
            Vector2 startSize = new Vector2(width * Main.rand.NextFloat(1.65f, 2.15f),
                travelLength * Main.rand.NextFloat(0.52f, 0.72f));
            Vector2 endSize = new Vector2(width * Main.rand.NextFloat(2.2f, 3.15f),
                travelLength * Main.rand.NextFloat(0.82f, 1.12f));

            OwlFatherSolarWispSystem.Spawn(
                origin + direction * startSize.Y * 0.12f,
                travel / Math.Max(1f, lifetime * 0.82f),
                startSize,
                endSize,
                direction.ToRotation() + MathHelper.PiOver2,
                Main.rand.NextFloat(-0.012f, 0.012f),
                lifetime,
                Main.rand.Next(3));
        }

        protected override bool HasSpectralOverlay => _spectralFormActive;
        // UseVanillaAncientArmor path: there is no oversized template, so THIS is what physically
        // enlarges phase two — a feet-anchored 2x scale of the plain small Ancient-set sprites (the
        // same Hydra-shield transform every other spectral-overlay puppet already uses), which is
        // also where the ghost-trail/halo copies below come from for free.
        // Legacy X2-template path (UseVanillaAncientArmor = false): the authored X2 equip sheets
        // provide the physical enlargement instead, so this stays at 1x to avoid enlarging an
        // already-oversized template a second time.
        protected override float SpectralOverlayScale => UseVanillaAncientArmor ? 2f : 1f;
        protected override int PuppetVisualWidth => 20;
        protected override int PuppetVisualHeight => 42;
        // The X2 template (legacy path) is the solid foreground body. The spectral pass is
        // deliberately an independent, behind-armor glow/trail so phase two keeps its ephemeral
        // presence without fading the authored armor back into a ghostly duplicate.
        protected override Color SpectralOverlayColor => new Color(255, 218, 70);
        protected override float SpectralCoreTintStrength => 0.08f;
        protected override float SpectralCoreOpacity => 0.85f;
        protected override Color SpectralHaloColor => new Color(255, 196, 42);
        protected override int SpectralHaloCopyCount => 10;
        protected override float SpectralHaloRadius => 9f;
        protected override float SpectralHaloOpacity => 0.19f;
        protected override float SpectralHaloScale => 1.025f;
        protected override bool SpectralHaloFollowsCoreOpacity => false;
        protected override float SpectralTrailOpacity => 0.2f;
        protected override bool SpectralExcludeDownwardCopies => true;
        protected override int LargeArmorMaskShaderId => OwlFatherSolarArmorShaderSystem.ShaderId;
        protected override bool IncludeMeleeWeaponInLargeArmorMask => true;

        private void ApplySpectralBodyHitbox()
        {
            if (NPC.width == SpectralHitboxWidth && NPC.height == SpectralHitboxHeight)
                return;

            // Keep feet planted while resizing the physical body to the 2x player-draw silhouette.
            // The axe remains a separate active attack, rather than a permanently hittable limb.
            Vector2 bottom = NPC.Bottom;
            NPC.width = SpectralHitboxWidth;
            NPC.height = SpectralHitboxHeight;
            NPC.Bottom = bottom;
        }

        protected override void RunMovementAI(float speedMult)
        {
            var globalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            globalNPC.NavSearchRadius = 70;
            globalNPC.RemembersLastKnownPos = true;

            SmartFighter4AI.Run(NPC,
                topSpeed: TopSpeed * speedMult,
                acceleration: Acceleration,
                doorBreakingDamage: 4,
                // This is SF4's re-aggro radius, not Owl Father's preferred attack distance.
                attackRange: 700f);
        }

        // Worn in both phases. Phase two additionally draws it at 2x via SpectralOverlayScale above,
        // instead of the bespoke X2 sprite sheets below — flip UseVanillaAncientArmor off to go
        // straight back to the original authored set (both phases); nothing about the X2 art or its
        // texture paths was touched.
        private const bool UseVanillaAncientArmor = true;

        protected override int HeadArmorItemType => UseVanillaAncientArmor
            ? ItemID.AncientArmorHat
            : ModContent.ItemType<OwlFatherMask>();
        protected override int BodyArmorItemType => UseVanillaAncientArmor
            ? ItemID.AncientArmorShirt
            : ModContent.ItemType<OwlFatherArmor>();
        protected override int LegsArmorItemType => UseVanillaAncientArmor
            ? ItemID.AncientArmorPants
            : ModContent.ItemType<OwlFatherGreaves>();
        protected override int HeadArmorDyeItemType =>
            UseVanillaAncientArmor ? ItemID.BrownAndBlackDye : 0;
        protected override int BodyArmorDyeItemType =>
            UseVanillaAncientArmor ? ItemID.BrownAndBlackDye : 0;
        protected override int LegsArmorDyeItemType =>
            UseVanillaAncientArmor ? ItemID.BrownAndBlackDye : 0;
        protected override int ArmorTemplateScale =>
            !UseVanillaAncientArmor && _spectralFormActive ? 2 : 1;
        protected override string LargeHeadArmorTemplateTexture => "tsorcRevamp/Content/Items/Armor/OwlFatherMask_Head_X2";
        protected override string LargeBodyArmorTemplateTexture => "tsorcRevamp/Content/Items/Armor/OwlFatherArmor_Body_X2";
        protected override string LargeLegsArmorTemplateTexture => "tsorcRevamp/Content/Items/Armor/OwlFatherGreaves_Legs_X2";

        protected override int MeleeWeaponItemType => ModContent.ItemType<EnemyGreatFireAxe>();
        // Sprite/animation only — Owl Father never gets a copy of this item; it isn't in
        // ModifyNPCLoot and this puppet-hand hookup adds no drop of its own.
        protected override int RangedWeaponItemType => ModContent.ItemType<BowOfEarendil>();
        protected override RangedStyle RangedAnimStyle => RangedStyle.Bow;
        protected override int RangedDamage => 26;
        // Comfortably past ComboMaxStartRange's own reach (440 / 880 in phase two) so the bow is a
        // genuine long-range option instead of being pre-empted by a melee combo every time.
        protected override float RangedRange => 900f;
        protected override float MinRangedRange => 220f;
        protected override int RangedTelegraphTicks => 42; // bow draw
        protected override int RangedRecoveryTicks => 36;
        protected override Color RangedTelegraphFlashColor => new Color(255, 150, 40);
        // A multi-arrow volley reads as a deliberate, planted "notch and loose" cadence, not
        // something Owl drifts through — root him for the whole burst instead of the 33% default.
        protected override int StandingRangedChance => 100;
        // Shot 0 (fired straight from the telegraph) aims at the player's exact position; every
        // shot after that (fired from the CrossbowBurstPause chain below) leads based on their
        // current velocity — see DoRangedAttack. Phase one is a fixed 1-direct + 2-leading volley
        // at a slow 45t cadence; phase two can roll either a 3- or 6-arrow volley at a faster 30t
        // notching cadence. (Array length = shots AFTER the first, so {30,30} is 3 total shots.)
        protected override int[][] PrimaryRangedBurstPatterns => _spectralFormActive
            ? new[] { new[] { 30, 30 }, new[] { 30, 30, 30, 30, 30 } }
            : new[] { new[] { 45, 45 } };
        protected override int[] PrimaryRangedBurstChances => _spectralFormActive
            ? new[] { 50, 50 }
            : null;
        // BowOfEarendil is drawn tall/vertical, not the horizontal shape the base (0.25, 0.5) grip
        // guess fits — that mismatch is what made the puppet's hand pivot sit off toward one edge
        // instead of the bow's actual center. Center grip (the riser/"white part") plus a slightly
        // smaller sprite so a puppet arm can plausibly reach it, pushed back out ~24px away from the
        // body (positive X here is mirrored by facing, so it always reads as "outward").
        protected override Vector2 GetHeldRangedGripNorm(int itemType) => new Vector2(0.5f, 0.5f);
        protected override float GetHeldRangedDrawScale(int itemType) => 0.9f;
        protected override Vector2 GetHeldRangedDrawOffset(int itemType) => new Vector2(24f, 0f);
        // Back arm reaches for a second grip point near the front hand — the existing two-handed-
        // weapon composite-arm IK (built for a great-weapon's second hilt point, unused by any
        // puppet until now) fits a bow held with both hands equally well. Scoped to exactly the
        // phases the bow is actually the held item, so the melee axe swing is untouched.
        private bool IsRangedBowPosePhase =>
            Phase == AttackPhase.RangedTelegraph
            || Phase == AttackPhase.RangedAttack
            || Phase == AttackPhase.CrossbowBurstPause;
        protected override bool UseCompositeArmForAdditionalPhase => IsRangedBowPosePhase;
        protected override bool UseTwoHandedCompositeSwing => IsRangedBowPosePhase;
        // The melee default (4px) target a hilt only a hand-width from the front grip — for a bow
        // the front hand is already dead-center (GetHeldRangedGripNorm above), so a 4px target gave
        // the IK solver nothing to reach for and it just settled on a relaxed, barely-visible pose.
        // A real separation forces it to pick a genuinely extended stretch frame instead.
        protected override float TwoHandedBackGripOffset => 18f;

        protected override int MeleeDamage => 30;

        protected override WeaponArchetype MeleeArchetype => WeaponArchetype.Axe;
        // Owl Father's axe art is authored with its blade facing the ground. Keep that orientation
        // during underhand/upward attacks instead of vertically mirroring the weapon to lead the arc.
        protected override bool MeleeWeaponIsSingleBladed => false;

        private static MeleeComboStep AxeSwing(
            ComboMotion motion,
            int telegraphTicks,
            int attackTicks,
            int pauseAfter = 0,
            float damageMult = 1f,
            float forwardPushMult = 0f,
            float reachMult = 1.15f,
            float leapHeightMult = 1f,
            float leapForwardSpeedMult = 1f,
            float leapStrikeRange = 0f,
            float leapDescentGravityMult = 1f,
            float leapApexRetargetStrength = 0f,
            SwingEaseStyle ease = SwingEaseStyle.Smooth)
            => new MeleeComboStep
            {
                Motion = motion,
                TelegraphTicks = telegraphTicks,
                AttackTicks = attackTicks,
                PostStepPause = pauseAfter,
                DamageMult = damageMult,
                ReachMult = reachMult,
                ForwardPushMult = forwardPushMult,
                SwingSpeedMult = 1f,
                Ease = ease,
                LeapHeightMult = leapHeightMult,
                LeapForwardSpeedMult = leapForwardSpeedMult,
                LeapStrikeRange = leapStrikeRange,
                LeapDescentGravityMult = leapDescentGravityMult,
                LeapApexRetargetStrength = leapApexRetargetStrength,
            };

        // The two V2 fundamentals are copied as definitions rather than shared by reference with
        // Studded. Their timings can now diverge as Owl's heavier axe is tuned.
        private static readonly PuppetAttackClip GreatfireDownwardSwingV2 = new PuppetAttackClip(
            name: "Greatfire Downward Swing",
            pose: PuppetPosePreset.TwoHandedSwing,
            windupTicks: 40,
            activeTicks: 26,
            recoveryTicks: 22,
            oppositeWindupRotation: 1.15f,
            attackStartRotation: -1.45f - OverheadWindupOvershootRadians,
            attackEndRotation: 1.15f,
            swingEase: SwingEaseStyle.Whip);

        private static readonly PuppetAttackClip GreatfireUpwardSwingV2 = new PuppetAttackClip(
            name: "Greatfire Upward Swing",
            pose: PuppetPosePreset.TwoHandedSwing,
            windupTicks: 38,
            activeTicks: 25,
            recoveryTicks: 22,
            oppositeWindupRotation: -1.15f,
            attackStartRotation: 1.25f,
            attackEndRotation: -1.25f,
            swingEase: SwingEaseStyle.Smooth);

        // Named so ShouldContinueMeleeCombo can re-check ITS reach/push without reaching into the
        // private, per-instance _activeMeleeCombo.Steps copy PuppetNPC keeps — this is the same
        // step data, just referenced directly instead of looked up at runtime.
        private static readonly MeleeComboStep DownUpReversalFollowUpStep = AxeSwing(
            ComboMotion.UnderhandArc, 0, 27,
            damageMult: 1.18f, forwardPushMult: 0.58f, reachMult: 1.18f);

        private static readonly MeleeCombo[] OwlFatherAxeCombos = new[]
        {
            new MeleeCombo
            {
                Name = "Greatfire Downward Swing",
                BaseWeight = 70,
                Preferred = ComboRangeBand.Close,
                InitialFlashColor = Color.OrangeRed,
                CooldownAfterUse = 75,
                MoveBrake = 0.22f,
                RuntimeV2Clip = GreatfireDownwardSwingV2,
                Steps = new[] { AxeSwing(ComboMotion.OverheadArc, 40, 26,
                    damageMult: 1.15f, forwardPushMult: 0.42f, reachMult: 1.18f,
                    ease: SwingEaseStyle.Whip) },
            },
            new MeleeCombo
            {
                Name = "Greatfire Upward Swing",
                BaseWeight = 65,
                Preferred = ComboRangeBand.Close,
                InitialFlashColor = Color.Orange,
                CooldownAfterUse = 80,
                MoveBrake = 0.20f,
                RuntimeV2Clip = GreatfireUpwardSwingV2,
                Steps = new[] { AxeSwing(ComboMotion.UnderhandArc, 38, 25,
                    damageMult: 1.12f, forwardPushMult: 0.45f, reachMult: 1.18f) },
            },
            new MeleeCombo
            {
                // The regular overhead starter. Its underhand follow-up is not a coin flip — see
                // ShouldContinueMeleeCombo: it plays when the overhead connects, or (on a whiff)
                // when its own reach plus the ground it covers during the push would still reach
                // the player from here. A clean miss with the player out of range just ends in
                // recovery instead of committing to a second swing that cannot possibly land.
                Name = DownUpReversalName,
                BaseWeight = 90,
                Preferred = ComboRangeBand.Close,
                InitialFlashColor = Color.OrangeRed,
                CooldownAfterUse = 145,
                MoveBrake = 0.16f,
                Steps = new[]
                {
                    AxeSwing(ComboMotion.OverheadArc, 34, 25, pauseAfter: 9,
                        damageMult: 0.88f, forwardPushMult: 0.48f),
                    DownUpReversalFollowUpStep,
                },
            },
            new MeleeCombo
            {
                Name = "Up-Down Reversal",
                BaseWeight = 80,
                Preferred = ComboRangeBand.Close,
                InitialFlashColor = Color.Gold,
                CooldownAfterUse = 150,
                MoveBrake = 0.16f,
                Steps = new[]
                {
                    AxeSwing(ComboMotion.UnderhandArc, 34, 25, pauseAfter: 9,
                        damageMult: 0.88f, forwardPushMult: 0.48f),
                    AxeSwing(ComboMotion.OverheadArc, 0, 28,
                        damageMult: 1.22f, forwardPushMult: 0.58f, reachMult: 1.20f,
                        ease: SwingEaseStyle.Whip),
                },
            },
            new MeleeCombo
            {
                Name = "Advancing Down-Up",
                BaseWeight = 65,
                Preferred = ComboRangeBand.Mid,
                InitialFlashColor = Color.DarkOrange,
                CooldownAfterUse = 185,
                MoveBrake = 0.06f,
                Steps = new[]
                {
                    AxeSwing(ComboMotion.OverheadArc, 38, 27, pauseAfter: 8,
                        damageMult: 0.95f, forwardPushMult: 0.78f, reachMult: 1.18f),
                    AxeSwing(ComboMotion.UnderhandArc, 0, 29,
                        damageMult: 1.28f, forwardPushMult: 0.88f, reachMult: 1.22f),
                },
            },
            new MeleeCombo
            {
                Name = "Delayed Heavy Chop",
                BaseWeight = 45,
                Preferred = ComboRangeBand.Close,
                InitialFlashColor = Color.Red,
                CooldownAfterUse = 230,
                HeavyCommit = true,
                HyperArmor = true,
                MoveBrake = 0.48f,
                Steps = new[] { AxeSwing(ComboMotion.OverheadArc, 54, 32,
                    damageMult: 1.80f, forwardPushMult: 0.22f, reachMult: 1.28f,
                    ease: SwingEaseStyle.Whip) },
            },
            new MeleeCombo
            {
                Name = HighLeapingSlamName,
                BaseWeight = 24,
                Preferred = ComboRangeBand.Mid,
                InitialFlashColor = Color.OrangeRed,
                CooldownAfterUse = 360,
                HeavyCommit = true,
                HyperArmor = true,
                RangedStartOnly = true,
                MoveBrake = 0.15f,
                Steps = new[] { AxeSwing(ComboMotion.LeapSlam, 44, 115,
                    damageMult: 1.55f, reachMult: 1.28f,
                    leapHeightMult: 1.18f, leapForwardSpeedMult: 1.05f,
                    // A high jump gives the player a long window to just walk out from under the
                    // launch-time landing spot. Re-aim at apex and slam down ~2x faster so that
                    // window closes instead of guaranteeing a whiff.
                    leapDescentGravityMult: 2.2f, leapApexRetargetStrength: 1f,
                    ease: SwingEaseStyle.Trapezoidal) },
            },
            // Attack spec / timing sheet — Greatfire Pursuit Slam
            // Weapon: EnemyGreatFireAxe, carried from -117 degrees to a +63 degree landing pose;
            // the 43t on-screen tell (36 authored * 1.20) has orange/gold blade embers.
            // Move/reach: 5.89px/t upward launch (0.62 height), ~39.3t flat-ground airtime,
            // 9.24px/t horizontal cap (1.70 forward), ~363px body travel plus 79px blade reach.
            // ReliableJumpStartRange subtracts the 24px landing standoff and 24px terrain margin,
            // so phase-one selection stops at ~394px instead of asking the leap to land short.
            // Live/counter: within 120px after the apex, the last <=10t downswing becomes live;
            // one well-timed 22t roll beats it, with 22t recovery (including a 6t planted hold).
            // Selection: one entry in the phase-one ranged bag, 150t personal cooldown; no fire
            // columns, projectile, copies, debuff, or player displacement. Damage is 1.25x (37).
            // Multiplayer: the shared combo picker/launch is server-authoritative; only cosmetic
            // dust, light, sound, and screen shake run client-side.
            new MeleeCombo
            {
                Name = GreatfirePursuitSlamName,
                BaseWeight = 90,
                Preferred = ComboRangeBand.Mid,
                InitialFlashColor = Color.DarkOrange,
                CooldownAfterUse = 150,
                HeavyCommit = true,
                HyperArmor = true,
                RangedStartOnly = true,
                MoveBrake = 0.06f,
                Steps = new[] { AxeSwing(ComboMotion.LeapSlam, 36, 82,
                    damageMult: 1.25f, reachMult: 1.25f,
                    leapHeightMult: 0.62f, leapForwardSpeedMult: 1.70f,
                    leapStrikeRange: 120f,
                    ease: SwingEaseStyle.Whip) },
            },
            new MeleeCombo
            {
                Name = HighLeapFollowUpName,
                BaseWeight = 42,
                Preferred = ComboRangeBand.Mid,
                InitialFlashColor = Color.Gold,
                CooldownAfterUse = 280,
                HeavyCommit = true,
                HyperArmor = true,
                RangedStartOnly = true,
                MoveBrake = 0.15f,
                Steps = new[]
                {
                    AxeSwing(ComboMotion.LeapSlam, 48, 118, pauseAfter: 10,
                        damageMult: 1.30f, reachMult: 1.26f,
                        leapHeightMult: 1.22f, leapForwardSpeedMult: 1.05f,
                        leapDescentGravityMult: 2.2f, leapApexRetargetStrength: 1f,
                        ease: SwingEaseStyle.Trapezoidal),
                    AxeSwing(ComboMotion.UnderhandArc, 0, 28,
                        damageMult: 1.25f, forwardPushMult: 0.38f, reachMult: 1.20f),
                },
            },
            new MeleeCombo
            {
                Name = GreatfireBreakerName,
                BaseWeight = 52,
                Preferred = ComboRangeBand.Close,
                InitialFlashColor = Color.OrangeRed,
                CooldownAfterUse = 250,
                HeavyCommit = true,
                HyperArmor = true,
                MoveBrake = 0.52f,
                Steps = new[] { AxeSwing(ComboMotion.OverheadArc, 56, 34,
                    damageMult: 1.65f, forwardPushMult: 0.18f, reachMult: 1.28f,
                    ease: SwingEaseStyle.Whip) },
            },
            new MeleeCombo
            {
                Name = GreatfireCrescentName,
                BaseWeight = 75,
                Preferred = ComboRangeBand.Far,
                InitialFlashColor = Color.Gold,
                CooldownAfterUse = 170,
                RangedStartOnly = true,
                MoveBrake = 0.36f,
                Steps = new[] { AxeSwing(ComboMotion.OverheadArc, 42, 30,
                    damageMult: 0.80f, reachMult: 1.16f,
                    ease: SwingEaseStyle.Whip) },
            },
            new MeleeCombo
            {
                Name = BackstepReentryName,
                BaseWeight = 55,
                Preferred = ComboRangeBand.Close,
                InitialFlashColor = Color.Orange,
                CooldownAfterUse = 230,
                HeavyCommit = true,
                HyperArmor = true,
                MoveBrake = 0f,
                Steps = new[]
                {
                    AxeSwing(ComboMotion.BackstepRaise, 30, 70, pauseAfter: 7, damageMult: 0f),
                    AxeSwing(ComboMotion.OverheadArc, 0, 29,
                        damageMult: 1.42f, forwardPushMult: 1.05f, reachMult: 1.24f,
                        ease: SwingEaseStyle.Whip),
                },
            },
            new MeleeCombo
            {
                Name = FireOwlBombardmentName,
                BaseWeight = 48,
                Preferred = ComboRangeBand.Far,
                InitialFlashColor = Color.Orange,
                CooldownAfterUse = 520,
                HeavyCommit = true,
                HyperArmor = true,
                RangedStartOnly = true,
                MoveBrake = 0.22f,
                // This short command only launches the background summon controller. Owl Father
                // can select and perform ordinary attacks while its 27-tick owl cadence continues.
                Steps = new[] { AxeSwing(ComboMotion.BackstepRaise, 24, 10, damageMult: 0f) },
            },
            new MeleeCombo
            {
                Name = FirefallArrayName,
                BaseWeight = 62,
                Preferred = ComboRangeBand.Far,
                InitialFlashColor = Color.Red,
                CooldownAfterUse = 300,
                HeavyCommit = true,
                HyperArmor = true,
                RangedStartOnly = true,
                MoveBrake = 0.55f,
                Steps = new[] { AxeSwing(ComboMotion.OverheadArc, 52, 32,
                    damageMult: 0f, ease: SwingEaseStyle.Whip) },
            },
            // Phase two only: pure traversal. A low, fast, zero-damage hop so Owl can close a long
            // gap by leaping instead of only ever plodding in on foot — not a committed attack, so
            // no HeavyCommit/HyperArmor and a short telegraph/recovery.
            new MeleeCombo
            {
                Name = ClosingLeapName,
                BaseWeight = 60,
                Preferred = ComboRangeBand.Far,
                InitialFlashColor = Color.Gold,
                CooldownAfterUse = 130,
                RecoveryTicks = 10,
                RangedStartOnly = true,
                MoveBrake = 0.06f,
                Steps = new[] { AxeSwing(ComboMotion.LeapSlam, 18, 70,
                    damageMult: 0f,
                    leapHeightMult: 0.55f, leapForwardSpeedMult: 1.85f,
                    ease: SwingEaseStyle.Whip) },
            },
        };

        protected override MeleeCombo[] MeleeComboPoolOverride => OwlFatherAxeCombos;

        // ── Swing-polish opt-ins — THE reason this invader exists ──────────────────
        protected override bool UseSwingEasing => true;
        // Attack direction is authored explicitly. Randomly reversing an arc makes the cutting edge
        // and its telegraph harder to read on this single-bladed axe.
        protected override bool UseAlternateFlip => false;
        protected override bool UseAimAdaptiveArc => true;
        protected override bool UseLogicalMeleeTelegraphs => true;
        protected override bool UseCompositeArmSwing => true;
        protected override float MeleeCompositeArmRotationOffset => MeleeWeaponRotationOffset;
        protected override bool PreserveShaftDirectionOnBladeFlip => true;
        protected override bool UseLandingTimedLeapSlam => true;
        // Winds the raised-overhead pose back an extra 35 degrees before every OverheadArc-family
        // downswing (and the LeapSlam ground telegraph, which shares the same table entry) commits.
        protected override float OverheadWindupOvershoot => OverheadWindupOvershootRadians;
        protected override int MeleeComboInterStepLingerTicks => 3;
        protected override int MeleeRecoveryLingerTicks => 6;
        protected override bool MirrorMeleeSwingRotationByFacing => true;
        // The animated procedural fire material (see PuppetNPC.HasFireSlashVFX) remains a free
        // layer above the swing rather than being clipped into a crescent mask. Each axe swing also
        // spawns a separate VanillaSwordArc sprite below, tracked from this puppet's live hand and
        // blade direction. Keeping them independent means the readable crescent stays exactly on
        // the weapon while the fire is free to curl, flow and fade on its own.
        protected override bool HasFireSlashVFX => true;
        protected override Color FireSlashCinderColor => new Color(40, 6, 2);
        protected override Color FireSlashFlameColor => new Color(226, 90, 20);
        protected override Color FireSlashCoreColor => new Color(255, 200, 80);
        // The axe's blade travels on the opposite side of its shaft from Gwyn's greatsword.
        // Reverse only the shader's local sweep so the bright curve sits on the axe tip instead
        // of bowing above and ahead of it; hand, weapon, and collision remain shared.
        protected override bool FireSlashSweepFlippedWhenFacingRight => false;
        // Calibrated against the axe's asymmetric head: the procedural arc needs to sit a little
        // farther along the blade and one-and-a-half tiles lower than the generic sword anchor.
        protected override float FireSlashForwardOffsetPixels => 5f;
        protected override Vector2 FireSlashWorldOffset => new Vector2(0f, 24f);

        protected override void ModifyMeleeArcEndpoints(
            ComboMotion motion, ref float startRotation, ref float endRotation)
        {
            base.ModifyMeleeArcEndpoints(motion, ref startRotation, ref endRotation);

            if (motion == ComboMotion.OverheadArc && ActiveMeleeComboName == GreatfireCrescentName)
            {
                // Pull only the start/top of Greatfire Crescent 15 degrees farther overhead. The
                // end point stays fixed, giving the downswing a slightly larger committed arc.
                startRotation -= GreatfireCrescentStartRaiseRadians;
            }
        }

        // ── Axe draw tuning ─────────────────────────────────────────────────────────
        // The shaft runs from (2, 61) to (51, 12) in the 72x64 texture: grip ~20% above its butt.
        protected override Vector2 MeleeHandleNorm => new Vector2(0.17f, 0.80f);
        protected override float MeleeWeaponDrawScale => 0.85f;
        // Scales 2x once the spectral form triggers (see SpectralReachMultiplier below) — the giant
        // duplicate's axe needs a hitbox that actually matches its 2x visual reach, replacing the
        // normal-size hitbox entirely rather than adding a second one (per design: one hitbox, sized
        // to whichever body is currently the "real" one).
        protected override float ComboReachBase => _spectralFormActive ? 90f * SpectralReachMultiplier : 90f;
        protected override float MeleeWeaponRotationOffset => 1.0f;

        protected override float TopSpeed => 2.95f;
        protected override float Acceleration => 0.095f;
        protected override float MeleeRange => _spectralFormActive ? 82f * SpectralReachMultiplier : 82f;
        protected override float StabRange => 150f; // unused — CanStab is false below
        // Matches Studded's 440 — was 360 for no recorded reason, and this is the radius the
        // Leaping Slam / Closing Leap combos (all RangedStartOnly gap-closers) can even be
        // rolled from. Wider means Owl reaches for a leap-in more often right after a ranged
        // exchange instead of falling through to a plain run. Also scales with the spectral form so
        // combos keep being selectable from the giant axe's actual reach.
        protected override float ComboMaxStartRange => _spectralFormActive ? 440f * SpectralReachMultiplier : 440f;
        // Normal chops should begin from visibly close range. When farther away, the shared
        // ClosingDistance phase pursues first; RangedStartOnly leaps and fire attacks bypass it.
        protected override float MeleeEngageRange => MeleeRange * 0.85f;
        protected override int ClosingDistanceMaxTicks => 130;
        protected override float LeapAttackForwardSpeed => TopSpeed * 2.05f;
        protected override float LeapAttackMinimumForwardSpeed => 0.55f;
        protected override float LeapAttackTargetLeadTicks => 10f;
        protected override float LeapAttackAscentTrackingStrength => 0.10f;
        protected override int MeleeComboChance => 100;
        protected override int RangedStartMeleeComboChance => 70;
        protected override float ComboTelegraphMultiplier => 1.20f;
        // Was 0.55 (Studded/Dread Wraith both run 0.85-0.9) for no recorded reason. This only
        // matters once a close-range swing is already telegraphing and the target backs away —
        // matching Studded's pace keeps that swing from whiffing as often.
        protected override float ComboTelegraphAdvanceSpeedMult => 0.85f;
        protected override float ComboTelegraphAdvanceStopDistance => MeleeRange * 0.62f;
        // The Leaping Slam and Closing Leap combos land about 1.5 tiles short of the
        // player's exact position instead of squarely on top of them — still comfortably inside
        // the landing slam's own reach (~80px), but reads as "closed most of the gap and arrived,"
        // not "teleported onto your face." See LeapLandingStandoff's doc comment in PuppetNPC.cs.
        protected override float LeapLandingStandoff => 24f;
        protected override int MeleeRecoveryTicks => 22;
        protected override int CasualStrollChance => 0;

        // One interruptible final-phase drink. It can restore a meaningful amount from critical
        // life, but the cap keeps Owl below one third so final-phase attacks remain unlocked.
        protected override int EstusChargesMax => 1;
        protected override float EstusHealFraction => 0.15f;
        protected override float FirstHealThreshold => 0.24f;
        protected override float SecondHealThreshold => -1f;
        protected override float HealLifeCapFraction => 0.329f;
        protected override int HealCooldownTicks => 900;
        protected override int HealAnimationTicks => 110;
        protected override float RecentDamageThreshold => 1f;
        protected override float FleeToHealDistance => 20f * 16f;
        protected override int FleeToHealMaxTicks => 120;

        protected override int MeleeTelegraphTicks => 36;
        protected override bool CanStab => false;

        protected override Color MeleeTelegraphFlashColor => new Color(255, 120, 40);

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 1;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Confused] = true;
        }

        public override void SetDefaults()
        {
            NPC.width = 20;
            NPC.height = 42;
            NPC.lifeMax = 2600;
            NPC.defense = 16;
            NPC.damage = 0;
            NPC.knockBackResist = 0.22f;
            NPC.aiStyle = -1;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath2;
            NPC.value = 14000f;
            NPC.boss = true;
            NPC.npcSlots = 5f;

            tsorcRevampGlobalNPC globalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            globalNPC.PoiseMax = 35f;
            globalNPC.PoiseStaggerResetsAI = true;
            globalNPC.NavGiveUpTicks = 180;
            globalNPC.CanUseRopes = true;
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<OwlFatherMask>()));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<OwlFatherArmor>()));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<OwlFatherGreaves>()));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<AncientFireAxe>(), 2));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<DarkSoulItem>(), 1, 500, 750));
        }

        protected override void DoMeleeAttack()
        {
            ArmFireSlashVFX(MeleeRange * 0.7f, 0.5f);
            SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.65f, PitchVariance = 0.2f }, NPC.Center);
            TryMeleeHit();
        }

        // How far ahead of the player's current position a leading shot (every arrow after the
        // first) aims, scaled by their live velocity. Flat px/tick speed the arrow itself travels at.
        private const float FlamingArrowLeadTicks = 12f;
        private const float FlamingArrowSpeed = 13f;

        protected override void DoRangedAttack()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            Player target = Main.player[NPC.target];
            Vector2 origin = PuppetHandPosition;

            // Shot 0 fires straight from the telegraph and aims at the player's exact position;
            // every later shot (fired from the CrossbowBurstPause chain, see PrimaryRangedBurstPatterns
            // above) leads based on their current velocity instead, so a volley already under way
            // can't be walked out from under entirely.
            Vector2 aimAt = CurrentBurstShotIndex == 0
                ? target.Center
                : target.Center + target.velocity * FlamingArrowLeadTicks;

            Vector2 aimDirection = (aimAt - origin).SafeNormalize(new Vector2(NPC.direction, 0f));

            SoundEngine.PlaySound(SoundID.Item5 with { Volume = 0.7f, PitchVariance = 0.15f }, origin);

            Projectile.NewProjectile(
                NPC.GetSource_FromThis(),
                origin,
                aimDirection * FlamingArrowSpeed,
                ModContent.ProjectileType<EnemyFlamingArrow>(),
                RangedDamage,
                2f,
                Main.myPlayer);
        }

        // Every confirmed axe hit (plain swings, combos) sets the target ablaze — matches the
        // player AncientFireAxe's own fire theme (that one applies 12s; 6s here per the enemy tuning).
        protected override void OnBladeHit(Player player)
        {
            player.AddBuff(BuffID.OnFire, 6 * 60);
        }

        protected override bool CanSelectMeleeCombo(MeleeCombo combo, float distance, float healthFraction)
        {
            // Save the more elaborate second high-leap conversion for the giant spectral phase.
            // Phase one gets one occasional high signature slam plus the low pursuit leap below.
            if (combo.Name == HighLeapFollowUpName && !_spectralFormActive)
                return false;

            // The giant phase-two form is where the far-range melee-trigger bug (swinging while the
            // player is nowhere close) actually gets reported, since ComboMaxStartRange doubles for
            // it — this pure-traversal hop belongs to the same phase.
            if (combo.Name == ClosingLeapName && !_spectralFormActive)
                return false;

            if ((combo.Name == GreatfireBreakerName || combo.Name == BackstepReentryName)
                && healthFraction > 0.66f)
                return false;

            if (combo.Name == FirefallArrayName && healthFraction > 1f / 3f)
                return false;

            if (combo.Name == FireOwlBombardmentName)
                return _spectralFormActive && !_fireOwlBombardmentActive
                    && _fireOwlBombardmentCooldown <= 0;

            // Neither of these is a gap-closer: Crescent's ground wave only travels ~480px before
            // PuppetGreatfireCrescent's 54-tick lifetime runs out, and Firefall Array is meant to
            // read as a close-range gesture. Unlike ComboMaxStartRange this is intentionally flat —
            // an axe swing's own reach doesn't grow with the phase-two giant scale the way the leap
            // combos' actual jump distance does, so doubling it here just let Owl swing at empty air
            // from clear across the arena (ComboMaxStartRange's own 880px phase-two ceiling).
            if ((combo.Name == GreatfireCrescentName || combo.Name == FirefallArrayName)
                && distance > RangedSwingMaxTriggerRange)
                return false;

            if (IsJumpGapCloser(combo) && distance > ReliableJumpStartRange(combo.Steps[0]))
                return false;

            return true;
        }

        protected override int ReactiveComboIndex(float distance, ComboRangeBand band, int[] ready)
        {
            if (_spectralFormActive)
                return base.ReactiveComboIndex(distance, band, ready);

            bool choosingRangedStart = false;
            for (int i = 0; i < OwlFatherAxeCombos.Length && i < ready.Length; i++)
            {
                if (ready[i] > 0 && OwlFatherAxeCombos[i].RangedStartOnly)
                {
                    choosingRangedStart = true;
                    break;
                }
            }

            if (!choosingRangedStart)
                return base.ReactiveComboIndex(distance, band, ready);

            // Cooldowns and health/range gates narrow the current bag. Invalid entries are dropped;
            // once empty, refill from every currently-ready phase-one ranged opener and shuffle.
            for (int i = _phaseOneRangedComboBag.Count - 1; i >= 0; i--)
            {
                int comboIndex = _phaseOneRangedComboBag[i];
                if (comboIndex < 0 || comboIndex >= ready.Length || ready[comboIndex] <= 0
                    || !OwlFatherAxeCombos[comboIndex].RangedStartOnly)
                    _phaseOneRangedComboBag.RemoveAt(i);
            }

            if (_phaseOneRangedComboBag.Count == 0)
            {
                for (int i = 0; i < OwlFatherAxeCombos.Length && i < ready.Length; i++)
                {
                    if (ready[i] > 0 && OwlFatherAxeCombos[i].RangedStartOnly)
                        _phaseOneRangedComboBag.Add(i);
                }

                for (int i = _phaseOneRangedComboBag.Count - 1; i > 0; i--)
                {
                    int swapIndex = Main.rand.Next(i + 1);
                    int held = _phaseOneRangedComboBag[i];
                    _phaseOneRangedComboBag[i] = _phaseOneRangedComboBag[swapIndex];
                    _phaseOneRangedComboBag[swapIndex] = held;
                }
            }

            if (_phaseOneRangedComboBag.Count == 0)
                return base.ReactiveComboIndex(distance, band, ready);

            int drawPosition = _phaseOneRangedComboBag.Count - 1;
            if (_phaseOneRangedComboBag.Count > 1
                && _phaseOneRangedComboBag[drawPosition] == _lastPhaseOneRangedComboIndex)
            {
                for (int i = 0; i < drawPosition; i++)
                {
                    if (_phaseOneRangedComboBag[i] == _lastPhaseOneRangedComboIndex)
                        continue;

                    int held = _phaseOneRangedComboBag[drawPosition];
                    _phaseOneRangedComboBag[drawPosition] = _phaseOneRangedComboBag[i];
                    _phaseOneRangedComboBag[i] = held;
                    break;
                }
            }

            int chosen = _phaseOneRangedComboBag[drawPosition];
            _phaseOneRangedComboBag.RemoveAt(drawPosition);
            _lastPhaseOneRangedComboIndex = chosen;
            return chosen;
        }

        private bool IsJumpGapCloser(MeleeCombo combo)
            => combo.Name == HighLeapingSlamName
                || combo.Name == HighLeapFollowUpName
                || combo.Name == GreatfirePursuitSlamName
                || combo.Name == ClosingLeapName;

        private float ReliableJumpStartRange(MeleeComboStep step)
        {
            float heightMult = step.LeapHeightMult > 0f ? step.LeapHeightMult : 1f;
            float forwardMult = step.LeapForwardSpeedMult > 0f ? step.LeapForwardSpeedMult : 1f;
            float airtime = 2f * LeapAttackUpSpeed * heightMult / 0.3f;
            float maximumTravel = airtime * LeapAttackForwardSpeed * forwardMult;
            float bladeReach = ComboReachBase * 0.7f * step.ReachMult;
            // Leave a small margin for uneven ground and motion after the ascent lock. If the
            // player is farther away, Owl Father chooses a real ranged move or pursues first.
            return maximumTravel + bladeReach - LeapLandingStandoff - 24f;
        }

        protected override void OnMeleeComboTelegraphTick(
            MeleeCombo combo, MeleeComboStep step, int elapsed, int total)
        {
            EmitFireColumnBladeCharge(combo, step, elapsed);

            if (combo.Name == GreatfirePursuitSlamName)
                EmitPursuitSlamTelegraph(step, elapsed, total);

            if (combo.Name == FirefallArrayName && elapsed == 10)
                SpawnFirefallArray();
            else if (combo.Name == FireOwlBombardmentName && elapsed == 12)
                StartFireOwlBombardment();
        }

        private void EmitPursuitSlamTelegraph(MeleeComboStep step, int elapsed, int total)
        {
            if (Main.dedServ || elapsed % 2 != 0)
                return;

            float progress = (elapsed + 1f) / Math.Max(1, total);
            Vector2 weaponDirection = PuppetWeaponDirection.SafeNormalize(
                new Vector2(NPC.direction, 0f));
            Vector2 bladeNormal = new Vector2(-weaponDirection.Y, weaponDirection.X);
            float bladeReach = ComboReachBase * 0.7f * step.ReachMult;
            Vector2 axeHead = PuppetHandPosition + weaponDirection * bladeReach * 0.86f;
            Dust ember = Dust.NewDustPerfect(
                axeHead + bladeNormal * Main.rand.NextFloat(-5f, 5f),
                Main.rand.NextBool(3) ? DustID.GoldFlame : DustID.OrangeTorch,
                new Vector2(-NPC.direction * Main.rand.NextFloat(0.8f, 2.2f),
                    Main.rand.NextFloat(-0.9f, 0.2f)),
                55,
                new Color(255, 150, 35),
                MathHelper.Lerp(0.65f, 1.25f, progress));
            ember.noGravity = true;
            ember.fadeIn = 0.8f + progress * 0.5f;
            Lighting.AddLight(axeHead, new Vector3(1f, 0.28f, 0.03f) * (0.2f + progress * 0.45f));
        }

        /// <summary>
        /// Summons a fixed airborne formation around the player's position when the attack began.
        /// Each side is a 2-by-4 grid: columns are 120px apart and rows are 100px apart, so every
        /// pair is separated by at least 100px. Its nearest row stays at least 250px above the live
        /// player position, with subsequent rows extending upward, while the horizontal formation
        /// remains at least 400px to the left or right of the captured attack position.
        /// </summary>
        private void StartFireOwlBombardment()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient || !NPC.HasValidTarget)
                return;

            Player target = Main.player[NPC.target];
            _fireOwlBombardmentCount = Main.rand.Next(8, 17);
            _fireOwlBombardmentSpawned = 0;
            _fireOwlBombardmentTimer = 0;
            _fireOwlBombardmentTarget = target.whoAmI;
            _fireOwlBombardmentAnchor = target.Center;
            _fireOwlBombardmentActive = true;
            _fireOwlBombardmentCooldown = FireOwlBombardmentCooldownTicks;
            NPC.netUpdate = true;
        }

        private void TickFireOwlBombardment()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient || !_fireOwlBombardmentActive)
                return;

            if (_fireOwlBombardmentTarget < 0 || _fireOwlBombardmentTarget >= Main.maxPlayers
                || !Main.player[_fireOwlBombardmentTarget].active || Main.player[_fireOwlBombardmentTarget].dead)
            {
                _fireOwlBombardmentActive = false;
                NPC.netUpdate = true;
                return;
            }

            if (++_fireOwlBombardmentTimer % FireOwlSummonIntervalTicks != 0)
                return;

            int ordinal = _fireOwlBombardmentSpawned++;
            Player target = Main.player[_fireOwlBombardmentTarget];
            int side = ordinal % 2 == 0 ? -1 : 1;
            int sideSlot = ordinal / 2;
            int column = sideSlot % 2;
            int row = sideSlot / 2;
            // Keep the original formation stable if the player falls, but shift the whole stack
            // upward if the player climbs during the long summon cadence. This preserves 100px row
            // spacing while guaranteeing that even the nearest row remains at least 250px above.
            float nearestRowY = Math.Min(
                _fireOwlBombardmentAnchor.Y - FireOwlMinimumHeightAbovePlayer,
                target.Center.Y - FireOwlMinimumHeightAbovePlayer);
            Vector2 spawnPosition = _fireOwlBombardmentAnchor + new Vector2(
                side * (400f + column * 120f),
                nearestRowY - _fireOwlBombardmentAnchor.Y - row * 100f);

            int owlIndex = NPC.NewNPC(NPC.GetSource_FromThis(), (int)spawnPosition.X,
                (int)spawnPosition.Y, ModContent.NPCType<OwlFireDiveCompanion>(),
                ai0: target.whoAmI, ai2: ordinal);
            if (owlIndex >= 0 && owlIndex < Main.maxNPCs)
            {
                NPC owl = Main.npc[owlIndex];
                owl.Center = spawnPosition;
                owl.target = target.whoAmI;
                owl.netUpdate = true;
            }

            SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.22f, Pitch = 0.38f }, spawnPosition);

            if (_fireOwlBombardmentSpawned >= _fireOwlBombardmentCount)
            {
                _fireOwlBombardmentActive = false;
                NPC.netUpdate = true;
            }
        }

        protected override void OnMeleeComboAttackTick(
            MeleeCombo combo, MeleeComboStep step, int elapsed, int total)
        {
            int telegraphTicks = Math.Max(MinComboTelegraphTicks,
                (int)(step.TelegraphTicks * ComboTelegraphMultiplier));
            EmitFireColumnBladeCharge(combo, step, telegraphTicks + elapsed);

            if (combo.Name == FireOwlBombardmentName)
                return;

            float bladeReach = ComboReachBase * 0.7f * step.ReachMult;

            // Landing-timed leap slams arm the shader and spawn their crescent from
            // OnLandingTimedLeapSlamSwingTick instead (below), same as Gwyn/Artorias's own leap
            // landings. That hook fires after UpdateLeapSlamPose has refreshed this tick's weapon
            // rotation; this method runs before it, so anything positioned here reads last tick's
            // rotation - a full tick behind during the fast, non-linear downswing, which is why the
            // shader and axe head visibly disagreed.
            if (step.Motion != ComboMotion.LeapSlam)
            {
                ArmFireSlashVFX(bladeReach, elapsed / (float)Math.Max(1, total - 1));

                if (Main.netMode != NetmodeID.MultiplayerClient && elapsed == 0)
                {
                    SpawnAxeSwingCrescent(step, total, bladeReach);
                }
            }

            if (elapsed != total / 2)
                return;

            if (combo.Name == GreatfireCrescentName)
                SpawnGreatfireCrescent();
        }

        // Matches Gwyn's / Artorias's own landing-timed-leap sword arcs: this fires once
        // UpdateLeapSlamPose has updated _weaponRotation for the current tick, so the shader and
        // crescent both track the live, mid-air-predicted blade pose instead of last tick's.
        protected override void OnLandingTimedLeapSlamSwingTick(MeleeComboStep step, float progress)
        {
            base.OnLandingTimedLeapSlamSwingTick(step, progress);

            float bladeReach = ComboReachBase * 0.7f * step.ReachMult;
            ArmFireSlashVFX(bladeReach, progress);

            if (_landingAxeCrescentSpawned || Main.netMode == NetmodeID.MultiplayerClient)
                return;

            _landingAxeCrescentSpawned = true;
            // The downswing can begin a few ticks before touchdown (predicted landing), so the arc
            // only needs to cover what's left of it. 5-tick floor keeps a same-frame landing visible.
            int remainingSwingTicks = Math.Max(5,
                (int)Math.Ceiling((1f - MathHelper.Clamp(progress, 0f, 1f)) * LeapSlamCrescentTicks));
            SpawnAxeSwingCrescent(step, remainingSwingTicks, bladeReach);
        }

        // Matches PuppetNPC's own private LeapSlamDownswingTicks (10) - the window the crescent's
        // remaining-duration estimate above is scaled against.
        private const int LeapSlamCrescentTicks = 10;

        private void SpawnAxeSwingCrescent(MeleeComboStep step, int duration, float bladeReach)
        {
            bool reverse = step.Motion == ComboMotion.UnderhandArc
                || step.Motion == ComboMotion.RisingUppercutLeap;
            float sweep = (reverse ? -1f : 1f) * NPC.direction * MathHelper.Pi;
            VanillaSwordArcSettings settings = new VanillaSwordArcSettings
            {
                Texture = VanillaSwordArcTexture.NightsEdge,
                Easing = VanillaSwordArcEasing.Linear,
                Duration = Math.Max(1, duration),
                StartAngle = PuppetWeaponDirection.ToRotation(),
                SweepAngle = sweep,
                Radius = Math.Max(56f, bladeReach * 1.04f),
                Opacity = 0.58f,
                FadeInFraction = Math.Min(0.14f, 2f / Math.Max(1, duration)),
                FadeOutFraction = 0.24f,
                AfterimageLag = MathHelper.PiOver4,
                AfterimageOpacity = 0.46f,
                BodyOpacity = 0.74f,
                CoreOpacity = 0.22f,
                DrawTipSparkle = false,
                TintWithWorldLighting = true,
                DarkColor = new Color(74, 13, 2),
                BodyColor = new Color(232, 79, 11),
                CoreColor = new Color(255, 194, 56),
                // This is deliberately a plain, readable sprite pass. Owl's existing FireSlashArc
                // shader continues separately in PostDraw, without the sprite acting as its mask.
                DrawCinderOverlay = false,
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
            _landingAxeCrescentSpawned = false;
        }

        private void EmitFireColumnBladeCharge(
            MeleeCombo combo, MeleeComboStep step, int comboTimelineTick)
        {
            if (Main.dedServ || !HasFireColumnFollowUp(combo.Name, step.Motion))
                return;

            int telegraphTicks = Math.Max(MinComboTelegraphTicks,
                (int)(step.TelegraphTicks * ComboTelegraphMultiplier));
            int releaseTimelineTick;

            if (step.Motion == ComboMotion.LeapSlam)
            {
                // Landing-timed slams do not have a fixed impact frame. Predict their flat-ground
                // airtime from the same launch velocity and 0.3px/tick gravity used by PuppetNPC,
                // placing the 60-tick tell across the latter part of the jump. Raised terrain can
                // shorten the final few ticks naturally because the real landing remains authoritative.
                float heightMult = step.LeapHeightMult > 0f ? step.LeapHeightMult : 1f;
                float launchSpeed = (LeapAttackUpSpeed + 1.8f) * heightMult;
                int expectedAirTicks = (int)Math.Ceiling(2f * launchSpeed / 0.3f);
                releaseTimelineTick = telegraphTicks + expectedAirTicks;
            }
            else
            {
                float swingSpeed = step.SwingSpeedMult > 0f ? step.SwingSpeedMult : 1f;
                int attackTicks = Math.Max(6, (int)Math.Round(step.AttackTicks / swingSpeed));
                releaseTimelineTick = telegraphTicks + (combo.Name == GreatfireCrescentName
                    ? attackTicks / 2
                    : attackTicks);
            }

            int chargeStartTick = Math.Max(0, releaseTimelineTick - FireColumnBladeChargeTicks);
            int chargeElapsed = comboTimelineTick - chargeStartTick;
            if (chargeElapsed < 0 || chargeElapsed >= FireColumnBladeChargeTicks)
                return;

            float progress = (chargeElapsed + 1f) / FireColumnBladeChargeTicks;
            Vector2 weaponDirection = PuppetWeaponDirection.SafeNormalize(
                new Vector2(NPC.direction, 0f));
            Vector2 bladeNormal = new Vector2(-weaponDirection.Y, weaponDirection.X);
            float bladeReach = ComboReachBase * 0.7f * step.ReachMult;
            // Doubled from the original 1/+1/+1 progression: this charge-up only ever runs for
            // swings gated into HasFireColumnFollowUp below, so making it denser reads as a clearer
            // "this one ends in a fire burst" cue without also lighting up the plain swings that
            // don't — those get none, keeping the two readable apart.
            int emberCount = 2;
            if (progress >= 0.4f && Main.rand.NextBool(2))
                emberCount += 2;
            if (progress >= 0.75f)
                emberCount += 2;

            for (int i = 0; i < emberCount; i++)
            {
                // Restrict the emitter to the outer 35% of the weapon so it reads as an axe-head
                // charge rather than a body aura or a line running down the handle.
                Vector2 position = PuppetHandPosition
                    + weaponDirection * bladeReach * Main.rand.NextFloat(0.65f, 1.0f)
                    + bladeNormal * Main.rand.NextFloat(-5f, 5f) * (0.65f + progress * 0.35f);
                float scale = Main.rand.NextFloat(0.5f, 0.72f)
                    + progress * Main.rand.NextFloat(0.65f, 1.05f);
                Vector2 velocity = NPC.velocity * 0.12f
                    + bladeNormal * Main.rand.NextFloat(-0.35f, 0.35f)
                    + new Vector2(Main.rand.NextFloat(-0.35f, 0.35f),
                        Main.rand.NextFloat(-1.55f, -0.55f));
                Dust ember = Dust.NewDustPerfect(
                    position,
                    Main.rand.NextBool(3) ? DustID.GoldFlame : DustID.OrangeTorch,
                    velocity,
                    55,
                    new Color(255, 174, 45),
                    scale);
                ember.noGravity = true;
                ember.fadeIn = scale + MathHelper.Lerp(0.25f, 0.7f, progress);
            }

            Vector2 bladeGlowCenter = PuppetHandPosition + weaponDirection * bladeReach * 0.82f;
            Lighting.AddLight(bladeGlowCenter,
                new Vector3(1f, 0.34f, 0.04f) * MathHelper.Lerp(0.18f, 0.75f, progress));
        }

        private static bool HasFireColumnFollowUp(string comboName, ComboMotion motion)
        {
            if (motion == ComboMotion.LeapSlam)
                return comboName == HighLeapingSlamName || comboName == HighLeapFollowUpName;

            return comboName == GreatfireCrescentName || comboName == GreatfireBreakerName;
        }

        private void SpawnGreatfireCrescent()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            int direction = NPC.direction < 0 ? -1 : 1;
            float startX = NPC.Bottom.X + direction * 22f;
            if (!PuppetGroundDustWave.TryFindGroundY(startX, NPC.Bottom.Y, out float groundY))
                return;

            Vector2 origin = new Vector2(startX, groundY - 26f);
            Projectile.NewProjectile(
                NPC.GetSource_FromThis(),
                origin,
                new Vector2(direction * 9.5f, 0f),
                ModContent.ProjectileType<PuppetGreatfireCrescent>(),
                22,
                3f,
                Main.myPlayer,
                PuppetGreatfireCrescent.GroundMode);
            SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.62f, Pitch = 0.10f }, origin);
        }

        private void SpawnGreatfireBreakerWaves()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            int projectileType = ModContent.ProjectileType<PuppetGreatfireCrescent>();
            for (int direction = -1; direction <= 1; direction += 2)
            {
                float startX = NPC.Bottom.X + direction * 14f;
                if (!PuppetGroundDustWave.TryFindGroundY(startX, NPC.Bottom.Y, out float groundY))
                    continue;

                Projectile.NewProjectile(
                    NPC.GetSource_FromThis(),
                    new Vector2(startX, groundY - 26f),
                    new Vector2(direction * 7f, 0f),
                    projectileType,
                    24,
                    4f,
                    Main.myPlayer,
                    PuppetGreatfireCrescent.GroundMode);
            }
        }

        private void SpawnHighLeapFireColumns()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            for (int direction = -1; direction <= 1; direction += 2)
            {
                for (int column = 0; column < 3; column++)
                {
                    float x = NPC.Bottom.X + direction * (24f + column * 48f);
                    // Use the same exposed-surface lookup as Owl Father's traveling ground fire.
                    // Each farther pair waits six more ticks, making the eruption visibly move
                    // outward from the impact instead of drawing six simultaneous static flames.
                    if (!PuppetGroundDustWave.TryFindGroundY(x, NPC.Bottom.Y, out float groundY))
                        continue;

                    Projectile.NewProjectile(NPC.GetSource_FromThis(),
                        new Vector2(x, groundY), Vector2.Zero,
                        ModContent.ProjectileType<PuppetFireWaveColumn>(), 22, 3f, Main.myPlayer,
                        96f, direction * 6f, PuppetFireWaveColumn.EncodeSlamDelay(column * 6));
                }
            }
        }

        protected override void OnLeapSlamLanded(MeleeComboStep step)
        {
            if (ActiveMeleeComboName == GreatfirePursuitSlamName)
            {
                SpawnPursuitSlamImpact();
                return;
            }

            // Only the signature high variants earn the six-column eruption. The low pursuit slam
            // is a mobility tool and deliberately keeps its landing readable and compact.
            if (ActiveMeleeComboName == HighLeapingSlamName
                || ActiveMeleeComboName == HighLeapFollowUpName)
                SpawnHighLeapFireColumns();
        }

        private void SpawnPursuitSlamImpact()
        {
            if (Main.dedServ)
                return;

            Vector2 impact = NPC.Bottom - Vector2.UnitY * 4f;
            for (int i = 0; i < 34; i++)
            {
                float outward = Main.rand.NextFloat(-4.8f, 4.8f);
                Dust dust = Dust.NewDustPerfect(
                    impact + new Vector2(Main.rand.NextFloat(-14f, 14f), Main.rand.NextFloat(-5f, 2f)),
                    Main.rand.NextBool(4) ? DustID.GoldFlame : DustID.OrangeTorch,
                    new Vector2(outward, Main.rand.NextFloat(-4.5f, -1.2f)),
                    45,
                    new Color(255, 136, 28),
                    Main.rand.NextFloat(0.85f, 1.55f));
                dust.noGravity = true;
                dust.fadeIn = Main.rand.NextFloat(0.8f, 1.25f);
            }

            Lighting.AddLight(impact, new Vector3(1f, 0.32f, 0.04f) * 1.15f);
            UsefulFunctions.ScreenShake(impact, 2.5f, 10, distanceFalloff: 500f);
            SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.42f, Pitch = -0.18f }, impact);
        }

        private void SpawnFirefallArray()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient || !NPC.HasValidTarget)
                return;

            Player target = Main.player[NPC.target];
            float predictedX = target.Center.X + target.velocity.X * 18f;
            float[] offsets = { 0f, 72f, -72f };
            int[] delays = { 58, 72, 86 };
            int projectileType = ModContent.ProjectileType<PuppetFirefallPillar>();

            for (int i = 0; i < offsets.Length; i++)
            {
                float x = predictedX + offsets[i];
                float groundY = PuppetGroundDustWave.FindGroundY(x, target.Bottom.Y);
                Projectile.NewProjectile(
                    NPC.GetSource_FromThis(),
                    new Vector2(x, groundY - 2f),
                    Vector2.Zero,
                    projectileType,
                    26,
                    3f,
                    Main.myPlayer,
                    delays[i],
                    0f,
                    PuppetFirefallPillar.DreadWraithFireVisualStyle);
            }

            SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.52f, Pitch = -0.22f }, target.Center);
        }

        protected override bool ShouldContinueMeleeCombo(
            string comboName, int nextStepIndex, Player target, bool previousStepHit)
        {
            if (comboName == HighLeapFollowUpName && nextStepIndex == 1)
            {
                // The rising cut is a landing conversion, not a disconnected swing at empty space.
                return previousStepHit || NPC.Distance(target.Center) <= 140f;
            }

            if (comboName == DownUpReversalName && nextStepIndex == 1)
            {
                // A landed overhead always earns the follow-up. A whiffed one only earns it if the
                // underhand's own reach, plus the ground its forward push actually covers, can still
                // reach the player from here — not a random continuation.
                return previousStepHit
                    || WouldForwardPushedSwingConnect(DownUpReversalFollowUpStep, target);
            }

            return base.ShouldContinueMeleeCombo(comboName, nextStepIndex, target, previousStepHit);
        }

        /// <summary>Whether a step's own reach, plus the ground its ForwardPushMult would cover over
        /// its AttackTicks, reaches the target from the puppet's current position. Used to gate a
        /// follow-up swing after a whiff so it is not a guaranteed second miss.</summary>
        private bool WouldForwardPushedSwingConnect(MeleeComboStep step, Player target)
        {
            float reach = ComboReachBase * 0.7f * step.ReachMult;
            float pushDistance = step.ForwardPushMult > 0f
                ? ComboForwardPushTopSpeed * step.ForwardPushMult * step.AttackTicks
                : 0f;
            return NPC.Distance(target.Center) <= reach + pushDistance;
        }

        protected override void OnComboStepCompleted(MeleeComboStep step)
        {
            bool breakerImpact = ActiveMeleeComboName == GreatfireBreakerName
                && step.Motion == ComboMotion.OverheadArc;
            bool landingImpact = step.Motion == ComboMotion.LeapSlam && NPC.velocity.Y == 0f;
            if (!breakerImpact && !landingImpact)
                return;

            if (breakerImpact)
                SpawnGreatfireBreakerWaves();

            UsefulFunctions.ScreenShake(
                NPC.Bottom,
                breakerImpact ? 3.75f : 3.25f,
                breakerImpact ? 11 : 9,
                distanceFalloff: 560f);

            // LeapSlam (the High/Pursuit/Closing leap combos) gets its own heavier impact; the
            // Greatfire Breaker's ground slam keeps the vanilla thud.
            if (step.Motion == ComboMotion.LeapSlam)
            {
                SoundEngine.PlaySound(
                    new SoundStyle("tsorcRevamp/Sounds/HollowKnight/false_knight_land_1st_time")
                        with { Volume = 0.42f, Pitch = 0.18f },
                    NPC.Bottom);
            }
            else
            {
                SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.42f, Pitch = 0.18f }, NPC.Bottom);
            }

            for (int i = 0; i < 18; i++)
            {
                Vector2 velocity = new Vector2(
                    Main.rand.NextFloat(-4.2f, 4.2f),
                    Main.rand.NextFloat(-4.8f, -0.7f));
                Dust ember = Dust.NewDustPerfect(
                    NPC.Bottom + new Vector2(Main.rand.NextFloat(-18f, 18f), -2f),
                    DustID.Torch,
                    velocity,
                    80,
                    default,
                    Main.rand.NextFloat(1.0f, 1.55f));
                ember.noGravity = true;
            }
        }

        // The shared PuppetNPC death burst is 150 Blood dusts. Against Owl Father's dark arena
        // that reads as almost nothing, especially when the 2x spectral body disappears on the
        // same frame. Give this boss a brighter, layered burst and keep OnKill as a fallback for
        // death paths that do not deliver a final client-side HitEffect.
        private bool _deathBurstPlayed;

        public override void HitEffect(NPC.HitInfo hit)
        {
            if (NPC.life <= 0)
                SpawnOwlFatherDeathBurst();
        }

        public override void OnKill()
        {
            base.OnKill();
            SpawnOwlFatherDeathBurst();
        }

        private void SpawnOwlFatherDeathBurst()
        {
            if (_deathBurstPlayed || Main.dedServ)
                return;

            _deathBurstPlayed = true;
            Vector2 center = NPC.Center;
            Vector2 inheritedVelocity = NPC.velocity * 0.15f;

            // Ninety gravity-affected blood particles are distributed from the top of the body to
            // the feet, alternating left and right so the whole disappearing silhouette ruptures.
            for (int i = 0; i < 90; i++)
            {
                float verticalProgress = (i + Main.rand.NextFloat()) / 90f;
                float side = i % 2 == 0 ? -1f : 1f;
                Vector2 position = new Vector2(
                    center.X + Main.rand.NextFloat(-NPC.width * 0.22f, NPC.width * 0.22f),
                    MathHelper.Lerp(NPC.Top.Y, NPC.Bottom.Y, verticalProgress));
                Dust blood = Dust.NewDustPerfect(
                    position,
                    DustID.Blood,
                    new Vector2(side * Main.rand.NextFloat(2.5f, 6f), Main.rand.NextFloat(-1.8f, 1.8f))
                        + inheritedVelocity,
                    20,
                    default,
                    Main.rand.NextFloat(1.45f, 2.55f));
                blood.noGravity = false;
            }

            // The 120 gold embers launch in every direction at exactly twice the blood speed range.
            // They retain gravity so the wide initial starburst bends back down around the corpse.
            for (int i = 0; i < 120; i++)
            {
                Vector2 direction = Main.rand.NextVector2CircularEdge(1f, 1f);
                float matchingBloodSpeed = Main.rand.NextFloat(2.5f, 6f);
                Dust ember = Dust.NewDustPerfect(
                    center + Main.rand.NextVector2Circular(NPC.width * 0.4f, NPC.height * 0.4f),
                    DustID.GoldFlame,
                    direction * matchingBloodSpeed * 2f + inheritedVelocity,
                    35,
                    new Color(255, 210, 55),
                    Main.rand.NextFloat(1.05f, 2.05f));
                ember.noGravity = false;
            }

            Lighting.AddLight(center, new Vector3(1f, 0.52f, 0.12f) * 1.4f);
        }

    }
}

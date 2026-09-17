using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items.Weapons.Enemy;
using tsorcRevamp.NPCs.AI;
using tsorcRevamp.NPCs.Puppets;
using tsorcRevamp.Projectiles.Enemy.OolacileCultist;
using tsorcRevamp.Projectiles.VFX;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.NPCs.Enemies
{
    // PLACEHOLDER sprite (copy of ClericOfSorrow.png) — the puppet body is drawn by PuppetNPC, this sheet is
    // only the NPC's own required texture; replace with bespoke bestiary art.
    //
    // A regular Crimson enemy (not an invader) on the puppet system: Brain of Cthulhu Mask + Solar Cultist Robe.
    //   Above 50% HP — a slow pyromancer that kites 8–20 tiles and keeps walking while it casts four fire
    //     formations (Kahlrun's Farron Dart patterns turned to fire) from an Orange Gem Staff, plus a
    //     point-blank Recoil Flare so hugging it is not a safe spot.
    //   At 50% HP — "Unbinding": the staff burns away raised overhead and throws five fire orbs in a 180° fan.
    //   Below 50% HP — melee only with bladed-glove claws at 3x speed: Rake, Twin Rake, Pounce, Bloodletting
    //     Flurry. Every claw hit applies 15s of Bleeding.
    // Kit split (enemy-redesign G0.7): the formations run through the base MAGIC phase; Recoil Flare and the
    // Unbinding set-piece run on AttackPhase.Custom; the claws are a legacy MeleeCombo table (Weighted ease).
    public class OolacileCultist : PuppetNPC, IHumanoidMeleeHitEffects
    {
        protected override bool AnnounceInvasion => false;
        // A regular enemy: no party-wipe despawn and no "melts away" broadcast when the player dies.
        protected override bool DespawnsOnPartyWipe => false;
        protected override string InvaderTitle => "Oolacile Cultist";

        #region Tuning constants
        // Speeds (px/tick). Frenzy is 3x the caster's walk and outruns an unbuffed player on purpose.
        private const float CasterTopSpeed = 1.1f;
        private const float FrenzyTopSpeed = 3.3f;
        private const float CasterAcceleration = 0.05f;
        private const float FrenzyAcceleration = 0.15f;

        // Kite band in TILES while a caster.
        private const float KiteMinTiles = 8f;
        private const float KiteMaxTiles = 20f;

        // Magic cast. The last 20 ticks of the 60-tick tell are hyper-armored; the flash + chime mark that flip.
        private const int CastTelegraphTicks = 60;
        private const int CastCommitTicks = 20;
        // Staff tip distance from the hand: OrangeGemStaff grip (12,27) to gem (38,3) ≈ 35px, × 0.9 draw scale.
        private const float StaffTipReach = 32f;
        private const float StaffDrawScale = 0.9f;

        private const float FlameBoltSpeed = 6f;
        private const float FlameFanSpeed = 5.5f;
        private const float FlameFanSpreadDegrees = 12f;
        private const float EmberRainSpeed = 7f;
        private const float EmberRainHeight = 250f;
        private const float EmberRainSpacing = 46f;
        private const int EmberChainIntervalTicks = 12;
        private const int AimLeadTicks = 9;

        // Recoil Flare: 18-tick aimed tell, release on tick 18, 8 ticks of follow-through.
        private const float RecoilFlareTriggerRange = 56f;
        private const int RecoilFlareTellTicks = 18;
        private const int RecoilFlareTotalTicks = 26;
        private const int RecoilFlareCooldownTicks = 150;
        private const float RecoilFlareSpreadDegrees = 15f;
        private const float RecoilHopBackSpeed = 4f;
        private const float RecoilHopUpSpeed = 4f;

        // Unbinding: raise 40 → burst 10 (empty hands) → draw claws 30. First claw tell is possible at tick 80.
        private const int UnbindRaiseTicks = 40;
        private const int UnbindBurstTicks = 10;
        private const int UnbindDrawTicks = 30;
        private const float UnbindOrbSpeed = 6f;
        private const float StaffRaisedStraightUp = -MathHelper.PiOver2;

        // Claw reach. BeastClaw grip (10,16) to blade tip (31,0) ≈ 26px at scale 1 (front claw drawn at 0.5 ≈ 13px);
        // hitbox = ComboReachBase 44 × 0.7 × ReachMult 1.1 ≈ 34px. The back claw draws at 0.5 (it sits behind the body).
        private const float OffHandClawDrawScale = 0.5f;
        private const float ClawReachBase = 44f;
        private const float ClawReachMult = 1.1f;
        private const float PounceMinRange = 80f;
        private const float PounceMaxRange = 160f;
        private const float PounceLandingOvershoot = 24f;
        private const float PounceLeapHeightMult = 0.65f;
        private const float PounceStrikeRange = 110f;
        private const float ComboPursuitStopDistance = 24f;

        // Claw rake poses: 170° envelope (~136° live). Deliberately under the 180° sword default — a claw
        // rakes short and fast. Overhead and underhand share these endpoints so chains never snap.
        private const float RakeHighPose = -1.55f;
        private const float RakeLowPose = 1.42f;

        private const int BleedingDebuffTicks = 15 * 60;
        private const int HeartDropCount = 2;
        private const int ManaStarDropCount = 2;
        private const int DeathCloudCount = 15;

        private const string RakeName = "Rake";
        private const string TwinRakeName = "Twin Rake";
        private const string PounceName = "Pounce";
        private const string FlurryName = "Bloodletting Flurry";
        #endregion

        private enum CustomKind : byte { None, RecoilFlare, UnbindRaise, UnbindBurst, UnbindDraw }
        private enum Formation : byte { FlameBolt, FlameFan, EmberChain, EmberRain }

        private bool _frenzyTriggered;   // the 50% transition has started (one-shot)
        private bool _staffBurned;       // the staff is gone: weapon + speed + kite band all switch to the frenzy set
        private CustomKind _customKind = CustomKind.None;
        private int _recoilFlareCooldown;

        // Server-only formation bag: deal all four before refilling, never the same one twice in a row.
        private readonly List<Formation> _formationBag = new List<Formation>();
        private Formation _lastFormation = Formation.EmberRain;

        #region Puppet loadout
        protected override int HeadArmorItemType => ItemID.BrainMask;
        protected override int BodyArmorItemType => ItemID.WhiteLunaticRobe;  // Solar Cultist Robe, bodySlot 180
        // No leg armor: the Solar Cultist Robe is a robe body, so it should draw its own floor-length skirt.
        // (Cenx's Dress Pants rendered pink.) 0 = empty slot, SetDefaults(0) gives an air item.
        protected override int LegsArmorItemType => 0;

        protected override int MeleeWeaponItemType => _staffBurned ? ModContent.ItemType<EnemyBeastClaw>() : -1;
        protected override int RangedWeaponItemType => -1;
        protected override int MagicWeaponItemType => _staffBurned ? -1 : ModContent.ItemType<OolacileCultistStaff>();

        // Hostile hitboxes and projectiles deal 2x in game.
        protected override int MeleeDamage
        {
            get
            {
                if (tsorcRevampWorld.SuperHardMode)
                {
                    return 25;
                }
                if (Main.hardMode)
                {
                    return 16;
                }
                return 11;
            }
        }

        protected override int MagicDamage
        {
            get
            {
                if (tsorcRevampWorld.SuperHardMode)
                {
                    return 23;
                }
                if (Main.hardMode)
                {
                    return 18;
                }
                return 10;
            }
        }

        protected override int RangedDamage => 0;
        protected override int EstusChargesMax => 0;

        protected override float TopSpeed => _staffBurned ? FrenzyTopSpeed : CasterTopSpeed;
        protected override float Acceleration => _staffBurned ? FrenzyAcceleration : CasterAcceleration;
        #endregion

        #region Magic (caster phase)
        protected override float MagicRange => KiteMaxTiles * 16f;
        protected override float MinMagicRange => 0f;
        protected override int MagicTelegraphTicks => CastTelegraphTicks;
        protected override int MagicAttackTicks => 14;
        protected override int MagicRecoveryTicks => 30;
        protected override int MagicCooldownAfterUse => 110;
        protected override Color MagicTelegraphFlashColor => new Color(255, 110, 40);
        protected override int MagicTelegraphFlashLeadTicks => CastCommitTicks;
        protected override bool BrakeDuringMagicCast => false;
        protected override bool UseAuthoredMagicCastPose => true;
        protected override float MagicCastStartRotation => 0.3f;
        protected override float MagicCastEndRotation => -0.9f;
        protected override float MagicWeaponRotationOffset => MathHelper.PiOver4;
        protected override int MagicWeaponRecoveryHoldTicks => 20;
        protected override Vector2 MagicGripNorm => new Vector2(0.29f, 0.68f);
        protected override float GetHeldRangedDrawScale(int itemType)
        {
            if (itemType == ModContent.ItemType<OolacileCultistStaff>())
            {
                return StaffDrawScale;
            }
            return base.GetHeldRangedDrawScale(itemType);
        }

        protected override bool UseCompositeArmForAdditionalPhase =>
            Phase == AttackPhase.MagicTelegraph
            || Phase == AttackPhase.MagicAttack
            || IsHoldingMagicWeaponDuringRecovery
            || Phase == AttackPhase.Custom;
        #endregion

        #region Claws (frenzy phase)
        protected override WeaponArchetype MeleeArchetype => WeaponArchetype.Dagger;
        // Grip ~7px in from the claw's bottom-left hilt corner: texel (5,25) of the 32x30 BeastClaw, inside the red cuff.
        protected override Vector2 MeleeHandleNorm => new Vector2(0.16f, 0.83f);
        protected override float MeleeWeaponDrawScale => 0.5f;
        protected override float MeleeBladeWidth => 24f;
        protected override float MeleeRange => 48f;
        protected override float StabRange => 120f;
        protected override float ComboReachBase => ClawReachBase;
        protected override float MeleeEngageRange => 60f;
        protected override float ComboMaxStartRange => 180f;
        protected override float RangedStartComboMaxRange => PounceMaxRange + 10f;
        protected override float ClosingDistanceSpeedMult => 1.2f;
        protected override int ClosingDistanceMaxTicks => 90;
        protected override int MeleeComboChance => 100;
        protected override int RangedStartMeleeComboChance => 100;
        protected override float ComboTelegraphMultiplier => 1f;
        protected override int MinComboTelegraphTicks => 18;
        protected override int MeleeComboInterStepLingerTicks => 3;
        protected override float ComboTelegraphAdvanceSpeedMult => 0.5f;
        protected override float ComboTelegraphAdvanceStopDistance => 36f;
        // No melee brake: tells, swings and pauses keep the navigator chasing at full frenzy speed (3.3 px/t),
        // so walking away never escapes a claw attack — the answer is a roll or a jump.
        protected override bool SlowDownBeforeMelee => false;
        protected override bool UseSwingEasing => true;
        protected override bool UseAuthoredComboSwingClock => true;
        protected override bool UseLogicalMeleeTelegraphs => true;
        protected override bool UseCompositeArmSwing => true;
        protected override bool MirrorMeleeSwingRotationByFacing => true;
        // Slashes use VanillaSwordArc (spawned per swing in OnMeleeComboAttackTick), not the legacy Slash.png sprite.
        protected override bool HasSlashVFX => false;


        // Both claws rest 45° lower than the base sword carry (-0.30): clockwise facing right, counter-clockwise
        // facing left (weapon space mirrors with facing). The logical telegraph settles from this same pose.
        private const float ClawCarryRotation = -0.30f + MathHelper.PiOver4;
        protected override float MeleeCarryRotation => ClawCarryRotation;
        protected override float OffHandCarryRotation => ClawCarryRotation;

        // Scope the landing-timed leap and its poses to Pounce; ActiveMeleeComboName is never cleared.
        // True through every phase of a claw combo (used for the mid-combo pursuit range).
        private bool IsSwingingClaws =>
            Phase == AttackPhase.MeleeComboTelegraph
            || Phase == AttackPhase.MeleeComboAttack
            || Phase == AttackPhase.MeleeComboPause
            || Phase == AttackPhase.MeleeComboRecovery;

        private bool PounceActive => ActiveMeleeComboName == PounceName
            && (Phase == AttackPhase.MeleeComboTelegraph
                || Phase == AttackPhase.MeleeComboAttack
                || Phase == AttackPhase.MeleeComboPause);

        protected override bool UseLandingTimedLeapSlam => PounceActive;
        // Pounce accuracy: aim where the player is heading, allow a bounded correction while rising, and let the
        // claws swing in the air once they are within PounceStrikeRange instead of waiting for the landing.
        protected override float LeapAttackTargetLeadTicks => 12f;
        protected override float LeapAttackAscentTrackingStrength => 0.12f;

        protected override float LeapSlamCarryRotation => PounceActive ? RakeHighPose : base.LeapSlamCarryRotation;
        protected override float LeapSlamImpactRotation => PounceActive ? RakeLowPose : base.LeapSlamImpactRotation;
        // Negative standoff = aim past the player, so an on-time pounce lands through them instead of short.
        protected override float LeapLandingStandoff => -PounceLandingOvershoot;

        private static MeleeComboStep ClawStep(
            ComboMotion motion,
            int telegraphTicks,
            int easeInTicks,
            int easeOutTicks,
            float easeOutDecay,
            int pauseAfter = 0,
            float damageMult = 1f,
            ComboHand hand = ComboHand.Front)
        {
            // Armed while angular speed >= 30% of peak: ease-in + out·ln(1/0.3)/k ticks.
            int attackTicks = easeInTicks + easeOutTicks;
            float liveTailTicks = easeOutTicks * MathF.Log(1f / 0.3f) / easeOutDecay;
            float hitWindowEnd = MathHelper.Clamp((easeInTicks + liveTailTicks) / attackTicks, 0f, 1f);

            return new MeleeComboStep
            {
                Motion = motion,
                Hand = hand,
                TelegraphTicks = telegraphTicks,
                AttackTicks = attackTicks,
                PostStepPause = pauseAfter,
                DamageMult = damageMult,
                ReachMult = ClawReachMult,
                ForwardPushMult = 0f, // no push: the navigator keeps chasing through the swing (SlowDownBeforeMelee off)
                SwingSpeedMult = 1f,
                Ease = SwingEaseStyle.Weighted,
                EaseInTicks = easeInTicks,
                EaseOutTicks = easeOutTicks,
                EaseOutDecay = easeOutDecay,
                HitWindowEnd = hitWindowEnd,
                LeapHeightMult = 1f,
                LeapForwardSpeedMult = 1f,
            };
        }

        // Timing sheet (60 ticks = 1s). Player budget: 22t roll i-frames, next roll 30t after the last STARTED,
        // 40t post-hit immunity.
        //
        // Move           Tell  Strike (in/out, k)  Live   Tail+Recovery        Notes
        // Rake            22    4 / 22, k7         ~8t    19 + 20 = 39t punish  rollable (8 < 22); peak ~33°/t vs tell 26°/t
        // Twin Rake       22    4 / 22, k7 ×2      ~8t    19 + 24              back claw, then front. pause 8. Step 2 live ~26t after
        //                                                                      step 1's live ends (< 30), so it is
        //                                                                      CONDITIONAL: only after a miss with the
        //                                                                      player still in front and in reach.
        // Pounce          28    leap, 10t swipe    swipe  36t landing punish   5–10 tiles, lands 24px past the player;
        //                                                                      aimed at release, so rolling away mid-
        //                                                                      air escapes. 0.65 height ≈ 41t aloft.
        // Flurry          30    4 / 16, k7 ×4      ~7t    13 + 45 recovery     starts 24t apart; standing still needs
        //                                                                      3–4 rolls. NO LONGER out-spaceable: with no
        //                                                                      melee brake it chases at 3.3 px/t vs player ~3.
        //                                                                      Post-hit immunity caps it at 2 hits.
        private static readonly MeleeCombo[] ClawCombos =
        {
            new MeleeCombo
            {
                Name = RakeName,
                BaseWeight = 100,
                Preferred = ComboRangeBand.Close,
                InitialFlashColor = new Color(210, 50, 60),
                CooldownAfterUse = 50,
                RecoveryTicks = 20,
                MoveBrake = 0.25f,
                Steps = new[]
                {
                    ClawStep(ComboMotion.OverheadArc, 22, 4, 22, 7f),
                },
            },
            new MeleeCombo
            {
                Name = TwinRakeName,
                BaseWeight = 80,
                Preferred = ComboRangeBand.Close,
                InitialFlashColor = new Color(235, 80, 80),
                CooldownAfterUse = 90,
                RecoveryTicks = 24,
                MoveBrake = 0.25f,
                Steps = new[]
                {
                    ClawStep(ComboMotion.OverheadArc, 22, 4, 22, 7f, pauseAfter: 8, damageMult: 0.9f, hand: ComboHand.Back),
                    ClawStep(ComboMotion.UnderhandArc, 0, 4, 22, 7f, damageMult: 0.9f),
                },
            },
            new MeleeCombo
            {
                Name = PounceName,
                BaseWeight = 70,
                Preferred = ComboRangeBand.Mid,
                InitialFlashColor = new Color(255, 40, 40),
                CooldownAfterUse = 200,
                RecoveryTicks = 36,
                RangedStartOnly = true,
                HyperArmor = true,
                MoveBrake = 0.35f,
                Steps = new[]
                {
                    // The 90-tick timeout is a safety net; the landing-timed leap ends on touchdown.
                    new MeleeComboStep
                    {
                        Motion = ComboMotion.LeapSlam,
                        Hand = ComboHand.Both, // both claws come down on the landing
                        TelegraphTicks = 28,
                        AttackTicks = 90,
                        PostStepPause = 0,
                        DamageMult = 1.15f,
                        ReachMult = ClawReachMult,
                        ForwardPushMult = 0f,
                        SwingSpeedMult = 1f,
                        Ease = SwingEaseStyle.Smooth,
                        HitWindowEnd = 1f,
                        LeapStrikeRange = PounceStrikeRange,
                        LeapHeightMult = PounceLeapHeightMult,
                        LeapForwardSpeedMult = 1f,
                    },
                },
            },
            new MeleeCombo
            {
                Name = FlurryName,
                BaseWeight = 45,
                Preferred = ComboRangeBand.Close,
                InitialFlashColor = new Color(140, 0, 20),
                CooldownAfterUse = 240,
                RecoveryTicks = 45,
                HeavyCommit = true,
                MoveBrake = 0.1f,
                Steps = new[]
                {
                    ClawStep(ComboMotion.OverheadArc, 30, 4, 16, 7f, pauseAfter: 4, damageMult: 0.8f),
                    ClawStep(ComboMotion.UnderhandArc, 0, 4, 16, 7f, pauseAfter: 4, damageMult: 0.8f, hand: ComboHand.Back),
                    ClawStep(ComboMotion.OverheadArc, 0, 4, 16, 7f, pauseAfter: 4, damageMult: 0.8f),
                    // Finisher: both claws rake up together.
                    ClawStep(ComboMotion.UnderhandArc, 0, 4, 16, 7f, damageMult: 1f, hand: ComboHand.Both),
                },
            },
        };

        protected override MeleeCombo[] MeleeComboPoolOverride => ClawCombos;

        // Hands: Rake alternates front/back each use (CustomizeMeleeCombo). Twin Rake = BACK then front, so the
        // back claw swings even when the conditional second rake is skipped. Flurry = front, back, front, then BOTH
        // as the finisher. Pounce = BOTH claws on the landing. Every hand switch has a PostStepPause >=
        // DualWieldHandBlendTicks (4).
        private PuppetWeapon CreateClawWeapon(float drawScale)
        {
            return new PuppetWeapon(
                ModContent.ItemType<EnemyBeastClaw>(),
                handleNorm: MeleeHandleNorm,
                drawScale: drawScale,
                bladeWidth: MeleeBladeWidth,
                archetype: MeleeArchetype);
        }

        // Server-only: the next Rake uses the back claw. The customized combo copy reaches clients in the snapshot.
        private bool _nextRakeUsesBackHand;

        protected override void CustomizeMeleeCombo(ref MeleeCombo combo, float healthFraction)
        {
            if (combo.Name != RakeName)
            {
                return;
            }

            if (_nextRakeUsesBackHand)
            {
                combo.Steps[0].Hand = ComboHand.Back;
            }
            _nextRakeUsesBackHand = !_nextRakeUsesBackHand;
        }
        #endregion

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 1;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.OnFire] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Confused] = true;
        }

        public override void SetDefaults()
        {
            NPC.width = 20;
            NPC.height = 42;
            NPC.aiStyle = -1;
            NPC.damage = 0; // puppets deal damage through their weapon hitboxes, never contact
            NPC.knockBackResist = 0.35f; // overridden by the PoiseProfiles entry
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.npcSlots = 2f;

            // Tier neighbours: Basilisk Walker / Shifter / Hunter — a bit more HP, a bit less defense, more value.
            if (tsorcRevampWorld.SuperHardMode)
            {
                NPC.lifeMax = 3000;
                NPC.defense = 70;
                NPC.value = 12500;
            }
            else if (Main.hardMode)
            {
                NPC.lifeMax = 450;
                NPC.defense = 45;
                NPC.value = 2250;
            }
            else
            {
                NPC.lifeMax = 200;
                NPC.defense = 5;
                NPC.value = 1000;
            }

            tsorcRevampGlobalNPC globalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            globalNPC.PoiseStaggerResetsAI = true;
            globalNPC.CanUseRopes = true;
        }

        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            Player player = spawnInfo.Player;

            if (player.townNPCs > 0f || !player.ZoneCrimson)
            {
                return 0f;
            }

            bool underground = player.ZoneDirtLayerHeight || player.ZoneRockLayerHeight;
            bool surfaceAtNight = player.ZoneOverworldHeight && !Main.dayTime;

            if (underground || surfaceAtNight)
            {
                return 0.05f;
            }

            return 0f;
        }

        protected override void RunMovementAI(float speedMult)
        {
            // Caster keeps a 8–20 tile band (backpedalling while facing the player); the frenzy drops the band
            // entirely so SmartFighter4 pursues straight in.
            tsorcRevampGlobalNPC globalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            globalNPC.NavSearchRadius = 50;
            globalNPC.RemembersLastKnownPos = true;

            if (_staffBurned)
            {
                globalNPC.KiteRangeMin = 0f;
                globalNPC.KiteRangeMax = 0f;
                globalNPC.CanWalkBackwards = false;
            }
            else
            {
                globalNPC.KiteRangeMin = KiteMinTiles;
                globalNPC.KiteRangeMax = KiteMaxTiles;
                globalNPC.KiteLooseness = 0.2f;
                globalNPC.CanWalkBackwards = true;
            }

            // Pursuit stop distance. Mid-combo it drops to near-contact so the navigator keeps walking INTO the
            // player through tells, swings and pauses — stopping at the normal engage range let a player just
            // walk backwards out of every claw swipe.
            float attackRange = MeleeEngageRange;
            if (!_staffBurned)
            {
                attackRange = MagicRange;
            }
            else if (IsSwingingClaws)
            {
                attackRange = ComboPursuitStopDistance;
            }

            SmartFighter4AI.Run(NPC,
                topSpeed: TopSpeed * speedMult,
                acceleration: Acceleration,
                doorBreakingDamage: 2,
                attackRange: attackRange);
        }

        public override void AI()
        {
            // Unbinding trigger. Deliberately NOT server-gated: NPC.life is synced, so every peer reaches this
            // on the same crossing (enemy-redesign G0.7 rule 3), which is tighter than waiting for a phase packet.
            bool belowHalf = NPC.life > 0 && NPC.life <= NPC.lifeMax / 2;
            if (belowHalf && !_frenzyTriggered)
            {
                _frenzyTriggered = true;
                _customKind = CustomKind.UnbindRaise;
                DebugAttackLabel = "Unbinding";
                StartCustomAttack(UnbindRaiseTicks, ModContent.ItemType<OolacileCultistStaff>());
                NPC.netUpdate = true;
            }

            // Claws in BOTH hands once the staff is gone. Equipped explicitly because the base caches the
            // front weapon from MeleeWeaponItemType on first read — which is -1 while this is still a caster.
            // Driven off the synced _staffBurned, so every peer equips on the same tick it learns of the burn.
            if (_staffBurned && FrontHandWeapon.IsEmpty)
            {
                EquipWeapon(PuppetHandSlot.Front, CreateClawWeapon(MeleeWeaponDrawScale));
                EquipWeapon(PuppetHandSlot.Back, CreateClawWeapon(OffHandClawDrawScale));
            }

            base.AI();

            // Safety net: if anything knocked the puppet out of the Unbinding chain before the staff burned
            // (a despawn-handler phase reset, a desync), finish the burn now instead of stranding a staff-less
            // caster that never frenzies.
            bool unbindingStranded = _frenzyTriggered && !_staffBurned && Phase != AttackPhase.Custom;
            if (unbindingStranded)
            {
                BurnStaff();
            }

            if (_recoilFlareCooldown > 0)
            {
                _recoilFlareCooldown--;
            }

            TryStartRecoilFlare();
            ApplyAttackArmor();
            SpawnAmbientEffects();
        }

        // Adds the hyper-armor windows the base phase lists don't know about. Runs after base.AI(), which has
        // just recomputed the flags for this tick, so these writes hold until the next tick's recompute.
        private void ApplyAttackArmor()
        {
            tsorcRevampGlobalNPC globalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            bool unbinding = Phase == AttackPhase.Custom
                && (_customKind == CustomKind.UnbindRaise
                    || _customKind == CustomKind.UnbindBurst
                    || _customKind == CustomKind.UnbindDraw);
            bool castCommitted = Phase == AttackPhase.MagicTelegraph && PhaseTimer <= CastCommitTicks;
            bool recoilTell = Phase == AttackPhase.Custom
                && _customKind == CustomKind.RecoilFlare
                && PhaseTimer > RecoilFlareTotalTicks - RecoilFlareTellTicks;
            bool recoilRelease = Phase == AttackPhase.Custom
                && _customKind == CustomKind.RecoilFlare
                && !recoilTell;

            if (unbinding || castCommitted || recoilRelease)
            {
                globalNPC.AttackCommitted = true;
                globalNPC.AttackTelegraphing = false;
            }
            else if (recoilTell)
            {
                globalNPC.AttackTelegraphing = true;
            }
        }

        private void TryStartRecoilFlare()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            bool free = Phase == AttackPhase.Idle || Phase == AttackPhase.CasualStroll;
            bool grounded = NPC.velocity.Y == 0f;
            if (_frenzyTriggered || !free || !grounded || _recoilFlareCooldown > 0 || !NPC.HasValidTarget)
            {
                return;
            }

            Player target = Main.player[NPC.target];
            float distance = NPC.Distance(target.Center);
            if (distance > RecoilFlareTriggerRange)
            {
                return;
            }

            int faceTarget = target.Center.X < NPC.Center.X ? -1 : 1;
            NPC.direction = faceTarget;
            NPC.spriteDirection = faceTarget;
            _recoilFlareCooldown = RecoilFlareCooldownTicks;
            _customKind = CustomKind.RecoilFlare;
            DebugAttackLabel = "Recoil Flare";
            StartCustomAttack(RecoilFlareTotalTicks, ModContent.ItemType<OolacileCultistStaff>());
            NPC.netUpdate = true;
        }

        // Recoil Flare's hop must keep its horizontal momentum, so only the Unbinding stages brake.
        protected override bool SlowDownDuringCustom => _customKind != CustomKind.RecoilFlare;

        protected override float? CustomWeaponRotation
        {
            get
            {
                if (_customKind == CustomKind.UnbindRaise)
                {
                    return StaffRaisedStraightUp;
                }

                if (_customKind == CustomKind.RecoilFlare && NPC.HasValidTarget)
                {
                    // Point the staff at the player. Magic-pose space: 0 = level forward, negative = up.
                    Player target = Main.player[NPC.target];
                    Vector2 toTarget = target.Center - NPC.Center;
                    float forwardX = Math.Max(1f, Math.Abs(toTarget.X));
                    float aimRotation = MathF.Atan2(toTarget.Y, forwardX);
                    return MathHelper.Clamp(aimRotation, -1.3f, 0.9f);
                }

                return null;
            }
        }

        protected override void DoCustomTick(int ticksRemaining)
        {
            switch (_customKind)
            {
                case CustomKind.RecoilFlare:
                    TickRecoilFlare(RecoilFlareTotalTicks - ticksRemaining);
                    break;

                case CustomKind.UnbindRaise:
                    TickUnbindRaise(UnbindRaiseTicks - ticksRemaining, ticksRemaining);
                    break;

                case CustomKind.UnbindBurst:
                    if (ticksRemaining == 1)
                    {
                        _customKind = CustomKind.UnbindDraw;
                        StartCustomAttack(UnbindDrawTicks, ModContent.ItemType<EnemyBeastClaw>());
                        if (!Main.dedServ)
                        {
                            SoundEngine.PlaySound(SoundID.Item71 with { Volume = 0.6f, Pitch = 0.3f }, NPC.Center);
                        }
                    }
                    break;

                case CustomKind.UnbindDraw:
                    if (!Main.dedServ && Main.rand.NextBool(3))
                    {
                        Dust blood = Dust.NewDustDirect(PuppetHandPosition - new Vector2(4f), 8, 8, DustID.Blood,
                            0f, 0f, 0, default, Main.rand.NextFloat(0.9f, 1.3f));
                        blood.velocity *= 0.4f;
                    }
                    break;
            }
        }

        private void TickRecoilFlare(int elapsed)
        {
            Vector2 tip = StaffTipPosition();

            if (elapsed < RecoilFlareTellTicks)
            {
                if (!Main.dedServ)
                {
                    // A bright flare gathering on the gem: tight, fast converge that thickens toward release.
                    float progress = elapsed / (float)RecoilFlareTellTicks;
                    int moteCount = 1 + (int)(progress * 3f);
                    for (int i = 0; i < moteCount; i++)
                    {
                        Vector2 offset = Main.rand.NextVector2CircularEdge(20f * (1f - progress) + 4f, 20f * (1f - progress) + 4f);
                        Dust mote = Dust.NewDustPerfect(tip + offset, DustID.Torch, -offset * 0.15f, 60, default,
                            MathHelper.Lerp(0.9f, 1.6f, progress));
                        mote.noGravity = true;
                    }
                    Lighting.AddLight(tip, 0.6f * progress + 0.2f, 0.25f * progress, 0.05f);

                    if (elapsed == 0)
                    {
                        SoundEngine.PlaySound(SoundID.Item34 with { Volume = 0.45f, Pitch = 0.3f }, NPC.Center);
                    }
                }
                return;
            }

            if (elapsed != RecoilFlareTellTicks)
            {
                return;
            }

            // Release: hop back while facing the player, and fire a 3-bolt cone from the tip.
            NPC.velocity.X = -NPC.direction * RecoilHopBackSpeed;
            NPC.velocity.Y = -RecoilHopUpSpeed;

            if (!Main.dedServ)
            {
                SoundEngine.PlaySound(SoundID.Item45 with { Volume = 0.7f, Pitch = 0.1f }, NPC.Center);
            }

            if (Main.netMode == NetmodeID.MultiplayerClient || !NPC.HasValidTarget)
            {
                return;
            }

            Player target = Main.player[NPC.target];
            Vector2 aim = (target.Center - tip).SafeNormalize(new Vector2(NPC.direction, 0f)) * FlameBoltSpeed;
            for (int i = -1; i <= 1; i++)
            {
                Vector2 velocity = aim.RotatedBy(MathHelper.ToRadians(i * RecoilFlareSpreadDegrees));
                SpawnFireBolt(tip, velocity, 0, OolacileFireBolt.ModeAimed);
            }
        }

        private void TickUnbindRaise(int elapsed, int ticksRemaining)
        {
            Vector2 tip = StaffTipPosition();

            if (!Main.dedServ)
            {
                // Orange and red dust climbing the shaft from the hand to the gem, denser as the burn spreads.
                float progress = elapsed / (float)UnbindRaiseTicks;
                int climbCount = 1 + (int)(progress * 4f);
                for (int i = 0; i < climbCount; i++)
                {
                    float alongShaft = Main.rand.NextFloat(0f, progress);
                    Vector2 shaftPoint = Vector2.Lerp(PuppetHandPosition, tip, alongShaft);
                    int dustType = Main.rand.NextBool() ? DustID.Torch : DustID.CrimsonTorch;
                    Dust ember = Dust.NewDustPerfect(shaftPoint + Main.rand.NextVector2Circular(3f, 3f), dustType,
                        new Vector2(Main.rand.NextFloat(-0.4f, 0.4f), Main.rand.NextFloat(-1.8f, -0.6f)), 70, default,
                        Main.rand.NextFloat(1f, 1.5f));
                    ember.noGravity = true;
                }

                Vector2 inward = Main.rand.NextVector2CircularEdge(40f, 40f);
                Dust converge = Dust.NewDustPerfect(tip + inward, DustID.Torch, -inward * 0.06f, 90, default, 1.1f);
                converge.noGravity = true;
                Lighting.AddLight(tip, 0.4f + 0.5f * progress, 0.15f + 0.2f * progress, 0.05f);

                if (elapsed == 0)
                {
                    SoundEngine.PlaySound(SoundID.Item34 with { Volume = 0.6f, Pitch = -0.4f }, NPC.Center);
                }
            }

            if (ticksRemaining == 1)
            {
                BurnStaff();
                _customKind = CustomKind.UnbindBurst;
                StartCustomAttack(UnbindBurstTicks);
            }
        }

        // The staff bursts from the raised tip: dust + five fire orbs (left, up-left, up, up-right, right).
        private void BurnStaff()
        {
            if (_staffBurned)
            {
                return;
            }

            Vector2 tip = StaffTipPosition();
            _staffBurned = true;
            NPC.netUpdate = true;

            if (!Main.dedServ)
            {
                SoundEngine.PlaySound(SoundID.Item74 with { Volume = 0.8f, Pitch = -0.1f }, NPC.Center);
                for (int i = 0; i < 50; i++)
                {
                    int dustType = Main.rand.NextBool() ? DustID.Torch : DustID.CrimsonTorch;
                    Dust burst = Dust.NewDustPerfect(tip, dustType, Main.rand.NextVector2Circular(5f, 5f), 60, default,
                        Main.rand.NextFloat(1.2f, 2f));
                    burst.noGravity = true;
                }
                UsefulFunctions.ScreenShake(NPC.Center, 4f, 10, distanceFalloff: 500f);
            }

            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            for (int i = 0; i <= 4; i++)
            {
                // 0 = right, then 45° steps counter-clockwise up to 180 = left (world Y is down, so up is negative).
                float angle = -MathHelper.PiOver4 * i;
                Vector2 velocity = angle.ToRotationVector2() * UnbindOrbSpeed;
                SpawnFireBolt(tip, velocity, 0, OolacileFireBolt.ModeAimed);
            }
        }

        #region Magic formations
        protected override void DoMagicTelegraphVFX(float progress)
        {
            if (Main.dedServ)
            {
                return;
            }

            Vector2 tip = StaffTipPosition();
            float radius = MathHelper.Lerp(26f, 6f, progress);
            int moteCount = 1 + (progress > 0.5f ? 1 : 0);
            for (int i = 0; i < moteCount; i++)
            {
                Vector2 offset = Main.rand.NextVector2CircularEdge(radius, radius);
                int dustType = Main.rand.NextBool(3) ? DustID.CrimsonTorch : DustID.Torch;
                Dust mote = Dust.NewDustPerfect(tip + offset, dustType,
                    -offset.SafeNormalize(Vector2.Zero) * MathHelper.Lerp(1f, 2.6f, progress), 80, default,
                    MathHelper.Lerp(0.8f, 1.3f, progress));
                mote.noGravity = true;
            }
            Lighting.AddLight(tip, 0.3f + 0.4f * progress, 0.12f + 0.15f * progress, 0.04f);

            // Commit cue: the moment the cast becomes hyper-armored (too late to stagger it out).
            if (PhaseTimer == CastCommitTicks)
            {
                SoundEngine.PlaySound(SoundID.Item45 with { Volume = 0.4f, Pitch = 0.5f }, NPC.Center);
            }
        }

        protected override void DoMagicAttack()
        {
            if (!Main.dedServ)
            {
                SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.6f, PitchVariance = 0.1f }, NPC.Center);
            }

            if (Main.netMode == NetmodeID.MultiplayerClient || !NPC.HasValidTarget)
            {
                // A client never rolls the formation, so its debug readout stays generic.
                DebugAttackLabel = "Pyromancy";
                return;
            }

            Player target = Main.player[NPC.target];
            Vector2 tip = StaffTipPosition();
            Vector2 aimPoint = target.Center + target.velocity * AimLeadTicks;
            Vector2 aimDirection = (aimPoint - tip).SafeNormalize(new Vector2(NPC.direction, 0f));
            Formation formation = DrawFormation();

            // Debug HUD / telemetry: name the cast instead of the bare "MagicAttack" phase.
            DebugAttackLabel = formation switch
            {
                Formation.FlameBolt => "Flame Bolt",
                Formation.FlameFan => "Flame Fan",
                Formation.EmberChain => "Ember Chain",
                Formation.EmberRain => "Ember Rain",
                _ => "Pyromancy",
            };

            switch (formation)
            {
                case Formation.FlameBolt:
                    SpawnFireBolt(tip, aimDirection * FlameBoltSpeed, 0, OolacileFireBolt.ModeAimed);
                    break;

                case Formation.FlameFan:
                    for (int i = -1; i <= 1; i++)
                    {
                        Vector2 fanVelocity = aimDirection.RotatedBy(MathHelper.ToRadians(i * FlameFanSpreadDegrees)) * FlameFanSpeed;
                        SpawnFireBolt(tip, fanVelocity, 0, OolacileFireBolt.ModeAimed);
                    }
                    break;

                case Formation.EmberChain:
                    // Three bolts form stacked at the tip and release 12 ticks apart, each re-aiming on release.
                    for (int i = 0; i < 3; i++)
                    {
                        Vector2 formPosition = tip + new Vector2(0f, (i - 1) * 10f);
                        int releaseDelay = EmberChainIntervalTicks * (i + 1);
                        SpawnFireBolt(formPosition, aimDirection * FlameBoltSpeed, releaseDelay,
                            OolacileFireBolt.ModeReaimOnRelease, target.whoAmI);
                    }
                    break;

                case Formation.EmberRain:
                    // Five embers form 250px above the player 46px apart and fall after 20–40 ticks of flicker.
                    // At 7 px/t with the 20t ramp that is ~45 ticks of warning. Spots inside solid tiles are skipped.
                    for (int i = -2; i <= 2; i++)
                    {
                        Vector2 rainOrigin = target.Center + new Vector2(i * EmberRainSpacing, -EmberRainHeight - Math.Abs(i) * 12f);
                        bool insideTiles = Collision.SolidCollision(rainOrigin - new Vector2(9f), 18, 18);
                        if (insideTiles)
                        {
                            continue;
                        }

                        Vector2 rainTarget = target.Center + target.velocity * (20f + i * 2f) + new Vector2(i * EmberRainSpacing * 0.5f, 0f);
                        Vector2 rainVelocity = (rainTarget - rainOrigin).SafeNormalize(Vector2.UnitY) * EmberRainSpeed;
                        int formDelay = 20 + (i + 2) * 5;
                        SpawnFireBolt(rainOrigin, rainVelocity, formDelay, OolacileFireBolt.ModeAimed);
                    }
                    break;
            }
        }

        // Shuffled bag without replacement; a refill never starts with the formation that just played.
        private Formation DrawFormation()
        {
            if (_formationBag.Count == 0)
            {
                _formationBag.Add(Formation.FlameBolt);
                _formationBag.Add(Formation.FlameFan);
                _formationBag.Add(Formation.EmberChain);
                _formationBag.Add(Formation.EmberRain);

                for (int i = _formationBag.Count - 1; i > 0; i--)
                {
                    int swapIndex = Main.rand.Next(i + 1);
                    Formation swapped = _formationBag[i];
                    _formationBag[i] = _formationBag[swapIndex];
                    _formationBag[swapIndex] = swapped;
                }

                bool repeatsLast = _formationBag[_formationBag.Count - 1] == _lastFormation;
                if (repeatsLast)
                {
                    Formation first = _formationBag[0];
                    _formationBag[0] = _formationBag[_formationBag.Count - 1];
                    _formationBag[_formationBag.Count - 1] = first;
                }
            }

            // Deal from the end of the list.
            int lastIndex = _formationBag.Count - 1;
            Formation drawn = _formationBag[lastIndex];
            _formationBag.RemoveAt(lastIndex);
            _lastFormation = drawn;
            return drawn;
        }
        #endregion

        private void SpawnFireBolt(Vector2 position, Vector2 velocity, int formDelayTicks, float mode, int targetIndex = -1)
        {
            Projectile.NewProjectile(
                NPC.GetSource_FromThis(),
                position,
                velocity,
                ModContent.ProjectileType<OolacileFireBolt>(),
                MagicDamage,
                1f,
                Main.myPlayer,
                formDelayTicks,
                mode,
                targetIndex);
        }

        // The hand pose only exists where the puppet is drawn; a dedicated server estimates the tip from the
        // cast's end angle (the same fallback Kahlrun uses).
        private Vector2 StaffTipPosition()
        {
            if (!Main.dedServ)
            {
                return PuppetWeaponTipPosition(StaffTipReach);
            }

            float worldAngle = MagicCastEndRotation;
            if (NPC.direction != 1)
            {
                worldAngle = MathHelper.Pi - MagicCastEndRotation;
            }

            Vector2 staffDirection = worldAngle.ToRotationVector2();
            return NPC.Center + new Vector2(NPC.direction * 4f, -2f) + staffDirection * 44f;
        }

        #region Claw combo hooks
        protected override bool CanSelectMeleeCombo(MeleeCombo combo, float distance, float healthFraction)
        {
            if (combo.Name == PounceName)
            {
                return distance >= PounceMinRange && distance <= PounceMaxRange;
            }
            return true;
        }

        protected override void OnMeleeComboStarted(MeleeCombo combo)
        {
            base.OnMeleeComboStarted(combo);
            _pounceArcSpawned = false;

            if (combo.Name != PounceName)
            {
                return;
            }

            // Pounce tell cue: a wet snarl and a spray of blood while the red flash builds.
            if (!Main.dedServ)
            {
                SoundEngine.PlaySound(SoundID.NPCHit20 with { Volume = 0.5f, Pitch = -0.3f }, NPC.Center);
                for (int i = 0; i < 12; i++)
                {
                    Dust blood = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, DustID.Blood, 0f, 0f, 0, default, 1.2f);
                    blood.velocity = Main.rand.NextVector2Circular(2f, 2f);
                }
            }
        }

        // Pounce's arc spawns once, either when its airborne downswing starts or on the landing.
        private bool _pounceArcSpawned;
        private const int PounceImpactArcTicks = 10;

        protected override void OnMeleeComboAttackTick(MeleeCombo combo, MeleeComboStep step, int elapsed, int total)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient || step.DamageMult <= 0f)
            {
                return;
            }

            bool rakeSwing = step.Motion == ComboMotion.OverheadArc || step.Motion == ComboMotion.UnderhandArc;
            if (rakeSwing && elapsed == 0)
            {
                SpawnClawArcs(step, total, impact: false);
                return;
            }

            // The landing-timed leap reports a live slash pose only once its downswing has begun (not while carried).
            bool pounceDownswing = step.Motion == ComboMotion.LeapSlam
                && !_pounceArcSpawned
                && TryGetMeleeSlashTrailPose(out _, out _, out _, out _, out _, out _, out _, out _);
            if (pounceDownswing)
            {
                _pounceArcSpawned = true;
                SpawnClawArcs(step, PounceImpactArcTicks, impact: true);
            }
        }

        protected override void OnLeapSlamLanded(MeleeComboStep step)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient || _pounceArcSpawned)
            {
                return;
            }

            _pounceArcSpawned = true;
            SpawnClawArcs(step, PounceImpactArcTicks, impact: true);
        }

        // One VanillaSwordArc per swinging hand, each tracking its own claw's live pivot and blade and running on
        // the step's exact Weighted clock, fading out as the blade disarms. Blood-red on the Night's Edge arc.
        private void SpawnClawArcs(MeleeComboStep step, int duration, bool impact)
        {
            float sweep = MathHelper.ToRadians(170f);
            if (step.Motion == ComboMotion.UnderhandArc)
            {
                sweep = -sweep;
            }

            float hitWindowEnd = step.HitWindowEnd > 0f ? step.HitWindowEnd : 0.5f;
            float fadeOutFraction = 1f - hitWindowEnd;
            if (impact)
            {
                fadeOutFraction = 0.25f;
            }

            float bladeReach = ComboReachBase * 0.7f * step.ReachMult;
            bool frontSwings = step.Hand != ComboHand.Back;
            bool backSwings = step.Hand != ComboHand.Front;

            if (frontSwings)
            {
                SpawnClawArc(step, duration, sweep, bladeReach, fadeOutFraction, trackBackHand: false);
            }
            if (backSwings)
            {
                SpawnClawArc(step, duration, sweep, bladeReach, fadeOutFraction, trackBackHand: true);
            }
        }

        private void SpawnClawArc(MeleeComboStep step, int duration, float sweep, float bladeReach, float fadeOutFraction, bool trackBackHand)
        {
            Vector2 bladeDirection = PuppetWeaponDirection;
            if (trackBackHand)
            {
                bladeDirection = PuppetBackWeaponDirection;
            }

            VanillaSwordArcSettings settings = new VanillaSwordArcSettings
            {
                Texture = VanillaSwordArcTexture.NightsEdge,
                Easing = VanillaSwordArcEasing.Weighted,
                WeightedEaseInTicks = step.EaseInTicks,
                WeightedEaseOutTicks = step.EaseOutTicks,
                WeightedEaseOutDecay = step.EaseOutDecay,
                Duration = duration,
                StartAngle = bladeDirection.ToRotation(),
                SweepAngle = sweep * NPC.direction,
                Radius = Math.Max(40f, bladeReach * 1.15f),
                Opacity = 0.5f,
                FadeInFraction = 0.04f,
                FadeOutFraction = fadeOutFraction,
                AfterimageLag = MathHelper.PiOver4,
                AfterimageOpacity = 0.6f,
                BodyOpacity = 0.7f,
                CoreOpacity = 0.3f,
                TipSparkleOpacity = 0f,
                DarkColor = new Color(40, 0, 6),
                BodyColor = new Color(170, 20, 35),
                CoreColor = new Color(240, 110, 110),
                DustType = DustID.Blood,
                DustColor = new Color(170, 20, 35),
                DustCount = 1,
                DustAlpha = 80,
                DustScale = 0.8f,
                DustRadiusFraction = 0.9f,
                DustAngularSpread = 0.45f,
                DustTangentialSpeed = 1.1f,
                DustVelocityJitter = 0.3f,
                DustInheritAnchorVelocity = 0.15f,
                DrawTipSparkle = false,
                TrackPuppetBlade = true,
                TrackPuppetBackHand = trackBackHand,
                EnableCollision = false,
            };

            VanillaSwordArc.SpawnForNPC(NPC.GetSource_FromAI(), NPC, 0, 0f, Main.myPlayer, settings, Vector2.Zero, hostile: false);
        }

        protected override bool ShouldContinueMeleeCombo(string comboName, int nextStepIndex, Player target, bool previousStepHit)
        {
            if (comboName == TwinRakeName && nextStepIndex == 1)
            {
                // The return rake goes live inside a late roller's 22–30 tick gap, so it only follows a MISS with
                // the player still in front and in reach. After a hit, post-hit immunity would eat it anyway.
                bool stillInFront = Math.Sign(target.Center.X - NPC.Center.X) == NPC.direction;
                float returnRakeReach = ComboReachBase * 0.7f * ClawReachMult + 30f;
                bool insideReach = NPC.Distance(target.Center) <= returnRakeReach;
                return !previousStepHit && stillInFront && insideReach;
            }
            return base.ShouldContinueMeleeCombo(comboName, nextStepIndex, target, previousStepHit);
        }

        protected override void ModifyMeleeArcEndpoints(ComboMotion motion, ref float startRotation, ref float endRotation)
        {
            bool comboPoseActive = Phase == AttackPhase.MeleeComboTelegraph
                || Phase == AttackPhase.MeleeComboAttack
                || Phase == AttackPhase.MeleeComboPause
                || Phase == AttackPhase.MeleeComboRecovery;
            if (!comboPoseActive)
            {
                return;
            }

            if (motion == ComboMotion.OverheadArc)
            {
                startRotation = RakeHighPose;
                endRotation = RakeLowPose;
            }
            else if (motion == ComboMotion.UnderhandArc)
            {
                startRotation = RakeLowPose;
                endRotation = RakeHighPose;
            }
        }

        protected override void DoComboMeleeHit(MeleeComboStep step)
        {
            if (step.DamageMult <= 0f)
            {
                return;
            }

            if (!Main.dedServ)
            {
                SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.55f, Pitch = 0.45f, PitchVariance = 0.15f }, NPC.Center);
            }
            base.DoComboMeleeHit(step);
        }

        protected override void DoMeleeAttack()
        {
            if (!Main.dedServ)
            {
                SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.55f, Pitch = 0.45f, PitchVariance = 0.15f }, NPC.Center);
            }
            TryMeleeHit();
        }

        protected override void DoRangedAttack() { }

        // Runs on the hit player's machine (PuppetMeleeHitbox.OnHitPlayer).
        public void OnHumanoidMeleeHit(Player target)
        {
            target.AddBuff(BuffID.Bleeding, BleedingDebuffTicks);
        }
        #endregion

        private void SpawnAmbientEffects()
        {
            if (Main.dedServ || Phase == AttackPhase.Custom)
            {
                return;
            }

            if (!_staffBurned)
            {
                // Caster identity: a faint ember drifting up from the robe.
                if (Main.GameUpdateCount % 8 == 0)
                {
                    Dust ember = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, DustID.Torch, 0f, -1f, 120, default, 0.8f);
                    ember.noGravity = true;
                    ember.velocity *= 0.3f;
                }
                return;
            }

            // Frenzy identity: blood dripping from the claws.
            if (Main.GameUpdateCount % 10 == 0 && MeleeWeaponItemType >= 0)
            {
                Dust drip = Dust.NewDustDirect(PuppetHandPosition - new Vector2(4f), 8, 8, DustID.Blood, 0f, 0f, 0, default, 0.9f);
                drip.velocity = new Vector2(0f, 0.5f);
            }
        }

        public override void HitEffect(NPC.HitInfo hit)
        {
            base.HitEffect(hit); // 150 Blood dust on death

            if (NPC.life > 0 || Main.dedServ)
            {
                return;
            }

            // 15 red-tinted animated clouds spinning 1–2 tiles outward.
            int cloudType = ModContent.GoreType<OolacileRedCloudGore>();
            for (int i = 0; i < DeathCloudCount; i++)
            {
                Vector2 outward = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(1f, 2f);
                Vector2 spawnPosition = NPC.Center - new Vector2(14f) + Main.rand.NextVector2Circular(8f, 12f);
                Gore cloud = Gore.NewGoreDirect(NPC.GetSource_Death(), spawnPosition, outward, cloudType, Main.rand.NextFloat(0.9f, 1.3f));
                cloud.velocity = outward;
            }
        }

        public override void OnKill()
        {
            for (int i = 0; i < HeartDropCount; i++)
            {
                Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ItemID.Heart);
            }
            for (int i = 0; i < ManaStarDropCount; i++)
            {
                Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ItemID.Star);
            }
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            base.SendExtraAI(writer);
            writer.Write(_frenzyTriggered);
            writer.Write(_staffBurned);
            writer.Write((byte)_customKind);
            writer.Write((short)Math.Clamp(_recoilFlareCooldown, 0, short.MaxValue));
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            base.ReceiveExtraAI(reader);
            _frenzyTriggered = reader.ReadBoolean();
            _staffBurned = reader.ReadBoolean();
            _customKind = (CustomKind)reader.ReadByte();
            _recoilFlareCooldown = reader.ReadInt16();
        }
    }

    /// <summary>
    /// Death cloud for the Oolacile Cultist: Gores/SmallGreyCloud.png (28x112, four 28x28 frames, no padding)
    /// tinted blood red. Cycles its frames, spins, and drifts outward under drag (~16x its launch speed in px,
    /// so a 1–2 px/tick launch travels the intended 1–2 tiles) before fading. A separate class so the shared
    /// grey cloud stays grey for anything else.
    /// </summary>
    public class OolacileRedCloudGore : ModGore
    {
        private const int LifetimeTicks = 60;
        private const int FadeTicks = 25;
        private const int TicksPerFrame = 6;
        private const float Drag = 0.94f;

        public override string Texture => "tsorcRevamp/Gores/SmallGreyCloud";

        public override void OnSpawn(Gore gore, IEntitySource source)
        {
            gore.Frame = new SpriteFrame(1, 4) { PaddingX = 0, PaddingY = 0 };
            gore.Frame.CurrentRow = (byte)Main.rand.Next(4);
            gore.timeLeft = LifetimeTicks;
            gore.alpha = 40;
            gore.rotation = Main.rand.NextFloat(MathHelper.TwoPi);
        }

        // Returns false: this fully replaces vanilla gore physics (no gravity, no sticking to tiles).
        public override bool Update(Gore gore)
        {
            gore.position += gore.velocity;
            gore.velocity *= Drag;

            int spinDirection = gore.velocity.X < 0f ? -1 : 1;
            gore.rotation += 0.05f * spinDirection;

            gore.frameCounter++;
            if (gore.frameCounter >= TicksPerFrame)
            {
                gore.frameCounter = 0;
                gore.Frame.CurrentRow = (byte)((gore.Frame.CurrentRow + 1) % 4);
            }

            gore.timeLeft--;
            if (gore.timeLeft < FadeTicks)
            {
                float fadeProgress = 1f - gore.timeLeft / (float)FadeTicks;
                gore.alpha = 40 + (int)(215f * fadeProgress);
            }

            if (gore.timeLeft <= 0)
            {
                gore.active = false;
            }
            return false;
        }

        public override Color? GetAlpha(Gore gore, Color lightColor)
        {
            Color bloodRed = new Color(190, 30, 40);
            float opacity = 1f - gore.alpha / 255f;
            return Color.Lerp(lightColor, bloodRed, 0.7f) * opacity;
        }
    }
}

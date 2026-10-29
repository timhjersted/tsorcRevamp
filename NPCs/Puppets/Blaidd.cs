using Microsoft.Xna.Framework;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;
using tsorcRevamp.Content.Items.Accessories.Defensive.Shields;
using tsorcRevamp.Content.Items.Materials;
using tsorcRevamp.Content.Items.Materials.Souls;
using tsorcRevamp.Content.Items.Materials.Souls.DarkSoul;
using tsorcRevamp.Content.Projectiles.Enemy;
using tsorcRevamp.Content.Projectiles.Enemy.Weapons;
using tsorcRevamp.NPCs.AI;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.NPCs.Puppets
{
    /// <summary>
    /// Invader puppet — Blaidd (Wolf Mask + Plaguebringer's Cloak + Jim's Leggings + Yoraiz0r's Scowl).
    /// Pure melee duelist. Phase 1: one-handed Titanium Sword + Spiked Iron Shield, advancing behind the raised shield
    /// at full speed, Shield Bash opener. Phase 2 (below 50%): tears the shield off, goes two-handed and faster.
    /// The Azure Suppression Flask (Suppressed + Grapple Malfunction, 100-tile ring) is his only ranged act.
    /// Stock swings borrow OwlFather / StuddedLeatherWarrior timings. Design: tsorcDocs/Blaidd_Rework_Proposal.md.
    /// </summary>
    [AutoloadBossHead]
    public class Blaidd : PuppetNPC, IHumanoidMeleeHitEffects
    {
        public override string BossHeadTexture => "tsorcRevamp/NPCs/Puppets/Blaidd_Head_Boss";

        protected override string InvaderTitle => "Blaidd";

        // ── Combo names (compared in the selection / continuation hooks) ─────────
        private const string WolfDownUpName = "Wolf Down-Up";
        private const string WolfUpDownName = "Wolf Up-Down";
        private const string AdvancingDownUpName = "Advancing Down-Up";
        private const string HuntingTripleName = "Hunting Triple";
        private const string HeavyChopName = "Wolf Heavy Chop";
        private const string BashFollowThroughName = "Bash Follow-Through";
        private const string FangThrustName = "Fang Thrust";
        private const string WolfPounceName = "Wolf Pounce";
        private const string BackstepReentryName = "Backstep Re-entry";
        private const string FeralFlurryName = "Feral Flurry";
        private const string WolfWhirlName = "Wolf Whirl";
        private const string MoonriseSlamName = "Moonrise Slam";
        private const string HowlingLungeName = "Howling Lunge";
        private const string RisingFangName = "Rising Fang";
        private const string LedgeHopName = "Ledge Hop";
        private const string MoonlightCrescentName = "Moonlight Crescent";
        private const string PackHuntName = "Pack Hunt";
        private const string HuntersPatienceName = "Hunter's Patience";
        private const string MoonlitExecutionName = "Moonlit Execution";

        // ── Smart shield ──────────────────────────────────────────────────────────
        // The advancing guard is raised only when it matters: closing the last ApproachGuardRange to the target,
        // answering a hit taken from farther than DistantHitRange (held DistantHitGuardTicks), or while the target
        // is using a non-melee weapon at range. While raised it blocks ALL frontal damage and destroys frontal
        // projectiles (Shield Deflect). Backs, tops and attack phases stay open.
        private const float ApproachGuardRange = 10f * 16f;
        private const float DistantHitRange = 160f;
        private const int DistantHitGuardTicks = 150;

        // ── Anti-air gates ────────────────────────────────────────────────────────
        // Ground cuts never start at a target more than GroundCutMaxRise above. Rising Fang covers
        // [GroundCutMaxRise, RisingFangMaxRise] within RisingFangMaxHorizontal; Ledge Hop covers targets on ledges
        // farther out.
        private const float GroundCutMaxRise = 48f;
        private const float RisingFangMaxRise = 240f;
        private const float RisingFangMaxHorizontal = 160f;
        private const float LedgeHopMaxRise = 300f;

        // ── Moon's Pull (anti-air howl) ───────────────────────────────────────────
        // Only against an unsuppressed flier (Suppressed already grounds wings). 40t howl tell (interruptible, no
        // hyper armor), then 45t of extra downward pull on every airborne unsuppressed player within 25 tiles.
        private const int MoonsPullTellTicks = 40;
        private const int MoonsPullTicks = 45;
        private const int MoonsPullCooldownTicks = 900;
        // 1.5s off the ground: longer than an ordinary jump, so only flying / gliding / long falls trigger it.
        private const int MoonsPullAirborneTrigger = 90;
        private const float MoonsPullRange = 25f * 16f;
        private const float MoonsPullGravity = 0.45f;
        private const float MoonsPullMaxFall = 8f;

        // ── Shield Charge (phase 1 anti-range) ────────────────────────────────────
        // 30t tell, then up to 90t sprint at 2x speed behind the raised shield (deflecting everything in front).
        // Reaching ChargeBashRange converts straight into the bash's lunge; otherwise a 40t winded recovery.
        private const int ChargeTellTicks = 30;
        private const int ChargeSprintTicks = 90;
        private const int ChargeWindedTicks = 40;
        private const int ChargeCooldownTicks = 600;
        private const float ChargeSpeedMult = 2f;
        private const float ChargeMinRange = 240f;
        private const float ChargeMaxRange = 640f;
        private const float ChargeBashRange = 64f;

        // ── Moonlight Crescent (phase 2) ──────────────────────────────────────────
        private const float CrescentMaxTriggerRange = 460f;

        // ── Pack Hunt ─────────────────────────────────────────────────────────────
        // The second pounce only plays on a whiff with the target still beyond PackHuntRepeatRange.
        private const float PackHuntRepeatRange = 200f;

        // ── Moonlit Execution ─────────────────────────────────────────────────────
        private const float ExecutionLifeFraction = 0.3f;

        // ── Hunter's Sprint ───────────────────────────────────────────────────────
        // After HuntersSprintSightTicks without line of sight, run at x1.6 for up to HuntersSprintTicks.
        private const int HuntersSprintSightTicks = 180;
        private const int HuntersSprintTicks = 120;
        private const float HuntersSprintSpeedMult = 1.6f;

        // ── Wolf's Howl (phase 2 self-buff) ───────────────────────────────────────
        // 40t howl (punishable), then 8s of +20% move speed and telegraphs x0.85.
        private const int WolfHowlTicks = 40;
        private const int WolfHowlCooldownTicks = 1200;
        private const int WolfHowlBuffTicks = 480;
        private const float WolfHowlSpeedMult = 1.2f;
        private const float WolfHowlTelegraphMult = 0.85f;

        // ── Stalking (phase 2 neutral) ────────────────────────────────────────────
        // After an attack, StalkChancePercent to back off and pace 6-10 tiles away for 2-3s, then for
        // StalkFinisherWindowTicks only a committed gap-closer (Howling Lunge / Pounce / Pack Hunt) may start.
        private const int StalkChancePercent = 25;
        private const int StalkMinTicks = 120;
        private const int StalkMaxTicks = 180;
        private const int StalkFinisherWindowTicks = 90;
        private const float StalkKiteMinTiles = 6f;
        private const float StalkKiteMaxTiles = 10f;

        // ── Phase 2 ───────────────────────────────────────────────────────────────
        private const float PhaseTwoHealthFraction = 0.5f;
        // Unleash transition: 90t hyper-armored set-piece. The shield leaves the arm at UnleashShieldThrowTick.
        private const int UnleashTicks = 90;
        private const int UnleashShieldThrowTick = 20;
        private const int UnleashHowlTick = 40;

        // ── Shield Bash (phase 1 opener out of the advancing guard) ───────────────
        // Tell 24t (shield cocked, white-blue flash) -> 14t lunge, distance-solved to arrive ~30px from the target ->
        // 32t planted recovery. If the target is still in reach at the lunge's end, the recovery is cut to 12t and
        // Bash Follow-Through is queued: its >=26t on-screen tell puts that swing 38t+ after the bash's live window
        // ends, past the 30t post-roll gap, so a roll spent on the bash never leaves the overhead unrollable.
        private const int BashTellTicks = 24;
        private const int BashLungeTicks = 14;
        private const int BashRecoveryTicks = 32;
        private const int BashFollowUpRecoveryTicks = 12;
        private const int BashCooldownTicks = 360;
        private const float BashTriggerMinRange = 48f;
        private const float BashTriggerMaxRange = 150f;
        private const float BashArriveDistance = 30f;
        private const float BashMinTravel = 40f;
        private const float BashMaxTravel = 110f;
        private const float BashDamageMult = 0.6f;
        private const float BashKnockback = 7f;
        private const int BashHitboxInterval = 4;
        private const int BashFollowUpWindowTicks = 40;

        // On-hit debuffs (seconds x 60). Sword cuts wither armor; the shield shove withers the weapon.
        private const int SwordWitheredArmorTicks = 6 * 60;
        private const int BashWitheredWeaponTicks = 8 * 60;

        // ── Azure Suppression Flask ───────────────────────────────────────────────
        // Not staggerable and not avoidable by design (it explains the debuff). Gated so it is never wasted:
        // not in the first FlaskFirstThrowDelayTicks, and only while someone within the ring's reach is unsuppressed.
        private const int FlaskFirstThrowDelayTicks = 600;
        private const int FlaskCooldownTicks = 1500;
        private const float FlaskThrowSpeed = 11f;
        private const float FlaskOvershoot = 80f;

        // Follow-up swings in the conditional combos only play if the last one hit, or the target is still inside
        // the next swing's reach plus the ground its push covers (~ComboReachBase*0.7*1.2 + 40px).
        private const float FollowUpPushAllowance = 40f;

        private enum BlaiddCustomKind : byte
        {
            None = 0,
            ShieldBash,
            Unleash,
            ShieldCharge,
            // Start-only: a Shield Charge that reached its target enters the bash at the lunge, skipping the tell.
            ChargeBash,
            MoonsPull,
            WolfHowl,
        }

        private bool _phaseTwoStarted;
        private bool _shieldDiscarded;
        private BlaiddCustomKind _customKind;
        // What the last set-piece was STARTED as (differs from _customKind only for ChargeBash); sent to clients.
        private BlaiddCustomKind _customStartKind;
        private byte _customSerial;
        private int _distantHitGuardTicks;
        private int _chargeCooldown;
        private int _moonsPullCooldown;
        private int _wolfHowlCooldown;
        private int _wolfHowlBuffTicks;
        private int _stalkTicks;
        private int _stalkFinisherWindow;
        // Local per peer (derived from synced positions): not sent.
        private int _noSightTicks;
        private int _huntersSprintTicks;
        private bool _wasFreePhase = true;
        private readonly int[] _playerAirborneTicks = new int[Main.maxPlayers];
        private int _customElapsed;
        private int _bashCooldown;
        private float _bashLungeSpeed;
        private int _bashFollowUpWindow;
        private int _fightTicks;

        // ── Loadout (all vanilla) ─────────────────────────────────────────────────
        protected override int HeadArmorItemType => ItemID.WolfMask;
        protected override int BodyArmorItemType => ItemID.PlaguebringerChestplate; // "Plaguebringer's Cloak"
        protected override int LegsArmorItemType => ItemID.JimsLeggings;

        protected override int HeadArmorDyeItemType => ItemID.BrightBrownDye;
        protected override int BodyArmorDyeItemType => ItemID.SilverDye;
        protected override int LegsArmorDyeItemType => ItemID.SilverDye;

        // Yoraiz0r's Scowl — glowing-eye vanity accessory
        protected override int[] AccessoryItemTypes => new int[]
        {
            ItemID.Yoraiz0rDarkness,
        };
        protected override int[] AccessoryDyeItemTypes => new int[]
        {
            0,
        };

        // ── Weapons ───────────────────────────────────────────────────────────────
        protected override int MeleeWeaponItemType => ItemID.TitaniumSword;
        // The flask is drawn with the vanilla Flask of Nanites sprite (blue glass) through the shared throw pose.
        protected override int RangedWeaponItemType => ItemID.FlaskofNanites;
        protected override RangedStyle RangedAnimStyle => RangedStyle.Throw;

        protected override WeaponArchetype MeleeArchetype => WeaponArchetype.Greatsword;

        protected override int MeleeDamage => 70;
        protected override int RangedDamage => 0;

        // ── Shield (phase 1 only) ─────────────────────────────────────────────────
        protected override int ShieldItemType => _shieldDiscarded ? -1 : ModContent.ItemType<SpikedIronShield>();
        // Stance guard only: raised while pursuing, dropped the moment an attack starts. No reactive planted guard,
        // which would stall a pure melee pressure fighter.
        protected override bool HoldShieldWhileAdvancing => true;
        protected override float AdvancingGuardMaxRange => 30f * 16f;

        // See the Smart shield constants. Every input here is shared by all peers: positions, the target's held
        // item / use animation (synced player state), and the synced _distantHitGuardTicks.
        protected override bool ShouldRaiseAdvancingGuard(Player target, float distance)
        {
            bool answeringDistantHit = _distantHitGuardTicks > 0;
            bool finalApproach = distance <= ApproachGuardRange;
            bool shootingFromRange = TargetShootingFromRange(target, distance);
            return answeringDistantHit || finalApproach || shootingFromRange;
        }

        // A hit from farther than DistantHitRange raises the guard for DistantHitGuardTicks. Runs where hits are
        // authoritative (server / singleplayer); synced to clients.
        protected override void OnHitRegistered(Vector2 sourceCenter, int damageDone, bool meleeHit)
        {
            if (_shieldDiscarded)
            {
                return;
            }

            float sourceDistance = Vector2.Distance(sourceCenter, NPC.Center);

            if (sourceDistance > DistantHitRange)
            {
                _distantHitGuardTicks = DistantHitGuardTicks;
                NPC.netUpdate = true;
            }
        }

        // Raised shield = the advancing guard, or the Shield Charge's tell + sprint.
        private bool ShieldRaised
        {
            get
            {
                if (_shieldDiscarded)
                {
                    return false;
                }

                bool charging = Phase == AttackPhase.Custom
                    && _customKind == BlaiddCustomKind.ShieldCharge
                    && _customElapsed <= ChargeTellTicks + ChargeSprintTicks;
                return AdvancingGuardUp || charging;
            }
        }

        // Full frontal block: hits from the front pass through untouched while the shield is raised.
        public override bool? CanBeHitByProjectile(Projectile projectile)
        {
            bool fromFront = System.Math.Sign(projectile.Center.X - NPC.Center.X) == NPC.direction;

            if (ShieldRaised && fromFront)
            {
                return false;
            }

            return base.CanBeHitByProjectile(projectile);
        }

        public override bool? CanBeHitByItem(Player player, Item item)
        {
            bool fromFront = System.Math.Sign(player.Center.X - NPC.Center.X) == NPC.direction;

            if (ShieldRaised && fromFront)
            {
                return false;
            }

            return base.CanBeHitByItem(player, item);
        }

        // ── Melee swing polish (OwlFather's opt-ins) ──────────────────────────────
        protected override Vector2 MeleeHandleNorm => new Vector2(0.12f, 0.88f);
        protected override float ComboReachBase => 96f;
        protected override float MeleeWeaponRotationOffset => 1.0f;
        protected override bool MirrorMeleeSwingRotationByFacing => true;
        protected override bool UseSwingEasing => true;
        protected override bool UseAimAdaptiveArc => true;
        protected override bool UseAuthoredComboSwingClock => true;
        protected override bool AuthoredClockCoversJoustDash => true;
        protected override bool UseLogicalMeleeTelegraphs => true;
        protected override bool UseCompositeArmSwing => true;
        protected override float MeleeCompositeArmRotationOffset => MeleeWeaponRotationOffset;
        // Phase 2 grips the sword with both hands; phase 1 needs the off hand for the shield.
        protected override bool UseTwoHandedCompositeSwing => _shieldDiscarded;
        protected override bool UseLandingTimedLeapSlam => true;
        protected override int MeleeComboInterStepLingerTicks => 3;
        protected override int MeleeRecoveryLingerTicks => 6;
        protected override bool HasSlashVFX => true;
        protected override Color SlashVFXColor => new Color(200, 220, 255);

        // The borrowed arcs only sweep ~110-130°. Extend the LOW end (overhead's finish, underhand's start) by
        // ~26°, widening every cut while keeping the shared low pose that lets Down-Up / Up-Down chain without a snap.
        // The high end is left alone: past ~-1.7 rad the arm folds through the torso.
        private const float LowPoseExtensionRadians = 0.45f;

        protected override void ModifyMeleeArcEndpoints(ComboMotion motion, ref float startRotation, ref float endRotation)
        {
            if (motion == ComboMotion.OverheadArc)
            {
                endRotation += LowPoseExtensionRadians;
            }
            else if (motion == ComboMotion.UnderhandArc)
            {
                startRotation += LowPoseExtensionRadians;
            }
        }

        // ── Melee combos ──────────────────────────────────────────────────────────
        private static MeleeComboStep WolfSwing(
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
            float hitWindowEnd = 0f,
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
                HitWindowEnd = hitWindowEnd,
            };

        // Owl's Greatfire Pursuit Slam step: Wolf Pounce and both Pack Hunt pounces. Named so Pack Hunt's
        // continuation can re-check its reliable jump range.
        private static readonly MeleeComboStep PounceStep = WolfSwing(ComboMotion.LeapSlam, 36, 82,
            damageMult: 1.25f, reachMult: 1.25f,
            leapHeightMult: 0.62f, leapForwardSpeedMult: 1.70f,
            leapStrikeRange: 120f,
            ease: SwingEaseStyle.Whip);

        private static readonly MeleeCombo[] BlaiddCombos = new[]
        {
            // ── Both phases ── (timings from OwlFather unless noted)
            // Owl's Down-Up Reversal. The follow-up is conditional (ShouldContinueMeleeCombo).
            new MeleeCombo
            {
                Name = WolfDownUpName,
                BaseWeight = 90,
                Preferred = ComboRangeBand.Close,
                InitialFlashColor = Color.White,
                CooldownAfterUse = 145,
                MoveBrake = 0.16f,
                Steps = new[]
                {
                    WolfSwing(ComboMotion.OverheadArc, 34, 25, pauseAfter: 9,
                        damageMult: 0.88f, forwardPushMult: 0.48f),
                    WolfSwing(ComboMotion.UnderhandArc, 0, 27,
                        damageMult: 1.18f, forwardPushMult: 0.58f, reachMult: 1.18f),
                },
            },
            // Owl's Up-Down Reversal. Conditional follow-up.
            new MeleeCombo
            {
                Name = WolfUpDownName,
                BaseWeight = 80,
                Preferred = ComboRangeBand.Close,
                InitialFlashColor = Color.LightCyan,
                CooldownAfterUse = 150,
                MoveBrake = 0.16f,
                Steps = new[]
                {
                    WolfSwing(ComboMotion.UnderhandArc, 34, 25, pauseAfter: 9,
                        damageMult: 0.88f, forwardPushMult: 0.48f),
                    WolfSwing(ComboMotion.OverheadArc, 0, 28,
                        damageMult: 1.22f, forwardPushMult: 0.58f, reachMult: 1.20f,
                        ease: SwingEaseStyle.Whip),
                },
            },
            // Owl's Advancing Down-Up: the pushing-in pair, chosen from mid range.
            new MeleeCombo
            {
                Name = AdvancingDownUpName,
                BaseWeight = 65,
                Preferred = ComboRangeBand.Mid,
                InitialFlashColor = Color.SkyBlue,
                CooldownAfterUse = 185,
                MoveBrake = 0.06f,
                Steps = new[]
                {
                    WolfSwing(ComboMotion.OverheadArc, 38, 27, pauseAfter: 8,
                        damageMult: 0.95f, forwardPushMult: 0.78f, reachMult: 1.18f),
                    WolfSwing(ComboMotion.UnderhandArc, 0, 29,
                        damageMult: 1.28f, forwardPushMult: 0.88f, reachMult: 1.22f),
                },
            },
            // Owl's Backstep Re-entry Chop.
            new MeleeCombo
            {
                Name = BackstepReentryName,
                BaseWeight = 50,
                Preferred = ComboRangeBand.Close,
                InitialFlashColor = Color.LightSteelBlue,
                CooldownAfterUse = 230,
                HeavyCommit = true,
                HyperArmor = true,
                MoveBrake = 0f,
                Steps = new[]
                {
                    WolfSwing(ComboMotion.BackstepRaise, 30, 70, pauseAfter: 7, damageMult: 0f),
                    WolfSwing(ComboMotion.OverheadArc, 0, 29,
                        damageMult: 1.42f, forwardPushMult: 1.05f, reachMult: 1.24f,
                        ease: SwingEaseStyle.Whip),
                },
            },
            // Owl's Greatfire Pursuit Slam: the low pounce gap-closer, gated to its reliable jump range.
            new MeleeCombo
            {
                Name = WolfPounceName,
                BaseWeight = 90,
                Preferred = ComboRangeBand.Mid,
                InitialFlashColor = Color.CornflowerBlue,
                CooldownAfterUse = 150,
                HeavyCommit = true,
                HyperArmor = true,
                RangedStartOnly = true,
                MoveBrake = 0.06f,
                Steps = new[] { PounceStep },
            },
            // Pack Hunt: Wolf Pounce that can chain a second pounce, once, if the first whiffs and the target is still
            // beyond PackHuntRepeatRange on landing (ShouldContinueMeleeCombo). Each pounce's strike is live <= 10t.
            new MeleeCombo
            {
                Name = PackHuntName,
                BaseWeight = 60,
                Preferred = ComboRangeBand.Far,
                InitialFlashColor = Color.DodgerBlue,
                CooldownAfterUse = 300,
                HeavyCommit = true,
                HyperArmor = true,
                RangedStartOnly = true,
                MoveBrake = 0.06f,
                Steps = new[]
                {
                    WolfSwing(ComboMotion.LeapSlam, 36, 82, pauseAfter: 10,
                        damageMult: 1.25f, reachMult: 1.25f,
                        leapHeightMult: 0.62f, leapForwardSpeedMult: 1.70f,
                        leapStrikeRange: 120f,
                        ease: SwingEaseStyle.Whip),
                    WolfSwing(ComboMotion.LeapSlam, 0, 82,
                        damageMult: 1.25f, reachMult: 1.25f,
                        leapHeightMult: 0.62f, leapForwardSpeedMult: 1.70f,
                        leapStrikeRange: 120f,
                        ease: SwingEaseStyle.Whip),
                },
            },
            // Studded's Leaping Uppercut: anti-air at a target above him (gated in CanSelectMeleeCombo).
            new MeleeCombo
            {
                Name = RisingFangName,
                BaseWeight = 120,
                Preferred = ComboRangeBand.Any,
                InitialFlashColor = Color.LightCyan,
                CooldownAfterUse = 210,
                HeavyCommit = true,
                MoveBrake = 0.08f,
                Steps = new[]
                {
                    WolfSwing(ComboMotion.RisingUppercutLeap, 34, 150,
                        damageMult: 1.22f, reachMult: 1.20f),
                },
            },
            // Owl's Closing Leap with a higher arc: a zero-damage hop onto the target's ledge.
            new MeleeCombo
            {
                Name = LedgeHopName,
                BaseWeight = 120,
                Preferred = ComboRangeBand.Any,
                InitialFlashColor = Color.LightSteelBlue,
                CooldownAfterUse = 130,
                RecoveryTicks = 10,
                RangedStartOnly = true,
                MoveBrake = 0.06f,
                Steps = new[] { WolfSwing(ComboMotion.LeapSlam, 18, 70,
                    damageMult: 0f,
                    leapHeightMult: 1.15f, leapForwardSpeedMult: 1.40f,
                    ease: SwingEaseStyle.Whip) },
            },
            // Studded's Apex Feint Leap: reads as the overhead, hangs at the apex a full second, then pounces.
            new MeleeCombo
            {
                Name = HuntersPatienceName,
                BaseWeight = 35,
                Preferred = ComboRangeBand.Close,
                InitialFlashColor = Color.LightGoldenrodYellow,
                CooldownAfterUse = 260,
                HeavyCommit = true,
                HyperArmor = true,
                MoveBrake = 0.35f,
                Steps = new[]
                {
                    WolfSwing(ComboMotion.Feint, 32, 60, pauseAfter: 6, damageMult: 0f),
                    WolfSwing(ComboMotion.LeapSlam, 0, 90, damageMult: 1.35f),
                },
            },
            // Owl's Delayed Heavy Chop at 2.2x: the finisher against a Suppressed target under 30% life.
            new MeleeCombo
            {
                Name = MoonlitExecutionName,
                BaseWeight = 200,
                Preferred = ComboRangeBand.Close,
                InitialFlashColor = Color.Aqua,
                CooldownAfterUse = 400,
                HeavyCommit = true,
                HyperArmor = true,
                MoveBrake = 0.48f,
                Steps = new[] { WolfSwing(ComboMotion.OverheadArc, 54, 32,
                    damageMult: 2.20f, forwardPushMult: 0.22f, reachMult: 1.28f,
                    ease: SwingEaseStyle.Whip) },
            },

            // ── Phase 1 only ──
            // Studded's Relentless Hew timings. Each step is live for its first 60% (HitWindowEnd) and steps 2-3 are
            // conditional, so the 5t pauses never chain an unconditional swing into a late roller's post-roll gap.
            new MeleeCombo
            {
                Name = HuntingTripleName,
                BaseWeight = 85,
                Preferred = ComboRangeBand.Close,
                InitialFlashColor = Color.Cyan,
                CooldownAfterUse = 180,
                MoveBrake = 0.08f,
                Steps = new[]
                {
                    WolfSwing(ComboMotion.OverheadArc, 28, 20, pauseAfter: 5,
                        damageMult: 0.72f, forwardPushMult: 0.58f, hitWindowEnd: 0.6f),
                    WolfSwing(ComboMotion.UnderhandArc, 0, 20, pauseAfter: 5,
                        damageMult: 0.78f, forwardPushMult: 0.68f, hitWindowEnd: 0.6f),
                    WolfSwing(ComboMotion.OverheadArc, 0, 24,
                        damageMult: 1.18f, forwardPushMult: 0.78f, reachMult: 1.16f, hitWindowEnd: 0.6f),
                },
            },
            // Owl's Delayed Heavy Chop.
            new MeleeCombo
            {
                Name = HeavyChopName,
                BaseWeight = 45,
                Preferred = ComboRangeBand.Close,
                InitialFlashColor = Color.RoyalBlue,
                CooldownAfterUse = 230,
                HeavyCommit = true,
                HyperArmor = true,
                MoveBrake = 0.48f,
                Steps = new[] { WolfSwing(ComboMotion.OverheadArc, 54, 32,
                    damageMult: 1.80f, forwardPushMult: 0.22f, reachMult: 1.28f,
                    ease: SwingEaseStyle.Whip) },
            },
            // Fang Thrust — replaces the legacy stab. Mid-range opener (gated 120-240px in CanSelectMeleeCombo).
            // Tell 30 x 1.2 = 36t. JoustDash on the authored clock: 24t at 2.8 x 1.6 = 4.5px/t ~108px of travel, so it
            // overshoots a standing target. Live 18t (HitWindowEnd 0.75, < 22 = rollable), then a harmless extension.
            new MeleeCombo
            {
                Name = FangThrustName,
                BaseWeight = 70,
                Preferred = ComboRangeBand.Mid,
                InitialFlashColor = Color.White,
                CooldownAfterUse = 200,
                RangedStartOnly = true,
                MoveBrake = 0f,
                Steps = new[] { WolfSwing(ComboMotion.JoustDash, 30, 24,
                    damageMult: 1.20f, forwardPushMult: 1.60f, reachMult: 1.20f,
                    hitWindowEnd: 0.75f, ease: SwingEaseStyle.Whip) },
            },
            // Queued only by the Shield Bash (CanSelectMeleeCombo / ReactiveComboIndex). See the bash timing note above.
            new MeleeCombo
            {
                Name = BashFollowThroughName,
                BaseWeight = 1,
                Preferred = ComboRangeBand.Close,
                InitialFlashColor = Color.LightBlue,
                CooldownAfterUse = 60,
                MoveBrake = 0.20f,
                Steps = new[] { WolfSwing(ComboMotion.OverheadArc, 22, 28,
                    damageMult: 1.15f, forwardPushMult: 0.50f, reachMult: 1.20f,
                    ease: SwingEaseStyle.Whip) },
            },

            // ── Phase 2 only (two-handed, no shield) ──
            // Feral Flurry: 4 alternating cuts that share endpoints. Tell 30 x 1.2 = 36t. Each live for its first 60%
            // (~14t), steps 2-4 conditional on a hit or the target still in reach — at most 4 rolls standing still,
            // and backing off ends it. Ends in a 70t exhausted recovery: the punish window for the whole string.
            new MeleeCombo
            {
                Name = FeralFlurryName,
                BaseWeight = 85,
                Preferred = ComboRangeBand.Close,
                InitialFlashColor = Color.DeepSkyBlue,
                CooldownAfterUse = 220,
                RecoveryTicks = 70,
                HeavyCommit = true,
                MoveBrake = 0.08f,
                Steps = new[]
                {
                    WolfSwing(ComboMotion.OverheadArc, 30, 24, pauseAfter: 8,
                        damageMult: 0.80f, forwardPushMult: 0.60f, hitWindowEnd: 0.6f),
                    WolfSwing(ComboMotion.UnderhandArc, 0, 24, pauseAfter: 8,
                        damageMult: 0.85f, forwardPushMult: 0.62f, hitWindowEnd: 0.6f),
                    WolfSwing(ComboMotion.OverheadArc, 0, 24, pauseAfter: 8,
                        damageMult: 0.90f, forwardPushMult: 0.66f, hitWindowEnd: 0.6f),
                    WolfSwing(ComboMotion.UnderhandArc, 0, 30,
                        damageMult: 1.30f, forwardPushMult: 0.78f, reachMult: 1.20f, hitWindowEnd: 0.6f,
                        ease: SwingEaseStyle.Whip),
                },
            },
            // Studded's Double Circle Slam: punishes standing behind him or rolling through.
            new MeleeCombo
            {
                Name = WolfWhirlName,
                BaseWeight = 50,
                Preferred = ComboRangeBand.Close,
                InitialFlashColor = Color.Aquamarine,
                CooldownAfterUse = 240,
                HeavyCommit = true,
                HyperArmor = true,
                MoveBrake = 0.40f,
                Steps = new[] { WolfSwing(ComboMotion.DoubleSpinSlam, 42, 54,
                    damageMult: 1.45f, forwardPushMult: 0.35f) },
            },
            // Owl's High Leaping Slam + blue ground waves on landing (OnComboStepCompleted).
            new MeleeCombo
            {
                Name = MoonriseSlamName,
                BaseWeight = 45,
                Preferred = ComboRangeBand.Mid,
                InitialFlashColor = Color.MediumSlateBlue,
                CooldownAfterUse = 360,
                HeavyCommit = true,
                HyperArmor = true,
                RangedStartOnly = true,
                MoveBrake = 0.15f,
                Steps = new[] { WolfSwing(ComboMotion.LeapSlam, 44, 115,
                    damageMult: 1.55f, reachMult: 1.28f,
                    leapHeightMult: 1.18f, leapForwardSpeedMult: 1.05f,
                    leapDescentGravityMult: 2.2f, leapApexRetargetStrength: 1f,
                    ease: SwingEaseStyle.Trapezoidal) },
            },
            // Howling Lunge: Fang Thrust at phase-2 speed (3.3 x 1.9 = 6.3px/t x 24t ~150px, catches a backpedal but
            // a roll beats the 18t live window), then an overhead that only follows a hit or a target still in reach.
            // Gated 140-300px.
            new MeleeCombo
            {
                Name = HowlingLungeName,
                BaseWeight = 70,
                Preferred = ComboRangeBand.Mid,
                InitialFlashColor = Color.LightSkyBlue,
                CooldownAfterUse = 240,
                HeavyCommit = true,
                RangedStartOnly = true,
                MoveBrake = 0f,
                Steps = new[]
                {
                    // 18t pause: the re-raise from the thrust into the overhead is ~120°, so it needs the time.
                    WolfSwing(ComboMotion.JoustDash, 30, 24, pauseAfter: 18,
                        damageMult: 1.15f, forwardPushMult: 1.90f, reachMult: 1.20f,
                        hitWindowEnd: 0.75f, ease: SwingEaseStyle.Whip),
                    WolfSwing(ComboMotion.OverheadArc, 0, 28,
                        damageMult: 1.30f, forwardPushMult: 0.55f, reachMult: 1.22f,
                        ease: SwingEaseStyle.Whip),
                },
            },
            // Owl's Greatfire Crescent: the overhead releases a turquoise moonflame wall along the ground at mid-step
            // (OnMeleeComboAttackTick). Gated to CrescentMaxTriggerRange, which the wave's 480px travel covers.
            new MeleeCombo
            {
                Name = MoonlightCrescentName,
                BaseWeight = 75,
                Preferred = ComboRangeBand.Far,
                InitialFlashColor = Color.Turquoise,
                CooldownAfterUse = 170,
                RangedStartOnly = true,
                MoveBrake = 0.36f,
                Steps = new[] { WolfSwing(ComboMotion.OverheadArc, 42, 30,
                    damageMult: 0.80f, reachMult: 1.16f,
                    ease: SwingEaseStyle.Whip) },
            },
        };

        protected override MeleeCombo[] MeleeComboPoolOverride => BlaiddCombos;

        // ── Combo engine tuning (OwlFather's values) ──────────────────────────────
        protected override float ComboTelegraphMultiplier
        {
            get
            {
                float multiplier = 1.20f;

                if (_wolfHowlBuffTicks > 0)
                {
                    multiplier *= WolfHowlTelegraphMult;
                }

                return multiplier;
            }
        }
        // Raised from the base 48px so Rising Fang can start at a target above him; ground cuts keep their own
        // GroundCutMaxRise gate in CanSelectMeleeCombo.
        protected override float ComboStartMaxTargetRise => RisingFangMaxRise;
        protected override float ComboTelegraphAdvanceSpeedMult => 0.85f;
        protected override float ComboTelegraphAdvanceStopDistance => MeleeRange * 0.62f;
        protected override float ComboMaxStartRange => 440f;
        protected override float MeleeEngageRange => MeleeRange * 0.85f;
        protected override int ClosingDistanceMaxTicks => 130;
        protected override float LeapAttackForwardSpeed => TopSpeed * 2.05f;
        protected override float LeapAttackMinimumForwardSpeed => 0.55f;
        protected override float LeapAttackTargetLeadTicks => 10f;
        protected override float LeapAttackAscentTrackingStrength => 0.10f;
        protected override float LeapLandingStandoff => 24f;
        protected override int MeleeComboChance => 100;
        protected override int RangedStartMeleeComboChance => 70;
        protected override int MeleeRecoveryTicks => 22;
        protected override int MeleeTelegraphTicks => 36;
        protected override Color MeleeTelegraphFlashColor => new Color(215, 225, 255);

        // ── Flask throw (the shared ranged phase; gated in AI) ────────────────────
        protected override float RangedRange => 640f;
        protected override float MinRangedRange => 140f;
        protected override int RangedTelegraphTicks => 40;
        protected override int RangedRecoveryTicks => 30;
        protected override int RangedCooldownAfterUse => FlaskCooldownTicks;
        protected override int MaxRangedBurst => 1;
        protected override int SingleRangedBurstChance => 100;
        protected override int StandingRangedChance => 100;
        protected override Color RangedTelegraphFlashColor => new Color(90, 160, 255);

        // ── Movement / stats ──────────────────────────────────────────────────────
        protected override float TopSpeed
        {
            get
            {
                float speed = 2.8f;

                if (_shieldDiscarded)
                {
                    speed = 3.3f;
                }

                if (_wolfHowlBuffTicks > 0)
                {
                    speed *= WolfHowlSpeedMult;
                }

                return speed;
            }
        }
        protected override float Acceleration => 0.10f;
        protected override float MeleeRange => 88f;
        protected override float StabRange => 170f; // unused — CanStab is false
        protected override bool CanStab => false;
        protected override bool SlowDownBeforeMelee => false;
        protected override int CasualStrollChance => 0;
        protected override int AbandonedDespawnTicks => 600;

        protected override int TeleportTelegraphTicks => 130;
        protected override int TeleportDustCount => 22;
        protected override Color TeleportDustTint => new Color(185, 185, 195);

        protected override float PuppetJumpPower => 10f;
        protected override float PuppetJumpBoost => 6f;

        // Unleash raises the sword overhead; the bash holds it low and back behind the shield.
        protected override float? CustomWeaponRotation
        {
            get
            {
                if (_customKind == BlaiddCustomKind.Unleash)
                {
                    return -1.4f;
                }

                if (_customKind == BlaiddCustomKind.ShieldBash)
                {
                    return 0.9f;
                }

                return null;
            }
        }

        protected override void RunMovementAI(float speedMult)
        {
            tsorcRevampGlobalNPC globalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            globalNPC.NavSearchRadius = 70;
            globalNPC.RemembersLastKnownPos = true;

            // Stalking: hold a 6-10 tile band, backpedalling while facing the target (GrandOccultist's levers).
            if (_stalkTicks > 0)
            {
                globalNPC.KiteRangeMin = StalkKiteMinTiles;
                globalNPC.KiteRangeMax = StalkKiteMaxTiles;
                globalNPC.KiteLooseness = 0f;
                globalNPC.CanWalkBackwards = true;
            }
            else
            {
                globalNPC.KiteRangeMin = 0f;
                globalNPC.KiteRangeMax = 0f;
                globalNPC.CanWalkBackwards = false;
            }

            // Hunter's Sprint: out of sight long enough -> run to the last known position, blue afterimage dust.
            bool huntersSprint = _noSightTicks >= HuntersSprintSightTicks && _huntersSprintTicks < HuntersSprintTicks;

            if (huntersSprint)
            {
                speedMult *= HuntersSprintSpeedMult;

                if (!Main.dedServ && Main.rand.NextBool(2))
                {
                    Dust trail = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, DustID.BlueTorch,
                        -NPC.velocity.X * 0.3f, 0f, 120, default, 1.2f);
                    trail.noGravity = true;
                }
            }

            SmartFighter4AI.Run(NPC,
                topSpeed: TopSpeed * speedMult,
                acceleration: Acceleration,
                doorBreakingDamage: 3,
                attackRange: 640f);
        }

        public override void AI()
        {
            if (_bashCooldown > 0)
            {
                _bashCooldown--;
            }

            if (_bashFollowUpWindow > 0)
            {
                _bashFollowUpWindow--;
            }

            if (_fightTicks < FlaskFirstThrowDelayTicks)
            {
                _fightTicks++;
            }

            if (_distantHitGuardTicks > 0)
            {
                _distantHitGuardTicks--;
            }

            if (_chargeCooldown > 0)
            {
                _chargeCooldown--;
            }

            if (_moonsPullCooldown > 0)
            {
                _moonsPullCooldown--;
            }

            if (_wolfHowlCooldown > 0)
            {
                _wolfHowlCooldown--;
            }

            if (_wolfHowlBuffTicks > 0)
            {
                _wolfHowlBuffTicks--;
            }

            if (_stalkFinisherWindow > 0)
            {
                _stalkFinisherWindow--;
            }

            // A stalk ending opens the gap-closer window (both synced, so every peer flips together).
            if (_stalkTicks > 0)
            {
                _stalkTicks--;

                if (_stalkTicks == 0)
                {
                    _stalkFinisherWindow = StalkFinisherWindowTicks;
                }
            }

            // Hunter's Sprint bookkeeping: line of sight to the target, from synced positions on every peer.
            if (NPC.HasValidTarget)
            {
                Player sightTarget = Main.player[NPC.target];
                bool canSeeTarget = Collision.CanHitLine(NPC.Center, 1, 1, sightTarget.Center, 1, 1);

                if (canSeeTarget)
                {
                    _noSightTicks = 0;
                    _huntersSprintTicks = 0;
                }
                else
                {
                    _noSightTicks++;

                    if (_noSightTicks >= HuntersSprintSightTicks)
                    {
                        _huntersSprintTicks++;
                    }
                }
            }

            // Moon's Pull input: how long each player has been off the ground. Only the server decides on it.
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                for (int i = 0; i < Main.maxPlayers; i++)
                {
                    Player candidate = Main.player[i];
                    bool airborne = candidate.active && !candidate.dead && candidate.velocity.Y != 0f;

                    if (airborne)
                    {
                        _playerAirborneTicks[i]++;
                    }
                    else
                    {
                        _playerAirborneTicks[i] = 0;
                    }
                }
            }

            bool freePhase = Phase == AttackPhase.Idle
                || Phase == AttackPhase.CasualStroll
                || Phase == AttackPhase.ClosingDistance;

            // Phase 2 trigger: driven by synced NPC.life, so every peer starts it on its own (no packet needed).
            // The flag is set as the transition STARTS so burst damage can't trigger it twice.
            bool belowPhaseTwo = NPC.life <= NPC.lifeMax * PhaseTwoHealthFraction;

            if (!_phaseTwoStarted && belowPhaseTwo && freePhase)
            {
                _phaseTwoStarted = true;
                BeginCustom(BlaiddCustomKind.Unleash);
                freePhase = false;
            }

            // Set-piece pick: a server decision, sent to clients through _customSerial. At most one per tick, in
            // priority order: Moon's Pull (a flier is escaping), Shield Charge (being shot at range), Wolf's Howl,
            // Shield Bash. Never while stalking — that is a deliberate wait.
            bool canPickSetPiece = Main.netMode != NetmodeID.MultiplayerClient
                && freePhase
                && _stalkTicks <= 0
                && NPC.velocity.Y == 0f
                && NPC.HasValidTarget;

            if (canPickSetPiece)
            {
                Player pickTarget = Main.player[NPC.target];
                float pickDistance = NPC.Distance(pickTarget.Center);
                float pickHeight = System.Math.Abs(pickTarget.Center.Y - NPC.Center.Y);
                bool pickClearLine = Collision.CanHitLine(NPC.Center, 1, 1, pickTarget.Center, 1, 1);

                // Moon's Pull: some unsuppressed player within range has been airborne long enough to be flying.
                bool flierEscaping = false;

                if (_moonsPullCooldown <= 0)
                {
                    int suppressedType = ModContent.BuffType<Suppressed>();

                    for (int i = 0; i < Main.maxPlayers; i++)
                    {
                        Player candidate = Main.player[i];
                        bool candidateValid = candidate.active && !candidate.dead;

                        if (!candidateValid)
                        {
                            continue;
                        }

                        bool flying = _playerAirborneTicks[i] >= MoonsPullAirborneTrigger;
                        bool inPullRange = candidate.Distance(NPC.Center) <= MoonsPullRange;

                        if (flying && inPullRange && !candidate.HasBuff(suppressedType))
                        {
                            flierEscaping = true;
                            break;
                        }
                    }
                }

                bool beingShot = _distantHitGuardTicks > 0 || TargetShootingFromRange(pickTarget, pickDistance);
                bool chargeInRange = pickDistance >= ChargeMinRange && pickDistance <= ChargeMaxRange;
                bool canCharge = !_shieldDiscarded && _chargeCooldown <= 0 && chargeInRange && pickClearLine && beingShot;
                bool canHowl = _shieldDiscarded && _wolfHowlCooldown <= 0 && pickDistance >= 120f && pickClearLine;
                bool bashInRange = pickDistance >= BashTriggerMinRange && pickDistance <= BashTriggerMaxRange;
                bool canBash = !_shieldDiscarded && _bashCooldown <= 0 && bashInRange && pickHeight < 40f && pickClearLine;

                BlaiddCustomKind pick = BlaiddCustomKind.None;

                if (flierEscaping)
                {
                    pick = BlaiddCustomKind.MoonsPull;
                }
                else if (canCharge)
                {
                    pick = BlaiddCustomKind.ShieldCharge;
                }
                else if (canHowl)
                {
                    pick = BlaiddCustomKind.WolfHowl;
                }
                else if (canBash)
                {
                    pick = BlaiddCustomKind.ShieldBash;
                }

                if (pick != BlaiddCustomKind.None)
                {
                    BeginCustom(pick);
                    NPC.netUpdate = true;
                }
            }

            // Flask gate: hold the shared ranged cooldown shut until it would actually suppress someone. Buffs sync to
            // every peer, so each evaluates the same answer.
            bool flaskWorthThrowing = false;

            if (_fightTicks >= FlaskFirstThrowDelayTicks)
            {
                int suppressedType = ModContent.BuffType<Suppressed>();

                for (int i = 0; i < Main.maxPlayers; i++)
                {
                    Player candidate = Main.player[i];
                    bool candidateValid = candidate.active && !candidate.dead;

                    if (!candidateValid)
                    {
                        continue;
                    }

                    bool inRingReach = candidate.Distance(NPC.Center) <= BlaiddSuppressionBurst.MaxRadius;

                    if (inRingReach && !candidate.HasBuff(suppressedType))
                    {
                        flaskWorthThrowing = true;
                        break;
                    }
                }
            }

            if (!flaskWorthThrowing && _rangedCooldown < 2)
            {
                _rangedCooldown = 2;
            }

            base.AI();

            // Hyper armor that the base commit flags don't cover, applied after base.AI() so UpdateAttackCommitFlags
            // can't overwrite it: the whole flask throw (unstaggerable by design), the Unleash set-piece, and the
            // bash's tell + lunge (its recovery stays open as the punish window).
            bool flaskPhase = Phase == AttackPhase.RangedTelegraph || Phase == AttackPhase.RangedAttack;
            bool unleashing = Phase == AttackPhase.Custom && _customKind == BlaiddCustomKind.Unleash;
            bool bashCommitted = Phase == AttackPhase.Custom
                && _customKind == BlaiddCustomKind.ShieldBash
                && _customElapsed <= BashTellTicks + BashLungeTicks;

            if (flaskPhase || unleashing || bashCommitted)
            {
                tsorcRevampGlobalNPC globalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
                globalNPC.AttackCommitted = true;
                globalNPC.AttackTelegraphing = false;
            }

            if (Phase != AttackPhase.Custom)
            {
                _customKind = BlaiddCustomKind.None;
            }

            // Stalking roll: in phase 2, coming back to neutral after an attack sometimes starts a stalk.
            bool freeNow = Phase == AttackPhase.Idle
                || Phase == AttackPhase.CasualStroll
                || Phase == AttackPhase.ClosingDistance;
            bool justFinishedAttack = freeNow && !_wasFreePhase;
            _wasFreePhase = freeNow;

            if (justFinishedAttack && _shieldDiscarded && _stalkTicks <= 0 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                if (Main.rand.Next(100) < StalkChancePercent)
                {
                    _stalkTicks = Main.rand.Next(StalkMinTicks, StalkMaxTicks + 1);
                    NPC.netUpdate = true;
                }
            }

            // Shield Deflect: friendly projectiles entering the shield's front face are destroyed (CanBeHitByProjectile
            // already stops their damage). Each projectile's owner kills it, so the kill syncs normally; sparks and
            // the clang play on every client. Minions, sentries and pets are only blocked, never destroyed.
            if (ShieldRaised)
            {
                Rectangle shieldFace = new Rectangle((int)(NPC.Center.X + NPC.direction * 14f) - 12, (int)NPC.position.Y - 4, 24, NPC.height + 8);

                for (int i = 0; i < Main.maxProjectiles; i++)
                {
                    Projectile incoming = Main.projectile[i];
                    bool deflectable = incoming.active && incoming.friendly && !incoming.hostile && incoming.damage > 0
                        && !incoming.minion && !incoming.sentry && !Main.projPet[incoming.type];

                    if (!deflectable || !incoming.Hitbox.Intersects(shieldFace))
                    {
                        continue;
                    }

                    bool fromFront = System.Math.Sign(incoming.Center.X - NPC.Center.X) == NPC.direction;

                    if (!fromFront)
                    {
                        continue;
                    }

                    if (!Main.dedServ)
                    {
                        SoundEngine.PlaySound(SoundID.NPCHit4 with { Volume = 0.6f, PitchVariance = 0.2f }, incoming.Center);

                        for (int j = 0; j < 8; j++)
                        {
                            Dust spark = Dust.NewDustPerfect(incoming.Center, DustID.SilverFlame, Main.rand.NextVector2Circular(3f, 3f), 0, default, 1.1f);
                            spark.noGravity = true;
                        }
                    }

                    if (incoming.owner == Main.myPlayer)
                    {
                        incoming.Kill();
                    }
                }
            }

            // Phase 2 identity: a faint blue moonlight mist off the body, stronger while Wolf's Howl is active.
            int mistChance = 4;

            if (_wolfHowlBuffTicks > 0)
            {
                mistChance = 1;
            }

            if (_shieldDiscarded && !Main.dedServ && Main.rand.NextBool(mistChance))
            {
                Dust moonlight = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, DustID.BlueTorch, 0f, -1f, 120, default, 1.1f);
                moonlight.noGravity = true;
            }
        }

        /// <summary>The target is using a non-melee weapon from beyond DistantHitRange. Inputs are synced player
        /// state, so every peer agrees.</summary>
        private bool TargetShootingFromRange(Player target, float distance)
        {
            Item heldItem = target.HeldItem;
            bool usingWeapon = target.itemAnimation > 0 && heldItem.damage > 0;
            bool nonMeleeWeapon = !heldItem.DamageType.CountsAsClass(DamageClass.Melee);
            return usingWeapon && nonMeleeWeapon && distance > DistantHitRange;
        }

        // The Shield Charge sprint drives its own movement; everything else plants like a channel.
        protected override bool SlowDownDuringCustom
        {
            get
            {
                bool sprinting = _customKind == BlaiddCustomKind.ShieldCharge
                    && _customElapsed > ChargeTellTicks
                    && _customElapsed <= ChargeTellTicks + ChargeSprintTicks;
                return !sprinting;
            }
        }

        /// <summary>Enters one of Blaidd's Custom set-pieces on this peer. The server bumps _customSerial so clients
        /// that receive it in ReceiveExtraAI start the same set-piece.</summary>
        private void BeginCustom(BlaiddCustomKind kind)
        {
            _customStartKind = kind;
            _customKind = kind;
            _customElapsed = 0;
            _customSerial++;

            int duration = UnleashTicks;
            int poseWeapon = MeleeWeaponItemType;

            if (kind == BlaiddCustomKind.ShieldBash)
            {
                duration = BashTellTicks + BashLungeTicks + BashRecoveryTicks;
                _bashCooldown = BashCooldownTicks;
            }
            else if (kind == BlaiddCustomKind.ChargeBash)
            {
                // The charge already was the tell: enter the bash at its lunge.
                _customKind = BlaiddCustomKind.ShieldBash;
                _customElapsed = BashTellTicks;
                duration = BashLungeTicks + BashRecoveryTicks;
                _bashCooldown = BashCooldownTicks;
            }
            else if (kind == BlaiddCustomKind.ShieldCharge)
            {
                duration = ChargeTellTicks + ChargeSprintTicks + ChargeWindedTicks;
                _chargeCooldown = ChargeCooldownTicks;
            }
            else if (kind == BlaiddCustomKind.MoonsPull)
            {
                duration = MoonsPullTellTicks + MoonsPullTicks;
                _moonsPullCooldown = MoonsPullCooldownTicks;
            }
            else if (kind == BlaiddCustomKind.WolfHowl)
            {
                duration = WolfHowlTicks;
                _wolfHowlCooldown = WolfHowlCooldownTicks;
            }

            StartCustomAttack(duration, poseWeapon, swingPose: true);
        }

        protected override void DoCustomAttack()
        {
            if (Main.dedServ)
            {
                return;
            }

            if (_customKind == BlaiddCustomKind.ShieldBash || _customKind == BlaiddCustomKind.ShieldCharge)
            {
                SoundEngine.PlaySound(SoundID.Item37 with { Volume = 0.8f, Pitch = -0.3f }, NPC.Center);
            }
            else if (_customKind == BlaiddCustomKind.Unleash)
            {
                SoundEngine.PlaySound(SoundID.Item119 with { Volume = 0.8f, Pitch = -0.4f }, NPC.Center);
            }
            else if (_customKind == BlaiddCustomKind.MoonsPull || _customKind == BlaiddCustomKind.WolfHowl)
            {
                SoundEngine.PlaySound(SoundID.Roar with { Volume = 0.85f, Pitch = -0.2f }, NPC.Center);
            }
        }

        protected override void DoCustomTick(int ticksRemaining)
        {
            int elapsed = _customElapsed;
            _customElapsed++;

            if (_customKind == BlaiddCustomKind.ShieldBash)
            {
                TickShieldBash(elapsed);
            }
            else if (_customKind == BlaiddCustomKind.Unleash)
            {
                TickUnleash(elapsed);
            }
            else if (_customKind == BlaiddCustomKind.ShieldCharge)
            {
                TickShieldCharge(elapsed);
            }
            else if (_customKind == BlaiddCustomKind.MoonsPull)
            {
                TickMoonsPull(elapsed);
            }
            else if (_customKind == BlaiddCustomKind.WolfHowl)
            {
                // Wolf's Howl: head-back howl with moonlight converging (no damage, punishable). The buff lands on
                // the final tick so interrupting it with a stagger cancels the buff too.
                if (!Main.dedServ)
                {
                    Vector2 offset = Main.rand.NextVector2CircularEdge(60f, 60f);
                    Dust swirl = Dust.NewDustPerfect(NPC.Center + offset, DustID.IceTorch, -offset * 0.07f, 80, default, 1.5f);
                    swirl.noGravity = true;
                }

                if (ticksRemaining == 1)
                {
                    _wolfHowlBuffTicks = WolfHowlBuffTicks;

                    if (!Main.dedServ)
                    {
                        UsefulFunctions.ScreenShake(NPC.Center, 2.5f, 12, distanceFalloff: 600f);
                    }
                }
            }
        }

        // Shield Bash beats, by elapsed tick: [0, tell) cocked + flash, [tell, tell+lunge) distance-solved shove with a
        // hitbox every BashHitboxInterval ticks, then planted recovery (cut short when the follow-through is queued).
        private void TickShieldBash(int elapsed)
        {
            int lungeStart = BashTellTicks;
            int lungeEnd = BashTellTicks + BashLungeTicks;
            Player target = Main.player[NPC.target];

            if (elapsed == 0)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    SpawnTelegraphFlash(new Color(170, 210, 255));
                }
            }

            if (elapsed < lungeStart)
            {
                // Facing tracks the target through the tell; the lunge commits to it.
                NPC.direction = target.Center.X < NPC.Center.X ? -1 : 1;
                NPC.spriteDirection = NPC.direction;

                if (!Main.dedServ && Main.rand.NextBool(2))
                {
                    Vector2 shieldSide = NPC.Center + new Vector2(NPC.direction * 10f, 0f);
                    Dust glint = Dust.NewDustPerfect(shieldSide + Main.rand.NextVector2Circular(8f, 14f), DustID.SilverFlame,
                        Vector2.Zero, 80, default, 1.1f);
                    glint.noGravity = true;
                }

                return;
            }

            if (elapsed == lungeStart)
            {
                // Solve the shove so it arrives BashArriveDistance short of the target, clamped to a sane travel.
                float gap = System.Math.Abs(target.Center.X - NPC.Center.X) - BashArriveDistance;
                float travel = MathHelper.Clamp(gap, BashMinTravel, BashMaxTravel);
                _bashLungeSpeed = travel / BashLungeTicks;

                if (!Main.dedServ)
                {
                    SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.9f, Pitch = -0.5f }, NPC.Center);
                }
            }

            if (elapsed < lungeEnd)
            {
                NPC.velocity.X = NPC.direction * _bashLungeSpeed;
                int lungeTick = elapsed - lungeStart;
                bool spawnHitbox = lungeTick % BashHitboxInterval == 0;

                if (spawnHitbox && Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Vector2 hitboxCenter = NPC.Center + new Vector2(NPC.direction * 22f, 0f);
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), hitboxCenter, Vector2.Zero,
                        ModContent.ProjectileType<PuppetMeleeHitbox>(), (int)(MeleeDamage * BashDamageMult), BashKnockback,
                        Main.myPlayer, 36f, 44f);
                }

                if (!Main.dedServ)
                {
                    Dust dust = Dust.NewDustDirect(NPC.Bottom - new Vector2(10f, 4f), 20, 4, DustID.Smoke, -NPC.direction * 1.5f, -0.5f, 120, default, 1f);
                    dust.noGravity = true;
                }

                return;
            }

            if (elapsed == lungeEnd)
            {
                if (!Main.dedServ)
                {
                    UsefulFunctions.ScreenShake(NPC.Center, 2f, 6, distanceFalloff: 400f);
                }

                // Follow-through only if the target is still in reach. Cutting the recovery to 12t keeps the queued
                // overhead's first live tick 38t+ after the bash went harmless (see the timing note at the top).
                // Every peer shortens its own recovery; only the server queues the combo. (+1: base decrements after.)
                float reach = ComboReachBase * 0.7f * 1.2f + FollowUpPushAllowance;
                bool stillInReach = NPC.Distance(target.Center) <= reach;

                if (stillInReach)
                {
                    PhaseTimer = BashFollowUpRecoveryTicks + 1;
                }

                if (stillInReach && Main.netMode != NetmodeID.MultiplayerClient)
                {
                    _bashFollowUpWindow = BashFollowUpWindowTicks;
                    NPC.netUpdate = true;
                }
            }
        }

        // Unleash beats: sword raised from frame 0, shield torn off and hurled at UnleashShieldThrowTick (the visual
        // swap to two-handed happens then), howl + shake at UnleashHowlTick, moonlight swirl throughout.
        private void TickUnleash(int elapsed)
        {
            if (elapsed == 0 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                SpawnTelegraphFlash(new Color(120, 170, 255));
            }

            if (elapsed == UnleashShieldThrowTick)
            {
                _shieldDiscarded = true;

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    float throwDirection = -NPC.direction;
                    Vector2 throwVelocity = new Vector2(throwDirection * 5f, -6f);
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, throwVelocity,
                        ModContent.ProjectileType<BlaiddDiscardedShield>(), 0, 0f, Main.myPlayer, throwDirection);
                }

                if (!Main.dedServ)
                {
                    SoundEngine.PlaySound(SoundID.Item52 with { Volume = 0.9f, Pitch = -0.4f }, NPC.Center);
                }
            }

            if (elapsed == UnleashHowlTick && !Main.dedServ)
            {
                SoundEngine.PlaySound(SoundID.Roar with { Volume = 0.9f, Pitch = -0.3f }, NPC.Center);
                UsefulFunctions.ScreenShake(NPC.Center, 4f, 18, distanceFalloff: 700f);
            }

            if (Main.dedServ)
            {
                return;
            }

            // Moonlight converging onto the body: the "big change coming" grammar.
            for (int i = 0; i < 3; i++)
            {
                Vector2 offset = Main.rand.NextVector2CircularEdge(70f, 70f);
                Vector2 inward = -offset * 0.06f;
                int dustType = DustID.BlueTorch;

                if (i == 0)
                {
                    dustType = DustID.IceTorch;
                }

                Dust swirl = Dust.NewDustPerfect(NPC.Center + offset, dustType, inward, 80, default, 1.5f);
                swirl.noGravity = true;
            }
        }

        // Shield Charge beats: [0, tell) planted behind the raised shield, white flash + dust; [tell, tell+sprint) run
        // at ChargeSpeedMult through the normal pathing with the shield raised (ShieldRaised); reaching ChargeBashRange
        // converts to the bash lunge (server decides, clients follow the serial). Missing leaves a winded recovery.
        private void TickShieldCharge(int elapsed)
        {
            int sprintEnd = ChargeTellTicks + ChargeSprintTicks;
            Player target = Main.player[NPC.target];

            if (elapsed == 0 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                SpawnTelegraphFlash(new Color(170, 210, 255));
            }

            if (elapsed < ChargeTellTicks)
            {
                NPC.direction = target.Center.X < NPC.Center.X ? -1 : 1;
                NPC.spriteDirection = NPC.direction;

                if (!Main.dedServ && Main.rand.NextBool(2))
                {
                    Dust glint = Dust.NewDustPerfect(NPC.Center + new Vector2(NPC.direction * 10f, Main.rand.NextFloat(-14f, 14f)),
                        DustID.SilverFlame, Vector2.Zero, 80, default, 1.1f);
                    glint.noGravity = true;
                }

                return;
            }

            if (elapsed < sprintEnd)
            {
                RunMovementAI(ChargeSpeedMult);

                if (!Main.dedServ)
                {
                    Dust kick = Dust.NewDustDirect(NPC.Bottom - new Vector2(10f, 4f), 20, 4, DustID.Smoke,
                        -NPC.velocity.X * 0.3f, -0.5f, 120, default, 1.1f);
                    kick.noGravity = true;
                }

                float distance = NPC.Distance(target.Center);
                bool reached = distance <= ChargeBashRange && NPC.velocity.Y == 0f;

                if (reached && Main.netMode != NetmodeID.MultiplayerClient)
                {
                    BeginCustom(BlaiddCustomKind.ChargeBash);
                    NPC.netUpdate = true;
                }

                return;
            }

            // Winded: shield lowered, heaving breath. The punish window for a charge that missed.
            if (elapsed == sprintEnd && !Main.dedServ)
            {
                SoundEngine.PlaySound(SoundID.Item16 with { Volume = 0.5f, Pitch = -0.6f }, NPC.Center);
            }

            if (!Main.dedServ && Main.rand.NextBool(4))
            {
                Dust breath = Dust.NewDustPerfect(NPC.Top + new Vector2(NPC.direction * 6f, 8f), DustID.Smoke,
                    new Vector2(NPC.direction * 0.6f, -0.4f), 150, default, 0.9f);
                breath.noGravity = true;
            }
        }

        // Moon's Pull beats: [0, tell) the howl, moonlight converging (no hyper armor: free hits); then MoonsPullTicks
        // of extra fall acceleration on THIS client's player if airborne, unsuppressed and in range. Each client owns
        // its player's velocity, so this runs everywhere and only touches Main.LocalPlayer. Capped at MoonsPullMaxFall.
        private void TickMoonsPull(int elapsed)
        {
            if (Main.dedServ)
            {
                return;
            }

            if (elapsed < MoonsPullTellTicks)
            {
                for (int i = 0; i < 2; i++)
                {
                    Vector2 offset = Main.rand.NextVector2CircularEdge(80f, 80f);
                    Dust swirl = Dust.NewDustPerfect(NPC.Center + offset, DustID.BlueTorch, -offset * 0.06f, 80, default, 1.5f);
                    swirl.noGravity = true;
                }

                return;
            }

            if (elapsed == MoonsPullTellTicks)
            {
                UsefulFunctions.ScreenShake(NPC.Center, 3f, 14, distanceFalloff: MoonsPullRange);
            }

            Player localPlayer = Main.LocalPlayer;
            bool localValid = localPlayer.active && !localPlayer.dead;
            bool inRange = localPlayer.Distance(NPC.Center) <= MoonsPullRange;
            bool airborne = localPlayer.velocity.Y != 0f;
            bool suppressed = localPlayer.HasBuff(ModContent.BuffType<Suppressed>());

            if (!localValid || !inRange || !airborne || suppressed)
            {
                return;
            }

            localPlayer.velocity.Y = System.Math.Min(localPlayer.velocity.Y + MoonsPullGravity, MoonsPullMaxFall);

            if (Main.rand.NextBool(2))
            {
                Dust drag = Dust.NewDustDirect(localPlayer.position, localPlayer.width, localPlayer.height, DustID.BlueTorch,
                    0f, 2f, 100, default, 1.2f);
                drag.noGravity = true;
            }
        }

        // ── Combo selection / continuation ────────────────────────────────────────
        protected override bool CanSelectMeleeCombo(MeleeCombo combo, float distance, float healthFraction)
        {
            if (combo.Name == BashFollowThroughName)
            {
                return _bashFollowUpWindow > 0;
            }

            bool phaseTwoOnly = combo.Name == FeralFlurryName
                || combo.Name == WolfWhirlName
                || combo.Name == MoonriseSlamName
                || combo.Name == HowlingLungeName
                || combo.Name == MoonlightCrescentName;
            bool phaseOneOnly = combo.Name == HuntingTripleName
                || combo.Name == HeavyChopName
                || combo.Name == FangThrustName;

            if (phaseTwoOnly && !_shieldDiscarded)
            {
                return false;
            }

            if (phaseOneOnly && _shieldDiscarded)
            {
                return false;
            }

            // Stalking is a deliberate wait: no combo starts. When it ends, only a committed gap-closer may.
            if (_stalkTicks > 0)
            {
                return false;
            }

            bool stalkGapCloser = combo.Name == HowlingLungeName
                || combo.Name == WolfPounceName
                || combo.Name == PackHuntName;

            if (_stalkFinisherWindow > 0 && !stalkGapCloser)
            {
                return false;
            }

            Player target = Main.player[NPC.target];
            float rise = NPC.Center.Y - target.Center.Y;
            float horizontal = System.Math.Abs(target.Center.X - NPC.Center.X);
            bool clearLine = Collision.CanHitLine(NPC.Center, 1, 1, target.Center, 1, 1);

            // Anti-air. Rising Fang: a target above him and close horizontally. Ledge Hop: a target on a ledge
            // farther out, within the leap's reliable range.
            if (combo.Name == RisingFangName)
            {
                bool aboveInBand = rise > GroundCutMaxRise && rise <= RisingFangMaxRise;
                return aboveInBand && horizontal <= RisingFangMaxHorizontal && clearLine;
            }

            if (combo.Name == LedgeHopName)
            {
                bool onLedge = rise > GroundCutMaxRise && rise <= LedgeHopMaxRise;
                bool fartherOut = horizontal > RisingFangMaxHorizontal;
                return onLedge && fartherOut && distance <= ReliableJumpStartRange(combo.Steps[0]);
            }

            // Everything else is a ground move: never at a target standing well above him.
            if (rise > GroundCutMaxRise)
            {
                return false;
            }

            // Finisher on a Suppressed target under ExecutionLifeFraction (its weight of 200 makes it the likely pick).
            bool executionTarget = target.HasBuff(ModContent.BuffType<Suppressed>())
                && target.statLife < target.statLifeMax2 * ExecutionLifeFraction;

            if (combo.Name == MoonlitExecutionName)
            {
                return executionTarget && clearLine;
            }

            // Leaps only from where they reliably land, so they never come down short.
            bool jumpGapCloser = combo.Name == WolfPounceName
                || combo.Name == MoonriseSlamName
                || combo.Name == PackHuntName;

            if (jumpGapCloser)
            {
                return distance <= ReliableJumpStartRange(combo.Steps[0]);
            }

            if (combo.Name == FangThrustName)
            {
                return distance >= 120f && distance <= 240f;
            }

            if (combo.Name == HowlingLungeName)
            {
                return distance >= 140f && distance <= 300f;
            }

            if (combo.Name == MoonlightCrescentName)
            {
                return distance <= CrescentMaxTriggerRange && clearLine;
            }

            // Ground cuts never start through a wall.
            return clearLine;
        }

        protected override int ReactiveComboIndex(float dist, ComboRangeBand band, int[] ready)
        {
            if (_bashFollowUpWindow <= 0)
            {
                return -1;
            }

            for (int i = 0; i < BlaiddCombos.Length && i < ready.Length; i++)
            {
                if (BlaiddCombos[i].Name == BashFollowThroughName && ready[i] > 0)
                {
                    _bashFollowUpWindow = 0;
                    return i;
                }
            }

            return -1;
        }

        protected override bool ShouldContinueMeleeCombo(
            string comboName, int nextStepIndex, Player target, bool previousStepHit)
        {
            // Conditional follow-ups: a hit always earns the next swing; a whiff only does if the next swing's reach
            // plus its push could still land from here. A clean roll-away ends the combo instead of swinging at air.
            bool conditionalCombo = comboName == WolfDownUpName
                || comboName == WolfUpDownName
                || comboName == HuntingTripleName
                || comboName == FeralFlurryName
                || comboName == HowlingLungeName;

            // Pack Hunt's second pounce: only after a whiff, with the target still out beyond PackHuntRepeatRange but
            // inside the pounce's reliable range.
            if (comboName == PackHuntName)
            {
                float targetDistance = NPC.Distance(target.Center);
                bool stillFar = targetDistance > PackHuntRepeatRange;
                bool reachable = targetDistance <= ReliableJumpStartRange(PounceStep);
                return !previousStepHit && stillFar && reachable;
            }

            if (conditionalCombo)
            {
                float reach = ComboReachBase * 0.7f * 1.2f + FollowUpPushAllowance;
                bool stillInReach = NPC.Distance(target.Center) <= reach;
                return previousStepHit || stillInReach;
            }

            return base.ShouldContinueMeleeCombo(comboName, nextStepIndex, target, previousStepHit);
        }

        // A fully whiffed Wolf cut sprints back in (ClosingDistance) and opens with the mirror pair once per chain.
        protected override int MeleeFollowUpChancePercent(int followUpsDone) => followUpsDone == 0 ? 60 : 0;

        protected override string MeleeFollowUpComboName(string finishedComboName)
        {
            if (finishedComboName == WolfDownUpName)
            {
                return WolfUpDownName;
            }

            if (finishedComboName == WolfUpDownName)
            {
                return WolfDownUpName;
            }

            return null;
        }

        // Moonlight Crescent releases its wave at mid-step (Owl's Greatfire Crescent beat), from the ground in front.
        protected override void OnMeleeComboAttackTick(MeleeCombo combo, MeleeComboStep step, int elapsed, int total)
        {
            base.OnMeleeComboAttackTick(combo, step, elapsed, total);

            bool releaseTick = combo.Name == MoonlightCrescentName && elapsed == total / 2;

            if (!releaseTick)
            {
                return;
            }

            if (!Main.dedServ)
            {
                SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.62f, Pitch = 0.25f }, NPC.Center);
            }

            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            int direction = 1;

            if (NPC.direction < 0)
            {
                direction = -1;
            }

            float startX = NPC.Bottom.X + direction * 22f;

            if (!PuppetGroundDustWave.TryFindGroundY(startX, NPC.Bottom.Y, out float groundY))
            {
                return;
            }

            int waveDamage = (int)(MeleeDamage * step.DamageMult * 0.5f);
            Projectile.NewProjectile(NPC.GetSource_FromThis(), new Vector2(startX, groundY - 2f),
                new Vector2(direction * BlaiddMoonlightCrescent.TravelSpeed, 0f),
                ModContent.ProjectileType<BlaiddMoonlightCrescent>(), waveDamage, 3f, Main.myPlayer, direction);
        }

        protected override void OnComboStepCompleted(MeleeComboStep step)
        {
            bool landed = step.Motion == ComboMotion.LeapSlam && NPC.velocity.Y == 0f;

            if (!landed)
            {
                return;
            }

            bool moonrise = ActiveMeleeComboName == MoonriseSlamName;

            if (!Main.dedServ)
            {
                float shake = 2.5f;

                if (moonrise)
                {
                    shake = 4f;
                }

                UsefulFunctions.ScreenShake(NPC.Bottom, shake, 10, distanceFalloff: 560f);
                SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.45f, Pitch = 0.1f }, NPC.Bottom);

                for (int i = 0; i < 16; i++)
                {
                    Vector2 velocity = new Vector2(Main.rand.NextFloat(-4f, 4f), Main.rand.NextFloat(-4.5f, -0.6f));
                    Dust burst = Dust.NewDustPerfect(NPC.Bottom + new Vector2(Main.rand.NextFloat(-18f, 18f), -2f),
                        DustID.BlueTorch, velocity, 80, default, Main.rand.NextFloat(1.1f, 1.6f));
                    burst.noGravity = true;
                }
            }

            if (moonrise && Main.netMode != NetmodeID.MultiplayerClient)
            {
                int waveType = ModContent.ProjectileType<BlaiddMoonWave>();
                int waveDamage = (int)(MeleeDamage * 0.4f);

                for (int direction = -1; direction <= 1; direction += 2)
                {
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Bottom - new Vector2(0f, 2f),
                        new Vector2(direction * BlaiddMoonWave.TravelSpeed, 0f), waveType, waveDamage, 2f,
                        Main.myPlayer, direction);
                }
            }
        }

        // ── Netcode ───────────────────────────────────────────────────────────────
        public override void SendExtraAI(BinaryWriter writer)
        {
            base.SendExtraAI(writer);
            writer.Write(_phaseTwoStarted);
            writer.Write(_shieldDiscarded);

            // Only a set-piece still running is worth starting on a client; a finished one sends None.
            BlaiddCustomKind runningStartKind = BlaiddCustomKind.None;

            if (Phase == AttackPhase.Custom)
            {
                runningStartKind = _customStartKind;
            }

            writer.Write((byte)runningStartKind);
            writer.Write(_customSerial);
            writer.Write((short)_bashCooldown);
            writer.Write((short)_bashFollowUpWindow);
            writer.Write((short)_fightTicks);
            writer.Write((short)_distantHitGuardTicks);
            writer.Write((short)_chargeCooldown);
            writer.Write((short)_moonsPullCooldown);
            writer.Write((short)_wolfHowlCooldown);
            writer.Write((short)_wolfHowlBuffTicks);
            writer.Write((short)_stalkTicks);
            writer.Write((short)_stalkFinisherWindow);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            base.ReceiveExtraAI(reader);
            _phaseTwoStarted = reader.ReadBoolean();
            _shieldDiscarded = reader.ReadBoolean();
            BlaiddCustomKind receivedKind = (BlaiddCustomKind)reader.ReadByte();
            byte receivedSerial = reader.ReadByte();
            _bashCooldown = reader.ReadInt16();
            _bashFollowUpWindow = reader.ReadInt16();
            _fightTicks = reader.ReadInt16();
            _distantHitGuardTicks = reader.ReadInt16();
            _chargeCooldown = reader.ReadInt16();
            _moonsPullCooldown = reader.ReadInt16();
            _wolfHowlCooldown = reader.ReadInt16();
            _wolfHowlBuffTicks = reader.ReadInt16();
            _stalkTicks = reader.ReadInt16();
            _stalkFinisherWindow = reader.ReadInt16();

            // A set-piece the server started that this client hasn't: start it here too. BeginCustom bumps the local
            // serial, so set it to the received value afterwards.
            bool newSetPiece = receivedSerial != _customSerial && receivedKind != BlaiddCustomKind.None;

            if (newSetPiece)
            {
                BeginCustom(receivedKind);
            }

            _customSerial = receivedSerial;
        }

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 1;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Confused] = true;
        }

        public override void SetDefaults()
        {
            NPC.width = 20;
            NPC.height = 42;
            NPC.lifeMax = 3000;
            NPC.defense = 35;
            NPC.damage = 0; // all damage comes from the weapon hitbox
            NPC.knockBackResist = 0.18f;
            NPC.aiStyle = -1;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath2;
            NPC.value = 12000f;
            NPC.boss = true;
            NPC.npcSlots = 10f;

            tsorcRevampGlobalNPC globalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            globalNPC.PoiseMax = 38f;
            globalNPC.PoiseStaggerResetsAI = true;
            globalNPC.NavGiveUpTicks = 180;
            globalNPC.CanUseRopes = true;
            globalNPC.CanTeleport = true;
            globalNPC.TeleportStyle = TeleportStyle.Aggressive;
            globalNPC.TeleportVisualStyle = TeleportVisualStyle.GreySmoke;
            // Giving up: walk back to the arena spawn, then AbandonedDespawnTicks ends the encounter. A player within
            // 60 tiles with a complete route re-aggroes him (OwlFather's setup).
            globalNPC.PatrolMode = PatrolMode.ReturnToSpawn;
            globalNPC.RouteReaggroRange = 60 * 16f;
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<DarkSoulItem>(), 1, 400, 600));
        }

        public override void OnKill()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            Terraria.ModLoader.Config.NPCDefinition definition = new(ModContent.NPCType<Blaidd>());
            if (!tsorcRevampWorld.NewSlain.ContainsKey(definition))
            {
                Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<global::tsorcRevamp.Content.Items.StaminaVessel>());
                tsorcRevampWorld.NewSlain.Add(definition, 1);

                if (Main.netMode == NetmodeID.Server)
                {
                    NetMessage.SendData(MessageID.WorldData);
                }
            }
        }

        // Runs on the HIT player's machine, only when a PuppetMeleeHitbox actually damages them (so a roll that
        // dodges the hit dodges the debuff too). Both blade and bash hits come through here; the bash is told apart
        // by this client's copy of the set-piece state, which is synced through _customSerial.
        public void OnHumanoidMeleeHit(Player target)
        {
            bool bashHit = Phase == AttackPhase.Custom && _customKind == BlaiddCustomKind.ShieldBash;

            if (bashHit)
            {
                target.AddBuff(BuffID.WitheredWeapon, BashWitheredWeaponTicks, false);
                return;
            }

            target.AddBuff(BuffID.WitheredArmor, SwordWitheredArmorTicks, false);
        }

        protected override void DoMeleeAttack()
        {
            SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.65f, PitchVariance = 0.2f }, NPC.Center);
            TryMeleeHit();
        }

        protected override void DoStabAttack()
        {
            // CanStab is false; Fang Thrust replaced the legacy stab.
        }

        // Azure Suppression Flask: a lob solved to land FlaskOvershoot past where the target is heading.
        protected override void DoRangedAttack()
        {
            PlayThrowSound();

            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            Player target = Main.player[NPC.target];
            Vector2 origin = PuppetHandPosition;
            float flightTicks = MathHelper.Clamp(Vector2.Distance(origin, target.Center) / FlaskThrowSpeed, 10f, 40f);
            Vector2 predicted = target.Center + new Vector2(target.velocity.X * flightTicks, 0f);
            float throwDirection = 1f;

            if (predicted.X < origin.X)
            {
                throwDirection = -1f;
            }

            Vector2 aimAt = predicted + new Vector2(throwDirection * FlaskOvershoot, 0f);
            Vector2 velocity = UsefulFunctions.BallisticTrajectory(
                origin, aimAt, FlaskThrowSpeed, BlaiddSuppressionFlask.Gravity, highAngle: false, fallback: true);

            Projectile.NewProjectile(NPC.GetSource_FromThis(), origin, velocity,
                ModContent.ProjectileType<BlaiddSuppressionFlask>(), 0, 0f, Main.myPlayer, throwDirection);
        }
    }
}

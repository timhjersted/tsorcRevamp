using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Accessories.Defensive.Shields;
using tsorcRevamp.Items.Armors;
using tsorcRevamp.Items.Materials;
using tsorcRevamp.Items.Weapons.Ranged.Crossbows;
using tsorcRevamp.Items.Weapons.Enemy;
using tsorcRevamp.NPCs.AI;
using tsorcRevamp.Utilities;

using tsorcRevamp.Items.Weapons.Melee.Axes;
using tsorcRevamp.Items;
namespace tsorcRevamp.NPCs.Puppets
{
    [AutoloadBossHead]
    public class StuddedLeatherWarrior : PuppetNPC, Projectiles.Enemy.Weapons.IPuppetSnareSource
    {
        public override string BossHeadTexture => "tsorcRevamp/NPCs/Puppets/StuddedLeatherWarrior_Head_Boss";

        private const string SeismicPursuitName = "Seismic Pursuit";
        private const int SnareCooldownTicks = 10 * 60;
        private int _snareCooldown;
        private bool _currentCrossbowShotIsSnare;

        protected override string InvaderTitle => "Studded Leather Warrior";

        public override void AI()
        {
            if (_snareCooldown > 0)
                _snareCooldown--;
            base.AI();
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            base.SendExtraAI(writer);
            writer.Write(_currentCrossbowShotIsSnare);
            writer.Write((short)_snareCooldown);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            base.ReceiveExtraAI(reader);
            _currentCrossbowShotIsSnare = reader.ReadBoolean();
            _snareCooldown = reader.ReadInt16();
        }

        public void NotifySnareCaught(int playerIndex)
        {
            if (playerIndex < 0 || playerIndex >= Main.maxPlayers)
                return;
            NPC.target = playerIndex;
            QueueRangedFollowUp(useSecondary: false, targetPlayer: playerIndex, lifetimeTicks: 300);
        }

        protected override void RunMovementAI(float speedMult)
        {
            var globalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            globalNPC.NavSearchRadius = 70;
            globalNPC.RemembersLastKnownPos = true;

            SmartFighter4AI.Run(NPC,
                topSpeed: TopSpeed * speedMult,
                acceleration: Acceleration,
                doorBreakingDamage: 3,
                // SF4's attackRange is its re-aggro radius, not its preferred fighting range.
                // Keep this melee-first puppet pursuing from long range even while its throws cool down.
                attackRange: 700f);
        }

        protected override int HeadArmorItemType => ModContent.ItemType<StuddedLeatherHelmet>();
        protected override int BodyArmorItemType => ModContent.ItemType<StuddedLeatherArmor>();
        protected override int LegsArmorItemType => ModContent.ItemType<StuddedLeatherGreaves>();

        protected override int MeleeWeaponItemType => ModContent.ItemType<DualBladedAxe>();
        protected override int RangedWeaponItemType => ModContent.ItemType<EnemyFireFlask>();

        protected override int MeleeDamage => 28;
        protected override int RangedDamage => 18;

        protected override int EstusChargesMax => 3;
        protected override int HealAnimationTicks => 180;

        protected override WeaponArchetype MeleeArchetype => WeaponArchetype.Axe;

        // Keep this moveset local: Owl Father also uses WeaponArchetype.Axe and should retain the
        // shared axe table. Every attack here is built from readable full-arm axe motions.
        private static MeleeComboStep AxeSwing(
            ComboMotion motion,
            int telegraphTicks,
            int attackTicks,
            int pauseAfter = 0,
            float damageMult = 1f,
            float forwardPushMult = 0f,
            float reachMult = 1.1f,
            float leapHeightMult = 1f,
            float leapForwardSpeedMult = 1f)
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
                Ease = SwingEaseStyle.Smooth,
                LeapHeightMult = leapHeightMult,
                LeapForwardSpeedMult = leapForwardSpeedMult,
            };

        private static readonly PuppetAttackClip DownwardSwingV2 = new PuppetAttackClip(
            name: "Downward Swing",
            pose: PuppetPosePreset.TwoHandedSwing,
            windupTicks: 36,
            activeTicks: 24,
            recoveryTicks: 20,
            oppositeWindupRotation: 1.0f,
            attackStartRotation: -1.3f,
            attackEndRotation: 1.0f);

        private static readonly PuppetAttackClip UpwardSwingV2 = new PuppetAttackClip(
            name: "Upward Swing",
            pose: PuppetPosePreset.TwoHandedSwing,
            windupTicks: 36,
            activeTicks: 24,
            recoveryTicks: 20,
            oppositeWindupRotation: -1.0f,
            attackStartRotation: 1.0f,
            attackEndRotation: -1.0f);

        private static readonly MeleeCombo[] StuddedAxeCombos = new[]
        {
            new MeleeCombo
            {
                Name = "Downward Swing",
                BaseWeight = 65,
                Preferred = ComboRangeBand.Close,
                InitialFlashColor = Color.White,
                CooldownAfterUse = 70,
                MoveBrake = 0.15f,
                RuntimeV2Clip = DownwardSwingV2,
                Steps = new[] { AxeSwing(ComboMotion.OverheadArc, 28, 24,
                    damageMult: 1.1f, forwardPushMult: 0.55f) },
            },
            new MeleeCombo
            {
                Name = "Upward Swing",
                BaseWeight = 65,
                Preferred = ComboRangeBand.Close,
                InitialFlashColor = Color.LightYellow,
                CooldownAfterUse = 70,
                MoveBrake = 0.15f,
                RuntimeV2Clip = UpwardSwingV2,
                Steps = new[] { AxeSwing(ComboMotion.UnderhandArc, 28, 24,
                    damageMult: 1.1f, forwardPushMult: 0.55f) },
            },
            new MeleeCombo
            {
                Name = "Down-Up",
                BaseWeight = 95,
                Preferred = ComboRangeBand.Close,
                InitialFlashColor = Color.Cyan,
                CooldownAfterUse = 140,
                MoveBrake = 0.20f,
                Steps = new[]
                {
                    AxeSwing(ComboMotion.OverheadArc, 26, 22, pauseAfter: 8,
                        damageMult: 0.85f, forwardPushMult: 0.50f),
                    AxeSwing(ComboMotion.UnderhandArc, 0, 24,
                        damageMult: 1.05f, forwardPushMult: 0.65f),
                },
            },
            new MeleeCombo
            {
                Name = "Up-Down",
                BaseWeight = 95,
                Preferred = ComboRangeBand.Close,
                InitialFlashColor = Color.LightCyan,
                CooldownAfterUse = 140,
                MoveBrake = 0.20f,
                Steps = new[]
                {
                    AxeSwing(ComboMotion.UnderhandArc, 26, 22, pauseAfter: 8,
                        damageMult: 0.85f, forwardPushMult: 0.50f),
                    AxeSwing(ComboMotion.OverheadArc, 0, 24,
                        damageMult: 1.05f, forwardPushMult: 0.65f),
                },
            },
            new MeleeCombo
            {
                Name = "Low Sweep - Reverse Chop",
                BaseWeight = 75,
                Preferred = ComboRangeBand.Close,
                InitialFlashColor = Color.LightGreen,
                CooldownAfterUse = 180,
                MoveBrake = 0.22f,
                Steps = new[]
                {
                    AxeSwing(ComboMotion.UnderhandArc, 30, 24, pauseAfter: 10,
                        damageMult: 0.85f, forwardPushMult: 0.45f),
                    AxeSwing(ComboMotion.OverheadArc, 0, 30,
                        damageMult: 1.30f, forwardPushMult: 0.65f, reachMult: 1.16f),
                },
            },
            new MeleeCombo
            {
                Name = "Advancing Figure Eight",
                BaseWeight = 80,
                Preferred = ComboRangeBand.Close,
                InitialFlashColor = Color.Aquamarine,
                CooldownAfterUse = 190,
                MoveBrake = 0.08f,
                Steps = new[]
                {
                    AxeSwing(ComboMotion.OverheadArc, 28, 24, pauseAfter: 6,
                        damageMult: 0.82f, forwardPushMult: 0.62f),
                    AxeSwing(ComboMotion.UnderhandArc, 0, 25,
                        damageMult: 1.02f, forwardPushMult: 0.72f),
                },
            },
            new MeleeCombo
            {
                Name = "Relentless Hew",
                BaseWeight = 90,
                Preferred = ComboRangeBand.Close,
                InitialFlashColor = Color.Gold,
                CooldownAfterUse = 180,
                MoveBrake = 0.08f,
                Steps = new[]
                {
                    AxeSwing(ComboMotion.OverheadArc, 28, 20, pauseAfter: 5,
                        damageMult: 0.72f, forwardPushMult: 0.58f),
                    AxeSwing(ComboMotion.UnderhandArc, 0, 20, pauseAfter: 5,
                        damageMult: 0.78f, forwardPushMult: 0.68f),
                    AxeSwing(ComboMotion.OverheadArc, 0, 24,
                        damageMult: 1.18f, forwardPushMult: 0.78f, reachMult: 1.16f),
                },
            },
            new MeleeCombo
            {
                Name = "Double Circle Slam",
                BaseWeight = 45,
                Preferred = ComboRangeBand.Close,
                InitialFlashColor = Color.OrangeRed,
                CooldownAfterUse = 240,
                HeavyCommit = true,
                HyperArmor = true,
                MoveBrake = 0.40f,
                Steps = new[] { AxeSwing(ComboMotion.DoubleSpinSlam, 42, 54,
                    damageMult: 1.45f, forwardPushMult: 0.35f) },
            },
            new MeleeCombo
            {
                Name = "Apex Feint Leap",
                BaseWeight = 30,
                Preferred = ComboRangeBand.Close,
                InitialFlashColor = Color.LightGoldenrodYellow,
                CooldownAfterUse = 260,
                HeavyCommit = true,
                HyperArmor = true,
                MoveBrake = 0.35f,
                Steps = new[]
                {
                    // Looks exactly like the start of the normal downward chop, then deliberately
                    // hangs at the apex for one full second before the actual leap slash.
                    AxeSwing(ComboMotion.Feint, 32, 60, pauseAfter: 6, damageMult: 0f),
                    AxeSwing(ComboMotion.LeapSlam, 0, 90, damageMult: 1.35f),
                },
            },
            new MeleeCombo
            {
                Name = "Axe Cast and Retrieve",
                BaseWeight = 35,
                Preferred = ComboRangeBand.Far,
                InitialFlashColor = Color.MediumPurple,
                CooldownAfterUse = 320,
                HeavyCommit = true,
                HyperArmor = true,
                RangedStartOnly = true,
                MoveBrake = 0.30f,
                Steps = new[]
                {
                    // The attack phase lasts long enough for flight, embed, read, and retrieval.
                    AxeSwing(ComboMotion.ThrownWeaponRetrieve, 46, 240, pauseAfter: 1, damageMult: 0f),
                    // Replaced at runtime after pickup: close = underhand uppercut; far = leap slam.
                    AxeSwing(ComboMotion.UnderhandArc, 0, 22,
                        damageMult: 1.15f, forwardPushMult: 0.65f, reachMult: 1.16f),
                },
            },
            new MeleeCombo
            {
                Name = "Rising Pursuit",
                BaseWeight = 55,
                Preferred = ComboRangeBand.Any,
                InitialFlashColor = Color.Orange,
                CooldownAfterUse = 300,
                HeavyCommit = true,
                HyperArmor = true,
                RangedStartOnly = true,
                MoveBrake = 0f,
                Steps = new[]
                {
                    // The running carry is itself the long telegraph: arm down/back, axe trailing.
                    AxeSwing(ComboMotion.LowAxeRun, 32, 140, pauseAfter: 1, damageMult: 0f),
                    // Safety timeout is deliberately long; the phase normally ends 30 ticks after
                    // the jump reaches its apex and begins falling.
                    AxeSwing(ComboMotion.RisingUppercutLeap, 0, 150,
                        damageMult: 1.35f, reachMult: 1.20f),
                },
            },
            new MeleeCombo
            {
                Name = "Leaping Uppercut",
                BaseWeight = 45,
                Preferred = ComboRangeBand.Mid,
                InitialFlashColor = Color.Orange,
                CooldownAfterUse = 210,
                HeavyCommit = true,
                RangedStartOnly = true,
                MoveBrake = 0.08f,
                Steps = new[]
                {
                    AxeSwing(ComboMotion.RisingUppercutLeap, 34, 150,
                        damageMult: 1.22f, reachMult: 1.20f),
                },
            },
            new MeleeCombo
            {
                Name = "Leaping Cleave",
                BaseWeight = 52,
                Preferred = ComboRangeBand.Mid,
                InitialFlashColor = Color.OrangeRed,
                CooldownAfterUse = 220,
                HeavyCommit = true,
                RangedStartOnly = true,
                MoveBrake = 0.12f,
                Steps = new[]
                {
                    AxeSwing(ComboMotion.LeapSlam, 36, 90,
                        damageMult: 1.30f, reachMult: 1.20f),
                },
            },
            new MeleeCombo
            {
                Name = SeismicPursuitName,
                BaseWeight = 48,
                Preferred = ComboRangeBand.Mid,
                InitialFlashColor = Color.SandyBrown,
                CooldownAfterUse = 300,
                HeavyCommit = true,
                HyperArmor = true,
                RangedStartOnly = true,
                MoveBrake = 0.12f,
                Steps = new[]
                {
                    // A taller, committed overhead leap. The pursuit steps are hit-confirmed below.
                    AxeSwing(ComboMotion.LeapSlam, 44, 110, pauseAfter: 4,
                        damageMult: 1.40f, reachMult: 1.22f,
                        leapHeightMult: 1.15f, leapForwardSpeedMult: 1.05f),
                    AxeSwing(ComboMotion.LowAxeRun, 0, 105, pauseAfter: 1, damageMult: 0f),
                    AxeSwing(ComboMotion.RisingUppercutLeap, 0, 150,
                        damageMult: 1.28f, reachMult: 1.20f),
                },
            },
        };

        protected override MeleeCombo[] MeleeComboPoolOverride => StuddedAxeCombos;

        // Off-hand shield — basic early Iron Shield (also what HollowSoldier drops).
        protected override int ShieldItemType => ModContent.ItemType<IronShield>();
        protected override bool AllowReactiveDefense => true;
        protected override int ShieldGuardTicksRanged => 42;
        protected override int ShieldGuardTicksMelee => 48;
        protected override int ShieldGuardCooldownTicks => 180;

        // ── Axe draw tuning ─────────────────────────────────────────────────────────
        // Grip slightly above the butt of the diagonal handle so a short length remains visible
        // behind the hand in every held pose, telegraph, and swing.
        protected override Vector2 MeleeHandleNorm => new Vector2(0.14f, 0.84f);
        protected override float MeleeWeaponDrawScale => 0.85f;
        // Match the tracked capsule to the axe's measured 74px visual reach. The old 82px base
        // became only ~63px after the shared 0.7 scale and visibly stopped short of both blades.
        protected override float ComboReachBase => 96f;
        // Preserve the wrist-to-weapon angle that tested well with the composite arm.
        protected override float MeleeWeaponRotationOffset => 1.0f;
        // The composite arm already mirrors its arc by facing. Mirror the axe's raw arc as well so
        // left-facing casts and uppercuts remain attached to the hand instead of rotating backward.
        protected override bool MirrorMeleeSwingRotationByFacing => true;

        // The replacement axe is symmetric, so vertical mirroring adds motion without improving
        // blade direction. This permanently gives this puppet the tested `/swingarm flip off` path.
        protected override bool MeleeWeaponIsSingleBladed => false;

        // A downward axe strike now winds from low -> overhead before its damaging overhead -> low
        // arc. This opt-in keeps the semantic telegraph change isolated to this puppet.
        protected override bool UseLogicalMeleeTelegraphs => true;

        // PuppetNPC already maps the BroadswordRework slash strip onto its own live blade angle and
        // reach. Enabling it here gives the axe honest swing VFX without a duplicate projectile.
        protected override bool HasSlashVFX => true;
        protected override Color SlashVFXColor => new Color(190, 175, 255);
        protected override float SlashVFXOpacity => 0.55f;
        // Keep the 64px BroadswordRework slash frame close to the axe's visual footprint. The
        // previous 1.1 multiplier doubled the intended frame size once reach scaling was applied.
        protected override float SlashVFXScale => 0.55f;

        protected override int ThrownComboWeaponProjectileType =>
            ModContent.ProjectileType<Projectiles.Enemy.Weapons.PuppetThrownDualBladedAxe>();

        protected override int SpawnThrownComboWeapon()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return -1;

            Player target = Main.player[NPC.target];
            Vector2 origin = PuppetHandPosition;
            const float throwSpeed = 11.5f;
            float leadTicks = MathHelper.Clamp(Vector2.Distance(origin, target.Center) / throwSpeed, 8f, 28f);
            Vector2 aimAt = target.Center + target.velocity * leadTicks;
            Vector2 velocity = UsefulFunctions.BallisticTrajectory(
                origin, aimAt, throwSpeed, 0.18f, highAngle: false, fallback: true);

            SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.8f, Pitch = -0.15f }, origin);
            return Projectile.NewProjectile(
                NPC.GetSource_FromThis(),
                origin,
                velocity,
                ThrownComboWeaponProjectileType,
                (int)(MeleeDamage * 0.9f),
                5f,
                Main.myPlayer,
                0f,
                NPC.whoAmI);
        }

        protected override void OnComboStepCompleted(MeleeComboStep step)
        {
            if (ActiveMeleeComboName == SeismicPursuitName
                && step.Motion == ComboMotion.LeapSlam
                && NPC.velocity.Y == 0f)
            {
                UsefulFunctions.ScreenShake(NPC.Bottom, 3.5f, 10, distanceFalloff: 560f);
                SoundEngine.PlaySound(SoundID.Dig with { Volume = 0.85f, Pitch = -0.35f }, NPC.Bottom);

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    int waveType = ModContent.ProjectileType<Projectiles.Enemy.Weapons.PuppetGroundDustWave>();
                    for (int direction = -1; direction <= 1; direction += 2)
                    {
                        Projectile.NewProjectile(
                            NPC.GetSource_FromThis(),
                            NPC.Bottom,
                            new Vector2(direction * 8f, 0f),
                            waveType,
                            0,
                            0f,
                            Main.myPlayer);
                    }
                }
            }
            else if (step.Motion == ComboMotion.DoubleSpinSlam)
            {
                UsefulFunctions.ScreenShake(NPC.Bottom, 3f, 8, distanceFalloff: 500f);
                SoundEngine.PlaySound(SoundID.Dig with { Volume = 0.75f, Pitch = -0.25f }, NPC.Bottom);
                for (int i = 0; i < 14; i++)
                {
                    int dust = Dust.NewDust(
                        NPC.Bottom - new Vector2(NPC.width, 4f),
                        NPC.width * 2,
                        8,
                        DustID.Dirt,
                        Main.rand.NextFloat(-3.5f, 3.5f),
                        Main.rand.NextFloat(-3.5f, -0.5f),
                        80,
                        default,
                        Main.rand.NextFloat(0.9f, 1.45f));
                    Main.dust[dust].noGravity = Main.rand.NextBool(3);
                }
            }
            else if (step.Motion == ComboMotion.ThrownWeaponRetrieve)
            {
                SoundEngine.PlaySound(SoundID.Item7 with { Volume = 0.65f, Pitch = 0.15f }, NPC.Center);
            }
            else if (step.Motion == ComboMotion.RisingUppercutLeap)
            {
                // The axe vanishes exactly as the 30-tick falling hold ends. Idle restores it only
                // after the normal combo recovery has completed.
                SetDisplayWeapon(-1, swing: false);
            }
        }

        protected override bool ShouldContinueMeleeCombo(
            string comboName, int nextStepIndex, Player target, bool previousStepHit)
        {
            // The running uppercut is earned only by landing the seismic axe hit. A clean roll gets
            // normal recovery instead of an invisible proximity check extending the sequence.
            if (comboName == SeismicPursuitName && nextStepIndex == 1)
                return previousStepHit;
            return base.ShouldContinueMeleeCombo(comboName, nextStepIndex, target, previousStepHit);
        }

        protected override void OnBladeHit(Player player)
        {
            if (ActiveMeleeComboName == SeismicPursuitName
                && ActiveMeleeComboMotion == ComboMotion.LeapSlam)
            {
                player.AddBuff(BuffID.Slow, 90);
            }
            base.OnBladeHit(player);
        }

        protected override void ModifyNextMeleeComboStep(
            string comboName, int nextStepIndex, Player target, ref MeleeComboStep nextStep)
        {
            if (comboName != "Axe Cast and Retrieve" || nextStepIndex != 1)
                return;

            if (NPC.Distance(target.Center) <= 130f)
            {
                // Pickup flows straight into a rising cut when the retrieval already closed space.
                nextStep = AxeSwing(ComboMotion.UnderhandArc, 0, 22,
                    damageMult: 1.15f, forwardPushMult: 0.65f, reachMult: 1.16f);
            }
            else
            {
                // If the player retreated while the axe was grounded, convert the pickup momentum
                // into an immediate overhead leap rather than ending the sequence out of range.
                nextStep = AxeSwing(ComboMotion.LeapSlam, 0, 90,
                    damageMult: 1.35f, reachMult: 1.20f);
            }
        }

        protected override void CustomizeMeleeCombo(ref MeleeCombo combo, float healthFraction)
        {
            if (combo.RuntimeV2Clip != null || combo.RangedStartOnly || combo.Steps == null)
                return;

            bool expandable = combo.Name == "Down-Up"
                || combo.Name == "Up-Down"
                || combo.Name == "Low Sweep - Reverse Chop"
                || combo.Name == "Advancing Figure Eight"
                || combo.Name == "Relentless Hew";
            if (!expandable)
                return;

            int extraSwings = healthFraction <= 0.33f ? 2
                : healthFraction <= 0.66f ? 1
                : 0;
            if (extraSwings <= 0)
                return;

            var steps = new List<MeleeComboStep>(combo.Steps);
            for (int i = 0; i < extraSwings; i++)
            {
                int previousIndex = steps.Count - 1;
                MeleeComboStep previous = steps[previousIndex];
                previous.PostStepPause = 5;
                steps[previousIndex] = previous;

                ComboMotion followUp = previous.Motion == ComboMotion.OverheadArc
                    ? ComboMotion.UnderhandArc
                    : ComboMotion.OverheadArc;
                bool finisher = i == extraSwings - 1;
                steps.Add(AxeSwing(
                    followUp,
                    0,
                    finisher ? 24 : 20,
                    damageMult: finisher ? 1.02f : 0.75f,
                    forwardPushMult: finisher ? 0.78f : 0.68f,
                    reachMult: finisher ? 1.16f : 1.10f));
            }
            combo.Steps = steps.ToArray();
        }

        // ── Composite-arm swing experiment ──────────────────────────────────────────
        // Enabled ONLY on this enemy so the new continuous-arm swing can be A/B tested against
        // the legacy 4-frame path before touching any other invader.  Flip the global
        // PuppetNPC.CompositeArmSwingMasterEnable off to fall back instantly.
        protected override bool UseCompositeArmSwing => true;

        protected override RangedStyle RangedAnimStyle => RangedStyle.Throw;
        protected override float RangedRange => 440f;
        protected override float MinRangedRange => 260f;
        protected override int RangedTelegraphTicks => 60;
        protected override int RangedCooldownAfterUse => 220;
        protected override int MaxRangedBurst => 1;
        protected override int SingleRangedBurstChance => 100;
        protected override int StandingRangedChance => 35;
        protected override Color RangedTelegraphFlashColor => new Color(255, 120, 40);

        protected override int SecondaryRangedWeaponItemType => ModContent.ItemType<HeavyCrossbow>();
        protected override int SecondaryRangedDamage => 24;
        protected override RangedStyle SecondaryRangedAnimStyle => RangedStyle.Crossbow;
        protected override float SecondaryRangedRange => 560f;
        protected override float SecondaryRangedMinRange => 180f;
        protected override int SecondaryRangedTelegraphTicks => 34;
        protected override int SecondaryRangedCooldownAfterUse => 135;
        protected override int SecondaryMaxRangedBurst => 1;

        protected override void ModifyRangedBurst(
            bool secondary,
            ref int itemType,
            ref RangedStyle style,
            ref Color flashColor,
            ref int telegraphTicks,
            ref int attackTicks,
            ref int recoveryTicks)
        {
            _currentCrossbowShotIsSnare = secondary && _snareCooldown <= 0;
            if (_currentCrossbowShotIsSnare)
            {
                // A dark, longer draw distinguishes the net payload from an ordinary damaging bolt.
                flashColor = new Color(45, 42, 52);
                telegraphTicks += 12;
                recoveryTicks += 8;
            }

            NPC.netUpdate = true;
        }
        protected override int SecondaryRangedChance
        {
            get
            {
                float hpFrac = NPC.lifeMax > 0 ? (float)NPC.life / NPC.lifeMax : 1f;
                if (hpFrac <= 0.35f)
                    return 65;
                if (hpFrac <= 0.65f)
                    return 60;
                return 55;
            }
        }
        protected override int SecondaryStandingRangedChance => 70;
        protected override Color SecondaryRangedFlashColor => Color.White;

        protected override int[][] SecondaryRangedBurstPatterns => new int[][]
        {
            new int[] { },
            new int[] { 30, 60 },
            new int[] { 20 },
            new int[] { 18, 18, 18 },
            new int[] { 30, 30, 30, 5, 5 },
        };

        protected override int[] SecondaryRangedBurstTelegraphExtras => new int[] { 0, 10, 0, 12, 30 };

        protected override Color[] SecondaryRangedBurstFlashColors => new Color[]
        {
            Color.White,
            Color.Yellow,
            Color.Cyan,
            Color.LightYellow,
            Color.Red,
        };

        protected override int[] SecondaryRangedBurstChances => new int[] { 90, 45, 55, 30, 10 };

        protected override float TopSpeed => 2.65f;
        protected override float Acceleration => 0.095f;
        protected override float ChargeAttackSpeedMult => 2.20f;
        protected override float MeleeRange => 82f;
        protected override float StabRange => 150f;
        protected override float ComboMaxStartRange => 440f;
        // 80 ticks was marginal for crossing the full 440px selection band after acceleration
        // and terrain routing, causing the closing phase to expire just before melee range.
        protected override int ClosingDistanceMaxTicks => 140;
        protected override int MeleeComboChance => 100;
        protected override int RangedStartMeleeComboChance => 55;
        protected override float ComboTelegraphMultiplier => 1.25f;
        protected override float ComboTelegraphAdvanceSpeedMult => 0.9f;
        protected override float ComboTelegraphAdvanceStopDistance => 68f;
        protected override int MeleeRecoveryTicks => 18;

        protected override int MeleeTelegraphTicks => 36;
        protected override int StabTelegraphTicks => 40;
        protected override int StabAttackTicks => 8;
        protected override int StabRecoveryTicks => 34;
        protected override int TeleportTelegraphTicks => 130;
        protected override int TeleportDustCount => 22;
        protected override Color TeleportDustTint => new Color(180, 180, 180);
        // Prototype is a melee pursuer: recover, then immediately resume engagement/navigation.
        protected override int CasualStrollChance => 0;
        // Stab is a thrust pose — reads wrong for an axe (an axe has no poke), so it's disabled;
        // the axe gets its reach from the combo swings + Charge/Leap gap-closers instead.
        protected override bool CanStab => false;

        protected override float PuppetJumpPower => 10f;
        protected override float PuppetJumpBoost => 6f;

        protected override Color MeleeTelegraphFlashColor => Color.White;

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
            NPC.value = 15000f;
            NPC.boss = true;
            NPC.npcSlots = 5f;

            tsorcRevampGlobalNPC globalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            globalNPC.PoiseMax = 35f;
            globalNPC.PoiseStaggerResetsAI = true;
            globalNPC.NavGiveUpTicks = 180;
            globalNPC.CanUseRopes = true;
            globalNPC.CanTeleport = true;
            globalNPC.TeleportStyle = TeleportStyle.Aggressive;
            globalNPC.TeleportVisualStyle = TeleportVisualStyle.GreySmoke;

            // Restore the disciplined iron-shield response without the old hit-triggered dash pool.
            // PuppetNPC bounds this as a short ShieldGuard action with a rearm cooldown, so repeated
            // hits cannot refresh Idle or cancel an attack already in progress.
            ShieldProfile.LothricKnight(globalNPC);
        }
        
        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            // Guaranteed: the full studded leather set it wears + the iron shield it carries.
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<StuddedLeatherHelmet>()));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<StuddedLeatherArmor>()));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<StuddedLeatherGreaves>()));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<IronShield>()));

            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<BrokenDualBladedAxe>()));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<ProudKnightSoul>()));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<global::tsorcRevamp.Items.Weapons.Throwing.FireFlask>()));
        }

        public override void HitEffect(NPC.HitInfo hit)
        {
            if (NPC.life <= 0)
            {
                for (int i = 0; i < 150; i++)
                {
                    int d = Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Blood, Main.rand.NextFloat(-6f, 6f), Main.rand.NextFloat(-6f, 6f), 100, default, Main.rand.NextFloat(1f, 2.2f));
                    Main.dust[d].noGravity = true;
                }
            }
        }

        public override void OnKill()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            Terraria.ModLoader.Config.NPCDefinition definition = new(ModContent.NPCType<StuddedLeatherWarrior>());
            if (!tsorcRevampWorld.NewSlain.ContainsKey(definition))
            {
                Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<global::tsorcRevamp.Items.StaminaDroplet>());
                tsorcRevampWorld.NewSlain.Add(definition, 1);

                if (Main.netMode == NetmodeID.Server)
                {
                    NetMessage.SendData(MessageID.WorldData);
                }
            }
        }

        protected override void DoMeleeAttack()
        {
            SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.65f, PitchVariance = 0.2f }, NPC.Center);
            TryMeleeHit();
        }

        protected override void DoStabAttack()
        {
            SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.7f, PitchVariance = 0.15f }, NPC.Center);
            TryMeleeHit(reach: StabRange * 0.55f);
        }

        protected override void DoRangedAttack()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            Player target = Main.player[NPC.target];

            if (IsSecondaryRangedActive)
            {
                SoundEngine.PlaySound(SoundID.Item98 with { Volume = 0.75f, PitchVariance = 0.1f }, NPC.Center);
                Vector2 muzzle = NPC.Center + new Vector2(0f, -NPC.height * 0.20f);
                Vector2 aimAt = target.Center + new Vector2(0f, -target.height * 0.25f);
                Vector2 toTarget = aimAt - muzzle;
                if (toTarget == Vector2.Zero)
                {
                    toTarget = new Vector2(NPC.direction, 0f);
                }

                toTarget.Normalize();
                float spread = MathHelper.ToRadians(Main.rand.NextFloat(-3f, 3f));
                Vector2 velocity = toTarget.RotatedBy(spread) * 14f;

                if (_currentCrossbowShotIsSnare)
                {
                    Projectile.NewProjectile(
                        NPC.GetSource_FromThis(),
                        muzzle,
                        velocity * 0.90f,
                        ModContent.ProjectileType<Projectiles.Enemy.Weapons.PuppetSnareBolt>(),
                        SecondaryRangedDamage,
                        3f,
                        Main.myPlayer,
                        NPC.whoAmI);
                    _currentCrossbowShotIsSnare = false;
                    _snareCooldown = SnareCooldownTicks;
                    NPC.netUpdate = true;
                    return;
                }

                Projectile.NewProjectile(
                    NPC.GetSource_FromThis(),
                    muzzle,
                    velocity,
                    ModContent.ProjectileType<Projectiles.Enemy.Weapons.PuppetCrossbowBolt>(),
                    SecondaryRangedDamage,
                    4f,
                    Main.myPlayer);
                return;
            }

            PlayThrowSound();
            Vector2 flaskMuzzle = NPC.Center + new Vector2(NPC.direction * 12f, -NPC.height * 0.25f);
            const float flaskSpeed = 10f;
            float leadTicks = MathHelper.Clamp(Vector2.Distance(flaskMuzzle, target.Center) / flaskSpeed, 8f, 32f);
            Vector2 flaskTarget = target.Center + target.velocity * leadTicks;
            Vector2 flaskVelocity = UsefulFunctions.BallisticTrajectory(flaskMuzzle, flaskTarget, flaskSpeed, 0.18f, highAngle: false, fallback: true);
            Projectile.NewProjectile(
                NPC.GetSource_FromThis(),
                flaskMuzzle,
                flaskVelocity,
                ModContent.ProjectileType<Projectiles.Enemy.Weapons.EnemyFireFlask>(),
                RangedDamage,
                3f,
                Main.myPlayer);
        }
    }
}

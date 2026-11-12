using System;
using System.IO;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items;
using tsorcRevamp.Content.Items.Armor.Enemy.ManhunterSet;
using tsorcRevamp.Content.Items.Potions.Lifegem;
using tsorcRevamp.Content.Items.Weapons.Enemy;
using tsorcRevamp.Content.Projectiles.Enemy.Weapons;
using tsorcRevamp.NPCs.Invaders;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.NPCs.Enemies.Archer
{
    /// <summary>
    /// Jungle kiting archer. Holds a wooden bow low in the far hand, raises it into the shared hunter string-draw
    /// pose (HunterArcherPuppet) to shoot single flaming arrows (chained shots as he loses health), and answers a
    /// player who closes in with a knife: a swipe at arm's length or a dash-stab from a little farther out.
    /// </summary>
    public class ManHunter : HunterArcherPuppet
    {
        // ── Bow shot ─────────────────────────────────────────────────────────────────────────────────────────
        // Legacy ArcherAI values: raw 14 arrow damage in every tier (x4 = Expert), 11 px/t, 60t aim, aim locked 15t
        // before the release, then 60t more before the next aim. Progression scaling handles HM/SHM globally.
        private const int ArrowExpertDamage = 56;
        private const float ArrowSpeedPixels = 11f;
        private const int DrawTicks = 60;
        private const int AimLockTickCount = 15;
        private const int ReleaseTicks = 6;
        // Release 6 + recovery 36 + cooldown 18 = the legacy 60t between the shot and the next aim.
        private const int ShotRecoveryTicks = 36;
        private const int ShotCooldownTicks = 18;
        // Legacy CombatTempoProfile.Standard: each chained shot added 18t to the post-chain recovery.
        private const int ChainRecoveryTicksPerExtraShot = 18;
        private const float MinShotRange = 128f;
        private const float MaxShotRange = 700f;
        private static readonly Color FlameOrange = new Color(255, 150, 50);

        // ── Wooden Bow geometry (16x32, facing right) ────────────────────────────────────────────────────────
        // Riser centre (13, 16); string inset at x2..3 / y6..25, limb tips at x0..5 above and below it.
        private static readonly Vector2 WoodenBowGripNorm = new Vector2(13f / 16f, 0.5f);
        private static readonly Rectangle WoodenBowStringTexels = new Rectangle(2, 6, 2, 20);

        // ── Chained shots ────────────────────────────────────────────────────────────────────────────────────
        // One static table so every peer rebuilds the same pause row from the synced pattern index. Each entry is
        // the CrossbowBurstPause before the next shot; legacy chain gaps were 10-22t.
        private static readonly int[][] ChainPatterns =
        {
            new int[0],
            new[] { 10 }, new[] { 16 }, new[] { 22 },
            new[] { 10, 10 }, new[] { 16, 16 }, new[] { 22, 22 },
            new[] { 10, 10, 10 }, new[] { 16, 16, 16 }, new[] { 22, 22, 22 },
        };
        // Weights per HP band, approximating Standard's 70% continue chance: one shot above 70% HP, mostly two
        // at or below, mostly three under 40%, mostly four under 25%.
        private static readonly int[] SingleShotChances = { 100, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
        private static readonly int[] TwoShotChances = { 30, 23, 23, 24, 0, 0, 0, 0, 0, 0 };
        private static readonly int[] ThreeShotChances = { 0, 10, 10, 10, 23, 23, 24, 0, 0, 0 };
        private static readonly int[] FourShotChances = { 0, 0, 0, 0, 10, 10, 10, 23, 23, 24 };

        // ── Knife ────────────────────────────────────────────────────────────────────────────────────────────
        // Tibian Amazon's knife: EnemyThrowingKnife art, 18x34 point-up, tip (8.5, 0), grip centre (9, 28.5).
        // The +45° per-weapon turn plus a 90° natural rest angle make it draw where a broadsword would.
        private const float KnifeDrawScale = 1f;
        private const float KnifeReach = 28.5f;
        private const float KnifeBladeWidth = 12f;
        private static readonly Vector2 KnifeHandleNorm = new Vector2(9f / 18f, 28.5f / 34f);
        private const int SwipeExpertDamage = 48;
        private const int DashStabExpertDamage = 64;
        private const string SwipeName = "Knife Swipe";
        private const string DashStabName = "Dash Stab";
        // Swipe starts inside this gap; the dash-stab covers the band from here out to the bow's minimum range.
        private const float SwipeRange = 52f;
        private const float DashStabMaxRange = MinShotRange;
        private const float DashStabMaxHeightGap = 48f;
        // Dash-stab step-in: ends DashStabCloseTo px from the target's centre, clamped to [min, max] px.
        private const float DashStabCloseTo = 28f;
        private const float DashStabStepInMin = 40f;
        private const float DashStabStepInMax = 112f;

        // Knife Swipe timing sheet
        // Poses:  -1.40 -> 2.10 rad (200° envelope, ~160° live): a compact knife cut, deliberately narrower than a sword.
        // Tell:   24t on screen (ComboTelegraphMultiplier 1, MinComboTelegraphTicks 20).
        // Strike: Weighted 6 in / 0 cruise / 24 out, k 7 -> peak ~37°/t.
        // Live:   armed to 10.1t (HitWindowEnd 0.338) < 22t: fully rollable.
        // Open:   20t harmless tail + 36t recovery.
        // Move:   16px step-in. Counter: roll through, or stay outside 52px. Rolls needed: 1.
        private const float SwipeStartRotation = -1.40f;
        private const float SwipeEndRotation = 2.10f;
        private const int SwipeTellTicks = 24;
        private const int SwipeEaseIn = 6;
        private const int SwipeEaseOut = 24;
        private const float SwipeDecay = 7f;
        private const int SwipeAttackTicks = SwipeEaseIn + SwipeEaseOut;

        // Dash Stab timing sheet
        // Poses:  rapier en-garde (knife held forward, 17° up) -> level forward; the arm stays out through the dash.
        // Tell:   30t on screen with the knife raised to the guard.
        // Strike: Weighted 3 in / 17 out, k 2.5; 40-112px step-in peaks ~15.5 px/t on the same curve.
        // Live:   armed to 11.2t (HitWindowEnd 0.56) < 22t: rollable through or away (dash < roll distance + reach).
        // Open:   ~9t tail + 48t recovery with the arm extended. Cooldown 240t.
        private const int DashStabTellTicks = 30;
        private const int DashStabEaseIn = 3;
        private const int DashStabEaseOut = 17;
        private const float DashStabDecay = 2.5f;
        // Matches the knife item's useAnimation (20), so the non-arc thrust clock isn't cut short.
        private const int DashStabAttackTicks = DashStabEaseIn + DashStabEaseOut;

        private static readonly PuppetWeapon Knife = new PuppetWeapon(
            ModContent.ItemType<EnemyTibianAmazonKnife>(),
            handleNorm: KnifeHandleNorm,
            drawScale: KnifeDrawScale,
            bladeWidth: KnifeBladeWidth,
            archetype: WeaponArchetype.Dagger,
            reachBase: KnifeReach / 0.7f,
            rotationOffset: MathHelper.PiOver4);


        // Damage 0 and no swing hand ever selects the back slot, so the carried bow never tests a blade capsule.
        private static readonly PuppetWeapon CarriedWoodenBow = new PuppetWeapon(
            ModContent.ItemType<EnemyManhunterBow>(),
            damage: 0,
            handleNorm: WoodenBowGripNorm,
            drawScale: 1f,
            bladeWidth: 1f);

        private static readonly MeleeCombo[] KnifeCombos =
        {
            new MeleeCombo
            {
                Name = SwipeName,
                BaseWeight = 100,
                Preferred = ComboRangeBand.Close,
                InitialFlashColor = Color.LightGray,
                RecoveryTicks = 36,
                CooldownAfterUse = 45,
                MoveBrake = 0.3f,
                Steps = new[]
                {
                    new MeleeComboStep
                    {
                        Motion = ComboMotion.OverheadArc,
                        TelegraphTicks = SwipeTellTicks,
                        AttackTicks = SwipeAttackTicks,
                        DamageMult = 1f,
                        ReachMult = 1f,
                        SwingSpeedMult = 1f,
                        StepInDistance = 16f,
                        Ease = SwingEaseStyle.Weighted,
                        EaseInTicks = SwipeEaseIn,
                        EaseOutTicks = SwipeEaseOut,
                        EaseOutDecay = SwipeDecay,
                        HitWindowEnd = (SwipeEaseIn + SwipeEaseOut * 1.204f / SwipeDecay) / SwipeAttackTicks,
                        LeapHeightMult = 1f,
                        LeapForwardSpeedMult = 1f,
                    },
                },
            },
            new MeleeCombo
            {
                Name = DashStabName,
                BaseWeight = 100,
                Preferred = ComboRangeBand.Mid,
                InitialFlashColor = Color.White,
                RecoveryTicks = 48,
                CooldownAfterUse = 240,
                RangedStartOnly = true,
                MoveBrake = 0.3f,
                Steps = new[]
                {
                    new MeleeComboStep
                    {
                        Motion = ComboMotion.Thrust,
                        TelegraphTicks = DashStabTellTicks,
                        AttackTicks = DashStabAttackTicks,
                        DamageMult = DashStabExpertDamage / (float)SwipeExpertDamage,
                        ReachMult = 1f,
                        SwingSpeedMult = 1f,
                        StepInDistance = DashStabStepInMin,
                        Ease = SwingEaseStyle.Weighted,
                        EaseInTicks = DashStabEaseIn,
                        EaseOutTicks = DashStabEaseOut,
                        EaseOutDecay = DashStabDecay,
                        HitWindowEnd = (DashStabEaseIn + DashStabEaseOut * 1.204f / DashStabDecay) / DashStabAttackTicks,
                        LeapHeightMult = 1f,
                        LeapForwardSpeedMult = 1f,
                    },
                },
            },
        };


        // ── Puppet configuration ─────────────────────────────────────────────────────────────────────────────
        public override bool UsesPuppetDifficultyScaling => false;
        protected override string InvaderTitle => "Man Hunter";
        protected override bool AnnounceInvasion => false;
        protected override bool AnnounceInvaderDefeat => false;
        protected override bool DespawnsOnPartyWipe => false;
        protected override int EstusChargesMax => 0;
        protected override int CasualStrollChance => 0;
        protected override bool EvadesProjectiles => false;
        protected override int HeadArmorItemType => ModContent.ItemType<ManhunterHat>();
        protected override int BodyArmorItemType => ModContent.ItemType<ManhunterJerkin>();
        protected override int LegsArmorItemType => ModContent.ItemType<ManhunterBoots>();
        protected override float TopSpeed => 1.3f;
        protected override float Acceleration => 0.08f;
        // Cross-through landing room past the target. PuppetNPC writes this onto the GlobalNPC every tick, so a
        // SetDefaults value is overwritten with the base 16px.
        protected override float QuickStepForwardRoom => 48f;

        // Knife (front hand, only out for its attacks).
        protected override int MeleeWeaponItemType => ModContent.ItemType<EnemyTibianAmazonKnife>();
        protected override int MeleeDamage => EnemyDamage.Projectile(SwipeExpertDamage);
        protected override WeaponArchetype MeleeArchetype => WeaponArchetype.Dagger;
        protected override MeleeCombo[] MeleeComboPoolOverride => KnifeCombos;
        protected override Vector2 MeleeHandleNorm => KnifeHandleNorm;
        protected override float MeleeWeaponDrawScale => KnifeDrawScale;
        protected override float MeleeBladeWidth => KnifeBladeWidth;
        protected override float ComboReachBase => KnifeReach / 0.7f;
        protected override float MeleeNaturalRestAngleDeg => 90f;
        // Only the combos swing: no legacy single slash.
        protected override float MeleeRange => 0f;
        protected override float MeleeEngageRange => SwipeRange;
        // Out of swipe range the dash-stab may open; when it can't, he keeps kiting instead of closing in.
        protected override float ComboMaxStartRange => SwipeRange;
        protected override float RangedStartComboMaxRange => DashStabMaxRange;
        protected override int MeleeComboChance => 100;
        protected override int RangedStartMeleeComboChance => 100;
        protected override bool UseAuthoredComboSwingClock => true;
        protected override bool UseSwingEasing => true;
        // Plain raise from the carry to the cocked start. The logical dip-then-raise loop swept ~336° in the 24t
        // tell, peaking at the strike's own speed, so the swipe lost its contrast.
        protected override bool UseLogicalMeleeTelegraphs => false;
        protected override bool UseSmoothRapierThrustPose => true;
        protected override bool MirrorMeleeSwingRotationByFacing => true;
        protected override float ComboTelegraphMultiplier => 1f;
        protected override int MinComboTelegraphTicks => 20;
        protected override bool HasSlashVFX => true;
        protected override Color SlashVFXColor => new Color(200, 205, 215);
        protected override float SlashVFXOpacity => 0.25f;

        // Bow (far hand at rest, shared string-draw pose to shoot; see HunterArcherPuppet).
        protected override PuppetWeapon CarriedBow => CarriedWoodenBow;
        protected override Vector2 BowGripNorm => WoodenBowGripNorm;
        protected override Rectangle BowStringTexels => WoodenBowStringTexels;
        protected override int ArrowProjectileType => ModContent.ProjectileType<EnemyFlamingArrow>();
        protected override float ArrowAi2 => EnemyFlamingArrow.VanillaBurnMode;
        protected override float ArrowSpeed => ArrowSpeedPixels;
        protected override int AimLockTicks => AimLockTickCount;
        protected override int ChainRecoveryPerExtraShot => ChainRecoveryTicksPerExtraShot;
        protected override int RangedWeaponItemType => ModContent.ItemType<EnemyManhunterBow>();
        protected override int RangedDamage => EnemyDamage.Projectile(ArrowExpertDamage);
        protected override float RangedRange => MaxShotRange;
        protected override float MinRangedRange => MinShotRange;
        protected override int RangedTelegraphTicks => DrawTicks;
        protected override int RangedAttackTicks => ReleaseTicks;
        protected override int RangedRecoveryTicks => ShotRecoveryTicks;
        protected override int RangedCooldownAfterUse => ShotCooldownTicks;
        protected override Color RangedTelegraphFlashColor => FlameOrange;
        protected override int[][] PrimaryRangedBurstPatterns => ChainPatterns;
        protected override int[] PrimaryRangedBurstChances
        {
            get
            {
                float healthFraction = NPC.life / (float)NPC.lifeMax;

                if (healthFraction < 0.25f)
                {
                    return FourShotChances;
                }

                if (healthFraction < 0.40f)
                {
                    return ThreeShotChances;
                }

                if (healthFraction <= 0.70f)
                {
                    return TwoShotChances;
                }

                return SingleShotChances;
            }
        }
        protected override string NockedBowArrowTexture => "Terraria/Images/Projectile_" + ProjectileID.FlamingArrow;

        public override void SetDefaults()
        {
            NPC.width = 20;
            NPC.height = 42;
            NPC.aiStyle = -1;
            NPC.npcSlots = 3;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.lifeMax = 125;
            NPC.defense = 5;
            NPC.value = 630;
            NPC.knockBackResist = 0.6f;
            EnemyDamage.SetContact(NPC, 0);
            Banner = NPC.type;
            BannerItem = ModContent.ItemType<Banners.ManHunterBanner>();

            if (Main.hardMode)
            {
                NPC.lifeMax = 250;
                NPC.defense = 22;
                NPC.value = 1250;
            }

            if (tsorcRevampWorld.SuperHardMode)
            {
                NPC.lifeMax = 600;
                NPC.defense = 38;
                NPC.value = 1500;
            }

            // Legacy archer levers, unchanged. RemembersLastKnownPos makes it reposition toward where it last saw
            // you; PrefersHighGround stays dormant until the patrol high-ground bias lands.
            tsorcRevampGlobalNPC archerGlobalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            archerGlobalNPC.PrefersHighGround = true;
            archerGlobalNPC.RemembersLastKnownPos = true;
            archerGlobalNPC.NavSearchRadius = 50;
            archerGlobalNPC.CanUseRopes = true;
            archerGlobalNPC.KiteRangeMin = 8f;
            archerGlobalNPC.KiteRangeMax = 22f;
            archerGlobalNPC.KiteLooseness = 0.35f;
            archerGlobalNPC.CanAdvanceAndShoot = true;
            archerGlobalNPC.CanFireWhileAdvancing = false;
            archerGlobalNPC.AdvanceAndShootSpeedMultiplier = 1.45f;
            archerGlobalNPC.AdvanceAndShootAccelerationMultiplier = 1.4f;

            // The forward branch of QuickStep is the shared flank move: cross completely through the target and
            // leave a three-tile gap, while retaining the ordinary backstep when a safe cross is out of reach.
            archerGlobalNPC.EvasiveQuickStep = true;
            // (QuickStepForwardRoom is the QuickStepForwardRoom override above: PuppetNPC rewrites it every tick.)
            archerGlobalNPC.QuickStepSpeed = 6f;
            archerGlobalNPC.QuickStepForwardChance = 0.8f;
            archerGlobalNPC.QuickStepMaxForwardTicks = 36;
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            npcLoot.Add(ItemDropRule.Common(ItemID.ArcheryPotion));
            npcLoot.Add(ItemDropRule.Common(ItemID.HunterPotion, 34));
            npcLoot.Add(ItemDropRule.Common(ItemID.AmmoReservationPotion, 34));
            npcLoot.Add(ItemDropRule.Common(ItemID.SwiftnessPotion, 34));
            npcLoot.Add(new CommonDrop(ItemID.HealingPotion, 100, 1, 1, 2));
            npcLoot.Add(ItemDropRule.Common(ItemID.UnholyArrow, 4, 10, 20));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<SoulCoin>(), 2, 1, 2));
            npcLoot.Add(ItemDropRule.ByCondition(tsorcRevamp.tsorcItemDropRuleConditions.CursedRule, ModContent.ItemType<LifegemItem>(), 8));
        }

        public override void AI()
        {
            EquipWeapon(PuppetHandSlot.Front, Knife);

            base.AI();

            // The puppet body never deals contact damage; the knife and arrows are the only hits.
            NPC.damage = 0;

            DebugAttackLabel = null;

            if (InShotPose || Phase == AttackPhase.RangedRecovery)
            {
                DebugAttackLabel = "Flaming Arrow";
            }
            else if (Phase == AttackPhase.MeleeComboTelegraph || Phase == AttackPhase.MeleeComboAttack
                || Phase == AttackPhase.MeleeComboRecovery)
            {
                DebugAttackLabel = ActiveMeleeComboName;
            }
        }

        protected override void DoMeleeAttack()
        {
        }

        // Full draw: embers and a little light at the nocked flaming arrowhead.
        protected override void DoArrowheadVFX(Vector2 arrowhead)
        {
            Lighting.AddLight(arrowhead, 0.5f, 0.26f, 0.06f);

            if (Main.GameUpdateCount % 4 == 0)
            {
                Dust ember = Dust.NewDustPerfect(arrowhead, DustID.Torch, new Vector2(0f, -0.4f), 100, default, 0.9f);
                ember.noGravity = true;
            }
        }

        // Dash-stab step-in sized to the post-tell gap: ends DashStabCloseTo px from the target, 40-112px.
        protected override void ModifyOpeningMeleeComboStep(string comboName, Player target, ref MeleeComboStep openingStep)
        {
            if (comboName != DashStabName)
            {
                return;
            }

            float gap = Math.Abs(target.Center.X - NPC.Center.X);
            openingStep.StepInDistance = MathHelper.Clamp(gap - DashStabCloseTo, DashStabStepInMin, DashStabStepInMax);
        }

        protected override bool CanSelectMeleeCombo(MeleeCombo combo, float distance, float healthFraction)
        {
            Player target = Main.player[NPC.target];

            if (combo.Name == SwipeName)
            {
                return distance <= SwipeRange;
            }

            float heightGap = Math.Abs(target.Center.Y - NPC.Center.Y);
            bool clearLane = Collision.CanHitLine(NPC.Center, 1, 1, target.Center, 1, 1);
            return distance > SwipeRange && distance <= DashStabMaxRange && heightGap <= DashStabMaxHeightGap && clearLane;
        }

        protected override void ModifyMeleeArcEndpoints(ComboMotion motion, ref float startRotation, ref float endRotation)
        {
            if (motion == ComboMotion.OverheadArc)
            {
                startRotation = SwipeStartRotation;
                endRotation = SwipeEndRotation;
            }
        }

        public override void HitEffect(NPC.HitInfo hit)
        {
            for (int i = 0; i < 5; i++)
            {
                int dustIndex = Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Blood);
                Dust dust = Main.dust[dustIndex];
                dust.velocity.X += Main.rand.Next(-50, 51) * 0.06f;
                dust.velocity.Y += Main.rand.Next(-50, 51) * 0.06f;
                dust.scale *= 1f + Main.rand.Next(-30, 31) * 0.01f;
                dust.noGravity = true;
            }

            if (NPC.life > 0)
            {
                return;
            }

            for (int i = 0; i < 25; i++)
            {
                Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Blood, Main.rand.Next(-3, 3), Main.rand.Next(-3, 3), 70, default, 1f);
            }

            if (Main.dedServ)
            {
                return;
            }

            int[] goreNumbers = { 1, 2, 3, 2, 3 };

            foreach (int goreNumber in goreNumbers)
            {
                Vector2 goreVelocity = new Vector2(Main.rand.Next(-30, 31) * 0.2f, Main.rand.Next(-30, 31) * 0.2f);
                int goreType = Mod.Find<ModGore>("Man Hunter Gore " + goreNumber).Type;
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, goreVelocity, goreType, 1f);
            }
        }
    }
}

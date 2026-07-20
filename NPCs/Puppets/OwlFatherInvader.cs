using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Armors;
using tsorcRevamp.Items.Materials;
using tsorcRevamp.Items.Weapons.Enemy;
using tsorcRevamp.Projectiles.Enemy.Weapons;
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
    /// Signature ranged attack "Fire Volley": reuses the shared Spiral Fan
    /// swing+staggered-burst system (axe swing -> 3 fireballs, 30 ticks apart) for the base volley,
    /// then chains via TryContinueSpiralFanChain into an optional reposition (backward leap if
    /// there's room, or a leaping dodge-roll THROUGH the player if they're close) for a second
    /// volley, and optionally escalates into a big arc-jump-over with a third volley fired mid-air.
    /// No recovery after the chain ends either way — a clean hit-and-run poke.
    /// </summary>
    [AutoloadBossHead]
    public class OwlFatherInvader : PuppetNPC
    {
        private const string HighLeapFollowUpName = "High Leaping Slam - Rising Follow-Up";
        private const string GreatfireBreakerName = "Greatfire Breaker";
        private const string GreatfireCrescentName = "Greatfire Crescent";
        private const string BackstepReentryName = "Backstep Re-entry Chop";
        private const string VolleyCommandName = "Volley Command";
        private const string FirefallArrayName = "Firefall Array";
        private const string ApexDiveName = "Apex Dive Cleave";

        public override string BossHeadTexture => "tsorcRevamp/NPCs/Puppets/OwlFatherInvader_Head_Boss";

        protected override string InvaderTitle => "Owl Father";

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
                // Match it to Fire Volley's outer limit so the melee boss keeps hunting at range.
                attackRange: 700f);
        }

        protected override int HeadArmorItemType => ModContent.ItemType<OwlFatherMask>();
        protected override int BodyArmorItemType => ModContent.ItemType<OwlFatherArmor>();
        protected override int LegsArmorItemType => ModContent.ItemType<OwlFatherGreaves>();

        protected override int MeleeWeaponItemType => ModContent.ItemType<EnemyGreatFireAxe>();
        // Fire Volley is a bespoke special attack rather than a conventional held ranged weapon.
        protected override int RangedWeaponItemType => -1;
        protected override int RangedDamage => 0;

        protected override int MeleeDamage => 30;

        protected override WeaponArchetype MeleeArchetype => WeaponArchetype.Axe;

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
            };

        // The two V2 fundamentals are copied as definitions rather than shared by reference with
        // Studded. Their timings can now diverge as Owl's heavier axe is tuned.
        private static readonly PuppetAttackClip GreatfireDownwardSwingV2 = new PuppetAttackClip(
            name: "Greatfire Downward Swing",
            pose: PuppetPosePreset.TwoHandedSwing,
            windupTicks: 40,
            activeTicks: 26,
            recoveryTicks: 22,
            oppositeWindupRotation: 1.0f,
            attackStartRotation: -1.3f,
            attackEndRotation: 1.0f,
            swingEase: SwingEaseStyle.Whip);

        private static readonly PuppetAttackClip GreatfireUpwardSwingV2 = new PuppetAttackClip(
            name: "Greatfire Upward Swing",
            pose: PuppetPosePreset.TwoHandedSwing,
            windupTicks: 38,
            activeTicks: 25,
            recoveryTicks: 22,
            oppositeWindupRotation: -1.0f,
            attackStartRotation: 1.0f,
            attackEndRotation: -1.0f,
            swingEase: SwingEaseStyle.Smooth);

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
                Name = "Down-Up Reversal",
                BaseWeight = 90,
                Preferred = ComboRangeBand.Close,
                InitialFlashColor = Color.OrangeRed,
                CooldownAfterUse = 145,
                MoveBrake = 0.16f,
                Steps = new[]
                {
                    AxeSwing(ComboMotion.OverheadArc, 34, 25, pauseAfter: 9,
                        damageMult: 0.88f, forwardPushMult: 0.48f),
                    AxeSwing(ComboMotion.UnderhandArc, 0, 27,
                        damageMult: 1.18f, forwardPushMult: 0.58f, reachMult: 1.18f),
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
                Name = "High Leaping Slam",
                BaseWeight = 65,
                Preferred = ComboRangeBand.Mid,
                InitialFlashColor = Color.OrangeRed,
                CooldownAfterUse = 220,
                HeavyCommit = true,
                HyperArmor = true,
                RangedStartOnly = true,
                MoveBrake = 0.15f,
                Steps = new[] { AxeSwing(ComboMotion.LeapSlam, 44, 115,
                    damageMult: 1.55f, reachMult: 1.28f,
                    leapHeightMult: 1.18f, leapForwardSpeedMult: 1.05f) },
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
                        leapHeightMult: 1.22f, leapForwardSpeedMult: 1.05f),
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
                Name = VolleyCommandName,
                BaseWeight = 80,
                Preferred = ComboRangeBand.Far,
                InitialFlashColor = Color.OrangeRed,
                CooldownAfterUse = 210,
                RangedStartOnly = true,
                MoveBrake = 0.42f,
                Steps = new[] { AxeSwing(ComboMotion.UnderhandArc, 46, 30,
                    damageMult: 0.55f, reachMult: 1.15f) },
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
            new MeleeCombo
            {
                Name = ApexDiveName,
                BaseWeight = 58,
                Preferred = ComboRangeBand.Mid,
                InitialFlashColor = Color.Gold,
                CooldownAfterUse = 310,
                HeavyCommit = true,
                HyperArmor = true,
                RangedStartOnly = true,
                MoveBrake = 0f,
                Steps = new[] { AxeSwing(ComboMotion.ApexDiveCleave, 50, 150,
                    damageMult: 1.50f, reachMult: 1.30f,
                    leapHeightMult: 1.20f, leapForwardSpeedMult: 1.05f,
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
        protected override bool MirrorMeleeSwingRotationByFacing => true;
        protected override bool HasSlashVFX => true;
        protected override Color SlashVFXColor => Color.OrangeRed; // matches AncientFireAxe's own slashColor
        protected override float SlashVFXOpacity => 0.48f;
        protected override float SlashVFXScale => 0.55f;

        // ── Axe draw tuning ─────────────────────────────────────────────────────────
        // Grip above the butt so every pose leaves a short, visible section below the hand.
        protected override Vector2 MeleeHandleNorm => new Vector2(0.12f, 0.86f);
        protected override float MeleeWeaponDrawScale => 0.85f;
        protected override float ComboReachBase => 90f;
        protected override float MeleeWeaponRotationOffset => 1.0f;

        protected override float TopSpeed => 2.65f;
        protected override float Acceleration => 0.095f;
        protected override float MeleeRange => 82f;
        protected override float StabRange => 150f;
        protected override float ComboMaxStartRange => 360f;
        protected override int ClosingDistanceMaxTicks => 130;
        protected override int MeleeComboChance => 100;
        protected override int RangedStartMeleeComboChance => 70;
        protected override float ComboTelegraphMultiplier => 1.20f;
        protected override float ComboTelegraphAdvanceSpeedMult => 0.55f;
        protected override float ComboTelegraphAdvanceStopDistance => 70f;
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
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<Items.Weapons.Melee.Axes.AncientFireAxe>(), 2));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<DarkSoul>(), 1, 500, 750));
        }

        protected override void DoMeleeAttack()
        {
            SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.65f, PitchVariance = 0.2f }, NPC.Center);
            TryMeleeHit();
        }

        protected override void DoRangedAttack()
        {
            // No ranged weapon yet (RangedWeaponItemType = -1 keeps this from ever being called) —
            // the player axe's fireball-on-hit is a planned follow-up, not part of this pass.
        }

        // Every confirmed axe hit (plain swings, combos) sets the target ablaze — matches the
        // player AncientFireAxe's own fire theme (that one applies 12s; 6s here per the enemy tuning).
        protected override void OnBladeHit(Player player)
        {
            player.AddBuff(BuffID.OnFire, 6 * 60);
        }

        protected override bool CanSelectMeleeCombo(MeleeCombo combo, float distance, float healthFraction)
        {
            if (combo.Name == GreatfireBreakerName || combo.Name == BackstepReentryName)
                return healthFraction <= 0.66f;

            if (combo.Name == VolleyCommandName)
                return healthFraction <= 1f / 3f && CountCommandableFireballs() > 0;

            if (combo.Name == FirefallArrayName || combo.Name == ApexDiveName)
                return healthFraction <= 1f / 3f;

            return true;
        }

        protected override void OnMeleeComboTelegraphTick(
            MeleeCombo combo, MeleeComboStep step, int elapsed, int total)
        {
            if (combo.Name == FirefallArrayName && elapsed == 10)
                SpawnFirefallArray();
            else if (combo.Name == VolleyCommandName && elapsed == 0)
                ReserveSuspendedFireballs();
        }

        protected override void OnMeleeComboAttackTick(
            MeleeCombo combo, MeleeComboStep step, int elapsed, int total)
        {
            if (elapsed != total / 2)
                return;

            if (combo.Name == GreatfireCrescentName)
                SpawnGreatfireCrescent();
            else if (combo.Name == VolleyCommandName)
                CommandSuspendedFireballs();
        }

        private void SpawnGreatfireCrescent()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            int direction = NPC.direction < 0 ? -1 : 1;
            Vector2 origin = PuppetHandPosition + new Vector2(direction * 22f, 2f);
            Projectile.NewProjectile(
                NPC.GetSource_FromThis(),
                origin,
                new Vector2(direction * 9.5f, 0f),
                ModContent.ProjectileType<PuppetGreatfireCrescent>(),
                22,
                3f,
                Main.myPlayer,
                PuppetGreatfireCrescent.DirectMode);
            SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.62f, Pitch = 0.10f }, origin);
        }

        private void SpawnGreatfireBreakerWaves()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            int projectileType = ModContent.ProjectileType<PuppetGreatfireCrescent>();
            for (int direction = -1; direction <= 1; direction += 2)
            {
                Projectile.NewProjectile(
                    NPC.GetSource_FromThis(),
                    NPC.Bottom + new Vector2(direction * 14f, -24f),
                    new Vector2(direction * 7f, 0f),
                    projectileType,
                    24,
                    4f,
                    Main.myPlayer,
                    PuppetGreatfireCrescent.GroundMode);
            }
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
                    delays[i]);
            }

            SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.52f, Pitch = -0.22f }, target.Center);
        }

        private int CountCommandableFireballs()
        {
            int count = 0;
            int projectileType = ModContent.ProjectileType<EnemyGreatFireAxeFireball>();
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile projectile = Main.projectile[i];
                if (!projectile.active || projectile.type != projectileType)
                    continue;
                if (projectile.ModProjectile is EnemyGreatFireAxeFireball fireball
                    && fireball.CanBeCommandedBy(NPC.whoAmI))
                    count++;
            }
            return count;
        }

        private void CommandSuspendedFireballs()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            int commandIndex = 0;
            int projectileType = ModContent.ProjectileType<EnemyGreatFireAxeFireball>();
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile projectile = Main.projectile[i];
                if (!projectile.active || projectile.type != projectileType)
                    continue;
                if (projectile.ModProjectile is not EnemyGreatFireAxeFireball fireball
                    || !fireball.CanBeCommandedBy(NPC.whoAmI))
                    continue;

                fireball.CommandDive(commandIndex * 10);
                commandIndex++;
            }

            if (commandIndex > 0)
                SoundEngine.PlaySound(SoundID.Item45 with { Volume = 0.62f, Pitch = -0.10f }, NPC.Center);
        }

        private void ReserveSuspendedFireballs()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            int projectileType = ModContent.ProjectileType<EnemyGreatFireAxeFireball>();
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile projectile = Main.projectile[i];
                if (!projectile.active || projectile.type != projectileType)
                    continue;
                if (projectile.ModProjectile is EnemyGreatFireAxeFireball fireball
                    && fireball.CanBeCommandedBy(NPC.whoAmI))
                    fireball.ReserveForCommand(90);
            }
        }

        protected override bool ShouldContinueMeleeCombo(
            string comboName, int nextStepIndex, Player target, bool previousStepHit)
        {
            if (comboName == HighLeapFollowUpName && nextStepIndex == 1)
            {
                // The rising cut is a landing conversion, not a disconnected swing at empty space.
                return previousStepHit || NPC.Distance(target.Center) <= 140f;
            }

            return base.ShouldContinueMeleeCombo(comboName, nextStepIndex, target, previousStepHit);
        }

        protected override void OnComboStepCompleted(MeleeComboStep step)
        {
            bool breakerImpact = ActiveMeleeComboName == GreatfireBreakerName
                && step.Motion == ComboMotion.OverheadArc;
            bool landingImpact = (step.Motion == ComboMotion.LeapSlam
                || step.Motion == ComboMotion.ApexDiveCleave)
                && NPC.velocity.Y == 0f;
            if (!breakerImpact && !landingImpact)
                return;

            if (breakerImpact)
                SpawnGreatfireBreakerWaves();

            UsefulFunctions.ScreenShake(
                NPC.Bottom,
                breakerImpact ? 3.75f : 3.25f,
                breakerImpact ? 11 : 9,
                distanceFalloff: 560f);
            SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.42f, Pitch = 0.18f }, NPC.Bottom);

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

        // ── Fire Volley (ranged attack #1 of 6) ─────────────────────────────────────
        protected override bool  CanSpiralFan               => true;
        protected override float SpiralFanMinRange          => 200f;  // "at range", not melee
        protected override float SpiralFanMaxRange          => 700f;
        protected override int   SpiralFanChance            => 12;
        protected override int   SpiralFanCooldownAfterUse  => 400;   // whole chain can run long
        protected override int   SpiralFanSwingTelegraphTicks => 30;
        protected override int   SpiralFanSwingTicks        => 30;
        protected override int   SpiralFanFireTicks         => 4;
        protected override int   SpiralFanRecoveryTicks     => 1;     // "no recovery" after the chain ends

        // 30 ticks between each of the 3 shots (fires at index 0/1/2; -1 after 2 ends the volley).
        protected override int NextSpiralFanDelay(int completedShotIndex)
            => completedShotIndex < 2 ? 30 : -1;

        protected override void DoSpiralFanFire(int shotIndex)
            => FireOneVolleyShot(shotIndex % FireVolleySpawnOffsets.Length);

        // Left / middle (1 tile higher) / right, each ~3 tiles (48px) apart.
        private static readonly Vector2[] FireVolleySpawnOffsets =
        {
            new Vector2(-48f, 0f),
            new Vector2(0f, -16f),
            new Vector2(48f, 0f),
        };

        private void FireOneVolleyShot(int offsetIndex)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient || !NPC.HasValidTarget) return;
            Player target = Main.player[NPC.target];

            Vector2 spawnPos = NPC.Center + new Vector2(0f, -NPC.height * 0.9f) + FireVolleySpawnOffsets[offsetIndex];
            Vector2 toPlayer = target.Center - spawnPos;
            toPlayer.Y -= 120f; // bias the launch upward, not straight at them — "travel up and towards"
            if (toPlayer == Vector2.Zero) toPlayer = new Vector2(0f, -1f);
            toPlayer.Normalize();
            Vector2 velocity = toPlayer * 6f;

            SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.6f, PitchVariance = 0.2f }, NPC.Center);
            Projectile.NewProjectile(NPC.GetSource_FromThis(), spawnPos, velocity,
                ModContent.ProjectileType<EnemyGreatFireAxeFireball>(),
                20, 2f, Main.myPlayer,
                ai0: 0f, ai1: 0f, ai2: NPC.whoAmI + 1f);
        }

        // ── Chain: reposition (backward leap / dodge-through) -> volley 2 -> optional arc-jump -> volley 3
        private int _fireVolleyChain; // 0 = fresh, 1 = one bonus volley granted, 2 = arc-jump granted

        private const int   FireVolleyChainChance = 45; // roll after volley 1, to attempt volley 2
        private const int   FireVolleyArcChance   = 40; // roll after volley 2, to escalate into the arc-jump
        private const float FireVolleyCloseRange  = 220f; // below this, dodge-through instead of back-leap

        protected override void OnSpiralFanSequenceStart()
        {
            _fireVolleyChain = 0;
        }

        protected override bool TryContinueSpiralFanChain()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient || !NPC.HasValidTarget) return false;
            Player target = Main.player[NPC.target];

            if (_fireVolleyChain == 0)
            {
                if (Main.rand.Next(100) >= FireVolleyChainChance) return false;

                float dist = NPC.Distance(target.Center);
                if (dist <= FireVolleyCloseRange)
                {
                    _fireVolleyChain = 1;
                    EnterPhase(AttackPhase.FireVolleyDodgeThrough, FireVolleyDodgeThroughTimeoutTicks);
                    return true;
                }

                if (HasRoomToBackLeap())
                {
                    _fireVolleyChain = 1;
                    EnterPhase(AttackPhase.FireVolleyBackLeap, FireVolleyBackLeapTicks);
                    return true;
                }

                return false; // no valid reposition — ends here, no recovery either way
            }

            if (_fireVolleyChain == 1)
            {
                if (Main.rand.Next(100) >= FireVolleyArcChance) return false;
                _fireVolleyChain = 2;
                BeginFireVolleyArcJump();
                EnterPhase(AttackPhase.FireVolleyArcJump, 100);
                return true;
            }

            return false;
        }

        // Both reposition moves (backward leap, dodge-through) funnel back into the same swing
        // system for volley 2 — re-entering SpiralFanSwingTelegraph gives it its own axe-swing
        // telegraph, matching "with axe swing as telegraph" for the dodge-through landing too.
        protected override void OnFireVolleyRepositionLanded()
            => EnterPhase(AttackPhase.SpiralFanSwingTelegraph, SpiralFanSwingTelegraphTicks);

        // Volley 3: all 3 shots fire together mid-air at the arc's apex (one continuous swing,
        // not a staggered burst like the grounded volleys).
        protected override void DoFireVolleyArcFire()
        {
            for (int i = 0; i < FireVolleySpawnOffsets.Length; i++)
                FireOneVolleyShot(i);
        }

        protected override void OnFireVolleyArcJumpLanded()
            => EnterCasualOrIdle();

        /// <summary>Lightweight clearance probe for the backward leap: an unobstructed line to the
        /// landing point, and solid ground under it. "About 8 tiles if there's room" is approximate
        /// by design, so this doesn't need to be pixel-exact.</summary>
        private bool HasRoomToBackLeap()
        {
            int dir = -NPC.direction; // away from the player
            Vector2 landing = NPC.Center + new Vector2(dir * 128f, 0f); // ~8 tiles
            bool clearPath = Collision.CanHitLine(
                NPC.position, NPC.width, NPC.height,
                landing - new Vector2(NPC.width / 2f, NPC.height / 2f), NPC.width, NPC.height);
            bool groundBelow = Collision.SolidCollision(
                new Vector2(landing.X - 8f, landing.Y + NPC.height / 2f), 16, 24);
            return clearPath && groundBelow;
        }
    }
}

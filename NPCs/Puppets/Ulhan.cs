using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items.Armor.Ranged;
using tsorcRevamp.Content.Items.Weapons.Enemy;
using tsorcRevamp.Content.Items.Weapons.Ranged.Crossbows;
using tsorcRevamp.Content.Items.Weapons.Summon;
using tsorcRevamp.Content.Projectiles.Enemy.Weapons;
using tsorcRevamp.NPCs.AI;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.NPCs.Puppets
{
    [AutoloadBossHead]
    public class Ulhan : PuppetNPC
    {
        // PuppetNPC points Texture at the shared puppet placeholder, so [AutoloadBossHead]'s
        // Texture + "_Head_Boss" convention would look for "PuppetPlaceholder_Head_Boss" and crash at load.
        public override string BossHeadTexture => UsefulFunctions.RefactorableFilepath(typeof(Ulhan)) + "_Head_Boss";

        private enum BowMovement : byte { Stand, Walk, Run, Jump, ArcLeap }
        private readonly List<BowMovement> _movementBag = new List<BowMovement>();
        private BowMovement _movement;
        private BowMovement _lastMovement = BowMovement.Jump;
        private float _jumpVx;
        private float _jumpUpSpeed = 8.5f;
        private int _jumpElapsed;
        private int _jumpShotIndex;
        private bool _jumpBackwards;
        private bool _secondJumpStarted;
        private int _secondJumpElapsed;
        private bool _lastUsedDoubleJump;
        private Vector2 _shotAim = Vector2.UnitX;
        private bool _closingForExplosive;
        private int _approachTicks;
        private bool _minesPending;
        private Vector2 _mineTarget;
        private readonly List<(int Slot, int Identity)> _mines = new List<(int, int)>();
        private const float JumpGravity = 0.3f;
        private const float SecondJumpSpeed = 6.2f;
        private const int ArcEarlyShotTick = 14;
        // Explosive crossbow bolt: selected and pursued only inside this radius (17.5 tiles).
        // The approach is dropped the moment the target leaves it, so it never becomes a long chase.
        private const float ExplosiveArrowRange = 280f;
        private const int ExplosiveApproachTicks = 180;
        // The crossbow is held level, so the bolt may only tilt this far before it visibly leaves the stock.
        private const float ExplosiveMaxAimAngle = 0.26f;
        // Rapier jab step-in, px across the strike. Curve: 2 ease-in / 10 ease-out authored ticks, decay k=2;
        // a 64px (4-tile) jab over 10 ticks peaks ~15 px/t. Ends JabCloseTo px from the target's centre.
        private const int JabEaseInTicks = 2;
        private const int JabEaseOutTicks = 10;
        private const float JabEaseOutDecay = 2f;
        private const float JabStepInMin = 16f;
        private const float JabStepInMax = 64f;
        private const float JabCloseTo = 40f;
        // Rapier Lunge gap-closer: 7-15 tiles, same level. 40t vibrating guard tell, then 14 px/t (5t ramp)
        // solved to run 40px past the target (~15-24t). Contact only during the dash; roll through it.
        private const float LungeMaxRange = 240f;
        private const float LungeMinRange = 112f;
        private const float LungeMaxHeightGap = 48f;
        private const int LungeContactDamage = 65;
        private const int LungeAfterimageTailTicks = 8;
        private int JumpApexTicks => Math.Max(1, (int)Math.Round(_jumpUpSpeed / JumpGravity));

        protected override string InvaderTitle => "Ulhan";
        protected override int HeadArmorItemType => ModContent.ItemType<MirkwoodElvenBlondeHairStyle>();
        protected override int BodyArmorItemType => ModContent.ItemType<MirkwoodElvenLeatherArmor>();
        protected override int LegsArmorItemType => ModContent.ItemType<MirkwoodElvenLeggings>();
        protected override int MeleeWeaponItemType => ModContent.ItemType<EnemyOldRapier>();
        protected override int MeleeDamage => EnemyDamage.Projectile(55);
        protected override WeaponArchetype MeleeArchetype => WeaponArchetype.Rapier;
        protected override float MeleeRange => 76f;
        // Jab step-in (up to JabStepInMax) lets combos open from farther out than the 53px hitbox alone.
        protected override float MeleeEngageRange => 100f;
        protected override float ComboMaxStartRange => 110f;
        protected override bool CanPierce => !_closingForExplosive && !_minesPending && NPC.HasValidTarget
            && Math.Abs(Main.player[NPC.target].Center.Y - NPC.Center.Y) <= LungeMaxHeightGap;
        protected override int PierceStabChance => 0;
        protected override float PierceRange => LungeMaxRange;
        protected override float MinPierceRange => LungeMinRange;
        protected override int PierceChance => 5;
        protected override int PierceTelegraphTicks => 40;
        protected override float PierceDashSpeed => 14f;
        protected override int PierceDashAccelTicks => 5;
        protected override float PierceOvershootPastTarget => 40f;
        protected override int PierceDashMaxTicks => 26;
        protected override int PierceRecoveryTicks => 50;
        // 10 s, counted from the end of the lunge's recovery.
        protected override int PierceCooldownAfterUse => 600;
        protected override float RangedStartComboMaxRange => 100f;
        protected override int MeleeComboChance => 100;
        protected override int RangedStartMeleeComboChance => 0;
        protected override float MeleeBladeWidth => 16f;
        protected override Vector2 MeleeHandleNorm => new Vector2(0.18f, 0.82f);
        protected override float MeleeWeaponDrawScale => 1.1f;
        protected override int MeleeComboInterStepLingerTicks => 4;
        protected override bool UseSmoothRapierThrustPose => true;
        protected override bool UseAuthoredComboSwingClock => true;
        protected override bool AuthoredClockCoversJoustDash => true;
        protected override int MeleeRecoveryTicks => 28;
        protected override int RangedWeaponItemType => ModContent.ItemType<EnemyTaintedBow>();
        // Same Heavy Crossbow prop the Studded Leather Warrior fires its bolts from.
        protected override int SecondaryRangedWeaponItemType => ModContent.ItemType<HeavyCrossbow>();
        protected override int RangedDamage => EnemyDamage.Projectile(50);
        protected override int SecondaryRangedDamage => EnemyDamage.Projectile(75);
        // The former EnemyCaltrop throw is disabled for Ulhan; its shared projectile stays intact.
        protected override int MagicWeaponItemType => ModContent.ItemType<EnemyProximityMine>();
        protected override int MagicDamage => EnemyDamage.Projectile(50);
        protected override float MinMagicRange => 80f;
        protected override float MagicRange => _minesPending && NPC.HasValidTarget && NPC.velocity.Y == 0f
            && Math.Abs(Main.player[NPC.target].Center.Y - NPC.Center.Y) <= 64f
            && Collision.CanHitLine(NPC.Center, 1, 1, Main.player[NPC.target].Center, 1, 1) ? 520f : 0f;
        protected override int MagicPreferenceChance => 100;
        protected override int MagicTelegraphTicks => 60;
        // Release, recover for 6t, raise through Use2/Use1 for 24t, release again at +30t.
        protected override int MagicAttackTicks => 42;
        protected override int MagicAttackBodyRow => PhaseTimer > 36 || PhaseTimer <= 12 ? 3
            : PhaseTimer > 24 ? 2 : 1;
        protected override float MagicAttackPoseProgress => PhaseTimer > 12
            ? 1f - (MagicAttackTicks - PhaseTimer) / 30f
            : 1f - (12f - PhaseTimer) / 12f;
        protected override int MagicRecoveryTicks => 60;
        protected override int MagicCooldownAfterUse => 480;
        protected override Color MagicTelegraphFlashColor => new Color(255, 170, 45);
        protected override int EstusChargesMax => 0;
        protected override bool PuppetCanDoubleJump => true;
        protected override float PuppetDoubleJumpPower => SecondJumpSpeed;
        protected override int CasualStrollChance => 0;
        protected override bool ShowRangedWeaponDuringRecovery => true;
        protected override float TopSpeed => 2.8f;
        protected override RangedStyle RangedAnimStyle => RangedStyle.Bow;
        protected override RangedStyle SecondaryRangedAnimStyle => RangedStyle.Crossbow;
        protected override float RangedRange => 900f;
        protected override float MinRangedRange => _closingForExplosive ? float.MaxValue : 0f;
        protected override float SecondaryRangedRange => ExplosiveArrowRange;
        protected override float SecondaryRangedMinRange => 0f;
        protected override bool SecondaryRangedAvailable => _closingForExplosive;
        protected override int SecondaryRangedChance => 100;
        protected override int RangedTelegraphTicks => 42;
        protected override int RangedRecoveryTicks => 36;
        protected override int RangedCooldownAfterUse => 90;
        protected override int SecondaryRangedTelegraphTicks => 48;
        protected override int SecondaryRangedRecoveryTicks => 60;
        protected override int SecondaryRangedCooldownAfterUse => 0;
        protected override int SecondaryMaxRangedBurst => 1;
        protected override int StandingRangedChance => _movement == BowMovement.Stand ? 100 : 0;
        protected override int SecondaryStandingRangedChance => 100;
        protected override int[][] PrimaryRangedBurstPatterns => _movement switch
        {
            BowMovement.Jump => _jumpBackwards ? new[] { Array.Empty<int>() }
                : new[] { new[] { 8 }, new[] { 8, 8 }, new[] { 8, 8, 8 } },
            BowMovement.ArcLeap => new[] { new[] { 12, 12 } },
            _ => new[] { new[] { 45, 45 } }
        };
        protected override bool AllowRangedPatternsAirborne => true;
        protected override Color RangedTelegraphFlashColor => new Color(175, 100, 220);
        protected override Color SecondaryRangedFlashColor => new Color(255, 150, 40);
        protected override bool UseCompositeArmSwing => true;
        protected override bool UseCompositeArmForAdditionalPhase =>
            Phase == AttackPhase.RangedTelegraph || Phase == AttackPhase.RangedAttack
            || Phase == AttackPhase.CrossbowBurstPause;
        protected override bool UseBowStringDrawPose => true;
        // TaintedBow: right-facing 18x46; riser texel (14,23), left-edge string rows 2..43.
        protected override Vector2 GetHeldRangedGripNorm(int itemType) => new Vector2(0.78f, 0.5f);
        protected override float GetHeldRangedDrawScale(int itemType) => 0.9f;
        protected override Rectangle GetBowStringTexels(int itemType) => new Rectangle(0, 2, 2, 42);
        // Only the bow nocks an arrow; the explosive bolt is a crossbow shot with no string pose.
        protected override string NockedBowArrowTexture => "Terraria/Images/Projectile_" + ProjectileID.UnholyArrow;
        // Explosive bolt leaves the level crossbow stock, not the bow grip. Position-only (no draw puppet)
        // so the server's spawn point and every client's telegraph agree. Same offset as Studded Leather Warrior.
        private Vector2 CrossbowMuzzle => NPC.Center + new Vector2(NPC.direction * 14f, -NPC.height * 0.2f);
        protected override float BowAimAngle => (float)Math.Atan2(_shotAim.Y, _shotAim.X * NPC.direction);

        protected override void CustomizeMeleeCombo(ref MeleeCombo combo, float healthFraction)
        {
            // Keep all five shared rapier moves, but separate chained thrusts by a full
            // roll cooldown. The player can dodge each jab without an unavoidable gap hit.
            for (int i = 0; i < combo.Steps.Length - 1; i++)
            {
                MeleeComboStep step = combo.Steps[i];
                step.PostStepPause = Math.Max(step.PostStepPause, 30);
                combo.Steps[i] = step;
            }

            // Each rapier jab lunges on the engine's shaped step-in (Weighted ease + StepInDistance): speed peaks
            // on tick 2 with the blade's front-loaded drive, then decays. The distance itself is re-solved from
            // the live gap as each jab starts (SolveJabStepIn); JabStepInMin is only the fallback.
            for (int i = 0; i < combo.Steps.Length; i++)
            {
                MeleeComboStep step = combo.Steps[i];

                if (step.Motion == ComboMotion.Thrust)
                {
                    step.ForwardPushMult = 0f;
                    step.Ease = SwingEaseStyle.Weighted;
                    step.EaseInTicks = JabEaseInTicks;
                    step.EaseOutTicks = JabEaseOutTicks;
                    step.EaseOutDecay = JabEaseOutDecay;
                    step.StepInDistance = JabStepInMin;
                    combo.Steps[i] = step;
                }
            }
            combo.RecoveryTicks = combo.Name == "Lunging Finisher" ? 44
                : combo.Name == "Triple Flurry" ? 36 : 28;
        }

        protected override void ModifyOpeningMeleeComboStep(string comboName, Player target, ref MeleeComboStep openingStep)
        {
            SolveJabStepIn(target, ref openingStep);
        }

        protected override void ModifyNextMeleeComboStep(string comboName, int nextStepIndex, Player target, ref MeleeComboStep nextStep)
        {
            SolveJabStepIn(target, ref nextStep);
        }

        // Size the jab's lunge to the horizontal gap so the strike ends with the target JabCloseTo px away
        // (inside the 53px hitbox): a close target gets a short step, a backing-off one up to 4 tiles.
        private void SolveJabStepIn(Player target, ref MeleeComboStep step)
        {
            if (step.Motion != ComboMotion.Thrust)
            {
                return;
            }

            float gap = Math.Abs(target.Center.X - NPC.Center.X);
            step.StepInDistance = MathHelper.Clamp(gap - JabCloseTo, JabStepInMin, JabStepInMax);
        }

        // Rapier Lunge (engine Piercing Dash): telegraph holds the raised rapier guard, then a distance-solved dash.
        protected override float PierceTelegraphRotation => RapierGuardRotation;

        protected override void DoPierceWindup(int elapsed)
        {
            if (elapsed == 0)
            {
                SpawnTelegraphFlash(Color.White);
                SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.6f, Pitch = -0.3f }, NPC.Center);
            }

            if (Main.dedServ)
            {
                return;
            }

            // Silver glints gathering along the leveled blade line ahead of him.
            if (elapsed % 3 == 0)
            {
                Vector2 glintPosition = NPC.Center + new Vector2(NPC.direction * Main.rand.NextFloat(20f, 50f), Main.rand.NextFloat(-6f, 2f));
                Dust glint = Dust.NewDustPerfect(glintPosition, DustID.SilverCoin, new Vector2(-NPC.direction * 0.8f, 0f), 80, default, 0.8f);
                glint.noGravity = true;
            }
        }

        protected override void DoPierceDashTick()
        {
            // Keep the cached-position body echoes alive for the whole dash plus a short fade past its end.
            AfterimageTicks = Math.Max(AfterimageTicks, LungeAfterimageTailTicks);

            if (Main.dedServ)
            {
                return;
            }

            Vector2 streakVelocity = -NPC.velocity * 0.15f;
            Dust streak = Dust.NewDustPerfect(NPC.Center + Main.rand.NextVector2Circular(8f, 16f), DustID.Smoke, streakVelocity, 120, Color.LightGray, 1.1f);
            streak.noGravity = true;
        }

        // Lunging Finisher's closing JoustDash starts from the rapier guard the preceding jab pause retracted to,
        // instead of the generic 45°-low couch, which opened the dash with a ~60° snap.
        protected override void ModifyMeleeArcEndpoints(ComboMotion motion, ref float startRotation, ref float endRotation)
        {
            if (motion == ComboMotion.JoustDash)
            {
                startRotation = RapierGuardRotation;
            }
        }

        public override void SetStaticDefaults()
        {
            base.SetStaticDefaults();
            // Caches NPC.oldPos for the Rapier Lunge's body afterimages.
            NPCID.Sets.TrailCacheLength[NPC.type] = 10;
            NPCID.Sets.TrailingMode[NPC.type] = 0;
        }

        public override void SetDefaults()
        {
            NPC.width = 20;
            NPC.height = 42;
            NPC.lifeMax = 11000;
            NPC.defense = 16;
            EnemyDamage.SetContact(NPC, 0);
            NPC.knockBackResist = 0.22f;
            NPC.aiStyle = -1;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath2;
            NPC.value = 50000f;
            NPC.boss = true;
            NPC.npcSlots = 10f;
            var globalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            globalNPC.PoiseMax = 35f;
            globalNPC.PoiseStaggerResetsAI = true;
            globalNPC.NavSearchRadius = 80; // Match the other puppet SF4 movers; enables ledge/pit routing and patrol navigation.
            globalNPC.NavGiveUpTicks = 180;
            globalNPC.CanUseRopes = true;
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot)
            => npcLoot.Add(ItemDropRule.ByCondition(new FirstBossKillRule(), ModContent.ItemType<ArcherSpiritBell>()));

        public override void OnKill()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;
            ClearMines();
            var definition = new Terraria.ModLoader.Config.NPCDefinition(Type);
            if (!tsorcRevampWorld.NewSlain.ContainsKey(definition))
            {
                tsorcRevampWorld.NewSlain.Add(definition, 1);
                if (Main.netMode == NetmodeID.Server)
                    NetMessage.SendData(MessageID.WorldData);
            }
        }

        protected override void FaceTargetForRangedShot(Player target)
        {
            // The final eight tell ticks and release retain the synchronized shot's facing.
            if (Phase == AttackPhase.RangedAttack || PhaseTimer <= 8)
                NPC.direction = NPC.spriteDirection = _shotAim.X < 0f ? -1 : 1;
            else
                base.FaceTargetForRangedShot(target);
        }

        public override void AI()
        {
            DebugAttackLabel = Phase == AttackPhase.MeleeComboTelegraph || Phase == AttackPhase.MeleeComboAttack
                || Phase == AttackPhase.MeleeComboPause || Phase == AttackPhase.MeleeComboRecovery
                ? "Rapier: " + ActiveMeleeComboName
                : Phase == AttackPhase.MagicTelegraph || Phase == AttackPhase.MagicAttack
                || Phase == AttackPhase.MagicRecovery ? "Proximity Mine Throw"
                : Phase == AttackPhase.RangedTelegraph || Phase == AttackPhase.RangedAttack
                || Phase == AttackPhase.CrossbowBurstPause || Phase == AttackPhase.RangedRecovery
                ? IsSecondaryRangedActive ? "Explosive Arrow" : _movement + " Unholy Bow Shot"
                : _closingForExplosive ? "Explosive Arrow Approach" : null;
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                if (_closingForExplosive)
                {
                    // Drop the explosive approach when it times out (unreachable terrain) or the target
                    // steps outside ExplosiveArrowRange; the normal bow kit resumes instead of a long chase.
                    _approachTicks--;
                    bool targetOutOfRange = !NPC.HasValidTarget
                        || NPC.Distance(Main.player[NPC.target].Center) > ExplosiveArrowRange;
                    bool explosiveShotStarted = IsSecondaryRangedActive
                        && (Phase == AttackPhase.RangedTelegraph || Phase == AttackPhase.RangedAttack);

                    if (!explosiveShotStarted && (_approachTicks <= 0 || targetOutOfRange))
                    {
                        _closingForExplosive = false;
                        NPC.netUpdate = true;
                    }
                }
                if ((Phase == AttackPhase.RangedTelegraph || Phase == AttackPhase.CrossbowBurstPause)
                    && (PhaseTimer > 8 || _movement == BowMovement.ArcLeap) && NPC.HasValidTarget)
                {
                    _shotAim = _movement == BowMovement.ArcLeap && !IsSecondaryRangedActive
                        ? SolveArcAim() : SolveShotAim(IsSecondaryRangedActive ? 10f : 13f);
                    if (PhaseTimer % 6 == 0 || PhaseTimer == 9)
                        NPC.netUpdate = true;
                }
            }
            AttackPhase previousPhase = Phase;
            base.AI();

            // Contact hurts only during the Rapier Lunge dash. Every peer derives it from the synced phase,
            // so the hit player's own machine resolves the contact (and its roll i-frames) itself.
            int contactDamage = 0;

            if (Phase == AttackPhase.PierceDash)
            {
                contactDamage = EnemyDamage.ScaleContact(LungeContactDamage);
            }
            NPC.damage = contactDamage;
            if (HasRangedJumpOverride && previousPhase == AttackPhase.CrossbowBurstPause)
                TickRangedJumpOverride();
            else if (HasRangedJumpOverride && previousPhase == AttackPhase.RangedRecovery
                && NPC.velocity.Y != 0f)
                TickRangedJumpOverride();
            bool usedDoubleJump = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>().UsedDoubleJump;
            if (usedDoubleJump && !_lastUsedDoubleJump)
                PlayDoubleJumpEffect();
            _lastUsedDoubleJump = usedDoubleJump;
            // The bow is only out while shooting; idle and strolling show no slung weapon.
            if (Phase == AttackPhase.RangedRecovery)
            {
                int recoveryWeapon = IsSecondaryRangedActive ? SecondaryRangedWeaponItemType : RangedWeaponItemType;
                SetDisplayWeapon(recoveryWeapon, swing: false);
            }
            if (Main.netMode == NetmodeID.MultiplayerClient && previousPhase == AttackPhase.MagicTelegraph
                && Phase == AttackPhase.MagicAttack)
                PlayThrowSound();
            if (Phase == AttackPhase.CrossbowBurstPause)
                DoRangedTelegraphVFX(false, MathHelper.Clamp(1f - PhaseTimer / (_movement == BowMovement.ArcLeap ? 12f : _movement == BowMovement.Jump ? 8f : 45f), 0f, 1f));
            if (Phase == AttackPhase.MagicTelegraph && !Main.dedServ && Main.GameUpdateCount % 4 == 0)
            {
                // These are the shared magic/throw body's authored Use2 and Use1 hand offsets.
                Vector2 hand = NPC.Center + (PhaseTimer > MagicTelegraphTicks * 0.5f
                    ? new Vector2(4f * NPC.direction, -8f) : new Vector2(-8f * NPC.direction, -9f));
                Dust dust = Dust.NewDustPerfect(hand + Main.rand.NextVector2Circular(3f, 3f),
                    Main.rand.NextBool() ? DustID.OrangeTorch : DustID.Torch,
                    new Vector2(0f, -0.25f), 80, default, 0.65f);
                dust.noGravity = true;
                if (Main.GameUpdateCount % 12 == 0)
                {
                    Vector2 release = NPC.Center + new Vector2(4f * NPC.direction, 2f);
                    for (int side = -1; side <= 1; side += 2)
                    {
                        Vector2 target = _mineTarget + new Vector2(side * NPC.direction * 90f, 0f);
                        for (int i = 1; i <= 6; i++)
                        {
                            Vector2 point = Vector2.Lerp(release, target, i / 6f);
                            Dust marker = Dust.NewDustPerfect(point, DustID.OrangeTorch,
                                Vector2.Zero, 160, default, 0.4f);
                            marker.noGravity = true;
                        }
                    }
                }
            }
        }

        protected override void OnMagicTelegraphStarting()
        {
            Player target = Main.player[NPC.target];
            Vector2 origin = NPC.Center + new Vector2(4f * NPC.direction, 2f);
            Vector2 offset = new Vector2(target.Center.X + target.velocity.X * 18f,
                target.Bottom.Y) - origin;
            if (offset.LengthSquared() > 520f * 520f)
                offset = offset.SafeNormalize(new Vector2(NPC.direction, 0f)) * 520f;
            _mineTarget = origin + offset;
            NPC.netUpdate = true;
        }

        protected override void DoMagicAttack()
        {
            PlayThrowSound();
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;
            ThrowMines(_mineTarget - new Vector2(NPC.direction * 90f, 0f));
        }

        protected override void DoMagicTick(int ticksRemaining)
        {
            if (ticksRemaining != 12)
                return;
            PlayThrowSound();
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;
            ThrowMines(_mineTarget + new Vector2(NPC.direction * 90f, 0f));
            _minesPending = false;

            // Arm the explosive follow-up only if the target is already close; a distant target
            // goes straight back to the bow rotation instead of being chased down.
            bool targetInExplosiveRange = NPC.HasValidTarget
                && NPC.Distance(Main.player[NPC.target].Center) <= ExplosiveArrowRange;
            _closingForExplosive = targetInExplosiveRange;
            _approachTicks = ExplosiveApproachTicks;
            NPC.netUpdate = true;
        }

        private void ThrowMines(Vector2 target)
        {
            // Match the forward Use3 hand displayed on the release frame, on every peer.
            Vector2 origin = NPC.Center + new Vector2(4f * NPC.direction, 2f);
            _mines.RemoveAll(tracked => !Main.projectile[tracked.Slot].active
                || Main.projectile[tracked.Slot].identity != tracked.Identity);
            foreach (int slot in ProximityMine.ThrowSpread(
                NPC.GetSource_FromThis(), origin, target, MagicDamage, 1.5f,
                Main.myPlayer, NPC.whoAmI))
            {
                if (slot >= 0 && slot < Main.maxProjectiles && Main.projectile[slot].active)
                    _mines.Add((slot, Main.projectile[slot].identity));
            }
        }

        protected override void OnPartyWipeDespawnStarted()
        {
            base.OnPartyWipeDespawnStarted();
            ClearMines();
        }

        private void ClearMines()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;
            foreach (var tracked in _mines)
            {
                Projectile projectile = Main.projectile[tracked.Slot];
                if (projectile.active && projectile.identity == tracked.Identity
                    && projectile.type == ModContent.ProjectileType<ProximityMine>())
                    projectile.Kill();
            }
            _mines.Clear();
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile projectile = Main.projectile[i];
                if (projectile.active && projectile.type == ModContent.ProjectileType<ProximityMineRocket>()
                    && projectile.ai[2] == NPC.whoAmI + 1
                    && projectile.ModProjectile is ProximityMineRocket rocket)
                    rocket.Disarm();
            }
        }

        protected override void RunMovementAI(float speedMult)
        {
            var globalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            globalNPC.RemembersLastKnownPos = true;
            // Scripted bow leaps provide their own second jump; navigation must not spend it early.
            globalNPC.CanDoubleJump = !(HasRangedJumpOverride
                && (Phase == AttackPhase.RangedTelegraph || Phase == AttackPhase.RangedAttack
                    || Phase == AttackPhase.CrossbowBurstPause || Phase == AttackPhase.RangedRecovery));
            if (_movement == BowMovement.Walk && (Phase == AttackPhase.RangedTelegraph
                || Phase == AttackPhase.RangedAttack || Phase == AttackPhase.CrossbowBurstPause))
                speedMult *= 0.45f;
            SmartFighter4AI.Run(NPC, topSpeed: TopSpeed * speedMult, acceleration: Acceleration,
                doorBreakingDamage: 4, attackRange: _closingForExplosive ? 160f
                    : _minesPending ? 480f : 232f);
        }

        private BowMovement DrawMovement()
        {
            if (_movementBag.Count == 0)
            {
                _movementBag.AddRange(new[] { BowMovement.Stand, BowMovement.Walk, BowMovement.Run,
                    BowMovement.Jump, BowMovement.ArcLeap });
                for (int i = _movementBag.Count - 1; i > 0; i--)
                {
                    int swap = Main.rand.Next(i + 1);
                    BowMovement held = _movementBag[i];
                    _movementBag[i] = _movementBag[swap];
                    _movementBag[swap] = held;
                }
                int last = _movementBag.Count - 1;
                if (_movementBag[last] == _lastMovement)
                {
                    BowMovement held = _movementBag[last];
                    _movementBag[last] = _movementBag[0];
                    _movementBag[0] = held;
                }
            }
            int index = _movementBag.Count - 1;
            _lastMovement = _movementBag[index];
            _movementBag.RemoveAt(index);
            return _lastMovement;
        }

        protected override void ModifyRangedBurst(bool secondary, ref int itemType, ref RangedStyle style,
            ref Color flashColor, ref int telegraphTicks, ref int attackTicks, ref int recoveryTicks)
        {
            _movement = secondary ? BowMovement.Stand : DrawMovement();
            _jumpUpSpeed = 8.5f;
            _jumpElapsed = 0;
            _jumpShotIndex = 0;
            _secondJumpStarted = false;
            _secondJumpElapsed = 0;
            _jumpBackwards = !secondary && Main.rand.NextBool();
            if (secondary)
                _closingForExplosive = false;
            else if (NPC.HasValidTarget && NPC.velocity.Y == 0f)
            {
                Player target = Main.player[NPC.target];
                float heightAbove = PuppetBowGripPosition.Y - target.Center.Y;
                if (heightAbove >= 48f || (heightAbove >= 16f
                    && !Collision.CanHitLine(PuppetBowGripPosition, 1, 1, target.Center, 1, 1)))
                {
                    _movement = BowMovement.Jump;
                    _jumpUpSpeed = (float)Math.Sqrt(2f * JumpGravity * MathHelper.Clamp(heightAbove, 48f, 112f));
                }
            }
            // Only launch a scripted hop from the ground; airborne navigation already owns its arc.
            if ((_movement == BowMovement.Jump || _movement == BowMovement.ArcLeap)
                && NPC.velocity.Y != 0f)
                _movement = BowMovement.Stand;
            if (_movement == BowMovement.ArcLeap && !TrySolveArcLeap(out _jumpVx))
                _movement = BowMovement.Jump;
            if (_movement == BowMovement.Jump && !HasJumpHeadroom(190f))
                _movement = BowMovement.Stand;
            if (_movement == BowMovement.Jump && NPC.HasValidTarget)
            {
                _jumpVx = (Main.player[NPC.target].Center.X < NPC.Center.X ? -1 : 1) * TopSpeed * 1.6f;
                telegraphTicks = JumpApexTicks;
                attackTicks = 2;
            }
            else if (_movement == BowMovement.ArcLeap)
            {
                // Draw on the ground, then take off: the three ordinary release poses
                // land at flight ticks 14, 28 and 42 rather than firing mid-pull.
                telegraphTicks = JumpApexTicks + ArcEarlyShotTick;
                attackTicks = 2;
            }
            _shotAim = _movement == BowMovement.ArcLeap ? SolveArcAim()
                : SolveShotAim(secondary ? 10f : 13f);
            NPC.netUpdate = true;
        }

        protected override bool HasRangedJumpOverride => !IsSecondaryRangedActive
            && (_movement == BowMovement.Jump || _movement == BowMovement.ArcLeap);
        protected override void TickRangedJumpOverride()
        {
            if (Phase != AttackPhase.RangedTelegraph && Phase != AttackPhase.RangedAttack
                && Phase != AttackPhase.CrossbowBurstPause && Phase != AttackPhase.RangedRecovery)
                return;
            if (_movement == BowMovement.ArcLeap && Phase == AttackPhase.RangedTelegraph
                && PhaseTimer > ArcEarlyShotTick)
            {
                NPC.velocity.X *= 0.1f;
                return;
            }
            if (_jumpElapsed > JumpApexTicks + 2 && NPC.velocity.Y == 0f)
            {
                NPC.velocity.X *= 0.7f;
                return;
            }
            int elapsed = _jumpElapsed++;
            if (_movement == BowMovement.Jump && elapsed == JumpApexTicks + 9)
            {
                _secondJumpStarted = true;
                _secondJumpElapsed = 0;
                _jumpVx = (_jumpBackwards ? -1f : 1f) * Math.Sign(_jumpVx)
                    * TopSpeed * (_jumpBackwards ? 1.15f : 1.6f);
                NPC.GetGlobalNPC<tsorcRevampGlobalNPC>().UsedDoubleJump = true;
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    NPC.netUpdate = true;
            }
            NPC.velocity.X = _jumpVx;
            NPC.velocity.Y = _secondJumpStarted
                ? -SecondJumpSpeed + JumpGravity * _secondJumpElapsed++
                : -_jumpUpSpeed + JumpGravity * elapsed;
        }

        private bool HasJumpHeadroom(float height)
            => !Collision.SolidCollision(NPC.position - new Vector2(0f, height),
                NPC.width, (int)height);

        private bool TrySolveArcLeap(out float velocityX)
        {
            velocityX = 0f;
            if (!NPC.HasValidTarget || NPC.velocity.Y != 0f || !HasJumpHeadroom(140f))
                return false;
            Player target = Main.player[NPC.target];
            float horizontal = target.Center.X - NPC.Center.X;
            float distance = Math.Abs(horizontal);
            if (distance < 12f * 16f || distance > 15f * 16f
                || Math.Abs(target.Bottom.Y - NPC.Bottom.Y) > 24f)
                return false;
            int direction = Math.Sign(horizontal);
            int airtime = JumpApexTicks * 2;
            velocityX = direction * (distance + 13.5f * 16f) / airtime;
            if (Math.Abs(velocityX) > 8.2f)
                return false;
            for (int tick = 2; tick <= airtime; tick += 2)
            {
                float x = NPC.position.X + velocityX * tick;
                float y = NPC.position.Y - _jumpUpSpeed * tick
                    + JumpGravity * tick * (tick - 1f) * 0.5f;
                if (Collision.SolidCollision(new Vector2(x, y), NPC.width, NPC.height))
                    return false;
            }
            Vector2 landingFeet = NPC.Bottom + new Vector2(velocityX * airtime, 2f);
            return Collision.SolidCollision(landingFeet - new Vector2(NPC.width * 0.5f, 0f),
                NPC.width, 5);
        }

        private Vector2 SolveArcAim()
        {
            if (!NPC.HasValidTarget)
                return new Vector2(NPC.direction, 0f);
            Player target = Main.player[NPC.target];
            Vector2 origin = PuppetBowGripPosition;
            float flightTicks = Vector2.Distance(origin, target.Center) / 13f;
            Vector2 predicted = target.Center + target.velocity * Math.Min(flightTicks, 16f) * 0.5f;
            predicted.Y -= 0.05f * flightTicks * flightTicks;
            return (predicted - origin).SafeNormalize(new Vector2(NPC.direction, 0f));
        }

        private void PlayDoubleJumpEffect()
        {
            if (Main.dedServ)
                return;
            SoundEngine.PlaySound(SoundID.DoubleJump, NPC.Bottom);
            for (int i = 0; i < 18; i++)
            {
                Vector2 velocity = Main.rand.NextVector2Circular(2.2f, 1.1f) + new Vector2(0f, 0.8f);
                Dust cloud = Dust.NewDustPerfect(NPC.Bottom + Main.rand.NextVector2Circular(9f, 2f),
                    DustID.Cloud, velocity, 100, Color.White, Main.rand.NextFloat(0.8f, 1.2f));
                cloud.noGravity = true;
            }
        }

        private Vector2 SolveShotAim(float speed)
        {
            if (!NPC.HasValidTarget)
                return new Vector2(NPC.direction, 0f);
            Player target = Main.player[NPC.target];
            Vector2 origin = PuppetBowGripPosition;
            float maxAimAngle = MathHelper.PiOver4;
            if (IsSecondaryRangedActive)
            {
                origin = CrossbowMuzzle;
                maxAimAngle = ExplosiveMaxAimAngle;
            }
            Vector2 aimAt = target.Center;
            for (int i = 0; i < 4; i++)
            {
                float flightTicks = Vector2.Distance(origin, aimAt) / speed;
                float fallingTicks = Math.Max(0f, flightTicks - 15f);
                aimAt = target.Center + new Vector2(target.velocity.X, target.velocity.Y * 0.5f) * flightTicks
                    - new Vector2(0f, 0.1f * fallingTicks * (fallingTicks + 1f) * 0.5f);
            }
            Vector2 direction = (aimAt - origin).SafeNormalize(new Vector2(NPC.direction, 0f));
            // Same ±45-degree articulation limit as Owl Father's bow hold (±15° for the level crossbow).
            int facing = direction.X < 0f ? -1 : 1;
            float angle = MathHelper.Clamp((float)Math.Atan2(direction.Y, Math.Abs(direction.X)),
                -maxAimAngle, maxAimAngle);
            return new Vector2((float)Math.Cos(angle) * facing, (float)Math.Sin(angle));
        }

        protected override void DoMeleeAttack() { }
        protected override void DoRangedAttack()
        {
            if (!NPC.HasValidTarget)
                return;
            bool explosive = IsSecondaryRangedActive;
            if (!explosive && _movement == BowMovement.ArcLeap)
                _shotAim = SolveArcAim();
            else if (!explosive && _movement == BowMovement.Jump)
                _shotAim = SolveShotAim(13f);
            if (!explosive && _movement == BowMovement.Jump && _jumpBackwards && _jumpShotIndex == 0)
            {
                FireUnholyArrow(_shotAim.RotatedBy(-0.16f));
                FireUnholyArrow(_shotAim, playSound: false);
                FireUnholyArrow(_shotAim.RotatedBy(0.16f), playSound: false);
            }
            else if (!explosive)
                FireUnholyArrow(_shotAim);
            else
            {
                Vector2 origin = CrossbowMuzzle;
                if (!Main.dedServ)
                {
                    SoundEngine.PlaySound(SoundID.Item98 with { Volume = 0.75f, PitchVariance = 0.1f }, origin);
                }
                if (Main.netMode != NetmodeID.MultiplayerClient)
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), origin, _shotAim * 10f,
                        ModContent.ProjectileType<UlhanExplosiveArrow>(), SecondaryRangedDamage, 2f, Main.myPlayer);
            }
            _jumpShotIndex++;
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;
            if (!explosive && IsFinalBurstShot)
            {
                _minesPending = true;
                NPC.netUpdate = true;
            }
        }

        private void FireUnholyArrow(Vector2 direction, bool playSound = true)
        {
            Vector2 origin = PuppetBowGripPosition;
            if (playSound && !Main.dedServ)
                SoundEngine.PlaySound(SoundID.Item5 with { Volume = 0.7f, PitchVariance = 0.15f }, origin);
            if (Main.netMode != NetmodeID.MultiplayerClient)
                Projectile.NewProjectile(NPC.GetSource_FromThis(), origin, direction * 13f,
                    ModContent.ProjectileType<UlhanUnholyArrow>(), RangedDamage, 2f, Main.myPlayer);
        }

        protected override void DoRangedTelegraphVFX(bool secondary, float progress)
        {
            if (Main.dedServ)
                return;
            Vector2 origin = PuppetBowGripPosition;
            if (secondary)
            {
                origin = CrossbowMuzzle;
            }
            if (Main.GameUpdateCount % 3 == 0)
            {
                Dust dust = Dust.NewDustPerfect(origin + Main.rand.NextVector2Circular(3f, 3f),
                    secondary ? DustID.OrangeTorch : DustID.DemonTorch, -_shotAim * 0.5f, 80, default, 0.7f);
                dust.noGravity = true;
            }
            if (Main.GameUpdateCount % 6 == 0)
            {
                float reach = secondary ? UlhanExplosiveArrow.MaximumTravel : 240f;
                for (int i = 1; i <= 8; i++)
                {
                    Dust dust = Dust.NewDustPerfect(origin + _shotAim * (reach * i / 8f),
                        secondary ? DustID.OrangeTorch : DustID.DemonTorch, Vector2.Zero, 180, default, 0.35f);
                    dust.noGravity = true;
                }
            }
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write((byte)_movement);
            writer.Write(_jumpVx);
            writer.Write(_jumpUpSpeed);
            writer.Write(_jumpElapsed);
            writer.Write(_jumpShotIndex);
            writer.Write(_jumpBackwards);
            writer.Write(_secondJumpStarted);
            writer.Write(_secondJumpElapsed);
            writer.Write(_shotAim.X);
            writer.Write(_shotAim.Y);
            writer.Write(_closingForExplosive);
            writer.Write(_approachTicks);
            writer.Write(_minesPending);
            writer.Write(_mineTarget.X);
            writer.Write(_mineTarget.Y);
            base.SendExtraAI(writer);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            // Read style first: base reconstructs the active burst pattern using this getter.
            _movement = (BowMovement)reader.ReadByte();
            _jumpVx = reader.ReadSingle();
            _jumpUpSpeed = reader.ReadSingle();
            _jumpElapsed = reader.ReadInt32();
            _jumpShotIndex = reader.ReadInt32();
            _jumpBackwards = reader.ReadBoolean();
            _secondJumpStarted = reader.ReadBoolean();
            _secondJumpElapsed = reader.ReadInt32();
            _shotAim = new Vector2(reader.ReadSingle(), reader.ReadSingle());
            _closingForExplosive = reader.ReadBoolean();
            _approachTicks = reader.ReadInt32();
            _minesPending = reader.ReadBoolean();
            _mineTarget = new Vector2(reader.ReadSingle(), reader.ReadSingle());
            base.ReceiveExtraAI(reader);
        }
    }
}

using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.NPCs.AI;
using System;
using System.Collections.Generic;
using tsorcRevamp.Content.Items.Materials;
using tsorcRevamp.Content.Items.Materials.Souls;
using tsorcRevamp.Content.Items.Materials.Souls.DarkSoul;
using tsorcRevamp.Content.Items.Weapons.Enemy;
using tsorcRevamp.Content.Items.Weapons.Melee.Flails;
using tsorcRevamp.Content.Projectiles.Enemy.Weapons;
using EnemyCaltrop = tsorcRevamp.Content.Items.Weapons.Enemy.EnemyCaltrop;

namespace tsorcRevamp.NPCs.Puppets
{
    [AutoloadBossHead]
    public class BlackNinja : PuppetNPC, IFlailAnchor
    {
        // Smoke bomb spec: a low ballistic lob released after its 60-tick throw tell. At 11 px/t
        // against the projectile's 0.18 px/t² gravity it can solve a static target anywhere in the
        // 20-tile selection radius (including one directly overhead); the 250px-radius cloud covers small
        // target movement during its roughly 30-tick maximum flight.
        private const float SmokeBombThrowSpeed = 11f;
        private const float SmokeBombGravity = 0.18f;
        private const float SmokeBombMaximumRange = 20f * 16f;

        private struct EncounterProjectile
        {
            public int Slot;
            public int Identity;
            public int Type;
        }

        // Black Ninja's stars, caltrops, smoke cloud and flail are encounter-owned. Their normal
        // lifetimes are intentional during combat, but they must not outlive a party-wipe despawn.
        private readonly List<EncounterProjectile> _encounterProjectiles = new();

        public override string BossHeadTexture => "tsorcRevamp/NPCs/Puppets/BlackNinja_Head_Boss";

        protected override string InvaderTitle => "Black Ninja";

        protected override void RunMovementAI(float speedMult)
        {
            var globalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            globalNPC.NavSearchRadius = 65;
            globalNPC.RemembersLastKnownPos = true;

            SmartFighter4AI.Run(NPC,
                topSpeed: TopSpeed * speedMult,
                acceleration: Acceleration,
                doorBreakingDamage: 2,
                attackRange: RangedRange);
        }

        protected override int HeadArmorItemType => ItemID.NinjaHood;
        protected override int BodyArmorItemType => ItemID.NinjaShirt;
        protected override int LegsArmorItemType => ItemID.NinjaPants;

        protected override int MeleeWeaponItemType => ModContent.ItemType<EnemyDiamondCrusher>();
        protected override int RangedWeaponItemType => ModContent.ItemType<EnemyNinjaStar>();
        protected override int SecondaryRangedWeaponItemType => ModContent.ItemType<EnemySmokeBomb>();
        protected override int MagicWeaponItemType => ModContent.ItemType<EnemyCaltrop>();

        protected override int MeleeDamage => 28;
        protected override int RangedDamage => 16;
        protected override int SecondaryRangedDamage => 1;
        protected override int MagicDamage => 14;

        protected override WeaponArchetype MeleeArchetype => WeaponArchetype.Flail;

        // Reverse Halo and Ankle Reaper live in the reusable optional mace table. Black Ninja opts
        // into them explicitly; bespoke flail users such as Dread Wraith keep their current pool until
        // they choose to add the same entries and selection rules.
        private static readonly MeleeCombo[] BlackNinjaMaceCombos = BuildBlackNinjaMaceComboPool();
        protected override MeleeCombo[] MeleeComboPoolOverride => BlackNinjaMaceCombos;

        // Server-only branch memory. A completed mace move can expose Backlash Reversal while the
        // target remains behind the facing that move committed to. The selected combo index and its
        // locked facing are already carried by PuppetNPC's normal snapshot.
        private ulong _backlashReadyUntil;
        private int _backlashFollowupFacing;
        private int _backlashAttackFacing;

        private static MeleeCombo[] BuildBlackNinjaMaceComboPool()
        {
            MeleeCombo[] core = WeaponArchetypeTables.Flail;
            MeleeCombo[] levelLashes = WeaponArchetypeTables.FlailLevelLashes;
            MeleeCombo[] reactive = WeaponArchetypeTables.FlailReactiveFollowups;
            MeleeCombo[] combined = new MeleeCombo[core.Length + levelLashes.Length + reactive.Length];
            Array.Copy(core, 0, combined, 0, core.Length);
            Array.Copy(levelLashes, 0, combined, core.Length, levelLashes.Length);
            Array.Copy(reactive, 0, combined, core.Length + levelLashes.Length, reactive.Length);
            return combined;
        }

        // The flail's visual is its ball + chain projectile, so don't draw the held item icon.
        protected override bool HideHeldMeleeSprite => true;

        // The smoke bomb (secondary) renders bigger than a throwing star, and sparks a lit fuse.
        protected override float GetHeldRangedDrawScale(int itemType)
            => itemType == SecondaryRangedWeaponItemType
                ? 0.78f
                : base.GetHeldRangedDrawScale(itemType) * 0.5f;
        protected override bool HeldRangedFuseSparks(int itemType)
            => itemType == SecondaryRangedWeaponItemType;
        protected override WeaponArchetype RangedArchetype => WeaponArchetype.Throwables;

        protected override RangedStyle RangedAnimStyle => RangedStyle.Throw;
        protected override float RangedRange => 430f;
        protected override float MinRangedRange => 150f;
        protected override int RangedTelegraphTicks => 60;
        protected override int RangedAttackTicks => 8;
        protected override int RangedRecoveryTicks => 38;
        protected override int RangedCooldownAfterUse => 150;
        protected override int MaxRangedBurst => 1;
        protected override int SingleRangedBurstChance => 100;
        protected override int StandingRangedChance => 20;
        protected override Color RangedTelegraphFlashColor => Color.White;

        protected override int[][] PrimaryRangedBurstPatterns => new int[][]
        {
            new int[] { },
            new int[] { 16 },
            new int[] { 10, 10 },
            new int[] { 8, 8, 18 },
            new int[] { 6, 6, 6, 6 },
        };
        protected override int[] PrimaryRangedBurstTelegraphExtras => new int[] { 0, 4, 8, 12, 18 };
        protected override Color[] PrimaryRangedBurstFlashColors => new Color[]
        {
            Color.White,
            Color.Cyan,
            Color.LightYellow,
            Color.Yellow,
            Color.Red,
        };
        protected override int[] PrimaryRangedBurstChances => new int[] { 85, 60, 40, 20, 8 };

        protected override RangedStyle SecondaryRangedAnimStyle => RangedStyle.Throw;
        protected override float SecondaryRangedRange => SmokeBombMaximumRange;
        protected override float SecondaryRangedMinRange => 120f;
        protected override int SecondaryRangedTelegraphTicks => 60;
        protected override int SecondaryRangedAttackTicks => 10;
        protected override int SecondaryRangedRecoveryTicks => 62;
        protected override int SecondaryRangedCooldownAfterUse => 420;
        protected override int SecondaryMaxRangedBurst => 1;
        protected override int SecondaryRangedChance => 25;
        protected override int SecondaryStandingRangedChance => 65;
        protected override Color SecondaryRangedFlashColor => new Color(100, 100, 100);

        protected override float MagicRange => 330f;
        protected override float MinMagicRange => 170f;
        protected override int MagicTelegraphTicks => 60;
        protected override int MagicAttackTicks => 10;
        protected override int MagicRecoveryTicks => 50;
        protected override int MagicCooldownAfterUse => 360;
        protected override Color MagicTelegraphFlashColor => new Color(80, 80, 80);

        public Vector2 GetFlailAnchor()
            => PuppetHandPosition;

        protected override float TopSpeed => 2.85f;
        protected override float Acceleration => 0.105f;
        protected override float MeleeRange => 88f;
        protected override float StabRange => 180f;
        protected override float ComboMaxStartRange => 225f;
        protected override int MeleeComboChance => 100;
        // Flail tells are authored in on-screen ticks because the projectile itself draws the tell.
        // Forty ticks gives the new patterns exactly one harmless orbit before release.
        protected override float ComboTelegraphMultiplier => 1f;
        protected override int MinComboTelegraphTicks => 40;
        protected override int MeleeTelegraphTicks => 60;
        protected override Color MeleeTelegraphFlashColor => Color.LightYellow;
        protected override int CasualStrollChance => 6;

        protected override float PuppetJumpPower => 10.2f;
        protected override float PuppetJumpBoost => 6.4f;
        protected override bool PuppetCanDoubleJump => true;
        protected override float PuppetDoubleJumpPower => 6.2f;

        // This invader's smoke blink is a rapid reposition, not a long disappearance.
        protected override int TeleportTelegraphTicks => 30;
        protected override int TeleportDustCount => 24;
        protected override Color TeleportDustTint => new Color(70, 70, 70);
        protected override int TeleportDustTypeId => DustID.Smoke;

        private void TrackEncounterProjectile(int projectileSlot)
        {
            if (projectileSlot < 0 || projectileSlot >= Main.maxProjectiles)
            {
                return;
            }

            Projectile projectile = Main.projectile[projectileSlot];
            if (projectile.active)
            {
                _encounterProjectiles.Add(new EncounterProjectile
                {
                    Slot = projectileSlot,
                    Identity = projectile.identity,
                    Type = projectile.type,
                });
            }
        }

        private void ClearEncounterProjectiles()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            foreach (EncounterProjectile tracked in _encounterProjectiles)
            {
                Projectile projectile = Main.projectile[tracked.Slot];
                // Slot + identity + type prevents an old slot from clearing an unrelated projectile
                // after Terraria has reused that slot.
                if (projectile.active && projectile.identity == tracked.Identity && projectile.type == tracked.Type)
                {
                    projectile.Kill();
                }
            }
            _encounterProjectiles.Clear();
        }

        protected override void OnPartyWipeDespawnStarted()
        {
            base.OnPartyWipeDespawnStarted();
            PuppetSmokeBomb.StopFuse();
            ClearEncounterProjectiles();
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
            NPC.lifeMax = 1800;
            NPC.defense = 10;
            NPC.damage = 0;
            NPC.knockBackResist = 0.24f;
            NPC.aiStyle = -1;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath2;
            NPC.value = 9000f;
            NPC.boss = true;
            NPC.npcSlots = 4f;

            tsorcRevampGlobalNPC globalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            globalNPC.PoiseMax = 24f;
            globalNPC.PoiseStaggerResetsAI = true;
            globalNPC.NavGiveUpTicks = 160;
            globalNPC.CanUseRopes = true;
            globalNPC.CanDoubleJump = true;
            globalNPC.DoubleJumpPower = PuppetDoubleJumpPower;
            globalNPC.CanTeleport = true;
            globalNPC.TeleportStyle = TeleportStyle.Aggressive;
            globalNPC.TeleportVisualStyle = TeleportVisualStyle.GreySmoke;
            globalNPC.TeleportAppearanceDelay = 0;
            globalNPC.TeleportArrivalMistTime = 0;
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            npcLoot.Add(ItemDropRule.Common(ItemID.NinjaHood));
            npcLoot.Add(ItemDropRule.Common(ItemID.NinjaShirt));
            npcLoot.Add(ItemDropRule.Common(ItemID.NinjaPants));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<DiamondCrusher>()));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<global::tsorcRevamp.Content.Items.Weapons.Throwing.Caltrop>()));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<DarkSoulItem>(), 1, 300, 500));
        }

        public override void OnKill()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            Terraria.ModLoader.Config.NPCDefinition definition = new(ModContent.NPCType<BlackNinja>());
            if (!tsorcRevampWorld.NewSlain.ContainsKey(definition))
            {
                Item.NewItem(NPC.GetSource_Loot(), NPC.getRect(), ModContent.ItemType<global::tsorcRevamp.Content.Items.StaminaDroplet>());
                tsorcRevampWorld.NewSlain.Add(definition, 1);

                if (Main.netMode == NetmodeID.Server)
                {
                    NetMessage.SendData(MessageID.WorldData);
                }
            }
        }

        protected override void OnRangedBurstStarted(bool secondary)
        {
            // Lit-fuse: when the smoke bomb appears in hand, start the 3 s fuse at half volume and
            // stash its slot so the bomb cuts it the instant it detonates.
            if (secondary && !Main.dedServ)
            {
                if (SoundEngine.TryGetActiveSound(PuppetSmokeBomb.ActiveFuseSlot, out var prev))
                    prev.Stop();
                // Item172 is the longer 3 s fuse — saved for a future bigger bomb:
                // ...ActiveFuseSlot = SoundEngine.PlaySound(SoundID.Item172 with { Volume = 0.5f }, NPC.Center);
                PuppetSmokeBomb.ActiveFuseSlot =
                    SoundEngine.PlaySound(UsefulFunctions.BombFuse with { Volume = 0.5f }, NPC.Center);
            }
        }

        protected override void DoMeleeAttack()
        {
            SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.65f, PitchVariance = 0.2f }, NPC.Center);
            TryMeleeHit();
        }

        /// <summary>True while this Ninja still owns a mace head and its chain. A new launch must wait
        /// until that projectile has reeled in and removed itself; checking the NPC owner in ai[0]
        /// prevents another Ninja's flail from blocking this one.</summary>
        private bool TryGetActiveMaceBall(out Projectile activeBall)
        {
            int ballType = ModContent.ProjectileType<EnemyDiamondCrusherBall>();

            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile projectile = Main.projectile[i];
                if (projectile.active && projectile.type == ballType && (int)projectile.ai[0] == NPC.whoAmI)
                {
                    activeBall = projectile;
                    return true;
                }
            }

            activeBall = null;
            return false;
        }

        private bool HasActiveMaceBall() => TryGetActiveMaceBall(out _);

        protected override void OnMeleeComboStarted(MeleeCombo combo)
        {
            base.OnMeleeComboStarted(combo);

            if (Main.netMode == NetmodeID.MultiplayerClient
                || !EnemyFlailAttackPatterns.TryResolve(combo.Name, out EnemyFlailAttackPattern pattern)
                || HasActiveMaceBall())
            {
                return;
            }

            int facing;
            if (pattern == EnemyFlailAttackPattern.BacklashReversal)
            {
                facing = _backlashFollowupFacing == 0
                    ? (NPC.direction == 0 ? 1 : NPC.direction)
                    : _backlashFollowupFacing;
                _backlashAttackFacing = facing;
                _backlashReadyUntil = 0;
            }
            else
            {
                _backlashReadyUntil = 0;
                facing = NPC.direction == 0 ? 1 : NPC.direction;
                if (NPC.HasValidTarget)
                {
                    facing = Main.player[NPC.target].Center.X < NPC.Center.X ? -1 : 1;
                }
            }

            int damage = (int)(MeleeDamage * combo.Steps[0].DamageMult);
            Projectile projectile = Projectile.NewProjectileDirect(
                NPC.GetSource_FromThis(),
                GetFlailAnchor(),
                new Vector2(facing, 0f),
                ModContent.ProjectileType<EnemyDiamondCrusherBall>(),
                damage,
                3.5f,
                Main.myPlayer,
                NPC.whoAmI,
                (float)pattern,
                EnemyFlailAttackPatterns.ReachFor(pattern));
            projectile.timeLeft = EnemyFlailAttackPatterns.MaximumLifetime;
            projectile.netUpdate = true;
            TrackEncounterProjectile(projectile.whoAmI);
        }

        // Defensive rule for any future multi-step flail entry: a step may not create another head
        // while the projectile-owned sequence is still using the first one.
        protected override bool ShouldContinueMeleeCombo(string comboName, int nextStepIndex, Player target, bool previousStepHit)
            => !HasActiveMaceBall() && base.ShouldContinueMeleeCombo(comboName, nextStepIndex, target, previousStepHit);

        protected override bool CanSelectMeleeCombo(MeleeCombo combo, float distance, float healthFraction)
        {
            if (HasActiveMaceBall() || !base.CanSelectMeleeCombo(combo, distance, healthFraction))
                return false;

            if (combo.Name == EnemyFlailAttackPatterns.BacklashName)
                return BacklashFollowupStillValid();

            if (!EnemyFlailAttackPatterns.RequiresLevelTarget(combo.Name))
                return true;

            if (!NPC.HasValidTarget)
                return false;

            Player target = Main.player[NPC.target];
            return Math.Abs(target.Center.Y - NPC.Center.Y)
                <= EnemyFlailAttackPatterns.LevelTargetVerticalTolerance;
        }

        protected override bool TrackMeleeComboFacingDuringTelegraph(MeleeCombo combo)
            => !EnemyFlailAttackPatterns.LocksFacingDuringTell(combo.Name);

        protected override int GetMeleeComboStartFacing(MeleeCombo combo, Player target)
            => combo.Name == EnemyFlailAttackPatterns.BacklashName && _backlashAttackFacing != 0
                ? _backlashAttackFacing
                : base.GetMeleeComboStartFacing(combo, target);

        protected override int ReactiveComboIndex(float dist, ComboRangeBand band, int[] ready)
        {
            if (!BacklashFollowupStillValid())
                return base.ReactiveComboIndex(dist, band, ready);

            for (int i = 0; i < BlackNinjaMaceCombos.Length && i < ready.Length; i++)
            {
                if (ready[i] > 0 && BlackNinjaMaceCombos[i].Name == EnemyFlailAttackPatterns.BacklashName)
                    return i;
            }

            return base.ReactiveComboIndex(dist, band, ready);
        }

        protected override void OnComboStepCompleted(MeleeComboStep step)
        {
            base.OnComboStepCompleted(step);

            if (Main.netMode == NetmodeID.MultiplayerClient
                || !NPC.HasValidTarget
                || !EnemyFlailAttackPatterns.TryResolve(
                    ActiveMeleeComboName, out EnemyFlailAttackPattern completedPattern)
                || completedPattern == EnemyFlailAttackPattern.BacklashReversal)
            {
                return;
            }

            Player target = Main.player[NPC.target];
            int facing = NPC.direction == 0 ? 1 : NPC.direction;
            if (TargetQualifiesForBacklash(target, facing))
            {
                _backlashFollowupFacing = facing;
                // Covers the ordinary 60t recovery plus the immediate post-recovery selection.
                _backlashReadyUntil = Main.GameUpdateCount + 120UL;
            }
            else
            {
                _backlashReadyUntil = 0;
            }
        }

        private bool BacklashFollowupStillValid()
        {
            if (_backlashReadyUntil == 0
                || Main.GameUpdateCount > _backlashReadyUntil
                || !NPC.HasValidTarget)
            {
                return false;
            }

            return TargetQualifiesForBacklash(Main.player[NPC.target], _backlashFollowupFacing);
        }

        private bool TargetQualifiesForBacklash(Player target, int facing)
        {
            if (!target.active || target.dead || facing == 0)
                return false;

            float horizontalBehind = (target.Center.X - NPC.Center.X) * facing;
            return horizontalBehind < -24f
                && Math.Abs(target.Center.Y - NPC.Center.Y)
                    <= EnemyFlailAttackPatterns.BacklashTargetVerticalTolerance
                && Vector2.Distance(target.Center, NPC.Center)
                    <= EnemyFlailAttackPatterns.BacklashReach + 48f;
        }

        protected override void DoComboMeleeHit(MeleeComboStep step)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            // The projectile was created at combo start so its orbit IS the visible telegraph. Release
            // that same head now; never spawn a second ball for a follow-up movement.
            if (EnemyFlailAttackPatterns.TryResolve(ActiveMeleeComboName, out _))
            {
                if (TryGetActiveMaceBall(out Projectile activeBall)
                    && activeBall.ModProjectile is EnemyFlailProjectileBase flail)
                {
                    flail.ReleaseAuthoredPattern();
                }
                return;
            }

            // Mirrors Dread Wraith's one-head-at-a-time rule. This server-authoritative failsafe also
            // covers an interrupted combo or a client/server phase correction.
            if (HasActiveMaceBall())
            {
                return;
            }

            Player target = Main.player[NPC.target];
            Vector2 origin = NPC.Center + new Vector2(NPC.direction * 12f, -NPC.height * 0.2f);
            Vector2 velocity = target.Center - origin;
            if (velocity == Vector2.Zero)
            {
                velocity = new Vector2(NPC.direction, 0f);
            }
            velocity.Normalize();
            velocity *= step.Motion == ComboMotion.VerticalChop ? 9.5f : 12f;

            bool spin = step.Motion == ComboMotion.Spin;
            int damage = (int)(MeleeDamage * step.DamageMult);
            Projectile projectile = Projectile.NewProjectileDirect(
                NPC.GetSource_FromThis(),
                origin,
                spin ? Vector2.Zero : velocity,
                ModContent.ProjectileType<EnemyDiamondCrusherBall>(),
                damage,
                3.5f,
                Main.myPlayer,
                NPC.whoAmI,
                spin ? 1f : 0f);
            TrackEncounterProjectile(projectile.whoAmI);

            projectile.timeLeft = spin ? Math.Max(34, step.AttackTicks + 8) : Math.Max(44, step.AttackTicks + 31);
            SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.56f, PitchVariance = 0.22f }, NPC.Center);
        }

        protected override void DoRangedAttack()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            Player target = Main.player[NPC.target];
            Vector2 origin = NPC.Center + new Vector2(NPC.direction * 12f, -NPC.height * 0.24f);
            Vector2 aimAt = target.Center + target.velocity * 12f;

            if (IsSecondaryRangedActive)
            {
                PlayThrowSound();
                // Solve against the target's current center. The old 7.2 px/t throw could only reach
                // 18 tiles at this gravity, despite the move being eligible at 22.5 tiles, causing it
                // to fall back to a straight (and consequently short) throw.
                Vector2 smokeVelocity = UsefulFunctions.BallisticTrajectory(origin, target.Center,
                    SmokeBombThrowSpeed, SmokeBombGravity, highAngle: false, fallback: true);
                TrackEncounterProjectile(Projectile.NewProjectile(
                    NPC.GetSource_FromThis(),
                    origin,
                    smokeVelocity,
                    ModContent.ProjectileType<PuppetSmokeBomb>(),
                    SecondaryRangedDamage,
                    0f,
                    Main.myPlayer));
                return;
            }

            Vector2 toTarget = aimAt - origin;
            if (toTarget == Vector2.Zero)
            {
                toTarget = new Vector2(NPC.direction, 0f);
            }
            toTarget.Normalize();

            PlayThrowSound();
            Vector2 starVelocity = toTarget.RotatedBy(MathHelper.ToRadians(Main.rand.NextFloat(-7f, 7f))) * 10.5f;
            TrackEncounterProjectile(Projectile.NewProjectile(
                NPC.GetSource_FromThis(),
                origin,
                starVelocity,
                ModContent.ProjectileType<EnemyNinjaStarProj>(),
                RangedDamage,
                2f,
                Main.myPlayer));
        }

        protected override void DoMagicAttack()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            Player target = Main.player[NPC.target];
            PlayThrowSound();
            Vector2 origin = NPC.Center + new Vector2(NPC.direction * 12f, -NPC.height * 0.25f);
            Vector2 throwTarget = target.Center + target.velocity * 16f;
            foreach (int projectileSlot in Content.Projectiles.Enemy.Weapons.EnemyCaltrop.ThrowSpread(
                NPC.GetSource_FromThis(), origin, throwTarget, MagicDamage, 1.4f, Main.myPlayer))
            {
                TrackEncounterProjectile(projectileSlot);
            }
        }
    }
}

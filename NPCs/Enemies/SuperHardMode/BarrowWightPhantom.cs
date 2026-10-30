using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items.Armor.BarrowWightPhantomSet;
using tsorcRevamp.Content.Items.Weapons.Enemy;
using tsorcRevamp.NPCs.Bosses.SuperHardMode;
using tsorcRevamp.NPCs.Puppets;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.NPCs.Enemies.SuperHardMode
{
    /// <summary>
    /// Barrow Wight Phantom: the Witchking's phase-2 summon,
    /// rebuilt from scratch as a puppet (the legacy aiStyle-22 NPC was never spawned by anything). It wears the
    /// BarrowWightPhantomSet and carries the Barrow Wight sword.
    ///   • It always hovers and passes through walls (its own steering below — no gravity, no tile collision); the body
    ///     and legs still play the walk cycle (WalkCycleWhileAirborne).
    ///   • Ghostly: the spectral overlay (Artorias Phantom's) gives a translucent tinted body, a faint halo and a motion
    ///     trail of afterimages. It fades in over 30t and waits 60t before acting.
    ///   • Attacks: Swoop Cut and Hovering Twin (glides down to the player's height, cuts, rises away) and Phase Rush
    ///     (dims and drifts through the rock to beside a distant player, the emerge spot marked by rising dust).
    ///   • It may not start a swing while inside solid tiles: it has to emerge first.
    ///   • ai[0] = the Witchking's whoAmI (vanilla-synced; PuppetNPC never uses NPC.ai). If he dies or leaves, it
    ///     dissolves over 30t.
    /// HP comes from Systems/EnemyBalance.cs (50,000). Not a boss, no loot.
    /// </summary>
    class BarrowWightPhantom : PuppetNPC, IHumanoidMeleeHitEffects
    {
        #region Tuning constants
        // Declared Expert Mode damage before player defense (EnemyDamage.Projectile). ESTIMATE.
        public const int CutDamage = 150;

        // EnemyBarrowWightSword: 52x52, grip texels ~5-7 along the diagonal → grip (6,45); grip-to-tip (51-6)·√2 ≈ 64px.
        // Hitbox = ComboReachBase 90 × 0.7 = 63px.
        private static readonly Vector2 SwordHandleNorm = new Vector2(0.115f, 0.875f);
        private const float SwordReachBase = 90f;

        // Hover point (the Attraidies Illusion's flight tuning, lowered for the Witchking's cave): above and to one side
        // of the target, switching sides every HoverDwellTicks.
        private const float HoverAltitude = 64f;
        private const float HoverSideOffset = 120f;
        private const float HoverTopSpeed = 3.5f;
        private const float HoverAcceleration = 0.08f;
        private const int HoverDwellTicks = 180;
        // While a swing is coming it glides to the player's own height, this far to its side of them.
        private const float StrikeStandoff = 56f;
        // Within this many px of the hover height it settles to exactly zero vertical speed (combos need it).
        private const float HoverSettleBand = 4f;

        private const int ArrivalFadeTicks = 30;
        private const int ArrivalWaitTicks = 60;
        private const int ExitTicks = 30;

        // Phase Rush: from beyond 220px, 10s apart; 45t dimmed drift to beside the player, the last 20t marked.
        private const float PhaseRushMinRange = 220f;
        private const int PhaseRushTicks = 45;
        private const int PhaseRushMarkTicks = 20;
        private const int PhaseRushCooldownTicks = 10 * 60;
        private const float PhaseRushSpeed = 12f;
        private const float PhaseRushStandoff = 70f;

        // Swing clock (the Witchking's Witch Cut): in 8 / out 32 / k 6, 224° envelope, live 14.4t (< 22: rollable).
        private const int CutEaseIn = 8;
        private const int CutEaseOut = 32;
        private const float CutDecay = 6f;
        private const int CutTellTicks = 30;
        private const float CutHighPose = -1.55f;
        private const float CutLowPose = 2.36f;

        private static readonly Color GhostColor = new Color(140, 170, 255);

        private const string SwoopCutName = "Swoop Cut";
        private const string HoveringTwinName = "Hovering Twin";
        #endregion

        private int _lifeTicks;
        private int _exitTimer = -1;
        private int _hoverSide = 1;
        private int _hoverDwell;
        private int _phaseRushCooldown = PhaseRushCooldownTicks / 2;
        private bool _phaseRushing;
        private Vector2 _phaseRushTarget;

        /// <summary>0..1: ramps in over the arrival, out over the exit; Phase Rush dims it to 15%.</summary>
        private float VisibleFraction
        {
            get
            {
                float arrival = MathHelper.Clamp(_lifeTicks / (float)ArrivalFadeTicks, 0f, 1f);
                float exit = 1f;
                if (_exitTimer >= 0)
                {
                    exit = MathHelper.Clamp(_exitTimer / (float)ExitTicks, 0f, 1f);
                }
                float rush = 1f;
                if (_phaseRushing)
                {
                    rush = 0.15f;
                }
                return arrival * exit * rush;
            }
        }

        #region Puppet loadout
        protected override string InvaderTitle => "Barrow Wight Phantom";
        protected override bool AnnounceInvasion => false;
        protected override bool AnnounceInvaderDefeat => false;
        public override bool UsesPuppetDifficultyScaling => false;
        protected override int EstusChargesMax => 0;
        protected override int CasualStrollChance => 0;
        protected override Color PuppetSkinColor => Color.Black;

        protected override int HeadArmorItemType => ModContent.ItemType<BarrowWightPhantomHelm>();
        protected override int BodyArmorItemType => ModContent.ItemType<BarrowWightPhantomArmor>();
        protected override int LegsArmorItemType => ModContent.ItemType<BarrowWightPhantomShroud>();

        protected override int MeleeWeaponItemType => ModContent.ItemType<EnemyBarrowWightSword>();
        protected override int RangedWeaponItemType => -1;
        protected override int MeleeDamage => EnemyDamage.Projectile(CutDamage);
        protected override int RangedDamage => 0;
        protected override WeaponArchetype MeleeArchetype => WeaponArchetype.Broadsword;
        protected override Vector2 MeleeHandleNorm => SwordHandleNorm;
        protected override float MeleeWeaponDrawScale => 1f;
        protected override float MeleeBladeWidth => 24f;
        protected override bool ShowWeaponDuringNeutral => true;

        protected override float TopSpeed => HoverTopSpeed;
        protected override float Acceleration => HoverAcceleration;
        protected override bool WalkCycleWhileAirborne => true;

        // No attack picks while materializing, dissolving or mid-rush.
        protected override bool HoldAttackSelection => _lifeTicks < ArrivalFadeTicks + ArrivalWaitTicks || _exitTimer >= 0 || _phaseRushing;
        #endregion

        #region Spectral look
        // The finished player draw cache is redrawn tinted and translucent with a halo and a motion trail — the
        // puppet-native ghost effect (GlobalNPC.HasGhostAfterimages draws the old NPC sheet, so it can't be used).
        protected override bool HasSpectralOverlay => true;
        protected override bool SpectralKeepsArmorDyes => true;
        protected override float SpectralOverlayScale => 1f;
        protected override Color SpectralOverlayColor => GhostColor;
        protected override float SpectralCoreTintStrength => 0.3f;
        protected override float SpectralCoreOpacity => 0.75f * VisibleFraction;
        protected override Color SpectralHaloColor => new Color(90, 120, 220);
        protected override int SpectralHaloCopyCount => 6;
        protected override float SpectralHaloRadius => 3f;
        protected override float SpectralHaloOpacity => 0.22f * VisibleFraction;
        protected override float SpectralHaloScale => 1f;
        protected override bool SpectralHaloFollowsCoreOpacity => true;
        protected override float SpectralTrailOpacity => 0.35f * VisibleFraction;
        #endregion

        #region Melee
        protected override float MeleeRange => 0f;
        protected override float ComboReachBase => SwordReachBase;
        // It hovers ~136px from the target (120 to the side, 64 up), so swings start from the hover point.
        protected override float MeleeEngageRange => 150f;
        protected override float ComboMaxStartRange => 200f;
        protected override int MeleeComboChance => 100;
        protected override float ComboTelegraphMultiplier => 1f;
        protected override int MinComboTelegraphTicks => 20;
        protected override int MeleeComboInterStepLingerTicks => 3;
        protected override bool SlowDownBeforeMelee => false;
        protected override bool UseSwingEasing => true;
        protected override bool UseAuthoredComboSwingClock => true;
        protected override bool UseLogicalMeleeTelegraphs => true;
        protected override float LogicalWindupSettleFraction => 0.4f;
        protected override bool UseCompositeArmSwing => true;
        protected override bool MirrorMeleeSwingRotationByFacing => true;
        protected override bool UseCompositeArmForAdditionalPhase => Phase == AttackPhase.Custom;

        private bool InComboPose =>
            Phase == AttackPhase.MeleeComboTelegraph
            || Phase == AttackPhase.MeleeComboAttack
            || Phase == AttackPhase.MeleeComboPause;

        // Armed while angular speed >= 30% of peak: 8 + 32·ln(1/0.3)/6 = 14.4 of 40 ticks.
        private static MeleeComboStep GhostCut(ComboMotion motion, int telegraphTicks, int pauseAfter = 0)
        {
            int attackTicks = CutEaseIn + CutEaseOut;
            float liveTailTicks = CutEaseOut * MathF.Log(1f / 0.3f) / CutDecay;
            return new MeleeComboStep
            {
                Motion = motion,
                TelegraphTicks = telegraphTicks,
                AttackTicks = attackTicks,
                PostStepPause = pauseAfter,
                DamageMult = 1f,
                ReachMult = 1f,
                SwingSpeedMult = 1f,
                Ease = SwingEaseStyle.Weighted,
                EaseInTicks = CutEaseIn,
                EaseOutTicks = CutEaseOut,
                EaseOutDecay = CutDecay,
                HitWindowEnd = (CutEaseIn + liveTailTicks) / attackTicks,
                LeapHeightMult = 1f,
                LeapForwardSpeedMult = 1f,
            };
        }

        // Swoop Cut: one cut after gliding down beside the player. Hovering Twin: two, 36t between live windows.
        private static readonly MeleeCombo[] GhostCombos =
        {
            new MeleeCombo
            {
                Name = SwoopCutName,
                BaseWeight = 100,
                Preferred = ComboRangeBand.Close,
                InitialFlashColor = GhostColor,
                CooldownAfterUse = 90,
                RecoveryTicks = 40,
                MoveBrake = 0.3f,
                Steps = new[] { GhostCut(ComboMotion.OverheadArc, CutTellTicks) },
            },
            new MeleeCombo
            {
                Name = HoveringTwinName,
                BaseWeight = 70,
                Preferred = ComboRangeBand.Close,
                InitialFlashColor = GhostColor,
                CooldownAfterUse = 180,
                RecoveryTicks = 40,
                MoveBrake = 0.3f,
                Steps = new[]
                {
                    GhostCut(ComboMotion.OverheadArc, CutTellTicks, 6),
                    GhostCut(ComboMotion.UnderhandArc, 0),
                },
            },
        };

        protected override MeleeCombo[] MeleeComboPoolOverride => GhostCombos;

        // It may not start a swing while inside rock: it has to emerge first.
        protected override bool CanSelectMeleeCombo(MeleeCombo combo, float distance, float healthFraction)
        {
            return !Collision.SolidCollision(NPC.position, NPC.width, NPC.height);
        }

        protected override void ModifyMeleeArcEndpoints(ComboMotion motion, ref float startRotation, ref float endRotation)
        {
            if (motion == ComboMotion.OverheadArc)
            {
                startRotation = CutHighPose;
                endRotation = CutLowPose;
            }
            else if (motion == ComboMotion.UnderhandArc)
            {
                startRotation = CutLowPose;
                endRotation = CutHighPose;
            }
        }

        protected override void OnMeleeComboAttackTick(MeleeCombo combo, MeleeComboStep step, int elapsed, int total)
        {
            if (Main.dedServ)
            {
                return;
            }

            float along = Main.rand.NextFloat(0.3f, 1f) * 64f;
            Vector2 bladePoint = PuppetHandPosition + PuppetWeaponDirection * along;
            Dust spirit = Dust.NewDustPerfect(bladePoint, DustID.DungeonSpirit, Main.rand.NextVector2Circular(0.8f, 0.8f), 80, default, 1.1f);
            spirit.noGravity = true;
        }

        protected override void DoComboMeleeHit(MeleeComboStep step)
        {
            if (!Main.dedServ)
            {
                SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.6f, Pitch = 0.2f }, NPC.Center);
            }
            base.DoComboMeleeHit(step);
        }

        protected override void DoMeleeAttack() { }
        protected override void DoRangedAttack() { }

        // Runs on the hit player's machine: the Witchking's void set (Weight of Shadow, Withered Armor/Weapon).
        public void OnHumanoidMeleeHit(Player target)
        {
            Witchking.ApplyVoidHit(target);
        }
        #endregion

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[Type] = 1;
            NPCID.Sets.TrailCacheLength[Type] = 8;
            NPCID.Sets.TrailingMode[Type] = 1;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Poisoned] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Venom] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Confused] = true;
        }

        public override void SetDefaults()
        {
            NPC.npcSlots = 0;
            NPC.aiStyle = -1;
            NPC.width = 20;
            NPC.height = 46;
            NPC.damage = 0; // puppet convention: only the blade hurts
            NPC.defense = 40;
            NPC.lifeMax = 50000; // Systems/EnemyBalance.cs is the authority
            NPC.knockBackResist = 0f;
            NPC.HitSound = SoundID.NPCHit54;
            NPC.DeathSound = SoundID.NPCDeath6;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.lavaImmune = true;
            NPC.value = 0;
            Banner = NPC.type;
            BannerItem = ModContent.ItemType<Banners.BarrowWightPhantomBanner>();
        }

        public override bool CheckActive()
        {
            return false;
        }

        public override void AI()
        {
            _lifeTicks++;

            // A ghost: no gravity, through walls, re-asserted every tick in case anything in the base touched them.
            NPC.noGravity = true;
            NPC.noTileCollide = true;

            TickOwnerAndExit();
            if (!NPC.active)
            {
                return;
            }

            TryStartPhaseRush();

            base.AI();

            NPC.noGravity = true;
            NPC.noTileCollide = true;

            if (_phaseRushCooldown > 0)
            {
                _phaseRushCooldown--;
            }

            SpawnGhostDust();
        }

        // Dissolve (30t) once the Witchking is gone; the server then removes it without a kill (no banner credit).
        private void TickOwnerAndExit()
        {
            int ownerIndex = (int)NPC.ai[0];
            bool ownerAlive = ownerIndex >= 0 && ownerIndex < Main.maxNPCs
                && Main.npc[ownerIndex].active
                && Main.npc[ownerIndex].ModNPC is Witchking;
            if (!ownerAlive && _exitTimer < 0)
            {
                _exitTimer = ExitTicks;
            }

            if (_exitTimer < 0)
            {
                return;
            }

            _exitTimer--;
            NPC.dontTakeDamage = true;
            NPC.velocity *= 0.9f;
            if (!Main.dedServ)
            {
                Dust fade = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, DustID.DungeonSpirit, 0f, -1f, 100, default, 1.2f);
                fade.noGravity = true;
            }

            if (_exitTimer <= 0 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                NPC.active = false;
                NPC.life = 0;
                if (Main.netMode == NetmodeID.Server)
                {
                    NetMessage.SendData(MessageID.SyncNPC, number: NPC.whoAmI);
                }
            }
        }

        // Hover steering (the base movement AI is a ground navigator). Neutral: hold the hover point above and to one
        // side of the target. A swing coming: glide down to the player's height beside them. Phase Rush: drift fast
        // through the rock to the emerge point. Settles to exactly zero vertical speed near its height (combos only
        // start from velocity.Y == 0).
        protected override void RunMovementAI(float speedMult)
        {
            if (!NPC.HasValidTarget)
            {
                NPC.velocity *= 0.95f;
                return;
            }

            Player target = Main.player[NPC.target];
            float side = Math.Sign(NPC.Center.X - target.Center.X);
            if (side == 0f)
            {
                side = 1f;
            }

            _hoverDwell++;
            if (_hoverDwell >= HoverDwellTicks)
            {
                _hoverDwell = 0;
                _hoverSide = -_hoverSide;
            }

            Vector2 desired = target.Center + new Vector2(_hoverSide * HoverSideOffset, -HoverAltitude);
            float topSpeed = HoverTopSpeed * speedMult;
            if (InComboPose)
            {
                desired = target.Center + new Vector2(side * StrikeStandoff, 0f);
            }
            if (_phaseRushing)
            {
                desired = _phaseRushTarget;
                topSpeed = PhaseRushSpeed;
            }

            Vector2 toDesired = desired - NPC.Center;
            float desiredSpeed = Math.Min(topSpeed, toDesired.Length() * 0.08f);
            Vector2 desiredVelocity = toDesired.SafeNormalize(Vector2.Zero) * desiredSpeed;
            NPC.velocity = Vector2.Lerp(NPC.velocity, desiredVelocity, HoverAcceleration * 2f);

            if (Math.Abs(toDesired.Y) < HoverSettleBand && !_phaseRushing)
            {
                NPC.velocity.Y = 0f;
            }

            int facing = Math.Sign(target.Center.X - NPC.Center.X);
            if (facing != 0 && !InComboPose)
            {
                NPC.direction = facing;
                NPC.spriteDirection = facing;
            }
        }

        // Server: from a free moment beyond 220px, dim and drift through the rock to beside the player. The emerge spot
        // is chosen now and synced; for its last 20t it is marked by rising dust (the tell for the cut that follows).
        private void TryStartPhaseRush()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient || _phaseRushCooldown > 0 || HoldAttackSelection || !NPC.HasValidTarget)
            {
                return;
            }

            bool free = Phase == AttackPhase.Idle || Phase == AttackPhase.CasualStroll || Phase == AttackPhase.ClosingDistance;
            Player target = Main.player[NPC.target];
            if (!free || NPC.Distance(target.Center) < PhaseRushMinRange)
            {
                return;
            }

            float side = Main.rand.NextBool() ? 1f : -1f;
            _phaseRushTarget = target.Center + new Vector2(side * PhaseRushStandoff, -8f);
            _phaseRushCooldown = PhaseRushCooldownTicks;
            _phaseRushing = true;
            DebugAttackLabel = "Phase Rush";
            StartCustomAttack(PhaseRushTicks, MeleeWeaponItemType, swingPose: false);
            NPC.netUpdate = true;
            RequestNetworkSnapshot();
        }

        // Phase Rush is a moving set-piece: the hover steering owns velocity, so the Custom phase must not brake it.
        protected override bool SlowDownDuringCustom => !_phaseRushing;

        protected override void DoCustomTick(int ticksRemaining)
        {
            if (!_phaseRushing)
            {
                return;
            }

            RunMovementAI(1f);

            if (ticksRemaining <= PhaseRushMarkTicks && !Main.dedServ)
            {
                Vector2 mark = _phaseRushTarget + new Vector2(Main.rand.NextFloat(-14f, 14f), 24f);
                Dust rising = Dust.NewDustPerfect(mark, DustID.DungeonSpirit, new Vector2(0f, -2f), 60, default, 1.3f);
                rising.noGravity = true;
            }

            if (ticksRemaining == 1)
            {
                _phaseRushing = false;
            }
        }

        // A faint spirit mist off the robe, plus a violet outline flicker while it is inside rock (always trackable).
        private void SpawnGhostDust()
        {
            if (Main.dedServ)
            {
                return;
            }

            if (Main.rand.NextBool(4))
            {
                Dust mist = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, DustID.DungeonSpirit, 0f, 0.6f, 120, default, 0.9f * VisibleFraction + 0.1f);
                mist.noGravity = true;
            }

            bool insideRock = Collision.SolidCollision(NPC.position, NPC.width, NPC.height);
            if (insideRock)
            {
                Dust outline = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, DustID.Shadowflame, 0f, 0f, 60, default, 1.2f);
                outline.noGravity = true;
                outline.velocity *= 0.2f;
            }
            Lighting.AddLight(NPC.Center, 0.2f, 0.25f, 0.45f);
        }

        public override void HitEffect(NPC.HitInfo hit)
        {
            if (NPC.life > 0 || Main.dedServ)
            {
                return;
            }

            // A gore-free violet dissolve.
            for (int i = 0; i < 50; i++)
            {
                int dustType = i % 2 == 0 ? DustID.DungeonSpirit : DustID.Shadowflame;
                Dust dissolve = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, dustType, 0f, 0f, 60, default, 1.5f);
                dissolve.velocity = Main.rand.NextVector2Circular(4f, 4f);
                dissolve.noGravity = true;
            }
        }

        public override void SendExtraAI(System.IO.BinaryWriter writer)
        {
            base.SendExtraAI(writer);
            writer.Write(_phaseRushing);
            writer.WriteVector2(_phaseRushTarget);
            writer.Write((sbyte)_hoverSide);
        }

        public override void ReceiveExtraAI(System.IO.BinaryReader reader)
        {
            base.ReceiveExtraAI(reader);
            _phaseRushing = reader.ReadBoolean();
            _phaseRushTarget = reader.ReadVector2();
            _hoverSide = reader.ReadSByte();
        }
    }
}

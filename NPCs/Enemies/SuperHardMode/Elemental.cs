using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items.Armor.Enemy.ElementalSet;
using tsorcRevamp.Content.Items.Materials.Titanite;
using tsorcRevamp.Content.Items.Weapons.Melee.Shortswords;
using tsorcRevamp.Content.Projectiles.Enemy;
using tsorcRevamp.Content.Projectiles.Enemy.Quara;
using tsorcRevamp.Content.Projectiles.Enemy.Weapons;
using tsorcRevamp.NPCs.Enemies.Dworc;
using tsorcRevamp.NPCs.Invaders;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.NPCs.Enemies.SuperHardMode
{
    // A blue-skinned elemental brute rendered as a puppet in the Elemental armor, Phase-dyed so it shimmers in and out of sight.
    // Natural spawns and Elementals raised by an Elemental Necromancer share a server-authoritative pack hive-mind:
    // - Distinct walking speeds per Elemental so packs spread out instead of clumping, with 1-2 flankers routing behind the player.
    // - Short-range Barrow Blade swings (only drawn during swings): overhand, underhand, 2-hit under-over, and a run-and-jump leap swipe.
    // - Synced hive-mind Fireball volley (all fire together), 30t staggered Water QuaraBurstBubbles, and synced Stone Fall boulders
    //   spawning 10-20 tiles above the player in a non-stacking 12-tile spread.
    // - Solo (unsynced, max 1 per pack) Wind pull-then-push attack: 2s light pull followed by a forceful push.
    // Raised Elementals pay no souls and burst apart when their Necromancer dies.
    class Elemental : PuppetNPC
    {
        enum SpellAct : byte
        {
            None = 0,
            SyncFireball,
            SyncWaterBubble,
            SyncStoneFall,
            WindPullPush,
        }

        // Server-only hive-mind coordinator shared by a pack (same Necromancer owner, or nearby wild Elementals targeting the same player).
        sealed class PackHiveState
        {
            public uint LastUpdatedTick;
            public int HiveCooldown = 160;
            public int WindCooldown = 210;
            public int FlankRefreshTimer;
            public AttackBag<SpellAct> HiveBag;
        }

        static readonly Dictionary<long, PackHiveState> PackStates = new Dictionary<long, PackHiveState>();
        static uint LastPackPruneTick;

        // Distinct walking speeds so multiple Elementals spread out instead of stacking into one silhouette.
        static readonly float[] SpeedTiers = { 2.45f, 2.75f, 3.05f, 3.35f, 3.65f, 3.95f };
        const byte UnassignedSpeedTier = 255;
        const float EnragedSpeedMultiplier = 1.35f;

        // Flank navigation: flankers route ~11 tiles past the player before closing in from behind.
        const float FlankWaypointDistance = 176f;
        const float FlankCloseEngageDistance = 112f;
        const float WildPackRadius = 960f;

        // A raised Elemental claws out of the ground for 24t, then gets 18t of grace before it may attack or hurt anyone.
        internal const int EmergenceTicks = 24;
        internal const int EmergenceGraceTicks = 18;
        const int OwnerCheckInterval = 30;

        // Barrow Blade melee constants (38x38 diagonal sprite: pommel bottom-left, tip top-right; grip at (7,31), visual reach ~43px).
        const string OverhandSwipeName = "Overhand Swipe";
        const string UnderhandSwipeName = "Underhand Swipe";
        const string UnderOverSwipeName = "Under-Over Swipe";
        const string LeapSwipeName = "Leap Swipe";
        const float SwipeHighPose = -1.62f;
        const float SwipeLowPose = 2.22f;
        const int SwipeEaseInTicks = 6;
        const int SwipeEaseOutTicks = 28;
        const float SwipeEaseDecay = 7f;
        const int SwipeAttackTicks = SwipeEaseInTicks + SwipeEaseOutTicks;
        const float LeapSwipeStrikeRange = 84f;
        const float LeapSwipeMinRange = 110f;
        const float LeapSwipeMaxRange = 240f;
        const int BladeExpertDamage = 160;

        // Synced Fireball timings (all participating Elementals telegraph and fire on the same tick).
        const int FireballTellTicks = 36;
        const int FireballAimLockTick = 26;
        const int FireballPreviewTicks = 18;
        const int FireballTotalTicks = FireballTellTicks + 24;
        const float FireballSpeed = 9.5f;
        const float FireballMaxRange = 720f;
        const int FireballExpertDamage = 168;

        // Synced Water Bubble timings (synchronized start, 30t stagger between each Elemental's release).
        const int WaterBaseTellTicks = 42;
        const int WaterStaggerInterval = 30;
        const int WaterRecoveryTicks = 24;
        const float WaterMaxRange = 720f;
        const int WaterBubbleExpertDamage = 176;

        // Synced Stone Fall timings (arms raised up; non-stacking boulders spawn 10-20 tiles above player in a 12-tile spread).
        const int StoneRaiseTicks = 16;
        const int StoneBoulderTelegraphTicks = 35;
        const int StoneTotalTicks = StoneRaiseTicks + StoneBoulderTelegraphTicks + 24;
        const float StoneMaxRange = 760f;
        const float StoneSpreadWidth = 192f;       // 12 tiles total spread (-96..+96px around player)
        const float StoneMinHeightAbove = 160f;    // 10 tiles above player
        const float StoneMaxHeightAbove = 320f;    // 20 tiles above player
        const float StoneMinSpacing = 48f;
        const int StoneMinBoulders = 3;
        const int StoneMaxBoulders = 6;
        const int StoneBoulderExpertDamage = 196;

        // Solo Wind pull-then-push timings (unsynced, max 1 per pack).
        const int WindTellTicks = 24;
        const int WindPullDuration = 120; // 2.0 seconds of light pull
        const int WindBraceTicks = 16;
        const int WindPushWindowTicks = 16;
        const int WindRecoveryTicks = 24;
        const int WindPullEndTick = WindTellTicks + WindPullDuration;
        const int WindPushTick = WindPullEndTick + WindBraceTicks;
        const int WindTotalTicks = WindPushTick + WindPushWindowTicks + WindRecoveryTicks;
        const float WindMinRange = 120f;
        const float WindMaxRange = 520f;
        const float WindForceRadius = 560f;
        const float WindPullInnerDeadzone = 72f;
        const float WindPullAccel = 0.20f;
        const float WindPullMaxSpeed = 4.2f;
        const float WindPushSpeed = 13.5f;
        const float WindPushLift = -3.5f;

        // Pack cooldowns (server-only).
        const int HiveCooldownMin = 260;
        const int HiveCooldownMax = 340;
        const int WindCooldownTicks = 300;
        const int FlankRefreshInterval = 30;

        // ── Synced state ────────────────────────────────────────────────────────
        int emergenceTimer;
        byte speedTier = UnassignedSpeedTier;
        sbyte flankSide; // 0 = direct, -1 = flank left of player, +1 = flank right of player
        SpellAct spellState = SpellAct.None;
        int spellTimer;
        int spellStaggerDelay;
        int spellTargetIndex;
        Vector2 lockedAim = Vector2.UnitX;

        // ── Server-only ownership & pack state ──────────────────────────────────
        int ownerSlot = -1;
        int ownerToken;
        int ownerCheckTimer;
        bool isStoneFallSpawner;

        #region Puppet loadout
        public override bool UsesPuppetDifficultyScaling => false;
        protected override bool AnnounceInvasion => false;
        protected override bool AnnounceInvaderDefeat => false;
        protected override bool DespawnsOnPartyWipe => false;
        protected override string InvaderTitle => "Elemental";
        protected override int HeadArmorItemType => ModContent.ItemType<ElementalHeadpiece>();
        protected override int BodyArmorItemType => ModContent.ItemType<ElementalBreastplate>();
        protected override int LegsArmorItemType => ModContent.ItemType<ElementalGreaves>();
        protected override int HeadArmorDyeItemType => ItemID.PhaseDye;
        protected override int BodyArmorDyeItemType => ItemID.PhaseDye;
        protected override int LegsArmorDyeItemType => ItemID.PhaseDye;
        protected override Color PuppetSkinColor => new Color(0x1F, 0x5E, 0x8F); // the sheet's blue skin, for anything the armor doesn't hide

        // Barrow Blade: drawn ONLY during melee swings, hidden during pursuit and elemental casts.
        protected override int MeleeWeaponItemType => ModContent.ItemType<BarrowBlade>();
        protected override int RangedWeaponItemType => -1;
        protected override int MagicWeaponItemType => -1;
        protected override int MeleeDamage => EnemyDamage.Projectile(BladeExpertDamage);
        protected override int RangedDamage => 0;
        protected override int EstusChargesMax => 0;
        protected override int CasualStrollChance => 0;
        protected override int UnblockableFinisherBlocks => 0;
        protected override WeaponArchetype MeleeArchetype => WeaponArchetype.Broadsword;
        // Measured on BarrowBlade.png (38x38 diagonal): grip pixel (7, 31) -> tip (37, 0) = 43.1px visual reach at 1.0 scale.
        protected override Vector2 MeleeHandleNorm => new Vector2(7f / 38f, 31f / 38f);
        protected override float MeleeWeaponDrawScale => 1f;
        protected override float ComboReachBase => 64f; // 64 * 0.7 = 44.8px hitbox vs 43.1px visual
        protected override float MeleeBladeWidth => 14f;
        protected override bool ShowWeaponDuringNeutral => false;
        protected override bool WeaponSheathed => spellState != SpellAct.None;
        protected override bool HoldAttackSelection => spellState != SpellAct.None || emergenceTimer > 0;
        protected override bool SlowDownDuringCustom => true;
        protected override bool SlowDownBeforeMelee => false;
        protected override float MeleeRange => 0f;
        protected override float MeleeEngageRange => 68f;
        protected override float ComboMaxStartRange => 92f;
        protected override float RangedStartComboMaxRange => LeapSwipeMaxRange;
        protected override int MeleeComboChance => 100;
        protected override int RangedStartMeleeComboChance => 75;
        protected override bool UseSwingEasing => true;
        protected override bool UseAuthoredComboSwingClock => true;
        protected override bool UseLogicalMeleeTelegraphs => true;
        protected override bool UseCompositeArmSwing => true;
        protected override bool MirrorMeleeSwingRotationByFacing => true;
        protected override float ComboTelegraphMultiplier => 1f;
        protected override int MinComboTelegraphTicks => 20;
        protected override int MeleeComboInterStepLingerTicks => 3;
        protected override int MeleeRecoveryLingerTicks => 6;
        protected override float LogicalWindupSettleFraction => 0.25f;
        protected override bool HasSlashVFX => true;
        protected override Color SlashVFXColor => new Color(130, 175, 255);
        protected override float SlashVFXOpacity => 0.28f;
        protected override float Acceleration => 0.085f;

        bool LeapSwipeActive => ActiveMeleeComboName == LeapSwipeName
            && (Phase == AttackPhase.MeleeComboTelegraph
                || Phase == AttackPhase.MeleeComboAttack
                || Phase == AttackPhase.MeleeComboPause);

        protected override bool UseLandingTimedLeapSlam => LeapSwipeActive;
        protected override float LeapAttackTargetLeadTicks => 10f;
        protected override float LeapAttackAscentTrackingStrength => 0.10f;
        protected override float LeapLandingStandoff => -24f;
        protected override float LeapSlamCarryRotation
        {
            get
            {
                if (LeapSwipeActive)
                {
                    return SwipeHighPose;
                }

                return base.LeapSlamCarryRotation;
            }
        }

        protected override float LeapSlamImpactRotation
        {
            get
            {
                if (LeapSwipeActive)
                {
                    return SwipeLowPose;
                }

                return base.LeapSlamImpactRotation;
            }
        }

        // Distinct per-Elemental top walking speed (enrages below half HP). Null-safe for headless SwingPreview reflection.
        protected override float TopSpeed
        {
            get
            {
                int tier = speedTier < SpeedTiers.Length ? speedTier : 2;
                float speed = SpeedTiers[tier];
                if (NPC != null && NPC.lifeMax > 0 && NPC.life < NPC.lifeMax / 2)
                {
                    return speed * EnragedSpeedMultiplier;
                }

                return speed;
            }
        }

        // Poise contract: only the last 6 ticks before Fireball/Water release and Wind push burst are committed.
        protected override bool CustomAttackCommitted
        {
            get
            {
                if (spellState == SpellAct.SyncFireball)
                {
                    return spellTimer > FireballTellTicks - 6 && spellTimer <= FireballTellTicks;
                }

                if (spellState == SpellAct.SyncWaterBubble)
                {
                    int releaseTick = WaterBaseTellTicks + spellStaggerDelay;
                    return spellTimer > releaseTick - 6 && spellTimer <= releaseTick;
                }

                if (spellState == SpellAct.WindPullPush)
                {
                    return spellTimer > WindPushTick - 6 && spellTimer <= WindPushTick + 4;
                }

                return false;
            }
        }

        protected override float? FreeFrontArmFacingAngle
        {
            get
            {
                if (spellState == SpellAct.None)
                {
                    return null;
                }

                return FrontArmPose;
            }
        }

        protected override float? FreeBackArmFacingAngle
        {
            get
            {
                if (spellState == SpellAct.None)
                {
                    return null;
                }

                return BackArmPose;
            }
        }

        // Timing sheet (60 ticks = 1s). Player counterplay budget: 22t roll i-frames, next roll 30t after start, 40t post-hit immunity.
        // Poses:    -1.62 rad <-> +2.22 rad (220° envelope; ~176° live sweep; endpoints shared across overhand/underhand).
        // Tell:     22t on screen (24t for Leap Swipe), settle 0.25 via LogicalWindupSettleFraction.
        // Strike:   Weighted 6 in / 0 cruise / 28 out, k 7 -> peak ~36.7°/t; armed while speed >= 30% peak -> HitWindowEnd ~0.32 (11t live < 22t roll).
        // Open:     23t harmless follow-through + 22t recovery = 45t punish window.
        // Chain:    Under-Over Swipe has 23t tail + 12t pause = 35t after step 1 live ends (>= 30t -> no post-roll gap trap; 46t >= 40t post-hit immunity).
        // Reach:    ComboReachBase 64 * 0.7 = 44.8px hitbox vs 43.1px visual grip-to-tip.
        // Leap:     Leap Swipe (RangedStartOnly, 110-240px): 24t tell, landing-timed overhead swipe past apex within 84px, lands 24px past player, 28t recovery.
        static MeleeComboStep SwipeStep(ComboMotion motion, int telegraphTicks, int pauseAfter = 0, float damageMult = 1f)
        {
            float liveTailTicks = SwipeEaseOutTicks * MathF.Log(1f / 0.3f) / SwipeEaseDecay;
            float hitWindowEnd = MathHelper.Clamp((SwipeEaseInTicks + liveTailTicks) / SwipeAttackTicks, 0f, 1f);

            return new MeleeComboStep
            {
                Motion = motion,
                TelegraphTicks = telegraphTicks,
                AttackTicks = SwipeAttackTicks,
                PostStepPause = pauseAfter,
                DamageMult = damageMult,
                ReachMult = 1f,
                ForwardPushMult = 0.35f,
                SwingSpeedMult = 1f,
                Ease = SwingEaseStyle.Weighted,
                EaseInTicks = SwipeEaseInTicks,
                EaseOutTicks = SwipeEaseOutTicks,
                EaseOutDecay = SwipeEaseDecay,
                HitWindowEnd = hitWindowEnd,
                LeapHeightMult = 1f,
                LeapForwardSpeedMult = 1f,
            };
        }

        static readonly MeleeCombo[] BladeCombos =
        {
            new MeleeCombo
            {
                Name = OverhandSwipeName,
                BaseWeight = 100,
                Preferred = ComboRangeBand.Close,
                InitialFlashColor = new Color(130, 175, 255),
                CooldownAfterUse = 45,
                RecoveryTicks = 22,
                MoveBrake = 0.25f,
                Steps = new[]
                {
                    SwipeStep(ComboMotion.OverheadArc, 22),
                },
            },
            new MeleeCombo
            {
                Name = UnderhandSwipeName,
                BaseWeight = 100,
                Preferred = ComboRangeBand.Close,
                InitialFlashColor = new Color(130, 175, 255),
                CooldownAfterUse = 45,
                RecoveryTicks = 22,
                MoveBrake = 0.25f,
                Steps = new[]
                {
                    SwipeStep(ComboMotion.UnderhandArc, 22),
                },
            },
            new MeleeCombo
            {
                Name = UnderOverSwipeName,
                BaseWeight = 75,
                Preferred = ComboRangeBand.Close,
                InitialFlashColor = new Color(165, 195, 255),
                CooldownAfterUse = 85,
                RecoveryTicks = 26,
                MoveBrake = 0.25f,
                Steps = new[]
                {
                    SwipeStep(ComboMotion.UnderhandArc, 22, pauseAfter: 12, damageMult: 0.9f),
                    SwipeStep(ComboMotion.OverheadArc, 0, pauseAfter: 0, damageMult: 1f),
                },
            },
            new MeleeCombo
            {
                Name = LeapSwipeName,
                BaseWeight = 90,
                Preferred = ComboRangeBand.Mid,
                InitialFlashColor = new Color(145, 210, 255),
                CooldownAfterUse = 140,
                RecoveryTicks = 28,
                RangedStartOnly = true,
                MoveBrake = 0.30f,
                Steps = new[]
                {
                    new MeleeComboStep
                    {
                        Motion = ComboMotion.LeapSlam,
                        TelegraphTicks = 24,
                        AttackTicks = 85,
                        PostStepPause = 0,
                        DamageMult = 1.15f,
                        ReachMult = 1f,
                        ForwardPushMult = 0f,
                        SwingSpeedMult = 1f,
                        Ease = SwingEaseStyle.Smooth,
                        HitWindowEnd = 1f,
                        LeapStrikeRange = LeapSwipeStrikeRange,
                        LeapHeightMult = 0.72f,
                        LeapForwardSpeedMult = 1f,
                    },
                },
            },
        };

        protected override MeleeCombo[] MeleeComboPoolOverride => BladeCombos;

        protected override bool CanSelectMeleeCombo(MeleeCombo combo, float distance, float healthFraction)
        {
            if (spellState != SpellAct.None || emergenceTimer > 0)
            {
                return false;
            }

            if (combo.Name == LeapSwipeName)
            {
                bool inLeapBand = distance >= LeapSwipeMinRange && distance <= LeapSwipeMaxRange;
                bool hasSight = NPC != null && NPC.HasValidTarget
                    && Collision.CanHitLine(NPC.Center, 1, 1, Main.player[NPC.target].Center, 1, 1);
                return inLeapBand && hasSight;
            }

            return true;
        }

        protected override bool ShouldContinueMeleeCombo(string comboName, int nextStepIndex, Player target, bool previousStepHit)
        {
            if (comboName == UnderOverSwipeName)
            {
                return NPC.Distance(target.Center) <= 96f;
            }

            return true;
        }

        protected override void ModifyMeleeArcEndpoints(ComboMotion motion, ref float startRotation, ref float endRotation)
        {
            if (motion == ComboMotion.OverheadArc)
            {
                startRotation = SwipeHighPose;
                endRotation = SwipeLowPose;
            }
            else if (motion == ComboMotion.UnderhandArc)
            {
                startRotation = SwipeLowPose;
                endRotation = SwipeHighPose;
            }
        }

        protected override void DoMeleeAttack()
        {
            // All melee swings run through the authored BladeCombos table above.
        }

        protected override void DoRangedAttack()
        {
        }
        #endregion

        public override void SetStaticDefaults()
        {
            // Keeps the old sheet's slicing for the bestiary/banner; the puppet Player draws the body in game.
            Main.npcFrameCount[NPC.type] = 1;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.CursedInferno] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Ichor] = true;
        }

        public override void SetDefaults()
        {
            NPC.npcSlots = 3;
            NPC.width = 18;
            NPC.height = 40;
            AnimationType = -1;
            NPC.aiStyle = -1;
            NPC.knockBackResist = 0.2f; // overridden by the PoiseProfiles entry
            NPC.timeLeft = 750;
            NPC.damage = 50;
            NPC.defense = 42;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath6;
            NPC.lavaImmune = true;
            NPC.lifeMax = 1600;
            NPC.value = 4000;
            Banner = NPC.type;
            BannerItem = ModContent.ItemType<Banners.ElementalBanner>();

            tsorcRevampGlobalNPC globalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            globalNPC.NavSearchRadius = 45;
            globalNPC.CanTeleport = true;
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            base.SendExtraAI(writer);
            writer.Write((byte)emergenceTimer);
            writer.Write(speedTier);
            writer.Write(flankSide);
            writer.Write((byte)spellState);
            writer.Write((short)spellTimer);
            writer.Write((short)spellStaggerDelay);
            writer.Write((byte)spellTargetIndex);
            writer.WriteVector2(lockedAim);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            base.ReceiveExtraAI(reader);
            emergenceTimer = reader.ReadByte();
            speedTier = reader.ReadByte();
            flankSide = reader.ReadSByte();
            spellState = (SpellAct)reader.ReadByte();
            spellTimer = reader.ReadInt16();
            spellStaggerDelay = reader.ReadInt16();
            spellTargetIndex = reader.ReadByte();
            lockedAim = reader.ReadVector2();
        }

        /// <summary>Server, right after an Elemental Necromancer raises this Elemental. Raised Elementals pay no Dark
        /// Souls or coins (both come from NPC.value at the kill, read server-side); their item drops are untouched.
        /// Starts the synced emergence + grace window and assigns an unused walking speed tier.</summary>
        public void BindOwner(int slot, int token)
        {
            ownerSlot = slot;
            ownerToken = token;
            NPC.value = 0f;
            emergenceTimer = EmergenceTicks + EmergenceGraceTicks;
            speedTier = PickUnusedSpeedTier();
            NPC.netUpdate = true;
        }

        /// <summary>Server: raised by this Necromancer (slot + spawn token).</summary>
        public bool BelongsTo(int slot, int token)
        {
            return ownerSlot == slot && ownerToken == token;
        }

        public override bool CanHitPlayer(Player target, ref int cooldownSlot)
        {
            // Still clawing out of the ground (or in its grace): the body is harmless.
            return emergenceTimer <= 0;
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo hurtInfo)
        {
            // Runs on the victim's machine, which owns its own buffs.
            if (Main.rand.NextBool(2))
            {
                target.AddBuff(BuffID.Battle, 30 * 60, false);
                target.AddBuff(BuffID.Weak, 40 * 60, false);
            }
        }

        public override void OnStagger(NPC npc)
        {
            base.OnStagger(npc);

            if (spellState == SpellAct.None)
            {
                return;
            }

            if (!Main.dedServ)
            {
                SoundEngine.PlaySound(SoundID.Item10 with { Volume = 0.55f, Pitch = -0.3f }, NPC.Center);
                for (int i = 0; i < 12; i++)
                {
                    Vector2 velocity = Main.rand.NextVector2Circular(3.2f, 3.2f);
                    Dust fizzle = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, DustID.PurpleTorch, velocity.X, velocity.Y, 90, default, 1.2f);
                    fizzle.noGravity = true;
                }
            }

            spellState = SpellAct.None;
            spellTimer = 0;
            spellStaggerDelay = 0;
            isStoneFallSpawner = false;
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                NPC.netUpdate = true;
            }
        }

        Vector2 FrontPalm => PuppetFrontHandPositionAt(FrontArmPose);
        Vector2 BackPalm => PuppetBackHandPositionAt(BackArmPose);
        Player SpellTarget => Main.player[Math.Clamp(spellTargetIndex, 0, Main.maxPlayers - 1)];

        protected override void RunMovementAI(float speedMult)
        {
            if (spellState != SpellAct.None)
            {
                NPC.velocity.X *= 0.85f;
                return;
            }

            // Flankers navigate to the player's far side first so packs pincer the player for melee and hive spells.
            Vector2? waypoint = null;
            if (flankSide != 0 && NPC.HasValidTarget)
            {
                Player target = Main.player[NPC.target];
                if (target.active && !target.dead)
                {
                    float deltaX = NPC.Center.X - target.Center.X;
                    bool alreadyOnFlankSide = deltaX * flankSide > 24f;
                    float distanceToTarget = NPC.Distance(target.Center);
                    bool shouldPursueWaypoint = !alreadyOnFlankSide || distanceToTarget > FlankCloseEngageDistance;
                    if (shouldPursueWaypoint)
                    {
                        float desiredX = target.Center.X + flankSide * FlankWaypointDistance;
                        if (PuppetGroundDustWave.TryFindGroundY(desiredX, target.Bottom.Y, out float groundY))
                        {
                            waypoint = new Vector2(desiredX, groundY - NPC.height * 0.5f);
                        }
                        else
                        {
                            waypoint = new Vector2(desiredX, target.Center.Y);
                        }
                    }
                }
            }

            tsorcRevampGlobalNPC globalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            globalNPC.RemembersLastKnownPos = true;
            SmartFighter4AI.Run(
                NPC,
                topSpeed: TopSpeed * speedMult,
                acceleration: Acceleration,
                doorBreakingDamage: 4,
                attackRange: RangedRange,
                movementWaypoint: waypoint);
        }

        public override void AI()
        {
            bool authority = Main.netMode != NetmodeID.MultiplayerClient;

            if (authority && speedTier == UnassignedSpeedTier)
            {
                speedTier = PickUnusedSpeedTier();
                NPC.netUpdate = true;
            }

            // Owner gone (dead, despawned, or its slot reused): a real kill, never life = 0 by hand (that skips OnKill
            // and loot). Its HitEffect plays the purple burst on every client.
            if (authority && ownerSlot >= 0)
            {
                ownerCheckTimer++;
                if (ownerCheckTimer >= OwnerCheckInterval)
                {
                    ownerCheckTimer = 0;
                    bool ownerAlive = false;
                    if (ownerSlot < Main.maxNPCs)
                    {
                        NPC owner = Main.npc[ownerSlot];
                        bool isNecromancer = owner.active && owner.life > 0 && owner.type == ModContent.NPCType<ElementalNecromancer>();
                        ownerAlive = isNecromancer && owner.GetGlobalNPC<DworcSpawnIdentity>().Token == ownerToken;
                    }

                    if (!ownerAlive)
                    {
                        NPC.StrikeInstantKill();
                        return;
                    }
                }
            }

            if (emergenceTimer > 0)
            {
                emergenceTimer--;
                if (!Main.dedServ && emergenceTimer > EmergenceGraceTicks && Main.rand.NextBool(2))
                {
                    // Still climbing out: grave dust kicked up around its feet.
                    Dust soil = Dust.NewDustDirect(new Vector2(NPC.position.X - 6f, NPC.Bottom.Y - 8f), NPC.width + 12, 8, DustID.PurpleTorch, 0f, -1.5f, 100, default, 1.1f);
                    soil.noGravity = true;
                }
            }

            if (authority && emergenceTimer <= 0)
            {
                TickPackHiveCoordinator();
            }

            // Keep the Custom phase alive while any elemental spell is active and face the locked target.
            if (spellState != SpellAct.None)
            {
                PhaseTimer = 2;
                NPC.target = spellTargetIndex;
                FaceSpellTarget();
            }

            tsorcRevampGlobalNPC globalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            globalNPC.CanTeleport = spellState == SpellAct.None;

            base.AI();

            if (spellState != SpellAct.None)
            {
                FaceSpellTarget();
            }

            if (spellState != SpellAct.None)
            {
                DebugAttackLabel = spellState + "/" + spellTimer;
            }

            if (!Main.dedServ)
            {
                Lighting.AddLight(NPC.Center, 0.12f, 0.08f, 0.28f);
            }
        }

        #region Pack Hive-Mind Coordinator (server-only)
        long GetPackKey()
        {
            if (ownerSlot >= 0)
            {
                return ((long)ownerToken << 16) | (uint)ownerSlot;
            }

            // Natural Elementals group by target player so wild groups within WildPackRadius act as a hive-mind too.
            int target = NPC.HasValidTarget ? NPC.target : 0;
            return -(target + 1L);
        }

        bool IsSamePack(Elemental other)
        {
            if (ownerSlot >= 0 || other.ownerSlot >= 0)
            {
                return ownerSlot == other.ownerSlot && ownerToken == other.ownerToken;
            }

            if (NPC.Distance(other.NPC.Center) > WildPackRadius)
            {
                return false;
            }

            return !NPC.HasValidTarget || !other.NPC.HasValidTarget || NPC.target == other.NPC.target;
        }

        byte PickUnusedSpeedTier()
        {
            bool[] used = new bool[SpeedTiers.Length];
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC candidate = Main.npc[i];
                if (i == NPC.whoAmI || !candidate.active || candidate.ModNPC is not Elemental other)
                {
                    continue;
                }

                if (IsSamePack(other) && other.speedTier < SpeedTiers.Length)
                {
                    used[other.speedTier] = true;
                }
            }

            for (int tier = 0; tier < SpeedTiers.Length; tier++)
            {
                if (!used[tier])
                {
                    return (byte)tier;
                }
            }

            return (byte)Main.rand.Next(SpeedTiers.Length);
        }

        void TickPackHiveCoordinator()
        {
            // Prune stale pack entries periodically and reset cleanly across world reloads.
            if (Main.GameUpdateCount < LastPackPruneTick)
            {
                PackStates.Clear();
                LastPackPruneTick = Main.GameUpdateCount;
            }
            else if (Main.GameUpdateCount - LastPackPruneTick >= 300)
            {
                LastPackPruneTick = Main.GameUpdateCount;
                List<long> staleKeys = new List<long>();
                foreach (KeyValuePair<long, PackHiveState> pair in PackStates)
                {
                    if (Main.GameUpdateCount - pair.Value.LastUpdatedTick > 600)
                    {
                        staleKeys.Add(pair.Key);
                    }
                }

                foreach (long staleKey in staleKeys)
                {
                    PackStates.Remove(staleKey);
                }
            }

            NPC.TargetClosest(false);
            if (!NPC.HasValidTarget)
            {
                return;
            }

            long packKey = GetPackKey();
            if (!PackStates.TryGetValue(packKey, out PackHiveState pack))
            {
                pack = new PackHiveState();
                PackStates[packKey] = pack;
            }

            // Ensure the pack coordinator runs exactly once per game tick regardless of pack size.
            if (pack.LastUpdatedTick == Main.GameUpdateCount)
            {
                return;
            }

            pack.LastUpdatedTick = Main.GameUpdateCount;

            List<Elemental> members = new List<Elemental>();
            bool anyCastingSpell = false;
            bool anyCastingWind = false;

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC candidate = Main.npc[i];
                if (!candidate.active || candidate.life <= 0 || candidate.ModNPC is not Elemental member)
                {
                    continue;
                }

                if (!IsSamePack(member))
                {
                    continue;
                }

                members.Add(member);
                if (member.spellState != SpellAct.None)
                {
                    anyCastingSpell = true;
                }

                if (member.spellState == SpellAct.WindPullPush)
                {
                    anyCastingWind = true;
                }
            }

            if (members.Count == 0)
            {
                return;
            }

            Player packTarget = Main.player[NPC.target];
            if (!packTarget.active || packTarget.dead)
            {
                return;
            }

            // Refresh flanking roles every 30t so 1-2 Elementals consistently pressure from the player's flank.
            pack.FlankRefreshTimer++;
            if (pack.FlankRefreshTimer >= FlankRefreshInterval)
            {
                pack.FlankRefreshTimer = 0;
                AssignPackFlankers(members, packTarget);
            }

            if (pack.HiveCooldown > 0)
            {
                pack.HiveCooldown--;
            }

            if (pack.WindCooldown > 0)
            {
                pack.WindCooldown--;
            }

            if (anyCastingSpell)
            {
                return;
            }

            // Gather members free to enter a spell (idle/closing, grounded, not emerging, not staggered).
            List<Elemental> readyMembers = new List<Elemental>();
            for (int i = 0; i < members.Count; i++)
            {
                Elemental member = members[i];
                if (member.IsReadyForSpell())
                {
                    readyMembers.Add(member);
                }
            }

            if (readyMembers.Count == 0)
            {
                return;
            }

            // Try synced hive-mind attack first (Fireball, staggered Water Bubble, or Stone Fall).
            if (pack.HiveCooldown <= 0)
            {
                pack.HiveBag ??= new AttackBag<SpellAct>(RefillHiveBag);
                if (pack.HiveBag.TryDraw(spell => IsHiveSpellEligible(spell, readyMembers, packTarget), out SpellAct chosen))
                {
                    StartHiveSpell(chosen, readyMembers, packTarget);
                    pack.HiveCooldown = Main.rand.Next(HiveCooldownMin, HiveCooldownMax + 1);
                    return;
                }

                pack.HiveCooldown = 30;
            }

            // Solo Wind pull-then-push: unsynced, max 1 Elemental in the pack at a time.
            if (!anyCastingWind && pack.WindCooldown <= 0)
            {
                Elemental windCaster = PickWindCaster(readyMembers, packTarget);
                if (windCaster != null)
                {
                    windCaster.BeginSpell(SpellAct.WindPullPush, NPC.target, 0, false);
                    pack.WindCooldown = WindCooldownTicks;
                }
                else
                {
                    pack.WindCooldown = 30;
                }
            }
        }

        static void RefillHiveBag(List<SpellAct> cards)
        {
            cards.Add(SpellAct.SyncFireball);
            cards.Add(SpellAct.SyncWaterBubble);
            cards.Add(SpellAct.SyncStoneFall);
        }

        static void AssignPackFlankers(List<Elemental> members, Player target)
        {
            int desiredFlankers = 0;
            if (members.Count >= 4)
            {
                desiredFlankers = 2;
            }
            else if (members.Count >= 2)
            {
                desiredFlankers = 1;
            }

            // Find which side the pack currently clusters on so the first flanker routes to the opposite side.
            float averageSide = 0f;
            for (int i = 0; i < members.Count; i++)
            {
                averageSide += members[i].NPC.Center.X - target.Center.X;
            }

            sbyte primaryFlank = 1;
            if (averageSide > 0f)
            {
                primaryFlank = -1;
            }

            // Assign the fastest tiers to flank so they reach the far side quickly while slower tiers push head-on.
            List<Elemental> sortedBySpeed = new List<Elemental>(members);
            sortedBySpeed.Sort((first, second) => second.speedTier.CompareTo(first.speedTier));

            for (int i = 0; i < sortedBySpeed.Count; i++)
            {
                sbyte assignedSide = 0;
                if (i == 0 && desiredFlankers >= 1)
                {
                    assignedSide = primaryFlank;
                }
                else if (i == 1 && desiredFlankers >= 2)
                {
                    assignedSide = (sbyte)(-primaryFlank);
                }

                if (sortedBySpeed[i].flankSide != assignedSide)
                {
                    sortedBySpeed[i].flankSide = assignedSide;
                    sortedBySpeed[i].NPC.netUpdate = true;
                }
            }
        }

        bool IsReadyForSpell()
        {
            if (emergenceTimer > 0 || spellState != SpellAct.None)
            {
                return false;
            }

            bool phaseFree = Phase == AttackPhase.Idle || Phase == AttackPhase.ClosingDistance;
            tsorcRevampGlobalNPC globalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            bool notBusy = globalNPC.StaggerTimer <= 0 && NPC.velocity.Y == 0f
                && globalNPC.TeleportCountdown == 0 && globalNPC.TeleportAppearanceTimer == 0;
            return phaseFree && notBusy;
        }

        static bool IsHiveSpellEligible(SpellAct spell, List<Elemental> readyMembers, Player target)
        {
            for (int i = 0; i < readyMembers.Count; i++)
            {
                Elemental member = readyMembers[i];
                float distance = member.NPC.Distance(target.Center);
                bool lineOfSight = Collision.CanHitLine(member.NPC.Center, 1, 1, target.Center, 1, 1);

                if (spell == SpellAct.SyncFireball && distance <= FireballMaxRange && lineOfSight)
                {
                    return true;
                }

                if (spell == SpellAct.SyncWaterBubble && distance <= WaterMaxRange && lineOfSight)
                {
                    return true;
                }

                if (spell == SpellAct.SyncStoneFall && distance <= StoneMaxRange)
                {
                    return true;
                }
            }

            return false;
        }

        static void StartHiveSpell(SpellAct spell, List<Elemental> readyMembers, Player target)
        {
            List<Elemental> participants = new List<Elemental>();
            for (int i = 0; i < readyMembers.Count; i++)
            {
                Elemental member = readyMembers[i];
                float distance = member.NPC.Distance(target.Center);
                bool lineOfSight = Collision.CanHitLine(member.NPC.Center, 1, 1, target.Center, 1, 1);

                if (spell == SpellAct.SyncFireball && distance <= FireballMaxRange && lineOfSight)
                {
                    participants.Add(member);
                }
                else if (spell == SpellAct.SyncWaterBubble && distance <= WaterMaxRange && lineOfSight)
                {
                    participants.Add(member);
                }
                else if (spell == SpellAct.SyncStoneFall && distance <= StoneMaxRange)
                {
                    participants.Add(member);
                }
            }

            if (participants.Count == 0)
            {
                return;
            }

            if (spell == SpellAct.SyncWaterBubble)
            {
                // Sort nearest-to-farthest so the 30-tick staggered water wave rolls cleanly across the arena.
                participants.Sort((first, second) => first.NPC.Distance(target.Center).CompareTo(second.NPC.Distance(target.Center)));
            }

            for (int i = 0; i < participants.Count; i++)
            {
                int staggerDelay = 0;
                if (spell == SpellAct.SyncWaterBubble)
                {
                    staggerDelay = i * WaterStaggerInterval;
                }

                bool stoneSpawner = spell == SpellAct.SyncStoneFall && i == 0;
                participants[i].BeginSpell(spell, target.whoAmI, staggerDelay, stoneSpawner);
            }
        }

        static Elemental PickWindCaster(List<Elemental> readyMembers, Player target)
        {
            for (int i = 0; i < readyMembers.Count; i++)
            {
                Elemental candidate = readyMembers[i];
                float distance = candidate.NPC.Distance(target.Center);
                bool inBand = distance >= WindMinRange && distance <= WindMaxRange;
                bool hasSight = Collision.CanHitLine(candidate.NPC.Center, 1, 1, target.Center, 1, 1);
                if (inBand && hasSight)
                {
                    return candidate;
                }
            }

            return null;
        }

        void BeginSpell(SpellAct action, int targetPlayerIndex, int staggerDelayTicks, bool spawnStoneFallBoulders)
        {
            spellState = action;
            spellTimer = 0;
            spellStaggerDelay = staggerDelayTicks;
            spellTargetIndex = targetPlayerIndex;
            isStoneFallSpawner = spawnStoneFallBoulders;

            Player target = Main.player[Math.Clamp(targetPlayerIndex, 0, Main.maxPlayers - 1)];
            lockedAim = (target.Center - NPC.Center).SafeNormalize(new Vector2(NPC.direction, 0f));

            StartCustomAttack(2);
            NPC.netUpdate = true;
        }

        void EndSpell()
        {
            spellState = SpellAct.None;
            spellTimer = 0;
            spellStaggerDelay = 0;
            isStoneFallSpawner = false;
            EnterPhase(AttackPhase.Idle, 0);
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                NPC.netUpdate = true;
            }
        }
        #endregion

        #region Spell Arm Poses
        int SpellTotalTicks
        {
            get
            {
                switch (spellState)
                {
                    case SpellAct.SyncFireball:
                        return FireballTotalTicks;
                    case SpellAct.SyncWaterBubble:
                        return WaterBaseTellTicks + spellStaggerDelay + WaterRecoveryTicks;
                    case SpellAct.SyncStoneFall:
                        return StoneTotalTicks;
                    case SpellAct.WindPullPush:
                        return WindTotalTicks;
                }

                return 1;
            }
        }

        float SpellPoseBlend
        {
            get
            {
                float blendIn = MathHelper.Clamp(spellTimer / 10f, 0f, 1f);
                float blendOut = MathHelper.Clamp((SpellTotalTicks - spellTimer) / 14f, 0f, 1f);
                return Math.Min(blendIn, blendOut);
            }
        }

        float FacingAngleFor(Vector2 worldDirection)
        {
            float forward = worldDirection.X * NPC.direction;
            float angle = (float)Math.Atan2(worldDirection.Y, Math.Max(forward, 0.05f));
            return MathHelper.Clamp(angle, -1.6f, 1.4f);
        }

        float FrontArmPose
        {
            get
            {
                float pose = MathHelper.PiOver2;
                switch (spellState)
                {
                    case SpellAct.SyncFireball:
                        {
                            Vector2 shoulder = NPC.Center + new Vector2(0f, -8f);
                            Vector2 aim = (SpellTarget.Center - shoulder).SafeNormalize(new Vector2(NPC.direction, 0f));
                            if (spellTimer >= FireballAimLockTick)
                            {
                                aim = lockedAim;
                            }

                            pose = FacingAngleFor(aim);
                            break;
                        }

                    case SpellAct.SyncWaterBubble:
                        pose = -0.45f;
                        break;

                    case SpellAct.SyncStoneFall:
                        // Both arms raised high up into the air to call boulders from above.
                        pose = -1.48f;
                        break;

                    case SpellAct.WindPullPush:
                        {
                            if (spellTimer <= WindPullEndTick)
                            {
                                // Extended forward with a subtle inward beckoning pulse during the 2s pull.
                                float sway = (float)Math.Sin(spellTimer * 0.14f) * 0.14f;
                                pose = -0.10f + sway;
                            }
                            else if (spellTimer < WindPushTick)
                            {
                                // Draw back before the forceful push burst.
                                float brace = (spellTimer - WindPullEndTick) / (float)WindBraceTicks;
                                pose = MathHelper.SmoothStep(-0.10f, 1.05f, brace);
                            }
                            else
                            {
                                // Thrust straight forward on the push.
                                float thrust = MathHelper.Clamp((spellTimer - WindPushTick) / 6f, 0f, 1f);
                                pose = MathHelper.SmoothStep(1.05f, -0.05f, thrust);
                            }

                            break;
                        }
                }

                return MathHelper.SmoothStep(MathHelper.PiOver2, pose, SpellPoseBlend);
            }
        }

        float BackArmPose
        {
            get
            {
                float pose = MathHelper.PiOver2;
                switch (spellState)
                {
                    case SpellAct.SyncFireball:
                        pose = 0.40f;
                        break;

                    case SpellAct.SyncWaterBubble:
                        pose = 0.25f;
                        break;

                    case SpellAct.SyncStoneFall:
                        pose = -1.36f;
                        break;

                    case SpellAct.WindPullPush:
                        {
                            if (spellTimer <= WindPullEndTick)
                            {
                                pose = 0.35f;
                            }
                            else if (spellTimer < WindPushTick)
                            {
                                float brace = (spellTimer - WindPullEndTick) / (float)WindBraceTicks;
                                pose = MathHelper.SmoothStep(0.35f, 0.95f, brace);
                            }
                            else
                            {
                                float thrust = MathHelper.Clamp((spellTimer - WindPushTick) / 6f, 0f, 1f);
                                pose = MathHelper.SmoothStep(0.95f, -0.15f, thrust);
                            }

                            break;
                        }
                }

                return MathHelper.SmoothStep(MathHelper.PiOver2, pose, SpellPoseBlend);
            }
        }

        void FaceSpellTarget()
        {
            int direction = 1;
            if (SpellTarget.Center.X < NPC.Center.X)
            {
                direction = -1;
            }

            NPC.direction = direction;
            NPC.spriteDirection = direction;
        }
        #endregion

        #region Spell Execution
        protected override void DoCustomTick(int ticksRemaining)
        {
            if (spellState == SpellAct.None)
            {
                return;
            }

            spellTimer++;
            bool authority = Main.netMode != NetmodeID.MultiplayerClient;
            if (authority && (!SpellTarget.active || SpellTarget.dead))
            {
                EndSpell();
                return;
            }

            switch (spellState)
            {
                case SpellAct.SyncFireball:
                    RunSyncFireball(authority);
                    break;
                case SpellAct.SyncWaterBubble:
                    RunSyncWaterBubble(authority);
                    break;
                case SpellAct.SyncStoneFall:
                    RunSyncStoneFall(authority);
                    break;
                case SpellAct.WindPullPush:
                    RunWindPullPush();
                    break;
            }

            if (authority && spellState != SpellAct.None && spellTimer >= SpellTotalTicks)
            {
                EndSpell();
            }
        }

        void RunSyncFireball(bool authority)
        {
            Vector2 palm = FrontPalm;

            if (authority && spellTimer == FireballAimLockTick)
            {
                lockedAim = (SpellTarget.Center - palm).SafeNormalize(new Vector2(NPC.direction, 0f));
                NPC.netUpdate = true;
            }

            if (!Main.dedServ && spellTimer <= FireballTellTicks)
            {
                if (spellTimer == 1)
                {
                    SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.55f, Pitch = -0.25f }, palm);
                }

                // Converging fire motes forming the fireball at the palm (max scale 1.2 * 1.25 = 1.50).
                float progress = spellTimer / (float)FireballTellTicks;
                int gatherCount = 1 + (int)(progress * 2f);
                for (int i = 0; i < gatherCount; i++)
                {
                    float angle = Main.rand.NextFloat(MathHelper.TwoPi);
                    Vector2 spawnPos = palm + angle.ToRotationVector2() * Main.rand.NextFloat(8f, 24f);
                    int dustType = DustID.Torch;
                    if (i == 2)
                    {
                        dustType = DustID.OrangeTorch;
                    }

                    Dust gather = Dust.NewDustPerfect(spawnPos, dustType, (palm - spawnPos) * 0.16f, 70, default, Main.rand.NextFloat(0.85f, 1.25f));
                    gather.noGravity = true;
                }

                Lighting.AddLight(palm, 0.7f * progress, 0.32f * progress, 0.08f * progress);
            }

            if (spellTimer != FireballTellTicks)
            {
                return;
            }

            if (authority)
            {
                Projectile.NewProjectile(
                    NPC.GetSource_FromThis(),
                    palm,
                    lockedAim * FireballSpeed,
                    ModContent.ProjectileType<ElementalFireball>(),
                    EnemyDamage.Projectile(FireballExpertDamage),
                    2f,
                    Main.myPlayer);
            }

            if (!Main.dedServ)
            {
                SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.7f, Pitch = 0.15f }, palm);
                for (int i = 0; i < 10; i++)
                {
                    Vector2 sparkVel = lockedAim.RotatedBy(Main.rand.NextFloat(-0.55f, 0.55f)) * Main.rand.NextFloat(2.5f, 6f);
                    Dust muzzle = Dust.NewDustPerfect(palm, DustID.Torch, sparkVel, 50, default, Main.rand.NextFloat(0.85f, 1.3f));
                    muzzle.noGravity = true;
                }
            }
        }

        void RunSyncWaterBubble(bool authority)
        {
            int releaseTick = WaterBaseTellTicks + spellStaggerDelay;
            int activeTelegraphStart = Math.Max(1, releaseTick - WaterBaseTellTicks);
            Vector2 palm = FrontPalm;

            if (!Main.dedServ && spellTimer <= releaseTick)
            {
                if (spellTimer == activeTelegraphStart)
                {
                    SoundEngine.PlaySound(SoundID.Item87 with { Volume = 0.55f, Pitch = -0.35f }, palm);
                }

                // Light idle water shimmer while waiting for our staggered turn, then inflating bubble ring over the last 42t.
                if (spellTimer < activeTelegraphStart)
                {
                    if (Main.rand.NextBool(3))
                    {
                        Dust waitDrop = Dust.NewDustPerfect(palm + Main.rand.NextVector2Circular(8f, 8f), DustID.Water, new Vector2(0f, -0.7f), 80, default, 1f);
                        waitDrop.noGravity = true;
                    }
                }
                else
                {
                    float progress = (spellTimer - activeTelegraphStart) / (float)WaterBaseTellTicks;
                    int ringCount = 1 + (int)(progress * 2f);
                    for (int i = 0; i < ringCount; i++)
                    {
                        float angle = Main.rand.NextFloat(MathHelper.TwoPi);
                        Vector2 ringPos = palm + angle.ToRotationVector2() * (6f + progress * 14f);
                        Dust bubble = Dust.NewDustPerfect(ringPos, DustID.Water, (palm - ringPos) * 0.08f, 70, default, Main.rand.NextFloat(0.95f, 1.25f));
                        bubble.noGravity = true;
                    }

                    Lighting.AddLight(palm, 0.14f * progress, 0.34f * progress, 0.58f * progress);
                }
            }

            if (spellTimer != releaseTick)
            {
                return;
            }

            if (authority)
            {
                Vector2 launchDir = (SpellTarget.Center - palm).SafeNormalize(new Vector2(NPC.direction, 0f));
                Projectile.NewProjectile(
                    NPC.GetSource_FromThis(),
                    palm,
                    launchDir * 2.4f,
                    ModContent.ProjectileType<QuaraBurstBubble>(),
                    EnemyDamage.Projectile(WaterBubbleExpertDamage),
                    2f,
                    Main.myPlayer,
                    spellTargetIndex);
            }

            if (!Main.dedServ)
            {
                SoundEngine.PlaySound(SoundID.Item87 with { Volume = 0.65f, Pitch = 0.2f }, palm);
            }
        }

        void RunSyncStoneFall(bool authority)
        {
            if (!Main.dedServ && spellTimer <= StoneRaiseTicks + StoneBoulderTelegraphTicks)
            {
                if (spellTimer == 1)
                {
                    SoundEngine.PlaySound(SoundID.Item69 with { Volume = 0.55f, Pitch = -0.35f }, NPC.Center);
                }

                // Rising clay and dirt motes around the Elementals' raised arms and feet.
                if (Main.rand.NextBool(2))
                {
                    int dustType = DustID.Clay;
                    if (Main.rand.NextBool())
                    {
                        dustType = DustID.Dirt;
                    }

                    Dust handDust = Dust.NewDustPerfect(FrontPalm + Main.rand.NextVector2Circular(6f, 6f), dustType, new Vector2(0f, -2.4f), 60, default, Main.rand.NextFloat(0.9f, 1.25f));
                    handDust.noGravity = true;

                    Dust offDust = Dust.NewDustPerfect(BackPalm + Main.rand.NextVector2Circular(6f, 6f), DustID.Stone, new Vector2(0f, -2f), 60, default, Main.rand.NextFloat(0.85f, 1.2f));
                    offDust.noGravity = true;
                }
            }

            if (!authority || !isStoneFallSpawner || spellTimer != StoneRaiseTicks)
            {
                return;
            }

            // Count active Stone Fall casters to size the boulder volley (3..6 boulders) in a non-stacking 12-tile spread.
            int casterCount = 0;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC candidate = Main.npc[i];
                if (candidate.active && candidate.ModNPC is Elemental member && IsSamePack(member) && member.spellState == SpellAct.SyncStoneFall)
                {
                    casterCount++;
                }
            }

            int boulderCount = Math.Clamp(casterCount, StoneMinBoulders, StoneMaxBoulders);
            SpawnSpreadBoulders(SpellTarget, boulderCount);
        }

        void SpawnSpreadBoulders(Player target, int count)
        {
            // Divide the 12-tile spread (192px) into evenly spaced horizontal slots covering behind, above, and in front
            // of the player, jittering inside each slot so boulders never stack on top of each other.
            float halfSpread = StoneSpreadWidth * 0.5f;
            float slotWidth = StoneSpreadWidth / count;
            List<Vector2> chosenPoints = new List<Vector2>(count);

            for (int i = 0; i < count; i++)
            {
                float slotMinX = target.Center.X - halfSpread + i * slotWidth;
                float slotCenterX = slotMinX + slotWidth * 0.5f;
                float jitterMax = Math.Max(0f, slotWidth * 0.35f);

                Vector2 chosen = new Vector2(slotCenterX, target.Center.Y - Main.rand.NextFloat(StoneMinHeightAbove, StoneMaxHeightAbove));

                for (int attempt = 0; attempt < 10; attempt++)
                {
                    float candidateX = slotCenterX + Main.rand.NextFloat(-jitterMax, jitterMax);
                    float candidateY = target.Center.Y - Main.rand.NextFloat(StoneMinHeightAbove, StoneMaxHeightAbove);
                    Vector2 candidate = new Vector2(candidateX, candidateY);

                    bool clearOfTiles = !Collision.SolidCollision(candidate - new Vector2(14f, 14f), 28, 28);
                    bool spacedFromOthers = true;
                    for (int j = 0; j < chosenPoints.Count; j++)
                    {
                        if (Vector2.Distance(chosenPoints[j], candidate) < StoneMinSpacing)
                        {
                            spacedFromOthers = false;
                            break;
                        }
                    }

                    if (clearOfTiles && spacedFromOthers)
                    {
                        chosen = candidate;
                        break;
                    }
                }

                chosenPoints.Add(chosen);

                // Roll direction: roll inward toward the player (or random for directly overhead) so boulders sweep the ground.
                float rollDirection = 1f;
                if (chosen.X > target.Center.X + 16f)
                {
                    rollDirection = -1f;
                }
                else if (Math.Abs(chosen.X - target.Center.X) <= 16f && Main.rand.NextBool())
                {
                    rollDirection = -1f;
                }

                Projectile.NewProjectile(
                    NPC.GetSource_FromThis(),
                    chosen,
                    Vector2.Zero,
                    ModContent.ProjectileType<ElementalBoulder>(),
                    EnemyDamage.Projectile(StoneBoulderExpertDamage),
                    4f,
                    Main.myPlayer,
                    StoneBoulderTelegraphTicks,
                    rollDirection);
            }
        }

        void RunWindPullPush()
        {
            Vector2 palm = FrontPalm;

            // Phase 1: 24t tell — swirling wind ring gathering at the palm.
            if (spellTimer <= WindTellTicks)
            {
                if (!Main.dedServ)
                {
                    if (spellTimer == 1)
                    {
                        SoundEngine.PlaySound(SoundID.DD2_BookStaffCast with { Volume = 0.65f, Pitch = 0.25f }, palm);
                    }

                    float progress = spellTimer / (float)WindTellTicks;
                    for (int i = 0; i < 2; i++)
                    {
                        float angle = spellTimer * 0.25f + i * MathHelper.Pi;
                        float radius = MathHelper.Lerp(32f, 10f, progress);
                        Vector2 swirlPos = palm + angle.ToRotationVector2() * radius;
                        Vector2 tangent = new Vector2(-MathF.Sin(angle), MathF.Cos(angle)) * 2.5f + (palm - swirlPos) * 0.1f;
                        Dust wind = Dust.NewDustPerfect(swirlPos, DustID.Cloud, tangent, 120, new Color(210, 240, 255), Main.rand.NextFloat(0.85f, 1.25f));
                        wind.noGravity = true;
                    }
                }

                return;
            }

            // Phase 2: 2.0s (120t) light pull — every client pulls its own local player lightly while wind streams flow inward.
            if (spellTimer <= WindPullEndTick)
            {
                if (!Main.dedServ)
                {
                    if (spellTimer == WindTellTicks + 1 || (spellTimer - WindTellTicks) % 30 == 0)
                    {
                        SoundEngine.PlaySound(SoundID.Item66 with { Volume = 0.45f, Pitch = -0.15f }, NPC.Center);
                    }

                    ApplyLocalPlayerWindPull();
                    EmitWindPullDusts(palm);
                }

                return;
            }

            // Phase 3: 16t compression brace — distinct cue right before the forceful push.
            if (spellTimer < WindPushTick)
            {
                if (!Main.dedServ)
                {
                    if (spellTimer == WindPullEndTick + 1)
                    {
                        SoundEngine.PlaySound(SoundID.DD2_BookStaffCast with { Volume = 0.7f, Pitch = 0.55f }, palm);
                    }

                    for (int i = 0; i < 3; i++)
                    {
                        Vector2 offset = Main.rand.NextVector2CircularEdge(22f, 22f);
                        Dust compress = Dust.NewDustPerfect(palm + offset, DustID.Cloud, -offset * 0.2f, 90, new Color(225, 248, 255), Main.rand.NextFloat(0.9f, 1.3f));
                        compress.noGravity = true;
                    }
                }

                return;
            }

            // Phase 4: Forceful push burst (velocity impulse on WindPushTick + outward gust streaks for 16t).
            if (spellTimer <= WindPushTick + WindPushWindowTicks)
            {
                if (!Main.dedServ)
                {
                    if (spellTimer == WindPushTick)
                    {
                        SoundEngine.PlaySound(SoundID.DD2_BetsyWindAttack with { Volume = 0.85f, Pitch = 0.1f }, palm);
                        ApplyLocalPlayerWindPush();
                        EmitWindPushBurst(palm, 26);
                    }
                    else
                    {
                        EmitWindPushBurst(palm, 5);
                    }
                }
            }
        }

        // Runs on each client for its own local player (player.velocity is client-owned in multiplayer; tiles stop movement normally).
        void ApplyLocalPlayerWindPull()
        {
            Player localPlayer = Main.LocalPlayer;
            if (localPlayer == null || !localPlayer.active || localPlayer.dead)
            {
                return;
            }

            Vector2 toCaster = NPC.Center - localPlayer.Center;
            float distance = toCaster.Length();
            if (distance > WindForceRadius || distance < WindPullInnerDeadzone)
            {
                return;
            }

            Vector2 pullDir = toCaster / distance;
            float currentTowardSpeed = Vector2.Dot(localPlayer.velocity, pullDir);
            if (currentTowardSpeed < WindPullMaxSpeed)
            {
                localPlayer.velocity += pullDir * WindPullAccel;
            }
        }

        void ApplyLocalPlayerWindPush()
        {
            Player localPlayer = Main.LocalPlayer;
            if (localPlayer == null || !localPlayer.active || localPlayer.dead)
            {
                return;
            }

            Vector2 awayFromCaster = localPlayer.Center - NPC.Center;
            float distance = awayFromCaster.Length();
            if (distance > WindForceRadius)
            {
                return;
            }

            Vector2 pushDir = awayFromCaster.SafeNormalize(new Vector2(NPC.direction, 0f));
            Vector2 impulse = pushDir * WindPushSpeed + new Vector2(0f, WindPushLift);
            localPlayer.velocity = impulse;
        }

        void EmitWindPullDusts(Vector2 palm)
        {
            // Stream wind motes from around the target and mid-air inward into the caster's palm.
            Vector2 towardTarget = (SpellTarget.Center - palm).SafeNormalize(new Vector2(NPC.direction, 0f));
            Vector2 perpendicular = new Vector2(-towardTarget.Y, towardTarget.X);
            float maxStreamDist = Math.Min(WindForceRadius, NPC.Distance(SpellTarget.Center) + 90f);

            for (int i = 0; i < 4; i++)
            {
                float along = Main.rand.NextFloat(70f, Math.Max(90f, maxStreamDist));
                float across = Main.rand.NextFloat(-75f, 75f);
                Vector2 spawnPos = palm + towardTarget * along + perpendicular * across;
                Vector2 inwardVel = (palm - spawnPos).SafeNormalize(Vector2.Zero) * Main.rand.NextFloat(6.5f, 11f);
                int dustType = DustID.Cloud;
                if (i == 3)
                {
                    dustType = DustID.WhiteTorch;
                }

                Dust streak = Dust.NewDustPerfect(spawnPos, dustType, inwardVel, 110, new Color(210, 240, 255), Main.rand.NextFloat(0.75f, 1.25f));
                streak.noGravity = true;
            }
        }

        void EmitWindPushBurst(Vector2 palm, int count)
        {
            Vector2 pushAxis = (SpellTarget.Center - palm).SafeNormalize(new Vector2(NPC.direction, 0f));
            Vector2 perpendicular = new Vector2(-pushAxis.Y, pushAxis.X);

            for (int i = 0; i < count; i++)
            {
                float spreadAngle = Main.rand.NextFloat(-0.5f, 0.5f);
                Vector2 gustDir = pushAxis.RotatedBy(spreadAngle);
                Vector2 startPos = palm + perpendicular * Main.rand.NextFloat(-18f, 18f);
                Vector2 gustVel = gustDir * Main.rand.NextFloat(9f, 16f);
                int dustType = DustID.Cloud;
                if (i % 3 == 0)
                {
                    dustType = DustID.WhiteTorch;
                }

                Dust gust = Dust.NewDustPerfect(startPos, dustType, gustVel, 80, new Color(220, 245, 255), Main.rand.NextFloat(0.85f, 1.4f));
                gust.noGravity = true;
            }
        }
        #endregion

        #region Draw & Death
        // Fireball preview grows in the casting palm over the last 18 tell ticks so the projectile never pops in.
        public override void PostDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            base.PostDraw(spriteBatch, screenPos, drawColor);

            bool previewingFireball = spellState == SpellAct.SyncFireball
                && spellTimer > FireballTellTicks - FireballPreviewTicks
                && spellTimer < FireballTellTicks;
            if (!previewingFireball)
            {
                return;
            }

            Main.instance.LoadProjectile(ProjectileID.Fireball);
            Texture2D texture = TextureAssets.Projectile[ProjectileID.Fireball].Value;
            float growth = (spellTimer - (FireballTellTicks - FireballPreviewTicks)) / (float)FireballPreviewTicks;
            float rotation = spellTimer * 0.25f * NPC.direction;

            spriteBatch.Draw(
                texture,
                FrontPalm - screenPos,
                null,
                Color.White * (0.5f + 0.5f * growth),
                rotation,
                texture.Size() * 0.5f,
                growth,
                SpriteEffects.None,
                0f);
        }

        // Replaces the puppet's generic blood burst. Runs on every client (OnKill does not run on MP clients).
        // ~80 purple motes thrown outward at 2-7 px/t plus a 24-mote ring at the edge; no gore. The death
        // sound is NPC.DeathSound (NPCDeath6).
        public override void HitEffect(NPC.HitInfo hit)
        {
            if (NPC.life > 0 || Main.dedServ)
            {
                return;
            }

            Lighting.AddLight(NPC.Center, 0.9f, 0.4f, 1.3f);

            for (int i = 0; i < 80; i++)
            {
                int dustType = DustID.PurpleTorch;
                if (i % 3 == 0)
                {
                    dustType = DustID.Shadowflame;
                }
                else if (i % 10 == 1)
                {
                    dustType = DustID.PurpleCrystalShard;
                }

                Vector2 velocity = Main.rand.NextVector2Unit() * Main.rand.NextFloat(2f, 7f);
                Vector2 position = NPC.Center + Main.rand.NextVector2Circular(NPC.width * 0.5f, NPC.height * 0.5f);
                Dust mote = Dust.NewDustPerfect(position, dustType, velocity, 60, default, Main.rand.NextFloat(1.2f, 2f));
                mote.noGravity = true;
            }

            for (int i = 0; i < 24; i++)
            {
                float angle = MathHelper.TwoPi * i / 24f;
                Vector2 direction = angle.ToRotationVector2();
                Dust ring = Dust.NewDustPerfect(NPC.Center + direction * 10f, DustID.PurpleTorch, direction * 7f, 40, default, 1.8f);
                ring.noGravity = true;
            }
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            npcLoot.Add(ItemDropRule.ByCondition(tsorcRevamp.tsorcItemDropRuleConditions.SuperHardmodeRule, ModContent.ItemType<WhiteTitanite>(), 4, 1, 2, 50));
        }
        #endregion
    }
}

using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;
using tsorcRevamp.Content.Items;
using tsorcRevamp.Content.Items.Materials;
using tsorcRevamp.Content.Items.Materials.Titanite;
using tsorcRevamp.Content.Projectiles.Enemy;
using tsorcRevamp.Content.Projectiles.Enemy.Weapons;
using tsorcRevamp.NPCs.Enemies;
using tsorcRevamp.NPCs.Invaders;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.NPCs.Invaders
{
    /// <summary>
    /// Great Red Knight: the RedKnightPuppet body in the RedKnightSet armour under a Red &amp; Black dye, carrying the
    /// Ancient Blood Lance. Phase one draws from the Great pool (TryStartGreat) plus the shared spear combos; at 30% it
    /// plants into Crimson Dominion and from then on fights from the narrow Dominion pool, with the Dominion lightning
    /// loop running every tick. Its death is the Dominion finale: replant, seal, nova, then death.
    /// Drawn at 1.0x like Red Knight (no PuppetDrawScale); size reads from the lance, dye and two-handed poses.
    /// </summary>
    [AutoloadBossHead]
    class GreatRedKnight : RedKnightPuppet, IHumanoidMeleeHitEffects
    {
        // PuppetNPC points Texture at the shared puppet placeholder, so [AutoloadBossHead]'s
        // Texture + "_Head_Boss" convention would look for "PuppetPlaceholder_Head_Boss" and crash at load.
        public override string BossHeadTexture => UsefulFunctions.RefactorableFilepath(typeof(GreatRedKnight)) + "_Head_Boss";

        NPCDespawnHandler despawnHandler;

        // --- Crimson Dominion death finale ----------------------------------------------------
        // Dominion no longer ends on a timer, so its finishing seal + nova is GRK's DEATH: the
        // moment lethal damage lands, CheckDead pins the knight at 1 HP and refuses to let it die
        // until the blast actually goes off. Precedent for the pattern: BossBase.CheckDead
        // (deathAnimationProgress) and Cataluminance.CheckDead (NPCs/Bosses/Triad/Cataluminance.cs:747).
        //
        // The order is deliberately NOVA-FIRST, DEATH-SECOND — the explosion is a real last attack,
        // not a cosmetic parting shot:
        //
        //   t = 0   .. 59   REPLANT. The knight freezes and drives its spear back into the ground in both
        //                   hands (the shared plant pose, finale timing) and the Destined Death engulf returns.
        //   t = 60          The finale CrimsonDominionController spawns and the seal starts filling.
        //   t = 60  .. 149  Seal fill (SealFillTicks = 90) — the telegraph to get clear.
        //   t = 150         NOVA. The blast fires AND the knight dies on the same tick, so loot,
        //                   Dark Souls, boss-down flags and BossExtras (all of which hang off the
        //                   real OnKill) land exactly when the explosion does.
        //
        // The finale runs as a held AttackPhase.Custom (started from CheckDead), so the puppet keeps animating and
        // every client follows the phase snapshot plus the synced dominionDeathTimer.
        // The controller projectile outlives the NPC by ~45 ticks to finish the nova + fade. That
        // is safe: it is netImportant, ShouldUpdatePosition() => false, and never dereferences the
        // NPC (ai[2] carries whoAmI but nothing reads it).
        // -1 = not started.
        int dominionDeathTimer = -1;

        /// <summary>Ticks the finale spends re-planting the spear before the seal begins to fill.</summary>
        public const int DominionDeathReplantTicks = 60;

        /// <summary>Ticks from lethal damage to the knight's ACTUAL death — which is the tick the
        /// nova fires, not the end of its fade.</summary>
        public const int DominionDeathSequenceTicks = DominionDeathReplantTicks
            + CrimsonDominionController.SealFillTicks;

        // Safety margin on the finale's Custom phase cap; the knight dies at DominionDeathSequenceTicks.
        const int FinalePhaseSlackTicks = 60;

        public bool InDominionDeathSequence => dominionDeathTimer >= 0;

        // ── Extended Great Red Knight melee combos ──────────────────────────────────────────────
        // Built on the shared spear/lance thrust & sweep engine, tuned for boss aggression:
        //
        // Crimson Flurry — close (<= 145px), 3-hit extended spear combo.
        //   Step 1: JoustDash thrust. Tell 28t on screen | 24t attack (punch 6t, live 6..15 = 10t, rollable) | pause 22t.
        //   Step 2: UnderhandArc sweep (1.13 -> -0.11). 26t Weighted 5/21 k6 -> HitWindowEnd 0.354 (~9t live, rollable).
        //           Starts 31t after Step 1 live ends (>= 30t, re-rollable). Pause 24t (re-faces player).
        //   Step 3: Heavy JoustDash finisher. 28t attack (punch 6t, live 6..16 = 11t, rollable), 1.65x push, 1.25x dmg.
        //           Starts 47t after Step 2 live ends (>= 30t, and >= 40t after Step 2 start so both can damage).
        //   Recovery 28t. Continues Step 2 within 195px, Step 3 within 215px.
        //
        // Bloodlance Rush — mid (130-360px, RangedStartOnly), distance-solved dash thrust into rising sweep.
        //   Step 1: JoustDash (authored 30t dash, +150px overshoot, live 6..19 = 13t, rollable). Tell 34t | pause 22t.
        //   Step 2: UnderhandArc sweep (24t Weighted 4/20 k6, HitWindowEnd 0.334 = 8t live, rollable).
        //           Starts 33t after Step 1 live ends (>= 30t). Continues if target stays within 175px after dash.
        //   Recovery 30t.
        //
        // Double Spear Lunge / Rising Skewer — faster boss-paced versions of the close-range lance staples.
        const string CrimsonFlurryName = "Crimson Flurry";
        const string BloodlanceRushName = "Bloodlance Rush";
        const string GreatDoubleLungeName = "Double Spear Lunge";
        const string GreatRisingSkewerName = "Rising Skewer";
        const int GreatThrustTicks = 24;
        const int GreatThrustLiveEndTick = 15;
        const int GreatHeavyThrustTicks = 28;
        const int GreatHeavyThrustLiveEndTick = 16;
        const int GreatChargeTicks = 30;
        const int GreatChargeLiveEndTick = 19;
        const float GreatMeleeEngageRange = 145f;
        const float GreatChargeMaxRange = 360f;
        const float FlurryStep2Range = 195f;
        const float FlurryStep3Range = 215f;
        const float RushFollowUpRange = 175f;

        static readonly MeleeCombo[] GreatRedKnightCombos = new MeleeCombo[]
        {
            new MeleeCombo
            {
                Name = CrimsonFlurryName, BaseWeight = 115, Preferred = ComboRangeBand.Close,
                InitialFlashColor = Color.Crimson, CooldownAfterUse = 75, RecoveryTicks = 28,
                Steps = new[]
                {
                    new MeleeComboStep
                    {
                        Motion = ComboMotion.JoustDash, TelegraphTicks = 28, AttackTicks = GreatThrustTicks,
                        PostStepPause = 22, DamageMult = 1f, ReachMult = 1f, ForwardPushMult = 1.35f,
                        SwingSpeedMult = 1f, Ease = SwingEaseStyle.Smooth,
                        HitWindowEnd = GreatThrustLiveEndTick / (float)GreatThrustTicks,
                    },
                    // HitWindowEnd = (5 + 21 * ln(1/0.3) / 6) / 26 = 0.354f.
                    new MeleeComboStep
                    {
                        Motion = ComboMotion.UnderhandArc, TelegraphTicks = 0, AttackTicks = 26,
                        PostStepPause = 24, DamageMult = 1.05f, ReachMult = 1f, ForwardPushMult = 1.1f,
                        SwingSpeedMult = 1f, Ease = SwingEaseStyle.Weighted,
                        EaseInTicks = 5, EaseOutTicks = 21, EaseOutDecay = 6f,
                        HitWindowEnd = 0.354f,
                    },
                    new MeleeComboStep
                    {
                        Motion = ComboMotion.JoustDash, TelegraphTicks = 0, AttackTicks = GreatHeavyThrustTicks,
                        DamageMult = 1.25f, ReachMult = 1.05f, ForwardPushMult = 1.65f,
                        SwingSpeedMult = 1f, Ease = SwingEaseStyle.Smooth,
                        HitWindowEnd = GreatHeavyThrustLiveEndTick / (float)GreatHeavyThrustTicks,
                    },
                },
            },
            new MeleeCombo
            {
                Name = BloodlanceRushName, BaseWeight = 120, Preferred = ComboRangeBand.Mid,
                InitialFlashColor = Color.Crimson, RangedStartOnly = true, CooldownAfterUse = 95, RecoveryTicks = 30,
                Steps = new[]
                {
                    // Step 0 uses RedKnightPuppet's distance-solved dash velocity, so ForwardPushMult stays 0.
                    new MeleeComboStep
                    {
                        Motion = ComboMotion.JoustDash, TelegraphTicks = 34, AttackTicks = GreatChargeTicks,
                        PostStepPause = 22, DamageMult = 1.25f, ReachMult = 1f, ForwardPushMult = 0f,
                        SwingSpeedMult = 1f, Ease = SwingEaseStyle.Smooth,
                        HitWindowEnd = GreatChargeLiveEndTick / (float)GreatChargeTicks,
                    },
                    // HitWindowEnd = (4 + 20 * ln(1/0.3) / 6) / 24 = 0.334f.
                    new MeleeComboStep
                    {
                        Motion = ComboMotion.UnderhandArc, TelegraphTicks = 0, AttackTicks = 24,
                        DamageMult = 1.15f, ReachMult = 1f, ForwardPushMult = 1f,
                        SwingSpeedMult = 1f, Ease = SwingEaseStyle.Weighted,
                        EaseInTicks = 4, EaseOutTicks = 20, EaseOutDecay = 6f,
                        HitWindowEnd = 0.334f,
                    },
                },
            },
            new MeleeCombo
            {
                Name = GreatDoubleLungeName, BaseWeight = 85, Preferred = ComboRangeBand.Close,
                InitialFlashColor = Color.OrangeRed, CooldownAfterUse = 60, RecoveryTicks = 26,
                Steps = new[]
                {
                    new MeleeComboStep
                    {
                        Motion = ComboMotion.JoustDash, TelegraphTicks = 28, AttackTicks = GreatThrustTicks,
                        PostStepPause = 22, DamageMult = 1f, ReachMult = 1f, ForwardPushMult = 1.35f,
                        SwingSpeedMult = 1f, Ease = SwingEaseStyle.Smooth,
                        HitWindowEnd = GreatThrustLiveEndTick / (float)GreatThrustTicks,
                    },
                    new MeleeComboStep
                    {
                        Motion = ComboMotion.JoustDash, TelegraphTicks = 0, AttackTicks = GreatThrustTicks,
                        DamageMult = 0.95f, ReachMult = 1f, ForwardPushMult = 1.35f,
                        SwingSpeedMult = 1f, Ease = SwingEaseStyle.Smooth,
                        HitWindowEnd = GreatThrustLiveEndTick / (float)GreatThrustTicks,
                    },
                },
            },
            new MeleeCombo
            {
                Name = GreatRisingSkewerName, BaseWeight = 70, Preferred = ComboRangeBand.Close,
                InitialFlashColor = Color.OrangeRed, CooldownAfterUse = 90, RecoveryTicks = 26,
                Steps = new[]
                {
                    new MeleeComboStep
                    {
                        Motion = ComboMotion.JoustDash, TelegraphTicks = 26, AttackTicks = GreatThrustTicks,
                        PostStepPause = 14, DamageMult = 1f, ReachMult = 1f, ForwardPushMult = 0.9f,
                        SwingSpeedMult = 1f, Ease = SwingEaseStyle.Smooth,
                        HitWindowEnd = GreatThrustLiveEndTick / (float)GreatThrustTicks,
                    },
                    new MeleeComboStep
                    {
                        Motion = ComboMotion.UnderhandArc, TelegraphTicks = 0, AttackTicks = 22,
                        DamageMult = 1.15f, ReachMult = 1f, ForwardPushMult = 0.7f, SwingSpeedMult = 1f,
                        Ease = SwingEaseStyle.Weighted, EaseInTicks = 4, EaseOutTicks = 18, EaseOutDecay = 6f,
                        HitWindowEnd = 0.346f,
                    },
                },
            },
        };

        // ── Aggression spike / breather tempo (server-authoritative; breatherActive synced for stalk speed) ──
        // Chains 2-3 (P1), 3-4 (<= 60% HP), or 3-5 (Dominion) melee/throw/dash attacks tightly,
        // then takes a deliberate stalking breather so the player has real punish/heal windows.
        const float BreatherWalkSpeedMult = 0.72f;
        int aggressionBurstRemaining;
        int breatherTicks;
        bool breatherActive;
        bool preferAggressiveChainSpecial;

        bool IsInMeleeComboPhase => Phase == AttackPhase.MeleeComboTelegraph
            || Phase == AttackPhase.MeleeComboAttack
            || Phase == AttackPhase.MeleeComboPause
            || Phase == AttackPhase.MeleeComboRecovery;

        #region Defaults
        protected override string InvaderTitle => "Great Red Knight";
        protected override bool UsesGreatKnightPoses => true;
        protected override bool HoldsFinalePose => dominionDeathTimer >= 0;
        protected override Color KnightAccentColor => new Color(206, 16, 34);
        protected override int KnightGlintDust => DustID.RedTorch;

        // The family armour, darkened to Destined Death black-and-crimson.
        protected override int HeadArmorDyeItemType => ItemID.RedandBlackDye;
        protected override int BodyArmorDyeItemType => ItemID.RedandBlackDye;
        protected override int LegsArmorDyeItemType => ItemID.RedandBlackDye;

        // Boss-paced movement matching Owl Father's tempo and accelerating further in Crimson Dominion.
        protected override float TopSpeed => specialAttacks.DominionEngaged ? 3.3f : 2.75f;
        protected override float Acceleration => 0.105f;
        protected override MeleeCombo[] MeleeComboPoolOverride => GreatRedKnightCombos;
        protected override float MeleeEngageRange => GreatMeleeEngageRange;
        protected override float ComboMaxStartRange => 150f;
        protected override float RangedStartComboMaxRange => GreatChargeMaxRange;
        protected override int RangedStartMeleeComboChance => 65;
        protected override float ComboTelegraphAdvanceSpeedMult => 0.85f;

        // EnemyAncientBloodLanceProj.png measured: 108x108, head (red blade) at texel (0,0), butt at (107,107). Same
        // top-left-head diagonal as Crystal Knight's North Pole, so the same +90 degree draw offset. At 0.75 the lance
        // is ~115px long against Red Knight's 62px spear (chosen from a 1.0 / 0.75 / 0.5 render comparison).
        protected override string SpearDrawTexturePath => UsefulFunctions.RefactorableFilepath(typeof(EnemyAncientBloodLanceProj));
        protected override Vector2 SpearTextureSize => new Vector2(108f);
        protected override Vector2 SpearHeadNorm => new Vector2(0.5f / 108f);
        protected override Vector2 SpearBaseNorm => new Vector2(107.5f / 108f);
        protected override float SpearDrawRotationOffset => MathHelper.PiOver2;
        protected override float MeleeWeaponDrawScale => 0.75f;
        // Rear hand sits this far up the shaft from the front hand (negative = toward the butt).
        protected override float TwoHandedBackGripOffset => -5f;

        public override void SetStaticDefaults()
        {
            // GreatRedKnight.png (16 frames) stays as the bestiary / placeholder sprite; the puppet draws the body.
            Main.npcFrameCount[NPC.type] = 1;
            NPCID.Sets.TrailCacheLength[NPC.type] = 3;
            NPCID.Sets.TrailingMode[NPC.type] = 0;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.OnFire] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.OnFire3] = true;
        }

        public override void SetDefaults()
        {
            NPC.npcSlots = 5;
            NPC.aiStyle = -1;
            NPC.width = 20;
            NPC.height = 46;
            // A humanoid puppet deals no body-contact damage; every hit is a lance, spell or bomb.
            NPC.damage = 0;
            NPC.defense = 61;
            NPC.lifeMax = 30000;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.value = 225000; // 9000 Dark Souls via GlobalNPC.OnKill expert-mode payout
            NPC.knockBackResist = 0.2f; // poise flinch dial (boss)
            NPC.boss = true;
            Banner = NPC.type;
            BannerItem = ModContent.ItemType<Banners.GreatRedKnightBanner>();
            despawnHandler = new NPCDespawnHandler(LangUtils.GetTextValue("NPCs.GreatRedKnight.DespawnHandler"), Color.Red, DustID.RedTorch);

            // Real expert damage (declared Expert hit before defense; spawns go through EnemyDamage.Projectile).
            redKnightsSpearDamage = 180;
            redMagicDamage = 160;
            redKnightsGreatDamage = 200;
            // Benchmarked against this boss's own numbers: the great attack (200) is the heaviest thing it has, and a
            // lobbed AoE with an arc, a visible fuse and a 120px blast the player has ~2s to leave sits just above it.
            redKnightsBombDamage = 248;

            tsorcRevampGlobalNPC redKnightGlobalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            redKnightGlobalNPC.Agility = 0.45f;
            redKnightGlobalNPC.DirectionalDodgeRolls = true;
            redKnightGlobalNPC.PreserveJumpFacingUntilLanding = true;
            EvasiveProfile.RedKnight(redKnightGlobalNPC);
            redKnightGlobalNPC.EvasiveRetreatJump = false;
            redKnightGlobalNPC.EvasiveRetreatDash = false;
            redKnightGlobalNPC.EvasiveDodgeRoll = true;
            redKnightGlobalNPC.EvasiveTeleportAway = false;
            redKnightGlobalNPC.EvasiveOnHitChanceDenominator = 6;
            redKnightGlobalNPC.EvasiveOnHitCooldownTicks = 120;

            // Poise: boss-tier — many hits to stagger, and the impulse is halved for bosses. Tunable lever.
            redKnightGlobalNPC.PoiseMax = 80f;
            redKnightGlobalNPC.PoiseStaggerResetsAI = true; // a stagger cancels a windup attack → neutral

            // Blink aggressively the moment it loses LOS (keeps arena pressure); Dominion switches it off.
            redKnightGlobalNPC.CanTeleport = true;
            redKnightGlobalNPC.TeleportStyle = TeleportStyle.Aggressive;
            redKnightGlobalNPC.TeleportVisualStyle = TeleportVisualStyle.Fire;
            // Reacquire = a flank on the target's far side, also after nav gave up, out to 100 tiles.
            redKnightGlobalNPC.TeleportFlanksTarget = true;
            redKnightGlobalNPC.TeleportReacquiresAfterGiveUp = true;
            redKnightGlobalNPC.TeleportReacquireMaxRange = 1600f;
            redKnightGlobalNPC.NavSearchRadius = 80;
            redKnightGlobalNPC.CanUseRopes = true;
        }
        #endregion

        //Never despawn except by timing out
        public override bool CheckActive()
        {
            return false;
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            base.SendExtraAI(writer);
            // CheckDead only ever runs server-side, so without this a multiplayer client would never pose the finale.
            writer.Write(dominionDeathTimer);
            writer.Write(breatherActive);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            base.ReceiveExtraAI(reader);
            dominionDeathTimer = reader.ReadInt32();
            breatherActive = reader.ReadBoolean();
        }

        #region AI
        public override void AI()
        {
            despawnHandler.TargetAndDespawn(NPC.whoAmI);
            if (!NPC.active || despawnHandler.IsDespawning)
            {
                return;
            }

            Player target = Main.player[NPC.target];
            bool specialActiveBefore = specialAttacks.Active;
            KnightSpecialAttack activeSpecialBefore = specialAttacks.Attack;
            bool inMeleeBefore = IsInMeleeComboPhase;

            if (Main.netMode != NetmodeID.MultiplayerClient && breatherTicks > 0)
            {
                breatherTicks--;
                if (breatherTicks == 0 && breatherActive)
                {
                    breatherActive = false;
                    NPC.netUpdate = true;
                }
            }

            tsorcRevampGlobalNPC globalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            if (dominionDeathTimer < 0)
            {
                // CRIMSON DOMINION phase 2 runs the lightning loop on top of everything else, including during the
                // plant-and-hold, which is what puts the first Stage A bolts inside it.
                KnightAttackStats attackStats = new KnightAttackStats(
                    redKnightsSpearDamage, redMagicDamage, redKnightsGreatDamage, redKnightsBombDamage);
                specialAttacks.TickDominionSequence(NPC, target, attackStats);
            }

            // Dominion owns a narrow vocabulary: Crimson Advance, Firebomb Reversal, Royal Spear Throw,
            // plus Great Red Knight's extended lance combos. Generic evasive dashes, fallback hops and
            // teleports are shut down so their tells cannot compete with it.
            if (specialAttacks.DominionEngaged)
            {
                globalNPC.EvasiveRetreatJump = false;
                globalNPC.EvasiveRetreatDash = false;
                globalNPC.EvasiveTeleportAway = false;
                globalNPC.EvasiveLeapForward = false;
                globalNPC.EvasiveRunningDash = false;
                globalNPC.InSustainedEvasion = false;
                globalNPC.EvasiveTelegraphing = false;
                globalNPC.EvasiveTimer = 0;
                globalNPC.CanTeleport = false;
                globalNPC.Agility = 0.58f;
            }

            base.AI();

            if (Main.netMode != NetmodeID.MultiplayerClient && dominionDeathTimer < 0)
            {
                UpdateAggressionTempo(target, specialActiveBefore, activeSpecialBefore, inMeleeBefore);
            }

            // The finale is immune throughout; re-assert after the base tick in case anything in it cleared the flag.
            if (dominionDeathTimer >= 0)
            {
                NPC.dontTakeDamage = true;
            }

            Lighting.AddLight(NPC.Center, new Color(190, 35, 30).ToVector3() * 0.3f);
            DebugAttackLabel = DominionDebugLabel();
        }

        // Tracks aggression spikes (2-5 tightly chained melee/throw/dash moves) and deliberate breathers
        // between spikes so the fight has Owl-Father-plus pressure spikes without erasing punish windows.
        void UpdateAggressionTempo(Player target, bool specialActiveBefore,
            KnightSpecialAttack activeSpecialBefore, bool inMeleeBefore)
        {
            bool specialStarted = !specialActiveBefore && specialAttacks.Active;
            bool meleeStarted = !inMeleeBefore && IsInMeleeComboPhase;
            if (specialStarted || meleeStarted)
            {
                if (aggressionBurstRemaining <= 0)
                {
                    if (specialAttacks.DominionEngaged)
                    {
                        aggressionBurstRemaining = Main.rand.Next(3, 6);
                    }
                    else if (NPC.life <= NPC.lifeMax * 0.6f)
                    {
                        aggressionBurstRemaining = Main.rand.Next(3, 5);
                    }
                    else
                    {
                        aggressionBurstRemaining = Main.rand.Next(2, 4);
                    }
                }

                // Forced phase transitions (heralds, Dominion plant) break any active breather cleanly.
                if (specialStarted && breatherActive)
                {
                    breatherTicks = 0;
                    breatherActive = false;
                    NPC.netUpdate = true;
                }
            }

            bool specialFinished = specialActiveBefore && !specialAttacks.Active;
            bool meleeFinished = inMeleeBefore && !IsInMeleeComboPhase;
            if (!specialFinished && !meleeFinished)
            {
                return;
            }

            // Major phase transitions end the current spike cleanly without piling on a long breather.
            bool phaseTransition = specialFinished
                && (activeSpecialBefore == KnightSpecialAttack.FurnaceHerald
                    || activeSpecialBefore == KnightSpecialAttack.StormHerald
                    || activeSpecialBefore == KnightSpecialAttack.CrimsonDominion);
            if (phaseTransition)
            {
                aggressionBurstRemaining = 0;
                preferAggressiveChainSpecial = false;
                if (activeSpecialBefore != KnightSpecialAttack.CrimsonDominion)
                {
                    specialAttacks.SetCooldown(75);
                }
                return;
            }

            if (aggressionBurstRemaining > 0)
            {
                aggressionBurstRemaining--;
            }

            if (aggressionBurstRemaining > 0)
            {
                float distance = target.active && !target.dead
                    ? NPC.Distance(target.Center)
                    : 0f;
                int chainDelay = specialAttacks.DominionEngaged || NPC.life <= NPC.lifeMax * 0.6f
                    ? Main.rand.Next(8, 15)
                    : Main.rand.Next(12, 20);

                if (meleeFinished)
                {
                    // Chain out of melee into Spear Throw, Crimson Advance dash, Firebomb Reversal, or Royal Standard
                    // most of the time; at point-blank occasionally allow a back-to-back melee combo instead.
                    bool chainDirectMelee = distance <= 155f && Main.rand.NextBool(3);
                    if (chainDirectMelee)
                    {
                        preferAggressiveChainSpecial = false;
                        specialAttacks.SetCooldown(45);
                    }
                    else
                    {
                        preferAggressiveChainSpecial = true;
                        specialAttacks.SetCooldown(chainDelay);
                    }
                }
                else
                {
                    // After a special, give melee combos / Bloodlance Rush a brief window to follow up
                    // when in dash range; otherwise immediately chain another aggressive special.
                    preferAggressiveChainSpecial = false;
                    if (distance <= GreatChargeMaxRange)
                    {
                        int meleeFollowUpWindow = specialAttacks.DominionEngaged ? 32 : 42;
                        specialAttacks.SetCooldown(meleeFollowUpWindow);
                    }
                    else
                    {
                        specialAttacks.SetCooldown(chainDelay);
                    }
                }
                return;
            }

            // Spike complete: take a deliberate stalking breather so the player can heal, recover stamina, or punish.
            if (specialAttacks.DominionEngaged)
            {
                breatherTicks = Main.rand.Next(85, 130);
            }
            else if (NPC.life <= NPC.lifeMax * 0.6f)
            {
                breatherTicks = Main.rand.Next(110, 160);
            }
            else
            {
                breatherTicks = Main.rand.Next(135, 195);
            }
            breatherActive = true;
            preferAggressiveChainSpecial = false;
            specialAttacks.SetCooldown(breatherTicks);
            NPC.netUpdate = true;
        }

        protected override void RunMovementAI(float speedMult)
        {
            if (breatherActive)
            {
                speedMult *= BreatherWalkSpeedMult;
            }
            base.RunMovementAI(speedMult);
        }

        public override void OnStagger(NPC npc)
        {
            base.OnStagger(npc);
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                aggressionBurstRemaining = 0;
                preferAggressiveChainSpecial = false;
                breatherTicks = 60;
                breatherActive = true;
                specialAttacks.SetCooldown(60);
                NPC.netUpdate = true;
            }
        }

        // The base labels controller attacks; Dominion is a permanent phase, so surface its stage and the finale.
        string DominionDebugLabel()
        {
            if (dominionDeathTimer >= 0)
            {
                string beat = "Seal Fill";
                if (dominionDeathTimer < DominionDeathReplantTicks)
                {
                    beat = "Replant";
                }
                return $"Dominion Finale — {beat} ({dominionDeathTimer}/{DominionDeathSequenceTicks})";
            }
            if (specialAttacks.Active)
            {
                return specialAttacks.DebugAttackName;
            }
            if (specialAttacks.DominionEngaged)
            {
                return "Dominion — " + specialAttacks.DominionStageName;
            }
            return null;
        }

        private protected override bool TryStartControllerAttack(Player target, tsorcRevampGlobalNPC globalNPC)
        {
            // The full Great pool is phase-one only (it also forces the Dominion plant at 30%).
            if (specialAttacks.DominionEngaged)
            {
                return specialAttacks.TryStartDominion(NPC, target, globalNPC);
            }
            bool started = specialAttacks.TryStartGreat(NPC, target, globalNPC, preferAggressiveChainSpecial);
            if (started)
            {
                preferAggressiveChainSpecial = false;
            }
            return started;
        }

        protected override bool IsChargingThrustCombo(string comboName)
        {
            return comboName == BloodlanceRushName || base.IsChargingThrustCombo(comboName);
        }

        protected override bool CanSelectMeleeCombo(MeleeCombo combo, float distance, float healthFraction)
        {
            if (breatherTicks > 0)
            {
                return false;
            }
            return base.CanSelectMeleeCombo(combo, distance, healthFraction);
        }

        protected override bool ShouldContinueMeleeCombo(string comboName, int nextStepIndex, Player target, bool previousStepHit)
        {
            if (!target.active || target.dead)
            {
                return false;
            }

            if (comboName == CrimsonFlurryName)
            {
                float maxRange = nextStepIndex == 1 ? FlurryStep2Range : FlurryStep3Range;
                return NPC.Distance(target.Center) <= maxRange;
            }

            if (comboName == BloodlanceRushName)
            {
                return NPC.Distance(target.Center) <= RushFollowUpRange;
            }

            return base.ShouldContinueMeleeCombo(comboName, nextStepIndex, target, previousStepHit);
        }

        protected override void DoCustomTick(int ticksRemaining)
        {
            if (dominionDeathTimer >= 0)
            {
                TickDominionDeathSequence();
                UpdateControllerPose();
                return;
            }

            base.DoCustomTick(ticksRemaining);

            if (!Main.dedServ && specialAttacks.Active && specialAttacks.SpearDamageWake)
            {
                EmitDestinedDeathLanceDust(count: 3, spread: 8f, speedScale: 2.2f);
            }
        }

        protected override void OnMeleeComboTelegraphTick(MeleeCombo combo, MeleeComboStep step, int elapsed, int total)
        {
            base.OnMeleeComboTelegraphTick(combo, step, elapsed, total);

            if (!Main.dedServ && elapsed >= total - 24)
            {
                EmitDestinedDeathLanceDust(count: 2, spread: 6f, speedScale: 1.1f);
            }
        }

        protected override void OnMeleeComboAttackTick(MeleeCombo combo, MeleeComboStep step, int elapsed, int total)
        {
            base.OnMeleeComboAttackTick(combo, step, elapsed, total);

            float progress = elapsed / (float)Math.Max(1, total);
            if (!Main.dedServ && CanDamageWithMeleeStep(step, progress))
            {
                int moteCount = IsChargingThrustCombo(combo.Name) && step.Motion == ComboMotion.JoustDash ? 4 : 3;
                EmitDestinedDeathLanceDust(count: moteCount, spread: 7f, speedScale: 2.4f);
            }
        }

        // On-brand Destined Death red & black embers along the Blood Lance tip and upper shaft:
        // crimson RedTorch (alpha 45) paired with dark Shadowflame soot (alpha 70, noLight).
        void EmitDestinedDeathLanceDust(int count, float spread, float speedScale)
        {
            Vector2 handPos = PuppetHandPosition;
            Vector2 tipPos = PuppetSpearTipPosition;
            Vector2 trailVelocity = -NPC.velocity * 0.25f;

            for (int i = 0; i < count; i++)
            {
                bool soot = i % 3 == 1;
                float alongShaft = MathHelper.Lerp(0.55f, 1f, Main.rand.NextFloat());
                Vector2 spawnPos = Vector2.Lerp(handPos, tipPos, alongShaft)
                    + Main.rand.NextVector2Circular(spread, spread);
                Vector2 velocity = trailVelocity + Main.rand.NextVector2Circular(speedScale, speedScale);
                Dust mote = Dust.NewDustPerfect(
                    spawnPos,
                    soot ? DustID.Shadowflame : DustID.RedTorch,
                    velocity,
                    soot ? 70 : 45,
                    soot ? new Color(18, 4, 10) : new Color(206, 16, 34),
                    soot ? Main.rand.NextFloat(0.95f, 1.35f) : Main.rand.NextFloat(0.85f, 1.35f));
                mote.noGravity = true;
                mote.noLight = soot;
            }
        }

        protected override void ModifyControllerPoseInput(ref ControllerPoseInput input)
        {
            if (dominionDeathTimer >= 0)
            {
                input.DominionTimer = dominionDeathTimer;
                input.DominionFinale = true;
            }
        }
        #endregion

        #region Dominion death finale
        /// <summary>
        /// Crimson Dominion's finishing seal + nova, repurposed as GRK's death animation. Once the
        /// knight is in Dominion it never leaves, so the blast is triggered by lethal damage rather
        /// than by a timer: pin at 1 HP, refuse to die, play the sequence, then actually die.
        /// Loot and boss-down flags all hang off OnKill, so they simply happen ~2.5s later.
        /// </summary>
        public override bool CheckDead()
        {
            // Echoes have one health pool. Skip the encounter's lethal-damage finale before it can refill life.
            if (MirrorShell)
            {
                return true;
            }

            // Killed before ever reaching 30% (e.g. a burst kill from above the gate): no Dominion,
            // no finale, die normally. Never gate the boss's death on a phase it never entered.
            if (!specialAttacks.DominionEngaged)
            {
                return base.CheckDead();
            }

            if (dominionDeathTimer < 0)
            {
                // BEAT 1 — the replant, as a held Custom phase so the puppet poses it. Any running controller attack
                // is dropped; emphatically no loot yet: the fight is not over until the nova fires.
                dominionDeathTimer = 0;
                NPC.dontTakeDamage = true;
                NPC.velocity = Vector2.Zero;
                if (specialAttacks.Active)
                {
                    specialAttacks.Cancel(NPC, NPC.GetGlobalNPC<tsorcRevampGlobalNPC>());
                }
                StartCustomAttack(DominionDeathSequenceTicks + FinalePhaseSlackTicks, MeleeWeaponItemType, swingPose: false);
                NPC.netUpdate = true;
                RequestNetworkSnapshot();
            }

            if (dominionDeathTimer < DominionDeathSequenceTicks)
            {
                NPC.life = 1;
                return false;
            }
            return true;
        }

        /// <summary>Runs as the finale's Custom tick on every machine. The knight is frozen, re-planted and engulfed,
        /// and immune; on the tick the nova fires it takes the killing blow it was spared, so OnKill (and therefore
        /// all loot) coincides with the explosion.</summary>
        void TickDominionDeathSequence()
        {
            dominionDeathTimer++;
            NPC.velocity.X = 0f;
            NPC.dontTakeDamage = true;
            NPC.knockBackResist = 0f;
            Lighting.AddLight(NPC.Center, new Color(206, 16, 34).ToVector3()
                * (0.5f + 0.5f * (dominionDeathTimer / (float)DominionDeathSequenceTicks)));

            if (dominionDeathTimer == 1 && !Main.dedServ)
            {
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item74 with { Volume = 0.9f, Pitch = -0.7f }, NPC.Center);
                tsorcRevampAIs.SpawnTelegraphFlash(NPC, new Color(206, 16, 34));
            }

            // BEAT 2 — the spear is in the ground and the flames are up, so the seal starts to
            // fill. |ai[0]| == 2 selects finale mode: seal fill -> nova -> fade, with none of the
            // containment ring, wall or arena-edge barrage. See CrimsonDominionController.
            if (dominionDeathTimer == DominionDeathReplantTicks
                && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Projectile.NewProjectile(NPC.GetSource_Death(), NPC.Center, Vector2.Zero,
                    ModContent.ProjectileType<CrimsonDominionController>(),
                    EnemyDamage.Projectile(redKnightsGreatDamage), 0f, Main.myPlayer,
                    2f, Main.rand.NextFloat(MathHelper.Pi / 12f), NPC.whoAmI);
                Terraria.Audio.SoundEngine.PlaySound(
                    SoundID.Item74 with { Volume = 0.8f, Pitch = -0.3f }, NPC.Center);
            }

            // BEAT 3 — the nova fires on this exact tick (spawn + SealFillTicks), and the knight
            // dies with it. Server-authoritative: a client never kills the NPC itself, it plays the
            // animation and waits for the server's death packet.
            if (dominionDeathTimer >= DominionDeathSequenceTicks
                && Main.netMode != NetmodeID.MultiplayerClient)
            {
                NPC.dontTakeDamage = false;
                NPC.life = 0;
                NPC.checkDead();
            }
        }
        #endregion

        public override void OnKill()
        {
            // create unknown embers that fill the explosion's radius
            for (int i = 0; i < 30; i++)
            {
                float velX = 1f - ((float)Main.rand.Next(20)) / 5f;
                float velY = 1f - ((float)Main.rand.Next(20)) / 5f;
                velX *= 4f;
                velY *= 4f;
                Dust.NewDust(new Vector2(NPC.position.X - (float)(NPC.width / 2), NPC.position.Y - (float)(NPC.height / 2)), NPC.width, NPC.height, DustID.EnchantedNightcrawler, velX, velY, 160, default, 1.5f);
                Dust.NewDust(new Vector2(NPC.position.X - (float)(NPC.width / 2), NPC.position.Y - (float)(NPC.height / 2)), NPC.width, NPC.height, DustID.CosmicEmber, velX, velY, 160, default, 1.5f);
                Dust.NewDust(new Vector2(NPC.position.X - (float)(NPC.width / 2), NPC.position.Y - (float)(NPC.height / 2)), NPC.width, NPC.height, DustID.EnchantedNightcrawler, velX, velY, 160, default, 1f);
                Dust.NewDust(new Vector2(NPC.position.X - (float)(NPC.width / 2), NPC.position.Y - (float)(NPC.height / 2)), NPC.width, NPC.height, DustID.CosmicEmber, velX, velY, 160, default, 1f);
            }

            if (!Main.dedServ)
            {
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Red Knight Gore 1").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Red Knight Gore 2").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Red Knight Gore 3").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Red Knight Gore 2").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Red Knight Gore 3").Type, 1f);
            }
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<Humanity>(), 1));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<SoulCoin>(), 1, 20, 50));

            IItemDropRule drop1 = ItemDropRule.Common(ModContent.ItemType<RedTitanite>(), 1, 4, 6);
            IItemDropRule drop2 = ItemDropRule.Common(ModContent.ItemType<PurgingStone>(), 1);
            IItemDropRule drop3 = ItemDropRule.Common(ModContent.ItemType<FlameOfTheAbyss>(), 1, 2, 3);
            SuperHardmodeRule SHM = new();
            IItemDropRule shmCondition = new LeadingConditionRule(SHM);
            shmCondition.OnSuccess(drop1);
            shmCondition.OnSuccess(drop2);
            shmCondition.OnSuccess(drop3);
            npcLoot.Add(shmCondition);
        }

        #region Debuffs
        // PuppetMeleeHitbox and RedKnightLungeHitbox both call this on the hit player's machine after Hurt.
        public void OnHumanoidMeleeHit(Player target)
        {
            target.AddBuff(BuffID.OnFire, 30 * 60, false);
            target.AddBuff(ModContent.BuffType<BrokenSpirit>(), 6 * 60, false); // knockback on hit
            target.AddBuff(ModContent.BuffType<DarkInferno>(), 3 * 60, false); // no health regen (halved from 6s)
            target.AddBuff(ModContent.BuffType<Crippled>(), 6 * 60, false); // loss of flight mobility
            target.AddBuff(BuffID.BrokenArmor, 6 * 60, false);
            target.AddBuff(ModContent.BuffType<DestinedDeath>(), DestinedDeath.DefaultDurationTicks, false); // 1 stack per hit
        }
        #endregion

        #region Draw
        /// <summary>
        /// Ramp for Crimson Dominion's body engulf. The flame belongs to the PLANTED SPEAR, not to
        /// the Dominion phase: it appears when the spear goes into the ground and leaves when the
        /// spear comes back out. So it plays exactly twice — across phase 1's plant-and-hold, and
        /// again across the death finale's replant.
        /// </summary>
        float DominionEngulfOpacity
        {
            get
            {
                // DEATH FINALE — the flames return as the finale's opening beat, then hold through the seal and nova.
                if (dominionDeathTimer >= 0)
                {
                    return MathHelper.Clamp(dominionDeathTimer / 30f, 0f, 1f);
                }

                // PHASE 1 (plant & hold) — on over the first 45t, and off again across the 50t retract.
                if (specialAttacks.Attack == KnightSpecialAttack.CrimsonDominion)
                {
                    int timer = specialAttacks.Timer;
                    return MathHelper.Clamp(timer / 45f, 0f, 1f)
                        * MathHelper.Clamp(
                            (RedKnightAttackController.DominionHoldTicks - timer) / 50f, 0f, 1f);
                }

                return 0f;
            }
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            // Synced hide state only: NPC.alpha is never synced (see RedKnightPuppet.PreDraw).
            tsorcRevampGlobalNPC globalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            bool visible = !specialAttacks.IsRedTeleportHidden && globalNPC.TeleportCountdown <= 0 && globalNPC.TeleportAppearanceTimer <= 0;
            if (visible && specialAttacks.IsHerald)
            {
                if (specialAttacks.Attack == KnightSpecialAttack.FurnaceHerald)
                {
                    // The Royal Standard's planted black-and-red fire sits behind the boss while
                    // the three Furnace Herald rings build and release.
                    RedKnightVFX.DrawStandardCharge(NPC.Bottom - new Vector2(0f, NPC.gfxOffY),
                        specialAttacks.TelegraphProgress, KnightStandardMode.GreatCenter);
                }
                RedKnightVFX.DrawHerald(NPC.Center, specialAttacks.TelegraphProgress,
                    specialAttacks.Attack == KnightSpecialAttack.StormHerald);
            }
            else if (visible && DominionEngulfOpacity > 0f)
            {
                // Back half of the engulf, anchored on the FEET: the flame technique is bottom-anchored.
                RedKnightVFX.DrawDominionEngulf(NPC.Bottom - new Vector2(0f, NPC.gfxOffY), NPC.scale,
                    DominionEngulfOpacity * 0.95f, front: false);
            }

            // The base draws the puppet (and the Spectral Hand Barrage's Ultrakill seal).
            return base.PreDraw(spriteBatch, screenPos, drawColor);
        }

        public override void PostDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            base.PostDraw(spriteBatch, screenPos, drawColor);

            // Front half of the engulf, over the body and the planted lance, much lighter so the knight stays readable.
            tsorcRevampGlobalNPC globalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            bool visible = !specialAttacks.IsRedTeleportHidden && globalNPC.TeleportCountdown <= 0 && globalNPC.TeleportAppearanceTimer <= 0;
            if (visible && DominionEngulfOpacity > 0f)
            {
                RedKnightVFX.DrawDominionEngulf(NPC.Bottom - new Vector2(0f, NPC.gfxOffY), NPC.scale,
                    DominionEngulfOpacity * 0.42f, front: true);
            }
        }
        #endregion
    }
}

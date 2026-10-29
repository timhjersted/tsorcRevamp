using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;
using tsorcRevamp.Content.Items;
using tsorcRevamp.Content.Items.Accessories.Mobility;
using tsorcRevamp.Content.Items.Armor.BlackKnightSet;
using tsorcRevamp.Content.Items.Weapons.Classless;
using tsorcRevamp.Content.Items.Weapons.Enemy;
using tsorcRevamp.Content.Items.Weapons.Melee.Spears;
using tsorcRevamp.Content.Items.Weapons.Throwing;
using tsorcRevamp.Content.Projectiles;
using tsorcRevamp.Content.Projectiles.Enemy;
using tsorcRevamp.Content.Projectiles.Enemy.Weapons;
using tsorcRevamp.NPCs.Puppets;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.NPCs.Enemies
{
    /// <summary>
    /// The Black Knight as a PuppetNPC: "puppet body, Black brain". BlackKnight.Attacks.cs is still the only author of
    /// his special attacks; each one runs inside one AttackPhase.Custom, so the puppet body, SF4 movement and the phase
    /// snapshot carry it. His melee is the Black Blade combo pool below; the thrown spear is his ranged weapon.
    /// See tsorcDocs/BlackKnightPuppetConversion_Proposal.md.
    /// </summary>
    // Partial with BlackKnight.Attacks.cs: the attacks need direct access to this class's gravefall wave state, the
    // pose hand positions and the spawn helpers, and no other enemy runs this state machine.
    [AutoloadBossHead]
    public partial class BlackKnight : PuppetNPC, IHumanoidMeleeHitEffects, ISpearMeleeWielder
    {
        // PuppetNPC points Texture at the shared puppet placeholder, so [AutoloadBossHead]'s
        // Texture + "_Head_Boss" convention would look for "PuppetPlaceholder_Head_Boss" and crash at load.
        public override string BossHeadTexture => UsefulFunctions.RefactorableFilepath(typeof(BlackKnight)) + "_Head_Boss";

        #region Defaults
        public int redKnightsSpearDamage = 15;
        public int redMagicDamage = 14;
        public int redKnightsGreatDamage = 18;
        Vector2 storedPlayerPosition = Vector2.Zero;
        public int framesSinceStoredPosition = 0;

        // Black rain barrage state — see StartGravefallBarrage. Self-contained on its own 90-tick
        // timer, so a barrage can run 2-3 waves independent of whatever attack is running.
        int gravefallWavesRemaining;
        int gravefallWaveTimer;
        int gravefallWaveIndex;
        bool gravefallWide;
        const int GravefallWaveCooldown = 90;

        // A brain attack ends itself (EndAttack); this cap only stops a stuck Custom phase from living forever.
        const int AttackPhaseCapTicks = 3600;

        public Player player => Main.player[NPC.target];

        protected override string InvaderTitle => "Black Knight";
        public override bool UsesPuppetDifficultyScaling => false;
        protected override bool AnnounceInvasion => false;
        protected override bool AnnounceInvaderDefeat => false;
        protected override int EstusChargesMax => 0;
        protected override int CasualStrollChance => 0;

        // Pure black under the armour, so any skin pixel the armour sheets leave uncovered reads as shadow.
        protected override Color PuppetSkinColor => Color.Black;

        // Black Knight set, tinted to polished steel with Reflective Metal Dye.
        protected override int HeadArmorItemType => ModContent.ItemType<BlackKnightHelmet>();
        protected override int BodyArmorItemType => ModContent.ItemType<BlackKnightBreastplate>();
        protected override int LegsArmorItemType => ModContent.ItemType<BlackKnightGreaves>();
        protected override int HeadArmorDyeItemType => ItemID.ReflectiveMetalDye;
        protected override int BodyArmorDyeItemType => ItemID.ReflectiveMetalDye;
        protected override int LegsArmorDyeItemType => ItemID.ReflectiveMetalDye;

        protected override int MeleeWeaponItemType => ModContent.ItemType<BlackBlade>();
        protected override int RangedWeaponItemType => -1;
        protected override int MeleeDamage => redKnightsSpearDamage;
        protected override int RangedDamage => 0;
        protected override void DoMeleeAttack() { } // All melee is the combo pool below.
        protected override void DoRangedAttack() { }

        // Slow and hulking: walks noticeably slower than the Red Knight (1.8).
        protected override float TopSpeed => 1.5f;
        protected override float Acceleration => 0.05f;
        protected override float PuppetJumpPower => 11f;
        protected override float PuppetJumpBoost => 6f;

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[Type] = 1;
            NPCID.Sets.TrailCacheLength[Type] = 4;
            NPCID.Sets.TrailingMode[Type] = 1;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Confused] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.OnFire] = true;
        }

        public override void SetDefaults()
        {
            NPC.npcSlots = 7;
            NPC.aiStyle = -1;
            NPC.width = 20;
            NPC.height = 46;
            NPC.damage = 0; // puppet convention: only the sword, the hitboxes and the projectiles hurt
            NPC.defense = 21;
            NPC.lifeMax = 1000;
            NPC.value = 5000;

            if (Main.hardMode)
            {
                NPC.lifeMax = 2000;
                NPC.defense = 50;
                NPC.value = 16000; // life / 1.25
                redKnightsGreatDamage = 21;
                redKnightsSpearDamage = 23;
                redMagicDamage = 19;
            }
            if (tsorcRevampWorld.SuperHardMode)
            {
                NPC.lifeMax = 4000;
                NPC.defense = 75;
                NPC.value = 16000; // life / 2.5
                redKnightsGreatDamage = 38;
                redKnightsSpearDamage = 32;
                redMagicDamage = 26;
            }

            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.lavaImmune = true;

            NPC.knockBackResist = 0.25f; // poise flinch dial: × PoiseFlinchFactor(0.4) ≈ 0.14 of full knockback per ordinary hit
            Banner = NPC.type;
            BannerItem = ModContent.ItemType<Banners.BlackKnightBanner>();

            // Identity: a slow, hulking plague-mage knight whose threat is commitment and area denial, not speed.
            tsorcRevampGlobalNPC blackKnightGlobalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            blackKnightGlobalNPC.Agility = 0.25f;
            blackKnightGlobalNPC.Aggression = 1f;
            blackKnightGlobalNPC.Patience = 2f;
            blackKnightGlobalNPC.CanTeleport = true;
            blackKnightGlobalNPC.TeleportStyle = NPCs.TeleportStyle.Aggressive;
            blackKnightGlobalNPC.TeleportVisualStyle = NPCs.TeleportVisualStyle.Plague;
            // 30 + SmokeFireTeleportSnapTicks (30) hidden, then he emerges with ~2s of the plague cloud still going.
            blackKnightGlobalNPC.TeleportTelegraphTime = 30;
            // Only the blink: a heavy thing does not scramble out of the way, it removes itself by magic.
            blackKnightGlobalNPC.EvasiveTeleportAway = true;

            // Poise: sturdier than the Red Knight (40). OnStagger below clears the brain's own state.
            blackKnightGlobalNPC.PoiseMax = 60f;

            blackKnightGlobalNPC.MaxJumpPower = 11f;
            blackKnightGlobalNPC.NavSearchRadius = 80;
            blackKnightGlobalNPC.CanUseRopes = true;
            blackKnightGlobalNPC.MaxJumpBoost = 7f;
            blackKnightGlobalNPC.CanDoubleJump = false;
            blackKnightGlobalNPC.RemembersLastKnownPos = true;
            // Spacing band (tiles). Every retreat drops a curse patch (RunPlagueTrail), so backing off is zoning.
            blackKnightGlobalNPC.KiteRangeMin = 2f;
            blackKnightGlobalNPC.KiteRangeMax = 15f;
            blackKnightGlobalNPC.KiteLooseness = 0.6f;
        }
        #endregion

        #region Spawn
        public override float SpawnChance(NPCSpawnInfo spawnInfo)
        {
            if (spawnInfo.Player.townNPCs > 1f) return 0f;
            if (!spawnInfo.Player.ZoneMeteor && !spawnInfo.Player.ZoneDungeon && !(spawnInfo.Player.ZoneCorrupt || spawnInfo.Player.ZoneCrimson) && spawnInfo.Player.ZoneOverworldHeight && NPC.downedBoss3 && !Main.dayTime && Main.rand.NextBool(250)) return 1;
            if (Main.hardMode && spawnInfo.Player.ZoneDungeon && Main.rand.NextBool(100)) return 1;
            if (Main.hardMode && !(spawnInfo.Player.ZoneCorrupt || spawnInfo.Player.ZoneCrimson) && !spawnInfo.Player.ZoneBeach && Main.rand.NextBool(250)) return 1;
            if (Main.hardMode && spawnInfo.Player.ZoneUnderworldHeight && Main.rand.NextBool(160)) return 1;
            if (tsorcRevampWorld.SuperHardMode && spawnInfo.Player.ZoneDungeon && Main.rand.NextBool(100)) return 1;

            return 0;
        }
        #endregion

        #region Melee combos
        const string GraveCutName = "Grave Cut";
        const string PlagueReaperName = "Plague Reaper";
        const string ChargingThrustName = "Charging Thrust";

        // ── Black Blade combos (Dark Knight's sword template, slower: he is the heavy one) ─────────────
        // Weighted hit windows end when angular speed drops below 30% of peak: (in + out * ln(1/0.3) / k).
        //
        // Move             Tell  Strike curve     Envelope / live   Live   Pause  Recovery
        // Grave Cut         40   8 in / 30 out k7  225° / ~180°     13t     -      36t
        // Plague Reaper     36   8 / 26 k6, then   225° each        13t     10     38t
        //                        8 / 32 k6 return (conditional)     16t
        // Charging Thrust   50   30t distance-solved dash, blade level   6..19 (13t)  -  42t
        //
        // Grave Cut: one overhead cut from behind the head to straight down (-1.55 -> 2.38). The tell holds the carry,
        //   then raises into the cock. 13t live = rollable. Counter: roll through, or step out of 120px during the tell.
        // Plague Reaper: the same overhead, then an underhand return along the same two poses (shared endpoints, no
        //   snap). Step 1's live window ends 13t in; the tail (21t), pause (10t) and linger (5t) put step 2's live
        //   start 36t after it (>= 30: a late roller can roll again). Step 2 only plays on a hit, or if the target is
        //   still in front within the return cut's reach. Rolls needed: 1-2.
        // Charging Thrust: 130-300px, RangedStartOnly. The sword is held straight out, arm level, and the BODY carries
        //   the reach (no spear punch). The dash locks 10t before release: gap + 150px overshoot, 96-420px, on Crystal
        //   Knight's DashProgress curve; live 6..19 (13t). Aborts harmlessly if the floor or a wall breaks the path.
        //   Counter: roll through; rolling away fails (it overshoots ~150px past where the target stood).
        const int GraveCutTell = 40;
        const int ReaperTell = 36;
        const int ReaperPause = 10;
        const int ChargeTell = 50;
        const int ChargeAttackTicks = 30;
        const int ChargeLiveStartTick = 6;
        const int ChargeLiveEndTick = 19;
        const int ChargeLockLeadTicks = 10;
        const float ChargeOvershootPixels = 150f;
        const float ChargeMinimumDistance = 96f;
        const float ChargeMaximumDistance = 420f;
        const float CloseComboMaximumRange = 120f;
        const float ChargeMinimumRange = 130f;
        const float ChargeMaximumRange = 300f;
        // Swing space: 0 = the sprite's 45-degree diagonal, so a level blade is +PiOver4.
        const float CutCockedSwingPose = -1.55f;
        const float CutEndSwingPose = 2.38f;
        const float CutCarrySwingPose = -0.30f;
        const float LevelSwingPose = MathHelper.PiOver4;
        const float SwordReachMult = 1.16f;

        static MeleeComboStep WeightedSwordStep(ComboMotion motion, int telegraphTicks, int easeInTicks, int easeOutTicks,
            float easeOutDecay, int pauseAfter, float damageMult)
        {
            int attackTicks = easeInTicks + easeOutTicks;
            float tailTicks = easeOutTicks * MathF.Log(1f / 0.3f) / easeOutDecay;
            float hitWindowEnd = MathHelper.Clamp((easeInTicks + tailTicks) / attackTicks, 0f, 1f);
            return new MeleeComboStep
            {
                Motion = motion,
                TelegraphTicks = telegraphTicks,
                AttackTicks = attackTicks,
                PostStepPause = pauseAfter,
                DamageMult = damageMult,
                ReachMult = SwordReachMult,
                ForwardPushMult = 0.15f,
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

        static readonly MeleeCombo[] BlackKnightCombos =
        {
            new MeleeCombo
            {
                Name = GraveCutName, BaseWeight = 100, Preferred = ComboRangeBand.Close,
                InitialFlashColor = new Color(120, 60, 170), CooldownAfterUse = 70, RecoveryTicks = 36, MoveBrake = 0.24f,
                Steps = new[]
                {
                    WeightedSwordStep(ComboMotion.OverheadArc, GraveCutTell, 8, 30, 7f, 0, 1f),
                },
            },
            new MeleeCombo
            {
                Name = PlagueReaperName, BaseWeight = 80, Preferred = ComboRangeBand.Close,
                InitialFlashColor = new Color(120, 60, 170), CooldownAfterUse = 130, RecoveryTicks = 38, MoveBrake = 0.22f,
                Steps = new[]
                {
                    WeightedSwordStep(ComboMotion.OverheadArc, ReaperTell, 8, 26, 6f, ReaperPause, 0.85f),
                    WeightedSwordStep(ComboMotion.UnderhandArc, 0, 8, 32, 6f, 0, 1.05f),
                },
            },
            new MeleeCombo
            {
                Name = ChargingThrustName, BaseWeight = 100, Preferred = ComboRangeBand.Mid,
                InitialFlashColor = new Color(120, 60, 170), RangedStartOnly = true, CooldownAfterUse = 150,
                RecoveryTicks = 42, MoveBrake = 0f,
                Steps = new[]
                {
                    // The dash is authored (OnMeleeComboAttackTick), so no constant push.
                    new MeleeComboStep
                    {
                        Motion = ComboMotion.JoustDash, TelegraphTicks = ChargeTell, AttackTicks = ChargeAttackTicks,
                        DamageMult = 1.3f, ReachMult = SwordReachMult, ForwardPushMult = 0f, SwingSpeedMult = 1f,
                        Ease = SwingEaseStyle.Smooth,
                        HitWindowEnd = ChargeLiveEndTick / (float)ChargeAttackTicks,
                        LeapHeightMult = 1f, LeapForwardSpeedMult = 1f,
                    },
                },
            },
        };

        // Charging Thrust's locked dash (server decides at the lock tick; synced).
        float _chargeDistance;
        int _chargeDirection = 1;
        // This tick's tile-clipped dash velocity, re-asserted after the base AI so SF4 can't overwrite it.
        Vector2 _chargeVelocity;

        protected override MeleeCombo[] MeleeComboPoolOverride => BlackKnightCombos;
        // No legacy slash path: MeleeRange 0 leaves the combo pool as the only melee.
        protected override float MeleeRange => 0f;
        protected override int MeleeComboChance => 100;
        // Rolled per neutral tick from 130-300px, so the charge comes out after ~25t of walking in, not instantly.
        protected override int RangedStartMeleeComboChance => 4;
        // BlackBlade.png measured: 62x62, pommel at (0,61), tip at (61,0), crossguard across y 44-55. The fist sits
        //   mid-handle at (5,56), so grip-to-tip is ~79px at scale 1; ComboReachBase 97 * 0.7 * 1.16 = 79px matches it.
        protected override float ComboReachBase => 97f;
        protected override float MeleeWeaponDrawScale => 1f;
        protected override Vector2 MeleeHandleNorm => new Vector2(5f / 62f, 56f / 62f);
        protected override float MeleeEngageRange => CloseComboMaximumRange;
        protected override float ComboMaxStartRange => ChargeMinimumRange;
        protected override float RangedStartComboMaxRange => ChargeMaximumRange;
        // Authored tells play as written (no 1.35x stretch); the 20t floor never binds (shortest tell is 36t).
        protected override float ComboTelegraphMultiplier => 1f;
        protected override int MinComboTelegraphTicks => 20;
        protected override int TelegraphFacingCommitTicks => 10;
        protected override bool UseLogicalMeleeTelegraphs => true;
        protected override int MeleeComboInterStepLingerTicks => 5;
        protected override int MeleeRecoveryLingerTicks => 8;
        protected override WeaponArchetype MeleeArchetype => WeaponArchetype.Broadsword;
        protected override bool UseCompositeArmSwing => true;
        protected override bool MirrorMeleeSwingRotationByFacing => true;
        protected override bool UseSwingEasing => true;
        protected override bool UseAuthoredComboSwingClock => true;
        protected override bool AuthoredClockCoversJoustDash => true;
        protected override bool HasSlashVFX => false;
        protected override float MeleeBladeWidth => 20f;

        // Brain attacks own velocity (throw hops, ward advance, leaps, the counter dash); the puppet must not brake them.
        protected override bool SlowDownDuringCustom => false;

        protected override void ModifyMeleeArcEndpoints(ComboMotion motion, ref float startRotation, ref float endRotation)
        {
            bool comboPoseActive = Phase == AttackPhase.MeleeComboTelegraph || Phase == AttackPhase.MeleeComboAttack
                || Phase == AttackPhase.MeleeComboPause || Phase == AttackPhase.MeleeComboRecovery;
            if (!comboPoseActive)
            {
                return;
            }

            if (motion == ComboMotion.JoustDash)
            {
                // Point-first for the whole dash and its recovery: the stab is the body moving, not the arm.
                startRotation = LevelSwingPose;
                endRotation = LevelSwingPose;
            }
            else if (motion == ComboMotion.OverheadArc)
            {
                startRotation = CutCockedSwingPose;
                endRotation = CutEndSwingPose;
                // The tell raises straight from the carry into the cock instead of retracing the live arc.
                if (Phase == AttackPhase.MeleeComboTelegraph)
                {
                    endRotation = CutCarrySwingPose;
                }
            }
            else if (motion == ComboMotion.UnderhandArc)
            {
                // Plague Reaper's return: the overhead's two poses in reverse, so the pause is a pure hold.
                startRotation = CutEndSwingPose;
                endRotation = CutCockedSwingPose;
            }
        }

        protected override bool CanDamageWithMeleeStep(MeleeComboStep step, float progress)
        {
            if (ActiveMeleeComboName != ChargingThrustName)
            {
                return true;
            }

            // A charge whose path broke at the lock tick plays out harmlessly; the first 6t are the dash spinning up.
            if (_chargeDistance <= 0f)
            {
                return false;
            }
            return progress >= ChargeLiveStartTick / (float)step.AttackTicks;
        }

        protected override bool CanSelectMeleeCombo(MeleeCombo combo, float distance, float healthFraction)
        {
            if (combo.Name == ChargingThrustName)
            {
                bool inBand = distance >= ChargeMinimumRange && distance <= ChargeMaximumRange;
                return inBand && NPC.HasValidTarget && CanChargeTo(Main.player[NPC.target]);
            }

            return distance <= CloseComboMaximumRange;
        }

        protected override bool ShouldContinueMeleeCombo(string comboName, int nextStepIndex, Player target, bool previousStepHit)
        {
            if (!target.active || target.dead)
            {
                return false;
            }

            if (comboName == PlagueReaperName)
            {
                bool inFront = Math.Sign(target.Center.X - NPC.Center.X) == NPC.direction;
                float returnCutReach = ComboReachBase * SwordReachMult + 34f;
                bool insideReturnCut = NPC.Distance(target.Center) <= returnCutReach;
                return previousStepHit || (inFront && insideReturnCut);
            }

            return true;
        }

        // Path check for the charge: open air at body height and floor under every 16px of it, out to the overshoot.
        bool CanChargeTo(Player target)
        {
            int direction = Math.Sign(target.Center.X - NPC.Center.X);
            if (direction == 0)
            {
                direction = NPC.direction;
            }

            float travel = Math.Abs(target.Center.X - NPC.Center.X) + ChargeOvershootPixels;
            travel = MathHelper.Clamp(travel, ChargeMinimumDistance, ChargeMaximumDistance);
            for (float x = 16f; x <= travel; x += 16f)
            {
                Vector2 position = NPC.position + new Vector2(direction * x, 0f);
                bool blocked = Collision.SolidCollision(position, NPC.width, NPC.height);
                bool floored = Collision.SolidCollision(position + new Vector2(0f, NPC.height), NPC.width, 20);
                if (blocked || !floored)
                {
                    return false;
                }
            }
            return true;
        }

        protected override void OnMeleeComboTelegraphTick(MeleeCombo combo, MeleeComboStep step, int elapsed, int total)
        {
            if (combo.Name != ChargingThrustName)
            {
                return;
            }

            // Lock the dash 10t before release; a path that broke during the tell aborts into a harmless recovery.
            if (elapsed == total - ChargeLockLeadTicks && Main.netMode != NetmodeID.MultiplayerClient && NPC.HasValidTarget)
            {
                Player target = Main.player[NPC.target];
                _chargeDirection = Math.Sign(target.Center.X - NPC.Center.X);
                if (_chargeDirection == 0)
                {
                    _chargeDirection = NPC.direction;
                }

                _chargeDistance = 0f;
                if (CanChargeTo(target))
                {
                    float travel = Math.Abs(target.Center.X - NPC.Center.X) + ChargeOvershootPixels;
                    _chargeDistance = MathHelper.Clamp(travel, ChargeMinimumDistance, ChargeMaximumDistance);
                }
                NPC.netUpdate = true;
                RequestNetworkSnapshot();
            }

            // Plague embers gathering on the blade tip for the last 30t of the tell.
            if (!Main.dedServ && elapsed >= total - 30 && elapsed % 2 == 0)
            {
                Vector2 tip = PuppetWeaponTipPosition(ComboReachBase * 0.7f * SwordReachMult);
                Dust glint = Dust.NewDustPerfect(tip + Main.rand.NextVector2Circular(3f, 3f), DustID.ShadowbeamStaff,
                    Main.rand.NextVector2Circular(0.6f, 0.6f), 80, default, Main.rand.NextFloat(1f, 1.4f));
                glint.noGravity = true;
            }
        }

        protected override void OnMeleeComboAttackTick(MeleeCombo combo, MeleeComboStep step, int elapsed, int total)
        {
            if (combo.Name != ChargingThrustName || _chargeDistance <= 0f)
            {
                return;
            }

            // Distance-solved dash: this tick's share of the locked length on the 30t accelerate-cruise-decay curve.
            float progressNow = SuperHardMode.CrystalKnight.DashProgress(elapsed);
            float progressNext = SuperHardMode.CrystalKnight.DashProgress(elapsed + 1);
            float speed = _chargeDistance * (progressNext - progressNow);
            Vector2 desired = new Vector2(_chargeDirection * speed, 0f);
            _chargeVelocity = Collision.TileCollision(NPC.position, desired, NPC.width, NPC.height);
            NPC.velocity.X = _chargeVelocity.X;
            if (_chargeVelocity.X != 0f)
            {
                AfterimageTicks = 3;
            }
        }
        #endregion

        #region Attack poses
        // ── Brain attack poses ───────────────────────────────────────────────────────────────────────
        // Every held prop is in the FRONT hand (the puppet only poses the back arm for a second weapon); the back arm
        // is a free palm for casts. Angles are FACING space: 0 = straight ahead, negative = up, mirrored for a
        // left-facing puppet; RedKnightPuppet.SwingPoseFor turns them into the swing pose (+45 degrees, clamped).
        //
        // Spear Throw (release R):  spear appears R-40 | carry | 24t javelin draw-back from R-30 (Red Knight's: arm
        //   back over the shoulder, spear tilted 0.35 rad above the aim, plague glints at the tip) | 6t cubic whip |
        //   leaves the hand at R. Leap Strike's backleap throw uses the same draw-back, compressed into its ascent.
        // Bomb Throw (R 60):  bomb at the hip from R-40, fuse lit | 24t wind-up from R-30, overhand when the solved
        //   arc climbs steeper than 25 degrees, otherwise underhand | 6t whip | thrown at R.
        // Leap Strike:  sword raised overhead over the 24t tell, carried there through the leap | 10t cubic downswing
        //   starting at the solved strike tick | held low through the plant | sword away, spear drawn for the backleap.
        // Counter Check:  20t tell, blade cocked back at the hip | the stab: arm and blade straight out, level, held
        //   through the dash | lowered over the recovery.
        // Palm casts (Homing Volley, Death Seal, Crystal Storm): the back palm rises 24t to 0.45 rad above the aim,
        //   6t push onto it at release, held, lowered over 12t. Black Rain / Death Seal gather: both palms overhead.
        // Curse Ward:  front palm pushed out toward the ward over its 16t rise and held for the 120t lock; the back
        //   palm lifts on the two rain ticks; both sweep outward on the 8t release.
        // Plague Seals:  Amethyst Staff raised 45 degrees for the cast, lifted higher from tick 70, planted butt-down
        //   over 6t on the flash at 90 and held there.
        const int PropShowAheadTicks = 10;
        const int ThrowWindupTicks = 24;
        const int ThrowWhipTicks = 6;
        const float SpearCarryFacing = 0.2f;
        const float ThrowArmDrawBack = 2.565f;
        const float ThrowSpearLift = 0.35f;
        const float ThrowGrip = 0.6f;
        const float ThrowCockRadians = 1.6f;
        const float BombHipFacing = 1.0f;
        const float BombUnderhandFacing = 2.1f;
        const float BombOverhandAboveFacing = -0.45f;
        const float SwordCarryFacing = CutCarrySwingPose - MathHelper.PiOver4;
        const float SwordOverheadFacing = -1.30f - MathHelper.PiOver4;
        // 66 degrees below level: straight down sank ~50px of the 89px blade into the floor on landing (rendered).
        const float SwordStrikeEndFacing = 1.15f;
        const int LeapRaiseTicks = 14;
        const int LeapDownswingTicks = 10;
        // Point near level while the arm draws back: a lower point made the tell read as a small low swing (rendered).
        const float CounterCockFacing = 0.15f;
        const float CounterCockArmOffset = 0.9f;
        const int CounterLowerTicks = 14;
        const int PalmRaiseTicks = 24;
        const int PalmPushTicks = 6;
        const int PalmHoldTicks = 14;
        const int PalmLowerTicks = 12;
        const float PalmHangFacing = 1.4f;
        const float PalmReadyLift = 0.45f;
        const float PalmMinimumFacing = -1.5f;
        const float PalmMaximumFacing = 1.0f;
        // Up-forward, not vertical: the back arm draws under the torso, and a vertical palm hides behind the head.
        const float FrontOverheadFacing = -0.8f;
        const float BackOverheadFacing = -0.95f;
        const float WardPalmFacing = -0.1f;
        const float WardReleaseSpreadFacing = 0.9f;
        const int WardReleaseSweepTicks = 8;
        const int WardRainLiftTicks = 10;
        const float StaffCastFacing = -MathHelper.PiOver4;
        const float StaffLiftFacing = -1.1f;
        // Planted butt-down, gem up: the staff stands vertical while the arm offset drops the fist to forward-low
        //   (+2.1 rad turns the arm from straight up to ~35 degrees below ahead), so the stroke drives the butt down.
        const float StaffPlantFacing = -MathHelper.PiOver2;
        const float StaffPlantArmOffset = 2.1f;
        const int StaffLiftTick = 70;
        const int StaffPlantTicks = 6;

        BlackKnightProp _frontProp;
        float _poseFacing;
        float _poseArmOffset;
        float _poseGrip = ThrowGrip;
        bool _bombFuseLit;
        float? _backPalmFacing;
        float? _frontFreeFacing;
        PuppetWeapon _spearPropWeapon;
        PuppetWeapon _bombPropWeapon;
        PuppetWeapon _staffPropWeapon;

        /// <summary>World position of the front hand, captured before each brain tick. Throws leave from here.</summary>
        internal Vector2 AttackHandWorld { get; private set; }

        /// <summary>World position of the back palm, captured with AttackHandWorld. Palm spells leave from here.</summary>
        internal Vector2 CastHandWorld { get; private set; }

        // The thrown spear is drawn from its own projectile sprite (SpearDrawTexturePath), so hand and flight match.
        PuppetWeapon SpearPropWeapon => _spearPropWeapon ??= new PuppetWeapon(ItemID.Spear, archetype: WeaponArchetype.Halberd);
        // The bomb is drawn in PostDraw; a sprite-hidden identity keeps the composite arm posed under it.
        PuppetWeapon BombPropWeapon => _bombPropWeapon ??= new PuppetWeapon(ItemID.Grenade, hideHeldSprite: true);
        // Amethyst Staff: the vanilla icon is a 45-degree diagonal like the sword; held ~30% up from the butt.
        //   Grip not yet render-measured.
        PuppetWeapon StaffPropWeapon => _staffPropWeapon ??= new PuppetWeapon(ItemID.AmethystStaff, handleNorm: new Vector2(0.28f, 0.72f));

        bool PoseActive => Phase == AttackPhase.Custom && AttackActive;
        bool FrontHandHoldsProp => PoseActive && _frontProp != BlackKnightProp.None;
        protected override bool UseCompositeArmForAdditionalPhase => FrontHandHoldsProp;
        // The Custom phase is entered with the sword as its pose weapon; put it away while the front hand is empty.
        protected override bool WeaponSheathed => PoseActive && !FrontHandHoldsProp;
        protected override float? FreeBackArmFacingAngle => PoseActive ? _backPalmFacing : null;
        protected override float? FreeFrontArmFacingAngle => PoseActive && !FrontHandHoldsProp ? _frontFreeFacing : null;
        // The javelin draw-back and the counter's cock swing the arm off the weapon's line; 0 outside a held prop.
        protected override float MeleeCompositeArmRotationOffset => FrontHandHoldsProp ? _poseArmOffset : 0f;

        protected override float? CustomWeaponRotation
        {
            get
            {
                if (!FrontHandHoldsProp)
                {
                    return null;
                }
                return RedKnightPuppet.SwingPoseFor(_poseFacing);
            }
        }

        // Spear draw only while the throwing spear is in hand; the sword and staff use the normal item draw.
        protected override bool DrawWeaponAsSpear => FrontHandHoldsProp && _frontProp == BlackKnightProp.Spear;
        protected override bool UseAuthoredSpearGrip => true;
        protected override bool MirrorSpearRotationByFacing => true;
        protected override string SpearDrawTexturePath => UsefulFunctions.RefactorableFilepath(typeof(BlackThrowingSpear));
        // BlackThrowingSpear.png measured: 14x84, tip straight up, top opaque row 0 and bottom row 83, both at x 6-7.
        protected override Vector2 SpearTextureSize => new Vector2(14f, 84f);
        protected override Vector2 SpearHeadNorm => new Vector2(0.5f, 0.5f / 84f);
        protected override Vector2 SpearBaseNorm => new Vector2(0.5f, 83.5f / 84f);
        // Swing space puts 0 on the 45-degree diagonal; a tip-up sprite needs +45 to match it.
        protected override float SpearDrawRotationOffset => MathHelper.PiOver4;

        protected override float? OverrideSpearGrip(float swingClockT)
        {
            if (DrawWeaponAsSpear)
            {
                return _poseGrip;
            }
            return null;
        }

        /// <summary>Everything the brain-attack pose reads, as plain synced values, so SwingPreview can drive the same
        /// function offline. Aims are facing-space radians.</summary>
        public struct BlackKnightPoseInput
        {
            public int Attack;
            public int Timer;
            public int SpearRelease;
            public int LeapStrikeStart;
            public float ThrowAim;
            public float BombAim;
            public float CastAim;
        }

        /// <summary>One tick of the brain-attack pose. Facing is the front arm's facing-space angle while it holds a
        /// prop; the palm angles are null when that arm is not gesturing.</summary>
        public struct BlackKnightPose
        {
            public BlackKnightProp FrontProp;
            public float Facing;
            public float ArmOffset;
            public float Grip;
            public bool FuseLit;
            public bool ThrowDrawBack;
            public float? BackPalmFacing;
            public float? FrontFreeFacing;
        }

        /// <summary>World-facing half of the pose: solves this tick's aims from synced state and hands them to
        /// <see cref="ResolveAttackPose"/>. Runs on every machine.</summary>
        void UpdateAttackPose()
        {
            BlackKnightPoseInput input = new BlackKnightPoseInput
            {
                Attack = (int)CurrentAttack,
                Timer = AttackTimer,
                SpearRelease = SpearReleaseTick,
                LeapStrikeStart = leapStrikeStartTick,
            };

            // NPC.Center stands in for the hand so the aim does not feed back through the arm pose it sets.
            Vector2 aimPoint = PredictedAimPoint();
            if (CurrentAttack == BlackKnightAttack.LeapStrike && NPC.HasValidTarget)
            {
                aimPoint = player.Center;
            }
            input.ThrowAim = RedKnightPuppet.FacingAngleOf(aimPoint - NPC.Center, NPC.direction);

            Vector2 bombLaunch = BombLaunchVelocity(NPC.Center);
            input.BombAim = RedKnightPuppet.FacingAngleOf(bombLaunch, NPC.direction);

            Vector2 castTarget = storedPlayerPosition;
            if (castTarget == Vector2.Zero && NPC.HasValidTarget)
            {
                castTarget = player.Center;
            }
            input.CastAim = RedKnightPuppet.FacingAngleOf(castTarget - NPC.Center, NPC.direction);

            BlackKnightPose pose = ResolveAttackPose(input);
            _frontProp = pose.FrontProp;
            _poseFacing = pose.Facing;
            _poseArmOffset = pose.ArmOffset;
            _poseGrip = pose.Grip;
            _bombFuseLit = pose.FuseLit;
            _backPalmFacing = pose.BackPalmFacing;
            _frontFreeFacing = pose.FrontFreeFacing;

            // Plague glints at the raised spear tip through the draw-back, every other tick.
            if (pose.ThrowDrawBack && !Main.dedServ && input.Timer % 2 == 0)
            {
                Dust glint = Dust.NewDustPerfect(PuppetSpearTipPosition, DustID.ShadowbeamStaff,
                    Main.rand.NextVector2Circular(0.5f, 0.5f), 80, default, Main.rand.NextFloat(0.9f, 1.3f));
                glint.noGravity = true;
            }
        }

        // Hang -> ready (SmoothStep) | cubic push onto the aim at release | hold to holdEnd | SmoothStep back down.
        static float StagePalm(int timer, int release, int holdEnd, float ready, float aim)
        {
            int pushStart = release - PalmPushTicks;
            int raiseStart = pushStart - PalmRaiseTicks;
            int lowerStart = holdEnd + PalmHoldTicks;
            if (timer < raiseStart)
            {
                return PalmHangFacing;
            }
            if (timer < pushStart)
            {
                float raise = (timer - raiseStart + 1f) / PalmRaiseTicks;
                return MathHelper.SmoothStep(PalmHangFacing, ready, raise);
            }
            if (timer < release)
            {
                float push = (timer - pushStart + 1f) / PalmPushTicks;
                return MathHelper.Lerp(ready, aim, push * push * push);
            }
            if (timer < lowerStart)
            {
                return aim;
            }
            float lower = MathHelper.Clamp((timer - lowerStart + 1f) / PalmLowerTicks, 0f, 1f);
            return MathHelper.SmoothStep(aim, PalmHangFacing, lower);
        }

        /// <summary>
        /// Picks the front-hand prop with its facing angle and spear grip, plus the free arms' palm angles, from the
        /// attack and its clock. Timings are in the block comment above the pose constants. Pure: public and static so
        /// SwingPreview renders exactly the poses the game plays.
        /// </summary>
        public static BlackKnightPose ResolveAttackPose(BlackKnightPoseInput input)
        {
            int timer = input.Timer;
            BlackKnightAttack attack = (BlackKnightAttack)input.Attack;
            BlackKnightPose pose = new BlackKnightPose
            {
                FrontProp = BlackKnightProp.None,
                Facing = SpearCarryFacing,
                Grip = ThrowGrip,
            };

            float castAim = MathHelper.Clamp(input.CastAim, PalmMinimumFacing, PalmMaximumFacing);

            switch (attack)
            {
                case BlackKnightAttack.SpearThrow:
                {
                    int release = input.SpearRelease;
                    int whipStart = release - ThrowWhipTicks;
                    int windupStart = whipStart - ThrowWindupTicks;
                    int appearTick = windupStart - PropShowAheadTicks;
                    if (timer < appearTick || timer >= release)
                    {
                        return pose;
                    }

                    float aim = input.ThrowAim;
                    float cocked = aim - ThrowSpearLift;
                    pose.FrontProp = BlackKnightProp.Spear;
                    if (timer < windupStart)
                    {
                        return pose;
                    }
                    if (timer < whipStart)
                    {
                        float windup = (timer - windupStart + 1f) / ThrowWindupTicks;
                        pose.Facing = MathHelper.SmoothStep(SpearCarryFacing, cocked, windup);
                        pose.ArmOffset = MathHelper.SmoothStep(0f, -ThrowArmDrawBack, windup);
                        pose.ThrowDrawBack = true;
                        return pose;
                    }

                    // Cubic ease-in: slow out of the cock and fastest on the release frame.
                    float whip = (timer - whipStart + 1f) / ThrowWhipTicks;
                    float eased = whip * whip * whip;
                    pose.Facing = MathHelper.Lerp(cocked, aim, eased);
                    pose.ArmOffset = MathHelper.Lerp(-ThrowArmDrawBack, 0f, eased);
                    return pose;
                }

                case BlackKnightAttack.BombThrow:
                {
                    int release = BombWindupTicks;
                    int whipStart = release - ThrowWhipTicks;
                    int windupStart = whipStart - ThrowWindupTicks;
                    int appearTick = windupStart - PropShowAheadTicks;
                    if (timer < appearTick || timer >= release)
                    {
                        return pose;
                    }

                    float aim = input.BombAim;
                    float cocked = BombUnderhandFacing;
                    if (aim < BombOverhandAboveFacing)
                    {
                        cocked = aim - ThrowCockRadians;
                    }
                    pose.FrontProp = BlackKnightProp.Bomb;
                    pose.FuseLit = true;
                    if (timer < windupStart)
                    {
                        pose.Facing = BombHipFacing;
                    }
                    else if (timer < whipStart)
                    {
                        float windup = (timer - windupStart + 1f) / ThrowWindupTicks;
                        pose.Facing = MathHelper.SmoothStep(BombHipFacing, cocked, windup);
                    }
                    else
                    {
                        float whip = (timer - whipStart + 1f) / ThrowWhipTicks;
                        float eased = whip * whip * whip;
                        pose.Facing = MathHelper.Lerp(cocked, aim, eased);
                    }
                    return pose;
                }

                case BlackKnightAttack.HomingVolley:
                {
                    pose.BackPalmFacing = StagePalm(timer, HomingFirstBurstTick, HomingSecondBurstTick,
                        castAim - PalmReadyLift, castAim);
                    return pose;
                }

                case BlackKnightAttack.CrystalStorm:
                {
                    // Both palms gather overhead, then the back palm pushes the volley out while the front stays up.
                    pose.BackPalmFacing = StagePalm(timer, CrystalStormReleaseTick, CrystalStormReleaseTick,
                        BackOverheadFacing, castAim);
                    pose.FrontFreeFacing = StagePalm(timer, CrystalStormReleaseTick, CrystalStormReleaseTick,
                        FrontOverheadFacing, FrontOverheadFacing);
                    return pose;
                }

                case BlackKnightAttack.BlackRain:
                {
                    // Both arms overhead by the wave at BlackRainWaveTick, held, then lowered over the tail.
                    float up = MathHelper.SmoothStep(0f, 1f, (timer + 1f) / BlackRainWaveTick);
                    if (timer >= BlackRainWaveTick + 10)
                    {
                        up = 1f - MathHelper.SmoothStep(0f, 1f, (timer - BlackRainWaveTick - 10f) / (BlackRainTicks - BlackRainWaveTick - 10f));
                    }
                    pose.BackPalmFacing = MathHelper.Lerp(PalmHangFacing, BackOverheadFacing, up);
                    pose.FrontFreeFacing = MathHelper.Lerp(PalmHangFacing, FrontOverheadFacing, up);
                    return pose;
                }

                case BlackKnightAttack.DeathSeal:
                {
                    // Both palms gather overhead, the front palm swings onto the locked aim at the flash and streams.
                    int streamEnd = DeathSealTelegraphTicks + DeathSealFireTicks;
                    float gather = MathHelper.SmoothStep(0f, 1f, (timer + 1f) / PalmRaiseTicks);
                    float lower = MathHelper.SmoothStep(0f, 1f, (timer - streamEnd + 1f) / PalmLowerTicks);
                    pose.BackPalmFacing = MathHelper.Lerp(PalmHangFacing, BackOverheadFacing, gather * (1f - lower));
                    float frontRaised = MathHelper.Lerp(PalmHangFacing, FrontOverheadFacing, gather);
                    if (timer >= DeathSealFlashTick)
                    {
                        float swing = MathHelper.SmoothStep(0f, 1f, (timer - DeathSealFlashTick + 1f) / 12f);
                        frontRaised = MathHelper.Lerp(FrontOverheadFacing, castAim, swing);
                    }
                    pose.FrontFreeFacing = MathHelper.Lerp(frontRaised, PalmHangFacing, lower);
                    return pose;
                }

                case BlackKnightAttack.PlagueAmbush:
                {
                    // Back palm drawn across the chest through the vanish tell.
                    if (timer <= PlagueAmbushVanishTick)
                    {
                        float draw = MathHelper.SmoothStep(0f, 1f, (timer + 1f) / PlagueAmbushVanishTick);
                        pose.BackPalmFacing = MathHelper.Lerp(PalmHangFacing, 0.3f, draw);
                    }
                    return pose;
                }

                case BlackKnightAttack.LeapStrike:
                {
                    if (timer >= LeapBackTick)
                    {
                        // Sword away, spear drawn for the apex throw: draw-back over the ascent, 6t whip into release.
                        int release = LeapThrowTick;
                        int whipStart = release - ThrowWhipTicks;
                        int windupStart = LeapBackTick;
                        if (timer >= release)
                        {
                            return pose;
                        }

                        float aim = input.ThrowAim;
                        float cocked = aim - ThrowSpearLift;
                        pose.FrontProp = BlackKnightProp.Spear;
                        if (timer < whipStart)
                        {
                            float windup = (timer - windupStart + 1f) / (whipStart - windupStart);
                            pose.Facing = MathHelper.SmoothStep(SpearCarryFacing, cocked, windup);
                            pose.ArmOffset = MathHelper.SmoothStep(0f, -ThrowArmDrawBack, windup);
                            pose.ThrowDrawBack = true;
                            return pose;
                        }

                        float whip = (timer - whipStart + 1f) / ThrowWhipTicks;
                        float eased = whip * whip * whip;
                        pose.Facing = MathHelper.Lerp(cocked, aim, eased);
                        pose.ArmOffset = MathHelper.Lerp(-ThrowArmDrawBack, 0f, eased);
                        return pose;
                    }

                    pose.FrontProp = BlackKnightProp.Sword;
                    int strikeStart = input.LeapStrikeStart;
                    if (strikeStart < 0 || timer < strikeStart)
                    {
                        // Raised overhead over the tell, then carried there through the leap.
                        float raise = MathHelper.SmoothStep(0f, 1f, (timer + 1f) / LeapRaiseTicks);
                        pose.Facing = MathHelper.Lerp(SwordCarryFacing, SwordOverheadFacing, raise);
                        return pose;
                    }

                    // Cubic downswing: the blade is fastest where it reaches the target, then held low.
                    float down = MathHelper.Clamp((timer - strikeStart + 1f) / LeapDownswingTicks, 0f, 1f);
                    pose.Facing = MathHelper.Lerp(SwordOverheadFacing, SwordStrikeEndFacing, down * down * down);
                    return pose;
                }

                case BlackKnightAttack.CounterCheck:
                {
                    pose.FrontProp = BlackKnightProp.Sword;
                    if (timer < CounterStrikeTick)
                    {
                        // Blade drawn back low at the hip, point forward: the stab is coming.
                        float cock = MathHelper.SmoothStep(0f, 1f, (timer + 1f) / CounterStrikeTick);
                        pose.Facing = MathHelper.Lerp(SwordCarryFacing, CounterCockFacing, cock);
                        pose.ArmOffset = MathHelper.Lerp(0f, CounterCockArmOffset, cock);
                        return pose;
                    }

                    // The stab: arm and blade straight out, level, for the dash; lowered over the end of the recovery.
                    int lowerStart = CounterTicks - CounterLowerTicks;
                    float level = MathHelper.Clamp((timer - CounterStrikeTick + 1f) / 3f, 0f, 1f);
                    pose.Facing = MathHelper.Lerp(CounterCockFacing, 0f, level);
                    pose.ArmOffset = MathHelper.Lerp(CounterCockArmOffset, 0f, level);
                    if (timer >= lowerStart)
                    {
                        float lowered = MathHelper.SmoothStep(0f, 1f, (timer - lowerStart + 1f) / CounterLowerTicks);
                        pose.Facing = MathHelper.Lerp(0f, SwordCarryFacing, lowered);
                    }
                    return pose;
                }

                case BlackKnightAttack.CurseWard:
                {
                    float push = MathHelper.SmoothStep(0f, 1f, (timer + 1f) / WardRiseTicks);
                    float front = MathHelper.Lerp(PalmHangFacing, WardPalmFacing, push);
                    float back = PalmHangFacing;
                    int sinceRainA = timer - WardRainTickA;
                    int sinceRainB = timer - WardRainTickB;
                    bool rainLift = (sinceRainA >= -WardRainLiftTicks && sinceRainA < WardRainLiftTicks)
                        || (sinceRainB >= -WardRainLiftTicks && sinceRainB < WardRainLiftTicks);
                    if (rainLift)
                    {
                        back = BackOverheadFacing;
                    }
                    if (timer >= WardFacingLockTicks)
                    {
                        // Release: both arms sweep outward as the volley vents, then hang.
                        float sweep = MathHelper.SmoothStep(0f, 1f, (timer - WardFacingLockTicks + 1f) / WardReleaseSweepTicks);
                        front = MathHelper.Lerp(WardPalmFacing, WardReleaseSpreadFacing, sweep);
                        back = MathHelper.Lerp(PalmHangFacing, -WardReleaseSpreadFacing, sweep);
                    }
                    pose.FrontFreeFacing = front;
                    pose.BackPalmFacing = back;
                    return pose;
                }

                case BlackKnightAttack.PlagueSeals:
                {
                    pose.FrontProp = BlackKnightProp.Staff;
                    pose.Facing = StaffCastFacing;
                    if (timer >= StaffLiftTick && timer < SealCastTicks)
                    {
                        float lift = MathHelper.SmoothStep(0f, 1f, (timer - StaffLiftTick + 1f) / (SealCastTicks - StaffLiftTick));
                        pose.Facing = MathHelper.Lerp(StaffCastFacing, StaffLiftFacing, lift);
                    }
                    else if (timer >= SealCastTicks)
                    {
                        // Planted on the flash: cubic, fastest as the butt strikes the floor.
                        float plant = MathHelper.Clamp((timer - SealCastTicks + 1f) / StaffPlantTicks, 0f, 1f);
                        float eased = plant * plant * plant;
                        pose.Facing = MathHelper.Lerp(StaffLiftFacing, StaffPlantFacing, eased);
                        pose.ArmOffset = MathHelper.Lerp(0f, StaffPlantArmOffset, eased);
                    }
                    return pose;
                }
            }

            return pose;
        }
        #endregion

        #region Hit Logic
        /// <summary>
        /// Curse Ward absorption: projectiles arriving from the warded side are eaten almost entirely.
        /// </summary>
        /// <remarks>
        /// Sidedness is taken from where the projectile CAME FROM (its previous position): by the time a hit registers
        /// it is usually already overlapping him. Runs on the projectile owner's machine, so wardFacing is synced.
        /// Deliberately not total immunity: chip damage still lands.
        /// </remarks>
        public override void ModifyHitByProjectile(Projectile projectile, ref NPC.HitModifiers modifiers)
        {
            base.ModifyHitByProjectile(projectile, ref modifiers);
            if (!CurseWardActive)
            {
                return;
            }

            float approachX = projectile.oldPosition.X + projectile.width * 0.5f - NPC.Center.X;
            bool fromWardedSide = approachX * CurseWardFacing > 0f;
            if (!fromWardedSide)
            {
                return; // flanked — the ward does nothing, which is the intended counterplay
            }

            modifiers.FinalDamage *= 0.12f;
            modifiers.Knockback *= 0f;
            // Charges the release volley in ReleaseWard, from the projectile's base damage: how hard the player committed.
            RegisterWardAbsorption(projectile.damage);
            if (Main.rand.NextBool(3))
            {
                Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCHit4 with { PitchVariance = 0.3f, Volume = 0.6f }, NPC.Center);
            }
        }

        // EvasiveOnHit flags the hit on a client and the hit report carries it to the server, which rolls it.
        public override void OnHitByItem(Player player, Item item, NPC.HitInfo hit, int damageDone)
        {
            base.OnHitByItem(player, item, hit, damageDone);
            tsorcRevampAIs.EvasiveOnHit(NPC, true);
        }

        public override void OnHitByProjectile(Projectile projectile, NPC.HitInfo hit, int damageDone)
        {
            base.OnHitByProjectile(projectile, hit, damageDone);
            tsorcRevampAIs.EvasiveOnHit(NPC, projectile.DamageType == DamageClass.Melee);
        }

        /// <summary>
        /// Poise stagger: the base already dropped the Custom phase and the combo; drop the brain attack and any chain.
        /// Only reachable from a windup — a committed attack is hyper-armoured (GlobalNPC gates stagger on AttackCommitted).
        /// </summary>
        public override void OnStagger(NPC npc)
        {
            base.OnStagger(npc);
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                chainedAttack = BlackKnightAttack.None;
                EndAttack(cooldown: OpenerFailCooldownTicks);
                RequestNetworkSnapshot();
            }
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            base.SendExtraAI(writer);
            writer.Write(storedPlayerPosition.X);
            writer.Write(storedPlayerPosition.Y);
            writer.Write((sbyte)wardFacing);
            writer.Write((byte)throwMovement);
            writer.Write((short)leapStrikeStartTick);
            writer.Write(_chargeDistance);
            writer.Write((sbyte)_chargeDirection);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            base.ReceiveExtraAI(reader);
            storedPlayerPosition = new Vector2(reader.ReadSingle(), reader.ReadSingle());
            wardFacing = reader.ReadSByte();
            throwMovement = (BlackThrowMovement)reader.ReadByte();
            leapStrikeStartTick = reader.ReadInt16();
            _chargeDistance = reader.ReadSingle();
            _chargeDirection = reader.ReadSByte();
        }
        #endregion

        #region AI
        public override void AI()
        {
            tsorcRevampGlobalNPC globalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            _chargeVelocity = Vector2.Zero;
            framesSinceStoredPosition++;
            bool isServer = Main.netMode != NetmodeID.MultiplayerClient;

            // Passive, server-side: cooldowns, the retreat curse trail and the 90t black rain waves outlive their attack.
            if (isServer)
            {
                if (counterCooldown > 0)
                {
                    counterCooldown--;
                }
                if (!AttackActive && attackCooldown > 0)
                {
                    attackCooldown--;
                }
                if (NPC.HasValidTarget)
                {
                    RunPlagueTrail(globalNPC);
                }
                RunGravefallWaves();
            }

            // Server picks the next brain attack from neutral and hosts it in a Custom phase. The melee combos are picked
            // by the base from the same neutral, inside 120px (no brain attack is in range there except the counter).
            bool neutral = Phase == AttackPhase.Idle || Phase == AttackPhase.CasualStroll || Phase == AttackPhase.ClosingDistance;
            if (isServer && neutral && NPC.HasValidTarget && !AttackActive && !InFinisherPunishWindow)
            {
                bool hasPlayerLOS = Collision.CanHitLine(NPC.Center, 1, 1, player.Center, 1, 1);
                bool started = TryStartAttack(globalNPC, hasPlayerLOS);
                if (started)
                {
                    StartCustomAttack(AttackPhaseCapTicks, MeleeWeaponItemType, swingPose: false);
                    NPC.netUpdate = true;
                    RequestNetworkSnapshot();
                }
            }

            base.AI();

            // The charge set its velocity inside the combo tick; movement code after it in the base tick must not win.
            if (_chargeVelocity != Vector2.Zero)
            {
                NPC.velocity.X = _chargeVelocity.X;
            }

            // A stagger or the safety cap ended the Custom phase under a live attack: drop the attack.
            if (isServer && AttackActive && Phase != AttackPhase.Custom)
            {
                chainedAttack = BlackKnightAttack.None;
                EndAttack(cooldown: OpenerFailCooldownTicks);
                RequestNetworkSnapshot();
            }

            // A throw or cast swapped the front hand; give the melee combos their sword back.
            if (!PoseActive)
            {
                _frontProp = BlackKnightProp.None;
                EquipWeapon(PuppetHandSlot.Front, DefaultFrontWeapon);
            }

            if (PoseActive)
            {
                // The base cleared both flags for Custom at the end of its tick; the brain's tell/commit split wins.
                globalNPC.AttackCommitted = InCommittedAttack(globalNPC);
                globalNPC.AttackTelegraphing = InAttackTelegraph(globalNPC);
                DebugAttackLabel = CurrentAttackDebugName;
            }
            else
            {
                DebugAttackLabel = null;
            }
            Lighting.AddLight(NPC.Center, Color.GhostWhite.ToVector3() * 0.6f);

            if (!Main.dedServ && Main.rand.NextBool(1500))
            {
                Terraria.Audio.SoundEngine.PlaySound(new Terraria.Audio.SoundStyle("tsorcRevamp/Sounds/DarkSouls/ominous-creature2") with { Volume = 0.8f }, NPC.Center);
            }
        }

        protected override bool TeleportCancelsAttack => false;

        protected override void RunMovementAI(float speedMult)
        {
            // The brain drives velocity for the whole attack, including its throw hops, leaps and dashes.
            if (Phase == AttackPhase.Custom)
            {
                return;
            }

            // attackRange is SF4's engage/re-aggro radius, not a weapon reach: he throws and casts out to ~900px.
            SmartFighter4AI.Run(NPC, topSpeed: TopSpeed * speedMult, acceleration: Acceleration,
                doorBreakingDamage: 4, attackRange: 700f);
        }

        protected override void DoCustomTick(int ticksRemaining)
        {
            if (!NPC.HasValidTarget)
            {
                return;
            }

            // Pose from the same clock the brain is about to process, so a release tick shows the release pose. The hand
            // positions are last tick's (the arm is posed after this, in TickWeaponAnim); one tick is invisible.
            AttackHandWorld = PuppetHandPosition;
            CastHandWorld = PuppetBackHandPosition;
            UpdateAttackPose();
            EquipPoseWeapon();

            // The brain marks netUpdate at its branch points. netSpam can hold a plain netUpdate for up to 30t, so turn
            // each new request into a direct snapshot.
            tsorcRevampGlobalNPC globalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            bool hasPlayerLOS = Collision.CanHitLine(NPC.Center, 1, 1, player.Center, 1, 1);
            bool netUpdateBefore = NPC.netUpdate;
            TickAttacks(globalNPC, hasPlayerLOS);
            if (!netUpdateBefore && NPC.netUpdate)
            {
                RequestNetworkSnapshot();
            }

            // EndAttack ran inside the tick: end the Custom phase on this same tick (the base decrements to 0).
            if (!AttackActive)
            {
                PhaseTimer = 1;
            }
        }

        // Puts this tick's prop in the front hand and makes it the displayed item. The base re-asserts the Custom pose
        // weapon (the sword) before DoCustomTick every tick, so this runs after it and wins.
        void EquipPoseWeapon()
        {
            PuppetWeapon weapon = DefaultFrontWeapon;
            int displayType = MeleeWeaponItemType;
            if (_frontProp == BlackKnightProp.Spear)
            {
                weapon = SpearPropWeapon;
                displayType = ItemID.Spear;
            }
            else if (_frontProp == BlackKnightProp.Bomb)
            {
                weapon = BombPropWeapon;
                displayType = ItemID.Grenade;
            }
            else if (_frontProp == BlackKnightProp.Staff)
            {
                weapon = StaffPropWeapon;
                displayType = ItemID.AmethystStaff;
            }

            EquipWeapon(PuppetHandSlot.Front, weapon);
            SetDisplayWeapon(displayType, swing: false);
        }
        #endregion

        #region Debuffs
        public void OnHumanoidMeleeHit(Player target)
        {
            target.AddBuff(ModContent.BuffType<BrokenSpirit>(), 600, false);
            target.AddBuff(BuffID.BrokenArmor, 600, false);
            target.AddBuff(ModContent.BuffType<CurseBuildup>(), 18000, false);
            target.AddBuff(ModContent.BuffType<GrappleMalfunction>(), 30 * 60, false);
        }
        #endregion

        #region Draw
        static Texture2D bombTexture;
        // EnemyMoonfuryBomb is 34x34; held at 0.6 in the fist, fuse along the arm.
        const float HeldBombScale = 0.6f;

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            // The plague teleport hides him with alpha 255; the puppet draw ignores alpha.
            if (NPC.alpha >= 255)
            {
                return false;
            }
            return base.PreDraw(spriteBatch, screenPos, drawColor);
        }

        public override void PostDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            base.PostDraw(spriteBatch, screenPos, drawColor);
            if (NPC.alpha >= 255)
            {
                return;
            }

            // Curse Ward plane, anchored to the locked facing so it visibly stays on the committed side.
            float wardRise = CurseWardRise;
            if (wardRise > 0.001f)
            {
                EnemyVFX.DrawBlackKnightCurseWard(NPC.Center, CurseWardFacing, wardRise, 0.9f * wardRise);
            }

            if (!PoseActive)
            {
                return;
            }

            // Death Seal's gathering sigil, and the aim thread from the front palm once it is locked.
            if (CurrentAttack == BlackKnightAttack.DeathSeal)
            {
                int timer = AttackTimer;
                EnemyVFX.DrawBlackKnightDeathSeal(NPC.Center, MathHelper.Clamp(timer / (float)DeathSealTelegraphTicks, 0f, 1f));
                if (timer >= DeathSealFlashTick && storedPlayerPosition != Vector2.Zero)
                {
                    float threadProgress = (timer - DeathSealFlashTick) / (float)(DeathSealTelegraphTicks - DeathSealFlashTick);
                    EnemyVFX.DrawBlackKnightAimThread(PuppetHandPosition, storedPlayerPosition, MathHelper.Clamp(threadProgress, 0f, 1f));
                }
            }

            // Shadow Crystal Storm: the hex crystal forms between the raised palms through the tell.
            if (CurrentAttack == BlackKnightAttack.CrystalStorm && AttackTimer < CrystalStormReleaseTick)
            {
                Vector2 palms = (PuppetHandPosition + PuppetBackHandPosition) * 0.5f;
                float stormProgress = MathHelper.Clamp(AttackTimer / (float)CrystalStormReleaseTick, 0f, 1f);
                EnemyVFX.DrawBlackKnightHexCrystal(palms, Vector2.Zero, stormProgress, false);
            }

            if (_frontProp == BlackKnightProp.Bomb)
            {
                bombTexture ??= ModContent.Request<Texture2D>(UsefulFunctions.RefactorableFilepath(typeof(EnemyMoonfuryBomb)),
                    ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;

                // The arm's world angle: facing space mirrored back for a left-facing puppet.
                float armWorldAngle = _poseFacing;
                if (NPC.direction < 0)
                {
                    armWorldAngle = MathHelper.Pi - _poseFacing;
                }

                Vector2 handWorld = PuppetHandPosition;
                float rotation = armWorldAngle + MathHelper.PiOver2;
                Vector2 origin = bombTexture.Size() * 0.5f;
                spriteBatch.Draw(bombTexture, handWorld - Main.screenPosition, null, drawColor,
                    rotation, origin, HeldBombScale, SpriteEffects.None, 0f);
                if (_bombFuseLit)
                {
                    float fuseProgress = MathHelper.Clamp(AttackTimer / (float)BombWindupTicks, 0f, 1f);
                    EnemyVFX.SpawnBombFuseSparks(handWorld, fuseProgress);
                }
            }
        }
        #endregion

        #region Gore
        public override void OnKill()
        {
            // create unknown embers that fill the explosion's radius
            for (int i = 0; i < 30; i++)
            {
                float velX = 1f - ((float)Main.rand.Next(20)) / 5f;
                float velY = 1f - ((float)Main.rand.Next(20)) / 5f;
                velX *= 2f;
                velY *= 2f;
                Dust.NewDust(new Vector2(NPC.position.X - (float)(NPC.width / 2), NPC.position.Y - (float)(NPC.height / 2)), NPC.width, NPC.height, DustID.EnchantedNightcrawler, velX, velY, 160, default, 1.5f);
                Dust.NewDust(new Vector2(NPC.position.X - (float)(NPC.width / 2), NPC.position.Y - (float)(NPC.height / 2)), NPC.width, NPC.height, DustID.CosmicEmber, velX, velY, 160, default, 1.5f);
            }

            if (!Main.dedServ)
            {
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Black Knight Gore 1").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Black Knight Gore 2").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Black Knight Gore 3").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Black Knight Gore 2").Type, 1f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Black Knight Gore 3").Type, 1f);
            }
        }
        #endregion

        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<OilPot>(), 1, 2, 6));
            npcLoot.Add(ItemDropRule.Common(ItemID.GreaterHealingPotion, 1));
            npcLoot.Add(new CommonDrop(ModContent.ItemType<ThrowingSpear>(), 100, 1, 50, 30));
            npcLoot.Add(new CommonDrop(ModContent.ItemType<RoyalThrowingSpear>(), 100, 1, 50, 30));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<BootsOfHaste>(), 10));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<AncientDragonLance>(), 20));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<OldHalberd>(), 5));
            npcLoot.Add(new CommonDrop(ItemID.IronskinPotion, 5, 1, 50, 2));
            npcLoot.Add(new CommonDrop(ItemID.ArcheryPotion, 5, 1, 50, 2));
            npcLoot.Add(new CommonDrop(ItemID.RegenerationPotion, 5, 1, 50, 2));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<Humanity>(), 1, 1, 2));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<PurgingStone>(), 1, 0, 1));
        }
    }

    /// <summary>What the Black Knight's front hand holds during a brain attack.</summary>
    public enum BlackKnightProp : byte
    {
        None,
        Sword,
        Spear,
        Bomb,
        Staff,
    }
}

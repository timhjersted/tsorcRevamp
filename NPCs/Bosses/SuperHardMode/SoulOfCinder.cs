using Microsoft.Xna.Framework;
using System;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;
using tsorcRevamp.Items.Armors;
using tsorcRevamp.Items.Weapons.Enemy;
using tsorcRevamp.Items.Weapons.Melee.Broadswords;
using tsorcRevamp.NPCs.Puppets;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.NPCs.Bosses.SuperHardMode
{
    [AutoloadBossHead]
    class SoulOfCinder : PuppetNPC
    {
        public override string BossHeadTexture => "tsorcRevamp/NPCs/Bosses/SuperHardMode/SoulOfCinder_Head_Boss";

        protected override string InvaderTitle => "Soul of Cinder";

        protected override int HeadArmorItemType => ModContent.ItemType<FirelinkHelm>();
        protected override int BodyArmorItemType => ModContent.ItemType<FirelinkArmor>();
        protected override int LegsArmorItemType => ModContent.ItemType<FirelinkLeggings>();

        protected override int MeleeWeaponItemType => ModContent.ItemType<SeveringDusk>();
        protected override int RangedWeaponItemType => UsesRangedMemory ? ModContent.ItemType<PurpleGemStaff>() : -1;
        protected override int SecondaryRangedWeaponItemType => UsesPyromancyMemory ? ModContent.ItemType<PurpleGemStaff>() : -1;
        protected override int MagicWeaponItemType => UsesMagicMemory ? ModContent.ItemType<PurpleGemStaff>() : -1;

        protected override Vector2 MeleeHandleNorm => new Vector2(0.14f, 0.87f);
        protected override WeaponArchetype MeleeArchetype => WeaponArchetype.Broadsword;
        protected override RangedStyle RangedAnimStyle => RangedStyle.Staff;
        protected override RangedStyle SecondaryRangedAnimStyle => RangedStyle.Staff;

        protected override int MeleeDamage => 95;
        protected override int RangedDamage => 58;
        protected override int SecondaryRangedDamage => 68;
        protected override int MagicDamage => 70;

        bool SecondPhase => _secondPhaseStarted;
        bool UsesAshenMemory => _memory == CinderMemory.AshenWarrior;
        bool UsesAbyssMemory => _memory == CinderMemory.Abyss;
        bool UsesPyromancyMemory => _memory == CinderMemory.Pyromancy;
        bool UsesSorceryMemory => _memory == CinderMemory.SorceryMiracle;
        bool UsesRangedMemory => UsesAbyssMemory || UsesPyromancyMemory;
        bool UsesMagicMemory => UsesPyromancyMemory || UsesSorceryMemory;
        bool FinaleUnlocked => SecondPhase && NPC.life <= NPC.lifeMax / 5;

        protected override float TopSpeed => SecondPhase ? 3.05f : 2.65f;
        protected override float Acceleration => 0.12f;
        protected override float RunDistance => 360f;
        protected override float MeleeRange => 112f;
        protected override float StabRange => 190f;
        protected override float ComboReachBase => 138f;
        protected override float MeleeEngageRange => 132f;
        protected override float ComboMaxStartRange => 290f;
        protected override int MeleeComboChance => UsesAshenMemory ? 100 : SecondPhase ? 55 : 0;
        protected override int RangedStartMeleeComboChance => UsesAshenMemory ? 80 : SecondPhase ? 45 : 0;
        protected override float ComboTelegraphMultiplier => 1.15f;
        protected override int MeleeRecoveryTicks => 32;
        protected override int MeleeRecoveryLingerTicks => 6;
        protected override int MeleeComboInterStepLingerTicks => 3;
        protected override bool CanStab => UsesAshenMemory;
        protected override bool UseCompositeArmSwing => true;
        protected override bool UseSwingEasing => true;
        protected override bool UseLogicalMeleeTelegraphs => true;
        protected override bool UseAuthoredComboSwingClock => true;
        protected override bool MirrorMeleeSwingRotationByFacing => true;
        protected override bool UseAlternateFlip => true;
        protected override bool UseAimAdaptiveArc => true;
        protected override bool HasSlashVFX => true;
        protected override Color SlashVFXColor => MemoryColor;
        protected override Color MeleeTelegraphFlashColor => MemoryColor;

        protected override int EstusChargesMax => 1;
        protected override float EstusHealFraction => 0.10f;
        protected override float FirstHealThreshold => 0.30f;
        protected override float SecondHealThreshold => -1f;
        protected override float HealLifeCapFraction => 0.499f;
        protected override int HealCooldownTicks => 900;
        protected override int HealAnimationTicks => 110;
        protected override float RecentDamageThreshold => 1f;
        protected override float FleeToHealDistance => 20f * 16f;
        protected override int FleeToHealMaxTicks => 120;

        protected override float RangedRange => 780f;
        protected override float MinRangedRange => 160f;
        protected override int RangedTelegraphTicks => 45;
        protected override int RangedAttackTicks => 10;
        protected override int RangedRecoveryTicks => 54;
        protected override int RangedCooldownAfterUse => 170;
        protected override int MaxRangedBurst => 2;
        protected override int SingleRangedBurstChance => 35;
        protected override Color RangedTelegraphFlashColor => MemoryColor;

        protected override float SecondaryRangedRange => 920f;
        protected override float SecondaryRangedMinRange => 300f;
        protected override int SecondaryRangedTelegraphTicks => 45;
        protected override int SecondaryRangedAttackTicks => 10;
        protected override int SecondaryRangedRecoveryTicks => 70;
        protected override int SecondaryRangedCooldownAfterUse => 210;
        protected override int SecondaryRangedChance => 45;
        protected override Color SecondaryRangedFlashColor => Color.LimeGreen;
        // Twelve rising lob shots, exactly five ticks apart, instead of a single generic staff bolt.
        protected override int[][] SecondaryRangedBurstPatterns => new[] { new[] { 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5 } };

        protected override float MagicRange => 1000f;
        protected override float MinMagicRange => 260f;
        protected override int MagicTelegraphTicks => 45;
        protected override int MagicAttackTicks => 16;
        protected override int MagicRecoveryTicks => 82;
        protected override int MagicCooldownAfterUse => 210;
        protected override int MagicPreferenceChance => 55;
        protected override Color MagicTelegraphFlashColor => MemoryColor;
        protected override bool UseAuthoredMagicCastPose => true;
        protected override float MagicCastStartRotation => -0.08f;
        protected override float MagicCastEndRotation => _queuedMagicAttack == CinderMagicAttack.IceStorm
            ? MathHelper.Lerp(-1.15f, 0.65f, _magicShotProgress)
            : -0.78f;
        protected override int MagicWeaponRecoveryHoldTicks => 20;

        protected override bool CanBreathe => UsesPyromancyMemory;
        protected override float BreathRange => 680f;
        protected override float MinBreathRange => 120f;
        protected override int BreathTelegraphTicks => 76;
        protected override int BreathDurationTicks => SecondPhase ? 135 : 105;
        protected override int BreathRecoveryTicks => 70;
        protected override int BreathCooldownAfterUse => 360;
        protected override int BreathChance => 5;

        protected override bool CanPierce => UsesAshenMemory;
        protected override float PierceRange => 760f;
        protected override float MinPierceRange => 230f;
        protected override int PierceChance => 5;
        protected override int PierceTelegraphTicks => 56;
        protected override int PierceDashTicks => 34;
        protected override float PierceDashSpeed => SecondPhase ? 18f : 15.5f;
        protected override int PierceRecoveryTicks => 82;
        protected override int PierceCooldownAfterUse => 300;
        protected override int PierceStabChance => 0;

        protected override bool CanJumpSlash => UsesAshenMemory;
        protected override float JumpSlashMinRange => 160f;
        protected override float JumpSlashMaxRange => 700f;
        protected override float JumpSlashLaunchUpSpeed => 12f;
        protected override float JumpSlashMaxUpSpeed => 16f;
        protected override float JumpSlashLaunchForwardSpeed => 11f;
        protected override float JumpSlashMaxForwardSpeed => 14f;
        protected override float JumpSlashTriggerRange => 112f;
        protected override int JumpSlashChance => 4;
        protected override int JumpSlashRecoveryTicks => 72;
        protected override int JumpSlashCooldownAfterUse => 300;

        protected override bool CanFlipSlash => UsesAshenMemory;
        protected override float FlipSlashMinRange => 130f;
        protected override float FlipSlashMaxRange => 470f;
        protected override int FlipSlashChance => 4;
        protected override int FlipSlashCooldownAfterUse => 360;

        protected override bool CanHomingVolley => false;
        protected override float HomingVolleyMinRange => 280f;
        protected override float HomingVolleyMaxRange => 760f;
        protected override int HomingVolleyChance => 7;
        protected override int HomingVolleyRecoveryTicks => 88;
        protected override int HomingVolleyCooldownAfterUse => 280;

        protected override bool CanBoomerang => UsesAbyssMemory;
        protected override float BoomerangMinRange => 120f;
        protected override float BoomerangMaxRange => 760f;
        protected override int BoomerangChance => 7;
        protected override int BoomerangRecoveryTicks => 92;
        protected override int BoomerangCooldownAfterUse => 320;

        protected override bool CanSpiralFan => UsesSorceryMemory;
        protected override float SpiralFanMinRange => 220f;
        protected override float SpiralFanMaxRange => 850f;
        protected override int SpiralFanChance => 7;
        protected override int SpiralFanRecoveryTicks => 95;
        protected override int SpiralFanCooldownAfterUse => 360;
        protected override int SpiralFanSwingTelegraphTicks => 45;
        protected override int SpiralFanWeaponItemType => ModContent.ItemType<PurpleGemStaff>();
        protected override bool UseAuthoredSpiralFanCastPose => true;
        protected override float SpiralFanCastStartRotation => -0.12f;
        protected override float SpiralFanCastEndRotation => -0.78f;

        public static readonly float ARENA_WIDTH = 864;
        public static readonly float ARENA_HEIGHT = 656;
        //ARENA_LOCATION_ADVENTURE removed: this boss is currently a copy of the old Gwyn boss and has no real
        //arena location yet — a proper event location is pending. Re-add (routed through ExpandedWorldTransform,
        //legacy 2000-space) once that's designed.

        const int BaseDefense = 130;
        const float ProtectionRadius = 1000f;
        const float KillRingRadius = 2000f;
        const int CowardGraceTicks = 90;

        const int DarkBeadDamage = 36;
        const int PhantomSeekerDamage = 55;
        const int BioSpitDamage = 68;
        const int FireBreathDamage = 50;
        const int IceStormDamage = 40;
        const int DisruptDamage = 68;
        const int LostSoulDamage = 38;
        const int OrangeProjDamage = 45;
        const int SwordProjectileDamage = 69;

        const int CB_REVERSAL = 1;
        const int CB_PURSUIT = 3;
        const int CB_BACKSTEP = 4;
        const int CB_SOUL_OF_LORDS = 5;
        const string SoulOfLordsComboName = "Soul of Lords";

        static MeleeComboStep CS(
            ComboMotion motion,
            int telegraph,
            int attack,
            int pause,
            float damage = 1f,
            float reach = 1f,
            float push = 0f,
            SwingEaseStyle ease = SwingEaseStyle.Smooth)
            => new MeleeComboStep
            {
                Motion = motion,
                TelegraphTicks = telegraph,
                AttackTicks = attack,
                PostStepPause = pause,
                DamageMult = damage,
                ReachMult = reach,
                ForwardPushMult = push,
                Ease = ease,
            };

        static readonly MeleeCombo[] CinderCombos =
        {
            new MeleeCombo
            {
                Name = "Cinder Cleave",
                BaseWeight = 85,
                Preferred = ComboRangeBand.Close,
                InitialFlashColor = new Color(255, 128, 52),
                CooldownAfterUse = 55,
                RecoveryTicks = 22,
                MoveBrake = 0.12f,
                Steps = new[]
                {
                    CS(ComboMotion.OverheadArc, 18, 22, 0, 1.05f, 1.08f, 0.30f, SwingEaseStyle.Snap),
                },
            },
            new MeleeCombo
            {
                Name = "Delayed Reversal",
                BaseWeight = 65,
                Preferred = ComboRangeBand.Close,
                InitialFlashColor = new Color(190, 75, 255),
                CooldownAfterUse = 130,
                RecoveryTicks = 30,
                MoveBrake = 0.18f,
                Steps = new[]
                {
                    CS(ComboMotion.UnderhandArc, 22, 20, 16, 0.95f, 1.05f, 0.35f),
                    CS(ComboMotion.OverheadArc, 0, 24, 0, 1.30f, 1.14f, 0.55f, SwingEaseStyle.Whip),
                },
            },
            new MeleeCombo
            {
                Name = "Borrowed Fury",
                BaseWeight = 58,
                Preferred = ComboRangeBand.Close,
                InitialFlashColor = new Color(255, 92, 36),
                CooldownAfterUse = 190,
                RecoveryTicks = 42,
                HyperArmor = true,
                MoveBrake = 0.08f,
                Steps = new[]
                {
                    CS(ComboMotion.HorizontalSweep, 18, 18, 10, 0.82f, 1.02f, 0.38f, SwingEaseStyle.Snap),
                    CS(ComboMotion.UnderhandArc, 0, 20, 13, 0.90f, 1.08f, 0.48f, SwingEaseStyle.Snap),
                    CS(ComboMotion.OverheadArc, 0, 26, 0, 1.38f, 1.18f, 0.62f, SwingEaseStyle.Whip),
                },
            },
            new MeleeCombo
            {
                Name = "Severing Pursuit",
                BaseWeight = 55,
                Preferred = ComboRangeBand.Mid,
                InitialFlashColor = new Color(205, 70, 255),
                CooldownAfterUse = 180,
                RecoveryTicks = 46,
                RangedStartOnly = true,
                MoveBrake = 0f,
                Steps = new[]
                {
                    CS(ComboMotion.JoustDash, 28, 20, 0, 1.28f, 1.35f, 1.75f, SwingEaseStyle.Snap),
                },
            },
            new MeleeCombo
            {
                Name = "Backstep Reprise",
                BaseWeight = 48,
                Preferred = ComboRangeBand.Close,
                InitialFlashColor = new Color(115, 175, 255),
                CooldownAfterUse = 210,
                RecoveryTicks = 34,
                MoveBrake = 0f,
                Steps = new[]
                {
                    CS(ComboMotion.BackstepRaise, 20, 60, 12, 0f, 1f, 0f, SwingEaseStyle.Linear),
                    CS(ComboMotion.HorizontalSweep, 0, 24, 0, 1.32f, 1.18f, 0.90f, SwingEaseStyle.Whip),
                },
            },
            new MeleeCombo
            {
                Name = SoulOfLordsComboName,
                BaseWeight = 26,
                Preferred = ComboRangeBand.Any,
                InitialFlashColor = Color.White,
                CooldownAfterUse = 420,
                RecoveryTicks = 78,
                HeavyCommit = true,
                HyperArmor = true,
                MoveBrake = 0.05f,
                Steps = new[]
                {
                    CS(ComboMotion.UnderhandArc, 36, 18, 9, 0.72f, 1.05f, 0.38f, SwingEaseStyle.Snap),
                    CS(ComboMotion.OverheadArc, 0, 18, 9, 0.72f, 1.05f, 0.42f, SwingEaseStyle.Snap),
                    CS(ComboMotion.UnderhandArc, 0, 19, 11, 0.82f, 1.08f, 0.46f, SwingEaseStyle.Snap),
                    CS(ComboMotion.OverheadArc, 0, 21, 14, 0.95f, 1.12f, 0.52f),
                    CS(ComboMotion.JoustDash, 0, 24, 0, 1.55f, 1.38f, 1.80f, SwingEaseStyle.Whip),
                },
            },
        };

        protected override MeleeCombo[] MeleeComboPoolOverride => CinderCombos;

        NPCDespawnHandler despawnHandler;
        int _protectionTextCooldown;
        readonly int[] _cowardTimers = new int[Main.maxPlayers];
        CinderRangedAttack _queuedRangedAttack = CinderRangedAttack.DarkBead;
        CinderMagicAttack _queuedMagicAttack = CinderMagicAttack.IceStorm;
        int _magicBurstsRemaining;
        int _magicBurstTimer;
        int _magicBurstTotal;
        float _magicShotProgress;
        bool _magicPrepared;
        CinderMemory _memory = CinderMemory.AshenWarrior;
        CinderCustomSequence _customSequence;
        AttackPhase _previousPhase = AttackPhase.Idle;
        // One completed attack per memory keeps the four identities cycling instead of letting
        // a lucky ranged roll repeat while another memory stays unseen.
        int _memoryAttacksRemaining = 1;
        bool _memoryShiftPending;
        bool _secondPhaseStarted;

        enum CinderMemory : byte
        {
            AshenWarrior,
            Abyss,
            Pyromancy,
            SorceryMiracle,
        }

        enum CinderCustomSequence : byte
        {
            None,
            MemoryShift,
            SecondPhaseTransition,
        }

        enum CinderRangedAttack
        {
            DarkBead,
            PhantomSeeker,
            BioSpit
        }

        enum CinderMagicAttack
        {
            IceStorm,
            PhasedMatterBlast,
            LostSoulCurse,
            CinderConstellation,
            FirelinkCross,
            AshenOrbit
        }

        Color MemoryColor => _memory switch
        {
            CinderMemory.AshenWarrior => new Color(255, 128, 52),
            CinderMemory.Abyss => new Color(180, 70, 255),
            CinderMemory.Pyromancy => new Color(255, 72, 30),
            CinderMemory.SorceryMiracle => new Color(115, 185, 255),
            _ => Color.White,
        };

        int MemoryDustType => _memory switch
        {
            CinderMemory.AshenWarrior => DustID.Torch,
            CinderMemory.Abyss => DustID.Shadowflame,
            CinderMemory.Pyromancy => DustID.GoldFlame,
            CinderMemory.SorceryMiracle => DustID.IceTorch,
            _ => DustID.Smoke,
        };

        string MemoryName => _memory switch
        {
            CinderMemory.AshenWarrior => "Ashen Warrior",
            CinderMemory.Abyss => "Abyss",
            CinderMemory.Pyromancy => "Pyromancy",
            CinderMemory.SorceryMiracle => "Sorcery / Miracle",
            _ => "Unknown",
        };

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[NPC.type] = 1;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Confused] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.CursedInferno] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Ichor] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.OnFire] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.OnFire3] = true;
        }

        public override void SetDefaults()
        {
            NPC.aiStyle = -1;
            NPC.npcSlots = 10;
            NPC.height = 40;
            NPC.width = 30;
            NPC.scale = 1.15f;
            NPC.damage = 0;
            NPC.defense = BaseDefense;
            NPC.lifeMax = 750000;
            NPC.knockBackResist = 0f;
            NPC.boss = true;
            NPC.netAlways = true;
            NPC.lavaImmune = true;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.value = 2000000;
            NPC.rarity = 47;
            Music = 12;
            despawnHandler = new NPCDespawnHandler(LangUtils.GetTextValue("NPCs.SoulOfCinder.DespawnHandler"), Color.OrangeRed, 6);

            tsorcRevampGlobalNPC globalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            globalNPC.Agility = 0.25f;
            globalNPC.CanTeleport = true;
            globalNPC.TeleportStyle = NPCs.TeleportStyle.Aggressive;
            globalNPC.TeleportVisualStyle = NPCs.TeleportVisualStyle.Fire;
            globalNPC.NavSearchRadius = 80;
            EvasiveProfile.RedKnight(globalNPC);
        }

        protected override bool CanSelectMeleeCombo(MeleeCombo combo, float distance, float healthFraction)
        {
            if (!UsesAshenMemory && !SecondPhase)
            {
                return false;
            }
            if (combo.Name == SoulOfLordsComboName)
            {
                return FinaleUnlocked;
            }
            if (combo.Name == "Severing Pursuit")
            {
                return distance <= 300f;
            }
            return true;
        }

        protected override int ReactiveComboIndex(float distance, ComboRangeBand band, int[] ready)
        {
            Player player = Main.player[NPC.target];
            if (player == null || !player.active || player.dead)
            {
                return -1;
            }

            bool dodging = player.GetModPlayer<tsorcRevampPlayer>().isDodging;
            float awaySign = Math.Sign(player.Center.X - NPC.Center.X);
            bool rollingAway = dodging
                && Math.Sign(player.velocity.X) == awaySign
                && Math.Abs(player.velocity.X) > 2f;
            bool rollingThrough = dodging && distance < MeleeRange + 24f;
            bool airborne = player.velocity.Y < -3f && player.Center.Y < NPC.Center.Y - 24f;

            if (rollingAway && Ready(ready, CB_PURSUIT))
            {
                return CB_PURSUIT;
            }
            if (rollingThrough && Ready(ready, CB_BACKSTEP))
            {
                return CB_BACKSTEP;
            }
            if (airborne && Ready(ready, CB_REVERSAL))
            {
                return CB_REVERSAL;
            }
            return -1;
        }

        static bool Ready(int[] ready, int index)
            => index >= 0 && index < ready.Length && ready[index] > 0;

        public override void AI()
        {
            TryStartSecondPhaseTransition();
            base.AI();
            PrepareMagicTelegraph();
            SpawnStaffSpellPreviews();
            UpdateAttackLabel();
            EmitSpiralStaffDust();
            despawnHandler.TargetAndDespawn(NPC.whoAmI);
            NPC.TargetClosest(true);

            TickMemoryCycle();
            TickDistanceRules();
        }

        void EmitSpiralStaffDust()
        {
            if (Main.dedServ || !IsSpiralFanPhase || !Main.rand.NextBool(2))
                return;

            Vector2 tip = NPC.Center + new Vector2(30f * NPC.direction, -42f);
            Dust purple = Dust.NewDustPerfect(tip, DustID.PurpleTorch, Main.rand.NextVector2Circular(0.7f, 0.7f), 100,
                new Color(190, 85, 255), 0.85f);
            purple.noGravity = true;
            if (Main.rand.NextBool(2))
            {
                Dust blue = Dust.NewDustPerfect(tip, DustID.IceTorch, Main.rand.NextVector2Circular(0.65f, 0.65f), 100,
                    new Color(85, 185, 255), 0.8f);
                blue.noGravity = true;
            }
        }

        bool IsSpiralFanPhase => Phase == AttackPhase.SpiralFanSwingTelegraph
            || Phase == AttackPhase.SpiralFanSwing
            || Phase == AttackPhase.SpiralFanBurst
            || Phase == AttackPhase.SpiralFanPause;

        void UpdateAttackLabel()
        {
            if (Phase == AttackPhase.Custom)
                return;

            DebugAttackLabel = Phase switch
            {
                AttackPhase.RangedTelegraph or AttackPhase.RangedAttack or AttackPhase.RangedRecovery
                    => _queuedRangedAttack == CinderRangedAttack.BioSpit ? "Emerald Rain Staff Volley"
                     : _queuedRangedAttack == CinderRangedAttack.PhantomSeeker ? "Phantom Staff Seeker"
                     : "Dark Bead Staff Volley",
                AttackPhase.MagicTelegraph or AttackPhase.MagicAttack or AttackPhase.MagicRecovery
                    => _queuedMagicAttack == CinderMagicAttack.IceStorm ? "Rising Ice Storm"
                     : _queuedMagicAttack == CinderMagicAttack.PhasedMatterBlast ? "Phased Matter Cast"
                     : _queuedMagicAttack == CinderMagicAttack.CinderConstellation ? "Cinder Constellation"
                     : _queuedMagicAttack == CinderMagicAttack.FirelinkCross ? "Firelink Cross"
                     : _queuedMagicAttack == CinderMagicAttack.AshenOrbit ? "Ashen Orbit"
                     : "Lost Soul Curse",
                AttackPhase.SpiralFanSwingTelegraph or AttackPhase.SpiralFanSwing or AttackPhase.SpiralFanBurst or AttackPhase.SpiralFanPause or AttackPhase.SpiralFanRecovery => "Azure Wisp Fan",
                AttackPhase.JumpSlashDodgeback or AttackPhase.JumpSlashRise or AttackPhase.JumpSlashAttack or AttackPhase.JumpSlashRecovery => "Leaping Cinderfall",
                AttackPhase.FlipSlashRise or AttackPhase.FlipSlashLand => "Cinder Somersault",
                AttackPhase.BoomerangSwingTelegraph or AttackPhase.BoomerangSwing or AttackPhase.BoomerangRecovery => "Cinder Blade Throw",
                AttackPhase.HomingVolleyDodgeback or AttackPhase.HomingVolleySwingTelegraph or AttackPhase.HomingVolleySwing or AttackPhase.HomingVolleyRecovery => "Skyfall Trident Volley",
                AttackPhase.BreathTelegraph or AttackPhase.Breathing or AttackPhase.BreathRecovery => "First Flame Breath",
                AttackPhase.PierceTelegraph or AttackPhase.PierceDash or AttackPhase.PierceRecovery => "Ashen Piercing Dash",
                _ => null,
            };
        }

        protected override void DoRangedTelegraphVFX(bool secondary, float progress)
        {
            if (Main.dedServ || !Main.rand.NextBool(2))
                return;

            Color color = secondary ? Color.LimeGreen : new Color(125, 185, 255);
            Vector2 tip = NPC.Center + new Vector2(30f * NPC.direction, -42f);
            Dust dust = Dust.NewDustPerfect(tip, secondary ? DustID.GreenTorch : DustID.IceTorch,
                Main.rand.NextVector2Circular(0.75f, 0.75f), 100, color, MathHelper.Lerp(0.7f, 1.15f, progress));
            dust.noGravity = true;
        }

        protected override void DoMagicTelegraphVFX(float progress)
        {
            if (Main.dedServ || !Main.rand.NextBool(2))
                return;

            bool fireRitual = _queuedMagicAttack >= CinderMagicAttack.CinderConstellation;
            Vector2 tip = NPC.Center + new Vector2(30f * NPC.direction, -42f);
            if (fireRitual)
            {
                int ritual = (int)_queuedMagicAttack - (int)CinderMagicAttack.CinderConstellation;
                if (ritual == 0) // Cinder Constellation: diagonal, inward-curling embers.
                {
                    float angle = progress * MathHelper.TwoPi * 1.7f * NPC.direction;
                    Dust dust = Dust.NewDustPerfect(tip + angle.ToRotationVector2() * 6f, DustID.OrangeTorch,
                        -angle.ToRotationVector2() * (0.7f + progress), 90, Color.Orange, 0.55f + progress * 0.35f);
                    dust.noGravity = true;
                    if (progress > 0.75f && Main.rand.NextBool(3))
                    {
                        Dust glint = Dust.NewDustPerfect(tip, DustID.AncientLight, Main.rand.NextVector2Circular(0.5f, 0.5f),
                            100, Color.LightGoldenrodYellow, 0.45f);
                        glint.noGravity = true;
                    }
                }
                else if (ritual == 1) // Firelink Cross: alternating horizontal and vertical red-orange strokes.
                {
                    bool vertical = ((int)(progress * 30f) / 2) % 2 == 0;
                    Vector2 direction = vertical ? Vector2.UnitY : Vector2.UnitX * NPC.direction;
                    Dust dust = Dust.NewDustPerfect(tip - direction * 7f, vertical ? DustID.RedTorch : DustID.OrangeTorch,
                        direction * (1f + progress), 90, Color.OrangeRed, 0.55f + progress * 0.35f);
                    dust.noGravity = true;
                }
                else // Ashen Orbit: a clockwise ember ring that becomes white-hot at release.
                {
                    float angle = progress * MathHelper.TwoPi * 2.2f * NPC.direction;
                    Vector2 tangent = (angle + MathHelper.PiOver2 * NPC.direction).ToRotationVector2();
                    Dust dust = Dust.NewDustPerfect(tip + angle.ToRotationVector2() * 7f, DustID.OrangeTorch,
                        tangent * (0.8f + progress), 90, Color.Orange, 0.52f + progress * 0.3f);
                    dust.noGravity = true;
                    if (progress > 0.76f && Main.rand.NextBool(3))
                    {
                        Dust glint = Dust.NewDustPerfect(tip, DustID.AncientLight, Main.rand.NextVector2Circular(0.45f, 0.45f),
                            100, Color.LightGoldenrodYellow, 0.42f);
                        glint.noGravity = true;
                    }
                }
                return;
            }

            Color color = _queuedMagicAttack == CinderMagicAttack.IceStorm
                ? new Color(115, 195, 255) : new Color(190, 95, 255);
            Dust spellDust = Dust.NewDustPerfect(tip, _queuedMagicAttack == CinderMagicAttack.IceStorm ? DustID.IceTorch : DustID.PurpleTorch,
                Main.rand.NextVector2Circular(0.85f, 0.85f), 90, color, 0.75f + progress * 0.45f);
            spellDust.noGravity = true;
        }

        void SpawnStaffSpellPreviews()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
                return;

            if (Phase == AttackPhase.RangedTelegraph && PhaseTimer == 30
                && _queuedRangedAttack == CinderRangedAttack.DarkBead)
            {
                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero,
                    ModContent.ProjectileType<Projectiles.Enemy.CinderDarkBeadPreview>(), 0, 0f, Main.myPlayer, NPC.whoAmI);
            }
            else if (Phase == AttackPhase.MagicTelegraph && PhaseTimer == 30)
            {
                if (_queuedMagicAttack == CinderMagicAttack.IceStorm)
                {
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, Vector2.Zero,
                        ModContent.ProjectileType<Projectiles.Enemy.CinderIceSpellPreview>(), 0, 0f, Main.myPlayer, NPC.whoAmI);
                }
                else if (_queuedMagicAttack >= CinderMagicAttack.CinderConstellation)
                {
                    SpawnRemoteFirePattern(Main.player[NPC.target], _queuedMagicAttack);
                }
            }
        }

        void SpawnRemoteFirePattern(Player target, CinderMagicAttack pattern)
        {
            int count = pattern == CinderMagicAttack.AshenOrbit ? 6 : 4;
            float radius = pattern == CinderMagicAttack.CinderConstellation ? 190f
                : pattern == CinderMagicAttack.AshenOrbit ? 220f : 165f;
            for (int i = 0; i < count; i++)
            {
                float angle = MathHelper.TwoPi * i / count + (pattern == CinderMagicAttack.CinderConstellation ? MathHelper.PiOver4 : 0f);
                int id = Projectile.NewProjectile(NPC.GetSource_FromThis(), target.Center + angle.ToRotationVector2() * radius,
                    Vector2.Zero, ModContent.ProjectileType<Projectiles.Enemy.CinderRemoteFlame>(), FireBreathDamage, 0f,
                    Main.myPlayer, target.whoAmI, pattern == CinderMagicAttack.CinderConstellation ? 0 : pattern == CinderMagicAttack.FirelinkCross ? 1 : 2);
                Main.projectile[id].rotation = angle;
            }
            SoundEngine.PlaySound(pattern switch
            {
                CinderMagicAttack.CinderConstellation => SoundID.Item29 with { Volume = 0.7f, Pitch = 0.1f },
                CinderMagicAttack.FirelinkCross => SoundID.Item74 with { Volume = 0.7f, Pitch = -0.2f },
                _ => SoundID.Item119 with { Volume = 0.65f, Pitch = 0.05f }
            }, NPC.Center);
        }

        void PrepareMagicTelegraph()
        {
            if (Phase != AttackPhase.MagicTelegraph)
            {
                if (Phase != AttackPhase.MagicAttack)
                    _magicPrepared = false;
                return;
            }

            if (_magicPrepared || Main.netMode == NetmodeID.MultiplayerClient)
                return;

            if (UsesPyromancyMemory)
            {
                _queuedMagicAttack = (CinderMagicAttack)Main.rand.Next((int)CinderMagicAttack.CinderConstellation, (int)CinderMagicAttack.AshenOrbit + 1);
            }
            else if (SecondPhase)
            {
                _queuedMagicAttack = Main.rand.Next(3) switch
                {
                    0 => CinderMagicAttack.IceStorm,
                    1 => CinderMagicAttack.PhasedMatterBlast,
                    _ => CinderMagicAttack.LostSoulCurse,
                };
            }
            else
            {
                _queuedMagicAttack = Main.rand.NextBool()
                    ? CinderMagicAttack.IceStorm
                    : CinderMagicAttack.PhasedMatterBlast;
            }
            _magicPrepared = true;
            NPC.netUpdate = true;
        }

        void TryStartSecondPhaseTransition()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient
                || _secondPhaseStarted
                || NPC.life > NPC.lifeMax / 2)
            {
                return;
            }

            _secondPhaseStarted = true;
            _memory = CinderMemory.AshenWarrior;
            _memoryAttacksRemaining = 1;
            _memoryShiftPending = false;
            _customSequence = CinderCustomSequence.SecondPhaseTransition;
            StartCustomAttack(90);
            NPC.netUpdate = true;
        }

        void TickMemoryCycle()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                _previousPhase = Phase;
                return;
            }

            bool returnedToNeutral = IsCompletedAttackPhase(_previousPhase) && IsNeutralPhase(Phase);
            if (returnedToNeutral)
            {
                _memoryAttacksRemaining--;
                _memoryShiftPending = _memoryAttacksRemaining <= 0;
            }

            if (_memoryShiftPending && IsNeutralPhase(Phase))
            {
                _memory = (CinderMemory)(((int)_memory + 1) % 4);
                _memoryAttacksRemaining = 1;
                _memoryShiftPending = false;
                _customSequence = CinderCustomSequence.MemoryShift;
                StartCustomAttack(32);
                NPC.netUpdate = true;
            }

            _previousPhase = Phase;
        }

        static bool IsNeutralPhase(AttackPhase phase)
            => phase == AttackPhase.Idle || phase == AttackPhase.CasualStroll;

        static bool IsCompletedAttackPhase(AttackPhase phase)
        {
            return phase == AttackPhase.MeleeRecovery
                || phase == AttackPhase.StabRecovery
                || phase == AttackPhase.RangedRecovery
                || phase == AttackPhase.MagicRecovery
                || phase == AttackPhase.BreathRecovery
                || phase == AttackPhase.PierceRecovery
                || phase == AttackPhase.JumpSlashRecovery
                || phase == AttackPhase.FlipSlashLand
                || phase == AttackPhase.HomingVolleyRecovery
                || phase == AttackPhase.BoomerangRecovery
                || phase == AttackPhase.SpiralFanRecovery
                || phase == AttackPhase.MeleeComboRecovery;
        }

        protected override void DoCustomAttack()
        {
            if (_customSequence == CinderCustomSequence.SecondPhaseTransition)
            {
                DebugAttackLabel = "Second Phase: Soul Rekindled";
                SoundEngine.PlaySound(SoundID.Item74 with { Volume = 0.9f, Pitch = -0.15f }, NPC.Center);
            }
            else
            {
                DebugAttackLabel = $"Memory Shift: {MemoryName}";
                SoundEngine.PlaySound(SoundID.Item29 with { Volume = 0.7f, Pitch = 0.1f }, NPC.Center);
            }

            if (Main.dedServ)
            {
                return;
            }

            for (int i = 0; i < 24; i++)
            {
                Vector2 velocity = Main.rand.NextVector2CircularEdge(5.5f, 5.5f);
                Dust dust = Dust.NewDustPerfect(NPC.Center, MemoryDustType, velocity, 80, MemoryColor, 1.35f);
                dust.noGravity = true;
            }
        }

        protected override void DoCustomTick(int ticksRemaining)
        {
            if (!Main.dedServ)
            {
                int duration = _customSequence == CinderCustomSequence.SecondPhaseTransition ? 90 : 32;
                float progress = 1f - ticksRemaining / (float)duration;
                float radius = MathHelper.Lerp(90f, 18f, progress);

                if (Main.GameUpdateCount % 2 == 0)
                {
                    Vector2 offset = Main.rand.NextVector2CircularEdge(radius, radius);
                    Dust dust = Dust.NewDustPerfect(
                        NPC.Center + offset,
                        MemoryDustType,
                        -offset.SafeNormalize(Vector2.Zero) * MathHelper.Lerp(1.2f, 4.5f, progress),
                        80,
                        MemoryColor,
                        1.15f);
                    dust.noGravity = true;
                }

                Lighting.AddLight(NPC.Center, MemoryColor.ToVector3() * (0.8f + progress * 1.5f));
            }

            if (ticksRemaining == 1)
            {
                _customSequence = CinderCustomSequence.None;
                DebugAttackLabel = null;
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    NPC.netUpdate = true;
                }
            }
        }

        void TickDistanceRules()
        {
            UsefulFunctions.DustRing(NPC.Center, (int)ProtectionRadius, DustID.BlueTorch, 20, 1f);
            UsefulFunctions.DustRing(NPC.Center, (int)KillRingRadius, DustID.RedsWingsRun, 1, 1f);
            UsefulFunctions.DustRing(NPC.Center, (int)KillRingRadius, DustID.Torch, 10, 1f);
            UsefulFunctions.DustRing(NPC.Center, (int)KillRingRadius, DustID.RedTorch, 5, 2f);
            UsefulFunctions.DustRing(NPC.Center, (int)KillRingRadius, DustID.Firefly, 100, -3f);

            bool protectedByDistance = NPC.HasValidTarget && NPC.Distance(Main.player[NPC.target].Center) > ProtectionRadius;
            if (protectedByDistance)
            {
                NPC.defense = 9999;
                if (_protectionTextCooldown <= 0)
                {
                    UsefulFunctions.BroadcastText(LangUtils.GetTextValue("NPCs.Gwyn.Protected"), 175, 75, 255);
                    _protectionTextCooldown = 200;
                }
            }
            else
            {
                NPC.defense = BaseDefense;
            }

            if (_protectionTextCooldown > 0)
            {
                _protectionTextCooldown--;
            }

            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player player = Main.player[i];
                if (!player.active || player.dead)
                {
                    _cowardTimers[i] = 0;
                    continue;
                }

                float distance = NPC.Distance(player.Center);
                if (distance < KillRingRadius)
                {
                    player.AddBuff(ModContent.BuffType<TornWings>(), 60, false);
                }
                if (distance < 700f)
                {
                    player.AddBuff(ModContent.BuffType<GrappleMalfunction>(), 60, false);
                }

                if (distance > KillRingRadius)
                {
                    _cowardTimers[i]++;
                    if (_cowardTimers[i] >= CowardGraceTicks)
                    {
                        player.AddBuff(ModContent.BuffType<CowardsAffliction>(), 30, false);
                        if (_cowardTimers[i] == CowardGraceTicks)
                        {
                            UsefulFunctions.BroadcastText(LangUtils.GetTextValue("NPCs.Gwyn.Coward"), 235, 199, 23);
                        }
                    }
                }
                else
                {
                    _cowardTimers[i] = 0;
                }
            }
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo hurtInfo)
        {
            target.AddBuff(BuffID.OnFire, 10 * 60, false);
            target.AddBuff(ModContent.BuffType<FracturingArmor>(), 40 * 60, false);
            target.AddBuff(ModContent.BuffType<SlowedLifeRegen>(), 30 * 60, false);
            target.AddBuff(ModContent.BuffType<BrokenSpirit>(), 30 * 60, false);
            if (Main.rand.NextBool(2))
            {
                target.AddBuff(BuffID.Weak, 10 * 60, false);
                target.AddBuff(BuffID.BrokenArmor, 3 * 60, false);
            }
        }

        protected override void DoMeleeAttack()
        {
            PlayMeleeSwingSound();
            TryMeleeHit(MeleeRange);
        }

        protected override void DoStabAttack()
        {
            SoundEngine.PlaySound(SoundID.Item71 with { Volume = 0.65f, PitchVariance = 0.15f }, NPC.Center);
            TryMeleeHit(StabRange * 0.7f);
        }

        protected override void DoPierceWindup(int elapsed)
        {
            if (Main.dedServ || elapsed % 3 != 0)
            {
                return;
            }

            float progress = MathHelper.Clamp(elapsed / (float)PierceTelegraphTicks, 0f, 1f);
            Vector2 offset = Main.rand.NextVector2CircularEdge(58f, 28f);
            Dust dust = Dust.NewDustPerfect(
                NPC.Center + offset,
                DustID.Shadowflame,
                -offset.SafeNormalize(Vector2.Zero) * MathHelper.Lerp(1.2f, 4f, progress),
                80,
                new Color(205, 70, 255),
                1.1f);
            dust.noGravity = true;
        }

        protected override void DoPierceDashTick()
        {
            if (Main.dedServ)
            {
                return;
            }

            Lighting.AddLight(NPC.Center, new Color(180, 55, 255).ToVector3() * 1.1f);
            if (Main.rand.NextBool(2))
            {
                Dust dust = Dust.NewDustPerfect(
                    NPC.Center + Main.rand.NextVector2Circular(12f, 20f),
                    DustID.Shadowflame,
                    -NPC.velocity * 0.22f,
                    90,
                    new Color(205, 70, 255),
                    1.05f);
                dust.noGravity = true;
            }
        }

        protected override void OnPierceContact(Player target, bool isStab)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            Projectile.NewProjectile(
                NPC.GetSource_FromThis(),
                target.Center,
                new Vector2(NPC.direction * 0.01f, 0f),
                ModContent.ProjectileType<Projectiles.Enemy.Weapons.PuppetMeleeHitbox>(),
                MeleeDamage + 7,
                6f,
                Main.myPlayer,
                target.width + 12f,
                target.height + 8f);
        }

        protected override void OnRangedBurstStarted(bool secondary)
        {
            if (secondary)
            {
                _queuedRangedAttack = CinderRangedAttack.BioSpit;
                return;
            }

            if (!secondary && SecondPhase && Main.rand.NextBool(3))
            {
                _queuedRangedAttack = CinderRangedAttack.PhantomSeeker;
            }
            else
            {
                _queuedRangedAttack = CinderRangedAttack.DarkBead;
            }
        }

        protected override void DoRangedAttack()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            Player target = Main.player[NPC.target];
            switch (_queuedRangedAttack)
            {
                case CinderRangedAttack.PhantomSeeker:
                    FirePhantomSeeker(target);
                    break;
                case CinderRangedAttack.BioSpit:
                    FireBioSpit(target, BioSpitDamage);
                    break;
                default:
                    FireDarkBead(target);
                    break;
            }
        }

        void FireDarkBead(Player target)
        {
            Vector2 baseVelocity = UsefulFunctions.Aim(NPC.Center, target.Center, 7f);
            for (int i = -1; i <= 1; i++)
            {
                Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, baseVelocity.RotatedBy(MathHelper.ToRadians(i * 9f)), ModContent.ProjectileType<Projectiles.Enemy.ArtoriasDarkBead>(), DarkBeadDamage, 0f, Main.myPlayer);
            }
            SoundEngine.PlaySound(SoundID.Item80 with { Volume = 0.4f, Pitch = 0.1f }, NPC.Center);
        }

        void FirePhantomSeeker(Player target)
        {
            int id = Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center + new Vector2(20f, 20f), Main.rand.NextVector2Circular(5f, 5f), ModContent.ProjectileType<Projectiles.Enemy.BurningPhantomSeeker>(), PhantomSeekerDamage, 0f, Main.myPlayer);
            Main.projectile[id].timeLeft = 460;
            Main.projectile[id].rotation = Main.rand.Next(700) / 100f;
            Main.projectile[id].ai[0] = target.whoAmI;
            SoundEngine.PlaySound(SoundID.Item17, NPC.Center);
        }

        void FireBioSpit(Player target, int damage)
        {
            float height = Main.rand.NextFloat(9f, 16f) * 16f;
            float verticalSpeed = -MathF.Sqrt(2f * 0.30f * height);
            float airTime = -2f * verticalSpeed / 0.30f;
            float horizontalSpeed = MathHelper.Clamp((target.Center.X - NPC.Center.X) / airTime, -10f, 10f);
            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center,
                new Vector2(horizontalSpeed, verticalSpeed), ModContent.ProjectileType<Projectiles.Enemy.CinderGreenRain>(),
                damage, 5f, Main.myPlayer, target.whoAmI);
            SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.2f, Pitch = 0.5f }, NPC.Center);
        }

        protected override void DoMagicAttack()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            // The selection was locked at telegraph entry so the staff, growing spell preview,
            // and release all tell the same attack.
            if (!_magicPrepared)
                PrepareMagicTelegraph();

            if (_queuedMagicAttack == CinderMagicAttack.IceStorm)
            {
                _magicBurstTotal = Main.rand.Next(6, 13);
                _magicBurstsRemaining = _magicBurstTotal - 1;
                _magicBurstTimer = 0;
                _magicShotProgress = 0f;
                _magicAttackTicksOverride = _magicBurstTotal * 12;
            }
            else if (_queuedMagicAttack >= CinderMagicAttack.CinderConstellation)
            {
                _magicBurstTotal = 0;
                _magicBurstsRemaining = 0;
                _magicAttackTicksOverride = 30;
                SoundEngine.PlaySound(SoundID.Item74 with { Volume = 0.55f, Pitch = -0.1f }, NPC.Center);
            }
            else
            {
                _magicBurstTotal = SecondPhase ? 3 : 2;
                _magicBurstsRemaining = _magicBurstTotal - 1;
                _magicBurstTimer = 0;
                _magicAttackTicksOverride = 56;
            }
            FireQueuedMagic(Main.player[NPC.target]);
        }

        protected override void DoMagicTick(int ticksRemaining)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient || _magicBurstsRemaining <= 0)
            {
                return;
            }

            _magicBurstTimer++;
            int interval = _queuedMagicAttack == CinderMagicAttack.IceStorm ? 12 : 18;
            if (_magicBurstTimer >= interval)
            {
                _magicBurstTimer = 0;
                FireQueuedMagic(Main.player[NPC.target]);
                _magicBurstsRemaining--;
                NPC.netUpdate = true;
            }
        }

        void FireQueuedMagic(Player target)
        {
            switch (_queuedMagicAttack)
            {
                case CinderMagicAttack.PhasedMatterBlast:
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, UsefulFunctions.BallisticTrajectory(NPC.Center, target.Center, 6f, 1.06f, true, true), ModContent.ProjectileType<Projectiles.Enemy.Okiku.PhasedMatterBlast>(), DisruptDamage, 5f, Main.myPlayer);
                    SoundEngine.PlaySound(SoundID.Item79 with { Volume = 0.2f, Pitch = 0.4f }, NPC.Center);
                    break;
                case CinderMagicAttack.LostSoulCurse:
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, UsefulFunctions.BallisticTrajectory(NPC.Center, target.Center, 8f, 1.06f, true, true), ProjectileID.DesertDjinnCurse, LostSoulDamage, 7f, Main.myPlayer);
                    SoundEngine.PlaySound(SoundID.Item24 with { Volume = 0.6f, Pitch = 0.5f }, NPC.Center);
                    break;
                case CinderMagicAttack.CinderConstellation:
                case CinderMagicAttack.FirelinkCross:
                case CinderMagicAttack.AshenOrbit:
                    // The brands were created at their remote positions during the last 30 tell ticks.
                    break;
                default:
                    float shotIndex = _magicBurstTotal - _magicBurstsRemaining - 1;
                    _magicShotProgress = _magicBurstTotal <= 1 ? 0.5f : shotIndex / (_magicBurstTotal - 1f);
                    float angle = MathHelper.Lerp(MathHelper.ToRadians(-145f), MathHelper.ToRadians(-35f), _magicShotProgress);
                    Vector2 velocity = angle.ToRotationVector2() * 12.5f;
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, velocity, ModContent.ProjectileType<Projectiles.Enemy.EnemySpellIce3Ball>(), IceStormDamage, 0f, Main.myPlayer);
                    SoundEngine.PlaySound(SoundID.Item30 with { Volume = 0.3f }, NPC.Center);
                    break;
            }
        }

        protected override void DoBreathWindup(int elapsed)
        {
            if (Main.dedServ)
            {
                return;
            }

            float t = MathHelper.Clamp(elapsed / (float)BreathTelegraphTicks, 0f, 1f);
            float radius = MathHelper.Lerp(60f, 12f, t);
            UsefulFunctions.DustRing(NPC.Center, (int)radius, DustID.Torch, 24, 2f);
            Lighting.AddLight(NPC.Center, Color.OrangeRed.ToVector3() * (1f + t * 3f));
        }

        protected override void OnBreathStart()
        {
            SoundEngine.PlaySound(new SoundStyle("tsorcRevamp/Sounds/DarkSouls/breath1") with { Volume = 0.5f }, NPC.Center);
        }

        protected override void DoBreathTick(int ticksRemaining)
        {
            Lighting.AddLight(NPC.Center, Color.OrangeRed.ToVector3() * 2f);
            if (Main.netMode == NetmodeID.MultiplayerClient || ticksRemaining % 3 != 0)
            {
                return;
            }

            Player target = Main.player[NPC.target];
            Vector2 breathVel = UsefulFunctions.Aim(NPC.Center, target.Center, 9f) + Main.rand.NextVector2Circular(1.5f, 1.5f);
            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center + new Vector2(5f * NPC.direction, -12f), breathVel, ModContent.ProjectileType<Projectiles.Enemy.FireBreath>(), FireBreathDamage, 0f, Main.myPlayer);
            SoundEngine.PlaySound(SoundID.Item34 with { Volume = 0.12f, Pitch = 0.2f }, NPC.Center);
        }

        protected override void DoBoomerangFire()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            Player target = Main.player[NPC.target];
            Vector2 velocity = UsefulFunctions.Aim(NPC.Center, target.Center, 18f);
            Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Center, velocity, ModContent.ProjectileType<Projectiles.Enemy.Cinder.SwordOfCinder>(), SwordProjectileDamage, 0.5f, Main.myPlayer, NPC.whoAmI);
        }

        protected override void DoSpiralFanFire(int shotIndex)
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            Player target = Main.player[NPC.target];
            Vector2 origin = NPC.Center + new Vector2(30f * NPC.direction, -42f);
            int bulletCount = SecondPhase ? 7 : 5;
            float spread = SecondPhase ? 67f : 52.5f;
            Vector2 baseVelocity = UsefulFunctions.Aim(origin, target.Center, 24f);
            for (int i = -(bulletCount / 2); i <= bulletCount / 2; i++)
            {
                Vector2 velocity = baseVelocity.RotatedBy(MathHelper.ToRadians(-((spread / (bulletCount - 1)) * i)));
                Projectile.NewProjectile(NPC.GetSource_FromThis(), origin, velocity,
                    ModContent.ProjectileType<Projectiles.Enemy.CinderBlueWisp>(), OrangeProjDamage, 1f, Main.myPlayer);
            }
        }

        protected override int NextSpiralFanDelay(int completedShotIndex)
        {
            return completedShotIndex < (SecondPhase ? 5 : 3) ? 30 : -1;
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            base.SendExtraAI(writer);
            writer.Write((byte)_memory);
            writer.Write((byte)_customSequence);
            writer.Write((byte)Math.Clamp(_memoryAttacksRemaining, 0, byte.MaxValue));
            writer.Write(_memoryShiftPending);
            writer.Write(_secondPhaseStarted);
            writer.Write((short)(_customSequence == CinderCustomSequence.None ? 0 : Math.Max(1, PhaseTimer)));
            writer.Write((byte)_queuedRangedAttack);
            writer.Write((byte)_queuedMagicAttack);
            writer.Write((byte)Math.Clamp(_magicBurstsRemaining, 0, byte.MaxValue));
            writer.Write((byte)Math.Clamp(_magicBurstTotal, 0, byte.MaxValue));
            writer.Write(_magicShotProgress);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            CinderCustomSequence previousCustomSequence = _customSequence;
            base.ReceiveExtraAI(reader);
            _memory = (CinderMemory)reader.ReadByte();
            _customSequence = (CinderCustomSequence)reader.ReadByte();
            _memoryAttacksRemaining = reader.ReadByte();
            _memoryShiftPending = reader.ReadBoolean();
            _secondPhaseStarted = reader.ReadBoolean();
            int customTicksRemaining = reader.ReadInt16();
            _queuedRangedAttack = (CinderRangedAttack)reader.ReadByte();
            _queuedMagicAttack = (CinderMagicAttack)reader.ReadByte();
            _magicBurstsRemaining = reader.ReadByte();
            _magicBurstTotal = reader.ReadByte();
            _magicShotProgress = reader.ReadSingle();

            if (_customSequence != CinderCustomSequence.None)
            {
                if (previousCustomSequence != _customSequence || Phase != AttackPhase.Custom)
                {
                    StartCustomAttack(Math.Max(1, customTicksRemaining));
                }
                else
                {
                    PhaseTimer = Math.Max(1, customTicksRemaining);
                }
            }
            else if (previousCustomSequence != CinderCustomSequence.None && Phase == AttackPhase.Custom)
            {
                DebugAttackLabel = null;
                EnterPhase(AttackPhase.Idle, 0);
            }
        }

        public override void ModifyHitByItem(Player player, Item item, ref NPC.HitModifiers modifiers)
        {
            modifiers.FinalDamage *= 1.1f;
        }

        public override void ModifyHitByProjectile(Projectile projectile, ref NPC.HitModifiers modifiers)
        {
            modifiers.FinalDamage *= 1.1f;

            if (projectile.minion)
            {
                modifiers.Knockback *= 0f;
            }
        }

        public override bool CheckActive()
        {
            return false;
        }

        public override void BossLoot(ref string name, ref int potionType)
        {
            potionType = ItemID.SuperHealingPotion;
        }

        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            npcLoot.Add(ItemDropRule.BossBag(ModContent.ItemType<Items.BossBags.SoulOfCinderBag>()));
        }

    }
}

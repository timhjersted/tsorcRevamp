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

        // ── Lob volley ───────────────────────────────────────────────────────────────────────────────────────
        // A sky-fall volley of ManHunterLobArrows: one draw, then LobArrowCount arrows loosed nearly straight up every 10t
        // (6t release + 4t pause, ChainPatterns row LobPatternIndex). Each re-enters above its own landing point and
        // falls straight down or at 45 degrees (see ManHunterLobArrow), all with the same flight time, so they land in a
        // marching line along the ground. Used two ways: the Hero of Lumelia's artillery archers (order-driven, ~100 tiles
        // out) and, in Hardmode, wild Man Hunters at the edge of the screen (a roll, 700-940px).
        private const int LobExpertDamage = 90;
        private const int LobArrowCount = 5;
        private const int LobPatternIndex = 10;
        // The bow points this far toward the target as it points up (x lean per unit of up): ~8.5 degrees off vertical.
        // Non-zero so the shot-facing code (which ignores |aim.X| < 0.05) turns the archer toward its target.
        private const float LobAimLean = 0.15f;
        // Descent direction per arrow, indexed by (arrowIndex + slot) % 3: straight down, down-left, down-right.
        private static readonly int[] LobDescentPattern = { 0, -1, 1 };
        // Artillery sweep: 4 archers x 5 arrows interleave into 20 landing points across +/-418px. Each archer's own
        // arrows are 176px apart; its slot shifts them 44px so the four archers fill the gaps between each other.
        private const float ArtilleryArrowSpacing = 176f;
        private const float ArtillerySlotSpacing = 44f;
        private const float ArtilleryHalfSweep = 418f;
        // A wild Man Hunter's five arrows: a tight 70px line centred on the player.
        private const float WildArrowSpacing = 70f;
        private const float LobJitterPx = 12f;
        private const int WildLobCooldownMin = 600;
        private const int WildLobCooldownMax = 900;
        private const int WildLobRollOneIn = 90;
        private const float WildLobMinRange = MaxShotRange + 40f;
        private const float WildLobMaxRange = 940f;

        // ── Hero of Lumelia artillery archers ────────────────────────────────────────────────────────────────
        // The Hero spawns four of these (ConfigureAsHeroArcher) and afterwards only sends a volley order and, at 60%
        // and 35% HP, BeginJoin. Everything else is the archer's own: it holds a stand-off band from its target,
        // re-posts when the player leaves the band, and fights as a normal Man Hunter once the player is close.
        private const float EngageTiles = 45f;
        private const float EngageReleaseTiles = 58f;
        private const float HoldMinTiles = 60f;
        private const float HoldMaxTiles = 125f;
        private const int RepositionTimeoutTicks = 900;
        private const int RetargetTicks = 45;
        private const int VolleyOrderLifeTicks = 150;
        private const float RepositionSpeedMult = 2f;
        // A joining archer walks in faster than a patrolling Man Hunter until it is inside bow range.
        private const float JoinWalkSpeedMult = 1.8f;
        private const float PostSearchTiles = 40f;
        private const int PostSearchHeightTiles = 24;
        private const int PostClearAboveTiles = 8;

        private enum ArcherMode : byte
        {
            Hold,
            Reposition,
            Engaged,
        }

        // Synced (SendExtraAI): who owns this archer and what it is doing.
        private short _ownerId = -1;
        private short _ownerType;
        private byte _slot;
        private sbyte _side = 1;
        private float _standoffTiles = 100f;
        private bool _artillery;
        private bool _forcePursue;
        private ArcherMode _mode = ArcherMode.Hold;
        private float _postX;
        private float _postFeetY;

        // Server only: the lob in progress and the brain's timers.
        private bool _lobActive;
        private Vector2 _lobAnchor;
        private int _lobSweepDir = 1;
        private readonly float[] _lobJitter = new float[LobArrowCount];
        private int _volleyOrderTicks;
        private int _retargetTimer;
        private int _repostCooldown;
        private int _repositionTicks;
        private int _wildLobCooldown;
        private bool _ownerLostDustDone;

        /// <summary>True while this archer is one of the Hero's long-range artillery (not yet joined, not wild).</summary>
        public bool IsArtillery => _artillery;

        /// <summary>whoAmI of the Hero that spawned this archer, or -1 for a wild Man Hunter.</summary>
        public int OwnerId => _ownerId;

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
            // Row 10 (LobPatternIndex): the lob volley's five shots, 4t pauses. Never rolled by the HP-band chances below
            // (they stop at index 9); only LobChances reaches it. A client rebuilds its pause row from this table by
            // the synced index, so the row has to live in the static table, not in a server-only field.
            new[] { 4, 4, 4, 4 },
        };
        private static readonly int[] LobChances = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 100 };
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
                // Read when a shot sequence is set up (server): a lob picks its own pattern row, always.
                if (_lobActive)
                {
                    return LobChances;
                }

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
            // 100 tiles (the SF4 window cap is 120): the Hero's artillery archers path across ~100 tiles of arena.
            archerGlobalNPC.NavSearchRadius = 100;
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

        // ── Hero-archer API (called by the Hero of Lumelia, server only) ──────────────────────────────────────

        /// <summary>Marks a freshly spawned Man Hunter as one of the Hero's four artillery archers. Slot picks the side
        /// (even = left, odd = right) and the interleave of its arrows; standoffTiles is the distance it prefers to hold.</summary>
        public void ConfigureAsHeroArcher(int ownerId, int ownerType, int slot, int side, float standoffTiles, Vector2 postFeet)
        {
            _ownerId = (short)ownerId;
            _ownerType = (short)ownerType;
            _slot = (byte)slot;
            _side = (sbyte)side;
            _standoffTiles = standoffTiles;
            _artillery = true;
            _forcePursue = false;
            _mode = ArcherMode.Hold;
            _postX = postFeet.X;
            _postFeetY = postFeet.Y;

            // A summoned archer is part of the encounter, not a farm: no souls (PreKill below also drops its loot).
            NPC.value = 0f;
            NPC.netUpdate = true;
        }

        /// <summary>The Hero's "volley now" cue. The archer fires only if it is holding a post with a clear arc; the
        /// order lapses after VolleyOrderLifeTicks if it can't (repositioning, engaged, mid-shot).</summary>
        public void OrderVolley(int sweepDirection)
        {
            _volleyOrderTicks = VolleyOrderLifeTicks;
            _lobSweepDir = sweepDirection;
        }

        /// <summary>Leaves the artillery role: from here it pursues like a normal Man Hunter (walking in faster until in
        /// bow range), and the Hero never respawns it.</summary>
        public void BeginJoin()
        {
            _artillery = false;
            _forcePursue = true;
            _volleyOrderTicks = 0;
            NPC.netUpdate = true;
        }

        // A summoned archer drops nothing: PreKill false also skips OnKill, so no banner kills or first-kill rewards.
        public override bool PreKill()
        {
            if (_ownerId >= 0)
            {
                return false;
            }

            return base.PreKill();
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(_ownerId);
            writer.Write(_ownerType);
            writer.Write(_slot);
            writer.Write(_side);
            writer.Write(_standoffTiles);
            writer.Write(_artillery);
            writer.Write(_forcePursue);
            writer.Write((byte)_mode);
            writer.Write(_postX);
            writer.Write(_postFeetY);
            base.SendExtraAI(writer);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            _ownerId = reader.ReadInt16();
            _ownerType = reader.ReadInt16();
            _slot = reader.ReadByte();
            _side = reader.ReadSByte();
            _standoffTiles = reader.ReadSingle();
            _artillery = reader.ReadBoolean();
            _forcePursue = reader.ReadBoolean();
            _mode = (ArcherMode)reader.ReadByte();
            _postX = reader.ReadSingle();
            _postFeetY = reader.ReadSingle();
            base.ReceiveExtraAI(reader);
        }

        // ── Archer brain ────────────────────────────────────────────────────────────────────────────────────
        // Runs before the puppet AI each tick. Decisions (mode, post, starting a lob) are the server's; every machine
        // re-asserts the pursuit state so a joined or engaged archer keeps chasing instead of falling back to Patrol.
        private void TickArcherBrain()
        {
            bool authority = Main.netMode != NetmodeID.MultiplayerClient;

            // The lob state is only meaningful while a shot pose is running; drop it the tick the pose ends.
            if (_lobActive && !InShotPose && Phase != AttackPhase.RangedRecovery)
            {
                _lobActive = false;
            }

            // The Hero's archers leave with the Hero (he died or despawned). Checked on every machine so the
            // vanish dust plays everywhere; the server deactivates the NPC.
            if (_ownerId >= 0)
            {
                bool ownerFighting = _ownerId < Main.maxNPCs && Main.npc[_ownerId].active && Main.npc[_ownerId].type == _ownerType;

                if (!ownerFighting)
                {
                    if (!_ownerLostDustDone && !Main.dedServ)
                    {
                        _ownerLostDustDone = true;

                        for (int i = 0; i < 30; i++)
                        {
                            Dust vanishDust = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, DustID.MagicMirror, 0f, -1f, 100, default, 1.4f);
                            vanishDust.noGravity = true;
                        }
                    }

                    if (authority)
                    {
                        NPC.active = false;
                        NPC.netUpdate = true;
                    }

                    return;
                }
            }

            // Joined and engaged archers chase: pin the pursuit state and zero its give-up clock every tick. Without
            // this a fresh spawn sits in Patrol (re-aggro needs the player within 700px) and wanders at its spawn point.
            bool pursuing = _forcePursue || (_artillery && _mode == ArcherMode.Engaged);

            if (pursuing)
            {
                tsorcRevampGlobalNPC globalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
                globalNPC.PursuitState = PursuitState.Pursue;
                globalNPC.DisengageTimer = 0;
            }

            if (!authority)
            {
                return;
            }

            if (_artillery)
            {
                TickArtillery();
            }
            else if (_ownerId < 0 && Main.hardMode)
            {
                TickWildLob();
            }
        }

        // Artillery archer: pick a mode from the target's distance, then (if ordered and holding) start the volley.
        private void TickArtillery()
        {
            _retargetTimer--;

            if (_retargetTimer <= 0 || !NPC.HasValidTarget)
            {
                NPC.TargetClosest(false);
                _retargetTimer = RetargetTicks;
            }

            if (!NPC.HasValidTarget)
            {
                return;
            }

            Player target = Main.player[NPC.target];
            float distanceTiles = NPC.Distance(target.Center) / 16f;
            ArcherMode previousMode = _mode;

            if (_mode == ArcherMode.Engaged)
            {
                // Hysteresis: engaged at 45 tiles, released at 58, so a player dancing on the line doesn't flip it every tick.
                if (distanceTiles > EngageReleaseTiles)
                {
                    _mode = ArcherMode.Hold;
                    _repostCooldown = 0;
                }
            }
            else if (distanceTiles <= EngageTiles)
            {
                _mode = ArcherMode.Engaged;
            }
            else if (_mode == ArcherMode.Reposition)
            {
                // Walking to a new post: done on arrival, when the nav reports it can't get closer, or on a timeout.
                _repositionTicks++;
                SmartFighter4WaypointStatus status = SmartFighter4AI.GetWaypointStatus(NPC);
                bool atPost = Math.Abs(NPC.Center.X - _postX) < 24f;
                bool navDone = status == SmartFighter4WaypointStatus.Reached || status == SmartFighter4WaypointStatus.Blocked;

                if (atPost || navDone || _repositionTicks > RepositionTimeoutTicks)
                {
                    _mode = ArcherMode.Hold;
                    _repostCooldown = 90;
                }
            }
            else
            {
                // Holding: stay put while the target is anywhere in the 60-125 tile band (this is what ignores
                // micro-movement). Outside it, find a new post on this archer's side at its stand-off distance.
                _repostCooldown--;
                bool outsideBand = distanceTiles < HoldMinTiles || distanceTiles > HoldMaxTiles;

                if (outsideBand && _repostCooldown <= 0)
                {
                    float desiredX = target.Center.X + _side * _standoffTiles * 16f;
                    int referenceFeetTileY = (int)(target.Bottom.Y / 16f);
                    Vector2 postFeet;
                    bool foundPost = TryFindPost(desiredX, referenceFeetTileY, out postFeet);

                    if (foundPost)
                    {
                        _postX = postFeet.X;
                        _postFeetY = postFeet.Y;
                        _mode = ArcherMode.Reposition;
                        _repositionTicks = 0;
                    }
                    else
                    {
                        _repostCooldown = 120;
                    }
                }
            }

            if (_mode != previousMode)
            {
                NPC.netUpdate = true;
            }

            if (_volleyOrderTicks > 0)
            {
                _volleyOrderTicks--;

                if (_mode == ArcherMode.Hold && IsNeutral)
                {
                    bool started = TryStartLob(target);

                    if (started)
                    {
                        _volleyOrderTicks = 0;
                    }
                }
            }
        }

        // Wild Hardmode Man Hunter: occasionally lob a five-arrow line at a player who is past bow range but still on screen.
        private void TickWildLob()
        {
            if (_wildLobCooldown > 0)
            {
                _wildLobCooldown--;
                return;
            }

            if (!NPC.HasValidTarget || !IsNeutral)
            {
                return;
            }

            Player target = Main.player[NPC.target];
            float distance = NPC.Distance(target.Center);
            bool inLobRange = distance >= WildLobMinRange && distance <= WildLobMaxRange;

            if (!inLobRange || !Main.rand.NextBool(WildLobRollOneIn))
            {
                return;
            }

            _lobSweepDir = Main.rand.NextBool() ? 1 : -1;
            bool started = TryStartLob(target);

            if (started)
            {
                _wildLobCooldown = Main.rand.Next(WildLobCooldownMin, WildLobCooldownMax + 1);
            }
            else
            {
                _wildLobCooldown = 120;
            }
        }

        // Server: begins a lob volley if every arrow's climb column and descent line are clear of tiles. SetupRangedBurst
        // (shotsOverride -1) picks the pattern row from PrimaryRangedBurstChances, which returns LobChances while
        // _lobActive is set.
        private bool TryStartLob(Player target)
        {
            _lobActive = true;

            for (int i = 0; i < LobArrowCount; i++)
            {
                _lobJitter[i] = Main.rand.NextFloat(-LobJitterPx, LobJitterPx);
            }

            UpdateLobAnchor(target);

            Vector2 origin = NPC.Top + new Vector2(0f, 14f);
            Vector2 climbEnd = origin + LobLaunchDirection() * (ManHunterLobArrow.AscendSpeed * ManHunterLobArrow.AscendTicks);
            bool pathClear = climbEnd.Y > 160f && Collision.CanHitLine(origin, 1, 1, climbEnd, 1, 1);

            // Each arrow re-enters FallHeight above its landing point: that whole line has to be open sky.
            for (int i = 0; i < LobArrowCount && pathClear; i++)
            {
                Vector2 landing;
                int descentDirection;
                ComputeLobArrow(i, out landing, out descentDirection);

                Vector2 fallStart = landing + new Vector2(-descentDirection * ManHunterLobArrow.FallHeight, -ManHunterLobArrow.FallHeight);
                Vector2 fallEnd = landing + new Vector2(0f, -12f);
                pathClear = fallStart.Y > 160f && Collision.CanHitLine(fallStart, 1, 1, fallEnd, 1, 1);
            }

            if (!pathClear)
            {
                _lobActive = false;
                return false;
            }

            SetupRangedBurst(false, -1, true);
            EnterPhase(AttackPhase.RangedTelegraph, RangedTelegraphTicks);
            NPC.netUpdate = true;
            return true;
        }

        // The volley's landing line is anchored on the target's feet and frozen when the first arrow's aim locks.
        private void UpdateLobAnchor(Player target)
        {
            _lobAnchor = new Vector2(target.Center.X, target.Bottom.Y - 6f);
        }

        // Nearly straight up, leaning toward the anchor. Every arrow of a volley climbs along this one direction, and the
        // drawn bow points along it (SolveShotAim), so the arrow leaves where the bow points.
        private Vector2 LobLaunchDirection()
        {
            int lean = -1;

            if (_lobAnchor.X >= NPC.Center.X)
            {
                lean = 1;
            }

            Vector2 direction = new Vector2(lean * LobAimLean, -1f);
            direction.Normalize();
            return direction;
        }

        // Where arrow arrowIndex lands and which way it falls (-1 down-left, 0 straight down, +1 down-right). The landing
        // points are a line across the anchor: the artillery's 4 archers x 5 arrows interleave into 20 points over
        // +/-418px, a wild Man Hunter's 5 arrows are a tight line; _lobSweepDir flips the order the line is walked.
        private void ComputeLobArrow(int arrowIndex, out Vector2 landing, out int descentDirection)
        {
            float spacing = WildArrowSpacing;
            float firstOffset = -2f * WildArrowSpacing;

            if (_ownerId >= 0 && _artillery)
            {
                spacing = ArtilleryArrowSpacing;
                firstOffset = -ArtilleryHalfSweep + ArtillerySlotSpacing * _slot;
            }

            float offsetX = (firstOffset + spacing * arrowIndex) * _lobSweepDir;
            landing = new Vector2(_lobAnchor.X + offsetX + _lobJitter[arrowIndex], _lobAnchor.Y);
            descentDirection = LobDescentPattern[(arrowIndex + _slot) % LobDescentPattern.Length];
        }

        /// <summary>
        /// Finds a standable spot near desiredX: solid floor, a flat 3-tile top, and PostClearAboveTiles of air above,
        /// within PostSearchTiles of desiredX and PostSearchHeightTiles of referenceFeetTileY. Prefers the smallest
        /// horizontal distance, then the smallest height difference. Returns the feet position (bottom-centre).
        /// </summary>
        public static bool TryFindPost(float desiredX, int referenceFeetTileY, out Vector2 postFeet)
        {
            postFeet = Vector2.Zero;
            int desiredTileX = (int)(desiredX / 16f);
            int searchTiles = (int)PostSearchTiles;
            int bestTileY = int.MinValue;
            int bestTileX = 0;
            int bestHeightGap = int.MaxValue;

            for (int offset = 0; offset <= searchTiles; offset++)
            {
                for (int sign = -1; sign <= 1; sign += 2)
                {
                    if (offset == 0 && sign == 1)
                    {
                        continue;
                    }

                    int tileX = desiredTileX + offset * sign;

                    if (tileX < 10 || tileX > Main.maxTilesX - 10)
                    {
                        continue;
                    }

                    for (int heightOffset = 0; heightOffset <= PostSearchHeightTiles; heightOffset++)
                    {
                        for (int heightSign = -1; heightSign <= 1; heightSign += 2)
                        {
                            int floorTileY = referenceFeetTileY + heightOffset * heightSign;

                            if (floorTileY < 10 || floorTileY > Main.maxTilesY - 10)
                            {
                                continue;
                            }

                            bool standable = WorldGen.SolidTile(tileX, floorTileY)
                                && WorldGen.SolidTile(tileX - 1, floorTileY)
                                && WorldGen.SolidTile(tileX + 1, floorTileY);

                            if (!standable)
                            {
                                continue;
                            }

                            bool clearAbove = true;

                            for (int rise = 1; rise <= PostClearAboveTiles; rise++)
                            {
                                if (WorldGen.SolidTile(tileX, floorTileY - rise))
                                {
                                    clearAbove = false;
                                    break;
                                }
                            }

                            if (clearAbove && heightOffset < bestHeightGap)
                            {
                                bestHeightGap = heightOffset;
                                bestTileX = tileX;
                                bestTileY = floorTileY;
                            }
                        }
                    }

                    // The ring at this horizontal offset produced a post: nearest-in-x wins, so stop searching outward.
                    if (bestTileY != int.MinValue)
                    {
                        postFeet = new Vector2(bestTileX * 16f + 8f, bestTileY * 16f);
                        return true;
                    }
                }
            }

            return false;
        }

        // Hold: the archer plants at its post and watches its target. Reposition: SF4 walks it to the post as a waypoint
        // (movementOnly pursuit would chase the player at distance 0). Engaged and every non-artillery archer use the
        // stock Man Hunter movement, with a faster walk while a joiner is still outside bow range.
        protected override void RunMovementAI(float speedMult)
        {
            if (_artillery && _mode == ArcherMode.Hold)
            {
                NPC.velocity.X *= 0.8f;

                if (NPC.HasValidTarget)
                {
                    int facing = Main.player[NPC.target].Center.X >= NPC.Center.X ? 1 : -1;
                    NPC.direction = facing;
                    NPC.spriteDirection = facing;
                }

                return;
            }

            if (_artillery && _mode == ArcherMode.Reposition)
            {
                Vector2 waypoint = new Vector2(_postX, _postFeetY - NPC.height * 0.5f);
                SmartFighter4AI.Run(NPC,
                    topSpeed: TopSpeed * speedMult * RepositionSpeedMult,
                    acceleration: Acceleration,
                    doorBreakingDamage: 4,
                    attackRange: RangedRange,
                    movementWaypoint: waypoint);
                return;
            }

            float walkMult = 1f;

            if (_forcePursue && NPC.HasValidTarget && NPC.Distance(Main.player[NPC.target].Center) > MaxShotRange)
            {
                walkMult = JoinWalkSpeedMult;
            }

            base.RunMovementAI(speedMult * walkMult);
        }

        // Lob mode swaps the direct-fire intercept for the climb direction (nearly straight up), so the drawn bow points
        // along the arrows it is about to loose. Arrow 0's call keeps tracking the player until the aim locks, which
        // only decides the lean; later arrows reuse the frozen anchor.
        protected override Vector2 SolveShotAim(Player target)
        {
            if (!_lobActive)
            {
                return base.SolveShotAim(target);
            }

            if (_shotsThisVolley == 0)
            {
                UpdateLobAnchor(target);
            }

            return LobLaunchDirection();
        }

        // Clients (and any direct shot) fall through to the base: sound only, no projectile. _lobActive is server-only.
        protected override void DoRangedAttack()
        {
            if (!_lobActive)
            {
                base.DoRangedAttack();
                return;
            }

            int arrowIndex = Math.Min(_shotsThisVolley, LobArrowCount - 1);
            _shotsThisVolley++;
            Vector2 origin = PuppetBowGripPosition;

            if (!Main.dedServ)
            {
                SoundEngine.PlaySound(SoundID.Item5 with { Volume = 0.7f, PitchVariance = 0.15f }, origin);
            }

            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            Vector2 landing;
            int descentDirection;
            ComputeLobArrow(arrowIndex, out landing, out descentDirection);
            Vector2 launch = LobLaunchDirection() * ManHunterLobArrow.AscendSpeed;

            Projectile.NewProjectile(NPC.GetSource_FromAI(), origin, launch,
                ModContent.ProjectileType<ManHunterLobArrow>(), EnemyDamage.Projectile(LobExpertDamage), 0f, Main.myPlayer,
                landing.X, landing.Y, descentDirection);
        }

        public override void AI()
        {
            EquipWeapon(PuppetHandSlot.Front, Knife);

            TickArcherBrain();

            base.AI();

            // The puppet body never deals contact damage; the knife and arrows are the only hits.
            NPC.damage = 0;

            DebugAttackLabel = null;

            if (_lobActive)
            {
                DebugAttackLabel = "Arrow Volley";
            }
            else if (InShotPose || Phase == AttackPhase.RangedRecovery)
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

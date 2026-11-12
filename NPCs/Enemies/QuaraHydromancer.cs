using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.Config;
using tsorcRevamp.Content.Items.Armor.Enemy.QuaraHydromancerSet;
using tsorcRevamp.Content.Items.Materials.Souls;
using tsorcRevamp.Content.Items.Weapons.Enemy;
using tsorcRevamp.Content.Projectiles.Enemy;
using tsorcRevamp.Content.Projectiles.Enemy.IceGigas;
using tsorcRevamp.Content.Projectiles.Enemy.Quara;
using tsorcRevamp.NPCs.Bosses.SuperHardMode.Fiends;
using tsorcRevamp.NPCs.Invaders;
using tsorcRevamp.Systems;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.NPCs.Enemies
{
    // A kiting water-mage displaced into the Hallow, with an actual gameplan: SOAK the target (bubbles /
    // wave / droplets / the Tide Rush puddle all apply Wet), because ink sticks to a wet target: the Ink
    // Geyser's Blackout and Broken Armor last TWICE as long against a soaked player.
    //
    // Kit: Bubble Barrage (Wet), Tidal Crest (jumpable ground wave, Wet + Chilled), Burst Bubble (its long
    // staggerable cast -> drifting bubble that pops into droplets), Ink Geyser (lands at the PLAYER's
    // position; gated on the Water Fiend Kraken kill, always available in SHM), three frost casts, and
    // Tide Rush: it collapses into a rushing puddle-surge, re-forms at kiting distance, then freezes the
    // water trail it left behind.
    //
    // Puppet body: wears the Quara Hydromancer armor and holds its 86px coral staff upright in the REAR
    // hand (the Dworc caster rig). Every spell forms at the staff's measured orb. All selection, spawns and
    // payouts are server-side; clients follow the synced State + AttackTimer clock, and every pose is
    // derived from that clock so peers agree without extra sync.
    //
    // Poise contract (0.35/30 in PoiseProfiles): casts are hyper-armored; Burst Bubble's long cast is
    // staggerable for its first two thirds. Shared evasion (retreat-jump / quick-step) only from neutral.
    class QuaraHydromancer : PuppetNPC
    {
        enum AttackState : byte
        {
            None = 0,
            BubbleBarrage, // three soaking bubbles, each bobbing on its own phase
            TidalCrest,    // the breaking wave: jump it or roll through
            BurstBubble,   // long staggerable cast -> big drifting bubble, pops into droplets
            InkGeyser,     // the payoff: ink cloud at the player's position (Wet doubles its stick)
            TideRush,      // repositioning surge: retreat, or flank to the player's far side
            IceArcVolley,  // half-ring gather -> six upward arcing, briefly guided frost shards
            IceOverflight, // spiral gather -> shards route over and behind before their pursuit line
            FrostSprites,  // delayed, terrain-safe frost sprites seed light-homing shard clumps
        }

        //Timings (ticks)
        const int BarrageTelegraphTicks = 32;
        const int CrestTelegraphTicks = 30;
        const int CrestSlamStartTick = 22; //the staff comes down over the last 8 tell ticks, landing on the release
        const int BurstCastTicks = 60;
        const int BurstStaggerableTicks = 40; //first two thirds: a poise break pops the cast
        const int InkTelegraphTicks = 35;
        const int InkThrustStartTick = 25;
        const int RushDissolveTicks = 15;
        const int RushMaxSurgeTicks = 60;
        const int RushReformTicks = 10;
        const int IceTelegraphTicks = 40;
        const int IceRecoveryTicks = 20;
        const int PoseRaiseTicks = 18;
        const int PoseLowerTicks = 20;
        const float IceGuidedThirty = 1f;
        const float IceOverflightThirty = 2f;
        const float IceGuidedSixty = 3f;
        const float IceWaterIgniter = 4f;

        // Staff pose angles. Arm: 0 = level forward, negative = raised, positive = lowered (Dworc convention).
        // Tilt: staff texture rotation in facing space, 0 = upright, positive = orb tips toward the target.
        const float RestArmAngle = 0.2f;
        const float RestStaffTilt = 0.04f;
        const float RaisedArmAngle = -1.15f;
        const float RaisedStaffTilt = -0.12f;
        const float AimedArmAngle = -0.65f;

        // QuaraHydromancerStaff.png (22x86, upright, 2x2 pixel art): shaft x 6-12, butt edge y 86, orb
        // centre (10, 14). Grip 20px above the butt so it rests near the feet in idle; tip = orb centre - grip.
        static readonly Vector2 StaffGrip = new Vector2(9f / 22f, 66f / 86f);
        static readonly Vector2 StaffTipOffset = new Vector2(1f, -52f);

        AttackState State = AttackState.None;
        int AttackTimer;
        int AttackCooldown = 90;
        int rushDir = 1;          //surge direction, locked at dissolve end
        float rushDestX;          //where the surge re-forms
        Vector2 rushStartGround;  //server-authoritative target for Tide Rush's returning ice volley
        int lastRushWaterTileX = int.MinValue;
        bool rushIceVolleyFired;
        bool statsInitialized;
        bool staffEquipped;
        bool HM;
        bool SHM;
        float savedKnockBackResist = -1f; //restored after the surge's brief knockback immunity
        AttackBag<AttackState> attacks;

        bool Authority => Main.netMode != NetmodeID.MultiplayerClient;
        bool Neutral => State == AttackState.None;
        int RushReformStartTick => RushDissolveTicks + RushMaxSurgeTicks;
        int RushIceTelegraphStartTick => RushReformStartTick + RushReformTicks;
        bool RushSurging => State == AttackState.TideRush && AttackTimer > RushDissolveTicks && AttackTimer <= RushReformStartTick;

        bool InkUnlocked => tsorcRevampWorld.SuperHardMode
            || tsorcRevampWorld.NewSlain.ContainsKey(new NPCDefinition(ModContent.NPCType<WaterFiendKraken>()));

        //Damage tiers preserved from the pre-puppet version: declared here in Expert units (raw x4) and
        //converted once at each root spawn by EnemyDamage.Projectile, so in-game hits are unchanged.
        int BubbleExpertDamage
        {
            get
            {
                if (SHM)
                {
                    return 220;
                }
                if (HM)
                {
                    return 180;
                }
                return 132;
            }
        }

        int CrestExpertDamage
        {
            get
            {
                if (SHM)
                {
                    return 200;
                }
                if (HM)
                {
                    return 160;
                }
                return 120;
            }
        }

        int BurstExpertDamage
        {
            get
            {
                if (SHM)
                {
                    return 220;
                }
                if (HM)
                {
                    return 180;
                }
                return 140;
            }
        }

        int InkExpertDamage
        {
            get
            {
                if (SHM)
                {
                    return 220;
                }
                if (HM)
                {
                    return 180;
                }
                return 132;
            }
        }

        int IceExpertDamage
        {
            get
            {
                if (SHM)
                {
                    return 152;
                }
                if (HM)
                {
                    return 120;
                }
                return 88;
            }
        }

        #region Puppet configuration
        public override bool UsesPuppetDifficultyScaling => false;
        protected override bool AnnounceInvasion => false;
        protected override bool AnnounceInvaderDefeat => false;
        protected override bool DespawnsOnPartyWipe => false;
        protected override string InvaderTitle => "Quara Hydromancer";
        protected override int HeadArmorItemType => ModContent.ItemType<QuaraHydromancerHeadpiece>();
        protected override int BodyArmorItemType => ModContent.ItemType<QuaraHydromancerBreastplate>();
        protected override int LegsArmorItemType => ModContent.ItemType<QuaraHydromancerGreaves>();
        protected override int MeleeWeaponItemType => -1;
        protected override int RangedWeaponItemType => -1;
        protected override int MagicWeaponItemType => ModContent.ItemType<QuaraHydromancerStaff>();
        protected override int MeleeDamage => 0;
        protected override int RangedDamage => 0;
        protected override int EstusChargesMax => 0;
        protected override int CasualStrollChance => 0;
        protected override int UnblockableFinisherBlocks => 0;
        protected override bool HoldAttackSelection => true;
        protected override bool ShowWeaponDuringNeutral => true;
        protected override bool IndependentBackHandWeapon => true;
        protected override float IndependentBackHandFacingAngle => StaffArmAngle;
        protected override float IndependentBackHandDrawRotation => StaffTilt;
        // Each cast brakes with its own pre-puppet damping, and the Tide Rush surge drives velocity itself.
        protected override bool SlowDownDuringCustom => false;
        protected override bool CustomAttackCommitted => IsCommitted();
        // Tide Rush writes NPC.alpha during the surge; the puppet body, armor and staff all fade with it.
        internal override float BodyOpacity => 1f - NPC.alpha / 255f;
        protected override float PuppetJumpPower => 9f;
        protected override float PuppetJumpBoost => 4f;
        protected override float RunSpeedMult => 1f;
        protected override float TopSpeed => 2f;
        protected override float Acceleration => 0.05f;
        protected override Color PuppetSkinColor => new Color(92, 156, 92);
        // The OnKill water spray below is the death burst; the base blood burst is switched off.
        protected override int OnKillDustCount => 0;
        #endregion

        public override void SetStaticDefaults()
        {
            // Kept so the bestiary and other resource consumers still slice the old 15-frame sheet.
            Main.npcFrameCount[Type] = 15;
        }

        public override void SetDefaults()
        {
            NPC.npcSlots = 5;
            AnimationType = -1;
            NPC.aiStyle = -1;
            NPC.damage = 0;
            NPC.defense = 22;
            NPC.width = 18;
            NPC.height = 42; //puppet standard (was 45); bottom-aligned, so the feet don't move
            NPC.lifeMax = 250;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.value = 1500;
            NPC.lavaImmune = true;
            NPC.knockBackResist = 0.25f; //overridden by the PoiseProfiles entry
            Banner = NPC.type;
            BannerItem = ModContent.ItemType<Banners.QuaraHydromancerBanner>();

            tsorcRevampGlobalNPC global = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            global.NavSearchRadius = 60;
            global.CanWalkBackwards = true;
            global.PoiseStaggerResetsAI = true;
            //A caster keeps its distance and backpedals while it works
            global.KiteRangeMin = 8f;
            global.KiteRangeMax = 16f;
            global.KiteLooseness = 0.25f;
            //Slippery: shared on-hit evasion, gated to neutral in OnHitBy*
            global.EvasiveRetreatJump = true;
            global.EvasiveQuickStep = true;
        }

        protected override void DoMeleeAttack()
        {
            // The held staff is a prop; the cast clock owns all outgoing damage.
        }

        protected override void DoRangedAttack()
        {
            // No parallel ranged selector: every projectile is released by the cast clock.
        }

        protected override void RunMovementAI(float speedMult)
        {
            if (!Neutral)
            {
                return;
            }

            SmartFighter4AI.Run(NPC, TopSpeed * speedMult, Acceleration, doorBreakingDamage: 0, attackRange: 700f);
        }

        public override void AI()
        {
            tsorcRevampGlobalNPC global = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            if (!staffEquipped)
            {
                staffEquipped = true;
                EquipWeapon(PuppetHandSlot.Back, new PuppetWeapon(MagicWeaponItemType, damage: 0,
                    handleNorm: StaffGrip, rotationOffset: MathHelper.PiOver2));
            }
            InitializeStats();

            //Idle identity: dripping wet, faint blue staff glow, the occasional gurgle
            if (!Main.dedServ)
            {
                if (Main.rand.NextBool(8))
                {
                    Vector2 dripPosition = NPC.position + new Vector2(Main.rand.NextFloat(NPC.width), Main.rand.NextFloat(NPC.height * 0.6f));
                    int drip = Dust.NewDust(dripPosition, 4, 4, DustID.Water, 0f, 1f, 100, default, 0.8f);
                    Main.dust[drip].velocity *= 0.3f;
                }
                Lighting.AddLight(StaffTip, 0.1f, 0.18f, 0.3f);
                if (Main.rand.NextBool(1000))
                {
                    SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.3f, Pitch = -0.3f }, NPC.Center); // water sound
                }
            }

            if (Authority && Neutral && global.StaggerTimer <= 0)
            {
                if (AttackCooldown > 0)
                {
                    AttackCooldown--;
                }

                bool canPick = AttackCooldown <= 0 && NPC.velocity.Y == 0f && !global.InEvasion;
                if (canPick)
                {
                    NPC.TargetClosest(false);
                    Player candidate = Main.player[NPC.target];
                    bool targetValid = NPC.HasValidTarget && NPC.Distance(candidate.Center) < 1000f;
                    if (targetValid)
                    {
                        PickAttack(candidate);
                    }
                }
            }

            if (!Neutral)
            {
                // Keep the Custom phase alive for as long as the cast clock runs (DoCustomTick ends it).
                PhaseTimer = 2;
                FaceForCast();
            }

            base.AI();

            if (!Neutral)
            {
                FaceForCast();
                global.AttackCommitted = CustomAttackCommitted;
                global.AttackTelegraphing = State == AttackState.BurstBubble && AttackTimer < BurstStaggerableTicks && !global.AttackCommitted;
            }

            // 85% transparent while racing along as a puddle.
            NPC.alpha = 0;
            if (RushSurging)
            {
                NPC.alpha = 217;
            }

            DebugAttackLabel = DescribeState() + "/" + AttackTimer;
        }

        //Face the target while casting, except mid-surge, where the locked rush direction leads.
        void FaceForCast()
        {
            int facing = NPC.direction;
            if (RushSurging)
            {
                facing = rushDir;
            }
            else
            {
                Player target = Main.player[NPC.target];
                facing = target.Center.X > NPC.Center.X ? 1 : -1;
            }

            NPC.direction = facing;
            NPC.spriteDirection = facing;
        }

        string DescribeState()
        {
            switch (State)
            {
                case AttackState.BubbleBarrage:
                    return "Bubble Barrage";
                case AttackState.TidalCrest:
                    return "Tidal Crest";
                case AttackState.BurstBubble:
                    return "Burst Bubble";
                case AttackState.InkGeyser:
                    return "Ink Geyser";
                case AttackState.TideRush:
                    return "Tide Rush";
                case AttackState.IceArcVolley:
                    return "Ice Arc Volley";
                case AttackState.IceOverflight:
                    return "Ice Overflight";
                case AttackState.FrostSprites:
                    return "Frost Sprite Barrage";
                default:
                    return "Neutral";
            }
        }

        void InitializeStats()
        {
            if (statsInitialized)
            {
                return;
            }
            statsInitialized = true;
            if (!Authority)
            {
                return; //clients receive the tier flags via sync
            }
            //World flags aren't reliable in SetDefaults, so the tier stats are applied on the first AI tick
            if (tsorcRevampWorld.SuperHardMode)
            {
                SHM = true;
                NPC.lifeMax = 1500;
                NPC.defense = 50;
                NPC.value = 3600;
            }
            else if (Main.hardMode)
            {
                HM = true;
                NPC.lifeMax = 500;
            }
            NPC.life = NPC.lifeMax;
            // New Enemy Balance: the registry value replaces the line above when enabled. It is applied here, after vanilla scaling, so the helper applies the Expert / Master multiplier.
            EnemyBalance.ApplyLateLife(NPC);
            NPC.netUpdate = true;
        }

        #region Selection
        // Tide Rush is a distance reaction rolled before the bag (35% under 10 tiles, else 15%: today's
        // effective share of the old weighted pool). Everything else is dealt from a 17-card bag whose
        // counts follow the old weights, so low-weight casts can no longer starve.
        void PickAttack(Player player)
        {
            float distanceTiles = NPC.Distance(player.Center) / 16f;
            float rushChance = 0.15f;
            if (distanceTiles < 10f)
            {
                rushChance = 0.35f;
            }

            AttackState chosen = AttackState.TideRush;
            float rushRoll = Main.rand.NextFloat();
            if (rushRoll >= rushChance)
            {
                attacks ??= new AttackBag<AttackState>(RefillAttacks);
                bool drew = attacks.TryDraw(IsEligible, out chosen);
                if (!drew)
                {
                    chosen = AttackState.BubbleBarrage;
                }
            }

            State = chosen;
            AttackTimer = 0;
            NPC.GetGlobalNPC<tsorcRevampGlobalNPC>().EvasiveOnHitRequested = false;
            StartCustomAttack(2);
            NPC.netUpdate = true;
        }

        static void RefillAttacks(List<AttackState> cards)
        {
            AddCards(cards, AttackState.BubbleBarrage, 3);
            AddCards(cards, AttackState.TidalCrest, 3);
            AddCards(cards, AttackState.BurstBubble, 2);
            AddCards(cards, AttackState.InkGeyser, 2);
            AddCards(cards, AttackState.IceArcVolley, 3);
            AddCards(cards, AttackState.IceOverflight, 2);
            AddCards(cards, AttackState.FrostSprites, 2);
        }

        static void AddCards(List<AttackState> cards, AttackState card, int count)
        {
            for (int i = 0; i < count; i++)
            {
                cards.Add(card);
            }
        }

        bool IsEligible(AttackState candidate)
        {
            Player target = Main.player[NPC.target];
            float distanceTiles = NPC.Distance(target.Center) / 16f;
            bool sameLevel = Math.Abs(target.Bottom.Y - NPC.Bottom.Y) < 4 * 16f;

            switch (candidate)
            {
                case AttackState.TidalCrest:
                    return distanceTiles < 18f && sameLevel;
                case AttackState.InkGeyser:
                    return InkUnlocked;
                case AttackState.IceArcVolley:
                    return distanceTiles < 34f;
                case AttackState.IceOverflight:
                    return distanceTiles < 38f;
                case AttackState.FrostSprites:
                    return distanceTiles < 32f;
                default:
                    return true;
            }
        }
        #endregion

        void EndAttack(int cooldown)
        {
            State = AttackState.None;
            AttackTimer = 0;
            RestoreKnockback();
            EnterPhase(AttackPhase.Idle, 0);
            if (Authority)
            {
                AttackCooldown = cooldown + Main.rand.Next(40);
                NPC.netUpdate = true;
            }
        }

        void RestoreKnockback()
        {
            if (savedKnockBackResist >= 0f)
            {
                NPC.knockBackResist = savedKnockBackResist;
                savedKnockBackResist = -1f;
            }
        }

        // Hyper-armored windows, unchanged from the pre-puppet commit flags. Recoveries stay open.
        bool IsCommitted()
        {
            switch (State)
            {
                case AttackState.BubbleBarrage:
                    return AttackTimer <= BarrageTelegraphTicks + 12;
                case AttackState.TidalCrest:
                    return AttackTimer <= CrestTelegraphTicks;
                case AttackState.BurstBubble:
                    return AttackTimer >= BurstStaggerableTicks && AttackTimer <= BurstCastTicks;
                case AttackState.InkGeyser:
                    return AttackTimer <= InkTelegraphTicks;
                case AttackState.IceArcVolley:
                case AttackState.IceOverflight:
                case AttackState.FrostSprites:
                    return AttackTimer <= IceTelegraphTicks;
                case AttackState.TideRush:
                    bool dissolvingOrSurging = AttackTimer <= RushReformStartTick;
                    bool iceTell = AttackTimer > RushIceTelegraphStartTick && AttackTimer <= RushIceTelegraphStartTick + IceTelegraphTicks;
                    return dissolvingOrSurging || iceTell;
                default:
                    return false;
            }
        }

        /// <summary>Poise break (neutral or the Burst Bubble's staggerable window): the cast pops early
        /// and harmlessly in its hands. Already-released projectiles live on.</summary>
        public override void OnStagger(NPC npc)
        {
            base.OnStagger(npc);
            if (State != AttackState.None && !Main.dedServ)
            {
                SoundEngine.PlaySound(SoundID.Item54 with { Volume = 0.7f, Pitch = 0.3f }, NPC.Center);
                for (int i = 0; i < 14; i++)
                {
                    Vector2 velocity = Main.rand.NextVector2Circular(3.5f, 3.5f);
                    int dust = Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Water, velocity.X, velocity.Y, 60, default, 1.3f);
                    Main.dust[dust].noGravity = true;
                }
            }
            State = AttackState.None;
            AttackTimer = 0;
            RestoreKnockback();
            AttackCooldown = Math.Max(AttackCooldown, 70);
        }

        // Runs every tick of the Custom phase on every machine; spawns and rolls stay behind Authority.
        protected override void DoCustomTick(int ticksRemaining)
        {
            if (Neutral)
            {
                return;
            }

            Player player = Main.player[NPC.target];
            if (player.dead || !player.active)
            {
                EndAttack(60);
                return;
            }

            AttackTimer++;
            switch (State)
            {
                case AttackState.BubbleBarrage:
                    RunBubbleBarrage(player);
                    break;
                case AttackState.TidalCrest:
                    RunTidalCrest();
                    break;
                case AttackState.BurstBubble:
                    RunBurstBubble();
                    break;
                case AttackState.InkGeyser:
                    RunInkGeyser(player);
                    break;
                case AttackState.TideRush:
                    RunTideRush(player);
                    break;
                case AttackState.IceArcVolley:
                    RunIceArcVolley(player);
                    break;
                case AttackState.IceOverflight:
                    RunIceOverflight(player);
                    break;
                case AttackState.FrostSprites:
                    RunFrostSprites(player);
                    break;
            }
        }

        #region Attacks
        void RunBubbleBarrage(Player player)
        {
            NPC.velocity.X *= 0.85f;

            if (AttackTimer <= BarrageTelegraphTicks && !Main.dedServ)
            {
                if (AttackTimer == 1)
                {
                    SoundEngine.PlaySound(SoundID.Item87 with { Volume = 0.6f, Pitch = -0.2f }, NPC.Center);
                }
                //BLUE: water gathering into the staff
                float progress = AttackTimer / (float)BarrageTelegraphTicks;
                for (int i = 0; i < 1 + (int)progress; i++)
                {
                    float angle = Main.rand.NextFloat(MathHelper.TwoPi);
                    Vector2 position = StaffTip + angle.ToRotationVector2() * Main.rand.NextFloat(8f, 24f);
                    int dust = Dust.NewDust(position, 4, 4, DustID.Water, 0f, 0f, 80, default, 1f);
                    Main.dust[dust].noGravity = true;
                    Main.dust[dust].velocity = (StaffTip - position) * 0.15f;
                }
                Lighting.AddLight(StaffTip, 0.15f * progress, 0.3f * progress, 0.5f * progress);
            }

            //Three soaking bubbles in a loose spread, re-aimed per shot
            bool shotTick = AttackTimer == BarrageTelegraphTicks || AttackTimer == BarrageTelegraphTicks + 6 || AttackTimer == BarrageTelegraphTicks + 12;
            if (shotTick)
            {
                if (Authority)
                {
                    Vector2 aim = (player.Center - StaffTip).SafeNormalize(new Vector2(NPC.direction, 0f));
                    Vector2 velocity = aim.RotatedBy(Main.rand.NextFloat(-0.15f, 0.15f)) * 6f;
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), StaffTip, velocity,
                        ModContent.ProjectileType<Bubble>(), EnemyDamage.Projectile(BubbleExpertDamage), 1f, Main.myPlayer, 0f, 1f); //ai[1]=1: soaking mode
                }
                if (!Main.dedServ)
                {
                    SoundEngine.PlaySound(SoundID.Item87 with { Volume = 0.5f, Pitch = 0.2f }, NPC.Center);
                }
            }

            if (AttackTimer >= BarrageTelegraphTicks + 12 + 20)
            {
                EndAttack(140);
            }
        }

        void RunTidalCrest()
        {
            NPC.velocity.X *= 0.8f;

            if (AttackTimer <= CrestTelegraphTicks)
            {
                if (!Main.dedServ)
                {
                    if (AttackTimer == 1)
                    {
                        SoundEngine.PlaySound(SoundID.Item21 with { Volume = 0.6f, Pitch = -0.4f }, NPC.Center);
                    }
                    //CYAN at the staff while water visibly mounds at its feet: the wave forming
                    if (Main.rand.NextBool(3))
                    {
                        int staffDust = Dust.NewDust(StaffTip, 4, 4, DustID.Water, 0f, -1f, 60, default, 1.2f);
                        Main.dust[staffDust].noGravity = true;
                        Vector2 moundPosition = new Vector2(NPC.position.X + Main.rand.NextFloat(NPC.width) + NPC.direction * 12f, NPC.Bottom.Y - 6f);
                        int mound = Dust.NewDust(moundPosition, 4, 4, DustID.Water, 0f, -2f, 80, default, 1.2f);
                        Main.dust[mound].noGravity = true;
                    }
                }
                if (AttackTimer == CrestTelegraphTicks)
                {
                    if (!Main.dedServ)
                    {
                        SoundEngine.PlaySound(SoundID.SplashWeak with { Volume = 0.7f, Pitch = -0.3f }, NPC.Bottom);
                    }
                    if (Authority)
                    {
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), NPC.Bottom + new Vector2(NPC.direction * 20f, -22f), Vector2.Zero,
                            ModContent.ProjectileType<QuaraTidalCrest>(), EnemyDamage.Projectile(CrestExpertDamage), 6f, Main.myPlayer, NPC.direction);
                    }
                }
            }
            else if (AttackTimer >= CrestTelegraphTicks + 25) //recovery, no armor
            {
                EndAttack(200);
            }
        }

        void RunBurstBubble()
        {
            if (AttackTimer <= BurstCastTicks)
            {
                NPC.velocity.X *= 0.8f;
                if (!Main.dedServ)
                {
                    if (AttackTimer == 1)
                    {
                        SoundEngine.PlaySound(SoundID.Item87 with { Volume = 0.6f, Pitch = -0.5f }, NPC.Center);
                    }
                    if (AttackTimer == BurstStaggerableTicks)
                    {
                        SoundEngine.PlaySound(SoundID.Item87 with { Volume = 0.6f, Pitch = 0.2f }, NPC.Center); //commit cue
                    }
                    //The bubble visibly inflating at the staff
                    float progress = AttackTimer / (float)BurstCastTicks;
                    float angle = Main.rand.NextFloat(MathHelper.TwoPi);
                    Vector2 position = StaffTip + angle.ToRotationVector2() * (6f + progress * 14f);
                    int dust = Dust.NewDust(position, 2, 2, DustID.Water, 0f, 0f, 70, default, 1f);
                    Main.dust[dust].noGravity = true;
                    Main.dust[dust].velocity *= 0.15f;
                }
                if (AttackTimer == BurstCastTicks && Authority)
                {
                    // ai[0] = the target this bubble drifts toward, locked here so every peer homes on the same player.
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), StaffTip, new Vector2(NPC.direction * 2f, -0.5f),
                        ModContent.ProjectileType<QuaraBurstBubble>(), EnemyDamage.Projectile(BurstExpertDamage), 2f, Main.myPlayer, NPC.target);
                }
            }
            else if (AttackTimer >= BurstCastTicks + 20)
            {
                EndAttack(300);
            }
        }

        void RunInkGeyser(Player player)
        {
            NPC.velocity.X *= 0.8f;

            if (AttackTimer <= InkTelegraphTicks)
            {
                if (!Main.dedServ)
                {
                    if (AttackTimer == 1)
                    {
                        SoundEngine.PlaySound(SoundID.Item87 with { Volume = 0.7f, Pitch = -0.7f }, NPC.Center);
                    }
                    //BLACK: ink gathering at the staff, the dark spell in the color language
                    if (Main.rand.NextBool(2))
                    {
                        int ink = Dust.NewDust(StaffTip - new Vector2(8f, 8f), 16, 16, DustID.Asphalt, 0f, -1f, 60, default, 1.4f);
                        Main.dust[ink].noGravity = true;
                    }
                }
                if (AttackTimer == InkTelegraphTicks && Authority)
                {
                    //The cloud forms ON the player's position (its own 120t build-up is the escape window)
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), player.Center, Vector2.Zero,
                        ModContent.ProjectileType<InkGeyser>(), EnemyDamage.Projectile(InkExpertDamage), 1f, Main.myPlayer, NPC.target);
                }
            }
            else if (AttackTimer >= InkTelegraphTicks + 25)
            {
                EndAttack(420);
            }
        }

        void RunIceArcVolley(Player player)
        {
            NPC.velocity.X *= 0.8f;
            if (AttackTimer <= IceTelegraphTicks)
            {
                EmitIceTelegraph(halfRing: true, spiral: false, AttackTimer);
                if (AttackTimer == IceTelegraphTicks && Authority)
                {
                    Vector2 aim = (player.Center - StaffTip).SafeNormalize(new Vector2(NPC.direction, 0f));
                    for (int i = 0; i < 6; i++)
                    {
                        float spread = i - 2.5f;
                        // Middle shards climb the highest; the six trajectories read as an upside-down U
                        // before their short 30-tick course correction begins.
                        Vector2 velocity = new Vector2(aim.X * 6f + spread * 1.15f, -8.2f + Math.Abs(spread) * 1.25f);
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), StaffTip, velocity,
                            ModContent.ProjectileType<GigasIceShard>(), EnemyDamage.Projectile(IceExpertDamage), 1f, Main.myPlayer,
                            0.10f, IceGuidedThirty, NPC.target);
                    }
                }
                if (AttackTimer == IceTelegraphTicks && !Main.dedServ)
                {
                    SoundEngine.PlaySound(SoundID.Item28 with { Volume = 0.65f, Pitch = 0.15f }, StaffTip);
                }
            }
            else if (AttackTimer >= IceTelegraphTicks + IceRecoveryTicks)
            {
                EndAttack(210);
            }
        }

        void RunIceOverflight(Player player)
        {
            NPC.velocity.X *= 0.8f;
            if (AttackTimer <= IceTelegraphTicks)
            {
                EmitIceTelegraph(halfRing: false, spiral: true, AttackTimer);
                if (AttackTimer == IceTelegraphTicks && Authority)
                {
                    int towardPlayer = Math.Sign(player.Center.X - StaffTip.X);
                    for (int i = 0; i < 4; i++)
                    {
                        float spread = i - 1.5f;
                        Vector2 velocity = new Vector2(towardPlayer * (6.2f + spread * 0.45f), -9f + Math.Abs(spread));
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), StaffTip, velocity,
                            ModContent.ProjectileType<GigasIceShard>(), EnemyDamage.Projectile(IceExpertDamage), 1f, Main.myPlayer,
                            0f, IceOverflightThirty, NPC.target);
                    }
                }
                if (AttackTimer == IceTelegraphTicks && !Main.dedServ)
                {
                    SoundEngine.PlaySound(SoundID.Item28 with { Volume = 0.65f, Pitch = -0.05f }, StaffTip);
                }
            }
            else if (AttackTimer >= IceTelegraphTicks + IceRecoveryTicks)
            {
                EndAttack(230);
            }
        }

        void RunFrostSprites(Player player)
        {
            NPC.velocity.X *= 0.82f;
            if (AttackTimer <= IceTelegraphTicks)
            {
                EmitIceTelegraph(halfRing: false, spiral: false, AttackTimer);
                if (AttackTimer == IceTelegraphTicks && Authority)
                {
                    int spawned = 0;
                    for (int attempt = 0; attempt < 32 && spawned < 8; attempt++)
                    {
                        if (!TryFindFrostSpritePosition(player, out Vector2 spawn))
                        {
                            continue;
                        }
                        Projectile.NewProjectile(NPC.GetSource_FromThis(), spawn, Vector2.Zero,
                            ModContent.ProjectileType<QuaraFrostSprite>(), EnemyDamage.Projectile(IceExpertDamage), 1f, Main.myPlayer,
                            spawned * 15f, NPC.target);
                        spawned++;
                    }
                }
                if (AttackTimer == IceTelegraphTicks && !Main.dedServ)
                {
                    SoundEngine.PlaySound(SoundID.Item30 with { Volume = 0.7f, Pitch = -0.35f }, StaffTip);
                }
            }
            // The seeded frost sprites own their own 3-second warning and delayed shots, so it may return
            // to its normal move pool after this short recovery.
            else if (AttackTimer >= IceTelegraphTicks + IceRecoveryTicks)
            {
                EndAttack(260);
            }
        }

        void EmitIceTelegraph(bool halfRing, bool spiral, int timer)
        {
            if (Main.dedServ)
            {
                return;
            }
            if (timer == 1)
            {
                SoundEngine.PlaySound(SoundID.Item30 with { Volume = 0.5f, Pitch = -0.55f }, StaffTip);
            }
            float progress = timer / (float)IceTelegraphTicks;
            int count = 1 + (int)(progress * 3f);
            for (int i = 0; i < count; i++)
            {
                float angle;
                if (spiral)
                {
                    angle = progress * MathHelper.TwoPi * 2.5f + Main.rand.NextFloat(-0.55f, 0.55f);
                }
                else if (halfRing)
                {
                    angle = Main.rand.NextFloat(MathHelper.Pi, MathHelper.TwoPi);
                }
                else
                {
                    angle = Main.rand.NextFloat(MathHelper.TwoPi);
                }
                Vector2 position = StaffTip + angle.ToRotationVector2() * Main.rand.NextFloat(10f, 34f);
                Dust dust = Dust.NewDustPerfect(position, DustID.Frost, (StaffTip - position) * (0.10f + progress * 0.10f), 80, default,
                    Main.rand.NextFloat(0.65f, 1.05f));
                dust.noGravity = true;
                if (i == 0)
                {
                    Dust glint = Dust.NewDustPerfect(position, DustID.IceTorch, dust.velocity * 0.55f, 100, default,
                        Main.rand.NextFloat(0.45f, 0.75f));
                    glint.noGravity = true;
                }
            }
            Lighting.AddLight(StaffTip, 0.18f * progress, 0.30f * progress, 0.52f * progress);
        }

        static bool TryFindFrostSpritePosition(Player player, out Vector2 spawn)
        {
            for (int attempt = 0; attempt < 12; attempt++)
            {
                int tileX = (int)(player.Center.X / 16f) + Main.rand.Next(-12, 13);
                // A seven-tile (112px) tall band high above the player leaves the requested 15
                // clear tiles underneath it in ordinary ground arenas, instead of rejecting every
                // location merely because the player is standing on a floor.
                int tileY = (int)(player.Center.Y / 16f) - Main.rand.Next(16, 23);
                if (!WorldGen.InWorld(tileX, tileY, 16) || !HasClearTilesBelow(tileX, tileY, 15))
                {
                    continue;
                }
                spawn = new Vector2(tileX * 16f + 8f, tileY * 16f + 8f);
                return true;
            }
            spawn = Vector2.Zero;
            return false;
        }

        static bool HasClearTilesBelow(int tileX, int tileY, int tiles)
        {
            for (int y = tileY; y <= tileY + tiles; y++)
            {
                if (!WorldGen.InWorld(tileX, y, 16) || WorldGen.SolidTile(tileX, y))
                {
                    return false;
                }
            }
            return true;
        }

        void RunTideRush(Player player)
        {
            if (AttackTimer <= RushDissolveTicks)
            {
                NPC.velocity.X *= 0.7f;
                if (AttackTimer == 1)
                {
                    rushStartGround = NPC.Bottom;
                    lastRushWaterTileX = int.MinValue;
                    rushIceVolleyFired = false;
                    if (!Main.dedServ)
                    {
                        SoundEngine.PlaySound(SoundID.SplashWeak with { Volume = 0.7f }, NPC.Center);
                        for (int i = 0; i < 15; i++)
                        {
                            Vector2 velocity = Main.rand.NextVector2Circular(3f, 3f);
                            int dust = Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Water, velocity.X, velocity.Y - 1f, 60, default, 1.3f);
                            Main.dust[dust].noGravity = true;
                        }
                    }
                }
                if (AttackTimer == RushDissolveTicks && Authority)
                {
                    //Retreat, or flank to the player's far side with 6 tiles space
                    int towardPlayer = player.Center.X > NPC.Center.X ? 1 : -1;
                    bool flank = Main.rand.NextBool();
                    if (flank)
                    {
                        rushDestX = player.Center.X + towardPlayer * 6 * 16f;
                    }
                    else
                    {
                        rushDestX = NPC.Center.X - towardPlayer * 8 * 16f;
                    }
                    rushDir = Math.Sign(rushDestX - NPC.Center.X);
                    savedKnockBackResist = NPC.knockBackResist;
                    NPC.knockBackResist = 0f; //a rushing puddle can't be shoved off course
                    NPC.netUpdate = true;
                }
            }
            else if (AttackTimer <= RushReformStartTick)
            {
                //The surge: a low racing puddle, damageable but unshovable
                NPC.velocity.X = rushDir * 9f;
                if (!Main.dedServ)
                {
                    for (int i = 0; i < 4; i++)
                    {
                        Vector2 position = new Vector2(NPC.position.X + Main.rand.NextFloat(NPC.width), NPC.Bottom.Y - Main.rand.NextFloat(14f));
                        int splash = Dust.NewDust(position, 4, 4, DustID.Water, -rushDir * 2.5f, -2f, 60, default, 1.4f);
                        Main.dust[splash].noGravity = true;
                    }

                    // Soaks anyone it flows through (no contact damage: NPC.damage is 0). Applied on the
                    // soaked player's own machine, because a server-side Player.AddBuff never reaches clients.
                    Player local = Main.LocalPlayer;
                    if (local.active && !local.dead && NPC.Hitbox.Intersects(local.Hitbox))
                    {
                        local.AddBuff(BuffID.Wet, 5 * 60);
                    }
                }
                if (Authority)
                {
                    SpawnRushWaterMarker();
                }
                bool arrived = (rushDir > 0 && NPC.Center.X >= rushDestX) || (rushDir < 0 && NPC.Center.X <= rushDestX);
                bool blocked = NPC.collideX && AttackTimer > RushDissolveTicks + 8;
                if (arrived || blocked)
                {
                    AttackTimer = RushReformStartTick; //skip to reform
                    if (Authority)
                    {
                        NPC.netUpdate = true;
                    }
                }
            }
            else
            {
                //Re-forming: vulnerable
                NPC.velocity.X *= 0.6f;
                RestoreKnockback();
                if (AttackTimer == RushReformStartTick + 1 && !Main.dedServ)
                {
                    SoundEngine.PlaySound(SoundID.SplashWeak with { Volume = 0.6f, Pitch = 0.3f }, NPC.Center);
                    for (int i = 0; i < 12; i++)
                    {
                        int dust = Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Water, 0f, -2f, 60, default, 1.2f);
                        Main.dust[dust].noGravity = true;
                    }
                }
                int iceTelegraphStart = RushIceTelegraphStartTick;
                if (AttackTimer > iceTelegraphStart && AttackTimer <= iceTelegraphStart + IceTelegraphTicks)
                {
                    EmitIceTelegraph(halfRing: false, spiral: false, AttackTimer - iceTelegraphStart);
                    bool volleyTick = AttackTimer == iceTelegraphStart + IceTelegraphTicks;
                    if (volleyTick && !rushIceVolleyFired && Authority)
                    {
                        rushIceVolleyFired = true;
                        Vector2 target = FindGroundPoint(rushStartGround, 8);
                        if (target == Vector2.Zero)
                        {
                            target = rushStartGround;
                        }
                        for (int i = 0; i < 4; i++)
                        {
                            Vector2 direction = (target - StaffTip).SafeNormalize(new Vector2(NPC.direction, 0f));
                            Vector2 velocity = direction.RotatedBy((i - 1.5f) * 0.10f) * Main.rand.NextFloat(8.5f, 10.5f);
                            Projectile.NewProjectile(NPC.GetSource_FromThis(), StaffTip, velocity,
                                ModContent.ProjectileType<GigasIceShard>(), EnemyDamage.Projectile(IceExpertDamage), 1f, Main.myPlayer,
                                0.12f, IceWaterIgniter, -1f);
                        }
                    }
                    if (volleyTick && !Main.dedServ)
                    {
                        SoundEngine.PlaySound(SoundID.Item28 with { Volume = 0.72f, Pitch = -0.15f }, StaffTip);
                    }
                }
                else if (AttackTimer >= iceTelegraphStart + IceTelegraphTicks + IceRecoveryTicks)
                {
                    EndAttack(250);
                }
            }
        }

        void SpawnRushWaterMarker()
        {
            int tileX = (int)(NPC.Center.X / 16f);
            if (tileX == lastRushWaterTileX)
            {
                return;
            }
            lastRushWaterTileX = tileX;
            Vector2 ground = FindGroundPoint(NPC.Bottom, 4);
            if (ground == Vector2.Zero)
            {
                return;
            }
            Projectile.NewProjectile(NPC.GetSource_FromAI(), ground - new Vector2(0f, 8f), Vector2.Zero,
                ModContent.ProjectileType<QuaraWaterResidue>(), EnemyDamage.Projectile(IceExpertDamage), 0f, Main.myPlayer);
        }

        static Vector2 FindGroundPoint(Vector2 worldPosition, int maxTilesDown)
        {
            int tileX = (int)(worldPosition.X / 16f);
            int startY = (int)(worldPosition.Y / 16f);
            for (int y = startY; y <= startY + maxTilesDown; y++)
            {
                if (WorldGen.InWorld(tileX, y, 16) && WorldGen.SolidTile(tileX, y))
                {
                    return new Vector2(tileX * 16f + 8f, y * 16f);
                }
            }
            return Vector2.Zero;
        }
        #endregion

        #region Staff pose
        ///<summary>The coral staff's orb, every cast's origin: the rear hand plus the measured grip-to-orb
        ///vector, mirrored by facing and rotated by the same tilt the staff is drawn with.</summary>
        Vector2 StaffTip
        {
            get
            {
                Vector2 offset = StaffTipOffset;
                offset.X *= NPC.direction;
                float rotation = StaffTilt * NPC.direction;
                return PuppetBackHandPosition + offset.RotatedBy(rotation) * NPC.scale;
            }
        }

        float StaffArmAngle
        {
            get
            {
                GetStaffPose(out float armAngle, out _);
                return armAngle;
            }
        }

        float StaffTilt
        {
            get
            {
                GetStaffPose(out _, out float tilt);
                return tilt;
            }
        }

        // The last tick of each cast's active part; the staff lowers back to rest over PoseLowerTicks after it.
        int PoseReleaseTick
        {
            get
            {
                switch (State)
                {
                    case AttackState.BubbleBarrage:
                        return BarrageTelegraphTicks + 12;
                    case AttackState.TidalCrest:
                        return CrestTelegraphTicks;
                    case AttackState.BurstBubble:
                        return BurstCastTicks;
                    case AttackState.InkGeyser:
                        return InkTelegraphTicks;
                    case AttackState.TideRush:
                        return RushIceTelegraphStartTick + IceTelegraphTicks;
                    default:
                        return IceTelegraphTicks;
                }
            }
        }

        // One pose per cast, derived purely from the synced State + AttackTimer so every peer draws the same
        // thing. Targets blend in from rest over PoseRaiseTicks and back to rest over PoseLowerTicks.
        void GetStaffPose(out float armAngle, out float tilt)
        {
            armAngle = RestArmAngle;
            tilt = RestStaffTilt;
            if (Neutral)
            {
                return;
            }

            float targetArm = RaisedArmAngle;
            float targetTilt = RaisedStaffTilt;
            int poseStart = 0;

            switch (State)
            {
                case AttackState.BubbleBarrage:
                    targetArm = AimedArmAngle;
                    targetTilt = 0.1f;
                    // A small upward kick for 4 ticks after each of the three shots.
                    int sinceFirstShot = AttackTimer - BarrageTelegraphTicks;
                    bool kicking = sinceFirstShot >= 0 && sinceFirstShot <= 16 && sinceFirstShot % 6 < 4;
                    if (kicking)
                    {
                        targetArm -= 0.1f;
                    }
                    break;

                case AttackState.TidalCrest:
                    // Lifted, then driven butt-first into the ground over the last 8 tell ticks so the strike
                    // lands on the release and the wave rises at its feet. The staff stays near upright.
                    if (AttackTimer > CrestSlamStartTick)
                    {
                        float slam = MathHelper.Clamp((AttackTimer - CrestSlamStartTick) / (float)(CrestTelegraphTicks - CrestSlamStartTick), 0f, 1f);
                        targetArm = MathHelper.Lerp(RaisedArmAngle, 0.5f, slam);
                        targetTilt = MathHelper.Lerp(RaisedStaffTilt, 0.15f, slam);
                    }
                    break;

                case AttackState.InkGeyser:
                    // Raised while the ink gathers, then thrust toward the target as the cloud is sent.
                    if (AttackTimer > InkThrustStartTick)
                    {
                        float thrust = MathHelper.Clamp((AttackTimer - InkThrustStartTick) / (float)(InkTelegraphTicks - InkThrustStartTick), 0f, 1f);
                        targetArm = MathHelper.Lerp(RaisedArmAngle, AimedArmAngle, thrust);
                        targetTilt = MathHelper.Lerp(RaisedStaffTilt, 0.5f, thrust);
                    }
                    break;

                case AttackState.IceOverflight:
                    // A slow circling wobble that matches the spiral gather dust (20-tick period).
                    float wobble = (float)Math.Sin(AttackTimer * MathHelper.TwoPi / 20f);
                    targetTilt += wobble * 0.18f;
                    break;

                case AttackState.TideRush:
                    if (AttackTimer <= RushReformStartTick)
                    {
                        // Dissolving into the puddle: staff lowered with the sinking body (hidden mid-surge).
                        targetArm = 0.3f;
                        targetTilt = 0.3f;
                    }
                    else
                    {
                        // Re-formed: the staff rises for the trail-freezing volley.
                        poseStart = RushReformStartTick;
                    }
                    break;
            }

            float blend = MathHelper.Clamp((AttackTimer - poseStart) / (float)PoseRaiseTicks, 0f, 1f);
            int releaseTick = PoseReleaseTick;
            if (AttackTimer > releaseTick)
            {
                blend = 1f - MathHelper.Clamp((AttackTimer - releaseTick) / (float)PoseLowerTicks, 0f, 1f);
            }

            armAngle = MathHelper.SmoothStep(RestArmAngle, targetArm, blend);
            tilt = MathHelper.SmoothStep(RestStaffTilt, targetTilt, blend);
        }

        // The near hand gestures at the target during casts; it cups upward for the overhead bubble and the
        // frost sprites, which form above.
        protected override float? FreeFrontArmFacingAngle
        {
            get
            {
                if (Neutral)
                {
                    return null;
                }

                float gestureTarget = 1.35f;
                if (State == AttackState.BurstBubble || State == AttackState.FrostSprites)
                {
                    gestureTarget = -1.2f;
                }

                float gesture = MathHelper.Clamp(AttackTimer / (float)PoseRaiseTicks, 0f, 1f);
                int releaseTick = PoseReleaseTick;
                if (AttackTimer > releaseTick)
                {
                    gesture = 1f - MathHelper.Clamp((AttackTimer - releaseTick) / (float)PoseLowerTicks, 0f, 1f);
                }

                return MathHelper.SmoothStep(MathHelper.PiOver2, gestureTarget, gesture);
            }
        }
        #endregion

        //On-hit evasion only from true neutral: recovery frames stay punishable
        public override void OnHitByItem(Player player, Item item, NPC.HitInfo hit, int damageDone)
        {
            if (Neutral)
            {
                tsorcRevampAIs.EvasiveOnHit(NPC, true);
            }
        }

        public override void OnHitByProjectile(Projectile projectile, NPC.HitInfo hit, int damageDone)
        {
            if (Neutral)
            {
                tsorcRevampAIs.EvasiveOnHit(NPC, projectile.DamageType == DamageClass.Melee);
            }
        }

        public override void PostDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            base.PostDraw(spriteBatch, screenPos, drawColor);
            if (Neutral)
            {
                return;
            }

            if (State == AttackState.TideRush)
            {
                if (AttackTimer <= RushDissolveTicks)
                {
                    EnemyVFX.DrawQuaraTideRush(NPC.Center, new Vector2(NPC.width, NPC.height), AttackTimer / (float)RushDissolveTicks, false, NPC.direction);
                }
                else if (AttackTimer <= RushReformStartTick)
                {
                    EnemyVFX.DrawQuaraTideRush(NPC.Center, new Vector2(NPC.width, NPC.height), 1f, false, rushDir);
                }
                else
                {
                    float reform = MathHelper.Clamp((AttackTimer - RushReformStartTick) / (float)RushReformTicks, 0f, 1f);
                    EnemyVFX.DrawQuaraTideRush(NPC.Center, new Vector2(NPC.width, NPC.height), reform, true, rushDir);
                }
                return;
            }

            float progress;
            int pattern;
            switch (State)
            {
                case AttackState.BubbleBarrage:
                    if (AttackTimer > BarrageTelegraphTicks + 12)
                    {
                        return;
                    }
                    progress = MathHelper.Clamp(AttackTimer / (float)BarrageTelegraphTicks, 0f, 1f);
                    pattern = 0;
                    break;
                case AttackState.TidalCrest:
                    if (AttackTimer > CrestTelegraphTicks)
                    {
                        return;
                    }
                    progress = MathHelper.Clamp(AttackTimer / (float)CrestTelegraphTicks, 0f, 1f);
                    pattern = 1;
                    break;
                case AttackState.BurstBubble:
                    if (AttackTimer > BurstCastTicks)
                    {
                        return;
                    }
                    progress = MathHelper.Clamp(AttackTimer / (float)BurstCastTicks, 0f, 1f);
                    pattern = 2;
                    break;
                case AttackState.InkGeyser:
                    if (AttackTimer > InkTelegraphTicks)
                    {
                        return;
                    }
                    progress = MathHelper.Clamp(AttackTimer / (float)InkTelegraphTicks, 0f, 1f);
                    pattern = 3;
                    break;
                default:
                    return;
            }
            EnemyVFX.DrawQuaraCast(StaffTip, progress, pattern);
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            base.SendExtraAI(writer);
            writer.Write((byte)State);
            writer.Write(AttackTimer);
            writer.Write(AttackCooldown);
            writer.Write((sbyte)rushDir);
            writer.Write(rushDestX);
            writer.Write(HM);
            writer.Write(SHM);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            base.ReceiveExtraAI(reader);
            State = (AttackState)reader.ReadByte();
            AttackTimer = reader.ReadInt32();
            AttackCooldown = reader.ReadInt32();
            rushDir = reader.ReadSByte();
            rushDestX = reader.ReadSingle();
            HM = reader.ReadBoolean();
            SHM = reader.ReadBoolean();
        }

        #region Gore
        public override void OnKill()
        {
            if (!Main.dedServ)
            {
                //The water in it escapes: a burst of spray before the body falls
                SoundEngine.PlaySound(SoundID.SplashWeak with { Volume = 0.8f, Pitch = -0.2f }, NPC.Center);
                for (int i = 0; i < 20; i++)
                {
                    Vector2 velocity = Main.rand.NextVector2Circular(4f, 4f);
                    int dust = Dust.NewDust(NPC.position, NPC.width, NPC.height, DustID.Water, velocity.X, velocity.Y - 2f, 50, default, 1.4f);
                    Main.dust[dust].noGravity = true;
                }
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Quara Hydromancer Gore 1").Type, 1.2f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Quara Hydromancer Gore 2").Type, 1.2f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Quara Hydromancer Gore 3").Type, 1.2f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Quara Hydromancer Gore 2").Type, 1.2f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, new Vector2((float)Main.rand.Next(-30, 31) * 0.2f, (float)Main.rand.Next(-30, 31) * 0.2f), Mod.Find<ModGore>("Quara Hydromancer Gore 3").Type, 1.2f);
            }
        }
        #endregion

        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            npcLoot.Add(ItemDropRule.Common(ItemID.ManaRegenerationPotion, 50));
            npcLoot.Add(ItemDropRule.Common(ItemID.IronskinPotion, 50));
            npcLoot.Add(ItemDropRule.Common(ItemID.SoulofLight, 2));
            npcLoot.Add(new CommonDrop(ItemID.GreaterHealingPotion, 100, 1, 1, 8));
            // Unique, guaranteed crafting soul for the Quara Hydromancer armor (3 per piece).
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<HydromancerSoul>()));
        }
    }
}

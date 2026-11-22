using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items.Armor.Enemy.NecromancerSet;
using tsorcRevamp.Content.Items.Potions;
using tsorcRevamp.Content.Projectiles.Enemy;
using tsorcRevamp.Content.Projectiles.Enemy.Weapons;
using tsorcRevamp.NPCs.Enemies.Dworc;
using tsorcRevamp.NPCs.Invaders;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.NPCs.Enemies
{
    // Bone Chaplain (was Fallen Necromancer until 2026-11): a support caster that never fights alone. It raises Skeleton
    // Warriors, and while it lives every Skeleton Warrior / Ice Skeleton within 150 tiles that dies drops into a HELD bone
    // pile (SkeletonPuppet's Vigil) that it can raise again with a 2s Resurrection channel, any number of times. It mends
    // every other wounded monster nearby by 35%, burns anyone who closes in with a Blight flame, kites and backsteps.
    // Answers: kill it first (its piles crumble, its raised warriors fall), shatter piles, finish skeletons with magic,
    // and stagger its channels - every channel is fully interruptible.
    //
    // Colour language: GREEN = its holy-ish rites (Resurrection, Mend), BLACK + YELLOW = Blight (the flame), BONE = raising.
    // Every tell is dust in the hands plus an arm pose - no drawn telegraph lines.
    [LegacyName("FallenNecromancer")]
    class BoneChaplain : PuppetNPC
    {
        enum Rite : byte
        {
            None = 0,
            BlightFlame,  // close-range flame from the palm, 30 Blight buildup
            RaiseWarrior, // grave ritual: one owned Skeleton Warrior claws up
            Resurrection, // 2s channel: every held pile within 150 tiles reforms
            Mend,         // 2s channel, arms raised: 35% heal to each marked wounded non-skeleton monster
        }

        // ── Timings (ticks) ───────────────────────────────────────────────────────────────────────────────
        const int FlameTellTicks = 30;
        const int FlameAimLockTick = 20;    // aim tracks until here, then holds for the last 10t of the tell
        const int FlameRecoveryTicks = 30;
        const int RaiseRitualTicks = 60;
        const int RaiseRecoveryTicks = 30;
        const int ResurrectionChannelTicks = 120;
        const int MendChannelTicks = 120;
        const int ChannelRecoveryTicks = 30;
        const int NeutralGapTicks = 30;     // after every action, before the next may start

        // Cooldowns after an action ends (or is staggered), per action
        const int FlameCooldownTicks = 90;
        const int RaiseCooldownTicks = 480;
        const int ResurrectionCooldownTicks = 360;
        const int MendCooldownTicks = 600;

        // ── Backstep: a short hop back when a player crowds it ─────────────────────────────────────────────
        const float BackstepTriggerRange = 64f;   // px (4 tiles), target centre to chaplain centre
        const int BackstepCrowdTicks = 20;        // consecutive neutral ticks that close before it hops
        const float BackstepSpeedX = 7.5f;
        const float BackstepSpeedY = -4.5f;
        const int BackstepTicks = 50;             // ~30t airborne + 20t landing, no action meanwhile
        const int BackstepCooldownTicks = 180;
        const float BackstepWallProbe = 48f;      // px behind it that must be open to hop

        // ── Values ────────────────────────────────────────────────────────────────────────────────────────
        const int BlightFlameExpertDamage = 72;   // Expert damage before defense, converted once by EnemyDamage
        const float FlameMaxRange = 224f;         // px; it kites 8-14 tiles, so it fights at the edge of this
        const float FlameSpeed = 7f;
        const int MaxRaisedWarriors = 3;          // its own warriors, standing or piled
        const float ResurrectionLifeFraction = 0.30f;
        const float MendRange = 640f;             // px (40 tiles)
        const float MendHealFraction = 0.35f;

        // ── Synced state ──────────────────────────────────────────────────────────────────────────────────
        Rite State = Rite.None;
        int AttackTimer;
        int targetIndex;
        float lockedFlameAngle;                   // world-space aim, held from FlameAimLockTick
        Vector2 graveSpot;                        // world point the Raise ritual digs at (ground level)
        int backstepTimer;
        readonly List<int> mendMarks = new List<int>(); // NPC slots marked at Mend entry (visual threads on clients)

        // ── Server-only ───────────────────────────────────────────────────────────────────────────────────
        int gapTimer = 90;                        // neutral gap before the next action
        readonly int[] actionReady = new int[5];  // ticks until each Rite may start again
        readonly List<int> mendTokens = new List<int>(); // DworcSpawnIdentity tokens matching mendMarks (slot reuse guard)
        int crowdTicks;
        int backstepReady;

        #region Puppet loadout
        public override bool UsesPuppetDifficultyScaling => false;
        protected override bool AnnounceInvasion => false;
        protected override bool AnnounceInvaderDefeat => false;
        protected override bool DespawnsOnPartyWipe => false;
        protected override string InvaderTitle => "Bone Chaplain";
        protected override int HeadArmorItemType => ModContent.ItemType<NecromancerHood>();
        protected override int BodyArmorItemType => ModContent.ItemType<NecromancerRobe>();
        protected override int LegsArmorItemType => ModContent.ItemType<NecromancerBoots>();
        // The Necromancer's set in a dried-blood red-and-black, after the Fallen Necromancer sprite's maroon robe.
        protected override int HeadArmorDyeItemType => ItemID.RedandBlackDye;
        protected override int BodyArmorDyeItemType => ItemID.RedandBlackDye;
        protected override int LegsArmorDyeItemType => ItemID.RedandBlackDye;
        protected override Color PuppetSkinColor => new Color(214, 200, 168); // bone-pale, for any skin the armor doesn't hide
        protected override int MeleeWeaponItemType => -1;
        protected override int RangedWeaponItemType => -1;
        protected override int MagicWeaponItemType => -1;
        protected override int MeleeDamage => 0;
        protected override int RangedDamage => 0;
        protected override int EstusChargesMax => 0;
        protected override int CasualStrollChance => 0;
        protected override int UnblockableFinisherBlocks => 0;
        protected override bool HoldAttackSelection => true;
        protected override bool SlowDownDuringCustom => true;
        protected override float TopSpeed => 1.8f;
        protected override float Acceleration => 0.05f;

        // Only the last 6t of the flame's tell resists stagger. Raise, Resurrection and Mend are interruptible throughout.
        protected override bool CustomAttackCommitted
        {
            get
            {
                if (State == Rite.BlightFlame)
                {
                    return AttackTimer >= FlameTellTicks - 6 && AttackTimer <= FlameTellTicks;
                }

                return false;
            }
        }

        protected override float? FreeFrontArmFacingAngle
        {
            get
            {
                if (State == Rite.None)
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
                if (State == Rite.None)
                {
                    return null;
                }

                return BackArmPose;
            }
        }

        protected override void DoMeleeAttack()
        {
            // Empty hands: everything is released by the Custom action clock below.
        }

        protected override void DoRangedAttack()
        {
        }
        #endregion

        public override void SetStaticDefaults()
        {
            // Keeps the old sheet's slicing for the bestiary/banner; the puppet Player draws the body in game.
            Main.npcFrameCount[NPC.type] = 15;
        }

        public override void SetDefaults()
        {
            NPC.npcSlots = 7;
            AnimationType = -1;
            NPC.knockBackResist = 0f; // overridden by the PoiseProfiles entry
            NPC.aiStyle = -1;
            NPC.damage = 0;
            NPC.defense = 30;
            NPC.height = 40;
            NPC.width = 20;
            NPC.scale = 1f;
            NPC.lifeMax = 4000; // EnemyBalance registers the live value (Hardmode 8,000)
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.lavaImmune = true;
            NPC.value = 20000;
            Banner = NPC.type;
            BannerItem = ModContent.ItemType<Banners.BoneChaplainBanner>();

            // Caster levers: remember last-known position; pace when it patrols; kite an 8-14 tile band (the outer edge
            // is the flame's reach) and backpedal rather than march into melee. Teleport is the navigation unstick,
            // neutral only (see AI). The old passive heals are off: Mend is its only healing.
            tsorcRevampGlobalNPC casterGlobalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            casterGlobalNPC.RemembersLastKnownPos = true;
            casterGlobalNPC.PatrolMode = NPCs.PatrolMode.Pace;
            casterGlobalNPC.NavSearchRadius = 80;
            casterGlobalNPC.KiteRangeMin = 8f;
            casterGlobalNPC.KiteRangeMax = 14f;
            casterGlobalNPC.KiteLooseness = 0.2f;
            casterGlobalNPC.CanWalkBackwards = true;
            casterGlobalNPC.CanTeleport = true;
            casterGlobalNPC.CanUseRopes = true;
            casterGlobalNPC.CanSelfHeal = false;
            casterGlobalNPC.CanHealAllies = false;
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            base.SendExtraAI(writer);
            writer.Write((byte)State);
            writer.Write(AttackTimer);
            writer.Write((byte)targetIndex);
            writer.Write(lockedFlameAngle);
            writer.WriteVector2(graveSpot);
            writer.Write(backstepTimer);
            writer.Write((byte)mendMarks.Count);
            foreach (int slot in mendMarks)
            {
                writer.Write((short)slot);
            }
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            base.ReceiveExtraAI(reader);
            State = (Rite)reader.ReadByte();
            AttackTimer = reader.ReadInt32();
            targetIndex = reader.ReadByte();
            lockedFlameAngle = reader.ReadSingle();
            graveSpot = reader.ReadVector2();
            backstepTimer = reader.ReadInt32();
            mendMarks.Clear();
            int markCount = reader.ReadByte();
            for (int i = 0; i < markCount; i++)
            {
                mendMarks.Add(reader.ReadInt16());
            }
        }

        ///<summary>Its casting palm - where the flame forms and leaves. Pose maths, so it is valid on a dedicated server.</summary>
        Vector2 Palm => PuppetFrontHandPositionAt(FrontArmPose);

        ///<summary>The off hand, for client-side dust only.</summary>
        Vector2 OffPalm => PuppetBackHandPositionAt(BackArmPose);

        Player Target => Main.player[targetIndex];

        // Server: this chaplain's spawn identity; its raised warriors and Mend marks compare against tokens.
        int SpawnToken => NPC.GetGlobalNPC<DworcSpawnIdentity>().Token;

        public override void OnStagger(NPC npc)
        {
            base.OnStagger(npc);

            // Poise break: the rite fizzles with no partial effect, and that action still goes on its cooldown.
            if (State != Rite.None && !Main.dedServ)
            {
                SoundEngine.PlaySound(SoundID.Item10 with { Volume = 0.6f, Pitch = -0.4f }, NPC.Center);
                for (int i = 0; i < 14; i++)
                {
                    Vector2 velocity = Main.rand.NextVector2Circular(3.5f, 3.5f);
                    Dust fizzle = Dust.NewDustPerfect(NPC.Center, DustID.GreenFairy, velocity, 90, default, 1.1f);
                    fizzle.noGravity = true;
                }
            }

            if (Main.netMode != NetmodeID.MultiplayerClient && State != Rite.None)
            {
                EndAction();
                gapTimer = Math.Max(gapTimer, 70);
            }
            else
            {
                State = Rite.None;
                AttackTimer = 0;
                mendMarks.Clear();
            }
        }

        protected override void RunMovementAI(float speedMult)
        {
            // Rooted while casting; airborne through a backstep (the hop's velocity carries it). Neutral uses the
            // shared puppet navigation, which honours the kite band set in SetDefaults.
            if (State != Rite.None)
            {
                NPC.velocity.X *= 0.85f;
                return;
            }

            if (backstepTimer > 0)
            {
                bool landed = NPC.velocity.Y == 0f;
                if (landed)
                {
                    NPC.velocity.X *= 0.8f;
                }

                return;
            }

            base.RunMovementAI(speedMult);
        }

        public override void AI()
        {
            tsorcRevampGlobalNPC globalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            bool authority = Main.netMode != NetmodeID.MultiplayerClient;

            if (backstepTimer > 0)
            {
                backstepTimer--;
            }

            if (authority)
            {
                for (int i = 0; i < actionReady.Length; i++)
                {
                    if (actionReady[i] > 0)
                    {
                        actionReady[i]--;
                    }
                }

                if (backstepReady > 0)
                {
                    backstepReady--;
                }
            }

            // Neutral decisions (server): backstep first if crowded, otherwise the next action by priority.
            bool neutral = State == Rite.None && backstepTimer == 0;
            bool free = globalNPC.StaggerTimer <= 0 && globalNPC.TeleportCountdown == 0 && globalNPC.TeleportAppearanceTimer == 0;
            if (authority && neutral && free)
            {
                NPC.TargetClosest(false);
                Player player = Main.player[NPC.target];
                bool targetUsable = NPC.HasValidTarget && !player.dead && player.active && NPC.Distance(player.Center) < 1000f;

                if (targetUsable)
                {
                    targetIndex = NPC.target;
                    bool grounded = NPC.velocity.Y == 0f;
                    bool crowded = NPC.Distance(player.Center) <= BackstepTriggerRange;
                    if (crowded && grounded)
                    {
                        crowdTicks++;
                    }
                    else
                    {
                        crowdTicks = 0;
                    }

                    bool hopped = false;
                    if (crowdTicks >= BackstepCrowdTicks && backstepReady == 0)
                    {
                        hopped = TryBackstep();
                    }

                    if (!hopped)
                    {
                        if (gapTimer > 0)
                        {
                            gapTimer--;
                        }

                        if (gapTimer <= 0 && grounded)
                        {
                            PickAction();
                        }
                    }
                }
            }

            // Keep the Custom phase alive while an action runs; face the target throughout.
            if (State != Rite.None)
            {
                PhaseTimer = 2;
                NPC.target = targetIndex;
                FaceTarget();
            }

            if (backstepTimer > 0)
            {
                FaceTarget();
            }

            globalNPC.CanTeleport = State == Rite.None && backstepTimer == 0;
            base.AI();

            if (State != Rite.None || backstepTimer > 0)
            {
                FaceTarget();
            }

            DebugAttackLabel = State + "/" + AttackTimer;

            // Idle identity: a faint trickle of bone dust off the robe and a dim green lamp-glow.
            if (!Main.dedServ)
            {
                if (Main.rand.NextBool(8))
                {
                    Vector2 position = NPC.position + new Vector2(Main.rand.NextFloat(NPC.width), Main.rand.NextFloat(NPC.height));
                    Dust mote = Dust.NewDustPerfect(position, DustID.Bone, new Vector2(0f, 0.3f), 120, default, 0.7f);
                    mote.noGravity = true;
                }

                Lighting.AddLight(NPC.Center, 0.08f, 0.16f, 0.08f);
            }
        }

        void FaceTarget()
        {
            int direction = 1;
            if (Target.Center.X < NPC.Center.X)
            {
                direction = -1;
            }

            NPC.direction = direction;
            NPC.spriteDirection = direction;
        }

        /// <summary>
        /// Server: a short hop straight back from a player who has stood within 4 tiles for 20 ticks. No
        /// invulnerability; hits mid-hop still build poise. Skipped when a wall is within 48px behind it (it then keeps
        /// its ground and the flame stays available). Returns whether it hopped.
        /// </summary>
        bool TryBackstep()
        {
            int awayDirection = 1;
            if (Target.Center.X > NPC.Center.X)
            {
                awayDirection = -1;
            }

            Vector2 probe = NPC.position + new Vector2(awayDirection * BackstepWallProbe, -4f);
            bool wallBehind = Collision.SolidCollision(probe, NPC.width, NPC.height);
            if (wallBehind)
            {
                crowdTicks = 0;
                return false;
            }

            NPC.velocity = new Vector2(awayDirection * BackstepSpeedX, BackstepSpeedY);
            backstepTimer = BackstepTicks;
            backstepReady = BackstepCooldownTicks;
            crowdTicks = 0;
            NPC.netUpdate = true;

            if (!Main.dedServ)
            {
                SoundEngine.PlaySound(SoundID.Item24 with { Volume = 0.5f, Pitch = -0.3f }, NPC.Center);
            }

            return true;
        }

        #region Selection
        /// <summary>Server, in neutral after the gap: the first ready action whose situation calls for it, in priority
        /// order Resurrection > Mend > Raise > Blight Flame. With nothing to do it keeps kiting and looks again soon.</summary>
        void PickAction()
        {
            Rite chosen = Rite.None;
            Player player = Target;
            float distance = NPC.Distance(player.Center);
            bool lineOfSight = Collision.CanHitLine(NPC.Center, 1, 1, player.Center, 1, 1);

            if (actionReady[(int)Rite.Resurrection] == 0 && AnyHeldPileInRange())
            {
                chosen = Rite.Resurrection;
            }
            else if (actionReady[(int)Rite.Mend] == 0 && MarkMendRecipients())
            {
                chosen = Rite.Mend;
            }
            else if (actionReady[(int)Rite.RaiseWarrior] == 0 && CountRaisedWarriors() < MaxRaisedWarriors && TryFindGraveSpot())
            {
                chosen = Rite.RaiseWarrior;
            }
            else if (actionReady[(int)Rite.BlightFlame] == 0 && distance <= FlameMaxRange && lineOfSight)
            {
                chosen = Rite.BlightFlame;
            }

            if (chosen == Rite.None)
            {
                gapTimer = 20;
                return;
            }

            State = chosen;
            AttackTimer = 0;
            lockedFlameAngle = (player.Center - NPC.Center).ToRotation();
            StartCustomAttack(2);
            NPC.netUpdate = true;
        }

        static int TotalTicks(Rite action)
        {
            switch (action)
            {
                case Rite.BlightFlame:
                    return FlameTellTicks + FlameRecoveryTicks;
                case Rite.RaiseWarrior:
                    return RaiseRitualTicks + RaiseRecoveryTicks;
                case Rite.Resurrection:
                    return ResurrectionChannelTicks + ChannelRecoveryTicks;
                case Rite.Mend:
                    return MendChannelTicks + ChannelRecoveryTicks;
            }

            return 1;
        }

        static int CooldownFor(Rite action)
        {
            switch (action)
            {
                case Rite.BlightFlame:
                    return FlameCooldownTicks;
                case Rite.RaiseWarrior:
                    return RaiseCooldownTicks;
                case Rite.Resurrection:
                    return ResurrectionCooldownTicks;
                case Rite.Mend:
                    return MendCooldownTicks;
            }

            return 0;
        }

        /// <summary>Server: ends the current action (completed or staggered) - its cooldown starts, plus the neutral gap.</summary>
        void EndAction()
        {
            actionReady[(int)State] = CooldownFor(State);
            gapTimer = NeutralGapTicks;
            State = Rite.None;
            AttackTimer = 0;
            mendMarks.Clear();
            mendTokens.Clear();
            EnterPhase(AttackPhase.Idle, 0);
            NPC.netUpdate = true;
        }

        /// <summary>Server: its own raised warriors, standing or piled.</summary>
        int CountRaisedWarriors()
        {
            int count = 0;
            int token = SpawnToken;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (npc.active && npc.ModNPC is SkeletonPuppet skeleton && skeleton.IsBoundTo(NPC.whoAmI, token))
                {
                    count++;
                }
            }

            return count;
        }

        /// <summary>Every machine: any bone pile within the Vigil range is waiting for a Resurrection.</summary>
        bool AnyHeldPileInRange()
        {
            float rangeSquared = SkeletonPuppet.VigilRange * SkeletonPuppet.VigilRange;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                bool held = npc.active && npc.ModNPC is SkeletonPuppet skeleton && skeleton.IsHeldPile;
                if (held && Vector2.DistanceSquared(npc.Center, NPC.Center) <= rangeSquared)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Server, at Mend's start: marks every wounded hostile monster within MendRange, by slot + spawn token. Never
        /// itself, another Bone Chaplain, or a skeleton (the bone-pile skeletons and vanilla's skeleton set). Worm
        /// segments sharing a health pool are skipped; their head is healed. Returns whether anyone qualified.
        /// </summary>
        bool MarkMendRecipients()
        {
            mendMarks.Clear();
            mendTokens.Clear();
            int chaplainType = NPC.type;

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC ally = Main.npc[i];
                bool wounded = ally.active && !ally.friendly && ally.life > 0 && ally.life < ally.lifeMax;
                if (!wounded || i == NPC.whoAmI || ally.type == chaplainType)
                {
                    continue;
                }

                bool skeleton = ally.ModNPC is SkeletonPuppet || NPCID.Sets.Skeletons[ally.type];
                bool sharedSegment = ally.realLife >= 0 && ally.realLife != i;
                bool dummy = ally.type == NPCID.TargetDummy || ally.immortal || ally.dontTakeDamage;
                if (skeleton || sharedSegment || dummy)
                {
                    continue;
                }

                if (ally.Distance(NPC.Center) > MendRange)
                {
                    continue;
                }

                mendMarks.Add(i);
                mendTokens.Add(ally.GetGlobalNPC<DworcSpawnIdentity>().Token);
            }

            return mendMarks.Count > 0;
        }

        /// <summary>Server, at Raise's start: supported ground 48-112px in front of it with room for a warrior's body,
        /// at least 96px from every player. Stored in graveSpot (ground level). Returns whether one exists.</summary>
        bool TryFindGraveSpot()
        {
            for (int step = 0; step < 5; step++)
            {
                float offset = 48f + step * 16f;
                float graveX = NPC.Center.X + NPC.direction * offset;

                if (!PuppetGroundDustWave.TryFindGroundY(graveX, NPC.Bottom.Y, out float groundY))
                {
                    continue;
                }

                // Room for the warrior's 20x40-ish body standing on that ground, and not more than 4 tiles off its level.
                bool bodyClear = !Collision.SolidCollision(new Vector2(graveX - 12f, groundY - 46f), 24, 44);
                bool nearLevel = Math.Abs(groundY - NPC.Bottom.Y) <= 64f;
                Vector2 spot = new Vector2(graveX, groundY);
                bool playerClear = true;

                for (int i = 0; i < Main.maxPlayers; i++)
                {
                    Player player = Main.player[i];
                    if (player.active && !player.dead && player.Distance(spot) < 96f)
                    {
                        playerClear = false;
                        break;
                    }
                }

                if (bodyClear && nearLevel && playerClear)
                {
                    graveSpot = spot;
                    return true;
                }
            }

            return false;
        }
        #endregion

        #region Arm poses
        // Facing-space arm angles: 0 = straight ahead, negative = up, +PiOver2 = hanging at rest.
        // Each pose eases in over 10 ticks and back to rest over the last 15 ticks of the action.
        static float PoseBlendFor(Rite action, int timer)
        {
            int total = TotalTicks(action);
            float blendIn = MathHelper.Clamp(timer / 10f, 0f, 1f);
            float blendOut = MathHelper.Clamp((total - timer) / 15f, 0f, 1f);
            return Math.Min(blendIn, blendOut);
        }

        // Facing-space angle from the shoulder along a world-space aim angle, clamped to what the arm can reach forward.
        float FacingAngleFor(float worldAngle)
        {
            Vector2 aim = worldAngle.ToRotationVector2();
            float forward = aim.X * NPC.direction;
            float angle = (float)Math.Atan2(aim.Y, Math.Max(forward, 0.05f));
            return MathHelper.Clamp(angle, -1.6f, 1.4f);
        }

        float FrontArmPose
        {
            get
            {
                float pose = MathHelper.PiOver2;
                switch (State)
                {
                    case Rite.BlightFlame:
                        // Palm along the (locked) aim line, pushed 0.1 rad further on release like a shove.
                        pose = FacingAngleFor(lockedFlameAngle);
                        if (AttackTimer > FlameTellTicks)
                        {
                            pose -= 0.1f;
                        }

                        break;

                    case Rite.RaiseWarrior:
                        // Palm starts low over the grave and lifts the dead out of it across the ritual.
                        pose = MathHelper.SmoothStep(0.9f, -0.5f, MathHelper.Clamp(AttackTimer / (float)RaiseRitualTicks, 0f, 1f));
                        break;

                    case Rite.Resurrection:
                    case Rite.Mend:
                        // Both arms raised high in the air for the whole channel.
                        pose = -1.35f;
                        break;
                }

                return MathHelper.SmoothStep(MathHelper.PiOver2, pose, PoseBlendFor(State, AttackTimer));
            }
        }

        float BackArmPose
        {
            get
            {
                float pose = MathHelper.PiOver2;
                switch (State)
                {
                    case Rite.BlightFlame:
                        pose = 0.5f; // held back at the hip, bracing
                        break;

                    case Rite.RaiseWarrior:
                        pose = MathHelper.SmoothStep(0.9f, -0.3f, MathHelper.Clamp(AttackTimer / (float)RaiseRitualTicks, 0f, 1f));
                        break;

                    case Rite.Resurrection:
                    case Rite.Mend:
                        pose = -1.25f;
                        break;
                }

                return MathHelper.SmoothStep(MathHelper.PiOver2, pose, PoseBlendFor(State, AttackTimer));
            }
        }
        #endregion

        #region Actions
        protected override void DoCustomTick(int ticksRemaining)
        {
            if (State == Rite.None)
            {
                return;
            }

            AttackTimer++;
            bool authority = Main.netMode != NetmodeID.MultiplayerClient;
            if (authority && (Target.dead || !Target.active))
            {
                EndAction();
                return;
            }

            switch (State)
            {
                case Rite.BlightFlame:
                    RunBlightFlame();
                    break;
                case Rite.RaiseWarrior:
                    RunRaiseWarrior();
                    break;
                case Rite.Resurrection:
                    RunResurrection();
                    break;
                case Rite.Mend:
                    RunMend();
                    break;
            }

            if (authority && State != Rite.None && AttackTimer >= TotalTicks(State))
            {
                EndAction();
            }
        }

        void RunBlightFlame()
        {
            bool authority = Main.netMode != NetmodeID.MultiplayerClient;

            // Aim tracks the target until the lock tick, then holds; the lock is synced so every client draws the same palm.
            if (authority && AttackTimer < FlameAimLockTick)
            {
                lockedFlameAngle = (Target.Center - NPC.Center).ToRotation();
            }

            if (authority && AttackTimer == FlameAimLockTick)
            {
                NPC.netUpdate = true;
            }

            Vector2 palm = Palm;
            if (!Main.dedServ && AttackTimer <= FlameTellTicks)
            {
                if (AttackTimer == 1)
                {
                    SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.5f, Pitch = -0.5f }, NPC.Center);
                }

                // BLACK + YELLOW: Blight smoke gathering into the palm, sparks thickening over the last 15 ticks.
                float progress = AttackTimer / (float)FlameTellTicks;
                int gatherCount = 1 + (int)(progress * 2f);
                for (int i = 0; i < gatherCount; i++)
                {
                    float angle = Main.rand.NextFloat(MathHelper.TwoPi);
                    Vector2 position = palm + angle.ToRotationVector2() * Main.rand.NextFloat(6f, 22f);
                    Dust smoke = Dust.NewDustPerfect(position, DustID.Wraith, (palm - position) * 0.15f, 80, default, 1.1f);
                    smoke.noGravity = true;
                }

                if (AttackTimer >= FlameTellTicks - 15 && Main.rand.NextBool(2))
                {
                    Dust spark = Dust.NewDustPerfect(palm + Main.rand.NextVector2Circular(4f, 4f), DustID.YellowTorch, Main.rand.NextVector2Circular(1f, 1f), 60, default, 1.1f);
                    spark.noGravity = true;
                }

                Lighting.AddLight(palm, 0.4f * progress, 0.35f * progress, 0.05f * progress);
            }

            if (AttackTimer != FlameTellTicks)
            {
                return;
            }

            if (authority)
            {
                Vector2 velocity = lockedFlameAngle.ToRotationVector2() * FlameSpeed;
                Projectile.NewProjectile(NPC.GetSource_FromThis(), palm, velocity, ModContent.ProjectileType<BoneChaplainBlightFlame>(),
                    EnemyDamage.Projectile(BlightFlameExpertDamage), 1f, Main.myPlayer);
            }

            if (!Main.dedServ)
            {
                SoundEngine.PlaySound(SoundID.Item45 with { Volume = 0.7f, Pitch = -0.3f }, palm);
            }
        }

        void RunRaiseWarrior()
        {
            if (AttackTimer > RaiseRitualTicks)
            {
                return;
            }

            if (!Main.dedServ)
            {
                if (AttackTimer == 1)
                {
                    SoundEngine.PlaySound(SoundID.Item17 with { Volume = 0.7f, Pitch = -0.7f }, NPC.Center);
                }

                if (AttackTimer == RaiseRitualTicks - 15)
                {
                    SoundEngine.PlaySound(SoundID.NPCHit2 with { Volume = 0.6f, Pitch = -0.5f }, graveSpot); // bones clacking below
                }

                // The grave: bone-grey dust simmering up from the spot with a faint green glint, from the first tick.
                for (int i = 0; i < 2; i++)
                {
                    Vector2 position = new Vector2(graveSpot.X - 16f + Main.rand.NextFloat(32f), graveSpot.Y - 6f);
                    int dustType = DustID.Bone;
                    if (Main.rand.NextBool(4))
                    {
                        dustType = DustID.GreenFairy;
                    }

                    Dust grave = Dust.NewDustPerfect(position, dustType, new Vector2(0f, -1.4f), 110, default, 1f);
                    grave.noGravity = true;
                }

                if (Main.rand.NextBool(2))
                {
                    Dust front = Dust.NewDustPerfect(Palm, DustID.Bone, new Vector2(0f, -1.2f), 100, default, 0.9f);
                    front.noGravity = true;
                    Dust back = Dust.NewDustPerfect(OffPalm, DustID.Bone, new Vector2(0f, -1.2f), 100, default, 0.9f);
                    back.noGravity = true;
                }
            }

            if (AttackTimer != RaiseRitualTicks)
            {
                return;
            }

            // The warrior claws up out of the grave (server); a burst of dirt and bone everywhere.
            if (Main.netMode != NetmodeID.MultiplayerClient && CountRaisedWarriors() < MaxRaisedWarriors)
            {
                int spawned = NPC.NewNPC(NPC.GetSource_FromAI(), (int)graveSpot.X, (int)graveSpot.Y, ModContent.NPCType<SkeletonWarrior>());
                if (spawned < Main.maxNPCs && Main.npc[spawned].ModNPC is SkeletonPuppet warrior)
                {
                    warrior.BindToChaplain(NPC.whoAmI, SpawnToken);
                    Main.npc[spawned].velocity.Y = -5f;
                    if (Main.netMode == NetmodeID.Server)
                    {
                        NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, spawned);
                    }
                }
            }

            if (!Main.dedServ)
            {
                SoundEngine.PlaySound(SoundID.NPCDeath2 with { Volume = 0.6f, Pitch = -0.2f }, graveSpot);
                for (int i = 0; i < 16; i++)
                {
                    Dust dirt = Dust.NewDustPerfect(graveSpot + new Vector2(Main.rand.NextFloat(-18f, 18f), -6f), DustID.Dirt,
                        new Vector2(Main.rand.NextFloat(-2f, 2f), Main.rand.NextFloat(-5f, -2f)), 60, default, 1.2f);
                    dirt.noGravity = false;
                    Dust bone = Dust.NewDustPerfect(graveSpot + new Vector2(Main.rand.NextFloat(-18f, 18f), -14f), DustID.Bone,
                        new Vector2(Main.rand.NextFloat(-1.5f, 1.5f), Main.rand.NextFloat(-3f, -1f)), 60, default, 1.1f);
                    bone.noGravity = true;
                }
            }
        }

        void RunResurrection()
        {
            bool channeling = AttackTimer <= ResurrectionChannelTicks;

            // Clients: green light streams up from both raised palms, and every held pile in range glows and rattles,
            // so a player standing by a pile off-screen still sees the rite coming.
            if (!Main.dedServ && channeling)
            {
                if (AttackTimer == 1)
                {
                    SoundEngine.PlaySound(SoundID.Item8 with { Volume = 0.7f, Pitch = -0.6f }, NPC.Center);
                }

                EmitRaisedPalmDust(DustID.GreenFairy);

                bool pileTick = AttackTimer % 4 == 0;
                float rangeSquared = SkeletonPuppet.VigilRange * SkeletonPuppet.VigilRange;
                for (int i = 0; pileTick && i < Main.maxNPCs; i++)
                {
                    NPC npc = Main.npc[i];
                    bool held = npc.active && npc.ModNPC is SkeletonPuppet skeleton && skeleton.IsHeldPile;
                    if (!held || Vector2.DistanceSquared(npc.Center, NPC.Center) > rangeSquared)
                    {
                        continue;
                    }

                    Vector2 spot = npc.Bottom + new Vector2(Main.rand.NextFloat(-16f, 16f), -6f);
                    Dust glow = Dust.NewDustPerfect(spot, DustID.GreenFairy, new Vector2(0f, -1.8f), 80, default, 1.1f);
                    glow.noGravity = true;
                    Lighting.AddLight(npc.Bottom, 0.1f, 0.35f, 0.1f);

                    if (AttackTimer % 40 == 0)
                    {
                        SoundEngine.PlaySound(SoundID.NPCHit2 with { Volume = 0.35f, Pitch = -0.6f }, npc.Center);
                    }
                }
            }

            if (AttackTimer != ResurrectionChannelTicks)
            {
                return;
            }

            // Payout (server): every held pile within the Vigil range starts its reform at 30% life.
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                float rangeSquared = SkeletonPuppet.VigilRange * SkeletonPuppet.VigilRange;
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    NPC npc = Main.npc[i];
                    bool held = npc.active && npc.ModNPC is SkeletonPuppet skeleton && skeleton.IsHeldPile;
                    if (held && Vector2.DistanceSquared(npc.Center, NPC.Center) <= rangeSquared)
                    {
                        ((SkeletonPuppet)npc.ModNPC).Resurrect(ResurrectionLifeFraction);
                    }
                }
            }

            if (!Main.dedServ)
            {
                SoundEngine.PlaySound(SoundID.Item29 with { Volume = 0.6f, Pitch = -0.5f }, NPC.Center);
                for (int i = 0; i < 24; i++)
                {
                    Vector2 velocity = Main.rand.NextVector2CircularEdge(3f, 3f) + new Vector2(0f, -1f);
                    Dust burst = Dust.NewDustPerfect(NPC.Center + new Vector2(0f, -24f), DustID.GreenFairy, velocity, 60, default, 1.2f);
                    burst.noGravity = true;
                }
            }
        }

        void RunMend()
        {
            bool channeling = AttackTimer <= MendChannelTicks;

            // Clients: green dust rising from both raised hands the whole channel; motes drift toward each marked ally
            // and the ally shimmers green, so the player can see who is about to be healed.
            if (!Main.dedServ && channeling)
            {
                if (AttackTimer == 1)
                {
                    SoundEngine.PlaySound(SoundID.Item4 with { Volume = 0.5f, Pitch = -0.4f }, NPC.Center);
                }

                EmitRaisedPalmDust(DustID.GreenFairy);

                if (AttackTimer % 5 == 0)
                {
                    foreach (int slot in mendMarks)
                    {
                        NPC ally = Main.npc[slot];
                        if (!ally.active)
                        {
                            continue;
                        }

                        Vector2 toAlly = ally.Center - Palm;
                        Dust mote = Dust.NewDustPerfect(Palm, DustID.GreenFairy, toAlly.SafeNormalize(Vector2.Zero) * 4f, 100, default, 0.9f);
                        mote.noGravity = true;

                        Vector2 shimmerSpot = ally.position + new Vector2(Main.rand.NextFloat(ally.width), Main.rand.NextFloat(ally.height));
                        Dust shimmer = Dust.NewDustPerfect(shimmerSpot, DustID.GreenFairy, new Vector2(0f, -1f), 100, default, 0.9f);
                        shimmer.noGravity = true;
                    }
                }
            }

            if (AttackTimer != MendChannelTicks)
            {
                return;
            }

            // Payout (server): each marked ally that is still the same NPC, alive, in range and wounded heals 35% of its
            // max life, capped at what it is missing.
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                for (int i = 0; i < mendMarks.Count; i++)
                {
                    NPC ally = Main.npc[mendMarks[i]];
                    bool sameAlly = ally.active && ally.life > 0 && !ally.friendly && ally.GetGlobalNPC<DworcSpawnIdentity>().Token == mendTokens[i];
                    if (!sameAlly || ally.Distance(NPC.Center) > MendRange)
                    {
                        continue;
                    }

                    int missing = ally.lifeMax - ally.life;
                    int heal = Math.Max(1, (int)(ally.lifeMax * MendHealFraction));
                    int amount = Math.Min(missing, heal);
                    if (amount > 0)
                    {
                        ally.life += amount;
                        ally.HealEffect(amount);
                        ally.netUpdate = true;
                    }
                }
            }

            if (!Main.dedServ)
            {
                SoundEngine.PlaySound(SoundID.Item4 with { Volume = 0.6f, Pitch = 0.2f }, NPC.Center);
            }
        }

        /// <summary>Client: dust streaming up from both raised palms (Resurrection and Mend channels).</summary>
        void EmitRaisedPalmDust(int dustType)
        {
            Vector2[] palms = { Palm, OffPalm };
            foreach (Vector2 palm in palms)
            {
                Vector2 velocity = new Vector2(Main.rand.NextFloat(-0.3f, 0.3f), -Main.rand.NextFloat(1.5f, 2.5f));
                Dust rising = Dust.NewDustPerfect(palm + Main.rand.NextVector2Circular(3f, 3f), dustType, velocity, 90, default, 1f);
                rising.noGravity = true;
            }

            Lighting.AddLight(Palm, 0.1f, 0.4f, 0.1f);
        }
        #endregion

        #region Death
        // Replaces the puppet's generic blood burst. Runs on every client (OnKill does not run on MP clients).
        // Its raised warriors fall on their own (SkeletonPuppet checks its chaplain), and held piles crumble.
        public override void HitEffect(NPC.HitInfo hit)
        {
            if (NPC.life > 0 || Main.dedServ)
            {
                return;
            }

            SoundEngine.PlaySound(SoundID.NPCDeath6 with { Volume = 0.6f, Pitch = -0.2f }, NPC.Center);
            for (int i = 0; i < 24; i++)
            {
                Vector2 velocity = Main.rand.NextVector2Circular(3f, 3f) + new Vector2(0f, -1.5f);
                int dustType = DustID.Bone;
                if (i % 3 == 0)
                {
                    dustType = DustID.GreenFairy;
                }

                Dust burst = Dust.NewDustPerfect(NPC.Center, dustType, velocity, 60, default, 1.2f);
                burst.noGravity = dustType == DustID.GreenFairy;
            }

            string[] goreNames = { "Necromancer Gore 1", "Necromancer Gore 2", "Necromancer Gore 3", "Necromancer Gore 2", "Necromancer Gore 3" };
            foreach (string goreName in goreNames)
            {
                Vector2 goreVelocity = new Vector2(Main.rand.Next(-30, 31) * 0.2f, Main.rand.Next(-30, 31) * 0.2f);
                Gore.NewGore(NPC.GetSource_Death(), NPC.position, goreVelocity, Mod.Find<ModGore>(goreName).Type, 1.1f);
            }
        }
        #endregion

        public override void ModifyNPCLoot(NPCLoot npcLoot)
        {
            npcLoot.Add(ItemDropRule.Common(ItemID.ManaRegenerationBand, 10));
            npcLoot.Add(ItemDropRule.Common(ItemID.GreaterHealingPotion, 10));
            npcLoot.Add(ItemDropRule.Common(ItemID.IronskinPotion, 20));
            npcLoot.Add(ItemDropRule.Common(ItemID.MagicPowerPotion, 25));
            npcLoot.Add(ItemDropRule.Common(ItemID.RegenerationPotion, 25));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<CrimsonPotion>(), 50));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<StrengthPotion>(), 50));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<ShockwavePotion>(), 50));
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<BattlefrontPotion>(), 50));
        }
    }
}

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
using tsorcRevamp.Content.Items.Ammo;
using tsorcRevamp.Content.Items.Armor.Enemy.ElementalNecromancerSet;
using tsorcRevamp.Content.Items.Potions.StarlightShard;
using tsorcRevamp.Content.Projectiles.Enemy;
using tsorcRevamp.Content.Projectiles.Enemy.Weapons;
using tsorcRevamp.NPCs.Enemies.Dworc;
using tsorcRevamp.NPCs.Invaders;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.NPCs.Enemies.SuperHardMode
{
    // A kiting SHM caster rendered as a puppet in the Elemental Necromancer armor, empty hands. It keeps 19-31 tiles away,
    // throws Death Sigil skulls, raises up to six Elementals, calls 30-tile Elemental Eruption columns out of the floor
    // (four patterns), mends wounded allies through a visible channel and Gravesteps away when cornered or blind.
    // It never heals itself. Every tell is dust + an arm pose (+ the eruption shader) - no drawn telegraph lines.
    //
    // Poise contract (0.4 / 26 in PoiseProfiles): every tell and channel takes ordinary poise damage and a break cancels
    // it. Only the Sigil's last 6 tell ticks and each eruption's last 6 ticks before rising are committed (hyper-armor).
    class ElementalNecromancer : PuppetNPC
    {
        enum Act : byte
        {
            None = 0,
            DeathSigil,      // a flying skull from the palm; aim tracks, then locks 12t before release
            SummonElemental, // ritual at a ground spot: one owned Elemental claws up
            Eruption,        // floor marks, then 30-tile light columns (patterns A-D)
            AllyHeal,        // visible, interruptible channel: 10% max HP to each marked ally
            Gravestep,       // telegraphed reposition when crowded or out of sight
        }

        // A = one column under the target; B = A, then a second under where the target is 120t later;
        // C = three at once (middle + 150px either side); D (below half) = six predictive marks, one every 90t.
        enum Pattern : byte
        {
            Single = 0,
            Double,
            Triple,
            Predictive,
        }

        const int ActCount = 6;

        // Neutral gap after every action (ticks); per-action cooldowns start on completion or stagger.
        const int NeutralGapTicks = 30;

        // Death Sigil: 36t tell, aim locks at t24, fires at t36, then a 10t tail + 32t recovery.
        const int SigilTellTicks = 36;
        const int SigilAimLockTick = 24;
        const int SigilPreviewTicks = 18;
        const int SigilTotalTicks = SigilTellTicks + 10 + 32;
        const int SigilCooldown = 90;
        const float SigilMinRange = 48f;
        const float SigilMaxRange = 640f;
        const float SigilSpeed = 8f;
        const int SigilExpertDamage = 208;

        // Summon Elemental: 72t ritual, then it waits out the 24t emergence + 18t grace, then 36t recovery.
        const int SummonRitualTicks = 72;
        const int SummonTotalTicks = SummonRitualTicks + Elemental.EmergenceTicks + Elemental.EmergenceGraceTicks + 36;
        const int SummonCooldown = 480;
        const int MaxOwnedElementals = 6;

        // Eruption: every mark telegraphs for 90t, columns rise 90t after their mark, the action ends 36t after the last.
        const int EruptionLeadTicks = 90;
        const int EruptionTailTicks = 36;
        const int EruptionCooldown = 360;
        const float EruptionMaxRange = 640f;
        const int EruptionExpertDamage = 224;
        const int MaxMarks = 6;
        const float TripleSpacing = 150f;
        const float PredictionClamp = 320f;
        const int PredictiveMarkInterval = 90; // D: one mark every 90t, so only one is live at a time
        const int PredictiveMarkCount = 6;

        // Ally Heal: 75t channel, heal at t75, 30t recovery; 500t readiness.
        const int HealChannelTicks = 75;
        const int HealTotalTicks = HealChannelTicks + 30;
        const int HealReadiness = 500;
        const float HealRange = 800f;
        const float HealFraction = 0.10f;
        const int MaxHealMarks = 16;

        // Gravestep: 30t departure, relocate at t30, 18t reform, 30t recovery.
        const int GravestepDepartTicks = 30;
        const int GravestepReformTicks = 18;
        const int GravestepTotalTicks = GravestepDepartTicks + GravestepReformTicks + 30;
        const int GravestepCooldown = 480;
        const int CrowdedTriggerTicks = 90;
        const int BlindTriggerTicks = 180;
        const float CrowdedRange = 192f;
        const int GravestepRetryTicks = 90;

        // ── Synced action state ─────────────────────────────────────────────────
        Act State = Act.None;
        int AttackTimer;
        int AttackCooldown = 90;
        int targetIndex;
        Vector2 lockedAim = Vector2.UnitX;  // Sigil: world direction, locked at t24
        Vector2 graveSpot;                  // Summon: ground point (bottom of the Elemental)
        Vector2 gravestepDestination;       // Gravestep: ground point (its new feet)
        Pattern pattern;
        readonly Vector2[] markGround = new Vector2[MaxMarks]; // Eruption: floor point of each mark
        readonly int[] markPlacedTick = new int[MaxMarks];     // AttackTimer the mark appeared on; its column rises 90t later
        int markCount;
        int markFadeTimer;                  // >0 = a staggered eruption's unrisen marks fading out (12t)
        readonly List<int> healSlots = new List<int>();
        readonly List<int> healTokens = new List<int>();

        // ── Server-only selection state (never synced; only the drawn State is) ─
        AttackBag<Act> attackBag;
        readonly int[] cooldowns = new int[ActCount];
        bool utilityUsedSinceOffence;
        bool belowHalfLatched;
        int crowdedTicks;
        int blindTicks;
        int gravestepRetry;
        int spawnToken; // DworcSpawnIdentity token; owned Elementals compare against it on the server

        #region Puppet loadout
        public override bool UsesPuppetDifficultyScaling => false;
        protected override bool AnnounceInvasion => false;
        protected override bool AnnounceInvaderDefeat => false;
        protected override bool DespawnsOnPartyWipe => false;
        protected override string InvaderTitle => "Elemental Necromancer";
        protected override int HeadArmorItemType => ModContent.ItemType<ElementalNecromancerHeadpiece>();
        protected override int BodyArmorItemType => ModContent.ItemType<ElementalNecromancerRobe>();
        protected override int LegsArmorItemType => ModContent.ItemType<ElementalNecromancerBoots>();
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
        protected override float Acceleration => 0.07f;

        // Committed (no poise build) only for the Sigil's last 6 tell ticks and each eruption's last 6 ticks before rising.
        protected override bool CustomAttackCommitted
        {
            get
            {
                if (State == Act.DeathSigil)
                {
                    return AttackTimer > SigilTellTicks - 6 && AttackTimer <= SigilTellTicks;
                }

                if (State == Act.Eruption)
                {
                    for (int i = 0; i < markCount; i++)
                    {
                        int ticksToRise = markPlacedTick[i] + EruptionLeadTicks - AttackTimer;
                        if (ticksToRise > 0 && ticksToRise <= 6)
                        {
                            return true;
                        }
                    }
                }

                return false;
            }
        }

        protected override float? FreeFrontArmFacingAngle
        {
            get
            {
                if (State == Act.None)
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
                if (State == Act.None)
                {
                    return null;
                }

                return BackArmPose;
            }
        }

        // Gravestep: fades to 25% over the 30t departure, stays there through the relocation, reforms over 18t.
        internal override float BodyOpacity
        {
            get
            {
                if (State != Act.Gravestep)
                {
                    return 1f;
                }

                if (AttackTimer <= GravestepDepartTicks)
                {
                    return MathHelper.Lerp(1f, 0.25f, AttackTimer / (float)GravestepDepartTicks);
                }

                float reform = MathHelper.Clamp((AttackTimer - GravestepDepartTicks) / (float)GravestepReformTicks, 0f, 1f);
                return MathHelper.Lerp(0.25f, 1f, reform);
            }
        }

        protected override void DoMeleeAttack()
        {
            // Empty hands: every spell is released by the Custom action clock below.
        }

        protected override void DoRangedAttack()
        {
        }
        #endregion

        public override void SetStaticDefaults()
        {
            // Keeps the old sheet's slicing for the bestiary/banner; the puppet Player draws the body in game.
            Main.npcFrameCount[NPC.type] = 15;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.CursedInferno] = true;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Ichor] = true;
        }

        public override void SetDefaults()
        {
            NPC.npcSlots = 5;
            AnimationType = -1;
            NPC.aiStyle = -1;
            NPC.knockBackResist = 0.1f; //overridden by the PoiseProfiles entry
            NPC.damage = 0;
            NPC.defense = 75;
            NPC.width = 20;
            NPC.height = 40;
            NPC.lifeMax = 3500;
            NPC.lavaImmune = true;
            NPC.HitSound = SoundID.NPCHit1;
            NPC.DeathSound = SoundID.NPCDeath1;
            NPC.value = 12000;
            Banner = NPC.type;
            BannerItem = ModContent.ItemType<Banners.ElementalNecromancerBanner>();

            // Caster levers: kite a 19-31 tile band (~300-500px), backpedal rather than walk into melee, remember where the
            // player was. Teleport is the navigation unstick, neutral-only (see AI). No lever-driven heals: the ally heal
            // is the visible channel below and it never heals itself.
            tsorcRevampGlobalNPC casterGlobalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            casterGlobalNPC.RemembersLastKnownPos = true;
            casterGlobalNPC.PatrolMode = NPCs.PatrolMode.Pace;
            casterGlobalNPC.NavSearchRadius = 80;
            casterGlobalNPC.KiteRangeMin = 19f;
            casterGlobalNPC.KiteRangeMax = 31f;
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
            writer.Write((short)AttackTimer);
            writer.Write((short)AttackCooldown);
            writer.Write((byte)targetIndex);
            writer.Write((byte)pattern);
            writer.WriteVector2(lockedAim);
            writer.WriteVector2(graveSpot);
            writer.WriteVector2(gravestepDestination);
            writer.Write((byte)markFadeTimer);
            writer.Write((byte)markCount);
            for (int i = 0; i < markCount; i++)
            {
                writer.WriteVector2(markGround[i]);
                writer.Write((short)markPlacedTick[i]);
            }

            writer.Write((byte)healSlots.Count);
            for (int i = 0; i < healSlots.Count; i++)
            {
                writer.Write((short)healSlots[i]);
                writer.Write(healTokens[i]);
            }
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            base.ReceiveExtraAI(reader);
            State = (Act)reader.ReadByte();
            AttackTimer = reader.ReadInt16();
            AttackCooldown = reader.ReadInt16();
            targetIndex = reader.ReadByte();
            pattern = (Pattern)reader.ReadByte();
            lockedAim = reader.ReadVector2();
            graveSpot = reader.ReadVector2();
            gravestepDestination = reader.ReadVector2();
            markFadeTimer = reader.ReadByte();
            markCount = reader.ReadByte();
            for (int i = 0; i < markCount; i++)
            {
                markGround[i] = reader.ReadVector2();
                markPlacedTick[i] = reader.ReadInt16();
            }

            healSlots.Clear();
            healTokens.Clear();
            int healCount = reader.ReadByte();
            for (int i = 0; i < healCount; i++)
            {
                healSlots.Add(reader.ReadInt16());
                healTokens.Add(reader.ReadInt32());
            }
        }

        ///<summary>Its casting palm - where the Sigil forms and leaves. Pose maths, so it is valid on a dedicated server.</summary>
        Vector2 Palm => PuppetFrontHandPositionAt(FrontArmPose);

        ///<summary>The off hand, for client-side dust only.</summary>
        Vector2 OffPalm => PuppetBackHandPositionAt(BackArmPose);

        Player Target => Main.player[targetIndex];

        public override void OnStagger(NPC npc)
        {
            base.OnStagger(npc);

            if (State == Act.None)
            {
                return;
            }

            // Poise break: pending payouts are cancelled. An eruption's columns that already rose finish on their own
            // (they are projectiles); its unrisen marks are kept only to fade out over 12t, then never fire.
            if (State == Act.Eruption)
            {
                int kept = 0;
                for (int i = 0; i < markCount; i++)
                {
                    bool risen = markPlacedTick[i] + EruptionLeadTicks <= AttackTimer;
                    if (!risen)
                    {
                        markGround[kept] = markGround[i];
                        kept++;
                    }
                }

                markCount = kept;
                markFadeTimer = 12;
            }
            else
            {
                markCount = 0;
            }

            if (!Main.dedServ)
            {
                SoundEngine.PlaySound(SoundID.Item10 with { Volume = 0.6f, Pitch = -0.4f }, NPC.Center);
                for (int i = 0; i < 14; i++)
                {
                    Vector2 velocity = Main.rand.NextVector2Circular(3.5f, 3.5f);
                    Dust fizzle = Dust.NewDustDirect(NPC.position, NPC.width, NPC.height, DustID.Shadowflame, velocity.X, velocity.Y, 90, default, 1.3f);
                    fizzle.noGravity = true;
                }
            }

            cooldowns[(int)State] = CooldownFor(State);
            State = Act.None;
            AttackTimer = 0;
            healSlots.Clear();
            healTokens.Clear();
            AttackCooldown = Math.Max(AttackCooldown, NeutralGapTicks);
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                NPC.netUpdate = true;
            }
        }

        protected override void RunMovementAI(float speedMult)
        {
            // Stands still to cast (SlowDownDuringCustom also brakes); neutral uses the shared puppet navigation,
            // which honours the 19-31 tile kite band set in SetDefaults.
            if (State != Act.None)
            {
                NPC.velocity.X *= 0.85f;
                return;
            }

            base.RunMovementAI(speedMult);
        }

        public override void AI()
        {
            tsorcRevampGlobalNPC globalNPC = NPC.GetGlobalNPC<tsorcRevampGlobalNPC>();
            bool authority = Main.netMode != NetmodeID.MultiplayerClient;

            if (authority)
            {
                if (spawnToken == 0)
                {
                    spawnToken = NPC.GetGlobalNPC<DworcSpawnIdentity>().Token;
                }

                for (int i = 0; i < ActCount; i++)
                {
                    if (cooldowns[i] > 0)
                    {
                        cooldowns[i]--;
                    }
                }

                // The second Eruption card is latched in for good once it first drops below half.
                if (!belowHalfLatched && NPC.life < NPC.lifeMax / 2)
                {
                    belowHalfLatched = true;
                    attackBag?.Clear();
                }
            }

            if (authority && State == Act.None)
            {
                RunNeutralDecisions(globalNPC);
            }

            // Keep the Custom phase alive for as long as an action runs; face the target throughout.
            if (State != Act.None)
            {
                PhaseTimer = 2;
                NPC.target = targetIndex;
                FaceTarget();
            }

            globalNPC.CanTeleport = State == Act.None;
            base.AI();

            if (State != Act.None)
            {
                FaceTarget();
            }

            if (markFadeTimer > 0)
            {
                markFadeTimer--;
                EmitMarkDust(markFadeTimer / 12f);
                if (markFadeTimer == 0)
                {
                    markCount = 0;
                }
            }

            DebugAttackLabel = State + "/" + AttackTimer;

            // Idle identity: a faint violet haze about the robe.
            if (!Main.dedServ)
            {
                if (Main.rand.NextBool(5))
                {
                    Vector2 position = NPC.position + new Vector2(Main.rand.NextFloat(NPC.width), Main.rand.NextFloat(NPC.height));
                    Dust haze = Dust.NewDustDirect(position, 4, 4, DustID.PurpleTorch, 0f, -0.5f, 150, default, 0.8f);
                    haze.noGravity = true;
                    haze.velocity *= 0.25f;
                }

                Lighting.AddLight(NPC.Center, 0.14f, 0.08f, 0.24f);
            }
        }

        // Server, neutral only: Gravestep reaction first, then (after the neutral gap) the next action.
        void RunNeutralDecisions(tsorcRevampGlobalNPC globalNPC)
        {
            if (AttackCooldown > 0)
            {
                AttackCooldown--;
            }

            if (gravestepRetry > 0)
            {
                gravestepRetry--;
            }

            bool notBusy = globalNPC.StaggerTimer <= 0 && NPC.velocity.Y == 0f
                && globalNPC.TeleportCountdown == 0 && globalNPC.TeleportAppearanceTimer == 0;

            NPC.TargetClosest(false);
            Player player = Main.player[NPC.target];
            bool targetUsable = NPC.HasValidTarget && !player.dead && player.active && NPC.Distance(player.Center) < 1000f;
            if (!targetUsable)
            {
                crowdedTicks = 0;
                blindTicks = 0;
                return;
            }

            // Gravestep triggers: 90 straight neutral ticks with the target inside 192px (it failed to kite away),
            // or 180 straight ticks with no line of sight.
            bool lineOfSight = Collision.CanHitLine(NPC.Center, 1, 1, player.Center, 1, 1);
            bool targetClose = NPC.Distance(player.Center) < CrowdedRange;
            if (targetClose)
            {
                crowdedTicks++;
            }
            else
            {
                crowdedTicks = 0;
            }

            if (lineOfSight)
            {
                blindTicks = 0;
            }
            else
            {
                blindTicks++;
            }

            bool crowded = crowdedTicks >= CrowdedTriggerTicks;
            bool blind = blindTicks >= BlindTriggerTicks;
            bool gravestepReady = cooldowns[(int)Act.Gravestep] == 0 && gravestepRetry == 0;
            if (notBusy && gravestepReady && (crowded || blind))
            {
                targetIndex = NPC.target;
                if (TryFindGravestepSpot(blind))
                {
                    crowdedTicks = 0;
                    blindTicks = 0;
                    BeginAction(Act.Gravestep);
                    return;
                }

                // No legal spot: keep navigating and look again in 90t.
                gravestepRetry = GravestepRetryTicks;
            }

            if (notBusy && AttackCooldown <= 0)
            {
                PickAction();
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

        #region Selection
        void PickAction()
        {
            attackBag ??= new AttackBag<Act>(RefillAttackBag);
            targetIndex = NPC.target;

            // Ally Heal is readiness-driven, not a card: at most one utility action between two offences.
            Act chosen;
            if (cooldowns[(int)Act.AllyHeal] == 0 && !utilityUsedSinceOffence && TrySnapshotHealMarks())
            {
                chosen = Act.AllyHeal;
                utilityUsedSinceOffence = true;
            }
            else if (attackBag.TryDraw(IsEligible, out chosen))
            {
                utilityUsedSinceOffence = false;
            }
            else
            {
                // Nothing castable from here: keep kiting/navigating and look again shortly.
                AttackCooldown = 20;
                return;
            }

            if (chosen == Act.DeathSigil)
            {
                lockedAim = (Target.Center - Palm).SafeNormalize(new Vector2(NPC.direction, 0f));
            }

            if (chosen == Act.SummonElemental)
            {
                TryFindGraveSpot();
            }

            if (chosen == Act.Eruption)
            {
                // A, B and C at equal weight; D only below half HP, then at double weight.
                int roll = Main.rand.Next(3);
                if (belowHalfLatched)
                {
                    roll = Main.rand.Next(5);
                }

                pattern = (Pattern)Math.Min(roll, (int)Pattern.Predictive);
                markCount = 0;
            }

            BeginAction(chosen);
        }

        void BeginAction(Act action)
        {
            State = action;
            AttackTimer = 0;
            markFadeTimer = 0;
            if (action != Act.Eruption)
            {
                markCount = 0;
            }

            StartCustomAttack(2);
            NPC.netUpdate = true;
        }

        // Death Sigil twice, Summon and Eruption once each; a second Eruption once it has been below half.
        void RefillAttackBag(List<Act> cards)
        {
            cards.Add(Act.DeathSigil);
            cards.Add(Act.DeathSigil);
            cards.Add(Act.SummonElemental);
            cards.Add(Act.Eruption);
            if (belowHalfLatched)
            {
                cards.Add(Act.Eruption);
            }
        }

        bool IsEligible(Act candidate)
        {
            if (cooldowns[(int)candidate] > 0)
            {
                return false;
            }

            Player player = Main.player[NPC.target];
            float distance = NPC.Distance(player.Center);
            bool lineOfSight = Collision.CanHitLine(NPC.Center, 1, 1, player.Center, 1, 1);
            switch (candidate)
            {
                case Act.DeathSigil:
                    return lineOfSight && distance >= SigilMinRange && distance <= SigilMaxRange;

                case Act.SummonElemental:
                    return CountOwnedElementals() < MaxOwnedElementals && TryFindGraveSpot();

                case Act.Eruption:
                    {
                        bool inReach = lineOfSight && distance <= EruptionMaxRange;
                        return inReach && TryFindMarkGround(player.Center.X, player.Center.Y, false, out _);
                    }
            }

            return false;
        }

        void EndAction()
        {
            cooldowns[(int)State] = CooldownFor(State);
            State = Act.None;
            AttackTimer = 0;
            markCount = 0;
            healSlots.Clear();
            healTokens.Clear();
            AttackCooldown = NeutralGapTicks;
            EnterPhase(AttackPhase.Idle, 0);
            NPC.netUpdate = true;
        }

        static int CooldownFor(Act action)
        {
            switch (action)
            {
                case Act.DeathSigil:
                    return SigilCooldown;
                case Act.SummonElemental:
                    return SummonCooldown;
                case Act.Eruption:
                    return EruptionCooldown;
                case Act.AllyHeal:
                    return HealReadiness;
                case Act.Gravestep:
                    return GravestepCooldown;
            }

            return 0;
        }

        int TotalTicks
        {
            get
            {
                switch (State)
                {
                    case Act.DeathSigil:
                        return SigilTotalTicks;
                    case Act.SummonElemental:
                        return SummonTotalTicks;
                    case Act.Eruption:
                        return LastEruptionPlacementTick(pattern) + EruptionLeadTicks + EruptionTailTicks;
                    case Act.AllyHeal:
                        return HealTotalTicks;
                    case Act.Gravestep:
                        return GravestepTotalTicks;
                }

                return 1;
            }
        }
        #endregion

        #region Arm poses
        // Facing-space arm angles: 0 = straight ahead, negative = up, +PiOver2 = hanging at rest.
        // Measured in the body render: this head's face sits low, so a front hand raised past about -0.1 rad covers it.
        // Authored front poses stay at or below -0.1; only live aim at a target above lifts higher.
        // Each pose eases in over 10 ticks and back to rest over the last 15 ticks of the action.
        float PoseBlend
        {
            get
            {
                float blendIn = MathHelper.Clamp(AttackTimer / 10f, 0f, 1f);
                float blendOut = MathHelper.Clamp((TotalTicks - AttackTimer) / 15f, 0f, 1f);
                return Math.Min(blendIn, blendOut);
            }
        }

        // Facing-space angle from the shoulder along a world direction, clamped to what the arm can reach forward.
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
                switch (State)
                {
                    case Act.DeathSigil:
                        {
                            // The palm rises toward the target; after t24 it holds the locked aim.
                            Vector2 shoulder = NPC.Center + new Vector2(0f, -8f);
                            Vector2 aim = (Target.Center - shoulder).SafeNormalize(new Vector2(NPC.direction, 0f));
                            if (AttackTimer >= SigilAimLockTick)
                            {
                                aim = lockedAim;
                            }

                            pose = FacingAngleFor(aim);
                            break;
                        }

                    case Act.SummonElemental:
                        // Palm starts low over the spot and lifts the Elemental out of it across the ritual.
                        pose = MathHelper.SmoothStep(0.9f, -0.1f, MathHelper.Clamp(AttackTimer / (float)SummonRitualTicks, 0f, 1f));
                        break;

                    case Act.Eruption:
                        {
                            // Raised (-0.1); for 12t after each mark is placed the palm thrusts down at the floor (+1.0) and back.
                            pose = -0.1f;
                            int sinceMark = TicksSinceLatestMark();
                            if (sinceMark >= 0 && sinceMark < 12)
                            {
                                float thrust = (float)Math.Sin(sinceMark / 12f * MathHelper.Pi);
                                pose = MathHelper.Lerp(-0.1f, 1.0f, thrust);
                            }

                            break;
                        }

                    case Act.AllyHeal:
                        pose = -0.1f;
                        break;

                    case Act.Gravestep:
                        pose = 1.3f; //drawn in against the robe
                        break;
                }

                return MathHelper.SmoothStep(MathHelper.PiOver2, pose, PoseBlend);
            }
        }

        float BackArmPose
        {
            get
            {
                float pose = MathHelper.PiOver2;
                switch (State)
                {
                    case Act.DeathSigil:
                        pose = 0.4f; //held at the chest, gathering violet light
                        break;

                    case Act.SummonElemental:
                        pose = MathHelper.SmoothStep(0.9f, -0.25f, MathHelper.Clamp(AttackTimer / (float)SummonRitualTicks, 0f, 1f));
                        break;

                    case Act.Eruption:
                        pose = -0.25f;
                        break;

                    case Act.AllyHeal:
                        pose = -0.3f;
                        break;

                    case Act.Gravestep:
                        pose = 1.2f;
                        break;
                }

                return MathHelper.SmoothStep(MathHelper.PiOver2, pose, PoseBlend);
            }
        }

        // Ticks since the most recently placed eruption mark, or -1 when none is placed yet.
        int TicksSinceLatestMark()
        {
            int latest = -1;
            for (int i = 0; i < markCount; i++)
            {
                latest = Math.Max(latest, markPlacedTick[i]);
            }

            if (latest < 0)
            {
                return -1;
            }

            return AttackTimer - latest;
        }
        #endregion

        #region Actions
        protected override void DoCustomTick(int ticksRemaining)
        {
            if (State == Act.None)
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
                case Act.DeathSigil:
                    RunDeathSigil();
                    break;
                case Act.SummonElemental:
                    RunSummonElemental();
                    break;
                case Act.Eruption:
                    RunEruption();
                    break;
                case Act.AllyHeal:
                    RunAllyHeal();
                    break;
                case Act.Gravestep:
                    RunGravestep();
                    break;
            }

            if (authority && State != Act.None && AttackTimer >= TotalTicks)
            {
                EndAction();
            }
        }

        void RunDeathSigil()
        {
            bool authority = Main.netMode != NetmodeID.MultiplayerClient;
            Vector2 palm = Palm;

            if (authority && AttackTimer == SigilAimLockTick)
            {
                lockedAim = (Target.Center - palm).SafeNormalize(new Vector2(NPC.direction, 0f));
                NPC.netUpdate = true;
            }

            if (!Main.dedServ && AttackTimer <= SigilTellTicks)
            {
                if (AttackTimer == 1)
                {
                    SoundEngine.PlaySound(SoundID.Item17 with { Volume = 0.65f, Pitch = -0.35f }, NPC.Center);
                }

                // Violet curse gathering into the palm, thicker as release nears; the rear hand at the chest glows with it.
                float progress = AttackTimer / (float)SigilTellTicks;
                int gatherCount = 1 + (int)(progress * 2f);
                for (int i = 0; i < gatherCount; i++)
                {
                    float angle = Main.rand.NextFloat(MathHelper.TwoPi);
                    Vector2 position = palm + angle.ToRotationVector2() * Main.rand.NextFloat(8f, 26f);
                    Dust gather = Dust.NewDustPerfect(position, DustID.Shadowflame, (palm - position) * 0.15f, 100, default, 1.1f);
                    gather.noGravity = true;
                }

                if (Main.rand.NextBool(3))
                {
                    Dust ember = Dust.NewDustPerfect(OffPalm + Main.rand.NextVector2Circular(4f, 4f), DustID.PurpleTorch, new Vector2(0f, -0.6f), 120, default, 0.9f);
                    ember.noGravity = true;
                }

                Lighting.AddLight(palm, 0.35f * progress, 0.15f * progress, 0.5f * progress);
                Lighting.AddLight(OffPalm, 0.2f * progress, 0.08f * progress, 0.3f * progress);
            }

            if (AttackTimer != SigilTellTicks)
            {
                return;
            }

            if (authority)
            {
                Projectile.NewProjectile(NPC.GetSource_FromThis(), palm, lockedAim * SigilSpeed,
                    ModContent.ProjectileType<ElementalNecromancerDeathSigil>(), EnemyDamage.Projectile(SigilExpertDamage), 1f, Main.myPlayer);
            }

            if (!Main.dedServ)
            {
                SoundEngine.PlaySound(SoundID.Item8 with { Volume = 0.7f, Pitch = -0.5f }, palm);
            }
        }

        void RunSummonElemental()
        {
            if (AttackTimer > SummonRitualTicks)
            {
                return; //waiting out the emergence + grace, then recovery
            }

            if (!Main.dedServ)
            {
                if (AttackTimer == 1)
                {
                    SoundEngine.PlaySound(SoundID.Item17 with { Volume = 0.7f, Pitch = -0.7f }, NPC.Center);
                }

                // Purple + blue simmer across the spot from t0.
                for (int i = 0; i < 2; i++)
                {
                    int dustType = DustID.PurpleTorch;
                    if (i == 1)
                    {
                        dustType = DustID.BlueTorch;
                    }

                    Vector2 position = new Vector2(graveSpot.X - 16f + Main.rand.NextFloat(32f), graveSpot.Y - 4f);
                    Dust simmer = Dust.NewDustPerfect(position, dustType, new Vector2(0f, -1.4f), 100, default, 1.1f);
                    simmer.noGravity = true;
                }

                // Over the last 36t a faint Elemental-shaped shimmer stands up out of the spot: a 20x40 body of motes.
                if (AttackTimer > SummonRitualTicks - 36)
                {
                    float rise = (AttackTimer - (SummonRitualTicks - 36)) / 36f;
                    Vector2 position = new Vector2(graveSpot.X - 10f + Main.rand.NextFloat(20f), graveSpot.Y - Main.rand.NextFloat(40f * rise));
                    Dust shimmer = Dust.NewDustPerfect(position, DustID.PurpleTorch, Vector2.Zero, 150, default, 0.9f);
                    shimmer.noGravity = true;
                }

                if (Main.rand.NextBool(2))
                {
                    Dust front = Dust.NewDustPerfect(Palm, DustID.PurpleTorch, new Vector2(0f, -1.5f), 100, default, 1f);
                    front.noGravity = true;
                    Dust back = Dust.NewDustPerfect(OffPalm, DustID.BlueTorch, new Vector2(0f, -1.5f), 100, default, 1f);
                    back.noGravity = true;
                }
            }

            if (AttackTimer != SummonRitualTicks)
            {
                return;
            }

            bool authority = Main.netMode != NetmodeID.MultiplayerClient;
            if (authority && CountOwnedElementals() < MaxOwnedElementals)
            {
                // NewNPC takes the bottom-centre, so the Elemental stands on the spot, then claws up out of it.
                int spawned = NPC.NewNPC(NPC.GetSource_FromAI(), (int)graveSpot.X, (int)graveSpot.Y, ModContent.NPCType<Elemental>());
                if (spawned < Main.maxNPCs && Main.npc[spawned].ModNPC is Elemental elemental)
                {
                    elemental.BindOwner(NPC.whoAmI, spawnToken);
                    Main.npc[spawned].velocity.Y = -5f;
                    if (Main.netMode == NetmodeID.Server)
                    {
                        NetMessage.SendData(MessageID.SyncNPC, -1, -1, null, spawned);
                    }
                }
            }

            if (!Main.dedServ)
            {
                SoundEngine.PlaySound(SoundID.NPCDeath6 with { Volume = 0.6f, Pitch = 0.3f }, graveSpot);
                for (int i = 0; i < 24; i++)
                {
                    Vector2 velocity = new Vector2(Main.rand.NextFloat(-2.5f, 2.5f), Main.rand.NextFloat(-5f, -1.5f));
                    Dust burst = Dust.NewDustPerfect(graveSpot + new Vector2(Main.rand.NextFloat(-14f, 14f), -6f), DustID.PurpleTorch, velocity, 60, default, 1.4f);
                    burst.noGravity = true;
                }
            }
        }

        void RunEruption()
        {
            bool authority = Main.netMode != NetmodeID.MultiplayerClient;

            if (authority)
            {
                PlaceScheduledMarks();

                // Each mark's column rises exactly 90t after the mark appeared.
                for (int i = 0; i < markCount; i++)
                {
                    if (markPlacedTick[i] + EruptionLeadTicks != AttackTimer)
                    {
                        continue;
                    }

                    Vector2 columnCenter = markGround[i] - new Vector2(0f, ElementalEruptionColumn.Height / 2f);
                    Projectile.NewProjectile(NPC.GetSource_FromThis(), columnCenter, Vector2.Zero,
                        ModContent.ProjectileType<ElementalEruptionColumn>(), EnemyDamage.Projectile(EruptionExpertDamage), 0f, Main.myPlayer);
                }
            }

            EmitMarkDust(1f);

            if (!Main.dedServ && Main.rand.NextBool(2))
            {
                Dust front = Dust.NewDustPerfect(Palm, DustID.PurpleTorch, new Vector2(0f, 1.2f), 100, default, 1f);
                front.noGravity = true;
                Dust back = Dust.NewDustPerfect(OffPalm, DustID.BlueTorch, new Vector2(0f, -0.8f), 100, default, 0.9f);
                back.noGravity = true;
            }
        }

        // Server: lays this tick's marks for the chosen pattern. Marks are synced; a mark with no valid floor is skipped.
        void PlaceScheduledMarks()
        {
            Player player = Target;
            bool placedAny = false;

            switch (pattern)
            {
                case Pattern.Single:
                    if (AttackTimer == 1)
                    {
                        placedAny |= TryAddMark(player.Center.X, player.Center.Y, false);
                    }
                    break;

                case Pattern.Double:
                    // The second column goes under where the target is at t120, not where it was.
                    if (AttackTimer == 1 || AttackTimer == 121)
                    {
                        placedAny |= TryAddMark(player.Center.X, player.Center.Y, false);
                    }
                    break;

                case Pattern.Triple:
                    if (AttackTimer == 1)
                    {
                        placedAny |= TryAddMark(player.Center.X, player.Center.Y, true);
                        placedAny |= TryAddMark(player.Center.X - TripleSpacing, player.Center.Y, true);
                        placedAny |= TryAddMark(player.Center.X + TripleSpacing, player.Center.Y, true);
                    }
                    break;

                case Pattern.Predictive:
                    // Every 90t: aim where the target will be in 90t (position + velocity x 90, at most 320px out).
                    if (AttackTimer >= 1 && AttackTimer <= LastEruptionPlacementTick(Pattern.Predictive) && (AttackTimer - 1) % PredictiveMarkInterval == 0)
                    {
                        Vector2 lead = player.velocity * EruptionLeadTicks;
                        if (lead.Length() > PredictionClamp)
                        {
                            lead = lead.SafeNormalize(Vector2.Zero) * PredictionClamp;
                        }

                        Vector2 predicted = player.Center + lead;
                        placedAny |= TryAddMark(predicted.X, predicted.Y, false);
                    }
                    break;
            }

            if (placedAny)
            {
                NPC.netUpdate = true;
            }
        }

        // Server: finds the floor for one mark and records it (locked once placed). Triple marks need 3 flat solid tiles
        // and slide to the nearest valid spot within 3 tiles sideways.
        bool TryAddMark(float worldX, float worldY, bool needsFlatThree)
        {
            if (markCount >= MaxMarks)
            {
                return false;
            }

            int[] sideSteps = { 0 };
            if (needsFlatThree)
            {
                sideSteps = new int[] { 0, 1, -1, 2, -2, 3, -3 };
            }

            foreach (int step in sideSteps)
            {
                if (TryFindMarkGround(worldX + step * 16f, worldY, needsFlatThree, out Vector2 ground))
                {
                    markGround[markCount] = ground;
                    markPlacedTick[markCount] = AttackTimer;
                    markCount++;
                    return true;
                }
            }

            return false;
        }

        // Every machine, from the synced marks: purple dust rising off a 3-tile floor patch for the whole 90t lead,
        // thickening over its last 20t, plus a low hum when the mark appears. strength scales it during a stagger fade.
        void EmitMarkDust(float strength)
        {
            if (Main.dedServ)
            {
                return;
            }

            for (int i = 0; i < markCount; i++)
            {
                int ticksToRise = markPlacedTick[i] + EruptionLeadTicks - AttackTimer;
                bool waiting = ticksToRise > 0 || markFadeTimer > 0;
                if (!waiting)
                {
                    continue;
                }

                Vector2 ground = markGround[i];
                if (markFadeTimer == 0 && AttackTimer - markPlacedTick[i] == 1)
                {
                    SoundEngine.PlaySound(SoundID.Item15 with { Volume = 0.5f, Pitch = -0.8f }, ground);
                }

                int count = 1;
                if (markFadeTimer == 0 && ticksToRise <= 20)
                {
                    count = 3;
                }

                for (int j = 0; j < count; j++)
                {
                    if (Main.rand.NextFloat() > strength)
                    {
                        continue;
                    }

                    Vector2 position = new Vector2(ground.X - 24f + Main.rand.NextFloat(48f), ground.Y - 2f);
                    Vector2 velocity = new Vector2(Main.rand.NextFloat(-0.3f, 0.3f), Main.rand.NextFloat(-3f, -1f));
                    Dust rising = Dust.NewDustPerfect(position, DustID.PurpleTorch, velocity, 80, default, 1.2f);
                    rising.noGravity = true;
                }

                Lighting.AddLight(ground - new Vector2(0f, 8f), 0.3f * strength, 0.12f * strength, 0.5f * strength);
            }
        }

        void RunAllyHeal()
        {
            if (AttackTimer > HealChannelTicks)
            {
                return; //recovery, no armor
            }

            if (!Main.dedServ)
            {
                if (AttackTimer == 1)
                {
                    SoundEngine.PlaySound(SoundID.Item4 with { Volume = 0.5f, Pitch = -0.5f }, NPC.Center);
                }

                // Blue-violet motes drifting from the raised palms to every marked ally, and shimmering on them.
                for (int i = 0; i < healSlots.Count; i++)
                {
                    NPC ally = Main.npc[healSlots[i]];
                    if (!ally.active)
                    {
                        continue;
                    }

                    if (Main.rand.NextBool(3))
                    {
                        Vector2 from = Palm;
                        if (Main.rand.NextBool())
                        {
                            from = OffPalm;
                        }

                        Vector2 toAlly = (ally.Center - from).SafeNormalize(Vector2.UnitX);
                        Dust drift = Dust.NewDustPerfect(from, DustID.BlueTorch, toAlly * 4f, 90, default, 1f);
                        drift.noGravity = true;
                    }

                    if (Main.rand.NextBool(4))
                    {
                        Dust shimmer = Dust.NewDustDirect(ally.position, ally.width, ally.height, DustID.PurpleTorch, 0f, -1f, 110, default, 0.9f);
                        shimmer.noGravity = true;
                    }
                }
            }

            if (AttackTimer != HealChannelTicks || Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            // Server payout: 10% max HP to each marked ally still alive, still the same NPC (token) and still in range.
            for (int i = 0; i < healSlots.Count; i++)
            {
                NPC ally = Main.npc[healSlots[i]];
                bool sameAlly = ally.active && ally.life > 0 && !ally.friendly && ally.GetGlobalNPC<DworcSpawnIdentity>().Token == healTokens[i];
                if (!sameAlly || ally.Distance(NPC.Center) > HealRange)
                {
                    continue;
                }

                int amount = Math.Min(ally.lifeMax - ally.life, Math.Max(1, (int)(ally.lifeMax * HealFraction)));
                if (amount > 0)
                {
                    ally.life += amount;
                    ally.HealEffect(amount);
                    ally.netUpdate = true;
                }
            }
        }

        void RunGravestep()
        {
            if (!Main.dedServ && AttackTimer <= GravestepDepartTicks)
            {
                if (AttackTimer == 1)
                {
                    SoundEngine.PlaySound(SoundID.Item79 with { Volume = 0.5f, Pitch = -0.3f }, NPC.Center);
                }

                // The arrival wisp: a purple flicker where it will reform, shown for the whole departure.
                if (Main.rand.NextBool(2))
                {
                    Vector2 position = gravestepDestination + new Vector2(Main.rand.NextFloat(-10f, 10f), -Main.rand.NextFloat(40f));
                    Dust wisp = Dust.NewDustPerfect(position, DustID.PurpleTorch, new Vector2(0f, -0.8f), 120, default, 1f);
                    wisp.noGravity = true;
                }
            }

            if (AttackTimer != GravestepDepartTicks)
            {
                return;
            }

            if (!Main.dedServ)
            {
                // ~18 motes at each end; this machine has not moved the body yet, so NPC.Center is still the departure.
                Vector2 arrivalCenter = gravestepDestination - new Vector2(0f, NPC.height / 2f);
                for (int i = 0; i < 18; i++)
                {
                    Dust leave = Dust.NewDustPerfect(NPC.Center + Main.rand.NextVector2Circular(10f, 20f), DustID.Shadowflame, Main.rand.NextVector2Circular(2.5f, 2.5f), 90, default, 1.2f);
                    leave.noGravity = true;
                    Dust arrive = Dust.NewDustPerfect(arrivalCenter + Main.rand.NextVector2Circular(10f, 20f), DustID.PurpleTorch, Main.rand.NextVector2Circular(2.5f, 2.5f), 90, default, 1.2f);
                    arrive.noGravity = true;
                }

                SoundEngine.PlaySound(SoundID.Item8 with { Volume = 0.6f }, gravestepDestination);
            }

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                NPC.Bottom = gravestepDestination;
                NPC.velocity = Vector2.Zero;
                NPC.netUpdate = true;
            }
        }
        #endregion

        #region Helpers
        ///<summary>Server: Elementals this Necromancer raised that are still alive.</summary>
        int CountOwnedElementals()
        {
            int count = 0;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (npc.active && npc.ModNPC is Elemental elemental && elemental.BelongsTo(NPC.whoAmI, spawnToken))
                {
                    count++;
                }
            }

            return count;
        }

        /// <summary>Server: supported ground 48-112px in front of it with room for an Elemental's body, within 64px of its
        /// feet level and at least 96px from every player. Stored in graveSpot (ground level). Returns whether one exists.</summary>
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

                bool bodyClear = !Collision.SolidCollision(new Vector2(graveX - 12f, groundY - 46f), 24, 44);
                bool nearLevel = Math.Abs(groundY - NPC.Bottom.Y) <= 64f;
                Vector2 spot = new Vector2(graveX, groundY);
                bool playerClear = PlayersFarFrom(spot, 96f);

                if (bodyClear && nearLevel && playerClear)
                {
                    graveSpot = spot;
                    return true;
                }
            }

            return false;
        }

        /// <summary>Server: a supported, body-clear floor point 300-500px from the target, within 480px of where it stands,
        /// at least 128px from every player, and (when it lost sight) with line of sight to the target. Stored in
        /// gravestepDestination (its new feet).</summary>
        bool TryFindGravestepSpot(bool needsLineOfSight)
        {
            Player player = Target;
            for (int attempt = 0; attempt < 40; attempt++)
            {
                int side = 1;
                if (Main.rand.NextBool())
                {
                    side = -1;
                }

                float x = player.Center.X + side * Main.rand.NextFloat(300f, 500f);
                if (!PuppetGroundDustWave.TryFindGroundY(x, player.Bottom.Y, out float groundY))
                {
                    continue;
                }

                Vector2 spot = new Vector2(x, groundY);
                bool bodyClear = !Collision.SolidCollision(new Vector2(x - 12f, groundY - 46f), 24, 44);
                float targetDistance = Vector2.Distance(spot, player.Center);
                bool inBand = targetDistance >= 300f && targetDistance <= 500f;
                bool closeToHere = Vector2.Distance(spot, NPC.Bottom) <= 480f;
                bool playerClear = PlayersFarFrom(spot, 128f);
                bool sightOk = !needsLineOfSight || Collision.CanHitLine(spot - new Vector2(0f, 24f), 1, 1, player.Center, 1, 1);

                if (bodyClear && inBand && closeToHere && playerClear && sightOk)
                {
                    gravestepDestination = spot;
                    return true;
                }
            }

            return false;
        }

        static bool PlayersFarFrom(Vector2 spot, float minimumDistance)
        {
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player player = Main.player[i];
                if (player.active && !player.dead && player.Distance(spot) < minimumDistance)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>The floor under a point: scanning down from it up to 12 tiles, the first solid tile top with open air
        /// above. needsFlatThree also requires the tiles either side to be solid at the same height with air above.</summary>
        static bool TryFindMarkGround(float worldX, float worldY, bool needsFlatThree, out Vector2 ground)
        {
            ground = Vector2.Zero;
            int tileX = (int)(worldX / 16f);
            int startY = (int)(worldY / 16f);
            if (tileX < 6 || tileX > Main.maxTilesX - 6 || startY < 6)
            {
                return false;
            }

            for (int depth = 0; depth <= 12; depth++)
            {
                int tileY = startY + depth;
                if (tileY >= Main.maxTilesY - 6)
                {
                    return false;
                }

                bool floor = WorldGen.SolidTile(tileX, tileY) && !WorldGen.SolidTile(tileX, tileY - 1);
                if (!floor)
                {
                    continue;
                }

                if (needsFlatThree)
                {
                    bool leftFlat = WorldGen.SolidTile(tileX - 1, tileY) && !WorldGen.SolidTile(tileX - 1, tileY - 1);
                    bool rightFlat = WorldGen.SolidTile(tileX + 1, tileY) && !WorldGen.SolidTile(tileX + 1, tileY - 1);
                    if (!leftFlat || !rightFlat)
                    {
                        return false;
                    }
                }

                ground = new Vector2(tileX * 16f + 8f, tileY * 16f);
                return true;
            }

            return false;
        }

        /// <summary>Server, when Ally Heal is considered: snapshots wounded hostile NPCs within 800px (not itself; its own
        /// Elementals count) by slot + spawn token. Returns whether there is anyone to heal.</summary>
        bool TrySnapshotHealMarks()
        {
            healSlots.Clear();
            healTokens.Clear();
            for (int i = 0; i < Main.maxNPCs && healSlots.Count < MaxHealMarks; i++)
            {
                NPC ally = Main.npc[i];
                if (i == NPC.whoAmI || !ally.active || ally.friendly || ally.life <= 0 || ally.life >= ally.lifeMax)
                {
                    continue;
                }

                bool dummy = ally.type == NPCID.TargetDummy || ally.immortal || ally.dontTakeDamage;
                bool sharedSegment = ally.realLife >= 0 && ally.realLife != i;
                if (dummy || sharedSegment || ally.Distance(NPC.Center) > HealRange)
                {
                    continue;
                }

                healSlots.Add(i);
                healTokens.Add(ally.GetGlobalNPC<DworcSpawnIdentity>().Token);
            }

            return healSlots.Count > 0;
        }

        static int LastEruptionPlacementTick(Pattern eruptionPattern)
        {
            switch (eruptionPattern)
            {
                case Pattern.Double:
                    return 121;
                case Pattern.Predictive:
                    return 1 + (PredictiveMarkCount - 1) * PredictiveMarkInterval;
            }

            return 1;
        }
        #endregion

        #region Draw
        // The Death Sigil skull grows in the casting palm over the last 18 tell ticks: the real projectile sheet, upright,
        // mirrored to its facing, scale 0 -> 1. Drawn after the puppet so it sits in front of the hand.
        public override void PostDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            base.PostDraw(spriteBatch, screenPos, drawColor);

            bool previewing = State == Act.DeathSigil && AttackTimer > SigilTellTicks - SigilPreviewTicks && AttackTimer < SigilTellTicks;
            if (!previewing)
            {
                return;
            }

            int projectileType = ModContent.ProjectileType<ElementalNecromancerDeathSigil>();
            Main.instance.LoadProjectile(projectileType);
            Texture2D texture = TextureAssets.Projectile[projectileType].Value;
            int frameIndex = (AttackTimer / 4) % ElementalNecromancerDeathSigil.FrameCount;
            Rectangle frame = new Rectangle(0, frameIndex * ElementalNecromancerDeathSigil.FrameHeight, texture.Width, ElementalNecromancerDeathSigil.FrameHeight);
            float growth = (AttackTimer - (SigilTellTicks - SigilPreviewTicks)) / (float)SigilPreviewTicks;
            SpriteEffects effects = SpriteEffects.None;
            if (NPC.direction < 0)
            {
                effects = SpriteEffects.FlipHorizontally;
            }

            spriteBatch.Draw(texture, Palm - screenPos, frame, Color.White * (0.5f + 0.5f * growth), 0f, frame.Size() * 0.5f, growth, effects, 0f);
        }
        #endregion

        #region Death
        // Replaces the puppet's generic blood burst. Runs on every client (OnKill does not run on MP clients).
        // Its owned Elementals die through their own server-side owner check within 30 ticks.
        public override void HitEffect(NPC.HitInfo hit)
        {
            if (NPC.life > 0 || Main.dedServ)
            {
                return;
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
            npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<TeslaBolt>(), 1, 8, 16));
            npcLoot.Add(ItemDropRule.ByCondition(tsorcRevamp.tsorcItemDropRuleConditions.CursedRule, ModContent.ItemType<StarlightShardItem>(), 6));
        }
    }
}

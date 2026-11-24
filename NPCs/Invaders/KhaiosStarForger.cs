using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using ReLogic.Utilities;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items.Weapons.Summon.Runeterra.AurelionSol.Bases;
using tsorcRevamp.Content.Items.Weapons.Summon.Runeterra.AurelionSol.Projectiles.Dragons.StarForger;
using tsorcRevamp.Content.Items.Weapons.Summon.Runeterra.AurelionSol.Sounds.CenterOfTheUniverse;
using tsorcRevamp.Content.Projectiles.Enemy.Khaios;
using tsorcRevamp.Utilities;

namespace tsorcRevamp.NPCs.Invaders
{
    /// <summary>
    /// Hostile Spirit of the Universe (StarForger) dragon summoned by Phase 1 Khaios below 66% HP.
    /// Acts as a slow, lumbering cosmic sentry that hovers near Khaios or gently drifts out to
    /// flank the player. All turns follow a smooth clockwise or counter-clockwise curve (capped
    /// angular turn rate, never snapping or flipping sharply). Detonates in a stardust explosion
    /// when Phase 1 Khaios dies.
    /// ai[0] = owner Khaios NPC.whoAmI.
    /// </summary>
    public class KhaiosStarForger : ModNPC, IDebugAttackLabel
    {
        private enum DragonState : byte
        {
            ArrivalFade = 0,
            HoverGuard = 1,
            FlankDrift = 2,
            BreathTell = 3,
            BreathSweep = 4,
            BreathRecover = 5,
            CometCast = 6,
            BreathApproach = 7,
        }

        private const float DragonScale = 0.80f;
        private const int ArrivalFadeTicks = 60;
        private const int ArrivalWaitTicks = 30;
        private const int BreathApproachMaxTicks = 180;
        private const int BreathTellTicks = 50;
        private const int BreathSweepTicks = 84;
        private const int BreathRecoverTicks = 36;
        private const int CometCastTicks = 82;
        private const int AttackCooldownMinTicks = 300;
        private const int AttackCooldownMaxTicks = 400;
        private const int BreathAudioFadeInTicks = 6;
        private const int BreathAudioFadeOutTicks = 16;

        // Movement tuning: slow lumbering flight with wide, graceful banking turns.
        private const float MaxTurnRateRadPerTick = 0.028f;
        private const float DriftCruiseSpeed = 4.4f;
        private const float HoverStrafeSpeed = 1.6f;
        private const float BreathApproachCruiseSpeed = 6.2f;
        private const float BreathPursuitMinSpeed = 1.6f;
        private const float BreathPursuitMaxSpeed = 4.4f;
        private const float BreathStartRangePixels = 310f;
        private const float BreathRangePixels = 460f;
        private const float BreathHalfConeRadians = 0.24f;
        private const float BreathTotalSweepRadians = 0.96f;

        private static readonly string StarForgerTextureBase = UsefulFunctions.RefactorableFilepath(typeof(StarForger));
        private static readonly string SoundBase = UsefulFunctions.RefactorableFilepath(typeof(CenterOfTheUniverseSound));

        private BodySegment frontBody;
        private BodySegment backBody;
        private BodySegment backRightLeg;
        private BodySegment backLeftLeg;
        private BodySegment frontRightLeg;
        private BodySegment frontLeftLeg;
        private BodySegment[] neckSegments;
        private BodySegment head;
        private BodySegment mouth;
        private bool bodyInitialized;

        private DragonState state = DragonState.ArrivalFade;
        private int stateTimer;
        private int attackCooldown = 90;
        private int flankSide = 1;
        private float headingAngle;
        private float breathAngle;
        private float breathSweepDirection = 1f;
        private float breathOpacity;
        private int breathHitCooldown;
        private Vector2 cometTargetPos;
        private int activeCometZoneIndex = -1;
        private bool lastAttackWasComet;
        private bool deathExploded;

        private SlotId breathLoopSlot;
        private float breathAudioGain;
        private static Effect breathShader;

        public override string Texture => StarForgerTextureBase + "_Dragon_Head";

        string IDebugAttackLabel.DebugAttackLabel
        {
            get
            {
                if (state == DragonState.ArrivalFade)
                {
                    return "Arrival Materialize";
                }

                if (state == DragonState.HoverGuard)
                {
                    return "Hover Guard";
                }

                if (state == DragonState.FlankDrift)
                {
                    return "Flank Drift";
                }

                if (state == DragonState.BreathApproach)
                {
                    return "Cosmic Breath: Approach";
                }

                if (state == DragonState.BreathTell)
                {
                    return "Cosmic Breath: Telegraph";
                }

                if (state == DragonState.BreathSweep)
                {
                    return "Cosmic Breath: Sweep";
                }

                if (state == DragonState.BreathRecover)
                {
                    return "Cosmic Breath: Recover";
                }

                if (state == DragonState.CometCast)
                {
                    return "Grand Comet Call";
                }

                return state.ToString();
            }
        }

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[Type] = 1;
            NPCID.Sets.SpecificDebuffImmunity[Type][BuffID.Confused] = true;
            NPCID.Sets.NPCBestiaryDrawModifiers hideBestiary = new NPCID.Sets.NPCBestiaryDrawModifiers
            {
                Hide = true,
            };
            NPCID.Sets.NPCBestiaryDrawOffset.Add(Type, hideBestiary);
        }

        public override void SetDefaults()
        {
            NPC.width = 76;
            NPC.height = 56;
            NPC.lifeMax = 1000;
            NPC.defense = 9999;
            NPC.damage = 0;
            NPC.knockBackResist = 0f;
            NPC.dontTakeDamage = true;
            NPC.noGravity = true;
            NPC.noTileCollide = true;
            NPC.aiStyle = -1;
            NPC.npcSlots = 0f;
            NPC.alpha = 255;
        }

        public override bool CheckActive() => false;

        /// <summary>Called by Phase 1 Khaios on death so the dragon dies in a stardust explosion.</summary>
        public void TriggerPhaseOneDeathExplosion()
        {
            SpawnDeathStardustExplosion();
            StopBreathLoop();

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                NPC.active = false;
                NPC.netUpdate = true;
            }
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write((byte)state);
            writer.Write(stateTimer);
            writer.Write(attackCooldown);
            writer.Write((sbyte)flankSide);
            writer.Write(headingAngle);
            writer.Write(breathAngle);
            writer.Write(breathSweepDirection);
            writer.WriteVector2(cometTargetPos);
            writer.Write(activeCometZoneIndex);
            writer.Write(lastAttackWasComet);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            state = (DragonState)reader.ReadByte();
            stateTimer = reader.ReadInt32();
            attackCooldown = reader.ReadInt32();
            flankSide = reader.ReadSByte();
            headingAngle = reader.ReadSingle();
            breathAngle = reader.ReadSingle();
            breathSweepDirection = reader.ReadSingle();
            cometTargetPos = reader.ReadVector2();
            activeCometZoneIndex = reader.ReadInt32();
            lastAttackWasComet = reader.ReadBoolean();
        }

        public override void AI()
        {
            EnsureBodyRig();

            int ownerIndex = (int)NPC.ai[0];
            bool ownerAlive = ownerIndex >= 0
                && ownerIndex < Main.maxNPCs
                && Main.npc[ownerIndex].active
                && Main.npc[ownerIndex].type == ModContent.NPCType<Khaios>();

            if (!ownerAlive)
            {
                TriggerPhaseOneDeathExplosion();
                return;
            }

            NPC owner = Main.npc[ownerIndex];
            NPC.target = owner.target;
            Player target = (NPC.target >= 0 && NPC.target < Main.maxPlayers && Main.player[NPC.target].active && !Main.player[NPC.target].dead)
                ? Main.player[NPC.target]
                : null;

            stateTimer++;
            if (attackCooldown > 0)
            {
                attackCooldown--;
            }

            if (breathHitCooldown > 0)
            {
                breathHitCooldown--;
            }

            switch (state)
            {
                case DragonState.ArrivalFade:
                    RunArrivalFade(owner, target);
                    break;
                case DragonState.HoverGuard:
                    RunHoverGuard(owner, target);
                    break;
                case DragonState.FlankDrift:
                    RunFlankDrift(owner, target);
                    break;
                case DragonState.BreathApproach:
                    RunBreathApproach(target);
                    break;
                case DragonState.BreathTell:
                    RunBreathTell(target);
                    break;
                case DragonState.BreathSweep:
                    RunBreathSweep(target);
                    break;
                case DragonState.BreathRecover:
                    RunBreathRecover();
                    break;
                case DragonState.CometCast:
                    RunCometCast(target);
                    break;
            }

            UpdateBreathLoopAudio(state == DragonState.BreathSweep);
            UpdateArticulatedRig(target);

            if (!Main.dedServ)
            {
                Lighting.AddLight(NPC.Center, 0.45f, 0.35f, 0.78f);

                if (Main.rand.NextBool(3))
                {
                    int ambientType = Main.rand.NextBool(3) ? DustID.GoldFlame : DustID.PurpleTorch;
                    Color ambientColor = ambientType == DustID.GoldFlame ? new Color(255, 220, 100) : Color.MediumPurple;
                    // dust-lint: ok (gentle ambient cosmic motes orbiting the hovering dragon)
                    Dust ambient = Dust.NewDustPerfect(
                        NPC.Center + Main.rand.NextVector2Circular(46f, 28f),
                        ambientType,
                        new Vector2(Main.rand.NextFloat(-0.6f, 0.6f), Main.rand.NextFloat(-1.1f, -0.2f)),
                        40,
                        ambientColor,
                        Main.rand.NextFloat(0.85f, 1.35f));
                    ambient.noGravity = true;
                }
            }
        }

        private void RunArrivalFade(NPC owner, Player target)
        {
            float fadeProgress = MathHelper.Clamp(stateTimer / (float)ArrivalFadeTicks, 0f, 1f);
            NPC.alpha = (int)MathHelper.Lerp(255f, 0f, fadeProgress);
            NPC.velocity = new Vector2(0f, -0.45f * (1f - fadeProgress));

            if (stateTimer == 1)
            {
                if (target != null)
                {
                    headingAngle = target.Center.X >= NPC.Center.X ? 0f : MathF.PI;
                }

                SpawnArrivalShowerBurst();
            }

            if (!Main.dedServ && stateTimer <= ArrivalFadeTicks)
            {
                // Continuous stardust cascade + inward convergence while the dragon materializes.
                for (int i = 0; i < 3; i++)
                {
                    Vector2 inwardOffset = Main.rand.NextVector2CircularEdge(115f * (1f - fadeProgress * 0.45f), 70f);
                    int dustType = DustID.PurpleTorch;
                    Color tint = Color.MediumPurple;
                    if (i == 0)
                    {
                        dustType = DustID.GoldFlame;
                        tint = new Color(255, 220, 95);
                    }
                    else if (i == 2)
                    {
                        dustType = DustID.BlueTorch;
                        tint = Color.DeepSkyBlue;
                    }

                    Dust convergeDust = Dust.NewDustPerfect(
                        NPC.Center + inwardOffset,
                        dustType,
                        -inwardOffset * 0.065f,
                        25,
                        tint,
                        Main.rand.NextFloat(1.0f, 1.6f));
                    convergeDust.noGravity = true;
                }

                for (int i = 0; i < 3; i++)
                {
                    int showerType = DustID.ShadowbeamStaff;
                    if (i == 0)
                    {
                        showerType = DustID.GoldFlame;
                    }
                    else if (i == 1)
                    {
                        showerType = DustID.IceTorch;
                    }

                    Vector2 showerOffset = Main.rand.NextVector2Circular(115f, 45f);
                    Vector2 showerVelocity = new Vector2(Main.rand.NextFloat(-3.2f, 3.2f), Main.rand.NextFloat(-3.6f, 0.4f));
                    Dust showerDust = Dust.NewDustPerfect(
                        NPC.Center + showerOffset,
                        showerType,
                        showerVelocity,
                        20,
                        Color.White,
                        Main.rand.NextFloat(0.95f, 1.55f));
                    showerDust.noGravity = false;
                }
            }

            if (stateTimer >= ArrivalFadeTicks + ArrivalWaitTicks && Main.netMode != NetmodeID.MultiplayerClient)
            {
                flankSide = owner.direction >= 0 ? -1 : 1;
                TransitionState(DragonState.HoverGuard);
            }
        }

        private void RunHoverGuard(NPC owner, Player target)
        {
            NPC.alpha = 0;
            float bobY = MathF.Sin(Main.GlobalTimeWrappedHourly * 1.9f) * 24f;
            float guardSide = target != null && target.Center.X >= owner.Center.X ? -1f : 1f;
            Vector2 desiredPos = owner.Center + new Vector2(guardSide * 170f, -155f + bobY);

            SteerWithGentleCurve(desiredPos, HoverStrafeSpeed, DriftCruiseSpeed, 1f);

            if (Main.netMode != NetmodeID.MultiplayerClient && target != null)
            {
                if (attackCooldown <= 0 && NPC.Distance(target.Center) < 900f)
                {
                    PickNextAttack(target);
                }
                else if (stateTimer >= 220)
                {
                    flankSide = target.Center.X >= owner.Center.X ? 1 : -1;
                    TransitionState(DragonState.FlankDrift);
                }
            }
        }

        private void RunFlankDrift(NPC owner, Player target)
        {
            NPC.alpha = 0;
            if (target == null)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    TransitionState(DragonState.HoverGuard);
                }
                return;
            }

            float bobY = MathF.Sin(Main.GlobalTimeWrappedHourly * 1.6f) * 28f;
            Vector2 flankAnchor = target.Center + new Vector2(flankSide * 310f, -135f + bobY);
            SteerWithGentleCurve(flankAnchor, HoverStrafeSpeed, DriftCruiseSpeed, 1f);

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                if (attackCooldown <= 0 && NPC.Distance(target.Center) < 620f)
                {
                    PickNextAttack(target);
                }
                else if (stateTimer >= 260)
                {
                    TransitionState(DragonState.HoverGuard);
                }
            }
        }

        private void PickNextAttack(Player target)
        {
            float dist = NPC.Distance(target.Center);
            bool chooseComet = !lastAttackWasComet && (dist > 420f || Main.rand.NextBool(2));
            lastAttackWasComet = chooseComet;

            if (chooseComet)
            {
                cometTargetPos = target.Center + target.velocity * 18f;
                activeCometZoneIndex = -1;
                TransitionState(DragonState.CometCast);
                return;
            }

            // Close to inside breath cone range before starting the inhale tell.
            if (dist > BreathStartRangePixels)
            {
                flankSide = NPC.Center.X >= target.Center.X ? 1 : -1;
                TransitionState(DragonState.BreathApproach);
                return;
            }

            BeginBreathTell(target);
        }

        private void BeginBreathTell(Player target)
        {
            Vector2 toTarget = target.Center - NPC.Center;
            float centerAngle = toTarget.SafeNormalize(Vector2.UnitX).ToRotation();
            breathSweepDirection = Main.rand.NextBool() ? 1f : -1f;
            breathAngle = MathHelper.WrapAngle(centerAngle - breathSweepDirection * (BreathTotalSweepRadians * 0.42f));
            TransitionState(DragonState.BreathTell);
        }

        private void RunBreathApproach(Player target)
        {
            NPC.alpha = 0;
            breathOpacity = 0f;

            if (target == null)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    TransitionState(DragonState.HoverGuard);
                }
                return;
            }

            // Curve smoothly into a close flanking vantage well inside the 460px flame cone.
            Vector2 approachVantage = target.Center + new Vector2(flankSide * 195f, -95f);
            SteerWithGentleCurve(approachVantage, 3.2f, BreathApproachCruiseSpeed, 1.35f);

            if (!Main.dedServ)
            {
                Vector2 mouthPos = mouth.finalPosition != Vector2.Zero ? mouth.finalPosition : NPC.Center;
                for (int i = 0; i < 2; i++)
                {
                    int dustType = i == 0 ? DustID.GoldFlame : DustID.PurpleTorch;
                    Dust approachEmber = Dust.NewDustPerfect(
                        mouthPos + Main.rand.NextVector2Circular(18f, 18f),
                        dustType,
                        NPC.velocity * 0.25f + Main.rand.NextVector2Circular(1.2f, 1.2f),
                        30,
                        Color.White,
                        Main.rand.NextFloat(0.85f, 1.35f));
                    approachEmber.noGravity = true;
                }
            }

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                float distanceToTarget = NPC.Distance(target.Center);
                if (distanceToTarget <= BreathStartRangePixels)
                {
                    BeginBreathTell(target);
                }
                else if (stateTimer >= BreathApproachMaxTicks)
                {
                    if (distanceToTarget <= BreathRangePixels - 40f)
                    {
                        BeginBreathTell(target);
                    }
                    else
                    {
                        // Player kited far across the arena: pivot to Grand Comet instead of breathing thin air.
                        cometTargetPos = target.Center + target.velocity * 18f;
                        activeCometZoneIndex = -1;
                        lastAttackWasComet = true;
                        TransitionState(DragonState.CometCast);
                    }
                }
            }
        }

        private void RunBreathTell(Player target)
        {
            breathOpacity = 0f;

            // Keep drifting smoothly toward the player during the inhale so the dragon stays in range.
            if (target != null)
            {
                float sideSign = NPC.Center.X >= target.Center.X ? 1f : -1f;
                Vector2 tellAnchor = target.Center + new Vector2(sideSign * 210f, -85f);
                Vector2 toAnchor = tellAnchor - NPC.Center;
                float anchorDist = toAnchor.Length();
                if (anchorDist > 36f)
                {
                    float tellSpeed = MathHelper.Lerp(1.2f, 3.4f, MathHelper.Clamp((anchorDist - 36f) / 180f, 0f, 1f));
                    Vector2 desiredVel = toAnchor.SafeNormalize(Vector2.UnitX) * tellSpeed;
                    NPC.velocity = Vector2.Lerp(NPC.velocity, desiredVel, 0.07f);
                }
                else
                {
                    NPC.velocity *= 0.92f;
                }

                // Track the player during the first half of the tell, then lock the opening sightline for the final 24 ticks.
                if (stateTimer <= 26)
                {
                    Vector2 origin = mouth.finalPosition != Vector2.Zero ? mouth.finalPosition : NPC.Center;
                    float desiredCenter = (target.Center - origin).SafeNormalize(Vector2.UnitX).ToRotation();
                    float desiredStart = MathHelper.WrapAngle(desiredCenter - breathSweepDirection * (BreathTotalSweepRadians * 0.42f));
                    float angleDelta = MathHelper.Clamp(MathHelper.WrapAngle(desiredStart - breathAngle), -0.045f, 0.045f);
                    breathAngle = MathHelper.WrapAngle(breathAngle + angleDelta);
                }
            }
            else
            {
                NPC.velocity *= 0.90f;
            }

            // Smoothly curve heading toward the starting breath angle without snapping.
            SteerHeadingToward(breathAngle, MaxTurnRateRadPerTick * 1.25f);

            if (stateTimer == 1 && !Main.dedServ)
            {
                SoundEngine.PlaySound(
                    new SoundStyle("tsorcRevamp/Sounds/DarkSouls/breath1") with { Volume = 0.48f, Pitch = -0.15f },
                    NPC.Center);
            }

            // Ramp mouth open frames 0 -> 4 over the 50t tell.
            int openFrame = Math.Clamp((stateTimer * 4) / BreathTellTicks, 0, 4);
            head.frame = openFrame;

            if (!Main.dedServ)
            {
                Vector2 mouthPos = mouth.finalPosition != Vector2.Zero ? mouth.finalPosition : NPC.Center;
                for (int i = 0; i < 3; i++)
                {
                    Vector2 offset = Main.rand.NextVector2CircularEdge(54f, 54f);
                    int dustType = i == 0 ? DustID.GoldFlame : DustID.PurpleTorch;
                    Color tint = i == 0 ? new Color(255, 220, 90) : Color.MediumPurple;
                    Dust inhale = Dust.NewDustPerfect(
                        mouthPos + offset,
                        dustType,
                        -offset * 0.11f,
                        25,
                        tint,
                        Main.rand.NextFloat(0.95f, 1.5f));
                    inhale.noGravity = true;
                }
            }

            if (stateTimer >= BreathTellTicks && Main.netMode != NetmodeID.MultiplayerClient)
            {
                TransitionState(DragonState.BreathSweep);
            }
        }

        private void RunBreathSweep(Player target)
        {
            // Advance sweep arc while smoothly pursuing the player so the dragon looms forward during flame breath.
            breathAngle = MathHelper.WrapAngle(breathAngle + breathSweepDirection * (BreathTotalSweepRadians / BreathSweepTicks));

            if (target != null)
            {
                float horizontalSide = NPC.Center.X >= target.Center.X ? 1f : -1f;
                Vector2 chasePoint = target.Center + new Vector2(horizontalSide * 170f, -72f);
                Vector2 toChase = chasePoint - NPC.Center;
                float chaseDistance = toChase.Length();
                float targetDistance = NPC.Distance(target.Center);

                float speedFactor = MathHelper.Clamp((targetDistance - 150f) / 210f, 0f, 1f);
                float pursueSpeed = MathHelper.Lerp(BreathPursuitMinSpeed, BreathPursuitMaxSpeed, speedFactor);
                if (chaseDistance > 24f)
                {
                    Vector2 desiredVel = toChase.SafeNormalize(Vector2.UnitX) * pursueSpeed;
                    NPC.velocity = Vector2.Lerp(NPC.velocity, desiredVel, 0.07f);
                }
                else
                {
                    NPC.velocity *= 0.92f;
                }

                // Gentle aim bias keeps the sweeping beam threatening as the player repositions without snapping.
                Vector2 mouthOrigin = mouth.finalPosition != Vector2.Zero ? mouth.finalPosition : NPC.Center;
                float playerAngle = (target.Center - mouthOrigin).SafeNormalize(Vector2.UnitX).ToRotation();
                float trackingBias = MathHelper.Clamp(MathHelper.WrapAngle(playerAngle - breathAngle), -0.009f, 0.009f);
                breathAngle = MathHelper.WrapAngle(breathAngle + trackingBias);
            }
            else
            {
                NPC.velocity *= 0.90f;
            }

            SteerHeadingToward(breathAngle, MaxTurnRateRadPerTick * 1.25f);

            float fadeIn = MathHelper.Clamp(stateTimer / 10f, 0f, 1f);
            breathOpacity = fadeIn;

            // Cycle active breathing head frames 4..7.
            head.frame = 4 + ((stateTimer / 5) % 4);

            if (!Main.dedServ)
            {
                Vector2 mouthPos = mouth.finalPosition != Vector2.Zero ? mouth.finalPosition : NPC.Center;
                Vector2 breathDir = breathAngle.ToRotationVector2();
                for (int i = 0; i < 3; i++)
                {
                    Vector2 sparkVel = breathDir.RotatedByRandom(BreathHalfConeRadians * 0.85f) * Main.rand.NextFloat(7f, 14f);
                    int dustType = i == 0 ? DustID.GoldFlame : DustID.PurpleTorch;
                    Dust spark = Dust.NewDustPerfect(
                        mouthPos + breathDir * Main.rand.NextFloat(12f, 44f),
                        dustType,
                        sparkVel,
                        20,
                        Color.White,
                        Main.rand.NextFloat(0.9f, 1.45f));
                    spark.noGravity = true;
                }
            }

            // Damage check against every active player inside the live cosmic breath cone.
            if (Main.netMode != NetmodeID.MultiplayerClient && stateTimer >= 8 && breathHitCooldown <= 0)
            {
                Vector2 origin = mouth.finalPosition != Vector2.Zero ? mouth.finalPosition : NPC.Center;
                Vector2 breathDir = breathAngle.ToRotationVector2();
                int breathDamage = EnemyDamage.Projectile(230);

                for (int i = 0; i < Main.maxPlayers; i++)
                {
                    Player player = Main.player[i];
                    if (!player.active || player.dead || player.immune)
                    {
                        continue;
                    }

                    Vector2 toPlayer = player.Center - origin;
                    float dist = toPlayer.Length();
                    if (dist < 24f || dist > BreathRangePixels)
                    {
                        continue;
                    }

                    float dot = Vector2.Dot(toPlayer / dist, breathDir);
                    if (dot >= MathF.Cos(BreathHalfConeRadians))
                    {
                        int hitDir = player.Center.X >= origin.X ? 1 : -1;
                        player.Hurt(
                            Terraria.DataStructures.PlayerDeathReason.ByNPC(NPC.whoAmI),
                            breathDamage * 2,
                            hitDir);
                        player.AddBuff(BuffID.ShadowFlame, 240);
                        breathHitCooldown = 16;
                        break;
                    }
                }
            }

            if (stateTimer >= BreathSweepTicks && Main.netMode != NetmodeID.MultiplayerClient)
            {
                TransitionState(DragonState.BreathRecover);
            }
        }

        private void RunBreathRecover()
        {
            NPC.velocity *= 0.90f;
            float recoverProgress = MathHelper.Clamp(stateTimer / (float)BreathRecoverTicks, 0f, 1f);
            breathOpacity = MathHelper.Clamp(1f - recoverProgress * 2.2f, 0f, 1f);
            head.frame = Math.Clamp(3 - (int)(recoverProgress * 4f), 0, 3);

            if (stateTimer >= BreathRecoverTicks && Main.netMode != NetmodeID.MultiplayerClient)
            {
                attackCooldown = Main.rand.Next(AttackCooldownMinTicks, AttackCooldownMaxTicks);
                flankSide = -flankSide;
                TransitionState(DragonState.FlankDrift);
            }
        }

        private void RunCometCast(Player target)
        {
            NPC.velocity *= 0.88f;
            breathOpacity = 0f;

            if (stateTimer == 1)
            {
                if (!Main.dedServ)
                {
                    SoundEngine.PlaySound(
                        new SoundStyle(SoundBase + "_CometCast") with { Volume = 0.52f },
                        NPC.Center);
                }

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    activeCometZoneIndex = Projectile.NewProjectile(
                        NPC.GetSource_FromAI(),
                        cometTargetPos,
                        Vector2.Zero,
                        ModContent.ProjectileType<KhaiosGrandCometZone>(),
                        EnemyDamage.Projectile(250),
                        4f,
                        Main.myPlayer);
                }
            }

            if (stateTimer == 36 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Vector2 skyStart = cometTargetPos + new Vector2(Main.rand.NextFloat(-360f, 360f), -920f);
                Projectile.NewProjectile(
                    NPC.GetSource_FromAI(),
                    skyStart,
                    Vector2.Zero,
                    ModContent.ProjectileType<KhaiosGrandCometMeteor>(),
                    EnemyDamage.Projectile(280),
                    5f,
                    Main.myPlayer,
                    cometTargetPos.X,
                    cometTargetPos.Y,
                    activeCometZoneIndex);
            }

            if (stateTimer >= CometCastTicks && Main.netMode != NetmodeID.MultiplayerClient)
            {
                attackCooldown = Main.rand.Next(AttackCooldownMinTicks, AttackCooldownMaxTicks);
                TransitionState(DragonState.HoverGuard);
            }
        }

        /// <summary>
        /// Drives slow, majestic sentry flight using a continuous heading angle with capped angular
        /// turn rate so turning around always follows a smooth CW or CCW arc instead of snapping.
        /// </summary>
        private void SteerWithGentleCurve(Vector2 destination, float hoverSpeed, float cruiseSpeed, float turnMultiplier)
        {
            Vector2 toDest = destination - NPC.Center;
            float distance = toDest.Length();

            if (distance < 48f)
            {
                NPC.velocity *= 0.92f;
                return;
            }

            float desiredAngle = toDest.ToRotation();
            SteerHeadingToward(desiredAngle, MaxTurnRateRadPerTick * turnMultiplier);

            float speedFactor = MathHelper.Clamp((distance - 48f) / 220f, 0f, 1f);
            float targetSpeed = MathHelper.Lerp(hoverSpeed, cruiseSpeed, speedFactor);
            Vector2 desiredVelocity = headingAngle.ToRotationVector2() * targetSpeed;
            NPC.velocity = Vector2.Lerp(NPC.velocity, desiredVelocity, 0.08f);
        }

        private void SteerHeadingToward(float targetAngle, float maxTurnStep)
        {
            float delta = MathHelper.WrapAngle(targetAngle - headingAngle);

            // When reversing nearly 180 degrees, deliberately curve upward so the dragon arcs over
            // the arena instead of dipping awkwardly into the ground.
            if (MathF.Abs(delta) > MathHelper.Pi - 0.18f)
            {
                float upwardBias = MathF.Cos(headingAngle) >= 0f ? -1f : 1f;
                delta = upwardBias * maxTurnStep;
            }
            else
            {
                delta = MathHelper.Clamp(delta, -maxTurnStep, maxTurnStep);
            }

            headingAngle = MathHelper.WrapAngle(headingAngle + delta);
        }

        private void TransitionState(DragonState nextState)
        {
            state = nextState;
            stateTimer = 0;
            NPC.netUpdate = true;
        }

        private void EnsureBodyRig()
        {
            if (bodyInitialized)
            {
                return;
            }

            bodyInitialized = true;
            BodySegment.BaseScale = DragonScale;
            neckSegments = new BodySegment[4];

            frontBody = new BodySegment(8, 0, 0, 7);
            frontBody.Texture = ModContent.Request<Texture2D>(StarForgerTextureBase + "_Front_Body");
            frontBody.segmentOrigin = new Vector2(66f, 46f);

            backBody = new BodySegment(8, 0, 0, 7);
            backBody.Texture = ModContent.Request<Texture2D>(StarForgerTextureBase + "_Back_Body");
            backBody.segmentOrigin = new Vector2(233f, 41f);
            backBody.offset = new Vector2(-31f, 7f);
            frontBody.backSegments.Add(backBody);

            backRightLeg = new BodySegment(8, 0, 0, 7);
            backRightLeg.Texture = ModContent.Request<Texture2D>(StarForgerTextureBase + "_Back_R_Leg");
            backRightLeg.segmentOrigin = new Vector2(76f, 14f);
            backRightLeg.offset = new Vector2(-37f, 9f);
            backBody.AddSegment(backRightLeg, false);

            backLeftLeg = new BodySegment(8, 0, 0, 7);
            backLeftLeg.Texture = ModContent.Request<Texture2D>(StarForgerTextureBase + "_Back_L_Leg");
            backLeftLeg.segmentOrigin = new Vector2(64f, 16f);
            backLeftLeg.offset = new Vector2(1f, 14f);
            backBody.AddSegment(backLeftLeg, true);

            frontRightLeg = new BodySegment(16, 4, 0, 7, 8, 15);
            frontRightLeg.Texture = ModContent.Request<Texture2D>(StarForgerTextureBase + "_Front_R_Leg");
            frontRightLeg.segmentOrigin = new Vector2(80f, 102f);
            frontRightLeg.offset = new Vector2(18f, 12f);
            frontBody.AddSegment(frontRightLeg, false);

            frontLeftLeg = new BodySegment(16, 0, 0, 7, 8, 15);
            frontLeftLeg.Texture = ModContent.Request<Texture2D>(StarForgerTextureBase + "_Front_L_Leg");
            frontLeftLeg.segmentOrigin = new Vector2(78f, 94f);
            frontLeftLeg.offset = new Vector2(54f, 4f);
            frontBody.AddSegment(frontLeftLeg, true);

            Vector2 neckOrigin = new Vector2(16f, 26f);
            BodySegment lastNeck = null;
            for (int i = 0; i < neckSegments.Length; i++)
            {
                BodySegment segment = new BodySegment(1, 0, 0, 0);
                segment.Texture = ModContent.Request<Texture2D>(StarForgerTextureBase + "_Neck_Segment");
                segment.segmentOrigin = neckOrigin;
                segment.offset = i == 0 ? new Vector2(52f, -2f) : new Vector2(20f, 0f);
                neckSegments[i] = segment;

                if (i == 0)
                {
                    frontBody.AddSegment(segment, true);
                }
                else
                {
                    lastNeck.AddSegment(segment, false);
                }

                lastNeck = segment;
            }

            head = new BodySegment(8, 0, -1, -1);
            head.Texture = ModContent.Request<Texture2D>(StarForgerTextureBase + "_Dragon_Head");
            head.segmentOrigin = new Vector2(70f, 76f);
            head.offset = new Vector2(17f, 4f);
            lastNeck.AddSegment(head, false);

            mouth = new BodySegment(0, 0, -1, -1);
            mouth.offset = new Vector2(0f, 26f);
            mouth.rotation = 0f;
            head.AddSegment(mouth, false);
        }

        private void UpdateArticulatedRig(Player target)
        {
            if (Main.dedServ)
            {
                mouth.finalPosition = NPC.Center + headingAngle.ToRotationVector2() * 68f;
                return;
            }

            bool facingLeft = MathF.Cos(headingAngle) < 0f;
            SpriteEffects effect = facingLeft ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            float bodyPitch = MathHelper.WrapAngle(headingAngle - (facingLeft ? MathF.PI : 0f));
            bodyPitch = MathHelper.Clamp(bodyPitch, -0.65f, 0.65f);

            float neckTarget = 0f;
            if (state == DragonState.BreathTell || state == DragonState.BreathSweep)
            {
                float aimPitch = MathHelper.WrapAngle(breathAngle - (facingLeft ? MathF.PI : 0f));
                neckTarget = MathHelper.Clamp(aimPitch - bodyPitch, -0.8f, 0.8f);
            }
            else if (target != null)
            {
                float lookAngle = (target.Center - NPC.Center).ToRotation();
                float lookPitch = MathHelper.WrapAngle(lookAngle - (facingLeft ? MathF.PI : 0f));
                neckTarget = MathHelper.Clamp((lookPitch - bodyPitch) * 0.45f, -0.45f, 0.45f);
            }

            float perNeckSegment = neckTarget / neckSegments.Length;
            for (int i = 0; i < neckSegments.Length; i++)
            {
                neckSegments[i].rotation = perNeckSegment;
            }

            head.rotation = perNeckSegment * 0.6f;
            bool playingAltCometPose = state == DragonState.CometCast;
            frontBody.Update(1, NPC.Center, bodyPitch, effect, facingLeft, playingAltCometPose);
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            EnsureBodyRig();

            float alphaScale = MathHelper.Clamp((255f - NPC.alpha) / 255f, 0f, 1f);
            Color bodyColor = Color.Lerp(drawColor, Color.White, 0.35f) * alphaScale;
            frontBody.Draw(bodyColor);

            if (breathOpacity > 0.02f)
            {
                DrawPixelatedCosmicBreath();
            }

            return false;
        }

        private void DrawPixelatedCosmicBreath()
        {
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(
                SpriteSortMode.Immediate,
                BlendState.Additive,
                SamplerState.LinearWrap,
                DepthStencilState.None,
                RasterizerState.CullNone,
                null,
                Main.GameViewMatrix.TransformationMatrix);

            breathShader ??= ModContent.Request<Effect>(
                "tsorcRevamp/Effects/DragonBreathPixelated",
                AssetRequestMode.ImmediateLoad).Value;

            float splitAngle = MathHelper.TwoPi / 10f;
            float shaderRotation = MathF.PI * 0.9f;
            Texture2D noiseSmooth = tsorcRevamp.NoiseSmooth;
            Texture2D noiseWavy = tsorcRevamp.NoiseWavy;

            breathShader.Parameters["Color"].SetValue(new Vector4(0.2f, 0.05f, 0.35f, 1f));
            breathShader.Parameters["Color2"].SetValue(new Vector4(1f, 0.75f, 0f, 1f));
            breathShader.Parameters["splitAngle"].SetValue(splitAngle);
            breathShader.Parameters["rotation"].SetValue(shaderRotation);
            breathShader.Parameters["time"].SetValue((float)Main.timeForVisualEffects / 252f);
            breathShader.Parameters["length"].SetValue(0.01f);
            breathShader.Parameters["opacity"].SetValue(2.5f * breathOpacity);
            breathShader.Parameters["texScale"].SetValue(noiseSmooth.Size() / 500f);
            breathShader.Parameters["texScale3"].SetValue(noiseWavy.Size() / 1000f);
            breathShader.Parameters["noiseTexture2"].SetValue(noiseWavy);
            breathShader.Parameters["rotationMinusPI"].SetValue(shaderRotation - MathHelper.Pi);
            breathShader.Parameters["splitAnglePlusRotationMinusPI"].SetValue(shaderRotation + splitAngle - MathHelper.Pi);
            breathShader.Parameters["RotationMinus2PIMinusSplitAngleMinusPI"].SetValue((shaderRotation - (MathHelper.TwoPi - splitAngle)) - MathHelper.Pi);

            float drawScale = 1.2f * 1.2f * 7.5f;
            const float breathPixelBlockSize = 4f;
            Vector2 pixelBlocks = Vector2.Max(noiseSmooth.Size() * drawScale / breathPixelBlockSize, Vector2.One);
            breathShader.Parameters["PixelGrid"]?.SetValue(
                new Vector4(pixelBlocks.X, pixelBlocks.Y, 1f / pixelBlocks.X, 1f / pixelBlocks.Y));

            breathShader.CurrentTechnique.Passes[0].Apply();

            Rectangle sourceRect = new Rectangle(0, 0, noiseSmooth.Width, noiseSmooth.Height);
            Vector2 origin = sourceRect.Size() * 0.5f;
            Vector2 mouthPos = mouth.finalPosition != Vector2.Zero ? mouth.finalPosition : NPC.Center;

            Main.spriteBatch.Draw(
                noiseSmooth,
                mouthPos - Main.screenPosition,
                sourceRect,
                Color.White,
                breathAngle,
                origin,
                drawScale,
                SpriteEffects.None,
                0f);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(
                SpriteSortMode.Deferred,
                BlendState.AlphaBlend,
                SamplerState.PointClamp,
                DepthStencilState.None,
                RasterizerState.CullNone,
                null,
                Main.GameViewMatrix.TransformationMatrix);
        }

        private void UpdateBreathLoopAudio(bool breathingActive)
        {
            if (Main.dedServ)
            {
                return;
            }

            if (breathingActive)
            {
                breathAudioGain = Math.Min(1f, breathAudioGain + (1f / BreathAudioFadeInTicks));
                if (!SoundEngine.TryGetActiveSound(breathLoopSlot, out ActiveSound activeSound))
                {
                    SoundStyle breathLoop = new SoundStyle(SoundBase + "_BreathLoop")
                    {
                        Volume = 0.24f,
                        IsLooped = true,
                        MaxInstances = 1,
                    };
                    breathLoopSlot = SoundEngine.PlaySound(breathLoop, NPC.Center);
                    SoundEngine.TryGetActiveSound(breathLoopSlot, out activeSound);
                }

                if (activeSound != null)
                {
                    activeSound.Position = NPC.Center;
                    activeSound.Volume = breathAudioGain;
                }

                return;
            }

            // Smooth quick fadeout over 16 ticks so the breath audio never cuts off abruptly.
            if (SoundEngine.TryGetActiveSound(breathLoopSlot, out ActiveSound fadingSound))
            {
                breathAudioGain = Math.Max(0f, breathAudioGain - (1f / BreathAudioFadeOutTicks));
                fadingSound.Position = NPC.Center;
                fadingSound.Volume = breathAudioGain;

                if (breathAudioGain <= 0.01f)
                {
                    fadingSound.Stop();
                    breathAudioGain = 0f;
                }
            }
            else
            {
                breathAudioGain = 0f;
            }
        }

        private void StopBreathLoop()
        {
            breathAudioGain = 0f;
            if (SoundEngine.TryGetActiveSound(breathLoopSlot, out ActiveSound activeSound))
            {
                activeSound.Stop();
            }
        }

        private void SpawnArrivalShowerBurst()
        {
            if (Main.dedServ)
            {
                return;
            }

            UsefulFunctions.ScreenShake(NPC.Center, 4.5f, 16, distanceFalloff: 850f);

            // Large celestial star-shower burst fanning upward and raining down across the arena.
            for (int i = 0; i < 95; i++)
            {
                Vector2 spawnOffset = Main.rand.NextVector2Circular(115f, 55f);
                Vector2 showerVel = new Vector2(
                    Main.rand.NextFloat(-9.5f, 9.5f),
                    Main.rand.NextFloat(-9.0f, -1.2f));
                int dustType = DustID.BlueTorch;
                Color dustColor = Color.DeepSkyBlue;

                if (i % 4 == 0)
                {
                    dustType = DustID.GoldFlame;
                    dustColor = new Color(255, 220, 90);
                }
                else if (i % 4 == 1)
                {
                    dustType = DustID.ShadowbeamStaff;
                    dustColor = Color.MediumPurple;
                }
                else if (i % 4 == 2)
                {
                    dustType = DustID.IceTorch;
                    dustColor = Color.Cyan;
                }

                Dust shower = Dust.NewDustPerfect(
                    NPC.Center + spawnOffset,
                    dustType,
                    showerVel,
                    20,
                    dustColor,
                    Main.rand.NextFloat(1.0f, 1.85f));
                shower.noGravity = i % 3 == 0;
            }

            // Crisp expanding golden-white starlight ring framing the dragon's silhouette.
            for (int i = 0; i < 48; i++)
            {
                float ringAngle = MathHelper.TwoPi * i / 48f;
                Vector2 ringDir = ringAngle.ToRotationVector2();
                int glintType = i % 2 == 0 ? DustID.GoldFlame : DustID.AncientLight;
                Dust ringSpark = Dust.NewDustPerfect(
                    NPC.Center + new Vector2(ringDir.X * 62f, ringDir.Y * 34f),
                    glintType,
                    new Vector2(ringDir.X * 7.8f, ringDir.Y * 5.2f),
                    10,
                    Color.White,
                    Main.rand.NextFloat(0.95f, 1.55f));
                ringSpark.noGravity = true;
            }
        }

        private void SpawnDeathStardustExplosion()
        {
            if (deathExploded || Main.dedServ)
            {
                return;
            }

            deathExploded = true;
            SoundEngine.PlaySound(
                new SoundStyle("tsorcRevamp/Sounds/HollowKnight/flamebearer_death_and_absorb") with { Volume = 0.65f, Pitch = -0.10f },
                NPC.Center);
            SoundEngine.PlaySound(
                new SoundStyle("tsorcRevamp/Sounds/DarkSouls/explosion-wind-tail") with { Volume = 0.52f, Pitch = 0.08f },
                NPC.Center);
            UsefulFunctions.ScreenShake(NPC.Center, 7f, 22, distanceFalloff: 900f);

            // Layered cosmic + golden stardust explosion across the dragon's full body span.
            for (int i = 0; i < 160; i++)
            {
                Vector2 offset = Main.rand.NextVector2Circular(110f, 55f);
                Vector2 burstVel = Main.rand.NextVector2Circular(9.5f, 9.5f);
                int dustType = DustID.ShadowbeamStaff;
                Color dustColor = Color.MediumPurple;

                if (i % 3 == 0)
                {
                    dustType = DustID.GoldFlame;
                    dustColor = new Color(255, 215, 70);
                }
                else if (i % 5 == 0)
                {
                    dustType = DustID.AncientLight;
                    dustColor = Color.White;
                }
                else if (i % 4 == 0)
                {
                    dustType = DustID.BlueTorch;
                    dustColor = Color.DeepSkyBlue;
                }

                Dust spark = Dust.NewDustPerfect(
                    NPC.Center + offset,
                    dustType,
                    burstVel,
                    20,
                    dustColor,
                    Main.rand.NextFloat(1.0f, 1.95f));
                spark.noGravity = i % 4 != 0;
            }
        }
    }
}

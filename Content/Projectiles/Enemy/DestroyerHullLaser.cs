using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.NPCs.Bosses.TheDestroyer;

namespace tsorcRevamp.Content.Projectiles.Enemy
{
    internal sealed class DestroyerHullLaser : ModProjectile
    {
        internal const int FireTicks = 30;
        internal const int CurtainRunFireTicks = FireTicks * 3;
        internal const int FadeTicks = 12;
        internal const int RamPort = 0;
        internal const int OverheadPort = 2;
        internal const int EnclosingPort = 4;
        internal const int ExtendedEnclosingPort = 6;
        internal const int BroadsidePort = 8;
        internal const int DriveThroughPort = 14;
        internal const int OriginalPassPort = 16;
        internal const int CurtainRunPort = 18;
        internal const int HourglassPort = 20;
        internal const int ReactorPort = 22;
        internal const int RipplePort = 24;
        internal const int ShortPairPort = 26;
        internal const int CurtainShortPairPort = 28;
        internal const int RippleFireTicks = 8;
        internal const int RippleFadeTicks = 6;
        internal const int ShortPairFireTicks = 180;
        internal const int ShortPairTellTicks = 45;
        internal const int PoseWaitTicks = 18;
        internal const float OverchargeFlag = 0.5f;
        internal const int ProbeLiveTicks = 16;
        private const int EnclosingTurnCheckLength = 1470;
        private const int EnclosingLength = EnclosingTurnCheckLength * 2;
        internal const int PassLength = 2000;
        internal const int RamLength = 840;
        internal const int ProbeLength = 60 * 16;
        private const int OverheadLength = 1120;
        private const int HourglassLength = 1380;
        private const int ReactorLength = 1160;
        private const int ReactorHitWidth = 64;
        internal const int RippleLength = 800;
        private const int RippleHitWidth = 44;
        private const int ShortPairLength = 4 * 16;
        private const int ShortPairHitWidth = 14;
        private const int HitWidth = 22;
        private const float PixelBlockSize = 3f;
        private const float MaxSweepSpeed = 36f;
        private static ReLogic.Content.Asset<Effect> laserEffect;
        private static ReLogic.Content.Asset<Effect> legacyBeamEffect;
        private static ReLogic.Content.Asset<Texture2D> turbulentNoise;
        private Vector2 initialDirection;
        private float initialHostRotation;
        private Vector2 previousHullMuzzle;
        private Vector2 previousHullDirection;
        private Vector2 oppositeMuzzle;
        private int poseWait;
        private NPC[] volleyEmitters;
        private int age;
        private bool initialized;
        private bool played;
        private bool armed;
        private bool fizzled;
        private bool turnSuppressed;

        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(EnemyLingeringLaser));

        private int TellTicks
        {
            get
            {
                if (IsRamBeam)
                {
                    // Ram timing carries the planned live duration. Its fixed tell shares the head's windup.
                    return TheDestroyer.RamBeamTellTicks + 1;
                }

                if (IsCurtainShortPair)
                {
                    return ShortPairTellTicks;
                }

                return (int)Projectile.ai[0] + PassDelay;
            }
        }
        private int PassDelay
        {
            get
            {
                if (Projectile.ai[2] < BroadsidePort || Projectile.ai[2] >= DriveThroughPort)
                {
                    return 0;
                }

                int portPair = ((int)Projectile.ai[2] - BroadsidePort) / 2;
                return portPair * TheDestroyer.RippleDelay;
            }
        }
        private int ShutterLead
        {
            get
            {
                if (IsOriginalPass || IsHourglass || IsReactor || IsRipple || IsShortPair)
                {
                    return 0;
                }

                if (Projectile.ai[2] >= DriveThroughPort)
                {
                    return 8;
                }

                return Projectile.ai[2] >= BroadsidePort ? TheDestroyer.PortTell : 0;
            }
        }
        private int PortHeatLead => IsOriginalPass ? TheDestroyer.PortTell : 0;
        private int HostIndex => (int)Projectile.ai[1];
        private bool IsProbe => Projectile.ai[2] < 0f;
        private bool IsRamBeam => Projectile.ai[2] >= RamPort && Projectile.ai[2] < OverheadPort;
        private bool IsHourglass => Projectile.ai[2] >= HourglassPort && Projectile.ai[2] < ReactorPort;
        private bool IsReactor => Projectile.ai[2] >= ReactorPort && Projectile.ai[2] < RipplePort;
        private bool IsRipple => Projectile.ai[2] >= RipplePort && Projectile.ai[2] < ShortPairPort;
        private bool IsShortPair => Projectile.ai[2] >= ShortPairPort;
        private bool IsCurtainShortPair => Projectile.ai[2] >= CurtainShortPairPort;
        private int FadeDuration => IsRipple ? RippleFadeTicks : FadeTicks;
        private bool IsOrbitBeam => IsReactor || IsHourglass || Projectile.ai[2] >= OverheadPort && Projectile.ai[2] < BroadsidePort;
        private bool IsEnclosing => Projectile.ai[2] >= EnclosingPort && Projectile.ai[2] < BroadsidePort;
        private bool IsOvercharged => IsEnclosing && Projectile.ai[2] >= ExtendedEnclosingPort
            && Projectile.ai[2] - (int)Projectile.ai[2] == OverchargeFlag;
        private bool IsOriginalPass => Projectile.ai[2] >= OriginalPassPort && Projectile.ai[2] < CurtainRunPort;
        private int PortSide => (int)Projectile.ai[2] % 2 == 0 ? 1 : -1;

        private int EndTick
        {
            get
            {
                if (IsProbe)
                {
                    return TellTicks + ProbeLiveTicks;
                }

                if (IsEnclosing)
                {
                    int liveTicks = Projectile.ai[2] >= ExtendedEnclosingPort
                        ? TheDestroyer.EnclosingSecondFireTicks
                        : TheDestroyer.EnclosingFirstFireTicks;
                    return TellTicks + liveTicks;
                }

                if (IsHourglass)
                {
                    return TellTicks + TheDestroyer.HourglassFireTicks;
                }

                if (IsReactor)
                {
                    return TellTicks + TheDestroyer.ReactorFireTicks;
                }

                if (IsOrbitBeam)
                {
                    return TellTicks + TheDestroyer.OrbitFireTicks;
                }

                if (IsRamBeam)
                {
                    return TellTicks + (int)Projectile.ai[0];
                }

                if (IsRipple)
                {
                    return TellTicks + RippleFireTicks;
                }

                if (IsCurtainShortPair)
                {
                    // The curtain holds these beams on through its return pass. AI sets the fade tick when the move ends.
                    int fadeStart = (int)Projectile.ai[0];
                    return fadeStart > 0 ? fadeStart : short.MaxValue - FadeTicks;
                }

                if (IsShortPair)
                {
                    return TellTicks + ShortPairFireTicks;
                }

                if (Projectile.ai[2] >= CurtainRunPort)
                {
                    return TellTicks + CurtainRunFireTicks;
                }

                return TellTicks + FireTicks;
            }
        }

        private float BeamLength
        {
            get
            {
                if (IsProbe)
                {
                    return ProbeLength;
                }

                if (IsHourglass)
                {
                    return HourglassLength;
                }

                if (IsReactor)
                {
                    return ReactorLength;
                }

                if (IsOrbitBeam)
                {
                    return IsEnclosing ? EnclosingLength : OverheadLength;
                }

                if (IsRamBeam)
                {
                    return RamLength;
                }

                if (IsRipple)
                {
                    return RippleLength;
                }

                if (IsShortPair)
                {
                    return ShortPairLength;
                }

                return PassLength;
            }
        }

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = Math.Max(ProbeLength, EnclosingLength);
        }

        public override void Unload()
        {
            laserEffect = null;
            legacyBeamEffect = null;
            turbulentNoise = null;
        }

        public override void SetDefaults()
        {
            Projectile.width = 2;
            Projectile.height = 2;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 64 + TheDestroyer.ReactorFireTicks + PoseWaitTicks + FadeTicks + 2;
            Projectile.netImportant = true;
            Projectile.hide = true;
        }

        public override bool ShouldUpdatePosition() => false;

        public override bool? CanDamage() => initialized && !turnSuppressed && (IsProbe || armed) && age >= TellTicks && age < EndTick;

        internal void JoinVolley(NPC[] emitters)
        {
            // The server evaluates these emitters together, then sends each beam's arm or fade decision.
            volleyEmitters = emitters;
        }

        internal static bool ExceedsSweepSpeed(Vector2 previousOrigin, Vector2 previousDirection,
            Vector2 origin, Vector2 direction, float length, bool checkAngle = true)
        {
            if (checkAngle && Vector2.Dot(previousDirection, direction) < 0.9985f)
            {
                return true;
            }

            // Project both ends across the beam's average axis. A small turn can still whip a long beam sideways.
            Vector2 axis = (previousDirection + direction).SafeNormalize(direction);
            Vector2 normal = new Vector2(-axis.Y, axis.X);
            Vector2 originTravel = origin - previousOrigin;
            Vector2 tipTravel = originTravel + (direction - previousDirection) * length;
            float muzzleSweep = Math.Abs(Vector2.Dot(originTravel, normal));
            float tipSweep = Math.Abs(Vector2.Dot(tipTravel, normal));
            return Math.Max(muzzleSweep, tipSweep) > MaxSweepSpeed;
        }

        public override void AI()
        {
            NPC host = null;
            if (HostIndex >= 0 && HostIndex < Main.maxNPCs)
            {
                NPC candidate = Main.npc[HostIndex];
                bool validType = false;
                if (IsProbe)
                {
                    validType = candidate.type == NPCID.Probe && candidate.life * 4 >= candidate.lifeMax;
                }
                else
                {
                    validType = candidate.type == NPCID.TheDestroyerBody
                        || candidate.type == NPCID.TheDestroyer
                        || candidate.type == NPCID.TheDestroyerTail;
                }

                if (candidate.active && validType)
                {
                    host = candidate;
                }
            }

            if (host == null)
            {
                Projectile.Kill();
                return;
            }

            int ramLiveTime = 0;
            if (IsRamBeam || IsRipple || IsShortPair)
            {
                if (!TheDestroyer.TryGetHead(host, out NPC head) || head.GetGlobalNPC<TheDestroyer>().IsRetreating)
                {
                    Projectile.Kill();
                    return;
                }

                TheDestroyer headState = head.GetGlobalNPC<TheDestroyer>();
                if (IsRamBeam)
                {
                    ramLiveTime = headState.RamBeamLiveTime;
                    if (!headState.IsRamLiningUp)
                    {
                        if (!armed)
                        {
                            Projectile.Kill();
                            return;
                        }

                        age = Math.Max(age, EndTick);
                    }
                }
                else if (IsCurtainShortPair && Projectile.ai[0] == 0f)
                {
                    // Keep the projectile alive while the attack owns it, with enough time left for the final fade.
                    Projectile.timeLeft = Math.Max(Projectile.timeLeft, FadeTicks + 2);
                    if (!headState.IsCurtainCallTwo && Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        if (!armed)
                        {
                            Projectile.Kill();
                            return;
                        }

                        Projectile.ai[0] = age;
                        Projectile.netUpdate = true;
                    }
                }
            }

            if (!initialized)
            {
                initialized = true;
                initialDirection = Projectile.velocity.SafeNormalize(Vector2.UnitY);
                if (IsOriginalPass)
                {
                    initialHostRotation = host.rotation;
                }
            }

            if (IsProbe)
            {
                Projectile.Center = host.Center;
                host.velocity *= 0.86f;
                if (age < TellTicks - 24 && host.HasValidTarget)
                {
                    Player targetPlayer = Main.player[host.target];
                    Vector2 toTarget = (targetPlayer.Center - host.Center).SafeNormalize(initialDirection);
                    float currentAngle = initialDirection.ToRotation();
                    float targetAngle = toTarget.ToRotation();
                    initialDirection = currentAngle.AngleTowards(targetAngle, 0.03f).ToRotationVector2();
                }

                Projectile.velocity = initialDirection;
            }
            else
            {
                Vector2 portDirection = host.rotation.ToRotationVector2() * PortSide;
                Vector2 muzzle = host.Center + portDirection * (TheDestroyer.HullPortOffset * host.scale);
                Vector2 beamDirection = portDirection;
                if (IsOriginalPass)
                {
                    // Restore the first pass's shallow tracking while keeping the muzzle and aim on the same side.
                    float turn = MathHelper.WrapAngle(host.rotation - initialHostRotation) * 0.22f;
                    float aim = initialDirection.ToRotation() + turn;
                    float portAngle = portDirection.ToRotation();
                    float safeAim = portAngle.AngleTowards(aim, MathHelper.Pi / 3f);
                    beamDirection = safeAim.ToRotationVector2();
                }
                else if (IsHourglass)
                {
                    float cant = (Projectile.ai[2] - (int)Projectile.ai[2] - 0.5f) * MathHelper.TwoPi;
                    beamDirection = portDirection.RotatedBy(cant);
                }

                if (previousHullDirection == Vector2.Zero)
                {
                    previousHullMuzzle = Projectile.Center;
                    previousHullDirection = Projectile.velocity.SafeNormalize(beamDirection);
                }

                // Check the coil's original combat reach so its steady rotation can carry the longer beam beyond the far hull.
                float sweepCheckLength = IsEnclosing ? EnclosingTurnCheckLength : BeamLength;
                turnSuppressed = ExceedsSweepSpeed(previousHullMuzzle, previousHullDirection, muzzle, beamDirection, sweepCheckLength, !IsShortPair);
                bool checkBothSides = IsRamBeam || IsShortPair
                    || Projectile.ai[2] >= BroadsidePort && !IsOriginalPass && !IsOrbitBeam && !IsRipple;
                if (checkBothSides)
                {
                    float portOffset = TheDestroyer.HullPortOffset * host.scale;
                    Vector2 previousOpposite = previousHullMuzzle - previousHullDirection * (2f * portOffset);
                    oppositeMuzzle = host.Center - portDirection * portOffset;
                    turnSuppressed |= ExceedsSweepSpeed(previousOpposite, -previousHullDirection, oppositeMuzzle, -portDirection, BeamLength, !IsShortPair);
                }

                if (turnSuppressed && !IsCurtainShortPair && Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Projectile.Kill();
                    return;
                }

                previousHullMuzzle = muzzle;
                previousHullDirection = beamDirection;
                Projectile.Center = muzzle;
                Projectile.velocity = beamDirection;
            }
            if (IsProbe || armed || fizzled || age < TellTicks - 1)
            {
                age++;
            }
            else if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                // An enclosing volley shares the movement gate so the ports come on together; ripple/pass volleys fire on their exact staggered tick.
                // The reactor opens out to the side, far from the player, before sweeping across them.
                // Its pointwise sweep-speed guard still runs throughout the warning and live arc.
                bool poseReady = !IsOrbitBeam || IsReactor || TheDestroyer.CanFireHullPort(host);
                if (IsRamBeam)
                {
                    poseReady = ramLiveTime > 0;
                }

                if (poseReady && !IsReactor && volleyEmitters != null)
                {
                    foreach (NPC emitter in volleyEmitters)
                    {
                        if (emitter == host)
                        {
                            continue;
                        }

                        if (!emitter.active || emitter.type != NPCID.TheDestroyerBody || emitter.realLife != host.realLife
                            || !TheDestroyer.CanFireHullPort(emitter))
                        {
                            poseReady = false;
                            break;
                        }
                    }
                }

                if (poseReady)
                {
                    if (IsRamBeam)
                    {
                        Projectile.ai[0] = ramLiveTime;
                    }

                    armed = true;
                    age++;
                    Projectile.netUpdate = true;
                }
                else if (!IsRamBeam && ++poseWait >= PoseWaitTicks)
                {
                    fizzled = true;
                    age = EndTick;
                    Projectile.netUpdate = true;
                }
            }

            if (age >= EndTick + FadeDuration)
            {
                Projectile.Kill();
                return;
            }

            bool bodyEmitter = !IsProbe && host.type == NPCID.TheDestroyerBody;
            bool emitChargeDust = true;
            if (bodyEmitter && !turnSuppressed)
            {
                TheDestroyer segment = host.GetGlobalNPC<TheDestroyer>();
                int heatOffset = PortHeatLead - PassDelay;
                TheDestroyer.HullHeat heat = segment.UpdateBeamHeat(age + heatOffset, TellTicks + heatOffset, EndTick - TellTicks);
                emitChargeDust = heat >= TheDestroyer.HullHeat.Warm;
            }

            if (!Main.dedServ && !turnSuppressed)
            {
                if (bodyEmitter && emitChargeDust && age < EndTick)
                {
                    // Keep the flame at the middle of the hull. Charge and release share the same port.
                    int warmStart = PassDelay + TheDestroyer.HullArmingTicks - PortHeatLead;
                    int warmDuration = Math.Max(1, TellTicks - warmStart);
                    float charge = MathHelper.Clamp((age - warmStart) / (float)warmDuration, 0f, 1f);
                    bool firing = age >= TellTicks;
                    int interval = (int)MathHelper.Lerp(5f, 2f, charge);
                    if (age == warmStart || age == TellTicks || Main.GameUpdateCount % interval == 0)
                    {
                        float scatter = 1.6f * host.scale;
                        Vector2 flameOrigin = host.Center + Main.rand.NextVector2Circular(scatter, scatter);
                        float flameSpeed = MathHelper.Lerp(0.45f, 1.65f, charge);
                        Vector2 flameVelocity = Projectile.velocity * flameSpeed;
                        flameVelocity += Main.rand.NextVector2Circular(0.45f, 0.45f);
                        float scale = MathHelper.Lerp(0.75f, 1.15f, charge);
                        int dustType = firing ? DustID.Torch : DustID.OrangeTorch;
                        Dust flame = Dust.NewDustPerfect(flameOrigin, dustType, flameVelocity, Scale: scale);
                        flame.noGravity = true;
                    }
                }

                if (age < TellTicks)
                {
                    if (!bodyEmitter && age % 5 == 0)
                    {
                        Vector2 chargeOffset = Main.rand.NextVector2CircularEdge(18f, 18f);
                        Vector2 chargeVelocity = -chargeOffset * 0.12f + host.velocity;
                        Dust chargeDust = Dust.NewDustPerfect(Projectile.Center + chargeOffset, DustID.OrangeTorch, chargeVelocity, Scale: 1.25f);
                        chargeDust.noGravity = true;
                    }
                }
                else if (age < EndTick)
                {
                    if (!played)
                    {
                        played = true;
                        bool playLaserSound = Projectile.identity % 4 == 0 && !IsShortPair;
                        if (IsRamBeam)
                        {
                            playLaserSound = PortSide > 0;
                        }

                        if (IsReactor)
                        {
                            SoundEngine.PlaySound(SoundID.Item122 with { Volume = 0.85f, Pitch = -0.25f }, Projectile.Center);
                            UsefulFunctions.ScreenShake(Projectile.Center, 8f, 18, distanceFalloff: 1350f);
                        }
                        else if (IsProbe || IsOrbitBeam || playLaserSound)
                        {
                            SoundEngine.PlaySound(SoundID.Item12 with { Volume = 0.55f }, Projectile.Center);
                        }
                    }

                    if (age % 5 == 0 && !IsShortPair)
                    {
                        Vector2 dustPosition = Projectile.Center + Projectile.velocity * Main.rand.NextFloat(24f, BeamLength * 0.85f);
                        Dust beamDust = Dust.NewDustPerfect(dustPosition, DustID.OrangeTorch, Projectile.velocity * 3.5f, Scale: 1.55f);
                        beamDust.noGravity = true;
                    }
                }
            }
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(initialized);
            if (initialized)
            {
                writer.Write((short)age);
                writer.WriteVector2(initialDirection);
                if (IsOriginalPass)
                {
                    writer.Write(initialHostRotation);
                }

                writer.Write(armed);
                writer.Write(fizzled);
            }
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            if (reader.ReadBoolean())
            {
                initialized = true;
                age = reader.ReadInt16();
                initialDirection = reader.ReadVector2();
                if (IsOriginalPass)
                {
                    initialHostRotation = reader.ReadSingle();
                }

                armed = reader.ReadBoolean();
                fizzled = reader.ReadBoolean();
            }
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float point = 0f;
            Vector2 start = Projectile.Center;
            Vector2 end = start + Projectile.velocity * BeamLength;
            if (IsShortPair)
            {
                Vector2 oppositeEnd = oppositeMuzzle - Projectile.velocity * ShortPairLength;
                if (Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), start, end, ShortPairHitWidth, ref point))
                {
                    return true;
                }

                point = 0f;
                return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), oppositeMuzzle, oppositeEnd, ShortPairHitWidth, ref point);
            }

            float hitWidth = IsReactor ? ReactorHitWidth : HitWidth;
            if (IsRipple)
            {
                hitWidth = RippleHitWidth;
            }

            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), start, end, hitWidth, ref point);
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            if (IsProbe)
            {
                target.AddBuff(ModContent.BuffType<Buffs.ThermalRise>(), 320);
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (!initialized || turnSuppressed || age < PassDelay + ShutterLead)
            {
                return false;
            }

            if (IsShortPair)
            {
                Vector2 onScreen = Projectile.Center - Main.screenPosition;
                const float margin = 280f;
                if (onScreen.X < -margin || onScreen.X > Main.screenWidth + margin
                    || onScreen.Y < -margin || onScreen.Y > Main.screenHeight + margin)
                {
                    return false;
                }
            }

            bool firing = !fizzled && age >= TellTicks;
            if (firing && IsReactor)
            {
                float fade = MathHelper.Clamp((EndTick + FadeTicks - age) / (float)FadeTicks, 0f, 1f);
                DrawLegacyBeam(Projectile.Center, Projectile.velocity, ReactorLength, age, fade);
                return false;
            }

            bool fiery = IsOvercharged || IsHourglass;
            float drawWidth = 110f;
            float opacity = 0.35f;
            float scroll = 0f;
            float coreIntensity = 0f;

            if (!firing)
            {
                int lineStart = PassDelay + ShutterLead;
                float chargeRatio = MathHelper.Clamp((age - lineStart) / (float)Math.Max(1, TellTicks - lineStart), 0f, 1f);
                float pulse = Math.Abs((float)Math.Cos(age / 14f)) * 0.16f;
                drawWidth = MathHelper.Lerp(90f, 130f, chargeRatio);
                opacity = 0.24f + 0.24f * chargeRatio + pulse;
                if (fizzled)
                {
                    opacity *= MathHelper.Clamp((EndTick + FadeDuration - age) / (float)FadeDuration, 0f, 1f);
                }
                scroll = 10f * age;
                coreIntensity = 0f;
            }
            else
            {
                float fade = MathHelper.Clamp((EndTick + FadeDuration - age) / (float)FadeDuration, 0f, 1f);
                int fireAge = age - TellTicks;
                drawWidth = IsOrbitBeam || IsRamBeam ? 110f : 220f;
                if (IsRipple)
                {
                    drawWidth = 154f;
                }
                opacity = fade;
                float scrollSpeed = fiery ? 21f : 12f;
                scroll = -scrollSpeed * fireAge;
                coreIntensity = 1.55f * fade;
            }

            Vector2? secondMuzzle = null;
            if (IsShortPair)
            {
                drawWidth = 54f;
                secondMuzzle = oppositeMuzzle;
            }

            Vector3 hslColor = Main.rgbToHsl(new Color(255, 36, 8));
            hslColor.X += 0.025f * (float)Math.Cos(age / 25f);
            Color beamColor = Main.hslToRgb(hslColor);
            string technique = firing && fiery ? "DestroyerOverchargedHull" : "DestroyerHullLaser";
            Vector2 endpoint = Projectile.Center + Projectile.velocity * BeamLength;
            DrawLaser(Projectile.Center, endpoint, drawWidth, beamColor, opacity, coreIntensity, scroll, technique, secondMuzzle);
            return false;
        }

        internal static void DrawLegacyBeam(Vector2 start, Vector2 direction, int length, int age, float opacity)
        {
            // The original Destroyer's packed shader supplies the turbulence around a straight beam axis.
            legacyBeamEffect ??= ModContent.Request<Effect>("tsorcRevamp/Effects/GenericLaser", ReLogic.Content.AssetRequestMode.ImmediateLoad);
            Effect effect = legacyBeamEffect.Value;
            Texture2D texture = tsorcRevamp.NoiseTurbulent;
            const int width = 210;
            Rectangle source = new Rectangle(0, 0, length, width);
            Vector2 drawOrigin = new Vector2(0f, width * 0.5f);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearWrap,
                DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            try
            {
                effect.CurrentTechnique = effect.Techniques["GenericLaser"];
                effect.Parameters["Time"].SetValue((float)age);
                effect.Parameters["Color"].SetValue(new Vector3(1f, 0.08f, 0.02f));
                effect.Parameters["SecondaryColor"].SetValue(Vector3.One);
                effect.Parameters["FadeOut"].SetValue(opacity);
                effect.Parameters["ProjectileSize"].SetValue(new Vector2(length, width));
                effect.Parameters["TextureSize"].SetValue((float)texture.Width);
                effect.CurrentTechnique.Passes[0].Apply();
                Main.EntitySpriteDraw(texture, start - Main.screenPosition, source, Color.White,
                    direction.ToRotation(), drawOrigin, 1f, SpriteEffects.None, 0f);
            }
            finally
            {
                UsefulFunctions.RestartSpritebatch(ref Main.spriteBatch);
            }
        }

        internal static void DrawLaser(Vector2 start, Vector2 end, float width, Color color, float opacity, float coreIntensity,
            float scroll, string technique, Vector2? secondMuzzle = null)
        {
            Vector2 offset = end - start;
            float length = offset.Length();
            if (length < 1f)
            {
                return;
            }

            laserEffect ??= ModContent.Request<Effect>("tsorcRevamp/Effects/DestroyerMawBeam", ReLogic.Content.AssetRequestMode.ImmediateLoad);
            turbulentNoise ??= ModContent.Request<Texture2D>("tsorcRevamp/Textures/Noise/TurbulentNoise", ReLogic.Content.AssetRequestMode.ImmediateLoad);
            Effect effect = laserEffect.Value;
            Texture2D primary = turbulentNoise.Value;

            Vector2 drawSize = new Vector2(length, width);
            Vector2 blocks = drawSize / PixelBlockSize;
            Vector2 origin = new Vector2(0f, primary.Height * 0.5f);
            Vector2 drawPosition = start - Main.screenPosition;
            float taper = length / 110f;
            if (technique == "DestroyerRepairBeam" || secondMuzzle.HasValue)
            {
                taper = Math.Max(6f, length / 64f);
            }

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearWrap,
                DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            try
            {
                effect.CurrentTechnique = effect.Techniques[technique];
                effect.Parameters["PixelGrid"].SetValue(new Vector4(blocks.X, blocks.Y, 1f / blocks.X, 1f / blocks.Y));
                effect.Parameters["Opacity"].SetValue(opacity);
                effect.Parameters["Time"].SetValue(coreIntensity);
                effect.Parameters["BeamColor"].SetValue(color.ToVector3());
                effect.Parameters["NoiseScale"].SetValue(new Vector4(
                    length / primary.Width,
                    width / primary.Height,
                    scroll / primary.Width,
                    taper));
                effect.CurrentTechnique.Passes[0].Apply();
                float rotation = offset.ToRotation();
                Vector2 scale = drawSize / primary.Size();
                Main.EntitySpriteDraw(primary, drawPosition, null, Color.White, rotation, origin, scale, SpriteEffects.None, 0f);
                if (secondMuzzle.HasValue)
                {
                    Vector2 oppositePosition = secondMuzzle.Value - Main.screenPosition;
                    Main.EntitySpriteDraw(primary, oppositePosition, null, Color.White, rotation + MathHelper.Pi,
                        origin, scale, SpriteEffects.None, 0f);
                }
            }
            finally
            {
                UsefulFunctions.RestartSpritebatch(ref Main.spriteBatch);
            }

        }

        public override void DrawBehind(int index, System.Collections.Generic.List<int> behindNPCsAndTiles,
            System.Collections.Generic.List<int> behindNPCs, System.Collections.Generic.List<int> behindProjectiles,
            System.Collections.Generic.List<int> overPlayers, System.Collections.Generic.List<int> overWiresUI)
        {
            behindProjectiles.Add(index);
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.dedServ || !initialized || !IsProbe && age - PassDelay + PortHeatLead < TheDestroyer.HullArmingTicks)
            {
                return;
            }

            int dustCount = IsShortPair ? 2 : 8;
            for (int i = 0; i < dustCount; i++)
            {
                Vector2 dustVelocity = Projectile.velocity * 3f + Main.rand.NextVector2Circular(4f, 4f);
                Dust.NewDustPerfect(Projectile.Center, DustID.OrangeTorch, dustVelocity, Scale: 1.25f).noGravity = true;
            }
        }
    }
}

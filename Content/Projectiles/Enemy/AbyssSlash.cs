using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.NPCs.Puppets;

namespace tsorcRevamp.Content.Projectiles.Enemy
{
    // Straight-line crescent slash, fired toward wherever the player was at release (no homing).
    // The source sprite is a plain white 4-frame crescent (170x170/frame) - tinted purple via
    // GetAlpha, scaled down small ("tiny"), with a purple point light and dust trail.
    // Spiral Fan's variant (ai[1] = 1) instead flies a boomerang loop: see UpdateFanBoomerangFlight.
    class AbyssSlash : ModProjectile
    {
        // Spiral Fan's crescent spins in place while the projectile keeps flying dead straight.
        // 0.38 rad/tick = ~22°/tick, ~3.6 turns a second: a buzzing blade, still under the ~30°/tick
        // where a 60fps spin starts to strobe or read as turning backwards.
        const float FanCrescentSpinPerTick = 0.38f;
        // World px one 170px sprite frame is drawn at (the old crisp core was 54x52).
        const float FanCrescentFrameDrawSize = 60f;

        bool UsesFanBoomerangVFX => Projectile.ai[1] >= 0.5f;
        // ai[2] = Spiral Fan's sweep direction (+1/-1), so the crescents spin the way the fan turns.
        float FanSpinDirection => Projectile.ai[2] < 0f ? -1f : 1f;

        // ── Spiral Fan boomerang flight (fan variant only). Speed stays the launch speed throughout. ──
        // Outbound: straight until it has flown this far beyond the nearest player's launch distance.
        const float FanOvershootPastPlayer = 500f;
        // Return: heading turns at most this many rad/tick toward the locked player. At the 8.5px/tick
        // fan speed that is a ~120px turning radius, so the U-turn takes ~45 ticks - a readable loop.
        const float FanReturnTurnRate = 0.07f;
        // "Lined up" = heading within ~45° of the player (cos 0.7). Only after lining up does the player
        // dropping behind the crescent (cos < 0) count as a pass - during the U-turn they start behind it.
        const float FanLinedUpCosine = 0.7f;
        // If the player keeps outrunning the return leg this long, stop homing and leave anyway.
        const int FanMaxReturnTicks = 180;
        // Off screen = outside this box around EVERY active player (1080p half-screen 960x540 + margin).
        // Measured against players rather than Main.screenPosition so the server can decide it too.
        const float FanOffscreenHalfWidth = 1050f;
        const float FanOffscreenHalfHeight = 650f;
        const int FanFadeTicks = 20;
        const int FanTurnPulseTicks = 12;
        // Backstop only: outbound ~140t at 650px range + ~45t U-turn + return + exit fits well inside.
        const int FanMaxLifetimeTicks = 720;

        enum FanFlight : byte { Outbound, Returning, Leaving, Fading }

        FanFlight _fanFlight;
        int _fanStateTicks;
        bool _fanLaunchSet;
        Vector2 _fanLaunchOrigin;
        float _fanTurnDistance;
        int _fanTargetIndex = -1;
        bool _fanLinedUp;

        float _dustOrbitAngle;


        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 4;
            ProjectileID.Sets.TrailCacheLength[Type] = 12;
            ProjectileID.Sets.TrailingMode[Type] = 2;
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 120;
        }

        public override void SetDefaults()
        {
            Projectile.width = 24;
            Projectile.height = 24;
            Projectile.scale = 0.35f;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = 1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.light = 0.6f;
            Projectile.timeLeft = 180;
        }

        public override void AI()
        {
            if (UsesFanBoomerangVFX)
            {
                // Visual spin only: velocity, and so the travel path and hitbox, are untouched.
                // TrailingMode 2 records this spun rotation in oldRot, so the afterimages spin too.
                Projectile.rotation += FanCrescentSpinPerTick * FanSpinDirection;
                UpdateFanBoomerangFlight();
            }
            else
            {
                Projectile.rotation = Projectile.velocity.ToRotation();
            }

            Lighting.AddLight(Projectile.Center, Color.White.ToVector3() * 1.1f);
            if (UsesFanBoomerangVFX)
            {
                // The fan variant carries the boomerang shader stack (purple magic) on top of the
                // plain white crescent light above - casts both instead of replacing one.
                Lighting.AddLight(Projectile.Center, new Vector3(0.55f, 0.15f, 0.85f));
            }

            Animate();

            if (!Main.dedServ && Main.rand.NextBool(UsesFanBoomerangVFX ? 2 : 3))
            {
                bool white = Main.rand.NextBool(UsesFanBoomerangVFX ? 6 : 5);
                Dust d = Dust.NewDustPerfect(Projectile.Center,
                    white ? DustID.SilverFlame : DustID.ShadowbeamStaff,
                    -Projectile.velocity * Main.rand.NextFloat(0.05f, 0.12f), 110,
                    white ? new Color(232, 226, 255) : new Color(150, 48, 228),
                    Main.rand.NextFloat(0.72f, 1.02f));
                d.noGravity = true;
            }

            if (UsesFanBoomerangVFX)
            {
                SpawnOrbitingDust();
            }
        }

        /// <summary>Extra purple motes circling INSIDE the boomerang shader silhouette (Orbit draws
        /// out to ~92px across - this stays well within that, ~10-22px out from center), fan variant
        /// only. Mirrors BoomerangCrescent's own SpawnOrbitingDust so the two attacks that share this
        /// shader stack read consistently.</summary>
        void SpawnOrbitingDust()
        {
            if (Main.dedServ)
            {
                return;
            }

            _dustOrbitAngle += 0.30f;
            if (!Main.rand.NextBool(2))
            {
                return;
            }

            float radius = Main.rand.NextFloat(10f, 22f);
            Vector2 radial = _dustOrbitAngle.ToRotationVector2();
            Vector2 tangent = radial.RotatedBy(MathHelper.PiOver2);
            Vector2 position = Projectile.Center + radial * radius;
            Vector2 velocity = tangent * Main.rand.NextFloat(0.9f, 1.7f);
            Dust d = Dust.NewDustPerfect(position, DustID.PurpleTorch, velocity, 110,
                new Color(170, 70, 235), Main.rand.NextFloat(0.55f, 0.85f));
            d.noGravity = true;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            float rotation = direction.ToRotation();

            if (UsesFanBoomerangVFX)
            {
                Texture2D texture = TextureAssets.Projectile[Type].Value;
                // Frames 0-2 only: frame 3 of the sheet is a few scattered specks, which the pixel
                // filter turns into a blinking cloud instead of a crescent every fourth frame.
                int crescentFrameIndex = Projectile.frame % 3;
                Rectangle frame = texture.Frame(1, Main.projFrames[Type], 0, crescentFrameIndex);

                // 1 in flight; ramps to 0 over FanFadeTicks once it has left every player's screen.
                float fade = 1f;
                if (_fanFlight == FanFlight.Fading)
                {
                    fade = MathHelper.Clamp(1f - _fanStateTicks / (float)FanFadeTicks, 0f, 1f);
                }

                // The return leg borrows Boomerang Crescent's magenta "coming back" palette on the
                // ribbon/orbit; the crescent itself stays purple.
                bool returning = _fanFlight == FanFlight.Returning;

                // Pixelated afterimages at 87% size (the old 0.27 vs 0.31 sprite scale ratio).
                for (int i = Projectile.oldPos.Length - 2; i >= 3; i -= 4)
                {
                    if (Projectile.oldPos[i] == Vector2.Zero)
                    {
                        continue;
                    }

                    float history = 1f - i / (float)Projectile.oldPos.Length;
                    Vector2 oldCenter = Projectile.oldPos[i] + Projectile.Size * 0.5f;
                    float echoOpacity = (0.22f + history * 0.30f) * fade;
                    ArtoriasVFX.DrawFanCrescent(texture, frame, oldCenter, Projectile.oldRot[i],
                        FanCrescentFrameDrawSize * 0.87f, echoOpacity);
                }

                // Ribbon and orbit keep the travel heading, not the spin: they describe the path.
                ArtoriasVFX.DrawBoomerangRibbon(Projectile.Center - direction * 40f,
                    rotation + MathHelper.PiOver2, new Vector2(38f, 116f),
                    returning, curveDirection: 1f, opacity: 0.88f * fade);
                ArtoriasVFX.DrawBoomerangOrbit(Projectile.Center, Vector2.One * 92f,
                    rotation * 0.28f, returning, curveDirection: 1f, opacity: 0.86f * fade);

                // The spinning crescent on top. It is opaque and outlined, so it carries the
                // projectile's silhouette on its own - the old alpha body sprite is gone.
                ArtoriasVFX.DrawFanCrescent(texture, frame, Projectile.Center, Projectile.rotation,
                    FanCrescentFrameDrawSize, fade);

                // Turnaround cue: the same expanding magenta vortex Boomerang Crescent flashes.
                if (returning && _fanStateTicks < FanTurnPulseTicks)
                {
                    float pulseProgress = _fanStateTicks / (float)FanTurnPulseTicks;
                    Vector2 pulseSize = Vector2.One * MathHelper.Lerp(74f, 108f, pulseProgress);
                    ArtoriasVFX.DrawBoomerangPulse(Projectile.Center, pulseSize, pulseProgress, 0.88f);
                }
                return false;
            }

            float animationProgress = (Projectile.frame + Projectile.frameCounter / 4f)
                / Main.projFrames[Type];
            // 94x110 = the old 78x92 at +20%, matching the approved C-shaped arc revamp
            // (ArtoriasSwordSwipe.fx - see the offline preview harness's abyss_slash_arc FOCUS).
            ArtoriasVFX.DrawSwordSwipe(Projectile.Center, rotation,
                new Vector2(94f, 110f), animationProgress, 0.94f);
            return false;
        }

        /// <summary>Spiral Fan's boomerang flight, fan variant only: straight out to 500px past the
        /// nearest player, a limited-turn U-turn locked onto the nearest player, straight on once it has
        /// passed them, then a fade after leaving every player's screen. Speed never changes.</summary>
        void UpdateFanBoomerangFlight()
        {
            _fanStateTicks++;

            if (!_fanLaunchSet)
            {
                // Locked on the first tick. Every shot gets the same turn distance, so shots fired away
                // from the player still come back, and a shot aimed at them turns exactly 500px past.
                _fanLaunchSet = true;
                _fanLaunchOrigin = Projectile.Center;
                int launchPlayerIndex = Player.FindClosest(Projectile.position, Projectile.width, Projectile.height);
                Player launchPlayer = Main.player[launchPlayerIndex];
                float launchPlayerDistance = Vector2.Distance(Projectile.Center, launchPlayer.Center);
                _fanTurnDistance = launchPlayerDistance + FanOvershootPastPlayer;
                Projectile.timeLeft = FanMaxLifetimeTicks;
            }

            switch (_fanFlight)
            {
                case FanFlight.Outbound:
                {
                    // The path is straight, so distance from the origin IS distance travelled.
                    float travelled = Vector2.Distance(Projectile.Center, _fanLaunchOrigin);
                    if (travelled >= _fanTurnDistance)
                    {
                        // Lock ONE player for the return so it can't swap targets mid-loop.
                        _fanTargetIndex = Player.FindClosest(Projectile.position, Projectile.width, Projectile.height);
                        _fanLinedUp = false;
                        EnterFanFlight(FanFlight.Returning);
                    }
                    break;
                }

                case FanFlight.Returning:
                {
                    bool targetInvalid = _fanTargetIndex < 0
                        || !Main.player[_fanTargetIndex].active
                        || Main.player[_fanTargetIndex].dead;
                    if (targetInvalid)
                    {
                        _fanTargetIndex = Player.FindClosest(Projectile.position, Projectile.width, Projectile.height);
                    }

                    Player target = Main.player[_fanTargetIndex];
                    Vector2 toTarget = target.Center - Projectile.Center;
                    float speed = Projectile.velocity.Length();

                    // Rotate the heading toward the player by at most FanReturnTurnRate; speed unchanged.
                    float currentAngle = Projectile.velocity.ToRotation();
                    float desiredAngle = toTarget.ToRotation();
                    float newAngle = currentAngle.AngleTowards(desiredAngle, FanReturnTurnRate);
                    Projectile.velocity = newAngle.ToRotationVector2() * speed;

                    // Cosine between heading and the line to the player: 1 = dead on, below 0 = behind us.
                    Vector2 heading = Projectile.velocity.SafeNormalize(Vector2.UnitX);
                    Vector2 targetDirection = toTarget.SafeNormalize(heading);
                    float facingCosine = Vector2.Dot(heading, targetDirection);
                    if (facingCosine >= FanLinedUpCosine)
                    {
                        _fanLinedUp = true;
                    }

                    bool passedTarget = _fanLinedUp && facingCosine < 0f;
                    bool gaveUp = _fanStateTicks >= FanMaxReturnTicks;
                    if (passedTarget || gaveUp)
                    {
                        EnterFanFlight(FanFlight.Leaving);
                    }
                    break;
                }

                case FanFlight.Leaving:
                {
                    // Keep flying straight until no active player could still have it on screen.
                    bool visibleToAnyone = false;
                    for (int i = 0; i < Main.maxPlayers; i++)
                    {
                        Player player = Main.player[i];
                        if (!player.active)
                        {
                            continue;
                        }

                        Vector2 offset = Projectile.Center - player.Center;
                        bool insideX = System.Math.Abs(offset.X) < FanOffscreenHalfWidth;
                        bool insideY = System.Math.Abs(offset.Y) < FanOffscreenHalfHeight;
                        if (insideX && insideY)
                        {
                            visibleToAnyone = true;
                            break;
                        }
                    }

                    if (!visibleToAnyone)
                    {
                        EnterFanFlight(FanFlight.Fading);
                    }
                    break;
                }

                case FanFlight.Fading:
                {
                    if (_fanStateTicks >= FanFadeTicks)
                    {
                        Projectile.Kill();
                    }
                    break;
                }
            }

            // Lifetime backstop: whatever the state, start the fade before timeLeft pops it at full opacity.
            if (_fanFlight != FanFlight.Fading && Projectile.timeLeft <= FanFadeTicks)
            {
                EnterFanFlight(FanFlight.Fading);
            }
        }

        void EnterFanFlight(FanFlight flight)
        {
            _fanFlight = flight;
            _fanStateTicks = 0;
            Projectile.netUpdate = true;
        }

        // A fading crescent deals no damage, so its last visible frames can never land a surprise hit.
        public override bool? CanDamage()
        {
            if (_fanFlight == FanFlight.Fading)
            {
                return false;
            }

            return null;
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write((byte)_fanFlight);
            writer.Write((short)_fanStateTicks);
            writer.Write((short)_fanTargetIndex);
            writer.Write(_fanLinedUp);
            writer.Write(_fanLaunchSet);
            writer.Write(_fanTurnDistance);
            writer.WriteVector2(_fanLaunchOrigin);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            _fanFlight = (FanFlight)reader.ReadByte();
            _fanStateTicks = reader.ReadInt16();
            _fanTargetIndex = reader.ReadInt16();
            _fanLinedUp = reader.ReadBoolean();
            _fanLaunchSet = reader.ReadBoolean();
            _fanTurnDistance = reader.ReadSingle();
            _fanLaunchOrigin = reader.ReadVector2();
        }

        void Animate()
        {
            Projectile.frameCounter++;
            if (Projectile.frameCounter >= 4)
            {
                Projectile.frameCounter = 0;
                Projectile.frame = (Projectile.frame + 1) % Main.projFrames[Type];
            }
        }

        public override Color? GetAlpha(Color lightColor)
        {
            return new Color(190, 90, 255, 220);
        }

        // ai[0] holds the firing NPC's whoAmI + 1 (0 = "no owner") so the dodge-punish-chain system
        // knows this swipe actually connected. The +1 offset exists because this projectile is ALSO
        // reused anonymously (no owner) by Spiral Fan's straight-shot bursts, which have nothing to
        // do with that system and must not accidentally report a hit against whatever NPC happens
        // to occupy slot 0. ai[1] = 1 is Spiral Fan's variant: the richer boomerang shader stack AND
        // the boomerang flight path (UpdateFanBoomerangFlight). Collision is unchanged either way.
        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            int ownerIdx = (int)Projectile.ai[0] - 1;
            if (ownerIdx >= 0 && ownerIdx < Main.maxNPCs && Main.npc[ownerIdx].active
                && Main.npc[ownerIdx].ModNPC is PuppetNPC invader)
            {
                invader.ReportAttackHit();
            }
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.dedServ)
            {
                return;
            }
            for (int i = 0; i < 4; i++)
            {
                Vector2 vel = Main.rand.NextVector2Circular(3f, 3f);
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.PurpleTorch, vel, 60, default, 1f);
                d.noGravity = true;
            }
        }
    }
}

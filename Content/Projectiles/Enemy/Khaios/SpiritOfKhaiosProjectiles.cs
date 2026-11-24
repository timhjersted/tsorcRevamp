using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items.Weapons.Summon.Runeterra.AurelionSol.Sounds.CenterOfTheUniverse;
using tsorcRevamp.Content.Projectiles.Enemy.Gwyn;
using tsorcRevamp.Content.Projectiles.Enemy.Triad;
using tsorcRevamp.Content.Projectiles.Ranged.Ammo;
using tsorcRevamp.Content.Projectiles.VFX;
using tsorcRevamp.NPCs.Invaders;

namespace tsorcRevamp.Content.Projectiles.Enemy.Khaios
{
    /// <summary>
    /// Hostile lightning arrow fired by Spirit of Khaios's Forgotten Thunder Bow.
    /// Uses the same ballistic profile as Owl Father's arrows (15t straight flight, then +0.1 px/t^2
    /// gravity) so gravity-compensated intercept aiming lands accurately at all ranges.
    /// </summary>
    public class KhaiosThunderArrow : ModProjectile
    {
        public const float LaunchSpeed = 14.5f;
        public const float GravityDelayTicks = 15f;
        public const float GravityAccel = 0.1f;

        private int flightTicks;

        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(LightningBoltProjectile));

        public override void SetDefaults()
        {
            Projectile.width = 12;
            Projectile.height = 12;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.tileCollide = true;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 240;
            Projectile.scale = 1.15f;
        }

        public override void AI()
        {
            flightTicks++;
            if (flightTicks > GravityDelayTicks)
            {
                Projectile.velocity.Y += GravityAccel;
            }

            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;

            if (!Main.dedServ)
            {
                Lighting.AddLight(Projectile.Center, 0.35f, 0.72f, 1.0f);

                int dustType = flightTicks % 3 == 0 ? DustID.GoldFlame : DustID.Electric;
                Dust spark = Dust.NewDustPerfect(
                    Projectile.Center + Main.rand.NextVector2Circular(4f, 4f),
                    dustType,
                    -Projectile.velocity * 0.15f + Main.rand.NextVector2Circular(0.8f, 0.8f),
                    40,
                    Color.Cyan,
                    Main.rand.NextFloat(0.9f, 1.3f));
                spark.noGravity = true;
            }
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(BuffID.Electrified, 180);
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.dedServ)
            {
                return;
            }

            SoundEngine.PlaySound(SoundID.Item94 with { Volume = 0.30f, Pitch = 0.35f, PitchVariance = 0.1f }, Projectile.Center);
            for (int i = 0; i < 48; i++)
            {
                int dustType = i % 3 == 0 ? DustID.GoldFlame : DustID.Electric;
                Dust burst = Dust.NewDustPerfect(
                    Projectile.Center,
                    dustType,
                    Main.rand.NextVector2Circular(6.2f, 6.2f),
                    40,
                    Color.Cyan,
                    Main.rand.NextFloat(0.95f, 1.65f));
                burst.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Projectile.type].Value;
            Vector2 origin = texture.Size() * 0.5f;
            Main.EntitySpriteDraw(
                texture,
                Projectile.Center - Main.screenPosition,
                null,
                Color.White,
                Projectile.rotation,
                origin,
                Projectile.scale,
                SpriteEffects.None,
                0);
            return false;
        }
    }

    /// <summary>
    /// Central storm-lance fired by Spirit of Khaios's Forgotten Thunder Split-Shot.
    /// Zero-gravity high-velocity bolt mirroring the player Forgotten Thunder Bow's central lightning arc.
    /// </summary>
    public class KhaiosThunderLance : ModProjectile
    {
        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(GwynLightningSpear));

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 4;
        }

        public override void SetDefaults()
        {
            Projectile.width = 34;
            Projectile.height = 20;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.tileCollide = true;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 180;
        }

        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation();
            Projectile.frameCounter++;
            if (Projectile.frameCounter >= 4)
            {
                Projectile.frameCounter = 0;
                Projectile.frame = (Projectile.frame + 1) % Main.projFrames[Projectile.type];
            }

            if (!Main.dedServ)
            {
                Lighting.AddLight(Projectile.Center, 0.55f, 0.85f, 1.1f);

                for (int i = 0; i < 2; i++)
                {
                    int dustType = i == 0 ? DustID.Electric : DustID.GoldFlame;
                    Dust trail = Dust.NewDustPerfect(
                        Projectile.Center + Main.rand.NextVector2Circular(10f, 10f),
                        dustType,
                        -Projectile.velocity * 0.12f + Main.rand.NextVector2Circular(1.4f, 1.4f),
                        30,
                        Color.Cyan,
                        Main.rand.NextFloat(1.0f, 1.55f));
                    trail.noGravity = true;
                }
            }
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(BuffID.Electrified, 240);
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.dedServ)
            {
                return;
            }

            SoundEngine.PlaySound(SoundID.Item94 with { Volume = 0.55f, Pitch = 0.12f }, Projectile.Center);
            UsefulFunctions.ScreenShake(Projectile.Center, 3.2f, 10, distanceFalloff: 600f);

            for (int i = 0; i < 65; i++)
            {
                int dustType = i % 3 == 0 ? DustID.GoldFlame : DustID.Electric;
                Dust spark = Dust.NewDustPerfect(
                    Projectile.Center,
                    dustType,
                    Main.rand.NextVector2Circular(8f, 8f),
                    30,
                    Color.Cyan,
                    Main.rand.NextFloat(1.05f, 1.85f));
                spark.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Projectile.type].Value;
            int frameHeight = texture.Height / Main.projFrames[Projectile.type];
            Rectangle frame = new Rectangle(0, Projectile.frame * frameHeight, texture.Width, frameHeight);
            Vector2 origin = frame.Size() * 0.5f;
            SpriteEffects spriteEffects = Projectile.velocity.X < 0f
                ? SpriteEffects.None
                : SpriteEffects.FlipHorizontally;

            Main.EntitySpriteDraw(
                texture,
                Projectile.Center - Main.screenPosition,
                frame,
                Color.Lerp(Color.White, Color.Cyan, 0.35f),
                Projectile.rotation,
                origin,
                0.65f,
                spriteEffects,
                0);
            return false;
        }
    }

    /// <summary>
    /// Phase 2 Celestial Star Ring orbiter using the pixelated InterstellarVessel shader.
    /// Lifecycle: 45t harmless fade-in -> ~240t expanding orbit (105px -> 325px) that hurts on
    /// contact AND intercepts non-piercing player projectiles -> staggered 25t glint tell ->
    /// sequential launch directly at the player with a full stardust shatter on kill.
    /// ai[0] = owner SpiritOfKhaios whoAmI, ai[1] = base orbit angle, ai[2] = launch slot index.
    /// </summary>
    public class KhaiosOrbitStar : DynamicTrail
    {
        private const int SpawnFadeTicks = 45;
        private const int ExpandingOrbitTicks = 235;
        private const int LaunchGlintTicks = 25;
        private const int LaunchSlotStaggerTicks = 24;
        private const float StartOrbitRadius = 105f;
        private const float MaxOrbitRadius = 325f;
        private const float OrbitAngularSpeed = 0.036f;
        private const float LaunchSpeed = 15.5f;
        private const float BlockRadiusPixels = 38f;

        private static readonly string StarSoundBase = UsefulFunctions.RefactorableFilepath(typeof(CenterOfTheUniverseSound));
        private static int globalBlockSoundCooldown;

        private int ageTicks;
        private float currentAngle;
        private bool launched;
        private bool launchLocked;
        private Vector2 lockedLaunchDir;
        private Vector2 samplePointOffset1;
        private Vector2 samplePointOffset2;
        private bool exploded;

        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(HomingStarStar));

        public override void SetStaticDefaults()
        {
            base.SetStaticDefaults();
        }

        public override void SetDefaults()
        {
            Projectile.width = 40;
            Projectile.height = 40;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 600;

            trailWidth = 28;
            trailPointLimit = 180;
            trailMaxLength = 185;
            newPointDistance = 6f;
            collisionPadding = 0;
            NPCSource = false;
            trailCollision = false;
            normalCollision = true;
            noFadeOut = true;
            customEffect = ModContent.Request<Effect>(
                "tsorcRevamp/Effects/InterstellarVesselPixelated",
                AssetRequestMode.ImmediateLoad).Value;
        }

        public override bool? CanDamage()
        {
            return ageTicks >= SpawnFadeTicks;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (ageTicks < SpawnFadeTicks)
            {
                return false;
            }

            Vector2 closest = Vector2.Clamp(
                Projectile.Center,
                new Vector2(targetHitbox.Left, targetHitbox.Top),
                new Vector2(targetHitbox.Right, targetHitbox.Bottom));
            return Vector2.DistanceSquared(Projectile.Center, closest) <= 22f * 22f;
        }

        public override void AI()
        {
            if (ageTicks == 0)
            {
                currentAngle = Projectile.ai[1];
            }

            ageTicks++;
            if (globalBlockSoundCooldown > 0)
            {
                globalBlockSoundCooldown--;
            }

            int ownerIndex = (int)Projectile.ai[0];
            bool ownerAlive = ownerIndex >= 0
                && ownerIndex < Main.maxNPCs
                && Main.npc[ownerIndex].active
                && Main.npc[ownerIndex].type == ModContent.NPCType<SpiritOfKhaios>();

            if (!launched && !ownerAlive)
            {
                SpawnStarShatter();
                Projectile.Kill();
                return;
            }

            int slotIndex = (int)Math.Round(Projectile.ai[2]);
            int glintStartTick = SpawnFadeTicks + ExpandingOrbitTicks + slotIndex * LaunchSlotStaggerTicks;
            int launchTick = glintStartTick + LaunchGlintTicks;

            if (!launched)
            {
                NPC owner = Main.npc[ownerIndex];
                float spawnProgress = MathHelper.Clamp(ageTicks / (float)SpawnFadeTicks, 0f, 1f);
                fadeOut = spawnProgress * 0.95f;

                float orbitElapsed = Math.Max(0f, ageTicks - SpawnFadeTicks);
                float expandProgress = MathHelper.Clamp(orbitElapsed / ExpandingOrbitTicks, 0f, 1f);

                // Smoothly expands outward away from the boss so the circling ring actively sweeps out to hit the player.
                float targetRadius = MathHelper.Lerp(StartOrbitRadius, MaxOrbitRadius, expandProgress);
                float speedScale = MathHelper.Lerp(1.0f, 1.38f, expandProgress);
                currentAngle += OrbitAngularSpeed * speedScale;

                Vector2 desiredCenter = owner.Center + currentAngle.ToRotationVector2() * targetRadius;
                Projectile.velocity = desiredCenter - Projectile.Center;
                Projectile.Center = desiredCenter;

                // Update trail AFTER moving the orbiter so vertex strip points never lag or kink on turns.
                base.AI();

                // Intercept non-piercing player projectiles while orbiting.
                if (ageTicks >= SpawnFadeTicks && Main.netMode != NetmodeID.MultiplayerClient)
                {
                    InterceptPlayerProjectiles();
                }

                // Pre-launch glint telegraph.
                if (ageTicks >= glintStartTick)
                {
                    if (!launchLocked)
                    {
                        launchLocked = true;
                        Player target = GetValidTarget(owner);
                        lockedLaunchDir = target != null
                            ? (target.Center + target.velocity * 8f - Projectile.Center).SafeNormalize(Vector2.UnitX)
                            : currentAngle.ToRotationVector2();
                        Projectile.netUpdate = true;

                        if (!Main.dedServ)
                        {
                            SoundEngine.PlaySound(
                                new SoundStyle(StarSoundBase + "_Marked") with { Volume = 0.42f, Pitch = 0.15f },
                                Projectile.Center);
                        }
                    }

                    if (!Main.dedServ)
                    {
                        Vector2 offset = Main.rand.NextVector2CircularEdge(32f, 32f);
                        Dust glint = Dust.NewDustPerfect(
                            Projectile.Center + offset,
                            DustID.GoldFlame,
                            -offset * 0.14f,
                            20,
                            new Color(255, 225, 95),
                            Main.rand.NextFloat(1.0f, 1.5f));
                        glint.noGravity = true;
                    }
                }

                if (ageTicks >= launchTick)
                {
                    launched = true;
                    Projectile.tileCollide = true;
                    Projectile.penetrate = 1;
                    Projectile.timeLeft = 180;
                    Projectile.velocity = lockedLaunchDir * LaunchSpeed;

                    // Restart the comet tail cleanly at launch so the sudden angle pivot never folds the vertex strip.
                    trailPositions?.Clear();
                    trailRotations?.Clear();
                    lastPosition = Projectile.Center;
                    Projectile.netUpdate = true;

                    if (!Main.dedServ)
                    {
                        int hitVariant = (slotIndex % 3) + 1;
                        SoundEngine.PlaySound(
                            new SoundStyle(StarSoundBase + "_CirclingProjectileHit" + hitVariant) with { Volume = 0.48f },
                            Projectile.Center);
                    }
                }
            }
            else
            {
                base.AI();
                fadeOut = 1f;
            }

            if (!Main.dedServ)
            {
                Lighting.AddLight(Projectile.Center, 0.5f, 0.72f, 1.0f);

                for (int i = 0; i < 2; i++)
                {
                    int dustType = i == 0 ? DustID.GoldFlame : DustID.IceTorch;
                    Color tint = i == 0 ? new Color(255, 232, 120) : Color.DeepSkyBlue;
                    Dust starDust = Dust.NewDustPerfect(
                        Projectile.Center + Main.rand.NextVector2Circular(10f, 10f),
                        dustType,
                        -Projectile.velocity * 0.10f + Main.rand.NextVector2Circular(1.0f, 1.0f),
                        20,
                        tint,
                        Main.rand.NextFloat(0.85f, 1.35f));
                    starDust.noGravity = true;
                }
            }
        }

        private void InterceptPlayerProjectiles()
        {
            float radiusSq = BlockRadiusPixels * BlockRadiusPixels;
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile other = Main.projectile[i];
                if (!other.active || !other.friendly || other.hostile || other.damage <= 0 || other.minion || other.sentry)
                {
                    continue;
                }

                // Only intercept standard finite-penetrate shots so player summons, whips, and beams stay reliable.
                if (other.penetrate < 0 || other.penetrate > 3)
                {
                    continue;
                }

                if (Vector2.DistanceSquared(Projectile.Center, other.Center) <= radiusSq)
                {
                    other.Kill();
                    SpawnDeflectSpark(other.Center);
                }
            }
        }

        private void SpawnDeflectSpark(Vector2 position)
        {
            if (Main.dedServ)
            {
                return;
            }

            if (globalBlockSoundCooldown <= 0)
            {
                globalBlockSoundCooldown = 14;
                SoundEngine.PlaySound(
                    new SoundStyle("tsorcRevamp/Sounds/HollowKnight/hero_thorn_counter") with { Volume = 0.36f, Pitch = 0.18f },
                    position);
            }

            for (int i = 0; i < 12; i++)
            {
                Dust spark = Dust.NewDustPerfect(
                    position,
                    i % 2 == 0 ? DustID.GoldFlame : DustID.Electric,
                    Main.rand.NextVector2Circular(4.5f, 4.5f),
                    30,
                    Color.Cyan,
                    Main.rand.NextFloat(0.9f, 1.35f));
                spark.noGravity = true;
            }
        }

        private static Player GetValidTarget(NPC owner)
        {
            if (owner.target >= 0 && owner.target < Main.maxPlayers)
            {
                Player target = Main.player[owner.target];
                if (target.active && !target.dead)
                {
                    return target;
                }
            }

            return null;
        }

        public override void OnKill(int timeLeft)
        {
            SpawnStarShatter();
        }

        private void SpawnStarShatter()
        {
            if (exploded || Main.dedServ)
            {
                return;
            }

            exploded = true;
            SoundEngine.PlaySound(SoundID.Item27 with { Volume = 0.38f, Pitch = 0.15f }, Projectile.Center);
            for (int i = 0; i < 52; i++)
            {
                int dustType = i % 3 == 0 ? DustID.GoldFlame : DustID.AncientLight;
                Dust burst = Dust.NewDustPerfect(
                    Projectile.Center,
                    dustType,
                    Main.rand.NextVector2Circular(7f, 7f),
                    40,
                    Color.DeepSkyBlue,
                    Main.rand.NextFloat(1.0f, 1.75f));
                burst.noGravity = true;
            }
        }

        public override void SetEffectParameters(Effect effect)
        {
            trailWidth = 28;
            trailMaxLength = 185;

            effect.Parameters["noiseTexture"].SetValue(tsorcRevamp.NoiseWavy);
            effect.Parameters["length"].SetValue(MathF.Max(32f, trailCurrentLength));
            float pixelBlocksX = MathF.Max(1f, trailCurrentLength / 2f);
            float pixelBlocksY = MathF.Max(1f, trailWidth / 2f);
            effect.Parameters["PixelGrid"]?.SetValue(
                new Vector4(pixelBlocksX, pixelBlocksY, 1f / pixelBlocksX, 1f / pixelBlocksY));

            float velLen = Projectile.velocity.Length();
            float step = Main.gamePaused ? 0f : 0.0008f * MathF.Max(4f, velLen);
            samplePointOffset1.X = (samplePointOffset1.X + step * 2.5f) % 1f;
            samplePointOffset1.Y = (samplePointOffset1.Y - 0.001f + 1f) % 1f;
            samplePointOffset2.X = (samplePointOffset2.X + step * 3.1f) % 1f;
            samplePointOffset2.Y = (samplePointOffset2.Y + 0.001f) % 1f;

            effect.Parameters["samplePointOffset1"].SetValue(samplePointOffset1);
            effect.Parameters["samplePointOffset2"].SetValue(samplePointOffset2);
            effect.Parameters["fadeOut"].SetValue(fadeOut);
            effect.Parameters["speed"].SetValue(velLen);
            effect.Parameters["time"].SetValue(Main.GlobalTimeWrappedHourly);
            effect.Parameters["shaderColor"].SetValue(new Color(0.24f, 0.52f, 1.0f, 1.0f).ToVector4());
            effect.Parameters["secondaryColor"].SetValue(new Color(1.0f, 0.85f, 0.25f, 1.0f).ToVector4());
            effect.Parameters["WorldViewProjection"].SetValue(GetWorldViewProjectionMatrix());
        }

        public override bool PreDraw(ref Color lightColor)
        {
            base.PreDraw(ref lightColor);

            // DynamicTrail runs PreDraw twice: additive pass renders the comet ribbon, normal pass renders the star core.
            if (additiveContext)
            {
                return false;
            }

            float spawnScale = MathHelper.Clamp(ageTicks / (float)SpawnFadeTicks, 0f, 1f);
            Texture2D starTex = TextureAssets.Projectile[Projectile.type].Value;
            Vector2 origin = starTex.Size() * 0.5f;
            Vector2 drawPos = Projectile.Center - Main.screenPosition;
            float pulse = launchLocked && !launched ? 1.30f : 1.0f;
            float starRotation = Main.GlobalTimeWrappedHourly * 2.6f;

            // Soft additive cyan-gold halo behind the crisp single star core.
            Main.EntitySpriteDraw(
                starTex,
                drawPos,
                null,
                new Color(110, 200, 255, 0) * (0.70f * spawnScale),
                -starRotation * 0.6f,
                origin,
                1.65f * spawnScale * pulse,
                SpriteEffects.None,
                0);

            Color coreTint = Color.Lerp(Color.White, new Color(255, 232, 125), launchLocked ? 0.55f : 0.25f) * spawnScale;
            Main.EntitySpriteDraw(
                starTex,
                drawPos,
                null,
                coreTint,
                starRotation,
                origin,
                1.10f * spawnScale * pulse,
                SpriteEffects.None,
                0);
            return false;
        }
    }

    /// <summary>
    /// Halo of Judgment blade: manifests in a 5-blade seraphic crown behind Spirit of Khaios's wings
    /// with 25t converging cyan-gold dust, tracks the target only within its spawn-locked horizontal
    /// hemisphere (never flipping 180 degrees), glints clearly, then launches sequentially.
    /// ai[0] = owner SpiritOfKhaios whoAmI, ai[1] = signed slot ((1..5) * castFacingSide), ai[2] = launch tick.
    /// </summary>
    public class KhaiosHaloBlade : ModProjectile
    {
        private const int BirthTicks = 25;
        private const int GlintLeadTicks = 16;
        private const float LaunchSpeed = 16.5f;
        private const float MaxAimAngleFromHorizontal = 1.30f;

        private int ageTicks;
        private bool launched;
        private int lockedFacingSide;
        private Vector2 lockedAimDir = Vector2.UnitX;

        public override string Texture => UsefulFunctions.VanillaTextureFilepath(ProjectileID.SkyFracture, "Projectile");

        public override void SetDefaults()
        {
            Projectile.width = 22;
            Projectile.height = 22;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.tileCollide = false;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 480;
        }

        public override bool? CanDamage()
        {
            return launched;
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(launched);
            writer.Write((sbyte)lockedFacingSide);
            writer.WriteVector2(lockedAimDir);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            launched = reader.ReadBoolean();
            lockedFacingSide = reader.ReadSByte();
            lockedAimDir = reader.ReadVector2();
        }

        public override void AI()
        {
            ageTicks++;
            int ownerIndex = (int)Projectile.ai[0];
            int signedSlot = (int)Math.Round(Projectile.ai[1]);
            int slotIndex = Math.Clamp(Math.Abs(signedSlot) - 1, 0, 4);
            int launchTick = Math.Max(BirthTicks + GlintLeadTicks, (int)Math.Round(Projectile.ai[2]));

            if (lockedFacingSide == 0)
            {
                lockedFacingSide = signedSlot < 0 ? -1 : 1;
                lockedAimDir = new Vector2(lockedFacingSide, 0f);
            }

            bool ownerAlive = ownerIndex >= 0
                && ownerIndex < Main.maxNPCs
                && Main.npc[ownerIndex].active
                && Main.npc[ownerIndex].type == ModContent.NPCType<SpiritOfKhaios>();

            if (!launched)
            {
                if (!ownerAlive)
                {
                    Projectile.Kill();
                    return;
                }

                NPC owner = Main.npc[ownerIndex];
                // Fan 5 blades in an angelic halo arc behind and above the boss's shoulders.
                float fanFrac = (slotIndex - 2f) / 2f;
                float arcAngle = -MathHelper.PiOver2 + fanFrac * 1.15f;
                Vector2 haloOffset = arcAngle.ToRotationVector2() * 64f + new Vector2(-lockedFacingSide * 10f, -12f);
                Projectile.Center = Vector2.Lerp(Projectile.Center, owner.Center + haloOffset, 0.25f);
                Projectile.velocity = Vector2.Zero;

                Player target = (owner.target >= 0 && owner.target < Main.maxPlayers && Main.player[owner.target].active && !Main.player[owner.target].dead)
                    ? Main.player[owner.target]
                    : null;

                if (ageTicks < launchTick - GlintLeadTicks && target != null)
                {
                    Vector2 aimPoint = target.Center + target.velocity * 10f;
                    Vector2 toTarget = aimPoint - Projectile.Center;

                    // Track only while the player remains on this blade's locked horizontal side; if the player
                    // crosses behind the boss, keep the last forward-side aim so blades never flip 180 degrees.
                    if (toTarget.X * lockedFacingSide > 0f)
                    {
                        float forwardBaseAngle = lockedFacingSide > 0 ? 0f : MathF.PI;
                        float desiredAngle = toTarget.ToRotation();
                        float clampedOffset = MathHelper.Clamp(
                            MathHelper.WrapAngle(desiredAngle - forwardBaseAngle),
                            -MaxAimAngleFromHorizontal,
                            MaxAimAngleFromHorizontal);
                        lockedAimDir = (forwardBaseAngle + clampedOffset).ToRotationVector2();
                    }
                }

                Projectile.rotation = lockedAimDir.ToRotation() + MathHelper.PiOver4;

                if (!Main.dedServ)
                {
                    if (ageTicks <= BirthTicks && ageTicks % 2 == 0)
                    {
                        Vector2 offset = Main.rand.NextVector2CircularEdge(26f, 26f);
                        Dust birth = Dust.NewDustPerfect(
                            Projectile.Center + offset,
                            DustID.Electric,
                            -offset * 0.14f,
                            40,
                            Color.Cyan,
                            Main.rand.NextFloat(0.85f, 1.3f));
                        birth.noGravity = true;
                    }
                    else if (ageTicks == launchTick - GlintLeadTicks)
                    {
                        SoundEngine.PlaySound(
                            new SoundStyle("tsorcRevamp/Sounds/DST/DarkSwordSummon1") with { Volume = 0.34f, Pitch = 0.32f },
                            Projectile.Center);

                        // Bright white-gold glint burst on the blade itself when its aim locks before launch.
                        for (int i = 0; i < 10; i++)
                        {
                            Dust lockGlint = Dust.NewDustPerfect(
                                Projectile.Center + Main.rand.NextVector2Circular(10f, 10f),
                                i % 2 == 0 ? DustID.WhiteTorch : DustID.GoldFlame,
                                Main.rand.NextVector2Circular(3.2f, 3.2f),
                                0,
                                Color.White,
                                Main.rand.NextFloat(1.05f, 1.55f));
                            lockGlint.noGravity = true;
                        }
                    }
                }

                if (ageTicks >= launchTick)
                {
                    launched = true;
                    Projectile.tileCollide = true;
                    Projectile.timeLeft = 180;
                    Projectile.velocity = lockedAimDir * LaunchSpeed;
                    Projectile.netUpdate = true;

                    if (!Main.dedServ)
                    {
                        SoundEngine.PlaySound(
                            new SoundStyle("tsorcRevamp/Sounds/HollowKnight/hornet_dash") with { Volume = 0.34f, Pitch = 0.24f },
                            Projectile.Center);
                    }
                }

                return;
            }

            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver4;

            if (!Main.dedServ)
            {
                Lighting.AddLight(Projectile.Center, 0.4f, 0.8f, 1.0f);
                Dust trail = Dust.NewDustPerfect(
                    Projectile.Center,
                    Main.rand.NextBool(3) ? DustID.GoldFlame : DustID.Electric,
                    -Projectile.velocity * 0.14f,
                    50,
                    Color.Cyan,
                    Main.rand.NextFloat(0.9f, 1.35f));
                trail.noGravity = true;
            }
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.dedServ)
            {
                return;
            }

            SoundEngine.PlaySound(SoundID.Item27 with { Volume = 0.30f, Pitch = 0.28f }, Projectile.Center);

            // Prominent oversized pure-white shatter motes so every expiring sword reads clearly in dark arenas.
            for (int i = 0; i < 14; i++)
            {
                Dust whiteCore = Dust.NewDustPerfect(
                    Projectile.Center + Main.rand.NextVector2Circular(10f, 10f),
                    DustID.WhiteTorch,
                    Main.rand.NextVector2Circular(6.2f, 6.2f),
                    0,
                    Color.White,
                    Main.rand.NextFloat(1.75f, 2.25f));
                whiteCore.noGravity = true;
            }

            // Secondary bright silver-white shatter shards.
            for (int i = 0; i < 12; i++)
            {
                Dust silverShard = Dust.NewDustPerfect(
                    Projectile.Center + Main.rand.NextVector2Circular(8f, 8f),
                    DustID.SilverFlame,
                    Main.rand.NextVector2Circular(5.4f, 5.4f),
                    10,
                    Color.White,
                    Main.rand.NextFloat(1.55f, 2.05f));
                silverShard.noGravity = true;
            }

            // Crisp radiant white sparkle glints.
            for (int i = 0; i < 12; i++)
            {
                Dust glint = Dust.NewDustPerfect(
                    Projectile.Center,
                    DustID.AncientLight,
                    Main.rand.NextVector2Circular(7.2f, 7.2f),
                    0,
                    Color.White,
                    Main.rand.NextFloat(1.15f, 1.65f));
                glint.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            float birthScale = MathHelper.Clamp(ageTicks / (float)BirthTicks, 0f, 1f);
            int launchTick = Math.Max(BirthTicks + GlintLeadTicks, (int)Math.Round(Projectile.ai[2]));
            bool inGlint = !launched && ageTicks >= launchTick - GlintLeadTicks;

            Texture2D bladeTex = TextureAssets.Projectile[Projectile.type].Value;
            Color tint = Color.Lerp(Color.White, new Color(255, 230, 130), inGlint ? 0.55f : 0.28f) * birthScale;
            float drawScale = birthScale * (inGlint ? 1.26f : 1.15f);

            Main.EntitySpriteDraw(
                bladeTex,
                Projectile.Center - Main.screenPosition,
                null,
                tint,
                Projectile.rotation,
                bladeTex.Size() * 0.5f,
                drawScale,
                SpriteEffects.None,
                0);
            return false;
        }
    }

    /// <summary>
    /// Seraphic Afterimage Echo left behind at Spirit of Khaios's prior perch during Split-Shots
    /// and aerial blinks. Shows a translucent golden-white silhouette + aim sightline, fires one
    /// delayed cross-angle KhaiosThunderArrow, then fades cleanly with spirit dust.
    /// ai[0] = target player whoAmI.
    /// </summary>
    public class KhaiosSeraphEcho : ModProjectile
    {
        private const int TotalLifetimeTicks = 60;
        private const int FireTick = 42;

        private int elapsedTicks;
        private Vector2 lockedAimDir = Vector2.UnitX;

        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(SpiritOfKhaios));

        public override void SetDefaults()
        {
            Projectile.width = 28;
            Projectile.height = 46;
            Projectile.hostile = false;
            Projectile.friendly = false;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = TotalLifetimeTicks;
        }

        public override bool? CanDamage() => false;

        public override void AI()
        {
            elapsedTicks++;
            Projectile.velocity = Vector2.Zero;

            int targetIndex = (int)Projectile.ai[0];
            Player target = (targetIndex >= 0 && targetIndex < Main.maxPlayers && Main.player[targetIndex].active && !Main.player[targetIndex].dead)
                ? Main.player[targetIndex]
                : null;

            if (elapsedTicks < FireTick - 14 && target != null)
            {
                lockedAimDir = (target.Center + target.velocity * 12f - Projectile.Center).SafeNormalize(Vector2.UnitX);
            }

            if (elapsedTicks == 1 && !Main.dedServ)
            {
                SoundEngine.PlaySound(
                    new SoundStyle("tsorcRevamp/Sounds/Runeterra/Shunpo2") with { Volume = 0.42f, Pitch = 0.12f },
                    Projectile.Center);
            }

            if (elapsedTicks == FireTick)
            {
                if (!Main.dedServ)
                {
                    SoundEngine.PlaySound(SoundID.Item5 with { Volume = 0.52f, Pitch = 0.08f }, Projectile.Center);
                    SoundEngine.PlaySound(SoundID.Item94 with { Volume = 0.28f, Pitch = 0.35f }, Projectile.Center);
                }

                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Projectile.NewProjectile(
                        Projectile.GetSource_FromThis(),
                        Projectile.Center,
                        lockedAimDir * KhaiosThunderArrow.LaunchSpeed,
                        ModContent.ProjectileType<KhaiosThunderArrow>(),
                        Projectile.damage,
                        2f,
                        Main.myPlayer);
                }
            }

            if (!Main.dedServ)
            {
                if (elapsedTicks >= 16 && elapsedTicks < FireTick)
                {
                    // Converging golden-cyan bow sparks replace any straight sightline before the echo fires.
                    Vector2 sparkOffset = Main.rand.NextVector2CircularEdge(28f, 28f);
                    Dust aimSpark = Dust.NewDustPerfect(
                        Projectile.Center + lockedAimDir * 14f + sparkOffset,
                        elapsedTicks % 2 == 0 ? DustID.GoldFlame : DustID.Electric,
                        -sparkOffset * 0.14f,
                        20,
                        new Color(255, 232, 120),
                        Main.rand.NextFloat(0.95f, 1.45f));
                    aimSpark.noGravity = true;
                }
                else if (elapsedTicks % 3 == 0)
                {
                    // dust-lint: ok (stationary spirit echo ambient motes)
                    Dust mote = Dust.NewDustPerfect(
                        Projectile.Center + Main.rand.NextVector2Circular(16f, 22f),
                        Main.rand.NextBool(3) ? DustID.GoldFlame : DustID.Ghost,
                        new Vector2(0f, Main.rand.NextFloat(-1.4f, -0.3f)),
                        70,
                        new Color(255, 232, 130),
                        Main.rand.NextFloat(0.85f, 1.35f));
                    mote.noGravity = true;
                }
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            float fadeIn = MathHelper.Clamp(elapsedTicks / 12f, 0f, 1f);
            float fadeOut = MathHelper.Clamp((TotalLifetimeTicks - elapsedTicks) / 14f, 0f, 1f);
            float alpha = fadeIn * fadeOut * 0.72f;

            Texture2D spiritTex = TextureAssets.Projectile[Projectile.type].Value;
            SpriteEffects flip = lockedAimDir.X < 0f ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            Color ghostTint = new Color(255, 238, 165) * alpha;

            Main.EntitySpriteDraw(
                spiritTex,
                Projectile.Center - Main.screenPosition,
                null,
                ghostTint,
                0f,
                spiritTex.Size() * 0.5f,
                1.05f,
                flip,
                0);
            return false;
        }
    }

    /// <summary>
    /// Spirit of Khaios's Paladin's Shield Consecrated Light beam (uses EnemyConsecratedLight.png).
    /// 60t growing golden 1x3-tile charge at the shield (no straight telegraph line) that locks aim
    /// 20t before firing, then fires for 120t (2 seconds) while slowly sweeping toward the player.
    /// Stops at 100 tiles (1600px), the first solid tile, or a living player, erupting with heavy
    /// golden-white dusts at origin, along the shaft, and continuously on impact.
    /// ai[0] = owner SpiritOfKhaios whoAmI.
    /// </summary>
    public class KhaiosShieldLightBeam : ModProjectile
    {
        public const int ChargeTicks = 60;
        public const int LockLeadTicks = 20;
        public const int FiringTicks = 120;
        public const int FadeOutTicks = 10;
        public const int TotalDurationTicks = ChargeTicks + FiringTicks + FadeOutTicks;

        private const float MaxRangePixels = 100f * 16f;
        private const float SweepTurnRateRadPerTick = 0.0145f;
        private const float ChargeAimTurnRateRadPerTick = 0.14f;
        private const float RayStepPixels = 8f;
        private const float BeamHitWidthPixels = 22f;

        private static readonly Rectangle HeadRect = new Rectangle(0, 0, 30, 24);
        private static readonly Rectangle BodyRect = new Rectangle(0, 26, 30, 30);
        private static readonly Rectangle TailRect = new Rectangle(0, 58, 30, 24);

        private int elapsedTicks;
        private float beamAngle;
        private bool aimInitialized;
        private float currentLength = 64f;
        private bool hitSomething;

        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(EnemyConsecratedLight));

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.DrawScreenCheckFluff[Projectile.type] = 2400;
        }

        public override void SetDefaults()
        {
            Projectile.width = 24;
            Projectile.height = 24;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = TotalDurationTicks + 8;
        }

        public override bool? CanDamage()
        {
            return elapsedTicks >= ChargeTicks && elapsedTicks < ChargeTicks + FiringTicks;
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(elapsedTicks);
            writer.Write(beamAngle);
            writer.Write(aimInitialized);
            writer.Write(currentLength);
            writer.Write(hitSomething);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            elapsedTicks = reader.ReadInt32();
            beamAngle = reader.ReadSingle();
            aimInitialized = reader.ReadBoolean();
            currentLength = reader.ReadSingle();
            hitSomething = reader.ReadBoolean();
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (CanDamage() != true)
            {
                return false;
            }

            Vector2 origin = Projectile.Center;
            Vector2 beamDir = beamAngle.ToRotationVector2();
            // Extend collision segment slightly past the visual truncation so stopping on the player still registers hits.
            Vector2 end = origin + beamDir * (currentLength + 14f);
            float collisionPoint = 0f;
            return Collision.CheckAABBvLineCollision(
                targetHitbox.TopLeft(),
                targetHitbox.Size(),
                origin,
                end,
                BeamHitWidthPixels,
                ref collisionPoint);
        }

        public override void AI()
        {
            int ownerIndex = (int)Projectile.ai[0];
            bool ownerAlive = ownerIndex >= 0
                && ownerIndex < Main.maxNPCs
                && Main.npc[ownerIndex].active
                && Main.npc[ownerIndex].type == ModContent.NPCType<SpiritOfKhaios>();

            if (!ownerAlive)
            {
                Projectile.Kill();
                return;
            }

            NPC owner = Main.npc[ownerIndex];
            Player target = GetValidTarget(owner);

            // Anchor directly at Spirit of Khaios's raised Paladin's Shield front.
            int facingSide = owner.direction >= 0 ? 1 : -1;
            Vector2 shieldOrigin = owner.Center + new Vector2(facingSide * 22f, -2f);
            Projectile.Center = shieldOrigin;
            Projectile.velocity = Vector2.Zero;

            if (!aimInitialized)
            {
                aimInitialized = true;
                if (target != null)
                {
                    beamAngle = (target.Center - shieldOrigin).SafeNormalize(new Vector2(facingSide, 0f)).ToRotation();
                }
                else
                {
                    beamAngle = facingSide > 0 ? 0f : MathF.PI;
                }
            }

            elapsedTicks++;

            // Phase 1: 60-tick growing golden 1x3-tile charge at shield point; locks aim 20t before firing.
            if (elapsedTicks < ChargeTicks)
            {
                bool beforeLockWindow = elapsedTicks < ChargeTicks - LockLeadTicks;
                if (beforeLockWindow && target != null && Main.netMode != NetmodeID.MultiplayerClient)
                {
                    float desiredAngle = (target.Center - shieldOrigin).SafeNormalize(new Vector2(facingSide, 0f)).ToRotation();
                    float step = MathHelper.Clamp(MathHelper.WrapAngle(desiredAngle - beamAngle), -ChargeAimTurnRateRadPerTick, ChargeAimTurnRateRadPerTick);
                    beamAngle = MathHelper.WrapAngle(beamAngle + step);
                    owner.direction = MathF.Cos(beamAngle) >= 0f ? 1 : -1;
                }

                if (elapsedTicks == ChargeTicks - LockLeadTicks)
                {
                    if (Main.netMode != NetmodeID.MultiplayerClient)
                    {
                        Projectile.netUpdate = true;
                    }

                    if (!Main.dedServ)
                    {
                        SoundEngine.PlaySound(SoundID.Item29 with { Volume = 0.36f, Pitch = 0.08f }, shieldOrigin);
                        for (int i = 0; i < 16; i++)
                        {
                            Vector2 burstPos = shieldOrigin + new Vector2(Main.rand.NextFloat(-8f, 8f), Main.rand.NextFloat(-24f, 24f));
                            Dust lockFlash = Dust.NewDustPerfect(
                                burstPos,
                                i % 2 == 0 ? DustID.WhiteTorch : DustID.GoldFlame,
                                Main.rand.NextVector2Circular(4.2f, 4.2f),
                                0,
                                Color.White,
                                Main.rand.NextFloat(1.1f, 1.75f));
                            lockFlash.noGravity = true;
                        }
                    }
                }

                EmitShieldChargeDust(shieldOrigin, elapsedTicks / (float)ChargeTicks);
                return;
            }

            // Ignition moment on tick 60.
            if (elapsedTicks == ChargeTicks)
            {
                if (Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Projectile.netUpdate = true;
                }

                if (!Main.dedServ)
                {
                    SoundEngine.PlaySound(SoundID.Item122 with { Volume = 0.58f, Pitch = 0.22f }, shieldOrigin);
                    SoundEngine.PlaySound(SoundID.Item74 with { Volume = 0.32f, Pitch = 0.12f }, shieldOrigin);
                    UsefulFunctions.ScreenShake(shieldOrigin, 4.5f, 14, distanceFalloff: 850f);
                }
            }

            // Phase 2: 2-second active beam slowly rotating toward the player.
            bool isFiring = elapsedTicks < ChargeTicks + FiringTicks;
            if (isFiring && target != null && Main.netMode != NetmodeID.MultiplayerClient)
            {
                float desiredAngle = (target.Center - shieldOrigin).SafeNormalize(Vector2.UnitX).ToRotation();
                float turnStep = MathHelper.Clamp(MathHelper.WrapAngle(desiredAngle - beamAngle), -SweepTurnRateRadPerTick, SweepTurnRateRadPerTick);
                beamAngle = MathHelper.WrapAngle(beamAngle + turnStep);
                owner.direction = MathF.Cos(beamAngle) >= 0f ? 1 : -1;

                if (elapsedTicks % 8 == 0)
                {
                    Projectile.netUpdate = true;
                }
            }

            // Keep shieldOrigin aligned if owner.direction flipped during the sweep.
            int liveFacing = owner.direction >= 0 ? 1 : -1;
            shieldOrigin = owner.Center + new Vector2(liveFacing * 22f, -2f);
            Projectile.Center = shieldOrigin;

            Vector2 beamDir = beamAngle.ToRotationVector2();
            SolveBeamTermination(shieldOrigin, beamDir, out currentLength, out hitSomething);

            if (!Main.dedServ && isFiring)
            {
                EmitActiveBeamDust(shieldOrigin, beamDir, currentLength, hitSomething);
            }

            if (elapsedTicks >= TotalDurationTicks)
            {
                Projectile.Kill();
            }
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(BuffID.OnFire3, 180);
        }

        private static Player GetValidTarget(NPC owner)
        {
            if (owner.target >= 0 && owner.target < Main.maxPlayers)
            {
                Player target = Main.player[owner.target];
                if (target.active && !target.dead)
                {
                    return target;
                }
            }

            return null;
        }

        private static void SolveBeamTermination(Vector2 origin, Vector2 beamDir, out float beamLength, out bool stoppedOnTarget)
        {
            float solidStopDist = MaxRangePixels;
            bool hitTile = false;

            for (float dist = RayStepPixels; dist <= MaxRangePixels; dist += RayStepPixels)
            {
                Vector2 sample = origin + beamDir * dist;
                int tileX = (int)(sample.X / 16f);
                int tileY = (int)(sample.Y / 16f);

                if (tileX < 2 || tileX >= Main.maxTilesX - 2 || tileY < 2 || tileY >= Main.maxTilesY - 2)
                {
                    solidStopDist = dist;
                    hitTile = true;
                    break;
                }

                Tile tile = Framing.GetTileSafely(tileX, tileY);
                if (tile.HasUnactuatedTile && Main.tileSolid[tile.TileType] && !Main.tileSolidTop[tile.TileType])
                {
                    solidStopDist = dist;
                    hitTile = true;
                    break;
                }
            }

            // Also stop the beam right at the nearest living player it intersects.
            float bestDist = solidStopDist;
            bool hitPlayer = false;

            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player player = Main.player[i];
                if (!player.active || player.dead)
                {
                    continue;
                }

                Rectangle hitBox = player.Hitbox;
                hitBox.Inflate(8, 8);
                float hitDist = RayIntersectRectangle(origin, beamDir, bestDist, hitBox);
                if (hitDist >= 0f && hitDist < bestDist)
                {
                    bestDist = hitDist;
                    hitPlayer = true;
                }
            }

            beamLength = Math.Max(20f, bestDist);
            stoppedOnTarget = hitTile || hitPlayer;
        }

        private static float RayIntersectRectangle(Vector2 origin, Vector2 dir, float maxDist, Rectangle rect)
        {
            float tMin = 0f;
            float tMax = maxDist;

            if (MathF.Abs(dir.X) < 0.0001f)
            {
                if (origin.X < rect.Left || origin.X > rect.Right)
                {
                    return -1f;
                }
            }
            else
            {
                float invX = 1f / dir.X;
                float nearHit = (rect.Left - origin.X) * invX;
                float farHit = (rect.Right - origin.X) * invX;
                if (nearHit > farHit)
                {
                    (nearHit, farHit) = (farHit, nearHit);
                }

                tMin = Math.Max(tMin, nearHit);
                tMax = Math.Min(tMax, farHit);
                if (tMin > tMax)
                {
                    return -1f;
                }
            }

            if (MathF.Abs(dir.Y) < 0.0001f)
            {
                if (origin.Y < rect.Top || origin.Y > rect.Bottom)
                {
                    return -1f;
                }
            }
            else
            {
                float invY = 1f / dir.Y;
                float nearHit = (rect.Top - origin.Y) * invY;
                float farHit = (rect.Bottom - origin.Y) * invY;
                if (nearHit > farHit)
                {
                    (nearHit, farHit) = (farHit, nearHit);
                }

                tMin = Math.Max(tMin, nearHit);
                tMax = Math.Min(tMax, farHit);
                if (tMin > tMax)
                {
                    return -1f;
                }
            }

            return tMin;
        }

        private void EmitShieldChargeDust(Vector2 shieldOrigin, float progress)
        {
            if (Main.dedServ)
            {
                return;
            }

            Lighting.AddLight(shieldOrigin, 0.95f * (0.4f + progress * 0.8f), 0.78f * (0.4f + progress * 0.8f), 0.28f);

            // Growing 60-tick golden dust spread out across 1x3 tiles (16px wide x 48px tall) right at the shield.
            int dustCount = 3 + (int)(progress * 9f);
            float scaleMin = MathHelper.Lerp(0.85f, 1.35f, progress);
            float scaleMax = MathHelper.Lerp(1.25f, 1.95f, progress);

            for (int i = 0; i < dustCount; i++)
            {
                Vector2 boxOffset = new Vector2(Main.rand.NextFloat(-8f, 8f), Main.rand.NextFloat(-24f, 24f));
                Vector2 inwardVel = -boxOffset * MathHelper.Lerp(0.03f, 0.09f, progress) + Main.rand.NextVector2Circular(0.8f, 0.8f);

                int dustType = DustID.GoldFlame;
                Color dustColor = new Color(255, 224, 90);
                if (progress > 0.55f && i % 4 == 0)
                {
                    dustType = DustID.WhiteTorch;
                    dustColor = Color.White;
                }
                else if (progress > 0.35f && i % 5 == 0)
                {
                    dustType = DustID.AncientLight;
                    dustColor = new Color(255, 245, 190);
                }

                Dust chargeDust = Dust.NewDustPerfect(
                    shieldOrigin + boxOffset,
                    dustType,
                    inwardVel,
                    (int)MathHelper.Lerp(35f, 0f, progress),
                    dustColor,
                    Main.rand.NextFloat(scaleMin, scaleMax));
                chargeDust.noGravity = true;
            }
        }

        private void EmitActiveBeamDust(Vector2 origin, Vector2 beamDir, float beamLength, bool hitSurface)
        {
            Vector2 perpDir = new Vector2(-beamDir.Y, beamDir.X);
            Vector2 impactPos = origin + beamDir * beamLength;

            DelegateMethods.v3_1 = new Vector3(1.0f, 0.86f, 0.36f);
            Utils.PlotTileLine(origin, impactPos, 24f, DelegateMethods.CastLight);

            // Heavy radiant origin dusts erupting from the shield point (1x3 tile spread).
            for (int i = 0; i < 7; i++)
            {
                int dustType = DustID.GoldFlame;
                Color tint = new Color(255, 225, 95);
                if (i == 0 || i == 3)
                {
                    dustType = DustID.WhiteTorch;
                    tint = Color.White;
                }
                else if (i == 6)
                {
                    dustType = DustID.AncientLight;
                    tint = new Color(255, 245, 195);
                }

                Vector2 spawnOffset = new Vector2(Main.rand.NextFloat(-8f, 8f), Main.rand.NextFloat(-22f, 22f));
                Vector2 originVel = beamDir * Main.rand.NextFloat(2.5f, 7.5f) + Main.rand.NextVector2Circular(2.4f, 2.4f);
                Dust originDust = Dust.NewDustPerfect(
                    origin + spawnOffset,
                    dustType,
                    originVel,
                    10,
                    tint,
                    Main.rand.NextFloat(1.05f, 1.75f));
                originDust.noGravity = true;
            }

            // Luminous dusts streaming along the full active shaft.
            int shaftDustCount = Math.Clamp((int)(beamLength / 52f), 4, 22);
            for (int i = 0; i < shaftDustCount; i++)
            {
                float distAlong = Main.rand.NextFloat(16f, Math.Max(20f, beamLength - 8f));
                float lateralOffset = Main.rand.NextFloat(-11f, 11f);
                Vector2 shaftPos = origin + beamDir * distAlong + perpDir * lateralOffset;
                Vector2 shaftVel = beamDir * Main.rand.NextFloat(1.8f, 4.6f) + perpDir * (lateralOffset * 0.12f);

                int dustType = DustID.GoldFlame;
                Color tint = new Color(255, 225, 95);
                if (i % 3 == 0)
                {
                    dustType = DustID.WhiteTorch;
                    tint = Color.White;
                }
                else if (i % 5 == 0)
                {
                    dustType = DustID.AncientLight;
                    tint = new Color(255, 248, 200);
                }

                Dust shaftDust = Dust.NewDustPerfect(
                    shaftPos,
                    dustType,
                    shaftVel,
                    15,
                    tint,
                    Main.rand.NextFloat(0.85f, 1.45f));
                shaftDust.noGravity = true;
            }

            // Constant heavy golden-white impact spray whenever the beam is searing a tile or player.
            if (hitSurface)
            {
                Lighting.AddLight(impactPos, 1.15f, 0.94f, 0.45f);

                for (int i = 0; i < 10; i++)
                {
                    int dustType = DustID.GoldFlame;
                    Color tint = new Color(255, 225, 95);
                    bool useGravity = i >= 8;

                    if (i < 3)
                    {
                        dustType = DustID.WhiteTorch;
                        tint = Color.White;
                    }
                    else if (i == 9)
                    {
                        dustType = DustID.AncientLight;
                        tint = Color.White;
                    }

                    Vector2 impactVel = -beamDir * Main.rand.NextFloat(1.5f, 4.5f)
                        + Main.rand.NextVector2Circular(6.5f, 6.5f);
                    if (useGravity)
                    {
                        impactVel.Y -= 2.2f;
                    }

                    Dust impactDust = Dust.NewDustPerfect(
                        impactPos + Main.rand.NextVector2Circular(10f, 10f),
                        dustType,
                        impactVel,
                        5,
                        tint,
                        Main.rand.NextFloat(1.1f, 1.85f));
                    impactDust.noGravity = !useGravity;
                }
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (elapsedTicks < ChargeTicks)
            {
                return false;
            }

            Texture2D texture = TextureAssets.Projectile[Projectile.type].Value;
            Vector2 beamDir = beamAngle.ToRotationVector2();
            // EnemyConsecratedLight.png points downward (+Y), so subtract Pi/2 to align with beamDir.
            float drawRotation = beamAngle - MathHelper.PiOver2;
            Vector2 originScreen = Projectile.Center - Main.screenPosition;

            int fireAge = elapsedTicks - ChargeTicks;
            float widthFactor = 1f;
            float alphaFactor = 1f;
            if (fireAge < 6)
            {
                widthFactor = MathHelper.Lerp(0.35f, 1.0f, fireAge / 6f);
            }
            else if (fireAge >= FiringTicks)
            {
                float fadeProgress = MathHelper.Clamp((fireAge - FiringTicks) / (float)FadeOutTicks, 0f, 1f);
                widthFactor = MathHelper.Lerp(1.0f, 0.1f, fadeProgress);
                alphaFactor = MathHelper.Lerp(1.0f, 0f, fadeProgress);
            }

            float pulse = 1.0f + 0.08f * MathF.Sin(Main.GlobalTimeWrappedHourly * 38f);
            float coreWidth = 1.08f * widthFactor * pulse;
            float glowWidth = 1.52f * widthFactor * pulse;

            DrawBeamPass(texture, originScreen, beamDir, drawRotation, glowWidth, new Color(255, 215, 90, 0) * (0.58f * alphaFactor));
            DrawBeamPass(texture, originScreen, beamDir, drawRotation, coreWidth, Color.White * alphaFactor);
            return false;
        }

        private void DrawBeamPass(
            Texture2D texture,
            Vector2 originScreen,
            Vector2 beamDir,
            float drawRotation,
            float widthScale,
            Color tint)
        {
            Vector2 headOrigin = new Vector2(HeadRect.Width * 0.5f, HeadRect.Height * 0.5f);
            Main.EntitySpriteDraw(
                texture,
                originScreen,
                HeadRect,
                tint,
                drawRotation,
                headOrigin,
                new Vector2(widthScale, 1.0f),
                SpriteEffects.None,
                0);

            float bodyStart = 12f;
            float bodyEnd = Math.Max(bodyStart, currentLength - 12f);
            float segmentStep = BodyRect.Height;
            Vector2 bodyOrigin = new Vector2(BodyRect.Width * 0.5f, 0f);

            for (float dist = bodyStart; dist < bodyEnd; dist += segmentStep)
            {
                float remaining = bodyEnd - dist;
                int sliceHeight = remaining < segmentStep ? Math.Max(1, (int)MathF.Ceiling(remaining)) : BodyRect.Height;
                Rectangle sliceRect = new Rectangle(BodyRect.X, BodyRect.Y, BodyRect.Width, sliceHeight);

                Main.EntitySpriteDraw(
                    texture,
                    originScreen + beamDir * dist,
                    sliceRect,
                    tint,
                    drawRotation,
                    bodyOrigin,
                    new Vector2(widthScale, 1.0f),
                    SpriteEffects.None,
                    0);
            }

            Vector2 tailOrigin = new Vector2(TailRect.Width * 0.5f, TailRect.Height * 0.75f);
            Main.EntitySpriteDraw(
                texture,
                originScreen + beamDir * currentLength,
                TailRect,
                tint,
                drawRotation,
                tailOrigin,
                new Vector2(widthScale, 1.0f),
                SpriteEffects.None,
                0);
        }
    }
}

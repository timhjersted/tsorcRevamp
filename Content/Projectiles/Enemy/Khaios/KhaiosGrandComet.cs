using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Content.Items.Weapons.Summon.Runeterra.AurelionSol.Projectiles.Dragons.StarForger.GrandComet;
using tsorcRevamp.Content.Projectiles.Enemy;
using tsorcRevamp.Content.Projectiles.VFX;

namespace tsorcRevamp.Content.Projectiles.Enemy.Khaios
{
    /// <summary>
    /// Stage 1 &amp; 3 of the Phase 1 StarForger dragon's Grand Comet Call:
    /// Telegraphs the impact zone for 80 ticks using GrandCometZone's 3-spiral art + vibrant swirling
    /// cosmic dust arms, then on meteor impact expands a pixelated CataluminanceTrail shockwave ring
    /// with true annulus collision (inside-safe and roll-through-able).
    /// ai[0] = 0 while telegraphing, 1 once detonated.
    /// </summary>
    public class KhaiosGrandCometZone : DynamicTrail
    {
        public const int TelegraphTicks = 80;
        private const int ShockwaveDurationTicks = 96;
        private const float MaxShockwaveRadius = 540f;
        private const float AnnulusHalfThickness = 22f;

        private float shockwaveRadius;
        private int shockwaveAge;
        private bool detonated;
        private int telegraphElapsed;
        private float spiralAngle;

        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(GrandCometZone));

        public override void SetStaticDefaults()
        {
            base.SetStaticDefaults();
        }

        public override void SetDefaults()
        {
            Projectile.width = 350;
            Projectile.height = 350;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = TelegraphTicks + ShockwaveDurationTicks + 40;

            trailWidth = 28;
            trailPointLimit = 180;
            trailMaxLength = 9999999f;
            collisionPadding = 0;
            NPCSource = false;
            trailCollision = false;
            normalCollision = false;
            noFadeOut = true;
            customEffect = ModContent.Request<Effect>(
                "tsorcRevamp/Effects/CataluminanceTrailPixelated",
                AssetRequestMode.ImmediateLoad).Value;
        }

        public override bool? CanDamage()
        {
            return detonated && shockwaveAge > 2 && shockwaveAge < ShockwaveDurationTicks - 14;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (!detonated || shockwaveAge <= 2 || shockwaveAge >= ShockwaveDurationTicks - 14)
            {
                return false;
            }

            Vector2 center = Projectile.Center;
            Vector2 closest = Vector2.Clamp(
                center,
                new Vector2(targetHitbox.Left, targetHitbox.Top),
                new Vector2(targetHitbox.Right, targetHitbox.Bottom));
            float minDist = Vector2.Distance(center, closest);

            float farX = Math.Abs(targetHitbox.Left - center.X) > Math.Abs(targetHitbox.Right - center.X)
                ? targetHitbox.Left
                : targetHitbox.Right;
            float farY = Math.Abs(targetHitbox.Top - center.Y) > Math.Abs(targetHitbox.Bottom - center.Y)
                ? targetHitbox.Top
                : targetHitbox.Bottom;
            float maxDist = Vector2.Distance(center, new Vector2(farX, farY));

            bool touchesOuter = minDist <= shockwaveRadius + AnnulusHalfThickness;
            bool outsideInner = maxDist >= Math.Max(0f, shockwaveRadius - AnnulusHalfThickness);
            return touchesOuter && outsideInner;
        }

        public override void AI()
        {
            Projectile.velocity = Vector2.Zero;

            if (!detonated && Projectile.ai[0] >= 1f)
            {
                BeginShockwave();
            }

            if (!detonated)
            {
                telegraphElapsed++;
                float progress = MathHelper.Clamp(telegraphElapsed / (float)TelegraphTicks, 0f, 1f);
                spiralAngle += MathHelper.Lerp(0.025f, 0.095f, progress);

                if (!Main.dedServ)
                {
                    Lighting.AddLight(Projectile.Center, 0.45f * progress, 0.50f * progress, 1.0f * progress);

                    // 3 luminous swirling arms spiraling inward to match the 3-orb GrandCometZone graphic.
                    float armRadius = MathHelper.Lerp(155f, 22f, progress);
                    for (int i = 0; i < 6; i++)
                    {
                        int armIndex = i % 3;
                        float angle = spiralAngle + armIndex * (MathHelper.TwoPi / 3f) + Main.rand.NextFloat(-0.22f, 0.22f);
                        float radialJitter = armRadius * Main.rand.NextFloat(0.78f, 1.12f);
                        Vector2 offset = angle.ToRotationVector2() * radialJitter;
                        Vector2 tangentDir = (angle + MathHelper.PiOver2).ToRotationVector2();
                        Vector2 inwardDir = -offset.SafeNormalize(Vector2.UnitY);
                        Vector2 swirlVel = inwardDir * Main.rand.NextFloat(2.4f, 4.6f) + tangentDir * 1.4f;

                        int dustType = DustID.PurpleTorch;
                        Color dustColor = Color.Violet;
                        if (armIndex == 0)
                        {
                            dustType = DustID.GoldFlame;
                            dustColor = new Color(255, 220, 95);
                        }
                        else if (armIndex == 1)
                        {
                            dustType = DustID.BlueTorch;
                            dustColor = Color.DeepSkyBlue;
                        }

                        if (i >= 3)
                        {
                            dustType = i == 3 ? DustID.AncientLight : DustID.IceTorch;
                            dustColor = Color.LightCyan;
                        }

                        Dust spiralDust = Dust.NewDustPerfect(
                            Projectile.Center + offset,
                            dustType,
                            swirlVel,
                            20,
                            dustColor,
                            Main.rand.NextFloat(0.85f, 1.45f));
                        spiralDust.noGravity = true;
                    }

                    // Intensifying focal core spark flash in the final 24 ticks before impact.
                    if (telegraphElapsed >= TelegraphTicks - 24)
                    {
                        Dust coreFlash = Dust.NewDustPerfect(
                            Projectile.Center + Main.rand.NextVector2Circular(18f, 18f),
                            DustID.Electric,
                            Main.rand.NextVector2Circular(2.4f, 2.4f),
                            0,
                            Color.Cyan,
                            Main.rand.NextFloat(0.8f, 1.3f));
                        coreFlash.noGravity = true;
                    }
                }

                // Fallback detonation if the meteor somehow failed to reach the zone.
                if (telegraphElapsed >= TelegraphTicks + 20)
                {
                    BeginShockwave();
                }

                return;
            }

            shockwaveAge++;
            float ringProgress = MathHelper.Clamp(shockwaveAge / (float)ShockwaveDurationTicks, 0f, 1f);
            shockwaveRadius = MathHelper.Lerp(28f, MaxShockwaveRadius, ringProgress);
            fadeOut = ringProgress > 0.72f
                ? MathHelper.Clamp(1f - (ringProgress - 0.72f) / 0.28f, 0f, 1f)
                : 0.95f;

            RebuildRingTrail(shockwaveRadius);

            if (!Main.dedServ && fadeOut > 0.18f)
            {
                // Vibrant electric/azure/gold sparks riding the perimeter of the expanding shockwave ring.
                for (int i = 0; i < 8; i++)
                {
                    float angle = Main.rand.NextFloat(MathHelper.TwoPi);
                    Vector2 radialDir = angle.ToRotationVector2();
                    Vector2 tangentDir = (angle + MathHelper.PiOver2).ToRotationVector2();
                    Vector2 ringPos = Projectile.Center + radialDir * (shockwaveRadius + Main.rand.NextFloat(-12f, 12f));
                    Vector2 sparkVel = radialDir * Main.rand.NextFloat(2.2f, 4.6f) + tangentDir * Main.rand.NextFloat(-1.6f, 1.6f);

                    int dustType = DustID.IceTorch;
                    Color dustTint = Color.DeepSkyBlue;
                    if (i % 4 == 0)
                    {
                        dustType = DustID.GoldFlame;
                        dustTint = new Color(255, 225, 100);
                    }
                    else if (i % 4 == 1)
                    {
                        dustType = DustID.BlueTorch;
                        dustTint = Color.RoyalBlue;
                    }
                    else if (i % 4 == 2)
                    {
                        dustType = DustID.Electric;
                        dustTint = Color.Cyan;
                    }

                    Dust spark = Dust.NewDustPerfect(
                        ringPos,
                        dustType,
                        sparkVel,
                        20,
                        dustTint,
                        Main.rand.NextFloat(0.8f, 1.45f));
                    spark.noGravity = true;
                }
            }

            if (shockwaveAge >= ShockwaveDurationTicks)
            {
                Projectile.Kill();
            }
        }

        private void BeginShockwave()
        {
            detonated = true;
            shockwaveAge = 0;
            shockwaveRadius = 28f;
            fadeOut = 0.96f;
            Projectile.timeLeft = ShockwaveDurationTicks + 4;
            RebuildRingTrail(shockwaveRadius);
        }

        private void RebuildRingTrail(float radius)
        {
            const int ringPoints = 72;
            trailPositions ??= new List<Vector2>(ringPoints + 1);
            trailRotations ??= new List<float>(ringPoints + 1);
            trailPositions.Clear();
            trailRotations.Clear();

            for (int i = 0; i <= ringPoints; i++)
            {
                float angle = MathHelper.TwoPi * i / ringPoints;
                trailPositions.Add(Projectile.Center + angle.ToRotationVector2() * radius);
                trailRotations.Add(angle + MathHelper.PiOver2);
            }

            trailCurrentLength = MathHelper.TwoPi * radius;
            initialized = true;
        }

        public override void SetEffectParameters(Effect effect)
        {
            visualizeTrail = false;
            trailWidth = 30;

            Color primaryColor = new Color(0.24f, 0.56f, 1.0f);
            Color secondaryColor = new Color(0.95f, 0.80f, 0.25f);

            effect.Parameters["noiseTexture"].SetValue(tsorcRevamp.NoiseWavy);
            effect.Parameters["fadeOut"].SetValue(fadeOut);
            effect.Parameters["finalStand"].SetValue(1f);
            effect.Parameters["time"].SetValue(Main.GlobalTimeWrappedHourly);
            effect.Parameters["shaderColor"].SetValue(primaryColor.ToVector4());
            effect.Parameters["shaderColor2"].SetValue(secondaryColor.ToVector4());
            effect.Parameters["length"].SetValue(MathF.Max(64f, trailCurrentLength));

            float pixelBlocksX = MathF.Max(1f, trailCurrentLength / 2f);
            float pixelBlocksY = MathF.Max(1f, trailWidth / 2f);
            effect.Parameters["PixelGrid"]?.SetValue(
                new Vector4(pixelBlocksX, pixelBlocksY, 1f / pixelBlocksX, 1f / pixelBlocksY));
            effect.Parameters["WorldViewProjection"].SetValue(GetWorldViewProjectionMatrix());
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (detonated)
            {
                base.PreDraw(ref lightColor);
                return false;
            }

            // DynamicTrail runs PreDraw twice; only draw the spiral telegraph sprite in the normal pass.
            if (additiveContext)
            {
                return false;
            }

            Texture2D zoneTexture = TextureAssets.Projectile[Projectile.type].Value;
            float progress = MathHelper.Clamp(telegraphElapsed / (float)TelegraphTicks, 0f, 1f);
            float pulse = 0.88f + 0.12f * MathF.Sin(Main.GlobalTimeWrappedHourly * 11f);
            float alpha = MathHelper.Lerp(0.22f, 0.94f, progress) * pulse;
            float drawScale = MathHelper.Lerp(0.50f, 0.84f, progress);
            Vector2 drawPos = Projectile.Center - Main.screenPosition;
            Vector2 origin = zoneTexture.Size() * 0.5f;

            // Additive cosmic outer glow pass behind the crisp gold-and-violet 3-spiral rune.
            Color glowTint = new Color(130, 150, 255, 0) * (alpha * 0.55f);
            Main.EntitySpriteDraw(
                zoneTexture,
                drawPos,
                null,
                glowTint,
                spiralAngle,
                origin,
                drawScale * 1.07f,
                SpriteEffects.None,
                0);

            Main.EntitySpriteDraw(
                zoneTexture,
                drawPos,
                null,
                Color.White * alpha,
                spiralAngle,
                origin,
                drawScale,
                SpriteEffects.None,
                0);

            return false;
        }
    }

    /// <summary>
    /// Stage 2 of the Phase 1 StarForger dragon's Grand Comet Call:
    /// Descending cosmic meteor aimed cleanly into its paired KhaiosGrandCometZone.
    /// Detonates into a cluster of in-place DarkExplosionX blasts, arcing AzureBlaze flames,
    /// a layered cosmic starburst, and triggers the zone's expanding shockwave ring.
    /// ai[0] = target X, ai[1] = target Y, ai[2] = zone projectile index.
    /// </summary>
    public class KhaiosGrandCometMeteor : ModProjectile
    {
        private const int DescentTicks = 44;
        private const float BlastClusterRadiusPixels = 140f;

        private int elapsedTicks;
        private Vector2 startPosition;
        private bool detonated;

        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(GrandCometProjectile));

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 4;
            ProjectileID.Sets.DrawScreenCheckFluff[Projectile.type] = 2400;
        }

        public override void SetDefaults()
        {
            Projectile.width = 84;
            Projectile.height = 84;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = DescentTicks + 12;
        }

        public override void AI()
        {
            Vector2 targetPos = new Vector2(Projectile.ai[0], Projectile.ai[1]);

            if (elapsedTicks == 0)
            {
                startPosition = Projectile.Center;
            }

            elapsedTicks++;
            float progress = MathHelper.Clamp(elapsedTicks / (float)DescentTicks, 0f, 1f);
            float eased = progress * progress;

            Vector2 previousPos = Projectile.Center;
            Projectile.Center = Vector2.Lerp(startPosition, targetPos, eased);
            Vector2 travelDelta = Projectile.Center - previousPos;
            Projectile.velocity = Vector2.Zero;

            if (travelDelta.LengthSquared() > 0.01f)
            {
                Projectile.rotation = travelDelta.ToRotation() - MathHelper.PiOver4;
            }

            Projectile.frameCounter++;
            if (Projectile.frameCounter >= 4)
            {
                Projectile.frameCounter = 0;
                Projectile.frame = (Projectile.frame + 1) % Main.projFrames[Projectile.type];
            }

            if (!Main.dedServ)
            {
                Lighting.AddLight(Projectile.Center, 0.75f, 0.80f, 1.0f);

                // Blazing interpolated comet wake streaming cleanly behind the descending meteor.
                for (int i = 0; i < 7; i++)
                {
                    float stepFraction = (i + Main.rand.NextFloat()) / 7f;
                    Vector2 trailPos = Projectile.Center
                        - travelDelta * stepFraction
                        + Main.rand.NextVector2Circular(26f, 26f);

                    int dustType = DustID.BlueTorch;
                    Color tint = Color.DeepSkyBlue;
                    float minScale = 1.0f;
                    float maxScale = 1.65f;

                    if (i == 0 || i == 3)
                    {
                        dustType = DustID.GoldFlame;
                        tint = new Color(255, 225, 100);
                    }
                    else if (i == 1 || i == 4)
                    {
                        dustType = DustID.IceTorch;
                        tint = Color.Cyan;
                    }
                    else if (i == 2)
                    {
                        dustType = DustID.ShadowbeamStaff;
                        tint = Color.MediumPurple;
                    }
                    else if (i == 6)
                    {
                        dustType = DustID.AncientLight;
                        tint = Color.White;
                        minScale = 0.7f;
                        maxScale = 1.15f;
                    }

                    Dust trailDust = Dust.NewDustPerfect(
                        trailPos,
                        dustType,
                        -travelDelta * 0.16f + Main.rand.NextVector2Circular(1.8f, 1.8f),
                        20,
                        tint,
                        Main.rand.NextFloat(minScale, maxScale));
                    trailDust.noGravity = true;
                }
            }

            if (progress >= 1f)
            {
                TriggerImpact();
                Projectile.Kill();
            }
        }

        public override void OnKill(int timeLeft)
        {
            TriggerImpact();
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Projectile.type].Value;
            int frameHeight = texture.Height / Main.projFrames[Projectile.type];
            Rectangle frame = new Rectangle(0, Projectile.frame * frameHeight, texture.Width, frameHeight);
            Vector2 origin = frame.Size() * 0.5f;
            Vector2 drawPos = Projectile.Center - Main.screenPosition;

            // Additive azure-gold halo behind the falling comet sprite.
            Main.EntitySpriteDraw(
                texture,
                drawPos,
                frame,
                new Color(120, 195, 255, 0) * 0.55f,
                Projectile.rotation,
                origin,
                0.66f,
                SpriteEffects.None,
                0);

            Main.EntitySpriteDraw(
                texture,
                drawPos,
                frame,
                Color.White,
                Projectile.rotation,
                origin,
                0.56f,
                SpriteEffects.None,
                0);

            return false;
        }

        private void TriggerImpact()
        {
            if (detonated)
            {
                return;
            }

            detonated = true;

            int zoneIndex = (int)Projectile.ai[2];
            if (zoneIndex >= 0 && zoneIndex < Main.maxProjectiles)
            {
                Projectile zone = Main.projectile[zoneIndex];
                if (zone.active && zone.type == ModContent.ProjectileType<KhaiosGrandCometZone>())
                {
                    zone.ai[0] = 1f;
                    zone.netUpdate = true;
                }
            }

            if (!Main.dedServ)
            {
                SoundEngine.PlaySound(
                    new SoundStyle("tsorcRevamp/Sounds/DarkSouls/explosion-wind-tail") with { Volume = 0.65f, Pitch = -0.10f },
                    Projectile.Center);
                SoundEngine.PlaySound(
                    SoundID.DD2_ExplosiveTrapExplode with { Volume = 0.45f, Pitch = 0.15f },
                    Projectile.Center);
                UsefulFunctions.ScreenShake(Projectile.Center, 8f, 20, distanceFalloff: 850f);

                // Layer 1 (50 motes): blazing azure & violet core shock burst.
                for (int i = 0; i < 50; i++)
                {
                    int dustType = DustID.BlueTorch;
                    Color tint = Color.DeepSkyBlue;
                    if (i % 3 == 0)
                    {
                        dustType = DustID.ShadowbeamStaff;
                        tint = Color.MediumPurple;
                    }
                    else if (i % 3 == 1)
                    {
                        dustType = DustID.IceTorch;
                        tint = Color.Cyan;
                    }

                    Vector2 burstVel = Main.rand.NextVector2Circular(11f, 11f);
                    Dust core = Dust.NewDustPerfect(
                        Projectile.Center + Main.rand.NextVector2Circular(34f, 34f),
                        dustType,
                        burstVel,
                        15,
                        tint,
                        Main.rand.NextFloat(1.05f, 1.8f));
                    core.noGravity = true;
                }

                // Layer 2 (45 motes): high-speed golden starlight & electric starburst rays.
                for (int i = 0; i < 45; i++)
                {
                    float rayAngle = Main.rand.NextFloat(MathHelper.TwoPi);
                    Vector2 rayVel = rayAngle.ToRotationVector2() * Main.rand.NextFloat(5.5f, 14.5f);
                    int dustType = DustID.GoldFlame;
                    Color tint = new Color(255, 225, 100);
                    if (i % 3 == 1)
                    {
                        dustType = DustID.Electric;
                        tint = Color.Cyan;
                    }
                    else if (i % 3 == 2)
                    {
                        dustType = DustID.AncientLight;
                        tint = Color.White;
                    }

                    Dust ray = Dust.NewDustPerfect(
                        Projectile.Center + Main.rand.NextVector2Circular(16f, 16f),
                        dustType,
                        rayVel,
                        5,
                        tint,
                        Main.rand.NextFloat(0.75f, 1.35f));
                    ray.noGravity = true;
                }

                // Layer 3 (45 motes): upward fountain shower of glowing sparks raining back down over the crater.
                for (int i = 0; i < 45; i++)
                {
                    int dustType = DustID.IceTorch;
                    Color tint = Color.DeepSkyBlue;
                    if (i % 3 == 0)
                    {
                        dustType = DustID.PurpleTorch;
                        tint = Color.Violet;
                    }
                    else if (i % 3 == 1)
                    {
                        dustType = DustID.GoldFlame;
                        tint = new Color(255, 220, 95);
                    }

                    Vector2 showerVel = new Vector2(Main.rand.NextFloat(-8.5f, 8.5f), Main.rand.NextFloat(-10.5f, -2.5f));
                    Dust ember = Dust.NewDustPerfect(
                        Projectile.Center + Main.rand.NextVector2Circular(44f, 24f),
                        dustType,
                        showerVel,
                        20,
                        tint,
                        Main.rand.NextFloat(0.95f, 1.65f));
                    ember.noGravity = false;
                }
            }

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                int blastDamage = (int)(Projectile.damage * 0.85f);
                int flameDamage = (int)(Projectile.damage * 0.78f);

                // Central DarkExplosionX detonating immediately right at the meteor impact point.
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    Projectile.Center,
                    Vector2.Zero,
                    ModContent.ProjectileType<DarkExplosionX>(),
                    blastDamage,
                    3f,
                    Main.myPlayer,
                    0f,
                    1.68f);

                // 6 staggered DarkExplosionX blasts exploding in place spread throughout the initial blast radius.
                const int clusterCount = 6;
                float baseAngle = Main.rand.NextFloat(MathHelper.TwoPi);
                for (int i = 0; i < clusterCount; i++)
                {
                    float angle = baseAngle + (MathHelper.TwoPi * i / clusterCount) + Main.rand.NextFloat(-0.24f, 0.24f);
                    float radius = Main.rand.NextFloat(64f, BlastClusterRadiusPixels);
                    Vector2 blastPos = Projectile.Center + angle.ToRotationVector2() * radius;
                    float staggerDelay = 4f + i * 3f + Main.rand.NextFloat(0f, 2.5f);
                    float blastScale = Main.rand.NextFloat(1.10f, 1.45f);

                    Projectile.NewProjectile(
                        Projectile.GetSource_FromThis(),
                        blastPos,
                        Vector2.Zero,
                        ModContent.ProjectileType<DarkExplosionX>(),
                        blastDamage,
                        2.5f,
                        Main.myPlayer,
                        staggerDelay,
                        blastScale);
                }

                // 6 rotating AzureBlaze flames popping upward from the blast and arcing back down.
                const int blazeCount = 6;
                for (int i = 0; i < blazeCount; i++)
                {
                    float spreadAngle = MathHelper.Lerp(-2.48f, -0.66f, i / (float)(blazeCount - 1)) + Main.rand.NextFloat(-0.08f, 0.08f);
                    float launchSpeed = Main.rand.NextFloat(8.0f, 11.4f);
                    Vector2 blazeVelocity = spreadAngle.ToRotationVector2() * launchSpeed;
                    Vector2 launchPos = Projectile.Center + new Vector2(Main.rand.NextFloat(-32f, 32f), Main.rand.NextFloat(-14f, 8f));
                    float blazeScale = Main.rand.NextFloat(0.72f, 0.90f);

                    Projectile.NewProjectile(
                        Projectile.GetSource_FromThis(),
                        launchPos,
                        blazeVelocity,
                        ModContent.ProjectileType<AzureBlaze>(),
                        flameDamage,
                        2.5f,
                        Main.myPlayer,
                        blazeScale);
                }
            }
        }
    }
}

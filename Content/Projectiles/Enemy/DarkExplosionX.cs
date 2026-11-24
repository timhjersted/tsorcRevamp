using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Projectiles.Enemy
{
    /// <summary>
    /// Stationary 6-frame cosmic dark-violet explosion that detonates in place (autoloads DarkExplosionX.png).
    /// ai[0] = optional stagger delay in ticks before ignition (0 = immediate).
    /// ai[1] = optional scale multiplier (defaults to 1.35f when &lt;= 0.05f).
    /// </summary>
    public class DarkExplosionX : ModProjectile
    {
        private const int FrameCount = 6;
        private const int TicksPerFrame = 4;
        private const int ActiveDurationTicks = FrameCount * TicksPerFrame;
        private const float BaseHitRadiusPixels = 34f;

        private static readonly SoundStyle DetonationPopSound = SoundID.Item14 with
        {
            Volume = 0.24f,
            Pitch = 0.35f,
            PitchVariance = 0.18f,
            MaxInstances = 4,
        };

        private bool initialized;
        private int delayTicksRemaining;
        private int detonationAge;
        private float angularDrift;

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = FrameCount;
        }

        public override void SetDefaults()
        {
            Projectile.width = 76;
            Projectile.height = 76;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 120;
        }

        public override bool? CanDamage()
        {
            // Only deal damage during the bright peak explosion frames (frames 1..3), never during delay or faded smoke.
            bool inActiveWindow = initialized
                && delayTicksRemaining <= 0
                && Projectile.frame >= 1
                && Projectile.frame <= 3;
            return inActiveWindow;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (CanDamage() != true)
            {
                return false;
            }

            float hitRadius = BaseHitRadiusPixels * Projectile.scale;
            Vector2 closest = Vector2.Clamp(
                Projectile.Center,
                new Vector2(targetHitbox.Left, targetHitbox.Top),
                new Vector2(targetHitbox.Right, targetHitbox.Bottom));
            float distSq = Vector2.DistanceSquared(Projectile.Center, closest);
            return distSq <= hitRadius * hitRadius;
        }

        public override void AI()
        {
            Projectile.velocity = Vector2.Zero;

            if (!initialized)
            {
                initialized = true;
                delayTicksRemaining = Math.Max(0, (int)MathF.Round(Projectile.ai[0]));
                Projectile.scale = Projectile.ai[1] > 0.05f ? Projectile.ai[1] : 1.35f;
                Projectile.rotation = Main.rand.NextFloat(MathHelper.TwoPi);
                angularDrift = Main.rand.NextBool() ? 0.014f : -0.014f;
                Projectile.timeLeft = delayTicksRemaining + ActiveDurationTicks + 4;
            }

            if (delayTicksRemaining > 0)
            {
                delayTicksRemaining--;

                if (!Main.dedServ)
                {
                    // Brief converging spark preview at the exact spot right before this staggered blast pops.
                    for (int i = 0; i < 2; i++)
                    {
                        Vector2 offset = Main.rand.NextVector2CircularEdge(28f, 28f);
                        int dustType = i == 0 ? DustID.PurpleTorch : DustID.BlueTorch;
                        Dust charge = Dust.NewDustPerfect(
                            Projectile.Center + offset,
                            dustType,
                            -offset * 0.14f,
                            20,
                            Color.White,
                            Main.rand.NextFloat(0.8f, 1.25f));
                        charge.noGravity = true;
                    }
                }

                return;
            }

            if (detonationAge == 0)
            {
                SpawnIgnitionBurst();
            }

            Projectile.frame = Math.Min(FrameCount - 1, detonationAge / TicksPerFrame);
            Projectile.rotation += angularDrift;
            detonationAge++;

            if (!Main.dedServ)
            {
                float lightStrength = MathHelper.Clamp(1f - (detonationAge / (float)ActiveDurationTicks), 0f, 1f);
                Lighting.AddLight(Projectile.Center, 0.72f * lightStrength, 0.34f * lightStrength, 0.95f * lightStrength);

                // Keep energetic violet and cyan sparks crackling outward through the active blast frames.
                if (detonationAge < ActiveDurationTicks - 4)
                {
                    for (int i = 0; i < 2; i++)
                    {
                        int dustType = i == 0 ? DustID.ShadowbeamStaff : DustID.IceTorch;
                        Dust crackle = Dust.NewDustPerfect(
                            Projectile.Center + Main.rand.NextVector2Circular(26f * Projectile.scale, 26f * Projectile.scale),
                            dustType,
                            Main.rand.NextVector2Circular(3.2f, 3.2f),
                            20,
                            Color.White,
                            Main.rand.NextFloat(0.85f, 1.35f));
                        crackle.noGravity = true;
                    }
                }
            }

            if (detonationAge >= ActiveDurationTicks)
            {
                Projectile.Kill();
            }
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(BuffID.ShadowFlame, 180);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (!initialized || delayTicksRemaining > 0)
            {
                return false;
            }

            Texture2D texture = TextureAssets.Projectile[Projectile.type].Value;
            int frameHeight = texture.Height / FrameCount;
            Rectangle sourceRect = new Rectangle(0, Projectile.frame * frameHeight, texture.Width, frameHeight);
            Vector2 origin = sourceRect.Size() * 0.5f;
            Vector2 drawPos = Projectile.Center - Main.screenPosition;

            float fadeAlpha = 1f;
            if (Projectile.frame >= 4)
            {
                float fadeProgress = MathHelper.Clamp((detonationAge - (4 * TicksPerFrame)) / (float)(2 * TicksPerFrame), 0f, 1f);
                fadeAlpha = MathHelper.Lerp(1f, 0.15f, fadeProgress);
            }

            // Soft additive violet-white bloom behind the peak explosion frames so it pops against dark bricks.
            if (Projectile.frame >= 1 && Projectile.frame <= 3)
            {
                Color glowColor = new Color(185, 115, 255, 0) * (0.55f * fadeAlpha);
                Main.EntitySpriteDraw(
                    texture,
                    drawPos,
                    sourceRect,
                    glowColor,
                    Projectile.rotation,
                    origin,
                    Projectile.scale * 1.14f,
                    SpriteEffects.None,
                    0);
            }

            Main.EntitySpriteDraw(
                texture,
                drawPos,
                sourceRect,
                Color.White * fadeAlpha,
                Projectile.rotation,
                origin,
                Projectile.scale,
                SpriteEffects.None,
                0);

            return false;
        }

        private void SpawnIgnitionBurst()
        {
            if (Main.dedServ)
            {
                return;
            }

            SoundEngine.PlaySound(DetonationPopSound, Projectile.Center);

            // 3-layer cosmic blast burst: violet body, azure-cyan flash, and crisp golden star glints.
            for (int i = 0; i < 30; i++)
            {
                int dustType = DustID.ShadowbeamStaff;
                Color tint = Color.MediumPurple;
                float minScale = 0.95f;
                float maxScale = 1.65f;

                if (i % 5 == 0)
                {
                    dustType = DustID.GoldFlame;
                    tint = new Color(255, 225, 105);
                    minScale = 0.75f;
                    maxScale = 1.25f;
                }
                else if (i % 5 == 1)
                {
                    dustType = DustID.BlueTorch;
                    tint = Color.DeepSkyBlue;
                }
                else if (i % 5 == 2)
                {
                    dustType = DustID.PurpleTorch;
                    tint = Color.Violet;
                }
                else if (i % 5 == 3)
                {
                    dustType = DustID.AncientLight;
                    tint = Color.White;
                    minScale = 0.65f;
                    maxScale = 1.05f;
                }

                Vector2 spawnPos = Projectile.Center + Main.rand.NextVector2Circular(16f * Projectile.scale, 16f * Projectile.scale);
                Vector2 burstVel = Main.rand.NextVector2Circular(6.5f, 6.5f);
                Dust spark = Dust.NewDustPerfect(
                    spawnPos,
                    dustType,
                    burstVel,
                    15,
                    tint,
                    Main.rand.NextFloat(minScale, maxScale));
                spark.noGravity = true;
            }
        }
    }
}

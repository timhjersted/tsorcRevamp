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
    /// Hostile swirling azure flame that pops upward from cosmic blasts, rotates in mid-air,
    /// and arcs down under gravity leaving a blazing sapphire trail (autoloads AzureBlaze.png).
    /// ai[0] = optional scale multiplier (defaults to 0.78f when &lt;= 0.05f).
    /// </summary>
    public class AzureBlaze : ModProjectile
    {
        private const int FrameCount = 5;
        private const float GravityPerTick = 0.24f;
        private const float MaxFallSpeed = 13.5f;
        private const int TileGraceTicks = 14;
        private const int LifetimeTicks = 150;

        private static readonly SoundStyle SplashSound = SoundID.Item20 with
        {
            Volume = 0.24f,
            Pitch = 0.25f,
            PitchVariance = 0.16f,
            MaxInstances = 4,
        };

        private bool initialized;
        private int elapsedTicks;
        private float spinSpeed;

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = FrameCount;
        }

        public override void SetDefaults()
        {
            Projectile.width = 34;
            Projectile.height = 34;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = 1;
            Projectile.timeLeft = LifetimeTicks;
        }

        public override void AI()
        {
            if (!initialized)
            {
                initialized = true;
                Projectile.scale = Projectile.ai[0] > 0.05f ? Projectile.ai[0] : 0.78f;
                float spinSign = Projectile.velocity.X >= 0f ? 1f : -1f;
                spinSpeed = spinSign * Main.rand.NextFloat(0.12f, 0.19f);
                Projectile.rotation = Main.rand.NextFloat(MathHelper.TwoPi);
            }

            elapsedTicks++;

            // Enable tile collision once the flame finishes popping upward so ground-level blasts never clip instantly.
            if (elapsedTicks >= TileGraceTicks || Projectile.velocity.Y > 0.5f)
            {
                Projectile.tileCollide = true;
            }

            Projectile.velocity.Y = Math.Min(MaxFallSpeed, Projectile.velocity.Y + GravityPerTick);
            Projectile.velocity.X *= 0.994f;
            Projectile.rotation += spinSpeed;

            // Bloom quickly 0 -> 4 on launch, then cycle roaring upper flame frames 2..4.
            if (elapsedTicks < 12)
            {
                Projectile.frame = Math.Min(FrameCount - 1, elapsedTicks / 3);
            }
            else
            {
                int cycleStep = ((elapsedTicks - 12) / 4) % 4;
                if (cycleStep == 0)
                {
                    Projectile.frame = 2;
                }
                else if (cycleStep == 1 || cycleStep == 3)
                {
                    Projectile.frame = 3;
                }
                else
                {
                    Projectile.frame = 4;
                }
            }

            if (!Main.dedServ)
            {
                Lighting.AddLight(Projectile.Center, 0.22f, 0.58f, 1.0f);

                // Interpolated azure flame wake so fast arcs stay continuous with zero dot gaps.
                for (int i = 0; i < 3; i++)
                {
                    float stepFraction = (i + Main.rand.NextFloat()) / 3f;
                    Vector2 trailPos = Projectile.Center
                        - Projectile.velocity * stepFraction
                        + Main.rand.NextVector2Circular(11f * Projectile.scale, 11f * Projectile.scale);
                    int dustType = i == 0 ? DustID.BlueTorch : DustID.IceTorch;
                    Color dustTint = i == 0 ? Color.RoyalBlue : Color.DeepSkyBlue;
                    Dust fireTrail = Dust.NewDustPerfect(
                        trailPos,
                        dustType,
                        -Projectile.velocity * 0.14f + Main.rand.NextVector2Circular(1.2f, 1.2f),
                        25,
                        dustTint,
                        Main.rand.NextFloat(0.95f, 1.45f));
                    fireTrail.noGravity = true;
                }

                // Bright foreground cyan glint riding the spinning flame core.
                int glintType = Main.rand.NextBool(3) ? DustID.Electric : DustID.Frost;
                Dust glint = Dust.NewDustPerfect(
                    Projectile.Center + Main.rand.NextVector2Circular(8f, 8f),
                    glintType,
                    -Projectile.velocity * 0.08f + Main.rand.NextVector2Circular(1.4f, 1.4f),
                    10,
                    Color.LightCyan,
                    Main.rand.NextFloat(0.65f, 0.95f));
                glint.noGravity = true;
            }
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(BuffID.Frostburn, 180);
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.dedServ)
            {
                return;
            }

            SoundEngine.PlaySound(SplashSound, Projectile.Center);

            // Midground sapphire flame splash fanning outward and upward on impact.
            for (int i = 0; i < 24; i++)
            {
                Vector2 splashVel = Main.rand.NextVector2Circular(5.4f, 5.4f) + new Vector2(0f, -1.4f);
                Dust flameSplash = Dust.NewDustPerfect(
                    Projectile.Center + Main.rand.NextVector2Circular(10f, 10f),
                    DustID.IceTorch,
                    splashVel,
                    20,
                    Color.DeepSkyBlue,
                    Main.rand.NextFloat(0.95f, 1.55f));
                flameSplash.noGravity = i % 3 != 0;
            }

            // Deep royal-blue secondary fire wave.
            for (int i = 0; i < 12; i++)
            {
                Vector2 deepVel = Main.rand.NextVector2Circular(4.2f, 4.2f) + new Vector2(0f, -1.0f);
                Dust deepFlame = Dust.NewDustPerfect(
                    Projectile.Center + Main.rand.NextVector2Circular(8f, 8f),
                    DustID.BlueTorch,
                    deepVel,
                    25,
                    Color.RoyalBlue,
                    Main.rand.NextFloat(1.0f, 1.55f));
                deepFlame.noGravity = true;
            }

            // Foreground electric cyan sparks popping cleanly off the splash.
            for (int i = 0; i < 10; i++)
            {
                Vector2 sparkVel = Main.rand.NextVector2Circular(6.5f, 6.5f);
                Dust electricSpark = Dust.NewDustPerfect(
                    Projectile.Center + Main.rand.NextVector2Circular(6f, 6f),
                    DustID.Electric,
                    sparkVel,
                    0,
                    Color.LightCyan,
                    Main.rand.NextFloat(0.7f, 1.1f));
                electricSpark.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Projectile.type].Value;
            int frameHeight = texture.Height / FrameCount;
            Rectangle sourceRect = new Rectangle(0, Projectile.frame * frameHeight, texture.Width, frameHeight);
            Vector2 origin = sourceRect.Size() * 0.5f;
            Vector2 drawPos = Projectile.Center - Main.screenPosition;

            float fadeAlpha = Projectile.timeLeft < 14
                ? MathHelper.Clamp(Projectile.timeLeft / 14f, 0f, 1f)
                : 1f;

            // Additive cyan aura pass so the rotating flame glows cleanly against dark dungeon walls.
            Color glowColor = new Color(60, 185, 255, 0) * (0.45f * fadeAlpha);
            Main.EntitySpriteDraw(
                texture,
                drawPos,
                sourceRect,
                glowColor,
                Projectile.rotation,
                origin,
                Projectile.scale * 1.16f,
                SpriteEffects.None,
                0);

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
    }
}

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Content.Projectiles.Enemy
{
    /// <summary>
    /// Hostile-only clone of Terraria's rolling boulder for the Elemental pack's synced Stone Fall attack.
    /// Uses vanilla Boulder art and drop-then-roll physics without damaging enemies.
    /// ai[0] = pre-drop telegraph ticks remaining (e.g. 35); ai[1] = synced roll direction (-1 or +1).
    /// </summary>
    public class ElementalBoulder : ModProjectile
    {
        const int DefaultTelegraphTicks = 35;
        const int LifetimeTicks = 340;
        const int FadeTicks = 15;
        const float GravityPerTick = 0.38f;
        const float MaxFallSpeed = 14.5f;
        const float TargetRollSpeed = 6.8f;
        const float RollAcceleration = 0.24f;

        bool startedTelegraphBurst;
        bool hasLanded;
        int initialTelegraphTicks = DefaultTelegraphTicks;

        public override string Texture => UsefulFunctions.VanillaTextureFilepath(ProjectileID.Boulder, "Projectile");

        public override void SetDefaults()
        {
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.width = 28;
            Projectile.height = 28;
            Projectile.DamageType = DamageClass.Generic;
            Projectile.penetrate = -1;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = false;
            Projectile.timeLeft = LifetimeTicks;
        }

        public override bool? CanDamage()
        {
            bool telegraphing = Projectile.ai[0] > 0f;
            bool fadingOut = Projectile.timeLeft <= FadeTicks;
            return !telegraphing && !fadingOut;
        }

        public override void AI()
        {
            // Phase 1: Hover at the air spawn point 10-20 tiles above the player while shedding clay and dirt dust.
            if (Projectile.ai[0] > 0f)
            {
                if (!startedTelegraphBurst)
                {
                    startedTelegraphBurst = true;
                    initialTelegraphTicks = Math.Max(1, (int)Projectile.ai[0]);
                    EmitSpawnTelegraphBurst(28);
                }

                Projectile.velocity = Vector2.Zero;
                Projectile.tileCollide = false;
                Projectile.ai[0] -= 1f;

                if (!Main.dedServ)
                {
                    // Steady cascade of clay, dirt, and stone crumbling downward from the spawn point.
                    for (int i = 0; i < 4; i++)
                    {
                        int dustType = DustID.Dirt;
                        if (i % 2 == 0)
                        {
                            dustType = DustID.Clay;
                        }
                        else if (i == 3)
                        {
                            dustType = DustID.Stone;
                        }

                        Vector2 offset = Main.rand.NextVector2Circular(16f, 12f);
                        Vector2 velocity = new Vector2(Main.rand.NextFloat(-1.4f, 1.4f), Main.rand.NextFloat(0.6f, 3.4f));
                        Dust clump = Dust.NewDustPerfect(Projectile.Center + offset, dustType, velocity, 40, default, Main.rand.NextFloat(0.95f, 1.45f));
                        clump.noGravity = false;
                    }
                }

                if (Projectile.ai[0] <= 0f)
                {
                    // Drop release: enable tile collision and push downward.
                    Projectile.tileCollide = true;
                    float driftDir = GetRollDirection();
                    Projectile.velocity = new Vector2(driftDir * 0.5f, 2f);
                    EmitSpawnTelegraphBurst(16);
                }

                return;
            }

            // Phase 2: Fall with gravity, then roll along the floor in the synced roll direction.
            Projectile.tileCollide = true;
            Projectile.velocity.Y += GravityPerTick;
            if (Projectile.velocity.Y > MaxFallSpeed)
            {
                Projectile.velocity.Y = MaxFallSpeed;
            }

            if (hasLanded)
            {
                float rollDir = GetRollDirection();
                float targetSpeed = rollDir * TargetRollSpeed;
                if (Projectile.velocity.X < targetSpeed)
                {
                    Projectile.velocity.X = Math.Min(Projectile.velocity.X + RollAcceleration, targetSpeed);
                }
                else if (Projectile.velocity.X > targetSpeed)
                {
                    Projectile.velocity.X = Math.Max(Projectile.velocity.X - RollAcceleration, targetSpeed);
                }

                Projectile.rotation += Projectile.velocity.X * 0.055f;

                if (!Main.dedServ && Projectile.velocity.Y == 0f && Main.rand.NextBool(2))
                {
                    int dustType = DustID.Dirt;
                    if (Main.rand.NextBool())
                    {
                        dustType = DustID.Clay;
                    }

                    Vector2 groundPos = Projectile.Bottom + new Vector2(Main.rand.NextFloat(-10f, 10f), -2f);
                    Vector2 kickVel = new Vector2(-Projectile.velocity.X * 0.3f, Main.rand.NextFloat(-2.4f, -0.6f));
                    Dust kick = Dust.NewDustPerfect(groundPos, dustType, kickVel, 40, default, Main.rand.NextFloat(0.85f, 1.25f));
                    kick.noGravity = false;
                }
            }
            else
            {
                Projectile.rotation += GetRollDirection() * 0.04f;

                if (!Main.dedServ && Main.rand.NextBool(2))
                {
                    int dustType = DustID.Clay;
                    if (Main.rand.NextBool())
                    {
                        dustType = DustID.Dirt;
                    }

                    Dust trail = Dust.NewDustPerfect(
                        Projectile.Center + Main.rand.NextVector2Circular(12f, 12f),
                        dustType,
                        new Vector2(Main.rand.NextFloat(-0.8f, 0.8f), -0.6f),
                        60,
                        default,
                        Main.rand.NextFloat(0.85f, 1.2f));
                    trail.noGravity = false;
                }
            }

            if (!Main.dedServ && Projectile.timeLeft <= FadeTicks && Main.rand.NextBool(2))
            {
                Dust crumble = Dust.NewDustPerfect(
                    Projectile.Center + Main.rand.NextVector2Circular(12f, 12f),
                    DustID.Stone,
                    Main.rand.NextVector2Circular(2f, 2f),
                    80,
                    default,
                    Main.rand.NextFloat(0.8f, 1.15f));
                crumble.noGravity = false;
            }
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            // Horizontal collision with a wall while rolling shatters the boulder cleanly.
            bool hitWall = Math.Abs(Projectile.velocity.X - oldVelocity.X) > 1.2f;
            if (hasLanded && hitWall)
            {
                return true;
            }

            // Floor landing: bounce lightly on the first impact and begin rolling.
            bool hitFloor = oldVelocity.Y > 1.5f && Math.Abs(Projectile.velocity.Y - oldVelocity.Y) > 0.8f;
            if (hitFloor)
            {
                if (!hasLanded)
                {
                    hasLanded = true;
                    Projectile.velocity.X = GetRollDirection() * 5.6f;
                    Projectile.velocity.Y = -MathHelper.Clamp(oldVelocity.Y * 0.24f, 1.8f, 3.6f);

                    if (!Main.dedServ)
                    {
                        SoundEngine.PlaySound(SoundID.Item70 with { Volume = 0.7f, Pitch = -0.25f }, Projectile.Bottom);
                        for (int i = 0; i < 20; i++)
                        {
                            int dustType = DustID.Dirt;
                            if (i % 3 == 0)
                            {
                                dustType = DustID.Clay;
                            }
                            else if (i % 3 == 1)
                            {
                                dustType = DustID.Stone;
                            }

                            Vector2 impactPos = Projectile.Bottom + new Vector2(Main.rand.NextFloat(-14f, 14f), -2f);
                            Vector2 impactVel = new Vector2(Main.rand.NextFloat(-4f, 4f), Main.rand.NextFloat(-4.5f, -1f));
                            Dust dust = Dust.NewDustPerfect(impactPos, dustType, impactVel, 40, default, Main.rand.NextFloat(0.95f, 1.45f));
                            dust.noGravity = false;
                        }
                    }
                }
                else
                {
                    Projectile.velocity.Y = 0f;
                }
            }

            return false;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Vector2 origin = texture.Size() * 0.5f;
            Vector2 drawPos = Projectile.Center - Main.screenPosition;
            float drawScale = Projectile.scale;
            float alpha = 1f;

            if (Projectile.ai[0] > 0f)
            {
                float progress = 1f - MathHelper.Clamp(Projectile.ai[0] / Math.Max(1, initialTelegraphTicks), 0f, 1f);
                drawScale *= MathHelper.Lerp(0.28f, 1f, progress);
                alpha = MathHelper.Lerp(0.45f, 1f, progress);
                drawPos += Main.rand.NextVector2Circular(1.8f, 1.8f);
            }
            else if (Projectile.timeLeft <= FadeTicks)
            {
                float fade = MathHelper.Clamp(Projectile.timeLeft / (float)FadeTicks, 0f, 1f);
                drawScale *= 0.35f + 0.65f * fade;
                alpha = fade;
            }

            Main.EntitySpriteDraw(texture, drawPos, null, lightColor * alpha, Projectile.rotation, origin, drawScale, SpriteEffects.None, 0f);
            return false;
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.dedServ)
            {
                return;
            }

            SoundEngine.PlaySound(SoundID.Dig with { Volume = 0.8f, Pitch = -0.3f }, Projectile.Center);
            SoundEngine.PlaySound(SoundID.Item70 with { Volume = 0.6f, Pitch = -0.35f }, Projectile.Center);

            // Shatter burst: stone, clay, and dirt chunks with gravity + dusty smoke puff.
            for (int i = 0; i < 34; i++)
            {
                int dustType = DustID.Stone;
                if (i % 3 == 0)
                {
                    dustType = DustID.Clay;
                }
                else if (i % 3 == 1)
                {
                    dustType = DustID.Dirt;
                }

                Vector2 velocity = Main.rand.NextVector2Circular(5.5f, 5.5f) + new Vector2(0f, -1.8f);
                Dust debris = Dust.NewDustPerfect(
                    Projectile.Center + Main.rand.NextVector2Circular(12f, 12f),
                    dustType,
                    velocity,
                    40,
                    default,
                    Main.rand.NextFloat(0.95f, 1.5f));
                debris.noGravity = false;
            }

            for (int i = 0; i < 8; i++)
            {
                Vector2 velocity = Main.rand.NextVector2Circular(2.2f, 1.8f) + new Vector2(0f, -0.6f);
                Dust puff = Dust.NewDustPerfect(
                    Projectile.Center + Main.rand.NextVector2Circular(10f, 10f),
                    DustID.Smoke,
                    velocity,
                    140,
                    new Color(120, 96, 78),
                    Main.rand.NextFloat(0.6f, 0.85f));
                puff.noGravity = true;
                puff.noLight = true;
                puff.fadeIn = Main.rand.NextFloat(1.35f, 1.7f);
            }
        }

        float GetRollDirection()
        {
            if (Projectile.ai[1] < 0f)
            {
                return -1f;
            }

            return 1f;
        }

        void EmitSpawnTelegraphBurst(int count)
        {
            if (Main.dedServ)
            {
                return;
            }

            SoundEngine.PlaySound(SoundID.Dig with { Volume = 0.6f, Pitch = -0.45f }, Projectile.Center);
            for (int i = 0; i < count; i++)
            {
                int dustType = DustID.Clay;
                if (i % 3 == 1)
                {
                    dustType = DustID.Dirt;
                }
                else if (i % 4 == 3)
                {
                    dustType = DustID.Stone;
                }

                Vector2 offset = Main.rand.NextVector2Circular(18f, 14f);
                Vector2 velocity = new Vector2(Main.rand.NextFloat(-3.2f, 3.2f), Main.rand.NextFloat(-1.5f, 3.5f));
                Dust chunk = Dust.NewDustPerfect(Projectile.Center + offset, dustType, velocity, 40, default, Main.rand.NextFloat(0.95f, 1.55f));
                chunk.noGravity = false;
            }
        }
    }
}

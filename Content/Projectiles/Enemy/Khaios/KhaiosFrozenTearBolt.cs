using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Content.Projectiles.VFX;

namespace tsorcRevamp.Content.Projectiles.Enemy.Khaios
{
    /// <summary>
    /// Hostile Frozen Tear III ricochet bolt used by Phase 1 Khaios.
    /// Zero-gravity straight-line ice bolt that banks off floors, walls, and ceilings with crisp
    /// WaterTrail shader ribbons, ricochet audio cues, and an authored shatter/fade death.
    /// ai[0] = remaining bounces (defaults to 3 when &lt;= 0).
    /// ai[1] = pre-launch bank sightline ticks (optional brief telegraph).
    /// </summary>
    public class KhaiosFrozenTearBolt : DynamicTrail
    {
        private const int DefaultBounceBudget = 3;
        private const int FadeOutDurationTicks = 18;
        private const int LifetimeTicks = 300;
        private const float MaxCollisionHalfSize = 12f;

        private static readonly SoundStyle IceBounceImpactSound = SoundID.Item50 with
        {
            Volume = 0.56f,
            Pitch = 0.35f,
            PitchVariance = 0.14f,
            MaxInstances = 4,
        };

        private static readonly SoundStyle IceBounceChimeSound = SoundID.Item27 with
        {
            Volume = 0.28f,
            Pitch = 0.55f,
            PitchVariance = 0.12f,
            MaxInstances = 4,
        };

        private float baseNoiseOffset;
        private int bouncesRemaining = -1;
        private bool fadingOut;
        private int fadeTicksRemaining;
        private bool shattered;

        public override string Texture => UsefulFunctions.RefactorableFilepath(typeof(FreezeBolt3));

        public override void SetStaticDefaults()
        {
            base.SetStaticDefaults();
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 6;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 0;
        }

        public override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = LifetimeTicks;

            trailCollision = true;
            normalCollision = false;
            trailWidth = 25;
            trailPointLimit = 150;
            trailYOffset = 0;
            trailMaxLength = 210;
            NPCSource = false;
            collisionPadding = 0;
            collisionEndPadding = 1;
            collisionFrequency = 2;
            noFadeOut = true;
            customEffect = ModContent.Request<Effect>("tsorcRevamp/Effects/WaterTrail", AssetRequestMode.ImmediateLoad).Value;
        }

        public override bool? CanDamage()
        {
            return !fadingOut;
        }

        public override void AI()
        {
            if (bouncesRemaining < 0)
            {
                int requested = (int)Math.Round(Projectile.ai[0]);
                bouncesRemaining = requested > 0 ? requested : DefaultBounceBudget;
            }

            base.AI();

            // Keep shader-trail hitbox padding synced in AI rather than PreDraw so collision never depends on rendering.
            if (trailPositions != null && trailPositions.Count > 0)
            {
                collisionEndPadding = trailPositions.Count / 5;
                collisionPadding = trailPositions.Count / 8;
            }

            if (!fadingOut && Projectile.timeLeft <= FadeOutDurationTicks)
            {
                BeginFadeOut();
            }

            if (fadingOut)
            {
                Projectile.velocity *= 0.86f;
                fadeTicksRemaining--;
                fadeOut = MathHelper.Clamp(fadeTicksRemaining / (float)FadeOutDurationTicks, 0f, 1f);
                if (fadeTicksRemaining <= 0)
                {
                    Projectile.Kill();
                    return;
                }
            }
            else
            {
                fadeOut = 1f;
                Projectile.rotation += 0.35f;
            }

            if (!Main.dedServ)
            {
                Lighting.AddLight(Projectile.Center, 0.25f, 0.65f, 1.0f);

                // Dense layered blue ice dust right over the leading circle sprite so it blazes into the shader trail.
                if (!fadingOut)
                {
                    for (int i = 0; i < 5; i++)
                    {
                        int dustType = DustID.IceTorch;
                        Color dustTint = Color.DeepSkyBlue;
                        if (i == 1 || i == 3)
                        {
                            dustType = DustID.BlueTorch;
                            dustTint = Color.CornflowerBlue;
                        }
                        else if (i == 4)
                        {
                            dustType = DustID.Frost;
                            dustTint = Color.Cyan;
                        }

                        Vector2 dustPos = Projectile.Center + Main.rand.NextVector2Circular(9f, 9f);
                        Vector2 sparkVelocity = -Projectile.velocity * Main.rand.NextFloat(0.05f, 0.16f)
                            + Main.rand.NextVector2Circular(1.3f, 1.3f);
                        Dust iceDust = Dust.NewDustPerfect(
                            dustPos,
                            dustType,
                            sparkVelocity,
                            25,
                            dustTint,
                            Main.rand.NextFloat(1.0f, 1.55f));
                        iceDust.noGravity = true;
                    }

                    for (int i = 0; i < 2; i++)
                    {
                        int glintType = i == 0 ? DustID.Frost : DustID.AncientLight;
                        Vector2 glintPos = Projectile.Center + Main.rand.NextVector2Circular(6f, 6f);
                        Vector2 glintVelocity = -Projectile.velocity * Main.rand.NextFloat(0.03f, 0.10f)
                            + Main.rand.NextVector2Circular(1.0f, 1.0f);
                        Dust glintDust = Dust.NewDustPerfect(
                            glintPos,
                            glintType,
                            glintVelocity,
                            0,
                            Color.LightCyan,
                            Main.rand.NextFloat(0.65f, 0.95f));
                        glintDust.noGravity = true;
                    }
                }
            }
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            if (fadingOut)
            {
                return false;
            }

            if (bouncesRemaining <= 0)
            {
                SpawnShatterBurst(oldVelocity);
                return true;
            }

            bouncesRemaining--;

            Vector2 reflected = oldVelocity;
            if (Math.Abs(Projectile.velocity.X - oldVelocity.X) > 0.01f)
            {
                reflected.X = -oldVelocity.X;
            }

            if (Math.Abs(Projectile.velocity.Y - oldVelocity.Y) > 0.01f)
            {
                reflected.Y = -oldVelocity.Y;
            }

            Projectile.velocity = reflected;
            Projectile.netUpdate = true;

            if (!Main.dedServ)
            {
                Collision.HitTiles(Projectile.position, oldVelocity, Projectile.width, Projectile.height);
                SoundEngine.PlaySound(IceBounceImpactSound, Projectile.Center);
                SoundEngine.PlaySound(IceBounceChimeSound, Projectile.Center);

                for (int i = 0; i < 14; i++)
                {
                    Vector2 burstVel = reflected.SafeNormalize(Vector2.UnitY).RotatedByRandom(1.1f)
                        * Main.rand.NextFloat(2.2f, 5.4f);
                    int dustType = i % 3 == 0 ? DustID.IceRod : DustID.IceTorch;
                    Dust spark = Dust.NewDustPerfect(
                        Projectile.Center,
                        dustType,
                        burstVel,
                        40,
                        Color.Cyan,
                        Main.rand.NextFloat(1.0f, 1.55f));
                    spark.noGravity = true;
                }
            }

            return false;
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(BuffID.Frostburn, 210);
            SpawnShatterBurst(Projectile.velocity);
            Projectile.Kill();
        }

        public override void OnKill(int timeLeft)
        {
            if (!fadingOut)
            {
                SpawnShatterBurst(Projectile.velocity);
            }
        }

        public override float CollisionWidthFunction(float progress)
        {
            return 11f;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (fadingOut)
            {
                return false;
            }

            // Primary hitbox: the live WaterTrail shader ribbon itself (matching player FreezeBolt3).
            if (base.Colliding(projHitbox, targetHitbox) == true)
            {
                return true;
            }

            // Leading icy orb check so contact at the very tip never misses before the trail catches up.
            Vector2 closest = Vector2.Clamp(
                Projectile.Center,
                new Vector2(targetHitbox.Left, targetHitbox.Top),
                new Vector2(targetHitbox.Right, targetHitbox.Bottom));
            float distSq = Vector2.DistanceSquared(Projectile.Center, closest);
            return distSq <= MaxCollisionHalfSize * MaxCollisionHalfSize;
        }

        public override void SetEffectParameters(Effect effect)
        {
            if (baseNoiseOffset == 0f)
            {
                baseNoiseOffset = Main.rand.NextFloat();
            }

            effect.Parameters["baseNoise"].SetValue(tsorcRevamp.NoiseSmooth);
            effect.Parameters["baseNoiseUOffset"].SetValue(baseNoiseOffset);
            visualizeTrail = false;
            effect.Parameters["fadeOut"].SetValue(fadeOut);
            effect.Parameters["time"].SetValue(Main.GlobalTimeWrappedHourly);

            Color coreColor = new Color(0.28f, 0.78f, 1f, 1f);
            Color edgeColor = new Color(0.05f, 0.22f, 1f, 1f);
            effect.Parameters["slashCenter"].SetValue(coreColor.ToVector4());
            effect.Parameters["slashEdge"].SetValue(edgeColor.ToVector4());
            effect.Parameters["WorldViewProjection"].SetValue(GetWorldViewProjectionMatrix());
        }

        public override bool PreDraw(ref Color lightColor)
        {
            base.PreDraw(ref lightColor);

            // DynamicTrail runs PreDraw twice: additive pass draws the WaterTrail shader, normal pass draws the leading orb sprite.
            if (additiveContext)
            {
                return false;
            }

            Texture2D texture = TextureAssets.Projectile[Projectile.type].Value;
            Vector2 origin = texture.Size() * 0.5f;
            float drawAlpha = fadingOut ? fadeOut : 1f;
            Color drawColor = Color.Lerp(Color.Cyan, Color.White, 0.45f) * drawAlpha;
            float drawScale = Projectile.scale * (0.95f + 0.12f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 14f));

            Main.EntitySpriteDraw(
                texture,
                Projectile.Center - Main.screenPosition,
                null,
                drawColor,
                Projectile.rotation,
                origin,
                drawScale,
                SpriteEffects.None,
                0);

            return false;
        }

        private void BeginFadeOut()
        {
            fadingOut = true;
            fadeTicksRemaining = FadeOutDurationTicks;
            Projectile.tileCollide = false;
            Projectile.timeLeft = FadeOutDurationTicks + 2;
        }

        private void SpawnShatterBurst(Vector2 impactVelocity)
        {
            if (shattered || Main.dedServ)
            {
                return;
            }

            shattered = true;
            SoundEngine.PlaySound(SoundID.Item27 with { Volume = 0.42f, Pitch = 0.12f, PitchVariance = 0.1f }, Projectile.Center);

            for (int i = 0; i < 42; i++)
            {
                Vector2 burstVelocity = Main.rand.NextVector2Circular(6.5f, 6.5f) - impactVelocity * 0.12f;
                int dustType = i % 4 == 0 ? DustID.Ice : DustID.IceTorch;
                Dust shard = Dust.NewDustPerfect(
                    Projectile.Center + Main.rand.NextVector2Circular(6f, 6f),
                    dustType,
                    burstVelocity,
                    50,
                    Color.DeepSkyBlue,
                    Main.rand.NextFloat(1.0f, 1.85f));
                shard.noGravity = i % 3 != 0;
            }
        }
    }
}

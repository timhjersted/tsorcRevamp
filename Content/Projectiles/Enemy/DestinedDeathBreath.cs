using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;
using tsorcRevamp.NPCs.Invaders;

namespace tsorcRevamp.Content.Projectiles.Enemy
{
    /// <summary>
    /// Destined Death flame used by the serpent's breath stream and Great Red Knight's falling cinders.
    /// </summary>
    public class DestinedDeathBreath : ModProjectile
    {
        internal const float CinderRainMode = 2f;
        const int RainLifetimeTicks = 240;
        bool IsCinderRain => Projectile.ai[1] == CinderRainMode;
        bool rainGroundImpact;

        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.scale = 1f;
            Projectile.hostile = true;
            Projectile.friendly = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = 3;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = RainLifetimeTicks;
            Projectile.light = 0.55f;
            Projectile.netImportant = true;
        }

        public override void AI()
        {
            bool cinderRain = IsCinderRain;
            if (cinderRain)
            {
                Projectile.tileCollide = true;
                int age = RainLifetimeTicks - Projectile.timeLeft;
                float fadeIn = MathHelper.Clamp((age + 1f) / 6f, 0f, 1f);
                float fadeOut = MathHelper.Clamp(Projectile.timeLeft / 16f, 0f, 1f);
                Projectile.alpha = (int)(255f * (1f - fadeIn * fadeOut));
            }
            else if (Projectile.ai[1] == 1f)
            {
                Projectile.tileCollide = true;

                if (Projectile.timeLeft > 48)
                {
                    Projectile.timeLeft = 48;
                }

                float lifeProgress = 1f - (Projectile.timeLeft / 48f);
                Projectile.scale = MathHelper.Lerp(1.15f, 2.35f, lifeProgress);
                Projectile.alpha = (int)MathHelper.Lerp(20f, 185f, lifeProgress);
            }

            int spinDirection = 1;

            if (Projectile.velocity.X < 0f)
            {
                spinDirection = -1;
            }

            if (cinderRain)
            {
                int age = RainLifetimeTicks - Projectile.timeLeft;
                Projectile.rotation = Projectile.ai[0] + age * 0.36f * spinDirection;
            }
            else
            {
                Projectile.rotation += 0.24f * spinDirection;
            }

            if (Main.dedServ)
            {
                return;
            }

            Lighting.AddLight(Projectile.Center, new Color(232, 30, 86).ToVector3() * 0.55f);

            int bodyCount = cinderRain ? 2 : 1;
            int accentChance = cinderRain ? 2 : 3;
            for (int i = 0; i < bodyCount; i++)
            {
                Dust body = Dust.NewDustPerfect(
                    Projectile.Center + Main.rand.NextVector2Circular(5f, 5f),
                    DustID.Blood,
                    -Projectile.velocity * Main.rand.NextFloat(0.025f, 0.07f)
                        + Main.rand.NextVector2Circular(0.25f, 0.25f),
                    95, default, Main.rand.NextFloat(0.75f, 1.2f));
                body.noGravity = true;
            }

            if (Main.rand.NextBool(accentChance))
            {
                Dust spark = Dust.NewDustPerfect(Projectile.Center,
                    DustID.TintableDustLighted,
                    -Projectile.velocity * Main.rand.NextFloat(0.08f, 0.16f)
                        + Main.rand.NextVector2Circular(0.35f, 0.35f),
                    75, new Color(255, 72, 150), Main.rand.NextFloat(0.42f, 0.72f));
                spark.noGravity = true;
            }

            if (Main.rand.NextBool(accentChance))
            {
                Dust soot = Dust.NewDustPerfect(Projectile.Center,
                    DustID.Wraith,
                    -Projectile.velocity * Main.rand.NextFloat(0.04f, 0.1f)
                        + Main.rand.NextVector2Circular(0.3f, 0.3f),
                    145, default, Main.rand.NextFloat(0.55f, 0.9f));
                soot.noGravity = true;
                soot.noLight = true;
            }
        }

        public override bool TileCollideStyle(ref int width, ref int height, ref bool fallThrough, ref Vector2 hitboxCenterFrac)
        {
            if (IsCinderRain)
            {
                // Fall through arena platforms so drops reach the solid floor below them.
                fallThrough = true;
            }

            return true;
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            rainGroundImpact = IsCinderRain && oldVelocity.Y > 0f && Projectile.velocity.Y < oldVelocity.Y;
            return true;
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(ModContent.BuffType<DestinedDeath>(), 600);
            if (Main.rand.NextBool(2))
            {
                target.AddBuff(BuffID.Bleeding, 1800);
                target.AddBuff(BuffID.Weak, 150);
            }
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.dedServ)
            {
                return;
            }

            int burstCount = rainGroundImpact ? 18 : 10;
            Vector2 origin = rainGroundImpact ? Projectile.Bottom : Projectile.Center;
            for (int i = 0; i < burstCount; i++)
            {
                bool wraith = i % 3 == 0;
                Vector2 velocity = Main.rand.NextVector2Circular(2.2f, 2.2f);
                if (rainGroundImpact)
                {
                    velocity.X *= 1.6f;
                    float riseFraction = Math.Abs(velocity.Y) / 2.2f;
                    velocity.Y = -MathHelper.Lerp(0.55f, 2.8f, riseFraction);
                }

                Dust mote = Dust.NewDustPerfect(origin,
                    wraith ? DustID.Wraith : DustID.Blood,
                    velocity, wraith ? 145 : 95, default, Main.rand.NextFloat(0.48f, 0.95f));
                mote.noGravity = true;
                mote.noLight = wraith;
            }
        }
    }

    public class CinderRainController : ModProjectile
    {
        const int RainDurationTicks = 330;
        const int MinimumVolleyDelay = 7;
        const int MaximumVolleyDelay = 11;
        const float HalfWidth = 300f;
        const float MaximumSpawnHeight = 550f;
        const float MinimumSpawnHeight = 192f;

        public override string Texture => "Terraria/Images/MagicPixel";

        public override void SetDefaults()
        {
            Projectile.width = 2;
            Projectile.height = 2;
            Projectile.timeLeft = RainDurationTicks;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
        }

        public override bool ShouldUpdatePosition() => false;
        public override bool? CanDamage() => false;
        public override bool PreDraw(ref Color lightColor) => false;

        public override void AI()
        {
            if (Main.netMode == NetmodeID.MultiplayerClient)
            {
                return;
            }

            int sourceIndex = (int)Projectile.ai[0];
            if (sourceIndex < 0 || sourceIndex >= Main.maxNPCs || !Main.npc[sourceIndex].active
                || Main.npc[sourceIndex].ModNPC is not GreatRedKnight knight || knight.InDominionDeathSequence)
            {
                Projectile.Kill();
                return;
            }

            Projectile.localAI[0]--;
            if (Projectile.localAI[0] > 0f)
            {
                return;
            }

            Projectile.localAI[0] = Main.rand.Next(MinimumVolleyDelay, MaximumVolleyDelay + 1);
            int dropCount = Main.rand.NextBool(3) ? 2 : 1;
            for (int drop = 0; drop < dropCount; drop++)
            {
                // Sample the full footprint each beat. Blocked columns get a bounded resample.
                for (int attempt = 0; attempt < 6; attempt++)
                {
                    float offset = Main.rand.NextFloat(-HalfWidth, HalfWidth);
                    float height = Main.rand.NextFloat(450f, MaximumSpawnHeight);
                    if (!TryFindOrigin(Projectile.Center, offset, height, out Vector2 origin))
                    {
                        continue;
                    }

                    Vector2 velocity = new Vector2(Main.rand.NextFloat(-0.45f, 0.45f), Main.rand.NextFloat(6.6f, 8.2f));
                    Projectile.NewProjectile(Projectile.GetSource_FromThis(), origin, velocity,
                        ModContent.ProjectileType<DestinedDeathBreath>(), Projectile.damage, 2f, Main.myPlayer,
                        ai0: Main.rand.NextFloat(MathHelper.TwoPi), ai1: DestinedDeathBreath.CinderRainMode);
                    break;
                }
            }
        }

        internal static bool HasUsableLane(Vector2 anchor)
        {
            int usable = 0;
            for (float offset = -HalfWidth; offset <= HalfWidth; offset += HalfWidth * 0.5f)
            {
                if (TryFindOrigin(anchor, offset, MaximumSpawnHeight, out _))
                {
                    usable++;
                }
            }

            return usable >= 2;
        }

        static bool TryFindOrigin(Vector2 anchor, float offset, float desiredHeight, out Vector2 origin)
        {
            float laneX = anchor.X + offset;
            Vector2 laneEnd = new Vector2(laneX, anchor.Y);
            for (float height = desiredHeight; height >= MinimumSpawnHeight; height -= 16f)
            {
                Vector2 candidate = new Vector2(laneX, anchor.Y - height);
                Vector2 hitboxTopLeft = candidate - new Vector2(8f);
                // Try under ceiling tiles while preserving ~23 ticks of visible fall even at max speed.
                if (!Collision.SolidCollision(hitboxTopLeft, 16, 16)
                    && Collision.CanHitLine(candidate, 2, 2, laneEnd, 2, 2))
                {
                    origin = candidate;
                    return true;
                }
            }

            origin = Vector2.Zero;
            return false;
        }
    }
}

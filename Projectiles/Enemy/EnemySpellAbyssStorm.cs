using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    class EnemySpellAbyssStorm : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Dark Wave Storm");
        }

        public override void SetDefaults()
        {
            Projectile.width = 194;
            Projectile.height = 194;
            DrawOriginOffsetX = -96;
            DrawOriginOffsetY = 94;
            Main.projFrames[Projectile.type] = 7;
            Projectile.hostile = true;
            Projectile.penetrate = 50;
            Projectile.scale = 2;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.light = 1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = InitialLifetime;
        }

        private const float MaxStormRadius = 60f * 16f;
        private const int StormGrowTicks = 45;
        private const int StormBrakeTicks = 60;
        private const int StormReturnAccelerationTicks = 30;
        private const int ReturnDamageArmTicks = 8;
        private const int InitialLifetime = 240;
        private const float StormGrowSpeed = MaxStormRadius / StormGrowTicks;
        private const float StormReturnTopSpeed = StormGrowSpeed * 0.5f;

        private float size;

        private int Age => InitialLifetime - Projectile.timeLeft;
        private bool Braking => Age >= StormGrowTicks && Age < StormGrowTicks + StormBrakeTicks;
        private int ReturnElapsed => Age - StormGrowTicks - StormBrakeTicks + 1;

        public override void AI()
        {
            float edgeVelocity;
            if (Age < StormGrowTicks)
            {
                float growProgress = (Age + 1f) / StormGrowTicks;
                size = MaxStormRadius * MathHelper.Clamp(growProgress, 0f, 1f);
                edgeVelocity = StormGrowSpeed;
            }
            else if (Braking)
            {
                size = MaxStormRadius;
                float brakeProgress = (Age - StormGrowTicks + 1f) / StormBrakeTicks;
                edgeVelocity = MathHelper.Lerp(
                    StormGrowSpeed,
                    0f,
                    MathHelper.SmoothStep(0f, 1f, MathHelper.Clamp(brakeProgress, 0f, 1f)));
            }
            else
            {
                float returnTravel = GetReturnTravel(ReturnElapsed);
                size = Math.Max(0f, MaxStormRadius - returnTravel);
                edgeVelocity = -GetReturnSpeed(ReturnElapsed);

                if (size <= 0f)
                {
                    SpawnCenterExplosion();
                    Projectile.Kill();
                    return;
                }
            }

            DrawStormEdge(edgeVelocity);
            Lighting.AddLight(Projectile.Center, 0.08f, 0.2f, 0.42f);
        }

        public override bool? CanDamage()
        {
            if (Braking)
                return false;

            // Let the returning edge visibly pull away from its stationary pose before it rearms.
            // At that point its accelerating 32px ring crosses a player well inside one roll window.
            if (Age >= StormGrowTicks + StormBrakeTicks && ReturnElapsed <= ReturnDamageArmTicks)
                return false;

            return null;
        }

        private static float GetReturnSpeed(int elapsed)
        {
            float accelerationProgress = MathHelper.Clamp(
                elapsed / (float)StormReturnAccelerationTicks, 0f, 1f);
            return StormReturnTopSpeed * MathHelper.SmoothStep(0f, 1f, accelerationProgress);
        }

        private static float GetReturnTravel(int elapsed)
        {
            float distance = 0f;
            for (int tick = 1; tick <= elapsed; tick++)
                distance += GetReturnSpeed(tick);
            return distance;
        }

        private void SpawnCenterExplosion()
        {
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    Projectile.Center,
                    Vector2.Zero,
                    ModContent.ProjectileType<EnemySpellAbyssStormExplosion>(),
                    Projectile.damage,
                    Projectile.knockBack,
                    Projectile.owner);
            }
        }

        private static int GetStormDustCount(float radius)
        {
            return (int)MathHelper.Clamp(MathHelper.TwoPi * radius / 12f, 24f, 260f);
        }

        private void DrawStormEdge(float outwardSpeed)
        {
            // The phase lasts much longer now, so stagger a third of the old circumference sample
            // per tick. Persistent dust fills the gaps without saturating Terraria's global dust pool.
            int count = Math.Max(8, GetStormDustCount(size) / 3);
            float phase = Projectile.localAI[0] * 0.08f;

            for (int j = 0; j < count; j++)
            {
                float rotation = phase + MathHelper.TwoPi * j / count;
                Vector2 direction = rotation.ToRotationVector2();
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center + direction * size,
                    DustID.BlueCrystalShard,
                    direction * outwardSpeed,
                    170,
                    default,
                    1.15f);

                dust.noGravity = true;
                dust.fadeIn = 0.2f;
            }

            Projectile.localAI[0]++;
        }

        //Circular collision
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float distance = Vector2.Distance(projHitbox.Center.ToVector2(), targetHitbox.Center.ToVector2());
            if (distance < size && distance > size - 32)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            return false;
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            if (Main.expertMode)
            {
                target.AddBuff(BuffID.Frostburn, 225, false);
            }
            else
            {
                target.AddBuff(BuffID.Frostburn, 450, false);
            }
        }
    }
}

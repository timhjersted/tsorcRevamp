using Microsoft.Xna.Framework;
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
        }

        float size = 0;
        int dustCount = 0;
        const float MaxStormRadius = 60f * 16f;
        const float StormGrowTime = 45f;
        const float StormGrowSpeed = MaxStormRadius / StormGrowTime;

        public override void AI()
        {
            bool growing = size < MaxStormRadius;
            if (size < MaxStormRadius)
            {
                size = MathHelper.Min(size + StormGrowSpeed, MaxStormRadius);
                dustCount = GetStormDustCount(size);
            }
            else
            {
                //Fade out after reaching max radius, and then despawn
                dustCount /= 2;
                if (dustCount <= 0)
                {
                    Projectile.Kill();
                    return;
                }
            }

            DrawStormEdge(growing ? StormGrowSpeed : 0f);
        }

        private static int GetStormDustCount(float radius)
        {
            return (int)MathHelper.Clamp(MathHelper.TwoPi * radius / 12f, 24f, 260f);
        }

        private void DrawStormEdge(float outwardSpeed)
        {
            int count = dustCount < 1 ? 1 : dustCount;
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

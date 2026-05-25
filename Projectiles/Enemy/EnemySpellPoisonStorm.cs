using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    public class EnemySpellPoisonStorm : ModProjectile
    {

        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("Enemy Spell Poison Storm");
            Main.projFrames[Projectile.type] = 7;
        }
        public override void SetDefaults()
        {

            Projectile.aiStyle = -1;
            Projectile.friendly = false;
            Projectile.hostile = true;
            Projectile.width = 190;
            Projectile.height = 190;
            Projectile.light = 1f;
            Projectile.penetrate = 1;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.scale = 2f;
            Projectile.tileCollide = true;
            DrawOriginOffsetY = 95;
            DrawOriginOffsetX = -95;
        }
        float size = 0;
        int dustCount = 0;
        const float MaxStormRadius = 300f;
        const float StormGrowTime = 30f;
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
                dustCount = (int)(dustCount / 1.1f);
                if (dustCount <= 0)
                {
                    Projectile.Kill();
                    return;
                }
            }

            // Let fresh dust ride the expanding edge instead of lingering inside the
            // circle, which keeps the damaging band readable during the whole growth.
            DrawStormEdge(growing ? StormGrowSpeed : 0f);
        }

        private static int GetStormDustCount(float radius)
        {
            return (int)MathHelper.Clamp(MathHelper.TwoPi * radius / 12f, 24f, 180f);
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
                    DustID.CursedTorch,
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
            if (distance < size && distance > size - 16)
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
            target.AddBuff(BuffID.Poisoned, 900, false);
        }
    }
}

using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles
{
    class Fireball1 : ModProjectile
    {

        public override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.scale = 1.1f;
            Projectile.timeLeft = 120;
            Projectile.hostile = false;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = false;
            Projectile.friendly = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 60;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.scale = 0.7f;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.OnFire, 300);
        }

        public override void AI()
        {
            Projectile.rotation += 0.25f;
            for (int i = 0; i < 2; i++)
            {
                Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(8, 8), DustID.InfernoFork, Main.rand.NextVector2Circular(2, 2), Scale: 0.7f).noGravity = true;
            }

            if (Projectile.wet)
            {
                Projectile.Kill();
            }

            if (Projectile.wet)
            {
                Projectile.Kill();
            }
        }
    }
}

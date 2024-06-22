using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles
{
    class Ice2Icicle : ModProjectile
    {
        public override void SetDefaults()
        {
            Projectile.width = 24;
            Projectile.height = 64;
            Projectile.friendly = true;
            Projectile.penetrate = 3;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 90;
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (!target.boss)
            {
                target.AddBuff(Terraria.ID.BuffID.Slow, 120);
                if (Main.rand.NextBool(30))
                {
                    target.AddBuff(Terraria.ID.BuffID.Frozen, 120);
                }
            }
        }
        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.ToRadians(90);

            if (Projectile.timeLeft <= 30)
            {
                Projectile.alpha += 6;
            }

            //keep a portion of the projectile's velocity when spawned, so we canmake sure it has the right knockback
            if (Projectile.ai[0] == 0)
            {
                Projectile.velocity.X *= 0.001f;
                Projectile.ai[0] = 1;
            }
        }
    }
}

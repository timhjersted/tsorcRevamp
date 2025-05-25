using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;


namespace tsorcRevamp.Projectiles
{
    class SpearOfMage : ModProjectile
    {
        public override void SetStaticDefaults()
        {
        }
        public override void SetDefaults()
        {
            Projectile.width = 22;
            Projectile.height = 138;
            Projectile.timeLeft = 120;
            Projectile.light = 0.5f;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.ownerHitCheck = false;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.tileCollide = false;
            Projectile.scale = 1f;

        }

        public override void AI()
        {
            Projectile.ai[0] += 1f;
            if (++Projectile.frameCounter >= 3)
            {
                Projectile.frameCounter = 0;
                if (++Projectile.frame >= 3)
                {
                    Projectile.frame = 0;
                }
            }
            if (Projectile.ai[0] >= 15f)
            { //this is the bit that makes it arc down
                Projectile.ai[0] = 15f;
                Projectile.velocity.Y += 0.1f;
            }
            if (Projectile.velocity.Y > 16f)
            { //this bit caps down velocity, and thus also caps down rotation if fired at a positive angle
                Projectile.velocity.Y = 16f;
            }
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.ToRadians(90f); //simplified rotation code (no trig!)
        }
    }
}

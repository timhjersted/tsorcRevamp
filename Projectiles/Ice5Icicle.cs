using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles
{
    class Ice5Icicle : ModProjectile
    {

        public override void SetDefaults()
        {
            Projectile.width = 32;
            Projectile.height = 88;
            Projectile.friendly = true;
            Projectile.penetrate = 5;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 200;
        }

        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation() + 1.57f; //90 degrees in radians!

            Lighting.AddLight(Projectile.Center, Color.BlueViolet.ToVector3() * .75f);

            if (Projectile.ai[0] >= 1)
            {
                //I intentionally leave Projectile.friendly = true, to make this deal 'friendly fire' against the Wyvern Mage's allies
                Projectile.hostile = true;
                Projectile.timeLeft = 400;
                Projectile.ai[0] = 0;
            }
        }
    }
}

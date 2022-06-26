using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles
{
    class Shockwave : ModProjectile
    {

        public override string Texture => "tsorcRevamp/Projectiles/Sand";
        public override void SetDefaults()
        {
            Projectile.width = 150;
            Projectile.height = 150;
            Projectile.timeLeft = 300;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.damage = 60;
            Projectile.friendly = true;
            Projectile.penetrate = 99;
            Projectile.alpha = 255;
            Projectile.usesIDStaticNPCImmunity = true; // only one of these can hit a target at once
            Projectile.idStaticNPCHitCooldown = 20; // after which it becomes immune to this projectile for 20 frames
        }

        public override void AI()
        {
            Projectile.ai[0]++;
            int dust = Dust.NewDust(new Vector2(Projectile.position.X, Projectile.position.Y - 10), Projectile.width, Projectile.height, 31, 0, 0, 100, default, 1.0f);
            Main.dust[dust].noGravity = true;
            int dust2 = Dust.NewDust(new Vector2(Projectile.position.X, Projectile.position.Y - 20), Projectile.width, Projectile.height, 31, 0, 0, 100, default, 1.0f);
            Main.dust[dust2].noGravity = true;
            int dust3 = Dust.NewDust(new Vector2(Projectile.position.X, Projectile.position.Y - 30), Projectile.width, Projectile.height, 31, 0, 0, 100, default, 1.0f);
            Main.dust[dust3].noGravity = true;

            if (Projectile.ai[0] > 10)
            { //delay the damage reduction (feels better)
                Projectile.damage = (int)(Projectile.damage * 0.925f); //scale down the damage as it gets further from the impact location
                if (Projectile.damage <= 2)
                {
                    Projectile.Kill();
                }
            }
        }
    }
}

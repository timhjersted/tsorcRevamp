using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles
{
    class Sand : ModProjectile
    {

        public override void SetDefaults()
        {
            Projectile.damage = 166;
            Projectile.aiStyle = 0;
            Projectile.friendly = true;
            Projectile.penetrate = 1;
            Projectile.tileCollide = true;
            Projectile.usesIDStaticNPCImmunity = true;
            Projectile.idStaticNPCHitCooldown = 6;
        }
        public override bool PreKill(int timeLeft)
        {
            Projectile.type = 15;
            return true;
        }
        public override void OnSpawn(IEntitySource source)
        {
            if (Projectile.ai[0] != 0)
            {
                Projectile.timeLeft = (int)Projectile.ai[0];
            }
            if (Projectile.ai[1] != 0)
            {
                Projectile.scale = Projectile.ai[1];
            }
        }
        public override void AI()
        {
            int D = Dust.NewDust(new Vector2((float)Projectile.position.X, (float)Projectile.position.Y), Projectile.width, Projectile.height, 10, 0, 0, 100, default, 2.0f);
            Main.dust[D].noGravity = true;
        }
    }
}

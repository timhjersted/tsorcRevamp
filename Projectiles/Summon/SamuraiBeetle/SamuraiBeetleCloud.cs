using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Weapons.Summon;

namespace tsorcRevamp.Projectiles.Summon.SamuraiBeetle
{
    public class SamuraiBeetleCloud : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 8;
        }
        public override void SetDefaults()
        {
            Projectile.width = 106;
            Projectile.height = 32;
            Projectile.friendly = true;
            Projectile.timeLeft = 180;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
        }

        public override void AI()
        {
            int frameSpeed = 3;

            Projectile.frameCounter++;

            if (Projectile.frameCounter >= frameSpeed)
            {
                Projectile.frameCounter = 0;
                Projectile.frame++;

                if (Projectile.frame >= Main.projFrames[Type])
                {
                    Projectile.Kill();
                }
            }
        }
    }
}

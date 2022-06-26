using System;
using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles
{
    class Bolt1Bolt : ModProjectile
    {

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 4;
        }

        public override void SetDefaults()
        {
            Projectile.width = 60;
            Projectile.height = 110;
            Projectile.penetrate = 4;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.light = 0.6f;

        }
        public override void OnHitNPC(NPC target, int damage, float knockback, bool crit)
        {
            if (Main.rand.Next(5) == 0)
            {
                target.AddBuff(Mod.Find<ModBuff>("ElectrocutedBuff").Type, 120);
            }
        }
        public override void AI()
        {
            //keep a portion of the projectile's velocity when spawned, so we canmake sure it has the right knockback
            if (Projectile.ai[0] == 0)
            {
                Projectile.velocity.X *= 0.001f;
                Projectile.velocity.Y *= 0.001f;
                Projectile.ai[0] = 1;
            }
            Projectile.frameCounter++;
            Projectile.frame = (int)Math.Floor((double)Projectile.frameCounter / 4);

            if (Projectile.frame >= 4)
            {
                Projectile.frame = 2;
            }
            if (Projectile.frameCounter > 17)
            { // (projFrames * 4.5) - 1
                Projectile.alpha += 15;
            }

            if (Projectile.alpha >= 255)
            {
                Projectile.Kill();
            }

        }
    }
}

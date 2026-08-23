using System;
using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Systems.Electrocute
{
    class ElectrocuteProjectile : ModProjectile
    {

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 4;
        }

        public override void SetDefaults()
        {
            Projectile.width = 60;
            Projectile.height = 110;
            Projectile.penetrate = -1;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.light = 0.6f;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.DamageType = DamageClass.Ranged;
        }
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
        }

        public bool AppliedOnSpawn = false;
        public override void AI()
        {
            //keep a portion of the projectile's velocity when spawned, so we canmake sure it has the right knockback
            if (!AppliedOnSpawn)
            {
                Projectile.velocity.X *= 0.001f;
                Projectile.velocity.Y *= 0.001f;
                Projectile.CritChance = (int)Projectile.ai[0];
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

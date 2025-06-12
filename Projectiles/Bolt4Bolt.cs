using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles
{
    class Bolt4Bolt : ModProjectile
    {

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 16;
        }

        public override void SetDefaults()
        {
            Projectile.width = 254;
            Projectile.height = 472;
            Projectile.penetrate = 8;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.light = 0.8f;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 25;
            Projectile.DamageType = DamageClass.Magic;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(Terraria.ID.BuffID.Slow, 180);
            target.AddBuff(Terraria.ID.BuffID.Electrified, 360);
        }

        public override void AI()
        {
            if (Projectile.ai[1] == 0f)
            {
                Terraria.Audio.SoundEngine.PlaySound(SoundID.DD2_EtherianPortalDryadTouch with { Volume = 0.8f }, Projectile.Center); //lightning sound
                Projectile.ai[1] = 1f;

            }

            if (Projectile.ai[0] == 0)
            {
                Projectile.velocity.X *= 0.001f;
                Projectile.velocity.Y *= 0.001f;
                Projectile.ai[0] = 1;
            }

            Projectile.frameCounter++;
            Projectile.frame = (int)Math.Floor((double)Projectile.frameCounter / 4);

            if (Projectile.frame >= 16)
            {
                Projectile.frame = 15;
            }
            if (Projectile.frameCounter > 71)
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


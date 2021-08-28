using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles
{
    class PoisonField2 : ModProjectile
    {
        public override string Texture => "tsorcRevamp/Projectiles/PoisonField";

        public override void SetStaticDefaults()
        {
            Main.projFrames[projectile.type] = 5;
        }

        public override void SetDefaults()
        {
            projectile.width = 32;
            projectile.height = 28;
            projectile.friendly = true;
            projectile.magic = true;
            projectile.tileCollide = false;
            projectile.ignoreWater = true;
            projectile.timeLeft = 300;
            projectile.alpha = 90;
            projectile.light = 0.3f;
            projectile.penetrate = 10;
            drawOffsetX = -4;
            drawOriginOffsetY = -10;
        }
        public override void OnHitNPC(NPC target, int damage, float knockback, bool crit)
        {
            if (Main.rand.Next(2) == 0)
            {
                target.AddBuff(BuffID.Poisoned, 580);
            }
        }

        public override void AI()
        {

            if (Main.rand.Next(10) == 0)
            {
                int dust = Dust.NewDust(projectile.position, projectile.width, projectile.height, 74, projectile.velocity.X * 0f, projectile.velocity.Y * 0f, 100, default(Color), .8f); ;
                Main.dust[dust].velocity *= 0f;
                Main.dust[dust].noGravity = true;
                Main.dust[dust].velocity += projectile.velocity;
                Main.dust[dust].fadeIn = 1f;
            }

            if (projectile.timeLeft <= 55)
            {
                projectile.alpha += 3;
            }


            if (projectile.ai[0] == 0)
            {
                projectile.velocity.X *= 0.001f;
                projectile.velocity.Y *= 0.001f;
                projectile.ai[0] = 1;
            }

            projectile.frameCounter++;
            if (projectile.frameCounter > 5)
            {
                projectile.frame++;
                projectile.frameCounter = 0;
            }
            if (projectile.frame >= 5)
            {
                projectile.frame = 0;
                return;
            }
        }
    }
}

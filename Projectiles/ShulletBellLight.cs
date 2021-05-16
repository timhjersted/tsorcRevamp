using Terraria;
using Terraria.ModLoader;
using System;
using Microsoft.Xna.Framework;


namespace tsorcRevamp.Projectiles
{
    public class ShulletBellLight : ModProjectile
    {
        public override void SetDefaults()
        {
            drawHeldProjInFrontOfHeldItemAndArms = true; // Makes projectile appear in front of arms, not just in between body and arms
            projectile.height = 4;
            projectile.width = 4;
            projectile.scale = 0.4f;
            projectile.hostile = false;
            projectile.friendly = true;
            projectile.timeLeft = 600;
            projectile.tileCollide = true;

            drawOffsetX = -5;
            drawOriginOffsetY = -8;
        }
        public override void AI()
        {
            Player projOwner = Main.player[projectile.owner];
            projOwner.heldProj = projectile.whoAmI; //this makes it appear in front of the player

            if (projectile.timeLeft < 255)
            {
                projectile.alpha += 1;
            }

            projectile.ai[0] += 1f;
            if (projectile.ai[0] > 7f)
            {
                projectile.ai[0] = 10f;
                // Roll speed dampening.
                if (projectile.velocity.Y == 0f && projectile.velocity.X != 0f)
                {
                    projectile.velocity.X = projectile.velocity.X * 0.95f;

                    if ((double)projectile.velocity.X > -0.01 && (double)projectile.velocity.X < 0.01)
                    {
                        projectile.velocity.X = 0f;
                        projectile.netUpdate = true;
                    }
                }
                projectile.velocity.Y = projectile.velocity.Y + 0.2f;
            }
            projectile.rotation += projectile.velocity.X * 0.4f;

            projectile.velocity.Y = projectile.velocity.Y + 0.1f; // 0.1f for arrow gravity, 0.4f for knife gravity
            if (projectile.velocity.Y > 16f) // This check implements "terminal velocity". We don't want the projectile to keep getting faster and faster. Past 16f this projectile will travel through blocks, so this check is useful.
            {
                projectile.velocity.Y = 16f;
            }
            return;
        }
        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            if (projectile.velocity.X != oldVelocity.X && Math.Abs(oldVelocity.X) > 1f)
            {
                projectile.velocity.X = oldVelocity.X * -0.3f;
            }
            if (projectile.velocity.Y != oldVelocity.Y && Math.Abs(oldVelocity.Y) > 1f)
            {
                projectile.velocity.Y = oldVelocity.Y * -0.3f;
            }
            return false;
        }
        public override bool CanDamage()
        {
            return false;
        }
    }
}
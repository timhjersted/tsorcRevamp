using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Items.Weapons.Ranged;
using Microsoft.Xna.Framework;
using Terraria;
using System;

namespace tsorcRevamp.Projectiles
{
    class ToxicCatExplosion : ModProjectile
    {
        public override void SetDefaults()
        {

            // while the sprite is actually bigger than 15x15, we use 15x15 since it lets the projectile clip into tiles as it bounces. It looks better.
            projectile.width = 40;
            projectile.height = 40;
            projectile.friendly = true;
            projectile.aiStyle = 0;
            projectile.ranged = true;
            projectile.tileCollide = false;
            projectile.timeLeft = 2;
            projectile.penetrate = -1; //this can be removed to only damage the host
            drawOffsetX = -2;
            drawOriginOffsetY = -2;
            projectile.usesLocalNPCImmunity = true; //any amount of explosions can damage a target simultaneously
            projectile.localNPCHitCooldown = -1; //but a single explosion can never damage the same enemy more than once
        }

        public override void AI()
        {
            if (projectile.owner == Main.myPlayer && projectile.timeLeft <= 2)
            {
                projectile.tileCollide = false;
                // Set to transparent. This projectile technically lives as  transparent for about 2 frames
                projectile.alpha = 255;
                // change the hitbox size, centered about the original projectile center. This makes the projectile damage enemies during the explosion.
                projectile.position.X = projectile.position.X + (float)(projectile.width / 2);
                projectile.position.Y = projectile.position.Y + (float)(projectile.height / 2);
                projectile.width = 30;
                projectile.height = 30;
                projectile.position.X = projectile.position.X - (float)(projectile.width / 2);
                projectile.position.Y = projectile.position.Y - (float)(projectile.height / 2);
                projectile.damage = 30; //DAMAGE OF EXPLOSION when fuse runs out, not when collidew/npc
                projectile.knockBack = 5f;
                //projectile.thrown = true;

                for (int i = 0; i < 40; i++)
                {
                    int dustIndex = Dust.NewDust(new Vector2(projectile.position.X, projectile.position.Y), projectile.width, projectile.height, 75, 0f, 0f, 100, default(Color), 2f);
                    Main.dust[dustIndex].noGravity = true;
                    Main.dust[dustIndex].velocity *= 2.5f;
                    dustIndex = Dust.NewDust(new Vector2(projectile.position.X, projectile.position.Y), projectile.width, projectile.height, 107, 0f, 0f, 100, default(Color), 1f);
                    Main.dust[dustIndex].velocity *= 3.5f;
                    Main.dust[dustIndex].noGravity = true;
                    dustIndex = Dust.NewDust(new Vector2(projectile.position.X, projectile.position.Y), projectile.width, projectile.height, 107, 0f, 0f, 100, default(Color), 1.5f);
                    Main.dust[dustIndex].velocity *= 2.5f;
                    Main.dust[dustIndex].noGravity = true;
                    dustIndex = Dust.NewDust(new Vector2(projectile.position.X, projectile.position.Y), projectile.width, projectile.height, 75, 0f, 0f, 100, default(Color), 1.5f);
                    Main.dust[dustIndex].velocity *= 3f;
                    Main.dust[dustIndex].noGravity = true;
                }
            }
        }
    }
}
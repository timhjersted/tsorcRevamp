using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

//using tsorcRevamp.Dusts;

namespace tsorcRevamp.Projectiles
{
    class EnchantedThrowingSpear : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            Main.projFrames[projectile.type] = 3;
        }
        public override void SetDefaults()
        {
            projectile.width = 45;
            projectile.height = 45;
            projectile.penetrate = 4;
            //we dont need a timeLeft if we kill the projectile in the AI 
            //projectile.timeLeft = 3600; 
            projectile.light = 0.5f;
            projectile.friendly = true; //can hit enemies
            projectile.hostile = false; //can hit player / friendly NPCs
            projectile.ownerHitCheck = false;
            projectile.melee = false;
            projectile.tileCollide = false;
            projectile.hide = false;
            //projectile.usesLocalNPCImmunity = true;
            //projectile.localNPCHitCooldown = 5;
            projectile.scale = 1f;

        }

        public float moveFactor
        { //controls spear speed
            get => projectile.ai[0];
            set => projectile.ai[0] = value;
        }

        public override void AI()
        {
            projectile.ai[0] += 1f;
            if (++projectile.frameCounter >= 3)
            {
                projectile.frameCounter = 0;
                if (++projectile.frame >= 3)
                {
                    projectile.frame = 0;
                }
            }
            if (projectile.ai[0] >= 15f) { //this is the bit that makes it arc down
                projectile.ai[0] = 15f;
                projectile.velocity.Y += 0.1f;
            }
            if (projectile.velocity.Y > 16f) { //this bit caps down velocity, and thus also caps down rotation if fired at a positive angle
                projectile.velocity.Y = 16f;
            }
            // Kill this projectile after 10 second
            if (projectile.ai[0] >= 600f)
            {
                Main.PlaySound(SoundID.Dig, (int)projectile.position.X, (int)projectile.position.Y, 1);
                for (int i = 0; i < 10; i++)
                {
                    Vector2 projPosition = new Vector2(projectile.position.X, projectile.position.Y);
                    Dust.NewDust(projPosition, projectile.width, projectile.height, 7, 0f, 0f, 0, default(Color), 1f);
                }
                projectile.Kill();
            }

            projectile.rotation = projectile.velocity.ToRotation() + MathHelper.ToRadians(90f); //simplified rotation code (no trig!)
            

        }

    }

}

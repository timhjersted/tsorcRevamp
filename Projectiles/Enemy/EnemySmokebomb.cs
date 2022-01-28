using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.GameContent.Achievements;

namespace tsorcRevamp.Projectiles.Enemy
{
    class EnemySmokebomb : ModProjectile
    {

        public override void SetDefaults()
        {
            projectile.aiStyle = 1;
            projectile.width = 14;
            projectile.height = 15;
            projectile.hostile = true;
            projectile.timeLeft = 840;
            projectile.penetrate = -1;
            projectile.knockBack = 22;
            projectile.thrown = true;
            projectile.light = 1;
            projectile.scale = 1f;

            // These 2 help the projectile hitbox be centered on the projectile sprite.
            drawOffsetX = -5;
            drawOriginOffsetY = -5;
        }
        public override void ModifyHitPlayer(Player target, ref int damage, ref bool crit)
        {
            projectile.timeLeft = 2; //sets it to 2 frames, to let the explosion ai kick in. Setdefaults is -1 pen, this allows it to only hit one npc, then run explosion ai.
            projectile.netUpdate = true;
        }
        public override void OnHitPlayer(Player target, int damage, bool crit)
        {
            target.AddBuff(BuffID.PotionSickness, 600); 
            projectile.timeLeft = 2;
        }
        
        
        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            projectile.timeLeft = 2;
            return true;
        }
        
        public override void AI()
        {
            projectile.rotation += 1f;
            if (Main.rand.Next(2) == 0)
            {
                int dust = Dust.NewDust(new Vector2((float)projectile.position.X, (float)projectile.position.Y), projectile.width, projectile.height, 6, 0, 0, 50, Color.Green, 1.0f);
                Main.dust[dust].noGravity = false;
            }
            Lighting.AddLight((int)(projectile.position.X / 16f), (int)(projectile.position.Y / 16f), 0.4f, 0.1f, 0.1f);

            //if (projectile.velocity.X <= 4 && projectile.velocity.Y <= 4 && projectile.velocity.X >= -4 && projectile.velocity.Y >= -4)
            //{
            //    float accel = 1f + (Main.rand.Next(10, 30) * 0.001f);
            //    projectile.velocity.X *= accel;
            //    projectile.velocity.Y *= accel;
            //}

            if (projectile.timeLeft <= 2)
            {
                projectile.tileCollide = false;
                // Set to transparent. This projectile technically lives as  transparent for about 2 frames
                projectile.alpha = 255;
                // change the hitbox size, centered about the original projectile center. This makes the projectile damage enemies during the explosion.
                projectile.position.X = projectile.position.X + (float)(projectile.width / 2);
                projectile.position.Y = projectile.position.Y + (float)(projectile.height / 2);
                projectile.width = 120;
                projectile.height = 120;
                projectile.position.X = projectile.position.X - (float)(projectile.width / 2);
                projectile.position.Y = projectile.position.Y - (float)(projectile.height / 2);
                projectile.damage = 22; //DAMAGE OF EXPLOSION when fuse runs out, not when collidew/npc
                projectile.knockBack = 22f;
                projectile.thrown = true;
            }
            else
            {
                // Smoke and fuse dust spawn.
                if (Main.rand.Next(4) == 0)
                {
                    int dustIndex = Dust.NewDust(new Vector2(projectile.position.X, projectile.position.Y), projectile.width, projectile.height, 6, 0f, 0f, 100, Color.SeaGreen, 1f);
                    Main.dust[dustIndex].scale = 0.1f + (float)Main.rand.Next(5) * 0.1f;
                    Main.dust[dustIndex].fadeIn = .5f + (float)Main.rand.Next(5) * 0.1f;
                    Main.dust[dustIndex].noGravity = true;
                    
                }
            }
            
            

            
        }

        public override void Kill(int timeLeft)
        {
            // Play explosion sound
            Main.PlaySound(SoundID.Item74.WithPitchVariance(.5f), projectile.position);
            // Fire Dust spawn
            for (int i = 0; i < 200; i++)
            {
                int dustIndex = Dust.NewDust(new Vector2(projectile.position.X + 36, projectile.position.Y + 36), projectile.width - 74, projectile.height - 74, 6, Main.rand.Next(-6, 6), Main.rand.Next(-6, 6), 100, default(Color), 2f);
                Main.dust[dustIndex].noGravity = true;
                Main.dust[dustIndex].velocity *= 3.5f;
            }
            // Large Smoke Gore spawn
            for (int g = 0; g < 2; g++)
            {
                int goreIndex = Gore.NewGore(new Vector2(projectile.position.X + (float)(projectile.width / 2) - 24f, projectile.position.Y + (float)(projectile.height / 2) - 24f), default(Vector2), Main.rand.Next(61, 64), .8f);
                Main.gore[goreIndex].scale = 1f;
                Main.gore[goreIndex].velocity.X = Main.gore[goreIndex].velocity.X + 1f;
                Main.gore[goreIndex].velocity.Y = Main.gore[goreIndex].velocity.Y + 1f;
                goreIndex = Gore.NewGore(new Vector2(projectile.position.X + (float)(projectile.width / 2) - 24f, projectile.position.Y + (float)(projectile.height / 2) - 24f), default(Vector2), Main.rand.Next(61, 64), .8f);
                Main.gore[goreIndex].scale = 1f;
                Main.gore[goreIndex].velocity.X = Main.gore[goreIndex].velocity.X - 1f;
                Main.gore[goreIndex].velocity.Y = Main.gore[goreIndex].velocity.Y + 1f;

            }
            // reset size to normal width and height.
            projectile.position.X = projectile.position.X + (float)(projectile.width / 2);
            projectile.position.Y = projectile.position.Y + (float)(projectile.height / 2);
            projectile.width = 14;
            projectile.height = 15;
            projectile.position.X = projectile.position.X - (float)(projectile.width / 2);
            projectile.position.Y = projectile.position.Y - (float)(projectile.height / 2);
        }
    }
}

using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles
{
    class BlackFirebomb : ModProjectile
    {

        public override void SetDefaults()
        {
            projectile.width = 15;
            projectile.height = 15;
            projectile.hostile = false;
            projectile.friendly = true;
            projectile.timeLeft = 240;
            projectile.penetrate = -1;
            projectile.knockBack = 9;
            projectile.thrown = true;
            projectile.scale = .8f;

            // These 2 help the projectile hitbox be centered on the projectile sprite.
            drawOffsetX = -5;
            drawOriginOffsetY = -5;
        }
        public override void ModifyHitNPC(NPC target, ref int damage, ref float knockback, ref bool crit, ref int hitDirection)
        {
            if (target.type == NPCID.EaterofWorldsHead || target.type == NPCID.EaterofWorldsBody || target.type == NPCID.EaterofWorldsTail)
            {
                damage /= 2;
            }
            projectile.timeLeft = 2; //sets it to 2 frames, to let the explosion ai kick in. Setdefaults is -1 pen, this allows it to only hit one npc, then run explosion ai.
            projectile.netUpdate = true;
        }
        public override void OnHitNPC(NPC target, int damage, float knockback, bool crit)
        {
            if (projectile.owner == Main.myPlayer && projectile.timeLeft <= 1) //the one frame make the explosion only deal damage once.
            {
                projectile.tileCollide = false;
                // Set to transparent. This projectile technically lives as  transparent for about 1 frames
                projectile.alpha = 255;
                // change the hitbox size, centered about the original projectile center. This makes the projectile damage enemies during the explosion.
                projectile.position.X = projectile.position.X + (float)(projectile.width / 2);
                projectile.position.Y = projectile.position.Y + (float)(projectile.height / 2);
                projectile.width = 200;
                projectile.height = 200;
                projectile.position.X = projectile.position.X - (float)(projectile.width / 2);
                projectile.position.Y = projectile.position.Y - (float)(projectile.height / 2);
                projectile.damage = 200;
                projectile.knockBack = 9f;
                projectile.thrown = true;
                projectile.netUpdate = true;
            }

            target.AddBuff(BuffID.OnFire, 600);

        }
        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            projectile.timeLeft = 2;
            return false;
        }
        public override void AI()
        {
            if (/*projectile.owner == Main.myPlayer &&*/ projectile.timeLeft <= 2)
            {
                projectile.tileCollide = false;
                // Set to transparent. This projectile technically lives as  transparent for about 2 frames
                projectile.alpha = 255;
                // change the hitbox size, centered about the original projectile center. This makes the projectile damage enemies during the explosion.
                projectile.position.X = projectile.position.X + (float)(projectile.width / 2);
                projectile.position.Y = projectile.position.Y + (float)(projectile.height / 2);
                projectile.width = 200;
                projectile.height = 200;
                projectile.position.X = projectile.position.X - (float)(projectile.width / 2);
                projectile.position.Y = projectile.position.Y - (float)(projectile.height / 2);
                projectile.damage = 200; //DAMAGE OF EXPLOSION when fuse runs out, not when collidew/npc
                projectile.knockBack = 9f;
                projectile.thrown = true;
            }
            else
            {
                // Smoke and fuse dust spawn.
                if (Main.rand.Next(4) == 0)
                {
                    int dustIndex = Dust.NewDust(new Vector2(projectile.position.X, projectile.position.Y), projectile.width, projectile.height, 6, 0f, 0f, 100, default(Color), 1f);
                    Main.dust[dustIndex].scale = 0.1f + (float)Main.rand.Next(5) * 0.1f;
                    Main.dust[dustIndex].fadeIn = .5f + (float)Main.rand.Next(5) * 0.1f;
                    Main.dust[dustIndex].noGravity = true;
                }
            }
            projectile.ai[0] += 1f;
            if (projectile.ai[0] > 5f)
            {
                projectile.ai[0] = 10f;
                // Roll speed dampening.
                if (projectile.velocity.Y == 0f && projectile.velocity.X != 0f)
                {
                    projectile.velocity.X = projectile.velocity.X * 0.97f;
                    //if (projectile.type == 29 || projectile.type == 470 || projectile.type == 637)
                    {
                        projectile.velocity.X = projectile.velocity.X * 0.99f;
                    }
                    if ((double)projectile.velocity.X > -0.01 && (double)projectile.velocity.X < 0.01)
                    {
                        projectile.velocity.X = 0f;
                        projectile.netUpdate = true;
                    }
                }
                projectile.velocity.Y = projectile.velocity.Y + 0.2f;
            }
            // Rotation increased by velocity.X 
            projectile.rotation += projectile.velocity.X * 0.08f;
            return;
        }

        public override void Kill(int timeLeft)
        {
            // Play explosion sound
            Main.PlaySound(SoundID.Item74.WithPitchVariance(.5f), projectile.position);
            projectile.damage = 40;
            projectile.knockBack = 2f;

            if (projectile.ai[1] == 0)
            {
                for (int i = 0; i < 10; i++)
                {

                    // Random upward vector.
                    Vector2 vel = new Vector2(Main.rand.NextFloat(-4f, 4f), Main.rand.NextFloat(-6, -2));
                    Projectile.NewProjectile(projectile.Center, vel, ProjectileID.MolotovFire, projectile.damage, projectile.knockBack, projectile.owner, 0, 1);
                }
            }
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
            projectile.width = 10;
            projectile.height = 10;
            projectile.position.X = projectile.position.X - (float)(projectile.width / 2);
            projectile.position.Y = projectile.position.Y - (float)(projectile.height / 2);
        }
    }
}

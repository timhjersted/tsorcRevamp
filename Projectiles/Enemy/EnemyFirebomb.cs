using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    class EnemyFirebomb : ModProjectile
    {

        public override void SetDefaults()
        {
            Projectile.width = 15;
            Projectile.height = 15;
            Projectile.hostile = true;
            Projectile.timeLeft = 240;
            Projectile.penetrate = -1;
            Projectile.knockBack = 9;
            Projectile.thrown = true;
            Projectile.scale = .8f;

            // These 2 help the projectile hitbox be centered on the projectile sprite.
            DrawOffsetX = -5;
            DrawOriginOffsetY = -5;
        }
        public override void ModifyHitPlayer(Player target, ref int damage, ref bool crit)
        {
            Projectile.timeLeft = 2; //sets it to 2 frames, to let the explosion ai kick in. Setdefaults is -1 pen, this allows it to only hit one npc, then run explosion ai.
            Projectile.netUpdate = true;
        }
        public override void OnHitPlayer(Player target, int damage, bool crit)
        {
            if (Projectile.owner == Main.myPlayer && Projectile.timeLeft <= 1) //the one frame make the explosion only deal damage once.
            {
                Projectile.tileCollide = false;
                // Set to transparent. This projectile technically lives as  transparent for about 1 frames
                Projectile.alpha = 255;
                // change the hitbox size, centered about the original projectile center. This makes the projectile damage enemies during the explosion.
                Projectile.position.X = Projectile.position.X + (float)(Projectile.width / 2);
                Projectile.position.Y = Projectile.position.Y + (float)(Projectile.height / 2);
                Projectile.width = 120;
                Projectile.height = 120;
                Projectile.position.X = Projectile.position.X - (float)(Projectile.width / 2);
                Projectile.position.Y = Projectile.position.Y - (float)(Projectile.height / 2);
                Projectile.damage = 18;
                Projectile.knockBack = 9f;
                Projectile.thrown = true;
                Projectile.netUpdate = true;
            }
        }
        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            //IF (NEW DIFFICULTY MODE) MAKE BOUNCE
            // This code makes the projectile bouncy.
            /*if (projectile.velocity.X != oldVelocity.X && Math.Abs(oldVelocity.X) > 1f)
            {
                projectile.velocity.X = oldVelocity.X * -0.5f;
            }
            if (projectile.velocity.Y != oldVelocity.Y && Math.Abs(oldVelocity.Y) > 1f)
            {
                projectile.velocity.Y = oldVelocity.Y * -0.5f;
            }
            return false;*/
            Projectile.timeLeft = 2;
            return false;
        }
        public override void AI()
        {
            if (/*projectile.owner == Main.myPlayer &&*/ Projectile.timeLeft <= 2)
            {
                Projectile.tileCollide = false;
                // Set to transparent. This projectile technically lives as  transparent for about 2 frames
                Projectile.alpha = 255;
                // change the hitbox size, centered about the original projectile center. This makes the projectile damage enemies during the explosion.
                Projectile.position.X = Projectile.position.X + (float)(Projectile.width / 2);
                Projectile.position.Y = Projectile.position.Y + (float)(Projectile.height / 2);
                Projectile.width = 120;
                Projectile.height = 120;
                Projectile.position.X = Projectile.position.X - (float)(Projectile.width / 2);
                Projectile.position.Y = Projectile.position.Y - (float)(Projectile.height / 2);
                Projectile.damage = 18; //DAMAGE OF EXPLOSION when fuse runs out, not when collidew/npc
                Projectile.knockBack = 9f;
                Projectile.thrown = true;
            }
            else
            {
                // Smoke and fuse dust spawn.
                if (Main.rand.Next(4) == 0)
                {
                    int dustIndex = Dust.NewDust(new Vector2(Projectile.position.X, Projectile.position.Y), Projectile.width, Projectile.height, 6, 0f, 0f, 100, default(Color), 1f);
                    Main.dust[dustIndex].scale = 0.1f + (float)Main.rand.Next(5) * 0.1f;
                    Main.dust[dustIndex].fadeIn = .5f + (float)Main.rand.Next(5) * 0.1f;
                    Main.dust[dustIndex].noGravity = true;
                    // Main.dust[dustIndex].position = projectile.Center + new Vector2(0f, (float)(-(float)projectile.height / 2)).RotatedBy((double)projectile.rotation, default(Vector2)) * 1.1f;
                    /*dustIndex = Dust.NewDust(new Vector2(projectile.position.X, projectile.position.Y), projectile.width, projectile.height, 75, 0f, 0f, 100, default(Color), 1f);
                    Main.dust[dustIndex].scale = 1f + (float)Main.rand.Next(5) * 0.1f;
                    Main.dust[dustIndex].noGravity = true;*/
                    // Main.dust[dustIndex].position = projectile.Center + new Vector2(0f, (float)(-(float)projectile.height / 2 - 6)).RotatedBy((double)projectile.rotation, default(Vector2)) * 1.1f;
                }
            }
            Projectile.ai[0] += 1f;
            if (Projectile.ai[0] > 5f)
            {
                Projectile.ai[0] = 10f;
                // Roll speed dampening.
                if (Projectile.velocity.Y == 0f && Projectile.velocity.X != 0f)
                {
                    Projectile.velocity.X = Projectile.velocity.X * 0.97f;
                    //if (projectile.type == 29 || projectile.type == 470 || projectile.type == 637)
                    {
                        Projectile.velocity.X = Projectile.velocity.X * 0.99f;
                    }
                    if ((double)Projectile.velocity.X > -0.01 && (double)Projectile.velocity.X < 0.01)
                    {
                        Projectile.velocity.X = 0f;
                        Projectile.netUpdate = true;
                    }
                }
                Projectile.velocity.Y = Projectile.velocity.Y + 0.2f;
            }
            // Rotation increased by velocity.X 
            Projectile.rotation += Projectile.velocity.X * 0.08f;
            return;
        }

        public override void Kill(int timeLeft)
        {
            // Play explosion sound
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item74.WithPitchVariance(.5f), Projectile.position);
            // Fire Dust spawn
            for (int i = 0; i < 200; i++)
            {
                int dustIndex = Dust.NewDust(new Vector2(Projectile.position.X + 36, Projectile.position.Y + 36), Projectile.width - 74, Projectile.height - 74, 6, Main.rand.Next(-6, 6), Main.rand.Next(-6, 6), 100, default(Color), 2f);
                Main.dust[dustIndex].noGravity = true;
                Main.dust[dustIndex].velocity *= 3.5f;
            }
            // Large Smoke Gore spawn
            for (int g = 0; g < 2; g++)
            {
                int goreIndex = Gore.NewGore(new Vector2(Projectile.position.X + (float)(Projectile.width / 2) - 24f, Projectile.position.Y + (float)(Projectile.height / 2) - 24f), default(Vector2), Main.rand.Next(61, 64), .8f);
                Main.gore[goreIndex].scale = 1f;
                Main.gore[goreIndex].velocity.X = Main.gore[goreIndex].velocity.X + 1f;
                Main.gore[goreIndex].velocity.Y = Main.gore[goreIndex].velocity.Y + 1f;
                goreIndex = Gore.NewGore(new Vector2(Projectile.position.X + (float)(Projectile.width / 2) - 24f, Projectile.position.Y + (float)(Projectile.height / 2) - 24f), default(Vector2), Main.rand.Next(61, 64), .8f);
                Main.gore[goreIndex].scale = 1f;
                Main.gore[goreIndex].velocity.X = Main.gore[goreIndex].velocity.X - 1f;
                Main.gore[goreIndex].velocity.Y = Main.gore[goreIndex].velocity.Y + 1f;

            }
            // reset size to normal width and height.
            Projectile.position.X = Projectile.position.X + (float)(Projectile.width / 2);
            Projectile.position.Y = Projectile.position.Y + (float)(Projectile.height / 2);
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.position.X = Projectile.position.X - (float)(Projectile.width / 2);
            Projectile.position.Y = Projectile.position.Y - (float)(Projectile.height / 2);
        }
    }
}

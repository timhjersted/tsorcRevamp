using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Microsoft.Xna.Framework.Graphics;


namespace tsorcRevamp.Projectiles
{
    class PolarisShot : ModProjectile
    {
        public override string Texture => "tsorcRevamp/Projectiles/PulsarShot";
        public override void SetStaticDefaults()
        {
            Main.projFrames[projectile.type] = 8;
        }
        public override void SetDefaults()
        {
            projectile.width = 10;
            projectile.height = 10;
            projectile.friendly = true;
            projectile.aiStyle = 0;
            projectile.ranged = true;
            projectile.tileCollide = true;
            projectile.timeLeft = 90;
            projectile.penetrate = -1;
        }

        public override void OnHitNPC(NPC target, int damage, float knockback, bool crit)
        {
            if (projectile.owner == Main.myPlayer && projectile.timeLeft <= 70)
            {
                if (Main.rand.Next(2) == 0)
                {
                    target.AddBuff(mod.BuffType("PolarisElectrocutedBuff"), 360);
                }
                projectile.timeLeft = 2;
            }

            else
            {
                projectile.tileCollide = true;
                projectile.position.X = projectile.position.X + (float)(projectile.width / 2);
                projectile.position.Y = projectile.position.Y + (float)(projectile.height / 2);
                projectile.width = 10;
                projectile.height = 10;
                projectile.position.X = projectile.position.X - (float)(projectile.width / 2);
                projectile.position.Y = projectile.position.Y - (float)(projectile.height / 2);
                projectile.damage = (int)(originalDamage * 1f);
                projectile.knockBack = 3f;
                projectile.ranged = true;

                if (Main.rand.Next(2) == 0)
                {
                    target.AddBuff(mod.BuffType("PolarisElectrocutedBuff"), 240);
                }

                projectile.timeLeft = 0;
            }
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            Texture2D texture = Main.projectileTexture[projectile.type];

            spriteBatch.Draw(texture, projectile.Center - Main.screenPosition, new Rectangle(0, projectile.frame * 32, 32, 32), Color.White, projectile.rotation, new Vector2(16, 16), projectile.scale, SpriteEffects.None, 0);

            return false;
        }
        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            if (projectile.owner == Main.myPlayer && projectile.timeLeft >= 72)
            {
                projectile.timeLeft = 0;

                if (Main.netMode != NetmodeID.Server)
                {
                    Main.PlaySound(mod.GetLegacySoundSlot(SoundType.Item, "Sounds/Item/PulsarBump").WithVolume(.6f).WithPitchVariance(.3f), projectile.Center);
                }

            }
            else
            {
                projectile.tileCollide = false;
                projectile.alpha = 255;
                projectile.position.X = projectile.position.X + (float)(projectile.width / 2);
                projectile.position.Y = projectile.position.Y + (float)(projectile.height / 2);
                projectile.width = 134;
                projectile.height = 134;
                projectile.position.X = projectile.position.X - (float)(projectile.width / 2);
                projectile.position.Y = projectile.position.Y - (float)(projectile.height / 2);
                projectile.damage = (int)(originalDamage * 1.36f);
                projectile.knockBack = 10f;
                projectile.ranged = true;
                projectile.timeLeft = 0;
                if (Main.netMode != NetmodeID.Server)
                {
                    Main.PlaySound(mod.GetLegacySoundSlot(SoundType.Item, "Sounds/Item/PulsarBoom").WithVolume(.6f).WithPitchVariance(.3f), projectile.Center);
                }
                for (int i = 0; i < 100; i++)
                {
                    int dust = Dust.NewDust(projectile.position, projectile.width, projectile.height, 226, projectile.velocity.X * 0f, projectile.velocity.Y * 0f, 30, default(Color), .4f);
                    Main.dust[dust].noGravity = true;
                }
                for (int i = 0; i < 100; i++)
                {
                    int dust = Dust.NewDust(projectile.position, projectile.width, projectile.height, 226, projectile.velocity.X * 0f, projectile.velocity.Y * 0f, 70, default(Color), 1f);
                    Main.dust[dust].noGravity = true;
                    Main.dust[dust].velocity *= Main.rand.NextFloat(3f, 4.5f);
                    Main.dust[dust].scale = 0.2f + (float)Main.rand.Next(5) * 0.1f;
                }

            }
            return false;
        }

        public int polarisdusttimer;
        public int originalDamage = 0;
        public bool spawned = false;

        public override void AI()
        {
            Lighting.AddLight(projectile.position, 0.0452f, 0.21f, 0.073f);

            float rotationsPerSecond = 1.6f;
            bool rotateClockwise = true;
            projectile.rotation += (rotateClockwise ? 1 : -1) * MathHelper.ToRadians(rotationsPerSecond * 6f);

            if (!spawned)
            {
                spawned = true;
                originalDamage = projectile.damage;
            }

            //DUST SPAWNING
            if (Main.rand.Next(3) == 0)
            {
                int dust = Dust.NewDust(projectile.position, projectile.width, projectile.height, 226, projectile.velocity.X * 0.5f, projectile.velocity.Y * 0.5f, 100, default(Color), .4f);
                Main.dust[dust].noGravity = true;
            }
            polarisdusttimer++;
            if (polarisdusttimer == 15)
            {
                for (int a = 0; a < 15; a++)
                {
                    int dust = Dust.NewDust(projectile.position, projectile.width - 9, projectile.height - 9, 226, projectile.velocity.X * 0f, projectile.velocity.Y * 0f, 100, default(Color), .4f);
                    Main.dust[dust].noGravity = true;
                }
            }
            if (polarisdusttimer == 30)
            {
                for (int a = 0; a < 15; a++)
                {
                    int dust = Dust.NewDust(projectile.position, projectile.width - 9, projectile.height - 9, 226, projectile.velocity.X * 0f, projectile.velocity.Y * 0f, 100, default(Color), .4f);
                    Main.dust[dust].noGravity = true;
                }
            }
            if (polarisdusttimer == 45)
            {
                for (int a = 0; a < 15; a++)
                {
                    int dust = Dust.NewDust(projectile.position, projectile.width - 9, projectile.height - 9, 226, projectile.velocity.X * 0f, projectile.velocity.Y * 0f, 100, default(Color), .4f);
                    Main.dust[dust].noGravity = true;
                }
            }
            if (polarisdusttimer == 60)
            {
                for (int a = 0; a < 15; a++)
                {
                    int dust = Dust.NewDust(projectile.position, projectile.width - 9, projectile.height - 9, 226, projectile.velocity.X * 0f, projectile.velocity.Y * 0f, 100, default(Color), .4f);
                    Main.dust[dust].noGravity = true;
                }
            }
            if (polarisdusttimer == 75)
            {
                for (int a = 0; a < 15; a++)
                {
                    int dust = Dust.NewDust(projectile.position, projectile.width - 9, projectile.height - 9, 226, projectile.velocity.X * 0f, projectile.velocity.Y * 0f, 100, default(Color), .4f);
                    Main.dust[dust].noGravity = true;
                }
            }

            //ANIMATION
            if (polarisdusttimer > 90)
            {
                polarisdusttimer = 0;
            }

            if (++projectile.frameCounter >= 10)
            {
                projectile.frameCounter = 0;
                if (projectile.timeLeft >= 72)
                {
                    if (++projectile.frame == 4)
                    {
                        projectile.frame = 0;
                    }
                }
                if (projectile.timeLeft <= 71)
                {
                    projectile.frameCounter = 5;
                    if (++projectile.frame >= 8)
                    {
                        projectile.frame = 5;
                    }
                }
            }

            Vector2 oldSize = projectile.Size;

            if (projectile.owner == Main.myPlayer && projectile.timeLeft <= 70)
            {
                projectile.tileCollide = true;
                projectile.width = 22;
                projectile.height = 22;
                projectile.damage = (int)(originalDamage * 1.36f);
                projectile.knockBack = 10f;
                projectile.ranged = true;
            }

            if (projectile.owner == Main.myPlayer && projectile.timeLeft == 71)
            {
                if (Main.netMode != NetmodeID.Server)
                {
                    Main.PlaySound(mod.GetLegacySoundSlot(SoundType.Item, "Sounds/Item/PulsarGrow").WithVolume(.6f).WithPitchVariance(.3f), projectile.Center);
                }
            }

            projectile.position = projectile.position - (projectile.Size - oldSize) / 2f;

            if (projectile.owner == Main.myPlayer && projectile.timeLeft == 1)
            {
                if (Main.netMode != NetmodeID.Server)
                {
                    Main.PlaySound(mod.GetLegacySoundSlot(SoundType.Item, "Sounds/Item/PulsarBoom").WithVolume(.6f).WithPitchVariance(.3f), projectile.Center);
                }
                for (int i = 0; i < 200; i++)
                {
                    int dust = Dust.NewDust(projectile.position, projectile.width, projectile.height, 226, projectile.velocity.X * 0f, projectile.velocity.Y * 0f, 30, default(Color), .4f);
                    Main.dust[dust].noGravity = true;
                }
                for (int i = 0; i < 150; i++)
                {
                    int dust = Dust.NewDust(projectile.position, projectile.width, projectile.height, 226, projectile.velocity.X * 0f, projectile.velocity.Y * 0f, 70, default(Color), .6f);
                    Main.dust[dust].noGravity = true;
                }
                for (int i = 0; i < 250; i++)
                {
                    int dust = Dust.NewDust(projectile.position, projectile.width, projectile.height, 226, projectile.velocity.X * 0f, projectile.velocity.Y * 0f, 70, default(Color), 1f);
                    Main.dust[dust].noGravity = true;
                    Main.dust[dust].velocity *= Main.rand.NextFloat(3f, 4.5f);
                    Main.dust[dust].scale = 0.2f + (float)Main.rand.Next(5) * 0.1f;
                }
            }

            if (projectile.owner == Main.myPlayer && projectile.timeLeft <= 2)
            {
                projectile.tileCollide = false;
                projectile.alpha = 255;
                projectile.position.X = projectile.position.X + (float)(projectile.width / 2);
                projectile.position.Y = projectile.position.Y + (float)(projectile.height / 2);
                projectile.width = 134;
                projectile.height = 134;
                projectile.position.X = projectile.position.X - (float)(projectile.width / 2);
                projectile.position.Y = projectile.position.Y - (float)(projectile.height / 2);
                projectile.damage = (int)(originalDamage * 1.36f);
                projectile.knockBack = 10f;
                projectile.ranged = true;
            }
            if (projectile.wet)
            {
                projectile.tileCollide = false;
                projectile.alpha = 255;
                projectile.position.X = projectile.position.X + (float)(projectile.width / 2);
                projectile.position.Y = projectile.position.Y + (float)(projectile.height / 2);
                projectile.width = 134;
                projectile.height = 134;
                projectile.position.X = projectile.position.X - (float)(projectile.width / 2);
                projectile.position.Y = projectile.position.Y - (float)(projectile.height / 2);
                projectile.damage = (int)(originalDamage * 1.5f);
                projectile.knockBack = 10f;
                projectile.ranged = true;
                if (Main.netMode != NetmodeID.Server)
                {
                    Main.PlaySound(mod.GetLegacySoundSlot(SoundType.Item, "Sounds/Item/PulsarBoom").WithVolume(.6f).WithPitchVariance(.3f), projectile.Center);
                }
                for (int i = 0; i < 70; i++)
                {
                    int dust = Dust.NewDust(projectile.position, projectile.width, projectile.height, 226, projectile.velocity.X * 0f, projectile.velocity.Y * 0f, 30, default(Color), .4f);
                    Main.dust[dust].noGravity = true;
                }
                for (int i = 0; i < 50; i++)
                {
                    int dust = Dust.NewDust(projectile.position, projectile.width, projectile.height, 226, projectile.velocity.X * 0f, projectile.velocity.Y * 0f, 70, default(Color), .6f);
                    Main.dust[dust].noGravity = true;
                }
                for (int i = 0; i < 70; i++)
                {
                    int dust = Dust.NewDust(projectile.position, projectile.width, projectile.height, 226, projectile.velocity.X * 0f, projectile.velocity.Y * 0f, 70, default(Color), 1f);
                    Main.dust[dust].noGravity = true;
                    Main.dust[dust].velocity *= Main.rand.NextFloat(2.5f, 4f);
                    Main.dust[dust].scale = 0.2f + (float)Main.rand.Next(5) * 0.1f;


                    projectile.timeLeft = 0;
                }
            }
        }

        public override void Kill(int timeLeft)
        {
            for (int i = 0; i < 100; i++)
            {
                int dust = Dust.NewDust(projectile.position, projectile.width, projectile.height, 226, projectile.velocity.X * 0f, projectile.velocity.Y * 0f, 70, default(Color), .6f);
                Main.dust[dust].noGravity = true;
            }
        }
    }
}
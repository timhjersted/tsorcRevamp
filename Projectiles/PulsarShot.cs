using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;


namespace tsorcRevamp.Projectiles
{
    class PulsarShot : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 8;
        }
        public override void SetDefaults()
        {
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.friendly = true;
            Projectile.aiStyle = 0;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.tileCollide = true;
            Projectile.timeLeft = 120;
            Projectile.penetrate = -1;

        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Projectile.owner == Main.myPlayer && Projectile.timeLeft <= 90)
            {
                if (Main.rand.NextBool(3))
                {
                    target.AddBuff(ModContent.BuffType<Buffs.ElectrocutedBuff>(), 180);
                }
                Projectile.timeLeft = 2;
            }

            else
            {
                Projectile.tileCollide = true;
                // change the hitbox size, centered about the original projectile center. This makes the projectile damage enemies during the explosion.
                Projectile.position.X = Projectile.position.X + (float)(Projectile.width / 2);
                Projectile.position.Y = Projectile.position.Y + (float)(Projectile.height / 2);
                Projectile.width = 10;
                Projectile.height = 10;
                Projectile.position.X = Projectile.position.X - (float)(Projectile.width / 2);
                Projectile.position.Y = Projectile.position.Y - (float)(Projectile.height / 2);
                Projectile.damage = (int)(originalDamage * 1f);
                Projectile.knockBack = 2f;
                Projectile.DamageType = DamageClass.Ranged;

                if (Main.rand.NextBool(2))
                {
                    target.AddBuff(ModContent.BuffType<Buffs.ElectrocutedBuff>(), 120);
                }

                /*if(Main.netMode != NetmodeID.MultiplayerClient)
                {
                    Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero, ModContent.ProjectileType<Projectiles.VFX.ShockwaveEffect>(), 0, 0, Main.myPlayer, 5, 10);
                }*/

                Projectile.timeLeft = 0;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = (Texture2D)Terraria.GameContent.TextureAssets.Projectile[Projectile.type];

            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, new Rectangle(0, Projectile.frame * 32, 32, 32), Color.White, Projectile.rotation, new Vector2(16, 16), Projectile.scale, SpriteEffects.None, 0);

            return false;
        }
        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            if (Projectile.owner == Main.myPlayer && Projectile.timeLeft >= 92)
            {
                Projectile.timeLeft = 0;

                if (Main.netMode != NetmodeID.Server)
                {
                    Terraria.Audio.SoundEngine.PlaySound(new Terraria.Audio.SoundStyle("tsorcRevamp/Sounds/Item/PulsarBump") with { Volume = 0.6f, PitchVariance = .3f }, Projectile.Center);
                }
            }
            else //(projectile.owner == Main.myPlayer && projectile.timeLeft <= 51)
            {
                Projectile.tileCollide = false;
                // Set to transparent. This projectile technically lives as  transparent for about 2 frames
                Projectile.alpha = 255;
                // change the hitbox size, centered about the original projectile center. This makes the projectile damage enemies during the explosion.
                Projectile.position.X = Projectile.position.X + (float)(Projectile.width / 2);
                Projectile.position.Y = Projectile.position.Y + (float)(Projectile.height / 2);
                Projectile.width = 60;
                Projectile.height = 60;
                Projectile.position.X = Projectile.position.X - (float)(Projectile.width / 2);
                Projectile.position.Y = Projectile.position.Y - (float)(Projectile.height / 2);
                Projectile.damage = (int)(originalDamage * 1.5f);
                Projectile.knockBack = 6f;
                Projectile.DamageType = DamageClass.Ranged;
                Projectile.timeLeft = 0;

                if (Main.netMode != NetmodeID.Server)
                {
                    Terraria.Audio.SoundEngine.PlaySound(new Terraria.Audio.SoundStyle("tsorcRevamp/Sounds/Item/PulsarBoom") with { Volume = 0.6f, PitchVariance = .3f }, Projectile.Center);
                    Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero, ModContent.ProjectileType<Projectiles.VFX.ShockwaveEffect>(), 0, 0, Main.myPlayer, 15, 15);
                }
                for (int i = 0; i < 110; i++)
                {
                    int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, 226, Projectile.velocity.X * 0f, Projectile.velocity.Y * 0f, 30, default(Color), .4f);
                    Main.dust[dust].noGravity = true;
                }
                for (int i = 0; i < 60; i++)
                {
                    int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, 226, Projectile.velocity.X * 0f, Projectile.velocity.Y * 0f, 70, default(Color), .6f);
                    Main.dust[dust].noGravity = true;
                }
                for (int i = 0; i < 110; i++)
                {
                    int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, 226, Projectile.velocity.X * 0f, Projectile.velocity.Y * 0f, 70, default(Color), 1f);
                    Main.dust[dust].noGravity = true;
                    Main.dust[dust].velocity *= Main.rand.NextFloat(2f, 3.5f);
                    Main.dust[dust].scale = 0.2f + (float)Main.rand.Next(5) * 0.1f;
                }

            }
            return false;
        }

        public int pulsardusttimer;
        public int originalDamage = 0;
        public bool spawned = false;

        public override void AI()
        {
            Lighting.AddLight(Projectile.position, 0.0452f, 0.21f, 0.073f);
            //projectile.rotation += 0.12f;

            //Change these two variables to affect the rotation of your projectile
            float rotationsPerSecond = 1.2f;
            bool rotateClockwise = true;
            //The rotation is set here
            Projectile.rotation += (rotateClockwise ? 1 : -1) * MathHelper.ToRadians(rotationsPerSecond * 6f);


            if (!spawned)
            {
                spawned = true;
                originalDamage = Projectile.damage;
            }

            //DUST SPAWNING

            if (Main.rand.NextBool(4))
            {
                int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, 226, Projectile.velocity.X * 0.5f, Projectile.velocity.Y * 0.5f, 100, default(Color), .4f);
                Main.dust[dust].noGravity = true;
            }
            pulsardusttimer++;
            if (pulsardusttimer >= 15 && pulsardusttimer <= 16)
            {
                for (int a = 0; a < 4; a++)
                {
                    int dust = Dust.NewDust(Projectile.position, Projectile.width - 9, Projectile.height - 9, 226, Projectile.velocity.X * 0f, Projectile.velocity.Y * 0f, 100, default(Color), .4f);
                    Main.dust[dust].noGravity = true;
                }
            }
            if (pulsardusttimer >= 34 && pulsardusttimer <= 35)
            {
                for (int a = 0; a < 4; a++)
                {
                    int dust = Dust.NewDust(Projectile.position, Projectile.width - 9, Projectile.height - 9, 226, Projectile.velocity.X * 0f, Projectile.velocity.Y * 0f, 100, default(Color), .4f);
                    Main.dust[dust].noGravity = true;
                }
            }
            if (pulsardusttimer >= 53 && pulsardusttimer <= 54)
            {
                for (int a = 0; a < 4; a++)
                {
                    int dust = Dust.NewDust(Projectile.position, Projectile.width - 9, Projectile.height - 9, 226, Projectile.velocity.X * 0f, Projectile.velocity.Y * 0f, 100, default(Color), .4f);
                    Main.dust[dust].noGravity = true;
                }
            }
            if (pulsardusttimer >= 72 && pulsardusttimer <= 73)
            {
                for (int a = 0; a < 4; a++)
                {
                    int dust = Dust.NewDust(Projectile.position, Projectile.width - 9, Projectile.height - 9, 226, Projectile.velocity.X * 0f, Projectile.velocity.Y * 0f, 100, default(Color), .4f);
                    Main.dust[dust].noGravity = true;
                }
            }

            //ANIMATION
            if (pulsardusttimer > 90)
            {
                pulsardusttimer = 0;
            }

            if (Projectile.timeLeft >= 92)
            {
                if (++Projectile.frameCounter >= 10) //ticks spent on each frame
                {
                    Projectile.frameCounter = 0;
                    if (++Projectile.frame == 4)
                    {
                        Projectile.frame = 0;
                    }
                }
            }

            if (Projectile.timeLeft <= 91)
            {
                if (Projectile.frame < 5)
                {
                    Projectile.frame = 5;
                }

                if (++Projectile.frameCounter >= 10)
                {
                    Projectile.frameCounter = 0;
                    if (++Projectile.frame >= 8)
                    {
                        Projectile.frame = 5;
                    }
                }
            }


            Vector2 oldSize = Projectile.Size;

            if (Projectile.owner == Main.myPlayer && Projectile.timeLeft <= 90)
            {
                Projectile.tileCollide = true;
                Projectile.width = 22;
                Projectile.height = 22;
                Projectile.damage = (int)(originalDamage * 1.5f);
                Projectile.knockBack = 6f;
                Projectile.DamageType = DamageClass.Ranged;
            }

            if (Projectile.owner == Main.myPlayer && Projectile.timeLeft == 91)
            {
                if (Main.netMode != NetmodeID.Server)
                {
                    Terraria.Audio.SoundEngine.PlaySound(new Terraria.Audio.SoundStyle("tsorcRevamp/Sounds/Item/PulsarGrow") with { Volume = 0.6f, PitchVariance = .3f }, Projectile.Center);
                }
            }

            Projectile.position = Projectile.position - (Projectile.Size - oldSize) / 2f;

            if (Projectile.owner == Main.myPlayer && Projectile.timeLeft == 1)
            {
                if (Main.netMode != NetmodeID.Server)
                {
                    Terraria.Audio.SoundEngine.PlaySound(new Terraria.Audio.SoundStyle("tsorcRevamp/Sounds/Item/PulsarBoom") with { Volume = 0.6f, PitchVariance = .3f }, Projectile.Center);
                    Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero, ModContent.ProjectileType<Projectiles.VFX.ShockwaveEffect>(), 0, 0, Main.myPlayer, 15, 15);
                }
                for (int i = 0; i < 110; i++)
                {
                    int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, 226, Projectile.velocity.X * 0f, Projectile.velocity.Y * 0f, 30, default(Color), .4f);
                    Main.dust[dust].noGravity = true;
                }
                for (int i = 0; i < 60; i++)
                {
                    int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, 226, Projectile.velocity.X * 0f, Projectile.velocity.Y * 0f, 70, default(Color), .6f);
                    Main.dust[dust].noGravity = true;
                }
                for (int i = 0; i < 110; i++)
                {
                    int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, 226, Projectile.velocity.X * 0f, Projectile.velocity.Y * 0f, 70, default(Color), 1f);
                    Main.dust[dust].noGravity = true;
                    Main.dust[dust].velocity *= Main.rand.NextFloat(2f, 3.5f);
                    Main.dust[dust].scale = 0.2f + (float)Main.rand.Next(5) * 0.1f;
                }
            }

            if (Projectile.owner == Main.myPlayer && Projectile.timeLeft <= 2)
            {
                Projectile.tileCollide = false;
                // Set to transparent. This projectile technically lives as  transparent for about 2 frames
                Projectile.alpha = 255;
                // change the hitbox size, centered about the original projectile center. This makes the projectile damage enemies during the explosion.
                Projectile.position.X = Projectile.position.X + (float)(Projectile.width / 2);
                Projectile.position.Y = Projectile.position.Y + (float)(Projectile.height / 2);
                Projectile.width = 60;
                Projectile.height = 60;
                Projectile.position.X = Projectile.position.X - (float)(Projectile.width / 2);
                Projectile.position.Y = Projectile.position.Y - (float)(Projectile.height / 2);
                Projectile.damage = (int)(originalDamage * 1.5f);
                Projectile.knockBack = 6f;
                Projectile.DamageType = DamageClass.Ranged;
            }
            if (Projectile.wet)
            {
                Projectile.tileCollide = false;
                // Set to transparent. This projectile technically lives as  transparent for about 2 frames
                Projectile.alpha = 255;
                // change the hitbox size, centered about the original projectile center. This makes the projectile damage enemies during the explosion.
                Projectile.position.X = Projectile.position.X + (float)(Projectile.width / 2);
                Projectile.position.Y = Projectile.position.Y + (float)(Projectile.height / 2);
                Projectile.width = 80;
                Projectile.height = 80;
                Projectile.position.X = Projectile.position.X - (float)(Projectile.width / 2);
                Projectile.position.Y = Projectile.position.Y - (float)(Projectile.height / 2);
                Projectile.damage = (int)(originalDamage * 1.5f);
                Projectile.knockBack = 6f;
                Projectile.DamageType = DamageClass.Ranged;

                if (Main.netMode != NetmodeID.Server)
                {
                    Terraria.Audio.SoundEngine.PlaySound(new Terraria.Audio.SoundStyle("tsorcRevamp/Sounds/Item/PulsarBoom") with { Volume = 0.6f, PitchVariance = .3f }, Projectile.Center);
                    Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero, ModContent.ProjectileType<Projectiles.VFX.ShockwaveEffect>(), 0, 0, Main.myPlayer, 15, 15);
                }

                for (int i = 0; i < 110; i++)
                {
                    int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, 226, Projectile.velocity.X * 0f, Projectile.velocity.Y * 0f, 30, default(Color), .4f);
                    Main.dust[dust].noGravity = true;
                }
                for (int i = 0; i < 60; i++)
                {
                    int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, 226, Projectile.velocity.X * 0f, Projectile.velocity.Y * 0f, 70, default(Color), .6f);
                    Main.dust[dust].noGravity = true;
                }
                for (int i = 0; i < 110; i++)
                {
                    int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, 226, Projectile.velocity.X * 0f, Projectile.velocity.Y * 0f, 70, default(Color), 1f);
                    Main.dust[dust].noGravity = true;
                    Main.dust[dust].velocity *= Main.rand.NextFloat(2f, 3.5f);
                    Main.dust[dust].scale = 0.2f + (float)Main.rand.Next(5) * 0.1f;
                    Projectile.timeLeft = 0;
                }
            }
        }

        public override void OnKill(int timeLeft)
        {
            for (int i = 0; i < 50; i++)
            {
                int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, 226, Projectile.velocity.X * 0f, Projectile.velocity.Y * 0f, 70, default(Color), .6f);
                Main.dust[dust].noGravity = true;
            }
        }
    }
}
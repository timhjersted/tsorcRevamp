using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;


namespace tsorcRevamp.Projectiles
{
    class GWPulsarShot : ModProjectile
    {
        public override string Texture => "tsorcRevamp/Projectiles/PulsarShot";
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

        public override void OnHitNPC(NPC target, int damage, float knockback, bool crit)
        {
            if (Projectile.owner == Main.myPlayer && Projectile.timeLeft <= 95)
            {

                target.AddBuff(Mod.Find<ModBuff>("ElectrocutedBuff").Type, 420);
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
                Projectile.knockBack = 2.5f;
                Projectile.DamageType = DamageClass.Ranged;

                target.AddBuff(Mod.Find<ModBuff>("ElectrocutedBuff").Type, 300);

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
            if (Projectile.owner == Main.myPlayer && Projectile.timeLeft >= 97)
            {
                Projectile.timeLeft = 0;

                if (Main.netMode != NetmodeID.Server)
                {
                    Terraria.Audio.SoundEngine.PlaySound(Mod.GetLegacySoundSlot(SoundType.Item, "Sounds/Item/PulsarBump").WithVolume(.6f).WithPitchVariance(.3f), Projectile.Center);
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
                Projectile.width = 85;
                Projectile.height = 85;
                Projectile.position.X = Projectile.position.X - (float)(Projectile.width / 2);
                Projectile.position.Y = Projectile.position.Y - (float)(Projectile.height / 2);
                Projectile.damage = (int)(originalDamage * 1.5f);
                Projectile.knockBack = 6.5f;
                Projectile.DamageType = DamageClass.Ranged;
                Projectile.timeLeft = 0;
                if (Main.netMode != NetmodeID.Server)
                {
                    Terraria.Audio.SoundEngine.PlaySound(Mod.GetLegacySoundSlot(SoundType.Item, "Sounds/Item/PulsarBoom").WithVolume(.6f).WithPitchVariance(.3f), Projectile.Center);
                }
                for (int i = 0; i < 120; i++)
                {
                    int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, 226, Projectile.velocity.X * 0f, Projectile.velocity.Y * 0f, 30, default(Color), .4f);
                    Main.dust[dust].noGravity = true;
                }
                for (int i = 0; i < 70; i++)
                {
                    int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, 226, Projectile.velocity.X * 0f, Projectile.velocity.Y * 0f, 70, default(Color), .6f);
                    Main.dust[dust].noGravity = true;
                }
                for (int i = 0; i < 120; i++)
                {
                    int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, 226, Projectile.velocity.X * 0f, Projectile.velocity.Y * 0f, 70, default(Color), 1f);
                    Main.dust[dust].noGravity = true;
                    Main.dust[dust].velocity *= Main.rand.NextFloat(2f, 3.5f);
                    Main.dust[dust].scale = 0.2f + (float)Main.rand.Next(5) * 0.1f;
                }

            }
            return false;
        }

        public int gwpulsardusttimer;
        public int originalDamage = 0;
        public bool spawned = false;

        public override void AI()
        {
            Lighting.AddLight(Projectile.position, 0.0452f, 0.21f, 0.073f);

            float rotationsPerSecond = 1.4f;
            bool rotateClockwise = true;
            Projectile.rotation += (rotateClockwise ? 1 : -1) * MathHelper.ToRadians(rotationsPerSecond * 6f);

            if (!spawned)
            {
                spawned = true;
                originalDamage = Projectile.damage;
            }

            //DUST SPAWNING
            if (Main.rand.Next(4) == 0)
            {
                int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, 226, Projectile.velocity.X * 0.5f, Projectile.velocity.Y * 0.5f, 100, default(Color), .4f);
                Main.dust[dust].noGravity = true;
            }
            gwpulsardusttimer++;
            if (gwpulsardusttimer == 15)
            {
                for (int a = 0; a < 7; a++)
                {
                    int dust = Dust.NewDust(Projectile.position, Projectile.width - 9, Projectile.height - 9, 226, Projectile.velocity.X * 0f, Projectile.velocity.Y * 0f, 100, default(Color), .4f);
                    Main.dust[dust].noGravity = true;
                }
            }
            if (gwpulsardusttimer == 34)
            {
                for (int a = 0; a < 7; a++)
                {
                    int dust = Dust.NewDust(Projectile.position, Projectile.width - 9, Projectile.height - 9, 226, Projectile.velocity.X * 0f, Projectile.velocity.Y * 0f, 100, default(Color), .4f);
                    Main.dust[dust].noGravity = true;
                }
            }
            if (gwpulsardusttimer == 53)
            {
                for (int a = 0; a < 7; a++)
                {
                    int dust = Dust.NewDust(Projectile.position, Projectile.width - 9, Projectile.height - 9, 226, Projectile.velocity.X * 0f, Projectile.velocity.Y * 0f, 100, default(Color), .4f);
                    Main.dust[dust].noGravity = true;
                }
            }
            if (gwpulsardusttimer == 72)
            {
                for (int a = 0; a < 7; a++)
                {
                    int dust = Dust.NewDust(Projectile.position, Projectile.width - 9, Projectile.height - 9, 226, Projectile.velocity.X * 0f, Projectile.velocity.Y * 0f, 100, default(Color), .4f);
                    Main.dust[dust].noGravity = true;
                }
            }

            //ANIMATION
            if (gwpulsardusttimer > 90)
            {
                gwpulsardusttimer = 0;
            }

            if (Projectile.timeLeft >= 97)
            {
                if (++Projectile.frameCounter >= 10)
                {
                    Projectile.frameCounter = 0;
                    if (++Projectile.frame == 4)
                    {
                        Projectile.frame = 0;
                    }
                }
            }

            if (Projectile.timeLeft <= 96)
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

            if (Projectile.owner == Main.myPlayer && Projectile.timeLeft <= 95)
            {
                Projectile.tileCollide = true;
                Projectile.width = 22;
                Projectile.height = 22;
                Projectile.damage = (int)(originalDamage * 1.5f);
                Projectile.knockBack = 6.5f;
                Projectile.DamageType = DamageClass.Ranged;
            }

            if (Projectile.owner == Main.myPlayer && Projectile.timeLeft == 96)
            {
                if (Main.netMode != NetmodeID.Server)
                {
                    Terraria.Audio.SoundEngine.PlaySound(Mod.GetLegacySoundSlot(SoundType.Item, "Sounds/Item/PulsarGrow").WithVolume(.6f).WithPitchVariance(.3f), Projectile.Center);
                }
            }

            Projectile.position = Projectile.position - (Projectile.Size - oldSize) / 2f;

            if (Projectile.owner == Main.myPlayer && Projectile.timeLeft == 1)
            {
                if (Main.netMode != NetmodeID.Server)
                {
                    Terraria.Audio.SoundEngine.PlaySound(Mod.GetLegacySoundSlot(SoundType.Item, "Sounds/Item/PulsarBoom").WithVolume(.6f).WithPitchVariance(.3f), Projectile.Center);
                }
                for (int i = 0; i < 120; i++)
                {
                    int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, 226, Projectile.velocity.X * 0f, Projectile.velocity.Y * 0f, 30, default(Color), .4f);
                    Main.dust[dust].noGravity = true;
                }
                for (int i = 0; i < 70; i++)
                {
                    int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, 226, Projectile.velocity.X * 0f, Projectile.velocity.Y * 0f, 70, default(Color), .6f);
                    Main.dust[dust].noGravity = true;
                }
                for (int i = 0; i < 120; i++)
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
                Projectile.alpha = 255;
                Projectile.position.X = Projectile.position.X + (float)(Projectile.width / 2);
                Projectile.position.Y = Projectile.position.Y + (float)(Projectile.height / 2);
                Projectile.width = 85;
                Projectile.height = 85;
                Projectile.position.X = Projectile.position.X - (float)(Projectile.width / 2);
                Projectile.position.Y = Projectile.position.Y - (float)(Projectile.height / 2);
                Projectile.damage = (int)(originalDamage * 1.5f);
                Projectile.knockBack = 6.5f;
                Projectile.DamageType = DamageClass.Ranged;
            }
            if (Projectile.wet)
            {
                Projectile.tileCollide = false;
                Projectile.alpha = 255;
                Projectile.position.X = Projectile.position.X + (float)(Projectile.width / 2);
                Projectile.position.Y = Projectile.position.Y + (float)(Projectile.height / 2);
                Projectile.width = 85;
                Projectile.height = 85;
                Projectile.position.X = Projectile.position.X - (float)(Projectile.width / 2);
                Projectile.position.Y = Projectile.position.Y - (float)(Projectile.height / 2);
                Projectile.damage = (int)(originalDamage * 1.5f);
                Projectile.knockBack = 6.5f;
                Projectile.DamageType = DamageClass.Ranged;
                if (Main.netMode != NetmodeID.Server)
                {
                    Terraria.Audio.SoundEngine.PlaySound(Mod.GetLegacySoundSlot(SoundType.Item, "Sounds/Item/PulsarBoom").WithVolume(.6f).WithPitchVariance(.3f), Projectile.Center);
                }
                for (int i = 0; i < 120; i++)
                {
                    int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, 226, Projectile.velocity.X * 0f, Projectile.velocity.Y * 0f, 30, default(Color), .4f);
                    Main.dust[dust].noGravity = true;
                }
                for (int i = 0; i < 70; i++)
                {
                    int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, 226, Projectile.velocity.X * 0f, Projectile.velocity.Y * 0f, 70, default(Color), .6f);
                    Main.dust[dust].noGravity = true;
                }
                for (int i = 0; i < 120; i++)
                {
                    int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, 226, Projectile.velocity.X * 0f, Projectile.velocity.Y * 0f, 70, default(Color), 1f);
                    Main.dust[dust].noGravity = true;
                    Main.dust[dust].velocity *= Main.rand.NextFloat(2f, 3.5f);
                    Main.dust[dust].scale = 0.2f + (float)Main.rand.Next(5) * 0.1f;
                    Projectile.timeLeft = 0;
                }
            }
        }

        public override void Kill(int timeLeft)
        {
            for (int i = 0; i < 50; i++)
            {
                int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, 226, Projectile.velocity.X * 0f, Projectile.velocity.Y * 0f, 70, default(Color), .6f);
                Main.dust[dust].noGravity = true;
            }
        }
    }
}
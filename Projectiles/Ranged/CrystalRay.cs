using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Ranged
{
    class CrystalRay : GenericLaser
    {

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Crystal Ray");
        }

        public override string Texture => base.Texture;

        public override void SetDefaults()
        {
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.friendly = true;
            Projectile.hostile = false;

            FollowHost = false;
            TelegraphTime = 0;
            LaserColor = Color.White;
            LaserDust = 0;
            LineDust = false;
            LaserTexture = TransparentTextureHandler.TransparentTextureType.CrystalRay;
            LaserTextureHead = new Rectangle(0, 0, 30, 24);
            LaserTextureBody = new Rectangle(0, 26, 30, 30);
            LaserTextureTail = new Rectangle(0, 58, 30, 24);
            LaserSize = 0.7f;
            DustAmount = 20;
            MaxCharge = 0;
            FiringDuration = 60;
            PierceNPCs = false;
            Projectile.penetrate = 999;
            LaserSound = null;
            //Each projectile can hit an NPC once and only once
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 999;
        }

        bool hasHit = false;
        public override void AI()
        {
            if (LaserOrigin == Vector2.Zero)
            {
                LaserOrigin = Projectile.Center;
            }
            Projectile.velocity.Normalize();


            //Set color based on bullet type
            switch (Projectile.ai[0])
            {
                case ProjectileID.Bullet:
                    {
                        LaserColor = Color.Gray;
                        break;
                    }
                case ProjectileID.MeteorShot:
                    {
                        LaserColor = Color.Brown;
                        break;
                    }
                    //This is also meteor shot, but the reflected one
                case -999:
                    {
                        LaserColor = Color.Brown;
                        break;
                    }
                case ProjectileID.CrystalBullet:
                    {
                        LaserColor = Color.Cyan;
                        break;
                    }
                case ProjectileID.CursedBullet:
                    {
                        LaserColor = Color.GreenYellow;
                        break;
                    }
                case ProjectileID.ChlorophyteBullet:
                    {
                        LaserColor = Color.YellowGreen;
                        break;
                    }
                case ProjectileID.BulletHighVelocity:
                    {
                        LaserColor = Color.Yellow;
                        break;
                    }
                case ProjectileID.IchorBullet:
                    {
                        LaserColor = Color.LightYellow;
                        break;
                    }
                case ProjectileID.VenomBullet:
                    {
                        LaserColor = Color.Purple;
                        break;
                    }
                case ProjectileID.PartyBullet:
                    {
                        LaserColor = Main.DiscoColor;
                        break;
                    }
                case ProjectileID.NanoBullet:
                    {
                        LaserColor = Color.Blue;
                        break;
                    }
                case ProjectileID.ExplosiveBullet:
                    {
                        LaserColor = Color.OrangeRed;
                        break;
                    }
                case ProjectileID.GoldenBullet:
                    {
                        LaserColor = Color.Gold;
                        break;
                    }
                case ProjectileID.MoonlordBullet:
                    {
                        LaserColor = Color.Teal;
                        break;
                    }
            }

            CastLight = true;
            LightColor = LaserColor;

            
            if (Projectile.ai[0] == ProjectileID.MeteorShot || Projectile.ai[0] == ProjectileID.MoonlordBullet)
            {
                PierceNPCs = true;
            }

            base.AI();

            if (!hasHit)
            {
                if (Projectile.ai[0] != ProjectileID.ExplosiveBullet && Projectile.ai[0] != ProjectileID.CrystalBullet && Projectile.ai[0] != -999)
                {
                    //Spawn any effects of the projectile (confetti, dust, etc) at its endpoint
                    Projectile.NewProjectileDirect(Projectile.GetSource_FromThis(), Projectile.Center + (Projectile.velocity * (Distance - 16)), Projectile.velocity, (int)Projectile.ai[0], 0, 0, Projectile.owner).Kill();

                    //Meteor shot reflects once
                    if (Projectile.ai[0] == ProjectileID.MeteorShot)
                    {
                        Vector2 colVel1 = Projectile.velocity;
                        Vector2 collision1 = Projectile.Center + Projectile.velocity * (Distance - 80);

                        Vector2 testCol1 = Vector2.Zero;
                        Vector2 testCol2 = Vector2.Zero;

                        Vector2 reflectionA = colVel1;
                        reflectionA.Y *= -1;
                        Vector2 reflectionB = colVel1;
                        reflectionB.X *= -1;

                        //Calcuate how the lasers should reflect
                        //If it's aiming up right or down left
                        if (colVel1.X >= 0 == colVel1.Y >= 0)
                        {
                            //Run test collisions aiming up left and down right to see which direction is longer
                            testCol1 = UsefulFunctions.GetFirstCollision(collision1, reflectionA, 5000, true, true);
                            testCol2 = UsefulFunctions.GetFirstCollision(collision1, reflectionB, 5000, true, true);
                        }
                        //And vice versa with quadrants 2/4 and 1/3
                        else if (colVel1.X < 0 == colVel1.Y > 0)
                        {
                            testCol1 = UsefulFunctions.GetFirstCollision(collision1, reflectionA, 5000, true, true);
                            testCol2 = UsefulFunctions.GetFirstCollision(collision1, reflectionB, 5000, true, true);
                        }

                        Vector2 collision2;
                        if (collision1.DistanceSQ(testCol1) > collision1.DistanceSQ(testCol2))
                        {
                            collision2 = testCol1;
                        }
                        else
                        {
                            collision2 = testCol2;
                        }

                        Vector2 colVel2 = collision2 - collision1;
                        colVel2.Normalize();

                        Projectile.NewProjectile(Projectile.GetSource_FromThis(), collision1, colVel2, ModContent.ProjectileType<CrystalRay>(), (int)(Projectile.damage / 2f), 0, Projectile.owner, -999);
                    }
                }
                else if (Projectile.ai[0] == ProjectileID.CrystalBullet)
                {
                    for (int i = 0; i < 2; i++)
                    {
                        Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center + Projectile.velocity * (Distance - 70), Main.rand.NextVector2Circular(15, 15), ProjectileID.CrystalShard, Projectile.damage / 8, 0.5f, Projectile.owner);
                    }
                }
                else if (Projectile.ai[0] == ProjectileID.ExplosiveBullet)
                {
                    Terraria.Audio.SoundEngine.PlaySound(SoundID.Item62 with { Volume = 0.5f, Pitch = 1.1f }, Projectile.Center + Projectile.velocity * (Distance - 32));
                    Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center + Projectile.velocity * (Distance - 32), Vector2.Zero, ModContent.ProjectileType<Projectiles.FireballInferno1>(), Projectile.damage, 0.5f, Projectile.owner, -999);
                    Projectile.damage = 0;
                }

                Terraria.Audio.SoundEngine.PlaySound(SoundID.Dig with { Volume = 0.5f, Pitch = 1.1f }, Projectile.Center + Projectile.velocity * (Distance - 32));
            }

            hasHit = true;
            //Projectile.Damage();
            //Only deal damage on the first frame of attack
            //Projectile.damage = 0;
        }

        public override void OnHitNPC(NPC target, int damage, float knockback, bool crit)
        {
            switch (Projectile.ai[0])
            {
                case ProjectileID.CursedBullet:
                    {
                        target.AddBuff(BuffID.CursedInferno, 420);
                        break;
                    }
                case ProjectileID.IchorBullet:
                    {
                        target.AddBuff(BuffID.Ichor, 420);
                        break;
                    }
                case ProjectileID.VenomBullet:
                    {
                        target.AddBuff(BuffID.Venom, 420);
                        break;
                    }
                case ProjectileID.PartyBullet:
                    {
                        //Spawn confetti
                        break;
                    }
                case ProjectileID.NanoBullet:
                    {
                        target.AddBuff(BuffID.Confused, 420);
                        break;
                    }
                case ProjectileID.GoldenBullet:
                    {
                        target.AddBuff(BuffID.Midas, 420);
                        break;
                    }
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            base.PreDraw(ref lightColor);
            return false;
        }
        public void DrawCrystalLaser(Texture2D texture, Vector2 start, Vector2 unit, Rectangle headRect, Rectangle bodyRect, Rectangle tailRect, float rotation = 0f, float scale = 1f, Color color = default)
        {
            //Defines an area where laser segments should actually draw, 100 pixels larger on each side than the screen
            Rectangle screenRect = new Rectangle((int)Main.screenPosition.X - 100, (int)Main.screenPosition.Y - 100, Main.screenWidth + 100, Main.screenHeight + 100);

            float r = unit.ToRotation() + rotation;
            Rectangle bodyFrame = bodyRect;
            bodyFrame.X = bodyRect.Width * currentFrame;
            Rectangle headFrame = headRect;
            headFrame.X *= headRect.Width * currentFrame;
            Rectangle tailFrame = tailRect;
            tailFrame.X *= tailRect.Width * currentFrame;

            float percentage = ((float)FiringTimeLeft) / (float)FiringDuration;
            scale *= percentage;

            if(scale == 0)
            {
                return;
            }

            color = color * percentage * percentage;

            Vector2 startPos = start;

            //Laser body
            float end = Distance - ((bodyFrame.Height) * scale);
            float i = end;
            for (; i >= 0; i -= (bodyFrame.Height) * scale)
            {
                Vector2 drawStart = startPos + i * unit;
                if (FastContainsPoint(screenRect, drawStart))
                {
                    Main.EntitySpriteDraw(texture, drawStart - Main.screenPosition, bodyFrame, color, r, new Vector2(bodyRect.Width * .5f, bodyRect.Height * .5f), scale, 0, 0);
                }
            }

            //Laser tail
            i = end;
            i += (LaserTextureTail.Height + 3) * scale; //Slightly fudged, need to find out why the laser tail is still misaligned for certain texture sizes
            startPos += i * unit;

            if (FastContainsPoint(screenRect, startPos))
            {
                Main.EntitySpriteDraw(texture, startPos - Main.screenPosition, tailFrame, color, r, new Vector2(tailRect.Width * .5f, tailRect.Height * .5f), scale, 0, 0);
            }
        }
    }

}

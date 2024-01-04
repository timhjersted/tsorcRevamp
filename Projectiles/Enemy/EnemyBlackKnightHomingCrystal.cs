using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.Buffs.Debuffs;


namespace tsorcRevamp.Projectiles.Enemy
{
    class EnemyBlackKnightHomingCrystal : ModProjectile
    {
        private float spinSpeed; // Current spin speed of the projectile
        private const float spinAcceleration = 0.001f; // How much the spin speed increases per AI update

        public override void SetDefaults()
        {
            Projectile.aiStyle = -1;
            Projectile.width = 26;
            Projectile.height = 20;
            Projectile.scale = 1f;
            Projectile.timeLeft = 240;
            Projectile.hostile = true;
            Projectile.penetrate = 1;
            Projectile.DamageType = DamageClass.Magic;
            //Projectile.tileCollide = false;
            Projectile.damage = 35;
            Projectile.knockBack = 12;
            Projectile.light = 0.7f;
        }


        int timer = 0;

        public override void AI()
        {

            // Increase the spin speed gradually over time
            spinSpeed += spinAcceleration;

            // Rotate the projectile based on the spin speed
            Projectile.rotation += spinSpeed;
            Projectile.tileCollide = false;
            if (timer > 60 && timer < 240)
            {
                Projectile.tileCollide = true;
                Player closest = UsefulFunctions.GetClosestPlayer(Projectile.Center);

                if (closest != null)
                {
                    UsefulFunctions.SmoothHoming(Projectile, closest.Center, 0.2f, 5, closest.velocity, false);
                }

                if (!Main.dedServ)
                {

                    // Smoke and fuse dust spawn.
                    if (Main.rand.NextBool(2))
                    {

                        int dustIndex = Dust.NewDust(new Vector2(Projectile.position.X, Projectile.position.Y), Projectile.width, Projectile.height, 6, 0f, 0f, 100, Color.MediumPurple, 1f);
                        Main.dust[dustIndex].scale = 0.1f + (float)Main.rand.Next(5) * 0.1f;
                        Main.dust[dustIndex].fadeIn = .5f + (float)Main.rand.Next(5) * 0.1f;
                        Main.dust[dustIndex].noGravity = true;

                        for (int i = 0; i < 2; i++)
                        {
                            int purple = Dust.NewDust(Projectile.position, Projectile.width * 2, Projectile.height, DustID.ShadowbeamStaff, Projectile.velocity.X, Projectile.velocity.Y, Scale: 0.5f);
                            Main.dust[purple].noGravity = true;
                            int wither = Dust.NewDust(new Vector2(Projectile.position.X, Projectile.position.Y), Projectile.width, Projectile.height, DustID.ShadowbeamStaff, 0f, 0f, 100, Color.MediumPurple, 0.5f);
                            Main.dust[wither].noGravity = true;
                        }

                    }


                    /*
                    for (int i = 0; i < 10; i++)
                    {
                        float velX = 2f - ((float)Main.rand.Next(20)) / 5f;
                        float velY = 2f - ((float)Main.rand.Next(20)) / 5f;
                        velX *= 2f;
                        velY *= 2f;
                        Dust.NewDust(new Vector2(Projectile.position.X - (float)(Projectile.width / 2), Projectile.position.Y - (float)(Projectile.height / 2)), Projectile.width, Projectile.height, DustID.Wraith, velX, velY, 160, default, 2f);
                        Dust.NewDust(new Vector2(Projectile.position.X - (float)(Projectile.width / 2), Projectile.position.Y - (float)(Projectile.height / 2)), Projectile.width, Projectile.height, DustID.FireflyHit, velX, velY, 160, default, 2f);
                      
                        Dust.NewDust(new Vector2(Projectile.position.X - (float)(Projectile.width / 2), Projectile.position.Y - (float)(Projectile.height / 2)), Projectile.width, Projectile.height, DustID.SparkForLightDisc, velX, velY, 160, default, 1f);
                    }
                    */

                }
            }
            else
            {
                Projectile.velocity *= 0.85f;
            }

            timer++;
        }

        #region Kill   
        public override void OnKill(int timeLeft)
        {
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Zombie82 with { Volume = 0.6f, Pitch = -3f, PitchVariance = 2f, MaxInstances = 5 }, Projectile.Center); //wraith


            // setup projectile for explosion
            Projectile.damage = (int)(Projectile.damage * 1.5f);
            Projectile.penetrate = 20;
            Projectile.width = Projectile.width * 3;
            Projectile.height = Projectile.height * 3;
            Projectile.position.X = Projectile.position.X - (float)(Projectile.width / 2);
            Projectile.position.Y = Projectile.position.Y - (float)(Projectile.height / 2);
            Projectile.timeLeft = 1;
            //Projectile.Damage();

            //if (Projectile.owner == Main.myPlayer)
            //{
            //    Projectile.NewProjectileDirect(Projectile.GetSource_FromThis(), Projectile.position, Vector2.Zero, ModContent.ProjectileType<Projectiles.VFX.ExplosionFlash>(), 10, 10, Main.myPlayer, 400, 30);
            //}

            // Fire Dust spawn
            for (int i = 0; i < 30; i++)
            {
                int dustIndex = Dust.NewDust(new Vector2(Projectile.position.X, Projectile.position.Y), Projectile.width, Projectile.height, 6, Main.rand.Next(-6, 6), Main.rand.Next(-6, 6), 100, default(Color), 3f);
                Main.dust[dustIndex].noGravity = true;
                Main.dust[dustIndex].velocity *= 2.5f;
            }
            // Large Smoke Gore spawn
            for (int g = 0; g < 2; g++) //6 was 2
            {
                if (!Main.dedServ)
                {
                    int goreIndex = Gore.NewGore(Projectile.GetSource_Death(), new Vector2(Projectile.position.X + (float)(Projectile.width / 2), Projectile.position.Y + (float)(Projectile.height / 2)), default(Vector2), Main.rand.Next(61, 64), .8f);
                    Main.gore[goreIndex].scale = 2f;
                    Main.gore[goreIndex].velocity.X = Main.gore[goreIndex].velocity.X + 1f;
                    Main.gore[goreIndex].velocity.Y = Main.gore[goreIndex].velocity.Y + 1f;
                    goreIndex = Gore.NewGore(Projectile.GetSource_Death(), new Vector2(Projectile.position.X + (float)(Projectile.width / 2), Projectile.position.Y + (float)(Projectile.height / 2)), default(Vector2), Main.rand.Next(61, 64), .8f);
                    Main.gore[goreIndex].scale = 2f;
                    Main.gore[goreIndex].velocity.X = Main.gore[goreIndex].velocity.X - 1f;
                    Main.gore[goreIndex].velocity.Y = Main.gore[goreIndex].velocity.Y + 1f;
                }
            }


            // create unknown embers that fill the explosion's radius
            for (int i = 0; i < 30; i++)
            {
                float velX = 1f - ((float)Main.rand.Next(20)) / 5f;
                float velY = 1f - ((float)Main.rand.Next(20)) / 5f;
                velX *= 2f;
                velY *= 2f;
                Dust.NewDust(new Vector2(Projectile.position.X, Projectile.position.Y), Projectile.width, Projectile.height, DustID.ShadowbeamStaff, velX, velY, 160, default, 1.5f);
                Dust.NewDust(new Vector2(Projectile.position.X, Projectile.position.Y), Projectile.width, Projectile.height, DustID.ShadowbeamStaff, velX, velY, 160, default, 1.5f);
                //Dust.NewDust(new Vector2(Projectile.position.X - (float)(Projectile.width / 2), Projectile.position.Y - (float)(Projectile.height / 2)), Projectile.width / 2, Projectile.height / 2, DustID.Firefly, velX, velY, 160, default, 1f);
                //Dust.NewDust(new Vector2(Projectile.position.X - (float)(Projectile.width / 2), Projectile.position.Y - (float)(Projectile.height / 2)), Projectile.width / 2, Projectile.height / 2, DustID.Firefly, velX, velY, 160, default, 1f);

            }



        }
        #endregion
        /*
        public override bool? CanHitNPC(NPC target)
        {
            if (target.immune[Projectile.owner] > 0)
            {
                return false;
            }
            else
            {
                return true;
            }
        }
        */

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            // Do nothing here to prevent damaging NPCs
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(ModContent.BuffType<CurseBuildup>(), 36000);
            target.AddBuff(BuffID.Weak, 180);

            Terraria.Audio.SoundEngine.PlaySound(SoundID.Zombie82 with { Volume = 0.6f, Pitch = -3f, PitchVariance = 2f, MaxInstances = 5 }, Projectile.Center); //wraith


            // setup projectile for explosion
            Projectile.damage = (int)(Projectile.damage * 1.5f);
            Projectile.penetrate = 20;
            Projectile.width = Projectile.width * 3;
            Projectile.height = Projectile.height * 3;
            Projectile.position.X = Projectile.position.X - (float)(Projectile.width / 2);
            Projectile.position.Y = Projectile.position.Y - (float)(Projectile.height / 2);
            Projectile.timeLeft = 1;
            //Projectile.Damage();

            //if (Projectile.owner == Main.myPlayer)
            //{
            //    Projectile.NewProjectileDirect(Projectile.GetSource_FromThis(), Projectile.position, Vector2.Zero, ModContent.ProjectileType<Projectiles.VFX.ExplosionFlash>(), 10, 10, Main.myPlayer, 400, 30);
            //}

            // Fire Dust spawn
            for (int i = 0; i < 30; i++)
            {
                int dustIndex = Dust.NewDust(new Vector2(Projectile.position.X, Projectile.position.Y), Projectile.width, Projectile.height, 6, Main.rand.Next(-6, 6), Main.rand.Next(-6, 6), 100, default(Color), 3f);
                Main.dust[dustIndex].noGravity = true;
                Main.dust[dustIndex].velocity *= 2.5f;
            }
            // Large Smoke Gore spawn
            for (int g = 0; g < 2; g++) //6 was 2
            {
                if (!Main.dedServ)
                {
                    int goreIndex = Gore.NewGore(Projectile.GetSource_Death(), new Vector2(Projectile.position.X + (float)(Projectile.width / 2), Projectile.position.Y + (float)(Projectile.height / 2)), default(Vector2), Main.rand.Next(61, 64), .8f);
                    Main.gore[goreIndex].scale = 2f;
                    Main.gore[goreIndex].velocity.X = Main.gore[goreIndex].velocity.X + 1f;
                    Main.gore[goreIndex].velocity.Y = Main.gore[goreIndex].velocity.Y + 1f;
                    goreIndex = Gore.NewGore(Projectile.GetSource_Death(), new Vector2(Projectile.position.X + (float)(Projectile.width / 2), Projectile.position.Y + (float)(Projectile.height / 2)), default(Vector2), Main.rand.Next(61, 64), .8f);
                    Main.gore[goreIndex].scale = 2f;
                    Main.gore[goreIndex].velocity.X = Main.gore[goreIndex].velocity.X - 1f;
                    Main.gore[goreIndex].velocity.Y = Main.gore[goreIndex].velocity.Y + 1f;
                }
            }


            // create unknown embers that fill the explosion's radius
            for (int i = 0; i < 30; i++)
            {
                float velX = 1f - ((float)Main.rand.Next(20)) / 5f;
                float velY = 1f - ((float)Main.rand.Next(20)) / 5f;
                velX *= 2f;
                velY *= 2f;
                Dust.NewDust(new Vector2(Projectile.position.X, Projectile.position.Y), Projectile.width, Projectile.height, DustID.ShadowbeamStaff, velX, velY, 160, default, 1.5f);
                Dust.NewDust(new Vector2(Projectile.position.X, Projectile.position.Y), Projectile.width, Projectile.height, DustID.ShadowbeamStaff, velX, velY, 160, default, 1.5f);
                //Dust.NewDust(new Vector2(Projectile.position.X - (float)(Projectile.width / 2), Projectile.position.Y - (float)(Projectile.height / 2)), Projectile.width / 2, Projectile.height / 2, DustID.Firefly, velX, velY, 160, default, 1f);
                //Dust.NewDust(new Vector2(Projectile.position.X - (float)(Projectile.width / 2), Projectile.position.Y - (float)(Projectile.height / 2)), Projectile.width / 2, Projectile.height / 2, DustID.Firefly, velX, velY, 160, default, 1f);

            }
        }

        //public override bool PreDraw(ref Color lightColor)
        //{
        //    Main.spriteBatch.Draw(((Texture2D)Terraria.GameContent.TextureAssets.Projectile[Projectile.type]), Projectile.Center - Main.screenPosition, Terraria.GameContent.TextureAssets.Projectile[Projectile.type].Frame(), Color.White, 0, Terraria.GameContent.TextureAssets.Projectile[Projectile.type].Size(), 1, SpriteEffects.None, 0); // 1 was 2
        //    return false;
        //}
    }
}

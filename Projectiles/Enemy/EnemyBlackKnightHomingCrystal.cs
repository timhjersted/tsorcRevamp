using Microsoft.CodeAnalysis;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
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
            Projectile.timeLeft = 460;
            Projectile.hostile = true;          
            Projectile.penetrate = 1;
            //Projectile.DamageType = DamageClass.Magic;
            Projectile.tileCollide = true;
            Projectile.damage = 35;
            Projectile.knockBack = 12;
            Projectile.light = 1;
        }


        int timer = 0;

        public override void AI()
        {

            // Increase the spin speed gradually over time
            spinSpeed += spinAcceleration;

            // Rotate the projectile based on the spin speed
            Projectile.rotation += spinSpeed;

            if (timer > 60 && timer < 300)
            {
                Player closest = UsefulFunctions.GetClosestPlayer(Projectile.Center);

                if (closest != null)
                {
                    UsefulFunctions.SmoothHoming(Projectile, closest.Center, 0.2f, 5, closest.velocity, false);
                }

                if (!Main.dedServ)
                {
                    //Gore.NewGore(Projectile.GetSource_FromThis(), Projectile.position - new Vector2(16, 16) - Projectile.velocity * 1, Vector2.Zero, Main.rand.Next(61, 64), Main.rand.NextFloat(0.1f, 0.5f));
                    //Gore.NewGore(Projectile.GetSource_FromThis(), Projectile.position - new Vector2(16, 16) - Projectile.velocity * 1, Vector2.Zero, Main.rand.Next(61, 64), Main.rand.NextFloat(0.1f, 0.5f));
                    //Gore.NewGore(Projectile.GetSource_FromThis(), Projectile.position - new Vector2(16, 16) - Projectile.velocity * 1, Vector2.Zero, Main.rand.Next(61, 64), Main.rand.NextFloat(0.1f, 0.5f));

                    // create unknown embers that fill the explosion's radius
                    for (int i = 0; i < 30; i++)
                    {
                        float velX = 2f - ((float)Main.rand.Next(20)) / 5f;
                        float velY = 2f - ((float)Main.rand.Next(20)) / 5f;
                        velX *= 4f;
                        velY *= 4f;
                        Dust.NewDust(new Vector2(Projectile.position.X - (float)(Projectile.width / 2), Projectile.position.Y - (float)(Projectile.height / 2)), Projectile.width, Projectile.height, DustID.Wraith, velX, velY, 160, default, 1f);
                        Dust.NewDust(new Vector2(Projectile.position.X - (float)(Projectile.width / 2), Projectile.position.Y - (float)(Projectile.height / 2)), Projectile.width, Projectile.height, DustID.Wraith, velX, velY, 160, default, 1f);
                        //Dust.NewDust(new Vector2(Projectile.position.X - (float)(Projectile.width / 2), Projectile.position.Y - (float)(Projectile.height / 2)), Projectile.width, Projectile.height, DustID.Firefly, velX, velY, 160, default, 1f);
                        Dust.NewDust(new Vector2(Projectile.position.X - (float)(Projectile.width / 2), Projectile.position.Y - (float)(Projectile.height / 2)), Projectile.width, Projectile.height, DustID.SparkForLightDisc, velX, velY, 160, default, 1f);
                    }

                }
            }
            else
            {
                Projectile.velocity *= 0.95f;
            }

            timer++;
        }

        #region Kill   
        public override void Kill(int timeLeft)
        {
            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item14, Projectile.Center);

            // setup projectile for explosion
            Projectile.damage = Projectile.damage * 2;
            Projectile.penetrate = 20;
            Projectile.width = Projectile.width << 3;
            Projectile.height = Projectile.height << 3;

            Projectile.Damage();

            if (Projectile.owner == Main.myPlayer)
            {
                //Projectile.NewProjectileDirect(Projectile.GetSource_FromThis(), Projectile.position, Vector2.Zero, ModContent.ProjectileType<Projectiles.VFX.ExplosionFlash>(), 10, 0, Main.myPlayer, 400, 30);
            }

            // create unknown embers that fill the explosion's radius
            for (int i = 0; i < 30; i++)
            {
                float velX = 2f - ((float)Main.rand.Next(20)) / 5f;
                float velY = 2f - ((float)Main.rand.Next(20)) / 5f;
                velX *= 4f;
                velY *= 4f;
                Dust.NewDust(new Vector2(Projectile.position.X - (float)(Projectile.width / 2), Projectile.position.Y - (float)(Projectile.height / 2)), Projectile.width, Projectile.height, DustID.EnchantedNightcrawler, velX, velY, 160, default, 1.5f);
                Dust.NewDust(new Vector2(Projectile.position.X - (float)(Projectile.width / 2), Projectile.position.Y - (float)(Projectile.height / 2)), Projectile.width, Projectile.height, DustID.CosmicEmber, velX, velY, 160, default, 1.5f);
            }

            if (!Projectile.active)
            {
                return;
            }
            Projectile.timeLeft = 0;
            {
                Terraria.Audio.SoundEngine.PlaySound(SoundID.NPCHit54, Projectile.Center);
                if (Projectile.owner == Main.myPlayer) Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.position.X + (float)(Projectile.width / 2), Projectile.position.Y + (float)(Projectile.height / 2), 0, 0, ModContent.ProjectileType<Projectiles.Enemy.EnemySpellSuddenDeathBall>(), Projectile.damage, 3f, Projectile.owner);
                Vector2 arg_1394_0 = new Vector2(Projectile.position.X - Projectile.velocity.X, Projectile.position.Y - Projectile.velocity.Y);
                int arg_1394_1 = Projectile.width;
                int arg_1394_2 = Projectile.height;
                int arg_1394_3 = 15;
                float arg_1394_4 = 0f;
                float arg_1394_5 = 0f;
                int arg_1394_6 = 100;
                Color newColor = default(Color);
                int num41 = Dust.NewDust(arg_1394_0, arg_1394_1, arg_1394_2, arg_1394_3, arg_1394_4, arg_1394_5, arg_1394_6, newColor, 2f);
                Main.dust[num41].noGravity = true;
                Dust expr_13B1 = Main.dust[num41];
                expr_13B1.velocity *= 2f;
                Vector2 arg_1422_0 = new Vector2(Projectile.position.X - Projectile.velocity.X, Projectile.position.Y - Projectile.velocity.Y);
                int arg_1422_1 = Projectile.width;
                int arg_1422_2 = Projectile.height;
                int arg_1422_3 = 15;
                float arg_1422_4 = 0f;
                float arg_1422_5 = 0f;
                int arg_1422_6 = 100;
                newColor = default(Color);
                num41 = Dust.NewDust(arg_1422_0, arg_1422_1, arg_1422_2, arg_1422_3, arg_1422_4, arg_1422_5, arg_1422_6, newColor, 1f);
            }
           
            // terminate projectile
            Projectile.active = false;

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

       
        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(ModContent.BuffType<CurseBuildup>(), 36000);       
            target.AddBuff(BuffID.Weak, 180);
        }

        //public override bool PreDraw(ref Color lightColor)
        //{
        //    Main.spriteBatch.Draw(((Texture2D)Terraria.GameContent.TextureAssets.Projectile[Projectile.type]), Projectile.Center - Main.screenPosition, Terraria.GameContent.TextureAssets.Projectile[Projectile.type].Frame(), Color.White, 0, Terraria.GameContent.TextureAssets.Projectile[Projectile.type].Size(), 1, SpriteEffects.None, 0); // 1 was 2
        //    return false;
        //}
    }
}

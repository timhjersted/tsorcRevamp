using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Graphics;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy.Triad
{
    class HomingStar : ModProjectile
    {
        
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Seeking Star");
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 60;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 3;

        }
        public override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.scale = 1.1f;
            Projectile.timeLeft = 1000;
            Projectile.hostile = true;
            Projectile.tileCollide = false;
            Projectile.friendly = false;
        }

        bool playedSound = false;
        float homingAcceleration = 0.15f;
        public override void AI()
        {
            Player target = UsefulFunctions.GetClosestPlayer(Projectile.Center);
            Projectile.rotation = Projectile.velocity.ToRotation();
            if (!playedSound)
            {
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item43 with { Volume = 0.5f}, Projectile.Center);

                if(Projectile.ai[1] == 1)
                {
                    Projectile.NewProjectileDirect(Projectile.GetSource_FromThis(), Projectile.Center, Projectile.velocity, ModContent.ProjectileType<Projectiles.Trails.HomingStarTrail>(), Projectile.damage, 0, Main.myPlayer, 0, Projectile.whoAmI);

                    if (Projectile.ai[0] == 0)
                    {
                        homingAcceleration = 0.05f;
                    }
                }

                //No homing
                if (Projectile.ai[0] == 2)
                {
                    homingAcceleration = 0;
                }

                playedSound = true;
            }

            
            //Accelerate downwards, do not despawn until impact
            if (Projectile.ai[0] == 1)
            {
                Projectile.timeLeft = 1000;
                float speedCap = 10;
                if(Projectile.ai[1] == 1)
                {
                    speedCap = 8;
                }
                if (Projectile.velocity.Y < speedCap)
                {
                    Projectile.velocity.Y += 1f;
                }
                homingAcceleration = 0;
            }

            //Stop homing after a few seconds
            if(Projectile.timeLeft < 800)
            {
                homingAcceleration = 0;
            }

            //Perform homing
            if (target != null)
            {
                UsefulFunctions.SmoothHoming(Projectile, target.Center, homingAcceleration, 20, target.velocity, false);
            }
        }

        public override void Kill(int timeLeft)
        {
            if (Projectile.ai[1] == 0)
            {
                for (int i = 0; i < 40; i++)
                {
                    Vector2 dustVel = Main.rand.NextVector2Circular(5, 5);
                    dustVel += Projectile.velocity / 3f;

                    switch (i % 4)
                    {
                        case 0:
                            Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.FireworkFountain_Blue, dustVel.X, dustVel.Y);
                            break;
                        case 1:
                            Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.BlueFairy, dustVel.X, dustVel.Y);
                            break;
                        case 2:
                            Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.PinkFairy, dustVel.X, dustVel.Y);
                            break;
                        case 3:
                            Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, DustID.BlueCrystalShard, dustVel.X, dustVel.Y);
                            break;
                    }
                }
            }
        }

        Texture2D texture;
        Texture2D starTexture;
        float starRotation;
        public override bool PreDraw(ref Color lightColor)
        {
            if (Projectile.ai[1] == 1)
            {
                return false;
            }
            if (texture == null || texture.IsDisposed)
            {
                texture = (Texture2D)ModContent.Request<Texture2D>(Texture, ReLogic.Content.AssetRequestMode.ImmediateLoad);
            }
            if (starTexture == null || starTexture.IsDisposed)
            {
                starTexture = (Texture2D)ModContent.Request<Texture2D>("tsorcRevamp/Projectiles/Enemy/Triad/HomingStarStar", ReLogic.Content.AssetRequestMode.ImmediateLoad);
            }

            Rectangle sourceRectangle = new Rectangle(0, 0, texture.Width, texture.Height);
            Rectangle starSourceRectangle = new Rectangle(0, 0, starTexture.Width, starTexture.Height);
            Vector2 origin = sourceRectangle.Size() / 2f;
            origin.Y += 20;
            Vector2 starOrigin = starSourceRectangle.Size() / 2f;
            DrawOriginOffsetY = 100;

            Vector2 offset = Projectile.position - Projectile.Center;
            //Draw shadow trails
            for (float i = 5; i >= 0; i--)
            {
                Main.spriteBatch.Draw(texture, Projectile.oldPos[(int)i * 2] - Main.screenPosition - offset, sourceRectangle, Color.Cyan * ((6 - i) / 6), Projectile.oldRot[(int)i * 2] - MathHelper.PiOver2, origin, Projectile.scale, SpriteEffects.None, 0);
            }
            Main.spriteBatch.Draw(texture, Projectile.Center - Main.screenPosition, sourceRectangle, Color.White, Projectile.rotation - MathHelper.PiOver2, origin, Projectile.scale, SpriteEffects.None, 0);
            Vector2 starOffset = Projectile.velocity;
            starOffset.Normalize();
            Main.spriteBatch.Draw(starTexture, Projectile.Center - Main.screenPosition, starSourceRectangle, Color.White, Projectile.rotation + starRotation, starOrigin, Projectile.scale * 0.75f, SpriteEffects.None, 0);
            starRotation += 0.1f;
            return false;
        }
    }
}

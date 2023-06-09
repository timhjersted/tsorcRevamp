using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    class EnemyRedirectingShark : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 4;
        }
        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.aiStyle = 8;
            Projectile.timeLeft = 600;
            Projectile.damage = 70;
            Projectile.light = 0.8f;
            Projectile.penetrate = 1;
            Projectile.hostile = true;
            Projectile.aiStyle = -1;
        }

        public override bool PreKill(int timeLeft)
        {
            return true;
        }

        int timer = 0;
        bool initialSetup = false;
        bool delayedMode = false;
        public override void PostAI()
        {
            
            Projectile.rotation = Projectile.velocity.ToRotation();


            int dust = Dust.NewDust(new Vector2((float)Projectile.position.X, (float)Projectile.position.Y), Projectile.width, Projectile.height, DustID.FireworkFountain_Red, Projectile.velocity.X / 3, Projectile.velocity.Y / 3, 50, Color.Chartreuse, 0.5f);
            Main.dust[dust].noGravity = true;

            if (Projectile.velocity.X <= 4 && Projectile.velocity.Y <= 4 && Projectile.velocity.X >= -4 && Projectile.velocity.Y >= -4)
            {
                float accel = 2f + (Main.rand.Next(10, 30) * 0.001f);
                Projectile.velocity.X *= accel;
                Projectile.velocity.Y *= accel;
            }

            if (!initialSetup)
            {
                initialSetup = true;
                if (Projectile.ai[0] != 0)
                {
                    delayedMode = true;
                    timer = 120;
                    Projectile.tileCollide = false;
                }
                else
                {
                    Projectile.tileCollide = true;
                }
            }

            if (delayedMode && timer > 0)
            {
                timer--;
                Projectile.velocity = Vector2.Zero;
                Vector2 targetingVector = UsefulFunctions.Aim(Projectile.Center, Main.player[(int)Projectile.ai[1]].Center, 1);
                Projectile.rotation = targetingVector.ToRotation();
                if (timer == 0)
                {
                    float velocity = 8;
                    if (Main.tile[(int)Projectile.Center.X / 16, (int)Projectile.Center.Y / 16].LiquidAmount != 0)
                    {
                        velocity = 5;
                    }
                    Projectile.velocity = targetingVector * velocity;
                }
            }
        }

        public static Texture2D texture;
        public override bool PreDraw(ref Color lightColor)
        {
            if(texture == null || texture.IsDisposed)
            {
                texture = (Texture2D)ModContent.Request<Texture2D>(Texture, ReLogic.Content.AssetRequestMode.ImmediateLoad);
            }

            Projectile.frameCounter++;
            
            if(Projectile.frameCounter > 4)
            {
                Projectile.frame++;
                if (Projectile.frame >= Main.projFrames[Projectile.type])
                {
                    Projectile.frame = 0;
                }

                Projectile.frameCounter = 0;
            }


            SpriteEffects spriteEffects = SpriteEffects.None;
            if (Projectile.velocity.X < 0)
            {
                spriteEffects = SpriteEffects.FlipVertically;
            }

            //Get the premultiplied, properly transparent texture
            int frameHeight = texture.Height / Main.projFrames[Projectile.type];
            int startY = frameHeight * Projectile.frame;
            Rectangle sourceRectangle = new Rectangle(0, startY, texture.Width, frameHeight);
            Vector2 origin = sourceRectangle.Size() / 2f;
            //origin.X = (float)(projectile.spriteDirection == 1 ? sourceRectangle.Width - 20 : 20);
            Main.EntitySpriteDraw(texture,
                Projectile.Center - Main.screenPosition + new Vector2(0f, Projectile.gfxOffY),
                sourceRectangle, Color.White, Projectile.rotation, origin, Projectile.scale, spriteEffects, 0);


            return false;
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            delayedMode = true;
            timer = 120;
            Projectile.timeLeft = 600;
            Projectile.tileCollide = false;

            return false;
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            int buffLengthMod = 1;
            if (Main.expertMode)
            {
                buffLengthMod = 2;
            }
            target.AddBuff(BuffID.BrokenArmor, 180 / buffLengthMod, false);
            target.AddBuff(BuffID.Bleeding, 1800 / buffLengthMod, false);
        }
    }
}

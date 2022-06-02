using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    class GreySlash : ModProjectile
    {

        public override void SetStaticDefaults()
        {
            Main.projFrames[Projectile.type] = 7;
        }

        public override void SetDefaults()
        {
            Projectile.width = 58;
            Projectile.height = 46;
            Projectile.damage = 10;
            Projectile.penetrate = 3;
            Projectile.hostile = true;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.alpha = 80;

        }

        float angle;
        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation();
            Projectile.spriteDirection = Projectile.direction;
            if (Projectile.spriteDirection == -1)
            {
                Projectile.rotation -= MathHelper.ToRadians(180f);
            }

            if (Main.rand.Next(3) == 0)
            {
                Dust dust2 = Main.dust[Dust.NewDust(new Vector2(Projectile.position.X, Projectile.position.Y), Projectile.width, Projectile.height, 89, 0, 0, 50, default(Color), .8f)];
                dust2.velocity *= 0;
                dust2.noGravity = true;
            }

            if (Projectile.ai[0] >= 10)
            {
                for (int i = 0; i < 3; i++)
                {
                    Dust dust2 = Main.dust[Dust.NewDust(new Vector2(Projectile.position.X - 10, Projectile.position.Y - 10), Projectile.width + 20, Projectile.height + 20, 89, 0, 0, 50, default(Color), 1f)];
                    dust2.velocity *= 0;
                    dust2.noGravity = true;
                }
            }

            //Proj when moving slow
            NPC owner = Main.npc[(int)Projectile.ai[1]];
            if (Projectile.ai[0] < 10)
            {
                if (Projectile.ai[0] < 1)
                {
                    ++Projectile.ai[0];
                    Vector2 difference = Projectile.Center - owner.Center;
                    angle = difference.ToRotation();
                }

                if (Projectile.ai[0] >= 1)
                {
                    //Create a new Vector2 with length offsetDistance, and then rotate it toward the correct direction
                    Vector2 offset = new Vector2(34, 0).RotatedBy(angle);
                    //Add that to the npc's position
                    Projectile.Center = owner.Center + offset;
                }
            }


            //Proj when moving fast
            if (Projectile.ai[0] >= 10 && Projectile.ai[0] < 20)
            {
                if (Projectile.ai[0] < 11)
                {
                    ++Projectile.ai[0];
                    Vector2 difference = Projectile.Center - owner.Center;
                    angle = difference.ToRotation();
                }

                if (Projectile.ai[0] >= 1)
                {
                    //Create a new Vector2 with length offsetDistance, and then rotate it toward the correct direction
                    Vector2 offset = new Vector2(34, 0).RotatedBy(angle);
                    //Add that to the npc's position
                    Projectile.Center = owner.Center + offset;
                }
            }


            if (++Projectile.frameCounter >= 2)
            {
                Projectile.frameCounter = 0;
                if (++Projectile.frame > 6)
                {
                    Projectile.Kill();
                }
            }
        }

        public override void OnHitPlayer(Player target, int damage, bool crit)
        {
            if (Projectile.ai[0] >= 10 && Projectile.ai[0] < 20)
            {
                target.AddBuff(ModContent.BuffType<Buffs.Crippled>(), 900);
            }
            else
            {
                target.AddBuff(ModContent.BuffType<Buffs.Crippled>(), 600);
            }
        }
        public override bool PreDraw(ref Color lightColor)
        {
            SpriteEffects spriteEffects = SpriteEffects.None;

            if (Projectile.spriteDirection == -1)
            {
                spriteEffects = SpriteEffects.FlipHorizontally;
            }

            Color alphalowered;
            Texture2D texture = (Texture2D)Terraria.GameContent.TextureAssets.Projectile[Projectile.type];
            Texture2D textureGlow = TransparentTextureHandler.TransparentTextures[TransparentTextureHandler.TransparentTextureType.GreySlashGlowmask];
            int frameHeight = ((Texture2D)Terraria.GameContent.TextureAssets.Projectile[Projectile.type]).Height / Main.projFrames[Projectile.type];
            int startY = frameHeight * Projectile.frame;
            Rectangle sourceRectangle = new Rectangle(0, startY, texture.Width, frameHeight);
            Vector2 origin = sourceRectangle.Size() / 2f;
            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition + new Vector2(0f, Projectile.gfxOffY), sourceRectangle, lightColor, Projectile.rotation, origin, Projectile.scale, spriteEffects, 0f);

            if (Projectile.ai[0] >= 10)
            {
                alphalowered = Color.White * 0.9f;
                Main.EntitySpriteDraw(textureGlow, Projectile.Center - Main.screenPosition + new Vector2(0f, Projectile.gfxOffY), sourceRectangle, alphalowered, Projectile.rotation, origin, Projectile.scale, spriteEffects, 0f);
            }

            return false;
        }
    }
}


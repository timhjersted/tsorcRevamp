using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles
{
    public class ElfinTargeting : ModProjectile
    {

        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Elfin Targeting");
        }
        public override void SetDefaults()
        {
            Projectile.height = 5;
            Projectile.width = 5;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 15;
        }

        public override void AI()
        {
            Lighting.AddLight(Projectile.Center, Color.Cyan.ToVector3() * 1.5f);
            Projectile.damage = 0;
            Projectile.rotation -= 0.05f;
            if (Main.npc[(int)Projectile.ai[0]].active)
            {
                Projectile.Center = Main.npc[(int)Projectile.ai[0]].Center;
                NPC target = Main.npc[(int)Projectile.ai[0]];


                //Ignore the weird math operations on 'size' here, it's all just fine-tuning how the dust ring scales with the size of the NPC being targeted
                float size;
                if (target.height > target.width)
                {
                    size = target.height;
                }
                else
                {
                    size = target.width;
                }
                size += 15;
                size /= 7;
                for (int i = 0; i < Math.Pow(size, 0.7) * 4; i++)
                {
                    Vector2 dir = Main.rand.NextVector2CircularEdge(1, 1);
                    dir.Normalize();
                    dir *= Main.npc[(int)Projectile.ai[0]].width * 1.9f + Main.rand.NextFloat(-size, size);
                    Vector2 vel = dir * -1;
                    vel.Normalize();
                    vel *= (float)Math.Pow(size, 1.5) / 20;
                    Dust d = Dust.NewDustPerfect(dir + Main.npc[(int)Projectile.ai[0]].Center, 174, vel, 10, default, (int)(Math.Log10(size) * 2));
                    d.noGravity = true;
                    //Turns out you can shade dust?? The More You Know(TM)
                    d.shader = GameShaders.Armor.GetSecondaryShader((byte)GameShaders.Armor.GetShaderIdFromItemId(ItemID.CyanGradientDye), Main.LocalPlayer);
                }
            }
            else
            {
                Projectile.active = false;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            //Just couldn't get the ring to look good. Texture wasn't made to be expanded that large, and couldn't really edit it well enough to make it work lol
            /*
            SpriteEffects spriteEffects = SpriteEffects.None;
            if (projectile.spriteDirection == -1)
            {
                spriteEffects = SpriteEffects.FlipHorizontally;
            }
            //Get the premultiplied, properly transparent texture
            Texture2D texture = TransparentTextureHandler.TransparentTextures[TransparentTextureHandler.TransparentTextureType.ElfinTargeting];
            int frameHeight = texture.Height / 3;
            int startY = frameHeight * projectile.frame;
            Rectangle sourceRectangle = new Rectangle(0, startY, texture.Width, frameHeight);
            Vector2 origin = sourceRectangle.Size() / 2f;

            for (int i = 0; i < 3; i++)
            {
                //Main.EntitySpriteDraw(texture,
                //  projectile.Center - Main.screenPosition, sourceRectangle, Color.White * (1 / (i + 2)), projectile.rotation + 0.05f + (0.05f * i), origin, projectile.scale, spriteEffects, 0);
            }
            //Main.EntitySpriteDraw(texture,
            //    projectile.Center - Main.screenPosition, sourceRectangle, Color.White, projectile.rotation, origin, projectile.scale, spriteEffects, 0);
            */
            return false;
        }
    }
}

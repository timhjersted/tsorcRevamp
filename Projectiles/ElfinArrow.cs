using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles {
    public class ElfinArrow : ModProjectile {

        public override string Texture => "tsorcRevamp/Items/Ammo/ArrowOfBard";
        public override void SetStaticDefaults() {
            DisplayName.SetDefault("Elfin Arrow");
            ProjectileID.Sets.TrailCacheLength[projectile.type] = 10;
            ProjectileID.Sets.TrailingMode[projectile.type] = 0;
        }
        public override void SetDefaults()
        {
            projectile.height = 5;
            projectile.width = 5;
            projectile.friendly = true;
            projectile.ranged = true;
            projectile.tileCollide = false;
        }

        float topSpeed = 14;
        float homingStrength = 2;
        public override void AI()
        {
            projectile.rotation = projectile.velocity.ToRotation() - MathHelper.PiOver2;
            homingStrength += 0.005f;
            if (projectile.ai[0] >= 0)
            {
                Projectile target = Main.projectile[(int)projectile.ai[0]];
                if (target != null && target.active)
                {
                    Vector2 homingDirection = Vector2.Normalize(target.Center - projectile.Center);
                    projectile.velocity = (projectile.velocity * (30 / homingStrength) + homingDirection * 14) / ((30 / homingStrength) + 1);

                    if (projectile.velocity.Length() < topSpeed)
                    {
                        projectile.velocity *= topSpeed / projectile.velocity.Length();
                    }
                    if (projectile.velocity.Length() > topSpeed)
                    {
                        projectile.velocity *= topSpeed / projectile.velocity.Length();
                    }

                }
            }
            base.AI();
        }

        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor)
        {
            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            ArmorShaderData data = GameShaders.Armor.GetSecondaryShader((byte)GameShaders.Armor.GetShaderIdFromItemId(ItemID.MartianArmorDye), Main.LocalPlayer);
            data.Apply(null);

            SpriteEffects spriteEffects = SpriteEffects.None;
            if (projectile.spriteDirection == -1)
            {
                spriteEffects = SpriteEffects.FlipHorizontally;
            }
            //Get the premultiplied, properly transparent texture
            Texture2D texture = TransparentTextureHandler.TransparentTextures[TransparentTextureHandler.TransparentTextureType.ElfinArrow];
            int frameHeight = Main.projectileTexture[projectile.type].Height / Main.projFrames[projectile.type];
            int startY = frameHeight * projectile.frame;
            Rectangle sourceRectangle = new Rectangle(0, startY, texture.Width, frameHeight);
            Vector2 origin = sourceRectangle.Size() / 2f;
            Color drawColor = projectile.GetAlpha(lightColor);
            for(int i = 0; i < 9; i++)
            {
                Main.spriteBatch.Draw(texture,
                  projectile.oldPos[9 - i] - Main.screenPosition + new Vector2(0f, projectile.gfxOffY),
                  sourceRectangle, Color.White * (0.15f * i), projectile.rotation, origin, projectile.scale, spriteEffects, 0f);
            }
            Main.spriteBatch.Draw(texture,
                projectile.Center - Main.screenPosition + new Vector2(0f, projectile.gfxOffY),
                sourceRectangle, Color.White, projectile.rotation, origin, projectile.scale, spriteEffects, 0f);

            spriteBatch.End();
            spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, (Effect)null, Main.GameViewMatrix.TransformationMatrix);

            return false;
        }
    }
}

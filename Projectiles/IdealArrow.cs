using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles {
    public class IdealArrow : ModProjectile {

        public override string Texture => "tsorcRevamp/Items/Ammo/ArrowOfBard";
        public override void SetStaticDefaults() {
            DisplayName.SetDefault("Ideal Arrow");
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
            projectile.timeLeft = 360;
            //Missing: aiStyle = 1;
            //Vanilla aiStyles are kinda janky. We can do better with just a few lines below in AI()...
        }

        public override void AI()
        {
			projectile.rotation = projectile.velocity.ToRotation() - MathHelper.PiOver2; //This makes it rotate to face where it's moving
			projectile.velocity.Y += (9.8f / 60); //This is its gravity. Comes out to about 0.16 per frame, which is actually really high!!

            Dust thisdust = Dust.NewDustDirect(projectile.position, projectile.width, projectile.height, DustID.MagicMirror, 0, 0, 0, default, 1f); //This creates a dust trail
            thisdust.velocity = Vector2.Zero; //This makes the dust stay still instead of wandering randomly
        }

        //The explosion of dust upon hitting an enemy
        public override bool PreKill(int timeLeft)
        {
            for (int i = 0; i < 10; i++)
            {
                Vector2 vel = projectile.velocity + Main.rand.NextVector2Circular(5, 5);
                Dust d = Dust.NewDustPerfect(projectile.Center, DustID.MagicMirror, vel, 10, default, 2);
                d.noGravity = true;
                d.shader = GameShaders.Armor.GetSecondaryShader((byte)GameShaders.Armor.GetShaderIdFromItemId(ItemID.MartianArmorDye), Main.LocalPlayer);
            }
            return true;
        }

        //Disabled multiple effects due to lag. The game is really not a fan of rapidly beginning and ending dozens of shaded spriteBatches every frame haha...
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

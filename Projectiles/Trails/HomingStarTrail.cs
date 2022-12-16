using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Graphics;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Trails
{
    class HomingStarTrail : DynamicTrail
    {
        public override void SetStaticDefaults()
        {
            base.SetStaticDefaults();
            DisplayName.SetDefault("Illuminant Trail");
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 60;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 3;
        }
        public override void SetDefaults()
        {
            Projectile.damage = 0;
            Projectile.width = 1;
            Projectile.height = 1;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 99999999;
            Projectile.penetrate = -1;
            Projectile.hostile = true;
            Projectile.friendly = false;
            trailWidth = 35;
            trailLength = 900;
            trailCollision = true;
            collisionFrequency = 5;
            trailYOffset = 50;
            widthFunction = HomingStarWidthFunction;
            colorFunction = HomingStarColorFunction;
            trailDistanceCap = 500;
            
        }

        public override void AI()
        {
            base.AI();
            Projectile.rotation = Projectile.velocity.ToRotation();
        }

        Color HomingStarColorFunction(float progress)
        {
            float timeFactor = (float)Math.Sin(Math.Abs(progress - Main.GlobalTimeWrappedHourly * 1));
            Color result = Color.Lerp(Color.Cyan, Color.DeepPink, (timeFactor + 1f) / 2f);
            result.A = 0;

            return result;
        }

        float HomingStarWidthFunction(float progress)
        {

            if (progress >= 0.85)
            {
                float scale = (1f - progress) / 0.15f;
                return (float)Math.Pow(scale, 0.1) * (float)trailWidth;
            }
            else
            {
                return (float)Math.Pow(progress, 0.6f) * trailWidth;
            }
        }

        BasicEffect basicEffect;
        Texture2D texture;
        Texture2D starTexture;
        float starRotation;
        public override bool PreDraw(ref Color lightColor)
        {
            if (hostProjectile != null)
            {
                //Projectile core
                if (texture == null || texture.IsDisposed)
                {
                    texture = (Texture2D)ModContent.Request<Texture2D>("tsorcRevamp/Projectiles/Enemy/Triplets/IlluminantHomingStar", ReLogic.Content.AssetRequestMode.ImmediateLoad);
                }
                if (starTexture == null || starTexture.IsDisposed)
                {
                    starTexture = (Texture2D)ModContent.Request<Texture2D>("tsorcRevamp/Projectiles/Enemy/Triplets/HomingStarStar", ReLogic.Content.AssetRequestMode.ImmediateLoad);
                }


                Rectangle sourceRectangle = new Rectangle(0, 0, texture.Width, texture.Height);
                Rectangle starSourceRectangle = new Rectangle(0, 0, starTexture.Width, starTexture.Height);
                Vector2 origin = sourceRectangle.Size() / 2f;
                origin.Y += 20;
                Vector2 starOrigin = starSourceRectangle.Size() / 2f;
                DrawOriginOffsetY = 100;

                Vector2 offset = hostProjectile.position - hostProjectile.Center;
                //Draw shadow trails
                for (float i = 5; i >= 0; i--)
                {
                    Main.spriteBatch.Draw(texture, hostProjectile.oldPos[(int)i * 2] - Main.screenPosition - offset, sourceRectangle, Color.MediumPurple * ((6 - i) / 6), hostProjectile.oldRot[(int)i * 2] - MathHelper.PiOver2, origin, Projectile.scale, SpriteEffects.None, 0);
                }
                Main.spriteBatch.Draw(texture, Projectile.Center - Main.screenPosition, sourceRectangle, Color.White, hostProjectile.rotation - MathHelper.PiOver2, origin, Projectile.scale, SpriteEffects.None, 0);
                Vector2 starOffset = Projectile.velocity;
                starOffset.Normalize();
                starRotation += 0.1f;
            }

            //Trail
            if (trailPositions == null)
            {
                return false;
            }

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            //If no custom effect is specified, just use BasicEffect as a placeholder
            if (customEffect == null)
            {
                if (basicEffect == null)
                {
                    basicEffect = new BasicEffect(Main.graphics.GraphicsDevice);
                    basicEffect.VertexColorEnabled = true;
                    basicEffect.FogEnabled = false;
                    basicEffect.View = Main.GameViewMatrix.TransformationMatrix;
                    var viewport = Main.instance.GraphicsDevice.Viewport;
                    basicEffect.Projection = Matrix.CreateOrthographicOffCenter(0, viewport.Width, viewport.Height, 0, -1, 1);
                }
                basicEffect.World = Matrix.CreateTranslation(-new Vector3(Main.screenPosition.X, Main.screenPosition.Y, 0));

                //Main.graphics.GraphicsDevice.RasterizerState = RasterizerState.CullNone;

                basicEffect.CurrentTechnique.Passes[0].Apply();
            }

            if (widthFunction == null)
            {
                widthFunction = DefaultWidthFunction;
            }
            if (colorFunction == null)
            {
                colorFunction = DefaultColorFunction;
            }

            VertexStrip vertexStrip = new VertexStrip();
            vertexStrip.PrepareStrip(trailPositions.ToArray(), trailRotations.ToArray(), colorFunction, widthFunction, includeBacksides: true);
            vertexStrip.DrawTrail();


            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            return false;
        }
    }
}
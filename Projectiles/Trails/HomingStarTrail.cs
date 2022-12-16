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
            trailLength = 23;
            trailCollision = true;
            collisionFrequency = 5;
            trailYOffset = 50;
            widthFunction = HomingStarWidthFunction;
            colorFunction = HomingStarColorFunction;
        }


        public override void AI()
        {
            base.AI();
        }
        float DefaultWidthFunction(float progress)
        {
            float num = 1f;
            float lerpValue = Utils.GetLerpValue(0f, 0.6f, 1 - progress, clamped: true);
            num *= 1f - (1f - lerpValue) * (1f - lerpValue);
            return MathHelper.Lerp(0f, 30f, num);
        }

        Color DefaultColorFunction(float progress)
        {
            float timeFactor = (float)Math.Sin(Math.Abs((1 - progress) * 10 - Main.GlobalTimeWrappedHourly * 20));
            Color result = Color.Lerp(Color.Orange, Color.Red, (timeFactor + 1f) / 2f);
            result.A = 0;

            return result;
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
        public override bool PreDraw(ref Color lightColor)
        {
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
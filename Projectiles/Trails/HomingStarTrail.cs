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
            trailPointLimit = 900;
            trailCollision = true;
            collisionFrequency = 5;
            trailYOffset = 50;
            widthFunction = HomingStarWidthFunction;
            colorFunction = HomingStarColorFunction;
            trailMaxLength = 500;
            
        }

        float fadeOut = 1;
        public override void AI()
        {
            base.AI();
            Projectile.rotation = Projectile.velocity.ToRotation();

            if(hostProjectile == null)
            {
                fadeOut++;
            }
        }

        Color HomingStarColorFunction(float progress)
        {
            return Color.White;
        }

        float HomingStarWidthFunction(float progress)
        {
            return 50;
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

        

        Texture2D noiseTexture;
        public override bool PreDraw(ref Color lightColor)
        {           
            //Trail
            if (trailPositions == null)
            {
                return false;
            }

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
                        
            if (noiseTexture == null || noiseTexture.IsDisposed)
            {
                noiseTexture = (Texture2D)ModContent.Request<Texture2D>("tsorcRevamp/Projectiles/Enemy/Marilith/CataclysmicFirestorm", ReLogic.Content.AssetRequestMode.ImmediateLoad);
            }
            Effect trailEffect = ModContent.Request<Effect>("tsorcRevamp/Effects/ScreenFilters/HomingStarShader", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;

            trailEffect.Parameters["noiseTexture"].SetValue(noiseTexture);
            trailEffect.Parameters["fadeOut"].SetValue(fadeOut);
            trailEffect.Parameters["time"].SetValue(Main.GlobalTimeWrappedHourly);
            trailEffect.Parameters["shaderColor"].SetValue(new Color(1.0f, 0.4f, 0.8f, 1.0f).ToVector4());


            Matrix view = Matrix.CreateLookAt(Vector3.Zero, Vector3.UnitZ, Vector3.Up) * Matrix.CreateTranslation(Main.graphics.GraphicsDevice.Viewport.Width / 2, Main.graphics.GraphicsDevice.Viewport.Height / -2, 0) * Matrix.CreateRotationZ(MathHelper.Pi) * Matrix.CreateScale(Main.GameViewMatrix.Zoom.X, Main.GameViewMatrix.Zoom.Y, 1f);
            var projection = Matrix.CreateOrthographic(Main.graphics.GraphicsDevice.Viewport.Width, Main.graphics.GraphicsDevice.Viewport.Height, 0, 1000);
            trailEffect.Parameters["WorldViewProjection"].SetValue(view * projection);
            trailEffect.Techniques[0].Passes[0].Apply();

            if (widthFunction == null)
            {
                widthFunction = DefaultWidthFunction;
            }
            if (colorFunction == null)
            {
                colorFunction = DefaultColorFunction;
            }

            VertexStrip vertexStrip = new VertexStrip();
            vertexStrip.PrepareStrip(trailPositions.ToArray(), trailRotations.ToArray(), colorFunction, widthFunction, -Main.screenPosition, includeBacksides: true);
            vertexStrip.DrawTrail();


            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            return false;
        }
    }
}
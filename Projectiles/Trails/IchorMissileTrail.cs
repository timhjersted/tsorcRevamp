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
    class IchorMissileTrail : DynamicTrail
    {
        public override void SetStaticDefaults()
        {
            base.SetStaticDefaults();
            DisplayName.SetDefault("Ichor Trail");
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
            trailLength = 500;
            trailCollision = false;
            trailYOffset = 50;
            widthFunction = IchorTrailWidth;
            colorFunction = IchorTrailColor;
            trailDistanceCap = 400;            
        }

        public override void AI()
        {
            base.AI();
            Projectile.rotation = Projectile.velocity.ToRotation();
        }

        Color IchorTrailColor(float progress)
        {
            return Color.Gold;
        }

        float IchorTrailWidth(float progress)
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
        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
        {
            overPlayers.Add(index);
        }

        BasicEffect basicEffect;
        public override bool PreDraw(ref Color lightColor)
        {
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

                //If this is uncommented, the rainbow rod does not draw. All other projectiles draw fine.
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
            //return false;
            VertexStrip vertexStrip = new VertexStrip();
            vertexStrip.PrepareStrip(trailPositions.ToArray(), trailRotations.ToArray(), colorFunction, widthFunction, includeBacksides: true);
            vertexStrip.DrawTrail();

            Main.spriteBatch.End();

            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullCounterClockwise    , null, Main.GameViewMatrix.TransformationMatrix);
            return false;
        }
    }
}
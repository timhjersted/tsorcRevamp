using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Graphics;
using Terraria.ID;
using Terraria.ModLoader;
using static Humanizer.In;
using tsorcRevamp.Buffs.Runeterra;
using rail;

namespace tsorcRevamp.Projectiles.Trails
{
    class ScorchingPointTrail : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            //Always draw this projectile even if its "center" is far offscreen
            ProjectileID.Sets.DrawScreenCheckFluff[Projectile.type] = 99999999;
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
        }

        public override string Texture => "tsorcRevamp/Projectiles/Enemy/Triplets/HomingStarStar";

        public int trailLength = 60;
        public int trailWidth = 30;
        public bool trailCollision = false;
        public int collisionFrequency = 5;

        public Effect customEffect;
        public VertexStrip.StripColorFunction colorFunction;
        public VertexStrip.StripHalfWidthFunction widthFunction;

        public bool NPCSource
        {
            get => Projectile.ai[0] == 1;
        }
        public int hostIndex
        {
            get => (int)Projectile.ai[1];
        }

        public Entity hostEntity
        {
            get
            {
                if (NPCSource)
                {
                    return Main.npc[hostIndex];
                }
                else
                {
                    return Main.projectile[hostIndex];
                }
            }
        }

        public List<Vector2> trailPositions;
        public List<float> trailRotations;
        public override void AI()
        {
            Player owner = Main.player[Projectile.owner];

            if (owner.HasBuff(ModContent.BuffType<CenterOfTheHeat>()))
            {
                Projectile.timeLeft = 2;
            }

            if (NPCSource)
            {
                Projectile.Center = Main.npc[hostIndex].Center;
            }
            else
            {
                Projectile.Center = Main.projectile[hostIndex].Center;
            }

            if (trailPositions == null)
            {
                trailPositions = new List<Vector2>();
            }
            if (trailRotations == null)
            {
                trailRotations = new List<float>();
            }


            if (hostEntity.active)
            {
                trailPositions.Add(hostEntity.Center);
                trailRotations.Add(hostEntity.velocity.ToRotation());

                if (trailPositions.Count > trailLength)
                {
                    trailPositions.RemoveAt(0);
                    trailRotations.RemoveAt(0);
                }
            }
            else
            {
                if (trailPositions.Count > 3)
                {
                    trailPositions.RemoveAt(0);
                    trailRotations.RemoveAt(0);
                }
                else
                {
                    Projectile.Kill();
                }
            }
        }

        float DefaultWidthFunction(float progress)
        {
            return 30;
        }

        Color DefaultColorFunction(float progress)
        {
            return Color.Red;
            float timeFactor = (float)Math.Sin(Math.Abs(progress - Main.GlobalTimeWrappedHourly * 1));
            Color result = Color.Lerp(Color.Cyan, Color.DeepPink, (timeFactor + 1f) / 2f);
            result.A = 0;

            return result;
        }


        BasicEffect basicEffect;
        public override bool PreDraw(ref Color lightColor)
        {
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
            vertexStrip.PrepareStrip(Projectile.oldPos, Projectile.oldRot, colorFunction, widthFunction, includeBacksides: true);
            vertexStrip.DrawTrail();


            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            return false;
        }

        int ꙮ;

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                if (Main.player[i].active && !Main.player[i].dead)
                {
                    Player player = Main.player[i];
                    float discard = 0;

                    //Draw a line between points 9 at a time to check for collision
                    for (int j = 0; j < trailPositions.Count - collisionFrequency; j += collisionFrequency)
                    {
                        if (trailPositions.Count < j + collisionFrequency - 1)
                        {
                            break;
                        }
                        if (trailPositions[j + collisionFrequency - 1] == Vector2.Zero)
                        {
                            break;
                        }
                        if (Collision.CheckAABBvLineCollision(player.position, player.Size, trailPositions[j], trailPositions[j + collisionFrequency - 1], 2 * widthFunction(j / trailPositions.Count), ref discard))
                        {
                            return true;
                        }
                    }
                }
            }
            return base.Colliding(projHitbox, targetHitbox);
        }
    }
}

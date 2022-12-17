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
    class DynamicTrail : ModProjectile
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
            widthFunction = DefaultWidthFunction;
            colorFunction = DefaultColorFunction;
        }

        public override string Texture => "tsorcRevamp/Projectiles/Enemy/Triplets/HomingStarStar";

        public int trailLength = 60;
        public int trailWidth = 30;
        public float trailDistanceCap = 200;
        private float trailDistance;
        public bool trailCollision = false;
        public int collisionFrequency = 5;
        public float trailYOffset = 0;
        public int hostEntityType = -1;
        public NPC hostNPC;
        public Projectile hostProjectile;

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
                    return hostNPC;
                }
                else
                {
                    return hostProjectile;
                }
            }
        }

        public List<Vector2> trailPositions;
        public List<float> trailRotations;
        private bool initialized = false;
        public override void AI()
        {
            Initialize();

            if (HostEntityValid())
            {
                Vector2 offset = hostEntity.velocity;
                offset.Normalize();
                offset *= trailYOffset;

                Projectile.Center = hostEntity.Center;
                trailPositions.Add(hostEntity.Center + offset);
                trailRotations.Add(hostEntity.velocity.ToRotation());

                while(trailPositions.Count > trailLength || CalculateLength() > trailDistanceCap)
                {
                    trailPositions.RemoveAt(0);
                    trailRotations.RemoveAt(0);
                }
            }
            else
            {
                hostNPC = null;
                hostProjectile = null;
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

        public float CalculateLength()
        {
            float calculatedLength = 0;
            for (int i = 0; i < trailPositions.Count - 2; i++)
            {
                calculatedLength += Vector2.Distance(trailPositions[i], trailPositions[i + 1]);
            }

            return calculatedLength;
        }

        public bool HostEntityValid()
        {
            if (hostEntity == null)
            {
                return false;
            }
            if (!hostEntity.active)
            {
                return false;
            }
            if(NPCSource && hostNPC.type != hostEntityType)
            {
                return false;
            }
            if (!NPCSource && hostProjectile.type != hostEntityType)
            {
                return false;
            }

            return true;
        }

        public void Initialize()
        {
            if (!initialized)
            {
                trailPositions = new List<Vector2>();
                trailRotations = new List<float>();

                if (hostNPC == null && NPCSource)
                {
                    hostNPC = Main.npc[hostIndex];
                    hostEntityType = hostNPC.type;
                }
                if (hostProjectile == null && !NPCSource)
                {
                    hostProjectile = Main.projectile[hostIndex];
                    hostEntityType = hostProjectile.type;
                }
                initialized = true;
            }
        }

        public float DefaultWidthFunction(float progress)
        {
            return trailWidth;
        }

        public Color DefaultColorFunction(float progress)
        {
            return Color.White;
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
            if(trailPositions == null)
            {
                return false;
            }

            for (int i = 0; i < Main.maxPlayers; i++)
            {
                if (Main.player[i].active && !Main.player[i].dead)
                {
                    Player player = Main.player[i];
                    float discard = 0;

                    //Draw a line between points 9 at a time to check for collision
                    for (int j = 0; j < trailPositions.Count - collisionFrequency; j += collisionFrequency)
                    {
                        if(trailPositions.Count < j + collisionFrequency - 1)
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

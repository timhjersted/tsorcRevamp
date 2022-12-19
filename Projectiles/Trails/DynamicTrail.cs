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
        }

        public override string Texture => "tsorcRevamp/Projectiles/Enemy/Triplets/HomingStarStar";

        /// <summary>
        /// The max number of points in the trail
        /// </summary>
        public int trailPointLimit = 60;
        /// <summary>
        /// The width of the trail
        /// </summary>
        public int trailWidth = 30;
        /// <summary>
        /// The maximum length of the trail
        /// </summary>
        public float trailMaxLength = 200;
        /// <summary>
        /// The current length of the trail
        /// </summary>
        public float trailCurrentLength;
        /// <summary>
        /// Variable used to make the trail fade out once its host is inactive
        /// </summary>
        public float fadeOut = 1;
        /// <summary>
        /// Can the trail deal damage?
        /// </summary>
        public bool trailCollision = false;
        /// <summary>
        /// Controls how fine-tuned the collision checking is
        /// There is rarely a need to mess with this
        /// </summary>
        public int collisionFrequency = 5;
        /// <summary>
        /// Shifts the trail
        /// </summary>
        public float trailYOffset = 0;
        /// <summary>
        /// Stores the type field of the host entity, to prevent it from attaching to another
        /// </summary>
        public int hostEntityType = -1;
        /// <summary>
        /// Allows you to make collision checking stop before the end of the trail
        /// Used on trails where the last few pieces of them are not visible
        /// </summary>
        public int collisionPadding = 2;
        /// <summary>
        /// If this projectile is attached to an NPC it is stored here
        /// </summary>
        public NPC hostNPC;
        /// <summary>
        /// If this projectile is attached to a Projectile it is stored here
        /// </summary>
        public Projectile hostProjectile;
        /// <summary>
        /// The effect this trail uses.
        /// Set its parameters by overriding SetEffectParameters
        /// </summary>
        public Effect customEffect;

        /// <summary>
        /// If Projectile.ai[0] is set to 1, then this projectile is attached to an NPC
        /// Otherwise, it is attached to another Projectile
        /// </summary>
        public bool NPCSource
        {
            get => Projectile.ai[0] == 1;
        }
        /// <summary>
        /// The index of the host in the NPC or Projectile array
        /// </summary>
        public int hostIndex
        {
            get => (int)Projectile.ai[1];
        }

        /// <summary>
        /// A reference to the host entity
        /// </summary>
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

        /// <summary>
        /// The list storing all the points on the trail
        /// </summary>
        public List<Vector2> trailPositions;
        /// <summary>
        /// The list storing all the rotations of each trail point
        /// </summary>
        public List<float> trailRotations;
        /// <summary>
        /// Whether the trail has completed its initialization tasks or not
        /// </summary>
        private bool initialized = false;
        public float lengthPercent
        {
            get
            {
                return trailCurrentLength / trailMaxLength;
            }
        }
        public override void AI()
        {
            Initialize();

            Projectile.rotation = Projectile.velocity.ToRotation();

            if (HostEntityValid())
            {
                Vector2 offset = hostEntity.velocity;
                offset.Normalize();
                offset *= trailYOffset;

                Projectile.Center = hostEntity.Center;
                trailPositions.Add(hostEntity.Center + offset);
                trailRotations.Add(hostEntity.velocity.ToRotation());
                trailCurrentLength = CalculateLength();

                while (trailPositions.Count > trailPointLimit || trailCurrentLength > trailMaxLength)
                {
                    trailPositions.RemoveAt(0);
                    trailRotations.RemoveAt(0);
                    trailCurrentLength = CalculateLength();
                }
            }
            else
            {
                fadeOut++;
                hostNPC = null;
                hostProjectile = null;
                if (trailPositions.Count > 3)
                {
                    trailPositions.RemoveAt(0);
                    trailRotations.RemoveAt(0);
                    trailCurrentLength = CalculateLength();
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

        public virtual float WidthFunction(float progress)
        {
            return trailWidth;
        }
        public virtual float CollisionWidthFunction(float progress)
        {
            return WidthFunction(progress);
        }

        public virtual Color ColorFunction(float progress)
        {
            return Color.White;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (trailPositions == null)
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
                    for (int j = collisionPadding; j < trailPositions.Count - collisionFrequency - 1; j += collisionFrequency)
                    {
                        if (trailPositions[j + collisionFrequency - 1] == Vector2.Zero)
                        {
                            break;
                        }
                        if (Collision.CheckAABBvLineCollision(player.position, player.Size, trailPositions[j], trailPositions[j + collisionFrequency - 1], 2 * CollisionWidthFunction(j / trailPositions.Count), ref discard))
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        int ꙮ;

        public static Matrix GetWorldViewProjectionMatrix()
        {
            Matrix view = Matrix.CreateLookAt(Vector3.Zero, Vector3.UnitZ, Vector3.Up) * Matrix.CreateTranslation(Main.graphics.GraphicsDevice.Viewport.Width / 2, Main.graphics.GraphicsDevice.Viewport.Height / -2, 0) * Matrix.CreateRotationZ(MathHelper.Pi) * Matrix.CreateScale(Main.GameViewMatrix.Zoom.X, Main.GameViewMatrix.Zoom.Y, 1f);
            Matrix projection = Matrix.CreateOrthographic(Main.graphics.GraphicsDevice.Viewport.Width, Main.graphics.GraphicsDevice.Viewport.Height, 0, 1000);

            return view * projection;
        }

        public virtual void SetEffectParameters(Effect effect) { }

        BasicEffect basicEffect;
        public override bool PreDraw(ref Color lightColor)
        {
            if(trailPositions == null)
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
                basicEffect.CurrentTechnique.Passes[0].Apply();
            }
            else
            {
                SetEffectParameters(customEffect);
                customEffect.CurrentTechnique.Passes[0].Apply();
            }

            VertexStrip vertexStrip = new VertexStrip();
            vertexStrip.PrepareStrip(trailPositions.ToArray(), trailRotations.ToArray(), ColorFunction, WidthFunction, -Main.screenPosition, includeBacksides: true);
            vertexStrip.DrawTrail();

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            return false;
        }



        //Shifting blue and pink
        //Could be useful later
        //float timeFactor = (float)Math.Sin(Math.Abs(progress - Main.GlobalTimeWrappedHourly * 1));
        //Color result = Color.Lerp(Color.Cyan, Color.DeepPink, (timeFactor + 1f) / 2f);
        //result.A = 0;

        //return result;
    }
}

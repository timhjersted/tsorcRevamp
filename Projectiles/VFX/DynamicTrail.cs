using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.Graphics;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.VFX
{
    public class DynamicTrail : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            //Always draw this projectile even if its "center" is far offscreen
            ProjectileID.Sets.DrawScreenCheckFluff[Projectile.type] = 99999999;
        }

        public override void SetDefaults()
        {
            Projectile.tileCollide = false;
            Projectile.damage = 0;
            Projectile.width = 1;
            Projectile.height = 1;
            Projectile.friendly = true;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 99999999;
            Projectile.penetrate = -1;
        }

        public override string Texture => "tsorcRevamp/Projectiles/Enemy/Triad/HomingStarStar";

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
        /// Allows you to make collision checking stop before the start of the trail
        /// Used on trails where the first few pieces of them are not visible
        /// </summary>
        public int collisionPadding = 2;

        /// <summary>
        /// Allows you to make collision checking stop before the end of the trail
        /// Used on trails where the last few pieces of them are not visible
        /// </summary>
        public int collisionEndPadding = 2;

        /// <summary>
        /// If this projectile is attached to an NPC it is stored here
        /// </summary>
        public bool dying;

        /// <summary>
        /// If this projectile is attached to an NPC it is stored here
        /// </summary>
        public float deathProgress;

        public float deathSpeed = 1f / 60f;

        /// <summary>
        /// If this projectile is attached to an NPC it is stored here
        /// </summary>
        public NPC hostNPC;       

        /// <summary>
        /// The effect this trail uses.
        /// Set its parameters by overriding SetEffectParameters
        /// </summary>
        public Effect customEffect;

        /// <summary>
        /// Turn this on for debugging, to see the trail hitbox with high precision
        /// </summary>
        public bool visualizeTrail = false;

        /// <summary>
        /// Trails fade out when their timeLeft gets low by default. Turn this on to disable that behavior.
        /// </summary>
        public bool noFadeOut = false;

        /// <summary>
        /// If Projectile.ai[0] is set to 1, then this projectile is attached to an NPC
        /// Otherwise, it is attached to another Projectile
        /// </summary>
        public bool NPCSource;

        /// <summary>
        /// The whoAmI of the host NPC (if it has one)
        /// </summary>
        public float HostNPCIdentifier
        {
            get => Projectile.ai[1];
        }

        /// <summary>
        /// A reference to the host entity
        /// </summary>
        public Entity HostEntity
        {
            get
            {
                if (NPCSource)
                {
                    return hostNPC;
                }
                else
                {
                    return Projectile;
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
        bool initialized = false;        

        public float lengthPercent
        {
            get
            {
                return trailCurrentLength / trailMaxLength;
            }
        }

        Vector2 lastPosition = Vector2.Zero;
        public override void AI()
        {            
            if (!initialized)
            {
                 Initialize();
            }

            if ((!HostEntityValid() || Projectile.timeLeft < 1f / deathSpeed || dying) && !noFadeOut)
            {
                dying = true;
                hostNPC = null;

                deathProgress += deathSpeed;
                if(deathProgress > 1)
                {
                    deathProgress = 1;
                    Projectile.Kill();
                }
                fadeOut = 1f - deathProgress;
            }
            else
            {
                Projectile.Center = HostEntity.Center;

                //Don't add new trail segments if it has not travelled far enough
                if (Vector2.Distance(lastPosition, HostEntity.Center) > 1f)
                {
                    lastPosition = HostEntity.Center;
                    trailPositions.Add(HostEntity.Center);
                    trailRotations.Add(HostEntity.velocity.ToRotation());

                }

                if (trailPositions.Count > 2)
                {
                    trailPositions[trailPositions.Count - 1] = HostEntity.Center;
                    trailRotations[trailRotations.Count - 1] = HostEntity.velocity.ToRotation();

                    trailCurrentLength = CalculateLength();

                    if (trailCurrentLength > trailMaxLength)
                    {
                        float shorteningDistance = trailCurrentLength - trailMaxLength;

                        while (shorteningDistance > Vector2.Distance(trailPositions[0], trailPositions[1]))
                        {
                            trailPositions.RemoveAt(0);
                            trailRotations.RemoveAt(0);
                            trailCurrentLength = CalculateLength();
                            shorteningDistance = trailCurrentLength - trailMaxLength;
                        }
                        if (shorteningDistance < Vector2.Distance(trailPositions[0], trailPositions[1]))
                        {
                            Vector2 diff = trailPositions[1] - trailPositions[0];
                            float currentDistance = diff.Length();
                            float newDistance = currentDistance - shorteningDistance;
                            trailPositions[0] = trailPositions[1] - Vector2.Normalize(diff) * newDistance;
                            if (Vector2.Distance(trailPositions[0], trailPositions[1]) < 0.1f)
                            {
                                trailPositions.RemoveAt(0);
                                trailRotations.RemoveAt(0);
                                trailCurrentLength = CalculateLength();
                            }
                        }
                    }
                }

                if (trailPositions.Count > 2)
                {
                    trailRotations[trailRotations.Count - 1] = (trailPositions[trailRotations.Count - 2] - trailPositions[trailRotations.Count - 1]).ToRotation();
                }

                //Smoothing
                if (trailPositions.Count > 3)
                {
                    for (int i = 3; i < trailPositions.Count - 1; i++)
                    {
                        trailPositions[i - 2] = (trailPositions[i - 3] + trailPositions[i - 1]) / 2f;
                        trailRotations[i - 2] = (trailPositions[i - 3] - trailPositions[i - 2]).ToRotation();
                    }
                }


                //This could be optimized to not require recomputing the length after each removal
                while (trailPositions.Count > trailPointLimit)
                {
                    trailPositions.RemoveAt(0);
                    trailRotations.RemoveAt(0);
                    trailCurrentLength = CalculateLength();
                }
            }
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(dying);
        }
        public override void ReceiveExtraAI(BinaryReader reader)
        {
            dying = reader.ReadBoolean();
        }

        public int getNextPointAlongLine(int trailIndex, Vector2 position)
        {
            float distance = Vector2.DistanceSquared(trailPositions[trailIndex], position);
            while (trailIndex < trailPositions.Count)
            {
                trailIndex++;
                float nextDistance = Vector2.DistanceSquared(trailPositions[trailIndex], position);
                if (nextDistance < distance)
                {
                    distance = nextDistance;
                }
                else
                {
                    return trailIndex;
                }
            }

            //Failsafe for if trailIndex is already at the end of the trail
            return trailIndex;
        }
                

        public float CalculateLength()
        {
            float calculatedLength = 0;
            for (int i = 0; i < trailPositions.Count - 1; i++)
            {
                float extraDistance = Vector2.Distance(trailPositions[i], trailPositions[i + 1]);

                //If the trail is discontinuous (because its host got teleported, for example) then restart it
                if(extraDistance > 60)
                {
                    trailPositions = new List<Vector2>();
                    trailRotations = new List<float>();
                    trailPositions.Add(HostEntity.Center);
                    trailPositions.Add(HostEntity.Center);
                    trailRotations.Add(HostEntity.velocity.ToRotation());
                    trailRotations.Add(HostEntity.velocity.ToRotation());
                    return 0;
                }
                calculatedLength += extraDistance;
            }
            return calculatedLength;
        }

        public bool HostEntityValid()
        {
            if (HostEntity == null)
            {
                return false;
            }
            if (!HostEntity.active)
            {
                return false;
            }
            if(NPCSource && hostNPC.type != hostEntityType)
            {
                return false;
            }

            return true;
        }

        public void Initialize()
        {            
            if(lastPosition == Vector2.Zero)
            {
                lastPosition = Projectile.Center;
            }
            trailPositions = new List<Vector2>();
            trailRotations = new List<float>();

            if (hostNPC == null && NPCSource)
            {
                hostNPC = Main.npc[(int)HostNPCIdentifier];
                hostEntityType = hostNPC.type;
            }
            initialized = true;            
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

            float discard = 0;

            //Draw a line between points to check for collision
            for (int i = collisionEndPadding; i < trailPositions.Count - collisionFrequency - 1 - collisionPadding; i += collisionFrequency)
            {
                if (trailPositions[i + collisionFrequency - 1] == Vector2.Zero)
                {
                    break;
                }
                if (Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), trailPositions[i], trailPositions[i + collisionFrequency - 1], 2 * CollisionWidthFunction((float)i / (float)trailPositions.Count), ref discard))
                {
                    return true;
                }
            }

            return false;
        }

        public static Matrix GetWorldViewProjectionMatrix()
        {
            Matrix view = Matrix.CreateLookAt(Vector3.Zero, Vector3.UnitZ, Vector3.Up) * Matrix.CreateTranslation(Main.graphics.GraphicsDevice.Viewport.Width / 2, Main.graphics.GraphicsDevice.Viewport.Height / -2, 0) * Matrix.CreateRotationZ(MathHelper.Pi) * Matrix.CreateScale(Main.GameViewMatrix.Zoom.X, Main.GameViewMatrix.Zoom.Y, 1f);
            Matrix projection = Matrix.CreateOrthographic(Main.graphics.GraphicsDevice.Viewport.Width, Main.graphics.GraphicsDevice.Viewport.Height, 0, 1000);

            return view * projection;
        }

        public virtual void SetEffectParameters(Effect effect) { }

        BasicEffect basicEffect;
        Texture2D starTexture;
        public bool additiveContext = false;
        public override bool PreDraw(ref Color lightColor)
        {
            if(trailPositions == null)
            {
                return false;
            }

            if (!additiveContext)
            {
                return false;
            }

            if (Main.spriteBatch.Name != null)
            {
                Main.spriteBatch.End();
            }


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


            
            if (visualizeTrail)
            {
                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

                if (starTexture == null || starTexture.IsDisposed)
                {
                    starTexture = (Texture2D)ModContent.Request<Texture2D>("tsorcRevamp/Projectiles/Enemy/Triad/HomingStarStar", ReLogic.Content.AssetRequestMode.ImmediateLoad);
                }
                Rectangle starSourceRectangle = new Rectangle(0, 0, starTexture.Width, starTexture.Height);
                Vector2 starOrigin = starSourceRectangle.Size() / 2f;

                for (int i = 0; i < trailPositions.Count - 0; i++)
                {
                    float scaleFactor = 0.75f;
                    if(i < collisionEndPadding || i > trailPositions.Count - collisionPadding)
                    {
                        scaleFactor /= 2f;
                    }
                    Main.spriteBatch.Draw(starTexture, trailPositions[i] - Main.screenPosition + new Vector2(CollisionWidthFunction((float)i / (float)trailPositions.Count), 0).RotatedBy(trailRotations[i] + MathHelper.PiOver2), starSourceRectangle, Color.White, trailRotations[i], starOrigin, Projectile.scale * scaleFactor, SpriteEffects.None, 0);
                    Main.spriteBatch.Draw(starTexture, trailPositions[i] - Main.screenPosition, starSourceRectangle, Color.White, trailRotations[i], starOrigin, Projectile.scale * 0.75f, SpriteEffects.None, 0);
                    Main.spriteBatch.Draw(starTexture, trailPositions[i] - Main.screenPosition - new Vector2(CollisionWidthFunction((float)i / (float)trailPositions.Count), 0).RotatedBy(trailRotations[i] + MathHelper.PiOver2), starSourceRectangle, Color.White, trailRotations[i], starOrigin, Projectile.scale * scaleFactor, SpriteEffects.None, 0);
                }
                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            }

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

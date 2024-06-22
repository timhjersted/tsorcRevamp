using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.Enums;
using Terraria.GameContent.Shaders;
using Terraria.Graphics.Effects;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles
{

    ///<summary>
    ///Lets you easily create lasers of any size and color, and give them a variety of behaviors
    ///</summary>
    public class GenericLaser : ModProjectile
    {
        /// <summary>
        ///Set to true if the laser originates from a projectile instead of an NPC
        /// </summary>
        public bool ProjectileSource = false;

        /// <summary>
        ///The name the laser will display if it kills the player
        /// </summary>
        public string LaserName = "DefaultLaserName";

        ///The source position of the laser. If FOLLOW_SOURCE is set to true, this will be ignored.
        public Vector2 LaserOrigin = new Vector2(0, 0);

        /// <summary>
        /// Should it stick to the center of the NPC or Projectile that spawned it?
        /// If not it will stay where it was spawned
        /// </summary>
        public bool FollowHost = false;

        /// <summary>
        ///Set to 0 for normal behavior
        ///Set to 1 to only display the transparent 'targeting' beam
        /// Set to 2 to draw in full, but still do no damage
        /// </summary>
        public int TargetingMode = 0;

        /// <summary>
        /// Should it pierce through all NPCs? If not it will stop at the first one it hits
        /// </summary>
        public bool PierceNPCs = true;

        /// <summary>
        /// Should the laser be offset from the center of its source? If so, how much?
        /// </summary>
        public Vector2 LaserOffset = new Vector2(0, 0);

        /// <summary>
        /// What color is the laser? Leave this blank if it has a custom texture
        /// </summary>
        public Color LaserColor = Color.White;

        /// <summary>
        /// How long should it telegraph its path with a targeting laser? This can be set to 0 for instant hits
        /// </summary>
        public int TelegraphTime = 60;

        /// <summary>
        /// How fast should it fade in when firing?
        /// </summary>
        public float FadeInSpeed = 0.10f;

        /// <summary>
        /// How many frames before despawning should it start fading out
        /// </summary>
        public int FadeOutFrames = 10;

        /// <summary>
        /// What dust should it spawn?
        /// </summary>
        public int LaserDust = 0;

        /// <summary>
        /// Should it create a line of dust along its length?
        /// </summary>
        public bool LineDust = false;

        /// <summary>
        /// How much dust?
        /// </summary>
        public int DustAmount = 100;

        /// <summary>
        /// Scales the size of the laser
        /// </summary>
        public float LaserSize = 0.4f;

        /// <summary>
        /// Should it stop when it hits tiles?
        /// </summary>
        public bool TileCollide = true;

        /// <summary>
        /// How long should the laser be? Max is 20000 units
        /// </summary>
        public int LaserLength = 5000;

        /// <summary>
        /// Lighting is computationally expensive. Set this to false to improve performance dramatically when many lasers are on the screen.
        /// </summary>
        public bool CastLight = true;

        /// <summary>
        /// Should it have a light color different than 'LaserColor'?
        /// </summary>
        public Color? LightColor = null;

        /// <summary>
        /// Should this laser be drawn with additive blending instead of normal?
        /// Currently does nothing, will be re-enabled in the future
        /// </summary>
        public bool Additive = true;

        /// <summary>
        /// What transparency should it be drawn with? 0 > 1
        /// </summary>
        public float LaserAlpha = 1;

        /// <summary>
        /// Should it be drawn with a custom shader? If not set it will default to the GenericLaser shader
        /// </summary>
        public Effect LaserShader;

        /// <summary>
        /// Should it play a sound? Set to 'null' to disable
        /// </summary>
        public SoundStyle? LaserSound = SoundID.Item12 with { Volume = 0.5f };

        /// <summary>
        /// Does it have a custom texture?
        /// </summary>
        public TransparentTextureHandler.TransparentTextureType LaserTexture = TransparentTextureHandler.TransparentTextureType.GenericLaser;

        /// <summary>
        /// What about for its targeting beam?
        /// </summary>
        public TransparentTextureHandler.TransparentTextureType LaserTargetingTexture = TransparentTextureHandler.TransparentTextureType.GenericLaserTargeting;

        public Rectangle LaserTextureBody = new Rectangle(0, 0, 46, 28);
        public Rectangle LaserTextureTail = new Rectangle(0, 30, 46, 28);
        public Rectangle LaserTextureHead = new Rectangle(0, 60, 46, 28);

        public Rectangle LaserTargetingBody = new Rectangle(0, 0, 46, 28);
        public Rectangle LaserTargetingTail = new Rectangle(0, 30, 46, 28);
        public Rectangle LaserTargetingHead = new Rectangle(0, 60, 46, 28);

        /// <summary>
        /// If it's animated, how many frames does it have?
        /// </summary>
        public int frameCount = 1;

        /// <summary>
        /// How many ticks per frame?
        /// </summary>
        public int frameDuration = 0;

        public int currentFrame = 0;
        public int frameCounter = 0;

        /// <summary>
        /// What debuffs should it inflict?
        /// </summary>
        public List<int> LaserDebuffs = new List<int>();
        /// <summary>
        /// How long should those debuffs last?
        /// </summary>
        public List<int> DebuffTimers = new List<int>();

        /// <summary>
        /// Has it already been initialized on the client it's running on? If so, re-setting all its basic values is unnecessary.
        /// </summary>
        public bool initialized = false;

        /// <summary>
        /// How long (in frames) does it have to charge before firing?
        ///If this is 0, the charge mechanic will simply be disabled
        /// </summary>
        public float MaxCharge = 120;

        /// <summary>
        /// How long (in frames) should the laser fire once it is charged? Defaults to 2 seconds
        /// </summary>
        public int FiringDuration = 120;

        /// <summary>
        /// Flag used when drawing it to inform the laser it's being drawn in a context where the spritebatch is in Additive mode. Not really intended to be messed with lol
        /// </summary>
        public bool AdditiveContext = false;

        /// <summary>
        /// How long should each "segment" of the laser be? This value should pretty much be fine
        /// </summary>
        private const float MOVE_DISTANCE = 20f;
        public float internalDistance;

        /// <summary>
        /// The current length of the laser
        /// </summary>
        public float Distance
        {
            get => internalDistance;
            set => internalDistance = value;
        }

        /// <summary>
        /// Contains the ID of the player, projectile, or NPC that this laser is attached to (if any).
        /// Note: Projectile.ai[0] is unused, feel free to use it for custom behavior
        /// </summary>

        public int HostIdentifier
        {
            get => (int)Projectile.ai[1];
            set => Projectile.ai[1] = value;
        }

        /// <summary>
        /// This variable counts up by one each frame until it reaches MaxCharge, and then fires
        /// </summary>
        public float Charge
        {
            get => Projectile.localAI[0];
            set => Projectile.localAI[0] = value;
        }

        /// <summary>
        /// Is the laser fully charged?
        /// </summary>
        public bool IsAtMaxCharge => (Charge == MaxCharge || MaxCharge == 0 || MaxCharge == -1);

        /// <summary>
        /// How much remaining time will it fire for?
        /// </summary>
        public int FiringTimeLeft = 0;

        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("DefaultLaserName");
            ProjectileID.Sets.DrawScreenCheckFluff[Projectile.type] = 99999999;

        }
        public override void SetDefaults()
        {
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.friendly = false;
            Projectile.hostile = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.damage = 25;

            if (HostIdentifier != -1)
            {
                LaserOrigin = ProjectileSource ? Main.projectile[UsefulFunctions.DecodeID(HostIdentifier)].position : Main.npc[HostIdentifier].position;
            }
        }

        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
        {
            // Add this projectile to the list of projectiles that will be drawn BEFORE tiles and NPC are drawn. This makes the projectile appear to be BEHIND the tiles and NPC.
            behindNPCs.Add(index);
        }

        public float timeFactor = 0;
        public float fadePercent;
        public bool additiveContext = false;
        public override bool PreDraw(ref Color lightColor)
        {
            if (!additiveContext)
            {
                return false;
            }


            //If no custom shader has been given then load the generic one
            if (LaserShader == null)
            {
                LaserShader = ModContent.Request<Effect>("tsorcRevamp/Effects/GenericLaser", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
            }

            //Gives the laser its 'flowing' effect
            timeFactor++;
            LaserShader.Parameters["Time"].SetValue(timeFactor);

            //Shifts its color slightly over time
            Vector3 hslColor = Main.rgbToHsl(LaserColor);
            hslColor.X += 0.03f * (float)Math.Cos(timeFactor / 50f);
            Color rgbColor = Main.hslToRgb(hslColor);
            LaserShader.Parameters["Color"].SetValue(rgbColor.ToVector3());

            float modifiedSize = LaserSize * 200;

            //Fade in and out, and pulse while targeting
            if ((IsAtMaxCharge && TargetingMode == 0) || (TargetingMode == 2))
            {
                if (FiringTimeLeft < FadeOutFrames)
                {
                    fadePercent = (float)FiringTimeLeft / (float)FadeOutFrames;
                }
                else
                {
                    fadePercent += FadeInSpeed;
                    if (fadePercent > 1)
                    {
                        fadePercent = 1;
                    }
                }
            }
            else if (TelegraphTime + Charge >= MaxCharge || TargetingMode == 1)
            {
                modifiedSize /= 2;
                fadePercent = (float)Math.Cos(timeFactor / 30f);
                fadePercent = Math.Abs(fadePercent) * 0.2f;
                fadePercent += 0.2f;
            }
            else
            {
                fadePercent = 0;
            }

            //Apply the rest of the parameters it needs
            LaserShader.Parameters["FadeOut"].SetValue(fadePercent);
            LaserShader.Parameters["SecondaryColor"].SetValue(Color.White.ToVector3());
            LaserShader.Parameters["ProjectileSize"].SetValue(new Vector2(Distance, modifiedSize));
            LaserShader.Parameters["TextureSize"].SetValue(tsorcRevamp.NoiseTurbulent.Width);

            //Calculate where to draw it
            Rectangle sourceRectangle = new Rectangle(0, 0, (int)Distance, (int)(modifiedSize));
            Vector2 origin = new Vector2(0, sourceRectangle.Height / 2f);

            //Apply the shader
            LaserShader.CurrentTechnique.Passes[0].Apply();

            //Draw the laser
            Main.EntitySpriteDraw(tsorcRevamp.NoiseTurbulent, Projectile.Center - Main.screenPosition, sourceRectangle, Color.White, Projectile.velocity.ToRotation(), origin, Projectile.scale, SpriteEffects.None, 0);

            return false;
        }

        public void DrawLaser(Texture2D texture, Vector2 start, Vector2 unit, Rectangle headRect, Rectangle bodyRect, Rectangle tailRect, float rotation = 0f, float scale = 1f, Color color = default)
        {
            //Defines an area where laser segments should actually draw, 100 pixels larger on each side than the screen
            Rectangle screenRect = new Rectangle((int)Main.screenPosition.X - 100, (int)Main.screenPosition.Y - 100, Main.screenWidth + 100, Main.screenHeight + 100);

            float r = unit.ToRotation() + rotation;
            Rectangle bodyFrame = bodyRect;
            bodyFrame.X = bodyRect.Width * currentFrame;
            Rectangle headFrame = headRect;
            headFrame.X *= headRect.Width * currentFrame;
            Rectangle tailFrame = tailRect;
            tailFrame.X *= tailRect.Width * currentFrame;

            frameCounter++;
            if (frameCounter >= frameDuration)
            {
                currentFrame++;
                frameCounter = 0;
                if (currentFrame > (frameCount - 1))
                {
                    currentFrame = 0;
                }
            }

            //Laser head
            Vector2 startPos = start;
            if (FastContainsPoint(screenRect, startPos))
            {
                Main.EntitySpriteDraw(texture, startPos - Main.screenPosition, headFrame, color, r, new Vector2(headRect.Width * .5f, headRect.Height * .5f), scale, 0, 0);
            }
            startPos += (unit * (headRect.Height) * scale);

            //Stopwatch drawWatch = Stopwatch.StartNew();

            int count = 0;
            /*
            Tuple <Vector2, Vector2> intersections = Intersections(screenRect, startPos, unit);

            int count = 0;
            if (intersections.Item1 != Vector2.Zero && intersections.Item2 != Vector2.Zero)
            {
                Vector2 change = intersections.Item2 - intersections.Item1;

                Vector2 step = change / (bodyFrame.Height * scale);
                int visibleSegments = (int)(change / step).X;

                for(float i = 0; i < visibleSegments; i++)
                {
                   Vector2 drawStart = intersections.Item1 * i;
                   Main.EntitySpriteDraw(texture, drawStart - Main.screenPosition, bodyFrame, color, r, new Vector2(bodyRect.Width * .5f, bodyRect.Height * .5f), scale, 0, 0);
                }
            }*/


            //Laser body
            float i = 0;
            for (; i <= Distance - ((bodyFrame.Height) * scale); i += (bodyFrame.Height) * scale)
            {
                Vector2 drawStart = startPos + i * unit;
                if (FastContainsPoint(screenRect, drawStart))
                {
                    Main.EntitySpriteDraw(texture, drawStart - Main.screenPosition, bodyFrame, color, r, new Vector2(bodyRect.Width * .5f, bodyRect.Height * .5f), scale, 0, 0);
                    count++;
                }
            }

            //drawWatch.Stop();
            //Main.NewText(count + " segments drawn in " + drawWatch.Elapsed);


            //Laser tail
            i -= (LaserTextureBody.Height) * scale;
            i += (LaserTextureTail.Height + 3) * scale; //Slightly fudged, need to find out why the laser tail is still misaligned for certain texture sizes
            startPos += i * unit;

            if (FastContainsPoint(screenRect, startPos))
            {
                Main.EntitySpriteDraw(texture, startPos - Main.screenPosition, tailFrame, color, r, new Vector2(tailRect.Width * .5f, tailRect.Height * .5f), scale, 0, 0);
            }
        }

        public static Tuple<Vector2, Vector2> Intersections(Rectangle screenRect, Vector2 lineStart, Vector2 lineDirection)
        {
            Vector2 firstResult = Vector2.Zero;
            Vector2 secondResult = Vector2.Zero;

            float rotation = lineDirection.ToRotation();
            float diff = (float)Math.Tan(rotation);
            diff *= -1;

            float leftIntersection = (diff * screenRect.Left) + lineStart.Y;
            if (leftIntersection > screenRect.Top && leftIntersection < screenRect.Bottom)
            {
                if (firstResult == Vector2.Zero)
                {
                    firstResult = new Vector2(screenRect.Left, leftIntersection);
                }
                else
                {
                    secondResult = new Vector2(screenRect.Left, leftIntersection);
                }
            }

            float rightIntersection = (diff * screenRect.Right) + lineStart.Y;
            if (rightIntersection > screenRect.Top && leftIntersection < screenRect.Bottom)
            {
                if (firstResult == Vector2.Zero)
                {
                    firstResult = new Vector2(screenRect.Right, rightIntersection);
                }
                else
                {
                    secondResult = new Vector2(screenRect.Right, rightIntersection);
                }
            }

            float topIntersection = (screenRect.Top - lineStart.Y) / diff;
            if (topIntersection > screenRect.Left && leftIntersection < screenRect.Right)
            {
                if (firstResult == Vector2.Zero)
                {
                    firstResult = new Vector2(topIntersection, screenRect.Top);
                }
                else
                {
                    secondResult = new Vector2(topIntersection, screenRect.Top);
                }
            }

            float bottomIntersection = (screenRect.Bottom - lineStart.Y) / diff;
            if (bottomIntersection > screenRect.Left && bottomIntersection < screenRect.Right)
            {
                if (firstResult == Vector2.Zero)
                {
                    firstResult = new Vector2(bottomIntersection, screenRect.Bottom);
                }
                else
                {
                    secondResult = new Vector2(bottomIntersection, screenRect.Bottom);
                }
            }

            return new Tuple<Vector2, Vector2>(firstResult, secondResult);
        }

        //20% Faster than converting drawStart into a Point and then running screenRect.Contains(point);
        public static bool FastContainsPoint(Rectangle screenrect, Vector2 point)
        {
            if (point.X < screenrect.X)
            {
                return false;
            }
            if (point.Y < screenrect.Y)
            {
                return false;
            }
            if (point.X > screenrect.Right)
            {
                return false;
            }
            if (point.Y > screenrect.Bottom)
            {
                return false;
            }

            return true;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (FiringTimeLeft <= 0 || !IsAtMaxCharge || TargetingMode != 0)
            {
                return false;
            }

            float point = 0f;
            Vector2 origin = GetOrigin();
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), origin,
                origin + Projectile.velocity * Distance, 22, ref point);
        }

        public override bool CanHitPlayer(Player target)
        {
            string deathMessage = Terraria.DataStructures.PlayerDeathReason.ByProjectile(-1, Projectile.whoAmI).GetDeathText(target.name).ToString();
            deathMessage = deathMessage.Replace("DefaultLaserName", LaserName);

            for (int i = 0; i < LaserDebuffs.Count; i++)
            {
                if (!target.immune)
                {
                    target.AddBuff(LaserDebuffs[i], DebuffTimers[i]);
                }
            }

            Vector2 endpoint = target.position;
            Vector2 origin = GetOrigin();
            float distance = Vector2.Distance(endpoint, origin);
            float velocity = -8f;
            Vector2 speed = ((endpoint - origin) / distance) * velocity;
            speed.X += Main.rand.Next(-1, 1);
            speed.Y += Main.rand.Next(-1, 1);
            int dust = Dust.NewDust(endpoint, 3, 3, LaserDust, speed.X + Main.rand.Next(-10, 10), speed.Y + Main.rand.Next(-10, 10), 20, default, 3.0f);
            Main.dust[dust].noGravity = true;
            dust = Dust.NewDust(endpoint, 3, 3, LaserDust, speed.X, speed.Y, 20, default, 1.0f);
            Main.dust[dust].noGravity = true;
            Main.dust[dust].shader = GameShaders.Armor.GetSecondaryShader(107, Main.LocalPlayer);
            dust = Dust.NewDust(endpoint, 30, 30, LaserDust, Main.rand.Next(-10, 10), Main.rand.Next(-10, 10), 20, default, 1.0f);
            Main.dust[dust].noGravity = true;

            target.Hurt(Terraria.DataStructures.PlayerDeathReason.ByCustomReason(deathMessage), Projectile.damage * 4, 1);
            return false;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            for (int i = 0; i < LaserDebuffs.Count; i++)
            {
                if (!target.dontTakeDamage)
                {
                    target.AddBuff(LaserDebuffs[i], DebuffTimers[i]);
                }
            }

            Vector2 endpoint = target.position;
            Vector2 origin = GetOrigin();
            float distance = Vector2.Distance(endpoint, origin);
            float velocity = -8f;
            Vector2 speed = ((endpoint - origin) / distance) * velocity;
            speed.X += Main.rand.Next(-1, 1);
            speed.Y += Main.rand.Next(-1, 1);
            int dust = Dust.NewDust(endpoint, 3, 3, LaserDust, speed.X + Main.rand.Next(-10, 10), speed.Y + Main.rand.Next(-10, 10), 20, default, 3.0f);
            Main.dust[dust].noGravity = true;
            dust = Dust.NewDust(endpoint, 3, 3, LaserDust, speed.X, speed.Y, 20, default, 1.0f);
            Main.dust[dust].noGravity = true;
            Main.dust[dust].shader = GameShaders.Armor.GetSecondaryShader(107, Main.LocalPlayer);
            dust = Dust.NewDust(endpoint, 30, 30, LaserDust, Main.rand.Next(-10, 10), Main.rand.Next(-10, 10), 20, default, 1.0f);
            Main.dust[dust].noGravity = true;
        }

        public override void AI()
        {
            Vector2 origin = GetOrigin();

            if (!ProjectileSource)
            {
                if (HostIdentifier != -1 && !Main.npc[HostIdentifier].active)
                {
                    Projectile.active = false;
                }
            }
            else
            {
                int decodedID = UsefulFunctions.DecodeID(HostIdentifier);
                if (decodedID == -1)
                {
                    Projectile.Kill();
                    Projectile.active = false;
                    return;
                }
                if (!Main.projectile[decodedID].active)
                {
                    Projectile.active = false;
                }
            }

            Projectile.position = origin + Projectile.velocity * MOVE_DISTANCE;
            Projectile.timeLeft = 2;

            ChargeLaser();
            if (TelegraphTime + Charge < MaxCharge) return;

            if (CastLight)
            {
                CastLights();
            }
            if (LineDust)
            {
                SpawnDusts();
            }

            CutTiles();
            SetLaserPosition();
            if (Projectile.tileCollide)
            {
                Vector2 endpoint = origin + Projectile.velocity * Distance;
                float velocity = -8f;
                Vector2 speed = ((endpoint - origin) / Distance) * velocity;
                speed.X += Main.rand.Next(-1, 1);
                speed.Y += Main.rand.Next(-1, 1);

                if (LaserDust != 0)
                {
                    //Smokey dust
                    int dust = Dust.NewDust(endpoint, 3, 3, 31, speed.X + Main.rand.Next(-10, 10), speed.Y + Main.rand.Next(-10, 10), 20, default, 1.0f);
                    //Main.dust[dust].noGravity = true;

                    //Colored dust (WIP, currently just uses LaserDust)
                    if (Main.rand.NextBool(20))
                    {
                        dust = Dust.NewDust(endpoint, 3, 3, LaserDust, speed.X, speed.Y, 20, default, 1.0f);
                        Main.dust[dust].noGravity = true;
                        Main.dust[dust].shader = GameShaders.Armor.GetSecondaryShader(107, Main.LocalPlayer);
                    }
                    if (Main.rand.NextBool(30))
                    {
                        dust = Dust.NewDust(endpoint, 30, 30, LaserDust, Main.rand.Next(-10, 10), Main.rand.Next(-10, 10), 20, default, 1.0f);
                        Main.dust[dust].noGravity = true;
                    }
                }
            }
            //float hitscanBeamLength = PerformBeamHitscan(hostPrism, chargeRatio >= 1f);
            //BeamLength = MathHelper.Lerp(BeamLength, hitscanBeamLength, BeamLengthChangeFactor);

            // This Vector2 stores the beam's hitbox statistics. X = beam length. Y = beam width.
            //Vector2 beamDims = new Vector2(projectile.velocity.Length() * BeamLength, projectile.width * projectile.scale);


            // If the game is rendering (i.e. isn't a dedicated server), make the beam disturb water.
            //if (Main.netMode != NetmodeID.Server)
            //{
            // ProduceWaterRipples(beamDims);
            //}
        }

        private void SetLaserPosition()
        {
            if (!TileCollide)
            {
                Distance = LaserLength;
                return;
            }

            Vector2? collision = UsefulFunctions.GetFirstCollision(GetOrigin(), Projectile.velocity, LaserLength, true, PierceNPCs);

            if (collision != null)
            {
                Distance = Vector2.Distance(GetOrigin(), collision.Value) + 32;
            }
            else
            {
                Distance = 2200f;
            }
        }

        private void ProduceWaterRipples(Vector2 beamDims)
        {
            WaterShaderData shaderData = (WaterShaderData)Filters.Scene["WaterDistortion"].GetShader();

            // A universal time-based sinusoid which updates extremely rapidly. GlobalTime is 0 to 3600, measured in seconds.
            float waveSine = 0.1f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 20f);
            Vector2 ripplePos = Projectile.position + new Vector2(beamDims.X * 0.5f, 0f).RotatedBy(Projectile.rotation);

            // WaveData is encoded as a Color. Not really sure why.
            Color waveData = new Color(0.5f, 0.1f * Math.Sign(waveSine) + 0.5f, 0f, 1f) * Math.Abs(waveSine);
            shaderData.QueueRipple(ripplePos, waveData, beamDims, RippleShape.Square, Projectile.rotation);
        }

        public void ChargeLaser()
        {
            if (Charge < MaxCharge || MaxCharge == 0)
            {
                Charge++;
                //Only play the sound once, on the frame it hits max charge
                if (Charge == MaxCharge || MaxCharge == 0)
                {
                    if (LaserSound != null)
                    {
                        Terraria.Audio.SoundEngine.PlaySound(LaserSound.Value);
                    }
                    //Then, set it to fire for the FIRING_TIME frames
                    FiringTimeLeft = FiringDuration;
                    MaxCharge = -1;
                }
            }

            if (TargetingMode == 0)
            {
                if (FiringTimeLeft > 0)
                {
                    FiringTimeLeft--;
                    if (FiringTimeLeft == 0)
                    {
                        Projectile.Kill();
                    }
                }
            }
            else
            {
                TelegraphTime--;
                if (TelegraphTime <= 0)
                {
                    Projectile.Kill();
                }
            }
        }


        private void CastLights()
        {
            // Cast a light along the line of the laser
            Color currentColor;
            if (LightColor == null)
            {
                currentColor = LaserColor;
            }
            else
            {
                currentColor = LightColor.Value;
            }
            Vector3 colorVector = currentColor.ToVector3();
            if (!IsAtMaxCharge)
            {
                //Draw it dimmer if it's not really firing
                colorVector /= 2;
            }
            DelegateMethods.v3_1 = colorVector;
            Utils.PlotTileLine(Projectile.Center, Projectile.Center + Projectile.velocity * (Distance - MOVE_DISTANCE), 8, DelegateMethods.CastLight);
        }
        private void SpawnDusts()
        {
            Vector2 unit = Projectile.velocity * -1;
            Vector2 origin = GetOrigin();
            Vector2 dustPos = origin + Projectile.velocity;


            if (Charge >= MaxCharge)
            {
                for (int j = 0; j < DustAmount; j++)
                {
                    if (Main.rand.NextBool(5))
                    {
                        Vector2 offset = Projectile.velocity.RotatedByRandom(MathHelper.ToRadians(8));
                        Dust dust = Main.dust[Dust.NewDust((origin + (Projectile.velocity * (Distance * (float)((float)j / (float)DustAmount)))) + offset - Vector2.One * 4f, 8, 8, LaserDust, 0.0f, 0.0f, 125, default, 2.0f)];
                        dust.velocity = Vector2.Zero;
                        dust.noGravity = true;
                        dust.rotation = Projectile.rotation;
                    }
                }
            }
        }

        public virtual Vector2 GetOrigin()
        {
            if (FollowHost)
            {
                if (ProjectileSource)
                {
                    int decodedID = UsefulFunctions.DecodeID(HostIdentifier);
                    if (decodedID == -1)
                    {
                        Projectile.Kill();
                        Projectile.active = false;
                        return Vector2.Zero;
                    }

                    return Main.projectile[decodedID].Center + LaserOffset;
                }
                else
                {
                    return Main.npc[HostIdentifier].Center + LaserOffset;
                }
            }
            else
            {
                return LaserOrigin + LaserOffset;
            }
        }

        public override bool ShouldUpdatePosition() => false;

        public override void CutTiles()
        {
            DelegateMethods.tilecut_0 = TileCuttingContext.AttackProjectile;
            DelegateMethods.tileCutIgnore = TileID.Sets.TileCutIgnore.None;
            Vector2 unit = Projectile.velocity;
            DelegateMethods.tilecut_0 = TileCuttingContext.AttackProjectile;
            DelegateMethods.tileCutIgnore = TileID.Sets.TileCutIgnore.None;
            Utils.PlotTileLine(Projectile.Center, Projectile.Center + unit * Distance, (Projectile.width + 16) * Projectile.scale, DelegateMethods.CutTiles);
        }
    }
}

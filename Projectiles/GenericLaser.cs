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

    public class GenericLaser : ModProjectile
    {

        //Generic laser class
        //Lets you easily create lasers of any size and color, and give them a variety of behaviors

        //Set to true if the laser originates from a projectile instead of an NPC
        public bool ProjectileSource = false;

        //The name the laser will display if it kills the player
        public string LaserName = "Laser";

        //The source position of the laser. If FOLLOW_SOURCE is set to true, this will be ignored.
        public Vector2 LaserOrigin = new Vector2(0, 0);

        //The target of the laser
        public Vector2 LaserTarget = new Vector2(0, 0);

        //Should it stick to the center of the NPC or Projectile that spawned it, or just where it is spawned?
        public bool FollowHost = false;

        //Set to 0 for normal behavior
        //Set to 1 to only display the transparent 'targeting' beam
        //Set to 2 to draw in full, but still do no damage
        public int TargetingMode = 0;

        //Should the laser be offset from the center of its source? If so, how much?
        public Vector2 LaserOffset = new Vector2(0, 0);

        //What color is the laser? Leave this blank if it has a custom texture
        public Color LaserColor = Color.White;

        //How long should it telegraph its path with a targeting laser? This can be set to 0 for instant hits
        public int TelegraphTime = 60;

        //What dust should it spawn?
        public int LaserDust = 0;

        //Should it create a line of dust along its length?
        public bool LineDust = false;

        //Scales the size of the laser
        public float LaserSize = 0.4f;

        //Should it stop when it hits tiles?
        public bool TileCollide = true;

        //How long should the laser be?
        //Defaults to 5000, max of 20000
        public int LaserLength = 5000;

        //Lighting is computationally expensive. Set this to false to improve performance dramatically when many lasers are on the screen.
        public bool CastLight = true;

        //Should it have a light color different than 'LaserColor'?
        public Color? lightColor = null;

        //Should it play a vanilla sound?
        public SoundStyle? LaserSound = SoundID.Item60;

        //What volume should it play the sound at?
        public float LaserVolume = 10f;

        //Does it have a custom texture?
        public TransparentTextureHandler.TransparentTextureType LaserTexture = TransparentTextureHandler.TransparentTextureType.GenericLaser;

        //What about for its targeting beam?
        public TransparentTextureHandler.TransparentTextureType LaserTargetingTexture = TransparentTextureHandler.TransparentTextureType.GenericLaserTargeting;

        public Rectangle LaserTextureBody = new Rectangle(0, 0, 46, 28);
        public Rectangle LaserTextureTail = new Rectangle(0, 30, 46, 28);
        public Rectangle LaserTextureHead = new Rectangle(0, 60, 46, 28);

        //If it's animated, how many frames does it have?
        public int frameCount = 1;

        //How many ticks per frame?
        public int frameDuration = 0;

        public int currentFrame = 0;
        public int frameCounter = 0;

        //What debuffs should it inflict?
        public List<int> LaserDebuffs = new List<int>();
        //How long should those debuffs last?
        public List<int> DebuffTimers = new List<int>();

        //Has it already been initialized on the client it's running on? If so, re-setting all its basic values is unnecessary.
        public bool initialized = false;

        //How long (in frames) does it have to charge before firing?
        //If this is 0, the charge mechanic will simply be disabled
        public float MaxCharge = 120;
        //How long (in frames) should the laser fire once it is charged? Defaults to 2 seconds
        public int FiringDuration = 120;

        //How long should each "segment" of the laser be? This value should pretty much be fine
        private const float MOVE_DISTANCE = 20f;

        public float Distance = 0;

        public bool customContext = false;

        /*{
            get => projectile.ai[0];
            set => projectile.ai[0] = value;
        }*/

        //Allows the projectile to be tagged with an ID upon creation, so that it can be identified across clients
        //Projectile id's aren't synced, so we have to do it ourself like this
        //Messing with this is only necessary if you need to change a laser *after* it has been created (ex: to make it move)
        public float NetworkID
        {
            get => Projectile.ai[0];
            set => Projectile.ai[0] = value;
        }

        public int HostIdentifier
        {
            get => (int)Projectile.ai[1];
            set => Projectile.ai[1] = value;
        }

        public float Charge
        {
            get => Projectile.localAI[0];
            set => Projectile.localAI[0] = value;
        }

        public bool IsAtMaxCharge => (Charge == MaxCharge || MaxCharge == 0 || MaxCharge == -1);

        public int FiringTimeLeft = 0;
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Laser");

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
            Projectile.hide = true;

            LaserOrigin = ProjectileSource ? Main.projectile[HostIdentifier].position : Main.npc[HostIdentifier].position;
        }

        //Terraria doesn't sync projectile id's. We need to find it somehow.
        //This lets us tag the ai[1] of laser projectiles with an id so it can be found later by clients.
        //The downside is that a new entry here is required for each new "category" of laser we add.
        //Kinda hacky. Class needs to be reworked conceptually.
        public enum GenericLaserID
        {
            DarkDivineSpark = 0,
            DarkDivineSparkTargeting = 1,
            AntiMatTargeting = 2,
            AttradiesDarkLaser = 3,
            SolarDetonator = 4,
            StardustLaser = 5,
        }

        //Gets all lasers with a certain ID, optionally only those which are owned by a certain NPC
        public static List<GenericLaser> GetLasersByID(GenericLaserID targetID, int laserHostIdentifier = -1)
        {
            List<GenericLaser> LaserList = new List<GenericLaser>();
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                if (Main.projectile[i].active && Main.projectile[i].type == ModContent.ProjectileType<GenericLaser>())
                {
                    GenericLaser currentLaser = (GenericLaser)Main.projectile[i].ModProjectile;
                    if (currentLaser != null)
                    {
                        if ((int)currentLaser.NetworkID == (int)targetID)
                        {
                            if (laserHostIdentifier == -1 || laserHostIdentifier == currentLaser.HostIdentifier)
                            {
                                LaserList.Add(currentLaser);
                            }
                        }
                    }
                }
            }
            return LaserList;
        }

        //TODO: Test this more.
        public override void OnHitPlayer(Player target, int damage, bool crit)
        {
            string deathMessage;
            if (ProjectileSource)
            {
                //??? What kind of index does ByOther want? Tile? Projectile? Why can't these just take a name...
                deathMessage = Terraria.DataStructures.PlayerDeathReason.ByOther(Projectile.whoAmI).GetDeathText(target.name).ToString();
            }
            else
            {
                //ByProjectile... doesn't work either. It's PVP only I think? Its first parameter "byplayerindex" specifies the index for the player who landed the final blow...
                deathMessage = Terraria.DataStructures.PlayerDeathReason.ByProjectile(target.whoAmI, Projectile.whoAmI).GetDeathText(target.name).ToString();
            }
            deathMessage = deathMessage.Replace("DefaultLaserName", LaserName);
            //target.Hurt(Terraria.DataStructures.PlayerDeathReason.ByCustomReason(deathMessage), damage, 1);
        }

        // Add this projectile to the list of projectiles that will be drawn BEFORE tiles and NPC are drawn. This makes the projectile appear to be BEHIND the tiles and NPC.
        public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs, List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI)
        {
            behindNPCs.Add(index);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (!customContext)
            {
                return false;
            }
  
            //LaserTextureBody.Height * LaserSize
            if ((IsAtMaxCharge && TargetingMode == 0) || (TargetingMode == 2))
            {
                DrawLaser(Main.spriteBatch, TransparentTextureHandler.TransparentTextures[LaserTexture], GetOrigin(),
                    Projectile.velocity, LaserTextureBody.Height * LaserSize, -1.57f, LaserSize, LaserLength, LaserColor, (int)MOVE_DISTANCE);
            }
            else if (TelegraphTime + Charge >= MaxCharge || TargetingMode == 1)
            {
                DrawLaser(Main.spriteBatch, TransparentTextureHandler.TransparentTextures[LaserTargetingTexture], GetOrigin(),
                    Projectile.velocity, LaserTextureBody.Height * LaserSize / 2f, -1.57f, LaserSize / 2, LaserLength, LaserColor, (int)MOVE_DISTANCE);
            }
            return false;
        }

        public void DrawLaser(SpriteBatch spriteBatch, Texture2D texture, Vector2 start, Vector2 unit, float step, float rotation = 0f, float scale = 1f, float maxDist = 2000f, Color color = default, int transDist = 50)
        {

            float r = unit.ToRotation() + rotation;
            Rectangle bodyFrame = LaserTextureBody;
            bodyFrame.X = LaserTextureBody.Width * currentFrame;
            Rectangle headFrame = LaserTextureHead;
            headFrame.X *= LaserTextureHead.Width * currentFrame;
            Rectangle tailFrame = LaserTextureTail;
            tailFrame.X *= LaserTextureTail.Width * currentFrame;

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

            Vector2 startPos = start + (LaserTextureHead.Height / 2) * unit * scale;
            Main.EntitySpriteDraw(texture, startPos - Main.screenPosition, headFrame, LaserColor, r, new Vector2(LaserTextureHead.Width * .5f, LaserTextureHead.Height * .5f), scale, 0, 0);

            float i = (LaserTextureBody.Height / 2 + LaserTextureHead.Height) * scale; //0;//( * 2) + 
            for (; i <= Distance; i += step)
            {
                startPos = start + i * unit;
                Main.EntitySpriteDraw(texture, startPos - Main.screenPosition, bodyFrame, LaserColor, r, new Vector2(LaserTextureBody.Width * .5f, LaserTextureBody.Height * .5f), scale, 0, 0);
            }
            startPos = start + i * unit;
            Main.EntitySpriteDraw(texture, startPos - Main.screenPosition, tailFrame, LaserColor, r, new Vector2(LaserTextureTail.Width * .5f, LaserTextureTail.Height * .5f), scale, 0, 0);


            if (CastLight)
            {
                CastLights();
            }
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (FiringTimeLeft <= 0 || !IsAtMaxCharge || TargetingMode != 0) return false;

            Vector2 unit = Projectile.velocity;
            float point = 0f;
            Vector2 origin = GetOrigin();
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), origin,
                origin + unit * Distance, 22, ref point);
        }

        public override void OnHitNPC(NPC target, int damage, float knockback, bool crit)
        {
            for (int i = 0; i < LaserDebuffs.Count; i++)
            {
                target.AddBuff(LaserDebuffs[i], DebuffTimers[i]);
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
                if (!Main.npc[HostIdentifier].active)
                {
                    Projectile.active = false;
                }
            }


            Projectile.position = origin + Projectile.velocity * MOVE_DISTANCE;
            Projectile.timeLeft = 2;

            UpdateProjectile();

            if (FiringTimeLeft > 0)
            {
                FiringTimeLeft--;
                if (FiringTimeLeft == 0)
                {
                    Projectile.Kill();
                }
            }

            ChargeLaser();
            if (LaserDust != 0)
            {
                int pointdust = Dust.NewDust(Projectile.position, 1, 1, LaserDust, Main.rand.Next(-5, 5), Main.rand.Next(-5, 5), 20, default, 1.0f);
                Main.dust[pointdust].noGravity = true;
            }
            if (TelegraphTime + Charge < MaxCharge) return;

            if (LineDust)
            {
                SpawnDusts();
            }
            SetLaserPosition();
            if (Projectile.tileCollide)
            {
                Vector2 endpoint = origin + Projectile.velocity * Distance;
                float distance = Vector2.Distance(endpoint, origin);
                float velocity = -8f;
                Vector2 speed = ((endpoint - origin) / distance) * velocity;
                speed.X += Main.rand.Next(-1, 1);
                speed.Y += Main.rand.Next(-1, 1);

                if (LaserDust != 0)
                {
                    //Smokey dust
                    int dust = Dust.NewDust(endpoint, 3, 3, 31, speed.X + Main.rand.Next(-10, 10), speed.Y + Main.rand.Next(-10, 10), 20, default, 1.0f);
                    Main.dust[dust].noGravity = true;

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
            for (Distance = MOVE_DISTANCE; Distance <= LaserLength; Distance += 50f)
            {
                if (!TileCollide)
                {
                    Distance = LaserLength;
                    break;
                }
                Vector2 origin = GetOrigin();
                var start = origin + Projectile.velocity * Distance;
                if (!Collision.CanHit(origin, 1, 1, start, 1, 1) && !Collision.CanHitLine(origin, 1, 1, start, 1, 1))
                {
                    Distance -= 5f;
                    break;
                }
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

        private void ChargeLaser()
        {
            if (Charge < MaxCharge || MaxCharge == 0)
            {
                Charge++;
                //Only play the sound once, on the frame it hits max charge
                if (Charge == MaxCharge || MaxCharge == 0)
                {
                    if (LaserSound != null)
                    {
                        Terraria.Audio.SoundEngine.PlaySound(LaserSound.Value with { Volume = LaserVolume });
                    }

                    //Then, set it to fire for the FIRING_TIME frames
                    FiringTimeLeft = FiringDuration;
                    MaxCharge = -1;
                }
            }
            Vector2 dustVelocity = Vector2.UnitX * 18f;
            dustVelocity = dustVelocity.RotatedBy(Projectile.rotation - 1.57f);
        }

        private void UpdateProjectile()
        {
            Vector2 origin = GetOrigin();
            Vector2 diff = LaserTarget - origin;
            diff.Normalize();
            Projectile.velocity = diff;
            Projectile.direction = LaserTarget.X > origin.X ? 1 : -1;
        }

        private void CastLights()
        {
            // Cast a light along the line of the laser
            Color currentColor;
            if (lightColor == null)
            {
                currentColor = LaserColor;
            }
            else
            {
                currentColor = lightColor.Value;
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
            for (int i = 0; i < 2; ++i)
            {

                float num1 = Projectile.velocity.ToRotation() + (Main.rand.NextBool(2) ? -1.0f : 1.0f) * 1.57f;
                float num2 = (float)(Main.rand.NextDouble() * 0.8f + 1.0f);
                Vector2 dustVel = new Vector2((float)Math.Cos(num1) * num2, (float)Math.Sin(num1) * num2);
                Dust dust = Main.dust[Dust.NewDust(dustPos, 0, 0, 226, dustVel.X, dustVel.Y)];
                dust.noGravity = true;
                dust.scale = 1.2f;
                Dust.NewDustDirect(origin, 0, 0, LaserDust,
                    -unit.X * Distance, -unit.Y * Distance, 255, Color.White, 10.0f);
            }

            for (int j = 0; j < 100; j++)
            {
                if (Main.rand.NextBool(5))
                {
                    Vector2 offset = Projectile.velocity.RotatedByRandom(MathHelper.ToRadians(8));
                    Dust dust = Main.dust[Dust.NewDust((origin + (Projectile.velocity * (Distance * (float)(j / 100f)))) + offset - Vector2.One * 4f, 8, 8, LaserDust, 0.0f, 0.0f, 125, Color.LightBlue, 4.0f)];
                    dust.velocity = Vector2.Zero;
                    dust.noGravity = true;
                    dust.rotation = Projectile.rotation;
                }
            }
        }

        public Vector2 GetOrigin()
        {
            if (FollowHost)
            {
                if (ProjectileSource)
                {
                    return Main.projectile[HostIdentifier].position + LaserOffset;
                }
                else
                {
                    return Main.npc[HostIdentifier].position + LaserOffset;
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
            if (Charge == MaxCharge)
            {
                DelegateMethods.tilecut_0 = TileCuttingContext.AttackProjectile;
                Vector2 unit = Projectile.velocity;
                Utils.PlotTileLine(Projectile.Center, Projectile.Center + unit * Distance, (Projectile.width + 16) * Projectile.scale, DelegateMethods.CutTiles);
            }
        }
    }
}

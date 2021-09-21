using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Enums;
using Terraria.GameContent.Shaders;
using Terraria.Graphics.Effects;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles {

    public class GenericLaser : ModProjectile {

        //Generic laser class
        //Lets you easily create lasers of any size and color, and give them a variety of behaviors

        //Set to true if the laser originates from a projectile instead of an NPC
        public bool ProjectileSource = false;

        //The source position of the laser. If FOLLOW_SOURCE is set to true, this will be ignored.
        public Vector2 LaserOrigin = new Vector2(0, 0);

        //The target of the laser
        public Vector2 LaserTarget = new Vector2(0, 0);

        //Should it stick to the center of the NPC or Projectile that spawned it, or just where it is spawned?
        public bool FollowHost = false;

        //Set to true if the laser is friendly
        public bool FriendlyLaser = false;

        //Should the laser be offset from the center of its source? If so, how much?
        public Vector2 LaserOffset = new Vector2(0, 0);

        //What color is the laser? Leave this blank if it has a custom texture
        public Color LaserColor = Color.White;

        //How long should it telegraph its path with a targeting laser? This can be set to 0 for instant hits
        public int TelegraphTime = 60;

        //What dust should it spawn?
        public int LaserDust = 0;

        //Scales the size of the laser
        public float LaserSize = 0.4f;

        //Should it stop when it hits tiles?
        public bool TileCollide = true;

        //How long should the laser be?
        //Defaults to 5000, max of 20000
        public int LaserLength = 5000;

        //Lighting is computationally expensive. Set this to false to improve performance dramatically when many lasers are on the screen.
        public bool CastLight = true;

        //Should it play a vanilla sound?
        public Terraria.Audio.LegacySoundStyle LaserSound = new Terraria.Audio.LegacySoundStyle(2, 60);

        //Sound it play a custom sound? This overrides whatver LASER_SOUND is set to
        public string CustomSound = null;

        //What volume should it play the sound at?
        public float LaserVolume = 10f;

        //Does it have a custom texture?
        public TransparentTextureHandler.TransparentTextureType LaserTexture = TransparentTextureHandler.TransparentTextureType.GenericLaser;

        //What about for its targeting beam?
        public TransparentTextureHandler.TransparentTextureType LaserTargetingTexture = TransparentTextureHandler.TransparentTextureType.GenericLaserTargeting;

        public Rectangle LaserTextureBody = new Rectangle(0, 0, 46, 28);
        public Rectangle LaserTextureTail = new Rectangle(0, 30, 46, 28);
        public Rectangle LaserTextureHead = new Rectangle(0, 60, 46, 28);


        //What debuffs should it inflict?
        public List<int> LaserDebuffs = new List<int>();
        //How long should those debuffs last?
        public List<int> DebuffTimers = new List<int>();

        //How long (in frames) does it have to charge before firing?
        //If this is 0, the charge mechanic will simply be disabled
        public float MaxCharge = 120;
        //How long (in frames) should the laser fire once it is charged? Defaults to 2 seconds
        public int FiringDuration = 120;

        //How long should each "segment" of the laser be? This value should pretty much be fine
        private const float MOVE_DISTANCE = 20f;

        public float Distance {
            get => projectile.ai[0];
            set => projectile.ai[0] = value;
        }

        public float Charge {
            get => projectile.localAI[0];
            set => projectile.localAI[0] = value;
        }

        public bool IsAtMaxCharge => Charge == MaxCharge;

        public int FiringTimeLeft = 0;
        public override void SetStaticDefaults()
        {
            DisplayName.SetDefault("Laser");

        }
        public override void SetDefaults() {
            projectile.width = 10;
            projectile.height = 10;
            projectile.friendly = false;
            projectile.hostile = true;
            projectile.penetrate = -1;
            projectile.tileCollide = false;
            projectile.magic = true;
            projectile.damage = 25;

            LaserOrigin = ProjectileSource ? Main.projectile[HostIndex].position : Main.npc[HostIndex].position;
        }       

        
        public override bool PreDraw(SpriteBatch spriteBatch, Color lightColor) {

            if (IsAtMaxCharge) 
            {
                DrawLaser(spriteBatch, TransparentTextureHandler.TransparentTextures[LaserTexture], GetOrigin(),
                    projectile.velocity, LaserTextureBody.Height * LaserSize, -1.57f, LaserSize, LaserLength, LaserColor, (int)MOVE_DISTANCE);
            }
            else if(TelegraphTime + Charge >= MaxCharge)
            {
                DrawLaser(spriteBatch, TransparentTextureHandler.TransparentTextures[LaserTargetingTexture], GetOrigin(),
                    projectile.velocity, LaserTextureBody.Height * LaserSize / 2, -1.57f, LaserSize / 2, LaserLength, LaserColor, (int)MOVE_DISTANCE);
            }
            return false;
        }

        public void DrawLaser(SpriteBatch spriteBatch, Texture2D texture, Vector2 start, Vector2 unit, float step, float rotation = 0f, float scale = 1f, float maxDist = 2000f, Color color = default, int transDist = 50) {
            float r = unit.ToRotation() + rotation;

            Vector2 startPos = start + (transDist - step) * unit;
            spriteBatch.Draw(texture, startPos - Main.screenPosition, LaserTextureHead, LaserColor, r, new Vector2(LaserTextureTail.Width * .5f, LaserTextureTail.Height * .5f), scale, 0, 0);
            
            float i = transDist;
            for (; i <= Distance; i += step) {
                startPos = start + i * unit;    
                spriteBatch.Draw(texture, startPos - Main.screenPosition, LaserTextureBody, LaserColor, r, new Vector2(LaserTextureBody.Width * .5f, LaserTextureBody.Height * .5f), scale, 0, 0);               
            }            
            startPos = start + i * unit;
            spriteBatch.Draw(texture, startPos - Main.screenPosition, LaserTextureTail, LaserColor, r, new Vector2(LaserTextureTail.Width * .5f, LaserTextureTail.Height * .5f), scale, 0, 0);
            

            if (CastLight)
            {
                CastLights();
            }
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
            if (FiringTimeLeft <= 0 || !IsAtMaxCharge) return false;            

            Vector2 unit = projectile.velocity;
            float point = 0f;
            Vector2 origin = GetOrigin();
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), origin,
                origin + unit * Distance, 22, ref point);
        }

        public override void OnHitNPC(NPC target, int damage, float knockback, bool crit)
        {
            for(int i = 0; i < LaserDebuffs.Count; i++)
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

        private int HostIndex
        {
            get => (int)projectile.ai[1];
            set => projectile.ai[1] = value;
        }

        public override void AI() {
            Vector2 origin = GetOrigin();
            projectile.position = origin + projectile.velocity * MOVE_DISTANCE;
            projectile.timeLeft = 2;

            UpdateProjectile();

            if(FiringTimeLeft > 0)
            {
                FiringTimeLeft--;
                if (FiringTimeLeft == 0)
                {
                    projectile.Kill();
                }
            }
            
            ChargeLaser();
            int pointdust = Dust.NewDust(projectile.position, 1, 1, LaserDust, Main.rand.Next(-5, 5), Main.rand.Next(-5, 5), 20, default, 1.0f);
            Main.dust[pointdust].noGravity = true;

            if (TelegraphTime + Charge < MaxCharge) return;

            SetLaserPosition();
            if (projectile.tileCollide)
            {
                Vector2 endpoint = origin + projectile.velocity * Distance;
                float distance = Vector2.Distance(endpoint, origin);
                float velocity = -8f;
                Vector2 speed = ((endpoint - origin) / distance) * velocity;
                speed.X += Main.rand.Next(-1, 1);
                speed.Y += Main.rand.Next(-1, 1);

                //Smokey dust
                int dust = Dust.NewDust(endpoint, 3, 3, 31, speed.X + Main.rand.Next(-10, 10), speed.Y + Main.rand.Next(-10, 10), 20, default, 1.0f);
                Main.dust[dust].noGravity = true;

                //Colored dust (WIP, currently just uses LaserDust)
                if (Main.rand.Next(20) == 1)
                {
                    dust = Dust.NewDust(endpoint, 3, 3, LaserDust, speed.X, speed.Y, 20, default, 1.0f);
                    Main.dust[dust].noGravity = true;
                    Main.dust[dust].shader = GameShaders.Armor.GetSecondaryShader(107, Main.LocalPlayer);
                }
                if (Main.rand.Next(30) == 1)
                {
                    dust = Dust.NewDust(endpoint, 30, 30, LaserDust, Main.rand.Next(-10, 10), Main.rand.Next(-10, 10), 20, default, 1.0f);
                    Main.dust[dust].noGravity = true;
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

        private void SetLaserPosition() {
            for (Distance = MOVE_DISTANCE; Distance <= 20200f; Distance += 50f)
            {
                if (!TileCollide)
                {
                    Distance = 20200;
                    break;
                }
                Vector2 origin = GetOrigin();
                var start = origin + projectile.velocity * Distance;
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
            float waveSine = 0.1f * (float)Math.Sin(Main.GlobalTime * 20f);
            Vector2 ripplePos = projectile.position + new Vector2(beamDims.X * 0.5f, 0f).RotatedBy(projectile.rotation);

            // WaveData is encoded as a Color. Not really sure why.
            Color waveData = new Color(0.5f, 0.1f * Math.Sign(waveSine) + 0.5f, 0f, 1f) * Math.Abs(waveSine);
            shaderData.QueueRipple(ripplePos, waveData, beamDims, RippleShape.Square, projectile.rotation);
        }

        private void ChargeLaser() {
            if (Charge < MaxCharge) {
                Charge++;
                //Only play the sound once, on the frame it hits max charge
                if(Charge == MaxCharge)
                {
                    if (CustomSound == null)
                    {
                        Main.PlaySound(LaserSound.WithVolume(LaserVolume));
                    }
                    else
                    {
                        Main.PlaySound(mod.GetLegacySoundSlot(SoundType.Item, CustomSound).WithVolume(LaserVolume));
                    }

                    //Then, set it to fire for the FIRING_TIME frames
                    FiringTimeLeft = FiringDuration;
                }
            }
            Vector2 dustVelocity = Vector2.UnitX * 18f;
            dustVelocity = dustVelocity.RotatedBy(projectile.rotation - 1.57f);      
        }

        private void UpdateProjectile() {
            Vector2 origin = GetOrigin();
            Vector2 diff = LaserTarget - origin;
            diff.Normalize();
            projectile.velocity = diff;
            projectile.direction = LaserTarget.X > origin.X ? 1 : -1;
            projectile.netUpdate = true;
        }

        private void CastLights() {
            // Cast a light along the line of the laser
            Vector3 colorVector = LaserColor.ToVector3();
            if (!IsAtMaxCharge)
            {
                //Draw it dimmer if it's not really firing
                colorVector /= 2;
            }
            DelegateMethods.v3_1 = colorVector;
            Utils.PlotTileLine(projectile.Center, projectile.Center + projectile.velocity * (Distance - MOVE_DISTANCE), 8, DelegateMethods.CastLight);
        }

        public Vector2 GetOrigin()
        {
            if (FollowHost)
            {
                if (ProjectileSource)
                {
                    return Main.projectile[HostIndex].position + LaserOffset;
                }
                else
                {
                    return Main.npc[HostIndex].position + LaserOffset;
                }
            }
            else
            {                
                return LaserOrigin + LaserOffset;                
            }
        }

        public override bool ShouldUpdatePosition() => false;

        public override void CutTiles() {
            if (Charge == MaxCharge)
            {
                DelegateMethods.tilecut_0 = TileCuttingContext.AttackProjectile;
                Vector2 unit = projectile.velocity;
                Utils.PlotTileLine(projectile.Center, projectile.Center + unit * Distance, (projectile.width + 16) * projectile.scale, DelegateMethods.CutTiles);
            }
        }
    }
}

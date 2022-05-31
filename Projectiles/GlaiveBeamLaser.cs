using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Enums;
using Terraria.GameContent.Shaders;
using Terraria.Graphics.Effects;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles {

    public class GlaiveBeamLaser : ModProjectile {

        //Fair warning: This is an absolute garbled mess right now, mostly as the result of experimentation.
        //Expect it to look pretty different in the future.

        public static float MAX_CHARGE = GlaiveBeamHoldout.MaxCharge;
        private const float MOVE_DISTANCE = 20f;
        public const int FIRING_TIME = 120;

        public float Distance {
            get => Projectile.ai[0];
            set => Projectile.ai[0] = value;
        }

        public float Charge {
            get => Projectile.localAI[0];
            set => Projectile.localAI[0] = value;
        }

        public bool IsAtMaxCharge => Charge == MAX_CHARGE;

        public int FiringTimeLeft = 0;

        public override void SetDefaults() {
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.hide = true;
            Projectile.damage = 25;
        }


        //Moves the centerpoint of the beam so it's always aligned with the sprite
        //Kinda hacky, required manually tuning it.
        //There was a better way to do it, which I learned later when drawing the glowmasks.
        //Might go back and change this to use that code for this at some point. It works for now, though.
        private Vector2 GetOrigin()
        {
            Vector2 origin = Main.player[Projectile.owner].Center;
            origin.X -= 5 * Main.player[Projectile.owner].direction;
            if (Main.player[Projectile.owner].gravDir != 1 && ModContent.GetInstance<tsorcRevampConfig>().GravityFix)
            {
                origin.Y += 15;
            }
            else
            {
                origin.Y -= 15;
            }
            if (Main.player[Projectile.owner].gravDir != 1 && ModContent.GetInstance<tsorcRevampConfig>().GravityFix)
            {
                if (Main.player[Projectile.owner].itemRotation < 0)
                {
                    //If aiming in the upper right quadrant
                    if (Main.player[Projectile.owner].direction == 1)
                    {
                        origin.X += 8 + -8 * (float)Math.Cos(Math.Abs(Main.player[Projectile.owner].itemRotation));
                        //origin.Y += 0 * (float)Math.Sin(Math.Abs(Main.player[projectile.owner].itemRotation));
                    }
                    //Bottom left
                    else
                    {
                        origin.X -= -10 * (float)Math.Cos(Math.Abs(Main.player[Projectile.owner].itemRotation));
                        origin.Y -= 10 * (float)Math.Sin(Math.Abs(Main.player[Projectile.owner].itemRotation));
                    }
                }
                else
                {
                    //Bottom right
                    if (Main.player[Projectile.owner].direction == 1)
                    {
                        origin.X += 0 * (float)Math.Cos(Math.Abs(Main.player[Projectile.owner].itemRotation));
                        origin.Y -= 15 * (float)Math.Sin(Math.Abs(Main.player[Projectile.owner].itemRotation));
                    }
                    //Upper left
                    else
                    {
                        origin.X -= 5 + -5 * (float)Math.Cos(Math.Abs(Main.player[Projectile.owner].itemRotation));
                        //origin.Y += 15 * (float)Math.Sin(Math.Abs(Main.player[projectile.owner].itemRotation));
                    }
                }
            }
            else
            {
                if (Main.player[Projectile.owner].itemRotation < 0)
                {
                    //If aiming in the upper right quadrant
                    if (Main.player[Projectile.owner].direction == 1)
                    {
                        origin.X -= 10 * (float)Math.Cos(Math.Abs(Main.player[Projectile.owner].itemRotation));
                        origin.Y -= -10 * (float)Math.Sin(Math.Abs(Main.player[Projectile.owner].itemRotation));
                    }
                    //Bottom left
                    else
                    {
                        origin.X -= 5 + -10 * (float)Math.Cos(Math.Abs(Main.player[Projectile.owner].itemRotation));
                        origin.Y += -10 * (float)Math.Sin(Math.Abs(Main.player[Projectile.owner].itemRotation));
                    }
                }
                else
                {
                    //Bottom right
                    if (Main.player[Projectile.owner].direction == 1)
                    {
                        origin.X += 6 + -11 * (float)Math.Cos(Math.Abs(Main.player[Projectile.owner].itemRotation));
                        origin.Y += -10 * (float)Math.Sin(Math.Abs(Main.player[Projectile.owner].itemRotation));
                    }
                    //Upper left
                    else
                    {
                        origin.X += 10 * (float)Math.Cos(Math.Abs(Main.player[Projectile.owner].itemRotation));
                        origin.Y -= -10 * (float)Math.Sin(Math.Abs(Main.player[Projectile.owner].itemRotation));
                    }
                }
            }

            return origin;
        }

        public override bool PreDraw(ref Color lightColor) {           
            //Get the premultiplied, properly transparent texture
            Texture2D texture = TransparentTextureHandler.TransparentTextures[TransparentTextureHandler.TransparentTextureType.GlaiveBeam];           

            if (IsAtMaxCharge && FiringTimeLeft > 0) {
                float scale = 0.4f;

                DrawLaser(spriteBatch, texture, GetOrigin(),
                    Projectile.velocity, 28 * scale, Projectile.damage, -1.57f, scale, 2000f, Color.White, (int)MOVE_DISTANCE);
            }
            return false;
        }

        public void DrawLaser(SpriteBatch spriteBatch, Texture2D texture, Vector2 start, Vector2 unit, float step, int damage, float rotation = 0f, float scale = 1f, float maxDist = 2000f, Color color = default, int transDist = 50) {
            float r = unit.ToRotation() + rotation;
            
            // Draws the laser 'body'
            for (float i = transDist; i <= Distance; i += step) {

                Color c = Color.White;
                var origin = start + i * unit;
                
                Main.EntitySpriteDraw(texture, origin - Main.screenPosition, new Rectangle(0, 0, 46, 28), i < transDist ? Color.Transparent : c, r, new Vector2(46 * .5f, 28 * .5f), scale, 0, 0);

                //If it's at the end, draws the end of the laser
                if((i + step) > Distance)
                {
                    i += step;
                    origin = start + i * unit;
                    Main.EntitySpriteDraw(texture, origin - Main.screenPosition, new Rectangle(0, 30, 46, 28), i < transDist ? Color.Transparent : c, r, new Vector2(46 * .5f, 28 * .5f), scale, 0, 0);
                }
            }
            CastLights();
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) {
            if ((!IsAtMaxCharge) || FiringTimeLeft <= 0) return false;


            Player player = Main.player[Projectile.owner];
            Vector2 unit = Projectile.velocity;
            float point = 0f;
            Vector2 origin = GetOrigin();
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), origin,
                origin + unit * Distance, 22, ref point);
        }

        public override void OnHitNPC(NPC target, int damage, float knockback, bool crit)
        {
            target.AddBuff(BuffID.OnFire, 300);

            Vector2 endpoint = target.position;
            Player player = Main.player[Projectile.owner];
            float distance = Vector2.Distance(endpoint, player.position);
            float velocity = -8f;
            Vector2 speed = ((endpoint - player.position) / distance) * velocity;
            speed.X += Main.rand.Next(-1, 1);
            speed.Y += Main.rand.Next(-1, 1);
            int dust = Dust.NewDust(endpoint, 3, 3, 127, speed.X + Main.rand.Next(-10, 10), speed.Y + Main.rand.Next(-10, 10), 20, default, 3.0f);
            Main.dust[dust].noGravity = true;
            dust = Dust.NewDust(endpoint, 3, 3, 130, speed.X, speed.Y, 20, default, 1.0f);
            Main.dust[dust].noGravity = true;
            Main.dust[dust].shader = GameShaders.Armor.GetSecondaryShader(107, Main.LocalPlayer);
            dust = Dust.NewDust(endpoint, 30, 30, 130, Main.rand.Next(-10, 10), Main.rand.Next(-10, 10), 20, default, 1.0f);
            Main.dust[dust].noGravity = true;            
        }

        private float HostGlaiveBeamIndex
        {
            get => Projectile.ai[1];
            set => Projectile.ai[1] = value;
        }

        public override void AI() { 

            FiringTimeLeft--;
            if (FiringTimeLeft == 0)
            {
                Charge = 0;
            }

            Player player = Main.player[Projectile.owner];
            Vector2 origin = GetOrigin();
            Projectile.position = origin + Projectile.velocity * MOVE_DISTANCE;
            Projectile.timeLeft = 2;

            UpdatePlayer(player);
            ChargeLaser(player); 

            if (Charge < MAX_CHARGE) return;

            SetLaserPosition(player);
            Vector2 endpoint = origin + Projectile.velocity * Distance;
            endpoint -= new Vector2(8, 8); //Offset endpoint so dust is centered on it, covering it better
            float distance = Vector2.Distance(endpoint, origin);
            float velocity = -10f;
            Vector2 speed = ((endpoint - origin) / distance) * velocity;
            speed.X += Main.rand.Next(-1, 1);
            speed.Y += Main.rand.Next(-1, 1);
            int dust;
            for (int i = 0; i < 8; i++)
            {
                dust = Dust.NewDust(endpoint, 16, 16, 127, speed.X * 1.2f + Main.rand.Next(-5, 5), speed.Y * 1.2f + Main.rand.Next(-5, 5), 20, default, 3.5f);
                Main.dust[dust].noGravity = true;
            }            
            if (Main.rand.Next(2) == 1)
            {
                dust = Dust.NewDust(endpoint, 16, 16, 130, speed.X, speed.Y, 20, default, 1.0f);
                Main.dust[dust].noGravity = true;
                Main.dust[dust].shader = GameShaders.Armor.GetSecondaryShader(107, Main.LocalPlayer);
            }
            if (Main.rand.Next(3) == 1)
            {
                dust = Dust.NewDust(endpoint, 30, 30, 130, Main.rand.Next(-10, 10), Main.rand.Next(-10, 10), 20, default, 1.0f);
                Main.dust[dust].noGravity = true;
            }

            // If the game is rendering (i.e. isn't a dedicated server), make the beam disturb water.
            //if (Main.netMode != NetmodeID.Server)
            //{
            // ProduceWaterRipples(beamDims);
            //}
        }

        private void SetLaserPosition(Player player) {
            for (Distance = MOVE_DISTANCE; Distance <= 2200f; Distance += 5f)
            {
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
            float waveSine = 0.1f * (float)Math.Sin(Main.GlobalTime * 20f);
            Vector2 ripplePos = Projectile.position + new Vector2(beamDims.X * 0.5f, 0f).RotatedBy(Projectile.rotation);

            // WaveData is encoded as a Color. Not really sure why.
            Color waveData = new Color(0.5f, 0.1f * Math.Sign(waveSine) + 0.5f, 0f, 1f) * Math.Abs(waveSine);
            shaderData.QueueRipple(ripplePos, waveData, beamDims, RippleShape.Square, Projectile.rotation);
        }

        private void ChargeLaser(Player player) {
            if (!player.channel) {
                Projectile.Kill();
            }
            else {
                Vector2 offset = Projectile.velocity;
                offset *= MOVE_DISTANCE - 20;
                Vector2 pos = GetOrigin() + offset - new Vector2(10, 10);
                if (Charge < MAX_CHARGE) {
                    Charge++;
                    //Only play the sound once, on the frame it hits max charge
                    if(Charge == MAX_CHARGE)
                    {
                        Main.PlaySound(Mod.GetLegacySoundSlot(SoundType.Item, "Sounds/Item/GlaiveBeam").WithVolume(50f));
                        FiringTimeLeft = FIRING_TIME;
                    }
                }
                Vector2 dustVelocity = Vector2.UnitX * 18f;
                dustVelocity = dustVelocity.RotatedBy(Projectile.rotation - 1.57f);
            }
        }

        private void UpdatePlayer(Player player) {
            // Multiplayer support here, only run this code if the client running it is the owner of the projectile
            if (Projectile.owner == Main.myPlayer) {
                Vector2 diff = Main.MouseWorld - GetOrigin();
                diff.Normalize();
                Projectile.velocity = diff;
                Projectile.direction = Main.MouseWorld.X > player.position.X ? 1 : -1;
                Projectile.netUpdate = true;
            }

            int dir = Projectile.direction;
            player.ChangeDir(dir); // Set player direction to where we are shooting
            player.heldProj = Projectile.whoAmI; // Update player's held projectile
            player.itemTime = 2; // Set item time to 2 frames while we are used
            player.itemAnimation = 2; // Set item animation time to 2 frames while we are used
            player.itemRotation = (float)Math.Atan2(Projectile.velocity.Y * dir, Projectile.velocity.X * dir); // Set the item rotation to where we are shooting
        }

        private void CastLights() {
            // Cast a light along the line of the laser
            DelegateMethods.v3_1 = new Vector3(1f, 0f, 0f);
            Utils.PlotTileLine(Projectile.Center, Projectile.Center + Projectile.velocity * (Distance - MOVE_DISTANCE), 8, DelegateMethods.CastLight);
        }

        public override bool ShouldUpdatePosition() => false;

        public override void CutTiles() {
            if (Charge == MAX_CHARGE)
            {
                DelegateMethods.tilecut_0 = TileCuttingContext.AttackProjectile;
                Vector2 unit = Projectile.velocity;
                Utils.PlotTileLine(Projectile.Center, Projectile.Center + unit * Distance, (Projectile.width + 16) * Projectile.scale, DelegateMethods.CutTiles);
            }
        }
    }
}

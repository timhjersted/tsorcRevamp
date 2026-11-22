using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Enums;
using Terraria.GameContent.Shaders;
using Terraria.Graphics.Effects;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;
using tsorcRevamp.NPCs.FriendlyPuppets;

namespace tsorcRevamp.Content.Projectiles
{

    public class GlaiveBeamLaser : ModProjectile
    {

        //Fair warning: This is an absolute garbled mess right now, mostly as the result of experimentation.
        //Expect it to look pretty different in the future.

        public static float MAX_CHARGE = GlaiveBeamHoldout.MaxCharge;
        private const float MOVE_DISTANCE = 20f;
        public const int FIRING_TIME = 120;

        public float Distance
        {
            get => Projectile.ai[0];
            set => Projectile.ai[0] = value;
        }

        public float Charge
        {
            get => Projectile.localAI[0];
            set => Projectile.localAI[0] = value;
        }

        public bool IsAtMaxCharge => Charge == MAX_CHARGE;

        public int FiringTimeLeft = 0;

        public override void SetDefaults()
        {
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.damage = 25;
        }


        private Vector2 GetOrigin()
        {
            Player player = Main.player[Projectile.owner];
            bool fromPuppet = Projectile.GetGlobalProjectile<MirrorPuppetProjectile>().FromMirrorPuppet;
            int facing = player.direction;
            float itemRotation = player.itemRotation;
            bool inverted = !fromPuppet && player.gravDir != 1 && ModContent.GetInstance<tsorcRevampConfig>().GravityFix;

            // Match the firing pose on every peer. Puppet draw and collision can't read the borrowed player's rotation.
            if (fromPuppet)
            {
                facing = Projectile.velocity.X < 0f ? -1 : 1;
                Vector2 itemDirection = Projectile.velocity * facing;
                itemRotation = itemDirection.ToRotation();
            }

            Vector2 origin = MirrorPuppetProjectile.GetOwnerVisualCenter(Projectile);
            origin.X -= 5 * facing;
            origin.Y += inverted ? 15f : -15f;
            float angle = Math.Abs(itemRotation);
            float cosine = (float)Math.Cos(angle);
            float sine = (float)Math.Sin(angle);

            if (inverted)
            {
                if (itemRotation < 0)
                {
                    if (facing == 1)
                    {
                        origin.X += 8 - 8 * cosine;
                    }
                    else
                    {
                        origin.X += 10 * cosine;
                        origin.Y -= 10 * sine;
                    }
                }
                else if (facing == 1)
                {
                    origin.Y -= 15 * sine;
                }
                else
                {
                    origin.X -= 5 - 5 * cosine;
                }

                return origin;
            }

            if (itemRotation < 0)
            {
                if (facing == 1)
                {
                    origin.X -= 10 * cosine;
                    origin.Y += 10 * sine;
                }
                else
                {
                    origin.X -= 5 - 10 * cosine;
                    origin.Y -= 10 * sine;
                }
            }
            else if (facing == 1)
            {
                origin.X += 6 - 11 * cosine;
                origin.Y -= 10 * sine;
            }
            else
            {
                origin.X += 10 * cosine;
                origin.Y += 10 * sine;
            }

            return origin;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            //Get the premultiplied, properly transparent texture
            Texture2D texture = TransparentTextureHandler.TransparentTextures[TransparentTextureHandler.TransparentTextureType.GlaiveBeam];

            if (IsAtMaxCharge && FiringTimeLeft > 0)
            {
                float scale = 0.4f;

                DrawLaser(Main.spriteBatch, texture, GetOrigin(),
                    Projectile.velocity, 28 * scale, Projectile.damage, -1.57f, scale, 2000f, Color.White, (int)MOVE_DISTANCE);
            }
            return false;
        }

        public void DrawLaser(SpriteBatch spriteBatch, Texture2D texture, Vector2 start, Vector2 unit, float step, int damage, float rotation = 0f, float scale = 1f, float maxDist = 2000f, Color color = default, int transDist = 50)
        {
            float r = unit.ToRotation() + rotation;

            // Draws the laser 'body'
            for (float i = transDist; i <= Distance; i += step)
            {

                Color c = Color.White;
                var origin = start + i * unit;

                Main.EntitySpriteDraw(texture, origin - Main.screenPosition, new Rectangle(0, 0, 46, 28), i < transDist ? Color.Transparent : c, r, new Vector2(46 * .5f, 28 * .5f), scale, 0, 0);

                //If it's at the end, draws the end of the laser
                if ((i + step) > Distance)
                {
                    i += step;
                    origin = start + i * unit;
                    Main.EntitySpriteDraw(texture, origin - Main.screenPosition, new Rectangle(0, 30, 46, 28), i < transDist ? Color.Transparent : c, r, new Vector2(46 * .5f, 28 * .5f), scale, 0, 0);
                }
            }
            CastLights();
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (!IsAtMaxCharge || FiringTimeLeft <= 0)
            {
                return false;
            }

            Vector2 origin = GetOrigin();
            Vector2 end = origin + Projectile.velocity * Distance;
            float point = 0f;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), origin, end, 22, ref point);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.Daybreak, 300);

            if (Main.dedServ)
            {
                return;
            }

            Vector2 endpoint = target.position;
            Vector2 towardShooter = GetOrigin() - endpoint;
            Vector2 speed = towardShooter.SafeNormalize(Vector2.Zero) * 8f;
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

        public override void AI()
        {
            MirrorPuppetProjectile tag = Projectile.GetGlobalProjectile<MirrorPuppetProjectile>();
            bool fromPuppet = tag.FromMirrorPuppet;
            bool remotePuppetBeam = fromPuppet && Projectile.owner != Main.myPlayer;

            if (fromPuppet && tag.GetFiringPuppet(Projectile) == null)
            {
                return;
            }

            Player player = Main.player[Projectile.owner];
            bool puppetReleased = fromPuppet && (!MirrorPuppetSwap.Active || !player.controlUseItem);

            if (!remotePuppetBeam && (!player.channel || puppetReleased))
            {
                Projectile.Kill();
                return;
            }

            FiringTimeLeft--;
            if (FiringTimeLeft == 0)
            {
                Charge = 0;
            }

            if (!remotePuppetBeam)
            {
                UpdatePlayer(player, fromPuppet);
            }

            Vector2 origin = GetOrigin();
            Projectile.position = origin + Projectile.velocity * MOVE_DISTANCE;
            Projectile.timeLeft = 2;

            if (Charge < MAX_CHARGE)
            {
                Charge++;

                if (!IsAtMaxCharge)
                {
                    return;
                }

                FiringTimeLeft = FIRING_TIME;

                if (!Main.dedServ)
                {
                    Terraria.Audio.SoundEngine.PlaySound(new Terraria.Audio.SoundStyle("tsorcRevamp/Sounds/Item/GlaiveBeam") with { Volume = 50f, PitchVariance = 0.1f }, Projectile.Center);
                }
            }

            SetLaserPosition(origin);

            if (Main.dedServ)
            {
                return;
            }

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
            if (Main.rand.NextBool(2))
            {
                dust = Dust.NewDust(endpoint, 16, 16, 130, speed.X, speed.Y, 20, default, 1.0f);
                Main.dust[dust].noGravity = true;
                Main.dust[dust].shader = GameShaders.Armor.GetSecondaryShader(107, Main.LocalPlayer);
            }
            if (Main.rand.NextBool(3))
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

        private void SetLaserPosition(Vector2 origin)
        {
            for (Distance = MOVE_DISTANCE; Distance <= 2200f; Distance += 5f)
            {
                Vector2 end = origin + Projectile.velocity * Distance;

                if (!Collision.CanHit(origin, 1, 1, end, 1, 1) && !Collision.CanHitLine(origin, 1, 1, end, 1, 1))
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

        private void UpdatePlayer(Player player, bool fromPuppet)
        {
            // Multiplayer support here, only run this code if the client running it is the owner of the projectile
            if (Projectile.owner == Main.myPlayer)
            {
                Vector2 aim = Main.MouseWorld - GetOrigin();
                Projectile.velocity = aim.SafeNormalize(Projectile.velocity);
                Projectile.direction = Main.MouseWorld.X > player.position.X ? 1 : -1;
                Projectile.netUpdate = true;
            }

            int facing = Projectile.direction;
            player.ChangeDir(facing);

            if (!fromPuppet)
            {
                player.heldProj = Projectile.whoAmI;
            }

            player.itemTime = 2;
            player.itemAnimation = 2;
            Vector2 itemDirection = Projectile.velocity * facing;
            player.itemRotation = itemDirection.ToRotation();
        }

        private void CastLights()
        {
            // Cast a light along the line of the laser
            DelegateMethods.v3_1 = new Vector3(1f, 0f, 0f);
            Utils.PlotTileLine(Projectile.Center, Projectile.Center + Projectile.velocity * (Distance - MOVE_DISTANCE), 8, DelegateMethods.CastLight);
        }

        public override bool ShouldUpdatePosition() => false;

        public override void CutTiles()
        {
            if (Charge == MAX_CHARGE)
            {
                DelegateMethods.tilecut_0 = TileCuttingContext.AttackProjectile;
                DelegateMethods.tileCutIgnore = TileID.Sets.TileCutIgnore.None;
                Vector2 unit = Projectile.velocity;
                Utils.PlotTileLine(Projectile.Center, Projectile.Center + unit * Distance, (Projectile.width + 16) * Projectile.scale, DelegateMethods.CutTiles);
            }
        }
    }
}

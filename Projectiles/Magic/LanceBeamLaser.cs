using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Magic
{

    public class LanceBeamLaser : GenericLaser
    {
        public override string Texture => "tsorcRevamp/Projectiles/Enemy/Okiku/PoisonSmog";

        public override void SetDefaults()
        {
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.friendly = false;
            Projectile.hostile = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.timeLeft = 999;

            FollowHost = true;
            LaserOrigin = Main.npc[HostIdentifier].Center;
            FiringDuration = 505;
            TelegraphTime = 60;
            MaxCharge = 60;
            LaserLength = 5000;
            LaserColor = Color.Red;
            TileCollide = false;
            LaserSize = 1.3f;
            LaserTexture = TransparentTextureHandler.TransparentTextureType.RedLaserTransparent;
            LaserTextureHead = new Rectangle(0, 0, 30, 24);
            LaserTextureBody = new Rectangle(0, 26, 30, 30);
            LaserTextureTail = new Rectangle(0, 58, 30, 24);
            LaserSound = SoundID.Item12 with { Volume = 0.5f };
            LaserDebuffs = new List<int>(); 
            DebuffTimers = new List<int>();
            LaserDebuffs.Add(BuffID.OnFire);
            DebuffTimers.Add(300);
            LaserName = "Lance Beam";
            LineDust = true;
            LaserDust = DustID.OrangeTorch;
            CastLight = true;
            Additive = true;
        }
        Player owner
        {
            get
            {
                return Main.player[Projectile.owner];
            }
        }
        public override void AI()
        {

            base.AI();

            if(owner == null || owner.dead)
            {
                Projectile.Kill();
                return;
            }

            if (!owner.channel || owner.noItems || owner.CCed || owner.statMana <= 0)
            {
                if (Projectile.timeLeft > 30)
                {
                    Projectile.timeLeft = 30;
                }
            }
            else
            {
                Projectile.timeLeft++;

                if (FiringTimeLeft > 0)
                {
                    FiringTimeLeft++;
                }
                if (Main.GameUpdateCount % 2 == 0)
                {
                    owner.statMana--;
                }
                owner.manaRegenDelay = 180;
            }

            if (owner != null && !owner.dead && owner.whoAmI == Main.myPlayer)
            {
                Projectile.velocity = UsefulFunctions.Aim(Projectile.Center, Main.MouseWorld, 1);
                Projectile.Center += Projectile.velocity * 30;
            }
        }

        public override Vector2 GetOrigin()
        {
            if (owner != null && !owner.dead)
            {
                return owner.Center;
            }
            else
            {
                Projectile.Kill();
                return Vector2.Zero;
            }
        }
        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(BuffID.OnFire, 300);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.OnFire, 300);
        }

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
    }
}

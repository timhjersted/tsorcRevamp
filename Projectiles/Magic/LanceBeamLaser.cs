using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Graphics.Shaders;
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
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.penetrate = -1;
            Projectile.tileCollide = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.timeLeft = 999;

            FollowHost = true;
            LaserOrigin = Main.npc[HostIdentifier].Center;
            FiringDuration = 505;
            TelegraphTime = 60;
            MaxCharge = 60;
            LaserLength = 5000;
            LaserColor = Color.Red;
            TileCollide = true;
            LaserSize = 1.0f;
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
            LineDust = false;
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

            if (IsAtMaxCharge)
            {
                //Drain BotC players stamina
                if (owner.GetModPlayer<tsorcRevampPlayer>().BearerOfTheCurse)
                {
                    owner.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceCurrent -= 1;
                }

                Vector2 endpoint = GetOrigin() + Projectile.velocity * Distance;
                endpoint -= new Vector2(8, 8); //Offset endpoint so dust is centered on it, covering it better
                float distance = Vector2.Distance(endpoint, GetOrigin());
                float velocity = -10f;
                Vector2 speed = ((endpoint - GetOrigin()) / distance) * velocity;
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
            }

            if (owner == null || owner.dead)
            {
                Projectile.Kill();
                return;
            }

            if (FiringTimeLeft > 0 && Main.GameUpdateCount % 9 == 0)
            {
                Terraria.Audio.SoundEngine.PlaySound(new Terraria.Audio.SoundStyle("tsorcRevamp/Sounds/Custom/LaserLoopable") with { Volume = 0.5f }, Projectile.Center);
            }

            if (owner.statMana <= 0 && !owner.GetModPlayer<tsorcRevampCeruleanPlayer>().isCeruleanRestoring && !owner.GetModPlayer<tsorcRevampCeruleanPlayer>().isDrinking)
            {
                MethodSwaps.TryUseQuickMana(owner);
            }

            if (!owner.channel || owner.noItems || owner.CCed || owner.statMana <= 0 || owner.GetModPlayer<tsorcRevampStaminaPlayer>().staminaResourceCurrent <= 0)
            {
                Projectile.damage = 0;

                if (IsAtMaxCharge)
                {
                    Terraria.Audio.SoundEngine.PlaySound(new Terraria.Audio.SoundStyle("tsorcRevamp/Sounds/Custom/LaserFadeout") with { Volume = 0.5f }, Projectile.Center);
                }
                Projectile.timeLeft--;
                if (Projectile.timeLeft > 10)
                {
                    Projectile.timeLeft = 10;
                }
                if (FiringTimeLeft > 10)
                {
                    FiringTimeLeft = 10;
                }
            }
            else
            {
                Projectile.timeLeft++;
                owner.manaRegenDelay = 180;
                Projectile.rotation = UsefulFunctions.Aim(owner.Center, Main.MouseWorld, 1).ToRotation();
                Projectile.direction = Main.MouseWorld.X > owner.Center.X ? 1 : -1;
                Vector2 rotDir = Projectile.rotation.ToRotationVector2();
                if (Projectile.direction == -1)
                {
                    rotDir *= -1;
                }
                owner.itemRotation = rotDir.ToRotation();
                owner.ChangeDir(Projectile.direction);
                owner.itemTime = 2; // Set item time to 2 frames while we are used
                owner.itemAnimation = 2; // Set item animation time to 2 frames while we are used

                if (FiringTimeLeft > 0)
                {
                    FiringTimeLeft++;
                    if (Main.GameUpdateCount % 2 == 0)
                    {
                        owner.statMana--;
                    }
                }
            }

            if (owner != null && !owner.dead && owner.whoAmI == Main.myPlayer)
            {
                Projectile.velocity = UsefulFunctions.Aim(owner.Center, Main.MouseWorld, 1);
            }
        }

        public override Vector2 GetOrigin()
        {
            if (owner != null && !owner.dead)
            {
                return owner.Center + new Vector2(32, 0).RotatedBy(owner.itemRotation - MathHelper.PiOver4);
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

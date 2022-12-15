using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy.Triplets
{
    public class RetOmegaLaser : ModProjectile
    {

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.DrawScreenCheckFluff[Projectile.type] = 99999999;
            DisplayName.SetDefault("Incinerating Gaze");
        }
        public override string Texture => "tsorcRevamp/Projectiles/Enemy/Triplets/HomingStarStar";

        public override void SetDefaults()
        {

            Projectile.friendly = false;
            Projectile.hostile = true;
            Projectile.penetrate = 50;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = 260;
            Projectile.width = 10;
            Projectile.height = 250;
        }

        float chargeProgress;
        float laserWidth = 250;
        public override void AI()
        {
            if(chargeProgress < 60)
            {
                chargeProgress++;
            }
            else
            {
                laserWidth += 60;
            }

            if(chargeProgress >= 59 && Main.GameUpdateCount % 8 == 0 && Projectile.timeLeft > 60)
            {
                Terraria.Audio.SoundEngine.PlaySound(new Terraria.Audio.SoundStyle("tsorcRevamp/Sounds/Item/MasterBuster") with { Volume = 50f, Pitch = 0.1f }, Projectile.Center);
            }

            if (Main.npc[(int)Projectile.ai[0]] != null && Main.npc[(int)Projectile.ai[0]].active && Main.npc[(int)Projectile.ai[0]].type == ModContent.NPCType<NPCs.Bosses.RetinazerV2>())
            {
                Projectile.rotation = Main.npc[(int)Projectile.ai[0]].rotation + MathHelper.PiOver2;
                Projectile.Center = Main.npc[(int)Projectile.ai[0]].Center + new Vector2(40, 0).RotatedBy(Projectile.rotation);
            }
            else if (Projectile.velocity != Vector2.Zero)
            {
                Projectile.rotation = Projectile.velocity.ToRotation();
                Projectile.velocity = Vector2.Zero;
            }

            //Cast light
            Vector3 colorVector = Color.OrangeRed.ToVector3() * 2f;
            if(chargeProgress < 60)
            {
                colorVector *= chargeProgress / 60f;
            }
            if(Projectile.timeLeft < 60)
            {
                colorVector *= Projectile.timeLeft / 60f;
            }
            DelegateMethods.v3_1 = colorVector;
            Vector2 startPoint = Projectile.Center;
            Vector2 endpoint = Projectile.Center + Projectile.rotation.ToRotationVector2() * laserWidth;
            if (Projectile.ai[0] == 0)
            {
                startPoint.Y -= Projectile.height / 2;
                endpoint.Y += Projectile.height;
            }
            else
            {
                startPoint.X -= Projectile.width / 2;
                endpoint.X += Projectile.width / 2;
            }
            Utils.PlotTileLine(startPoint, endpoint, 16, DelegateMethods.CastLight);


            //Collision
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                if (Projectile.Hitbox.Contains(Main.player[i].Center.ToPoint()))
                {
                    Main.player[i].statLife -= 5;
                    CombatText.NewText(Main.player[i].Hitbox, Color.Red, 5);
                    if (Main.player[i].statLife < 1)
                    {
                        Main.player[i].statLife = 1;
                        Main.player[i].immune = false;
                        Main.player[i].immuneTime = 0;
                    }
                }
            }
        }

        public static Texture2D flameJetTexture;
        public static ArmorShaderData data;
        float modifiedTime;
        public override bool PreDraw(ref Color lightColor)
        {
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            //Apply the shader, caching it as well
            //if (data == null)
            {
                data = new ArmorShaderData(new Ref<Effect>(ModContent.Request<Effect>("tsorcRevamp/Effects/ScreenFilters/IncineratingGaze", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value), "IncineratingGazePass");
            }

            if (flameJetTexture == null || flameJetTexture.IsDisposed)
            {
                flameJetTexture = (Texture2D)ModContent.Request<Texture2D>("tsorcRevamp/Projectiles/Enemy/Marilith/CataclysmicFirestorm", ReLogic.Content.AssetRequestMode.ImmediateLoad);
            }

            //Pass relevant data to the shader via these parameters
            data.UseSaturation(modifiedTime);            
            data.UseTargetPosition(new Vector2(laserWidth, 250));
            float scaleDown = 1;
            if (Projectile.timeLeft < 60)
            {
                scaleDown = Projectile.timeLeft / 60f;

            }
            if(chargeProgress < 60)
            {
                scaleDown = (float)Math.Pow(chargeProgress / 60f, 0.2f) / 1.1f;
            }
            else
            {
                modifiedTime++;
            }


            data.UseOpacity(scaleDown);
            //data.UseSecondaryColor(1, 1, Main.time);

            //Apply the shader
            data.Apply(null);

            SpriteEffects spriteEffects = SpriteEffects.None;
            if (Projectile.spriteDirection == -1)
            {
                spriteEffects = SpriteEffects.FlipHorizontally;
            }
            Rectangle sourceRectangle = new Rectangle(0, 0, (int)laserWidth, Projectile.height);
            Vector2 origin = new Vector2(0, sourceRectangle.Height / 2);

            Main.EntitySpriteDraw(flameJetTexture, Projectile.Center - Main.screenPosition, sourceRectangle, Color.White, Projectile.rotation, origin, Projectile.scale, spriteEffects, 0);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            return false;
        }
    }
}

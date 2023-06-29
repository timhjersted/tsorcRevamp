using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy.Prime
{
    public class IonBomb : ModProjectile
    {

        public override void SetDefaults()
        {
            Projectile.aiStyle = 0;
            Projectile.height = 16;
            Projectile.scale = 1.2f;
            Projectile.tileCollide = false;
            Projectile.width = 16;
            Projectile.timeLeft = DetonationTime;
            Projectile.hostile = true;
        }

        float DetonationRange = 80;
        int DetonationTime = 240;
        float DetonationProgress = 0;
        bool spawnedLasers = false;
        const int LASER_COUNT = 6;
        int[] pickedDirections = new int[LASER_COUNT];

        float size = 200;
        float maxSize = 200;
        float detonationPercent
        {
            get =>  1f - (DetonationProgress / DetonationTime);
        }
        float easeInOutQuad(float x)
        {
            return x < 0.5 ? 2 * x * x : 1 - (float)Math.Pow(-2 * x + 2, 2) / 2;
        }
        float easeOutQuad(float x)
        {
            return x < 0.5 ? 2 * x * x : 1 - (float)Math.Pow(-2 * x + 2, 14) / 2;
        }

        float growRate;
        float shrinkRate;
        float sizeChange = 0;
        Vector2 newCenter;
        public override void AI()
        {
            Projectile.velocity *= 0.95f;
            DetonationProgress++;
            Projectile.rotation++;
            size -= sizeChange;
            sizeChange += 0.01f;

            if(DetonationProgress == 30 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Projectile.NewProjectileDirect(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero, ModContent.ProjectileType<Projectiles.VFX.ShockwaveEffect>(), 0, 0, Main.myPlayer, 240, 25);
                float randomRadius = 200;

                if(Projectile.ai[1] == 1)
                {
                    randomRadius = 400;
                }
                else if (Projectile.ai[1] == 2)
                {
                    randomRadius = 0;
                }

                newCenter = Main.player[(int)Projectile.ai[0]].Center + Main.rand.NextVector2CircularEdge(randomRadius, randomRadius);
                Projectile.NewProjectileDirect(Projectile.GetSource_FromThis(), newCenter, Vector2.Zero, ModContent.ProjectileType<Projectiles.VFX.ShockwaveEffect>(), 0, 0, Main.myPlayer, 120, 25);
                Projectile.NewProjectileDirect(Projectile.GetSource_FromThis(), newCenter, Vector2.Zero, ModContent.ProjectileType<Projectiles.VFX.ShockwaveEffect>(), 0, 0, Main.myPlayer, 240, 25);
            }

            if (DetonationProgress == 40 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Projectile.Center = newCenter;
                Projectile.netUpdate = true;
            }
        }


        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if(Projectile.Distance(targetHitbox.Center.ToVector2()) < 230 && DetonationTime == DetonationProgress + 1)
            {
                return true;
            }
            return false;
        }

        
        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(BuffID.Electrified, 300);
        }


        public override bool PreKill(int timeLeft)
        {
            Terraria.Audio.SoundEngine.PlaySound(new Terraria.Audio.SoundStyle("tsorcRevamp/Sounds/Item/PulsarBoom"), Projectile.Center);

            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                Projectile.NewProjectileDirect(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero, ModContent.ProjectileType<Projectiles.VFX.ExplosionFlash>(), 0, 0, Main.myPlayer, 900, 45);
                Projectile.NewProjectileDirect(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero, ModContent.ProjectileType<Projectiles.VFX.ExplosionFlash>(), 0, 0, Main.myPlayer, 600, 30);
                Projectile.NewProjectileDirect(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero, ModContent.ProjectileType<Projectiles.VFX.ExplosionFlash>(), 0, 0, Main.myPlayer, 300, 20);

                float rotation = Main.rand.NextFloat(0, MathHelper.TwoPi);
                for (int i = 0; i < 4; i++)
                {
                    Projectile.NewProjectileDirect(Projectile.GetSource_FromThis(), Projectile.Center, new Vector2(12, 0).RotatedBy(rotation + (MathHelper.PiOver2 / 4f) + i * MathHelper.TwoPi / 4f), ModContent.ProjectileType<Marilith.MarilithLightning>(), Projectile.damage / 2, .5f, Main.myPlayer);
                }
            }

            for (int i = 0; i < 50; i++)
            {
                Vector2 dustVel = Main.rand.NextVector2Circular(28, 28);
                Dust.NewDustPerfect(Projectile.Center, DustID.FireworkFountain_Blue, dustVel, 200, Color.White, 1.3f).noGravity = true;
            }
            return true;
        }

        float effectTimer;
        float starRotation;
        public static Effect RingEffect;
        public static Effect CoreEffect;
        public override bool PreDraw(ref Color lightColor)
        {
            Vector3 hslColor1 = Main.rgbToHsl(Color.Cyan);
            Vector3 hslColor2 = Main.rgbToHsl(Color.Navy);
            hslColor1.X += 0.03f * (float)Math.Cos(effectTimer / 25f);
            hslColor2.X += 0.03f * (float)Math.Cos(effectTimer / 25f);
            effectTimer++;
            Color rgbColor1 = Main.hslToRgb(hslColor1);
            Color rgbColor2 = Main.hslToRgb(hslColor2);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            //Cache shaders
            if (CoreEffect == null)
            {
                CoreEffect = ModContent.Request<Effect>("tsorcRevamp/Effects/CatFinalStandAttack", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
            }

            starRotation += 0.02f;
            Rectangle starRectangle = new Rectangle(0, 0, 600, 600);
            float attackFadePercent = (float)Math.Pow(detonationPercent, .2) / 2f;
            starRectangle.Width = (int)(starRectangle.Width * (Math.Pow(detonationPercent, .3)));
            starRectangle.Height = (int)(starRectangle.Height * (Math.Pow(detonationPercent, .3)));

            Vector2 starOrigin = starRectangle.Size() / 2f;

            //Pass relevant data to the shader via these parameters
            CoreEffect.Parameters["textureSize"].SetValue(tsorcRevamp.NoiseWavy.Width);
            CoreEffect.Parameters["effectSize"].SetValue(starRectangle.Size());
            CoreEffect.Parameters["effectColor"].SetValue(rgbColor1.ToVector4());
            CoreEffect.Parameters["ringProgress"].SetValue(0.5f);
            CoreEffect.Parameters["fadePercent"].SetValue(attackFadePercent);
            CoreEffect.Parameters["time"].SetValue(-Main.GlobalTimeWrappedHourly * 3f);

            //Apply the shader
            CoreEffect.CurrentTechnique.Passes[0].Apply();

            Main.EntitySpriteDraw(tsorcRevamp.NoiseWavy, Projectile.Center - Main.screenPosition, starRectangle, Color.White, starRotation, starOrigin, 1, SpriteEffects.None, 0);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            //Pass relevant data to the shader via these parameters
            CoreEffect.Parameters["textureSize"].SetValue(tsorcRevamp.NoiseWavy.Width);
            CoreEffect.Parameters["effectSize"].SetValue(starRectangle.Size());
            CoreEffect.Parameters["effectColor"].SetValue(rgbColor2.ToVector4());
            CoreEffect.Parameters["ringProgress"].SetValue(0.5f);
            CoreEffect.Parameters["fadePercent"].SetValue(attackFadePercent);
            CoreEffect.Parameters["time"].SetValue(-Main.GlobalTimeWrappedHourly * 3f);

            //Apply the shader
            CoreEffect.CurrentTechnique.Passes[0].Apply();

            Main.EntitySpriteDraw(tsorcRevamp.NoiseWavy, Projectile.Center - Main.screenPosition, starRectangle, Color.White, -starRotation, starOrigin, 1, SpriteEffects.None, 0);

            if (DetonationProgress > 45)
            {
                if (RingEffect == null)
                {
                    RingEffect = ModContent.Request<Effect>("tsorcRevamp/Effects/SimpleRing", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
                }
                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

                Rectangle ringRectangle = new Rectangle(0, 0, 1200, 1200);
                Vector2 ringOrigin = ringRectangle.Size() / 2f;

                float shaderAngle = MathHelper.Pi * 1.2f * ((float)DetonationProgress) / DetonationTime;
                float shaderRotation = 0;
                RingEffect.Parameters["textureToSizeRatio"].SetValue(tsorcRevamp.NoiseWavy.Size() / ringRectangle.Size());
                RingEffect.Parameters["shaderColor"].SetValue(Color.Blue.ToVector3());
                RingEffect.Parameters["splitAngle"].SetValue(shaderAngle);
                RingEffect.Parameters["rotation"].SetValue(shaderRotation);
                RingEffect.Parameters["length"].SetValue(.2f);
                RingEffect.Parameters["firstEdge"].SetValue(.25f);
                RingEffect.Parameters["secondEdge"].SetValue(.015f);

                //Precomputed
                RingEffect.Parameters["rotationMinusPI"].SetValue(shaderRotation - MathHelper.Pi);
                RingEffect.Parameters["splitAnglePlusRotationMinusPI"].SetValue(shaderRotation + shaderAngle - MathHelper.Pi);
                RingEffect.Parameters["RotationMinus2PIMinusSplitAngleMinusPI"].SetValue((shaderRotation - (MathHelper.TwoPi - shaderAngle)) - MathHelper.Pi);
                RingEffect.CurrentTechnique.Passes[0].Apply();

                Main.EntitySpriteDraw(tsorcRevamp.NoiseWavy, Projectile.Center - Main.screenPosition, ringRectangle, Color.White, MathHelper.PiOver2 - 0.35f, ringOrigin, 1, SpriteEffects.None);
                Main.EntitySpriteDraw(tsorcRevamp.NoiseWavy, Projectile.Center - Main.screenPosition, ringRectangle, Color.White, MathHelper.Pi + MathHelper.PiOver2 - 0.35f, ringOrigin, 1, SpriteEffects.None);
            }
            UsefulFunctions.RestartSpritebatch(ref Main.spriteBatch);
            return false;
        }
    }
}

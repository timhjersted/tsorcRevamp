
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Graphics.Effects;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.VFX
{
    class ShockwaveEffect : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("ShockwaveEffect");
        }

        public override string Texture => "tsorcRevamp/Projectiles/Enemy/Triad/HomingStarStar";

        public override void SetDefaults()
        {
            Projectile.friendly = true;
            Projectile.width = 48;
            Projectile.height = 62;
            Projectile.penetrate = -1;
            Projectile.scale = 1;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 999;
        }


        public string filterIndex;

        float effectTimer = 0;
        bool initialized = false;
        float effectSpeed = 2;
        float effectRadius = 1;
        public override void AI()
        {
            if (!initialized && Main.netMode != NetmodeID.Server)
            {
                int index = 0;
                do
                {
                    string currentIndex = "tsorcRevamp:shockwave" + index;

                    //If there is an unused loaded shader, then start using it instead of creating a new one
                    if (Filters.Scene[currentIndex] != null && !Filters.Scene[currentIndex].Active)
                    {
                        Filters.Scene.Activate(currentIndex, Projectile.Center).GetShader().UseTargetPosition(Projectile.Center);
                        filterIndex = currentIndex;
                        tsorcRevampWorld.boundShaders.Add(filterIndex);
                        initialized = true;
                        break;
                    }

                    //If we have reached the point no more entries exist, then create a new one
                    if (Filters.Scene[currentIndex] == null)
                    {
                        Filters.Scene[currentIndex] = new Filter(new ScreenShaderData(new Ref<Effect>(ModContent.Request<Effect>("tsorcRevamp/Effects/ScreenFilters/TriadShockwave", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value), "TriadShockwavePass").UseImage("Images/Misc/noise"), EffectPriority.VeryHigh);
                        filterIndex = currentIndex;
                        tsorcRevampWorld.boundShaders.Add(filterIndex);
                        initialized = true;
                        break;
                    }

                    //If more than 10 are already active at once, give up and just kill the shockwave instead of creating yet another one.
                    if (index >= 10)
                    {
                        initialized = true;
                        Projectile.Kill();
                        break;
                    }
                    index++;

                } while (index < 10);
            }

            if (filterIndex == null && Main.netMode != NetmodeID.Server)
            {
                Projectile.Kill();
                return;
            }

            Projectile.timeLeft++;
            effectTimer++;
            float maxRadius = Projectile.ai[0];
            float effectLimit = Projectile.ai[1];
            effectSpeed = 1 - (effectTimer / effectLimit);
            effectRadius += (maxRadius / effectLimit) * effectSpeed;
            if (effectSpeed < 0.1)
            {
                effectSpeed = 0.1f;
            }

            if (Main.netMode != NetmodeID.Server && !Filters.Scene[filterIndex].IsActive())
            {
                Filters.Scene.Activate(filterIndex, Projectile.Center).GetShader().UseTargetPosition(Projectile.Center);
            }

            if (Main.netMode != NetmodeID.Server && Filters.Scene[filterIndex].IsActive())
            {

                float progress = 0.00125f * effectRadius;
                float opacity = 1 - (float)Math.Pow(effectTimer / effectLimit, 0.0001);
                opacity *= 20048;
                if (opacity < 0)
                {
                    opacity = 0;
                }
                Filters.Scene[filterIndex].GetShader().UseTargetPosition(Projectile.Center).UseProgress(progress).UseOpacity(opacity).UseIntensity(0.00000000000001f).UseColor(Color.White.ToVector3());
            }

            if (effectTimer > effectLimit * 0.99f)
            {
                Projectile.Kill();
            }
        }
        public override bool PreDraw(ref Color lightColor)
        {
            return false;
        }

        public override bool PreKill(int timeLeft)
        {
            if (filterIndex != null)
            {
                if (Main.netMode != NetmodeID.Server && Filters.Scene[filterIndex].IsActive())
                {
                    Filters.Scene[filterIndex].Deactivate();
                    tsorcRevampWorld.boundShaders.Remove(filterIndex);
                }
            }
            return base.PreKill(timeLeft);
        }


        public static Effect lightEffect;
        float starRotation;
        public void DrawFlash()
        {

            Vector3 hslColor1 = Main.rgbToHsl(Color.White);
            Vector3 hslColor2 = Main.rgbToHsl(Color.White);
            if (Projectile.ai[0] == 2)
            {
                hslColor2 = Main.rgbToHsl(Color.Cyan);
            }
            hslColor1.X += 0.03f * (float)Math.Cos(effectTimer / 25f);
            hslColor2.X += 0.03f * (float)Math.Cos(effectTimer / 25f);
            Color rgbColor1 = Main.hslToRgb(hslColor1);
            Color rgbColor2 = Main.hslToRgb(hslColor2);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            //Apply the shader, caching it as well
            if (lightEffect == null)
            {
                lightEffect = ModContent.Request<Effect>("tsorcRevamp/Effects/CatFinalStandAttack", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
            }

            starRotation = 0;
            Rectangle starRectangle = new Rectangle(0, 0, 12048, 12048);
            if (Projectile.ai[0] == 1)
            {
                starRectangle.Width = (int)(starRectangle.Width * 0.5f);
                starRectangle.Height = (int)(starRectangle.Height * 0.5f);
            }
            float effectFactor = (float)Math.Pow((360f - effectTimer) / 360f, 14);
            starRectangle.Width = (int)(starRectangle.Width * effectFactor);
            starRectangle.Height = (int)(starRectangle.Height * effectFactor);

            Vector2 starOrigin = starRectangle.Size() / 2f;

            //Pass relevant data to the shader via these parameters
            lightEffect.Parameters["textureSize"].SetValue(tsorcRevamp.NoiseWavy.Width);
            lightEffect.Parameters["effectSize"].SetValue(starRectangle.Size());
            lightEffect.Parameters["effectColor"].SetValue(rgbColor1.ToVector4());
            lightEffect.Parameters["ringProgress"].SetValue(0.5f);
            lightEffect.Parameters["fadePercent"].SetValue(effectFactor);
            lightEffect.Parameters["time"].SetValue(-Main.GlobalTimeWrappedHourly * 3f);

            //Apply the shader
            lightEffect.CurrentTechnique.Passes[0].Apply();

            Main.EntitySpriteDraw(tsorcRevamp.NoiseWavy, Projectile.Center - Main.screenPosition, starRectangle, Color.White, starRotation, starOrigin, 1, SpriteEffects.None, 0);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            //Pass relevant data to the shader via these parameters
            lightEffect.Parameters["textureSize"].SetValue(tsorcRevamp.NoiseWavy.Width);
            lightEffect.Parameters["effectSize"].SetValue(starRectangle.Size());
            lightEffect.Parameters["effectColor"].SetValue(rgbColor2.ToVector4());
            lightEffect.Parameters["ringProgress"].SetValue(0.5f);
            lightEffect.Parameters["fadePercent"].SetValue(effectFactor);
            lightEffect.Parameters["time"].SetValue(-Main.GlobalTimeWrappedHourly * 3f);

            //Apply the shader
            lightEffect.CurrentTechnique.Passes[0].Apply();

            Main.EntitySpriteDraw(tsorcRevamp.NoiseWavy, Projectile.Center - Main.screenPosition, starRectangle, Color.White, -starRotation, starOrigin, 1, SpriteEffects.None, 0);



            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
        }

        public override bool? CanDamage()
        {
            return false;
        }
    }
}
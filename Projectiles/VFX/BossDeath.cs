
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
    class BossDeath : ModProjectile
    {
        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("TriadDeath");
        }
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
        Color AuraColor
        {
            get
            {
                return UsefulFunctions.ColorFromFloat(Projectile.ai[1]);
            }
        }

        string FilterID
        {
            get
            {
                return "tsorcRevamp:shockwave" + Projectile.whoAmI;
            }
        }

        float effectTimer = 0;
        public override void AI()
        {
            Projectile.timeLeft++;
            float effectLimit = 600;
            effectTimer++;

            //Faster if the 'startup' VFX
            if (Projectile.ai[0] == 0)
            {
                effectTimer += 3;
            }
            else
            {
                effectLimit *= 5;
            }

            if (Filters.Scene[FilterID] == null)
            {
                Filters.Scene[FilterID] = new Filter(new ScreenShaderData(ModContent.Request<Effect>("tsorcRevamp/Effects/ScreenFilters/TriadShockwave"), "TriadShockwavePass").UseImage("Images/Misc/noise"), EffectPriority.VeryHigh);
                tsorcRevampWorld.boundShaders.Add(FilterID);
            }

            if (Main.netMode != NetmodeID.Server && !Filters.Scene[FilterID].IsActive())
            {
                Filters.Scene.Activate(FilterID, Projectile.Center).GetShader().UseTargetPosition(Projectile.Center);
                if (!tsorcRevampWorld.boundShaders.Contains(FilterID))
                {
                    tsorcRevampWorld.boundShaders.Add(FilterID);
                }
            }

            if (Main.netMode != NetmodeID.Server && Filters.Scene[FilterID].IsActive())
            {
                float opacity = 2 * (1f - effectTimer / effectLimit);

                float progress = effectTimer / 100f;

                if (Projectile.ai[0] == 0)
                {
                    progress /= 3f;
                }
                else
                {
                    opacity *= 2;
                }
                Filters.Scene[FilterID].GetShader().UseTargetPosition(Projectile.Center).UseProgress(progress).UseOpacity(5 * opacity).UseIntensity(0.000001f).UseColor(AuraColor.ToVector3());
            }

            if (effectTimer > effectLimit)
            {
                if (Main.netMode != NetmodeID.Server && Filters.Scene[FilterID].IsActive())
                {
                    Filters.Scene[FilterID].Deactivate();
                }
                Projectile.Kill();
            }
        }

        public override bool PreKill(int timeLeft)
        {
            if (FilterID != null)
            {
                if (Main.netMode != NetmodeID.Server && Filters.Scene[FilterID].IsActive())
                {
                    Filters.Scene[FilterID].Deactivate();
                    tsorcRevampWorld.boundShaders.Remove(FilterID);
                }
            }

            return base.PreKill(timeLeft);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            DrawAura();
            if (effectTimer < 600)
            {
                DrawFlash();
            }
            return false;
        }

        public static Effect AuraEffect;
        public void DrawAura()
        {
            if (Projectile.ai[0] == 0)
            {
                return;
            }
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            //Apply the shader, caching it as well
            if (AuraEffect == null)
            {
                if (Projectile.ai[0] <= 1)
                {
                    AuraEffect = ModContent.Request<Effect>("tsorcRevamp/Effects/RetAura", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
                }
                if (Projectile.ai[0] == 3)
                {
                    AuraEffect = ModContent.Request<Effect>("tsorcRevamp/Effects/SpazAura", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
                }
                if (Projectile.ai[0] == 2)
                {
                    AuraEffect = ModContent.Request<Effect>("tsorcRevamp/Effects/CatAura", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
                }
                if (Projectile.ai[0] == 4)
                {
                    AuraEffect = ModContent.Request<Effect>("tsorcRevamp/Effects/PrimeAura", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
                }

                if (AuraEffect == null)
                {
                    Projectile.Kill();
                    return;
                }
            }

            Rectangle sourceRectangle = new Rectangle(0, 0, (int)(650 / 0.7f), (int)(650 / 0.7f));
            Vector2 origin = sourceRectangle.Size() / 2f;

            //Pass relevant data to the shader via these parameters
            AuraEffect.Parameters["textureSize"].SetValue(tsorcRevamp.NoiseWavy.Width);
            AuraEffect.Parameters["effectSize"].SetValue(sourceRectangle.Size());
            AuraEffect.Parameters["effectColor"].SetValue(AuraColor.ToVector3());
            AuraEffect.Parameters["ringProgress"].SetValue(effectTimer / 360f);
            AuraEffect.Parameters["fadePercent"].SetValue(effectTimer / 360f);
            AuraEffect.Parameters["time"].SetValue(Main.GlobalTimeWrappedHourly * 2.5f);

            //Apply the shader
            AuraEffect.CurrentTechnique.Passes[0].Apply();

            Main.EntitySpriteDraw(tsorcRevamp.NoiseWavy, Projectile.Center - Main.screenPosition, sourceRectangle, Color.White, 0, origin, 1, SpriteEffects.None, 0);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
        }

        public static Effect lightEffect;
        float starRotation;
        public void DrawFlash()
        {

            Vector3 hslColor1 = Main.rgbToHsl(AuraColor);
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
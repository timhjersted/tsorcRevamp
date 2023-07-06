
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.Graphics.Effects;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria.Graphics.Shaders;

namespace tsorcRevamp.Projectiles.VFX
{
    class TelegraphFlash : ModProjectile
    {
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


        float effectBaseSize = 200;
        float effectScale = 1;
        float effectTimer = 0;
        public override void AI()
        {
            effectBaseSize = 150;
            if (Projectile.ai[0] != 0)
            {
                effectBaseSize *= Projectile.ai[0];
                Projectile.ai[0] = 0;
            }

            effectScale = (float)Math.Pow(0.95f, (float)Math.Pow(effectTimer, 1.3f));

            Projectile.timeLeft++;
            effectTimer++;
            if (effectTimer > 25)
            {
                Projectile.Kill();
            }

        }

        public override bool PreDraw(ref Color lightColor)
        {
            DrawFlash();
            return false;
        }


        public static Effect lightEffect;
        float starRotation;
        public void DrawFlash()
        {
            Vector3 hslColor1 = Main.rgbToHsl(Color.White);
            Vector3 hslColor2 = Main.rgbToHsl(Color.Red);
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

            starRotation += 0.1f;
            Rectangle starRectangle = new Rectangle(0, 0, (int)(effectBaseSize * effectScale), (int)(effectBaseSize * effectScale));

            Vector2 starOrigin = starRectangle.Size() / 2f;

            //Pass relevant data to the shader via these parameters
            lightEffect.Parameters["textureSize"].SetValue(tsorcRevamp.NoiseWavy.Width);
            lightEffect.Parameters["effectSize"].SetValue(starRectangle.Size());
            lightEffect.Parameters["effectColor"].SetValue(rgbColor1.ToVector4());
            lightEffect.Parameters["ringProgress"].SetValue(0.5f);
            lightEffect.Parameters["fadePercent"].SetValue(effectScale);
            lightEffect.Parameters["time"].SetValue(-Main.GlobalTimeWrappedHourly * 3f);

            //Apply the shader
            lightEffect.CurrentTechnique.Passes[0].Apply();

            Main.EntitySpriteDraw(tsorcRevamp.NoiseWavy, Projectile.Center - Main.screenPosition, starRectangle, Color.White, starRotation, starOrigin, 1, SpriteEffects.None, 0);
            Main.EntitySpriteDraw(tsorcRevamp.NoiseWavy, Projectile.Center - Main.screenPosition, starRectangle, Color.White, -starRotation, starOrigin, 1, SpriteEffects.None, 0);



            //Do it all again for the other color
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            //Pass relevant data to the shader via these parameters
            lightEffect.Parameters["textureSize"].SetValue(tsorcRevamp.NoiseWavy.Width);
            lightEffect.Parameters["effectSize"].SetValue(starRectangle.Size());
            lightEffect.Parameters["effectColor"].SetValue(rgbColor2.ToVector4());
            lightEffect.Parameters["ringProgress"].SetValue(0.5f);
            lightEffect.Parameters["fadePercent"].SetValue(effectScale);
            lightEffect.Parameters["time"].SetValue(-Main.GlobalTimeWrappedHourly * 3f);

            //Apply the shader
            lightEffect.CurrentTechnique.Passes[0].Apply();

            Main.EntitySpriteDraw(tsorcRevamp.NoiseWavy, Projectile.Center - Main.screenPosition, starRectangle, Color.White, -starRotation, starOrigin, 1, SpriteEffects.None, 0);

            UsefulFunctions.RestartSpritebatch(ref Main.spriteBatch);
        }

        public override bool? CanDamage()
        {
            return false;
        }
    }
}
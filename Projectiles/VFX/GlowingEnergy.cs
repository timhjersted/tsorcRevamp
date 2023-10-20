
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.VFX
{
    class GlowingEnergy : ModProjectile
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


        public string filterIndex;

        float effectTimer = 0;
        float effectLimit = 200;
        float effectSpeed = 2;
        public override void AI()
        {
            Projectile.Center = Main.npc[(int)Projectile.ai[0]].Center;
            if (Main.GameUpdateCount % 4 == 0 && Main.netMode != NetmodeID.MultiplayerClient && effectTimer < 100)
            {
                Projectile.NewProjectileDirect(Projectile.GetSource_FromThis(), Projectile.Center, Main.npc[(int)Projectile.ai[0]].velocity, ModContent.ProjectileType<Projectiles.VFX.EnergyGathering>(), 0, 0, Main.myPlayer, Projectile.ai[0], Projectile.ai[1]);
            }
            Projectile.timeLeft++;
            effectTimer++;
            effectSpeed = 1 - (effectTimer / effectLimit);
            if (effectSpeed < 0.1)
            {
                effectSpeed = 0.1f;
            }

            if (effectTimer > effectLimit * 0.99f)
            {
                Projectile.Kill();
            }
        }
        public override bool PreDraw(ref Color lightColor)
        {
            DrawFlash();
            return false;
        }

        public override bool PreKill(int timeLeft)
        {
            return base.PreKill(timeLeft);
        }


        public static Effect lightEffect;
        float starRotation;
        public void DrawFlash()
        {
            Vector3 hslColor1 = Main.rgbToHsl(Color.White);
            Vector3 hslColor2 = Main.rgbToHsl(UsefulFunctions.ColorFromFloat(Projectile.ai[1]));
            if (Main.npc[(int)Projectile.ai[0]].type == ModContent.NPCType<NPCs.Bosses.TheRage>())
            {
                hslColor2 = Main.rgbToHsl(Color.OrangeRed);
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

            starRotation += 0.05f;
            Rectangle starRectangle = new Rectangle(0, 0, 550, 550);

            float effectFactor = (float)Math.Pow(effectTimer / effectLimit, 2);

            if (effectLimit - effectTimer < 30)
            {
                effectFactor = MathHelper.Lerp(0, effectFactor, (effectLimit - effectTimer) / 30f);
            }

            starRectangle.Width = (int)(starRectangle.Width * effectFactor);
            starRectangle.Height = (int)(starRectangle.Height * effectFactor);

            Vector2 starOrigin = starRectangle.Size() / 2f;

            //Pass relevant data to the shader via these parameters
            lightEffect.Parameters["textureSize"].SetValue(tsorcRevamp.NoiseWavy.Width);
            lightEffect.Parameters["effectSize"].SetValue(starRectangle.Size());
            lightEffect.Parameters["effectColor"].SetValue(rgbColor1.ToVector4());
            lightEffect.Parameters["ringProgress"].SetValue(0.5f);
            lightEffect.Parameters["fadePercent"].SetValue(effectTimer / effectLimit);
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
            lightEffect.Parameters["fadePercent"].SetValue(1 - (effectTimer / effectLimit));
            lightEffect.Parameters["time"].SetValue(Main.GlobalTimeWrappedHourly * 3f);

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
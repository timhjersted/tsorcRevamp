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
                newCenter = Main.player[(int)Projectile.ai[0]].Center + Main.rand.NextVector2CircularEdge(200, 200);
                Projectile.NewProjectileDirect(Projectile.GetSource_FromThis(), newCenter, Vector2.Zero, ModContent.ProjectileType<Projectiles.VFX.ShockwaveEffect>(), 0, 0, Main.myPlayer, 240, 25);
                Projectile.NewProjectileDirect(Projectile.GetSource_FromThis(), newCenter, Vector2.Zero, ModContent.ProjectileType<Projectiles.VFX.ShockwaveEffect>(), 0, 0, Main.myPlayer, 240, 25);
            }
            if (DetonationProgress == 40 && Main.netMode != NetmodeID.MultiplayerClient)
            {
                Projectile.Center = newCenter;
                Projectile.netUpdate = true;
            }

            if (DetonationProgress > 45)
            {
                UsefulFunctions.DustRing(Projectile.Center, 300, DustID.FireworkFountain_Blue, 1 + (int)(2 * detonationPercent * detonationPercent), 15 * detonationPercent * detonationPercent);
            }
        }


        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if(Projectile.Distance(targetHitbox.Center.ToVector2()) < 300 && DetonationTime == DetonationProgress + 1)
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
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                Projectile.NewProjectileDirect(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero, ModContent.ProjectileType<Projectiles.VFX.ShockwaveEffect>(), 10, 0, Main.myPlayer, 700, 60);
                Projectile.NewProjectileDirect(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero, ModContent.ProjectileType<Projectiles.VFX.ShockwaveEffect>(), 10, 0, Main.myPlayer, 400, 45);
                Projectile.NewProjectileDirect(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero, ModContent.ProjectileType<Projectiles.VFX.ShockwaveEffect>(), 10, 0, Main.myPlayer, 220, 35);

                float rotation = Main.rand.NextFloat(0, MathHelper.TwoPi);
                for (int i = 0; i < 7; i++)
                {
                    Projectile.NewProjectileDirect(Projectile.GetSource_FromThis(), Projectile.Center, new Vector2(12, 0).RotatedBy(rotation + (MathHelper.PiOver2 / 7f) + i * MathHelper.TwoPi / 7f), ModContent.ProjectileType<Marilith.MarilithLightning>(), Projectile.damage, .5f, Main.myPlayer);
                }
            }

            for (int i = 0; i < 50; i++)
            {
                Vector2 dustVel = Main.rand.NextVector2Circular(28, 28);
                Dust.NewDustPerfect(Projectile.Center, DustID.FireworkFountain_Blue, dustVel, 250, Color.White, 1.3f).noGravity = true;
            }
            return true;
        }

        float effectTimer;
        float starRotation;
        public static Effect CoreEffect;
        public static ArmorShaderData data;
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

            //Apply the shader, caching it as well
            //if (effect == null)
            {
                CoreEffect = ModContent.Request<Effect>("tsorcRevamp/Effects/CatFinalStandAttack", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
            }

            starRotation += 0.02f;
            Rectangle starRectangle = new Rectangle(0, 0, 400, 400);
            float attackFadePercent = (float)Math.Pow(detonationPercent, .2) / 2f;
            starRectangle.Width = 150 + (int)(starRectangle.Width * (1 - Math.Pow(detonationPercent, .3)));
            starRectangle.Height = 150 + (int)(starRectangle.Height * (1 - Math.Pow(detonationPercent, .3)));

            Vector2 starOrigin = starRectangle.Size() / 2f;

            //Pass relevant data to the shader via these parameters
            CoreEffect.Parameters["textureSize"].SetValue(tsorcRevamp.tNoiseTexture3.Width);
            CoreEffect.Parameters["effectSize"].SetValue(starRectangle.Size());
            CoreEffect.Parameters["effectColor"].SetValue(rgbColor1.ToVector4());
            CoreEffect.Parameters["ringProgress"].SetValue(0.5f);
            CoreEffect.Parameters["fadePercent"].SetValue(attackFadePercent);
            CoreEffect.Parameters["time"].SetValue(-Main.GlobalTimeWrappedHourly * 3f);

            //Apply the shader
            CoreEffect.CurrentTechnique.Passes[0].Apply();

            Main.EntitySpriteDraw(tsorcRevamp.tNoiseTexture3, Projectile.Center - Main.screenPosition, starRectangle, Color.White, starRotation, starOrigin, 1, SpriteEffects.None, 0);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            //Pass relevant data to the shader via these parameters
            CoreEffect.Parameters["textureSize"].SetValue(tsorcRevamp.tNoiseTexture3.Width);
            CoreEffect.Parameters["effectSize"].SetValue(starRectangle.Size());
            CoreEffect.Parameters["effectColor"].SetValue(rgbColor2.ToVector4());
            CoreEffect.Parameters["ringProgress"].SetValue(0.5f);
            CoreEffect.Parameters["fadePercent"].SetValue(attackFadePercent);
            CoreEffect.Parameters["time"].SetValue(-Main.GlobalTimeWrappedHourly * 3f);

            //Apply the shader
            CoreEffect.CurrentTechnique.Passes[0].Apply();

            Main.EntitySpriteDraw(tsorcRevamp.tNoiseTexture3, Projectile.Center - Main.screenPosition, starRectangle, Color.White, -starRotation, starOrigin, 1, SpriteEffects.None, 0);



            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearWrap, DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            return false;
        }
    }
}

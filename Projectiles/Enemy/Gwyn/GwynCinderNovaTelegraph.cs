using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    ///<summary>A harmless inward-collapsing preview of Gwyn's Cinder Nova damage ring.</summary>
    class GwynCinderNovaTelegraph : ModProjectile
    {
        public override string Texture => "tsorcRevamp/Projectiles/InvisibleProj";

        const float StartRadius = 155f;
        const float EndRadius = 18f;
        const float RingHalfThickness = 12f;
        const float DrawPadding = 10f;
        const string TextureRoot = "tsorcRevamp/Textures/Noise/";

        static Asset<Effect> cinderNovaEffect;
        static Asset<Texture2D> smoothNoise;
        static Asset<Texture2D> brokenNoise;

        int GwynIndex => (int)Projectile.ai[0];
        int TotalTicks => Math.Max(1, (int)Projectile.ai[1]);
        float Progress => MathHelper.Clamp(Projectile.localAI[0] / TotalTicks, 0f, 1f);
        float Radius => MathHelper.Lerp(StartRadius, EndRadius, Progress * Progress);

        public override void SetDefaults()
        {
            Projectile.width = 2;
            Projectile.height = 2;
            Projectile.hostile = false;
            Projectile.friendly = false;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 60;
        }

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            Projectile.timeLeft = TotalTicks;
        }

        public override bool? CanDamage() => false;

        public override void AI()
        {
            if (GwynIndex < 0 || GwynIndex >= Main.maxNPCs || !Main.npc[GwynIndex].active)
            {
                Projectile.Kill();
                return;
            }

            Projectile.Center = Main.npc[GwynIndex].Center;
            Projectile.velocity = Vector2.Zero;
            Projectile.localAI[0]++;
        }

        static void LoadAssets()
        {
            cinderNovaEffect ??= ModContent.Request<Effect>("tsorcRevamp/Effects/GwynCinderNova", AssetRequestMode.ImmediateLoad);
            smoothNoise ??= ModContent.Request<Texture2D>(TextureRoot + "T_VFX_NoiseF1", AssetRequestMode.ImmediateLoad);
            brokenNoise ??= ModContent.Request<Texture2D>(TextureRoot + "T_VFX_Noise41", AssetRequestMode.ImmediateLoad);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            LoadAssets();

            float drawRadius = StartRadius + RingHalfThickness + DrawPadding;
            int drawDiameter = (int)Math.Ceiling(drawRadius * 2f);
            Rectangle source = new Rectangle(0, 0, drawDiameter, drawDiameter);
            float opacity = MathHelper.Lerp(0.42f, 0.95f, Progress);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearWrap,
                DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            Effect effect = cinderNovaEffect.Value;
            GraphicsDevice graphicsDevice = Main.instance.GraphicsDevice;
            Texture previousTexture = graphicsDevice.Textures[1];
            SamplerState previousSampler = graphicsDevice.SamplerStates[1];

            try
            {
                graphicsDevice.Textures[1] = brokenNoise.Value;
                graphicsDevice.SamplerStates[1] = SamplerState.LinearWrap;

                effect.Parameters["OuterColor"].SetValue(new Color(166, 28, 4).ToVector3());
                effect.Parameters["FlameColor"].SetValue(new Color(255, 112, 14).ToVector3());
                effect.Parameters["CoreColor"].SetValue(new Color(255, 232, 154).ToVector3());
                effect.Parameters["Opacity"].SetValue(opacity);
                effect.Parameters["Time"].SetValue(Main.GlobalTimeWrappedHourly);
                effect.Parameters["DrawSize"].SetValue(source.Size());
                effect.Parameters["PrimaryTextureSize"].SetValue(smoothNoise.Value.Size());
                effect.Parameters["RingRadius"].SetValue(Radius);
                effect.Parameters["RingHalfThickness"].SetValue(RingHalfThickness);
                effect.Parameters["TrailLength"].SetValue(28f);
                effect.CurrentTechnique.Passes[0].Apply();

                Main.EntitySpriteDraw(smoothNoise.Value, Projectile.Center - Main.screenPosition, source,
                    Color.White, 0f, source.Size() * 0.5f, 1f, SpriteEffects.None, 0);
            }
            finally
            {
                graphicsDevice.Textures[1] = previousTexture;
                graphicsDevice.SamplerStates[1] = previousSampler;
            }

            UsefulFunctions.RestartSpritebatch(ref Main.spriteBatch);
            return false;
        }
    }
}
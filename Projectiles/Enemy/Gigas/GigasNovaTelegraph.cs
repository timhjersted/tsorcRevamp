using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    /// <summary>Harmless projection of Wrath of Gold's filled circle. It follows Gigas for the
    /// 100-tick cast and shows the exact area that will become dangerous on release.</summary>
    class GigasNovaTelegraph : ModProjectile
    {
        const int TelegraphTicks = 100;
        const string TextureRoot = "tsorcRevamp/Textures/Noise/";
        static Asset<Effect> novaEffect;
        static Asset<Texture2D> macroNoise;
        static Asset<Texture2D> detailNoise;
        float MaxRadius => Projectile.ai[0] > 0f ? Projectile.ai[0] : 270f;
        int GigasIndex => (int)Projectile.ai[1];

        public override string Texture => "tsorcRevamp/Projectiles/InvisibleProj";
        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 2;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = TelegraphTicks;
        }
        public override bool? CanDamage() => false;
        public override bool ShouldUpdatePosition() => false;
        public override void AI()
        {
            if (GigasIndex < 0 || GigasIndex >= Main.maxNPCs || !Main.npc[GigasIndex].active)
            {
                Projectile.Kill();
                return;
            }
            Projectile.Center = Main.npc[GigasIndex].Center;
            Projectile.localAI[0]++;
        }
        static void LoadAssets()
        {
            novaEffect ??= ModContent.Request<Effect>("tsorcRevamp/Effects/GigasNovaRing", AssetRequestMode.ImmediateLoad);
            macroNoise ??= ModContent.Request<Texture2D>(TextureRoot + "SmoothNoise", AssetRequestMode.ImmediateLoad);
            detailNoise ??= ModContent.Request<Texture2D>(TextureRoot + "Turbulence_06-512x512", AssetRequestMode.ImmediateLoad);
        }
        public override bool PreDraw(ref Color lightColor)
        {
            LoadAssets();
            int diameter = (int)((MaxRadius + 16f) * 2f);
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearWrap,
                DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            GraphicsDevice device = Main.instance.GraphicsDevice;
            Texture previousTexture = device.Textures[1]; SamplerState previousSampler = device.SamplerStates[1];
            try
            {
                device.Textures[1] = detailNoise.Value; device.SamplerStates[1] = SamplerState.LinearWrap;
                Effect effect = novaEffect.Value;
                effect.CurrentTechnique = effect.Techniques["GigasNovaTelegraph"];
                effect.Parameters["OuterColor"].SetValue(new Color(92, 57, 8).ToVector3());
                effect.Parameters["MiddleColor"].SetValue(new Color(238, 161, 24).ToVector3());
                effect.Parameters["CoreColor"].SetValue(new Color(255, 245, 190).ToVector3());
                effect.Parameters["Opacity"].SetValue(0.58f);
                effect.Parameters["Time"].SetValue(Main.GlobalTimeWrappedHourly);
                effect.Parameters["DrawSize"].SetValue(new Vector2(diameter));
                effect.Parameters["RingRadius"].SetValue(MaxRadius);
                effect.Parameters["Progress"].SetValue(MathHelper.Clamp(Projectile.localAI[0] / TelegraphTicks, 0f, 1f));
                effect.CurrentTechnique.Passes[0].Apply();
                Main.EntitySpriteDraw(macroNoise.Value, Projectile.Center - Main.screenPosition, null, Color.White,
                    0f, macroNoise.Value.Size() * 0.5f, diameter / (float)macroNoise.Value.Width, SpriteEffects.None, 0);
            }
            finally { device.Textures[1] = previousTexture; device.SamplerStates[1] = previousSampler; }
            UsefulFunctions.RestartSpritebatch(ref Main.spriteBatch);
            return false;
        }
    }
}

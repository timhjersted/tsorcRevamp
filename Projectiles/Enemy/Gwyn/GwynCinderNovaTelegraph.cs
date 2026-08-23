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
        //Fixed, not derived from the current radius: the shader's iris is an annulus whose outer
        //wall maxes at (34 + RingRadius * 0.22) * 1.60, so at the 155px start it reaches radius 264.
        //320 leaves 56px of clear quad at every stage of the collapse, and a constant quad means the
        //2px pixel grid does not resize under the effect while it plays.
        const float DrawRadius = 320f;
        const float PixelBlockSize = 2f;
        const string TextureRoot = "tsorcRevamp/Textures/Noise/";

        static Asset<Effect> cinderNovaEffect;
        static Asset<Texture2D> shapeNoise;
        static Asset<Texture2D> detailNoise;

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
            shapeNoise ??= ModContent.Request<Texture2D>(TextureRoot + "Turbulence_06-512x512", AssetRequestMode.ImmediateLoad);
            detailNoise ??= ModContent.Request<Texture2D>(TextureRoot + "Turbulence_07-512x512", AssetRequestMode.ImmediateLoad);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            LoadAssets();

            int drawDiameter = (int)(DrawRadius * 2f);
            Rectangle source = new Rectangle(0, 0, drawDiameter, drawDiameter);
            float opacity = MathHelper.Lerp(0.42f, 0.95f, Progress);

            //Premultiplied alpha, matching the blast. The windup shares the blast's palette and its
            //fire maths on purpose — it is the same flame — and differs only in which edge is crisp.
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.LinearWrap,
                DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            Effect effect = cinderNovaEffect.Value;
            GraphicsDevice graphicsDevice = Main.instance.GraphicsDevice;
            Texture previousTexture = graphicsDevice.Textures[1];
            SamplerState previousSampler = graphicsDevice.SamplerStates[1];

            try
            {
                graphicsDevice.Textures[1] = detailNoise.Value;
                graphicsDevice.SamplerStates[1] = SamplerState.LinearWrap;

                Vector2 drawSize = source.Size();
                Vector2 pixelBlocks = drawSize / PixelBlockSize;
                Vector4 pixelGrid = new Vector4(pixelBlocks.X, pixelBlocks.Y, 1f / pixelBlocks.X, 1f / pixelBlocks.Y);

                effect.CurrentTechnique = effect.Techniques["GwynCinderNovaCharge"];
                effect.Parameters["OuterColor"].SetValue(new Color(58, 9, 3).ToVector3());
                effect.Parameters["FlameColor"].SetValue(new Color(255, 122, 16).ToVector3());
                effect.Parameters["CoreColor"].SetValue(new Color(255, 238, 176).ToVector3());
                effect.Parameters["Opacity"].SetValue(opacity);
                effect.Parameters["Time"].SetValue(Main.GlobalTimeWrappedHourly);
                effect.Parameters["DrawSize"].SetValue(drawSize);
                effect.Parameters["CoordScale"].SetValue(shapeNoise.Value.Size() / drawSize);
                effect.Parameters["PixelGrid"].SetValue(pixelGrid);
                effect.Parameters["RingRadius"].SetValue(Radius);
                effect.Parameters["RingHalfThickness"].SetValue(RingHalfThickness);
                effect.CurrentTechnique.Passes[0].Apply();

                Main.EntitySpriteDraw(shapeNoise.Value, Projectile.Center - Main.screenPosition, source,
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
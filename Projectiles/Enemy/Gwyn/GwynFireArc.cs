using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace tsorcRevamp.Projectiles.Enemy
{
    ///<summary>
    ///A crescent of cinder-fire thrown off Gwyn's heavier greatsword swings, so "melee" reaches a
    ///tile or two past the blade. Shader-drawn with ember dust, short-lived, travels in the swing
    ///direction with a slight fan. ai[0] = direction (-1/1), ai[1] = vertical bias (for overhead /
    ///rising swings). Applies On Fire!.
    ///</summary>
    class GwynFireArc : ModProjectile
    {
        public override string Texture => "tsorcRevamp/Projectiles/InvisibleProj";

        const int Lifetime = 22;
        const string TextureRoot = "tsorcRevamp/Textures/Noise/";

        static Asset<Effect> cinderTrailEffect;
        static Asset<Texture2D> auraNoise;
        static Asset<Texture2D> flowNoise;

        public override void SetDefaults()
        {
            Projectile.hostile = true;
            Projectile.width = 40;
            Projectile.height = 40;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.aiStyle = 0;
            Projectile.timeLeft = Lifetime; //~a couple tiles of reach then it dies
            Projectile.light = 0.5f;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override void AI()
        {
            int dir = (int)Projectile.ai[0] >= 0 ? 1 : -1;
            float vBias = Projectile.ai[1];
            //Drifts outward along the swing, slowing — a crescent flung off the edge
            if (Projectile.localAI[0] == 0f)
            {
                Projectile.velocity = new Vector2(dir * 6f, vBias);
            }
            Projectile.localAI[0]++;
            Projectile.velocity *= 0.9f;

            //Fiery crescent: dense flame biased to the leading edge
            for (int i = 0; i < 4; i++)
            {
                Vector2 pos = Projectile.Center + new Vector2(dir * Main.rand.NextFloat(0f, Projectile.width * 0.5f), Main.rand.NextFloat(-Projectile.height * 0.5f, Projectile.height * 0.5f));
                int type = Main.rand.NextBool() ? DustID.Torch : DustID.GoldFlame;
                int dust = Dust.NewDust(pos, 4, 4, type, dir * 1f, 0f, 60, default, Main.rand.NextFloat(1.2f, 1.8f));
                Main.dust[dust].noGravity = true;
                Main.dust[dust].velocity = new Vector2(dir * Main.rand.NextFloat(0.5f, 2f), Main.rand.NextFloat(-1f, 1f));
            }
            Lighting.AddLight(Projectile.Center, 0.6f, 0.35f, 0.1f);
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(BuffID.OnFire, 4 * 60);
        }

        static void LoadAssets()
        {
            cinderTrailEffect ??= ModContent.Request<Effect>("tsorcRevamp/Effects/GwynCinderTrail", AssetRequestMode.ImmediateLoad);
            auraNoise ??= ModContent.Request<Texture2D>(TextureRoot + "T_Aurax44", AssetRequestMode.ImmediateLoad);
            flowNoise ??= ModContent.Request<Texture2D>(TextureRoot + "T_VFX_NoiseF1", AssetRequestMode.ImmediateLoad);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            LoadAssets();

            Rectangle source = new Rectangle(0, 0, Projectile.width, Projectile.height);
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            float progress = MathHelper.Clamp(Projectile.localAI[0] / Lifetime, 0f, 1f);
            float opacity = MathHelper.Clamp(Projectile.timeLeft / 5f, 0.4f, 1f);
            int direction = (int)Projectile.ai[0] >= 0 ? 1 : -1;
            float rotation = Projectile.velocity.LengthSquared() > 0.001f
                ? Projectile.velocity.ToRotation()
                : (direction > 0 ? 0f : MathHelper.Pi);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.LinearWrap,
                DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);

            Effect effect = cinderTrailEffect.Value;
            GraphicsDevice graphicsDevice = Main.instance.GraphicsDevice;
            Texture previousTexture = graphicsDevice.Textures[1];
            SamplerState previousSampler = graphicsDevice.SamplerStates[1];

            try
            {
                graphicsDevice.Textures[1] = flowNoise.Value;
                graphicsDevice.SamplerStates[1] = SamplerState.LinearWrap;

                effect.CurrentTechnique = effect.Techniques["GwynCinderArc"];
                effect.Parameters["CinderColor"].SetValue(new Color(255, 42, 4).ToVector3());
                effect.Parameters["FlameColor"].SetValue(new Color(255, 128, 15).ToVector3());
                effect.Parameters["CoreColor"].SetValue(new Color(255, 230, 146).ToVector3());
                effect.Parameters["Opacity"].SetValue(opacity);
                effect.Parameters["Time"].SetValue(Main.GlobalTimeWrappedHourly);
                effect.Parameters["DrawSize"].SetValue(source.Size());
                effect.Parameters["PrimaryTextureSize"].SetValue(auraNoise.Value.Size());
                effect.Parameters["Progress"].SetValue(progress);
                effect.CurrentTechnique.Passes[0].Apply();

                Main.EntitySpriteDraw(auraNoise.Value, drawPosition, source, Color.White, rotation,
                    source.Size() * 0.5f, 1f, SpriteEffects.None, 0);
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
